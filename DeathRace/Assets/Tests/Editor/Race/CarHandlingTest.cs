using NUnit.Framework;

namespace Portfolio.DeathRace.Tests
{
    /// <summary>What the stats and the upgrades of a car do to its handling.</summary>
    public class CarHandlingTest
    {
        private static CarHandling Of(CarModel model, int engine = 0, int tires = 0)
        {
            return new CarHandling(new CarLoadout { model = model, engine = engine, tires = tires }.Stats());
        }

        [Test]
        public void ATyrantIsFasterQuickerAndHeavierThanARustbucket()
        {
            CarHandling rust = Of(CarModel.Rustbucket);
            CarHandling tyrant = Of(CarModel.Tyrant);
            Assert.Greater(tyrant.TopSpeed, rust.TopSpeed * 1.5f);
            Assert.Less(tyrant.TimeTo(0.9f) * rust.TopSpeed / tyrant.TopSpeed, rust.TimeTo(0.9f), "it gets to its (higher) speed in less time per m/s");
            Assert.Greater(tyrant.Mass, rust.Mass * 1.4f);
            Assert.Greater(tyrant.LateralGrip, rust.LateralGrip);
            Assert.Less(tyrant.YawResponse / tyrant.Steering, rust.YawResponse / rust.Steering, "weight slows its response");
            Assert.Greater(tyrant.CornerSpeed(1f / 40f), rust.CornerSpeed(1f / 40f));
        }

        [Test]
        public void TheEnginePullsHardAtFirstAndFadesToNothingAtTheTopSpeed()
        {
            CarHandling car = Of(CarModel.Viper);
            Assert.AreEqual(car.Acceleration, car.DriveAcceleration(0f, false), 1e-4f);
            Assert.Greater(car.DriveAcceleration(car.TopSpeed * 0.5f, false), car.Acceleration * 0.75f);
            Assert.AreEqual(0f, car.DriveAcceleration(car.TopSpeed, false), 1e-3f);
            Assert.Less(car.DriveAcceleration(car.TopSpeed * 1.2f, false), 0f, "over the top speed (after a nitro) it slows down");
            float toNinety = car.TimeTo(0.9f);
            Assert.Greater(toNinety, 2f);
            Assert.Less(toNinety, 5f);
        }

        [Test]
        public void NitroRaisesTheTopSpeedAndThePull()
        {
            CarHandling car = Of(CarModel.Hornet);
            Assert.AreEqual(car.TopSpeed * CarHandling.NitroTopSpeed, car.TopSpeedWith(true), 1e-4f);
            Assert.AreEqual(car.Acceleration * CarHandling.NitroAcceleration, car.DriveAcceleration(0f, true), 1e-4f);
            Assert.Greater(car.DriveAcceleration(car.TopSpeed, true), 0f, "with nitro it keeps pulling past the normal top speed");
        }

        [Test]
        public void EngineAndTyreUpgradesShowInTheHandling()
        {
            CarHandling stock = Of(CarModel.Bulldog);
            CarHandling engine = Of(CarModel.Bulldog, engine: 3);
            CarHandling tyres = Of(CarModel.Bulldog, tires: 3);
            Assert.AreEqual(stock.TopSpeed * (1f + 3 * CarStats.EnginePerLevelSpeed), engine.TopSpeed, 1e-3f);
            Assert.AreEqual(stock.Acceleration * (1f + 3 * CarStats.EnginePerLevelAcceleration), engine.Acceleration, 1e-3f);
            Assert.Greater(tyres.LateralGrip, stock.LateralGrip * 1.15f);
            Assert.Greater(tyres.MaxYawRate(20f), stock.MaxYawRate(20f));
            Assert.Greater(tyres.Brake, stock.Brake);
        }

        [Test]
        public void SteeringNeedsSpeedAndTightensLessTheFasterTheCarGoes()
        {
            CarHandling car = Of(CarModel.Rustbucket);
            Assert.AreEqual(0f, car.MaxYawRate(0f), 1e-5f, "no turning on the spot");
            Assert.Greater(car.MaxYawRate(8f), car.MaxYawRate(2f));
            Assert.Greater(car.MaxYawRate(10f), car.MaxYawRate(30f));
            Assert.AreEqual(car.MaxYawRate(-10f), car.MaxYawRate(10f), 1e-5f, "the same either way (the body flips its sign in reverse)");
            // At speed full lock asks for a little more than the tyres hold: the tail can be made to slide.
            float asked = car.MaxYawRate(30f) * 30f;
            Assert.Greater(asked, car.GripAt(30f, 0f));
            Assert.Less(asked, car.GripAt(30f, 0f) * 1.8f);
        }

        [Test]
        public void BrakingIntoATurnLetsTheTailGo()
        {
            CarHandling car = Of(CarModel.Viper);
            Assert.Less(car.GripAt(25f, 1f), car.GripAt(25f, 0f) * 0.6f);
            Assert.Less(car.GripAt(40f, 0f), car.GripAt(10f, 0f), "a little less grip at speed");
        }

        [Test]
        public void ReverseIsSlowAndThePedalsStopAtItsTopSpeed()
        {
            CarHandling car = Of(CarModel.Reaper);
            Assert.AreEqual(car.TopSpeed * CarHandling.ReverseShare, car.ReverseTopSpeed, 1e-4f);
            Assert.Greater(car.ReverseAccelerationAt(0f), 0f);
            Assert.AreEqual(0f, car.ReverseAccelerationAt(car.ReverseTopSpeed), 1e-4f);
        }

        [Test]
        public void TheEngineSoundClimbsThroughTheGearsAndDropsAtEachChange()
        {
            float low = CarAudio.EnginePitch(0.05f, 1f, false);
            float endOfFirst = CarAudio.EnginePitch(0.199f, 1f, false);
            float startOfSecond = CarAudio.EnginePitch(0.201f, 1f, false);
            Assert.Greater(endOfFirst, low);
            Assert.Less(startOfSecond, endOfFirst, "the change up drops the revs");
            Assert.Greater(CarAudio.EnginePitch(0f, 1f, true), CarAudio.EnginePitch(0f, 0f, true), "revving on the grid");
        }
    }
}
