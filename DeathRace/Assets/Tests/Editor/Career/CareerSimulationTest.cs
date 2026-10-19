using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Portfolio.DeathRace.Tests
{
    /// <summary>
    /// Whole careers played by an average player (<see cref="CareerSimulation"/>): the tuning of <see cref="CareerRules"/>
    /// should bring them to the top of the ladder in about 25 to 40 races, with a car or two bought on the way, money
    /// short at first and plentiful later.
    /// </summary>
    public sealed class CareerSimulationTest
    {
        private const int Seeds = 12;
        private const int Days = 60;

        [Test]
        public void AnAverageCareerReachesTheTopInASensibleNumberOfRaces()
        {
            var tops = new List<int>();
            foreach (int seed in Enumerable.Range(1, Seeds))
            {
                var simulation = new CareerSimulation(seed * 101);
                for (int day = 0; day < Days && simulation.RacesToTitle < 0; day++)
                {
                    simulation.Day();
                }
                Console.WriteLine($"CareerSim seed {seed}: top after {simulation.RacesToTop} races, title after {simulation.RacesToTitle}, " +
                    $"cars {string.Join(", ", simulation.Cars.Select(car => $"{car.car}@{car.race}"))}, money {simulation.State.wallet.Money}, " +
                    $"earned {simulation.State.earned}, points {simulation.State.points}, wins {simulation.State.wins}/{simulation.State.races}");
                Console.WriteLine($"CareerSim   spent repairs {simulation.SpentRepairs}, market {simulation.SpentMarket}, upgrades {simulation.SpentUpgrades}, cars {simulation.SpentCars}; {simulation.RaceSummary()}");
                tops.Add(simulation.RacesToTop < 0 ? 999 : simulation.RacesToTop);
            }
            tops.Sort();
            int median = tops[tops.Count / 2];
            Console.WriteLine($"CareerSim median races to the top: {median} (all: {string.Join(", ", tops)})");
            Assert.That(median, Is.InRange(25, 40), "the median career reaches first place in 25 to 40 races");
            Assert.That(tops.Count(top => top < 999), Is.GreaterThanOrEqualTo(Seeds * 3 / 4), "most careers reach the top within 60 days");
        }

        [Test]
        public void MoneyIsTightEarlyAndPlentifulLater()
        {
            var early = new List<double>();
            var late = new List<double>();
            var earlyCosts = new List<double>();
            foreach (int seed in Enumerable.Range(1, Seeds))
            {
                var simulation = new CareerSimulation(seed * 37);
                for (int race = 1; race <= 40; race++)
                {
                    int spent = simulation.SpentRepairs + simulation.SpentMarket;
                    CareerReport report = simulation.Day();
                    if (race <= 6)
                    {
                        early.Add(report.Earned);
                        earlyCosts.Add(simulation.SpentRepairs + simulation.SpentMarket - spent);
                    }
                    else if (race > 30)
                    {
                        late.Add(report.Earned);
                    }
                }
            }
            double earlyAverage = early.Average();
            double lateAverage = late.Average();
            Console.WriteLine($"CareerSim earned per race: races 1-6 {earlyAverage:0} (repairs and restocking {earlyCosts.Average():0}), races 31-40 {lateAverage:0}");
            Assert.That(earlyAverage, Is.LessThan(2500), "an early race pays for a part or two, not a car");
            Assert.That(earlyCosts.Average(), Is.GreaterThan(earlyAverage * 0.15), "repairs and restocking eat a good share of it");
            Assert.That(lateAverage, Is.GreaterThan(earlyAverage * 1.75), "a late race pays far more than an early one");
        }

        [Test]
        public void TheLadderLeaderKeepsMovingAwayFromTheBottom()
        {
            var simulation = new CareerSimulation(5);
            for (int day = 0; day < 20; day++)
            {
                simulation.Day();
            }
            List<LadderEntry> ladder = Ladder.Entries(simulation.State);
            LadderEntry top = ladder.First(entry => !entry.IsPlayer);
            LadderEntry bottom = ladder.Last(entry => !entry.IsPlayer);
            Console.WriteLine($"CareerSim after 20 days: best rival {top.Name} {top.Points}, worst {bottom.Name} {bottom.Points}, player #{Ladder.Rank(simulation.State)} {simulation.State.points}");
            Assert.That(top.Points, Is.GreaterThan(CareerRules.TopRivalStartPoints), "the best rivals score too");
            Assert.That(top.Points - bottom.Points, Is.GreaterThan(CareerRules.TopRivalStartPoints), "the ladder spreads out");
        }
    }
}
