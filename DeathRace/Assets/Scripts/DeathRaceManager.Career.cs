using System.Collections.Generic;
using Gamebox;
using Gamebox.Lockstep;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// The career and quick races. The career is a <see cref="CareerState"/> held here: the menu's Career opens a new
    /// one (<see cref="NewCareerWindow"/>) or the garage of the one being played (<see cref="GarageWindow"/>), an offer
    /// of the garage becomes a race (<see cref="EnterOffer"/>), and when it is over <see cref="Career.Apply"/> books it,
    /// the results show, the career saves itself and the garage opens again on the next day (or the champion's window,
    /// once The Warden is beaten). A quick race is set up in its own window and its results offer the same race again.
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

        /// <summary>Starts a new career for <paramref name="driverName"/> in colour <paramref name="colorIndex"/> and opens its garage.</summary>
        public void StartCareer(string driverName, int colorIndex, int seed = 0)
        {
            TakeCareer(Career.New(driverName, colorIndex, seed != 0 ? seed : System.Environment.TickCount & 0x7FFFFFFF));
            Autosave();
            GarageWindow.For(this).Show(GarageWindow.RacePage);
        }

        /// <summary>Takes up <paramref name="state"/> (a new career or a loaded save) as the career being played.</summary>
        private void TakeCareer(CareerState state)
        {
            career = state;
            Progress.OpenCareerTracks(career.tracksOpen);
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
            GarageWindow.For(this).Hide();
            BeginRace(setup);
            return true;
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
            Progress.OpenCareerTracks(career.tracksOpen);
            if (report.Champion)
            {
                Progress.WinChampionship();
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
        /// mines, rockets and nitro, against three computer cars of the same model, as strong and as upgraded as the
        /// settings' difficulty says, driven by drivers of the matching third of the career's roster.
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
                setup.Entrants.Add(new RaceEntrant
                {
                    Seat = seat, Name = Portfolio.DeathRace.L.Data(driver.Name), Color = DriverColor(color == playerColor ? 0 : color),
                    Driver = DriverKind.Computer, Skill = CareerRules.AiSkill(difficulty, (seat - 1) / 2f),
                    Loadout = loadout
                });
            }
            return setup;
        }

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
