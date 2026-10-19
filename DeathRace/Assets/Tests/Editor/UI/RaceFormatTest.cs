using NUnit.Framework;

namespace Portfolio.DeathRace.Tests
{
    /// <summary>How the windows and the HUD write money, times, ranks and shares (<see cref="RaceFormat"/>), and the stat bars (<see cref="CarBars"/>).</summary>
    public sealed class RaceFormatTest
    {
        [Test]
        public void MoneyHasThousandsSeparatorsAndASign()
        {
            Assert.AreEqual("$0", RaceFormat.Money(0));
            Assert.AreEqual("$1,500", RaceFormat.Money(1500));
            Assert.AreEqual("$9,999,999", RaceFormat.Money(9999999));
            Assert.AreEqual("-$450", RaceFormat.Money(-450));
        }

        [Test]
        public void TimesAreMinutesSecondsAndHundredths()
        {
            Assert.AreEqual(RaceFormat.NoTime, RaceFormat.Time(0f));
            Assert.AreEqual(RaceFormat.NoTime, RaceFormat.Time(float.NaN));
            Assert.AreEqual("0:05.07", RaceFormat.Time(5.07f));
            Assert.AreEqual("1:02.34", RaceFormat.Time(62.34f));
            Assert.AreEqual("12:00.00", RaceFormat.Time(720f));
            Assert.AreEqual("1:00.00", RaceFormat.Time(59.999f), "rounded to the hundredth, carried into the minute");
        }

        [Test]
        public void TheHudWritesTimesWithoutStrings()
        {
            var buffer = new char[16];
            foreach (float seconds in new[] { 0.01f, 5.07f, 62.34f, 599.99f, 720f, 59.999f })
            {
                int length = RaceFormat.TimeChars(seconds, buffer);
                Assert.AreEqual(RaceFormat.Time(seconds), new string(buffer, 0, length), seconds.ToString());
            }
            Assert.AreEqual("0:00.00", new string(buffer, 0, RaceFormat.TimeChars(0f, buffer)), "the clock before the start");
        }

        [Test]
        public void RanksSharesAndCounts()
        {
            Assert.AreEqual("#7", RaceFormat.Rank(7));
            Assert.AreEqual("+3", RaceFormat.RankChange(10, 7));
            Assert.AreEqual("-2", RaceFormat.RankChange(5, 7));
            Assert.AreEqual("0", RaceFormat.RankChange(4, 4));
            Assert.AreEqual("46%", RaceFormat.Percent(0.456f));
            Assert.AreEqual("100%", RaceFormat.Percent(1.4f));
            Assert.AreEqual("3/8", RaceFormat.OutOf(3, 8));
            Assert.AreEqual("12,000", RaceFormat.Number(12000));
        }

        [Test]
        public void StatBarsAreSharesOfTheWardensCar()
        {
            CarStats warden = Career.WardenLoadout().Stats();
            Assert.AreEqual(1f, CarBars.Speed(warden), 1e-4f);
            Assert.AreEqual(1f, CarBars.Acceleration(warden), 1e-4f);
            Assert.AreEqual(1f, CarBars.Handling(warden), 1e-4f);
            Assert.AreEqual(1f, CarBars.Armour(warden), 1e-4f);
            CarStats rustbucket = new CarLoadout { model = CarModel.Rustbucket }.Stats();
            CarStats upgraded = CarLoadout.Upgraded(CarModel.Rustbucket, 2).Stats();
            foreach (System.Func<CarStats, float> bar in new System.Func<CarStats, float>[] { CarBars.Speed, CarBars.Acceleration, CarBars.Handling, CarBars.Armour })
            {
                Assert.That(bar(rustbucket), Is.InRange(0.1f, 0.9f));
                Assert.Greater(bar(upgraded), bar(rustbucket), "an upgrade lengthens its bar");
            }
        }
    }
}
