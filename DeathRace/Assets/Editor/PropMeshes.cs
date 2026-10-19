using System.Collections.Generic;
using Gamebox.Editor;
using UnityEngine;

namespace Portfolio.DeathRace.EditorTools
{
    /// <summary>
    /// The palette of the scenery: every colour a prop is painted with, by name, with how shiny and metallic it is and
    /// how it glows by day and at night (windows, lamps, neon, fire). The meshes point their UVs at the texel of a name
    /// (<see cref="UV"/>), so one material per theme colours every prop, and the night look swaps the palette's glow and
    /// wetness, not the meshes. Names keep their texel because they are always added in the same order.
    /// </summary>
    internal static class PropPalette
    {
        public const int Size = 16;

        /// <summary>The strongest glow; the emission texture holds glow / MaxGlow and the material multiplies it back.</summary>
        public const float MaxGlow = 6f;

        private sealed class Entry
        {
            public string Name;
            public Color Day;
            public Color Night;
            public float Smooth;
            public float Metal;
            public float DayGlow;
            public float NightGlow;
        }

        private static readonly List<Entry> Entries = new List<Entry>();
        private static PaletteTexture layout;

        private static PaletteTexture Layout
        {
            get
            {
                if (layout == null)
                {
                    Define();
                    layout = new PaletteTexture(Size);
                    foreach (Entry entry in Entries)
                    {
                        layout.Add(entry.Name, entry.Day);
                    }
                }
                return layout;
            }
        }

        public static Vector2 UV(string name)
        {
            return Layout.UV(name);
        }

        public static bool Has(string name)
        {
            return Layout.Has(name);
        }

        /// <summary>The colours (sRGB).</summary>
        public static Texture2D Albedo(bool night)
        {
            return Build(entry => night ? entry.Night : entry.Day, Color.magenta, false);
        }

        /// <summary>The glow (sRGB encoded, scaled down by <see cref="MaxGlow"/> in linear light).</summary>
        public static Texture2D Emission(bool night)
        {
            return Build(entry =>
            {
                float glow = night ? entry.NightGlow : entry.DayGlow;
                if (glow <= 0f)
                {
                    return Color.black;
                }
                Color linear = (night ? entry.Night : entry.Day).linear * (glow / MaxGlow);
                return new Color(Mathf.Clamp01(linear.r), Mathf.Clamp01(linear.g), Mathf.Clamp01(linear.b), 1f).gamma;
            }, Color.black, false);
        }

        /// <summary>URP's metallic map: R = metallic, A = smoothness (a little more at night: the rain).</summary>
        public static Texture2D MetalGloss(bool night)
        {
            return Build(entry =>
            {
                float smooth = entry.Smooth + (night && entry.DayGlow <= 0f && entry.NightGlow <= 0f ? 0.12f : 0f);
                return new Color(entry.Metal, entry.Metal, entry.Metal, Mathf.Clamp01(smooth));
            }, new Color(0f, 0f, 0f, 0.2f), true);
        }

        private static Texture2D Build(System.Func<Entry, Color> colorOf, Color empty, bool linear)
        {
            int count = Layout.Count;
            var pixels = new Color[Size * Size];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = empty;
            }
            for (int i = 0; i < count; i++)
            {
                pixels[i] = colorOf(Entries[i]);
            }
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false, linear)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private static void C(string name, uint day, float smooth = 0.2f, float metal = 0f, float dayGlow = 0f, float nightGlow = 0f, uint night = 0xFFFFFFFF)
        {
            Color dayColor = DeathRaceArtSurfaces.Hex(day);
            Entries.Add(new Entry
            {
                Name = name,
                Day = dayColor,
                Night = night == 0xFFFFFFFF ? dayColor : DeathRaceArtSurfaces.Hex(night),
                Smooth = smooth,
                Metal = metal,
                DayGlow = dayGlow,
                NightGlow = nightGlow
            });
        }

        /// <summary>Every colour, in the fixed order that gives each its texel. Append only: inserting moves the others.</summary>
        private static void Define()
        {
            Entries.Clear();
            // Neutrals and building materials.
            C("Black", 0x1A1A1C, 0.3f);
            C("Charcoal", 0x2E2D2C, 0.25f);
            C("DarkGrey", 0x4A4947, 0.25f);
            C("Grey", 0x7A7874, 0.25f);
            C("LightGrey", 0xA8A6A1, 0.25f);
            C("White", 0xE8E6E0, 0.3f);
            C("Concrete", 0x9C9890, 0.15f);
            C("ConcreteDark", 0x6E6A64, 0.15f);
            C("ConcreteLight", 0xBDB9B0, 0.15f);
            C("Asphalt", 0x3A3A3C, 0.25f);
            // Metals.
            C("Steel", 0x8A9096, 0.55f, 0.6f);
            C("SteelDark", 0x4E5458, 0.5f, 0.6f);
            C("Chrome", 0xC8CDD2, 0.85f, 0.9f);
            C("Rust", 0x8A4A26, 0.2f, 0.15f);
            C("RustDark", 0x5A2E18, 0.15f, 0.1f);
            C("RustLight", 0xB0663A, 0.25f, 0.15f);
            C("Soot", 0x1C1918, 0.08f);
            C("Ash", 0x5A5450, 0.08f);
            // Rubber and wood.
            C("Tyre", 0x1E1E1F, 0.25f);
            C("Wood", 0x8A6240, 0.15f);
            C("WoodDark", 0x5A3E28, 0.12f);
            C("WoodLight", 0xB08A5E, 0.15f);
            // Glass and lights.
            C("Glass", 0x2C3A48, 0.9f, 0.1f);
            C("WindowLit", 0x3A4A5A, 0.9f, 0.1f, 0f, 1.15f, 0xFFA838);
            C("WindowWarm", 0x34414E, 0.9f, 0.1f, 0f, 1f, 0xFF8030);
            C("WindowCool", 0x3E5060, 0.9f, 0.1f, 0f, 1f, 0x58B4FF);
            C("WindowDark", 0x26303A, 0.9f, 0.1f);
            C("LampHead", 0xDCDCD0, 0.8f, 0f, 0f, 6f, 0xFFD08A);
            C("LampPole", 0x3A3D40, 0.5f, 0.5f);
            C("NeonPink", 0xE07AB8, 0.6f, 0f, 0f, 3.5f, 0xFF2FB8);
            C("NeonCyan", 0x7ACDE0, 0.6f, 0f, 0f, 3.5f, 0x1FD8FF);
            C("NeonYellow", 0xE0D07A, 0.6f, 0f, 0f, 3f, 0xFFD21F);
            C("NeonGreen", 0x9AD87A, 0.6f, 0f, 0f, 3f, 0x4CFF3A);
            C("NeonRed", 0xD86A6A, 0.6f, 0f, 0f, 3f, 0xFF2A2A);
            C("Fire", 0xE8641E, 0.2f, 0f, 0.7f, 3.5f, 0xFF7A1A);
            C("Ember", 0xB4401A, 0.2f, 0f, 0.4f, 2.5f, 0xD8501A);
            C("TailLight", 0xA01818, 0.7f, 0f, 0f, 2.5f, 0xFF2A2A);
            C("HeadLight", 0xE8E8DC, 0.8f, 0f, 0f, 2f, 0xFFF2D0);
            C("Beacon", 0xC8301E, 0.6f, 0f, 0.6f, 4f, 0xFF3A1E);
            // Paints.
            C("Red", 0xC62E24, 0.35f);
            C("RedDark", 0x8E1E18, 0.3f);
            C("Orange", 0xE8742A, 0.35f);
            C("OrangeDark", 0xA84E18, 0.3f);
            C("Yellow", 0xE8B62A, 0.35f);
            C("YellowDark", 0xA8801A, 0.3f);
            C("HazardYellow", 0xE8B21C, 0.35f);
            C("HazardBlack", 0x1C1B1A, 0.3f);
            C("Blue", 0x2E5EA8, 0.35f);
            C("BlueDark", 0x1E3E70, 0.3f);
            C("Green", 0x3E8A46, 0.35f);
            C("GreenDark", 0x26582C, 0.3f);
            C("Teal", 0x2A8A86, 0.35f);
            C("Purple", 0x6A3E8E, 0.35f);
            C("Cream", 0xE6D8B8, 0.3f);
            C("Pink", 0xD86A9A, 0.35f);
            // Desert.
            C("Sand", 0xC9A272, 0.08f);
            C("SandDark", 0x9C7350, 0.08f);
            C("SandLight", 0xE2C79E, 0.08f);
            C("Rock", 0xA0704E, 0.12f);
            C("RockDark", 0x744C34, 0.12f);
            C("RockLight", 0xC49272, 0.12f);
            C("RockRed", 0xB05A3A, 0.12f);
            C("RockRedDark", 0x7E3A24, 0.12f);
            C("RockBand", 0xD49A6A, 0.12f);
            C("Cactus", 0x5E8A3E, 0.25f);
            C("CactusDark", 0x3E6428, 0.2f);
            C("CactusLight", 0x86AE5A, 0.25f);
            C("DeadWood", 0x8A7458, 0.1f);
            C("DeadWoodDark", 0x5E4C38, 0.1f);
            C("Bone", 0xE8DEC8, 0.3f);
            C("Scrub", 0x8A8048, 0.1f);
            C("ScrubDark", 0x5E5630, 0.1f);
            C("BusYellow", 0xE8A81E, 0.3f);
            C("BusYellowDark", 0xA8761A, 0.25f);
            C("DrumRed", 0xB83A24, 0.4f, 0.3f);
            C("DrumBlue", 0x2E4E8A, 0.4f, 0.3f);
            // Industrial.
            C("ContainerRed", 0xA8382A, 0.3f, 0.3f);
            C("ContainerRedDark", 0x742418, 0.25f, 0.3f);
            C("ContainerBlue", 0x2E5E8E, 0.3f, 0.3f);
            C("ContainerBlueDark", 0x1E3E60, 0.25f, 0.3f);
            C("ContainerGreen", 0x3E7A4A, 0.3f, 0.3f);
            C("ContainerGreenDark", 0x285430, 0.25f, 0.3f);
            C("ContainerOrange", 0xD06A26, 0.3f, 0.3f);
            C("ContainerOrangeDark", 0x904818, 0.25f, 0.3f);
            C("ContainerGrey", 0x8A8E92, 0.3f, 0.3f);
            C("ContainerGreyDark", 0x5E6266, 0.25f, 0.3f);
            C("Brick", 0x8E4A36, 0.12f);
            C("BrickDark", 0x6A3426, 0.12f);
            C("TankWhite", 0xD8D6D0, 0.4f, 0.2f);
            C("TankStripe", 0xC24A2A, 0.35f, 0.2f);
            C("CraneYellow", 0xE2A21A, 0.4f, 0.2f);
            C("CraneYellowDark", 0x9A6A12, 0.35f, 0.2f);
            C("Fence", 0x9AA0A4, 0.5f, 0.6f);
            // Snow.
            C("Snow", 0xEEF3FA, 0.35f);
            C("SnowShade", 0xC9D6E8, 0.35f);
            C("SnowBlue", 0xA8BCD8, 0.4f);
            C("Ice", 0x9FC0D8, 0.9f);
            C("IceDark", 0x6E94B3, 0.85f);
            C("Pine", 0x2E5A3E, 0.15f);
            C("PineDark", 0x1E3E2A, 0.12f);
            C("PineLight", 0x467A52, 0.15f);
            C("Bark", 0x5A4232, 0.1f);
            C("CabinWood", 0x7A5032, 0.15f);
            C("CabinWoodDark", 0x54361E, 0.12f);
            C("CabinRoof", 0x4A2E24, 0.2f);
            C("RockGrey", 0x6E6E70, 0.15f);
            C("RockGreyDark", 0x4A4A4E, 0.15f);
            // City.
            C("Building", 0x8E8A84, 0.2f);
            C("BuildingDark", 0x5E5C5A, 0.2f);
            C("BuildingLight", 0xB8B4AC, 0.2f);
            C("BuildingBrick", 0x9A5A44, 0.15f);
            C("BuildingBlue", 0x4A5E78, 0.6f, 0.3f);
            C("BuildingTeal", 0x3E6A6E, 0.5f, 0.2f);
            C("Roof", 0x4A4846, 0.2f);
            C("RoofDark", 0x2E2D2C, 0.2f);
            C("ConeOrange", 0xF06A1A, 0.35f);
            C("CarRed", 0xB8281E, 0.7f, 0.4f);
            C("CarBlue", 0x2A4E9A, 0.7f, 0.4f);
            C("CarWhite", 0xDCDCD8, 0.7f, 0.3f);
            C("CarBlack", 0x1E1F22, 0.75f, 0.4f);
            C("CarYellow", 0xE0B020, 0.7f, 0.4f);
            C("CarGreen", 0x2E6A3E, 0.7f, 0.4f);
            C("CarSilver", 0xA8ACB0, 0.75f, 0.6f);
            C("WaterTower", 0x8A6A4A, 0.15f);
            C("WaterTowerDark", 0x5A4430, 0.12f);
            C("Grass", 0x5E7A3A, 0.15f);
            C("GrassDark", 0x3E5428, 0.12f);
            C("Leaf", 0x4E7A3A, 0.15f);
            C("LeafDark", 0x34562A, 0.12f);
        }
    }


    /// <summary>
    /// Recipes of the scenery, written with BaseGame's <see cref="LowPolyBuilder"/> in the colours of
    /// <see cref="PropPalette"/>. Every model stands on y = 0 with its pivot at the middle of its base and is sized in
    /// metres; they are seen from 40 m above, so they are made of big, readable shapes that cast clear shadows, a few
    /// hundred triangles each. This file holds the parts several biomes share (tyres, drums, cars); the biomes are in
    /// PropMeshes.Desert/Industrial/Snow/City.cs.
    /// </summary>
    internal static partial class PropMeshes
    {
        private static Vector2 P(string name)
        {
            return PropPalette.UV(name);
        }

        private static float Rand(System.Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }

        /// <summary>A tyre lying flat (axis up) at <paramref name="center"/> (its middle).</summary>
        private static void Tyre(LowPolyBuilder m, Vector3 center, float radius = 0.36f, float width = 0.24f, string paint = null)
        {
            m.Color(P("Tyre")).Torus(center, radius - width * 0.36f, width * 0.5f, 10, 6, 360f, false);
            if (paint != null)
            {
                m.Color(P(paint)).Torus(center + Vector3.up * width * 0.38f, radius - width * 0.36f, width * 0.16f, 10, 4, 360f, false);
            }
        }

        /// <summary>A tyre standing on its tread, its axis along <paramref name="axis"/> (horizontal).</summary>
        private static void StandingTyre(LowPolyBuilder m, Vector3 center, Vector3 axis, float radius = 0.36f, float width = 0.24f)
        {
            m.Push(center, Quaternion.FromToRotation(Vector3.up, axis));
            m.Color(P("Tyre")).Torus(Vector3.zero, radius - width * 0.36f, width * 0.5f, 10, 6, 360f, false);
            m.Color(P("SteelDark")).Cylinder(new Vector3(0f, -width * 0.2f, 0f), radius * 0.45f, radius * 0.45f, width * 0.4f, 8);
            m.Pop();
        }

        /// <summary>An oil drum standing at <paramref name="foot"/>.</summary>
        private static void Drum(LowPolyBuilder m, Vector3 foot, string paint, bool rusty = false)
        {
            m.Color(P(paint)).Cylinder(foot, 0.3f, 0.3f, 0.88f, 10);
            m.Color(P(rusty ? "RustDark" : "SteelDark")).Cylinder(foot + Vector3.up * 0.28f, 0.31f, 0.31f, 0.05f, 10);
            m.Color(P(rusty ? "RustDark" : "SteelDark")).Cylinder(foot + Vector3.up * 0.58f, 0.31f, 0.31f, 0.05f, 10);
            m.Color(P(rusty ? "Rust" : "Steel")).Cylinder(foot + Vector3.up * 0.88f, 0.29f, 0.29f, 0.02f, 10);
            if (rusty)
            {
                m.Color(P("Rust")).Cylinder(foot, 0.305f, 0.305f, 0.22f, 10);
            }
        }

        /// <summary>A drum lying on its side along <paramref name="axis"/>, its middle at <paramref name="center"/>.</summary>
        private static void LyingDrum(LowPolyBuilder m, Vector3 center, Vector3 axis, string paint)
        {
            m.Push(center - axis.normalized * 0.44f, Quaternion.FromToRotation(Vector3.up, axis));
            Drum(m, Vector3.zero, paint, true);
            m.Pop();
        }

        /// <summary>
        /// A small car, 4.2 m long along +Z: body, cabin with glass, wheels; <paramref name="burnt"/> makes it a charred,
        /// rusted hulk on its rims with the glass gone; <paramref name="snowy"/> heaps snow on it.
        /// </summary>
        private static void Car(LowPolyBuilder m, string paint, bool burnt, bool snowy, int seed)
        {
            var random = new System.Random(seed);
            string body = burnt ? "Soot" : paint;
            string trim = burnt ? "RustDark" : "Charcoal";
            float lift = burnt ? 0.18f : 0.32f;
            m.Color(P(body)).BeveledBox(new Vector3(0f, lift + 0.36f, 0f), new Vector3(1.8f, 0.62f, 4.2f), 0.12f);
            m.Color(P(body)).BeveledBox(new Vector3(0f, lift + 0.86f, -0.25f), new Vector3(1.55f, 0.55f, 2.1f), 0.18f);
            if (burnt)
            {
                m.Color(P("Rust")).BeveledBox(new Vector3(0.3f, lift + 0.68f, 1.3f), new Vector3(1.1f, 0.06f, 1.2f), 0.02f);
                m.Color(P("Black")).Box(new Vector3(0f, lift + 0.9f, -0.25f), new Vector3(1.4f, 0.4f, 1.95f));
                m.Color(P("RustLight")).BeveledBox(new Vector3(-0.5f, lift + 0.68f, -1.6f), new Vector3(0.6f, 0.04f, 0.7f), 0.02f);
            }
            else
            {
                m.Color(P("Glass")).Box(new Vector3(0f, lift + 0.88f, 0.84f), new Vector3(1.42f, 0.42f, 0.06f));
                m.Color(P("Glass")).Box(new Vector3(0f, lift + 0.88f, -1.32f), new Vector3(1.42f, 0.4f, 0.06f));
                m.Color(P("Glass")).Box(new Vector3(0f, lift + 0.9f, -0.25f), new Vector3(1.58f, 0.34f, 1.7f));
                m.Color(P("HeadLight")).Box(new Vector3(0.6f, lift + 0.42f, 2.1f), new Vector3(0.36f, 0.14f, 0.04f));
                m.Color(P("HeadLight")).Box(new Vector3(-0.6f, lift + 0.42f, 2.1f), new Vector3(0.36f, 0.14f, 0.04f));
                m.Color(P("TailLight")).Box(new Vector3(0.62f, lift + 0.46f, -2.1f), new Vector3(0.32f, 0.12f, 0.04f));
                m.Color(P("TailLight")).Box(new Vector3(-0.62f, lift + 0.46f, -2.1f), new Vector3(0.32f, 0.12f, 0.04f));
            }
            m.Color(P(trim)).Box(new Vector3(0f, lift + 0.12f, 2.08f), new Vector3(1.7f, 0.18f, 0.1f));
            m.Color(P(trim)).Box(new Vector3(0f, lift + 0.12f, -2.08f), new Vector3(1.7f, 0.18f, 0.1f));
            foreach (Vector3 wheel in new[] { new Vector3(0.82f, 0.34f, 1.35f), new Vector3(-0.82f, 0.34f, 1.35f), new Vector3(0.82f, 0.34f, -1.35f), new Vector3(-0.82f, 0.34f, -1.35f) })
            {
                if (burnt)
                {
                    m.Push(new Vector3(wheel.x, 0.26f, wheel.z), Quaternion.Euler(0f, 0f, 90f));
                    m.Color(P("RustDark")).Cylinder(new Vector3(0f, -0.1f, 0f), 0.26f, 0.26f, 0.2f, 8);
                    m.Pop();
                }
                else
                {
                    StandingTyre(m, wheel, Vector3.right, 0.34f, 0.24f);
                }
            }
            if (snowy)
            {
                m.Color(P("Snow")).Blob(new Vector3(0f, lift + 1.12f, -0.2f), new Vector3(0.85f, 0.22f, 1.15f), 1, 0.15f, seed + 1);
                m.Color(P("Snow")).Blob(new Vector3(0f, lift + 0.72f, 1.5f), new Vector3(0.95f, 0.16f, 0.7f), 1, 0.15f, seed + 2);
                m.Color(P("Snow")).Blob(new Vector3(0f, lift + 0.72f, -1.75f), new Vector3(0.95f, 0.14f, 0.45f), 1, 0.15f, seed + 3);
                m.Color(P("SnowShade")).Blob(new Vector3(Rand(random, -0.9f, 0.9f), 0.2f, Rand(random, -2f, 2f)), new Vector3(0.9f, 0.4f, 1.2f), 1, 0.2f, seed + 4);
            }
        }

        /// <summary>A billboard on two posts, 6 m wide, its face toward -Z; the panel's art is bands of paint, lit by two lamps at night.</summary>
        private static void Billboard(LowPolyBuilder m, string[] art, int seed, bool rusty)
        {
            var random = new System.Random(seed);
            string post = rusty ? "Rust" : "SteelDark";
            m.Color(P(post)).Box(new Vector3(-2f, 2f, 0.2f), new Vector3(0.22f, 4f, 0.22f));
            m.Color(P(post)).Box(new Vector3(2f, 2f, 0.2f), new Vector3(0.22f, 4f, 0.22f));
            m.Color(P(post)).Box(new Vector3(0f, 2.6f, 0.2f), new Vector3(4.2f, 0.12f, 0.12f));
            m.Color(P(rusty ? "RustDark" : "Charcoal")).Box(new Vector3(0f, 5.2f, 0.12f), new Vector3(6.2f, 3.2f, 0.16f));
            m.Color(P(art[0])).Box(new Vector3(0f, 5.2f, 0f), new Vector3(5.9f, 2.9f, 0.1f));
            m.Color(P(art[1])).Box(new Vector3(-0.8f, 5.6f, -0.02f), new Vector3(3.8f, 0.8f, 0.1f));
            m.Color(P(art[2])).Box(new Vector3(1.6f, 4.4f, -0.02f), new Vector3(2.2f, 0.9f, 0.1f));
            m.Color(P(art[3])).Box(new Vector3(-1.9f, 4.5f, -0.03f), new Vector3(1.6f, 1.4f, 0.1f));
            if (rusty)
            {
                m.Color(P("RustDark")).Box(new Vector3(Rand(random, -2f, 2f), Rand(random, 4.2f, 6.2f), -0.04f), new Vector3(1.1f, 0.6f, 0.1f));
            }
            // A catwalk with two lamps on top.
            m.Color(P(post)).Box(new Vector3(0f, 3.55f, -0.35f), new Vector3(6f, 0.08f, 0.7f));
            foreach (float x in new[] { -1.6f, 1.6f })
            {
                m.Color(P(post)).Rod(new Vector3(x, 6.8f, 0.1f), new Vector3(x, 7.2f, -0.6f), 0.05f, 4, false);
                m.Color(P("LampHead")).Box(new Vector3(x, 7.15f, -0.7f), new Vector3(0.5f, 0.14f, 0.3f));
            }
        }

        /// <summary>
        /// A block of a building (<paramref name="size"/>, standing on y = 0) with rows of windows on its four faces (a
        /// window every ~2.2 m and a floor every 3 m), some lit at night: lit ones are picked by the seed.
        /// </summary>
        private static void WindowedBlock(LowPolyBuilder m, Vector3 center, Vector3 size, string wall, int seed, string[] windows, float litChance = 0.55f)
        {
            var random = new System.Random(seed);
            m.Color(P(wall)).Box(center + Vector3.up * size.y * 0.5f, size);
            int floors = Mathf.Max(1, Mathf.FloorToInt((size.y - 1f) / 3f));
            for (int face = 0; face < 4; face++)
            {
                bool alongX = face < 2;
                float sign = face % 2 == 0 ? 1f : -1f;
                float width = alongX ? size.x : size.z;
                float depth = alongX ? size.z : size.x;
                int columns = Mathf.Max(1, Mathf.FloorToInt((width - 0.8f) / 2.2f));
                float step = width / columns;
                for (int floor = 0; floor < floors; floor++)
                {
                    float y = 1.6f + floor * 3f;
                    if (y > size.y - 0.8f)
                    {
                        break;
                    }
                    for (int col = 0; col < columns; col++)
                    {
                        float along = -width * 0.5f + step * (col + 0.5f);
                        string glass = random.NextDouble() < litChance ? windows[random.Next(windows.Length)] : "WindowDark";
                        Vector3 offset = alongX ? new Vector3(along, y, sign * (depth * 0.5f + 0.02f)) : new Vector3(sign * (depth * 0.5f + 0.02f), y, along);
                        Vector3 extent = alongX ? new Vector3(step * 0.55f, 1.5f, 0.06f) : new Vector3(0.06f, 1.5f, step * 0.55f);
                        m.Color(P(glass)).Box(center + offset, extent);
                    }
                }
            }
        }

        /// <summary>A cluster of rocks of <paramref name="size"/> metres.</summary>
        private static void RockPile(LowPolyBuilder m, int seed, string light, string dark, float size)
        {
            var random = new System.Random(seed);
            m.Color(P(light)).Blob(new Vector3(0f, 0.45f * size, 0f), new Vector3(1f, 0.7f, 0.85f) * size, 1, 0.28f, seed);
            m.Color(P(dark)).Blob(new Vector3(0.75f * size, 0.25f * size, 0.35f * size), new Vector3(0.55f, 0.45f, 0.5f) * size, 1, 0.3f, seed + 1);
            m.Color(P(light)).Blob(new Vector3(Rand(random, -0.9f, -0.5f) * size, 0.2f * size, Rand(random, -0.6f, 0.6f) * size), new Vector3(0.45f, 0.35f, 0.4f) * size, 0, 0.3f, seed + 2);
        }
    }
}
