using System.Collections;
using Gamebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace Portfolio.Asteroids.Tests
{
    /// <summary>The on-screen controls of phones and tablets fly the ship like the keyboard does.</summary>
    public class TouchControlsTest
    {
        [TearDown]
        public void TearDown()
        {
            MobilePlatform.ForceTouch = false;
        }

        [UnityTest]
        public IEnumerator StickTurnsTheShipAndFireShoots()
        {
            MobilePlatform.ForceTouch = true;
            yield return TestScenes.StartFirstMission();
            AsteroidsPlayer ship = TestScenes.Manager.Ship;
            ShipTouchControls controls = Object.FindAnyObjectByType<ShipTouchControls>();
            Assert.NotNull(controls, "The HUD has touch controls.");
            Assert.IsTrue(controls.Active, "The touch controls are in use.");
            Assert.AreSame(controls, ship.TouchControls, "The ship reads the touch controls.");

            // A thumb on the stick area pushes to the right of where it landed: the ship, nose up, turns right.
            RectTransform area = (RectTransform)controls.stick.transform;
            Vector2 landing = RectTransformUtility.WorldToScreenPoint(null, area.TransformPoint(area.rect.center));
            var thumb = new PointerEventData(EventSystem.current) { pointerId = 10, position = landing };
            controls.stick.OnPointerDown(thumb);
            thumb.position = landing + new Vector2(1000f, 0f);
            controls.stick.OnDrag(thumb);
            Assert.IsTrue(controls.Steering, "The stick is pushed.");
            float heading = ship.transform.eulerAngles.z;
            yield return new WaitForSeconds(0.4f);
            Assert.Less(Mathf.DeltaAngle(heading, ship.transform.eulerAngles.z), -20f, "The ship turned right, toward the stick.");

            // Holding FIRE shoots.
            int volleys = 0;
            ship.VolleyFired += count => volleys++;
            var finger = new PointerEventData(EventSystem.current) { pointerId = 11 };
            controls.fire.OnPointerDown(finger);
            yield return new WaitForSeconds(0.5f);
            controls.fire.OnPointerUp(finger);
            controls.stick.OnPointerUp(thumb);
            Assert.Greater(volleys, 0, "Holding FIRE fired the guns.");
            Assert.IsFalse(controls.Steering, "Letting go of the stick centres it.");
        }
    }
}
