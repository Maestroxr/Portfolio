using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.EndlessRunner.EditorTools
{
    /// <summary>
    /// Recipes of the scenery beside the road in the Night Shift theme's five worlds: the downtown blocks, the subway
    /// tunnel, the docks, the highway and the rooftops. Every model stands on y = 0; pieces that span the road (the
    /// tunnel arches, the overpasses) are placed at x = 0 by the world's scenery table.
    /// </summary>
    internal static class UrbanScenery
    {
        // ------------------------------------------------------------------ downtown

        /// <summary>
        /// A city block: a box of floors with rows of windows (some of them lit), a parapet, a roof box and a water
        /// tank. <paramref name="lit"/> is the share of windows with a light on.
        /// </summary>
        public static MeshBuilder Tower(int seed, float width, float depth, int floors, Swatch wall, float lit)
        {
            var m = new MeshBuilder();
            var random = new System.Random(seed);
            const float floorHeight = 3.2f;
            float height = floors * floorHeight;
            m.Color(wall).Box(new Vector3(0f, height * 0.5f, 0f), new Vector3(width, height, depth));
            m.Color(Swatch.ConcreteDark).Box(new Vector3(0f, height + 0.2f, 0f), new Vector3(width + 0.3f, 0.4f, depth + 0.3f));
            m.Color(Swatch.ConcreteDark).Box(new Vector3(0f, 0.3f, 0f), new Vector3(width + 0.4f, 0.6f, depth + 0.4f));
            Windows(m, random, width, depth, floors, floorHeight, lit);
            m.Color(Swatch.ConcreteDark).Box(new Vector3(width * 0.25f, height + 1.2f, depth * 0.1f), new Vector3(width * 0.3f, 2f, depth * 0.35f));
            if (random.NextDouble() < 0.6)
            {
                WaterTank(m, new Vector3(-width * 0.25f, height + 0.4f, -depth * 0.15f), 1.1f);
            }
            if (random.NextDouble() < 0.5)
            {
                m.Color(Swatch.NeonRed).Sphere(new Vector3(width * 0.25f, height + 2.5f, depth * 0.1f), 0.18f, 4, 8);
            }
            return m;
        }

        /// <summary>Rows of windows on all four faces of a block; a lit window is a warm box standing off the wall.</summary>
        private static void Windows(MeshBuilder m, System.Random random, float width, float depth, int floors, float floorHeight, float lit)
        {
            const float pitch = 2.2f;
            for (int floor = 0; floor < floors; floor++)
            {
                float y = floor * floorHeight + floorHeight * 0.55f;
                for (int side = -1; side <= 1; side += 2)
                {
                    int columns = Mathf.Max(1, Mathf.FloorToInt(width / pitch));
                    for (int i = 0; i < columns; i++)
                    {
                        float x = -width * 0.5f + (i + 0.5f) * width / columns;
                        bool on = random.NextDouble() < lit;
                        m.Color(on ? Swatch.WindowLit : Swatch.WindowDark).Box(new Vector3(x, y, side * (depth * 0.5f + 0.02f)), new Vector3(1.2f, 1.5f, 0.04f));
                    }
                    int rows = Mathf.Max(1, Mathf.FloorToInt(depth / pitch));
                    for (int i = 0; i < rows; i++)
                    {
                        float z = -depth * 0.5f + (i + 0.5f) * depth / rows;
                        bool on = random.NextDouble() < lit;
                        m.Color(on ? Swatch.WindowLit : Swatch.WindowDark).Box(new Vector3(side * (width * 0.5f + 0.02f), y, z), new Vector3(0.04f, 1.5f, 1.2f));
                    }
                }
            }
        }

        private static void WaterTank(MeshBuilder m, Vector3 bottom, float size)
        {
            for (int i = 0; i < 4; i++)
            {
                float angle = i * Mathf.PI * 0.5f + Mathf.PI * 0.25f;
                var foot = bottom + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * size * 0.7f;
                m.Color(Swatch.Iron).Rod(foot, foot + Vector3.up * size * 1.2f, 0.05f * size, 5, false);
            }
            m.Color(Swatch.WoodDark).Cylinder(bottom + Vector3.up * size * 1.1f, size * 0.75f, size * 0.8f, size * 1.4f, 10);
            m.Color(Swatch.SteelDark).Cylinder(bottom + Vector3.up * size * 2.5f, size * 0.85f, 0f, size * 0.5f, 10);
            m.Color(Swatch.Iron).Torus(bottom + Vector3.up * size * 1.5f, size * 0.78f, 0.03f * size, 12, 4);
            m.Color(Swatch.Iron).Torus(bottom + Vector3.up * size * 2.2f, size * 0.8f, 0.03f * size, 12, 4);
        }

        /// <summary>A two floor shop with a lit window, an awning and a neon sign box over the door.</summary>
        public static MeshBuilder Shopfront(int seed, Swatch neon)
        {
            var m = new MeshBuilder();
            var random = new System.Random(seed);
            m.Color(Swatch.Brick).Box(new Vector3(0f, 3.5f, 0f), new Vector3(7f, 7f, 6f));
            m.Color(Swatch.ConcreteDark).Box(new Vector3(0f, 7.15f, 0f), new Vector3(7.3f, 0.3f, 6.3f));
            m.Color(Swatch.WindowLit).Box(new Vector3(-1.4f, 1.5f, -3.02f), new Vector3(3f, 2f, 0.04f));
            m.Color(Swatch.SteelDark).Box(new Vector3(-1.4f, 1.5f, -3.04f), new Vector3(0.08f, 2f, 0.02f));
            m.Color(Swatch.SteelDark).Box(new Vector3(2f, 1.2f, -3.02f), new Vector3(1.2f, 2.4f, 0.04f));
            m.Color(Swatch.WindowLit).Box(new Vector3(2f, 1.6f, -3.04f), new Vector3(0.7f, 0.9f, 0.02f));
            for (int i = 0; i < 3; i++)
            {
                bool on = random.NextDouble() < 0.5;
                m.Color(on ? Swatch.WindowLit : Swatch.WindowDark).Box(new Vector3(-2.2f + i * 2.2f, 5f, -3.02f), new Vector3(1.2f, 1.5f, 0.04f));
            }
            m.Push(new Vector3(-1.4f, 2.9f, -3.5f), Quaternion.Euler(-20f, 0f, 0f));
            m.Color(random.Next(2) == 0 ? Swatch.Container1 : Swatch.Container2).Box(Vector3.zero, new Vector3(3.6f, 0.08f, 1.3f));
            m.Pop();
            m.Color(Swatch.Tar).Box(new Vector3(0f, 3.5f, -3.15f), new Vector3(4.5f, 0.9f, 0.3f));
            m.Color(neon).Box(new Vector3(0f, 3.5f, -3.32f), new Vector3(3.8f, 0.5f, 0.04f));
            m.Color(Swatch.Tar).Box(new Vector3(3.7f, 4.8f, -2.4f), new Vector3(0.4f, 2.2f, 0.9f));
            m.Color(neon).Box(new Vector3(3.7f, 4.8f, -2.4f), new Vector3(0.2f, 1.8f, 0.5f));
            return m;
        }

        /// <summary>A parked taxi beside the curb: a yellow cab with a roof sign and its lights on. Along z.</summary>
        public static MeshBuilder Taxi(int seed)
        {
            var m = new MeshBuilder();
            bool dark = new System.Random(seed).Next(3) == 0;
            Swatch body = dark ? Swatch.Iron : Swatch.TaxiYellow;
            Swatch trim = dark ? Swatch.Tar : Swatch.TaxiDark;
            m.Color(body).BeveledBox(new Vector3(0f, 0.75f, 0f), new Vector3(1.9f, 0.7f, 4.4f), 0.08f);
            m.Color(body).BeveledBox(new Vector3(0f, 1.35f, -0.2f), new Vector3(1.7f, 0.6f, 2.4f), 0.1f);
            m.Color(Swatch.Glass).Box(new Vector3(0f, 1.38f, -0.2f), new Vector3(1.72f, 0.42f, 2.2f));
            m.Color(trim).Box(new Vector3(0f, 0.95f, 0f), new Vector3(1.92f, 0.12f, 4.2f));
            foreach (float z in new[] { -1.45f, 1.45f })
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    m.Push(new Vector3(side * 0.85f, 0.36f, z), Quaternion.Euler(0f, 0f, 90f));
                    m.Color(Swatch.Tar).Cylinder(new Vector3(0f, -0.1f, 0f), 0.36f, 0.36f, 0.2f, 12);
                    m.Color(Swatch.SteelLight).Cylinder(new Vector3(0f, -0.11f, 0f), 0.18f, 0.18f, 0.22f, 8);
                    m.Pop();
                }
            }
            m.Color(Swatch.LampWhite).Box(new Vector3(-0.65f, 0.8f, 2.21f), new Vector3(0.4f, 0.2f, 0.04f));
            m.Color(Swatch.LampWhite).Box(new Vector3(0.65f, 0.8f, 2.21f), new Vector3(0.4f, 0.2f, 0.04f));
            m.Color(Swatch.NeonRed).Box(new Vector3(-0.65f, 0.8f, -2.21f), new Vector3(0.4f, 0.18f, 0.04f));
            m.Color(Swatch.NeonRed).Box(new Vector3(0.65f, 0.8f, -2.21f), new Vector3(0.4f, 0.18f, 0.04f));
            m.Color(Swatch.Tar).Box(new Vector3(0f, 1.72f, -0.2f), new Vector3(0.7f, 0.14f, 0.3f));
            m.Color(Swatch.NeonAmber).Box(new Vector3(0f, 1.76f, -0.2f), new Vector3(0.6f, 0.16f, 0.24f));
            return m;
        }

        /// <summary>A precast concrete barrier, 2 m long, along z.</summary>
        public static MeshBuilder ConcreteBarrier()
        {
            var m = new MeshBuilder();
            m.Push(Vector3.zero, Quaternion.Euler(0f, 90f, 0f));
            m.Color(Swatch.Concrete).Prism(new[]
            {
                new Vector2(-0.35f, 0f), new Vector2(0.35f, 0f), new Vector2(0.35f, 0.25f), new Vector2(0.14f, 0.55f),
                new Vector2(0.12f, 0.95f), new Vector2(-0.12f, 0.95f), new Vector2(-0.14f, 0.55f), new Vector2(-0.35f, 0.25f)
            }, 2f);
            m.Pop();
            m.Color(Swatch.ConeOrange).Box(new Vector3(0f, 0.75f, -0.7f), new Vector3(0.26f, 0.2f, 0.3f));
            m.Color(Swatch.ConeWhite).Box(new Vector3(0f, 0.75f, 0f), new Vector3(0.26f, 0.2f, 0.3f));
            m.Color(Swatch.ConeOrange).Box(new Vector3(0f, 0.75f, 0.7f), new Vector3(0.26f, 0.2f, 0.3f));
            return m;
        }

        /// <summary>A street lamp for the left side of the road, its arm over the curb (+x), with a cool white light.</summary>
        public static MeshBuilder StreetLamp(Swatch light)
        {
            var m = new MeshBuilder();
            m.Color(Swatch.ConcreteDark).Cylinder(Vector3.zero, 0.22f, 0.18f, 0.35f, 8);
            m.Color(Swatch.SteelDark).Cylinder(new Vector3(0f, 0.35f, 0f), 0.09f, 0.06f, 5.4f, 8, true);
            m.Color(Swatch.SteelDark).Rod(new Vector3(0f, 5.6f, 0f), new Vector3(1.4f, 5.9f, 0f), 0.05f, 6);
            m.Color(Swatch.SteelDark).Box(new Vector3(1.55f, 5.9f, 0f), new Vector3(0.8f, 0.14f, 0.3f));
            m.Color(light).Box(new Vector3(1.55f, 5.82f, 0f), new Vector3(0.7f, 0.04f, 0.24f));
            m.Color(Swatch.SteelDark).Box(new Vector3(0f, 2.4f, 0.1f), new Vector3(0.44f, 0.56f, 0.03f));
            m.Color(Swatch.Container2).Box(new Vector3(0f, 2.4f, 0.12f), new Vector3(0.38f, 0.5f, 0.02f));
            m.Color(Swatch.SignWhite).Box(new Vector3(-0.05f, 2.4f, 0.133f), new Vector3(0.06f, 0.3f, 0.01f));
            m.Color(Swatch.SignWhite).Box(new Vector3(0.03f, 2.5f, 0.133f), new Vector3(0.12f, 0.1f, 0.01f));
            return m;
        }

        /// <summary>A fire hydrant.</summary>
        public static MeshBuilder Hydrant()
        {
            var m = new MeshBuilder();
            m.Color(Swatch.ConcreteDark).Cylinder(Vector3.zero, 0.2f, 0.16f, 0.15f, 8);
            m.Color(Swatch.Container1).Cylinder(new Vector3(0f, 0.15f, 0f), 0.14f, 0.13f, 0.65f, 8, true);
            m.Color(Swatch.Container1).Sphere(new Vector3(0f, 0.82f, 0f), new Vector3(0.15f, 0.12f, 0.15f), 5, 8);
            m.Color(Swatch.SteelLight).Cylinder(new Vector3(0f, 0.86f, 0f), 0.05f, 0.03f, 0.1f, 6);
            m.Color(Swatch.SteelLight).Rod(new Vector3(-0.22f, 0.5f, 0f), new Vector3(0.22f, 0.5f, 0f), 0.06f, 6, false);
            return m;
        }

        /// <summary>A pair of trash cans and a stack of boxes against a wall.</summary>
        public static MeshBuilder TrashCans(int seed)
        {
            var m = new MeshBuilder();
            var random = new System.Random(seed);
            for (int i = 0; i < 2; i++)
            {
                var root = new Vector3(i * 0.85f - 0.4f, 0f, ((float)random.NextDouble() - 0.5f) * 0.3f);
                m.Color(Swatch.SteelDark).Cylinder(root, 0.34f, 0.32f, 0.9f, 10, true);
                m.Color(Swatch.SteelLight).Cylinder(root + Vector3.up * 0.9f, 0.37f, 0.3f, 0.1f, 10);
                m.Color(Swatch.SteelDark).Cylinder(root + Vector3.up * 0.45f, 0.35f, 0.35f, 0.05f, 10, true);
            }
            m.Color(Swatch.Tar).Blob(new Vector3(0.9f, 0.3f, 0.2f), new Vector3(0.4f, 0.3f, 0.35f), 0, 0.2f, seed);
            m.Color(Swatch.WoodLight).BeveledBox(new Vector3(-1.1f, 0.3f, 0.1f), new Vector3(0.7f, 0.6f, 0.6f), 0.02f);
            return m;
        }

        /// <summary>A billboard on two posts, lit from below, its face toward -z.</summary>
        public static MeshBuilder Billboard(int seed)
        {
            var m = new MeshBuilder();
            var random = new System.Random(seed);
            m.Push(Vector3.zero, Quaternion.Euler(0f, -90f, 0f));
            Swatch[] paints = { Swatch.Container1, Swatch.Container2, Swatch.TileGreen, Swatch.Tarp };
            m.Color(Swatch.SteelDark).Cylinder(new Vector3(0f, 0f, -2f), 0.12f, 0.1f, 5f, 8, true);
            m.Color(Swatch.SteelDark).Cylinder(new Vector3(0f, 0f, 2f), 0.12f, 0.1f, 5f, 8, true);
            m.Color(Swatch.Iron).Box(new Vector3(0f, 6.5f, 0f), new Vector3(0.3f, 3.4f, 6.2f));
            m.Color(paints[random.Next(paints.Length)]).Box(new Vector3(-0.17f, 6.5f, 0f), new Vector3(0.04f, 3f, 5.8f));
            m.Color(Swatch.SignWhite).Box(new Vector3(-0.2f, 6.9f, -0.6f), new Vector3(0.02f, 0.7f, 3.4f));
            m.Color(Swatch.SignWhite).Box(new Vector3(-0.2f, 6f, 0.4f), new Vector3(0.02f, 0.4f, 2.2f));
            m.Color(Swatch.SteelDark).Box(new Vector3(-0.5f, 4.7f, 0f), new Vector3(0.7f, 0.08f, 5.6f));
            m.Color(Swatch.LampWhite).Box(new Vector3(-0.5f, 4.74f, 0f), new Vector3(0.5f, 0.02f, 5f));
            m.Pop();
            return m;
        }

        // ------------------------------------------------------------------ subway

        /// <summary>A 6 m stretch of tiled tunnel wall with a pillar, a cable tray and a service light, for the left side.</summary>
        public static MeshBuilder TunnelWall(float length)
        {
            var m = new MeshBuilder();
            m.Submesh(0).Color(Swatch.ConcreteDark).Box(new Vector3(0f, 3f, length * 0.5f), new Vector3(0.6f, 6f, length));
            m.Submesh(0).Color(Swatch.TileWhite).Box(new Vector3(0.31f, 1.6f, length * 0.5f), new Vector3(0.02f, 2.6f, length));
            for (float z = 0.4f; z < length; z += 0.8f)
            {
                for (float y = 0.4f; y < 2.9f; y += 0.5f)
                {
                    // White glazed tiles with one green band at shoulder height, as old stations have them.
                    m.Color(y > 1.3f && y < 1.5f ? Swatch.TileGreen : Swatch.TileWhite).Box(new Vector3(0.325f, y, z), new Vector3(0.01f, 0.44f, 0.7f));
                }
            }
            m.Color(Swatch.TileGreen).Box(new Vector3(0.32f, 2.95f, length * 0.5f), new Vector3(0.02f, 0.2f, length));
            m.Color(Swatch.SteelDark).Box(new Vector3(0.38f, 4.6f, length * 0.5f), new Vector3(0.14f, 0.14f, length * 0.9f));
            m.Color(Swatch.LampWhite).Box(new Vector3(0.44f, 4.56f, length * 0.5f), new Vector3(0.03f, 0.06f, length * 0.85f));
            m.Color(Swatch.Iron).Box(new Vector3(0.25f, 5.2f, length * 0.5f), new Vector3(0.5f, 0.3f, length));
            m.Color(Swatch.Cable).Rod(new Vector3(0.45f, 4.1f, 0f), new Vector3(0.45f, 3.85f, length * 0.5f), 0.04f, 5, false);
            m.Color(Swatch.Cable).Rod(new Vector3(0.45f, 3.85f, length * 0.5f), new Vector3(0.45f, 4.1f, length), 0.04f, 5, false);
            m.Color(Swatch.Cable).Rod(new Vector3(0.5f, 4.3f, 0f), new Vector3(0.5f, 4.05f, length * 0.5f), 0.03f, 5, false);
            m.Color(Swatch.Cable).Rod(new Vector3(0.5f, 4.05f, length * 0.5f), new Vector3(0.5f, 4.3f, length), 0.03f, 5, false);
            m.Color(Swatch.SteelDark).Box(new Vector3(0.36f, 3.4f, length * 0.5f), new Vector3(0.12f, 0.4f, 0.24f));
            m.Color(Swatch.NeonAmber).Box(new Vector3(0.43f, 3.4f, length * 0.5f), new Vector3(0.02f, 0.3f, 0.16f));
            m.Color(Swatch.ConcreteDark).Box(new Vector3(0.45f, 3f, 0f), new Vector3(0.4f, 6f, 0.5f));
            return m;
        }

        /// <summary>A concrete portal spanning the road, 12 m wide and 6.5 m high, with a row of lamps under it.</summary>
        public static MeshBuilder TunnelArch()
        {
            var m = new MeshBuilder();
            const float halfWidth = 6.2f;
            const float lintel = 9f;
            for (int side = -1; side <= 1; side += 2)
            {
                m.Color(Swatch.ConcreteDark).Box(new Vector3(side * halfWidth, lintel * 0.5f - 0.3f, 0f), new Vector3(1.2f, lintel - 0.6f, 1.2f));
                m.Color(Swatch.TileGreen).Box(new Vector3(side * (halfWidth - 0.61f), 1.2f, 0f), new Vector3(0.02f, 2.4f, 1f));
                m.Color(Swatch.NeonAmber).Box(new Vector3(side * (halfWidth - 0.62f), 5.2f, 0f), new Vector3(0.02f, 0.3f, 0.5f));
            }
            m.Color(Swatch.ConcreteDark).Box(new Vector3(0f, lintel, 0f), new Vector3(halfWidth * 2f + 1.2f, 1.4f, 1.4f));
            m.Color(Swatch.Concrete).Box(new Vector3(0f, lintel - 0.68f, 0f), new Vector3(halfWidth * 2f, 0.06f, 1.2f));
            for (float x = -4.5f; x <= 4.6f; x += 3f)
            {
                m.Color(Swatch.SteelDark).Box(new Vector3(x, lintel - 0.8f, 0f), new Vector3(1.2f, 0.16f, 0.3f));
                m.Color(Swatch.LampWhite).Box(new Vector3(x, lintel - 0.9f, 0f), new Vector3(1.1f, 0.04f, 0.24f));
            }
            m.Color(Swatch.Cable).Rod(new Vector3(-halfWidth, lintel - 1f, 0.5f), new Vector3(halfWidth, lintel - 1f, 0.5f), 0.04f, 5, false);
            m.Color(Swatch.SignWhite).Box(new Vector3(0f, lintel, -0.71f), new Vector3(2.4f, 0.6f, 0.02f));
            m.Color(Swatch.TileGreen).Box(new Vector3(0f, lintel, -0.73f), new Vector3(2.1f, 0.14f, 0.01f));
            return m;
        }

        /// <summary>A round steel pillar with a base plate and a hazard band.</summary>
        public static MeshBuilder Pillar(int seed)
        {
            var m = new MeshBuilder();
            float height = 5.5f + (seed % 3) * 0.5f;
            m.Color(Swatch.ConcreteDark).Cylinder(Vector3.zero, 0.5f, 0.42f, 0.3f, 8);
            m.Color(Swatch.SteelDark).Cylinder(new Vector3(0f, 0.3f, 0f), 0.3f, 0.3f, height, 10, true);
            m.Color(Swatch.NeonAmber).Cylinder(new Vector3(0f, 1.2f, 0f), 0.31f, 0.31f, 0.35f, 10, true);
            m.Color(Swatch.Tar).Cylinder(new Vector3(0f, 1.55f, 0f), 0.31f, 0.31f, 0.35f, 10, true);
            m.Color(Swatch.NeonAmber).Cylinder(new Vector3(0f, 1.9f, 0f), 0.31f, 0.31f, 0.35f, 10, true);
            m.Color(Swatch.SteelDark).Box(new Vector3(0f, height + 0.45f, 0f), new Vector3(1.4f, 0.3f, 1.4f));
            return m;
        }

        /// <summary>A signal on a post: red, amber and green lamps, one of them lit.</summary>
        public static MeshBuilder Signal(int seed)
        {
            var m = new MeshBuilder();
            int lit = seed % 3;
            m.Color(Swatch.SteelDark).Cylinder(Vector3.zero, 0.15f, 0.12f, 0.1f, 8);
            m.Color(Swatch.SteelDark).Cylinder(new Vector3(0f, 0.1f, 0f), 0.05f, 0.05f, 2.2f, 6, true);
            m.Color(Swatch.Tar).Box(new Vector3(0f, 2.75f, 0f), new Vector3(0.36f, 1.1f, 0.3f));
            Swatch[] lamps = { Swatch.NeonRed, Swatch.NeonAmber, Swatch.NeonGreen };
            for (int i = 0; i < 3; i++)
            {
                m.Color(i == lit ? lamps[i] : Swatch.Iron).Sphere(new Vector3(0f, 3.05f - i * 0.32f, -0.16f), new Vector3(0.11f, 0.11f, 0.06f), 5, 8);
                m.Color(Swatch.Tar).Box(new Vector3(0f, 3.15f - i * 0.32f, -0.2f), new Vector3(0.3f, 0.03f, 0.12f));
            }
            return m;
        }

        /// <summary>A bundle of cables and junction boxes hung along the wall.</summary>
        public static MeshBuilder CableRun(float length)
        {
            var m = new MeshBuilder();
            for (int i = 0; i < 3; i++)
            {
                float y = 3.6f + i * 0.22f;
                var path = new List<Vector3>();
                for (int k = 0; k <= 8; k++)
                {
                    float t = k / 8f;
                    path.Add(new Vector3(0f, y - Mathf.Sin(t * Mathf.PI) * 0.25f, t * length));
                }
                m.Color(Swatch.Cable).Tube(path, 0.035f, 5);
            }
            m.Color(Swatch.SteelDark).Box(new Vector3(0f, 3.8f, 0f), new Vector3(0.3f, 0.7f, 0.3f));
            m.Color(Swatch.SteelDark).Box(new Vector3(0f, 3.8f, length), new Vector3(0.3f, 0.7f, 0.3f));
            m.Color(Swatch.NeonGreen).Sphere(new Vector3(0f, 3.9f, 0.16f), 0.04f, 4, 6);
            return m;
        }

        // ------------------------------------------------------------------ docks

        /// <summary>A shipping container, 6 m long along z, with corrugated sides.</summary>
        public static MeshBuilder Container(Swatch paint, float length = 6f)
        {
            var m = new MeshBuilder();
            m.Color(paint).Box(new Vector3(0f, 1.3f, 0f), new Vector3(2.4f, 2.6f, length));
            for (int side = -1; side <= 1; side += 2)
            {
                for (float z = -length * 0.5f + 0.4f; z < length * 0.5f - 0.3f; z += 0.5f)
                {
                    m.Color(paint).Box(new Vector3(side * 1.22f, 1.3f, z), new Vector3(0.05f, 2.3f, 0.2f));
                }
                m.Color(Swatch.ContainerDark).Box(new Vector3(side * 1.21f, 0.12f, 0f), new Vector3(0.04f, 0.2f, length));
                m.Color(Swatch.ContainerDark).Box(new Vector3(side * 1.21f, 2.5f, 0f), new Vector3(0.04f, 0.16f, length));
                m.Color(Swatch.SignWhite).Box(new Vector3(side * 1.25f, 2.0f, -length * 0.25f), new Vector3(0.01f, 0.3f, 1.4f));
            }
            m.Color(Swatch.ContainerDark).Box(new Vector3(-0.62f, 1.3f, -length * 0.5f - 0.02f), new Vector3(1.16f, 2.4f, 0.06f));
            m.Color(Swatch.ContainerDark).Box(new Vector3(0.62f, 1.3f, -length * 0.5f - 0.02f), new Vector3(1.16f, 2.4f, 0.06f));
            m.Color(Swatch.SteelLight).Box(new Vector3(-0.3f, 1.3f, -length * 0.5f - 0.07f), new Vector3(0.06f, 2.2f, 0.06f));
            m.Color(Swatch.SteelLight).Box(new Vector3(0.3f, 1.3f, -length * 0.5f - 0.07f), new Vector3(0.06f, 2.2f, 0.06f));
            foreach (int cx in new[] { -1, 1 })
            {
                foreach (int cz in new[] { -1, 1 })
                {
                    m.Color(Swatch.ContainerDark).Box(new Vector3(cx * 1.18f, 0.12f, cz * (length * 0.5f - 0.12f)), new Vector3(0.26f, 0.24f, 0.26f));
                    m.Color(Swatch.ContainerDark).Box(new Vector3(cx * 1.18f, 2.48f, cz * (length * 0.5f - 0.12f)), new Vector3(0.26f, 0.24f, 0.26f));
                }
            }
            return m;
        }

        /// <summary>A stack of two or three containers, the top one a little off.</summary>
        public static MeshBuilder ContainerStack(int seed)
        {
            var m = new MeshBuilder();
            var random = new System.Random(seed);
            Swatch[] paints = { Swatch.Container1, Swatch.Container2, Swatch.Container3, Swatch.Container4 };
            int count = 2 + random.Next(2);
            for (int i = 0; i < count; i++)
            {
                m.Push(new Vector3(((float)random.NextDouble() - 0.5f) * 0.3f, i * 2.62f, ((float)random.NextDouble() - 0.5f) * 0.8f), Quaternion.Euler(0f, ((float)random.NextDouble() - 0.5f) * 4f, 0f));
                MeshBuilder box = Container(paints[random.Next(paints.Length)]);
                Append(m, box);
                m.Pop();
            }
            return m;
        }

        /// <summary>A gantry crane far off the road: two legs, a beam over them and a hanging spreader. Along z.</summary>
        public static MeshBuilder Crane()
        {
            var m = new MeshBuilder();
            const float height = 22f;
            for (int side = -1; side <= 1; side += 2)
            {
                for (int leg = -1; leg <= 1; leg += 2)
                {
                    m.Color(Swatch.Container2).Box(new Vector3(side * 7f, height * 0.5f, leg * 4f), new Vector3(0.9f, height, 0.9f));
                }
                m.Color(Swatch.Container2).Box(new Vector3(side * 7f, height - 1f, 0f), new Vector3(1.2f, 2f, 9.5f));
                m.Color(Swatch.Container2).Box(new Vector3(side * 7f, 1f, 0f), new Vector3(1.4f, 2f, 10f));
                m.Color(Swatch.Iron).Rod(new Vector3(side * 7f, 2f, -4f), new Vector3(side * 7f, height - 2f, 4f), 0.2f, 5, false);
            }
            m.Color(Swatch.Container2).Box(new Vector3(0f, height, 0f), new Vector3(30f, 1.8f, 2f));
            m.Color(Swatch.Tar).Box(new Vector3(0f, height + 1.2f, 0f), new Vector3(30f, 0.6f, 2.4f));
            m.Color(Swatch.SteelDark).Box(new Vector3(9f, height + 2.2f, 0f), new Vector3(3f, 2.4f, 2.6f));
            m.Color(Swatch.WindowLit).Box(new Vector3(9f, height + 2.4f, -1.31f), new Vector3(2.4f, 1.2f, 0.04f));
            m.Color(Swatch.Iron).Rod(new Vector3(-3f, height - 0.9f, 0f), new Vector3(-3f, height - 8f, 0f), 0.08f, 4, false);
            m.Color(Swatch.Iron).Rod(new Vector3(-1f, height - 0.9f, 0f), new Vector3(-1f, height - 8f, 0f), 0.08f, 4, false);
            m.Color(Swatch.TaxiYellow).Box(new Vector3(-2f, height - 8.5f, 0f), new Vector3(3.5f, 1f, 2.2f));
            m.Color(Swatch.NeonRed).Sphere(new Vector3(-15f, height + 2f, 0f), 0.35f, 4, 8);
            m.Color(Swatch.NeonRed).Sphere(new Vector3(15f, height + 2f, 0f), 0.35f, 4, 8);
            return m;
        }

        /// <summary>A pallet of wooden crates under a tarp.</summary>
        public static MeshBuilder CratePile(int seed)
        {
            var m = new MeshBuilder();
            var random = new System.Random(seed);
            m.Color(Swatch.WoodDark).Box(new Vector3(0f, 0.08f, 0f), new Vector3(2.2f, 0.16f, 2.2f));
            m.Color(Swatch.Wood).BeveledBox(new Vector3(-0.5f, 0.68f, -0.4f), new Vector3(1f, 1f, 1.1f), 0.02f);
            m.Color(Swatch.WoodLight).BeveledBox(new Vector3(0.55f, 0.58f, 0.3f), new Vector3(0.9f, 0.8f, 1.3f), 0.02f);
            m.Color(Swatch.Wood).BeveledBox(new Vector3(-0.4f, 1.55f, 0.1f), new Vector3(0.9f, 0.7f, 0.9f), 0.02f);
            if (random.Next(2) == 0)
            {
                m.Color(Swatch.Tarp).BeveledBox(new Vector3(0.1f, 1.62f, 0.05f), new Vector3(2f, 0.9f, 1.9f), 0.15f);
            }
            m.Color(Swatch.BagStrap).Box(new Vector3(0f, 1f, 0f), new Vector3(2.3f, 0.06f, 0.08f));
            return m;
        }

        /// <summary>A cast iron mooring bollard.</summary>
        public static MeshBuilder Bollard()
        {
            var m = new MeshBuilder();
            m.Color(Swatch.Iron).Cylinder(Vector3.zero, 0.34f, 0.28f, 0.2f, 10);
            m.Color(Swatch.Iron).Cylinder(new Vector3(0f, 0.2f, 0f), 0.24f, 0.28f, 0.6f, 10, true);
            m.Color(Swatch.Iron).Sphere(new Vector3(0f, 0.85f, 0f), new Vector3(0.32f, 0.16f, 0.32f), 5, 10);
            return m;
        }

        /// <summary>A floodlight mast for the left side of the road, its lamps toward +x.</summary>
        public static MeshBuilder FloodlightMast()
        {
            var m = new MeshBuilder();
            m.Color(Swatch.ConcreteDark).Box(new Vector3(0f, 0.25f, 0f), new Vector3(1f, 0.5f, 1f));
            m.Color(Swatch.SteelDark).Cylinder(new Vector3(0f, 0.5f, 0f), 0.16f, 0.1f, 9f, 8, true);
            m.Color(Swatch.SteelDark).Box(new Vector3(0.3f, 9.4f, 0f), new Vector3(1.2f, 0.2f, 1.6f));
            for (int i = 0; i < 3; i++)
            {
                m.Color(Swatch.Tar).Box(new Vector3(0.6f, 9.7f, (i - 1) * 0.55f), new Vector3(0.5f, 0.4f, 0.45f));
                m.Color(Swatch.LampWhite).Box(new Vector3(0.86f, 9.7f, (i - 1) * 0.55f), new Vector3(0.02f, 0.34f, 0.38f));
            }
            m.Color(Swatch.NeonRed).Sphere(new Vector3(0f, 9.75f, 0f), 0.09f, 4, 8);
            return m;
        }

        // ------------------------------------------------------------------ highway

        /// <summary>A 6 m guard rail on posts along z, for the left side of the road, with a reflector.</summary>
        public static MeshBuilder GuardRail(float length)
        {
            var m = new MeshBuilder();
            for (float z = 0f; z <= length + 0.01f; z += length / 3f)
            {
                m.Color(Swatch.SteelDark).Box(new Vector3(0f, 0.35f, Mathf.Min(z, length - 0.06f)), new Vector3(0.1f, 0.7f, 0.12f));
            }
            m.Color(Swatch.SteelLight).Box(new Vector3(0.08f, 0.62f, length * 0.5f), new Vector3(0.06f, 0.16f, length));
            m.Color(Swatch.Steel).Box(new Vector3(0.1f, 0.5f, length * 0.5f), new Vector3(0.04f, 0.08f, length));
            m.Color(Swatch.SteelLight).Box(new Vector3(0.08f, 0.38f, length * 0.5f), new Vector3(0.06f, 0.16f, length));
            m.Color(Swatch.NeonAmber).Box(new Vector3(0.12f, 0.5f, length * 0.5f), new Vector3(0.02f, 0.08f, 0.16f));
            return m;
        }

        /// <summary>A traffic cone.</summary>
        public static MeshBuilder Cone()
        {
            var m = new MeshBuilder();
            m.Color(Swatch.Tar).Box(new Vector3(0f, 0.03f, 0f), new Vector3(0.5f, 0.06f, 0.5f));
            m.Color(Swatch.ConeOrange).Cylinder(new Vector3(0f, 0.06f, 0f), 0.18f, 0.04f, 0.7f, 8, true);
            m.Color(Swatch.ConeWhite).Cylinder(new Vector3(0f, 0.34f, 0f), 0.125f, 0.1f, 0.12f, 8, true);
            return m;
        }

        /// <summary>A pair of cones with a striped bar between them.</summary>
        public static MeshBuilder ConeRow()
        {
            var m = new MeshBuilder();
            for (int i = 0; i < 2; i++)
            {
                m.Push(new Vector3(0f, 0f, i * 1.6f - 0.8f));
                Append(m, Cone());
                m.Pop();
            }
            for (int i = 0; i < 6; i++)
            {
                m.Color(i % 2 == 0 ? Swatch.ConeOrange : Swatch.ConeWhite).Box(new Vector3(0f, 0.5f, -0.75f + (i + 0.5f) * 0.25f), new Vector3(0.05f, 0.08f, 0.25f));
            }
            return m;
        }

        /// <summary>A green highway sign on two posts, its face toward -z, the road side.</summary>
        public static MeshBuilder RoadSign(int seed)
        {
            var m = new MeshBuilder();
            var random = new System.Random(seed);
            m.Color(Swatch.SteelDark).Cylinder(new Vector3(-1.2f, 0f, 0f), 0.08f, 0.07f, 3.4f, 6, true);
            m.Color(Swatch.SteelDark).Cylinder(new Vector3(1.2f, 0f, 0f), 0.08f, 0.07f, 3.4f, 6, true);
            m.Color(Swatch.SignGreen).Box(new Vector3(0f, 3.9f, 0f), new Vector3(3.4f, 1.6f, 0.08f));
            m.Color(Swatch.SignWhite).Box(new Vector3(0f, 3.9f, -0.05f), new Vector3(3.3f, 1.5f, 0.02f));
            m.Color(Swatch.SignGreen).Box(new Vector3(0f, 3.9f, -0.07f), new Vector3(3.2f, 1.4f, 0.02f));
            int lines = 1 + random.Next(2);
            for (int i = 0; i < lines; i++)
            {
                float width = 1.2f + (float)random.NextDouble() * 1.4f;
                m.Color(Swatch.SignWhite).Box(new Vector3(-1.5f + width * 0.5f, 4.2f - i * 0.5f, -0.09f), new Vector3(width, 0.24f, 0.02f));
            }
            m.Color(Swatch.SignWhite).Box(new Vector3(1.1f, 3.5f, -0.09f), new Vector3(0.5f, 0.24f, 0.02f));
            return m;
        }

        /// <summary>A concrete overpass crossing the road 7 m up: two piers and a deck with a parapet and lamps.</summary>
        public static MeshBuilder Overpass()
        {
            var m = new MeshBuilder();
            const float halfSpan = 9f;
            const float deck = 9.2f;
            for (int side = -1; side <= 1; side += 2)
            {
                m.Color(Swatch.ConcreteDark).Box(new Vector3(side * 6.5f, (deck - 0.6f) * 0.5f, 0f), new Vector3(1.4f, deck - 0.6f, 2.6f));
                m.Color(Swatch.Concrete).Box(new Vector3(side * 6.5f, 0.3f, 0f), new Vector3(2.2f, 0.6f, 3.4f));
            }
            m.Color(Swatch.Concrete).Box(new Vector3(0f, deck, 0f), new Vector3(halfSpan * 2f, 1.2f, 4f));
            m.Color(Swatch.ConcreteDark).Box(new Vector3(0f, deck - 0.65f, 0f), new Vector3(halfSpan * 2f - 2f, 0.1f, 3.6f));
            m.Color(Swatch.ConcreteLight).Box(new Vector3(0f, deck + 1f, -1.9f), new Vector3(halfSpan * 2f, 0.9f, 0.25f));
            m.Color(Swatch.ConcreteLight).Box(new Vector3(0f, deck + 1f, 1.9f), new Vector3(halfSpan * 2f, 0.9f, 0.25f));
            for (float x = -6f; x <= 6.1f; x += 6f)
            {
                m.Color(Swatch.SteelDark).Cylinder(new Vector3(x, deck + 1.4f, 1.9f), 0.06f, 0.05f, 3f, 6, true);
                m.Color(Swatch.NeonAmber).Sphere(new Vector3(x, deck + 4.4f, 1.9f), new Vector3(0.3f, 0.14f, 0.3f), 4, 8);
            }
            for (float x = -halfSpan + 1.5f; x < halfSpan; x += 3f)
            {
                m.Color(Swatch.LampWhite).Box(new Vector3(x, deck - 0.68f, 0f), new Vector3(1f, 0.04f, 0.3f));
            }
            m.Color(Swatch.SignGreen).Box(new Vector3(2.5f, deck - 1.6f, -1.5f), new Vector3(3f, 1.2f, 0.1f));
            m.Color(Swatch.SignWhite).Box(new Vector3(2.5f, deck - 1.45f, -1.56f), new Vector3(2.2f, 0.24f, 0.02f));
            m.Color(Swatch.SignWhite).Box(new Vector3(2.2f, deck - 1.85f, -1.56f), new Vector3(1.4f, 0.2f, 0.02f));
            return m;
        }

        /// <summary>A tall highway light: a mast with its lamp on an arm toward -z (turned over the road where it stands).</summary>
        public static MeshBuilder HighwayLight()
        {
            var m = new MeshBuilder();
            m.Push(Vector3.zero, Quaternion.Euler(0f, 90f, 0f));
            m.Color(Swatch.ConcreteDark).Cylinder(Vector3.zero, 0.3f, 0.24f, 0.4f, 8);
            m.Color(Swatch.SteelLight).Cylinder(new Vector3(0f, 0.4f, 0f), 0.12f, 0.07f, 8.6f, 8, true);
            m.Color(Swatch.SteelLight).Rod(new Vector3(0f, 8.8f, 0f), new Vector3(2.4f, 9.4f, 0f), 0.06f, 6);
            m.Color(Swatch.SteelDark).Box(new Vector3(2.7f, 9.4f, 0f), new Vector3(1f, 0.18f, 0.4f));
            m.Color(Swatch.NeonAmber).Box(new Vector3(2.7f, 9.3f, 0f), new Vector3(0.9f, 0.04f, 0.32f));
            m.Pop();
            return m;
        }

        /// <summary>A wrecked car on the shoulder, along z, a wheel gone.</summary>
        public static MeshBuilder Wreck(int seed)
        {
            var m = new MeshBuilder();
            Swatch paint = seed % 2 == 0 ? Swatch.Concrete : Swatch.Container1;
            m.Push(new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, 0f, -6f));
            m.Color(paint).BeveledBox(new Vector3(0f, 0.6f, 0f), new Vector3(1.8f, 0.6f, 4.2f), 0.08f);
            m.Color(Swatch.Rust).BeveledBox(new Vector3(0.2f, 1.15f, -0.1f), new Vector3(1.5f, 0.55f, 2.2f), 0.1f);
            m.Color(Swatch.WindowDark).Box(new Vector3(0.2f, 1.18f, -0.1f), new Vector3(1.52f, 0.36f, 2f));
            m.Pop();
            foreach ((float x, float z) in new[] { (-0.85f, -1.4f), (0.85f, -1.4f), (0.85f, 1.4f) })
            {
                m.Push(new Vector3(x, 0.32f, z), Quaternion.Euler(0f, 0f, 90f));
                m.Color(Swatch.Tar).Cylinder(new Vector3(0f, -0.1f, 0f), 0.32f, 0.32f, 0.2f, 10);
                m.Pop();
            }
            m.Color(Swatch.Rust).Box(new Vector3(-0.85f, 0.15f, 1.4f), new Vector3(0.2f, 0.3f, 0.5f));
            return m;
        }

        // ------------------------------------------------------------------ rooftops

        /// <summary>An air conditioning unit with a fan grille.</summary>
        public static MeshBuilder AcUnit(int seed)
        {
            var m = new MeshBuilder();
            bool twin = seed % 2 == 0;
            float width = twin ? 2.6f : 1.4f;
            m.Color(Swatch.SteelDark).Box(new Vector3(0f, 0.1f, 0f), new Vector3(width + 0.2f, 0.2f, 1.6f));
            m.Color(Swatch.SteelLight).BeveledBox(new Vector3(0f, 0.75f, 0f), new Vector3(width, 1.1f, 1.4f), 0.05f);
            for (int i = 0; i < (twin ? 2 : 1); i++)
            {
                float x = twin ? (i - 0.5f) * 1.3f : 0f;
                m.Color(Swatch.Tar).Cylinder(new Vector3(x, 1.3f, 0f), 0.5f, 0.5f, 0.04f, 14);
                m.Color(Swatch.SteelLight).Torus(new Vector3(x, 1.33f, 0f), 0.5f, 0.03f, 14, 4);
                for (int blade = 0; blade < 4; blade++)
                {
                    m.Push(new Vector3(x, 1.34f, 0f), Quaternion.Euler(0f, blade * 45f, 0f));
                    m.Color(Swatch.SteelLight).Box(Vector3.zero, new Vector3(0.9f, 0.02f, 0.06f));
                    m.Pop();
                }
            }
            for (int i = 0; i < 6; i++)
            {
                m.Color(Swatch.SteelDark).Box(new Vector3(0f, 0.35f + i * 0.14f, -0.71f), new Vector3(width - 0.2f, 0.04f, 0.02f));
            }
            m.Color(Swatch.Cable).Rod(new Vector3(width * 0.5f, 0.4f, 0.3f), new Vector3(width * 0.5f + 0.6f, 0.05f, 0.5f), 0.03f, 4, false);
            return m;
        }

        /// <summary>A rooftop water tower on a steel stand.</summary>
        public static MeshBuilder WaterTower()
        {
            var m = new MeshBuilder();
            WaterTank(m, Vector3.zero, 1.6f);
            m.Color(Swatch.Iron).Box(new Vector3(0f, 0.05f, 0f), new Vector3(2.8f, 0.1f, 2.8f));
            return m;
        }

        /// <summary>A mast with antennas and a blinking light.</summary>
        public static MeshBuilder Antenna(int seed)
        {
            var m = new MeshBuilder();
            float height = 4f + (seed % 3) * 1.2f;
            m.Color(Swatch.ConcreteDark).Box(new Vector3(0f, 0.15f, 0f), new Vector3(0.8f, 0.3f, 0.8f));
            m.Color(Swatch.SteelDark).Cylinder(new Vector3(0f, 0.3f, 0f), 0.07f, 0.04f, height, 6, true);
            for (int i = 0; i < 3; i++)
            {
                float y = height * (0.5f + i * 0.17f);
                float angle = i * 120f + seed * 20f;
                m.Push(new Vector3(0f, y, 0f), Quaternion.Euler(0f, angle, 0f));
                m.Color(Swatch.SteelLight).Box(new Vector3(0.45f, 0f, 0f), new Vector3(0.9f, 0.03f, 0.03f));
                m.Color(Swatch.SteelLight).Box(new Vector3(0.8f, 0f, 0f), new Vector3(0.03f, 0.5f, 0.03f));
                m.Pop();
            }
            m.Color(Swatch.SteelDark).Box(new Vector3(0.3f, height * 0.35f, 0f), new Vector3(0.6f, 0.9f, 0.1f));
            m.Color(Swatch.NeonRed).Sphere(new Vector3(0f, height + 0.4f, 0f), 0.09f, 4, 8);
            for (int i = 0; i < 3; i++)
            {
                float angle = i * Mathf.PI * 2f / 3f;
                m.Color(Swatch.Cable).Rod(new Vector3(0f, height * 0.8f, 0f), new Vector3(Mathf.Cos(angle) * 1.6f, 0.05f, Mathf.Sin(angle) * 1.6f), 0.015f, 3, false);
            }
            return m;
        }

        /// <summary>A pitched skylight with a lit interior, along z.</summary>
        public static MeshBuilder Skylight()
        {
            var m = new MeshBuilder();
            m.Color(Swatch.ConcreteDark).Box(new Vector3(0f, 0.25f, 0f), new Vector3(2f, 0.5f, 3.2f));
            m.Push(new Vector3(0f, 0.5f, 0f), Quaternion.Euler(0f, 90f, 0f));
            m.Color(Swatch.SteelDark).Prism(new[] { new Vector2(-1.55f, 0f), new Vector2(1.55f, 0f), new Vector2(0f, 0.9f) }, 1.9f);
            m.Color(Swatch.WindowLit).Prism(new[] { new Vector2(-1.45f, 0.02f), new Vector2(1.45f, 0.02f), new Vector2(0f, 0.86f) }, 1.96f);
            m.Pop();
            for (float z = -1.2f; z <= 1.21f; z += 0.6f)
            {
                m.Color(Swatch.SteelDark).Box(new Vector3(0f, 0.96f, z), new Vector3(2.1f, 0.05f, 0.05f));
            }
            return m;
        }

        /// <summary>A roof vent: a curved pipe with a hood.</summary>
        public static MeshBuilder Vent()
        {
            var m = new MeshBuilder();
            m.Color(Swatch.ConcreteDark).Cylinder(Vector3.zero, 0.35f, 0.3f, 0.2f, 8);
            m.Color(Swatch.SteelLight).Cylinder(new Vector3(0f, 0.2f, 0f), 0.2f, 0.2f, 1.1f, 10, true);
            var bend = new List<Vector3>();
            for (int i = 0; i <= 6; i++)
            {
                float t = i / 6f * Mathf.PI * 0.5f;
                bend.Add(new Vector3(Mathf.Sin(t) * 0.35f, 1.3f + Mathf.Sin(t) * 0.0f - (1f - Mathf.Cos(t)) * 0.35f + 0.35f * (1f - Mathf.Cos(t)) * 0f, 0f) + new Vector3(0f, (1f - Mathf.Cos(t)) * 0.35f, 0f));
            }
            m.Color(Swatch.SteelLight).Tube(bend, 0.2f, 8);
            m.Color(Swatch.SteelDark).Cylinder(new Vector3(0.36f, 1.55f, 0f), 0.26f, 0.32f, 0.14f, 10);
            return m;
        }

        /// <summary>A low parapet wall along z with a coping, for the left side of the road.</summary>
        public static MeshBuilder Parapet(float length)
        {
            var m = new MeshBuilder();
            m.Color(Swatch.Brick).Box(new Vector3(0f, 0.45f, length * 0.5f), new Vector3(0.36f, 0.9f, length));
            m.Color(Swatch.ConcreteLight).Box(new Vector3(0f, 0.94f, length * 0.5f), new Vector3(0.46f, 0.08f, length));
            for (float z = 0.3f; z < length; z += 0.6f)
            {
                m.Color(Swatch.BrickDark).Box(new Vector3(0.181f, 0.3f, z), new Vector3(0.01f, 0.02f, 0.6f));
                m.Color(Swatch.BrickDark).Box(new Vector3(0.181f, 0.6f, z), new Vector3(0.01f, 0.02f, 0.6f));
                m.Color(Swatch.BrickDark).Box(new Vector3(-0.181f, 0.45f, z), new Vector3(0.01f, 0.02f, 0.6f));
            }
            m.Color(Swatch.SteelDark).Box(new Vector3(-0.1f, 0.25f, 0.6f), new Vector3(0.14f, 0.5f, 0.14f));
            m.Color(Swatch.NeonAmber).Sphere(new Vector3(-0.1f, 0.55f, 0.6f), 0.06f, 4, 6);
            return m;
        }

        /// <summary>A distant skyscraper: a tall, narrow block of lit windows with a spire.</summary>
        public static MeshBuilder Skyscraper(int seed)
        {
            var m = new MeshBuilder();
            var random = new System.Random(seed);
            int floors = 14 + random.Next(10);
            float width = 9f + (float)random.NextDouble() * 5f;
            float depth = 9f + (float)random.NextDouble() * 5f;
            Swatch wall = random.Next(2) == 0 ? Swatch.Glass : Swatch.ConcreteDark;
            const float floorHeight = 3.2f;
            float height = floors * floorHeight;
            m.Color(wall).Box(new Vector3(0f, height * 0.5f, 0f), new Vector3(width, height, depth));
            Windows(m, random, width, depth, floors, floorHeight, 0.45f);
            m.Color(Swatch.ConcreteDark).Box(new Vector3(0f, height + 1f, 0f), new Vector3(width * 0.5f, 2f, depth * 0.5f));
            m.Color(Swatch.SteelDark).Cylinder(new Vector3(0f, height + 2f, 0f), 0.3f, 0.05f, 6f, 6, true);
            m.Color(Swatch.NeonRed).Sphere(new Vector3(0f, height + 8.2f, 0f), 0.35f, 4, 8);
            return m;
        }

        /// <summary>Copies the faces of <paramref name="source"/> into <paramref name="target"/> under its current transform.</summary>
        private static void Append(MeshBuilder target, MeshBuilder source)
        {
            target.Append(source);
        }
    }
}
