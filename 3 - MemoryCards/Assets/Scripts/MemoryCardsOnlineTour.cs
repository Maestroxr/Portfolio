using System;
using System.Collections;
using System.IO;
using System.Linq;
using Gamebox;
using Gamebox.Online;
using UnityEngine;

namespace Portfolio.MemoryCards
{
    /// <summary>
    /// Started with <c>-memorycards-online host &lt;folder&gt;</c> or <c>-memorycards-online join &lt;folder&gt;</c>, it
    /// plays an online versus game by itself and saves a screenshot of every step into the folder, then quits: the
    /// host opens a room and starts it once somebody joined and is ready, the other one joins the first room it sees,
    /// and both play their turns with the <see cref="MemoryCardsAutopilot"/>, which online knows only the cards that
    /// were turned. Two more guests try what goes wrong between people: <c>idle</c> joins and never plays, so every turn
    /// of theirs runs out, and <c>quitter</c> leaves in the middle of the game (<c>-memorycards-mistakes 0.6</c> makes a
    /// player miss that often, so the turn changes hands). Run two players (with
    /// <c>-gamebox-identity</c> to tell them apart, and <c>-gamebox-server</c> / <c>-gamebox-database</c> for a test
    /// server) to try a game end to end. Without the argument it does nothing.
    /// </summary>
    public class MemoryCardsOnlineTour : MonoBehaviour
    {
        private const string Argument = "-memorycards-online";

        private bool host;
        private string mode;
        private string folder;
        private MemoryCardsGameManager manager;
        private StreamWriter log;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 2; i++)
            {
                if (args[i] == Argument)
                {
                    var tourObject = new GameObject("Memory Cards Online Tour");
                    DontDestroyOnLoad(tourObject);
                    var tour = tourObject.AddComponent<MemoryCardsOnlineTour>();
                    tour.mode = args[i + 1];
                    tour.host = tour.mode == "host";
                    tour.folder = args[i + 2];
                    return;
                }
            }
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
                int level = manager.online.LevelChoices[Mathf.Min(2, manager.online.LevelChoices.Count - 1)].Index;
                server.CreateRoom("Tour room", level, manager.online.ComposeOptions(level, "turn=10"), 2, error => Note("create room: " + (error ?? "ok")));
                yield return WaitFor(() => server.InRoom, 15f, "own room");
                yield return Shot($"{who}_01_room");
                yield return WaitFor(() => server.Members.Count >= 2 && server.Members.All(member => member.Ready || server.IsLocal(member)), 120f, "a ready guest");
                yield return new WaitForSeconds(0.5f);
                yield return Shot($"{who}_02_everybody_ready");
                server.StartRoom(error => Note("start room: " + (error ?? "ok")));
            }
            else
            {
                yield return WaitFor(() => server.Rooms.Any(room => room.Phase != RoomPhase.Playing), 120f, "a room");
                yield return Shot($"{who}_01_rooms");
                RoomInfo open = server.Rooms.First(room => room.Phase != RoomPhase.Playing);
                server.JoinRoom(open.Id, error => Note("join room: " + (error ?? "ok")));
                yield return WaitFor(() => server.InRoom, 15f, "the room");
                server.SetReady(true, error => Note("ready: " + (error ?? "ok")));
                yield return new WaitForSeconds(0.7f);
                yield return Shot($"{who}_02_room");
            }

            yield return WaitFor(() => manager.IsOnlineVersus && manager.IsPlaying, 60f, "the game");
            Note("playing");
            var pilot = gameObject.AddComponent<MemoryCardsAutopilot>();
            pilot.manager = manager;
            pilot.mistakeRate = float.TryParse(MobilePlatform.ArgumentValue("-memorycards-mistakes"), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float mistakes) ? Mathf.Clamp01(mistakes) : 0.1f;
            pilot.interval = 0.55f;
            pilot.enabled = mode != "idle";
            yield return new WaitForSeconds(7f);
            yield return Shot($"{who}_03_playing");
            if (mode == "quitter")
            {
                Note("leaving the match");
                manager.online.LeaveMatch();
                yield return new WaitForSeconds(2f);
                yield return Shot($"{who}_04_left");
                Note("done");
                Quit();
                yield break;
            }
            yield return new WaitForSeconds(14f);
            yield return Shot($"{who}_04_playing_later");
            yield return WaitFor(() => manager.State.Is(BaseGameState.Victory) || manager.State.Is(BaseGameState.GameOver), 600f, "the end of the game");
            pilot.enabled = false;
            yield return new WaitForSeconds(2.5f);
            yield return Shot($"{who}_05_results");
            Note($"result: {manager.State}; " + string.Join(", ", manager.Versus.Seats.Select(seat => $"{seat.Name} {seat.Score} ({seat.Sets} sets)")));

            manager.ReturnToLevelSelect();
            yield return new WaitForSeconds(1.5f);
            yield return Shot($"{who}_06_back_in_the_room");
            // The guest leaves first, so the host still sees the room lose a member.
            yield return new WaitForSeconds(host ? 3f : 0.5f);
            server.LeaveRoom();
            yield return new WaitForSeconds(1f);
            Note("done");
            Quit();
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
