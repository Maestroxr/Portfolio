using System.Collections.Generic;
using Gamebox;

namespace Portfolio.EndlessRunner
{
    /// <summary>
    /// A race of several runners at one device (<see cref="LocalMatch"/>): a run per seat on the same track. It knows who
    /// leads, how far the field is spread (the track is kept from the last runner to the first), when the race is over
    /// (every run across the line or out of hearts) and the places, by the rule of the online race
    /// (<see cref="RaceStandings"/>): the higher score is ahead, a point a meter and the points of the coins.
    /// </summary>
    internal sealed class LocalRace
    {
        private readonly List<RunnerRun> runs = new List<RunnerRun>();
        private readonly List<Racer> ranking = new List<Racer>();

        public LocalRace(LocalMatch match)
        {
            Match = match;
        }

        public LocalMatch Match { get; }

        /// <summary>The runs by seat.</summary>
        public IReadOnlyList<RunnerRun> Runs => runs;

        /// <summary>The runners by place, once <see cref="Rank"/> ran.</summary>
        public IReadOnlyList<Racer> Ranking => ranking;

        /// <summary>Adds the run of <paramref name="seat"/>, with its line on the scoreboard.</summary>
        public void Add(RunnerRun run, LocalSeat seat)
        {
            run.Seat = seat;
            run.Racer = new Racer { Seat = seat.Index, Slot = runs.Count, Name = seat.Name };
            runs.Add(run);
        }

        /// <summary>The run of <paramref name="runner"/>, or null.</summary>
        public RunnerRun RunOf(RunnerPlayer runner)
        {
            if (runner == null)
            {
                return null;
            }
            foreach (RunnerRun run in runs)
            {
                if (run.Runner == runner)
                {
                    return run;
                }
            }
            return null;
        }

        /// <summary>Every run is over: across the line or out of hearts.</summary>
        public bool IsOver
        {
            get
            {
                foreach (RunnerRun run in runs)
                {
                    if (!run.Done)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        /// <summary>The runner in front of the ones who still run, or null when nobody does.</summary>
        public RunnerRun Leader()
        {
            RunnerRun leader = null;
            foreach (RunnerRun run in runs)
            {
                if (run.Phase == RunPhase.Running && (leader == null || run.Z > leader.Z))
                {
                    leader = run;
                }
            }
            return leader;
        }

        /// <summary>
        /// Where the first and the last runner on the track are: the runs that are not over (a runner who just crossed the
        /// line or fell still has its own view of the track). False when every run is over.
        /// </summary>
        public bool TrySpan(out float front, out float back)
        {
            front = float.NegativeInfinity;
            back = float.PositiveInfinity;
            foreach (RunnerRun run in runs)
            {
                if (run.Done)
                {
                    continue;
                }
                front = run.Z > front ? run.Z : front;
                back = run.Z < back ? run.Z : back;
            }
            return front >= back;
        }

        /// <summary>Brings the scoreboard up to date and gives every runner its place.</summary>
        public void Rank()
        {
            ranking.Clear();
            foreach (RunnerRun run in runs)
            {
                Racer racer = run.Racer;
                racer.Distance = run.Distance;
                racer.Coins = run.Coins;
                racer.Score = (long)run.Score;
                racer.Done = run.Done;
                ranking.Add(racer);
            }
            RaceStandings.Rank(ranking);
        }

        /// <summary>The runs that share the first place (more than one is a tie); <see cref="Rank"/> first.</summary>
        public List<RunnerRun> Winners()
        {
            var winners = new List<RunnerRun>();
            foreach (RunnerRun run in runs)
            {
                if (run.Racer.Place == 1)
                {
                    winners.Add(run);
                }
            }
            return winners;
        }
    }
}
