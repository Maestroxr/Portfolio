using System.Collections;
using System.Linq;
using Gamebox;
using Gamebox.Online;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// Development players only. Started with <c>-asteroids-online &lt;role&gt; &lt;folder&gt; [name]</c>, it flies an
    /// online mission by itself and saves a log and screenshots into the folder, then quits (<see cref="OnlineTour"/>):
    /// the <c>host</c> opens a room and starts it once somebody joined and is ready, <c>join</c> joins the first room it
    /// sees, and both ships are flown by the <see cref="AsteroidsAutopilot"/>. The screenshots of a mission are taken the
    /// same number of seconds after it began on both clients, and a line with the bodies of the playfield goes into the
    /// log with each, so the two can be compared. <c>-asteroids-level &lt;index&gt;</c> picks the mission,
    /// <c>-asteroids-lives &lt;n&gt;</c> the ships per pilot and <c>-asteroids-leave &lt;seconds&gt;</c> makes this pilot
    /// leave the match that long after it began. Run the players with <c>-gamebox-identity</c> to tell them apart, and
    /// <c>-gamebox-server</c> / <c>-gamebox-database</c> for a test server.
    /// </summary>
    public class AsteroidsOnlineTour : OnlineTour
    {
        private const string Argument = "-asteroids-online";
        private const string LevelArgument = "-asteroids-level";
        private const string LivesArgument = "-asteroids-lives";
        private const string LeaveArgument = "-asteroids-leave";

        private static readonly float[] ShotTimes = { 4f, 12f, 24f, 40f, 60f, 90f };

        private AsteroidsGameManager manager;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Launch()
        {
            Bootstrap<AsteroidsOnlineTour>(Argument);
        }

        private bool Ended => manager.State.Is(BaseGameState.Victory) || manager.State.Is(BaseGameState.GameOver) || !manager.InSession;

        private IEnumerator Start()
        {
            OpenLog(true);
            while ((manager = FindAnyObjectByType<AsteroidsGameManager>()) == null || manager.Progress == null)
            {
                yield return null;
            }
            yield return new WaitForSeconds(1f);
            if (manager.online == null)
            {
                yield return Fail("the scene has no online controller");
                yield break;
            }
            yield return Shot("00_missions");
            yield return LogIn(manager.online);
            if (!LoggedIn)
            {
                yield return Fail("no login");
                yield break;
            }
            yield return new WaitForSeconds(0.5f);

            if (Hosting)
            {
                string options = RoomOptions.Write(CoopRules.LivesKey, NumberArgument(LivesArgument, 5), CoopRules.MissionsKey, manager.LevelCount);
                yield return HostRoom("Tour room", NumberArgument(LevelArgument, 0), options, 2);
                yield return Shot("01_room");
                yield return WaitForGuests(1, 180f);
                yield return new WaitForSeconds(0.5f);
                yield return Shot("02_everybody_ready");
                StartRoom();
            }
            else
            {
                yield return JoinFirstRoom(180f);
                yield return new WaitForSeconds(0.7f);
                yield return Shot("02_room");
            }

            yield return WaitFor(() => manager.IsCoop && manager.IsGameRunning, 60f, "the mission");
            if (!manager.IsCoop)
            {
                yield return Fail("the mission did not start");
                yield break;
            }
            Note($"mission {manager.LevelIndex} started, simulates: {manager.Simulates}, playfield {manager.Playground.HalfSize}");
            yield return Shot("03_briefing");
            yield return WaitFor(() => manager.IsMissionActive, 20f, "the countdown");
            var pilot = manager.Ship.GetComponent<AsteroidsAutopilot>();
            if (pilot != null)
            {
                pilot.enabled = true;
            }

            // The same moments on both clients: seconds since the mission went live.
            float leaveAfter = NumberArgument(LeaveArgument, -1f);
            float began = Time.realtimeSinceStartup;
            int shot = 0;
            while (!Ended)
            {
                float elapsed = Time.realtimeSinceStartup - began;
                if (shot < ShotTimes.Length && elapsed >= ShotTimes[shot])
                {
                    Note($"t+{ShotTimes[shot]:0}s {Describe()}");
                    yield return Shot($"04_flying_{shot + 1}");
                    shot++;
                }
                if (leaveAfter > 0f && elapsed >= leaveAfter)
                {
                    Note("leaving the match");
                    manager.online.LeaveMatch();
                    yield return new WaitForSeconds(2f);
                    yield return Shot("05_left");
                    Note("done");
                    Quit();
                    yield break;
                }
                if (elapsed > 600f)
                {
                    Note("TIMEOUT waiting for the end of the mission");
                    break;
                }
                yield return null;
            }
            if (pilot != null)
            {
                pilot.enabled = false;
            }
            Note($"ended after {Time.realtimeSinceStartup - began:0.0}s in state {manager.State}; {Describe()}");
            yield return new WaitForSeconds(2.5f);
            yield return Shot("05_results");
            Note("standings: " + manager.online.DescribeStandings().Replace("\n", " | "));

            manager.ReturnToMissionSelect();
            yield return new WaitForSeconds(1.5f);
            yield return Shot("06_back_in_the_room");
            Note($"back in the room: in session {manager.InSession}, playfield fixed {manager.Playground.IsFixed}, stand-ins {manager.RemoteShips.Count}");
            // The guest leaves first, so the host still sees the room lose a member.
            yield return new WaitForSeconds(Hosting ? 3f : 0.5f);
            Server.LeaveRoom();
            yield return new WaitForSeconds(1f);
            Note("done");
            Quit();
        }

        /// <summary>What this client has in its playfield right now, for comparing it with the other one.</summary>
        private string Describe()
        {
            SpaceField field = manager.Field;
            AsteroidsPlayer ship = manager.Ship;
            string ships = string.Join(", ", manager.RemoteShips.Select(remote =>
                $"{remote.Player.DisplayName} at ({remote.transform.position.x:0.0}, {remote.transform.position.y:0.0}) {(remote.Flying ? "flying" : "down")}"));
            return $"score {manager.Scoring.Score}, lives {manager.Lives}, ship ({ship.Position.x:0.0}, {ship.Position.y:0.0}) {(ship.IsAlive ? "alive" : "down")}; " +
                   $"others: [{ships}]; field: targets {field.Targets.Count}, enemy shots {field.EnemyShots.Count}, pickups {field.Rewards.Count}, " +
                   $"own and ghost shots {field.PlayerShots.Count}; net: {manager.online.Replication.Describe()}";
        }
    }
}
