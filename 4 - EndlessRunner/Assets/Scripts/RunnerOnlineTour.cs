using System.Collections;
using System.Linq;
using Gamebox;
using Gamebox.Online;
using UnityEngine;

namespace Portfolio.EndlessRunner
{
    /// <summary>
    /// Development players only. Started with <c>-runner-online &lt;role&gt; &lt;folder&gt; [name]</c>, it runs an online
    /// race by itself and saves a log and screenshots into the folder, then quits (<see cref="OnlineTour"/>): the
    /// <c>host</c> opens a room (for the first level, or the one after <c>-runner-level</c>) and starts it once somebody
    /// joined and is ready, <c>join</c> joins the first room it sees, and both run with the <see cref="RunnerAutopilot"/>.
    /// Other guests try what happens between people: <c>crash</c> runs without the autopilot, so it is soon out of hearts
    /// and watches the rest of the race, and <c>quitter</c> leaves in the middle of it. <c>-runner-give-up &lt;seconds&gt;</c>
    /// takes the autopilot away after a while, which ends an endless run, and <c>-runner-races &lt;n&gt;</c> runs several
    /// races in the room. The log says who got which coins and what the track looked like, to compare between the
    /// players: no coin may count twice, and together they cannot have more than the track holds.
    /// <c>-runner-online solo &lt;folder&gt;</c> plays the first level alone and offline the same way. Run the players with
    /// <c>-gamebox-identity</c> to tell them apart, and <c>-gamebox-server</c> / <c>-gamebox-database</c> for a test server.
    /// </summary>
    public class RunnerOnlineTour : OnlineTour
    {
        private const string Argument = "-runner-online";
        private const string LevelArgument = "-runner-level";
        private const string GiveUpArgument = "-runner-give-up";
        private const string RacesArgument = "-runner-races";

        private RunnerGameManager manager;
        private RunnerAutopilot pilot;
        private bool left;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Launch()
        {
            Bootstrap<RunnerOnlineTour>(Argument);
        }

        private bool Ended => manager.State.Is(BaseGameState.Victory) || manager.State.Is(BaseGameState.GameOver);

        private IEnumerator Start()
        {
            OpenLog(true);
            while ((manager = FindAnyObjectByType<RunnerGameManager>()) == null || manager.State == null)
            {
                yield return null;
            }
            yield return new WaitForSeconds(1f);
            yield return Shot("00_level_select");
            if (Role == "solo")
            {
                yield return Solo();
                yield break;
            }
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
                var choices = manager.online.LevelChoices;
                int wanted = NumberArgument(LevelArgument, choices[0].Index);
                int level = choices.Any(choice => choice.Index == wanted) ? wanted : choices[0].Index;
                string options = manager.online.ComposeOptions(level, string.Empty);
                Note($"opening a room for level {level} with options {options}");
                yield return HostRoom("Tour race", level, options, 2);
                yield return Shot("01_room");
            }
            else
            {
                yield return JoinFirstRoom(120f);
            }

            int races = Mathf.Max(1, NumberArgument(RacesArgument, 1));
            for (int number = 1; number <= races && !left; number++)
            {
                string tag = number == 1 ? string.Empty : $"race{number}_";
                if (Hosting)
                {
                    yield return WaitForGuests(1, 120f);
                    yield return new WaitForSeconds(0.5f);
                    yield return Shot($"{tag}02_everybody_ready");
                    StartRoom();
                }
                else
                {
                    Server.SetReady(true, error => Note("ready: " + (error ?? "ok")));
                    yield return new WaitForSeconds(0.7f);
                    yield return Shot($"{tag}02_room");
                }
                yield return Race(tag);
            }
            if (left)
            {
                yield break;
            }
            // The guest leaves first, so the host still sees the room lose a member.
            yield return new WaitForSeconds(Hosting ? 3f : 0.5f);
            Server.LeaveRoom();
            yield return new WaitForSeconds(1f);
            Note("done");
            Quit();
        }

        /// <summary>A race of the room, from its start to the way back into the room.</summary>
        private IEnumerator Race(string tag)
        {
            yield return WaitFor(() => manager.InRace && manager.IsGameRunning, 60f, "the race");
            if (!manager.InRace)
            {
                left = true;
                yield return Fail("the race did not start");
                yield break;
            }
            Note($"racing on seat {manager.LocalSeat} of {manager.Racers.Count}; the track holds {manager.track.LevelCoins} coins; "
                + $"layout of the first 250 m: {manager.track.LayoutHash(250f):X8}");
            if (Role != "crash" && pilot == null)
            {
                pilot = manager.Player.gameObject.AddComponent<RunnerAutopilot>();
            }
            float giveUp = NumberArgument(GiveUpArgument, -1f);
            float started = Time.unscaledTime;
            yield return new WaitForSeconds(1.2f);
            yield return Shot($"{tag}03_countdown");
            float[] pauses = { 4.5f, 6f, 8f, 10f };
            for (int i = 0; i < pauses.Length && !Ended; i++)
            {
                yield return WaitOrEnd(pauses[i]);
                Progress();
                yield return Shot($"{tag}{4 + i:00}_{(manager.IsWatching ? "watching" : "running")}");
                if (Role == "quitter" && i == 1)
                {
                    Note("leaving the race");
                    manager.online.LeaveMatch();
                    yield return new WaitForSeconds(2f);
                    yield return Shot($"{tag}06_left");
                    Note($"in a race: {manager.InRace}; in a session: {manager.InSession}; in a room: {Server.InRoom}; state {manager.State}");
                    Note("done");
                    left = true;
                    Quit();
                    yield break;
                }
                if (Hosting && i == 1 && !manager.IsWatching && !Ended)
                {
                    // A race does not pause: what pauses a game alone opens the match menu, and the runner runs on.
                    float before = manager.Distance;
                    manager.ActiveController.TransitionState(BaseGameState.Paused);
                    yield return new WaitForSeconds(0.6f);
                    yield return Shot($"{tag}05_match_menu");
                    Note($"match menu: state {manager.State}, time scale {Time.timeScale}, ran {manager.Distance - before:0.0} m meanwhile");
                    manager.ActiveController.TransitionState(BaseGameState.Running);
                }
            }
            while (!Ended && !manager.IsWatching && Time.unscaledTime - started < 300f)
            {
                if (pilot != null && giveUp > 0f && Time.unscaledTime - started > giveUp)
                {
                    Note("giving up: the autopilot lets go");
                    Destroy(pilot);
                    pilot = null;
                }
                yield return null;
            }
            Note(Ended || manager.IsWatching ? "the own run is over" : "TIMEOUT waiting for the end of the own run");
            if (manager.IsWatching)
            {
                Progress();
                yield return new WaitForSeconds(2.5f);
                yield return Shot($"{tag}08_watching");
                yield return WaitOrEnd(6f);
                if (manager.IsWatching)
                {
                    Progress();
                    yield return Shot($"{tag}08_watching_later");
                }
            }
            yield return WaitFor(() => Ended, 600f, "the end of the race");
            yield return new WaitForSeconds(2.5f);
            yield return Shot($"{tag}09_results");
            Report();

            manager.ReturnToLevelSelect();
            yield return new WaitForSeconds(1.5f);
            yield return Shot($"{tag}10_back_in_the_room");
            Note($"in a race: {manager.InRace}; in a session: {manager.InSession}; in a room: {Server.InRoom}");
        }

        /// <summary>The first level alone and offline: the game for one has to play as it always did.</summary>
        private IEnumerator Solo()
        {
            manager.SelectLevel(0);
            manager.PlaySelectedLevel();
            yield return WaitFor(() => manager.IsGameRunning, 10f, "the run");
            Note($"running alone: in a session {manager.InSession}, in a race {manager.InRace}; the track holds {manager.track.LevelCoins} coins; "
                + $"layout of the first 250 m: {manager.track.LayoutHash(250f):X8}");
            pilot = manager.Player.gameObject.AddComponent<RunnerAutopilot>();
            yield return new WaitForSeconds(1.2f);
            yield return Shot("03_countdown");
            yield return WaitOrEnd(9f);
            Note($"at {manager.Distance:0} m with {manager.Coins} coins");
            yield return Shot("04_running");
            manager.ActiveController.TransitionState(BaseGameState.Paused);
            yield return new WaitForSecondsRealtime(1f);
            float pausedAt = manager.Distance;
            yield return Shot("05_paused");
            yield return new WaitForSecondsRealtime(1f);
            Note($"paused: state {manager.State}, time scale {Time.timeScale}, moved {manager.Distance - pausedAt:0.00} m in a second");
            manager.ActiveController.TransitionState(BaseGameState.Running);
            yield return WaitFor(() => Ended, 240f, "the end of the run");
            yield return new WaitForSeconds(2.5f);
            yield return Shot("09_results");
            Note($"result: {manager.State}; {manager.Distance:0} m, {manager.Coins} coins, score {manager.PlayerScore:0}");
            manager.ReturnToLevelSelect();
            yield return new WaitForSeconds(1.5f);
            yield return Shot("10_level_select");
            Note("done");
            Quit();
        }

        private void Progress()
        {
            Note($"at {manager.Distance:0} m with {manager.Coins} coins; claims settled {manager.Claims.SettledCount}, pending {manager.Claims.PendingCount}; "
                + string.Join(", ", manager.Racers.Select(racer => $"{racer.Name} {racer.Distance:0} m {racer.Coins} c {racer.Score} p{(racer.Done ? " done" : "")}")));
        }

        /// <summary>The books of the race as this player has them; the other player's log has to agree.</summary>
        private void Report()
        {
            CoinClaims claims = manager.Claims;
            Note($"result: {manager.State}; " + string.Join(", ", manager.Racers.OrderBy(racer => racer.Place)
                .Select(racer => $"{RaceStandings.Ordinal(racer.Place)} {racer.Name} {racer.Score} ({racer.Distance:0} m, {racer.Coins} coins)")));
            Note($"coins: counted {manager.Coins} here; the track holds {manager.track.LevelCoins}; all runners took {claims.SettledCoins} in {claims.SettledCount} pieces; "
                + $"pending {claims.PendingCount}");
            foreach (Racer racer in manager.Racers)
            {
                Note($"pieces of seat {racer.Seat}{(racer.Local ? " (local)" : "")}: {claims.PiecesOf(racer.Seat)} worth {claims.CoinsOf(racer.Seat)}: "
                    + string.Join(" ", claims.PiecesOwnedBy(racer.Seat)));
            }
        }

        private IEnumerator WaitOrEnd(float seconds)
        {
            float waited = 0f;
            while (waited < seconds && !Ended)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
        }
    }
}
