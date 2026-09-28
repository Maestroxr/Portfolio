using System;
using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.EndlessRunner.EditorTools
{
    /// <summary>
    /// Procedural textures of the Endless Runner: tileable ground and road surfaces, water and lava, curb stripes,
    /// particle sprites and the interface icons. Icons are drawn from signed distance functions with an outline, a
    /// vertical gradient and a highlight, then written as PNG files by the art builder.
    /// </summary>
    internal static class TextureFactory
    {
        // ------------------------------------------------------------------ noise

        private static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 144269504;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (float)0xFFFFFF;
            }
        }

        /// <summary>Value noise that tiles every <paramref name="period"/> cells.</summary>
        private static float TileNoise(float x, float y, int period, int seed)
        {
            int x0 = Mathf.FloorToInt(x);
            int y0 = Mathf.FloorToInt(y);
            float fx = x - x0;
            float fy = y - y0;
            float u = fx * fx * (3f - 2f * fx);
            float v = fy * fy * (3f - 2f * fy);
            float a = Hash(Mod(x0, period), Mod(y0, period), seed);
            float b = Hash(Mod(x0 + 1, period), Mod(y0, period), seed);
            float c = Hash(Mod(x0, period), Mod(y0 + 1, period), seed);
            float d = Hash(Mod(x0 + 1, period), Mod(y0 + 1, period), seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
        }

        /// <summary>Tileable fractal noise over a square texture of <paramref name="size"/> pixels, in 0..1.</summary>
        private static float Fbm(int px, int py, int size, int baseCells, int octaves, int seed)
        {
            float value = 0f;
            float amplitude = 0.5f;
            float total = 0f;
            int cells = baseCells;
            for (int o = 0; o < octaves; o++)
            {
                value += amplitude * TileNoise(px * cells / (float)size, py * cells / (float)size, cells, seed + o * 31);
                total += amplitude;
                amplitude *= 0.5f;
                cells *= 2;
            }
            return value / total;
        }

        private static int Mod(int value, int period)
        {
            int result = value % period;
            return result < 0 ? result + period : result;
        }

        private static Texture2D Make(int size, Func<int, int, Color> pixel)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    pixels[y * size + x] = pixel(x, y);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        // ------------------------------------------------------------------ surfaces

        public static Texture2D Palette(bool emission)
        {
            var texture = new Texture2D(EditorTools.Palette.Size, EditorTools.Palette.Size, TextureFormat.RGBA32, false);
            texture.SetPixels(EditorTools.Palette.Pixels(emission));
            texture.Apply();
            return texture;
        }

        /// <summary>Grey asphalt/stone: tinted per world by the road material.</summary>
        public static Texture2D Asphalt()
        {
            const int size = 256;
            return Make(size, (x, y) =>
            {
                float n = Fbm(x, y, size, 8, 4, 11);
                float speckle = Hash(x, y, 3) > 0.93f ? 0.12f : Hash(x, y, 5) < 0.05f ? -0.1f : 0f;
                float value = 0.78f + (n - 0.5f) * 0.28f + speckle;
                return new Color(value, value, value, 1f);
            });
        }

        public static Texture2D Sand()
        {
            const int size = 256;
            return Make(size, (x, y) =>
            {
                float n = Fbm(x, y, size, 4, 4, 21);
                float ripple = Mathf.Sin((x + y * 0.35f) / size * Mathf.PI * 2f * 9f + n * 6f) * 0.5f + 0.5f;
                float speckle = Hash(x, y, 7) > 0.96f ? -0.12f : 0f;
                float value = 0.86f + (n - 0.5f) * 0.18f + ripple * 0.08f + speckle;
                return new Color(value, value * 0.97f, value * 0.93f, 1f);
            });
        }

        public static Texture2D Snow()
        {
            const int size = 256;
            return Make(size, (x, y) =>
            {
                float n = Fbm(x, y, size, 6, 4, 31);
                float sparkle = Hash(x, y, 13) > 0.985f ? 0.12f : 0f;
                float value = 0.9f + (n - 0.5f) * 0.14f + sparkle;
                return new Color(value * 0.95f, value * 0.98f, Mathf.Min(1f, value * 1.04f), 1f);
            });
        }

        /// <summary>Dark volcanic ash crossed by cracks; the emission texture lights the cracks up.</summary>
        public static Texture2D Ash(bool emission)
        {
            const int size = 256;
            const int cells = 6;
            return Make(size, (x, y) =>
            {
                float edge = CellEdge(x, y, size, cells, 41);
                float n = Fbm(x, y, size, 8, 3, 43);
                float crack = Mathf.Clamp01(1f - edge / 0.035f) * Mathf.Clamp01((n - 0.3f) * 3f);
                if (emission)
                {
                    return new Color(1f, 0.35f, 0.05f, 1f) * crack;
                }
                float value = 0.55f + (n - 0.5f) * 0.35f;
                Color rock = new Color(value, value * 0.94f, value * 0.95f, 1f);
                return Color.Lerp(rock, new Color(1f, 0.45f, 0.1f, 1f), crack);
            });
        }

        /// <summary>Distance to the nearest Voronoi cell border (tileable), in cell units.</summary>
        private static float CellEdge(int px, int py, int size, int cells, int seed)
        {
            float x = px * cells / (float)size;
            float y = py * cells / (float)size;
            int cx = Mathf.FloorToInt(x);
            int cy = Mathf.FloorToInt(y);
            float first = float.MaxValue;
            float second = float.MaxValue;
            for (int oy = -1; oy <= 1; oy++)
            {
                for (int ox = -1; ox <= 1; ox++)
                {
                    int gx = cx + ox;
                    int gy = cy + oy;
                    float fx = gx + Hash(Mod(gx, cells), Mod(gy, cells), seed);
                    float fy = gy + Hash(Mod(gx, cells), Mod(gy, cells), seed + 1);
                    float distance = Mathf.Sqrt((fx - x) * (fx - x) + (fy - y) * (fy - y));
                    if (distance < first)
                    {
                        second = first;
                        first = distance;
                    }
                    else if (distance < second)
                    {
                        second = distance;
                    }
                }
            }
            return (second - first) * 0.5f;
        }

        public static Texture2D Water()
        {
            const int size = 128;
            return Make(size, (x, y) =>
            {
                float n = Fbm(x, y, size, 4, 3, 51);
                float caustic = Mathf.Pow(1f - Mathf.Abs(Mathf.Sin((n * 3f + x / (float)size) * Mathf.PI * 2f)), 6f);
                float value = 0.8f + n * 0.25f + caustic * 0.35f;
                return new Color(value * 0.9f, value, Mathf.Min(1f, value * 1.05f), 1f);
            });
        }

        public static Texture2D Lava()
        {
            const int size = 128;
            return Make(size, (x, y) =>
            {
                float n = Fbm(x, y, size, 4, 4, 61);
                float hot = Mathf.Clamp01((n - 0.35f) * 2.2f);
                Color dark = new Color(0.55f, 0.08f, 0.02f);
                Color mid = new Color(1f, 0.35f, 0.05f);
                Color bright = new Color(1f, 0.85f, 0.3f);
                return hot < 0.6f ? Color.Lerp(dark, mid, hot / 0.6f) : Color.Lerp(mid, bright, (hot - 0.6f) / 0.4f);
            });
        }

        /// <summary>Two-colour curb stripes along u.</summary>
        public static Texture2D Stripes(Color first, Color second)
        {
            var texture = new Texture2D(16, 4, TextureFormat.RGBA32, false);
            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    texture.SetPixel(x, y, x < 8 ? first : second);
                }
            }
            texture.Apply();
            return texture;
        }

        // ------------------------------------------------------------------ particles

        public static Texture2D SoftDot()
        {
            const int size = 64;
            return Make(size, (x, y) =>
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size / 2f, size / 2f)) / (size / 2f);
                float a = Mathf.Clamp01(1f - d);
                return new Color(1f, 1f, 1f, a * a * (3f - 2f * a));
            });
        }

        public static Texture2D Sparkle()
        {
            const int size = 64;
            return Make(size, (x, y) =>
            {
                float px = (x + 0.5f) / size * 2f - 1f;
                float py = (y + 0.5f) / size * 2f - 1f;
                float star = Mathf.Max(Mathf.Clamp01(1f - Mathf.Abs(px) * 7f - Mathf.Abs(py) * 1.1f), Mathf.Clamp01(1f - Mathf.Abs(py) * 7f - Mathf.Abs(px) * 1.1f));
                float glow = Mathf.Clamp01(1f - Mathf.Sqrt(px * px + py * py) * 1.6f);
                float a = Mathf.Clamp01(star + glow * glow * 0.6f);
                return new Color(1f, 1f, 1f, a);
            });
        }

        public static Texture2D Smoke()
        {
            const int size = 64;
            return Make(size, (x, y) =>
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size / 2f, size / 2f)) / (size / 2f);
                float n = Fbm(x, y, size, 4, 3, 71);
                float a = Mathf.Clamp01(1f - d * (1.1f + (0.5f - n) * 0.8f));
                return new Color(1f, 1f, 1f, a * a);
            });
        }

        public static Texture2D Ring()
        {
            const int size = 128;
            return Make(size, (x, y) =>
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size / 2f, size / 2f)) / (size / 2f);
                float rim = Mathf.Clamp01(1f - Mathf.Abs(d - 0.86f) / 0.12f);
                float inner = Mathf.Clamp01(1f - d) * 0.18f;
                float a = d > 1f ? 0f : Mathf.Clamp01(rim * rim + inner);
                return new Color(1f, 1f, 1f, a);
            });
        }

        public static Texture2D Square()
        {
            return Make(8, (x, y) => Color.white);
        }

        /// <summary>A soft vertical streak: a drop of rain, drawn as a stretched particle.</summary>
        public static Texture2D RainStreak()
        {
            var texture = new Texture2D(16, 64, TextureFormat.RGBA32, false);
            for (int y = 0; y < 64; y++)
            {
                float v = (y + 0.5f) / 64f;
                float along = Mathf.Sin(v * Mathf.PI);
                for (int x = 0; x < 16; x++)
                {
                    float u = Mathf.Abs((x + 0.5f) / 16f * 2f - 1f);
                    float a = Mathf.Clamp01(1f - u * 1.6f) * along;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
            }
            texture.Apply();
            return texture;
        }

        // ------------------------------------------------------------------ the city at night

        /// <summary>Dark asphalt with the sheen of puddles: the wet streets of the night city.</summary>
        public static Texture2D WetAsphalt()
        {
            const int size = 256;
            return Make(size, (x, y) =>
            {
                float n = Fbm(x, y, size, 8, 4, 111);
                float puddles = Mathf.Clamp01((Fbm(x, y, size, 3, 3, 113) - 0.56f) * 6f);
                float speckle = Hash(x, y, 115) > 0.95f ? 0.1f : 0f;
                float value = 0.62f + (n - 0.5f) * 0.3f + speckle;
                value = Mathf.Lerp(value, 0.95f, puddles * 0.5f);
                return new Color(value * 0.96f, value * 0.98f, value, 1f);
            });
        }

        /// <summary>Grey concrete slabs with seams and stains.</summary>
        public static Texture2D Concrete()
        {
            const int size = 256;
            return Make(size, (x, y) =>
            {
                float n = Fbm(x, y, size, 6, 4, 121);
                float stain = Fbm(x, y, size, 2, 2, 123);
                int slab = size / 2;
                bool seam = x % slab < 3 || y % slab < 3;
                float value = 0.8f + (n - 0.5f) * 0.22f - (stain - 0.5f) * 0.18f;
                if (seam)
                {
                    value *= 0.72f;
                }
                return new Color(value, value, value * 0.98f, 1f);
            });
        }

        /// <summary>Roof gravel: a fine, dark, speckled grit.</summary>
        public static Texture2D Gravel()
        {
            const int size = 256;
            return Make(size, (x, y) =>
            {
                float n = Fbm(x, y, size, 32, 3, 131);
                float coarse = Fbm(x, y, size, 4, 2, 133);
                float speckle = Hash(x, y, 135) > 0.9f ? 0.18f : Hash(x, y, 137) < 0.08f ? -0.14f : 0f;
                float value = 0.74f + (n - 0.5f) * 0.4f + (coarse - 0.5f) * 0.12f + speckle;
                return new Color(value, value * 0.98f, value * 0.95f, 1f);
            });
        }

        /// <summary>Glazed wall tiles with dark grout, a few of them chipped: the subway.</summary>
        public static Texture2D Tiles()
        {
            const int size = 256;
            const int cell = 32;
            return Make(size, (x, y) =>
            {
                int row = y / cell;
                int shifted = x + (row % 2 == 0 ? 0 : cell / 2);
                int column = shifted / cell;
                bool grout = shifted % cell < 3 || y % cell < 3;
                float n = Fbm(x, y, size, 8, 3, 141);
                float tile = 0.86f + (Hash(Mod(column, size / cell), row, 143) - 0.5f) * 0.16f + (n - 0.5f) * 0.08f;
                float value = grout ? 0.32f : tile;
                return new Color(value, value * 0.99f, value * 0.96f, 1f);
            });
        }

        /// <summary>Black with scattered warm and cool points of light: a street seen from far above.</summary>
        public static Texture2D CityLights()
        {
            const int size = 128;
            return Make(size, (x, y) =>
            {
                float h = Hash(x / 3, y / 3, 151);
                if (h < 0.965f)
                {
                    return new Color(0.02f, 0.02f, 0.03f, 1f);
                }
                float warm = Hash(x / 3, y / 3, 153);
                float bright = 0.6f + Hash(x, y, 155) * 0.4f;
                return warm < 0.7f ? new Color(1f, 0.8f, 0.45f, 1f) * bright : new Color(0.7f, 0.85f, 1f, 1f) * bright;
            });
        }

        /// <summary>Film grain: white noise in the alpha, tiled over the screen in the theme's grain colour.</summary>
        public static Texture2D Grain()
        {
            const int size = 128;
            return Make(size, (x, y) =>
            {
                float a = Hash(x, y, 161) * 0.7f + Hash(x, y, 163) * 0.3f;
                return new Color(1f, 1f, 1f, a);
            });
        }

        /// <summary>A white picture that darkens toward the edges: stretched behind the pause menu and tinted by the theme.</summary>
        public static Texture2D Backdrop()
        {
            const int size = 128;
            return Make(size, (x, y) =>
            {
                float px = (x + 0.5f) / size * 2f - 1f;
                float py = (y + 0.5f) / size * 2f - 1f;
                float d = Mathf.Sqrt(px * px * 0.7f + py * py * 0.9f);
                float shade = Mathf.Lerp(0.72f, 1f, Mathf.Clamp01(d));
                return new Color(shade, shade, shade, Mathf.Lerp(0.82f, 1f, Mathf.Clamp01(d)));
            });
        }

        // ------------------------------------------------------------------ icons

        /// <summary>One shape of an icon: where it is (signed distance, negative inside) and how it is painted.</summary>
        private sealed class Layer
        {
            public Func<Vector2, float> Shape;
            public Color Top = Color.white;
            public Color Bottom = Color.white;
            public Color Outline = new Color(0f, 0f, 0f, 0f);
            public float OutlineWidth;
            public float Highlight;
            public Func<Vector2, Color> Paint;
        }

        private static Layer L(Func<Vector2, float> shape, Color top, Color bottom, Color outline, float outlineWidth = 0.07f, float highlight = 0.35f)
        {
            return new Layer { Shape = shape, Top = top, Bottom = bottom, Outline = outline, OutlineWidth = outlineWidth, Highlight = highlight };
        }

        /// <summary>Draws layers over each other in a -1..1 square, anti-aliased by the pixel size.</summary>
        private static Texture2D Draw(int size, params Layer[] layers)
        {
            float pixel = 2f / size;
            return Make(size, (x, y) =>
            {
                var p = new Vector2((x + 0.5f) / size * 2f - 1f, (y + 0.5f) / size * 2f - 1f);
                Color result = new Color(0f, 0f, 0f, 0f);
                foreach (Layer layer in layers)
                {
                    float d = layer.Shape(p);
                    float coverage = Mathf.Clamp01(0.5f - d / pixel);
                    if (coverage <= 0f)
                    {
                        continue;
                    }
                    Color fill = layer.Paint != null ? layer.Paint(p) : Color.Lerp(layer.Bottom, layer.Top, (p.y + 1f) * 0.5f);
                    if (layer.Highlight > 0f)
                    {
                        float glint = Mathf.Clamp01(1f - Vector2.Distance(p, new Vector2(-0.3f, 0.45f)) / 0.55f);
                        fill = Color.Lerp(fill, Color.white, glint * glint * layer.Highlight);
                    }
                    if (layer.OutlineWidth > 0f && layer.Outline.a > 0f)
                    {
                        float edge = Mathf.Clamp01(0.5f + (d + layer.OutlineWidth) / pixel);
                        fill = Color.Lerp(fill, layer.Outline, edge);
                    }
                    fill.a *= coverage;
                    result = Over(fill, result);
                }
                return result;
            });
        }

        private static Color Over(Color top, Color bottom)
        {
            float a = top.a + bottom.a * (1f - top.a);
            if (a <= 0f)
            {
                return new Color(0f, 0f, 0f, 0f);
            }
            Color color = (top * top.a + bottom * bottom.a * (1f - top.a)) / a;
            color.a = a;
            return color;
        }

        private static float Circle(Vector2 p, Vector2 center, float radius)
        {
            return Vector2.Distance(p, center) - radius;
        }

        private static float Box(Vector2 p, Vector2 center, Vector2 half, float radius = 0f)
        {
            Vector2 q = new Vector2(Mathf.Abs(p.x - center.x), Mathf.Abs(p.y - center.y)) - half + Vector2.one * radius;
            return new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
        }

        private static float Segment(Vector2 p, Vector2 a, Vector2 b, float radius)
        {
            Vector2 pa = p - a;
            Vector2 ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude - radius;
        }

        private static float Polygon(Vector2 p, IList<Vector2> points)
        {
            float distance = float.MaxValue;
            bool inside = false;
            for (int i = 0, j = points.Count - 1; i < points.Count; j = i++)
            {
                Vector2 a = points[j];
                Vector2 b = points[i];
                distance = Mathf.Min(distance, Segment(p, a, b, 0f));
                if ((b.y > p.y) != (a.y > p.y) && p.x < (a.x - b.x) * (p.y - b.y) / (a.y - b.y) + b.x)
                {
                    inside = !inside;
                }
            }
            return inside ? -distance : distance;
        }

        private static float Heart(Vector2 p)
        {
            p = new Vector2(Mathf.Abs(p.x), p.y + 0.62f) / 1.25f;
            float d;
            if (p.y + p.x > 1f)
            {
                d = (p - new Vector2(0.25f, 0.75f)).magnitude - Mathf.Sqrt(2f) / 4f;
            }
            else
            {
                float a = (p - new Vector2(0f, 1f)).sqrMagnitude;
                float b = (p - 0.5f * Mathf.Max(p.x + p.y, 0f) * Vector2.one).sqrMagnitude;
                d = Mathf.Sqrt(Mathf.Min(a, b)) * Mathf.Sign(p.x - p.y);
            }
            return d * 1.25f;
        }

        private static Vector2[] Star(float outer, float inner, Vector2 center)
        {
            Vector2[] outline = MeshBuilder.StarOutline(5, outer, inner);
            for (int i = 0; i < outline.Length; i++)
            {
                outline[i] += center;
            }
            return outline;
        }

        /// <summary>
        /// The colours an icon set is drawn with: every icon takes its colours from one of these roles, so a theme
        /// gets its own set by changing the roles (the classic set is the default).
        /// </summary>
        internal sealed class IconStyle
        {
            public Color Ink = new Color(0.12f, 0.1f, 0.18f, 1f);
            /// <summary>How strong the glint in the upper left of the shapes is; 0 draws them flat.</summary>
            public float Gloss = 1f;
            public Color Gold = new Color(1f, 0.87f, 0.3f);
            public Color GoldDark = new Color(0.93f, 0.6f, 0.08f);
            public Color GoldOutline = new Color(0.5f, 0.3f, 0.02f);
            public Color GoldCore = new Color(1f, 0.95f, 0.6f);
            /// <summary>A hexagon socket in the middle of the coin instead of the star: a credit chip.</summary>
            public bool ChipCoin;
            public Color Gem = new Color(1f, 0.55f, 0.95f);
            public Color GemDark = new Color(0.6f, 0.2f, 0.85f);
            public Color GemOutline = new Color(0.3f, 0.05f, 0.4f);
            public Color GemFacet = new Color(1f, 0.85f, 1f);
            public Color GemFacetDark = new Color(1f, 0.7f, 1f);
            public Color Heart = new Color(1f, 0.42f, 0.45f);
            public Color HeartDark = new Color(0.85f, 0.1f, 0.2f);
            public Color HeartOutline = new Color(0.4f, 0.02f, 0.08f);
            public Color Empty = new Color(0.25f, 0.25f, 0.35f, 0.7f);
            public Color EmptyDark = new Color(0.15f, 0.15f, 0.22f, 0.7f);
            public Color EmptyOutline = new Color(0.75f, 0.75f, 0.85f);
            public Color StarEmpty = new Color(0.3f, 0.3f, 0.4f, 0.75f);
            public Color StarEmptyDark = new Color(0.18f, 0.18f, 0.26f, 0.75f);
            public Color StarEmptyOutline = new Color(0.7f, 0.72f, 0.8f);
            public Color Star = new Color(1f, 0.92f, 0.35f);
            public Color StarDark = new Color(1f, 0.62f, 0.05f);
            public Color StarOutline = new Color(0.55f, 0.3f, 0.02f);
            public Color Metal = new Color(0.85f, 0.87f, 0.92f);
            public Color MetalDark = new Color(0.6f, 0.62f, 0.7f);
            public Color Lock = new Color(1f, 0.82f, 0.3f);
            public Color LockDark = new Color(0.9f, 0.55f, 0.1f);
            public Color Magnet = new Color(1f, 0.4f, 0.4f);
            public Color MagnetDark = new Color(0.8f, 0.1f, 0.12f);
            public Color MagnetTip = Color.white;
            public Color MagnetTipDark = new Color(0.75f, 0.78f, 0.85f);
            public Color Shield = new Color(0.45f, 0.75f, 1f);
            public Color ShieldDark = new Color(0.15f, 0.4f, 0.9f);
            public Color ShieldOutline = new Color(0.95f, 0.7f, 0.15f);
            public Color ShieldStar = Color.white;
            public Color ShieldStarDark = new Color(0.9f, 0.95f, 1f);
            public Color Multiplier = new Color(0.8f, 0.5f, 1f);
            public Color MultiplierDark = new Color(0.5f, 0.2f, 0.85f);
            public Color MultiplierOutline = new Color(0.25f, 0.05f, 0.4f);
            public Color MultiplierStar = new Color(1f, 0.95f, 0.5f);
            public Color Spring = new Color(0.6f, 1f, 0.6f);
            public Color SpringDark = new Color(0.2f, 0.75f, 0.3f);
            public Color SpringSole = new Color(0.4f, 1f, 0.5f);
            public Color SpringSoleDark = new Color(0.2f, 0.8f, 0.3f);
            public Color SpringShoe = Color.white;
            public Color SpringShoeDark = new Color(0.85f, 0.88f, 0.9f);
            /// <summary>The light shapes: play, retry, levels, settings, exit, infinity, the shoe of the spring.</summary>
            public Color Paper = Color.white;
            public Color PaperDark = new Color(0.88f, 0.92f, 1f);
            /// <summary>The round pause button and the infinity sign, a touch greyer than the paper.</summary>
            public Color PauseDark = new Color(0.85f, 0.88f, 0.95f);
            public Color InfinityDark = new Color(0.85f, 0.9f, 1f);
            public Color Pole = new Color(0.8f, 0.8f, 0.85f);
            public Color PoleDark = new Color(0.6f, 0.6f, 0.7f);
            public Color Skin = new Color(1f, 0.85f, 0.65f);
            public Color SkinDark = new Color(0.95f, 0.72f, 0.5f);
            public Color Cap = new Color(1f, 0.4f, 0.35f);
            public Color CapDark = new Color(0.85f, 0.2f, 0.18f);
            /// <summary>The runner icon wears a hood instead of the cap.</summary>
            public bool Hooded;
            public Color Check = new Color(0.6f, 1f, 0.5f);
            public Color CheckDark = new Color(0.3f, 0.8f, 0.25f);

            public static IconStyle Classic => new IconStyle();
        }

        public static Texture2D Icon(string name)
        {
            return Icon(name, IconStyle.Classic);
        }

        public static Texture2D Icon(string name, IconStyle style)
        {
            const int size = 128;
            Color ink = style.Ink;
            float gloss = style.Gloss;
            switch (name)
            {
                case "Coin":
                    return Draw(size,
                        L(p => Circle(p, Vector2.zero, 0.9f), style.Gold, style.GoldDark, style.GoldOutline, 0.09f, 0.35f * gloss),
                        L(p => Mathf.Abs(Circle(p, Vector2.zero, 0.62f)) - 0.05f, style.GoldDark, style.GoldDark, Color.clear, 0f, 0f),
                        style.ChipCoin
                            ? L(p => Polygon(p, Hexagon(0.34f)), style.GoldCore, style.GoldCore, style.GoldOutline, 0.06f, 0f)
                            : L(p => Polygon(p, Star(0.42f, 0.18f, Vector2.zero)), style.GoldCore, style.Gold, Color.clear, 0f, 0.2f * gloss));
                case "Gem":
                    return Draw(size,
                        L(p => Polygon(p, new[] { new Vector2(0f, -0.88f), new Vector2(0.82f, 0.12f), new Vector2(0.5f, 0.62f), new Vector2(-0.5f, 0.62f), new Vector2(-0.82f, 0.12f) }),
                            style.Gem, style.GemDark, style.GemOutline, 0.08f, 0.35f * gloss),
                        L(p => Polygon(p, new[] { new Vector2(-0.5f, 0.62f), new Vector2(0.5f, 0.62f), new Vector2(0.25f, 0.12f), new Vector2(-0.25f, 0.12f) }),
                            style.GemFacet, style.GemFacetDark, Color.clear, 0f, 0f));
                case "Heart":
                    return Draw(size, L(Heart, style.Heart, style.HeartDark, style.HeartOutline, 0.09f, 0.5f * gloss));
                case "HeartEmpty":
                    return Draw(size, L(Heart, style.Empty, style.EmptyDark, style.EmptyOutline, 0.09f, 0f));
                case "Star":
                    return Draw(size, L(p => Polygon(p, Star(0.95f, 0.42f, new Vector2(0f, -0.04f))), style.Star, style.StarDark, style.StarOutline, 0.08f, 0.45f * gloss));
                case "StarEmpty":
                    return Draw(size, L(p => Polygon(p, Star(0.95f, 0.42f, new Vector2(0f, -0.04f))), style.StarEmpty, style.StarEmptyDark, style.StarEmptyOutline, 0.08f, 0f));
                case "Lock":
                    return Draw(size,
                        L(p => Mathf.Max(Mathf.Abs(Circle(p, new Vector2(0f, 0.2f), 0.38f)) - 0.1f, -p.y + 0.15f), style.Metal, style.MetalDark, ink, 0.07f, 0f),
                        L(p => Box(p, new Vector2(0f, -0.3f), new Vector2(0.62f, 0.48f), 0.12f), style.Lock, style.LockDark, ink, 0.08f, 0.35f * gloss),
                        L(p => Mathf.Min(Circle(p, new Vector2(0f, -0.22f), 0.12f), Box(p, new Vector2(0f, -0.42f), new Vector2(0.05f, 0.18f))), ink, ink, Color.clear, 0f, 0f));
                case "Magnet":
                    return Draw(size,
                        L(p => Mathf.Min(Mathf.Max(Mathf.Abs(Circle(p, new Vector2(0f, -0.05f), 0.5f)) - 0.2f, p.y + 0.05f),
                                Mathf.Min(Box(p, new Vector2(-0.5f, 0.3f), new Vector2(0.2f, 0.35f)), Box(p, new Vector2(0.5f, 0.3f), new Vector2(0.2f, 0.35f)))),
                            style.Magnet, style.MagnetDark, ink, 0.08f, 0.35f * gloss),
                        L(p => Mathf.Min(Box(p, new Vector2(-0.5f, 0.55f), new Vector2(0.2f, 0.14f)), Box(p, new Vector2(0.5f, 0.55f), new Vector2(0.2f, 0.14f))),
                            style.MagnetTip, style.MagnetTipDark, ink, 0.08f, 0f));
                case "Shield":
                    return Draw(size,
                        L(p => Polygon(p, new[]
                        {
                            new Vector2(-0.75f, 0.75f), new Vector2(0.75f, 0.75f), new Vector2(0.75f, 0.05f), new Vector2(0.55f, -0.4f),
                            new Vector2(0.3f, -0.7f), new Vector2(0f, -0.92f), new Vector2(-0.3f, -0.7f), new Vector2(-0.55f, -0.4f), new Vector2(-0.75f, 0.05f)
                        }), style.Shield, style.ShieldDark, style.ShieldOutline, 0.12f, 0.35f * gloss),
                        L(p => Polygon(p, Star(0.38f, 0.16f, new Vector2(0f, 0.05f))), style.ShieldStar, style.ShieldStarDark, Color.clear, 0f, 0f));
                case "Multiplier":
                    return Draw(size,
                        L(p => Circle(p, Vector2.zero, 0.9f), style.Multiplier, style.MultiplierDark, style.MultiplierOutline, 0.09f, 0.35f * gloss),
                        L(p => Polygon(p, Star(0.6f, 0.27f, new Vector2(0f, -0.03f))), style.MultiplierStar, style.Gold, Color.clear, 0f, 0.3f * gloss));
                case "Spring":
                {
                    return Draw(size,
                        L(p =>
                        {
                            float d = float.MaxValue;
                            for (int i = 0; i < 4; i++)
                            {
                                float y = -0.85f + i * 0.28f;
                                d = Mathf.Min(d, Segment(p, new Vector2(-0.35f, y), new Vector2(0.35f, y + 0.14f), 0.08f));
                                d = Mathf.Min(d, Segment(p, new Vector2(0.35f, y + 0.14f), new Vector2(-0.35f, y + 0.28f), 0.08f));
                            }
                            return d;
                        }, style.Spring, style.SpringDark, ink, 0.07f, 0f),
                        L(p => Box(p, new Vector2(0.05f, 0.5f), new Vector2(0.62f, 0.26f), 0.2f), style.SpringShoe, style.SpringShoeDark, ink, 0.08f, 0.35f * gloss),
                        L(p => Box(p, new Vector2(0.05f, 0.28f), new Vector2(0.62f, 0.07f), 0.05f), style.SpringSole, style.SpringSoleDark, ink, 0.05f, 0f));
                }
                case "Pause":
                    return Draw(size,
                        L(p => Circle(p, Vector2.zero, 0.92f), Alpha(style.Paper, 0.95f), Alpha(style.PauseDark, 0.95f), ink, 0.07f, 0f),
                        L(p => Mathf.Min(Box(p, new Vector2(-0.22f, 0f), new Vector2(0.12f, 0.42f), 0.05f), Box(p, new Vector2(0.22f, 0f), new Vector2(0.12f, 0.42f), 0.05f)), ink, ink, Color.clear, 0f, 0f));
                case "Flag":
                {
                    var flag = L(p => Box(p, new Vector2(0.15f, 0.35f), new Vector2(0.62f, 0.42f), 0.04f), style.Paper, style.Paper, ink, 0.07f, 0f);
                    flag.Paint = p => (Mathf.FloorToInt((p.x + 2f) / 0.21f) + Mathf.FloorToInt((p.y + 2f) / 0.21f)) % 2 == 0 ? style.Paper : ink;
                    return Draw(size, L(p => Segment(p, new Vector2(-0.52f, -0.9f), new Vector2(-0.52f, 0.85f), 0.07f), style.Pole, style.PoleDark, ink, 0.05f, 0f), flag);
                }
                case "Runner":
                    return style.Hooded
                        ? Draw(size,
                            L(p => Mathf.Max(Circle(p, new Vector2(0f, -0.02f), 0.95f), -(p.y + 0.62f)), style.Cap, style.CapDark, ink, 0.08f, 0.15f * gloss),
                            L(p => Circle(p, new Vector2(0f, -0.1f), 0.6f), style.Skin, style.SkinDark, ink, 0.07f, 0.2f * gloss),
                            L(p => Mathf.Max(Circle(p, new Vector2(0f, -0.02f), 0.95f), -Circle(p, new Vector2(0f, -0.05f), 0.66f), -(p.y + 0.62f), p.y - 0.95f), style.Cap, style.CapDark, Color.clear, 0f, 0f),
                            L(p => Mathf.Min(Circle(p, new Vector2(-0.2f, -0.1f), 0.08f), Circle(p, new Vector2(0.2f, -0.1f), 0.08f)), ink, ink, Color.clear, 0f, 0f))
                        : Draw(size,
                            L(p => Circle(p, new Vector2(0f, -0.08f), 0.78f), style.Skin, style.SkinDark, ink, 0.08f, 0.2f * gloss),
                            L(p => Mathf.Max(Circle(p, new Vector2(0f, -0.02f), 0.84f), -p.y + 0.12f), style.Cap, style.CapDark, ink, 0.08f, 0.35f * gloss),
                            L(p => Mathf.Min(Circle(p, new Vector2(-0.26f, -0.12f), 0.1f), Circle(p, new Vector2(0.26f, -0.12f), 0.1f)), ink, ink, Color.clear, 0f, 0f));
                case "Infinity":
                    return Draw(size, L(p => Mathf.Min(Mathf.Abs(Circle(p, new Vector2(-0.3f, 0f), 0.34f)), Mathf.Abs(Circle(p, new Vector2(0.3f, 0f), 0.34f))) - 0.1f,
                        style.Paper, style.InfinityDark, ink, 0.07f, 0f));
                case "Play":
                    return Draw(size, L(p => Polygon(p, new[] { new Vector2(-0.45f, -0.7f), new Vector2(0.7f, 0f), new Vector2(-0.45f, 0.7f) }), style.Paper, style.PaperDark, Color.clear, 0f, 0f));
                case "Retry":
                    return Draw(size,
                        L(p => Mathf.Min(Mathf.Max(Mathf.Abs(Circle(p, new Vector2(0f, -0.08f), 0.55f)) - 0.13f, Mathf.Min(p.x, p.y + 0.08f)),
                            Polygon(p, new[] { new Vector2(0.24f, -0.02f), new Vector2(0.86f, -0.02f), new Vector2(0.55f, 0.42f) })), style.Paper, style.PaperDark, Color.clear, 0f, 0f));
                case "Levels":
                    return Draw(size, L(p =>
                    {
                        float d = float.MaxValue;
                        for (int i = 0; i < 4; i++)
                        {
                            d = Mathf.Min(d, Box(p, new Vector2(i % 2 == 0 ? -0.36f : 0.36f, i < 2 ? -0.36f : 0.36f), new Vector2(0.28f, 0.28f), 0.08f));
                        }
                        return d;
                    }, style.Paper, style.PaperDark, Color.clear, 0f, 0f));
                case "Settings":
                    return Draw(size, L(p =>
                    {
                        float angle = Mathf.Atan2(p.y, p.x);
                        float teeth = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(Mathf.Cos(angle * 8f) * 3f + 0.5f)) * 0.18f;
                        return Mathf.Max(p.magnitude - (0.62f + teeth), -(p.magnitude - 0.25f));
                    }, style.Paper, style.PaperDark, Color.clear, 0f, 0f));
                case "Exit":
                    return Draw(size,
                        L(p => Mathf.Max(Box(p, new Vector2(-0.25f, 0f), new Vector2(0.5f, 0.75f), 0.1f), -Box(p, new Vector2(-0.25f, 0f), new Vector2(0.34f, 0.59f), 0.05f)),
                            style.Paper, style.PaperDark, Color.clear, 0f, 0f),
                        L(p => Mathf.Min(Segment(p, new Vector2(0f, 0f), new Vector2(0.75f, 0f), 0.09f),
                            Polygon(p, new[] { new Vector2(0.5f, 0.3f), new Vector2(0.92f, 0f), new Vector2(0.5f, -0.3f) })), style.Paper, style.Paper, Color.clear, 0f, 0f));
                case "Check":
                    return Draw(size, L(p => Mathf.Min(Segment(p, new Vector2(-0.55f, 0f), new Vector2(-0.15f, -0.45f), 0.14f), Segment(p, new Vector2(-0.15f, -0.45f), new Vector2(0.6f, 0.5f), 0.14f)),
                        style.Check, style.CheckDark, ink, 0.06f, 0f));
                default:
                    throw new ArgumentException($"Unknown icon {name}");
            }
        }

        private static Color Alpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private static Vector2[] Hexagon(float radius)
        {
            var outline = new Vector2[6];
            for (int i = 0; i < 6; i++)
            {
                float angle = i * Mathf.PI / 3f;
                outline[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }
            return outline;
        }

        public static readonly string[] IconNames =
        {
            "Coin", "Gem", "Heart", "HeartEmpty", "Star", "StarEmpty", "Lock", "Magnet", "Shield", "Multiplier", "Spring",
            "Pause", "Flag", "Runner", "Infinity", "Play", "Retry", "Levels", "Settings", "Exit", "Check"
        };

        /// <summary>The shape of a theme's panels and buttons: how round the corners are, and whether they look like glass.</summary>
        internal sealed class PanelStyle
        {
            public float Radius = 22f;
            /// <summary>A thin light edge along the top and a darker foot, like a pane of dark glass, instead of the cartoon lip.</summary>
            public bool Glass;

            public static PanelStyle Classic => new PanelStyle();
        }

        /// <summary>A white rounded rectangle for 9-sliced panels and buttons; buttons get a darker lower lip.</summary>
        public static Texture2D RoundedPanel(bool button)
        {
            return RoundedPanel(button, PanelStyle.Classic);
        }

        /// <summary>
        /// A white rounded rectangle for 9-sliced panels and buttons in the given style. <paramref name="shade"/> lightens
        /// (above 1) or darkens the whole shape: the hover and pressed states of a button.
        /// </summary>
        public static Texture2D RoundedPanel(bool button, PanelStyle style, float shade = 1f)
        {
            const int size = 64;
            float radius = style.Radius;
            return Make(size, (x, y) =>
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                float d = Box(p, new Vector2(size / 2f, size / 2f), new Vector2(size / 2f - 1f, size / 2f - 1f), radius);
                float a = Mathf.Clamp01(0.5f - d);
                float value = 1f;
                if (button && !style.Glass)
                {
                    value = y < 7 ? 0.72f : y < 9 ? 0.86f : 1f;
                    float gloss = y > size * 0.55f ? 0.06f : 0f;
                    value = Mathf.Min(1f, value + gloss);
                }
                else if (style.Glass)
                {
                    float edge = -d;
                    value = Mathf.Lerp(0.88f, 1f, (y + 0.5f) / size);
                    if (edge < 2.2f && y > size * 0.5f)
                    {
                        value = 1.25f;
                    }
                    else if (edge < 1.5f)
                    {
                        value = 0.7f;
                    }
                    else if (button && y < 4)
                    {
                        value = 0.8f;
                    }
                }
                value = Mathf.Clamp01(value * shade);
                return new Color(value, value, value, a);
            });
        }

        /// <summary>Transparent in the middle, opaque at the edges: the red flash when the runner gets hurt.</summary>
        public static Texture2D Vignette()
        {
            const int size = 128;
            return Make(size, (x, y) =>
            {
                float px = (x + 0.5f) / size * 2f - 1f;
                float py = (y + 0.5f) / size * 2f - 1f;
                float d = Mathf.Sqrt(px * px * 0.8f + py * py);
                float a = Mathf.Clamp01((d - 0.55f) / 0.6f);
                return new Color(1f, 1f, 1f, a * a);
            });
        }

        /// <summary>Soft vertical gradient (transparent at the top), used behind text on the title screen.</summary>
        public static Texture2D Fade()
        {
            var texture = new Texture2D(4, 64, TextureFormat.RGBA32, false);
            for (int y = 0; y < 64; y++)
            {
                float a = Mathf.Clamp01(1f - y / 63f);
                for (int x = 0; x < 4; x++)
                {
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
            }
            texture.Apply();
            return texture;
        }
    }
}
