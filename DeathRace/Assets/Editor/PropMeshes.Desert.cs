using System.Collections.Generic;
using Gamebox.Editor;
using UnityEngine;

namespace Portfolio.DeathRace.EditorTools
{
    /// <summary>The desert: rocks and spires, cacti, dead trees, the junk of past races, and the mesa, the bus and the gas station.</summary>
    internal static partial class PropMeshes
    {
        public static LowPolyBuilder Boulders(int seed)
        {
            var m = new LowPolyBuilder();
            RockPile(m, seed, "Rock", "RockDark", 1.7f);
            m.Color(P("RockLight")).Blob(new Vector3(-1.6f, 0.3f, 1.2f), new Vector3(0.6f, 0.45f, 0.55f), 0, 0.3f, seed + 5);
            return m;
        }

        /// <summary>A pillar of stacked sandstone, banded, with an overhanging cap: ~8 m tall.</summary>
        public static LowPolyBuilder RockSpire(int seed)
        {
            var m = new LowPolyBuilder();
            var random = new System.Random(seed);
            m.Color(P("RockDark")).Blob(new Vector3(0f, 0.9f, 0f), new Vector3(2.6f, 1.3f, 2.3f), 1, 0.22f, seed);
            m.Color(P("Rock")).Blob(new Vector3(0.1f, 2.8f, 0f), new Vector3(1.9f, 1.7f, 1.7f), 1, 0.2f, seed + 1);
            m.Color(P("RockBand")).Blob(new Vector3(-0.1f, 4.6f, 0.1f), new Vector3(1.5f, 1.2f, 1.4f), 1, 0.18f, seed + 2);
            m.Color(P("RockRed")).Blob(new Vector3(0f, 6.1f, 0f), new Vector3(1.15f, 1.1f, 1.1f), 1, 0.18f, seed + 3);
            m.Color(P("RockLight")).Blob(new Vector3(0.15f, 7.3f, 0.05f), new Vector3(1.55f, 0.5f, 1.4f), 1, 0.2f, seed + 4);
            for (int i = 0; i < 4; i++)
            {
                float angle = i * Mathf.PI * 0.5f + Rand(random, -0.4f, 0.4f);
                var at = new Vector3(Mathf.Cos(angle) * 2.9f, 0.3f, Mathf.Sin(angle) * 2.6f);
                m.Color(P(i % 2 == 0 ? "Rock" : "RockDark")).Blob(at, new Vector3(0.6f, 0.45f, 0.55f) * Rand(random, 0.7f, 1.2f), 0, 0.3f, seed + 10 + i);
            }
            return m;
        }

        public static LowPolyBuilder Saguaro(int seed)
        {
            var m = new LowPolyBuilder();
            var random = new System.Random(seed);
            float height = Rand(random, 4.2f, 5.4f);
            m.Color(P("Cactus")).Cylinder(Vector3.zero, 0.42f, 0.36f, height, 8);
            m.Color(P("Cactus")).Sphere(new Vector3(0f, height, 0f), new Vector3(0.36f, 0.34f, 0.36f), 3, 8, false, 0.5f);
            m.Color(P("CactusDark")).Cylinder(Vector3.zero, 0.45f, 0.43f, 0.5f, 8);
            int arms = 2 + random.Next(2);
            for (int i = 0; i < arms; i++)
            {
                float angle = i * Mathf.PI * 2f / arms + Rand(random, -0.3f, 0.3f);
                var side = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                float armY = Rand(random, 1.5f, 2.6f);
                float reach = Rand(random, 0.9f, 1.15f);
                float rise = Rand(random, 1.1f, 1.8f);
                m.Color(P("CactusDark")).Rod(new Vector3(0f, armY, 0f), side * reach + Vector3.up * (armY + 0.15f), 0.24f, 8, false);
                m.Color(P("Cactus")).Cylinder(side * reach + Vector3.up * (armY - 0.05f), 0.26f, 0.23f, rise, 8);
                m.Color(P("CactusLight")).Sphere(side * reach + Vector3.up * (armY - 0.05f + rise), new Vector3(0.23f, 0.2f, 0.23f), 3, 8, false, 0.5f);
            }
            m.Color(P("NeonPink")).Sphere(new Vector3(0.12f, height + 0.3f, 0.1f), 0.1f, 3, 6, false);
            return m;
        }

        /// <summary>A cluster of ribbed barrel cacti with flowers.</summary>
        public static LowPolyBuilder BarrelCacti(int seed)
        {
            var m = new LowPolyBuilder();
            var random = new System.Random(seed);
            int count = 3 + random.Next(3);
            for (int i = 0; i < count; i++)
            {
                float angle = Rand(random, 0f, Mathf.PI * 2f);
                float distance = i == 0 ? 0f : Rand(random, 0.6f, 1.3f);
                float scale = i == 0 ? 1f : Rand(random, 0.5f, 0.8f);
                var at = new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);
                m.Color(P(i % 2 == 0 ? "Cactus" : "CactusLight")).Cylinder(at, 0.5f * scale, 0.42f * scale, 0.75f * scale, 10);
                m.Color(P("CactusDark")).Sphere(at + Vector3.up * 0.75f * scale, new Vector3(0.42f, 0.3f, 0.42f) * scale, 3, 10, false, 0.5f);
                m.Color(P(i % 2 == 0 ? "Yellow" : "Pink")).Sphere(at + Vector3.up * (0.98f * scale), 0.12f * scale, 3, 6, false);
            }
            m.Color(P("SandDark")).Blob(new Vector3(0f, 0.02f, 0f), new Vector3(1.5f, 0.12f, 1.4f), 1, 0.2f, seed + 3);
            return m;
        }

        /// <summary>A bare, bleached tree with crooked branches.</summary>
        public static LowPolyBuilder DeadTree(int seed)
        {
            var m = new LowPolyBuilder();
            var random = new System.Random(seed);
            var top = new Vector3(Rand(random, -0.4f, 0.4f), 3.2f, Rand(random, -0.4f, 0.4f));
            m.Color(P("DeadWoodDark")).Rod(Vector3.zero, top, 0.28f, 6, false, 0.14f);
            m.Color(P("DeadWoodDark")).Cylinder(Vector3.zero, 0.5f, 0.28f, 0.35f, 6);
            for (int i = 0; i < 5; i++)
            {
                float t = Rand(random, 0.35f, 1f);
                Vector3 start = top * t;
                float angle = i * 1.3f + Rand(random, -0.3f, 0.3f);
                Vector3 end = start + new Vector3(Mathf.Cos(angle) * Rand(random, 1f, 1.8f), Rand(random, 0.6f, 1.4f), Mathf.Sin(angle) * Rand(random, 1f, 1.8f));
                m.Color(P("DeadWood")).Rod(start, end, 0.12f * (1.2f - t * 0.5f), 5, false, 0.04f);
                Vector3 twig = end + new Vector3(Rand(random, -0.6f, 0.6f), Rand(random, 0.3f, 0.7f), Rand(random, -0.6f, 0.6f));
                m.Color(P("DeadWood")).Rod(Vector3.Lerp(start, end, 0.7f), twig, 0.05f, 4, false, 0.02f);
            }
            return m;
        }

        /// <summary>Stacks of old tyres and one leaning against them.</summary>
        public static LowPolyBuilder TyreStack(int seed)
        {
            var m = new LowPolyBuilder();
            var random = new System.Random(seed);
            var stacks = new[] { new Vector3(0f, 0f, 0f), new Vector3(0.8f, 0f, 0.35f), new Vector3(-0.3f, 0f, 0.82f) };
            for (int s = 0; s < stacks.Length; s++)
            {
                int height = 3 + random.Next(3);
                for (int i = 0; i < height; i++)
                {
                    Vector3 wobble = new Vector3(Rand(random, -0.05f, 0.05f), 0f, Rand(random, -0.05f, 0.05f));
                    string paint = i == height - 1 && s % 2 == 0 ? "White" : i == height - 1 ? "Red" : null;
                    Tyre(m, stacks[s] + wobble + Vector3.up * (0.12f + i * 0.24f), 0.38f, 0.24f, paint);
                }
            }
            StandingTyre(m, new Vector3(-0.9f, 0.4f, -0.3f), new Vector3(0.8f, 0.2f, 0.4f).normalized, 0.38f, 0.24f);
            return m;
        }

        public static LowPolyBuilder OilDrums(int seed)
        {
            var m = new LowPolyBuilder();
            Drum(m, new Vector3(0f, 0f, 0f), "DrumRed");
            Drum(m, new Vector3(0.66f, 0f, 0.1f), "DrumBlue", true);
            Drum(m, new Vector3(0.25f, 0f, 0.62f), "Rust", true);
            LyingDrum(m, new Vector3(-0.7f, 0.3f, 0.5f), new Vector3(0.4f, 0f, 1f), "DrumRed");
            m.Color(P("Soot")).Blob(new Vector3(-0.3f, 0.01f, -0.4f), new Vector3(0.9f, 0.04f, 0.6f), 1, 0.25f, seed);
            return m;
        }

        public static LowPolyBuilder CarHulk(int seed)
        {
            var m = new LowPolyBuilder();
            m.Push(Vector3.zero, Quaternion.Euler(0f, 0f, 4f));
            Car(m, "Soot", true, false, seed);
            m.Pop();
            m.Color(P("Ash")).Blob(new Vector3(0f, 0.02f, 0f), new Vector3(1.6f, 0.06f, 2.6f), 1, 0.25f, seed + 9);
            return m;
        }

        public static LowPolyBuilder DesertBillboard(int seed)
        {
            var m = new LowPolyBuilder();
            Billboard(m, new[] { "Cream", "Red", "Blue", "Yellow" }, seed, true);
            return m;
        }

        /// <summary>A dry, round tumbleweed of twigs.</summary>
        public static LowPolyBuilder Tumbleweed(int seed)
        {
            var m = new LowPolyBuilder();
            var random = new System.Random(seed);
            m.Color(P("Scrub")).Blob(new Vector3(0f, 0.55f, 0f), new Vector3(0.7f, 0.55f, 0.7f), 1, 0.35f, seed);
            m.Color(P("ScrubDark")).Blob(new Vector3(0.55f, 0.35f, 0.3f), new Vector3(0.45f, 0.35f, 0.45f), 1, 0.35f, seed + 1);
            for (int i = 0; i < 6; i++)
            {
                var direction = new Vector3(Rand(random, -1f, 1f), Rand(random, -0.2f, 1f), Rand(random, -1f, 1f)).normalized;
                m.Color(P("DeadWood")).Rod(new Vector3(0f, 0.55f, 0f), new Vector3(0f, 0.55f, 0f) + direction * 0.9f, 0.03f, 3, false);
            }
            return m;
        }

        /// <summary>A ring of stones round a pit of embers (the Crucible's fire pits; a fire effect can burn on top).</summary>
        public static LowPolyBuilder FirePit(int seed)
        {
            var m = new LowPolyBuilder();
            var random = new System.Random(seed);
            const int stones = 10;
            for (int i = 0; i < stones; i++)
            {
                float angle = i * Mathf.PI * 2f / stones;
                var at = new Vector3(Mathf.Cos(angle) * 1.5f, 0.25f, Mathf.Sin(angle) * 1.5f);
                m.Color(P(i % 2 == 0 ? "RockDark" : "Rock")).Blob(at, new Vector3(0.45f, 0.35f, 0.4f) * Rand(random, 0.85f, 1.15f), 0, 0.25f, seed + i);
            }
            m.Color(P("Soot")).Cylinder(new Vector3(0f, 0f, 0f), 1.35f, 1.3f, 0.06f, 12);
            for (int i = 0; i < 7; i++)
            {
                var at = new Vector3(Rand(random, -0.8f, 0.8f), 0.12f, Rand(random, -0.8f, 0.8f));
                m.Color(P(i % 3 == 0 ? "Fire" : "Ember")).Blob(at, new Vector3(0.32f, 0.14f, 0.28f), 0, 0.3f, seed + 20 + i);
            }
            m.Color(P("DeadWoodDark")).Rod(new Vector3(-0.7f, 0.15f, -0.2f), new Vector3(0.6f, 0.3f, 0.3f), 0.1f, 5, false);
            m.Color(P("Soot")).Rod(new Vector3(-0.3f, 0.15f, 0.7f), new Vector3(0.4f, 0.25f, -0.6f), 0.09f, 5, false);
            return m;
        }

        /// <summary>A rusted drum with a fire burning in it.</summary>
        public static LowPolyBuilder BurningBarrel(int seed)
        {
            var m = new LowPolyBuilder();
            Drum(m, Vector3.zero, "Rust", true);
            m.Color(P("Ember")).Cylinder(new Vector3(0f, 0.86f, 0f), 0.27f, 0.27f, 0.04f, 10);
            m.Color(P("Fire")).Blob(new Vector3(0f, 1.05f, 0f), new Vector3(0.24f, 0.26f, 0.24f), 0, 0.35f, seed);
            m.Color(P("Fire")).Blob(new Vector3(0.06f, 1.3f, 0.02f), new Vector3(0.12f, 0.18f, 0.12f), 0, 0.35f, seed + 1);
            return m;
        }

        // ------------------------------------------------------------------------------------------------
        // Landmarks
        // ------------------------------------------------------------------------------------------------

        /// <summary>A banded, flat topped mesa about 26 x 18 m and 11 m tall, with fallen rock round its foot.</summary>
        public static LowPolyBuilder Mesa(int seed)
        {
            var m = new LowPolyBuilder();
            var random = new System.Random(seed);
            const int corners = 16;
            var outline = new Vector2[corners];
            for (int i = 0; i < corners; i++)
            {
                float angle = i * Mathf.PI * 2f / corners;
                float wobble = Rand(random, 0.82f, 1.08f);
                outline[i] = new Vector2(Mathf.Cos(angle) * 13f * wobble, Mathf.Sin(angle) * 9f * wobble);
            }
            string[] bands = { "RockDark", "Rock", "RockBand", "RockRed", "Rock", "RockLight" };
            float y = 0f;
            float scale = 1f;
            for (int layer = 0; layer < bands.Length; layer++)
            {
                float height = layer == bands.Length - 1 ? 0.5f : Rand(random, 1.6f, 2.4f);
                var shrunk = new Vector2[corners];
                for (int i = 0; i < corners; i++)
                {
                    shrunk[i] = outline[i] * scale * Rand(random, 0.97f, 1.03f);
                }
                m.Push(new Vector3(0f, y + height * 0.5f, 0f), Quaternion.Euler(-90f, 0f, 0f));
                m.Color(P(bands[layer])).Prism(shrunk, height);
                m.Pop();
                y += height;
                scale *= layer == 0 ? 0.92f : Rand(random, 0.95f, 0.99f);
            }
            m.Push(new Vector3(0f, y + 0.1f, 0f), Quaternion.Euler(-90f, 0f, 0f));
            var top = new Vector2[corners];
            for (int i = 0; i < corners; i++)
            {
                top[i] = outline[i] * scale * 0.96f;
            }
            m.Color(P("SandLight")).Prism(top, 0.2f);
            m.Pop();
            for (int i = 0; i < 14; i++)
            {
                float angle = Rand(random, 0f, Mathf.PI * 2f);
                float wobble = Rand(random, 1.02f, 1.18f);
                var at = new Vector3(Mathf.Cos(angle) * 13f * wobble, 0.4f, -Mathf.Sin(angle) * 9f * wobble);
                float size = Rand(random, 0.8f, 1.8f);
                m.Color(P(i % 3 == 0 ? "RockDark" : "Rock")).Blob(at, new Vector3(1f, 0.7f, 0.9f) * size, 0, 0.3f, seed + 30 + i);
            }
            return m;
        }

        /// <summary>A rusted school bus lying on its side, half sunk in drifted sand, 11 m long.</summary>
        public static LowPolyBuilder WreckedBus(int seed)
        {
            var m = new LowPolyBuilder();
            var random = new System.Random(seed);
            m.Push(new Vector3(0f, 1.3f, 0f), Quaternion.Euler(0f, 0f, 84f));
            m.Color(P("BusYellow")).BeveledBox(new Vector3(0f, 0f, 0f), new Vector3(2.6f, 2.5f, 11f), 0.2f);
            m.Color(P("BusYellowDark")).Box(new Vector3(0f, -0.62f, 0f), new Vector3(2.64f, 0.16f, 10.8f));
            m.Color(P("Black")).Box(new Vector3(0f, -1.32f, 0f), new Vector3(2.4f, 0.12f, 10.4f));
            for (int i = 0; i < 7; i++)
            {
                float z = -4.2f + i * 1.35f;
                m.Color(P(random.NextDouble() < 0.4 ? "Black" : "Glass")).Box(new Vector3(1.31f, 0.4f, z), new Vector3(0.06f, 0.8f, 1.05f));
                m.Color(P(random.NextDouble() < 0.4 ? "Black" : "Glass")).Box(new Vector3(-1.31f, 0.4f, z), new Vector3(0.06f, 0.8f, 1.05f));
            }
            m.Color(P("Glass")).Box(new Vector3(0f, 0.4f, 5.52f), new Vector3(2.2f, 0.9f, 0.06f));
            m.Color(P("Charcoal")).Box(new Vector3(0f, -0.9f, 5.55f), new Vector3(2.4f, 0.35f, 0.1f));
            for (int i = 0; i < 4; i++)
            {
                var at = new Vector3(Rand(random, -1.3f, 1.3f), Rand(random, -1f, 1f), Rand(random, -5f, 5f));
                m.Color(P(i % 2 == 0 ? "Rust" : "RustDark")).Box(at, new Vector3(2.66f, Rand(random, 0.4f, 0.9f), Rand(random, 0.6f, 1.6f)));
            }
            m.Pop();
            // The wheels of the side that now faces the sky.
            foreach (float z in new[] { 3.6f, -3.4f })
            {
                StandingTyre(m, new Vector3(0.75f, 2.55f, z), Vector3.up, 0.5f, 0.32f);
            }
            m.Color(P("Sand")).Blob(new Vector3(0.8f, 0.2f, -2f), new Vector3(2.2f, 0.7f, 3.8f), 1, 0.2f, seed + 1);
            m.Color(P("SandLight")).Blob(new Vector3(1.2f, 0.15f, 3.5f), new Vector3(1.6f, 0.5f, 2.2f), 1, 0.2f, seed + 2);
            return m;
        }

        /// <summary>An abandoned gas station: canopy over two pumps, a little shop with a sign, a tall price pole.</summary>
        public static LowPolyBuilder GasStation(int seed)
        {
            var m = new LowPolyBuilder();
            m.Color(P("ConcreteDark")).Box(new Vector3(0f, 0.08f, 0f), new Vector3(16f, 0.16f, 12f));
            // Canopy.
            foreach (Vector2 post in new[] { new Vector2(-3f, -2f), new Vector2(3f, -2f), new Vector2(-3f, 2f), new Vector2(3f, 2f) })
            {
                m.Color(P("White")).Box(new Vector3(post.x, 2.35f, post.y), new Vector3(0.3f, 4.4f, 0.3f));
            }
            m.Color(P("White")).Box(new Vector3(0f, 4.7f, 0f), new Vector3(9f, 0.4f, 6.4f));
            m.Color(P("Red")).Box(new Vector3(0f, 4.72f, 0f), new Vector3(9.06f, 0.24f, 6.46f));
            m.Color(P("NeonRed")).Box(new Vector3(0f, 4.5f, -3.23f), new Vector3(8.8f, 0.06f, 0.04f));
            m.Color(P("Rust")).Box(new Vector3(2.4f, 4.92f, 1.2f), new Vector3(3.4f, 0.04f, 2.6f));
            m.Color(P("LampHead")).Box(new Vector3(-1.8f, 4.48f, 0f), new Vector3(1.4f, 0.04f, 0.4f));
            m.Color(P("LampHead")).Box(new Vector3(1.8f, 4.48f, 0f), new Vector3(1.4f, 0.04f, 0.4f));
            // Pump islands.
            foreach (float x in new[] { -1.8f, 1.8f })
            {
                m.Color(P("Concrete")).Box(new Vector3(x, 0.25f, 0f), new Vector3(1.1f, 0.2f, 3.2f));
                m.Color(P("Red")).Box(new Vector3(x, 1f, -0.6f), new Vector3(0.6f, 1.3f, 0.5f));
                m.Color(P("Cream")).Box(new Vector3(x, 1.3f, -0.86f), new Vector3(0.44f, 0.4f, 0.04f));
                m.Color(P("Red")).Box(new Vector3(x, 1f, 0.6f), new Vector3(0.6f, 1.3f, 0.5f));
                m.Color(P("Charcoal")).Rod(new Vector3(x + 0.3f, 1.2f, 0.6f), new Vector3(x + 0.5f, 0.4f, 1.1f), 0.04f, 4, false);
            }
            // The shop.
            WindowedBlock(m, new Vector3(0f, 0.16f, 5f), new Vector3(7f, 3.6f, 3.6f), "Cream", seed, new[] { "WindowWarm" }, 0.6f);
            m.Color(P("RedDark")).Box(new Vector3(0f, 3.95f, 5f), new Vector3(7.2f, 0.3f, 3.8f));
            m.Color(P("Red")).Box(new Vector3(0f, 3.3f, 3.15f), new Vector3(4f, 0.7f, 0.12f));
            m.Color(P("NeonYellow")).Box(new Vector3(0f, 3.3f, 3.08f), new Vector3(3.2f, 0.28f, 0.04f));
            // Price pole.
            m.Color(P("SteelDark")).Box(new Vector3(6.5f, 3.5f, -4.5f), new Vector3(0.3f, 7f, 0.3f));
            m.Color(P("White")).Box(new Vector3(6.5f, 7.6f, -4.5f), new Vector3(2.6f, 1.6f, 0.4f));
            m.Color(P("Red")).Box(new Vector3(6.5f, 8f, -4.71f), new Vector3(2.3f, 0.5f, 0.04f));
            m.Color(P("NeonRed")).Box(new Vector3(6.5f, 7.3f, -4.72f), new Vector3(2f, 0.36f, 0.04f));
            Drum(m, new Vector3(-6.2f, 0.16f, 4.5f), "DrumRed", true);
            Drum(m, new Vector3(-5.6f, 0.16f, 4.9f), "DrumBlue", true);
            Tyre(m, new Vector3(-6.4f, 0.28f, 3.6f), 0.38f, 0.24f);
            return m;
        }

        /// <summary>A wooden pole with an arm reaching along +Z and a lamp at its end, 7 m up.</summary>
        public static LowPolyBuilder PoleLamp(int seed, out Vector3 head)
        {
            var m = new LowPolyBuilder();
            m.Color(P("WoodDark")).Cylinder(Vector3.zero, 0.16f, 0.12f, 7.4f, 6);
            m.Color(P("Wood")).Box(new Vector3(0f, 6.9f, 0f), new Vector3(1.4f, 0.12f, 0.14f));
            m.Color(P("LampPole")).Rod(new Vector3(0f, 6.6f, 0f), new Vector3(0f, 7.05f, 2.6f), 0.06f, 5, false);
            m.Color(P("LampPole")).Box(new Vector3(0f, 7.02f, 2.7f), new Vector3(0.42f, 0.18f, 0.8f));
            m.Color(P("LampHead")).Box(new Vector3(0f, 6.91f, 2.72f), new Vector3(0.34f, 0.06f, 0.64f));
            head = new Vector3(0f, 6.88f, 2.72f);
            return m;
        }
    }
}
