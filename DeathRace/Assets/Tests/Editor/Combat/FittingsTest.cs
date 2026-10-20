using NUnit.Framework;
using UnityEngine;

namespace Portfolio.DeathRace.Tests
{
    /// <summary>
    /// The arithmetic of the workshop's fittings and the new upgrades (<see cref="CarStats"/>, <see cref="CarHandling"/>,
    /// <see cref="CombatRules"/>): the roll cage, blast shielding, the radar jammer, the field welder, the extended
    /// magazine, the gun mount, the nitro system, and the guns' damage and reach. No engine needed.
    /// </summary>
    public class FittingsTest
    {
        [Test]
        public void AStockCarHasNoneOfThem()
        {
            CarStats stats = new CarLoadout { model = CarModel.Viper }.Stats();
            Assert.AreEqual(1f, stats.CrashTaken);
            Assert.AreEqual(1f, stats.BlastTaken);
            Assert.AreEqual(1f, stats.GunRate);
            Assert.AreEqual(1f, stats.GunDamage);
            Assert.AreEqual(1f, stats.NitroPower);
            Assert.AreEqual(CombatRules.NitroSeconds, stats.NitroSeconds, 1e-5f);
            Assert.IsFalse(stats.Jammer);
            Assert.IsFalse(stats.Welder);
            Assert.AreEqual(GunKind.MachineGun, stats.Gun);
            Assert.AreEqual(CarSpecs.Of(CarModel.Viper).AmmoCapacity, stats.AmmoCapacity);
        }

        [Test]
        public void TheCageSoaksCrashesAndTheShieldBlasts()
        {
            CarStats caged = new CarLoadout { model = CarModel.Viper, cage = true }.Stats();
            CarStats shielded = new CarLoadout { model = CarModel.Viper, shield = true }.Stats();
            Assert.AreEqual(CarStats.CageCrashTaken, caged.CrashTaken);
            Assert.AreEqual(1f, caged.BlastTaken, "a cage does nothing against a blast");
            Assert.AreEqual(CarStats.ShieldBlastTaken, shielded.BlastTaken);
            Assert.AreEqual(1f, shielded.CrashTaken);
            Assert.AreEqual(20f * CarStats.CageCrashTaken, CombatRules.CrashTaken(20f, caged), 1e-4f);
            Assert.AreEqual(20f, CombatRules.CrashTaken(20f, shielded), 1e-4f);
            Assert.AreEqual(20f * CarStats.ShieldBlastTaken, CombatRules.BlastTaken(20f, shielded), 1e-4f);
            Assert.AreEqual(20f, CombatRules.BlastTaken(20f, default), 1e-4f, "stats of nothing take it all");
            Assert.AreEqual(0f, CombatRules.CrashTaken(-3f, caged), "nothing negative");
        }

        [Test]
        public void TheJammerAndTheWelderAreFlags()
        {
            Assert.IsTrue(new CarLoadout { jammer = true }.Stats().Jammer);
            Assert.IsTrue(new CarLoadout { welder = true }.Stats().Welder);
        }

        [Test]
        public void TheMagazineAndTheGunMountCarryMoreRounds()
        {
            int stock = new CarLoadout { model = CarModel.Tyrant }.Stats().AmmoCapacity;
            Assert.AreEqual(Mathf.RoundToInt(stock * CarStats.MagazineAmmo), new CarLoadout { model = CarModel.Tyrant, magazine = true }.Stats().AmmoCapacity);
            CarStats mounted = new CarLoadout { model = CarModel.Tyrant, gunMount = 2 }.Stats();
            Assert.AreEqual(Mathf.RoundToInt(stock * (1f + 2 * CarStats.GunMountPerLevelAmmo)), mounted.AmmoCapacity);
            Assert.AreEqual(1f + 2 * CarStats.GunMountPerLevelRate, mounted.GunRate, 1e-5f);
            Assert.AreEqual(1f + 2 * CarStats.GunMountPerLevelDamage, mounted.GunDamage, 1e-5f);
            CarStats both = new CarLoadout { model = CarModel.Tyrant, gunMount = 2, piercing = true }.Stats();
            Assert.AreEqual(CarStats.PiercingDamage * (1f + 2 * CarStats.GunMountPerLevelDamage), both.GunDamage, 1e-5f, "they multiply");
        }

        [Test]
        public void AGunCarriesItsShareOfRoundsAndOnlyAnOwnedGunIsMounted()
        {
            var loadout = new CarLoadout { model = CarModel.Reaper, gun = GunKind.Plasma };
            Assert.AreEqual(GunKind.MachineGun, loadout.Stats().Gun, "a gun not bought is not on the bonnet");
            loadout.GiveGun(GunKind.Plasma);
            CarStats stats = loadout.Stats();
            Assert.AreEqual(GunKind.Plasma, stats.Gun);
            Assert.AreEqual(Mathf.RoundToInt(CarSpecs.Of(CarModel.Reaper).AmmoCapacity * CombatRules.PlasmaAmmoFactor), stats.AmmoCapacity);
            Assert.IsTrue(loadout.OwnsGun(GunKind.MachineGun), "the machine gun is always there");
            loadout.GiveGun(GunKind.Minigun, false);
            Assert.AreEqual(GunKind.Plasma, loadout.Stats().Gun, "bought, not mounted");
            Assert.IsTrue(loadout.OwnsGun(GunKind.Minigun));
        }

        [Test]
        public void ARoundOfEachGunHitsForItsDamageTimesTheCars()
        {
            CarStats stock = new CarLoadout().Stats();
            CarStats strong = new CarLoadout { model = CarModel.Tyrant, piercing = true, gunMount = 3 }.Stats();
            foreach (GunSpec gun in Guns.All)
            {
                Assert.AreEqual(gun.Damage, CombatRules.RoundDamage(stock, gun), 1e-5f, gun.Name);
                Assert.AreEqual(gun.Damage * strong.GunDamage, CombatRules.RoundDamage(strong, gun), 1e-5f, gun.Name);
            }
            Assert.AreEqual(CombatRules.RoundDamage(strong), CombatRules.RoundDamage(strong, Guns.Of(GunKind.MachineGun)), 1e-5f);
        }

        [Test]
        public void TheNitroSystemBurnsLongerAndPushesHarder()
        {
            CarStats stock = new CarLoadout { model = CarModel.Viper }.Stats();
            CarStats boosted = new CarLoadout { model = CarModel.Viper, boost = 3 }.Stats();
            Assert.AreEqual(CombatRules.NitroSeconds * (1f + 3 * CarStats.BoostPerLevelTime), boosted.NitroSeconds, 1e-4f);
            Assert.AreEqual(1f + 3 * CarStats.BoostPerLevelPower, boosted.NitroPower, 1e-5f);
            var plain = new CarHandling(stock);
            var strong = new CarHandling(boosted);
            Assert.AreEqual(plain.TopSpeed * CarHandling.NitroTopSpeed, plain.TopSpeedWith(true), 1e-3f, "stock nitro is the old +35 %");
            Assert.AreEqual(plain.TopSpeed, strong.TopSpeed, 1e-4f, "without nitro the system adds nothing");
            float extra = (CarHandling.NitroTopSpeed - 1f) * boosted.NitroPower;
            Assert.AreEqual(strong.TopSpeed * (1f + extra), strong.TopSpeedWith(true), 1e-3f, "the extra speed grows by the power");
            Assert.Greater(strong.DriveAcceleration(10f, true), plain.DriveAcceleration(10f, true));
            Assert.AreEqual(plain.DriveAcceleration(10f, false), strong.DriveAcceleration(10f, false), 1e-4f);
            var charged = new WeaponStock(100, 0, 0, 1, 0, 0, 0, 0f, boosted.NitroSeconds);
            Assert.AreEqual(boosted.NitroSeconds, charged.NitroSeconds, 1e-5f);
        }

        [Test]
        public void AJammedEngineHasLessTopSpeed()
        {
            var handling = new CarHandling(new CarLoadout { model = CarModel.Viper }.Stats());
            float jammed = handling.TopSpeed * CombatRules.EmpTopSpeed;
            Assert.Greater(handling.DriveAcceleration(jammed - 2f, false, CombatRules.EmpTopSpeed), 0f, "it still pulls up to what it has left");
            Assert.Less(handling.DriveAcceleration(jammed + 5f, false, CombatRules.EmpTopSpeed), 0f, "and no further");
            Assert.Greater(handling.DriveAcceleration(jammed + 5f, false), 0f, "unjammed it would pull on");
            Assert.AreEqual(CombatRules.EmpTopSpeed, CombatRules.TopSpeedShare(true));
            Assert.AreEqual(1f, CombatRules.TopSpeedShare(false));
        }

        [Test]
        public void TheWelderWaitsThenPatchesUpToItsLimit()
        {
            const float max = 150f;
            Assert.AreEqual(0f, CombatRules.WelderMend(max, 50f, CombatRules.WelderDelay - 0.1f, 1f), "not while it was just hit");
            Assert.AreEqual(max * CombatRules.WelderPerSecond * 0.5f, CombatRules.WelderMend(max, 50f, CombatRules.WelderDelay, 0.5f), 1e-4f);
            Assert.AreEqual(0f, CombatRules.WelderMend(max, max * CombatRules.WelderLimit, 100f, 1f), "never past its limit");
            Assert.AreEqual(1f, CombatRules.WelderMend(max, max * CombatRules.WelderLimit - 1f, 100f, 10f), 1e-4f, "it stops at the limit");
            Assert.AreEqual(0f, CombatRules.WelderMend(max, 0f, 100f, 1f), "a wreck is not mended");
        }

        [Test]
        public void TheMortarLandsFurtherTheFasterTheCarGoes()
        {
            Assert.AreEqual(CombatRules.MortarRange, CombatRules.MortarReach(0f), 1e-4f);
            Assert.AreEqual(CombatRules.MortarRange + 30f * CombatRules.MortarFlightSeconds, CombatRules.MortarReach(30f), 1e-4f);
            Assert.AreEqual(CombatRules.MortarRange, CombatRules.MortarReach(-10f), 1e-4f, "reversing does not shorten it");
            var from = new Vector3(0f, 1.5f, 0f);
            var to = new Vector3(0f, 0f, 40f);
            float apex = CombatRules.MortarApex(40f);
            Assert.AreEqual(from, CombatRules.MortarArc(from, to, apex, 0f));
            Assert.AreEqual(to, CombatRules.MortarArc(from, to, apex, 1f));
            Vector3 middle = CombatRules.MortarArc(from, to, apex, 0.5f);
            Assert.AreEqual(20f, middle.z, 1e-4f);
            Assert.AreEqual(0.75f + apex, middle.y, 1e-4f, "the top of the arc is the apex over the line");
            Assert.GreaterOrEqual(CombatRules.MortarApex(5f), CombatRules.MortarMinApex);
        }

        [Test]
        public void AFlameReachesOnlyCloseAndAhead()
        {
            Vector2 ahead = Vector2.up;
            Assert.IsTrue(CombatRules.FlameReaches(ahead, new Vector2(0f, 10f)));
            Assert.IsTrue(CombatRules.FlameReaches(ahead, new Vector2(2f, 12f)), "a little to the side");
            Assert.IsFalse(CombatRules.FlameReaches(ahead, new Vector2(0f, CombatRules.FlameRange + 1f)), "too far");
            Assert.IsFalse(CombatRules.FlameReaches(ahead, new Vector2(8f, 8f)), "outside the cone");
            Assert.IsFalse(CombatRules.FlameReaches(ahead, new Vector2(0f, -5f)), "behind");
        }

        [Test]
        public void OilCountsForThePourerWhenTheCarHitsAWall()
        {
            var model = new HealthModel(100f, 0.05f, 1f);
            model.Assist(2, 0, 10f);
            Assert.AreEqual(2, model.LastAttacker);
            Assert.AreEqual(2, model.KillerOf(HealthModel.Nobody, 0, 10f + CombatRules.AssistWindow - 0.5f), "a wall soon after: the pourer's kill");
            Assert.AreEqual(HealthModel.Nobody, model.KillerOf(HealthModel.Nobody, 0, 10f + CombatRules.AssistWindow + 0.5f), "long after: nobody's");
            model.Assist(0, 0, 11f);
            Assert.AreEqual(2, model.LastAttacker, "a car's own oil is nobody's assist");
        }
    }
}
