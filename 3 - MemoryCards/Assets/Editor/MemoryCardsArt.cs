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
        Zigzag,
        /// <summary>A diagonal lattice with a bud in every cell (velvet wallpaper).</summary>
        Damask,
        /// <summary>Rows of scallops and dots.</summary>
        Lace,
        /// <summary>Rays from the bottom of the card (an art deco fan).</summary>
        Sunburst,
        /// <summary>Wide zigzags.</summary>
        Chevron
    }


    /// <summary>
    /// Draws the generated art of Memory Cards: card fronts, backs, shadows and glows, the ice overlays, the faces of
    /// the special cards, the interface icons, hearts, panels, particles, backdrop shapes and the launcher icon. The
    /// colours and the frames come from an <see cref="ArtStyle"/>, so the same drawings serve every theme (Kenney's
    /// flat rounded style, or black and gold with art deco frames). Every method returns a canvas; the art builder saves them.
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

        /// <summary>The face side of every card: warm white with a soft border, or black velvet in a gold deco frame.</summary>
        public static Canvas2D CardFront(ArtStyle style)
        {
            var canvas = new Canvas2D(CardWidth, CardHeight);
            canvas.Fill(p => CardShape(p), p => Color.Lerp(style.FrontBottom, style.FrontTop, p.y / CardHeight));
            if (style.Deco)
            {
                canvas.Fill(p => Outline(CardShape(p, 11f), 2f), style.FrontInner);
                DecoCorners(canvas, 11f, 30f, style.FrontInner);
                canvas.Fill(p => Outline(CardShape(p, 2.5f), 3f), style.FrontOuter);
            }
            else
            {
                canvas.Fill(p => Outline(CardShape(p, 7f), 4f), style.FrontInner);
                canvas.Fill(p => Outline(CardShape(p, 2.5f), 3f), style.FrontOuter);
            }
            return canvas;
        }

        /// <summary>Gold brackets in the corners of a card, inside a border at <paramref name="inset"/>.</summary>
        private static void DecoCorners(Canvas2D canvas, float inset, float length, Color color)
        {
            float gap = inset + 6f;
            var corners = new[]
            {
                new Vector2(gap, gap), new Vector2(CardWidth - gap, gap), new Vector2(gap, CardHeight - gap), new Vector2(CardWidth - gap, CardHeight - gap)
            };
            foreach (Vector2 corner in corners)
            {
                float sx = corner.x < CardWidth * 0.5f ? 1f : -1f;
                float sy = corner.y < CardHeight * 0.5f ? 1f : -1f;
                Vector2 c = corner + new Vector2(sx * 6f, sy * 6f);
                canvas.Fill(p => Union(Segment(p, c, c + new Vector2(sx * length, 0f), 1.6f), Segment(p, c, c + new Vector2(0f, sy * length), 1.6f)), color);
                Vector2 gem = c + new Vector2(sx * 9f, sy * 9f);
                canvas.Fill(p => Box(Rotate(p, gem, 45f), gem, new Vector2(4f, 4f)), color);
            }
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

        /// <summary>The back of a world's cards: its colour, a printed pattern, a border and a medallion with the world's emblem.</summary>
        public static Canvas2D CardBack(Color color, Color dark, CardPattern pattern, CardEmblem emblem, ArtStyle style, bool medallion)
        {
            var canvas = new Canvas2D(CardWidth, CardHeight);
            Color light = Color.Lerp(color, style.Deco ? style.Gold : Color.white, style.Deco ? 0.12f : 0.18f);
            canvas.Fill(p => CardShape(p), p => Color.Lerp(color, light, p.y / CardHeight));
            // The printed pattern, inside the inner border.
            Sdf inner = p => CardShape(p, 16f);
            canvas.Fill(p => Intersect(inner(p), PatternShape(pattern, p)), style.BackInk);
            canvas.Fill(p => Outline(CardShape(p, 14f), style.Deco ? 2f : 4f), style.BackInner);
            if (style.Deco)
            {
                DecoCorners(canvas, 14f, 26f, style.BackInner);
            }
            canvas.Fill(p => Outline(CardShape(p, 2.5f), 4f), style.BackOuter.a > 0f ? style.BackOuter : dark);
            if (medallion)
            {
                Vector2 c = CardCenter;
                Color ring = style.MedallionRing.a > 0f ? style.MedallionRing : Color.Lerp(dark, color, 0.4f);
                Color mark = style.Emblem.a > 0f ? style.Emblem : color;
                canvas.Shadow(p => Circle(p, c, 62f), new Color(0f, 0f, 0f, 0.25f), 8f, new Vector2(0f, -5f));
                canvas.Fill(p => Circle(p, c, 62f), style.MedallionFill);
                canvas.Fill(p => Outline(Circle(p, c, 62f), 5f), ring);
                if (style.Deco)
                {
                    canvas.Fill(p => Outline(Circle(p, c, 52f), 1.5f), new Color(ring.r, ring.g, ring.b, 0.5f));
                }
                canvas.Fill(p => EmblemShape(emblem, p, c, 78f), mark);
            }
            return canvas;
        }

        /// <summary>The emblem of a world about <paramref name="size"/> across, centred at <paramref name="c"/>.</summary>
        public static float EmblemShape(CardEmblem emblem, Vector2 p, Vector2 c, float size)
        {
            float s = size;
            switch (emblem)
            {
                case CardEmblem.Diamond:
                {
                    float crown = Polygon(p, c + new Vector2(-0.6f * s, 0.12f * s), c + new Vector2(-0.34f * s, 0.4f * s), c + new Vector2(0.34f * s, 0.4f * s), c + new Vector2(0.6f * s, 0.12f * s));
                    float pavilion = Polygon(p, c + new Vector2(-0.6f * s, 0.12f * s), c + new Vector2(0.6f * s, 0.12f * s), c + new Vector2(0f, -0.5f * s));
                    float facets = Union(Segment(p, c + new Vector2(-0.6f * s, 0.12f * s), c + new Vector2(0.6f * s, 0.12f * s), 0.02f * s),
                        Segment(p, c + new Vector2(-0.3f * s, 0.12f * s), c + new Vector2(0f, -0.5f * s), 0.02f * s),
                        Segment(p, c + new Vector2(0.3f * s, 0.12f * s), c + new Vector2(0f, -0.5f * s), 0.02f * s),
                        Segment(p, c + new Vector2(-0.34f * s, 0.4f * s), c + new Vector2(-0.3f * s, 0.12f * s), 0.02f * s),
                        Segment(p, c + new Vector2(0.34f * s, 0.4f * s), c + new Vector2(0.3f * s, 0.12f * s), 0.02f * s));
                    return Subtract(Union(crown, pavilion), facets);
                }
                case CardEmblem.Lips:
                {
                    float upper = Union(Ellipse(p, c + new Vector2(-0.27f * s, 0.1f * s), new Vector2(0.38f * s, 0.24f * s)),
                        Ellipse(p, c + new Vector2(0.27f * s, 0.1f * s), new Vector2(0.38f * s, 0.24f * s)));
                    float lower = Ellipse(p, c + new Vector2(0f, -0.1f * s), new Vector2(0.64f * s, 0.3f * s));
                    float slit = Box(p, c, new Vector2(0.58f * s, 0.022f * s), 0.02f * s);
                    return Subtract(Union(upper, lower), slit);
                }
                case CardEmblem.Mask:
                {
                    float face = Ellipse(p, c, new Vector2(0.7f * s, 0.3f * s));
                    float peaks = Union(Polygon(p, c + new Vector2(-0.55f * s, 0.12f * s), c + new Vector2(-0.3f * s, 0.5f * s), c + new Vector2(-0.1f * s, 0.2f * s)),
                        Polygon(p, c + new Vector2(0.55f * s, 0.12f * s), c + new Vector2(0.3f * s, 0.5f * s), c + new Vector2(0.1f * s, 0.2f * s)));
                    float eyes = Union(Vesica(p, c + new Vector2(-0.3f * s, 0.02f * s), 0.2f * s, 0.1f * s), Vesica(p, c + new Vector2(0.3f * s, 0.02f * s), 0.2f * s, 0.1f * s));
                    return Subtract(Union(face, peaks), eyes);
                }
                case CardEmblem.Star:
                    return Star(p, c, 0.5f * s, 5, 0.45f);
                default:
                    return Paw(p, c, s);
            }
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
                case CardPattern.Damask:
                {
                    float cell = 48f;
                    float u = (p.x + p.y) / Mathf.Sqrt(2f);
                    float v = (p.x - p.y) / Mathf.Sqrt(2f);
                    float lattice = Mathf.Min(Mathf.Abs(Mathf.Repeat(u, cell) - cell * 0.5f), Mathf.Abs(Mathf.Repeat(v, cell) - cell * 0.5f)) - 1.2f;
                    Vector2 q = new Vector2(Mathf.Repeat(u, cell), Mathf.Repeat(v, cell)) - new Vector2(cell * 0.5f, cell * 0.5f);
                    float bud = Union(Circle(q, new Vector2(0f, 5f), 4f), Circle(q, new Vector2(0f, -5f), 4f), Circle(q, new Vector2(5f, 0f), 4f), Circle(q, new Vector2(-5f, 0f), 4f));
                    return Union(lattice, bud);
                }
                case CardPattern.Lace:
                {
                    float cell = 30f;
                    float row = Mathf.Floor(p.y / cell);
                    float offset = Mathf.Repeat(row, 2f) * cell * 0.5f;
                    Vector2 q = new Vector2(Mathf.Repeat(p.x + offset, cell), Mathf.Repeat(p.y, cell)) - new Vector2(cell * 0.5f, cell * 0.5f);
                    float scallop = Mathf.Abs(Circle(q, new Vector2(0f, -4f), 10f)) - 1.2f;
                    float dot = Circle(q, new Vector2(0f, 9f), 2.2f);
                    return Union(Intersect(scallop, -(q.y + 4f)), dot);
                }
                case CardPattern.Sunburst:
                {
                    var origin = new Vector2(CardWidth * 0.5f, -40f);
                    Vector2 d = p - origin;
                    float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
                    float m = Mathf.Repeat(angle, 12f) - 6f;
                    float rays = Mathf.Abs(m) * Mathf.Deg2Rad * d.magnitude - 2.2f;
                    float arcs = Mathf.Abs(Mathf.Repeat(d.magnitude, 70f) - 35f) - 1.2f;
                    return Union(rays, arcs);
                }
                case CardPattern.Chevron:
                {
                    float period = 64f;
                    float amplitude = 14f;
                    float wave = Mathf.Abs(Mathf.Repeat(p.x, period) - period * 0.5f) / (period * 0.5f) * amplitude * 2f - amplitude;
                    float m = Mathf.Repeat(p.y + wave, 40f) - 20f;
                    return Mathf.Abs(m) - 2.5f;
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
        public static Canvas2D Ice(bool cracked, ArtStyle style)
        {
            var canvas = new Canvas2D(CardWidth, CardHeight);
            Color top = new Color(style.IceTop.r, style.IceTop.g, style.IceTop.b, cracked ? 0.5f : 0.72f);
            Color bottom = new Color(style.IceBottom.r, style.IceBottom.g, style.IceBottom.b, cracked ? 0.45f : 0.68f);
            canvas.Fill(p => CardShape(p), p => Color.Lerp(bottom, top, p.y / CardHeight));
            // Diagonal gleams.
            canvas.Fill(p => Intersect(CardShape(p, 10f), Mathf.Abs((p.x - p.y * 0.6f) - 10f) - 16f), new Color(1f, 1f, 1f, 0.35f));
            canvas.Fill(p => Intersect(CardShape(p, 10f), Mathf.Abs((p.x - p.y * 0.6f) - 70f) - 6f), new Color(1f, 1f, 1f, 0.3f));
            canvas.Fill(p => Outline(CardShape(p, 5f), 8f), new Color(1f, 1f, 1f, 0.75f));
            Vector2 c = CardCenter;
            canvas.Fill(p => Flake(p, c, 54f, 7f), new Color(1f, 1f, 1f, cracked ? 0.45f : 0.85f));
            if (cracked)
            {
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
                        canvas.Fill(p => Segment(p, a, b, 2.4f), style.IceCrack);
                        canvas.Fill(p => Segment(p + new Vector2(2f, -2f), a, b, 1.2f), new Color(1f, 1f, 1f, 0.7f));
                    }
                }
            }
            return canvas;
        }

        /// <summary>The check badge of a matched card.</summary>
        public static Canvas2D Badge(ArtStyle style)
        {
            var canvas = new Canvas2D(96, 96);
            var c = new Vector2(0.5f, 0.5f);
            canvas.FillN(p => Circle(p, c, 0.46f), style.BadgeRing);
            canvas.FillN(p => Circle(p, c, 0.39f), p => Color.Lerp(style.BadgeBottom, style.BadgeTop, p.y));
            canvas.FillN(p => Union(Segment(p, new Vector2(0.29f, 0.5f), new Vector2(0.44f, 0.35f), 0.065f),
                Segment(p, new Vector2(0.44f, 0.35f), new Vector2(0.72f, 0.64f), 0.065f)), style.BadgeMark);
            return canvas;
        }

        #endregion

        #region Special card faces

        /// <summary>The disc behind a special card's picture (its lower part in a shade), with the style's ring around it.</summary>
        private static void FaceDisc(Canvas2D canvas, ArtStyle style, Color color, Color lower, float shadeBelow = 0.4f)
        {
            var c = new Vector2(0.5f, 0.48f);
            canvas.FillN(p => Circle(p, c, 0.42f), color);
            canvas.FillN(p => Intersect(Circle(p, c, 0.42f), p.y - shadeBelow), lower);
            if (style.DiscRing.a > 0f)
            {
                canvas.FillN(p => Outline(Circle(p, c, 0.41f), 0.02f), style.DiscRing);
            }
        }

        /// <summary>The wild card: a rainbow (or gold) star with sparkles.</summary>
        public static Canvas2D WildFace(ArtStyle style)
        {
            var canvas = new Canvas2D(512, 512);
            var c = new Vector2(0.5f, 0.48f);
            FaceDisc(canvas, style, style.WildDisc, style.WildDiscLow, 0.42f);
            canvas.ShadowN(p => Star(p, c, 0.36f, 5, 0.5f) - 0.02f, new Color(0.3f, 0.1f, 0.5f, 0.3f), 0.02f, new Vector2(0f, -0.02f));
            canvas.FillN(p => Star(p, c, 0.36f, 5, 0.5f) - 0.03f, style.Deco ? style.GoldDark : White);
            canvas.FillN(p => Star(p, c, 0.36f, 5, 0.5f), p =>
            {
                float angle = Mathf.Atan2(p.y - c.y, p.x - c.x) / (Mathf.PI * 2f) + 0.5f;
                float shade = p.y < c.y - 0.05f ? 0.9f : 1f;
                if (style.WildStar.a > 0f)
                {
                    return Color.Lerp(style.WildStar, style.GoldLight, Mathf.Clamp01((p.y - c.y + 0.36f) / 0.72f)) * shade;
                }
                Color rainbow = Color.HSVToRGB(Mathf.Repeat(angle + 0.1f, 1f), 0.62f, 1f);
                return rainbow * shade;
            });
            canvas.FillN(p => Star(p, c + new Vector2(-0.05f, 0.06f), 0.1f, 5, 0.5f), new Color(1f, 1f, 1f, 0.55f));
            DrawSparkle(canvas, new Vector2(0.18f, 0.78f), 0.08f, style.Deco ? style.GoldLight : Hex("#FFD84A"));
            DrawSparkle(canvas, new Vector2(0.82f, 0.8f), 0.06f, style.Deco ? Hex("#F4E7CF") : Hex("#FF7AB6"));
            DrawSparkle(canvas, new Vector2(0.84f, 0.2f), 0.07f, style.Deco ? style.GoldLight : Hex("#63C8FF"));
            return canvas;
        }

        /// <summary>The bomb: a round black bomb with a lit fuse.</summary>
        public static Canvas2D BombFace(ArtStyle style)
        {
            var canvas = new Canvas2D(512, 512);
            var c = new Vector2(0.48f, 0.42f);
            FaceDisc(canvas, style, style.BombDisc, style.BombDiscLow);
            // Fuse and spark.
            canvas.FillN(p => Union(Segment(p, new Vector2(0.62f, 0.64f), new Vector2(0.7f, 0.74f), 0.022f),
                Segment(p, new Vector2(0.7f, 0.74f), new Vector2(0.78f, 0.76f), 0.022f)), Hex("#8B5A2B"));
            canvas.FillN(p => Box(Rotate(p, new Vector2(0.6f, 0.62f), -40f), new Vector2(0.6f, 0.62f), new Vector2(0.07f, 0.05f), 0.02f), style.Deco ? style.GoldDark : Hex("#5C5F72"));
            canvas.FillN(p => Circle(p, c, 0.28f), p => Color.Lerp(Hex("#2A2C3A"), Hex("#474A60"), p.y * 1.2f));
            canvas.FillN(p => Intersect(Circle(p, c, 0.28f), -(p.y - c.y + 0.12f)), new Color(0f, 0f, 0f, 0.18f));
            if (style.Deco)
            {
                canvas.FillN(p => Outline(Circle(p, c, 0.28f), 0.012f), style.Gold);
            }
            canvas.FillN(p => Ellipse(Rotate(p, c + new Vector2(-0.1f, 0.1f), 35f), c + new Vector2(-0.1f, 0.1f), new Vector2(0.07f, 0.04f)), new Color(1f, 1f, 1f, 0.55f));
            DrawSparkle(canvas, new Vector2(0.8f, 0.77f), 0.1f, Hex("#FFB02E"));
            DrawSparkle(canvas, new Vector2(0.8f, 0.77f), 0.05f, Hex("#FFF1A8"));
            return canvas;
        }

        /// <summary>The clock card: an alarm clock (a gilded one in the deco style).</summary>
        public static Canvas2D ClockFace(ArtStyle style)
        {
            var canvas = new Canvas2D(512, 512);
            var c = new Vector2(0.5f, 0.46f);
            FaceDisc(canvas, style, style.ClockDisc, style.ClockDiscLow);
            Color body = style.ClockBody;
            // Bells and feet.
            canvas.FillN(p => Circle(p, new Vector2(0.3f, 0.7f), 0.09f), body);
            canvas.FillN(p => Circle(p, new Vector2(0.7f, 0.7f), 0.09f), body);
            canvas.FillN(p => Segment(p, new Vector2(0.36f, 0.22f), new Vector2(0.31f, 0.16f), 0.03f), body);
            canvas.FillN(p => Segment(p, new Vector2(0.64f, 0.22f), new Vector2(0.69f, 0.16f), 0.03f), body);
            canvas.FillN(p => Circle(p, c, 0.27f), body);
            canvas.FillN(p => Circle(p, c, 0.21f), style.Deco ? Hex("#F4E7CF") : White);
            canvas.FillN(p => Intersect(Circle(p, c, 0.21f), -(p.y - c.y + 0.1f)), style.Deco ? Hex("#E6D6B4") : Hex("#EEF3F6"));
            for (int i = 0; i < 12; i++)
            {
                float angle = i * 30f * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
                float length = i % 3 == 0 ? 0.035f : 0.018f;
                Vector2 a = c + dir * 0.17f;
                Vector2 b = c + dir * (0.17f - length);
                canvas.FillN(p => Segment(p, a, b, 0.009f), style.Deco ? style.GoldDark : Hex("#9AA3B5"));
            }
            canvas.FillN(p => Segment(p, c, c + new Vector2(0f, 0.13f), 0.018f), style.ClockHands);
            canvas.FillN(p => Segment(p, c, c + new Vector2(0.1f, -0.03f), 0.018f), style.ClockHands);
            canvas.FillN(p => Circle(p, c, 0.025f), Hex("#FF5A5A"));
            // A plus badge: the card gives time.
            var badge = new Vector2(0.76f, 0.26f);
            canvas.FillN(p => Circle(p, badge, 0.1f), style.Deco ? style.GoldLight : White);
            canvas.FillN(p => Circle(p, badge, 0.08f), style.Deco ? Hex("#8E1631") : Hex("#FFB02E"));
            canvas.FillN(p => Union(Box(p, badge, new Vector2(0.045f, 0.013f), 0.01f), Box(p, badge, new Vector2(0.013f, 0.045f), 0.01f)), style.Deco ? style.GoldLight : White);
            return canvas;
        }

        /// <summary>The peek card: a big friendly eye.</summary>
        public static Canvas2D PeekFace(ArtStyle style)
        {
            var canvas = new Canvas2D(512, 512);
            var c = new Vector2(0.5f, 0.47f);
            FaceDisc(canvas, style, style.PeekDisc, style.PeekDiscLow);
            Color lid = style.PeekLid;
            canvas.FillN(p => Vesica(p, c, 0.34f, 0.2f), lid);
            canvas.FillN(p => Vesica(p, c, 0.3f, 0.165f), style.Deco ? Hex("#F4E7CF") : White);
            canvas.FillN(p => Intersect(Circle(p, c, 0.14f), Vesica(p, c, 0.3f, 0.165f)), p => Color.Lerp(style.PeekIris, style.PeekIrisLight, (p.y - c.y + 0.14f) / 0.28f));
            canvas.FillN(p => Circle(p, c, 0.068f), Hex("#1E2233"));
            canvas.FillN(p => Circle(p, c + new Vector2(0.045f, 0.05f), 0.032f), White);
            // Lashes.
            for (int i = -2; i <= 2; i++)
            {
                float angle = (90f + i * 24f) * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 a = c + new Vector2(dir.x * 0.26f, 0.19f - (1f - Mathf.Abs(dir.x)) * 0.02f + Mathf.Abs(i) * -0.03f);
                Vector2 b = a + dir * 0.06f;
                canvas.FillN(p => Segment(p, a, b, 0.016f), lid);
            }
            DrawSparkle(canvas, new Vector2(0.8f, 0.24f), 0.08f, style.Deco ? style.GoldLight : Hex("#FFD84A"));
            return canvas;
        }

        private static void DrawSparkle(Canvas2D canvas, Vector2 center, float size, Color color)
        {
            canvas.FillN(p => Sparkle(p, center, size), color);
        }

        /// <summary>A four pointed sparkle.</summary>
        public static float Sparkle(Vector2 p, Vector2 center, float size)
        {
            Vector2 q = new Vector2(Mathf.Abs(p.x - center.x), Mathf.Abs(p.y - center.y)) / size;
            // Astroid-like: |x|^0.5 + |y|^0.5 = 1
            float k = Mathf.Sqrt(q.x) + Mathf.Sqrt(q.y) - 1f;
            return k * size * 0.5f;
        }

        #endregion

        #region Icons

        /// <summary>A white interface icon on a transparent 128 pixel square.</summary>
        public static Canvas2D Icon(string name, ArtStyle style)
        {
            var canvas = new Canvas2D(128, 128);
            Sdf shape = IconShape(name, style.Deco);
            canvas.FillN(shape, White, 1.2f);
            return canvas;
        }

        /// <summary>The shape of an icon; the deco set trades the paw print for a gem.</summary>
        public static Sdf IconShape(string name, bool deco = false)
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
                    return deco ? (Sdf)(p => EmblemShape(CardEmblem.Diamond, p, c, 0.8f)) : p => Paw(p, c, 0.9f);
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

        /// <summary>A heart for the HUD: red and glossy, or an empty one.</summary>
        public static Canvas2D HeartIcon(bool full, ArtStyle style)
        {
            var canvas = new Canvas2D(128, 128);
            var c = new Vector2(0.5f, 0.47f);
            if (full)
            {
                canvas.FillN(p => Heart(p, c, 0.8f), style.HeartRim);
                canvas.FillN(p => Heart(p, c + new Vector2(0f, 0.025f), 0.74f), p => Color.Lerp(style.HeartBottom, style.HeartTop, p.y));
                canvas.FillN(p => Ellipse(Rotate(p, new Vector2(0.33f, 0.64f), -35f), new Vector2(0.33f, 0.64f), new Vector2(0.08f, 0.045f)), new Color(1f, 1f, 1f, 0.7f));
            }
            else
            {
                canvas.FillN(p => Heart(p, c, 0.8f), style.HeartEmpty);
                canvas.FillN(p => Heart(p, c + new Vector2(0f, 0.02f), 0.66f), new Color(1f, 1f, 1f, style.Deco ? 0.1f : 0.25f));
            }
            return canvas;
        }

        #endregion

        #region Panels, kit and particles

        /// <summary>A rounded panel (white, or the style's dark velvet in a gold rim), nine-sliced by the importer.</summary>
        public static Canvas2D Panel(int size, float radius, ArtStyle style)
        {
            var canvas = new Canvas2D(size, size);
            var center = new Vector2(size * 0.5f, size * 0.5f);
            Sdf shape = p => Box(p, center, new Vector2(size * 0.5f - 1f, size * 0.5f - 1f), radius);
            canvas.Fill(shape, style.PanelFill);
            Rim(canvas, shape, style, 3f);
            return canvas;
        }

        /// <summary>A rounded panel with a darker lip at the bottom, like the Kenney buttons.</summary>
        public static Canvas2D PanelDepth(int size, float radius, float depth, ArtStyle style)
        {
            var canvas = new Canvas2D(size, size + Mathf.RoundToInt(depth));
            var lower = new Vector2(size * 0.5f, size * 0.5f);
            var upper = new Vector2(size * 0.5f, size * 0.5f + depth);
            Vector2 half = new Vector2(size * 0.5f - 1f, size * 0.5f - 1f);
            canvas.Fill(p => Box(p, lower, half, radius), style.PanelLip);
            Sdf face = p => Box(p, upper, half, radius);
            canvas.Fill(face, style.PanelFill);
            Rim(canvas, face, style, 3f);
            return canvas;
        }

        public static Canvas2D Pill(int width, int height, ArtStyle style)
        {
            var canvas = new Canvas2D(width, height);
            var center = new Vector2(width * 0.5f, height * 0.5f);
            Sdf shape = p => Box(p, center, new Vector2(width * 0.5f - 1f, height * 0.5f - 1f), height * 0.5f - 1f);
            canvas.Fill(shape, style.PanelFill);
            Rim(canvas, shape, style, 2.5f);
            return canvas;
        }

        public static Canvas2D Disc(int size, ArtStyle style)
        {
            var canvas = new Canvas2D(size, size);
            Sdf shape = p => Circle(p, new Vector2(size * 0.5f, size * 0.5f), size * 0.49f);
            canvas.Fill(shape, style.PanelFill);
            Rim(canvas, shape, style, 3f);
            return canvas;
        }

        /// <summary>The style's rim along the inside of a shape, when it has one.</summary>
        private static void Rim(Canvas2D canvas, Sdf shape, ArtStyle style, float width)
        {
            if (style.PanelRim.a > 0f)
            {
                canvas.Fill(p => Outline(shape(p) + width * 0.5f + 1f, width), style.PanelRim);
            }
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

        /// <summary>
        /// A white panel with a lip, for the game to tint (world tabs, seat frames): the lip and an engraved line inside
        /// are grey, so they come out as darker shades of the tint.
        /// </summary>
        public static Canvas2D TintPanel(ArtStyle style)
        {
            const int size = 128;
            const float depth = 10f;
            const float radius = 30f;
            var canvas = new Canvas2D(size, size + (int)depth);
            var lower = new Vector2(size * 0.5f, size * 0.5f);
            var upper = new Vector2(size * 0.5f, size * 0.5f + depth);
            Vector2 half = new Vector2(size * 0.5f - 1f, size * 0.5f - 1f);
            canvas.Fill(p => Box(p, lower, half, radius), new Color(0.62f, 0.6f, 0.6f));
            Sdf face = p => Box(p, upper, half, radius);
            canvas.Fill(face, White);
            canvas.Fill(p => Outline(face(p) + 8f, 2f), new Color(0.66f, 0.64f, 0.64f));
            return canvas;
        }

        /// <summary>A white pill with an engraved line inside, for the game to tint (ribbons, badges, bars).</summary>
        public static Canvas2D TintPill(ArtStyle style)
        {
            var canvas = new Canvas2D(128, 64);
            var center = new Vector2(64f, 32f);
            Sdf shape = p => Box(p, center, new Vector2(63f, 31f), 31f);
            canvas.Fill(shape, White);
            canvas.Fill(p => Outline(shape(p) + 6f, 1.6f), new Color(0.68f, 0.66f, 0.66f));
            return canvas;
        }

        /// <summary>A white disc with an engraved ring, for the game to tint.</summary>
        public static Canvas2D TintDisc(ArtStyle style)
        {
            var canvas = new Canvas2D(128, 128);
            var c = new Vector2(64f, 64f);
            canvas.Fill(p => Circle(p, c, 62.5f), White);
            canvas.Fill(p => Outline(Circle(p, c, 54f), 2f), new Color(0.68f, 0.66f, 0.66f));
            return canvas;
        }

        /// <summary>A button of the drawn kit: a rounded face over a darker lip, with the style's rim (nine-sliced).</summary>
        public static Canvas2D KitButton(Color face, Color lip, ArtStyle style)
        {
            const int size = 128;
            const float depth = 10f;
            const float radius = 26f;
            var canvas = new Canvas2D(size, size + (int)depth);
            var lower = new Vector2(size * 0.5f, size * 0.5f);
            var upper = new Vector2(size * 0.5f, size * 0.5f + depth);
            Vector2 half = new Vector2(size * 0.5f - 1f, size * 0.5f - 1f);
            canvas.Fill(p => Box(p, lower, half, radius), lip);
            Sdf shape = p => Box(p, upper, half, radius);
            Color light = Color.Lerp(face, Color.white, 0.14f);
            canvas.Fill(shape, p => Color.Lerp(face, light, Mathf.Clamp01((p.y - depth) / size)));
            Rim(canvas, shape, style, 3f);
            canvas.Fill(p => Intersect(shape(p) + 8f, -(p.y - (size * 0.5f + depth + 30f))), new Color(1f, 1f, 1f, 0.08f));
            return canvas;
        }

        /// <summary>A round button of the drawn kit.</summary>
        public static Canvas2D KitRound(Color face, Color lip, ArtStyle style)
        {
            const int size = 128;
            var canvas = new Canvas2D(size, size);
            var c = new Vector2(size * 0.5f, size * 0.5f);
            canvas.Fill(p => Circle(p, c + new Vector2(0f, -5f), size * 0.46f), lip);
            Sdf shape = p => Circle(p, c + new Vector2(0f, 2f), size * 0.46f);
            Color light = Color.Lerp(face, Color.white, 0.14f);
            canvas.Fill(shape, p => Color.Lerp(face, light, Mathf.Clamp01(p.y / size)));
            Rim(canvas, shape, style, 3f);
            canvas.Fill(p => Intersect(shape(p) + 8f, -(p.y - (c.y + 24f))), new Color(1f, 1f, 1f, 0.08f));
            return canvas;
        }

        /// <summary>The field of an input, nine-sliced.</summary>
        public static Canvas2D InputField(ArtStyle style)
        {
            var canvas = new Canvas2D(128, 64);
            var center = new Vector2(64f, 32f);
            Sdf shape = p => Box(p, center, new Vector2(63f, 31f), 12f);
            canvas.Fill(shape, style.InputFill);
            canvas.Fill(p => Outline(shape(p) + 2f, 2.5f), style.PanelRim.a > 0f ? style.PanelRim : Hex("#C9CEDB"));
            return canvas;
        }

        /// <summary>A star of the level select and the results: full, or an empty outline.</summary>
        public static Canvas2D StarSprite(bool full, ArtStyle style)
        {
            var canvas = new Canvas2D(128, 128);
            var c = new Vector2(0.5f, 0.52f);
            if (full)
            {
                canvas.FillN(p => Star(p, c, 0.47f, 5, 0.5f) - 0.02f, style.StarRim);
                canvas.FillN(p => Star(p, c, 0.4f, 5, 0.5f) - 0.015f, p => Color.Lerp(Color.Lerp(style.StarFill, style.StarRim, 0.35f), style.StarFill, p.y));
                canvas.FillN(p => Star(p, c + new Vector2(-0.06f, 0.06f), 0.13f, 5, 0.5f), new Color(1f, 1f, 1f, 0.45f));
            }
            else
            {
                canvas.FillN(p => Star(p, c, 0.47f, 5, 0.5f) - 0.02f, style.StarEmpty);
                canvas.FillN(p => Star(p, c, 0.4f, 5, 0.5f) - 0.015f, new Color(0f, 0f, 0f, 0.25f));
            }
            return canvas;
        }

        /// <summary>The mark beside a goal of the results: reached, or missed.</summary>
        public static Canvas2D GoalMark(bool done, ArtStyle style)
        {
            var canvas = new Canvas2D(96, 96);
            var c = new Vector2(0.5f, 0.5f);
            if (done)
            {
                canvas.FillN(p => Circle(p, c, 0.46f), style.BadgeRing);
                canvas.FillN(p => Circle(p, c, 0.39f), p => Color.Lerp(style.BadgeBottom, style.BadgeTop, p.y));
                canvas.FillN(p => Union(Segment(p, new Vector2(0.29f, 0.5f), new Vector2(0.44f, 0.35f), 0.065f),
                    Segment(p, new Vector2(0.44f, 0.35f), new Vector2(0.72f, 0.64f), 0.065f)), style.BadgeMark);
            }
            else
            {
                canvas.FillN(p => Circle(p, c, 0.46f), new Color(style.StarEmpty.r, style.StarEmpty.g, style.StarEmpty.b, 0.7f));
                canvas.FillN(p => Circle(p, c, 0.39f), Color.Lerp(style.PanelLip, style.PanelFill, 0.5f));
                canvas.FillN(p => Union(Segment(p, new Vector2(0.34f, 0.34f), new Vector2(0.66f, 0.66f), 0.06f),
                    Segment(p, new Vector2(0.34f, 0.66f), new Vector2(0.66f, 0.34f), 0.06f)), new Color(style.StarEmpty.r, style.StarEmpty.g, style.StarEmpty.b, 0.9f));
            }
            return canvas;
        }

        /// <summary>An art deco flourish for the title: a gem in the middle and tapering lines with smaller gems, drawn white.</summary>
        public static Canvas2D Ornament()
        {
            var canvas = new Canvas2D(512, 128);
            var c = new Vector2(256f, 64f);
            // A small gem in the middle, so the flourish fits between the title and its subtitle.
            canvas.Fill(p => Box(Rotate(p, c, 45f), c, new Vector2(8f, 8f)), White);
            canvas.Fill(p => Outline(Box(Rotate(p, c, 45f), c, new Vector2(13f, 13f)), 1.6f), White);
            foreach (float side in new[] { -1f, 1f })
            {
                Vector2 start = c + new Vector2(side * 26f, 0f);
                Vector2 end = c + new Vector2(side * 236f, 0f);
                canvas.Fill(p => Polygon(p, start + new Vector2(0f, 3.5f), end + new Vector2(0f, 0.8f), end + new Vector2(0f, -0.8f), start + new Vector2(0f, -3.5f)), White);
                canvas.Fill(p => Polygon(p, start + new Vector2(0f, 7f), c + new Vector2(side * 150f, 5.5f), c + new Vector2(side * 150f, 4.5f), start + new Vector2(0f, 6f)), new Color(1f, 1f, 1f, 0.7f));
                canvas.Fill(p => Polygon(p, start + new Vector2(0f, -7f), c + new Vector2(side * 150f, -5.5f), c + new Vector2(side * 150f, -4.5f), start + new Vector2(0f, -6f)), new Color(1f, 1f, 1f, 0.7f));
                foreach (float x in new[] { 96f, 176f })
                {
                    Vector2 gem = c + new Vector2(side * x, 0f);
                    canvas.Fill(p => Box(Rotate(p, gem, 45f), gem, new Vector2(7f, 7f)), White);
                }
                Vector2 tip = end + new Vector2(side * 10f, 0f);
                canvas.Fill(p => Circle(p, tip, 4f), White);
            }
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

        /// <summary>A shape floating in the backdrop, by name (white; the world tints it).</summary>
        public static Canvas2D Ambient(string name)
        {
            switch (name)
            {
                case "Leaf": return Leaf();
                case "Snowflake": return Snowflake();
                case "Balloon": return Balloon();
                case "Smoke": return Smoke();
                case "Petal": return Petal();
                case "Sparkle": return SparkleShape();
                case "Bubble": return Bubble();
                default: return Cloud();
            }
        }

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

        /// <summary>A wisp of smoke: a soft blob that fades out at its edge.</summary>
        public static Canvas2D Smoke()
        {
            var canvas = new Canvas2D(256, 150);
            float s = 256f;
            Sdf shape = p =>
            {
                Vector2 q = p / s;
                return Union(Circle(q, new Vector2(0.3f, 0.28f), 0.14f), Circle(q, new Vector2(0.48f, 0.34f), 0.18f),
                    Circle(q, new Vector2(0.68f, 0.3f), 0.13f), Ellipse(q, new Vector2(0.5f, 0.24f), new Vector2(0.34f, 0.1f))) * s;
            };
            canvas.Fill(p => shape(p) - 40f, p =>
            {
                float d = shape(p);
                float a = Mathf.Clamp01(-d / 45f + 0.35f);
                return new Color(1f, 1f, 1f, a * a);
            });
            return canvas;
        }

        /// <summary>A rose petal: a leaf shape with a lighter heart.</summary>
        public static Canvas2D Petal()
        {
            var canvas = new Canvas2D(128, 128);
            var c = new Vector2(0.5f, 0.5f);
            Sdf petal = p => Vesica(Rotate(p, c, 30f), c, 0.42f, 0.24f);
            canvas.FillN(petal, White);
            canvas.FillN(p => Vesica(Rotate(p, c + new Vector2(0.03f, 0.03f), 30f), c + new Vector2(0.03f, 0.03f), 0.28f, 0.12f), new Color(1f, 0.85f, 0.9f, 0.6f));
            return canvas;
        }

        /// <summary>A four pointed sparkle in a soft glow.</summary>
        public static Canvas2D SparkleShape()
        {
            var canvas = new Canvas2D(128, 128);
            var c = new Vector2(0.5f, 0.5f);
            canvas.FillN(p => Circle(p, c, 0.45f), p =>
            {
                float d = (p - c).magnitude / 0.45f;
                return new Color(1f, 1f, 1f, Mathf.Clamp01(1f - d) * 0.35f);
            });
            canvas.FillN(p => Sparkle(p, c, 0.46f), White);
            canvas.FillN(p => Sparkle(Rotate(p, c, 45f), c, 0.2f), new Color(1f, 1f, 1f, 0.8f));
            return canvas;
        }

        /// <summary>A champagne bubble: a thin ring with a gleam.</summary>
        public static Canvas2D Bubble()
        {
            var canvas = new Canvas2D(128, 128);
            var c = new Vector2(0.5f, 0.5f);
            canvas.FillN(p => Circle(p, c, 0.42f), new Color(1f, 1f, 1f, 0.16f));
            canvas.FillN(p => Ring(p, c, 0.42f, 0.05f), White);
            canvas.FillN(p => Ellipse(Rotate(p, new Vector2(0.36f, 0.66f), -40f), new Vector2(0.36f, 0.66f), new Vector2(0.1f, 0.05f)), new Color(1f, 1f, 1f, 0.85f));
            return canvas;
        }

        /// <summary>A tile of scattered paw prints and dots (or a damask lattice) for the scrolling backdrop pattern.</summary>
        public static Canvas2D Pattern(bool deco)
        {
            const int size = 256;
            var canvas = new Canvas2D(size, size);
            if (deco)
            {
                var c = new Vector2(size * 0.5f, size * 0.5f);
                canvas.Fill(p =>
                {
                    float d = float.MaxValue;
                    for (int ox = -1; ox <= 1; ox++)
                    {
                        for (int oy = -1; oy <= 1; oy++)
                        {
                            Vector2 q = p + new Vector2(ox * size, oy * size);
                            float lattice = Mathf.Abs(Mathf.Abs(q.x - c.x) + Mathf.Abs(q.y - c.y) - 128f) - 2f;
                            float quatrefoil = Union(Circle(q, c + new Vector2(0f, 22f), 18f), Circle(q, c + new Vector2(0f, -22f), 18f),
                                Circle(q, c + new Vector2(22f, 0f), 18f), Circle(q, c + new Vector2(-22f, 0f), 18f));
                            float hole = Circle(q, c, 9f);
                            float corner = Box(Rotate(q, Vector2.zero, 45f), Vector2.zero, new Vector2(12f, 12f));
                            d = Mathf.Min(d, Mathf.Min(lattice, Mathf.Min(Subtract(quatrefoil, hole), corner)));
                        }
                    }
                    return d;
                }, White);
                return canvas;
            }
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
