using System.Collections.Generic;
using System.Linq;
using Gamebox;
using Gamebox.Editor;
using Gamebox.UI;
using Portfolio.EndlessRunner.Server;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Portfolio.EndlessRunner.EditorTools
{
    /// <summary>
    /// Builds the Endless Runner scene from the generated assets: environment, camera, runner, the game object with
    /// the manager, track pools, effects and audio, the shared menu (restyled as the pause menu), the runner's own
    /// canvas with the level select, the HUD and the results screen, and online play (the server client, the online
    /// controller and the shared lobby, through <see cref="OnlineInstaller"/>).
    /// </summary>
    internal static class RunnerSceneBuilder
    {
        public const string ScenePath = "Scenes/EndlessRunner.unity";
        /// <summary>The name the module of Server/ is published under (spacetime.json of the game's project).</summary>
        public const string Database = "skinnerboxes-endlessrunner";

        private static readonly Color PanelColor = new Color(0.06f, 0.08f, 0.2f, 0.86f);
        private static readonly Color Gold = new Color(1f, 0.83f, 0.26f);
        private static readonly Color Green = new Color(0.3f, 0.76f, 0.34f);
        private static readonly Color Orange = new Color(0.98f, 0.58f, 0.2f);
        private static readonly Color Blue = new Color(0.26f, 0.55f, 0.95f);
        private static readonly Color Red = new Color(0.9f, 0.32f, 0.32f);
        private static readonly Color Soft = new Color(0.84f, 0.87f, 0.95f);

        private static Material titleFont;
        private static Material hudFont;
        private static Sprite panelSprite;
        private static Sprite buttonSprite;

        public static void Build()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            PrepareFonts();
            panelSprite = RunnerArtBuilder.Icon("Panel");
            buttonSprite = RunnerArtBuilder.Icon("Button");

            RunnerLevel firstLevel = RunnerContentBuilder.FirstLevel;
            RunnerTheme theme = firstLevel.Theme;
            Material sky = RunnerArtBuilder.Material("Sky");

            Light sun = BuildSun();
            RenderSettings.skybox = sky;
            RenderSettings.sun = sun;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            ThemeController.ApplyToScene(theme, sun, sky);
            ConfigureLighting();

            Camera camera = BuildCamera(out RunnerCamera runnerCamera);
            var volume = new GameObject("Global Volume").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = RunnerContentBuilder.VolumeProfile;

            var playerPrefab = RunnerAssets.Load<GameObject>("Prefabs/Player.prefab");
            var playerObject = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            playerObject.name = "Runner";
            playerObject.transform.position = new Vector3(0f, 0.05f, 0f);
            var player = playerObject.GetComponent<RunnerPlayer>();
            runnerCamera.target = player;
            runnerCamera.view = camera;
            camera.transform.position = new Vector3(2.4f, 1.4f, 4.3f);
            camera.transform.LookAt(new Vector3(0.75f, 1.05f, 0f));

            var game = new GameObject("Game");
            var manager = game.AddComponent<RunnerGameManager>();
            var storage = game.AddComponent<Storage>();
            var controller = game.AddComponent<RunnerController>();
            var track = game.AddComponent<TrackGenerator>();
            var themes = game.AddComponent<ThemeController>();
            var effects = game.AddComponent<RunnerEffects>();
            var sounds = game.AddComponent<RunnerAudio>();

            RunnerAssets.Set(storage, "DefaultStorageStrategy", p => p.enumValueIndex = (int)StorageStrategies.PlayerPrefs);
            BuildPools(game.transform, track);
            BuildEffects(game.transform, effects, themes, camera.transform);
            BuildAudio(sounds);
            themes.sun = sun;
            themes.skyMaterial = sky;
            themes.follow = camera.transform;

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            GameObject menu = GameMenuInstaller.InstallMenu(scene, 100, false);
            var ui = GameMenuInstaller.EnsureComponent<RunnerUI>(menu);
            GameMenuInstaller.WireGameUI(ui, menu, controller, manager, menu.GetComponent<CanvasScaler>());
            var defaults = RunnerAssets.Load<RunnerSettings>("Config/RunnerSettings.asset");
            var settingsUi = (RunnerSettingsUI)GameMenuInstaller.WireSettingsPanel(menu, typeof(RunnerSettingsUI), ui, defaults);
            WireSettingsFields(menu, settingsUi);
            RestyleMenu(menu);

            var campaign = RunnerAssets.Load<Campaign>("Config/Campaign/RunnerCampaign.asset");
            BuildRunnerCanvas(ui, campaign != null ? campaign.Count : 9);

            RunnerAssets.SetObject(controller, "UI", ui);
            RunnerAssets.SetObject(controller, "BaseManager", manager);
            RunnerAssets.SetObject(manager, "runnerSettings", firstLevel.Settings);
            RunnerAssets.SetObject(manager, "controller", controller);
            RunnerAssets.SetObject(manager, "ui", ui);
            RunnerAssets.SetObject(manager, "campaign", campaign);
            RunnerAssets.SetObject(manager, "StorageBehaviour", storage);
            RunnerAssets.Set(manager, "GameIdentifier", p => p.intValue = (int)GameType.EndlessRunner);
            RunnerAssets.SetObjects(manager, "PlayerList", player);
            manager.runner = player;
            manager.track = track;
            manager.themes = themes;
            manager.runnerCamera = runnerCamera;
            manager.effects = effects;
            manager.sounds = sounds;
            track.catalog = RunnerContentBuilder.Catalog;

            // Online play: the ghosts of the other runners of a race are copies of the runner prefab. A race has no turns,
            // so the lobby's turn banner stays off.
            manager.ghostPrefab = playerPrefab;
            manager.ghostLabelMaterial = hudFont;
            manager.online = (RunnerOnlineController)OnlineInstaller.Install(scene, typeof(GameServerClient), typeof(RunnerOnlineController),
                manager, ui, Database, new OnlineInstaller.LobbyStyle
                {
                    Title = "MULTIPLAYER",
                    Accent = Blue,
                    Window = new Color(0.07f, 0.09f, 0.22f, 0.98f),
                    Row = new Color(0.13f, 0.17f, 0.36f, 1f),
                    HideTurnBanner = true
                });

            GameMenuInstaller.EnsureUrpCameras();
            string path = RunnerAssets.Path(ScenePath);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, path);
            GameSceneBuildSettings.Sync(true);
        }

        // ------------------------------------------------------------------ environment

        private static Light BuildSun()
        {
            var sunObject = new GameObject("Sun");
            var sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            sun.shadowBias = 0.05f;
            sun.shadowNormalBias = 0.4f;
            sun.lightmapBakeType = LightmapBakeType.Realtime;
            sunObject.AddComponent<UniversalAdditionalLightData>();
            return sun;
        }

        private static void ConfigureLighting()
        {
            string path = RunnerAssets.Path("Scenes/EndlessRunnerSettings.lighting");
            var lighting = AssetDatabase.LoadAssetAtPath<LightingSettings>(path);
            if (lighting == null)
            {
                lighting = new LightingSettings { name = "EndlessRunnerSettings" };
                AssetDatabase.CreateAsset(lighting, path);
            }
            lighting.bakedGI = false;
            lighting.realtimeGI = false;
            lighting.autoGenerate = false;
            EditorUtility.SetDirty(lighting);
            Lightmapping.lightingSettings = lighting;
        }

        private static Camera BuildCamera(out RunnerCamera runnerCamera)
        {
            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.fieldOfView = 58f;
            camera.nearClipPlane = 0.2f;
            camera.farClipPlane = 450f;
            camera.allowHDR = true;
            cameraObject.AddComponent<AudioListener>();
            UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.High;
            runnerCamera = cameraObject.AddComponent<RunnerCamera>();
            return camera;
        }

        // ------------------------------------------------------------------ pools

        private static void BuildPools(Transform parent, TrackGenerator track)
        {
            var pools = new GameObject("Pools").transform;
            pools.SetParent(parent, false);
            var tileCaches = new List<TerrainCache>
            {
                TilePool(pools, RunnerArtBuilder.Tile(false), 90),
                TilePool(pools, RunnerArtBuilder.Tile(true), 12)
            };
            PieceCatalog catalog = RunnerContentBuilder.Catalog;
            var pieces = new List<TrackPiece>
            {
                catalog.coin, catalog.gem, catalog.magnet, catalog.shield, catalog.multiplier, catalog.superJump, catalog.jumpPad,
                catalog.hurdle, catalog.barrier, catalog.ramp, catalog.bridge, catalog.cart, catalog.finishLine
            };
            pieces.AddRange(catalog.blocks);
            pieces.AddRange(catalog.shortWagons);
            pieces.AddRange(catalog.longWagons);
            pieces.AddRange(RunnerArtBuilder.AllScenery());
            var piecePools = new List<TrackPiecePool>();
            foreach (TrackPiece piece in pieces.Where(piece => piece != null).Distinct())
            {
                int size = piece == catalog.coin ? 500 : piece is Obstacle || piece is Collidable ? 60 : 260;
                piecePools.Add(PiecePool(pools, piece, size));
            }
            track.tilePools = tileCaches.ToArray();
            track.piecePools = piecePools.ToArray();
        }

        private static TerrainCache TilePool(Transform parent, TerrainBehaviour prefab, int size)
        {
            var go = new GameObject($"{prefab.name} Pool");
            go.transform.SetParent(parent, false);
            var cache = go.AddComponent<TerrainCache>();
            cache.prefab = prefab;
            RunnerAssets.SetObject(cache, "Instance", prefab.gameObject);
            RunnerAssets.Set(cache, "MaxSize", p => p.intValue = size);
            return cache;
        }

        private static TrackPiecePool PiecePool(Transform parent, TrackPiece prefab, int size)
        {
            var go = new GameObject($"{prefab.name} Pool");
            go.transform.SetParent(parent, false);
            var pool = go.AddComponent<TrackPiecePool>();
            pool.prefab = prefab;
            RunnerAssets.SetObject(pool, "Instance", prefab.gameObject);
            RunnerAssets.Set(pool, "MaxSize", p => p.intValue = size);
            return pool;
        }

        // ------------------------------------------------------------------ effects and audio

        private static void BuildEffects(Transform parent, RunnerEffects effects, ThemeController themes, Transform camera)
        {
            var root = new GameObject("Effects").transform;
            root.SetParent(parent, false);
            Material sparkle = RunnerArtBuilder.Material("ParticleSparkle");
            Material glow = RunnerAssets.Load<Material>("Art/Materials/ParticleGlow.mat");
            Material soft = RunnerArtBuilder.Material("ParticleSoft");
            Material smoke = RunnerArtBuilder.Material("ParticleSmoke");
            Material confetti = RunnerArtBuilder.Material("ParticleConfetti");
            Material debris = RunnerArtBuilder.Material("ParticleDebris");

            effects.coinSparkle = Burst(root, "CoinSparkle", sparkle, 0.3f, 0.55f, 2f, 4.5f, 0.25f, 0.5f, new Color(1f, 0.92f, 0.4f), new Color(1f, 1f, 0.85f), 0.3f);
            effects.gemSparkle = Burst(root, "GemSparkle", sparkle, 0.4f, 0.8f, 3f, 6f, 0.3f, 0.6f, new Color(1f, 0.45f, 0.95f), new Color(0.7f, 0.6f, 1f), 0.3f);
            effects.powerUpBurst = Burst(root, "PowerUpBurst", sparkle, 0.5f, 0.9f, 4f, 8f, 0.35f, 0.7f, Color.white, Color.white, 0.1f);
            effects.shieldBreak = Burst(root, "ShieldBreak", sparkle, 0.4f, 0.7f, 4f, 9f, 0.3f, 0.6f, new Color(0.5f, 0.85f, 1f), Color.white, 0.4f);
            effects.bounceRing = Burst(root, "BounceRing", glow, 0.3f, 0.5f, 2f, 4f, 0.3f, 0.55f, new Color(0.4f, 1f, 0.5f), new Color(0.8f, 1f, 0.6f), -0.2f);
            ParticleFactory.Circle(effects.bounceRing, 0.8f, true);

            ParticleSystem crash = ParticleFactory.Create("CrashDebris", root, debris);
            ParticleFactory.Lifetime(crash, 0.8f, 1.3f);
            ParticleFactory.Speed(crash, 5f, 10f);
            ParticleFactory.Size(crash, 0.12f, 0.32f);
            ParticleFactory.RandomColors(crash, new Color(0.72f, 0.48f, 0.27f), new Color(0.5f, 0.33f, 0.19f), new Color(0.6f, 0.62f, 0.66f), new Color(0.95f, 0.8f, 0.3f));
            ParticleFactory.Gravity(crash, 2.4f);
            ParticleFactory.Sphere(crash, 0.4f, true);
            ParticleFactory.Tumble(crash, 540f);
            ParticleFactory.MeshParticles(crash, Resources.GetBuiltinResource<Mesh>("Cube.fbx"));
            effects.crashDebris = crash;

            effects.crashPuff = Puff(root, "CrashPuff", smoke, 0.5f, 0.8f, 1f, 2.5f, 0.8f, 1.5f, new Color(0.9f, 0.9f, 0.92f, 0.8f), 0.3f);
            effects.landPuff = Puff(root, "LandPuff", smoke, 0.35f, 0.55f, 1.2f, 2.2f, 0.35f, 0.65f, new Color(0.85f, 0.8f, 0.7f, 0.6f), 0.3f);
            ParticleFactory.Circle(effects.landPuff, 0.35f, true);

            ParticleSystem confettiSystem = ParticleFactory.Create("Confetti", root, confetti);
            ParticleFactory.Lifetime(confettiSystem, 2.5f, 4f);
            ParticleFactory.Speed(confettiSystem, 6f, 13f);
            ParticleFactory.Size(confettiSystem, 0.14f, 0.26f);
            ParticleFactory.RandomColors(confettiSystem, new Color(1f, 0.3f, 0.3f), new Color(1f, 0.85f, 0.2f), new Color(0.3f, 0.6f, 1f),
                new Color(0.35f, 0.9f, 0.4f), new Color(1f, 0.5f, 0.85f), new Color(0.7f, 0.45f, 1f));
            ParticleFactory.Gravity(confettiSystem, 0.9f);
            ParticleFactory.Cone(confettiSystem, 55f, 3f);
            ParticleFactory.Tumble(confettiSystem, 720f);
            ParticleFactory.Noise(confettiSystem, 1.2f, 0.6f);
            ParticleFactory.MaxParticles(confettiSystem, 600);
            var confettiRenderer = confettiSystem.GetComponent<ParticleSystemRenderer>();
            confettiRenderer.alignment = ParticleSystemRenderSpace.World;
            effects.confetti = confettiSystem;

            ParticleSystem splash = ParticleFactory.Create("Splash", root, soft);
            ParticleFactory.Lifetime(splash, 0.6f, 1f);
            ParticleFactory.Speed(splash, 4f, 8f);
            ParticleFactory.Size(splash, 0.2f, 0.45f);
            ParticleFactory.Gravity(splash, 2f);
            ParticleFactory.Cone(splash, 25f, 0.6f);
            ParticleFactory.FadeOut(splash);
            effects.splash = splash;

            var ambient = new GameObject("Ambient").transform;
            ambient.SetParent(parent, false);
            themes.pollen = Ambient(ambient, "Pollen", soft, new Vector3(40f, 8f, 40f), 30f, 5f, 0.07f, 0.13f, new Color(1f, 1f, 0.85f, 0.8f), Vector3.zero, 0.6f);
            themes.dust = Ambient(ambient, "Dust", soft, new Vector3(40f, 2.5f, 40f), 20f, 4f, 0.05f, 0.09f, new Color(1f, 0.82f, 0.58f, 0.35f), new Vector3(2f, 0.1f, 0f), 0.4f);
            themes.snow = Ambient(ambient, "Snow", soft, new Vector3(50f, 1f, 50f), 140f, 6f, 0.1f, 0.2f, new Color(1f, 1f, 1f, 0.9f), new Vector3(0.3f, -2.5f, 0f), 0.5f);
            themes.fireflies = Ambient(ambient, "Fireflies", glow, new Vector3(40f, 5f, 40f), 22f, 5f, 0.18f, 0.3f, new Color(0.8f, 1f, 0.4f, 1f), Vector3.zero, 1f);
            themes.embers = Ambient(ambient, "Embers", glow, new Vector3(40f, 2f, 40f), 45f, 4f, 0.08f, 0.16f, new Color(1f, 0.55f, 0.15f, 1f), new Vector3(0f, 1.8f, 0f), 0.8f);
        }

        private static ParticleSystem Burst(Transform parent, string name, Material material, float minLife, float maxLife, float minSpeed, float maxSpeed,
            float minSize, float maxSize, Color a, Color b, float gravity)
        {
            ParticleSystem system = ParticleFactory.Create(name, parent, material);
            ParticleFactory.Lifetime(system, minLife, maxLife);
            ParticleFactory.Speed(system, minSpeed, maxSpeed);
            ParticleFactory.Size(system, minSize, maxSize);
            ParticleFactory.Colors(system, a, b);
            ParticleFactory.Gravity(system, gravity);
            ParticleFactory.Sphere(system, 0.25f);
            ParticleFactory.SizeOverLife(system, 1f, 0.1f);
            ParticleFactory.FadeOut(system);
            ParticleFactory.Spin(system, 360f);
            return system;
        }

        private static ParticleSystem Puff(Transform parent, string name, Material material, float minLife, float maxLife, float minSpeed, float maxSpeed,
            float minSize, float maxSize, Color color, float radius)
        {
            ParticleSystem system = ParticleFactory.Create(name, parent, material);
            ParticleFactory.Lifetime(system, minLife, maxLife);
            ParticleFactory.Speed(system, minSpeed, maxSpeed);
            ParticleFactory.Size(system, minSize, maxSize);
            ParticleFactory.Colors(system, color, color);
            ParticleFactory.Sphere(system, radius, true);
            ParticleFactory.SizeOverLife(system, 0.6f, 1.8f);
            ParticleFactory.FadeOut(system);
            ParticleFactory.Spin(system, 90f);
            return system;
        }

        private static ParticleSystem Ambient(Transform parent, string name, Material material, Vector3 box, float rate, float life, float minSize, float maxSize,
            Color color, Vector3 drift, float noise)
        {
            ParticleSystem system = ParticleFactory.Create(name, parent, material);
            ParticleFactory.Lifetime(system, life * 0.7f, life);
            ParticleFactory.Speed(system, 0f, 0.3f);
            ParticleFactory.Size(system, minSize, maxSize);
            ParticleFactory.Colors(system, color, color * new Color(1f, 1f, 1f, 0.6f));
            ParticleFactory.Box(system, box);
            ParticleFactory.Rate(system, rate);
            ParticleFactory.FadeOut(system, 0.15f);
            ParticleFactory.MaxParticles(system, 800);
            if (drift != Vector3.zero)
            {
                ParticleFactory.Velocity(system, drift * 0.7f, drift * 1.3f);
            }
            if (noise > 0f)
            {
                ParticleFactory.Noise(system, noise, 0.4f);
            }
            system.gameObject.SetActive(true);
            ParticleSystem.MainModule main = system.main;
            main.playOnAwake = false;
            return system;
        }

        private static void BuildAudio(RunnerAudio sounds)
        {
            GameObject host = sounds.gameObject;
            sounds.effects = Source(host, false, 0.9f);
            sounds.coinSource = Source(host, false, 0.8f);
            sounds.music = Source(host, true, 0.4f);
            sounds.coin = RunnerArtBuilder.Sound("Coin");
            sounds.gem = RunnerArtBuilder.Sound("Gem");
            sounds.jump = RunnerArtBuilder.Sound("Jump");
            sounds.slide = RunnerArtBuilder.Sound("Slide");
            sounds.land = RunnerArtBuilder.Sound("Land");
            sounds.crash = RunnerArtBuilder.Sound("Crash");
            sounds.bump = RunnerArtBuilder.Sound("Bump");
            sounds.whoosh = RunnerArtBuilder.Sound("Whoosh");
            sounds.powerUp = RunnerArtBuilder.Sound("PowerUp");
            sounds.shieldBreak = RunnerArtBuilder.Sound("ShieldBreak");
            sounds.bounce = RunnerArtBuilder.Sound("Bounce");
            sounds.splash = RunnerArtBuilder.Sound("Splash");
            sounds.countdown = RunnerArtBuilder.Sound("Countdown");
            sounds.go = RunnerArtBuilder.Sound("Go");
            sounds.victory = RunnerArtBuilder.Sound("Victory");
            sounds.gameOver = RunnerArtBuilder.Sound("GameOver");
            sounds.star = RunnerArtBuilder.Sound("Star");
            sounds.click = RunnerArtBuilder.Sound("Click");
            sounds.menuMusic = RunnerArtBuilder.Sound("MenuMusic");
            sounds.runMusic = RunnerArtBuilder.Sound("RunMusic");
            sounds.musicVolume = 0.4f;
        }

        private static AudioSource Source(GameObject host, bool loop, float volume)
        {
            var source = host.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.volume = volume;
            source.spatialBlend = 0f;
            return source;
        }

        // ------------------------------------------------------------------ shared menu

        private static void WireSettingsFields(GameObject menu, RunnerSettingsUI settingsUi)
        {
            var fields = new (string name, string label, TMP_InputField.ContentType type, string property)[]
            {
                ("ForwardSpeed", "Start speed (m/s)", TMP_InputField.ContentType.DecimalNumber, "forwardSpeed"),
                ("MaxSpeed", "Top speed (m/s)", TMP_InputField.ContentType.DecimalNumber, "maxSpeed"),
                ("Acceleration", "Acceleration", TMP_InputField.ContentType.DecimalNumber, "acceleration"),
                ("SideSpeed", "Lane switch speed (m/s)", TMP_InputField.ContentType.DecimalNumber, "sideSpeed"),
                ("JumpHeight", "Jump height (m)", TMP_InputField.ContentType.DecimalNumber, "jumpHeight"),
                ("Hearts", "Hearts", TMP_InputField.ContentType.IntegerNumber, "hearts")
            };
            foreach ((string name, string label, TMP_InputField.ContentType type, string property) in fields)
            {
                TMP_InputField field = GameMenuInstaller.AddInputField(menu, name, label, type);
                RunnerAssets.SetObject(settingsUi, property, field);
            }
        }

        /// <summary>The shared menu becomes the pause menu: relabelled, recoloured, without save/load.</summary>
        private static void RestyleMenu(GameObject menu)
        {
            Transform root = menu.transform;
            foreach (string hidden in new[] { "Score", "Level", "Timer", "SaveGame", "LoadGame" })
            {
                Transform child = GameMenuInstaller.FindChild(root, hidden);
                if (child != null)
                {
                    child.gameObject.SetActive(false);
                }
            }
            Transform panel = GameMenuInstaller.FindChild(root, "MenuPanel");
            if (panel != null && panel.TryGetComponent(out Image panelImage))
            {
                panelImage.sprite = panelSprite;
                panelImage.type = UnityEngine.UI.Image.Type.Sliced;
                panelImage.color = PanelColor;
            }
            Transform settings = GameMenuInstaller.FindChild(root, "SettingsPanel");
            if (settings != null && settings.TryGetComponent(out Image settingsImage))
            {
                settingsImage.color = new Color(0.1f, 0.12f, 0.27f, 0.97f);
            }
            SetLabel(GameMenuInstaller.FindChildComponent<TMP_Text>(root, "Header"), "PAUSED", Gold);
            SetLabel(GameMenuInstaller.FindChildComponent<TMP_Text>(root, "SettingsHeader"), "SETTINGS", Gold);
            StyleMenuButton(root, "ReturnToGame", "Resume", Green);
            StyleMenuButton(root, "StartNewGame", "Restart Level", Orange);
            StyleMenuButton(root, "Game Settings", "Settings", Blue);
            StyleMenuButton(root, "ExitGame", "Quit Game", Red);
            Transform resume = GameMenuInstaller.FindChild(root, "ReturnToGame");
            if (resume != null)
            {
                resume.SetSiblingIndex(0);
            }
        }

        private static void SetLabel(TMP_Text text, string label, Color color)
        {
            if (text == null)
            {
                return;
            }
            text.text = label;
            text.color = color;
            text.fontStyle = FontStyles.Bold;
            text.fontSharedMaterial = titleFont;
        }

        private static void StyleMenuButton(Transform root, string name, string label, Color color)
        {
            Transform button = GameMenuInstaller.FindChild(root, name);
            if (button == null)
            {
                return;
            }
            if (button.TryGetComponent(out Image image))
            {
                image.sprite = buttonSprite;
                image.type = UnityEngine.UI.Image.Type.Sliced;
                image.color = color;
            }
            var text = button.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
            {
                text.text = label;
                text.color = Color.white;
                text.fontStyle = FontStyles.Bold;
                text.fontSharedMaterial = hudFont;
            }
        }

        // ------------------------------------------------------------------ runner canvas

        private static void BuildRunnerCanvas(RunnerUI ui, int levelCount)
        {
            var canvasObject = new GameObject("RunnerCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.layer = 5;
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            // Expands from 1920 x 1080 to any screen shape; the screens keep their texts and buttons in a safe area.
            UIBuildUtils.ConfigureScaler(canvasObject.GetComponent<CanvasScaler>());
            Transform root = canvasObject.transform;

            BuildHud(root, ui);
            BuildTitle(root, ui, levelCount);
            BuildResults(root, ui);

            Image curtain = Image(root, "Curtain", null, new Color(0.02f, 0.03f, 0.08f, 0f), Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            StretchFull(curtain.rectTransform);
            curtain.raycastTarget = false;
            ui.curtain = curtain;

            ui.starFull = RunnerArtBuilder.Icon("Star");
            ui.starEmpty = RunnerArtBuilder.Icon("StarEmpty");
            ui.heartFull = RunnerArtBuilder.Icon("Heart");
            ui.heartEmpty = RunnerArtBuilder.Icon("HeartEmpty");
        }

        private static void BuildTitle(Transform canvas, RunnerUI ui, int levelCount)
        {
            CanvasGroup screen = Screen(canvas, "LevelSelect");
            Transform root = screen.transform;
            ui.titleScreen = screen;

            Image fade = Image(root, "BottomFade", RunnerArtBuilder.Icon("Fade"), new Color(0.02f, 0.03f, 0.1f, 0.8f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(1920f, 420f));
            fade.rectTransform.anchorMin = new Vector2(0f, 0f);
            fade.rectTransform.anchorMax = new Vector2(1f, 0f);
            fade.rectTransform.sizeDelta = new Vector2(0f, 420f);
            fade.raycastTarget = false;
            root = UIBuildUtils.CreateSafeArea(root);

            RectTransform logo = Rect(root, "Logo", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(1100f, 270f));
            logo.localRotation = Quaternion.Euler(0f, 0f, 2.5f);
            TextMeshProUGUI endless = Text(logo, "Endless", "ENDLESS", 116f, Color.white, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-40f, 0f), new Vector2(1100f, 130f), titleFont);
            Gradient(endless, new Color(1f, 0.97f, 0.55f), new Color(1f, 0.62f, 0.12f));
            TextMeshProUGUI runner = Text(logo, "Runner", "RUNNER", 136f, Color.white, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(60f, -110f), new Vector2(1100f, 150f), titleFont);
            Gradient(runner, new Color(0.55f, 0.95f, 1f), new Color(0.15f, 0.5f, 1f));
            endless.characterSpacing = 6f;
            runner.characterSpacing = 6f;

            // Level details
            RectTransform details = Panel(root, "Details", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(50f, 110f), new Vector2(580f, 560f));
            ui.detailWorld = Text(details, "World", "SUNNY MEADOWS", 28f, Gold, TextAlignmentOptions.Left, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(520f, 40f), hudFont);
            ui.detailTitle = Text(details, "Title", "1. Sunny Start", 50f, Color.white, TextAlignmentOptions.Left, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -64f), new Vector2(520f, 70f), titleFont);
            ui.detailStars = Stars(details, "Stars", new Vector2(0.5f, 1f), new Vector2(-190f, -170f), 64f, 70f, false);
            ui.detailDescription = Text(details, "Description", "Description", 26f, Soft, TextAlignmentOptions.TopLeft, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -210f), new Vector2(520f, 120f), null, false);
            ui.detailGoals = Text(details, "Goals", "Goals", 25f, new Color(1f, 0.93f, 0.7f), TextAlignmentOptions.TopLeft, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -330f), new Vector2(520f, 110f), null, false);
            ui.playButton = Button(details, "Play", "PLAY", RunnerArtBuilder.Icon("Play"), Green, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(440f, 108f), 56f, out TextMeshProUGUI playLabel);
            ui.playLabel = playLabel;

            // Progress and menu buttons
            RectTransform progress = Panel(root, "Progress", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-50f, 110f), new Vector2(420f, 560f));
            Text(progress, "Header", "YOUR STARS", 30f, Gold, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(380f, 40f), hudFont);
            Image(progress, "StarIcon", RunnerArtBuilder.Icon("Star"), Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-110f, -72f), new Vector2(84f, 84f));
            ui.starsTotal = Text(progress, "Total", "0 / 24", 54f, Color.white, TextAlignmentOptions.Left, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(60f, -80f), new Vector2(240f, 70f), titleFont);
            // A race against other players, in a room of the game's server.
            ui.multiplayerButton = Button(progress, "Multiplayer", "Multiplayer", RunnerArtBuilder.Icon("Runner"), Orange, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -178f), new Vector2(340f, 82f), 32f, out _);
            ui.titleSettingsButton = Button(progress, "Settings", "Settings", RunnerArtBuilder.Icon("Settings"), Blue, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -276f), new Vector2(340f, 82f), 34f, out _);
            ui.titleExitButton = Button(progress, "Exit", "Quit", RunnerArtBuilder.Icon("Exit"), Red, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -374f), new Vector2(340f, 82f), 34f, out _);
            ui.resetProgressButton = TextButton(progress, "ResetProgress", "Reset progress", new Vector2(0.5f, 0f), new Vector2(0f, 58f), new Vector2(300f, 40f), 22f, out TextMeshProUGUI resetLabel);
            ui.resetProgressLabel = resetLabel;
            TextMeshProUGUI keys = Text(progress, "Controls", "Arrows / WASD / Space - or swipe", 20f, new Color(0.7f, 0.74f, 0.85f), TextAlignmentOptions.Center, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(390f, 34f), null, false);
            UIBuildUtils.ShowOnly(keys.gameObject, TouchLayout.Visibility.WithoutTouch);
            TextMeshProUGUI swipes = Text(progress, "TouchControls", "Swipe to switch lanes, jump and slide", 20f, new Color(0.7f, 0.74f, 0.85f), TextAlignmentOptions.Center, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(390f, 34f), null, false);
            UIBuildUtils.ShowOnly(swipes.gameObject, TouchLayout.Visibility.TouchOnly);

            // Level strip
            RectTransform strip = Rect(root, "Levels", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(1860f, 250f));
            var layout = strip.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            var cards = new List<LevelCard>();
            for (int i = 0; i < levelCount; i++)
            {
                cards.Add(Card(strip, i));
            }
            ui.levelCards = cards.ToArray();
        }

        private static LevelCard Card(Transform parent, int index)
        {
            RectTransform rect = Rect(parent, $"Level{index + 1}", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(186f, 224f));
            var card = rect.gameObject.AddComponent<LevelCard>();
            Image frame = Image(rect, "Frame", panelSprite, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(204f, 242f), true);
            frame.enabled = false;
            Image background = Image(rect, "Background", panelSprite, Green, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(186f, 224f), true);
            Image shine = Image(rect, "Shine", RunnerArtBuilder.Icon("Fade"), new Color(1f, 1f, 1f, 0.18f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(170f, 100f));
            shine.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 180f);
            shine.raycastTarget = false;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.colors = ButtonColors();
            card.button = button;
            card.background = background;
            card.frame = frame;
            card.number = Text(rect, "Number", (index + 1).ToString(), 78f, Color.white, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(180f, 100f), titleFont);
            Image endless = Image(rect, "Endless", RunnerArtBuilder.Icon("Infinity"), Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(110f, 90f));
            endless.gameObject.SetActive(false);
            card.endlessIcon = endless.gameObject;
            card.title = Text(rect, "Title", "Level", 21f, Color.white, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(172f, 56f), hudFont);
            card.stars = Stars(rect, "Stars", new Vector2(0.5f, 0f), new Vector2(0f, 30f), 42f, 50f, true);
            RectTransform locked = Rect(rect, "Locked", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(186f, 224f));
            Image dim = Image(locked, "Dim", panelSprite, new Color(0f, 0f, 0.05f, 0.45f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(186f, 224f), true);
            dim.raycastTarget = false;
            Image lockIcon = Image(locked, "Lock", RunnerArtBuilder.Icon("Lock"), Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 48f), new Vector2(78f, 78f));
            lockIcon.raycastTarget = false;
            card.lockIcon = locked.gameObject;
            return card;
        }

        private static void BuildHud(Transform canvas, RunnerUI ui)
        {
            CanvasGroup screen = Screen(canvas, "HUD");
            Transform root = screen.transform;
            ui.hudScreen = screen;

            Image flash = Image(root, "DamageFlash", RunnerArtBuilder.Icon("Vignette"), new Color(1f, 0.1f, 0.1f, 0f), Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            StretchFull(flash.rectTransform);
            flash.raycastTarget = false;
            ui.damageFlash = flash;
            root = UIBuildUtils.CreateSafeArea(root);

            RectTransform coins = Panel(root, "Coins", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -24f), new Vector2(330f, 96f));
            Image coinIcon = Image(coins, "Icon", RunnerArtBuilder.Icon("Coin"), Color.white, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(56f, 0f), new Vector2(78f, 78f));
            ui.coinsIcon = coinIcon.rectTransform;
            ui.coinsText = Text(coins, "Value", "0", 56f, Color.white, TextAlignmentOptions.Left, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(104f, 0f), new Vector2(220f, 80f), hudFont);
            RectTransform badge = Panel(root, "Multiplier", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(370f, -42f), new Vector2(110f, 62f), new Color(0.55f, 0.25f, 0.9f, 0.95f));
            Text(badge, "Label", "x2", 42f, Color.white, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(110f, 62f), titleFont);
            badge.gameObject.SetActive(false);
            ui.multiplierBadge = badge.gameObject;
            Text(root, "ScoreLabel", "SCORE", 24f, new Color(1f, 1f, 1f, 0.75f), TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -132f), new Vector2(120f, 40f), hudFont);
            ui.scoreText = Text(root, "Score", "0", 38f, Color.white, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(140f, -126f), new Vector2(300f, 48f), hudFont);
            ui.raceHud = BuildRaceHud(root);

            ui.levelText = Text(root, "Level", "LEVEL 1", 28f, Color.white, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(1000f, 42f), hudFont);
            RectTransform progress = Rect(root, "Progress", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(640f, 28f));
            Image back = Image(progress, "Back", panelSprite, new Color(0f, 0f, 0.05f, 0.55f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(652f, 40f), true);
            back.raycastTarget = false;
            Image fill = Image(progress, "Fill", RunnerArtBuilder.Icon("Button"), Gold, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640f, 28f));
            fill.type = UnityEngine.UI.Image.Type.Filled;
            fill.preserveAspect = false;
            fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 0f;
            ui.progressFill = fill;
            Image flag = Image(progress, "Flag", RunnerArtBuilder.Icon("Flag"), Color.white, new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(18f, 16f), new Vector2(58f, 58f));
            flag.raycastTarget = false;
            Image marker = Image(progress, "Marker", RunnerArtBuilder.Icon("Runner"), Color.white, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60f, 60f));
            ui.progressMarker = marker.rectTransform;
            ui.progressRoot = progress.gameObject;
            ui.distanceText = Text(root, "Distance", "0 m", 30f, Color.white, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -104f), new Vector2(600f, 44f), hudFont);

            RectTransform hearts = Rect(root, "Hearts", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-128f, -30f), new Vector2(9 * 62f, 64f));
            var heartLayout = hearts.gameObject.AddComponent<HorizontalLayoutGroup>();
            heartLayout.childAlignment = TextAnchor.MiddleRight;
            heartLayout.spacing = 4f;
            heartLayout.childControlWidth = false;
            heartLayout.childControlHeight = false;
            heartLayout.childForceExpandWidth = false;
            heartLayout.childForceExpandHeight = false;
            var heartImages = new List<Image>();
            for (int i = 0; i < RunnerSettings.HeartLimit; i++)
            {
                Image heart = Image(hearts, $"Heart{i + 1}", RunnerArtBuilder.Icon("Heart"), Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(58f, 58f));
                heart.raycastTarget = false;
                heart.gameObject.SetActive(i < 3);
                heartImages.Add(heart);
            }
            ui.hearts = heartImages.ToArray();
            ui.heartsRoot = hearts;

            Image pause = Image(root, "Pause", RunnerArtBuilder.Icon("Pause"), Color.white, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-26f, -24f), new Vector2(86f, 86f));
            var pauseButton = pause.gameObject.AddComponent<Button>();
            pauseButton.targetGraphic = pause;
            pauseButton.colors = ButtonColors();
            ui.pauseButton = pauseButton;

            RectTransform powerUps = Rect(root, "PowerUps", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-28f, 40f), new Vector2(116f, 500f));
            var powerLayout = powerUps.gameObject.AddComponent<VerticalLayoutGroup>();
            powerLayout.spacing = 12f;
            powerLayout.childAlignment = TextAnchor.MiddleCenter;
            powerLayout.childControlWidth = false;
            powerLayout.childControlHeight = false;
            powerLayout.childForceExpandWidth = false;
            powerLayout.childForceExpandHeight = false;
            var slots = new List<RunnerUI.PowerUpSlot>();
            var icons = new[] { ("Magnet", PowerUpType.Magnet, new Color(1f, 0.35f, 0.35f)), ("Shield", PowerUpType.Shield, new Color(0.35f, 0.75f, 1f)),
                ("Multiplier", PowerUpType.Multiplier, new Color(0.75f, 0.45f, 1f)), ("Spring", PowerUpType.SuperJump, new Color(0.4f, 1f, 0.45f)) };
            foreach ((string icon, PowerUpType type, Color color) in icons)
            {
                RectTransform slot = Rect(powerUps, type.ToString(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(110f, 110f));
                Image(slot, "Back", RunnerArtBuilder.Icon("Glow"), new Color(0f, 0f, 0.05f, 0.75f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(130f, 130f)).raycastTarget = false;
                Image ringFill = Image(slot, "Timer", RunnerArtBuilder.Icon("Ring"), color, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(112f, 112f));
                ringFill.type = UnityEngine.UI.Image.Type.Filled;
                ringFill.preserveAspect = false;
                ringFill.fillMethod = UnityEngine.UI.Image.FillMethod.Radial360;
                ringFill.fillOrigin = (int)UnityEngine.UI.Image.Origin360.Top;
                ringFill.fillClockwise = false;
                ringFill.raycastTarget = false;
                Image(slot, "Icon", RunnerArtBuilder.Icon(icon), Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(66f, 66f)).raycastTarget = false;
                slot.gameObject.SetActive(false);
                slots.Add(new RunnerUI.PowerUpSlot { type = type, root = slot.gameObject, fill = ringFill });
            }
            ui.powerUpSlots = slots.ToArray();

            TextMeshProUGUI countdown = Text(root, "Countdown", "3", 230f, Gold, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 80f), new Vector2(1000f, 320f), titleFont);
            countdown.gameObject.SetActive(false);
            ui.countdownText = countdown;
            TextMeshProUGUI toast = Text(root, "Toast", "Toast", 72f, Color.white, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(1500f, 240f), titleFont);
            toast.gameObject.SetActive(false);
            ui.toastText = toast;

            RectTransform hint = Panel(root, "Hint", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(1180f, 96f));
            var hintGroup = hint.gameObject.AddComponent<CanvasGroup>();
            hintGroup.alpha = 0f;
            hintGroup.blocksRaycasts = false;
            hintGroup.interactable = false;
            ui.hintGroup = hintGroup;
            ui.hintText = Text(hint, "Text", "Hint", 34f, Color.white, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1140f, 90f), hudFont);
        }

        /// <summary>The scoreboard of a race: the place of the local runner and a row per runner, under the coins and the score.</summary>
        private static RaceHud BuildRaceHud(Transform root)
        {
            const int runners = 4;
            const float header = 62f;
            const float rowHeight = 58f;
            RectTransform panel = Panel(root, "Race", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -190f), new Vector2(390f, header + runners * rowHeight + 10f));
            panel.GetComponent<Image>().raycastTarget = false;
            var hud = panel.gameObject.AddComponent<RaceHud>();
            hud.panel = panel;
            hud.headerHeight = header;
            hud.rowHeight = rowHeight;
            Text(panel, "Header", "RACE", 26f, Gold, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -14f), new Vector2(160f, 38f), hudFont);
            hud.placeText = Text(panel, "Place", "1st", 46f, Color.white, TextAlignmentOptions.Right, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-22f, -4f), new Vector2(200f, 56f), titleFont);
            var rows = new List<RaceHud.Row>();
            for (int i = 0; i < runners; i++)
            {
                RectTransform row = Rect(panel, $"Runner{i + 1}", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -header - i * rowHeight), new Vector2(374f, rowHeight - 4f));
                Image highlight = Image(row, "Highlight", panelSprite, new Color(1f, 1f, 1f, 0.14f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(374f, rowHeight - 4f), true);
                highlight.raycastTarget = false;
                highlight.enabled = false;
                Image chip = Image(row, "Chip", buttonSprite, Color.white, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(44f, 44f), true);
                chip.raycastTarget = false;
                TextMeshProUGUI place = Text(chip.transform, "Place", (i + 1).ToString(), 30f, Color.white, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), new Vector2(44f, 44f), hudFont);
                TextMeshProUGUI name = Text(row, "Name", "Runner", 23f, Color.white, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(62f, -2f), new Vector2(208f, 30f), hudFont);
                name.textWrappingMode = TextWrappingModes.NoWrap;
                name.enableAutoSizing = true;
                name.fontSizeMin = 13f;
                name.fontSizeMax = 23f;
                TextMeshProUGUI score = Text(row, "Score", "0", 26f, Color.white, TextAlignmentOptions.Right, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-10f, -1f), new Vector2(100f, 30f), hudFont);
                TextMeshProUGUI detail = Text(row, "Detail", "0 m   0 coins", 18f, Soft, TextAlignmentOptions.Left, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(62f, 1f), new Vector2(300f, 24f), null, false);
                detail.textWrappingMode = TextWrappingModes.NoWrap;
                rows.Add(new RaceHud.Row { root = row.gameObject, highlight = highlight, chip = chip, place = place, runnerName = name, score = score, detail = detail });
            }
            hud.rows = rows.ToArray();
            panel.gameObject.SetActive(false);
            return hud;
        }

        private static void BuildResults(Transform canvas, RunnerUI ui)
        {
            CanvasGroup screen = Screen(canvas, "Results");
            Transform root = screen.transform;
            ui.resultsScreen = screen;
            Image dim = Image(root, "Dim", null, new Color(0f, 0f, 0.04f, 0.5f), Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            StretchFull(dim.rectTransform);
            root = UIBuildUtils.CreateSafeArea(root);

            RectTransform panel = Panel(root, "Panel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 900f));
            ui.resultTitle = Text(panel, "Title", "LEVEL COMPLETE!", 80f, Gold, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(860f, 104f), titleFont);
            ui.resultTitle.textWrappingMode = TextWrappingModes.NoWrap;
            ui.resultTitle.enableAutoSizing = true;
            ui.resultTitle.fontSizeMin = 40f;
            ui.resultTitle.fontSizeMax = 80f;
            ui.resultSubtitle = Text(panel, "Subtitle", "Level", 34f, Soft, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -128f), new Vector2(800f, 50f), hudFont);
            RectTransform stars = Rect(panel, "Stars", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -178f), new Vector2(620f, 190f));
            var starImages = new List<Image>();
            foreach ((float x, float y, float size) in new[] { (-200f, -20f, 150f), (0f, 10f, 180f), (200f, -20f, 150f) })
            {
                starImages.Add(Image(stars, $"Star{starImages.Count + 1}", RunnerArtBuilder.Icon("StarEmpty"), Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(size, size)));
            }
            ui.resultStars = starImages.ToArray();
            ui.resultStarsRoot = stars;
            ui.resultStats = Text(panel, "Stats", "Stats", 36f, Color.white, TextAlignmentOptions.Top, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -382f), new Vector2(780f, 150f), hudFont);
            ui.resultGoals = Text(panel, "Goals", "Goals", 28f, Soft, TextAlignmentOptions.Top, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -548f), new Vector2(800f, 140f), null, false);
            RectTransform buttons = Rect(panel, "Buttons", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 36f), new Vector2(820f, 100f));
            var row = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 22f;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = false;
            row.childControlHeight = false;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            ui.levelsButton = Button(buttons, "Levels", "Levels", RunnerArtBuilder.Icon("Levels"), Blue, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(250f, 100f), 36f, out TextMeshProUGUI levelsLabel);
            ui.levelsLabel = levelsLabel;
            ui.retryButton = Button(buttons, "Retry", "Retry", RunnerArtBuilder.Icon("Retry"), Orange, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(250f, 100f), 36f, out _);
            ui.nextButton = Button(buttons, "Next", "Next", RunnerArtBuilder.Icon("Play"), Green, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(250f, 100f), 36f, out _);
        }

        // ------------------------------------------------------------------ widgets

        private static void PrepareFonts()
        {
            TMP_FontAsset font = TMP_Settings.defaultFontAsset;
            titleFont = RunnerAssets.SaveMaterial("Art/Materials/FontTitle.mat", font.material.shader, m =>
            {
                m.CopyPropertiesFromMaterial(font.material);
                m.EnableKeyword("OUTLINE_ON");
                m.EnableKeyword("UNDERLAY_ON");
                m.SetFloat("_FaceDilate", 0.15f);
                m.SetFloat("_OutlineWidth", 0.26f);
                m.SetColor("_OutlineColor", new Color(0.1f, 0.07f, 0.2f, 1f));
                m.SetColor("_UnderlayColor", new Color(0f, 0f, 0.05f, 0.65f));
                m.SetFloat("_UnderlayOffsetX", 0.5f);
                m.SetFloat("_UnderlayOffsetY", -0.7f);
                m.SetFloat("_UnderlayDilate", 0.3f);
                m.SetFloat("_UnderlaySoftness", 0.25f);
            });
            hudFont = RunnerAssets.SaveMaterial("Art/Materials/FontHud.mat", font.material.shader, m =>
            {
                m.CopyPropertiesFromMaterial(font.material);
                m.EnableKeyword("OUTLINE_ON");
                m.DisableKeyword("UNDERLAY_ON");
                m.SetFloat("_FaceDilate", 0.08f);
                m.SetFloat("_OutlineWidth", 0.18f);
                m.SetColor("_OutlineColor", new Color(0.05f, 0.05f, 0.12f, 1f));
            });
        }

        private static CanvasGroup Screen(Transform parent, string name)
        {
            RectTransform rect = Rect(parent, name, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            StretchFull(rect);
            return rect.gameObject.AddComponent<CanvasGroup>();
        }

        private static RectTransform Rect(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Image Image(Transform parent, string name, Sprite sprite, Color color, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size, bool sliced = false)
        {
            RectTransform rect = Rect(parent, name, anchor, pivot, position, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.type = sliced ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            image.preserveAspect = !sliced && sprite != null;
            return image;
        }

        private static RectTransform Panel(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size, Color? color = null)
        {
            Image image = Image(parent, name, panelSprite, color ?? PanelColor, anchor, pivot, position, size, true);
            return image.rectTransform;
        }

        private static TextMeshProUGUI Text(Transform parent, string name, string text, float size, Color color, TextAlignmentOptions alignment,
            Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 box, Material material, bool bold = true)
        {
            RectTransform rect = Rect(parent, name, anchor, pivot, position, box);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Overflow;
            if (material != null)
            {
                label.fontSharedMaterial = material;
            }
            return label;
        }

        private static void Gradient(TextMeshProUGUI text, Color top, Color bottom)
        {
            text.enableVertexGradient = true;
            text.colorGradient = new VertexGradient(top, top, bottom, bottom);
        }

        private static Image[] Stars(Transform parent, string name, Vector2 anchor, Vector2 position, float size, float spacing, bool centered)
        {
            RectTransform row = Rect(parent, name, anchor, new Vector2(0.5f, 0.5f), position + new Vector2(centered ? 0f : spacing, 0f), new Vector2(spacing * 3f, size));
            var stars = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                stars[i] = Image(row, $"Star{i + 1}", RunnerArtBuilder.Icon("StarEmpty"), Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2((i - 1) * spacing, 0f), new Vector2(size, size));
                stars[i].raycastTarget = false;
            }
            return stars;
        }

        private static Button Button(Transform parent, string name, string label, Sprite icon, Color color, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size,
            float fontSize, out TextMeshProUGUI text)
        {
            Image background = Image(parent, name, buttonSprite, color, anchor, pivot, position, size, true);
            var button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.colors = ButtonColors();
            float iconSize = size.y * 0.52f;
            float labelOffset = 0f;
            if (icon != null)
            {
                Image image = Image(background.transform, "Icon", icon, Color.white, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(size.y * 0.55f, 2f), new Vector2(iconSize, iconSize));
                image.raycastTarget = false;
                labelOffset = size.y * 0.35f;
            }
            text = Text(background.transform, "Label", label, fontSize, Color.white, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(labelOffset, 3f), new Vector2(size.x - labelOffset * 2f, size.y), hudFont);
            return button;
        }

        private static Button TextButton(Transform parent, string name, string label, Vector2 anchor, Vector2 position, Vector2 size, float fontSize, out TextMeshProUGUI text)
        {
            RectTransform rect = Rect(parent, name, anchor, new Vector2(0.5f, 0f), position, size);
            var hit = rect.gameObject.AddComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0f);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = hit;
            text = Text(rect, "Label", label, fontSize, new Color(0.75f, 0.78f, 0.9f), TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size, null);
            text.fontStyle = FontStyles.Underline;
            return button;
        }

        private static ColorBlock ButtonColors()
        {
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
            colors.selectedColor = Color.white;
            colors.pressedColor = new Color(0.78f, 0.78f, 0.82f, 1f);
            colors.disabledColor = new Color(0.55f, 0.55f, 0.6f, 0.75f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            return colors;
        }
    }
}
