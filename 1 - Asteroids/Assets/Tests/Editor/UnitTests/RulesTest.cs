using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Portfolio.Asteroids.Tests
{
    public class RulesTest
    {
        [Test]
        public void LargeAsteroidsSplitIntoMediumOnes()
        {
            Assert.That(AsteroidRules.SplitCount(AsteroidKind.Rock, AsteroidSize.Large, 0.9f), Is.EqualTo(2));
            Assert.That(AsteroidRules.SplitCount(AsteroidKind.Rock, AsteroidSize.Large, 0.1f), Is.EqualTo(3));
            Assert.That(AsteroidRules.SplitSize(AsteroidKind.Rock, AsteroidSize.Large), Is.EqualTo(AsteroidSize.Medium));
            Assert.That(AsteroidRules.SplitSize(AsteroidKind.Rock, AsteroidSize.Medium), Is.EqualTo(AsteroidSize.Small));
            Assert.That(AsteroidRules.SplitCount(AsteroidKind.Rock, AsteroidSize.Small, 0f), Is.Zero, "Small asteroids do not split.");
        }

        [Test]
        public void IceShattersIntoSmallShards()
        {
            Assert.That(AsteroidRules.SplitCount(AsteroidKind.Ice, AsteroidSize.Large, 0.5f), Is.EqualTo(4));
            Assert.That(AsteroidRules.SplitSize(AsteroidKind.Ice, AsteroidSize.Large), Is.EqualTo(AsteroidSize.Small));
        }

        [Test]
        public void SmallAsteroidsAreWorthTheMost()
        {
            foreach (AsteroidKind kind in System.Enum.GetValues(typeof(AsteroidKind)))
            {
                Assert.Greater(AsteroidRules.Score(kind, AsteroidSize.Small), AsteroidRules.Score(kind, AsteroidSize.Large), kind.ToString());
                Assert.Greater(AsteroidRules.Radius(kind, AsteroidSize.Large), AsteroidRules.Radius(kind, AsteroidSize.Small), kind.ToString());
                Assert.GreaterOrEqual(AsteroidRules.Health(kind, AsteroidSize.Large), AsteroidRules.Health(kind, AsteroidSize.Small), kind.ToString());
            }
        }

        [Test]
        public void OreDropsCrystalsAndVoidCrystalsReleaseShards()
        {
            Assert.That(AsteroidRules.CrystalDrops(AsteroidKind.Ore, AsteroidSize.Large), Is.EqualTo(3));
            Assert.That(AsteroidRules.CrystalDrops(AsteroidKind.Rock, AsteroidSize.Large), Is.Zero);
            Assert.That(AsteroidRules.ShardCount(AsteroidKind.Crystal, AsteroidSize.Small), Is.EqualTo(2));
            Assert.That(AsteroidRules.ShardCount(AsteroidKind.Magma, AsteroidSize.Large), Is.Zero);
        }

        [Test]
        public void EveryWeaponLevelFiresMoreOrHarder()
        {
            var barrels = new List<Barrel>();
            foreach (WeaponType type in System.Enum.GetValues(typeof(WeaponType)))
            {
                int previousCount = 0;
                float previousPower = 0f;
                for (int level = 1; level <= WeaponRules.MaxLevel; level++)
                {
                    WeaponRules.Volley(type, level, barrels);
                    Assert.Greater(barrels.Count, 0, $"{type} level {level} fires nothing.");
                    Assert.Greater(WeaponRules.Cooldown(type, level), 0f);
                    float power = barrels.Count * WeaponRules.Damage(type, level) * WeaponRules.Pierce(type, level) / WeaponRules.Cooldown(type, level);
                    Assert.GreaterOrEqual(barrels.Count, previousCount, $"{type} level {level} fires fewer shots.");
                    Assert.Greater(power, previousPower, $"{type} level {level} is not stronger than level {level - 1}.");
                    previousCount = barrels.Count;
                    previousPower = power;
                }
            }
        }

        [Test]
        public void WeaponCratesRaiseTheLevelUpToTheMaximum()
        {
            var weapons = new ShipWeapons();
            int changes = 0;
            weapons.Changed += () => changes++;
            weapons.Upgrade(WeaponType.Laser);
            Assert.That(weapons.Type, Is.EqualTo(WeaponType.Laser));
            Assert.That(weapons.Level, Is.EqualTo(2));
            for (int i = 0; i < 10; i++)
            {
                weapons.Upgrade(WeaponType.Scatter);
            }
            Assert.That(weapons.Level, Is.EqualTo(WeaponRules.MaxLevel));
            weapons.Downgrade();
            Assert.That(weapons.Level, Is.EqualTo(WeaponRules.MaxLevel - 1));
            Assert.That(weapons.Type, Is.EqualTo(WeaponType.Scatter));
            Assert.Greater(changes, 10);
        }

        [Test]
        public void CombosMultiplyPointsAndBreakWhenIdle()
        {
            var score = new ScoreKeeper();
            for (int i = 0; i < 4; i++)
            {
                Assert.That(score.AddKill(100), Is.EqualTo(100));
            }
            Assert.That(score.AddKill(100), Is.EqualTo(200), "The fifth kill in a row doubles.");
            Assert.That(score.Multiplier, Is.EqualTo(2));
            score.Tick(ScoreKeeper.ComboWindow + 0.1f);
            Assert.That(score.Combo, Is.Zero, "The combo breaks without a kill in the window.");
            Assert.That(score.AddKill(100), Is.EqualTo(100));
            Assert.That(ScoreKeeper.MultiplierFor(40), Is.EqualTo(5));
            Assert.That(score.Score, Is.EqualTo(700));
        }

        [Test]
        public void LootPicksByWeight()
        {
            var loot = ScriptableObject.CreateInstance<Loot>();
            var first = new GameObject("First").AddComponent<HealthReward>();
            var second = new GameObject("Second").AddComponent<ShieldReward>();
            try
            {
                loot.Rewards = new List<Reward> { first, second };
                loot.Weights = new List<int> { 1, 3 };
                Assert.That(loot.TotalWeight, Is.EqualTo(4));
                Assert.That(loot.Pick(0.1f), Is.SameAs(first));
                Assert.That(loot.Pick(0.3f), Is.SameAs(second));
                Assert.That(loot.Pick(0.99f), Is.SameAs(second));
            }
            finally
            {
                Object.DestroyImmediate(first.gameObject);
                Object.DestroyImmediate(second.gameObject);
                Object.DestroyImmediate(loot);
            }
        }

        [Test]
        public void ObjectivesCompleteOnTheirOwnTerms()
        {
            var clear = new MissionObjective(LevelObjective.ClearWaves, 0, 3, null);
            clear.WaveCleared();
            clear.WaveCleared();
            Assert.IsFalse(clear.IsComplete);
            clear.WaveCleared();
            Assert.IsTrue(clear.IsComplete);

            var survive = new MissionObjective(LevelObjective.Survive, 60, 3, null);
            survive.Survive(59f);
            Assert.IsFalse(survive.IsComplete);
            survive.Survive(1.5f);
            Assert.IsTrue(survive.IsComplete);

            var collect = new MissionObjective(LevelObjective.Collect, 2, 3, null);
            collect.CrystalCollected();
            Assert.That(collect.Progress, Is.EqualTo(0.5f));
            collect.CrystalCollected();
            Assert.IsTrue(collect.IsComplete);

            var boss = new MissionObjective(LevelObjective.Boss, 0, 1, "Rock Titan");
            Assert.That(boss.Briefing, Does.Contain("Rock Titan"));
            boss.Defeat();
            Assert.IsTrue(boss.IsComplete);

            Assert.IsFalse(new MissionObjective(LevelObjective.Endless, 0, 0, null).IsComplete, "Endless missions never complete.");
        }
    }
}
