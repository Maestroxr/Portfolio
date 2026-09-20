using System;
using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Monopoly.EditorTools
{
    /// <summary>A signed distance: negative inside the shape, in pixels.</summary>
    internal delegate float Sdf(Vector2 p);

    /// <summary>
    /// A small CPU rasteriser for the generated art. Shapes are signed distance functions filled with antialiased edges
    /// and blended over the canvas (straight alpha). Every fill works only inside a bounding rectangle, so large
    /// canvases (the 4096 pixel board) stay quick. Coordinates are pixels with y up.
    /// </summary>
    internal sealed class Raster
    {
        public readonly int Width;
        public readonly int Height;
        private readonly Color[] pixels;

        public Raster(int width, int height, Color clear)
        {
            Width = width;
            Height = height;
            pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = clear;
            }
        }

        public Color[] Pixels => pixels;

        public Color Get(int x, int y)
        {
            return pixels[Mathf.Clamp(y, 0, Height - 1) * Width + Mathf.Clamp(x, 0, Width - 1)];
        }

        public void Set(int x, int y, Color color)
        {
            if (x >= 0 && y >= 0 && x < Width && y < Height)
            {
                pixels[y * Width + x] = color;
            }
        }

        /// <summary>Fills a shape with one colour inside <paramref name="bounds"/>.</summary>
        public void Fill(Sdf shape, Color color, Rect bounds, float feather = 1.2f)
        {
            Fill(shape, _ => color, bounds, feather);
        }

        /// <summary>Fills a shape inside <paramref name="bounds"/>; <paramref name="shader"/> colours every covered pixel.</summary>
        public void Fill(Sdf shape, Func<Vector2, Color> shader, Rect bounds, float feather = 1.2f)
        {
            float half = Mathf.Max(0.0001f, feather) * 0.5f;
            Clip(bounds, half + 1f, out int x0, out int y0, out int x1, out int y1);
            for (int y = y0; y < y1; y++)
            {
                for (int x = x0; x < x1; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    float d = shape(p);
                    if (d > half)
                    {
                        continue;
                    }
                    float coverage = d < -half ? 1f : Mathf.Clamp01(0.5f - d / (half * 2f));
                    Color c = shader(p);
                    c.a *= coverage;
                    Blend(x, y, c);
                }
            }
        }

        /// <summary>A soft shadow: the shape's coverage fades over <paramref name="blur"/> pixels, moved by <paramref name="offset"/>.</summary>
        public void Shadow(Sdf shape, Color color, float blur, Vector2 offset, Rect bounds)
        {
            float soft = Mathf.Max(0.5f, blur);
            Rect moved = new Rect(bounds.position + offset, bounds.size);
            Clip(moved, soft + 1f, out int x0, out int y0, out int x1, out int y1);
            for (int y = y0; y < y1; y++)
            {
                for (int x = x0; x < x1; x++)
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

        /// <summary>Cuts a shape out of what is drawn (a hole: an eye, a hub cap).</summary>
        public void Erase(Sdf shape, Rect bounds, float feather = 1.2f)
        {
            float half = Mathf.Max(0.0001f, feather) * 0.5f;
            Clip(bounds, half + 1f, out int x0, out int y0, out int x1, out int y1);
            for (int y = y0; y < y1; y++)
            {
                for (int x = x0; x < x1; x++)
                {
                    float d = shape(new Vector2(x + 0.5f, y + 0.5f));
                    if (d > half)
                    {
                        continue;
                    }
                    float coverage = d < -half ? 1f : Mathf.Clamp01(0.5f - d / (half * 2f));
                    pixels[y * Width + x].a *= 1f - coverage;
                }
            }
        }

        /// <summary>Multiplies the alpha inside <paramref name="bounds"/> by the coverage of a shape (clips to it).</summary>
        public void Mask(Sdf shape, Rect bounds, float feather = 1.2f)
        {
            float half = Mathf.Max(0.0001f, feather) * 0.5f;
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    int index = y * Width + x;
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    if (!bounds.Contains(p))
                    {
                        pixels[index].a = 0f;
                        continue;
                    }
                    float d = shape(p);
                    float coverage = d < -half ? 1f : d > half ? 0f : Mathf.Clamp01(0.5f - d / (half * 2f));
                    pixels[index].a *= coverage;
                }
            }
        }

        /// <summary>Fills an axis aligned rectangle exactly (no antialiasing needed on pixel edges).</summary>
        public void FillRect(Rect rect, Color color)
        {
            Clip(rect, 0f, out int x0, out int y0, out int x1, out int y1);
            for (int y = y0; y < y1; y++)
            {
                for (int x = x0; x < x1; x++)
                {
                    Blend(x, y, color);
                }
            }
        }

        /// <summary>Adds noise to the colour inside a rectangle (paper grain, wood).</summary>
        public void Grain(Rect rect, float amount, int seed)
        {
            var random = new System.Random(seed);
            Clip(rect, 0f, out int x0, out int y0, out int x1, out int y1);
            for (int y = y0; y < y1; y++)
            {
                for (int x = x0; x < x1; x++)
                {
                    int index = y * Width + x;
                    float n = ((float)random.NextDouble() - 0.5f) * amount;
                    Color c = pixels[index];
                    pixels[index] = new Color(Mathf.Clamp01(c.r + n), Mathf.Clamp01(c.g + n), Mathf.Clamp01(c.b + n), c.a);
                }
            }
        }

        public void Blend(int x, int y, Color src)
        {
            if (src.a <= 0f || x < 0 || y < 0 || x >= Width || y >= Height)
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

        private void Clip(Rect rect, float pad, out int x0, out int y0, out int x1, out int y1)
        {
            x0 = Mathf.Max(0, Mathf.FloorToInt(rect.xMin - pad));
            y0 = Mathf.Max(0, Mathf.FloorToInt(rect.yMin - pad));
            x1 = Mathf.Min(Width, Mathf.CeilToInt(rect.xMax + pad));
            y1 = Mathf.Min(Height, Mathf.CeilToInt(rect.yMax + pad));
        }

        /// <summary>
        /// The canvas as a texture. Fully transparent pixels take the colour of their visible neighbours so bilinear
        /// filtering and mipmaps do not bleed dark fringes into the edges.
        /// </summary>
        public Texture2D ToTexture(bool bleed = true)
        {
            Color[] output = (Color[])pixels.Clone();
            if (bleed)
            {
                Bleed(output);
            }
            var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            texture.SetPixels(output);
            texture.Apply(false, false);
            return texture;
        }

        private void Bleed(Color[] output)
        {
            // A few passes of dilation of the colour (not the alpha) into transparent pixels.
            var next = (Color[])output.Clone();
            for (int pass = 0; pass < 4; pass++)
            {
                bool changed = false;
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        int i = y * Width + x;
                        if (output[i].a > 0.02f)
                        {
                            continue;
                        }
                        float r = 0f, g = 0f, b = 0f, w = 0f;
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                int nx = x + dx, ny = y + dy;
                                if (nx < 0 || ny < 0 || nx >= Width || ny >= Height)
                                {
                                    continue;
                                }
                                Color n = output[ny * Width + nx];
                                if (n.a > 0.02f)
                                {
                                    r += n.r;
                                    g += n.g;
                                    b += n.b;
                                    w += 1f;
                                }
                            }
                        }
                        if (w > 0f)
                        {
                            next[i] = new Color(r / w, g / w, b / w, output[i].a);
                            changed = true;
                        }
                    }
                }
                Array.Copy(next, output, output.Length);
                if (!changed)
                {
                    break;
                }
            }
        }
    }

    /// <summary>Signed distance functions of the shapes the art is drawn with (after Inigo Quilez).</summary>
    internal static class Sd
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

        /// <summary>An ellipse: the implicit function divided by its gradient, which stays accurate near the edge even for flat ellipses.</summary>
        public static float Ellipse(Vector2 p, Vector2 center, Vector2 radii)
        {
            Vector2 d = p - center;
            float k0 = new Vector2(d.x / radii.x, d.y / radii.y).magnitude;
            float k1 = new Vector2(d.x / (radii.x * radii.x), d.y / (radii.y * radii.y)).magnitude;
            if (k1 < 1e-6f)
            {
                return -Mathf.Min(radii.x, radii.y);
            }
            return k0 * (k0 - 1f) / k1;
        }

        public static float Ring(Vector2 p, Vector2 center, float radius, float thickness)
        {
            return Mathf.Abs((p - center).magnitude - radius) - thickness * 0.5f;
        }

        /// <summary>A polygon given by its corners (any winding).</summary>
        public static float Polygon(Vector2 p, IList<Vector2> v)
        {
            float d = Vector2.Dot(p - v[0], p - v[0]);
            float s = 1f;
            for (int i = 0, j = v.Count - 1; i < v.Count; j = i, i++)
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

        /// <summary>Turns <paramref name="p"/> around <paramref name="center"/> the opposite way, so a shape evaluated on the result is rotated by <paramref name="degrees"/>.</summary>
        public static Vector2 Rotate(Vector2 p, Vector2 center, float degrees)
        {
            float a = -degrees * Mathf.Deg2Rad;
            Vector2 q = p - center;
            return center + new Vector2(q.x * Mathf.Cos(a) - q.y * Mathf.Sin(a), q.x * Mathf.Sin(a) + q.y * Mathf.Cos(a));
        }

        public static float Union(float a, float b)
        {
            return Mathf.Min(a, b);
        }

        public static float Subtract(float shape, float cut)
        {
            return Mathf.Max(shape, -cut);
        }

        public static float Intersect(float a, float b)
        {
            return Mathf.Max(a, b);
        }

        public static float SmoothUnion(float a, float b, float k)
        {
            float h = Mathf.Clamp01(0.5f + 0.5f * (b - a) / k);
            return Mathf.Lerp(b, a, h) - k * h * (1f - h);
        }

        public static float Outline(float d, float width)
        {
            return Mathf.Abs(d) - width * 0.5f;
        }

        public static Rect Around(Vector2 center, float radius)
        {
            return new Rect(center.x - radius, center.y - radius, radius * 2f, radius * 2f);
        }

        public static Rect Around(Vector2 center, Vector2 half)
        {
            return new Rect(center - half, half * 2f);
        }

        public static Rect Bounds(IList<Vector2> points)
        {
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (Vector2 p in points)
            {
                minX = Mathf.Min(minX, p.x);
                minY = Mathf.Min(minY, p.y);
                maxX = Mathf.Max(maxX, p.x);
                maxY = Mathf.Max(maxY, p.y);
            }
            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }
    }
}
