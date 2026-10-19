using NUnit.Framework;

namespace Portfolio.DeathRace.Tests
{
    /// <summary>The numbers of <see cref="CareerRules"/>: prices, prizes, points, trade-ins and repairs.</summary>
    public sealed class CareerRulesTest
    {
        [Test]
        public void UpgradesCostByTierAndLevelRoundedToFifty()
        {
            Assert.AreEqual(450, CareerRules.UpgradePrice(UpgradeKind.Engine, 1, 1));
            Assert.AreEqual(1250, CareerRules.UpgradePrice(UpgradeKind.Engine, 1, 2), "450 * 2^1.5 = 1273");
            Assert.AreEqual(800, CareerRules.UpgradePrice(UpgradeKind.Tires, 2, 1), "320 * 2^1.3 = 788");
            Assert.AreEqual(11950, CareerRules.UpgradePrice(UpgradeKind.Armor, 4, 3), "380 * 4^1.3 * 3^1.5 = 11972");
            Assert.AreEqual(0, CareerRules.UpgradePrice(UpgradeKind.Engine, 3, 0));
            Assert.AreEqual(450 + 1250, CareerRules.UpgradesCost(UpgradeKind.Engine, 1, 2));
            foreach (UpgradeKind kind in CareerRules.Upgrades)
            {
                for (int tier = 1; tier <= 6; tier++)
                {
                    for (int level = 1; level <= 5; level++)
                    {
                        int price = CareerRules.UpgradePrice(kind, tier, level);
                        Assert.AreEqual(0, price % CareerRules.PriceStep);
                        Assert.Greater(price, CareerRules.UpgradePrice(kind, tier, level - 1), "every level costs more than the one before");
                    }
                }
            }
        }

        [Test]
        public void EquipmentRepairsAndSabotageHaveTheirPrices()
        {
            Assert.AreEqual(900, CareerRules.SpikesPrice(1));
            Assert.AreEqual(1550, CareerRules.SpikesPrice(2), "900 * 2^0.8 = 1567");
            Assert.AreEqual(4250, CareerRules.PiercingPrice(4), "1400 * 4^0.8 = 4244");
            Assert.AreEqual(3, CareerRules.RepairPrice(1));
            Assert.AreEqual(15, CareerRules.RepairPrice(5));
            Assert.AreEqual(400, CareerRules.SabotagePrice(Difficulty.Easy));
            Assert.AreEqual(1200, CareerRules.SabotagePrice(Difficulty.Hard));
            Assert.AreEqual(1600, CareerRules.SabotagePrice(Difficulty.Showdown));
        }

        [Test]
        public void TheDealerGivesHalfThePriceAndHalfWhatWasSpent()
        {
            var stock = new CarLoadout { model = CarModel.Rustbucket };
            Assert.AreEqual(0, CareerRules.TradeIn(stock), "the Rustbucket was free");
            var hornet = new CarLoadout { model = CarModel.Hornet, engine = 1 };
            Assert.AreEqual(1100, CareerRules.Invested(hornet));
            Assert.AreEqual(3050, CareerRules.TradeIn(hornet), "(5000 + 1100) / 2");
            hornet.spikes = true;
            Assert.AreEqual(1100 + 1550, CareerRules.Invested(hornet), "equipment counts as money spent on the car");
        }

        [Test]
        public void PrizesAndPointsGoByPlaceAndDifficulty()
        {
            Assert.AreEqual(1000, CareerRules.Prize(Difficulty.Easy, 1));
            Assert.AreEqual(0, CareerRules.Prize(Difficulty.Easy, 5), "nothing past fourth");
            Assert.AreEqual(30000, CareerRules.Prize(Difficulty.Showdown, 1));
            Assert.AreEqual(0, CareerRules.Prize(Difficulty.Showdown, 2), "the Showdown pays the winner only");
            Assert.AreEqual(3, CareerRules.Points(Difficulty.Easy, 1));
            Assert.AreEqual(10, CareerRules.Points(Difficulty.Hard, 1));
            Assert.AreEqual(0, CareerRules.Points(Difficulty.Showdown, 1), "the Showdown is for the title, not points");
            foreach (Difficulty difficulty in new[] { Difficulty.Easy, Difficulty.Medium, Difficulty.Hard })
            {
                for (int place = 2; place <= 4; place++)
                {
                    Assert.LessOrEqual(CareerRules.Prize(difficulty, place), CareerRules.Prize(difficulty, place - 1));
                    Assert.LessOrEqual(CareerRules.Points(difficulty, place), CareerRules.Points(difficulty, place - 1));
                }
            }
            Assert.Less(CareerRules.Bounty(Difficulty.Easy), CareerRules.Bounty(Difficulty.Hard));
            Assert.Less(CareerRules.CashPerPickup(Difficulty.Easy), CareerRules.CashPerPickup(Difficulty.Hard));
            Assert.Greater(CareerRules.Prize(Difficulty.Hard, 1), CareerRules.Prize(Difficulty.Medium, 1));
            Assert.Greater(CareerRules.Prize(Difficulty.Medium, 1), CareerRules.Prize(Difficulty.Easy, 1));
        }

        [Test]
        public void TheRivalsStartSpreadOverTheLadder()
        {
            Assert.AreEqual(0, CareerRules.StartPoints(0));
            Assert.AreEqual(CareerRules.TopRivalStartPoints, CareerRules.StartPoints(CareerRules.RivalCount - 1));
            Assert.AreEqual(1, CareerRules.StartTier(0));
            Assert.AreEqual(CareerRules.MaxRivalTier, CareerRules.StartTier(CareerRules.RivalCount - 1));
            for (int i = 1; i < CareerRules.RivalCount; i++)
            {
                Assert.GreaterOrEqual(CareerRules.StartPoints(i), CareerRules.StartPoints(i - 1));
                Assert.GreaterOrEqual(CareerRules.StartTier(i), CareerRules.StartTier(i - 1));
                Assert.Greater(CareerRules.StartSkill(i), CareerRules.StartSkill(i - 1));
            }
            Assert.AreEqual(CareerRules.StartTier(3), CareerRules.TierFor(3, CareerRules.StartPoints(3) + CareerRules.PointsPerTier - 1));
            Assert.AreEqual(CareerRules.StartTier(3) + 1, CareerRules.TierFor(3, CareerRules.StartPoints(3) + CareerRules.PointsPerTier));
            Assert.AreEqual(CareerRules.MaxRivalTier, CareerRules.TierFor(0, 10000), "nobody but The Warden drives a Tyrant");
        }

        [Test]
        public void TheLadderOpensTracksAsThePlayerClimbs()
        {
            Assert.AreEqual(CareerRules.StartTracks, CareerRules.TracksOpenAt(20));
            Assert.AreEqual(CareerRules.StartTracks + 1, CareerRules.TracksOpenAt(16));
            Assert.AreEqual(CareerRules.ShowdownTrack, CareerRules.TracksOpenAt(1), "every ordinary track is open at the top");
            Assert.AreEqual(0, CareerRules.RankOpening(0));
            Assert.AreEqual(16, CareerRules.RankOpening(CareerRules.StartTracks));
        }

        [Test]
        public void EasyRacesAreALapShorter()
        {
            Assert.AreEqual(3, CareerRules.Laps(Difficulty.Easy, 4));
            Assert.AreEqual(4, CareerRules.Laps(Difficulty.Medium, 4));
            Assert.AreEqual(5, CareerRules.Laps(Difficulty.Hard, 5));
            Assert.AreEqual(3, CareerRules.Laps(Difficulty.Easy, 3), "never under three");
        }

        [Test]
        public void ComputerCarsGrowWithTheSeason()
        {
            CarLoadout early = Career.AiLoadout(4, 1, Difficulty.Medium, null);
            CarLoadout late = Career.AiLoadout(4, 40, Difficulty.Medium, null);
            Assert.AreEqual(CarModel.Viper, early.model);
            Assert.AreEqual(0, early.engine);
            Assert.Greater(late.engine, early.engine);
            Assert.IsFalse(early.spikes);
            Assert.IsTrue(late.spikes && late.piercing);
            Assert.Greater(Career.AiLoadout(4, 1, Difficulty.Hard, null).engine, early.engine, "a Hard race brings better cars");
            CarLoadout warden = Career.WardenLoadout();
            Assert.AreEqual(CarModel.Tyrant, warden.model);
            Assert.AreEqual(CarSpecs.Of(CarModel.Tyrant).MaxLevelOf(UpgradeKind.Engine), warden.engine);
            Assert.AreEqual(1f, Career.CarRating(warden), 1e-4f);
            Assert.AreEqual(0f, Career.CarRating(new CarLoadout()), 1e-4f);
        }
    }
}
