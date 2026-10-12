using Gamebox.Editor;
using UnityEngine;

namespace Portfolio.BoardGames.EditorTools
{
    /// <summary>
    /// The pictures of the game, drawn into a <see cref="Raster"/>: the grain of the squares, the soft highlight disc,
    /// the contact shadow under a piece, the rounded sprites of the interface and the launcher icon. Everything is
    /// seeded, so a rebuild draws the same pixels.
    /// </summary>
    internal static class BoardGamesArt
    {
        /// <summary>
        /// A square's surface: wood grain (long streaks along y) or slate (cloudy speckle) in the base colour, lightened
        /// and darkened by the two grain tints of the spec.
        /// </summary>
        public static Texture2D Surface(int size, Color baseColor, BoardGamesThemeSpec spec, int seed)
        {
            var raster = new Raster(size, size, baseColor);
            float ox = seed * 17.31f;
            float oy = seed * 7.77f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size;
                    float v = y / (float)size;
                    float n;
                    if (spec.Slate)
                    {
                        float a = Mathf.PerlinNoise(ox + u * 6f, oy + v * 6f);
                        float b = Mathf.PerlinNoise(ox + u * 23f, oy + v * 23f);
                        n = a * 0.7f + b * 0.3f;
                    }
                    else
                    {
                        float wobble = Mathf.PerlinNoise(ox + u * 3f, oy + v * 1.5f) * 0.12f;
                        float rings = Mathf.PerlinNoise(ox + (u + wobble) * 28f, oy + v * 2.2f);
                        float fine = Mathf.PerlinNoise(ox + u * 90f, oy + v * 9f);
                        n = rings * 0.75f + fine * 0.25f;
                    }
                    Color light = spec.SquareGrainA;
                    Color dark = spec.SquareGrainB;
                    float t = n * 2f - 1f;
                    Color tint = t > 0f ? light : dark;
                    tint.a *= Mathf.Abs(t);
                    raster.Blend(x, y, tint);
                }
            }
            raster.Grain(new Rect(0f, 0f, size, size), spec.Slate ? 0.03f : 0.02f, seed);
            return raster.ToTexture(false);
        }

        /// <summary>A soft white disc with a feathered edge: the highlight under a selected square or a legal move.</summary>
        public static Texture2D HighlightDisc(int size)
        {
            var raster = new Raster(size, size, Color.clear);
            var center = new Vector2(size * 0.5f, size * 0.5f);
            float radius = size * 0.46f;
            raster.Fill(p => Sd.Circle(p, center, radius), p =>
            {
                float d = Vector2.Distance(p, center) / radius;
                float a = Mathf.Clamp01(1f - Mathf.Pow(d, 3f));
                return new Color(1f, 1f, 1f, 0.35f + 0.65f * a);
            }, new Rect(0f, 0f, size, size));
            return raster.ToTexture();
        }

        /// <summary>The dark blur under a piece.</summary>
        public static Texture2D ContactShadow(int size)
        {
            var raster = new Raster(size, size, Color.clear);
            var center = new Vector2(size * 0.5f, size * 0.5f);
            float radius = size * 0.42f;
            raster.Fill(p => Sd.Circle(p, center, radius), p =>
            {
                float d = Vector2.Distance(p, center) / radius;
                float a = Mathf.Clamp01(1f - d * d);
                return new Color(0f, 0f, 0f, a * 0.8f);
            }, new Rect(0f, 0f, size, size));
            return raster.ToTexture();
        }

        /// <summary>A white rounded square for nine sliced windows, buttons and rows.</summary>
        public static Texture2D Rounded(int size, float radius)
        {
            var raster = new Raster(size, size, Color.clear);
            var center = new Vector2(size * 0.5f, size * 0.5f);
            raster.Fill(p => Sd.Box(p, center, center, radius), Color.white, new Rect(0f, 0f, size, size));
            return raster.ToTexture();
        }

        /// <summary>A white disc for badges, knobs and check marks.</summary>
        public static Texture2D Circle(int size)
        {
            var raster = new Raster(size, size, Color.clear);
            var center = new Vector2(size * 0.5f, size * 0.5f);
            raster.Fill(p => Sd.Circle(p, center, size * 0.48f), Color.white, new Rect(0f, 0f, size, size));
            return raster.ToTexture();
        }

        /// <summary>A white five pointed star, tinted at run time: the stars a campaign level earned.</summary>
        public static Texture2D Star(int size)
        {
            var raster = new Raster(size, size, Color.clear);
            var center = new Vector2(size * 0.5f, size * 0.5f);
            raster.Fill(p => Sd.Star(p, center, size * 0.47f, 5, 0.5f), Color.white, new Rect(0f, 0f, size, size));
            return raster.ToTexture();
        }

        /// <summary>A soft radial glow fading to the edges: the backdrop of the menu.</summary>
        public static Texture2D Glow(int size)
        {
            var raster = new Raster(size, size, Color.clear);
            var center = new Vector2(size * 0.5f, size * 0.5f);
            float radius = size * 0.5f;
            raster.Fill(p => Sd.Circle(p, center, radius), p =>
            {
                float d = Vector2.Distance(p, center) / radius;
                return new Color(1f, 1f, 1f, Mathf.Clamp01(1f - d * d));
            }, new Rect(0f, 0f, size, size));
            return raster.ToTexture();
        }

        /// <summary>A wide rounded band with a thin inner line: the banner behind a header.</summary>
        public static Texture2D Banner(int width, int height)
        {
            var raster = new Raster(width, height, Color.clear);
            var center = new Vector2(width * 0.5f, height * 0.5f);
            var half = new Vector2(width * 0.5f - 2f, height * 0.5f - 2f);
            float radius = height * 0.45f;
            var bounds = new Rect(0f, 0f, width, height);
            raster.Fill(p => Sd.Box(p, center, half, radius), Color.white, bounds);
            raster.Fill(p => Sd.Outline(Sd.Box(p, center, half - new Vector2(6f, 6f), radius - 6f), 1.5f), new Color(1f, 1f, 1f, 0.5f), bounds);
            return raster.ToTexture();
        }

        /// <summary>A white rounded frame (the outline of <see cref="Rounded"/>) for nine sliced outlined buttons.</summary>
        public static Texture2D RoundedFrame(int size, float radius, float thickness)
        {
            var raster = new Raster(size, size, Color.clear);
            var center = new Vector2(size * 0.5f, size * 0.5f);
            var half = center - new Vector2(1f, 1f);
            raster.Fill(p => Sd.Subtract(Sd.Box(p, center, half, radius), Sd.Box(p, center, half - new Vector2(thickness, thickness), radius - thickness)),
                Color.white, new Rect(0f, 0f, size, size));
            return raster.ToTexture();
        }

        /// <summary>
        /// The corners of a chess knight's silhouette facing left, its plinth on y = 0 and its ear at y = 1; the width is
        /// 0.78 of the height. Placed by <see cref="Knight"/>.
        /// </summary>
        private static readonly Vector2[] KnightOutline =
        {
            new Vector2(0.10f, 0.00f), new Vector2(0.90f, 0.00f), new Vector2(0.90f, 0.09f), new Vector2(0.80f, 0.12f),
            new Vector2(0.80f, 0.17f), new Vector2(0.74f, 0.20f), new Vector2(0.80f, 0.42f), new Vector2(0.77f, 0.62f),
            new Vector2(0.68f, 0.80f), new Vector2(0.58f, 0.91f), new Vector2(0.53f, 1.00f), new Vector2(0.46f, 0.90f),
            new Vector2(0.37f, 0.87f), new Vector2(0.24f, 0.73f), new Vector2(0.10f, 0.58f), new Vector2(0.06f, 0.49f),
            new Vector2(0.12f, 0.42f), new Vector2(0.26f, 0.45f), new Vector2(0.37f, 0.52f), new Vector2(0.36f, 0.38f),
            new Vector2(0.27f, 0.20f), new Vector2(0.20f, 0.17f), new Vector2(0.20f, 0.12f), new Vector2(0.10f, 0.09f)
        };

        /// <summary>The knight's silhouette in pixels: the middle of its plinth's bottom at <paramref name="foot"/>, <paramref name="height"/> tall.</summary>
        private static Vector2[] Knight(Vector2 foot, float height)
        {
            float width = height * 0.78f;
            var corners = new Vector2[KnightOutline.Length];
            for (int i = 0; i < corners.Length; i++)
            {
                corners[i] = new Vector2(foot.x + (KnightOutline[i].x - 0.5f) * width, foot.y + KnightOutline[i].y * height);
            }
            return corners;
        }

        /// <summary>
        /// The launcher icon: the three games on a rounded board tile: a chess knight standing at the back, a draughts
        /// piece at its foot on the left and a die in front on the right, in the colours of <paramref name="spec"/>'s look.
        /// </summary>
        public static Texture2D GameIcon(int size, BoardGamesThemeSpec spec)
        {
            var raster = new Raster(size, size, Color.clear);
            var bounds = new Rect(0f, 0f, size, size);
            var center = new Vector2(size * 0.5f, size * 0.5f);
            float half = size * 0.47f;
            raster.Fill(p => Sd.Box(p, center, new Vector2(half, half), size * 0.14f), spec.DarkSquare, bounds);
            float cell = half / 2f;
            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    if ((row + col) % 2 == 1)
                    {
                        var c = new Vector2(center.x - half + cell * (col + 0.5f), center.y - half + cell * (row + 0.5f));
                        raster.Fill(p => Sd.Box(p, c, new Vector2(cell * 0.5f, cell * 0.5f)), spec.LightSquare, bounds);
                    }
                }
            }
            raster.Mask(p => Sd.Box(p, center, new Vector2(half, half), size * 0.14f), bounds);
            var shadow = new Color(0f, 0f, 0f, 0.5f);
            var drop = new Vector2(size * 0.012f, -size * 0.022f);

            // The knight, in the light pieces' colour with a rim and an eye.
            Vector2[] knight = Knight(new Vector2(size * 0.5f, size * 0.31f), size * 0.6f);
            Rect knightBounds = Sd.Bounds(knight);
            knightBounds.min -= new Vector2(4f, 4f);
            knightBounds.max += new Vector2(4f, 4f);
            raster.Shadow(p => Sd.Polygon(p, knight), shadow, size * 0.03f, drop, knightBounds);
            Color knightBody = spec.LightPiece;
            Color knightRim = spec.Emissive ? spec.LightColor : Darken(spec.LightPiece, 0.5f);
            float knightBottom = knightBounds.yMin;
            float knightTop = knightBounds.yMax;
            raster.Fill(p => Sd.Polygon(p, knight), p =>
            {
                float shade = Mathf.Clamp01((p.y - knightBottom) / (knightTop - knightBottom));
                return Color.Lerp(Darken(knightBody, 0.18f), Lighten(knightBody, 0.12f), shade);
            }, knightBounds);
            raster.Fill(p => Sd.Outline(Sd.Polygon(p, knight), size * 0.014f), knightRim, knightBounds);
            var eye = new Vector2(size * 0.5f + (0.38f - 0.5f) * size * 0.6f * 0.78f, size * 0.31f + 0.74f * size * 0.6f);
            raster.Fill(p => Sd.Circle(p, eye, size * 0.018f), knightRim, Sd.Around(eye, size * 0.03f));

            // The draughts piece at the knight's foot.
            float radius = size * 0.155f;
            var piece = new Vector2(size * 0.26f, size * 0.27f);
            raster.Shadow(p => Sd.Circle(p, piece, radius), shadow, size * 0.03f, drop, bounds);
            Disc(raster, piece, radius, spec.DarkPiece, spec.Emissive ? spec.DarkColor : Lighten(spec.DarkPiece, 0.25f), bounds);

            // The die in front, turned a little, showing five.
            var die = new Vector2(size * 0.73f, size * 0.26f);
            float dieHalf = size * 0.135f;
            const float turn = 12f;
            Color dieBody = spec.Emissive ? spec.Paper : Lighten(spec.LightPiece, 0.5f);
            Color pip = spec.Emissive ? spec.LightColor : spec.Ink;
            Color dieRim = spec.Emissive ? spec.LightColor : Darken(dieBody, 0.35f);
            Rect dieBounds = Sd.Around(die, dieHalf * 1.5f);
            raster.Shadow(p => Sd.Box(Sd.Rotate(p, die, turn), die, new Vector2(dieHalf, dieHalf), dieHalf * 0.3f), shadow, size * 0.03f, drop, dieBounds);
            raster.Fill(p => Sd.Box(Sd.Rotate(p, die, turn), die, new Vector2(dieHalf, dieHalf), dieHalf * 0.3f), p =>
            {
                float shade = Mathf.Clamp01((p.y - die.y) / dieHalf * 0.5f + 0.5f);
                return Color.Lerp(Darken(dieBody, 0.12f), dieBody, shade);
            }, dieBounds);
            raster.Fill(p => Sd.Outline(Sd.Box(Sd.Rotate(p, die, turn), die, new Vector2(dieHalf, dieHalf), dieHalf * 0.3f), size * 0.01f), dieRim, dieBounds);
            var pips = new[] { Vector2.zero, new Vector2(-1f, -1f), new Vector2(1f, -1f), new Vector2(-1f, 1f), new Vector2(1f, 1f) };
            foreach (Vector2 offset in pips)
            {
                // Sd.Rotate turns a point the opposite way: turning by -turn places the pip on the turned face.
                Vector2 at = Sd.Rotate(die + offset * dieHalf * 0.52f, die, -turn);
                raster.Fill(p => Sd.Circle(p, at, size * 0.024f), pip, Sd.Around(at, size * 0.04f));
            }
            return raster.ToTexture();
        }

        private static void Disc(Raster raster, Vector2 center, float radius, Color body, Color rim, Rect bounds)
        {
            raster.Fill(p => Sd.Circle(p, center, radius), p =>
            {
                float d = Vector2.Distance(p, center) / radius;
                float shade = Mathf.Clamp01((p.y - center.y) / radius * 0.5f + 0.5f);
                Color c = Color.Lerp(Darken(body, 0.2f), body, shade);
                return Color.Lerp(c, rim, Mathf.Clamp01((d - 0.78f) / 0.22f) * 0.8f);
            }, bounds);
            raster.Fill(p => Sd.Ring(p, center, radius * 0.86f, radius * 0.02f), new Color(rim.r, rim.g, rim.b, 0.6f), bounds);
        }

        private static Color Lighten(Color c, float amount)
        {
            return new Color(Mathf.Lerp(c.r, 1f, amount), Mathf.Lerp(c.g, 1f, amount), Mathf.Lerp(c.b, 1f, amount), c.a);
        }

        private static Color Darken(Color c, float amount)
        {
            return new Color(c.r * (1f - amount), c.g * (1f - amount), c.b * (1f - amount), c.a);
        }
    }
}
