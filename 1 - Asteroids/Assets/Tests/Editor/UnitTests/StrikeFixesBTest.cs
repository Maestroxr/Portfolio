using System.Collections.Generic;
using Gamebox;
using NUnit.Framework;
using UnityEngine;

namespace Portfolio.Asteroids.Tests
{
    /// <summary>
    /// What the pilots of a shared strike mission see alike: the warning of a unit's attack travels, a beam burns for its
    /// own time on every client, a megabomb clears the screen it was set off on, the simulator's own kills count
    /// everywhere, a shot that only speeds up is not reported again, and an air boss comes in on puppets too.
    /// </summary>
    public class StrikeFixesBTest
    {
        private const float Step = 1f / 60f;

        private readonly TemporaryObjects objects = new TemporaryObjects();
        private SpaceField field;


        /// <summary>A link of a client that does not simulate: it only takes notes.</summary>
        private sealed class ReplicaLink : IFieldLink
        {
            public readonly List<Shot> Absorbed = new List<Shot>();
            public int Novas;

            public bool Simulates => false;
            public void BodyAdded(SpaceBody body) { }
            public void BodyRemoved(SpaceBody body) { }
            public void BlastSetOff(Blast blast) { }
            public void PuppetHit(Shootable puppet, DamageInfo hit) { }
            public void PuppetRammed(Shootable puppet, Vector2 direction, bool dashing) { }
            public void PuppetShotAbsorbed(Shot puppet) => Absorbed.Add(puppet);
            public void RewardTouched(Reward reward) { }
            public void ShotFired(Shot shot, byte kind, int level) { }
            public void NovaFired(Vector2 center, float damage, float bossDamage) => Novas++;
            public void RoomWanted(Vector2 center) { }
            public void ScrollReported(float distance, float speed) { }
        }


        [SetUp]
        public void SetUp()
        {
            Playground playground = objects.Component<Playground>("Playground");
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


        private void Run(float seconds)
        {
            for (float time = 0f; time < seconds - 1e-4f; time += Step)
            {
                field.Tick(Step);
            }
        }


        private GroundUnit Tower(bool puppet)
        {
            GroundUnit tower = objects.Component<GroundUnit>("Tower");
            tower.unit = StrikeUnit.LaserTower;
            tower.radius = 1f;
            tower.maxHealth = 20f;
            tower.pattern = AttackPattern.Laser;
            tower.interval = 1f;
            tower.telegraph = new GameObject("Glow");
            tower.telegraph.transform.SetParent(tower.transform, false);
            tower.IsPuppet = puppet;
            tower.Place(new StrikeEvent(), 0, Vector2.zero);
            field.Add(tower);
            return tower;
        }


        // ------------------------------------------------------------------ warnings

        [Test]
        public void TheWarningFlagTravelsBesideThePhase()
        {
            Assert.AreEqual(32u, (uint)BodyFlags.Warning, "The numbers travel: they must never move.");
            BodyFlags flags = BodyCodec.WithPhase(BodyFlags.Warning | BodyFlags.Shielded, 2);
            Assert.IsTrue(BodyCodec.Has(flags, BodyFlags.Warning));
            Assert.IsTrue(BodyCodec.Has(flags, BodyFlags.Shielded));
            Assert.AreEqual(2, BodyCodec.Phase(flags));
        }


        [Test]
        public void ALaserTowersGlowIsReportedAndShownOnItsPuppet()
        {
            GroundUnit tower = Tower(false);
            bool reported = false;
            for (int i = 0; i < 180 && !reported; i++)
            {
                field.Tick(Step);
                uint flags = FieldReplication.FlagsOf(tower);
                Assert.AreEqual(tower.IsWarning, BodyCodec.Has((BodyFlags)flags, BodyFlags.Warning), "The flag follows the glow.");
                reported = tower.IsWarning;
            }
            Assert.IsTrue(reported, "The tower glowed before its laser, and the simulator reports it.");

            GroundUnit puppet = Tower(true);
            Assert.IsFalse(puppet.telegraph.activeSelf);
            Run(3f);
            Assert.IsFalse(puppet.telegraph.activeSelf, "A puppet has no clock of its own.");
            puppet.ShowWarning(true);
            Assert.IsTrue(puppet.telegraph.activeSelf, "It glows while its simulator reports the warning.");
            field.Tick(Step);
            Assert.IsTrue(puppet.telegraph.activeSelf, "Its tick does not put the glow out.");
            puppet.ShowWarning(false);
            Assert.IsFalse(puppet.telegraph.activeSelf);
        }


        [Test]
        public void AnEnemyThatDoesNotWarnReportsNoWarning()
        {
            StrikeAircraft dart = objects.Component<StrikeAircraft>("Dart");
            dart.pattern = AttackPattern.None;
            dart.Launch(new StrikeEvent { path = FlightPath.StraightDown }, new Vector2(0f, 12f), 0f, 1f);
            field.Add(dart);
            Run(0.5f);
            Assert.IsFalse(BodyCodec.Has((BodyFlags)FieldReplication.FlagsOf(dart), BodyFlags.Warning));
            dart.ShowWarning(true);
            dart.ShowWarning(true);
            dart.ShowWarning(false);
        }


        // ------------------------------------------------------------------ the beam

        [Test]
        public void ABeamBurnsForItsTimeWhateverTheFrames()
        {
            float burned = 0f;
            float age = 0f;
            for (int i = 0; i < 60; i++)
            {
                age += Step;
                burned += EnemyBeam.BurnSeconds(age, Step, 0.17f);
            }
            Assert.AreEqual(0.17f, burned, 1e-4f, "Short frames add up to the beam's time.");
            Assert.AreEqual(0.17f, EnemyBeam.BurnSeconds(0.5f, 0.5f, 0.17f), 1e-5f, "One long frame burns no longer.");
            Assert.AreEqual(0f, EnemyBeam.BurnSeconds(0.4f, 0.1f, 0.17f), "Past its time it burns nothing.");
            Assert.AreEqual(0.1f, EnemyBeam.BurnSeconds(3f, 0.1f, 0f), 1e-6f, "A beam without an end burns every frame.");
        }


        private EnemyBeam Beam(bool puppet)
        {
            EnemyBeam beam = objects.Component<EnemyBeam>("Beam");
            beam.enemy = true;
            beam.visual = new GameObject("Visual").transform;
            beam.visual.SetParent(beam.transform, false);
            beam.IsPuppet = puppet;
            beam.Position = new Vector2(0f, 5f);
            field.Add(beam);
            return beam;
        }


        [Test]
        public void APuppetBeamStopsBurningOnItsOwnClockAndWaitsForItsExit()
        {
            EnemyBeam real = Beam(false);
            Assert.AreEqual(0.17f, real.Lifetime, 1e-5f, "A beam lives for its burn time.");
            Run(0.25f);
            Assert.IsFalse(real.InPlay, "The simulator's beam expires.");

            EnemyBeam puppet = Beam(true);
            puppet.BurnFor(0.1f);
            Run(0.05f);
            Assert.IsTrue(puppet.IsBurning);
            Assert.IsTrue(puppet.Visual.gameObject.activeSelf);
            Run(0.5f);
            Assert.IsTrue(puppet.InPlay, "Only the simulator's exit takes a puppet off.");
            Assert.IsFalse(puppet.IsBurning, "It stopped burning when the simulator's beam had no time left.");
            Assert.IsFalse(puppet.Visual.gameObject.activeSelf, "It shows no column while it waits.");
        }


        // ------------------------------------------------------------------ the megabomb of a guest

        [Test]
        public void AGuestsMegabombClearsTheEnemyShotsOnItsScreenAtOnce()
        {
            var link = new ReplicaLink();
            field.Link = link;
            Shot shot = objects.Component<Shot>("Flak");
            shot.enemy = true;
            shot.IsPuppet = true;
            shot.Position = new Vector2(0f, 2f);
            shot.Velocity = Vector2.down;
            field.Add(shot);
            CollectionAssert.Contains(field.EnemyShots, shot);
            field.Nova(Vector2.zero, 100f, 50f);
            Assert.AreEqual(1, link.Novas, "The simulator hears of it.");
            Assert.IsFalse(shot.InPlay, "The shot is gone from this screen at once.");
            Assert.IsEmpty(link.Absorbed, "Nothing is reported for it: the simulator clears the real shots itself.");
            field.Link = null;
        }


        // ------------------------------------------------------------------ the simulator's own kills

        [Test]
        public void TheSimulatorsOwnKillNamesItsSeatInAStrike()
        {
            GroundUnit unit = Tower(false);
            unit.pattern = AttackPattern.None;
            unit.TakeHit(new DamageInfo(1000f, Vector2.up, unit.Position, DamageSource.PlayerShot, true));
            Assert.IsFalse(unit.InPlay);
            Assert.AreEqual(ExitReason.Destroyed, unit.Exit);
            Assert.IsNull(unit.ExitSeat, "The local pilot's kill has no seat of another device.");
            Assert.IsTrue(unit.ExitByPlayer);
            Assert.AreEqual(2, FieldReplication.ExitSeatOf(unit, 2, true), "The others count it as the simulator's kill.");
            Assert.AreEqual(byte.MaxValue, FieldReplication.ExitSeatOf(unit, 2, false), "The asteroid field keeps its exits.");

            GroundUnit other = Tower(false);
            other.TakeHit(new DamageInfo(1000f, Vector2.up, other.Position, DamageSource.PlayerShot, true) { Seat = 3 });
            Assert.AreEqual(3, FieldReplication.ExitSeatOf(other, 2, true), "A guest's kill names the guest.");

            GroundUnit blown = Tower(false);
            blown.TakeHit(new DamageInfo(1000f, Vector2.up, blown.Position, DamageSource.Explosion, false));
            Assert.AreEqual(byte.MaxValue, FieldReplication.ExitSeatOf(blown, 2, true), "Nobody's kill names nobody.");
        }


        // ------------------------------------------------------------------ shots that speed up

        [Test]
        public void AShotThatOnlySpeedsUpIsNotSteering()
        {
            Shot shot = objects.Component<Shot>("Rocket");
            shot.enemy = true;
            shot.acceleration = 22f;
            shot.maxSpeed = 23f;
            shot.Velocity = new Vector2(0f, -5f);
            Vector2 sent = shot.Velocity;
            float since = 0f;
            for (int i = 0; i < 90; i++)
            {
                shot.Tick(Step);
                since += Step;
                Vector2 predicted = FieldReplication.Predicted(shot, sent, since);
                Assert.Less((shot.Velocity - predicted).magnitude, 0.35f, $"No report needed after {since:0.00} s.");
            }
            Assert.AreEqual(23f, shot.Velocity.magnitude, 1e-3f, "It reached its top speed.");

            shot.Velocity = new Vector2(-23f, 0f);
            Assert.Greater((shot.Velocity - FieldReplication.Predicted(shot, sent, since)).magnitude, 0.35f,
                "A change of course is still steering.");

            Shot plain = objects.Component<Shot>("Plasma");
            Assert.AreEqual(new Vector2(0f, -9f), FieldReplication.Predicted(plain, new Vector2(0f, -9f), 1f),
                "A shot that keeps its speed keeps its velocity.");
        }


        // ------------------------------------------------------------------ an air boss comes in

        [Test]
        public void AnAirBossPuppetComesInLikeTheSimulatorsBoss()
        {
            StrikeBoss puppet = objects.Component<StrikeBoss>("Boss");
            puppet.radius = 3f;
            puppet.maxHealth = 100f;
            puppet.entrySeconds = 2.5f;
            puppet.attacks = new BossAttack[0];
            puppet.IsPuppet = true;
            field.Add(puppet);
            float startY = puppet.Position.y;
            Assert.AreEqual(field.Playground.Top + puppet.Radius + 1f, startY, 1e-3f, "It appears just above the top edge.");
            Run(1f);
            Assert.Less(puppet.Position.y, startY - 3f, "It flies down without a report.");
            Run(1.5f);
            Assert.AreEqual(puppet.HoldY, puppet.Position.y, 0.3f, "It is at its hover line when the simulator's starts to fight.");
        }
    }
}
