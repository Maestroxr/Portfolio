using System.Collections.Generic;
using Gamebox;
using Gamebox.Editor;
using Gamebox.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Portfolio.MemoryCards.EditorTools
{
    /// <summary>
    /// Builds the card prefab and Scenes/MemoryCards.unity: the backdrop, the board with its card pool, the level
    /// select, the HUD, the results screen, the pause menu and the free play settings panel (wired to the base
    /// <see cref="GameUI"/> and <see cref="SettingsUI"/> fields), particles, popups and audio.
    /// </summary>
    internal static class MemoryCardsSceneBuilder
    {
        public const string ScenePath = "Scenes/MemoryCards.unity";
        public const string CardPrefabPath = "Prefabs/Card.prefab";

        private static readonly Vector2 Reference = new Vector2(1920f, 1080f);
        private static readonly Color Ink = Shapes.Hex("#2B2D42");
        private static readonly Color SoftInk = Shapes.Hex("#6B6F86");
        private static readonly Color Gold = Shapes.Hex("#FFC933");
        private static readonly Color Dim = new Color(0.08f, 0.06f, 0.16f, 0.58f);

        private static TMP_FontAsset font;
        private static Material outline;
        private static Material shadow;
        private static Material title;

        private static Vector2 Center => new Vector2(0.5f, 0.5f);

        public static void Build()
        {
            font = MemoryCardsArtBuilder.Font;
            outline = MemoryCardsArtBuilder.FontMaterial("Outline");
            shadow = MemoryCardsArtBuilder.FontMaterial("Shadow");
            title = MemoryCardsArtBuilder.FontMaterial("Title");
            GameObject cardPrefab = BuildCardPrefab();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildCamera();
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            MemoryCardsCampaign campaign = MemoryCardsContentBuilder.Campaign;
            CardWorld farm = MemoryCardsContentBuilder.World(0);

            var game = new GameObject("Game");
            var manager = game.AddComponent<MemoryCardsGameManager>();
            var storage = game.AddComponent<Storage>();
            var controller = game.AddComponent<MemoryCardsController>();
            var sounds = game.AddComponent<CardsAudio>();
            MemoryCardsAssets.Set(storage, "DefaultStorageStrategy", p => p.enumValueIndex = (int)StorageStrategies.PlayerPrefs);
            var playerObject = new GameObject("Player");
            playerObject.transform.SetParent(game.transform, false);
            var player = playerObject.AddComponent<MemoryCardsPlayer>();
            BuildAudio(game.transform, sounds);

            Canvas canvas = BuildCanvas("MemoryCardsCanvas", 0);
            Transform root = canvas.transform;
            AmbientBackdrop backdrop = BuildBackdrop(root);
            // The board, the screens' texts and buttons stay clear of notches and rounded corners; backgrounds, dims and
            // overlays cover the whole screen.
            RectTransform boardArea = Stretch(UIBuildUtils.CreateSafeArea(root, "BoardSafeArea"), "BoardArea", 70f, 92f, 70f, 196f);
            RectTransform boardRect = Stretch(boardArea, "Board");
            var board = boardRect.gameObject.AddComponent<CardBoard>();
            FlippableCache pool = BuildPool(game.transform, cardPrefab, boardRect);

            var ui = canvas.gameObject.AddComponent<MemoryCardsUI>();
            RectTransform screens = Stretch(root, "Screens");
            BuildTitle(screens, ui, campaign);
            BuildHud(screens, ui);
            BuildResults(screens, ui);
            UIParticles particles = BuildParticles(root);
            BuildOverlays(root, ui);
            BuildMenu(root, ui, manager, controller, canvas.GetComponent<CanvasScaler>());
            ui.curtain = Image(root, "Curtain", null, new Color(1f, 1f, 1f, 0f), Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            StretchRect(ui.curtain.rectTransform);

            ui.starFull = MemoryCardsArtBuilder.Kenney("StarFull");
            ui.starEmpty = MemoryCardsArtBuilder.Kenney("StarEmpty");
            ui.heartFull = MemoryCardsArtBuilder.Icon("HeartFull");
            ui.heartEmpty = MemoryCardsArtBuilder.Icon("HeartEmpty");
            ui.goalDone = MemoryCardsArtBuilder.Kenney("GoalDone");
            ui.goalMissed = MemoryCardsArtBuilder.Kenney("GoalMissed");
            ui.modeIcons = new[]
            {
                MemoryCardsArtBuilder.Icon("Cards"), MemoryCardsArtBuilder.Icon("Stopwatch"), MemoryCardsArtBuilder.Icon("Heart"),
                MemoryCardsArtBuilder.Icon("Moves"), MemoryCardsArtBuilder.Icon("Flag"), MemoryCardsArtBuilder.Icon("Infinity"),
                MemoryCardsArtBuilder.Icon("Sliders")
            };

            MemoryCardsAssets.SetObject(controller, "UI", ui);
            MemoryCardsAssets.SetObject(controller, "BaseManager", manager);
            MemoryCardsAssets.SetObject(manager, "CardsCacheBehaviour", pool);
            MemoryCardsAssets.SetObject(manager, "defaultSettings", MemoryCardsContentBuilder.DefaultSettings);
            MemoryCardsAssets.SetObject(manager, "customSettings", MemoryCardsContentBuilder.CustomSettings);
            MemoryCardsAssets.SetObject(manager, "currentGameSettings", MemoryCardsContentBuilder.CurrentSettings);
            MemoryCardsAssets.SetObject(manager, "gameUI", ui);
            MemoryCardsAssets.SetObject(manager, "controller", controller);
            MemoryCardsAssets.SetObject(manager, "campaign", campaign);
            MemoryCardsAssets.SetObject(manager, "StorageBehaviour", storage);
            MemoryCardsAssets.Set(manager, "GameIdentifier", p => p.intValue = (int)GameType.MemoryCards);
            MemoryCardsAssets.SetObjects(manager, "PlayerList", player);
            MemoryCardsAssets.SetObject(manager, "Audio", sounds.effects[0]);
            manager.player = player;
            manager.board = board;
            manager.backdrop = backdrop;
            manager.particles = particles;
            manager.sounds = sounds;
            manager.defaultCardBack = farm != null ? farm.cardBack : null;
            manager.specialFaces = new[]
            {
                MemoryCardsArtBuilder.Card("Wild"), MemoryCardsArtBuilder.Card("Bomb"), MemoryCardsArtBuilder.Card("Clock"), MemoryCardsArtBuilder.Card("Peek")
            };
            backdrop.Apply(farm, true);

            GameMenuInstaller.EnsureUrpCameras();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, MemoryCardsAssets.Path(ScenePath));
            GameSceneBuildSettings.Sync(true);
        }

        #region Card

        /// <summary>The card: shadow, a body that flips (back, front with the face and badge, ice) and a glow.</summary>
        private static GameObject BuildCardPrefab()
        {
            var root = new GameObject("Card", typeof(RectTransform), typeof(CanvasGroup));
            root.layer = 5;
            var rect = (RectTransform)root.transform;
            rect.sizeDelta = new Vector2(190f, 252f);
            var card = root.AddComponent<Flippable>();
            card.group = root.GetComponent<CanvasGroup>();

            float padX = (float)MemoryCardsArt.CardPadding / MemoryCardsArt.CardWidth;
            float padY = (float)MemoryCardsArt.CardPadding / MemoryCardsArt.CardHeight;
            card.shadow = Image(rect, "Shadow", MemoryCardsArtBuilder.Card("CardShadow"), Color.white, Vector2.zero, Center, Vector2.zero, Vector2.zero);
            Anchor(card.shadow.rectTransform, new Vector2(-padX, -padY), new Vector2(1f + padX, 1f + padY));

            card.body = Stretch(rect, "Body");
            card.back = Image(card.body, "Back", MemoryCardsArtBuilder.Card("BackFarm"), Color.white, Vector2.zero, Center, Vector2.zero, Vector2.zero, false, true);
            StretchRect(card.back.rectTransform);
            card.front = Image(card.body, "Front", MemoryCardsArtBuilder.Card("CardFront"), Color.white, Vector2.zero, Center, Vector2.zero, Vector2.zero, false, true);
            StretchRect(card.front.rectTransform);
            card.face = Image(card.front.transform, "Face", MemoryCardsArtBuilder.Animal("giraffe"), Color.white, Vector2.zero, Center, Vector2.zero, Vector2.zero);
            Anchor(card.face.rectTransform, new Vector2(0.07f, 0.17f), new Vector2(0.93f, 0.83f));
            card.face.preserveAspect = true;
            card.badge = Image(card.front.transform, "Badge", MemoryCardsArtBuilder.Card("Badge"), Color.white, new Vector2(1f, 1f), Center, new Vector2(-24f, -24f), new Vector2(54f, 54f));
            card.ice = Image(card.body, "Ice", MemoryCardsArtBuilder.Card("Ice"), Color.white, Vector2.zero, Center, Vector2.zero, Vector2.zero);
            StretchRect(card.ice.rectTransform);
            card.iceSprite = MemoryCardsArtBuilder.Card("Ice");
            card.crackedIceSprite = MemoryCardsArtBuilder.Card("IceCracked");
            card.glow = Image(card.body, "Glow", MemoryCardsArtBuilder.Card("CardGlow"), new Color(1f, 1f, 1f, 0f), Vector2.zero, Center, Vector2.zero, Vector2.zero);
            Anchor(card.glow.rectTransform, new Vector2(-padX, -padY), new Vector2(1f + padX, 1f + padY));
            card.front.gameObject.SetActive(false);
            card.badge.gameObject.SetActive(false);
            card.ice.gameObject.SetActive(false);
            return MemoryCardsAssets.SavePrefab(root, CardPrefabPath);
        }

        private static FlippableCache BuildPool(Transform parent, GameObject cardPrefab, RectTransform board)
        {
            var poolObject = new GameObject("CardPool");
            poolObject.transform.SetParent(parent, false);
            var recycled = new GameObject("Recycled", typeof(RectTransform));
            recycled.transform.SetParent(poolObject.transform, false);
            recycled.SetActive(false);
            var pool = poolObject.AddComponent<FlippableCache>();
            MemoryCardsAssets.Set(pool, "MaxSize", p => p.intValue = RoundRules.MaxCards + 4);
            MemoryCardsAssets.SetObject(pool, "Instance", cardPrefab);
            MemoryCardsAssets.SetObject(pool, "DeployedParent", board);
            MemoryCardsAssets.SetObject(pool, "RecycledParent", recycled.transform);
            return pool;
        }

        #endregion

        #region Scene basics

        private static void BuildCamera()
        {
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Shapes.Hex("#62C4FF");
            camera.orthographic = true;
            camera.cullingMask = 0;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
        }

        private static Canvas BuildCanvas(string name, int order)
        {
            var canvasObject = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.layer = 5;
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            // Expands from the reference to any screen shape: phones add room at the sides, tablets above and below.
            UIBuildUtils.ConfigureScaler(canvasObject.GetComponent<CanvasScaler>(), Reference);
            return canvas;
        }

        private static void BuildAudio(Transform parent, CardsAudio sounds)
        {
            var audio = new GameObject("Audio");
            audio.transform.SetParent(parent, false);
            sounds.musicA = Source(audio, "Music A", true);
            sounds.musicB = Source(audio, "Music B", true);
            var effects = new AudioSource[6];
            for (int i = 0; i < effects.Length; i++)
            {
                effects[i] = Source(audio, $"Effects {i + 1}", false);
            }
            sounds.effects = effects;
            sounds.menuMusic = MemoryCardsArtBuilder.GeneratedSound("MenuMusic");
            sounds.playMusic = MemoryCardsArtBuilder.GeneratedSound("PlayMusic");
            sounds.flips = new[] { MemoryCardsArtBuilder.KenneySound("card-slide-1"), MemoryCardsArtBuilder.KenneySound("card-slide-2"), MemoryCardsArtBuilder.KenneySound("card-slide-3") };
            sounds.deals = new[] { MemoryCardsArtBuilder.KenneySound("card-place-1"), MemoryCardsArtBuilder.KenneySound("card-place-2"), MemoryCardsArtBuilder.KenneySound("card-place-3") };
            sounds.shuffle = MemoryCardsArtBuilder.KenneySound("card-shuffle");
            sounds.fan = MemoryCardsArtBuilder.KenneySound("card-fan-1");
            sounds.match = MemoryCardsArtBuilder.GeneratedSound("Match");
            sounds.mismatch = MemoryCardsArtBuilder.KenneySound("question_004");
            sounds.wrongOrder = MemoryCardsArtBuilder.KenneySound("question_002");
            sounds.bomb = MemoryCardsArtBuilder.GeneratedSound("Bomb");
            sounds.clock = MemoryCardsArtBuilder.KenneySound("drop_004");
            sounds.peek = MemoryCardsArtBuilder.KenneySound("maximize_005");
            sounds.wild = MemoryCardsArtBuilder.GeneratedSound("Wild");
            sounds.crack = MemoryCardsArtBuilder.GeneratedSound("Crack");
            sounds.heart = MemoryCardsArtBuilder.GeneratedSound("Heart");
            sounds.click = MemoryCardsArtBuilder.KenneySound("click_002");
            sounds.back = MemoryCardsArtBuilder.KenneySound("back_001");
            sounds.select = MemoryCardsArtBuilder.KenneySound("select_002");
            sounds.locked = MemoryCardsArtBuilder.KenneySound("error_004");
            sounds.tick = MemoryCardsArtBuilder.KenneySound("tick_002");
            sounds.go = MemoryCardsArtBuilder.KenneySound("maximize_006");
            sounds.star = MemoryCardsArtBuilder.KenneySound("glass_002");
            sounds.victory = MemoryCardsArtBuilder.KenneySound("jingles_STEEL02");
            sounds.gameOver = MemoryCardsArtBuilder.KenneySound("jingles_PIZZI01");
            sounds.record = MemoryCardsArtBuilder.KenneySound("jingles_PIZZI02");
        }

        private static AudioSource Source(GameObject host, string name, bool loop)
        {
            var sourceObject = new GameObject(name);
            sourceObject.transform.SetParent(host.transform, false);
            var source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            source.volume = loop ? 0f : 1f;
            return source;
        }

        private static AmbientBackdrop BuildBackdrop(Transform parent)
        {
            RectTransform rect = Stretch(parent, "Backdrop");
            var backdrop = rect.gameObject.AddComponent<AmbientBackdrop>();
            RectTransform sky = Stretch(rect, "Sky");
            backdrop.gradient = sky.gameObject.AddComponent<GradientGraphic>();
            backdrop.gradient.raycastTarget = false;
            RectTransform patternRect = Stretch(rect, "Pattern");
            var pattern = patternRect.gameObject.AddComponent<RawImage>();
            pattern.texture = MemoryCardsArtBuilder.Pattern;
            pattern.color = new Color(1f, 1f, 1f, 0.07f);
            pattern.raycastTarget = false;
            backdrop.pattern = pattern;
            backdrop.patternTileSize = 300f;
            backdrop.shapesRoot = Stretch(rect, "Shapes");
            backdrop.shapeTemplate = Image(backdrop.shapesRoot, "Shape", null, Color.white, Center, Center, Vector2.zero, new Vector2(100f, 100f));
            backdrop.shapeTemplate.gameObject.SetActive(false);
            Image vignette = Image(rect, "Vignette", MemoryCardsArtBuilder.Ui("Soft"), new Color(1f, 1f, 1f, 0.12f), Center, Center, new Vector2(0f, 60f), new Vector2(1900f, 1300f));
            vignette.raycastTarget = false;
            return backdrop;
        }

        private static UIParticles BuildParticles(Transform parent)
        {
            RectTransform rect = Stretch(parent, "Particles");
            var particles = rect.gameObject.AddComponent<UIParticles>();
            particles.template = Image(rect, "Particle", MemoryCardsArtBuilder.Particle("Spark"), Color.white, Center, Center, Vector2.zero, new Vector2(24f, 24f));
            particles.template.gameObject.SetActive(false);
            particles.spark = MemoryCardsArtBuilder.Particle("Spark");
            particles.star = MemoryCardsArtBuilder.Particle("Star");
            particles.circle = MemoryCardsArtBuilder.Particle("Circle");
            particles.confetti = MemoryCardsArtBuilder.Particle("Confetti");
            particles.shard = MemoryCardsArtBuilder.Particle("Shard");
            return particles;
        }

        #endregion

        #region Level select

        private static void BuildTitle(Transform parent, MemoryCardsUI ui, MemoryCardsCampaign campaign)
        {
            CanvasGroup screen = Screen(parent, "Title");
            ui.titleScreen = screen;
            Transform root = UIBuildUtils.CreateSafeArea(screen.transform);

            // Logo with animals around it.
            RectTransform logo = Rect(root, "Logo", new Vector2(0.5f, 1f), Center, new Vector2(0f, -104f), new Vector2(1180f, 170f));
            ui.logo = logo;
            TextMeshProUGUI name = Text(logo, "Name", "MEMORY CARDS", 104f, Color.white, TextAlignmentOptions.Center, Center, Center, new Vector2(0f, 18f), new Vector2(1180f, 120f), title);
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.enableAutoSizing = true;
            name.fontSizeMin = 60f;
            name.fontSizeMax = 104f;
            name.enableVertexGradient = true;
            name.colorGradient = new VertexGradient(Color.white, Color.white, Shapes.Hex("#FFE9A8"), Shapes.Hex("#FFD36B"));
            name.characterSpacing = 4f;
            Text(logo, "Subtitle", "A critter matching adventure", 30f, Color.white, TextAlignmentOptions.Center, Center, Center, new Vector2(0f, -62f), new Vector2(900f, 44f), outline);
            var mascots = new List<RectTransform>
            {
                Mascot(root, "giraffe", new Vector2(-712f, -104f), 150f, -8f),
                Mascot(root, "panda", new Vector2(712f, -104f), 150f, 8f),
                Mascot(root, "chick", new Vector2(-868f, -150f), 92f, -14f),
                Mascot(root, "penguin", new Vector2(868f, -150f), 92f, 12f)
            };
            ui.titleMascots = mascots.ToArray();

            // World tabs.
            RectTransform tabs = Rect(root, "Worlds", new Vector2(0.5f, 1f), Center, new Vector2(0f, -262f), new Vector2(1400f, 110f));
            int worldCount = campaign != null ? campaign.WorldCount : 4;
            var worldTabs = new List<WorldTab>();
            float tabWidth = 322f;
            float tabStep = tabWidth + 24f;
            for (int i = 0; i < worldCount; i++)
            {
                worldTabs.Add(BuildWorldTab(tabs, i, new Vector2((i - (worldCount - 1) * 0.5f) * tabStep, 0f), new Vector2(tabWidth, 104f)));
            }
            ui.worldTabs = worldTabs.ToArray();

            // Level cards on the left, details on the right.
            RectTransform grid = Rect(root, "Levels", new Vector2(0f, 0f), Center, new Vector2(540f, 420f), new Vector2(760f, 600f));
            var cards = new List<LevelCard>();
            for (int i = 0; i < 6; i++)
            {
                cards.Add(BuildLevelCard(grid, i));
            }
            ui.levelCards = cards.ToArray();
            ui.levelSpacing = new Vector2(232f, 294f);
            ui.levelsPerRow = 3;

            BuildDetails(root, ui);

            // Bottom bar: stars, reset, continue, settings and exit.
            RectTransform stars = Rect(root, "Stars", Vector2.zero, new Vector2(0f, 0f), new Vector2(30f, 18f), new Vector2(400f, 104f));
            Image starsPanel = Image(stars, "Panel", MemoryCardsArtBuilder.Ui("PanelDepth"), new Color(1f, 1f, 1f, 0.92f), Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, true);
            StretchRect(starsPanel.rectTransform);
            Image(stars, "Star", MemoryCardsArtBuilder.Kenney("StarFull"), Color.white, new Vector2(0f, 0.5f), Center, new Vector2(58f, 6f), new Vector2(70f, 66f));
            ui.starsTotal = Text(stars, "Total", "0 / 54", 44f, Ink, TextAlignmentOptions.Left, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(106f, 18f), new Vector2(280f, 52f));
            ui.lifetimeText = Text(stars, "Lifetime", "Welcome!", 22f, SoftInk, TextAlignmentOptions.Left, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(108f, -22f), new Vector2(280f, 32f));
            ui.resetProgressButton = TextButton(root, "ResetProgress", "Reset progress", Vector2.zero, new Vector2(0f, 0f), new Vector2(452f, 30f), new Vector2(250f, 40f), out TextMeshProUGUI resetLabel);
            ui.resetProgressLabel = resetLabel;

            ui.continueButton = Button(root, "Continue", MemoryCardsArtBuilder.Kenney("ButtonYellow"), "CONTINUE", MemoryCardsArtBuilder.Icon("Play"), new Vector2(1f, 0f),
                new Vector2(0.5f, 0f), new Vector2(-640f, 14f), new Vector2(330f, 94f), 34f, Ink, out _);
            ui.titleSettingsButton = RoundButton(root, "Settings", MemoryCardsArtBuilder.Kenney("RoundBlue"), MemoryCardsArtBuilder.Icon("Settings"), Color.white,
                new Vector2(1f, 0f), new Vector2(-190f, 70f), 104f);
            ui.titleExitButton = RoundButton(root, "Exit", MemoryCardsArtBuilder.Kenney("RoundRed"), MemoryCardsArtBuilder.Icon("Power"), Color.white,
                new Vector2(1f, 0f), new Vector2(-68f, 70f), 104f);
        }

        private static RectTransform Mascot(Transform parent, string animal, Vector2 position, float size, float angle)
        {
            Image image = Image(parent, $"Mascot {animal}", MemoryCardsArtBuilder.Animal(animal), Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.2f), position - new Vector2(0f, size * 0.3f), new Vector2(size, size));
            image.preserveAspect = true;
            image.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
            return image.rectTransform;
        }

        private static WorldTab BuildWorldTab(Transform parent, int index, Vector2 position, Vector2 size)
        {
            RectTransform rect = Rect(parent, $"World {index + 1}", Center, Center, position, size);
            var tab = rect.gameObject.AddComponent<WorldTab>();
            tab.content = Stretch(rect, "Content");
            tab.background = Image(tab.content, "Background", MemoryCardsArtBuilder.Ui("PanelDepth"), Color.white, Vector2.zero, Center, Vector2.zero, Vector2.zero, true, true);
            StretchRect(tab.background.rectTransform);
            tab.mascot = Image(tab.content, "Mascot", MemoryCardsArtBuilder.Animal("chick"), Color.white, new Vector2(0f, 0.5f), Center, new Vector2(64f, 6f), new Vector2(92f, 92f));
            tab.mascot.preserveAspect = true;
            tab.title = Text(tab.content, "Title", "World", 27f, Ink, TextAlignmentOptions.Left, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(120f, 20f), new Vector2(190f, 44f));
            tab.title.textWrappingMode = TextWrappingModes.NoWrap;
            tab.title.enableAutoSizing = true;
            tab.title.fontSizeMin = 13f;
            tab.title.fontSizeMax = 26f;
            tab.starIcon = Image(tab.content, "Star", MemoryCardsArtBuilder.Kenney("StarFull"), Color.white, new Vector2(0f, 0.5f), Center, new Vector2(137f, -20f), new Vector2(32f, 30f));
            tab.subtitle = Text(tab.content, "Stars", "0/18", 23f, SoftInk, TextAlignmentOptions.Left, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(160f, -20f), new Vector2(150f, 36f));
            tab.subtitle.textWrappingMode = TextWrappingModes.NoWrap;
            tab.subtitle.enableAutoSizing = true;
            tab.subtitle.fontSizeMin = 13f;
            tab.subtitle.fontSizeMax = 23f;
            tab.lockIcon = Image(tab.content, "Lock", MemoryCardsArtBuilder.Icon("Lock"), Color.white, new Vector2(0f, 0.5f), Center, new Vector2(64f, 6f), new Vector2(44f, 44f));
            tab.button = rect.gameObject.AddComponent<Button>();
            tab.button.targetGraphic = tab.background;
            tab.button.transition = Selectable.Transition.None;
            var press = rect.gameObject.AddComponent<PressScale>();
            press.target = tab.content;
            return tab;
        }

        private static LevelCard BuildLevelCard(Transform parent, int index)
        {
            RectTransform rect = Rect(parent, $"Level {index + 1}", Center, Center, Vector2.zero, new Vector2(184f, 244f));
            var card = rect.gameObject.AddComponent<LevelCard>();
            card.content = Stretch(rect, "Content");
            float padX = (float)MemoryCardsArt.CardPadding / MemoryCardsArt.CardWidth;
            float padY = (float)MemoryCardsArt.CardPadding / MemoryCardsArt.CardHeight;
            Image cardShadow = Image(card.content, "Shadow", MemoryCardsArtBuilder.Card("CardShadow"), Color.white, Vector2.zero, Center, Vector2.zero, Vector2.zero);
            Anchor(cardShadow.rectTransform, new Vector2(-padX, -padY - 0.02f), new Vector2(1f + padX, 1f + padY - 0.02f));
            card.outline = Image(card.content, "Outline", MemoryCardsArtBuilder.Card("CardGlow"), Color.white, Vector2.zero, Center, Vector2.zero, Vector2.zero);
            Anchor(card.outline.rectTransform, new Vector2(-padX, -padY), new Vector2(1f + padX, 1f + padY));
            card.back = Image(card.content, "Back", MemoryCardsArtBuilder.Card("BackFarmPlain"), Color.white, Vector2.zero, Center, Vector2.zero, Vector2.zero, false, true);
            StretchRect(card.back.rectTransform);
            Image plate = Image(card.content, "Plate", MemoryCardsArtBuilder.Ui("Disc"), Color.white, Center, Center, new Vector2(0f, 30f), new Vector2(104f, 104f));
            plate.raycastTarget = false;
            card.number = Text(plate.transform, "Number", "1", 60f, Ink, TextAlignmentOptions.Center, Center, Center, new Vector2(0f, 2f), new Vector2(84f, 70f), shadow);
            card.number.textWrappingMode = TextWrappingModes.NoWrap;
            card.number.enableAutoSizing = true;
            card.number.fontSizeMin = 30f;
            card.number.fontSizeMax = 60f;
            card.icon = Image(plate.transform, "Icon", MemoryCardsArtBuilder.Icon("Infinity"), Ink, Center, Center, Vector2.zero, new Vector2(72f, 72f));
            Image titlePlate = Image(card.content, "TitlePlate", MemoryCardsArtBuilder.Ui("Pill"), new Color(1f, 1f, 1f, 0.95f), Center, Center, new Vector2(0f, -54f), new Vector2(166f, 44f), true);
            titlePlate.raycastTarget = false;
            card.title = Text(titlePlate.transform, "Title", "Level", 19f, Ink, TextAlignmentOptions.Center, Center, Center, Vector2.zero, new Vector2(154f, 40f));
            card.title.enableAutoSizing = true;
            card.title.fontSizeMin = 12f;
            card.title.fontSizeMax = 19f;
            var stars = new List<Image>();
            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * 52f;
                float y = i == 1 ? -114f : -108f;
                stars.Add(Image(card.content, $"Star {i + 1}", MemoryCardsArtBuilder.Kenney("StarEmpty"), Color.white, Center, Center, new Vector2(x, y), i == 1 ? new Vector2(54f, 51f) : new Vector2(46f, 43f)));
            }
            card.stars = stars.ToArray();
            RectTransform lockRoot = Stretch(card.content, "Lock");
            Image lockShade = Image(lockRoot, "Shade", MemoryCardsArtBuilder.Card("CardFront"), new Color(0.12f, 0.12f, 0.22f, 0.5f), Vector2.zero, Center, Vector2.zero, Vector2.zero);
            StretchRect(lockShade.rectTransform);
            Image(lockRoot, "Disc", MemoryCardsArtBuilder.Ui("Disc"), new Color(0.15f, 0.16f, 0.26f, 0.85f), Center, Center, new Vector2(0f, 30f), new Vector2(96f, 96f));
            Image(lockRoot, "Icon", MemoryCardsArtBuilder.Icon("Lock"), Color.white, Center, Center, new Vector2(0f, 32f), new Vector2(58f, 58f));
            card.lockRoot = lockRoot.gameObject;
            card.newBadge = Image(card.content, "New", MemoryCardsArtBuilder.Ui("Pill"), Gold, new Vector2(1f, 1f), Center, new Vector2(-12f, -6f), new Vector2(86f, 40f), true);
            Text(card.newBadge.transform, "Label", "NEW", 22f, Ink, TextAlignmentOptions.Center, Center, Center, new Vector2(0f, 1f), new Vector2(80f, 36f));
            card.newBadge.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -12f);
            card.endlessIcon = MemoryCardsArtBuilder.Icon("Infinity");
            card.freePlayIcon = MemoryCardsArtBuilder.Icon("Sliders");
            card.button = rect.gameObject.AddComponent<Button>();
            card.button.targetGraphic = card.back;
            card.button.transition = Selectable.Transition.None;
            var press = rect.gameObject.AddComponent<PressScale>();
            press.target = (RectTransform)card.content.parent;
            press.hoverScale = 1.04f;
            return card;
        }

        private static void BuildDetails(Transform root, MemoryCardsUI ui)
        {
            RectTransform panel = Rect(root, "Details", new Vector2(1f, 0f), Center, new Vector2(-500f, 418f), new Vector2(740f, 600f));
            Image background = Image(panel, "Panel", MemoryCardsArtBuilder.Ui("PanelDepth"), Color.white, Vector2.zero, Center, Vector2.zero, Vector2.zero, true);
            StretchRect(background.rectTransform);
            ui.detailHeader = Image(panel, "Header", MemoryCardsArtBuilder.Ui("Pill"), Shapes.Hex("#FF9A1F"), new Vector2(0.5f, 1f), Center, new Vector2(0f, -4f), new Vector2(520f, 62f), true);
            ui.detailWorld = Text(ui.detailHeader.transform, "World", "SUNNY FARM", 28f, Color.white, TextAlignmentOptions.Center, Center, Center, new Vector2(0f, 2f), new Vector2(500f, 56f), outline);
            ui.detailTitle = Text(panel, "Title", "1. Hello, Farm!", 46f, Ink, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -50f), new Vector2(660f, 62f));
            ui.detailTitle.enableAutoSizing = true;
            ui.detailTitle.fontSizeMin = 28f;
            ui.detailTitle.fontSizeMax = 46f;
            ui.detailModeIcon = Image(panel, "ModeIcon", MemoryCardsArtBuilder.Icon("Cards"), SoftInk, new Vector2(0f, 1f), Center, new Vector2(62f, -138f), new Vector2(42f, 42f));
            ui.detailMode = Text(panel, "Mode", "CLASSIC", 26f, SoftInk, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(92f, -138f), new Vector2(300f, 40f));
            RectTransform newRoot = Rect(panel, "New", new Vector2(1f, 1f), Center, new Vector2(-186f, -138f), new Vector2(300f, 48f));
            Image newPill = Image(newRoot, "Pill", MemoryCardsArtBuilder.Ui("Pill"), Gold, Vector2.zero, Center, Vector2.zero, Vector2.zero, true);
            StretchRect(newPill.rectTransform);
            ui.detailNew = Text(newRoot, "Label", "NEW: Memorize", 22f, Ink, TextAlignmentOptions.Center, Center, Center, new Vector2(0f, 1f), new Vector2(290f, 46f));
            ui.detailNew.enableAutoSizing = true;
            ui.detailNew.fontSizeMin = 14f;
            ui.detailNew.fontSizeMax = 22f;
            ui.detailNewRoot = newRoot.gameObject;
            ui.detailDescription = Text(panel, "Description", "Description", 26f, Ink, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -170f), new Vector2(660f, 98f));
            ui.detailDescription.enableAutoSizing = true;
            ui.detailDescription.fontSizeMin = 18f;
            ui.detailDescription.fontSizeMax = 26f;
            ui.detailDescription.lineSpacing = -6f;
            ui.detailBoard = Text(panel, "Board", "4x3 board, 6 pairs", 20f, SoftInk, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -272f), new Vector2(660f, 32f));
            ui.detailBoard.enableAutoSizing = true;
            ui.detailBoard.fontSizeMin = 14f;
            ui.detailBoard.fontSizeMax = 20f;

            RectTransform goals = Rect(panel, "Goals", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -310f), new Vector2(660f, 140f));
            ui.detailGoalsRoot = goals.gameObject;
            var stars = new List<Image>();
            var texts = new List<TMP_Text>();
            for (int i = 0; i < 3; i++)
            {
                float y = -22f - i * 44f;
                stars.Add(Image(goals, $"Star {i + 1}", MemoryCardsArtBuilder.Kenney("StarEmpty"), Color.white, new Vector2(0f, 1f), Center, new Vector2(22f, y), new Vector2(40f, 38f)));
                texts.Add(Text(goals, $"Goal {i + 1}", "Goal", 25f, Ink, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(56f, y), new Vector2(610f, 44f)));
            }
            ui.detailStars = stars.ToArray();
            ui.detailGoals = texts.ToArray();
            ui.detailBest = Text(panel, "Best", "Best score", 22f, SoftInk, TextAlignmentOptions.Left, new Vector2(0f, 0f), new Vector2(0f, 0.5f), new Vector2(40f, 140f), new Vector2(660f, 32f));
            ui.detailBest.enableAutoSizing = true;
            ui.detailBest.fontSizeMin = 14f;
            ui.detailBest.fontSizeMax = 22f;
            ui.playButton = Button(panel, "Play", MemoryCardsArtBuilder.Kenney("ButtonGreen"), "PLAY", MemoryCardsArtBuilder.Icon("Play"), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(420f, 104f), 50f, Color.white, out TextMeshProUGUI playLabel);
            playLabel.fontSharedMaterial = outline;
            ui.playLabel = playLabel;
        }

        #endregion

        #region HUD

        private static void BuildHud(Transform parent, MemoryCardsUI ui)
        {
            CanvasGroup screen = Screen(parent, "Hud");
            ui.hudScreen = screen;
            Transform root = UIBuildUtils.CreateSafeArea(screen.transform);

            // Level and mode, top left.
            RectTransform level = Rect(root, "Level", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(26f, -22f), new Vector2(560f, 112f));
            Image levelPanel = Image(level, "Panel", MemoryCardsArtBuilder.Ui("PanelDepth"), new Color(1f, 1f, 1f, 0.94f), Vector2.zero, Center, Vector2.zero, Vector2.zero, true);
            StretchRect(levelPanel.rectTransform);
            ui.modeIcon = Image(level, "ModeIcon", MemoryCardsArtBuilder.Icon("Cards"), SoftInk, new Vector2(0f, 0.5f), Center, new Vector2(58f, 6f), new Vector2(60f, 60f));
            ui.levelText = Text(level, "Title", "1. Hello, Farm!", 30f, Ink, TextAlignmentOptions.Left, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(104f, 22f), new Vector2(430f, 44f));
            ui.levelText.enableAutoSizing = true;
            ui.levelText.fontSizeMin = 18f;
            ui.levelText.fontSizeMax = 30f;
            ui.modeText = Text(level, "Mode", "CLASSIC", 22f, SoftInk, TextAlignmentOptions.Left, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(104f, -16f), new Vector2(430f, 34f));

            // Score, combo and sets or parade, top centre.
            Text(root, "ScoreLabel", "SCORE", 24f, Color.white, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), Center, new Vector2(0f, -30f), new Vector2(300f, 36f), outline);
            ui.scoreText = Text(root, "Score", "0", 64f, Color.white, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), Center, new Vector2(0f, -84f), new Vector2(420f, 80f), title);
            ui.comboBadge = Rect(root, "Combo", new Vector2(0.5f, 1f), Center, new Vector2(214f, -80f), new Vector2(118f, 62f));
            Image comboPill = Image(ui.comboBadge, "Pill", MemoryCardsArtBuilder.Ui("Pill"), Gold, Vector2.zero, Center, Vector2.zero, Vector2.zero, true);
            StretchRect(comboPill.rectTransform);
            ui.comboText = Text(ui.comboBadge, "Text", "x2", 38f, Color.white, TextAlignmentOptions.Center, Center, Center, new Vector2(0f, 2f), new Vector2(110f, 56f), outline);
            ui.comboBadge.gameObject.SetActive(false);

            RectTransform sets = Rect(root, "Sets", new Vector2(0.5f, 1f), Center, new Vector2(0f, -150f), new Vector2(200f, 54f));
            Image setsPill = Image(sets, "Pill", MemoryCardsArtBuilder.Ui("Pill"), new Color(0.1f, 0.1f, 0.22f, 0.45f), Vector2.zero, Center, Vector2.zero, Vector2.zero, true);
            StretchRect(setsPill.rectTransform);
            Image(sets, "Icon", MemoryCardsArtBuilder.Icon("Cards"), Color.white, new Vector2(0f, 0.5f), Center, new Vector2(34f, 0f), new Vector2(36f, 36f));
            ui.setsText = Text(sets, "Text", "0/8", 32f, Color.white, TextAlignmentOptions.Center, Center, Center, new Vector2(18f, 1f), new Vector2(140f, 50f), outline);
            ui.setsRoot = sets.gameObject;

            RectTransform parade = Rect(root, "Parade", new Vector2(0.5f, 1f), Center, new Vector2(0f, -150f), new Vector2(430f, 92f));
            Image paradePill = Image(parade, "Pill", MemoryCardsArtBuilder.Ui("Pill"), new Color(0.1f, 0.1f, 0.22f, 0.5f), Vector2.zero, Center, Vector2.zero, Vector2.zero, true);
            StretchRect(paradePill.rectTransform);
            Text(parade, "Label", "NEXT", 24f, Color.white, TextAlignmentOptions.Center, new Vector2(0f, 0.5f), Center, new Vector2(52f, 0f), new Vector2(90f, 40f), outline);
            var slots = new List<Image>();
            float x = 124f;
            for (int i = 0; i < 4; i++)
            {
                float size = i == 0 ? 80f : 62f;
                Image disc = Image(parade, $"Slot {i + 1}", MemoryCardsArtBuilder.Ui("Disc"), i == 0 ? Gold : new Color(1f, 1f, 1f, 0.85f), new Vector2(0f, 0.5f), Center, new Vector2(x + size * 0.5f, 0f), new Vector2(size, size));
                Image animal = Image(disc.transform, "Animal", MemoryCardsArtBuilder.Animal("pig"), Color.white, Center, Center, Vector2.zero, new Vector2(size * 0.86f, size * 0.86f));
                animal.preserveAspect = true;
                slots.Add(animal);
                x += size + 12f;
            }
            ui.paradeSlots = slots.ToArray();
            ui.paradeRoot = parade.gameObject;
            parade.gameObject.SetActive(false);

            // Timer, hearts, moves and pause, top right.
            ui.pauseButton = RoundButton(root, "Pause", MemoryCardsArtBuilder.Kenney("RoundYellow"), MemoryCardsArtBuilder.Icon("Pause"), Ink, new Vector2(1f, 1f), new Vector2(-70f, -72f), 104f);
            RectTransform timer = Rect(root, "Timer", new Vector2(1f, 1f), Center, new Vector2(-270f, -72f), new Vector2(236f, 80f));
            Image timerPill = Image(timer, "Pill", MemoryCardsArtBuilder.Ui("Pill"), new Color(0.1f, 0.1f, 0.22f, 0.5f), Vector2.zero, Center, Vector2.zero, Vector2.zero, true);
            StretchRect(timerPill.rectTransform);
            ui.timerIcon = Image(timer, "Icon", MemoryCardsArtBuilder.Icon("Stopwatch"), Color.white, new Vector2(0f, 0.5f), Center, new Vector2(44f, 2f), new Vector2(50f, 50f));
            ui.timerText = Text(timer, "Text", "0:00", 44f, Color.white, TextAlignmentOptions.Center, Center, Center, new Vector2(26f, 1f), new Vector2(160f, 70f), outline);
            ui.timerRoot = timer.gameObject;

            RectTransform hearts = Rect(root, "Hearts", new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-40f, -154f), new Vector2(420f, 60f));
            var heartImages = new List<Image>();
            for (int i = 0; i < 8; i++)
            {
                heartImages.Add(Image(hearts, $"Heart {i + 1}", MemoryCardsArtBuilder.Icon("HeartFull"), Color.white, new Vector2(1f, 0.5f), Center, new Vector2(-26f - i * 50f, 0f), new Vector2(52f, 52f)));
            }
            ui.hearts = heartImages.ToArray();
            ui.heartsRoot = hearts.gameObject;

            RectTransform moves = Rect(root, "Moves", new Vector2(1f, 1f), Center, new Vector2(-526f, -72f), new Vector2(236f, 76f));
            Image movesPill = Image(moves, "Pill", MemoryCardsArtBuilder.Ui("Pill"), new Color(0.1f, 0.1f, 0.22f, 0.5f), Vector2.zero, Center, Vector2.zero, Vector2.zero, true);
            StretchRect(movesPill.rectTransform);
            Image(moves, "Icon", MemoryCardsArtBuilder.Icon("Moves"), Color.white, new Vector2(0f, 0.5f), Center, new Vector2(42f, 0f), new Vector2(46f, 46f));
            ui.movesText = Text(moves, "Text", "20", 38f, Color.white, TextAlignmentOptions.Center, Center, Center, new Vector2(26f, 1f), new Vector2(150f, 60f), outline);
            Text(moves, "Label", "moves", 18f, new Color(1f, 1f, 1f, 0.8f), TextAlignmentOptions.Center, new Vector2(0.5f, 0f), Center, new Vector2(26f, -10f), new Vector2(150f, 24f), outline);
            ui.movesRoot = moves.gameObject;
        }

        private static void BuildOverlays(Transform root, MemoryCardsUI ui)
        {
            RectTransform popups = Stretch(root, "Popups");
            ui.popupRoot = popups;
            ui.popupTemplate = Text(popups, "Popup", "+100", 46f, Gold, TextAlignmentOptions.Center, Center, Center, Vector2.zero, new Vector2(500f, 90f), outline);
            ui.popupTemplate.gameObject.SetActive(false);

            RectTransform banner = Rect(root, "Banner", Center, Center, new Vector2(0f, 40f), new Vector2(1600f, 240f));
            ui.bannerGroup = banner.gameObject.AddComponent<CanvasGroup>();
            ui.bannerGroup.blocksRaycasts = false;
            ui.bannerGroup.interactable = false;
            ui.bannerText = Text(banner, "Text", "GO!", 110f, Color.white, TextAlignmentOptions.Center, Center, Center, new Vector2(0f, 28f), new Vector2(1600f, 150f), title);
            ui.bannerSub = Text(banner, "Sub", string.Empty, 40f, Color.white, TextAlignmentOptions.Center, Center, Center, new Vector2(0f, -72f), new Vector2(1500f, 60f), outline);

            RectTransform memorize = Rect(root, "Memorize", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(760f, 58f));
            Image track = Image(memorize, "Track", MemoryCardsArtBuilder.Ui("Pill"), new Color(0.1f, 0.1f, 0.22f, 0.6f), Vector2.zero, Center, Vector2.zero, Vector2.zero, true);
            StretchRect(track.rectTransform);
            ui.memorizeFill = Image(memorize, "Fill", MemoryCardsArtBuilder.Ui("Pill"), Gold, Vector2.zero, Center, Vector2.zero, Vector2.zero);
            StretchRect(ui.memorizeFill.rectTransform, 6f, 6f, 6f, 6f);
            Text(memorize, "Label", "MEMORIZE!", 30f, Color.white, TextAlignmentOptions.Center, Center, Center, new Vector2(0f, 1f), new Vector2(700f, 50f), outline);
            ui.memorizeFill.type = UnityEngine.UI.Image.Type.Filled;
            ui.memorizeFill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            ui.memorizeFill.fillOrigin = 0;
            ui.memorizeRoot = memorize.gameObject;

            RectTransform tip = Rect(root, "Tip", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(1500f, 62f));
            ui.tipGroup = tip.gameObject.AddComponent<CanvasGroup>();
            ui.tipGroup.blocksRaycasts = false;
            Image tipPill = Image(tip, "Pill", MemoryCardsArtBuilder.Ui("Pill"), new Color(0.1f, 0.1f, 0.22f, 0.72f), Vector2.zero, Center, Vector2.zero, Vector2.zero, true);
            StretchRect(tipPill.rectTransform);
            Image(tip, "Icon", MemoryCardsArtBuilder.Icon("Paw"), Gold, new Vector2(0f, 0.5f), Center, new Vector2(44f, 0f), new Vector2(40f, 40f));
            ui.tipText = Text(tip, "Text", "Tip", 26f, Color.white, TextAlignmentOptions.Center, Center, Center, new Vector2(24f, 1f), new Vector2(1380f, 56f), shadow);
            ui.tipText.textWrappingMode = TextWrappingModes.NoWrap;
            ui.tipText.enableAutoSizing = true;
            ui.tipText.fontSizeMin = 16f;
            ui.tipText.fontSizeMax = 26f;

            ui.flash = Image(root, "Flash", null, new Color(1f, 1f, 1f, 0f), Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            StretchRect(ui.flash.rectTransform);
        }

        #endregion

        #region Results

        private static void BuildResults(Transform parent, MemoryCardsUI ui)
        {
            CanvasGroup screen = Screen(parent, "Results");
            ui.resultsScreen = screen;
            Transform root = screen.transform;
            Image dim = Image(root, "Dim", null, Dim, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, false, true);
            StretchRect(dim.rectTransform);
            root = UIBuildUtils.CreateSafeArea(root);

            RectTransform panel = Rect(root, "Panel", Center, Center, new Vector2(0f, -20f), new Vector2(940f, 800f));
            ui.resultPanel = panel;
            Image background = Image(panel, "Background", MemoryCardsArtBuilder.Ui("PanelDepth"), Color.white, Vector2.zero, Center, Vector2.zero, Vector2.zero, true, true);
            StretchRect(background.rectTransform);
            ui.resultHeader = Image(panel, "Ribbon", MemoryCardsArtBuilder.Ui("Pill"), Shapes.Hex("#FF9A1F"), new Vector2(0.5f, 1f), Center, new Vector2(0f, 6f), new Vector2(720f, 118f), true);
            ui.resultTitle = Text(ui.resultHeader.transform, "Title", "GREAT JOB!", 70f, Color.white, TextAlignmentOptions.Center, Center, Center, new Vector2(0f, 4f), new Vector2(690f, 110f), title);
            ui.resultTitle.enableAutoSizing = true;
            ui.resultTitle.fontSizeMin = 40f;
            ui.resultTitle.fontSizeMax = 70f;
            ui.resultSubtitle = Text(panel, "Subtitle", "1. Hello, Farm!", 34f, SoftInk, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), Center, new Vector2(0f, -94f), new Vector2(860f, 50f));

            RectTransform stars = Rect(panel, "Stars", new Vector2(0.5f, 1f), Center, new Vector2(0f, -208f), new Vector2(600f, 180f));
            ui.resultStarsRoot = stars;
            ui.resultStars = new[]
            {
                Image(stars, "Star 1", MemoryCardsArtBuilder.Kenney("StarEmpty"), Color.white, Center, Center, new Vector2(-190f, -14f), new Vector2(150f, 141f)),
                Image(stars, "Star 2", MemoryCardsArtBuilder.Kenney("StarEmpty"), Color.white, Center, Center, new Vector2(0f, 12f), new Vector2(190f, 178f)),
                Image(stars, "Star 3", MemoryCardsArtBuilder.Kenney("StarEmpty"), Color.white, Center, Center, new Vector2(190f, -14f), new Vector2(150f, 141f))
            };

            ui.resultStats = Text(panel, "Stats", "Score", 30f, Ink, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -318f), new Vector2(860f, 150f));
            ui.resultStats.lineSpacing = 8f;

            RectTransform goals = Rect(panel, "Goals", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -478f), new Vector2(620f, 150f));
            var goalTexts = new List<TMP_Text>();
            var goalIcons = new List<Image>();
            for (int i = 0; i < 3; i++)
            {
                float y = -22f - i * 46f;
                goalIcons.Add(Image(goals, $"Icon {i + 1}", MemoryCardsArtBuilder.Kenney("GoalDone"), Color.white, new Vector2(0f, 1f), Center, new Vector2(22f, y), new Vector2(38f, 36f)));
                goalTexts.Add(Text(goals, $"Goal {i + 1}", "Goal", 26f, Ink, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(56f, y), new Vector2(560f, 42f)));
            }
            ui.resultGoals = goalTexts.ToArray();
            ui.resultGoalIcons = goalIcons.ToArray();
            ui.resultBest = Text(panel, "Best", "Best", 25f, Shapes.Hex("#C96A00"), TextAlignmentOptions.Center, new Vector2(0.5f, 0f), Center, new Vector2(0f, 150f), new Vector2(860f, 40f));
            ui.resultBest.enableAutoSizing = true;
            ui.resultBest.fontSizeMin = 16f;
            ui.resultBest.fontSizeMax = 25f;

            ui.levelsButton = RoundButton(panel, "Levels", MemoryCardsArtBuilder.Kenney("RoundGrey"), MemoryCardsArtBuilder.Icon("Levels"), Ink, new Vector2(0.5f, 0f), new Vector2(-250f, 76f), 116f);
            ui.retryButton = RoundButton(panel, "Retry", MemoryCardsArtBuilder.Kenney("RoundYellow"), MemoryCardsArtBuilder.Icon("Retry"), Ink, new Vector2(0.5f, 0f), new Vector2(-110f, 76f), 116f);
            ui.nextButton = Button(panel, "Next", MemoryCardsArtBuilder.Kenney("ButtonGreen"), "NEXT", MemoryCardsArtBuilder.Icon("Next"), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f),
                new Vector2(170f, 80f), new Vector2(330f, 112f), 46f, Color.white, out TextMeshProUGUI nextLabel);
            nextLabel.fontSharedMaterial = outline;
        }

        #endregion

        #region Pause menu and settings

        private static void BuildMenu(Transform root, MemoryCardsUI ui, MemoryCardsGameManager manager, MemoryCardsController controller, CanvasScaler scaler)
        {
            RectTransform menu = Stretch(root, "Menu");
            Image dim = menu.gameObject.AddComponent<Image>();
            dim.color = Dim;
            dim.raycastTarget = true;

            // The pause panel is the base menu's button group: it hides while the settings panel shows.
            RectTransform pause = Rect(menu, "PausePanel", Center, Center, new Vector2(0f, -10f), new Vector2(580f, 860f));
            Image pausePanel = Image(pause, "Background", MemoryCardsArtBuilder.Ui("PanelDepth"), Color.white, Vector2.zero, Center, Vector2.zero, Vector2.zero, true, true);
            StretchRect(pausePanel.rectTransform);
            Image ribbon = Image(pause, "Ribbon", MemoryCardsArtBuilder.Ui("Pill"), Shapes.Hex("#8E7CFF"), new Vector2(0.5f, 1f), Center, new Vector2(0f, 4f), new Vector2(420f, 104f), true);
            Text(ribbon.transform, "Title", "PAUSED", 58f, Color.white, TextAlignmentOptions.Center, Center, Center, new Vector2(0f, 3f), new Vector2(400f, 96f), title);
            var buttons = new (string name, string label, string sprite, string icon)[]
            {
                ("ReturnToGame", "RESUME", "ButtonGreen", "Play"),
                ("StartNewGame", "RESTART", "ButtonYellow", "Retry"),
                ("SaveGame", "SAVE GAME", "ButtonBlue", null),
                ("LoadGame", "LOAD GAME", "ButtonBlue", null),
                ("Game Settings", "SETTINGS", "ButtonBlue", "Settings"),
                ("LevelSelect", "LEVELS", "ButtonGrey", "Levels"),
                ("ExitGame", "QUIT", "ButtonRed", "Power")
            };
            var created = new Dictionary<string, Button>();
            for (int i = 0; i < buttons.Length; i++)
            {
                (string name, string label, string sprite, string icon) = buttons[i];
                Color labelColor = sprite == "ButtonYellow" || sprite == "ButtonGrey" ? Ink : Color.white;
                Button button = Button(pause, name, MemoryCardsArtBuilder.Kenney(sprite), label, icon != null ? MemoryCardsArtBuilder.Icon(icon) : null,
                    new Vector2(0.5f, 1f), Center, new Vector2(0f, -128f - i * 100f), new Vector2(420f, 88f), 34f, labelColor, out TextMeshProUGUI text);
                if (labelColor == Color.white)
                {
                    text.fontSharedMaterial = outline;
                }
                created[name] = button;
            }
            created["LoadGame"].interactable = false;
            ui.levelSelectButton = created["LevelSelect"];
            TextMeshProUGUI error = Text(root, "ErrorText", string.Empty, 30f, Color.white, TextAlignmentOptions.Center, new Vector2(0.5f, 0f), Center, new Vector2(0f, 128f), new Vector2(1400f, 60f), outline);
            error.raycastTarget = false;

            MemoryCardsSettingsUI settings = BuildSettings(menu);

            MemoryCardsAssets.SetObject(ui, "Controller", controller);
            MemoryCardsAssets.SetObject(ui, "Manager", manager);
            MemoryCardsAssets.SetObject(ui, "MenuRoot", menu);
            MemoryCardsAssets.SetObject(ui, "MenuButtonsUI", pause);
            MemoryCardsAssets.SetObject(ui, "Scaler", scaler);
            MemoryCardsAssets.SetObject(ui, "ErrorConsole", error);
            MemoryCardsAssets.SetObject(ui, "StartNewGame", created["StartNewGame"]);
            MemoryCardsAssets.SetObject(ui, "ReturnToGame", created["ReturnToGame"]);
            MemoryCardsAssets.SetObject(ui, "SaveGame", created["SaveGame"]);
            MemoryCardsAssets.SetObject(ui, "LoadGame", created["LoadGame"]);
            MemoryCardsAssets.SetObject(ui, "SettingsButton", created["Game Settings"]);
            MemoryCardsAssets.SetObject(ui, "ExitButton", created["ExitGame"]);
            MemoryCardsAssets.SetObject(ui, "GameSettingsUI", settings);
            MemoryCardsAssets.SetObject(settings, "OwnerUI", ui);
            MemoryCardsAssets.SetObject(settings, "DefaultSettings", MemoryCardsContentBuilder.DefaultSettings);
            settings.gameObject.SetActive(false);
            menu.gameObject.SetActive(false);
        }

        private static MemoryCardsSettingsUI BuildSettings(Transform menu)
        {
            RectTransform panel = Rect(menu, "SettingsPanel", Center, Center, new Vector2(0f, -10f), new Vector2(1120f, 860f));
            var settings = panel.gameObject.AddComponent<MemoryCardsSettingsUI>();
            Image background = Image(panel, "Background", MemoryCardsArtBuilder.Ui("PanelDepth"), Color.white, Vector2.zero, Center, Vector2.zero, Vector2.zero, true, true);
            StretchRect(background.rectTransform);
            Image ribbon = Image(panel, "Ribbon", MemoryCardsArtBuilder.Ui("Pill"), Shapes.Hex("#1FC2FF"), new Vector2(0.5f, 1f), Center, new Vector2(0f, 4f), new Vector2(620f, 104f), true);
            Text(ribbon.transform, "Title", "FREE PLAY RULES", 50f, Color.white, TextAlignmentOptions.Center, Center, Center, new Vector2(0f, 3f), new Vector2(600f, 96f), title);
            Text(panel, "Subtitle", "Free Play in the Critter Carnival deals its board with these rules. Use 0 to switch a limit off.", 24f, SoftInk,
                TextAlignmentOptions.Center, new Vector2(0.5f, 1f), Center, new Vector2(0f, -92f), new Vector2(1040f, 40f));

            var fields = new (string field, string label, TMP_InputField.ContentType type)[]
            {
                ("CardsAmount", "Cards", TMP_InputField.ContentType.IntegerNumber),
                ("CardsPerRow", "Cards per row", TMP_InputField.ContentType.IntegerNumber),
                ("FlippedCardsPerMatch", "Cards per match", TMP_InputField.ContentType.IntegerNumber),
                ("TimePerGame", "Time limit (s)", TMP_InputField.ContentType.DecimalNumber),
                ("TimeUntilUnflip", "Unflip delay (s)", TMP_InputField.ContentType.DecimalNumber),
                ("PreviewTime", "Memorize time (s)", TMP_InputField.ContentType.DecimalNumber),
                ("Hearts", "Hearts", TMP_InputField.ContentType.IntegerNumber),
                ("MoveLimit", "Move limit", TMP_InputField.ContentType.IntegerNumber),
                ("ShuffleEvery", "Shuffle after mistakes", TMP_InputField.ContentType.IntegerNumber),
                ("Bombs", "Bomb cards", TMP_InputField.ContentType.IntegerNumber),
                ("Wilds", "Wild cards", TMP_InputField.ContentType.IntegerNumber),
                ("Clocks", "Clock cards", TMP_InputField.ContentType.IntegerNumber),
                ("Peeks", "Peek cards", TMP_InputField.ContentType.IntegerNumber),
                ("Frozen", "Frozen cards", TMP_InputField.ContentType.IntegerNumber)
            };
            for (int i = 0; i < fields.Length; i++)
            {
                int column = i / 7;
                int row = i % 7;
                var position = new Vector2(column == 0 ? -270f : 270f, 250f - row * 76f);
                TMP_InputField input = Field(panel, fields[i].field, fields[i].label, fields[i].type, position);
                MemoryCardsAssets.SetObject(settings, fields[i].field, input);
            }

            Button custom = Button(panel, "Custom Settings", MemoryCardsArtBuilder.Kenney("ButtonYellow"), "Use Custom Rules", null, new Vector2(0.5f, 0f), Center,
                new Vector2(-330f, 84f), new Vector2(360f, 92f), 28f, Ink, out TextMeshProUGUI customLabel);
            Button save = Button(panel, "SaveSettingsButton", MemoryCardsArtBuilder.Kenney("ButtonGreen"), "SAVE", null, new Vector2(0.5f, 0f), Center,
                new Vector2(-30f, 84f), new Vector2(200f, 92f), 30f, Color.white, out TextMeshProUGUI saveLabel);
            saveLabel.fontSharedMaterial = outline;
            Button load = Button(panel, "LoadSettingsButton", MemoryCardsArtBuilder.Kenney("ButtonBlue"), "LOAD", null, new Vector2(0.5f, 0f), Center,
                new Vector2(190f, 84f), new Vector2(200f, 92f), 30f, Color.white, out TextMeshProUGUI loadLabel);
            loadLabel.fontSharedMaterial = outline;
            Button back = Button(panel, "BackToMenu", MemoryCardsArtBuilder.Kenney("ButtonGrey"), "BACK", MemoryCardsArtBuilder.Icon("Back"), new Vector2(0.5f, 0f), Center,
                new Vector2(410f, 84f), new Vector2(200f, 92f), 30f, Ink, out _);
            MemoryCardsAssets.SetObject(settings, "CustomSettingsButtonText", customLabel);
            MemoryCardsAssets.SetObject(settings, "CustomSettingsButton", custom);
            MemoryCardsAssets.SetObject(settings, "SaveSettingsButton", save);
            MemoryCardsAssets.SetObject(settings, "LoadSettingsButton", load);
            MemoryCardsAssets.SetObject(settings, "BackButton", back);
            return settings;
        }

        private static TMP_InputField Field(Transform parent, string name, string label, TMP_InputField.ContentType type, Vector2 position)
        {
            RectTransform row = Rect(parent, name, Center, Center, position, new Vector2(500f, 64f));
            Text(row, "Label", label, 25f, Ink, TextAlignmentOptions.Left, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(320f, 56f));
            GameObject fieldObject = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources());
            fieldObject.name = "Input";
            var fieldRect = (RectTransform)fieldObject.transform;
            fieldRect.SetParent(row, false);
            fieldRect.anchorMin = fieldRect.anchorMax = new Vector2(1f, 0.5f);
            fieldRect.pivot = new Vector2(1f, 0.5f);
            fieldRect.anchoredPosition = Vector2.zero;
            fieldRect.sizeDelta = new Vector2(160f, 60f);
            SetLayer(fieldObject);
            var image = fieldObject.GetComponent<Image>();
            image.sprite = MemoryCardsArtBuilder.Kenney("InputField");
            image.type = UnityEngine.UI.Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 2f;
            var input = fieldObject.GetComponent<TMP_InputField>();
            input.contentType = type;
            input.characterLimit = 6;
            input.pointSize = 28f;
            foreach (TMP_Text text in fieldObject.GetComponentsInChildren<TMP_Text>(true))
            {
                text.font = font;
                text.fontSize = 28f;
                text.alignment = TextAlignmentOptions.Center;
                text.color = text == input.placeholder ? new Color(0.4f, 0.42f, 0.5f, 0.5f) : Ink;
            }
            if (input.placeholder is TMP_Text placeholder)
            {
                placeholder.text = "0";
            }
            input.fontAsset = font;
            return input;
        }

        #endregion

        #region Helpers

        private static CanvasGroup Screen(Transform parent, string name)
        {
            RectTransform rect = Stretch(parent, name);
            return rect.gameObject.AddComponent<CanvasGroup>();
        }

        private static RectTransform Rect(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static RectTransform Stretch(Transform parent, string name, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
        {
            RectTransform rect = Rect(parent, name, Vector2.zero, Center, Vector2.zero, Vector2.zero);
            StretchRect(rect, left, bottom, right, top);
            return rect;
        }

        private static void StretchRect(RectTransform rect, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = Center;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void Anchor(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = Center;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Image Image(Transform parent, string name, Sprite sprite, Color color, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size,
            bool sliced = false, bool raycast = false)
        {
            RectTransform rect = Rect(parent, name, anchor, pivot, position, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = raycast;
            if (sliced)
            {
                image.type = UnityEngine.UI.Image.Type.Sliced;
            }
            return image;
        }

        private static TextMeshProUGUI Text(Transform parent, string name, string text, float size, Color color, TextAlignmentOptions alignment,
            Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 box, Material material = null)
        {
            RectTransform rect = Rect(parent, name, anchor, pivot, position, box);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            if (material != null)
            {
                label.fontSharedMaterial = material;
            }
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Overflow;
            return label;
        }

        /// <summary>A Kenney button with a label and an optional icon in front of it.</summary>
        private static Button Button(Transform parent, string name, Sprite background, string label, Sprite icon, Vector2 anchor, Vector2 pivot, Vector2 position,
            Vector2 size, float fontSize, Color labelColor, out TextMeshProUGUI text)
        {
            RectTransform rect = Rect(parent, name, anchor, pivot, position, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = background;
            image.type = UnityEngine.UI.Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1.4f;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.colors = ButtonColors();
            rect.gameObject.AddComponent<PressScale>();
            float iconSize = size.y * 0.5f;
            float offset = icon != null ? iconSize * 0.45f : 0f;
            if (icon != null)
            {
                Image(rect, "Icon", icon, labelColor, new Vector2(0.5f, 0.5f), Center, new Vector2(-size.x * 0.5f + iconSize * 0.9f, size.y * 0.04f), new Vector2(iconSize, iconSize));
            }
            text = Text(rect, "Label", label, fontSize, labelColor, TextAlignmentOptions.Center, Center, Center, new Vector2(offset, size.y * 0.05f),
                new Vector2(size.x - offset * 2f - 30f, size.y));
            text.enableAutoSizing = true;
            text.fontSizeMin = fontSize * 0.55f;
            text.fontSizeMax = fontSize;
            return button;
        }

        private static Button RoundButton(Transform parent, string name, Sprite background, Sprite icon, Color iconColor, Vector2 anchor, Vector2 position, float size)
        {
            RectTransform rect = Rect(parent, name, anchor, Center, position, new Vector2(size, size));
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = background;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.colors = ButtonColors();
            rect.gameObject.AddComponent<PressScale>();
            Image(rect, "Icon", icon, iconColor, Center, Center, new Vector2(0f, size * 0.04f), new Vector2(size * 0.5f, size * 0.5f));
            return button;
        }

        private static Button TextButton(Transform parent, string name, string label, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size, out TextMeshProUGUI text)
        {
            RectTransform rect = Rect(parent, name, anchor, pivot, position, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            text = Text(rect, "Label", label, 22f, Color.white, TextAlignmentOptions.Left, Center, Center, Vector2.zero, size, outline);
            text.fontStyle = FontStyles.Underline;
            return button;
        }

        private static ColorBlock ButtonColors()
        {
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.05f, 1.05f, 1.05f);
            colors.pressedColor = new Color(0.86f, 0.86f, 0.9f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.62f, 0.62f, 0.66f, 0.7f);
            colors.fadeDuration = 0.08f;
            return colors;
        }

        private static void SetLayer(GameObject go)
        {
            go.layer = 5;
            foreach (Transform child in go.transform)
            {
                SetLayer(child.gameObject);
            }
        }

        #endregion
    }
}
