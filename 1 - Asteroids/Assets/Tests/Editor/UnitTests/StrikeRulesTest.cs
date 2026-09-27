using NUnit.Framework;
using UnityEngine;

namespace Portfolio.Asteroids.Tests
{
    public class StrikeRulesTest
    {
        [Test]
        public void EverySpecialHasAVolley()
        {
            for (var item = StrikeWeaponRules.FirstSpecial; item <= StrikeWeaponRules.LastSpecial; item++)
            {
                StrikeWeaponInfo info = StrikeWeaponRules.Info(item);
                if (item == StrikeItem.PowerDisrupter)
                {
                    Assert.IsFalse(StrikeWeaponRules.IsSpecial(item), "The disrupter is cut.");
                    Assert.IsFalse(info.Fires);
                    continue;
                }
                Assert.IsTrue(StrikeWeaponRules.IsSpecial(item), item.ToString());
                Assert.That(info.Group, Is.EqualTo(ItemGroup.Special), item.ToString());
                Assert.IsTrue(info.Fires, $"{item} fires.");
                Assert.That(info.Barrels, Is.GreaterThanOrEqualTo(1), item.ToString());
                Assert.That(info.Damage + info.BurstDamage, Is.GreaterThan(0f), $"{item} hurts.");
                Assert.IsTrue(info.Beam || info.Hitscan || info.Speed > 0f, $"{item} reaches its target.");
                if (info.Beam)
                {
                    Assert.That(info.BeamOn, Is.InRange(0.01f, info.Rate));
                }
            }
            foreach (StrikeItem item in StrikeWeaponRules.AlwaysOn)
            {
                Assert.That(StrikeWeaponRules.Group(item), Is.EqualTo(ItemGroup.AlwaysOn));
                Assert.IsTrue(StrikeWeaponRules.Info(item).Fires);
            }
        }


        [Test]
        public void MasksAndRatesFollowTheArsenal()
        {
            Altitude A = Altitude.Air, G = Altitude.Ground, B = Altitude.Both;
            var expected = new (StrikeItem item, Altitude mask, float rate)[]
            {
                (StrikeItem.MachineGun, B, 0.086f), (StrikeItem.PlasmaCannon, A, 0.43f), (StrikeItem.MicroMissiles, B, 0.17f),
                (StrikeItem.Dumbfire, B, 0.43f), (StrikeItem.MiniGun, B, 0.043f), (StrikeItem.LaserTurret, A, 0.26f),
                (StrikeItem.MissilePods, A, 0.21f), (StrikeItem.AirMissiles, A, 0.43f), (StrikeItem.GroundMissiles, G, 0.86f),
                (StrikeItem.Bombs, G, 1.29f), (StrikeItem.PulseCannon, B, 0.13f), (StrikeItem.Deathray, B, 0.3f),
                (StrikeItem.TwinLaser, B, 0.3f)
            };
            foreach ((StrikeItem item, Altitude mask, float rate) in expected)
            {
                Assert.That(StrikeWeaponRules.Mask(item), Is.EqualTo(mask), item.ToString());
                Assert.That(StrikeWeaponRules.Info(item).Rate, Is.EqualTo(rate).Within(1e-5f), item.ToString());
            }
            Assert.That(StrikeWeaponRules.Info(StrikeItem.Bombs).BurstRadius, Is.EqualTo(2.2f));
            Assert.That(StrikeWeaponRules.Info(StrikeItem.Bombs).BurstDamage, Is.EqualTo(50f));
            Assert.That(StrikeWeaponRules.Info(StrikeItem.TwinLaser).Barrels, Is.EqualTo(2));
            Assert.IsTrue(StrikeWeaponRules.Info(StrikeItem.LaserTurret).Hitscan);
            Assert.IsTrue(StrikeWeaponRules.Info(StrikeItem.MiniGun).AutoTrack);
            Assert.That(StrikeWeaponRules.Group(StrikeItem.MegaBomb), Is.EqualTo(ItemGroup.Consumable));
            Assert.That(StrikeWeaponRules.Group(StrikeItem.IonScanner), Is.EqualTo(ItemGroup.Equipment));
        }


        [Test]
        public void UnitsSitOnTheirLayer()
        {
            int air = 0;
            int ground = 0;
            foreach (StrikeUnit unit in StrikeUnitRules.All)
            {
                StrikeUnitInfo info = StrikeUnitRules.Info(unit);
                Assert.That(info.Unit, Is.EqualTo(unit));
                Assert.That(info.Health, Is.GreaterThan(0f), unit.ToString());
                Assert.That(info.Radius, Is.GreaterThan(0f), unit.ToString());
                Assert.That(info.Bounty, Is.GreaterThan(0), unit.ToString());
                if (StrikeUnitRules.IsAir(unit))
                {
                    air++;
                    Assert.That(info.Layer, Is.EqualTo(Altitude.Air), unit.ToString());
                    Assert.That(info.Speed, Is.GreaterThan(0f), unit.ToString());
                    Assert.That(StrikeUnitRules.PoolIndex(unit), Is.InRange(0, StrikeUnitRules.AirCount - 1));
                }
                else
                {
                    ground++;
                    Assert.That(info.Layer, Is.EqualTo(Altitude.Ground), unit.ToString());
                    Assert.That(StrikeUnitRules.PoolIndex(unit), Is.InRange(0, StrikeUnitRules.GroundCount - 1));
                }
            }
            Assert.That(air, Is.EqualTo(StrikeUnitRules.AirCount));
            Assert.That(ground, Is.EqualTo(StrikeUnitRules.GroundCount), "The eleven ground units of the table plus the crate.");
            StrikeUnitInfo dart = StrikeUnitRules.Info(StrikeUnit.Dart);
            Assert.That((dart.Health, dart.Radius, dart.Bounty, dart.Speed), Is.EqualTo((4f, 0.9f, 150, 11f)));
            StrikeUnitInfo depot = StrikeUnitRules.Info(StrikeUnit.Depot);
            Assert.IsTrue(depot.Explodes && depot.AlwaysDrops);
            Assert.That(depot.BlastRadius, Is.EqualTo(4.5f));
            Assert.IsTrue(StrikeUnitRules.Info(StrikeUnit.Transport).AlwaysDrops);
            Assert.That(StrikeUnitRules.Shot(EnemyShotKind.Flak).Damage, Is.EqualTo(2f));
            Assert.That(StrikeUnitRules.Shot(EnemyShotKind.Beam).Damage, Is.EqualTo(40f));
        }


        [Test]
        public void DifficultyAndCoopNumbers()
        {
            Assert.That(StrikeRules.DamageTaken(StrikeDifficulty.Rookie), Is.EqualTo(0.5f));
            Assert.That(StrikeRules.DamageTaken(StrikeDifficulty.Veteran), Is.EqualTo(1f));
            Assert.That(StrikeRules.DamageTaken(StrikeDifficulty.Elite), Is.EqualTo(1f));
            Assert.IsTrue(StrikeRules.RegeneratesEnergy(StrikeDifficulty.Veteran));
            Assert.IsFalse(StrikeRules.RegeneratesEnergy(StrikeDifficulty.Elite));
            Assert.That(StrikeRules.BossHealthScale(StrikeDifficulty.Rookie, 1), Is.EqualTo(0.5f));
            Assert.That(StrikeRules.BossHealthScale(StrikeDifficulty.Veteran, 4), Is.EqualTo(2.5f));
            Assert.That(StrikeRules.BossHealthScale(StrikeDifficulty.Rookie, 2), Is.EqualTo(0.75f));
            Assert.That(StrikeRules.BossBurstScale(StrikeDifficulty.Rookie), Is.EqualTo(0.75f));
        }


        [Test]
        public void ShipMoneyAndPlayfieldNumbers()
        {
            Assert.That(StrikeRules.HalfSize.x, Is.EqualTo(17.7778f).Within(1e-3f));
            Assert.That(StrikeRules.HalfSize.y, Is.EqualTo(10f));
            Assert.That(StrikeRules.ShipMaxSpeed(11f), Is.EqualTo(17f).Within(1e-4f));
            Assert.That(StrikeRules.ShipMaxSpeed(1f), Is.EqualTo(17f * 0.85f).Within(1e-4f));
            Assert.That(StrikeRules.ShipMaxSpeed(99f), Is.EqualTo(17f * 1.2f).Within(1e-4f));
            Assert.That(StrikeRules.RamPilotDamage(1.8f), Is.EqualTo(4.5f).Within(1e-5f));
            Assert.That(StrikeRules.RamPilotDamage(6.4f), Is.EqualTo(6f).Within(1e-5f), "Big units hurt at most as much as a 2.4 m one.");
            foreach (int money in new[] { 0, 50, 35200, 55700, 76000, 93800, 122500 })
            {
                Assert.IsTrue(StrikeRules.IsMoneyValue(money), money.ToString());
            }
            Assert.IsFalse(StrikeRules.IsMoneyValue(1000));
            Assert.That(StrikeRules.AddToWallet(9999000, 5000), Is.EqualTo(9999999));
            Assert.That(StrikeRules.AddToWallet(int.MaxValue, int.MaxValue), Is.EqualTo(9999999));
            Assert.That(StrikeRules.AddToWallet(100, -500), Is.Zero);
            Assert.That(Mathf.RoundToInt(StrikeRules.StarDamage), Is.EqualTo(30));
        }
    }
}
