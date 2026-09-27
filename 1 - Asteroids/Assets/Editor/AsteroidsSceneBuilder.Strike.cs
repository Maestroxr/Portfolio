using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Portfolio.Asteroids.EditorTools
{
    /// <summary>
    /// The strike parts of the scene: the pools of the air and ground units, the strike shots, the enemy shot kinds 5 to 9,
    /// the strike pickups (appended to the reward pools) and the strike effects (sizes of design 4.3, enough for four pilots
    /// with their ghost shots); the strike clips and music; the terrain object and the strike UI. Ground unit prefabs come
    /// from the ground content (Prefabs/Strike/Ground/{unit}); a missing one leaves its pool empty with a warning.
    /// </summary>
    internal static partial class AsteroidsSceneBuilder
    {
        static partial void BuildStrikePools(Transform pools, SpawnService spawner, SpaceEffects effects)
        {
            var air = new EnemyPool[StrikeUnitRules.AirCount];
            for (int i = 0; i < air.Length; i++)
            {
                var unit = (StrikeUnit)i;
                air[i] = OptionalPool<EnemyPool>(pools, $"Strike/Air/{unit}", unit == StrikeUnit.Dart ? 20 : 12);
            }
            spawner.airPools = air;

            var ground = new EnemyPool[StrikeUnitRules.GroundCount];
            for (int i = 0; i < ground.Length; i++)
            {
                var unit = (StrikeUnit)(StrikeUnitRules.FirstGround + i);
                int size = unit == StrikeUnit.Crate ? 8 : unit == StrikeUnit.Depot ? 6 : 16;
                ground[i] = OptionalPool<EnemyPool>(pools, $"Strike/Ground/{unit}", size);
            }
            spawner.groundPools = ground;

            var shotSizes = new Dictionary<StrikeShotKind, int>
            {
                { StrikeShotKind.Bullet, 96 }, { StrikeShotKind.PlasmaBolt, 24 }, { StrikeShotKind.MicroMissile, 60 }, { StrikeShotKind.Dumbfire, 40 },
                { StrikeShotKind.MiniGunRound, 96 }, { StrikeShotKind.PodMissile, 48 }, { StrikeShotKind.AirMissile, 40 }, { StrikeShotKind.GroundMissile, 24 },
                { StrikeShotKind.Bomb, 16 }, { StrikeShotKind.DisrupterOrb, 4 }, { StrikeShotKind.Pulse, 60 }
            };
            var shots = new ShotPool[shotSizes.Count];
            foreach (KeyValuePair<StrikeShotKind, int> entry in shotSizes)
            {
                shots[(int)entry.Key] = OptionalPool<ShotPool>(pools, $"Strike/Shots/{entry.Key}", entry.Value);
            }
            spawner.strikeShotPools = shots;

            // The enemy shot pools grow from the five field kinds to all ten, in EnemyShotKind order.
            var enemy = new List<ShotPool>(spawner.enemyShotPools);
            (EnemyShotKind kind, int size)[] enemyKinds =
            {
                (EnemyShotKind.Flak, 90), (EnemyShotKind.Bolt, 90), (EnemyShotKind.Rocket, 40), (EnemyShotKind.Beam, 12), (EnemyShotKind.SkyMine, 24)
            };
            foreach ((EnemyShotKind kind, int size) in enemyKinds)
            {
                while (enemy.Count < (int)kind)
                {
                    enemy.Add(null);
                }
                enemy.Insert((int)kind, OptionalPool<ShotPool>(pools, $"Strike/Shots/{AsteroidsPrefabBuilder.EnemyShotName(kind)}", size));
            }
            spawner.enemyShotPools = enemy.ToArray();

            // Strike pickups go after the field's (a co-op claim needs kind Reward; the field's indices do not move).
            var rewards = new List<RewardPool>(spawner.rewardPools);
            foreach (string name in AsteroidsPrefabBuilder.StrikePickupNames)
            {
                RewardPool pool = OptionalPool<RewardPool>(pools, $"Strike/Pickups/{name}", name == "CreditOrb" ? 48 : 8);
                if (pool != null)
                {
                    rewards.Add(pool);
                }
            }
            spawner.rewardPools = rewards.ToArray();

            effects.groundExplosion = OptionalPool<EffectPool>(pools, "Strike/Effects/GroundExplosion", 24);
            effects.megabombFlash = OptionalPool<EffectPool>(pools, "Strike/Effects/MegabombFlash", 2);
            effects.muzzle = OptionalPool<EffectPool>(pools, "Strike/Effects/Muzzle", 24);
        }


        /// <summary>
        /// A pool of a strike prefab; null when the prefab does not exist (the scene still builds, but the build reports it
        /// as a problem: see <see cref="AsteroidsAssets.Problems"/>).
        /// </summary>
        private static T OptionalPool<T>(Transform parent, string prefab, int size) where T : MonoBehaviour
        {
            if (AsteroidsAssets.Load<GameObject>($"Prefabs/{prefab}.prefab") == null)
            {
                AsteroidsAssets.Problem($"the strike prefab {prefab} does not exist; its pool is left empty.");
                return null;
            }
            return Pool<T>(parent, prefab, size);
        }


        static partial void BuildStrikeAudio(AsteroidsAudio audio)
        {
            AudioClip S(string name) => AsteroidsArtBuilder.StrikeSound(name);
            audio.machineGunClip = S("MachineGun");
            audio.missileLaunchClip = S("MissileLaunch");
            audio.laserZapClip = S("LaserZap");
            audio.bombDropClip = S("BombDrop");
            audio.groundBoomClip = S("GroundBoom");
            audio.megabombClip = S("Megabomb");
            audio.cashClip = S("CashPickup");
            audio.itemPickupClip = S("ItemPickup");
            audio.shopBuyClip = S("ShopBuy");
            audio.shopSellClip = S("ShopSell");
            audio.shieldLowClip = S("ShieldLow");
            audio.weaponLostClip = S("WeaponLost");
            audio.flyByClip = S("FlyBy");
            audio.beamHumClip = S("BeamHum");
            audio.bossAlarmClip = S("BossAlarm");
            audio.strikeMusicClip = S("StrikeMusic");
            audio.strikeBossMusicClip = S("StrikeBossMusic");
        }


        static partial void BuildStrikeScene(Scene scene, AsteroidsGameManager manager, AsteroidsUI ui, Camera camera, Transform canvas)
        {
            // The scrolling ground of strike missions: hidden until a strike mission (or its preview) shows it.
            var terrainObject = new GameObject("StrikeTerrain");
            var terrain = terrainObject.AddComponent<StrikeTerrain>();
            terrain.playground = manager.Playground;
            terrain.sun = RenderSettings.sun;
            terrain.craterPrefab = AsteroidsAssets.Load<GameObject>("Prefabs/Strike/Effects/Crater.prefab");
            if (manager.field != null)
            {
                manager.field.terrain = terrain;
            }

            AsteroidsCampaign campaign = AsteroidsContentBuilder.Campaign;
            int missions = campaign != null ? campaign.MissionCountOf(MissionMode.Strike) : 0;
            int sectors = campaign != null ? campaign.SectorCountOf(MissionMode.Strike) : 0;
            if (canvas != null)
            {
                AsteroidsInterfaceBuilder.BuildStrike(ui, canvas, camera, missions, sectors);
            }
        }
    }
}
