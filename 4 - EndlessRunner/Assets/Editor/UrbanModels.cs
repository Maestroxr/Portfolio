using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.EndlessRunner.EditorTools
{
    /// <summary>
    /// Recipes of the Night Shift theme's gameplay models: the courier who runs the night city, and the urban versions
    /// of every track piece. Each piece keeps the size, the pivot and the collider of its classic counterpart (see
    /// <see cref="RunnerModels"/>), so a race lays out the same track whatever theme a device shows.
    /// </summary>
    internal static class UrbanModels
    {
        // ------------------------------------------------------------------ the courier

        /// <summary>Jeans, a dark jacket with the hood down, a zipper and a messenger bag at the hip. Pivot at the hips.</summary>
        public static MeshBuilder Torso()
        {
            var m = new MeshBuilder();
            m.Color(Swatch.Denim).BeveledBox(new Vector3(0f, 0.04f, 0f), new Vector3(0.32f, 0.14f, 0.2f), 0.03f);
            m.Color(Swatch.JacketDark).Box(new Vector3(0f, 0.1f, 0f), new Vector3(0.33f, 0.04f, 0.21f));
            m.Color(Swatch.Jacket).BeveledBox(new Vector3(0f, 0.32f, 0f), new Vector3(0.36f, 0.42f, 0.24f), 0.05f);
            m.Color(Swatch.SteelLight).Box(new Vector3(0f, 0.31f, 0.121f), new Vector3(0.015f, 0.36f, 0.01f));
            m.Color(Swatch.ConeWhite).Box(new Vector3(0f, 0.42f, -0.121f), new Vector3(0.28f, 0.025f, 0.01f));
            m.Color(Swatch.JacketDark).Sphere(new Vector3(0f, 0.5f, -0.09f), new Vector3(0.16f, 0.07f, 0.11f), 5, 10, true, 0.5f);
            m.Color(Swatch.JacketDark).Torus(new Vector3(0f, 0.53f, 0f), 0.085f, 0.03f, 12, 6);
            m.Color(Swatch.Skin).Cylinder(new Vector3(0f, 0.5f, 0f), 0.055f, 0.05f, 0.1f, 10, true);
            m.Push(new Vector3(0f, 0.32f, 0f), Quaternion.Euler(0f, 0f, 38f));
            m.Color(Swatch.BagStrap).Box(new Vector3(0f, 0f, 0.128f), new Vector3(0.06f, 0.5f, 0.012f));
            m.Color(Swatch.BagStrap).Box(new Vector3(0f, 0f, -0.128f), new Vector3(0.06f, 0.5f, 0.012f));
            m.Pop();
            m.Color(Swatch.Bag).BeveledBox(new Vector3(-0.2f, 0.08f, -0.1f), new Vector3(0.26f, 0.2f, 0.13f), 0.03f);
            m.Color(Swatch.BagStrap).Box(new Vector3(-0.2f, 0.14f, -0.1f), new Vector3(0.27f, 0.09f, 0.14f));
            m.Color(Swatch.ConeWhite).Box(new Vector3(-0.2f, 0.06f, -0.168f), new Vector3(0.12f, 0.02f, 0.01f));
            return m;
        }

        /// <summary>An adult head with stubble, a beanie and a tired look. Pivot at the neck.</summary>
        public static MeshBuilder Head()
        {
            var m = new MeshBuilder();
            var c = new Vector3(0f, 0.15f, 0.01f);
            m.Color(Swatch.Skin).Sphere(c, new Vector3(0.125f, 0.15f, 0.13f), 10, 14);
            m.Push(c + new Vector3(0f, -0.02f, 0.015f), Quaternion.Euler(180f, 0f, 0f));
            m.Color(Swatch.Beard).Sphere(Vector3.zero, new Vector3(0.115f, 0.1f, 0.12f), 6, 12, true, 0.5f);
            m.Pop();
            for (int side = -1; side <= 1; side += 2)
            {
                m.Color(Swatch.Skin).Sphere(c + new Vector3(side * 0.125f, -0.01f, 0f), new Vector3(0.025f, 0.04f, 0.03f), 5, 8);
                m.Color(Swatch.Eye).Sphere(c + new Vector3(side * 0.045f, 0.02f, 0.118f), new Vector3(0.022f, 0.02f, 0.015f), 5, 8);
                m.Color(Swatch.Beard).Box(c + new Vector3(side * 0.045f, 0.06f, 0.122f), new Vector3(0.05f, 0.012f, 0.01f));
            }
            m.Color(Swatch.SkinShade).Sphere(c + new Vector3(0f, -0.02f, 0.135f), new Vector3(0.02f, 0.025f, 0.02f), 5, 8);
            m.Color(Swatch.Beard).Box(c + new Vector3(0f, -0.07f, 0.118f), new Vector3(0.05f, 0.008f, 0.01f));
            m.Color(Swatch.Beanie).Sphere(c + new Vector3(0f, 0.06f, -0.005f), new Vector3(0.135f, 0.13f, 0.14f), 8, 14, true, 0.55f);
            m.Color(Swatch.Beanie).Torus(c + new Vector3(0f, 0.045f, -0.005f), 0.128f, 0.028f, 14, 6);
            return m;
        }

        /// <summary>The jacket sleeve from the shoulder to the elbow. Pivot at the shoulder.</summary>
        public static MeshBuilder UpperArm()
        {
            var m = new MeshBuilder();
            m.Color(Swatch.Jacket).Sphere(Vector3.zero, 0.066f, 6, 10);
            m.Color(Swatch.Jacket).Cylinder(new Vector3(0f, -0.28f, 0f), 0.05f, 0.062f, 0.28f, 10, true, true, false);
            m.Color(Swatch.ConeWhite).Cylinder(new Vector3(0f, -0.1f, 0f), 0.058f, 0.06f, 0.02f, 10, true, false, false);
            return m;
        }

        /// <summary>The lower sleeve, its cuff and a bare hand. Pivot at the elbow.</summary>
        public static MeshBuilder Forearm()
        {
            var m = new MeshBuilder();
            m.Color(Swatch.Jacket).Sphere(Vector3.zero, 0.052f, 6, 10);
            m.Color(Swatch.Jacket).Cylinder(new Vector3(0f, -0.16f, 0f), 0.046f, 0.052f, 0.16f, 10, true, false, false);
            m.Color(Swatch.JacketDark).Cylinder(new Vector3(0f, -0.19f, 0f), 0.054f, 0.054f, 0.035f, 10, true);
            m.Color(Swatch.Skin).Cylinder(new Vector3(0f, -0.23f, 0f), 0.038f, 0.042f, 0.05f, 8, true, false, false);
            m.Color(Swatch.Skin).Sphere(new Vector3(0f, -0.265f, 0.01f), new Vector3(0.048f, 0.06f, 0.045f), 6, 10);
            return m;
        }

        /// <summary>A jeans leg down to the knee. Pivot at the hip.</summary>
        public static MeshBuilder Thigh()
        {
            var m = new MeshBuilder();
            m.Color(Swatch.Denim).Sphere(Vector3.zero, 0.085f, 6, 10);
            m.Color(Swatch.Denim).Cylinder(new Vector3(0f, -0.42f, 0f), 0.07f, 0.085f, 0.42f, 10, true, true, false);
            m.Color(Swatch.DenimDark).Box(new Vector3(0f, -0.1f, 0.078f), new Vector3(0.06f, 0.08f, 0.01f));
            return m;
        }

        /// <summary>The lower jeans leg and a low sneaker; the sole touches the ground in the rest pose. Pivot at the knee.</summary>
        public static MeshBuilder Shin()
        {
            var m = new MeshBuilder();
            m.Color(Swatch.Denim).Sphere(Vector3.zero, 0.068f, 6, 8);
            m.Color(Swatch.Denim).Cylinder(new Vector3(0f, -0.36f, 0f), 0.055f, 0.066f, 0.36f, 10, true, false, false);
            m.Color(Swatch.DenimDark).Cylinder(new Vector3(0f, -0.39f, 0f), 0.058f, 0.058f, 0.03f, 10, true);
            m.Color(Swatch.Sneaker).BeveledBox(new Vector3(0f, -0.435f, 0.045f), new Vector3(0.13f, 0.085f, 0.26f), 0.03f);
            m.Color(Swatch.SneakerSole).BeveledBox(new Vector3(0f, -0.465f, 0.045f), new Vector3(0.14f, 0.03f, 0.27f), 0.01f);
            m.Color(Swatch.SneakerSole).Box(new Vector3(0.066f, -0.43f, 0.05f), new Vector3(0.008f, 0.02f, 0.14f));
            m.Color(Swatch.SneakerSole).Box(new Vector3(-0.066f, -0.43f, 0.05f), new Vector3(0.008f, 0.02f, 0.14f));
            m.Color(Swatch.ConeWhite).Box(new Vector3(0f, -0.41f, 0.13f), new Vector3(0.05f, 0.03f, 0.05f));
            return m;
        }

        // ------------------------------------------------------------------ obstacles

        /// <summary>A sawhorse road block with reflective boards and a warning lamp, 1 m high. Pivot at the middle of its base.</summary>
        public static MeshBuilder RoadBlock()
        {
            var m = new MeshBuilder();
            const float post = 1.0f;
            for (int side = -1; side <= 1; side += 2)
            {
                for (int leg = -1; leg <= 1; leg += 2)
                {
                    m.Color(Swatch.SteelDark).Rod(new Vector3(side * post, 0f, leg * 0.32f), new Vector3(side * post, 0.95f, 0f), 0.03f, 6, false);
                }
                m.Color(Swatch.SteelDark).Box(new Vector3(side * post, 0.03f, 0f), new Vector3(0.12f, 0.06f, 0.72f));
            }
            for (int board = 0; board < 2; board++)
            {
                float y = board == 0 ? 0.84f : 0.5f;
                float height = board == 0 ? 0.2f : 0.14f;
                const int stripes = 8;
                const float width = 2.1f / stripes;
                for (int i = 0; i < stripes; i++)
                {
                    m.Color(i % 2 == 0 ? Swatch.ConeOrange : Swatch.ConeWhite).Box(new Vector3(-1.05f + width * (i + 0.5f), y, 0f), new Vector3(width, height, 0.06f));
                }
            }
            m.Color(Swatch.Tar).Box(new Vector3(0f, 0.98f, 0f), new Vector3(0.2f, 0.1f, 0.12f));
            m.Color(Swatch.NeonAmber).Sphere(new Vector3(0f, 1.02f, 0f), new Vector3(0.07f, 0.05f, 0.07f), 4, 8);
            return m;
        }

        /// <summary>A scaffold to slide under: tube posts, a plywood panel with warning tape, planks on top. Pivot at the middle of its base.</summary>
        public static MeshBuilder Scaffold()
        {
            var m = new MeshBuilder();
            const float post = 1.18f;
            for (int side = -1; side <= 1; side += 2)
            {
                m.Color(Swatch.SteelDark).Box(new Vector3(side * post, 0.04f, 0f), new Vector3(0.36f, 0.08f, 0.36f));
                m.Color(Swatch.Steel).Cylinder(new Vector3(side * post, 0.08f, 0f), 0.05f, 0.05f, 2.25f, 8, true);
                m.Color(Swatch.SteelDark).Cylinder(new Vector3(side * post, 1.16f, 0f), 0.075f, 0.075f, 0.08f, 8, true);
                m.Color(Swatch.SteelDark).Cylinder(new Vector3(side * post, 2.02f, 0f), 0.075f, 0.075f, 0.08f, 8, true);
            }
            m.Color(Swatch.Steel).Rod(new Vector3(-post, 1.2f, 0f), new Vector3(post, 1.2f, 0f), 0.04f, 8, true);
            m.Color(Swatch.Steel).Rod(new Vector3(-post, 2.06f, 0f), new Vector3(post, 2.06f, 0f), 0.04f, 8, true);
            m.Color(Swatch.Steel).Rod(new Vector3(-post + 0.1f, 1.25f, 0f), new Vector3(post - 0.1f, 2.0f, 0f), 0.025f, 6, true);
            var board = new Rect(-post + 0.12f, 1.25f, post * 2f - 0.24f, 0.8f);
            m.Color(Swatch.WoodDark).Box(new Vector3(0f, board.center.y, 0.03f), new Vector3(board.width, board.height, 0.06f));
            for (float x = board.xMin - board.height; x < board.xMax; x += 0.5f)
            {
                var stripe = new[]
                {
                    new Vector2(x, board.yMin), new Vector2(x + 0.22f, board.yMin),
                    new Vector2(x + 0.22f + board.height, board.yMax), new Vector2(x + board.height, board.yMax)
                };
                List<Vector2> clipped = MeshBuilder.Clip(stripe, board);
                if (clipped.Count >= 3)
                {
                    m.Color(Swatch.NeonAmber).Prism(clipped, 0.02f, new Vector3(0f, 0f, -0.01f));
                }
            }
            m.Color(Swatch.Tar).Box(new Vector3(0f, board.yMax + 0.04f, 0.03f), new Vector3(board.width + 0.1f, 0.08f, 0.1f));
            m.Color(Swatch.Tar).Box(new Vector3(0f, board.yMin - 0.04f, 0.03f), new Vector3(board.width + 0.1f, 0.08f, 0.1f));
            for (int i = 0; i < 4; i++)
            {
                m.Color(i % 2 == 0 ? Swatch.Wood : Swatch.WoodDark).Box(new Vector3(0f, 2.16f, -0.36f + i * 0.24f), new Vector3(post * 2f + 0.5f, 0.06f, 0.22f));
            }
            for (int side = -1; side <= 1; side += 2)
            {
                m.Color(Swatch.NeonRed).Sphere(new Vector3(side * post, 2.36f, 0f), 0.06f, 4, 8);
            }
            return m;
        }

        /// <summary>A steel dumpster with its lid ajar and a bag of trash on top, 2.2 m high. Pivot at the middle of its base.</summary>
        public static MeshBuilder Dumpster()
        {
            var m = new MeshBuilder();
            m.Color(Swatch.Container3).BeveledBox(new Vector3(0f, 1.05f, 0f), new Vector3(1.9f, 1.5f, 1.6f), 0.04f);
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 3; i++)
                {
                    m.Color(Swatch.ContainerDark).Box(new Vector3(side * 0.96f, 1.05f, -0.5f + i * 0.5f), new Vector3(0.04f, 1.4f, 0.1f));
                }
                m.Color(Swatch.ContainerDark).Box(new Vector3(0f, 1.05f, side * 0.81f), new Vector3(1.8f, 0.1f, 0.04f));
                m.Color(Swatch.Iron).Cylinder(new Vector3(side * 0.7f, 0f, -0.6f), 0.15f, 0.15f, 0.3f, 8, true);
                m.Color(Swatch.Iron).Cylinder(new Vector3(side * 0.7f, 0f, 0.6f), 0.15f, 0.15f, 0.3f, 8, true);
            }
            m.Color(Swatch.SignWhite).Box(new Vector3(0f, 1.3f, -0.81f), new Vector3(0.7f, 0.3f, 0.02f));
            m.Color(Swatch.ContainerDark).Box(new Vector3(0f, 1.3f, -0.83f), new Vector3(0.5f, 0.06f, 0.02f));
            m.Push(new Vector3(0f, 1.8f, 0.8f), Quaternion.Euler(-22f, 0f, 0f));
            m.Color(Swatch.ContainerDark).BeveledBox(new Vector3(0f, 0.05f, -0.8f), new Vector3(1.94f, 0.1f, 1.64f), 0.03f);
            m.Pop();
            m.Color(Swatch.Tar).Blob(new Vector3(-0.3f, 2.0f, -0.2f), new Vector3(0.45f, 0.32f, 0.4f), 0, 0.2f, 91);
            m.Color(Swatch.Tar).Blob(new Vector3(0.35f, 1.95f, 0.1f), new Vector3(0.38f, 0.28f, 0.34f), 0, 0.2f, 92);
            return m;
        }

        /// <summary>Two vending machines side by side, their fronts lit, 2.1 m high. Pivot at the middle of its base.</summary>
        public static MeshBuilder VendingMachines()
        {
            var m = new MeshBuilder();
            VendingMachine(m, new Vector3(-0.47f, 0f, 0f), Swatch.NeonCyan, Swatch.Container2);
            VendingMachine(m, new Vector3(0.47f, 0f, 0f), Swatch.NeonPink, Swatch.Container1);
            return m;
        }

        /// <summary>One vending machine, 0.9 m wide and 2.05 m high, its glass front toward -z.</summary>
        public static void VendingMachine(MeshBuilder m, Vector3 bottom, Swatch light, Swatch livery)
        {
            m.Color(Swatch.SteelDark).BeveledBox(bottom + new Vector3(0f, 1.025f, 0f), new Vector3(0.9f, 2.05f, 0.85f), 0.03f);
            m.Color(Swatch.Iron).Box(bottom + new Vector3(0f, 0.08f, 0f), new Vector3(0.92f, 0.16f, 0.87f));
            m.Color(Swatch.Glass).Box(bottom + new Vector3(-0.1f, 1.2f, -0.42f), new Vector3(0.6f, 1.3f, 0.03f));
            for (int shelf = 0; shelf < 4; shelf++)
            {
                float y = 0.7f + shelf * 0.3f;
                m.Color(Swatch.WindowLit).Box(bottom + new Vector3(-0.1f, y, -0.41f), new Vector3(0.58f, 0.02f, 0.02f));
                for (int i = 0; i < 3; i++)
                {
                    Swatch can = (i + shelf) % 3 == 0 ? Swatch.NeonRed : (i + shelf) % 3 == 1 ? livery : Swatch.TaxiYellow;
                    m.Color(can).Cylinder(bottom + new Vector3(-0.32f + i * 0.2f, y + 0.02f, -0.38f), 0.05f, 0.05f, 0.16f, 6, true);
                }
            }
            m.Color(light).Box(bottom + new Vector3(0f, 1.92f, -0.43f), new Vector3(0.8f, 0.14f, 0.02f));
            m.Color(livery).Box(bottom + new Vector3(0.3f, 1.2f, -0.43f), new Vector3(0.18f, 1.3f, 0.02f));
            m.Color(Swatch.Steel).Box(bottom + new Vector3(0.3f, 1.5f, -0.445f), new Vector3(0.12f, 0.24f, 0.01f));
            m.Color(Swatch.Iron).Box(bottom + new Vector3(-0.1f, 0.35f, -0.43f), new Vector3(0.5f, 0.16f, 0.02f));
        }

        /// <summary>
        /// A delivery truck to run on, 2.4 m high: a flat-nosed cab and a cargo box in a company livery. Pivot at the
        /// middle of its front bottom edge.
        /// </summary>
        public static MeshBuilder BoxTruck(float length, Swatch body, Swatch stripe)
        {
            var m = new MeshBuilder();
            const float width = 2.3f;
            const float top = 2.4f;
            const float bottom = 0.45f;
            const float cab = 2.4f;
            m.Color(Swatch.Iron).Box(new Vector3(0f, 0.36f, length * 0.5f), new Vector3(1.9f, 0.18f, length - 0.3f));
            Wheels(m, length, new[] { 1.5f, length - 2.6f, length - 1.4f });
            m.Color(body).BeveledBox(new Vector3(0f, (top + bottom) * 0.5f, cab * 0.5f), new Vector3(width, top - bottom, cab), 0.1f);
            m.Color(Swatch.Glass).Box(new Vector3(0f, 1.75f, -0.006f), new Vector3(width - 0.3f, 0.75f, 0.02f));
            m.Color(Swatch.SteelDark).Box(new Vector3(0f, 0.6f, -0.02f), new Vector3(width - 0.1f, 0.22f, 0.06f));
            m.Color(Swatch.Iron).Box(new Vector3(0f, 1.05f, -0.006f), new Vector3(1.2f, 0.36f, 0.02f));
            for (int side = -1; side <= 1; side += 2)
            {
                m.Color(Swatch.WindowLit).Box(new Vector3(side * 0.85f, 0.95f, -0.012f), new Vector3(0.36f, 0.2f, 0.02f));
                m.Color(Swatch.Glass).Box(new Vector3(side * (width * 0.5f + 0.006f), 1.7f, 1.3f), new Vector3(0.02f, 0.6f, 0.9f));
                m.Color(Swatch.SteelDark).Box(new Vector3(side * (width * 0.5f + 0.1f), 1.75f, 0.35f), new Vector3(0.18f, 0.22f, 0.12f));
                m.Color(Swatch.NeonAmber).Box(new Vector3(side * 0.9f, top - 0.06f, -0.012f), new Vector3(0.14f, 0.06f, 0.02f));
            }
            m.Color(Swatch.ContainerDark).Box(new Vector3(0f, (top + bottom) * 0.5f, cab + 0.15f), new Vector3(width - 0.4f, top - bottom - 0.3f, 0.3f));
            float box = length - cab - 0.3f;
            float middle = cab + 0.3f + box * 0.5f;
            m.Color(body).BeveledBox(new Vector3(0f, (top + bottom) * 0.5f, middle), new Vector3(width, top - bottom, box), 0.06f);
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * (width * 0.5f + 0.006f);
                m.Color(stripe).Box(new Vector3(x, 1.35f, middle), new Vector3(0.02f, 0.32f, box - 0.4f));
                m.Color(Swatch.SignWhite).Box(new Vector3(x, 1.95f, middle), new Vector3(0.02f, 0.2f, box * 0.5f));
                m.Color(Swatch.Tar).Box(new Vector3(x, top - 0.05f, middle), new Vector3(0.02f, 0.06f, box - 0.1f));
            }
            m.Color(Swatch.SteelDark).Box(new Vector3(0f, 1.42f, length + 0.006f), new Vector3(width - 0.2f, top - bottom - 0.1f, 0.02f));
            for (float y = 0.6f; y < top - 0.1f; y += 0.22f)
            {
                m.Color(Swatch.Iron).Box(new Vector3(0f, y, length + 0.012f), new Vector3(width - 0.3f, 0.02f, 0.02f));
            }
            m.Color(Swatch.NeonRed).Box(new Vector3(-0.95f, 0.75f, length + 0.012f), new Vector3(0.2f, 0.12f, 0.02f));
            m.Color(Swatch.NeonRed).Box(new Vector3(0.95f, 0.75f, length + 0.012f), new Vector3(0.2f, 0.12f, 0.02f));
            Rungs(m, top);
            m.Color(Swatch.Tar).Box(new Vector3(0f, top + 0.006f, middle), new Vector3(width - 0.6f, 0.012f, box - 0.6f));
            return m;
        }

        /// <summary>
        /// A subway car to run on, 2.4 m high: steel sides, a row of lit windows, doors and a stripe in the colour of
        /// its line. Pivot at the middle of its front bottom edge.
        /// </summary>
        public static MeshBuilder SubwayCar(float length, Swatch line)
        {
            var m = new MeshBuilder();
            const float width = 2.3f;
            const float top = 2.4f;
            const float bottom = 0.45f;
            float middle = length * 0.5f;
            m.Color(Swatch.Iron).Box(new Vector3(0f, 0.36f, middle), new Vector3(1.9f, 0.18f, length - 0.3f));
            Wheels(m, length, new[] { 2.2f, 3.2f, length - 3.2f, length - 2.2f });
            m.Color(Swatch.Steel).BeveledBox(new Vector3(0f, (top + bottom) * 0.5f, middle), new Vector3(width, top - bottom, length), 0.1f);
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * (width * 0.5f + 0.006f);
                m.Color(line).Box(new Vector3(x, 1.0f, middle), new Vector3(0.02f, 0.18f, length - 0.4f));
                m.Color(Swatch.SteelDark).Box(new Vector3(x, top - 0.12f, middle), new Vector3(0.02f, 0.08f, length - 0.4f));
                for (float z = 1.2f; z < length - 1f; z += 1.6f)
                {
                    bool door = Mathf.Abs(z - length * 0.25f) < 0.8f || Mathf.Abs(z - length * 0.75f) < 0.8f;
                    if (door)
                    {
                        m.Color(Swatch.SteelDark).Box(new Vector3(x, 1.35f, z), new Vector3(0.02f, 1.7f, 1.3f));
                        m.Color(Swatch.WindowLit).Box(new Vector3(side * (width * 0.5f + 0.012f), 1.75f, z), new Vector3(0.02f, 0.5f, 1.1f));
                    }
                    else
                    {
                        m.Color(Swatch.WindowLit).Box(new Vector3(x, 1.75f, z), new Vector3(0.02f, 0.55f, 1.15f));
                    }
                }
            }
            m.Color(Swatch.Glass).Box(new Vector3(0f, 1.8f, -0.006f), new Vector3(width - 0.5f, 0.6f, 0.02f));
            m.Color(Swatch.SteelDark).Box(new Vector3(0f, 0.62f, -0.06f), new Vector3(2.1f, 0.24f, 0.14f));
            for (int side = -1; side <= 1; side += 2)
            {
                m.Color(Swatch.WindowLit).Box(new Vector3(side * 0.8f, 1.15f, -0.012f), new Vector3(0.24f, 0.18f, 0.02f));
                m.Color(Swatch.NeonRed).Box(new Vector3(side * 0.8f, 0.85f, length + 0.012f), new Vector3(0.2f, 0.12f, 0.02f));
            }
            m.Color(line).Box(new Vector3(0f, 2.15f, -0.012f), new Vector3(0.9f, 0.3f, 0.02f));
            Rungs(m, top);
            m.Color(Swatch.SteelDark).Box(new Vector3(0f, top + 0.006f, middle), new Vector3(width - 0.6f, 0.012f, length - 0.6f));
            return m;
        }

        /// <summary>Twin wheels under a wagon-sized vehicle, at the given distances along z.</summary>
        private static void Wheels(MeshBuilder m, float length, float[] axles)
        {
            foreach (float z in axles)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    m.Push(new Vector3(side * 0.95f, 0.36f, z), Quaternion.Euler(0f, 0f, 90f));
                    m.Color(Swatch.Tar).Cylinder(new Vector3(0f, -0.1f, 0f), 0.36f, 0.36f, 0.2f, 12);
                    m.Color(Swatch.SteelLight).Cylinder(new Vector3(0f, -0.12f, 0f), 0.16f, 0.16f, 0.24f, 8);
                    m.Pop();
                }
            }
        }

        /// <summary>Service rungs on the front face, the way the classic wagons have them: the runner climbs them off the ramp.</summary>
        private static void Rungs(MeshBuilder m, float top)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                m.Color(Swatch.SteelDark).Box(new Vector3(side * 0.25f, 1.45f, -0.045f), new Vector3(0.05f, 1.8f, 0.05f));
            }
            for (float y = 0.75f; y < top - 0.1f; y += 0.3f)
            {
                m.Color(Swatch.SteelLight).Box(new Vector3(0f, y, -0.045f), new Vector3(0.5f, 0.04f, 0.04f));
            }
        }

        /// <summary>A steel loading ramp with grip bars and hazard edges, up to truck height. Pivot at the middle of its lower edge.</summary>
        public static MeshBuilder LoadingRamp(float width, float height, float length)
        {
            var m = new MeshBuilder();
            m.Color(Swatch.SteelDark).Wedge(width - 0.2f, height - 0.08f, length);
            float slope = Mathf.Sqrt(length * length + height * height);
            float angle = Mathf.Atan2(height, length) * Mathf.Rad2Deg;
            const int plates = 6;
            for (int i = 0; i < plates; i++)
            {
                float s = (i + 0.5f) / plates;
                m.Push(new Vector3(0f, s * height - 0.02f, s * length), Quaternion.Euler(-angle, 0f, 0f));
                m.Color(i % 2 == 0 ? Swatch.Steel : Swatch.SteelLight).Box(Vector3.zero, new Vector3(width, 0.08f, slope / plates * 0.96f));
                m.Pop();
            }
            for (int i = 0; i < 11; i++)
            {
                float s = (i + 0.5f) / 11f;
                m.Push(new Vector3(0f, s * height + 0.035f, s * length), Quaternion.Euler(-angle, 0f, 0f));
                m.Color(Swatch.ConcreteDark).Box(Vector3.zero, new Vector3(width - 0.3f, 0.03f, 0.06f));
                m.Pop();
            }
            for (int side = -1; side <= 1; side += 2)
            {
                m.Push(new Vector3(side * width * 0.5f, height * 0.5f + 0.05f, length * 0.5f), Quaternion.Euler(-angle, 0f, 0f));
                m.Color(Swatch.Iron).Box(Vector3.zero, new Vector3(0.1f, 0.14f, slope));
                for (int i = 0; i < 8; i++)
                {
                    m.Color(i % 2 == 0 ? Swatch.NeonAmber : Swatch.Tar).Box(new Vector3(0f, 0.05f, -slope * 0.5f + slope * (i + 0.5f) / 8f), new Vector3(0.11f, 0.05f, slope / 8f));
                }
                m.Pop();
            }
            return m;
        }

        /// <summary>A steel plate bridge with pipe railings across a chasm. Pivot at the middle of its near end.</summary>
        public static MeshBuilder SteelBridge(float length)
        {
            var m = new MeshBuilder();
            m.Color(Swatch.Steel).Box(new Vector3(0f, -0.06f, length * 0.5f), new Vector3(2.3f, 0.1f, length));
            for (float z = 0.25f; z < length; z += 0.45f)
            {
                m.Color(Swatch.SteelDark).Box(new Vector3(0f, -0.005f, z), new Vector3(2.1f, 0.012f, 0.06f));
            }
            for (int side = -1; side <= 1; side += 2)
            {
                m.Color(Swatch.Iron).Box(new Vector3(side * 1.1f, -0.16f, length * 0.5f), new Vector3(0.14f, 0.14f, length));
                m.Color(Swatch.NeonAmber).Box(new Vector3(side * 1.1f, -0.005f, 0.12f), new Vector3(0.14f, 0.012f, 0.2f));
                m.Color(Swatch.NeonAmber).Box(new Vector3(side * 1.1f, -0.005f, length - 0.12f), new Vector3(0.14f, 0.012f, 0.2f));
                foreach (float z in new[] { 0.15f, length * 0.5f, length - 0.15f })
                {
                    m.Color(Swatch.SteelLight).Cylinder(new Vector3(side * 1.15f, -0.1f, z), 0.03f, 0.03f, 1.0f, 6, true);
                }
                m.Color(Swatch.SteelLight).Rod(new Vector3(side * 1.15f, 0.9f, 0.15f), new Vector3(side * 1.15f, 0.9f, length - 0.15f), 0.03f, 6);
                m.Color(Swatch.SteelLight).Rod(new Vector3(side * 1.15f, 0.5f, 0.15f), new Vector3(side * 1.15f, 0.5f, length - 0.15f), 0.02f, 6);
            }
            return m;
        }

        /// <summary>Body of a runaway mail cart stacked with parcels. Pivot at the middle of its base; it rolls toward -z.</summary>
        public static MeshBuilder MailCart()
        {
            var m = new MeshBuilder();
            m.Color(Swatch.Iron).Box(new Vector3(0f, 0.3f, 0f), new Vector3(1.3f, 0.12f, 1.8f));
            m.Color(Swatch.Steel).Box(new Vector3(0f, 0.5f, 0f), new Vector3(1.6f, 0.06f, 2.0f));
            foreach (Vector3 corner in new[] { new Vector3(-0.78f, 0f, -0.98f), new Vector3(0.78f, 0f, -0.98f), new Vector3(-0.78f, 0f, 0.98f), new Vector3(0.78f, 0f, 0.98f) })
            {
                m.Color(Swatch.SteelDark).Cylinder(corner + new Vector3(0f, 0.5f, 0f), 0.03f, 0.03f, 0.9f, 6, true);
            }
            foreach (float y in new[] { 0.7f, 0.95f, 1.2f, 1.4f })
            {
                m.Color(Swatch.SteelLight).Box(new Vector3(-0.78f, y, 0f), new Vector3(0.02f, 0.02f, 1.96f));
                m.Color(Swatch.SteelLight).Box(new Vector3(0.78f, y, 0f), new Vector3(0.02f, 0.02f, 1.96f));
                m.Color(Swatch.SteelLight).Box(new Vector3(0f, y, 0.98f), new Vector3(1.56f, 0.02f, 0.02f));
            }
            var parcels = new[]
            {
                (new Vector3(-0.35f, 0.75f, -0.4f), new Vector3(0.7f, 0.44f, 0.9f), Swatch.Wood),
                (new Vector3(0.4f, 0.72f, -0.3f), new Vector3(0.6f, 0.4f, 0.7f), Swatch.WoodLight),
                (new Vector3(0.1f, 0.7f, 0.5f), new Vector3(1.2f, 0.36f, 0.8f), Swatch.Tarp),
                (new Vector3(-0.2f, 1.14f, -0.1f), new Vector3(0.8f, 0.4f, 0.9f), Swatch.WoodLight),
                (new Vector3(0.45f, 1.1f, 0.45f), new Vector3(0.5f, 0.34f, 0.6f), Swatch.Wood),
                (new Vector3(-0.15f, 1.5f, 0.2f), new Vector3(0.5f, 0.3f, 0.5f), Swatch.ConeWhite)
            };
            foreach ((Vector3 center, Vector3 size, Swatch color) in parcels)
            {
                m.Color(color).BeveledBox(center, size, 0.02f);
                m.Color(Swatch.BagStrap).Box(center + new Vector3(0f, 0f, 0f), new Vector3(size.x + 0.01f, size.y + 0.01f, 0.06f));
            }
            m.Color(Swatch.SteelDark).Cylinder(new Vector3(0f, 0.5f, -1.0f), 0.025f, 0.025f, 1.0f, 6, true);
            m.Color(Swatch.Tar).Box(new Vector3(0f, 1.55f, -1.0f), new Vector3(0.16f, 0.1f, 0.16f));
            m.Color(Swatch.NeonAmber).Sphere(new Vector3(0f, 1.62f, -1.0f), 0.07f, 4, 8);
            m.Color(Swatch.SignWhite).Box(new Vector3(0f, 0.85f, -1.0f), new Vector3(0.5f, 0.3f, 0.02f));
            return m;
        }

        // ------------------------------------------------------------------ pickups and pads

        /// <summary>A credit chip: a dark steel disc with a glowing ring and a hexagon socket. Pivot at its centre.</summary>
        public static MeshBuilder CreditChip()
        {
            var m = new MeshBuilder();
            m.Push(Vector3.zero, Quaternion.Euler(90f, 0f, 0f));
            m.Color(Swatch.Chip).Cylinder(new Vector3(0f, -0.05f, 0f), 0.4f, 0.4f, 0.1f, 24, true);
            m.Color(Swatch.ChipRim).Torus(new Vector3(0f, 0.05f, 0f), 0.36f, 0.03f, 24, 6);
            m.Color(Swatch.ChipRim).Torus(new Vector3(0f, -0.05f, 0f), 0.36f, 0.03f, 24, 6);
            m.Color(Swatch.NeonCyan).Torus(new Vector3(0f, 0.052f, 0f), 0.25f, 0.022f, 24, 6);
            m.Color(Swatch.NeonCyan).Torus(new Vector3(0f, -0.052f, 0f), 0.25f, 0.022f, 24, 6);
            m.Pop();
            m.Color(Swatch.Iron).Prism(Hexagon(0.15f), 0.13f);
            m.Color(Swatch.NeonCyan).Prism(Hexagon(0.07f), 0.14f);
            return m;
        }

        /// <summary>A hydraulic lift pad: a steel base with hazard ring and pistons. Pivot at the middle of its base.</summary>
        public static MeshBuilder LiftPadBase()
        {
            var m = new MeshBuilder();
            m.Color(Swatch.SteelDark).Cylinder(Vector3.zero, 0.98f, 0.92f, 0.16f, 18);
            m.Color(Swatch.NeonAmber).Torus(new Vector3(0f, 0.17f, 0f), 0.86f, 0.06f, 22, 6);
            for (int i = 0; i < 6; i++)
            {
                float angle = i * Mathf.PI / 3f;
                Vector3 center = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 0.55f;
                m.Color(Swatch.Steel).Cylinder(center + Vector3.up * 0.14f, 0.07f, 0.07f, 0.1f, 8, true);
                m.Color(Swatch.SteelLight).Cylinder(center + Vector3.up * 0.24f, 0.045f, 0.045f, 0.08f, 8, true);
            }
            return m;
        }

        /// <summary>A finish gantry of steel lattice, a lit sign and a chequered line. Pivot on the finish line.</summary>
        public static MeshBuilder FinishGantry()
        {
            var m = new MeshBuilder();
            const float pillarX = 5.1f;
            const float beamY = 6.0f;
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * pillarX;
                m.Color(Swatch.ConcreteDark).BeveledBox(new Vector3(x, 0.15f, 0f), new Vector3(1f, 0.3f, 1f), 0.05f);
                for (int leg = -1; leg <= 1; leg += 2)
                {
                    m.Color(Swatch.SteelDark).Box(new Vector3(x + leg * 0.3f, 3.15f, 0f), new Vector3(0.1f, 5.7f, 0.1f));
                    m.Color(Swatch.SteelDark).Box(new Vector3(x, 3.15f, leg * 0.3f), new Vector3(0.1f, 5.7f, 0.1f));
                }
                for (float y = 0.6f; y < beamY; y += 1.2f)
                {
                    m.Color(Swatch.Steel).Rod(new Vector3(x - 0.3f, y, 0f), new Vector3(x + 0.3f, y + 0.9f, 0f), 0.03f, 5, false);
                    m.Color(Swatch.Steel).Box(new Vector3(x, y, 0f), new Vector3(0.7f, 0.04f, 0.7f));
                }
                m.Color(Swatch.NeonRed).Sphere(new Vector3(x, beamY + 0.75f, 0f), 0.1f, 4, 8);
                m.Color(Swatch.Iron).Cylinder(new Vector3(x, beamY + 0.25f, 0f), 0.03f, 0.03f, 0.5f, 5, true);
            }
            m.Color(Swatch.Iron).Box(new Vector3(0f, beamY, 0f), new Vector3(pillarX * 2f + 0.7f, 0.5f, 0.5f));
            m.Color(Swatch.Tar).Box(new Vector3(0f, 5.05f, 0f), new Vector3(pillarX * 2f - 0.2f, 1.3f, 0.16f));
            const float square = 0.32f;
            int columns = Mathf.RoundToInt((pillarX * 2f - 0.4f) / square);
            for (int column = 0; column < columns; column++)
            {
                float x = -pillarX + 0.2f + (column + 0.5f) * square;
                m.Color(column % 2 == 0 ? Swatch.SignWhite : Swatch.Tar).Box(new Vector3(x, 4.56f, 0f), new Vector3(square, 0.28f, 0.18f));
                m.Color(column % 2 == 0 ? Swatch.Tar : Swatch.SignWhite).Box(new Vector3(x, 4.84f, 0f), new Vector3(square, 0.28f, 0.18f));
                if (column % 2 == 0)
                {
                    m.Color(Swatch.NeonAmber).Sphere(new Vector3(x, 5.62f, 0f), 0.07f, 4, 8);
                }
            }
            m.Color(Swatch.NeonCyan).Box(new Vector3(0f, 5.25f, 0f), new Vector3(pillarX * 2f - 1.2f, 0.12f, 0.2f));
            RunnerModels.ChequeredLine(m, Swatch.SignWhite, Swatch.Tar);
            return m;
        }

        public static Vector2[] Hexagon(float radius)
        {
            var outline = new Vector2[6];
            for (int i = 0; i < 6; i++)
            {
                float angle = i * Mathf.PI / 3f;
                outline[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }
            return outline;
        }
    }
}
