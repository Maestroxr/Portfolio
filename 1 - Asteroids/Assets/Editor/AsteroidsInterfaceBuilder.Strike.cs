using System.Collections.Generic;
using Gamebox.Editor;
using Gamebox.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Asteroids.EditorTools
{
    /// <summary>The strike interface: the PLANET STRIKE tab, the Supply Room, the strike HUD and results.</summary>
    internal static partial class AsteroidsInterfaceBuilder
    {
        private static readonly Color StrikeAccent = Classic.strikeAccent;
        private static readonly Color TabIdle = Classic.tabIdle;

        /// <summary>The shop cards: one per item of the armory, whatever the list shows.</summary>
        private const int ShopCardCount = 18;

        /// <summary>
        /// Builds the <see cref="StrikeUI"/> on the Asteroids canvas and wires it to <paramref name="ui"/>: the strike tab with
        /// <paramref name="missionCount"/> missions in <paramref name="sectorCount"/> sectors, the Supply Room, the strike
        /// HUD and touch buttons, the strike results.
        /// </summary>
        public static void BuildStrike(AsteroidsUI ui, Transform canvas, Camera camera, int missionCount, int sectorCount)
        {
            if (ui == null || canvas == null || ui.titleScreen == null || ui.hudScreen == null || ui.resultsScreen == null)
            {
                return;
            }
            titleFont = AsteroidsSceneBuilder.TitleFont;
            hudFont = AsteroidsSceneBuilder.HudFont;
            panel = AsteroidsArtBuilder.Interface("Panel");
            button = AsteroidsArtBuilder.Interface("Button");

            var strike = canvas.gameObject.AddComponent<StrikeUI>();
            strike.ui = ui;
            ui.strike = strike;
            strike.starFull = AsteroidsArtBuilder.Icon("Star");
            strike.starEmpty = AsteroidsArtBuilder.Icon("StarEmpty");
            strike.itemSprites = ItemSprites();

            // Until the campaign has its strike missions the map is laid out for the planned 3 sectors of 3.
            int sectors = sectorCount > 0 ? sectorCount : 3;
            int missions = missionCount > 0 ? missionCount : 9;

            Transform titleSafe = ui.titleScreen.transform.Find("SafeArea");
            Transform hudSafe = ui.hudScreen.transform.Find("SafeArea");
            Transform resultSafe = ui.resultsScreen.transform.Find("SafeArea");
            BuildModeTabs(titleSafe != null ? titleSafe : ui.titleScreen.transform, ui);
            BuildStrikeSelect(ui.titleScreen.transform, strike, missions, sectors);
            BuildSupply(ui.titleScreen.transform, strike);
            BuildStrikeHud(ui.hudScreen.transform, hudSafe, strike);
            BuildStrikeResults(ui.resultsScreen.transform, strike);
            BuildStrikeTouch(ui, strike);

            ui.fieldMenuParts = Children(titleSafe, "Details", "SectorMap", "Stars", "Controls", "TouchControls");
            ui.fieldHudParts = Children(hudSafe, "Score", "Mission", "Objective", "ObjectiveBack", "ObjectiveFill", "Lives", "ShipStatus", "Dash", "DashRing",
                "Weapon", "PowerUps");
            // In local co-op the pilots list stands in for the parts about one ship.
            ui.shipHudParts = Children(hudSafe, "Lives", "ShipStatus", "Dash", "DashRing", "Weapon", "PowerUps");
            ui.fieldResultParts = Children(resultSafe, "Panel");
        }


        /// <summary>The direct children of <paramref name="root"/> with these names.</summary>
        private static GameObject[] Children(Transform root, params string[] names)
        {
            var found = new List<GameObject>();
            if (root == null)
            {
                return found.ToArray();
            }
            foreach (string name in names)
            {
                Transform child = root.Find(name);
                if (child != null)
                {
                    found.Add(child.gameObject);
                }
            }
            return found.ToArray();
        }


        /// <summary>An icon per <see cref="StrikeItem"/>: the item's own icon when the art has one, else the closest field icon.</summary>
        private static Sprite[] ItemSprites()
        {
            var items = (StrikeItem[])System.Enum.GetValues(typeof(StrikeItem));
            var sprites = new Sprite[(int)StrikeItem.IonScanner + 1];
            foreach (StrikeItem item in items)
            {
                Sprite sprite = AsteroidsArtBuilder.StrikeIcon(item);
                if (sprite == null)
                {
                    sprite = AsteroidsArtBuilder.Icon(item.ToString());
                }
                if (sprite == null)
                {
                    sprite = AsteroidsArtBuilder.Icon(FallbackIcon(item));
                }
                sprites[(int)item] = sprite;
            }
            return sprites;
        }


        /// <summary>A strike icon by name (Art/Strike/Icons), or the field icon <paramref name="fallback"/> when it is missing.</summary>
        private static Sprite StrikeIconOr(string name, string fallback)
        {
            Sprite sprite = AsteroidsArtBuilder.StrikeIcon(name);
            return sprite != null ? sprite : AsteroidsArtBuilder.Icon(fallback);
        }


        private static string FallbackIcon(StrikeItem item)
        {
            switch (item)
            {
                case StrikeItem.MachineGun: return "Blaster";
                case StrikeItem.PlasmaCannon:
                case StrikeItem.LaserTurret:
                case StrikeItem.Deathray:
                case StrikeItem.TwinLaser: return "Laser";
                case StrikeItem.MiniGun:
                case StrikeItem.PulseCannon: return "Scatter";
                case StrikeItem.Bombs:
                case StrikeItem.MegaBomb: return "Nova";
                case StrikeItem.EnergyModule: return "Repair";
                case StrikeItem.PhaseShield: return "Shield";
                case StrikeItem.IonScanner:
                case StrikeItem.PowerDisrupter: return "Warning";
                default: return "Missile";
            }
        }

        // ------------------------------------------------------------------ mission select

        /// <summary>The ASTEROID FIELD and PLANET STRIKE tabs under the logo, in place of its subtitle.</summary>
        private static void BuildModeTabs(Transform safe, AsteroidsUI ui)
        {
            Transform subtitle = safe.Find("Subtitle");
            if (subtitle != null)
            {
                subtitle.gameObject.SetActive(false);
            }
            RectTransform tabs = Rect(safe, "ModeTabs", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60f, -148f), new Vector2(660f, 58f));
            ui.fieldTab = Button(tabs, "FieldTab", "ASTEROID FIELD", AsteroidsArtBuilder.Icon("Asteroid"), Blue, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                Vector2.zero, new Vector2(320f, 58f), 22f, out _);
            ui.strikeTab = Button(tabs, "StrikeTab", "PLANET STRIKE", StrikeIconOr("Strike", "Ship"), TabIdle, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(336f, 0f), new Vector2(320f, 58f), 22f, out _);
        }


        private static void BuildStrikeSelect(Transform titleScreen, StrikeUI strike, int missionCount, int sectorCount)
        {
            CanvasGroup screen = Screen(titleScreen, "StrikeSelect");
            // Behind everything else of the title (the logo, the tabs and the buttons shared by both tabs), and so below
            // the hangar, which opens over either tab.
            screen.transform.SetSiblingIndex(0);
            strike.missionSelect = screen;
            // The ground scrolls behind the menu in daylight: a flat scrim keeps the map and the texts readable over it.
            Image scrim = Image(screen.transform, "Scrim", null, new Color(0f, 0.02f, 0.06f, 0.55f), Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            StretchFull(scrim.rectTransform);
            scrim.raycastTarget = false;
            Transform root = UIBuildUtils.CreateSafeArea(screen.transform);

            // Wallet and stars
            RectTransform wallet = Panel(root, "Wallet", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-410f, -40f), new Vector2(360f, 96f));
            Image(wallet, "Icon", StrikeIconOr("Money", "Crystal"), Color.white, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(52f, 0f), new Vector2(60f, 60f)).raycastTarget = false;
            strike.walletText = Text(wallet, "Money", "$10,000", 42f, Color.white, TextAlignmentOptions.Left, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(96f, 0f),
                new Vector2(250f, 64f), titleFont);
            AutoSize(strike.walletText, 24f, 42f);
            RectTransform stars = Panel(root, "StrikeStars", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-60f, -40f), new Vector2(330f, 96f));
            Image(stars, "Icon", AsteroidsArtBuilder.Icon("Star"), Color.white, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(58f, 0f), new Vector2(72f, 72f)).raycastTarget = false;
            strike.starsTotal = Text(stars, "Total", "0 / 27", 48f, Color.white, TextAlignmentOptions.Left, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(110f, 0f),
                new Vector2(210f, 70f), titleFont);

            // Mission details
            RectTransform details = Panel(root, "StrikeDetails", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(60f, -30f), new Vector2(620f, 660f));
            Image accent = Image(details, "Accent", AsteroidsArtBuilder.Interface("Bar"), StrikeAccent, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(8f, 600f), true);
            accent.raycastTarget = false;
            strike.detailAccent = accent;
            strike.detailSector = Text(details, "Sector", "ARES FLATS - MISSION 1", 24f, StrikeAccent, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(40f, -26f), new Vector2(560f, 34f), hudFont);
            strike.detailSector.characterSpacing = 4f;
            strike.detailTitle = Text(details, "Title", "Dust Devil", 54f, Color.white, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -62f),
                new Vector2(560f, 70f), titleFont);
            AutoSize(strike.detailTitle, 32f, 54f);
            strike.detailStars = StarRow(details, "Stars", new Vector2(0f, 1f), new Vector2(40f, -145f), 54f, 62f);
            strike.detailDescription = Text(details, "Description", "Description", 23f, Soft, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(40f, -198f), new Vector2(550f, 92f), null, false);
            strike.detailBoss = Text(details, "Boss", "BOSS  Sand Crawler", 24f, Color.white, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(40f, -294f), new Vector2(550f, 34f), null);
            strike.detailGoals = Text(details, "Goals", "Goals", 21f, Soft, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -334f),
                new Vector2(550f, 90f), null, false);
            Text(details, "DifficultyLabel", "DIFFICULTY", 18f, Dim, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -434f),
                new Vector2(140f, 40f), hudFont).characterSpacing = 2f;
            var difficulty = new List<Button>();
            string[] levels = { "ROOKIE", "VETERAN", "ELITE" };
            for (int i = 0; i < levels.Length; i++)
            {
                difficulty.Add(Button(details, levels[i], levels[i], null, TabIdle, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(180f + i * 132f, -430f),
                    new Vector2(124f, 46f), 18f, out _));
            }
            strike.difficultyButtons = difficulty.ToArray();
            strike.detailDifficulty = Text(details, "DifficultyText", "The campaign as it was meant to be flown.", 19f, Soft, TextAlignmentOptions.Left, new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(40f, -482f), new Vector2(550f, 28f), null, false);
            strike.detailBest = Text(details, "Best", "Not flown yet", 22f, Gold, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -506f),
                new Vector2(550f, 30f), null);
            strike.supplyButton = Button(details, "Supply", "SUPPLY", StrikeIconOr("SupplyRoom", "Hangar"), Blue, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(40f, 26f),
                new Vector2(250f, 96f), 30f, out _);
            strike.launchButton = Button(details, "Launch", "LAUNCH", AsteroidsArtBuilder.Icon("Play"), Green, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 26f),
                new Vector2(270f, 96f), 38f, out TextMeshProUGUI launchLabel);
            strike.launchLabel = launchLabel;

            // Sector map: a column per sector, its missions zig-zagging down it.
            RectTransform map = Rect(root, "StrikeMap", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-60f, -30f), new Vector2(1150f, 660f));
            const float gap = 20f;
            float column = (1150f - gap * (sectorCount - 1)) / sectorCount;
            int perSector = Mathf.Max(1, Mathf.CeilToInt(missionCount / (float)sectorCount));
            var nodes = new List<MissionNode>();
            var titles = new List<TMP_Text>();
            var gates = new List<TMP_Text>();
            var columns = new List<GameObject>();
            for (int s = 0; s < sectorCount; s++)
            {
                float x = s * (column + gap);
                RectTransform header = Panel(map, $"Sector{s + 1}", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, 0f), new Vector2(column, 84f), new Color(0.9f, 0.55f, 0.3f, 0.9f));
                TextMeshProUGUI title = Text(header, "Title", "SECTOR", 24f, StrikeAccent, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -10f), new Vector2(column - 16f, 36f), hudFont);
                title.textWrappingMode = TextWrappingModes.NoWrap;
                AutoSize(title, 15f, 24f);
                titles.Add(title);
                gates.Add(Text(header, "Gate", "OPEN", 17f, Dim, TextAlignmentOptions.Center, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f),
                    new Vector2(column - 16f, 28f), null));
                Image line = Image(map, $"Path{s + 1}", AsteroidsArtBuilder.Interface("Bar"), new Color(1f, 0.7f, 0.4f, 0.25f), new Vector2(0f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(x + column * 0.5f, -150f), new Vector2(6f, 340f), true);
                line.raycastTarget = false;
                columns.Add(header.gameObject);
                columns.Add(line.gameObject);
                for (int m = 0; m < perSector && nodes.Count < missionCount; m++)
                {
                    float offset = m % 2 == 0 ? -60f : 60f;
                    nodes.Add(HexNode(map, nodes.Count, new Vector2(x + column * 0.5f + offset, -176f - m * 168f)));
                }
            }
            strike.missionNodes = nodes.ToArray();
            strike.sectorTitles = titles.ToArray();
            strike.sectorGates = gates.ToArray();
            strike.sectorColumns = columns.ToArray();
            TextMeshProUGUI empty = Text(root, "Empty", "The strike campaign has no missions yet.", 34f, Soft, TextAlignmentOptions.Center, new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f), new Vector2(-60f, -30f), new Vector2(1150f, 90f), hudFont);
            empty.gameObject.SetActive(false);
            strike.emptyNote = empty.gameObject;

            TextMeshProUGUI keys = Text(root, "StrikeControls", "W A S D / ARROWS fly    SPACE fire    SHIFT weapon    B megabomb    ESC pause", 21f, Dim, TextAlignmentOptions.Left,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(60f, 40f), new Vector2(570f, 70f), null, false);
            UIBuildUtils.ShowOnly(keys.gameObject, TouchLayout.Visibility.WithoutTouch);
            TextMeshProUGUI thumbs = Text(root, "StrikeTouchControls", "LEFT THUMB fly    HOLD FIRE to shoot    WEAPON cycles    MEGA bomb    BACK pause", 21f, Dim,
                TextAlignmentOptions.Left, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(60f, 40f), new Vector2(570f, 70f), null, false);
            UIBuildUtils.ShowOnly(thumbs.gameObject, TouchLayout.Visibility.TouchOnly);
            screen.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ Supply Room

        private static void BuildSupply(Transform titleScreen, StrikeUI strike)
        {
            CanvasGroup screen = Screen(titleScreen, "Supply");
            strike.supply = screen;
            Image dim = Image(screen.transform, "Dim", null, new Color(0f, 0.01f, 0.04f, 0.96f), Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            StretchFull(dim.rectTransform);
            Transform root = UIBuildUtils.CreateSafeArea(screen.transform);

            TextMeshProUGUI title = Text(root, "Title", "SUPPLY ROOM", 76f, Color.white, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -30f), new Vector2(1000f, 100f), titleFont);
            title.characterSpacing = 16f;
            Gradient(title, new Color(1f, 0.92f, 0.7f), new Color(1f, 0.55f, 0.2f));
            strike.quartermaster = Text(root, "Quartermaster", "Welcome back, pilot. What'll it be?", 26f, Soft, TextAlignmentOptions.Center, new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -128f), new Vector2(1100f, 40f), null, false);
            strike.quartermaster.fontStyle = FontStyles.Italic;

            RectTransform money = Panel(root, "Money", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-60f, -40f), new Vector2(400f, 100f));
            Text(money, "Label", "CREDITS", 18f, Dim, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(26f, -10f), new Vector2(200f, 26f), hudFont)
                .characterSpacing = 8f;
            Image(money, "Icon", StrikeIconOr("Money", "Crystal"), Color.white, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(50f, -12f), new Vector2(46f, 46f)).raycastTarget = false;
            strike.supplyMoney = Text(money, "Digits", "0010000", 50f, Money, TextAlignmentOptions.Left, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(86f, -12f),
                new Vector2(290f, 60f), titleFont);
            strike.supplyMoney.characterSpacing = 10f;

            strike.buyTab = Button(root, "BuyTab", "BUY", StrikeIconOr("Money", "Crystal"), StrikeAccent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60f, -188f),
                new Vector2(220f, 64f), 28f, out _);
            strike.sellTab = Button(root, "SellTab", "SELL", AsteroidsArtBuilder.Icon("Retry"), TabIdle, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(296f, -188f),
                new Vector2(220f, 64f), 28f, out _);

            RectTransform items = Rect(root, "Items", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60f, -270f), new Vector2(1200f, 620f));
            var grid = items.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(388f, 92f);
            grid.spacing = new Vector2(18f, 12f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;
            var cards = new List<ShopCard>();
            for (int i = 0; i < ShopCardCount; i++)
            {
                cards.Add(ShopCard(items, i));
            }
            strike.shopCards = cards.ToArray();
            strike.supplyEmpty = Text(root, "Empty", "Nothing to sell.", 30f, Soft, TextAlignmentOptions.Center, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(60f, -300f), new Vector2(1200f, 60f), hudFont);
            strike.supplyEmpty.gameObject.SetActive(false);

            // The item
            RectTransform item = Panel(root, "Item", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-60f, -188f), new Vector2(560f, 580f));
            Image glow = Image(item, "Glow", AsteroidsArtBuilder.Interface("Glow"), new Color(1f, 0.6f, 0.3f, 0.3f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -96f), new Vector2(220f, 220f));
            glow.raycastTarget = false;
            strike.itemIcon = Image(item, "Icon", null, Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -96f), new Vector2(120f, 120f));
            strike.itemIcon.raycastTarget = false;
            strike.itemTitle = Text(item, "Title", "Item", 30f, Color.white, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -170f),
                new Vector2(520f, 44f), titleFont);
            strike.itemTitle.textWrappingMode = TextWrappingModes.NoWrap;
            AutoSize(strike.itemTitle, 18f, 30f);
            strike.itemGroup = Text(item, "Group", "SPECIAL WEAPON", 19f, Cyan, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -216f),
                new Vector2(520f, 28f), hudFont);
            strike.itemGroup.characterSpacing = 4f;
            strike.itemDescription = Text(item, "Description", "Description", 22f, Soft, TextAlignmentOptions.Top, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -254f), new Vector2(500f, 90f), null, false);
            strike.itemPrice = Text(item, "Price", "PRICE  $0", 26f, Gold, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -352f),
                new Vector2(520f, 36f), hudFont);
            strike.itemOwned = Text(item, "Owned", "Not owned", 22f, Soft, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -394f),
                new Vector2(520f, 32f), null);
            strike.actionButton = Button(item, "Action", "BUY", AsteroidsArtBuilder.Icon("Check"), Green, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 26f),
                new Vector2(320f, 92f), 38f, out TextMeshProUGUI actionLabel);
            strike.actionLabel = actionLabel;

            // The pilot
            RectTransform status = Panel(root, "Pilot", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-60f, 150f), new Vector2(560f, 110f));
            Image(status, "EnergyIcon", StrikeIconOr("Energy", "Repair"), Color.white, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(40f, -34f), new Vector2(38f, 38f))
                .raycastTarget = false;
            strike.supplyEnergyFill = Meter(status, "Energy", new Vector2(70f, -26f), new Vector2(360f, 18f), new Color(0.35f, 1f, 0.55f));
            strike.supplyEnergyText = Text(status, "EnergyValue", "75", 26f, Color.white, TextAlignmentOptions.Right, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -14f),
                new Vector2(100f, 40f), hudFont);
            Image(status, "ShieldIcon", AsteroidsArtBuilder.Icon("Shield"), Color.white, new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(40f, 32f), new Vector2(38f, 38f))
                .raycastTarget = false;
            strike.supplyShields = Text(status, "Shields", "x0", 26f, Color.white, TextAlignmentOptions.Left, new Vector2(0f, 0f), new Vector2(0f, 0.5f), new Vector2(70f, 32f),
                new Vector2(120f, 40f), hudFont);
            Image(status, "MegabombIcon", AsteroidsArtBuilder.Icon("Nova"), Color.white, new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(230f, 32f), new Vector2(38f, 38f))
                .raycastTarget = false;
            strike.supplyMegabombs = Text(status, "Megabombs", "x0", 26f, Color.white, TextAlignmentOptions.Left, new Vector2(0f, 0f), new Vector2(0f, 0.5f), new Vector2(260f, 32f),
                new Vector2(120f, 40f), hudFont);

            strike.supplyBackButton = Button(root, "Back", "Back", AsteroidsArtBuilder.Icon("Levels"), Orange, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f),
                new Vector2(300f, 86f), 34f, out _);
            screen.gameObject.SetActive(false);
        }


        private static ShopCard ShopCard(Transform parent, int index)
        {
            RectTransform rect = Rect(parent, $"Card{index + 1}", new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(388f, 92f));
            var card = rect.gameObject.AddComponent<ShopCard>();
            Image background = Image(rect, "Background", button, new Color(0.14f, 0.3f, 0.5f, 1f), Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, true);
            StretchFull(background.rectTransform);
            Image highlight = Image(rect, "Highlight", AsteroidsArtBuilder.Interface("Frame"), StrikeAccent, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, true);
            StretchFull(highlight.rectTransform);
            highlight.rectTransform.offsetMin = new Vector2(-6f, -6f);
            highlight.rectTransform.offsetMax = new Vector2(6f, 6f);
            highlight.raycastTarget = false;
            highlight.enabled = false;
            var buttonComponent = rect.gameObject.AddComponent<Button>();
            buttonComponent.targetGraphic = background;
            buttonComponent.colors = ButtonColors();
            card.button = buttonComponent;
            card.background = background;
            card.highlight = highlight;
            card.icon = Image(rect, "Icon", null, Color.white, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(46f, 0f), new Vector2(60f, 60f));
            card.icon.raycastTarget = false;
            card.title = Text(rect, "Title", "Item", 21f, Color.white, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(88f, -8f),
                new Vector2(286f, 34f), hudFont);
            card.title.textWrappingMode = TextWrappingModes.NoWrap;
            AutoSize(card.title, 13f, 21f);
            card.price = Text(rect, "Price", "$0", 22f, Gold, TextAlignmentOptions.Left, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(88f, 10f),
                new Vector2(170f, 32f), hudFont);
            card.owned = Text(rect, "Owned", string.Empty, 18f, Soft, TextAlignmentOptions.Right, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-14f, 12f),
                new Vector2(120f, 28f), null);
            return card;
        }

        // ------------------------------------------------------------------ HUD

        private static void BuildStrikeHud(Transform hudScreen, Transform fieldSafe, StrikeUI strike)
        {
            CanvasGroup screen = Screen(hudScreen, "StrikeHud");
            // Under the field HUD's own layer (announcements, countdown, touch controls), above the damage flash.
            if (fieldSafe != null)
            {
                screen.transform.SetSiblingIndex(fieldSafe.GetSiblingIndex());
            }
            screen.blocksRaycasts = false;
            screen.interactable = false;
            strike.hud = screen;
            Transform root = UIBuildUtils.CreateSafeArea(screen.transform);

            // Money and progress (top centre)
            RectTransform money = Panel(root, "Money", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(540f, 92f));
            Image(money, "Icon", StrikeIconOr("Money", "Crystal"), Color.white, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(46f, 0f), new Vector2(50f, 50f)).raycastTarget = false;
            strike.hudWallet = Text(money, "Wallet", "$10,000", 42f, Color.white, TextAlignmentOptions.Left, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(84f, 0f),
                new Vector2(260f, 60f), hudFont);
            AutoSize(strike.hudWallet, 24f, 42f);
            strike.hudEarned = Text(money, "Earned", "+$0", 30f, Gold, TextAlignmentOptions.Right, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24f, 0f),
                new Vector2(190f, 50f), hudFont);
            AutoSize(strike.hudEarned, 18f, 30f);
            Image progressBack = Image(root, "ProgressBack", AsteroidsArtBuilder.Interface("Bar"), new Color(1f, 1f, 1f, 0.12f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -122f), new Vector2(520f, 10f), true);
            progressBack.raycastTarget = false;
            Image progress = Image(root, "Progress", AsteroidsArtBuilder.Interface("Bar"), StrikeAccent, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -122f),
                new Vector2(520f, 10f));
            progress.type = UnityEngine.UI.Image.Type.Filled;
            progress.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            progress.preserveAspect = false;
            progress.raycastTarget = false;
            progress.fillAmount = 0f;
            strike.progressFill = progress;
            Image(root, "BossMark", AsteroidsArtBuilder.Icon("Skull"), new Color(1f, 0.5f, 0.45f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(282f, -127f),
                new Vector2(30f, 30f)).raycastTarget = false;

            // The special weapon (top right, left of the pause button)
            RectTransform special = Panel(root, "Special", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-130f, -24f), new Vector2(400f, 92f));
            strike.specialIcon = Image(special, "Icon", AsteroidsArtBuilder.Icon("Missile"), Color.white, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(52f, 0f),
                new Vector2(66f, 66f));
            strike.specialIcon.raycastTarget = false;
            strike.specialText = Text(special, "Name", "NO SPECIAL", 26f, StrikeAccent, TextAlignmentOptions.Left, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(98f, 4f),
                new Vector2(284f, 40f), hudFont);
            strike.specialText.textWrappingMode = TextWrappingModes.NoWrap;
            AutoSize(strike.specialText, 15f, 26f);
            TextMeshProUGUI cycleKey = Text(special, "Key", "SHIFT  next", 15f, Dim, TextAlignmentOptions.Left, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(98f, 6f),
                new Vector2(200f, 22f), null);
            UIBuildUtils.ShowOnly(cycleKey.gameObject, TouchLayout.Visibility.WithoutTouch);

            // Phase shields (top left) and the shield in use (left edge)
            RectTransform phase = Rect(root, "PhaseShields", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -26f), new Vector2(StrikeRules.MaxPhaseShields * 50f, 46f));
            var phaseIcons = new List<Image>();
            for (int i = 0; i < StrikeRules.MaxPhaseShields; i++)
            {
                Image icon = Image(phase, $"Shield{i + 1}", AsteroidsArtBuilder.Icon("Shield"), new Color(1f, 1f, 1f, 0.15f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(i * 50f, 0f), new Vector2(44f, 44f));
                icon.raycastTarget = false;
                phaseIcons.Add(icon);
            }
            strike.phaseIcons = phaseIcons.ToArray();
            strike.phaseFill = VerticalMeter(root, "Phase", new Vector2(0f, 0.5f), new Vector2(26f, -20f), new Color(0.45f, 0.85f, 1f), "Shield");

            // Energy (right edge)
            strike.energyFill = VerticalMeter(root, "Energy", new Vector2(1f, 0.5f), new Vector2(-26f, -20f), new Color(0.35f, 1f, 0.55f), "Repair");
            strike.energyText = Text(root, "EnergyValue", "75", 24f, Color.white, TextAlignmentOptions.Center, new Vector2(1f, 0.5f), new Vector2(0.5f, 1f), new Vector2(-36f, -252f),
                new Vector2(70f, 32f), hudFont);

            // Megabombs (bottom left; under the special weapon with touch, where the stick is)
            RectTransform bombs = Rect(root, "Megabombs", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(64f, 24f), new Vector2(StrikeRules.MaxMegabombs * 48f + 70f, 60f));
            UIBuildUtils.MoveForTouch(bombs, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-130f, -126f));
            var bombIcons = new List<Image>();
            for (int i = 0; i < StrikeRules.MaxMegabombs; i++)
            {
                Image bomb = Image(bombs, $"Megabomb{i + 1}", AsteroidsArtBuilder.Icon("Nova"), new Color(1f, 1f, 1f, 0.15f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(i * 48f, 0f), new Vector2(42f, 42f));
                bomb.raycastTarget = false;
                bombIcons.Add(bomb);
            }
            strike.megabombIcons = bombIcons.ToArray();
            Image ring = Image(bombs, "Ready", AsteroidsArtBuilder.Interface("TimerRing"), Gold, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(StrikeRules.MaxMegabombs * 48f + 30f, 0f), new Vector2(48f, 48f));
            ring.type = UnityEngine.UI.Image.Type.Filled;
            ring.fillMethod = UnityEngine.UI.Image.FillMethod.Radial360;
            ring.fillOrigin = (int)UnityEngine.UI.Image.Origin360.Top;
            ring.preserveAspect = false;
            ring.raycastTarget = false;
            strike.megabombRing = ring;
            TextMeshProUGUI bombKey = Text(bombs, "Key", "B", 18f, Dim, TextAlignmentOptions.Center, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(StrikeRules.MaxMegabombs * 48f + 30f, 0f), new Vector2(40f, 24f), null);
            UIBuildUtils.ShowOnly(bombKey.gameObject, TouchLayout.Visibility.WithoutTouch);

            // Boss bar (bottom centre; top with touch)
            RectTransform boss = Rect(root, "Boss", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(980f, 70f));
            UIBuildUtils.MoveForTouch(boss, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -146f));
            Image(boss, "Skull", AsteroidsArtBuilder.Icon("Skull"), Color.white, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(30f, 0f), new Vector2(56f, 56f)).raycastTarget = false;
            strike.bossName = Text(boss, "Name", "BOSS", 24f, new Color(1f, 0.55f, 0.5f), TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(70f, 2f),
                new Vector2(600f, 30f), hudFont);
            strike.bossName.characterSpacing = 6f;
            Image bossBack = Image(boss, "Back", AsteroidsArtBuilder.Interface("Bar"), new Color(0.3f, 0.05f, 0.08f, 0.8f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(70f, 8f),
                new Vector2(900f, 22f), true);
            bossBack.raycastTarget = false;
            Image bossFill = Image(boss, "Fill", AsteroidsArtBuilder.Interface("Bar"), new Color(1f, 0.25f, 0.3f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(70f, 8f),
                new Vector2(900f, 22f));
            bossFill.type = UnityEngine.UI.Image.Type.Filled;
            bossFill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            bossFill.preserveAspect = false;
            bossFill.raycastTarget = false;
            strike.bossFill = bossFill;
            strike.bossNoScanner = Text(boss, "NoScanner", "NO SCANNER", 18f, new Color(1f, 0.75f, 0.7f, 0.8f), TextAlignmentOptions.Center, new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(70f, 4f), new Vector2(900f, 28f), hudFont);
            strike.bossNoScanner.characterSpacing = 10f;
            strike.bossBar = boss.gameObject;
            boss.gameObject.SetActive(false);

            // Warnings
            TextMeshProUGUI warning = Text(root, "Warning", "SHIELD LOW", 64f, new Color(1f, 0.32f, 0.28f), TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(1200f, 90f), titleFont);
            warning.characterSpacing = 10f;
            warning.gameObject.SetActive(false);
            strike.warningText = warning;
            // In local co-op the pilots list stands in for the parts about one ship.
            strike.shipHudParts = Children(root, "Special", "PhaseShields", "PhaseBack", "PhaseFill", "PhaseIcon", "EnergyBack", "EnergyFill", "EnergyIcon",
                "EnergyValue", "Megabombs");
            screen.gameObject.SetActive(false);
        }


        /// <summary>A vertical bar at a side of the screen with its icon above it; returns the fill.</summary>
        private static Image VerticalMeter(Transform parent, string name, Vector2 anchor, Vector2 position, Color color, string icon)
        {
            var pivot = new Vector2(anchor.x, 0.5f);
            Image back = Image(parent, $"{name}Back", AsteroidsArtBuilder.Interface("Bar"), new Color(1f, 1f, 1f, 0.12f), anchor, pivot, position, new Vector2(20f, 420f), true);
            back.raycastTarget = false;
            Image fill = Image(parent, $"{name}Fill", AsteroidsArtBuilder.Interface("Bar"), color, anchor, pivot, position, new Vector2(20f, 420f));
            fill.type = UnityEngine.UI.Image.Type.Filled;
            fill.fillMethod = UnityEngine.UI.Image.FillMethod.Vertical;
            fill.fillOrigin = (int)UnityEngine.UI.Image.OriginVertical.Bottom;
            fill.preserveAspect = false;
            fill.raycastTarget = false;
            float side = anchor.x < 0.5f ? 1f : -1f;
            Image(parent, $"{name}Icon", AsteroidsArtBuilder.Icon(icon), Color.white, anchor, new Vector2(0.5f, 0f), new Vector2(position.x + side * 10f, position.y + 218f),
                new Vector2(36f, 36f)).raycastTarget = false;
            return fill;
        }

        // ------------------------------------------------------------------ results

        private static void BuildStrikeResults(Transform resultsScreen, StrikeUI strike)
        {
            CanvasGroup screen = Screen(resultsScreen, "StrikeResults");
            strike.results = screen;
            Transform root = UIBuildUtils.CreateSafeArea(screen.transform);
            RectTransform panelRect = Panel(root, "Panel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 900f));
            strike.resultTitle = Text(panelRect, "Title", "MISSION COMPLETE", 76f, Green, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -30f), new Vector2(940f, 100f), titleFont);
            strike.resultTitle.textWrappingMode = TextWrappingModes.NoWrap;
            AutoSize(strike.resultTitle, 40f, 76f);
            strike.resultTitle.characterSpacing = 6f;
            strike.resultSubtitle = Text(panelRect, "Subtitle", "DUST DEVIL", 30f, StrikeAccent, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -128f), new Vector2(900f, 44f), hudFont);
            strike.resultSubtitle.characterSpacing = 12f;
            RectTransform stars = Rect(panelRect, "Stars", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -176f), new Vector2(620f, 180f));
            var starImages = new List<Image>();
            foreach ((float x, float y, float size) in new[] { (-190f, -20f, 140f), (0f, 10f, 170f), (190f, -20f, 140f) })
            {
                Image star = Image(stars, $"Star{starImages.Count + 1}", AsteroidsArtBuilder.Icon("StarEmpty"), Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(x, y), new Vector2(size, size));
                star.raycastTarget = false;
                starImages.Add(star);
            }
            strike.resultStars = starImages.ToArray();
            strike.resultMoney = Text(panelRect, "Money", "+$0", 64f, Money, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -362f),
                new Vector2(900f, 80f), titleFont);
            strike.resultWallet = Text(panelRect, "Wallet", "WALLET  $0", 26f, Soft, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -442f),
                new Vector2(900f, 36f), hudFont);
            strike.resultStats = Text(panelRect, "Stats", "Stats", 26f, Soft, TextAlignmentOptions.Top, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -490f),
                new Vector2(900f, 80f), null, false);
            strike.resultGoals = Text(panelRect, "Goals", "Goals", 26f, Soft, TextAlignmentOptions.Top, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -584f),
                new Vector2(900f, 150f), hudFont);
            RectTransform buttons = Rect(panelRect, "Buttons", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(960f, 92f));
            var row = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 16f;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = false;
            row.childControlHeight = false;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            strike.resultMissions = Button(buttons, "Missions", "Missions", AsteroidsArtBuilder.Icon("Levels"), Blue, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(220f, 88f), 28f, out TextMeshProUGUI missionsLabel);
            strike.resultMissionsLabel = missionsLabel;
            strike.resultSupply = Button(buttons, "Supply", "Supply", AsteroidsArtBuilder.Icon("Hangar"), StrikeAccent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(220f, 88f), 28f, out _);
            strike.resultRetry = Button(buttons, "Retry", "Retry", AsteroidsArtBuilder.Icon("Retry"), Orange, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(210f, 88f), 28f, out _);
            strike.resultNext = Button(buttons, "Next", "Next", AsteroidsArtBuilder.Icon("Play"), Green, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(210f, 88f), 28f, out TextMeshProUGUI nextLabel);
            strike.resultNextLabel = nextLabel;
            screen.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ touch

        /// <summary>
        /// In strike the touch DASH button cycles the special weapon (WEAPON, showing its icon) and NOVA drops a megabomb
        /// (MEGA); the controls label the buttons themselves.
        /// </summary>
        private static void BuildStrikeTouch(AsteroidsUI ui, StrikeUI strike)
        {
            ShipTouchControls controls = ui.shipControls;
            strike.touch = controls;
            if (controls == null || controls.dash == null)
            {
                return;
            }
            Transform icon = controls.dash.transform.Find("Icon");
            strike.touchCycleIcon = icon != null ? icon.GetComponent<Image>() : null;
            strike.touchDashSprite = strike.touchCycleIcon != null ? strike.touchCycleIcon.sprite : null;
        }


        private static readonly Color Money = new Color(1f, 0.84f, 0.35f);


        private static void AutoSize(TMP_Text text, float min, float max)
        {
            text.enableAutoSizing = true;
            text.fontSizeMin = min;
            text.fontSizeMax = max;
        }
    }
}
