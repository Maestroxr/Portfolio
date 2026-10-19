using System.Collections.Generic;
using Gamebox.Racing;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>Where a race is in its course.</summary>
    public enum RacePhase
    {
        /// <summary>The cars are on the grid, the race is being built.</summary>
        Grid = 0,
        /// <summary>3, 2, 1...</summary>
        Countdown = 1,
        Racing = 2,
        /// <summary>The people are done; the computer's cars get a little while to finish.</summary>
        Finishing = 3,
        Over = 4
    }

    /// <summary>
    /// One race being run: its setup, its track and racing line, its cars and their racers, the clock and the phase, and
    /// the race's effects, sounds, pickups and cameras. The <see cref="RaceDirector"/> builds and runs it; the cars, the
    /// weapons and the HUDs read it.
    /// </summary>
    public class Race
    {
        private readonly List<RaceRanking.Entry> entries = new List<RaceRanking.Entry>();
        private readonly List<Car> standings = new List<Car>();

        public Race(RaceSetup setup, Track track, Transform root)
        {
            Setup = setup;
            Track = track;
            Root = root;
        }

        public RaceSetup Setup { get; }
        public Track Track { get; }

        /// <summary>The object everything of the race is under (destroyed with it).</summary>
        public Transform Root { get; }

        public List<Car> Cars { get; } = new List<Car>();
        public RacePhase Phase { get; set; } = RacePhase.Grid;

        /// <summary>Seconds since the green light (negative on the grid and during the countdown).</summary>
        public float Time { get; set; }

        /// <summary>The countdown's word to show now ("3", "2", "1", "GO!"), or null.</summary>
        public string CountdownLabel { get; set; }

        public int Laps => Setup != null ? Setup.Laps : 1;

        public RaceEffects Effects { get; set; }
        public RaceAudio Audio { get; set; }
        public PickupField Pickups { get; set; }

        /// <summary>The racing line of the track (the computer's drivers blend it with the middle of the road).</summary>
        public RacingLine Line { get; set; }

        /// <summary>The cameras looking at the race (one per person at this device, or the overview).</summary>
        public List<RaceCamera> Cameras { get; } = new List<RaceCamera>();

        /// <summary>The fastest lap of the race so far (seconds, 0 before the first).</summary>
        public float BestLap { get; set; }

        /// <summary>The car that drove <see cref="BestLap"/>.</summary>
        public Car BestLapCar { get; set; }

        /// <summary>Whether a person at this device races (not the computer cars behind the menu).</summary>
        public bool HasHumans
        {
            get
            {
                foreach (Car car in Cars)
                {
                    if (car != null && car.IsHuman)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public bool IsRunning => Phase == RacePhase.Racing || Phase == RacePhase.Finishing;

        public Car CarAt(int seat)
        {
            foreach (Car car in Cars)
            {
                if (car != null && car.Entrant.Seat == seat)
                {
                    return car;
                }
            }
            return null;
        }

        /// <summary>
        /// The cars from first to last as they stand now: those that finished by their time, then those still racing by
        /// how far they got, then the wrecked ones by how far they got. The list is reused: copy it to keep it.
        /// </summary>
        public virtual List<Car> Standings()
        {
            entries.Clear();
            foreach (Car car in Cars)
            {
                Racer racer = car.Racer;
                entries.Add(new RaceRanking.Entry
                {
                    Id = car.Entrant.Seat,
                    Finished = racer != null && racer.Finished,
                    FinishTime = racer != null ? racer.FinishTime : 0f,
                    Out = car.IsWrecked,
                    Progress = racer != null ? racer.Progress : 0f
                });
            }
            int[] order = RaceRanking.Order(entries);
            standings.Clear();
            foreach (int index in order)
            {
                standings.Add(Cars[index]);
            }
            return standings;
        }

        /// <summary>The current order (see <see cref="Standings"/>).</summary>
        public List<Car> Places()
        {
            return Standings();
        }

        /// <summary>The car in the lead now, or null.</summary>
        public Car Leader
        {
            get
            {
                List<Car> order = Standings();
                return order.Count > 0 ? order[0] : null;
            }
        }
    }
}
