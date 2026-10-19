using UnityEngine;
using N = Portfolio.DeathRace.EditorTools.DeathRaceArtNoise;

namespace Portfolio.DeathRace.EditorTools
{
    /// <summary>
    /// The surfaces of the track itself: the asphalt (wear, cracks, patches, oil, faded lines), the shoulder, the striped
    /// curbs, the barriers of each biome (a tyre wall, a steel guardrail, snow capped concrete, a concrete jersey
    /// barrier) and the checkered line. The barrier picture runs along U (4 m) and up V (foot to top, the top cap at
    /// V 0.9-1); its reflector strip glows at night through <see cref="BarrierGlow"/>.
    /// </summary>
    internal static partial class DeathRaceArtSurfaces
    {
        public const int RoadPixels = 512;

        /// <summary>Metres one road tile covers along the track (TRACK writes V = metres / 16).</summary>
        public const float RoadTileLength = 16f;

        /// <summary>The road width the picture is drawn for (tracks are 12-16 m wide; U stretches to fit).</summary>
        public const float RoadWidth = 14f;

        /// <summary>The band of the barrier picture that is the reflector strip on its faces, and the line on its top.</summary>
        public static readonly Vector2 StripBand = new Vector2(0.70f, 0.75f);
        public static readonly Vector2 TopStripBand = new Vector2(0.935f, 0.965f);

        public static float RoadRelief => 3f;

        // ------------------------------------------------------------------------------------------------
        // Road
        // ------------------------------------------------------------------------------------------------

        private struct RoadLook
        {
            public Color Dark;
            public Color Light;
            public Color EdgePaint;
            public Color CentrePaint;
            public float Paint;
            public float Oil;
            public float Cracks;
            public int Seed;
        }

        private static RoadLook LookOf(Biome biome)
        {
            switch (biome)
            {
                case Biome.Industrial:
                    return new RoadLook { Dark = Hex(0x232324), Light = Hex(0x3B3A38), EdgePaint = Hex(0xD9AE2B), CentrePaint = Hex(0xCFCBC2), Paint = 0.55f, Oil = 0.5f, Cracks = 0.7f, Seed = 200 };
                case Biome.Snow:
                    return new RoadLook { Dark = Hex(0x222428), Light = Hex(0x393C42), EdgePaint = Hex(0xE6E6E2), CentrePaint = Hex(0xE6E6E2), Paint = 0.6f, Oil = 0.25f, Cracks = 0.5f, Seed = 300 };
                case Biome.City:
                    return new RoadLook { Dark = Hex(0x262628), Light = Hex(0x3C3C3F), EdgePaint = Hex(0xEDEDE8), CentrePaint = Hex(0xEDEDE8), Paint = 0.85f, Oil = 0.35f, Cracks = 0.35f, Seed = 400 };
                default:
                    return new RoadLook { Dark = Hex(0x37332F), Light = Hex(0x524C45), EdgePaint = Hex(0xE8E2D2), CentrePaint = Hex(0xE5B83A), Paint = 0.5f, Oil = 0.35f, Cracks = 0.8f, Seed = 100 };
            }
        }

        /// <summary>
        /// Asphalt across the whole road (U 0 = left edge, 1 = right edge) and 16 m along: aggregate, the darker rubbered
        /// band where the cars run, cracks, patched repairs, oil, wheel ruts that hold water at night, faded edge lines and
        /// a dashed centre line, and what each biome leaves on it (sand, snow and slush, drains and a manhole).
        /// </summary>
        public static SurfaceMap Road(Biome biome)
        {
            RoadLook look = LookOf(biome);
            int seed = look.Seed;
            var map = new SurfaceMap(RoadPixels, RoadPixels);
            map.Paint((i, u, v) =>
            {
                float across = u * RoadWidth;
                float along = v * RoadTileLength;
                float patches = N.Fbm(u, v, 7, 8, 4, seed);
                Color c = Mix(look.Dark, look.Light, patches * 1.2f - 0.1f);
                float h = 0.5f + patches * 0.05f;

                // Aggregate: light chips and dark pits.
                N.Cell chips = N.Cells(u, v, 200, 228, seed + 1, 0.9f);
                float chip = chips.Id > 0.72f ? 1f - N.Smooth(0.18f, 0.34f, chips.F1) : 0f;
                float pit = chips.Id < 0.12f ? 1f - N.Smooth(0.12f, 0.26f, chips.F1) : 0f;
                c = Mix(c, Shade(look.Light, 1.25f), chip * 0.5f);
                c = Mix(c, Shade(look.Dark, 0.6f), pit * 0.6f);
                h += chip * 0.12f - pit * 0.12f;

                // Rubber laid down where the cars run, dust where they do not.
                float middle = 1f - N.Smooth(0.3f, 0.4f, Mathf.Abs(u - 0.5f));
                float rubber = middle * (0.55f + 0.45f * N.Fbm(u, v, 1, 12, 2, seed + 2));
                c = Mix(c, Shade(c, 0.8f), rubber * 0.4f);
                float edgeDust = N.Smooth(0.47f, 0.5f, Mathf.Abs(u - 0.5f));
                c = Mix(c, Shade(look.Light, 1.1f), edgeDust * 0.2f);

                // Wheel ruts (where water stands at night).
                float rutA = 1f - N.Smooth(0.02f, 0.06f, Mathf.Abs(u - 0.3f));
                float rutB = 1f - N.Smooth(0.02f, 0.06f, Mathf.Abs(u - 0.7f));
                float rut = Mathf.Max(rutA, rutB);
                float wet = rut * N.Smooth(0.52f, 0.62f, N.Fbm(u, v, 4, 10, 3, seed + 3));
                h -= rut * 0.06f;

                // Cracks: branching ones in fields, and the long one beside each edge.
                float crackField = N.Smooth(1f - look.Cracks * 0.5f, 1.05f - look.Cracks * 0.5f, N.Fbm(u, v, 3, 4, 3, seed + 4));
                float crack = N.Smooth(0.88f, 0.95f, N.Ridge(u, v, 6, 7, 3, seed + 5)) * crackField;
                float wobble = N.Fbm(u, v, 2, 12, 2, seed + 6) * 0.03f;
                float edgeCrack = Mathf.Max(1f - Mathf.Abs(u - 0.075f - wobble) / 0.0025f, 1f - Mathf.Abs(u - 0.925f + wobble) / 0.0025f);
                edgeCrack = Mathf.Clamp01(edgeCrack) * N.Smooth(0.45f, 0.6f, N.Fbm(u, v, 2, 8, 2, seed + 7)) * look.Cracks;
                crack = Mathf.Max(crack, edgeCrack);
                c = Mix(c, Shade(look.Dark, 0.45f), crack * 0.9f);
                h -= crack * 0.3f;

                // Repairs: darker, smoother asphalt with a sealed seam.
                float patch = Patch(u, v, 0.58f, 0.76f, 0.12f, 0.26f);
                if (biome == Biome.Industrial)
                {
                    patch += Patch(u, v, 0.18f, 0.42f, 0.66f, 0.72f);
                }
                if (biome == Biome.City)
                {
                    patch += Patch(u, v, 0.62f, 0.68f, 0.38f, 0.98f);
                }
                float patchInside = Mathf.Clamp01(patch * 4f) >= 1f ? 1f : 0f;
                float patchSeam = Mathf.Clamp01(patch * 4f) > 0f && patchInside < 1f ? 1f : 0f;
                c = Mix(c, Mix(Shade(look.Dark, 0.9f), look.Dark, N.Fbm(u, v, 30, 34, 2, seed + 8)), patchInside * 0.55f);
                c = Mix(c, Shade(look.Dark, 0.6f), patchSeam * 0.5f);
                h += patchInside * 0.03f + patchSeam * 0.04f;

                // Oil: dark glossy blots, more of them in the lanes.
                N.Cell blots = N.Cells(u, v, 5, 6, seed + 9, 0.9f);
                float warp = N.Fbm(u, v, 14, 16, 3, seed + 10) * 0.35f;
                float oil = blots.Id < look.Oil * 0.5f ? 1f - N.Smooth(0.08f, 0.15f, blots.F1 + warp * 0.6f - 0.1f) : 0f;
                oil *= 0.5f + middle * 0.5f;
                c = Mix(c, Hex(0x141312), oil * 0.55f);

                // Lines: edge lines 0.35 m in, a dashed centre line (3 m dashes every 8 m), worn and chipped.
                float edge = Mathf.Min(Mathf.Abs(across - 0.55f), Mathf.Abs(across - (RoadWidth - 0.55f)));
                float edgeLine = 1f - N.Smooth(0.07f, 0.1f, edge);
                float dash = Mathf.Repeat(along, 8f) < 3f ? 1f : 0f;
                float centreLine = (1f - N.Smooth(0.06f, 0.09f, Mathf.Abs(across - RoadWidth * 0.5f))) * dash;
                float worn = N.Smooth(0.28f, 0.6f, N.Fbm(u, v, 40, 46, 3, seed + 11));
                float scuff = 1f - rubber * 0.6f;
                float paintEdge = edgeLine * look.Paint * worn;
                float paintCentre = centreLine * look.Paint * worn * scuff;
                c = Mix(c, look.EdgePaint, paintEdge);
                c = Mix(c, look.CentrePaint, paintCentre);
                h += (paintEdge + paintCentre) * 0.04f;

                float gloss = 0.32f + rubber * 0.1f - patchInside * 0.06f + oil * 0.65f + wet * 0.55f - crack * 0.2f;
                switch (biome)
                {
                    case Biome.Desert:
                    {
                        // Sand blown in from the edges, in tongues across the road.
                        float fromEdge = Mathf.Min(u, 1f - u);
                        float tongues = N.Fbm(u, v, 4, 6, 4, seed + 20);
                        float sand = N.Smooth(0.022f + tongues * 0.03f, 0.008f, fromEdge) * (0.6f + 0.4f * N.Fbm(u, v, 3, 20, 3, seed + 23));
                        sand = Mathf.Max(sand, N.Smooth(0.8f, 0.88f, tongues * 0.7f + N.Fbm(u, v, 10, 3, 3, seed + 21) * 0.4f) * 0.7f);
                        float grains = N.Fbm(u, v, 120, 136, 2, seed + 22);
                        c = Mix(c, Mix(Hex(0xB08B62), Hex(0xD3B488), grains), sand * 0.9f);
                        h += sand * 0.05f;
                        gloss = Mathf.Lerp(gloss, 0.1f, sand);
                        break;
                    }
                    case Biome.Snow:
                    {
                        // A crisp lip of ploughed snow at the edges, grey slush here and there, a haze of dried salt.
                        float fromEdge = Mathf.Min(u, 1f - u);
                        float bank = N.Smooth(0.024f + N.Fbm(u, v, 3, 10, 3, seed + 30) * 0.03f, 0.008f, fromEdge);
                        float slush = N.Smooth(0.76f, 0.84f, N.Fbm(u, v, 6, 8, 4, seed + 32)) * (1f - rut) * 0.5f;
                        float salt = N.Smooth(0.55f, 0.9f, N.Fbm(u, v, 5, 6, 3, seed + 33));
                        c = Mix(c, Shade(c, 1.25f), salt * 0.25f);
                        c = Mix(c, Mix(Hex(0x5E6268), Hex(0x7E838A), N.Fbm(u, v, 20, 24, 2, seed + 34)), slush);
                        c = Mix(c, Mix(Hex(0xDCE5F0), Hex(0xF2F6FB), N.Fbm(u, v, 30, 34, 2, seed + 35)), bank);
                        h += bank * 0.25f;
                        gloss = Mathf.Lerp(gloss, 0.5f, slush) + rut * 0.15f;
                        break;
                    }
                    case Biome.City:
                    {
                        // A manhole cover and a drain grate at the kerb.
                        float d = new Vector2((u - 0.34f) * RoadWidth, (v - 0.47f) * RoadTileLength).magnitude;
                        if (d < 0.38f)
                        {
                            float ring = d > 0.33f ? 1f : 0f;
                            float grid = (Mathf.Repeat(across * 6f, 1f) < 0.3f || Mathf.Repeat(along * 6f, 1f) < 0.3f) ? 1f : 0f;
                            c = ring > 0f ? Hex(0x3A3836) : Mix(Hex(0x2B2A29), Hex(0x403E3B), grid);
                            h = 0.5f + grid * 0.05f - ring * 0.04f;
                            gloss = 0.55f;
                        }
                        float gx = Mathf.Abs(across - 0.45f);
                        float gy = Mathf.Abs(along - 3.2f);
                        if (gx < 0.3f && gy < 0.5f)
                        {
                            bool bar = Mathf.Repeat(along * 9f, 1f) < 0.5f;
                            c = gx > 0.25f || gy > 0.45f ? Hex(0x3B3A38) : bar ? Hex(0x2A2928) : Hex(0x070707);
                            h = bar ? 0.48f : 0.2f;
                            gloss = 0.5f;
                        }
                        break;
                    }
                    case Biome.Industrial:
                    {
                        // A concrete repair strip across the road and old skid marks.
                        float skid = (1f - N.Smooth(0.004f, 0.01f, Mathf.Abs(u - 0.42f - Mathf.Sin(v * Mathf.PI * 2f) * 0.05f)))
                            + (1f - N.Smooth(0.004f, 0.01f, Mathf.Abs(u - 0.55f - Mathf.Sin(v * Mathf.PI * 2f) * 0.05f)));
                        skid *= N.Smooth(0.5f, 0.62f, N.Fbm(u, v, 2, 5, 2, seed + 40));
                        c = Mix(c, Hex(0x101010), Mathf.Clamp01(skid) * 0.6f);
                        break;
                    }
                }
                map.Albedo[i] = WithGloss(c, gloss);
                map.Heights[i] = h;
            });
            return map;
        }

        /// <summary>
        /// The same surface after rain: the colours a little darker, every spot glossier (the alpha that scales the
        /// material's smoothness), so the night look's road shines where the light catches it.
        /// </summary>
        public static SurfaceMap Wet(SurfaceMap dry)
        {
            var wet = new SurfaceMap(dry.Width, dry.Height);
            for (int i = 0; i < dry.Albedo.Length; i++)
            {
                Color c = dry.Albedo[i];
                wet.Albedo[i] = new Color(c.r * 0.9f, c.g * 0.9f, c.b * 0.92f, Mathf.Clamp01(0.32f + c.a * 0.85f));
                wet.Heights[i] = dry.Heights[i];
            }
            return wet;
        }

        /// <summary>1 inside a rectangle of the unit square, 0.25..1 on its 2 pixel seam band, 0 outside.</summary>
        private static float Patch(float u, float v, float u0, float u1, float v0, float v1)
        {
            float du = Mathf.Min(u - u0, u1 - u);
            float dv = Mathf.Min(v - v0, v1 - v);
            float inside = Mathf.Min(du, dv);
            if (inside < 0f)
            {
                return 0f;
            }
            return inside < 0.004f ? 0.2f : 1f;
        }

        // ------------------------------------------------------------------------------------------------
        // Shoulder (U 0 at the road edge -> 1 at the barrier, ~3 m; V 8 m)
        // ------------------------------------------------------------------------------------------------

        public static SurfaceMap Shoulder(Biome biome)
        {
            var map = new SurfaceMap(256, 512);
            int seed = 500 + (int)biome * 37;
            map.Paint((i, u, v) =>
            {
                Color c;
                float h;
                float gloss;
                float fine = N.Fbm(u, v, 24, 64, 3, seed);
                switch (biome)
                {
                    case Biome.Industrial:
                    {
                        // Ballast: dense grey stones over black dirt, oil and weeds.
                        N.Cell stone = N.Cells(u, v, 34, 90, seed + 1, 0.9f);
                        float rock = 1f - N.Smooth(0.35f, 0.55f, stone.F1);
                        c = Mix(Hex(0x2E2B27), Mix(Hex(0x5E5A54), Hex(0x948F86), stone.Id), rock);
                        c = Shade(c, 0.85f + fine * 0.3f);
                        float oil = N.Smooth(0.68f, 0.76f, N.Fbm(u, v, 3, 8, 3, seed + 2));
                        c = Mix(c, Hex(0x121110), oil * 0.7f);
                        float weed = N.Smooth(0.78f, 0.86f, N.Fbm(u, v, 12, 32, 2, seed + 3)) * N.Smooth(0.5f, 1f, u);
                        c = Mix(c, Hex(0x56603A), weed * 0.8f);
                        h = rock * Mathf.Sqrt(Mathf.Max(0f, 1f - stone.F1 / 0.55f)) * 0.6f;
                        gloss = 0.18f + oil * 0.6f;
                        break;
                    }
                    case Biome.Snow:
                    {
                        // Brown slush by the road, rising into a white ploughed bank at the barrier.
                        float bank = N.Smooth(0.25f, 0.7f, u + (N.Fbm(u, v, 4, 10, 3, seed + 4) - 0.5f) * 0.35f);
                        N.Cell grit = N.Cells(u, v, 40, 106, seed + 5, 0.9f);
                        float pebble = grit.Id > 0.6f ? 1f - N.Smooth(0.15f, 0.3f, grit.F1) : 0f;
                        Color slush = Mix(Hex(0x6F6A64), Hex(0x9EA1A6), fine);
                        slush = Mix(slush, Hex(0x3E3A35), pebble * 0.8f);
                        Color snow = Mix(Hex(0xCCD7E6), Hex(0xF1F5FB), N.Fbm(u, v, 10, 26, 3, seed + 6));
                        c = Mix(slush, snow, bank);
                        h = bank * 0.8f + fine * 0.1f + pebble * 0.05f;
                        gloss = Mathf.Lerp(0.5f, 0.3f, bank);
                        break;
                    }
                    case Biome.City:
                    {
                        // Gutter, granite kerb, then pavement slabs.
                        float across = u * 3f;
                        float along = v * 8f;
                        if (across < 0.35f)
                        {
                            c = Mix(Hex(0x7E7C77), Hex(0x96938D), fine);
                            c = Mix(c, Hex(0x45433F), (1f - N.Smooth(0.02f, 0.08f, across)) * 0.7f);
                            h = 0.3f - across * 0.2f;
                            float drain = Mathf.Abs(along - 4f) < 0.45f && across > 0.05f && across < 0.3f ? 1f : 0f;
                            if (drain > 0f)
                            {
                                bool bar = Mathf.Repeat(along * 10f, 1f) < 0.5f;
                                c = bar ? Hex(0x2A2928) : Hex(0x080808);
                                h = bar ? 0.28f : 0.1f;
                            }
                            gloss = 0.3f + drain * 0.2f;
                        }
                        else if (across < 0.6f)
                        {
                            float top = N.Smooth(0.35f, 0.42f, across);
                            c = Mix(Hex(0x8A8984), Hex(0xB3B1AB), fine * 0.6f + top * 0.4f);
                            float joint = Mathf.Repeat(along, 1f) < 0.02f ? 1f : 0f;
                            c = Mix(c, Hex(0x55534F), joint);
                            h = 0.3f + top * 0.6f;
                            gloss = 0.25f;
                        }
                        else
                        {
                            float sx = (across - 0.6f) / 1.2f;
                            float sy = along / 1.6f;
                            float fx = Mathf.Repeat(sx, 1f);
                            float fy = Mathf.Repeat(sy, 1f);
                            float seam = Mathf.Min(Mathf.Min(fx, 1f - fx) * 1.2f, Mathf.Min(fy, 1f - fy) * 1.6f);
                            float joint = 1f - N.Smooth(0.015f, 0.035f, seam);
                            float id = N.Hash(Mathf.FloorToInt(sx), N.Mod(Mathf.FloorToInt(sy), 5), seed + 7);
                            c = Mix(Hex(0x86847E), Hex(0xA9A7A1), 0.3f + id * 0.7f);
                            c = Shade(c, 0.94f + fine * 0.1f);
                            c = Mix(c, Hex(0x4E4C49), joint * 0.85f);
                            h = 0.9f - joint * 0.2f;
                            gloss = 0.22f;
                        }
                        break;
                    }
                    default:
                    {
                        // Gravel and broken asphalt by the road, fading into the sand of the desert.
                        N.Cell stone = N.Cells(u, v, 30, 80, seed + 8, 0.9f);
                        float density = 1f - N.Smooth(0.1f, 0.8f, u);
                        float rock = stone.Id < 0.25f + density * 0.6f ? 1f - N.Smooth(0.3f, 0.5f, stone.F1) : 0f;
                        Color sand = Mix(Hex(0xA9825B), Hex(0xCCA87C), N.Fbm(u, v, 8, 22, 3, seed + 9));
                        Color pebble = Mix(Hex(0x6E6155), Hex(0xA8998A), stone.Id);
                        c = Mix(sand, pebble, rock * 0.9f);
                        float crumbs = (1f - N.Smooth(0.012f, 0.035f, u + (N.Fbm(u, v, 6, 30, 2, seed + 10) - 0.5f) * 0.04f));
                        c = Mix(c, Hex(0x4A443E), crumbs * 0.7f);
                        h = rock * Mathf.Sqrt(Mathf.Max(0f, 1f - stone.F1 / 0.5f)) * 0.5f + fine * 0.1f + crumbs * 0.1f;
                        gloss = 0.14f + rock * 0.1f;
                        break;
                    }
                }
                map.Albedo[i] = WithGloss(c, gloss);
                map.Heights[i] = h;
            });
            return map;
        }

        // ------------------------------------------------------------------------------------------------
        // Curb (U 0 road side -> 1 outer, 1.5 m; V 4 m = one stripe of each colour)
        // ------------------------------------------------------------------------------------------------

        public static SurfaceMap Curb(Biome biome)
        {
            bool hazard = biome == Biome.Industrial || biome == Biome.City;
            Color first = hazard ? Hex(0xE8B21C) : Hex(0xC8241C);
            Color second = hazard ? Hex(0x1C1B1A) : Hex(0xEDEAE2);
            int seed = 600 + (int)biome * 41;
            var map = new SurfaceMap(128, 256);
            map.Paint((i, u, v) =>
            {
                float stripe = N.Smooth(0.495f, 0.505f, v) * (1f - N.Smooth(0.995f, 1.005f, v));
                Color paint = Mix(first, second, stripe);
                // Ridges across: a saw tooth every 0.25 m, rounded at the road side.
                float ridge = Mathf.Repeat(v * 16f, 1f);
                float tooth = ridge < 0.8f ? ridge / 0.8f : (1f - ridge) / 0.2f;
                float bevel = N.Smooth(0f, 0.12f, u) * N.Smooth(1f, 0.9f, u);
                float h = bevel * (0.5f + tooth * 0.25f);
                Color c = Shade(paint, 0.9f + tooth * 0.15f);
                // Chipped paint showing the concrete, rubber smudged on by the cars.
                float chips = N.Smooth(0.7f, 0.78f, N.Fbm(u, v, 12, 32, 3, seed));
                c = Mix(c, Mix(Hex(0x77746E), Hex(0x9A968F), N.Fbm(u, v, 40, 100, 2, seed + 1)), chips * 0.85f);
                float rubber = N.Smooth(0.5f, 0.15f, u) * N.Smooth(0.4f, 0.7f, N.Fbm(u, v, 4, 10, 3, seed + 2));
                c = Mix(c, Hex(0x1A1918), rubber * 0.5f);
                c = Mix(c, Shade(c, 0.6f), 1f - N.Smooth(0f, 0.06f, u));
                if (biome == Biome.Snow)
                {
                    float frost = N.Smooth(0.55f, 0.75f, u + (N.Fbm(u, v, 6, 16, 3, seed + 3) - 0.5f) * 0.5f);
                    c = Mix(c, Hex(0xE9EFF7), frost * 0.8f);
                    h += frost * 0.1f;
                }
                if (biome == Biome.Desert)
                {
                    float dust = N.Smooth(0.62f, 0.75f, N.Fbm(u, v, 5, 14, 3, seed + 4));
                    c = Mix(c, Hex(0xC6A57C), dust * 0.45f);
                }
                map.Albedo[i] = WithGloss(c, 0.4f - chips * 0.15f + rubber * 0.1f);
                map.Heights[i] = h;
            });
            return map;
        }

        // ------------------------------------------------------------------------------------------------
        // Barrier (U along, 4 m; V 0 foot -> 1 top, the top cap at V 0.9-1)
        // ------------------------------------------------------------------------------------------------

        public static SurfaceMap Barrier(Biome biome)
        {
            var map = new SurfaceMap(512, 128);
            int seed = 700 + (int)biome * 43;
            map.Paint((i, u, v) =>
            {
                Color c;
                float h;
                float gloss;
                float along = u * 4f;
                float fine = N.Fbm(u, v, 64, 16, 3, seed);
                switch (biome)
                {
                    case Biome.Industrial:
                        Guardrail(u, v, along, fine, seed, out c, out h, out gloss);
                        break;
                    case Biome.Snow:
                        Jersey(u, v, along, fine, seed, true, out c, out h, out gloss);
                        break;
                    case Biome.City:
                        Jersey(u, v, along, fine, seed, false, out c, out h, out gloss);
                        break;
                    default:
                        TyreWall(u, v, along, fine, seed, out c, out h, out gloss);
                        break;
                }
                // The reflector strip (white by day, it glows in the night look).
                float strip = Band(v, StripBand) + Band(v, TopStripBand);
                if (strip > 0f)
                {
                    c = Mix(c, Hex(0xE8ECEF), strip * 0.85f);
                    h += strip * 0.05f;
                    gloss = Mathf.Lerp(gloss, 0.7f, strip);
                }
                map.Albedo[i] = WithGloss(c, gloss);
                map.Heights[i] = h;
            });
            return map;
        }

        private static float Band(float v, Vector2 band)
        {
            return N.Smooth(band.x - 0.006f, band.x + 0.004f, v) * (1f - N.Smooth(band.y - 0.004f, band.y + 0.006f, v));
        }

        /// <summary>The mask of the glowing strips, the same for every barrier (white where it glows).</summary>
        public static Texture2D BarrierGlow()
        {
            const int width = 32;
            const int height = 128;
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                float v = (y + 0.5f) / height;
                float strip = Mathf.Clamp01(Band(v, StripBand) + Band(v, TopStripBand));
                for (int x = 0; x < width; x++)
                {
                    pixels[y * width + x] = new Color(strip, strip, strip, 1f);
                }
            }
            return N.ToTexture(pixels, width, height);
        }

        /// <summary>
        /// Stacked tyres, five columns per 4 m, three high; the top tyre of every column painted, white and red by turns,
        /// so from above the wall reads as a red and white line of rings.
        /// </summary>
        private static void TyreWall(float u, float v, float along, float fine, int seed, out Color c, out float h, out float gloss)
        {
            const float column = 4f / 5f;
            int index = Mathf.FloorToInt(along / column);
            float s = along / column - index;
            Color paint = index % 2 == 0 ? Hex(0xEDE8DE) : Hex(0xCC2A20);
            float worn = N.Smooth(0.25f, 0.4f, N.Fbm(u, v, 24, 6, 3, seed + 1));
            if (v >= 0.9f)
            {
                // The top tyre from above: a painted ring around a dark hole.
                float t = (v - 0.9f) / 0.1f;
                float r = new Vector2(s - 0.5f, (t - 0.5f) * 0.6f).magnitude * 2f;
                float rubber = N.Smooth(1f, 0.9f, r) * N.Smooth(0.36f, 0.46f, r);
                Color top = Mix(Hex(0x1C1C1D), Mix(Shade(paint, 0.8f), paint, N.Smooth(0.9f, 0.6f, r)), worn);
                c = Mix(Hex(0x050505), top, rubber);
                h = rubber * 0.7f;
                gloss = 0.2f;
                return;
            }
            float stack = v / 0.9f * 3f;
            int row = Mathf.FloorToInt(stack);
            float r2 = stack - row;
            // A tyre's side: rounded across the column and across its own height, two broad grooves of tread.
            float roundS = Mathf.Sin(s * Mathf.PI);
            float roundR = Mathf.Sin(r2 * Mathf.PI);
            float groove = Mathf.Abs(r2 - 0.33f) < 0.04f || Mathf.Abs(r2 - 0.67f) < 0.04f ? 1f : 0f;
            Color rubberColor = Mix(Hex(0x18181A), Hex(0x303033), fine * 0.6f + roundR * 0.4f);
            c = Shade(rubberColor, 0.3f + roundS * 0.8f);
            c = Mix(c, Hex(0x0A0A0A), groove * 0.6f);
            if (row == 2)
            {
                c = Mix(c, Shade(paint, 0.5f + roundS * 0.55f), worn * 0.95f);
            }
            float gap = 1f - N.Smooth(0.02f, 0.07f, Mathf.Min(s, 1f - s));
            c = Mix(c, Hex(0x030303), gap);
            float dust = N.Smooth(0.35f, 0f, v) * N.Smooth(0.4f, 0.6f, fine);
            c = Mix(c, Hex(0x9C7D5C), dust * 0.6f);
            h = roundS * roundR * 0.8f - groove * 0.15f;
            gloss = 0.18f;
        }

        /// <summary>A galvanised W-beam rail over posts every 2 m, with rust streaks and bolts; the post shadows below it.</summary>
        private static void Guardrail(float u, float v, float along, float fine, int seed, out Color c, out float h, out float gloss)
        {
            float post = Mathf.Abs(Mathf.Repeat(along + 1f, 2f) - 1f);
            bool atPost = post < 0.09f;
            Color steel = Mix(Hex(0x7E8388), Hex(0xA9AEB2), fine);
            float rust = N.Smooth(0.6f, 0.75f, N.Fbm(u, v * 0.3f, 12, 4, 3, seed + 2)) * N.Smooth(0.9f, 0.45f, v);
            if (v >= 0.9f)
            {
                c = Mix(Shade(steel, 1.08f), Hex(0x8C5232), rust * 0.6f);
                h = 0.8f;
                gloss = 0.55f;
                if (atPost)
                {
                    c = Shade(c, 0.75f);
                }
                return;
            }
            if (v < 0.42f)
            {
                c = atPost ? Mix(Hex(0x5A5F63), Hex(0x787D81), fine) : Mix(Hex(0x0C0D0E), Hex(0x1A1B1C), fine);
                c = Mix(c, Hex(0x5E4632), N.Smooth(0.15f, 0f, v) * 0.6f);
                h = atPost ? 0.5f : 0f;
                gloss = atPost ? 0.4f : 0.1f;
                return;
            }
            // Two corrugations of the beam.
            float t = (v - 0.42f) / 0.48f;
            float wave = Mathf.Cos(t * Mathf.PI * 4f);
            c = Shade(steel, 0.8f + wave * 0.22f);
            float streak = N.Smooth(0.55f, 0.7f, N.Fbm(u * 1f, v, 40, 2, 2, seed + 3)) * (1f - t);
            c = Mix(c, Hex(0x7B4A2E), Mathf.Max(rust, streak) * 0.65f);
            float bolt = atPost ? 1f - N.Smooth(0.012f, 0.02f, new Vector2(post, (t - 0.5f) * 0.48f).magnitude) : 0f;
            c = Mix(c, Hex(0x4B4F52), bolt);
            h = 0.5f + wave * 0.3f + bolt * 0.2f;
            gloss = 0.6f - rust * 0.4f;
        }

        /// <summary>
        /// Concrete jersey barrier sections (joints every 2 m): City has a hazard band and a few tags, Snow a red and white
        /// reflector band, slush at the foot and a cap of snow with drips.
        /// </summary>
        private static void Jersey(float u, float v, float along, float fine, int seed, bool snowy, out Color c, out float h, out float gloss)
        {
            float calm = 0.5f + (fine - 0.5f) * 0.45f;
            Color concrete = snowy ? Mix(Hex(0x8C9096), Hex(0xAFB3B8), calm) : Mix(Hex(0x928F88), Hex(0xB8B4AC), calm);
            float joint = Mathf.Abs(Mathf.Repeat(along + 1f, 2f) - 1f);
            float seam = 1f - N.Smooth(0.012f, 0.03f, joint);
            float stain = N.Smooth(0.55f, 0.75f, N.Fbm(u, v, 10, 3, 3, seed + 1));
            c = Mix(concrete, Shade(concrete, 0.78f), stain * 0.5f);
            h = 0.5f + fine * 0.1f;
            gloss = 0.22f;
            if (v >= 0.9f)
            {
                c = Shade(c, 1.06f);
                c = Mix(c, Hex(0x4F4D49), seam);
                if (snowy)
                {
                    c = Mix(Hex(0xDCE4EE), Hex(0xF5F8FC), fine);
                    h = 0.9f;
                    gloss = 0.3f;
                }
                return;
            }
            if (snowy)
            {
                // A red and white band and the cap's snow dripping over the edge.
                if (v > 0.5f && v < 0.6f)
                {
                    c = Mathf.Repeat(along, 1f) < 0.5f ? Hex(0xB8231C) : Hex(0xE6E2DA);
                    c = Mix(c, concrete, N.Smooth(0.7f, 0.85f, N.Fbm(u, v, 30, 8, 2, seed + 2)) * 0.6f);
                }
                float drip = 0.86f - N.Fbm(u, 0.5f, 48, 1, 3, seed + 3) * 0.12f - N.Smooth(0.75f, 0.95f, N.Hash(Mathf.FloorToInt(u * 96f), 0, seed + 4)) * 0.08f;
                if (v > drip)
                {
                    c = Mix(Hex(0xD3DCE8), Hex(0xF2F6FB), fine);
                    h = 0.8f;
                    gloss = 0.35f;
                }
                float slush = N.Smooth(0.28f + fine * 0.1f, 0.05f, v);
                c = Mix(c, Mix(Hex(0x6B6762), Hex(0xC9D2DE), N.Smooth(0.12f, 0f, v)), slush);
            }
            else
            {
                // Black and yellow hazard band; spray painted tags on some sections.
                if (v > 0.56f && v < 0.66f)
                {
                    float diagonal = Mathf.Repeat(along * 2.5f + v * 2.2f, 1f);
                    c = diagonal < 0.5f ? Hex(0xE2AE1F) : Hex(0x1D1C1A);
                    c = Mix(c, concrete, N.Smooth(0.72f, 0.85f, N.Fbm(u, v, 30, 8, 2, seed + 5)) * 0.7f);
                }
                int section = Mathf.FloorToInt(along / 2f);
                if (section == 1 && v > 0.15f && v < 0.5f)
                {
                    float tag = N.Smooth(0.9f, 0.96f, N.Ridge(u * 2f, v, 6, 3, 2, seed + 6));
                    c = Mix(c, Hex(0xC23B9A), tag * 0.9f);
                    float tag2 = N.Smooth(0.92f, 0.97f, N.Ridge(u * 2f + 0.3f, v, 5, 3, 2, seed + 7));
                    c = Mix(c, Hex(0x2D9BD1), tag2 * 0.9f);
                }
                float grime = N.Smooth(0.3f, 0f, v);
                c = Mix(c, Hex(0x48443F), grime * 0.55f);
            }
            // The chamfer at the foot of the jersey profile.
            float foot = 1f - N.Smooth(0.18f, 0.24f, v);
            c = Shade(c, 1f - foot * 0.12f);
            c = Mix(c, Hex(0x3E3C39), seam * 0.9f);
            h -= seam * 0.3f;
        }

        // ------------------------------------------------------------------------------------------------
        // Line (U = metres across / 4, V = metres along / 4: a 4 m tile)
        // ------------------------------------------------------------------------------------------------

        /// <summary>The start and finish line: white bands at both sides of three rows of 1 m checks, scuffed by tyres.</summary>
        public static SurfaceMap Line()
        {
            var map = new SurfaceMap(256, 256);
            map.Paint((i, u, v) =>
            {
                float across = u * 4f;
                float along = v * 4f;
                Color white = Hex(0xEEEDE8);
                Color black = Hex(0x161616);
                Color c;
                if (along < 0.5f || along > 3.5f)
                {
                    c = white;
                    float edge = Mathf.Min(Mathf.Abs(along - 0.5f), Mathf.Abs(along - 3.5f));
                    c = Mix(c, Shade(white, 0.85f), 1f - N.Smooth(0f, 0.04f, edge));
                }
                else
                {
                    int col = Mathf.FloorToInt(across);
                    int row = Mathf.FloorToInt(along - 0.5f);
                    c = (col + row) % 2 == 0 ? black : white;
                }
                float scuff = N.Smooth(0.45f, 0.75f, N.Fbm(u, v, 4, 4, 3, 801)) * N.Smooth(0.4f, 0.7f, N.Fbm(u, v, 40, 40, 2, 802));
                c = Mix(c, Hex(0x2A2928), scuff * 0.5f);
                float chips = N.Smooth(0.74f, 0.8f, N.Fbm(u, v, 24, 24, 3, 803));
                c = Mix(c, Hex(0x3A3836), chips * 0.6f);
                map.Albedo[i] = WithGloss(c, 0.35f + scuff * 0.1f);
                map.Heights[i] = 0.5f - chips * 0.1f;
            });
            return map;
        }
    }
}
