using System.Collections.Generic;
using Gamebox.Editor;
using UnityEngine;

namespace Portfolio.DeathRace.EditorTools
{
    /// <summary>
    /// The six car models. Each is drawn to read from about sixty degrees above: the roof and the bonnet carry the
    /// driver's colour, and the silhouette tells the cars apart (the Rustbucket's roof rack, the Hornet's stripes, the
    /// Bulldog's bed and roll bar, the Viper's scoop and wing, the Reaper's black canopy, the Tyrant's turret and ram).
    /// Sizes follow <see cref="CarSpec.Size"/> (width, height, length); the car faces +z.
    /// </summary>
    internal static partial class CarMeshes
    {
        /// <summary>A painted pillar across both side windows of a cabin at <paramref name="z"/>.</summary>
        private static void Pillar(CarRig rig, IList<Ring> cabin, int segment, float z, float width, string color = "Paint")
        {
            Ring a = cabin[segment];
            Ring b = cabin[segment + 1];
            float v = (z - a.Z) / (b.Z - a.Z);
            float dv = width * 0.5f / (b.Z - a.Z);
            Panel(rig.Paint, color, cabin, segment, Ring.CabinRight, 0f, 1f, v - dv, v + dv, 0.006f);
            Panel(rig.Paint, color, cabin, segment, Ring.CabinLeft, 0f, 1f, v - dv, v + dv, 0.006f);
        }

        /// <summary>The faces of a usual cabin: rear window, painted roof, windscreen, glass sides, painted pillars.</summary>
        private static Face CabinFace(CarRig rig, int segment, int band, bool paintedRear = true)
        {
            switch (band)
            {
                case Ring.CabinBottom: return Face.None;
                case Ring.CabinTop: return segment == 1 ? rig.PaintFace() : rig.BodyFace("Glass");
                case Ring.CabinChamferLeft:
                case Ring.CabinChamferRight: return rig.PaintFace();
                default: return segment == 0 && paintedRear ? rig.PaintFace("PaintShade") : rig.BodyFace("Glass");
            }
        }

        // ------------------------------------------------------------------ Rustbucket

        /// <summary>
        /// A boxy hatchback held together by rust: a primer bonnet, a faded door from another car, rust patches, one
        /// round and one square headlight, a sagging bumper, a single mirror and a roof rack with a spare tyre and a
        /// tarpaulin bundle. A crude single gun is bolted to the bonnet.
        /// </summary>
        private static void Rustbucket(CarRig rig)
        {
            Wheels(rig, 0.35f, 0.25f, 0.84f, 1.3f, 0.35f, 0.25f, 0.84f, -1.28f, "LightGray", false, 4);
            var body = new[]
            {
                Ring.Body(-2.08f, 0.32f, 0.56f, 0.84f, 0.94f, 0.07f),
                Ring.Body(-1.98f, 0.26f, 0.58f, 0.9f, 0.98f, 0.08f),
                Ring.Body(-0.55f, 0.26f, 0.58f, 0.9f, 0.99f, 0.08f),
                Ring.Body(0.95f, 0.26f, 0.58f, 0.89f, 0.99f, 0.08f),
                Ring.Body(1.9f, 0.27f, 0.56f, 0.84f, 0.97f, 0.08f),
                Ring.Body(2.08f, 0.32f, 0.54f, 0.76f, 0.92f, 0.07f)
            };
            Loft(body, (s, band) =>
            {
                if (s >= 3 && (band == Ring.Top || band == Ring.ChamferLeft || band == Ring.ChamferRight))
                {
                    return rig.BodyFace("Primer");
                }
                if (s == 2 && (band == Ring.UpperLeft || band == Ring.LowerLeft || band == Ring.ChamferLeft))
                {
                    return rig.BodyFace("Faded");
                }
                return rig.StandardBody(s, band);
            }, rig.PaintFace("PaintShade"), rig.PaintFace("PaintShade"));

            var cabin = new[]
            {
                Ring.Cabin(-1.96f, 0.88f, 0.92f, 0.84f, 0.82f, 0.02f),
                Ring.Cabin(-1.8f, 0.88f, 1.36f, 0.84f, 0.76f, 0.1f),
                Ring.Cabin(0.3f, 0.87f, 1.38f, 0.84f, 0.76f, 0.1f),
                Ring.Cabin(1.0f, 0.87f, 0.91f, 0.84f, 0.82f, 0.02f)
            };
            Loft(cabin, (s, band) => CabinFace(rig, s, band), rig.PaintFace(), rig.PaintFace());
            Pillar(rig, cabin, 1, -0.6f, 0.1f);
            Shine(rig, cabin, 2, Ring.CabinTop);
            Shine(rig, cabin, 0, Ring.CabinTop, 0.25f, 0.12f);

            // Rust: a patch on the roof, one on the bonnet, one on the rear quarter.
            rig.B("Rust").Box(new Vector3(-0.42f, 1.368f, -1.45f), new Vector3(0.34f, 0.012f, 0.4f));
            rig.B("RustDark").Box(new Vector3(-0.48f, 1.372f, -1.5f), new Vector3(0.16f, 0.012f, 0.18f));
            Stripe(rig.Body, "Rust", body, 0.38f, 0.66f, 1.6f, 1.88f, 0.006f);
            Panel(rig.Body, "Rust", body, 1, Ring.LowerLeft, 0.15f, 0.85f, 0.04f, 0.2f, 0.004f);
            Panel(rig.Body, "RustDark", body, 1, Ring.UpperRight, 0.1f, 0.6f, 0.05f, 0.14f, 0.004f);

            // Roof rack with a tarpaulin bundle and a spare tyre.
            const float roof = 1.38f;
            for (int side = -1; side <= 1; side += 2)
            {
                rig.B("Gray").Rod(new Vector3(side * 0.6f, roof + 0.08f, -1.62f), new Vector3(side * 0.6f, roof + 0.08f, 0.2f), 0.026f, 6, false);
                rig.B("Gray").Rod(new Vector3(side * 0.6f, roof - 0.02f, -1.55f), new Vector3(side * 0.6f, roof + 0.08f, -1.55f), 0.022f, 5, false);
                rig.B("Gray").Rod(new Vector3(side * 0.6f, roof - 0.02f, 0.12f), new Vector3(side * 0.6f, roof + 0.08f, 0.12f), 0.022f, 5, false);
            }
            foreach (float z in new[] { -1.45f, -0.75f, -0.05f })
            {
                rig.B("Gray").Rod(new Vector3(-0.64f, roof + 0.1f, z), new Vector3(0.64f, roof + 0.1f, z), 0.024f, 6, false);
            }
            rig.B("Canvas").BeveledBox(new Vector3(0.06f, roof + 0.25f, -1.08f), new Vector3(0.96f, 0.26f, 0.74f), 0.07f);
            rig.B("CanvasDark").BeveledBox(new Vector3(-0.15f, roof + 0.4f, -1.18f), new Vector3(0.4f, 0.1f, 0.36f), 0.04f);
            rig.B("Strap").Box(new Vector3(0.06f, roof + 0.255f, -1.28f), new Vector3(0.98f, 0.27f, 0.05f));
            rig.B("Strap").Box(new Vector3(0.06f, roof + 0.255f, -0.86f), new Vector3(0.98f, 0.27f, 0.05f));
            rig.B("Rubber").Cylinder(new Vector3(-0.08f, roof + 0.12f, -0.12f), 0.3f, 0.3f, 0.16f, 14, true);
            rig.B("Gray").Cylinder(new Vector3(-0.08f, roof + 0.28f, -0.12f), 0.17f, 0.17f, 0.012f, 12);
            rig.B("Hub").Cylinder(new Vector3(-0.08f, roof + 0.29f, -0.12f), 0.06f, 0.05f, 0.02f, 8);

            // The bonnet gun: one barrel, bolted on off-centre.
            rig.Muzzle = MachineGun(rig, new Vector3(0.36f, 0.874f, 1.12f), 0.95f, 1);
            rig.B("Gray").Box(new Vector3(0.36f, 0.88f, 1.0f), new Vector3(0.46f, 0.02f, 0.06f));

            // Front: a round and a square headlight, a tired grille, a sagging bumper.
            Headlight(rig, new Vector3(-0.6f, 0.64f, 2.09f), new Vector2(0.28f, 0.17f), Vector3.forward, "Headlight", "Gray");
            Headlight(rig, new Vector3(0.6f, 0.64f, 2.09f), new Vector2(0.1f, 0f), Vector3.forward, "Headlight", "Gray");
            rig.Beams.Add(new Vector3(-0.6f, 0.64f, 2.12f));
            rig.Beams.Add(new Vector3(0.6f, 0.64f, 2.12f));
            Grille(rig, new Vector3(0f, 0.62f, 2.085f), new Vector2(0.62f, 0.16f), 3, "Gray");
            rig.Body.Push(new Vector3(0f, 0.38f, 2.15f), Quaternion.Euler(0f, 0f, 4f));
            rig.B("Gray").BeveledBox(Vector3.zero, new Vector3(1.9f, 0.15f, 0.16f), 0.04f);
            rig.Body.Pop();

            // Back: rusty bumper, small tail lights, plate, one rusty exhaust.
            rig.B("RustDark").BeveledBox(new Vector3(0f, 0.4f, -2.12f), new Vector3(1.86f, 0.15f, 0.14f), 0.04f);
            Taillight(rig, new Vector3(-0.72f, 0.72f, -2.09f), new Vector2(0.16f, 0.2f));
            Taillight(rig, new Vector3(0.72f, 0.72f, -2.09f), new Vector2(0.16f, 0.2f));
            Plate(rig, new Vector3(0f, 0.6f, -2.1f));
            Exhaust(rig, new Vector3(0.55f, 0.27f, -2.22f), Vector3.back, 0.045f, 0.42f, "Rust", "RustDark");

            Mirror(rig, new Vector3(-0.84f, 0.95f, 0.85f), -1);
            rig.B("Charcoal").Rod(new Vector3(0.84f, 0.9f, 0.9f), new Vector3(0.9f, 0.95f, 0.9f), 0.015f, 4, false);
            rig.B("Black").Rod(new Vector3(0.82f, 0.9f, -1.9f), new Vector3(0.95f, 2.1f, -2.3f), 0.008f, 4, false);
            Arches(rig, 0.97f);
            rig.MineDrop = new Vector3(0f, 0.3f, -2.45f);
            rig.RocketMount = new Vector3(0.4f, 1.62f, 0.3f);
        }

        // ------------------------------------------------------------------ Hornet

        /// <summary>
        /// A small sporty coupe: low wedge nose, fastback, twin white racing stripes over the bonnet and roof, a black
        /// lip spoiler, side skirts, bonnet vents and a slim twin gun between the stripes.
        /// </summary>
        private static void Hornet(CarRig rig)
        {
            Wheels(rig, 0.33f, 0.26f, 0.8f, 1.35f, 0.33f, 0.26f, 0.8f, -1.3f, "Chrome", false, 6);
            var body = new[]
            {
                Ring.Body(-2.13f, 0.3f, 0.5f, 0.74f, 0.86f, 0.06f),
                Ring.Body(-1.98f, 0.24f, 0.52f, 0.82f, 0.93f, 0.07f),
                Ring.Body(-1.25f, 0.22f, 0.52f, 0.82f, 0.95f, 0.08f),
                Ring.Body(-0.2f, 0.22f, 0.5f, 0.78f, 0.92f, 0.08f),
                Ring.Body(0.9f, 0.22f, 0.48f, 0.73f, 0.93f, 0.08f),
                Ring.Body(1.75f, 0.22f, 0.44f, 0.63f, 0.91f, 0.08f),
                Ring.Body(2.13f, 0.26f, 0.36f, 0.47f, 0.8f, 0.07f)
            };
            Loft(body, rig.StandardBody, rig.PaintFace("PaintShade"), rig.PaintFace("PaintShade"));
            var cabin = new[]
            {
                Ring.Cabin(-1.92f, 0.8f, 0.84f, 0.76f, 0.74f, 0.02f),
                Ring.Cabin(-0.6f, 0.78f, 1.22f, 0.77f, 0.64f, 0.1f),
                Ring.Cabin(0.1f, 0.75f, 1.24f, 0.77f, 0.65f, 0.1f),
                Ring.Cabin(0.92f, 0.71f, 0.75f, 0.78f, 0.76f, 0.02f)
            };
            Loft(cabin, (s, band) => CabinFace(rig, s, band), rig.PaintFace(), rig.PaintFace());
            Shine(rig, cabin, 2, Ring.CabinTop);
            Shine(rig, cabin, 0, Ring.CabinTop, 0.3f, 0.1f);

            // Racing stripes: bonnet and nose, roof, the tail.
            for (int side = -1; side <= 1; side += 2)
            {
                float x0 = side < 0 ? -0.27f : 0.1f;
                float x1 = side < 0 ? -0.1f : 0.27f;
                Stripe(rig.Body, "White", body, x0, x1, 0.9f, 2.2f);
                Stripe(rig.Body, "White", cabin, x0, x1, -0.6f, 0.1f);
                Stripe(rig.Body, "White", body, x0, x1, -2.2f, -1.92f);
            }
            Stripe(rig.Body, "Grille", body, 0.38f, 0.54f, 1.15f, 1.5f);
            Stripe(rig.Body, "Grille", body, -0.54f, -0.38f, 1.15f, 1.5f);
            rig.B("Black").BeveledBox(new Vector3(0f, 0.8f, -2.04f), new Vector3(1.46f, 0.05f, 0.16f), 0.015f);
            for (int side = -1; side <= 1; side += 2)
            {
                rig.B("Black").BeveledBox(new Vector3(side * 0.9f, 0.25f, 0.02f), new Vector3(0.07f, 0.08f, 1.75f), 0.02f);
                Mirror(rig, new Vector3(side * 0.77f, 0.8f, 0.75f), side, 0.9f);
            }

            rig.Muzzle = MachineGun(rig, new Vector3(0f, 0.718f, 0.94f), 0.8f, 2);

            Headlight(rig, new Vector3(-0.58f, 0.42f, 2.12f), new Vector2(0.3f, 0.09f), new Vector3(0f, 0.35f, 1f));
            Headlight(rig, new Vector3(0.58f, 0.42f, 2.12f), new Vector2(0.3f, 0.09f), new Vector3(0f, 0.35f, 1f));
            rig.Beams.Add(new Vector3(-0.58f, 0.42f, 2.15f));
            rig.Beams.Add(new Vector3(0.58f, 0.42f, 2.15f));
            Grille(rig, new Vector3(0f, 0.33f, 2.135f), new Vector2(0.7f, 0.09f), 2, "Charcoal");
            Taillight(rig, new Vector3(-0.52f, 0.64f, -2.135f), new Vector2(0.36f, 0.09f));
            Taillight(rig, new Vector3(0.52f, 0.64f, -2.135f), new Vector2(0.36f, 0.09f));
            Plate(rig, new Vector3(0f, 0.46f, -2.14f));
            Exhaust(rig, new Vector3(-0.14f, 0.27f, -2.2f), Vector3.back, 0.042f, 0.3f, "Exhaust", "Chrome");
            Exhaust(rig, new Vector3(0.14f, 0.27f, -2.2f), Vector3.back, 0.042f, 0.3f, "Exhaust", "Chrome");
            Arches(rig, 0.93f);
            rig.MineDrop = new Vector3(0f, 0.3f, -2.45f);
            rig.RocketMount = new Vector3(0f, 1.32f, -0.25f);
        }

        // ------------------------------------------------------------------ Bulldog

        /// <summary>
        /// A pickup with a tubular bull bar, a light bar on the cab, an open bed (spare tyre, jerry cans, ammo crate)
        /// and a roll bar carrying a heavy gun and a two-tube rocket pod. Big knobbly tyres.
        /// </summary>
        private static void Bulldog(CarRig rig)
        {
            Wheels(rig, 0.45f, 0.34f, 0.92f, 1.5f, 0.45f, 0.34f, 0.92f, -1.45f, "LightGray", true, 6);
            var nose = new[]
            {
                Ring.Body(-0.12f, 0.5f, 0.84f, 1.1f, 1.08f, 0.06f, 0.06f, 0.05f),
                Ring.Body(1.95f, 0.5f, 0.84f, 1.1f, 1.08f, 0.06f, 0.06f, 0.05f),
                Ring.Body(2.32f, 0.52f, 0.82f, 1.03f, 1.05f, 0.06f, 0.06f, 0.05f)
            };
            Loft(nose, rig.StandardBody, rig.PaintFace("PaintShade"), rig.PaintFace("PaintShade"));

            // The bed.
            rig.P("PaintShade").BeveledBox(new Vector3(0f, 0.64f, -1.25f), new Vector3(2.12f, 0.28f, 2.26f), 0.04f);
            rig.B("Charcoal").Box(new Vector3(0f, 0.79f, -1.25f), new Vector3(1.92f, 0.03f, 2.2f));
            for (int i = 0; i < 6; i++)
            {
                rig.B("DarkGray").Box(new Vector3(-0.8f + i * 0.32f, 0.81f, -1.25f), new Vector3(0.05f, 0.02f, 2.15f));
            }
            for (int side = -1; side <= 1; side += 2)
            {
                rig.P("Paint").BeveledBox(new Vector3(side * 1.02f, 0.98f, -1.25f), new Vector3(0.1f, 0.4f, 2.26f), 0.02f);
                rig.B("Charcoal").Box(new Vector3(side * 1.02f, 1.19f, -1.25f), new Vector3(0.13f, 0.03f, 2.28f));
                rig.P("PaintDark").BeveledBox(new Vector3(side * 0.8f, 0.9f, -1.45f), new Vector3(0.3f, 0.22f, 0.95f), 0.05f);
            }
            rig.P("PaintShade").BeveledBox(new Vector3(0f, 0.98f, -2.33f), new Vector3(2.12f, 0.4f, 0.1f), 0.02f);
            rig.B("Charcoal").Box(new Vector3(0f, 1.19f, -2.33f), new Vector3(2.14f, 0.03f, 0.12f));
            rig.B("Rubber").Cylinder(new Vector3(0.32f, 0.8f, -0.95f), 0.36f, 0.36f, 0.2f, 14, true);
            rig.B("Gray").Cylinder(new Vector3(0.32f, 1.0f, -0.95f), 0.2f, 0.2f, 0.012f, 12);
            rig.B("Hub").Cylinder(new Vector3(0.32f, 1.01f, -0.95f), 0.07f, 0.06f, 0.02f, 8);
            rig.B("Red").BeveledBox(new Vector3(-0.62f, 1.0f, -1.95f), new Vector3(0.2f, 0.4f, 0.3f), 0.03f);
            rig.B("Olive").BeveledBox(new Vector3(-0.38f, 1.0f, -1.95f), new Vector3(0.2f, 0.4f, 0.3f), 0.03f);
            rig.B("OliveDark").BeveledBox(new Vector3(-0.45f, 0.93f, -0.78f), new Vector3(0.5f, 0.26f, 0.4f), 0.03f);
            rig.B("Hazard").Box(new Vector3(-0.45f, 1.065f, -0.78f), new Vector3(0.3f, 0.01f, 0.08f));

            // The cab.
            var cab = new[]
            {
                Ring.Cabin(-0.1f, 1.08f, 1.12f, 0.97f, 0.96f, 0.02f),
                Ring.Cabin(-0.06f, 1.08f, 1.66f, 0.97f, 0.9f, 0.08f),
                Ring.Cabin(0.55f, 1.08f, 1.68f, 0.97f, 0.9f, 0.08f),
                Ring.Cabin(1.02f, 1.08f, 1.12f, 0.97f, 0.96f, 0.02f)
            };
            Loft(cab, (s, band) => CabinFace(rig, s, band, false), rig.PaintFace(), rig.PaintFace());
            Pillar(rig, cab, 1, 0.22f, 0.1f);
            Shine(rig, cab, 2, Ring.CabinTop);
            rig.B("Charcoal").BeveledBox(new Vector3(0f, 1.71f, 0.42f), new Vector3(1.3f, 0.05f, 0.14f), 0.015f);
            for (int i = 0; i < 4; i++)
            {
                Headlight(rig, new Vector3(-0.45f + i * 0.3f, 1.77f, 0.47f), new Vector2(0.17f, 0.09f), Vector3.forward, "Spot", "Charcoal");
            }

            // Bull bar, bumper, grille, headlights.
            const float zb = 2.55f;
            for (int side = -1; side <= 1; side += 2)
            {
                Bar(rig.B("Gunmetal"), new Vector3(side * 0.62f, 0.42f, zb), new Vector3(side * 0.62f, 1.14f, zb - 0.06f), 0.09f);
                Bar(rig.B("Gunmetal"), new Vector3(side * 0.62f, 0.72f, zb - 0.03f), new Vector3(side * 0.62f, 0.72f, 2.3f), 0.07f);
                Bar(rig.B("Gunmetal"), new Vector3(side * 0.22f, 0.45f, zb), new Vector3(side * 0.22f, 1.14f, zb - 0.06f), 0.06f);
            }
            Bar(rig.B("Gunmetal"), new Vector3(-0.66f, 1.14f, zb - 0.06f), new Vector3(0.66f, 1.14f, zb - 0.06f), 0.09f);
            Bar(rig.B("Gunmetal"), new Vector3(-0.64f, 0.84f, zb - 0.03f), new Vector3(0.64f, 0.84f, zb - 0.03f), 0.07f);
            rig.B("Gunmetal").BeveledBox(new Vector3(0f, 0.5f, 2.42f), new Vector3(2.1f, 0.22f, 0.22f), 0.04f);
            Grille(rig, new Vector3(0f, 0.86f, 2.325f), new Vector2(1.1f, 0.3f), 4, "Chrome");
            Headlight(rig, new Vector3(-0.8f, 0.88f, 2.33f), new Vector2(0.24f, 0.18f), Vector3.forward);
            Headlight(rig, new Vector3(0.8f, 0.88f, 2.33f), new Vector2(0.24f, 0.18f), Vector3.forward);
            rig.Beams.Add(new Vector3(-0.8f, 0.88f, 2.36f));
            rig.Beams.Add(new Vector3(0.8f, 0.88f, 2.36f));

            // Roll bar with the heavy gun, a spotlight and the rocket pod.
            for (int side = -1; side <= 1; side += 2)
            {
                Bar(rig.B("Gunmetal"), new Vector3(side * 0.97f, 1.17f, -0.35f), new Vector3(side * 0.88f, 2.0f, -0.35f), 0.1f);
                Bar(rig.B("Gunmetal"), new Vector3(side * 0.88f, 2.0f, -0.35f), new Vector3(side * 0.98f, 1.2f, -1.95f), 0.08f);
            }
            Bar(rig.B("Gunmetal"), new Vector3(-0.92f, 2.0f, -0.35f), new Vector3(0.92f, 2.0f, -0.35f), 0.1f);
            Bar(rig.B("Gunmetal"), new Vector3(-0.88f, 1.98f, -0.36f), new Vector3(0.95f, 1.2f, -0.36f), 0.06f);
            rig.Muzzle = MachineGun(rig, new Vector3(0f, 2.04f, -0.42f), 1.05f, 1);
            Headlight(rig, new Vector3(-0.62f, 2.11f, -0.3f), new Vector2(0.08f, 0f), Vector3.forward, "Spot", "Charcoal");
            RocketPod(rig, new Vector3(0.6f, 2.14f, -0.1f), 1, 2, 0.06f, 0.55f);
            rig.RocketMount = new Vector3(0.6f, 2.14f, 0.05f);

            // Back.
            Taillight(rig, new Vector3(-1.0f, 0.98f, -2.39f), new Vector2(0.1f, 0.28f));
            Taillight(rig, new Vector3(1.0f, 0.98f, -2.39f), new Vector2(0.1f, 0.28f));
            Plate(rig, new Vector3(0f, 0.66f, -2.39f));
            rig.B("Gunmetal").BeveledBox(new Vector3(0f, 0.5f, -2.4f), new Vector3(2.0f, 0.12f, 0.14f), 0.03f);
            Exhaust(rig, new Vector3(0.62f, 0.42f, -2.47f), Vector3.back, 0.05f, 0.4f);
            for (int side = -1; side <= 1; side += 2)
            {
                rig.B("Rubber").Box(new Vector3(side * 0.92f, 0.36f, -2.0f), new Vector3(0.32f, 0.3f, 0.025f));
                rig.B("Rubber").Box(new Vector3(side * 0.92f, 0.36f, 0.95f), new Vector3(0.32f, 0.3f, 0.025f));
                Mirror(rig, new Vector3(side * 0.97f, 1.25f, 0.9f), side, 1.25f);
            }
            Arches(rig, 1.09f, 0.08f, "Black", 0.07f);
            rig.MineDrop = new Vector3(0f, 0.35f, -2.7f);
        }

        // ------------------------------------------------------------------ Viper

        /// <summary>
        /// A muscle car: long bonnet with a black gun scoop (twin barrels out of its mouth), cabin set far back, a painted
        /// rear wing on struts, chrome bumpers, quad round headlights, fat rear tyres and chrome side pipes.
        /// </summary>
        private static void Viper(CarRig rig)
        {
            Wheels(rig, 0.34f, 0.26f, 0.84f, 1.5f, 0.37f, 0.34f, 0.8f, -1.42f, "Chrome", false, 5);
            var body = new[]
            {
                Ring.Body(-2.28f, 0.3f, 0.55f, 0.78f, 0.9f, 0.06f),
                Ring.Body(-2.12f, 0.25f, 0.57f, 0.86f, 0.98f, 0.07f),
                Ring.Body(-1.35f, 0.23f, 0.58f, 0.86f, 1.0f, 0.08f),
                Ring.Body(-0.3f, 0.23f, 0.56f, 0.82f, 0.95f, 0.08f),
                Ring.Body(0.4f, 0.23f, 0.56f, 0.81f, 0.96f, 0.08f),
                Ring.Body(1.45f, 0.23f, 0.56f, 0.8f, 0.98f, 0.08f),
                Ring.Body(2.12f, 0.25f, 0.52f, 0.74f, 0.95f, 0.07f),
                Ring.Body(2.28f, 0.3f, 0.48f, 0.64f, 0.86f, 0.06f)
            };
            Loft(body, rig.StandardBody, rig.PaintFace("PaintShade"), rig.PaintFace("PaintShade"));
            var cabin = new[]
            {
                Ring.Cabin(-1.8f, 0.84f, 0.88f, 0.8f, 0.78f, 0.02f),
                Ring.Cabin(-1.1f, 0.83f, 1.23f, 0.8f, 0.64f, 0.09f),
                Ring.Cabin(-0.35f, 0.81f, 1.24f, 0.8f, 0.66f, 0.09f),
                Ring.Cabin(0.42f, 0.79f, 0.83f, 0.81f, 0.79f, 0.02f)
            };
            Loft(cabin, (s, band) => CabinFace(rig, s, band), rig.PaintFace(), rig.PaintFace());
            Shine(rig, cabin, 2, Ring.CabinTop);
            Shine(rig, cabin, 0, Ring.CabinTop, 0.28f, 0.1f);

            // The gun scoop.
            rig.B("Charcoal").BeveledBox(new Vector3(0f, 0.88f, 1.05f), new Vector3(0.56f, 0.16f, 1.0f), 0.04f);
            rig.B("Gunmetal").BeveledBox(new Vector3(0f, 0.985f, 0.95f), new Vector3(0.3f, 0.07f, 0.5f), 0.02f);
            rig.B("Grille").Box(new Vector3(0f, 0.89f, 1.555f), new Vector3(0.46f, 0.09f, 0.02f));
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 0.11f;
                rig.B("GunDark").Rod(new Vector3(x, 0.89f, 1.4f), new Vector3(x, 0.89f, 1.9f), 0.04f, 8, false);
                rig.B("Gunmetal").Rod(new Vector3(x, 0.89f, 1.9f), new Vector3(x, 0.89f, 2.15f), 0.022f, 6, false);
                rig.B("GunDark").Rod(new Vector3(x, 0.89f, 2.12f), new Vector3(x, 0.89f, 2.22f), 0.036f, 8, false);
            }
            rig.Muzzle = new Vector3(0f, 0.89f, 2.24f);

            // Rear wing.
            for (int side = -1; side <= 1; side += 2)
            {
                rig.B("Black").Box(new Vector3(side * 0.55f, 0.99f, -2.05f), new Vector3(0.06f, 0.24f, 0.14f));
                rig.B("Black").Box(new Vector3(side * 0.94f, 1.1f, -2.08f), new Vector3(0.03f, 0.16f, 0.42f));
            }
            rig.P("Paint").BeveledBox(new Vector3(0f, 1.12f, -2.08f), new Vector3(1.86f, 0.05f, 0.38f), 0.015f);

            rig.B("Chrome").BeveledBox(new Vector3(0f, 0.36f, 2.29f), new Vector3(1.78f, 0.12f, 0.1f), 0.03f);
            rig.B("Chrome").BeveledBox(new Vector3(0f, 0.38f, -2.29f), new Vector3(1.78f, 0.12f, 0.1f), 0.03f);
            for (int side = -1; side <= 1; side += 2)
            {
                Headlight(rig, new Vector3(side * 0.48f, 0.56f, 2.285f), new Vector2(0.075f, 0f), Vector3.forward);
                Headlight(rig, new Vector3(side * 0.7f, 0.56f, 2.27f), new Vector2(0.075f, 0f), Vector3.forward);
                rig.Beams.Add(new Vector3(side * 0.6f, 0.56f, 2.3f));
                Taillight(rig, new Vector3(side * 0.52f, 0.66f, -2.285f), new Vector2(0.46f, 0.1f));
                rig.B("Chrome").Rod(new Vector3(side * 1.0f, 0.3f, 0.75f), new Vector3(side * 1.0f, 0.3f, -0.8f), 0.05f, 8, false);
                Exhaust(rig, new Vector3(side * 1.0f, 0.3f, -0.95f), Vector3.back, 0.055f, 0.18f, "Chrome", "ExhaustTip");
                Mirror(rig, new Vector3(side * 0.8f, 0.88f, 0.32f), side, 0.9f);
            }
            Grille(rig, new Vector3(0f, 0.48f, 2.285f), new Vector2(0.56f, 0.14f), 3, "Chrome");
            Plate(rig, new Vector3(0f, 0.52f, -2.29f));
            Arches(rig, 0.99f);
            rig.MineDrop = new Vector3(0f, 0.3f, -2.55f);
            rig.RocketMount = new Vector3(0f, 1.32f, -0.75f);
        }

        // ------------------------------------------------------------------ Reaper

        /// <summary>
        /// A low wedge supercar, all angles: carbon-black lower body, a black canopy and louvred engine cover, side
        /// intakes, a low carbon wing, a splitter and diffuser, xenon slit headlights, a full-width tail light, and two
        /// guns in pods on the front wings.
        /// </summary>
        private static void Reaper(CarRig rig)
        {
            Wheels(rig, 0.33f, 0.28f, 0.85f, 1.42f, 0.35f, 0.34f, 0.83f, -1.38f, "ChromeDark", false, 5);
            var body = new[]
            {
                Ring.Body(-2.24f, 0.28f, 0.5f, 0.76f, 0.96f, 0.12f, 0.06f, 0.08f),
                Ring.Body(-1.4f, 0.2f, 0.5f, 0.82f, 1.02f, 0.14f, 0.06f, 0.08f),
                Ring.Body(-0.3f, 0.2f, 0.46f, 0.7f, 0.98f, 0.12f, 0.06f, 0.08f),
                Ring.Body(0.95f, 0.19f, 0.42f, 0.6f, 0.99f, 0.1f, 0.06f, 0.08f),
                Ring.Body(1.75f, 0.19f, 0.36f, 0.48f, 0.97f, 0.1f, 0.06f, 0.08f),
                Ring.Body(2.25f, 0.2f, 0.26f, 0.32f, 0.82f, 0.06f, 0.04f, 0.06f)
            };
            Loft(body, (s, band) =>
            {
                if (band == Ring.Bottom || band == Ring.LowerLeft || band == Ring.LowerRight)
                {
                    return rig.BodyFace("Carbon");
                }
                return rig.PaintFace();
            }, rig.BodyFace("Carbon"), rig.PaintFace("PaintShade"));
            var cabin = new[]
            {
                Ring.Cabin(-1.36f, 0.79f, 0.83f, 0.76f, 0.74f, 0.02f),
                Ring.Cabin(-0.5f, 0.71f, 1.08f, 0.78f, 0.56f, 0.14f),
                Ring.Cabin(0.05f, 0.66f, 1.08f, 0.79f, 0.57f, 0.14f),
                Ring.Cabin(0.98f, 0.58f, 0.62f, 0.8f, 0.78f, 0.02f)
            };
            Loft(cabin, (s, band) =>
            {
                switch (band)
                {
                    case Ring.CabinBottom: return Face.None;
                    case Ring.CabinTop: return s == 2 ? rig.BodyFace("Glass") : rig.BodyFace("Carbon");
                    case Ring.CabinChamferLeft:
                    case Ring.CabinChamferRight: return rig.BodyFace("CarbonLight");
                    default: return s == 0 ? rig.BodyFace("Carbon") : rig.BodyFace("Glass");
                }
            }, rig.BodyFace("Carbon"), rig.BodyFace("Carbon"));
            Shine(rig, cabin, 2, Ring.CabinTop, 0.2f, 0.12f);
            for (int i = 0; i < 5; i++)
            {
                float v = 0.14f + i * 0.16f;
                Panel(rig.Body, "DarkGray", cabin, 0, Ring.CabinTop, 0.1f, 0.9f, v, v + 0.05f, 0.008f);
            }
            Stripe(rig.Body, "Carbon", body, -0.13f, 0.13f, 0.98f, 2.3f);

            for (int side = -1; side <= 1; side += 2)
            {
                rig.B("Grille").BeveledBox(new Vector3(side * 0.97f, 0.42f, -0.62f), new Vector3(0.08f, 0.2f, 0.8f), 0.02f);
                rig.B("Carbon").Box(new Vector3(side * 0.6f, 0.86f, -2.0f), new Vector3(0.05f, 0.16f, 0.22f));
                // Gun pods on the front wings.
                rig.B("Carbon").BeveledBox(new Vector3(side * 0.58f, 0.565f, 1.35f), new Vector3(0.18f, 0.1f, 0.72f), 0.03f);
                rig.B("GunDark").Rod(new Vector3(side * 0.58f, 0.565f, 1.7f), new Vector3(side * 0.58f, 0.565f, 2.2f), 0.028f, 6, false);
                rig.B("GunDark").Rod(new Vector3(side * 0.58f, 0.565f, 2.17f), new Vector3(side * 0.58f, 0.565f, 2.27f), 0.04f, 8, false);
                Headlight(rig, new Vector3(side * 0.55f, 0.3f, 2.26f), new Vector2(0.34f, 0.045f), new Vector3(0f, 0.6f, 1f), "Xenon", "Carbon");
                rig.Beams.Add(new Vector3(side * 0.55f, 0.3f, 2.29f));
                Exhaust(rig, new Vector3(side * 0.2f, 0.38f, -2.28f), Vector3.back, 0.06f, 0.2f, "Carbon", "ChromeDark");
                Mirror(rig, new Vector3(side * 0.8f, 0.66f, 0.85f), side, 0.8f);
            }
            rig.Muzzle = new Vector3(0f, 0.565f, 2.3f);
            rig.B("Carbon").BeveledBox(new Vector3(0f, 0.96f, -2.04f), new Vector3(1.92f, 0.035f, 0.34f), 0.01f);
            for (int i = -2; i <= 2; i++)
            {
                rig.B("Carbon").Box(new Vector3(i * 0.3f, 0.26f, -2.24f), new Vector3(0.03f, 0.14f, 0.2f));
            }
            rig.B("Carbon").Box(new Vector3(0f, 0.2f, 2.22f), new Vector3(1.6f, 0.03f, 0.22f));
            Taillight(rig, new Vector3(0f, 0.66f, -2.245f), new Vector2(1.5f, 0.05f), "Taillight", "Carbon");
            Arches(rig, 1.0f, 0.045f, "Carbon");
            rig.MineDrop = new Vector3(0f, 0.3f, -2.5f);
            rig.RocketMount = new Vector3(0f, 0.95f, -1.15f);
        }

        // ------------------------------------------------------------------ Tyrant

        /// <summary>
        /// An armoured interceptor: slab sides, armoured skirts over the wheels, a plated cabin with slit windows, bonnet
        /// plates, a hazard-striped ram, a roof turret with twin barrels, rocket pods behind the cabin, exhaust stacks,
        /// spotlights and a beacon.
        /// </summary>
        private static void Tyrant(CarRig rig)
        {
            Wheels(rig, 0.43f, 0.36f, 0.93f, 1.62f, 0.43f, 0.36f, 0.93f, -1.6f, "Gunmetal", true, 6);
            var body = new[]
            {
                Ring.Body(-2.48f, 0.36f, 0.72f, 0.98f, 1.06f, 0.12f, 0.08f, 0.08f),
                Ring.Body(-2.3f, 0.3f, 0.74f, 1.02f, 1.14f, 0.14f, 0.08f, 0.08f),
                Ring.Body(1.95f, 0.3f, 0.74f, 1.0f, 1.14f, 0.14f, 0.08f, 0.08f),
                Ring.Body(2.35f, 0.34f, 0.68f, 0.86f, 1.08f, 0.12f, 0.08f, 0.08f),
                Ring.Body(2.48f, 0.38f, 0.62f, 0.76f, 0.98f, 0.1f, 0.08f, 0.08f)
            };
            Loft(body, (s, band) =>
            {
                if (band == Ring.LowerLeft || band == Ring.LowerRight)
                {
                    return rig.PaintFace("PaintDark");
                }
                return rig.StandardBody(s, band);
            }, rig.PaintFace("PaintDark"), rig.PaintFace("PaintDark"));
            var cabin = new[]
            {
                Ring.Cabin(-1.72f, 0.99f, 1.03f, 0.92f, 0.9f, 0.02f),
                Ring.Cabin(-1.42f, 0.99f, 1.48f, 0.92f, 0.8f, 0.12f),
                Ring.Cabin(0.3f, 0.99f, 1.5f, 0.92f, 0.8f, 0.12f),
                Ring.Cabin(0.86f, 0.99f, 1.03f, 0.92f, 0.9f, 0.02f)
            };
            Loft(cabin, (s, band) =>
            {
                switch (band)
                {
                    case Ring.CabinBottom: return Face.None;
                    case Ring.CabinTop: return s == 1 ? rig.PaintFace() : s == 2 ? rig.BodyFace("Plate") : rig.PaintFace("PaintShade");
                    case Ring.CabinChamferLeft:
                    case Ring.CabinChamferRight: return rig.PaintFace("PaintDark");
                    default: return rig.PaintFace("PaintShade");
                }
            }, rig.PaintFace("PaintShade"), rig.PaintFace("PaintShade"));
            // Slit windows with bars.
            Panel(rig.Body, "Glass", cabin, 2, Ring.CabinTop, 0.12f, 0.88f, 0.34f, 0.62f, 0.012f);
            foreach (float u in new[] { 0.3f, 0.5f, 0.7f })
            {
                Panel(rig.Body, "Black", cabin, 2, Ring.CabinTop, u - 0.012f, u + 0.012f, 0.32f, 0.64f, 0.02f);
            }
            foreach ((float from, float to) in new[] { (0.05f, 0.38f), (0.5f, 0.92f) })
            {
                Panel(rig.Body, "Glass", cabin, 1, Ring.CabinRight, 0.45f, 0.8f, from, to, 0.01f);
                Panel(rig.Body, "Glass", cabin, 1, Ring.CabinLeft, 0.2f, 0.55f, from, to, 0.01f);
            }

            // Bonnet plates with rivets.
            for (int side = -1; side <= 1; side += 2)
            {
                rig.Body.Push(new Vector3(side * 0.46f, 1.03f, 1.42f), Quaternion.Euler(0f, 0f, -side * 5f));
                rig.B("Plate").BeveledBox(Vector3.zero, new Vector3(0.84f, 0.06f, 1.5f), 0.02f);
                for (int i = 0; i < 4; i++)
                {
                    rig.B("Rivet").Box(new Vector3((i % 2 == 0 ? -1f : 1f) * 0.36f, 0.035f, (i < 2 ? -1f : 1f) * 0.66f), new Vector3(0.05f, 0.02f, 0.05f));
                }
                rig.Body.Pop();
            }

            // The ram, striped.
            rig.Body.Push(new Vector3(0f, 0.46f, 2.64f), Quaternion.Euler(-28f, 0f, 0f));
            rig.B("PlateDark").BeveledBox(Vector3.zero, new Vector3(2.16f, 0.36f, 0.12f), 0.03f);
            var plate = new Rect(-1.03f, -0.14f, 2.06f, 0.28f);
            for (float x = plate.xMin - plate.height; x < plate.xMax; x += 0.32f)
            {
                var stripe = new[]
                {
                    new Vector2(x, plate.yMin), new Vector2(x + 0.16f, plate.yMin),
                    new Vector2(x + 0.16f + plate.height, plate.yMax), new Vector2(x + plate.height, plate.yMax)
                };
                List<Vector2> clipped = LowPolyBuilder.Clip(stripe, plate);
                if (clipped.Count >= 3)
                {
                    rig.B("Hazard").Prism(clipped, 0.02f, new Vector3(0f, 0f, 0.062f));
                }
            }
            rig.Body.Pop();
            for (int side = -1; side <= 1; side += 2)
            {
                Bar(rig.B("Gunmetal"), new Vector3(side * 0.62f, 0.48f, 2.62f), new Vector3(side * 0.62f, 0.5f, 2.38f), 0.1f);
            }

            // The turret.
            rig.B("PlateDark").Cylinder(new Vector3(0f, 1.48f, -0.6f), 0.54f, 0.52f, 0.09f, 16);
            rig.P("PaintDark").BeveledBox(new Vector3(0f, 1.73f, -0.66f), new Vector3(0.86f, 0.34f, 1.02f), 0.08f);
            rig.B("Plate").BeveledBox(new Vector3(0f, 1.91f, -0.76f), new Vector3(0.62f, 0.05f, 0.6f), 0.02f);
            rig.B("PlateDark").Cylinder(new Vector3(0.14f, 1.93f, -0.85f), 0.15f, 0.14f, 0.05f, 10);
            rig.B("Olive").BeveledBox(new Vector3(0.52f, 1.68f, -0.66f), new Vector3(0.2f, 0.26f, 0.46f), 0.03f);
            rig.B("Gunmetal").BeveledBox(new Vector3(0f, 1.7f, -0.08f), new Vector3(0.5f, 0.2f, 0.2f), 0.04f);
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 0.15f;
                rig.B("Gunmetal").Rod(new Vector3(x, 1.7f, -0.1f), new Vector3(x, 1.7f, 0.35f), 0.095f, 8, false);
                rig.B("GunDark").Rod(new Vector3(x, 1.7f, 0.35f), new Vector3(x, 1.7f, 1.12f), 0.055f, 8, false);
                rig.B("GunDark").Rod(new Vector3(x, 1.7f, 1.06f), new Vector3(x, 1.7f, 1.24f), 0.08f, 8, false);
                rig.B("Soot").Rod(new Vector3(x, 1.7f, 1.24f), new Vector3(x, 1.7f, 1.244f), 0.05f, 8, false);
            }
            rig.Muzzle = new Vector3(0f, 1.7f, 1.26f);

            // Rocket pods, exhaust stacks, skirts.
            for (int side = -1; side <= 1; side += 2)
            {
                RocketPod(rig, new Vector3(side * 0.6f, 1.17f, -1.75f), 2, 2, 0.06f, 0.7f);
                rig.B("Exhaust").Rod(new Vector3(side * 1.02f, 0.9f, -1.98f), new Vector3(side * 1.02f, 1.62f, -1.98f), 0.065f, 8, false);
                Exhaust(rig, new Vector3(side * 1.02f, 1.74f, -2.02f), new Vector3(0f, 0.55f, -0.83f), 0.07f, 0.14f);
                Headlight(rig, new Vector3(side * 0.55f, 1.56f, 0.26f), new Vector2(0.075f, 0f), Vector3.forward, "Spot", "Charcoal");
                Headlight(rig, new Vector3(side * 0.7f, 0.7f, 2.485f), new Vector2(0.24f, 0.13f), Vector3.forward, "Headlight", "Black");
                rig.Beams.Add(new Vector3(side * 0.7f, 0.7f, 2.52f));
                for (int k = -1; k <= 1; k++)
                {
                    rig.B("Black").Box(new Vector3(side * 0.7f + k * 0.08f, 0.7f, 2.53f), new Vector3(0.02f, 0.17f, 0.02f));
                }
                Taillight(rig, new Vector3(side * 0.88f, 0.84f, -2.485f), new Vector2(0.22f, 0.12f));
            }
            rig.RocketMount = new Vector3(-0.6f, 1.17f, -1.7f);
            for (int i = 0; i < 4; i++)
            {
                Vector3 wheel = rig.Wheels[i];
                float side = Mathf.Sign(wheel.x);
                rig.B("Plate").BeveledBox(new Vector3(side * 1.17f, 0.66f, wheel.z), new Vector3(0.07f, 0.34f, 1.1f), 0.02f);
                for (int k = -1; k <= 1; k++)
                {
                    rig.B("Rivet").Box(new Vector3(side * 1.21f, 0.78f, wheel.z + k * 0.42f), new Vector3(0.02f, 0.04f, 0.04f));
                }
            }
            rig.B("Beacon").Sphere(new Vector3(0.45f, 1.5f, -1.15f), new Vector3(0.07f, 0.07f, 0.07f), 4, 8, false, 0.5f);
            rig.B("Gunmetal").BeveledBox(new Vector3(0f, 0.46f, -2.5f), new Vector3(2.0f, 0.16f, 0.14f), 0.03f);
            Plate(rig, new Vector3(0f, 0.66f, -2.49f));
            rig.MineDrop = new Vector3(0f, 0.35f, -2.75f);
        }
    }
}
