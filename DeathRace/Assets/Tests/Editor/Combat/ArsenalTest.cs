using System.Collections.Generic;
using NUnit.Framework;

namespace Portfolio.DeathRace.Tests
{
    /// <summary>The tables of the guns, the special weapons and the fittings: in the order of their enums, sound numbers, unique ids.</summary>
    public class ArsenalTest
    {
        [Test]
        public void EveryGunIsInTheTableInTheOrderOfItsKind()
        {
            Assert.AreEqual(System.Enum.GetValues(typeof(GunKind)).Length, Guns.Count);
            for (int i = 0; i < Guns.Count; i++)
            {
                GunSpec gun = Guns.All[i];
                Assert.AreEqual((GunKind)i, gun.Kind);
                Assert.AreSame(gun, Guns.Of(gun.Kind));
                Assert.Greater(gun.RoundsPerSecond, 0f, gun.Name);
                Assert.Greater(gun.Damage, 0f, gun.Name);
                Assert.Greater(gun.Range, 0f, gun.Name);
                Assert.Greater(gun.AmmoFactor, 0f, gun.Name);
                Assert.IsFalse(string.IsNullOrEmpty(gun.Name) || string.IsNullOrEmpty(gun.Blurb) || string.IsNullOrEmpty(gun.Icon), gun.Name);
            }
        }

        [Test]
        public void TheGunsAreWhatTheySay()
        {
            GunSpec machineGun = Guns.Of(GunKind.MachineGun);
            GunSpec minigun = Guns.Of(GunKind.Minigun);
            GunSpec flame = Guns.Of(GunKind.Flamethrower);
            GunSpec plasma = Guns.Of(GunKind.Plasma);
            Assert.AreEqual(0, machineGun.PriceBase, "the machine gun comes with the car");
            Assert.AreEqual(CombatRules.GunRoundsPerSecond, machineGun.RoundsPerSecond);
            Assert.AreEqual(CombatRules.GunDamage, machineGun.Damage);
            Assert.IsFalse(machineGun.IsFlame || machineGun.IsProjectile || minigun.IsFlame || minigun.IsProjectile, "the guns are hitscan");
            Assert.Greater(minigun.RoundsPerSecond, machineGun.RoundsPerSecond);
            Assert.Greater(minigun.Spread, machineGun.Spread, "the minigun strays more");
            Assert.IsTrue(flame.IsFlame);
            Assert.Less(flame.Range, machineGun.Range * 0.5f, "a flame reaches only so far");
            Assert.IsTrue(plasma.IsProjectile);
            Assert.Greater(plasma.Splash, 0f);
            foreach (GunSpec bought in new[] { minigun, flame, plasma })
            {
                // On paper the guns are close: what tells them apart is how they land (the flame never misses and licks
                // several cars, a bolt never strays and bursts), which whole races measure (CombatCheck -full).
                float perSecond = bought.DamagePerSecond + (bought.IsFlame ? CombatRules.BurnDamagePerSecond : 0f);
                Assert.That(perSecond, Is.InRange(machineGun.DamagePerSecond * 0.85f, machineGun.DamagePerSecond * 1.5f), bought.Name);
                Assert.Greater(bought.PriceBase, 0, bought.Name);
            }
            Assert.Greater(minigun.DamagePerSecond, machineGun.DamagePerSecond, "the minigun pours out more");
            Assert.Less(minigun.PriceBase, flame.PriceBase);
            Assert.Less(flame.PriceBase, plasma.PriceBase);
        }

        [Test]
        public void EveryGunsRoundsLastAboutAsLong()
        {
            // The ammunition a gun carries follows its rate: a full load lasts about as long whichever gun is mounted.
            GunSpec machineGun = Guns.Of(GunKind.MachineGun);
            float seconds = machineGun.AmmoFactor / machineGun.RoundsPerSecond;
            foreach (GunSpec gun in Guns.All)
            {
                Assert.That(gun.AmmoFactor / gun.RoundsPerSecond, Is.InRange(seconds * 0.8f, seconds * 1.3f), gun.Name);
            }
        }

        [Test]
        public void TheSpecialsAndFittingsHaveTheirOwnIds()
        {
            Assert.AreEqual(System.Enum.GetValues(typeof(SpecialKind)).Length, Specials.Count);
            Assert.AreEqual(System.Enum.GetValues(typeof(EquipmentKind)).Length, Equipment.Count);
            var ids = new HashSet<string>();
            for (int i = 0; i < Specials.Count; i++)
            {
                SpecialSpec special = Specials.All[i];
                Assert.AreEqual((SpecialKind)i, special.Kind);
                Assert.AreSame(special, Specials.Find(special.Id));
                Assert.Greater(special.Price, 0);
                Assert.Greater(special.Max, 0);
                Assert.IsTrue(ids.Add(special.Id), special.Id);
            }
            for (int i = 0; i < Equipment.Count; i++)
            {
                EquipmentSpec fitting = Equipment.All[i];
                Assert.AreEqual((EquipmentKind)i, fitting.Kind);
                Assert.AreSame(fitting, Equipment.Find(fitting.Id));
                Assert.Greater(fitting.PriceBase, 0);
                Assert.IsTrue(ids.Add(fitting.Id), fitting.Id);
            }
            foreach (GunSpec gun in Guns.All)
            {
                Assert.IsTrue(ids.Add(gun.Id), gun.Id);
            }
            Assert.AreEqual("spikes", Equipment.Of(EquipmentKind.Spikes).Id, "the first game's ids stay");
            Assert.AreEqual("piercing", Equipment.Of(EquipmentKind.Piercing).Id);
            Assert.IsNull(Specials.Find("nothing"));
            Assert.IsNull(Equipment.Find("nothing"));
        }
    }
}
