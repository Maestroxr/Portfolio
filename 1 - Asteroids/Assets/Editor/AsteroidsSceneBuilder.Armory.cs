using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Asteroids.EditorTools
{
    /// <summary>
    /// The armory's parts of the scene: the field shot pools of the four new weapons (appended to the spawner's
    /// <see cref="SpawnService.playerShotPools"/>, which go in <see cref="WeaponType"/> order) and the flak shrapnel; the
    /// strike shot pools of kinds 11 to 19 (the strike pools go in <see cref="StrikeShotKind"/> order); the pickups of the
    /// new strike items (appended to the reward pools); the lightning and flak burst effects; and the new clips. Pool sizes
    /// allow four pilots with their ghost shots.
    /// </summary>
    internal static partial class AsteroidsSceneBuilder
    {
        static partial void BuildArmoryPools(Transform pools, SpawnService spawner, SpaceEffects effects)
        {
            int[] fieldSizes = { 40, 16, 30, 24 };
            var field = new List<ShotPool>(spawner.playerShotPools);
            for (int i = 0; i < AsteroidsPrefabBuilder.ArmoryFieldShots.Length; i++)
            {
                int index = (int)WeaponType.Arc + i;
                while (field.Count <= index)
                {
                    field.Add(null);
                }
                field[index] = OptionalPool<ShotPool>(pools, $"Shots/{AsteroidsPrefabBuilder.ArmoryFieldShots[i]}", fieldSizes[i]);
            }
            spawner.playerShotPools = field.ToArray();
            spawner.flakShrapnelPool = OptionalPool<ShotPool>(pools, "Shots/FlakShrapnel", 200);

            (StrikeShotKind kind, int size)[] strikeKinds =
            {
                (StrikeShotKind.FlakShell, 24), (StrikeShotKind.FlakShard, 120), (StrikeShotKind.ClusterShell, 8), (StrikeShotKind.Bomblet, 48),
                (StrikeShotKind.NapalmCanister, 8), (StrikeShotKind.NapalmFire, 16), (StrikeShotKind.SwarmRocket, 60), (StrikeShotKind.ArcBolt, 24),
                (StrikeShotKind.RailSlug, 12)
            };
            var strike = new List<ShotPool>(spawner.strikeShotPools);
            foreach ((StrikeShotKind kind, int size) in strikeKinds)
            {
                while (strike.Count <= (int)kind)
                {
                    strike.Add(null);
                }
                strike[(int)kind] = OptionalPool<ShotPool>(pools, $"Strike/Shots/{kind}", size);
            }
            spawner.strikeShotPools = strike.ToArray();

            var rewards = new List<RewardPool>(spawner.rewardPools);
            foreach (StrikeItem item in AsteroidsPrefabBuilder.ArmoryStrikePickups)
            {
                RewardPool pool = OptionalPool<RewardPool>(pools, $"Strike/Pickups/{item}", 6);
                if (pool != null)
                {
                    rewards.Add(pool);
                }
            }
            spawner.rewardPools = rewards.ToArray();

            effects.lightning = OptionalPool<LightningPool>(pools, "Effects/Lightning", 40);
            effects.flakBurst = OptionalPool<EffectPool>(pools, "Effects/FlakBurst", 24);
        }


        static partial void BuildArmoryAudio(AsteroidsAudio audio)
        {
            AudioClip S(string name) => AsteroidsArtBuilder.Sound(name);
            audio.arcClip = S("ArcCaster");
            audio.railClip = S("RailDriver");
            audio.flakClip = S("FlakBurster");
            audio.waveClip = S("IonWave");
            audio.lightningClip = S("Lightning");
            audio.flakBurstClip = S("FlakBurst");
            audio.clusterClip = S("ClusterBurst");
            audio.napalmClip = S("NapalmBurst");
            audio.repairClip = S("RepairKit");
        }
    }
}
