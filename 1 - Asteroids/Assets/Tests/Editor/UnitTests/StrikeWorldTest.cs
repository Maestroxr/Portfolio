using System.Collections.Generic;
using Gamebox;
using NUnit.Framework;
using UnityEngine;

namespace Portfolio.Asteroids.Tests
{
    /// <summary>
    /// The strike world without a scene: the attack clock and the shape of a volley, the beam's column, and a bare field
    /// (a non-wrapping playground, no spawner, sounds or effects) with ground units, aircraft and bosses built on the spot.
    /// </summary>
    public class StrikeWorldTest
    {
        private const float Step = 1f / 60f;

        private readonly TemporaryObjects objects = new TemporaryObjects();
        private SpaceField field;
        private readonly List<SpaceBody> entered = new List<SpaceBody>();
        private readonly List<Shootable> destroyed = new List<Shootable>();


        [SetUp]
        public void SetUp()
        {
            Playground playground = objects.Component<Playground>("Playground");
            playground.Fix(StrikeRules.HalfSize);
            playground.Wraps = false;
            field = objects.Component<SpaceField>("Field");
            field.playground = playground;
            entered.Clear();
            destroyed.Clear();
            field.HostileEntered += body => entered.Add(body);
            field.TargetDestroyed += (target, hit) => destroyed.Add(target);
        }


        [TearDown]
        public void TearDown()
        {
            objects.Dispose();
        }


        private void Run(float seconds)
        {
            for (float time = 0f; time < seconds - 1e-4f; time += Step)
            {
                field.Tick(Step);
            }
        }


        private static DamageInfo Kill(Vector2 at)
        {
            return new DamageInfo(10000f, Vector2.up, at, DamageSource.PlayerShot, true);
        }


        // ------------------------------------------------------------------ the attack clock

        private static List<float> FireTimes(StrikeGun gun, float seconds, bool canFire = true)
        {
            var times = new List<float>();
            float time = 0f;
            for (int i = 0; i < Mathf.RoundToInt(seconds / 0.01f); i++)
            {
                time += 0.01f;
                if (gun.Tick(0.01f, canFire))
                {
                    times.Add(time);
                }
            }
            return times;
        }


        [Test]
        public void GunFiresAfterItsDelayThenEveryInterval()
        {
            var gun = new StrikeGun();
            gun.Configure(AttackPattern.Aimed, EnemyShotKind.Flak, 1, 0f, 2f);
            gun.Restart(0.5f);
            List<float> times = FireTimes(gun, 5f);
            Assert.AreEqual(3, times.Count, "Three attacks in five seconds.");
            Assert.AreEqual(0.5f, times[0], 0.011f);
            Assert.AreEqual(2.5f, times[1], 0.011f);
            Assert.AreEqual(4.5f, times[2], 0.011f);
        }


        [Test]
        public void GunFiresItsBurstGapApart()
        {
            var gun = new StrikeGun();
            gun.Configure(AttackPattern.AimedBurst, EnemyShotKind.Flak, 3, 0.15f, 2.2f);
            gun.Restart(0.1f);
            List<float> times = FireTimes(gun, 2f);
            Assert.AreEqual(3, times.Count, "One attack of three volleys.");
            Assert.AreEqual(0.15f, times[1] - times[0], 0.011f);
            Assert.AreEqual(0.15f, times[2] - times[1], 0.011f);
        }


        [Test]
        public void GunAlternatesItsPatterns()
        {
            var gun = new StrikeGun();
            gun.Configure(AttackPattern.Angled, EnemyShotKind.Bolt, 1, 0f, 1f, AttackPattern.Rockets, EnemyShotKind.Rocket);
            gun.Restart(0f);
            var patterns = new List<AttackPattern>();
            var kinds = new List<EnemyShotKind>();
            for (int i = 0; i < 400; i++)
            {
                if (gun.Tick(0.01f, true))
                {
                    patterns.Add(gun.CurrentPattern);
                    kinds.Add(gun.CurrentKind);
                }
            }
            CollectionAssert.AreEqual(new[] { AttackPattern.Angled, AttackPattern.Rockets, AttackPattern.Angled, AttackPattern.Rockets }, patterns);
            CollectionAssert.AreEqual(new[] { EnemyShotKind.Bolt, EnemyShotKind.Rocket, EnemyShotKind.Bolt, EnemyShotKind.Rocket }, kinds);
        }


        [Test]
        public void GunWarnsBeforeALaser()
        {
            var gun = new StrikeGun();
            gun.Configure(AttackPattern.Laser, EnemyShotKind.Beam, 1, 0f, 3f, telegraph: 0.6f);
            gun.Restart(0.2f);
            bool warned = false;
            float fired = -1f;
            float time = 0f;
            for (int i = 0; i < 150 && fired < 0f; i++)
            {
                time += 0.01f;
                bool fire = gun.Tick(0.01f, true);
                warned |= gun.IsWarning;
                if (fire)
                {
                    fired = time;
                }
            }
            Assert.IsTrue(warned, "The gun glowed first.");
            Assert.AreEqual(0.8f, fired, 0.021f, "The laser fired after its delay and the warning.");
            Assert.IsFalse(gun.IsWarning);
        }


        [Test]
        public void GunHoldsFireOffScreen()
        {
            var gun = new StrikeGun();
            gun.Configure(AttackPattern.AimedBurst, EnemyShotKind.Flak, 3, 0.15f, 1f);
            gun.Restart(0f);
            Assert.AreEqual(0, FireTimes(gun, 3f, false).Count, "Nothing fires while it may not.");
            Assert.IsTrue(gun.Tick(0.01f, true), "The first volley goes as soon as it may.");
            Assert.IsFalse(gun.Tick(0.2f, false), "Leaving the screen breaks the burst off.");
            Assert.IsFalse(gun.Tick(0.2f, true), "The broken burst does not resume.");
        }


        [Test]
        public void UnarmedGunNeverFires()
        {
            var gun = new StrikeGun();
            gun.Configure(AttackPattern.None, EnemyShotKind.Flak, 1, 0f, 1f);
            gun.Restart(0f);
            Assert.AreEqual(0, FireTimes(gun, 3f).Count);
            gun.Configure(AttackPattern.Aimed, EnemyShotKind.Flak, 1, 0f, 0f);
            Assert.AreEqual(0, FireTimes(gun, 3f).Count, "An interval of zero means no attack.");
        }


        [Test]
        public void VolleysFanOut()
        {
            Assert.AreEqual(-45f, StrikeGun.FanAngle(AttackPattern.Angled, 0, 2), 1e-4f);
            Assert.AreEqual(45f, StrikeGun.FanAngle(AttackPattern.Angled, 1, 2), 1e-4f);
            Assert.AreEqual(-30f, StrikeGun.FanAngle(AttackPattern.Spread, 0, 3), 1e-4f);
            Assert.AreEqual(0f, StrikeGun.FanAngle(AttackPattern.Spread, 1, 3), 1e-4f);
            Assert.AreEqual(-60f, StrikeGun.FanAngle(AttackPattern.Spread, 0, 5), 1e-4f);
            Assert.AreEqual(60f, StrikeGun.FanAngle(AttackPattern.Spread, 4, 5), 1e-4f);
            Assert.AreEqual(0f, StrikeGun.FanAngle(AttackPattern.Down, 1, 2), 1e-4f, "Barrels side by side fire straight.");
            Assert.AreEqual(0f, StrikeGun.FanAngle(AttackPattern.Spread, 0, 1), 1e-4f);
            Assert.IsTrue(StrikeGun.Aims(AttackPattern.Aimed));
            Assert.IsTrue(StrikeGun.Aims(AttackPattern.Rockets));
            Assert.IsFalse(StrikeGun.Aims(AttackPattern.Down));
            Assert.IsFalse(StrikeGun.Aims(AttackPattern.Mines));
            Assert.AreEqual(0f, StrikeGun.Lateral(0, 1, 2f));
            Assert.AreEqual(-0.7f, StrikeGun.Lateral(0, 2, 2f), 1e-4f);
        }


        [Test]
        public void HeadingsPointAsTheLevelsSay()
        {
            AssertClose(Vector2.up, StrikeGun.HeadingDirection(0f));
            AssertClose(Vector2.left, StrikeGun.HeadingDirection(90f));
            AssertClose(Vector2.down, StrikeGun.HeadingDirection(180f));
            AssertClose(Vector2.right, StrikeGun.HeadingDirection(270f));
            Assert.AreEqual(0f, Mathf.DeltaAngle(0f, StrikeGun.HeadingOf(Vector2.up)), 1e-3f);
            Assert.AreEqual(0f, Mathf.DeltaAngle(90f, StrikeGun.HeadingOf(Vector2.left)), 1e-3f);
            AssertClose(Vector2.left, StrikeGun.Rotate(Vector2.down, -90f));
        }


        [Test]
        public void BeamBurnsItsColumnDown()
        {
            var top = new Vector2(2f, 4f);
            Assert.IsTrue(EnemyBeam.InColumn(top, 0.35f, new Vector2(2f, -9f), 0.5f), "Straight below, to the bottom edge.");
            Assert.IsTrue(EnemyBeam.InColumn(top, 0.35f, new Vector2(2.8f, 0f), 0.5f), "A ship that overlaps the edge of the column.");
            Assert.IsFalse(EnemyBeam.InColumn(top, 0.35f, new Vector2(3f, 0f), 0.5f), "Beside the column.");
            Assert.IsFalse(EnemyBeam.InColumn(top, 0.35f, new Vector2(2f, 5f), 0.5f), "Above the gun.");
        }


        private static void AssertClose(Vector2 expected, Vector2 actual)
        {
            Assert.Less((expected - actual).magnitude, 1e-4f, $"{expected} expected, {actual} found.");
        }


        // ------------------------------------------------------------------ ground units

        private GroundUnit Ground(StrikeEvent spawn, Vector2 at)
        {
            GroundUnit unit = objects.Component<GroundUnit>("Ground");
            unit.radius = 1f;
            unit.maxHealth = 20f;
            unit.pattern = AttackPattern.None;
            unit.Place(spawn, 0, at);
            field.Add(unit);
            return unit;
        }


        [Test]
        public void GroundUnitRidesTheGroundAndDrivesOnceOnScreen()
        {
            field.ScrollSpeed = 2.4f;
            var spawn = new StrikeEvent { unit = StrikeUnit.Truck, heading = 90f, drive = 3f };
            GroundUnit truck = Ground(spawn, new Vector2(0f, 13f));
            Assert.AreEqual(Altitude.Ground, truck.Altitude);
            Run(0.5f);
            Assert.AreEqual(0f, truck.Position.x, 1e-3f, "Parked until it is on screen.");
            Assert.AreEqual(13f - 1.2f, truck.Position.y, 0.02f, "The ground carried it down.");
            Assert.AreEqual(0, entered.Count);
            Run(1f);
            Assert.AreEqual(1, entered.Count, "It counted as a hostile that entered, once.");
            Assert.Less(truck.Position.x, -1f, "It drives left (heading 90) once on screen.");
            Assert.AreEqual(0f, truck.Velocity.y, 1e-4f, "Its velocity is only its own drive on the ground.");
            Run(0.5f);
            Assert.AreEqual(1, entered.Count);
        }


        [Test]
        public void GroundUnitLeavesOnlyBelowTheBottom()
        {
            GroundUnit side = Ground(new StrikeEvent(), new Vector2(30f, 0f));
            GroundUnit low = Ground(new StrikeEvent(), new Vector2(0f, -11f));
            GroundUnit gone = Ground(new StrikeEvent(), new Vector2(0f, -12.6f));
            field.Tick(Step);
            Assert.IsTrue(side.InPlay, "Off a side it stays (it scrolls down in time).");
            Assert.IsTrue(low.InPlay, "Still partly under the bottom edge.");
            Assert.IsFalse(gone.InPlay, "Wholly below the bottom edge: gone.");
        }


        [Test]
        public void ExplodingUnitSetsOffTheGroundAroundIt()
        {
            GroundUnit tank = Ground(new StrikeEvent(), new Vector2(0f, 0f));
            tank.explodes = true;
            tank.blastRadius = 3.5f;
            tank.blastDamage = 12f;
            GroundUnit near = Ground(new StrikeEvent(), new Vector2(2f, 0f));
            near.maxHealth = 5f;
            near.OnSpawned();
            GroundUnit far = Ground(new StrikeEvent(), new Vector2(9f, 0f));
            far.maxHealth = 5f;
            far.OnSpawned();
            tank.TakeHit(Kill(tank.Position));
            Assert.IsFalse(tank.InPlay);
            Assert.IsFalse(near.InPlay, "The unit next to it went up in the chain.");
            Assert.IsTrue(far.InPlay, "The unit far away survived.");
            Assert.Contains(near, destroyed, "The chain kill counted.");
        }


        [Test]
        public void GroundUnitIsNotPushedOrNudged()
        {
            GroundUnit bunker = Ground(new StrikeEvent(), new Vector2(0f, 0f));
            bunker.maxHealth = 1000f;
            bunker.OnSpawned();
            bunker.TakeHit(new DamageInfo(1f, Vector2.right, bunker.Position, DamageSource.PlayerShot, true));
            field.Explode(new Blast { Center = new Vector2(-1f, 0f), Radius = 3f, Damage = 1f, Push = 8f, ByPlayer = true });
            Assert.AreEqual(Vector2.zero, bunker.Velocity);
        }


        // ------------------------------------------------------------------ aircraft

        [Test]
        public void AircraftWaitsItsTurnThenFliesItsPathAtConstantSpeed()
        {
            StrikeAircraft dart = objects.Component<StrikeAircraft>("Dart");
            dart.radius = 0.9f;
            dart.maxHealth = 4f;
            dart.baseSpeed = 10f;
            dart.pattern = AttackPattern.None;
            var spawn = new StrikeEvent { unit = StrikeUnit.Dart, path = FlightPath.Zigzag, x = 3f };
            Vector2 start = FlightPaths.Waypoints(FlightPath.Zigzag, false)[0] + new Vector2(3f, 0f);
            dart.Launch(spawn, start, 0.5f, 1.2f);
            field.Add(dart);
            Run(0.5f);
            Assert.IsTrue((dart.Position - start).magnitude < 1e-3f, "It waits at the start for its turn.");
            Run(1f);
            float flown = 12f * 1f;
            Vector2 expected = dart.CurveOffset + dart.Curve.PointAt(flown);
            Assert.Less((dart.Position - expected).magnitude, 0.25f, "It is as far along the path as its speed took it.");
            Assert.AreEqual(12f, dart.Speed, 1e-4f, "The event's speed multiplier applies.");
            Run(2f);
            Assert.AreEqual(1, entered.Count, "It counted as a hostile that entered, once.");
        }


        [Test]
        public void AircraftFacesDownTheScreenFlyingDown()
        {
            StrikeAircraft dart = objects.Component<StrikeAircraft>("Dart");
            dart.pattern = AttackPattern.None;
            dart.Launch(new StrikeEvent { path = FlightPath.StraightDown }, new Vector2(0f, 12f), 0f, 1f);
            field.Add(dart);
            Run(1f);
            Assert.Less(dart.Velocity.y, 0f);
            Assert.AreEqual(0f, Mathf.DeltaAngle(0f, dart.transform.eulerAngles.z), 2f, "The root is not turned for a dive straight down.");
        }


        // ------------------------------------------------------------------ bosses

        private StrikeBoss Boss(bool coreless, int coreTier, params int[] tiers)
        {
            StrikeBoss boss = objects.Component<StrikeBoss>("Boss");
            boss.radius = 3f;
            boss.maxHealth = 100f;
            boss.entrySeconds = 0f;
            boss.coreless = coreless;
            boss.coreTier = coreTier;
            boss.attacks = new BossAttack[0];
            boss.parts = new BossPart[tiers.Length];
            for (int i = 0; i < tiers.Length; i++)
            {
                BossPart part = objects.Component<BossPart>("Part" + i);
                part.transform.SetParent(boss.transform, false);
                part.transform.localPosition = new Vector3(i * 2f - 1f, -1f, 0f);
                part.radius = 0.8f;
                part.maxHealth = 50f;
                part.tier = tiers[i];
                part.pattern = AttackPattern.None;
                boss.parts[i] = part;
            }
            field.Add(boss);
            boss.AttachParts();
            return boss;
        }


        [Test]
        public void BossPartsAttachInOrder()
        {
            StrikeBoss boss = Boss(false, 0, 0, 0);
            for (int i = 0; i < 2; i++)
            {
                Assert.AreSame(boss, boss.Parts[i].Boss);
                Assert.AreEqual(i, boss.Parts[i].Index);
                Assert.IsTrue(boss.Parts[i].InPlay);
                CollectionAssert.Contains(field.Targets, boss.Parts[i]);
            }
            Assert.AreEqual(1f, boss.BarFraction, 1e-4f);
            Assert.IsTrue(boss.Invulnerable && boss.Parts[0].Invulnerable, "Everything is armoured while it comes in.");
        }


        [Test]
        public void ArmourAndPhasesFollowTheTiers()
        {
            StrikeBoss boss = Boss(false, 2, 0, 1);
            field.Tick(Step);
            Assert.IsTrue(boss.HasEntered, "It fights.");
            BossPart low = boss.Parts[0];
            BossPart high = boss.Parts[1];
            Assert.IsFalse(low.Invulnerable, "The lowest tier is open.");
            Assert.IsTrue(high.Invulnerable, "A higher tier is armoured while a lower one stands.");
            Assert.IsTrue(boss.Invulnerable, "The core is armoured while a part below its tier stands.");
            Assert.IsFalse(high.TakeHit(Kill(high.Position)), "Armour takes no damage.");
            Assert.AreEqual(50f, high.Health);

            low.TakeHit(Kill(low.Position));
            Assert.IsFalse(low.InPlay, "The part died on its own.");
            Assert.AreEqual(1, boss.Phase, "Clearing tier 0 is the next phase.");
            Assert.IsFalse(high.Invulnerable, "Tier 1 is open now.");
            Assert.IsTrue(boss.Invulnerable, "The core waits for tier 1.");
            Assert.AreEqual((100f + 50f) / 200f, boss.BarFraction, 1e-4f, "A dead part counts nothing on the bar.");

            high.TakeHit(Kill(high.Position));
            Assert.AreEqual(2, boss.Phase);
            Assert.IsFalse(boss.Invulnerable, "The core is open once every lower tier is down.");
            Assert.IsTrue(boss.InPlay);
            Assert.IsFalse(boss.IsDying, "The core still has to be destroyed.");
        }


        [Test]
        public void CorelessBossFallsWithItsLastPart()
        {
            StrikeBoss boss = Boss(true, 0, 0, 0);
            field.Tick(Step);
            Assert.IsTrue(boss.Invulnerable, "A coreless boss's core cannot be hit.");
            Assert.AreEqual(1f, boss.BarFraction, 1e-4f);
            boss.Parts[0].TakeHit(Kill(boss.Parts[0].Position));
            Assert.AreEqual(0.5f, boss.BarFraction, 1e-4f, "Only the parts count on its bar.");
            Assert.IsFalse(boss.IsDying);
            boss.Parts[1].TakeHit(Kill(boss.Parts[1].Position));
            Assert.IsTrue(boss.IsDying, "The last part down defeats it.");
            Assert.Contains(boss, destroyed, "Its defeat was reported (the bounty is paid).");
        }


        [Test]
        public void GroundBossRidesInAndFightsWhenTheScrollStops()
        {
            StrikeBoss boss = Boss(false, 0);
            boss.groundBoss = true;
            boss.holdLine = 0.5f;
            boss.OnSpawned();
            field.ScrollSpeed = 2.4f;
            float startY = boss.Position.y;
            Assert.AreEqual(field.Playground.Top + boss.Radius + 1f, startY, 1e-3f, "It appears just above the top edge.");
            Assert.AreEqual(Altitude.Ground, boss.Altitude);
            Run(1f);
            Assert.AreEqual(startY - 2.4f, boss.Position.y, 0.05f, "The ground carries it in.");
            Assert.IsFalse(boss.HasEntered, "It does not fight while the ground moves.");
            field.ScrollSpeed = 0f;
            Run(3f);
            Assert.IsTrue(boss.HasEntered, "It fights once the ground stands.");
            Assert.AreEqual(boss.HoldY, boss.Position.y, 0.1f, "It stands on its hold line.");
            Assert.AreEqual(5f, boss.HoldY, 1e-4f);
        }
    }
}
