using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Portfolio.DeathRace.Tests
{
    /// <summary>What each pickup gives, how a cluster mixes its kinds, and how spots are grouped into clusters.</summary>
    public class PickupEffectTest
    {
        [Test]
        public void CashGivesWhatTheRacePaysPerPickup()
        {
            PickupEffect cash = PickupEffect.Of(PickupKind.Cash, 150, 120f, 150);
            Assert.AreEqual(150, cash.Cash);
            Assert.AreEqual(150, cash.Amount);
            Assert.AreEqual(0f, cash.Repair);
            Assert.AreEqual(0, cash.Ammo);
            Assert.AreEqual(0, cash.Nitro);
        }

        [Test]
        public void RepairGivesAQuarterOfTheMaximum()
        {
            PickupEffect repair = PickupEffect.Of(PickupKind.Repair, 50, 140f, 170);
            Assert.AreEqual(35f, repair.Repair, 0.0001f);
            Assert.AreEqual(35, repair.Amount);
            Assert.AreEqual(0, repair.Cash);
        }

        [Test]
        public void AmmoGivesHalfTheCapacityRoundedUp()
        {
            Assert.AreEqual(50, PickupEffect.Of(PickupKind.Ammo, 50, 100f, 100).Ammo);
            Assert.AreEqual(75, PickupEffect.Of(PickupKind.Ammo, 50, 100f, 150).Ammo);
            Assert.AreEqual(1, PickupEffect.Of(PickupKind.Ammo, 50, 100f, 1).Ammo);
        }

        [Test]
        public void NitroGivesOneCharge()
        {
            PickupEffect nitro = PickupEffect.Of(PickupKind.Nitro, 50, 100f, 100);
            Assert.AreEqual(1, nitro.Nitro);
            Assert.AreEqual(1, nitro.Amount);
        }

        [Test]
        public void ARepairPickupOnAHealthModelStopsAtTheMaximum()
        {
            var model = new HealthModel(100f, 0.9f, 1f);
            Assert.AreEqual(10f, model.Repair(PickupEffect.Of(PickupKind.Repair, 0, 100f, 0).Repair), 0.0001f);
            Assert.AreEqual(100f, model.Health, 0.0001f);
        }

        [Test]
        public void AmmoPickupOnAStockStopsAtTheCapacity()
        {
            var stock = new WeaponStock(100, 0, 0, 0);
            while (stock.RoundsFired < 80)
            {
                stock.Fire(true, 0.02f);
            }
            Assert.AreEqual(20, stock.Ammo);
            Assert.AreEqual(50, stock.AddAmmo(PickupEffect.Of(PickupKind.Ammo, 0, 0f, 100).Ammo));
            Assert.AreEqual(30, stock.AddAmmo(PickupEffect.Of(PickupKind.Ammo, 0, 0f, 100).Ammo));
            Assert.AreEqual(100, stock.Ammo);
        }

        [Test]
        public void EveryClusterOfTwoOrMoreMixesItsKinds()
        {
            var random = new System.Random(5);
            for (int round = 0; round < 300; round++)
            {
                int count = 2 + round % 4;
                List<PickupKind> kinds = PickupEffect.KindsFor(count, random);
                Assert.AreEqual(count, kinds.Count);
                Assert.IsFalse(kinds.TrueForAll(k => k == kinds[0]), $"round {round}: a cluster of {count} of one kind");
            }
        }

        [Test]
        public void TheSameSeedLaysTheSameField()
        {
            List<PickupKind> a = PickupEffect.KindsFor(12, new System.Random(42));
            List<PickupKind> b = PickupEffect.KindsFor(12, new System.Random(42));
            CollectionAssert.AreEqual(a, b);
        }

        [Test]
        public void EveryKindTurnsUp()
        {
            var seen = new HashSet<PickupKind>();
            var random = new System.Random(9);
            for (int i = 0; i < 50; i++)
            {
                seen.UnionWith(PickupEffect.KindsFor(3, random));
            }
            Assert.AreEqual(4, seen.Count);
        }

        [Test]
        public void SpotsThatLieTogetherFormACluster()
        {
            var spots = new List<Vector3>
            {
                new Vector3(0f, 0f, 0f), new Vector3(4f, 0f, 0f), new Vector3(-4f, 0f, 1f),
                new Vector3(100f, 0f, 0f), new Vector3(104f, 0f, 0f),
                new Vector3(0f, 0f, 200f)
            };
            List<List<Vector3>> clusters = PickupField.Clusters(spots);
            Assert.AreEqual(3, clusters.Count);
            Assert.AreEqual(3, clusters[0].Count);
            Assert.AreEqual(2, clusters[1].Count);
            Assert.AreEqual(1, clusters[2].Count);
        }
    }
}
