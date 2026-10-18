using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Asteroids.EditorTools
{
    /// <summary>
    /// The scene's parts of the Iron Tide and Hive Fall campaigns: the pools of their enemy shot kinds (each at its kind's
    /// index, whatever came before it) and room for their swarms in the pools of the drone and the stinger. Their units'
    /// pools come with the others (<see cref="StrikeUnitRules.AirCount"/>, <see cref="StrikeUnitRules.GroundCount"/>).
    /// </summary>
    internal static partial class AsteroidsSceneBuilder
    {
        static partial void BuildStrikeCampaignPools(Transform pools, SpawnService spawner, SpaceEffects effects)
        {
            var enemy = new List<ShotPool>(spawner.enemyShotPools);
            (EnemyShotKind kind, int size)[] kinds =
            {
                (EnemyShotKind.Shell, 16), (EnemyShotKind.Spore, 48), (EnemyShotKind.Needle, 120), (EnemyShotKind.Seeker, 30)
            };
            foreach ((EnemyShotKind kind, int size) in kinds)
            {
                while (enemy.Count <= (int)kind)
                {
                    enemy.Add(null);
                }
                enemy[(int)kind] = OptionalPool<ShotPool>(pools, $"Strike/Shots/{AsteroidsPrefabBuilder.EnemyShotName(kind)}", size);
            }
            spawner.enemyShotPools = enemy.ToArray();

            // Drones come in swarms and spawners keep launching stingers: their pools hold more.
            Resize(spawner.airPools, (int)StrikeUnit.Drone, 32);
            Resize(spawner.airPools, (int)StrikeUnit.Stinger, 20);
            Resize(spawner.airPools, (int)StrikeUnit.Cruiser, 6);
            Resize(spawner.airPools, (int)StrikeUnit.Mite, 32);
        }


        private static void Resize<T>(T[] pools, int index, int size) where T : Object
        {
            if (pools != null && index >= 0 && index < pools.Length && pools[index] != null)
            {
                AsteroidsAssets.Set(pools[index], "MaxSize", p => p.intValue = size);
            }
        }
    }
}
