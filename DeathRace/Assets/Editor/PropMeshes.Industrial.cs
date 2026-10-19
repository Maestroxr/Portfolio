using Gamebox.Editor;
using UnityEngine;

namespace Portfolio.DeathRace.EditorTools
{
    /// <summary>The yard: containers, crates, pipes, tanks, drums and scrap; a gantry crane, a smelter's chimney and a tank farm.</summary>
    internal static partial class PropMeshes
    {
        private static readonly string[] ContainerPaints = { "ContainerRed", "ContainerBlue", "ContainerGreen", "ContainerOrange", "ContainerGrey" };

        /// <summary>A 20 ft shipping container (6.1 m along Z) standing at <paramref name="foot"/>, ribbed, doors at +Z.</summary>
        private static void ShippingContainer(LowPolyBuilder m, Vector3 foot, string paint)
        {
            string dark = paint + "Dark";
            const float length = 6.1f;
            const float width = 2.44f;
            const float height = 2.6f;
            m.Color(P(paint)).Box(foot + Vector3.up * height * 0.5f, new Vector3(width, height, length));
            for (int i = 0; i < 11; i++)
            {
                float z = -length * 0.5f + 0.45f + i * 0.52f;
                m.Color(P(dark)).Box(foot + new Vector3(0f, height * 0.5f, z), new Vector3(width + 0.08f, height - 0.2f, 0.12f));
            }
            m.Color(P(dark)).Box(foot + new Vector3(0f, height + 0.01f, 0f), new Vector3(width - 0.2f, 0.04f, length - 0.3f));
            m.Color(P(dark)).Box(foot + new Vector3(0f, height * 0.5f, length * 0.5f + 0.02f), new Vector3(width - 0.1f, height - 0.1f, 0.06f));
            foreach (float x in new[] { -0.55f, -0.2f, 0.2f, 0.55f })
            {
                m.Color(P("SteelDark")).Box(foot + new Vector3(x, height * 0.5f, length * 0.5f + 0.07f), new Vector3(0.05f, height - 0.3f, 0.05f));
            }
            foreach (Vector3 corner in new[] { new Vector3(1f, 0f, 1f), new Vector3(-1f, 0f, 1f), new Vector3(1f, 0f, -1f), new Vector3(-1f, 0f, -1f) })
            {
                Vector3 at = foot + new Vector3(corner.x * (width * 0.5f - 0.1f), 0f, corner.z * (length * 0.5f - 0.1f));
                m.Color(P("SteelDark")).Box(at + Vector3.up * 0.1f, new Vector3(0.24f, 0.2f, 0.24f));
                m.Color(P("SteelDark")).Box(at + Vector3.up * (height - 0.1f), new Vector3(0.24f, 0.2f, 0.24f));
            }
        }

        private static string ContainerPaint(System.Random random)
        {
            return ContainerPaints[random.Next(ContainerPaints.Length)];
        }

        /// <summary>Three containers: two side by side and one on top, turned a little.</summary>
        public static LowPolyBuilder ContainerStack(int seed)
        {
            var m = new LowPolyBuilder();
            var random = new System.Random(seed);
            ShippingContainer(m, new Vector3(-1.3f, 0f, 0f), ContainerPaint(random));
            ShippingContainer(m, new Vector3(1.3f, 0f, 0.3f), ContainerPaint(random));
            m.Push(new Vector3(0.2f, 2.6f, 0.1f), Quaternion.Euler(0f, Rand(random, -8f, 8f), 0f));
            ShippingContainer(m, Vector3.zero, ContainerPaint(random));
            m.Pop();
            return m;
        }

        public static LowPolyBuilder Container(int seed)
        {
            var m = new LowPolyBuilder();
            var random = new System.Random(seed);
            ShippingContainer(m, Vector3.zero, ContainerPaint(random));
            return m;
        }

        private static void Crate(LowPolyBuilder m, Vector3 foot, float size, bool dark)
        {
            m.Color(P(dark ? "Wood" : "WoodLight")).Box(foot + Vector3.up * size * 0.5f, Vector3.one * size);
            string frame = dark ? "WoodDark" : "Wood";
            float half = size * 0.5f;
            foreach (Vector3 edge in new[] { new Vector3(1f, 0f, 1f), new Vector3(-1f, 0f, 1f), new Vector3(1f, 0f, -1f), new Vector3(-1f, 0f, -1f) })
            {
                m.Color(P(frame)).Box(foot + new Vector3(edge.x * half, half, edge.z * half), new Vector3(0.12f, size + 0.02f, 0.12f));
            }
            m.Color(P(frame)).Box(foot + new Vector3(0f, size, 0f), new Vector3(size + 0.04f, 0.08f, 0.14f));
            m.Color(P(frame)).Box(foot + new Vector3(0f, size, 0f), new Vector3(0.14f, 0.08f, size + 0.04f));
        }

        public static LowPolyBuilder Crates(int seed)
        {
            var m = new LowPolyBuilder();
            var random = new System.Random(seed);
            Crate(m, new Vector3(0f, 0f, 0f), 1.3f, false);
            Crate(m, new Vector3(1.4f, 0f, 0.2f), 1.2f, true);
            Crate(m, new Vector3(0.6f, 1.3f, 0.1f), 1.1f, random.NextDouble() < 0.5);
            Crate(m, new Vector3(-0.2f, 0f, 1.5f), 1f, true);
            m.Push(new Vector3(-1.4f, 0f, -0.4f), Quaternion.Euler(0f, 25f, 0f));
            Crate(m, Vector3.zero, 0.9f, false);
            m.Pop();
            return m;
        }

        /// <summary>A stack of pallets with a few sacks on top.</summary>
        public static LowPolyBuilder Pallets(int seed)
        {
            var m = new LowPolyBuilder();
            var random = new System.Random(seed);
            for (int i = 0; i < 4; i++)
            {
                float y = i * 0.15f;
                float turn = Rand(random, -6f, 6f);
                m.Push(new Vector3(0f, y, 0f), Quaternion.Euler(0f, turn, 0f));
                for (int plank = 0; plank < 5; plank++)
                {
                    m.Color(P(i % 2 == 0 ? "WoodLight" : "Wood")).Box(new Vector3(-0.48f + plank * 0.24f, 0.13f, 0f), new Vector3(0.18f, 0.03f, 1.2f));
                }
                foreach (float z in new[] { -0.5f, 0f, 0.5f })
                {
                    m.Color(P("WoodDark")).Box(new Vector3(0f, 0.06f, z), new Vector3(1.1f, 0.1f, 0.14f));
                }
                m.Pop();
            }
            for (int i = 0; i < 3; i++)
            {
                m.Color(P(i == 1 ? "Cream" : "Sand")).Blob(new Vector3(-0.25f + i * 0.28f, 0.82f, Rand(random, -0.3f, 0.3f)), new Vector3(0.3f, 0.16f, 0.45f), 1, 0.12f, seed + i);
            }
            return m;
        }

        /// <summary>Three pipes on H-frame supports, 9 m along Z, with flanges.</summary>
        public static LowPolyBuilder Pipes(int seed)
        {
            var m = new LowPolyBuilder();
            string[] paints = { "Steel", "Rust", "Yellow" };
            float[] radii = { 0.38f, 0.3f, 0.22f };
            for (int s = 0; s < 4; s++)
            {
                float z = -4.5f + s * 3f;
                m.Color(P("SteelDark")).Box(new Vector3(-1.4f, 0.9f, z), new Vector3(0.18f, 1.8f, 0.18f));
                m.Color(P("SteelDark")).Box(new Vector3(1.4f, 0.9f, z), new Vector3(0.18f, 1.8f, 0.18f));
                m.Color(P("SteelDark")).Box(new Vector3(0f, 1.2f, z), new Vector3(3f, 0.16f, 0.2f));
            }
            for (int p = 0; p < 3; p++)
            {
                float x = -0.85f + p * 0.85f;
                float y = 1.28f + radii[p];
                m.Color(P(paints[p])).Rod(new Vector3(x, y, -5f), new Vector3(x, y, 5f), radii[p], 8, false);
                for (int f = 0; f < 3; f++)
                {
                    float z = -3f + f * 3f;
                    m.Color(P("SteelDark")).Rod(new Vector3(x, y, z - 0.06f), new Vector3(x, y, z + 0.06f), radii[p] * 1.25f, 8, false);
                }
            }
            m.Color(P("Steel")).Rod(new Vector3(0.85f, 1.5f, 5f), new Vector3(0.85f, 0f, 5.8f), 0.22f, 8, false);
            return m;
        }

        /// <summary>A white storage tank, 4 m across, with a domed roof, a red band and a ladder.</summary>
        public static LowPolyBuilder Tank(int seed)
        {
            var m = new LowPolyBuilder();
            m.Color(P("Concrete")).Cylinder(Vector3.zero, 2.3f, 2.3f, 0.3f, 14);
            m.Color(P("TankWhite")).Cylinder(new Vector3(0f, 0.3f, 0f), 2f, 2f, 4.6f, 14);
            m.Color(P("TankStripe")).Cylinder(new Vector3(0f, 3.2f, 0f), 2.02f, 2.02f, 0.6f, 14);
            m.Color(P("LightGrey")).Sphere(new Vector3(0f, 4.9f, 0f), new Vector3(2f, 0.7f, 2f), 4, 14, false, 0.5f);
            m.Color(P("SteelDark")).Box(new Vector3(0f, 2.8f, -2.1f), new Vector3(0.5f, 5f, 0.08f));
            for (int i = 0; i < 4; i++)
            {
                m.Color(P("Rust")).Box(new Vector3(-1.2f + i * 0.8f, 0.9f, -1.85f + Mathf.Abs(i - 1.5f) * 0.2f), new Vector3(0.12f, 1.2f, 0.06f));
            }
            m.Color(P("Steel")).Rod(new Vector3(1.6f, 0.8f, 1.4f), new Vector3(2.8f, 0.8f, 2.2f), 0.2f, 6, false);
            return m;
        }

        /// <summary>Hazard drums, one fallen over and leaking.</summary>
        public static LowPolyBuilder HazardDrums(int seed)
        {
            var m = new LowPolyBuilder();
            Drum(m, new Vector3(0f, 0f, 0f), "HazardYellow");
            Drum(m, new Vector3(0.66f, 0f, -0.1f), "HazardYellow", true);
            Drum(m, new Vector3(0.3f, 0f, 0.6f), "Blue");
            LyingDrum(m, new Vector3(-0.75f, 0.3f, 0.3f), new Vector3(1f, 0f, 0.3f), "HazardYellow");
            m.Color(P("Black")).Blob(new Vector3(-1.5f, 0.01f, 0.5f), new Vector3(1f, 0.03f, 0.7f), 1, 0.3f, seed);
            m.Color(P("HazardBlack")).Cylinder(new Vector3(0f, 0.4f, 0f), 0.305f, 0.305f, 0.14f, 10);
            return m;
        }

        /// <summary>An 8 m stretch of chain link fence along Z: posts, rails and barbed wire on a low curb.</summary>
        public static LowPolyBuilder ChainFence(int seed)
        {
            var m = new LowPolyBuilder();
            m.Color(P("ConcreteDark")).Box(new Vector3(0f, 0.1f, 0f), new Vector3(0.3f, 0.2f, 8f));
            for (int i = 0; i <= 4; i++)
            {
                float z = -4f + i * 2f;
                m.Color(P("Fence")).Cylinder(new Vector3(0f, 0.2f, z), 0.05f, 0.05f, 2.2f, 5);
                m.Color(P("Fence")).Rod(new Vector3(0f, 2.3f, z), new Vector3(-0.35f, 2.65f, z), 0.03f, 4, false);
            }
            m.Color(P("Fence")).Box(new Vector3(0f, 2.3f, 0f), new Vector3(0.06f, 0.06f, 8f));
            m.Color(P("Fence")).Box(new Vector3(0f, 1.2f, 0f), new Vector3(0.04f, 0.04f, 8f));
            // The mesh, as a faint grey panel with diagonal wires.
            m.Color(P("SteelDark")).Box(new Vector3(0f, 1.25f, 0f), new Vector3(0.015f, 2.1f, 8f));
            for (int i = 0; i < 16; i++)
            {
                float z = -4f + i * 0.5f;
                m.Color(P("Fence")).Rod(new Vector3(0.02f, 0.2f, z), new Vector3(0.02f, 2.3f, z + 0.5f), 0.012f, 3, false);
            }
            foreach (float y in new[] { 2.5f, 2.62f })
            {
                m.Color(P("SteelDark")).Box(new Vector3(-0.2f - (y - 2.5f) * 1.2f, y, 0f), new Vector3(0.02f, 0.02f, 8f));
            }
            return m;
        }

        /// <summary>A yellow forklift with a crate on its forks.</summary>
        public static LowPolyBuilder Forklift(int seed)
        {
            var m = new LowPolyBuilder();
            m.Color(P("CraneYellow")).BeveledBox(new Vector3(0f, 0.7f, -0.2f), new Vector3(1.2f, 0.8f, 2.1f), 0.08f);
            m.Color(P("Charcoal")).BeveledBox(new Vector3(0f, 0.75f, -1.3f), new Vector3(1.15f, 0.7f, 0.4f), 0.06f);
            m.Color(P("Black")).Box(new Vector3(0f, 1.18f, -0.4f), new Vector3(0.5f, 0.12f, 0.5f));
            foreach (Vector3 post in new[] { new Vector3(0.52f, 0f, -0.9f), new Vector3(-0.52f, 0f, -0.9f), new Vector3(0.52f, 0f, 0.35f), new Vector3(-0.52f, 0f, 0.35f) })
            {
                m.Color(P("Charcoal")).Box(post + Vector3.up * 1.6f, new Vector3(0.07f, 1.0f, 0.07f));
            }
            m.Color(P("CraneYellowDark")).Box(new Vector3(0f, 2.12f, -0.28f), new Vector3(1.2f, 0.07f, 1.4f));
            m.Color(P("SteelDark")).Box(new Vector3(0.35f, 1.4f, 0.95f), new Vector3(0.1f, 2.6f, 0.1f));
            m.Color(P("SteelDark")).Box(new Vector3(-0.35f, 1.4f, 0.95f), new Vector3(0.1f, 2.6f, 0.1f));
            m.Color(P("Steel")).Box(new Vector3(0.28f, 0.25f, 1.6f), new Vector3(0.12f, 0.06f, 1.2f));
            m.Color(P("Steel")).Box(new Vector3(-0.28f, 0.25f, 1.6f), new Vector3(0.12f, 0.06f, 1.2f));
            Crate(m, new Vector3(0f, 0.29f, 1.6f), 1f, true);
            foreach (Vector3 wheel in new[] { new Vector3(0.62f, 0.32f, 0.5f), new Vector3(-0.62f, 0.32f, 0.5f), new Vector3(0.62f, 0.28f, -0.9f), new Vector3(-0.62f, 0.28f, -0.9f) })
            {
                StandingTyre(m, wheel, Vector3.right, wheel.y, 0.22f);
            }
            m.Color(P("Beacon")).Box(new Vector3(0f, 2.22f, -0.6f), new Vector3(0.14f, 0.12f, 0.14f));
            return m;
        }

        /// <summary>A heap of scrap metal: rusted blobs, plates, a tyre and a car door.</summary>
        public static LowPolyBuilder ScrapPile(int seed)
        {
            var m = new LowPolyBuilder();
            var random = new System.Random(seed);
            m.Color(P("RustDark")).Blob(new Vector3(0f, 0.6f, 0f), new Vector3(2.2f, 0.9f, 1.8f), 1, 0.35f, seed);
            m.Color(P("Rust")).Blob(new Vector3(0.8f, 0.9f, 0.3f), new Vector3(1.1f, 0.7f, 1f), 1, 0.35f, seed + 1);
            for (int i = 0; i < 7; i++)
            {
                var at = new Vector3(Rand(random, -1.6f, 1.6f), Rand(random, 0.6f, 1.4f), Rand(random, -1.3f, 1.3f));
                m.Push(at, Quaternion.Euler(Rand(random, -40f, 40f), Rand(random, 0f, 180f), Rand(random, -40f, 40f)));
                string paint = i % 3 == 0 ? "SteelDark" : i % 3 == 1 ? "RustLight" : ContainerPaints[random.Next(ContainerPaints.Length)];
                m.Color(P(paint)).Box(Vector3.zero, new Vector3(Rand(random, 0.6f, 1.4f), 0.06f, Rand(random, 0.4f, 1f)));
                m.Pop();
            }
            StandingTyre(m, new Vector3(-1.7f, 0.42f, 0.8f), new Vector3(1f, 0.3f, 0.2f).normalized, 0.38f, 0.24f);
            m.Push(new Vector3(1.2f, 1.1f, -0.9f), Quaternion.Euler(10f, 30f, 60f));
            m.Color(P("CarBlue")).Box(Vector3.zero, new Vector3(1f, 1.1f, 0.08f));
            m.Color(P("Glass")).Box(new Vector3(0f, 0.3f, 0.02f), new Vector3(0.8f, 0.4f, 0.06f));
            m.Pop();
            return m;
        }

        public static LowPolyBuilder Dumpster(int seed)
        {
            var m = new LowPolyBuilder();
            m.Color(P("Green")).BeveledBox(new Vector3(0f, 0.75f, 0f), new Vector3(1.9f, 1.25f, 1.2f), 0.05f);
            m.Color(P("GreenDark")).Box(new Vector3(0f, 1.42f, -0.05f), new Vector3(1.95f, 0.08f, 1.3f));
            m.Push(new Vector3(0.48f, 1.47f, 0.6f), Quaternion.Euler(-35f, 0f, 0f));
            m.Color(P("Charcoal")).Box(new Vector3(0f, 0f, -0.32f), new Vector3(0.95f, 0.05f, 0.66f));
            m.Pop();
            m.Color(P("Black")).Box(new Vector3(-0.48f, 1.43f, 0.02f), new Vector3(0.9f, 0.05f, 1.15f));
            foreach (float x in new[] { -0.8f, 0.8f })
            {
                m.Color(P("SteelDark")).Box(new Vector3(x, 0.08f, 0f), new Vector3(0.12f, 0.16f, 1.1f));
            }
            m.Color(P("Rust")).Box(new Vector3(0.4f, 0.4f, -0.61f), new Vector3(0.6f, 0.4f, 0.02f));
            return m;
        }

        /// <summary>A grey electrical transformer with cooling fins and a hazard band.</summary>
        public static LowPolyBuilder Transformer(int seed)
        {
            var m = new LowPolyBuilder();
            m.Color(P("ConcreteDark")).Box(new Vector3(0f, 0.15f, 0f), new Vector3(2.6f, 0.3f, 2.2f));
            m.Color(P("ContainerGrey")).Box(new Vector3(0f, 1.2f, 0f), new Vector3(1.8f, 1.8f, 1.4f));
            for (int i = 0; i < 6; i++)
            {
                m.Color(P("ContainerGreyDark")).Box(new Vector3(-0.75f + i * 0.3f, 1.1f, 0.8f), new Vector3(0.06f, 1.4f, 0.24f));
                m.Color(P("ContainerGreyDark")).Box(new Vector3(-0.75f + i * 0.3f, 1.1f, -0.8f), new Vector3(0.06f, 1.4f, 0.24f));
            }
            m.Color(P("HazardYellow")).Box(new Vector3(0f, 1.9f, 0f), new Vector3(1.84f, 0.2f, 1.44f));
            foreach (float x in new[] { -0.5f, 0f, 0.5f })
            {
                m.Color(P("White")).Cylinder(new Vector3(x, 2.1f, 0f), 0.1f, 0.06f, 0.5f, 6);
            }
            return m;
        }

        // ------------------------------------------------------------------------------------------------
        // Landmarks
        // ------------------------------------------------------------------------------------------------

        /// <summary>A gantry crane 13 m tall spanning 16 m (legs at x = +-8), a trolley and a container hanging from it.</summary>
        public static LowPolyBuilder GantryCrane(int seed)
        {
            var m = new LowPolyBuilder();
            var random = new System.Random(seed);
            foreach (float x in new[] { -8f, 8f })
            {
                m.Color(P("CraneYellowDark")).Box(new Vector3(x, 0.4f, 0f), new Vector3(1.2f, 0.8f, 7f));
                m.Color(P("CraneYellow")).Rod(new Vector3(x, 0.8f, -3f), new Vector3(x, 12.4f, -0.6f), 0.4f, 6, false);
                m.Color(P("CraneYellow")).Rod(new Vector3(x, 0.8f, 3f), new Vector3(x, 12.4f, 0.6f), 0.4f, 6, false);
                m.Color(P("CraneYellowDark")).Box(new Vector3(x, 6f, 0f), new Vector3(0.3f, 0.3f, 4.2f));
                foreach (float z in new[] { -2.8f, 2.8f })
                {
                    StandingTyre(m, new Vector3(x + 0.7f, 0.4f, z), Vector3.right, 0.4f, 0.3f);
                }
            }
            m.Color(P("CraneYellow")).Box(new Vector3(0f, 12.9f, -0.6f), new Vector3(18f, 1f, 0.6f));
            m.Color(P("CraneYellow")).Box(new Vector3(0f, 12.9f, 0.6f), new Vector3(18f, 1f, 0.6f));
            for (int i = 0; i < 8; i++)
            {
                m.Color(P("CraneYellowDark")).Box(new Vector3(-7f + i * 2f, 13.45f, 0f), new Vector3(0.2f, 0.12f, 1.8f));
            }
            m.Color(P("Charcoal")).Box(new Vector3(2.5f, 13.7f, 0f), new Vector3(2.4f, 0.8f, 2f));
            m.Color(P("WindowCool")).Box(new Vector3(2.5f, 13.7f, -1.02f), new Vector3(1.6f, 0.4f, 0.04f));
            m.Color(P("SteelDark")).Box(new Vector3(2.5f, 9.6f, 0.5f), new Vector3(0.06f, 6.2f, 0.06f));
            m.Color(P("SteelDark")).Box(new Vector3(2.5f, 9.6f, -0.5f), new Vector3(0.06f, 6.2f, 0.06f));
            m.Color(P("SteelDark")).Box(new Vector3(2.5f, 6.4f, 0f), new Vector3(2.6f, 0.2f, 0.3f));
            m.Push(new Vector3(2.5f, 3.6f, 0f), Quaternion.Euler(0f, 90f + Rand(random, -10f, 10f), 0f));
            ShippingContainer(m, Vector3.zero, ContainerPaint(random));
            m.Pop();
            m.Color(P("Beacon")).Box(new Vector3(-8.6f, 13.5f, 0f), new Vector3(0.3f, 0.3f, 0.3f));
            m.Color(P("Beacon")).Box(new Vector3(8.6f, 13.5f, 0f), new Vector3(0.3f, 0.3f, 0.3f));
            return m;
        }

        /// <summary>A brick smelter hall with a saw tooth roof and lit windows, and its banded chimney 26 m tall.</summary>
        public static LowPolyBuilder Smokestack(int seed)
        {
            var m = new LowPolyBuilder();
            WindowedBlock(m, new Vector3(-5f, 0f, 0f), new Vector3(14f, 7f, 10f), "Brick", seed, new[] { "WindowWarm", "WindowLit" }, 0.5f);
            for (int i = 0; i < 4; i++)
            {
                float x = -10.5f + i * 3.5f;
                m.Push(new Vector3(x, 7f, 0f), Quaternion.identity);
                m.Color(P("RoofDark")).Prism(new[] { new Vector2(-1.75f, 0f), new Vector2(1.75f, 0f), new Vector2(1.75f, 1.6f) }, 10f);
                m.Color(P("WindowWarm")).Box(new Vector3(1.72f, 0.8f, 0f), new Vector3(0.06f, 1.2f, 9.4f));
                m.Pop();
            }
            m.Color(P("BrickDark")).Box(new Vector3(4.5f, 0.8f, 0f), new Vector3(5f, 1.6f, 5f));
            m.Color(P("BrickDark")).Cylinder(new Vector3(4.5f, 1.6f, 0f), 1.8f, 1.25f, 24.4f, 12);
            string[] bands = { "TankStripe", "White", "TankStripe", "White" };
            for (int i = 0; i < bands.Length; i++)
            {
                float y = 18f + i * 2f;
                float t = (y - 1.6f) / 24.4f;
                float radius = Mathf.Lerp(1.8f, 1.25f, t) + 0.03f;
                m.Color(P(bands[i])).Cylinder(new Vector3(4.5f, y, 0f), radius, radius - 0.08f, 2f, 12);
            }
            m.Color(P("Soot")).Cylinder(new Vector3(4.5f, 26f, 0f), 1.32f, 1.32f, 0.3f, 12, false, true, false);
            m.Color(P("Black")).Cylinder(new Vector3(4.5f, 26.2f, 0f), 1.0f, 1.0f, 0.12f, 12);
            m.Color(P("Beacon")).Box(new Vector3(5.9f, 25.4f, 0f), new Vector3(0.3f, 0.3f, 0.3f));
            m.Color(P("Beacon")).Box(new Vector3(3.1f, 25.4f, 0f), new Vector3(0.3f, 0.3f, 0.3f));
            m.Color(P("SteelDark")).Box(new Vector3(4.5f, 12f, -1.6f), new Vector3(0.4f, 20f, 0.06f));
            return m;
        }

        /// <summary>Three big tanks on a concrete pad, linked by pipes and a catwalk.</summary>
        public static LowPolyBuilder TankFarm(int seed)
        {
            var m = new LowPolyBuilder();
            m.Color(P("Concrete")).Box(new Vector3(0f, 0.1f, 0f), new Vector3(24f, 0.2f, 12f));
            var centres = new[] { new Vector3(-7.5f, 0.2f, 0f), new Vector3(0f, 0.2f, 0.5f), new Vector3(7.5f, 0.2f, -0.3f) };
            for (int i = 0; i < centres.Length; i++)
            {
                Vector3 c = centres[i];
                float height = i == 1 ? 8f : 6.5f;
                m.Color(P(i == 1 ? "LightGrey" : "TankWhite")).Cylinder(c, 3.6f, 3.6f, height, 16);
                m.Color(P("TankStripe")).Cylinder(c + Vector3.up * (height - 1.6f), 3.62f, 3.62f, 0.6f, 16);
                m.Color(P("Grey")).Sphere(c + Vector3.up * height, new Vector3(3.6f, 0.9f, 3.6f), 4, 16, false, 0.5f);
                m.Color(P("Rust")).Cylinder(c, 3.63f, 3.63f, 0.7f, 16);
                m.Color(P("SteelDark")).Box(c + new Vector3(0f, height * 0.5f, -3.7f), new Vector3(0.5f, height, 0.08f));
            }
            m.Color(P("SteelDark")).Box(new Vector3(0f, 7.4f, 0f), new Vector3(16f, 0.12f, 0.9f));
            m.Color(P("Steel")).Rod(new Vector3(-7.5f, 1.2f, 3.8f), new Vector3(7.5f, 1.2f, 3.8f), 0.3f, 8, false);
            m.Color(P("Yellow")).Rod(new Vector3(-7.5f, 1.8f, 4.3f), new Vector3(7.5f, 1.8f, 4.3f), 0.2f, 8, false);
            m.Color(P("Beacon")).Box(new Vector3(0f, 9.1f, 0.5f), new Vector3(0.3f, 0.3f, 0.3f));
            return m;
        }

        /// <summary>A floodlight mast: a steel pole 9 m tall with two lamps leaning out along +Z.</summary>
        public static LowPolyBuilder FloodMast(int seed, out Vector3 head)
        {
            var m = new LowPolyBuilder();
            m.Color(P("ConcreteDark")).Box(new Vector3(0f, 0.2f, 0f), new Vector3(0.9f, 0.4f, 0.9f));
            m.Color(P("LampPole")).Cylinder(new Vector3(0f, 0.4f, 0f), 0.2f, 0.13f, 8.6f, 8);
            m.Color(P("LampPole")).Box(new Vector3(0f, 8.9f, 0.4f), new Vector3(1.8f, 0.12f, 0.12f));
            foreach (float x in new[] { -0.65f, 0.65f })
            {
                m.Push(new Vector3(x, 8.85f, 0.8f), Quaternion.Euler(35f, 0f, 0f));
                m.Color(P("LampPole")).Box(Vector3.zero, new Vector3(0.6f, 0.45f, 0.25f));
                m.Color(P("LampHead")).Box(new Vector3(0f, -0.05f, 0.13f), new Vector3(0.5f, 0.35f, 0.04f));
                m.Pop();
            }
            head = new Vector3(0f, 8.7f, 1f);
            return m;
        }
    }
}
