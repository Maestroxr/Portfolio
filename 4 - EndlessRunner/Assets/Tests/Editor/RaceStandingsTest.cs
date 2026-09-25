using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Portfolio.EndlessRunner.Tests
{
    public class RaceStandingsTest
    {
        private static Racer Runner(int seat, long score)
        {
            return new Racer { Seat = seat, Name = $"P{seat + 1}", Score = score };
        }

        [Test]
        public void TheHigherScoreIsAhead()
        {
            var racers = new List<Racer> { Runner(0, 300), Runner(1, 520), Runner(2, 410) };
            RaceStandings.Rank(racers);
            CollectionAssert.AreEqual(new[] { 1, 2, 0 }, racers.Select(racer => racer.Seat));
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, racers.Select(racer => racer.Place));
        }

        [Test]
        public void EqualScoresShareAPlaceAndTheLowerSeatIsListedFirst()
        {
            // The rule of the base server, so the scoreboard of a race and the results of the room agree.
            var racers = new List<Racer> { Runner(3, 100), Runner(1, 400), Runner(0, 400), Runner(2, 100) };
            RaceStandings.Rank(racers);
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, racers.Select(racer => racer.Seat));
            CollectionAssert.AreEqual(new[] { 1, 1, 3, 3 }, racers.Select(racer => racer.Place));
        }

        [Test]
        public void ARunnerAloneIsFirst()
        {
            var racers = new List<Racer> { Runner(2, 0) };
            RaceStandings.Rank(racers);
            Assert.AreEqual(1, racers[0].Place);
        }

        [Test]
        public void ARunIsWorthItsMetersAndTheCoinsOnTop()
        {
            Assert.AreEqual(0, RaceStandings.Score(0f, 0, 10));
            Assert.AreEqual(419, RaceStandings.Score(419.9f, 0, 10));
            Assert.AreEqual(419 + 570, RaceStandings.Score(419.9f, 57, 10));
            Assert.AreEqual(30, RaceStandings.Score(-2.5f, 3, 10), "the fourth runner starts behind the line");
        }
    }
}
