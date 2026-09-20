using System.Collections.Generic;
using Gamebox.Editor;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Portfolio.Monopoly.EditorTools.UIKit;

namespace Portfolio.Monopoly.EditorTools
{
    /// <summary>
    /// Builds the interface of the Monopoly scene: the HUD (four player panels in the corners, the match info, the
    /// news feed and the action panel over the middle of the board), the popups (title deed, card, auction, property
    /// manager, trade, trade offer), the new game and results screens, the title/pause menu with the house rules
    /// panel, and the layers for banners and flying money. Everything is wired to <see cref="MonopolyUI"/> and the
    /// fields of the shared <see cref="Gamebox.UI.GameUI"/>.
    /// </summary>
    internal static class MonopolyInterfaceBuilder
    {
        private static readonly Color Ink = MonopolyStyle.Ink;
        private static readonly Color Muted = MonopolyStyle.Muted;
        private static readonly Color White = Color.white;
        private static readonly Color Soft = MonopolyStyle.Hex(0xEEF1F5);

        public static MonopolyUI Build(MonopolyGameManager manager, MonopolyController controller, CameraRig rig, MonopolyAudio audio, Camera camera)
        {
            var canvasObject = new GameObject("Interface", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.layer = LayerMask.NameToLayer("UI");
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            UIBuildUtils.ConfigureScaler(scaler);
            var ui = canvasObject.AddComponent<MonopolyUI>();
            Transform root = canvasObject.transform;

            // Layers, back to front.
            RectTransform hud = Stretch(Rect(root, "HUD"));
            CanvasGroup hudGroup = Group(hud.gameObject);
            RectTransform floatingLayer = Stretch(Rect(root, "Floating"));
            RectTransform popups = Stretch(Rect(root, "Popups"));
            RectTransform bannerLayer = Stretch(Rect(root, "Banners"));
            Image dim = Image(root, "Dim", null, new Color(0.02f, 0.03f, 0.06f, 0f), false);
            Stretch(dim.rectTransform, -600f, -600f, -600f, -600f);
            RectTransform screens = Stretch(Rect(root, "Screens"));
            RectTransform menu = Stretch(Rect(root, "Menu"));
            TextMeshProUGUI error = Text(root, "ErrorText", "", 24f, MonopolyStyle.Red, Bold);
            Place(error.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(1200f, 40f));

            // The HUD.
            RectTransform safe = UIBuildUtils.CreateSafeArea(hud);
            RectTransform left = Rect(safe, "LeftColumn");
            left.anchorMin = new Vector2(0f, 0f);
            left.anchorMax = new Vector2(0f, 1f);
            left.pivot = new Vector2(0f, 0.5f);
            left.anchoredPosition = new Vector2(18f, 0f);
            left.sizeDelta = new Vector2(400f, -36f);
            RectTransform right = Rect(safe, "RightColumn");
            right.anchorMin = new Vector2(1f, 0f);
            right.anchorMax = new Vector2(1f, 1f);
            right.pivot = new Vector2(1f, 0.5f);
            right.anchoredPosition = new Vector2(-18f, 0f);
            right.sizeDelta = new Vector2(400f, -36f);
            var panels = new PlayerUI[4];
            panels[0] = PlayerPanel(left, 0, true, true);
            panels[1] = PlayerPanel(right, 1, true, false);
            panels[2] = PlayerPanel(right, 2, false, false);
            panels[3] = PlayerPanel(left, 3, false, true);
            Toasts toasts = BuildToasts(left);
            InfoCard info = BuildInfoCard(right);
            ActionPanel actions = BuildActionPanel(hud);

            FloatingTexts floating = BuildFloating(floatingLayer, camera);
            DeedCardUI deed = BuildDeed(popups);
            CardRevealUI cards = BuildCardReveal(popups);
            AuctionUI auction = BuildAuction(popups);
            ManageUI manage = BuildManage(popups);
            TradeUI trade = BuildTrade(popups);
            TradeOfferUI offer = BuildOffer(popups);
            BannerParts banner = BuildBanner(bannerLayer);
            SetupUI setup = BuildSetup(screens);
            ResultsUI results = BuildResults(screens);
            MenuParts parts = BuildMenu(menu, ui);

            // The shared menu fields of GameUI.
            MonopolyAssets.SetObject(ui, "Controller", controller);
            MonopolyAssets.SetObject(ui, "Manager", manager);
            MonopolyAssets.SetObject(ui, "GameSettingsUI", parts.settings);
            MonopolyAssets.SetObject(ui, "MenuRoot", menu);
            MonopolyAssets.SetObject(ui, "MenuButtonsUI", parts.buttons);
            MonopolyAssets.SetObject(ui, "MenuHeader", parts.header);
            MonopolyAssets.SetObject(ui, "Scaler", scaler);
            MonopolyAssets.SetObject(ui, "ErrorConsole", error);
            MonopolyAssets.SetObject(ui, "StartNewGame", parts.start);
            MonopolyAssets.SetObject(ui, "ReturnToGame", parts.resume);
            MonopolyAssets.SetObject(ui, "SaveGame", parts.save);
            MonopolyAssets.SetObject(ui, "LoadGame", parts.load);
            MonopolyAssets.SetObject(ui, "SettingsButton", parts.houseRules);
            MonopolyAssets.SetObject(ui, "ExitButton", parts.exit);
            MonopolyAssets.SetObject(ui, "PauseButton", info.pause);

            // Monopoly's own fields.
            MonopolyAssets.SetObject(ui, "hud", hud);
            MonopolyAssets.SetObject(ui, "hudGroup", hudGroup);
            MonopolyAssets.SetObject(ui, "dim", dim);
            MonopolyAssets.SetObject(ui, "titleLogo", parts.logo);
            MonopolyAssets.SetObject(ui, "menuTitle", parts.pauseTitle);
            MonopolyAssets.SetObject(ui, "mainMenuButton", parts.mainMenu);
            MonopolyAssets.SetObject(ui, "startLabel", LabelOf(parts.start));
            MonopolyAssets.SetObject(ui, "loadLabel", LabelOf(parts.load));
            MonopolyAssets.SetObject(ui, "footer", parts.footer);
            MonopolyAssets.SetObjects(ui, "players", panels);
            MonopolyAssets.SetObject(ui, "leftColumn", left);
            MonopolyAssets.SetObject(ui, "rightColumn", right);
            MonopolyAssets.SetObject(ui, "modeText", info.mode);
            MonopolyAssets.SetObject(ui, "roundText", info.round);
            MonopolyAssets.SetObject(ui, "potRow", info.potRow);
            MonopolyAssets.SetObject(ui, "potText", info.pot);
            MonopolyAssets.SetObject(ui, "bankText", info.bank);
            MonopolyAssets.SetObject(ui, "soundButton", info.sound);
            MonopolyAssets.SetObject(ui, "soundIcon", IconOf(info.sound));
            MonopolyAssets.SetObject(ui, "musicButton", info.music);
            MonopolyAssets.SetObject(ui, "musicIcon", IconOf(info.music));
            MonopolyAssets.SetObject(ui, "banner", banner.root);
            MonopolyAssets.SetObject(ui, "bannerText", banner.text);
            MonopolyAssets.SetObject(ui, "bannerBackground", banner.background);
            MonopolyAssets.SetObject(ui, "actions", actions);
            MonopolyAssets.SetObject(ui, "deed", deed);
            MonopolyAssets.SetObject(ui, "cards", cards);
            MonopolyAssets.SetObject(ui, "auction", auction);
            MonopolyAssets.SetObject(ui, "manage", manage);
            MonopolyAssets.SetObject(ui, "trade", trade);
            MonopolyAssets.SetObject(ui, "offer", offer);
            MonopolyAssets.SetObject(ui, "setup", setup);
            MonopolyAssets.SetObject(ui, "results", results);
            MonopolyAssets.SetObject(ui, "toasts", toasts);
            MonopolyAssets.SetObject(ui, "floating", floating);
            var badges = new Object[MonopolyStyle.TokenCount];
            for (int i = 0; i < badges.Length; i++)
            {
                badges[i] = MonopolyArtBuilder.Badge(i);
            }
            MonopolyAssets.SetObjects(ui, "tokenSprites", badges);
            MonopolyAssets.SetObject(ui, "sound", audio);
            MonopolyAssets.SetObject(ui, "cameraRig", rig);

            // Popups start hidden.
            foreach (Popup popup in new Popup[] { deed, cards, auction, manage, trade, offer, setup, results })
            {
                popup.gameObject.SetActive(false);
            }
            actions.gameObject.SetActive(false);
            return ui;
        }

        /// <summary>
        /// A single line that shrinks to fit its box. (Truncating or ellipsis modes would hide the whole line when the
        /// box is lower than Poppins' tall line height.)
        /// </summary>
        private static void FitOneLine(TextMeshProUGUI text, float minimum)
        {
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.enableAutoSizing = true;
            text.fontSizeMax = text.fontSize;
            text.fontSizeMin = minimum;
        }

        // ------------------------------------------------------------------ HUD

        private static PlayerUI PlayerPanel(RectTransform column, int seat, bool top, bool leftSide)
        {
            RectTransform rect = Rect(column, $"Player {seat + 1}");
            Place(rect, new Vector2(0.5f, top ? 1f : 0f), new Vector2(0.5f, top ? 1f : 0f), Vector2.zero, new Vector2(400f, 176f));
            var panel = rect.gameObject.AddComponent<PlayerUI>();
            CanvasGroup group = Group(rect.gameObject);
            Image glow = Image(rect, "Glow", MonopolyArtBuilder.UI("Glow"), new Color(1f, 1f, 1f, 0f));
            Stretch(glow.rectTransform, -70f, -60f, -70f, -60f);
            Image frame = Card(rect, "Card", MonopolyStyle.Panel, 0.55f);
            Stretch((RectTransform)frame.transform.parent);
            Image accent = Rounded(rect, "Accent", MonopolyStyle.PlayerColor(seat), 0.2f);
            Place(accent.rectTransform, new Vector2(leftSide ? 0f : 1f, 0.5f), new Vector2(leftSide ? 0f : 1f, 0.5f), new Vector2(leftSide ? 8f : -8f, 0f), new Vector2(10f, 150f));

            float x = leftSide ? 0f : 0f;
            Image badge = Image(rect, "Badge", MonopolyArtBuilder.UI("Circle"), MonopolyStyle.PlayerColor(seat));
            Place(badge.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(74f + x, -70f), new Vector2(104f, 104f));
            Image ring = Image(badge.transform, "Ring", MonopolyArtBuilder.UI("Ring"), new Color(1f, 1f, 1f, 0.85f));
            Stretch(ring.rectTransform, 4f, 4f, 4f, 4f);
            Image token = Image(badge.transform, "Token", MonopolyArtBuilder.Badge(seat), White);
            Stretch(token.rectTransform, 16f, 16f, 16f, 16f);
            token.preserveAspect = true;

            TextMeshProUGUI name = Text(rect, "Name", "Player", 27f, Ink, Bold, TextAlignmentOptions.MidlineLeft);
            Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(138f, -10f), new Vector2(150f, 44f));
            FitOneLine(name, 16f);
            TextMeshProUGUI cash = Text(rect, "Cash", "$1,500", 42f, Ink, Heavy, TextAlignmentOptions.MidlineLeft);
            Place(cash.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(138f, -48f), new Vector2(250f, 52f));
            TextMeshProUGUI worth = Text(rect, "Worth", "Net worth $1,500", 17f, Muted, Body, TextAlignmentOptions.MidlineLeft);
            Place(worth.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(138f, -100f), new Vector2(250f, 26f));

            // Tags: computer player, jail, Get Out of Jail Free cards.
            RectTransform tags = Row(rect, "Tags", 6f, TextAnchor.MiddleRight).GetComponent<RectTransform>();
            // Clear of the seat colour bar on the right hand panels.
            Place(tags, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(leftSide ? -14f : -28f, -16f), new Vector2(200f, 30f));
            GameObject cpu = Tag(tags, "CpuTag", Icons.Robot, "NORMAL", Ink, out TextMeshProUGUI cpuText);
            GameObject jail = Tag(tags, "JailTag", Icons.Lock, "JAIL", MonopolyStyle.ChanceOrange, out _);
            GameObject card = Tag(tags, "CardTag", Icons.Ticket, "", MonopolyStyle.Gold, out TextMeshProUGUI cardCount);

            // A chip per set.
            var chips = new Image[MonopolyStyle.Groups.Length];
            var counts = new TextMeshProUGUI[chips.Length];
            for (int i = 0; i < chips.Length; i++)
            {
                chips[i] = Rounded(rect, $"Chip {MonopolyStyle.Groups[i]}", MonopolyStyle.GroupColor(MonopolyStyle.Groups[i]), 0.2f);
                Place(chips[i].rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(138f + i * 25.5f, 14f), new Vector2(22f, 22f));
                counts[i] = Text(chips[i].transform, "Count", "", 14f, Ink, Heavy);
                Stretch(counts[i].rectTransform);
            }

            TextMeshProUGUI arrow = Icon(rect, "TurnArrow", leftSide ? Icons.Right : Icons.Left, 44f, MonopolyStyle.PlayerColor(seat));
            Place(arrow.rectTransform, new Vector2(leftSide ? 1f : 0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(leftSide ? 26f : -26f, 0f), new Vector2(40f, 60f));
            arrow.gameObject.SetActive(false);

            TextMeshProUGUI stamp = Text(rect, "Bankrupt", "BANKRUPT", 46f, MonopolyStyle.Red, Heavy);
            Stretch(stamp.rectTransform);
            stamp.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 10f);
            stamp.gameObject.SetActive(false);

            MonopolyAssets.SetObject(panel, "group", group);
            MonopolyAssets.SetObject(panel, "frame", frame);
            MonopolyAssets.SetObject(panel, "accent", accent);
            MonopolyAssets.SetObject(panel, "glow", glow);
            MonopolyAssets.SetObject(panel, "badge", badge);
            MonopolyAssets.SetObject(panel, "tokenIcon", token);
            MonopolyAssets.SetObject(panel, "nameText", name);
            MonopolyAssets.SetObject(panel, "cashText", cash);
            MonopolyAssets.SetObject(panel, "worthText", worth);
            MonopolyAssets.SetObject(panel, "cpuTag", cpu);
            MonopolyAssets.SetObject(panel, "cpuText", cpuText);
            MonopolyAssets.SetObject(panel, "jailTag", jail);
            MonopolyAssets.SetObject(panel, "cardTag", card);
            MonopolyAssets.SetObject(panel, "cardCount", cardCount);
            MonopolyAssets.SetObjects(panel, "chips", chips);
            MonopolyAssets.SetObjects(panel, "chipCounts", counts);
            MonopolyAssets.SetObject(panel, "bankruptStamp", stamp.gameObject);
            MonopolyAssets.SetObject(panel, "turnArrow", arrow.gameObject);
            return panel;
        }

        private static GameObject Tag(RectTransform parent, string name, string icon, string label, Color color, out TextMeshProUGUI text)
        {
            Image pill = Rounded(parent, name, color, 0.5f);
            pill.rectTransform.sizeDelta = new Vector2(string.IsNullOrEmpty(label) ? 44f : 96f, 28f);
            TextMeshProUGUI glyph = Icon(pill.transform, "Icon", icon, 15f, White);
            Place(glyph.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(20f, 24f));
            text = Text(pill.transform, "Label", label, 14f, White, Heavy, TextAlignmentOptions.MidlineLeft);
            Stretch(text.rectTransform, 30f, 0f, 6f, 0f);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return pill.gameObject;
        }

        private static Toasts BuildToasts(RectTransform column)
        {
            RectTransform rect = Rect(column, "News");
            Place(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(400f, 330f));
            var toasts = rect.gameObject.AddComponent<Toasts>();
            var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.LowerLeft;
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            RectTransform item = Rect(rect, "Toast");
            item.sizeDelta = new Vector2(400f, 62f);
            CanvasGroup group = Group(item.gameObject);
            Image bar = Rounded(item, "Bar", MonopolyStyle.Red, 0.2f);
            Place(bar.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(8f, 54f));
            Image back = Rounded(item, "Back", new Color(1f, 1f, 1f, 0.93f), 0.35f);
            Stretch(back.rectTransform, 12f, 0f, 0f, 0f);
            TextMeshProUGUI label = Text(back.transform, "Text", "News", 18f, Ink, Body, TextAlignmentOptions.MidlineLeft);
            Stretch(label.rectTransform, 12f, 4f, 10f, 4f);
            label.enableAutoSizing = true;
            label.fontSizeMin = 13f;
            label.fontSizeMax = 18f;
            MonopolyAssets.SetObject(toasts, "list", rect);
            MonopolyAssets.SetObject(toasts, "template", group);
            return toasts;
        }

        private struct InfoCard
        {
            public TextMeshProUGUI mode;
            public TextMeshProUGUI round;
            public GameObject potRow;
            public TextMeshProUGUI pot;
            public TextMeshProUGUI bank;
            public Button pause;
            public Button sound;
            public Button music;
        }

        private static InfoCard BuildInfoCard(RectTransform column)
        {
            var info = new InfoCard();
            RectTransform rect = Rect(column, "Info");
            Place(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(400f, 300f));
            Image card = Card(rect, "Card", White, 0.55f);
            Stretch((RectTransform)card.transform.parent);
            info.mode = Text(rect, "Mode", "Classic", 30f, Ink, Heavy);
            Place(info.mode.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(360f, 40f));
            info.round = Text(rect, "Round", "Round 1", 21f, Muted, Bold);
            Place(info.round.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(360f, 30f));

            Image pot = Rounded(rect, "Jackpot", MonopolyStyle.Tint(MonopolyStyle.Gold, 0.75f), 0.4f);
            Place(pot.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(360f, 60f));
            TextMeshProUGUI potIcon = Icon(pot.transform, "Icon", Icons.Car, 26f, MonopolyStyle.Red);
            Place(potIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(40f, 40f));
            TextMeshProUGUI potLabel = Text(pot.transform, "Label", "FREE PARKING\nJACKPOT", 14f, Ink, Heavy, TextAlignmentOptions.MidlineLeft);
            Place(potLabel.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(60f, 0f), new Vector2(150f, 50f));
            info.pot = Text(pot.transform, "Amount", "$0", 30f, MonopolyStyle.Shade(MonopolyStyle.Gold, 0.6f), Heavy, TextAlignmentOptions.MidlineRight);
            Place(info.pot.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-14f, 0f), new Vector2(150f, 50f));
            info.potRow = pot.gameObject;

            info.bank = Text(rect, "Bank", "", 20f, Muted, Bold);
            Place(info.bank.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 96f), new Vector2(360f, 30f));
            TextMeshProUGUI bankCaption = Text(rect, "BankCaption", "BANK", 13f, Muted, Heavy);
            Place(bankCaption.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 124f), new Vector2(360f, 20f));

            HorizontalLayoutGroup buttons = Row(rect, "Buttons", 18f);
            Place((RectTransform)buttons.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(360f, 68f));
            info.pause = RoundButton(buttons.transform, "PauseButton", Icons.Pause, MonopolyStyle.Red, 64f, White);
            info.sound = RoundButton(buttons.transform, "Sound", Icons.SoundOn, Soft, 64f, Ink);
            info.music = RoundButton(buttons.transform, "Music", Icons.Music, Soft, 64f, Ink);
            return info;
        }

        private static ActionPanel BuildActionPanel(RectTransform hud)
        {
            // Over the lower half of the board's middle, clear of the bottom row of spaces.
            RectTransform rect = Rect(hud, "Actions");
            Center(rect, new Vector2(0f, -157f), new Vector2(640f, 190f));
            var panel = rect.gameObject.AddComponent<ActionPanel>();
            CanvasGroup group = Group(rect.gameObject);
            Image card = Card(rect, "Card", new Color(1f, 1f, 1f, 0.97f), 0.6f);
            Stretch((RectTransform)card.transform.parent);
            Image badge = Image(rect, "Badge", MonopolyArtBuilder.UI("Circle"), MonopolyStyle.Red);
            Place(badge.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(56f, -54f), new Vector2(76f, 76f));
            Image token = Image(badge.transform, "Token", MonopolyArtBuilder.Badge(0), White);
            Stretch(token.rectTransform, 12f, 12f, 12f, 12f);
            token.preserveAspect = true;
            TextMeshProUGUI title = Text(rect, "Title", "Your turn", 32f, Ink, Heavy, TextAlignmentOptions.MidlineLeft);
            Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(108f, -8f), new Vector2(510f, 52f));
            FitOneLine(title, 20f);
            TextMeshProUGUI subtitle = Text(rect, "Subtitle", "Roll the dice!", 19f, Muted, Body, TextAlignmentOptions.TopLeft);
            Place(subtitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(108f, -56f), new Vector2(510f, 50f));

            HorizontalLayoutGroup row = Row(rect, "Choices", 12f);
            Place((RectTransform)row.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(610f, 66f));
            var buttons = new List<ActionButton>();
            for (int i = 0; i < 4; i++)
            {
                Button button = Button(row.transform, $"Choice {i + 1}", "Choice", Icons.Dice, MonopolyStyle.Red, new Vector2(160f, 62f), 22f);
                var action = button.gameObject.AddComponent<ActionButton>();
                Object.DestroyImmediate(button.GetComponent<PressScale>());
                // The action button paints its own disabled look.
                ColorBlock colors = button.colors;
                colors.disabledColor = Color.white;
                button.colors = colors;
                MonopolyAssets.SetObject(action, "button", button);
                MonopolyAssets.SetObject(action, "background", FaceOf(button));
                MonopolyAssets.SetObject(action, "lip", GameMenuInstaller.FindChildComponent<Image>(button.transform, "Lip"));
                MonopolyAssets.SetObject(action, "label", LabelOf(button));
                MonopolyAssets.SetObject(action, "icon", IconOf(button));
                buttons.Add(action);
            }

            RectTransform thinking = Rect(rect, "Thinking");
            Place(thinking, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(300f, 62f));
            Image spinner = Image(thinking, "Spinner", MonopolyArtBuilder.UI("Ring"), MonopolyStyle.Red);
            Place(spinner.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(40f, 0f), new Vector2(44f, 44f));
            Image gap = Image(spinner.transform, "Gap", MonopolyArtBuilder.UI("Circle"), White);
            Place(gap.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -4f), new Vector2(16f, 16f));
            TextMeshProUGUI dots = Text(thinking, "Label", "Thinking...", 22f, Muted, Bold, TextAlignmentOptions.MidlineLeft);
            Stretch(dots.rectTransform, 80f, 0f, 0f, 0f);
            thinking.gameObject.SetActive(false);

            MonopolyAssets.SetObject(panel, "group", group);
            MonopolyAssets.SetObject(panel, "badge", badge);
            MonopolyAssets.SetObject(panel, "tokenIcon", token);
            MonopolyAssets.SetObject(panel, "title", title);
            MonopolyAssets.SetObject(panel, "subtitle", subtitle);
            MonopolyAssets.SetObjects(panel, "buttons", buttons.ToArray());
            MonopolyAssets.SetObject(panel, "row", row);
            MonopolyAssets.SetObject(panel, "thinking", thinking.gameObject);
            MonopolyAssets.SetObject(panel, "spinner", spinner.rectTransform);
            return panel;
        }

        private static FloatingTexts BuildFloating(RectTransform layer, Camera camera)
        {
            var floating = layer.gameObject.AddComponent<FloatingTexts>();
            TextMeshProUGUI text = Text(layer, "Float", "+$200", 44f, MonopolyStyle.Green, Heavy);
            text.fontSharedMaterial = MonopolyArtBuilder.TitleMaterial;
            Center(text.rectTransform, Vector2.zero, new Vector2(360f, 70f));
            text.textWrappingMode = TextWrappingModes.NoWrap;
            Image bill = Image(layer, "Bill", MonopolyArtBuilder.UI("Bill"), White);
            Center(bill.rectTransform, Vector2.zero, new Vector2(84f, 42f));
            MonopolyAssets.SetObject(floating, "layer", layer);
            MonopolyAssets.SetObject(floating, "textTemplate", text);
            MonopolyAssets.SetObject(floating, "billTemplate", bill);
            MonopolyAssets.SetObject(floating, "worldCamera", camera);
            return floating;
        }

        private struct BannerParts
        {
            public RectTransform root;
            public TextMeshProUGUI text;
            public Image background;
        }

        private static BannerParts BuildBanner(RectTransform layer)
        {
            RectTransform rect = Rect(layer, "Banner");
            Center(rect, new Vector2(0f, 80f), new Vector2(980f, 150f));
            Group(rect.gameObject);
            Image shadow = Image(rect, "Shadow", MonopolyArtBuilder.UI("Shadow"), new Color(0f, 0f, 0f, 0.45f));
            Stretch(shadow.rectTransform, -24f, -34f, -24f, -12f);
            Image background = Rounded(rect, "Ribbon", MonopolyStyle.Red, 0.35f);
            Stretch(background.rectTransform);
            Image keyline = Rounded(rect, "Keyline", new Color(1f, 1f, 1f, 0.9f), 0.3f);
            Stretch(keyline.rectTransform, 10f, 10f, 10f, 10f);
            keyline.fillCenter = false;
            TextMeshProUGUI text = Text(rect, "Text", "DOUBLES!", 76f, White, Heavy);
            Stretch(text.rectTransform, 20f, 0f, 20f, 0f);
            text.fontSharedMaterial = MonopolyArtBuilder.TitleMaterial;
            text.enableAutoSizing = true;
            text.fontSizeMin = 30f;
            text.fontSizeMax = 76f;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return new BannerParts { root = rect, text = text, background = background };
        }

        // ------------------------------------------------------------------ popups

        private static void SetPopup(Popup popup, CanvasGroup group, RectTransform card)
        {
            MonopolyAssets.SetObject(popup, "group", group);
            MonopolyAssets.SetObject(popup, "card", card);
        }

        private static Button CloseButton(RectTransform card)
        {
            Button close = RoundButton(card, "Close", Icons.Close, Soft, 48f, Ink);
            Place((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-30f, -30f), new Vector2(48f, 48f));
            return close;
        }

        private static DeedCardUI BuildDeed(RectTransform layer)
        {
            // Above the action panel, which stays in view below it.
            RectTransform root = PopupRoot(layer, "Deed", new Vector2(0f, 172f), new Vector2(440f, 640f), 0f, out CanvasGroup group, out RectTransform card);
            var deed = root.gameObject.AddComponent<DeedCardUI>();
            SetPopup(deed, group, card);
            Image header = Rounded(card, "Header", MonopolyStyle.GroupColor(ColorGroup.Red), 0.35f);
            Place(header.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(404f, 132f));
            TextMeshProUGUI caption = Text(header.transform, "Caption", "TITLE DEED", 15f, White, Heavy);
            Place(caption.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(380f, 22f));
            caption.characterSpacing = 8f;
            TextMeshProUGUI name = Text(header.transform, "Name", "SHIBUYA", 34f, White, Heavy);
            Place(name.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -4f), new Vector2(380f, 50f));
            name.enableAutoSizing = true;
            name.fontSizeMin = 18f;
            name.fontSizeMax = 34f;
            TextMeshProUGUI city = Text(header.transform, "City", "Tokyo", 17f, White, Bold);
            Place(city.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(380f, 24f));
            TextMeshProUGUI icon = Icon(header.transform, "Icon", Icons.Train, 44f, White);
            Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, -4f), new Vector2(56f, 56f));

            TextMeshProUGUI labels = Text(card, "Labels", "Rent", 20f, Ink, Body, TextAlignmentOptions.TopLeft);
            Place(labels.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -170f), new Vector2(260f, 260f));
            labels.lineSpacing = 12f;
            TextMeshProUGUI values = Text(card, "Values", "$18", 20f, Ink, Bold, TextAlignmentOptions.TopRight);
            Place(values.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -170f), new Vector2(120f, 260f));
            values.lineSpacing = 12f;
            Image rule = Image(card, "Rule", null, MonopolyStyle.Tint(Muted, 0.6f));
            Place(rule.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -438f), new Vector2(360f, 2f));
            TextMeshProUGUI footer = Text(card, "Footer", "", 15f, Muted, Body);
            Place(footer.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -448f), new Vector2(380f, 48f));
            TextMeshProUGUI owner = Text(card, "Owner", "For sale", 18f, Ink, Bold);
            Place(owner.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -500f), new Vector2(390f, 30f));

            // Buy, Auction and Manage in a row that stays centred when some are hidden.
            HorizontalLayoutGroup choices = Row(card, "Choices", 10f);
            Place((RectTransform)choices.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(404f, 62f));
            Button buy = Button(choices.transform, "Buy", "Buy $200", Icons.House, MonopolyStyle.Green, new Vector2(176f, 62f), 22f);
            Button auction = Button(choices.transform, "Auction", "Auction", Icons.Gavel, MonopolyStyle.ChanceOrange, new Vector2(150f, 62f), 22f);
            Button manageButton = Button(choices.transform, "Manage", "", Icons.Building, Soft, new Vector2(58f, 62f), 22f);
            Button close = CloseButton(card);

            TextMeshProUGUI stamp = Text(card, "MortgagedStamp", "MORTGAGED", 52f, MonopolyStyle.Red, Heavy);
            Center(stamp.rectTransform, new Vector2(0f, -40f), new Vector2(420f, 90f));
            stamp.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 18f);
            stamp.alpha = 0.82f;
            stamp.gameObject.SetActive(false);

            MonopolyAssets.SetObject(deed, "header", header);
            MonopolyAssets.SetObject(deed, "caption", caption);
            MonopolyAssets.SetObject(deed, "nameText", name);
            MonopolyAssets.SetObject(deed, "cityText", city);
            MonopolyAssets.SetObject(deed, "iconText", icon);
            MonopolyAssets.SetObject(deed, "labels", labels);
            MonopolyAssets.SetObject(deed, "values", values);
            MonopolyAssets.SetObject(deed, "footer", footer);
            MonopolyAssets.SetObject(deed, "ownerText", owner);
            MonopolyAssets.SetObject(deed, "mortgagedStamp", stamp.gameObject);
            MonopolyAssets.SetObject(deed, "buyButton", buy);
            MonopolyAssets.SetObject(deed, "buyLabel", LabelOf(buy));
            MonopolyAssets.SetObject(deed, "auctionButton", auction);
            MonopolyAssets.SetObject(deed, "auctionLabel", LabelOf(auction));
            MonopolyAssets.SetObject(deed, "closeButton", close);
            MonopolyAssets.SetObject(deed, "manageButton", manageButton);
            return deed;
        }

        private static CardRevealUI BuildCardReveal(RectTransform layer)
        {
            RectTransform root = PopupRoot(layer, "Card", new Vector2(0f, 110f), new Vector2(620f, 360f), 0f, out CanvasGroup group, out RectTransform card);
            var reveal = root.gameObject.AddComponent<CardRevealUI>();
            SetPopup(reveal, group, card);
            Image face = card.GetComponentInChildren<Image>();
            Image band = Rounded(card, "Band", MonopolyStyle.ChanceOrange, 0.35f);
            Place(band.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(588f, 72f));
            TextMeshProUGUI title = Text(band.transform, "Deck", "CHANCE", 32f, White, Heavy);
            Stretch(title.rectTransform);
            title.characterSpacing = 6f;
            TextMeshProUGUI icon = Text(card, "Icon", "?", 130f, MonopolyStyle.ChanceOrange, Heavy);
            Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(100f, -30f), new Vector2(150f, 170f));
            icon.fontSharedMaterial = MonopolyArtBuilder.TextShadow;
            TextMeshProUGUI body = Text(card, "Body", "Advance to GO.", 27f, Ink, Bold, TextAlignmentOptions.MidlineLeft);
            Place(body.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(190f, -20f), new Vector2(400f, 160f));
            body.enableAutoSizing = true;
            body.fontSizeMin = 18f;
            body.fontSizeMax = 27f;
            TextMeshProUGUI by = Text(card, "DrawnBy", "Drawn by", 16f, Muted, Body, TextAlignmentOptions.MidlineLeft);
            Place(by.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(30f, 24f), new Vector2(300f, 30f));
            Button ok = Button(card, "OK", "OK", Icons.Check, MonopolyStyle.ChanceOrange, new Vector2(160f, 58f), 24f);
            Place((RectTransform)ok.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 20f), new Vector2(160f, 58f));
            MonopolyAssets.SetObject(reveal, "face", face);
            MonopolyAssets.SetObject(reveal, "band", band);
            MonopolyAssets.SetObject(reveal, "deckTitle", title);
            MonopolyAssets.SetObject(reveal, "icon", icon);
            MonopolyAssets.SetObject(reveal, "body", body);
            MonopolyAssets.SetObject(reveal, "drawnBy", by);
            MonopolyAssets.SetObject(reveal, "okButton", ok);
            MonopolyAssets.SetObject(reveal, "okBackground", FaceOf(ok));
            return reveal;
        }

        private static AuctionUI BuildAuction(RectTransform layer)
        {
            RectTransform root = PopupRoot(layer, "Auction", new Vector2(0f, 152f), new Vector2(620f, 600f), 0f, out CanvasGroup group, out RectTransform card);
            var auction = root.gameObject.AddComponent<AuctionUI>();
            SetPopup(auction, group, card);
            TextMeshProUGUI title = Text(card, "Title", $"{Icons.Gavel}  AUCTION", 30f, Ink, Heavy);
            Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(560f, 40f));
            Image band = Rounded(card, "Lot", MonopolyStyle.GroupColor(ColorGroup.Orange), 0.3f);
            Place(band.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -66f), new Vector2(560f, 58f));
            TextMeshProUGUI lot = Text(band.transform, "Name", "LA RAMBLA", 26f, White, Heavy);
            Stretch(lot.rectTransform);
            TextMeshProUGUI price = Text(card, "Price", "List price $180", 17f, Muted, Bold);
            Place(price.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(560f, 26f));
            TextMeshProUGUI bid = Text(card, "Bid", "$120", 64f, Ink, Heavy);
            Place(bid.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -160f), new Vector2(560f, 80f));
            TextMeshProUGUI leader = Text(card, "Leader", "Blue leads", 21f, Ink, Bold);
            Place(leader.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -240f), new Vector2(560f, 30f));

            HorizontalLayoutGroup slotsRow = Row(card, "Bidders", 12f);
            Place((RectTransform)slotsRow.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -284f), new Vector2(560f, 130f));
            var slots = new List<AuctionUI.BidderSlot>();
            for (int i = 0; i < 4; i++)
            {
                RectTransform slot = Rect(slotsRow.transform, $"Bidder {i + 1}");
                slot.sizeDelta = new Vector2(128f, 128f);
                Image frame = Rounded(slot, "Frame", MonopolyStyle.Panel, 0.4f);
                Stretch(frame.rectTransform);
                Image badge = Image(slot, "Badge", MonopolyArtBuilder.UI("Circle"), MonopolyStyle.PlayerColor(i));
                Place(badge.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(58f, 58f));
                Image token = Image(badge.transform, "Token", MonopolyArtBuilder.Badge(i), White);
                Stretch(token.rectTransform, 9f, 9f, 9f, 9f);
                token.preserveAspect = true;
                TextMeshProUGUI name = Text(slot, "Name", "Player", 16f, Ink, Bold);
                Place(name.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(120f, 28f));
                FitOneLine(name, 11f);
                TextMeshProUGUI status = Text(slot, "Status", "In", 15f, Muted, Heavy);
                Place(status.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(120f, 22f));
                slots.Add(new AuctionUI.BidderSlot { root = slot.gameObject, badge = badge, token = token, name = name, status = status, frame = frame });
            }
            TextMeshProUGUI turn = Text(card, "Turn", "Your bid", 21f, Ink, Bold);
            Place(turn.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 94f), new Vector2(560f, 30f));

            RectTransform controls = Row(card, "Controls", 12f).GetComponent<RectTransform>();
            Place(controls, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(580f, 64f));
            var raises = new List<Button>();
            var labels = new List<TextMeshProUGUI>();
            foreach (string label in new[] { "$10", "$50", "$100" })
            {
                Button raise = Button(controls, $"Bid {label}", label, Icons.Plus, MonopolyStyle.Green, new Vector2(136f, 60f), 22f);
                raises.Add(raise);
                labels.Add(LabelOf(raise));
            }
            Button pass = Button(controls, "Pass", "Pass", Icons.Close, MonopolyStyle.Tint(Muted, 0.35f), new Vector2(124f, 60f), 22f);

            MonopolyAssets.SetObject(auction, "band", band);
            MonopolyAssets.SetObject(auction, "lotName", lot);
            MonopolyAssets.SetObject(auction, "lotPrice", price);
            MonopolyAssets.SetObject(auction, "bidText", bid);
            MonopolyAssets.SetObject(auction, "leaderText", leader);
            MonopolyAssets.SetObject(auction, "turnText", turn);
            MonopolyAssets.Set(auction, "slots", list =>
            {
                list.arraySize = slots.Count;
                for (int i = 0; i < slots.Count; i++)
                {
                    var entry = list.GetArrayElementAtIndex(i);
                    entry.FindPropertyRelative("root").objectReferenceValue = slots[i].root;
                    entry.FindPropertyRelative("badge").objectReferenceValue = slots[i].badge;
                    entry.FindPropertyRelative("token").objectReferenceValue = slots[i].token;
                    entry.FindPropertyRelative("name").objectReferenceValue = slots[i].name;
                    entry.FindPropertyRelative("status").objectReferenceValue = slots[i].status;
                    entry.FindPropertyRelative("frame").objectReferenceValue = slots[i].frame;
                }
            });
            MonopolyAssets.SetObjects(auction, "raiseButtons", raises.ToArray());
            MonopolyAssets.SetObjects(auction, "raiseLabels", labels.ToArray());
            MonopolyAssets.SetObject(auction, "passButton", pass);
            MonopolyAssets.SetObject(auction, "humanControls", controls.gameObject);
            return auction;
        }

        private static ManageUI BuildManage(RectTransform layer)
        {
            RectTransform root = PopupRoot(layer, "Manage", new Vector2(0f, 30f), new Vector2(820f, 800f), 0.35f, out CanvasGroup group, out RectTransform card);
            var manage = root.gameObject.AddComponent<ManageUI>();
            SetPopup(manage, group, card);
            TextMeshProUGUI title = Text(card, "Title", "Your properties", 32f, Ink, Heavy, TextAlignmentOptions.MidlineLeft);
            Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -22f), new Vector2(520f, 44f));
            TextMeshProUGUI cash = Text(card, "Cash", "$1,500", 32f, MonopolyStyle.Green, Heavy, TextAlignmentOptions.MidlineRight);
            Place(cash.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-84f, -22f), new Vector2(220f, 44f));
            TextMeshProUGUI hint = Text(card, "Hint", "", 18f, Muted, Body, TextAlignmentOptions.MidlineLeft);
            Place(hint.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -68f), new Vector2(740f, 30f));
            ScrollRect scroll = ScrollList(card, "List", out RectTransform content, 8f);
            Place((RectTransform)scroll.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(760f, 668f));
            PropertyRow template = PropertyRowTemplate(content);
            TextMeshProUGUI empty = Text(card, "Empty", "No properties yet. Buy some!", 24f, Muted, Bold);
            Center(empty.rectTransform, new Vector2(0f, -40f), new Vector2(600f, 60f));
            Button close = CloseButton(card);
            MonopolyAssets.SetObject(manage, "title", title);
            MonopolyAssets.SetObject(manage, "cashText", cash);
            MonopolyAssets.SetObject(manage, "hint", hint);
            MonopolyAssets.SetObject(manage, "list", content);
            MonopolyAssets.SetObject(manage, "template", template);
            MonopolyAssets.SetObject(manage, "emptyText", empty);
            MonopolyAssets.SetObject(manage, "closeButton", close);
            return manage;
        }

        private static PropertyRow PropertyRowTemplate(RectTransform list)
        {
            RectTransform rect = Rect(list, "Row");
            rect.sizeDelta = new Vector2(740f, 74f);
            Size(rect, 0f, 74f);
            var row = rect.gameObject.AddComponent<PropertyRow>();
            Image background = Rounded(rect, "Back", White, 0.3f);
            Stretch(background.rectTransform);
            var outline = background.gameObject.AddComponent<Outline>();
            outline.effectColor = MonopolyStyle.Tint(Muted, 0.55f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            Image bar = Rounded(rect, "Color", MonopolyStyle.GroupColor(ColorGroup.Red), 0.2f);
            Place(bar.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(16f, 58f));
            TextMeshProUGUI name = Text(rect, "Name", "Shibuya", 22f, Ink, Bold, TextAlignmentOptions.MidlineLeft);
            Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -2f), new Vector2(260f, 38f));
            FitOneLine(name, 15f);
            TextMeshProUGUI state = Text(rect, "State", "Rent $18", 16f, Muted, Body, TextAlignmentOptions.MidlineLeft);
            Place(state.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(36f, 6f), new Vector2(260f, 30f));
            state.textWrappingMode = TextWrappingModes.NoWrap;
            Button build = Button(rect, "Build", "$150", Icons.House, MonopolyStyle.Green, new Vector2(112f, 52f), 19f);
            Place((RectTransform)build.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-348f, 0f), new Vector2(112f, 52f));
            Button sell = Button(rect, "Sell", "Sell +$75", null, MonopolyStyle.ChanceOrange, new Vector2(126f, 52f), 18f);
            Place((RectTransform)sell.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-214f, 0f), new Vector2(126f, 52f));
            TextMeshProUGUI sellText = LabelOf(sell);
            sellText.enableAutoSizing = true;
            sellText.fontSizeMax = 18f;
            sellText.fontSizeMin = 12f;
            Button mortgage = Button(rect, "Mortgage", "Mortgage", null, MonopolyStyle.Blue, new Vector2(196f, 52f), 17f);
            Place((RectTransform)mortgage.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-10f, 0f), new Vector2(196f, 52f));
            MonopolyAssets.SetObject(row, "colorBar", bar);
            MonopolyAssets.SetObject(row, "nameText", name);
            MonopolyAssets.SetObject(row, "stateText", state);
            MonopolyAssets.SetObject(row, "buildButton", build);
            MonopolyAssets.SetObject(row, "buildLabel", LabelOf(build));
            MonopolyAssets.SetObject(row, "buildIcon", IconOf(build));
            MonopolyAssets.SetObject(row, "sellButton", sell);
            MonopolyAssets.SetObject(row, "sellLabel", LabelOf(sell));
            MonopolyAssets.SetObject(row, "mortgageButton", mortgage);
            MonopolyAssets.SetObject(row, "mortgageLabel", LabelOf(mortgage));
            MonopolyAssets.SetObject(row, "background", background);
            return row;
        }

        private static TradeUI BuildTrade(RectTransform layer)
        {
            RectTransform root = PopupRoot(layer, "Trade", new Vector2(0f, 20f), new Vector2(1200f, 820f), 0.4f, out CanvasGroup group, out RectTransform card);
            var trade = root.gameObject.AddComponent<TradeUI>();
            SetPopup(trade, group, card);
            TextMeshProUGUI title = Text(card, "Title", "Trade", 32f, Ink, Heavy, TextAlignmentOptions.MidlineLeft);
            Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -22f), new Vector2(500f, 44f));

            HorizontalLayoutGroup partnerRow = Row(card, "Partners", 14f, TextAnchor.MiddleRight);
            Place((RectTransform)partnerRow.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -18f), new Vector2(760f, 64f));
            var partners = new List<TradeUI.PartnerButton>();
            for (int i = 0; i < 3; i++)
            {
                RectTransform rect = Rect(partnerRow.transform, $"Partner {i + 1}");
                rect.sizeDelta = new Vector2(230f, 60f);
                Image frame = Rounded(rect, "Frame", White, 0.5f, true);
                Stretch(frame.rectTransform);
                var outline = frame.gameObject.AddComponent<Outline>();
                outline.effectColor = MonopolyStyle.Tint(Muted, 0.4f);
                outline.effectDistance = new Vector2(1.5f, -1.5f);
                Image badge = Image(rect, "Badge", MonopolyArtBuilder.UI("Circle"), MonopolyStyle.PlayerColor(i + 1));
                Place(badge.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(46f, 46f));
                Image token = Image(badge.transform, "Token", MonopolyArtBuilder.Badge(i + 1), White);
                Stretch(token.rectTransform, 7f, 7f, 7f, 7f);
                token.preserveAspect = true;
                TextMeshProUGUI name = Text(rect, "Name", "Player", 20f, Ink, Bold, TextAlignmentOptions.MidlineLeft);
                Stretch(name.rectTransform, 64f, 0f, 8f, 0f);
                var button = rect.gameObject.AddComponent<Button>();
                button.targetGraphic = frame;
                rect.gameObject.AddComponent<PressScale>();
                partners.Add(new TradeUI.PartnerButton { button = button, badge = badge, token = token, name = name, frame = frame });
            }

            var sides = new List<(TextMeshProUGUI header, RectTransform list, TextMeshProUGUI cash, Button[] steps, Toggle card)>();
            for (int s = 0; s < 2; s++)
            {
                float x = s == 0 ? -290f : 290f;
                TextMeshProUGUI header = Text(card, s == 0 ? "GiveHeader" : "GetHeader", s == 0 ? "You give" : "They give", 22f, Ink, Heavy, TextAlignmentOptions.MidlineLeft);
                Place(header.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x, -100f), new Vector2(540f, 34f));
                ScrollRect scroll = ScrollList(card, s == 0 ? "GiveList" : "GetList", out RectTransform content, 6f);
                Place((RectTransform)scroll.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x, -140f), new Vector2(540f, 390f));
                Image listBack = Rounded(scroll.transform, "Back", MonopolyStyle.Panel, 0.3f);
                Stretch(listBack.rectTransform);
                listBack.transform.SetAsFirstSibling();
                RectTransform cashRow = Rect(card, s == 0 ? "GiveCash" : "GetCash");
                Place(cashRow, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x, -546f), new Vector2(540f, 60f));
                var steps = new Button[4];
                string[] captions = { "-50", "-10", "+10", "+50" };
                float[] offsets = { -230f, -150f, 150f, 230f };
                for (int i = 0; i < 4; i++)
                {
                    steps[i] = Button(cashRow, $"Step {captions[i]}", captions[i], null, i < 2 ? MonopolyStyle.Tint(Muted, 0.45f) : MonopolyStyle.Green, new Vector2(72f, 52f), 18f);
                    Center((RectTransform)steps[i].transform, new Vector2(offsets[i], 0f), new Vector2(72f, 52f));
                }
                TextMeshProUGUI cash = Text(cashRow, "Amount", "$0", 30f, Ink, Heavy);
                Center(cash.rectTransform, Vector2.zero, new Vector2(200f, 52f));
                Toggle jailCard = Switch(card, s == 0 ? "GiveCard" : "GetCard", "Get Out of Jail Free card", 540f, 50f);
                Place((RectTransform)jailCard.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x, -612f), new Vector2(540f, 50f));
                sides.Add((header, content, cash, steps, jailCard));
            }
            TradeItem item = TradeItemTemplate(sides[0].list);
            TextMeshProUGUI status = Text(card, "Status", "", 18f, Muted, Body);
            Place(status.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 96f), new Vector2(1100f, 30f));
            Button propose = Button(card, "Propose", "Propose", Icons.Handshake, MonopolyStyle.Green, new Vector2(240f, 64f), 24f);
            Place((RectTransform)propose.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(130f, 22f), new Vector2(240f, 64f));
            Button cancel = Button(card, "Cancel", "Cancel", Icons.Close, Soft, new Vector2(200f, 64f), 24f);
            Place((RectTransform)cancel.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-120f, 22f), new Vector2(200f, 64f));

            MonopolyAssets.SetObject(trade, "title", title);
            MonopolyAssets.Set(trade, "partners", list =>
            {
                list.arraySize = partners.Count;
                for (int i = 0; i < partners.Count; i++)
                {
                    var entry = list.GetArrayElementAtIndex(i);
                    entry.FindPropertyRelative("button").objectReferenceValue = partners[i].button;
                    entry.FindPropertyRelative("badge").objectReferenceValue = partners[i].badge;
                    entry.FindPropertyRelative("token").objectReferenceValue = partners[i].token;
                    entry.FindPropertyRelative("name").objectReferenceValue = partners[i].name;
                    entry.FindPropertyRelative("frame").objectReferenceValue = partners[i].frame;
                }
            });
            MonopolyAssets.SetObject(trade, "giveHeader", sides[0].header);
            MonopolyAssets.SetObject(trade, "getHeader", sides[1].header);
            MonopolyAssets.SetObject(trade, "giveList", sides[0].list);
            MonopolyAssets.SetObject(trade, "getList", sides[1].list);
            MonopolyAssets.SetObject(trade, "template", item);
            MonopolyAssets.SetObject(trade, "giveCashText", sides[0].cash);
            MonopolyAssets.SetObject(trade, "getCashText", sides[1].cash);
            MonopolyAssets.SetObjects(trade, "giveCashButtons", sides[0].steps);
            MonopolyAssets.SetObjects(trade, "getCashButtons", sides[1].steps);
            MonopolyAssets.SetObject(trade, "giveCard", sides[0].card);
            MonopolyAssets.SetObject(trade, "getCard", sides[1].card);
            MonopolyAssets.SetObject(trade, "status", status);
            MonopolyAssets.SetObject(trade, "proposeButton", propose);
            MonopolyAssets.SetObject(trade, "cancelButton", cancel);
            return trade;
        }

        private static TradeItem TradeItemTemplate(RectTransform list)
        {
            RectTransform rect = Rect(list, "Item");
            rect.sizeDelta = new Vector2(520f, 54f);
            Size(rect, 0f, 54f);
            var item = rect.gameObject.AddComponent<TradeItem>();
            Image background = Rounded(rect, "Back", White, 0.3f, true);
            Stretch(background.rectTransform);
            Image bar = Rounded(rect, "Color", MonopolyStyle.GroupColor(ColorGroup.Green), 0.2f);
            Place(bar.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(14f, 40f));
            TextMeshProUGUI name = Text(rect, "Name", "Abbey Road", 21f, Ink, Bold, TextAlignmentOptions.MidlineLeft);
            Stretch(name.rectTransform, 34f, 0f, 200f, 0f);
            TextMeshProUGUI detail = Text(rect, "Detail", "$300", 17f, Muted, Bold, TextAlignmentOptions.MidlineRight);
            Stretch(detail.rectTransform, 280f, 0f, 60f, 0f);
            Image box = Rounded(rect, "Box", MonopolyStyle.Panel, 0.3f);
            Place(box.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(34f, 34f));
            TextMeshProUGUI check = Icon(box.transform, "Check", Icons.Check, 22f, MonopolyStyle.Green);
            Stretch(check.rectTransform);
            var toggle = rect.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = background;
            toggle.graphic = check;
            toggle.isOn = false;
            MonopolyAssets.SetObject(item, "toggle", toggle);
            MonopolyAssets.SetObject(item, "colorBar", bar);
            MonopolyAssets.SetObject(item, "nameText", name);
            MonopolyAssets.SetObject(item, "detail", detail);
            MonopolyAssets.SetObject(item, "background", background);
            return item;
        }

        private static TradeOfferUI BuildOffer(RectTransform layer)
        {
            RectTransform root = PopupRoot(layer, "Offer", new Vector2(0f, 60f), new Vector2(760f, 540f), 0.45f, out CanvasGroup group, out RectTransform card);
            var offer = root.gameObject.AddComponent<TradeOfferUI>();
            SetPopup(offer, group, card);
            Image badge = Image(card, "Badge", MonopolyArtBuilder.UI("Circle"), MonopolyStyle.PlayerColor(1));
            Place(badge.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -56f), new Vector2(84f, 84f));
            Image token = Image(badge.transform, "Token", MonopolyArtBuilder.Badge(1), White);
            Stretch(token.rectTransform, 13f, 13f, 13f, 13f);
            token.preserveAspect = true;
            TextMeshProUGUI title = Text(card, "Title", "Blue offers you a trade", 28f, Ink, Heavy);
            Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -106f), new Vector2(700f, 40f));
            for (int s = 0; s < 2; s++)
            {
                float x = s == 0 ? -180f : 180f;
                Image column = Rounded(card, s == 0 ? "GetBox" : "GiveBox", s == 0 ? MonopolyStyle.Tint(MonopolyStyle.Green, 0.88f) : MonopolyStyle.Tint(MonopolyStyle.Red, 0.9f), 0.35f);
                Place(column.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x, -160f), new Vector2(330f, 250f));
                TextMeshProUGUI header = Text(column.transform, "Header", s == 0 ? "YOU GET" : "YOU GIVE", 18f, s == 0 ? MonopolyStyle.Green : MonopolyStyle.Red, Heavy);
                Place(header.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(300f, 26f));
                TextMeshProUGUI list = Text(column.transform, "List", "", 20f, Ink, Bold, TextAlignmentOptions.TopLeft);
                Stretch(list.rectTransform, 18f, 12f, 12f, 44f);
                MonopolyAssets.SetObject(offer, s == 0 ? "getText" : "giveText", list);
            }
            Button accept = Button(card, "Accept", "Accept", Icons.Check, MonopolyStyle.Green, new Vector2(220f, 64f), 24f);
            Place((RectTransform)accept.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(120f, 26f), new Vector2(220f, 64f));
            Button decline = Button(card, "Decline", "Decline", Icons.Close, MonopolyStyle.Red, new Vector2(220f, 64f), 24f);
            Place((RectTransform)decline.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-120f, 26f), new Vector2(220f, 64f));
            MonopolyAssets.SetObject(offer, "fromBadge", badge);
            MonopolyAssets.SetObject(offer, "fromToken", token);
            MonopolyAssets.SetObject(offer, "title", title);
            MonopolyAssets.SetObject(offer, "acceptButton", accept);
            MonopolyAssets.SetObject(offer, "declineButton", decline);
            return offer;
        }

        // ------------------------------------------------------------------ screens

        private static SetupUI BuildSetup(RectTransform layer)
        {
            RectTransform root = PopupRoot(layer, "Setup", new Vector2(0f, 0f), new Vector2(1780f, 1010f), 0.55f, out CanvasGroup group, out RectTransform card);
            var setup = root.gameObject.AddComponent<SetupUI>();
            SetPopup(setup, group, card);
            TextMeshProUGUI title = Text(card, "Title", "NEW GAME", 46f, Ink, Heavy, TextAlignmentOptions.MidlineLeft);
            Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(46f, -24f), new Vector2(600f, 60f));
            TextMeshProUGUI starRules = Text(card, "StarRules", MonopolyCampaign.StarRules, 17f, Muted, Body, TextAlignmentOptions.MidlineRight);
            Place(starRules.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-46f, -34f), new Vector2(1000f, 40f));

            HorizontalLayoutGroup modesRow = Row(card, "Modes", 22f);
            Place((RectTransform)modesRow.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(1700f, 250f));
            var modes = new List<ModeCard>();
            for (int i = 0; i < 6; i++)
            {
                modes.Add(ModeCardTemplate(modesRow.transform, i));
            }
            TextMeshProUGUI modeTitle = Text(card, "ModeTitle", "Classic", 26f, Ink, Heavy, TextAlignmentOptions.TopLeft);
            Place(modeTitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60f, -372f), new Vector2(260f, 40f));
            TextMeshProUGUI modeDescription = Text(card, "ModeDescription", "", 18f, MonopolyStyle.Shade(Muted, 0.8f), Body, TextAlignmentOptions.TopLeft);
            Place(modeDescription.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(320f, -372f), new Vector2(1400f, 84f));

            HorizontalLayoutGroup seatsRow = Row(card, "Seats", 24f);
            Place((RectTransform)seatsRow.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -470f), new Vector2(1700f, 380f));
            var seats = new List<SeatCard>();
            for (int i = 0; i < 4; i++)
            {
                seats.Add(SeatCardTemplate(seatsRow.transform, i));
            }

            TextMeshProUGUI status = Text(card, "Status", "", 19f, Muted, Bold, TextAlignmentOptions.MidlineLeft);
            Place(status.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(300f, 44f), new Vector2(900f, 40f));
            Button back = Button(card, "Back", "Back", Icons.Left, Soft, new Vector2(220f, 76f), 26f);
            Place((RectTransform)back.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(46f, 26f), new Vector2(220f, 76f));
            Button start = Button(card, "Start", "START GAME", Icons.Play, MonopolyStyle.Red, new Vector2(380f, 84f), 32f);
            Place((RectTransform)start.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-46f, 24f), new Vector2(380f, 84f));

            MonopolyAssets.SetObjects(setup, "seats", seats.ToArray());
            MonopolyAssets.SetObjects(setup, "modes", modes.ToArray());
            MonopolyAssets.SetObject(setup, "modeTitle", modeTitle);
            MonopolyAssets.SetObject(setup, "modeDescription", modeDescription);
            MonopolyAssets.SetObject(setup, "starRules", starRules);
            MonopolyAssets.SetObject(setup, "status", status);
            MonopolyAssets.SetObject(setup, "startButton", start);
            MonopolyAssets.SetObject(setup, "backButton", back);
            return setup;
        }

        private static ModeCard ModeCardTemplate(Transform parent, int index)
        {
            RectTransform rect = Rect(parent, $"Mode {index + 1}");
            rect.sizeDelta = new Vector2(262f, 240f);
            var mode = rect.gameObject.AddComponent<ModeCard>();
            Image frame = Rounded(rect, "Frame", White, 0.55f, true);
            Stretch(frame.rectTransform, -5f, -5f, -5f, -5f);
            Image face = Card(rect, "Card", White, 0.5f);
            Stretch((RectTransform)face.transform.parent);
            face.raycastTarget = false;
            Image iconBack = Image(rect, "IconBack", MonopolyArtBuilder.UI("Circle"), MonopolyStyle.Red);
            Place(iconBack.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(80f, 80f));
            TextMeshProUGUI icon = Icon(iconBack.transform, "Icon", Icons.House, 38f, White);
            Stretch(icon.rectTransform);
            TextMeshProUGUI title = Text(rect, "Title", "Classic", 25f, Ink, Heavy);
            Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -98f), new Vector2(246f, 34f));
            TextMeshProUGUI tagline = Text(rect, "Tagline", "", 15f, Muted, Body, TextAlignmentOptions.Top);
            Place(tagline.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -134f), new Vector2(236f, 66f));
            var stars = new TextMeshProUGUI[3];
            for (int i = 0; i < 3; i++)
            {
                stars[i] = Icon(rect, $"Star {i + 1}", Icons.Star, 22f, MonopolyStyle.Gold);
                Place(stars[i].rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2((i - 1) * 30f, 10f), new Vector2(28f, 28f));
            }
            TextMeshProUGUI mark = Icon(rect, "Selected", Icons.Check, 20f, White);
            Place(mark.rectTransform, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-18f, -18f), new Vector2(28f, 28f));
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = frame;
            button.transition = Selectable.Transition.None;
            MonopolyAssets.SetObject(mode, "button", button);
            MonopolyAssets.SetObject(mode, "frame", frame);
            MonopolyAssets.SetObject(mode, "iconBackground", iconBack);
            MonopolyAssets.SetObject(mode, "icon", icon);
            MonopolyAssets.SetObject(mode, "title", title);
            MonopolyAssets.SetObject(mode, "tagline", tagline);
            MonopolyAssets.SetObjects(mode, "stars", stars);
            MonopolyAssets.SetObject(mode, "selectedMark", mark.gameObject);
            return mode;
        }

        private static SeatCard SeatCardTemplate(Transform parent, int seat)
        {
            RectTransform rect = Rect(parent, $"Seat {seat + 1}");
            rect.sizeDelta = new Vector2(400f, 376f);
            var card = rect.gameObject.AddComponent<SeatCard>();
            Image face = Card(rect, "Card", MonopolyStyle.Panel, 0.5f);
            Stretch((RectTransform)face.transform.parent);
            Image band = Rounded(rect, "Band", MonopolyStyle.PlayerColor(seat), 0.3f);
            Place(band.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(384f, 12f));
            RectTransform content = Stretch(Rect(rect, "Content"));
            CanvasGroup group = Group(content.gameObject);
            Image badge = Image(content, "Badge", MonopolyArtBuilder.UI("Circle"), MonopolyStyle.PlayerColor(seat));
            Place(badge.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -86f), new Vector2(118f, 118f));
            Image ring = Image(badge.transform, "Ring", MonopolyArtBuilder.UI("Ring"), new Color(1f, 1f, 1f, 0.9f));
            Stretch(ring.rectTransform, 5f, 5f, 5f, 5f);
            Image token = Image(badge.transform, "Token", MonopolyArtBuilder.Badge(seat), White);
            Stretch(token.rectTransform, 18f, 18f, 18f, 18f);
            token.preserveAspect = true;
            Button previous = RoundButton(content, "Previous", Icons.Left, White, 52f, Ink);
            Place((RectTransform)previous.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-112f, -86f), new Vector2(52f, 52f));
            Button next = RoundButton(content, "Next", Icons.Right, White, 52f, Ink);
            Place((RectTransform)next.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(112f, -86f), new Vector2(52f, 52f));
            TextMeshProUGUI tokenName = Text(content, "TokenName", "Race Car", 19f, Muted, Bold);
            Place(tokenName.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(360f, 28f));
            TMP_InputField nameField = TextInput(content, "NameField", "Name", new Vector2(320f, 54f), 24f);
            Place((RectTransform)nameField.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -184f), new Vector2(320f, 54f));

            var kinds = new Button[3];
            var kindBackgrounds = new Image[3];
            string[] kindLabels = { "HUMAN", "CPU", "OFF" };
            string[] kindIcons = { Icons.User, Icons.Robot, Icons.Close };
            for (int i = 0; i < 3; i++)
            {
                kinds[i] = Button(rect, $"Kind {kindLabels[i]}", kindLabels[i], kindIcons[i], White, new Vector2(118f, 48f), 17f, Ink);
                Place((RectTransform)kinds[i].transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2((i - 1) * 124f, 74f), new Vector2(118f, 48f));
                kindBackgrounds[i] = FaceOf(kinds[i]);
            }
            RectTransform levelRow = Rect(content, "Levels");
            Place(levelRow, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(372f, 44f));
            var levels = new Button[3];
            var levelBackgrounds = new Image[3];
            string[] levelLabels = { "EASY", "NORMAL", "HARD" };
            for (int i = 0; i < 3; i++)
            {
                levels[i] = Button(levelRow, $"Level {levelLabels[i]}", levelLabels[i], null, White, new Vector2(118f, 42f), 16f, Ink);
                Center((RectTransform)levels[i].transform, new Vector2((i - 1) * 124f, 0f), new Vector2(118f, 42f));
                levelBackgrounds[i] = FaceOf(levels[i]);
            }
            MonopolyAssets.SetObject(card, "band", band);
            MonopolyAssets.SetObject(card, "badge", badge);
            MonopolyAssets.SetObject(card, "token", token);
            MonopolyAssets.SetObject(card, "tokenName", tokenName);
            MonopolyAssets.SetObject(card, "nameField", nameField);
            MonopolyAssets.SetObject(card, "previous", previous);
            MonopolyAssets.SetObject(card, "next", next);
            MonopolyAssets.SetObjects(card, "kinds", kinds);
            MonopolyAssets.SetObjects(card, "kindBackgrounds", kindBackgrounds);
            MonopolyAssets.SetObjects(card, "levels", levels);
            MonopolyAssets.SetObjects(card, "levelBackgrounds", levelBackgrounds);
            MonopolyAssets.SetObject(card, "levelRow", levelRow.gameObject);
            MonopolyAssets.SetObject(card, "content", group);
            return card;
        }

        private static ResultsUI BuildResults(RectTransform layer)
        {
            RectTransform root = PopupRoot(layer, "Results", new Vector2(0f, 0f), new Vector2(980f, 860f), 0.55f, out CanvasGroup group, out RectTransform card);
            var results = root.gameObject.AddComponent<ResultsUI>();
            SetPopup(results, group, card);
            Image badge = Image(card, "WinnerBadge", MonopolyArtBuilder.UI("Circle"), MonopolyStyle.PlayerColor(0));
            Place(badge.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), new Vector2(170f, 170f));
            Image glow = Image(badge.transform, "Glow", MonopolyArtBuilder.UI("Glow"), new Color(1f, 0.85f, 0.3f, 0.6f));
            Stretch(glow.rectTransform, -70f, -70f, -70f, -70f);
            glow.transform.SetAsFirstSibling();
            Image ring = Image(badge.transform, "Ring", MonopolyArtBuilder.UI("Ring"), White);
            Stretch(ring.rectTransform, 6f, 6f, 6f, 6f);
            Image token = Image(badge.transform, "Token", MonopolyArtBuilder.Badge(0), White);
            Stretch(token.rectTransform, 28f, 28f, 28f, 28f);
            token.preserveAspect = true;
            TextMeshProUGUI crown = Icon(badge.transform, "Crown", Icons.Crown, 56f, MonopolyStyle.Gold);
            Place(crown.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, 14f), new Vector2(80f, 60f));
            TextMeshProUGUI headline = Text(card, "Headline", "You win!", 54f, Ink, Heavy);
            Place(headline.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(900f, 70f));
            TextMeshProUGUI subline = Text(card, "Subline", "", 20f, Muted, Body);
            Place(subline.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(900f, 30f));

            var rows = new List<ResultsUI.StandingRow>();
            for (int i = 0; i < 4; i++)
            {
                RectTransform row = Rect(card, $"Place {i + 1}");
                Place(row, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -236f - i * 92f), new Vector2(860f, 82f));
                Image back = Rounded(row, "Back", i == 0 ? MonopolyStyle.Tint(MonopolyStyle.Gold, 0.8f) : MonopolyStyle.Panel, 0.35f);
                Stretch(back.rectTransform);
                TextMeshProUGUI place = Text(row, "Place", "1", 36f, Ink, Heavy);
                Place(place.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(50f, 60f));
                Image rowBadge = Image(row, "Badge", MonopolyArtBuilder.UI("Circle"), MonopolyStyle.PlayerColor(i));
                Place(rowBadge.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(70f, 0f), new Vector2(62f, 62f));
                Image rowToken = Image(rowBadge.transform, "Token", MonopolyArtBuilder.Badge(i), White);
                Stretch(rowToken.rectTransform, 10f, 10f, 10f, 10f);
                rowToken.preserveAspect = true;
                TextMeshProUGUI name = Text(row, "Name", "Player", 26f, Ink, Bold, TextAlignmentOptions.MidlineLeft);
                Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(150f, -6f), new Vector2(460f, 38f));
                TextMeshProUGUI detail = Text(row, "Detail", "", 17f, Muted, Body, TextAlignmentOptions.MidlineLeft);
                Place(detail.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(150f, 6f), new Vector2(460f, 30f));
                TextMeshProUGUI worth = Text(row, "Worth", "$0", 32f, Ink, Heavy, TextAlignmentOptions.MidlineRight);
                Place(worth.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-22f, 0f), new Vector2(240f, 60f));
                rows.Add(new ResultsUI.StandingRow { root = row.gameObject, place = place, badge = rowBadge, token = rowToken, name = name, detail = detail, worth = worth });
            }
            var stars = new TextMeshProUGUI[3];
            for (int i = 0; i < 3; i++)
            {
                stars[i] = Icon(card, $"Star {i + 1}", Icons.Star, 58f, MonopolyStyle.Gold);
                Place(stars[i].rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 76f, 196f), new Vector2(70f, 70f));
            }
            TextMeshProUGUI caption = Text(card, "StarsCaption", "", 19f, Muted, Bold);
            Place(caption.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 122f), new Vector2(900f, 30f));
            Button again = Button(card, "Again", "PLAY AGAIN", Icons.Undo, MonopolyStyle.Red, new Vector2(300f, 72f), 26f);
            Place((RectTransform)again.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(160f, 30f), new Vector2(300f, 72f));
            Button menu = Button(card, "Menu", "MAIN MENU", Icons.Home, Soft, new Vector2(280f, 72f), 26f);
            Place((RectTransform)menu.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-160f, 30f), new Vector2(280f, 72f));
            MonopolyAssets.SetObject(results, "winnerBadge", badge);
            MonopolyAssets.SetObject(results, "winnerToken", token);
            MonopolyAssets.SetObject(results, "headline", headline);
            MonopolyAssets.SetObject(results, "subline", subline);
            MonopolyAssets.Set(results, "rows", list =>
            {
                list.arraySize = rows.Count;
                for (int i = 0; i < rows.Count; i++)
                {
                    var entry = list.GetArrayElementAtIndex(i);
                    entry.FindPropertyRelative("root").objectReferenceValue = rows[i].root;
                    entry.FindPropertyRelative("place").objectReferenceValue = rows[i].place;
                    entry.FindPropertyRelative("badge").objectReferenceValue = rows[i].badge;
                    entry.FindPropertyRelative("token").objectReferenceValue = rows[i].token;
                    entry.FindPropertyRelative("name").objectReferenceValue = rows[i].name;
                    entry.FindPropertyRelative("detail").objectReferenceValue = rows[i].detail;
                    entry.FindPropertyRelative("worth").objectReferenceValue = rows[i].worth;
                }
            });
            MonopolyAssets.SetObjects(results, "stars", stars);
            MonopolyAssets.SetObject(results, "starsCaption", caption);
            MonopolyAssets.SetObject(results, "againButton", again);
            MonopolyAssets.SetObject(results, "menuButton", menu);
            return results;
        }

        // ------------------------------------------------------------------ menu

        private struct MenuParts
        {
            public RectTransform header;
            public GameObject logo;
            public TextMeshProUGUI pauseTitle;
            public RectTransform buttons;
            public Button start;
            public Button resume;
            public Button load;
            public Button save;
            public Button houseRules;
            public Button mainMenu;
            public Button exit;
            public TextMeshProUGUI footer;
            public MonopolySettingsUI settings;
        }

        private static MenuParts BuildMenu(RectTransform menu, MonopolyUI ui)
        {
            var parts = new MenuParts();
            parts.header = Rect(menu, "Header");
            Place(parts.header, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(1000f, 330f));

            RectTransform logo = Rect(parts.header, "Logo");
            Stretch(logo);
            Image banner = Image(logo, "Banner", MonopolyArtBuilder.UI("Logo"), White);
            Center(banner.rectTransform, new Vector2(0f, 40f), new Vector2(900f, 264f));
            TextMeshProUGUI word = Text(banner.transform, "Word", "MONOPOLY", 150f, White, Heavy);
            Stretch(word.rectTransform, 40f, 20f, 40f, 16f);
            word.fontSharedMaterial = MonopolyArtBuilder.TitleMaterial;
            word.enableAutoSizing = true;
            word.fontSizeMin = 60f;
            word.fontSizeMax = 150f;
            word.characterSpacing = 4f;
            Image ribbon = Rounded(logo, "Edition", White, 0.8f);
            Center(ribbon.rectTransform, new Vector2(0f, -122f), new Vector2(460f, 54f));
            TextMeshProUGUI edition = Text(ribbon.transform, "Text", "WORLD TOUR EDITION", 26f, MonopolyStyle.Red, Heavy);
            Stretch(edition.rectTransform);
            edition.characterSpacing = 8f;
            parts.logo = logo.gameObject;

            parts.pauseTitle = Text(parts.header, "PauseTitle", "PAUSED", 84f, White, Heavy);
            Center(parts.pauseTitle.rectTransform, new Vector2(0f, 40f), new Vector2(900f, 120f));
            parts.pauseTitle.fontSharedMaterial = MonopolyArtBuilder.TitleMaterial;
            parts.pauseTitle.gameObject.SetActive(false);

            VerticalLayoutGroup column = Column(menu, "MenuButtons", 12f, TextAnchor.UpperCenter);
            parts.buttons = (RectTransform)column.transform;
            Place(parts.buttons, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -410f), new Vector2(440f, 620f));
            parts.resume = Button(column.transform, "ReturnToGame", "RESUME", Icons.Play, MonopolyStyle.Green, new Vector2(440f, 74f), 30f);
            parts.start = Button(column.transform, "StartNewGame", "PLAY", Icons.Dice, MonopolyStyle.Red, new Vector2(440f, 74f), 30f);
            parts.load = Button(column.transform, "LoadGame", "CONTINUE", Icons.Folder, White, new Vector2(440f, 74f), 28f);
            parts.save = Button(column.transform, "SaveGame", "SAVE GAME", Icons.Save, White, new Vector2(440f, 74f), 28f);
            parts.houseRules = Button(column.transform, "Game Settings", "HOUSE RULES", Icons.Gear, White, new Vector2(440f, 74f), 28f);
            parts.mainMenu = Button(column.transform, "MainMenu", "MAIN MENU", Icons.Home, White, new Vector2(440f, 74f), 28f);
            parts.exit = Button(column.transform, "ExitGame", "EXIT", Icons.Exit, White, new Vector2(440f, 74f), 28f);
            parts.load.interactable = false;

            parts.footer = Text(menu, "Footer", "Up to 4 players on one screen  •  computer opponents  •  6 ways to play", 20f, new Color(1f, 1f, 1f, 0.8f), Bold);
            Place(parts.footer.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(1400f, 34f));
            parts.footer.fontSharedMaterial = MonopolyArtBuilder.TextShadow;

            parts.settings = BuildSettings(menu, ui);
            return parts;
        }

        private static MonopolySettingsUI BuildSettings(RectTransform menu, MonopolyUI owner)
        {
            RectTransform rect = Rect(menu, "SettingsPanel");
            Center(rect, new Vector2(0f, -10f), new Vector2(840f, 960f));
            Image face = Card(rect, "Card", White, 0.6f);
            Stretch((RectTransform)face.transform.parent);
            var settings = rect.gameObject.AddComponent<MonopolySettingsUI>();
            TextMeshProUGUI title = Text(rect, "Title", "HOUSE RULES", 40f, Ink, Heavy);
            Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(760f, 50f));
            TextMeshProUGUI subtitle = Text(rect, "Subtitle", "The rules of the Custom Rules mode. Standard rules are locked; switch to your own to edit them.", 17f, Muted, Body);
            Place(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -76f), new Vector2(760f, 50f));

            VerticalLayoutGroup list = Column(rect, "InputPanel", 4f, TextAnchor.UpperCenter);
            list.childControlWidth = true;
            list.childControlHeight = true;
            list.childForceExpandWidth = true;
            list.childForceExpandHeight = false;
            Place((RectTransform)list.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -134f), new Vector2(760f, 620f));
            TMP_InputField cash = NumberInput(list.transform, "StartingCash", "Starting cash ($)", 760f);
            TMP_InputField rounds = NumberInput(list.transform, "RoundLimit", "Round limit (0 = none)", 760f);
            TMP_InputField dealt = NumberInput(list.transform, "Dealt", "Title deeds dealt at the start", 760f);
            Toggle auctions = Switch(list.transform, "Auctions", "Auctions for properties nobody buys", 760f);
            Toggle speed = Switch(list.transform, "SpeedDie", "Speed die (Mega Edition)", 760f);
            Toggle jackpot = Switch(list.transform, "Jackpot", "Free Parking jackpot", 760f);
            Toggle doubleGo = Switch(list.transform, "DoubleGo", "Double salary for landing on GO", 760f);
            Toggle noRent = Switch(list.transform, "NoRentInJail", "No rent collected from jail", 760f);
            Toggle party = Switch(list.transform, "PartyCards", "Party cards in the decks", 760f);
            Toggle fast = Switch(list.transform, "Fast", "Fast animations", 760f);

            Button custom = Button(rect, "Custom Settings", "Edit house rules", Icons.Gear, MonopolyStyle.Blue, new Vector2(420f, 62f), 22f);
            Place((RectTransform)custom.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 108f), new Vector2(420f, 62f));
            Button save = Button(rect, "SaveSettingsButton", "Save", Icons.Save, MonopolyStyle.Green, new Vector2(200f, 62f), 22f);
            Place((RectTransform)save.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-220f, 28f), new Vector2(200f, 62f));
            Button load = Button(rect, "LoadSettingsButton", "Load", Icons.Folder, Soft, new Vector2(200f, 62f), 22f);
            Place((RectTransform)load.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(200f, 62f));
            Button back = Button(rect, "BackToMenu", "Back", Icons.Left, Soft, new Vector2(200f, 62f), 22f);
            Place((RectTransform)back.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(220f, 28f), new Vector2(200f, 62f));

            MonopolyAssets.SetObject(settings, "startingCash", cash);
            MonopolyAssets.SetObject(settings, "roundLimit", rounds);
            MonopolyAssets.SetObject(settings, "dealtProperties", dealt);
            MonopolyAssets.SetObject(settings, "auctions", auctions);
            MonopolyAssets.SetObject(settings, "speedDie", speed);
            MonopolyAssets.SetObject(settings, "jackpot", jackpot);
            MonopolyAssets.SetObject(settings, "doubleGo", doubleGo);
            MonopolyAssets.SetObject(settings, "noRentInJail", noRent);
            MonopolyAssets.SetObject(settings, "partyCards", party);
            MonopolyAssets.SetObject(settings, "fastAnimations", fast);
            MonopolyAssets.SetObject(settings, "CustomSettingsButtonText", LabelOf(custom));
            MonopolyAssets.SetObject(settings, "CustomSettingsButton", custom);
            MonopolyAssets.SetObject(settings, "SaveSettingsButton", save);
            MonopolyAssets.SetObject(settings, "LoadSettingsButton", load);
            MonopolyAssets.SetObject(settings, "BackButton", back);
            MonopolyAssets.SetObject(settings, "OwnerUI", owner);
            MonopolyAssets.SetObject(settings, "DefaultSettings", MonopolyContentBuilder.DefaultSettings);
            rect.gameObject.SetActive(false);
            return settings;
        }
    }
}
