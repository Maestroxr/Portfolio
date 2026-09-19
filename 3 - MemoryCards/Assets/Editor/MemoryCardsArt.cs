using System;
using UnityEngine;
using static Portfolio.MemoryCards.EditorTools.Shapes;

namespace Portfolio.MemoryCards.EditorTools
{
    /// <summary>The pattern printed on the back of a world's cards.</summary>
    internal enum CardPattern
    {
        Dots,
        Stripes,
        Flakes,
        Zigzag
    }


    /// <summary>
    /// Draws the generated art of Memory Cards in the flat, rounded style of the Kenney packs it sits next to: card
    /// fronts, backs, shadows and glows, the ice overlays, the faces of the special cards, the interface icons, hearts,
    /// panels, particles, backdrop shapes and the launcher icon. Every method returns a canvas; the art builder saves them.
    /// </summary>
    internal static class MemoryCardsArt
    {
        public const int CardWidth = 256;
        public const int CardHeight = 340;
        public const int CardRadius = 30;
        /// <summary>Extra room around the card in the shadow and glow textures.</summary>
        public const int CardPadding = 24;

        private static readonly Color White = Color.white;

        private static Vector2 CardCenter => new Vector2(CardWidth * 0.5f, CardHeight * 0.5f);

        private static float CardShape(Vector2 p, float inset = 2f)
        {
            return Box(p, CardCenter, new Vector2(CardWidth * 0.5f - inset, CardHeight * 0.5f - inset), CardRadius - inset * 0.5f);
        }

        #region Cards

        /// <summary>The face side of every card: warm white with a soft border.</summary>
        public static Canvas2D CardFront()
        {
            var canvas = new Canvas2D(CardWidth, CardHeight);
            Color top = Hex("#FFFEFA");
            Color bottom = Hex("#F4EEE2");
            canvas.Fill(p => CardShape(p), p => Color.Lerp(bottom, top, p.y / CardHeight));
            canvas.Fill(p => Outline(CardShape(p, 7f), 4f), Hex("#E9E0CF"));
            canvas.Fill(p => Outline(CardShape(p, 2.5f), 3f), Hex("#D8CDB8"));
            return canvas;
        }

        /// <summary>The soft shadow under a card (with padding around it).</summary>
        public static Canvas2D CardShadow()
        {
            var canvas = new Canvas2D(CardWidth + CardPadding * 2, CardHeight + CardPadding * 2);
            var center = new Vector2(canvas.Width * 0.5f, canvas.Height * 0.5f);
            canvas.Shadow(p => Box(p, center, new Vector2(CardWidth * 0.5f - 6f, CardHeight * 0.5f - 6f), CardRadius), new Color(0.05f, 0.08f, 0.2f, 0.42f), 14f, Vector2.zero);
            return canvas;
        }

        /// <summary>A glow hugging the card's outline, tinted at runtime.</summary>
        public static Canvas2D CardGlow()
        {
            var canvas = new Canvas2D(CardWidth + CardPadding * 2, CardHeight + CardPadding * 2);
            var center = new Vector2(canvas.Width * 0.5f, canvas.Height * 0.5f);
            Vector2 half = new Vector2(CardWidth * 0.5f, CardHeight * 0.5f);
            canvas.Fill(p =>
            {
                float d = Box(p, center, half, CardRadius);
                return d < -3f ? 1f : -1f;
            }, p =>
            {
                float d = Box(p, center, half, CardRadius);
                float outside = Mathf.Clamp01(1f - Mathf.Abs(d + 1f) / 20f);
                return new Color(1f, 1f, 1f, outside * outside * (d > -3f ? 1f : 0f));
            }, 1f);
            return canvas;
        }

        /// <summary>The back of a world's cards: its colour, a printed pattern, a border and a paw print medallion.</summary>
        public static Canvas2D CardBack(Color color, Color dark, CardPattern pattern, bool medallion)
        {
            var canvas = new Canvas2D(CardWidth, CardHeight);
            Color light = Color.Lerp(color, Color.white, 0.18f);
            canvas.Fill(p => CardShape(p), p => Color.Lerp(color, light, p.y / CardHeight));
            // The printed pattern, inside the inner border.
            Color ink = new Color(1f, 1f, 1f, 0.2f);
            Sdf inner = p => CardShape(p, 16f);
            canvas.Fill(p => Intersect(inner(p), PatternShape(pattern, p)), ink);
            canvas.Fill(p => Outline(CardShape(p, 14f), 4f), new Color(1f, 1f, 1f, 0.55f));
            canvas.Fill(p => Outline(CardShape(p, 2.5f), 4f), dark);
            if (medallion)
            {
                Vector2 c = CardCenter;
                canvas.Shadow(p => Circle(p, c, 62f), new Color(0f, 0f, 0f, 0.25f), 8f, new Vector2(0f, -5f));
                canvas.Fill(p => Circle(p, c, 62f), White);
                canvas.Fill(p => Outline(Circle(p, c, 62f), 5f), Color.Lerp(dark, color, 0.4f));
                canvas.Fill(p => Paw(p, c, 78f), color);
            }
            return canvas;
        }

        private static float PatternShape(CardPattern pattern, Vector2 p)
        {
            switch (pattern)
            {
                case CardPattern.Stripes:
                {
                    float u = (p.x + p.y) / Mathf.Sqrt(2f);
                    float m = Mathf.Repeat(u, 34f) - 17f;
                    return Mathf.Abs(m) - 6f;
                }
                case CardPattern.Flakes:
                {
                    float cell = 38f;
                    float row = Mathf.Floor(p.y / cell);
                    float offset = Mathf.Repeat(row, 2f) * cell * 0.5f;
                    Vector2 q = new Vector2(Mathf.Repeat(p.x + offset, cell), Mathf.Repeat(p.y, cell)) - new Vector2(cell * 0.5f, cell * 0.5f);
                    return Flake(q, Vector2.zero, 11f, 2f);
                }
                case CardPattern.Zigzag:
                {
                    float period = 30f;
                    float amplitude = 9f;
                    float wave = Mathf.Abs(Mathf.Repeat(p.x, period) - period * 0.5f) / (period * 0.5f) * amplitude * 2f - amplitude;
                    float m = Mathf.Repeat(p.y + wave, 34f) - 17f;
                    return Mathf.Abs(m) - 4.5f;
                }
                default:
                {
                    float cell = 34f;
                    float row = Mathf.Floor(p.y / cell);
                    float offset = Mathf.Repeat(row, 2f) * cell * 0.5f;
                    Vector2 q = new Vector2(Mathf.Repeat(p.x + offset, cell), Mathf.Repeat(p.y, cell)) - new Vector2(cell * 0.5f, cell * 0.5f);
                    return q.magnitude - 7.5f;
                }
            }
        }

        /// <summary>Ice over a frozen card; cracked ice keeps the frost but shows cracks.</summary>
        public static Canvas2D Ice(bool cracked)
        {
            var canvas = new Canvas2D(CardWidth, CardHeight);
            Color top = new Color(0.86f, 0.97f, 1f, cracked ? 0.5f : 0.72f);
            Color bottom = new Color(0.62f, 0.86f, 1f, cracked ? 0.45f : 0.68f);
            canvas.Fill(p => CardShape(p), p => Color.Lerp(bottom, top, p.y / CardHeight));
            // Diagonal gleams.
            canvas.Fill(p => Intersect(CardShape(p, 10f), Mathf.Abs((p.x - p.y * 0.6f) - 10f) - 16f), new Color(1f, 1f, 1f, 0.35f));
            canvas.Fill(p => Intersect(CardShape(p, 10f), Mathf.Abs((p.x - p.y * 0.6f) - 70f) - 6f), new Color(1f, 1f, 1f, 0.3f));
            canvas.Fill(p => Outline(CardShape(p, 5f), 8f), new Color(1f, 1f, 1f, 0.75f));
            Vector2 c = CardCenter;
            canvas.Fill(p => Flake(p, c, 54f, 7f), new Color(1f, 1f, 1f, cracked ? 0.45f : 0.85f));
            if (cracked)
            {
                Color crack = new Color(0.2f, 0.45f, 0.65f, 0.85f);
                Vector2 origin = c + new Vector2(18f, 30f);
                Vector2[][] lines =
                {
                    new[] { origin, origin + new Vector2(-40f, 50f), origin + new Vector2(-70f, 110f) },
                    new[] { origin, origin + new Vector2(55f, 20f), origin + new Vector2(95f, 70f) },
                    new[] { origin, origin + new Vector2(-10f, -60f), origin + new Vector2(-45f, -120f), origin + new Vector2(-60f, -170f) },
                    new[] { origin, origin + new Vector2(50f, -45f), origin + new Vector2(80f, -110f) },
                    new[] { origin + new Vector2(-40f, 50f), origin + new Vector2(-95f, 40f) }
                };
                foreach (Vector2[] line in lines)
                {
                    for (int i = 0; i < line.Length - 1; i++)
                    {
                        Vector2 a = line[i];
                        Vector2 b = line[i + 1];
                        canvas.Fill(p => Segment(p, a, b, 2.4f), crack);
                        canvas.Fill(p => Segment(p + new Vector2(2f, -2f), a, b, 1.2f), new Color(1f, 1f, 1f, 0.7f));
                    }
                }
            }
            return canvas;
        }

        /// <summary>The green check badge of a matched card.</summary>
        public static Canvas2D Badge()
        {
            var canvas = new Canvas2D(96, 96);
            var c = new Vector2(0.5f, 0.5f);
            canvas.FillN(p => Circle(p, c, 0.46f), Hex("#FFFFFF"));
            canvas.FillN(p => Circle(p, c, 0.39f), p => Color.Lerp(Hex("#2FA94E"), Hex("#4CD06C"), p.y));
            canvas.FillN(p => Union(Segment(p, new Vector2(0.29f, 0.5f), new Vector2(0.44f, 0.35f), 0.065f),
                Segment(p, new Vector2(0.44f, 0.35f), new Vector2(0.72f, 0.64f), 0.065f)), White);
            return canvas;
        }

        #endregion

        #region Special card faces

        /// <summary>The wild card: a rainbow star with sparkles.</summary>
        public static Canvas2D WildFace()
        {
            var canvas = new Canvas2D(512, 512);
            var c = new Vector2(0.5f, 0.48f);
            canvas.FillN(p => Circle(p, c, 0.42f), Hex("#EFE6FF"));
            canvas.FillN(p => Intersect(Circle(p, c, 0.42f), p.y - 0.42f), Hex("#E2D3FF"));
            canvas.ShadowN(p => Star(p, c, 0.36f, 5, 0.5f) - 0.02f, new Color(0.3f, 0.1f, 0.5f, 0.3f), 0.02f, new Vector2(0f, -0.02f));
            canvas.FillN(p => Star(p, c, 0.36f, 5, 0.5f) - 0.03f, White);
            canvas.FillN(p => Star(p, c, 0.36f, 5, 0.5f), p =>
            {
                float angle = Mathf.Atan2(p.y - c.y, p.x - c.x) / (Mathf.PI * 2f) + 0.5f;
                Color rainbow = Color.HSVToRGB(Mathf.Repeat(angle + 0.1f, 1f), 0.62f, 1f);
                float shade = p.y < c.y - 0.05f ? 0.9f : 1f;
                return rainbow * shade;
            });
            canvas.FillN(p => Star(p, c + new Vector2(-0.05f, 0.06f), 0.1f, 5, 0.5f), new Color(1f, 1f, 1f, 0.55f));
            DrawSparkle(canvas, new Vector2(0.18f, 0.78f), 0.08f, Hex("#FFD84A"));
            DrawSparkle(canvas, new Vector2(0.82f, 0.8f), 0.06f, Hex("#FF7AB6"));
            DrawSparkle(canvas, new Vector2(0.84f, 0.2f), 0.07f, Hex("#63C8FF"));
            return canvas;
        }

        /// <summary>The bomb: a round black bomb with a lit fuse.</summary>
        public static Canvas2D BombFace()
        {
            var canvas = new Canvas2D(512, 512);
            var c = new Vector2(0.48f, 0.42f);
            canvas.FillN(p => Circle(p, new Vector2(0.5f, 0.48f), 0.42f), Hex("#FFE2DC"));
            canvas.FillN(p => Intersect(Circle(p, new Vector2(0.5f, 0.48f), 0.42f), p.y - 0.4f), Hex("#FFD2C8"));
            // Fuse and spark.
            canvas.FillN(p => Union(Segment(p, new Vector2(0.62f, 0.64f), new Vector2(0.7f, 0.74f), 0.022f),
                Segment(p, new Vector2(0.7f, 0.74f), new Vector2(0.78f, 0.76f), 0.022f)), Hex("#8B5A2B"));
            canvas.FillN(p => Box(Rotate(p, new Vector2(0.6f, 0.62f), -40f), new Vector2(0.6f, 0.62f), new Vector2(0.07f, 0.05f), 0.02f), Hex("#5C5F72"));
            canvas.FillN(p => Circle(p, c, 0.28f), p => Color.Lerp(Hex("#2A2C3A"), Hex("#474A60"), p.y * 1.2f));
            canvas.FillN(p => Intersect(Circle(p, c, 0.28f), -(p.y - c.y + 0.12f)), new Color(0f, 0f, 0f, 0.18f));
            canvas.FillN(p => Ellipse(Rotate(p, c + new Vector2(-0.1f, 0.1f), 35f), c + new Vector2(-0.1f, 0.1f), new Vector2(0.07f, 0.04f)), new Color(1f, 1f, 1f, 0.55f));
            DrawSparkle(canvas, new Vector2(0.8f, 0.77f), 0.1f, Hex("#FFB02E"));
            DrawSparkle(canvas, new Vector2(0.8f, 0.77f), 0.05f, Hex("#FFF1A8"));
            return canvas;
        }

        /// <summary>The clock card: a green alarm clock.</summary>
        public static Canvas2D ClockFace()
        {
            var canvas = new Canvas2D(512, 512);
            var c = new Vector2(0.5f, 0.46f);
            canvas.FillN(p => Circle(p, new Vector2(0.5f, 0.48f), 0.42f), Hex("#DDF7E3"));
            canvas.FillN(p => Intersect(Circle(p, new Vector2(0.5f, 0.48f), 0.42f), p.y - 0.4f), Hex("#CBF0D4"));
            Color body = Hex("#34B25A");
            // Bells and feet.
            canvas.FillN(p => Circle(p, new Vector2(0.3f, 0.7f), 0.09f), body);
            canvas.FillN(p => Circle(p, new Vector2(0.7f, 0.7f), 0.09f), body);
            canvas.FillN(p => Segment(p, new Vector2(0.36f, 0.22f), new Vector2(0.31f, 0.16f), 0.03f), body);
            canvas.FillN(p => Segment(p, new Vector2(0.64f, 0.22f), new Vector2(0.69f, 0.16f), 0.03f), body);
            canvas.FillN(p => Circle(p, c, 0.27f), body);
            canvas.FillN(p => Circle(p, c, 0.21f), White);
            canvas.FillN(p => Intersect(Circle(p, c, 0.21f), -(p.y - c.y + 0.1f)), Hex("#EEF3F6"));
            for (int i = 0; i < 12; i++)
            {
                float angle = i * 30f * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
                float length = i % 3 == 0 ? 0.035f : 0.018f;
                Vector2 a = c + dir * 0.17f;
                Vector2 b = c + dir * (0.17f - length);
                canvas.FillN(p => Segment(p, a, b, 0.009f), Hex("#9AA3B5"));
            }
            canvas.FillN(p => Segment(p, c, c + new Vector2(0f, 0.13f), 0.018f), Hex("#2A2C3A"));
            canvas.FillN(p => Segment(p, c, c + new Vector2(0.1f, -0.03f), 0.018f), Hex("#2A2C3A"));
            canvas.FillN(p => Circle(p, c, 0.025f), Hex("#FF5A5A"));
            // A plus badge: the card gives time.
            var badge = new Vector2(0.76f, 0.26f);
            canvas.FillN(p => Circle(p, badge, 0.1f), White);
            canvas.FillN(p => Circle(p, badge, 0.08f), Hex("#FFB02E"));
            canvas.FillN(p => Union(Box(p, badge, new Vector2(0.045f, 0.013f), 0.01f), Box(p, badge, new Vector2(0.013f, 0.045f), 0.01f)), White);
            return canvas;
        }

        /// <summary>The peek card: a big friendly eye.</summary>
        public static Canvas2D PeekFace()
        {
            var canvas = new Canvas2D(512, 512);
            var c = new Vector2(0.5f, 0.47f);
            canvas.FillN(p => Circle(p, new Vector2(0.5f, 0.48f), 0.42f), Hex("#EDE3FF"));
            canvas.FillN(p => Intersect(Circle(p, new Vector2(0.5f, 0.48f), 0.42f), p.y - 0.4f), Hex("#E2D5FF"));
            Color purple = Hex("#8B5CF6");
            canvas.FillN(p => Vesica(p, c, 0.34f, 0.2f), purple);
            canvas.FillN(p => Vesica(p, c, 0.3f, 0.165f), White);
            canvas.FillN(p => Intersect(Circle(p, c, 0.14f), Vesica(p, c, 0.3f, 0.165f)), p => Color.Lerp(Hex("#3F7BF7"), Hex("#62C3FF"), (p.y - c.y + 0.14f) / 0.28f));
            canvas.FillN(p => Circle(p, c, 0.068f), Hex("#1E2233"));
            canvas.FillN(p => Circle(p, c + new Vector2(0.045f, 0.05f), 0.032f), White);
            // Lashes.
            for (int i = -2; i <= 2; i++)
            {
                float angle = (90f + i * 24f) * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 a = c + new Vector2(dir.x * 0.26f, 0.19f - (1f - Mathf.Abs(dir.x)) * 0.02f + Mathf.Abs(i) * -0.03f);
                Vector2 b = a + dir * 0.06f;
                canvas.FillN(p => Segment(p, a, b, 0.016f), purple);
            }
            DrawSparkle(canvas, new Vector2(0.8f, 0.24f), 0.08f, Hex("#FFD84A"));
            return canvas;
        }

        private static void DrawSparkle(Canvas2D canvas, Vector2 center, float size, Color color)
        {
            canvas.FillN(p => Sparkle(p, center, size), color);
        }

        /// <summary>A four pointed sparkle.</summary>
        private static float Sparkle(Vector2 p, Vector2 center, float size)
        {
            Vector2 q = new Vector2(Mathf.Abs(p.x - center.x), Mathf.Abs(p.y - center.y)) / size;
            // Astroid-like: |x|^0.5 + |y|^0.5 = 1
            float k = Mathf.Sqrt(q.x) + Mathf.Sqrt(q.y) - 1f;
            return k * size * 0.5f;
        }

        #endregion

        #region Icons

        /// <summary>A white interface icon on a transparent 128 pixel square.</summary>
        public static Canvas2D Icon(string name)
        {
            var canvas = new Canvas2D(128, 128);
            Sdf shape = IconShape(name);
            canvas.FillN(shape, White, 1.2f);
            return canvas;
        }

        public static Sdf IconShape(string name)
        {
            var c = new Vector2(0.5f, 0.5f);
            switch (name)
            {
                case "Pause":
                    return p => Union(Box(p, new Vector2(0.37f, 0.5f), new Vector2(0.075f, 0.25f), 0.05f), Box(p, new Vector2(0.63f, 0.5f), new Vector2(0.075f, 0.25f), 0.05f));
                case "Settings":
                    return p =>
                    {
                        float d = Circle(p, c, 0.25f);
                        for (int i = 0; i < 8; i++)
                        {
                            float angle = i * 45f;
                            Vector2 q = Rotate(p, c, angle);
                            d = Mathf.Min(d, Box(q, c + new Vector2(0f, 0.3f), new Vector2(0.065f, 0.07f), 0.025f));
                        }
                        return Subtract(d, Circle(p, c, 0.1f));
                    };
                case "Power":
                    return p =>
                    {
                        float ring = Subtract(Ring(p, new Vector2(0.5f, 0.46f), 0.26f, 0.09f), Box(p, new Vector2(0.5f, 0.78f), new Vector2(0.11f, 0.2f), 0f));
                        float bar = Segment(p, new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.82f), 0.05f);
                        return Union(ring, bar);
                    };
                case "Home":
                    return p =>
                    {
                        float roof = Polygon(p, new Vector2(0.16f, 0.52f), new Vector2(0.5f, 0.84f), new Vector2(0.84f, 0.52f)) - 0.02f;
                        float body = Box(p, new Vector2(0.5f, 0.36f), new Vector2(0.24f, 0.2f), 0.03f);
                        float door = Box(p, new Vector2(0.5f, 0.27f), new Vector2(0.07f, 0.11f), 0.02f);
                        return Subtract(Union(roof, body), door);
                    };
                case "Levels":
                    return p => Union(Box(p, new Vector2(0.32f, 0.32f), new Vector2(0.14f, 0.14f), 0.045f), Box(p, new Vector2(0.68f, 0.32f), new Vector2(0.14f, 0.14f), 0.045f),
                        Box(p, new Vector2(0.32f, 0.68f), new Vector2(0.14f, 0.14f), 0.045f), Box(p, new Vector2(0.68f, 0.68f), new Vector2(0.14f, 0.14f), 0.045f));
                case "Lock":
                    return p =>
                    {
                        float shackle = Intersect(Ring(p, new Vector2(0.5f, 0.58f), 0.16f, 0.075f), -(p.y - 0.58f));
                        shackle = Mathf.Min(shackle, Segment(p, new Vector2(0.34f, 0.58f), new Vector2(0.34f, 0.46f), 0.0375f));
                        shackle = Mathf.Min(shackle, Segment(p, new Vector2(0.66f, 0.58f), new Vector2(0.66f, 0.46f), 0.0375f));
                        float body = Box(p, new Vector2(0.5f, 0.34f), new Vector2(0.25f, 0.19f), 0.05f);
                        float hole = Union(Circle(p, new Vector2(0.5f, 0.37f), 0.05f), Box(p, new Vector2(0.5f, 0.28f), new Vector2(0.022f, 0.07f), 0.01f));
                        return Subtract(Union(shackle, body), hole);
                    };
                case "Heart":
                    return p => Heart(p, new Vector2(0.5f, 0.48f), 0.74f);
                case "Clock":
                    return p => Union(Ring(p, c, 0.3f, 0.09f), Segment(p, c, c + new Vector2(0f, 0.17f), 0.04f), Segment(p, c, c + new Vector2(0.13f, 0f), 0.04f));
                case "Stopwatch":
                    return p =>
                    {
                        var face = new Vector2(0.5f, 0.45f);
                        return Union(Ring(p, face, 0.28f, 0.085f), Segment(p, face, face + new Vector2(0.1f, 0.13f), 0.04f),
                            Box(p, new Vector2(0.5f, 0.82f), new Vector2(0.08f, 0.04f), 0.02f), Segment(p, new Vector2(0.5f, 0.74f), new Vector2(0.5f, 0.8f), 0.03f),
                            Segment(p, new Vector2(0.73f, 0.7f), new Vector2(0.78f, 0.75f), 0.035f));
                    };
                case "Moves":
                    return p =>
                    {
                        float left = Foot(Rotate(p, new Vector2(0.34f, 0.4f), 14f), new Vector2(0.34f, 0.4f));
                        float right = Foot(Rotate(p, new Vector2(0.66f, 0.6f), -14f), new Vector2(0.66f, 0.6f));
                        return Union(left, right);
                    };
                case "Check":
                    return p => Union(Segment(p, new Vector2(0.24f, 0.52f), new Vector2(0.42f, 0.32f), 0.075f), Segment(p, new Vector2(0.42f, 0.32f), new Vector2(0.78f, 0.7f), 0.075f));
                case "Cross":
                    return p => Union(Segment(p, new Vector2(0.28f, 0.28f), new Vector2(0.72f, 0.72f), 0.075f), Segment(p, new Vector2(0.28f, 0.72f), new Vector2(0.72f, 0.28f), 0.075f));
                case "Play":
                    return p => Polygon(p, new Vector2(0.38f, 0.29f), new Vector2(0.38f, 0.71f), new Vector2(0.73f, 0.5f)) - 0.06f;
                case "Retry":
                    return p =>
                    {
                        float arc = Subtract(Ring(p, c, 0.25f, 0.09f), Polygon(p, c, new Vector2(1f, 0.62f), new Vector2(1f, 1f), new Vector2(0.75f, 1f)));
                        float head = Polygon(p, new Vector2(0.62f, 0.62f), new Vector2(0.86f, 0.62f), new Vector2(0.78f, 0.86f)) - 0.02f;
                        return Union(arc, head);
                    };
                case "Next":
                    return p => Union(Segment(p, new Vector2(0.24f, 0.5f), new Vector2(0.6f, 0.5f), 0.07f),
                        Polygon(p, new Vector2(0.52f, 0.28f), new Vector2(0.8f, 0.5f), new Vector2(0.52f, 0.72f)) - 0.035f);
                case "Back":
                    return p => Union(Segment(p, new Vector2(0.76f, 0.5f), new Vector2(0.4f, 0.5f), 0.07f),
                        Polygon(p, new Vector2(0.48f, 0.28f), new Vector2(0.2f, 0.5f), new Vector2(0.48f, 0.72f)) - 0.035f);
                case "Infinity":
                    return p => Union(Ring(p, new Vector2(0.33f, 0.5f), 0.14f, 0.075f), Ring(p, new Vector2(0.67f, 0.5f), 0.14f, 0.075f));
                case "Sliders":
                    return p => Union(Segment(p, new Vector2(0.2f, 0.28f), new Vector2(0.8f, 0.28f), 0.03f), Segment(p, new Vector2(0.2f, 0.5f), new Vector2(0.8f, 0.5f), 0.03f),
                        Segment(p, new Vector2(0.2f, 0.72f), new Vector2(0.8f, 0.72f), 0.03f), Circle(p, new Vector2(0.36f, 0.28f), 0.075f),
                        Circle(p, new Vector2(0.66f, 0.5f), 0.075f), Circle(p, new Vector2(0.44f, 0.72f), 0.075f));
                case "Flag":
                    return p => Union(Segment(p, new Vector2(0.3f, 0.16f), new Vector2(0.3f, 0.84f), 0.04f),
                        Polygon(p, new Vector2(0.32f, 0.84f), new Vector2(0.8f, 0.7f), new Vector2(0.32f, 0.54f)) - 0.02f);
                case "Cards":
                    return p =>
                    {
                        float front = Box(Rotate(p, new Vector2(0.58f, 0.46f), -10f), new Vector2(0.58f, 0.46f), new Vector2(0.17f, 0.24f), 0.05f);
                        float backCard = Box(Rotate(p, new Vector2(0.4f, 0.54f), 12f), new Vector2(0.4f, 0.54f), new Vector2(0.17f, 0.24f), 0.05f);
                        return Union(front, Subtract(backCard, front - 0.045f));
                    };
                case "Paw":
                    return p => Paw(p, c, 0.9f);
                case "Star":
                    return p => Star(p, c, 0.4f, 5, 0.48f) - 0.02f;
                default:
                    return p => Circle(p, c, 0.3f);
            }
        }

        /// <summary>A paw print about <paramref name="size"/> across.</summary>
        public static float Paw(Vector2 p, Vector2 center, float size)
        {
            float s = size;
            float pad = SmoothUnion(Ellipse(p, center + new Vector2(0f, -0.14f * s), new Vector2(0.2f * s, 0.16f * s)),
                Ellipse(p, center + new Vector2(0f, -0.2f * s), new Vector2(0.13f * s, 0.1f * s)), 0.05f * s);
            float toes = Union(Ellipse(p, center + new Vector2(-0.27f * s, 0.06f * s), new Vector2(0.075f * s, 0.095f * s)),
                Ellipse(p, center + new Vector2(-0.1f * s, 0.2f * s), new Vector2(0.08f * s, 0.1f * s)),
                Ellipse(p, center + new Vector2(0.1f * s, 0.2f * s), new Vector2(0.08f * s, 0.1f * s)),
                Ellipse(p, center + new Vector2(0.27f * s, 0.06f * s), new Vector2(0.075f * s, 0.095f * s)));
            return Union(pad, toes);
        }

        /// <summary>A footprint: a sole with three toes above it.</summary>
        private static float Foot(Vector2 p, Vector2 center)
        {
            float sole = SmoothUnion(Ellipse(p, center + new Vector2(0f, -0.04f), new Vector2(0.095f, 0.15f)),
                Ellipse(p, center + new Vector2(0f, -0.17f), new Vector2(0.07f, 0.06f)), 0.03f);
            float toes = Union(Circle(p, center + new Vector2(-0.07f, 0.16f), 0.036f), Circle(p, center + new Vector2(0f, 0.18f), 0.04f),
                Circle(p, center + new Vector2(0.07f, 0.16f), 0.036f));
            return Union(sole, toes);
        }

        /// <summary>A six armed snowflake of <paramref name="radius"/> with arm width <paramref name="width"/>.</summary>
        public static float Flake(Vector2 p, Vector2 center, float radius, float width)
        {
            float d = float.MaxValue;
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 tip = center + dir * radius;
                d = Mathf.Min(d, Segment(p, center, tip, width * 0.5f));
                Vector2 branch = center + dir * radius * 0.55f;
                Vector2 side = new Vector2(-dir.y, dir.x);
                d = Mathf.Min(d, Segment(p, branch, branch + (dir + side) * radius * 0.25f, width * 0.4f));
                d = Mathf.Min(d, Segment(p, branch, branch + (dir - side) * radius * 0.25f, width * 0.4f));
            }
            return d;
        }

        /// <summary>A heart for the HUD: red and glossy, or an empty grey one.</summary>
        public static Canvas2D HeartIcon(bool full)
        {
            var canvas = new Canvas2D(128, 128);
            var c = new Vector2(0.5f, 0.47f);
            if (full)
            {
                canvas.FillN(p => Heart(p, c, 0.8f), Hex("#C81E45"));
                canvas.FillN(p => Heart(p, c + new Vector2(0f, 0.025f), 0.74f), p => Color.Lerp(Hex("#E8335A"), Hex("#FF6F8C"), p.y));
                canvas.FillN(p => Ellipse(Rotate(p, new Vector2(0.33f, 0.64f), -35f), new Vector2(0.33f, 0.64f), new Vector2(0.08f, 0.045f)), new Color(1f, 1f, 1f, 0.7f));
            }
            else
            {
                canvas.FillN(p => Heart(p, c, 0.8f), new Color(0.2f, 0.22f, 0.32f, 0.55f));
                canvas.FillN(p => Heart(p, c + new Vector2(0f, 0.02f), 0.66f), new Color(1f, 1f, 1f, 0.25f));
            }
            return canvas;
        }

        #endregion

        #region Panels and particles

        /// <summary>A white rounded panel, nine-sliced by the importer.</summary>
        public static Canvas2D Panel(int size, float radius)
        {
            var canvas = new Canvas2D(size, size);
            var center = new Vector2(size * 0.5f, size * 0.5f);
            canvas.Fill(p => Box(p, center, new Vector2(size * 0.5f - 1f, size * 0.5f - 1f), radius), White);
            return canvas;
        }

        /// <summary>A rounded panel with a darker lip at the bottom, like the Kenney buttons.</summary>
        public static Canvas2D PanelDepth(int size, float radius, float depth)
        {
            var canvas = new Canvas2D(size, size + Mathf.RoundToInt(depth));
            var lower = new Vector2(size * 0.5f, size * 0.5f);
            var upper = new Vector2(size * 0.5f, size * 0.5f + depth);
            canvas.Fill(p => Box(p, lower, new Vector2(size * 0.5f - 1f, size * 0.5f - 1f), radius), Hex("#C9CEDB"));
            canvas.Fill(p => Box(p, upper, new Vector2(size * 0.5f - 1f, size * 0.5f - 1f), radius), White);
            return canvas;
        }

        public static Canvas2D Pill(int width, int height)
        {
            var canvas = new Canvas2D(width, height);
            var center = new Vector2(width * 0.5f, height * 0.5f);
            canvas.Fill(p => Box(p, center, new Vector2(width * 0.5f - 1f, height * 0.5f - 1f), height * 0.5f - 1f), White);
            return canvas;
        }

        public static Canvas2D Disc(int size)
        {
            var canvas = new Canvas2D(size, size);
            canvas.FillN(p => Circle(p, new Vector2(0.5f, 0.5f), 0.49f), White);
            return canvas;
        }

        /// <summary>A soft round glow.</summary>
        public static Canvas2D Soft(int size)
        {
            var canvas = new Canvas2D(size, size);
            canvas.FillN(p => Circle(p, new Vector2(0.5f, 0.5f), 0.5f), p =>
            {
                float d = (p - new Vector2(0.5f, 0.5f)).magnitude / 0.5f;
                float a = Mathf.Clamp01(1f - d);
                return new Color(1f, 1f, 1f, a * a);
            });
            return canvas;
        }

        public static Canvas2D Particle(string name)
        {
            switch (name)
            {
                case "Spark":
                {
                    var canvas = new Canvas2D(64, 64);
                    canvas.FillN(p => Circle(p, new Vector2(0.5f, 0.5f), 0.45f), p =>
                    {
                        float d = (p - new Vector2(0.5f, 0.5f)).magnitude / 0.45f;
                        return new Color(1f, 1f, 1f, Mathf.Clamp01(1f - d) * 0.5f);
                    });
                    canvas.FillN(p => Sparkle(p, new Vector2(0.5f, 0.5f), 0.46f), White);
                    return canvas;
                }
                case "Star":
                {
                    var canvas = new Canvas2D(64, 64);
                    canvas.FillN(p => Star(p, new Vector2(0.5f, 0.5f), 0.46f, 5, 0.5f) - 0.02f, White);
                    return canvas;
                }
                case "Confetti":
                {
                    var canvas = new Canvas2D(32, 32);
                    canvas.FillN(p => Box(p, new Vector2(0.5f, 0.5f), new Vector2(0.44f, 0.24f), 0.08f), White);
                    return canvas;
                }
                case "Shard":
                {
                    var canvas = new Canvas2D(48, 48);
                    canvas.FillN(p => Polygon(p, new Vector2(0.18f, 0.2f), new Vector2(0.86f, 0.36f), new Vector2(0.4f, 0.86f)), White);
                    return canvas;
                }
                default:
                {
                    var canvas = new Canvas2D(64, 64);
                    canvas.FillN(p => Circle(p, new Vector2(0.5f, 0.5f), 0.48f), p =>
                    {
                        float d = (p - new Vector2(0.5f, 0.5f)).magnitude / 0.48f;
                        return new Color(1f, 1f, 1f, Mathf.Clamp01((1f - d) * 2.2f));
                    });
                    return canvas;
                }
            }
        }

        #endregion

        #region Backdrop

        public static Canvas2D Cloud()
        {
            var canvas = new Canvas2D(256, 150);
            float s = 256f;
            Sdf shape = p =>
            {
                Vector2 q = p / s;
                float d = Union(Circle(q, new Vector2(0.3f, 0.26f), 0.16f), Circle(q, new Vector2(0.5f, 0.33f), 0.2f),
                    Circle(q, new Vector2(0.7f, 0.26f), 0.15f), Box(q, new Vector2(0.5f, 0.18f), new Vector2(0.34f, 0.08f), 0.08f));
                return d * s;
            };
            canvas.Fill(shape, White);
            canvas.Fill(p => Intersect(shape(p), -(p.y - 40f)), new Color(0.88f, 0.92f, 1f));
            return canvas;
        }

        public static Canvas2D Leaf()
        {
            var canvas = new Canvas2D(128, 128);
            var c = new Vector2(0.5f, 0.5f);
            Sdf leaf = p => Vesica(Rotate(p, c, 40f), c, 0.42f, 0.2f);
            canvas.FillN(leaf, White);
            canvas.FillN(p => Intersect(leaf(p), Segment(Rotate(p, c, 40f), new Vector2(0.14f, 0.5f), new Vector2(0.86f, 0.5f), 0.018f)), new Color(0.75f, 0.85f, 0.75f));
            return canvas;
        }

        public static Canvas2D Snowflake()
        {
            var canvas = new Canvas2D(128, 128);
            canvas.FillN(p => Flake(p, new Vector2(0.5f, 0.5f), 0.42f, 0.07f), White);
            return canvas;
        }

        public static Canvas2D Balloon()
        {
            var canvas = new Canvas2D(128, 176);
            float s = 128f;
            canvas.Fill(p =>
            {
                Vector2 q = p / s;
                return Union(Ellipse(q, new Vector2(0.5f, 0.86f), new Vector2(0.36f, 0.44f)),
                    Polygon(q, new Vector2(0.44f, 0.4f), new Vector2(0.56f, 0.4f), new Vector2(0.5f, 0.46f))) * s;
            }, White);
            canvas.Fill(p => Segment(p / s, new Vector2(0.5f, 0.4f), new Vector2(0.46f, 0.02f), 0.012f) * s, new Color(1f, 1f, 1f, 0.8f));
            canvas.Fill(p => Ellipse(p / s, new Vector2(0.38f, 1.02f), new Vector2(0.07f, 0.12f)) * s, new Color(0.9f, 0.93f, 1f));
            return canvas;
        }

        /// <summary>A tile of scattered paw prints and dots for the scrolling backdrop pattern.</summary>
        public static Canvas2D Pattern()
        {
            const int size = 256;
            var canvas = new Canvas2D(size, size);
            var paws = new[] { new Vector3(64f, 70f, 20f), new Vector3(190f, 180f, -25f), new Vector3(200f, 40f, 10f), new Vector3(70f, 200f, -8f) };
            var dots = new[] { new Vector2(128f, 128f), new Vector2(20f, 140f), new Vector2(150f, 240f), new Vector2(240f, 110f), new Vector2(120f, 10f) };
            canvas.Fill(p =>
            {
                float d = float.MaxValue;
                for (int ox = -1; ox <= 1; ox++)
                {
                    for (int oy = -1; oy <= 1; oy++)
                    {
                        Vector2 q = p + new Vector2(ox * size, oy * size);
                        foreach (Vector3 paw in paws)
                        {
                            var center = new Vector2(paw.x, paw.y);
                            d = Mathf.Min(d, Paw(Rotate(q, center, paw.z), center, 46f));
                        }
                        foreach (Vector2 dot in dots)
                        {
                            d = Mathf.Min(d, Circle(q, dot, 6f));
                        }
                    }
                }
                return d;
            }, White);
            return canvas;
        }

        #endregion

        /// <summary>
        /// The launcher icon: a card back of the farm behind a card showing <paramref name="animal"/>.
        /// </summary>
        public static Canvas2D LauncherIcon(Color[] animal, int animalSize, Color backColor, Color backDark)
        {
            const int size = 256;
            var canvas = new Canvas2D(size, size);
            var backCenter = new Vector2(98f, 138f);
            var frontCenter = new Vector2(152f, 118f);
            Vector2 half = new Vector2(64f, 86f);
            canvas.Shadow(p => Box(Rotate(p, backCenter, 10f), backCenter, half, 16f), new Color(0f, 0f, 0f, 0.3f), 8f, new Vector2(0f, -6f));
            canvas.Fill(p => Box(Rotate(p, backCenter, 10f), backCenter, half, 16f), backColor);
            canvas.Fill(p => Outline(Box(Rotate(p, backCenter, 10f), backCenter, half - new Vector2(8f, 8f), 12f), 3f), new Color(1f, 1f, 1f, 0.6f));
            canvas.Fill(p => Paw(Rotate(p, backCenter, 10f), backCenter, 60f), new Color(1f, 1f, 1f, 0.9f));
            canvas.Shadow(p => Box(p, frontCenter, half, 16f), new Color(0f, 0f, 0f, 0.35f), 8f, new Vector2(0f, -6f));
            canvas.Fill(p => Box(p, frontCenter, half, 16f), Hex("#FFFDF8"));
            canvas.Fill(p => Outline(Box(p, frontCenter, half - new Vector2(3f, 3f), 14f), 4f), backDark);
            float stamp = 112f;
            canvas.Stamp(animal, animalSize, animalSize, frontCenter.x - stamp * 0.5f, frontCenter.y - stamp * 0.5f, stamp, stamp);
            return canvas;
        }
    }
}
