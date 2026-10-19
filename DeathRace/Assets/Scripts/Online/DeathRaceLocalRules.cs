using System;
using System.Collections.Generic;
using Gamebox;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// Split screen: what Death Race offers two to four people at one device (BaseGame's local play setup), and how the
    /// setup they made becomes a race. Everybody drives at once on controls of their own (<see cref="DeathRaceControls"/>);
    /// a seat may be the computer's (an AI car at the seat's level), and computer cars can fill the grid to four. The
    /// options: the track (only the tracks this device has opened, of all twelve), the laps, the car everybody drives (any
    /// of the twelve, the same car for a fair race, or a random car per seat for a wild one) and how good the cars that
    /// fill the grid are.
    /// <see cref="BuildSetup"/> turns a match into the <see cref="RaceSetup"/> the director runs; it is plain code, so the
    /// tests build setups without a scene.
    /// </summary>
    public sealed class DeathRaceLocalRules : LocalPlayRules
    {
        public const string TrackOption = "track";
        public const string LapsOption = "laps";
        public const string CarOption = "car";
        public const string FillOption = "fill";

        /// <summary>The car option's last value: every seat gets a car drawn at random, again for every race.</summary>
        public const string MixedCars = "Mixed";

        /// <summary>The cars of a full grid.</summary>
        public const int GridSize = 4;

        /// <summary>The laps option: the track's own number of laps, or a number of the people's choosing.</summary>
        public static readonly string[] LapValues = { "Track's own", "1", "2", "3", "4", "5", "6", "7", "8", "9" };

        /// <summary>The computer cars that fill the grid to four: none, or at a level.</summary>
        public static readonly string[] FillValues = { "None", "Easy", "Normal", "Hard" };

        /// <summary>How well a computer car drives at each level of <see cref="LocalPlayRules.ComputerLevels"/>.</summary>
        public static readonly float[] LevelSkills = { 0.3f, 0.55f, 0.85f };

        /// <summary>How many of each special weapon (oil, EMP, mortar) every car of a split screen race carries.</summary>
        public const int SpecialsPerSeat = 1;

        /// <summary>The car a first setup drives: a mid-field car that can win against anything on the list.</summary>
        public const CarModel DefaultCar = CarModel.Viper;

        private readonly List<int> tracks = new List<int>();

        /// <param name="openTracks">The tracks the setup offers, by their index in <see cref="TrackLayouts.All"/>.</param>
        /// <param name="seatColors">The colours of the four seats (the look's), or null for BaseGame's.</param>
        public DeathRaceLocalRules(IEnumerable<int> openTracks, Color[] seatColors)
        {
            Game = GameType.DeathRace;
            Title = "Split Screen";
            Hint = "Two to four drivers at one screen, each in a view of their own. Computer cars can fill the grid.";
            MinPlayers = 2;
            MaxPlayers = GridSize;
            DefaultPlayers = 2;
            MinHumans = 2;
            Simultaneous = true;
            Controls = DeathRaceControls.Scheme();
            AllowComputers = true;
            DefaultComputerLevel = 1;
            SeatColors = seatColors;
            if (openTracks != null)
            {
                foreach (int track in openTracks)
                {
                    if (track >= 0 && track < TrackLayouts.Count && !tracks.Contains(track))
                    {
                        tracks.Add(track);
                    }
                }
            }
            if (tracks.Count == 0)
            {
                tracks.Add(0);
            }
            var trackNames = new string[tracks.Count];
            for (int i = 0; i < tracks.Count; i++)
            {
                trackNames[i] = TrackLayouts.Of(tracks[i]).Name;
            }
            var carNames = new string[CarSpecs.Count + 1];
            for (int i = 0; i < CarSpecs.Count; i++)
            {
                carNames[i] = CarSpecs.All[i].Name;
            }
            carNames[CarSpecs.Count] = MixedCars;
            Options.Add(new LocalOption(TrackOption, "Track", trackNames));
            Options.Add(new LocalOption(LapsOption, "Laps", LapValues));
            Options.Add(new LocalOption(CarOption, "Car", carNames, (int)DefaultCar));
            Options.Add(new LocalOption(FillOption, "Computer cars", FillValues, 2));
        }

        /// <summary>The tracks offered, by their index in <see cref="TrackLayouts.All"/>, in the order of the option.</summary>
        public IReadOnlyList<int> Tracks => tracks;

        /// <summary>The track the match races, an index into <see cref="TrackLayouts.All"/>.</summary>
        public int TrackOf(LocalMatch match)
        {
            int index = match != null ? match.Option(TrackOption) : 0;
            return tracks[Mathf.Clamp(index, 0, tracks.Count - 1)];
        }

        /// <summary>The laps of the match: the people's number, or the track's own.</summary>
        public int LapsOf(LocalMatch match)
        {
            int index = match != null ? match.Option(LapsOption) : 0;
            return index > 0 && index < LapValues.Length ? index : TrackLayouts.Of(TrackOf(match)).Laps;
        }

        /// <summary>The car everybody drives, or null for a random car per seat (<see cref="MixedCars"/>).</summary>
        public CarModel? CarOf(LocalMatch match)
        {
            int index = match != null ? match.Option(CarOption) : (int)DefaultCar;
            if (index < 0)
            {
                return DefaultCar;
            }
            return index < CarSpecs.Count ? (CarModel)index : (CarModel?)null;
        }

        /// <summary>The level of the computer cars that fill the grid (0 Easy to 2 Hard), or -1 for none.</summary>
        public int FillLevelOf(LocalMatch match)
        {
            int index = match != null ? match.Option(FillOption) : 2;
            return Mathf.Clamp(index, 0, FillValues.Length - 1) - 1;
        }

        /// <summary>Next to each seat of a mixed grid: that its car will be drawn at random (a matched car is the option's).</summary>
        public override string SeatDetail(LocalMatch match, int seat)
        {
            return CarOf(match).HasValue ? null : L.T("Random car");
        }

        /// <summary>
        /// The race of <paramref name="match"/>: the people and the computer seats in their seat order, then the computer
        /// cars that fill the grid. Every car carries the same load (one level of every upgrade, three mines, two rockets,
        /// three nitro charges and one of each special weapon), so only the drivers make the difference; a computer car
        /// drives at its seat's level. The people get the views of the split screen in their seat order.
        /// <paramref name="translate"/> turns the English names of the drivers who fill the grid into the language shown;
        /// <paramref name="fillerColor"/> gives the colour of the roster driver with that index (the look's rival colours).
        /// </summary>
        public RaceSetup BuildSetup(LocalMatch match, int seed, Func<string, string> translate = null, Func<int, Color> fillerColor = null)
        {
            int track = TrackOf(match);
            var setup = new RaceSetup
            {
                Mode = RaceMode.Local,
                Track = track,
                Laps = LapsOf(match),
                Difficulty = Difficulty.Medium,
                Seed = seed,
                CashPerPickup = CareerRules.CashPerPickup(Difficulty.Medium),
                Pickups = true
            };
            var random = new System.Random(seed);
            CarModel? matched = CarOf(match);
            int views = 0;
            IReadOnlyList<LocalSeat> players = match.Players;
            foreach (LocalSeat seat in players)
            {
                var entrant = new RaceEntrant
                {
                    Seat = setup.Entrants.Count,
                    Name = seat.Name,
                    Color = seat.Color,
                    Loadout = Loadout(matched ?? RandomCar(random))
                };
                if (seat.IsHuman)
                {
                    entrant.Driver = DriverKind.Human;
                    entrant.Skill = 1f;
                    entrant.Controls = seat.Controls;
                    entrant.Viewport = views++;
                }
                else
                {
                    entrant.Driver = DriverKind.Computer;
                    entrant.Skill = SkillOf(seat.Level);
                }
                setup.Entrants.Add(entrant);
            }

            int fill = FillLevelOf(match);
            if (fill >= 0)
            {
                List<RosterDriver> drivers = Fillers(fill, random);
                for (int i = 0; setup.Entrants.Count < GridSize && i < drivers.Count; i++)
                {
                    RosterDriver driver = drivers[i];
                    int rosterIndex = Roster.IndexOf(driver.Id) + 1;
                    setup.Entrants.Add(new RaceEntrant
                    {
                        Seat = setup.Entrants.Count,
                        Name = translate != null ? translate(driver.Name) : driver.Name,
                        Color = fillerColor != null ? fillerColor(rosterIndex) : Color.HSVToRGB(rosterIndex * 0.13f % 1f, 0.7f, 0.85f),
                        Driver = DriverKind.Computer,
                        Skill = SkillOf(fill),
                        RivalId = driver.Id,
                        Loadout = Loadout(matched ?? RandomCar(random))
                    });
                }
            }
            return setup;
        }

        /// <summary>How well a computer car drives at <paramref name="level"/> (0 Easy to 2 Hard).</summary>
        public static float SkillOf(int level)
        {
            return LevelSkills[Mathf.Clamp(level, 0, LevelSkills.Length - 1)];
        }

        /// <summary>
        /// The load every car of a split screen race carries: one level of every upgrade, a few of everything and
        /// <see cref="SpecialsPerSeat"/> of each special weapon, the oil slick armed first.
        /// </summary>
        public static CarLoadout Loadout(CarModel model)
        {
            CarLoadout loadout = CarLoadout.Upgraded(model, 1);
            loadout.mines = 3;
            loadout.rockets = 2;
            loadout.nitro = 3;
            foreach (SpecialKind kind in (SpecialKind[])Enum.GetValues(typeof(SpecialKind)))
            {
                loadout.SetSpecials(kind, SpecialsPerSeat);
            }
            loadout.special = SpecialKind.Oil;
            return loadout;
        }

        private static CarModel RandomCar(System.Random random)
        {
            return (CarModel)random.Next(CarSpecs.Count);
        }

        /// <summary>Drivers of the roster's third that matches <paramref name="level"/>, in a random order.</summary>
        private static List<RosterDriver> Fillers(int level, System.Random random)
        {
            int third = Roster.Rivals.Length / 3;
            int start = Mathf.Clamp(level, 0, 2) * third;
            int end = level >= 2 ? Roster.Rivals.Length : start + third;
            var pool = new List<RosterDriver>();
            for (int i = start; i < end; i++)
            {
                pool.Add(Roster.Rivals[i]);
            }
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }
            return pool;
        }
    }
}
