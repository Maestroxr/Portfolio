using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Gamebox;
using Gamebox.Online;
using UnityEngine;

namespace Portfolio.MemoryCards
{
    /// <summary>
    /// Development players only. Started with <c>-memorycards-online &lt;role&gt; &lt;folder&gt; [name]</c>, it plays an
    /// online versus game by itself and saves a log and screenshots into the folder, then quits (<see cref="OnlineTour"/>):
    /// the <c>host</c> opens a room (the third level a room can play, ten seconds a turn) and starts it once somebody
    /// joined and is ready, <c>join</c> joins the first room it sees, and both play their turns with the
    /// <see cref="MemoryCardsAutopilot"/>, which online knows only the cards that were turned. Two more guests try what
    /// goes wrong between people: <c>idle</c> joins and never plays, so every turn of theirs runs out, and <c>quitter</c>
    /// leaves in the middle of the game. <c>-memorycards-mistakes 0.6</c> makes a player miss that often, so the turn
    /// changes hands, <c>-memorycards-bombs</c> lets it flip bombs, and the host's <c>-memorycards-level &lt;n&gt;</c> picks
    /// another of the levels a room can play. Run the players with <c>-gamebox-identity</c> to tell them apart, and
    /// <c>-gamebox-server</c> / <c>-gamebox-database</c> for a test server.
    /// </summary>
    public class MemoryCardsOnlineTour : OnlineTour
    {
        private const string Argument = "-memorycards-online";
        private const string MistakesArgument = "-memorycards-mistakes";
        private const string BombsArgument = "-memorycards-bombs";
        private const string LevelArgument = "-memorycards-level";
        /// <summary>Which of the levels a room can play the host picks unless told otherwise: the third, or the last.</summary>
        private const int LevelChoice = 2;
        private const int TurnSeconds = 10;

        private MemoryCardsGameManager manager;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Launch()
        {
            Bootstrap<MemoryCardsOnlineTour>(Argument);
        }

        private bool Over => manager.State.Is(BaseGameState.Victory) || manager.State.Is(BaseGameState.GameOver);

        private IEnumerator Start()
        {
            OpenLog(true);
            while ((manager = FindAnyObjectByType<MemoryCardsGameManager>()) == null || manager.Progress == null)
            {
                yield return null;
            }
            yield return new WaitForSeconds(1f);
            if (manager.online == null)
            {
                yield return Fail("the scene has no online controller");
                yield break;
            }
            yield return LogIn(manager.online);
            if (!LoggedIn)
            {
                yield return Fail("no login");
                yield break;
            }
            yield return new WaitForSeconds(0.5f);

            if (Hosting)
            {
                IReadOnlyList<RoomLevelChoice> choices = manager.online.LevelChoices;
                if (choices.Count == 0)
                {
                    yield return Fail("no level to play online");
                    yield break;
                }
                int level = choices[Mathf.Clamp(NumberArgument(LevelArgument, LevelChoice), 0, choices.Count - 1)].Index;
                string options = manager.online.ComposeOptions(level, RoomOptions.Write(RoomOptions.TurnOption, TurnSeconds));
                yield return HostRoom("Tour room", level, options, 2);
                yield return Shot("01_room");
                yield return WaitForGuests(1, 120f);
                yield return new WaitForSeconds(0.5f);
                yield return Shot("02_everybody_ready");
                StartRoom();
            }
            else
            {
                yield return JoinFirstRoom(120f);
                yield return new WaitForSeconds(0.7f);
                yield return Shot("02_room");
            }

            yield return WaitFor(() => manager.IsOnlineVersus && manager.IsPlaying, 60f, "the game");
            if (!manager.IsOnlineVersus)
            {
                yield return Fail("the game did not start");
                yield break;
            }
            Note("playing");
            var pilot = gameObject.AddComponent<MemoryCardsAutopilot>();
            pilot.manager = manager;
            pilot.mistakeRate = Mathf.Clamp01(NumberArgument(MistakesArgument, 0.1f));
            pilot.avoidBombs = !HasArgument(BombsArgument);
            pilot.interval = 0.55f;
            pilot.enabled = Role != "idle";
            yield return new WaitForSeconds(7f);
            yield return Shot("03_playing");
            if (Role == "quitter")
            {
                Note("leaving the match");
                manager.online.LeaveMatch();
                yield return new WaitForSeconds(2f);
                yield return Shot("04_left");
                Note("done");
                Quit();
                yield break;
            }
            yield return new WaitForSeconds(14f);
            yield return Shot("04_playing_later");
            yield return WaitFor(() => Over, 600f, "the end of the game");
            pilot.enabled = false;
            yield return new WaitForSeconds(2.5f);
            yield return Shot("05_results");
            Note($"result: {manager.State}; " + string.Join(", ", manager.Versus.Seats.Select(seat => $"{seat.Name} {seat.Score} ({seat.Sets} sets)")));
            yield return WaitFor(() => Server.CurrentRoom != null && Server.CurrentRoom.Phase == RoomPhase.Finished, 30f, "the room to finish");
            Note("places: " + string.Join(", ", Server.Members.Select(member => $"{Server.NameOf(member.Identity)} {member.Place}. with {member.Score}")));

            manager.ReturnToLevelSelect();
            yield return new WaitForSeconds(1.5f);
            yield return Shot("06_back_in_the_room");
            // The guest leaves first, so the host still sees the room lose a member.
            yield return new WaitForSeconds(Hosting ? 3f : 0.5f);
            Server.LeaveRoom();
            yield return new WaitForSeconds(1f);
            Note("done");
            Quit();
        }
    }
}
