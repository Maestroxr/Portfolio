using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using Gamebox;
using Gamebox.Online;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// Started with <c>-asteroids-online host &lt;folder&gt;</c> or <c>-asteroids-online join &lt;folder&gt;</c>, it flies an
    /// online mission by itself and saves a screenshot of every step into the folder, then quits: the host opens a room
    /// and starts it once somebody joined and is ready, the other one joins the first room it sees, and both ships are
    /// flown by the <see cref="AsteroidsAutopilot"/>. The screenshots of a mission are taken the same number of seconds
    /// after it began on both clients, and a line with the bodies of the playfield goes into the log with each, so the two
    /// can be compared. <c>-asteroids-level &lt;index&gt;</c> picks the mission, <c>-asteroids-lives &lt;n&gt;</c> the ships per
    /// pilot and <c>-asteroids-leave &lt;seconds&gt;</c> makes this pilot leave the match that long after it began. Run two
    /// players (with <c>-gamebox-identity</c> to tell them apart, and <c>-gamebox-server</c> / <c>-gamebox-database</c> for a
    /// test server) to try a mission end to end. Without the argument it does nothing.
    /// </summary>
    public class AsteroidsOnlineTour : MonoBehaviour
    {
        private const string Argument = "-asteroids-online";

        private static readonly float[] ShotTimes = { 4f, 12f, 24f, 40f, 60f, 90f };

        private bool host;
        private string mode;
        private string folder;
        private int level;
        private int lives = 5;
        private float leaveAfter = -1f;
        private AsteroidsGameManager manager;
        private StreamWriter log;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 2; i++)
            {
                if (args[i] != Argument)
                {
                    continue;
                }
                var tourObject = new GameObject("Asteroids Online Tour");
                DontDestroyOnLoad(tourObject);
                var tour = tourObject.AddComponent<AsteroidsOnlineTour>();
                tour.mode = args[i + 1];
                tour.host = tour.mode == "host";
                tour.folder = args[i + 2];
                tour.level = (int)Number(args, "-asteroids-level", 0f);
                tour.lives = (int)Number(args, "-asteroids-lives", 5f);
                tour.leaveAfter = Number(args, "-asteroids-leave", -1f);
                return;
            }
        }

        private static float Number(string[] args, string name, float fallback)
        {
            int index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length && float.TryParse(args[index + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out float value)
                ? value
                : fallback;
        }

        private IEnumerator Start()
        {
            Directory.CreateDirectory(folder);
            string who = host ? "host" : mode;
            log = new StreamWriter(Path.Combine(folder, $"{who}.log")) { AutoFlush = true };
            Application.logMessageReceived += (message, stack, type) =>
            {
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Warning)
                {
                    Note($"{type}: {message}");
                }
            };
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
            yield return Shot($"{who}_00_missions");
            ServerClient server = manager.online.ServerClient;
            server.Failed += error => Note("server: " + error);
            manager.OpenOnline();
            yield return WaitFor(() => server.IsLoggedIn, 30f, "login");
            if (!server.IsLoggedIn)
            {
                yield return Fail("no login");
                yield break;
            }
            Note($"logged in as {server.LocalPlayer.Name}");
            yield return new WaitForSeconds(0.5f);

            if (host)
            {
                string options = RoomOptions.Write(CoopRules.LivesKey, lives, CoopRules.MissionsKey, manager.LevelCount);
                server.CreateRoom("Tour room", level, options, 2, error => Note("create room: " + (error ?? "ok")));
                yield return WaitFor(() => server.InRoom, 15f, "own room");
                yield return Shot($"{who}_01_room");
                yield return WaitFor(() => server.Members.Count >= 2 && server.Members.All(member => member.Ready || server.IsLocal(member)), 180f, "a ready guest");
                yield return new WaitForSeconds(0.5f);
                yield return Shot($"{who}_02_everybody_ready");
                server.StartRoom(error => Note("start room: " + (error ?? "ok")));
            }
            else
            {
                yield return WaitFor(() => server.Rooms.Any(room => room.Phase != RoomPhase.Playing), 180f, "a room");
                yield return Shot($"{who}_01_rooms");
                RoomInfo open = server.Rooms.First(room => room.Phase != RoomPhase.Playing);
                server.JoinRoom(open.Id, error => Note("join room: " + (error ?? "ok")));
                yield return WaitFor(() => server.InRoom, 15f, "the room");
                server.SetReady(true, error => Note("ready: " + (error ?? "ok")));
                yield return new WaitForSeconds(0.7f);
                yield return Shot($"{who}_02_room");
            }

            yield return WaitFor(() => manager.IsCoop && manager.IsGameRunning, 60f, "the mission");
            Note($"mission {manager.LevelIndex} started, simulates: {manager.Simulates}, playfield {manager.Playground.HalfSize}");
            yield return Shot($"{who}_03_briefing");
            yield return WaitFor(() => manager.IsMissionActive, 20f, "the countdown");
            var pilot = manager.Ship.GetComponent<AsteroidsAutopilot>();
            if (pilot != null)
            {
                pilot.enabled = true;
            }

            // The same moments on both clients: seconds since the mission went live.
            float began = Time.realtimeSinceStartup;
            int shot = 0;
            bool left = false;
            while (!Ended())
            {
                float elapsed = Time.realtimeSinceStartup - began;
                if (shot < ShotTimes.Length && elapsed >= ShotTimes[shot])
                {
                    Note($"t+{ShotTimes[shot]:0}s {Describe()}");
                    yield return Shot($"{who}_04_flying_{shot + 1}");
                    shot++;
                }
                if (!left && leaveAfter > 0f && elapsed >= leaveAfter)
                {
                    left = true;
                    Note("leaving the match");
                    manager.online.LeaveMatch();
                    yield return new WaitForSeconds(2f);
                    yield return Shot($"{who}_05_left");
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
            yield return Shot($"{who}_05_results");
            Note("standings: " + manager.online.DescribeStandings().Replace("\n", " | "));

            manager.ReturnToMissionSelect();
            yield return new WaitForSeconds(1.5f);
            yield return Shot($"{who}_06_back_in_the_room");
            Note($"back in the room: in session {manager.InSession}, playfield fixed {manager.Playground.IsFixed}, stand-ins {manager.RemoteShips.Count}");
            // The guest leaves first, so the host still sees the room lose a member.
            yield return new WaitForSeconds(host ? 3f : 0.5f);
            server.LeaveRoom();
            yield return new WaitForSeconds(1f);
            Note("done");
            Quit();
        }

        private bool Ended()
        {
            return manager.State.Is(BaseGameState.Victory) || manager.State.Is(BaseGameState.GameOver) || !manager.InSession;
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

        private IEnumerator WaitFor(Func<bool> condition, float timeout, string what)
        {
            float waited = 0f;
            while (!condition() && waited < timeout)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            Note(condition() ? $"got {what} after {waited:0.0}s" : $"TIMEOUT waiting for {what}");
        }

        private IEnumerator Shot(string shotName)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, shotName + ".png"));
            yield return null;
        }

        private IEnumerator Fail(string reason)
        {
            Note("FAILED: " + reason);
            yield return Shot(host ? "host_failed" : "join_failed");
            Quit();
        }

        private void Note(string message)
        {
            log?.WriteLine($"{Time.realtimeSinceStartup:0.0}s {message}");
        }

        private void Quit()
        {
            log?.Dispose();
            log = null;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
