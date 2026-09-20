using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Portfolio.Monopoly.EditorTools
{
    /// <summary>
    /// Draws the generated textures: the board (tracks, colour bars, jail cell, card spots and the globe in the
    /// middle; names, prices and icons are TextMesh Pro on top of it), the wooden table, the dice faces, the card
    /// backs, the interface sprites and the token badges. Everything is deterministic, so a rebuild writes the same
    /// bytes and changes nothing.
    /// </summary>
    internal static class MonopolyArt
    {
        public static readonly Color Mint = MonopolyStyle.Hex(0xC4E3C6);
        public static readonly Color Line = MonopolyStyle.Hex(0x1F2A24);

        // ------------------------------------------------------------------ board

        /// <summary>The board surface: <paramref name="size"/> pixels for the whole square of <paramref name="geometry"/>.</summary>
        public static Texture2D Board(BoardGeometry geometry, IList<SpaceData> spaces, int size)
        {
            var raster = new Raster(size, size, Mint);
            float ppu = size / geometry.Side;
            Func<Vector2, Vector2> px = p => (p + new Vector2(geometry.Half, geometry.Half)) * ppu;
            Func<Rect, Rect> pxRect = r => Rect.MinMaxRect(px(r.min).x, px(r.min).y, px(r.max).x, px(r.max).y);
            float line = Mathf.Max(2f, ppu * 0.016f);

            // The globe in the middle: meridians and parallels in a darker mint.
            Vector2 middle = new Vector2(size * 0.5f, size * 0.5f);
            float globe = ppu * 3.3f;
            Color faint = new Color(0.2f, 0.42f, 0.3f, 0.16f);
            raster.Fill(p => Sd.Ring(p, middle, globe, line * 1.6f), faint, Sd.Around(middle, globe + line * 2f));
            for (int i = 1; i <= 3; i++)
            {
                float rx = globe * (i / 4f);
                raster.Fill(p => Sd.Outline(Sd.Ellipse(p, middle, new Vector2(rx, globe)), line), faint, Sd.Around(middle, globe + line));
            }
            for (int i = -2; i <= 2; i++)
            {
                float y = globe * i / 3.2f;
                float half = Mathf.Sqrt(Mathf.Max(0f, globe * globe - y * y));
                Vector2 a = middle + new Vector2(-half, y);
                Vector2 b = middle + new Vector2(half, y);
                raster.Fill(p => Sd.Segment(p, a, b, line * 0.5f), faint, Rect.MinMaxRect(a.x - line, a.y - line, b.x + line, b.y + line));
            }

            // The card spots: Community Chest up left, Chance down right, turned like on the classic board.
            CardSpot(raster, px(new Vector2(-2.35f, 2.35f)), ppu * 0.9f, ppu * 1.3f, 45f, MonopolyStyle.ChestBlue, line);
            CardSpot(raster, px(new Vector2(2.35f, -2.35f)), ppu * 0.9f, ppu * 1.3f, 45f, MonopolyStyle.ChanceOrange, line);

            // Spaces: colour bars, the jail cell and the outlines.
            for (int space = 0; space < spaces.Count; space++)
            {
                SpaceData data = spaces[space];
                if (data.kind == SpaceKind.Street)
                {
                    Rect bar = pxRect(geometry.Part(space, 1f - 0.24f, 1f));
                    raster.FillRect(bar, MonopolyStyle.GroupColor(data.group));
                    // A soft sheen on the bar.
                    raster.FillRect(new Rect(bar.x, bar.y, bar.width, bar.height), new Color(1f, 1f, 1f, 0.06f));
                    Rect under = pxRect(geometry.Part(space, 1f - 0.24f, 1f - 0.24f));
                    raster.FillRect(Grow(under, line * 0.5f), Line);
                }
                if (data.kind == SpaceKind.Jail)
                {
                    Rect cell = pxRect(geometry.Part(space, 0.3f, 1f, 0f, 0.7f));
                    raster.FillRect(cell, MonopolyStyle.Hex(0xF7941D));
                    for (int bar = 1; bar <= 5; bar++)
                    {
                        float x = Mathf.Lerp(cell.xMin, cell.xMax, bar / 6f);
                        raster.FillRect(new Rect(x - line * 0.9f, cell.yMin + cell.height * 0.1f, line * 1.8f, cell.height * 0.8f), new Color(0.12f, 0.12f, 0.12f, 0.85f));
                    }
                    Outline(raster, cell, line, Line);
                }
                if (data.kind == SpaceKind.Go)
                {
                    // A red flash behind the GO arrow.
                    Rect go = pxRect(geometry.Bounds(space));
                    Vector2 c = go.center;
                    raster.Fill(p => Sd.Circle(p, c, go.width * 0.36f), new Color(0.89f, 0f, 0.17f, 0.12f), Sd.Around(c, go.width * 0.4f));
                }
                Outline(raster, pxRect(geometry.Bounds(space)), line, Line);
            }

            // The border between the track and the middle, and the board's edge.
            float inner = geometry.Half - geometry.Corner;
            Outline(raster, pxRect(Rect.MinMaxRect(-inner, -inner, inner, inner)), line * 1.6f, Line);
            Outline(raster, new Rect(0, 0, size, size), line * 2.2f, Line);
            return raster.ToTexture(false);
        }

        private static void CardSpot(Raster raster, Vector2 center, float halfWidth, float halfHeight, float angle, Color color, float line)
        {
            var half = new Vector2(halfWidth, halfHeight);
            float radius = halfWidth * 0.12f;
            Sdf box = p => Sd.Box(Sd.Rotate(p, center, angle), center, half, radius);
            Rect bounds = Sd.Around(center, halfHeight * 1.5f);
            raster.Fill(box, MonopolyStyle.WithAlpha(color, 0.14f), bounds);
            raster.Fill(p => Sd.Outline(box(p), line * 1.4f), MonopolyStyle.WithAlpha(color, 0.9f), bounds);
            raster.Fill(p => Sd.Outline(Sd.Box(Sd.Rotate(p, center, angle), center, half - Vector2.one * line * 4f, radius), line * 0.7f), MonopolyStyle.WithAlpha(color, 0.5f), bounds);
        }

        private static void Outline(Raster raster, Rect rect, float width, Color color)
        {
            float h = width * 0.5f;
            raster.FillRect(Rect.MinMaxRect(rect.xMin - h, rect.yMin - h, rect.xMax + h, rect.yMin + h), color);
            raster.FillRect(Rect.MinMaxRect(rect.xMin - h, rect.yMax - h, rect.xMax + h, rect.yMax + h), color);
            raster.FillRect(Rect.MinMaxRect(rect.xMin - h, rect.yMin + h, rect.xMin + h, rect.yMax - h), color);
            raster.FillRect(Rect.MinMaxRect(rect.xMax - h, rect.yMin + h, rect.xMax + h, rect.yMax - h), color);
        }

        private static Rect Grow(Rect rect, float amount)
        {
            return Rect.MinMaxRect(rect.xMin - amount, rect.yMin - amount, rect.xMax + amount, rect.yMax + amount);
        }

        // ------------------------------------------------------------------ table

        /// <summary>A tileable wooden tabletop of four planks.</summary>
        public static Texture2D Table(int size)
        {
            var raster = new Raster(size, size, Color.black);
            Color dark = MonopolyStyle.Hex(0x3E2416);
            Color light = MonopolyStyle.Hex(0x7A4A2B);
            int planks = 4;
            int plankWidth = size / planks;
            var random = new System.Random(11);
            var tints = Enumerable.Range(0, planks).Select(_ => 0.85f + (float)random.NextDouble() * 0.3f).ToArray();
            var joints = Enumerable.Range(0, planks).Select(_ => random.Next(size)).ToArray();
            for (int y = 0; y < size; y++)
            {
                float v = y / (float)size;
                for (int x = 0; x < size; x++)
                {
                    int plank = x / plankWidth;
                    float u = x / (float)size;
                    float warp = 0.25f * Mathf.Sin(2f * Mathf.PI * (v * 2f + plank * 0.37f)) + 0.06f * Mathf.Sin(2f * Mathf.PI * (v * 9f + plank));
                    float grain = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * (u * 52f + warp * 3f));
                    float fine = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * (u * 190f + warp * 7f + v * 3f));
                    float t = Mathf.Clamp01(0.35f + 0.45f * grain * grain + 0.12f * fine);
                    Color c = Color.Lerp(dark, light, t) * tints[plank];
                    int inPlank = x - plank * plankWidth;
                    if (inPlank < 3 || inPlank > plankWidth - 3)
                    {
                        c *= 0.55f;
                    }
                    int joint = (y - joints[plank] + size) % size;
                    if (joint < 3)
                    {
                        c *= 0.6f;
                    }
                    c.a = 1f;
                    raster.Set(x, y, c);
                }
            }
            raster.Grain(new Rect(0, 0, size, size), 0.035f, 5);
            return raster.ToTexture(false);
        }

        // ------------------------------------------------------------------ dice

        /// <summary>A 3 by 2 atlas of die faces (1 to 6 as the die mesh maps them); the speed die has 1, 2, 3, the bus and Mr. Monopoly twice.</summary>
        public static Texture2D DiceAtlas(bool speed, int cell)
        {
            Color body = speed ? MonopolyStyle.Hex(0xD7263D) : MonopolyStyle.Hex(0xFBFAF6);
            Color pip = speed ? Color.white : MonopolyStyle.Hex(0x1A1A1D);
            var raster = new Raster(cell * 3, cell * 2, body);
            for (int face = 0; face < 6; face++)
            {
                var origin = new Vector2((face % 3) * cell, face < 3 ? cell : 0);
                Vector2 center = origin + new Vector2(cell * 0.5f, cell * 0.5f);
                // A little shading towards the edges hints at the rounding.
                raster.Fill(p => Sd.Box(p, center, new Vector2(cell * 0.5f, cell * 0.5f), 0f),
                    p => new Color(0f, 0f, 0f, Mathf.Clamp01(((p - center).magnitude / (cell * 0.5f) - 0.75f) * 0.25f)), new Rect(origin, new Vector2(cell, cell)));
                int value = face + 1;
                if (speed && value >= 4)
                {
                    if (value == 4)
                    {
                        Bus(raster, center, cell * 0.36f, pip);
                    }
                    else
                    {
                        TopHat(raster, center, cell * 0.34f, pip);
                    }
                    continue;
                }
                foreach (Vector2 offset in Pips(value))
                {
                    Vector2 c = center + offset * cell * 0.27f;
                    float r = cell * 0.085f;
                    raster.Fill(p => Sd.Circle(p, c, r), pip, Sd.Around(c, r + 2f));
                    // The pips are drilled: a soft highlight on their lower edge.
                    raster.Fill(p => Sd.Outline(Sd.Circle(p, c, r * 0.82f), r * 0.18f), p => new Color(1f, 1f, 1f, p.y < c.y ? 0.18f : 0f), Sd.Around(c, r));
                }
            }
            return raster.ToTexture(false);
        }

        private static IEnumerable<Vector2> Pips(int value)
        {
            switch (value)
            {
                case 1: return new[] { Vector2.zero };
                case 2: return new[] { new Vector2(-1, 1), new Vector2(1, -1) };
                case 3: return new[] { new Vector2(-1, 1), Vector2.zero, new Vector2(1, -1) };
                case 4: return new[] { new Vector2(-1, 1), new Vector2(1, 1), new Vector2(-1, -1), new Vector2(1, -1) };
                case 5: return new[] { new Vector2(-1, 1), new Vector2(1, 1), Vector2.zero, new Vector2(-1, -1), new Vector2(1, -1) };
                default: return new[] { new Vector2(-1, 1), new Vector2(1, 1), new Vector2(-1, 0), new Vector2(1, 0), new Vector2(-1, -1), new Vector2(1, -1) };
            }
        }

        /// <summary>Mr. Monopoly's top hat, a white silhouette.</summary>
        public static void TopHat(Raster raster, Vector2 center, float size, Color color)
        {
            Vector2 brim = center + new Vector2(0f, -size * 0.55f);
            Rect bounds = Sd.Around(center, size * 1.2f);
            Sdf hat = p => Sd.Union(
                Sd.Ellipse(p, brim, new Vector2(size * 0.95f, size * 0.2f)),
                Sd.Box(p, center + new Vector2(0f, size * 0.12f), new Vector2(size * 0.52f, size * 0.68f), size * 0.08f));
            raster.Fill(hat, color, bounds);
            // The band.
            raster.Fill(p => Sd.Box(p, center + new Vector2(0f, -size * 0.3f), new Vector2(size * 0.53f, size * 0.09f)), MonopolyStyle.WithAlpha(Color.black, 0.35f), bounds);
        }

        /// <summary>The bus of the speed die.</summary>
        public static void Bus(Raster raster, Vector2 center, float size, Color color)
        {
            Rect bounds = Sd.Around(center, size * 1.4f);
            Vector2 body = center + new Vector2(0f, size * 0.12f);
            raster.Fill(p => Sd.Box(p, body, new Vector2(size * 1.1f, size * 0.62f), size * 0.18f), color, bounds);
            Color window = MonopolyStyle.WithAlpha(MonopolyStyle.Hex(0x7A0F1C), 0.95f);
            for (int i = 0; i < 4; i++)
            {
                Vector2 w = body + new Vector2(-size * 0.72f + i * size * 0.44f, size * 0.18f);
                raster.Fill(p => Sd.Box(p, w, new Vector2(size * 0.16f, size * 0.17f), size * 0.04f), window, bounds);
            }
            foreach (float x in new[] { -0.62f, 0.62f })
            {
                Vector2 wheel = center + new Vector2(size * x, -size * 0.55f);
                raster.Fill(p => Sd.Circle(p, wheel, size * 0.2f), color, bounds);
                raster.Fill(p => Sd.Circle(p, wheel, size * 0.09f), window, bounds);
            }
        }

        // ------------------------------------------------------------------ cards

        /// <summary>The back of a Chance or Community Chest card (the icon and name are TextMesh Pro on top).</summary>
        public static Texture2D CardBack(Color color, int width, int height)
        {
            var raster = new Raster(width, height, Color.white);
            var center = new Vector2(width * 0.5f, height * 0.5f);
            var half = new Vector2(width * 0.5f - width * 0.06f, height * 0.5f - width * 0.06f);
            Rect all = new Rect(0, 0, width, height);
            raster.Fill(p => Sd.Box(p, center, half, width * 0.06f), p =>
            {
                float stripe = Mathf.Repeat((p.x + p.y) / (width * 0.12f), 1f) < 0.5f ? 0.04f : 0f;
                return Color.Lerp(color, Color.white, stripe);
            }, all);
            raster.Fill(p => Sd.Outline(Sd.Box(p, center, half - Vector2.one * width * 0.04f, width * 0.04f), width * 0.012f), new Color(1f, 1f, 1f, 0.85f), all);
            return raster.ToTexture(false);
        }

        // ------------------------------------------------------------------ interface

        public static Texture2D Rounded(int size, float radius)
        {
            var raster = new Raster(size, size, Color.clear);
            var center = new Vector2(size * 0.5f, size * 0.5f);
            raster.Fill(p => Sd.Box(p, center, center, radius), Color.white, new Rect(0, 0, size, size));
            return raster.ToTexture();
        }

        /// <summary>A soft shadow of a rounded rectangle, for nine slicing (<paramref name="blur"/> pixels of fade).</summary>
        public static Texture2D Shadow(int size, float radius, float blur)
        {
            var raster = new Raster(size, size, Color.clear);
            var center = new Vector2(size * 0.5f, size * 0.5f);
            var half = center - Vector2.one * blur;
            raster.Shadow(p => Sd.Box(p, center, half, radius), new Color(0f, 0f, 0f, 1f), blur, Vector2.zero, new Rect(0, 0, size, size));
            return raster.ToTexture();
        }

        public static Texture2D Circle(int size)
        {
            var raster = new Raster(size, size, Color.clear);
            var center = new Vector2(size * 0.5f, size * 0.5f);
            raster.Fill(p => Sd.Circle(p, center, size * 0.5f - 1f), Color.white, new Rect(0, 0, size, size));
            return raster.ToTexture();
        }

        public static Texture2D Ring(int size, float thickness)
        {
            var raster = new Raster(size, size, Color.clear);
            var center = new Vector2(size * 0.5f, size * 0.5f);
            raster.Fill(p => Sd.Ring(p, center, size * 0.5f - thickness * 0.5f - 1f, thickness), Color.white, new Rect(0, 0, size, size));
            return raster.ToTexture();
        }

        /// <summary>A radial glow, opaque in the middle and fading out.</summary>
        public static Texture2D Glow(int size)
        {
            var raster = new Raster(size, size, Color.clear);
            var center = new Vector2(size * 0.5f, size * 0.5f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = (new Vector2(x + 0.5f, y + 0.5f) - center).magnitude / (size * 0.5f);
                    float a = Mathf.Clamp01(1f - d);
                    raster.Set(x, y, new Color(1f, 1f, 1f, a * a));
                }
            }
            return raster.ToTexture();
        }

        /// <summary>A vertical gradient from white to transparent (a glossy highlight on buttons).</summary>
        public static Texture2D Sheen(int width, int height)
        {
            var raster = new Raster(width, height, Color.clear);
            for (int y = 0; y < height; y++)
            {
                float t = y / (float)(height - 1);
                for (int x = 0; x < width; x++)
                {
                    raster.Set(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01((t - 0.45f) * 1.4f) * 0.55f));
                }
            }
            return raster.ToTexture();
        }

        /// <summary>A banknote in the classic pastel green.</summary>
        public static Texture2D Bill(int width, int height)
        {
            var raster = new Raster(width, height, Color.clear);
            var center = new Vector2(width * 0.5f, height * 0.5f);
            var half = center - Vector2.one * 3f;
            Rect all = new Rect(0, 0, width, height);
            raster.Fill(p => Sd.Box(p, center, half, 6f), MonopolyStyle.Hex(0xBFE3B4), all);
            raster.Fill(p => Sd.Outline(Sd.Box(p, center, half - Vector2.one * 7f, 4f), 3f), MonopolyStyle.Hex(0x3C8A4B), all);
            raster.Fill(p => Sd.Ellipse(p, center, new Vector2(height * 0.3f, height * 0.3f)), MonopolyStyle.Hex(0x3C8A4B), all);
            raster.Fill(p => Sd.Ellipse(p, center, new Vector2(height * 0.22f, height * 0.22f)), MonopolyStyle.Hex(0xE8F5E2), all);
            return raster.ToTexture();
        }

        /// <summary>The glowing frame around a highlighted space.</summary>
        public static Texture2D HighlightFrame(int size)
        {
            var raster = new Raster(size, size, Color.clear);
            var center = new Vector2(size * 0.5f, size * 0.5f);
            float edge = size * 0.5f - size * 0.08f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Sd.Box(new Vector2(x + 0.5f, y + 0.5f), center, new Vector2(edge, edge), size * 0.06f);
                    float line = Mathf.Clamp01(1f - Mathf.Abs(d) / (size * 0.018f));
                    float glow = Mathf.Clamp01(1f - Mathf.Abs(d) / (size * 0.075f));
                    float inside = d < 0f ? 0.16f : 0f;
                    float a = Mathf.Max(line, glow * glow * 0.75f, inside);
                    raster.Set(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            return raster.ToTexture();
        }

        /// <summary>The red banner of the logo, with a white keyline (the word itself is TextMesh Pro).</summary>
        public static Texture2D LogoBanner(int width, int height)
        {
            var raster = new Raster(width, height, Color.clear);
            var center = new Vector2(width * 0.5f, height * 0.5f);
            Rect all = new Rect(0, 0, width, height);
            var half = center - Vector2.one * height * 0.08f;
            raster.Shadow(p => Sd.Box(p, center, half, height * 0.06f), new Color(0f, 0f, 0f, 0.35f), height * 0.06f, new Vector2(0f, -height * 0.02f), all);
            raster.Fill(p => Sd.Box(p, center, half, height * 0.06f), MonopolyStyle.Red, all);
            raster.Fill(p => Sd.Outline(Sd.Box(p, center, half - Vector2.one * height * 0.06f, height * 0.03f), height * 0.035f), Color.white, all);
            return raster.ToTexture();
        }

        /// <summary>A soft round shadow for the feet of tokens and dice.</summary>
        public static Texture2D ContactShadow(int size)
        {
            var raster = new Raster(size, size, Color.clear);
            var center = new Vector2(size * 0.5f, size * 0.5f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = (new Vector2(x + 0.5f, y + 0.5f) - center).magnitude / (size * 0.5f);
                    float a = Mathf.Clamp01(1f - d);
                    raster.Set(x, y, new Color(0f, 0f, 0f, a * a * 0.7f));
                }
            }
            return raster.ToTexture();
        }

        // ------------------------------------------------------------------ token badges

        /// <summary>A white silhouette of a token for the interface badges, in <paramref name="size"/> pixels.</summary>
        public static Texture2D TokenBadge(int token, int size)
        {
            var raster = new Raster(size, size, Color.clear);
            Color white = Color.white;
            switch (token)
            {
                case 0:
                    DrawCar(raster, size, white);
                    break;
                case 1:
                {
                    var c = new Vector2(size * 0.5f, size * 0.52f);
                    TopHat(raster, c, size * 0.34f, white);
                    break;
                }
                case 2:
                    FillOutline(raster, TokenOutlines.Outline(TokenOutlines.Dog), size, white);
                    break;
                case 3:
                    DrawShip(raster, size, white);
                    break;
                case 4:
                    FillOutline(raster, TokenOutlines.Outline(TokenOutlines.Cat), size, white);
                    break;
                case 5:
                    DrawDuck(raster, size, white);
                    break;
                case 6:
                    DrawPenguin(raster, size, white);
                    break;
                default:
                    FillOutline(raster, TokenOutlines.Outline(TokenOutlines.Dino), size, white);
                    break;
            }
            return raster.ToTexture();
        }

        /// <summary>Fills an outline scaled to fit the badge with a margin.</summary>
        private static void FillOutline(Raster raster, List<Vector2> outline, int size, Color color)
        {
            List<Vector2> fitted = Fit(outline, size, 0.1f);
            raster.Fill(p => Sd.Polygon(p, fitted), color, Sd.Bounds(fitted));
        }

        private static List<Vector2> Fit(IList<Vector2> points, int size, float margin)
        {
            Rect bounds = Sd.Bounds(points);
            float scale = size * (1f - margin * 2f) / Mathf.Max(bounds.width, bounds.height);
            Vector2 offset = new Vector2(size * 0.5f, size * 0.5f) - bounds.center * scale;
            return points.Select(p => p * scale + offset).ToList();
        }

        private static void DrawCar(Raster raster, int size, Color color)
        {
            List<Vector2> body = TokenOutlines.Outline(TokenOutlines.CarBody);
            // The car with its wheels spans x 0..108 and y 0..44 in design units.
            float scale = size * 0.8f / 108f;
            Vector2 offset = new Vector2(size * 0.1f, size * 0.32f);
            List<Vector2> fitted = body.Select(p => p * scale + offset).ToList();
            raster.Fill(p => Sd.Polygon(p, fitted), color, Sd.Bounds(fitted));
            foreach (float x in new[] { 22f, 88f })
            {
                Vector2 wheel = new Vector2(x, 13f) * scale + offset;
                raster.Fill(p => Sd.Circle(p, wheel, 14f * scale), color, Sd.Around(wheel, 15f * scale));
                raster.Erase(p => Sd.Circle(p, wheel, 5f * scale), Sd.Around(wheel, 6f * scale));
            }
            Vector2 head = new Vector2(40f, 43f) * scale + offset;
            raster.Fill(p => Sd.Circle(p, head, 7.5f * scale), color, Sd.Around(head, 8f * scale));
        }

        private static void DrawShip(Raster raster, int size, Color color)
        {
            float s = size / 256f;
            var hull = new List<Vector2>
            {
                new Vector2(20, 118), new Vector2(236, 118), new Vector2(214, 82), new Vector2(40, 82)
            }.Select(p => p * s).ToList();
            raster.Fill(p => Sd.Polygon(p, hull), color, Sd.Bounds(hull));
            Rect all = new Rect(0, 0, size, size);
            raster.Fill(p => Sd.Box(p, new Vector2(128, 128) * s, new Vector2(56, 14) * s, 3f * s), color, all);
            raster.Fill(p => Sd.Box(p, new Vector2(118, 150) * s, new Vector2(24, 16) * s, 3f * s), color, all);
            raster.Fill(p => Sd.Box(p, new Vector2(96, 150) * s, new Vector2(8, 24) * s, 2f * s), color, all);
            raster.Fill(p => Sd.Segment(p, new Vector2(128, 166) * s, new Vector2(128, 200) * s, 3f * s), color, all);
            foreach (float x in new[] { 64f, 192f })
            {
                Vector2 turret = new Vector2(x, 124) * s;
                raster.Fill(p => Sd.Box(p, turret, new Vector2(14, 9) * s, 5f * s), color, all);
                Vector2 tip = turret + new Vector2(x < 128 ? -34f : 34f, 6f) * s;
                raster.Fill(p => Sd.Segment(p, turret + new Vector2(0f, 4f) * s, tip, 3f * s), color, all);
            }
        }

        private static void DrawDuck(Raster raster, int size, Color color)
        {
            float s = size / 256f;
            Rect all = new Rect(0, 0, size, size);
            Sdf duck = p => Sd.Union(Sd.Union(
                    Sd.Ellipse(p, new Vector2(118, 100) * s, new Vector2(86, 56) * s),
                    Sd.Circle(p, new Vector2(160, 164) * s, 42f * s)),
                Sd.Union(Sd.Ellipse(p, new Vector2(212, 156) * s, new Vector2(30, 12) * s),
                    Sd.Polygon(p, new[] { new Vector2(34, 120) * s, new Vector2(46, 172) * s, new Vector2(72, 132) * s })));
            raster.Fill(duck, color, all);
            raster.Erase(p => Sd.Circle(p, new Vector2(172, 176) * s, 7f * s), all);
        }

        private static void DrawPenguin(Raster raster, int size, Color color)
        {
            float s = size / 256f;
            Rect all = new Rect(0, 0, size, size);
            Sdf penguin = p => Sd.Union(Sd.Union(
                    Sd.Ellipse(p, new Vector2(124, 102) * s, new Vector2(58, 80) * s),
                    Sd.Circle(p, new Vector2(128, 190) * s, 40f * s)),
                Sd.Union(Sd.Polygon(p, new[] { new Vector2(160, 196) * s, new Vector2(198, 186) * s, new Vector2(160, 178) * s }),
                    Sd.Union(Sd.Ellipse(p, new Vector2(150, 24) * s, new Vector2(26, 9) * s), Sd.Ellipse(p, new Vector2(92, 24) * s, new Vector2(24, 9) * s))));
            raster.Fill(penguin, color, all);
            raster.Erase(p => Sd.Outline(Sd.Ellipse(Sd.Rotate(p, new Vector2(88, 108) * s, -18f), new Vector2(88, 108) * s, new Vector2(14, 46) * s), 4f * s), all);
            raster.Erase(p => Sd.Circle(p, new Vector2(142, 198) * s, 6f * s), all);
        }
    }
}
