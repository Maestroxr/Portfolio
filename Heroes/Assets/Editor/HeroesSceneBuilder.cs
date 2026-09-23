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
        private const string Database = "heroes";

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

            // The shared menu of the launcher: new game, settings, load, exit.
            GameObject menu = GameMenuInstaller.InstallMenu(scene, 100, false);
            var scaler = menu.GetComponent<CanvasScaler>();
            GameMenuInstaller.WireGameUI(ui, menu, controller, manager, scaler, true);
            GameMenuInstaller.WireSettingsPanel(menu, typeof(HeroesSettingsUI), ui, HeroesContentBuilder.DefaultSettings);
            foreach (string unused in new[] { "Score", "Level", "Timer" })
            {
                Transform label = GameMenuInstaller.FindChild(menu.transform, unused);
                if (label != null)
                {
                    label.gameObject.SetActive(false);
                }
            }
            SettingsPanel(menu);

            // Online play needs the bindings of the server generated into Scripts/Server; without them the scene is
            // built for a game at one device and the lobby is left out.
            Type client = Type.GetType("Portfolio.Heroes.Server.GameServerClient, Skinnerboxes.Heroes", false);
            if (client != null)
            {
                Component online = OnlineInstaller.Install(scene, client, typeof(HeroesOnlineController),
                    manager, ui, Database, new OnlineInstaller.LobbyStyle
                    {
                        Title = "PLAY ONLINE",
                        Font = art != null ? art.titleFont : null,
                        Accent = HeroesUIArt.Gold,
                        Window = HeroesUIArt.Leather,
                        Row = HeroesUIArt.Stone
                    });
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
            EditorUtility.SetDirty(definition);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Puts the shaders the terrain engine looks up by name into the build; nothing references them from an
        /// asset. The engine also asks for Nature/Terrain/BillboardTree, which this pipeline does not have: the trees
        /// of this game are near enough to be drawn as models, so its one line in a player's log is expected.
        /// </summary>
        private static void EnsureTerrainShaders()
        {
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            SerializedProperty shaders = settings.FindProperty("m_AlwaysIncludedShaders");
            if (shaders == null)
            {
                return;
            }
            foreach (string name in new[] { "Hidden/TerrainEngine/Details/UniversalPipeline/Vertexlit" })
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

        // ------------------------------------------------------------------ the settings panel

        /// <summary>Fills the shared settings panel with the rows this game has: sliders and a few switches.</summary>
        private static void SettingsPanel(GameObject menu)
        {
            Transform panel = GameMenuInstaller.PrepareInputPanel(menu);
            var settings = menu.GetComponentInChildren<HeroesSettingsUI>(true);
            if (panel == null || settings == null)
            {
                return;
            }
            HeroesAssets.SetObject(settings, "mapSize", Slider(panel, "Map size", 0f, 2f, 1f, true));
            HeroesAssets.SetObject(settings, "treasure", Slider(panel, "Treasure", 1f, 3f, 2f, true));
            HeroesAssets.SetObject(settings, "monsters", Slider(panel, "Wandering armies", 1f, 3f, 2f, true));
            HeroesAssets.SetObject(settings, "difficulty", Slider(panel, "Difficulty", 0f, 2f, 1f, true));
            HeroesAssets.SetObject(settings, "heroSpeed", Slider(panel, "Hero speed", 0.5f, 4f, 1.5f, false));
            HeroesAssets.SetObject(settings, "battleSpeed", Slider(panel, "Battle speed", 0.5f, 4f, 1.25f, false));
            HeroesAssets.SetObject(settings, "music", Slider(panel, "Music", 0f, 1f, 0.55f, false));
            HeroesAssets.SetObject(settings, "effects", Slider(panel, "Sound", 0f, 1f, 0.8f, false));
            HeroesAssets.SetObject(settings, "enemyMoves", Toggle(panel, "Show enemy moves", true));
            HeroesAssets.SetObject(settings, "edgeScroll", Toggle(panel, "Scroll at the edges", true));
            HeroesAssets.SetObject(settings, "autoSave", Toggle(panel, "Save every day", true));
        }

        private static Slider Slider(Transform panel, string label, float min, float max, float value, bool whole)
        {
            RectTransform row = Row(panel, label);
            var sliderObject = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            var rect = (RectTransform)sliderObject.transform;
            rect.SetParent(row, false);
            rect.anchorMin = new Vector2(0.45f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(0f, -10f);
            rect.offsetMax = new Vector2(-12f, 10f);

            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            var backgroundRect = (RectTransform)background.transform;
            backgroundRect.SetParent(rect, false);
            Stretch(backgroundRect);
            background.GetComponent<Image>().color = new Color(0.16f, 0.14f, 0.12f, 0.9f);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            var fillAreaRect = (RectTransform)fillArea.transform;
            fillAreaRect.SetParent(rect, false);
            Stretch(fillAreaRect);
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            var fillRect = (RectTransform)fill.transform;
            fillRect.SetParent(fillAreaRect, false);
            Stretch(fillRect);
            fill.GetComponent<Image>().color = HeroesUIArt.Gold;

            var slider = sliderObject.GetComponent<Slider>();
            slider.fillRect = fillRect;
            slider.targetGraphic = fill.GetComponent<Image>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = whole;
            slider.value = value;
            return slider;
        }

        private static Toggle Toggle(Transform panel, string label, bool value)
        {
            RectTransform row = Row(panel, label);
            var toggleObject = new GameObject("Toggle", typeof(RectTransform), typeof(Toggle));
            var rect = (RectTransform)toggleObject.transform;
            rect.SetParent(row, false);
            rect.anchorMin = new Vector2(0.45f, 0.5f);
            rect.anchorMax = new Vector2(0.45f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(34f, 34f);

            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            var backgroundRect = (RectTransform)background.transform;
            backgroundRect.SetParent(rect, false);
            Stretch(backgroundRect);
            background.GetComponent<Image>().color = new Color(0.16f, 0.14f, 0.12f, 0.95f);

            var check = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            var checkRect = (RectTransform)check.transform;
            checkRect.SetParent(backgroundRect, false);
            Stretch(checkRect, 6f);
            check.GetComponent<Image>().color = HeroesUIArt.Gold;

            var toggle = toggleObject.GetComponent<Toggle>();
            toggle.targetGraphic = background.GetComponent<Image>();
            toggle.graphic = check.GetComponent<Image>();
            toggle.isOn = value;
            return toggle;
        }

        private static RectTransform Row(Transform panel, string label)
        {
            var row = new GameObject(label, typeof(RectTransform));
            var rect = (RectTransform)row.transform;
            rect.SetParent(panel, false);
            rect.sizeDelta = GameMenuInstaller.InputRowSize;

            var text = new GameObject("Label", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            var textRect = (RectTransform)text.transform;
            textRect.SetParent(rect, false);
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(0.44f, 1f);
            textRect.offsetMin = new Vector2(12f, 0f);
            textRect.offsetMax = Vector2.zero;
            var label3 = text.GetComponent<TMPro.TextMeshProUGUI>();
            label3.text = label;
            label3.fontSize = 24f;
            label3.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
            label3.color = new Color(0.92f, 0.88f, 0.78f);
            return rect;
        }

        private static void Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }
    }
}
