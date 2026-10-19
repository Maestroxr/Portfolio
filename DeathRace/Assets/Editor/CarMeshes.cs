using System;
using System.Collections.Generic;
using Gamebox.Editor;
using UnityEngine;

namespace Portfolio.DeathRace.EditorTools
{
    /// <summary>One face colour of a loft: the builder the face goes into (body, paint, lights) and its palette colour.</summary>
    internal readonly struct Face
    {
        public readonly LowPolyBuilder Target;
        public readonly Vector2 Uv;

        public Face(LowPolyBuilder target, string color)
        {
            Target = target;
            Uv = CarArt.UV(color);
        }

        /// <summary>No face (hidden inside another part).</summary>
        public static Face None => default;
    }

    /// <summary>
    /// A cross-section of a car part at one point along its length (x across, y up, at <see cref="Z"/>). Consecutive
    /// rings of a part are joined face to face by <see cref="CarMeshes.Loft"/>; every ring of a part has the same number
    /// of points, so its faces form bands that run along the car (the roof, the shoulders, the sides).
    /// </summary>
    internal sealed class Ring
    {
        public readonly float Z;
        public readonly Vector2[] Points;

        /// <summary>The band whose two points form the flat top of the ring (stripes are painted on it).</summary>
        public readonly int TopBand;

        public Ring(float z, int topBand, params Vector2[] points)
        {
            Z = z;
            TopBand = topBand;
            Points = points;
        }

        public int Count => Points.Length;

        public Vector3 Point(int index)
        {
            Vector2 p = Points[(index % Points.Length + Points.Length) % Points.Length];
            return new Vector3(p.x, p.y, Z);
        }

        public Vector3 Centroid
        {
            get
            {
                Vector2 sum = Vector2.zero;
                foreach (Vector2 p in Points)
                {
                    sum += p;
                }
                sum /= Points.Length;
                return new Vector3(sum.x, sum.y, Z);
            }
        }

        public float TopY => Points[TopBand].y;

        public float TopHalf => Mathf.Abs(Points[TopBand].x);

        // Bands of a body ring.
        public const int Bottom = 0;
        public const int LowerRight = 1;
        public const int UpperRight = 2;
        public const int ChamferRight = 3;
        public const int Top = 4;
        public const int ChamferLeft = 5;
        public const int UpperLeft = 6;
        public const int LowerLeft = 7;

        /// <summary>
        /// An eight-sided section of a car's lower body: the sill (<paramref name="tuck"/> in from the widest point), the
        /// beltline at its widest, the shoulder (<paramref name="tumble"/> in), a chamfer and the flat top.
        /// </summary>
        public static Ring Body(float z, float yBottom, float yBelt, float yTop, float half, float chamfer, float tuck = 0.07f, float tumble = 0.07f)
        {
            float sill = half - tuck;
            float shoulder = half - tumble;
            float top = shoulder - chamfer;
            return new Ring(z, Top,
                new Vector2(-sill, yBottom), new Vector2(sill, yBottom), new Vector2(half, yBelt),
                new Vector2(shoulder, yTop - chamfer), new Vector2(top, yTop), new Vector2(-top, yTop),
                new Vector2(-shoulder, yTop - chamfer), new Vector2(-half, yBelt));
        }

        // Bands of a cabin ring.
        public const int CabinBottom = 0;
        public const int CabinRight = 1;
        public const int CabinChamferRight = 2;
        public const int CabinTop = 3;
        public const int CabinChamferLeft = 4;
        public const int CabinLeft = 5;

        /// <summary>A six-sided section of a cabin: its base on the body, the sides (windows), the roof edges and the roof.</summary>
        public static Ring Cabin(float z, float yBottom, float yTop, float halfBottom, float halfTop, float chamfer)
        {
            float top = halfTop - chamfer;
            return new Ring(z, CabinTop,
                new Vector2(-halfBottom, yBottom), new Vector2(halfBottom, yBottom), new Vector2(halfTop, yTop - chamfer),
                new Vector2(top, yTop), new Vector2(-top, yTop), new Vector2(-halfTop, yTop - chamfer));
        }
    }

    /// <summary>
    /// A car model as built: the meshes of its parts (the palette body, the painted panels, the head and tail light
    /// lenses, the wheels) and where its wheels, gun muzzle, mine chute, rocket mount, exhausts and lights are. The car
    /// faces +z with its wheels on the ground (y = 0); the prefab builder turns it into a prefab with a CarModelView.
    /// </summary>
    internal sealed class CarRig
    {
        public readonly CarModel Model;
        public readonly LowPolyBuilder Body = new LowPolyBuilder();
        public readonly LowPolyBuilder Paint = new LowPolyBuilder();
        public readonly LowPolyBuilder Heads = new LowPolyBuilder();
        public readonly LowPolyBuilder Tails = new LowPolyBuilder();
        public LowPolyBuilder FrontWheel;
        public LowPolyBuilder RearWheel;
        public float FrontRadius;
        public float RearRadius;

        /// <summary>Wheel centres: front left, front right, rear left, rear right.</summary>
        public readonly Vector3[] Wheels = new Vector3[4];

        public Vector3 Muzzle;
        public Vector3 MineDrop;
        public Vector3 RocketMount;
        public readonly List<Vector3> Exhausts = new List<Vector3>();
        public readonly List<Vector3> ExhaustDirections = new List<Vector3>();

        /// <summary>The two main headlights the night beams start from (left, right).</summary>
        public readonly List<Vector3> Beams = new List<Vector3>();

        /// <summary>The rear lights the night glow sits behind.</summary>
        public readonly List<Vector3> TailSpots = new List<Vector3>();

        public CarRig(CarModel model)
        {
            Model = model;
        }

        public CarSpec Spec => CarSpecs.Of(Model);

        /// <summary>The body builder, colour set.</summary>
        public LowPolyBuilder B(string color)
        {
            return Body.Color(CarArt.UV(color));
        }

        /// <summary>The paint builder, colour set ("Paint", "PaintShade", "PaintDark", "PaintDeep").</summary>
        public LowPolyBuilder P(string color = "Paint")
        {
            return Paint.Color(CarArt.UV(color));
        }

        public Face BodyFace(string color)
        {
            return new Face(Body, color);
        }

        public Face PaintFace(string color = "Paint")
        {
            return new Face(Paint, color);
        }

        /// <summary>The usual colours of a lower body: dark underside, shaded lower sides, painted above.</summary>
        public Face StandardBody(int segment, int band)
        {
            switch (band)
            {
                case Ring.Bottom: return BodyFace("Charcoal");
                case Ring.LowerRight:
                case Ring.LowerLeft: return PaintFace("PaintShade");
                default: return PaintFace();
            }
        }
    }

    /// <summary>
    /// The parts every car is built from: lofted bodies and cabins, wheels, the machine gun, lights, mirrors, wheel
    /// arches, exhausts and bars. The six models themselves are in CarMeshes.Models.cs.
    /// </summary>
    internal static partial class CarMeshes
    {
        private static Vector2 UV(string color)
        {
            return CarArt.UV(color);
        }

        // ------------------------------------------------------------------ lofts

        /// <summary>
        /// Joins consecutive <paramref name="rings"/> (ordered from the back to the front) with flat faces; each face takes
        /// the colour and builder <paramref name="face"/> gives its segment (between ring s and s + 1) and band (between
        /// point j and j + 1). The two ends are closed with fans.
        /// </summary>
        public static void Loft(IList<Ring> rings, Func<int, int, Face> face, Face back, Face front)
        {
            int count = rings[0].Count;
            for (int s = 0; s + 1 < rings.Count; s++)
            {
                Ring a = rings[s];
                Ring b = rings[s + 1];
                Vector3 inside = (a.Centroid + b.Centroid) * 0.5f;
                for (int j = 0; j < count; j++)
                {
                    Face f = face(s, j);
                    if (f.Target == null)
                    {
                        continue;
                    }
                    f.Target.Color(f.Uv);
                    f.Target.Quad(a.Point(j), a.Point(j + 1), b.Point(j + 1), b.Point(j), inside);
                }
            }
            Cap(rings[0], back, Vector3.back);
            Cap(rings[rings.Count - 1], front, Vector3.forward);
        }

        private static void Cap(Ring ring, Face face, Vector3 outward)
        {
            if (face.Target == null)
            {
                return;
            }
            face.Target.Color(face.Uv);
            Vector3 center = ring.Centroid;
            for (int i = 0; i < ring.Count; i++)
            {
                face.Target.TriangleFacing(center, ring.Point(i), ring.Point(i + 1), outward);
            }
        }

        /// <summary>
        /// A stripe painted along the flat tops of a loft, from <paramref name="x0"/> to <paramref name="x1"/> across and
        /// <paramref name="fromZ"/> to <paramref name="toZ"/> along (cut to the top where the body narrows).
        /// </summary>
        public static void Stripe(LowPolyBuilder target, string color, IList<Ring> rings, float x0, float x1, float fromZ, float toZ, float lift = 0.008f)
        {
            target.Color(UV(color));
            for (int s = 0; s + 1 < rings.Count; s++)
            {
                Ring a = rings[s];
                Ring b = rings[s + 1];
                float lo = Mathf.Max(a.Z, fromZ);
                float hi = Mathf.Min(b.Z, toZ);
                if (hi - lo < 0.001f)
                {
                    continue;
                }
                float ta = (lo - a.Z) / (b.Z - a.Z);
                float tb = (hi - a.Z) / (b.Z - a.Z);
                float ya = Mathf.Lerp(a.TopY, b.TopY, ta) + lift;
                float yb = Mathf.Lerp(a.TopY, b.TopY, tb) + lift;
                float wa = Mathf.Lerp(a.TopHalf, b.TopHalf, ta);
                float wb = Mathf.Lerp(a.TopHalf, b.TopHalf, tb);
                float ax0 = Mathf.Clamp(x0, -wa, wa);
                float ax1 = Mathf.Clamp(x1, -wa, wa);
                float bx0 = Mathf.Clamp(x0, -wb, wb);
                float bx1 = Mathf.Clamp(x1, -wb, wb);
                if (ax1 - ax0 < 0.004f && bx1 - bx0 < 0.004f)
                {
                    continue;
                }
                target.QuadFacing(new Vector3(ax0, ya, lo), new Vector3(ax1, ya, lo), new Vector3(bx1, yb, hi), new Vector3(bx0, yb, hi), Vector3.up);
            }
        }

        /// <summary>
        /// A panel laid on one face of a loft (segment <paramref name="segment"/>, band <paramref name="band"/>):
        /// <paramref name="u0"/>..<paramref name="u1"/> across the band, <paramref name="v0"/>..<paramref name="v1"/>
        /// along the car, lifted off the face: slit windows, the shine on a windscreen, a primer patch.
        /// </summary>
        public static void Panel(LowPolyBuilder target, string color, IList<Ring> rings, int segment, int band,
            float u0, float u1, float v0, float v1, float lift = 0.01f)
        {
            Ring a = rings[segment];
            Ring b = rings[segment + 1];
            Vector3 a0 = a.Point(band);
            Vector3 a1 = a.Point(band + 1);
            Vector3 b0 = b.Point(band);
            Vector3 b1 = b.Point(band + 1);

            Vector3 At(float u, float v)
            {
                return Vector3.Lerp(Vector3.Lerp(a0, a1, u), Vector3.Lerp(b0, b1, u), v);
            }

            Vector3 p00 = At(u0, v0);
            Vector3 p10 = At(u1, v0);
            Vector3 p11 = At(u1, v1);
            Vector3 p01 = At(u0, v1);
            Vector3 normal = Vector3.Cross(p10 - p00, p01 - p00);
            if (normal.sqrMagnitude < 1e-10f)
            {
                normal = Vector3.Cross(p11 - p10, p01 - p10);
            }
            Vector3 center = (a.Centroid + b.Centroid) * 0.5f;
            if (Vector3.Dot(normal, (p00 + p11) * 0.5f - center) < 0f)
            {
                normal = -normal;
            }
            normal.Normalize();
            Vector3 offset = normal * lift;
            target.Color(UV(color));
            target.QuadFacing(p00 + offset, p10 + offset, p11 + offset, p01 + offset, normal);
        }

        /// <summary>A light diagonal band across a window: the shine that makes glass read as glass.</summary>
        public static void Shine(CarRig rig, IList<Ring> rings, int segment, int band, float from = 0.18f, float width = 0.16f)
        {
            Panel(rig.Body, "GlassShine", rings, segment, band, from, from + width, 0.12f, 0.88f, 0.006f);
            Panel(rig.Body, "GlassShine", rings, segment, band, from + width * 1.6f, from + width * 2.1f, 0.12f, 0.88f, 0.006f);
        }

        // ------------------------------------------------------------------ wheels

        /// <summary>
        /// A wheel around its centre, the axle along x: tyre with rounded shoulders (knobbly for off-road tyres), a rim
        /// with spokes and a hub on both faces, so one mesh serves both sides of the car.
        /// </summary>
        public static LowPolyBuilder Wheel(float radius, float width, string rimColor, bool knobbly, int spokes = 5)
        {
            var m = new LowPolyBuilder();
            float half = width * 0.5f;
            float bevel = Mathf.Min(radius * 0.16f, half * 0.45f);
            const int segments = 18;
            float rim = radius * 0.62f;
            m.Push(Vector3.zero, Quaternion.Euler(0f, 0f, -90f));
            m.Color(UV("Rubber")).Cylinder(new Vector3(0f, -half, 0f), radius - bevel, radius, bevel, segments, true, true, false);
            m.Color(UV("Tread")).Cylinder(new Vector3(0f, -half + bevel, 0f), radius, radius, width - 2f * bevel, segments, true, false, false);
            m.Color(UV("Rubber")).Cylinder(new Vector3(0f, half - bevel, 0f), radius, radius - bevel, bevel, segments, true, false, true);
            if (knobbly)
            {
                const int blocks = 16;
                m.Color(UV("Rubber"));
                for (int i = 0; i < blocks; i++)
                {
                    float angle = i * Mathf.PI * 2f / blocks;
                    var radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                    float across = (i % 2 == 0 ? -1f : 1f) * width * 0.2f;
                    m.Push(radial * (radius + 0.012f) + new Vector3(0f, across, 0f), Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f));
                    m.Box(Vector3.zero, new Vector3(0.05f, width * 0.5f, radius * 0.32f));
                    m.Pop();
                }
            }
            for (int side = -1; side <= 1; side += 2)
            {
                m.Push(new Vector3(0f, side * half, 0f), side > 0 ? Quaternion.identity : Quaternion.Euler(180f, 0f, 0f));
                m.Color(UV(rimColor)).Cylinder(new Vector3(0f, -0.03f, 0f), rim, rim, 0.035f, segments, false, false, true);
                m.Color(UV("RimDark")).Cylinder(new Vector3(0f, -0.03f, 0f), rim * 0.82f, rim * 0.82f, 0.04f, segments, false, false, true);
                m.Color(UV(rimColor));
                for (int k = 0; k < spokes; k++)
                {
                    m.Push(new Vector3(0f, 0.012f, 0f), Quaternion.Euler(0f, k * 360f / spokes, 0f));
                    m.Box(new Vector3(rim * 0.46f, 0f, 0f), new Vector3(rim * 0.74f, 0.014f, rim * 0.2f));
                    m.Pop();
                }
                m.Color(UV("Hub")).Cylinder(new Vector3(0f, 0f, 0f), rim * 0.3f, rim * 0.24f, 0.035f, 10, false, false, true);
                m.Pop();
            }
            m.Pop();
            return m;
        }

        /// <summary>Places the four wheels of <paramref name="rig"/> and builds their meshes.</summary>
        public static void Wheels(CarRig rig, float frontRadius, float frontWidth, float frontX, float frontZ,
            float rearRadius, float rearWidth, float rearX, float rearZ, string rim, bool knobbly, int spokes = 5)
        {
            rig.FrontRadius = frontRadius;
            rig.RearRadius = rearRadius;
            rig.FrontWheel = Wheel(frontRadius, frontWidth, rim, knobbly, spokes);
            rig.RearWheel = Mathf.Approximately(frontRadius, rearRadius) && Mathf.Approximately(frontWidth, rearWidth)
                ? rig.FrontWheel
                : Wheel(rearRadius, rearWidth, rim, knobbly, spokes);
            rig.Wheels[0] = new Vector3(-frontX, frontRadius, frontZ);
            rig.Wheels[1] = new Vector3(frontX, frontRadius, frontZ);
            rig.Wheels[2] = new Vector3(-rearX, rearRadius, rearZ);
            rig.Wheels[3] = new Vector3(rearX, rearRadius, rearZ);
        }

        /// <summary>A dark flare over each wheel on the body side (<paramref name="x"/> = the body's half width there).</summary>
        public static void Arches(CarRig rig, float x, float tube = 0.055f, string color = "Black", float extra = 0.06f)
        {
            for (int i = 0; i < 4; i++)
            {
                Vector3 wheel = rig.Wheels[i];
                float radius = (i < 2 ? rig.FrontRadius : rig.RearRadius) + extra;
                float side = Mathf.Sign(wheel.x);
                rig.Body.Push(new Vector3(side * x, wheel.y, wheel.z), Quaternion.Euler(0f, 0f, 90f));
                rig.B(color).Torus(Vector3.zero, radius, tube, 10, 5, 180f, false, -90f);
                rig.Body.Pop();
            }
        }

        // ------------------------------------------------------------------ weapons

        /// <summary>
        /// A machine gun on a pedestal, the base at <paramref name="at"/> (on the surface it stands on), pointing along +z:
        /// receiver, ammunition box, cooling jacket, barrel and muzzle brake; one or two barrels. Returns the muzzle point.
        /// </summary>
        public static Vector3 MachineGun(CarRig rig, Vector3 at, float scale, int barrels, bool pedestal = true)
        {
            LowPolyBuilder m = rig.Body;
            m.Push(at, Quaternion.identity, Vector3.one * scale);
            float axis = pedestal ? 0.24f : 0.11f;
            if (pedestal)
            {
                rig.B("Gunmetal").BeveledBox(new Vector3(0f, 0.025f, -0.05f), new Vector3(0.34f, 0.05f, 0.42f), 0.015f);
                rig.B("GunDark").Cylinder(new Vector3(0f, 0.05f, -0.05f), 0.065f, 0.055f, 0.1f, 8);
            }
            float receiverWidth = barrels > 1 ? 0.32f : 0.22f;
            rig.B("Gunmetal").BeveledBox(new Vector3(0f, axis, -0.08f), new Vector3(receiverWidth, 0.17f, 0.52f), 0.035f);
            rig.B("GunDark").Box(new Vector3(0f, axis + 0.095f, -0.1f), new Vector3(receiverWidth * 0.7f, 0.03f, 0.42f));
            rig.B("Olive").BeveledBox(new Vector3(receiverWidth * 0.5f + 0.08f, axis - 0.02f, -0.1f), new Vector3(0.15f, 0.19f, 0.3f), 0.02f);
            rig.B("OliveDark").Box(new Vector3(receiverWidth * 0.5f + 0.08f, axis + 0.08f, -0.1f), new Vector3(0.155f, 0.02f, 0.31f));
            rig.B("Brass").Box(new Vector3(receiverWidth * 0.5f + 0.005f, axis + 0.03f, -0.02f), new Vector3(0.03f, 0.05f, 0.12f));
            for (int side = -1; side <= 1; side += 2)
            {
                rig.B("Rubber").Rod(new Vector3(side * 0.06f, axis - 0.02f, -0.34f), new Vector3(side * 0.06f, axis - 0.1f, -0.45f), 0.02f, 6, false);
            }
            float spread = barrels > 1 ? 0.075f : 0f;
            for (int b = 0; b < barrels; b++)
            {
                float x = barrels > 1 ? (b == 0 ? -spread : spread) : 0f;
                rig.B("GunDark").Rod(new Vector3(x, axis, 0.15f), new Vector3(x, axis, 0.72f), 0.05f, 8, false);
                rig.B("Gunmetal").Rod(new Vector3(x, axis, 0.72f), new Vector3(x, axis, 1.0f), 0.026f, 6, false);
                rig.B("GunDark").Rod(new Vector3(x, axis, 0.98f), new Vector3(x, axis, 1.1f), 0.042f, 8, false);
                rig.B("Soot").Rod(new Vector3(x, axis, 1.1f), new Vector3(x, axis, 1.104f), 0.022f, 6, false);
            }
            m.Pop();
            return at + new Vector3(0f, axis, 1.12f) * scale;
        }

        /// <summary>A pod of rocket tubes (rows x columns) facing +z, its front centre at <paramref name="front"/>.</summary>
        public static void RocketPod(CarRig rig, Vector3 front, int rows, int columns, float tube = 0.065f, float length = 0.7f, string color = "OliveDark")
        {
            float pitch = tube * 2.4f;
            var size = new Vector3(columns * pitch + 0.04f, rows * pitch + 0.04f, length);
            rig.B(color).BeveledBox(front + new Vector3(0f, 0f, -length * 0.5f), size, 0.03f);
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    var center = new Vector3((c - (columns - 1) * 0.5f) * pitch, (r - (rows - 1) * 0.5f) * pitch, 0f);
                    rig.B("Gunmetal").Rod(front + center + Vector3.back * 0.05f, front + center + Vector3.forward * 0.04f, tube, 8, false);
                    rig.B("RocketNose").Rod(front + center + Vector3.forward * 0.04f, front + center + Vector3.forward * 0.05f, tube * 0.7f, 8, false);
                }
            }
        }

        // ------------------------------------------------------------------ lights and trim

        /// <summary>
        /// A headlight: a bezel on the body and a glowing lens (in the headlight renderer) facing <paramref name="facing"/>;
        /// round when <paramref name="size"/>.y is zero (then size.x is the radius).
        /// </summary>
        public static void Headlight(CarRig rig, Vector3 center, Vector2 size, Vector3 facing, string lens = "Headlight", string bezel = "Chrome")
        {
            Lamp(rig.Body, rig.Heads, center, size, facing, lens, bezel);
        }

        public static void Taillight(CarRig rig, Vector3 center, Vector2 size, string lens = "Taillight", string bezel = "Black")
        {
            Lamp(rig.Body, rig.Tails, center, size, Vector3.back, lens, bezel);
            rig.TailSpots.Add(center);
        }

        private static void Lamp(LowPolyBuilder body, LowPolyBuilder glass, Vector3 center, Vector2 size, Vector3 facing, string lens, string bezel)
        {
            Quaternion rotation = Quaternion.LookRotation(facing.normalized, Mathf.Abs(facing.normalized.y) > 0.95f ? Vector3.forward : Vector3.up);
            if (size.y <= 0f)
            {
                float radius = size.x;
                body.Push(center, rotation * Quaternion.Euler(90f, 0f, 0f));
                body.Color(UV(bezel)).Cylinder(new Vector3(0f, -0.05f, 0f), radius * 1.25f, radius * 1.25f, 0.06f, 12);
                body.Pop();
                glass.Push(center, rotation * Quaternion.Euler(90f, 0f, 0f));
                glass.Color(UV(lens)).Cylinder(new Vector3(0f, -0.02f, 0f), radius, radius * 0.9f, 0.04f, 12);
                glass.Pop();
                return;
            }
            body.Push(center, rotation);
            body.Color(UV(bezel)).BeveledBox(new Vector3(0f, 0f, -0.02f), new Vector3(size.x + 0.05f, size.y + 0.05f, 0.06f), 0.012f);
            body.Pop();
            glass.Push(center, rotation);
            glass.Color(UV(lens)).BeveledBox(new Vector3(0f, 0f, 0.012f), new Vector3(size.x, size.y, 0.04f), 0.008f);
            glass.Pop();
        }

        /// <summary>A side mirror on a stalk: <paramref name="side"/> -1 left, +1 right.</summary>
        public static void Mirror(CarRig rig, Vector3 root, int side, float scale = 1f)
        {
            Vector3 head = root + new Vector3(side * 0.12f * scale, 0.05f * scale, -0.02f);
            rig.B("Charcoal").Rod(root, head, 0.018f * scale, 5, false);
            rig.P("Paint").BeveledBox(head + new Vector3(side * 0.05f * scale, 0f, 0f), new Vector3(0.12f, 0.09f, 0.07f) * scale, 0.02f * scale);
            rig.B("GlassShine").Box(head + new Vector3(side * 0.05f * scale, 0f, -0.037f * scale), new Vector3(0.1f, 0.07f, 0.01f) * scale);
        }

        /// <summary>An exhaust pipe ending at <paramref name="tip"/>, blowing along <paramref name="direction"/>.</summary>
        public static void Exhaust(CarRig rig, Vector3 tip, Vector3 direction, float radius = 0.05f, float length = 0.35f, string pipe = "Exhaust", string tipColor = "ExhaustTip")
        {
            direction.Normalize();
            rig.B(pipe).Rod(tip - direction * length, tip - direction * 0.06f, radius, 8, false);
            rig.B(tipColor).Rod(tip - direction * 0.08f, tip, radius * 1.2f, 8, false);
            rig.B("Soot").Rod(tip - direction * 0.01f, tip + direction * 0.002f, radius * 0.8f, 8, false);
            rig.Exhausts.Add(tip);
            rig.ExhaustDirections.Add(direction);
        }

        /// <summary>A box between two points (a bar of a roll cage or a rack), square in section.</summary>
        public static void Bar(LowPolyBuilder target, Vector3 from, Vector3 to, float thickness)
        {
            target.Rod(from, to, thickness * 0.5f, 6, false);
        }

        /// <summary>A grille: a dark panel facing +z with <paramref name="slats"/> horizontal slats over it.</summary>
        public static void Grille(CarRig rig, Vector3 center, Vector2 size, int slats, string slat = "ChromeDark", string back = "Grille")
        {
            rig.B(back).Box(center, new Vector3(size.x, size.y, 0.04f));
            for (int i = 0; i < slats; i++)
            {
                float y = center.y - size.y * 0.5f + size.y * (i + 0.5f) / slats;
                rig.B(slat).Box(new Vector3(center.x, y, center.z + 0.025f), new Vector3(size.x * 0.96f, size.y / slats * 0.35f, 0.02f));
            }
        }

        /// <summary>A licence plate on the back.</summary>
        public static void Plate(CarRig rig, Vector3 center)
        {
            rig.B("License").Box(center, new Vector3(0.34f, 0.11f, 0.02f));
            rig.B("Charcoal").Box(center + new Vector3(0f, 0f, -0.012f), new Vector3(0.22f, 0.03f, 0.005f));
        }

        // ------------------------------------------------------------------ build

        public static CarRig Build(CarModel model)
        {
            var rig = new CarRig(model);
            switch (model)
            {
                case CarModel.Rustbucket: Rustbucket(rig); break;
                case CarModel.Hornet: Hornet(rig); break;
                case CarModel.Bulldog: Bulldog(rig); break;
                case CarModel.Viper: Viper(rig); break;
                case CarModel.Reaper: Reaper(rig); break;
                default: Tyrant(rig); break;
            }
            return rig;
        }
    }
}
