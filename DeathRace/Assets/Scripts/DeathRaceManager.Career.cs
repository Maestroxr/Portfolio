using System.Collections.Generic;
using Gamebox;
using Gamebox.Lockstep;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// The career and quick races. The career is a <see cref="CareerState"/> held here: the menu's Career opens a new
    /// one in a league open on this device (<see cref="NewCareerWindow"/>) or the garage of the one being played
    /// (<see cref="GarageWindow"/>), an offer of the garage becomes a race (<see cref="EnterOffer"/>), and when it is
    /// over <see cref="Career.Apply"/> books it, the device records the tracks it opened and a title, the results show,
    /// the career saves itself and the garage opens again on the next day (or the champion's window, once the league's
    /// champion is beaten, which offers to move up to the next league: <see cref="MoveUpLeague"/>). A quick race is set
    /// up in its own window and its results offer the same race again.
    /// </summary>
    public partial class DeathRaceManager
    {
        private CareerState career;
        private RaceSetup lastQuickRace;

        /// <summary>The career being played, or null.</summary>
        public CareerState CurrentCareer => career;

        public bool HasCareer => career != null;

        /// <summary>The menu's Career: a new career, or the garage of the one being played.</summary>
        public void OpenCareer()
        {
            if (career == null)
            {
                NewCareerWindow.For(this).Show();
            }
            else
            {
                GarageWindow.For(this).Show();
            }
        }

        /// <summary>The menu's Quick Race: the setup of a race against the computer.</summary>
        public void OpenQuickRace()
        {
            QuickRaceWindow.For(this).Show();
        }

        /// <summary>The garage of the career, on <paramref name="page"/> (null: the page it was on, or the races of the day).</summary>
        public void OpenGarage(string page = null)
        {
            if (career == null)
            {
                OpenCareer();
                return;
            }
            GarageWindow.For(this).Show(page);
        }

        /// <summary>
        /// Starts a new career for <paramref name="driverName"/> in colour <paramref name="colorIndex"/> in
        /// <paramref name="league"/> and opens its garage.
        /// </summary>
        public void StartCareer(string driverName, int colorIndex, int seed = 0, LeagueKind league = LeagueKind.Wasteland)
        {
            TakeCareer(Career.New(driverName, colorIndex, seed != 0 ? seed : NewSeed(), league));
            Autosave();
            GarageWindow.For(this).Show(GarageWindow.RacePage);
        }

        /// <summary>The league after the one of the career being played, when its champion has been beaten there; else null.</summary>
        public LeagueSpec NextLeague => career != null && career.champion ? career.League.Next : null;

        /// <summary>
        /// The champion moves up: the finished career is kept as a save named after the title, and a new career of the
        /// next league starts for the same driver in the same colour, in its garage. False when there is no league to
        /// move up to (or the finished career could not be kept, which is said).
        /// </summary>
        public bool MoveUpLeague()
        {
            LeagueSpec next = NextLeague;
            if (next == null)
            {
                return false;
            }
            if (!SaveAs(TitleSaveName(career), out string problem))
            {
                UI?.UpdateError(problem);
                return false;
            }
            TakeCareer(Career.MoveUp(career, NewSeed()));
            Autosave();
            GarageWindow.For(this).Show(GarageWindow.RacePage);
            return true;
        }

        /// <summary>"Ada, Wasteland Circuit champion": the name the finished career of a champion is kept under.</summary>
        public static string TitleSaveName(CareerState state)
        {
            string name = Portfolio.DeathRace.L.F("{0}, {1} champion", state.driverName, Portfolio.DeathRace.L.Data(state.League.Name));
            return SaveLibrary.CleanName(name) ?? state.driverName;
        }

        private static int NewSeed()
        {
            return System.Environment.TickCount & 0x7FFFFFFF;
        }

        /// <summary>Takes up <paramref name="state"/> (a new career or a loaded save) as the career being played.</summary>
        private void TakeCareer(CareerState state)
        {
            career = state;
            RecordTracks();
            // Cash bought in the shop while no career was open goes into this one.
            int cash = DeathRaceShop.TakePendingCash(Progress);
            if (cash > 0)
            {
                career.wallet.Add(cash);
            }
        }

        /// <summary>
        /// Races today's offer <paramref name="index"/>: the career saves itself as it is before the start (leaving the
        /// race midway loses nothing but the race), the garage closes and the race begins.
        /// </summary>
        public bool EnterOffer(int index)
        {
            if (career == null || index < 0 || index >= career.offers.Count || IsRacing)
            {
                return false;
            }
            Autosave();
            RaceSetup setup = Career.Enter(career, index, DriverColor);
            foreach (RaceEntrant entrant in setup.Entrants)
            {
                // The rivals race under their names in the language shown (the HUD, the minimap, the results).
                entrant.Name = entrant.IsHuman ? entrant.Name : Portfolio.DeathRace.L.Data(entrant.Name);
            }
            GarageWindow.For(this).Hide();
            BeginRace(setup);
            return true;
        }

        /// <summary>
        /// Remembers on the device the tracks the career has opened (quick races and split screen offer them): every league
        /// by track, the first league also by its old count, for what reads that.
        /// </summary>
        private void RecordTracks()
        {
            Progress.OpenCareerTracks(career.OpenTracks);
            if (career.League.Kind == LeagueKind.Wasteland)
            {
                Progress.OpenCareerTracks(career.tracksOpen);
            }
        }

        /// <summary>The colour of a driver of the career in the look shown (an index into its driver colours).</summary>
        public Color DriverColor(int colorIndex)
        {
            DeathRaceTheme theme = Look;
            return theme != null && theme.rivalColors != null && theme.rivalColors.Length > 0
                ? theme.RivalColor(colorIndex)
                : RaceWindowParts.FallbackColor(colorIndex);
        }

        partial void OnCareerRaceFinished(RaceResult result)
        {
            if (career == null)
            {
                return;
            }
            CareerReport report = Career.Apply(career, result);
            RecordTracks();
            if (report.Champion)
            {
                Progress.WinLeague(career.league);
            }
            Autosave();
            ShowResultsState(result);
            ResultsWindow.For(this).ShowCareer(result, report, AfterCareerResults);
        }

        /// <summary>The results of a career race were read: back to the garage, or the champion's moment.</summary>
        private void AfterCareerResults(CareerReport report)
        {
            TransitionState(BaseGameState.Initialization);
            if (report != null && report.Champion)
            {
                ChampionWindow.For(this).Show();
            }
            else
            {
                GarageWindow.For(this).Show(GarageWindow.RacePage);
            }
        }

        partial void OnQuickRaceFinished(RaceResult result)
        {
            lastQuickRace = result.Setup;
            ShowResultsState(result);
            ResultsWindow.For(this).ShowQuick(result, RaceAgain, () => TransitionState(BaseGameState.Initialization));
        }

        /// <summary>The quick race just run, again: the same track, car, difficulty and laps, a new seed.</summary>
        private void RaceAgain()
        {
            if (lastQuickRace == null)
            {
                BeginRace(QuickRaceSetup(LevelIndex));
                return;
            }
            RaceEntrant player = lastQuickRace.EntrantAt(0);
            CarModel car = player != null ? player.Loadout.model : CarModel.Viper;
            BeginRace(QuickRaceSetup(lastQuickRace.Track, car, lastQuickRace.Difficulty, lastQuickRace.Laps));
        }

        /// <summary>The race is over: the game's state says how it went for the person at this device (the results window shows over it).</summary>
        private void ShowResultsState(RaceResult result)
        {
            RacerResult player = result.Of(0);
            bool won = player != null && player.Driver == DriverKind.Human && player.Place == 1 && !player.Wrecked;
            TransitionState(won ? BaseGameState.Victory : BaseGameState.GameOver);
        }

        /// <summary>
        /// A quick race on <paramref name="track"/>: the settings' car with every part one level up and a full load of
        /// mines, rockets, nitro and two of each special weapon, against three computer cars of the same model, as strong
        /// and as upgraded as the settings' difficulty says (with the first league's special weapons for the difficulty,
        /// of a kind of their own, and on Hard a gun of their own), driven by drivers of the matching third of the
        /// career's roster.
        /// </summary>
        public RaceSetup QuickRaceSetup(int track)
        {
            DeathRaceSettings options = Options;
            return QuickRaceSetup(track, options != null ? options.QuickCar : CarModel.Viper, options != null ? options.QuickDifficulty : Difficulty.Medium,
                options != null ? options.quickLaps : 0);
        }

        /// <summary>A quick race on <paramref name="track"/> in <paramref name="model"/> (the quick race window's choice); 0 laps is the track's own.</summary>
        public RaceSetup QuickRaceSetup(int track, CarModel model, Difficulty difficulty, int laps)
        {
            track = Mathf.Clamp(track, 0, TrackLayouts.Count - 1);
            difficulty = (Difficulty)Mathf.Clamp((int)difficulty, 0, (int)Difficulty.Hard);
            laps = laps > 0 ? Mathf.Clamp(laps, DeathRaceSettings.MinLaps, DeathRaceSettings.MaxLaps) : TrackLayouts.Of(track).Laps;
            int seed = System.Environment.TickCount & 0x7FFFFFFF;
            var setup = new RaceSetup
            {
                Mode = RaceMode.Quick,
                Track = track,
                Laps = laps,
                Difficulty = difficulty,
                Seed = seed,
                CashPerPickup = CareerRules.CashPerPickup(difficulty)
            };
            CarLoadout player = CarLoadout.Upgraded(model, 1);
            player.mines = 3;
            player.rockets = 2;
            player.nitro = 3;
            foreach (SpecialSpec special in Specials.All)
            {
                player.SetSpecials(special.Kind, QuickSpecials);
            }
            int playerColor = career != null ? career.colorIndex : 0;
            setup.Entrants.Add(new RaceEntrant
            {
                Seat = 0, Name = career != null ? career.driverName : PlayerName(), Color = DriverColor(playerColor), Driver = DriverKind.Human,
                Skill = 1f, Loadout = player, Viewport = 0
            });
            var random = new SeededRandom((uint)seed);
            List<RosterDriver> drivers = QuickRivals(difficulty, random);
            int level = (int)difficulty;
            for (int seat = 1; seat < CareerRules.CarsPerRace; seat++)
            {
                RosterDriver driver = drivers[seat - 1];
                int color = Roster.IndexOf(driver.Id) + 1;
                CarLoadout loadout = CarLoadout.Upgraded(model, level);
                loadout.spikes = difficulty == Difficulty.Hard && seat == 1;
                loadout.piercing = difficulty == Difficulty.Hard;
                (loadout.mines, loadout.rockets, loadout.nitro) = CareerRules.AiCarries(difficulty);
                var special = (SpecialKind)random.Range(0, Specials.Count);
                loadout.SetSpecials(special, CareerRules.AiSpecials(Leagues.Wasteland, difficulty));
                loadout.special = special;
                if (difficulty == Difficulty.Hard)
                {
                    loadout.GiveGun(CareerRules.BoughtGuns[random.Range(0, CareerRules.BoughtGuns.Length)]);
                }
                setup.Entrants.Add(new RaceEntrant
                {
                    Seat = seat, Name = Portfolio.DeathRace.L.Data(driver.Name), Color = DriverColor(color == playerColor ? 0 : color),
                    Driver = DriverKind.Computer, Skill = CareerRules.AiSkill(difficulty, (seat - 1) / 2f),
                    Loadout = loadout
                });
            }
            return setup;
        }

        /// <summary>How many of each special weapon the person carries into a quick race.</summary>
        public const int QuickSpecials = 2;

        /// <summary>Three drivers of the roster's third that matches <paramref name="difficulty"/>, in a random order.</summary>
        private static List<RosterDriver> QuickRivals(Difficulty difficulty, IRandom random)
        {
            int third = Roster.Rivals.Length / 3;
            int start = difficulty == Difficulty.Easy ? 0 : difficulty == Difficulty.Medium ? third : 2 * third;
            int end = difficulty == Difficulty.Hard ? Roster.Rivals.Length : start + third;
            var pool = new List<RosterDriver>();
            for (int i = start; i < end; i++)
            {
                pool.Add(Roster.Rivals[i]);
            }
            random.Shuffle(pool);
            return pool;
        }

        /// <summary>The name of the person at this device: their avatar's, else "You".</summary>
        public string PlayerName()
        {
            AvatarState avatar = LocalAvatar.Instance.State;
            string name = avatar != null ? avatar.Name : "";
            return string.IsNullOrWhiteSpace(name) ? Portfolio.DeathRace.L.T("You") : name;
        }

        /// <summary>Starts a quick race (the quick race window's Start).</summary>
        public void StartQuickRace(int track, CarModel model, Difficulty difficulty, int laps)
        {
            BeginRace(QuickRaceSetup(track, model, difficulty, laps));
        }
    }
}
