using System;
using System.Collections;
using System.Globalization;
using System.IO;
using Gamebox;
using Gamebox.UI;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// Development players only: the tour of the whole game at this device. Started with <c>-deathrace-tour &lt;folder&gt;</c>
    /// it plays through everything a player can do offline and saves a numbered screenshot of every step into the folder:
    /// the menu with the computer cars racing behind it, the settings, a new career (the new career window, the garage on
    /// every page, an upgrade bought, an offer entered and raced, the results, the garage of the next day), the quick race
    /// window and a quick race (with the pause menu in the middle of it), and split screen with two and with four people
    /// (the setup, the controls page, the race in its views, the results). The people's cars are driven by the computer
    /// (<see cref="AiDriver"/>) so the races play themselves, a little faster than real time between the pictures.
    /// Anything that does not happen as it should is a FAIL line in <c>tour.log</c>, so are errors and exceptions; the
    /// tour leaves no career behind and quits. The look and the language are the command line's
    /// (<c>-gamebox-theme Nightfall</c>, <c>-gamebox-language he</c>).
    /// <c>-deathrace-tour-speed &lt;x&gt;</c> sets the speed of the races between pictures (default 2.5),
    /// <c>-deathrace-tour-skip career,quick,split</c> leaves parts out.
    /// </summary>
    public sealed class DeathRaceTour : MonoBehaviour
    {
        public const string Argument = "-deathrace-tour";
        public const string SpeedArgument = "-deathrace-tour-speed";
        public const string SkipArgument = "-deathrace-tour-skip";

        private const float RaceTimeout = 360f;

        private string folder;
        private StreamWriter log;
        private DeathRaceManager manager;
        private int shots;
        private int failures;
        private int errors;
        private float speed = 2.5f;
        private string skip = "";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Launch()
        {
            if (Application.isEditor || !Debug.isDebugBuild)
            {
                return;
            }
            string target = MobilePlatform.ArgumentValue(Argument);
            if (string.IsNullOrEmpty(target) || FindAnyObjectByType<DeathRaceTour>() != null)
            {
                return;
            }
            var holder = new GameObject("DeathRaceTour");
            DontDestroyOnLoad(holder);
            holder.AddComponent<DeathRaceTour>().folder = target;
        }

        private IEnumerator Start()
        {
            Directory.CreateDirectory(folder);
            log = new StreamWriter(Path.Combine(folder, "tour.log"), false) { AutoFlush = true };
            Application.logMessageReceived += OnLog;
            Application.runInBackground = true;
            speed = Mathf.Clamp(Number(SpeedArgument, 2.5f), 0.5f, 3f);
            skip = MobilePlatform.ArgumentValue(SkipArgument) ?? "";
            float waited = 0f;
            while (manager == null && waited < 30f)
            {
                manager = FindAnyObjectByType<DeathRaceManager>();
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            if (manager == null || manager.Director == null)
            {
                Fail("no Death Race manager or race director");
                Quit();
                yield break;
            }
            Note($"tour: screen {Screen.width}x{Screen.height}, language {GameLanguages.Code}, look {(manager.Look != null ? manager.Look.name : "none")}, speed {speed}");
            manager.Director.Started += Pilot;

            yield return Menu();
            if (!Skipped("career"))
            {
                yield return Career();
            }
            if (!Skipped("quick"))
            {
                yield return QuickRace();
            }
            if (!Skipped("split"))
            {
                yield return SplitScreen(2, (int)CarModel.Viper);
                yield return SplitScreen(4, CarSpecs.Count);
            }

            manager.Director.Started -= Pilot;
            Time.timeScale = 1f;
            manager.ForgetAutosave();
            Note($"tour done: {shots} screenshots, {failures} FAIL, {errors} errors");
            Quit();
        }

        private bool Skipped(string part)
        {
            return skip.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // ------------------------------------------------------------------------------------------------
        // The parts of the tour
        // ------------------------------------------------------------------------------------------------

        /// <summary>The menu with the cars racing behind it, and the settings.</summary>
        private IEnumerator Menu()
        {
            yield return new WaitForSecondsRealtime(4f);
            Race attract = manager.CurrentRace;
            Expect(attract != null && attract.Setup.Mode == RaceMode.Attract, "computer cars race behind the menu");
            yield return Shot("menu");
            yield return new WaitForSecondsRealtime(5f);
            yield return Shot("menu-attract");
            manager.Interface.ShowSettings();
            yield return Settle();
            Expect(manager.Interface.IsSettingsShown, "the settings open");
            yield return Shot("settings");
            manager.Interface.HideSettings();
            yield return Settle();
        }

        /// <summary>A new career: the garage, an upgrade, a race of the day, its results and the next day.</summary>
        private IEnumerator Career()
        {
            manager.OpenCareer();
            yield return Settle();
            Expect(NewCareerWindow.For(manager).IsOpen, "the new career window opens");
            yield return Shot("career-new");
            NewCareerWindow.For(manager).Hide();
            manager.StartCareer("Max Kowalski", 5, 20261019);
            yield return Settle();
            Expect(manager.HasCareer, "a career was started");
            GarageWindow garage = GarageWindow.For(manager);
            Expect(garage.IsOpen, "the garage opens on a new career");
            foreach (string page in new[] { GarageWindow.RacePage, GarageWindow.GaragePage, GarageWindow.MarketPage, GarageWindow.DealerPage, GarageWindow.StandingsPage })
            {
                garage.TurnTo(page);
                yield return Settle();
                yield return Shot("garage-" + page);
            }

            CareerState state = manager.CurrentCareer;
            int money = state.wallet.Money;
            garage.TurnTo(GarageWindow.GaragePage);
            garage.Press(CareerShop.EngineId);
            yield return Settle();
            Expect(state.Current.loadout.engine == 1 && state.wallet.Money < money, "an engine upgrade is bought");
            yield return Shot("garage-upgrade-bought");
            garage.TurnTo(GarageWindow.RacePage);
            yield return Settle();

            int offer = state.offers.FindIndex(o => o.difficulty == Difficulty.Easy);
            bool entered = manager.EnterOffer(Mathf.Max(0, offer));
            Expect(entered, "an offer of the day is entered");
            if (!entered)
            {
                yield break;
            }
            Expect(!garage.IsOpen, "the garage closes for the race");
            Expect(manager.CurrentRace != null && manager.CurrentRace.Setup.Mode == RaceMode.Career, "the career race runs");
            ResultsWindow results = ResultsWindow.For(manager);
            yield return RaceWithShots("career", results, new[] { 20f, 60f });
            if (!results.IsOpen)
            {
                manager.LeaveRace();
                yield break;
            }
            Expect(state.day == 2, "the career moved on to the next day");
            manager.Back();
            yield return Settle();
            Expect(GarageWindow.For(manager).IsOpen, "Continue goes back to the garage");
            yield return Shot("garage-next-day");
            GarageWindow.For(manager).Hide();
            yield return Settle();
        }

        /// <summary>The quick race window, then a quick race with the pause menu in the middle of it.</summary>
        private IEnumerator QuickRace()
        {
            manager.OpenQuickRace();
            yield return Settle();
            Expect(QuickRaceWindow.For(manager).IsOpen, "the quick race window opens");
            yield return Shot("quick-race-window");
            QuickRaceWindow.For(manager).Hide();
            yield return Settle();
            manager.StartQuickRace(1, CarModel.Reaper, Difficulty.Hard, 2);
            Race race = manager.CurrentRace;
            Expect(race != null && race.Setup.Mode == RaceMode.Quick, "the quick race runs");
            yield return Countdown("quick");
            yield return new WaitForSecondsRealtime(RaceDirector.CountdownSeconds + 4f);
            manager.Back();
            yield return Settle();
            Expect(manager.State != null && manager.State.BaseState == BaseGameState.Paused, "Escape pauses the race");
            Expect(Time.timeScale == 0f, "the race stands still while paused");
            yield return Shot("quick-pause-menu");
            manager.Back();
            yield return Settle();
            Expect(manager.State != null && manager.State.BaseState == BaseGameState.Running, "Escape again resumes it");
            ResultsWindow results = ResultsWindow.For(manager);
            yield return RaceWithShots("quick", results, new[] { 15f, 45f }, false);
            if (results.IsOpen)
            {
                manager.Back();
                yield return Settle();
            }
            else
            {
                manager.LeaveRace();
            }
            yield return new WaitForSecondsRealtime(1.5f);
            Expect(manager.State != null && manager.State.BaseState == BaseGameState.Initialization, "the results' way out is the menu");
            Expect(manager.CurrentRace != null && manager.CurrentRace.Setup.Mode == RaceMode.Attract, "the cars race behind the menu again");
        }

        /// <summary>Split screen with <paramref name="people"/> people: the setup, the controls, the race in its views, the results.</summary>
        private IEnumerator SplitScreen(int people, int car)
        {
            manager.OpenLocalPlay();
            yield return Settle();
            LocalPlayUI setup = LocalPlayUI.For(manager);
            Expect(setup.IsOpen, "the split screen setup opens");
            LocalMatch match = setup.Match;
            if (match == null)
            {
                Fail("the split screen setup has no match");
                yield break;
            }
            match.Count = people;
            for (int seat = 0; seat < people; seat++)
            {
                match[seat].Kind = SeatKind.Human;
            }
            match.SetOption(DeathRaceLocalRules.TrackOption, people == 2 ? 0 : Mathf.Min(2, ((DeathRaceLocalRules)match.Rules).Tracks.Count - 1));
            match.SetOption(DeathRaceLocalRules.LapsOption, 1);
            match.SetOption(DeathRaceLocalRules.CarOption, car);
            match.SetOption(DeathRaceLocalRules.FillOption, 2);
            match.Rules.OptionsChanged(match);
            setup.ShowSetup();
            yield return Settle();
            yield return Shot($"split{people}-setup");
            if (people == 2)
            {
                setup.ShowControls(1);
                yield return Settle();
                yield return Shot("split-controls");
                setup.ShowSetup();
                yield return Settle();
            }
            bool started = setup.StartMatch();
            Expect(started, $"split screen starts with {people} people");
            if (!started)
            {
                setup.Hide();
                yield break;
            }
            Race race = manager.CurrentRace;
            Expect(manager.InLocalMatch, "a local match is on");
            Expect(race != null && race.Setup.Mode == RaceMode.Local, "the split screen race runs");
            Expect(race != null && race.Cars.Count == DeathRaceLocalRules.GridSize, "computer cars fill the grid to four");
            Expect(manager.Director.Huds.Count == people, $"a HUD for each of the {people} people");
            int views = 0;
            foreach (RaceCamera view in manager.Director.Cameras)
            {
                views += view != null && view.Target != null ? 1 : 0;
            }
            Expect(views == people, $"a view for each of the {people} people ({views})");
            ResultsWindow results = ResultsWindow.For(manager);
            yield return RaceWithShots($"split{people}", results, new[] { 12f, 40f });
            if (results.IsOpen)
            {
                manager.Back();
                yield return Settle();
            }
            else
            {
                manager.LeaveRace();
            }
            yield return new WaitForSecondsRealtime(1.5f);
            Expect(!manager.InLocalMatch, "the results' Menu ends the split screen match");
        }

        /// <summary>
        /// Watches the race on until its results show: the countdown, pictures at the race times of <paramref name="at"/>
        /// (at real speed), the rest faster; then the results.
        /// </summary>
        private IEnumerator RaceWithShots(string label, ResultsWindow results, float[] at, bool countdown = true)
        {
            if (countdown)
            {
                yield return Countdown(label);
            }
            float start = Time.unscaledTime;
            int next = 0;
            while (!results.IsOpen && Time.unscaledTime - start < RaceTimeout)
            {
                Race race = manager.CurrentRace;
                if (race == null)
                {
                    break;
                }
                bool paused = manager.State != null && manager.State.BaseState == BaseGameState.Paused;
                if (!paused && race.Phase != RacePhase.Over)
                {
                    Time.timeScale = race.IsRunning ? speed : 1f;
                }
                if (next < at.Length && race.Time > at[next])
                {
                    next++;
                    Time.timeScale = 1f;
                    yield return Shot($"{label}-race-{next}");
                    Note($"  {label} t {race.Time:0.0}: {Standings(race)}");
                }
                yield return null;
            }
            Time.timeScale = 1f;
            Expect(results.IsOpen, $"the results show after the {label} race");
            if (results.IsOpen)
            {
                yield return new WaitForSecondsRealtime(2.5f);
                yield return Shot(label + "-results");
            }
        }

        /// <summary>The grid and the count, a moment after the race was built.</summary>
        private IEnumerator Countdown(string label)
        {
            yield return new WaitForSecondsRealtime(RaceDirector.GridSeconds + 1f);
            yield return Shot(label + "-countdown");
        }

        /// <summary>A race was built: the people's cars are handed to the computer, so the race drives itself.</summary>
        private void Pilot(Race race)
        {
            if (race.Setup.Mode == RaceMode.Attract)
            {
                return;
            }
            foreach (Car car in race.Cars)
            {
                if (car.IsHuman)
                {
                    car.Driver = new AiDriver(0.85f, 4242 + car.Entrant.Seat);
                }
                Car watched = car;
                car.Wrecked += (wreck, cause) => Note($"  wreck t {race.Time:0.0}: {wreck.Entrant.Name} by {(cause.Attacker != null ? cause.Attacker.Entrant.Name : "-")} ({cause.Source})");
            }
            Note($"race: {race.Setup.Mode} on {race.Track.Layout.Name}, {race.Laps} laps, {race.Cars.Count} cars ({race.Setup.Humans} people)");
        }

        private static string Standings(Race race)
        {
            var text = new System.Text.StringBuilder();
            foreach (Car car in race.Standings())
            {
                text.Append(car.Racer != null ? car.Racer.Place : 0).Append('.').Append(car.Entrant.Name)
                    .Append(car.IsWrecked ? " (wrecked)" : car.HasFinished ? " (done)" : "").Append(' ');
            }
            return text.ToString();
        }

        // ------------------------------------------------------------------------------------------------

        private static IEnumerator Settle()
        {
            yield return null;
            yield return null;
            yield return new WaitForSecondsRealtime(0.5f);
        }

        private IEnumerator Shot(string label)
        {
            shots++;
            string file = Path.Combine(folder, $"{shots:00}_{label}.png");
            ScreenCapture.CaptureScreenshot(file);
            yield return null;
            yield return new WaitForEndOfFrame();
            yield return new WaitForSecondsRealtime(0.3f);
            Note($"shot {Path.GetFileName(file)}");
        }

        private void Expect(bool condition, string what)
        {
            if (condition)
            {
                Note("ok " + what);
            }
            else
            {
                Fail(what);
            }
        }

        private void Fail(string what)
        {
            failures++;
            Note("FAIL " + what);
        }

        private void Note(string line)
        {
            log?.WriteLine($"[{Time.realtimeSinceStartup:0.0}] {line}");
        }

        private static float Number(string name, float fallback)
        {
            string value = MobilePlatform.ArgumentValue(name);
            return !string.IsNullOrEmpty(value) && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float number) ? number : fallback;
        }

        private void OnLog(string message, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                errors++;
                Note($"FAIL {type}: {message}\n{stackTrace}");
            }
        }

        private void Quit()
        {
            Application.logMessageReceived -= OnLog;
            log?.Dispose();
            log = null;
            Application.Quit();
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= OnLog;
            log?.Dispose();
            log = null;
        }
    }
}
