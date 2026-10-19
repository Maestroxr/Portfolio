using System.Collections.Generic;
using NUnit.Framework;

namespace Portfolio.DeathRace.Tests
{
    /// <summary>When a race ends, how its places fall and what its result says, with made-up racers.</summary>
    public class RaceRulesTest
    {
        private static RaceRules.Runner Runner(bool human, bool finished = false, bool wrecked = false)
        {
            return new RaceRules.Runner { Human = human, Finished = finished, Out = wrecked };
        }

        [Test]
        public void TheRaceRunsUntilThePeopleAreDone()
        {
            float since = 0f;
            var runners = new List<RaceRules.Runner> { Runner(true), Runner(false, finished: true), Runner(false), Runner(false) };
            Assert.AreEqual(RacePhase.Racing, RaceRules.Next(RacePhase.Racing, runners, 50f, ref since), "a computer car finishing first does not end it");
            runners[0] = Runner(true, finished: true);
            Assert.AreEqual(RacePhase.Finishing, RaceRules.Next(RacePhase.Racing, runners, 60f, ref since));
            Assert.AreEqual(60f, since);
        }

        [Test]
        public void TheComputerCarsGetTwelveSecondsOnceThePeopleAreDone()
        {
            float since = 60f;
            var runners = new List<RaceRules.Runner> { Runner(true, wrecked: true), Runner(false), Runner(false, finished: true) };
            Assert.AreEqual(RacePhase.Finishing, RaceRules.Next(RacePhase.Finishing, runners, 60f + RaceRules.ComputerGrace - 0.1f, ref since));
            Assert.AreEqual(RacePhase.Over, RaceRules.Next(RacePhase.Finishing, runners, 60f + RaceRules.ComputerGrace, ref since));
        }

        [Test]
        public void TheRaceEndsAtOnceWhenEveryCarIsDone()
        {
            float since = 0f;
            var runners = new List<RaceRules.Runner> { Runner(true, finished: true), Runner(false, wrecked: true), Runner(false, finished: true) };
            Assert.AreEqual(RacePhase.Over, RaceRules.Next(RacePhase.Racing, runners, 80f, ref since));
        }

        [Test]
        public void ARaceWithoutPeopleEndsAfterItsWinnerAndNeverRunsForEver()
        {
            float since = 0f;
            var runners = new List<RaceRules.Runner> { Runner(false), Runner(false), Runner(false) };
            Assert.AreEqual(RacePhase.Racing, RaceRules.Next(RacePhase.Racing, runners, 10f, ref since, 500f));
            runners[1] = Runner(false, finished: true);
            Assert.AreEqual(RacePhase.Finishing, RaceRules.Next(RacePhase.Racing, runners, 90f, ref since, 500f));
            Assert.AreEqual(RacePhase.Over, RaceRules.Next(RacePhase.Finishing, runners, 90f + RaceRules.ComputerGrace, ref since, 500f));
            runners[1] = Runner(false);
            Assert.AreEqual(RacePhase.Over, RaceRules.Next(RacePhase.Racing, runners, 500f, ref since, 500f), "stuck cars end at the limit");
            Assert.Greater(RaceRules.TimeLimit(800f, 2), 800f * 2f / 40f);
        }

        [Test]
        public void TheGridAndTheOverAreLeftAlone()
        {
            float since = 0f;
            var runners = new List<RaceRules.Runner> { Runner(true, finished: true) };
            Assert.AreEqual(RacePhase.Countdown, RaceRules.Next(RacePhase.Countdown, runners, 0f, ref since));
            Assert.AreEqual(RacePhase.Over, RaceRules.Next(RacePhase.Over, runners, 0f, ref since));
        }

        [Test]
        public void FinishersComeFirstByTimeThenRunnersByProgressThenWrecksByProgress()
        {
            var standings = new List<RaceRules.Standing>
            {
                Standing(0, progress: 900f, wrecked: true, outTime: 40f),
                Standing(1, progress: 1200f),
                Standing(2, finished: true, finishTime: 95f, progress: 2400f),
                Standing(3, progress: 500f, wrecked: true, outTime: 10f),
                Standing(4, finished: true, finishTime: 90f, progress: 2400f),
                Standing(5, progress: 1300f)
            };
            int[] places = RaceRules.Places(standings);
            CollectionAssert.AreEqual(new[] { 5, 4, 2, 6, 1, 3 }, places);
        }

        [Test]
        public void ACarWreckedAfterTheFlagKeepsItsFinish()
        {
            var standings = new List<RaceRules.Standing>
            {
                Standing(0, finished: true, finishTime: 100f, progress: 2400f, wrecked: true, outTime: 104f),
                Standing(1, progress: 2300f)
            };
            RaceResult result = RaceRules.Result(new RaceSetup(), standings, 110f);
            RacerResult first = result.Racers[0];
            Assert.AreEqual(0, first.Seat);
            Assert.AreEqual(1, first.Place);
            Assert.IsTrue(first.Finished);
            Assert.IsFalse(first.Wrecked);
            Assert.AreEqual(100f, first.Time);
        }

        [Test]
        public void TheResultCarriesEveryCarsRaceInPlaceOrder()
        {
            var setup = new RaceSetup { Mode = RaceMode.Career, Track = 3, Laps = 4 };
            var standings = new List<RaceRules.Standing>
            {
                Standing(0, progress: 1000f, wrecked: true, outTime: 37.5f),
                Standing(1, finished: true, finishTime: 120f, progress: 2400f)
            };
            RaceRules.Standing filled = standings[1];
            filled.BestLap = 28.4f;
            filled.LapsCompleted = 4;
            filled.Kills = 1;
            filled.CashPicked = 150;
            filled.HealthLeft = 0.4f;
            filled.MinesUsed = 2;
            filled.RocketsUsed = 1;
            filled.NitroUsed = 3;
            filled.OilUsed = 2;
            filled.EmpUsed = 1;
            filled.MortarUsed = 3;
            filled.Entrant.RivalId = "rival.7";
            standings[1] = filled;
            RaceResult result = RaceRules.Result(setup, standings, 130f);
            Assert.AreSame(setup, result.Setup);
            Assert.AreEqual(2, result.Racers.Count);
            RacerResult winner = result.Racers[0];
            Assert.AreEqual(1, winner.Seat);
            Assert.AreEqual(1, winner.Place);
            Assert.AreEqual("Car 1", winner.Name);
            Assert.AreEqual(DriverKind.Computer, winner.Driver);
            Assert.AreEqual("rival.7", winner.RivalId);
            Assert.AreEqual(120f, winner.Time);
            Assert.AreEqual(28.4f, winner.BestLap);
            Assert.AreEqual(4, winner.LapsCompleted);
            Assert.AreEqual(1, winner.Kills);
            Assert.AreEqual(150, winner.CashPicked);
            Assert.AreEqual(0.4f, winner.HealthLeft, 1e-5f);
            Assert.AreEqual(2, winner.MinesUsed);
            Assert.AreEqual(1, winner.RocketsUsed);
            Assert.AreEqual(3, winner.NitroUsed);
            Assert.AreEqual(2, winner.OilUsed);
            Assert.AreEqual(1, winner.EmpUsed);
            Assert.AreEqual(3, winner.MortarUsed);
            Assert.AreEqual(2, winner.SpecialsUsed(SpecialKind.Oil));
            Assert.AreEqual(1, winner.SpecialsUsed(SpecialKind.Emp));
            Assert.AreEqual(3, winner.SpecialsUsed(SpecialKind.Mortar));
            RacerResult wreck = result.Of(0);
            Assert.AreEqual(2, wreck.Place);
            Assert.IsTrue(wreck.Wrecked);
            Assert.IsFalse(wreck.Finished);
            Assert.AreEqual(37.5f, wreck.Time, "a wreck's time is when it was wrecked");
        }

        [Test]
        public void ARunnerStillGoingAtTheEndGetsTheEndTime()
        {
            var standings = new List<RaceRules.Standing> { Standing(0, progress: 300f) };
            RaceResult result = RaceRules.Result(new RaceSetup(), standings, 77f);
            Assert.AreEqual(77f, result.Racers[0].Time);
            Assert.IsFalse(result.Racers[0].Finished);
            Assert.IsFalse(result.Racers[0].Wrecked);
        }

        [Test]
        public void PeopleStartAtTheBackOfACareerOrQuickRaceAndInSeatOrderOnline()
        {
            RaceSetup setup = SetupWith(RaceMode.Quick, DriverKind.Human, DriverKind.Computer, DriverKind.Computer, DriverKind.Computer);
            List<RaceEntrant> grid = RaceRules.GridOrder(setup);
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 0 }, grid.ConvertAll(entrant => entrant.Seat));

            setup = SetupWith(RaceMode.Local, DriverKind.Human, DriverKind.Human, DriverKind.Computer, DriverKind.Computer);
            CollectionAssert.AreEqual(new[] { 2, 3, 0, 1 }, RaceRules.GridOrder(setup).ConvertAll(entrant => entrant.Seat));

            setup = SetupWith(RaceMode.Online, DriverKind.Remote, DriverKind.Human, DriverKind.Remote);
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, RaceRules.GridOrder(setup).ConvertAll(entrant => entrant.Seat));

            setup = SetupWith(RaceMode.Attract, DriverKind.Computer, DriverKind.Computer);
            CollectionAssert.AreEqual(new[] { 0, 1 }, RaceRules.GridOrder(setup).ConvertAll(entrant => entrant.Seat));
        }

        private static RaceSetup SetupWith(RaceMode mode, params DriverKind[] drivers)
        {
            var setup = new RaceSetup { Mode = mode };
            // Added out of order on purpose: the grid goes by seat.
            for (int seat = drivers.Length - 1; seat >= 0; seat--)
            {
                setup.Entrants.Add(new RaceEntrant { Seat = seat, Driver = drivers[seat] });
            }
            return setup;
        }

        private static RaceRules.Standing Standing(int seat, bool finished = false, float finishTime = 0f, float progress = 0f, bool wrecked = false, float outTime = -1f)
        {
            return new RaceRules.Standing
            {
                Entrant = new RaceEntrant { Seat = seat, Name = $"Car {seat}" },
                Finished = finished,
                FinishTime = finishTime,
                Progress = progress,
                Out = wrecked,
                OutTime = outTime
            };
        }
    }
}
