using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.EndlessRunner.EditorTools
{
    /// <summary>
    /// Recipes of the Endless Runner's gameplay models: the runner (one mesh per animated body part, pivot at the
    /// joint), obstacles, pickups, road tiles and the finish arch. Sizes are in meters; the track runs along +z and
    /// the runner approaches every obstacle from -z.
    /// </summary>
    internal static class RunnerModels
    {
        public const float TileLength = 6f;
        public const float RoadHalfWidth = 3.75f;
        public const float CurbWidth = 0.6f;
        public const float GroundHalfWidth = 180f;
        public const float WaterLevel = -0.55f;

        // ------------------------------------------------------------------ runner

        /// <summary>Hips up to the neck: shorts, hoodie and backpack. Pivot at the hips.</summary>
        public static MeshBuilder Torso()
        {
            var m = new MeshBuilder();
            m.Color(Swatch.Shorts).BeveledBox(new Vector3(0f, 0.03f, 0f), new Vector3(0.34f, 0.16f, 0.22f), 0.04f);
            m.Color(Swatch.Hoodie).BeveledBox(new Vector3(0f, 0.29f, 0f), new Vector3(0.42f, 0.4f, 0.26f), 0.07f);
            m.Color(Swatch.HoodieDark).BeveledBox(new Vector3(0f, 0.11f, 0f), new Vector3(0.43f, 0.06f, 0.27f), 0.02f);
            m.Color(Swatch.HoodieDark).BeveledBox(new Vector3(0f, 0.2f, 0.128f), new Vector3(0.24f, 0.1f, 0.02f), 0.008f);
            m.Color(Swatch.White).Box(new Vector3(0f, 0.37f, 0.131f), new Vector3(0.025f, 0.2f, 0.01f));
            m.Color(Swatch.HoodieDark).Torus(new Vector3(0f, 0.48f, -0.01f), 0.1f, 0.045f, 12, 6);
            m.Color(Swatch.Skin).Cylinder(new Vector3(0f, 0.45f, 0f), 0.065f, 0.06f, 0.12f, 10, true);
            m.Color(Swatch.Backpack).BeveledBox(new Vector3(0f, 0.3f, -0.2f), new Vector3(0.32f, 0.34f, 0.15f), 0.05f);
            m.Color(Swatch.Orange).BeveledBox(new Vector3(0f, 0.21f, -0.28f), new Vector3(0.22f, 0.12f, 0.04f), 0.015f);
            m.Color(Swatch.Orange).Box(new Vector3(-0.13f, 0.35f, 0.132f), new Vector3(0.045f, 0.24f, 0.012f));
            m.Color(Swatch.Orange).Box(new Vector3(0.13f, 0.35f, 0.132f), new Vector3(0.045f, 0.24f, 0.012f));
            return m;
        }

        /// <summary>Head with face, hair and cap. Pivot at the neck.</summary>
        public static MeshBuilder Head()
        {
            var m = new MeshBuilder();
            var c = new Vector3(0f, 0.2f, 0.01f);
            m.Color(Swatch.Hair).Sphere(c + new Vector3(0f, 0.02f, -0.045f), new Vector3(0.24f, 0.22f, 0.22f), 10, 14);
            m.Color(Swatch.Skin).Sphere(c, 0.23f, 12, 18);
            for (int side = -1; side <= 1; side += 2)
            {
                m.Color(Swatch.Skin).Sphere(c + new Vector3(side * 0.225f, -0.01f, 0f), new Vector3(0.04f, 0.06f, 0.05f), 6, 8);
                m.Color(Swatch.Eye).Sphere(c + new Vector3(side * 0.08f, 0.02f, 0.2f), new Vector3(0.034f, 0.05f, 0.028f), 6, 10);
                m.Color(Swatch.White).Sphere(c + new Vector3(side * 0.08f + 0.012f, 0.042f, 0.226f), 0.011f, 4, 6);
                m.Color(Swatch.Cheek).Sphere(c + new Vector3(side * 0.13f, -0.05f, 0.182f), new Vector3(0.042f, 0.026f, 0.02f), 4, 8);
            }
            m.Color(Swatch.SkinShade).Sphere(c + new Vector3(0f, -0.03f, 0.228f), 0.032f, 6, 8);
            m.Push(c + new Vector3(0f, -0.085f, 0.203f), Quaternion.Euler(-90f + 12f, 0f, 0f));
            m.Color(Swatch.Eye).Torus(Vector3.zero, 0.05f, 0.012f, 8, 5, 140f, true, 200f);
            m.Pop();
            m.Color(Swatch.Cap).Sphere(c + new Vector3(0f, 0.035f, -0.005f), new Vector3(0.245f, 0.22f, 0.245f), 10, 18, true, 0.5f);
            m.Push(c + new Vector3(0f, 0.05f, 0.2f), Quaternion.Euler(8f, 0f, 0f));
            m.Color(Swatch.Cap).BeveledBox(new Vector3(0f, 0f, 0.04f), new Vector3(0.3f, 0.028f, 0.18f), 0.01f);
            m.Pop();
            m.Color(Swatch.White).Sphere(c + new Vector3(0f, 0.258f, 0f), 0.026f, 4, 6);
            m.Color(Swatch.White).Sphere(c + new Vector3(0f, 0.15f, 0.215f), new Vector3(0.05f, 0.04f, 0.02f), 4, 8);
            return m;
        }

        /// <summary>Hoodie sleeve from the shoulder to the elbow. Pivot at the shoulder.</summary>
        public static MeshBuilder UpperArm()
        {
            var m = new MeshBuilder();
            m.Color(Swatch.Hoodie).Sphere(Vector3.zero, 0.078f, 6, 10);
            m.Color(Swatch.Hoodie).Cylinder(new Vector3(0f, -0.25f, 0f), 0.062f, 0.072f, 0.25f, 10, true, true, false);
            return m;
        }

        /// <summary>Sleeve, cuff and hand. Pivot at the elbow.</summary>
        public static MeshBuilder Forearm()
        {
            var m = new MeshBuilder();
            m.Color(Swatch.Hoodie).Sphere(Vector3.zero, 0.062f, 6, 10);
            m.Color(Swatch.Hoodie).Cylinder(new Vector3(0f, -0.13f, 0f), 0.058f, 0.062f, 0.13f, 10, true, false, false);
            m.Color(Swatch.HoodieDark).Cylinder(new Vector3(0f, -0.16f, 0f), 0.066f, 0.066f, 0.035f, 10, true);
            m.Color(Swatch.Skin).Cylinder(new Vector3(0f, -0.2f, 0f), 0.045f, 0.05f, 0.05f, 8, true, false, false);
            m.Color(Swatch.Skin).Sphere(new Vector3(0f, -0.235f, 0.01f), new Vector3(0.058f, 0.068f, 0.055f), 6, 10);
            return m;
        }

        /// <summary>Shorts leg and knee. Pivot at the hip.</summary>
        public static MeshBuilder Thigh()
        {
            var m = new MeshBuilder();
            m.Color(Swatch.Shorts).Sphere(Vector3.zero, 0.098f, 6, 10);
            m.Color(Swatch.Shorts).Cylinder(new Vector3(0f, -0.2f, 0f), 0.092f, 0.1f, 0.2f, 10, true, true, false);
            m.Color(Swatch.Skin).Cylinder(new Vector3(0f, -0.36f, 0f), 0.068f, 0.078f, 0.18f, 10, true, false, false);
            return m;
        }

        /// <summary>Lower leg, sock and sneaker; the sole touches the ground in the rest pose. Pivot at the knee.</summary>
        public static MeshBuilder Shin()
        {
            var m = new MeshBuilder();
            m.Color(Swatch.Skin).Sphere(Vector3.zero, 0.07f, 6, 8);
            m.Color(Swatch.Skin).Cylinder(new Vector3(0f, -0.26f, 0f), 0.056f, 0.068f, 0.26f, 10, true, false, false);
            m.Color(Swatch.White).Cylinder(new Vector3(0f, -0.33f, 0f), 0.063f, 0.06f, 0.08f, 10, true);
            m.Color(Swatch.Shoe).BeveledBox(new Vector3(0f, -0.362f, 0.045f), new Vector3(0.15f, 0.09f, 0.26f), 0.035f);
            m.Color(Swatch.ShoeSole).BeveledBox(new Vector3(0f, -0.405f, 0.045f), new Vector3(0.16f, 0.03f, 0.27f), 0.01f);
            m.Color(Swatch.ShoeSole).Box(new Vector3(0.077f, -0.36f, 0.05f), new Vector3(0.01f, 0.03f, 0.14f));
            m.Color(Swatch.ShoeSole).Box(new Vector3(-0.077f, -0.36f, 0.05f), new Vector3(0.01f, 0.03f, 0.14f));
            return m;
        }

        // ------------------------------------------------------------------ obstacles

        /// <summary>A striped hurdle to jump, 1 m high. Pivot at the middle of its base.</summary>
        public static MeshBuilder Hurdle()
        {
            var m = new MeshBuilder();
            const float post = 1.05f;
            for (int side = -1; side <= 1; side += 2)
            {
                m.Color(Swatch.DarkGray).BeveledBox(new Vector3(side * post, 0.05f, 0f), new Vector3(0.18f, 0.1f, 0.7f), 0.03f);
                m.Color(Swatch.LightGray).Cylinder(new Vector3(side * post, 0.05f, 0f), 0.06f, 0.05f, 0.93f, 8, true);
                m.Color(Swatch.Red).Sphere(new Vector3(side * post, 1f, 0f), 0.08f, 5, 8);
            }
            for (int board = 0; board < 2; board++)
            {
                float y = board == 0 ? 0.84f : 0.52f;
                float height = board == 0 ? 0.2f : 0.14f;
                const int stripes = 7;
                const float width = 2f / stripes;
                for (int i = 0; i < stripes; i++)
                {
                    m.Color(i % 2 == 0 ? Swatch.Red : Swatch.White).Box(new Vector3(-1f + width * (i + 0.5f), y, 0f), new Vector3(width, height, 0.08f));
                }
            }
            return m;
        }

        /// <summary>An overhead barrier to slide under: its board starts 1.2 m up. Pivot at the middle of its base.</summary>
        public static MeshBuilder Barrier()
        {
            var m = new MeshBuilder();
            const float post = 1.18f;
            for (int side = -1; side <= 1; side += 2)
            {
                m.Color(Swatch.DarkGray).BeveledBox(new Vector3(side * post, 0.06f, 0f), new Vector3(0.3f, 0.12f, 0.5f), 0.03f);
                for (int band = 0; band < 8; band++)
                {
                    m.Color(band % 2 == 0 ? Swatch.HazardYellow : Swatch.HazardBlack)
                        .Cylinder(new Vector3(side * post, 0.12f + band * 0.26f, 0f), 0.07f, 0.07f, 0.26f, 8, true, band == 0, band == 7);
                }
            }
            var board = new Rect(-post, 1.25f, post * 2f, 0.8f);
            m.Color(Swatch.HazardBlack).Box(new Vector3(0f, board.center.y, 0f), new Vector3(board.width, board.height, 0.1f));
            for (float x = board.xMin - board.height; x < board.xMax; x += 0.5f)
            {
                var stripe = new[]
                {
                    new Vector2(x, board.yMin), new Vector2(x + 0.25f, board.yMin),
                    new Vector2(x + 0.25f + board.height, board.yMax), new Vector2(x + board.height, board.yMax)
                };
                List<Vector2> clipped = MeshBuilder.Clip(stripe, board);
                if (clipped.Count >= 3)
                {
                    m.Color(Swatch.HazardYellow).Prism(clipped, 0.12f);
                }
            }
            m.Color(Swatch.White).Box(new Vector3(0f, board.yMax + 0.04f, 0f), new Vector3(board.width + 0.1f, 0.08f, 0.16f));
            m.Color(Swatch.White).Box(new Vector3(0f, board.yMin - 0.04f, 0f), new Vector3(board.width + 0.1f, 0.08f, 0.16f));
            for (int side = -1; side <= 1; side += 2)
            {
                Chevron(m, new Vector3(side * 0.55f, board.center.y, -0.075f));
            }
            return m;
        }

        /// <summary>A white arrow pointing down (slide!) on the front of a barrier.</summary>
        private static void Chevron(MeshBuilder m, Vector3 center)
        {
            m.Color(Swatch.White);
            m.Push(center + new Vector3(-0.09f, 0.03f, 0f), Quaternion.Euler(0f, 0f, 45f));
            m.Box(Vector3.zero, new Vector3(0.28f, 0.08f, 0.03f));
            m.Pop();
            m.Push(center + new Vector3(0.09f, 0.03f, 0f), Quaternion.Euler(0f, 0f, -45f));
            m.Box(Vector3.zero, new Vector3(0.28f, 0.08f, 0.03f));
            m.Pop();
        }

        /// <summary>Two wooden crates stacked, 2.2 m high. Pivot at the middle of its base.</summary>
        public static MeshBuilder CrateStack()
        {
            var m = new MeshBuilder();
            Crate(m, new Vector3(0f, 0.6f, 0f), new Vector3(1.95f, 1.2f, 1.7f), 0f);
            Crate(m, new Vector3(0.08f, 1.7f, 0.05f), new Vector3(1.5f, 1f, 1.3f), 12f);
            return m;
        }

        private static void Crate(MeshBuilder m, Vector3 center, Vector3 size, float yaw)
        {
            m.Push(center, Quaternion.Euler(0f, yaw, 0f));
            m.Color(Swatch.Wood).Box(Vector3.zero, size * 0.96f);
            Vector3 half = size * 0.5f;
            const float t = 0.1f;
            m.Color(Swatch.WoodDark);
            for (int a = -1; a <= 1; a += 2)
            {
                for (int b = -1; b <= 1; b += 2)
                {
                    m.Box(new Vector3(0f, a * (half.y - t / 2), b * (half.z - t / 2)), new Vector3(size.x, t, t));
                    m.Box(new Vector3(a * (half.x - t / 2), 0f, b * (half.z - t / 2)), new Vector3(t, size.y, t));
                    m.Box(new Vector3(a * (half.x - t / 2), b * (half.y - t / 2), 0f), new Vector3(t, t, size.z));
                }
            }
            float frontAngle = Mathf.Atan2(size.y, size.x) * Mathf.Rad2Deg;
            float sideAngle = Mathf.Atan2(size.y, size.z) * Mathf.Rad2Deg;
            for (int side = -1; side <= 1; side += 2)
            {
                m.Push(new Vector3(0f, 0f, side * (half.z - 0.01f)), Quaternion.Euler(0f, 0f, frontAngle));
                m.Box(Vector3.zero, new Vector3(Mathf.Sqrt(size.x * size.x + size.y * size.y) * 0.9f, 0.1f, 0.05f));
                m.Pop();
                m.Push(new Vector3(side * (half.x - 0.01f), 0f, 0f), Quaternion.Euler(sideAngle, 0f, 0f));
                m.Box(Vector3.zero, new Vector3(0.05f, 0.1f, Mathf.Sqrt(size.z * size.z + size.y * size.y) * 0.9f));
                m.Pop();
            }
            m.Pop();
        }

        /// <summary>Three oil drums, two below and one on top. Pivot at the middle of its base.</summary>
        public static MeshBuilder BarrelStack()
        {
            var m = new MeshBuilder();
            Barrel(m, new Vector3(-0.48f, 0f, 0f), Swatch.Red);
            Barrel(m, new Vector3(0.48f, 0f, 0f), Swatch.Blue);
            Barrel(m, new Vector3(0f, 1.06f, 0.02f), Swatch.Red);
            return m;
        }

        private static void Barrel(MeshBuilder m, Vector3 bottom, Swatch body)
        {
            m.Color(body).Cylinder(bottom, 0.44f, 0.44f, 1.04f, 14, true);
            m.Color(Swatch.MetalDark).Cylinder(bottom + Vector3.up * 0.22f, 0.455f, 0.455f, 0.06f, 14, true);
            m.Color(Swatch.MetalDark).Cylinder(bottom + Vector3.up * 0.76f, 0.455f, 0.455f, 0.06f, 14, true);
            m.Color(Swatch.HazardYellow).Cylinder(bottom + Vector3.up * 0.45f, 0.448f, 0.448f, 0.14f, 14, true);
            m.Color(Swatch.MetalDark).Cylinder(bottom + Vector3.up * 1.04f, 0.3f, 0.3f, 0.02f, 10, true);
        }

        /// <summary>A freight wagon to run on, 2.4 m high. Pivot at the middle of its front bottom edge.</summary>
        public static MeshBuilder Wagon(float length, Swatch body, Swatch rib)
        {
            var m = new MeshBuilder();
            const float width = 2.3f;
            const float top = 2.4f;
            const float bottom = 0.45f;
            float middle = length * 0.5f;
            m.Color(Swatch.WagonDark).Box(new Vector3(0f, 0.36f, middle), new Vector3(1.9f, 0.18f, length - 0.3f));
            foreach (float z in new[] { 1.2f, 2.1f, length - 2.1f, length - 1.2f })
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    m.Push(new Vector3(side * 0.95f, 0.3f, z), Quaternion.Euler(0f, 0f, 90f));
                    m.Color(Swatch.Charcoal).Cylinder(new Vector3(0f, -0.08f, 0f), 0.3f, 0.3f, 0.16f, 12);
                    m.Color(Swatch.Metal).Cylinder(new Vector3(0f, -0.1f, 0f), 0.1f, 0.1f, 0.2f, 8);
                    m.Pop();
                }
            }
            m.Color(body).BeveledBox(new Vector3(0f, (top + bottom) * 0.5f, middle), new Vector3(width, top - bottom, length), 0.08f);
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * (width * 0.5f + 0.006f);
                m.Color(Swatch.WagonTrim).Box(new Vector3(x, top - 0.16f, middle), new Vector3(0.02f, 0.1f, length - 0.2f));
                m.Color(Swatch.WagonTrim).Box(new Vector3(x, 1.3f, middle), new Vector3(0.02f, 0.2f, length - 0.5f));
                for (float z = 0.75f; z < length - 0.5f; z += 1.5f)
                {
                    m.Color(rib).Box(new Vector3(side * (width * 0.5f + 0.025f), (top + bottom) * 0.5f, z), new Vector3(0.05f, top - bottom - 0.12f, 0.12f));
                }
            }
            m.Color(Swatch.WagonTrim).Box(new Vector3(0f, top - 0.16f, -0.006f), new Vector3(width - 0.2f, 0.1f, 0.02f));
            m.Color(Swatch.WagonDark).Box(new Vector3(0f, 0.58f, -0.08f), new Vector3(2.1f, 0.22f, 0.16f));
            for (int side = -1; side <= 1; side += 2)
            {
                m.Push(new Vector3(side * 0.72f, 0.9f, -0.01f), Quaternion.Euler(-90f, 0f, 0f));
                m.Color(Swatch.MetalDark).Cylinder(new Vector3(0f, -0.02f, 0f), 0.14f, 0.14f, 0.04f, 10);
                m.Color(Swatch.GlowYellow).Cylinder(new Vector3(0f, 0.02f, 0f), 0.1f, 0.1f, 0.03f, 10);
                m.Pop();
                m.Color(Swatch.MetalDark).Box(new Vector3(side * 0.25f, 1.45f, -0.045f), new Vector3(0.05f, 1.8f, 0.05f));
            }
            for (float y = 0.75f; y < 2.3f; y += 0.3f)
            {
                m.Color(Swatch.Metal).Box(new Vector3(0f, y, -0.045f), new Vector3(0.5f, 0.04f, 0.04f));
            }
            m.Color(rib).Box(new Vector3(0f, top + 0.006f, middle), new Vector3(width - 0.6f, 0.012f, length - 0.6f));
            return m;
        }

        /// <summary>A wooden ramp up to wagon height. Pivot at the middle of its lower edge.</summary>
        public static MeshBuilder Ramp(float width, float height, float length)
        {
            var m = new MeshBuilder();
            m.Color(Swatch.WoodDark).Wedge(width - 0.2f, height - 0.08f, length);
            float slope = Mathf.Sqrt(length * length + height * height);
            float angle = Mathf.Atan2(height, length) * Mathf.Rad2Deg;
            const int planks = 12;
            for (int i = 0; i < planks; i++)
            {
                float s = (i + 0.5f) / planks;
                m.Push(new Vector3(0f, s * height - 0.02f, s * length), Quaternion.Euler(-angle, 0f, 0f));
                m.Color(i % 2 == 0 ? Swatch.Wood : Swatch.WoodLight).Box(Vector3.zero, new Vector3(width, 0.08f, slope / planks * 0.94f));
                m.Pop();
            }
            for (int side = -1; side <= 1; side += 2)
            {
                m.Push(new Vector3(side * width * 0.5f, height * 0.5f + 0.05f, length * 0.5f), Quaternion.Euler(-angle, 0f, 0f));
                m.Color(Swatch.WoodDark).Box(Vector3.zero, new Vector3(0.1f, 0.14f, slope));
                m.Pop();
            }
            for (int i = 0; i < 3; i++)
            {
                float s = 0.25f + i * 0.25f;
                m.Push(new Vector3(0f, s * height + 0.04f, s * length), Quaternion.Euler(-angle, 0f, 0f));
                m.Color(Swatch.HazardYellow);
                m.Push(new Vector3(-0.13f, 0f, 0f), Quaternion.Euler(0f, 45f, 0f));
                m.Box(Vector3.zero, new Vector3(0.36f, 0.02f, 0.1f));
                m.Pop();
                m.Push(new Vector3(0.13f, 0f, 0f), Quaternion.Euler(0f, -45f, 0f));
                m.Box(Vector3.zero, new Vector3(0.36f, 0.02f, 0.1f));
                m.Pop();
                m.Pop();
            }
            return m;
        }

        /// <summary>Collision shape of a ramp: the slope runs on a little past the wagon so the runner never catches its edge.</summary>
        public static MeshBuilder RampCollider(float width, float height, float length, float overlap)
        {
            var m = new MeshBuilder();
            float total = length + overlap;
            m.Wedge(width, height * total / length, total);
            return m;
        }

        /// <summary>Body of a runaway mine cart full of gold. Pivot at the middle of its base; it rolls toward -z.</summary>
        public static MeshBuilder CartBody()
        {
            var m = new MeshBuilder();
            m.Color(Swatch.Charcoal).Box(new Vector3(0f, 0.3f, 0f), new Vector3(1.3f, 0.14f, 1.8f));
            m.Color(Swatch.Wood).BeveledBox(new Vector3(0f, 0.8f, 0f), new Vector3(1.6f, 0.86f, 2f), 0.06f);
            foreach (float z in new[] { -0.75f, 0f, 0.75f })
            {
                m.Color(Swatch.MetalDark).Box(new Vector3(0f, 0.8f, z), new Vector3(1.66f, 0.9f, 0.08f));
            }
            m.Color(Swatch.MetalDark).Box(new Vector3(0f, 1.26f, -1.02f), new Vector3(1.7f, 0.08f, 0.08f));
            m.Color(Swatch.MetalDark).Box(new Vector3(0f, 1.26f, 1.02f), new Vector3(1.7f, 0.08f, 0.08f));
            m.Color(Swatch.MetalDark).Box(new Vector3(-0.83f, 1.26f, 0f), new Vector3(0.08f, 0.08f, 2.1f));
            m.Color(Swatch.MetalDark).Box(new Vector3(0.83f, 1.26f, 0f), new Vector3(0.08f, 0.08f, 2.1f));
            var nuggets = new[]
            {
                new Vector3(-0.4f, 1.28f, -0.5f), new Vector3(0.35f, 1.3f, -0.4f), new Vector3(0f, 1.36f, 0.1f),
                new Vector3(-0.35f, 1.3f, 0.55f), new Vector3(0.4f, 1.28f, 0.5f), new Vector3(0.05f, 1.26f, -0.8f)
            };
            for (int i = 0; i < nuggets.Length; i++)
            {
                m.Color(i % 3 == 0 ? Swatch.GlowYellow : Swatch.Gold).Blob(nuggets[i], new Vector3(0.28f, 0.2f, 0.26f), 0, 0.2f, 40 + i);
            }
            m.Color(Swatch.MetalDark).Cylinder(new Vector3(0f, 0.8f, -1.04f), 0.13f, 0.13f, 0.05f, 10);
            m.Push(new Vector3(0f, 0.95f, -1.02f), Quaternion.Euler(-90f, 0f, 0f));
            m.Color(Swatch.GlowWarm).Cylinder(Vector3.zero, 0.11f, 0.11f, 0.05f, 10);
            m.Pop();
            return m;
        }

        /// <summary>A cart wheel with spokes, turning around x. Pivot at its axle.</summary>
        public static MeshBuilder Wheel()
        {
            var m = new MeshBuilder();
            m.Push(Vector3.zero, Quaternion.Euler(0f, 0f, 90f));
            m.Color(Swatch.Charcoal).Cylinder(new Vector3(0f, -0.06f, 0f), 0.3f, 0.3f, 0.12f, 12);
            m.Color(Swatch.Metal).Cylinder(new Vector3(0f, -0.08f, 0f), 0.08f, 0.08f, 0.16f, 8);
            m.Pop();
            m.Color(Swatch.Metal).Box(new Vector3(0f, 0f, 0f), new Vector3(0.14f, 0.5f, 0.06f));
            m.Color(Swatch.Metal).Box(new Vector3(0f, 0f, 0f), new Vector3(0.14f, 0.06f, 0.5f));
            return m;
        }

        /// <summary>A plank bridge across a chasm. Pivot at the middle of its near end.</summary>
        public static MeshBuilder Bridge(float length)
        {
            var m = new MeshBuilder();
            int planks = Mathf.RoundToInt(length / 0.3f);
            for (int i = 0; i < planks; i++)
            {
                float z = (i + 0.5f) * length / planks;
                m.Color(i % 2 == 0 ? Swatch.Wood : Swatch.WoodLight).Box(new Vector3(0f, -0.06f, z), new Vector3(2.3f, 0.1f, length / planks * 0.9f));
            }
            for (int side = -1; side <= 1; side += 2)
            {
                m.Color(Swatch.WoodDark).Box(new Vector3(side * 1.1f, -0.16f, length * 0.5f), new Vector3(0.14f, 0.14f, length));
                m.Color(Swatch.WoodDark).Cylinder(new Vector3(side * 1.15f, -0.1f, 0.1f), 0.06f, 0.05f, 1.1f, 6);
                m.Color(Swatch.WoodDark).Cylinder(new Vector3(side * 1.15f, -0.1f, length - 0.1f), 0.06f, 0.05f, 1.1f, 6);
                var rope = new List<Vector3>();
                for (int i = 0; i <= 12; i++)
                {
                    float t = i / 12f;
                    rope.Add(new Vector3(side * 1.15f, 0.95f - Mathf.Sin(t * Mathf.PI) * 0.25f, Mathf.Lerp(0.1f, length - 0.1f, t)));
                }
                m.Color(Swatch.WoodLight).Tube(rope, 0.03f, 5);
            }
            return m;
        }

        /// <summary>Base of a bounce pad: rim and springs. Pivot at the middle of its base.</summary>
        public static MeshBuilder JumpPadBase()
        {
            var m = new MeshBuilder();
            m.Color(Swatch.MetalDark).Cylinder(Vector3.zero, 0.98f, 0.92f, 0.16f, 18);
            m.Color(Swatch.HazardYellow).Torus(new Vector3(0f, 0.17f, 0f), 0.86f, 0.07f, 22, 6);
            for (int i = 0; i < 6; i++)
            {
                float angle = i * Mathf.PI / 3f;
                Vector3 center = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 0.55f;
                var coil = new List<Vector3>();
                for (int k = 0; k <= 24; k++)
                {
                    float t = k / 24f;
                    float turn = t * Mathf.PI * 2f * 3f;
                    coil.Add(center + new Vector3(Mathf.Cos(turn) * 0.08f, 0.14f + t * 0.16f, Mathf.Sin(turn) * 0.08f));
                }
                m.Color(Swatch.Silver).Tube(coil, 0.018f, 5);
            }
            return m;
        }

        /// <summary>The bouncy top of a pad with an arrow on it. Pivot at its underside.</summary>
        public static MeshBuilder JumpPadMembrane()
        {
            var m = new MeshBuilder();
            m.Color(Swatch.GlowGreen).Cylinder(Vector3.zero, 0.8f, 0.78f, 0.07f, 18);
            m.Push(new Vector3(0f, 0.075f, 0f), Quaternion.Euler(90f, 0f, 0f));
            m.Color(Swatch.White).Prism(new[] { new Vector2(-0.14f, -0.4f), new Vector2(0.14f, -0.4f), new Vector2(0.14f, 0.02f), new Vector2(-0.14f, 0.02f) }, 0.02f);
            m.Color(Swatch.White).Prism(new[] { new Vector2(-0.34f, 0.02f), new Vector2(0.34f, 0.02f), new Vector2(0f, 0.42f) }, 0.02f);
            m.Pop();
            return m;
        }

        // ------------------------------------------------------------------ pickups

        /// <summary>A coin standing on its edge, with a star on both faces. Pivot at its centre.</summary>
        public static MeshBuilder Coin()
        {
            var m = new MeshBuilder();
            m.Push(Vector3.zero, Quaternion.Euler(90f, 0f, 0f));
            m.Color(Swatch.Gold).Cylinder(new Vector3(0f, -0.05f, 0f), 0.4f, 0.4f, 0.1f, 24, true);
            m.Color(Swatch.Gold).Torus(new Vector3(0f, 0.05f, 0f), 0.35f, 0.035f, 24, 6);
            m.Color(Swatch.Gold).Torus(new Vector3(0f, -0.05f, 0f), 0.35f, 0.035f, 24, 6);
            m.Pop();
            m.Color(Swatch.Gold).Prism(MeshBuilder.StarOutline(5, 0.2f, 0.09f), 0.15f);
            return m;
        }

        /// <summary>A cut gem. Pivot at its centre.</summary>
        public static MeshBuilder Gem()
        {
            var m = new MeshBuilder();
            m.Color(Swatch.GlowMagenta).Cylinder(Vector3.zero, 0.34f, 0.2f, 0.16f, 8);
            m.Color(Swatch.GlowPurple).Cylinder(new Vector3(0f, -0.38f, 0f), 0f, 0.34f, 0.38f, 8, false, false, false);
            return m;
        }

        /// <summary>A horseshoe magnet. Pivot at its centre.</summary>
        public static MeshBuilder Magnet()
        {
            var m = new MeshBuilder();
            m.Push(new Vector3(0f, -0.05f, 0f), Quaternion.Euler(-90f, 0f, 0f));
            m.Color(Swatch.Red).Torus(Vector3.zero, 0.24f, 0.1f, 12, 8, 180f, true, 180f);
            m.Pop();
            for (int side = -1; side <= 1; side += 2)
            {
                m.Color(Swatch.Red).Cylinder(new Vector3(side * 0.24f, -0.05f, 0f), 0.1f, 0.1f, 0.2f, 10, true, false, false);
                m.Color(Swatch.GlowWhite).Cylinder(new Vector3(side * 0.24f, 0.15f, 0f), 0.1f, 0.1f, 0.13f, 10, true);
            }
            return m;
        }

        /// <summary>A heater shield with a star. Pivot at its centre.</summary>
        public static MeshBuilder Shield()
        {
            var outline = new[]
            {
                new Vector2(-0.3f, 0.3f), new Vector2(0.3f, 0.3f), new Vector2(0.3f, 0.02f), new Vector2(0.22f, -0.16f),
                new Vector2(0.12f, -0.28f), new Vector2(0f, -0.38f), new Vector2(-0.12f, -0.28f), new Vector2(-0.22f, -0.16f),
                new Vector2(-0.3f, 0.02f)
            };
            var rim = new Vector2[outline.Length];
            for (int i = 0; i < outline.Length; i++)
            {
                rim[i] = outline[i] * 1.14f;
            }
            var m = new MeshBuilder();
            m.Color(Swatch.Gold).Prism(rim, 0.08f);
            m.Color(Swatch.GlowBlue).Prism(outline, 0.12f);
            m.Color(Swatch.GlowWhite).Prism(MeshBuilder.StarOutline(5, 0.14f, 0.06f), 0.14f, new Vector3(0f, 0.02f, 0f));
            return m;
        }

        /// <summary>A double star: the coin multiplier. Pivot at its centre.</summary>
        public static MeshBuilder StarPower()
        {
            var m = new MeshBuilder();
            m.Color(Swatch.GlowPurple).Prism(MeshBuilder.StarOutline(5, 0.36f, 0.16f), 0.14f);
            m.Color(Swatch.GlowYellow).Prism(MeshBuilder.StarOutline(5, 0.17f, 0.08f), 0.18f);
            return m;
        }

        /// <summary>A sneaker on a spring: super jump. Pivot at its centre.</summary>
        public static MeshBuilder SpringShoe()
        {
            var m = new MeshBuilder();
            var coil = new List<Vector3>();
            for (int k = 0; k <= 48; k++)
            {
                float t = k / 48f;
                float turn = t * Mathf.PI * 2f * 4f;
                coil.Add(new Vector3(Mathf.Cos(turn) * 0.13f, -0.38f + t * 0.3f, Mathf.Sin(turn) * 0.13f));
            }
            m.Color(Swatch.GlowGreen).Tube(coil, 0.03f, 6);
            m.Color(Swatch.Shoe).BeveledBox(new Vector3(0f, 0.02f, 0.04f), new Vector3(0.26f, 0.16f, 0.44f), 0.06f);
            m.Color(Swatch.GlowGreen).BeveledBox(new Vector3(0f, -0.07f, 0.04f), new Vector3(0.28f, 0.05f, 0.46f), 0.02f);
            m.Color(Swatch.GlowGreen).Box(new Vector3(0.132f, 0.03f, 0.04f), new Vector3(0.01f, 0.05f, 0.26f));
            m.Color(Swatch.GlowGreen).Box(new Vector3(-0.132f, 0.03f, 0.04f), new Vector3(0.01f, 0.05f, 0.26f));
            for (int side = -1; side <= 1; side += 2)
            {
                m.Push(new Vector3(side * 0.16f, 0.08f, -0.12f), Quaternion.Euler(0f, side * 20f, side * -35f));
                m.Color(Swatch.White).BeveledBox(new Vector3(side * 0.1f, 0f, 0f), new Vector3(0.22f, 0.03f, 0.14f), 0.01f);
                m.Pop();
            }
            return m;
        }

        // ------------------------------------------------------------------ track

        /// <summary>
        /// A road tile: road (submesh 0), lane stripes (1), curbs (2) and ground (3). A chasm tile leaves a gap between
        /// <paramref name="gapStart"/> and <paramref name="gapEnd"/> with rock walls (4) down to the water.
        /// </summary>
        public static MeshBuilder RoadTile(bool chasm, float gapStart, float gapEnd)
        {
            var m = new MeshBuilder();
            var spans = chasm ? new[] { new Vector2(0f, gapStart), new Vector2(gapEnd, TileLength) } : new[] { new Vector2(0f, TileLength) };
            float curbOuter = RoadHalfWidth + CurbWidth;
            foreach (Vector2 span in spans)
            {
                float z0 = span.x;
                float z1 = span.y;
                float middle = (z0 + z1) * 0.5f;
                float halfLength = (z1 - z0) * 0.5f;

                m.Submesh(0).Project(new Vector3(1f / 3f, 0f, 0f), new Vector3(0f, 0f, 1f / 3f));
                m.Plane(new Vector3(0f, 0f, middle), new Vector3(RoadHalfWidth, 0f, 0f), new Vector3(0f, 0f, halfLength), Vector3.up);

                m.Submesh(1).Color(Swatch.White);
                foreach (float x in new[] { -LayoutBuilder.LaneWidth * 0.5f, LayoutBuilder.LaneWidth * 0.5f })
                {
                    float dashStart = Mathf.Max(z0, 1.2f);
                    float dashEnd = Mathf.Min(z1, 4.2f);
                    if (dashEnd > dashStart + 0.05f)
                    {
                        m.Plane(new Vector3(x, 0.006f, (dashStart + dashEnd) * 0.5f), new Vector3(0.07f, 0f, 0f), new Vector3(0f, 0f, (dashEnd - dashStart) * 0.5f), Vector3.up);
                    }
                }
                foreach (float x in new[] { -RoadHalfWidth + 0.25f, RoadHalfWidth - 0.25f })
                {
                    m.Plane(new Vector3(x, 0.006f, middle), new Vector3(0.06f, 0f, 0f), new Vector3(0f, 0f, halfLength), Vector3.up);
                }

                m.Submesh(2).Project(new Vector3(0f, 0f, 1f / 1.5f), new Vector3(0f, 1f, 0f));
                for (int side = -1; side <= 1; side += 2)
                {
                    m.Box(new Vector3(side * (RoadHalfWidth + CurbWidth * 0.5f), 0.03f, middle), new Vector3(CurbWidth, 0.3f, z1 - z0));
                }

                m.Submesh(3).Project(new Vector3(1f / 3f, 0f, 0f), new Vector3(0f, 0f, 1f / 3f));
                for (int side = -1; side <= 1; side += 2)
                {
                    float center = side * (curbOuter + GroundHalfWidth) * 0.5f;
                    m.Plane(new Vector3(center, -0.02f, middle), new Vector3((GroundHalfWidth - curbOuter) * 0.5f, 0f, 0f), new Vector3(0f, 0f, halfLength), Vector3.up);
                }
            }
            if (chasm)
            {
                m.Submesh(4).Project(new Vector3(1f / 3f, 0f, 0f), new Vector3(0f, 1f / 3f, 0f));
                const float depth = -3f;
                m.QuadFacing(new Vector3(-GroundHalfWidth, depth, gapStart), new Vector3(GroundHalfWidth, depth, gapStart),
                    new Vector3(GroundHalfWidth, 0.18f, gapStart), new Vector3(-GroundHalfWidth, 0.18f, gapStart), Vector3.forward);
                m.QuadFacing(new Vector3(-GroundHalfWidth, depth, gapEnd), new Vector3(GroundHalfWidth, depth, gapEnd),
                    new Vector3(GroundHalfWidth, 0.18f, gapEnd), new Vector3(-GroundHalfWidth, 0.18f, gapEnd), Vector3.back);
            }
            return m;
        }

        /// <summary>The water (or lava) filling a chasm; its material comes from the theme.</summary>
        public static MeshBuilder ChasmFill(float gapStart, float gapEnd)
        {
            var m = new MeshBuilder();
            m.Project(new Vector3(0.25f, 0f, 0f), new Vector3(0f, 0f, 0.25f));
            m.Plane(new Vector3(0f, WaterLevel, (gapStart + gapEnd) * 0.5f), new Vector3(GroundHalfWidth, 0f, 0f), new Vector3(0f, 0f, (gapEnd - gapStart) * 0.5f + 0.1f), Vector3.up);
            return m;
        }

        /// <summary>The finish arch with a chequered banner, balloons and a chequered line. Pivot on the finish line.</summary>
        public static MeshBuilder FinishArch()
        {
            var m = new MeshBuilder();
            const float pillarX = 5.1f;
            const float bannerBottom = 4.5f;
            const float bannerTop = 5.8f;
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * pillarX;
                m.Color(Swatch.DarkGray).BeveledBox(new Vector3(x, 0.15f, 0f), new Vector3(0.9f, 0.3f, 0.9f), 0.06f);
                for (int band = 0; band < 6; band++)
                {
                    m.Color(band % 2 == 0 ? Swatch.White : Swatch.FlagRed).Cylinder(new Vector3(x, 0.3f + band * 1.0f, 0f), 0.3f, 0.3f, 1f, 12, true, band == 0, band == 5);
                }
                m.Color(Swatch.Gold).Sphere(new Vector3(x, 6.4f, 0f), 0.35f, 8, 12);
                Swatch[] balloons = { Swatch.Red, Swatch.Yellow, Swatch.Blue, Swatch.Green, Swatch.Pink };
                for (int i = 0; i < 3; i++)
                {
                    var offset = new Vector3(side * (0.2f + i * 0.35f) - side * 0.4f, 7.5f + (i % 2) * 0.5f, (i - 1) * 0.45f);
                    Vector3 balloon = new Vector3(x, 0f, 0f) + offset;
                    m.Color(balloons[(i + (side > 0 ? 2 : 0)) % balloons.Length]).Sphere(balloon, new Vector3(0.42f, 0.52f, 0.42f), 8, 12);
                    m.Color(Swatch.White).Rod(new Vector3(x, 6.6f, 0f), balloon - Vector3.up * 0.5f, 0.012f, 4);
                }
            }
            var banner = new Rect(-pillarX, bannerBottom, pillarX * 2f, bannerTop - bannerBottom);
            const float square = 0.325f;
            int columns = Mathf.RoundToInt(banner.width / square);
            int rows = Mathf.RoundToInt(banner.height / square);
            float cellWidth = banner.width / columns;
            float cellHeight = banner.height / rows;
            for (int column = 0; column < columns; column++)
            {
                for (int row = 0; row < rows; row++)
                {
                    m.Color((column + row) % 2 == 0 ? Swatch.White : Swatch.Black)
                        .Box(new Vector3(banner.xMin + (column + 0.5f) * cellWidth, banner.yMin + (row + 0.5f) * cellHeight, 0f), new Vector3(cellWidth, cellHeight, 0.16f));
                }
            }
            m.Color(Swatch.FlagRed).Box(new Vector3(0f, bannerBottom - 0.06f, 0f), new Vector3(banner.width, 0.12f, 0.2f));
            m.Color(Swatch.FlagRed).Box(new Vector3(0f, bannerTop + 0.06f, 0f), new Vector3(banner.width, 0.12f, 0.2f));
            const int lineColumns = 16;
            float lineCell = RoadHalfWidth * 2f / lineColumns;
            for (int column = 0; column < lineColumns; column++)
            {
                for (int row = 0; row < 2; row++)
                {
                    m.Color((column + row) % 2 == 0 ? Swatch.White : Swatch.Black)
                        .Box(new Vector3(-RoadHalfWidth + (column + 0.5f) * lineCell, 0.005f, (row - 0.5f) * lineCell), new Vector3(lineCell, 0.01f, lineCell));
                }
            }
            return m;
        }

        /// <summary>A glowing ring (magnet aura, spring boots). Pivot at its centre.</summary>
        public static MeshBuilder Ring(float radius, float thickness, Swatch color)
        {
            var m = new MeshBuilder();
            m.Color(color).Torus(Vector3.zero, radius, thickness, 28, 6);
            return m;
        }
    }
}
