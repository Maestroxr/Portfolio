using NUnit.Framework;
using UnityEngine;

namespace Portfolio.DeathRace.Tests
{
    /// <summary>
    /// A real car (the stand-in model with every part the factory adds) driven by its body in the physics engine, stepped
    /// by hand: it pulls away, turns the way the wheel says (the other way in reverse), stays put on the grid and rolls to
    /// rest when wrecked. Needs the engine (the Unity test runner, not the quick host).
    /// </summary>
    public class CarBodyTest
    {
        private const float Step = 0.02f;

        private SimulationMode previousMode;
        private Car car;

        [SetUp]
        public void SetUp()
        {
            previousMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
        }

        [TearDown]
        public void TearDown()
        {
            if (car != null)
            {
                Object.DestroyImmediate(car.gameObject);
            }
            Physics.simulationMode = previousMode;
        }

        private Car Spawn(CarModel model)
        {
            var entrant = new RaceEntrant { Seat = 0, Name = "Test", Driver = DriverKind.Computer, Loadout = new CarLoadout { model = model } };
            car = CarFactory.Spawn(entrant, new Pose(new Vector3(0f, 0f, -2000f), Quaternion.identity), null, null);
            return car;
        }

        private static void Drive(Car driven, CarInput input, float seconds)
        {
            for (float time = 0f; time < seconds; time += Step)
            {
                driven.Body.Step(input, Step);
                Physics.Simulate(Step);
            }
        }

        [Test]
        public void TheFactoryPutsEveryPartOnTheCar()
        {
            Car spawned = Spawn(CarModel.Bulldog);
            Assert.IsNotNull(spawned.Body);
            Assert.IsNotNull(spawned.Health);
            Assert.IsNotNull(spawned.Weapons);
            Assert.IsNotNull(spawned.View);
            Assert.IsNotNull(spawned.Audio);
            Assert.AreEqual(DeathRaceLayers.Car, spawned.gameObject.layer);
            Rigidbody body = spawned.Body.Rigidbody;
            Assert.AreEqual(CarSpecs.Of(CarModel.Bulldog).Mass, body.mass, 1e-3f);
            Assert.IsFalse(body.useGravity);
            Assert.AreEqual(RigidbodyInterpolation.Interpolate, body.interpolation);
            Assert.IsTrue((body.constraints & RigidbodyConstraints.FreezePositionY) != 0);
            Assert.AreEqual(CarSpecs.Of(CarModel.Bulldog).Size, spawned.Body.Collider.size);
            Assert.Greater(spawned.Body.Collider.center.y - spawned.Body.Collider.size.y * 0.5f, 0.05f, "the box stands off the road");
            Assert.AreEqual(1, spawned.GetComponentsInChildren<Collider>().Length, "only the box collides");
        }

        [Test]
        public void ItPullsAwayTowardsItsTopSpeed()
        {
            Car driven = Spawn(CarModel.Viper);
            Drive(driven, new CarInput { Throttle = 1f }, 4f);
            Assert.Greater(driven.Body.ForwardSpeed, driven.Stats.TopSpeed * 0.85f);
            Assert.LessOrEqual(driven.Body.ForwardSpeed, driven.Stats.TopSpeed + 0.1f);
            Assert.Greater(driven.Body.Rigidbody.position.z, -2000f + 50f, "it went forward (+z)");
            Assert.AreEqual(0f, driven.Body.Rigidbody.position.y, 1e-3f, "and stayed on the ground");
        }

        [Test]
        public void TheWheelTurnsItRightAndTheOtherWayInReverse()
        {
            Car driven = Spawn(CarModel.Rustbucket);
            Drive(driven, new CarInput { Throttle = 1f }, 1f);
            Drive(driven, new CarInput { Throttle = 1f, Steer = 1f }, 0.6f);
            Vector3 forward = driven.Body.Rigidbody.rotation * Vector3.forward;
            Assert.Greater(forward.x, 0.3f, "steering right turns the nose right (east)");

            Object.DestroyImmediate(driven.gameObject);
            Car backing = Spawn(CarModel.Rustbucket);
            Drive(backing, new CarInput { Throttle = -1f }, 1.5f);
            Assert.Less(backing.Body.ForwardSpeed, -3f, "it reverses");
            Drive(backing, new CarInput { Throttle = -1f, Steer = 1f }, 0.8f);
            forward = backing.Body.Rigidbody.rotation * Vector3.forward;
            Assert.Less(forward.x, -0.1f, "backing up with the wheel right swings the nose left");
        }

        [Test]
        public void ItCannotTurnStandingStill()
        {
            Car driven = Spawn(CarModel.Reaper);
            Drive(driven, new CarInput { Steer = 1f }, 1f);
            Vector3 forward = driven.Body.Rigidbody.rotation * Vector3.forward;
            Assert.AreEqual(1f, forward.z, 1e-3f);
        }

        [Test]
        public void TheBrakesStopItQuicklyAndThenItReverses()
        {
            Car driven = Spawn(CarModel.Hornet);
            Drive(driven, new CarInput { Throttle = 1f }, 3f);
            float speed = driven.Body.ForwardSpeed;
            float time = 0f;
            while (driven.Body.ForwardSpeed > 0.5f && time < 5f)
            {
                Drive(driven, new CarInput { Throttle = -1f }, Step);
                time += Step;
            }
            Assert.Less(time, speed / (driven.Body.Handling.Brake * 0.9f), "it stops about as fast as the brakes say");
            Assert.GreaterOrEqual(driven.Body.ForwardSpeed, 0f, "braking itself never throws it backwards");
            Drive(driven, new CarInput { Throttle = -1f }, 1.5f);
            Assert.Less(driven.Body.ForwardSpeed, -2f, "holding the brake once stopped reverses");
        }

        [Test]
        public void FullLockAtSpeedSlidesALittleAndTheCarCatchesIt()
        {
            Car driven = Spawn(CarModel.Viper);
            Drive(driven, new CarInput { Throttle = 1f }, 3f);
            Drive(driven, new CarInput { Throttle = 0.6f, Steer = 1f }, 0.8f);
            Assert.Greater(driven.Body.Slip, 0.05f, "the tail is out");
            float nose = Mathf.Abs(Mathf.Atan2(driven.Body.LateralSpeed, driven.Body.ForwardSpeed)) * Mathf.Rad2Deg;
            Assert.Less(nose, 45f, "but it does not spin");
            Drive(driven, new CarInput { Throttle = 0.6f }, 1.5f);
            Assert.Less(Mathf.Abs(driven.Body.LateralSpeed), 1.5f, "let go of the wheel and it straightens up");
        }

        [Test]
        public void OnTheGridItStaysPut()
        {
            Car driven = Spawn(CarModel.Tyrant);
            driven.Body.Held = true;
            Drive(driven, new CarInput { Throttle = 1f, Steer = 1f }, 1f);
            Assert.AreEqual(-2000f, driven.Body.Rigidbody.position.z, 1e-3f);
            Assert.AreEqual(1f, driven.Body.Throttle, "the engine is revved");
        }

        [Test]
        public void AWreckRollsToRestAndStaysAsAnObstacle()
        {
            Car driven = Spawn(CarModel.Bulldog);
            Drive(driven, new CarInput { Throttle = 1f }, 2f);
            driven.Wreck(new DamageInfo(100f, DamageSource.Rocket, null, Vector3.zero, Vector3.forward));
            Assert.IsTrue(driven.IsWrecked);
            Drive(driven, new CarInput { Throttle = 1f }, 6f);
            Assert.IsTrue(driven.Body.Rigidbody.isKinematic);
            Assert.AreEqual(0f, driven.Body.ForwardSpeed);
        }
    }
}
