using System.Collections.Generic;
using Gamebox.Editor;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Asteroids.EditorTools
{
    /// <summary>
    /// Builds the Asteroids module's own canvas (mission select with the sector map and the hangar, the HUD and the
    /// results screen) and restyles the shared BaseGame menu as the pause menu, wiring everything to
    /// <see cref="AsteroidsUI"/>.
    /// </summary>
    internal static class AsteroidsInterfaceBuilder
    {
        private static readonly Color PanelColor = new Color(0.35f, 0.75f, 1f, 0.95f);
        private static readonly Color Soft = new Color(0.78f, 0.86f, 0.96f);
        private static readonly Color Dim = new Color(0.55f, 0.65f, 0.8f);
        private static readonly Color Green = new Color(0.25f, 0.85f, 0.5f);
        private static readonly Color Blue = new Color(0.3f, 0.6f, 1f);
        private static readonly Color Orange = new Color(1f, 0.6f, 0.25f);
        private static readonly Color Red = new Color(1f, 0.35f, 0.35f);
        private static readonly Color Gold = new Color(1f, 0.82f, 0.3f);
        private static readonly Color Cyan = new Color(0.35f, 0.9f, 1f);

        private static Material titleFont;
        private static Material hudFont;
        private static Sprite panel;
        private static Sprite button;

        public static void Build(AsteroidsUI ui, GameObject menu, Camera worldCamera, int missionCount, int sectorCount, int shipCount)
        {
            titleFont = AsteroidsSceneBuilder.TitleFont;
            hudFont = AsteroidsSceneBuilder.HudFont;
            panel = AsteroidsArtBuilder.Interface("Panel");
            button = AsteroidsArtBuilder.Interface("Button");

            RestyleMenu(menu);

            var canvasObject = new GameObject("AsteroidsCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.layer = 5;
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            Transform root = canvasObject.transform;

            BuildHud(root, ui);
            BuildTitle(root, ui, missionCount, sectorCount);
            BuildHangar(ui.titleScreen.transform, ui, shipCount);
            BuildResults(root, ui);

            Image curtain = Image(root, "Curtain", null, new Color(0.01f, 0.02f, 0.05f, 0f), Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            StretchFull(curtain.rectTransform);
            curtain.raycastTarget = false;
            ui.curtain = curtain;
            ui.worldCamera = worldCamera;
            ui.starFull = AsteroidsArtBuilder.Icon("Star");
            ui.starEmpty = AsteroidsArtBuilder.Icon("StarEmpty");
            ui.weaponSprites = new[] { AsteroidsArtBuilder.Icon("Blaster"), AsteroidsArtBuilder.Icon("Laser"), AsteroidsArtBuilder.Icon("Scatter"), AsteroidsArtBuilder.Icon("Missile") };
            var previews = new List<Sprite>();
            foreach (var hull in AsteroidsArtBuilder.Hulls)
            {
                previews.Add(AsteroidsArtBuilder.Preview(hull.name));
            }
            ui.shipPreviews = previews.ToArray();
        }

        // ------------------------------------------------------------------ shared menu

        /// <summary>The shared menu becomes the pause menu: chamfered panel, relabelled and recoloured buttons.</summary>
        private static void RestyleMenu(GameObject menu)
        {
            Transform root = menu.transform;
            foreach (string hidden in new[] { "Score", "Level", "Timer" })
            {
                Transform child = GameMenuInstaller.FindChild(root, hidden);
                if (child != null)
                {
                    child.gameObject.SetActive(false);
                }
            }
            Transform menuPanel = GameMenuInstaller.FindChild(root, "MenuPanel");
            if (menuPanel != null && menuPanel.TryGetComponent(out Image panelImage))
            {
                panelImage.sprite = panel;
                panelImage.type = UnityEngine.UI.Image.Type.Sliced;
                panelImage.color = PanelColor;
            }
            Transform settings = GameMenuInstaller.FindChild(root, "SettingsPanel");
            if (settings != null && settings.TryGetComponent(out Image settingsImage))
            {
                settingsImage.sprite = panel;
                settingsImage.type = UnityEngine.UI.Image.Type.Sliced;
                settingsImage.color = new Color(0.4f, 0.8f, 1f, 1f);
            }
            Transform inputs = GameMenuInstaller.FindChild(root, "InputPanel");
            if (inputs != null)
            {
                if (inputs.TryGetComponent(out Image inputsImage))
                {
                    inputsImage.sprite = panel;
                    inputsImage.type = UnityEngine.UI.Image.Type.Sliced;
                    inputsImage.color = new Color(0.03f, 0.08f, 0.16f, 0.92f);
                }
                foreach (TMP_InputField field in inputs.GetComponentsInChildren<TMP_InputField>(true))
                {
                    StyleInputField(field);
                }
            }
            if (menuPanel != null)
            {
                // The HUD stays visible while paused: lower the menu (a fresh instance on every rebuild) below its objective line.
                foreach (string part in new[] { "Header", "MenuButtons", "SettingsPanel" })
                {
                    if (GameMenuInstaller.FindChild(menuPanel, part) is RectTransform rect)
                    {
                        rect.anchoredPosition += new Vector2(0f, -60f);
                    }
                }
            }
            SetLabel(GameMenuInstaller.FindChildComponent<TMP_Text>(root, "Header"), "PAUSED", Cyan, titleFont);
            SetLabel(GameMenuInstaller.FindChildComponent<TMP_Text>(root, "SettingsHeader"), "SETTINGS", Cyan, titleFont);
            StyleMenuButton(root, "ReturnToGame", "Resume", Green);
            StyleMenuButton(root, "StartNewGame", "Restart Mission", Orange);
            StyleMenuButton(root, "SaveGame", "Save Mission", Blue);
            StyleMenuButton(root, "LoadGame", "Load Mission", Blue);
            StyleMenuButton(root, "Game Settings", "Settings", Blue);
            StyleMenuButton(root, "ExitGame", "Abandon Mission", Red);
            StyleMenuButton(root, "Custom Settings", null, Blue);
            StyleMenuButton(root, "SaveSettingsButton", null, Green);
            StyleMenuButton(root, "LoadSettingsButton", null, Blue);
            StyleMenuButton(root, "BackToMenu", null, Orange);
            Transform resume = GameMenuInstaller.FindChild(root, "ReturnToGame");
            if (resume != null)
            {
                resume.SetSiblingIndex(0);
            }
        }

        private static void SetLabel(TMP_Text text, string label, Color color, Material material)
        {
            if (text == null)
            {
                return;
            }
            text.text = label;
            text.color = color;
            text.fontStyle = FontStyles.Bold;
            text.characterSpacing = 8f;
            if (material != null)
            {
                text.fontSharedMaterial = material;
            }
        }

        /// <summary>A settings row in the game's look: a soft label that shrinks to fit and a dark chamfered field.</summary>
        private static void StyleInputField(TMP_InputField field)
        {
            if (field.TryGetComponent(out Image background))
            {
                background.sprite = button;
                background.type = UnityEngine.UI.Image.Type.Sliced;
                background.color = new Color(0.14f, 0.3f, 0.5f, 1f);
            }
            field.customCaretColor = true;
            field.caretColor = Cyan;
            field.selectionColor = new Color(0.35f, 0.75f, 1f, 0.45f);
            if (field.textComponent != null)
            {
                field.textComponent.color = Color.white;
                field.textComponent.fontStyle = FontStyles.Bold;
            }
            if (field.placeholder is TMP_Text placeholder)
            {
                placeholder.color = new Color(1f, 1f, 1f, 0.35f);
            }
            TMP_Text label = field.transform.parent != null ? GameMenuInstaller.FindChildComponent<TMP_Text>(field.transform.parent, "Label") : null;
            if (label != null)
            {
                label.color = Soft;
                label.fontStyle = FontStyles.Bold;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.enableAutoSizing = true;
                label.fontSizeMin = 14f;
                label.fontSizeMax = 22f;
            }
        }

        private static void StyleMenuButton(Transform root, string name, string label, Color color)
        {
            Transform target = GameMenuInstaller.FindChild(root, name);
            if (target == null)
            {
                return;
            }
            if (target.TryGetComponent(out Image image))
            {
                image.sprite = button;
                image.type = UnityEngine.UI.Image.Type.Sliced;
                image.color = color;
            }
            if (target.TryGetComponent(out Button component))
            {
                component.colors = ButtonColors();
            }
            var text = target.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
            {
                if (label != null)
                {
                    text.text = label;
                }
                text.color = Color.white;
                text.fontStyle = FontStyles.Bold;
                text.fontSharedMaterial = hudFont;
            }
        }

        // ------------------------------------------------------------------ mission select

        private static void BuildTitle(Transform canvas, AsteroidsUI ui, int missionCount, int sectorCount)
        {
            CanvasGroup screen = Screen(canvas, "MissionSelect");
            Transform root = screen.transform;
            ui.titleScreen = screen;

            Image top = Image(root, "TopFade", AsteroidsArtBuilder.Interface("Fade"), new Color(0f, 0.01f, 0.04f, 0.85f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1920f, 260f));
            top.rectTransform.anchorMin = new Vector2(0f, 1f);
            top.rectTransform.anchorMax = new Vector2(1f, 1f);
            top.rectTransform.sizeDelta = new Vector2(0f, 260f);
            top.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 180f);
            top.raycastTarget = false;
            Image bottom = Image(root, "BottomFade", AsteroidsArtBuilder.Interface("Fade"), new Color(0f, 0.01f, 0.04f, 0.8f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(1920f, 220f));
            bottom.rectTransform.anchorMin = Vector2.zero;
            bottom.rectTransform.anchorMax = new Vector2(1f, 0f);
            bottom.rectTransform.sizeDelta = new Vector2(0f, 220f);
            bottom.raycastTarget = false;

            // Logo
            TextMeshProUGUI logo = Text(root, "Logo", "ASTEROIDS", 118f, Color.white, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60f, -18f), new Vector2(900f, 130f), titleFont);
            logo.characterSpacing = 14f;
            Gradient(logo, new Color(0.75f, 0.97f, 1f), new Color(0.25f, 0.6f, 1f));
            TextMeshProUGUI subtitle = Text(root, "Subtitle", "DEEP SPACE CAMPAIGN", 28f, Cyan, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(66f, -140f), new Vector2(900f, 40f), hudFont);
            subtitle.characterSpacing = 22f;

            // Stars
            RectTransform stars = Panel(root, "Stars", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-60f, -40f), new Vector2(330f, 96f));
            Image(stars, "Icon", AsteroidsArtBuilder.Icon("Star"), Color.white, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(58f, 0f), new Vector2(72f, 72f)).raycastTarget = false;
            ui.starsTotal = Text(stars, "Total", "0 / 36", 48f, Color.white, TextAlignmentOptions.Left, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(110f, 0f), new Vector2(210f, 70f), titleFont);
            ui.resetProgressButton = TextButton(root, "ResetProgress", "Reset progress", new Vector2(1f, 1f), new Vector2(-60f, -150f), new Vector2(330f, 34f), 20f, out TextMeshProUGUI resetLabel);
            ui.resetProgressLabel = resetLabel;

            // Mission details
            RectTransform details = Panel(root, "Details", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(60f, -30f), new Vector2(620f, 660f));
            Image accent = Image(details, "Accent", AsteroidsArtBuilder.Interface("Bar"), Cyan, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(8f, 600f), true);
            accent.raycastTarget = false;
            ui.detailAccent = accent;
            ui.detailSector = Text(details, "Sector", "KEPLER BELT - MISSION 1", 24f, Cyan, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -26f), new Vector2(560f, 34f), hudFont);
            ui.detailSector.characterSpacing = 4f;
            ui.detailTitle = Text(details, "Title", "First Light", 54f, Color.white, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -62f), new Vector2(560f, 70f), titleFont);
            ui.detailStars = StarRow(details, "Stars", new Vector2(0f, 1f), new Vector2(40f, -145f), 54f, 62f);
            ui.detailDescription = Text(details, "Description", "Description", 25f, Soft, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -206f), new Vector2(550f, 120f), null, false);
            ui.detailObjective = Text(details, "Objective", "Objective", 25f, Color.white, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -336f), new Vector2(550f, 70f), null);
            ui.detailNew = Text(details, "New", "New", 23f, Soft, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -400f), new Vector2(550f, 90f), null, false);
            ui.detailBest = Text(details, "Best", "Best", 23f, Gold, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -494f), new Vector2(550f, 40f), null);
            ui.launchButton = Button(details, "Launch", "LAUNCH", AsteroidsArtBuilder.Icon("Play"), Green, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(540f, 96f), 46f, out TextMeshProUGUI launchLabel);
            ui.launchLabel = launchLabel;

            // Sector map
            RectTransform map = Rect(root, "SectorMap", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-60f, -30f), new Vector2(1150f, 660f));
            var nodes = new List<MissionNode>();
            var titles = new List<TMP_Text>();
            var gates = new List<TMP_Text>();
            const float column = 272f;
            const float gap = 20.6f;
            int perSector = sectorCount > 0 ? Mathf.Max(1, (missionCount - 1) / sectorCount) : 3;
            for (int s = 0; s < sectorCount; s++)
            {
                float x = s * (column + gap);
                RectTransform header = Panel(map, $"Sector{s + 1}", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, 0f), new Vector2(column, 84f), new Color(0.3f, 0.6f, 0.9f, 0.9f));
                TextMeshProUGUI title = Text(header, "Title", "SECTOR", 24f, Cyan, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(column - 16f, 36f), hudFont);
                title.textWrappingMode = TextWrappingModes.NoWrap;
                title.enableAutoSizing = true;
                title.fontSizeMin = 15f;
                title.fontSizeMax = 24f;
                titles.Add(title);
                gates.Add(Text(header, "Gate", "OPEN", 17f, Dim, TextAlignmentOptions.Center, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(column - 16f, 28f), null));
                Image line = Image(map, $"Path{s + 1}", AsteroidsArtBuilder.Interface("Bar"), new Color(0.4f, 0.8f, 1f, 0.25f), new Vector2(0f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(x + column * 0.5f, -150f), new Vector2(6f, 340f), true);
                line.raycastTarget = false;
                for (int m = 0; m < perSector; m++)
                {
                    float offset = m % 2 == 0 ? -34f : 34f;
                    nodes.Add(HexNode(map, s * perSector + m, new Vector2(x + column * 0.5f + offset, -176f - m * 168f)));
                }
            }
            nodes.Add(EndlessNode(map, missionCount - 1));
            ui.missionNodes = nodes.ToArray();
            ui.sectorTitles = titles.ToArray();
            ui.sectorGates = gates.ToArray();

            // Buttons
            RectTransform buttons = Rect(root, "Buttons", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-60f, 34f), new Vector2(1100f, 84f));
            var row = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 18f;
            row.childAlignment = TextAnchor.MiddleRight;
            row.childControlWidth = false;
            row.childControlHeight = false;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            ui.continueButton = Button(buttons, "Continue", "Continue", AsteroidsArtBuilder.Icon("Retry"), Green, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(250f, 80f), 30f, out _);
            ui.hangarButton = Button(buttons, "Hangar", "Hangar", AsteroidsArtBuilder.Icon("Hangar"), Blue, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(250f, 80f), 30f, out _);
            ui.titleSettingsButton = Button(buttons, "Settings", "Settings", AsteroidsArtBuilder.Icon("Settings"), Blue, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(250f, 80f), 30f, out _);
            ui.titleExitButton = Button(buttons, "Quit", "Quit", AsteroidsArtBuilder.Icon("Exit"), Red, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(220f, 80f), 30f, out _);
            Text(root, "Controls", "W A S D / ARROWS fly    SPACE fire    SHIFT dash    B nova bomb    ESC pause", 21f, Dim, TextAlignmentOptions.Left,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(60f, 40f), new Vector2(760f, 70f), null, false);
        }

        private static MissionNode HexNode(Transform parent, int index, Vector2 position)
        {
            RectTransform rect = Rect(parent, $"Mission{index + 1}", new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), position, new Vector2(150f, 130f));
            var node = rect.gameObject.AddComponent<MissionNode>();
            Image frame = Image(rect, "Frame", AsteroidsArtBuilder.Interface("HexagonFrame"), Cyan, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(186f, 162f));
            frame.enabled = false;
            frame.raycastTarget = false;
            Image background = Image(rect, "Background", AsteroidsArtBuilder.Interface("Hexagon"), Blue, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150f, 130f));
            background.preserveAspect = false;
            var buttonComponent = rect.gameObject.AddComponent<Button>();
            buttonComponent.targetGraphic = background;
            buttonComponent.colors = ButtonColors();
            node.button = buttonComponent;
            node.background = background;
            node.frame = frame;
            node.number = Text(rect, "Number", (index + 1).ToString(), 50f, Color.white, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(130f, 64f), titleFont);
            Image boss = Image(rect, "Boss", AsteroidsArtBuilder.Icon("Skull"), Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(46f, 36f), new Vector2(44f, 44f));
            boss.raycastTarget = false;
            node.bossIcon = boss.gameObject;
            node.stars = StarRow(rect, "Stars", new Vector2(0.5f, 0.5f), new Vector2(-30f, -34f), 28f, 30f);
            node.title = Text(rect, "Title", "Mission", 19f, Soft, TextAlignmentOptions.Center, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(250f, 30f), hudFont);
            RectTransform locked = Rect(rect, "Locked", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150f, 130f));
            Image lockIcon = Image(locked, "Lock", AsteroidsArtBuilder.Icon("Lock"), Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(58f, 58f));
            lockIcon.raycastTarget = false;
            node.lockIcon = locked.gameObject;
            return node;
        }

        private static MissionNode EndlessNode(Transform parent, int index)
        {
            RectTransform rect = Rect(parent, "Endless", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(1150f, 84f));
            var node = rect.gameObject.AddComponent<MissionNode>();
            Image frame = Image(rect, "Frame", AsteroidsArtBuilder.Interface("Frame"), Cyan, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1172f, 106f), true);
            frame.enabled = false;
            frame.raycastTarget = false;
            Image background = Image(rect, "Background", button, Blue, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1150f, 84f), true);
            var buttonComponent = rect.gameObject.AddComponent<Button>();
            buttonComponent.targetGraphic = background;
            buttonComponent.colors = ButtonColors();
            node.button = buttonComponent;
            node.background = background;
            node.frame = frame;
            Image infinity = Image(rect, "Infinity", AsteroidsArtBuilder.Icon("Infinity"), Color.white, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(60f, 0f), new Vector2(76f, 60f));
            infinity.raycastTarget = false;
            node.endlessIcon = infinity.gameObject;
            node.title = Text(rect, "Title", "Deep Field", 34f, Color.white, TextAlignmentOptions.Left, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(118f, 0f), new Vector2(500f, 60f), titleFont);
            Text(rect, "Kind", "ENDLESS SURVIVAL", 22f, Soft, TextAlignmentOptions.Right, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-30f, 0f), new Vector2(420f, 40f), hudFont);
            RectTransform locked = Rect(rect, "Locked", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1150f, 84f));
            Image lockIcon = Image(locked, "Lock", AsteroidsArtBuilder.Icon("Lock"), Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(56f, 56f));
            lockIcon.raycastTarget = false;
            node.lockIcon = locked.gameObject;
            node.number = null;
            node.stars = new Image[0];
            return node;
        }

        // ------------------------------------------------------------------ hangar

        private static void BuildHangar(Transform titleScreen, AsteroidsUI ui, int shipCount)
        {
            CanvasGroup screen = Screen(titleScreen, "Hangar");
            Transform root = screen.transform;
            ui.hangarScreen = screen;
            Image dim = Image(root, "Dim", null, new Color(0f, 0.01f, 0.04f, 0.85f), Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            StretchFull(dim.rectTransform);
            TextMeshProUGUI title = Text(root, "Title", "HANGAR", 76f, Color.white, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(1000f, 100f), titleFont);
            title.characterSpacing = 16f;
            Gradient(title, new Color(0.75f, 0.97f, 1f), new Color(0.25f, 0.6f, 1f));
            Text(root, "Subtitle", "Earn stars in the campaign to unlock new ships", 26f, Soft, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(1200f, 40f), null, false);
            RectTransform cards = Rect(root, "Ships", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(1640f, 620f));
            var layout = cards.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 24f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            var list = new List<ShipCard>();
            for (int i = 0; i < shipCount; i++)
            {
                list.Add(ShipCard(cards, i));
            }
            ui.shipCards = list.ToArray();
            ui.hangarBackButton = Button(root, "Back", "Back", AsteroidsArtBuilder.Icon("Levels"), Orange, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(300f, 86f), 34f, out _);
            screen.gameObject.SetActive(false);
        }

        private static ShipCard ShipCard(Transform parent, int index)
        {
            RectTransform rect = Rect(parent, $"Ship{index + 1}", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(380f, 600f));
            var card = rect.gameObject.AddComponent<ShipCard>();
            Image frame = Image(rect, "Frame", AsteroidsArtBuilder.Interface("Frame"), Cyan, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(404f, 624f), true);
            frame.enabled = false;
            frame.raycastTarget = false;
            Image background = Image(rect, "Background", panel, PanelColor, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(380f, 600f), true);
            var buttonComponent = rect.gameObject.AddComponent<Button>();
            buttonComponent.targetGraphic = background;
            buttonComponent.colors = ButtonColors();
            Image glow = Image(rect, "Glow", AsteroidsArtBuilder.Interface("Glow"), new Color(0.3f, 0.7f, 1f, 0.35f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -170f), new Vector2(320f, 320f));
            glow.raycastTarget = false;
            card.button = buttonComponent;
            card.frame = frame;
            card.preview = Image(rect, "Preview", null, Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -170f), new Vector2(270f, 270f));
            card.preview.preserveAspect = true;
            card.preview.raycastTarget = false;
            Image lockIcon = Image(rect, "Lock", AsteroidsArtBuilder.Icon("Lock"), Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -170f), new Vector2(96f, 96f));
            lockIcon.raycastTarget = false;
            card.lockIcon = lockIcon.gameObject;
            card.title = Text(rect, "Title", "SPARROW", 36f, Color.white, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -318f), new Vector2(360f, 48f), titleFont);
            card.description = Text(rect, "Description", "Description", 20f, Soft, TextAlignmentOptions.Top, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -368f), new Vector2(340f, 60f), null, false);
            var bars = new List<Image>();
            string[] labels = { "SPEED", "HANDLING", "ARMOR", "FIREPOWER" };
            for (int i = 0; i < labels.Length; i++)
            {
                float y = -440f - i * 30f;
                Text(rect, labels[i], labels[i], 17f, Dim, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(26f, y), new Vector2(140f, 26f), null);
                Image back = Image(rect, $"{labels[i]}Back", AsteroidsArtBuilder.Interface("Bar"), new Color(1f, 1f, 1f, 0.12f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(160f, y - 4f), new Vector2(190f, 16f), true);
                back.raycastTarget = false;
                Image fill = Image(rect, $"{labels[i]}Fill", AsteroidsArtBuilder.Interface("Bar"), Cyan, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(160f, y - 4f), new Vector2(190f, 16f));
                fill.type = UnityEngine.UI.Image.Type.Filled;
                fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
                fill.preserveAspect = false;
                fill.raycastTarget = false;
                bars.Add(fill);
            }
            card.bars = bars.ToArray();
            card.status = Text(rect, "Status", "SELECTED", 22f, Cyan, TextAlignmentOptions.Center, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(360f, 34f), hudFont);
            return card;
        }

        // ------------------------------------------------------------------ HUD

        private static void BuildHud(Transform canvas, AsteroidsUI ui)
        {
            CanvasGroup screen = Screen(canvas, "HUD");
            Transform root = screen.transform;
            ui.hudScreen = screen;

            Image flash = Image(root, "DamageFlash", AsteroidsArtBuilder.Interface("Vignette"), new Color(1f, 0.1f, 0.15f, 0f), Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            StretchFull(flash.rectTransform);
            flash.raycastTarget = false;
            ui.damageFlash = flash;

            // Warnings (below everything else of the HUD)
            var warnings = new List<AsteroidsUI.WarningMarker>();
            for (int i = 0; i < 3; i++)
            {
                Image lane = Image(root, $"Lane{i + 1}", AsteroidsArtBuilder.Interface("Lane"), new Color(1f, 0.25f, 0.2f, 0.3f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(3200f, 90f));
                lane.raycastTarget = false;
                Image arrow = Image(root, $"Warning{i + 1}", AsteroidsArtBuilder.Icon("Arrow"), Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(96f, 96f));
                arrow.raycastTarget = false;
                warnings.Add(new AsteroidsUI.WarningMarker { arrow = arrow.rectTransform, lane = lane });
            }
            ui.warnings = warnings.ToArray();

            // Score and combo
            RectTransform score = Panel(root, "Score", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -24f), new Vector2(360f, 118f));
            Text(score, "Label", "SCORE", 20f, Dim, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -12f), new Vector2(160f, 28f), hudFont).characterSpacing = 8f;
            ui.scoreText = Text(score, "Value", "0", 50f, Color.white, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -38f), new Vector2(240f, 60f), hudFont);
            ui.scoreText.rectTransform.pivot = new Vector2(0f, 0.5f);
            ui.scoreText.rectTransform.anchoredPosition = new Vector2(22f, -68f);
            ui.multiplierText = Text(score, "Multiplier", "x2", 44f, Gold, TextAlignmentOptions.Center, new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-56f, 6f), new Vector2(100f, 60f), titleFont);
            ui.multiplierText.gameObject.SetActive(false);
            Image comboBack = Image(score, "ComboBack", AsteroidsArtBuilder.Interface("Bar"), new Color(1f, 1f, 1f, 0.1f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 12f), new Vector2(312f, 8f), true);
            comboBack.raycastTarget = false;
            Image combo = Image(score, "Combo", AsteroidsArtBuilder.Interface("Bar"), Gold, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 12f), new Vector2(312f, 8f));
            combo.type = UnityEngine.UI.Image.Type.Filled;
            combo.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            combo.preserveAspect = false;
            combo.raycastTarget = false;
            combo.fillAmount = 0f;
            ui.comboFill = combo;

            // Objective
            ui.missionText = Text(root, "Mission", "FIRST LIGHT", 22f, Cyan, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(900f, 32f), hudFont);
            ui.missionText.characterSpacing = 10f;
            ui.objectiveText = Text(root, "Objective", "WAVE 1 / 3", 34f, Color.white, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(900f, 46f), hudFont);
            Image objectiveBack = Image(root, "ObjectiveBack", AsteroidsArtBuilder.Interface("Bar"), new Color(1f, 1f, 1f, 0.12f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -102f), new Vector2(520f, 10f), true);
            objectiveBack.raycastTarget = false;
            Image objective = Image(root, "ObjectiveFill", AsteroidsArtBuilder.Interface("Bar"), Cyan, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -102f), new Vector2(520f, 10f));
            objective.type = UnityEngine.UI.Image.Type.Filled;
            objective.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            objective.preserveAspect = false;
            objective.raycastTarget = false;
            objective.fillAmount = 0f;
            ui.objectiveFill = objective;

            // Boss bar
            RectTransform boss = Rect(root, "Boss", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -128f), new Vector2(980f, 70f));
            Image(boss, "Skull", AsteroidsArtBuilder.Icon("Skull"), Color.white, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(30f, 0f), new Vector2(56f, 56f)).raycastTarget = false;
            ui.bossName = Text(boss, "Name", "BOSS", 24f, new Color(1f, 0.55f, 0.5f), TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(70f, 2f), new Vector2(600f, 30f), hudFont);
            ui.bossName.characterSpacing = 6f;
            Image bossBack = Image(boss, "Back", AsteroidsArtBuilder.Interface("Bar"), new Color(0.3f, 0.05f, 0.08f, 0.8f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(70f, 8f), new Vector2(900f, 22f), true);
            bossBack.raycastTarget = false;
            Image bossFill = Image(boss, "Fill", AsteroidsArtBuilder.Interface("Bar"), new Color(1f, 0.25f, 0.3f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(70f, 8f), new Vector2(900f, 22f));
            bossFill.type = UnityEngine.UI.Image.Type.Filled;
            bossFill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            bossFill.preserveAspect = false;
            bossFill.raycastTarget = false;
            ui.bossFill = bossFill;
            ui.bossBar = boss.gameObject;

            // Lives and pause
            RectTransform lives = Rect(root, "Lives", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-130f, -34f), new Vector2(9 * 46f, 50f));
            var livesLayout = lives.gameObject.AddComponent<HorizontalLayoutGroup>();
            livesLayout.childAlignment = TextAnchor.MiddleRight;
            livesLayout.spacing = 2f;
            livesLayout.childControlWidth = false;
            livesLayout.childControlHeight = false;
            livesLayout.childForceExpandWidth = false;
            livesLayout.childForceExpandHeight = false;
            var lifeIcons = new List<Image>();
            for (int i = 0; i < AsteroidSettings.LivesLimit; i++)
            {
                Image life = Image(lives, $"Life{i + 1}", AsteroidsArtBuilder.Icon("Ship"), Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(44f, 44f));
                life.raycastTarget = false;
                life.gameObject.SetActive(i < 3);
                lifeIcons.Add(life);
            }
            ui.lifeIcons = lifeIcons.ToArray();
            Image pause = Image(root, "Pause", AsteroidsArtBuilder.Icon("Pause"), Color.white, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -22f), new Vector2(88f, 88f));
            var pauseButton = pause.gameObject.AddComponent<Button>();
            pauseButton.targetGraphic = pause;
            pauseButton.colors = ButtonColors();
            ui.pauseButton = pauseButton;

            // Hull, shield, dash
            RectTransform ship = Panel(root, "ShipStatus", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(28f, 24f), new Vector2(430f, 118f));
            Image(ship, "HullIcon", AsteroidsArtBuilder.Icon("Repair"), Color.white, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(40f, -36f), new Vector2(40f, 40f)).raycastTarget = false;
            ui.hullFill = Meter(ship, "Hull", new Vector2(70f, -28f), new Vector2(330f, 18f), new Color(0.35f, 1f, 0.55f));
            Image(ship, "ShieldIcon", AsteroidsArtBuilder.Icon("Shield"), Color.white, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(40f, -80f), new Vector2(40f, 40f)).raycastTarget = false;
            ui.shieldFill = Meter(ship, "ShieldBar", new Vector2(70f, -72f), new Vector2(330f, 18f), new Color(0.35f, 0.75f, 1f));
            Image dashIcon = Image(root, "Dash", AsteroidsArtBuilder.Icon("Dash"), new Color(1f, 1f, 1f, 0.9f), new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(510f, 84f), new Vector2(58f, 58f));
            dashIcon.raycastTarget = false;
            Image dash = Image(root, "DashRing", AsteroidsArtBuilder.Interface("TimerRing"), Cyan, new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(510f, 84f), new Vector2(86f, 86f));
            dash.type = UnityEngine.UI.Image.Type.Filled;
            dash.fillMethod = UnityEngine.UI.Image.FillMethod.Radial360;
            dash.fillOrigin = (int)UnityEngine.UI.Image.Origin360.Top;
            dash.preserveAspect = false;
            dash.raycastTarget = false;
            ui.dashFill = dash;

            // Weapon and bombs
            RectTransform weapon = Panel(root, "Weapon", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-28f, 24f), new Vector2(430f, 118f));
            ui.weaponIcon = Image(weapon, "Icon", AsteroidsArtBuilder.Icon("Blaster"), Color.white, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(60f, 4f), new Vector2(76f, 76f));
            ui.weaponIcon.raycastTarget = false;
            ui.weaponText = Text(weapon, "Name", "BLASTER", 30f, Cyan, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(112f, -14f), new Vector2(200f, 40f), hudFont);
            var pips = new List<Image>();
            for (int i = 0; i < WeaponRules.MaxLevel; i++)
            {
                Image pip = Image(weapon, $"Level{i + 1}", AsteroidsArtBuilder.Interface("Bar"), Cyan, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(114f + i * 44f, -66f), new Vector2(38f, 12f), true);
                pip.raycastTarget = false;
                pips.Add(pip);
            }
            ui.weaponPips = pips.ToArray();
            var bombs = new List<Image>();
            for (int i = 0; i < AsteroidsPlayer.MaxBombs; i++)
            {
                Image bomb = Image(weapon, $"Bomb{i + 1}", AsteroidsArtBuilder.Icon("Nova"), Color.white, new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-34f - i * 46f, -20f), new Vector2(42f, 42f));
                bomb.raycastTarget = false;
                bombs.Add(bomb);
            }
            bombs.Reverse();
            ui.bombIcons = bombs.ToArray();
            Text(weapon, "BombKey", "B", 18f, Dim, TextAlignmentOptions.Center, new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-80f, 22f), new Vector2(40f, 24f), null);

            // Power-ups
            RectTransform powerUps = Rect(root, "PowerUps", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-26f, 20f), new Vector2(104f, 460f));
            var column = powerUps.gameObject.AddComponent<VerticalLayoutGroup>();
            column.spacing = 12f;
            column.childAlignment = TextAnchor.MiddleCenter;
            column.childControlWidth = false;
            column.childControlHeight = false;
            column.childForceExpandWidth = false;
            column.childForceExpandHeight = false;
            var slots = new List<AsteroidsUI.PowerUpSlot>();
            foreach (PowerUpType type in (PowerUpType[])System.Enum.GetValues(typeof(PowerUpType)))
            {
                RectTransform slot = Rect(powerUps, type.ToString(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100f, 100f));
                Image(slot, "Back", AsteroidsArtBuilder.Interface("Glow"), new Color(0f, 0.02f, 0.08f, 0.8f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120f, 120f)).raycastTarget = false;
                Image ring = Image(slot, "Timer", AsteroidsArtBuilder.Interface("TimerRing"), PowerUps.Tint(type), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100f, 100f));
                ring.type = UnityEngine.UI.Image.Type.Filled;
                ring.fillMethod = UnityEngine.UI.Image.FillMethod.Radial360;
                ring.fillOrigin = (int)UnityEngine.UI.Image.Origin360.Top;
                ring.fillClockwise = false;
                ring.preserveAspect = false;
                ring.raycastTarget = false;
                Image(slot, "Icon", AsteroidsArtBuilder.Icon(type.ToString()), Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(58f, 58f)).raycastTarget = false;
                slot.gameObject.SetActive(false);
                slots.Add(new AsteroidsUI.PowerUpSlot { type = type, root = slot.gameObject, fill = ring });
            }
            ui.powerUpSlots = slots.ToArray();

            // Briefing, countdown, announcements, toasts, hints
            RectTransform briefing = Rect(root, "Briefing", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 250f), new Vector2(1400f, 220f));
            var briefingGroup = briefing.gameObject.AddComponent<CanvasGroup>();
            briefingGroup.blocksRaycasts = false;
            briefingGroup.interactable = false;
            ui.briefingGroup = briefingGroup;
            ui.briefingSector = Text(briefing, "Sector", "KEPLER BELT", 28f, Cyan, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1400f, 40f), hudFont);
            ui.briefingSector.characterSpacing = 18f;
            ui.briefingTitle = Text(briefing, "Title", "FIRST LIGHT", 76f, Color.white, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(1400f, 100f), titleFont);
            ui.briefingTitle.characterSpacing = 8f;
            ui.briefingObjective = Text(briefing, "Objective", "Clear 3 waves", 32f, Soft, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(1400f, 50f), hudFont);

            // Below the ship, which waits at the centre during the countdown.
            TextMeshProUGUI countdown = Text(root, "Countdown", "3", 170f, Cyan, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -235f), new Vector2(900f, 220f), titleFont);
            countdown.gameObject.SetActive(false);
            ui.countdownText = countdown;

            RectTransform announce = Rect(root, "Announce", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(1500f, 180f));
            var announceGroup = announce.gameObject.AddComponent<CanvasGroup>();
            announceGroup.blocksRaycasts = false;
            announceGroup.interactable = false;
            ui.announceGroup = announceGroup;
            ui.announceTitle = Text(announce, "Title", "WAVE 2", 92f, Cyan, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1500f, 120f), titleFont);
            ui.announceTitle.characterSpacing = 10f;
            ui.announceSubtitle = Text(announce, "Subtitle", "2 of 3", 32f, Soft, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -118f), new Vector2(1500f, 50f), hudFont);

            TextMeshProUGUI toast = Text(root, "Toast", "Toast", 40f, Color.white, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -210f), new Vector2(1400f, 60f), hudFont);
            toast.characterSpacing = 4f;
            toast.gameObject.SetActive(false);
            ui.toastText = toast;

            RectTransform hint = Panel(root, "Hint", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 160f), new Vector2(1100f, 78f));
            var hintGroup = hint.gameObject.AddComponent<CanvasGroup>();
            hintGroup.alpha = 0f;
            hintGroup.blocksRaycasts = false;
            hintGroup.interactable = false;
            ui.hintGroup = hintGroup;
            ui.hintText = Text(hint, "Text", "Hint", 28f, Color.white, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1060f, 70f), hudFont);
            ui.hintText.textWrappingMode = TextWrappingModes.NoWrap;
            ui.hintText.enableAutoSizing = true;
            ui.hintText.fontSizeMin = 18f;
            ui.hintText.fontSizeMax = 28f;
        }

        private static Image Meter(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            Image back = Image(parent, $"{name}Back", AsteroidsArtBuilder.Interface("Bar"), new Color(1f, 1f, 1f, 0.12f), new Vector2(0f, 1f), new Vector2(0f, 1f), position, size, true);
            back.raycastTarget = false;
            Image fill = Image(parent, $"{name}Fill", AsteroidsArtBuilder.Interface("Bar"), color, new Vector2(0f, 1f), new Vector2(0f, 1f), position, size);
            fill.type = UnityEngine.UI.Image.Type.Filled;
            fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            fill.preserveAspect = false;
            fill.raycastTarget = false;
            return fill;
        }

        // ------------------------------------------------------------------ results

        private static void BuildResults(Transform canvas, AsteroidsUI ui)
        {
            CanvasGroup screen = Screen(canvas, "Results");
            Transform root = screen.transform;
            ui.resultsScreen = screen;
            Image dim = Image(root, "Dim", null, new Color(0f, 0.01f, 0.04f, 0.6f), Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            StretchFull(dim.rectTransform);

            RectTransform panelRect = Panel(root, "Panel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(980f, 880f));
            ui.resultTitle = Text(panelRect, "Title", "MISSION COMPLETE", 76f, Green, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(940f, 100f), titleFont);
            ui.resultTitle.textWrappingMode = TextWrappingModes.NoWrap;
            ui.resultTitle.enableAutoSizing = true;
            ui.resultTitle.fontSizeMin = 40f;
            ui.resultTitle.fontSizeMax = 76f;
            ui.resultTitle.characterSpacing = 6f;
            ui.resultSubtitle = Text(panelRect, "Subtitle", "FIRST LIGHT", 30f, Cyan, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -128f), new Vector2(900f, 44f), hudFont);
            ui.resultSubtitle.characterSpacing = 12f;
            RectTransform stars = Rect(panelRect, "Stars", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -180f), new Vector2(620f, 180f));
            var starImages = new List<Image>();
            foreach ((float x, float y, float size) in new[] { (-190f, -20f, 140f), (0f, 10f, 170f), (190f, -20f, 140f) })
            {
                Image star = Image(stars, $"Star{starImages.Count + 1}", AsteroidsArtBuilder.Icon("StarEmpty"), Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(size, size));
                star.raycastTarget = false;
                starImages.Add(star);
            }
            ui.resultStars = starImages.ToArray();
            ui.resultScore = Text(panelRect, "Score", "0", 66f, Color.white, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -370f), new Vector2(900f, 84f), titleFont);
            ui.resultStats = Text(panelRect, "Stats", "Stats", 28f, Soft, TextAlignmentOptions.Top, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -462f), new Vector2(900f, 130f), null, false);
            ui.resultGoals = Text(panelRect, "Goals", "Goals", 28f, Soft, TextAlignmentOptions.Top, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -604f), new Vector2(900f, 140f), hudFont);
            RectTransform buttons = Rect(panelRect, "Buttons", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(900f, 96f));
            var row = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 22f;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = false;
            row.childControlHeight = false;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            ui.missionsButton = Button(buttons, "Missions", "Missions", AsteroidsArtBuilder.Icon("Levels"), Blue, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(270f, 92f), 32f, out _);
            ui.retryButton = Button(buttons, "Retry", "Retry", AsteroidsArtBuilder.Icon("Retry"), Orange, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(270f, 92f), 32f, out _);
            ui.nextButton = Button(buttons, "Next", "Next", AsteroidsArtBuilder.Icon("Play"), Green, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(270f, 92f), 32f, out TextMeshProUGUI nextLabel);
            ui.nextLabel = nextLabel;
        }

        // ------------------------------------------------------------------ widgets

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
            Image image = Image(parent, name, panel, color ?? PanelColor, anchor, pivot, position, size, true);
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

        private static Image[] StarRow(Transform parent, string name, Vector2 anchor, Vector2 position, float size, float spacing)
        {
            RectTransform row = Rect(parent, name, anchor, new Vector2(0f, 0.5f), position, new Vector2(spacing * 3f, size));
            var stars = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                stars[i] = Image(row, $"Star{i + 1}", AsteroidsArtBuilder.Icon("StarEmpty"), Color.white, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(i * spacing, 0f), new Vector2(size, size));
                stars[i].raycastTarget = false;
            }
            return stars;
        }

        private static Button Button(Transform parent, string name, string label, Sprite icon, Color color, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size,
            float fontSize, out TextMeshProUGUI text)
        {
            Image background = Image(parent, name, button, color, anchor, pivot, position, size, true);
            var component = background.gameObject.AddComponent<Button>();
            component.targetGraphic = background;
            component.colors = ButtonColors();
            float iconSize = size.y * 0.5f;
            float labelOffset = 0f;
            if (icon != null)
            {
                Image image = Image(background.transform, "Icon", icon, Color.white, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(size.y * 0.55f, 0f), new Vector2(iconSize, iconSize));
                image.raycastTarget = false;
                labelOffset = size.y * 0.32f;
            }
            text = Text(background.transform, "Label", label, fontSize, Color.white, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(labelOffset, 2f), new Vector2(size.x - labelOffset * 2f, size.y), hudFont);
            text.characterSpacing = 4f;
            return component;
        }

        private static Button TextButton(Transform parent, string name, string label, Vector2 anchor, Vector2 position, Vector2 size, float fontSize, out TextMeshProUGUI text)
        {
            RectTransform rect = Rect(parent, name, anchor, new Vector2(1f, 0.5f), position, size);
            var hit = rect.gameObject.AddComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0f);
            var component = rect.gameObject.AddComponent<Button>();
            component.targetGraphic = hit;
            text = Text(rect, "Label", label, fontSize, Dim, TextAlignmentOptions.Right, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size, null);
            text.fontStyle = FontStyles.Underline;
            return component;
        }

        private static ColorBlock ButtonColors()
        {
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            colors.selectedColor = Color.white;
            colors.pressedColor = new Color(0.75f, 0.75f, 0.8f, 1f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.5f, 0.7f);
            colors.colorMultiplier = 1.2f;
            colors.fadeDuration = 0.08f;
            return colors;
        }
    }
}
