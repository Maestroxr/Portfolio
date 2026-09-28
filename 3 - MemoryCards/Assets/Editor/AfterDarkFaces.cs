using UnityEngine;
using static Portfolio.MemoryCards.EditorTools.Shapes;

namespace Portfolio.MemoryCards.EditorTools
{
    /// <summary>
    /// The deck of the After Dark theme: thirty keepsakes of a night out (lipstick, a stiletto, a masquerade mask, a
    /// champagne flute...) drawn in a flat vector style on a transparent 512 pixel square, like the animal renders of
    /// the original deck. Everything is suggestion and silhouette: the dancer is a shadow in a spotlight. The faces are
    /// named in <see cref="ThemeSpecs.AfterDarkFaces"/>; <see cref="Draw"/> returns the canvas of one.
    /// </summary>
    internal static class AfterDarkFaces
    {
        private static readonly Color Red = Hex("#C41E3A");
        private static readonly Color RedLight = Hex("#E8455F");
        private static readonly Color Wine = Hex("#7A1230");
        private static readonly Color Gold = Hex("#D9B14C");
        private static readonly Color GoldLight = Hex("#F3DB8C");
        private static readonly Color GoldDark = Hex("#8F6F1F");
        private static readonly Color Cream = Hex("#F4E7CF");
        private static readonly Color Blush = Hex("#E8A0B4");
        private static readonly Color Pink = Hex("#E2557B");
        private static readonly Color Plum = Hex("#2C1226");
        private static readonly Color Ink = Hex("#160E18");
        private static readonly Color Charcoal = Hex("#3A2E3A");
        private static readonly Color Smoke = Hex("#8E8296");
        private static readonly Color Green = Hex("#4E8F5C");
        private static readonly Color GreenDark = Hex("#2F6B3A");
        private static readonly Color Ice = Hex("#DDF3FF");
        private static readonly Color IceDark = Hex("#9CCBE8");
        private static readonly Color White = Color.white;

        /// <summary>The face called <paramref name="name"/>.</summary>
        public static Canvas2D Draw(string name)
        {
            switch (name)
            {
                case "lipstick": return Lipstick();
                case "stiletto": return Stiletto();
                case "corset": return Corset();
                case "garter": return Garter();
                case "champagne": return Champagne();
                case "rose": return Rose();
                case "mask": return Mask();
                case "fan": return Fan();
                case "perfume": return Perfume();
                case "pearls": return Pearls();
                case "dancer": return Dancer();
                case "cocktail": return Cocktail();
                case "glove": return Glove();
                case "lips": return Lips();
                case "cherry": return Cherry();
                case "handcuffs": return Handcuffs();
                case "candle": return Candle();
                case "ring": return DiamondRing();
                case "boot": return Boot();
                case "holder": return Holder();
                case "bow": return Bow();
                case "letter": return Letter();
                case "key": return Key();
                case "curtain": return Curtain();
                case "chandelier": return Chandelier();
                case "tophat": return TopHat();
                case "cane": return Cane();
                case "bowtie": return BowTie();
                case "dice": return Dice();
                case "playingcard": return PlayingCard();
                default: throw new System.ArgumentException($"After Dark has no face called {name}.");
            }
        }

        private static Canvas2D New()
        {
            return new Canvas2D(512, 512);
        }

        /// <summary>A soft radial glow, for candle light and spotlights.</summary>
        private static void Glow(Canvas2D canvas, Vector2 at, float radius, Color color)
        {
            canvas.FillN(p => Circle(p, at, radius), p =>
            {
                float d = (p - at).magnitude / radius;
                float a = Mathf.Clamp01(1f - d);
                return new Color(color.r, color.g, color.b, color.a * a * a);
            });
        }

        private static void Sparkle(Canvas2D canvas, Vector2 at, float size, Color color)
        {
            canvas.FillN(p => MemoryCardsArt.Sparkle(p, at, size), color);
        }

        /// <summary>A rounded highlight on a shiny surface.</summary>
        private static void Shine(Canvas2D canvas, Vector2 at, Vector2 radii, float angle, float alpha = 0.45f)
        {
            canvas.FillN(p => Ellipse(Rotate(p, at, angle), at, radii), new Color(1f, 1f, 1f, alpha));
        }

        private static Canvas2D Lipstick()
        {
            Canvas2D canvas = New();
            canvas.FillN(p => Box(p, new Vector2(0.5f, 0.3f), new Vector2(0.11f, 0.2f), 0.03f), Plum);
            canvas.FillN(p => Box(p, new Vector2(0.44f, 0.3f), new Vector2(0.02f, 0.17f), 0.01f), new Color(1f, 1f, 1f, 0.12f));
            canvas.FillN(p => Box(p, new Vector2(0.5f, 0.11f), new Vector2(0.11f, 0.022f), 0.01f), Gold);
            canvas.FillN(p => Box(p, new Vector2(0.5f, 0.5f), new Vector2(0.115f, 0.035f), 0.012f), Gold);
            canvas.FillN(p => Box(p, new Vector2(0.5f, 0.5f), new Vector2(0.115f, 0.008f)), GoldLight);
            canvas.FillN(p => Box(p, new Vector2(0.5f, 0.6f), new Vector2(0.085f, 0.08f), 0.01f), Red);
            canvas.FillN(p => Polygon(p, new Vector2(0.415f, 0.62f), new Vector2(0.585f, 0.62f), new Vector2(0.585f, 0.8f), new Vector2(0.415f, 0.7f)), Red);
            canvas.FillN(p => Polygon(p, new Vector2(0.5f, 0.62f), new Vector2(0.585f, 0.62f), new Vector2(0.585f, 0.8f), new Vector2(0.5f, 0.75f)), RedLight);
            canvas.FillN(p => Box(p, new Vector2(0.45f, 0.63f), new Vector2(0.012f, 0.06f), 0.01f), new Color(1f, 1f, 1f, 0.4f));
            Sparkle(canvas, new Vector2(0.72f, 0.8f), 0.05f, GoldLight);
            return canvas;
        }

        private static Canvas2D Stiletto()
        {
            // A patent pump seen from the side: pointed toe, high arch, a thin heel with a gold tip.
            Canvas2D canvas = New();
            var o = new Vector2(0f, 0.08f);
            Sdf body = p => Polygon(p - o, new Vector2(0.08f, 0.23f), new Vector2(0.14f, 0.28f), new Vector2(0.24f, 0.335f), new Vector2(0.36f, 0.37f),
                new Vector2(0.44f, 0.375f), new Vector2(0.62f, 0.5f), new Vector2(0.8f, 0.62f), new Vector2(0.835f, 0.6f), new Vector2(0.81f, 0.53f),
                new Vector2(0.66f, 0.42f), new Vector2(0.52f, 0.3f), new Vector2(0.4f, 0.235f), new Vector2(0.3f, 0.215f), new Vector2(0.16f, 0.21f));
            Sdf heel = p => Polygon(p - o, new Vector2(0.745f, 0.53f), new Vector2(0.815f, 0.55f), new Vector2(0.792f, 0.2f), new Vector2(0.774f, 0.2f));
            canvas.FillN(p => Ellipse(p - o, new Vector2(0.46f, 0.195f), new Vector2(0.38f, 0.018f)), new Color(0f, 0f, 0f, 0.3f));
            canvas.FillN(heel, Wine);
            canvas.FillN(p => Box(p - o, new Vector2(0.783f, 0.21f), new Vector2(0.013f, 0.016f), 0.004f), Gold);
            canvas.FillN(body, p => Color.Lerp(Wine, RedLight, Mathf.Clamp01((p.y - o.y - 0.22f) / 0.3f)));
            canvas.FillN(p => Polygon(p - o, new Vector2(0.44f, 0.375f), new Vector2(0.62f, 0.5f), new Vector2(0.8f, 0.62f), new Vector2(0.795f, 0.595f),
                new Vector2(0.61f, 0.475f), new Vector2(0.46f, 0.365f)), Plum);
            Vector2[] sole = { new Vector2(0.09f, 0.225f), new Vector2(0.3f, 0.214f), new Vector2(0.4f, 0.236f), new Vector2(0.52f, 0.3f), new Vector2(0.66f, 0.421f), new Vector2(0.81f, 0.532f) };
            for (int i = 0; i < sole.Length - 1; i++)
            {
                Vector2 a = sole[i] + o;
                Vector2 b = sole[i + 1] + o;
                canvas.FillN(p => Segment(p, a, b, 0.009f), Plum);
            }
            Shine(canvas, new Vector2(0.25f, 0.305f) + o, new Vector2(0.075f, 0.014f), 24f, 0.55f);
            Shine(canvas, new Vector2(0.68f, 0.51f) + o, new Vector2(0.05f, 0.01f), 34f, 0.35f);
            Sparkle(canvas, new Vector2(0.84f, 0.84f), 0.05f, GoldLight);
            return canvas;
        }

        private static Canvas2D Corset()
        {
            Canvas2D canvas = New();
            Sdf body = p => Polygon(p, new Vector2(0.28f, 0.8f), new Vector2(0.72f, 0.8f), new Vector2(0.64f, 0.5f), new Vector2(0.74f, 0.2f), new Vector2(0.26f, 0.2f), new Vector2(0.36f, 0.5f));
            Sdf cups = p => Union(Circle(p, new Vector2(0.39f, 0.8f), 0.11f), Circle(p, new Vector2(0.61f, 0.8f), 0.11f));
            Sdf corset = p => Union(body(p), cups(p));
            canvas.FillN(corset, Plum);
            canvas.FillN(p => Intersect(corset(p), Mathf.Abs(Mathf.Repeat(p.x * 14f, 1f) - 0.5f) - 0.12f), new Color(1f, 1f, 1f, 0.06f));
            canvas.FillN(p => Outline(corset(p) + 0.012f, 0.014f), Red);
            canvas.FillN(p => Intersect(corset(p), -(p.y - 0.27f)), Red);
            for (int i = 0; i < 6; i++)
            {
                float y = 0.32f + i * 0.09f;
                Vector2 a = new Vector2(0.43f, y);
                Vector2 b = new Vector2(0.57f, y + 0.045f);
                Vector2 c = new Vector2(0.43f, y + 0.09f);
                canvas.FillN(p => Union(Segment(p, a, b, 0.007f), Segment(p, b, c, 0.007f)), Gold);
                canvas.FillN(p => Union(Circle(p, a, 0.013f), Circle(p, b, 0.013f)), GoldLight);
            }
            canvas.FillN(p => Union(Circle(p, new Vector2(0.39f, 0.8f), 0.11f), Circle(p, new Vector2(0.61f, 0.8f), 0.11f)) + 0.03f, new Color(1f, 1f, 1f, 0.08f));
            return canvas;
        }

        private static Canvas2D Garter()
        {
            Canvas2D canvas = New();
            Sdf band = p => Box(p, new Vector2(0.5f, 0.5f), new Vector2(0.36f, 0.1f), 0.1f);
            canvas.FillN(band, Plum);
            canvas.FillN(p => Intersect(band(p), Mathf.Abs(Mathf.Repeat(p.x * 18f, 1f) - 0.5f) - 0.2f), new Color(1f, 1f, 1f, 0.05f));
            canvas.FillN(p => Outline(band(p) + 0.02f, 0.012f), Cream);
            for (int i = 0; i < 11; i++)
            {
                float x = 0.17f + i * 0.066f;
                canvas.FillN(p => Union(Circle(p, new Vector2(x, 0.62f), 0.028f), Circle(p, new Vector2(x, 0.38f), 0.028f)), Cream);
                canvas.FillN(p => Union(Circle(p, new Vector2(x, 0.62f), 0.012f), Circle(p, new Vector2(x, 0.38f), 0.012f)), Plum);
            }
            canvas.FillN(p => Union(Polygon(p, new Vector2(0.47f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.44f, 0.3f), new Vector2(0.38f, 0.34f)),
                Polygon(p, new Vector2(0.5f, 0.5f), new Vector2(0.53f, 0.5f), new Vector2(0.62f, 0.34f), new Vector2(0.56f, 0.3f))), Red);
            canvas.FillN(p => Union(Ellipse(Rotate(p, new Vector2(0.41f, 0.53f), 25f), new Vector2(0.41f, 0.53f), new Vector2(0.1f, 0.06f)),
                Ellipse(Rotate(p, new Vector2(0.59f, 0.53f), -25f), new Vector2(0.59f, 0.53f), new Vector2(0.1f, 0.06f))), Red);
            canvas.FillN(p => Union(Ellipse(Rotate(p, new Vector2(0.41f, 0.53f), 25f), new Vector2(0.41f, 0.53f), new Vector2(0.05f, 0.025f)),
                Ellipse(Rotate(p, new Vector2(0.59f, 0.53f), -25f), new Vector2(0.59f, 0.53f), new Vector2(0.05f, 0.025f))), Wine);
            canvas.FillN(p => Box(p, new Vector2(0.5f, 0.52f), new Vector2(0.035f, 0.04f), 0.015f), RedLight);
            return canvas;
        }

        private static Canvas2D Champagne()
        {
            Canvas2D canvas = New();
            Sdf bowl = p => Polygon(p, new Vector2(0.41f, 0.88f), new Vector2(0.59f, 0.88f), new Vector2(0.56f, 0.48f), new Vector2(0.44f, 0.48f));
            canvas.FillN(bowl, new Color(1f, 1f, 1f, 0.16f));
            canvas.FillN(p => Intersect(bowl(p) + 0.012f, -(p.y - 0.76f)), p => Color.Lerp(Gold, GoldLight, (p.y - 0.48f) / 0.3f));
            canvas.FillN(p => Outline(bowl(p), 0.01f), Cream);
            canvas.FillN(p => Union(Segment(p, new Vector2(0.5f, 0.48f), new Vector2(0.5f, 0.2f), 0.012f), Ellipse(p, new Vector2(0.5f, 0.18f), new Vector2(0.13f, 0.03f))), Cream);
            canvas.FillN(p => Ellipse(p, new Vector2(0.5f, 0.76f), new Vector2(0.075f, 0.014f)), new Color(1f, 1f, 1f, 0.5f));
            foreach (Vector2 bubble in new[] { new Vector2(0.47f, 0.56f), new Vector2(0.53f, 0.63f), new Vector2(0.48f, 0.7f), new Vector2(0.54f, 0.53f), new Vector2(0.51f, 0.66f) })
            {
                canvas.FillN(p => Ring(p, bubble, 0.012f, 0.006f), Cream);
            }
            canvas.FillN(p => Box(p, new Vector2(0.455f, 0.66f), new Vector2(0.008f, 0.16f), 0.008f), new Color(1f, 1f, 1f, 0.35f));
            Sparkle(canvas, new Vector2(0.7f, 0.86f), 0.06f, GoldLight);
            Sparkle(canvas, new Vector2(0.32f, 0.8f), 0.04f, Cream);
            return canvas;
        }

        private static Canvas2D Rose()
        {
            Canvas2D canvas = New();
            var c = new Vector2(0.5f, 0.62f);
            canvas.FillN(p => Segment(p, new Vector2(0.5f, 0.5f), new Vector2(0.44f, 0.1f), 0.016f), GreenDark);
            canvas.FillN(p => Vesica(Rotate(p, new Vector2(0.37f, 0.3f), -40f), new Vector2(0.37f, 0.3f), 0.11f, 0.045f), Green);
            canvas.FillN(p => Vesica(Rotate(p, new Vector2(0.56f, 0.24f), 35f), new Vector2(0.56f, 0.24f), 0.1f, 0.04f), Green);
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f;
                Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                Vector2 at = c + dir * 0.14f;
                canvas.FillN(p => Ellipse(Rotate(p, at, angle), at, new Vector2(0.14f, 0.09f)), Red);
                canvas.FillN(p => Outline(Ellipse(Rotate(p, at, angle), at, new Vector2(0.14f, 0.09f)), 0.008f), Wine);
            }
            for (int i = 0; i < 4; i++)
            {
                float angle = 45f + i * 90f;
                Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                Vector2 at = c + dir * 0.07f;
                canvas.FillN(p => Ellipse(Rotate(p, at, angle), at, new Vector2(0.09f, 0.06f)), RedLight);
                canvas.FillN(p => Outline(Ellipse(Rotate(p, at, angle), at, new Vector2(0.09f, 0.06f)), 0.007f), Wine);
            }
            canvas.FillN(p => Circle(p, c, 0.065f), Wine);
            canvas.FillN(p => Ring(Rotate(p, c, 0f), c, 0.04f, 0.012f), RedLight);
            canvas.FillN(p => Intersect(Ring(p, c, 0.04f, 0.012f), p.x - c.x), Wine);
            return canvas;
        }

        private static Canvas2D Mask()
        {
            Canvas2D canvas = New();
            var c = new Vector2(0.5f, 0.55f);
            canvas.FillN(p => Union(Segment(p, new Vector2(0.72f, 0.5f), new Vector2(0.9f, 0.12f), 0.012f), Circle(p, new Vector2(0.9f, 0.11f), 0.025f)), Gold);
            foreach (float angle in new[] { 62f, 78f, 94f })
            {
                Vector2 at = new Vector2(0.7f, 0.68f) + new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * 0.16f;
                canvas.FillN(p => Vesica(Rotate(p, at, angle), at, 0.16f, 0.045f), angle == 78f ? Pink : Blush);
                canvas.FillN(p => Intersect(Vesica(Rotate(p, at, angle), at, 0.16f, 0.045f), Segment(Rotate(p, at, angle), at - new Vector2(0.15f, 0f), at + new Vector2(0.15f, 0f), 0.005f)), Wine);
            }
            Sdf face = p => Ellipse(p, c, new Vector2(0.36f, 0.15f));
            Sdf peaks = p => Union(Polygon(p, new Vector2(0.24f, 0.6f), new Vector2(0.33f, 0.79f), new Vector2(0.42f, 0.63f)),
                Polygon(p, new Vector2(0.76f, 0.6f), new Vector2(0.67f, 0.79f), new Vector2(0.58f, 0.63f)),
                Polygon(p, new Vector2(0.45f, 0.64f), new Vector2(0.5f, 0.84f), new Vector2(0.55f, 0.64f)));
            Sdf eyes = p => Union(Vesica(p, new Vector2(0.36f, 0.55f), 0.1f, 0.05f), Vesica(p, new Vector2(0.64f, 0.55f), 0.1f, 0.05f));
            Sdf mask = p => Subtract(Union(face(p), peaks(p)), eyes(p));
            canvas.FillN(mask, Plum);
            canvas.FillN(p => Intersect(mask(p), Mathf.Abs(Mathf.Repeat((p.x + p.y) * 12f, 1f) - 0.5f) - 0.2f), new Color(1f, 1f, 1f, 0.05f));
            canvas.FillN(p => Outline(mask(p) + 0.01f, 0.012f), Gold);
            canvas.FillN(p => Outline(eyes(p), 0.01f), GoldLight);
            foreach (Vector2 gem in new[] { new Vector2(0.5f, 0.6f), new Vector2(0.33f, 0.7f), new Vector2(0.67f, 0.7f) })
            {
                canvas.FillN(p => Box(Rotate(p, gem, 45f), gem, new Vector2(0.018f, 0.018f)), Red);
                canvas.FillN(p => Box(Rotate(p, gem, 45f), gem, new Vector2(0.008f, 0.008f)), RedLight);
            }
            return canvas;
        }

        private static Canvas2D Fan()
        {
            Canvas2D canvas = New();
            var pivot = new Vector2(0.5f, 0.16f);
            for (int i = 0; i < 9; i++)
            {
                float angle = 40f + i * 12.5f;
                Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                Vector2 at = pivot + dir * 0.4f;
                Color plume = i % 2 == 0 ? Blush : Pink;
                canvas.FillN(p => Vesica(Rotate(p, at, angle), at, 0.34f, 0.075f), plume);
                canvas.FillN(p => Intersect(Vesica(Rotate(p, at, angle), at, 0.34f, 0.075f), Segment(p, pivot + dir * 0.1f, pivot + dir * 0.72f, 0.004f)), Gold);
            }
            for (int i = 0; i < 9; i++)
            {
                float angle = 40f + i * 12.5f;
                Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                canvas.FillN(p => Segment(p, pivot, pivot + dir * 0.2f, 0.007f), GoldDark);
            }
            canvas.FillN(p => Box(p, new Vector2(0.5f, 0.13f), new Vector2(0.05f, 0.06f), 0.02f), Gold);
            canvas.FillN(p => Circle(p, pivot, 0.03f), GoldLight);
            return canvas;
        }

        private static Canvas2D Perfume()
        {
            Canvas2D canvas = New();
            Sdf bottle = p => Box(p, new Vector2(0.48f, 0.4f), new Vector2(0.2f, 0.22f), 0.06f);
            canvas.FillN(bottle, p => Color.Lerp(Pink, Blush, (p.y - 0.18f) / 0.44f));
            canvas.FillN(p => Intersect(bottle(p), -(p.y - 0.3f)), new Color(Wine.r, Wine.g, Wine.b, 0.35f));
            canvas.FillN(p => Outline(bottle(p) + 0.008f, 0.01f), GoldDark);
            canvas.FillN(p => Box(p, new Vector2(0.48f, 0.42f), new Vector2(0.1f, 0.07f), 0.02f), Cream);
            canvas.FillN(p => Heart(p, new Vector2(0.48f, 0.42f), 0.07f), Red);
            canvas.FillN(p => Box(p, new Vector2(0.4f, 0.4f), new Vector2(0.018f, 0.17f), 0.015f), new Color(1f, 1f, 1f, 0.3f));
            canvas.FillN(p => Box(p, new Vector2(0.48f, 0.67f), new Vector2(0.06f, 0.05f), 0.01f), GoldDark);
            canvas.FillN(p => Box(p, new Vector2(0.48f, 0.78f), new Vector2(0.085f, 0.065f), 0.02f), Gold);
            canvas.FillN(p => Box(p, new Vector2(0.45f, 0.78f), new Vector2(0.015f, 0.05f), 0.01f), GoldLight);
            canvas.FillN(p => Segment(p, new Vector2(0.56f, 0.8f), new Vector2(0.7f, 0.76f), 0.012f), GoldDark);
            canvas.FillN(p => Circle(p, new Vector2(0.75f, 0.74f), 0.07f), Red);
            Shine(canvas, new Vector2(0.72f, 0.77f), new Vector2(0.025f, 0.014f), 30f, 0.5f);
            Sparkle(canvas, new Vector2(0.24f, 0.7f), 0.05f, GoldLight);
            return canvas;
        }

        private static Canvas2D Pearls()
        {
            Canvas2D canvas = New();
            var centre = new Vector2(0.5f, 0.78f);
            const float radius = 0.34f;
            Vector2 left = centre + new Vector2(Mathf.Cos(200f * Mathf.Deg2Rad), Mathf.Sin(200f * Mathf.Deg2Rad)) * radius;
            Vector2 right = centre + new Vector2(Mathf.Cos(340f * Mathf.Deg2Rad), Mathf.Sin(340f * Mathf.Deg2Rad)) * radius;
            var clasp = new Vector2(0.5f, 0.9f);
            canvas.FillN(p => Union(Segment(p, left, clasp, 0.006f), Segment(p, right, clasp, 0.006f)), Gold);
            canvas.FillN(p => Circle(p, clasp, 0.03f), Gold);
            canvas.FillN(p => Circle(p, clasp, 0.014f), GoldLight);
            for (int i = 0; i < 13; i++)
            {
                float angle = (200f + i * 140f / 12f) * Mathf.Deg2Rad;
                Vector2 at = centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                float size = i == 6 ? 0.05f : 0.042f;
                canvas.FillN(p => Circle(p, at, size), p => Color.Lerp(Hex("#D8C8B4"), Cream, Mathf.Clamp01((p.y - at.y + size) / (size * 2f))));
                canvas.FillN(p => Circle(p, at + new Vector2(-size * 0.3f, size * 0.3f), size * 0.3f), new Color(1f, 1f, 1f, 0.8f));
            }
            return canvas;
        }

        private static Canvas2D Dancer()
        {
            Canvas2D canvas = New();
            var c = new Vector2(0.5f, 0.5f);
            canvas.FillN(p => Circle(p, c, 0.44f), p =>
            {
                float d = (p - c).magnitude / 0.44f;
                Color inner = Color.Lerp(Wine, GoldLight, Mathf.Clamp01((p.y - 0.1f) / 0.8f));
                return Color.Lerp(inner, Wine, d * d);
            });
            canvas.FillN(p => Outline(Circle(p, c, 0.43f), 0.016f), Gold);
            Sdf figure = p => Union(
                Circle(p, new Vector2(0.5f, 0.79f), 0.055f),
                Circle(p, new Vector2(0.535f, 0.845f), 0.03f),
                Segment(p, new Vector2(0.5f, 0.75f), new Vector2(0.5f, 0.7f), 0.02f),
                Polygon(p, new Vector2(0.44f, 0.72f), new Vector2(0.56f, 0.72f), new Vector2(0.545f, 0.57f), new Vector2(0.455f, 0.57f)),
                Polygon(p, new Vector2(0.455f, 0.58f), new Vector2(0.545f, 0.58f), new Vector2(0.7f, 0.3f), new Vector2(0.3f, 0.3f)),
                Circle(p, new Vector2(0.34f, 0.3f), 0.055f), Circle(p, new Vector2(0.44f, 0.28f), 0.06f), Circle(p, new Vector2(0.56f, 0.28f), 0.06f), Circle(p, new Vector2(0.66f, 0.3f), 0.055f),
                Segment(p, new Vector2(0.55f, 0.7f), new Vector2(0.67f, 0.86f), 0.02f),
                Segment(p, new Vector2(0.45f, 0.69f), new Vector2(0.37f, 0.6f), 0.02f), Segment(p, new Vector2(0.37f, 0.6f), new Vector2(0.45f, 0.55f), 0.018f),
                Segment(p, new Vector2(0.47f, 0.3f), new Vector2(0.44f, 0.1f), 0.022f), Segment(p, new Vector2(0.44f, 0.1f), new Vector2(0.4f, 0.08f), 0.016f),
                Segment(p, new Vector2(0.54f, 0.3f), new Vector2(0.72f, 0.16f), 0.022f), Segment(p, new Vector2(0.72f, 0.16f), new Vector2(0.76f, 0.12f), 0.014f));
            canvas.FillN(figure, Ink);
            foreach (float angle in new[] { 40f, 75f, 110f })
            {
                Vector2 at = new Vector2(0.68f, 0.88f) + new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * 0.08f;
                canvas.FillN(p => Vesica(Rotate(p, at, angle), at, 0.09f, 0.03f), angle == 75f ? Pink : Blush);
            }
            return canvas;
        }

        private static Canvas2D Cocktail()
        {
            Canvas2D canvas = New();
            Sdf bowl = p => Polygon(p, new Vector2(0.2f, 0.72f), new Vector2(0.8f, 0.72f), new Vector2(0.5f, 0.42f));
            canvas.FillN(bowl, new Color(1f, 1f, 1f, 0.16f));
            canvas.FillN(p => Polygon(p, new Vector2(0.27f, 0.66f), new Vector2(0.73f, 0.66f), new Vector2(0.5f, 0.43f)), p => Color.Lerp(Pink, Blush, (p.y - 0.43f) / 0.23f));
            canvas.FillN(p => Outline(bowl(p), 0.01f), Cream);
            canvas.FillN(p => Union(Segment(p, new Vector2(0.5f, 0.42f), new Vector2(0.5f, 0.16f), 0.014f), Ellipse(p, new Vector2(0.5f, 0.14f), new Vector2(0.17f, 0.035f))), Cream);
            canvas.FillN(p => Segment(p, new Vector2(0.3f, 0.84f), new Vector2(0.63f, 0.52f), 0.008f), Gold);
            canvas.FillN(p => Circle(p, new Vector2(0.61f, 0.55f), 0.05f), Green);
            canvas.FillN(p => Circle(p, new Vector2(0.61f, 0.55f), 0.02f), Red);
            canvas.FillN(p => Box(p, new Vector2(0.36f, 0.63f), new Vector2(0.006f, 0.05f), 0.006f), new Color(1f, 1f, 1f, 0.4f));
            Sparkle(canvas, new Vector2(0.78f, 0.84f), 0.05f, GoldLight);
            return canvas;
        }

        private static Canvas2D Glove()
        {
            Canvas2D canvas = New();
            Sdf glove = p =>
            {
                float d = Box(p, new Vector2(0.5f, 0.38f), new Vector2(0.1f, 0.28f), 0.08f);
                d = Mathf.Min(d, Ellipse(p, new Vector2(0.5f, 0.66f), new Vector2(0.13f, 0.1f)));
                for (int i = 0; i < 4; i++)
                {
                    float x = 0.41f + i * 0.06f;
                    float top = i == 1 || i == 2 ? 0.9f : 0.86f;
                    d = Mathf.Min(d, Segment(p, new Vector2(x, 0.7f), new Vector2(x + (i - 1.5f) * 0.01f, top), 0.026f));
                }
                d = Mathf.Min(d, Segment(p, new Vector2(0.4f, 0.64f), new Vector2(0.3f, 0.74f), 0.027f));
                return d;
            };
            canvas.FillN(glove, Wine);
            canvas.FillN(p =>
            {
                Vector2 q = new Vector2(Mathf.Repeat(p.x, 0.05f), Mathf.Repeat(p.y + Mathf.Floor(p.x / 0.05f) * 0.025f, 0.05f)) - new Vector2(0.025f, 0.025f);
                return Intersect(glove(p) + 0.012f, q.magnitude - 0.011f);
            }, new Color(Cream.r, Cream.g, Cream.b, 0.55f));
            canvas.FillN(p => Outline(glove(p) + 0.006f, 0.008f), Blush);
            canvas.FillN(p => Intersect(glove(p), Mathf.Abs(p.y - 0.13f) - 0.03f), Plum);
            for (int i = 0; i < 5; i++)
            {
                float x = 0.42f + i * 0.04f;
                canvas.FillN(p => Circle(p, new Vector2(x, 0.1f), 0.014f), Cream);
            }
            return canvas;
        }

        private static Canvas2D Lips()
        {
            Canvas2D canvas = New();
            var c = new Vector2(0.5f, 0.5f);
            Sdf lips = p => MemoryCardsArt.EmblemShape(CardEmblem.Lips, p, c, 0.66f);
            canvas.FillN(lips, p => Color.Lerp(Wine, Red, Mathf.Clamp01((p.y - 0.3f) / 0.4f)));
            canvas.FillN(p => Intersect(lips(p), p.y - 0.5f), p => Color.Lerp(Red, RedLight, Mathf.Clamp01((p.y - 0.5f) / 0.2f)));
            canvas.FillN(p => Box(p, c, new Vector2(0.37f, 0.012f), 0.012f), Plum);
            Shine(canvas, new Vector2(0.42f, 0.41f), new Vector2(0.07f, 0.03f), 10f, 0.35f);
            Shine(canvas, new Vector2(0.6f, 0.6f), new Vector2(0.04f, 0.02f), -20f, 0.35f);
            Sparkle(canvas, new Vector2(0.8f, 0.72f), 0.05f, GoldLight);
            return canvas;
        }

        private static Canvas2D Cherry()
        {
            Canvas2D canvas = New();
            canvas.FillN(p => Union(Segment(p, new Vector2(0.4f, 0.4f), new Vector2(0.56f, 0.82f), 0.014f), Segment(p, new Vector2(0.62f, 0.34f), new Vector2(0.56f, 0.82f), 0.014f)), GreenDark);
            canvas.FillN(p => Vesica(Rotate(p, new Vector2(0.66f, 0.82f), 20f), new Vector2(0.66f, 0.82f), 0.12f, 0.05f), Green);
            canvas.FillN(p => Intersect(Vesica(Rotate(p, new Vector2(0.66f, 0.82f), 20f), new Vector2(0.66f, 0.82f), 0.12f, 0.05f), Segment(Rotate(p, new Vector2(0.66f, 0.82f), 20f), new Vector2(0.55f, 0.82f), new Vector2(0.77f, 0.82f), 0.004f)), GreenDark);
            foreach (Vector2 at in new[] { new Vector2(0.38f, 0.32f), new Vector2(0.63f, 0.26f) })
            {
                canvas.FillN(p => Circle(p, at, 0.15f), p => Color.Lerp(Wine, Red, Mathf.Clamp01((p.y - at.y + 0.15f) / 0.3f)));
                canvas.FillN(p => Circle(p, at + new Vector2(0.03f, -0.02f), 0.11f), new Color(RedLight.r, RedLight.g, RedLight.b, 0.35f));
                Shine(canvas, at + new Vector2(-0.05f, 0.06f), new Vector2(0.04f, 0.022f), -30f, 0.55f);
            }
            return canvas;
        }

        private static Canvas2D Handcuffs()
        {
            Canvas2D canvas = New();
            foreach (float x in new[] { 0.45f, 0.5f, 0.55f })
            {
                canvas.FillN(p => Ring(p, new Vector2(x, 0.5f), 0.03f, 0.012f), Gold);
            }
            foreach (Vector2 c in new[] { new Vector2(0.3f, 0.5f), new Vector2(0.7f, 0.5f) })
            {
                canvas.FillN(p => Ring(p, c, 0.15f, 0.07f), Blush);
                for (int i = 0; i < 14; i++)
                {
                    float angle = i * (360f / 14f) * Mathf.Deg2Rad;
                    Vector2 at = c + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 0.15f;
                    float size = i % 2 == 0 ? 0.052f : 0.044f;
                    canvas.FillN(p => Circle(p, at, size), i % 2 == 0 ? Pink : Blush);
                }
                canvas.FillN(p => Ring(p, c, 0.09f, 0.02f), Wine);
                canvas.FillN(p => Ring(p, c, 0.09f, 0.008f), GoldDark);
            }
            return canvas;
        }

        private static Canvas2D Candle()
        {
            Canvas2D canvas = New();
            Glow(canvas, new Vector2(0.5f, 0.78f), 0.3f, new Color(1f, 0.75f, 0.3f, 0.45f));
            canvas.FillN(p => Ellipse(p, new Vector2(0.5f, 0.12f), new Vector2(0.22f, 0.05f)), Gold);
            canvas.FillN(p => Ellipse(p, new Vector2(0.5f, 0.14f), new Vector2(0.15f, 0.03f)), GoldLight);
            Sdf body = p => Box(p, new Vector2(0.5f, 0.4f), new Vector2(0.09f, 0.26f), 0.02f);
            canvas.FillN(body, p => Color.Lerp(Hex("#D8C8B0"), Cream, Mathf.Clamp01((p.x - 0.41f) / 0.12f)));
            canvas.FillN(p => Union(Ellipse(p, new Vector2(0.44f, 0.6f), new Vector2(0.03f, 0.06f)), Ellipse(p, new Vector2(0.57f, 0.56f), new Vector2(0.025f, 0.09f)), Ellipse(p, new Vector2(0.5f, 0.65f), new Vector2(0.1f, 0.03f))), Cream);
            canvas.FillN(p => Segment(p, new Vector2(0.5f, 0.65f), new Vector2(0.5f, 0.71f), 0.008f), Charcoal);
            canvas.FillN(p => Vesica(Rotate(p, new Vector2(0.5f, 0.79f), 90f), new Vector2(0.5f, 0.79f), 0.1f, 0.045f), p => Color.Lerp(RedLight, GoldLight, Mathf.Clamp01((p.y - 0.7f) / 0.12f)));
            canvas.FillN(p => Vesica(Rotate(p, new Vector2(0.5f, 0.76f), 90f), new Vector2(0.5f, 0.76f), 0.05f, 0.022f), Cream);
            return canvas;
        }

        private static Canvas2D DiamondRing()
        {
            Canvas2D canvas = New();
            var c = new Vector2(0.5f, 0.4f);
            canvas.FillN(p => Ring(p, c, 0.24f, 0.065f), Gold);
            canvas.FillN(p => Intersect(Ring(p, c, 0.24f, 0.065f), p.x - c.x - 0.02f), GoldDark);
            canvas.FillN(p => Intersect(Ring(p, c, 0.24f, 0.03f), -(p.x - c.x + 0.05f)), GoldLight);
            canvas.FillN(p => Box(p, new Vector2(0.5f, 0.66f), new Vector2(0.075f, 0.03f), 0.01f), Gold);
            Sdf pavilion = p => Polygon(p, new Vector2(0.36f, 0.72f), new Vector2(0.64f, 0.72f), new Vector2(0.5f, 0.58f));
            Sdf crown = p => Polygon(p, new Vector2(0.36f, 0.72f), new Vector2(0.64f, 0.72f), new Vector2(0.58f, 0.82f), new Vector2(0.42f, 0.82f));
            canvas.FillN(p => Union(pavilion(p), crown(p)), Ice);
            canvas.FillN(p => Polygon(p, new Vector2(0.5f, 0.72f), new Vector2(0.64f, 0.72f), new Vector2(0.5f, 0.58f)), IceDark);
            canvas.FillN(p => Polygon(p, new Vector2(0.42f, 0.82f), new Vector2(0.58f, 0.82f), new Vector2(0.5f, 0.72f)), new Color(1f, 1f, 1f, 0.7f));
            canvas.FillN(p => Union(Segment(p, new Vector2(0.36f, 0.72f), new Vector2(0.64f, 0.72f), 0.005f), Segment(p, new Vector2(0.42f, 0.82f), new Vector2(0.5f, 0.72f), 0.004f), Segment(p, new Vector2(0.58f, 0.82f), new Vector2(0.5f, 0.72f), 0.004f)), White);
            Sparkle(canvas, new Vector2(0.62f, 0.86f), 0.07f, White);
            Sparkle(canvas, new Vector2(0.34f, 0.8f), 0.04f, GoldLight);
            return canvas;
        }

        private static Canvas2D Boot()
        {
            Canvas2D canvas = New();
            Sdf shaft = p => Box(p, new Vector2(0.46f, 0.6f), new Vector2(0.12f, 0.3f), 0.06f);
            Sdf foot = p => Union(Ellipse(p, new Vector2(0.66f, 0.27f), new Vector2(0.2f, 0.09f)), Polygon(p, new Vector2(0.34f, 0.36f), new Vector2(0.58f, 0.36f), new Vector2(0.7f, 0.3f), new Vector2(0.7f, 0.19f), new Vector2(0.34f, 0.19f)));
            Sdf boot = p => Union(shaft(p), foot(p));
            canvas.FillN(p => Box(p, new Vector2(0.39f, 0.15f), new Vector2(0.04f, 0.06f), 0.01f), Plum);
            canvas.FillN(boot, Wine);
            canvas.FillN(p => Intersect(boot(p), p.x - 0.5f), new Color(0f, 0f, 0f, 0.15f));
            canvas.FillN(p => Segment(p, new Vector2(0.36f, 0.19f), new Vector2(0.86f, 0.22f), 0.012f), Plum);
            canvas.FillN(p => Box(p, new Vector2(0.46f, 0.88f), new Vector2(0.125f, 0.03f), 0.01f), Blush);
            for (int i = 0; i < 6; i++)
            {
                float y = 0.42f + i * 0.08f;
                Vector2 a = new Vector2(0.4f, y);
                Vector2 b = new Vector2(0.52f, y + 0.04f);
                canvas.FillN(p => Union(Segment(p, a, b, 0.006f), Segment(p, new Vector2(0.52f, y), new Vector2(0.4f, y + 0.04f), 0.006f)), Gold);
                canvas.FillN(p => Union(Circle(p, a, 0.012f), Circle(p, new Vector2(0.52f, y), 0.012f)), GoldLight);
            }
            Shine(canvas, new Vector2(0.62f, 0.3f), new Vector2(0.06f, 0.02f), 10f, 0.3f);
            return canvas;
        }

        private static Canvas2D Holder()
        {
            Canvas2D canvas = New();
            var a = new Vector2(0.12f, 0.3f);
            var b = new Vector2(0.62f, 0.56f);
            var tip = new Vector2(0.84f, 0.67f);
            canvas.FillN(p => Segment(p, a, b, 0.024f), Ink);
            canvas.FillN(p => Segment(p, a + new Vector2(0.02f, 0.02f), b + new Vector2(-0.02f, 0.01f), 0.006f), new Color(1f, 1f, 1f, 0.25f));
            canvas.FillN(p => Segment(p, a, a + new Vector2(0.08f, 0.04f), 0.028f), Gold);
            canvas.FillN(p => Segment(p, b - new Vector2(0.04f, 0.02f), b + new Vector2(0.02f, 0.01f), 0.028f), Gold);
            canvas.FillN(p => Segment(p, b, tip, 0.02f), Cream);
            canvas.FillN(p => Segment(p, b, b + (tip - b) * 0.25f, 0.02f), Hex("#D8B892"));
            canvas.FillN(p => Circle(p, tip, 0.022f), RedLight);
            canvas.FillN(p => Circle(p, tip, 0.012f), GoldLight);
            // Soft puffs of smoke rising from the tip, bigger and fainter as they go.
            Vector2[] puffs = { tip + new Vector2(0.01f, 0.07f), tip + new Vector2(-0.025f, 0.13f), tip + new Vector2(0.015f, 0.2f) };
            for (int i = 0; i < puffs.Length; i++)
            {
                Vector2 at = puffs[i];
                float radius = 0.022f + i * 0.012f;
                float alpha = 0.55f - i * 0.15f;
                canvas.FillN(p => Circle(p, at, radius * 1.6f), p =>
                {
                    float d = (p - at).magnitude / (radius * 1.6f);
                    float fade = Mathf.Clamp01(1f - d);
                    return new Color(Smoke.r, Smoke.g, Smoke.b, alpha * fade * 1.6f);
                });
            }
            return canvas;
        }

        private static Canvas2D Bow()
        {
            Canvas2D canvas = New();
            var left = new Vector2(0.34f, 0.56f);
            var right = new Vector2(0.66f, 0.56f);
            canvas.FillN(p => Union(Polygon(p, new Vector2(0.47f, 0.5f), new Vector2(0.51f, 0.5f), new Vector2(0.43f, 0.18f), new Vector2(0.33f, 0.26f)),
                Polygon(p, new Vector2(0.49f, 0.5f), new Vector2(0.53f, 0.5f), new Vector2(0.67f, 0.26f), new Vector2(0.57f, 0.18f))), Red);
            canvas.FillN(p => Union(Polygon(p, new Vector2(0.48f, 0.48f), new Vector2(0.5f, 0.48f), new Vector2(0.44f, 0.24f), new Vector2(0.4f, 0.27f)),
                Polygon(p, new Vector2(0.5f, 0.48f), new Vector2(0.52f, 0.48f), new Vector2(0.6f, 0.27f), new Vector2(0.56f, 0.24f))), Wine);
            canvas.FillN(p => Union(Ellipse(Rotate(p, left, 25f), left, new Vector2(0.17f, 0.11f)), Ellipse(Rotate(p, right, -25f), right, new Vector2(0.17f, 0.11f))), Red);
            canvas.FillN(p => Union(Ellipse(Rotate(p, left, 25f), left, new Vector2(0.1f, 0.05f)), Ellipse(Rotate(p, right, -25f), right, new Vector2(0.1f, 0.05f))), Wine);
            Shine(canvas, left + new Vector2(-0.02f, 0.07f), new Vector2(0.06f, 0.02f), 25f, 0.35f);
            Shine(canvas, right + new Vector2(0.02f, 0.07f), new Vector2(0.06f, 0.02f), -25f, 0.35f);
            canvas.FillN(p => Box(p, new Vector2(0.5f, 0.55f), new Vector2(0.05f, 0.065f), 0.02f), RedLight);
            canvas.FillN(p => Box(p, new Vector2(0.485f, 0.55f), new Vector2(0.01f, 0.05f), 0.01f), new Color(1f, 1f, 1f, 0.3f));
            return canvas;
        }

        private static Canvas2D Letter()
        {
            Canvas2D canvas = New();
            Sdf envelope = p => Box(p, new Vector2(0.5f, 0.46f), new Vector2(0.31f, 0.21f), 0.02f);
            canvas.FillN(envelope, Cream);
            canvas.FillN(p => Polygon(p, new Vector2(0.19f, 0.67f), new Vector2(0.81f, 0.67f), new Vector2(0.5f, 0.44f)), Hex("#E6D6BC"));
            canvas.FillN(p => Union(Segment(p, new Vector2(0.19f, 0.25f), new Vector2(0.5f, 0.48f), 0.004f), Segment(p, new Vector2(0.81f, 0.25f), new Vector2(0.5f, 0.48f), 0.004f)), Hex("#D8C8B0"));
            canvas.FillN(p => Outline(envelope(p), 0.008f), GoldDark);
            canvas.FillN(p => Circle(p, new Vector2(0.5f, 0.46f), 0.085f), Red);
            canvas.FillN(p => Circle(p, new Vector2(0.5f, 0.46f), 0.085f) + 0.02f, Wine);
            canvas.FillN(p => Heart(p, new Vector2(0.5f, 0.45f), 0.07f), GoldLight);
            canvas.FillN(p => MemoryCardsArt.EmblemShape(CardEmblem.Lips, p, new Vector2(0.7f, 0.32f), 0.12f), new Color(RedLight.r, RedLight.g, RedLight.b, 0.8f));
            return canvas;
        }

        private static Canvas2D Key()
        {
            Canvas2D canvas = New();
            var bow = new Vector2(0.28f, 0.64f);
            var tipEnd = new Vector2(0.84f, 0.27f);
            Vector2 dir = (tipEnd - bow).normalized;
            Vector2 side = new Vector2(-dir.y, dir.x);
            canvas.FillN(p => Ring(p, bow, 0.12f, 0.065f), Gold);
            canvas.FillN(p => Union(Circle(p, bow + new Vector2(0f, 0.12f), 0.035f), Circle(p, bow + new Vector2(-0.1f, -0.06f), 0.035f), Circle(p, bow + new Vector2(0.1f, -0.06f), 0.035f)), Gold);
            canvas.FillN(p => Ring(p, bow, 0.12f, 0.02f), GoldLight);
            canvas.FillN(p => Segment(p, bow + dir * 0.1f, tipEnd, 0.028f), Gold);
            canvas.FillN(p => Segment(p, bow + dir * 0.12f + side * 0.008f, tipEnd - dir * 0.02f + side * 0.008f, 0.006f), GoldLight);
            Vector2 bit1 = tipEnd - dir * 0.06f;
            Vector2 bit2 = tipEnd - dir * 0.14f;
            canvas.FillN(p => Union(Segment(p, bit1, bit1 - side * 0.07f, 0.02f), Segment(p, bit2, bit2 - side * 0.05f, 0.02f)), Gold);
            canvas.FillN(p => Circle(p, tipEnd, 0.028f), GoldDark);
            Sparkle(canvas, new Vector2(0.8f, 0.7f), 0.05f, GoldLight);
            return canvas;
        }

        private static Canvas2D Curtain()
        {
            Canvas2D canvas = New();
            canvas.FillN(p => Box(p, new Vector2(0.5f, 0.9f), new Vector2(0.34f, 0.022f), 0.01f), Gold);
            canvas.FillN(p => Union(Circle(p, new Vector2(0.14f, 0.9f), 0.035f), Circle(p, new Vector2(0.86f, 0.9f), 0.035f)), GoldLight);
            Sdf drape = p => Polygon(p, new Vector2(0.24f, 0.88f), new Vector2(0.76f, 0.88f), new Vector2(0.76f, 0.56f), new Vector2(0.58f, 0.44f), new Vector2(0.66f, 0.12f), new Vector2(0.34f, 0.12f), new Vector2(0.42f, 0.44f), new Vector2(0.24f, 0.56f));
            canvas.FillN(drape, Red);
            for (int i = 0; i < 6; i++)
            {
                float x = 0.29f + i * 0.084f;
                float xLow = 0.4f + i * 0.04f;
                canvas.FillN(p => Intersect(drape(p), Union(Segment(p, new Vector2(x, 0.88f), new Vector2(0.5f + (x - 0.5f) * 0.35f, 0.46f), 0.012f),
                    Segment(p, new Vector2(0.5f + (x - 0.5f) * 0.35f, 0.42f), new Vector2(xLow, 0.12f), 0.012f))), Wine);
            }
            canvas.FillN(p => Intersect(drape(p), p.x - 0.62f), new Color(0f, 0f, 0f, 0.18f));
            canvas.FillN(p => Box(Rotate(p, new Vector2(0.5f, 0.44f), 8f), new Vector2(0.5f, 0.44f), new Vector2(0.11f, 0.035f), 0.02f), Gold);
            canvas.FillN(p => Union(Vesica(Rotate(p, new Vector2(0.56f, 0.34f), 90f), new Vector2(0.56f, 0.34f), 0.06f, 0.02f), Segment(p, new Vector2(0.55f, 0.42f), new Vector2(0.56f, 0.38f), 0.005f)), GoldLight);
            return canvas;
        }

        private static Canvas2D Chandelier()
        {
            Canvas2D canvas = New();
            Glow(canvas, new Vector2(0.5f, 0.6f), 0.4f, new Color(1f, 0.85f, 0.5f, 0.25f));
            canvas.FillN(p => Segment(p, new Vector2(0.5f, 0.96f), new Vector2(0.5f, 0.74f), 0.01f), Gold);
            canvas.FillN(p => Ellipse(p, new Vector2(0.5f, 0.74f), new Vector2(0.07f, 0.03f)), Gold);
            canvas.FillN(p => Segment(p, new Vector2(0.5f, 0.74f), new Vector2(0.5f, 0.5f), 0.014f), Gold);
            canvas.FillN(p => Union(Intersect(Ring(p, new Vector2(0.37f, 0.66f), 0.13f, 0.018f), -(p.y - 0.66f)), Intersect(Ring(p, new Vector2(0.63f, 0.66f), 0.13f, 0.018f), -(p.y - 0.66f))), Gold);
            foreach (Vector2 arm in new[] { new Vector2(0.24f, 0.66f), new Vector2(0.5f, 0.5f), new Vector2(0.76f, 0.66f) })
            {
                canvas.FillN(p => Ellipse(p, arm, new Vector2(0.045f, 0.018f)), GoldLight);
                canvas.FillN(p => Box(p, arm + new Vector2(0f, 0.06f), new Vector2(0.016f, 0.055f), 0.005f), Cream);
                Vector2 flame = arm + new Vector2(0f, 0.15f);
                canvas.FillN(p => Vesica(Rotate(p, flame, 90f), flame, 0.045f, 0.02f), p => Color.Lerp(RedLight, GoldLight, Mathf.Clamp01((p.y - flame.y + 0.04f) / 0.06f)));
            }
            foreach (Vector2 crystal in new[] { new Vector2(0.3f, 0.52f), new Vector2(0.42f, 0.46f), new Vector2(0.58f, 0.46f), new Vector2(0.7f, 0.52f), new Vector2(0.5f, 0.36f) })
            {
                canvas.FillN(p => Segment(p, crystal + new Vector2(0f, 0.06f), crystal, 0.003f), GoldLight);
                canvas.FillN(p => Box(Rotate(p, crystal, 45f), crystal, new Vector2(0.022f, 0.022f)), Ice);
                canvas.FillN(p => Box(Rotate(p, crystal, 45f), crystal, new Vector2(0.01f, 0.01f)), White);
            }
            canvas.FillN(p => Circle(p, new Vector2(0.5f, 0.3f), 0.025f), Gold);
            return canvas;
        }

        private static Canvas2D TopHat()
        {
            Canvas2D canvas = New();
            canvas.FillN(p => Ellipse(p, new Vector2(0.5f, 0.32f), new Vector2(0.36f, 0.07f)), Charcoal);
            canvas.FillN(p => Ellipse(p, new Vector2(0.5f, 0.335f), new Vector2(0.34f, 0.05f)), Hex("#4A3C4A"));
            Sdf crown = p => Box(p, new Vector2(0.5f, 0.6f), new Vector2(0.19f, 0.26f), 0.025f);
            canvas.FillN(crown, Charcoal);
            canvas.FillN(p => Intersect(crown(p), p.x - 0.6f), Ink);
            canvas.FillN(p => Box(p, new Vector2(0.36f, 0.62f), new Vector2(0.02f, 0.2f), 0.015f), new Color(1f, 1f, 1f, 0.12f));
            canvas.FillN(p => Ellipse(p, new Vector2(0.5f, 0.86f), new Vector2(0.19f, 0.035f)), Hex("#4A3C4A"));
            canvas.FillN(p => Box(p, new Vector2(0.5f, 0.42f), new Vector2(0.19f, 0.035f)), Red);
            canvas.FillN(p => Box(p, new Vector2(0.5f, 0.42f), new Vector2(0.19f, 0.006f)), Gold);
            canvas.FillN(p => Union(Circle(p, new Vector2(0.64f, 0.42f), 0.02f), Circle(p, new Vector2(0.64f, 0.42f), 0.02f)), Gold);
            return canvas;
        }

        private static Canvas2D Cane()
        {
            Canvas2D canvas = New();
            var foot = new Vector2(0.3f, 0.14f);
            var top = new Vector2(0.66f, 0.76f);
            canvas.FillN(p => Segment(p, foot, top, 0.028f), Charcoal);
            canvas.FillN(p => Segment(p, foot + new Vector2(-0.008f, 0.01f), top + new Vector2(-0.008f, 0.01f), 0.007f), new Color(1f, 1f, 1f, 0.28f));
            canvas.FillN(p => Segment(p, foot, foot + (top - foot).normalized * 0.06f, 0.03f), Gold);
            canvas.FillN(p => Segment(p, top - (top - foot).normalized * 0.05f, top, 0.032f), Gold);
            canvas.FillN(p => Circle(p, new Vector2(0.69f, 0.81f), 0.075f), p => Color.Lerp(GoldDark, Gold, Mathf.Clamp01((p.y - 0.74f) / 0.14f)));
            Shine(canvas, new Vector2(0.665f, 0.84f), new Vector2(0.028f, 0.016f), 40f, 0.55f);
            Sparkle(canvas, new Vector2(0.8f, 0.9f), 0.05f, GoldLight);
            return canvas;
        }

        private static Canvas2D BowTie()
        {
            Canvas2D canvas = New();
            Sdf left = p => Polygon(p, new Vector2(0.46f, 0.5f), new Vector2(0.18f, 0.68f), new Vector2(0.18f, 0.32f));
            Sdf right = p => Polygon(p, new Vector2(0.54f, 0.5f), new Vector2(0.82f, 0.68f), new Vector2(0.82f, 0.32f));
            canvas.FillN(p => Union(left(p), right(p)) - 0.02f, Red);
            canvas.FillN(p => Union(Segment(p, new Vector2(0.44f, 0.5f), new Vector2(0.2f, 0.6f), 0.006f), Segment(p, new Vector2(0.44f, 0.5f), new Vector2(0.2f, 0.4f), 0.006f),
                Segment(p, new Vector2(0.56f, 0.5f), new Vector2(0.8f, 0.6f), 0.006f), Segment(p, new Vector2(0.56f, 0.5f), new Vector2(0.8f, 0.4f), 0.006f)), Wine);
            canvas.FillN(p => Box(p, new Vector2(0.5f, 0.5f), new Vector2(0.065f, 0.075f), 0.025f), RedLight);
            canvas.FillN(p => Box(p, new Vector2(0.48f, 0.5f), new Vector2(0.012f, 0.055f), 0.01f), new Color(1f, 1f, 1f, 0.3f));
            Shine(canvas, new Vector2(0.28f, 0.57f), new Vector2(0.06f, 0.02f), 15f, 0.3f);
            Shine(canvas, new Vector2(0.72f, 0.57f), new Vector2(0.06f, 0.02f), -15f, 0.3f);
            return canvas;
        }

        private static Canvas2D Dice()
        {
            Canvas2D canvas = New();
            DrawDie(canvas, new Vector2(0.36f, 0.56f), 12f, new[] { new Vector2(0f, 0f), new Vector2(-0.08f, 0.08f), new Vector2(0.08f, 0.08f), new Vector2(-0.08f, -0.08f), new Vector2(0.08f, -0.08f) });
            DrawDie(canvas, new Vector2(0.66f, 0.38f), -18f, new[] { new Vector2(0f, 0f), new Vector2(-0.08f, 0.08f), new Vector2(0.08f, -0.08f) });
            return canvas;
        }

        private static void DrawDie(Canvas2D canvas, Vector2 at, float angle, Vector2[] pips)
        {
            canvas.FillN(p => Box(Rotate(p, at, angle), at, new Vector2(0.16f, 0.16f), 0.035f), p => Color.Lerp(Wine, Red, Mathf.Clamp01((p.y - at.y + 0.16f) / 0.32f)));
            canvas.FillN(p => Outline(Box(Rotate(p, at, angle), at, new Vector2(0.15f, 0.15f), 0.03f), 0.006f), GoldDark);
            foreach (Vector2 pip in pips)
            {
                canvas.FillN(p => Circle(Rotate(p, at, angle), at + pip, 0.03f), Cream);
            }
        }

        private static Canvas2D PlayingCard()
        {
            Canvas2D canvas = New();
            var c = new Vector2(0.5f, 0.5f);
            Sdf card = p => Box(Rotate(p, c, -8f), c, new Vector2(0.21f, 0.29f), 0.03f);
            canvas.FillN(card, Cream);
            canvas.FillN(p => Outline(card(p) + 0.02f, 0.006f), GoldDark);
            canvas.FillN(p => Outline(card(p), 0.008f), Plum);
            canvas.FillN(p => Heart(Rotate(p, c, -8f), c + new Vector2(0f, -0.01f), 0.24f), Red);
            canvas.FillN(p => Union(Heart(Rotate(p, c, -8f), c + new Vector2(-0.15f, 0.22f), 0.05f), Heart(Rotate(p, c, -8f), c + new Vector2(0.15f, -0.22f), 0.05f)), Red);
            canvas.FillN(p => Union(Box(Rotate(p, c, -8f), c + new Vector2(-0.15f, 0.16f), new Vector2(0.012f, 0.004f)), Box(Rotate(p, c, -8f), c + new Vector2(0.15f, -0.16f), new Vector2(0.012f, 0.004f))), Plum);
            Shine(canvas, c + new Vector2(-0.05f, 0.03f), new Vector2(0.03f, 0.015f), 30f, 0.4f);
            return canvas;
        }
    }
}
