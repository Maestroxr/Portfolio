using Gamebox.Editor;
using UnityEngine;

namespace Portfolio.DeathRace.EditorTools
{
    /// <summary>
    /// The drivers' portraits: a racing helmet in the driver's colour, seen from the front, with a visor and one of a
    /// set of liveries (stripes, flames, a star, a lightning bolt, checks...), on a plate in the look's style. Twenty, one
    /// per driver colour (index = the driver's colour), and The Warden's: black with a red visor and a skull.
    /// </summary>
    internal static class DeathRaceArtPortraits
    {
        public const int Size = 128;

        /// <summary>The index of The Warden's portrait (after the twenty drivers).</summary>
        public const int Warden = 20;

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
            Skull
        }

        public static Texture2D Draw(int index, DeathRaceThemeSpec spec)
        {
            var r = new Raster(Size, Size, Color.clear);
            var bounds = new Rect(0f, 0f, Size, Size);
            var center = new Vector2(Size * 0.5f, Size * 0.5f);
            bool warden = index >= Warden;
            Color paint = warden ? DeathRaceThemeSpec.Hex(0x1C1C20) : spec.RivalColors[index % spec.RivalColors.Length];
            float light = 0.2126f * paint.r + 0.7152f * paint.g + 0.0722f * paint.b;
            Color trim = warden ? DeathRaceThemeSpec.Hex(0xC62E24) : light > 0.55f ? DeathRaceThemeSpec.Hex(0x1C1C20) : DeathRaceThemeSpec.Hex(0xF2EEE6);
            Livery livery = warden ? Livery.Skull : (Livery)((index + 1) % 8);

            // The plate behind the helmet.
            Sdf plate = p => Sd.Box(p, center, new Vector2(60f, 60f), 14f);
            if (spec.Night)
            {
                r.Fill(plate, p => Color.Lerp(spec.PlateDark, spec.PlateLight, p.y / Size), bounds);
                r.Fill(p => Sd.Outline(plate(p) + 1.5f, 2.5f), warden ? DeathRaceThemeSpec.Hex(0xFF3A3A) : spec.Accent, bounds);
            }
            else
            {
                r.Fill(plate, p => Color.Lerp(spec.PlateDark, spec.PlateLight, p.y / Size), bounds);
                r.Fill(p => Sd.Outline(plate(p) + 1.5f, 2.5f), warden ? DeathRaceThemeSpec.Hex(0xC62E24) : spec.Edge, bounds);
            }
            // Shoulders and collar.
            r.Fill(p => Sd.Ellipse(p, new Vector2(64f, 4f), new Vector2(56f, 30f)), p => Color.Lerp(DeathRaceArtInterface.Shade(paint, 0.45f), DeathRaceArtInterface.Shade(paint, 0.7f), p.y / 34f), bounds);
            r.Fill(p => Sd.Box(p, new Vector2(64f, 28f), new Vector2(18f, 8f), 4f), DeathRaceArtInterface.Shade(trim, 0.85f), bounds);

            // The helmet: a dome with a chin guard.
            Sdf helmet = p => Sd.SmoothUnion(Sd.Ellipse(p, new Vector2(64f, 70f), new Vector2(38f, 40f)), Sd.Box(p, new Vector2(64f, 42f), new Vector2(30f, 14f), 12f), 8f);
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
            Color glass = warden ? DeathRaceThemeSpec.Hex(0xB01818) : spec.Night ? DeathRaceThemeSpec.Hex(0x103048) : DeathRaceThemeSpec.Hex(0x1A2430);
            r.Fill(p => visor(p) - 2f, DeathRaceThemeSpec.Hex(0x0E0A08), bounds);
            r.Fill(visor, p => Color.Lerp(DeathRaceArtInterface.Shade(glass, 0.6f), DeathRaceArtInterface.Lighten(glass, 0.25f), Mathf.Clamp01((p.y - 52f) / 20f)), bounds);
            r.Fill(p => Sd.Intersect(Sd.Segment(p, new Vector2(46f, 56f), new Vector2(60f, 70f), 2.4f), visor(p)), new Color(1f, 1f, 1f, 0.55f), bounds);
            r.Fill(p => Sd.Intersect(Sd.Segment(p, new Vector2(56f, 55f), new Vector2(66f, 65f), 1.2f), visor(p)), new Color(1f, 1f, 1f, 0.35f), bounds);
            if (warden)
            {
                r.Shadow(visor, new Color(1f, 0.2f, 0.15f, 0.5f), 6f, Vector2.zero, bounds);
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
                default:
                    return c;
            }
        }
    }
}
