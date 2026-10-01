using System.Collections.Generic;
using Gamebox;
using Gamebox.Online;
using Gamebox.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.EndlessRunner
{
    /// <summary>
    /// The HUD of one runner of a local race, over its part of the split screen (<see cref="SplitScreen.AnchorTo"/>): the
    /// player's name in the colour of the seat and the place in the race, the hearts, the coins, the score and the
    /// distance, the power-ups running out, words for this player alone (a power-up picked up, a shield that saved them)
    /// and a banner once the run is over (across the line or out of hearts, and who the view follows now). Built in code
    /// in the look of the game's theme; the runner's whole HUD of the game alone stays hidden meanwhile.
    /// </summary>
    public class RunnerSeatHud : MonoBehaviour
    {
        private sealed class PowerUpIcon
        {
            public PowerUpType Type;
            public GameObject Root;
            public Image Fill;
            public Image Icon;
        }

        private const float DesignHeight = 540f;

        private RectTransform content;
        private Image panel;
        private Image strip;
        private Image flash;
        private TMP_Text nameText;
        private TMP_Text placeText;
        private TMP_Text coinsText;
        private TMP_Text scoreText;
        private TMP_Text distanceText;
        private TMP_Text toastText;
        private TMP_Text bannerText;
        private Image coinIcon;
        private RectTransform heartsRow;
        private CanvasGroup banner;
        private readonly List<Image> hearts = new List<Image>();
        private readonly List<PowerUpIcon> powerUps = new List<PowerUpIcon>();
        private LocalSeat seat;
        private RunnerGameTheme look;
        private float scale = 1f;
        private Vector2 heartsRest;
        private int shownPlace = -1;
        private int shownCoins = -1;
        private int shownScore = -1;
        private int shownMeters = -1;
        private int shownHearts = -1;
        private int shownMaxHearts = -1;
        private bool endless;
        private float length;
        private float toastTime = -1f;
        private float flashAlpha;
        private float heartShake;
        private float coinPunch;

        /// <summary>
        /// Builds the HUD of <paramref name="seat"/> over <paramref name="viewport"/> (a normalized rect of the screen) under
        /// <paramref name="parent"/>, a rect that covers the screen.
        /// </summary>
        public static RunnerSeatHud Create(Transform parent, Rect viewport, LocalSeat seat, RunnerGameTheme look)
        {
            RectTransform root = OverlayKit.Rect(parent, $"Seat {seat.Index + 1}");
            SplitScreen.AnchorTo(root, viewport);
            var hud = root.gameObject.AddComponent<RunnerSeatHud>();
            hud.seat = seat;
            hud.look = look;
            // A quarter of the screen is as tall as a half: the HUD keeps one size, a little smaller in a quarter.
            hud.scale = viewport.width < 0.75f && viewport.height < 0.75f ? 0.82f : 1f;
            hud.Build(root);
            return hud;
        }

        private InterfaceSprites Sprites => look != null ? look.Sprites : null;

        private InterfaceColors Colors => look != null ? look.Colors : new InterfaceColors();

        private void Build(RectTransform root)
        {
            flash = Picture(root, "DamageFlash", Sprites?.vignette, new Color(1f, 0.1f, 0.1f, 0f));
            OverlayKit.Stretch(flash.rectTransform);

            content = OverlayKit.Rect(root, "Content");
            content.anchorMin = content.anchorMax = content.pivot = new Vector2(0.5f, 0.5f);
            content.localScale = Vector3.one * scale;
            Fit();

            panel = Picture(content, "Panel", Sprites?.panel, Colors.panel);
            panel.type = Image.Type.Sliced;
            OverlayKit.Anchor(panel.rectTransform, new Vector2(0f, 1f), new Vector2(18f, -18f), new Vector2(440f, 196f));
            Transform card = panel.transform;

            strip = Picture(card, "Colour", Sprites?.button, seat.Color);
            strip.type = Image.Type.Sliced;
            OverlayKit.Anchor(strip.rectTransform, new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(12f, 172f));

            nameText = Words(card, "Name", seat.Name, 34f, Color.Lerp(seat.Color, Color.white, 0.35f), TextAlignmentOptions.Left, TextRole.Body);
            OverlayKit.Anchor(nameText.rectTransform, new Vector2(0f, 1f), new Vector2(32f, -10f), new Vector2(270f, 46f));
            nameText.enableAutoSizing = true;
            nameText.fontSizeMin = 18f;
            nameText.fontSizeMax = 34f;
            placeText = Words(card, "Place", string.Empty, 52f, Colors.accent, TextAlignmentOptions.Right, TextRole.Title);
            OverlayKit.Anchor(placeText.rectTransform, new Vector2(1f, 1f), new Vector2(-16f, -4f), new Vector2(130f, 64f));

            heartsRow = OverlayKit.Rect(card, "Hearts");
            OverlayKit.Anchor(heartsRow, new Vector2(0f, 1f), new Vector2(32f, -60f), new Vector2(390f, 40f));
            heartsRest = heartsRow.anchoredPosition;
            var row = heartsRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 4f;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = row.childControlHeight = false;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            for (int i = 0; i < RunnerSettings.HeartLimit; i++)
            {
                Image heart = Picture(heartsRow, $"Heart{i + 1}", Sprites?.heart, Color.white);
                heart.rectTransform.sizeDelta = new Vector2(38f, 38f);
                heart.preserveAspect = true;
                hearts.Add(heart);
            }

            coinIcon = Picture(card, "Coin", Sprites?.coin, Color.white);
            coinIcon.preserveAspect = true;
            OverlayKit.Anchor(coinIcon.rectTransform, new Vector2(0f, 1f), new Vector2(30f, -106f), new Vector2(46f, 46f));
            coinsText = Words(card, "Coins", "0", 42f, Color.white, TextAlignmentOptions.Left, TextRole.Body);
            OverlayKit.Anchor(coinsText.rectTransform, new Vector2(0f, 1f), new Vector2(84f, -102f), new Vector2(150f, 54f));
            scoreText = Words(card, "Score", "0", 42f, Color.white, TextAlignmentOptions.Right, TextRole.Body);
            OverlayKit.Anchor(scoreText.rectTransform, new Vector2(1f, 1f), new Vector2(-16f, -102f), new Vector2(200f, 54f));
            distanceText = Words(card, "Distance", string.Empty, 25f, Colors.soft, TextAlignmentOptions.Left, TextRole.Body);
            OverlayKit.Anchor(distanceText.rectTransform, new Vector2(0f, 1f), new Vector2(32f, -152f), new Vector2(390f, 34f));

            RectTransform powers = OverlayKit.Rect(content, "PowerUps");
            OverlayKit.Anchor(powers, new Vector2(0f, 1f), new Vector2(22f, -224f), new Vector2(4f * 80f, 76f));
            var powerRow = powers.gameObject.AddComponent<HorizontalLayoutGroup>();
            powerRow.spacing = 8f;
            powerRow.childAlignment = TextAnchor.MiddleLeft;
            powerRow.childControlWidth = powerRow.childControlHeight = false;
            powerRow.childForceExpandWidth = powerRow.childForceExpandHeight = false;
            for (int i = 0; i < PowerUps.Count; i++)
            {
                powerUps.Add(PowerUpSlot(powers, (PowerUpType)i));
            }

            toastText = Words(content, "Toast", string.Empty, 56f, Color.white, TextAlignmentOptions.Center, TextRole.Title);
            OverlayKit.Anchor(toastText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(900f, 200f));
            toastText.gameObject.SetActive(false);

            Image bannerPanel = Picture(content, "Banner", Sprites?.panel, Colors.panel);
            bannerPanel.type = Image.Type.Sliced;
            OverlayKit.Anchor(bannerPanel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 36f), new Vector2(820f, 128f));
            banner = bannerPanel.gameObject.AddComponent<CanvasGroup>();
            banner.blocksRaycasts = false;
            banner.alpha = 0f;
            bannerText = Words(bannerPanel.transform, "Text", string.Empty, 40f, Color.white, TextAlignmentOptions.Center, TextRole.Body);
            OverlayKit.Stretch(bannerText.rectTransform);
            bannerText.margin = new Vector4(24f, 8f, 24f, 8f);
            bannerText.enableAutoSizing = true;
            bannerText.fontSizeMin = 20f;
            bannerText.fontSizeMax = 40f;
        }

        private PowerUpIcon PowerUpSlot(Transform parent, PowerUpType type)
        {
            RectTransform slot = OverlayKit.Rect(parent, type.ToString());
            slot.sizeDelta = new Vector2(76f, 76f);
            Image back = Picture(slot, "Back", Sprites?.glow, new Color(0f, 0f, 0.05f, 0.75f));
            OverlayKit.Anchor(back.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(90f, 90f));
            Image fill = Picture(slot, "Timer", Sprites?.ring, Colors.PowerUp(type));
            OverlayKit.Anchor(fill.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(78f, 78f));
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Radial360;
            fill.fillOrigin = (int)Image.Origin360.Top;
            fill.fillClockwise = false;
            Image icon = Picture(slot, "Icon", PowerUpSprite(type), Color.white);
            icon.preserveAspect = true;
            OverlayKit.Anchor(icon.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(46f, 46f));
            slot.gameObject.SetActive(false);
            return new PowerUpIcon { Type = type, Root = slot.gameObject, Fill = fill, Icon = icon };
        }

        private Sprite PowerUpSprite(PowerUpType type)
        {
            InterfaceSprites sprites = Sprites;
            if (sprites == null)
            {
                return null;
            }
            switch (type)
            {
                case PowerUpType.Magnet: return sprites.magnet;
                case PowerUpType.Shield: return sprites.shield;
                case PowerUpType.Multiplier: return sprites.multiplier;
                default: return sprites.spring;
            }
        }

        private static Image Picture(Transform parent, string pictureName, Sprite sprite, Color color)
        {
            Image image = OverlayKit.NewImage(parent, pictureName, color);
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        private TMP_Text Words(Transform parent, string textName, string text, float size, Color color, TextAlignmentOptions alignment, TextRole role)
        {
            RectTransform rect = OverlayKit.Rect(parent, textName);
            var words = rect.gameObject.AddComponent<TextMeshProUGUI>();
            look?.Fonts.Apply(words, role);
            words.text = text;
            words.fontSize = size;
            words.color = color;
            words.alignment = alignment;
            words.textWrappingMode = TextWrappingModes.NoWrap;
            words.overflowMode = TextOverflowModes.Overflow;
            words.raycastTarget = false;
            return words;
        }

        // ------------------------------------------------------------------------------------------------
        // What the race shows
        // ------------------------------------------------------------------------------------------------

        /// <summary>A run begins: full hearts, nothing collected, no banner.</summary>
        public void Begin(bool endlessRun, float runLength, int maxHearts)
        {
            endless = endlessRun;
            length = runLength;
            toastTime = -1f;
            toastText.gameObject.SetActive(false);
            flashAlpha = 0f;
            heartShake = 0f;
            banner.alpha = 0f;
            Redraw();
            foreach (PowerUpIcon slot in powerUps)
            {
                slot.Root.SetActive(false);
            }
            UpdateRun(0, 0, 0f, maxHearts, maxHearts);
        }

        /// <summary>Everything is written again at the next update (the language changed).</summary>
        public void Redraw()
        {
            shownPlace = shownCoins = shownScore = shownMeters = shownHearts = shownMaxHearts = -1;
        }

        public void UpdateRun(int coins, int score, float distance, int heartsLeft, int maxHearts)
        {
            if (coins != shownCoins)
            {
                shownCoins = coins;
                coinsText.text = coins.ToString();
            }
            if (score != shownScore)
            {
                shownScore = score;
                scoreText.text = score.ToString();
            }
            int meters = Mathf.FloorToInt(Mathf.Max(0f, distance));
            if (meters != shownMeters)
            {
                shownMeters = meters;
                distanceText.text = endless || length <= 0f ? RunnerText.F("{0} m", meters) : RunnerText.F("{0} / {1:0} m", meters, length);
            }
            if (heartsLeft != shownHearts || maxHearts != shownMaxHearts)
            {
                shownHearts = heartsLeft;
                shownMaxHearts = maxHearts;
                for (int i = 0; i < hearts.Count; i++)
                {
                    hearts[i].gameObject.SetActive(i < maxHearts);
                    hearts[i].sprite = i < heartsLeft ? Sprites?.heart : Sprites?.heartEmpty;
                }
            }
        }

        public void SetPlace(int place)
        {
            if (place != shownPlace)
            {
                shownPlace = place;
                placeText.text = place > 0 ? Standings.Ordinal(place) : string.Empty;
            }
        }

        /// <summary>How much of a power-up is left, from 1 to 0 (gone).</summary>
        public void SetPowerUp(PowerUpType type, float fraction)
        {
            foreach (PowerUpIcon slot in powerUps)
            {
                if (slot.Type != type)
                {
                    continue;
                }
                bool active = fraction > 0f;
                if (slot.Root.activeSelf != active)
                {
                    slot.Root.SetActive(active);
                }
                slot.Fill.fillAmount = fraction;
            }
        }

        public void PunchCoins()
        {
            coinPunch = 1f;
        }

        public void HeartLost()
        {
            heartShake = 1f;
            flashAlpha = 0.5f;
        }

        /// <summary>Words for this player for a moment, already in the language and the theme's words.</summary>
        public void Toast(string text, Color color)
        {
            toastText.text = text;
            toastText.color = Color.Lerp(color, Color.white, 0.25f);
            toastText.gameObject.SetActive(true);
            toastTime = 0f;
        }

        /// <summary>A banner at the bottom of the view that stays until it changes: the run is over, who is watched. Null hides it.</summary>
        public void Banner(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                banner.alpha = 0f;
                return;
            }
            if (bannerText.text != text)
            {
                bannerText.text = text;
            }
            banner.alpha = 1f;
        }

        /// <summary>Draws the HUD in the look of another theme.</summary>
        public void ApplyTheme(RunnerGameTheme theme)
        {
            if (theme == null)
            {
                return;
            }
            look = theme;
            panel.sprite = Sprites.panel;
            panel.color = Colors.panel;
            banner.GetComponent<Image>().sprite = Sprites.panel;
            banner.GetComponent<Image>().color = Colors.panel;
            coinIcon.sprite = Sprites.coin;
            placeText.color = Colors.accent;
            distanceText.color = Colors.soft;
            theme.Fonts.Apply(placeText, TextRole.Title);
            theme.Fonts.Apply(toastText, TextRole.Title);
            foreach (TMP_Text text in new[] { nameText, coinsText, scoreText, distanceText, bannerText })
            {
                theme.Fonts.Apply(text, TextRole.Body);
            }
            foreach (PowerUpIcon slot in powerUps)
            {
                slot.Icon.sprite = PowerUpSprite(slot.Type);
                slot.Fill.color = Colors.PowerUp(slot.Type);
            }
            Redraw();
        }

        // ------------------------------------------------------------------------------------------------
        // Animation
        // ------------------------------------------------------------------------------------------------

        /// <summary>The content keeps the size of the view at its scale, whatever the shape of the screen.</summary>
        private void Fit()
        {
            Vector2 size = ((RectTransform)transform).rect.size / scale;
            if (content.sizeDelta != size)
            {
                content.sizeDelta = size;
            }
        }

        private void Update()
        {
            Fit();
            float deltaTime = Time.unscaledDeltaTime;
            coinPunch = Mathf.MoveTowards(coinPunch, 0f, deltaTime * 5f);
            coinIcon.rectTransform.localScale = Vector3.one * (1f + coinPunch * 0.35f);

            heartShake = Mathf.MoveTowards(heartShake, 0f, deltaTime * 2.5f);
            heartsRow.anchoredPosition = heartsRest + new Vector2(Mathf.Sin(Time.unscaledTime * 60f) * 8f * heartShake, 0f);

            flashAlpha = Mathf.MoveTowards(flashAlpha, 0f, deltaTime * 1.2f);
            Color color = flash.color;
            color.a = flashAlpha;
            flash.color = color;
            flash.enabled = flashAlpha > 0.001f;

            if (toastTime >= 0f)
            {
                toastTime += deltaTime;
                float t = toastTime / 2.2f;
                toastText.rectTransform.anchoredPosition = new Vector2(0f, 60f + Tween.OutCubic(Mathf.Clamp01(t * 3f)) * 30f);
                toastText.alpha = t < 0.75f ? Mathf.Clamp01(toastTime * 6f) : Mathf.Clamp01(1f - (t - 0.75f) / 0.25f);
                if (t >= 1f)
                {
                    toastTime = -1f;
                    toastText.gameObject.SetActive(false);
                }
            }
        }
    }


    /// <summary>
    /// The standings of a local race of three, in the quarter of the screen no runner has: the runners by place, with their
    /// names in the colours of their seats, their scores, how far they got and their coins.
    /// </summary>
    public class LocalStandingsBoard : MonoBehaviour
    {
        private Image panel;
        private TMP_Text header;
        private TMP_Text lines;
        private string shown;

        /// <summary>Builds the board over <paramref name="viewport"/> (a normalized rect of the screen) under <paramref name="parent"/>.</summary>
        public static LocalStandingsBoard Create(Transform parent, Rect viewport, RunnerGameTheme look)
        {
            RectTransform root = OverlayKit.Rect(parent, "Standings");
            SplitScreen.AnchorTo(root, viewport);
            var board = root.gameObject.AddComponent<LocalStandingsBoard>();
            InterfaceColors colors = look != null ? look.Colors : new InterfaceColors();
            Image backdrop = OverlayKit.NewImage(root, "Backdrop", new Color(0.02f, 0.03f, 0.08f, 0.92f));
            backdrop.raycastTarget = false;
            OverlayKit.Stretch(backdrop.rectTransform);
            board.panel = OverlayKit.NewImage(root, "Panel", colors.panel);
            board.panel.sprite = look != null ? look.Sprites.panel : null;
            board.panel.type = Image.Type.Sliced;
            board.panel.raycastTarget = false;
            RectTransform rect = board.panel.rectTransform;
            rect.anchorMin = new Vector2(0.06f, 0.08f);
            rect.anchorMax = new Vector2(0.94f, 0.92f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            board.header = Text(rect, "Header", RunnerText.T("RACE"), 40f, colors.accent, TextAlignmentOptions.Center, look, TextRole.Title);
            board.header.rectTransform.anchorMin = new Vector2(0f, 1f);
            board.header.rectTransform.anchorMax = new Vector2(1f, 1f);
            board.header.rectTransform.pivot = new Vector2(0.5f, 1f);
            board.header.rectTransform.anchoredPosition = new Vector2(0f, -14f);
            board.header.rectTransform.sizeDelta = new Vector2(0f, 56f);
            board.lines = Text(rect, "Lines", string.Empty, 34f, Color.white, TextAlignmentOptions.Left, look, TextRole.Body);
            OverlayKit.Stretch(board.lines.rectTransform);
            board.lines.rectTransform.offsetMin = new Vector2(28f, 20f);
            board.lines.rectTransform.offsetMax = new Vector2(-28f, -80f);
            board.lines.textWrappingMode = TextWrappingModes.NoWrap;
            board.lines.enableAutoSizing = true;
            board.lines.fontSizeMin = 16f;
            board.lines.fontSizeMax = 34f;
            board.lines.lineSpacing = 18f;
            board.lines.alignment = TextAlignmentOptions.MidlineLeft;
            return board;
        }

        private static TMP_Text Text(Transform parent, string textName, string text, float size, Color color, TextAlignmentOptions alignment,
            RunnerGameTheme look, TextRole role)
        {
            RectTransform rect = OverlayKit.Rect(parent, textName);
            var words = rect.gameObject.AddComponent<TextMeshProUGUI>();
            look?.Fonts.Apply(words, role);
            words.text = text;
            words.fontSize = size;
            words.color = color;
            words.alignment = alignment;
            words.raycastTarget = false;
            return words;
        }

        /// <summary>The standings, a line a runner, in the language shown.</summary>
        public void Show(string standings)
        {
            header.text = RunnerText.T("RACE");
            if (standings != shown)
            {
                shown = standings;
                lines.text = standings;
            }
        }
    }
}
