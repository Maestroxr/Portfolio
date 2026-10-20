using Gamebox.Editor;
using UnityEngine;

namespace Portfolio.DeathRace.EditorTools
{
    /// <summary>
    /// The drivers' portraits: a racing helmet in the driver's colour, seen from the front, with a visor and one of a
    /// set of liveries (stripes, flames, a star, a lightning bolt, checks...), on a plate in the look's style. Twenty, one
    /// per driver colour (index = the driver's colour), then the champions of the three leagues: The Warden's (black, a
    /// red visor and a skull), The Iron Baron's (riveted steel with a spike on top and an amber visor) and Nemesis's
    /// (deep violet with horns and a magenta visor that glows).
    /// </summary>
    internal static class DeathRaceArtPortraits
    {
        public const int Size = 128;

        /// <summary>The index of The Warden's portrait (after the twenty drivers): the Wasteland's champion.</summary>
        public const int Warden = 20;

        /// <summary>The index of The Iron Baron's portrait: the Iron League's champion.</summary>
        public const int IronBaron = 21;

        /// <summary>The index of Nemesis's portrait: the Hellfire League's champion.</summary>
        public const int Nemesis = 22;

        /// <summary>How many portraits a look has: the twenty drivers and the three champions.</summary>
        public const int Count = 23;

        private enum Livery
        {
            Plain,
            Stripe,
            Twin,
            Flames,
            Star,
            Bolt,
            Checks,
            Band,
            Skull,
            Rivets,
            Horns
        }

        public static Texture2D Draw(int index, DeathRaceThemeSpec spec)
        {
            var r = new Raster(Size, Size, Color.clear);
            var bounds = new Rect(0f, 0f, Size, Size);
            var center = new Vector2(Size * 0.5f, Size * 0.5f);
            bool champion = index >= Warden;
            bool baron = index == IronBaron;
            bool nemesis = index >= Nemesis;
            Color paint = baron ? DeathRaceThemeSpec.Hex(0x6A706C) : nemesis ? DeathRaceThemeSpec.Hex(0x3A1858)
                : champion ? DeathRaceThemeSpec.Hex(0x1C1C20) : spec.RivalColors[index % spec.RivalColors.Length];
            float light = 0.2126f * paint.r + 0.7152f * paint.g + 0.0722f * paint.b;
            Color trim = baron ? DeathRaceThemeSpec.Hex(0x2A2D33) : nemesis ? DeathRaceThemeSpec.Hex(0xFF2FB4)
                : champion ? DeathRaceThemeSpec.Hex(0xC62E24) : light > 0.55f ? DeathRaceThemeSpec.Hex(0x1C1C20) : DeathRaceThemeSpec.Hex(0xF2EEE6);
            Livery livery = baron ? Livery.Rivets : nemesis ? Livery.Horns : champion ? Livery.Skull : (Livery)((index + 1) % 8);
            // The champions' plates are edged in their own colour, their visors tinted and glowing.
            Color edge = baron ? DeathRaceThemeSpec.Hex(0xFF9A2E) : nemesis ? DeathRaceThemeSpec.Hex(0xFF2FB4)
                : spec.Night ? DeathRaceThemeSpec.Hex(0xFF3A3A) : DeathRaceThemeSpec.Hex(0xC62E24);
            Color visorGlass = baron ? DeathRaceThemeSpec.Hex(0xD87A14) : nemesis ? DeathRaceThemeSpec.Hex(0xD8189A) : DeathRaceThemeSpec.Hex(0xB01818);
            Color visorGlow = baron ? new Color(1f, 0.62f, 0.15f, 0.5f) : nemesis ? new Color(1f, 0.2f, 0.75f, 0.5f) : new Color(1f, 0.2f, 0.15f, 0.5f);

            // The plate behind the helmet.
            Sdf plate = p => Sd.Box(p, center, new Vector2(60f, 60f), 14f);
            if (spec.Night)
            {
                r.Fill(plate, p => Color.Lerp(spec.PlateDark, spec.PlateLight, p.y / Size), bounds);
                r.Fill(p => Sd.Outline(plate(p) + 1.5f, 2.5f), champion ? edge : spec.Accent, bounds);
            }
            else
            {
                r.Fill(plate, p => Color.Lerp(spec.PlateDark, spec.PlateLight, p.y / Size), bounds);
                r.Fill(p => Sd.Outline(plate(p) + 1.5f, 2.5f), champion ? edge : spec.Edge, bounds);
            }
            // Shoulders and collar.
            r.Fill(p => Sd.Ellipse(p, new Vector2(64f, 4f), new Vector2(56f, 30f)), p => Color.Lerp(DeathRaceArtInterface.Shade(paint, 0.45f), DeathRaceArtInterface.Shade(paint, 0.7f), p.y / 34f), bounds);
            r.Fill(p => Sd.Box(p, new Vector2(64f, 28f), new Vector2(18f, 8f), 4f), DeathRaceArtInterface.Shade(trim, 0.85f), bounds);

            // The helmet: a dome with a chin guard (the Baron's spike and Nemesis's horns behind it).
            Sdf helmet = p => Sd.SmoothUnion(Sd.Ellipse(p, new Vector2(64f, 70f), new Vector2(38f, 40f)), Sd.Box(p, new Vector2(64f, 42f), new Vector2(30f, 14f), 12f), 8f);
            if (baron)
            {
                Sdf spike = p => Sd.Union(Sd.Polygon(p, new[] { new Vector2(58f, 104f), new Vector2(70f, 104f), new Vector2(64f, 124f) }),
                    Sd.Box(p, new Vector2(64f, 106f), new Vector2(10f, 4f), 2f));
                r.Fill(p => spike(p) - 2f, DeathRaceThemeSpec.Hex(0x0E0A08), bounds);
                r.Fill(spike, p => Color.Lerp(DeathRaceThemeSpec.Hex(0x8A9096), DeathRaceThemeSpec.Hex(0xE6EAEE), Mathf.Clamp01((p.x - 56f) / 14f)), bounds);
            }
            if (nemesis)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    float s = side;
                    var horn = new[]
                    {
                        new Vector2(64f + s * 22f, 92f), new Vector2(64f + s * 34f, 84f), new Vector2(64f + s * 46f, 104f),
                        new Vector2(64f + s * 46f, 120f), new Vector2(64f + s * 36f, 104f)
                    };
                    r.Fill(p => Sd.Polygon(p, horn) - 2f, DeathRaceThemeSpec.Hex(0x0E0A08), bounds);
                    r.Fill(p => Sd.Polygon(p, horn), p => Color.Lerp(DeathRaceThemeSpec.Hex(0x8A7A6A), DeathRaceThemeSpec.Hex(0xEDE4D2), Mathf.Clamp01((p.y - 84f) / 38f)), bounds);
                }
            }
            r.Shadow(helmet, new Color(0f, 0f, 0f, 0.55f), 5f, new Vector2(2f, -3f), bounds);
            r.Fill(p => helmet(p) - 2.5f, DeathRaceThemeSpec.Hex(0x0E0A08), bounds);
            r.Fill(helmet, p =>
            {
                float shade = Mathf.Clamp01((p.x - 26f) / 76f);
                float top = Mathf.Clamp01((p.y - 30f) / 80f);
                Color c = Color.Lerp(DeathRaceArtInterface.Shade(paint, 0.6f), DeathRaceArtInterface.Lighten(paint, 0.15f), top * 0.7f + (1f - Mathf.Abs(shade - 0.4f)) * 0.3f);
                return LiveryAt(livery, p, c, trim);
            }, bounds);
            // The shine of the shell.
            r.Fill(p => Sd.Intersect(Sd.Ellipse(p, new Vector2(50f, 92f), new Vector2(14f, 8f)), helmet(p)), new Color(1f, 1f, 1f, 0.35f), bounds);

            // The visor.
            Sdf visor = p => Sd.Box(p, new Vector2(64f, 62f), new Vector2(29f, 11f), 9f);
            Color glass = champion ? visorGlass : spec.Night ? DeathRaceThemeSpec.Hex(0x103048) : DeathRaceThemeSpec.Hex(0x1A2430);
            r.Fill(p => visor(p) - 2f, DeathRaceThemeSpec.Hex(0x0E0A08), bounds);
            r.Fill(visor, p => Color.Lerp(DeathRaceArtInterface.Shade(glass, 0.6f), DeathRaceArtInterface.Lighten(glass, 0.25f), Mathf.Clamp01((p.y - 52f) / 20f)), bounds);
            r.Fill(p => Sd.Intersect(Sd.Segment(p, new Vector2(46f, 56f), new Vector2(60f, 70f), 2.4f), visor(p)), new Color(1f, 1f, 1f, 0.55f), bounds);
            r.Fill(p => Sd.Intersect(Sd.Segment(p, new Vector2(56f, 55f), new Vector2(66f, 65f), 1.2f), visor(p)), new Color(1f, 1f, 1f, 0.35f), bounds);
            if (champion)
            {
                r.Shadow(visor, visorGlow, 6f, Vector2.zero, bounds);
            }
            // Vents on the chin guard.
            for (int i = -1; i <= 1; i++)
            {
                var at = new Vector2(64f + i * 8f, 38f);
                r.Fill(p => Sd.Box(p, at, new Vector2(2.2f, 4f), 1f), DeathRaceThemeSpec.Hex(0x0E0A08, 0.8f), bounds);
            }
            r.Mask(plate, bounds);
            return r.ToTexture();
        }

        /// <summary>The paint of the helmet's shell at <paramref name="p"/> under a livery.</summary>
        private static Color LiveryAt(Livery livery, Vector2 p, Color c, Color trim)
        {
            switch (livery)
            {
                case Livery.Stripe:
                    return Mathf.Abs(p.x - 64f) < 6f && p.y > 72f ? trim : c;
                case Livery.Twin:
                    return Mathf.Abs(Mathf.Abs(p.x - 64f) - 11f) < 3.5f && p.y > 72f ? trim : c;
                case Livery.Flames:
                {
                    float wave = Mathf.Sin(p.x * 0.35f) * 5f + Mathf.Sin(p.x * 0.13f + 1f) * 3f;
                    return p.y < 52f + wave && p.y > 30f ? Color.Lerp(DeathRaceThemeSpec.Hex(0xFF7A1A), DeathRaceThemeSpec.Hex(0xFFD21F), (p.y - 30f) / 26f) : c;
                }
                case Livery.Star:
                    return Sd.Star(p, new Vector2(64f, 94f), 11f, 5, 0.45f) < 0f ? trim : c;
                case Livery.Bolt:
                {
                    var bolt = new[] { new Vector2(70f, 108f), new Vector2(54f, 86f), new Vector2(64f, 86f), new Vector2(56f, 74f), new Vector2(74f, 92f), new Vector2(64f, 92f) };
                    return Sd.Polygon(p, bolt) < 0f ? DeathRaceThemeSpec.Hex(0xFFD21F) : c;
                }
                case Livery.Checks:
                    return p.y > 80f && p.y < 92f && (Mathf.FloorToInt(p.x / 6f) + Mathf.FloorToInt(p.y / 6f)) % 2 == 0 ? trim : c;
                case Livery.Band:
                    return p.y > 76f && p.y < 84f ? trim : c;
                case Livery.Skull:
                {
                    var at = new Vector2(64f, 94f);
                    float skull = Sd.SmoothUnion(Sd.Circle(p, at, 9f), Sd.Box(p, at + new Vector2(0f, -8f), new Vector2(5f, 4f), 2f), 3f);
                    float eyes = Mathf.Min(Sd.Circle(p, at + new Vector2(-3.5f, 0.5f), 2.6f), Sd.Circle(p, at + new Vector2(3.5f, 0.5f), 2.6f));
                    return skull < 0f && eyes > 0f ? DeathRaceThemeSpec.Hex(0xE8DEC8) : c;
                }
                case Livery.Rivets:
                {
                    // A riveted crest from front to back, and a band of rivets round the brow.
                    if (Mathf.Abs(p.x - 64f) < 5f && p.y > 76f)
                    {
                        return Mathf.Abs(p.x - 64f) < 1.2f ? DeathRaceArtInterface.Shade(trim, 0.7f) : trim;
                    }
                    float rivets = float.MaxValue;
                    foreach (float x in new[] { 36f, 46f, 82f, 92f })
                    {
                        rivets = Mathf.Min(rivets, Sd.Circle(p, new Vector2(x, 80f), 2.4f));
                    }
                    foreach (float y in new[] { 88f, 98f })
                    {
                        rivets = Mathf.Min(rivets, Mathf.Min(Sd.Circle(p, new Vector2(56f, y), 2f), Sd.Circle(p, new Vector2(72f, y), 2f)));
                    }
                    return rivets < 0f ? DeathRaceThemeSpec.Hex(0xC8CED2) : c;
                }
                case Livery.Horns:
                {
                    // Jagged magenta flashes up from the visor, like cracks with light behind them.
                    float flash = float.MaxValue;
                    foreach (float s in new[] { -1f, 1f })
                    {
                        flash = Mathf.Min(flash, Sd.Polygon(p, new[]
                        {
                            new Vector2(64f + s * 10f, 74f), new Vector2(64f + s * 18f, 86f), new Vector2(64f + s * 14f, 88f),
                            new Vector2(64f + s * 24f, 104f), new Vector2(64f + s * 10f, 88f), new Vector2(64f + s * 14f, 86f)
                        }));
                    }
                    return flash < 0f ? trim : c;
                }
                default:
                    return c;
            }
        }
    }
}
