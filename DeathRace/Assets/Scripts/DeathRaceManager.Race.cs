using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// The computer cars behind the menu (attract mode), and what the manager does when a race starts.
    ///
    /// Behind the main menu four computer cars race two laps of a track, seen by the overview camera drifting over the
    /// leaders, with no HUD and their engines low; when that race ends the next track of the game takes its turn, so
    /// the menu shows every track in time. They give way to any race of people and come back with the menu.
    /// </summary>
    public partial class DeathRaceManager
    {
        /// <summary>The laps of a race behind the menu.</summary>
        public const int AttractLaps = 2;

        /// <summary>The cars of a race behind the menu, a spread from the middle of the field to the top of the last league.</summary>
        private static readonly CarModel[] AttractCars =
        {
            CarModel.Hornet, CarModel.Viper, CarModel.Bulldog, CarModel.Reaper, CarModel.Tyrant, CarModel.Jackal, CarModel.Mauler, CarModel.Banshee,
            CarModel.Spectre, CarModel.Goliath, CarModel.Apocalypse
        };

        private int attractTrack;
        private int attractRaces;

        /// <summary>Starts the computer cars racing behind the menu unless they already are (called often: keep it idempotent).</summary>
        public void StartAttract()
        {
            if (director == null || !isActiveAndEnabled)
            {
                return;
            }
            Race current = director.Current;
            if (current != null)
            {
                if (current.Setup.Mode != RaceMode.Attract || current.Phase != RacePhase.Over)
                {
                    // A race of people is on, or the cars are already racing behind the menu.
                    return;
                }
                // The race behind the menu ended: the next track takes its turn.
                attractTrack = (attractTrack + 1) % TrackLayouts.Count;
            }
            director.Begin(AttractSetup(attractTrack, attractRaces++));
        }

        /// <summary>Takes the computer cars behind the menu away (a race of people is about to start).</summary>
        public void StopAttract()
        {
            if (director != null && director.Current != null && director.Current.Setup.Mode == RaceMode.Attract)
            {
                director.Abort();
            }
        }

        /// <summary>
        /// The race behind the menu on <paramref name="track"/>: four computer cars of the upper tiers, armed (most with a
        /// gun of their own, each with a special weapon), so the menu shows off the whole armoury in time.
        /// </summary>
        public RaceSetup AttractSetup(int track, int round = 0)
        {
            var random = new System.Random(track * 7919 + round * 104729 + 11);
            var setup = new RaceSetup
            {
                Mode = RaceMode.Attract,
                Track = Mathf.Clamp(track, 0, TrackLayouts.Count - 1),
                Laps = AttractLaps,
                Difficulty = Difficulty.Hard,
                Seed = random.Next(),
                Pickups = true
            };
            DeathRaceTheme theme = Look;
            int first = random.Next(AttractCars.Length);
            for (int seat = 0; seat < 4; seat++)
            {
                CarModel model = AttractCars[(first + seat * 2) % AttractCars.Length];
                Color color = theme != null && theme.rivalColors != null && theme.rivalColors.Length > 0
                    ? theme.rivalColors[(seat * 5 + round) % theme.rivalColors.Length]
                    : Color.HSVToRGB((seat * 0.27f + round * 0.11f) % 1f, 0.75f, 0.9f);
                CarLoadout loadout = CarLoadout.Upgraded(model, random.Next(1, 4));
                loadout.mines = 2;
                loadout.rockets = 2;
                loadout.nitro = 2;
                loadout.GiveGun((GunKind)random.Next(0, Guns.Count));
                loadout.special = (SpecialKind)random.Next(0, Specials.Count);
                loadout.SetSpecials(loadout.special, 2);
                setup.Entrants.Add(new RaceEntrant
                {
                    Seat = seat,
                    Name = CarSpecs.Of(model).Name,
                    Color = color,
                    Driver = DriverKind.Computer,
                    Skill = 0.6f + 0.35f * (float)random.NextDouble(),
                    Loadout = loadout
                });
            }
            return setup;
        }

        /// <summary>A race started: the music of the menu behind the menu, the music of the race otherwise.</summary>
        partial void OnRaceStarted(Race race)
        {
            if (sounds == null || content == null || content.sounds == null)
            {
                return;
            }
            AudioClip clip = race.Setup.Mode == RaceMode.Attract ? content.sounds.menuMusic : content.sounds.raceMusic;
            if (clip != null)
            {
                sounds.PlayMusic(clip, 1.5f);
            }
        }
    }
}
