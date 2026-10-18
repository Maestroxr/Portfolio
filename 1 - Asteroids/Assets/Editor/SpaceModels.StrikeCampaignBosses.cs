using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Asteroids.EditorTools
{
    /// <summary>
    /// The procedural hulls and parts of the Iron Tide's and Hive Fall's bosses, in the palette like the Shadow Strike's
    /// (<see cref="SpaceModels"/>: +Y toward the camera, +Z the nose, metres; ground models stand on y = 0). The Iron Tide
    /// builds in light steel with orange marks, the Hive grows in purple flesh and dark chitin with glowing sacs, the
    /// crystal things are rose and ice with glowing cores. Where a part sits on a hull, the hull has a socket for it and the
    /// prefab builder puts the part at the same place.
    /// </summary>
    internal static partial class SpaceModels
    {
        // ------------------------------------------------------------------ helpers

        /// <summary>A flat outline (x right, y toward the nose) extruded <paramref name="height"/> up from <paramref name="bottom"/>.</summary>
        private static void Slab(MeshBuilder b, IList<Vector2> outline, float bottom, float height)
        {
            b.Push(new Vector3(0f, bottom + height * 0.5f, 0f), Quaternion.Euler(90f, 0f, 0f));
            b.Prism(outline, height);
            b.Pop();
        }


        /// <summary>The outline of a ship's hull, <paramref name="length"/> long and <paramref name="width"/> wide, its bow at +y.</summary>
        private static List<Vector2> ShipOutline(float length, float width, float bow)
        {
            float l = length * 0.5f;
            float w = width * 0.5f;
            return new List<Vector2>
            {
                new Vector2(0f, l), new Vector2(w * 0.55f, l - bow * 0.45f), new Vector2(w * 0.92f, l - bow), new Vector2(w, l - bow * 1.6f),
                new Vector2(w, -l + 1f), new Vector2(w * 0.85f, -l), new Vector2(-w * 0.85f, -l), new Vector2(-w, -l + 1f),
                new Vector2(-w, l - bow * 1.6f), new Vector2(-w * 0.92f, l - bow), new Vector2(-w * 0.55f, l - bow * 0.45f)
            };
        }


        /// <summary>A socket ring for a part on a deck.</summary>
        private static void Socket(MeshBuilder b, Vector3 at, float radius)
        {
            b.Color(Swatch.Trim);
            b.Cylinder(at, radius, radius * 0.96f, 0.1f, 16);
            b.Color(Swatch.Hazard);
            b.Torus(at + Vector3.up * 0.1f, radius * 0.9f, 0.04f, 18, 4);
        }

        // ------------------------------------------------------------------ Iron Tide parts

        /// <summary>A howitzer head: a boxy mantlet and one long barrel with a muzzle brake along +Z (muzzle at (0, 0.45, 3.1)).</summary>
        public static MeshBuilder HowitzerHead()
        {
            var b = new MeshBuilder();
            b.Color(Swatch.Hull);
            b.BeveledBox(new Vector3(0f, 0.42f, -0.1f), new Vector3(1.3f, 0.62f, 1.5f), 0.14f);
            b.Color(Swatch.Orange);
            b.Box(new Vector3(0f, 0.74f, -0.5f), new Vector3(1f, 0.02f, 0.18f));
            b.Color(Swatch.Gunmetal);
            b.Cylinder(new Vector3(0f, 0.45f, 0.62f), 0.26f, 0.26f, 0.08f, 12);
            b.Rod(new Vector3(0f, 0.45f, 0.65f), new Vector3(0f, 0.45f, 2.95f), 0.13f, 10);
            b.Color(Swatch.Black);
            b.BeveledBox(new Vector3(0f, 0.45f, 2.95f), new Vector3(0.36f, 0.28f, 0.32f), 0.04f);
            return b;
        }


        /// <summary>A SAM rack head: four tubes in a tilted box along +Z, orange caps (muzzles at (+-0.25, 0.85, 1)).</summary>
        public static MeshBuilder SamRack()
        {
            var b = new MeshBuilder();
            b.Color(Swatch.Gunmetal);
            b.Cylinder(Vector3.zero, 0.5f, 0.45f, 0.2f, 12);
            b.Push(new Vector3(0f, 0.6f, 0.1f), Quaternion.Euler(-20f, 0f, 0f));
            b.Color(Swatch.Hull);
            b.BeveledBox(Vector3.zero, new Vector3(1f, 0.6f, 1.3f), 0.06f);
            for (int i = 0; i < 4; i++)
            {
                float x = i % 2 == 0 ? -0.25f : 0.25f;
                float y = i < 2 ? -0.13f : 0.15f;
                b.Color(Swatch.Black);
                b.Cylinder(new Vector3(x, y, 0.66f), 0.12f, 0.12f, 0.02f, 8);
                b.Color(Swatch.Orange);
                b.Cylinder(new Vector3(x, y, 0.67f), 0.07f, 0.07f, 0.02f, 8);
            }
            b.Color(Swatch.Orange);
            b.Box(new Vector3(0f, 0.31f, -0.2f), new Vector3(0.8f, 0.02f, 0.16f));
            b.Pop();
            return b;
        }


        /// <summary>A sweeping gun: a squat drum with three short barrels along +Z and a glowing band (muzzles at (-0.3..0.3, 0.4, 1.3)).</summary>
        public static MeshBuilder SweepGun()
        {
            var b = new MeshBuilder();
            b.Color(Swatch.HullDark);
            b.Cylinder(Vector3.zero, 0.75f, 0.68f, 0.55f, 14);
            b.Color(Swatch.GlowOrange);
            b.Cylinder(new Vector3(0f, 0.3f, 0f), 0.77f, 0.77f, 0.08f, 14, false, false, false);
            b.Color(Swatch.Hull);
            b.Sphere(new Vector3(0f, 0.55f, 0f), new Vector3(0.62f, 0.25f, 0.62f), 4, 12, true, 0.5f);
            b.Color(Swatch.Gunmetal);
            for (int i = -1; i <= 1; i++)
            {
                b.Rod(new Vector3(i * 0.3f, 0.4f, 0.5f), new Vector3(i * 0.3f, 0.4f, 1.3f), 0.08f, 6);
            }
            return b;
        }


        /// <summary>A hangar bay module: an armoured box with split doors over a lit launch well (aircraft launch from it).</summary>
        public static MeshBuilder HangarBay()
        {
            var b = new MeshBuilder();
            b.Color(Swatch.HullDark);
            b.BeveledBox(new Vector3(0f, 0.35f, 0f), new Vector3(2.4f, 0.7f, 2.8f), 0.12f);
            b.Color(Swatch.Black);
            b.Box(new Vector3(0f, 0.71f, 0.2f), new Vector3(1.6f, 0.02f, 2f));
            b.Color(Swatch.GlowOrange);
            b.Box(new Vector3(0f, 0.72f, 0.2f), new Vector3(1.2f, 0.02f, 0.12f));
            for (int i = 0; i < 4; i++)
            {
                b.Box(new Vector3(-0.7f + i * 0.47f, 0.72f, 1.05f), new Vector3(0.12f, 0.02f, 0.12f));
            }
            b.Color(Swatch.Hull);
            b.BeveledBox(new Vector3(-0.95f, 0.8f, 0.2f), new Vector3(0.45f, 0.16f, 2.2f), 0.04f);
            b.BeveledBox(new Vector3(0.95f, 0.8f, 0.2f), new Vector3(0.45f, 0.16f, 2.2f), 0.04f);
            b.Color(Swatch.Orange);
            b.Box(new Vector3(0f, 0.72f, -1.25f), new Vector3(2f, 0.03f, 0.14f));
            return b;
        }


        /// <summary>A radar dome on a ring of armour: the Iron Tide's command core, its dish glowing orange.</summary>
        public static MeshBuilder Radome(float radius)
        {
            var b = new MeshBuilder();
            b.Color(Swatch.Gunmetal);
            b.Cylinder(Vector3.zero, radius * 1.1f, radius * 1.05f, 0.5f, 20);
            b.Color(Swatch.Orange);
            b.Cylinder(new Vector3(0f, 0.5f, 0f), radius * 1.06f, radius * 1.06f, 0.08f, 20, false, false, false);
            b.Color(Swatch.White);
            b.Sphere(new Vector3(0f, 0.55f, 0f), new Vector3(radius, radius * 0.8f, radius), 8, 20, true, 0.5f);
            b.Color(Swatch.Panel);
            for (int i = 0; i < 6; i++)
            {
                b.Push(new Vector3(0f, 0.55f, 0f), Quaternion.Euler(0f, i * 30f, 0f));
                b.Torus(Vector3.zero, radius * 1.002f, 0.025f, 24, 3, 180f, false, 0f);
                b.Pop();
            }
            b.Color(Swatch.GlowOrange);
            b.Sphere(new Vector3(0f, 0.55f + radius * 0.8f, 0f), radius * 0.16f, 4, 8);
            return b;
        }

        // ------------------------------------------------------------------ Iron Tide hulls

        /// <summary>
        /// The Glacier Hauler: a tracked ice hauler 11 m long with a cab at the front, two cargo tanks, a reactor well in the
        /// middle; deck at 2.0, sockets for turrets at (+-1.75, 2.2) and SAM racks at (+-1.75, -3.9).
        /// </summary>
        public static MeshBuilder HaulerHull()
        {
            var b = new MeshBuilder();
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 2.95f;
                b.Color(Swatch.Black);
                b.BeveledBox(new Vector3(x, 0.62f, 0f), new Vector3(1.3f, 1.24f, 10.6f), 0.25f);
                b.Color(Swatch.Gunmetal);
                for (int i = 0; i < 15; i++)
                {
                    b.Box(new Vector3(x, 1.26f, -4.9f + i * 0.7f), new Vector3(1.24f, 0.06f, 0.3f));
                }
                b.Color(Swatch.Orange);
                b.BeveledBox(new Vector3(x - side * 0.75f, 1.2f, 0f), new Vector3(0.22f, 0.4f, 9.6f), 0.05f);
            }
            b.Color(Swatch.HullDark);
            b.BeveledBox(new Vector3(0f, 1.1f, 0f), new Vector3(4.7f, 1.6f, 10f), 0.2f);
            b.Color(Swatch.Panel);
            b.BeveledBox(new Vector3(0f, 1.9f, -0.6f), new Vector3(4.3f, 0.2f, 7.4f), 0.06f);
            // The cab with its windows and lamps.
            b.Color(Swatch.Hull);
            b.BeveledBox(new Vector3(0f, 2.3f, 4.4f), new Vector3(4.2f, 1.2f, 1.9f), 0.15f);
            b.Color(Swatch.Black);
            b.Box(new Vector3(0f, 2.55f, 5.36f), new Vector3(3.4f, 0.35f, 0.04f));
            b.Color(Swatch.Orange);
            b.Box(new Vector3(0f, 2.91f, 4.4f), new Vector3(3.8f, 0.03f, 0.25f));
            b.Color(Swatch.GlowYellow);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Sphere(new Vector3(side * 1.7f, 1.6f, 5.05f), 0.16f, 3, 8);
            }
            // Cargo tanks behind the reactor.
            for (int side = -1; side <= 1; side += 2)
            {
                b.Color(Swatch.White);
                b.Cylinder(new Vector3(side * 1.4f, 2f, -1.9f), 0.8f, 0.8f, 1.1f, 16);
                b.Color(Swatch.Orange);
                b.Cylinder(new Vector3(side * 1.4f, 2.6f, -1.9f), 0.81f, 0.81f, 0.18f, 16, false, false, false);
                b.Color(Swatch.Hull);
                b.Sphere(new Vector3(side * 1.4f, 3.1f, -1.9f), new Vector3(0.8f, 0.3f, 0.8f), 3, 16, true, 0.5f);
            }
            b.Color(Swatch.Black);
            b.Cylinder(new Vector3(0f, 2f, 0.2f), 1.25f, 1.25f, 0.05f, 20);
            Socket(b, new Vector3(1.75f, 2f, 2.2f), 0.95f);
            Socket(b, new Vector3(-1.75f, 2f, 2.2f), 0.95f);
            Socket(b, new Vector3(1.75f, 2f, -3.9f), 0.8f);
            Socket(b, new Vector3(-1.75f, 2f, -3.9f), 0.8f);
            return b;
        }


        /// <summary>
        /// The Icebreaker: a ship 15 m long with a red armoured bow, a bridge in the middle (the core) and a funnel; deck at
        /// 1.4, sockets along its middle line for the bow turret (+5.2), the forward howitzer (+2.9), launchers (+-1.3, -3.6)
        /// and a stern turret (-5.8).
        /// </summary>
        public static MeshBuilder IcebreakerHull()
        {
            var b = new MeshBuilder();
            List<Vector2> outline = ShipOutline(15f, 5.2f, 2.2f);
            b.Color(Swatch.Navy);
            Slab(b, outline, 0f, 1f);
            var deck = new List<Vector2>();
            foreach (Vector2 p in outline)
            {
                deck.Add(p * 0.93f);
            }
            b.Color(Swatch.Panel);
            Slab(b, deck, 1f, 0.4f);
            b.Color(Swatch.Red);
            var bow = new List<Vector2> { new Vector2(0f, 7.6f), new Vector2(1.5f, 6.4f), new Vector2(2.45f, 5f), new Vector2(2.65f, 3.9f), new Vector2(-2.65f, 3.9f), new Vector2(-2.45f, 5f), new Vector2(-1.5f, 6.4f) };
            Slab(b, bow, 0f, 1.15f);
            b.Color(Swatch.White);
            b.Box(new Vector3(0f, 1.42f, -7.2f), new Vector3(3.8f, 0.04f, 0.2f));
            // The bridge block and its decks.
            b.Color(Swatch.Hull);
            b.BeveledBox(new Vector3(0f, 2.1f, 0f), new Vector3(3.4f, 1.4f, 3.6f), 0.12f);
            b.BeveledBox(new Vector3(0f, 3f, 0.3f), new Vector3(2.6f, 0.6f, 2.2f), 0.1f);
            b.Color(Swatch.Black);
            b.Box(new Vector3(0f, 3.05f, 1.42f), new Vector3(2.2f, 0.3f, 0.04f));
            b.Color(Swatch.Orange);
            b.Box(new Vector3(0f, 2.82f, 0f), new Vector3(3.42f, 0.08f, 3.62f));
            b.Color(Swatch.Gunmetal);
            b.Rod(new Vector3(0f, 3.3f, -0.4f), new Vector3(0f, 4.4f, -0.4f), 0.06f, 5);
            b.Color(Swatch.GlowRed);
            b.Sphere(new Vector3(0f, 4.45f, -0.4f), 0.12f, 3, 6);
            // The funnel behind the bridge.
            b.Color(Swatch.HullDark);
            b.Cylinder(new Vector3(0f, 1.4f, -1.95f), 0.75f, 0.65f, 1.9f, 12);
            b.Color(Swatch.Orange);
            b.Cylinder(new Vector3(0f, 2.75f, -1.95f), 0.68f, 0.67f, 0.3f, 12, false, false, false);
            b.Color(Swatch.Black);
            b.Cylinder(new Vector3(0f, 3.3f, -1.95f), 0.55f, 0.55f, 0.02f, 12);
            Socket(b, new Vector3(0f, 1.4f, 5.2f), 0.95f);
            Socket(b, new Vector3(0f, 1.4f, 2.9f), 1.05f);
            Socket(b, new Vector3(1.3f, 1.4f, -3.6f), 0.85f);
            Socket(b, new Vector3(-1.3f, 1.4f, -3.6f), 0.85f);
            Socket(b, new Vector3(0f, 1.4f, -5.8f), 0.9f);
            return b;
        }


        /// <summary>
        /// The Gale Carrier: a flying wing 14 m across with a raised bridge in the middle, a hangar socket in each wing
        /// root (+-3.6, -0.4), flak sockets at the wing tips (+-5.6, 1.6) and four engines at the trailing edge.
        /// </summary>
        public static MeshBuilder CarrierHull()
        {
            var b = new MeshBuilder();
            var wing = new List<Vector2>
            {
                new Vector2(0f, 3.8f), new Vector2(2.2f, 3.2f), new Vector2(7.2f, 1.4f), new Vector2(7.4f, 0.4f), new Vector2(6.8f, -1.6f),
                new Vector2(2.4f, -3.4f), new Vector2(-2.4f, -3.4f), new Vector2(-6.8f, -1.6f), new Vector2(-7.4f, 0.4f), new Vector2(-7.2f, 1.4f), new Vector2(-2.2f, 3.2f)
            };
            b.Color(Swatch.HullDark);
            Slab(b, wing, 0f, 0.6f);
            var top = new List<Vector2>();
            foreach (Vector2 p in wing)
            {
                top.Add(new Vector2(p.x * 0.94f, p.y * 0.9f));
            }
            b.Color(Swatch.Hull);
            Slab(b, top, 0.6f, 0.25f);
            b.Color(Swatch.Panel);
            b.BeveledBox(new Vector3(0f, 1f, 0f), new Vector3(3f, 0.9f, 6.2f), 0.2f);
            b.Color(Swatch.Hull);
            b.BeveledBox(new Vector3(0f, 1.6f, 0.6f), new Vector3(2f, 0.5f, 2.6f), 0.15f);
            b.Color(Swatch.Black);
            b.Box(new Vector3(0f, 1.7f, 1.92f), new Vector3(1.6f, 0.22f, 0.04f));
            b.Color(Swatch.Orange);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Box(new Vector3(side * 5f, 0.87f, 1.2f), new Vector3(2.6f, 0.03f, 0.2f));
            }
            for (int i = 0; i < 4; i++)
            {
                float x = i < 2 ? (i == 0 ? -1f : 1f) : (i == 2 ? -4.6f : 4.6f);
                float z = i < 2 ? -3.3f : -2.3f;
                b.Color(Swatch.Gunmetal);
                b.Cylinder(new Vector3(x, 0.45f, z - 0.2f), 0.45f, 0.45f, 0.3f, 12);
                b.Color(Swatch.GlowOrange);
                b.Cylinder(new Vector3(x, 0.45f, z - 0.25f), 0.3f, 0.3f, 0.32f, 12);
            }
            Socket(b, new Vector3(3.6f, 0.85f, -0.4f), 1.35f);
            Socket(b, new Vector3(-3.6f, 0.85f, -0.4f), 1.35f);
            Socket(b, new Vector3(5.6f, 0.85f, 1.6f), 0.9f);
            Socket(b, new Vector3(-5.6f, 0.85f, 1.6f), 0.9f);
            return b;
        }


        /// <summary>
        /// The Reef Bastion: an octagonal sea fort 13 m across on a reef of rocks, gun towers at its four corners
        /// (+-4.3, +-3.1, deck 1.9), two gun pits in the front wall (+-2, 4.4) and the keep in the middle (the core).
        /// </summary>
        public static MeshBuilder AtollFortress()
        {
            var b = new MeshBuilder();
            b.Color(Swatch.Rock);
            b.Blob(new Vector3(0f, 0.15f, 0f), new Vector3(7.2f, 0.5f, 6f), 1, 0.12f, 5101);
            var outline = new List<Vector2>();
            for (int i = 0; i < 8; i++)
            {
                float angle = (i * 45f + 22.5f) * Mathf.Deg2Rad;
                outline.Add(new Vector2(Mathf.Cos(angle) * 6.3f, Mathf.Sin(angle) * 5.3f));
            }
            b.Color(Swatch.Panel);
            Slab(b, outline, 0f, 1.7f);
            var court = new List<Vector2>();
            foreach (Vector2 p in outline)
            {
                court.Add(p * 0.82f);
            }
            b.Color(Swatch.HullDark);
            Slab(b, court, 1.7f, 0.08f);
            b.Color(Swatch.Orange);
            for (int i = 0; i < 8; i++)
            {
                Vector2 a = outline[i];
                Vector2 c = outline[(i + 1) % 8];
                Vector2 mid = (a + c) * 0.5f;
                b.Push(new Vector3(mid.x, 1.72f, mid.y), Quaternion.Euler(0f, -Mathf.Atan2(c.y - a.y, c.x - a.x) * Mathf.Rad2Deg, 0f));
                b.Box(Vector3.zero, new Vector3((c - a).magnitude * 0.6f, 0.04f, 0.18f));
                b.Pop();
            }
            foreach (Vector2 corner in new[] { new Vector2(4.3f, 3.1f), new Vector2(-4.3f, 3.1f), new Vector2(4.3f, -3.1f), new Vector2(-4.3f, -3.1f) })
            {
                b.Color(Swatch.Hull);
                b.Cylinder(new Vector3(corner.x, 0f, corner.y), 1.3f, 1.2f, 1.9f, 12);
                Socket(b, new Vector3(corner.x, 1.9f, corner.y), 1.05f);
            }
            Socket(b, new Vector3(2f, 1.75f, 4.4f), 0.85f);
            Socket(b, new Vector3(-2f, 1.75f, 4.4f), 0.85f);
            b.Color(Swatch.Hull);
            b.Cylinder(new Vector3(0f, 1.7f, 0f), 2.2f, 2f, 0.6f, 16);
            return b;
        }


        /// <summary>
        /// The Sea Wolves: two attack hydrofoils 7.5 m long side by side (x +-3.6), each riding on its foils with a turret
        /// socket at its bow (z +1.9) and a launcher socket at its stern (z -2.2), deck at 1.25.
        /// </summary>
        public static MeshBuilder Hydrofoils()
        {
            var b = new MeshBuilder();
            List<Vector2> outline = ShipOutline(7.5f, 2.6f, 1.6f);
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 3.6f;
                b.Push(new Vector3(x, 0f, 0f));
                b.Color(Swatch.Gunmetal);
                for (int i = -1; i <= 1; i += 2)
                {
                    b.Rod(new Vector3(i * 0.9f, 0f, 1.6f), new Vector3(i * 0.9f, 0.6f, 1.6f), 0.08f, 6);
                    b.Rod(new Vector3(i * 0.9f, 0f, -2f), new Vector3(i * 0.9f, 0.6f, -2f), 0.08f, 6);
                }
                b.Box(new Vector3(0f, 0.05f, 1.6f), new Vector3(2.6f, 0.08f, 0.5f));
                b.Box(new Vector3(0f, 0.05f, -2f), new Vector3(2.2f, 0.08f, 0.4f));
                b.Color(Swatch.Navy);
                Slab(b, outline, 0.6f, 0.55f);
                var deck = new List<Vector2>();
                foreach (Vector2 p in outline)
                {
                    deck.Add(p * 0.9f);
                }
                b.Color(Swatch.Hull);
                Slab(b, deck, 1.15f, 0.1f);
                b.Color(Swatch.Panel);
                b.BeveledBox(new Vector3(0f, 1.55f, -0.2f), new Vector3(1.6f, 0.6f, 1.8f), 0.1f);
                b.Color(Swatch.Black);
                b.Box(new Vector3(0f, 1.62f, 0.72f), new Vector3(1.3f, 0.22f, 0.04f));
                b.Color(Swatch.Orange);
                b.Box(new Vector3(0f, 1.26f, 3.1f), new Vector3(0.6f, 0.02f, 0.6f));
                Socket(b, new Vector3(0f, 1.25f, 1.9f), 0.85f);
                Socket(b, new Vector3(0f, 1.25f, -2.2f), 0.8f);
                b.Pop();
            }
            b.Color(Swatch.Gunmetal);
            b.Rod(new Vector3(-2.4f, 1f, -0.4f), new Vector3(2.4f, 1f, -0.4f), 0.12f, 8);
            return b;
        }


        /// <summary>
        /// The Drydock: a dock basin 14 x 10 m between two concrete walls (deck 2.2), a half-built hull inside it and a
        /// gantry crane across it whose cab is the core; sockets on the walls at (+-5.7, +-2.6).
        /// </summary>
        public static MeshBuilder Drydock()
        {
            var b = new MeshBuilder();
            b.Color(Swatch.Black);
            b.Box(new Vector3(0f, 0.02f, 0f), new Vector3(9f, 0.04f, 10.4f));
            for (int side = -1; side <= 1; side += 2)
            {
                b.Color(Swatch.Panel);
                b.BeveledBox(new Vector3(side * 5.7f, 1.1f, 0f), new Vector3(2.6f, 2.2f, 10.6f), 0.15f);
                b.Color(Swatch.Hazard);
                for (int i = 0; i < 8; i++)
                {
                    b.Box(new Vector3(side * 4.42f, 2.21f, -4.6f + i * 1.3f), new Vector3(0.12f, 0.03f, 0.6f));
                }
                b.Color(Swatch.Trim);
                b.Box(new Vector3(side * 6.4f, 2.22f, 0f), new Vector3(0.3f, 0.04f, 10.2f));
                Socket(b, new Vector3(side * 5.7f, 2.2f, 2.6f), 0.95f);
                Socket(b, new Vector3(side * 5.7f, 2.2f, -2.6f), 0.95f);
            }
            // The half-built warship in the basin.
            List<Vector2> hull = ShipOutline(9.5f, 4f, 1.8f);
            b.Color(Swatch.Rock);
            Slab(b, hull, 0f, 1.3f);
            b.Color(Swatch.HullDark);
            for (int i = 0; i < 6; i++)
            {
                b.Box(new Vector3(0f, 1.32f, -3.5f + i * 1.4f), new Vector3(3.6f, 0.06f, 0.2f));
            }
            // The gantry: legs on the walls, a beam across, the crane cab in the middle.
            b.Color(Swatch.Orange);
            for (int side = -1; side <= 1; side += 2)
            {
                b.BeveledBox(new Vector3(side * 5.7f, 3f, 0f), new Vector3(0.7f, 1.6f, 0.9f), 0.06f);
            }
            b.BeveledBox(new Vector3(0f, 3.9f, 0f), new Vector3(12.2f, 0.5f, 1f), 0.08f);
            b.Color(Swatch.Black);
            for (int i = 0; i < 10; i++)
            {
                b.Box(new Vector3(-5f + i * 1.1f, 4.16f, 0f), new Vector3(0.1f, 0.02f, 0.9f));
            }
            b.Color(Swatch.Hull);
            b.BeveledBox(new Vector3(0f, 3.5f, 0f), new Vector3(2.4f, 1.1f, 2f), 0.12f);
            return b;
        }


        /// <summary>
        /// The Battleship: 18 m long, a raised citadel with the bridge tower in the middle (the core), two main turret
        /// barbettes on its middle line (+-4.6) and secondary sockets along its sides (+-1.4 at +-7 and +-2.4); deck 1.5.
        /// </summary>
        public static MeshBuilder BattleshipHull()
        {
            var b = new MeshBuilder();
            List<Vector2> outline = ShipOutline(18.5f, 5.8f, 3f);
            b.Color(Swatch.Navy);
            Slab(b, outline, 0f, 1.1f);
            var deck = new List<Vector2>();
            foreach (Vector2 p in outline)
            {
                deck.Add(p * 0.94f);
            }
            b.Color(Swatch.Rock);
            Slab(b, deck, 1.1f, 0.4f);
            b.Color(Swatch.HullDark);
            for (int i = 0; i < 16; i++)
            {
                b.Box(new Vector3(0f, 1.51f, -7.6f + i * 1f), new Vector3(4.6f, 0.02f, 0.05f));
            }
            b.Color(Swatch.Hull);
            b.BeveledBox(new Vector3(0f, 2f, 0f), new Vector3(3.6f, 1f, 6.4f), 0.15f);
            b.BeveledBox(new Vector3(0f, 2.9f, 0.4f), new Vector3(2.4f, 0.9f, 2.4f), 0.12f);
            b.BeveledBox(new Vector3(0f, 3.6f, 0.6f), new Vector3(1.6f, 0.6f, 1.4f), 0.1f);
            b.Color(Swatch.Black);
            b.Box(new Vector3(0f, 3.7f, 1.32f), new Vector3(1.3f, 0.22f, 0.04f));
            b.Color(Swatch.Orange);
            b.Box(new Vector3(0f, 2.51f, -2.4f), new Vector3(3.2f, 0.04f, 0.3f));
            b.Color(Swatch.HullDark);
            b.Cylinder(new Vector3(0f, 2.5f, -2f), 0.7f, 0.6f, 1.5f, 12);
            b.Color(Swatch.Black);
            b.Cylinder(new Vector3(0f, 4f, -2f), 0.5f, 0.5f, 0.02f, 12);
            b.Color(Swatch.Gunmetal);
            b.Rod(new Vector3(0f, 4.2f, 0.4f), new Vector3(0f, 5.4f, 0.4f), 0.07f, 5);
            b.Color(Swatch.GlowRed);
            b.Sphere(new Vector3(0f, 5.45f, 0.4f), 0.14f, 3, 6);
            foreach (float z in new[] { 4.6f, -4.6f })
            {
                b.Color(Swatch.HullDark);
                b.Cylinder(new Vector3(0f, 1.5f, z), 1.45f, 1.4f, 0.3f, 16);
                Socket(b, new Vector3(0f, 1.8f, z), 1.3f);
            }
            foreach (float z in new[] { 7f, 2.4f, -2.4f, -7f })
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    Socket(b, new Vector3(side * 1.75f, 1.5f, z), 0.85f);
                }
            }
            return b;
        }


        /// <summary>
        /// The Leviathan: the Iron Tide's flagship, a flying fortress 19 m across: a long armoured hull, two engine nacelles,
        /// broad wings with six gun sockets along their edges, hangar and sweeper sockets inboard, and a glowing reactor
        /// crown in the middle (the core).
        /// </summary>
        public static MeshBuilder LeviathanHull()
        {
            var b = new MeshBuilder();
            var wing = new List<Vector2>
            {
                new Vector2(0f, 5.2f), new Vector2(3f, 3.6f), new Vector2(9.4f, 1.2f), new Vector2(9.6f, -0.6f), new Vector2(8.4f, -2.4f),
                new Vector2(3.4f, -3.2f), new Vector2(2f, -5f), new Vector2(-2f, -5f), new Vector2(-3.4f, -3.2f), new Vector2(-8.4f, -2.4f),
                new Vector2(-9.6f, -0.6f), new Vector2(-9.4f, 1.2f), new Vector2(-3f, 3.6f)
            };
            b.Color(Swatch.HullDark);
            Slab(b, wing, 0f, 0.8f);
            var top = new List<Vector2>();
            foreach (Vector2 p in wing)
            {
                top.Add(new Vector2(p.x * 0.95f, p.y * 0.92f));
            }
            b.Color(Swatch.Hull);
            Slab(b, top, 0.8f, 0.3f);
            b.Color(Swatch.Panel);
            b.BeveledBox(new Vector3(0f, 1.4f, 0f), new Vector3(4.4f, 1f, 9.4f), 0.25f);
            b.Color(Swatch.Hull);
            b.BeveledBox(new Vector3(0f, 2.1f, 0.6f), new Vector3(3.2f, 0.6f, 5f), 0.2f);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Color(Swatch.Gunmetal);
                b.Rod(new Vector3(side * 6.2f, 1.1f, 2.4f), new Vector3(side * 6.2f, 1.1f, -3.2f), 0.75f, 12);
                b.Color(Swatch.GlowOrange);
                b.Cylinder(new Vector3(side * 6.2f, 1.1f, -3.3f), 0.55f, 0.55f, 0.02f, 12);
                b.Color(Swatch.Orange);
                b.Box(new Vector3(side * 4.2f, 1.12f, 0.8f), new Vector3(1.4f, 0.03f, 3.4f));
                b.Color(Swatch.Black);
                b.Box(new Vector3(side * 7.9f, 1.12f, -1.6f), new Vector3(2.4f, 0.03f, 0.25f));
            }
            for (int i = 0; i < 3; i++)
            {
                b.Color(Swatch.Gunmetal);
                b.Cylinder(new Vector3(-1.3f + i * 1.3f, 1f, -4.9f), 0.5f, 0.5f, 0.6f, 12);
                b.Color(Swatch.GlowOrange);
                b.Cylinder(new Vector3(-1.3f + i * 1.3f, 1.2f, -5f), 0.35f, 0.35f, 0.3f, 12);
            }
            // Gun sockets on the wings, hangar and sweeper sockets inboard (see the prefab builder).
            foreach (Vector2 s in new[] { new Vector2(8.2f, 0.4f), new Vector2(5.6f, 1.9f), new Vector2(3.4f, 3f) })
            {
                Socket(b, new Vector3(s.x, 1.1f, s.y), 0.9f);
                Socket(b, new Vector3(-s.x, 1.1f, s.y), 0.9f);
            }
            foreach (Vector2 s in new[] { new Vector2(3.6f, -1.6f), new Vector2(6.6f, -1.2f) })
            {
                Socket(b, new Vector3(s.x, 1.1f, s.y), 1.2f);
                Socket(b, new Vector3(-s.x, 1.1f, s.y), 1.2f);
            }
            return b;
        }


        /// <summary>The Leviathan's reactor crown: armoured petals round a glowing core, lifted over the hull (the core's look).</summary>
        public static MeshBuilder ReactorCrown()
        {
            var b = new MeshBuilder();
            b.Color(Swatch.Gunmetal);
            b.Cylinder(Vector3.zero, 1.9f, 1.7f, 0.5f, 18);
            b.Color(Swatch.GlowOrange);
            b.Sphere(new Vector3(0f, 0.55f, 0f), new Vector3(1.15f, 0.8f, 1.15f), 6, 16, true, 0.5f);
            for (int i = 0; i < 8; i++)
            {
                b.Push(new Vector3(0f, 0.4f, 0f), Quaternion.Euler(0f, i * 45f, 0f));
                b.Color(i % 2 == 0 ? Swatch.Hull : Swatch.HullDark);
                b.Push(new Vector3(0f, 0f, 1.35f), Quaternion.Euler(-28f, 0f, 0f));
                b.BeveledBox(new Vector3(0f, 0.35f, 0f), new Vector3(0.8f, 0.7f, 0.18f), 0.04f);
                b.Pop();
                b.Pop();
            }
            b.Color(Swatch.Orange);
            b.Torus(new Vector3(0f, 0.5f, 0f), 1.75f, 0.06f, 24, 4);
            return b;
        }

        // ------------------------------------------------------------------ Hive parts

        /// <summary>A hive maw: a fleshy neck that ends in a ringed mouth along +Z, glowing inside (muzzle at (0, 0.55, 1.15)).</summary>
        public static MeshBuilder HiveMaw()
        {
            var b = new MeshBuilder();
            b.Color(Swatch.Purple);
            b.Sphere(new Vector3(0f, 0.3f, 0f), new Vector3(0.8f, 0.42f, 0.8f), 5, 12);
            b.Rod(new Vector3(0f, 0.45f, 0.1f), new Vector3(0f, 0.55f, 0.8f), 0.36f, 10, true, 0.3f);
            b.Color(Swatch.CrystalDark);
            b.Torus(new Vector3(0f, 0.55f, 0.95f), 0.32f, 0.1f, 14, 5);
            b.Color(Swatch.GlowGreen);
            b.Cylinder(new Vector3(0f, 0.55f, 0.9f), 0.25f, 0.25f, 0.04f, 12);
            b.Color(Swatch.Basalt);
            for (int i = 0; i < 5; i++)
            {
                float angle = i * 72f * Mathf.Deg2Rad;
                var at = new Vector3(Mathf.Cos(angle) * 0.36f, 0.55f + Mathf.Sin(angle) * 0.36f, 0.95f);
                b.Rod(at, at + new Vector3(Mathf.Cos(angle) * 0.05f, Mathf.Sin(angle) * 0.05f, 0.3f), 0.05f, 4, true, 0.005f);
            }
            return b;
        }


        /// <summary>A spore sac: a swollen pod on a root collar with glowing veins and a mouth on top (the launchers and spore pods).</summary>
        public static MeshBuilder SporeSac(float size)
        {
            var b = new MeshBuilder();
            b.Push(Vector3.zero, Quaternion.identity, Vector3.one * size);
            b.Color(Swatch.Basalt);
            b.Torus(new Vector3(0f, 0.1f, 0f), 0.8f, 0.16f, 14, 5);
            b.Color(Swatch.Magenta);
            b.Sphere(new Vector3(0f, 0.7f, 0f), new Vector3(0.78f, 0.72f, 0.78f), 7, 14);
            b.Color(Swatch.GlowMagenta);
            for (int i = 0; i < 5; i++)
            {
                b.Push(new Vector3(0f, 0.7f, 0f), Quaternion.Euler(0f, i * 72f, 0f));
                b.Torus(Vector3.zero, 0.79f, 0.03f, 16, 3, 120f, false, -60f);
                b.Pop();
            }
            b.Color(Swatch.CrystalDark);
            b.Torus(new Vector3(0f, 1.4f, 0f), 0.24f, 0.08f, 12, 4);
            b.Color(Swatch.GlowGreen);
            b.Cylinder(new Vector3(0f, 1.38f, 0f), 0.18f, 0.18f, 0.04f, 10);
            b.Pop();
            return b;
        }


        /// <summary>A hive heart: a pulsing knot of flesh in a cage of chitin ribs, glowing green in its seams (the Hive's cores).</summary>
        public static MeshBuilder HiveHeart(float radius)
        {
            var b = new MeshBuilder();
            b.Color(Swatch.GlowGreen);
            b.Sphere(new Vector3(0f, radius * 0.55f, 0f), new Vector3(radius * 0.6f, radius * 0.5f, radius * 0.6f), 6, 12);
            b.Color(Swatch.DarkRed);
            b.Blob(new Vector3(0f, radius * 0.6f, 0f), new Vector3(radius * 0.72f, radius * 0.55f, radius * 0.72f), 1, 0.22f, 6101);
            b.Color(Swatch.Basalt);
            for (int i = 0; i < 7; i++)
            {
                float angle = i * Mathf.PI * 2f / 7f;
                var foot = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                var mid = new Vector3(Mathf.Cos(angle) * radius * 0.9f, radius * 0.8f, Mathf.Sin(angle) * radius * 0.9f);
                var tip = new Vector3(Mathf.Cos(angle) * radius * 0.35f, radius * 1.15f, Mathf.Sin(angle) * radius * 0.35f);
                b.Rod(foot, mid, radius * 0.09f, 5);
                b.Rod(mid, tip, radius * 0.07f, 5, true, radius * 0.02f);
            }
            return b;
        }


        /// <summary>A segment of the Burrow Wyrm: a ringed armour plate over a fleshy body, spines along its back.</summary>
        public static MeshBuilder WyrmSegment(float radius)
        {
            var b = new MeshBuilder();
            b.Color(Swatch.Purple);
            b.Sphere(new Vector3(0f, radius * 0.45f, 0f), new Vector3(radius, radius * 0.55f, radius * 0.85f), 6, 14);
            b.Color(Swatch.CrystalDark);
            b.Sphere(new Vector3(0f, radius * 0.6f, -radius * 0.1f), new Vector3(radius * 0.95f, radius * 0.5f, radius * 0.6f), 5, 14, true, 0.55f);
            b.Color(Swatch.GlowGreen);
            b.Torus(new Vector3(0f, radius * 0.45f, radius * 0.55f), radius * 0.72f, 0.05f, 16, 3, 180f, false, 180f);
            b.Color(Swatch.Basalt);
            for (int i = -1; i <= 1; i++)
            {
                var foot = new Vector3(i * radius * 0.4f, radius * 0.9f, -radius * 0.1f);
                b.Rod(foot, foot + new Vector3(i * 0.15f, radius * 0.55f, -radius * 0.35f), radius * 0.1f, 5, true, 0.01f);
            }
            return b;
        }


        /// <summary>The Burrow Wyrm's head: an armoured skull with four mandibles round a glowing maw along +Z.</summary>
        public static MeshBuilder WyrmHead()
        {
            var b = new MeshBuilder();
            b.Color(Swatch.Purple);
            b.Sphere(new Vector3(0f, 0.9f, -0.3f), new Vector3(1.9f, 1f, 1.7f), 7, 16);
            b.Color(Swatch.Basalt);
            b.Sphere(new Vector3(0f, 1.25f, -0.6f), new Vector3(1.7f, 0.8f, 1.4f), 6, 16, true, 0.55f);
            b.Color(Swatch.CrystalDark);
            b.Torus(new Vector3(0f, 0.9f, 1.25f), 0.75f, 0.2f, 16, 6);
            b.Color(Swatch.GlowGreen);
            b.Cylinder(new Vector3(0f, 0.9f, 1.15f), 0.62f, 0.62f, 0.05f, 16);
            b.Color(Swatch.Basalt);
            for (int i = 0; i < 4; i++)
            {
                float angle = (i * 90f + 45f) * Mathf.Deg2Rad;
                var root = new Vector3(Mathf.Cos(angle) * 0.85f, 0.9f + Mathf.Sin(angle) * 0.7f, 1.2f);
                var bend = root + new Vector3(Mathf.Cos(angle) * 0.5f, Mathf.Sin(angle) * 0.35f, 0.9f);
                var tip = bend + new Vector3(-Mathf.Cos(angle) * 0.45f, -Mathf.Sin(angle) * 0.3f, 0.6f);
                b.Rod(root, bend, 0.16f, 6);
                b.Rod(bend, tip, 0.12f, 6, true, 0.01f);
            }
            b.Color(Swatch.GlowMagenta);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Sphere(new Vector3(side * 1f, 1.5f, 0.5f), 0.16f, 3, 8);
                b.Sphere(new Vector3(side * 0.6f, 1.75f, 0.3f), 0.11f, 3, 8);
            }
            return b;
        }


        /// <summary>A spore tower: a tall ribbed column of flesh from a root ring, crowned by a sac with a glowing mouth.</summary>
        public static MeshBuilder SporeTower()
        {
            var b = new MeshBuilder();
            b.Color(Swatch.Basalt);
            b.Torus(new Vector3(0f, 0.15f, 0f), 1.35f, 0.28f, 16, 5);
            b.Color(Swatch.Purple);
            b.Cylinder(new Vector3(0f, 0f, 0f), 1.25f, 0.85f, 2.2f, 14, true);
            b.Color(Swatch.CrystalDark);
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f * Mathf.Deg2Rad;
                var foot = new Vector3(Mathf.Cos(angle) * 1.4f, 0.1f, Mathf.Sin(angle) * 1.4f);
                var top = new Vector3(Mathf.Cos(angle) * 0.85f, 2.4f, Mathf.Sin(angle) * 0.85f);
                b.Rod(foot, top, 0.14f, 5);
            }
            b.Color(Swatch.Magenta);
            b.Sphere(new Vector3(0f, 2.8f, 0f), new Vector3(1.15f, 0.85f, 1.15f), 7, 14);
            b.Color(Swatch.GlowMagenta);
            b.Torus(new Vector3(0f, 2.8f, 0f), 1.16f, 0.05f, 18, 3);
            b.Color(Swatch.CrystalDark);
            b.Torus(new Vector3(0f, 3.62f, 0f), 0.36f, 0.12f, 12, 4);
            b.Color(Swatch.GlowGreen);
            b.Cylinder(new Vector3(0f, 3.6f, 0f), 0.28f, 0.28f, 0.04f, 12);
            return b;
        }

        // ------------------------------------------------------------------ Hive hulls

        /// <summary>The Brood Mound: a mound of flesh 10 m across under a chitin crust, ribs and egg sacs on its flanks, a well for the heart.</summary>
        public static MeshBuilder BroodMound()
        {
            var b = new MeshBuilder();
            b.Color(Swatch.Basalt);
            b.Blob(new Vector3(0f, 0.2f, 0f), new Vector3(5.4f, 0.6f, 4.6f), 1, 0.14f, 6201);
            b.Color(Swatch.Purple);
            b.Blob(new Vector3(0f, 0.6f, 0f), new Vector3(4.6f, 1.1f, 3.9f), 2, 0.1f, 6203);
            b.Color(Swatch.CrystalDark);
            for (int i = 0; i < 9; i++)
            {
                float angle = i * Mathf.PI * 2f / 9f;
                var foot = new Vector3(Mathf.Cos(angle) * 5f, 0.1f, Mathf.Sin(angle) * 4.3f);
                var top = new Vector3(Mathf.Cos(angle) * 2.2f, 1.75f, Mathf.Sin(angle) * 1.9f);
                b.Rod(foot, top, 0.22f, 6, true, 0.12f);
            }
            b.Color(Swatch.Magenta);
            for (int i = 0; i < 6; i++)
            {
                float angle = (i * 60f + 30f) * Mathf.Deg2Rad;
                b.Sphere(new Vector3(Mathf.Cos(angle) * 3.6f, 0.9f, Mathf.Sin(angle) * 3.1f), new Vector3(0.45f, 0.35f, 0.45f), 4, 8);
            }
            b.Color(Swatch.Basalt);
            b.Torus(new Vector3(0f, 1.7f, 0f), 1.5f, 0.3f, 18, 6);
            return b;
        }


        /// <summary>The Spore Towers' ground: a fleshy ridge 13 m long joining two tower roots at x = +-4.2, sac sockets at (+-1.4, -1).</summary>
        public static MeshBuilder TowerRidge()
        {
            var b = new MeshBuilder();
            b.Color(Swatch.Basalt);
            b.Blob(new Vector3(0f, 0.15f, 0f), new Vector3(7f, 0.4f, 3.2f), 1, 0.15f, 6301);
            b.Color(Swatch.Purple);
            b.Blob(new Vector3(0f, 0.4f, 0f), new Vector3(6.2f, 0.7f, 2.4f), 1, 0.12f, 6303);
            b.Color(Swatch.CrystalDark);
            b.Rod(new Vector3(-4.2f, 0.9f, 0f), new Vector3(4.2f, 0.9f, 0f), 0.35f, 8, true, 0.35f);
            for (int i = -2; i <= 2; i++)
            {
                b.Rod(new Vector3(i * 1.5f, 0.2f, 2.4f), new Vector3(i * 1.4f, 1.1f, 0.4f), 0.14f, 5);
                b.Rod(new Vector3(i * 1.5f, 0.2f, -2.4f), new Vector3(i * 1.4f, 1.1f, -0.4f), 0.14f, 5);
            }
            b.Color(Swatch.GlowGreen);
            b.Sphere(new Vector3(0f, 1.15f, 0f), new Vector3(0.6f, 0.3f, 0.6f), 4, 10);
            return b;
        }


        /// <summary>The Burrow Wyrm's burrow: a crater of torn ground the wyrm rises from (at the tail, z = -7.5).</summary>
        public static MeshBuilder Burrow()
        {
            var b = new MeshBuilder();
            b.Color(Swatch.RockDark);
            b.Torus(new Vector3(0f, 0.2f, -7.5f), 3f, 0.8f, 18, 6);
            b.Color(Swatch.Basalt);
            b.Cylinder(new Vector3(0f, 0f, -7.5f), 2.6f, 2.6f, 0.05f, 18);
            b.Color(Swatch.Rock);
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f * Mathf.Deg2Rad;
                b.Blob(new Vector3(Mathf.Cos(angle) * 3.9f, 0.3f, -7.5f + Mathf.Sin(angle) * 3.9f), new Vector3(0.7f, 0.5f, 0.6f), 0, 0.3f, 6400 + i);
            }
            return b;
        }

        /// <summary>
        /// The Brood Ship: an organic carrier 13 m long with its nose at +Z: a ridged carapace over a glowing belly, two pairs
        /// of membrane wings, maw sockets at the front (+-1.9, 3.4), sac sockets on the wings (+-4.6, 0.2) and spine sockets
        /// at the back (+-2.2, -3.6); deck 1.3.
        /// </summary>
        public static MeshBuilder BroodShipBody()
        {
            var b = new MeshBuilder();
            for (int pair = 0; pair < 2; pair++)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    b.Push(new Vector3(side * 1.6f, 0.7f, 1f - pair * 2.6f), Quaternion.Euler(0f, side * (pair == 0 ? -18f : 22f), side * 6f));
                    b.Color(pair == 0 ? Swatch.Violet : Swatch.Purple);
                    b.Sphere(new Vector3(side * 3.4f, 0f, 0f), new Vector3(3.6f, 0.1f, pair == 0 ? 1.7f : 1.3f), 4, 16);
                    b.Color(Swatch.CrystalDark);
                    b.Rod(Vector3.zero, new Vector3(side * 6.6f, 0.1f, 0.6f), 0.12f, 5, true, 0.03f);
                    b.Rod(Vector3.zero, new Vector3(side * 5.6f, 0.1f, -1f), 0.1f, 5, true, 0.03f);
                    b.Pop();
                }
            }
            b.Color(Swatch.GlowGreen);
            b.Sphere(new Vector3(0f, 0.55f, -0.4f), new Vector3(1.9f, 0.5f, 4.8f), 5, 14);
            b.Color(Swatch.Purple);
            b.Sphere(new Vector3(0f, 1f, 0f), new Vector3(2.4f, 0.9f, 6.2f), 7, 16);
            b.Color(Swatch.Basalt);
            for (int i = 0; i < 7; i++)
            {
                float z = -4.6f + i * 1.45f;
                float w = 2.3f * Mathf.Sqrt(Mathf.Max(0.05f, 1f - (z * z) / 42f));
                b.Push(new Vector3(0f, 1.2f, z), Quaternion.identity);
                b.Torus(Vector3.zero, w, 0.16f, 18, 4, 180f, false, 0f);
                b.Pop();
            }
            b.Color(Swatch.CrystalDark);
            b.Sphere(new Vector3(0f, 1.1f, 5.6f), new Vector3(1.2f, 0.7f, 1.2f), 5, 12);
            b.Color(Swatch.GlowMagenta);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Sphere(new Vector3(side * 0.6f, 1.45f, 6.2f), 0.18f, 3, 8);
            }
            return b;
        }


        /// <summary>
        /// The Hive Mother: a queen 12 m long lying in her nest, her head at +Z: a vast ringed abdomen behind, a thorax with
        /// folded legs, a crowned head over the heart's well (the core); her rings of sacs and maws turn above her.
        /// </summary>
        public static MeshBuilder HiveMotherBody()
        {
            var b = new MeshBuilder();
            b.Color(Swatch.RockDark);
            b.Blob(new Vector3(0f, 0.1f, 0f), new Vector3(7.6f, 0.5f, 7.2f), 1, 0.15f, 6501);
            b.Color(Swatch.Purple);
            b.Sphere(new Vector3(0f, 1.1f, -3.4f), new Vector3(3.6f, 1.4f, 4.2f), 8, 18);
            b.Color(Swatch.CrystalDark);
            for (int i = 0; i < 6; i++)
            {
                float z = -6.6f + i * 1.25f;
                float t = (z + 3.4f) / 4.2f;
                float w = 3.6f * Mathf.Sqrt(Mathf.Max(0.05f, 1f - t * t));
                b.Push(new Vector3(0f, 1.15f, z));
                b.Torus(Vector3.zero, w, 0.18f, 20, 4, 180f, false, 0f);
                b.Pop();
            }
            b.Color(Swatch.Magenta);
            b.Sphere(new Vector3(0f, 1.3f, 1.2f), new Vector3(2.2f, 1.1f, 1.8f), 6, 14);
            b.Color(Swatch.Basalt);
            for (int side = -1; side <= 1; side += 2)
            {
                for (int leg = 0; leg < 3; leg++)
                {
                    var hip = new Vector3(side * 1.9f, 1f, 1.8f - leg * 0.8f);
                    var knee = new Vector3(side * 4.2f, 1.6f, 2.6f - leg * 1.4f);
                    var foot = new Vector3(side * 5.2f, 0.1f, 3.4f - leg * 2.4f);
                    b.Rod(hip, knee, 0.24f, 6);
                    b.Rod(knee, foot, 0.18f, 6, true, 0.05f);
                }
            }
            b.Color(Swatch.Basalt);
            b.Sphere(new Vector3(0f, 1.5f, 3.4f), new Vector3(1.7f, 1f, 1.4f), 6, 14);
            for (int i = 0; i < 7; i++)
            {
                float angle = (i * 30f - 90f) * Mathf.Deg2Rad;
                var root = new Vector3(Mathf.Cos(angle) * 1.4f, 2.2f, 3.2f + Mathf.Sin(angle) * 0.4f - 0.4f);
                b.Rod(root, root + new Vector3(Mathf.Cos(angle) * 0.8f, 1.1f, -0.6f), 0.13f, 5, true, 0.01f);
            }
            b.Color(Swatch.GlowGreen);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Sphere(new Vector3(side * 0.85f, 1.9f, 4.4f), 0.24f, 3, 8);
            }
            return b;
        }


        // ------------------------------------------------------------------ crystal parts and hulls

        /// <summary>A crystal prism part: a tall six-sided crystal on a dark foot, a glowing core (muzzle at (0, 1.3, 0)).</summary>
        public static MeshBuilder CrystalPrism(float size)
        {
            var b = new MeshBuilder();
            b.Push(Vector3.zero, Quaternion.identity, Vector3.one * size);
            b.Color(Swatch.Basalt);
            b.Cylinder(Vector3.zero, 0.8f, 0.7f, 0.25f, 6);
            b.Color(Swatch.Pink);
            b.Cylinder(new Vector3(0f, 0.25f, 0f), 0.5f, 0.42f, 1.3f, 6, false, true, false);
            b.Cylinder(new Vector3(0f, 1.55f, 0f), 0.42f, 0f, 0.6f, 6, false, false, false);
            b.Color(Swatch.IceLight);
            for (int i = 0; i < 3; i++)
            {
                b.Push(new Vector3(0f, 0.25f, 0f), Quaternion.Euler(0f, i * 120f + 30f, 0f) * Quaternion.Euler(32f, 0f, 0f));
                b.Cylinder(new Vector3(0f, 0f, 0.4f), 0.18f, 0.15f, 0.7f, 6);
                b.Cylinder(new Vector3(0f, 0.7f, 0.4f), 0.15f, 0f, 0.25f, 6);
                b.Pop();
            }
            b.Color(Swatch.GlowMagenta);
            b.Sphere(new Vector3(0f, 1.3f, 0f), new Vector3(0.3f, 0.25f, 0.3f), 4, 6);
            b.Pop();
            return b;
        }


        /// <summary>
        /// The Shard Colossus: a giant of crystal lying in wait, 13 m across: a faceted body of dark glass and rose crystal,
        /// two arms reaching forward (laser sockets at their ends, (+-4.8, 2.6)), shoulder sockets (+-2.8, -0.6), deck 1.6.
        /// </summary>
        public static MeshBuilder ColossusBody()
        {
            var b = new MeshBuilder();
            b.Color(Swatch.Basalt);
            b.Blob(new Vector3(0f, 0.3f, -0.5f), new Vector3(4.2f, 0.9f, 3.4f), 1, 0.18f, 7101);
            b.Color(Swatch.CrystalDark);
            b.Blob(new Vector3(0f, 0.9f, -0.6f), new Vector3(3.2f, 0.9f, 2.6f), 1, 0.15f, 7103);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Color(Swatch.CrystalDark);
                b.Rod(new Vector3(side * 2.8f, 1.2f, -0.6f), new Vector3(side * 4.2f, 0.9f, 1.4f), 0.8f, 6, false, 0.6f);
                b.Rod(new Vector3(side * 4.2f, 0.9f, 1.4f), new Vector3(side * 4.8f, 0.7f, 2.6f), 0.6f, 6, false, 0.5f);
                b.Color(Swatch.Pink);
                for (int i = 0; i < 4; i++)
                {
                    b.Push(new Vector3(side * (1.6f + i * 0.5f), 1.4f, -1.6f - (i % 2) * 0.6f), Quaternion.Euler(-20f - i * 8f, side * 20f, side * -25f));
                    b.Cylinder(Vector3.zero, 0.22f, 0.18f, 1f + i * 0.2f, 6);
                    b.Cylinder(new Vector3(0f, 1f + i * 0.2f, 0f), 0.18f, 0f, 0.4f, 6);
                    b.Pop();
                }
            }
            b.Color(Swatch.IceLight);
            b.Push(new Vector3(0f, 1.6f, -2.2f), Quaternion.Euler(-35f, 0f, 0f));
            b.Cylinder(Vector3.zero, 0.5f, 0.42f, 1.8f, 6);
            b.Cylinder(new Vector3(0f, 1.8f, 0f), 0.42f, 0f, 0.6f, 6);
            b.Pop();
            Socket(b, new Vector3(2.8f, 1.6f, -0.6f), 0.9f);
            Socket(b, new Vector3(-2.8f, 1.6f, -0.6f), 0.9f);
            return b;
        }


        /// <summary>The Colossus's heart, its core: a cluster of glowing crystal round a white-hot centre.</summary>
        public static MeshBuilder CrystalHeart(float radius)
        {
            var b = new MeshBuilder();
            b.Color(Swatch.GlowWhite);
            b.Sphere(new Vector3(0f, radius * 0.5f, 0f), radius * 0.4f, 5, 10);
            for (int i = 0; i < 7; i++)
            {
                float yaw = i * 360f / 7f;
                b.Push(new Vector3(0f, radius * 0.3f, 0f), Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(50f + (i % 3) * 10f, 0f, 0f));
                b.Color(i % 2 == 0 ? Swatch.Magenta : Swatch.Pink);
                b.Cylinder(Vector3.zero, radius * 0.18f, radius * 0.15f, radius * 0.9f, 6);
                b.Cylinder(new Vector3(0f, radius * 0.9f, 0f), radius * 0.15f, 0f, radius * 0.3f, 6);
                b.Pop();
            }
            b.Color(Swatch.GlowMagenta);
            b.Torus(new Vector3(0f, radius * 0.3f, 0f), radius * 0.85f, 0.05f, 20, 3);
            return b;
        }


        /// <summary>
        /// The Resonator's base: a crystal-grown platform 12 m across (deck 1.2) under the rings; a ring of crystal
        /// segments (<see cref="CrystalRing"/>) turns above it, the core in the middle.
        /// </summary>
        public static MeshBuilder ResonatorBase()
        {
            var b = new MeshBuilder();
            var outline = new List<Vector2>();
            for (int i = 0; i < 12; i++)
            {
                float angle = (i * 30f + 15f) * Mathf.Deg2Rad;
                float r = i % 2 == 0 ? 6.2f : 5.6f;
                outline.Add(new Vector2(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r * 0.88f));
            }
            b.Color(Swatch.Basalt);
            Slab(b, outline, 0f, 1.2f);
            b.Color(Swatch.CrystalDark);
            Slab(b, new List<Vector2>(outline.ConvertAll(p => p * 0.85f)), 1.2f, 0.1f);
            b.Color(Swatch.GlowMagenta);
            b.Torus(new Vector3(0f, 1.32f, 0f), 2.6f, 0.06f, 28, 3);
            b.Color(Swatch.Pink);
            for (int i = 0; i < 12; i++)
            {
                float angle = i * 30f;
                b.Push(new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad) * 5.8f, 0.6f, Mathf.Sin(angle * Mathf.Deg2Rad) * 5.1f), Quaternion.Euler(0f, -angle, 0f) * Quaternion.Euler(0f, 0f, -35f));
                b.Cylinder(Vector3.zero, 0.3f, 0.26f, 1.2f, 6);
                b.Cylinder(new Vector3(0f, 1.2f, 0f), 0.26f, 0f, 0.4f, 6);
                b.Pop();
            }
            return b;
        }


        /// <summary>A ring of crystal segments lying in the XZ plane, <paramref name="radius"/> across the middle, with <paramref name="mounts"/> mounts.</summary>
        public static MeshBuilder CrystalRing(float radius, int mounts)
        {
            var b = new MeshBuilder();
            int segments = mounts * 3;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * 360f / segments;
                b.Push(Vector3.zero, Quaternion.Euler(0f, angle, 0f));
                b.Color(i % 3 == 0 ? Swatch.CrystalDark : Swatch.Violet);
                b.BeveledBox(new Vector3(0f, 0f, radius), new Vector3(radius * 2f * Mathf.PI / segments * 0.9f, 0.45f, 0.6f), 0.1f);
                b.Pop();
            }
            b.Color(Swatch.GlowMagenta);
            b.Torus(new Vector3(0f, 0.24f, 0f), radius, 0.06f, 48, 3);
            return b;
        }
    }
}
