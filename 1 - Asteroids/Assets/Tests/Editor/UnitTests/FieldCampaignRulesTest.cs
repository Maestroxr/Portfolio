using System.Collections.Generic;
using Gamebox;
using NUnit.Framework;

namespace Portfolio.Asteroids.Tests
{
    /// <summary>
    /// The rules of the field campaigns: iron, hydra and wreck rocks, the counts of the new wave entries, the wave director
    /// with the new enemies and objectives, the escort, defence and hunt objectives (also as a shared mission reports
    /// them), and what the new bodies are on the wire.
    /// </summary>
    public class FieldCampaignRulesTest
    {
        /// <summary>Counts what the director asks for; enemies count for the wave, like in the game.</summary>
        private class FakeSpawner : IWaveSpawner
        {
            public int Alive;
            public readonly List<AsteroidKind> Asteroids = new List<AsteroidKind>();
            public readonly List<HazardKind> Hazards = new List<HazardKind>();
            public int Drifters;

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
                if (kind == HazardKind.Sentry || kind == HazardKind.Minelayer || kind == HazardKind.Stalker || kind == HazardKind.Corsair)
                {
                    Alive++;
                }
                return null;
            }

            public Boss SpawnBoss(Boss prefab)
            {
                return null;
            }
        }

        private readonly TemporaryObjects objects = new TemporaryObjects();

        [TearDown]
        public void TearDown()
        {
            objects.Dispose();
        }

        private AsteroidsLevel Level(LevelObjective objective, int target, params WaveSpec[] waves)
        {
            AsteroidsLevel level = objects.Asset<AsteroidsLevel>();
            level.objective = objective;
            level.objectiveTarget = target;
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


        // ------------------------------------------------------------------ rocks

        [Test]
        public void IronShrugsOffShotsAndCracksInBlasts()
        {
            Assert.That(AsteroidRules.DamageTaken(AsteroidKind.Metal, DamageSource.PlayerShot, 2f), Is.EqualTo(1f));
            Assert.That(AsteroidRules.DamageTaken(AsteroidKind.Metal, DamageSource.Explosion, 2f), Is.EqualTo(3f));
            Assert.That(AsteroidRules.DamageTaken(AsteroidKind.Metal, DamageSource.Collision, 2f), Is.EqualTo(2f));
            Assert.That(AsteroidRules.DamageTaken(AsteroidKind.Rock, DamageSource.PlayerShot, 2f), Is.EqualTo(2f), "Other rocks take every shot in full.");
            Assert.Greater(AsteroidRules.Health(AsteroidKind.Metal, AsteroidSize.Large), AsteroidRules.Health(AsteroidKind.Ore, AsteroidSize.Large));
            Assert.Greater(AsteroidRules.Score(AsteroidKind.Metal, AsteroidSize.Small), AsteroidRules.Score(AsteroidKind.Rock, AsteroidSize.Small));
            Assert.That(AsteroidRules.ContactDamage(AsteroidKind.Metal, AsteroidSize.Large), Is.EqualTo(AsteroidRules.ContactDamage(AsteroidKind.Rock, AsteroidSize.Large)),
                "Iron is armour against shots: it rams like any rock.");
        }


        [Test]
        public void HydraSplitsInThreeAndOnlyItsSmallPiecesRegrow()
        {
            Assert.That(AsteroidRules.SplitCount(AsteroidKind.Hydra, AsteroidSize.Large, 0.9f), Is.EqualTo(3));
            Assert.That(AsteroidRules.SplitCount(AsteroidKind.Hydra, AsteroidSize.Medium, 0.9f), Is.EqualTo(2));
            Assert.That(AsteroidRules.SplitCount(AsteroidKind.Hydra, AsteroidSize.Medium, 0.1f), Is.EqualTo(3));
            Assert.That(AsteroidRules.SplitCount(AsteroidKind.Hydra, AsteroidSize.Small, 0f), Is.Zero);
            Assert.IsTrue(AsteroidRules.Regrows(AsteroidKind.Hydra, AsteroidSize.Small));
            Assert.IsFalse(AsteroidRules.Regrows(AsteroidKind.Hydra, AsteroidSize.Medium));
            Assert.IsFalse(AsteroidRules.Regrows(AsteroidKind.Rock, AsteroidSize.Small));
            Assert.Less(AsteroidRules.HydraWarningSeconds, AsteroidRules.HydraRegrowSeconds);
        }


        [Test]
        public void WreckageHoldsSalvageAndItsTankedPieceExplodes()
        {
            Assert.That(AsteroidRules.CrystalDrops(AsteroidKind.Wreck, AsteroidSize.Large), Is.EqualTo(2));
            Assert.That(AsteroidRules.CrystalDrops(AsteroidKind.Wreck, AsteroidSize.Small), Is.Zero);
            Assert.IsTrue(AsteroidRules.Explodes(AsteroidKind.Wreck, AsteroidRules.VolatileWreckShape));
            Assert.IsFalse(AsteroidRules.Explodes(AsteroidKind.Wreck, 0));
            Assert.IsTrue(AsteroidRules.Explodes(AsteroidKind.Magma, 1), "Magma explodes whatever its shape.");
            Assert.IsFalse(AsteroidRules.Explodes(AsteroidKind.Rock, AsteroidRules.VolatileWreckShape));
        }


        [Test]
        public void TheNewKindsHaveNamesAndFitTheWire()
        {
            foreach (AsteroidKind kind in new[] { AsteroidKind.Metal, AsteroidKind.Hydra, AsteroidKind.Wreck })
            {
                Assert.That(AsteroidRules.Title(kind), Is.Not.EqualTo("Rock"), $"{kind} has a name of its own.");
                BodyCodec.UnpackAsteroid(BodyCodec.PackAsteroid(kind, AsteroidSize.Medium, 2), out AsteroidKind back, out AsteroidSize size, out int shape);
                Assert.That(back, Is.EqualTo(kind));
                Assert.That(size, Is.EqualTo(AsteroidSize.Medium));
                Assert.That(shape, Is.EqualTo(2));
            }
            Assert.That((int)AsteroidKind.Wreck, Is.LessThan(16), "Asteroid kinds travel in four bits.");
            Assert.That((int)BodyKind.SolarFlare, Is.EqualTo(15));
            Assert.That((int)BodyKind.Ally, Is.EqualTo(21));
        }


        [Test]
        public void AnAllyTravelsWithItsKindAndHull()
        {
            int variant = SpawnService.AllyVariant(AllyKind.Station, 240f);
            Assert.That(variant & 0xF, Is.EqualTo((int)AllyKind.Station));
            Assert.That(variant >> 4, Is.EqualTo(240));
            Assert.That(SpawnService.AllyVariant(AllyKind.Freighter, 130.4f) >> 4, Is.EqualTo(130));
        }


        // ------------------------------------------------------------------ waves

        [Test]
        public void WavesCountTheNewRocksAndHazardsAsThemselves()
        {
            var wave = new WaveSpec { rocks = 1, metal = 2, hydra = 3, wrecks = 4, flares = 1, storms = 2, sentries = 3, minelayers = 1, stalkers = 2, corsairs = 3 };
            Assert.That(wave.AsteroidCount, Is.EqualTo(10));
            Assert.That(wave.Count(AsteroidKind.Metal), Is.EqualTo(2));
            Assert.That(wave.Count(AsteroidKind.Hydra), Is.EqualTo(3));
            Assert.That(wave.Count(AsteroidKind.Wreck), Is.EqualTo(4));
            Assert.That(wave.Count(AsteroidKind.Rock), Is.EqualTo(1));
            Assert.That(wave.HazardCount, Is.EqualTo(12));
            Assert.That(wave.Count(HazardKind.SolarFlare), Is.EqualTo(1));
            Assert.That(wave.Count(HazardKind.IonStorm), Is.EqualTo(2));
            Assert.That(wave.Count(HazardKind.Sentry), Is.EqualTo(3));
            Assert.That(wave.Count(HazardKind.Minelayer), Is.EqualTo(1));
            Assert.That(wave.Count(HazardKind.Stalker), Is.EqualTo(2));
            Assert.That(wave.Count(HazardKind.Corsair), Is.EqualTo(3));
            Assert.That(wave.Count(HazardKind.SupplyPod), Is.Zero, "The new hazards are no supply pods.");
            WaveSpec copy = wave.Copy();
            Assert.That(copy.corsairs, Is.EqualTo(3));
        }


        [Test]
        public void TheDirectorSpawnsTheNewKindsAndWaitsForTheNewEnemies()
        {
            AsteroidsLevel level = Level(LevelObjective.ClearWaves, 0, new WaveSpec { metal = 1, wrecks = 1, sentries = 1, flares = 1, hazardInterval = 2f });
            var spawner = new FakeSpawner();
            var director = new WaveDirector(level, Settings(false), spawner, new System.Random(3));
            director.Begin(1, true);
            CollectionAssert.AreEquivalent(new[] { AsteroidKind.Metal, AsteroidKind.Wreck }, spawner.Asteroids);
            spawner.Alive = 0;
            Run(director, 1.5f);
            Assert.That(director.State, Is.EqualTo(WaveDirector.Stage.Wave), "The sentry still to come keeps the wave going.");
            Run(director, 6f);
            Assert.That(spawner.Hazards, Does.Contain(HazardKind.Sentry));
            Assert.That(spawner.Hazards, Does.Contain(HazardKind.SolarFlare));
        }


        [Test]
        public void ADefenceRunsOnTimedWavesAndTheNewObjectivesTrickleRocks()
        {
            AsteroidsLevel level = Level(LevelObjective.Defend, 60, new WaveSpec { rocks = 2, duration = 10f });
            var spawner = new FakeSpawner();
            var director = new WaveDirector(level, Settings(true), spawner, new System.Random(5));
            director.Begin(1, true);
            Run(director, 5f);
            Assert.That(director.WaveNumber, Is.EqualTo(1), "The rocks are alive and the time is not up.");
            Assert.Greater(spawner.Drifters, 0, "Rocks drift in during a defence.");
            Run(director, 9f);
            Assert.That(director.WaveNumber, Is.GreaterThanOrEqualTo(2), "The wave ended with its time, rocks alive or not.");

            AsteroidsLevel escort = Level(LevelObjective.Escort, 2, new WaveSpec { rocks = 1 });
            var escortSpawner = new FakeSpawner();
            var escortDirector = new WaveDirector(escort, Settings(false), escortSpawner, new System.Random(7));
            escortDirector.Begin(1, true);
            escortSpawner.Alive = 0;
            Run(escortDirector, 6f);
            Assert.That(escortDirector.State, Is.Not.EqualTo(WaveDirector.Stage.Done), "An escort goes on past its planned waves until the freighters are through.");
            Assert.Greater(escortDirector.WaveNumber, 1);
        }


        // ------------------------------------------------------------------ objectives

        [Test]
        public void AnEscortCountsTheFreightersThatGetThrough()
        {
            var objective = new MissionObjective(LevelObjective.Escort, 2, 3, null);
            Assert.IsTrue(objective.HasAlly);
            objective.EscortUnderWay(0.5f);
            Assert.That(objective.Progress, Is.EqualTo(0.25f).Within(0.001f));
            Assert.That(objective.Escorted, Is.Zero);
            objective.FreighterDelivered();
            Assert.That(objective.Escorted, Is.EqualTo(1));
            Assert.IsFalse(objective.IsComplete);
            objective.EscortUnderWay(1f);
            Assert.That(objective.Escorted, Is.EqualTo(1), "A freighter under way counts only once it is through.");
            objective.FreighterDelivered();
            Assert.IsTrue(objective.IsComplete);
            objective.AllyHull = 0.5f;
            Assert.That(objective.Status(1), Does.Contain("2 / 2"));
            Assert.That(objective.Status(1), Does.Contain("50%"));
        }


        [Test]
        public void ADefenceCountsTheTimeTheStationStands()
        {
            var objective = new MissionObjective(LevelObjective.Defend, 90, 3, null);
            objective.Hold(30f);
            Assert.That(objective.Progress, Is.EqualTo(1f / 3f).Within(0.001f));
            Assert.IsFalse(objective.IsComplete);
            objective.Hold(60f);
            Assert.IsTrue(objective.IsComplete);
            objective.Survive(5f);
            Assert.That(objective.Survived, Is.LessThanOrEqualTo(95f));
        }


        [Test]
        public void AHuntCountsTheMarkedKillsAndOnlyThose()
        {
            var objective = new MissionObjective(LevelObjective.Hunt, 3, 3, null);
            Assert.IsFalse(objective.HasAlly);
            objective.CrystalCollected();
            objective.FreighterDelivered();
            objective.Hold(10f);
            Assert.That(objective.MarkedDestroyed, Is.Zero, "Crystals and the other objectives' measures do not count.");
            objective.MarkedDown();
            objective.MarkedDown();
            Assert.IsFalse(objective.IsComplete);
            objective.MarkedDown();
            Assert.IsTrue(objective.IsComplete);
            objective.MarkedDown();
            Assert.That(objective.MarkedDestroyed, Is.EqualTo(3));
        }


        [Test]
        public void ASharedMissionsReportReadsTheSame()
        {
            var escort = new MissionObjective(LevelObjective.Escort, 3, 3, null);
            Assert.That(escort.Status(1, 2f / 3f), Does.Contain("2 / 3"));
            var hunt = new MissionObjective(LevelObjective.Hunt, 8, 3, null);
            Assert.That(hunt.Status(1, 0.5f), Does.Contain("4 / 8"));
            var defend = new MissionObjective(LevelObjective.Defend, 120, 3, null);
            Assert.That(defend.Status(1, 0.5f), Does.Contain("1:00"));
            foreach (MissionObjective objective in new[] { escort, hunt, defend })
            {
                Assert.That(objective.Briefing, Is.Not.Empty);
            }
        }


        [Test]
        public void TheNewObjectivesNeedATargetAndAHuntItsQuarry()
        {
            AsteroidSettings settings = Settings(true);
            settings.Lives = 3;
            settings.HullStrength = 100f;
            settings.AsteroidSpeed = 1f;
            settings.AsteroidSpawnRate = 5f;
            settings.AsteroidExplosionRadius = 3f;
            AsteroidsLevel hunt = Level(LevelObjective.Hunt, 4, new WaveSpec { corsairs = 2 });
            AsteroidsAssets(hunt, settings);
            Assert.IsFalse(hunt.IsLevelValid(out string message), "A hunt without a quarry is not valid.");
            Assert.That(message, Does.Contain("quarry"));
            hunt.quarry = new[] { HazardKind.Corsair };
            Assert.IsTrue(hunt.IsLevelValid(out message), message);
            Assert.IsTrue(hunt.IsQuarry(HazardKind.Corsair));
            Assert.IsFalse(hunt.IsQuarry(HazardKind.Sentry));
            hunt.objectiveTarget = 0;
            Assert.IsFalse(hunt.IsLevelValid(out _));
        }


        /// <summary>Gives a test level its settings and a theme (what a level needs to be valid at all).</summary>
        private void AsteroidsAssets(AsteroidsLevel level, AsteroidSettings settings)
        {
            level.theme = objects.Asset<SectorTheme>();
            var field = typeof(AsteroidsLevel).GetProperty(nameof(AsteroidsLevel.Settings));
            field?.SetValue(level, settings);
        }
    }
}
