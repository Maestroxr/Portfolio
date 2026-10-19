using System;
using System.Collections;
using System.IO;
using Gamebox;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// Development players only: the check of the career and its windows. Started with <c>-deathrace-careercheck
    /// &lt;folder&gt;</c>, it walks through every screen of the game but the shared menu and saves a numbered screenshot of
    /// each into the folder: the new career window, a new career's garage on every page, a purchase in the garage and
    /// in the market (the sabotage picker too), a career race entered through an offer and driven by a computer driver
    /// in the person's seat (the countdown, the HUD mid race), its results, the garage of the next day, the champion's
    /// window and the quick race window. Problems (a window that did not open, errors and exceptions) are FAIL lines in
    /// <c>career.log</c>. It leaves no save behind and quits.
    /// <c>-deathrace-careercheck-speed &lt;x&gt;</c> runs the race faster (default 2.5), <c>-deathrace-careercheck-norace</c> skips it.
    /// </summary>
    public sealed class CareerCheck : MonoBehaviour
    {
        public const string Argument = "-deathrace-careercheck";
        public const string SpeedArgument = "-deathrace-careercheck-speed";
        public const string NoRaceArgument = "-deathrace-careercheck-norace";

        private const float RaceTimeout = 420f;

        private string folder;
        private StreamWriter log;
        private DeathRaceManager manager;
        private int shots;
        private int failures;
        private int errors;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Launch()
        {
            if (Application.isEditor || !Debug.isDebugBuild)
            {
                return;
            }
            string target = MobilePlatform.ArgumentValue(Argument);
            if (string.IsNullOrEmpty(target) || FindAnyObjectByType<CareerCheck>() != null)
            {
                return;
            }
            var holder = new GameObject("CareerCheck");
            DontDestroyOnLoad(holder);
            holder.AddComponent<CareerCheck>().folder = target;
        }

        private IEnumerator Start()
        {
            Directory.CreateDirectory(folder);
            log = new StreamWriter(Path.Combine(folder, "career.log"), false) { AutoFlush = true };
            Application.logMessageReceived += OnLog;
            Application.runInBackground = true;
            float waited = 0f;
            while (manager == null && waited < 30f)
            {
                manager = FindAnyObjectByType<DeathRaceManager>();
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            if (manager == null)
            {
                Fail("no Death Race manager");
                Quit();
                yield break;
            }
            Note($"careercheck: screen {Screen.width}x{Screen.height}, language {GameLanguages.Code}, look {(manager.Look != null ? manager.Look.name : "none")}");
            yield return new WaitForSecondsRealtime(3f);
            yield return Shot("menu");

            yield return NewCareer();
            yield return Garage();
            if (Array.IndexOf(Environment.GetCommandLineArgs(), NoRaceArgument) < 0)
            {
                yield return CareerRace();
            }
            yield return Champion();
            yield return QuickRace();

            Note($"careercheck done: {shots} screenshots, {failures} FAIL, {errors} errors");
            manager.ForgetAutosave();
            Quit();
        }

        private IEnumerator NewCareer()
        {
            manager.OpenCareer();
            yield return Settle();
            Expect(NewCareerWindow.For(manager).IsOpen, "the new career window opens when there is no career");
            yield return Shot("new-career");
            NewCareerWindow.For(manager).Hide();
            manager.StartCareer("Ace Rivera", 3, 20261018);
            yield return Settle();
            Expect(manager.HasCareer, "a career was started");
            Expect(GarageWindow.For(manager).IsOpen, "the garage opens on a new career");
        }

        private IEnumerator Garage()
        {
            GarageWindow garage = GarageWindow.For(manager);
            yield return Shot("garage-races");
            foreach (string page in new[] { GarageWindow.GaragePage, GarageWindow.MarketPage, GarageWindow.DealerPage, GarageWindow.StandingsPage })
            {
                garage.TurnTo(page);
                yield return Settle();
                yield return Shot("garage-" + page);
            }

            // Spend a little: an engine level and a mine, then aim a sabotage.
            CareerState state = manager.CurrentCareer;
            int money = state.wallet.Money;
            state.Current.loadout.health = 0.62f;
            garage.TurnTo(GarageWindow.GaragePage);
            garage.Press(CareerShop.EngineId);
            yield return Settle();
            Expect(state.Current.loadout.engine == 1 && state.wallet.Money < money, "an engine upgrade is bought in the garage");
            yield return Shot("garage-bought-engine");
            garage.TurnTo(GarageWindow.MarketPage);
            garage.Press(CareerShop.MineId);
            yield return Settle();
            Expect(state.consumables.Get(CareerShop.MineId) == CareerRules.StartMines + 1, "a mine is bought in the market");
            garage.Press(CareerShop.SabotageId);
            yield return Settle();
            Expect(garage.IsPicking, "sabotage opens the picker of the drivers racing today");
            yield return Shot("market-sabotage-picker");
            // The race goes better in a whole car (the dented one was for the repair buttons of the garage page).
            state.wallet.Add(500);
            new CareerShop(state).RepairAll();
            garage.TurnTo(GarageWindow.RacePage);
            yield return Settle();
        }

        private IEnumerator CareerRace()
        {
            CareerState state = manager.CurrentCareer;
            RaceDirector director = manager.Director;
            if (director == null)
            {
                Fail("no race director");
                yield break;
            }
            int easy = state.offers.FindIndex(offer => offer.difficulty == Difficulty.Easy);
            director.Started += Pilot;
            float speed = Mathf.Clamp(Number(SpeedArgument, 2.5f), 0.5f, 3f);
            bool entered = manager.EnterOffer(easy);
            Expect(entered, "an offer is entered");
            director.Started -= Pilot;
            if (!entered)
            {
                yield break;
            }
            Expect(!GarageWindow.For(manager).IsOpen, "the garage closes for the race");
            // The HUD of the very first frames: it must sit in the corners already, not small in the middle.
            yield return null;
            yield return Shot("race-grid");
            yield return new WaitForSecondsRealtime(2f);
            yield return Shot("race-countdown");
            yield return new WaitForSecondsRealtime(2.5f);
            Time.timeScale = speed;
            float start = Time.unscaledTime;
            // The HUD a little way in and late in the race (laps, toasts, a wreck or two by then).
            float[] hudShots = { 25f, 75f };
            int hudShot = 0;
            ResultsWindow results = ResultsWindow.For(manager);
            while (!results.IsOpen && Time.unscaledTime - start < RaceTimeout)
            {
                Race race = manager.CurrentRace;
                if (hudShot < hudShots.Length && race != null && race.Time > hudShots[hudShot])
                {
                    hudShot++;
                    Time.timeScale = 1f;
                    yield return Shot($"race-hud-{hudShot}");
                    Time.timeScale = speed;
                }
                yield return null;
            }
            Time.timeScale = 1f;
            Expect(results.IsOpen, "the results show after the career race");
            if (!results.IsOpen)
            {
                manager.LeaveRace();
                yield break;
            }
            yield return new WaitForSecondsRealtime(2.5f);
            yield return Shot("results");
            Expect(manager.CurrentCareer.day == 2, "the career moved on to the next day");
            Expect(manager.LatestSave != null && manager.LatestSave.Summary != null && manager.LatestSave.Summary.Contains("day 2"),
                "the career saved itself after the race (the save is of day 2)");
            manager.Back();
            yield return Settle();
            Expect(GarageWindow.For(manager).IsOpen, "Continue goes back to the garage");
            yield return Shot("garage-next-day");
        }

        /// <summary>The career race was built: the person's seat is handed to a computer driver.</summary>
        private void Pilot(Race race)
        {
            foreach (Car car in race.Cars)
            {
                if (car.IsHuman)
                {
                    car.Driver = new AiDriver(0.9f, 4242);
                }
            }
        }

        private IEnumerator Champion()
        {
            GarageWindow.For(manager).Hide();
            CareerState state = manager.CurrentCareer;
            state.champion = true;
            state.championDay = state.day;
            ChampionWindow.For(manager).Show();
            yield return Settle();
            Expect(ChampionWindow.For(manager).IsOpen, "the champion's window opens");
            yield return Shot("champion");
            ChampionWindow.For(manager).Hide();
            state.champion = false;
        }

        private IEnumerator QuickRace()
        {
            manager.OpenQuickRace();
            yield return Settle();
            Expect(QuickRaceWindow.For(manager).IsOpen, "the quick race window opens");
            yield return Shot("quick-race");
            QuickRaceWindow.For(manager).Hide();
            yield return Settle();
        }

        // ------------------------------------------------------------------------------------------------

        private static IEnumerator Settle()
        {
            yield return null;
            yield return null;
            yield return new WaitForSecondsRealtime(0.4f);
        }

        private IEnumerator Shot(string label)
        {
            shots++;
            string file = Path.Combine(folder, $"{shots:00}_{label}.png");
            ScreenCapture.CaptureScreenshot(file);
            yield return null;
            yield return new WaitForEndOfFrame();
            yield return new WaitForSecondsRealtime(0.3f);
            Note($"shot {file}");
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
            return !string.IsNullOrEmpty(value) && float.TryParse(value, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float number) ? number : fallback;
        }

        private void OnLog(string message, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                errors++;
                Note($"ERROR {type}: {message}\n{stackTrace}");
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
