using System;
using NUnit.Framework;
using Portfolio.Asteroids;
using UnityEngine;

namespace Portfolio.Asteroids.Tests
{
    public class MockTransformAdapter : ILocalTransformAdapter
    {
        public Vector3 LocalPosition { get; set; }
        public Quaternion LocalRotation { get; set; } = Quaternion.identity;
        public Vector3 Forward => LocalRotation * Vector3.up;
    }

    public class PlayerSimulationTest
    {
        private PlayerSimulation playerSimulation;
        private PlayerSettings playerSettings;
        private MockTransformAdapter transform;

        private const float Thrust = 20f;
        private const float MaxSpeed = 10f;
        private const float Drag = 0.5f;
        private const float RotationSpeed = 180f;

        [SetUp]
        public void PlayerMovementSetUp()
        {
            playerSettings = ScriptableObject.CreateInstance<PlayerSettings>();
            playerSettings.MockSettings(Thrust, MaxSpeed, Drag, RotationSpeed, 10f);
            transform = new MockTransformAdapter();
            playerSimulation = new PlayerSimulation(transform, playerSettings);
        }

        [TearDown]
        public void PlayerMovementTearDown()
        {
            UnityEngine.Object.DestroyImmediate(playerSettings);
        }

        [Test]
        public void PlayerMovementForward()
        {
            int framesToTest = 100;
            float frameTime = 0.1f;
            Vector3 initialPlayerPosition = transform.LocalPosition;
            for (int i = 0; i < framesToTest; i++)
            {
                playerSimulation.MoveForward(frameTime);
            }
            Vector3 lastPlayerPosition = transform.LocalPosition;
            float distanceTraveled = (lastPlayerPosition - initialPlayerPosition).magnitude;
            float distanceExpected = framesToTest * frameTime * MaxSpeed;
            float epsilon = 0.001f;
            Assert.GreaterOrEqual(epsilon, Math.Abs(distanceExpected - distanceTraveled), "Player did not move as far as expected.");
        }

        [Test]
        public void ThrustAcceleratesAlongTheNose()
        {
            playerSimulation.Thrust(0.1f);
            playerSimulation.Step(0.1f);
            Assert.That(playerSimulation.Velocity.y, Is.EqualTo(Thrust * 0.1f).Within(0.001f), "One tenth of a second of thrust.");
            Assert.That(playerSimulation.Velocity.x, Is.EqualTo(0f).Within(0.001f));
            Assert.Greater(transform.LocalPosition.y, 0f, "The ship moved forward.");
        }

        [Test]
        public void SpeedIsLimited()
        {
            for (int i = 0; i < 300; i++)
            {
                playerSimulation.Thrust(0.02f);
                playerSimulation.Step(0.02f);
            }
            Assert.That(playerSimulation.Speed, Is.EqualTo(MaxSpeed).Within(0.01f), "Thrusting for seconds reaches the top speed and no more.");
        }

        [Test]
        public void CoastingShipDriftsAndSlowsDown()
        {
            playerSimulation.Velocity = new Vector3(0f, 8f, 0f);
            playerSimulation.Step(0.5f);
            float after = playerSimulation.Speed;
            Assert.Less(after, 8f, "Drag slows a coasting ship.");
            Assert.Greater(after, 4f, "The ship keeps drifting; space has little drag.");
        }

        [Test]
        public void BrakeStopsTheShip()
        {
            playerSimulation.Velocity = new Vector3(3f, 4f, 0f);
            for (int i = 0; i < 60; i++)
            {
                playerSimulation.Brake(0.02f);
            }
            Assert.That(playerSimulation.Speed, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void SteeringLeftTurnsCounterClockwise()
        {
            for (int i = 0; i < 25; i++)
            {
                playerSimulation.Steer(1f, 0.02f);
                playerSimulation.Step(0.02f);
            }
            Vector3 forward = transform.Forward;
            Assert.Less(forward.x, -0.1f, "Turning left points the nose to negative x.");
            Assert.That(playerSimulation.AngularVelocity, Is.EqualTo(RotationSpeed).Within(1f), "The turn rate eases up to the top rate.");
        }

        [Test]
        public void DashBurstsForwardAndRecharges()
        {
            Assert.IsTrue(playerSimulation.Dash(), "The first dash is ready.");
            Assert.IsTrue(playerSimulation.IsDashing);
            Assert.Greater(playerSimulation.Speed, MaxSpeed, "A dash goes beyond the cruising speed.");
            Assert.IsFalse(playerSimulation.Dash(), "A second dash has to wait for the recharge.");
            for (int i = 0; i < 200; i++)
            {
                playerSimulation.Step(0.02f);
            }
            Assert.IsFalse(playerSimulation.IsDashing);
            Assert.That(playerSimulation.DashRecharge, Is.EqualTo(0f));
            Assert.IsTrue(playerSimulation.Dash(), "The dash is ready again after the cooldown.");
        }
    }
}
