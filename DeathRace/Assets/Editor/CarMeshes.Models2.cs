using System.Collections.Generic;
using Gamebox.Editor;
using UnityEngine;

namespace Portfolio.DeathRace.EditorTools
{
    /// <summary>
    /// The six cars added with the harder leagues, drawn like the first six to read from about sixty degrees above, each
    /// told apart by its silhouette: the Jackal's open roll cage and bare engine, the Mauler's tall box and battering ram,
    /// the Banshee's blower through the bonnet, the Spectre's faceted wedge with its light strips, the Goliath's cab,
    /// plough and twin guns, and the Apocalypse's long nose, fins and jet nacelles. Sizes follow
    /// <see cref="CarSpec.Size"/>; the car faces +z.
    /// </summary>
    internal static partial class CarMeshes
    {
        /// <summary>A plate of hazard stripes (yellow on black) on the front face of the builder's frame: x across, y up, facing +z.</summary>
        private static void HazardPlate(CarRig rig, Rect plate, float z, float pitch = 0.3f)
        {
            rig.B("HazardDark").Box(new Vector3(plate.center.x, plate.center.y, z - 0.004f), new Vector3(plate.width, plate.height, 0.012f));
            for (float x = plate.xMin - plate.height; x < plate.xMax; x += pitch)
            {
                var stripe = new[]
                {
                    new Vector2(x, plate.yMin), new Vector2(x + pitch * 0.5f, plate.yMin),
                    new Vector2(x + pitch * 0.5f + plate.height, plate.yMax), new Vector2(x + plate.height, plate.yMax)
                };
                List<Vector2> clipped = LowPolyBuilder.Clip(stripe, plate);
                if (clipped.Count >= 3)
                {
                    rig.B("Hazard").Prism(clipped, 0.012f, new Vector3(0f, 0f, z + 0.004f));
                }
            }
        }

        /// <summary>
        /// A nine-sided section with a ridge along the middle of its top, for faceted bodies: the sill, the beltline, the
        /// shoulder (<paramref name="tumble"/> in, <paramref name="chamfer"/> down), the edges of the top and the ridge
        /// <paramref name="ridge"/> above them. Its bands are those of <see cref="Ring.Body"/> with the top split in two
        /// (4 right, 5 left) and the left side one further on (6 chamfer, 7 upper, 8 lower).
        /// </summary>
        private static Ring Ridged(float z, float yBottom, float yBelt, float yTop, float half, float chamfer, float tuck, float tumble, float ridge)
        {
            float sill = half - tuck;
            float shoulder = half - tumble;
            float top = shoulder - chamfer;
            return new Ring(z, Ring.Top,
                new Vector2(-sill, yBottom), new Vector2(sill, yBottom), new Vector2(half, yBelt), new Vector2(shoulder, yTop - chamfer),
                new Vector2(top, yTop), new Vector2(0f, yTop + ridge), new Vector2(-top, yTop), new Vector2(-shoulder, yTop - chamfer),
                new Vector2(-half, yBelt));
        }

        // ------------------------------------------------------------------ Jackal

        /// <summary>
        /// A dune buggy: a low tub with painted side pods, the driver in the open under a tube roll cage (a light bar on
        /// its front hoop, the gun on its roof), a spare tyre strapped to the nose, the flat-four engine bare at the back
        /// with its pipes curling up, skinny front tyres under cycle fenders, fat knobbly rear ones and a whip antenna.
        /// </summary>
        private static void Jackal(CarRig rig)
        {
            Wheels(rig, 0.34f, 0.24f, 0.8f, 1.3f, 0.44f, 0.4f, 0.77f, -1.22f, "White", true, 6);
            var body = new[]
            {
                Ring.Body(-1.72f, 0.36f, 0.5f, 0.6f, 0.44f, 0.05f, 0.05f, 0.04f),
                Ring.Body(-1.58f, 0.3f, 0.52f, 0.64f, 0.5f, 0.06f, 0.05f, 0.04f),
                Ring.Body(-0.74f, 0.28f, 0.5f, 0.64f, 0.52f, 0.06f, 0.05f, 0.04f),
                Ring.Body(-0.64f, 0.27f, 0.42f, 0.48f, 0.56f, 0.03f, 0.05f, 0.03f),
                Ring.Body(0.46f, 0.27f, 0.42f, 0.48f, 0.56f, 0.03f, 0.05f, 0.03f),
                Ring.Body(0.58f, 0.28f, 0.5f, 0.7f, 0.6f, 0.06f, 0.06f, 0.05f),
                Ring.Body(1.3f, 0.3f, 0.5f, 0.74f, 0.6f, 0.07f, 0.06f, 0.05f),
                Ring.Body(1.82f, 0.34f, 0.48f, 0.62f, 0.55f, 0.06f, 0.06f, 0.05f),
                Ring.Body(1.95f, 0.4f, 0.47f, 0.54f, 0.46f, 0.05f, 0.05f, 0.04f)
            };
            Loft(body, (s, band) =>
            {
                bool top = band == Ring.Top || band == Ring.ChamferLeft || band == Ring.ChamferRight;
                if (!top)
                {
                    return rig.StandardBody(s, band);
                }
                switch (s)
                {
                    case 0:
                    case 1: return rig.BodyFace("Charcoal");
                    case 2: return rig.BodyFace("DarkGray");
                    case 3: return rig.BodyFace(band == Ring.Top ? "Interior" : "Charcoal");
                    case 4: return rig.BodyFace("Charcoal");
                    default: return rig.PaintFace();
                }
            }, rig.PaintFace("PaintShade"), rig.PaintFace("PaintShade"));

            // Side pods, the walls of the open cockpit, with nerf bars outside them.
            for (int side = -1; side <= 1; side += 2)
            {
                rig.P().BeveledBox(new Vector3(side * 0.66f, 0.5f, -0.1f), new Vector3(0.22f, 0.4f, 1.16f), 0.05f);
                rig.B("Black").Box(new Vector3(side * 0.565f, 0.705f, -0.1f), new Vector3(0.05f, 0.03f, 1.08f));
                Bar(rig.B("Gunmetal"), new Vector3(side * 0.86f, 0.42f, -0.62f), new Vector3(side * 0.86f, 0.42f, 0.8f), 0.06f);
                Bar(rig.B("Gunmetal"), new Vector3(side * 0.76f, 0.42f, -0.5f), new Vector3(side * 0.86f, 0.42f, -0.56f), 0.05f);
                Bar(rig.B("Gunmetal"), new Vector3(side * 0.62f, 0.42f, 0.66f), new Vector3(side * 0.86f, 0.42f, 0.76f), 0.05f);
            }

            // The seat and the driver, helmet in the driver's colour.
            rig.B("Seat").BeveledBox(new Vector3(0f, 0.56f, -0.2f), new Vector3(0.5f, 0.16f, 0.52f), 0.04f);
            rig.Body.Push(new Vector3(0f, 0.86f, -0.5f), Quaternion.Euler(-14f, 0f, 0f));
            rig.B("Seat").BeveledBox(Vector3.zero, new Vector3(0.5f, 0.64f, 0.12f), 0.04f);
            rig.Body.Pop();
            rig.B("Charcoal").BeveledBox(new Vector3(0f, 0.8f, -0.33f), new Vector3(0.42f, 0.42f, 0.26f), 0.08f);
            for (int side = -1; side <= 1; side += 2)
            {
                rig.B("Charcoal").Rod(new Vector3(side * 0.2f, 0.92f, -0.3f), new Vector3(side * 0.13f, 0.86f, 0.08f), 0.045f, 6, false);
                rig.B("Hazard").Box(new Vector3(side * 0.09f, 0.86f, -0.195f), new Vector3(0.05f, 0.34f, 0.02f));
            }
            rig.P().Sphere(new Vector3(0f, 1.08f, -0.3f), new Vector3(0.15f, 0.16f, 0.17f), 6, 10, true);
            rig.B("Glass").BeveledBox(new Vector3(0f, 1.07f, -0.15f), new Vector3(0.22f, 0.08f, 0.05f), 0.015f);
            rig.Body.Push(new Vector3(0f, 0.86f, 0.12f), Quaternion.Euler(-62f, 0f, 0f));
            rig.B("Black").Torus(Vector3.zero, 0.14f, 0.022f, 12, 4, 360f, false);
            rig.Body.Pop();
            rig.B("DarkGray").Rod(new Vector3(0f, 0.85f, 0.14f), new Vector3(0f, 0.7f, 0.5f), 0.025f, 6, false);

            // The roll cage: a main hoop behind the seat, a front hoop from the dash, roof rails, a cross bar under the
            // gun plate and struts back to the frame round the engine.
            const float roof = 1.3f;
            LowPolyBuilder cage = rig.B("Gunmetal");
            for (int side = -1; side <= 1; side += 2)
            {
                Bar(cage, new Vector3(side * 0.56f, 0.6f, -0.66f), new Vector3(side * 0.5f, roof, -0.62f), 0.08f);
                Bar(cage, new Vector3(side * 0.58f, 0.7f, 0.56f), new Vector3(side * 0.47f, roof, 0.1f), 0.075f);
                Bar(cage, new Vector3(side * 0.5f, roof, -0.62f), new Vector3(side * 0.47f, roof, 0.1f), 0.075f);
                Bar(cage, new Vector3(side * 0.5f, roof, -0.62f), new Vector3(side * 0.42f, 0.66f, -1.66f), 0.07f);
                cage.Sphere(new Vector3(side * 0.5f, roof, -0.62f), 0.05f, 4, 6, false);
                cage.Sphere(new Vector3(side * 0.47f, roof, 0.1f), 0.05f, 4, 6, false);
            }
            Bar(cage, new Vector3(-0.5f, roof, -0.62f), new Vector3(0.5f, roof, -0.62f), 0.08f);
            Bar(cage, new Vector3(-0.47f, roof, 0.1f), new Vector3(0.47f, roof, 0.1f), 0.075f);
            Bar(cage, new Vector3(-0.49f, roof, -0.26f), new Vector3(0.49f, roof, -0.26f), 0.06f);
            Bar(cage, new Vector3(-0.55f, 0.62f, -0.65f), new Vector3(0.5f, roof - 0.04f, -0.62f), 0.06f);
            Bar(cage, new Vector3(-0.44f, 0.66f, -1.68f), new Vector3(0.44f, 0.66f, -1.68f), 0.07f);

            // The light bar on the front hoop and the gun on a plate across the roof.
            rig.B("Charcoal").BeveledBox(new Vector3(0f, roof + 0.07f, 0.12f), new Vector3(1.0f, 0.08f, 0.12f), 0.02f);
            for (int i = 0; i < 4; i++)
            {
                Headlight(rig, new Vector3(-0.36f + i * 0.24f, roof + 0.07f, 0.19f), new Vector2(0.17f, 0.06f), Vector3.forward, "Spot", "Charcoal");
            }
            rig.B("PlateDark").BeveledBox(new Vector3(0.22f, roof + 0.045f, -0.42f), new Vector3(0.44f, 0.03f, 0.42f), 0.01f);
            rig.Muzzle = MachineGun(rig, new Vector3(0.22f, roof + 0.06f, -0.42f), 0.75f, 1);
            rig.RocketMount = new Vector3(-0.3f, roof + 0.14f, -0.2f);

            // The spare tyre strapped on the nose.
            rig.B("Rubber").Cylinder(new Vector3(0f, 0.7f, 1.38f), 0.27f, 0.27f, 0.14f, 14, true);
            rig.B("White").Cylinder(new Vector3(0f, 0.835f, 1.38f), 0.16f, 0.16f, 0.012f, 12);
            rig.B("Hub").Cylinder(new Vector3(0f, 0.84f, 1.38f), 0.06f, 0.05f, 0.02f, 8);
            rig.B("Strap").Box(new Vector3(0f, 0.775f, 1.38f), new Vector3(0.06f, 0.15f, 0.58f));

            // The engine, bare at the back: crankcase, finned cylinders, air filter, fan housing, pipes up and out.
            rig.B("Gunmetal").BeveledBox(new Vector3(0f, 0.8f, -1.2f), new Vector3(0.5f, 0.34f, 0.72f), 0.05f);
            rig.B("Black").BeveledBox(new Vector3(0f, 0.99f, -1.42f), new Vector3(0.38f, 0.2f, 0.28f), 0.04f);
            rig.B("Red").Cylinder(new Vector3(0f, 0.97f, -1.02f), 0.15f, 0.15f, 0.12f, 12);
            rig.B("Chrome").Cylinder(new Vector3(0f, 1.09f, -1.02f), 0.16f, 0.12f, 0.03f, 12);
            for (int side = -1; side <= 1; side += 2)
            {
                rig.B("LightGray").BeveledBox(new Vector3(side * 0.36f, 0.76f, -1.2f), new Vector3(0.22f, 0.22f, 0.56f), 0.04f);
                for (int k = 0; k < 5; k++)
                {
                    rig.B("Gray").Box(new Vector3(side * 0.36f, 0.76f, -1.42f + k * 0.11f), new Vector3(0.25f, 0.25f, 0.025f));
                }
                rig.B("Charcoal").BeveledBox(new Vector3(side * 0.49f, 0.76f, -1.2f), new Vector3(0.06f, 0.2f, 0.5f), 0.02f);
                rig.B("Exhaust").Rod(new Vector3(side * 0.36f, 0.62f, -1.48f), new Vector3(side * 0.24f, 0.7f, -1.68f), 0.04f, 6, false);
                Exhaust(rig, new Vector3(side * 0.22f, 0.94f, -1.92f), new Vector3(0f, 0.55f, -0.85f), 0.045f, 0.34f, "Exhaust", "Chrome");
                Taillight(rig, new Vector3(side * 0.42f, 0.66f, -1.74f), new Vector2(0.055f, 0f));
            }
            Bar(rig.B("Gunmetal"), new Vector3(-0.5f, 0.4f, -1.8f), new Vector3(0.5f, 0.4f, -1.8f), 0.07f);
            for (int side = -1; side <= 1; side += 2)
            {
                Bar(rig.B("Gunmetal"), new Vector3(side * 0.42f, 0.4f, -1.8f), new Vector3(side * 0.42f, 0.66f, -1.68f), 0.06f);
            }
            Plate(rig, new Vector3(0f, 0.5f, -1.74f));

            // The whip antenna with its orange flag.
            rig.B("Black").Rod(new Vector3(0.42f, 0.68f, -1.66f), new Vector3(0.5f, 2.3f, -1.9f), 0.008f, 4, false);
            rig.Body.Push(new Vector3(0.5f, 2.28f, -1.9f), Quaternion.Euler(0f, -90f, 0f));
            rig.B("Orange").Prism(new[] { new Vector2(0f, 0f), new Vector2(0f, -0.15f), new Vector2(-0.24f, -0.075f) }, 0.012f);
            rig.Body.Pop();

            // Front: round lamps on the nose, a nudge bar, cycle fenders over the skinny front wheels.
            for (int side = -1; side <= 1; side += 2)
            {
                Headlight(rig, new Vector3(side * 0.3f, 0.52f, 1.955f), new Vector2(0.075f, 0f), new Vector3(0f, 0.15f, 1f));
                rig.Beams.Add(new Vector3(side * 0.3f, 0.52f, 1.99f));
                Bar(rig.B("Gunmetal"), new Vector3(side * 0.36f, 0.3f, 2.04f), new Vector3(side * 0.36f, 0.64f, 2.02f), 0.06f);
                Bar(rig.B("Gunmetal"), new Vector3(side * 0.36f, 0.4f, 2.04f), new Vector3(side * 0.36f, 0.42f, 1.84f), 0.05f);
            }
            Bar(rig.B("Gunmetal"), new Vector3(-0.42f, 0.64f, 2.02f), new Vector3(0.42f, 0.64f, 2.02f), 0.06f);
            Bar(rig.B("Gunmetal"), new Vector3(-0.42f, 0.36f, 2.05f), new Vector3(0.42f, 0.36f, 2.05f), 0.06f);
            Fender(rig, 0, 0.28f, 0.05f, 30f, 160f, null, 6);
            Fender(rig, 1, 0.28f, 0.05f, 30f, 160f, null, 6);
            for (int side = -1; side <= 1; side += 2)
            {
                rig.B("Rubber").Box(new Vector3(side * 0.77f, 0.3f, -1.75f), new Vector3(0.34f, 0.28f, 0.025f));
            }
            rig.MineDrop = new Vector3(0f, 0.32f, -2.1f);
        }

        // ------------------------------------------------------------------ Mauler

        /// <summary>
        /// An armoured security van: a tall slab box on a short bonnet, riveted plates on its sides, slit windows, an
        /// armoured windscreen, a round roof hatch, a gun behind a shield on the roof, amber beacons, barred headlights,
        /// rear doors, and a battering ram braced in front of the bumper.
        /// </summary>
        private static void Mauler(CarRig rig)
        {
            Wheels(rig, 0.44f, 0.36f, 0.95f, 1.6f, 0.44f, 0.36f, 0.95f, -1.62f, "Gunmetal", false, 6);
            var body = new[]
            {
                Ring.Body(-2.5f, 0.42f, 0.72f, 1.0f, 1.08f, 0.06f, 0.05f, 0.03f),
                Ring.Body(-2.42f, 0.34f, 0.74f, 1.04f, 1.14f, 0.07f, 0.06f, 0.03f),
                Ring.Body(1.62f, 0.34f, 0.74f, 1.04f, 1.14f, 0.07f, 0.06f, 0.03f),
                Ring.Body(2.12f, 0.36f, 0.72f, 1.0f, 1.1f, 0.1f, 0.06f, 0.05f),
                Ring.Body(2.27f, 0.42f, 0.68f, 0.9f, 1.02f, 0.08f, 0.06f, 0.05f)
            };
            Loft(body, (s, band) => band == Ring.LowerLeft || band == Ring.LowerRight ? rig.PaintFace("PaintDark") : rig.StandardBody(s, band),
                rig.PaintFace("PaintDark"), rig.PaintFace("PaintDark"));
            var box = new[]
            {
                Ring.Cabin(-2.44f, 1.0f, 1.06f, 1.12f, 1.11f, 0.02f),
                Ring.Cabin(-2.4f, 1.0f, 1.84f, 1.12f, 1.07f, 0.08f),
                Ring.Cabin(0.98f, 1.0f, 1.84f, 1.12f, 1.07f, 0.08f),
                Ring.Cabin(1.56f, 1.0f, 1.08f, 1.12f, 1.1f, 0.02f)
            };
            Loft(box, (s, band) =>
            {
                switch (band)
                {
                    case Ring.CabinBottom: return Face.None;
                    case Ring.CabinTop: return s == 1 ? rig.PaintFace() : s == 2 ? rig.BodyFace("PlateDark") : rig.PaintFace("PaintShade");
                    case Ring.CabinChamferLeft:
                    case Ring.CabinChamferRight: return rig.PaintFace("PaintDark");
                    default: return rig.PaintFace("PaintShade");
                }
            }, rig.PaintFace("PaintShade"), rig.PaintFace("PaintShade"));

            // The armoured windscreen: a slit of glass behind three bars.
            Panel(rig.Body, "Glass", box, 2, Ring.CabinTop, 0.1f, 0.9f, 0.2f, 0.44f, 0.012f);
            Panel(rig.Body, "GlassShine", box, 2, Ring.CabinTop, 0.16f, 0.26f, 0.24f, 0.4f, 0.016f);
            foreach (float u in new[] { 0.3f, 0.5f, 0.7f })
            {
                Panel(rig.Body, "Black", box, 2, Ring.CabinTop, u - 0.012f, u + 0.012f, 0.18f, 0.46f, 0.02f);
            }
            // Slit windows of the cab and gun ports of the box; riveted plates along both sides.
            foreach (int band in new[] { Ring.CabinRight, Ring.CabinLeft })
            {
                bool right = band == Ring.CabinRight;
                float U(float u) => right ? u : 1f - u;
                Panel(rig.Body, "Glass", box, 1, band, U(0.5f), U(0.66f), 0.84f, 0.97f, 0.012f);
                foreach (float v in new[] { 0.22f, 0.5f })
                {
                    Panel(rig.Body, "Black", box, 1, band, U(0.55f), U(0.6f), v, v + 0.06f, 0.024f);
                }
                foreach ((float from, float to) in new[] { (0.04f, 0.33f), (0.37f, 0.66f) })
                {
                    RivetedPanel(rig, "Plate", box, 1, band, U(0.12f), U(0.44f), from, to);
                }
                RivetedPanel(rig, "Plate", box, 1, band, U(0.12f), U(0.44f), 0.7f, 0.95f);
            }

            // The roof: plate seams with rivets, the hatch, vents, beacons and the gun behind its shield.
            foreach (float v in new[] { 0.3f, 0.62f })
            {
                Panel(rig.Paint, "PaintDark", box, 1, Ring.CabinTop, 0f, 1f, v - 0.004f, v + 0.004f, 0.006f);
                float z = Mathf.Lerp(-2.4f, 0.98f, v);
                Rivets(rig, new Vector3(-0.9f, 1.845f, z - 0.05f), new Vector3(0.9f, 1.845f, z - 0.05f), 7, Vector3.up);
            }
            rig.B("PlateDark").Cylinder(new Vector3(-0.42f, 1.84f, -0.9f), 0.34f, 0.32f, 0.07f, 14);
            rig.B("Plate").Cylinder(new Vector3(-0.42f, 1.9f, -0.9f), 0.28f, 0.26f, 0.04f, 14);
            rig.B("Gunmetal").Rod(new Vector3(-0.56f, 1.97f, -0.9f), new Vector3(-0.28f, 1.97f, -0.9f), 0.02f, 6, false);
            rig.B("Gunmetal").Box(new Vector3(-0.42f, 1.89f, -1.25f), new Vector3(0.24f, 0.06f, 0.07f));
            for (int side = -1; side <= 1; side += 2)
            {
                rig.B("Grille").BeveledBox(new Vector3(side * 0.5f, 1.86f, -2.0f), new Vector3(0.42f, 0.05f, 0.3f), 0.01f);
                for (int k = 0; k < 4; k++)
                {
                    rig.B("DarkGray").Box(new Vector3(side * 0.5f, 1.89f, -2.11f + k * 0.075f), new Vector3(0.4f, 0.015f, 0.02f));
                }
                rig.B("Charcoal").Cylinder(new Vector3(side * 0.88f, 1.84f, 0.82f), 0.07f, 0.07f, 0.03f, 8);
                rig.B("Indicator").Sphere(new Vector3(side * 0.88f, 1.87f, 0.82f), new Vector3(0.075f, 0.09f, 0.075f), 4, 8, false, 0.5f);
            }
            rig.Muzzle = MachineGun(rig, new Vector3(0.42f, 1.84f, 0.1f), 1.0f, 1);
            rig.B("Plate").BeveledBox(new Vector3(0.42f, 2.1f, 0.6f), new Vector3(0.62f, 0.34f, 0.05f), 0.015f);
            Rivets(rig, new Vector3(0.16f, 2.22f, 0.63f), new Vector3(0.68f, 2.22f, 0.63f), 4, Vector3.forward, 0.035f);
            RocketPod(rig, new Vector3(-0.5f, 1.96f, 0.72f), 1, 2, 0.06f, 0.6f);
            rig.B("Gunmetal").Box(new Vector3(-0.5f, 1.86f, 0.42f), new Vector3(0.22f, 0.06f, 0.3f));
            rig.RocketMount = new Vector3(-0.5f, 1.96f, 0.82f);

            // The ram: a heavy beam with a steel head, braced back to the bumper corners.
            rig.B("Gunmetal").BeveledBox(new Vector3(0f, 0.56f, 2.32f), new Vector3(2.12f, 0.26f, 0.2f), 0.04f);
            rig.B("Gunmetal").Rod(new Vector3(0f, 0.6f, 2.3f), new Vector3(0f, 0.6f, 2.5f), 0.12f, 8, false);
            rig.Body.Push(new Vector3(0f, 0.6f, 2.48f), Quaternion.Euler(90f, 0f, 0f));
            rig.B("PlateDark").Cylinder(Vector3.zero, 0.26f, 0.21f, 0.12f, 8, false, true, true, Mathf.PI / 8f);
            rig.Body.Pop();
            Rivets(rig, new Vector3(-0.12f, 0.6f, 2.605f), new Vector3(0.12f, 0.6f, 2.605f), 2, Vector3.forward, 0.04f);
            Rivets(rig, new Vector3(0f, 0.48f, 2.605f), new Vector3(0f, 0.72f, 2.605f), 2, Vector3.forward, 0.04f);
            for (int side = -1; side <= 1; side += 2)
            {
                Bar(rig.B("Gunmetal"), new Vector3(side * 0.75f, 0.56f, 2.4f), new Vector3(side * 0.12f, 0.6f, 2.52f), 0.09f);
            }

            // Front: grille and headlights behind bars, mirrors on stalks.
            Grille(rig, new Vector3(0f, 0.77f, 2.285f), new Vector2(0.86f, 0.22f), 4, "PlateLight");
            for (int side = -1; side <= 1; side += 2)
            {
                Headlight(rig, new Vector3(side * 0.72f, 0.77f, 2.285f), new Vector2(0.22f, 0.13f), Vector3.forward, "Headlight", "Black");
                rig.Beams.Add(new Vector3(side * 0.72f, 0.77f, 2.32f));
                for (int k = -1; k <= 1; k++)
                {
                    rig.B("Black").Box(new Vector3(side * 0.72f + k * 0.07f, 0.77f, 2.33f), new Vector3(0.02f, 0.16f, 0.02f));
                }
                Mirror(rig, new Vector3(side * 1.12f, 1.42f, 1.15f), side, 1.5f);
                rig.B("Plate").BeveledBox(new Vector3(side * 1.165f, 0.66f, 0f), new Vector3(0.04f, 0.4f, 2.0f), 0.015f);
                Rivets(rig, new Vector3(side * 1.19f, 0.8f, -0.9f), new Vector3(side * 1.19f, 0.8f, 0.9f), 5, new Vector3(side, 0f, 0f));
            }

            // Back: the doors (a seam, handles, slit windows), lamps high and low, the step bumper.
            Panel(rig.Paint, "PaintDark", box, 0, Ring.CabinTop, 0.495f, 0.505f, 0.04f, 0.97f, 0.012f);
            Panel(rig.Body, "Glass", box, 0, Ring.CabinTop, 0.2f, 0.36f, 0.64f, 0.76f, 0.012f);
            Panel(rig.Body, "Glass", box, 0, Ring.CabinTop, 0.64f, 0.8f, 0.64f, 0.76f, 0.012f);
            for (int side = -1; side <= 1; side += 2)
            {
                rig.B("Chrome").Box(new Vector3(side * 0.12f, 1.34f, -2.46f), new Vector3(0.05f, 0.16f, 0.03f));
                Taillight(rig, new Vector3(side * 0.92f, 1.62f, -2.45f), new Vector2(0.14f, 0.24f));
                Taillight(rig, new Vector3(side * 0.82f, 0.84f, -2.51f), new Vector2(0.26f, 0.1f));
            }
            rig.B("Gunmetal").BeveledBox(new Vector3(0f, 0.48f, -2.57f), new Vector3(2.0f, 0.14f, 0.22f), 0.03f);
            Plate(rig, new Vector3(0f, 0.68f, -2.51f));
            Exhaust(rig, new Vector3(0.7f, 0.36f, -2.64f), Vector3.back, 0.05f, 0.4f);
            Arches(rig, 1.15f, 0.07f, "Black", 0.06f);
            rig.MineDrop = new Vector3(0f, 0.35f, -2.8f);
        }

        // ------------------------------------------------------------------ Banshee

        /// <summary>
        /// A supercharged muscle car: a coke-bottle body with wide haunches over fat rear tyres, a fastback, a chrome
        /// blower and scoop through the bonnet, broad black racing stripes with white pinstripes, a ducktail, side pipes
        /// that dump out in front of the rear wheels, gold rims, triple round tail lights, and twin guns under the nose.
        /// </summary>
        private static void Banshee(CarRig rig)
        {
            Wheels(rig, 0.34f, 0.27f, 0.84f, 1.52f, 0.38f, 0.38f, 0.82f, -1.45f, "Brass", false, 5);
            var body = new[]
            {
                Ring.Body(-2.4f, 0.32f, 0.56f, 0.8f, 0.96f, 0.06f),
                Ring.Body(-2.26f, 0.27f, 0.58f, 0.88f, 1.04f, 0.07f),
                Ring.Body(-0.95f, 0.25f, 0.6f, 0.88f, 1.05f, 0.08f),
                Ring.Body(-0.4f, 0.24f, 0.57f, 0.84f, 0.95f, 0.08f),
                Ring.Body(0.7f, 0.24f, 0.57f, 0.84f, 0.95f, 0.08f),
                Ring.Body(1.5f, 0.25f, 0.58f, 0.86f, 0.99f, 0.08f),
                Ring.Body(2.22f, 0.27f, 0.54f, 0.8f, 0.97f, 0.07f),
                Ring.Body(2.4f, 0.32f, 0.5f, 0.7f, 0.88f, 0.06f)
            };
            Loft(body, rig.StandardBody, rig.PaintFace("PaintShade"), rig.PaintFace("PaintShade"));
            var cabin = new[]
            {
                Ring.Cabin(-1.98f, 0.86f, 0.9f, 0.84f, 0.82f, 0.02f),
                Ring.Cabin(-0.7f, 0.85f, 1.2f, 0.8f, 0.64f, 0.09f),
                Ring.Cabin(-0.05f, 0.84f, 1.22f, 0.8f, 0.65f, 0.09f),
                Ring.Cabin(0.68f, 0.83f, 0.87f, 0.8f, 0.78f, 0.02f)
            };
            Loft(cabin, (s, band) => CabinFace(rig, s, band), rig.PaintFace(), rig.PaintFace());
            Shine(rig, cabin, 2, Ring.CabinTop);
            Shine(rig, cabin, 0, Ring.CabinTop, 0.3f, 0.1f);

            // Broad stripes over the boot, roof and bonnet, pinstriped white on the outside.
            for (int side = -1; side <= 1; side += 2)
            {
                float x0 = side < 0 ? -0.38f : 0.12f;
                float x1 = side < 0 ? -0.12f : 0.38f;
                float p0 = side < 0 ? -0.42f : 0.39f;
                foreach ((IList<Ring> rings, float from, float to) in new (IList<Ring>, float, float)[] { (body, 0.68f, 2.42f), (cabin, -0.7f, -0.05f), (body, -2.42f, -1.98f) })
                {
                    Stripe(rig.Body, "Black", rings, x0, x1, from, to);
                    Stripe(rig.Body, "White", rings, p0, p0 + 0.03f, from, to);
                }
            }

            // The blower: a ribbed chrome case through a hole in the bonnet, the scoop on top, the drive belt in front.
            rig.B("Black").BeveledBox(new Vector3(0f, 0.86f, 1.12f), new Vector3(0.58f, 0.05f, 0.78f), 0.015f);
            rig.B("Chrome").BeveledBox(new Vector3(0f, 1.0f, 1.1f), new Vector3(0.42f, 0.27f, 0.64f), 0.06f);
            for (int i = 0; i < 7; i++)
            {
                rig.B("ChromeDark").Box(new Vector3(0f, 1.0f, 0.82f + i * 0.093f), new Vector3(0.44f, 0.22f, 0.02f));
            }
            rig.B("ChromeDark").BeveledBox(new Vector3(0f, 1.19f, 1.13f), new Vector3(0.38f, 0.12f, 0.44f), 0.03f);
            for (int side = -1; side <= 1; side += 2)
            {
                rig.B("Grille").Box(new Vector3(side * 0.09f, 1.252f, 1.13f), new Vector3(0.14f, 0.01f, 0.32f));
                rig.B("Chrome").Box(new Vector3(side * 0.09f, 1.256f, 1.13f), new Vector3(0.14f, 0.008f, 0.03f));
            }
            rig.B("Grille").Box(new Vector3(0f, 1.19f, 1.355f), new Vector3(0.32f, 0.08f, 0.02f));
            rig.Body.Push(new Vector3(0f, 0.97f, 1.44f), Quaternion.Euler(90f, 0f, 0f));
            rig.B("Black").Cylinder(Vector3.zero, 0.1f, 0.1f, 0.05f, 10);
            rig.Body.Pop();

            // Twin guns under the nose, out through the chin.
            rig.B("Charcoal").BeveledBox(new Vector3(0f, 0.3f, 2.2f), new Vector3(0.9f, 0.12f, 0.5f), 0.03f);
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 0.28f;
                rig.B("GunDark").Rod(new Vector3(x, 0.3f, 2.1f), new Vector3(x, 0.3f, 2.52f), 0.035f, 8, false);
                rig.B("GunDark").Rod(new Vector3(x, 0.3f, 2.48f), new Vector3(x, 0.3f, 2.6f), 0.048f, 8, false);
                rig.B("Soot").Rod(new Vector3(x, 0.3f, 2.6f), new Vector3(x, 0.3f, 2.604f), 0.026f, 6, false);
            }
            rig.Muzzle = new Vector3(0f, 0.3f, 2.62f);

            // Front: a wide grille between round lamps, a split chrome bumper.
            Grille(rig, new Vector3(0f, 0.55f, 2.405f), new Vector2(1.24f, 0.17f), 3, "ChromeDark");
            for (int side = -1; side <= 1; side += 2)
            {
                Headlight(rig, new Vector3(side * 0.72f, 0.56f, 2.41f), new Vector2(0.085f, 0f), Vector3.forward);
                rig.Beams.Add(new Vector3(side * 0.72f, 0.56f, 2.44f));
                rig.B("Chrome").BeveledBox(new Vector3(side * 0.56f, 0.39f, 2.43f), new Vector3(0.66f, 0.1f, 0.1f), 0.03f);
                Mirror(rig, new Vector3(side * 0.8f, 0.87f, 0.5f), side, 0.9f);
            }

            // The ducktail, the triple tail lights, the bumper.
            rig.Paint.Push(new Vector3(0f, 0.86f, -2.3f), Quaternion.Euler(14f, 0f, 0f));
            rig.P().BeveledBox(Vector3.zero, new Vector3(1.8f, 0.06f, 0.24f), 0.02f);
            rig.Paint.Pop();
            rig.B("Black").Box(new Vector3(0f, 0.66f, -2.404f), new Vector3(1.64f, 0.16f, 0.01f));
            for (int side = -1; side <= 1; side += 2)
            {
                foreach (float x in new[] { 0.32f, 0.52f, 0.72f })
                {
                    Taillight(rig, new Vector3(side * x, 0.66f, -2.41f), new Vector2(0.06f, 0f));
                }
            }
            rig.B("Chrome").BeveledBox(new Vector3(0f, 0.38f, -2.44f), new Vector3(1.8f, 0.1f, 0.1f), 0.03f);
            Plate(rig, new Vector3(0f, 0.5f, -2.41f));

            // Side pipes under the doors, dumping out ahead of the rear wheels.
            for (int side = -1; side <= 1; side += 2)
            {
                rig.B("Exhaust").Rod(new Vector3(side * 0.96f, 0.26f, 0.95f), new Vector3(side * 0.98f, 0.26f, -0.8f), 0.055f, 8, false);
                rig.B("Chrome").BeveledBox(new Vector3(side * 1.0f, 0.3f, 0.08f), new Vector3(0.05f, 0.12f, 1.2f), 0.02f);
                for (int k = 0; k < 6; k++)
                {
                    rig.B("ChromeDark").Box(new Vector3(side * 1.026f, 0.3f, -0.4f + k * 0.18f), new Vector3(0.01f, 0.08f, 0.06f));
                }
                Exhaust(rig, new Vector3(side * 1.08f, 0.26f, -0.92f), new Vector3(side * 0.6f, -0.1f, -0.8f), 0.06f, 0.22f, "Chrome", "ExhaustTip");
            }
            for (int i = 0; i < 4; i++)
            {
                Arch(rig, i, i < 2 ? 0.99f : 1.06f, 0.06f);
            }
            rig.MineDrop = new Vector3(0f, 0.3f, -2.65f);
            rig.RocketMount = new Vector3(0f, 1.25f, -0.6f);
        }

        // ------------------------------------------------------------------ Spectre

        /// <summary>
        /// A stealth hypercar, all flat facets: a low wedge with faceted pods over the wheels, a fighter's canopy, the
        /// paint matte, cyan light strips framing the bonnet and along the doors, slit headlights, a hairline tail light,
        /// a louvred engine cover, side intakes, a nose cannon in a faceted fairing and a finned rear diffuser.
        /// </summary>
        private static void Spectre(CarRig rig)
        {
            rig.PaintGloss = 0.32f;
            Wheels(rig, 0.34f, 0.3f, 0.86f, 1.42f, 0.36f, 0.38f, 0.84f, -1.42f, "Gunmetal", false, 6);
            var body = new[]
            {
                Ridged(-2.35f, 0.3f, 0.5f, 0.72f, 0.96f, 0.16f, 0.08f, 0.14f, 0.03f),
                Ridged(-1.95f, 0.22f, 0.52f, 0.78f, 1.05f, 0.22f, 0.08f, 0.16f, 0.06f),
                Ridged(-0.85f, 0.2f, 0.5f, 0.8f, 1.05f, 0.24f, 0.08f, 0.18f, 0.06f),
                Ridged(0.25f, 0.19f, 0.42f, 0.64f, 1.02f, 0.18f, 0.08f, 0.2f, 0.07f),
                Ridged(1.35f, 0.19f, 0.36f, 0.48f, 1.0f, 0.12f, 0.08f, 0.2f, 0.08f),
                Ridged(2.05f, 0.2f, 0.3f, 0.37f, 0.92f, 0.07f, 0.06f, 0.2f, 0.06f),
                Ridged(2.38f, 0.22f, 0.26f, 0.3f, 0.42f, 0.04f, 0.04f, 0.1f, 0.02f)
            };
            Face Ridge(int s, int band)
            {
                switch (band)
                {
                    case 0:
                    case 1:
                    case 8: return rig.BodyFace("Carbon");
                    case 2:
                    case 7: return rig.BodyFace("CarbonLight");
                    case 3:
                    case 6: return rig.PaintFace("PaintShade");
                    default: return rig.PaintFace();
                }
            }

            Face Facet(int s, int band)
            {
                switch (band)
                {
                    case Ring.Bottom:
                    case Ring.LowerLeft:
                    case Ring.LowerRight: return rig.BodyFace("Carbon");
                    case Ring.UpperLeft:
                    case Ring.UpperRight: return rig.BodyFace("CarbonLight");
                    case Ring.ChamferLeft:
                    case Ring.ChamferRight: return rig.PaintFace("PaintShade");
                    default: return rig.PaintFace();
                }
            }

            Loft(body, Ridge, rig.BodyFace("Carbon"), rig.PaintFace("PaintShade"));

            // Faceted pods over the four wheels.
            foreach ((int wheel, float from, float to, float top, float half) in new[] { (0, 0.98f, 1.92f, 0.74f, 0.22f), (2, -1.98f, -0.88f, 0.84f, 0.25f) })
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    float x = side * Mathf.Abs(rig.Wheels[wheel].x);
                    float lip = top - 0.3f;
                    float third = (to - from) / 3.4f;
                    var pod = new[]
                    {
                        Ring.Body(from, lip - 0.08f, lip - 0.04f, lip + 0.02f, half - 0.02f, 0.05f, 0.02f, 0.04f).Shifted(x),
                        Ring.Body(from + third, lip - 0.08f, lip + 0.1f, top, half, 0.1f, 0.02f, 0.05f).Shifted(x),
                        Ring.Body(to - third, lip - 0.08f, lip + 0.1f, top, half, 0.1f, 0.02f, 0.05f).Shifted(x),
                        Ring.Body(to, lip - 0.08f, lip - 0.05f, lip + 0.01f, half - 0.02f, 0.05f, 0.02f, 0.04f).Shifted(x)
                    };
                    Loft(pod, Facet, rig.BodyFace("Carbon"), rig.BodyFace("Carbon"));
                }
            }

            // The canopy, small and low like a fighter's: carbon behind, glass all round, a carbon bow across it.
            var canopy = new[]
            {
                Ring.Cabin(-1.1f, 0.77f, 0.9f, 0.42f, 0.4f, 0.02f),
                Ring.Cabin(-0.5f, 0.74f, 1.0f, 0.48f, 0.27f, 0.1f),
                Ring.Cabin(0.2f, 0.64f, 0.98f, 0.48f, 0.27f, 0.1f),
                Ring.Cabin(0.95f, 0.52f, 0.64f, 0.42f, 0.38f, 0.02f)
            };
            Loft(canopy, (s, band) => band == Ring.CabinBottom ? Face.None : s == 0 ? rig.BodyFace("Carbon") : rig.BodyFace("Glass"),
                rig.BodyFace("Carbon"), rig.BodyFace("Carbon"));
            for (int band = Ring.CabinRight; band <= Ring.CabinLeft; band++)
            {
                Panel(rig.Body, "Carbon", canopy, 1, band, 0f, 1f, 0.62f, 0.7f, 0.008f);
            }
            Shine(rig, canopy, 2, Ring.CabinTop, 0.2f, 0.12f);

            // The engine cover: louvres, two angular intakes; side intakes and ducts behind the doors.
            for (int i = 0; i < 4; i++)
            {
                float v = 0.1f + i * 0.13f;
                Panel(rig.Body, "Carbon", body, 1, 4, 0.1f, 0.9f, v, v + 0.05f, 0.008f);
                Panel(rig.Body, "Carbon", body, 1, 5, 0.1f, 0.9f, v, v + 0.05f, 0.008f);
            }
            foreach (int band in new[] { 2, 7 })
            {
                Panel(rig.Body, "Grille", body, 2, band, 0.15f, 0.85f, 0.02f, 0.42f, 0.008f);
            }
            foreach (int band in new[] { 3, 6 })
            {
                Panel(rig.Body, "Carbon", body, 2, band, 0.3f, 0.7f, 0.06f, 0.4f, 0.008f);
            }

            // The light strips: a frame round the bonnet and a line along each door; the headlights are slits.
            float t = (0.95f - body[3].Z) / (body[4].Z - body[3].Z);
            float frontY = Mathf.Lerp(body[3].TopY, body[4].TopY, t) + 0.008f;
            float frontX = Mathf.Lerp(body[3].TopHalf, body[4].TopHalf, t);
            var frame = new List<Vector3>();
            for (int side = -1; side <= 1; side += 2)
            {
                frame.Clear();
                frame.Add(new Vector3(side * frontX, frontY, 0.95f));
                for (int r = 4; r <= 6; r++)
                {
                    frame.Add(new Vector3(side * body[r].TopHalf, body[r].TopY + 0.008f, body[r].Z - (r == 6 ? 0.012f : 0f)));
                }
                frame.Add(new Vector3(0f, body[6].TopY + 0.028f, body[6].Z - 0.012f));
                GlowLine(rig, "GlowCyan", 0.02f, frame.ToArray());
                GlowLine(rig, "GlowCyan", 0.02f, new Vector3(side * 1.066f, 0.5f, -0.8f), new Vector3(side * 1.032f, 0.42f, 0.25f),
                    new Vector3(side * 1.018f, 0.383f, 0.9f));
                Headlight(rig, new Vector3(side * 0.42f, 0.28f, 2.24f), new Vector2(0.28f, 0.03f), new Vector3(side * 0.6f, 0.45f, 1f), "Xenon", "Carbon");
                rig.Beams.Add(new Vector3(side * 0.42f, 0.28f, 2.28f));
                Mirror(rig, new Vector3(side * 0.6f, 0.68f, 0.7f), side, 0.7f);
            }
            Taillight(rig, new Vector3(0f, 0.62f, -2.355f), new Vector2(1.4f, 0.035f), "Taillight", "Carbon");

            // The nose cannon in a faceted fairing along the spine.
            rig.Body.Push(new Vector3(0f, 0.5f, 1.62f), Quaternion.Euler(9f, 0f, 0f));
            rig.B("Carbon").BeveledBox(Vector3.zero, new Vector3(0.24f, 0.12f, 0.95f), 0.04f);
            rig.B("CarbonLight").BeveledBox(new Vector3(0f, 0.065f, -0.1f), new Vector3(0.14f, 0.02f, 0.6f), 0.008f);
            rig.B("GunDark").Rod(new Vector3(0f, 0f, 0.45f), new Vector3(0f, 0f, 0.85f), 0.028f, 6, false);
            rig.B("GunDark").Rod(new Vector3(0f, 0f, 0.8f), new Vector3(0f, 0f, 0.9f), 0.038f, 8, false);
            rig.Body.Pop();
            rig.Muzzle = new Vector3(0f, 0.5f - 0.92f * Mathf.Sin(9f * Mathf.Deg2Rad), 1.62f + 0.92f * Mathf.Cos(9f * Mathf.Deg2Rad));

            // The diffuser: fins under the tail and two outlets in the middle of it.
            for (int i = -3; i <= 3; i++)
            {
                rig.B("Carbon").Box(new Vector3(i * 0.2f, 0.24f, -2.3f), new Vector3(0.025f, 0.16f, 0.34f));
            }
            rig.B("Carbon").Box(new Vector3(0f, 0.17f, -2.24f), new Vector3(1.4f, 0.03f, 0.3f));
            for (int side = -1; side <= 1; side += 2)
            {
                Exhaust(rig, new Vector3(side * 0.16f, 0.42f, -2.4f), Vector3.back, 0.06f, 0.2f, "Carbon", "GunDark");
            }
            rig.MineDrop = new Vector3(0f, 0.3f, -2.6f);
            rig.RocketMount = new Vector3(0f, 0.9f, -1.4f);
        }

        // ------------------------------------------------------------------ Goliath

        /// <summary>
        /// A war rig: a tall cab behind a long bonnet and a chrome grille, a V plough with hazard stripes on the nose, big
        /// fenders, twin guns on the cab roof, chrome exhaust stacks, fuel tanks, riveted armour on the doors and the bed,
        /// and a short bed carrying a six-tube rocket launcher and ammunition crates. Big knobbly tyres.
        /// </summary>
        private static void Goliath(CarRig rig)
        {
            Wheels(rig, 0.52f, 0.44f, 1.0f, 1.72f, 0.52f, 0.5f, 0.98f, -1.68f, "Gunmetal", true, 6);
            for (int side = -1; side <= 1; side += 2)
            {
                rig.B("Gunmetal").Box(new Vector3(side * 0.5f, 0.58f, -0.2f), new Vector3(0.16f, 0.22f, 5.0f));
            }

            // The bonnet, its grille and the big fenders.
            var hood = new[]
            {
                Ring.Body(0.5f, 0.66f, 0.95f, 1.3f, 0.74f, 0.08f, 0.04f, 0.04f),
                Ring.Body(2.15f, 0.66f, 0.95f, 1.28f, 0.74f, 0.1f, 0.04f, 0.04f),
                Ring.Body(2.36f, 0.7f, 0.92f, 1.2f, 0.7f, 0.08f, 0.04f, 0.04f)
            };
            Loft(hood, (s, band) => band == Ring.Bottom ? rig.BodyFace("Charcoal")
                    : band == Ring.LowerLeft || band == Ring.LowerRight ? rig.PaintFace("PaintShade") : rig.PaintFace(),
                rig.PaintFace("PaintShade"), rig.PaintFace("PaintShade"));
            foreach (int band in new[] { Ring.UpperRight, Ring.UpperLeft })
            {
                for (int i = 0; i < 4; i++)
                {
                    float v = 0.5f + i * 0.1f;
                    Panel(rig.Body, "Grille", hood, 0, band, 0.25f, 0.75f, v, v + 0.04f, 0.008f);
                }
            }
            Grille(rig, new Vector3(0f, 0.95f, 2.37f), new Vector2(1.18f, 0.44f), 6, "Chrome");
            rig.B("Chrome").BeveledBox(new Vector3(0f, 1.19f, 2.37f), new Vector3(1.28f, 0.05f, 0.06f), 0.015f);
            rig.B("Chrome").BeveledBox(new Vector3(0f, 0.71f, 2.37f), new Vector3(1.28f, 0.05f, 0.06f), 0.015f);
            Fender(rig, 0, 0.52f, 0.07f, 12f, 168f, null, 8);
            Fender(rig, 1, 0.52f, 0.07f, 12f, 168f, null, 8);

            // The cab on its lower box, fuel tanks under the doors.
            rig.P("PaintDark").BeveledBox(new Vector3(0f, 0.8f, 0f), new Vector3(1.5f, 0.42f, 1.18f), 0.04f);
            rig.P("PaintDark").BeveledBox(new Vector3(0f, 0.96f, 0f), new Vector3(2.38f, 0.1f, 1.18f), 0.03f);
            var cab = new[]
            {
                Ring.Cabin(-0.6f, 1.0f, 1.04f, 1.19f, 1.18f, 0.02f),
                Ring.Cabin(-0.56f, 1.0f, 1.95f, 1.19f, 1.12f, 0.1f),
                Ring.Cabin(0.2f, 1.0f, 1.95f, 1.19f, 1.12f, 0.1f),
                Ring.Cabin(0.58f, 1.0f, 1.32f, 1.19f, 1.17f, 0.04f)
            };
            Loft(cab, (s, band) =>
            {
                switch (band)
                {
                    case Ring.CabinBottom: return Face.None;
                    case Ring.CabinTop: return s == 1 ? rig.PaintFace() : s == 2 ? rig.BodyFace("Glass") : rig.PaintFace("PaintShade");
                    case Ring.CabinChamferLeft:
                    case Ring.CabinChamferRight: return rig.PaintFace("PaintDark");
                    default: return rig.PaintFace("PaintShade");
                }
            }, rig.PaintFace("PaintShade"), rig.PaintFace("PaintShade"));
            Shine(rig, cab, 2, Ring.CabinTop, 0.15f, 0.12f);
            foreach (float u in new[] { 0.25f, 0.5f, 0.75f })
            {
                Panel(rig.Body, "Black", cab, 2, Ring.CabinTop, u - 0.01f, u + 0.01f, 0f, 1f, 0.016f);
            }
            Panel(rig.Body, "Black", cab, 2, Ring.CabinTop, 0f, 1f, 0.48f, 0.54f, 0.016f);
            foreach (int band in new[] { Ring.CabinRight, Ring.CabinLeft })
            {
                bool right = band == Ring.CabinRight;
                Panel(rig.Body, "Glass", cab, 1, band, right ? 0.45f : 0.18f, right ? 0.82f : 0.55f, 0.6f, 0.95f, 0.01f);
                RivetedPanel(rig, "Plate", cab, 1, band, right ? 0.08f : 0.62f, right ? 0.38f : 0.92f, 0.08f, 0.92f);
            }
            for (int side = -1; side <= 1; side += 2)
            {
                rig.B("Chrome").Rod(new Vector3(side * 0.98f, 0.6f, -0.5f), new Vector3(side * 0.98f, 0.6f, 0.46f), 0.22f, 10, false);
                foreach (float z in new[] { -0.3f, 0.26f })
                {
                    rig.B("Black").Rod(new Vector3(side * 0.98f, 0.6f, z - 0.03f), new Vector3(side * 0.98f, 0.6f, z + 0.03f), 0.23f, 10, false);
                }
                Mirror(rig, new Vector3(side * 1.2f, 1.6f, 0.45f), side, 1.6f);
                Headlight(rig, new Vector3(side * 0.95f, 2.02f, 0.18f), new Vector2(0.075f, 0f), Vector3.forward, "Spot", "Charcoal");
            }
            for (int i = -1; i <= 1; i++)
            {
                rig.B("Indicator").BeveledBox(new Vector3(i * 0.22f, 1.96f, 0.2f), new Vector3(0.1f, 0.05f, 0.05f), 0.01f);
            }

            // Twin guns on a plate across the roof.
            rig.B("PlateDark").BeveledBox(new Vector3(0f, 1.97f, -0.18f), new Vector3(1.4f, 0.05f, 0.56f), 0.02f);
            Vector3 leftMuzzle = MachineGun(rig, new Vector3(-0.42f, 2.0f, -0.2f), 1.1f, 1);
            Vector3 rightMuzzle = MachineGun(rig, new Vector3(0.42f, 2.0f, -0.2f), 1.1f, 1);
            rig.Muzzle = (leftMuzzle + rightMuzzle) * 0.5f;

            // Exhaust stacks behind the cab.
            for (int side = -1; side <= 1; side += 2)
            {
                rig.B("Chrome").Rod(new Vector3(side * 1.02f, 0.9f, -0.72f), new Vector3(side * 1.02f, 2.32f, -0.72f), 0.08f, 10, false);
                rig.B("Black").Rod(new Vector3(side * 1.02f, 1.25f, -0.72f), new Vector3(side * 1.02f, 1.6f, -0.72f), 0.095f, 10, false);
                rig.B("Gunmetal").Box(new Vector3(side * 1.02f, 1.5f, -0.64f), new Vector3(0.05f, 0.05f, 0.14f));
                Exhaust(rig, new Vector3(side * 1.02f, 2.45f, -0.77f), new Vector3(0f, 1f, -0.35f), 0.085f, 0.2f, "Chrome", "ExhaustTip");
            }

            // The short bed: plated sides, the rocket launcher on its turntable, crates.
            rig.P("PaintDark").BeveledBox(new Vector3(0f, 0.92f, -1.8f), new Vector3(1.4f, 0.3f, 1.9f), 0.03f);
            rig.B("Charcoal").Box(new Vector3(0f, 1.1f, -1.8f), new Vector3(2.3f, 0.06f, 1.9f));
            for (int side = -1; side <= 1; side += 2)
            {
                rig.P("PaintShade").BeveledBox(new Vector3(side * 1.12f, 1.31f, -1.8f), new Vector3(0.08f, 0.36f, 1.9f), 0.02f);
                rig.B("Charcoal").Box(new Vector3(side * 1.12f, 1.5f, -1.8f), new Vector3(0.1f, 0.03f, 1.92f));
                foreach (float z in new[] { -2.45f, -1.8f, -1.15f })
                {
                    rig.B("Plate").BeveledBox(new Vector3(side * 1.175f, 1.3f, z), new Vector3(0.04f, 0.3f, 0.56f), 0.012f);
                    Rivets(rig, new Vector3(side * 1.198f, 1.41f, z - 0.22f), new Vector3(side * 1.198f, 1.41f, z + 0.22f), 3, new Vector3(side, 0f, 0f), 0.04f);
                }
                rig.B("Rubber").Box(new Vector3(side * 0.98f, 0.45f, -2.3f), new Vector3(0.42f, 0.4f, 0.03f));
            }
            rig.P("PaintShade").BeveledBox(new Vector3(0f, 1.31f, -2.72f), new Vector3(2.32f, 0.36f, 0.08f), 0.02f);
            rig.P("PaintShade").BeveledBox(new Vector3(0f, 1.31f, -0.88f), new Vector3(2.32f, 0.36f, 0.08f), 0.02f);
            rig.B("PlateDark").Cylinder(new Vector3(0f, 1.13f, -1.55f), 0.42f, 0.4f, 0.08f, 14);
            rig.B("Gunmetal").BeveledBox(new Vector3(0f, 1.32f, -1.6f), new Vector3(0.3f, 0.3f, 0.4f), 0.04f);
            RocketPod(rig, new Vector3(0f, 1.6f, -1.12f), 2, 3, 0.07f, 0.8f);
            rig.RocketMount = new Vector3(0f, 1.6f, -1.0f);
            for (int side = -1; side <= 1; side += 2)
            {
                rig.B("OliveDark").BeveledBox(new Vector3(side * 0.72f, 1.27f, -2.36f), new Vector3(0.42f, 0.28f, 0.5f), 0.03f);
                rig.B("Hazard").Box(new Vector3(side * 0.72f, 1.415f, -2.36f), new Vector3(0.24f, 0.01f, 0.08f));
            }
            rig.B("Red").BeveledBox(new Vector3(0.75f, 1.32f, -1.12f), new Vector3(0.2f, 0.38f, 0.3f), 0.03f);

            // The plough: two blades meeting in a V ahead of the grille, hazard striped along the top.
            for (int side = -1; side <= 1; side += 2)
            {
                var tip = new Vector3(0f, 0.56f, 2.78f);
                var end = new Vector3(side * 1.3f, 0.56f, 2.42f);
                Vector3 along = end - tip;
                var normal = new Vector3(-along.z, 0f, along.x);
                if (normal.z < 0f)
                {
                    normal = -normal;
                }
                float length = along.magnitude + 0.08f;
                rig.Body.Push((tip + end) * 0.5f, Quaternion.LookRotation(normal, Vector3.up) * Quaternion.Euler(12f, 0f, 0f));
                rig.B("PlateDark").BeveledBox(Vector3.zero, new Vector3(length, 0.58f, 0.07f), 0.02f);
                HazardPlate(rig, new Rect(-length * 0.5f + 0.02f, 0.08f, length - 0.04f, 0.16f), 0.036f);
                rig.B("Hazard").BeveledBox(new Vector3(0f, 0.3f, 0.03f), new Vector3(length, 0.04f, 0.16f), 0.01f);
                rig.B("Gunmetal").Box(new Vector3(0f, -0.3f, 0.03f), new Vector3(length, 0.06f, 0.1f));
                foreach (float x in new[] { -0.4f, 0.1f, 0.5f })
                {
                    rig.B("Gunmetal").Box(new Vector3(x * length * 0.7f, 0f, -0.07f), new Vector3(0.05f, 0.5f, 0.1f));
                }
                rig.Body.Pop();
                Bar(rig.B("Gunmetal"), new Vector3(side * 0.5f, 0.58f, 2.2f), new Vector3(side * 0.42f, 0.56f, 2.6f), 0.12f);
            }
            rig.B("Gunmetal").Cylinder(new Vector3(0f, 0.26f, 2.78f), 0.07f, 0.07f, 0.6f, 8);

            // Front lamps on brackets, back lamps, bumper.
            for (int side = -1; side <= 1; side += 2)
            {
                rig.B("Gunmetal").Rod(new Vector3(side * 0.74f, 1.08f, 2.2f), new Vector3(side * 0.88f, 1.08f, 2.2f), 0.03f, 6, false);
                Headlight(rig, new Vector3(side * 0.9f, 1.08f, 2.24f), new Vector2(0.11f, 0f), Vector3.forward);
                rig.Beams.Add(new Vector3(side * 0.9f, 1.08f, 2.3f));
                Taillight(rig, new Vector3(side * 0.95f, 0.9f, -2.77f), new Vector2(0.24f, 0.12f));
            }
            rig.B("Gunmetal").BeveledBox(new Vector3(0f, 0.62f, -2.85f), new Vector3(2.2f, 0.18f, 0.2f), 0.03f);
            Plate(rig, new Vector3(0f, 0.86f, -2.77f));
            Arch(rig, 2, 1.12f, 0.07f, "Black", 0.06f);
            Arch(rig, 3, 1.12f, 0.07f, "Black", 0.06f);
            rig.MineDrop = new Vector3(0f, 0.35f, -3.05f);
        }

        // ------------------------------------------------------------------ Apocalypse

        /// <summary>
        /// A jet-assisted interceptor on the Warden's lines: a long pointed nose with canards and two gun pods, the
        /// front wheels out under their own fenders, a fighter's bubble canopy and spine, and on the wide rear deck two
        /// jet nacelles with tall canted fins, missiles on pylons beside them and nozzles that glow (hotter with nitro).
        /// </summary>
        private static void Apocalypse(CarRig rig)
        {
            rig.FlameScale = 1.6f;
            Wheels(rig, 0.34f, 0.3f, 0.88f, 1.4f, 0.38f, 0.4f, 0.86f, -1.5f, "ChromeDark", false, 6);
            var body = new[]
            {
                Ring.Body(-2.48f, 0.3f, 0.5f, 0.72f, 1.0f, 0.12f, 0.08f, 0.1f),
                Ring.Body(-2.22f, 0.24f, 0.52f, 0.78f, 1.08f, 0.14f, 0.08f, 0.1f),
                Ring.Body(-0.78f, 0.22f, 0.52f, 0.78f, 1.08f, 0.14f, 0.08f, 0.1f),
                Ring.Body(0.1f, 0.21f, 0.48f, 0.7f, 0.8f, 0.12f, 0.07f, 0.08f),
                Ring.Body(1.0f, 0.24f, 0.44f, 0.62f, 0.58f, 0.1f, 0.06f, 0.07f),
                Ring.Body(1.9f, 0.28f, 0.4f, 0.52f, 0.46f, 0.08f, 0.05f, 0.06f),
                Ring.Body(2.42f, 0.32f, 0.38f, 0.45f, 0.28f, 0.04f, 0.04f, 0.05f),
                Ring.Body(2.6f, 0.36f, 0.38f, 0.41f, 0.1f, 0.015f, 0.02f, 0.02f)
            };
            Loft(body, (s, band) => band == Ring.Bottom || band == Ring.LowerLeft || band == Ring.LowerRight ? rig.BodyFace("Carbon") : rig.PaintFace(),
                rig.BodyFace("Carbon"), rig.PaintFace("PaintShade"));
            Stripe(rig.Body, "Carbon", body, -0.07f, 0.07f, 1.15f, 2.62f);

            // The canopy and the spine behind it.
            var canopy = new[]
            {
                Ring.Cabin(-0.55f, 0.75f, 0.79f, 0.42f, 0.4f, 0.02f),
                Ring.Cabin(-0.1f, 0.71f, 1.06f, 0.46f, 0.3f, 0.12f),
                Ring.Cabin(0.5f, 0.67f, 1.04f, 0.46f, 0.3f, 0.12f),
                Ring.Cabin(1.15f, 0.59f, 0.62f, 0.38f, 0.36f, 0.02f)
            };
            Loft(canopy, (s, band) => band == Ring.CabinBottom ? Face.None : s == 0 ? rig.PaintFace("PaintShade") : rig.BodyFace("Glass"),
                rig.PaintFace("PaintShade"), rig.BodyFace("Glass"));
            Shine(rig, canopy, 2, Ring.CabinTop, 0.22f, 0.12f);
            Shine(rig, canopy, 1, Ring.CabinTop, 0.3f, 0.1f);
            for (int band = Ring.CabinRight; band <= Ring.CabinLeft; band++)
            {
                Panel(rig.Body, "Carbon", canopy, 2, band, 0f, 1f, 0.02f, 0.08f, 0.008f);
            }
            rig.P().BeveledBox(new Vector3(0f, 0.86f, -1.25f), new Vector3(0.34f, 0.2f, 1.3f), 0.06f);
            rig.B("Black").Box(new Vector3(0f, 0.92f, -0.6f), new Vector3(0.22f, 0.1f, 0.03f));

            // The nose: two gun pods, canards ahead of the front wheels, slit headlights.
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 0.22f;
                rig.Body.Push(new Vector3(x, 0.6f, 1.62f), Quaternion.Euler(6f, 0f, 0f));
                rig.B("Carbon").BeveledBox(Vector3.zero, new Vector3(0.12f, 0.09f, 0.9f), 0.03f);
                rig.B("GunDark").Rod(new Vector3(0f, 0f, 0.4f), new Vector3(0f, 0f, 0.92f), 0.026f, 6, false);
                rig.B("GunDark").Rod(new Vector3(0f, 0f, 0.86f), new Vector3(0f, 0f, 0.96f), 0.036f, 8, false);
                rig.Body.Pop();
                rig.Body.Push(new Vector3(0f, 0.42f, 2.1f), Quaternion.Euler(90f, 0f, 0f));
                rig.B("Carbon").Prism(new[] { new Vector2(side * 0.24f, 0.22f), new Vector2(side * 0.74f, -0.06f), new Vector2(side * 0.74f, -0.18f), new Vector2(side * 0.24f, -0.12f) }, 0.035f);
                rig.Body.Pop();
                Headlight(rig, new Vector3(side * 0.24f, 0.4f, 2.36f), new Vector2(0.2f, 0.035f), new Vector3(side * 0.5f, 0.2f, 1f), "Xenon", "Carbon");
                rig.Beams.Add(new Vector3(side * 0.26f, 0.4f, 2.4f));
                // The front wheel's fender on a strut from the body.
                rig.P("PaintDark").BeveledBox(new Vector3(side * 0.61f, 0.48f, 1.4f), new Vector3(0.22f, 0.04f, 0.44f), 0.01f);
            }
            rig.Muzzle = new Vector3(0f, 0.5f, 2.59f);
            Fender(rig, 0, 0.32f, 0.04f, 30f, 155f, null, 6);
            Fender(rig, 1, 0.32f, 0.04f, 30f, 155f, null, 6);

            // The nacelles: painted, an intake with a cone at the front, the nozzle at the back glowing; fins on top,
            // missiles on pylons outside them.
            const float y = 0.98f;
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 0.5f;
                rig.P("PaintShade").Rod(new Vector3(x, y, -2.42f), new Vector3(x, y, -0.85f), 0.25f, 12, false);
                rig.P().Rod(new Vector3(x, y, -0.85f), new Vector3(x, y, -0.67f), 0.25f, 12, false, 0.23f);
                rig.B("Black").Rod(new Vector3(x, y, -0.69f), new Vector3(x, y, -0.65f), 0.21f, 12, false);
                rig.B("ChromeDark").Rod(new Vector3(x, y, -0.67f), new Vector3(x, y, -0.49f), 0.1f, 10, false, 0f);
                rig.B("Gunmetal").Rod(new Vector3(x, y, -2.62f), new Vector3(x, y, -2.4f), 0.22f, 12, false, 0.26f);
                rig.B("Black").Rod(new Vector3(x, y, -2.3f), new Vector3(x, y, -2.24f), 0.262f, 12, false);
                rig.G("Flame").Rod(new Vector3(x, y, -2.632f), new Vector3(x, y, -2.622f), 0.18f, 12, false);
                rig.G("FlameCore").Rod(new Vector3(x, y, -2.64f), new Vector3(x, y, -2.632f), 0.11f, 12, false);
                rig.G("JetHot").Rod(new Vector3(x, y, -2.626f), new Vector3(x, y, -2.62f), 0.205f, 12, false);
                rig.Exhausts.Add(new Vector3(x, y, -2.66f));
                rig.ExhaustDirections.Add(Vector3.back);

                rig.Paint.Push(new Vector3(x, y + 0.2f, -1.9f), Quaternion.Euler(0f, 0f, -side * 14f) * Quaternion.Euler(0f, -90f, 0f));
                rig.P().Prism(new[] { new Vector2(0.5f, 0f), new Vector2(-0.45f, 0f), new Vector2(-0.7f, 0.5f), new Vector2(-0.34f, 0.5f) }, 0.05f);
                rig.Paint.Pop();
                rig.Body.Push(new Vector3(x, y + 0.2f, -1.9f), Quaternion.Euler(0f, 0f, -side * 14f) * Quaternion.Euler(0f, -90f, 0f));
                rig.B("Carbon").Prism(new[] { new Vector2(-0.672f, 0.44f), new Vector2(-0.35f, 0.44f), new Vector2(-0.34f, 0.5f), new Vector2(-0.7f, 0.5f) }, 0.056f);
                rig.Body.Pop();

                float mx = side * 0.84f;
                rig.B("Gunmetal").Box(new Vector3(side * 0.78f, 0.9f, -0.95f), new Vector3(0.1f, 0.05f, 0.8f));
                rig.B("RocketBody").Rod(new Vector3(mx, 0.9f, -1.45f), new Vector3(mx, 0.9f, -0.5f), 0.07f, 8, false);
                rig.B("RocketNose").Rod(new Vector3(mx, 0.9f, -0.5f), new Vector3(mx, 0.9f, -0.3f), 0.07f, 8, false, 0f);
                rig.B("RocketBand").Box(new Vector3(mx, 0.9f, -1.36f), new Vector3(0.22f, 0.015f, 0.14f));
                rig.B("RocketBand").Box(new Vector3(mx, 0.9f, -1.36f), new Vector3(0.015f, 0.22f, 0.14f));
                rig.B("RocketBand").Box(new Vector3(mx, 0.9f, -0.62f), new Vector3(0.145f, 0.145f, 0.03f));

                Taillight(rig, new Vector3(side * 0.82f, 0.6f, -2.485f), new Vector2(0.28f, 0.05f), "Taillight", "Carbon");
            }
            rig.RocketMount = new Vector3(0.84f, 0.9f, -0.2f);

            // The tail: a carbon panel between the nozzles, diffuser fins.
            rig.B("Carbon").Box(new Vector3(0f, 0.62f, -2.49f), new Vector3(0.4f, 0.16f, 0.02f));
            for (int i = -2; i <= 2; i++)
            {
                rig.B("Carbon").Box(new Vector3(i * 0.3f, 0.25f, -2.42f), new Vector3(0.03f, 0.14f, 0.22f));
            }
            Arch(rig, 2, 1.08f, 0.05f, "Carbon");
            Arch(rig, 3, 1.08f, 0.05f, "Carbon");
            rig.MineDrop = new Vector3(0f, 0.3f, -2.85f);
        }
    }
}
