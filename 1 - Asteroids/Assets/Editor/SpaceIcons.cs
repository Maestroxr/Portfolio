using System;
using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Asteroids.EditorTools
{
    /// <summary>
    /// The interface art of the Asteroids module: chamfered sci-fi panels, buttons and frames for 9-slicing, the hexagon
    /// of a mission node, and the icons, drawn from signed distance functions with an outline, a soft glow and a
    /// vertical gradient.
    /// </summary>
    internal static class SpaceIcons
    {
        private sealed class Layer
        {
            public Func<Vector2, float> Shape;
            public Color Top = Color.white;
            public Color Bottom = Color.white;
            public Color Outline = new Color(0f, 0f, 0f, 0f);
            public float OutlineWidth;
            public Color Glow = new Color(0f, 0f, 0f, 0f);
            public float GlowWidth;
        }

        private static readonly Color Ink = new Color(0.03f, 0.05f, 0.1f, 1f);
        private static readonly Color Cyan = new Color(0.45f, 0.92f, 1f);
        private static readonly Color CyanDark = new Color(0.15f, 0.55f, 0.85f);

        private static Layer L(Func<Vector2, float> shape, Color top, Color bottom, float outline = 0.06f, Color? glow = null, float glowWidth = 0.12f)
        {
            return new Layer
            {
                Shape = shape,
                Top = top,
                Bottom = bottom,
                Outline = outline > 0f ? Ink : new Color(0f, 0f, 0f, 0f),
                OutlineWidth = outline,
                Glow = glow ?? new Color(0f, 0f, 0f, 0f),
                GlowWidth = glow.HasValue ? glowWidth : 0f
            };
        }

        /// <summary>Draws layers over each other in a -1..1 square, anti-aliased by the pixel size.</summary>
        private static Texture2D Draw(int size, params Layer[] layers)
        {
            float pixel = 2f / size;
            return SpaceTextures.Make(size, (x, y) =>
            {
                var p = new Vector2((x + 0.5f) / size * 2f - 1f, (y + 0.5f) / size * 2f - 1f);
                Color result = new Color(0f, 0f, 0f, 0f);
                foreach (Layer layer in layers)
                {
                    float d = layer.Shape(p);
                    if (layer.GlowWidth > 0f && d > 0f)
                    {
                        float glow = Mathf.Clamp01(1f - d / layer.GlowWidth);
                        Color halo = layer.Glow;
                        halo.a *= glow * glow;
                        result = Over(halo, result);
                    }
                    float coverage = Mathf.Clamp01(0.5f - d / pixel);
                    if (coverage <= 0f)
                    {
                        continue;
                    }
                    Color fill = Color.Lerp(layer.Bottom, layer.Top, (p.y + 1f) * 0.5f);
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

        // ------------------------------------------------------------------ distance functions

        private static float Circle(Vector2 p, Vector2 center, float radius)
        {
            return Vector2.Distance(p, center) - radius;
        }

        private static float Box(Vector2 p, Vector2 center, Vector2 half, float radius = 0f)
        {
            Vector2 q = new Vector2(Mathf.Abs(p.x - center.x), Mathf.Abs(p.y - center.y)) - half + Vector2.one * radius;
            return new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
        }

        /// <summary>A box with its corners cut at 45 degrees.</summary>
        private static float Chamfer(Vector2 p, Vector2 center, Vector2 half, float cut)
        {
            Vector2 q = new Vector2(Mathf.Abs(p.x - center.x), Mathf.Abs(p.y - center.y));
            float box = Mathf.Max(q.x - half.x, q.y - half.y);
            float corner = (q.x + q.y - (half.x + half.y - cut)) * 0.70710678f;
            return Mathf.Max(box, corner);
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

        private static Vector2[] RegularPolygon(int sides, float radius, float rotation, Vector2 center)
        {
            var points = new Vector2[sides];
            for (int i = 0; i < sides; i++)
            {
                float angle = rotation + Mathf.PI * 2f * i / sides;
                points[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }
            return points;
        }

        private static Vector2[] StarPoints(float outer, float inner, Vector2 center)
        {
            var outline = new Vector2[10];
            for (int i = 0; i < outline.Length; i++)
            {
                float angle = Mathf.PI * 0.5f + Mathf.PI * i / 5f;
                float radius = i % 2 == 0 ? outer : inner;
                outline[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }
            return outline;
        }

        private static Vector2 Rotate(Vector2 p, float degrees)
        {
            float r = degrees * Mathf.Deg2Rad;
            float c = Mathf.Cos(r);
            float s = Mathf.Sin(r);
            return new Vector2(p.x * c - p.y * s, p.x * s + p.y * c);
        }

        // ------------------------------------------------------------------ interface sprites

        /// <summary>
        /// A chamfered panel for 9-slicing: a dim interior and a bright rim, so an Image tinted with an accent colour
        /// gets a dark body and a glowing border.
        /// </summary>
        public static Texture2D Panel(bool bright)
        {
            const int size = 64;
            return SpaceTextures.Make(size, (x, y) =>
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                float d = Chamfer(p, new Vector2(size / 2f, size / 2f), new Vector2(size / 2f - 1f, size / 2f - 1f), 14f);
                float a = Mathf.Clamp01(0.5f - d);
                float rim = Mathf.Clamp01(1f - Mathf.Abs(d + 1.5f) / 1.5f);
                float body = bright ? 0.62f + 0.12f * (y / (float)size) : 0.16f + 0.08f * (y / (float)size);
                float value = Mathf.Max(body, rim);
                float alpha = bright ? a : a * Mathf.Max(0.85f, rim);
                return new Color(value, value, value, alpha);
            });
        }

        /// <summary>Just the chamfered rim, for selection frames.</summary>
        public static Texture2D Frame()
        {
            const int size = 64;
            return SpaceTextures.Make(size, (x, y) =>
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                float d = Chamfer(p, new Vector2(size / 2f, size / 2f), new Vector2(size / 2f - 1f, size / 2f - 1f), 14f);
                float rim = Mathf.Clamp01(1f - Mathf.Abs(d + 2.5f) / 2.5f);
                float glow = d < 0f ? Mathf.Clamp01(1f + d / 9f) * 0.35f : 0f;
                return new Color(1f, 1f, 1f, Mathf.Clamp01(rim + glow * glow));
            });
        }

        /// <summary>A hexagon (pointy sides left and right) with a bright rim, for mission nodes.</summary>
        public static Texture2D Hexagon(bool rimOnly)
        {
            const int size = 128;
            Vector2[] hexagon = RegularPolygon(6, 0.96f, 0f, Vector2.zero);
            return SpaceTextures.Make(size, (x, y) =>
            {
                var p = new Vector2((x + 0.5f) / size * 2f - 1f, (y + 0.5f) / size * 2f - 1f);
                float d = Polygon(p, hexagon);
                float pixel = 2f / size;
                float a = Mathf.Clamp01(0.5f - d / pixel);
                float rim = Mathf.Clamp01(1f - Mathf.Abs(d + 0.035f) / 0.03f);
                if (rimOnly)
                {
                    float glow = d < 0f ? Mathf.Clamp01(1f + d / 0.18f) * 0.4f : 0f;
                    return new Color(1f, 1f, 1f, Mathf.Clamp01(rim + glow * glow) * a);
                }
                float body = 0.3f + 0.2f * (p.y + 1f) * 0.5f;
                float value = Mathf.Max(body, rim);
                return new Color(value, value, value, a);
            });
        }

        /// <summary>A plain rounded bar for fills (health, shield, progress).</summary>
        public static Texture2D Bar()
        {
            const int width = 64;
            const int height = 16;
            return SpaceTextures.Make(width, height, (x, y) =>
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                float d = Chamfer(p, new Vector2(width / 2f, height / 2f), new Vector2(width / 2f - 0.5f, height / 2f - 0.5f), 5f);
                float a = Mathf.Clamp01(0.5f - d);
                float shine = y > height * 0.55f ? 1f : 0.82f;
                return new Color(shine, shine, shine, a);
            });
        }

        /// <summary>A thick ring for radial timers.</summary>
        public static Texture2D TimerRing()
        {
            const int size = 128;
            return SpaceTextures.Make(size, (x, y) =>
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size / 2f, size / 2f)) / (size / 2f);
                float ring = Mathf.Clamp01(1f - Mathf.Abs(d - 0.86f) / 0.1f * 1.2f);
                return new Color(1f, 1f, 1f, Mathf.Clamp01(ring * 1.5f));
            });
        }

        /// <summary>Transparent in the middle, opaque at the edges: the red flash when the ship is hit.</summary>
        public static Texture2D Vignette()
        {
            const int size = 128;
            return SpaceTextures.Make(size, (x, y) =>
            {
                float px = (x + 0.5f) / size * 2f - 1f;
                float py = (y + 0.5f) / size * 2f - 1f;
                float d = Mathf.Sqrt(px * px * 0.8f + py * py);
                float a = Mathf.Clamp01((d - 0.5f) / 0.65f);
                return new Color(1f, 1f, 1f, a * a);
            });
        }

        /// <summary>A soft vertical gradient (transparent at the top).</summary>
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

        /// <summary>A soft horizontal line (for the comet lanes and dividers).</summary>
        public static Texture2D Lane()
        {
            const int width = 64;
            const int height = 32;
            return SpaceTextures.Make(width, height, (x, y) =>
            {
                float py = (y + 0.5f) / height * 2f - 1f;
                float px = (x + 0.5f) / width;
                float across = Mathf.Exp(-py * py * 6f);
                float dash = Mathf.Repeat(px * 2f, 1f) < 0.7f ? 1f : 0.35f;
                return new Color(1f, 1f, 1f, across * dash);
            });
        }

        // ------------------------------------------------------------------ icons

        public static readonly string[] IconNames =
        {
            "Star", "StarEmpty", "Lock", "Play", "Retry", "Levels", "Settings", "Exit", "Pause", "Ship", "Nova", "Skull",
            "Infinity", "Blaster", "Laser", "Scatter", "Missile", "Overdrive", "Magnet", "Chrono", "Drones", "Shield",
            "Repair", "Crystal", "Warning", "Arrow", "Check", "Hangar", "Asteroid", "Dash"
        };

        public static Texture2D Icon(string name)
        {
            const int size = 128;
            Color white = Color.white;
            Color pale = new Color(0.8f, 0.9f, 1f);
            switch (name)
            {
                case "Star":
                    return Draw(size, L(p => Polygon(p, StarPoints(0.95f, 0.42f, new Vector2(0f, -0.04f))), new Color(1f, 0.95f, 0.5f), new Color(1f, 0.62f, 0.1f), 0.07f, new Color(1f, 0.75f, 0.2f, 0.6f)));
                case "StarEmpty":
                    return Draw(size, L(p => Polygon(p, StarPoints(0.95f, 0.42f, new Vector2(0f, -0.04f))), new Color(0.22f, 0.26f, 0.36f, 0.85f), new Color(0.12f, 0.15f, 0.22f, 0.85f), 0.07f));
                case "Lock":
                    return Draw(size,
                        L(p => Mathf.Max(Mathf.Abs(Circle(p, new Vector2(0f, 0.18f), 0.38f)) - 0.1f, -p.y + 0.15f), new Color(0.85f, 0.9f, 0.98f), new Color(0.55f, 0.62f, 0.75f)),
                        L(p => Chamfer(p, new Vector2(0f, -0.3f), new Vector2(0.62f, 0.48f), 0.16f), new Color(1f, 0.82f, 0.35f), new Color(0.9f, 0.5f, 0.1f), 0.07f),
                        L(p => Mathf.Min(Circle(p, new Vector2(0f, -0.22f), 0.12f), Box(p, new Vector2(0f, -0.42f), new Vector2(0.05f, 0.18f))), Ink, Ink, 0f));
                case "Play":
                    return Draw(size, L(p => Polygon(p, new[] { new Vector2(-0.45f, -0.7f), new Vector2(0.7f, 0f), new Vector2(-0.45f, 0.7f) }), white, pale, 0f));
                case "Retry":
                    return Draw(size, L(p => Mathf.Min(Mathf.Max(Mathf.Abs(Circle(p, new Vector2(0f, -0.08f), 0.55f)) - 0.13f, Mathf.Min(p.x, p.y + 0.08f)),
                        Polygon(p, new[] { new Vector2(0.24f, -0.02f), new Vector2(0.86f, -0.02f), new Vector2(0.55f, 0.42f) })), white, pale, 0f));
                case "Levels":
                    return Draw(size, L(p =>
                    {
                        float d = float.MaxValue;
                        for (int i = 0; i < 3; i++)
                        {
                            float x = -0.55f + i * 0.55f;
                            d = Mathf.Min(d, Polygon(p, RegularPolygon(6, 0.26f, 0f, new Vector2(x, i == 1 ? 0.3f : -0.2f))));
                        }
                        return d;
                    }, white, pale, 0f));
                case "Settings":
                    return Draw(size, L(p =>
                    {
                        float angle = Mathf.Atan2(p.y, p.x);
                        float teeth = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(Mathf.Cos(angle * 8f) * 3f + 0.5f)) * 0.18f;
                        return Mathf.Max(p.magnitude - (0.62f + teeth), -(p.magnitude - 0.25f));
                    }, white, pale, 0f));
                case "Exit":
                    return Draw(size,
                        L(p => Mathf.Max(Chamfer(p, new Vector2(-0.25f, 0f), new Vector2(0.5f, 0.75f), 0.15f), -Chamfer(p, new Vector2(-0.25f, 0f), new Vector2(0.34f, 0.59f), 0.1f)), white, pale, 0f),
                        L(p => Mathf.Min(Segment(p, new Vector2(0f, 0f), new Vector2(0.75f, 0f), 0.09f),
                            Polygon(p, new[] { new Vector2(0.5f, 0.3f), new Vector2(0.92f, 0f), new Vector2(0.5f, -0.3f) })), white, white, 0f));
                case "Pause":
                    return Draw(size,
                        L(p => Polygon(p, RegularPolygon(6, 0.95f, 0f, Vector2.zero)), new Color(0.12f, 0.3f, 0.45f, 0.95f), new Color(0.05f, 0.12f, 0.22f, 0.95f), 0.05f, new Color(0.3f, 0.85f, 1f, 0.5f)),
                        L(p => Mathf.Min(Box(p, new Vector2(-0.2f, 0f), new Vector2(0.1f, 0.38f), 0.03f), Box(p, new Vector2(0.2f, 0f), new Vector2(0.1f, 0.38f), 0.03f)), Cyan, Cyan, 0f));
                case "Ship":
                    return Draw(size, L(Ship, new Color(0.9f, 0.97f, 1f), new Color(0.45f, 0.75f, 1f), 0.07f, new Color(0.3f, 0.8f, 1f, 0.45f)));
                case "Hangar":
                    return Draw(size,
                        L(p => Polygon(p, RegularPolygon(6, 0.97f, 0f, Vector2.zero)), new Color(0.1f, 0.25f, 0.4f, 0.9f), new Color(0.04f, 0.1f, 0.2f, 0.9f), 0.04f),
                        L(p => Ship(p / 0.72f) * 0.72f, white, pale, 0.06f));
                case "Nova":
                    return Draw(size,
                        L(p =>
                        {
                            float angle = Mathf.Atan2(p.y, p.x);
                            float spikes = Mathf.Pow(Mathf.Abs(Mathf.Cos(angle * 4f)), 6f) * 0.35f;
                            return p.magnitude - (0.45f + spikes);
                        }, new Color(0.7f, 0.95f, 1f), new Color(0.3f, 0.6f, 1f), 0.05f, new Color(0.4f, 0.8f, 1f, 0.6f)),
                        L(p => Circle(p, Vector2.zero, 0.24f), white, new Color(0.85f, 0.95f, 1f), 0f));
                case "Skull":
                    return Draw(size,
                        L(p => Mathf.Min(Circle(p, new Vector2(0f, 0.12f), 0.66f), Chamfer(p, new Vector2(0f, -0.5f), new Vector2(0.36f, 0.3f), 0.12f)), new Color(1f, 0.55f, 0.5f), new Color(0.85f, 0.2f, 0.25f), 0.07f, new Color(1f, 0.3f, 0.3f, 0.5f)),
                        L(p => Mathf.Min(Circle(p, new Vector2(-0.26f, 0.08f), 0.17f), Mathf.Min(Circle(p, new Vector2(0.26f, 0.08f), 0.17f),
                            Polygon(p, new[] { new Vector2(0f, -0.08f), new Vector2(0.08f, -0.24f), new Vector2(-0.08f, -0.24f) }))), Ink, Ink, 0f));
                case "Infinity":
                    return Draw(size, L(p => Mathf.Min(Mathf.Abs(Circle(p, new Vector2(-0.3f, 0f), 0.34f)), Mathf.Abs(Circle(p, new Vector2(0.3f, 0f), 0.34f))) - 0.1f,
                        white, pale, 0.05f, new Color(0.5f, 0.85f, 1f, 0.5f)));
                case "Blaster":
                    return Draw(size, L(p =>
                    {
                        float d = Segment(p, new Vector2(0f, -0.55f), new Vector2(0f, 0.55f), 0.16f);
                        d = Mathf.Min(d, Segment(p, new Vector2(-0.45f, -0.35f), new Vector2(-0.45f, 0.25f), 0.1f));
                        return Mathf.Min(d, Segment(p, new Vector2(0.45f, -0.35f), new Vector2(0.45f, 0.25f), 0.1f));
                    }, new Color(0.75f, 0.97f, 1f), new Color(0.3f, 0.75f, 1f), 0.06f, new Color(0.3f, 0.85f, 1f, 0.6f)));
                case "Laser":
                    return Draw(size,
                        L(p => Segment(p, new Vector2(-0.75f, -0.75f), new Vector2(0.75f, 0.75f), 0.12f), new Color(1f, 0.7f, 0.75f), new Color(1f, 0.25f, 0.4f), 0.05f, new Color(1f, 0.3f, 0.45f, 0.7f), 0.2f),
                        L(p => Segment(p, new Vector2(-0.7f, -0.7f), new Vector2(0.7f, 0.7f), 0.04f), white, white, 0f));
                case "Scatter":
                    return Draw(size, L(p =>
                    {
                        float d = float.MaxValue;
                        for (int i = -2; i <= 2; i++)
                        {
                            Vector2 tip = Rotate(new Vector2(0f, 0.75f), i * 18f) + new Vector2(0f, -0.6f);
                            d = Mathf.Min(d, Circle(p, tip, 0.13f));
                            d = Mathf.Min(d, Segment(p, new Vector2(0f, -0.6f), tip, 0.035f));
                        }
                        return d;
                    }, new Color(1f, 0.85f, 0.5f), new Color(1f, 0.5f, 0.15f), 0.05f, new Color(1f, 0.6f, 0.2f, 0.6f)));
                case "Missile":
                    return Draw(size, L(p =>
                    {
                        Vector2 q = Rotate(p, -35f);
                        float body = Box(q, new Vector2(0f, -0.05f), new Vector2(0.14f, 0.5f), 0.1f);
                        float nose = Polygon(q, new[] { new Vector2(-0.14f, 0.4f), new Vector2(0.14f, 0.4f), new Vector2(0f, 0.78f) });
                        float fins = Polygon(q, new[] { new Vector2(-0.38f, -0.6f), new Vector2(0.38f, -0.6f), new Vector2(0.14f, -0.25f), new Vector2(-0.14f, -0.25f) });
                        return Mathf.Min(body, Mathf.Min(nose, fins));
                    }, new Color(0.8f, 1f, 0.8f), new Color(0.3f, 0.85f, 0.4f), 0.06f, new Color(0.4f, 1f, 0.5f, 0.55f)));
                case "Overdrive":
                    return Draw(size, L(p => Polygon(p, new[]
                    {
                        new Vector2(0.15f, 0.9f), new Vector2(-0.5f, -0.05f), new Vector2(-0.05f, -0.05f),
                        new Vector2(-0.2f, -0.9f), new Vector2(0.5f, 0.12f), new Vector2(0.06f, 0.12f)
                    }), new Color(1f, 0.97f, 0.6f), new Color(1f, 0.7f, 0.1f), 0.06f, new Color(1f, 0.8f, 0.2f, 0.6f)));
                case "Magnet":
                    return Draw(size,
                        L(p => Mathf.Min(Mathf.Max(Mathf.Abs(Circle(p, new Vector2(0f, -0.05f), 0.5f)) - 0.2f, p.y + 0.05f),
                            Mathf.Min(Box(p, new Vector2(-0.5f, 0.3f), new Vector2(0.2f, 0.35f)), Box(p, new Vector2(0.5f, 0.3f), new Vector2(0.2f, 0.35f)))),
                            new Color(1f, 0.45f, 0.5f), new Color(0.85f, 0.15f, 0.2f), 0.07f, new Color(1f, 0.3f, 0.35f, 0.5f)),
                        L(p => Mathf.Min(Box(p, new Vector2(-0.5f, 0.55f), new Vector2(0.2f, 0.14f)), Box(p, new Vector2(0.5f, 0.55f), new Vector2(0.2f, 0.14f))), white, pale, 0.07f));
                case "Chrono":
                    return Draw(size,
                        L(p => Polygon(p, new[]
                        {
                            new Vector2(-0.55f, 0.8f), new Vector2(0.55f, 0.8f), new Vector2(0.08f, 0f), new Vector2(0.55f, -0.8f),
                            new Vector2(-0.55f, -0.8f), new Vector2(-0.08f, 0f)
                        }), new Color(0.75f, 0.8f, 1f), new Color(0.4f, 0.45f, 1f), 0.06f, new Color(0.5f, 0.55f, 1f, 0.6f)),
                        L(p => Polygon(p, new[] { new Vector2(-0.3f, -0.66f), new Vector2(0.3f, -0.66f), new Vector2(0f, -0.25f) }), white, white, 0f));
                case "Drones":
                    return Draw(size,
                        L(p => Mathf.Abs(Circle(p, Vector2.zero, 0.62f)) - 0.035f, new Color(0.5f, 1f, 0.8f, 0.8f), new Color(0.5f, 1f, 0.8f, 0.8f), 0f),
                        L(p => Mathf.Min(Circle(p, new Vector2(-0.62f, 0f), 0.2f), Circle(p, new Vector2(0.62f, 0f), 0.2f)), new Color(0.7f, 1f, 0.9f), new Color(0.3f, 0.9f, 0.6f), 0.06f, new Color(0.4f, 1f, 0.7f, 0.6f)),
                        L(p => Ship(p / 0.5f) * 0.5f, white, pale, 0.05f));
                case "Shield":
                    return Draw(size,
                        L(p => Polygon(p, RegularPolygon(6, 0.92f, Mathf.PI / 6f, Vector2.zero)), new Color(0.5f, 0.85f, 1f), new Color(0.15f, 0.45f, 0.95f), 0.07f, new Color(0.3f, 0.7f, 1f, 0.6f)),
                        L(p => Mathf.Abs(Polygon(p, RegularPolygon(6, 0.55f, Mathf.PI / 6f, Vector2.zero))) - 0.05f, white, new Color(0.85f, 0.95f, 1f), 0f));
                case "Repair":
                    return Draw(size,
                        L(p => Mathf.Min(Box(p, Vector2.zero, new Vector2(0.24f, 0.72f), 0.05f), Box(p, Vector2.zero, new Vector2(0.72f, 0.24f), 0.05f)),
                            new Color(0.6f, 1f, 0.7f), new Color(0.2f, 0.8f, 0.35f), 0.07f, new Color(0.3f, 1f, 0.5f, 0.55f)));
                case "Crystal":
                    return Draw(size,
                        L(p => Polygon(p, new[] { new Vector2(0f, 0.92f), new Vector2(0.52f, 0.2f), new Vector2(0f, -0.92f), new Vector2(-0.52f, 0.2f) }),
                            new Color(0.75f, 1f, 1f), new Color(0.2f, 0.7f, 1f), 0.06f, new Color(0.3f, 0.9f, 1f, 0.6f)),
                        L(p => Polygon(p, new[] { new Vector2(0f, 0.92f), new Vector2(0.52f, 0.2f), new Vector2(0f, 0.2f) }), new Color(1f, 1f, 1f, 0.8f), new Color(0.9f, 1f, 1f, 0.6f), 0f));
                case "Warning":
                    return Draw(size,
                        L(p => Polygon(p, new[] { new Vector2(0f, 0.88f), new Vector2(0.92f, -0.72f), new Vector2(-0.92f, -0.72f) }), new Color(1f, 0.85f, 0.3f), new Color(1f, 0.45f, 0.1f), 0.07f, new Color(1f, 0.4f, 0.1f, 0.6f)),
                        L(p => Mathf.Min(Segment(p, new Vector2(0f, 0.38f), new Vector2(0f, -0.2f), 0.09f), Circle(p, new Vector2(0f, -0.45f), 0.1f)), Ink, Ink, 0f));
                case "Arrow":
                    return Draw(size, L(p => Polygon(p, new[]
                    {
                        new Vector2(0.9f, 0f), new Vector2(-0.3f, 0.7f), new Vector2(-0.05f, 0f), new Vector2(-0.3f, -0.7f)
                    }), new Color(1f, 0.6f, 0.3f), new Color(1f, 0.25f, 0.15f), 0.07f, new Color(1f, 0.35f, 0.15f, 0.7f), 0.15f));
                case "Check":
                    return Draw(size, L(p => Mathf.Min(Segment(p, new Vector2(-0.55f, 0f), new Vector2(-0.15f, -0.45f), 0.14f), Segment(p, new Vector2(-0.15f, -0.45f), new Vector2(0.6f, 0.5f), 0.14f)),
                        new Color(0.6f, 1f, 0.6f), new Color(0.3f, 0.85f, 0.35f), 0.06f));
                case "Dash":
                    return Draw(size, L(p => Mathf.Min(Polygon(p, new[] { new Vector2(0.85f, 0f), new Vector2(0.2f, 0.55f), new Vector2(0.2f, -0.55f) }),
                        Mathf.Min(Box(p, new Vector2(-0.2f, 0.25f), new Vector2(0.35f, 0.07f)), Mathf.Min(Box(p, new Vector2(-0.3f, 0f), new Vector2(0.45f, 0.07f)), Box(p, new Vector2(-0.2f, -0.25f), new Vector2(0.35f, 0.07f))))),
                        new Color(0.7f, 0.95f, 1f), new Color(0.3f, 0.65f, 1f), 0.05f, new Color(0.3f, 0.8f, 1f, 0.5f)));
                case "Asteroid":
                    return Draw(size,
                        L(p =>
                        {
                            float angle = Mathf.Atan2(p.y, p.x);
                            float lumps = Mathf.Sin(angle * 5f) * 0.06f + Mathf.Sin(angle * 9f + 1.3f) * 0.04f;
                            return p.magnitude - (0.78f + lumps);
                        }, new Color(0.62f, 0.58f, 0.55f), new Color(0.3f, 0.28f, 0.28f), 0.06f),
                        L(p => Mathf.Min(Circle(p, new Vector2(-0.25f, 0.25f), 0.17f), Mathf.Min(Circle(p, new Vector2(0.3f, -0.15f), 0.12f), Circle(p, new Vector2(-0.1f, -0.38f), 0.09f))),
                            new Color(0.25f, 0.23f, 0.23f), new Color(0.35f, 0.32f, 0.3f), 0f));
                default:
                    throw new ArgumentException($"Unknown icon {name}");
            }
        }

        /// <summary>A delta-wing ship pointing up.</summary>
        private static float Ship(Vector2 p)
        {
            return Polygon(p, new[]
            {
                new Vector2(0f, 0.92f), new Vector2(0.22f, 0.1f), new Vector2(0.8f, -0.55f), new Vector2(0.62f, -0.78f),
                new Vector2(0.18f, -0.5f), new Vector2(0f, -0.72f), new Vector2(-0.18f, -0.5f), new Vector2(-0.62f, -0.78f),
                new Vector2(-0.8f, -0.55f), new Vector2(-0.22f, 0.1f)
            });
        }

        /// <summary>The launcher icon: a ship dodging an asteroid over a starry hexagon.</summary>
        public static Texture2D GameIcon()
        {
            const int size = 256;
            Texture2D asteroid = Icon("Asteroid");
            Texture2D ship = Icon("Ship");
            var hex = RegularPolygon(6, 0.97f, 0f, Vector2.zero);
            Texture2D result = SpaceTextures.Make(size, (x, y) =>
            {
                var p = new Vector2((x + 0.5f) / size * 2f - 1f, (y + 0.5f) / size * 2f - 1f);
                float d = Polygon(p, hex);
                float pixel = 2f / size;
                float a = Mathf.Clamp01(0.5f - d / pixel);
                if (a <= 0f)
                {
                    return new Color(0f, 0f, 0f, 0f);
                }
                Color space = Color.Lerp(new Color(0.02f, 0.03f, 0.1f), new Color(0.12f, 0.08f, 0.3f), (p.y + 1f) * 0.5f);
                float star = SpaceTextures.Hash(x / 3, y / 3, 17) > 0.985f ? 1f : 0f;
                space = Color.Lerp(space, Color.white, star * 0.8f);
                float rim = Mathf.Clamp01(1f - Mathf.Abs(d + 0.03f) / 0.025f);
                Color color = Color.Lerp(space, new Color(0.4f, 0.9f, 1f), rim);
                color = Blend(color, asteroid, p * 1.25f + new Vector2(-0.35f, -0.3f));
                color = Blend(color, ship, Rotate(p, -25f) * 2.1f + new Vector2(0.8f, -0.9f));
                color.a = a;
                return color;
            });
            UnityEngine.Object.DestroyImmediate(asteroid);
            UnityEngine.Object.DestroyImmediate(ship);
            return result;
        }

        /// <summary>Paints <paramref name="icon"/> over <paramref name="under"/> at icon coordinates <paramref name="q"/> (-1..1).</summary>
        private static Color Blend(Color under, Texture2D icon, Vector2 q)
        {
            if (Mathf.Abs(q.x) > 1f || Mathf.Abs(q.y) > 1f)
            {
                return under;
            }
            Color over = icon.GetPixelBilinear((q.x + 1f) * 0.5f, (q.y + 1f) * 0.5f);
            return Color.Lerp(under, new Color(over.r, over.g, over.b, 1f), over.a);
        }
    }
}
