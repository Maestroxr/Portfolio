using Gamebox;
using NUnit.Framework;
using UnityEngine;

namespace Portfolio.DeathRace.Tests
{
    /// <summary>
    /// The fighting on real cars (stand-ins of the factory, no track): the gun's rounds, a blast, the pickups, and who
    /// gets the kill. Runs in the editor only (physics queries and components): <c>drtests.sh</c>.
    /// </summary>
    public class CombatOnCarsTest
    {
        private TemporaryObjects temporary;
        private Race race;

        [SetUp]
        public void SetUp()
        {
            temporary = new TemporaryObjects();
            Transform root = temporary.Keep(new GameObject("Race")).transform;
            race = new Race(new RaceSetup { Mode = RaceMode.Quick, Seed = 3, CashPerPickup = 100 }, null, root) { Phase = RacePhase.Racing };
        }

        [TearDown]
        public void TearDown()
        {
            temporary.Dispose();
        }

        private Car Spawn(int seat, Vector3 position, float health = 1f, int armor = 0)
        {
            var loadout = new CarLoadout { model = CarModel.Rustbucket, armor = armor, health = health, mines = 2, rockets = 1, nitro = 1 };
            var entrant = new RaceEntrant { Seat = seat, Name = $"Car {seat}", Driver = DriverKind.Computer, Loadout = loadout };
            Car car = CarFactory.Spawn(entrant, new Pose(position, Quaternion.identity), null, null, race, null, race.Root);
            car.AttachRacer(new Racer(seat, null));
            race.Cars.Add(car);
            return car;
        }

        private static void Shoot(Car shooter, int steps)
        {
            Physics.SyncTransforms();
            for (int i = 0; i < steps; i++)
            {
                shooter.Weapons.Tick(new CarInput { Fire = true }, 0.02f);
            }
        }

        [Test]
        public void TheGunHitsTheCarAheadThroughItsArmour()
        {
            Car shooter = Spawn(0, Vector3.zero);
            Car target = Spawn(1, new Vector3(0f, 0f, 20f), 1f, 2);
            Shoot(shooter, 50);
            int hits = shooter.Weapons.RoundsHit;
            Assert.Greater(hits, 5, "most rounds hit a car 20 m ahead");
            float expected = hits * CombatRules.GunDamage * target.Stats.DamageTaken;
            Assert.AreEqual(expected, target.Health.DamageTaken, 0.01f);
            Assert.AreEqual(target.Health.Max - expected, target.Health.Health, 0.01f);
            Assert.AreEqual(shooter.Weapons.AmmoCapacity - shooter.Weapons.RoundsFired, shooter.Weapons.Ammo);
        }

        [Test]
        public void TheGunNeverHitsItsOwnCarOrSomethingOutOfRange()
        {
            Car shooter = Spawn(0, Vector3.zero);
            Car far = Spawn(1, new Vector3(0f, 0f, CombatRules.GunRange + 10f));
            Shoot(shooter, 30);
            Assert.AreEqual(0, shooter.Weapons.RoundsHit);
            Assert.AreEqual(shooter.Health.Max, shooter.Health.Health);
            Assert.AreEqual(far.Health.Max, far.Health.Health);
        }

        [Test]
        public void AWreckIsTheKillOfTheCarThatShotIt()
        {
            Car shooter = Spawn(0, Vector3.zero);
            Car target = Spawn(1, new Vector3(0f, 0f, 15f), 0.08f);
            DamageInfo cause = default;
            bool wrecked = false;
            target.Wrecked += (car, info) =>
            {
                wrecked = true;
                cause = info;
            };
            Shoot(shooter, 100);
            Assert.IsTrue(wrecked);
            Assert.IsTrue(target.IsWrecked);
            Assert.AreEqual(shooter, cause.Attacker);
            Assert.AreEqual(DamageSource.Gun, cause.Source);
            Assert.AreEqual(0f, target.Health.Health);
        }

        [Test]
        public void ABlastHurtsTheNearestMostAndNothingBeyondItsReach()
        {
            Car near = Spawn(0, new Vector3(0f, 0f, 2f));
            Car middle = Spawn(1, new Vector3(5.5f, 0f, 0f));
            Car far = Spawn(2, new Vector3(30f, 0f, 0f));
            Physics.SyncTransforms();
            int hurt = Blast.Explode(race, Vector3.zero, CombatRules.MineBlastRadius, CombatRules.MineDamage, 0f, DamageSource.Mine, null);
            Assert.AreEqual(2, hurt);
            Assert.Greater(near.Health.DamageTaken, middle.Health.DamageTaken);
            Assert.Greater(middle.Health.DamageTaken, 0f);
            Assert.AreEqual(0f, far.Health.DamageTaken);
            Assert.LessOrEqual(near.Health.DamageTaken, CombatRules.MineDamage + 0.001f);
        }

        [Test]
        public void OnesOwnBlastIsNobodysKill()
        {
            Car car = Spawn(0, Vector3.zero, 0.1f);
            DamageInfo cause = default;
            car.Wrecked += (wreck, info) => cause = info;
            Physics.SyncTransforms();
            Blast.Explode(race, new Vector3(0f, 0f, -3f), CombatRules.MineBlastRadius, CombatRules.MineDamage, 0f, DamageSource.Mine, car);
            Assert.IsTrue(car.IsWrecked);
            Assert.IsNull(cause.Attacker);
        }

        [Test]
        public void PickupsGiveWhatTheySay()
        {
            Car car = Spawn(0, Vector3.zero, 0.5f);
            car.Weapons.Tick(new CarInput { Fire = true }, 2f);
            int ammo = car.Weapons.Ammo;
            PickupField.Apply(car, PickupEffect.Of(PickupKind.Repair, 100, car.Health.Max, car.Weapons.AmmoCapacity));
            Assert.AreEqual(0.75f, car.Health.Health01, 0.001f);
            PickupField.Apply(car, PickupEffect.Of(PickupKind.Ammo, 100, car.Health.Max, car.Weapons.AmmoCapacity));
            Assert.AreEqual(Mathf.Min(car.Weapons.AmmoCapacity, ammo + car.Weapons.AmmoCapacity / 2), car.Weapons.Ammo);
            PickupField.Apply(car, PickupEffect.Of(PickupKind.Nitro, 100, car.Health.Max, car.Weapons.AmmoCapacity));
            Assert.AreEqual(2, car.Weapons.NitroCharges);
            PickupField.Apply(car, PickupEffect.Of(PickupKind.Cash, 100, car.Health.Max, car.Weapons.AmmoCapacity));
            Assert.AreEqual(100, car.Racer.CashPicked);
        }

        [Test]
        public void ConsumablesAreCountedForTheResult()
        {
            Car car = Spawn(0, Vector3.zero);
            car.Weapons.Tick(new CarInput { DropMine = true }, 0.02f);
            car.Weapons.Tick(new CarInput { DropMine = true }, 0.02f);
            car.Weapons.Tick(new CarInput { DropMine = true }, 0.02f);
            car.Weapons.Tick(new CarInput { FireRocket = true }, 0.02f);
            car.Weapons.Tick(new CarInput { Nitro = true }, 0.02f);
            Assert.AreEqual(2, car.Weapons.MinesUsed, "two carried, two dropped");
            Assert.AreEqual(0, car.Weapons.Mines);
            Assert.AreEqual(1, car.Weapons.RocketsUsed);
            Assert.AreEqual(1, car.Weapons.NitroUsed);
            Assert.IsTrue(car.Weapons.NitroActive);
            int lying = 0;
            foreach (Mine mine in Mine.Active)
            {
                lying += mine != null && mine.Owner == car ? 1 : 0;
            }
            Assert.AreEqual(2, lying);
            car.Weapons.Disarm();
            Assert.IsFalse(car.Weapons.NitroActive);
        }
    }
}
