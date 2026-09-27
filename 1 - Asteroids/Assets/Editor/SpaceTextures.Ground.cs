using System;
using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Asteroids.EditorTools
{
    /// <summary>Named colours of the ground palette (terrain props and ground units); append only, the value is the texel.</summary>
    internal enum GroundSwatch
    {
        Sand, SandLight, SandDark, Ochre, Clay, Terracotta, Brick, Rust,
        Olive, OliveDark, Khaki, Tan, ArmyGreen, ArmyDark, Leaf, LeafDark,
        LeafLight, Moss, Palm, Trunk, Bark, Straw, Concrete, ConcreteDark,
        ConcreteLight, Asphalt, Steel, SteelDark, Metal, RoofRed, RoofBlue, RoofGreen,
        Tarp, Canvas, Wood, WoodDark, Stone, StoneDark, Basalt, Ash,
        Regolith, RegolithDark, White, Black, Glass, Hazard, HazardDark, EnemyRed,
        EnemyRedDark, Navy, BoatGrey, Solar, Pipe, PipeDark, Cactus, Crop,
        GlowRed, GlowOrange, GlowYellow, GlowCyan, GlowWhite, GlowGreen, Window, Lamp,
        Rubber, Gold, Mud, LeafDeep, Flower, Teal, Plate, PlateDark,
        Mare, ObsidianGlow, Lava, Coolant
    }


    /// <summary>
    /// The ground textures of the strike mode: the ground palette of the props and units, periodic noise, the tiling
    /// detail layers of the terrain (colour with the default sun's light and shadows baked in, height in alpha) and the
    /// per-tile mask and colour maps the ground shader lays them out with, plus decals.
    /// </summary>
    internal static partial class SpaceTextures
    {
        /// <summary>
        /// Direction toward the default strike sun in texture space (x right, y up the screen, z toward the camera): the
        /// sun of every theme uses it, so baked shading, baked prop shadows and the drop shadows all agree
        /// (<see cref="StrikeRules.ShadowOffset"/> points the other way).
        /// </summary>
        public static readonly Vector3 GroundLight = new Vector3(-0.469f, 0.625f, 0.625f).normalized;

        /// <summary>Ground distance a shadow falls per metre of height (along -GroundLight's ground part).</summary>
        public static Vector2 ShadowStep => new Vector2(-GroundLight.x, -GroundLight.y) / GroundLight.z;

        // ------------------------------------------------------------------ ground palette

        private static readonly Dictionary<GroundSwatch, (string hex, float glow)> GroundColors = new Dictionary<GroundSwatch, (string, float)>
        {
            { GroundSwatch.Sand, ("D2B07A", 0f) }, { GroundSwatch.SandLight, ("E6CC98", 0f) }, { GroundSwatch.SandDark, ("A9854F", 0f) },
            { GroundSwatch.Ochre, ("B98A3E", 0f) }, { GroundSwatch.Clay, ("A0633C", 0f) }, { GroundSwatch.Terracotta, ("B8542F", 0f) },
            { GroundSwatch.Brick, ("8C3B2A", 0f) }, { GroundSwatch.Rust, ("7A4128", 0f) },
            { GroundSwatch.Olive, ("6B6B3A", 0f) }, { GroundSwatch.OliveDark, ("45462A", 0f) }, { GroundSwatch.Khaki, ("9C9063", 0f) },
            { GroundSwatch.Tan, ("B39C72", 0f) }, { GroundSwatch.ArmyGreen, ("56613E", 0f) }, { GroundSwatch.ArmyDark, ("33392A", 0f) },
            { GroundSwatch.Leaf, ("3F7A33", 0f) }, { GroundSwatch.LeafDark, ("27501F", 0f) },
            { GroundSwatch.LeafLight, ("6FA045", 0f) }, { GroundSwatch.Moss, ("5B7A3A", 0f) }, { GroundSwatch.Palm, ("4E8A3A", 0f) },
            { GroundSwatch.Trunk, ("6A4B30", 0f) }, { GroundSwatch.Bark, ("4A3424", 0f) }, { GroundSwatch.Straw, ("C9B26A", 0f) },
            { GroundSwatch.Concrete, ("9A9892", 0f) }, { GroundSwatch.ConcreteDark, ("6E6D69", 0f) },
            { GroundSwatch.ConcreteLight, ("BDBAB2", 0f) }, { GroundSwatch.Asphalt, ("3A3B3E", 0f) }, { GroundSwatch.Steel, ("7D858C", 0f) },
            { GroundSwatch.SteelDark, ("4A5057", 0f) }, { GroundSwatch.Metal, ("A3AAB0", 0f) }, { GroundSwatch.RoofRed, ("9E3B2E", 0f) },
            { GroundSwatch.RoofBlue, ("3E5E7E", 0f) }, { GroundSwatch.RoofGreen, ("4C6B4A", 0f) },
            { GroundSwatch.Tarp, ("5F6A45", 0f) }, { GroundSwatch.Canvas, ("B7A983", 0f) }, { GroundSwatch.Wood, ("8A6440", 0f) },
            { GroundSwatch.WoodDark, ("5C4128", 0f) }, { GroundSwatch.Stone, ("8B8378", 0f) }, { GroundSwatch.StoneDark, ("5E574F", 0f) },
            { GroundSwatch.Basalt, ("2E2B2D", 0f) }, { GroundSwatch.Ash, ("4A4644", 0f) },
            { GroundSwatch.Regolith, ("8E8C88", 0f) }, { GroundSwatch.RegolithDark, ("5C5B59", 0f) }, { GroundSwatch.White, ("E8E6E0", 0f) },
            { GroundSwatch.Black, ("18191C", 0f) }, { GroundSwatch.Glass, ("22364A", 0f) }, { GroundSwatch.Hazard, ("E0AE22", 0f) },
            { GroundSwatch.HazardDark, ("26221C", 0f) }, { GroundSwatch.EnemyRed, ("B8322A", 0f) },
            { GroundSwatch.EnemyRedDark, ("74201C", 0f) }, { GroundSwatch.Navy, ("26344F", 0f) }, { GroundSwatch.BoatGrey, ("6C747C", 0f) },
            { GroundSwatch.Solar, ("1F2E52", 0f) }, { GroundSwatch.Pipe, ("8F959A", 0f) }, { GroundSwatch.PipeDark, ("555B61", 0f) },
            { GroundSwatch.Cactus, ("5E8A48", 0f) }, { GroundSwatch.Crop, ("8FA84A", 0f) },
            { GroundSwatch.GlowRed, ("FF3A2E", 1f) }, { GroundSwatch.GlowOrange, ("FF8A2A", 1f) }, { GroundSwatch.GlowYellow, ("FFD760", 1f) },
            { GroundSwatch.GlowCyan, ("5CF2FF", 1f) }, { GroundSwatch.GlowWhite, ("FFFFFF", 1f) }, { GroundSwatch.GlowGreen, ("6CFF8A", 1f) },
            { GroundSwatch.Window, ("FFD58A", 0.8f) }, { GroundSwatch.Lamp, ("FFE9B0", 1f) },
            { GroundSwatch.Rubber, ("2A2A2C", 0f) }, { GroundSwatch.Gold, ("C9A13F", 0f) }, { GroundSwatch.Mud, ("5E4A32", 0f) },
            { GroundSwatch.LeafDeep, ("1D3B1A", 0f) }, { GroundSwatch.Flower, ("C75A7A", 0f) }, { GroundSwatch.Teal, ("2F7F7A", 0f) },
            { GroundSwatch.Plate, ("7C8490", 0f) }, { GroundSwatch.PlateDark, ("4B525C", 0f) },
            { GroundSwatch.Mare, ("4E4D4C", 0f) }, { GroundSwatch.ObsidianGlow, ("FF5A1A", 0.6f) }, { GroundSwatch.Lava, ("FF7A20", 1f) },
            { GroundSwatch.Coolant, ("40E0FF", 0.7f) }
        };


        public static Color GroundColor(GroundSwatch swatch)
        {
            return ColorUtility.TryParseHtmlString("#" + GroundColors[swatch].hex, out Color color) ? color : Color.magenta;
        }


        /// <summary>The ground palette (or its emission), one texel per <see cref="GroundSwatch"/>, laid out like <see cref="EditorTools.Palette"/>.</summary>
        public static Texture2D GroundPalette(bool emission)
        {
            int size = EditorTools.Palette.Size;
            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = emission ? Color.black : Color.magenta;
            }
            foreach (KeyValuePair<GroundSwatch, (string hex, float glow)> entry in GroundColors)
            {
                Color color = GroundColor(entry.Key);
                pixels[(int)entry.Key] = emission ? color * Mathf.Min(1f, entry.Value.glow) : color;
            }
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        // ------------------------------------------------------------------ periodic noise

        /// <summary>Value noise at (x, y) in 0..1 space with <paramref name="cellsX"/> by <paramref name="cellsY"/> cells, repeating every 1 in both.</summary>
        public static float PNoise(float x, float y, int cellsX, int cellsY, int seed)
        {
            x *= cellsX;
            y *= cellsY;
            int x0 = Mathf.FloorToInt(x);
            int y0 = Mathf.FloorToInt(y);
            float fx = x - x0;
            float fy = y - y0;
            float u = fx * fx * (3f - 2f * fx);
            float v = fy * fy * (3f - 2f * fy);
            float a = Hash(Mod(x0, cellsX), Mod(y0, cellsY), seed);
            float b = Hash(Mod(x0 + 1, cellsX), Mod(y0, cellsY), seed);
            float c = Hash(Mod(x0, cellsX), Mod(y0 + 1, cellsY), seed);
            float d = Hash(Mod(x0 + 1, cellsX), Mod(y0 + 1, cellsY), seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
        }


        /// <summary>Fractal <see cref="PNoise"/> in 0..1, repeating every 1 in both directions.</summary>
        public static float PFbm(float x, float y, int cellsX, int cellsY, int octaves, int seed, float persistence = 0.5f)
        {
            float value = 0f;
            float amplitude = 0.5f;
            float total = 0f;
            for (int o = 0; o < octaves; o++)
            {
                value += amplitude * PNoise(x, y, cellsX, cellsY, seed + o * 31);
                total += amplitude;
                amplitude *= persistence;
                cellsX *= 2;
                cellsY *= 2;
            }
            return value / total;
        }


        /// <summary>
        /// Periodic Voronoi cells at (x, y) in 0..1 space: returns the distance to the nearest cell point; <paramref name="edge"/>
        /// is the distance to the nearest cell border and <paramref name="id"/> a 0..1 value of the cell (cell units).
        /// </summary>
        public static float PCells(float x, float y, int cellsX, int cellsY, int seed, out float edge, out float id)
        {
            x *= cellsX;
            y *= cellsY;
            int cx = Mathf.FloorToInt(x);
            int cy = Mathf.FloorToInt(y);
            float first = float.MaxValue;
            float second = float.MaxValue;
            id = 0f;
            for (int oy = -1; oy <= 1; oy++)
            {
                for (int ox = -1; ox <= 1; ox++)
                {
                    int gx = cx + ox;
                    int gy = cy + oy;
                    int hx = Mod(gx, cellsX);
                    int hy = Mod(gy, cellsY);
                    float fx = gx + Hash(hx, hy, seed);
                    float fy = gy + Hash(hx, hy, seed + 1);
                    float distance = Mathf.Sqrt((fx - x) * (fx - x) + (fy - y) * (fy - y));
                    if (distance < first)
                    {
                        second = first;
                        first = distance;
                        id = Hash(hx, hy, seed + 2);
                    }
                    else if (distance < second)
                    {
                        second = distance;
                    }
                }
            }
            edge = (second - first) * 0.5f;
            return first;
        }


        /// <summary>A smooth step from <paramref name="edge0"/> to <paramref name="edge1"/> (for the ground recipes).</summary>
        public static float Step(float edge0, float edge1, float x)
        {
            return Smooth(edge0, edge1, x);
        }

        // ------------------------------------------------------------------ detail layers

        /// <summary>
        /// A tiling detail layer: its height field and colouring over 0..1 texture space covering <see cref="Meters"/>. The
        /// generator bakes the default sun's shading and cast shadows into the colour.
        /// </summary>
        internal sealed class Surface
        {
            public int Size = 512;
            /// <summary>Metres the texture covers (its repeat).</summary>
            public float Meters = 5f;
            /// <summary>Metres of relief for a height of 1.</summary>
            public float Relief = 0.05f;
            /// <summary>Height 0..1 at texture coordinates (u, v).</summary>
            public Func<float, float, float> Height;
            /// <summary>Unlit colour at (u, v) with height h.</summary>
            public Func<float, float, float, Color> Albedo;
            /// <summary>Strength of the cast shadows (0 = none).</summary>
            public float Shadows = 0.6f;
            /// <summary>Share of the light that comes from the sky (flat shading).</summary>
            public float Ambient = 0.45f;
        }


        /// <summary>Renders a <see cref="Surface"/>: RGB lit colour, A height.</summary>
        public static Texture2D Detail(Surface surface)
        {
            int size = surface.Size;
            var heights = new float[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    heights[y * size + x] = Mathf.Clamp01(surface.Height((x + 0.5f) / size, (y + 0.5f) / size));
                }
            }
            float texel = surface.Meters / size;
            float relief = surface.Relief;
            Vector3 light = GroundLight;
            float flat = light.z;
            Vector2 lightStep = new Vector2(light.x, light.y).normalized;
            float rise = light.z / new Vector2(light.x, light.y).magnitude;
            float H(int x, int y) => heights[Mod(y, size) * size + Mod(x, size)];

            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float h = heights[y * size + x];
                    float dx = (H(x + 1, y) - H(x - 1, y)) * relief / (2f * texel);
                    float dy = (H(x, y + 1) - H(x, y - 1)) * relief / (2f * texel);
                    Vector3 normal = new Vector3(-dx, -dy, 1f).normalized;
                    float direct = Mathf.Max(0f, Vector3.Dot(normal, light)) / flat;
                    if (surface.Shadows > 0f && relief > 0f)
                    {
                        float top = h * relief;
                        float shade = 0f;
                        for (int s = 1; s <= 24; s++)
                        {
                            float step = s * 1.6f;
                            int sx = Mathf.RoundToInt(x + lightStep.x * step);
                            int sy = Mathf.RoundToInt(y + lightStep.y * step);
                            float over = H(sx, sy) * relief - (top + step * texel * rise);
                            if (over > 0f)
                            {
                                shade = Mathf.Max(shade, Mathf.Clamp01(over / (relief * 0.08f + 0.002f)));
                            }
                        }
                        direct *= 1f - shade * surface.Shadows;
                    }
                    float lit = surface.Ambient + (1f - surface.Ambient) * direct;
                    Color albedo = surface.Albedo((x + 0.5f) / size, (y + 0.5f) / size, h);
                    Color color = albedo * lit;
                    color.a = h;
                    pixels[y * size + x] = color;
                }
            }
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, true, false);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }


        /// <summary>A colour between <paramref name="a"/> and <paramref name="b"/> at <paramref name="t"/> (clamped).</summary>
        public static Color Mix(Color a, Color b, float t)
        {
            return Color.Lerp(a, b, Mathf.Clamp01(t));
        }


        /// <summary>A colour from its hex code (RRGGBB).</summary>
        public static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString("#" + hex, out Color color) ? color : Color.magenta;
        }

        // ------------------------------------------------------------------ layer recipes (every function repeats over 0..1)

        private static float Speck(float u, float v, int size, int seed)
        {
            return Hash(Mathf.FloorToInt(u * size), Mathf.FloorToInt(v * size), seed);
        }


        /// <summary>Loose ground: sand with wind ripples, regolith, ash (<paramref name="ripples"/> 0 for none; <paramref name="pits"/> small craters).</summary>
        public static Surface Grainy(Color dark, Color light, float relief, float ripples, float pits, int seed)
        {
            return new Surface
            {
                Meters = 5f,
                Relief = relief,
                Shadows = 0.5f,
                Height = (u, v) =>
                {
                    float warp = PFbm(u, v, 3, 3, 3, seed);
                    float ripple = 0.5f + 0.5f * Mathf.Sin((v * 19f + u * 4f + warp * 3f) * Mathf.PI * 2f);
                    float lumps = PFbm(u, v, 5, 5, 5, seed + 3);
                    float grain = PNoise(u, v, 160, 160, seed + 7);
                    float h = lumps * 0.55f + grain * 0.15f + ripple * ripple * ripples * 0.35f;
                    if (pits > 0f)
                    {
                        h += Pits(u, v, 4, pits * 0.5f, seed + 11) + Pits(u, v, 11, pits, seed + 13) * 0.6f;
                    }
                    return h;
                },
                Albedo = (u, v, h) =>
                {
                    float speck = Speck(u, v, 512, seed + 21);
                    float tone = 0.3f + h * 0.55f + (speck - 0.5f) * 0.22f;
                    Color c = Mix(dark, light, tone);
                    if (speck > 0.985f)
                    {
                        c = Color.Lerp(c, light * 1.15f, 0.6f);
                    }
                    return c;
                }
            };
        }


        /// <summary>Small craters of different sizes at a share of <paramref name="cells"/> x <paramref name="cells"/> random spots (height change).</summary>
        private static float Pits(float u, float v, int cells, float share, int seed)
        {
            float near = PCells(u, v, cells, cells, seed, out float edge, out float id);
            if (id < 1f - share)
            {
                return 0f;
            }
            float r = 0.12f + Hash(Mathf.FloorToInt(id * 9973f), 3, seed) * 0.28f;
            float bowl = 1f - Smooth(0f, r, near);
            float rim = Smooth(r * 0.75f, r, near) * (1f - Smooth(r, r * 1.5f, near));
            return rim * 0.25f - bowl * bowl * 0.5f;
        }


        /// <summary>Cracked plates: dried mud, hardpan, basalt columns, dark mare (<paramref name="cells"/> plates across 5 m).</summary>
        public static Surface Cracked(Color plate, Color plate2, Color crack, int cells, float relief, int seed)
        {
            return new Surface
            {
                Meters = 5f,
                Relief = relief,
                Shadows = 0.5f,
                Height = (u, v) =>
                {
                    PCells(u, v, cells, cells, seed, out float edge, out float id);
                    float gap = Mathf.Lerp(0.35f, 1f, Smooth(0.006f, 0.03f, edge));
                    float surface = 0.55f + id * 0.08f + PFbm(u, v, 8, 8, 4, seed + 5) * 0.3f;
                    float pebble = Smooth(0.72f, 0.8f, PNoise(u, v, 90, 90, seed + 21)) * 0.25f;
                    return surface * gap + PNoise(u, v, 128, 128, seed + 9) * 0.06f + pebble;
                },
                Albedo = (u, v, h) =>
                {
                    PCells(u, v, cells, cells, seed, out float edge, out float id);
                    float gap = Mathf.Lerp(0.25f, 1f, Smooth(0.004f, 0.022f, edge));
                    Color c = Mix(plate2, plate, id * 0.3f + PFbm(u, v, 6, 6, 4, seed + 13) * 0.8f - 0.05f);
                    c *= 0.9f + (Speck(u, v, 512, seed + 17) - 0.5f) * 0.18f;
                    return Color.Lerp(crack, c, gap);
                }
            };
        }


        /// <summary>Rock outcrops over 10 m: sandstone, moon boulders, jagged basalt, rubble (<paramref name="strata"/> adds layered bands).</summary>
        public static Surface Rocky(Color low, Color high, Color crevice, float relief, float strata, int seed)
        {
            return new Surface
            {
                Meters = 10f,
                Relief = relief,
                Shadows = 0.85f,
                Ambient = 0.4f,
                Height = (u, v) =>
                {
                    float ridge = 1f - Mathf.Abs(PFbm(u, v, 3, 3, 6, seed) * 2f - 1f);
                    float lumps = PFbm(u, v, 6, 6, 5, seed + 7);
                    PCells(u, v, 5, 5, seed + 3, out float edge, out float id);
                    float fracture = id > 0.55f ? 1f - Smooth(0f, 0.04f, edge) : 0f;
                    float h = ridge * ridge * 0.55f + lumps * 0.4f - fracture * 0.25f + PNoise(u, v, 200, 200, seed + 5) * 0.05f;
                    if (strata > 0f)
                    {
                        h += (Mathf.Floor(h * 6f) / 6f - h) * strata;
                    }
                    return h;
                },
                Albedo = (u, v, h) =>
                {
                    float band = strata > 0f ? 0.5f + 0.5f * Mathf.Sin(h * 40f) : 0.5f;
                    Color c = Mix(low, high, h * 0.9f + (band - 0.5f) * strata * 0.6f);
                    c *= 0.88f + (Speck(u, v, 1024, seed + 19) - 0.5f) * 0.2f;
                    return Color.Lerp(crevice, c, Smooth(0.08f, 0.3f, h));
                }
            };
        }


        /// <summary>Asphalt (or a dark deck): fine aggregate, a few patches and cracks.</summary>
        public static Surface Asphalt(Color dark, Color light, int seed)
        {
            return new Surface
            {
                Meters = 5f,
                Relief = 0.006f,
                Shadows = 0f,
                Ambient = 0.55f,
                Height = (u, v) => PNoise(u, v, 300, 300, seed) * 0.5f + PFbm(u, v, 6, 6, 3, seed + 3) * 0.5f,
                Albedo = (u, v, h) =>
                {
                    float speck = Speck(u, v, 512, seed + 5);
                    float patch = Smooth(0.58f, 0.62f, PFbm(u, v, 3, 3, 4, seed + 9));
                    Color c = Mix(dark, light, 0.35f + (speck - 0.5f) * 0.5f + h * 0.2f);
                    c *= 1f - patch * 0.12f;
                    PCells(u, v, 5, 5, seed + 13, out float edge, out float id);
                    if (id > 0.8f)
                    {
                        c *= Mathf.Lerp(0.55f, 1f, Smooth(0.004f, 0.012f, edge));
                    }
                    return c;
                }
            };
        }


        /// <summary>Slabs or plates, <paramref name="per5m"/> to 5 m, with seams, a tone per slab and stains; <paramref name="rivets"/> adds bolts at the corners.</summary>
        public static Surface Slabs(Color a, Color b, Color seam, int per5m, float stains, bool rivets, int seed)
        {
            return new Surface
            {
                Meters = 5f,
                Relief = 0.012f,
                Shadows = 0.3f,
                Ambient = 0.5f,
                Height = (u, v) =>
                {
                    float fx = u * per5m - Mathf.Floor(u * per5m);
                    float fy = v * per5m - Mathf.Floor(v * per5m);
                    float border = Mathf.Min(Mathf.Min(fx, 1f - fx), Mathf.Min(fy, 1f - fy)) / per5m * 5f;
                    float h = Smooth(0.01f, 0.03f, border) * 0.8f + PNoise(u, v, 256, 256, seed) * 0.1f;
                    if (rivets)
                    {
                        float cx = Mathf.Min(fx, 1f - fx) / per5m * 5f;
                        float cy = Mathf.Min(fy, 1f - fy) / per5m * 5f;
                        float d = Mathf.Sqrt((cx - 0.08f) * (cx - 0.08f) + (cy - 0.08f) * (cy - 0.08f));
                        h += (1f - Smooth(0.012f, 0.022f, d)) * 0.3f;
                    }
                    return h;
                },
                Albedo = (u, v, h) =>
                {
                    int ix = Mathf.FloorToInt(u * per5m);
                    int iy = Mathf.FloorToInt(v * per5m);
                    float tone = Hash(Mod(ix, per5m), Mod(iy, per5m), seed + 3);
                    Color c = Mix(a, b, tone * 0.7f + PFbm(u, v, 5, 5, 4, seed + 5) * 0.4f);
                    float stain = Smooth(0.55f, 0.75f, PFbm(u, v, 4, 4, 5, seed + 7)) * stains;
                    c *= (1f - stain * 0.25f) * (0.94f + (Speck(u, v, 512, seed + 11) - 0.5f) * 0.1f);
                    return Color.Lerp(seam, c, Smooth(0.25f, 0.6f, h));
                }
            };
        }


        /// <summary>Dense low vegetation seen from above: clumps of leaves with their own shadows.</summary>
        public static Surface Foliage(Color dark, Color light, Color soil, int seed)
        {
            return new Surface
            {
                Meters = 5f,
                Relief = 0.12f,
                Shadows = 0.7f,
                Ambient = 0.4f,
                Height = (u, v) =>
                {
                    float clumps = 1f - PCells(u, v, 14, 14, seed, out float edge, out float id) * 1.1f;
                    float leaves = PFbm(u, v, 24, 24, 3, seed + 3);
                    return Mathf.Clamp01(clumps * 0.6f + leaves * 0.45f + id * 0.1f - 0.1f);
                },
                Albedo = (u, v, h) =>
                {
                    PCells(u, v, 14, 14, seed, out float edge, out float id);
                    Color c = Mix(dark, light, h * 0.8f + id * 0.3f - 0.1f);
                    c *= 0.9f + (Speck(u, v, 512, seed + 9) - 0.5f) * 0.25f;
                    return Color.Lerp(soil, c, Smooth(0.12f, 0.3f, h));
                }
            };
        }


        /// <summary>Farmland: crop rows along the level (2 rows a metre) over tilled soil.</summary>
        public static Surface Rows(Color soil, Color crop, int seed)
        {
            return new Surface
            {
                Meters = 5f,
                Relief = 0.03f,
                Shadows = 0.5f,
                Height = (u, v) =>
                {
                    float row = 0.5f + 0.5f * Mathf.Cos(u * 10f * Mathf.PI * 2f);
                    float plants = PFbm(u, v, 10, 40, 3, seed);
                    return row * row * (0.55f + plants * 0.45f) + PNoise(u, v, 128, 128, seed + 3) * 0.08f;
                },
                Albedo = (u, v, h) =>
                {
                    float row = 0.5f + 0.5f * Mathf.Cos(u * 10f * Mathf.PI * 2f);
                    Color c = Color.Lerp(soil, crop, Smooth(0.2f, 0.7f, row) * (0.55f + PFbm(u, v, 10, 40, 3, seed) * 0.5f));
                    return c * (0.9f + (Speck(u, v, 512, seed + 9) - 0.5f) * 0.2f);
                }
            };
        }


        /// <summary>The pattern liquid is animated with (waves; alpha only).</summary>
        public static Surface Ripples(int seed)
        {
            return new Surface
            {
                Meters = 5f,
                Relief = 0f,
                Shadows = 0f,
                Ambient = 1f,
                Height = (u, v) =>
                {
                    float warp = PFbm(u, v, 3, 3, 3, seed);
                    float waves = 0.5f + 0.5f * Mathf.Sin((u * 3f + v * 7f + warp * 2.5f) * Mathf.PI * 2f);
                    return waves * 0.55f + PFbm(u, v, 6, 6, 4, seed + 3) * 0.45f;
                },
                Albedo = (u, v, h) => new Color(h, h, h, 1f)
            };
        }


        /// <summary>Lava crust: dark plates over glowing cracks (the pattern of the liquid layer; low alpha glows).</summary>
        public static Surface LavaCrust(int seed)
        {
            return new Surface
            {
                Meters = 5f,
                Relief = 0f,
                Shadows = 0f,
                Ambient = 1f,
                Height = (u, v) =>
                {
                    PCells(u, v, 6, 6, seed, out float edge, out float id);
                    float crust = Smooth(0.02f, 0.14f, edge);
                    return Mathf.Clamp01(crust * (0.7f + id * 0.3f) + PFbm(u, v, 8, 8, 3, seed + 5) * 0.2f);
                },
                Albedo = (u, v, h) => new Color(h, h, h, 1f)
            };
        }

        // ------------------------------------------------------------------ tile maps

        /// <summary>Pixels per metre of the per-tile mask and colour maps.</summary>
        public const int TileTexels = 4;


        /// <summary>
        /// The mask map of a tile: every channel is a signed distance (metres, positive inside) mapped to 0.5 + d / (2 x
        /// range). Channels: R paved, G liquid, B rock, A alt ground. <paramref name="field"/> gives the four distances at a
        /// point in tile metres (x in -32..32, y in -10..10).
        /// </summary>
        public static Texture2D TileMasks(Func<Vector2, Vector4> field, Vector4 ranges)
        {
            int width = (int)StrikeRules.TileWidth * TileTexels;
            int height = (int)StrikeRules.TileLength * TileTexels;
            return Make(width, height, (x, y) =>
            {
                Vector4 d = field(TilePoint(x, y, width, height));
                return new Color(Map(d.x, ranges.x), Map(d.y, ranges.y), Map(d.z, ranges.z), Map(d.w, ranges.w));
            }, true);
        }


        /// <summary>The colour map of a tile: RGB half the tint (1 = unchanged), A the concrete share of paving.</summary>
        public static Texture2D TileColors(Func<Vector2, Color> tint)
        {
            int width = (int)StrikeRules.TileWidth * TileTexels;
            int height = (int)StrikeRules.TileLength * TileTexels;
            return Make(width, height, (x, y) =>
            {
                Color c = tint(TilePoint(x, y, width, height));
                return new Color(Mathf.Clamp01(c.r * 0.5f), Mathf.Clamp01(c.g * 0.5f), Mathf.Clamp01(c.b * 0.5f), Mathf.Clamp01(c.a));
            }, true);
        }


        /// <summary>The tile point (metres) at the centre of texel (x, y).</summary>
        public static Vector2 TilePoint(int x, int y, int width, int height)
        {
            return new Vector2(((x + 0.5f) / width - 0.5f) * StrikeRules.TileWidth, ((y + 0.5f) / height - 0.5f) * StrikeRules.TileLength);
        }


        private static float Map(float distance, float range)
        {
            return Mathf.Clamp01(0.5f + distance / (2f * Mathf.Max(0.01f, range)));
        }

        // ------------------------------------------------------------------ decals

        /// <summary>
        /// A crater or scorch decal (alpha blended, white where the material's colour shows): a dark burnt pit, a rim of
        /// thrown-out ground and splashes; <paramref name="rubble"/> adds broken chunks for destroyed buildings.
        /// </summary>
        public static Texture2D CraterDecal(bool rubble, int seed)
        {
            const int size = 256;
            return Make(size, size, (x, y) =>
            {
                float u = (x + 0.5f) / size * 2f - 1f;
                float v = (y + 0.5f) / size * 2f - 1f;
                float angle = Mathf.Atan2(v, u) / (2f * Mathf.PI) + 0.5f;
                float wobble = PNoise(angle, 0.5f, 9, 1, seed) * 0.22f;
                float r = Mathf.Sqrt(u * u + v * v) / (0.72f + wobble);
                float grain = Fbm((x + 0.5f) / size, (y + 0.5f) / size, 8, 4, seed + 5);
                float splash = Mathf.Clamp01((PNoise(angle, Mathf.Min(0.99f, r * 0.3f), 23, 2, seed + 9) - 0.45f) * 3f);
                float pit = 1f - Smooth(0.3f, 0.62f, r);
                float rim = Smooth(0.42f, 0.6f, r) * (1f - Smooth(0.68f, 0.92f, r));
                float spray = (1f - Smooth(0.75f, 1.3f, r)) * splash * (1f - pit);
                float alpha = Mathf.Clamp01(pit * 0.9f + rim * 0.5f + spray * 0.55f) * (0.8f + 0.2f * grain);
                float tone = Mathf.Lerp(0.18f, 0.42f, grain) * (1f - pit * 0.55f) + rim * 0.45f;
                Color color = new Color(tone, tone, tone, alpha);
                if (rubble)
                {
                    PCells(u * 0.5f + 0.5f, v * 0.5f + 0.5f, 7, 7, seed + 17, out float edge, out float id);
                    float chunk = Smooth(0.04f, 0.1f, edge) * (id > 0.4f ? 1f : 0f) * (1f - Smooth(0.5f, 0.85f, r));
                    float stone = 0.55f + 0.4f * id;
                    color = Color.Lerp(color, new Color(stone, stone * 0.97f, stone * 0.92f, 1f), chunk);
                    color.a = Mathf.Max(color.a, chunk);
                }
                return color;
            });
        }


        /// <summary>A soft round blob (white, alpha is the darkness) for the shadows under ground units.</summary>
        public static Texture2D ShadowBlob()
        {
            const int size = 64;
            return Make(size, size, (x, y) =>
            {
                float u = (x + 0.5f) / size * 2f - 1f;
                float v = (y + 0.5f) / size * 2f - 1f;
                float r = Mathf.Sqrt(u * u + v * v);
                return new Color(1f, 1f, 1f, 1f - Smooth(0.35f, 1f, r));
            });
        }
    }
}
