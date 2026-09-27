using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Portfolio.Asteroids.Tests
{
    /// <summary>
    /// The pilot of a strike mission: the direct flight (<see cref="PlayerSimulation.Fly"/>, <see cref="PlayerSimulation.StepFree"/>),
    /// the playfield clamp, the controls of the strike mode, the damage rules through the pilot's loadout, the weapons'
    /// timing, the megabomb and the autopilot's dodging.
    /// </summary>
    public class StrikePilotTest
    {
        private const float Step = 1f / 60f;

        private readonly List<Object> made = new List<Object>();


        [TearDown]
        public void TearDown()
        {
            foreach (Object thing in made)
            {
                if (thing != null)
                {
                    Object.DestroyImmediate(thing);
                }
            }
            made.Clear();
        }


        // ------------------------------------------------------------------ flight

        [Test]
        public void Fly_ReachesTopSpeedOnEachAxisInTheAccelerationTime()
        {
            var simulation = new PlayerSimulation(new MockTransformAdapter(), null);
            float max = StrikeRules.ShipMaxSpeed(11f);
            Assert.AreEqual(17f, max, 1e-4f);
            int steps = Mathf.CeilToInt(StrikeRules.ShipAccelerationTime / Step);
            for (int i = 0; i < steps - 2; i++)
            {
                simulation.Fly(new Vector2(1f, 0f), max, Step);
            }
            Assert.Less(simulation.Velocity.x, max, "not at top speed before the acceleration time");
            for (int i = 0; i < 3; i++)
            {
                simulation.Fly(new Vector2(1f, 0f), max, Step);
            }
            Assert.AreEqual(max, simulation.Velocity.x, 1e-3f);
            Assert.AreEqual(0f, simulation.Velocity.y, 1e-5f);
            Assert.IsTrue(simulation.IsThrusting);
        }


        [Test]
        public void Fly_DiagonalFliesBothAxesAtTopSpeed()
        {
            var simulation = new PlayerSimulation(new MockTransformAdapter(), null);
            for (int i = 0; i < 60; i++)
            {
                simulation.Fly(new Vector2(-1f, 1f), 17f, Step);
            }
            Assert.AreEqual(-17f, simulation.Velocity.x, 1e-3f);
            Assert.AreEqual(17f, simulation.Velocity.y, 1e-3f);
        }


        [Test]
        public void Fly_AnAxisWithoutInputHalvesEveryTick()
        {
            var simulation = new PlayerSimulation(new MockTransformAdapter(), null) { Velocity = new Vector3(10f, -8f, 0f) };
            simulation.Fly(new Vector2(0f, -1f), 17f, StrikeRules.Tick);
            Assert.AreEqual(5f, simulation.Velocity.x, 1e-3f, "x halves");
            Assert.Less(simulation.Velocity.y, -8f, "y keeps speeding up downward");
            simulation.Fly(Vector2.zero, 17f, StrikeRules.Tick * 2f);
            Assert.AreEqual(1.25f, simulation.Velocity.x, 1e-3f);
            Assert.IsFalse(simulation.IsThrusting);
        }


        [Test]
        public void StepFree_MovesAlongTheVelocityWithoutTurning()
        {
            var transform = new MockTransformAdapter();
            var simulation = new PlayerSimulation(transform, null) { Velocity = new Vector3(3f, -2f, 0f) };
            simulation.StepFree(0.5f);
            Assert.AreEqual(new Vector3(1.5f, -1f, 0f), transform.LocalPosition);
            Assert.AreEqual(Quaternion.identity, transform.LocalRotation);
            Assert.AreEqual(new Vector3(3f, -2f, 0f), simulation.Velocity, "no drag");
        }


        [Test]
        public void Clamp_KeepsTheShipInsideTheStrikePlayfield()
        {
            Playground playground = MakePlayground();
            Vector2 half = StrikeRules.HalfSize;
            Assert.AreEqual(new Vector2(half.x - 0.6f, playground.Top - 0.6f), playground.Clamp(new Vector2(99f, 99f), 0.6f));
            Assert.AreEqual(new Vector2(-half.x + 0.6f, playground.Bottom + 0.6f), playground.Clamp(new Vector2(-99f, -99f), 0.6f));
            Assert.AreEqual(new Vector2(1f, 2f), playground.Clamp(new Vector2(1f, 2f), 0.6f));
        }


        [Test]
        public void Simulate_TheShipCannotLeaveTheScreenAndStopsAtTheEdge()
        {
            SpaceField field = MakeField();
            AsteroidsPlayer ship = MakeShip(field, StrikeLoadout.NewPilot());
            var input = new FixedInput { Move = new Vector2(1f, 1f) };
            ship.Input = input;
            for (int i = 0; i < 300; i++)
            {
                ship.Simulate(Step);
            }
            Playground playground = field.Playground;
            Assert.AreEqual(playground.HalfSize.x - ship.Radius, ship.Position.x, 1e-3f);
            Assert.AreEqual(playground.Top - ship.Radius, ship.Position.y, 1e-3f, "the whole height is reachable");
            Assert.AreEqual(Vector2.zero, ship.Velocity, "no speed pressing into the edge");
            Assert.AreEqual(Quaternion.identity, ship.transform.rotation, "the nose stays up");
        }


        // ------------------------------------------------------------------ controls

        [Test]
        public void MoveOf_KeysWinOverTheGamepadAndTheStickWinsOverBoth()
        {
            Assert.AreEqual(new Vector2(-1f, 1f), PlayerShipInput.MoveOf(new Vector2(-1f, 1f), 0.9f, -0.9f, false, Vector2.zero));
            Assert.AreEqual(new Vector2(0.9f, -0.5f), PlayerShipInput.MoveOf(Vector2.zero, 0.9f, -0.5f, false, Vector2.zero));
            Assert.AreEqual(Vector2.zero, PlayerShipInput.MoveOf(Vector2.zero, 0.15f, -0.1f, false, Vector2.zero), "dead zone");
            Assert.AreEqual(new Vector2(0.3f, -0.7f), PlayerShipInput.MoveOf(new Vector2(1f, 1f), 1f, 1f, true, new Vector2(0.3f, -0.7f)));
            Assert.AreEqual(new Vector2(1f, -1f), PlayerShipInput.MoveOf(new Vector2(3f, -3f), 0f, 0f, false, Vector2.zero), "clamped");
        }


        [Test]
        public void CycleAndMegabombKeys_AreTheContractsKeys()
        {
            foreach (KeyCode key in new[] { KeyCode.LeftShift, KeyCode.RightShift, KeyCode.K, KeyCode.Q, KeyCode.Tab, KeyCode.JoystickButton2, KeyCode.JoystickButton4 })
            {
                Assert.IsTrue(PlayerShipInput.IsCycleKey(key), key.ToString());
            }
            foreach (KeyCode key in new[] { KeyCode.B, KeyCode.E, KeyCode.L, KeyCode.JoystickButton1, KeyCode.JoystickButton3 })
            {
                Assert.IsTrue(PlayerShipInput.IsBombKey(key), key.ToString());
                Assert.IsFalse(PlayerShipInput.IsCycleKey(key), key + " is a bomb key, not a cycle key");
            }
            Assert.IsFalse(PlayerShipInput.IsCycleKey(KeyCode.Space));
            Assert.IsFalse(PlayerShipInput.IsCycleKey(KeyCode.J));
        }


        [Test]
        public void TheAsteroidAutopilot_DoesNotFlyStrike()
        {
            var go = new GameObject("Autopilot");
            made.Add(go);
            IShipInput autopilot = go.AddComponent<AsteroidsAutopilot>();
            Assert.AreEqual(Vector2.zero, autopilot.Move);
            Assert.IsFalse(autopilot.CyclePressed);
        }


        // ------------------------------------------------------------------ the ship in strike

        [Test]
        public void ResetForStrike_MirrorsTheLoadout()
        {
            StrikeLoadout pilot = StrikeLoadout.NewPilot();
            pilot.Energy = 60f;
            pilot.PhaseShields = 2;
            pilot.ShieldPoints = 40f;
            pilot.Megabombs = 4;
            AsteroidsPlayer ship = MakeShip(MakeField(), pilot);
            Assert.AreEqual(MissionMode.Strike, ship.Mode);
            Assert.IsNotNull(ship.Strike);
            Assert.AreSame(pilot, ship.Strike.Loadout);
            Assert.AreEqual(100f, ship.MaxHealth);
            Assert.AreEqual(60f, ship.Health);
            Assert.AreEqual(100f, ship.MaxShield);
            Assert.AreEqual(40f, ship.Shield);
            Assert.AreEqual(4, ship.Bombs);
            Assert.AreEqual(5, ship.BombCapacity);
            Assert.AreEqual(StrikeRules.StartInvulnerability, ship.InvulnerableTime, 1e-5f);
            Assert.AreEqual(ship.Field.Playground.Bottom + AsteroidsPlayer.StartHeight, ship.Position.y, 1e-4f);
            Assert.AreEqual(0f, ship.Position.x, 1e-4f);

            pilot.Megabombs = 1;
            pilot.NotifyChanged();
            Assert.AreEqual(1, ship.Bombs, "the ship follows the loadout's changes");

            ship.ResetForMission(100f);
            Assert.AreEqual(MissionMode.Field, ship.Mode);
            Assert.IsNull(ship.Strike);
            Assert.AreEqual(AsteroidsPlayer.MaxBombs, ship.BombCapacity);
            pilot.Megabombs = 3;
            pilot.NotifyChanged();
            Assert.AreEqual(1, ship.Bombs, "a field ship no longer mirrors the strike loadout");
        }


        [Test]
        public void StrikeDamage_RookieHalvesAndTheHullDivides()
        {
            Assert.AreEqual(5f, AsteroidsPlayer.StrikeDamage(10f, StrikeDifficulty.Rookie, 1f), 1e-5f);
            Assert.AreEqual(10f, AsteroidsPlayer.StrikeDamage(10f, StrikeDifficulty.Veteran, 1f), 1e-5f);
            Assert.AreEqual(10f, AsteroidsPlayer.StrikeDamage(10f, StrikeDifficulty.Elite, 1f), 1e-5f);
            Assert.AreEqual(8f, AsteroidsPlayer.StrikeDamage(10f, StrikeDifficulty.Veteran, 1.25f), 1e-5f);
        }


        [Test]
        public void TakeDamage_NotDuringTheStartInvulnerability_ThenWithoutCooldown()
        {
            StrikeLoadout pilot = StrikeLoadout.NewPilot();
            pilot.Energy = 100f;
            AsteroidsPlayer ship = MakeShip(MakeField(), pilot);
            Assert.IsFalse(ship.TakeDamage(Hit(10f)), "invulnerable at the start");
            Assert.AreEqual(100f, pilot.Energy);
            EndInvulnerability(ship);
            Assert.IsTrue(ship.TakeDamage(Hit(10f)));
            Assert.IsTrue(ship.TakeDamage(Hit(10f)), "no hurt cooldown in strike");
            Assert.AreEqual(80f, pilot.Energy, 1e-4f);
            Assert.AreEqual(80f, ship.Health, 1e-4f);
        }


        [Test]
        public void TakeDamage_GoesToThePhaseShieldFirst_AndReportsTheDamageTaken()
        {
            StrikeLoadout pilot = StrikeLoadout.NewPilot();
            pilot.Energy = 100f;
            pilot.PhaseShields = 1;
            pilot.ShieldPoints = 100f;
            pilot.Difficulty = StrikeDifficulty.Rookie;
            AsteroidsPlayer ship = MakeShip(MakeField(), pilot);
            EndInvulnerability(ship);
            float reported = 0f;
            ship.Damaged += hit => reported += hit.Amount;
            ship.TakeDamage(Hit(30f));
            Assert.AreEqual(85f, pilot.ShieldPoints, 1e-4f, "Rookie halves");
            Assert.AreEqual(100f, pilot.Energy, 1e-4f);
            Assert.AreEqual(85f, ship.Shield, 1e-4f);
            Assert.AreEqual(15f, reported, 1e-4f);
        }


        [Test]
        public void TakeDamage_AtLowEnergyDestroysTheSelectedSpecial()
        {
            StrikeLoadout pilot = StrikeLoadout.NewPilot();
            pilot.Energy = 8f;
            pilot.SetCount(StrikeItem.MissilePods, 1);
            pilot.Special = StrikeItem.MissilePods;
            AsteroidsPlayer ship = MakeShip(MakeField(), pilot);
            EndInvulnerability(ship);
            var lost = new List<StrikeItem>();
            ship.Strike.WeaponLost += lost.Add;
            ship.TakeDamage(Hit(1f));
            CollectionAssert.AreEqual(new[] { StrikeItem.MissilePods }, lost);
            Assert.AreEqual(0, pilot.Count(StrikeItem.MissilePods));
            Assert.AreEqual(7f, pilot.Energy, 1e-4f);
            Assert.IsTrue(ship.IsAlive);
        }


        [Test]
        public void TakeDamage_EnergyZeroDestroysTheShip()
        {
            StrikeLoadout pilot = StrikeLoadout.NewPilot();
            pilot.Energy = 20f;
            AsteroidsPlayer ship = MakeShip(MakeField(), pilot);
            EndInvulnerability(ship);
            AsteroidsPlayer destroyed = null;
            ship.Destroyed += s => destroyed = s;
            ship.TakeDamage(Hit(25f));
            Assert.AreSame(ship, destroyed);
            Assert.IsFalse(ship.IsAlive);
            Assert.IsFalse(ship.TakeDamage(Hit(5f)), "a destroyed ship takes no more damage");
        }


        [Test]
        public void Energy_RegeneratesWhileNotFiring_NeverOnElite()
        {
            StrikeLoadout veteran = StrikeLoadout.NewPilot();
            veteran.Energy = 50f;
            AsteroidsPlayer ship = MakeShip(MakeField(), veteran);
            ship.Input = new FixedInput();
            Fly(ship, StrikeRules.EnergyRegenInterval + 0.1f);
            Assert.AreEqual(51f, veteran.Energy, 1e-4f);

            StrikeLoadout elite = StrikeLoadout.NewPilot();
            elite.Energy = 50f;
            elite.Difficulty = StrikeDifficulty.Elite;
            ship.ResetForStrike(elite);
            Fly(ship, StrikeRules.EnergyRegenInterval * 2f + 0.1f);
            Assert.AreEqual(50f, elite.Energy, 1e-4f);
        }


        // ------------------------------------------------------------------ weapons

        [Test]
        public void Intervals_FollowTheTableAndTheHullsFireRate()
        {
            Assert.AreEqual(0.086f, StrikeGunnery.Interval(StrikeItem.MachineGun, 1f), 1e-5f);
            Assert.AreEqual(0.043f, StrikeGunnery.Interval(StrikeItem.MachineGun, 2f), 1e-5f);
            Assert.AreEqual(0.26f, StrikeGunnery.Interval(StrikeItem.LaserTurret, 1f), 1e-5f);
            Assert.AreEqual(0f, StrikeGunnery.Interval(StrikeItem.MegaBomb, 1f));
        }


        [Test]
        public void Beams_AreOnForPartOfEveryCycle()
        {
            Assert.IsTrue(StrikeGunnery.IsBeamOn(StrikeItem.Deathray, 0f, 1f));
            Assert.IsTrue(StrikeGunnery.IsBeamOn(StrikeItem.Deathray, 0.16f, 1f));
            Assert.IsFalse(StrikeGunnery.IsBeamOn(StrikeItem.Deathray, 0.2f, 1f));
            Assert.IsTrue(StrikeGunnery.IsBeamOn(StrikeItem.TwinLaser, 0.31f, 1f));
            Assert.IsFalse(StrikeGunnery.IsBeamOn(StrikeItem.PulseCannon, 0f, 1f));
            Assert.AreEqual(1, StrikeGunnery.BeamKindOf(StrikeItem.Deathray));
            Assert.AreEqual(2, StrikeGunnery.BeamKindOf(StrikeItem.TwinLaser));
            Assert.AreEqual(0, StrikeGunnery.BeamKindOf(StrikeItem.PulseCannon));
            Assert.AreEqual(StrikeItem.TwinLaser, StrikeGunnery.BeamOfKind(2));
            Assert.AreEqual(StrikeItem.MachineGun, StrikeGunnery.BeamOfKind(0));
        }


        [Test]
        public void BeamOrigins_TheDeathrayFromTheNoseAndTheTwinLaserFromTheWings()
        {
            var gun = new Vector2(0f, 0.8f);
            Assert.AreEqual(new Vector2(2f, 0.8f), PlayerBeam.BeamOrigin(StrikeItem.Deathray, 0, new Vector2(2f, 0f), gun));
            Vector2 left = PlayerBeam.BeamOrigin(StrikeItem.TwinLaser, 0, Vector2.zero, gun);
            Vector2 right = PlayerBeam.BeamOrigin(StrikeItem.TwinLaser, 1, Vector2.zero, gun);
            Assert.AreEqual(-right.x, left.x, 1e-5f);
            Assert.Greater(right.x, 0.5f);
        }


        [Test]
        public void Cycle_SelectsTheNextOwnedSpecial()
        {
            StrikeLoadout pilot = StrikeLoadout.NewPilot();
            pilot.SetCount(StrikeItem.AirMissiles, 1);
            pilot.SetCount(StrikeItem.Bombs, 1);
            pilot.Special = StrikeItem.AirMissiles;
            var gunnery = new StrikeGunnery(pilot);
            int changes = 0;
            gunnery.SpecialChanged += () => changes++;
            gunnery.Cycle();
            Assert.AreEqual(StrikeItem.Bombs, gunnery.Special);
            gunnery.Cycle();
            Assert.AreEqual(StrikeItem.AirMissiles, gunnery.Special);
            Assert.AreEqual(2, changes);
        }


        [Test]
        public void Megabomb_ClearsEnemyShots_AndWaitsForItsCooldown()
        {
            SpaceField field = MakeField();
            StrikeLoadout pilot = StrikeLoadout.NewPilot();
            pilot.Megabombs = 2;
            AsteroidsPlayer ship = MakeShip(field, pilot);
            Shot shot = MakeEnemyShot(field, new Vector2(3f, 2f), Vector2.down);
            ship.Strike.Megabomb(ship);
            Assert.IsFalse(shot.InPlay, "enemy fire is wiped out");
            Assert.AreEqual(1, pilot.Megabombs);
            Assert.AreEqual(1, ship.Bombs);
            Assert.AreEqual(StrikeRules.MegabombCooldown, ship.Strike.MegabombCooldown, 1e-5f);
            ship.Strike.Megabomb(ship);
            Assert.AreEqual(1, pilot.Megabombs, "a press during the cooldown spends nothing");
            ship.Strike.Update(ship, StrikeRules.MegabombCooldown + 0.01f, false);
            ship.Strike.Megabomb(ship);
            Assert.AreEqual(0, pilot.Megabombs);
        }


        // ------------------------------------------------------------------ autopilot

        [Test]
        public void Autopilot_SidestepsAShotComingStraightDown()
        {
            SpaceField field = MakeField();
            AsteroidsPlayer ship = MakeShip(field, StrikeLoadout.NewPilot());
            StrikeAutopilot autopilot = ship.gameObject.AddComponent<StrikeAutopilot>();
            autopilot.Read(ship, Step);
            Assert.IsTrue(autopilot.Fire, "always fires");
            Vector2 start = ship.Position;
            MakeEnemyShot(field, start + new Vector2(0f, 5f), new Vector2(0f, -14f));
            autopilot.Read(ship, Step);
            Assert.Greater(Mathf.Abs(autopilot.Move.x), 0.5f, "moves out of the shot's way");
            Assert.Greater(Mathf.Abs(autopilot.Goal.x - start.x), 1.2f);
            Assert.Greater(autopilot.HereDanger, autopilot.GoalDanger);
        }


        [Test]
        public void Autopilot_KeepsToTheLowerPartOfTheField()
        {
            SpaceField field = MakeField();
            AsteroidsPlayer ship = MakeShip(field, StrikeLoadout.NewPilot());
            ship.Position = new Vector2(0f, 8f);
            StrikeAutopilot autopilot = ship.gameObject.AddComponent<StrikeAutopilot>();
            autopilot.Read(ship, Step);
            Assert.Less(autopilot.Move.y, -0.5f);
            Assert.Less(autopilot.Goal.y, field.Playground.Bottom + field.Playground.HalfSize.y * 2f * autopilot.lowerShare + 0.01f);
        }


        [Test]
        public void Autopilot_DodgesMostOfABarrage()
        {
            int passive = Barrage(false, 1);
            int flown = Barrage(true, 1);
            int flownAgain = Barrage(true, 2);
            TestContext.WriteLine($"hits in 40 s: standing still {passive}, autopilot {flown} / {flownAgain}");
            Assert.Greater(passive, 40, "the barrage is aimed at the ship");
            Assert.Less(flown, passive / 5, "the autopilot dodges most of it");
            Assert.Less(flownAgain, passive / 5);
        }


        /// <summary>
        /// 40 seconds of enemy fire from the top edge (aimed bolts every 0.3 s and a spread of three flak rounds every
        /// 1.2 s) at a ship flown by the autopilot or standing still; returns the hits taken.
        /// </summary>
        private int Barrage(bool autopilot, int seed)
        {
            SpaceField field = MakeField();
            StrikeLoadout pilot = StrikeLoadout.NewPilot();
            pilot.Energy = 100f;
            AsteroidsPlayer ship = MakeShip(field, pilot);
            if (autopilot)
            {
                ship.Input = ship.gameObject.AddComponent<StrikeAutopilot>();
            }
            else
            {
                ship.Input = new FixedInput();
            }
            int hits = 0;
            ship.Damaged += hit =>
            {
                hits++;
                pilot.Energy = 100f;
            };
            var random = new System.Random(seed);
            Playground playground = field.Playground;
            float bolt = 0f;
            float flak = 0f;
            for (float time = 0f; time < 40f; time += Step)
            {
                bolt -= Step;
                flak -= Step;
                if (bolt <= 0f)
                {
                    bolt += 0.3f;
                    var from = new Vector2(((float)random.NextDouble() * 2f - 1f) * playground.HalfSize.x * 0.9f, playground.Top - 0.5f);
                    MakeEnemyShot(field, from, (ship.Position - from).normalized * 12f).Lifetime = 5f;
                }
                if (flak <= 0f)
                {
                    flak += 1.2f;
                    var from = new Vector2(((float)random.NextDouble() * 2f - 1f) * playground.HalfSize.x * 0.6f, playground.Top - 0.5f);
                    Vector2 aim = (ship.Position - from).normalized;
                    for (int i = -1; i <= 1; i++)
                    {
                        MakeEnemyShot(field, from, (Vector2)(Quaternion.Euler(0f, 0f, i * 12f) * aim) * 9f).Lifetime = 5f;
                    }
                }
                ship.Simulate(Step);
                field.Tick(Step);
            }
            return hits;
        }


        // ------------------------------------------------------------------ helpers

        /// <summary>Commands that stay as set.</summary>
        private class FixedInput : IShipInput
        {
            public float Turn => 0f;
            public float Thrust => 0f;
            public bool Brake => false;
            public bool Fire { get; set; }
            public bool DashPressed => false;
            public bool BombPressed { get; set; }
            public Vector2 Move { get; set; }
            public bool CyclePressed { get; set; }

            public void Read(AsteroidsPlayer ship, float deltaTime)
            {
            }
        }


        private static DamageInfo Hit(float amount)
        {
            return new DamageInfo(amount, Vector2.down, Vector2.zero, DamageSource.Enemy, false);
        }


        private static void Fly(AsteroidsPlayer ship, float seconds)
        {
            for (float time = 0f; time < seconds; time += Step)
            {
                ship.Simulate(Step);
            }
        }


        private static void EndInvulnerability(AsteroidsPlayer ship)
        {
            ship.Simulate(StrikeRules.StartInvulnerability + 0.01f, false);
            Assert.AreEqual(0f, ship.InvulnerableTime);
        }


        private Playground MakePlayground()
        {
            var go = new GameObject("Playground");
            made.Add(go);
            Playground playground = go.AddComponent<Playground>();
            playground.Fix(StrikeRules.HalfSize);
            playground.Wraps = false;
            return playground;
        }


        private SpaceField MakeField()
        {
            Playground playground = MakePlayground();
            SpaceField field = playground.gameObject.AddComponent<SpaceField>();
            field.playground = playground;
            return field;
        }


        private AsteroidsPlayer MakeShip(SpaceField field, StrikeLoadout loadout)
        {
            var go = new GameObject("Ship");
            made.Add(go);
            AsteroidsPlayer ship = go.AddComponent<AsteroidsPlayer>();
            ship.Field = field;
            field.Player = ship;
            ship.ResetForStrike(loadout);
            return ship;
        }


        private Shot MakeEnemyShot(SpaceField field, Vector2 position, Vector2 velocity)
        {
            var go = new GameObject("EnemyShot");
            made.Add(go);
            Shot shot = go.AddComponent<Shot>();
            shot.enemy = true;
            shot.Damage = 3f;
            shot.Radius = 0.25f;
            shot.Position = position;
            shot.Velocity = velocity;
            field.Add(shot);
            return shot;
        }
    }
}
