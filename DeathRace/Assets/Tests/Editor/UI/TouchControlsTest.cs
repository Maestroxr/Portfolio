using Gamebox;
using Gamebox.UI;
using NUnit.Framework;
using UnityEngine;

namespace Portfolio.DeathRace.Tests
{
    /// <summary>
    /// Racing by touch (<see cref="RaceTouchControls"/>): the pad over the HUD drives the car beside the keys, the car
    /// accelerates by itself unless the setting is off, the buttons show what is left, and the HUD leaves the bottom
    /// corners to the thumbs.
    /// </summary>
    public class TouchControlsTest
    {
        private TemporaryObjects temporary;
        private Race race;
        private RectTransform hudRoot;
        private int frame;

        [SetUp]
        public void SetUp()
        {
            temporary = new TemporaryObjects();
            Transform root = temporary.Keep(new GameObject("Race")).transform;
            race = new Race(new RaceSetup { Mode = RaceMode.Quick, Seed = 4 }, null, root) { Phase = RacePhase.Racing };
            hudRoot = OverlayKit.Canvas(root, "HUD", RaceDirector.HudSortingOrder).GetComponent<RectTransform>();
            frame = 50;
            PlayerControls.Frame = () => frame;
            ControlInput.Source = new ScriptedInput();
        }

        [TearDown]
        public void TearDown()
        {
            temporary.Dispose();
            PlayerControls.Frame = () => Time.frameCount;
            ControlInput.Source = null;
        }

        private Car Spawn(ICarDriver driver)
        {
            var loadout = new CarLoadout { model = CarModel.Rustbucket, mines = 2, rockets = 0, nitro = 1 };
            var entrant = new RaceEntrant { Seat = 0, Name = "You", Driver = DriverKind.Human, Loadout = loadout };
            Car car = CarFactory.Spawn(entrant, new Pose(Vector3.zero, Quaternion.identity), null, null, race, driver, race.Root);
            car.AttachRacer(new Racer(0, null));
            race.Cars.Add(car);
            return car;
        }

        [Test]
        public void ThePadDrivesTheCar_AndTheCarAcceleratesByItself()
        {
            var driver = new HumanDriver();
            Car car = Spawn(driver);
            var settings = ScriptableObject.CreateInstance<DeathRaceSettings>();
            temporary.Keep(settings);
            RaceTouchControls touch = RaceTouchControls.Create(hudRoot, car, null, null, settings);

            Assert.AreSame(touch.Controls, driver.Touch, "The driver reads the pad beside the keys");
            Assert.AreEqual(5, touch.Pad.ButtonCount, "The gun, the nitro, the mine, the rocket and the special");
            Assert.IsTrue(touch.Pad.AutoForward, "Accelerating by itself is the default");
            Assert.AreEqual(1f, driver.Drive(car, 0.02f).Throttle, 1e-4f, "Nobody touches the pad: the car accelerates");

            settings.autoAccelerate = false;
            touch.ApplySettings(settings);
            frame++;
            Assert.AreEqual(0f, driver.Drive(car, 0.02f).Throttle, 1e-4f, "Without the setting the stick is the pedals too");
        }

        [Test]
        public void ANewLookBuildsThePadAgain_TheDriverKeepsIt()
        {
            var driver = new HumanDriver();
            Car car = Spawn(driver);
            RaceTouchControls touch = RaceTouchControls.Create(hudRoot, car, null, null, null);
            TouchPad first = touch.Pad;
            touch.Restyle(null);
            Assert.AreNotSame(first, touch.Pad);
            Assert.AreSame(touch.Controls, driver.Touch, "The driver reads the new pad");
            Assert.IsTrue(touch.Pad.AutoForward, "The accelerator stays as it was");
            Assert.AreEqual(1, touch.GetComponentsInChildren<TouchPad>(true).Length, "The old pad is gone");
        }

        [Test]
        public void TheButtonsShowWhatIsLeft()
        {
            Car car = Spawn(new HumanDriver());
            RaceTouchControls touch = RaceTouchControls.Create(hudRoot, car, null, null, null);
            // Without a look there are no icons: the buttons keep their words and fade when empty.
            Assert.IsFalse(touch.Pad.HasIcon(DeathRaceControls.Mine));
            Assert.AreEqual(1f, touch.Pad.ButtonOf(DeathRaceControls.Mine).GetComponent<CanvasGroup>().alpha, "Two mines");
            Assert.Less(touch.Pad.ButtonOf(DeathRaceControls.Rocket).GetComponent<CanvasGroup>().alpha, 1f, "No rockets: faded");
        }

        [Test]
        public void TheHudLeavesTheBottomCornersToTheThumbs()
        {
            Car car = Spawn(new HumanDriver());
            RaceHud keys = RaceHud.Create(hudRoot, new Rect(0f, 0f, 1f, 1f), car, race, null, null);
            RaceHud touch = RaceHud.Create(hudRoot, new Rect(0f, 0f, 1f, 1f), car, race, null, null, true);

            Assert.AreEqual(new Vector2(0f, 0f), Panel(keys, "Car").anchorMin, "With keys the car panel is bottom left");
            Assert.AreEqual(new Vector2(1f, 0f), Panel(keys, "Cash").anchorMin);
            Assert.AreEqual(new Vector2(0.5f, 1f), Panel(touch, "Car").anchorMin, "With touch it is at the top in the middle");
            Assert.AreEqual(new Vector2(0f, 1f), Panel(touch, "Cash").anchorMin, "and the cash under the place");
            Assert.IsTrue(touch.Touch);
            Assert.IsNotNull(Panel(touch, "SafeArea"), "A view over the whole screen keeps clear of a notch");
            Assert.AreSame(Panel(touch, "SafeArea"), Panel(touch, "Car").parent, "The panels sit inside it");
        }

        private static RectTransform Panel(RaceHud hud, string panelName)
        {
            foreach (RectTransform rect in hud.GetComponentsInChildren<RectTransform>(true))
            {
                if (rect.name == panelName)
                {
                    return rect;
                }
            }
            Assert.Fail($"The HUD has no {panelName} panel");
            return null;
        }

        [Test]
        public void TheSettingsKeepAutoAccelerate()
        {
            var storage = new TransientStrategy();
            var settings = temporary.Keep(ScriptableObject.CreateInstance<DeathRaceSettings>());
            Assert.IsTrue(settings.autoAccelerate);
            settings.autoAccelerate = false;
            settings.SaveSettings(storage, "Test");

            var loaded = temporary.Keep(ScriptableObject.CreateInstance<DeathRaceSettings>());
            loaded.LoadSettings(storage, "Test");
            Assert.IsFalse(loaded.autoAccelerate);

            var copy = temporary.Keep(ScriptableObject.CreateInstance<DeathRaceSettings>());
            copy.CopySettings(loaded);
            Assert.IsFalse(copy.autoAccelerate);
        }
    }
}
