using System.Collections.Generic;
using Gamebox.Racing;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// The rules of a race that need no scene, so they can be tested with made-up racers: the order of the grid, when a
    /// race is over and how its result is drawn up.
    ///
    /// The end: once every person at this device has finished or been wrecked, the computer's cars still running get
    /// <see cref="ComputerGrace"/> seconds to finish (or the race ends as soon as all of them have). A race without people
    /// (the cars behind the menu) ends the same way after its winner crosses the line, and in any case once
    /// <see cref="TimeLimit"/> runs out, so it never goes on for ever. The places: whoever finished by their time, then
    /// whoever is still running by how far they got, then the wrecked by how far they got.
    /// </summary>
    public static class RaceRules
    {
        /// <summary>The seconds the computer's cars get once the people are done.</summary>
        public const float ComputerGrace = 12f;

        /// <summary>How one car stands, for the end rules.</summary>
        public struct Runner
        {
            /// <summary>A person at this device drives it.</summary>
            public bool Human;
            public bool Finished;
            /// <summary>Wrecked.</summary>
            public bool Out;

            public bool Done => Finished || Out;
        }

        /// <summary>One car's whole race, for the result.</summary>
        public struct Standing
        {
            public RaceEntrant Entrant;
            public bool Finished;
            public bool Out;
            public float FinishTime;
            public float OutTime;
            public float Progress;
            public float BestLap;
            public int LapsCompleted;
            public int Kills;
            public int CashPicked;
            public float HealthLeft;
            public int MinesUsed;
            public int RocketsUsed;
            public int NitroUsed;
        }

        /// <summary>
        /// The grid from pole to last: the order of the seats, except that in a career, quick or split screen race the
        /// people start behind the computer's cars and have to fight their way through.
        /// </summary>
        public static List<RaceEntrant> GridOrder(RaceSetup setup)
        {
            var order = new List<RaceEntrant>(setup.Entrants);
            order.Sort((a, b) => a.Seat.CompareTo(b.Seat));
            bool humansBack = setup.Mode == RaceMode.Career || setup.Mode == RaceMode.Quick || setup.Mode == RaceMode.Local;
            if (!humansBack)
            {
                return order;
            }
            var grid = new List<RaceEntrant>(order.Count);
            foreach (RaceEntrant entrant in order)
            {
                if (!entrant.IsHuman)
                {
                    grid.Add(entrant);
                }
            }
            foreach (RaceEntrant entrant in order)
            {
                if (entrant.IsHuman)
                {
                    grid.Add(entrant);
                }
            }
            return grid;
        }

        /// <summary>
        /// How long a race without people may run at most: the laps at a crawl of 8 m/s plus half a minute (only a race of
        /// stuck cars gets near it).
        /// </summary>
        public static float TimeLimit(float trackLength, int laps)
        {
            return trackLength * Mathf.Max(1, laps) / 8f + 30f;
        }

        /// <summary>
        /// The phase after this moment of a race in <paramref name="phase"/> (Racing or Finishing): <paramref name="since"/>
        /// is when Finishing began (set when the phase turns to it); <paramref name="limit"/> (0 = none) ends it outright.
        /// </summary>
        public static RacePhase Next(RacePhase phase, IReadOnlyList<Runner> runners, float time, ref float since, float limit = 0f)
        {
            if (phase != RacePhase.Racing && phase != RacePhase.Finishing)
            {
                return phase;
            }
            bool anyHuman = false;
            bool humansDone = true;
            bool allDone = true;
            bool anyFinished = false;
            foreach (Runner runner in runners)
            {
                allDone &= runner.Done;
                anyFinished |= runner.Finished;
                if (runner.Human)
                {
                    anyHuman = true;
                    humansDone &= runner.Done;
                }
            }
            if (allDone || (limit > 0f && time >= limit))
            {
                return RacePhase.Over;
            }
            bool peopleDone = anyHuman ? humansDone : anyFinished;
            if (phase == RacePhase.Racing)
            {
                if (!peopleDone)
                {
                    return RacePhase.Racing;
                }
                since = time;
                return RacePhase.Finishing;
            }
            return time - since >= ComputerGrace ? RacePhase.Over : RacePhase.Finishing;
        }

        /// <summary>The places (from 1) of <paramref name="standings"/>, in their order.</summary>
        public static int[] Places(IReadOnlyList<Standing> standings)
        {
            var entries = new List<RaceRanking.Entry>(standings.Count);
            foreach (Standing standing in standings)
            {
                entries.Add(new RaceRanking.Entry
                {
                    Id = standing.Entrant != null ? standing.Entrant.Seat : entries.Count,
                    Finished = standing.Finished,
                    FinishTime = standing.FinishTime,
                    Out = standing.Out && !standing.Finished,
                    Progress = standing.Progress
                });
            }
            return RaceRanking.Places(entries);
        }

        /// <summary>The result of a race that ended at <paramref name="endTime"/>: every car's race, first to last.</summary>
        public static RaceResult Result(RaceSetup setup, IReadOnlyList<Standing> standings, float endTime)
        {
            int[] places = Places(standings);
            var racers = new List<RacerResult>(standings.Count);
            for (int i = 0; i < standings.Count; i++)
            {
                Standing standing = standings[i];
                RaceEntrant entrant = standing.Entrant ?? new RaceEntrant();
                bool wrecked = standing.Out && !standing.Finished;
                racers.Add(new RacerResult
                {
                    Seat = entrant.Seat,
                    Name = entrant.Name,
                    Color = entrant.Color,
                    Driver = entrant.Driver,
                    RivalId = entrant.RivalId,
                    Place = places[i],
                    Finished = standing.Finished,
                    Wrecked = wrecked,
                    Time = standing.Finished ? standing.FinishTime : wrecked ? Mathf.Max(0f, standing.OutTime) : Mathf.Max(0f, endTime),
                    BestLap = standing.BestLap,
                    LapsCompleted = standing.LapsCompleted,
                    Kills = standing.Kills,
                    CashPicked = standing.CashPicked,
                    HealthLeft = Mathf.Clamp01(standing.HealthLeft),
                    MinesUsed = standing.MinesUsed,
                    RocketsUsed = standing.RocketsUsed,
                    NitroUsed = standing.NitroUsed
                });
            }
            racers.Sort((a, b) => a.Place.CompareTo(b.Place));
            return new RaceResult { Setup = setup, Racers = racers };
        }
    }
}
