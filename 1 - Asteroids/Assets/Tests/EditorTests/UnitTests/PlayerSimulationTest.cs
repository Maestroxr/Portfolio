using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Portfolio;
using System;

namespace Tests
{
    public class MockTransformAdapter : ILocalTransformAdapter
    {
        public Vector3 LocalPosition { get; set; }
        public Quaternion LocalRotation { get; set; }
        public Vector3 Forward => Vector3.forward;
    }

    public class PlayerSimulationTest
    {
        private PlayerSimulation playerSimulation;
        private PlayerSettings playerSettings;
        private MockTransformAdapter transform;

        private const float PlayerSpeed = 3;
        private const float RotationSpeed = 100;

        [SetUp]
        public void PlayerMovementSetUp()
        {
            playerSettings = new PlayerSettings();
            playerSettings.MockSettings(PlayerSpeed, RotationSpeed);
            transform = new MockTransformAdapter();
            playerSimulation = new PlayerSimulation(transform, playerSettings);
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
            float distanceExpected = framesToTest * frameTime * PlayerSpeed;
            float epsilon = 0.001f;
            Assert.GreaterOrEqual(epsilon, Math.Abs(distanceExpected - distanceTraveled), $"Player did not move as far as expected.");
        }
    }

}
