using NUnit.Framework;

namespace Portfolio.DeathRace.Tests
{
    /// <summary>The stat bars of the windows (<see cref="CarBars"/>): shares of the best any car reaches, so no bar runs past its end.</summary>
    public class CarBarsTest
    {
        private static readonly System.Func<CarStats, float>[] Bars = { CarBars.Speed, CarBars.Acceleration, CarBars.Handling, CarBars.Armour };

        [Test]
        public void NoCarFullyUpgradedRunsPastTheEnd()
        {
            foreach (CarSpec spec in CarSpecs.All)
            {
                CarStats best = CarLoadout.Upgraded(spec.Model, 99).Stats();
                foreach (System.Func<CarStats, float> bar in Bars)
                {
                    Assert.LessOrEqual(bar(best), 1f + 1e-4f, spec.Name);
                }
            }
        }

        [Test]
        public void TheFastestCarFillsTheSpeedBar()
        {
            float fastest = 0f;
            foreach (CarSpec spec in CarSpecs.All)
            {
                fastest = System.Math.Max(fastest, CarBars.Speed(CarLoadout.Upgraded(spec.Model, 99).Stats()));
            }
            Assert.AreEqual(1f, fastest, 1e-4f);
            Assert.AreEqual(1f, CarBars.Speed(CarLoadout.Upgraded(CarModel.Apocalypse, 99).Stats()), 1e-4f, "the Apocalypse is the fastest");
        }

        [Test]
        public void AnUpgradeLengthensABarAndTheFirstCarIsShort()
        {
            CarStats rustbucket = new CarLoadout { model = CarModel.Rustbucket }.Stats();
            CarStats upgraded = CarLoadout.Upgraded(CarModel.Rustbucket, 2).Stats();
            foreach (System.Func<CarStats, float> bar in Bars)
            {
                Assert.That(bar(rustbucket), Is.InRange(0.1f, 0.9f));
                Assert.Greater(bar(upgraded), bar(rustbucket), "an upgrade lengthens its bar");
            }
            Assert.Greater(CarBars.Speed(new CarLoadout { model = CarModel.Spectre }.Stats()), CarBars.Speed(new CarLoadout { model = CarModel.Tyrant }.Stats()));
        }
    }
}
