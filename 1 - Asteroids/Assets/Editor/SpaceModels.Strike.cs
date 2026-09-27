using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Asteroids.EditorTools
{
    /// <summary>
    /// The procedural models of the strike mode, coloured through the palette like the other generated models: the
    /// gunship and its rotors, the hulls and weapon parts of the nine bosses, and the money pickups. As in the rest of
    /// <see cref="SpaceModels"/>, +Y points to the camera and +Z is the nose; ground models stand on y = 0, so a prefab
    /// puts their base on the ground depth. Sizes are in meters.
    /// </summary>
    internal static partial class SpaceModels
    {
        // ------------------------------------------------------------------ aircraft

        /// <summary>
        /// The gunship's fuselage (4 m long): a rounded body with a glass canopy, stub wings carrying two rocket pods, a tail
        /// boom and a fin. Its main rotor is <see cref="RotorBlades"/>, turned by a particle on the prefab.
        /// </summary>
        public static MeshBuilder GunshipBody()
        {
            var b = new MeshBuilder();
            b.Color(Swatch.HullDark);
            b.Sphere(new Vector3(0f, 0.55f, 0.35f), new Vector3(0.62f, 0.5f, 1.35f), 8, 14, true);
            b.Color(Swatch.Gunmetal);
            b.Rod(new Vector3(0f, 0.6f, -0.7f), new Vector3(0f, 0.72f, -2.25f), 0.22f, 8, true, 0.12f);
            b.Color(Swatch.Trim);
            b.BeveledBox(new Vector3(0f, 0.95f, -2.1f), new Vector3(0.08f, 0.55f, 0.5f), 0.02f);
            b.BeveledBox(new Vector3(0f, 0.75f, -2.2f), new Vector3(0.9f, 0.06f, 0.3f), 0.02f);
            b.Color(Swatch.GlowRed);
            b.Sphere(new Vector3(0f, 0.78f, 1.1f), new Vector3(0.36f, 0.26f, 0.5f), 6, 10, true);
            b.Color(Swatch.Panel);
            b.BeveledBox(new Vector3(0f, 0.55f, 0.1f), new Vector3(2.4f, 0.1f, 0.45f), 0.03f);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Color(Swatch.DarkRed);
                b.Rod(new Vector3(side * 1.05f, 0.45f, -0.35f), new Vector3(side * 1.05f, 0.45f, 0.75f), 0.17f, 8, true);
                b.Color(Swatch.Black);
                b.Cylinder(new Vector3(side * 1.05f, 0.45f, 0.75f), 0.12f, 0.12f, 0.02f, 8);
                b.Color(Swatch.Hazard);
                b.BeveledBox(new Vector3(side * 1.05f, 0.62f, 0.2f), new Vector3(0.1f, 0.02f, 0.3f), 0.005f);
            }
            b.Color(Swatch.Black);
            b.Cylinder(new Vector3(0f, 1f, 0.2f), 0.18f, 0.12f, 0.25f, 10, true);
            return b;
        }


        /// <summary>A rotor of <paramref name="blades"/> blades, <paramref name="radius"/> long, around a hub (lies in the XZ plane).</summary>
        public static MeshBuilder RotorBlades(int blades, float radius)
        {
            var b = new MeshBuilder();
            b.Color(Swatch.Gunmetal);
            b.Cylinder(new Vector3(0f, -0.06f, 0f), 0.22f, 0.16f, 0.14f, 10, true);
            for (int i = 0; i < blades; i++)
            {
                float angle = i * 360f / blades;
                b.Push(Vector3.zero, Quaternion.Euler(0f, angle, 0f));
                b.Color(Swatch.Black);
                b.BeveledBox(new Vector3(0f, 0f, radius * 0.5f + 0.1f), new Vector3(0.22f, 0.04f, radius), 0.015f);
                b.Color(Swatch.Hazard);
                b.BeveledBox(new Vector3(0f, 0.005f, radius - 0.1f), new Vector3(0.23f, 0.045f, 0.2f), 0.01f);
                b.Pop();
            }
            return b;
        }


        // ------------------------------------------------------------------ boss hulls

        /// <summary>
        /// The crawler: a tracked fortress 10 m long and 7.4 m wide with a raised deck, sockets for two turrets at
        /// (+-1.7, 2.9) and the main cannon at (0, -0.6) (deck height 2.2), exhaust stacks and hazard stripes at the front.
        /// </summary>
        public static MeshBuilder CrawlerHull(bool foundry)
        {
            var b = new MeshBuilder();
            Swatch hull = foundry ? Swatch.Basalt : Swatch.HullDark;
            Swatch deck = foundry ? Swatch.RockDark : Swatch.Panel;
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 3f;
                b.Color(Swatch.Black);
                b.BeveledBox(new Vector3(x, 0.65f, 0f), new Vector3(1.4f, 1.3f, 10f), 0.25f);
                b.Color(Swatch.Gunmetal);
                for (int i = 0; i < 14; i++)
                {
                    b.Box(new Vector3(x, 1.32f, -4.55f + i * 0.7f), new Vector3(1.3f, 0.06f, 0.3f));
                }
                b.Color(Swatch.Trim);
                b.BeveledBox(new Vector3(x - side * 0.8f, 1.2f, 0f), new Vector3(0.25f, 0.5f, 9.2f), 0.06f);
            }
            b.Color(hull);
            b.BeveledBox(new Vector3(0f, 1.1f, 0f), new Vector3(4.8f, 1.4f, 9f), 0.2f);
            b.Color(deck);
            b.BeveledBox(new Vector3(0f, 1.95f, -0.3f), new Vector3(4.2f, 0.5f, 6.6f), 0.15f);
            b.Color(Swatch.Trim);
            b.Cylinder(new Vector3(-1.7f, 2.1f, 2.9f), 0.95f, 0.9f, 0.12f, 16);
            b.Cylinder(new Vector3(1.7f, 2.1f, 2.9f), 0.95f, 0.9f, 0.12f, 16);
            b.Cylinder(new Vector3(0f, 2.1f, -0.6f), 1.5f, 1.4f, 0.12f, 20);
            b.Color(hull);
            b.BeveledBox(new Vector3(0f, 1.5f, 4.35f), new Vector3(4.4f, 0.9f, 0.7f), 0.15f);
            for (int i = 0; i < 6; i++)
            {
                b.Color(i % 2 == 0 ? Swatch.Hazard : Swatch.Black);
                b.Box(new Vector3(-1.75f + i * 0.7f, 1.97f, 4.55f), new Vector3(0.7f, 0.04f, 0.3f));
            }
            for (int side = -1; side <= 1; side += 2)
            {
                b.Color(Swatch.Gunmetal);
                b.Cylinder(new Vector3(side * 1.4f, 2.1f, -3.6f), 0.35f, 0.3f, 0.8f, 10);
                b.Color(foundry ? Swatch.GlowOrange : Swatch.GlowRed);
                b.Cylinder(new Vector3(side * 1.4f, 2.88f, -3.6f), 0.24f, 0.24f, 0.04f, 10);
            }
            b.Color(foundry ? Swatch.GlowOrange : Swatch.GlowYellow);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Sphere(new Vector3(side * 1.9f, 1.4f, 4.72f), 0.18f, 4, 8, true);
            }
            return b;
        }


        /// <summary>A round turret socket (the fixed base of a turret part), 0.3 m high.</summary>
        public static MeshBuilder TurretBase(float radius)
        {
            var b = new MeshBuilder();
            b.Color(Swatch.Gunmetal);
            b.Cylinder(Vector3.zero, radius, radius * 0.9f, 0.3f, 16);
            b.Color(Swatch.Hazard);
            b.Torus(new Vector3(0f, 0.3f, 0f), radius * 0.86f, 0.04f, 20, 4);
            return b;
        }


        /// <summary>
        /// A turret head with <paramref name="barrels"/> barrels pointing along +Z, <paramref name="length"/> long: the
        /// part that turns. The muzzles are at (+-spacing, 0.35, 0.5 + length).
        /// </summary>
        public static MeshBuilder TurretHead(int barrels, float length, float spacing, Swatch body, float size = 1f)
        {
            var b = new MeshBuilder();
            b.Push(Vector3.zero, Quaternion.identity, Vector3.one * size);
            b.Color(body);
            b.BeveledBox(new Vector3(0f, 0.35f, -0.05f), new Vector3(1.1f, 0.5f, 1.2f), 0.12f);
            b.Color(Swatch.Trim);
            b.BeveledBox(new Vector3(0f, 0.63f, -0.2f), new Vector3(0.6f, 0.1f, 0.5f), 0.04f);
            b.Color(Swatch.GlowRed);
            b.BeveledBox(new Vector3(0f, 0.5f, 0.5f), new Vector3(0.5f, 0.12f, 0.1f), 0.02f);
            b.Pop();
            for (int i = 0; i < barrels; i++)
            {
                float x = barrels == 1 ? 0f : Mathf.Lerp(-spacing, spacing, i / (float)(barrels - 1));
                b.Color(Swatch.Black);
                b.Rod(new Vector3(x, 0.35f * size, 0.4f * size), new Vector3(x, 0.35f * size, 0.5f * size + length), 0.09f * size, 8, true);
                b.Color(Swatch.Gunmetal);
                b.Cylinder(new Vector3(x, 0.35f * size, 0.5f * size + length - 0.15f), 0.13f * size, 0.13f * size, 0.02f, 8);
            }
            return b;
        }


        /// <summary>A missile launcher head: a box of 2 x 2 tubes pointing along +Z, glowing tips.</summary>
        public static MeshBuilder LauncherHead()
        {
            var b = new MeshBuilder();
            b.Color(Swatch.DarkGreen);
            b.BeveledBox(new Vector3(0f, 0.55f, 0f), new Vector3(1.4f, 0.8f, 1.5f), 0.1f);
            for (int i = 0; i < 4; i++)
            {
                float x = i % 2 == 0 ? -0.35f : 0.35f;
                float y = i < 2 ? 0.35f : 0.75f;
                b.Color(Swatch.Black);
                b.Cylinder(new Vector3(x, y, 0.76f), 0.22f, 0.22f, 0.02f, 10);
                b.Color(Swatch.GlowOrange);
                b.Cylinder(new Vector3(x, y, 0.77f), 0.12f, 0.12f, 0.02f, 8);
            }
            b.Color(Swatch.Hazard);
            b.Box(new Vector3(0f, 0.97f, -0.4f), new Vector3(1.2f, 0.04f, 0.2f));
            return b;
        }


        /// <summary>A laser emitter: a dome with a glowing lens ring, <paramref name="radius"/> across.</summary>
        public static MeshBuilder LaserEmitter(float radius, Swatch glow)
        {
            var b = new MeshBuilder();
            b.Color(Swatch.Gunmetal);
            b.Cylinder(Vector3.zero, radius, radius * 0.95f, 0.4f, 20);
            b.Color(Swatch.HullDark);
            b.Sphere(new Vector3(0f, 0.4f, 0f), new Vector3(radius * 0.85f, radius * 0.6f, radius * 0.85f), 6, 18, true, 0.5f);
            b.Color(glow);
            b.Torus(new Vector3(0f, 0.42f, 0f), radius * 0.6f, 0.07f, 24, 5);
            b.Sphere(new Vector3(0f, 0.4f + radius * 0.6f, 0f), radius * 0.28f, 5, 10, true);
            for (int i = 0; i < 4; i++)
            {
                b.Push(Vector3.zero, Quaternion.Euler(0f, i * 90f + 45f, 0f));
                b.Color(Swatch.Trim);
                b.BeveledBox(new Vector3(0f, 0.55f, radius * 0.72f), new Vector3(0.2f, 0.35f, 0.3f), 0.04f);
                b.Pop();
            }
            return b;
        }


        /// <summary>The refinery's pump station: a 9 x 7 m concrete platform with tanks, pipes and a reactor well at the centre.</summary>
        public static MeshBuilder PumpStation()
        {
            var b = new MeshBuilder();
            b.Color(Swatch.Panel);
            b.BeveledBox(new Vector3(0f, 0.4f, 0f), new Vector3(9f, 0.8f, 7f), 0.2f);
            b.Color(Swatch.Trim);
            b.BeveledBox(new Vector3(0f, 0.9f, 0f), new Vector3(8.2f, 0.2f, 6.2f), 0.08f);
            float[,] tanks = { { -3.3f, 2.3f }, { 3.3f, 2.3f }, { -3.3f, -2.3f }, { 3.3f, -2.3f } };
            for (int i = 0; i < 4; i++)
            {
                var at = new Vector3(tanks[i, 0], 1f, tanks[i, 1]);
                b.Color(Swatch.Trim);
                b.Cylinder(at, 1.05f, 1.05f, 0.25f, 16);
            }
            b.Color(Swatch.Gunmetal);
            b.Rod(new Vector3(-3.3f, 1.3f, 0f), new Vector3(3.3f, 1.3f, 0f), 0.25f, 10);
            b.Rod(new Vector3(0f, 1.3f, -2.3f), new Vector3(0f, 1.3f, 2.3f), 0.25f, 10);
            b.Color(Swatch.Hazard);
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f;
                b.Push(new Vector3(0f, 1f, 0f), Quaternion.Euler(0f, angle, 0f));
                b.Box(new Vector3(0f, 0.02f, 2.05f), new Vector3(0.6f, 0.04f, 0.25f));
                b.Pop();
            }
            b.Color(Swatch.Black);
            b.Cylinder(new Vector3(0f, 1f, 0f), 1.9f, 1.9f, 0.1f, 24);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Color(Swatch.Gunmetal);
                b.BeveledBox(new Vector3(side * 4.2f, 1.2f, 0f), new Vector3(0.5f, 0.8f, 2.2f), 0.08f);
                b.Color(Swatch.GlowYellow);
                b.Box(new Vector3(side * 4.2f, 1.62f, 0f), new Vector3(0.2f, 0.04f, 1.6f));
            }
            return b;
        }


        /// <summary>A reactor core: a ribbed cylinder under a glowing dome (the refinery's core).</summary>
        public static MeshBuilder ReactorCore(float radius, Swatch glow)
        {
            var b = new MeshBuilder();
            b.Color(Swatch.Gunmetal);
            b.Cylinder(Vector3.zero, radius, radius * 0.92f, 0.9f, 20);
            b.Color(Swatch.Trim);
            for (int i = 0; i < 10; i++)
            {
                b.Push(Vector3.zero, Quaternion.Euler(0f, i * 36f, 0f));
                b.BeveledBox(new Vector3(0f, 0.5f, radius * 0.97f), new Vector3(0.25f, 0.9f, 0.18f), 0.04f);
                b.Pop();
            }
            b.Color(glow);
            b.Sphere(new Vector3(0f, 0.9f, 0f), new Vector3(radius * 0.8f, radius * 0.55f, radius * 0.8f), 6, 18, true, 0.5f);
            b.Color(Swatch.Black);
            b.Torus(new Vector3(0f, 0.92f, 0f), radius * 0.82f, 0.08f, 24, 5);
            return b;
        }


        /// <summary>The sea fortress: an oil rig platform of 12 x 9 m on four legs, a helipad, a derrick and deck sockets.</summary>
        public static MeshBuilder RigPlatform()
        {
            var b = new MeshBuilder();
            for (int i = 0; i < 4; i++)
            {
                float x = i % 2 == 0 ? -5f : 5f;
                float z = i < 2 ? -3.6f : 3.6f;
                b.Color(Swatch.Hazard);
                b.Cylinder(new Vector3(x, 0f, z), 0.7f, 0.7f, 0.5f, 12);
                b.Color(Swatch.Gunmetal);
                b.Cylinder(new Vector3(x, 0.5f, z), 0.6f, 0.6f, 1.2f, 12);
            }
            b.Color(Swatch.HullDark);
            b.BeveledBox(new Vector3(0f, 2f, 0f), new Vector3(12f, 0.8f, 9f), 0.15f);
            b.Color(Swatch.Panel);
            b.Box(new Vector3(0f, 2.42f, 0f), new Vector3(11.4f, 0.05f, 8.4f));
            b.Color(Swatch.Trim);
            for (int i = 0; i < 7; i++)
            {
                b.Box(new Vector3(-5.4f + i * 1.8f, 2.46f, 0f), new Vector3(0.06f, 0.04f, 8.4f));
            }
            // Helipad at the back left.
            b.Color(Swatch.Black);
            b.Cylinder(new Vector3(-3.6f, 2.45f, -2.2f), 1.6f, 1.6f, 0.06f, 24);
            b.Color(Swatch.Yellow);
            b.Torus(new Vector3(-3.6f, 2.52f, -2.2f), 1.35f, 0.06f, 24, 4);
            b.Box(new Vector3(-4f, 2.53f, -2.2f), new Vector3(0.12f, 0.03f, 1.1f));
            b.Box(new Vector3(-3.2f, 2.53f, -2.2f), new Vector3(0.12f, 0.03f, 1.1f));
            b.Box(new Vector3(-3.6f, 2.53f, -2.2f), new Vector3(0.8f, 0.03f, 0.12f));
            // Derrick at the back right.
            b.Color(Swatch.Red);
            for (int i = 0; i < 4; i++)
            {
                float x = 3.6f + (i % 2 == 0 ? -0.8f : 0.8f);
                float z = -2.2f + (i < 2 ? -0.8f : 0.8f);
                b.Rod(new Vector3(x, 2.4f, z), new Vector3(3.6f, 6f, -2.2f), 0.09f, 6, false);
            }
            b.Color(Swatch.GlowRed);
            b.Sphere(new Vector3(3.6f, 6.05f, -2.2f), 0.22f, 4, 8, true);
            // Sockets: launchers at (+-4.2, 2.8), flak at (+-2, 3.3), the laser core at the centre.
            b.Color(Swatch.Trim);
            foreach (Vector3 socket in new[] { new Vector3(-4.2f, 2.45f, 2.8f), new Vector3(4.2f, 2.45f, 2.8f), new Vector3(-1.6f, 2.45f, 3.4f), new Vector3(1.6f, 2.45f, 3.4f) })
            {
                b.Cylinder(socket, 0.95f, 0.95f, 0.08f, 16);
            }
            b.Cylinder(new Vector3(0f, 2.45f, 0.4f), 1.8f, 1.8f, 0.08f, 24);
            return b;
        }


        /// <summary>A concrete pad joining two silos (the coreless boss's body), 12 x 5 m, with the silo sockets at x = +-4.</summary>
        public static MeshBuilder SiloPad()
        {
            var b = new MeshBuilder();
            b.Color(Swatch.Panel);
            b.BeveledBox(new Vector3(0f, 0.3f, 0f), new Vector3(12.5f, 0.6f, 5f), 0.2f);
            b.Color(Swatch.HullDark);
            b.BeveledBox(new Vector3(0f, 0.9f, 0f), new Vector3(3f, 0.8f, 2.4f), 0.12f);
            b.Color(Swatch.Gunmetal);
            b.Rod(new Vector3(-3f, 0.8f, 0f), new Vector3(3f, 0.8f, 0f), 0.3f, 10);
            b.Color(Swatch.GlowRed);
            b.Box(new Vector3(0f, 1.32f, 0.8f), new Vector3(2f, 0.05f, 0.2f));
            for (int i = 0; i < 10; i++)
            {
                b.Color(i % 2 == 0 ? Swatch.Hazard : Swatch.Black);
                b.Box(new Vector3(-5.4f + i * 1.2f, 0.62f, 2.3f), new Vector3(1.2f, 0.04f, 0.3f));
            }
            return b;
        }


        /// <summary>A laser silo: a thick armoured cylinder whose split hatch shows a glowing emitter.</summary>
        public static MeshBuilder Silo(float radius)
        {
            var b = new MeshBuilder();
            b.Color(Swatch.Trim);
            b.Cylinder(Vector3.zero, radius * 1.1f, radius * 1.05f, 0.4f, 24);
            b.Color(Swatch.HullDark);
            b.Cylinder(new Vector3(0f, 0.4f, 0f), radius, radius * 0.95f, 1.4f, 24);
            b.Color(Swatch.Gunmetal);
            b.Box(new Vector3(-radius * 0.5f, 1.85f, 0f), new Vector3(radius * 0.85f, 0.12f, radius * 1.7f));
            b.Box(new Vector3(radius * 0.5f, 1.85f, 0f), new Vector3(radius * 0.85f, 0.12f, radius * 1.7f));
            b.Color(Swatch.GlowRed);
            b.Box(new Vector3(0f, 1.84f, 0f), new Vector3(0.22f, 0.1f, radius * 1.6f));
            b.Color(Swatch.Hazard);
            b.Torus(new Vector3(0f, 1.8f, 0f), radius * 0.97f, 0.05f, 24, 4);
            return b;
        }


        /// <summary>The dome fortress: a hexagonal bastion 12 m across with four turret sockets and a dome well.</summary>
        public static MeshBuilder Bastion()
        {
            var b = new MeshBuilder();
            var outline = new List<Vector2>();
            for (int i = 0; i < 8; i++)
            {
                float angle = (i * 45f + 22.5f) * Mathf.Deg2Rad;
                outline.Add(new Vector2(Mathf.Cos(angle) * 6.4f, Mathf.Sin(angle) * 5.2f));
            }
            b.Color(Swatch.Panel);
            b.Push(new Vector3(0f, 0.6f, 0f), Quaternion.Euler(90f, 0f, 0f));
            b.Prism(outline, 1.2f);
            b.Pop();
            var inner = new List<Vector2>();
            foreach (Vector2 point in outline)
            {
                inner.Add(point * 0.86f);
            }
            b.Color(Swatch.HullDark);
            b.Push(new Vector3(0f, 1.35f, 0f), Quaternion.Euler(90f, 0f, 0f));
            b.Prism(inner, 0.3f);
            b.Pop();
            b.Color(Swatch.Trim);
            foreach (Vector3 socket in new[] { new Vector3(-3.6f, 1.5f, 2.6f), new Vector3(3.6f, 1.5f, 2.6f), new Vector3(-3.6f, 1.5f, -2.6f), new Vector3(3.6f, 1.5f, -2.6f) })
            {
                b.Cylinder(socket, 1f, 1f, 0.1f, 16);
            }
            b.Cylinder(new Vector3(0f, 1.5f, 0f), 2.4f, 2.4f, 0.1f, 28);
            b.Color(Swatch.Hazard);
            b.Torus(new Vector3(0f, 1.62f, 0f), 2.55f, 0.06f, 28, 4);
            for (int i = 0; i < 4; i++)
            {
                b.Push(Vector3.zero, Quaternion.Euler(0f, i * 90f, 0f));
                b.Color(Swatch.Gunmetal);
                b.BeveledBox(new Vector3(0f, 1.2f, 4.6f), new Vector3(2.2f, 0.8f, 0.8f), 0.1f);
                b.Color(Swatch.GlowRed);
                b.Box(new Vector3(0f, 1.62f, 4.95f), new Vector3(1.4f, 0.04f, 0.12f));
                b.Pop();
            }
            return b;
        }


        /// <summary>A flame vent: a grated box with a glowing mouth (the foundry crawler's side weapons).</summary>
        public static MeshBuilder FlameVent()
        {
            var b = new MeshBuilder();
            b.Color(Swatch.Basalt);
            b.BeveledBox(new Vector3(0f, 0.45f, 0f), new Vector3(1.4f, 0.9f, 1.6f), 0.12f);
            b.Color(Swatch.GlowOrange);
            b.Box(new Vector3(0f, 0.92f, 0.1f), new Vector3(1f, 0.04f, 1.1f));
            b.Color(Swatch.Black);
            for (int i = 0; i < 5; i++)
            {
                b.Box(new Vector3(0f, 0.96f, -0.35f + i * 0.2f), new Vector3(1.1f, 0.05f, 0.06f));
            }
            b.Color(Swatch.GlowYellow);
            b.BeveledBox(new Vector3(0f, 0.45f, 0.82f), new Vector3(0.9f, 0.4f, 0.06f), 0.02f);
            return b;
        }


        /// <summary>The Shadow's station core: a dark sphere in a glowing equator, antenna spines and a cannon mouth.</summary>
        public static MeshBuilder StationCore(float radius)
        {
            var b = new MeshBuilder();
            b.Color(Swatch.Basalt);
            b.Sphere(Vector3.zero, new Vector3(radius, radius * 0.7f, radius), 10, 24, true);
            b.Color(Swatch.GlowMagenta);
            b.Torus(Vector3.zero, radius * 1.01f, 0.18f, 36, 6);
            b.Color(Swatch.GlowRed);
            b.Sphere(new Vector3(0f, radius * 0.66f, 0f), radius * 0.28f, 6, 12, true);
            b.Color(Swatch.Gunmetal);
            b.Torus(new Vector3(0f, radius * 0.6f, 0f), radius * 0.36f, 0.1f, 20, 5);
            for (int i = 0; i < 6; i++)
            {
                b.Push(Vector3.zero, Quaternion.Euler(0f, i * 60f + 30f, 0f));
                b.Color(Swatch.CrystalDark);
                b.Rod(new Vector3(0f, radius * 0.3f, radius * 0.7f), new Vector3(0f, radius * 0.1f, radius * 1.35f), 0.2f, 6, false, 0.05f);
                b.Pop();
            }
            return b;
        }


        /// <summary>A ring of the station (lying in the XZ plane), <paramref name="radius"/> across the middle, with <paramref name="mounts"/> mounts.</summary>
        public static MeshBuilder StationRing(float radius, int mounts, Swatch glow)
        {
            var b = new MeshBuilder();
            b.Color(Swatch.HullDark);
            b.Torus(Vector3.zero, radius, 0.35f, 48, 6);
            b.Color(glow);
            b.Torus(new Vector3(0f, 0.2f, 0f), radius, 0.08f, 48, 4);
            for (int i = 0; i < mounts; i++)
            {
                float angle = i * 360f / mounts;
                b.Push(Vector3.zero, Quaternion.Euler(0f, angle, 0f));
                b.Color(Swatch.Trim);
                b.BeveledBox(new Vector3(0f, 0f, radius), new Vector3(1.4f, 0.5f, 1.4f), 0.12f);
                b.Pop();
                b.Push(Vector3.zero, Quaternion.Euler(0f, angle + 180f / mounts, 0f));
                b.Color(Swatch.Gunmetal);
                b.Rod(new Vector3(0f, 0f, radius * 0.25f), new Vector3(0f, 0f, radius - 0.3f), 0.12f, 6, false);
                b.Pop();
            }
            return b;
        }


        /// <summary>A module pod hung under the Skyhammer: a capsule with a hatch and fins, 3 m long along +Z.</summary>
        public static MeshBuilder ModulePod(Swatch body, Swatch glow)
        {
            var b = new MeshBuilder();
            b.Push(Vector3.zero, Quaternion.Euler(90f, 0f, 0f));
            b.Color(body);
            b.Cylinder(new Vector3(0f, -1.1f, 0f), 0.6f, 0.6f, 2.2f, 14, true, false, false);
            b.Color(Swatch.Gunmetal);
            b.Sphere(new Vector3(0f, 1.1f, 0f), 0.6f, 6, 14, true);
            b.Sphere(new Vector3(0f, -1.1f, 0f), 0.6f, 6, 14, true);
            b.Color(glow);
            b.Cylinder(new Vector3(0f, 0.3f, 0f), 0.62f, 0.62f, 0.12f, 14, true, false, false);
            b.Pop();
            b.Color(Swatch.Trim);
            for (int side = -1; side <= 1; side += 2)
            {
                b.BeveledBox(new Vector3(side * 0.75f, 0f, -1.2f), new Vector3(0.5f, 0.08f, 0.6f), 0.02f);
            }
            b.Color(Swatch.Hazard);
            b.Box(new Vector3(0f, 0.6f, 0f), new Vector3(0.3f, 0.02f, 1f));
            return b;
        }


        // ------------------------------------------------------------------ pickups

        /// <summary>Small arms: an olive ammunition crate with a yellow stencil and cartridges on the lid.</summary>
        public static MeshBuilder AmmoCrate()
        {
            var b = new MeshBuilder();
            b.Color(Swatch.DarkGreen);
            b.BeveledBox(new Vector3(0f, 0.25f, 0f), new Vector3(1.1f, 0.5f, 0.75f), 0.05f);
            b.Color(Swatch.Hazard);
            b.Box(new Vector3(0f, 0.51f, 0f), new Vector3(0.7f, 0.02f, 0.14f));
            b.Color(Swatch.Black);
            b.Box(new Vector3(-0.5f, 0.3f, 0f), new Vector3(0.08f, 0.52f, 0.78f));
            b.Box(new Vector3(0.5f, 0.3f, 0f), new Vector3(0.08f, 0.52f, 0.78f));
            for (int i = 0; i < 5; i++)
            {
                float x = -0.32f + i * 0.16f;
                b.Color(Swatch.Gold);
                b.Cylinder(new Vector3(x, 0.52f, 0.22f), 0.05f, 0.05f, 0.22f, 6);
                b.Color(Swatch.GlowYellow);
                b.Cylinder(new Vector3(x, 0.74f, 0.22f), 0.05f, 0f, 0.1f, 6);
            }
            return b;
        }


        /// <summary>Isotopes: a steel canister with glowing green bands and a trefoil cap.</summary>
        public static MeshBuilder IsotopeCanister()
        {
            var b = new MeshBuilder();
            b.Color(Swatch.Chrome);
            b.Cylinder(Vector3.zero, 0.36f, 0.36f, 0.95f, 14, true);
            b.Color(Swatch.GlowGreen);
            b.Cylinder(new Vector3(0f, 0.2f, 0f), 0.37f, 0.37f, 0.12f, 14, true, false, false);
            b.Cylinder(new Vector3(0f, 0.62f, 0f), 0.37f, 0.37f, 0.12f, 14, true, false, false);
            b.Color(Swatch.Hazard);
            b.Cylinder(new Vector3(0f, 0.95f, 0f), 0.3f, 0.26f, 0.08f, 14);
            b.Color(Swatch.Black);
            for (int i = 0; i < 3; i++)
            {
                b.Push(new Vector3(0f, 1.03f, 0f), Quaternion.Euler(0f, i * 120f, 0f));
                b.Box(new Vector3(0f, 0.005f, 0.14f), new Vector3(0.12f, 0.01f, 0.14f));
                b.Pop();
            }
            return b;
        }


        /// <summary>Thaelite: a cluster of blue crystals on a rock.</summary>
        public static MeshBuilder ThaeliteCluster()
        {
            var b = new MeshBuilder();
            b.Color(Swatch.RockDark);
            b.Blob(new Vector3(0f, 0.1f, 0f), new Vector3(0.55f, 0.22f, 0.45f), 1, 0.12f, 71);
            (Vector3 at, Vector3 tilt, float size)[] crystals =
            {
                (new Vector3(0f, 0.15f, 0f), new Vector3(0f, 0f, 0f), 1f), (new Vector3(-0.28f, 0.1f, 0.1f), new Vector3(0f, 0f, 30f), 0.7f),
                (new Vector3(0.3f, 0.1f, -0.05f), new Vector3(10f, 0f, -35f), 0.65f), (new Vector3(0.05f, 0.1f, -0.3f), new Vector3(-35f, 0f, 0f), 0.55f)
            };
            for (int i = 0; i < crystals.Length; i++)
            {
                b.Push(crystals[i].at, Quaternion.Euler(crystals[i].tilt), Vector3.one * crystals[i].size);
                b.Color(i % 2 == 0 ? Swatch.GlowBlue : Swatch.GlowCyan);
                b.Cylinder(Vector3.zero, 0.14f, 0.14f, 0.55f, 6);
                b.Color(Swatch.IceLight);
                b.Cylinder(new Vector3(0f, 0.55f, 0f), 0.14f, 0f, 0.22f, 6);
                b.Pop();
            }
            return b;
        }


        /// <summary>A fusion core: a glowing orange sphere in a steel cage with two rings.</summary>
        public static MeshBuilder FusionCore()
        {
            var b = new MeshBuilder();
            b.Color(Swatch.GlowOrange);
            b.Sphere(new Vector3(0f, 0.45f, 0f), 0.3f, 8, 14, true);
            b.Color(Swatch.Gunmetal);
            b.Cylinder(Vector3.zero, 0.38f, 0.34f, 0.12f, 12);
            b.Cylinder(new Vector3(0f, 0.8f, 0f), 0.34f, 0.3f, 0.1f, 12);
            for (int i = 0; i < 4; i++)
            {
                float angle = (i * 90f + 45f) * Mathf.Deg2Rad;
                var foot = new Vector3(Mathf.Cos(angle) * 0.32f, 0.1f, Mathf.Sin(angle) * 0.32f);
                b.Rod(foot, foot + new Vector3(0f, 0.72f, 0f), 0.035f, 5, false);
            }
            b.Color(Swatch.GlowYellow);
            b.Torus(new Vector3(0f, 0.45f, 0f), 0.42f, 0.03f, 20, 4);
            return b;
        }


        /// <summary>Freylium ore: a dark purple rock veined with glowing magenta crystal.</summary>
        public static MeshBuilder FreyliumOre()
        {
            var b = new MeshBuilder();
            b.Color(Swatch.CrystalDark);
            b.Blob(new Vector3(0f, 0.3f, 0f), new Vector3(0.5f, 0.32f, 0.42f), 1, 0.18f, 211);
            b.Color(Swatch.GlowMagenta);
            b.Push(new Vector3(0.1f, 0.45f, 0.05f), Quaternion.Euler(0f, 0f, -25f));
            b.Cylinder(Vector3.zero, 0.1f, 0f, 0.45f, 5);
            b.Pop();
            b.Push(new Vector3(-0.2f, 0.4f, -0.1f), Quaternion.Euler(20f, 0f, 30f));
            b.Cylinder(Vector3.zero, 0.08f, 0f, 0.35f, 5);
            b.Pop();
            b.Color(Swatch.GlowViolet);
            b.Push(new Vector3(0.05f, 0.35f, 0.3f), Quaternion.Euler(50f, 0f, 0f));
            b.Cylinder(Vector3.zero, 0.07f, 0f, 0.3f, 5);
            b.Pop();
            return b;
        }


        /// <summary>A credit orb: a thick gold coin with a glowing rim.</summary>
        public static MeshBuilder CreditCoin()
        {
            var b = new MeshBuilder();
            b.Color(Swatch.Gold);
            b.Cylinder(new Vector3(0f, -0.06f, 0f), 0.4f, 0.4f, 0.12f, 18, false);
            b.Color(Swatch.GlowYellow);
            b.Torus(Vector3.zero, 0.4f, 0.05f, 18, 4);
            b.Color(Swatch.Yellow);
            b.Box(new Vector3(0f, 0.07f, 0f), new Vector3(0.08f, 0.02f, 0.46f));
            return b;
        }
    }
}
