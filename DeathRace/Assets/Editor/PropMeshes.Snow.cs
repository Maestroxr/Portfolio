using Gamebox.Editor;
using UnityEngine;

namespace Portfolio.DeathRace.EditorTools
{
    /// <summary>The pass: snowy pines, rocks, ice and banks, a cabin, frozen trucks; the lodge, a jack-knifed tanker and an ice ridge.</summary>
    internal static partial class PropMeshes
    {
        /// <summary>A pine of four tiers of needles, each with a cap of snow, <paramref name="height"/> metres tall.</summary>
        private static void Pine(LowPolyBuilder m, Vector3 foot, float height, int seed, bool snowy)
        {
            var random = new System.Random(seed);
            float scale = height / 7f;
            m.Push(foot, Quaternion.Euler(0f, Rand(random, 0f, 360f), 0f), Vector3.one * scale);
            m.Color(P("Bark")).Cylinder(Vector3.zero, 0.3f, 0.2f, 1.4f, 6);
            float radius = 2.3f;
            float y = 1f;
            for (int tier = 0; tier < 4; tier++)
            {
                float tierHeight = 2.2f - tier * 0.25f;
                m.Color(P(tier % 2 == 0 ? "Pine" : "PineDark")).Cylinder(new Vector3(0f, y, 0f), radius, 0f, tierHeight, 8, false, true, false, tier * 0.4f);
                if (snowy)
                {
                    float capY = y + tierHeight * 0.42f;
                    m.Color(P(tier == 3 ? "Snow" : "SnowShade")).Cylinder(new Vector3(0f, capY, 0f), radius * 0.82f, 0f, tierHeight * 0.6f, 8, false, true, false, tier * 0.4f + 0.2f);
                    m.Color(P("Snow")).Cylinder(new Vector3(0f, capY + tierHeight * 0.12f, 0f), radius * 0.66f, 0f, tierHeight * 0.5f, 8, false, false, false, tier * 0.4f + 0.2f);
                }
                y += tierHeight * 0.62f;
                radius *= 0.74f;
            }
            m.Pop();
            if (snowy)
            {
                m.Color(P("Snow")).Blob(foot + new Vector3(0f, 0.05f, 0f), new Vector3(2.2f, 0.25f, 2.2f) * scale, 1, 0.2f, seed + 1);
            }
        }

        public static LowPolyBuilder SnowPine(int seed)
        {
            var m = new LowPolyBuilder();
            Pine(m, Vector3.zero, new System.Random(seed).Next(65, 85) / 10f, seed, true);
            return m;
        }

        public static LowPolyBuilder PineGroup(int seed)
        {
            var m = new LowPolyBuilder();
            var random = new System.Random(seed);
            Pine(m, Vector3.zero, 7.5f, seed, true);
            Pine(m, new Vector3(2.8f, 0f, 1.2f), 5f, seed + 1, true);
            Pine(m, new Vector3(-1.4f, 0f, 2.6f), Rand(random, 3.6f, 4.6f), seed + 2, true);
            Pine(m, new Vector3(1.2f, 0f, -2.5f), Rand(random, 3f, 4f), seed + 3, true);
            return m;
        }

        public static LowPolyBuilder SnowRock(int seed)
        {
            var m = new LowPolyBuilder();
            RockPile(m, seed, "RockGrey", "RockGreyDark", 1.6f);
            m.Color(P("Snow")).Blob(new Vector3(0f, 1.25f, 0f), new Vector3(1.25f, 0.32f, 1.05f), 1, 0.2f, seed + 7);
            m.Color(P("SnowShade")).Blob(new Vector3(1.2f, 0.7f, 0.5f), new Vector3(0.7f, 0.2f, 0.6f), 1, 0.2f, seed + 8);
            m.Color(P("Snow")).Blob(new Vector3(0f, 0.05f, 0f), new Vector3(2.4f, 0.3f, 2f), 1, 0.2f, seed + 9);
            return m;
        }

        /// <summary>Broken blocks of blue ice, tilted and heaped.</summary>
        public static LowPolyBuilder IceBlocks(int seed)
        {
            var m = new LowPolyBuilder();
            var random = new System.Random(seed);
            for (int i = 0; i < 6; i++)
            {
                var at = new Vector3(Rand(random, -1.6f, 1.6f), Rand(random, 0.3f, 0.6f), Rand(random, -1.4f, 1.4f));
                if (i == 0)
                {
                    at = new Vector3(0f, 0.9f, 0f);
                }
                m.Push(at, Quaternion.Euler(Rand(random, -25f, 25f), Rand(random, 0f, 90f), Rand(random, -25f, 25f)));
                Vector3 size = i == 0 ? new Vector3(1.6f, 1.8f, 1.3f) : new Vector3(Rand(random, 0.7f, 1.3f), Rand(random, 0.5f, 1f), Rand(random, 0.6f, 1.2f));
                m.Color(P(i % 2 == 0 ? "Ice" : "IceDark")).BeveledBox(Vector3.zero, size, 0.1f);
                m.Color(P("Snow")).Box(new Vector3(0f, size.y * 0.5f, 0f), new Vector3(size.x * 0.9f, 0.08f, size.z * 0.9f));
                m.Pop();
            }
            m.Color(P("SnowShade")).Blob(new Vector3(0f, 0.05f, 0f), new Vector3(2.6f, 0.25f, 2.2f), 1, 0.2f, seed + 9);
            return m;
        }

        /// <summary>A long mound of ploughed snow, 7 m along Z.</summary>
        public static LowPolyBuilder Snowbank(int seed)
        {
            var m = new LowPolyBuilder();
            m.Color(P("Snow")).Blob(new Vector3(0f, 0.3f, 0f), new Vector3(1.4f, 0.9f, 3.5f), 1, 0.15f, seed);
            m.Color(P("SnowShade")).Blob(new Vector3(0.5f, 0.2f, 2.2f), new Vector3(1f, 0.6f, 1.6f), 1, 0.15f, seed + 1);
            m.Color(P("Snow")).Blob(new Vector3(-0.3f, 0.2f, -2.4f), new Vector3(1.1f, 0.7f, 1.6f), 1, 0.15f, seed + 2);
            m.Color(P("SnowBlue")).Blob(new Vector3(0.9f, 0.05f, -0.6f), new Vector3(0.8f, 0.3f, 1.4f), 1, 0.2f, seed + 3);
            return m;
        }

        /// <summary>A log cabin with a snowy roof, a smoking chimney's stack, lit windows and a woodpile.</summary>
        public static LowPolyBuilder Cabin(int seed)
        {
            var m = new LowPolyBuilder();
            const float width = 5.4f;
            const float depth = 4.2f;
            for (int i = 0; i < 9; i++)
            {
                float y = 0.15f + i * 0.3f;
                string log = i % 2 == 0 ? "CabinWood" : "CabinWoodDark";
                m.Color(P(log)).Rod(new Vector3(-width * 0.5f - 0.2f, y, depth * 0.5f), new Vector3(width * 0.5f + 0.2f, y, depth * 0.5f), 0.16f, 6, false);
                m.Color(P(log)).Rod(new Vector3(-width * 0.5f - 0.2f, y, -depth * 0.5f), new Vector3(width * 0.5f + 0.2f, y, -depth * 0.5f), 0.16f, 6, false);
                m.Color(P(log)).Rod(new Vector3(width * 0.5f, y + 0.15f, -depth * 0.5f - 0.2f), new Vector3(width * 0.5f, y + 0.15f, depth * 0.5f + 0.2f), 0.16f, 6, false);
                m.Color(P(log)).Rod(new Vector3(-width * 0.5f, y + 0.15f, -depth * 0.5f - 0.2f), new Vector3(-width * 0.5f, y + 0.15f, depth * 0.5f + 0.2f), 0.16f, 6, false);
            }
            m.Color(P("CabinWoodDark")).Box(new Vector3(0f, 1.4f, 0f), new Vector3(width - 0.2f, 2.8f, depth - 0.2f));
            m.Push(new Vector3(0f, 2.85f, 0f), Quaternion.identity);
            m.Color(P("CabinRoof")).Prism(new[] { new Vector2(-3.3f, 0f), new Vector2(3.3f, 0f), new Vector2(0f, 2.1f) }, depth + 0.9f);
            m.Color(P("Snow")).Prism(new[] { new Vector2(-3.35f, 0.08f), new Vector2(3.35f, 0.08f), new Vector2(0f, 2.22f) }, depth + 0.7f, new Vector3(0f, 0.06f, 0f));
            m.Pop();
            m.Color(P("RockGreyDark")).Box(new Vector3(1.6f, 4f, -0.8f), new Vector3(0.6f, 2f, 0.6f));
            m.Color(P("Snow")).Box(new Vector3(1.6f, 5.02f, -0.8f), new Vector3(0.7f, 0.08f, 0.7f));
            foreach (float x in new[] { -1.4f, 1.4f })
            {
                m.Color(P("WindowWarm")).Box(new Vector3(x, 1.5f, depth * 0.5f + 0.18f), new Vector3(0.9f, 0.8f, 0.06f));
                m.Color(P("CabinWoodDark")).Box(new Vector3(x, 1.5f, depth * 0.5f + 0.21f), new Vector3(0.08f, 0.8f, 0.04f));
            }
            m.Color(P("WindowLit")).Box(new Vector3(width * 0.5f + 0.18f, 1.5f, 0f), new Vector3(0.06f, 0.8f, 0.9f));
            m.Color(P("WoodDark")).Box(new Vector3(0f, 1.05f, depth * 0.5f + 0.17f), new Vector3(0.95f, 2f, 0.06f));
            for (int i = 0; i < 6; i++)
            {
                m.Color(P(i % 2 == 0 ? "Wood" : "WoodLight")).Rod(new Vector3(-3.9f, 0.2f + (i / 3) * 0.36f, -1.5f + (i % 3) * 0.38f), new Vector3(-2.9f, 0.2f + (i / 3) * 0.36f, -1.5f + (i % 3) * 0.38f), 0.17f, 6, false);
            }
            m.Color(P("Snow")).Blob(new Vector3(0f, 0.05f, 0f), new Vector3(4.2f, 0.3f, 3.4f), 1, 0.15f, seed + 3);
            return m;
        }

        /// <summary>A pickup truck buried in snow, with a flatbed.</summary>
        public static LowPolyBuilder FrozenTruck(int seed)
        {
            var m = new LowPolyBuilder();
            m.Push(Vector3.zero, Quaternion.Euler(0f, 0f, -3f));
            Car(m, "CarRed", false, true, seed);
            m.Color(P("CarRed")).Box(new Vector3(0f, 0.95f, -1.6f), new Vector3(1.75f, 0.12f, 1.3f));
            m.Color(P("Snow")).Box(new Vector3(0f, 1.1f, -1.6f), new Vector3(1.6f, 0.22f, 1.2f));
            m.Pop();
            m.Color(P("Ice")).Box(new Vector3(0.95f, 0.5f, 1.3f), new Vector3(0.1f, 0.5f, 0.06f));
            return m;
        }

        /// <summary>A snowman with a scarf, which every snowy track needs.</summary>
        public static LowPolyBuilder Snowman(int seed)
        {
            var m = new LowPolyBuilder();
            m.Color(P("Snow")).Sphere(new Vector3(0f, 0.7f, 0f), new Vector3(0.75f, 0.7f, 0.75f), 5, 10, false);
            m.Color(P("Snow")).Sphere(new Vector3(0f, 1.75f, 0f), new Vector3(0.55f, 0.5f, 0.55f), 5, 10, false);
            m.Color(P("Snow")).Sphere(new Vector3(0f, 2.5f, 0f), new Vector3(0.38f, 0.36f, 0.38f), 5, 10, false);
            m.Color(P("Red")).Torus(new Vector3(0f, 2.15f, 0f), 0.36f, 0.09f, 10, 5, 360f, false);
            m.Color(P("Orange")).Rod(new Vector3(0f, 2.52f, 0.33f), new Vector3(0f, 2.5f, 0.68f), 0.06f, 5, false, 0f);
            m.Color(P("Black")).Cylinder(new Vector3(0f, 2.78f, 0f), 0.3f, 0.3f, 0.06f, 10);
            m.Color(P("Black")).Cylinder(new Vector3(0f, 2.84f, 0f), 0.2f, 0.2f, 0.38f, 10);
            m.Color(P("WoodDark")).Rod(new Vector3(0.5f, 1.85f, 0f), new Vector3(1.2f, 2.3f, 0.1f), 0.04f, 4, false);
            m.Color(P("WoodDark")).Rod(new Vector3(-0.5f, 1.85f, 0f), new Vector3(-1.1f, 2.4f, -0.1f), 0.04f, 4, false);
            return m;
        }

        public static LowPolyBuilder SnowDrums(int seed)
        {
            var m = new LowPolyBuilder();
            Drum(m, Vector3.zero, "DrumBlue", true);
            Drum(m, new Vector3(0.66f, 0f, 0.15f), "DrumRed", true);
            LyingDrum(m, new Vector3(-0.2f, 0.3f, 0.8f), new Vector3(1f, 0f, 0.2f), "DrumBlue");
            m.Color(P("Snow")).Cylinder(new Vector3(0f, 0.88f, 0f), 0.3f, 0.26f, 0.12f, 10);
            m.Color(P("Snow")).Cylinder(new Vector3(0.66f, 0.88f, 0.15f), 0.3f, 0.26f, 0.12f, 10);
            m.Color(P("Snow")).Blob(new Vector3(0.2f, 0.05f, 0.3f), new Vector3(1.4f, 0.35f, 1.3f), 1, 0.2f, seed);
            return m;
        }

        /// <summary>A dead pine: a grey trunk, a few bare branches, snow on them.</summary>
        public static LowPolyBuilder DeadPine(int seed)
        {
            var m = new LowPolyBuilder();
            var random = new System.Random(seed);
            m.Color(P("RockGreyDark")).Cylinder(Vector3.zero, 0.26f, 0.06f, 6.5f, 6);
            for (int i = 0; i < 7; i++)
            {
                float y = 1.6f + i * 0.65f;
                float angle = i * 2.3f;
                float reach = (7f - y) * 0.28f;
                var start = new Vector3(0f, y, 0f);
                var end = start + new Vector3(Mathf.Cos(angle) * reach, -0.15f, Mathf.Sin(angle) * reach);
                m.Color(P("DeadWoodDark")).Rod(start, end, 0.06f, 4, false, 0.02f);
                m.Color(P("Snow")).Rod(start + Vector3.up * 0.06f, end + Vector3.up * 0.05f, 0.05f, 4, false, 0.02f);
            }
            m.Color(P("Snow")).Blob(new Vector3(0f, 0.05f, 0f), new Vector3(1.3f, 0.25f, 1.3f), 1, 0.2f, seed + Mathf.RoundToInt(Rand(random, 0f, 10f)));
            return m;
        }

        public static LowPolyBuilder LogPile(int seed)
        {
            var m = new LowPolyBuilder();
            for (int row = 0; row < 3; row++)
            {
                for (int i = 0; i < 4 - row; i++)
                {
                    float x = -0.75f + i * 0.5f + row * 0.25f;
                    float y = 0.25f + row * 0.43f;
                    m.Color(P(i % 2 == 0 ? "CabinWood" : "Bark")).Rod(new Vector3(x, y, -1.8f), new Vector3(x, y, 1.8f), 0.24f, 7, false);
                    m.Color(P("WoodLight")).Cylinder(new Vector3(x, y, 1.8f), 0.2f, 0.2f, 0.01f, 7);
                }
            }
            m.Color(P("Snow")).Blob(new Vector3(0f, 1.3f, 0f), new Vector3(0.6f, 0.2f, 1.7f), 1, 0.15f, seed);
            m.Color(P("Snow")).Blob(new Vector3(0f, 0.05f, 0f), new Vector3(1.6f, 0.25f, 2.4f), 1, 0.15f, seed + 1);
            return m;
        }

        /// <summary>A yellow warning sign on a post (a car sliding), with snow on top.</summary>
        public static LowPolyBuilder WarningSign(int seed)
        {
            var m = new LowPolyBuilder();
            m.Color(P("Steel")).Box(new Vector3(0f, 1.2f, 0f), new Vector3(0.1f, 2.4f, 0.1f));
            m.Push(new Vector3(0f, 2.5f, -0.08f), Quaternion.Euler(0f, 0f, 45f));
            m.Color(P("Black")).Box(Vector3.zero, new Vector3(0.95f, 0.95f, 0.05f));
            m.Color(P("HazardYellow")).Box(new Vector3(0f, 0f, -0.02f), new Vector3(0.85f, 0.85f, 0.05f));
            m.Pop();
            m.Color(P("Black")).Box(new Vector3(0f, 2.5f, -0.13f), new Vector3(0.36f, 0.16f, 0.02f));
            m.Color(P("Snow")).Box(new Vector3(0f, 3.17f, -0.08f), new Vector3(0.18f, 0.08f, 0.1f));
            m.Color(P("Snow")).Blob(new Vector3(0f, 0.05f, 0f), new Vector3(0.6f, 0.2f, 0.6f), 1, 0.2f, seed);
            return m;
        }

        // ------------------------------------------------------------------------------------------------
        // Landmarks
        // ------------------------------------------------------------------------------------------------

        /// <summary>A two storey ski lodge under a steep snowy roof, its windows lit, a deck and pines beside it.</summary>
        public static LowPolyBuilder Lodge(int seed)
        {
            var m = new LowPolyBuilder();
            WindowedBlock(m, Vector3.zero, new Vector3(13f, 6.4f, 9f), "CabinWood", seed, new[] { "WindowWarm", "WindowLit" }, 0.75f);
            m.Color(P("RockGreyDark")).Box(new Vector3(0f, 0.5f, 0f), new Vector3(13.4f, 1f, 9.4f));
            m.Push(new Vector3(0f, 6.4f, 0f), Quaternion.Euler(0f, 90f, 0f));
            m.Color(P("CabinRoof")).Prism(new[] { new Vector2(-5.6f, 0f), new Vector2(5.6f, 0f), new Vector2(0f, 4.6f) }, 14.2f);
            m.Color(P("Snow")).Prism(new[] { new Vector2(-5.65f, 0.1f), new Vector2(5.65f, 0.1f), new Vector2(0f, 4.75f) }, 13.8f, new Vector3(0f, 0.08f, 0f));
            m.Pop();
            m.Color(P("WindowWarm")).Prism(new[] { new Vector2(-2.6f, 0f), new Vector2(2.6f, 0f), new Vector2(0f, 2.2f) }, 0.1f, new Vector3(0f, 6.5f, 4.56f));
            m.Color(P("RockGrey")).Box(new Vector3(-4f, 9.5f, 2f), new Vector3(1f, 3f, 1f));
            m.Color(P("Snow")).Box(new Vector3(-4f, 11.02f, 2f), new Vector3(1.1f, 0.1f, 1.1f));
            m.Color(P("Wood")).Box(new Vector3(0f, 0.7f, 6.2f), new Vector3(10f, 0.2f, 3.2f));
            for (int i = 0; i < 6; i++)
            {
                m.Color(P("WoodDark")).Box(new Vector3(-4.8f + i * 1.92f, 1.2f, 7.7f), new Vector3(0.12f, 1f, 0.12f));
            }
            m.Color(P("WoodDark")).Box(new Vector3(0f, 1.7f, 7.7f), new Vector3(10f, 0.1f, 0.12f));
            m.Color(P("Snow")).Box(new Vector3(0f, 0.84f, 6.6f), new Vector3(9.6f, 0.08f, 2f));
            Pine(m, new Vector3(-9.5f, 0f, -2f), 8f, seed + 1, true);
            Pine(m, new Vector3(9f, 0f, 3f), 6.5f, seed + 2, true);
            Pine(m, new Vector3(8.5f, 0f, -4f), 5f, seed + 3, true);
            m.Color(P("Snow")).Blob(new Vector3(0f, 0.05f, 0f), new Vector3(10f, 0.4f, 8f), 1, 0.15f, seed + 4);
            return m;
        }

        /// <summary>A semi with a tanker trailer, jack-knifed and frozen, 17 m long.</summary>
        public static LowPolyBuilder FrozenTanker(int seed)
        {
            var m = new LowPolyBuilder();
            // The trailer along Z.
            m.Color(P("TankWhite")).Rod(new Vector3(0f, 1.9f, -6f), new Vector3(0f, 1.9f, 5f), 1.25f, 12, false);
            m.Color(P("TankStripe")).Rod(new Vector3(0f, 1.9f, -1.2f), new Vector3(0f, 1.9f, -0.6f), 1.28f, 12, false);
            m.Color(P("Snow")).Box(new Vector3(0f, 3.15f, -0.5f), new Vector3(1.4f, 0.24f, 10.4f));
            m.Color(P("Ice")).Box(new Vector3(1.24f, 1.2f, 1.5f), new Vector3(0.08f, 1f, 3f));
            m.Color(P("SteelDark")).Box(new Vector3(0f, 0.7f, -4.5f), new Vector3(2.2f, 0.4f, 2.6f));
            foreach (float z in new[] { -4f, -5.2f })
            {
                StandingTyre(m, new Vector3(1.05f, 0.5f, z), Vector3.right, 0.5f, 0.35f);
                StandingTyre(m, new Vector3(-1.05f, 0.5f, z), Vector3.right, 0.5f, 0.35f);
            }
            // The cab, turned 40 degrees at the front.
            m.Push(new Vector3(0.6f, 0f, 6.6f), Quaternion.Euler(0f, 40f, 0f));
            m.Color(P("CarBlue")).BeveledBox(new Vector3(0f, 1.9f, 0.6f), new Vector3(2.4f, 2.2f, 2.4f), 0.15f);
            m.Color(P("CarBlue")).BeveledBox(new Vector3(0f, 1.2f, 2.4f), new Vector3(2.3f, 1.2f, 1.6f), 0.15f);
            m.Color(P("Glass")).Box(new Vector3(0f, 2.3f, 1.82f), new Vector3(2.1f, 0.8f, 0.06f));
            m.Color(P("Chrome")).Box(new Vector3(0f, 1.1f, 3.22f), new Vector3(1.6f, 0.8f, 0.06f));
            m.Color(P("HeadLight")).Box(new Vector3(0.85f, 0.95f, 3.24f), new Vector3(0.3f, 0.2f, 0.04f));
            m.Color(P("HeadLight")).Box(new Vector3(-0.85f, 0.95f, 3.24f), new Vector3(0.3f, 0.2f, 0.04f));
            m.Color(P("Chrome")).Cylinder(new Vector3(1.05f, 1f, -0.5f), 0.1f, 0.1f, 2.8f, 6);
            m.Color(P("Snow")).Blob(new Vector3(0f, 3.05f, 0.6f), new Vector3(1.2f, 0.25f, 1.2f), 1, 0.15f, seed);
            m.Color(P("Snow")).Box(new Vector3(0f, 1.83f, 2.4f), new Vector3(2.1f, 0.12f, 1.4f));
            foreach (float z in new[] { 2.4f, -0.2f })
            {
                StandingTyre(m, new Vector3(1.15f, 0.55f, z), Vector3.right, 0.55f, 0.36f);
                StandingTyre(m, new Vector3(-1.15f, 0.55f, z), Vector3.right, 0.55f, 0.36f);
            }
            m.Pop();
            m.Color(P("Snow")).Blob(new Vector3(1.5f, 0.1f, 0f), new Vector3(3.2f, 0.6f, 7f), 1, 0.15f, seed + 1);
            m.Color(P("SnowShade")).Blob(new Vector3(-1.8f, 0.1f, -2f), new Vector3(1.6f, 0.5f, 3f), 1, 0.15f, seed + 2);
            return m;
        }

        /// <summary>A ridge of rock and ice 20 m long: jagged blue blades on grey rock, snow on the top.</summary>
        public static LowPolyBuilder IceRidge(int seed)
        {
            var m = new LowPolyBuilder();
            var random = new System.Random(seed);
            for (int i = 0; i < 6; i++)
            {
                float z = -8f + i * 3.2f;
                m.Color(P(i % 2 == 0 ? "RockGrey" : "RockGreyDark")).Blob(new Vector3(Rand(random, -0.8f, 0.8f), 1.4f, z), new Vector3(3f, 2.2f, 2.4f), 1, 0.25f, seed + i);
            }
            for (int i = 0; i < 9; i++)
            {
                float z = -8.5f + i * 2.1f + Rand(random, -0.5f, 0.5f);
                float height = Rand(random, 3f, 6f);
                m.Push(new Vector3(Rand(random, -1.2f, 1.2f), 0f, z), Quaternion.Euler(Rand(random, -12f, 12f), Rand(random, 0f, 180f), Rand(random, -12f, 12f)));
                m.Color(P(i % 3 == 0 ? "IceDark" : "Ice")).Cylinder(Vector3.zero, Rand(random, 0.9f, 1.4f), 0.1f, height, 5);
                m.Pop();
            }
            for (int i = 0; i < 5; i++)
            {
                m.Color(P("Snow")).Blob(new Vector3(Rand(random, -0.6f, 0.6f), 3f, -7f + i * 3.5f), new Vector3(2f, 0.5f, 1.8f), 1, 0.2f, seed + 20 + i);
            }
            m.Color(P("Snow")).Blob(new Vector3(0f, 0.1f, 0f), new Vector3(5f, 0.6f, 11f), 1, 0.15f, seed + 30);
            return m;
        }
    }
}
