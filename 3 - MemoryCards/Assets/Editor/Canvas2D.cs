using System;
using UnityEngine;

namespace Portfolio.MemoryCards.EditorTools
{
    /// <summary>A signed distance: negative inside the shape, in the units of the point it is given.</summary>
    internal delegate float Sdf(Vector2 p);


    /// <summary>
    /// A small CPU rasteriser for the generated art: shapes are signed distance functions, filled with antialiased
    /// edges and blended over the canvas (straight alpha). Coordinates are in pixels with y up, or normalised (0 to 1
    /// on both axes of a square canvas) through the <c>N</c> methods.
    /// </summary>
    internal sealed class Canvas2D
    {
        public readonly int Width;
        public readonly int Height;
        private readonly Color[] pixels;

        public Canvas2D(int width, int height)
        {
            Width = width;
            Height = height;
            pixels = new Color[width * height];
        }

        public Color this[int x, int y]
        {
            get => pixels[y * Width + x];
            set => pixels[y * Width + x] = value;
        }

        public void Clear(Color color)
        {
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
        }

        /// <summary>Fills a shape given in pixel space with a flat colour.</summary>
        public void Fill(Sdf shape, Color color, float feather = 1f)
        {
            Fill(shape, _ => color, feather);
        }

        /// <summary>Fills a shape given in pixel space; <paramref name="shader"/> colours every covered pixel.</summary>
        public void Fill(Sdf shape, Func<Vector2, Color> shader, float feather = 1f)
        {
            float half = Mathf.Max(0.0001f, feather) * 0.5f;
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    float d = shape(p);
                    if (d > half)
                    {
                        continue;
                    }
                    float coverage = d < -half ? 1f : Mathf.Clamp01(0.5f - d / (half * 2f));
                    Color color = shader(p);
                    color.a *= coverage;
                    Blend(x, y, color);
                }
            }
        }

        /// <summary>A soft shadow of a shape: coverage fades over <paramref name="blur"/> pixels around its edge.</summary>
        public void Shadow(Sdf shape, Color color, float blur, Vector2 offset)
        {
            float soft = Mathf.Max(0.5f, blur);
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f) - offset;
                    float d = shape(p);
                    if (d > soft)
                    {
                        continue;
                    }
                    float t = Mathf.Clamp01((d + soft) / (soft * 2f));
                    float coverage = 1f - t * t * (3f - 2f * t);
                    Color c = color;
                    c.a *= coverage;
                    Blend(x, y, c);
                }
            }
        }

        /// <summary>Fills a shape given in normalised coordinates (the canvas is taken as square).</summary>
        public void FillN(Sdf shape, Color color, float feather = 1f)
        {
            float size = Mathf.Min(Width, Height);
            Fill(p => shape(p / size) * size, color, feather);
        }

        public void FillN(Sdf shape, Func<Vector2, Color> shader, float feather = 1f)
        {
            float size = Mathf.Min(Width, Height);
            Fill(p => shape(p / size) * size, p => shader(p / size), feather);
        }

        public void ShadowN(Sdf shape, Color color, float blur, Vector2 offset)
        {
            float size = Mathf.Min(Width, Height);
            Shadow(p => shape(p / size) * size, color, blur * size, offset * size);
        }

        /// <summary>Paints <paramref name="source"/> scaled into the rectangle at <paramref name="x"/>, <paramref name="y"/> (bilinear).</summary>
        public void Stamp(Color[] source, int sourceWidth, int sourceHeight, float x, float y, float width, float height, float alpha = 1f)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt(x));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(y));
            int x1 = Mathf.Min(Width, Mathf.CeilToInt(x + width));
            int y1 = Mathf.Min(Height, Mathf.CeilToInt(y + height));
            for (int py = y0; py < y1; py++)
            {
                for (int px = x0; px < x1; px++)
                {
                    float u = (px + 0.5f - x) / width * sourceWidth - 0.5f;
                    float v = (py + 0.5f - y) / height * sourceHeight - 0.5f;
                    if (u < -0.5f || v < -0.5f || u > sourceWidth - 0.5f || v > sourceHeight - 0.5f)
                    {
                        continue;
                    }
                    Color c = Sample(source, sourceWidth, sourceHeight, u, v);
                    c.a *= alpha;
                    Blend(px, py, c);
                }
            }
        }

        private static Color Sample(Color[] source, int width, int height, float u, float v)
        {
            int ux = Mathf.Clamp(Mathf.FloorToInt(u), 0, width - 1);
            int vy = Mathf.Clamp(Mathf.FloorToInt(v), 0, height - 1);
            int ux1 = Mathf.Min(ux + 1, width - 1);
            int vy1 = Mathf.Min(vy + 1, height - 1);
            float fx = Mathf.Clamp01(u - ux);
            float fy = Mathf.Clamp01(v - vy);
            Color a = Premultiply(source[vy * width + ux]);
            Color b = Premultiply(source[vy * width + ux1]);
            Color c = Premultiply(source[vy1 * width + ux]);
            Color d = Premultiply(source[vy1 * width + ux1]);
            Color mixed = Color.Lerp(Color.Lerp(a, b, fx), Color.Lerp(c, d, fx), fy);
            if (mixed.a <= 0.0001f)
            {
                return Color.clear;
            }
            return new Color(mixed.r / mixed.a, mixed.g / mixed.a, mixed.b / mixed.a, mixed.a);
        }

        private static Color Premultiply(Color c)
        {
            return new Color(c.r * c.a, c.g * c.a, c.b * c.a, c.a);
        }

        public void Blend(int x, int y, Color src)
        {
            if (src.a <= 0f)
            {
                return;
            }
            int index = y * Width + x;
            Color dst = pixels[index];
            float outA = src.a + dst.a * (1f - src.a);
            if (outA <= 0.0001f)
            {
                pixels[index] = Color.clear;
                return;
            }
            float r = (src.r * src.a + dst.r * dst.a * (1f - src.a)) / outA;
            float g = (src.g * src.a + dst.g * dst.a * (1f - src.a)) / outA;
            float b = (src.b * src.a + dst.b * dst.a * (1f - src.a)) / outA;
            pixels[index] = new Color(r, g, b, outA);
        }

        /// <summary>Multiplies the alpha of every pixel by a mask shape's coverage (clips to the shape).</summary>
        public void Clip(Sdf shape, float feather = 1f)
        {
            float half = Mathf.Max(0.0001f, feather) * 0.5f;
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    float d = shape(new Vector2(x + 0.5f, y + 0.5f));
                    float coverage = d < -half ? 1f : d > half ? 0f : Mathf.Clamp01(0.5f - d / (half * 2f));
                    int index = y * Width + x;
                    Color c = pixels[index];
                    c.a *= coverage;
                    pixels[index] = c;
                }
            }
        }

        public Color[] Pixels => pixels;

        public Texture2D ToTexture()
        {
            var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            // Transparent pixels keep the colour of their neighbourhood so bilinear filtering has no dark fringes.
            var output = new Color[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
            {
                output[i] = pixels[i];
            }
            texture.SetPixels(output);
            texture.Apply(false, false);
            return texture;
        }
    }


    /// <summary>Signed distance functions of the shapes the art is drawn with (after Inigo Quilez).</summary>
    internal static class Shapes
    {
        public static float Circle(Vector2 p, Vector2 center, float radius)
        {
            return (p - center).magnitude - radius;
        }

        /// <summary>A box with rounded corners; <paramref name="half"/> is half its size.</summary>
        public static float Box(Vector2 p, Vector2 center, Vector2 half, float radius = 0f)
        {
            Vector2 q = new Vector2(Mathf.Abs(p.x - center.x), Mathf.Abs(p.y - center.y)) - half + new Vector2(radius, radius);
            Vector2 outside = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f));
            return outside.magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
        }

        /// <summary>A capsule (thick line) from <paramref name="a"/> to <paramref name="b"/>.</summary>
        public static float Segment(Vector2 p, Vector2 a, Vector2 b, float radius)
        {
            Vector2 pa = p - a;
            Vector2 ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Mathf.Max(0.000001f, Vector2.Dot(ba, ba)));
            return (pa - ba * h).magnitude - radius;
        }

        /// <summary>An ellipse (approximate distance, good enough for small shapes).</summary>
        public static float Ellipse(Vector2 p, Vector2 center, Vector2 radii)
        {
            Vector2 q = new Vector2((p.x - center.x) / radii.x, (p.y - center.y) / radii.y);
            float k = q.magnitude;
            return (k - 1f) * Mathf.Min(radii.x, radii.y);
        }

        public static float Ring(Vector2 p, Vector2 center, float radius, float thickness)
        {
            return Mathf.Abs((p - center).magnitude - radius) - thickness * 0.5f;
        }

        /// <summary>A polygon given by its corners (any winding).</summary>
        public static float Polygon(Vector2 p, params Vector2[] v)
        {
            float d = Vector2.Dot(p - v[0], p - v[0]);
            float s = 1f;
            for (int i = 0, j = v.Length - 1; i < v.Length; j = i, i++)
            {
                Vector2 e = v[j] - v[i];
                Vector2 w = p - v[i];
                Vector2 b = w - e * Mathf.Clamp01(Vector2.Dot(w, e) / Vector2.Dot(e, e));
                d = Mathf.Min(d, Vector2.Dot(b, b));
                bool c1 = p.y >= v[i].y;
                bool c2 = p.y < v[j].y;
                bool c3 = e.x * w.y > e.y * w.x;
                if ((c1 && c2 && c3) || (!c1 && !c2 && !c3))
                {
                    s = -s;
                }
            }
            return s * Mathf.Sqrt(d);
        }

        /// <summary>A star with <paramref name="points"/> points; <paramref name="inner"/> is the inner radius as a fraction of the outer.</summary>
        public static float Star(Vector2 p, Vector2 center, float radius, int points, float inner, float rotation = 90f)
        {
            var corners = new Vector2[points * 2];
            for (int i = 0; i < corners.Length; i++)
            {
                float angle = (rotation + i * 180f / points) * Mathf.Deg2Rad;
                float r = i % 2 == 0 ? radius : radius * inner;
                corners[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
            }
            return Polygon(p, corners);
        }

        /// <summary>A heart centred at <paramref name="center"/>, <paramref name="size"/> wide.</summary>
        public static float Heart(Vector2 p, Vector2 center, float size)
        {
            // iq's heart lives in a unit box with its tip at the origin.
            Vector2 q = (p - center) / size + new Vector2(0f, 0.55f);
            q.x = Mathf.Abs(q.x);
            float d;
            if (q.y + q.x > 1f)
            {
                d = Mathf.Sqrt(Dot2(q - new Vector2(0.25f, 0.75f))) - Mathf.Sqrt(2f) / 4f;
            }
            else
            {
                d = Mathf.Sqrt(Mathf.Min(Dot2(q - new Vector2(0f, 1f)), Dot2(q - 0.5f * Mathf.Max(q.x + q.y, 0f) * Vector2.one))) *
                    Mathf.Sign(q.x - q.y);
            }
            return d * size;
        }

        private static float Dot2(Vector2 v)
        {
            return Vector2.Dot(v, v);
        }

        /// <summary>A vesica (leaf or eye shape) between two circle arcs, pointing along x.</summary>
        public static float Vesica(Vector2 p, Vector2 center, float halfLength, float halfWidth)
        {
            Vector2 q = new Vector2(Mathf.Abs(p.y - center.y), Mathf.Abs(p.x - center.x));
            float r = (halfLength * halfLength + halfWidth * halfWidth) / (2f * halfWidth);
            float d = r - halfWidth;
            if ((q.y - halfLength) * d > q.x * halfLength)
            {
                return (q - new Vector2(0f, halfLength)).magnitude;
            }
            return (q - new Vector2(-d, 0f)).magnitude - r;
        }

        public static Vector2 Rotate(Vector2 p, Vector2 center, float degrees)
        {
            float a = -degrees * Mathf.Deg2Rad;
            Vector2 q = p - center;
            return center + new Vector2(q.x * Mathf.Cos(a) - q.y * Mathf.Sin(a), q.x * Mathf.Sin(a) + q.y * Mathf.Cos(a));
        }

        public static float Union(params float[] d)
        {
            float m = d[0];
            for (int i = 1; i < d.Length; i++)
            {
                m = Mathf.Min(m, d[i]);
            }
            return m;
        }

        public static float Subtract(float shape, float cut)
        {
            return Mathf.Max(shape, -cut);
        }

        public static float Intersect(float a, float b)
        {
            return Mathf.Max(a, b);
        }

        /// <summary>Smooth union, blending the two shapes over <paramref name="k"/>.</summary>
        public static float SmoothUnion(float a, float b, float k)
        {
            float h = Mathf.Clamp01(0.5f + 0.5f * (b - a) / k);
            return Mathf.Lerp(b, a, h) - k * h * (1f - h);
        }

        /// <summary>The shape turned into an outline <paramref name="width"/> wide.</summary>
        public static float Outline(float d, float width)
        {
            return Mathf.Abs(d) - width * 0.5f;
        }

        public static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex.StartsWith("#") ? hex : "#" + hex, out Color color) ? color : Color.magenta;
        }
    }
}
