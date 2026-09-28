using System;
using System.Collections.Generic;
using System.Linq;
using Gamebox;
using Gamebox.Editor;
using Gamebox.UI;
using Portfolio.MemoryCards.Server;
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
    /// <see cref="GameUI"/> and <see cref="SettingsUI"/> fields), particles, popups and audio, and online play (the
    /// server client, the online controller and the shared lobby, through <see cref="OnlineInstaller"/>). The scene is
    /// laid out in the look of the Classic <see cref="MemoryCardsTheme"/>: every sprite, font, material and colour is
    /// read from that asset, and the parts that show them carry a <see cref="ThemedImage"/> or a
    /// <see cref="ThemedLabel"/> with the key of what they show ("kit.primary", "palette.ink"), so the active theme
    /// re-skins them at run time. Parts whose sprite or colour the game sets while it runs (a world's card back, a
    /// seat's colour) carry no key for it.
    /// </summary>
    internal static class MemoryCardsSceneBuilder
    {
        public const string ScenePath = "Scenes/MemoryCards.unity";
        /// <summary>The name the module of Server/ is published under (spacetime.json of the game's project).</summary>
        public const string Database = "skinnerboxes-memorycards";
        public const string CardPrefabPath = "Prefabs/Card.prefab";

        private const string InkKey = "palette.ink";
        private const string SoftInkKey = "palette.softInk";
        private const string LightKey = "palette.light";
        private const string GoldKey = "palette.gold";
        private const string ButtonTextKey = "palette.buttonText";
        private const string AccentTextKey = "palette.accentText";
        private const string NeutralTextKey = "palette.neutralText";
        private const string PanelKey = "palette.panel";
        private const string PillKey = "palette.pill";
        private const string DimKey = "palette.dim";
        private const string OutlineKey = "text.outline";
        private const string ShadowKey = "text.shadow";
        private const string TitleKey = "text.title";

        private static readonly Vector2 Reference = new Vector2(1920f, 1080f);

        /// <summary>The theme the scene is laid out in (Classic).</summary>
        private static MemoryCardsTheme look;

        private static Vector2 Center => new Vector2(0.5f, 0.5f);

        public static void Build()
        {
            look = MemoryCardsContentBuilder.Theme(ThemeSpecs.Classic);
            if (look == null)
            {
                throw new InvalidOperationException("Memory Cards: the Classic theme is missing; run Rebuild Worlds and Levels first.");
            }
            GameObject cardPrefab = BuildCardPrefab();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildCamera();
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            MemoryCardsCampaign campaign = MemoryCardsContentBuilder.Campaign;
            CardWorld farm = look.World(0);

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

            ui.starFull = look.Hud.starFull;
            ui.starEmpty = look.Hud.starEmpty;
            ui.heartFull = look.Icons.heartFull;
            ui.heartEmpty = look.Icons.heartEmpty;
            ui.goalDone = look.Hud.goalDone;
            ui.goalMissed = look.Hud.goalMissed;
            ui.modeIcons = look.Hud.modeIcons.ToArray();

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
            // The theme wins at run time; these are the fallbacks of a scene without one.
            manager.defaultCardBack = farm != null ? farm.cardBack : look.Card.defaultBack;
            manager.specialFaces = new[] { look.Special.wild, look.Special.bomb, look.Special.clock, look.Special.peek };
            manager.goodColor = look.Colors.good;
            manager.badColor = look.Colors.bad;
            manager.gold = look.Colors.gold;
            // Only the sky of the first world: the floating shapes are made at run time (made here, they would stay put).
            if (farm != null)
            {
                backdrop.gradient.Top = farm.skyTop;
                backdrop.gradient.Bottom = farm.skyBottom;
            }

            // Online play: the scoreboard of the HUD shows whose turn it is, so the lobby's own banner stays off.
            manager.online = (MemoryCardsOnlineController)OnlineInstaller.Install(scene, typeof(GameServerClient), typeof(MemoryCardsOnlineController),
                manager, ui, Database, new OnlineInstaller.LobbyStyle
                {
                    Title = "PLAY ONLINE",
                    Font = look.Fonts.body,
                    Accent = look.Colors.lobbyAccent,
                    Window = look.Colors.lobbyWindow,
                    Row = look.Colors.lobbyRow,
                    HideTurnBanner = true
                });

            GameMenuInstaller.EnsureUrpCameras();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, MemoryCardsAssets.Path(ScenePath));
            GameSceneBuildSettings.Sync(true);
        }

        #region Card

        /// <summary>
        /// The card: shadow, a body that flips (back, front with the face and badge, ice) and a glow. Its sprites are
        /// the Classic ones; the manager re-skins every card from the active theme when it deals it.
        /// </summary>
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
            card.shadow = Image(rect, "Shadow", look.Card.shadow, Color.white, Vector2.zero, Center, Vector2.zero, Vector2.zero);
            Anchor(card.shadow.rectTransform, new Vector2(-padX, -padY), new Vector2(1f + padX, 1f + padY));

            card.body = Stretch(rect, "Body");
            card.back = Image(card.body, "Back", look.Card.defaultBack, Color.white, Vector2.zero, Center, Vector2.zero, Vector2.zero, false, true);
            StretchRect(card.back.rectTransform);
            card.front = Image(card.body, "Front", look.Card.front, Color.white, Vector2.zero, Center, Vector2.zero, Vector2.zero, false, true);
            StretchRect(card.front.rectTransform);
            card.face = Image(card.front.transform, "Face", look.Faces[0], Color.white, Vector2.zero, Center, Vector2.zero, Vector2.zero);
            Anchor(card.face.rectTransform, new Vector2(0.07f, 0.17f), new Vector2(0.93f, 0.83f));
            card.face.preserveAspect = true;
            card.badge = Image(card.front.transform, "Badge", look.Card.badge, Color.white, new Vector2(1f, 1f), Center, new Vector2(-24f, -24f), new Vector2(54f, 54f));
            card.ice = Image(card.body, "Ice", look.Card.ice, Color.white, Vector2.zero, Center, Vector2.zero, Vector2.zero);
            StretchRect(card.ice.rectTransform);
            card.iceSprite = look.Card.ice;
            card.crackedIceSprite = look.Card.crackedIce;
            card.glow = Image(card.body, "Glow", look.Card.glow, new Color(1f, 1f, 1f, 0f), Vector2.zero, Center, Vector2.zero, Vector2.zero);
            Anchor(card.glow.rectTransform, new Vector2(-padX, -padY), new Vector2(1f + padX, 1f + padY));
            card.matchGlow = look.Colors.matchGlow;
            card.mistakeTint = look.Colors.mistakeTint;
            card.wiltTint = look.Colors.wiltTint;
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
            camera.backgroundColor = look.Colors.sky;
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
            pattern.texture = look.Backdrop.pattern;
            pattern.color = look.Backdrop.patternColor;
            pattern.raycastTarget = false;
            var themedPattern = patternRect.gameObject.AddComponent<ThemedRawImage>();
            themedPattern.TextureKey = "backdrop.pattern";
            themedPattern.ThemedGame = GameType.MemoryCards;
            MemoryCardsAssets.Set(themedPattern, "color", p => p.stringValue = "backdrop.patternColor");
            backdrop.pattern = pattern;
            backdrop.patternTileSize = look.Backdrop.patternTileSize;
            backdrop.shapesRoot = Stretch(rect, "Shapes");
            backdrop.shapeTemplate = Image(backdrop.shapesRoot, "Shape", null, Color.white, Center, Center, Vector2.zero, new Vector2(100f, 100f));
            backdrop.shapeTemplate.gameObject.SetActive(false);
            Image vignette = Themed(rect, "Vignette", "backdrop.vignette", "backdrop.vignetteColor", Center, Center, new Vector2(0f, 60f), new Vector2(1900f, 1300f));
            vignette.raycastTarget = false;
            return backdrop;
        }

        private static UIParticles BuildParticles(Transform parent)
        {
            RectTransform rect = Stretch(parent, "Particles");
            var particles = rect.gameObject.AddComponent<UIParticles>();
            particles.template = Image(rect, "Particle", look.Particles.spark, Color.white, Center, Center, Vector2.zero, new Vector2(24f, 24f));
            particles.template.gameObject.SetActive(false);
            particles.spark = look.Particles.spark;
            particles.star = look.Particles.star;
            particles.circle = look.Particles.circle;
            particles.confetti = look.Particles.confetti;
            particles.shard = look.Particles.shard;
            return particles;
        }

        #endregion

        #region Level select

        private static void BuildTitle(Transform parent, MemoryCardsUI ui, MemoryCardsCampaign campaign)
        {
            CanvasGroup screen = Screen(parent, "Title");
            ui.titleScreen = screen;
            Transform root = UIBuildUtils.CreateSafeArea(screen.transform);

            // Logo with an ornament behind it and the theme's figures around it.
            RectTransform logo = Rect(root, "Logo", new Vector2(0.5f, 1f), Center, new Vector2(0f, -104f), new Vector2(1180f, 170f));
            ui.logo = logo;
            ui.logoOrnament = Image(logo, "Ornament", look.Title.ornament, look.Title.ornamentColor, Center, Center, new Vector2(0f, -47f), new Vector2(1100f, 150f));
            ui.logoOrnament.preserveAspect = false;
            TextMeshProUGUI name = Text(logo, "Name", look.Title.words, 104f, LightKey, TextAlignmentOptions.Center, Center, Center, new Vector2(0f, 18f), new Vector2(1180f, 120f), TitleKey, TextRole.Title);
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.enableAutoSizing = true;
            name.fontSizeMin = 60f;
            name.fontSizeMax = 104f;
            name.enableVertexGradient = true;
            name.colorGradient = new VertexGradient(look.Title.gradientTop, look.Title.gradientTop, look.Title.gradientBottom, look.Title.gradientBottom);
            name.characterSpacing = 4f;
            ui.logoName = name;
            ui.logoSubtitle = Text(logo, "Subtitle", look.Title.subtitle, 30f, LightKey, TextAlignmentOptions.Center, Center, Center, new Vector2(0f, -62f), new Vector2(900f, 44f), OutlineKey);
            var places = new[]
            {
                (new Vector2(-712f, -104f), 150f, -8f), (new Vector2(712f, -104f), 150f, 8f), (new Vector2(-868f, -150f), 92f, -14f), (new Vector2(868f, -150f), 92f, 12f)
            };
            var mascots = new List<RectTransform>();
            for (int i = 0; i < places.Length; i++)
            {
                Sprite figure = i < look.Title.mascots.Count ? look.Title.mascots[i] : null;
                mascots.Add(Mascot(root, i + 1, figure, places[i].Item1, places[i].Item2, places[i].Item3));
            }
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
            Image starsPanel = Themed(stars, "Panel", "kit.panelDepth", new Color(1f, 1f, 1f, 0.92f), Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, true);
            StretchRect(starsPanel.rectTransform);
            Themed(stars, "Star", "hud.starFull", Color.white, new Vector2(0f, 0.5f), Center, new Vector2(58f, 6f), new Vector2(70f, 66f));
            ui.starsTotal = Text(stars, "Total", "0 / 54", 44f, InkKey, TextAlignmentOptions.Left, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(106f, 18f), new Vector2(280f, 52f));
            ui.lifetimeText = Text(stars, "Lifetime", "Welcome!", 22f, SoftInkKey, TextAlignmentOptions.Left, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(108f, -22f), new Vector2(280f, 32f));
            ui.resetProgressButton = TextButton(root, "ResetProgress", "Reset progress", Vector2.zero, new Vector2(0f, 0f), new Vector2(452f, 30f), new Vector2(250f, 40f), out TextMeshProUGUI resetLabel);
            ui.resetProgressLabel = resetLabel;

            ui.continueButton = Button(root, "Continue", "kit.accent", "CONTINUE", "icons.play", new Vector2(1f, 0f),
                new Vector2(0.5f, 0f), new Vector2(-640f, 14f), new Vector2(330f, 94f), 34f, AccentTextKey, out _);
            // Several players: at this device (the button steps from one to four), or online.
            ui.playersButton = Button(root, "Players", "kit.secondary", "1 PLAYER", null, new Vector2(1f, 0f),
                new Vector2(0.5f, 0f), new Vector2(-1112f, 14f), new Vector2(200f, 94f), 28f, ButtonTextKey, out TextMeshProUGUI playersLabel);
            ui.playersLabel = playersLabel;
            playersLabel.textWrappingMode = TextWrappingModes.NoWrap;
            ui.onlineButton = Button(root, "Online", "kit.primary", "ONLINE", null, new Vector2(1f, 0f),
                new Vector2(0.5f, 0f), new Vector2(-908f, 14f), new Vector2(190f, 94f), 28f, ButtonTextKey, out _);
            ui.titleSettingsButton = RoundButton(root, "Settings", "kit.roundSecondary", "icons.settings", ButtonTextKey, new Vector2(1f, 0f), new Vector2(-190f, 70f), 104f);
            ui.titleExitButton = RoundButton(root, "Exit", "kit.roundDanger", "icons.power", ButtonTextKey, new Vector2(1f, 0f), new Vector2(-68f, 70f), 104f);
        }

        /// <summary>A figure of the theme beside the title; the interface swaps it when the theme changes.</summary>
        private static RectTransform Mascot(Transform parent, int index, Sprite figure, Vector2 position, float size, float angle)
        {
            Image image = Image(parent, $"Mascot {index}", figure, Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.2f), position - new Vector2(0f, size * 0.3f), new Vector2(size, size));
            image.preserveAspect = true;
            image.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
            image.gameObject.SetActive(figure != null);
            return image.rectTransform;
        }

        private static WorldTab BuildWorldTab(Transform parent, int index, Vector2 position, Vector2 size)
        {
            RectTransform rect = Rect(parent, $"World {index + 1}", Center, Center, position, size);
            var tab = rect.gameObject.AddComponent<WorldTab>();
            tab.content = Stretch(rect, "Content");
            tab.background = Themed(tab.content, "Background", "kit.tintPanel", Color.white, Vector2.zero, Center, Vector2.zero, Vector2.zero, true, true);
            StretchRect(tab.background.rectTransform);
            CardWorld first = look.World(0);
            tab.mascot = Image(tab.content, "Mascot", first != null ? first.mascot : null, Color.white, new Vector2(0f, 0.5f), Center, new Vector2(64f, 6f), new Vector2(92f, 92f));
            tab.mascot.preserveAspect = true;
            // The tab colours its words by its state; they come from the palette through WorldTab.ApplyLook.
            tab.title = Text(tab.content, "Title", "World", 27f, look.Colors.tabText, TextAlignmentOptions.Left, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(120f, 20f), new Vector2(190f, 44f));
            tab.title.textWrappingMode = TextWrappingModes.NoWrap;
            tab.title.enableAutoSizing = true;
            tab.title.fontSizeMin = 13f;
            tab.title.fontSizeMax = 26f;
            tab.starIcon = Themed(tab.content, "Star", "hud.starFull", Color.white, new Vector2(0f, 0.5f), Center, new Vector2(137f, -20f), new Vector2(32f, 30f));
            tab.subtitle = Text(tab.content, "Stars", "0/18", 23f, look.Colors.tabText, TextAlignmentOptions.Left, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(160f, -20f), new Vector2(150f, 36f));
            tab.subtitle.textWrappingMode = TextWrappingModes.NoWrap;
            tab.subtitle.enableAutoSizing = true;
            tab.subtitle.fontSizeMin = 13f;
            tab.subtitle.fontSizeMax = 23f;
            tab.lockIcon = Themed(tab.content, "Lock", "icons.padlock", LightKey, new Vector2(0f, 0.5f), Center, new Vector2(64f, 6f), new Vector2(44f, 44f));
            tab.textColor = look.Colors.tabText;
            tab.textSelected = look.Colors.tabTextSelected;
            tab.idleBlend = look.Colors.tabIdle;
            tab.lockedColor = look.Colors.tabLocked;
            tab.button = rect.gameObject.AddComponent<Button>();
            tab.button.targetGraphic = tab.background;
            tab.button.transition = Selectable.Transition.None;
            Springy(rect, tab.content);
            return tab;
        }

        private static LevelCard BuildLevelCard(Transform parent, int index)
        {
            RectTransform rect = Rect(parent, $"Level {index + 1}", Center, Center, Vector2.zero, new Vector2(184f, 244f));
            var card = rect.gameObject.AddComponent<LevelCard>();
            card.content = Stretch(rect, "Content");
            float padX = (float)MemoryCardsArt.CardPadding / MemoryCardsArt.CardWidth;
            float padY = (float)MemoryCardsArt.CardPadding / MemoryCardsArt.CardHeight;
            Image cardShadow = Themed(card.content, "Shadow", "card.shadow", Color.white, Vector2.zero, Center, Vector2.zero, Vector2.zero);
            Anchor(cardShadow.rectTransform, new Vector2(-padX, -padY - 0.02f), new Vector2(1f + padX, 1f + padY - 0.02f));
            card.outline = Themed(card.content, "Outline", "card.glow", Color.white, Vector2.zero, Center, Vector2.zero, Vector2.zero);
            Anchor(card.outline.rectTransform, new Vector2(-padX, -padY), new Vector2(1f + padX, 1f + padY));
            CardWorld first = look.World(0);
            card.back = Image(card.content, "Back", first != null ? first.levelCardBack : look.Card.defaultBack, Color.white, Vector2.zero, Center, Vector2.zero, Vector2.zero, false, true);
            StretchRect(card.back.rectTransform);
            Image plate = Themed(card.content, "Plate", "kit.disc", Color.white, Center, Center, new Vector2(0f, 30f), new Vector2(104f, 104f));
            plate.raycastTarget = false;
            card.number = Text(plate.transform, "Number", "1", 60f, look.Colors.ink, TextAlignmentOptions.Center, Center, Center, new Vector2(0f, 2f), new Vector2(84f, 70f), ShadowKey);
            card.number.textWrappingMode = TextWrappingModes.NoWrap;
            card.number.enableAutoSizing = true;
            card.number.fontSizeMin = 30f;
            card.number.fontSizeMax = 60f;
            card.icon = Image(plate.transform, "Icon", look.Icons.infinity, look.Colors.ink, Center, Center, Vector2.zero, new Vector2(72f, 72f));
            Image titlePlate = Themed(card.content, "TitlePlate", "kit.pill", new Color(1f, 1f, 1f, 0.95f), Center, Center, new Vector2(0f, -54f), new Vector2(166f, 44f), true);
            titlePlate.raycastTarget = false;
            card.title = Text(titlePlate.transform, "Title", "Level", 19f, InkKey, TextAlignmentOptions.Center, Center, Center, Vector2.zero, new Vector2(154f, 40f));
            card.title.enableAutoSizing = true;
            card.title.fontSizeMin = 12f;
            card.title.fontSizeMax = 19f;
            var stars = new List<Image>();
            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * 52f;
                float y = i == 1 ? -114f : -108f;
                stars.Add(Image(card.content, $"Star {i + 1}", look.Hud.starEmpty, Color.white, Center, Center, new Vector2(x, y), i == 1 ? new Vector2(54f, 51f) : new Vector2(46f, 43f)));
            }
            card.stars = stars.ToArray();
            RectTransform lockRoot = Stretch(card.content, "Lock");
            Image lockShade = Themed(lockRoot, "Shade", "card.front", "palette.lockShade", Vector2.zero, Center, Vector2.zero, Vector2.zero);
            StretchRect(lockShade.rectTransform);
            Themed(lockRoot, "Disc", "kit.disc", "palette.lockDisc", Center, Center, new Vector2(0f, 30f), new Vector2(96f, 96f));
            Themed(lockRoot, "Icon", "icons.padlock", LightKey, Center, Center, new Vector2(0f, 32f), new Vector2(58f, 58f));
            card.lockRoot = lockRoot.gameObject;
            card.newBadge = Themed(card.content, "New", "kit.tintPill", GoldKey, new Vector2(1f, 1f), Center, new Vector2(-12f, -6f), new Vector2(86f, 40f), true);
            Text(card.newBadge.transform, "Label", "NEW", 22f, AccentTextKey, TextAlignmentOptions.Center, Center, Center, new Vector2(0f, 1f), new Vector2(80f, 36f));
            card.newBadge.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -12f);
            card.endlessIcon = look.Icons.infinity;
            card.freePlayIcon = look.Icons.sliders;
            card.lockedTint = look.Colors.cardLocked;
            card.button = rect.gameObject.AddComponent<Button>();
            card.button.targetGraphic = card.back;
            card.button.transition = Selectable.Transition.None;
            Springy(rect, card.content.parent, 1.04f);
            return card;
        }

        private static void BuildDetails(Transform root, MemoryCardsUI ui)
        {
            RectTransform panel = Rect(root, "Details", new Vector2(1f, 0f), Center, new Vector2(-500f, 418f), new Vector2(740f, 600f));
            Image background = Themed(panel, "Panel", "kit.panelDepth", PanelKey, Vector2.zero, Center, Vector2.zero, Vector2.zero, true);
            StretchRect(background.rectTransform);
            // The header takes the colour of the selected world at run time.
            ui.detailHeader = Themed(panel, "Header", "kit.tintPill", look.Colors.detailsHeader, new Vector2(0.5f, 1f), Center, new Vector2(0f, -4f), new Vector2(520f, 62f), true);
            ui.detailWorld = Text(ui.detailHeader.transform, "World", "SUNNY FARM", 28f, LightKey, TextAlignmentOptions.Center, Center, Center, new Vector2(0f, 2f), new Vector2(500f, 56f), OutlineKey);
            ui.detailTitle = Text(panel, "Title", "1. Hello, Farm!", 46f, InkKey, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -50f), new Vector2(660f, 62f));
            ui.detailTitle.enableAutoSizing = true;
            ui.detailTitle.fontSizeMin = 28f;
            ui.detailTitle.fontSizeMax = 46f;
            ui.detailModeIcon = Image(panel, "ModeIcon", look.Icons.cards, look.Colors.softInk, new Vector2(0f, 1f), Center, new Vector2(62f, -138f), new Vector2(42f, 42f));
            ui.detailMode = Text(panel, "Mode", "CLASSIC", 26f, look.Colors.softInk, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(92f, -138f), new Vector2(300f, 40f));
            RectTransform newRoot = Rect(panel, "New", new Vector2(1f, 1f), Center, new Vector2(-186f, -138f), new Vector2(300f, 48f));
            Image newPill = Themed(newRoot, "Pill", "kit.tintPill", GoldKey, Vector2.zero, Center, Vector2.zero, Vector2.zero, true);
            StretchRect(newPill.rectTransform);
            ui.detailNew = Text(newRoot, "Label", "NEW: Memorize", 22f, AccentTextKey, TextAlignmentOptions.Center, Center, Center, new Vector2(0f, 1f), new Vector2(290f, 46f));
            ui.detailNew.enableAutoSizing = true;
            ui.detailNew.fontSizeMin = 14f;
            ui.detailNew.fontSizeMax = 22f;
            ui.detailNewRoot = newRoot.gameObject;
            ui.detailDescription = Text(panel, "Description", "Description", 26f, InkKey, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -170f), new Vector2(660f, 98f));
            ui.detailDescription.enableAutoSizing = true;
            ui.detailDescription.fontSizeMin = 18f;
            ui.detailDescription.fontSizeMax = 26f;
            ui.detailDescription.lineSpacing = -6f;
            ui.detailBoard = Text(panel, "Board", "4x3 board, 6 pairs", 20f, SoftInkKey, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -272f), new Vector2(660f, 32f));
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
                stars.Add(Image(goals, $"Star {i + 1}", look.Hud.starEmpty, Color.white, new Vector2(0f, 1f), Center, new Vector2(22f, y), new Vector2(40f, 38f)));
                texts.Add(Text(goals, $"Goal {i + 1}", "Goal", 25f, InkKey, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(56f, y), new Vector2(610f, 44f)));
            }
            ui.detailStars = stars.ToArray();
            ui.detailGoals = texts.ToArray();
            ui.detailBest = Text(panel, "Best", "Best score", 22f, SoftInkKey, TextAlignmentOptions.Left, new Vector2(0f, 0f), new Vector2(0f, 0.5f), new Vector2(40f, 140f), new Vector2(660f, 32f));
            ui.detailBest.enableAutoSizing = true;
            ui.detailBest.fontSizeMin = 14f;
            ui.detailBest.fontSizeMax = 22f;
            ui.playButton = Button(panel, "Play", "kit.primary", "PLAY", "icons.play", new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(420f, 104f), 50f, ButtonTextKey, out TextMeshProUGUI playLabel, OutlineKey);
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
            Image levelPanel = Themed(level, "Panel", "kit.panelDepth", new Color(1f, 1f, 1f, 0.94f), Vector2.zero, Center, Vector2.zero, Vector2.zero, true);
            StretchRect(levelPanel.rectTransform);
            ui.modeIcon = Image(level, "ModeIcon", look.Icons.cards, look.Colors.softInk, new Vector2(0f, 0.5f), Center, new Vector2(58f, 6f), new Vector2(60f, 60f));
            Tint(ui.modeIcon, SoftInkKey);
            ui.levelText = Text(level, "Title", "1. Hello, Farm!", 30f, InkKey, TextAlignmentOptions.Left, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(104f, 22f), new Vector2(430f, 44f));
            ui.levelText.enableAutoSizing = true;
            ui.levelText.fontSizeMin = 18f;
            ui.levelText.fontSizeMax = 30f;
            ui.modeText = Text(level, "Mode", "CLASSIC", 22f, SoftInkKey, TextAlignmentOptions.Left, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(104f, -16f), new Vector2(430f, 34f));

            // Score, combo and sets or parade, top centre.
            TextMeshProUGUI scoreLabel = Text(root, "ScoreLabel", "SCORE", 24f, LightKey, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), Center, new Vector2(0f, -30f), new Vector2(300f, 36f), OutlineKey);
            ui.scoreText = Text(root, "Score", "0", 64f, LightKey, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), Center, new Vector2(0f, -84f), new Vector2(420f, 80f), TitleKey, TextRole.Title);
            ui.comboBadge = Rect(root, "Combo", new Vector2(0.5f, 1f), Center, new Vector2(214f, -80f), new Vector2(118f, 62f));
            Image comboPill = Themed(ui.comboBadge, "Pill", "kit.tintPill", GoldKey, Vector2.zero, Center, Vector2.zero, Vector2.zero, true);
            StretchRect(comboPill.rectTransform);
            ui.comboText = Text(ui.comboBadge, "Text", "x2", 38f, LightKey, TextAlignmentOptions.Center, Center, Center, new Vector2(0f, 2f), new Vector2(110f, 56f), OutlineKey);
            ui.comboBadge.gameObject.SetActive(false);

            RectTransform sets = Rect(root, "Sets", new Vector2(0.5f, 1f), Center, new Vector2(0f, -150f), new Vector2(200f, 54f));
            Image setsPill = Themed(sets, "Pill", "kit.pill", PillKey, Vector2.zero, Center, Vector2.zero, Vector2.zero, true);
            StretchRect(setsPill.rectTransform);
            Themed(sets, "Icon", "icons.cards", LightKey, new Vector2(0f, 0.5f), Center, new Vector2(34f, 0f), new Vector2(36f, 36f));
            ui.setsText = Text(sets, "Text", "0/8", 32f, LightKey, TextAlignmentOptions.Center, Center, Center, new Vector2(18f, 1f), new Vector2(140f, 50f), OutlineKey);
            ui.setsRoot = sets.gameObject;

            RectTransform parade = Rect(root, "Parade", new Vector2(0.5f, 1f), Center, new Vector2(0f, -150f), new Vector2(430f, 92f));
            Image paradePill = Themed(parade, "Pill", "kit.pill", PillKey, Vector2.zero, Center, Vector2.zero, Vector2.zero, true);
            StretchRect(paradePill.rectTransform);
            Text(parade, "Label", "NEXT", 24f, LightKey, TextAlignmentOptions.Center, new Vector2(0f, 0.5f), Center, new Vector2(52f, 0f), new Vector2(90f, 40f), OutlineKey);
            var slots = new List<Image>();
            float x = 124f;
            for (int i = 0; i < 4; i++)
            {
                float size = i == 0 ? 80f : 62f;
                Image disc = i == 0
                    ? Themed(parade, $"Slot {i + 1}", "kit.tintDisc", GoldKey, new Vector2(0f, 0.5f), Center, new Vector2(x + size * 0.5f, 0f), new Vector2(size, size))
                    : Themed(parade, $"Slot {i + 1}", "kit.disc", new Color(1f, 1f, 1f, 0.85f), new Vector2(0f, 0.5f), Center, new Vector2(x + size * 0.5f, 0f), new Vector2(size, size));
                Image animal = Image(disc.transform, "Animal", look.Faces[0], Color.white, Center, Center, Vector2.zero, new Vector2(size * 0.86f, size * 0.86f));
                animal.preserveAspect = true;
                slots.Add(animal);
                x += size + 12f;
            }
            ui.paradeSlots = slots.ToArray();
            ui.paradeRoot = parade.gameObject;
            parade.gameObject.SetActive(false);

            // Timer, hearts, moves and pause, top right.
            ui.pauseButton = RoundButton(root, "Pause", "kit.roundAccent", "icons.pause", AccentTextKey, new Vector2(1f, 1f), new Vector2(-70f, -72f), 104f);
            RectTransform timer = Rect(root, "Timer", new Vector2(1f, 1f), Center, new Vector2(-270f, -72f), new Vector2(236f, 80f));
            Image timerPill = Themed(timer, "Pill", "kit.pill", PillKey, Vector2.zero, Center, Vector2.zero, Vector2.zero, true);
            StretchRect(timerPill.rectTransform);
            ui.timerIcon = Themed(timer, "Icon", "icons.stopwatch", LightKey, new Vector2(0f, 0.5f), Center, new Vector2(44f, 2f), new Vector2(50f, 50f));
            ui.timerText = Text(timer, "Text", "0:00", 44f, look.Colors.light, TextAlignmentOptions.Center, Center, Center, new Vector2(26f, 1f), new Vector2(160f, 70f), OutlineKey);
            ui.timerRoot = timer.gameObject;

            RectTransform hearts = Rect(root, "Hearts", new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-40f, -154f), new Vector2(420f, 60f));
            var heartImages = new List<Image>();
            for (int i = 0; i < 8; i++)
            {
                heartImages.Add(Image(hearts, $"Heart {i + 1}", look.Icons.heartFull, Color.white, new Vector2(1f, 0.5f), Center, new Vector2(-26f - i * 50f, 0f), new Vector2(52f, 52f)));
            }
            ui.hearts = heartImages.ToArray();
            ui.heartsRoot = hearts.gameObject;

            RectTransform moves = Rect(root, "Moves", new Vector2(1f, 1f), Center, new Vector2(-526f, -72f), new Vector2(236f, 76f));
            Image movesPill = Themed(moves, "Pill", "kit.pill", PillKey, Vector2.zero, Center, Vector2.zero, Vector2.zero, true);
            StretchRect(movesPill.rectTransform);
            Themed(moves, "Icon", "icons.moves", LightKey, new Vector2(0f, 0.5f), Center, new Vector2(42f, 0f), new Vector2(46f, 46f));
            ui.movesText = Text(moves, "Text", "20", 38f, look.Colors.light, TextAlignmentOptions.Center, Center, Center, new Vector2(26f, 1f), new Vector2(150f, 60f), OutlineKey);
            Text(moves, "Label", "moves", 18f, new Color(1f, 1f, 1f, 0.8f), TextAlignmentOptions.Center, new Vector2(0.5f, 0f), Center, new Vector2(26f, -10f), new Vector2(150f, 24f), OutlineKey);
            ui.movesRoot = moves.gameObject;

            ui.soloWidgets = new[] { scoreLabel.gameObject, ui.scoreText.gameObject, timer.gameObject };
            ui.versusHud = BuildVersusHud(root);
        }

        /// <summary>The scoreboard of a versus game: a chip per player, between the level panel and the pause button.</summary>
        private static VersusHud BuildVersusHud(Transform root)
        {
            RectTransform strip = Rect(root, "Versus", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(150f, -18f), new Vector2(900f, 128f));
            var versus = strip.gameObject.AddComponent<VersusHud>();
            versus.seatColors = look.Colors.seats.ToArray();
            versus.hurryColor = look.Colors.hurry;
            var chips = new List<VersusHud.Chip>();
            for (int i = 0; i < VersusMatch.MaxPlayers; i++)
            {
                RectTransform chip = Rect(strip, $"Seat {i + 1}", Center, Center, new Vector2((i - 1.5f) * 224f, 0f), new Vector2(210f, 122f));
                Image frame = Themed(chip, "Frame", "kit.tintPanel", new Color(1f, 1f, 1f, 0f), Vector2.zero, Center, Vector2.zero, Vector2.zero, true);
                StretchRect(frame.rectTransform, -7f, -7f, -7f, -7f);
                Image panel = Themed(chip, "Panel", "kit.panelDepth", new Color(1f, 1f, 1f, 0.94f), Vector2.zero, Center, Vector2.zero, Vector2.zero, true);
                StretchRect(panel.rectTransform);
                // The name takes the seat's colour at run time.
                TextMeshProUGUI name = Text(chip, "Name", $"Player {i + 1}", 22f, look.Colors.ink, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(194f, 30f));
                name.textWrappingMode = TextWrappingModes.NoWrap;
                name.enableAutoSizing = true;
                name.fontSizeMin = 13f;
                name.fontSizeMax = 22f;
                TextMeshProUGUI score = Text(chip, "Score", "0", 40f, InkKey, TextAlignmentOptions.Center, Center, Center, new Vector2(0f, 2f), new Vector2(194f, 46f));
                TextMeshProUGUI sets = Text(chip, "Sets", "0 sets", 18f, SoftInkKey, TextAlignmentOptions.Center, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(194f, 24f));
                Image track = Themed(chip, "Clock", "kit.pill", new Color(0.1f, 0.1f, 0.22f, 0.25f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(176f, 10f), true);
                Image fill = Themed(track.transform, "Fill", "kit.tintPill", Color.white, Vector2.zero, Center, Vector2.zero, Vector2.zero);
                StretchRect(fill.rectTransform);
                fill.type = UnityEngine.UI.Image.Type.Filled;
                fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
                fill.fillOrigin = 0;
                chips.Add(new VersusHud.Chip
                {
                    root = chip, panel = panel, frame = frame, nameText = name, scoreText = score, setsText = sets, clockTrack = track, clockFill = fill
                });
            }
            versus.chips = chips.ToArray();
            versus.chipWidth = 210f;
            versus.chipGap = 14f;
            strip.gameObject.SetActive(false);
            return versus;
        }

        private static void BuildOverlays(Transform root, MemoryCardsUI ui)
        {
            RectTransform popups = Stretch(root, "Popups");
            ui.popupRoot = popups;
            ui.popupTemplate = Text(popups, "Popup", "+100", 46f, look.Colors.gold, TextAlignmentOptions.Center, Center, Center, Vector2.zero, new Vector2(500f, 90f), OutlineKey);
            ui.popupTemplate.gameObject.SetActive(false);

            RectTransform banner = Rect(root, "Banner", Center, Center, new Vector2(0f, 40f), new Vector2(1600f, 240f));
            ui.bannerGroup = banner.gameObject.AddComponent<CanvasGroup>();
            ui.bannerGroup.blocksRaycasts = false;
            ui.bannerGroup.interactable = false;
            // The banner takes the colour of what it announces.
            ui.bannerText = Text(banner, "Text", "GO!", 110f, look.Colors.light, TextAlignmentOptions.Center, Center, Center, new Vector2(0f, 28f), new Vector2(1600f, 150f), TitleKey, TextRole.Title);
            ui.bannerSub = Text(banner, "Sub", string.Empty, 40f, look.Colors.light, TextAlignmentOptions.Center, Center, Center, new Vector2(0f, -72f), new Vector2(1500f, 60f), OutlineKey);

            RectTransform memorize = Rect(root, "Memorize", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(760f, 58f));
            Image track = Themed(memorize, "Track", "kit.pill", PillKey, Vector2.zero, Center, Vector2.zero, Vector2.zero, true);
            StretchRect(track.rectTransform);
            ui.memorizeFill = Themed(memorize, "Fill", "kit.tintPill", GoldKey, Vector2.zero, Center, Vector2.zero, Vector2.zero);
            StretchRect(ui.memorizeFill.rectTransform, 6f, 6f, 6f, 6f);
            Text(memorize, "Label", "MEMORIZE!", 30f, LightKey, TextAlignmentOptions.Center, Center, Center, new Vector2(0f, 1f), new Vector2(700f, 50f), OutlineKey);
            ui.memorizeFill.type = UnityEngine.UI.Image.Type.Filled;
            ui.memorizeFill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            ui.memorizeFill.fillOrigin = 0;
            ui.memorizeRoot = memorize.gameObject;

            RectTransform tip = Rect(root, "Tip", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(1500f, 62f));
            ui.tipGroup = tip.gameObject.AddComponent<CanvasGroup>();
            ui.tipGroup.blocksRaycasts = false;
            Image tipPill = Themed(tip, "Pill", "kit.pill", PillKey, Vector2.zero, Center, Vector2.zero, Vector2.zero, true);
            StretchRect(tipPill.rectTransform);
            Themed(tip, "Icon", "icons.paw", GoldKey, new Vector2(0f, 0.5f), Center, new Vector2(44f, 0f), new Vector2(40f, 40f));
            ui.tipText = Text(tip, "Text", "Tip", 26f, LightKey, TextAlignmentOptions.Center, Center, Center, new Vector2(24f, 1f), new Vector2(1380f, 56f), ShadowKey);
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
            Image dim = Image(root, "Dim", null, look.Colors.dim, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, false, true);
            Tint(dim, DimKey);
            StretchRect(dim.rectTransform);
            root = UIBuildUtils.CreateSafeArea(root);

            RectTransform panel = Rect(root, "Panel", Center, Center, new Vector2(0f, -20f), new Vector2(940f, 800f));
            ui.resultPanel = panel;
            Image background = Themed(panel, "Background", "kit.panelDepth", PanelKey, Vector2.zero, Center, Vector2.zero, Vector2.zero, true, true);
            StretchRect(background.rectTransform);
            // The ribbon takes the colour of the world (or of a defeat) at run time.
            ui.resultHeader = Themed(panel, "Ribbon", "kit.tintPill", look.Colors.resultsRibbon, new Vector2(0.5f, 1f), Center, new Vector2(0f, 6f), new Vector2(720f, 118f), true);
            ui.resultTitle = Text(ui.resultHeader.transform, "Title", "GREAT JOB!", 70f, LightKey, TextAlignmentOptions.Center, Center, Center, new Vector2(0f, 4f), new Vector2(690f, 110f), TitleKey, TextRole.Title);
            ui.resultTitle.enableAutoSizing = true;
            ui.resultTitle.fontSizeMin = 40f;
            ui.resultTitle.fontSizeMax = 70f;
            ui.resultSubtitle = Text(panel, "Subtitle", "1. Hello, Farm!", 34f, SoftInkKey, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), Center, new Vector2(0f, -94f), new Vector2(860f, 50f));

            RectTransform stars = Rect(panel, "Stars", new Vector2(0.5f, 1f), Center, new Vector2(0f, -208f), new Vector2(600f, 180f));
            ui.resultStarsRoot = stars;
            ui.resultStars = new[]
            {
                Image(stars, "Star 1", look.Hud.starEmpty, Color.white, Center, Center, new Vector2(-190f, -14f), new Vector2(150f, 141f)),
                Image(stars, "Star 2", look.Hud.starEmpty, Color.white, Center, Center, new Vector2(0f, 12f), new Vector2(190f, 178f)),
                Image(stars, "Star 3", look.Hud.starEmpty, Color.white, Center, Center, new Vector2(190f, -14f), new Vector2(150f, 141f))
            };

            ui.resultStats = Text(panel, "Stats", "Score", 30f, InkKey, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -318f), new Vector2(860f, 150f));
            ui.resultStats.lineSpacing = 8f;

            RectTransform goals = Rect(panel, "Goals", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -478f), new Vector2(620f, 150f));
            var goalTexts = new List<TMP_Text>();
            var goalIcons = new List<Image>();
            for (int i = 0; i < 3; i++)
            {
                float y = -22f - i * 46f;
                goalIcons.Add(Image(goals, $"Icon {i + 1}", look.Hud.goalDone, Color.white, new Vector2(0f, 1f), Center, new Vector2(22f, y), new Vector2(38f, 36f)));
                goalTexts.Add(Text(goals, $"Goal {i + 1}", "Goal", 26f, look.Colors.ink, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(56f, y), new Vector2(560f, 42f)));
            }
            ui.resultGoals = goalTexts.ToArray();
            ui.resultGoalIcons = goalIcons.ToArray();
            ui.resultBest = Text(panel, "Best", "Best", 25f, "palette.best", TextAlignmentOptions.Center, new Vector2(0.5f, 0f), Center, new Vector2(0f, 150f), new Vector2(860f, 40f));
            ui.resultBest.enableAutoSizing = true;
            ui.resultBest.fontSizeMin = 16f;
            ui.resultBest.fontSizeMax = 25f;

            ui.levelsButton = RoundButton(panel, "Levels", "kit.roundNeutral", "icons.levels", NeutralTextKey, new Vector2(0.5f, 0f), new Vector2(-250f, 76f), 116f);
            ui.retryButton = RoundButton(panel, "Retry", "kit.roundAccent", "icons.retry", AccentTextKey, new Vector2(0.5f, 0f), new Vector2(-110f, 76f), 116f);
            ui.nextButton = Button(panel, "Next", "kit.primary", "NEXT", "icons.next", new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f),
                new Vector2(170f, 80f), new Vector2(330f, 112f), 46f, ButtonTextKey, out _, OutlineKey);
        }

        #endregion

        #region Pause menu and settings

        private static void BuildMenu(Transform root, MemoryCardsUI ui, MemoryCardsGameManager manager, MemoryCardsController controller, CanvasScaler scaler)
        {
            RectTransform menu = Stretch(root, "Menu");
            Image dim = menu.gameObject.AddComponent<Image>();
            dim.color = look.Colors.dim;
            dim.raycastTarget = true;
            Tint(dim, DimKey);

            // The pause panel is the base menu's button group: it hides while the settings panel shows.
            RectTransform pause = Rect(menu, "PausePanel", Center, Center, new Vector2(0f, -10f), new Vector2(580f, 860f));
            Image pausePanel = Themed(pause, "Background", "kit.panelDepth", PanelKey, Vector2.zero, Center, Vector2.zero, Vector2.zero, true, true);
            StretchRect(pausePanel.rectTransform);
            Image ribbon = Themed(pause, "Ribbon", "kit.tintPill", "palette.pauseRibbon", new Vector2(0.5f, 1f), Center, new Vector2(0f, 4f), new Vector2(420f, 104f), true);
            Text(ribbon.transform, "Title", "PAUSED", 58f, LightKey, TextAlignmentOptions.Center, Center, Center, new Vector2(0f, 3f), new Vector2(400f, 96f), TitleKey, TextRole.Title);
            var buttons = new (string name, string label, string kit, string icon)[]
            {
                ("ReturnToGame", "RESUME", "kit.primary", "icons.play"),
                ("StartNewGame", "RESTART", "kit.accent", "icons.retry"),
                ("SaveGame", "SAVE GAME", "kit.secondary", null),
                ("LoadGame", "LOAD GAME", "kit.secondary", null),
                ("Game Settings", "SETTINGS", "kit.secondary", "icons.settings"),
                ("LevelSelect", "LEVELS", "kit.neutral", "icons.levels"),
                ("ExitGame", "QUIT", "kit.danger", "icons.power")
            };
            var created = new Dictionary<string, Button>();
            for (int i = 0; i < buttons.Length; i++)
            {
                (string name, string label, string kit, string icon) = buttons[i];
                bool quiet = kit == "kit.accent" || kit == "kit.neutral";
                string words = kit == "kit.accent" ? AccentTextKey : kit == "kit.neutral" ? NeutralTextKey : ButtonTextKey;
                created[name] = Button(pause, name, kit, label, icon, new Vector2(0.5f, 1f), Center, new Vector2(0f, -128f - i * 100f), new Vector2(420f, 88f), 34f,
                    words, out _, quiet ? null : OutlineKey);
            }
            created["LoadGame"].interactable = false;
            ui.levelSelectButton = created["LevelSelect"];
            TextMeshProUGUI error = Text(root, "ErrorText", string.Empty, 30f, LightKey, TextAlignmentOptions.Center, new Vector2(0.5f, 0f), Center, new Vector2(0f, 128f), new Vector2(1400f, 60f), OutlineKey);
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
            Image background = Themed(panel, "Background", "kit.panelDepth", PanelKey, Vector2.zero, Center, Vector2.zero, Vector2.zero, true, true);
            StretchRect(background.rectTransform);
            Image ribbon = Themed(panel, "Ribbon", "kit.tintPill", "palette.settingsRibbon", new Vector2(0.5f, 1f), Center, new Vector2(0f, 4f), new Vector2(620f, 104f), true);
            Text(ribbon.transform, "Title", "FREE PLAY RULES", 50f, LightKey, TextAlignmentOptions.Center, Center, Center, new Vector2(0f, 3f), new Vector2(600f, 96f), TitleKey, TextRole.Title);
            Text(panel, "Subtitle", "Free Play deals its board with these rules. Use 0 to switch a limit off.", 24f, SoftInkKey,
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
                var position = new Vector2(column == 0 ? -270f : 270f, 262f - row * 70f);
                TMP_InputField input = Field(panel, fields[i].field, fields[i].label, fields[i].type, position);
                MemoryCardsAssets.SetObject(settings, fields[i].field, input);
            }
            BuildLookRow(panel, new Vector2(0f, -236f));

            Button custom = Button(panel, "Custom Settings", "kit.accent", "Use Custom Rules", null, new Vector2(0.5f, 0f), Center,
                new Vector2(-330f, 84f), new Vector2(360f, 92f), 28f, AccentTextKey, out TextMeshProUGUI customLabel);
            Button save = Button(panel, "SaveSettingsButton", "kit.primary", "SAVE", null, new Vector2(0.5f, 0f), Center,
                new Vector2(-30f, 84f), new Vector2(200f, 92f), 30f, ButtonTextKey, out _, OutlineKey);
            Button load = Button(panel, "LoadSettingsButton", "kit.secondary", "LOAD", null, new Vector2(0.5f, 0f), Center,
                new Vector2(190f, 84f), new Vector2(200f, 92f), 30f, ButtonTextKey, out _, OutlineKey);
            Button back = Button(panel, "BackToMenu", "kit.neutral", "BACK", "icons.back", new Vector2(0.5f, 0f), Center,
                new Vector2(410f, 84f), new Vector2(200f, 92f), 30f, NeutralTextKey, out _);
            MemoryCardsAssets.SetObject(settings, "CustomSettingsButtonText", customLabel);
            MemoryCardsAssets.SetObject(settings, "CustomSettingsButton", custom);
            MemoryCardsAssets.SetObject(settings, "SaveSettingsButton", save);
            MemoryCardsAssets.SetObject(settings, "LoadSettingsButton", load);
            MemoryCardsAssets.SetObject(settings, "BackButton", back);
            return settings;
        }

        /// <summary>
        /// The "Look" row of the settings panel: a <see cref="ChoiceSelector"/> driven by a <see cref="ThemeSelector"/>
        /// that steps through the themes the game lists (hidden while there are fewer than two).
        /// </summary>
        private static void BuildLookRow(Transform panel, Vector2 position)
        {
            RectTransform row = Rect(panel, "LookRow", Center, Center, position, new Vector2(620f, 66f));
            Text(row, "Label", "Look", 25f, InkKey, TextAlignmentOptions.Left, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(60f, 0f), new Vector2(200f, 56f));
            RectTransform area = Rect(row, "Choice", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-40f, 0f), new Vector2(340f, 62f));
            Button previous = RoundButton(area, "Previous", "kit.roundSecondary", "icons.back", ButtonTextKey, new Vector2(0f, 0.5f), new Vector2(30f, 0f), 60f);
            Button next = RoundButton(area, "Next", "kit.roundSecondary", "icons.next", ButtonTextKey, new Vector2(1f, 0.5f), new Vector2(-30f, 0f), 60f);
            TextMeshProUGUI shown = Text(area, "Value", "Classic", 26f, InkKey, TextAlignmentOptions.Center, Center, Center, Vector2.zero, new Vector2(200f, 56f));
            shown.textWrappingMode = TextWrappingModes.NoWrap;
            shown.enableAutoSizing = true;
            shown.fontSizeMin = 14f;
            shown.fontSizeMax = 26f;
            var choice = area.gameObject.AddComponent<ChoiceSelector>();
            MemoryCardsAssets.SetObject(choice, "label", shown);
            MemoryCardsAssets.SetObject(choice, "previous", previous);
            MemoryCardsAssets.SetObject(choice, "next", next);
            choice.Options = new[] { "Classic" };
            var selector = row.gameObject.AddComponent<ThemeSelector>();
            MemoryCardsAssets.SetObject(selector, "choice", choice);
            MemoryCardsAssets.Set(selector, "game", p => p.enumValueIndex = Array.IndexOf(p.enumNames, GameType.MemoryCards.ToString()));
        }

        private static TMP_InputField Field(Transform parent, string name, string label, TMP_InputField.ContentType type, Vector2 position)
        {
            RectTransform row = Rect(parent, name, Center, Center, position, new Vector2(500f, 64f));
            Text(row, "Label", label, 25f, InkKey, TextAlignmentOptions.Left, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(320f, 56f));
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
            image.sprite = Sprite("kit.inputField");
            image.type = UnityEngine.UI.Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 2f;
            AddThemed(image, "kit.inputField", null);
            var input = fieldObject.GetComponent<TMP_InputField>();
            input.contentType = type;
            input.characterLimit = 6;
            input.pointSize = 28f;
            foreach (TMP_Text text in fieldObject.GetComponentsInChildren<TMP_Text>(true))
            {
                text.font = look.Fonts.body;
                text.fontSharedMaterial = look.Fonts.MaterialOf(TextRole.Body);
                text.fontSize = 28f;
                text.alignment = TextAlignmentOptions.Center;
                bool placeholder = text == input.placeholder;
                text.color = placeholder ? look.Colors.placeholder : look.Colors.ink;
                Themed(text, placeholder ? "palette.placeholder" : InkKey, null, TextRole.Body);
            }
            if (input.placeholder is TMP_Text placeholderText)
            {
                placeholderText.text = "0";
            }
            input.fontAsset = look.Fonts.body;
            return input;
        }

        #endregion

        #region Helpers

        /// <summary>A sprite of the Classic theme by key; the scene cannot be built without it.</summary>
        private static Sprite Sprite(string key)
        {
            Sprite sprite = look.SpriteOf(key);
            if (sprite == null)
            {
                throw new InvalidOperationException($"Memory Cards: the Classic theme has no sprite at {key}.");
            }
            return sprite;
        }

        /// <summary>A colour of the Classic theme by key; the scene cannot be built without it.</summary>
        private static Color ColorOf(string key)
        {
            if (!look.TryColor(key, out Color color))
            {
                throw new InvalidOperationException($"Memory Cards: the Classic theme has no colour at {key}.");
            }
            return color;
        }

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

        /// <summary>An image with a fixed sprite (one the game sets while it runs, or none).</summary>
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

        /// <summary>An image whose sprite and colour come from the theme by key (the colour key may be null).</summary>
        private static Image Themed(Transform parent, string name, string spriteKey, string colorKey, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size,
            bool sliced = false, bool raycast = false)
        {
            Image image = Image(parent, name, Sprite(spriteKey), colorKey != null ? ColorOf(colorKey) : Color.white, anchor, pivot, position, size, sliced, raycast);
            AddThemed(image, spriteKey, colorKey);
            return image;
        }

        /// <summary>An image whose sprite comes from the theme by key, in a colour the game keeps to itself.</summary>
        private static Image Themed(Transform parent, string name, string spriteKey, Color color, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size,
            bool sliced = false, bool raycast = false)
        {
            Image image = Image(parent, name, Sprite(spriteKey), color, anchor, pivot, position, size, sliced, raycast);
            AddThemed(image, spriteKey, null);
            return image;
        }

        private static ThemedImage AddThemed(Image image, string spriteKey, string colorKey)
        {
            var themed = image.gameObject.AddComponent<ThemedImage>();
            themed.SpriteKey = spriteKey ?? string.Empty;
            themed.ColorKey = colorKey ?? string.Empty;
            themed.ThemedGame = GameType.MemoryCards;
            return themed;
        }

        /// <summary>Colours an image (whose sprite is its own) from the theme by key.</summary>
        private static void Tint(Image image, string colorKey)
        {
            AddThemed(image, null, colorKey);
        }

        /// <summary>A label in the theme's font of its role, a material of the theme and a colour of the palette, all by key.</summary>
        private static TextMeshProUGUI Text(Transform parent, string name, string text, float size, string colorKey, TextAlignmentOptions alignment,
            Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 box, string material = null, TextRole role = TextRole.Body)
        {
            TextMeshProUGUI label = Text(parent, name, text, size, ColorOf(colorKey), alignment, anchor, pivot, position, box, material, role);
            Themed(label, colorKey, material, role);
            return label;
        }

        /// <summary>A label in a colour the game keeps to itself (the theme still gives it its font and material).</summary>
        private static TextMeshProUGUI Text(Transform parent, string name, string text, float size, Color color, TextAlignmentOptions alignment,
            Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 box, string material = null, TextRole role = TextRole.Body)
        {
            RectTransform rect = Rect(parent, name, anchor, pivot, position, box);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = look.Fonts.Font(role);
            Material found = material != null ? look.MaterialOf(material) : null;
            label.fontSharedMaterial = found != null ? found : look.Fonts.MaterialOf(role);
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Overflow;
            if (!rect.GetComponent<ThemedLabel>())
            {
                Themed(label, null, material, role);
            }
            return label;
        }

        private static ThemedLabel Themed(TMP_Text label, string colorKey, string material, TextRole role)
        {
            ThemedLabel themed = label.GetComponent<ThemedLabel>();
            if (themed == null)
            {
                themed = label.gameObject.AddComponent<ThemedLabel>();
            }
            themed.Role = role;
            themed.MaterialKey = material ?? string.Empty;
            themed.ColorKey = colorKey ?? string.Empty;
            themed.ThemedGame = GameType.MemoryCards;
            return themed;
        }

        /// <summary>A button of the kit with a label and an optional icon in front of it.</summary>
        private static Button Button(Transform parent, string name, string kitKey, string label, string iconKey, Vector2 anchor, Vector2 pivot, Vector2 position,
            Vector2 size, float fontSize, string labelColorKey, out TextMeshProUGUI text, string material = null)
        {
            RectTransform rect = Rect(parent, name, anchor, pivot, position, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = Sprite(kitKey);
            image.type = UnityEngine.UI.Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1.4f;
            AddThemed(image, kitKey, null);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.colors = ButtonColors();
            Springy(rect);
            float iconSize = size.y * 0.5f;
            float offset = iconKey != null ? iconSize * 0.45f : 0f;
            if (iconKey != null)
            {
                Themed(rect, "Icon", iconKey, labelColorKey, new Vector2(0.5f, 0.5f), Center, new Vector2(-size.x * 0.5f + iconSize * 0.9f, size.y * 0.04f), new Vector2(iconSize, iconSize));
            }
            text = Text(rect, "Label", label, fontSize, labelColorKey, TextAlignmentOptions.Center, Center, Center, new Vector2(offset, size.y * 0.05f),
                new Vector2(size.x - offset * 2f - 30f, size.y), material);
            text.enableAutoSizing = true;
            text.fontSizeMin = fontSize * 0.55f;
            text.fontSizeMax = fontSize;
            return button;
        }

        private static Button RoundButton(Transform parent, string name, string kitKey, string iconKey, string iconColorKey, Vector2 anchor, Vector2 position, float size)
        {
            RectTransform rect = Rect(parent, name, anchor, Center, position, new Vector2(size, size));
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = Sprite(kitKey);
            AddThemed(image, kitKey, null);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.colors = ButtonColors();
            Springy(rect);
            Themed(rect, "Icon", iconKey, iconColorKey, Center, Center, new Vector2(0f, size * 0.04f), new Vector2(size * 0.5f, size * 0.5f));
            return button;
        }

        /// <summary>
        /// Makes a control spring a little bigger under the pointer and squash under a press, the Memory Cards way, scaling
        /// <paramref name="scaled"/> (the control itself when null).
        /// </summary>
        private static PressFeedback Springy(RectTransform rect, Transform scaled = null, float hover = 1.06f)
        {
            return PressFeedback.Attach(rect.gameObject, hover, 0.93f, scaled, true);
        }

        private static Button TextButton(Transform parent, string name, string label, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size, out TextMeshProUGUI text)
        {
            RectTransform rect = Rect(parent, name, anchor, pivot, position, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            text = Text(rect, "Label", label, 22f, LightKey, TextAlignmentOptions.Left, Center, Center, Vector2.zero, size, OutlineKey);
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
