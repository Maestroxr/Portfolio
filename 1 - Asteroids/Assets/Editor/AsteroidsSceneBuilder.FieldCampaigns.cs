using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Asteroids.EditorTools
{
    /// <summary>
    /// The field campaigns' parts of the scene (AsteroidsSceneBuilder.FieldCampaigns.cs): the pools of the iron, hydra and
    /// wreck rocks (at their kinds' places among the asteroid pools), of the lightning, lances, sweep beams and nova orbs
    /// (at their kinds' places among the enemy shot pools), of the flares, storms, the four enemies and the two allies;
    /// and the two campaigns' battle loops.
    /// </summary>
    internal static partial class AsteroidsSceneBuilder
    {
        static partial void BuildFieldCampaignPools(Transform pools, SpawnService spawner, SpaceEffects effects)
        {
            var rocks = new List<AsteroidPool>(spawner.asteroidPools);
            Place(rocks, (int)AsteroidKind.Metal, Pool<AsteroidPool>(pools, "Asteroids/Metal", 50));
            Place(rocks, (int)AsteroidKind.Hydra, Pool<AsteroidPool>(pools, "Asteroids/Hydra", 70));
            Place(rocks, (int)AsteroidKind.Wreck, Pool<AsteroidPool>(pools, "Asteroids/Wreck", 50));
            spawner.asteroidPools = rocks.ToArray();

            var shots = new List<ShotPool>(spawner.enemyShotPools);
            foreach ((EnemyShotKind kind, string name, int size) in AsteroidsPrefabBuilder.FieldEnemyShots)
            {
                Place(shots, (int)kind, Pool<ShotPool>(pools, $"Shots/{name}", size));
            }
            spawner.enemyShotPools = shots.ToArray();

            spawner.flarePool = Pool<HazardPool>(pools, "Hazards/SolarFlare", 3);
            spawner.stormPool = Pool<HazardPool>(pools, "Hazards/IonStorm", 3);
            spawner.sentryPool = Pool<EnemyPool>(pools, "Enemies/Sentry", 8);
            spawner.minelayerPool = Pool<EnemyPool>(pools, "Enemies/Minelayer", 4);
            spawner.stalkerPool = Pool<EnemyPool>(pools, "Enemies/Stalker", 10);
            spawner.corsairPool = Pool<EnemyPool>(pools, "Enemies/Corsair", 12);
            spawner.allyPools = new[] { Pool<HazardPool>(pools, "Allies/Freighter", 2), Pool<HazardPool>(pools, "Allies/Station", 1) };
        }


        /// <summary>Puts <paramref name="pool"/> at <paramref name="index"/> of a list of pools by kind, growing it with gaps if needed.</summary>
        private static void Place<T>(List<T> pools, int index, T pool) where T : class
        {
            while (pools.Count <= index)
            {
                pools.Add(null);
            }
            pools[index] = pool;
        }


        static partial void BuildFieldCampaignAudio(AsteroidsAudio audio)
        {
            audio.fieldMusic = new[] { null, AsteroidsArtBuilder.FieldSound("FrontierMusic"), AsteroidsArtBuilder.FieldSound("GateMusic") };
        }
    }
}
