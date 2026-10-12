using System.Collections.Generic;
using Gamebox;
using Gamebox.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Checkers
{
    /// <summary>
    /// The campaign: a row per level with its title, the rules and the computer's strength, the stars earned on it and
    /// a Play button; a level is open once the one before it is won, the rest are dimmed. The hint counts the stars of
    /// the whole campaign. Built in code over the game (<see cref="OverlayWindow"/>) in the game's look.
    /// </summary>
    public sealed class CampaignWindow : OverlayWindow
    {
        private static readonly string[] LevelNames = { "Easy", "Normal", "Hard" };

        /// <summary>Width of a star of a row and the distance between the middles of two.</summary>
        private const float StarSize = 40f;
        private const float StarStep = 50f;

        private sealed class LevelRow
        {
            public RectTransform Row;
            public CanvasGroup Group;
            public TMP_Text Title;
            public TMP_Text Words;
            public Image[] Stars;
            public Button Play;
        }

        private readonly List<LevelRow> rows = new List<LevelRow>();

        protected override string CanvasName => "CheckersCampaign";

        protected override Vector2 PanelSize => new Vector2(1240f, 960f);

        /// <summary>The campaign window of <paramref name="manager"/>'s game (added to the manager when it has none).</summary>
        public static CampaignWindow For(BaseGameManager manager)
        {
            return For<CampaignWindow>(manager);
        }

        public void Show()
        {
            Open();
        }

        private CheckersGameManager Checkers => manager as CheckersGameManager;

        private CheckersCampaign Campaign => manager != null ? manager.Campaign as CheckersCampaign : null;

        private CampaignProgress Progress => Checkers != null ? Checkers.Progress : null;

        /// <summary>The star picture the HUD was built with (null leaves the stars of the rows unseen).</summary>
        private Sprite Star => Checkers != null && Checkers.UI is CheckersUI ui ? ui.StarSprite : null;

        protected override void BuildContent()
        {
            rows.Clear();
            title.text = L("Campaign");
            // Six rows of 118 and five gaps of 12 are 768: between the hint (140 from the top) and 40 over the panel's
            // bottom edge the list has 780, so every level shows at once.
            RectTransform list = kit.ScrollList(panel, "Levels", new Vector2(40f, 40f), new Vector2(-40f, -140f), 12f);
            CheckersCampaign campaign = Campaign;
            int count = campaign != null ? campaign.Count : 0;
            if (count == 0)
            {
                kit.Note(list, L("There are no levels yet."));
                return;
            }
            OverlayLook look = kit.Look;
            float width = PanelSize.x - 80f;
            for (int i = 0; i < count; i++)
            {
                int index = i;
                var level = new LevelRow();
                level.Row = kit.Row(list, 118f);
                level.Group = level.Row.gameObject.AddComponent<CanvasGroup>();
                level.Title = kit.Text(level.Row, "Title", "", 32f, look.text, TextAlignmentOptions.MidlineLeft, FontStyles.Bold, true);
                OverlayKit.Anchor(level.Title.rectTransform, new Vector2(0f, 1f), new Vector2(28f, -10f), new Vector2(width - 420f, 44f));
                OverlayKit.Fit(level.Title, 32f);
                // Two lines of words: the rules and the opponent, then the sentence about the level.
                level.Words = kit.Text(level.Row, "Words", "", 21f, look.muted, TextAlignmentOptions.TopLeft);
                level.Words.textWrappingMode = TextWrappingModes.Normal;
                OverlayKit.Anchor(level.Words.rectTransform, new Vector2(0f, 1f), new Vector2(28f, -54f), new Vector2(width - 420f, 58f));
                OverlayKit.Fit(level.Words, 21f);
                level.Stars = BuildStars(level.Row, campaign.MaxStarsPerLevel, look);
                level.Play = kit.MakeButton(level.Row, "Play", look.accent, new Vector2(160f, 60f), () => PlayLevel(index));
                OverlayKit.Anchor(level.Play.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(-28f, 0f), new Vector2(160f, 60f));
                rows.Add(level);
            }
        }

        protected override void Refresh()
        {
            CheckersCampaign campaign = Campaign;
            CampaignProgress progress = Progress;
            if (campaign == null)
            {
                hint.text = "";
                return;
            }
            hint.text = LF("{0} of {1} stars", campaign.TotalStars(progress), campaign.MaxStars) + "   " + L(CheckersCampaign.StarRules);
            for (int i = 0; i < rows.Count; i++)
            {
                LevelRow row = rows[i];
                CheckersLevel level = campaign.Level(i);
                bool unlocked = campaign.IsUnlocked(i, progress);
                int stars = progress != null ? progress.Stars(i) : 0;
                string name = level != null ? Portfolio.Checkers.L.Data(level.Title) : "";
                row.Title.text = LF("Level {0}", i + 1) + (string.IsNullOrEmpty(name) ? "" : ": " + name);
                row.Words.text = level != null ? Words(level) : "";
                ShowStars(row.Stars, stars, kit.Look);
                row.Group.alpha = unlocked ? 1f : 0.45f;
                row.Group.interactable = unlocked;
                row.Play.interactable = unlocked;
                TMP_Text label = OverlayKit.LabelOf(row.Play);
                if (label != null)
                {
                    label.text = unlocked ? L("Play") : L("Locked");
                }
            }
        }

        /// <summary>"Russian, Normal computer, you play Light. A sentence about the level."</summary>
        private string Words(CheckersLevel level)
        {
            string rules = L(VariantRules.Of(level.Variant).NameKey);
            int strength = Mathf.Clamp(level.ComputerLevel, 0, LevelNames.Length - 1);
            string words = LF("{0}, {1} computer, you play {2}", rules, L(LevelNames[strength]), L(Sides.NameKey(level.Side)));
            string description = Portfolio.Checkers.L.Data(level.Description);
            return string.IsNullOrEmpty(description) ? words : words + ".  " + description;
        }

        /// <summary>The star pictures of a row, between the words and the Play button, centred 325 from the row's right edge.</summary>
        private Image[] BuildStars(RectTransform row, int count, OverlayLook look)
        {
            Sprite star = Star;
            var images = new Image[count];
            // The right edge of the rightmost star; the earned stars fill from the left.
            float right = -325f + (count - 1) * StarStep * 0.5f + StarSize * 0.5f;
            for (int i = 0; i < count; i++)
            {
                Image image = OverlayKit.NewImage(row, $"Star {i + 1}", look.muted);
                image.sprite = star;
                image.enabled = star != null;
                image.preserveAspect = true;
                image.raycastTarget = false;
                OverlayKit.Anchor(image.rectTransform, new Vector2(1f, 0.5f), new Vector2(right - (count - 1 - i) * StarStep, 0f), new Vector2(StarSize, StarSize));
                images[i] = image;
            }
            return images;
        }

        /// <summary>The stars of a level: the earned ones in the accent colour, the rest faint.</summary>
        private static void ShowStars(Image[] stars, int earned, OverlayLook look)
        {
            Color dim = look.muted;
            dim.a *= 0.45f;
            for (int i = 0; i < stars.Length; i++)
            {
                if (stars[i] != null)
                {
                    stars[i].color = i < earned ? look.accent : dim;
                }
            }
        }

        private void PlayLevel(int index)
        {
            CheckersCampaign campaign = Campaign;
            if (campaign == null || !campaign.IsUnlocked(index, Progress))
            {
                ShowMessage(L("Win the level before it first."));
                return;
            }
            Hide();
            Checkers?.StartCampaignLevel(index);
        }
    }
}
