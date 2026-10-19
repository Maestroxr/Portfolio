using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Portfolio.DeathRace.Tests
{
    /// <summary>
    /// Whole careers of the later leagues played by an average player (<see cref="CareerSimulation"/>): the tuning of
    /// the leagues (<see cref="Leagues"/>) should make them champion in about 30 to 50 races, the Hellfire League a few
    /// races harder than the Iron League, with money short at first and plentiful later and the higher tiers' cars worth
    /// saving for (the title comes with the champion's tier). Every league fields better drivers in better cars, for
    /// bigger purses, than the one before. (The first league is the career as it always was: CareerSimulationTest.)
    /// </summary>
    public sealed class LeagueSimulationTest
    {
        private const int Seeds = 36;
        private const int Days = 90;

        private static readonly Dictionary<LeagueKind, List<CareerSimulation>> Played = new Dictionary<LeagueKind, List<CareerSimulation>>();

        /// <summary>The careers of <paramref name="league"/>, each played until its title (or <see cref="Days"/> days); played once for the tests that share them.</summary>
        private static List<CareerSimulation> Play(LeagueKind league)
        {
            if (Played.TryGetValue(league, out List<CareerSimulation> played))
            {
                return played;
            }
            var careers = new List<CareerSimulation>();
            foreach (int seed in Enumerable.Range(1, Seeds))
            {
                var simulation = new CareerSimulation(seed * 7717 + 3, league);
                for (int day = 0; day < Days && simulation.RacesToTitle < 0; day++)
                {
                    simulation.Day();
                }
                careers.Add(simulation);
            }
            foreach (CareerSimulation simulation in careers.Take(4))
            {
                Console.WriteLine($"LeagueSim {league}: top after {simulation.RacesToTop} races, title after {simulation.RacesToTitle}, " +
                    $"cars {string.Join(", ", simulation.Cars.Select(car => $"{car.car}@{car.race}"))}, money {simulation.State.wallet.Money}, " +
                    $"wins {simulation.State.wins}/{simulation.State.races}, showdowns lost {simulation.State.showdownsLost}; {simulation.RaceSummary()}");
            }
            Played[league] = careers;
            return careers;
        }

        private static int Median(IEnumerable<int> values)
        {
            List<int> sorted = values.OrderBy(value => value).ToList();
            return sorted[sorted.Count / 2];
        }

        private static int MedianTitle(LeagueKind league)
        {
            List<int> titles = Play(league).Select(career => career.RacesToTitle < 0 ? 999 : career.RacesToTitle).ToList();
            int median = Median(titles);
            Console.WriteLine($"LeagueSim {league} median races to the title: {median} (all: {string.Join(", ", titles.OrderBy(title => title))})");
            return median;
        }

        private static int MedianTop(LeagueKind league)
        {
            int median = Median(Play(league).Select(career => career.RacesToTop < 0 ? 999 : career.RacesToTop));
            Console.WriteLine($"LeagueSim {league} median races to the top of the ladder: {median}");
            return median;
        }

        [TestCase(LeagueKind.Iron)]
        [TestCase(LeagueKind.Hellfire)]
        public void ALeagueIsWonInThirtyToFiftyRaces(LeagueKind league)
        {
            Assert.That(MedianTitle(league), Is.InRange(30, 50), "the median career takes the title in 30 to 50 races");
            Assert.That(Play(league).Count(career => career.RacesToTitle > 0), Is.GreaterThanOrEqualTo(Seeds * 3 / 4), "most careers are won");
        }

        [Test]
        public void TheHellfireLeagueTakesLongerThanTheIronLeague()
        {
            Assert.Greater(MedianTitle(LeagueKind.Hellfire), MedianTitle(LeagueKind.Iron), "the title");
            Assert.Greater(MedianTop(LeagueKind.Hellfire), MedianTop(LeagueKind.Iron), "the top of the ladder");
        }

        [TestCase(LeagueKind.Iron)]
        [TestCase(LeagueKind.Hellfire)]
        public void MoneyIsTightEarlyAndPlentifulLaterAndTheTopTierIsWorthSavingFor(LeagueKind league)
        {
            var early = new List<double>();
            var late = new List<double>();
            var championTier = new List<int>();
            foreach (CareerSimulation simulation in Play(league))
            {
                for (int race = 0; race < simulation.Reports.Count; race++)
                {
                    if (race < 6)
                    {
                        early.Add(simulation.Reports[race].Earned);
                    }
                    else if (race >= 30 && race < 40)
                    {
                        late.Add(simulation.Reports[race].Earned);
                    }
                }
                // The race before which the career had a car of the champion's tier (the title follows it).
                int bought = simulation.Cars.FindIndex(car => CarSpecs.Of(car.car).Tier >= CarSpecs.Of(simulation.League.ChampionCar).Tier);
                championTier.Add(bought >= 0 ? simulation.Cars[bought].race : 999);
            }
            LeagueSpec spec = Leagues.Of(league);
            int nextCar = CarSpecs.Of(CarSpecs.OfTier(CarSpecs.Of(spec.StartCar).Tier + 1)).Price;
            double earlyAverage = early.Average();
            double lateAverage = late.Average();
            Console.WriteLine($"LeagueSim {league} earned per race: races 1-6 {earlyAverage:0}, races 31-40 {lateAverage:0}; " +
                $"a car of the champion's tier before race {Median(championTier)}");
            Assert.That(earlyAverage, Is.LessThan(nextCar * 0.5), "an early race pays for a part or two, not the next car");
            Assert.That(lateAverage, Is.GreaterThan(earlyAverage * 1.75), "a late race pays far more than an early one");
            Assert.That(Median(championTier), Is.InRange(15, 50), "a car of the champion's tier takes many races of saving");
        }

        [Test]
        public void EveryLeagueFieldsBetterDriversInBetterCarsForBiggerPursesThanTheOneBefore()
        {
            for (int i = 1; i < Leagues.Count; i++)
            {
                LeagueSpec before = Leagues.Of(i - 1);
                LeagueSpec league = Leagues.Of(i);
                string name = league.Name;
                Assert.Greater(CarSpecs.Of(league.StartCar).Tier, CarSpecs.Of(before.StartCar).Tier, name + ": a better car to start");
                Assert.Greater(league.MaxRivalTier, before.MaxRivalTier, name + ": better cars for the rivals");
                Assert.Greater(CarSpecs.Of(league.ChampionCar).Tier, CarSpecs.Of(before.ChampionCar).Tier, name + ": a better car for the champion");
                Assert.Greater(league.BottomSkill, before.BottomSkill, name + ": better drivers at the bottom");
                Assert.Greater(league.TopSkill, before.TopSkill, name + ": better drivers at the top");
                Assert.LessOrEqual(league.DaysPerAiUpgrade, before.DaysPerAiUpgrade, name + ": rivals who upgrade as fast or faster");
                for (int index = 0; index < CareerRules.RivalCount; index++)
                {
                    Assert.GreaterOrEqual(CareerRules.StartTier(league, index), CareerRules.StartTier(before, index), $"{name}: rival {index}'s car");
                }
                foreach (Difficulty difficulty in new[] { Difficulty.Easy, Difficulty.Medium, Difficulty.Hard, Difficulty.Showdown })
                {
                    Assert.GreaterOrEqual(CareerRules.AiSkill(league, difficulty, 0f), CareerRules.AiSkill(before, difficulty, 0f), $"{name}: {difficulty} drivers");
                    Assert.Greater(CareerRules.Prize(league, difficulty, 1), CareerRules.Prize(before, difficulty, 1), $"{name}: {difficulty} purse");
                    Assert.GreaterOrEqual(CareerRules.AiSpecials(league, difficulty), CareerRules.AiSpecials(before, difficulty), $"{name}: {difficulty} specials");
                }
                for (int day = 1; day <= 60; day += 10)
                {
                    foreach (Difficulty difficulty in new[] { Difficulty.Easy, Difficulty.Medium, Difficulty.Hard })
                    {
                        // The same driver in the same car of each league: the later league's car carries more.
                        CarLoadout earlier = Career.AiLoadout(before, 5, "nobody", day, difficulty, null);
                        CarLoadout later = Career.AiLoadout(league, 5, "nobody", day, difficulty, null);
                        Assert.GreaterOrEqual(Career.ExtraShare(later), Career.ExtraShare(earlier), $"{name}: day {day} {difficulty} fittings and guns");
                    }
                }
                Assert.AreEqual(league.MaxDealerTier, CarSpecs.Of(league.ChampionCar).Tier, name + ": the dealer sells the champion's tier at last");
            }
        }
    }
}
