using System;
using System.Collections.Generic;
using Gamebox.Racing;
using Gamebox.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// The pieces the windows of Death Race are built of, in the look of the theme: texts that shrink to fit, colour
    /// chips, pictures of cars that hide while the art is missing, stat bars, rows of stars, buttons and the minimap of a
    /// track, plus the names and colours of difficulties, cars, tracks and drivers. Made for one build of a window from
    /// its <see cref="OverlayKit"/>; the garage (a <see cref="ShopWindow"/>) and the other windows
    /// (<see cref="DeathRaceWindow"/>) share it.
    /// </summary>
    public sealed class RaceWindowParts
    {
        /// <summary>The colours of the difficulties, the same in both looks: green, amber, red and the Showdown's blood.</summary>
        private static readonly Color[] DifficultyColors =
        {
            new Color(0.36f, 0.78f, 0.42f, 1f),
            new Color(0.98f, 0.70f, 0.18f, 1f),
            new Color(0.94f, 0.30f, 0.20f, 1f),
            new Color(0.78f, 0.08f, 0.12f, 1f)
        };

        private static readonly string[] DifficultyNames = { "Easy", "Medium", "Hard", "Showdown" };

        /// <summary>The centre line of every track for the minimaps (the same for every window: made once).</summary>
        private static readonly Dictionary<int, RacePath> Paths = new Dictionary<int, RacePath>();

        private static bool warnedPicture;

        private readonly OverlayKit kit;
        private readonly Func<string, string> translate;

        public RaceWindowParts(OverlayKit kit, DeathRaceTheme theme, Func<string, string> translate)
        {
            this.kit = kit;
            this.translate = translate ?? (key => key);
            Theme = theme;
        }

        public DeathRaceTheme Theme { get; }

        public OverlayLook Look => kit.Look;

        /// <summary>The theme's accent (the HUD's), else the window's.</summary>
        public Color Accent => Theme != null ? Theme.hudAccent : kit.Look.accent;

        public Color Warn => Theme != null ? Theme.hudWarn : kit.Look.warn;

        /// <summary>A picture of the look by name (ART-WORLD's extra icons: "Star", "Lock", "Trophy", "Engine"...), else <paramref name="drawn"/>.</summary>
        public Sprite Icon(string key, Sprite drawn = null)
        {
            Sprite sprite = Theme != null ? Theme.SpriteOf(key) : null;
            return sprite != null ? sprite : drawn;
        }

        public Sprite Star => Icon("Star", RaceShapes.Star);

        public Sprite Lock => Icon("Lock", RaceShapes.Lock);

        /// <summary>A word of the window in the language shown.</summary>
        public string T(string key)
        {
            return translate(key);
        }

        // ------------------------------------------------------------------------------------------------
        // Words
        // ------------------------------------------------------------------------------------------------

        public static Color DifficultyColor(Difficulty difficulty)
        {
            return DifficultyColors[Mathf.Clamp((int)difficulty, 0, DifficultyColors.Length - 1)];
        }

        public string DifficultyName(Difficulty difficulty)
        {
            return T(DifficultyNames[Mathf.Clamp((int)difficulty, 0, DifficultyNames.Length - 1)]);
        }

        public static string CarName(CarModel model)
        {
            return Portfolio.DeathRace.L.Data(CarSpecs.Of(model).Name);
        }

        public static string TrackName(int track)
        {
            return Portfolio.DeathRace.L.Data(TrackLayouts.Of(track).Name);
        }

        /// <summary>A driver of the career by id (a rival or The Warden), in the language shown.</summary>
        public static string DriverName(CareerState state, string id)
        {
            RivalState rival = state?.Rival(id);
            if (rival != null)
            {
                return Portfolio.DeathRace.L.Data(rival.name);
            }
            RosterDriver driver = Roster.Find(id);
            return driver != null ? Portfolio.DeathRace.L.Data(driver.Name) : id;
        }

        /// <summary>The colour of a driver of the career (an index into the theme's driver colours).</summary>
        public Color DriverColor(int colorIndex)
        {
            return Theme != null && Theme.rivalColors != null && Theme.rivalColors.Length > 0 ? Theme.RivalColor(colorIndex) : FallbackColor(colorIndex);
        }

        /// <summary>The colour of a driver of the career by id; The Warden's is his own.</summary>
        public Color DriverColor(CareerState state, string id)
        {
            if (id == Roster.WardenId)
            {
                return Career.WardenPaint;
            }
            RivalState rival = state?.Rival(id);
            return rival != null ? DriverColor(rival.colorIndex) : kit.Look.muted;
        }

        /// <summary>Twenty distinct colours round the hue circle, for a theme that has none yet.</summary>
        public static Color FallbackColor(int index)
        {
            float hue = (Mathf.Abs(index) * 0.618034f) % 1f;
            return Color.HSVToRGB(hue, index % 3 == 0 ? 0.55f : 0.8f, index % 2 == 0 ? 0.95f : 0.8f);
        }

        // ------------------------------------------------------------------------------------------------
        // Layout
        // ------------------------------------------------------------------------------------------------

        /// <summary>Stretches <paramref name="rect"/> between two anchors of its parent, inset by the offsets.</summary>
        public static RectTransform Span(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return rect;
        }

        /// <summary>An empty area of the window to lay things out in.</summary>
        public static RectTransform Area(Transform parent, string areaName)
        {
            RectTransform rect = OverlayKit.Rect(parent, areaName);
            OverlayKit.Stretch(rect);
            return rect;
        }

        /// <summary>A panel in <paramref name="color"/> stretched over its parent (place it with <see cref="Span"/>).</summary>
        public RectTransform Box(Transform parent, string boxName, Color color)
        {
            RectTransform box = kit.Panel(parent, boxName, color, Vector2.zero, kit.Look.rowSprite);
            OverlayKit.Stretch(box);
            box.GetComponent<Image>().raycastTarget = false;
            return box;
        }

        /// <summary>
        /// A rounded shape in exactly <paramref name="color"/>, stretched over its parent: for a band of colour. A
        /// <see cref="Box"/> wears the look's row sprite, which a theme may draw untinted, so its colour can disappear.
        /// </summary>
        public RectTransform Fill(Transform parent, string fillName, Color color)
        {
            Image image = OverlayKit.NewImage(parent, fillName, color);
            image.sprite = kit.Rounded;
            image.type = Image.Type.Sliced;
            image.raycastTarget = false;
            OverlayKit.Stretch(image.rectTransform);
            return image.rectTransform;
        }

        /// <summary>Lays <paramref name="color"/> lightly over <paramref name="row"/> (the player's line, the pick of a list), under its contents.</summary>
        public Image Highlight(RectTransform row, Color color, float alpha = 0.26f)
        {
            RectTransform fill = Fill(row, "Highlight", new Color(color.r, color.g, color.b, alpha));
            fill.SetAsFirstSibling();
            return fill.GetComponent<Image>();
        }

        /// <summary>A text that shrinks to stay in its box; place it with <see cref="OverlayKit.Anchor"/> or <see cref="Span"/>.</summary>
        public TMP_Text Words(Transform parent, string content, float size, Color color, TextAlignmentOptions alignment,
            FontStyles style = FontStyles.Normal, bool heading = false)
        {
            TMP_Text text = kit.Text(parent, "Text", content, size, color, alignment, style, heading);
            OverlayKit.Fit(text, size);
            return text;
        }

        /// <summary>A text placed at one point of its parent (see <see cref="OverlayKit.Anchor"/>).</summary>
        public TMP_Text WordsAt(Transform parent, string content, float size, Color color, TextAlignmentOptions alignment, Vector2 anchor,
            Vector2 position, Vector2 extent, FontStyles style = FontStyles.Normal, bool heading = false)
        {
            TMP_Text text = Words(parent, content, size, color, alignment, style, heading);
            OverlayKit.Anchor(text.rectTransform, anchor, position, extent);
            return text;
        }

        /// <summary>A white shape tinted <paramref name="color"/>, placed at one point of its parent.</summary>
        public static Image Shape(Transform parent, Sprite sprite, Color color, Vector2 anchor, Vector2 position, Vector2 size)
        {
            Image image = OverlayKit.NewImage(parent, sprite != null ? sprite.name : "Shape", color);
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            OverlayKit.Anchor(image.rectTransform, anchor, position, size);
            return image;
        }

        /// <summary>A square of a driver's colour with rounded corners, the badge of a driver in every list.</summary>
        public Image Chip(Transform parent, Color color, Vector2 anchor, Vector2 position, float size)
        {
            Image chip = OverlayKit.NewImage(parent, "Chip", color);
            chip.sprite = kit.Rounded;
            chip.type = Image.Type.Sliced;
            chip.raycastTarget = false;
            OverlayKit.Anchor(chip.rectTransform, anchor, position, new Vector2(size, size));
            return chip;
        }

        /// <summary>
        /// A picture of the game's art (a car, an icon), kept in its proportions; hidden when the art is not there yet,
        /// which is said once in the log.
        /// </summary>
        public static Image Picture(Transform parent, Sprite sprite, Vector2 anchor, Vector2 position, Vector2 size)
        {
            Image image = OverlayKit.NewImage(parent, "Picture", Color.white);
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.enabled = sprite != null;
            OverlayKit.Anchor(image.rectTransform, anchor, position, size);
            if (sprite == null && !warnedPicture)
            {
                warnedPicture = true;
                Debug.LogWarning("Death Race: a picture of the interface is missing in the theme; it is left out until the art is built.");
            }
            return image;
        }

        /// <summary>The picture of a car in the look shown.</summary>
        public Sprite CarPicture(CarModel model)
        {
            return Theme != null ? Theme.CarPicture(model) : null;
        }

        /// <summary>A button in <paramref name="color"/> placed at one point of its parent.</summary>
        public Button ButtonAt(Transform parent, string label, Color color, Vector2 anchor, Vector2 position, Vector2 size, System.Action clicked,
            bool literal = false)
        {
            Button button = kit.MakeButton(parent, label, color, size, clicked, null, false, literal);
            OverlayKit.Anchor(button.GetComponent<RectTransform>(), anchor, position, size);
            return button;
        }

        /// <summary>Gives a button another label (already translated) and whether it can be pressed.</summary>
        public static void Relabel(Button button, string label, bool interactable = true)
        {
            if (button == null)
            {
                return;
            }
            TMP_Text text = OverlayKit.LabelOf(button);
            if (text != null)
            {
                text.text = label;
            }
            button.interactable = interactable;
        }

        /// <summary>A row of <paramref name="count"/> stars, <paramref name="lit"/> of them in the accent colour.</summary>
        public Image[] Stars(Transform parent, int count, Vector2 anchor, Vector2 position, float size)
        {
            var stars = new Image[count];
            for (int i = 0; i < count; i++)
            {
                stars[i] = Shape(parent, Star, kit.Look.muted, anchor, position + new Vector2(i * size * 1.05f, 0f), new Vector2(size, size));
            }
            return stars;
        }

        public void LightStars(Image[] stars, int lit)
        {
            Color dim = kit.Look.muted;
            dim.a = 0.35f;
            for (int i = 0; i < stars.Length; i++)
            {
                stars[i].color = i < lit ? Accent : dim;
            }
        }

        // ------------------------------------------------------------------------------------------------
        // Bars
        // ------------------------------------------------------------------------------------------------

        /// <summary>A bar of a car's stat: the value, and lighter in front of it what an upgrade would add.</summary>
        public sealed class StatBar
        {
            public RectTransform Rect;
            public Image Back;
            public Image Fill;
            public Image Preview;

            public void Set(float value, float preview = -1f)
            {
                value = Mathf.Clamp01(value);
                Fill.rectTransform.anchorMax = new Vector2(value, 1f);
                bool showPreview = preview > value + 0.001f;
                Preview.enabled = showPreview;
                Preview.rectTransform.anchorMin = new Vector2(value, 0f);
                Preview.rectTransform.anchorMax = new Vector2(showPreview ? Mathf.Clamp01(preview) : value, 1f);
            }
        }

        public StatBar Bar(Transform parent, Color fill, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var bar = new StatBar { Rect = OverlayKit.Rect(parent, "Bar") };
            OverlayKit.Anchor(bar.Rect, anchor, position, size);
            Color back = Color.Lerp(kit.Look.window, Color.black, 0.35f);
            bar.Back = Strip(bar.Rect, "Back", back);
            OverlayKit.Stretch(bar.Back.rectTransform);
            bar.Preview = Strip(bar.Rect, "Preview", Color.Lerp(fill, Color.white, 0.55f));
            bar.Fill = Strip(bar.Rect, "Fill", fill);
            foreach (Image strip in new[] { bar.Preview, bar.Fill })
            {
                RectTransform rect = strip.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = new Vector2(0f, 1f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
            bar.Set(0f);
            return bar;
        }

        private Image Strip(RectTransform parent, string stripName, Color color)
        {
            Image image = OverlayKit.NewImage(parent, stripName, color);
            image.sprite = kit.Rounded;
            image.type = Image.Type.Sliced;
            image.raycastTarget = false;
            return image;
        }

        // ------------------------------------------------------------------------------------------------
        // Maps
        // ------------------------------------------------------------------------------------------------

        /// <summary>The centre line of <paramref name="track"/>, or null for a layout without a shape.</summary>
        public static RacePath PathOf(int track)
        {
            if (!Paths.TryGetValue(track, out RacePath path))
            {
                TrackLayout layout = TrackLayouts.Of(track);
                path = layout.Points != null && layout.Points.Length >= 3 ? new RacePath(layout.Points, true, 4f) : null;
                Paths[track] = path;
            }
            return path;
        }

        /// <summary>The minimap of <paramref name="track"/> in the colours of its land in the look shown, with the start line marked.</summary>
        public Minimap TrackMap(RectTransform parent, int track, Vector2 anchor, Vector2 position, Vector2 size)
        {
            RectTransform holder = OverlayKit.Rect(parent, "Map");
            OverlayKit.Anchor(holder, anchor, position, size);
            Minimap map = Minimap.Create(holder, size);
            OverlayKit.Stretch(map.Rect);
            TrackLayout layout = TrackLayouts.Of(track);
            BiomeLook land = Theme != null ? Theme.Biome(layout.Biome) : null;
            Color road = land != null ? land.minimapRoad : new Color(0.85f, 0.8f, 0.7f, 1f);
            Color edge = land != null ? land.minimapEdge : new Color(0.1f, 0.08f, 0.06f, 1f);
            RacePath path = PathOf(track);
            if (path != null)
            {
                map.SetPath(path.Samples, layout.Width, road, edge);
                int line = map.AddMarker(Accent, Mathf.Max(8f, size.x * 0.06f), true);
                map.SetMarker(line, path.PointAt(layout.StartDistance));
            }
            return map;
        }
    }
}
