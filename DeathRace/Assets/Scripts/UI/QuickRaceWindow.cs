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
    /// A race against the computer, set up in one window: the tracks on the left (stars and best lap of each; the ones
    /// not open yet locked, with what opens them), the map of the track picked in the middle, and on the right the car
    /// (any of the six, with its picture and stats), how hard the computer races and how many laps. Start races it.
    /// The picks are remembered for the session (and so for Race Again).
    /// </summary>
    public sealed class QuickRaceWindow : DeathRaceWindow
    {
        private const float RowHeight = 78f;

        private static readonly string[] Difficulties = { "Easy", "Medium", "Hard" };

        private sealed class TrackRow
        {
            public int Track;
            public RectTransform Rect;
            public Image Back;
            public Image Picked;
            public CanvasGroup Group;
            public TMP_Text Name;
            public TMP_Text Detail;
            public Image[] Stars;
            public Image Lock;
        }

        private readonly List<TrackRow> rows = new List<TrackRow>();
        private RectTransform mapHolder;
        private TMP_Text trackTitle, trackDetail, trackBest;
        private Image[] trackStars;
        private OverlayKit.Stepper carStepper, difficultyStepper, lapsStepper;
        private Image carPicture;
        private TMP_Text carBlurb;
        private RaceWindowParts.StatBar speed, acceleration, handling, armour;
        private Button start;
        private bool picked;
        private int track;
        private CarModel car;
        private Difficulty difficulty;
        private int laps;

        protected override string CanvasName => "DeathRaceQuickRace";

        protected override Vector2 PanelSize => new Vector2(1720f, 980f);

        protected override float MessageWidth => 900f;

        public static QuickRaceWindow For(BaseGameManager manager)
        {
            return For<QuickRaceWindow>(manager);
        }

        public void Show()
        {
            if (!picked && Game != null)
            {
                DeathRaceSettings options = Game.Options;
                track = Game.LevelIndex;
                car = options != null ? options.QuickCar : CarModel.Viper;
                difficulty = options != null ? options.QuickDifficulty : Difficulty.Medium;
                laps = options != null ? options.quickLaps : 0;
                picked = true;
            }
            if (!IsTrackOpen(track))
            {
                track = 0;
            }
            Present();
        }

        protected override void Compose()
        {
            rows.Clear();
            OverlayLook look = kit.Look;
            title.text = L("Quick Race");
            hint.text = L("Pick a track, a car and how hard the computer races. No money, no ladder: just the race.");

            // The tracks.
            RectTransform list = Area(panel, "Tracks");
            Span(list, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(40f, 130f), new Vector2(640f, -146f));
            var layout = list.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            for (int i = 0; i < TrackLayouts.Count; i++)
            {
                rows.Add(BuildRow(list, i, look));
            }

            // The track picked.
            RectTransform middle = ui.Box(panel, "Track", look.row);
            Span(middle, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(664f, 130f), new Vector2(1124f, -146f));
            trackTitle = ui.WordsAt(middle, "", 34f, look.text, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(24f, -16f),
                new Vector2(412f, 46f), FontStyles.Bold, true);
            trackDetail = ui.WordsAt(middle, "", 22f, look.muted, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), new Vector2(24f, -66f),
                new Vector2(412f, 60f));
            trackDetail.textWrappingMode = TextWrappingModes.Normal;
            mapHolder = OverlayKit.Rect(middle, "Map Holder");
            OverlayKit.Anchor(mapHolder, new Vector2(0.5f, 1f), new Vector2(0f, -136f), new Vector2(412f, 412f));
            trackStars = ui.Stars(middle, 3, new Vector2(0f, 0f), new Vector2(24f, 22f), 40f);
            trackBest = ui.WordsAt(middle, "", 24f, look.text, TextAlignmentOptions.MidlineRight, new Vector2(1f, 0f), new Vector2(-24f, 22f),
                new Vector2(260f, 40f));

            // The car, the difficulty and the laps.
            RectTransform right = Area(panel, "Choices");
            Span(right, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(1148f, 130f), new Vector2(-40f, -146f));
            var carNames = new string[CarSpecs.Count];
            for (int i = 0; i < carNames.Length; i++)
            {
                carNames[i] = CarName((CarModel)i);
            }
            RectTransform carRow = ui.Box(right, "Car", look.row);
            Span(carRow, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -470f), Vector2.zero);
            carStepper = kit.StepperIn(carRow, carNames, (int)car, () =>
            {
                car = (CarModel)carStepper.Index;
                RefreshCar();
            }, 56f, 492f, new Vector2(-20f, 196f));
            carPicture = Picture(carRow, null, new Vector2(0.5f, 1f), new Vector2(0f, -86f), new Vector2(360f, 170f));
            carBlurb = ui.WordsAt(carRow, "", 20f, look.muted, TextAlignmentOptions.Top, new Vector2(0.5f, 1f), new Vector2(0f, -262f),
                new Vector2(490f, 52f), FontStyles.Italic);
            carBlurb.textWrappingMode = TextWrappingModes.Normal;
            speed = StatRow(carRow, "Speed", -326f, look);
            acceleration = StatRow(carRow, "Acceleration", -360f, look);
            handling = StatRow(carRow, "Handling", -394f, look);
            armour = StatRow(carRow, "Armor", -428f, look);

            var difficultyNames = new string[Difficulties.Length];
            for (int i = 0; i < difficultyNames.Length; i++)
            {
                difficultyNames[i] = L(Difficulties[i]);
            }
            difficultyStepper = Choice(right, "Computer", difficultyNames, (int)difficulty, -490f, () =>
            {
                difficulty = (Difficulty)difficultyStepper.Index;
                difficultyStepper.Value.color = DifficultyColor(difficulty);
            });
            lapsStepper = Choice(right, "Laps", LapLabels(), laps, -568f, () => laps = lapsStepper.Index);

            start = ui.ButtonAt(panel, "Start", Accent, new Vector2(1f, 0f), new Vector2(-40f, 36f), new Vector2(300f, 76f), StartRace);
            ui.ButtonAt(panel, "Back", look.row, new Vector2(1f, 0f), new Vector2(-360f, 36f), new Vector2(220f, 76f), Hide);
        }

        private TrackRow BuildRow(RectTransform list, int index, OverlayLook look)
        {
            var row = new TrackRow { Track = index };
            row.Rect = kit.Row(list, RowHeight);
            row.Back = row.Rect.GetComponent<Image>();
            row.Back.raycastTarget = true;
            row.Picked = ui.Highlight(row.Rect, Accent, 0.32f);
            row.Group = row.Rect.gameObject.AddComponent<CanvasGroup>();
            Button pick = row.Rect.gameObject.AddComponent<Button>();
            pick.targetGraphic = row.Back;
            pick.onClick.AddListener(() => Pick(index));
            row.Name = ui.WordsAt(row.Rect, TrackName(index), 28f, look.text, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f),
                new Vector2(20f, -6f), new Vector2(380f, 38f), FontStyles.Bold);
            row.Detail = ui.WordsAt(row.Rect, "", 20f, look.muted, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 0f), new Vector2(20f, 6f),
                new Vector2(500f, 30f));
            row.Stars = ui.Stars(row.Rect, 3, new Vector2(1f, 0.5f), new Vector2(-136f, 0f), 34f);
            row.Lock = Shape(row.Rect, ui.Lock, look.muted, new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(40f, 40f));
            return row;
        }

        private RaceWindowParts.StatBar StatRow(RectTransform parent, string label, float y, OverlayLook look)
        {
            ui.WordsAt(parent, L(label), 20f, look.muted, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(24f, y),
                new Vector2(170f, 28f));
            return ui.Bar(parent, Accent, new Vector2(0f, 1f), new Vector2(200f, y - 6f), new Vector2(300f, 16f));
        }

        private OverlayKit.Stepper Choice(RectTransform parent, string label, string[] labels, int index, float y, System.Action changed)
        {
            RectTransform row = ui.Box(parent, label, kit.Look.row);
            Span(row, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, y - 64f), new Vector2(0f, y));
            ui.WordsAt(row, L(label), 24f, kit.Look.muted, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 0.5f), new Vector2(24f, 0f),
                new Vector2(160f, 40f));
            return kit.StepperIn(row, labels, index, changed, 48f, 330f, new Vector2(-16f, 0f));
        }

        private string[] LapLabels()
        {
            var labels = new string[DeathRaceSettings.MaxLaps + 1];
            labels[0] = L("Track's own");
            for (int i = 1; i < labels.Length; i++)
            {
                labels[i] = RaceFormat.Number(i);
            }
            return labels;
        }

        protected override void Refresh()
        {
            DeathRaceProgress progress = Game != null ? Game.Progress : null;
            for (int i = 0; i < rows.Count; i++)
            {
                TrackRow row = rows[i];
                bool open = IsTrackOpen(row.Track);
                TrackLayout layout = TrackLayouts.Of(row.Track);
                row.Group.alpha = open ? 1f : 0.5f;
                row.Lock.enabled = !open;
                foreach (Image star in row.Stars)
                {
                    star.enabled = open;
                }
                ui.LightStars(row.Stars, progress != null ? progress.Stars(row.Track) : 0);
                float best = progress != null ? progress.BestLap(row.Track) : 0f;
                row.Detail.text = open
                    ? LF("{0}  ·  {1} laps  ·  best {2}", L(BiomeName(layout.Biome)), layout.Laps, RaceFormat.Time(best))
                    : LockReason(row.Track);
                row.Picked.enabled = row.Track == track;
            }
            RefreshTrack(progress);
            difficultyStepper.Select((int)difficulty, false);
            difficultyStepper.Value.color = DifficultyColor(difficulty);
            lapsStepper.Select(laps, false);
            carStepper.Select((int)car, false);
            RefreshCar();
        }

        private void RefreshTrack(DeathRaceProgress progress)
        {
            TrackLayout layout = TrackLayouts.Of(track);
            trackTitle.text = TrackName(track);
            trackDetail.text = string.IsNullOrEmpty(layout.Description) ? L(BiomeName(layout.Biome)) : Portfolio.DeathRace.L.Data(layout.Description);
            OverlayKit.Clear(mapHolder);
            ui.TrackMap(mapHolder, track, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(412f, 412f));
            ui.LightStars(trackStars, progress != null ? progress.Stars(track) : 0);
            float best = progress != null ? progress.BestLap(track) : 0f;
            trackBest.text = LF("Best lap {0}", RaceFormat.Time(best));
        }

        private void RefreshCar()
        {
            CarSpec spec = CarSpecs.Of(car);
            carPicture.sprite = ui.CarPicture(car);
            carPicture.enabled = carPicture.sprite != null;
            carBlurb.text = Portfolio.DeathRace.L.Data(spec.Blurb);
            // The car a quick race gives: every part one level up.
            CarStats stats = CarLoadout.Upgraded(car, 1).Stats();
            speed.Set(CarBars.Speed(stats));
            acceleration.Set(CarBars.Acceleration(stats));
            handling.Set(CarBars.Handling(stats));
            armour.Set(CarBars.Armour(stats));
        }

        private void Pick(int index)
        {
            if (!IsTrackOpen(index))
            {
                ShowMessage(LockReason(index));
                return;
            }
            track = index;
            Refresh();
        }

        private bool IsTrackOpen(int index)
        {
            DeathRaceCampaign campaign = Game != null ? Game.Tracks : null;
            return campaign == null || campaign.Count == 0 || campaign.IsUnlocked(index, Game.Progress);
        }

        /// <summary>What opens a track: the Showdown's arena the title, the others a star on the track before or the career.</summary>
        private string LockReason(int index)
        {
            if (TrackLayouts.Of(index).Showdown)
            {
                return L("Beat The Warden in a career to race here.");
            }
            int rank = CareerRules.RankOpening(index);
            string before = TrackName(Mathf.Max(0, index - 1));
            return rank > 0 ? LF("A podium at {0} or {1} in a career opens it.", before, RaceFormat.Rank(rank))
                : LF("A podium at {0} opens it.", before);
        }

        public static string BiomeName(Biome biome)
        {
            switch (biome)
            {
                case Biome.Desert: return "Desert";
                case Biome.Industrial: return "Industrial";
                case Biome.Snow: return "Snow";
                default: return "City";
            }
        }

        private void StartRace()
        {
            if (!IsTrackOpen(track) || Game == null)
            {
                ShowMessage(LockReason(track));
                return;
            }
            Hide();
            Game.LoadLevel(track);
            Game.StartQuickRace(track, car, difficulty, laps);
        }
    }
}
