using Gamebox.Editor;
using UnityEngine;

namespace Portfolio.Checkers.EditorTools
{
    /// <summary>
    /// The pictures of the game, drawn into a <see cref="Raster"/>: the grain of the squares, the soft highlight disc,
    /// the contact shadow under a piece, the rounded sprites of the interface and the launcher icon. Everything is
    /// seeded, so a rebuild draws the same pixels.
    /// </summary>
    internal static class CheckersArt
    {
        /// <summary>
        /// A square's surface: wood grain (long streaks along y) or slate (cloudy speckle) in the base colour, lightened
        /// and darkened by the two grain tints of the spec.
        /// </summary>
        public static Texture2D Surface(int size, Color baseColor, CheckersThemeSpec spec, int seed)
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

        /// <summary>The launcher icon: two stacked discs, light over dark, on a rounded board tile.</summary>
        public static Texture2D GameIcon(int size, CheckersThemeSpec spec)
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
            float radius = size * 0.26f;
            var darkCenter = new Vector2(center.x + size * 0.06f, center.y - size * 0.1f);
            var lightCenter = new Vector2(center.x - size * 0.06f, center.y + size * 0.1f);
            raster.Shadow(p => Sd.Circle(p, darkCenter, radius), new Color(0f, 0f, 0f, 0.5f), size * 0.03f, new Vector2(0f, -size * 0.02f), bounds);
            Disc(raster, darkCenter, radius, spec.DarkPiece, spec.Emissive ? spec.DarkColor : Lighten(spec.DarkPiece, 0.25f), bounds);
            raster.Shadow(p => Sd.Circle(p, lightCenter, radius), new Color(0f, 0f, 0f, 0.5f), size * 0.03f, new Vector2(0f, -size * 0.02f), bounds);
            Disc(raster, lightCenter, radius, spec.LightPiece, spec.Emissive ? spec.LightColor : Lighten(spec.LightPiece, 0.25f), bounds);
            raster.Fill(p => Sd.Ring(p, lightCenter, radius * 0.62f, size * 0.012f), spec.LightCrown, bounds);
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
