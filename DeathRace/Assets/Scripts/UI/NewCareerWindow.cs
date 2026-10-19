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
    /// The start of a career: the driver's name and colour, a card of the driver as the ladder will show them, and the
    /// three leagues to start in, each a card of its own (its name and line, the car and the money a career of it starts
    /// with, its champion and their car). A league not open on this device yet says what opens it (the title of the one
    /// before: <see cref="DeathRaceProgress.IsLeagueOpen"/>). Start begins a career in the league picked; when there is
    /// a saved career, Continue takes up the latest save instead.
    /// </summary>
    public sealed class NewCareerWindow : DeathRaceWindow
    {
        public const int MaxNameLength = 18;

        private const float Swatch = 50f;
        private const float SwatchStep = 58f;
        private const int SwatchesPerRow = 10;

        private sealed class LeagueCard
        {
            public LeagueSpec League;
            public Image Frame;
            public CanvasGroup Group;
        }

        private readonly List<Image> rings = new List<Image>();
        private readonly List<LeagueCard> cards = new List<LeagueCard>();
        private TMP_InputField nameField;
        private TMP_Text previewName;
        private TMP_Text previewLine;
        private Image previewChip;
        private Image previewCar;
        private Button startButton;
        private Button continueButton;
        private TMP_Text continueLine;
        private string typedName;
        private int color;
        private int league = -1;

        protected override string CanvasName => "DeathRaceNewCareer";

        protected override Vector2 PanelSize => new Vector2(1720f, 1000f);

        protected override float MessageWidth => 820f;

        public static NewCareerWindow For(BaseGameManager manager)
        {
            return For<NewCareerWindow>(manager);
        }

        /// <summary>The league picked (the highest one open on this device until the player picks another).</summary>
        public LeagueKind League => (LeagueKind)Mathf.Max(0, league);

        public void Show()
        {
            typedName = null;
            league = -1;
            Present();
        }

        /// <summary>For the career check: picks <paramref name="kind"/> as a click on its card would (a locked one is refused).</summary>
        internal bool Pick(LeagueKind kind)
        {
            return PickLeague(Leagues.Of(kind));
        }

        protected override void BeforeRebuild()
        {
            typedName = nameField != null ? nameField.text : typedName;
        }

        protected override void Compose()
        {
            rings.Clear();
            cards.Clear();
            OverlayLook look = kit.Look;
            if (league < 0)
            {
                league = HighestOpenLeague();
            }
            title.text = L("New Career");

            // The driver: name and colour on the left.
            ui.WordsAt(panel, L("Driver"), 26f, look.muted, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(48f, -150f),
                new Vector2(560f, 36f));
            nameField = kit.Input(panel, "Name", "Your driver's name", MaxNameLength);
            OverlayKit.Anchor(nameField.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(48f, -190f), new Vector2(600f, 64f));
            nameField.text = typedName ?? DefaultName();
            nameField.onValueChanged.AddListener(_ => RefreshPreview());

            ui.WordsAt(panel, L("Colour"), 26f, look.muted, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(48f, -272f),
                new Vector2(560f, 36f));
            for (int i = 0; i < Career.Colors; i++)
            {
                int index = i;
                Vector2 position = new Vector2(48f + (i % SwatchesPerRow) * SwatchStep, -314f - (i / SwatchesPerRow) * SwatchStep);
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
                new Vector2(48f, -440f), new Vector2(600f, 32f), FontStyles.Italic);

            BuildPreview(look);

            // The leagues across the bottom.
            ui.WordsAt(panel, L("League"), 26f, look.muted, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(48f, -490f),
                new Vector2(560f, 36f));
            const float gap = 24f;
            float width = (PanelSize.x - 96f - gap * (Leagues.Count - 1)) / Leagues.Count;
            for (int i = 0; i < Leagues.Count; i++)
            {
                cards.Add(BuildLeague(Leagues.Of(i), new Vector2(48f + i * (width + gap), -530f), new Vector2(width, 340f)));
            }

            startButton = ui.ButtonAt(panel, "Start Career", Accent, new Vector2(1f, 0f), new Vector2(-48f, 36f), new Vector2(330f, 76f), StartCareer);
            continueButton = ui.ButtonAt(panel, "Continue", look.row, new Vector2(1f, 0f), new Vector2(-398f, 36f), new Vector2(260f, 76f), Continue);
            continueLine = ui.WordsAt(panel, "", 20f, look.muted, TextAlignmentOptions.MidlineRight, new Vector2(1f, 0f), new Vector2(-678f, 54f),
                new Vector2(520f, 40f));
        }

        /// <summary>The card on the right: the car of the league picked, the name in its colour, where the career starts.</summary>
        private void BuildPreview(OverlayLook look)
        {
            RectTransform card = ui.Box(panel, "Card", look.row);
            Span(card, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-940f, -470f), new Vector2(-48f, -140f));
            previewCar = Picture(card, null, new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(380f, 230f));
            previewChip = ui.Chip(card, ui.DriverColor(color), new Vector2(0f, 1f), new Vector2(28f, -40f), 40f);
            previewName = ui.WordsAt(card, "", 38f, look.text, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(84f, -34f),
                new Vector2(400f, 52f), FontStyles.Bold, true);
            previewLine = ui.WordsAt(card, "", 22f, look.muted, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), new Vector2(28f, -104f),
                new Vector2(450f, 200f));
            previewLine.textWrappingMode = TextWrappingModes.Normal;
            previewLine.richText = true;
        }

        /// <summary>One league's card: its name, line, start car and kit, its champion; dimmed with what opens it while locked.</summary>
        private LeagueCard BuildLeague(LeagueSpec spec, Vector2 position, Vector2 size)
        {
            OverlayLook look = kit.Look;
            bool open = IsLeagueOpen(spec);
            var card = new LeagueCard { League = spec };
            card.Frame = OverlayKit.NewImage(panel, spec.Name, look.row);
            card.Frame.sprite = kit.Rounded;
            card.Frame.type = Image.Type.Sliced;
            OverlayKit.Anchor(card.Frame.rectTransform, new Vector2(0f, 1f), position, size);
            card.Group = card.Frame.gameObject.AddComponent<CanvasGroup>();
            RectTransform body = ui.Box(card.Frame.rectTransform, "Body", look.row);
            body.offsetMin = new Vector2(4f, 4f);
            body.offsetMax = new Vector2(-4f, -4f);
            Image back = body.GetComponent<Image>();
            back.raycastTarget = true;
            Button pick = body.gameObject.AddComponent<Button>();
            pick.targetGraphic = back;
            pick.onClick.AddListener(() =>
            {
                if (!PickLeague(spec))
                {
                    ShowMessage(OpensWith(spec));
                }
            });

            float width = size.x - 8f;
            Shape(body, open ? ui.Icon("League", RaceShapes.Flag) : ui.Lock, open ? Accent : look.muted, new Vector2(0f, 1f), new Vector2(18f, -16f),
                new Vector2(40f, 40f));
            ui.WordsAt(body, L(spec.Name), 30f, look.text, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(68f, -12f),
                new Vector2(width - 90f, 48f), FontStyles.Bold, true);
            Picture(body, ui.CarPicture(spec.StartCar), new Vector2(0f, 1f), new Vector2(14f, -70f), new Vector2(190f, 104f));
            ui.WordsAt(body, CarName(spec.StartCar), 24f, Accent, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(214f, -72f),
                new Vector2(width - 230f, 34f), FontStyles.Bold);
            ui.WordsAt(body, RaceFormat.Money(spec.StartMoney), 24f, look.good, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f),
                new Vector2(214f, -106f), new Vector2(width - 230f, 34f), FontStyles.Bold);
            TMP_Text kitLine = ui.WordsAt(body, KitLine(spec), 18f, look.muted, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f),
                new Vector2(214f, -140f), new Vector2(width - 230f, 44f));
            kitLine.textWrappingMode = TextWrappingModes.Normal;
            TMP_Text blurb = ui.WordsAt(body, L(spec.Blurb), 19f, Color.Lerp(look.text, look.muted, 0.35f), TextAlignmentOptions.TopLeft,
                new Vector2(0f, 1f), new Vector2(18f, -190f), new Vector2(width - 36f, 54f));
            blurb.textWrappingMode = TextWrappingModes.Normal;
            blurb.fontSizeMin = 14f;
            if (!open)
            {
                ui.WordsAt(body, OpensWith(spec), 20f, Warn, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(18f, -248f),
                    new Vector2(width - 36f, 32f), FontStyles.Bold | FontStyles.Italic);
            }
            ui.Chip(body, spec.ChampionPaint, new Vector2(0f, 0f), new Vector2(18f, 14f), 30f);
            string champion = LF("{0}  ·  {1}", Portfolio.DeathRace.L.Data(spec.Champion.Name), CarName(spec.ChampionCar));
            ui.WordsAt(body, champion, 21f, look.text, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 0f), new Vector2(58f, 10f),
                new Vector2(width - 76f, 38f), FontStyles.Bold);
            return card;
        }

        /// <summary>"3 mines · 2 rockets · 2 nitro · 1 oil slick": what a career of the league carries on its first day.</summary>
        private string KitLine(LeagueSpec spec)
        {
            var parts = new List<string>();
            AddCount(parts, spec.StartMines, "1 mine", "{0} mines");
            AddCount(parts, spec.StartRockets, "1 rocket", "{0} rockets");
            AddCount(parts, spec.StartNitro, "1 nitro", "{0} nitros");
            AddCount(parts, spec.StartOil, "1 oil slick", "{0} oil slicks");
            AddCount(parts, spec.StartEmp, "1 EMP blast", "{0} EMP blasts");
            AddCount(parts, spec.StartMortar, "1 mortar shell", "{0} mortar shells");
            return string.Join("  ·  ", parts);
        }

        private void AddCount(List<string> parts, int count, string one, string many)
        {
            if (count > 0)
            {
                parts.Add(count == 1 ? L(one) : LF(many, count));
            }
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
            OverlayLook look = kit.Look;
            for (int i = 0; i < rings.Count; i++)
            {
                rings[i].enabled = i == color;
            }
            LeagueSpec spec = Leagues.Of(League);
            previewChip.color = ui.DriverColor(color);
            string name = nameField != null ? nameField.text.Trim() : "";
            previewName.text = name.Length > 0 ? name : L("Rookie");
            previewName.color = Color.Lerp(ui.DriverColor(color), look.text, 0.25f);
            Sprite car = ui.CarPicture(spec.StartCar);
            previewCar.sprite = car;
            previewCar.enabled = car != null;
            previewLine.text = "<b>" + L(spec.Name) + "</b>\n" + LF("Rank {0} of {1}", RaceFormat.Rank(CareerRules.LadderSize), CareerRules.LadderSize) +
                "\n" + LF("{0}, with {1}", CarName(spec.StartCar), RaceFormat.Money(spec.StartMoney)) + "\n" +
                LF("Beat {0} to take the title.", Portfolio.DeathRace.L.Data(spec.Champion.Name));
            hint.text = L(spec.Blurb);
            foreach (LeagueCard card in cards)
            {
                bool picked = card.League.Kind == League;
                card.Frame.color = picked ? Accent : new Color(0f, 0f, 0f, 0f);
                card.Group.alpha = IsLeagueOpen(card.League) ? 1f : 0.55f;
            }
            Relabel(startButton, L("Start Career"), IsLeagueOpen(spec));
        }

        /// <summary>Picks <paramref name="spec"/> when it is open on this device.</summary>
        private bool PickLeague(LeagueSpec spec)
        {
            if (spec == null || !IsLeagueOpen(spec))
            {
                Game?.Sounds?.Deny();
                return false;
            }
            league = spec.Index;
            Game?.Sounds?.Click();
            RefreshPreview();
            return true;
        }

        private bool IsLeagueOpen(LeagueSpec spec)
        {
            return Game == null || Game.Progress.IsLeagueOpen(spec.Index);
        }

        /// <summary>The highest league open on this device: a champion goes on where they left off.</summary>
        private int HighestOpenLeague()
        {
            int highest = 0;
            foreach (LeagueSpec spec in Leagues.All)
            {
                if (IsLeagueOpen(spec))
                {
                    highest = spec.Index;
                }
            }
            return highest;
        }

        /// <summary>"Win the Wasteland Circuit to open it.": what opens a league that is locked.</summary>
        private string OpensWith(LeagueSpec spec)
        {
            LeagueSpec previous = spec.Previous;
            return previous != null ? LF("Win the {0} to open it.", L(previous.Name)) : "";
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
            LeagueSpec spec = Leagues.Of(League);
            if (!IsLeagueOpen(spec))
            {
                ShowMessage(OpensWith(spec));
                return;
            }
            Hide();
            Game?.StartCareer(name, color, 0, spec.Kind);
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
