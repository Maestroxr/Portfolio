using System;
using System.Collections.Generic;
using Gamebox;
using Gamebox.Editor;
using Gamebox.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Portfolio.Checkers.EditorTools
{
    /// <summary>
    /// Builds the scene of the game from the generated assets: lighting, camera and volume, the table and the board
    /// with its square and piece pools, the manager with its controllers and players, the shared menu as the title and
    /// pause menu, the HUD canvas, the online lobby and the event system. The scene file is replaced, its GUID kept.
    /// </summary>
    internal static class CheckersSceneBuilder
    {
        public const string Database = "skinnerboxes-checkers";
        public const string VolumePath = "Config/CheckersVolume.asset";
        public const string SquarePrefabPath = "Prefabs/Square.prefab";
        public const string PiecePrefabPath = "Prefabs/Piece.prefab";
        public const string LocalizationTable = "Checkers";

        /// <summary>Height of the playing surface over the board root; the slab's top sits just below it.</summary>
        public const float Surface = 0.15f;

        private const float SlabThickness = 0.3f;

        public static void Build()
        {
            CheckersThemeSpec spec = CheckersThemeSpec.Classic;
            CheckersTheme classic = CheckersContentBuilder.ClassicTheme;
            BuildVolumeProfile();
            GameObject squarePrefab = BuildSquarePrefab(spec);
            GameObject piecePrefab = BuildPiecePrefab(spec);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildLighting(spec);
            Camera camera = BuildCamera(spec, out CameraRig rig);
            GameObject table = BuildTable(spec);
            BoardView board = BuildBoard(spec, squarePrefab, piecePrefab);
            CheckersAssets.SetObject(board, "table", table.GetComponent<MeshRenderer>());
            CheckersAssets.SetObject(rig, "view", camera);
            CheckersAssets.SetObject(rig, "board", board);
            CheckersAudio audio = BuildAudio();

            var managerObject = new GameObject("Checkers");
            var storage = managerObject.AddComponent<Storage>();
            CheckersAssets.Set(storage, "DefaultStorageStrategy", p => p.enumValueIndex = (int)StorageStrategies.PlayerPrefs);
            var manager = managerObject.AddComponent<CheckersGameManager>();
            var controllerObject = new GameObject("Controller");
            var controller = controllerObject.AddComponent<CheckersController>();
            CheckersPlayer[] players = BuildPlayers();

            GameObject menu = BuildMenu(scene, manager, controller, classic, out CheckersUI ui);
            BuildHud(ui, classic, spec);

            var input = managerObject.AddComponent<BoardInput>();
            CheckersAssets.SetObject(input, "manager", manager);
            CheckersAssets.SetObject(input, "board", board);
            CheckersAssets.SetObject(input, "view", camera);

            CheckersAssets.SetObject(manager, "settings", CheckersContentBuilder.DefaultSettings);
            CheckersAssets.SetObject(manager, "controller", controller);
            CheckersAssets.SetObject(manager, "ui", ui);
            CheckersAssets.SetObject(manager, "campaign", CheckersContentBuilder.Campaign);
            CheckersAssets.SetObject(manager, "board", board);
            CheckersAssets.SetObject(manager, "cameraRig", rig);
            CheckersAssets.SetObject(manager, "sound", audio);
            CheckersAssets.SetObjects(manager, "players", players);
            CheckersAssets.SetObjects(manager, "PlayerList", players);
            CheckersAssets.SetObject(manager, "StorageBehaviour", storage);
            CheckersAssets.Set(manager, "GameIdentifier", p => p.intValue = (int)GameType.Checkers);
            CheckersAssets.SetObject(controller, "UI", ui);
            CheckersAssets.SetObject(controller, "BaseManager", manager);

            Component online = OnlineInstaller.Install(scene, typeof(Portfolio.Checkers.Server.GameServerClient), typeof(CheckersOnlineController), manager, ui, Database,
                new OnlineInstaller.LobbyStyle
                {
                    Title = "PLAY ONLINE",
                    Font = CheckersArtBuilder.TitleFont,
                    BodyFont = CheckersArtBuilder.BodyFont,
                    Accent = spec.Accent,
                    Window = spec.LobbyWindow,
                    Row = spec.LobbyRow,
                    HideTurnBanner = true
                });
            CheckersAssets.SetObject(manager, "online", online);

            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }

            GameMenuInstaller.EnsureUrpCameras();
            string path = CheckersAssets.Path(CheckersContentBuilder.ScenePath);
            CheckersAssets.EnsureFolder("Scenes");
            EditorSceneManager.SaveScene(scene, path);
            GameSceneBuildSettings.Sync(true);
            Debug.Log($"Checkers scene saved to {path} ({menu.name} installed).");
        }

        private static void BuildVolumeProfile()
        {
            string path = CheckersAssets.Path(VolumePath);
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                CheckersAssets.EnsureFolder("Config");
                AssetDatabase.CreateAsset(profile, path);
            }
            var bloom = Get<Bloom>(profile);
            bloom.threshold.Override(1f);
            bloom.intensity.Override(0.3f);
            bloom.scatter.Override(0.6f);
            var vignette = Get<Vignette>(profile);
            vignette.intensity.Override(0.3f);
            vignette.smoothness.Override(0.45f);
            var colors = Get<ColorAdjustments>(profile);
            colors.contrast.Override(8f);
            colors.saturation.Override(8f);
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

        private static void BuildLighting(CheckersThemeSpec spec)
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = spec.AmbientSky;
            RenderSettings.ambientEquatorColor = spec.AmbientEquator;
            RenderSettings.ambientGroundColor = spec.AmbientGround;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = spec.Background;
            RenderSettings.fogStartDistance = 30f;
            RenderSettings.fogEndDistance = 70f;

            var sunObject = new GameObject("Sun");
            var sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = spec.SunColor;
            sun.intensity = spec.SunIntensity;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.65f;
            sun.shadowBias = 0.02f;
            sun.shadowNormalBias = 0.3f;
            sun.lightmapBakeType = LightmapBakeType.Realtime;
            sunObject.transform.rotation = Quaternion.Euler(52f, -32f, 0f);
            sunObject.AddComponent<UniversalAdditionalLightData>();
            RenderSettings.sun = sun;

            var fillObject = new GameObject("Fill");
            var fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = spec.AmbientSky;
            fill.intensity = 0.35f;
            fill.shadows = LightShadows.None;
            fill.lightmapBakeType = LightmapBakeType.Realtime;
            fillObject.transform.rotation = Quaternion.Euler(30f, 150f, 0f);
            fillObject.AddComponent<UniversalAdditionalLightData>();
        }

        private static Camera BuildCamera(CheckersThemeSpec spec, out CameraRig rig)
        {
            var rigObject = new GameObject("CameraRig");
            rig = rigObject.AddComponent<CameraRig>();
            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            cameraObject.transform.SetParent(rigObject.transform, false);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = spec.Background;
            camera.fieldOfView = 30f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 150f;
            camera.allowHDR = true;
            camera.allowMSAA = true;
            cameraObject.AddComponent<AudioListener>();
            UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.High;
            data.renderShadows = true;
            cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 12f, -9f), Quaternion.Euler(52f, 0f, 0f));

            var volumeObject = new GameObject("Global Volume");
            var volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = CheckersAssets.Load<VolumeProfile>(VolumePath);
            return camera;
        }

        private static GameObject MeshObject(string name, Transform parent, Mesh mesh, Material material, bool shadows = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            return go;
        }

        private static GameObject BuildTable(CheckersThemeSpec spec)
        {
            GameObject table = MeshObject("Table", null, CheckersArtBuilder.Mesh("Table"), CheckersArtBuilder.Material("Table", spec), false);
            table.transform.localPosition = new Vector3(0f, -0.001f, 0f);
            return table;
        }

        /// <summary>The board root with its slab and the two pools; squares and pieces are deployed at run time.</summary>
        private static BoardView BuildBoard(CheckersThemeSpec spec, GameObject squarePrefab, GameObject piecePrefab)
        {
            var boardObject = new GameObject("Board");
            var board = boardObject.AddComponent<BoardView>();
            GameObject slab = MeshObject("Slab", boardObject.transform, CheckersArtBuilder.Mesh("Slab"), CheckersArtBuilder.Material("Slab", spec));
            slab.transform.localPosition = new Vector3(0f, Surface - SlabThickness * 0.5f - 0.002f, 0f);
            slab.transform.localScale = new Vector3(8.6f, 1f, 8.6f);
            SquarePool squarePool = BuildPool<SquarePool>("Squares", boardObject.transform, squarePrefab, 100);
            PiecePool piecePool = BuildPool<PiecePool>("Pieces", boardObject.transform, piecePrefab, 40);
            CheckersAssets.SetFloat(board, "surface", Surface);
            CheckersAssets.SetObject(board, "slab", slab.transform);
            CheckersAssets.SetObject(board, "squarePool", squarePool);
            CheckersAssets.SetObject(board, "piecePool", piecePool);
            return board;
        }

        private static T BuildPool<T>(string name, Transform parent, GameObject prefab, int size) where T : Component
        {
            var poolObject = new GameObject(name);
            poolObject.transform.SetParent(parent, false);
            poolObject.SetActive(false);
            var pool = poolObject.AddComponent<T>();
            CheckersAssets.Set(pool, "MaxSize", p => p.intValue = size);
            CheckersAssets.SetObject(pool, "Instance", prefab);
            poolObject.SetActive(true);
            return pool;
        }

        /// <summary>One square: a lit quad, a highlight quad and a smaller dot quad (both inactive, tinted at run time).</summary>
        private static GameObject BuildSquarePrefab(CheckersThemeSpec spec)
        {
            GameObject root = MeshObject("Square", null, CheckersArtBuilder.Mesh("Square"), CheckersArtBuilder.Material("LightSquare", spec), false);
            var square = root.AddComponent<SquareView>();
            Material highlightMaterial = CheckersArtBuilder.Material("Highlight", spec);
            GameObject highlight = MeshObject("Highlight", root.transform, CheckersArtBuilder.Mesh("Square"), highlightMaterial, false);
            highlight.transform.localPosition = new Vector3(0f, 0.006f, 0f);
            highlight.transform.localScale = new Vector3(0.96f, 1f, 0.96f);
            highlight.SetActive(false);
            GameObject dot = MeshObject("Dot", root.transform, CheckersArtBuilder.Mesh("Square"), highlightMaterial, false);
            dot.transform.localPosition = new Vector3(0f, 0.008f, 0f);
            dot.transform.localScale = new Vector3(0.34f, 1f, 0.34f);
            dot.SetActive(false);
            CheckersAssets.SetInt(square, "index", -1);
            CheckersAssets.SetObject(square, "face", root.GetComponent<MeshRenderer>());
            CheckersAssets.SetObject(square, "highlight", highlight.GetComponent<MeshRenderer>());
            CheckersAssets.SetObject(square, "dot", dot.GetComponent<MeshRenderer>());
            return CheckersAssets.SavePrefab(root, SquarePrefabPath);
        }

        /// <summary>One piece: the disc, a crown on top (inactive until promoted) and a contact shadow under it.</summary>
        private static GameObject BuildPiecePrefab(CheckersThemeSpec spec)
        {
            GameObject root = MeshObject("Piece", null, CheckersArtBuilder.Mesh("Piece"), CheckersArtBuilder.Material("LightPiece", spec));
            var piece = root.AddComponent<PieceView>();
            GameObject crown = MeshObject("Crown", root.transform, CheckersArtBuilder.Mesh("Crown"), CheckersArtBuilder.Material("LightCrown", spec));
            crown.transform.localPosition = new Vector3(0f, CheckersArtBuilder.PieceHeight - 0.012f, 0f);
            crown.SetActive(false);
            GameObject shadow = MeshObject("Shadow", root.transform, CheckersArtBuilder.Mesh("Square"), CheckersArtBuilder.Material("ContactShadow", spec), false);
            shadow.transform.localPosition = new Vector3(0f, 0.003f, 0f);
            shadow.transform.localScale = new Vector3(0.9f, 1f, 0.9f);
            shadow.GetComponent<MeshRenderer>().receiveShadows = false;
            CheckersAssets.SetObject(piece, "disc", root.GetComponent<MeshRenderer>());
            CheckersAssets.SetObject(piece, "crown", crown);
            CheckersAssets.SetObject(piece, "shadow", shadow.transform);
            return CheckersAssets.SavePrefab(root, PiecePrefabPath);
        }

        private static CheckersAudio BuildAudio()
        {
            var audioObject = new GameObject("Audio");
            var effects = audioObject.AddComponent<AudioSource>();
            effects.playOnAwake = false;
            effects.spatialBlend = 0f;
            var audio = audioObject.AddComponent<CheckersAudio>();
            CheckersAssets.SetObject(audio, "effects", effects);
            CheckersAssets.SetObject(audio, "move", CheckersArtBuilder.Clip("Move"));
            CheckersAssets.SetObject(audio, "capture", CheckersArtBuilder.Clip("Capture"));
            CheckersAssets.SetObject(audio, "king", CheckersArtBuilder.Clip("King"));
            CheckersAssets.SetObject(audio, "win", CheckersArtBuilder.Clip("Win"));
            CheckersAssets.SetObject(audio, "lose", CheckersArtBuilder.Clip("Lose"));
            CheckersAssets.SetObject(audio, "click", CheckersArtBuilder.Clip("Click"));
            return audio;
        }

        private static CheckersPlayer[] BuildPlayers()
        {
            var root = new GameObject("Players");
            var players = new CheckersPlayer[2];
            for (int i = 0; i < players.Length; i++)
            {
                var playerObject = new GameObject($"Player {i + 1}");
                playerObject.transform.SetParent(root.transform, false);
                players[i] = playerObject.AddComponent<CheckersPlayer>();
            }
            return players;
        }

        /// <summary>The shared menu prefab as the title and pause menu, restyled from the classic theme.</summary>
        private static GameObject BuildMenu(Scene scene, CheckersGameManager manager, CheckersController controller, CheckersTheme classic, out CheckersUI ui)
        {
            GameObject menu = GameMenuInstaller.InstallMenu(scene, 100, false);
            ui = menu.AddComponent<CheckersUI>();
            var scaler = menu.GetComponent<CanvasScaler>();
            GameMenuInstaller.WireGameUI(ui, menu, controller, manager, scaler, false);
            SettingsUI settings = GameMenuInstaller.WireSettingsPanel(menu, typeof(CheckersSettingsUI), ui, CheckersContentBuilder.DefaultSettings);
            Button quickGame = GameMenuInstaller.AddMenuButton(menu, "QuickGame", "Play vs Computer");
            Button localPlay = GameMenuInstaller.AddMenuButton(menu, "LocalPlay", "Two Players");
            Button playOnline = GameMenuInstaller.AddMenuButton(menu, "PlayOnline", "Play Online");

            GameMenuInstaller.MenuStyle style = GameMenuInstaller.StyleFrom(classic.Menu, classic.Fonts);
            style.ReferenceResolution = new Vector2(1920f, 1080f);
            style.ReferencePixelsPerUnit = 100f;
            style.Header = "CHECKERS";
            style.SettingsHeader = "Settings";
            style.HeaderFontSize = 44f;
            style.Labels["StartNewGame"] = "Campaign";
            style.Labels["ReturnToGame"] = "Resume";
            style.Labels["SaveGame"] = "Save Game";
            style.Labels["LoadGame"] = "Load Game";
            style.Labels["Game Settings"] = "Settings";
            style.Labels["ExitGame"] = "Exit";
            style.Labels["BackToMenu"] = "Back";
            style.Order = new[] { "ReturnToGame", "StartNewGame", "QuickGame", "LocalPlay", "PlayOnline", "LoadGame", "SaveGame", "Game Settings", "ExitGame" };
            style.Hidden = new[] { "Score", "Level", "Timer", "BarLeft", "BarRight" };
            style.WindowPadding = new RectOffset(46, 46, 78, 44);
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

            CheckersAssets.SetObject(settings, "showMoves", GameMenuInstaller.AddToggle(menu, "ShowMoves", "Show legal moves", true, style));
            CheckersAssets.SetObject(settings, "turnBoard", GameMenuInstaller.AddToggle(menu, "TurnBoard", "Turn board each move", false, style));
            Slider thinkingDelay = GameMenuInstaller.AddSlider(menu, "ThinkingDelay", "Computer thinking time", 0f, 3f, 0.6f, false, style,
                SliderValueLabel.Style.Number, "0.0");
            CheckersAssets.SetObject(settings, "thinkingDelay", thinkingDelay);
            // The value is a time: its unit follows the number.
            var thinkingLabel = thinkingDelay.GetComponentInParent<SliderValueLabel>(true);
            if (thinkingLabel != null)
            {
                CheckersAssets.SetString(thinkingLabel, "suffix", " s");
            }
            CheckersAssets.SetObject(settings, "volume", GameMenuInstaller.AddSlider(menu, "Volume", "Sound volume", 0f, 1f, 0.8f, false, style,
                SliderValueLabel.Style.Percent, "0"));
            CheckersAssets.SetObject(settings, "rules", GameMenuInstaller.AddChoice(menu, "Rules", "Rules", new[] { "English", "Russian", "International" }, 0, style));
            CheckersAssets.SetObject(settings, "computerLevel", GameMenuInstaller.AddSlider(menu, "ComputerLevel", "Computer", 0f, 2f, 1f, true, style,
                SliderValueLabel.Style.Names, "0", new[] { "Easy", "Normal", "Hard" }));
            GameMenuInstaller.AddThemeChoice(menu, style, "Look", GameType.Checkers);
            GameMenuInstaller.MakeThemed(menu, GameType.Checkers);

            CheckersAssets.SetObject(ui, "campaignButton", ui.StartNewGame);
            CheckersAssets.SetObject(ui, "quickGameButton", quickGame);
            CheckersAssets.SetObject(ui, "localPlayButton", localPlay);
            CheckersAssets.SetObject(ui, "onlineButton", playOnline);
            GameMenuInstaller.LocalizeTexts(menu, LocalizationTable);
            return menu;
        }

        /// <summary>The HUD canvas: the two seat panels, the turn line and clock, the toast, the resign button and the result panel.</summary>
        private static void BuildHud(CheckersUI ui, CheckersTheme classic, CheckersThemeSpec spec)
        {
            var hudObject = new GameObject("Hud", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            hudObject.layer = LayerMask.NameToLayer("UI");
            var canvas = hudObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = hudObject.GetComponent<CanvasScaler>();
            UIBuildUtils.ConfigureScaler(scaler);
            scaler.referencePixelsPerUnit = 100f;
            RectTransform safe = UIBuildUtils.CreateSafeArea(hudObject.transform);

            TMP_FontAsset title = CheckersArtBuilder.TitleFont;
            TMP_FontAsset bold = CheckersArtBuilder.BoldFont;
            TMP_FontAsset body = CheckersArtBuilder.BodyFont;
            Sprite rounded = CheckersArtBuilder.UI("Rounded");
            Sprite circle = CheckersArtBuilder.UI("Circle");
            Sprite star = CheckersArtBuilder.UI("Star");
            Color ink = spec.Ink;
            Color paper = spec.Paper;

            RectTransform parts = UIBuildUtils.CreateRect(safe, "Parts");
            UIBuildUtils.Stretch(parts);

            var seatPanels = new SeatPanel[2];
            seatPanels[0] = BuildSeatPanel(parts, "Seat 1", new Vector2(0f, 1f), new Vector2(24f, -24f), rounded, circle, bold, body, paper, ink, spec.LightColor, spec.Accent);
            seatPanels[1] = BuildSeatPanel(parts, "Seat 2", new Vector2(1f, 1f), new Vector2(-24f, -24f), rounded, circle, bold, body, paper, ink, spec.DarkColor, spec.Accent);

            TextMeshProUGUI turnText = Label(parts, "TurnText", "", 34f, Color.white, bold, TextAlignmentOptions.Center);
            UIBuildUtils.Anchor(turnText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(800f, 52f));

            RectTransform clock = UIBuildUtils.CreateRect(parts, "Clock");
            UIBuildUtils.Anchor(clock, new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(360f, 18f));
            Image track = UIBuildUtils.CreateImage(clock, "Track", new Color(0f, 0f, 0f, 0.45f));
            track.sprite = rounded;
            track.type = Image.Type.Sliced;
            UIBuildUtils.Stretch(track.rectTransform);
            Image clockFill = UIBuildUtils.CreateImage(clock, "Fill", spec.Accent);
            clockFill.sprite = rounded;
            clockFill.type = Image.Type.Filled;
            clockFill.fillMethod = Image.FillMethod.Horizontal;
            clockFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            clockFill.fillAmount = 1f;
            UIBuildUtils.Stretch(clockFill.rectTransform, 3f, 3f, 3f, 3f);
            // The seconds sit under the bar: the box hangs from its bottom pivot, clear of the bar's lower edge.
            TextMeshProUGUI clockText = Label(clock, "ClockText", "", 24f, Color.white, bold, TextAlignmentOptions.Center);
            UIBuildUtils.Anchor(clockText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, -38f), new Vector2(360f, 34f));

            TextMeshProUGUI toastText = Label(parts, "Toast", "", 30f, Color.white, bold, TextAlignmentOptions.Center);
            UIBuildUtils.Anchor(toastText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 48f), new Vector2(1200f, 46f));

            Button resign = RoundButton(parts, "ResignButton", "Resign", new Vector2(200f, 54f), rounded, bold, paper, ink, 24f);
            UIBuildUtils.Anchor(resign.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(-24f, 24f), new Vector2(200f, 54f));

            // The result panel is a sibling of the parts, so it can show while the parts are hidden.
            RectTransform result = UIBuildUtils.CreateRect(safe, "ResultPanel");
            UIBuildUtils.Anchor(result, new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(680f, 440f));
            Image resultBackground = UIBuildUtils.CreateImage(result, "Background", paper);
            resultBackground.sprite = rounded;
            resultBackground.type = Image.Type.Sliced;
            UIBuildUtils.Stretch(resultBackground.rectTransform);
            TextMeshProUGUI resultTitle = Label(result, "Title", "", 52f, ink, title, TextAlignmentOptions.Center);
            UIBuildUtils.Anchor(resultTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(620f, 70f));
            TextMeshProUGUI resultDetail = Label(result, "Detail", "", 26f, ink, body, TextAlignmentOptions.Center);
            resultDetail.textWrappingMode = TextWrappingModes.Normal;
            resultDetail.enableAutoSizing = false;
            UIBuildUtils.Anchor(resultDetail.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(600f, 110f));
            // Three stars in a row under the words; lit or dim at run time.
            RectTransform starsRow = UIBuildUtils.CreateRect(result, "Stars");
            UIBuildUtils.Anchor(starsRow, new Vector2(0.5f, 1f), new Vector2(0f, -250f), new Vector2(220f, 60f));
            var starImages = new Image[3];
            for (int i = 0; i < starImages.Length; i++)
            {
                Image image = UIBuildUtils.CreateImage(starsRow, $"Star {i + 1}", spec.Accent, false);
                image.sprite = star;
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
                image.raycastTarget = false;
                UIBuildUtils.Anchor(image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 68f, 0f), new Vector2(54f, 54f));
                starImages[i] = image;
            }
            Button again = RoundButton(result, "AgainButton", "Play again", new Vector2(260f, 60f), rounded, bold, spec.Accent, Color.white, 26f);
            UIBuildUtils.Anchor(again.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(-150f, 50f), new Vector2(260f, 60f));
            Button menuButton = RoundButton(result, "MenuButton", "Menu", new Vector2(260f, 60f), rounded, bold, spec.Panel, ink, 26f);
            UIBuildUtils.Anchor(menuButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(150f, 50f), new Vector2(260f, 60f));
            result.gameObject.SetActive(false);

            CheckersAssets.SetObject(ui, "hud", parts.gameObject);
            CheckersAssets.SetObjects(ui, "seatPanels", seatPanels);
            CheckersAssets.SetObject(ui, "turnText", turnText);
            CheckersAssets.SetObject(ui, "clockFill", clockFill);
            CheckersAssets.SetObject(ui, "clockText", clockText);
            CheckersAssets.SetObject(ui, "toastText", toastText);
            CheckersAssets.SetObject(ui, "resultPanel", result.gameObject);
            CheckersAssets.SetObject(ui, "resultTitle", resultTitle);
            CheckersAssets.SetObject(ui, "resultDetail", resultDetail);
            CheckersAssets.SetObject(ui, "starsRow", starsRow.gameObject);
            CheckersAssets.SetObjects(ui, "starImages", starImages);
            CheckersAssets.Set(ui, "starLit", p => p.colorValue = spec.Accent);
            CheckersAssets.Set(ui, "starDim", p => p.colorValue = new Color(ink.r, ink.g, ink.b, 0.18f));
            CheckersAssets.SetObject(ui, "starSprite", star);
            CheckersAssets.SetObject(ui, "againButton", again);
            CheckersAssets.SetObject(ui, "menuButton", menuButton);
            CheckersAssets.SetObject(ui, "resignButton", resign);
            GameMenuInstaller.LocalizeTexts(hudObject, LocalizationTable);
        }

        /// <summary>
        /// A seat's card: colour badge, name, side, captured count, a turn mark (in <paramref name="mark"/>, a colour
        /// that shows on the card whichever side the seat plays) and a king icon (both inactive).
        /// </summary>
        private static SeatPanel BuildSeatPanel(Transform parent, string name, Vector2 anchor, Vector2 position, Sprite rounded, Sprite circle,
            TMP_FontAsset bold, TMP_FontAsset body, Color paper, Color ink, Color seatColor, Color mark)
        {
            // Anchor() puts the pivot at the anchor, so every child is placed from the card's top left corner: the
            // badge in the left column, three lines of words beside it, the turn mark at the right edge.
            var size = new Vector2(360f, 120f);
            RectTransform card = UIBuildUtils.CreateRect(parent, name);
            UIBuildUtils.Anchor(card, anchor, position, size);
            card.pivot = anchor;
            Image background = UIBuildUtils.CreateImage(card, "Background", new Color(paper.r, paper.g, paper.b, 0.92f));
            background.sprite = rounded;
            background.type = Image.Type.Sliced;
            UIBuildUtils.Stretch(background.rectTransform);
            Image badge = UIBuildUtils.CreateImage(card, "Badge", seatColor, false);
            badge.sprite = circle;
            badge.type = Image.Type.Simple;
            UIBuildUtils.Anchor(badge.rectTransform, new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(56f, 56f));
            TextMeshProUGUI nameText = Label(card, "Name", "", 28f, ink, bold, TextAlignmentOptions.Left);
            UIBuildUtils.Anchor(nameText.rectTransform, new Vector2(0f, 1f), new Vector2(92f, -12f), new Vector2(222f, 40f));
            TextMeshProUGUI sideText = Label(card, "Side", "", 20f, new Color(ink.r, ink.g, ink.b, 0.7f), body, TextAlignmentOptions.Left);
            UIBuildUtils.Anchor(sideText.rectTransform, new Vector2(0f, 1f), new Vector2(92f, -52f), new Vector2(222f, 28f));
            TextMeshProUGUI capturedText = Label(card, "Captured", "", 22f, ink, bold, TextAlignmentOptions.Left);
            UIBuildUtils.Anchor(capturedText.rectTransform, new Vector2(0f, 1f), new Vector2(92f, -80f), new Vector2(222f, 30f));
            Image turnMark = UIBuildUtils.CreateImage(card, "TurnMark", mark, false);
            turnMark.sprite = circle;
            turnMark.type = Image.Type.Simple;
            UIBuildUtils.Anchor(turnMark.rectTransform, new Vector2(1f, 0.5f), new Vector2(-20f, 28f), new Vector2(20f, 20f));
            turnMark.gameObject.SetActive(false);
            Image kingIcon = UIBuildUtils.CreateImage(card, "KingIcon", new Color(0.85f, 0.7f, 0.35f), false);
            kingIcon.sprite = circle;
            kingIcon.type = Image.Type.Simple;
            UIBuildUtils.Anchor(kingIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(40f, -42f), new Vector2(16f, 16f));
            kingIcon.gameObject.SetActive(false);
            var panel = card.gameObject.AddComponent<SeatPanel>();
            CheckersAssets.SetObject(panel, "badge", badge);
            CheckersAssets.SetObject(panel, "nameText", nameText);
            CheckersAssets.SetObject(panel, "sideText", sideText);
            CheckersAssets.SetObject(panel, "capturedText", capturedText);
            CheckersAssets.SetObject(panel, "turnMark", turnMark.gameObject);
            CheckersAssets.SetObject(panel, "kingIcon", kingIcon.gameObject);
            return panel;
        }

        /// <summary>A one line label: no wrapping, overflow allowed, sized down to fit.</summary>
        private static TextMeshProUGUI Label(Transform parent, string name, string words, float size, Color color, TMP_FontAsset font, TextAlignmentOptions alignment)
        {
            TextMeshProUGUI text = UIBuildUtils.CreateText(parent, name, words, size, color, alignment);
            text.font = font;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.enableAutoSizing = true;
            text.fontSizeMax = size;
            text.fontSizeMin = Mathf.Max(12f, size * 0.5f);
            return text;
        }

        private static Button RoundButton(Transform parent, string name, string label, Vector2 size, Sprite rounded, TMP_FontAsset font, Color color, Color textColor, float fontSize)
        {
            Button button = UIBuildUtils.CreateButton(parent, name, label, size, fontSize);
            var image = button.GetComponent<Image>();
            image.sprite = rounded;
            image.type = Image.Type.Sliced;
            image.color = color;
            var text = button.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.font = font;
                text.color = textColor;
                text.textWrappingMode = TextWrappingModes.NoWrap;
                text.overflowMode = TextOverflowModes.Overflow;
                text.enableAutoSizing = true;
                text.fontSizeMax = fontSize;
                text.fontSizeMin = 12f;
            }
            return button;
        }
    }
}
