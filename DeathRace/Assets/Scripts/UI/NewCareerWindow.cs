using System.Collections.Generic;
using Gamebox;
using Gamebox.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Portfolio.DeathRace.RaceWindowParts;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// The start of a career: the driver's name and colour, a card of what they start with (the Rustbucket, the money,
    /// the bottom of the ladder) and Start; when there is a saved career, Continue takes up the latest save instead.
    /// </summary>
    public sealed class NewCareerWindow : DeathRaceWindow
    {
        public const int MaxNameLength = 18;

        private const float Swatch = 50f;
        private const float SwatchStep = 58f;
        private const int SwatchesPerRow = 10;

        private readonly List<Image> rings = new List<Image>();
        private TMP_InputField nameField;
        private TMP_Text previewName;
        private Image previewChip;
        private Button continueButton;
        private TMP_Text continueLine;
        private string typedName;
        private int color;

        protected override string CanvasName => "DeathRaceNewCareer";

        protected override Vector2 PanelSize => new Vector2(1280f, 800f);

        protected override float MessageWidth => 560f;

        public static NewCareerWindow For(BaseGameManager manager)
        {
            return For<NewCareerWindow>(manager);
        }

        public void Show()
        {
            typedName = null;
            Present();
        }

        protected override void BeforeRebuild()
        {
            typedName = nameField != null ? nameField.text : typedName;
        }

        protected override void Compose()
        {
            rings.Clear();
            OverlayLook look = kit.Look;
            title.text = L("New Career");
            hint.text = L("Twenty drivers, one champion. You start at the bottom with a wreck, a little money and a grudge.");

            // The driver: name and colour on the left.
            ui.WordsAt(panel, L("Driver"), 26f, look.muted, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(48f, -160f),
                new Vector2(560f, 36f));
            nameField = kit.Input(panel, "Name", "Your driver's name", MaxNameLength);
            OverlayKit.Anchor(nameField.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(48f, -200f), new Vector2(600f, 64f));
            nameField.text = typedName ?? DefaultName();
            nameField.onValueChanged.AddListener(_ => RefreshPreview());

            ui.WordsAt(panel, L("Colour"), 26f, look.muted, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(48f, -296f),
                new Vector2(560f, 36f));
            for (int i = 0; i < Career.Colors; i++)
            {
                int index = i;
                Vector2 position = new Vector2(48f + (i % SwatchesPerRow) * SwatchStep, -340f - (i / SwatchesPerRow) * SwatchStep);
                Image ring = ui.Chip(panel, look.text, new Vector2(0f, 1f), position - new Vector2(4f, -4f), Swatch + 8f);
                rings.Add(ring);
                Image swatch = ui.Chip(panel, ui.DriverColor(i), new Vector2(0f, 1f), position, Swatch);
                swatch.raycastTarget = true;
                Button pick = swatch.gameObject.AddComponent<Button>();
                pick.targetGraphic = swatch;
                pick.onClick.AddListener(() =>
                {
                    color = index;
                    RefreshPreview();
                });
            }

            ui.WordsAt(panel, L("Rivals take the colours you leave."), 22f, look.muted, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f),
                new Vector2(48f, -470f), new Vector2(600f, 32f), FontStyles.Italic);

            BuildPreview(look);

            ui.ButtonAt(panel, "Start Career", Accent, new Vector2(1f, 0f), new Vector2(-48f, 40f), new Vector2(330f, 76f), StartCareer);
            continueButton = ui.ButtonAt(panel, "Continue", look.row, new Vector2(1f, 0f), new Vector2(-398f, 40f), new Vector2(260f, 76f), Continue);
            continueLine = ui.WordsAt(panel, "", 20f, look.muted, TextAlignmentOptions.MidlineRight, new Vector2(1f, 0f), new Vector2(-48f, 128f),
                new Vector2(610f, 30f));
        }

        /// <summary>The card on the right: the car, the name in its colour, what the career starts with.</summary>
        private void BuildPreview(OverlayLook look)
        {
            RectTransform card = ui.Box(panel, "Card", look.row);
            Span(card, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-540f, -560f), new Vector2(-48f, -150f));
            Picture(card, ui.CarPicture(CareerRules.StartCar), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(420f, 210f));
            previewChip = ui.Chip(card, ui.DriverColor(color), new Vector2(0f, 1f), new Vector2(28f, -246f), 40f);
            previewName = ui.WordsAt(card, "", 38f, look.text, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(84f, -240f),
                new Vector2(380f, 52f), FontStyles.Bold, true);
            string kitLine = LF("{0}  ·  {1} mines  ·  {2} nitro", RaceFormat.Money(CareerRules.StartMoney), CareerRules.StartMines, CareerRules.StartNitro);
            ui.WordsAt(card, CarName(CareerRules.StartCar), 26f, Accent, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(28f, -300f),
                new Vector2(440f, 36f), FontStyles.Bold);
            ui.WordsAt(card, kitLine, 24f, look.text, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(28f, -340f), new Vector2(440f, 32f));
            ui.WordsAt(card, LF("Rank {0} of {1}", RaceFormat.Rank(CareerRules.LadderSize), CareerRules.LadderSize), 24f, look.muted,
                TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(28f, -376f), new Vector2(440f, 32f));
        }

        protected override void Refresh()
        {
            SavedGame latest = manager != null ? manager.LatestSave : null;
            continueButton.gameObject.SetActive(latest != null);
            continueLine.text = latest != null ? LF("Latest save: {0}", latest.Summary ?? latest.Name) : "";
            RefreshPreview();
        }

        private void RefreshPreview()
        {
            for (int i = 0; i < rings.Count; i++)
            {
                rings[i].enabled = i == color;
            }
            previewChip.color = ui.DriverColor(color);
            string name = nameField != null ? nameField.text.Trim() : "";
            previewName.text = name.Length > 0 ? name : L("Rookie");
            previewName.color = Color.Lerp(ui.DriverColor(color), kit.Look.text, 0.25f);
        }

        /// <summary>The name of the avatar at this device when it has one, else "Rookie".</summary>
        private string DefaultName()
        {
            AvatarState avatar = LocalAvatar.Instance.State;
            return avatar != null && !string.IsNullOrWhiteSpace(avatar.Name) ? avatar.Name : L("Rookie");
        }

        private void StartCareer()
        {
            string name = nameField.text.Trim();
            if (name.Length == 0)
            {
                ShowMessage(L("Your driver needs a name."));
                return;
            }
            Hide();
            Game?.StartCareer(name, color);
        }

        private void Continue()
        {
            if (manager != null && manager.ContinueLatest())
            {
                Hide();
            }
            else
            {
                ShowMessage(L("The latest save could not be loaded."));
            }
        }
    }
}
