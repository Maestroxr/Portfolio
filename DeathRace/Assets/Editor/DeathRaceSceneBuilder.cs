using Gamebox;
using Gamebox.Editor;
using Gamebox.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Portfolio.DeathRace.EditorTools
{
    /// <summary>
    /// Builds the scene of the game: the sun, the camera and the post processing, the game object with the manager, the
    /// race director and the sounds, the shared menu as the title and pause menu (with the game's buttons and settings),
    /// online play (<see cref="BuildOnline"/>, DeathRaceSceneBuilder.Online.cs) and the event system. Everything of a race
    /// (the track, the cars, the HUD, the windows) is built at run time, so the scene stays small. The scene file is
    /// replaced, its GUID kept.
    /// </summary>
    internal static partial class DeathRaceSceneBuilder
    {
        public const string VolumePath = "Config/DeathRaceVolume.asset";
        public const string LocalizationTable = "DeathRace";

        /// <summary>The shared menu's canvas order (the race HUD is drawn under it, the game's windows over it).</summary>
        private const int MenuSortingOrder = 100;

        public static void Build()
        {
            DeathRaceTheme starting = DeathRaceThemeBuilder.Starting;
            BuildVolumeProfile();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Light sun = BuildLighting(starting);
            Camera camera = BuildCamera(starting, out Volume volume);

            var gameObject = new GameObject("Game");
            var storage = gameObject.AddComponent<Storage>();
            DeathRaceAssets.Set(storage, "DefaultStorageStrategy", p => p.enumValueIndex = (int)StorageStrategies.PlayerPrefs);
            var manager = gameObject.AddComponent<DeathRaceManager>();
            var director = gameObject.AddComponent<RaceDirector>();
            var sounds = gameObject.AddComponent<RaceAudio>();
            AudioSource music = BuildMusic(gameObject.transform);
            var controller = new GameObject("Controller").AddComponent<DeathRaceController>();

            GameObject menu = BuildMenu(scene, manager, controller, starting, out DeathRaceUI ui);

            DeathRaceAssets.SetObject(manager, "settings", DeathRaceContentBuilder.DefaultSettings);
            DeathRaceAssets.SetObject(manager, "controller", controller);
            DeathRaceAssets.SetObject(manager, "ui", ui);
            DeathRaceAssets.SetObject(manager, "campaign", DeathRaceContentBuilder.Campaign);
            DeathRaceAssets.SetObject(manager, "content", DeathRaceContentBuilder.Content);
            DeathRaceAssets.SetObject(manager, "director", director);
            DeathRaceAssets.SetObject(manager, "mainCamera", camera);
            DeathRaceAssets.SetObject(manager, "sun", sun);
            DeathRaceAssets.SetObject(manager, "volume", volume);
            DeathRaceAssets.SetObject(manager, "music", music);
            DeathRaceAssets.SetObject(manager, "sounds", sounds);
            DeathRaceAssets.SetObject(manager, "StorageBehaviour", storage);
            DeathRaceAssets.Set(manager, "GameIdentifier", p => p.intValue = (int)GameType.DeathRace);
            DeathRaceAssets.SetObject(director, "manager", manager);
            DeathRaceAssets.SetObject(controller, "UI", ui);
            DeathRaceAssets.SetObject(controller, "BaseManager", manager);

            Component online = BuildOnline(scene, manager, ui, starting);
            if (online != null)
            {
                DeathRaceAssets.SetObject(manager, "online", online);
            }

            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }

            GameMenuInstaller.EnsureUrpCameras();
            string path = DeathRaceAssets.Path(DeathRaceContentBuilder.ScenePath);
            DeathRaceAssets.EnsureFolder("Scenes");
            EditorSceneManager.SaveScene(scene, path);
            GameSceneBuildSettings.Sync(true);
            Debug.Log($"Death Race scene saved to {path} ({menu.name} installed).");
        }

        /// <summary>The default post processing: a little bloom for the lights and fire, a vignette, punchier colours.</summary>
        private static void BuildVolumeProfile()
        {
            string path = DeathRaceAssets.Path(VolumePath);
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                DeathRaceAssets.EnsureFolder("Config");
                AssetDatabase.CreateAsset(profile, path);
            }
            var bloom = Get<Bloom>(profile);
            bloom.threshold.Override(0.95f);
            bloom.intensity.Override(0.45f);
            bloom.scatter.Override(0.65f);
            var vignette = Get<Vignette>(profile);
            vignette.intensity.Override(0.28f);
            vignette.smoothness.Override(0.45f);
            var colors = Get<ColorAdjustments>(profile);
            colors.contrast.Override(10f);
            colors.saturation.Override(6f);
            colors.postExposure.Override(0f);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
        }

        private static T Get<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (!profile.TryGet(out T component))
            {
                component = profile.Add<T>(true);
                component.name = typeof(T).Name;
                AssetDatabase.AddObjectToAsset(component, profile);
            }
            return component;
        }

        private static Light BuildLighting(DeathRaceTheme theme)
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            if (theme != null)
            {
                RenderSettings.ambientSkyColor = theme.ambientSky;
                RenderSettings.ambientEquatorColor = theme.ambientEquator;
                RenderSettings.ambientGroundColor = theme.ambientGround;
                RenderSettings.fog = theme.fog;
                RenderSettings.fogColor = theme.background;
                RenderSettings.fogStartDistance = theme.fogStart;
                RenderSettings.fogEndDistance = theme.fogEnd;
            }
            RenderSettings.fogMode = FogMode.Linear;

            var sunObject = new GameObject("Sun");
            var sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = theme != null ? theme.sunColor : Color.white;
            sun.intensity = theme != null ? theme.sunIntensity : 1.2f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.7f;
            sun.shadowBias = 0.03f;
            sun.shadowNormalBias = 0.4f;
            sun.lightmapBakeType = LightmapBakeType.Realtime;
            sunObject.transform.rotation = Quaternion.Euler(theme != null ? theme.sunEuler : new Vector3(52f, -35f, 0f));
            sunObject.AddComponent<UniversalAdditionalLightData>();
            RenderSettings.sun = sun;
            return sun;
        }

        private static Camera BuildCamera(DeathRaceTheme theme, out Volume volume)
        {
            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = theme != null ? theme.background : new Color(0.5f, 0.42f, 0.32f);
            camera.fieldOfView = 40f;
            camera.nearClipPlane = 0.5f;
            camera.farClipPlane = 700f;
            camera.allowHDR = true;
            camera.allowMSAA = true;
            cameraObject.AddComponent<AudioListener>();
            UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.High;
            data.renderShadows = true;
            cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 140f, -80f), Quaternion.Euler(60f, 0f, 0f));

            var volumeObject = new GameObject("Global Volume");
            volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = theme != null && theme.volume != null ? theme.volume : DeathRaceAssets.Load<VolumeProfile>(VolumePath);
            return camera;
        }

        private static AudioSource BuildMusic(Transform parent)
        {
            var musicObject = new GameObject("Music");
            musicObject.transform.SetParent(parent, false);
            var music = musicObject.AddComponent<AudioSource>();
            music.playOnAwake = false;
            music.loop = true;
            music.spatialBlend = 0f;
            music.volume = 0.5f;
            return music;
        }

        /// <summary>The shared menu prefab as the title and pause menu, restyled from the starting theme.</summary>
        private static GameObject BuildMenu(Scene scene, DeathRaceManager manager, DeathRaceController controller, DeathRaceTheme starting, out DeathRaceUI ui)
        {
            GameObject menu = GameMenuInstaller.InstallMenu(scene, MenuSortingOrder, false);
            ui = menu.AddComponent<DeathRaceUI>();
            var scaler = menu.GetComponent<CanvasScaler>();
            GameMenuInstaller.WireGameUI(ui, menu, controller, manager, scaler, true);
            SettingsUI settings = GameMenuInstaller.WireSettingsPanel(menu, typeof(DeathRaceSettingsUI), ui, DeathRaceContentBuilder.DefaultSettings);
            Button quickRace = GameMenuInstaller.AddMenuButton(menu, "QuickRace", "Quick Race");
            Button localPlay = GameMenuInstaller.AddMenuButton(menu, "LocalPlay", "Split Screen");
            Button playOnline = GameMenuInstaller.AddMenuButton(menu, "PlayOnline", "Play Online");

            GameMenuInstaller.MenuStyle style = starting != null ? GameMenuInstaller.StyleFrom(starting.Menu, starting.Fonts) : new GameMenuInstaller.MenuStyle();
            style.ReferenceResolution = new Vector2(1920f, 1080f);
            style.ReferencePixelsPerUnit = 100f;
            style.Header = "DEATH RACE";
            style.SettingsHeader = "Settings";
            style.HeaderFontSize = 54f;
            style.Labels["StartNewGame"] = "Career";
            style.Labels["ReturnToGame"] = "Resume";
            style.Labels["SaveGame"] = "Save Career";
            style.Labels["LoadGame"] = "Load Career";
            style.Labels["Game Settings"] = "Settings";
            style.Labels["ExitGame"] = "Exit";
            style.Labels["BackToMenu"] = "Back";
            style.Order = new[] { "ReturnToGame", "StartNewGame", "QuickRace", "LocalPlay", "PlayOnline", "LoadGame", "SaveGame", "Game Settings", "ExitGame" };
            // The pause button's icon is two bars named BarLeft and BarRight: they stay (a race pauses from the corner).
            style.Hidden = new[] { "Score", "Level", "Timer" };
            style.WindowPadding = new RectOffset(46, 46, 86, 44);
            style.ButtonSize = new Vector2(420f, 60f);
            style.ButtonSpacing = 10f;
            style.ButtonFontSize = 26f;
            style.SettingsSize = new Vector2(1200f, 640f);
            style.SettingsButtonSize = new Vector2(270f, 56f);
            style.SettingsButtonMargin = new Vector2(40f, 30f);
            style.RowsMargin = new RectOffset(40, 40, 106, 104);
            style.RowColumns = 2;
            style.RowSpacing = 8f;
            style.ColumnSpacing = 18f;
            style.ScrollRows = true;
            style.RowHeight = 56f;
            style.RowFontSize = 22f;
            style.TrackHeight = 18f;
            GameMenuInstaller.Restyle(menu, style);

            DeathRaceAssets.SetObject(settings, "volume", GameMenuInstaller.AddSlider(menu, "Volume", "Sound volume", 0f, 1f, 0.8f, false, style,
                SliderValueLabel.Style.Percent, "0"));
            DeathRaceAssets.SetObject(settings, "musicVolume", GameMenuInstaller.AddSlider(menu, "MusicVolume", "Music volume", 0f, 1f, 0.5f, false, style,
                SliderValueLabel.Style.Percent, "0"));
            DeathRaceAssets.SetObject(settings, "cameraDistance", GameMenuInstaller.AddChoice(menu, "CameraDistance", "Camera", new[] { "Near", "Normal", "Far" }, 1, style));
            DeathRaceAssets.SetObject(settings, "screenShake", GameMenuInstaller.AddToggle(menu, "ScreenShake", "Screen shake", true, style));
            DeathRaceAssets.SetObject(settings, "showMinimap", GameMenuInstaller.AddToggle(menu, "ShowMinimap", "Show the minimap", true, style));
            var lapNames = new string[DeathRaceSettings.MaxLaps + 1];
            lapNames[0] = "Track's own";
            for (int laps = 1; laps <= DeathRaceSettings.MaxLaps; laps++)
            {
                lapNames[laps] = laps.ToString();
            }
            DeathRaceAssets.SetObject(settings, "quickLaps", GameMenuInstaller.AddSlider(menu, "QuickLaps", "Quick race laps", 0f, DeathRaceSettings.MaxLaps, 0f, true, style,
                SliderValueLabel.Style.Names, "0", lapNames));
            GameMenuInstaller.AddThemeChoice(menu, style, "Look", GameType.DeathRace);
            GameMenuInstaller.MakeThemed(menu, GameType.DeathRace);

            DeathRaceAssets.SetObject(ui, "quickRaceButton", quickRace);
            DeathRaceAssets.SetObject(ui, "localPlayButton", localPlay);
            DeathRaceAssets.SetObject(ui, "onlineButton", playOnline);
            GameMenuInstaller.LocalizeTexts(menu, LocalizationTable);
            return menu;
        }
    }
}
