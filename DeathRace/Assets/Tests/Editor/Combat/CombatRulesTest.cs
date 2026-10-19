using NUnit.Framework;
using UnityEngine;

namespace Portfolio.DeathRace.Tests
{
    /// <summary>The arithmetic of the fighting: armour, crashes and rams, blasts, the cone of a rocket.</summary>
    public class CombatRulesTest
    {
        [Test]
        public void ArmourTakesItsShareOfEveryHit()
        {
            Assert.AreEqual(10f, CombatRules.AfterArmour(10f, 1f), 0.0001f);
            var armoured = new CarLoadout { model = CarModel.Bulldog, armor = 2 };
            float taken = armoured.Stats().DamageTaken;
            Assert.AreEqual(1f - 2 * CarStats.ArmorPerLevelProtection, taken, 0.0001f);
            Assert.AreEqual(10f * taken, CombatRules.AfterArmour(10f, taken), 0.0001f);
            Assert.AreEqual(0f, CombatRules.AfterArmour(-5f, taken), "a negative hit heals nothing");
        }

        [Test]
        public void AHealthModelHitsThroughArmour()
        {
            var model = new HealthModel(100f, 1f, 0.82f);
            float taken = model.Apply(20f, DamageSource.Gun, 1, 0, 0f);
            Assert.AreEqual(16.4f, taken, 0.0001f);
            Assert.AreEqual(83.6f, model.Health, 0.0001f);
            Assert.AreEqual(0.836f, model.Health01, 0.0001f);
        }

        [Test]
        public void AHitNeverTakesMoreThanIsLeftAndADeadCarTakesNothing()
        {
            var model = new HealthModel(50f, 0.2f, 1f);
            Assert.AreEqual(10f, model.Health, 0.0001f);
            Assert.AreEqual(10f, model.Apply(30f, DamageSource.Rocket, 2, 0, 1f), 0.0001f);
            Assert.IsTrue(model.Dead);
            Assert.AreEqual(0f, model.Apply(30f, DamageSource.Rocket, 2, 0, 2f));
            Assert.AreEqual(0f, model.Repair(20f), "a wreck is not repaired on the road");
        }

        [Test]
        public void GunRoundsFollowPiercingRounds()
        {
            Assert.AreEqual(CombatRules.GunDamage, CombatRules.RoundDamage(new CarLoadout().Stats()), 0.0001f);
            Assert.AreEqual(CombatRules.GunDamage * 1.5f, CombatRules.RoundDamage(new CarLoadout { piercing = true }.Stats()), 0.0001f);
        }

        [Test]
        public void CrashesHurtOnlyBeyondTheThreshold()
        {
            Assert.AreEqual(0f, CombatRules.CrashDamage(5f));
            Assert.AreEqual(0f, CombatRules.CrashDamage(7f));
            Assert.AreEqual(11f, CombatRules.CrashDamage(17f), 0.0001f);
            Assert.AreEqual(11f, CombatRules.CrashDamage(-17f), 0.0001f, "the side of the normal does not matter");
            Assert.AreEqual(11f * CombatRules.WallShare, CombatRules.WallDamage(17f), 0.0001f);
        }

        [Test]
        public void ASpikedBumperBitesHarderAndSoaksMoreHeadOn()
        {
            CarStats spiked = new CarLoadout { spikes = true }.Stats();
            CarStats plain = new CarLoadout().Stats();
            float baseDamage = CombatRules.CrashDamage(17f);

            // The plain car rammed head on by the spiked one: the spikes bite.
            float plainTakes = CombatRules.RamDamage(17f, false, plain.RamTaken, true, spiked.RamDealt);
            Assert.AreEqual(baseDamage * 2.5f, plainTakes, 0.0001f);

            // The spiked car, hitting with its front: its bumper soaks half.
            float spikedTakes = CombatRules.RamDamage(17f, true, spiked.RamTaken, false, plain.RamDealt);
            Assert.AreEqual(baseDamage * 0.5f, spikedTakes, 0.0001f);

            // Side by side, nobody's front: no multipliers.
            Assert.AreEqual(baseDamage, CombatRules.RamDamage(17f, false, spiked.RamTaken, false, spiked.RamDealt), 0.0001f);

            // Two spiked cars head on: each takes 0.5 * 2.5 of the crash.
            Assert.AreEqual(baseDamage * 1.25f, CombatRules.RamDamage(17f, true, spiked.RamTaken, true, spiked.RamDealt), 0.0001f);
        }

        [Test]
        public void TheFrontIsTheConeAheadOfTheCar()
        {
            Vector2 north = Vector2.up;
            Assert.IsTrue(CombatRules.IsFront(north, new Vector2(0f, 2f)));
            Assert.IsTrue(CombatRules.IsFront(north, new Vector2(1f, 2f)));
            Assert.IsFalse(CombatRules.IsFront(north, new Vector2(1f, 0f)), "the side is not the front");
            Assert.IsFalse(CombatRules.IsFront(north, new Vector2(0f, -2f)));
            Assert.IsFalse(CombatRules.IsFront(north, Vector2.zero));
        }

        [Test]
        public void ABlastFallsOffFromItsCentreToItsEdge()
        {
            Assert.AreEqual(34f, CombatRules.BlastDamage(34f, 5f, 0f), 0.0001f);
            float previous = 34f;
            for (float distance = 0.5f; distance < 5f; distance += 0.5f)
            {
                float damage = CombatRules.BlastDamage(34f, 5f, distance);
                Assert.Less(damage, previous, $"falls off at {distance} m");
                Assert.Greater(damage, 34f * 0.2f - 0.0001f, $"at least a fifth inside, at {distance} m");
                previous = damage;
            }
            Assert.AreEqual(0f, CombatRules.BlastDamage(34f, 5f, 5f));
            Assert.AreEqual(0f, CombatRules.BlastDamage(34f, 5f, 9f));
            Assert.AreEqual(0f, CombatRules.BlastDamage(34f, 0f, 0f), "a blast of no size hurts nothing");
        }

        [Test]
        public void ABlastPushesHardestAtItsCentre()
        {
            Assert.AreEqual(1f, CombatRules.BlastPush(4f, 0f), 0.0001f);
            Assert.AreEqual(0.25f, CombatRules.BlastPush(4f, 2f), 0.0001f);
            Assert.AreEqual(0f, CombatRules.BlastPush(4f, 4f));
        }

        [Test]
        public void ARocketSeesOnlyTheConeAhead()
        {
            Vector2 forward = Vector2.up;
            Assert.IsTrue(CombatRules.InCone(forward, new Vector2(0f, 30f), CombatRules.RocketCone));
            Assert.IsTrue(CombatRules.InCone(forward, new Vector2(Mathf.Tan(19f * Mathf.Deg2Rad) * 30f, 30f), CombatRules.RocketCone));
            Assert.IsFalse(CombatRules.InCone(forward, new Vector2(Mathf.Tan(21f * Mathf.Deg2Rad) * 30f, 30f), CombatRules.RocketCone));
            Assert.IsFalse(CombatRules.InCone(forward, new Vector2(0f, -30f), CombatRules.RocketCone));
        }

        [Test]
        public void HowHurtACarLooks()
        {
            Assert.AreEqual(0, HealthModel.DamageStage(1f));
            Assert.AreEqual(0, HealthModel.DamageStage(0.5f));
            Assert.AreEqual(1, HealthModel.DamageStage(0.49f));
            Assert.AreEqual(1, HealthModel.DamageStage(0.25f));
            Assert.AreEqual(2, HealthModel.DamageStage(0.24f));
            Assert.AreEqual(2, HealthModel.DamageStage(0f));
        }
    }
}
