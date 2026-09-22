using System;
using System.Collections;
using System.IO;
using System.Linq;
using Gamebox;
using Gamebox.Online;
using UnityEngine;

namespace Portfolio.EndlessRunner
{
    /// <summary>
    /// Started with <c>-runner-online host &lt;folder&gt;</c> or <c>-runner-online join &lt;folder&gt;</c>, it runs an online
    /// race by itself and saves a screenshot of every step into the folder, then quits: the host opens a room (for the
    /// first level, or the one after <c>-runner-level</c>) and starts it once somebody joined and is ready, the other one
    /// joins the first room it sees, and both run with the <see cref="RunnerAutopilot"/>. Other guests try what happens
    /// between people: <c>crash</c> runs without the autopilot, so it is soon out of hearts and watches the rest of the
    /// race, and <c>quitter</c> leaves in the middle of it. <c>-runner-give-up &lt;seconds&gt;</c> takes the autopilot away
    /// after a while, which ends an endless run, and <c>-runner-races &lt;n&gt;</c> runs several races in the room. The log
    /// says who got which coins and what the track looked like, to compare between the players: no coin may count twice,
    /// and together they cannot have more than the track holds. <c>-runner-online solo &lt;folder&gt;</c> plays the first
    /// level alone and offline the same way. Run two players (with <c>-gamebox-identity</c> to tell them apart, and
    /// <c>-gamebox-server</c> / <c>-gamebox-database</c> for a test server) to try a race end to end. Without the argument
    /// it does nothing.
    /// </summary>
    public class RunnerOnlineTour : MonoBehaviour
    {
        private const string Argument = "-runner-online";
        private const string LevelArgument = "-runner-level";
        private const string GiveUpArgument = "-runner-give-up";
        private const string RacesArgument = "-runner-races";

        private bool host;
        private string mode;
        private string folder;
        private RunnerGameManager manager;
        private ServerClient server;
        private RunnerAutopilot pilot;
        private StreamWriter log;
        private bool left;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 2; i++)
            {
                if (args[i] == Argument)
                {
                    var tourObject = new GameObject("Runner Online Tour");
                    DontDestroyOnLoad(tourObject);
                    var tour = tourObject.AddComponent<RunnerOnlineTour>();
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
            string who = mode;
            log = new StreamWriter(Path.Combine(folder, $"{who}.log")) { AutoFlush = true };
            Application.logMessageReceived += (message, stack, type) =>
            {
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Warning)
                {
                    Note($"{type}: {message}");
                }
            };
            while ((manager = FindAnyObjectByType<RunnerGameManager>()) == null || manager.State == null)
            {
                yield return null;
            }
            yield return new WaitForSeconds(1f);
            yield return Shot($"{who}_00_level_select");
            if (mode == "solo")
            {
                yield return Solo();
                yield break;
            }
            if (manager.online == null)
            {
                yield return Fail("the scene has no online controller");
                yield break;
            }
            server = manager.online.ServerClient;
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
                var choices = manager.online.LevelChoices;
                int wanted = int.TryParse(MobilePlatform.ArgumentValue(LevelArgument), out int index) ? index : choices[0].Index;
                int level = choices.Any(choice => choice.Index == wanted) ? wanted : choices[0].Index;
                string options = manager.online.ComposeOptions(level, string.Empty);
                Note($"opening a room for level {level} with options {options}");
                server.CreateRoom("Tour race", level, options, 2, error => Note("create room: " + (error ?? "ok")));
                yield return WaitFor(() => server.InRoom, 15f, "own room");
                yield return Shot($"{who}_01_room");
            }
            else
            {
                yield return WaitFor(() => server.Rooms.Any(room => room.Phase != RoomPhase.Playing), 120f, "a room");
                yield return Shot($"{who}_01_rooms");
                RoomInfo open = server.Rooms.First(room => room.Phase != RoomPhase.Playing);
                server.JoinRoom(open.Id, error => Note("join room: " + (error ?? "ok")));
                yield return WaitFor(() => server.InRoom, 15f, "the room");
            }

            int races = int.TryParse(MobilePlatform.ArgumentValue(RacesArgument), out int wantedRaces) ? Mathf.Max(1, wantedRaces) : 1;
            for (int number = 1; number <= races && !left; number++)
            {
                string tag = number == 1 ? who : $"{who}_race{number}";
                if (host)
                {
                    yield return WaitFor(() => server.Members.Count >= 2 && server.Members.All(member => member.Ready || server.IsLocal(member)), 120f, "a ready guest");
                    yield return new WaitForSeconds(0.5f);
                    yield return Shot($"{tag}_02_everybody_ready");
                    server.StartRoom(error => Note("start room: " + (error ?? "ok")));
                }
                else
                {
                    server.SetReady(true, error => Note("ready: " + (error ?? "ok")));
                    yield return new WaitForSeconds(0.7f);
                    yield return Shot($"{tag}_02_room");
                }
                yield return Race(tag);
            }
            if (left)
            {
                yield break;
            }
            // The guest leaves first, so the host still sees the room lose a member.
            yield return new WaitForSeconds(host ? 3f : 0.5f);
            server.LeaveRoom();
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
            if (mode != "crash" && pilot == null)
            {
                pilot = manager.Player.gameObject.AddComponent<RunnerAutopilot>();
            }
            float giveUp = float.TryParse(MobilePlatform.ArgumentValue(GiveUpArgument), out float seconds) ? seconds : -1f;
            float started = Time.unscaledTime;
            yield return new WaitForSeconds(1.2f);
            yield return Shot($"{tag}_03_countdown");
            float[] pauses = { 4.5f, 6f, 8f, 10f };
            for (int i = 0; i < pauses.Length && !Ended; i++)
            {
                yield return WaitOrEnd(pauses[i]);
                Progress();
                yield return Shot($"{tag}_{4 + i:00}_{(manager.IsWatching ? "watching" : "running")}");
                if (mode == "quitter" && i == 1)
                {
                    Note("leaving the race");
                    manager.online.LeaveMatch();
                    yield return new WaitForSeconds(2f);
                    yield return Shot($"{tag}_06_left");
                    Note($"in a race: {manager.InRace}; in a session: {manager.InSession}; in a room: {server.InRoom}; state {manager.State}");
                    Note("done");
                    left = true;
                    Quit();
                    yield break;
                }
                if (host && i == 1 && !manager.IsWatching && !Ended)
                {
                    // A race does not pause: what pauses a game alone opens the match menu, and the runner runs on.
                    float before = manager.Distance;
                    manager.ActiveController.TransitionState(BaseGameState.Paused);
                    yield return new WaitForSeconds(0.6f);
                    yield return Shot($"{tag}_05_match_menu");
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
                yield return Shot($"{tag}_08_watching");
                yield return WaitOrEnd(6f);
                if (manager.IsWatching)
                {
                    Progress();
                    yield return Shot($"{tag}_08_watching_later");
                }
            }
            yield return WaitFor(() => Ended, 600f, "the end of the race");
            yield return new WaitForSeconds(2.5f);
            yield return Shot($"{tag}_09_results");
            Report();

            manager.ReturnToLevelSelect();
            yield return new WaitForSeconds(1.5f);
            yield return Shot($"{tag}_10_back_in_the_room");
            Note($"in a race: {manager.InRace}; in a session: {manager.InSession}; in a room: {server.InRoom}");
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
            yield return Shot("solo_03_countdown");
            yield return WaitOrEnd(9f);
            Note($"at {manager.Distance:0} m with {manager.Coins} coins");
            yield return Shot("solo_04_running");
            manager.ActiveController.TransitionState(BaseGameState.Paused);
            yield return new WaitForSecondsRealtime(1f);
            float pausedAt = manager.Distance;
            yield return Shot("solo_05_paused");
            yield return new WaitForSecondsRealtime(1f);
            Note($"paused: state {manager.State}, time scale {Time.timeScale}, moved {manager.Distance - pausedAt:0.00} m in a second");
            manager.ActiveController.TransitionState(BaseGameState.Running);
            yield return WaitFor(() => Ended, 240f, "the end of the run");
            yield return new WaitForSeconds(2.5f);
            yield return Shot("solo_09_results");
            Note($"result: {manager.State}; {manager.Distance:0} m, {manager.Coins} coins, score {manager.PlayerScore:0}");
            manager.ReturnToLevelSelect();
            yield return new WaitForSeconds(1.5f);
            yield return Shot("solo_10_level_select");
            Note("done");
            Quit();
        }

        private bool Ended => manager.State.Is(BaseGameState.Victory) || manager.State.Is(BaseGameState.GameOver);

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
            yield return Shot($"{mode}_failed");
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
