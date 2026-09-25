using System.Collections.Generic;
using Gamebox;
using NUnit.Framework;
using UnityEngine;

namespace Portfolio.Asteroids.Tests
{
    public class WaveDirectorTest
    {
        /// <summary>Counts what the director asks for; the test decides how many targets are alive.</summary>
        private class FakeSpawner : IWaveSpawner
        {
            public int Alive;
            public readonly List<AsteroidKind> Asteroids = new List<AsteroidKind>();
            public readonly List<HazardKind> Hazards = new List<HazardKind>();
            public int Drifters;
            public int Bosses;

            public int WaveTargetsAlive => Alive;
            public int AsteroidsAlive => Alive;

            public Asteroid SpawnWaveAsteroid(AsteroidKind kind)
            {
                Asteroids.Add(kind);
                Alive++;
                return null;
            }

            public Asteroid SpawnDrifter(AsteroidKind kind)
            {
                Drifters++;
                return null;
            }

            public SpaceBody SpawnHazard(HazardKind kind)
            {
                Hazards.Add(kind);
                if (kind == HazardKind.Saucer || kind == HazardKind.Wasp)
                {
                    // Enemies count for the wave, like in the game.
                    Alive++;
                }
                return null;
            }

            public Boss SpawnBoss(Boss prefab)
            {
                Bosses++;
                return null;
            }
        }

        private readonly TemporaryObjects objects = new TemporaryObjects();

        [TearDown]
        public void TearDown()
        {
            objects.Dispose();
        }

        private AsteroidsLevel Level(LevelObjective objective, params WaveSpec[] waves)
        {
            AsteroidsLevel level = objects.Asset<AsteroidsLevel>();
            level.objective = objective;
            level.waves = waves;
            return level;
        }

        private AsteroidSettings Settings(bool trickle)
        {
            AsteroidSettings settings = objects.Asset<AsteroidSettings>();
            settings.SpawnAsteroid = trickle;
            settings.AsteroidSpawnRate = 2f;
            return settings;
        }

        private static void Run(WaveDirector director, float seconds)
        {
            for (float t = 0f; t < seconds; t += 0.1f)
            {
                director.Tick(0.1f);
            }
        }

        [Test]
        public void ClearingMissionAdvancesWhenTheWaveIsGone()
        {
            AsteroidsLevel level = Level(LevelObjective.ClearWaves,
                new WaveSpec { rocks = 2 },
                new WaveSpec { rocks = 1, ores = 2, mines = 1, hazardInterval = 2f });
            var spawner = new FakeSpawner();
            var director = new WaveDirector(level, Settings(true), spawner, new System.Random(1));
            int cleared = 0;
            bool finished = false;
            director.WaveCleared += wave => cleared++;
            director.AllWavesCleared += () => finished = true;

            director.Begin(1, true);
            Assert.That(director.WaveNumber, Is.EqualTo(1));
            Assert.That(spawner.Asteroids.Count, Is.EqualTo(2));
            Run(director, 5f);
            Assert.That(cleared, Is.Zero, "The wave waits for its asteroids.");
            Assert.That(spawner.Drifters, Is.Zero, "Clearing missions do not trickle in extra asteroids.");

            spawner.Alive = 0;
            Run(director, 0.5f);
            Assert.That(cleared, Is.EqualTo(1));
            Assert.That(director.State, Is.EqualTo(WaveDirector.Stage.Break));
            Run(director, WaveDirector.BreakTime + 0.2f);
            Assert.That(director.WaveNumber, Is.EqualTo(2));
            Assert.That(spawner.Asteroids.FindAll(kind => kind == AsteroidKind.Ore).Count, Is.EqualTo(2));
            Run(director, 4f);
            Assert.That(spawner.Hazards, Does.Contain(HazardKind.Mine), "The wave's mine arrives while it lasts.");

            spawner.Alive = 0;
            Run(director, 1.5f);
            Assert.IsTrue(finished);
            Assert.That(director.State, Is.EqualTo(WaveDirector.Stage.Done));
        }

        [Test]
        public void EnemiesStillDueArriveBeforeTheWaveEnds()
        {
            AsteroidsLevel level = Level(LevelObjective.ClearWaves, new WaveSpec { rocks = 1, saucers = 1, hazardInterval = 30f });
            var spawner = new FakeSpawner();
            var director = new WaveDirector(level, Settings(false), spawner, new System.Random(2));
            director.Begin(1, true);
            spawner.Alive = 0;
            Run(director, 1.5f);
            Assert.That(spawner.Hazards, Does.Contain(HazardKind.Saucer), "With the rocks gone the saucer comes right away.");
            Assert.That(director.State, Is.EqualTo(WaveDirector.Stage.Wave), "The wave is not over before its saucer arrived.");
        }

        [Test]
        public void BossMissionSummonsTheBossAfterTheLastWave()
        {
            AsteroidsLevel level = Level(LevelObjective.Boss, new WaveSpec { rocks = 1 });
            level.boss = objects.Component<Boss>("Boss");
            var spawner = new FakeSpawner();
            var director = new WaveDirector(level, Settings(false), spawner, new System.Random(3));
            director.Begin(1, true);
            spawner.Alive = 0;
            Run(director, 1.5f);
            Assert.That(spawner.Bosses, Is.EqualTo(1));
        }

        [Test]
        public void SurvivalWavesAdvanceWithTimeAndTrickle()
        {
            AsteroidsLevel level = Level(LevelObjective.Survive,
                new WaveSpec { rocks = 1, duration = 5f },
                new WaveSpec { rocks = 1, duration = 5f });
            var spawner = new FakeSpawner();
            var director = new WaveDirector(level, Settings(true), spawner, new System.Random(4));
            director.Begin(1, true);
            Run(director, 5.2f);
            Assert.That(director.State, Is.EqualTo(WaveDirector.Stage.Break), "A survival wave ends when its time is up.");
            Assert.Greater(spawner.Drifters, 0, "Asteroids drift in during survival missions.");
            Run(director, WaveDirector.BreakTime + 10f);
            Assert.GreaterOrEqual(director.WaveNumber, 3, "Survival missions repeat their last wave.");
        }

        [Test]
        public void EndlessWavesGrow()
        {
            var random = new System.Random(5);
            WaveSpec first = WaveSpec.Endless(1, random);
            WaveSpec later = WaveSpec.Endless(20, random);
            Assert.Greater(first.AsteroidCount, 0);
            Assert.Greater(later.AsteroidCount, first.AsteroidCount);
            Assert.Greater(later.HazardCount, first.HazardCount);
        }
    }
}
