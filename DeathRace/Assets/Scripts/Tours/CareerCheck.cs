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
    /// each into the folder: the new career window (with only the first league open, then with every league open and
    /// the last one picked), a new career's garage on every page, a purchase in the garage, the workshop (a fitting, a
    /// gun bought and the machine gun mounted again) and the market (a mine, a special weapon armed first, the sabotage
    /// picker), a career race entered through an offer and driven by a computer driver in the person's seat (the
    /// countdown, the HUD mid race), its results, the garage of the next day, the champion's window and moving up to
    /// the Iron League (its garage and a race of it), and the quick race window. Problems (a window that did not open,
    /// errors and exceptions) are FAIL lines in <c>career.log</c>. It leaves no save behind, puts the device's titles
    /// and opened tracks back as they were (<see cref="TourLeagues"/>) and quits.
    /// <c>-deathrace-careercheck-speed &lt;x&gt;</c> runs the races faster (default 2.5), <c>-deathrace-careercheck-norace</c> skips them.
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
        private TourLeagues leagues;
        private string titleSave;
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
            leagues = new TourLeagues(manager.Progress);
            leagues.Close();
            yield return new WaitForSecondsRealtime(3f);
            yield return Shot("menu");

            bool race = Array.IndexOf(Environment.GetCommandLineArgs(), NoRaceArgument) < 0;
            yield return NewCareer();
            yield return Garage();
            yield return Workshop();
            yield return Market();
            if (race)
            {
                yield return CareerRace("race");
            }
            yield return Champion();
            if (race && manager.CurrentCareer != null && manager.CurrentCareer.League.Kind == LeagueKind.Iron)
            {
                yield return CareerRace("iron-race");
            }
            yield return QuickRace();

            Note($"careercheck done: {shots} screenshots, {failures} FAIL, {errors} errors");
            manager.ForgetAutosave();
            if (titleSave != null)
            {
                manager.SavedGames?.Delete(titleSave);
            }
            leagues.Restore();
            Quit();
        }

        private IEnumerator NewCareer()
        {
            manager.OpenCareer();
            yield return Settle();
            NewCareerWindow window = NewCareerWindow.For(manager);
            Expect(window.IsOpen, "the new career window opens when there is no career");
            Expect(window.League == LeagueKind.Wasteland && !window.Pick(LeagueKind.Iron), "only the first league is open on a new device");
            yield return Shot("new-career");
            window.Hide();

            // Every league open (as on a device where both titles were won): the picker starts on the last one.
            leagues.OpenUpTo(LeagueKind.Hellfire);
            manager.OpenCareer();
            yield return Settle();
            Expect(window.IsOpen && window.League == LeagueKind.Hellfire, "the picker starts on the highest league open");
            yield return Shot("new-career-leagues");
            Expect(window.Pick(LeagueKind.Iron), "an open league can be picked");
            yield return Settle();
            yield return Shot("new-career-iron");
            window.Hide();
            leagues.Close();

            manager.StartCareer("Ace Rivera", 3, 20261018);
            yield return Settle();
            Expect(manager.HasCareer && manager.CurrentCareer.League.Kind == LeagueKind.Wasteland, "a career of the first league was started");
            Expect(GarageWindow.For(manager).IsOpen, "the garage opens on a new career");
        }

        private IEnumerator Garage()
        {
            GarageWindow garage = GarageWindow.For(manager);
            yield return Shot("garage-races");
            foreach (string page in new[] { GarageWindow.GaragePage, GarageWindow.WorkshopPage, GarageWindow.MarketPage, GarageWindow.DealerPage, GarageWindow.StandingsPage })
            {
                garage.TurnTo(page);
                yield return Settle();
                yield return Shot("garage-" + page);
            }

            // Spend a little: an engine level and the nitro system.
            CareerState state = manager.CurrentCareer;
            state.wallet.Add(6000);
            int money = state.wallet.Money;
            state.Current.loadout.health = 0.62f;
            garage.TurnTo(GarageWindow.GaragePage);
            garage.Press(CareerShop.EngineId);
            garage.Press(CareerShop.BoostId);
            yield return Settle();
            Expect(state.Current.loadout.engine == 1 && state.Current.loadout.boost == 1 && state.wallet.Money < money, "an engine and a nitro system level are bought in the garage");
            yield return Shot("garage-bought-engine");
        }

        private IEnumerator Workshop()
        {
            GarageWindow garage = GarageWindow.For(manager);
            CareerState state = manager.CurrentCareer;
            garage.TurnTo(GarageWindow.WorkshopPage);
            yield return Settle();
            garage.Pick(CareerShop.FittingId(EquipmentKind.Magazine));
            garage.Press(CareerShop.FittingId(EquipmentKind.Magazine));
            yield return Settle();
            Expect(state.Current.loadout.magazine, "the extended magazine is fitted in the workshop");
            garage.Pick(CareerShop.GunId(GunKind.Minigun));
            garage.Press(CareerShop.GunId(GunKind.Minigun));
            yield return Settle();
            Expect(state.Current.loadout.gun == GunKind.Minigun, "the minigun is bought and mounted");
            yield return Shot("workshop-minigun");
            garage.Pick(CareerShop.GunId(GunKind.MachineGun));
            int money = state.wallet.Money;
            garage.Press(CareerShop.GunId(GunKind.MachineGun));
            yield return Settle();
            Expect(state.Current.loadout.gun == GunKind.MachineGun && state.wallet.Money == money, "the machine gun goes back on for nothing");
            garage.Pick(CareerShop.GunId(GunKind.Minigun));
            yield return Settle();
            yield return Shot("workshop-minigun-free");
            garage.Press(CareerShop.GunId(GunKind.Minigun));
            yield return Settle();
            Expect(state.Current.loadout.gun == GunKind.Minigun && state.wallet.Money == money, "the minigun goes back on for nothing");
        }

        private IEnumerator Market()
        {
            GarageWindow garage = GarageWindow.For(manager);
            CareerState state = manager.CurrentCareer;
            garage.TurnTo(GarageWindow.MarketPage);
            yield return Settle();
            int mines = state.consumables.Get(CareerShop.MineId);
            garage.Press(CareerShop.MineId);
            garage.Press(CareerShop.SpecialId(SpecialKind.Oil));
            garage.Press(CareerShop.SpecialId(SpecialKind.Emp));
            yield return Settle();
            Expect(state.consumables.Get(CareerShop.MineId) == mines + 1, "a mine is bought in the market");
            Expect(state.consumables.Get("oil") == 1 && state.consumables.Get("emp") == 1, "an oil slick and an EMP blast are bought");
            garage.Pick(CareerShop.SpecialId(SpecialKind.Emp));
            yield return Settle();
            garage.PressArm();
            yield return Settle();
            Expect(state.armed == SpecialKind.Emp, "the EMP blast is armed first");
            yield return Shot("market-emp-armed");
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

        private IEnumerator CareerRace(string label)
        {
            CareerState state = manager.CurrentCareer;
            RaceDirector director = manager.Director;
            if (director == null)
            {
                Fail("no race director");
                yield break;
            }
            int day = state.day;
            int easy = state.offers.FindIndex(offer => offer.difficulty == Difficulty.Easy);
            director.Started += Pilot;
            float speed = Mathf.Clamp(Number(SpeedArgument, 2.5f), 0.5f, 3f);
            bool entered = manager.EnterOffer(easy);
            Expect(entered, $"an offer of the {state.League.Name} is entered");
            director.Started -= Pilot;
            if (!entered)
            {
                yield break;
            }
            Expect(!GarageWindow.For(manager).IsOpen, "the garage closes for the race");
            // The HUD of the very first frames: it must sit in the corners already, not small in the middle.
            yield return null;
            yield return Shot(label + "-grid");
            yield return new WaitForSecondsRealtime(2f);
            yield return Shot(label + "-countdown");
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
                    yield return Shot($"{label}-hud-{hudShot}");
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
            yield return Shot(label + "-results");
            Expect(manager.CurrentCareer.day == day + 1, "the career moved on to the next day");
            Expect(manager.LatestSave != null && manager.LatestSave.Summary == DeathRaceManager.SummaryOf(manager.CurrentCareer),
                "the career saved itself after the race (the save is of the next day)");
            manager.Back();
            yield return Settle();
            Expect(GarageWindow.For(manager).IsOpen, "Continue goes back to the garage");
            yield return Shot(label + "-garage-next-day");
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

        /// <summary>The champion's window of the first league, and moving up to the Iron League from it.</summary>
        private IEnumerator Champion()
        {
            GarageWindow.For(manager).Hide();
            CareerState state = manager.CurrentCareer;
            state.champion = true;
            state.championDay = state.day;
            manager.Progress.WinLeague(state.league);
            ChampionWindow window = ChampionWindow.For(manager);
            window.Show();
            yield return Settle();
            Expect(window.IsOpen, "the champion's window opens");
            Expect(window.OffersMoveUp, "the champion of the first league is offered the Iron League");
            yield return Shot("champion");
            titleSave = DeathRaceManager.TitleSaveName(state);
            window.Hide();
            Expect(manager.MoveUpLeague(), "the champion moves up");
            yield return Settle();
            CareerState iron = manager.CurrentCareer;
            Expect(iron != state && iron.League.Kind == LeagueKind.Iron && iron.driverName == state.driverName && iron.colorIndex == state.colorIndex,
                "a career of the Iron League for the same driver in the same colour");
            Expect(manager.SavedGames != null && manager.SavedGames.Find(titleSave) != null, "the finished career is kept as a save named after the title");
            GarageWindow garage = GarageWindow.For(manager);
            Expect(garage.IsOpen, "the Iron League's garage opens");
            yield return Shot("iron-garage-races");
            foreach (string page in new[] { GarageWindow.GaragePage, GarageWindow.DealerPage, GarageWindow.StandingsPage })
            {
                garage.TurnTo(page);
                yield return Settle();
                yield return Shot("iron-garage-" + page);
            }
            garage.TurnTo(GarageWindow.RacePage);
            yield return Settle();
        }

        private IEnumerator QuickRace()
        {
            GarageWindow.For(manager).Hide();
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
            // The development console pops up over the game on any error (the errors are in the log already).
            Debug.developerConsoleVisible = false;
            yield return null;
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

    /// <summary>
    /// Development players only: what the tours do to the device's record of the leagues. A tour opens leagues as a
    /// device where the leagues before were won would have them (<see cref="OpenUpTo"/>), or closes them for a look at
    /// a new device (<see cref="Close"/>), rather than the game having a cheat; <see cref="Restore"/> puts the titles and
    /// the tracks the careers opened back as they were before the tour, so a tour leaves the device as it found it.
    /// </summary>
    public sealed class TourLeagues
    {
        private const int Missing = int.MinValue;

        private static readonly string[] Keys =
        {
            "Championships", "LeagueTitles.Recorded", "LeagueTitles.0", "LeagueTitles.1", "LeagueTitles.2", "CareerTracks", "CareerTrackMask"
        };

        private readonly DeathRaceProgress progress;
        private readonly int[] saved;

        public TourLeagues(DeathRaceProgress progress)
        {
            this.progress = progress;
            saved = new int[Keys.Length];
            for (int i = 0; i < Keys.Length; i++)
            {
                saved[i] = progress.Value(Keys[i], Missing);
            }
        }

        /// <summary>Opens every league up to <paramref name="league"/>: the ones before it have a title on the device.</summary>
        public void OpenUpTo(LeagueKind league)
        {
            for (int i = 0; i < (int)league; i++)
            {
                if (progress.LeagueTitles(i) == 0)
                {
                    progress.WinLeague(i);
                }
            }
        }

        /// <summary>No title on the device: only the first league is open (as on a new device).</summary>
        public void Close()
        {
            progress.Reset(0, null, new[] { "Championships", "LeagueTitles.0", "LeagueTitles.1", "LeagueTitles.2" });
            progress.SetValue("LeagueTitles.Recorded", 1);
        }

        /// <summary>The titles and the opened tracks as they were when the tour began.</summary>
        public void Restore()
        {
            for (int i = 0; i < Keys.Length; i++)
            {
                if (saved[i] == Missing)
                {
                    progress.Reset(0, null, new[] { Keys[i] });
                }
                else
                {
                    progress.SetValue(Keys[i], saved[i]);
                }
            }
        }
    }
}
