using System;
using UnityEngine;
using N = Portfolio.DeathRace.EditorTools.DeathRaceArtNoise;

namespace Portfolio.DeathRace.EditorTools
{
    /// <summary>
    /// A generated surface: its colours (alpha = how glossy each spot is: the material's smoothness is multiplied by it,
    /// so the same picture is dry by day and wet at night) and a height field for its normal map. Both tile.
    /// </summary>
    internal sealed class SurfaceMap
    {
        public readonly int Width;
        public readonly int Height;
        public readonly Color[] Albedo;
        public readonly float[] Heights;

        public SurfaceMap(int width, int height)
        {
            Width = width;
            Height = height;
            Albedo = new Color[width * height];
            Heights = new float[width * height];
        }

        /// <summary>Runs <paramref name="pixel"/> for every texel with its (u, v) centre in the unit square.</summary>
        public void Paint(Action<int, float, float> pixel)
        {
            for (int y = 0; y < Height; y++)
            {
                float v = (y + 0.5f) / Height;
                for (int x = 0; x < Width; x++)
                {
                    pixel(y * Width + x, (x + 0.5f) / Width, v);
                }
            }
        }

        public Texture2D AlbedoTexture()
        {
            return N.ToTexture(Albedo, Width, Height);
        }

        public Texture2D NormalTexture(float strength)
        {
            return N.ToTexture(N.NormalMap(Heights, Width, Height, strength), Width, Height, true);
        }
    }


    /// <summary>
    /// The pictures of the land the tracks are built on, one set per biome (shared by both looks: the night look changes
    /// the light, the tint and how wet the materials are, not the pictures). The ground tiles every 32 m and carries the
    /// small things (pebbles, cracks, ripples, slabs); a soft macro picture over ~100 m breaks its repetition. The
    /// road, shoulder, curb, barrier and line follow the UV convention agreed with the track builder (see
    /// DeathRaceArtBuilder): road and shoulder across 0..1, along in tiles of 16 m and 8 m.
    /// </summary>
    internal static partial class DeathRaceArtSurfaces
    {
        public const int GroundPixels = 1024;
        public const int MacroPixels = 256;

        public static Color Hex(uint rgb, float alpha = 1f)
        {
            return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, alpha);
        }

        private static Color Mix(Color a, Color b, float t)
        {
            return Color.LerpUnclamped(a, b, Mathf.Clamp01(t));
        }

        private static Color Shade(Color c, float factor)
        {
            return new Color(c.r * factor, c.g * factor, c.b * factor, c.a);
        }

        private static Color WithGloss(Color c, float gloss)
        {
            c.a = Mathf.Clamp01(gloss);
            return c;
        }

        // ------------------------------------------------------------------------------------------------
        // Ground
        // ------------------------------------------------------------------------------------------------

        public static SurfaceMap Ground(Biome biome)
        {
            switch (biome)
            {
                case Biome.Industrial: return IndustrialGround();
                case Biome.Snow: return SnowGround();
                case Biome.City: return CityGround();
                default: return DesertGround();
            }
        }

        /// <summary>How strongly each ground's heights bend its normals.</summary>
        public static float GroundRelief(Biome biome)
        {
            switch (biome)
            {
                case Biome.Industrial: return 5f;
                case Biome.Snow: return 11f;
                case Biome.City: return 4f;
                default: return 6f;
            }
        }

        /// <summary>
        /// Sun baked desert: sand in soft patches of several sizes, wind ripples where it drifts, darker stony "desert
        /// pavement", a few fields of cracked mud, scattered pebbles, rare tufts of dry scrub and the ruts of an old track.
        /// Nothing large and distinct, so the 32 m tile does not show.
        /// </summary>
        private static SurfaceMap DesertGround()
        {
            var map = new SurfaceMap(GroundPixels, GroundPixels);
            Color sand = Hex(0xC29C6C);
            Color sandDark = Hex(0x977050);
            Color sandLight = Hex(0xD8BC92);
            Color pavement = Hex(0x8C6A4E);
            Color mud = Hex(0xAE8660);
            Color crack = Hex(0x75533A);
            Color scrub = Hex(0x7D7444);
            Color scrubDark = Hex(0x544A2C);
            map.Paint((i, u, v) =>
            {
                float patches = N.Fbm(u, v, 6, 5, 11);
                float broad = N.Fbm(u, v, 2, 3, 111);
                float fine = N.Fbm(u, v, 96, 3, 12);
                float grain = (N.Hash((int)(u * 4096f), (int)(v * 4096f), 112) - 0.5f) * 0.05f;
                Color c = Mix(sandDark, sandLight, 0.3f + patches * 0.55f + (broad - 0.5f) * 0.35f);
                c = Mix(c, sand, 0.3f);
                c = Shade(c, 1f + grain + (fine - 0.5f) * 0.06f);
                float h = patches * 0.3f + fine * 0.08f;

                // Ripples on the drifts: bands across the wind, bent by noise (whole waves per tile so they repeat cleanly).
                float drift = N.Smooth(0.47f, 0.62f, N.Fbm(u, v, 4, 3, 13));
                float warp = N.Fbm(u, v, 6, 2, 14) * 0.6f;
                float ripple = Mathf.Sin((v * 48f + u * 7f + warp * 6f) * Mathf.PI * 2f) * 0.5f + 0.5f;
                h += ripple * drift * 0.16f;
                c = Mix(c, sandLight, drift * 0.3f);
                c = Shade(c, 1f - drift * (1f - ripple) * 0.07f);

                // Desert pavement: darker ground packed with small stones.
                float stony = N.Smooth(0.6f, 0.72f, N.Fbm(u, v, 7, 4, 22)) * (1f - drift);
                N.Cell grit = N.Cells(u, v, 260, 260, 23, 0.9f);
                float chip = 1f - N.Smooth(0.25f, 0.45f, grit.F1);
                c = Mix(c, Mix(pavement, Mix(Hex(0x5E4A3A), Hex(0xA08A72), grit.Id), chip * 0.7f), stony * 0.45f);
                h += stony * chip * 0.12f;

                // A few fields of cracked mud where the sand is thin.
                N.Cell cell = N.Cells(u, v, 30, 30, 15, 0.9f);
                float lines = 1f - N.Smooth(0.015f, 0.055f, cell.Edge);
                float crackMask = (1f - drift) * (1f - stony) * N.Smooth(0.6f, 0.7f, N.Fbm(u, v, 5, 3, 16));
                c = Mix(c, Mix(mud, sand, cell.Id * 0.5f), crackMask * 0.4f);
                c = Mix(c, crack, lines * crackMask * 0.55f);
                h -= lines * crackMask * 0.2f;

                // Pebbles, thicker in some places than others.
                N.Cell stones = N.Cells(u, v, 110, 110, 17, 0.8f);
                float density = N.Smooth(0.35f, 0.75f, N.Fbm(u, v, 8, 3, 24));
                float radius = 0.16f + stones.Id * 0.2f;
                float pebble = stones.Id > 1f - density * 0.45f ? 1f - N.Smooth(radius * 0.7f, radius, stones.F1) : 0f;
                Color stone = Mix(Hex(0x7A6656), Hex(0xB9A58E), N.Hash((int)(stones.Id * 9973f), 3, 18));
                c = Mix(c, stone, pebble * 0.85f);
                h += pebble * Mathf.Sqrt(Mathf.Max(0f, 1f - stones.F1 / radius)) * 0.35f;

                // Dry scrub tufts, rare.
                N.Cell tufts = N.Cells(u, v, 16, 16, 19, 0.9f);
                float tuft = tufts.Id < 0.07f ? 1f - N.Smooth(0.1f, 0.2f, tufts.F1 + (fine - 0.5f) * 0.12f) : 0f;
                float blades = N.Ridge(u, v, 180, 180, 1, 20);
                c = Mix(c, Mix(scrubDark, scrub, blades), tuft * 0.8f);
                h += tuft * blades * 0.2f;

                // An old pair of ruts meandering through (a whole number of waves, so it tiles).
                float track = u - 0.62f - Mathf.Sin(v * Mathf.PI * 2f) * 0.06f - Mathf.Sin(v * Mathf.PI * 6f + 1f) * 0.012f;
                float rut = Mathf.Max(1f - Mathf.Abs(track) / 0.006f, 1f - Mathf.Abs(track - 0.05f) / 0.006f);
                rut = Mathf.Clamp01(rut) * (0.4f + 0.4f * N.Fbm(u, v, 9, 2, 21));
                c = Mix(c, Shade(c, 0.88f), rut);
                h -= rut * 0.1f;

                float gloss = 0.16f + pebble * 0.2f + stony * 0.08f - lines * crackMask * 0.1f;
                map.Albedo[i] = WithGloss(c, gloss);
                map.Heights[i] = h;
            });
            return map;
        }

        /// <summary>
        /// The yard: packed dirt and gravel, worn concrete pads with seams, rust and oil stains, puddles in the low spots
        /// and weeds along the cracks.
        /// </summary>
        private static SurfaceMap IndustrialGround()
        {
            var map = new SurfaceMap(GroundPixels, GroundPixels);
            Color dirt = Hex(0x5E5A52);
            Color dirtDark = Hex(0x433F39);
            Color dirtLight = Hex(0x76726A);
            Color concrete = Hex(0x8E8B84);
            Color concreteDark = Hex(0x6E6B66);
            Color rust = Hex(0x8A4E2A);
            Color oil = Hex(0x1E1C1B);
            Color weed = Hex(0x55613A);
            map.Paint((i, u, v) =>
            {
                float patches = N.Fbm(u, v, 8, 5, 31);
                float fine = N.Fbm(u, v, 80, 2, 32);
                Color c = Mix(dirtDark, dirtLight, patches * 1.2f - 0.1f);
                float h = patches * 0.25f + fine * 0.06f;

                // Gravel everywhere on the dirt.
                N.Cell gravel = N.Cells(u, v, 170, 170, 33, 0.9f);
                float grit = 1f - N.Smooth(0.2f, 0.42f, gravel.F1);
                Color stone = Mix(Hex(0x55534F), Hex(0x96938C), gravel.Id);
                c = Mix(c, stone, grit * 0.5f);
                h += grit * 0.15f;

                // Broken concrete slabs (4 m) in patches small enough not to show the tile.
                float pad = N.Smooth(0.56f, 0.6f, N.Fbm(u, v, 6, 3, 34));
                float sx = u * 8f;
                float sy = v * 8f;
                float seam = Mathf.Min(Mathf.Abs(sx - Mathf.Round(sx)), Mathf.Abs(sy - Mathf.Round(sy)));
                float joint = 1f - N.Smooth(0.006f, 0.016f, seam);
                float slabId = N.Hash(Mathf.FloorToInt(sx), Mathf.FloorToInt(sy), 35);
                Color slab = Mix(concreteDark, concrete, 0.55f + slabId * 0.45f);
                slab = Mix(slab, Shade(slab, 0.85f), N.Fbm(u, v, 24, 3, 36));
                float crackLine = N.Smooth(0.86f, 0.95f, N.Ridge(u, v, 10, 10, 3, 37)) * N.Smooth(0.4f, 0.7f, slabId);
                slab = Mix(slab, Hex(0x45423E), crackLine);
                c = Mix(c, slab, pad);
                c = Mix(c, Hex(0x3C3935), joint * pad * 0.8f);
                h = Mathf.Lerp(h, 0.45f + fine * 0.03f - joint * 0.12f - crackLine * 0.08f, pad);

                // Rust and oil.
                float rustStain = N.Smooth(0.68f, 0.8f, N.Fbm(u, v, 9, 4, 38));
                c = Mix(c, Mix(rust, c, fine), rustStain * 0.55f);
                float oilStain = N.Smooth(0.72f, 0.8f, N.Fbm(u, v, 10, 4, 39));
                c = Mix(c, oil, oilStain * 0.8f);

                // Puddles in the low spots of the dirt.
                float puddle = (1f - pad) * N.Smooth(0.3f, 0.24f, patches);
                c = Mix(c, Shade(dirtDark, 0.7f), puddle * 0.7f);
                h = Mathf.Lerp(h, 0.05f, puddle);

                // Weeds in the cracks and at the pad edges.
                float weeds = N.Smooth(0.78f, 0.9f, N.Fbm(u, v, 30, 2, 40)) * (joint * pad + crackLine * 0.6f + (1f - pad) * 0.25f);
                c = Mix(c, weed, Mathf.Clamp01(weeds) * 0.8f);

                float gloss = 0.16f + pad * 0.06f + oilStain * 0.6f + puddle * 0.75f;
                map.Albedo[i] = WithGloss(c, gloss);
                map.Heights[i] = h;
            });
            return map;
        }

        /// <summary>
        /// Wind packed snow with soft drifts and ridges, sheets of ice, ruts of compacted snow, and dark ground with
        /// dry grass where it blew clear.
        /// </summary>
        private static SurfaceMap SnowGround()
        {
            var map = new SurfaceMap(GroundPixels, GroundPixels);
            Color snow = Hex(0xD6E0EE);
            Color snowShade = Hex(0xAABBD6);
            Color ice = Hex(0x9FC0D8);
            Color iceDeep = Hex(0x6E94B3);
            Color earth = Hex(0x5B5249);
            Color grass = Hex(0x8F8463);
            map.Paint((i, u, v) =>
            {
                float drifts = N.Fbm(u, v, 3, 5, 51);
                float fine = N.Fbm(u, v, 70, 2, 52);
                // Wind ridges: stretched noise.
                float sastrugi = N.Ridge(u, v, 6, 22, 3, 53);
                float h = drifts * 0.8f + sastrugi * 0.22f + fine * 0.03f;
                Color c = Mix(snowShade, snow, 0.35f + drifts * 0.7f + (sastrugi - 0.3f) * 0.25f);

                // Ice sheets in the hollows.
                float iceMask = N.Smooth(0.3f, 0.25f, drifts) * N.Smooth(0.55f, 0.68f, N.Fbm(u, v, 4, 3, 54));
                float iceCracks = N.Smooth(0.9f, 0.97f, N.Ridge(u, v, 14, 14, 2, 55));
                Color iceColor = Mix(iceDeep, ice, N.Fbm(u, v, 12, 3, 56));
                iceColor = Mix(iceColor, Hex(0xE8F2FA), iceCracks * 0.8f);
                c = Mix(c, iceColor, iceMask);
                h = Mathf.Lerp(h, 0.22f + iceCracks * 0.02f, iceMask);

                // Ground showing through on the windward bumps.
                float bare = N.Smooth(0.8f, 0.86f, drifts * 0.6f + N.Fbm(u, v, 9, 3, 57) * 0.5f);
                float tufts = N.Ridge(u, v, 160, 160, 1, 58);
                Color ground = Mix(earth, grass, tufts * 0.8f);
                ground = Mix(ground, snow, N.Smooth(0.6f, 0.75f, fine) * 0.7f);
                c = Mix(c, ground, bare * 0.85f);
                h += bare * tufts * 0.06f;

                // Compacted ruts of a snowcat (a whole wave per tile).
                float track = u - 0.3f - Mathf.Sin(v * Mathf.PI * 2f + 0.7f) * 0.08f;
                float rut = Mathf.Max(1f - Mathf.Abs(track) / 0.014f, 1f - Mathf.Abs(track - 0.06f) / 0.014f);
                rut = Mathf.Clamp01(rut) * (1f - iceMask);
                float tread = Mathf.Sin(v * 1400f) * 0.5f + 0.5f;
                c = Mix(c, Mix(snowShade, Shade(snowShade, 0.92f), tread), rut * 0.35f);
                h -= rut * 0.1f;

                float gloss = 0.3f + iceMask * 0.6f - bare * 0.2f;
                map.Albedo[i] = WithGloss(c, gloss);
                map.Heights[i] = h;
            });
            return map;
        }

        /// <summary>
        /// Pavement: 2 m concrete slabs in slightly different greys, seams, cracked and patched slabs, stains, drains, a
        /// few puddles that shine at night.
        /// </summary>
        private static SurfaceMap CityGround()
        {
            var map = new SurfaceMap(GroundPixels, GroundPixels);
            Color slabLight = Hex(0xA6A49E);
            Color slabDark = Hex(0x7F7D78);
            Color seamColor = Hex(0x4E4C49);
            Color stain = Hex(0x55524C);
            map.Paint((i, u, v) =>
            {
                const float slabs = 16f;
                float sx = u * slabs;
                float sy = v * slabs;
                int ix = Mathf.FloorToInt(sx);
                int iy = Mathf.FloorToInt(sy);
                float fx = sx - ix;
                float fy = sy - iy;
                float slabId = N.Hash(N.Mod(ix, 16), N.Mod(iy, 16), 71);
                float seam = Mathf.Min(Mathf.Min(fx, 1f - fx), Mathf.Min(fy, 1f - fy));
                float joint = 1f - N.Smooth(0.012f, 0.03f, seam);
                float fine = N.Fbm(u, v, 96, 2, 72);
                Color c = Mix(slabDark, slabLight, 0.62f + slabId * 0.3f);
                c = Mix(c, Shade(c, 0.9f), N.Fbm(u, v, 20, 3, 73));
                c = Shade(c, 0.97f + fine * 0.06f);
                float h = 0.5f + fine * 0.04f - joint * 0.3f;

                // Some slabs cracked, a few replaced with asphalt.
                bool patched = slabId > 0.975f;
                if (patched)
                {
                    c = Mix(Hex(0x5E5E60), Hex(0x6E6E70), fine);
                    h = 0.45f + fine * 0.05f;
                }
                float cracked = slabId > 0.7f && !patched ? 1f : 0f;
                float crackLine = N.Smooth(0.9f, 0.96f, N.Ridge(u, v, 24, 24, 2, 74)) * cracked;
                c = Mix(c, seamColor, crackLine);
                h -= crackLine * 0.15f;
                c = Mix(c, seamColor, joint * 0.85f);

                // Stains, chewing gum spots, a drain now and then.
                float stains = N.Smooth(0.64f, 0.76f, N.Fbm(u, v, 8, 4, 75));
                c = Mix(c, stain, stains * 0.4f);
                N.Cell gum = N.Cells(u, v, 90, 90, 76, 0.9f);
                float spot = gum.Id > 0.86f ? 1f - N.Smooth(0.08f, 0.14f, gum.F1) : 0f;
                c = Mix(c, Hex(0x3E3D3B), spot * 0.7f);
                bool drain = N.Hash(N.Mod(ix, 16), N.Mod(iy, 16), 77) > 0.992f;
                float grate = 0f;
                if (drain)
                {
                    float gx = Mathf.Abs(fx - 0.5f);
                    float gy = Mathf.Abs(fy - 0.5f);
                    if (gx < 0.16f && gy < 0.1f)
                    {
                        grate = 1f;
                        bool bar = Mathf.Repeat(fx * 22f, 1f) < 0.45f;
                        c = bar ? Hex(0x2C2B2A) : Hex(0x0E0E0E);
                        h = bar ? 0.48f : 0.2f;
                    }
                }

                // Puddles in sunken slabs.
                float puddle = slabId < 0.08f ? N.Smooth(0.42f, 0.55f, N.Fbm(u, v, 30, 3, 78)) : 0f;
                c = Mix(c, Shade(c, 0.8f), puddle * 0.35f);
                h -= puddle * 0.05f;

                float gloss = 0.22f + puddle * 0.7f + stains * 0.1f + grate * 0.3f + (patched ? 0.1f : 0f);
                map.Albedo[i] = WithGloss(c, gloss);
                map.Heights[i] = h;
            });
            return map;
        }

        /// <summary>
        /// The large scale variation over the ground (URP's detail albedo, multiplied in at twice its value, so 0.5 grey
        /// leaves the ground as it is): soft patches a little lighter, darker or of another hue, ~100 m across. Linear.
        /// </summary>
        public static Texture2D Macro(Biome biome)
        {
            int size = MacroPixels;
            var pixels = new Color[size * size];
            Color warm;
            Color cool;
            switch (biome)
            {
                case Biome.Industrial:
                    warm = new Color(0.56f, 0.5f, 0.44f);
                    cool = new Color(0.44f, 0.47f, 0.5f);
                    break;
                case Biome.Snow:
                    warm = new Color(0.53f, 0.52f, 0.5f);
                    cool = new Color(0.45f, 0.48f, 0.53f);
                    break;
                case Biome.City:
                    warm = new Color(0.54f, 0.52f, 0.49f);
                    cool = new Color(0.45f, 0.47f, 0.5f);
                    break;
                default:
                    warm = new Color(0.55f, 0.5f, 0.44f);
                    cool = new Color(0.47f, 0.48f, 0.49f);
                    break;
            }
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size;
                    float v = (y + 0.5f) / size;
                    float hue = N.Fbm(u, v, 3, 4, 91 + (int)biome);
                    float light = N.Fbm(u, v, 5, 4, 95 + (int)biome);
                    Color c = Color.Lerp(cool, warm, N.Smooth(0.3f, 0.7f, hue));
                    float scale = 0.84f + light * 0.32f;
                    pixels[y * size + x] = new Color(c.r * scale, c.g * scale, c.b * scale, 1f);
                }
            }
            return N.ToTexture(pixels, size, size, true);
        }
    }
}
