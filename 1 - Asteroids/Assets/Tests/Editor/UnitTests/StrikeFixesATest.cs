using Gamebox;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Portfolio.Asteroids.Tests
{
    /// <summary>
    /// The fixes of the strike review (group A) that need no scene: the kamikaze dives on screen, side entries start off
    /// screen, the saved phase shield keeps what is left of it, and the Supply Room's energy and wallet rules.
    /// </summary>
    public class StrikeFixesATest
    {
        private const float Step = 1f / 60f;

        private readonly TemporaryObjects objects = new TemporaryObjects();
        private SpaceField field;
        private Playground playground;


        [SetUp]
        public void SetUp()
        {
            playground = objects.Component<Playground>("Playground");
            playground.Fix(StrikeRules.HalfSize);
            playground.Wraps = false;
            field = objects.Component<SpaceField>("Field");
            field.playground = playground;
        }


        [TearDown]
        public void TearDown()
        {
            objects.Dispose();
        }


        // ------------------------------------------------------------------ the kamikaze

        [TestCase(FlightPath.DiveLeft, false)]
        [TestCase(FlightPath.DiveLeft, true)]
        [TestCase(FlightPath.Swoop, false)]
        [TestCase(FlightPath.Swoop, true)]
        [TestCase(FlightPath.StraightDown, false)]
        public void KamikazeDivesWhileOnScreen(FlightPath path, bool mirror)
        {
            StrikeAircraft kamikaze = objects.Component<StrikeAircraft>("Kamikaze");
            kamikaze.unit = StrikeUnit.Kamikaze;
            kamikaze.radius = 0.8f;
            kamikaze.maxHealth = 100f;
            kamikaze.baseSpeed = 12f;
            kamikaze.pattern = AttackPattern.None;
            var spawn = new StrikeEvent { unit = StrikeUnit.Kamikaze, path = path, mirror = mirror };
            Vector2 start = ScrollDirector.AirStart(spawn, FlightPaths.Waypoints(path, mirror), 0f);
            kamikaze.Launch(spawn, start, 0f, 1f);
            field.Add(kamikaze);
            Assert.AreEqual(FlightType.Kamikaze, kamikaze.Type);
            for (float time = 0f; time < 20f && kamikaze.InPlay && !kamikaze.IsDiving; time += Step)
            {
                field.Tick(Step);
            }
            Assert.IsTrue(kamikaze.IsDiving, "It locked on and dived.");
            Assert.IsTrue(kamikaze.InPlay, "It dived before it could leave.");
            Assert.IsTrue(playground.IsInside(kamikaze.Position, -kamikaze.Radius), "It dives from where the pilot can see it.");
            Assert.AreEqual(StrikeRules.KamikazeDiveSpeed, kamikaze.Speed, 1e-4f);
        }


        [Test]
        public void TheDivePointOfEveryPathIsOnScreen()
        {
            foreach (FlightPath path in FlightPaths.All)
            {
                foreach (bool mirror in new[] { false, true })
                {
                    FlightCurve curve = FlightPaths.Curve(path, mirror, false);
                    Assert.Less(curve.DiveAt, curve.Length, $"{path}: the dive comes before the end.");
                    Assert.IsTrue(playground.IsInside(curve.PointAt(curve.DiveAt)), $"{path} (mirror {mirror}): the dive starts on screen.");
                }
            }
        }


        // ------------------------------------------------------------------ side entries

        [Test]
        public void SideEntriesAreNeverMovedOntoTheScreen()
        {
            Vector2[] cross = FlightPaths.Waypoints(FlightPath.CrossLeft, false);
            Vector2[] crossMirrored = FlightPaths.Waypoints(FlightPath.CrossLeft, true);
            Vector2 inward = ScrollDirector.AirStart(new StrikeEvent { path = FlightPath.CrossLeft, x = -9f }, cross, 0f);
            Assert.AreEqual(cross[0].x, inward.x, 1e-4f, "An x toward the screen is dropped.");
            Vector2 mirrored = ScrollDirector.AirStart(new StrikeEvent { path = FlightPath.CrossLeft, mirror = true, x = 9f }, crossMirrored, 0f);
            Assert.AreEqual(crossMirrored[0].x, mirrored.x, 1e-4f, "Mirrored too.");
            Vector2 outward = ScrollDirector.AirStart(new StrikeEvent { path = FlightPath.CrossLeft, x = 3f }, cross, 0f);
            Assert.AreEqual(cross[0].x + 3f, outward.x, 1e-4f, "An x further out is kept.");
            Vector2[] down = FlightPaths.Waypoints(FlightPath.StraightDown, false);
            Vector2 top = ScrollDirector.AirStart(new StrikeEvent { path = FlightPath.StraightDown, x = -5f }, down, 2f);
            Assert.AreEqual(new Vector2(down[0].x - 5f, down[0].y + 2f), top, "A path from the top moves across as before.");
        }


        [Test]
        public void EveryAirEventOfTheCampaignStartsOffScreen()
        {
            int checkedEvents = 0;
            for (int number = 1; number <= 9; number++)
            {
                var level = AssetDatabase.LoadAssetAtPath<StrikeLevel>($"Assets/Config/Strike/Level{number}.asset");
                if (level == null)
                {
                    continue;
                }
                foreach (StrikeEvent spawn in level.Events)
                {
                    if (spawn == null || !spawn.IsAir)
                    {
                        continue;
                    }
                    Vector2 start = ScrollDirector.AirStart(spawn, FlightPaths.Waypoints(spawn.path, spawn.mirror), 0f);
                    Assert.IsFalse(playground.IsInside(start, -0.5f), $"Level {number}, {spawn.unit} at {spawn.at} starts on screen at {start}.");
                    checkedEvents++;
                }
            }
            Assert.Greater(checkedEvents, 0, "The campaign's strike levels were found.");
        }


        // ------------------------------------------------------------------ the saved phase shield

        [Test]
        public void ShieldPointsAreSavedRoundedDownButNeverToNothing()
        {
            Assert.AreEqual(0, AsteroidsProgress.ShieldHundredths(0f));
            Assert.AreEqual(1, AsteroidsProgress.ShieldHundredths(0.003f), "What is left of a shield stays.");
            Assert.AreEqual(9999, AsteroidsProgress.ShieldHundredths(99.995f), "A damaged shield is not whole after a reload.");
            Assert.AreEqual(10000, AsteroidsProgress.ShieldHundredths(100f));
            Assert.AreEqual(5625, AsteroidsProgress.ShieldHundredths(56.25f));
            Assert.AreEqual(3333, AsteroidsProgress.ShieldHundredths(33.33f));
        }


        [Test]
        public void ANearlyEmptyShieldReloadsNearlyEmpty()
        {
            var progress = new AsteroidsProgress(new TransientStrategy(), GameType.Asteroids);
            StrikeLoadout pilot = StrikeLoadout.NewPilot();
            pilot.PhaseShields = 2;
            pilot.ShieldPoints = 0.003f;
            progress.SavePilot(pilot);
            StrikeLoadout loaded = progress.LoadPilot();
            Assert.AreEqual(2, loaded.PhaseShields);
            Assert.Greater(loaded.ShieldPoints, 0f);
            Assert.Less(loaded.ShieldPoints, 0.02f, "Not a free full shield.");

            pilot.PhaseShields = 1;
            pilot.ShieldPoints = 99.996f;
            progress.SavePilot(pilot);
            Assert.IsFalse(StrikeArmory.CanSell(progress.LoadPilot(), StrikeItem.PhaseShield), "Still damaged, so not for resale.");
        }


        // ------------------------------------------------------------------ the Supply Room

        [Test]
        public void EnergyIsSoldOnlyWhileTheLaunchMinimumStays()
        {
            StrikeLoadout pilot = StrikeLoadout.NewPilot();
            pilot.Energy = 49f;
            Assert.IsFalse(StrikeArmory.CanSell(pilot, StrikeItem.EnergyModule), "A launch would give the sold energy back.");
            Assert.IsFalse(StrikeArmory.Sell(pilot, StrikeItem.EnergyModule));
            Assert.AreEqual(49f, pilot.Energy);
            pilot.Energy = 50f;
            Assert.IsTrue(StrikeArmory.CanSellEnergy(pilot));
            Assert.IsTrue(StrikeArmory.Sell(pilot, StrikeItem.EnergyModule));
            Assert.AreEqual(25f, pilot.Energy);
            int money = pilot.Money;
            pilot.EnsureLaunchEnergy();
            Assert.AreEqual(25f, pilot.Energy, "Nothing to top up.");
            Assert.AreEqual(money, pilot.Money);
        }


        [Test]
        public void TheWalletMustTakeTheWholeSale()
        {
            StrikeLoadout pilot = StrikeLoadout.NewPilot();
            pilot.AddItem(StrikeItem.TwinLaser);
            int resale = StrikeArmory.SellPrice(pilot, StrikeItem.TwinLaser);
            pilot.Money = StrikeRules.WalletCap - resale;
            Assert.IsTrue(StrikeArmory.WalletTakes(pilot, StrikeItem.TwinLaser), "An exact fit is paid in full.");
            pilot.Money = StrikeRules.WalletCap - resale + 1;
            Assert.IsFalse(StrikeArmory.WalletTakes(pilot, StrikeItem.TwinLaser), "A sale cut short by the cap.");
            pilot.Money = StrikeRules.WalletCap;
            Assert.IsFalse(StrikeArmory.WalletTakes(pilot, StrikeItem.TwinLaser), "A full wallet takes nothing.");
        }
    }
}
