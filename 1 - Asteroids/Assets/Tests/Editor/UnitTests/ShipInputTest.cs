using NUnit.Framework;
using UnityEngine;

namespace Portfolio.Asteroids.Tests
{
    /// <summary>How the touch stick turns into steering and thrust (<see cref="PlayerShipInput.Steer"/>).</summary>
    public class ShipInputTest
    {
        [Test]
        public void StickAlongTheNose_ThrustsWithoutTurning()
        {
            PlayerShipInput.Steer(Vector2.up, Vector2.up * 0.8f, 0f, out float turn, out float thrust);
            Assert.AreEqual(0f, turn, 1e-4f);
            Assert.AreEqual(0.8f, thrust, 1e-4f);
        }

        [Test]
        public void StickToTheLeft_TurnsLeftWithoutThrust()
        {
            PlayerShipInput.Steer(Vector2.up, Vector2.left, 0f, out float turn, out float thrust);
            Assert.AreEqual(1f, turn, 1e-4f);
            Assert.AreEqual(0f, thrust, 1e-4f);
        }

        [Test]
        public void StickToTheRight_TurnsRight()
        {
            PlayerShipInput.Steer(Vector2.up, Vector2.right, 0f, out float turn, out _);
            Assert.AreEqual(-1f, turn, 1e-4f);
        }

        [Test]
        public void StickBehind_TurnsAroundBeforeThrusting()
        {
            PlayerShipInput.Steer(Vector2.up, new Vector2(0.1f, -1f), 0f, out float turn, out float thrust);
            Assert.AreEqual(1f, Mathf.Abs(turn), 1e-4f);
            Assert.AreEqual(0f, thrust, 1e-4f);
        }

        [Test]
        public void NearTheStick_TurnsLessAndLessToSettle()
        {
            Vector2 stick = Quaternion.Euler(0f, 0f, 10f) * Vector2.up;
            PlayerShipInput.Steer(Vector2.up, stick, 0f, out float slow, out float thrust);
            Assert.AreEqual(10f / PlayerShipInput.FullTurnAngle, slow, 1e-3f);
            Assert.Greater(thrust, 0.9f);

            // Already turning toward the stick fast enough: ease off (or counter-steer) instead of swinging past.
            PlayerShipInput.Steer(Vector2.up, stick, 200f, out float settling, out _);
            Assert.Less(settling, 0f);
        }

        [Test]
        public void NoStick_NoCommands()
        {
            PlayerShipInput.Steer(Vector2.up, Vector2.zero, 90f, out float turn, out float thrust);
            Assert.AreEqual(0f, turn);
            Assert.AreEqual(0f, thrust);
        }
    }
}
