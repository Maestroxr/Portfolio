using System;
using System.Linq;
using Gamebox;
using Gamebox.Launcher;
using Gamebox.Editor;
using Portfolio.Heroes.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Portfolio.Heroes.EditorTools
{
    /// <summary>
    /// Builds the scene the game is played in. There is very little in it: a light, a camera on its rig, the manager
    /// with its controllers, the sound and the menu of the shared launcher. The map itself, and the whole interface,
    /// are made when a scenario starts, because both depend on what the scenario turns out to be.
    /// </summary>
    internal static class HeroesSceneBuilder
    {
        private const string ScenePath = "Scenes/Heroes.unity";
        private const string Database = "skinnerboxes-heroes";

        [MenuItem("Heroes/Rebuild Scene", false, 22)]
        public static void Build()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Lighting();
            Camera camera = MakeCamera(out CameraRig rig);

            var managerObject = new GameObject("Heroes");
            var storage = managerObject.AddComponent<Storage>();
            HeroesAssets.Set(storage, "DefaultStorageStrategy", p => p.enumValueIndex = (int)StorageStrategies.PlayerPrefs);
            var manager = managerObject.AddComponent<HeroesGameManager>();
            var audio = managerObject.AddComponent<HeroesAudio>();

            var controllerObject = new GameObject("Controller");
            var controller = controllerObject.AddComponent<HeroesController>();

            var uiObject = new GameObject("Interface");
            var ui = uiObject.AddComponent<HeroesUI>();

            HeroesArt art = HeroesAssets.Load<HeroesArt>("Art/HeroesArt.asset");
            HeroesAssets.SetObject(audio, "art", art);
            HeroesAssets.SetObject(audio, "settings", HeroesContentBuilder.DefaultSettings);

            HeroesAssets.SetObject(manager, "settings", HeroesContentBuilder.DefaultSettings);
            HeroesAssets.SetObject(manager, "controller", controller);
            HeroesAssets.SetObject(manager, "ui", ui);
            HeroesAssets.SetObject(manager, "campaign", HeroesContentBuilder.Campaign);
            HeroesAssets.SetObject(manager, "art", art);
            HeroesAssets.SetObject(manager, "cameraRig", rig);
            HeroesAssets.SetObject(manager, "sound", audio);
            HeroesAssets.SetObject(manager, "StorageBehaviour", storage);
            HeroesAssets.Set(manager, "GameIdentifier", p => p.intValue = (int)GameType.Heroes);
            HeroesAssets.SetObject(controller, "UI", ui);
            HeroesAssets.SetObject(controller, "BaseManager", manager);
            HeroesAssets.SetObject(rig, "view", camera);

            // The shared menu of the launcher is the pause menu and the settings panel, in the look of the game; the
            // title screen is the game's own (HeroesUI), and the map's Menu command pauses (no pause button of the menu).
            GameObject menu = GameMenuInstaller.InstallMenu(scene, 100, false);
            var scaler = menu.GetComponent<CanvasScaler>();
            GameMenuInstaller.WireGameUI(ui, menu, controller, manager, scaler, false);
            GameMenuInstaller.WireSettingsPanel(menu, typeof(HeroesSettingsUI), ui, HeroesContentBuilder.DefaultSettings);
            Button titleButton = GameMenuInstaller.AddMenuButton(menu, TitleButtonName, "Leave to the Title");
            GameMenuInstaller.MenuStyle style = MenuStyle(art);
            GameMenuInstaller.Restyle(menu, style);
            HeroesAssets.SetObject(ui, "titleButton", titleButton);
            SettingsPanel(menu, style);

            // Online play needs the bindings of the server generated into Scripts/Server; without them the scene is
            // built for a game at one device and the lobby is left out.
            Type client = Type.GetType("Portfolio.Heroes.Server.GameServerClient, Skinnerboxes.Heroes", false);
            if (client != null)
            {
                Component online = OnlineInstaller.Install(scene, client, typeof(HeroesOnlineController),
                    manager, ui, Database, LobbyStyle(art));
                HeroesAssets.SetObject(manager, "online", online);
            }
            else
            {
                Debug.LogWarning("Heroes: the server bindings are missing, so the scene has no lobby.");
            }

            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }

            GameMenuInstaller.EnsureUrpCameras();
            EnsureTerrainShaders();
            string path = HeroesAssets.Path(ScenePath);
            HeroesAssets.EnsureFolderOf(ScenePath);
            EditorSceneManager.SaveScene(scene, path);
            BattleScene(art);
            Definition(path, art);
            GameSceneBuildSettings.Sync(true);
            Debug.Log($"Heroes: scene built at {path}.");
        }


        /// <summary>
        /// The card of the game in the launcher: what it is called, what it is, and the scene that starts it. It also
        /// puts the scene into the build, which the launcher's list is built from.
        /// </summary>
        private static void Definition(string scenePath, HeroesArt art)
        {
            const string relative = "Resources/Games/Heroes.asset";
            var definition = HeroesAssets.Load<GameDefinition>(relative);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<GameDefinition>();
                HeroesAssets.EnsureFolderOf(relative);
                AssetDatabase.CreateAsset(definition, HeroesAssets.Path(relative));
            }
            HeroesAssets.Set(definition, "type", p => p.enumValueIndex = (int)GameType.Heroes);
            HeroesAssets.SetString(definition, "displayName", "Heroes");
            HeroesAssets.SetString(definition, "description",
                "A turn based strategy of heroes, towns and armies, fought on a hexagonal map.");
            HeroesAssets.SetInt(definition, "sortOrder", 50);
            HeroesAssets.SetObject(definition, "icon", art != null ? art.Icon("kingdom") : null);
            HeroesAssets.SetObject(definition, "scene", AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath));
            HeroesAssets.SetString(definition, "scenePath", scenePath);
            definition.ExtraScenes = ExtraScenes();
            EditorUtility.SetDirty(definition);
            AssetDatabase.SaveAssets();
        }

        // ------------------------------------------------------------------ the battlefield scene

        private const string BattleScenePath = "Scenes/HeroesBattle.unity";
        private const string BattleAssets = "Scenes/HeroesBattle";
        private const string WallModel = "KayKit/Buildings/Neutral/wall_straight";
        private const string GateModel = "KayKit/Buildings/Neutral/wall_straight_gate";
        private const string BridgeModel = "KayKit/Props/pallet";
        private const string StoneModel = "KayKit/Props/resource_stone";
        /// <summary>How long the gatehouse is along the wall: the gate's row (1.8 m) and a little of the wall either side.</summary>
        private const float GateLength = 2.8f;
        /// <summary>How far the middle of the gatehouse stands out before the middle of the wall, toward the besiegers.</summary>
        private const float GateJut = 0.4f;
        /// <summary>The grey of the wall's stone, for the dungeon's rubble and the loose blocks in a heap of it.</summary>
        private static readonly Color RuinStone = new Color(0.62f, 0.64f, 0.68f);
        private static readonly Color LooseStone = new Color(0.6f, 0.61f, 0.65f);
        /// <summary>The old oak of the drawbridge, darker than the pallet it is made from.</summary>
        private static readonly Color DarkWood = new Color(0.5f, 0.38f, 0.3f);

        /// <summary>
        /// Builds the battlefield scene by itself, next to the scene that is open, and lists it in the game's definition
        /// and the build after the game's own scene (Rebuild Scene does all of this too).
        /// </summary>
        [MenuItem("Heroes/Rebuild Battle Scene", false, 23)]
        public static void BuildBattle()
        {
            HeroesArt art = HeroesAssets.Load<HeroesArt>("Art/HeroesArt.asset");
            string path = BattleScene(art);
            if (path == null)
            {
                return;
            }
            var definition = HeroesAssets.Load<GameDefinition>("Resources/Games/Heroes.asset");
            if (definition != null)
            {
                definition.ExtraScenes = ExtraScenes();
                EditorUtility.SetDirty(definition);
            }
            AssetDatabase.SaveAssets();
            GameSceneBuildSettings.Sync(true);
            Debug.Log($"Heroes: battle scene built at {path}.");
        }

        /// <summary>The scenes the game loads by name while it runs, for its definition: the battlefield.</summary>
        private static SceneAsset[] ExtraScenes()
        {
            var battle = AssetDatabase.LoadAssetAtPath<SceneAsset>(HeroesAssets.Path(BattleScenePath));
            return battle != null ? new[] { battle } : new SceneAsset[0];
        }

        /// <summary>
        /// The scene a battle on a battlefield of its own is shown in (HeroesBattle, loaded by name next to the game's
        /// scene while the battle lasts): the <see cref="BattlefieldScene"/> that lays the field out from the battle, a
        /// camera of its own (not the main camera, and without a listener: the game's camera goes on hearing
        /// everything), a sun, a post processing volume with a light grading of its own, and the sky, light and fog of a
        /// battle on grass, which the field changes to those of the land each battle is fought on. There is no event
        /// system in it: the interface uses the one of the game's scene. Returns the scene's path, or null when it could
        /// not be built.
        /// </summary>
        private static string BattleScene(HeroesArt art)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path))
                {
                    // Unity refuses to add a scene next to an untitled one.
                    Debug.LogError("Heroes: save the open scene first; the battle scene is built next to it.");
                    return null;
                }
            }
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            string path = HeroesAssets.Path(BattleScenePath);
            try
            {
                // The lighting of a scene is the one set while it is the active scene.
                SceneManager.SetActiveScene(scene);
                var root = new GameObject("Battlefield");
                var field = root.AddComponent<BattlefieldScene>();

                var cameraObject = new GameObject("Battle Camera", typeof(Camera));
                cameraObject.transform.SetParent(root.transform, false);
                var camera = cameraObject.GetComponent<Camera>();
                BattlefieldScene.Configure(camera);
                // Somewhere to look from in the editor; the field frames itself once a battle is laid out on it.
                cameraObject.transform.SetPositionAndRotation(new Vector3(16f, 30f, -12f), Quaternion.Euler(54f, 0f, 0f));

                var sunObject = new GameObject("Battle Sun");
                sunObject.transform.SetParent(root.transform, false);
                var sun = sunObject.AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.shadows = LightShadows.Soft;
                sun.shadowStrength = 0.75f;

                var gradingObject = new GameObject("Battle Grading");
                gradingObject.transform.SetParent(root.transform, false);
                var volume = gradingObject.AddComponent<Volume>();
                volume.isGlobal = true;
                volume.priority = 1f;
                volume.sharedProfile = Grading();

                BattleLighting(art, sun, camera);
                HeroesAssets.SetObject(field, "art", art);
                HeroesAssets.SetObject(field, "view", camera);
                HeroesAssets.SetObject(field, "sun", sun);
                HeroesAssets.SetObject(field, "volume", volume);
                HeroesAssets.SetObject(field, "gate", Gate());
                HeroesAssets.SetObject(field, "ruin", Ruin());
                HeroesAssets.SetObject(field, "stones", Stones());
                HeroesAssets.SetObjects(field, "banners", Banners());

                HeroesAssets.EnsureFolderOf(BattleScenePath);
                EditorSceneManager.SaveScene(scene, path);
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded)
                {
                    SceneManager.SetActiveScene(previous);
                }
                EditorSceneManager.CloseScene(scene, true);
            }
            return path;
        }

        /// <summary>The sky, the light and the fog of a battle on grass, from the art of that land.</summary>
        private static void BattleLighting(HeroesArt art, Light sun, Camera camera)
        {
            HeroesArt.SkyArt sky = art != null ? art.Sky(TerrainType.Grass) : null;
            sun.color = sky != null ? sky.sun : new Color(1f, 0.96f, 0.88f);
            sun.intensity = sky != null ? sky.sunIntensity : 1.4f;
            Vector2 angles = sky != null ? sky.sunAngles : new Vector2(46f, 138f);
            sun.transform.rotation = Quaternion.Euler(angles.x, angles.y, 0f);
            RenderSettings.sun = sun;
            RenderSettings.skybox = sky != null ? sky.material : null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = sky != null ? sky.ambientSky : new Color(0.48f, 0.55f, 0.66f);
            RenderSettings.ambientEquatorColor = sky != null ? sky.ambientEquator : new Color(0.40f, 0.42f, 0.40f);
            RenderSettings.ambientGroundColor = sky != null ? sky.ambientGround : new Color(0.24f, 0.21f, 0.17f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = sky != null ? sky.fog : new Color(0.62f, 0.68f, 0.75f);
            RenderSettings.fogStartDistance = 75f;
            RenderSettings.fogEndDistance = 290f;
            camera.backgroundColor = RenderSettings.fogColor;
        }

        /// <summary>
        /// The grading of the battlefield, an asset next to its scene with the settings of
        /// <see cref="BattlefieldScene.Grade"/>: a touch of bloom, a little more contrast and color, darker corners.
        /// </summary>
        private static VolumeProfile Grading()
        {
            const string relative = BattleAssets + "/Grading.asset";
            var profile = HeroesAssets.Load<VolumeProfile>(relative);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                HeroesAssets.EnsureFolderOf(relative);
                AssetDatabase.CreateAsset(profile, HeroesAssets.Path(relative));
            }
            BattlefieldScene.Grade(profile);
            foreach (VolumeComponent component in profile.components)
            {
                if (!AssetDatabase.Contains(component))
                {
                    // The overrides live in the profile's file, as the profile editor keeps them.
                    component.name = component.GetType().Name;
                    component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                    AssetDatabase.AddObjectToAsset(component, profile);
                }
                EditorUtility.SetDirty(component);
            }
            EditorUtility.SetDirty(profile);
            return profile;
        }

        /// <summary>
        /// The gatehouse in the wall of a besieged town, at the size it stands on the field (the scene stands it as it
        /// is): the gateway of the wall's own set, stretched a little longer than the gate's row of the wall (the walls
        /// on either side run into it), taller than the wall and jutting out well before it on the besiegers' side (+z),
        /// so it stands out of the line seen from above, doors shut; and before it the drawbridge down on the ground. It
        /// runs along x, centered on its origin (the middle of the wall).
        /// </summary>
        private static GameObject Gate()
        {
            GameObject wall = HeroesArtBuilder.Model(WallModel);
            float wallLength = wall != null ? Mathf.Max(0.01f, HeroesArtBuilder.BoundsOf(wall).size.x) : 2f;
            // The scale the lengths of wall of the field have: one across a cell.
            float scale = (HexLayout.DefaultRadius * 1.7320508f + 0.1f) / wallLength;
            GameObject root = HeroesArtBuilder.Root("Gate");
            GameObject gateway = HeroesArtBuilder.Piece(root.transform, GateModel, Vector3.zero, 0f, scale);
            float depth = 1f;
            if (gateway != null)
            {
                Bounds made = HeroesArtBuilder.BoundsOf(gateway);
                float along = GateLength / Mathf.Max(0.01f, made.size.x);
                gateway.transform.localScale = Vector3.Scale(gateway.transform.localScale,
                    new Vector3(along, BattlefieldScene.WallHeight * 1.2f, BattlefieldScene.WallThickness * 1.9f));
                Bounds sized = HeroesArtBuilder.BoundsOf(gateway);
                gateway.transform.localPosition -= new Vector3(sized.center.x, sized.min.y, sized.center.z - GateJut);
                depth = sized.size.z;
            }
            GameObject bridge = HeroesArtBuilder.Piece(root.transform, BridgeModel, Vector3.zero, 0f, 1f, 0f, DarkWood);
            if (bridge != null)
            {
                Bounds plank = HeroesArtBuilder.BoundsOf(bridge);
                // A little wider than the gateway's opening, reaching out across the gate's hexagon, and thin.
                bridge.transform.localScale = Vector3.Scale(bridge.transform.localScale,
                    new Vector3(1.8f / plank.size.x, 0.14f / plank.size.y, 1.9f / plank.size.z));
                Bounds sized = HeroesArtBuilder.BoundsOf(bridge);
                bridge.transform.localPosition += new Vector3(-sized.center.x, 0.02f - sized.min.y, GateJut + depth * 0.5f + 0.9f - sized.center.z);
                bridge.name = "Drawbridge";
            }
            return HeroesArtBuilder.Save(root, BattleAssets + "/Gate.prefab");
        }

        /// <summary>
        /// A heap of the wall's stones, about a cell across and knee high, standing on the ground at its origin: where an
        /// arrow tower fell, and at the broken ends of the wall beside a breach (smaller there).
        /// </summary>
        private static GameObject Ruin()
        {
            GameObject root = HeroesArtBuilder.Root("Ruin");
            HeroesArtBuilder.Piece(root.transform, "KayKit/Dungeon/rubble_large", new Vector3(0.1f, 0f, 0.05f), 20f, 1f, 0.75f, RuinStone);
            HeroesArtBuilder.Piece(root.transform, "KayKit/Dungeon/rubble_half", new Vector3(-0.35f, 0f, -0.25f), 140f, 1f, 0.6f, RuinStone);
            // A broken length of the wall itself, tipped over and half buried.
            GameObject slab = HeroesArtBuilder.Piece(root.transform, WallModel, new Vector3(0.25f, -0.2f, 0.4f), 0f, 0.42f);
            if (slab != null)
            {
                slab.transform.localRotation = Quaternion.Euler(28f, 65f, 12f);
            }
            float[,] blocks = { { 0.7f, -0.35f, 30f, 1.7f }, { -0.75f, 0.3f, 75f, 1.5f }, { 0.05f, -0.8f, 10f, 1.3f }, { -0.2f, 0.75f, 115f, 1.6f }, { 0.85f, 0.45f, 160f, 1.2f } };
            for (int i = 0; i < blocks.GetLength(0); i++)
            {
                GameObject block = HeroesArtBuilder.Piece(root.transform, StoneModel, new Vector3(blocks[i, 0], 0f, blocks[i, 1]), blocks[i, 2], blocks[i, 3], 0f, LooseStone);
                if (block != null && i % 2 == 1)
                {
                    block.transform.localRotation = Quaternion.Euler(0f, blocks[i, 2], 18f);
                }
            }
            return HeroesArtBuilder.Save(root, BattleAssets + "/Ruin.prefab");
        }

        /// <summary>A few loose stones of the wall lying on the ground, about a pace across, for the ground of a breach.</summary>
        private static GameObject Stones()
        {
            GameObject root = HeroesArtBuilder.Root("Stones");
            HeroesArtBuilder.Piece(root.transform, StoneModel, new Vector3(0.2f, 0f, -0.15f), 25f, 1.2f, 0f, LooseStone);
            GameObject tipped = HeroesArtBuilder.Piece(root.transform, StoneModel, new Vector3(-0.3f, 0f, 0.2f), 80f, 1f, 0f, LooseStone);
            if (tipped != null)
            {
                tipped.transform.localRotation = Quaternion.Euler(0f, 80f, 14f);
            }
            HeroesArtBuilder.Piece(root.transform, StoneModel, new Vector3(0.05f, 0f, 0.4f), 140f, 0.8f, 0f, LooseStone);
            return HeroesArtBuilder.Save(root, BattleAssets + "/Stones.prefab");
        }

        /// <summary>
        /// The banners a town's gatehouse hangs out, by the color of its owner (red, blue, green, yellow, and white for
        /// nobody): the dungeon's banners on their brackets at not quite half their size, standing on their lower edge, the cloth
        /// across x.
        /// </summary>
        private static GameObject[] Banners()
        {
            string[] colors = { "red", "blue", "green", "yellow", "white" };
            var banners = new GameObject[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                GameObject root = HeroesArtBuilder.Root($"Banner{i}");
                HeroesArtBuilder.Piece(root.transform, $"KayKit/Dungeon/banner_{colors[i]}", Vector3.zero, 0f, 0.42f);
                banners[i] = HeroesArtBuilder.Save(root, $"{BattleAssets}/Banner{i}.prefab");
            }
            return banners;
        }

        /// <summary>
        /// Puts the shaders the terrain engine looks up by name into the build; nothing references them from an
        /// asset. The details shader of the pipeline, and the built in billboard shaders of the trees (the engine asks
        /// for one as Nature/Terrain/BillboardTree whenever a terrain has trees, the camera facing one at the quality
        /// levels whose billboards face the camera, and a development build shows an error on screen without it).
        /// </summary>
        private static void EnsureTerrainShaders()
        {
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            SerializedProperty shaders = settings.FindProperty("m_AlwaysIncludedShaders");
            if (shaders == null)
            {
                return;
            }
            string[] names =
            {
                "Hidden/TerrainEngine/Details/UniversalPipeline/Vertexlit", "Hidden/TerrainEngine/BillboardTree",
                "Hidden/TerrainEngine/CameraFacingBillboardTree"
            };
            foreach (string name in names)
            {
                // Built in shaders are not found by name until something references them, so they are loaded
                // from Unity's own resources.
                Shader shader = Shader.Find(name)
                    ?? AssetDatabase.GetBuiltinExtraResource<Shader>(name + ".shader")
                    ?? AssetDatabase.LoadAllAssetsAtPath("Resources/unity_builtin_extra")
                        .OfType<Shader>().FirstOrDefault(candidate => candidate.name == name);
                if (shader == null)
                {
                    Debug.LogWarning($"Heroes: the shader {name} was not found, so it cannot be put into the build.");
                    continue;
                }
                bool present = false;
                for (int i = 0; i < shaders.arraySize && !present; i++)
                {
                    present = shaders.GetArrayElementAtIndex(i).objectReferenceValue == shader;
                }
                if (present)
                {
                    continue;
                }
                shaders.InsertArrayElementAtIndex(shaders.arraySize);
                shaders.GetArrayElementAtIndex(shaders.arraySize - 1).objectReferenceValue = shader;
            }
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------------------ the world of the scene

        private static void Lighting()
        {
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.96f, 0.88f);
            sun.intensity = 1.45f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.72f;
            sun.transform.rotation = Quaternion.Euler(46f, 138f, 0f);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.48f, 0.55f, 0.66f);
            RenderSettings.ambientEquatorColor = new Color(0.40f, 0.42f, 0.40f);
            RenderSettings.ambientGroundColor = new Color(0.24f, 0.21f, 0.17f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.62f, 0.68f, 0.75f);
            RenderSettings.fogStartDistance = 90f;
            RenderSettings.fogEndDistance = 260f;
        }

        private static Camera MakeCamera(out CameraRig rig)
        {
            var rigObject = new GameObject("CameraRig");
            rig = rigObject.AddComponent<CameraRig>();
            var cameraObject = new GameObject("Camera", typeof(Camera));
            cameraObject.transform.SetParent(rigObject.transform, false);
            var camera = cameraObject.GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.backgroundColor = new Color(0.16f, 0.22f, 0.3f);
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 600f;
            camera.fieldOfView = 42f;
            cameraObject.AddComponent<AudioListener>();
            return camera;
        }

        // ------------------------------------------------------------------ the menus

        private const string TitleButtonName = "ExitToTitle";

        /// <summary>
        /// The look of the pause menu and the settings panel: a framed window of leather with its title on the crimson
        /// ribbon, the stone buttons, the fonts of the game, and rows of settings on cards in two columns.
        /// </summary>
        private static GameMenuInstaller.MenuStyle MenuStyle(HeroesArt art)
        {
            var style = new GameMenuInstaller.MenuStyle
            {
                ReferenceResolution = new Vector2(1920f, 1080f),
                ReferencePixelsPerUnit = 100f,
                Header = "Paused",
                SettingsHeader = "Settings",
                Hidden = new[] { "StartNewGame", "Score", "Level", "Timer", "PauseButton" },
                Order = new[] { "ReturnToGame", "SaveGame", "LoadGame", "Game Settings", TitleButtonName, "ExitGame" },
                HeaderFontSize = 32f,
                HeaderColor = Color.white,
                HeaderGradient = new TMPro.VertexGradient(new Color(1f, 0.93f, 0.7f), new Color(1f, 0.93f, 0.7f),
                    new Color(0.86f, 0.66f, 0.3f), new Color(0.86f, 0.66f, 0.3f)),
                TextColor = UIKit.Ink,
                ValueColor = UIKit.Gold,
                ErrorColor = UIKit.Bad,
                ButtonTextColor = UIKit.Ink,
                BackdropColor = new Color(0.03f, 0.02f, 0.01f, 0.72f),
                WindowColor = Color.white,
                TileWindow = true,
                WindowPadding = new RectOffset(46, 46, 78, 44),
                HeaderBannerSize = new Vector2(440f, 84f),
                HeaderBannerPadding = new Vector4(56f, 12f, 56f, 26f),
                HeaderBannerLift = -12f,
                ButtonSize = new Vector2(420f, 62f),
                ButtonSpacing = 12f,
                ButtonFontSize = 27f,
                SettingsSize = new Vector2(1200f, 606f),
                SettingsButtonSize = new Vector2(270f, 56f),
                SettingsButtonMargin = new Vector2(40f, 30f),
                RowsMargin = new RectOffset(40, 40, 106, 104),
                RowColumns = 2,
                RowSpacing = 8f,
                ColumnSpacing = 18f,
                ScrollRows = true,
                RowHeight = 56f,
                RowFontSize = 22f,
                TrackHeight = 18f,
                FillColor = UIKit.Gold,
                KnobSize = new Vector2(34f, 34f),
                CheckSize = new Vector2(38f, 38f),
                ArrowGlyph = "▲",
                ArrowRotation = -90f,
                Labels =
                {
                    ["ReturnToGame"] = "Return to the Game",
                    ["SaveGame"] = "Save the Game",
                    ["LoadGame"] = "Load the Game",
                    ["Game Settings"] = "Settings",
                    ["ExitGame"] = "Exit",
                    ["SaveSettingsButton"] = "Save Settings",
                    ["LoadSettingsButton"] = "Load Settings",
                    ["BackToMenu"] = "Back"
                }
            };
            if (art == null)
            {
                return style;
            }
            style.TitleFont = art.titleFont;
            style.TitleMaterial = art.TextMaterial(art.titleFont, TextLook.Gold);
            style.BodyFont = art.bodyFont;
            style.BodyMaterial = art.TextMaterial(art.bodyFont, TextLook.Shadow);
            style.Window = art.frame;
            style.HeaderBanner = art.ribbon;
            style.Button = art.button;
            style.ButtonHover = art.buttonHover;
            style.ButtonPressed = art.buttonPressed;
            style.ButtonDisabled = art.buttonDisabled;
            style.RowBackground = art.card;
            style.Track = art.barFrame;
            style.Fill = art.barFill;
            style.Knob = art.knob;
            style.CheckBox = art.checkBox;
            style.CheckMark = art.checkMark;
            style.ArrowButton = art.round;
            return style;
        }

        /// <summary>
        /// Fills the settings panel with the rows of the game, in two columns read across: where battles are fought and
        /// the speeds, the rules of a skirmish and the sound, and three switches. Every value is written beside its row.
        /// </summary>
        private static void SettingsPanel(GameObject menu, GameMenuInstaller.MenuStyle style)
        {
            var settings = menu.GetComponentInChildren<HeroesSettingsUI>(true);
            if (settings == null)
            {
                return;
            }
            HeroesSettings defaults = HeroesContentBuilder.DefaultSettings;
            string[] battles = { "On the map", "On a battlefield" };
            HeroesAssets.SetObject(settings, "battles",
                GameMenuInstaller.AddChoice(menu, "Battles", "Battles", battles, defaults != null ? defaults.battleStyle : 1, style));
            HeroesAssets.SetObject(settings, "heroSpeed", Slider(menu, "HeroSpeed", "Hero speed", 0.5f, 4f, 1.5f, false, style, "0.0x"));
            HeroesAssets.SetObject(settings, "mapSize", Slider(menu, "MapSize", "Map size", 0f, 2f, 1f, true, style, "0",
                new[] { "Small", "Medium", "Large" }));
            HeroesAssets.SetObject(settings, "battleSpeed", Slider(menu, "BattleSpeed", "Battle speed", 0.5f, 4f, 1.25f, false, style, "0.0x"));
            HeroesAssets.SetObject(settings, "treasure", Slider(menu, "Treasure", "Treasure", 1f, 3f, 2f, true, style, "0",
                new[] { "Scarce", "Normal", "Rich" }));
            HeroesAssets.SetObject(settings, "music", Slider(menu, "Music", "Music", 0f, 1f, 0.55f, false, style, null));
            HeroesAssets.SetObject(settings, "monsters", Slider(menu, "Monsters", "Wandering armies", 1f, 4f, 2f, true, style, "0",
                new[] { "Few", "Some", "Many", "Hordes" }));
            HeroesAssets.SetObject(settings, "effects", Slider(menu, "Sound", "Sound", 0f, 1f, 0.8f, false, style, null));
            HeroesAssets.SetObject(settings, "difficulty", Slider(menu, "Difficulty", "Difficulty", 0f, 2f, 1f, true, style, "0",
                new[] { "Easy", "Normal", "Hard" }));
            HeroesAssets.SetObject(settings, "edgeScroll", GameMenuInstaller.AddToggle(menu, "EdgeScroll", "Scroll at the edges", true, style));
            HeroesAssets.SetObject(settings, "enemyMoves", GameMenuInstaller.AddToggle(menu, "EnemyMoves", "Show enemy moves", true, style));
            HeroesAssets.SetObject(settings, "autoSave", GameMenuInstaller.AddToggle(menu, "AutoSave", "Save every day", true, style));
        }

        /// <summary>
        /// A slider row: its value as a number in <paramref name="format"/>, as a percentage when the format is null, or
        /// as one of <paramref name="names"/>.
        /// </summary>
        private static Slider Slider(GameObject menu, string name, string label, float min, float max, float value, bool whole,
            GameMenuInstaller.MenuStyle style, string format, string[] names = null)
        {
            Gamebox.UI.SliderValueLabel.Style shown = names != null ? Gamebox.UI.SliderValueLabel.Style.Names
                : format == null ? Gamebox.UI.SliderValueLabel.Style.Percent : Gamebox.UI.SliderValueLabel.Style.Number;
            return GameMenuInstaller.AddSlider(menu, name, label, min, max, value, whole, style, shown, format ?? "0", names);
        }

        /// <summary>
        /// The look of the online lobby: the leather window, card rows, stone buttons and sunken fields of the game, its
        /// fonts and the colours of its words. Its turn banner does not show in a game (HeroesUI switches it off): the bar
        /// along the top of the map and the battle bar show whose turn it is and the clock.
        /// </summary>
        private static OnlineInstaller.LobbyStyle LobbyStyle(HeroesArt art)
        {
            return new OnlineInstaller.LobbyStyle
            {
                Title = "PLAY ONLINE",
                Font = art != null ? art.titleFont : null,
                BodyFont = art != null ? art.bodyFont : null,
                Accent = HeroesUIArt.Gold,
                // The window, rows and plain buttons as the kit draws them; the gold and green buttons tinted a little.
                Window = Color.white,
                Row = Color.white,
                Text = UIKit.Ink,
                Muted = UIKit.Dim,
                Good = UIKit.Good,
                Warn = UIKit.Bad,
                Dim = new Color(0.02f, 0.01f, 0f, 0.78f),
                ButtonText = UIKit.Ink,
                WindowSprite = art != null ? art.frame : null,
                RowSprite = art != null ? art.card : null,
                ButtonSprite = art != null ? art.button : null,
                InputSprite = art != null ? art.inset : null,
                TileSprites = true,
                SpriteTint = 0.35f
            };
        }
    }
}
