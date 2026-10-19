using NUnit.Framework;

namespace Portfolio.DeathRace.Tests
{
    /// <summary>Who a wreck counts for, and the grace against the same crash landing again and again.</summary>
    public class KillCreditTest
    {
        private const int Self = 0;
        private const int Rival = 2;
        private const int Other = 3;

        [Test]
        public void TheCarThatDealtTheLastBlowGetsTheKill()
        {
            var model = new HealthModel(10f, 1f, 1f);
            model.Apply(4f, DamageSource.Gun, Other, Self, 1f);
            model.Apply(20f, DamageSource.Rocket, Rival, Self, 2f);
            Assert.IsTrue(model.Dead);
            Assert.AreEqual(Rival, model.KillerOf(Rival, Self, 2f));
        }

        [Test]
        public void PushedIntoTheWallIsTheKillOfWhoeverPushed()
        {
            var model = new HealthModel(10f, 1f, 1f);
            model.Apply(4f, DamageSource.Ram, Rival, Self, 5f);
            model.Apply(20f, DamageSource.Wall, HealthModel.Nobody, Self, 5f + CombatRules.AssistWindow - 0.5f);
            Assert.AreEqual(Rival, model.KillerOf(HealthModel.Nobody, Self, 5f + CombatRules.AssistWindow - 0.5f));
        }

        [Test]
        public void ALongAgoHitDoesNotEarnAWallKill()
        {
            var model = new HealthModel(10f, 1f, 1f);
            model.Apply(4f, DamageSource.Gun, Rival, Self, 1f);
            float later = 1f + CombatRules.AssistWindow + 0.5f;
            model.Apply(20f, DamageSource.Wall, HealthModel.Nobody, Self, later);
            Assert.AreEqual(HealthModel.Nobody, model.KillerOf(HealthModel.Nobody, Self, later));
        }

        [Test]
        public void OnesOwnMineIsNobodysKillUnlessSomeoneHelped()
        {
            var alone = new HealthModel(10f, 1f, 1f);
            alone.Apply(40f, DamageSource.Mine, Self, Self, 3f);
            Assert.AreEqual(HealthModel.Nobody, alone.KillerOf(Self, Self, 3f));
            Assert.AreEqual(HealthModel.Nobody, alone.LastAttacker, "hurting oneself is no one's attack");

            var helped = new HealthModel(10f, 1f, 1f);
            helped.Apply(3f, DamageSource.Gun, Rival, Self, 2f);
            helped.Apply(40f, DamageSource.Mine, Self, Self, 3f);
            Assert.AreEqual(Rival, helped.KillerOf(Self, Self, 3f));
        }

        [Test]
        public void TheSameCrashCannotLandTwiceInAMoment()
        {
            var model = new HealthModel(100f, 1f, 1f);
            Assert.AreEqual(10f, model.Apply(10f, DamageSource.Ram, Rival, Self, 1f), 0.0001f);
            Assert.AreEqual(0f, model.Apply(10f, DamageSource.Ram, Rival, Self, 1f + CombatRules.CrashCooldown * 0.5f), "the same rammer, grinding");
            Assert.AreEqual(10f, model.Apply(10f, DamageSource.Ram, Other, Self, 1f + CombatRules.CrashCooldown * 0.5f), 0.0001f, "another car's ram lands");
            Assert.AreEqual(10f, model.Apply(10f, DamageSource.Ram, Rival, Self, 1f + CombatRules.CrashCooldown + 0.01f), 0.0001f, "after the cooldown");
            Assert.AreEqual(10f, model.Apply(10f, DamageSource.Wall, HealthModel.Nobody, Self, 1.2f), 0.0001f, "a barrier is another source");
            Assert.AreEqual(0f, model.Apply(10f, DamageSource.Wall, HealthModel.Nobody, Self, 1.3f));
        }

        [Test]
        public void BulletsAreNeverHeldBack()
        {
            var model = new HealthModel(100f, 1f, 1f);
            for (int i = 0; i < 9; i++)
            {
                Assert.AreEqual(2.2f, model.Apply(2.2f, DamageSource.Gun, Rival, Self, 1f + i * 0.01f), 0.0001f);
            }
        }

        [Test]
        public void WearIsSteadyAndThroughArmour()
        {
            var model = new HealthModel(100f, 1f, 0.5f);
            for (int i = 0; i < 10; i++)
            {
                Assert.AreEqual(0.5f, model.Wear(1f), 0.0001f);
            }
            Assert.AreEqual(95f, model.Health, 0.001f);
        }
    }
}
