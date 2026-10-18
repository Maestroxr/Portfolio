using System.Collections;
using Gamebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Portfolio.Asteroids.Tests
{
    /// <summary>
    /// Play tests of the armory in the game scene: each new field weapon's volley does its trick to real rocks (the slug
    /// goes through a column of them, the lightning jumps from one to the next, the flak shell bursts in a cluster, the
    /// wave takes two side by side), the Phase Rounds and the Aegis Barrier, and the strike gear on a pilot who flies a
    /// strike mission with it (wingman drones out, armor taken off a hit, a repair kit used when the energy runs low).
    /// </summary>
    public class ArmoryPlayTest
    {
        private static Asteroid Rock(AsteroidsGameManager manager, Vector2 at)
        {
            Asteroid rock = manager.spawner.SpawnAsteroid(AsteroidKind.Rock, AsteroidSize.Small, at, Vector2.zero, false);
            Assert.NotNull(rock, "The scene has a rock pool.");
            rock.Velocity = Vector2.zero;
            return rock;
        }


        private static Shot Fire(AsteroidsGameManager manager, WeaponType type, int level, Vector2 from)
        {
            Shot shot = manager.spawner.FirePlayerShot(type, level, from, Vector2.up, Vector2.zero, manager.Ship);
            Assert.NotNull(shot, $"The scene has a {type} shot pool.");
            return shot;
        }


        [UnityTest]
        public IEnumerator TheNewFieldWeaponsDoTheirTricks()
        {
            yield return TestScenes.StartFirstMission();
            AsteroidsGameManager manager = TestScenes.Manager;
            SpaceField field = manager.Field;
            field.Clear();
            manager.Ship.Position = new Vector2(0f, -9f);

            // Rail Driver: a column of three rocks, one slug.
            Asteroid[] column = { Rock(manager, new Vector2(-12f, -2f)), Rock(manager, new Vector2(-12f, 1f)), Rock(manager, new Vector2(-12f, 4f)) };
            Shot slug = Fire(manager, WeaponType.Rail, 1, new Vector2(-12f, -6f));
            Assert.IsInstanceOf<RailShot>(slug);
            Assert.That(slug.Pierce, Is.EqualTo(WeaponRules.Pierce(WeaponType.Rail, 1)));

            // Arc Caster: the spark hits one rock, the lightning jumps to its neighbour.
            Asteroid struck = Rock(manager, new Vector2(-4f, 0f));
            Asteroid neighbour = Rock(manager, new Vector2(-1.5f, 1f));
            Shot spark = Fire(manager, WeaponType.Arc, 1, new Vector2(-4f, -4f));
            Assert.That(((ArcShot)spark).Chains, Is.EqualTo(WeaponRules.Chains(WeaponType.Arc, 1)), "The level's chains are set as it fires.");

            // Flak Burster: a cluster of three rocks, one shell at level 4.
            Asteroid[] cluster = { Rock(manager, new Vector2(5f, 2f)), Rock(manager, new Vector2(4f, 2.8f)), Rock(manager, new Vector2(6f, 2.8f)) };
            Shot shell = Fire(manager, WeaponType.Flak, 4, new Vector2(5f, -3f));
            Assert.That(((BurstShot)shell).Fragments, Is.EqualTo(WeaponRules.Fragments(WeaponType.Flak, 4)));

            // Ion Wave: two rocks side by side, one wave at level 4.
            Asteroid[] pair = { Rock(manager, new Vector2(12f, 3f)), Rock(manager, new Vector2(13.4f, 3.4f)) };
            Shot wave = Fire(manager, WeaponType.Wave, 4, new Vector2(12.7f, -4f));
            Assert.That(((WaveShot)wave).Growth, Is.EqualTo(WeaponRules.Growth(WeaponType.Wave, 4)));

            yield return new WaitForSeconds(1.2f);
            foreach (Asteroid rock in column)
            {
                Assert.IsFalse(rock.InPlay, "The slug went through the whole column.");
            }
            Assert.IsFalse(struck.InPlay, "The spark destroyed the rock it hit.");
            Assert.IsFalse(neighbour.InPlay, "The lightning jumped to the rock next to it.");
            int burst = 0;
            foreach (Asteroid rock in cluster)
            {
                burst += rock.InPlay ? 0 : 1;
            }
            Assert.That(burst, Is.GreaterThanOrEqualTo(2), "The shell burst in the cluster and its shrapnel broke it up.");
            Assert.IsFalse(pair[0].InPlay || pair[1].InPlay, "The wave took both rocks.");
            Assert.That(manager.Scoring.ShotsLanded, Is.LessThanOrEqualTo(manager.Scoring.ShotsFired + 4), "One landing per slug, spark, shell and wave: shrapnel does not count.");
        }


        [UnityTest]
        public IEnumerator PhaseRoundsAndTheAegisBarrier()
        {
            yield return TestScenes.StartFirstMission();
            AsteroidsGameManager manager = TestScenes.Manager;
            manager.Field.Clear();
            AsteroidsPlayer ship = manager.Ship;
            ship.ActivatePowerUp(PowerUpType.Piercing, 10f);
            Shot bolt = Fire(manager, WeaponType.Blaster, 1, new Vector2(0f, -6f));
            Assert.That(bolt.Pierce, Is.EqualTo(1 + PowerUps.ExtraPierce), "Phase Rounds: two more targets.");

            yield return TestScenes.WaitForInvulnerability(ship);
            ship.ActivatePowerUp(PowerUpType.Aegis, 10f);
            float hull = ship.Health;
            Assert.IsTrue(ship.TakeDamage(new DamageInfo(40f, Vector2.up, ship.Position, DamageSource.Enemy, false)), "The barrier takes the hit.");
            Assert.That(ship.Health, Is.EqualTo(hull), "The hull takes nothing.");
            Assert.That(ship.Shield, Is.EqualTo(ship.MaxShield), "The shield stays full.");
        }


        /// <summary>
        /// Another pilot's shots (a shared mission) show as ghosts: the new field weapons' with their level's tricks, the
        /// strike specials' with their prefab's pierce; a ghost shell bursts into ghost shrapnel here, and none of it hurts.
        /// </summary>
        [UnityTest]
        public IEnumerator GhostsOfTheNewShotsShowAndHurtNothing()
        {
            yield return TestScenes.StartFirstMission();
            AsteroidsGameManager manager = TestScenes.Manager;
            SpaceField field = manager.Field;
            field.Clear();
            manager.Ship.Position = new Vector2(-14f, -8f);
            Asteroid rock = Rock(manager, new Vector2(0f, 0f));
            float health = rock.Health;

            Shot shell = manager.spawner.FireGhostShot((byte)WeaponType.Flak, 4, new Vector2(0f, -4f), new Vector2(0f, 19f), 0.5f);
            Assert.IsInstanceOf<BurstShot>(shell);
            Assert.IsTrue(shell.IsGhost);
            Assert.That(((BurstShot)shell).Fragments, Is.EqualTo(WeaponRules.Fragments(WeaponType.Flak, 4)), "The ghost bursts like the real shell.");
            Shot spark = manager.spawner.FireGhostShot((byte)WeaponType.Arc, 3, new Vector2(3f, -4f), new Vector2(0f, 30f), 0.5f);
            Assert.That(((ArcShot)spark).Chains, Is.EqualTo(WeaponRules.Chains(WeaponType.Arc, 3)));
            Shot slug = manager.spawner.FireGhostShot((byte)(SpawnService.StrikeGhostBase + (int)StrikeShotKind.RailSlug), 1, new Vector2(6f, -4f),
                new Vector2(0f, 80f), 0.3f);
            Assert.IsInstanceOf<RailShot>(slug);
            Assert.That(slug.Pierce, Is.EqualTo(StrikeWeaponRules.Info(StrikeItem.RailGun).Pierce), "The ghost slug goes through, as the real one does.");
            foreach (StrikeShotKind kind in new[] { StrikeShotKind.FlakShell, StrikeShotKind.ClusterShell, StrikeShotKind.NapalmCanister, StrikeShotKind.SwarmRocket, StrikeShotKind.ArcBolt })
            {
                Shot ghost = manager.spawner.FireGhostShot((byte)(SpawnService.StrikeGhostBase + (int)kind), 1, new Vector2(12f, -8f), new Vector2(0f, 5f), 0.2f);
                Assert.NotNull(ghost, $"The scene has a pool for the ghosts of {kind}.");
                Assert.IsTrue(ghost.IsGhost);
            }

            int shards = 0;
            float until = Time.time + 0.7f;
            while (Time.time < until)
            {
                foreach (Shot shot in field.PlayerShots)
                {
                    if (shot.InPlay && shot.IsFragment)
                    {
                        Assert.IsTrue(shot.IsGhost, "A ghost shell's shrapnel is a ghost too.");
                        shards++;
                    }
                }
                yield return null;
            }
            Assert.That(shards, Is.GreaterThan(0), "The ghost shell burst.");
            Assert.IsTrue(rock.InPlay, "Nothing of a ghost hurts.");
            Assert.That(rock.Health, Is.EqualTo(health));
        }


        [UnityTest]
        public IEnumerator TheStrikeGearWorksOnAMission()
        {
            yield return TestScenes.Load(TestScenes.GameScene);
            AsteroidsGameManager manager = TestScenes.Manager;
            manager.Progress?.ResetAll(manager.LevelCount);
            int index = -1;
            for (int i = 0; i < manager.LevelCount && index < 0; i++)
            {
                if (manager.AsteroidsCampaign.Mission(i) is StrikeLevel)
                {
                    index = i;
                }
            }
            if (index < 0)
            {
                Assert.Ignore("The campaign has no strike mission.");
            }
            StrikeLoadout pilot = manager.Pilot;
            pilot.SetCount(StrikeItem.WingmanDrones, 1);
            pilot.SetCount(StrikeItem.ArmorPlating, 3);
            pilot.SetCount(StrikeItem.Afterburner, 1);
            pilot.SetCount(StrikeItem.SalvageMagnet, 1);
            pilot.SetCount(StrikeItem.RepairKit, 1);
            pilot.SetCount(StrikeItem.RailGun, 1);
            pilot.Special = StrikeItem.RailGun;
            pilot.PhaseShields = 0;
            pilot.ShieldPoints = 0f;
            pilot.Energy = StrikeRules.MaxEnergy;
            pilot.Difficulty = StrikeDifficulty.Veteran;
            ((IGameController)manager.Controller).PrepareGame(LevelData.Create(index));
            float waited = 0f;
            while (!manager.IsMissionActive && waited < 6f)
            {
                waited += Time.deltaTime;
                yield return null;
            }
            Assert.IsTrue(manager.IsMissionActive, "The strike mission started.");
            AsteroidsPlayer ship = manager.Ship;
            yield return null;
            Assert.IsTrue(ship.HasStrikeDrones);
            Assert.IsTrue(ship.HasSalvageMagnet);
            Assert.That(ship.StrikeSpeedFactor, Is.EqualTo(StrikeArmory.AfterburnerSpeed));
            int flying = 0;
            foreach (Drone drone in ship.drones)
            {
                flying += drone != null && drone.gameObject.activeSelf ? 1 : 0;
            }
            Assert.That(flying, Is.EqualTo(ship.drones.Length).And.GreaterThan(0), "The wingman drones fly.");
            Assert.That(manager.Ship.Strike.Special, Is.EqualTo(StrikeItem.RailGun));

            yield return TestScenes.WaitForInvulnerability(ship);
            StrikeLoadout working = manager.Working;
            float before = working.Energy;
            float hull = ship.PlayerSettings != null ? ship.PlayerSettings.HullMultiplier : 1f;
            ship.TakeDamage(new DamageInfo(10f, Vector2.down, ship.Position, DamageSource.Enemy, false));
            float expected = AsteroidsPlayer.StrikeDamage(10f, working.Difficulty, hull) * 0.7f;
            Assert.That(before - working.Energy, Is.EqualTo(expected).Within(0.01f), "Three plates take 30% off the hit.");

            working.Energy = 20f;
            working.NotifyChanged();
            yield return null;
            yield return null;
            Assert.That(working.Count(StrikeItem.RepairKit), Is.Zero, "The kit was used.");
            Assert.That(working.Energy, Is.GreaterThanOrEqualTo(20f + StrikeArmory.RepairKitEnergy - 1f), "...and patched the hull.");
            manager.ReturnToMissionSelect();
            yield return null;
            Assert.That(manager.Pilot.Count(StrikeItem.RepairKit), Is.EqualTo(1), "An unfinished mission leaves the saved pilot as it was.");
        }
    }
}
