using System.Collections;
using Gamebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Portfolio.Asteroids.Tests
{
    /// <summary>
    /// The bodies of the field campaigns in the game scene: iron shrugs off shots, a tanked wreck blows up its
    /// neighbours, a small hydra rock grows back, an ally takes enemy fire but not the pilot's, an escort mission sends
    /// its freighter, and every new boss enters and fights for a while without trouble.
    /// </summary>
    public class FieldCampaignPlayTest
    {
        /// <summary>Loads the game and starts the mission at <paramref name="index"/>, then waits for the countdown to end.</summary>
        private static IEnumerator StartMission(int index)
        {
            yield return TestScenes.Load(TestScenes.GameScene);
            AsteroidsGameManager manager = TestScenes.Manager;
            Assert.NotNull(manager, "The game scene has no Asteroids manager.");
            ((IGameController)manager.Controller).PrepareGame(LevelData.Create(index));
            float waited = 0f;
            while (!manager.IsMissionActive && waited < 6f)
            {
                waited += Time.deltaTime;
                yield return null;
            }
            Assert.IsTrue(manager.IsMissionActive, "The mission did not start.");
        }


        /// <summary>The first mission of field campaign <paramref name="arc"/> with <paramref name="objective"/>, or -1.</summary>
        private static int CampaignMission(int arc, LevelObjective objective)
        {
            AsteroidsCampaign campaign = TestScenes.Manager != null ? TestScenes.Manager.AsteroidsCampaign : null;
            if (campaign == null || campaign.ArcCount(MissionMode.Field) <= arc)
            {
                return -1;
            }
            foreach (int index in campaign.MissionsOf(MissionMode.Field, arc))
            {
                if (campaign.Mission(index).Objective == objective)
                {
                    return index;
                }
            }
            return -1;
        }


        [UnityTest]
        public IEnumerator IronShrugsOffHalfOfEveryShot()
        {
            yield return TestScenes.StartFirstMission();
            AsteroidsGameManager manager = TestScenes.Manager;
            manager.Field.Clear();
            Asteroid iron = manager.spawner.SpawnAsteroid(AsteroidKind.Metal, AsteroidSize.Large, new Vector2(8f, 6f), Vector2.zero, false);
            Assert.NotNull(iron, "The scene has a pool of iron rocks.");
            float before = iron.Health;
            iron.TakeHit(new DamageInfo(2f, Vector2.right, iron.Position, DamageSource.PlayerShot, true));
            Assert.That(iron.Health, Is.EqualTo(before - 1f).Within(0.001f), "A shot does half its damage.");
            iron.TakeHit(new DamageInfo(2f, Vector2.right, iron.Position, DamageSource.Explosion, true));
            Assert.That(iron.Health, Is.EqualTo(before - 4f).Within(0.001f), "A blast does half again more.");
        }


        [UnityTest]
        public IEnumerator ATankedWreckBlowsUpItsNeighbours()
        {
            yield return TestScenes.StartFirstMission();
            AsteroidsGameManager manager = TestScenes.Manager;
            SpaceField field = manager.Field;
            field.Clear();
            manager.Ship.Position = new Vector2(-12f, -7f);
            Asteroid wreck = manager.spawner.SpawnAsteroid(AsteroidKind.Wreck, AsteroidSize.Small, new Vector2(8f, 0f), Vector2.zero, false);
            Assert.NotNull(wreck, "The scene has a pool of wreckage.");
            wreck.Configure(AsteroidSize.Small, 1f, AsteroidRules.VolatileWreckShape);
            Asteroid near = manager.spawner.SpawnAsteroid(AsteroidKind.Rock, AsteroidSize.Small, new Vector2(9f, 0.5f), Vector2.zero, false);
            near.Velocity = Vector2.zero;
            wreck.Velocity = Vector2.zero;
            wreck.TakeHit(new DamageInfo(100f, Vector2.right, wreck.Position, DamageSource.PlayerShot, true));
            yield return null;
            Assert.IsFalse(near.InPlay, "The rock next to the tanked wreck went up in its blast.");
        }


        [UnityTest]
        public IEnumerator ASmallHydraRockGrowsBackWhenLeftAlone()
        {
            yield return TestScenes.StartFirstMission();
            AsteroidsGameManager manager = TestScenes.Manager;
            SpaceField field = manager.Field;
            field.Clear();
            Asteroid small = manager.spawner.SpawnAsteroid(AsteroidKind.Hydra, AsteroidSize.Small, new Vector2(10f, 5f), Vector2.zero, true);
            Assert.NotNull(small, "The scene has a pool of hydra rocks.");
            small.Velocity = Vector2.zero;
            small.Tick(AsteroidRules.HydraRegrowSeconds + 0.1f);
            yield return null;
            Assert.IsFalse(small.InPlay, "The small rock is gone.");
            int medium = field.CountTargets(target => target is Asteroid rock && rock.Kind == AsteroidKind.Hydra && rock.Size == AsteroidSize.Medium);
            Assert.That(medium, Is.EqualTo(1), "A medium hydra rock grew in its place.");
        }


        [UnityTest]
        public IEnumerator AnAllyTakesEnemyFireButNotThePilots()
        {
            yield return TestScenes.StartFirstMission();
            AsteroidsGameManager manager = TestScenes.Manager;
            SpaceField field = manager.Field;
            field.Clear();
            manager.Ship.Position = new Vector2(-12f, -7f);
            Ally station = manager.spawner.SpawnAlly(AllyKind.Station, 200f);
            Assert.NotNull(station, "The scene has a pool of stations.");
            station.Anchor(new Vector2(6f, 0f));
            manager.spawner.FirePlayerShot(WeaponType.Blaster, 1, new Vector2(2f, 0f), Vector2.right, Vector2.zero, manager.Ship);
            yield return new WaitForSeconds(0.4f);
            Assert.That(station.Hull, Is.EqualTo(200f), "The pilot's shots pass a station by.");
            manager.spawner.FireEnemyShot(EnemyShotKind.Plasma, new Vector2(2f, 0f), Vector2.right * 9f);
            yield return new WaitForSeconds(0.6f);
            Assert.Less(station.Hull, 200f, "Enemy fire hurts it.");
            Assert.IsFalse(station.IsWrecked);
            station.Damage(1000f);
            Assert.IsTrue(station.IsWrecked);
            yield return new WaitForSeconds(2.2f);
            Assert.IsFalse(station.InPlay, "A wrecked station breaks up and goes.");
        }


        [UnityTest]
        public IEnumerator AnEscortSendsItsFreighterAndCountsIt()
        {
            yield return TestScenes.Load(TestScenes.GameScene);
            int escort = CampaignMission(1, LevelObjective.Escort);
            Assume.That(escort, Is.GreaterThanOrEqualTo(0), "The campaign has an escort mission.");
            yield return StartMission(escort);
            AsteroidsGameManager manager = TestScenes.Manager;
            Ally freighter = null;
            float waited = 0f;
            while (freighter == null && waited < 6f)
            {
                waited += Time.deltaTime;
                foreach (Ally ally in Ally.Active)
                {
                    freighter = ally != null && ally.InPlay ? ally : freighter;
                }
                yield return null;
            }
            Assert.NotNull(freighter, "A freighter set off.");
            Assert.That(freighter.Kind, Is.EqualTo(AllyKind.Freighter));
            Assert.That(manager.Objective.AllyHull, Is.GreaterThan(0f));
            yield return new WaitForSeconds(1f);
            Assert.Greater(freighter.Share, 0f, "It is on its way.");
            Assert.Greater(manager.Objective.Progress, 0f, "The objective follows it.");
        }


        [UnityTest]
        public IEnumerator EveryNewBossEntersAndFights()
        {
            yield return TestScenes.StartFirstMission();
            AsteroidsGameManager manager = TestScenes.Manager;
            AsteroidsCampaign campaign = manager.AsteroidsCampaign;
            Assume.That(campaign.ArcCount(MissionMode.Field), Is.GreaterThanOrEqualTo(3), "The field campaigns are built.");
            manager.Director?.Stop();
            Time.timeScale = 3f;
            try
            {
                for (int arc = 1; arc <= 2; arc++)
                {
                    foreach (int index in campaign.MissionsOf(MissionMode.Field, arc))
                    {
                        Boss prefab = campaign.Mission(index).BossPrefab;
                        if (prefab == null)
                        {
                            continue;
                        }
                        manager.Field.Clear();
                        Boss boss = manager.spawner.SpawnBoss(prefab);
                        Assert.NotNull(boss, $"{prefab.name} spawned.");
                        float waited = 0f;
                        while (waited < prefab.entrySeconds + 5f)
                        {
                            waited += Time.deltaTime;
                            manager.Ship.Repair(1000f);
                            yield return null;
                        }
                        Assert.IsTrue(boss.InPlay, $"{prefab.name} is still fighting.");
                        Assert.IsTrue(boss.HasEntered, $"{prefab.name} made its entrance.");
                        // A blinking boss cannot be hurt for a moment: the test hits it between blinks.
                        boss.Invulnerable = false;
                        boss.TakeHit(new DamageInfo(boss.MaxHealth * 0.5f, Vector2.up, boss.Position, DamageSource.PlayerShot, true));
                        Assert.GreaterOrEqual(boss.Phase, 1, $"{prefab.name} moved into its next phase.");
                        yield return new WaitForSeconds(1f);
                        boss.Despawn();
                    }
                }
            }
            finally
            {
                Time.timeScale = 1f;
            }
        }
    }
}
