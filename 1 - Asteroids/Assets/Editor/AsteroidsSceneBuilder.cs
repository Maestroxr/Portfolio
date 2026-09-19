using System.Collections.Generic;
using Gamebox;
using Gamebox.Editor;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Portfolio.Asteroids.EditorTools
{
    /// <summary>
    /// Builds Scenes/Asteroids.unity from the generated assets: the camera rig with post-processing, the star's light,
    /// the space backdrop, the game object with the manager, the playfield, spawner, effects, audio and every pool, the
    /// ship, the shared menu (as the pause menu) and the game's own interface.
    /// </summary>
    internal static class AsteroidsSceneBuilder
    {
        public const string ScenePath = "Scenes/Asteroids.unity";
        public const float HalfHeight = 10f;

        private static Material titleFont;
        private static Material hudFont;

        /// <summary>The title font material: outline, soft glow underlay.</summary>
        public static Material TitleFont
        {
            get
            {
                if (titleFont == null)
                {
                    PrepareFonts();
                }
                return titleFont;
            }
        }

        /// <summary>The HUD font material: a crisp dark outline.</summary>
        public static Material HudFont
        {
            get
            {
                if (hudFont == null)
                {
                    PrepareFonts();
                }
                return hudFont;
            }
        }

        public static void Build()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            PrepareFonts();
            AsteroidsCampaign campaign = AsteroidsContentBuilder.Campaign;
            AsteroidsLevel first = AsteroidsContentBuilder.FirstLevel;

            ConfigureLighting();
            Light sun = BuildSun();
            Camera camera = BuildCamera(out CameraRig rig, out Volume volume);
            SpaceBackdrop backdrop = BuildBackdrop(camera, sun);

            var game = new GameObject("Game");
            var manager = game.AddComponent<AsteroidsGameManager>();
            var storage = game.AddComponent<Storage>();
            var controller = game.AddComponent<AsteroidsController>();
            var playground = game.AddComponent<Playground>();
            var field = game.AddComponent<SpaceField>();
            var spawner = game.AddComponent<SpawnService>();
            var effects = game.AddComponent<SpaceEffects>();
            var sounds = game.AddComponent<AsteroidsAudio>();
            AsteroidsAssets.Set(storage, "DefaultStorageStrategy", p => p.enumValueIndex = (int)StorageStrategies.PlayerPrefs);

            playground.view = camera;
            playground.halfHeight = HalfHeight;
            rig.view = camera;
            rig.playground = playground;
            rig.volume = volume;
            rig.Place();
            field.playground = playground;
            field.spawner = spawner;
            field.effects = effects;
            field.sounds = sounds;
            field.cameraRig = rig;

            BuildPools(game.transform, spawner, effects);
            BuildAudio(sounds);

            var shipPrefab = AsteroidsAssets.Load<GameObject>("Prefabs/Ship.prefab");
            var shipObject = (GameObject)PrefabUtility.InstantiatePrefab(shipPrefab, scene);
            shipObject.name = "Ship";
            var ship = shipObject.GetComponent<AsteroidsPlayer>();

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            GameObject menu = GameMenuInstaller.InstallMenu(scene, 100, false);
            var ui = GameMenuInstaller.EnsureComponent<AsteroidsUI>(menu);
            GameMenuInstaller.WireGameUI(ui, menu, controller, manager, menu.GetComponent<CanvasScaler>());
            AsteroidSettings defaults = first != null ? first.Settings : null;
            var settingsUi = (AsteroidsSettingsUI)GameMenuInstaller.WireSettingsPanel(menu, typeof(AsteroidsSettingsUI), ui, defaults);
            settingsUi.lives = GameMenuInstaller.AddInputField(menu, "Lives", "Ships per mission", TMP_InputField.ContentType.IntegerNumber);
            settingsUi.hullStrength = GameMenuInstaller.AddInputField(menu, "HullStrength", "Hull strength", TMP_InputField.ContentType.DecimalNumber);
            settingsUi.asteroidSpeed = GameMenuInstaller.AddInputField(menu, "AsteroidSpeed", "Asteroid speed", TMP_InputField.ContentType.DecimalNumber);
            settingsUi.spawnRate = GameMenuInstaller.AddInputField(menu, "SpawnRate", "Rock spawn delay (s)", TMP_InputField.ContentType.DecimalNumber);
            settingsUi.explosionRadius = GameMenuInstaller.AddInputField(menu, "ExplosionRadius", "Explosion radius (m)", TMP_InputField.ContentType.DecimalNumber);
            int missions = campaign != null ? campaign.Count : 13;
            int sectors = campaign != null ? campaign.SectorCount : 4;
            AsteroidsInterfaceBuilder.Build(ui, menu, camera, missions, sectors, AsteroidsArtBuilder.Hulls.Length);

            AsteroidsAssets.SetObject(controller, "UI", ui);
            AsteroidsAssets.SetObject(controller, "BaseManager", manager);
            AsteroidsAssets.SetObject(manager, "asteroidSettings", defaults);
            AsteroidsAssets.SetObject(manager, "controller", controller);
            AsteroidsAssets.SetObject(manager, "ui", ui);
            AsteroidsAssets.SetObject(manager, "campaign", campaign);
            AsteroidsAssets.SetObject(manager, "StorageBehaviour", storage);
            AsteroidsAssets.Set(manager, "GameIdentifier", p => p.intValue = (int)GameType.Asteroids);
            AsteroidsAssets.SetObjects(manager, "PlayerList", ship);
            manager.Playground = playground;
            manager.field = field;
            manager.spawner = spawner;
            manager.backdrop = backdrop;
            manager.cameraRig = rig;
            manager.effects = effects;
            manager.sounds = sounds;
            manager.ship = ship;
            manager.hangar = AsteroidsContentBuilder.Hangar;
            if (first != null && first.Theme != null)
            {
                backdrop.Apply(first.Theme, true);
            }

            GameMenuInstaller.EnsureUrpCameras();
            string path = AsteroidsAssets.Path(ScenePath);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, path);
            GameSceneBuildSettings.Sync(true);
        }

        // ------------------------------------------------------------------ environment

        private static void ConfigureLighting()
        {
            string path = AsteroidsAssets.Path("Scenes/AsteroidsSettings.lighting");
            var lighting = AssetDatabase.LoadAssetAtPath<LightingSettings>(path);
            if (lighting == null)
            {
                lighting = new LightingSettings { name = "AsteroidsSettings" };
                AssetDatabase.CreateAsset(lighting, path);
            }
            lighting.bakedGI = false;
            lighting.realtimeGI = false;
            EditorUtility.SetDirty(lighting);
            Lightmapping.lightingSettings = lighting;
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.16f, 0.19f, 0.27f);
            // Without a skybox, metal would reflect black: a soft generated studio of space light gives hulls their sheen.
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = AsteroidsArtBuilder.ReflectionCube;
            RenderSettings.reflectionIntensity = 1f;
            RenderSettings.fog = false;
        }

        private static Light BuildSun()
        {
            var sunObject = new GameObject("Sun");
            var sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.None;
            sun.intensity = 1.5f;
            sun.lightmapBakeType = LightmapBakeType.Realtime;
            sunObject.transform.rotation = Quaternion.Euler(35f, -40f, 0f);
            sunObject.AddComponent<UniversalAdditionalLightData>();
            RenderSettings.sun = sun;
            return sun;
        }

        private static Camera BuildCamera(out CameraRig rig, out Volume volume)
        {
            var rigObject = new GameObject("CameraRig");
            rig = rigObject.AddComponent<CameraRig>();
            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            cameraObject.transform.SetParent(rigObject.transform, false);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.fieldOfView = 40f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 1200f;
            camera.allowHDR = true;
            camera.allowMSAA = false;
            cameraObject.AddComponent<AudioListener>();
            UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.High;
            data.renderShadows = false;

            var volumeObject = new GameObject("Global Volume");
            volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = AsteroidsContentBuilder.VolumeProfile;
            return camera;
        }

        private static SpaceBackdrop BuildBackdrop(Camera camera, Light sun)
        {
            var root = new GameObject("Backdrop");
            var backdrop = root.AddComponent<SpaceBackdrop>();
            Mesh quad = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            Mesh sphere = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");

            var space = new GameObject("Space");
            space.transform.SetParent(root.transform, false);
            space.transform.localPosition = new Vector3(0f, 0f, 420f);
            space.transform.localScale = new Vector3(1000f, 440f, 1f);
            space.AddComponent<MeshFilter>().sharedMesh = quad;
            var spaceRenderer = space.AddComponent<MeshRenderer>();
            spaceRenderer.sharedMaterial = AsteroidsArtBuilder.Material("Space");
            Quiet(spaceRenderer);

            var planet = new GameObject("Planet");
            planet.transform.SetParent(root.transform, false);
            planet.transform.localPosition = new Vector3(60f, -40f, 150f);
            planet.transform.localScale = Vector3.one * 90f;
            Mesh planetMesh = AsteroidsArtBuilder.PackMesh("BonusContent/Planet.FBX", "Planet");
            Mesh cloudMesh = AsteroidsArtBuilder.PackMesh("BonusContent/Clouds.FBX", "Clouds");
            float unit = planetMesh != null ? 1f / planetMesh.bounds.size.x : 1f / 24.88f;
            MeshRenderer surface = Part(planet.transform, "Surface", planetMesh, AsteroidsArtBuilder.Material("PlanetKepler"), Vector3.one * unit);
            MeshRenderer clouds = Part(planet.transform, "Clouds", cloudMesh, AsteroidsArtBuilder.PackMaterial("BonusContent/CloudsRed"), Vector3.one * unit * 1.01f);
            MeshRenderer atmosphere = Part(planet.transform, "Atmosphere", sphere, AsteroidsArtBuilder.Material("Atmosphere"), AsteroidsPrefabBuilder.SphereScale(1.02f));
            MeshRenderer halo = Part(planet.transform, "Halo", quad, AsteroidsArtBuilder.Material("AtmosphereHalo"), Vector3.one * 1.7f);
            MeshRenderer rings = Part(planet.transform, "Rings", AsteroidsArtBuilder.Model("PlanetRing"), AsteroidsArtBuilder.Material("PlanetRing"), Vector3.one * 0.5f);
            rings.gameObject.SetActive(false);

            ParticleSystem motes = ParticleFactory.Create("Motes", root.transform, AsteroidsArtBuilder.Material("ParticleAdd"));
            motes.transform.localPosition = new Vector3(0f, 0f, 18f);
            ParticleSystem.MainModule main = motes.main;
            main.prewarm = true;
            main.loop = true;
            ParticleFactory.Lifetime(motes, 9f, 14f);
            ParticleFactory.Speed(motes, 0f, 0.15f);
            ParticleFactory.Size(motes, 0.08f, 0.22f);
            ParticleFactory.Rate(motes, 26f);
            ParticleFactory.Box(motes, new Vector3(64f, 38f, 30f));
            ParticleFactory.FadeOut(motes, 0.2f);
            ParticleFactory.Velocity(motes, new Vector3(-0.15f, -0.6f, 0f), new Vector3(0.15f, -0.25f, 0f));
            ParticleFactory.MaxParticles(motes, 500);

            var rimObject = new GameObject("Rim");
            rimObject.transform.SetParent(root.transform, false);
            rimObject.transform.rotation = Quaternion.Euler(-35f, 150f, 0f);
            var rim = rimObject.AddComponent<Light>();
            rim.type = LightType.Directional;
            rim.shadows = LightShadows.None;
            rim.intensity = 0.75f;
            rim.color = new Color(0.45f, 0.65f, 1f);
            rimObject.AddComponent<UniversalAdditionalLightData>();

            backdrop.rim = rim;
            backdrop.view = camera;
            backdrop.background = spaceRenderer;
            backdrop.planet = planet.transform;
            backdrop.planetRenderer = surface;
            backdrop.cloudRenderer = clouds;
            backdrop.atmosphere = atmosphere;
            backdrop.halo = halo;
            backdrop.rings = rings;
            backdrop.sun = sun;
            backdrop.motes = motes;
            return backdrop;
        }

        private static MeshRenderer Part(Transform parent, string name, Mesh mesh, Material material, Vector3 scale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            Quiet(renderer);
            return renderer;
        }

        private static void Quiet(Renderer renderer)
        {
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        // ------------------------------------------------------------------ pools

        private static void BuildPools(Transform parent, SpawnService spawner, SpaceEffects effects)
        {
            var pools = new GameObject("Pools").transform;
            pools.SetParent(parent, false);

            spawner.field = parent.GetComponent<SpaceField>();
            spawner.asteroidPools = new[]
            {
                Pool<AsteroidPool>(pools, "Asteroids/Rock", 70),
                Pool<AsteroidPool>(pools, "Asteroids/Ore", 50),
                Pool<AsteroidPool>(pools, "Asteroids/Magma", 50),
                Pool<AsteroidPool>(pools, "Asteroids/Ice", 60),
                Pool<AsteroidPool>(pools, "Asteroids/Crystal", 40)
            };
            spawner.playerShotPools = new[]
            {
                Pool<ShotPool>(pools, "Shots/PlayerBolt", 100),
                Pool<ShotPool>(pools, "Shots/PlayerLaser", 50),
                Pool<ShotPool>(pools, "Shots/PlayerPellet", 140),
                Pool<ShotPool>(pools, "Shots/PlayerMissile", 40)
            };
            spawner.droneShotPool = Pool<ShotPool>(pools, "Shots/DroneBolt", 40);
            spawner.enemyShotPools = new[]
            {
                Pool<ShotPool>(pools, "Shots/EnemyPlasma", 90),
                Pool<ShotPool>(pools, "Shots/EnemyShrapnel", 90),
                Pool<ShotPool>(pools, "Shots/VoidShard", 50),
                Pool<ShotPool>(pools, "Shots/EnemyMissile", 24),
                Pool<ShotPool>(pools, "Shots/EnemyAcid", 60)
            };
            spawner.minePool = Pool<ExplodablePool>(pools, "Hazards/Mine", 24);
            spawner.bombPool = Pool<ExplodablePool>(pools, "Hazards/ClusterBomb", 8);
            spawner.podPool = Pool<LootablePool>(pools, "Hazards/SupplyPod", 6);
            spawner.saucerPool = Pool<EnemyPool>(pools, "Enemies/Saucer", 5);
            spawner.scoutPool = Pool<EnemyPool>(pools, "Enemies/Scout", 5);
            spawner.waspPool = Pool<EnemyPool>(pools, "Enemies/Wasp", 24);
            spawner.cometPool = Pool<HazardPool>(pools, "Hazards/Comet", 6);
            spawner.wellPool = Pool<HazardPool>(pools, "Hazards/GravityWell", 3);
            var rewardPools = new List<RewardPool>();
            foreach (Reward reward in AsteroidsPrefabBuilder.AllPickups())
            {
                string relative = AssetDatabase.GetAssetPath(reward.gameObject).Replace(AsteroidsAssets.Root + "/Prefabs/", "").Replace(".prefab", "");
                rewardPools.Add(Pool<RewardPool>(pools, relative, reward is PointReward ? 80 : 12));
            }
            spawner.rewardPools = rewardPools.ToArray();
            spawner.crystal = AsteroidsPrefabBuilder.Load<Reward>("Pickups/Crystal");
            var bosses = new GameObject("Bosses").transform;
            bosses.SetParent(parent, false);
            spawner.bossParent = bosses;

            effects.explosion = Pool<EffectPool>(pools, "Effects/Explosion", 32);
            effects.rockDebris = Pool<EffectPool>(pools, "Effects/RockDebris", 32);
            effects.iceShards = Pool<EffectPool>(pools, "Effects/IceShards", 20);
            effects.crystalShards = Pool<EffectPool>(pools, "Effects/CrystalShards", 16);
            effects.spark = Pool<EffectPool>(pools, "Effects/Spark", 50);
            effects.pickup = Pool<EffectPool>(pools, "Effects/Pickup", 12);
            effects.warpIn = Pool<EffectPool>(pools, "Effects/WarpIn", 16);
            effects.shockwave = Pool<EffectPool>(pools, "Effects/Shockwave", 20);
            effects.nova = Pool<EffectPool>(pools, "Effects/Nova", 2);
            effects.shipExplosion = Pool<EffectPool>(pools, "Effects/ShipExplosion", 2);
            effects.dashTrail = Pool<EffectPool>(pools, "Effects/DashTrail", 4);
            effects.telegraph = Pool<EffectPool>(pools, "Effects/Telegraph", 3);
            effects.popups = Pool<PopupPool>(pools, "Effects/ScorePopup", 36);
        }

        private static T Pool<T>(Transform parent, string prefab, int size) where T : MonoBehaviour
        {
            var asset = AsteroidsAssets.Load<GameObject>($"Prefabs/{prefab}.prefab");
            string name = prefab.Substring(prefab.LastIndexOf('/') + 1);
            var go = new GameObject($"{name} Pool");
            go.transform.SetParent(parent, false);
            go.SetActive(false);
            var pool = go.AddComponent<T>();
            AsteroidsAssets.SetObject(pool, "Instance", asset);
            AsteroidsAssets.Set(pool, "MaxSize", p => p.intValue = size);
            go.SetActive(true);
            if (asset == null)
            {
                Debug.LogError($"Asteroids: the prefab {prefab} is missing; build the art first.");
            }
            return pool;
        }

        private static void BuildAudio(AsteroidsAudio sounds)
        {
            AudioClip S(string name) => AsteroidsArtBuilder.Sound(name);
            sounds.blaster = S("Blaster");
            sounds.laser = S("Laser");
            sounds.scatter = S("Scatter");
            sounds.missile = S("Missile");
            sounds.droneShot = S("DroneShot");
            sounds.enemyShot = S("EnemyShot");
            sounds.nova = S("Nova");
            sounds.dash = S("Dash");
            sounds.denied = S("Denied");
            sounds.rockBreak = S("RockBreak");
            sounds.rockBreakSmall = S("RockBreakSmall");
            sounds.iceBreak = S("IceBreak");
            sounds.crystalBreak = S("CrystalBreak");
            sounds.explosion = S("Explosion");
            sounds.bigExplosion = S("BigExplosion");
            sounds.hullHit = S("HullHit");
            sounds.shieldHit = S("ShieldHit");
            sounds.shieldDown = S("ShieldDown");
            sounds.shipExplode = S("ShipExplode");
            sounds.enemyExplode = S("EnemyExplode");
            sounds.pickup = S("Pickup");
            sounds.crystal = S("Crystal");
            sounds.powerUp = S("PowerUp");
            sounds.weaponUp = S("WeaponUp");
            sounds.extraLife = S("ExtraLife");
            sounds.podOpen = S("PodOpen");
            sounds.mineBeep = S("MineBeep");
            sounds.bombTick = S("BombTick");
            sounds.warning = S("Warning");
            sounds.cometPass = S("CometPass");
            sounds.warpIn = S("WarpIn");
            sounds.wellOpen = S("WellOpen");
            sounds.bossRoar = S("BossRoar");
            sounds.respawn = S("Respawn");
            sounds.countdown = S("Countdown");
            sounds.go = S("Go");
            sounds.waveStart = S("WaveStart");
            sounds.waveClear = S("WaveClear");
            sounds.victory = S("Victory");
            sounds.gameOver = S("GameOver");
            sounds.star = S("Star");
            sounds.click = S("Click");
            sounds.combo = S("Combo");
            sounds.menuMusic = S("MenuMusic");
            sounds.battleMusic = S("BattleMusic");
            sounds.bossMusic = S("BossMusic");
            sounds.musicVolume = 0.42f;
            sounds.effectsVolume = 0.85f;
        }

        // ------------------------------------------------------------------ fonts

        private static void PrepareFonts()
        {
            TMP_FontAsset font = TMP_Settings.defaultFontAsset;
            if (font == null)
            {
                return;
            }
            titleFont = AsteroidsAssets.SaveMaterial("Art/Materials/FontTitle.mat", font.material.shader, m =>
            {
                m.CopyPropertiesFromMaterial(font.material);
                m.EnableKeyword("OUTLINE_ON");
                m.EnableKeyword("UNDERLAY_ON");
                m.SetFloat("_FaceDilate", 0.12f);
                m.SetFloat("_OutlineWidth", 0.22f);
                m.SetColor("_OutlineColor", new Color(0.02f, 0.08f, 0.18f, 1f));
                m.SetColor("_UnderlayColor", new Color(0.1f, 0.55f, 1f, 0.55f));
                m.SetFloat("_UnderlayOffsetX", 0f);
                m.SetFloat("_UnderlayOffsetY", 0f);
                m.SetFloat("_UnderlayDilate", 0.6f);
                m.SetFloat("_UnderlaySoftness", 0.6f);
            });
            hudFont = AsteroidsAssets.SaveMaterial("Art/Materials/FontHud.mat", font.material.shader, m =>
            {
                m.CopyPropertiesFromMaterial(font.material);
                m.EnableKeyword("OUTLINE_ON");
                m.DisableKeyword("UNDERLAY_ON");
                m.SetFloat("_FaceDilate", 0.08f);
                m.SetFloat("_OutlineWidth", 0.2f);
                m.SetColor("_OutlineColor", new Color(0.01f, 0.03f, 0.08f, 1f));
            });
        }
    }
}
