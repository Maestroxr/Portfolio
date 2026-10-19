using System.Collections.Generic;
using Gamebox;
using Gamebox.Online;
using Gamebox.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// The heads-up display of one person's car, laid over their part of the screen (<see cref="SplitScreen.AnchorTo"/>):
    /// the place ("2nd" big, "/4" small), the lap, the time of this lap and the best, the health bar, the rounds, mines,
    /// rockets and nitro the car has left, the cash picked up this race, the minimap with every car on it (the own one
    /// bigger, with an arrow), the big countdown, WRONG WAY, toasts for what happens to the car (a wreck it caused, a
    /// pickup, a lap, the fastest lap, a weapon with nothing left) and the banner at the finish.
    ///
    /// It is laid out once for a 1920 x 1080 screen in a frame that is scaled to the view (a split screen half or quarter
    /// gets smaller pieces, still in the corners), and drawn again on a new look or language (<see cref="Restyle"/>).
    /// Every frame it only reads the car and the race and writes what changed: no strings or garbage per frame.
    /// </summary>
    public class RaceHud : MonoBehaviour
    {
        private const float DesignWidth = 1920f;
        private const float DesignHeight = 1080f;
        private const int ToastSlots = 4;
        private const float ToastSeconds = 2.4f;
        private const float BannerSeconds = 4f;

        /// <summary>The whole numbers the counters show, written once.</summary>
        private static readonly string[] Numbers = new string[1000];

        private DeathRaceTheme theme;
        private DeathRaceSettings settings;
        private Rect viewport;
        private CanvasScaler scaler;
        private RectTransform frame;
        private Vector2 frameFor;

        private TMP_Text placeText, ofText, lapText, timeText, bestText, cashText;
        private TMP_Text countdownText, wrongWayText, bannerText;
        private Counter ammo, mines, rockets, nitro;
        private HudBar health;
        private RectTransform mapPanel;
        private Minimap map;
        private readonly Dictionary<Car, int> markers = new Dictionary<Car, int>();
        private readonly Toast[] toasts = new Toast[ToastSlots];

        private readonly char[] timeChars = new char[16];
        private int shownPlace = -1, shownLap = -1, shownCash = -1, shownHundredths = -1;
        private float shownBest = -1f;
        private string shownCountdown;
        private float countdownSince;
        private float bannerUntil;
        private bool finished;

        private sealed class Counter
        {
            public TMP_Text Text;
            public Image Icon;
            public int Shown = -1;
            public float Pulse;
        }

        private sealed class Toast
        {
            public TMP_Text Text;
            public float Until;
        }

        public Car Car { get; protected set; }
        public Race Race { get; protected set; }

        /// <summary>
        /// A HUD for <paramref name="car"/> under <paramref name="root"/> (a screen canvas of the director), fitted into
        /// <paramref name="viewport"/> (normalised, as a camera's rect).
        /// </summary>
        public static RaceHud Create(RectTransform root, Rect viewport, Car car, Race race, DeathRaceTheme theme, DeathRaceSettings settings)
        {
            var go = new GameObject($"HUD {car.name}", typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rect = (RectTransform)go.transform;
            rect.SetParent(root, false);
            SplitScreen.AnchorTo(rect, viewport);
            RaceHud hud = go.AddComponent<RaceHud>();
            hud.Car = car;
            hud.Race = race;
            hud.theme = theme;
            hud.settings = settings;
            hud.viewport = viewport;
            hud.Build();
            car.Happened += hud.OnHappened;
            return hud;
        }

        /// <summary>The look or the language changed: draw everything again.</summary>
        public virtual void Restyle(DeathRaceTheme theme)
        {
            this.theme = theme;
            Build();
        }

        public virtual void ApplySettings(DeathRaceSettings settings)
        {
            this.settings = settings;
            if (mapPanel != null)
            {
                mapPanel.gameObject.SetActive(settings == null || settings.showMinimap);
            }
        }

        public void Remove()
        {
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (Car != null)
            {
                Car.Happened -= OnHappened;
            }
        }

        // ------------------------------------------------------------------------------------------------
        // Building
        // ------------------------------------------------------------------------------------------------

        private Color Text => theme != null ? theme.hudText : new Color(0.96f, 0.93f, 0.86f, 1f);
        private Color Accent => theme != null ? theme.hudAccent : new Color(1f, 0.62f, 0.12f, 1f);
        private Color Warn => theme != null ? theme.hudWarn : new Color(0.95f, 0.25f, 0.18f, 1f);
        private static Color Shade => new Color(0.02f, 0.02f, 0.03f, 0.62f);

        /// <summary>The width of the panel of the car's health and weapons, and of one weapon's count in it.</summary>
        private const float CarPanelWidth = 640f;
        private const float CounterWidth = 149f;

        private void Build()
        {
            if (frame != null)
            {
                Destroy(frame.gameObject);
            }
            markers.Clear();
            shownPlace = shownLap = shownCash = shownHundredths = -1;
            shownBest = -1f;
            shownCountdown = null;

            frame = OverlayKit.Rect(transform, "Frame");
            frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(0.5f, 0.5f);
            frameFor = Vector2.zero;
            Fit();

            BuildStanding();
            BuildCar();
            BuildMap();
            BuildMiddle();
            ApplySettings(settings);
            Tick(true);
        }

        /// <summary>
        /// Scales the frame to the view: a full screen draws at the design size, a half or a quarter smaller (but not so
        /// small it cannot be read), and the frame stretches over the whole view at that scale.
        /// </summary>
        private void Fit()
        {
            Vector2 size = ViewSize();
            if (size.x < 1f || size.y < 1f || size == frameFor)
            {
                return;
            }
            frameFor = size;
            float scale = Mathf.Clamp(Mathf.Min(size.x / DesignWidth, size.y / DesignHeight) * 1.35f, 0.5f, 1f);
            frame.localScale = new Vector3(scale, scale, 1f);
            frame.sizeDelta = size / scale;
        }

        /// <summary>
        /// The size of the view in the canvas's units. Worked out from the screen and the canvas scaler's own rule rather
        /// than read from the laid out rect: in the first frames of a new canvas (and right after the window is resized)
        /// that rect still has the size of the unscaled screen, and a frame fitted to it would sit small in the middle.
        /// </summary>
        private Vector2 ViewSize()
        {
            if (scaler == null)
            {
                scaler = GetComponentInParent<CanvasScaler>();
            }
            Vector2 screen = new Vector2(Screen.width, Screen.height);
            if (scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize && screen.x > 0f && screen.y > 0f &&
                scaler.screenMatchMode != CanvasScaler.ScreenMatchMode.MatchWidthOrHeight)
            {
                Vector2 reference = scaler.referenceResolution;
                float wide = screen.x / reference.x, high = screen.y / reference.y;
                float factor = scaler.screenMatchMode == CanvasScaler.ScreenMatchMode.Expand ? Mathf.Min(wide, high) : Mathf.Max(wide, high);
                return new Vector2(screen.x * viewport.width, screen.y * viewport.height) / Mathf.Max(0.0001f, factor);
            }
            return ((RectTransform)transform).rect.size;
        }

        /// <summary>The top left: place, lap, the lap's time and the best.</summary>
        private void BuildStanding()
        {
            RectTransform panel = Panel("Standing", new Vector2(0f, 1f), new Vector2(28f, -28f), new Vector2(360f, 238f));
            placeText = Words(panel, "", 104f, Accent, TextAlignmentOptions.BottomLeft, true, new Vector2(0f, 1f), new Vector2(22f, -10f),
                new Vector2(220f, 110f));
            ofText = Words(panel, "", 46f, Text, TextAlignmentOptions.BottomLeft, true, new Vector2(0f, 1f), new Vector2(206f, -18f),
                new Vector2(130f, 70f));
            lapText = Words(panel, "", 40f, Text, TextAlignmentOptions.MidlineLeft, true, new Vector2(0f, 1f), new Vector2(24f, -122f),
                new Vector2(320f, 50f));
            timeText = Words(panel, "", 32f, Text, TextAlignmentOptions.MidlineLeft, false, new Vector2(0f, 1f), new Vector2(24f, -170f),
                new Vector2(200f, 40f));
            bestText = Words(panel, "", 24f, Muted, TextAlignmentOptions.MidlineRight, false, new Vector2(1f, 1f), new Vector2(-22f, -174f),
                new Vector2(160f, 34f));
            Words(panel, L.T("BEST"), 16f, Muted, TextAlignmentOptions.MidlineRight, true, new Vector2(1f, 1f), new Vector2(-22f, -150f),
                new Vector2(160f, 24f));
        }

        private Color Muted => Color.Lerp(Text, new Color(0.5f, 0.5f, 0.5f, 1f), 0.45f);

        /// <summary>The bottom left: health and what the car carries; the bottom right: the cash of the race.</summary>
        private void BuildCar()
        {
            RectTransform panel = Panel("Car", new Vector2(0f, 0f), new Vector2(28f, 28f), new Vector2(CarPanelWidth, 168f));
            Image heart = Icon(panel, theme != null ? theme.iconHealth : null, Text, new Vector2(0f, 1f), new Vector2(20f, -20f), 40f);
            heart.enabled = heart.sprite != null;
            float barLeft = heart.enabled ? 74f : 22f;
            Sprite fill = theme != null ? theme.barFill : null;
            float barWidth = CarPanelWidth - barLeft - 22f;
            health = HudBar.Create(panel, new Vector2(barWidth, 36f), new Color(0f, 0f, 0f, 0.6f), HealthColor(1f), fill);
            OverlayKit.Anchor(health.Rect, new Vector2(0f, 1f), new Vector2(barLeft, -22f), new Vector2(barWidth, 36f));
            health.LowThreshold = 0.3f;
            health.LowColor = Warn;
            health.SetValue(Car != null && Car.Health != null ? Car.Health.Health01 : 1f, true);

            ammo = MakeCounter(panel, theme != null ? theme.iconAmmo : null, L.T("AMMO"), 0);
            mines = MakeCounter(panel, theme != null ? theme.iconMine : null, L.T("MINES"), 1);
            rockets = MakeCounter(panel, theme != null ? theme.iconRocket : null, L.T("ROCKETS"), 2);
            nitro = MakeCounter(panel, theme != null ? theme.iconNitro : null, L.T("NITRO"), 3);

            RectTransform money = Panel("Cash", new Vector2(1f, 0f), new Vector2(-28f, 28f), new Vector2(300f, 84f));
            Image coin = Icon(money, theme != null ? theme.iconCash : null, Accent, new Vector2(0f, 0.5f), new Vector2(18f, 0f), 48f);
            coin.enabled = coin.sprite != null;
            cashText = Words(money, "", 44f, Accent, TextAlignmentOptions.MidlineRight, true, new Vector2(1f, 0.5f), new Vector2(-20f, 0f),
                new Vector2(coin.enabled ? 200f : 260f, 60f));
        }

        /// <summary>A count with its icon (or its name in small letters when the look has no icon).</summary>
        private Counter MakeCounter(RectTransform panel, Sprite icon, string name, int slot)
        {
            const float width = CounterWidth;
            float x = 22f + slot * width;
            var counter = new Counter
            {
                Icon = Icon(panel, icon, Text, new Vector2(0f, 0f), new Vector2(x, 22f), 52f)
            };
            if (icon == null)
            {
                counter.Icon.enabled = false;
                Words(panel, name, 16f, Muted, TextAlignmentOptions.BottomLeft, true, new Vector2(0f, 0f), new Vector2(x, 70f), new Vector2(width - 8f, 22f));
            }
            counter.Text = Words(panel, "", 40f, Text, TextAlignmentOptions.MidlineLeft, true, new Vector2(0f, 0f),
                new Vector2(icon != null ? x + 58f : x, 22f), new Vector2(icon != null ? width - 64f : width - 8f, 52f));
            return counter;
        }

        /// <summary>The top right: the track and a dot for every car, the own one an arrow drawn over the others.</summary>
        private void BuildMap()
        {
            // The map's panel is darker than the others: the track has to read over a busy world.
            // In the top right corner of the screen it stays clear of the menu's pause button.
            bool corner = viewport.xMax > 0.99f && viewport.yMax > 0.99f;
            mapPanel = Panel("Map", new Vector2(1f, 1f), new Vector2(-28f, corner ? -104f : -28f), new Vector2(320f, 320f), 0.86f);
            map = Minimap.Create(mapPanel, new Vector2(300f, 300f));
            OverlayKit.Stretch(map.Rect);
            map.Rect.offsetMin = new Vector2(10f, 10f);
            map.Rect.offsetMax = new Vector2(-10f, -10f);
            Track track = Race != null ? Race.Track : null;
            if (track == null || track.MinimapOutline.Count < 3)
            {
                return;
            }
            BiomeLook land = theme != null && track.Layout != null ? theme.Biome(track.Layout.Biome) : null;
            map.SetPath(track.MinimapOutline, track.HalfWidth * 2f, land != null ? land.minimapRoad : new Color(0.8f, 0.78f, 0.72f, 1f),
                land != null ? land.minimapEdge : new Color(0.08f, 0.07f, 0.06f, 1f));
            int line = map.AddMarker(Color.white, 14f, true);
            map.SetMarker(line, track.Path.PointAt(track.LineDistance));
            foreach (Car other in Race.Cars)
            {
                if (other != null && other != Car)
                {
                    markers[other] = map.AddMarker(other.Color, 15f);
                }
            }
            if (Car != null)
            {
                markers[Car] = map.AddMarker(Car.Color, 26f, Minimap.MarkerShape.Arrow);
                map.BringToFront(markers[Car]);
            }
        }

        /// <summary>The middle: the countdown, WRONG WAY, the toasts and the finish banner.</summary>
        private void BuildMiddle()
        {
            countdownText = Words(frame, "", 240f, Accent, TextAlignmentOptions.Midline, true, new Vector2(0.5f, 0.5f), new Vector2(0f, 60f),
                new Vector2(900f, 280f));
            wrongWayText = Words(frame, L.T("WRONG WAY"), 86f, Warn, TextAlignmentOptions.Midline, true, new Vector2(0.5f, 0.5f), new Vector2(0f, 150f),
                new Vector2(1000f, 110f));
            wrongWayText.gameObject.SetActive(false);
            bannerText = Words(frame, "", 110f, Accent, TextAlignmentOptions.Midline, true, new Vector2(0.5f, 0.5f), new Vector2(0f, 40f),
                new Vector2(1400f, 260f));
            bannerText.textWrappingMode = TextWrappingModes.Normal;
            bannerText.gameObject.SetActive(false);
            for (int i = 0; i < ToastSlots; i++)
            {
                TMP_Text text = Words(frame, "", 40f, Text, TextAlignmentOptions.Midline, true, new Vector2(0.5f, 1f), new Vector2(0f, -120f - i * 54f),
                    new Vector2(1100f, 52f));
                text.gameObject.SetActive(false);
                toasts[i] = new Toast { Text = text };
            }
        }

        private RectTransform Panel(string panelName, Vector2 anchor, Vector2 position, Vector2 size, float opacity = -1f)
        {
            Color shade = Shade;
            if (opacity >= 0f)
            {
                shade.a = opacity;
            }
            Image image = OverlayKit.NewImage(frame, panelName, shade);
            Sprite sprite = theme != null ? theme.hudPanel : null;
            image.sprite = sprite != null ? sprite : RaceShapes.Rounded;
            image.type = Image.Type.Sliced;
            image.raycastTarget = false;
            OverlayKit.Anchor(image.rectTransform, anchor, position, size);
            return image.rectTransform;
        }

        private TMP_Text Words(RectTransform parent, string content, float size, Color color, TextAlignmentOptions alignment, bool title,
            Vector2 anchor, Vector2 position, Vector2 extent)
        {
            RectTransform rect = OverlayKit.Rect(parent, "Text");
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (theme == null || !theme.Fonts.Apply(text, title ? TextRole.Title : TextRole.Body))
            {
                text.fontStyle = title ? FontStyles.Bold : FontStyles.Normal;
            }
            text.text = content;
            text.fontSize = size;
            text.enableAutoSizing = true;
            text.fontSizeMax = size;
            text.fontSizeMin = Mathf.Min(size, 14f);
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            OverlayKit.Anchor(rect, anchor, position, extent);
            return text;
        }

        private static Image Icon(RectTransform parent, Sprite sprite, Color color, Vector2 anchor, Vector2 position, float size)
        {
            Image image = OverlayKit.NewImage(parent, "Icon", color);
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            OverlayKit.Anchor(image.rectTransform, anchor, position, new Vector2(size, size));
            return image;
        }

        // ------------------------------------------------------------------------------------------------
        // Every frame
        // ------------------------------------------------------------------------------------------------

        private void LateUpdate()
        {
            Fit();
            Tick(false);
        }

        /// <summary>Writes what changed since the last frame (everything when <paramref name="all"/>).</summary>
        private void Tick(bool all)
        {
            if (Car == null || frame == null)
            {
                return;
            }
            Racer racer = Car.Racer;
            int cars = Race != null ? Race.Cars.Count : 1;
            int laps = Race != null ? Race.Laps : 1;

            int place = racer != null ? Mathf.Clamp(racer.Place, 1, 99) : 1;
            if (place != shownPlace || all)
            {
                shownPlace = place;
                // "2nd" in English; elsewhere the ordinal is a phrase ("place 2") too long for the big figure: the number.
                placeText.text = GameLanguages.IsEnglish ? Standings.Ordinal(place) : Number(place);
                ofText.text = "/" + Number(cars);
                // The "/4" follows the place, however wide it came out.
                placeText.ForceMeshUpdate();
                RectTransform of = ofText.rectTransform;
                of.anchoredPosition = new Vector2(22f + Mathf.Clamp(placeText.textBounds.size.x, 0f, 230f) + 10f, of.anchoredPosition.y);
            }
            int lap = racer != null ? Mathf.Clamp(racer.Lap, 1, laps) : 1;
            if (lap != shownLap || all)
            {
                shownLap = lap;
                lapText.text = lap == laps && laps > 1 ? L.T("FINAL LAP") : L.F("LAP {0}/{1}", lap, laps);
            }
            float current = racer != null && racer.Tracker != null && Race != null && Race.Time > 0f && !racer.Finished ? racer.Tracker.CurrentLapTime(Race.Time) : 0f;
            int hundredths = Mathf.RoundToInt(current * 100f);
            if (hundredths != shownHundredths || all)
            {
                shownHundredths = hundredths;
                int length = RaceFormat.TimeChars(current, timeChars);
                timeText.SetCharArray(timeChars, 0, length);
            }
            float best = racer != null && racer.Tracker != null ? racer.Tracker.BestLap : 0f;
            if (!Mathf.Approximately(best, shownBest) || all)
            {
                shownBest = best;
                bestText.text = RaceFormat.Time(best);
            }
            int cash = racer != null ? racer.CashPicked : 0;
            if (cash != shownCash || all)
            {
                shownCash = cash;
                cashText.text = RaceFormat.Money(cash);
            }

            CarHealth hurt = Car.Health;
            float health01 = hurt != null ? hurt.Health01 : 1f;
            health.Value = health01;
            health.SetColor(HealthColor(health01));

            CarWeapons weapons = Car.Weapons;
            if (weapons != null)
            {
                Count(ammo, weapons.Ammo, all);
                Count(mines, weapons.Mines, all);
                Count(rockets, weapons.Rockets, all);
                Count(nitro, weapons.NitroCharges, all);
                nitro.Text.color = weapons.NitroActive ? Accent : nitro.Pulse > 0f ? Accent : Text;
                ammo.Text.color = weapons.Ammo <= 0 ? Warn : weapons.AmmoCapacity > 0 && weapons.Ammo < weapons.AmmoCapacity / 5 ? Accent : Text;
            }
            Pulse(ammo);
            Pulse(mines);
            Pulse(rockets);
            Pulse(nitro);

            MoveMarkers();
            ShowCountdown();
            bool wrongWay = racer != null && racer.WrongWay && Race != null && Race.IsRunning && !racer.Finished && !Car.IsWrecked;
            wrongWayText.gameObject.SetActive(wrongWay && Mathf.Repeat(Time.unscaledTime, 0.8f) < 0.55f);
            FadeToasts();
            if (bannerText.gameObject.activeSelf && !finished && Time.unscaledTime > bannerUntil)
            {
                bannerText.gameObject.SetActive(false);
            }
        }

        private void Count(Counter counter, int value, bool all)
        {
            if (value == counter.Shown && !all)
            {
                return;
            }
            if (value > counter.Shown && counter.Shown >= 0)
            {
                counter.Pulse = 0.5f;
            }
            counter.Shown = value;
            counter.Text.text = Number(value);
        }

        private static void Pulse(Counter counter)
        {
            if (counter.Pulse <= 0f)
            {
                return;
            }
            counter.Pulse = Mathf.Max(0f, counter.Pulse - Time.unscaledDeltaTime);
            float scale = 1f + counter.Pulse * 0.5f;
            counter.Text.rectTransform.localScale = new Vector3(scale, scale, 1f);
        }

        private void MoveMarkers()
        {
            if (map == null || mapPanel == null || !mapPanel.gameObject.activeSelf)
            {
                return;
            }
            foreach (KeyValuePair<Car, int> marker in markers)
            {
                Car car = marker.Key;
                if (car == null)
                {
                    continue;
                }
                map.SetMarker(marker.Value, car.Position2D, true);
                if (car == Car)
                {
                    map.SetHeading(marker.Value, car.Forward2D);
                }
                else if (car.IsWrecked)
                {
                    map.SetMarkerColor(marker.Value, new Color(0.25f, 0.25f, 0.25f, 0.8f));
                }
            }
        }

        private void ShowCountdown()
        {
            string label = Race != null ? Race.CountdownLabel : null;
            if (!ReferenceEquals(label, shownCountdown))
            {
                shownCountdown = label;
                countdownSince = Time.unscaledTime;
                countdownText.text = label ?? "";
                countdownText.gameObject.SetActive(!string.IsNullOrEmpty(label));
            }
            if (countdownText.gameObject.activeSelf)
            {
                // Each number lands big and settles.
                float t = Mathf.Clamp01((Time.unscaledTime - countdownSince) / 0.35f);
                float scale = Mathf.Lerp(1.6f, 1f, 1f - (1f - t) * (1f - t));
                countdownText.rectTransform.localScale = new Vector3(scale, scale, 1f);
                Color color = countdownText.color;
                color.a = Mathf.Lerp(0.2f, 1f, t);
                countdownText.color = color;
            }
        }

        private Color HealthColor(float health01)
        {
            Color good = new Color(0.36f, 0.82f, 0.38f, 1f);
            return health01 > 0.6f ? good : health01 > 0.3f ? Color.Lerp(Accent, good, (health01 - 0.3f) / 0.3f) : Warn;
        }

        // ------------------------------------------------------------------------------------------------
        // What happened
        // ------------------------------------------------------------------------------------------------

        private void OnHappened(CarEvent happened)
        {
            switch (happened.Kind)
            {
                case CarEventKind.Kill:
                    string victim = happened.Other != null && happened.Other.Entrant != null ? happened.Other.Entrant.Name : "";
                    int bounty = Race != null && Race.Setup != null && Race.Setup.Mode == RaceMode.Career ? CareerRules.Bounty(Race.Setup.Difficulty) : 0;
                    ShowToast(bounty > 0 ? L.F("WRECKED {0}! +{1}", victim, RaceFormat.Money(bounty)) : L.F("WRECKED {0}!", victim), Accent);
                    break;
                case CarEventKind.Wrecked:
                    finished = true;
                    Banner(L.T("WRECKED!"), Warn);
                    break;
                case CarEventKind.Pickup:
                    ShowToast(PickupWords(happened), Pickup.ColorOf(happened.Pickup));
                    break;
                case CarEventKind.Lap:
                    int next = happened.Amount + 1;
                    int laps = Race != null ? Race.Laps : next;
                    ShowToast(next >= laps ? L.T("FINAL LAP!") : L.F("LAP {0}/{1}", next, laps), Text);
                    break;
                case CarEventKind.BestLap:
                    ShowToast(L.F("FASTEST LAP {0}", RaceFormat.Time(happened.Value)), Accent);
                    break;
                case CarEventKind.Empty:
                    ShowToast(L.T("EMPTY!"), Warn);
                    break;
                case CarEventKind.Finished:
                    finished = true;
                    int place = Mathf.Max(1, happened.Amount);
                    Banner(place == 1 ? L.T("YOU WIN!") : L.F("FINISHED {0}", Standings.Ordinal(place)), place == 1 ? Accent : Text);
                    break;
            }
        }

        private static string PickupWords(CarEvent happened)
        {
            switch (happened.Pickup)
            {
                case PickupKind.Cash: return "+" + RaceFormat.Money(happened.Amount);
                case PickupKind.Repair: return L.F("REPAIR +{0}", happened.Amount);
                case PickupKind.Ammo: return L.F("AMMO +{0}", happened.Amount);
                default: return L.T("NITRO +1");
            }
        }

        /// <summary>A line at the top of the middle; older ones move down, the oldest gives way.</summary>
        private void ShowToast(string words, Color color)
        {
            for (int i = ToastSlots - 1; i > 0; i--)
            {
                toasts[i].Text.text = toasts[i - 1].Text.text;
                toasts[i].Text.color = toasts[i - 1].Text.color;
                toasts[i].Until = toasts[i - 1].Until;
                toasts[i].Text.gameObject.SetActive(toasts[i - 1].Text.gameObject.activeSelf);
            }
            toasts[0].Text.text = words;
            toasts[0].Text.color = color;
            toasts[0].Until = Time.unscaledTime + ToastSeconds;
            toasts[0].Text.gameObject.SetActive(true);
        }

        private void FadeToasts()
        {
            float now = Time.unscaledTime;
            foreach (Toast toast in toasts)
            {
                if (!toast.Text.gameObject.activeSelf)
                {
                    continue;
                }
                float left = toast.Until - now;
                if (left <= 0f)
                {
                    toast.Text.gameObject.SetActive(false);
                    continue;
                }
                Color color = toast.Text.color;
                color.a = Mathf.Clamp01(left / 0.5f);
                toast.Text.color = color;
                float age = ToastSeconds - left;
                float pop = age < 0.15f ? Mathf.Lerp(1.3f, 1f, age / 0.15f) : 1f;
                toast.Text.rectTransform.localScale = new Vector3(pop, pop, 1f);
            }
        }

        private void Banner(string words, Color color)
        {
            bannerText.text = words;
            bannerText.color = color;
            bannerText.gameObject.SetActive(true);
            bannerUntil = Time.unscaledTime + BannerSeconds;
        }

        // ------------------------------------------------------------------------------------------------

        private static string Number(int value)
        {
            if (value < 0 || value >= Numbers.Length)
            {
                return RaceFormat.Number(value);
            }
            return Numbers[value] ??= value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
