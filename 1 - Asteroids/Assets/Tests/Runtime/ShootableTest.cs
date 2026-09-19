using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Portfolio.Asteroids.Tests
{
    public class ShootableTest
    {
        private static int CountAsteroids(SpaceField field, AsteroidKind kind, AsteroidSize size)
        {
            return field.CountTargets(target => target is Asteroid asteroid && asteroid.Kind == kind && asteroid.Size == size);
        }


        [UnityTest]
        public IEnumerator LargeAsteroidSplitsWhenDestroyed()
        {
            yield return TestScenes.StartFirstMission();
            AsteroidsGameManager manager = TestScenes.Manager;
            SpaceField field = manager.Field;
            field.Clear();
            int before = CountAsteroids(field, AsteroidKind.Rock, AsteroidSize.Medium);
            Asteroid rock = manager.spawner.SpawnAsteroid(AsteroidKind.Rock, AsteroidSize.Large, new Vector2(8f, 6f), Vector2.zero, false);
            Assert.NotNull(rock);
            rock.Velocity = Vector2.zero;
            bool destroyed = false;
            rock.OnShotEvent += (shootable, shot) => destroyed = true;
            rock.TakeHit(new DamageInfo(100f, Vector2.right, rock.Position, DamageSource.PlayerShot, true));
            yield return null;
            Assert.IsTrue(destroyed, "The asteroid reported its destruction.");
            Assert.IsFalse(rock.InPlay, "The destroyed asteroid left play.");
            Assert.GreaterOrEqual(CountAsteroids(field, AsteroidKind.Rock, AsteroidSize.Medium) - before, 2, "It broke into at least two medium rocks.");
        }


        [UnityTest]
        public IEnumerator OreDropsCrystals()
        {
            yield return TestScenes.StartFirstMission();
            AsteroidsGameManager manager = TestScenes.Manager;
            SpaceField field = manager.Field;
            field.Clear();
            Asteroid ore = manager.spawner.SpawnAsteroid(AsteroidKind.Ore, AsteroidSize.Small, new Vector2(-8f, 6f), Vector2.zero, false);
            Assert.NotNull(ore);
            ore.TakeHit(new DamageInfo(100f, Vector2.right, ore.Position, DamageSource.PlayerShot, true));
            yield return null;
            int crystals = 0;
            foreach (Reward reward in field.Rewards)
            {
                if (reward is PointReward && reward.InPlay)
                {
                    crystals++;
                }
            }
            Assert.GreaterOrEqual(crystals, 1, "The ore rock dropped a crystal.");
        }


        [UnityTest]
        public IEnumerator ExplodableExplodesOthers()
        {
            yield return TestScenes.StartFirstMission();
            AsteroidsGameManager manager = TestScenes.Manager;
            SpaceField field = manager.Field;
            field.Clear();
            manager.Ship.Position = new Vector2(-12f, -7f);
            Mine mine = manager.spawner.SpawnMine(new Vector2(8f, 0f), Vector2.zero);
            Assert.NotNull(mine, "The scene has a mine pool.");
            Asteroid near = manager.spawner.SpawnAsteroid(AsteroidKind.Rock, AsteroidSize.Small, new Vector2(9f, 0.5f), Vector2.zero, false);
            Asteroid far = manager.spawner.SpawnAsteroid(AsteroidKind.Rock, AsteroidSize.Small, new Vector2(-8f, 6f), Vector2.zero, false);
            near.Velocity = Vector2.zero;
            far.Velocity = Vector2.zero;
            mine.TakeHit(new DamageInfo(100f, Vector2.right, mine.Position, DamageSource.PlayerShot, true));
            yield return null;
            Assert.IsFalse(mine.InPlay, "The mine blew up.");
            Assert.IsFalse(near.InPlay, "The rock next to the mine was caught in the blast.");
            Assert.IsTrue(far.InPlay, "The rock far away survived.");
        }


        [UnityTest]
        public IEnumerator ShotsDestroyAsteroidsAndScore()
        {
            yield return TestScenes.StartFirstMission();
            AsteroidsGameManager manager = TestScenes.Manager;
            SpaceField field = manager.Field;
            field.Clear();
            AsteroidsPlayer ship = manager.Ship;
            ship.Position = Vector2.zero;
            ship.transform.rotation = Quaternion.identity;
            Asteroid target = manager.spawner.SpawnAsteroid(AsteroidKind.Rock, AsteroidSize.Small, new Vector2(0f, 5f), Vector2.zero, false);
            target.Velocity = Vector2.zero;
            int score = manager.Scoring.Score;
            manager.spawner.FirePlayerShot(WeaponType.Blaster, 1, ship.Position + Vector2.up, Vector2.up, Vector2.zero, ship);
            yield return new WaitForSeconds(0.5f);
            Assert.IsFalse(target.InPlay, "The shot destroyed the small rock.");
            Assert.Greater(manager.Scoring.Score, score, "Destroying it scored.");
        }
    }
}
