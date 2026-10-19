using Gamebox.Racing;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// How one car's race goes: its laps (a <see cref="LapTracker"/>), its place, its kills, the cash it picked up and,
    /// if it was wrecked, when.
    /// </summary>
    public class Racer
    {
        public Racer(int seat, LapTracker tracker)
        {
            Seat = seat;
            Tracker = tracker;
        }

        public int Seat { get; }
        public LapTracker Tracker { get; }

        /// <summary>The place now (from 1).</summary>
        public int Place { get; set; } = 1;

        public int Kills { get; set; }
        public int CashPicked { get; set; }

        public bool Finished => Tracker != null && Tracker.Finished;
        public float FinishTime => Tracker != null ? Tracker.FinishTime : 0f;
        public float Progress => Tracker != null ? Tracker.Progress : 0f;
        public int Lap => Tracker != null ? Tracker.CurrentLap : 1;
        public int LapsCompleted => Tracker != null ? Tracker.Lap : 0;
        public float BestLap => Tracker != null ? Tracker.BestLap : 0f;
        public bool WrongWay => Tracker != null && Tracker.WrongWay;

        /// <summary>The time of the lap being driven at <paramref name="raceTime"/>.</summary>
        public float CurrentLapTime(float raceTime)
        {
            return Tracker != null ? Tracker.CurrentLapTime(raceTime) : 0f;
        }

        /// <summary>Whether the car was wrecked (<see cref="OutTime"/> is when).</summary>
        public bool Out => OutTime >= 0f;

        /// <summary>When the car was wrecked (race time), or -1.</summary>
        public float OutTime { get; set; } = -1f;
    }
}
