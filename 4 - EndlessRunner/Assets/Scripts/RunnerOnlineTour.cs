using System.Collections;
using System.Linq;
using Gamebox;
using Gamebox.Online;
using Gamebox.UI;
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
    /// <c>-runner-online solo &lt;folder&gt;</c> plays the first level (or the one after <c>-runner-level</c>) alone and
    /// offline the same way, after a picture of every world of the campaign behind the level select.
    /// <c>-runner-online local &lt;folder&gt;</c> plays races at one device (<see cref="LocalRace"/>): the setup,
    /// the controls page, then a race for each count of <c>-runner-local-players</c> (default <c>2,4,3</c>) with an
    /// autopilot on every runner; the first race checks that each seat's keys move its own runner, that Escape pauses and
    /// that Retry races again, the race of four lets one runner run out of hearts and watch the others, and a run alone
    /// afterwards checks that the game for one is back. Run the players with <c>-gamebox-identity</c> to tell them apart,
    /// and <c>-gamebox-server</c> / <c>-gamebox-database</c> for a test server.
    /// </summary>
    public class RunnerOnlineTour : OnlineTour
    {
        private const string Argument = "-runner-online";
        private const string LevelArgument = "-runner-level";
        private const string GiveUpArgument = "-runner-give-up";
        private const string RacesArgument = "-runner-races";
        private const string LocalPlayersArgument = "-runner-local-players";

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
            if (Role == "local")
            {
                yield return Local();
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

        /// <summary>
        /// The flag button's click on a screen: the next language, a shot of the screen redrawn in it, and back (the words
        /// written by code have to follow as well as the fixed ones).
        /// </summary>
        private IEnumerator SwitchLanguage(string shot)
        {
            string before = GameLanguages.Code;
            LanguageButton flag = FindObjectsByType<LanguageButton>(FindObjectsInactive.Include).FirstOrDefault();
            UnityEngine.UI.Button button = flag != null ? flag.GetComponent<UnityEngine.UI.Button>() : null;
            if (button == null)
            {
                Note("no language button to click");
                yield break;
            }
            button.onClick.Invoke();
            yield return new WaitForSecondsRealtime(1f);
            yield return Shot($"{shot}_{GameLanguages.Code}");
            for (int clicks = 0; clicks < 4 && GameLanguages.Code != before; clicks++)
            {
                button.onClick.Invoke();
                yield return new WaitForSecondsRealtime(1f);
            }
            Note($"switched the language from {before} and back to {GameLanguages.Code}");
        }

        /// <summary>A level alone and offline: the game for one has to play as it always did.</summary>
        private IEnumerator Solo()
        {
            // Another look picked on the level select redraws it at once, without loading the scene again.
            GameTheme look = GameThemes.Active(GameType.EndlessRunner);
            if (look != null && GameThemes.Available(GameType.EndlessRunner).Count > 1)
            {
                GameTheme other = GameThemes.SelectNext(GameType.EndlessRunner, 1, false);
                yield return new WaitForSeconds(1.5f);
                yield return Shot("00_level_select_switched");
                GameThemes.Select(GameType.EndlessRunner, look, false);
                yield return new WaitForSeconds(1.5f);
                yield return Shot("00_level_select_back");
                Note($"switched the look to {other.DisplayName} on the level select and back to {look.DisplayName}");
            }
            // Every world of the campaign behind the level select, in the theme this player shows.
            RunnerTheme shown = null;
            for (int i = 0; i < manager.LevelCount; i++)
            {
                if (!(manager.Campaign[i] is RunnerLevel candidate) || candidate.IsEndless || candidate.Theme == shown)
                {
                    continue;
                }
                shown = candidate.Theme;
                manager.SelectLevel(i);
                yield return new WaitForSeconds(1.5f);
                yield return Shot($"01_world_{i + 1}");
            }
            int level = Mathf.Clamp(NumberArgument(LevelArgument, 0), 0, Mathf.Max(0, manager.LevelCount - 1));
            manager.SelectLevel(level);
            yield return new WaitForSeconds(0.5f);
            manager.PlayLevel(level);
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
            yield return SwitchLanguage("09_results");
            manager.ReturnToLevelSelect();
            yield return new WaitForSeconds(1.5f);
            yield return Shot("10_level_select");
            yield return SwitchLanguage("10_level_select");
            Note("done");
            Quit();
        }

        /// <summary>Races at this device: the setup, the controls page, a race per count of players, and the game alone again.</summary>
        private IEnumerator Local()
        {
            int level = Mathf.Clamp(NumberArgument(LevelArgument, 0), 0, Mathf.Max(0, manager.LevelCount - 1));
            manager.SelectLevel(level);
            yield return new WaitForSeconds(0.5f);
            string players = MobilePlatform.ArgumentValue(LocalPlayersArgument);
            int[] counts = (string.IsNullOrEmpty(players) ? "2,4,3" : players).Split(',')
                .Select(count => int.TryParse(count, out int value) ? value : 0).Where(count => count >= 2 && count <= 4).ToArray();
            for (int i = 0; i < counts.Length; i++)
            {
                yield return RaceAtThisDevice(counts[i], i == 0);
                if (left)
                {
                    yield break;
                }
            }

            manager.PlayLevel(level);
            yield return WaitFor(() => manager.IsGameRunning, 10f, "the run alone");
            yield return new WaitForSeconds(5f);
            Note($"alone again: in a local race {manager.InLocalRace}, {manager.Players.Count} players, main camera {Camera.main.rect}, "
                + $"at {manager.Distance:0} m with {manager.Coins} coins");
            yield return Shot("40_alone_again");
            manager.ReturnToLevelSelect();
            yield return new WaitForSecondsRealtime(1f);
            Note("done");
            Quit();
        }

        /// <summary>A race of <paramref name="count"/> runners at this device, from the setup to the level select.</summary>
        private IEnumerator RaceAtThisDevice(int count, bool first)
        {
            string tag = $"{count}p";
            // The setup opens on what was set up last: a race of this many.
            PlayerPrefs.SetString(LocalMatch.Key(GameType.EndlessRunner), $"count={count}\n");
            manager.OpenLocalPlay();
            yield return new WaitForSecondsRealtime(1f);
            LocalPlayUI setup = LocalPlayUI.For(manager);
            if (!setup.IsOpen)
            {
                left = true;
                yield return Fail("the local play setup did not open");
                yield break;
            }
            yield return Shot($"{tag}_10_setup");
            if (first)
            {
                setup.ShowControls(0);
                yield return new WaitForSecondsRealtime(0.8f);
                yield return Shot($"{tag}_11_controls_player1");
                setup.ShowControls(1);
                yield return new WaitForSecondsRealtime(0.5f);
                yield return Shot($"{tag}_12_controls_player2");
                setup.ShowSetup();
                yield return new WaitForSecondsRealtime(0.3f);
            }
            if (!setup.StartMatch())
            {
                left = true;
                yield return Fail("the local race did not start");
                yield break;
            }
            yield return WaitFor(() => manager.InLocalRace && manager.IsGameRunning, 10f, "the local race");
            LocalRace race = manager.LocalRace;
            Note($"local race of {race.Runs.Count} on level {manager.LevelIndex}: "
                + string.Join(", ", race.Runs.Select(run => $"{run.Seat.Name} lane {run.Runner.Lane} viewport {run.Camera.View.rect}")));
            yield return new WaitForSeconds(1.2f);
            yield return Shot($"{tag}_13_countdown");
            yield return WaitFor(() => race.Runs.All(run => run.Runner.IsRunning), 10f, "the start");
            if (first)
            {
                yield return PressSeatKeys(race);
            }
            foreach (RunnerRun run in race.Runs)
            {
                if (run.Runner.GetComponent<RunnerAutopilot>() == null)
                {
                    run.Runner.gameObject.AddComponent<RunnerAutopilot>();
                }
            }
            yield return WaitOrEnd(6f);
            LocalProgress(race);
            yield return Shot($"{tag}_14_running");
            if (first)
            {
                // Escape pauses a race at one device like a run alone.
                manager.Back();
                yield return new WaitForSecondsRealtime(0.8f);
                float pausedAt = race.Runs[0].Distance;
                yield return Shot($"{tag}_15_paused");
                yield return new WaitForSecondsRealtime(0.8f);
                Note($"paused: state {manager.State}, time scale {Time.timeScale}, moved {race.Runs[0].Distance - pausedAt:0.00} m");
                manager.Back();
                yield return new WaitForSecondsRealtime(0.3f);
            }
            if (count == 4)
            {
                // The last runner runs on its own: it is soon out of hearts and watches the others.
                Destroy(race.Runs[3].Runner.GetComponent<RunnerAutopilot>());
                yield return WaitFor(() => race.Runs[3].Done || Ended, 60f, "the last runner out of hearts");
                yield return new WaitForSeconds(1.5f);
                LocalProgress(race);
                yield return Shot($"{tag}_16_out_and_watching");
            }
            yield return WaitOrEnd(10f);
            if (!Ended)
            {
                LocalProgress(race);
                yield return Shot($"{tag}_17_later");
            }
            yield return WaitFor(() => Ended || race.Runs.Any(run => run.Done && run.Finished), 240f, "the first runner over the line");
            if (!Ended)
            {
                yield return new WaitForSeconds(1.5f);
                LocalProgress(race);
                yield return Shot($"{tag}_18_first_done");
            }
            yield return WaitFor(() => Ended, 300f, "the end of the local race");
            yield return new WaitForSeconds(2f);
            yield return Shot($"{tag}_19_results");
            Note($"result: {manager.State}; " + string.Join(", ", race.Ranking.Select(racer =>
                $"{Standings.Ordinal(racer.Place)} {racer.Name} {racer.Score} ({racer.Distance:0} m, {racer.Coins} coins)")));
            if (first)
            {
                yield return SwitchLanguage($"{tag}_19_results");
                manager.RetryLevel();
                yield return WaitFor(() => manager.IsGameRunning, 10f, "the race again");
                yield return new WaitForSeconds(2f);
                Note($"retry: in a local race {manager.InLocalRace} of {manager.LocalRace?.Runs.Count}; state {manager.State}");
                yield return Shot($"{tag}_20_retry");
                yield return WaitFor(() => Ended, 300f, "the end of the race again");
                yield return new WaitForSeconds(1f);
            }
            manager.ReturnToLevelSelect();
            yield return new WaitForSecondsRealtime(1.5f);
            Note($"back in the level select: in a local race {manager.InLocalRace}, {manager.Players.Count} players, main camera {Camera.main.rect}");
            yield return Shot($"{tag}_21_level_select");
        }

        /// <summary>The keys of the first two seats as people would press them: each runner changes lanes on its own keys.</summary>
        private IEnumerator PressSeatKeys(LocalRace race)
        {
            var keys = new ScriptedInput();
            ControlInput.Source = keys;
            int first = race.Runs[0].Runner.Lane;
            int second = race.Runs[1].Runner.Lane;
            // Player 1 goes right (D), player 2 goes right (the right arrow).
            keys.Press(KeyCode.D);
            keys.Press(KeyCode.RightArrow);
            // The runners read the keys in the next frame; after it they are only held.
            yield return null;
            keys.EndFrame();
            keys.ReleaseAll();
            yield return null;
            Note($"keys: player 1 lane {first} -> {race.Runs[0].Runner.Lane}, player 2 lane {second} -> {race.Runs[1].Runner.Lane}"
                + (race.Runs.Count > 2 ? $", player 3 lane {race.Runs[2].Runner.Lane}" : string.Empty));
            ControlInput.Source = null;
        }

        private void LocalProgress(LocalRace race)
        {
            Note(string.Join(", ", race.Runs.Select(run =>
                $"{run.Seat.Name} {run.Phase} {run.Distance:0} m {run.Coins} c {run.Hearts} h p{run.Racer.Place}")));
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
                .Select(racer => $"{Standings.Ordinal(racer.Place)} {racer.Name} {racer.Score} ({racer.Distance:0} m, {racer.Coins} coins)")));
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
