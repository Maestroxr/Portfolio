using System.Collections.Generic;
using Gamebox;
using NUnit.Framework;
using UnityEngine;

namespace Portfolio.DeathRace.Tests
{
    /// <summary>
    /// The new guns, the special weapons and the fittings on real cars (stand-ins of the factory, no track), stepped by
    /// hand: the minigun, the flamethrower (its reach, its cone, a barrier between, the burning it leaves), the plasma
    /// cannon (a bolt that flies, hits and bursts), the oil slick (who it slicks, the pourer's grace), the EMP (who it
    /// jams, what a jam stops), the mortar (where it lands, blast shielding), the radar jammer against rockets, the field
    /// welder, and a car of another device that nothing here may put under a status. Runs in the editor only (physics
    /// queries and components): <c>drtests.sh</c>.
    /// </summary>
    public class WeaponsOnCarsTest
    {
        private const float Step = 0.02f;

        private TemporaryObjects temporary;
        private Race race;

        [SetUp]
        public void SetUp()
        {
            temporary = new TemporaryObjects();
            Transform root = temporary.Keep(new GameObject("Race")).transform;
            race = new Race(new RaceSetup { Mode = RaceMode.Quick, Seed = 5, CashPerPickup = 100 }, null, root) { Phase = RacePhase.Racing };
        }

        [TearDown]
        public void TearDown()
        {
            temporary.Dispose();
        }

        private Car Spawn(int seat, Vector3 position, CarLoadout loadout = null)
        {
            loadout ??= new CarLoadout { model = CarModel.Rustbucket };
            var entrant = new RaceEntrant { Seat = seat, Name = $"Car {seat}", Driver = DriverKind.Computer, Loadout = loadout };
            Car car = CarFactory.Spawn(entrant, new Pose(position, Quaternion.identity), null, null, race, null, race.Root);
            car.AttachRacer(new Racer(seat, null));
            race.Cars.Add(car);
            return car;
        }

        private static CarLoadout Armed(GunKind gun, int oil = 0, int emp = 0, int mortar = 0)
        {
            var loadout = new CarLoadout { model = CarModel.Rustbucket, oil = oil, emp = emp, mortar = mortar };
            loadout.GiveGun(gun);
            return loadout;
        }

        private static void Hold(Car car, CarInput input, int steps)
        {
            Physics.SyncTransforms();
            for (int i = 0; i < steps; i++)
            {
                car.Weapons.Tick(input, Step);
            }
        }

        private static void Press(Car car)
        {
            Physics.SyncTransforms();
            car.Weapons.Tick(new CarInput { Special = true }, Step);
        }

        /// <summary>A barrier of the track: a box on the barriers' layer.</summary>
        private GameObject Wall(Vector3 centre, Vector3 size)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Barrier";
            wall.layer = DeathRaceLayers.Wall;
            wall.transform.SetParent(race.Root, false);
            wall.transform.position = centre;
            wall.transform.localScale = size;
            return wall;
        }

        // ------------------------------------------------------------------------------------------------
        // Guns
        // ------------------------------------------------------------------------------------------------

        [Test]
        public void TheMinigunFiresFastAndHitsForItsDamage()
        {
            Car shooter = Spawn(0, Vector3.zero, Armed(GunKind.Minigun));
            Car target = Spawn(1, new Vector3(0f, 0f, 15f));
            Assert.AreEqual(GunKind.Minigun, shooter.Weapons.Gun);
            Hold(shooter, new CarInput { Fire = true }, 50);
            Assert.That(shooter.Weapons.RoundsFired, Is.InRange(16, 17), "sixteen a second");
            int hits = shooter.Weapons.RoundsHit;
            Assert.Greater(hits, 8, "most rounds hit a car 15 m ahead");
            Assert.AreEqual(hits * CombatRules.MinigunDamage, target.Health.DamageTaken, 0.01f);
            Assert.AreEqual(Mathf.RoundToInt(CarSpecs.Of(CarModel.Rustbucket).AmmoCapacity * CombatRules.MinigunAmmoFactor), shooter.Weapons.AmmoCapacity);
        }

        [Test]
        public void TheGunMountFiresFasterAndHitsHarder()
        {
            CarLoadout mounted = Armed(GunKind.MachineGun);
            mounted.gunMount = 1;
            mounted.model = CarModel.Hornet;
            Car shooter = Spawn(0, Vector3.zero, mounted);
            Car target = Spawn(1, new Vector3(0f, 0f, 15f));
            Assert.AreEqual(CombatRules.GunRoundsPerSecond * (1f + CarStats.GunMountPerLevelRate), shooter.Weapons.RoundsPerSecond, 1e-4f);
            Hold(shooter, new CarInput { Fire = true }, 30);
            Assert.AreEqual(shooter.Weapons.RoundsHit * CombatRules.GunDamage * (1f + CarStats.GunMountPerLevelDamage), target.Health.DamageTaken, 0.01f);
        }

        [Test]
        public void TheFlamethrowerBurnsWhatIsCloseAheadAndSetsItAlight()
        {
            Car shooter = Spawn(0, Vector3.zero, Armed(GunKind.Flamethrower));
            Car near = Spawn(1, new Vector3(1.5f, 0f, 10f));
            Car far = Spawn(2, new Vector3(0f, 0f, CombatRules.FlameRange + 8f));
            Car behind = Spawn(3, new Vector3(0f, 0f, -8f));
            Hold(shooter, new CarInput { Fire = true }, 10);
            int puffs = shooter.Weapons.RoundsFired;
            Assert.Greater(puffs, 1);
            Assert.AreEqual(puffs * CombatRules.FlameDamage, near.Health.DamageTaken, 0.01f, "every puff licks the car close ahead");
            Assert.IsTrue(near.Status.IsBurning);
            Assert.AreEqual(shooter, near.Status.Cause(CarStatusKind.Burning));
            Assert.AreEqual(0f, far.Health.DamageTaken, "out of reach");
            Assert.IsFalse(far.Status.IsBurning);
            Assert.AreEqual(0f, behind.Health.DamageTaken, "behind the nozzle");
            Assert.IsTrue(shooter.Weapons.Firing);
            Assert.AreEqual(0f, shooter.Health.DamageTaken, "never its own car");
        }

        [Test]
        public void ABarrierStopsTheFlame()
        {
            Car shooter = Spawn(0, Vector3.zero, Armed(GunKind.Flamethrower));
            Car hidden = Spawn(1, new Vector3(0f, 0f, 12f));
            Wall(new Vector3(0f, 1f, 6f), new Vector3(8f, 2f, 0.5f));
            Hold(shooter, new CarInput { Fire = true }, 10);
            Assert.AreEqual(0f, hidden.Health.DamageTaken);
            Assert.IsFalse(hidden.Status.IsBurning);
        }

        [Test]
        public void ABurningCarLosesHealthUntilTheFireGoesOut()
        {
            Car shooter = Spawn(0, Vector3.zero);
            Car victim = Spawn(1, new Vector3(0f, 0f, 30f));
            Assert.IsTrue(victim.Status.Burn(shooter));
            float seconds = 0f;
            while (victim.Status.IsBurning && seconds < 10f)
            {
                victim.Status.Step(Step);
                seconds += Step;
            }
            Assert.AreEqual(CombatRules.BurnSeconds, seconds, Step * 1.5f);
            Assert.AreEqual(CombatRules.BurnDamagePerSecond * CombatRules.BurnSeconds, victim.Health.DamageTaken, CombatRules.BurnDamagePerSecond * CombatRules.BurnTick + 0.01f);
            Assert.AreEqual(shooter, victim.Health.LastHit.Attacker, "the fire counts for whoever lit it");
            Assert.AreEqual(DamageSource.Flame, victim.Health.LastHit.Source);
            float taken = victim.Health.DamageTaken;
            victim.Status.Step(1f);
            Assert.AreEqual(taken, victim.Health.DamageTaken, "out: no more damage");
        }

        [Test]
        public void APlasmaBoltFliesHitsAndBursts()
        {
            Car shooter = Spawn(0, Vector3.zero, Armed(GunKind.Plasma));
            Car target = Spawn(1, new Vector3(0f, 0f, 30f));
            Car bystander = Spawn(2, new Vector3(2.1f, 0f, 30.5f));
            Hold(shooter, new CarInput { Fire = true }, 1);
            Assert.AreEqual(1, shooter.Weapons.RoundsFired);
            PlasmaBolt bolt = null;
            foreach (PlasmaBolt flying in PlasmaBolt.Active)
            {
                bolt = flying != null && flying.Owner == shooter ? flying : bolt;
            }
            Assert.IsNotNull(bolt, "the bolt is in the air");
            Assert.AreEqual(0f, target.Health.DamageTaken, "not at once: it flies");
            for (int i = 0; i < 60 && bolt != null; i++)
            {
                bolt.Step(Step);
            }
            Assert.IsTrue(bolt == null, "it hit something and is gone");
            Assert.AreEqual(1, shooter.Weapons.RoundsHit);
            Assert.AreEqual(CombatRules.PlasmaDamage, target.Health.DamageTaken, 0.01f, "the car hit takes the bolt whole (its burst spares it)");
            Assert.Greater(bystander.Health.DamageTaken, 0f, "the burst hurts the car beside it");
            Assert.Less(bystander.Health.DamageTaken, CombatRules.PlasmaDamage * CombatRules.PlasmaSplashShare + 0.01f);
        }

        // ------------------------------------------------------------------------------------------------
        // Special weapons
        // ------------------------------------------------------------------------------------------------

        [Test]
        public void TheSpecialButtonFiresTheArmedOneThenTheNext()
        {
            CarLoadout loadout = Armed(GunKind.MachineGun, 0, 1, 1);
            loadout.special = SpecialKind.Oil;
            Car car = Spawn(0, Vector3.zero, loadout);
            var made = new List<string>();
            car.Weapons.OilDropped += slick => made.Add("oil");
            car.Weapons.EmpFired += blast => made.Add(blast != null ? "emp" : "null");
            car.Weapons.MortarFired += shell => made.Add(shell != null ? "mortar" : "null");
            var empty = 0;
            car.Happened += happened => empty += happened.Kind == CarEventKind.Empty ? 1 : 0;
            Assert.AreEqual(SpecialKind.Emp, car.Weapons.NextSpecial, "no oil: the next one there is");
            Press(car);
            Assert.AreEqual(SpecialKind.Mortar, car.Weapons.NextSpecial);
            Press(car);
            Assert.IsNull(car.Weapons.NextSpecial);
            Press(car);
            CollectionAssert.AreEqual(new[] { "emp", "mortar" }, made, "each raised once, with what it made");
            Assert.AreEqual(1, empty, "nothing left: a dry click");
            Assert.AreEqual(1, car.Weapons.SpecialsUsed(SpecialKind.Emp));
            Assert.AreEqual(1, car.Weapons.SpecialsUsed(SpecialKind.Mortar));
            Assert.AreEqual(0, car.Weapons.SpecialsUsed(SpecialKind.Oil));
        }

        [Test]
        public void OilSlicksTheCarsThatDriveIntoItButNotItsPourerAtFirst()
        {
            Car pourer = Spawn(0, Vector3.zero, Armed(GunKind.MachineGun, 1));
            OilSlick slick = null;
            pourer.Weapons.OilDropped += poured => slick = poured;
            Press(pourer);
            Assert.IsNotNull(slick);
            Assert.AreEqual(pourer, slick.Owner);
            Assert.AreEqual(1, slick.Id);
            Assert.Less(slick.Position.z, -2f, "behind the car");
            Car follower = Spawn(1, new Vector3(slick.Position.x + 1f, 0f, slick.Position.z));
            Car clear = Spawn(2, new Vector3(slick.Position.x + 12f, 0f, slick.Position.z));
            // The pourer sits in its own slick: nothing for a moment.
            pourer.Body.Teleport(new Pose(new Vector3(slick.Position.x - 1.5f, 0f, slick.Position.z), Quaternion.identity));
            Physics.SyncTransforms();
            slick.Step(0.5f);
            Assert.IsTrue(follower.Status.IsSlicked);
            Assert.AreEqual(pourer, follower.Status.Cause(CarStatusKind.Slicked));
            Assert.IsFalse(pourer.Status.IsSlicked, "the pourer's grace");
            Assert.IsFalse(clear.Status.IsSlicked);
            follower.Status.Step(Step);
            Assert.AreEqual(CombatRules.OilGrip, follower.Body.GripFactor, 1e-5f);
            Assert.AreEqual(CombatRules.OilSteering, follower.Body.SteerFactor, 1e-5f);
            slick.Step(CombatRules.OilOwnerGrace);
            Assert.IsTrue(pourer.Status.IsSlicked, "its grace over, its own oil gets it too");
            for (float t = 0f; t < CombatRules.OilSlickSeconds + 0.2f; t += Step)
            {
                follower.Status.Step(Step);
            }
            Assert.IsFalse(follower.Status.IsSlicked, "off the oil, it wears off");
            Assert.AreEqual(1f, follower.Body.GripFactor);
        }

        [Test]
        public void ASlickDriesUpAndOnlySoManyLie()
        {
            Car pourer = Spawn(0, Vector3.zero);
            var slicks = new List<OilSlick>();
            for (int i = 0; i < CombatRules.MaxOilSlicks + 2; i++)
            {
                slicks.Add(OilSlick.Spawn(race, null, new Vector3(i * 20f, 0f, 100f), Quaternion.identity, pourer, i + 1));
                foreach (OilSlick each in slicks)
                {
                    if (each != null)
                    {
                        each.Step(0.1f);
                    }
                }
            }
            int lying = 0;
            foreach (OilSlick slick in OilSlick.Active)
            {
                lying += slick != null && slick.Race == race ? 1 : 0;
            }
            Assert.AreEqual(CombatRules.MaxOilSlicks, lying, "only so many lie at once");
            Assert.IsTrue(slicks[0] == null && slicks[1] == null, "the oldest dried up");
            Assert.IsTrue(slicks[2] != null);
            OilSlick last = slicks[slicks.Count - 1];
            last.Step(CombatRules.OilLifetime);
            Assert.IsTrue(last == null, "dried up");
        }

        [Test]
        public void AnEmpJamsTheCarsRoundItAndNothingFiresWhileJammed()
        {
            Car caster = Spawn(0, Vector3.zero, Armed(GunKind.MachineGun, 0, 1));
            CarLoadout loaded = Armed(GunKind.MachineGun, 1, 1, 1);
            loaded.nitro = 2;
            loaded.mines = 2;
            Car near = Spawn(1, new Vector3(0f, 0f, 8f), loaded);
            Car far = Spawn(2, new Vector3(0f, 0f, CombatRules.EmpRadius + 8f));
            Hold(near, new CarInput { Nitro = true }, 1);
            Assert.IsTrue(near.Weapons.NitroActive);
            EmpBlast blast = null;
            caster.Weapons.EmpFired += fired => blast = fired;
            Press(caster);
            Assert.IsNotNull(blast);
            CollectionAssert.AreEquivalent(new[] { near }, blast.Caught);
            Assert.IsTrue(near.Status.IsJammed);
            Assert.AreEqual(caster, near.Status.Cause(CarStatusKind.Jammed));
            Assert.AreEqual(CombatRules.EmpDamage, near.Health.DamageTaken, 0.01f);
            Assert.IsFalse(far.Status.IsJammed);
            Assert.IsFalse(caster.Status.IsJammed, "never its own car");
            Assert.AreEqual(0f, caster.Health.DamageTaken);

            near.Status.Step(Step);
            Assert.AreEqual(CombatRules.EmpTopSpeed, near.Body.TopSpeedFactor, 1e-5f);
            Hold(near, new CarInput { Fire = true, DropMine = true, Special = true, Nitro = true }, 5);
            Assert.IsFalse(near.Weapons.NitroActive, "the burning charge went out");
            Assert.AreEqual(0, near.Weapons.RoundsFired, "no gun");
            Assert.AreEqual(0, near.Weapons.MinesUsed, "no mines");
            Assert.AreEqual(0, near.Weapons.SpecialsUsed(SpecialKind.Oil) + near.Weapons.SpecialsUsed(SpecialKind.Emp), "no specials");
            Assert.AreEqual(1, near.Weapons.NitroUsed, "no new charge");
            for (float t = 0f; t < CombatRules.EmpJamSeconds + 0.1f; t += Step)
            {
                near.Status.Step(Step);
            }
            Assert.IsFalse(near.Status.IsJammed);
            Assert.AreEqual(1f, near.Body.TopSpeedFactor);
            Hold(near, new CarInput { Fire = true }, 2);
            Assert.Greater(near.Weapons.RoundsFired, 0, "back in the fight");
        }

        [Test]
        public void AMortarShellLandsAheadAndBlastShieldingSoaksItsBlast()
        {
            Car gunner = Spawn(0, Vector3.zero, Armed(GunKind.MachineGun, 0, 0, 2));
            CarLoadout shielded = new CarLoadout { model = CarModel.Rustbucket, shield = true };
            Vector3 aim = gunner.Weapons.MortarAim();
            Assert.AreEqual(CombatRules.MortarReach(0f), aim.z, 0.01f, "standing still: the range straight ahead (no track)");
            Car plain = Spawn(1, new Vector3(-3f, 0f, aim.z));
            Car armoured = Spawn(2, new Vector3(3f, 0f, aim.z), shielded);
            MortarShell shell = null;
            gunner.Weapons.MortarFired += fired => shell = fired;
            Press(gunner);
            Assert.IsNotNull(shell);
            Assert.AreEqual(aim.x, shell.Target.x, 0.01f);
            Assert.AreEqual(aim.z, shell.Target.z, 0.01f);
            Assert.AreEqual(0f, plain.Health.DamageTaken, "it flies first");
            float apex = 0f;
            for (int i = 0; i < 80 && shell != null; i++)
            {
                apex = Mathf.Max(apex, shell.Position.y);
                shell.Step(Step);
            }
            Assert.IsTrue(shell == null, "it landed");
            Assert.Greater(apex, CombatRules.MortarMinApex * 0.9f, "over the cars on an arc");
            Assert.Greater(plain.Health.DamageTaken, 0f);
            Assert.AreEqual(plain.Health.DamageTaken * CarStats.ShieldBlastTaken, armoured.Health.DamageTaken, 0.05f, "the same blast, a share soaked");
            Assert.AreEqual(gunner, plain.Health.LastHit.Attacker);
            Assert.AreEqual(DamageSource.Mortar, plain.Health.LastHit.Source);
        }

        // ------------------------------------------------------------------------------------------------
        // Fittings
        // ------------------------------------------------------------------------------------------------

        [Test]
        public void ARocketNeverLocksOnToARadarJammer()
        {
            CarLoadout jammer = new CarLoadout { model = CarModel.Rustbucket, jammer = true };
            Car shooter = Spawn(0, Vector3.zero);
            Car hidden = Spawn(1, new Vector3(0f, 0f, 20f), jammer);
            Car seen = Spawn(2, new Vector3(1f, 0f, 40f));
            IReadOnlyList<Car> cars = Targeting.CarsOf(race);
            Assert.AreEqual(hidden, Targeting.NearestInCone(cars, shooter, Vector3.zero, Vector3.forward, CombatRules.RocketCone, CombatRules.RocketSeekRange),
                "the nearest, to a plain look");
            Assert.AreEqual(seen, Targeting.NearestInCone(cars, shooter, Vector3.zero, Vector3.forward, CombatRules.RocketCone, CombatRules.RocketSeekRange, true),
                "a rocket's seeker does not see the jammer");
            Assert.IsFalse(Targeting.Lockable(hidden));
            Assert.IsTrue(Targeting.Lockable(seen));
        }

        [Test]
        public void TheWelderMendsAWhileAfterTheLastHitUpToItsLimit()
        {
            Car welded = Spawn(0, Vector3.zero, new CarLoadout { model = CarModel.Rustbucket, welder = true, health = 0.5f });
            Car plain = Spawn(1, new Vector3(20f, 0f, 0f), new CarLoadout { model = CarModel.Rustbucket, health = 0.5f });
            race.Time = 10f;
            welded.Health.TakeDamage(new DamageInfo(10f, DamageSource.Wall, null, Vector3.zero, Vector3.forward));
            float hurt = welded.Health.Health;
            race.Time = 10f + CombatRules.WelderDelay - 1f;
            welded.Health.Weld(1f);
            Assert.AreEqual(hurt, welded.Health.Health, 1e-4f, "it waits after a hit");
            Assert.IsFalse(welded.Health.Welding);
            race.Time = 10f + CombatRules.WelderDelay + 0.5f;
            welded.Health.Weld(1f);
            Assert.AreEqual(hurt + welded.Health.Max * CombatRules.WelderPerSecond, welded.Health.Health, 1e-3f);
            Assert.IsTrue(welded.Health.Welding);
            for (int i = 0; i < 200; i++)
            {
                welded.Health.Weld(1f);
            }
            Assert.AreEqual(welded.Health.Max * CombatRules.WelderLimit, welded.Health.Health, 1e-3f, "up to its limit, no further");
            Assert.IsFalse(welded.Health.Welding);
            plain.Health.Weld(5f);
            Assert.AreEqual(plain.Health.Max * 0.5f, plain.Health.Health, 1e-4f, "no welder, no mending");
            welded.Health.TakeDamage(new DamageInfo(5f, DamageSource.Gun, plain, Vector3.zero, Vector3.forward));
            float shot = welded.Health.Health;
            welded.Health.Weld(1f);
            Assert.AreEqual(shot, welded.Health.Health, 1e-4f, "a new hit: it waits again");
        }

        [Test]
        public void ACarOfAnotherDeviceIsUnderNoStatusHere()
        {
            Car caster = Spawn(0, Vector3.zero, Armed(GunKind.Flamethrower, 0, 1));
            Car remote = Spawn(1, new Vector3(0f, 0f, 8f));
            remote.Health.Authoritative = false;
            Hold(caster, new CarInput { Fire = true }, 5);
            Press(caster);
            OilSlick slick = OilSlick.Spawn(race, null, remote.transform.position, Quaternion.identity, caster, 7);
            slick.Authoritative = false;
            slick.Step(0.5f);
            Assert.IsFalse(remote.Status.IsBurning);
            Assert.IsFalse(remote.Status.IsJammed);
            Assert.IsFalse(remote.Status.IsSlicked);
            Assert.AreEqual(0f, remote.Health.DamageTaken, "its own device judges every hit");
            Assert.IsFalse(remote.Status.Burn(caster), "nor by hand");
        }
    }
}
