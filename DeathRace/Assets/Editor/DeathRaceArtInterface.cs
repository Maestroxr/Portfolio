using Gamebox.Editor;
using UnityEngine;
using N = Portfolio.DeathRace.EditorTools.DeathRaceArtNoise;

namespace Portfolio.DeathRace.EditorTools
{
    /// <summary>
    /// The sprites of the interface, drawn into a <see cref="Raster"/> in the style of a look: Scorched is gritty
    /// gunmetal with bevels, rivets, rust and hazard stripes in orange and yellow; Nightfall is dark glass outlined in cyan
    /// neon with magenta accents. They are drawn at twice the size they show at (imported at 200 pixels per unit, so a
    /// 36 pixel border is 18 units of the 1920 x 1080 layout) and nine sliced: the grit and the rivets live in the corners,
    /// which never stretch, and the middles are smooth, so any size looks right.
    /// </summary>
    internal static class DeathRaceArtInterface
    {
        /// <summary>Pixels of a sprite per unit of the layout (sprites are imported at 100 * Detail pixels per unit).</summary>
        public const float Detail = 2f;

        public static Color Hex(uint rgb, float alpha = 1f)
        {
            return DeathRaceThemeSpec.Hex(rgb, alpha);
        }

        public static Color WithAlpha(Color c, float alpha)
        {
            return new Color(c.r, c.g, c.b, alpha);
        }

        public static Color Shade(Color c, float factor)
        {
            return new Color(c.r * factor, c.g * factor, c.b * factor, c.a);
        }

        public static Color Lighten(Color c, float amount)
        {
            return new Color(Mathf.Lerp(c.r, 1f, amount), Mathf.Lerp(c.g, 1f, amount), Mathf.Lerp(c.b, 1f, amount), c.a);
        }

        private static Rect All(Raster r)
        {
            return new Rect(0f, 0f, r.Width, r.Height);
        }

        // ------------------------------------------------------------------------------------------------
        // Shared pieces of the Scorched style
        // ------------------------------------------------------------------------------------------------

        /// <summary>Gunmetal: a vertical gradient with faint brushed streaks, darker where the grit is.</summary>
        private static Color Metal(Vector2 p, Color top, Color bottom, float height, int seed)
        {
            float t = Mathf.Clamp01(p.y / height);
            Color c = Color.Lerp(bottom, top, t);
            float streak = N.Plain(p.x / 110f, p.y / 1.6f, 1, seed) - 0.5f;
            return Shade(c, 1f + streak * 0.08f);
        }

        /// <summary>Rust and scratches over metal near the corners of a sprite (inside its slice borders).</summary>
        private static Color Grit(Color c, Vector2 p, Raster r, float border, int seed, float amount)
        {
            float cx = Mathf.Min(p.x, r.Width - p.x);
            float cy = Mathf.Min(p.y, r.Height - p.y);
            float corner = Mathf.Clamp01(1f - Mathf.Max(cx, cy) / border);
            float rust = N.Smooth(0.6f, 0.75f, N.Plain(p.x / 22f, p.y / 22f, 3, seed)) * corner * amount;
            c = Color.Lerp(c, Hex(0x7A3A1A, c.a), rust * 0.8f);
            float pits = N.Hash(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y), seed + 1) > 0.985f ? 0.25f : 0f;
            c = Shade(c, 1f - pits * corner * amount);
            float scratch = N.Smooth(0.94f, 0.985f, N.PlainRidge(p.x / 40f, p.y / 40f, 2, seed + 2)) * corner * amount;
            return Color.Lerp(c, Lighten(c, 0.45f), scratch * 0.6f);
        }

        private static void Rivet(Raster r, Vector2 at, float radius)
        {
            r.Shadow(p => Sd.Circle(p, at, radius), new Color(0f, 0f, 0f, 0.6f), radius * 0.7f, new Vector2(radius * 0.25f, -radius * 0.35f), Sd.Around(at, radius * 2.5f));
            r.Fill(p => Sd.Circle(p, at, radius), p =>
            {
                float t = Mathf.Clamp01((p.y - at.y) / radius * 0.5f + 0.5f);
                return Color.Lerp(Hex(0x3A3632), Hex(0xB9B0A4), t);
            }, Sd.Around(at, radius + 2f));
            r.Fill(p => Sd.Circle(p, at + new Vector2(-radius * 0.3f, radius * 0.3f), radius * 0.3f), new Color(1f, 1f, 1f, 0.5f), Sd.Around(at, radius + 2f));
        }

        /// <summary>Yellow and black diagonal stripes inside <paramref name="shape"/>.</summary>
        private static void Hazard(Raster r, Sdf shape, Rect bounds, float period, Color yellow, float alpha = 1f)
        {
            r.Fill(shape, p =>
            {
                bool dark = Mathf.Repeat((p.x + p.y) / period, 1f) < 0.5f;
                Color c = dark ? Hex(0x1A1714) : yellow;
                float wear = N.Smooth(0.62f, 0.75f, N.Plain(p.x / 16f, p.y / 16f, 3, 77));
                c = Color.Lerp(c, Hex(0x5A4A3A), wear * 0.5f);
                c.a = alpha;
                return c;
            }, bounds);
        }

        /// <summary>
        /// A shadow (or glow) inside <paramref name="shape"/> along its edge, <paramref name="depth"/> deep, shifted by
        /// <paramref name="offset"/> (down for a recess lit from above); nothing outside the shape.
        /// </summary>
        private static void Inner(Raster r, Sdf shape, Rect bounds, Color color, float depth, Vector2 offset)
        {
            r.Fill(shape, p =>
            {
                float d = -shape(p - offset);
                float a = Mathf.Clamp01(1f - d / depth);
                return WithAlpha(color, color.a * a * a);
            }, bounds);
        }

        /// <summary>The bevel of a plate: a light rim at the top, a dark one at the bottom.</summary>
        private static void Bevel(Raster r, Sdf shape, Rect bounds, float width, float strength)
        {
            r.Fill(p => Sd.Intersect(shape(p), -(shape(p) + width)), p =>
            {
                // Above the middle a light rim, below it a dark one.
                float t = Mathf.Clamp01((p.y - bounds.center.y) / (bounds.height * 0.5f));
                return p.y > bounds.center.y ? new Color(1f, 1f, 1f, 0.22f * strength * t) : new Color(0f, 0f, 0f, 0.35f * strength * -((p.y - bounds.center.y) / (bounds.height * 0.5f)));
            }, bounds, 1f);
        }

        // ------------------------------------------------------------------------------------------------
        // Shared pieces of the Nightfall style
        // ------------------------------------------------------------------------------------------------

        /// <summary>A neon line along the edge of <paramref name="shape"/>: a soft glow, the coloured tube and its hot core.</summary>
        private static void Neon(Raster r, Sdf shape, Rect bounds, Color color, float width, float glow, float glowAlpha = 0.45f)
        {
            if (glow > 0f)
            {
                r.Shadow(p => Sd.Outline(shape(p), width), WithAlpha(color, glowAlpha), glow, Vector2.zero, bounds);
            }
            r.Fill(p => Sd.Outline(shape(p), width), color, bounds);
            r.Fill(p => Sd.Outline(shape(p), width * 0.35f), WithAlpha(Lighten(color, 0.65f), 0.9f), bounds);
        }

        private static Color Glass(Vector2 p, Color top, Color bottom, float height, float alpha)
        {
            float t = Mathf.Clamp01(p.y / height);
            Color c = Color.Lerp(bottom, top, t * t);
            c.a = alpha;
            return c;
        }

        /// <summary>Short L shaped brackets in the four corners of a rounded box.</summary>
        private static void Brackets(Raster r, Vector2 center, Vector2 half, float length, float width, Color color)
        {
            for (int sx = -1; sx <= 1; sx += 2)
            {
                for (int sy = -1; sy <= 1; sy += 2)
                {
                    Vector2 corner = center + new Vector2(sx * half.x, sy * half.y);
                    Vector2 alongX = corner - new Vector2(sx * length, 0f);
                    Vector2 alongY = corner - new Vector2(0f, sy * length);
                    Vector2 inner = corner - new Vector2(sx * width * 0.5f, sy * width * 0.5f);
                    r.Fill(p => Sd.Union(Sd.Segment(p, inner, alongX - new Vector2(0f, sy * width * 0.5f), width * 0.5f),
                        Sd.Segment(p, inner, alongY - new Vector2(sx * width * 0.5f, 0f), width * 0.5f)), color, Sd.Around(corner, length + width));
                }
            }
        }

        // ------------------------------------------------------------------------------------------------
        // Sprites
        // ------------------------------------------------------------------------------------------------

        /// <summary>The big window (menu, settings, the game's windows): 256 px square, slice border 80.</summary>
        public static Texture2D Window(DeathRaceThemeSpec spec)
        {
            const int size = 256;
            var r = new Raster(size, size, Color.clear);
            var center = new Vector2(size * 0.5f, size * 0.5f);
            if (!spec.Night)
            {
                var outer = new Vector2(size * 0.5f - 10f, size * 0.5f - 10f);
                Sdf frame = p => Sd.Box(p, center, outer, 22f);
                Sdf plate = p => Sd.Box(p, center, outer - new Vector2(16f, 16f), 10f);
                r.Shadow(frame, new Color(0f, 0f, 0f, 0.65f), 9f, new Vector2(0f, -4f), All(r));
                r.Fill(frame, p => Grit(Metal(p, spec.PlateLight, spec.PlateDark, size, 11), p, r, 80f, 12, 1f), All(r));
                Bevel(r, frame, All(r), 4f, 1f);
                r.Fill(plate, p => Grit(Metal(p, spec.Plate, Shade(spec.PlateDark, 0.9f), size, 13), p, r, 70f, 14, 0.5f), All(r));
                Inner(r, plate, All(r), new Color(0f, 0f, 0f, 0.5f), 9f, new Vector2(0f, -3f));
                r.Fill(p => Sd.Outline(plate(p) + 1.5f, 2.5f), WithAlpha(spec.Edge, 0.85f), All(r));
                // Hazard brackets in two corners.
                var a = new[] { new Vector2(14f, size - 14f), new Vector2(64f, size - 14f), new Vector2(14f, size - 64f) };
                Hazard(r, p => Sd.Intersect(Sd.Polygon(p, a), frame(p)), Sd.Bounds(a), 14f, spec.Second);
                var b = new[] { new Vector2(size - 14f, 14f), new Vector2(size - 64f, 14f), new Vector2(size - 14f, 64f) };
                Hazard(r, p => Sd.Intersect(Sd.Polygon(p, b), frame(p)), Sd.Bounds(b), 14f, spec.Second);
                foreach (Vector2 at in new[] { new Vector2(size - 19f, size - 19f), new Vector2(19f, 19f), new Vector2(size - 46f, size - 19f), new Vector2(19f, 46f) })
                {
                    Rivet(r, at, 4.5f);
                }
            }
            else
            {
                var outer = new Vector2(size * 0.5f - 12f, size * 0.5f - 12f);
                Sdf box = p => Sd.Box(p, center, outer, 18f);
                r.Fill(box, p => Glass(p, spec.PlateLight, spec.PlateDark, size, 0.94f), All(r));
                Neon(r, box, All(r), spec.Accent, 3f, 10f);
                r.Fill(p => Sd.Outline(box(p) + 9f, 1.2f), WithAlpha(spec.Accent, 0.3f), All(r));
                Brackets(r, center, outer - new Vector2(4f, 4f), 30f, 4f, spec.Second);
            }
            return r.ToTexture();
        }

        /// <summary>A smaller panel (the HUD's, cards, the rows area): 128 px, border 40.</summary>
        public static Texture2D Panel(DeathRaceThemeSpec spec)
        {
            const int size = 128;
            var r = new Raster(size, size, Color.clear);
            var center = new Vector2(size * 0.5f, size * 0.5f);
            if (!spec.Night)
            {
                var half = new Vector2(size * 0.5f - 6f, size * 0.5f - 6f);
                Sdf plate = p => Sd.Box(p, center, half, 12f);
                r.Shadow(plate, new Color(0f, 0f, 0f, 0.55f), 5f, new Vector2(0f, -2f), All(r));
                r.Fill(plate, p => WithAlpha(Grit(Metal(p, spec.Plate, spec.PlateDark, size, 21), p, r, 40f, 22, 0.8f), 0.9f), All(r));
                Bevel(r, plate, All(r), 3f, 1f);
                r.Fill(p => Sd.Outline(plate(p) + 1.5f, 1.6f), WithAlpha(spec.Edge, 0.55f), All(r));
                var tick = new[] { new Vector2(6f, size - 6f), new Vector2(34f, size - 6f), new Vector2(6f, size - 34f) };
                Hazard(r, p => Sd.Intersect(Sd.Polygon(p, tick), plate(p)), Sd.Bounds(tick), 10f, spec.Second);
            }
            else
            {
                var half = new Vector2(size * 0.5f - 8f, size * 0.5f - 8f);
                Sdf box = p => Sd.Box(p, center, half, 12f);
                r.Fill(box, p => Glass(p, spec.PlateLight, spec.PlateDark, size, 0.86f), All(r));
                Neon(r, box, All(r), spec.Accent, 2f, 6f, 0.35f);
                Brackets(r, center, half - new Vector2(3f, 3f), 16f, 3f, spec.Second);
            }
            return r.ToTexture();
        }

        /// <summary>A row of a list: 128 x 64 px, border 24; an accent tick at its left end.</summary>
        public static Texture2D Row(DeathRaceThemeSpec spec)
        {
            const int width = 128;
            const int height = 64;
            var r = new Raster(width, height, Color.clear);
            var center = new Vector2(width * 0.5f, height * 0.5f);
            var half = new Vector2(width * 0.5f - 3f, height * 0.5f - 3f);
            Sdf row = p => Sd.Box(p, center, half, 8f);
            if (!spec.Night)
            {
                r.Fill(row, p => WithAlpha(Metal(p, Shade(spec.Plate, 1.1f), spec.PlateDark, height, 31), 0.92f), All(r));
                Bevel(r, row, All(r), 2f, 0.8f);
                r.Fill(p => Sd.Box(p, new Vector2(9f, center.y), new Vector2(3f, half.y - 9f), 1.5f), spec.Accent, All(r));
            }
            else
            {
                r.Fill(row, p => Glass(p, spec.PlateLight, spec.Plate, height, 0.8f), All(r));
                r.Fill(p => Sd.Outline(row(p), 1.2f), WithAlpha(spec.Accent, 0.35f), All(r));
                r.Shadow(p => Sd.Box(p, new Vector2(9f, center.y), new Vector2(2f, half.y - 10f), 1f), WithAlpha(spec.Accent, 0.6f), 5f, Vector2.zero, All(r));
                r.Fill(p => Sd.Box(p, new Vector2(9f, center.y), new Vector2(2f, half.y - 10f), 1f), Lighten(spec.Accent, 0.3f), All(r));
            }
            return r.ToTexture();
        }

        public enum ButtonState
        {
            Normal,
            Hover,
            Pressed,
            Disabled
        }

        /// <summary>A button in one of its states: 160 x 96 px (or square), border 36.</summary>
        public static Texture2D Button(DeathRaceThemeSpec spec, ButtonState state, bool square = false)
        {
            int width = square ? 96 : 160;
            const int height = 96;
            var r = new Raster(width, height, Color.clear);
            var center = new Vector2(width * 0.5f, height * 0.5f);
            if (!spec.Night)
            {
                var half = new Vector2(width * 0.5f - 7f, height * 0.5f - 9f);
                Sdf plate = p => Sd.Box(p, center + new Vector2(0f, state == ButtonState.Pressed ? -2f : 1f), half, 10f);
                Color top;
                Color bottom;
                Color edge;
                float edgeWidth;
                switch (state)
                {
                    case ButtonState.Hover:
                        top = Color.Lerp(spec.PlateLight, spec.AccentDark, 0.45f);
                        bottom = Color.Lerp(spec.Plate, spec.AccentDark, 0.25f);
                        edge = spec.Second;
                        edgeWidth = 3.5f;
                        break;
                    case ButtonState.Pressed:
                        top = spec.Accent;
                        bottom = spec.AccentDark;
                        edge = spec.Second;
                        edgeWidth = 3f;
                        break;
                    case ButtonState.Disabled:
                        top = Hex(0x4A4846);
                        bottom = Hex(0x2E2D2C);
                        edge = Hex(0x6A6866);
                        edgeWidth = 2f;
                        break;
                    default:
                        top = spec.PlateLight;
                        bottom = spec.Plate;
                        edge = spec.Accent;
                        edgeWidth = 2.5f;
                        break;
                }
                if (state != ButtonState.Pressed)
                {
                    r.Shadow(plate, new Color(0f, 0f, 0f, 0.6f), 5f, new Vector2(0f, -4f), All(r));
                }
                r.Fill(plate, p => Metal(p, top, bottom, height, 41), All(r));
                if (state == ButtonState.Hover)
                {
                    Inner(r, plate, All(r), WithAlpha(spec.Accent, 0.5f), 14f, Vector2.zero);
                }
                Bevel(r, plate, All(r), 3f, state == ButtonState.Pressed ? 0.6f : 1f);
                r.Fill(p => Sd.Outline(plate(p) + edgeWidth * 0.5f, edgeWidth), edge, All(r));
                if (!square && state != ButtonState.Disabled)
                {
                    // A little hazard tab at each end.
                    foreach (float sx in new[] { -1f, 1f })
                    {
                        Vector2 tab = center + new Vector2(sx * (half.x - 14f), 0f);
                        Hazard(r, p => Sd.Intersect(Sd.Box(p, tab, new Vector2(6f, half.y - 12f), 2f), plate(p)), Sd.Around(tab, new Vector2(8f, half.y)), 9f,
                            state == ButtonState.Pressed ? Hex(0xFFE08A) : spec.Second, state == ButtonState.Normal ? 0.75f : 1f);
                    }
                }
            }
            else
            {
                var half = new Vector2(width * 0.5f - 10f, height * 0.5f - 12f);
                Sdf box = p => Sd.Box(p, center, half, 12f);
                Color fillTop;
                Color fillBottom;
                Color line;
                float lineWidth;
                float glow;
                switch (state)
                {
                    case ButtonState.Hover:
                        fillTop = Color.Lerp(spec.PlateLight, spec.AccentDark, 0.55f);
                        fillBottom = Color.Lerp(spec.Plate, spec.AccentDark, 0.3f);
                        line = Lighten(spec.Accent, 0.25f);
                        lineWidth = 3f;
                        glow = 9f;
                        break;
                    case ButtonState.Pressed:
                        fillTop = Color.Lerp(spec.PlateLight, spec.Second, 0.45f);
                        fillBottom = Color.Lerp(spec.Plate, spec.Second, 0.25f);
                        line = spec.Second;
                        lineWidth = 3f;
                        glow = 8f;
                        break;
                    case ButtonState.Disabled:
                        fillTop = Hex(0x1A1E2A);
                        fillBottom = Hex(0x10131C);
                        line = Hex(0x4A5266);
                        lineWidth = 1.5f;
                        glow = 0f;
                        break;
                    default:
                        fillTop = spec.PlateLight;
                        fillBottom = spec.Plate;
                        line = spec.Accent;
                        lineWidth = 2.2f;
                        glow = 6f;
                        break;
                }
                r.Fill(box, p => Glass(p, fillTop, fillBottom, height, 0.92f), All(r));
                if (state == ButtonState.Hover || state == ButtonState.Pressed)
                {
                    Inner(r, box, All(r), WithAlpha(line, 0.4f), 14f, Vector2.zero);
                }
                Neon(r, box, All(r), line, lineWidth, glow, 0.4f);
            }
            return r.ToTexture();
        }

        /// <summary>
        /// The banner behind the menu's title: 1024 x 192 px, borders 160 at the ends and 60 above and below. Scorched: a
        /// steel band with hazard striped, cut ends and rivets; Nightfall: a neon outlined band with angled ends.
        /// </summary>
        public static Texture2D Banner(DeathRaceThemeSpec spec)
        {
            const int width = 1024;
            const int height = 192;
            var r = new Raster(width, height, Color.clear);
            float top = height - 24f;
            float bottom = 28f;
            float cut = 42f;
            var outline = new[]
            {
                new Vector2(14f + cut, bottom), new Vector2(width - 14f - cut, bottom), new Vector2(width - 14f, (top + bottom) * 0.5f),
                new Vector2(width - 14f - cut, top), new Vector2(14f + cut, top), new Vector2(14f, (top + bottom) * 0.5f)
            };
            Sdf band = p => Sd.Polygon(p, outline);
            if (!spec.Night)
            {
                r.Shadow(band, new Color(0f, 0f, 0f, 0.7f), 10f, new Vector2(0f, -6f), All(r));
                r.Fill(band, p => Grit(Metal(p, spec.PlateLight, spec.PlateDark, height, 51), p, r, 220f, 52, 0.9f), All(r));
                Bevel(r, band, All(r), 4f, 1.2f);
                foreach (float sx in new[] { 0f, 1f })
                {
                    float x0 = sx < 0.5f ? 14f : width - 124f;
                    var block = new Rect(x0, 0f, 110f, height);
                    Hazard(r, p => Sd.Intersect(band(p) + 9f, sx < 0.5f ? p.x - (x0 + 104f) : (x0 + 6f) - p.x), block, 22f, spec.Second);
                    float rivetX = sx < 0.5f ? 145f : width - 145f;
                    Rivet(r, new Vector2(rivetX, top - 18f), 6f);
                    Rivet(r, new Vector2(rivetX, bottom + 18f), 6f);
                }
                r.Fill(p => Sd.Outline(band(p) + 9f, 3f), WithAlpha(spec.Edge, 0.9f), All(r));
            }
            else
            {
                r.Fill(band, p => Glass(p, spec.PlateLight, spec.PlateDark, height, 0.92f), All(r));
                Neon(r, band, All(r), spec.Accent, 3.5f, 14f, 0.5f);
                Neon(r, p => band(p) + 12f, All(r), spec.Second, 1.8f, 6f, 0.35f);
                foreach (float x in new[] { 84f, width - 84f })
                {
                    for (int i = 0; i < 3; i++)
                    {
                        float dx = (i - 1) * 22f * (x < width * 0.5f ? 1f : -1f);
                        var chevron = new[] { new Vector2(x + dx - 6f, top - 34f), new Vector2(x + dx + 8f, (top + bottom) * 0.5f), new Vector2(x + dx - 6f, bottom + 34f) };
                        if (x > width * 0.5f)
                        {
                            for (int k = 0; k < chevron.Length; k++)
                            {
                                chevron[k].x = 2f * (x + dx) - chevron[k].x;
                            }
                        }
                        r.Fill(p => Sd.Union(Sd.Segment(p, chevron[0], chevron[1], 3f), Sd.Segment(p, chevron[1], chevron[2], 3f)), WithAlpha(spec.Accent, 0.4f + i * 0.25f), All(r));
                    }
                }
            }
            return r.ToTexture();
        }

        /// <summary>The trough of a slider or a bar: 64 x 32 px, border 14.</summary>
        public static Texture2D Track(DeathRaceThemeSpec spec)
        {
            const int width = 64;
            const int height = 32;
            var r = new Raster(width, height, Color.clear);
            var center = new Vector2(width * 0.5f, height * 0.5f);
            Sdf trough = p => Sd.Box(p, center, new Vector2(width * 0.5f - 2f, height * 0.5f - 3f), 10f);
            if (!spec.Night)
            {
                r.Fill(trough, Hex(0x0E0C0B, 0.95f), All(r));
                Inner(r, trough, All(r), new Color(0f, 0f, 0f, 0.8f), 5f, new Vector2(0f, -2f));
                r.Fill(p => Sd.Intersect(Sd.Outline(trough(p), 1.5f), center.y - p.y), new Color(1f, 1f, 1f, 0.18f), All(r));
            }
            else
            {
                r.Fill(trough, WithAlpha(spec.PlateDark, 0.95f), All(r));
                r.Fill(p => Sd.Outline(trough(p), 1.2f), WithAlpha(spec.Accent, 0.4f), All(r));
            }
            return r.ToTexture();
        }

        /// <summary>A white fill with a glossy top half, tinted in code: 64 x 32 px, border 14.</summary>
        public static Texture2D Fill(DeathRaceThemeSpec spec)
        {
            const int width = 64;
            const int height = 32;
            var r = new Raster(width, height, Color.clear);
            var center = new Vector2(width * 0.5f, height * 0.5f);
            Sdf bar = p => Sd.Box(p, center, new Vector2(width * 0.5f - 4f, height * 0.5f - 6f), 7f);
            if (spec.Night)
            {
                r.Shadow(bar, new Color(1f, 1f, 1f, 0.35f), 4f, Vector2.zero, All(r));
            }
            r.Fill(bar, p =>
            {
                float t = (p.y - (center.y - 10f)) / 20f;
                float shine = t > 0.55f ? 1f : 0.82f + t * 0.2f;
                return new Color(shine, shine, shine, 1f);
            }, All(r));
            return r.ToTexture();
        }

        /// <summary>The knob of a slider: a hex bolt head (Scorched) or a glowing ring (Nightfall). 64 px.</summary>
        public static Texture2D Knob(DeathRaceThemeSpec spec)
        {
            const int size = 64;
            var r = new Raster(size, size, Color.clear);
            var center = new Vector2(size * 0.5f, size * 0.5f);
            if (!spec.Night)
            {
                var hex = new Vector2[6];
                for (int i = 0; i < 6; i++)
                {
                    float angle = (30f + i * 60f) * Mathf.Deg2Rad;
                    hex[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 22f;
                }
                r.Shadow(p => Sd.Circle(p, center, 26f), new Color(0f, 0f, 0f, 0.6f), 4f, new Vector2(1f, -3f), All(r));
                r.Fill(p => Sd.Circle(p, center, 26f), spec.Accent, All(r));
                r.Fill(p => Sd.Outline(Sd.Circle(p, center, 26f), 2f), Shade(spec.AccentDark, 0.7f), All(r));
                r.Fill(p => Sd.Polygon(p, hex), p => Color.Lerp(Hex(0x4A4440), Hex(0xC9C0B4), Mathf.Clamp01((p.y - center.y) / 44f + 0.5f)), All(r));
                r.Fill(p => Sd.Outline(Sd.Polygon(p, hex), 1.6f), Hex(0x2A2522), All(r));
                r.Fill(p => Sd.Circle(p, center, 6f), Hex(0x2A2522), All(r));
            }
            else
            {
                r.Shadow(p => Sd.Circle(p, center, 20f), WithAlpha(spec.Accent, 0.6f), 9f, Vector2.zero, All(r));
                r.Fill(p => Sd.Circle(p, center, 20f), spec.PlateDark, All(r));
                Neon(r, p => Sd.Circle(p, center, 18f), All(r), spec.Accent, 4f, 0f);
                r.Fill(p => Sd.Circle(p, center, 6f), Lighten(spec.Accent, 0.6f), All(r));
            }
            return r.ToTexture();
        }

        /// <summary>The box of a toggle: 72 px.</summary>
        public static Texture2D CheckBox(DeathRaceThemeSpec spec)
        {
            const int size = 72;
            var r = new Raster(size, size, Color.clear);
            var center = new Vector2(size * 0.5f, size * 0.5f);
            Sdf box = p => Sd.Box(p, center, new Vector2(28f, 28f), 7f);
            if (!spec.Night)
            {
                r.Shadow(box, new Color(0f, 0f, 0f, 0.6f), 4f, new Vector2(0f, -3f), All(r));
                r.Fill(box, p => Metal(p, spec.PlateLight, spec.Plate, size, 61), All(r));
                Bevel(r, box, All(r), 3f, 1f);
                r.Fill(p => Sd.Box(p, center, new Vector2(19f, 19f), 4f), Hex(0x0E0C0B), All(r));
                r.Fill(p => Sd.Outline(box(p) + 1.5f, 2.5f), spec.Accent, All(r));
            }
            else
            {
                r.Fill(box, WithAlpha(spec.PlateDark, 0.95f), All(r));
                Neon(r, box, All(r), spec.Accent, 2.5f, 6f);
            }
            return r.ToTexture();
        }

        /// <summary>The tick of a toggle that is on: 72 px.</summary>
        public static Texture2D CheckMark(DeathRaceThemeSpec spec)
        {
            const int size = 72;
            var r = new Raster(size, size, Color.clear);
            var a = new Vector2(20f, 37f);
            var b = new Vector2(31f, 25f);
            var c = new Vector2(52f, 50f);
            Sdf tick = p => Sd.Union(Sd.Segment(p, a, b, 5f), Sd.Segment(p, b, c, 5f));
            if (!spec.Night)
            {
                r.Fill(p => tick(p) - 2.5f, Hex(0x1A0E06), All(r));
                r.Fill(tick, p => Color.Lerp(spec.AccentDark, spec.Second, Mathf.Clamp01((p.y - 20f) / 30f)), All(r));
            }
            else
            {
                r.Shadow(tick, WithAlpha(spec.Accent, 0.7f), 8f, Vector2.zero, All(r));
                r.Fill(tick, spec.Accent, All(r));
                r.Fill(p => tick(p) + 2.8f, Lighten(spec.Accent, 0.7f), All(r));
            }
            return r.ToTexture();
        }

        /// <summary>The box of a text field: 128 x 64 px, border 24.</summary>
        public static Texture2D InputBox(DeathRaceThemeSpec spec)
        {
            const int width = 128;
            const int height = 64;
            var r = new Raster(width, height, Color.clear);
            var center = new Vector2(width * 0.5f, height * 0.5f);
            Sdf box = p => Sd.Box(p, center, new Vector2(width * 0.5f - 4f, height * 0.5f - 4f), 8f);
            if (!spec.Night)
            {
                r.Fill(box, Hex(0x14110F, 0.96f), All(r));
                Inner(r, box, All(r), new Color(0f, 0f, 0f, 0.8f), 6f, new Vector2(0f, -2f));
                r.Fill(p => Sd.Outline(box(p) + 1f, 1.6f), WithAlpha(spec.Muted, 0.6f), All(r));
            }
            else
            {
                r.Fill(box, WithAlpha(spec.PlateDark, 0.96f), All(r));
                r.Fill(p => Sd.Outline(box(p) + 1f, 1.6f), WithAlpha(spec.Accent, 0.55f), All(r));
            }
            return r.ToTexture();
        }

        /// <summary>The frame of a HUD bar (health, nitro): a trough in a frame, 192 x 48 px, borders 24 / 20.</summary>
        public static Texture2D BarFrame(DeathRaceThemeSpec spec)
        {
            const int width = 192;
            const int height = 48;
            var r = new Raster(width, height, Color.clear);
            var center = new Vector2(width * 0.5f, height * 0.5f);
            Sdf frame = p => Sd.Box(p, center, new Vector2(width * 0.5f - 3f, height * 0.5f - 3f), 9f);
            Sdf trough = p => Sd.Box(p, center, new Vector2(width * 0.5f - 9f, height * 0.5f - 9f), 5f);
            if (!spec.Night)
            {
                r.Shadow(frame, new Color(0f, 0f, 0f, 0.55f), 3f, new Vector2(0f, -2f), All(r));
                r.Fill(frame, p => Metal(p, spec.PlateLight, spec.PlateDark, height, 71), All(r));
                Bevel(r, frame, All(r), 2.5f, 1f);
                r.Fill(trough, Hex(0x0B0908, 0.95f), All(r));
                foreach (float x in new[] { 7f, width - 7f })
                {
                    r.Fill(p => Sd.Box(p, new Vector2(x, center.y), new Vector2(1.2f, 8f)), spec.Accent, All(r));
                }
            }
            else
            {
                r.Fill(frame, WithAlpha(spec.PlateDark, 0.92f), All(r));
                Neon(r, frame, All(r), spec.Accent, 2f, 4f, 0.35f);
                r.Fill(trough, WithAlpha(Hex(0x03040A), 0.95f), All(r));
            }
            return r.ToTexture();
        }

        /// <summary>The fill of a HUD bar, white (tinted in code), lined up with the trough of <see cref="BarFrame"/>.</summary>
        public static Texture2D BarFill(DeathRaceThemeSpec spec)
        {
            const int width = 192;
            const int height = 48;
            var r = new Raster(width, height, Color.clear);
            var center = new Vector2(width * 0.5f, height * 0.5f);
            Sdf bar = p => Sd.Box(p, center, new Vector2(width * 0.5f - 11f, height * 0.5f - 11f), 4f);
            r.Fill(bar, p =>
            {
                float t = (p.y - 11f) / (height - 22f);
                float shine = t > 0.6f ? 1f : 0.78f + t * 0.25f;
                return new Color(shine, shine, shine, 1f);
            }, All(r));
            return r.ToTexture();
        }

        /// <summary>The full screen backdrop of the menu: the race behind shows through the middle, the edges darken.</summary>
        public static Texture2D Backdrop(DeathRaceThemeSpec spec)
        {
            const int width = 512;
            const int height = 288;
            var r = new Raster(width, height, Color.clear);
            Color tint = spec.Night ? Hex(0x03040C) : Hex(0x120A05);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = (x + 0.5f) / width * 2f - 1f;
                    float dy = (y + 0.5f) / height * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx * 0.8f + dy * dy) / 1.34f;
                    float a = Mathf.Lerp(0.28f, 0.92f, N.Smooth(0.15f, 1f, d));
                    // A darker strip on the left, behind the menu's buttons.
                    a = Mathf.Max(a, 0.5f * N.Smooth(0.1f, -0.45f, dx));
                    r.Set(x, y, WithAlpha(tint, a));
                }
            }
            return r.ToTexture(false);
        }

        // ------------------------------------------------------------------------------------------------
        // Plain white shapes, tinted by whoever uses them
        // ------------------------------------------------------------------------------------------------

        public static Texture2D Rounded(int size, float radius)
        {
            var r = new Raster(size, size, Color.clear);
            var center = new Vector2(size * 0.5f, size * 0.5f);
            r.Fill(p => Sd.Box(p, center, center - new Vector2(1f, 1f), radius), Color.white, All(r));
            return r.ToTexture();
        }

        public static Texture2D Circle(int size)
        {
            var r = new Raster(size, size, Color.clear);
            var center = new Vector2(size * 0.5f, size * 0.5f);
            r.Fill(p => Sd.Circle(p, center, size * 0.48f), Color.white, All(r));
            return r.ToTexture();
        }

        public static Texture2D Glow(int size)
        {
            var r = new Raster(size, size, Color.clear);
            var center = new Vector2(size * 0.5f, size * 0.5f);
            float radius = size * 0.5f;
            r.Fill(p => Sd.Circle(p, center, radius), p =>
            {
                float d = Vector2.Distance(p, center) / radius;
                float a = Mathf.Clamp01(1f - d);
                return new Color(1f, 1f, 1f, a * a);
            }, All(r));
            return r.ToTexture();
        }

        public static Texture2D Pill(int size)
        {
            var r = new Raster(size, size, Color.clear);
            var center = new Vector2(size * 0.5f, size * 0.5f);
            r.Fill(p => Sd.Circle(p, center, size * 0.5f - 1f), Color.white, All(r));
            return r.ToTexture();
        }

        /// <summary>Clear at the top, white at the bottom: fades a picture into what is under it.</summary>
        public static Texture2D Fade()
        {
            var r = new Raster(8, 128, Color.clear);
            for (int y = 0; y < 128; y++)
            {
                float t = 1f - y / 127f;
                for (int x = 0; x < 8; x++)
                {
                    r.Set(x, y, new Color(1f, 1f, 1f, t * t * (3f - 2f * t)));
                }
            }
            return r.ToTexture(false);
        }
    }
}
