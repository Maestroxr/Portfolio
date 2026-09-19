using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.EndlessRunner.EditorTools
{
    /// <summary>Recipes of the scenery beside the road, grouped by world. Every model stands on y = 0.</summary>
    internal static class SceneryModels
    {
        // ------------------------------------------------------------------ meadow

        public static MeshBuilder RoundTree(int seed, Swatch leaf, Swatch leafDark, Swatch trunk, Swatch fruit = Swatch.Black, bool fruits = false)
        {
            var m = new MeshBuilder();
            m.Color(trunk).Cylinder(Vector3.zero, 0.24f, 0.15f, 1.9f, 7);
            m.Color(trunk).Rod(new Vector3(0f, 1.3f, 0f), new Vector3(0.55f, 2f, 0.2f), 0.08f, 5, false);
            m.Color(leaf).Blob(new Vector3(0f, 2.5f, 0f), new Vector3(1.35f, 1.15f, 1.35f), 1, 0.16f, seed);
            m.Color(leafDark).Blob(new Vector3(0.7f, 2.1f, 0.35f), new Vector3(0.85f, 0.75f, 0.85f), 1, 0.16f, seed + 1);
            m.Color(leaf).Blob(new Vector3(-0.55f, 3.1f, -0.25f), new Vector3(0.85f, 0.75f, 0.85f), 1, 0.16f, seed + 2);
            if (fruits)
            {
                var random = new System.Random(seed);
                for (int i = 0; i < 9; i++)
                {
                    float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                    float height = 1.8f + (float)random.NextDouble() * 1.5f;
                    var position = new Vector3(Mathf.Cos(angle) * 1.25f, height, Mathf.Sin(angle) * 1.25f);
                    m.Color(fruit).Sphere(position, 0.11f, 4, 6);
                }
            }
            return m;
        }

        public static MeshBuilder Poplar(int seed)
        {
            var m = new MeshBuilder();
            m.Color(Swatch.Bark).Cylinder(Vector3.zero, 0.18f, 0.12f, 1.4f, 6);
            m.Color(Swatch.LeafLight).Blob(new Vector3(0f, 3f, 0f), new Vector3(0.9f, 2.2f, 0.9f), 1, 0.12f, seed);
            m.Color(Swatch.Leaf).Blob(new Vector3(0.25f, 2.2f, 0.2f), new Vector3(0.7f, 1.2f, 0.7f), 1, 0.12f, seed + 3);
            return m;
        }

        public static MeshBuilder Pine(int seed, Swatch needles, Swatch needlesDark, Swatch trunk, bool snow, float scale = 1f)
        {
            var m = new MeshBuilder();
            m.Push(Vector3.zero, Quaternion.identity, Vector3.one * scale);
            m.Color(trunk).Cylinder(Vector3.zero, 0.22f, 0.16f, 1.1f, 6);
            float radius = 1.6f;
            float y = 0.8f;
            for (int tier = 0; tier < 4; tier++)
            {
                float height = 1.6f - tier * 0.2f;
                m.Color(tier % 2 == 0 ? needles : needlesDark).Cylinder(new Vector3(0f, y, 0f), radius, 0f, height, 8, false, true, false, tier * 0.4f);
                if (snow)
                {
                    m.Color(Swatch.Snow).Cylinder(new Vector3(0f, y + height * 0.45f, 0f), radius * 0.56f, 0f, height * 0.56f, 8, false, true, false, tier * 0.4f);
                }
                y += height * 0.62f;
                radius *= 0.74f;
            }
            m.Pop();
            return m;
        }

        public static MeshBuilder Bush(int seed, Swatch leaf, Swatch leafDark, Swatch berries = Swatch.Black, bool withBerries = false)
        {
            var m = new MeshBuilder();
            m.Color(leaf).Blob(new Vector3(0f, 0.45f, 0f), new Vector3(0.8f, 0.6f, 0.8f), 1, 0.2f, seed);
            m.Color(leafDark).Blob(new Vector3(0.6f, 0.35f, 0.2f), new Vector3(0.55f, 0.45f, 0.55f), 1, 0.2f, seed + 1);
            m.Color(leaf).Blob(new Vector3(-0.5f, 0.3f, -0.2f), new Vector3(0.5f, 0.4f, 0.5f), 1, 0.2f, seed + 2);
            if (withBerries)
            {
                var random = new System.Random(seed);
                for (int i = 0; i < 7; i++)
                {
                    float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                    m.Color(berries).Sphere(new Vector3(Mathf.Cos(angle) * 0.7f, 0.35f + (float)random.NextDouble() * 0.4f, Mathf.Sin(angle) * 0.7f), 0.07f, 4, 6);
                }
            }
            return m;
        }

        public static MeshBuilder Flowers(int seed)
        {
            var m = new MeshBuilder();
            var random = new System.Random(seed);
            Swatch[] colors = { Swatch.FlowerRed, Swatch.FlowerYellow, Swatch.FlowerPink, Swatch.FlowerPurple, Swatch.FlowerWhite };
            for (int i = 0; i < 9; i++)
            {
                var root = new Vector3(((float)random.NextDouble() - 0.5f) * 1.6f, 0f, ((float)random.NextDouble() - 0.5f) * 1.6f);
                float height = 0.3f + (float)random.NextDouble() * 0.3f;
                m.Color(Swatch.Leaf).Cylinder(root, 0.025f, 0.02f, height, 4);
                Swatch petal = colors[random.Next(colors.Length)];
                Vector3 head = root + Vector3.up * height;
                for (int p = 0; p < 5; p++)
                {
                    float angle = p * Mathf.PI * 2f / 5f;
                    m.Color(petal).Sphere(head + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 0.07f, new Vector3(0.06f, 0.03f, 0.06f), 3, 5, false);
                }
                m.Color(petal == Swatch.FlowerYellow ? Swatch.Orange : Swatch.FlowerYellow).Sphere(head + Vector3.up * 0.02f, 0.045f, 3, 5, false);
                m.Color(Swatch.LeafDark).Sphere(root + new Vector3(0.06f, 0.08f, 0f), new Vector3(0.1f, 0.03f, 0.05f), 3, 5, false);
            }
            return m;
        }

        public static MeshBuilder Rock(int seed, Swatch color, Swatch shade, float size = 1f)
        {
            var m = new MeshBuilder();
            m.Color(color).Blob(new Vector3(0f, 0.35f * size, 0f), new Vector3(0.9f, 0.6f, 0.75f) * size, 1, 0.25f, seed);
            m.Color(shade).Blob(new Vector3(0.65f * size, 0.2f * size, 0.3f * size), new Vector3(0.45f, 0.35f, 0.4f) * size, 0, 0.25f, seed + 1);
            return m;
        }

        public static MeshBuilder Mushroom(int seed, Swatch cap, Swatch dots, Swatch stem)
        {
            var m = new MeshBuilder();
            var random = new System.Random(seed);
            int count = 1 + random.Next(3);
            for (int i = 0; i < count; i++)
            {
                float scale = i == 0 ? 1f : 0.45f + (float)random.NextDouble() * 0.3f;
                var root = i == 0 ? Vector3.zero : new Vector3(((float)random.NextDouble() - 0.5f) * 1.2f, 0f, ((float)random.NextDouble() - 0.5f) * 1.2f);
                m.Color(stem).Cylinder(root, 0.16f * scale, 0.12f * scale, 0.7f * scale, 8, true);
                Vector3 capCenter = root + Vector3.up * 0.65f * scale;
                m.Color(cap).Sphere(capCenter, new Vector3(0.55f, 0.42f, 0.55f) * scale, 6, 12, true, 0.5f);
                for (int d = 0; d < 6; d++)
                {
                    float angle = d * Mathf.PI * 2f / 6f + i;
                    float tilt = 0.55f + (d % 2) * 0.25f;
                    Vector3 direction = new Vector3(Mathf.Cos(angle) * Mathf.Sin(tilt), Mathf.Cos(tilt), Mathf.Sin(angle) * Mathf.Sin(tilt));
                    Vector3 point = capCenter + Vector3.Scale(direction, new Vector3(0.55f, 0.42f, 0.55f) * scale);
                    m.Color(dots).Sphere(point, 0.06f * scale, 3, 5, false);
                }
            }
            return m;
        }

        public static MeshBuilder Cottage()
        {
            var m = new MeshBuilder();
            m.Color(Swatch.Gray).Box(new Vector3(0f, 0.15f, 0f), new Vector3(4.4f, 0.3f, 3.6f));
            m.Color(Swatch.Wall).BeveledBox(new Vector3(0f, 1.5f, 0f), new Vector3(4f, 2.4f, 3.2f), 0.05f);
            m.Push(new Vector3(0f, 2.7f, 0f), Quaternion.Euler(0f, 90f, 0f));
            m.Color(Swatch.Roof).Prism(new[] { new Vector2(-2.05f, 0f), new Vector2(2.05f, 0f), new Vector2(0f, 1.6f) }, 4.5f);
            m.Pop();
            m.Push(new Vector3(0f, 2.66f, 0f), Quaternion.Euler(0f, 90f, 0f));
            m.Color(Swatch.RoofDark).Prism(new[] { new Vector2(-2.15f, -0.05f), new Vector2(2.15f, -0.05f), new Vector2(0f, 1.62f) }, 4.3f);
            m.Pop();
            m.Color(Swatch.RoofDark).Box(new Vector3(1.1f, 3.6f, 0.6f), new Vector3(0.45f, 1.2f, 0.45f));
            m.Color(Swatch.Brown).Box(new Vector3(0f, 1f, 1.61f), new Vector3(0.8f, 1.4f, 0.06f));
            m.Color(Swatch.Gold).Sphere(new Vector3(0.25f, 1f, 1.66f), 0.05f, 3, 5);
            foreach (Vector3 window in new[] { new Vector3(-1.2f, 1.6f, 1.61f), new Vector3(1.2f, 1.6f, 1.61f), new Vector3(-1.2f, 1.6f, -1.61f), new Vector3(1.2f, 1.6f, -1.61f) })
            {
                m.Color(Swatch.White).Box(window, new Vector3(0.8f, 0.8f, 0.06f));
                m.Color(Swatch.Window).Box(window + new Vector3(0f, 0f, Mathf.Sign(window.z) * 0.02f), new Vector3(0.62f, 0.62f, 0.05f));
                m.Color(Swatch.White).Box(window + new Vector3(0f, 0f, Mathf.Sign(window.z) * 0.04f), new Vector3(0.06f, 0.62f, 0.02f));
                m.Color(Swatch.Green).Box(window + new Vector3(0f, -0.48f, Mathf.Sign(window.z) * 0.12f), new Vector3(0.8f, 0.14f, 0.22f));
            }
            return m;
        }

        /// <summary>A 6 m fence segment along z, for the left side of the road.</summary>
        public static MeshBuilder Fence(float length)
        {
            var m = new MeshBuilder();
            for (float z = 0f; z <= length + 0.01f; z += length / 4f)
            {
                m.Color(Swatch.WoodLight).Box(new Vector3(0f, 0.55f, z), new Vector3(0.14f, 1.1f, 0.14f));
                m.Color(Swatch.Wood).Box(new Vector3(0f, 1.14f, z), new Vector3(0.18f, 0.08f, 0.18f));
            }
            m.Color(Swatch.White).Box(new Vector3(0.02f, 0.85f, length * 0.5f), new Vector3(0.06f, 0.14f, length));
            m.Color(Swatch.White).Box(new Vector3(0.02f, 0.45f, length * 0.5f), new Vector3(0.06f, 0.14f, length));
            return m;
        }

        // ------------------------------------------------------------------ desert

        public static MeshBuilder Saguaro(int seed)
        {
            var m = new MeshBuilder();
            var random = new System.Random(seed);
            float height = 2.6f + (float)random.NextDouble() * 1.2f;
            m.Color(Swatch.Cactus).Cylinder(Vector3.zero, 0.36f, 0.32f, height, 8);
            m.Color(Swatch.Cactus).Sphere(new Vector3(0f, height, 0f), new Vector3(0.32f, 0.3f, 0.32f), 4, 8, false, 0.5f);
            int arms = 1 + random.Next(2);
            for (int i = 0; i < arms; i++)
            {
                float side = i == 0 ? 1f : -1f;
                float armY = 1f + (float)random.NextDouble() * 0.8f;
                float reach = 0.7f + (float)random.NextDouble() * 0.2f;
                float rise = 0.8f + (float)random.NextDouble() * 0.6f;
                m.Color(Swatch.CactusDark).Rod(new Vector3(0f, armY, 0f), new Vector3(side * reach, armY + 0.1f, 0f), 0.2f, 8, false);
                m.Color(Swatch.Cactus).Cylinder(new Vector3(side * reach, armY - 0.05f, 0f), 0.22f, 0.2f, rise, 8);
                m.Color(Swatch.Cactus).Sphere(new Vector3(side * reach, armY - 0.05f + rise, 0f), new Vector3(0.2f, 0.18f, 0.2f), 3, 8, false, 0.5f);
            }
            m.Color(Swatch.FlowerPink).Sphere(new Vector3(0.1f, height + 0.25f, 0.1f), 0.09f, 3, 6, false);
            m.Color(Swatch.FlowerYellow).Sphere(new Vector3(-0.12f, height + 0.22f, -0.05f), 0.08f, 3, 6, false);
            return m;
        }

        public static MeshBuilder BarrelCactus(int seed)
        {
            var m = new MeshBuilder();
            m.Color(Swatch.CactusLight).Sphere(new Vector3(0f, 0.42f, 0f), new Vector3(0.55f, 0.48f, 0.55f), 5, 10, false);
            m.Color(Swatch.Cactus).Sphere(new Vector3(0.6f, 0.26f, 0.2f), new Vector3(0.32f, 0.3f, 0.32f), 4, 8, false);
            for (int i = 0; i < 4; i++)
            {
                float angle = i * Mathf.PI * 0.5f + seed;
                m.Color(i % 2 == 0 ? Swatch.FlowerPink : Swatch.FlowerYellow).Sphere(new Vector3(Mathf.Cos(angle) * 0.18f, 0.9f, Mathf.Sin(angle) * 0.18f), 0.08f, 3, 5, false);
            }
            return m;
        }

        public static MeshBuilder Mesa(int seed)
        {
            var m = new MeshBuilder();
            var random = new System.Random(seed);
            Swatch[] layers = { Swatch.RockRed, Swatch.RockOrange, Swatch.Terracotta, Swatch.RockOrange };
            float radius = 7f;
            float y = 0f;
            for (int i = 0; i < layers.Length; i++)
            {
                float height = 1.4f + (float)random.NextDouble() * 1f;
                float top = radius * (0.9f - (float)random.NextDouble() * 0.08f);
                m.Color(layers[i]).Cylinder(new Vector3(0f, y, 0f), radius, top, height, 9, false, i == 0, i == layers.Length - 1, i * 0.3f);
                y += height;
                radius = top * 0.97f;
            }
            return m;
        }

        public static MeshBuilder DesertRock(int seed)
        {
            var m = new MeshBuilder();
            m.Color(Swatch.RockOrange).Blob(new Vector3(0f, 0.6f, 0f), new Vector3(1.3f, 0.9f, 1.1f), 1, 0.22f, seed);
            m.Color(Swatch.RockRed).Blob(new Vector3(0.9f, 0.3f, 0.4f), new Vector3(0.6f, 0.45f, 0.55f), 0, 0.2f, seed + 1);
            m.Color(Swatch.Terracotta).Box(new Vector3(0f, 0.95f, 0f), new Vector3(1.5f, 0.12f, 1.2f));
            return m;
        }

        public static MeshBuilder PalmTree(int seed)
        {
            var m = new MeshBuilder();
            var random = new System.Random(seed);
            float lean = 0.3f + (float)random.NextDouble() * 0.4f;
            Vector3 previous = Vector3.zero;
            const int segments = 7;
            for (int i = 1; i <= segments; i++)
            {
                float t = i / (float)segments;
                var point = new Vector3(lean * t * t * 2f, t * 4.2f, 0f);
                m.Color(i % 2 == 0 ? Swatch.Bark : Swatch.Wood).Rod(previous, point, 0.2f - t * 0.06f, 6, false, 0.2f - t * 0.06f);
                previous = point;
            }
            for (int leaf = 0; leaf < 7; leaf++)
            {
                float angle = leaf * Mathf.PI * 2f / 7f;
                var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Vector3 start = previous;
                Vector3 middle = start + direction * 1f + Vector3.up * 0.35f;
                Vector3 end = start + direction * 1.9f - Vector3.up * 0.45f;
                m.Color(leaf % 2 == 0 ? Swatch.Leaf : Swatch.LeafLight);
                m.Push((start + middle) * 0.5f, Quaternion.LookRotation(middle - start));
                m.Box(Vector3.zero, new Vector3(0.45f, 0.05f, (middle - start).magnitude));
                m.Pop();
                m.Push((middle + end) * 0.5f, Quaternion.LookRotation(end - middle));
                m.Box(Vector3.zero, new Vector3(0.35f, 0.05f, (end - middle).magnitude));
                m.Pop();
            }
            for (int i = 0; i < 3; i++)
            {
                float angle = i * 2.1f;
                m.Color(Swatch.DarkBrown).Sphere(previous + new Vector3(Mathf.Cos(angle) * 0.18f, -0.2f, Mathf.Sin(angle) * 0.18f), 0.13f, 4, 6, false);
            }
            return m;
        }

        public static MeshBuilder DeadBush(int seed)
        {
            var m = new MeshBuilder();
            var random = new System.Random(seed);
            for (int i = 0; i < 7; i++)
            {
                float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                float tilt = 0.4f + (float)random.NextDouble() * 0.6f;
                var tip = new Vector3(Mathf.Cos(angle) * Mathf.Sin(tilt), Mathf.Cos(tilt), Mathf.Sin(angle) * Mathf.Sin(tilt)) * (0.7f + (float)random.NextDouble() * 0.5f);
                m.Color(Swatch.Bark).Rod(Vector3.zero, tip, 0.04f, 4, false, 0.015f);
                m.Color(Swatch.Bark).Rod(tip * 0.6f, tip * 0.6f + new Vector3(tip.z, 0.3f, -tip.x) * 0.4f, 0.025f, 4, false, 0.01f);
            }
            return m;
        }

        // ------------------------------------------------------------------ snow

        public static MeshBuilder Snowman()
        {
            var m = new MeshBuilder();
            m.Color(Swatch.Snow).Sphere(new Vector3(0f, 0.55f, 0f), 0.6f, 10, 14);
            m.Color(Swatch.Snow).Sphere(new Vector3(0f, 1.35f, 0f), 0.44f, 10, 14);
            m.Color(Swatch.Snow).Sphere(new Vector3(0f, 1.98f, 0f), 0.32f, 10, 14);
            m.Color(Swatch.Black).Sphere(new Vector3(-0.11f, 2.06f, 0.28f), 0.045f, 4, 6);
            m.Color(Swatch.Black).Sphere(new Vector3(0.11f, 2.06f, 0.28f), 0.045f, 4, 6);
            m.Push(new Vector3(0f, 1.98f, 0.3f), Quaternion.Euler(90f, 0f, 0f));
            m.Color(Swatch.Carrot).Cylinder(Vector3.zero, 0.06f, 0f, 0.3f, 6);
            m.Pop();
            for (int i = 0; i < 3; i++)
            {
                m.Color(Swatch.Black).Sphere(new Vector3(0f, 1.2f + i * 0.17f, 0.42f - Mathf.Abs(i - 1) * 0.02f), 0.045f, 4, 6);
            }
            m.Color(Swatch.Red).Torus(new Vector3(0f, 1.72f, 0f), 0.3f, 0.07f, 14, 6);
            m.Color(Swatch.Red).Box(new Vector3(0.18f, 1.55f, 0.25f), new Vector3(0.12f, 0.35f, 0.05f));
            m.Color(Swatch.Black).Cylinder(new Vector3(0f, 2.22f, 0f), 0.36f, 0.36f, 0.05f, 12);
            m.Color(Swatch.Black).Cylinder(new Vector3(0f, 2.25f, 0f), 0.22f, 0.22f, 0.36f, 12);
            m.Color(Swatch.Red).Cylinder(new Vector3(0f, 2.28f, 0f), 0.225f, 0.225f, 0.07f, 12);
            m.Color(Swatch.Bark).Rod(new Vector3(0.38f, 1.4f, 0f), new Vector3(0.95f, 1.8f, 0.05f), 0.035f, 4, false, 0.02f);
            m.Color(Swatch.Bark).Rod(new Vector3(-0.38f, 1.4f, 0f), new Vector3(-0.9f, 1.72f, -0.05f), 0.035f, 4, false, 0.02f);
            return m;
        }

        public static MeshBuilder Crystals(int seed, Swatch color, Swatch accent, float size = 1f)
        {
            var m = new MeshBuilder();
            var random = new System.Random(seed);
            for (int i = 0; i < 6; i++)
            {
                float height = (0.6f + (float)random.NextDouble() * 1.2f) * size;
                float radius = (0.12f + (float)random.NextDouble() * 0.12f) * size;
                var tilt = Quaternion.Euler(((float)random.NextDouble() - 0.5f) * 50f, (float)random.NextDouble() * 360f, ((float)random.NextDouble() - 0.5f) * 50f);
                var root = new Vector3(((float)random.NextDouble() - 0.5f) * 0.8f * size, 0f, ((float)random.NextDouble() - 0.5f) * 0.8f * size);
                m.Push(root, tilt);
                m.Color(i % 3 == 0 ? accent : color).Cylinder(Vector3.zero, radius, radius, height, 6);
                m.Color(i % 3 == 0 ? accent : color).Cylinder(Vector3.up * height, radius, 0f, radius * 1.8f, 6, false, false, false);
                m.Pop();
            }
            return m;
        }

        public static MeshBuilder SnowPole()
        {
            var m = new MeshBuilder();
            for (int band = 0; band < 6; band++)
            {
                m.Color(band % 2 == 0 ? Swatch.Red : Swatch.White).Cylinder(new Vector3(0f, band * 0.3f, 0f), 0.05f, 0.05f, 0.3f, 6, true, band == 0, band == 5);
            }
            m.Color(Swatch.GlowRed).Box(new Vector3(0.06f, 1.6f, 0f), new Vector3(0.02f, 0.14f, 0.06f));
            return m;
        }

        // ------------------------------------------------------------------ night

        public static MeshBuilder GlowMushrooms(int seed)
        {
            var m = new MeshBuilder();
            var random = new System.Random(seed);
            Swatch[] caps = { Swatch.GlowCyan, Swatch.GlowMagenta, Swatch.GlowPurple };
            for (int i = 0; i < 4; i++)
            {
                float scale = i == 0 ? 1.2f : 0.5f + (float)random.NextDouble() * 0.5f;
                var root = i == 0 ? Vector3.zero : new Vector3(((float)random.NextDouble() - 0.5f) * 1.4f, 0f, ((float)random.NextDouble() - 0.5f) * 1.4f);
                m.Color(Swatch.LightGray).Cylinder(root, 0.1f * scale, 0.07f * scale, 0.8f * scale, 7, true);
                m.Color(caps[(i + seed) % caps.Length]).Sphere(root + Vector3.up * 0.75f * scale, new Vector3(0.42f, 0.3f, 0.42f) * scale, 5, 10, true, 0.5f);
            }
            return m;
        }

        /// <summary>A street lamp for the left side of the road; its arm reaches over the curb (+x).</summary>
        public static MeshBuilder LampPost()
        {
            var m = new MeshBuilder();
            m.Color(Swatch.Charcoal).Cylinder(Vector3.zero, 0.2f, 0.16f, 0.3f, 8);
            m.Color(Swatch.Charcoal).Cylinder(new Vector3(0f, 0.3f, 0f), 0.08f, 0.07f, 3.6f, 8, true);
            m.Color(Swatch.Charcoal).Rod(new Vector3(0f, 3.8f, 0f), new Vector3(0.8f, 4.1f, 0f), 0.05f, 6);
            m.Color(Swatch.Charcoal).Cylinder(new Vector3(0.85f, 3.9f, 0f), 0.3f, 0.12f, 0.25f, 8);
            m.Color(Swatch.GlowWarm).Sphere(new Vector3(0.85f, 3.85f, 0f), 0.16f, 5, 8);
            m.Color(Swatch.Charcoal).Sphere(new Vector3(0f, 3.95f, 0f), 0.09f, 4, 6);
            return m;
        }

        // ------------------------------------------------------------------ volcano

        public static MeshBuilder BasaltColumns(int seed)
        {
            var m = new MeshBuilder();
            var random = new System.Random(seed);
            for (int i = 0; i < 7; i++)
            {
                float angle = i * Mathf.PI * 2f / 7f;
                float distance = i == 0 ? 0f : 0.5f;
                var root = new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);
                float height = 0.8f + (float)random.NextDouble() * 2.4f;
                m.Color(Swatch.Basalt).Cylinder(root, 0.3f, 0.3f, height, 6, false, false, false);
                m.Color(Swatch.Ash).Cylinder(root + Vector3.up * height, 0.3f, 0.26f, 0.06f, 6);
            }
            return m;
        }

        public static MeshBuilder DeadTree(int seed)
        {
            var m = new MeshBuilder();
            var random = new System.Random(seed);
            m.Color(Swatch.Obsidian).Cylinder(Vector3.zero, 0.25f, 0.12f, 2.6f, 6);
            for (int i = 0; i < 5; i++)
            {
                float y = 1.2f + i * 0.3f;
                float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                var tip = new Vector3(Mathf.Cos(angle) * 1f, y + 0.6f + (float)random.NextDouble() * 0.5f, Mathf.Sin(angle) * 1f);
                m.Color(Swatch.Obsidian).Rod(new Vector3(0f, y, 0f), tip, 0.07f, 5, false, 0.02f);
            }
            return m;
        }

        public static MeshBuilder LavaRock(int seed)
        {
            var m = new MeshBuilder();
            m.Color(Swatch.GlowLava).Blob(new Vector3(0f, 0.45f, 0f), new Vector3(0.95f, 0.62f, 0.85f), 1, 0.05f, seed + 7);
            m.Color(Swatch.Basalt).Blob(new Vector3(0f, 0.5f, 0f), new Vector3(1f, 0.7f, 0.9f), 1, 0.28f, seed);
            m.Color(Swatch.Ash).Blob(new Vector3(0.7f, 0.25f, 0.3f), new Vector3(0.45f, 0.35f, 0.4f), 0, 0.25f, seed + 1);
            return m;
        }

        public static MeshBuilder Volcano()
        {
            var m = new MeshBuilder();
            m.Color(Swatch.Basalt).Cylinder(Vector3.zero, 16f, 6f, 9f, 11, false, false, false);
            m.Color(Swatch.Ash).Cylinder(new Vector3(0f, 9f, 0f), 6f, 3.5f, 3f, 11, false, false, false, 0.2f);
            m.Color(Swatch.GlowLava).Cylinder(new Vector3(0f, 11.2f, 0f), 3.3f, 3.3f, 0.2f, 11);
            for (int i = 0; i < 4; i++)
            {
                float angle = i * 1.7f;
                var top = new Vector3(Mathf.Cos(angle) * 3.4f, 11.6f, Mathf.Sin(angle) * 3.4f);
                var bottom = new Vector3(Mathf.Cos(angle + 0.2f) * 13f, 2f, Mathf.Sin(angle + 0.2f) * 13f);
                m.Color(Swatch.GlowOrange).Rod(top, bottom, 0.35f, 5, false, 0.15f);
            }
            return m;
        }

        public static MeshBuilder Torch()
        {
            var m = new MeshBuilder();
            m.Color(Swatch.Basalt).Cylinder(Vector3.zero, 0.25f, 0.2f, 0.3f, 6);
            m.Color(Swatch.Bark).Cylinder(new Vector3(0f, 0.3f, 0f), 0.07f, 0.06f, 1.9f, 6);
            m.Color(Swatch.MetalDark).Cylinder(new Vector3(0f, 2.1f, 0f), 0.12f, 0.24f, 0.25f, 8);
            m.Color(Swatch.GlowOrange).Cylinder(new Vector3(0f, 2.3f, 0f), 0.2f, 0f, 0.55f, 6);
            m.Color(Swatch.GlowYellow).Cylinder(new Vector3(0f, 2.32f, 0f), 0.1f, 0f, 0.35f, 6);
            return m;
        }

        /// <summary>A puffy cloud made of a few blobs; used far off the road.</summary>
        public static MeshBuilder Cloud(int seed, Swatch color)
        {
            var m = new MeshBuilder();
            var random = new System.Random(seed);
            for (int i = 0; i < 5; i++)
            {
                var center = new Vector3((i - 2) * 1.5f, ((float)random.NextDouble() - 0.3f) * 0.8f, ((float)random.NextDouble() - 0.5f) * 1.2f);
                float size = 1.2f + (float)random.NextDouble() * 0.9f - Mathf.Abs(i - 2) * 0.25f;
                m.Color(color).Blob(center, new Vector3(size, size * 0.8f, size), 1, 0.12f, seed + i);
            }
            return m;
        }

        public static IEnumerable<int> Seeds(int count, int start)
        {
            for (int i = 0; i < count; i++)
            {
                yield return start + i * 13;
            }
        }
    }
}
