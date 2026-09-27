using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Asteroids.EditorTools
{
    /// <summary>The props the terrain tiles are dressed with (baked into one mesh per tile).</summary>
    internal enum PropKind
    {
        Rock, Boulders, Shrub, Cactus, Palm, Tree, Bush, Fern, Building, Hangar, Tent, Tank, Dome, Pipe, CraterRim,
        Antenna, Barrels, Sandbags, Fence, Container, Solar, Lamp, Car, Wall, Bridge, Ridge, Hedge, Vent, Tower, Mark,
        Crates, Spire, Tower2, Crystal, Stump
    }


    /// <summary>
    /// One prop on a tile: where it stands (tile metres), how it is turned (degrees about the view axis), its size in model
    /// space (x width, y height, z length), its colours and seed. The tile's baked shadow uses the same numbers.
    /// </summary>
    internal struct PropSpot
    {
        public PropKind Kind;
        public Vector2 Position;
        public float Yaw;
        public Vector3 Size;
        public GroundSwatch Main;
        public GroundSwatch Accent;
        public int Seed;
        /// <summary>Casts a baked shadow on the ground (flat markings do not).</summary>
        public bool Shadow;
        /// <summary>Its windows and roof lights glow (night themes).</summary>
        public bool Lit;
    }


    /// <summary>
    /// A ground unit's model in parts, built with +Y up and +Z toward its nose: the body, an optional turret and spinner
    /// (each built around its pivot) and the muzzles (in the turret's space when it has one).
    /// </summary>
    internal sealed class UnitModel
    {
        /// <summary>
        /// A model <paramref name="height"/> tall. The body is built into the view frame of the unit's "Visual" (nose up the
        /// screen, top toward the camera) with the ground at +height / 2, so the depth anchor at half the height centres it.
        /// </summary>
        public UnitModel(float height, float shadowRadius)
        {
            Height = height;
            ShadowRadius = shadowRadius;
            Body = new MeshBuilder();
            Body.Push(new Vector3(0f, 0f, height * 0.5f), FaceCamera);
        }

        /// <summary>Turns a model built with +Y up and +Z forward so it faces the camera with its nose up the screen.</summary>
        public static readonly Quaternion FaceCamera = Quaternion.Euler(-90f, 0f, 0f);

        public readonly MeshBuilder Body;
        public MeshBuilder Turret;
        public Vector3 TurretPivot;
        public MeshBuilder Spinner;
        public Vector3 SpinnerPivot;
        public readonly List<Vector3> Muzzles = new List<Vector3>();
        /// <summary>Where the attack warning glows (model space); null for none.</summary>
        public Vector3? Glow;
        /// <summary>Height of the model (m); the depth anchor sits at half of it.</summary>
        public readonly float Height;
        /// <summary>Radius of the soft shadow under it.</summary>
        public readonly float ShadowRadius;


        /// <summary>A part turning about its own pivot (a turret, a dish), built in the view frame around the pivot.</summary>
        public static MeshBuilder Part()
        {
            var builder = new MeshBuilder();
            builder.Push(Vector3.zero, FaceCamera);
            return builder;
        }


        /// <summary>Where a model-space point lands under the "Visual".</summary>
        public Vector3 ToVisual(Vector3 model)
        {
            return FaceCamera * model + new Vector3(0f, 0f, Height * 0.5f);
        }
    }


    /// <summary>The ground models of the strike mode: terrain props and the ground units, in the ground palette.</summary>
    internal static partial class SpaceModels
    {
        /// <summary>The colours of the ground units (the enemy's livery, readable over every theme's ground).</summary>
        private const GroundSwatch UnitHull = GroundSwatch.Tarp;
        private const GroundSwatch UnitDark = GroundSwatch.ArmyDark;
        private const GroundSwatch UnitMetal = GroundSwatch.SteelDark;
        private const GroundSwatch UnitMark = GroundSwatch.EnemyRed;

        /// <summary>Sets the face colour to a ground palette swatch (the ground palette has the same layout as the main one).</summary>
        public static MeshBuilder Tint(this MeshBuilder builder, GroundSwatch swatch)
        {
            return builder.Color((Swatch)(int)swatch);
        }

        // ------------------------------------------------------------------ tiles

        /// <summary>The flat ground of one tile: 64 x 20 m in the XY plane facing the camera (-Z), UV 0..1.</summary>
        public static Mesh GroundQuad()
        {
            float x = StrikeRules.TileWidth * 0.5f;
            float y = StrikeRules.TileLength * 0.5f;
            var mesh = new Mesh { name = "GroundTile" };
            mesh.vertices = new[] { new Vector3(-x, -y, 0f), new Vector3(x, -y, 0f), new Vector3(x, y, 0f), new Vector3(-x, y, 0f) };
            mesh.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
            mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateBounds();
            return mesh;
        }


        /// <summary>Adds <paramref name="spot"/> to a tile's prop mesh (tile frame: x right, y up the screen, -z up from the ground).</summary>
        public static void Prop(MeshBuilder b, PropSpot spot)
        {
            b.Push(new Vector3(spot.Position.x, spot.Position.y, 0f), Quaternion.Euler(0f, 0f, spot.Yaw) * Quaternion.Euler(-90f, 0f, 0f));
            var random = new System.Random(spot.Seed);
            Vector3 s = spot.Size;
            switch (spot.Kind)
            {
                case PropKind.Rock: Rock(b, s, spot.Main, spot.Seed); break;
                case PropKind.Boulders: Boulders(b, s, spot.Main, spot.Accent, random); break;
                case PropKind.Shrub: Shrub(b, s, spot.Main, random); break;
                case PropKind.Cactus: Cactus(b, s, random); break;
                case PropKind.Palm: Palm(b, s, random); break;
                case PropKind.Tree: Tree(b, s, spot.Main, spot.Accent, random); break;
                case PropKind.Bush: b.Tint(spot.Main).Blob(new Vector3(0f, s.y * 0.5f, 0f), s * 0.5f, 1, 0.22f, spot.Seed); break;
                case PropKind.Fern: Fern(b, s, spot.Main, random); break;
                case PropKind.Building: Building(b, s, spot.Main, spot.Accent, random, spot.Lit); break;
                case PropKind.Hangar: Hangar(b, s, spot.Main, spot.Accent); break;
                case PropKind.Tent: Tent(b, s, spot.Main); break;
                case PropKind.Tank: StorageTank(b, s, spot.Main, spot.Accent); break;
                case PropKind.Dome: Dome(b, s, spot.Main, spot.Accent); break;
                case PropKind.Pipe: PipeRun(b, s, spot.Main, spot.Accent); break;
                case PropKind.CraterRim: CraterRim(b, s, spot.Main, spot.Seed); break;
                case PropKind.Antenna: Antenna(b, s, spot.Main, spot.Accent); break;
                case PropKind.Barrels: Barrels(b, s, spot.Main, spot.Accent, random); break;
                case PropKind.Sandbags: Sandbags(b, s, spot.Main, random); break;
                case PropKind.Fence: Fence(b, s, spot.Main); break;
                case PropKind.Container: Container(b, s, spot.Main, spot.Accent); break;
                case PropKind.Solar: Solar(b, s, spot.Main, spot.Accent); break;
                case PropKind.Lamp: Lamp(b, s, spot.Main, spot.Accent); break;
                case PropKind.Car: Car(b, s, spot.Main); break;
                case PropKind.Wall: b.Tint(spot.Main).BeveledBox(new Vector3(0f, s.y * 0.5f, 0f), s, Mathf.Min(0.05f, s.x * 0.2f)); break;
                case PropKind.Bridge: Bridge(b, s, spot.Main, spot.Accent); break;
                case PropKind.Ridge: Ridge(b, s, spot.Main, spot.Accent, random); break;
                case PropKind.Hedge: Hedge(b, s, spot.Main, random); break;
                case PropKind.Vent: Vent(b, s, spot.Main, spot.Accent); break;
                case PropKind.Tower: Tower(b, s, spot.Main, spot.Accent); break;
                case PropKind.Mark: b.Tint(spot.Main).Box(new Vector3(0f, s.y * 0.5f, 0f), s); break;
                case PropKind.Crates: Crates(b, s, spot.Main, spot.Accent, random); break;
                case PropKind.Spire: Spire(b, s, spot.Main, spot.Accent, random); break;
                case PropKind.Tower2: CoolingTower(b, s, spot.Main, spot.Accent); break;
                case PropKind.Crystal: Crystal(b, s, spot.Main, spot.Accent, random); break;
                case PropKind.Stump: b.Tint(spot.Main).Cylinder(Vector3.zero, s.x * 0.5f, s.x * 0.42f, s.y, 7); break;
            }
            b.Pop();
        }


        private static float Range(System.Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }

        // ------------------------------------------------------------------ nature

        private static void Rock(MeshBuilder b, Vector3 s, GroundSwatch main, int seed)
        {
            b.Tint(main).Blob(new Vector3(0f, s.y * 0.35f, 0f), new Vector3(s.x * 0.5f, s.y * 0.65f, s.z * 0.5f), 1, 0.28f, seed);
        }


        private static void Boulders(MeshBuilder b, Vector3 s, GroundSwatch main, GroundSwatch accent, System.Random random)
        {
            int count = 3 + random.Next(3);
            for (int i = 0; i < count; i++)
            {
                float r = Range(random, 0.25f, 0.5f);
                var at = new Vector3(Range(random, -0.5f, 0.5f) * s.x * (1f - r), 0f, Range(random, -0.5f, 0.5f) * s.z * (1f - r));
                float size = r * Mathf.Min(s.x, s.z);
                b.Tint(i % 3 == 2 ? accent : main).Blob(at + Vector3.up * size * 0.3f, new Vector3(size, Mathf.Min(s.y, size) * 0.8f, size * Range(random, 0.8f, 1.2f)),
                    i == 0 ? 1 : 0, 0.3f, random.Next());
            }
        }


        private static void Shrub(MeshBuilder b, Vector3 s, GroundSwatch main, System.Random random)
        {
            int count = 4 + random.Next(3);
            for (int i = 0; i < count; i++)
            {
                float angle = i * Mathf.PI * 2f / count + Range(random, -0.3f, 0.3f);
                float d = Range(random, 0.1f, 0.3f);
                var at = new Vector3(Mathf.Cos(angle) * d * s.x, s.y * 0.35f, Mathf.Sin(angle) * d * s.z);
                float r = Range(random, 0.22f, 0.32f);
                b.Tint(i % 2 == 0 ? main : GroundSwatch.OliveDark).Blob(at, new Vector3(r * s.x, s.y * 0.5f, r * s.z), 0, 0.25f, random.Next());
            }
        }


        private static void Cactus(MeshBuilder b, Vector3 s, System.Random random)
        {
            float r = s.x * 0.12f;
            b.Tint(GroundSwatch.Cactus).Cylinder(Vector3.zero, r, r * 0.9f, s.y * 0.92f, 7, true);
            b.Sphere(new Vector3(0f, s.y * 0.92f, 0f), r * 0.9f, 4, 7);
            for (int side = -1; side <= 1; side += 2)
            {
                if (random.NextDouble() < 0.25)
                {
                    continue;
                }
                float h = Range(random, 0.35f, 0.55f) * s.y;
                float reach = s.x * Range(random, 0.28f, 0.4f);
                var elbow = new Vector3(side * reach, h, 0f);
                b.Rod(new Vector3(0f, h - 0.05f, 0f), elbow, r * 0.7f, 6);
                b.Rod(elbow, elbow + Vector3.up * s.y * Range(random, 0.2f, 0.35f), r * 0.7f, 6);
                b.Sphere(elbow + Vector3.up * s.y * 0.3f, r * 0.7f, 3, 6);
            }
        }


        private static void Palm(MeshBuilder b, Vector3 s, System.Random random)
        {
            float lean = Range(random, 0.1f, 0.3f) * s.x;
            var top = new Vector3(lean, s.y * 0.82f, 0f);
            b.Tint(GroundSwatch.Trunk).Rod(Vector3.zero, top, s.x * 0.05f, 6, true, s.x * 0.035f);
            int fronds = 7;
            for (int i = 0; i < fronds; i++)
            {
                float angle = (i + Range(random, -0.2f, 0.2f)) * Mathf.PI * 2f / fronds;
                var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                var side = new Vector3(-direction.z, 0f, direction.x);
                float length = s.x * Range(random, 0.42f, 0.52f);
                Vector3 tip = top + direction * length + Vector3.down * s.y * 0.18f;
                Vector3 middle = top + direction * length * 0.5f + Vector3.up * s.y * 0.05f;
                float width = s.x * 0.09f;
                b.Tint(i % 2 == 0 ? GroundSwatch.Palm : GroundSwatch.Leaf);
                b.TriangleFacing(top, middle + side * width, middle - side * width, Vector3.up);
                b.TriangleFacing(middle + side * width, tip, middle - side * width, Vector3.up);
            }
            b.Tint(GroundSwatch.Bark).Sphere(top + Vector3.down * 0.03f, s.x * 0.06f, 3, 6);
        }


        private static void Tree(MeshBuilder b, Vector3 s, GroundSwatch main, GroundSwatch accent, System.Random random)
        {
            b.Tint(GroundSwatch.Trunk).Cylinder(Vector3.zero, s.x * 0.06f, s.x * 0.04f, s.y * 0.5f, 5);
            int blobs = 3 + random.Next(3);
            for (int i = 0; i < blobs; i++)
            {
                float angle = i * Mathf.PI * 2f / blobs + Range(random, -0.4f, 0.4f);
                float d = i == 0 ? 0f : Range(random, 0.15f, 0.28f);
                float r = i == 0 ? 0.36f : Range(random, 0.22f, 0.3f);
                var at = new Vector3(Mathf.Cos(angle) * d * s.x, s.y * Range(random, 0.6f, 0.72f), Mathf.Sin(angle) * d * s.z);
                b.Tint(i % 3 == 1 ? accent : main).Blob(at, new Vector3(r * s.x, s.y * 0.3f, r * s.z), 1, 0.2f, random.Next());
            }
        }


        private static void Fern(MeshBuilder b, Vector3 s, GroundSwatch main, System.Random random)
        {
            int leaves = 6;
            var center = new Vector3(0f, s.y * 0.3f, 0f);
            for (int i = 0; i < leaves; i++)
            {
                float angle = (i + Range(random, -0.25f, 0.25f)) * Mathf.PI * 2f / leaves;
                var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                var side = new Vector3(-direction.z, 0f, direction.x);
                Vector3 tip = center + direction * s.x * 0.5f + Vector3.up * s.y * 0.5f;
                Vector3 middle = center + direction * s.x * 0.25f + Vector3.up * s.y * 0.6f;
                b.Tint(i % 2 == 0 ? main : GroundSwatch.LeafLight);
                b.TriangleFacing(center, middle + side * s.x * 0.08f, middle - side * s.x * 0.08f, Vector3.up);
                b.TriangleFacing(middle + side * s.x * 0.08f, tip, middle - side * s.x * 0.08f, Vector3.up);
            }
        }


        private static void Ridge(MeshBuilder b, Vector3 s, GroundSwatch main, GroundSwatch accent, System.Random random)
        {
            int count = 4 + random.Next(3);
            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 0f : i / (count - 1f) - 0.5f;
                float r = Range(random, 0.22f, 0.32f) * s.x;
                var at = new Vector3(t * (s.x - r), 0f, Range(random, -0.15f, 0.15f) * s.z);
                float h = s.y * Range(random, 0.5f, 1f) * (1f - Mathf.Abs(t) * 0.8f);
                b.Tint(i % 3 == 1 ? accent : main).Blob(at + Vector3.up * h * 0.3f, new Vector3(r, h * 0.7f, s.z * Range(random, 0.35f, 0.5f)), 1, 0.3f, random.Next());
            }
        }


        private static void Hedge(MeshBuilder b, Vector3 s, GroundSwatch main, System.Random random)
        {
            int count = Mathf.Max(2, Mathf.RoundToInt(s.x / 0.7f));
            for (int i = 0; i < count; i++)
            {
                float t = (i + 0.5f) / count - 0.5f;
                b.Tint(i % 2 == 0 ? main : GroundSwatch.LeafDark).Blob(new Vector3(t * s.x, s.y * 0.45f, 0f),
                    new Vector3(s.x / count * 0.75f, s.y * 0.5f, s.z * 0.5f), 0, 0.2f, random.Next());
            }
        }


        private static void CraterRim(MeshBuilder b, Vector3 s, GroundSwatch main, int seed)
        {
            float major = s.x * 0.42f;
            b.Push(Vector3.zero, Quaternion.identity, new Vector3(1f, s.y / (s.x * 0.08f), s.z / s.x));
            b.Tint(main).Torus(Vector3.zero, major, s.x * 0.08f, 16, 5, 360f, false, seed % 90);
            b.Pop();
        }


        private static void Crystal(MeshBuilder b, Vector3 s, GroundSwatch main, GroundSwatch accent, System.Random random)
        {
            int count = 3 + random.Next(3);
            for (int i = 0; i < count; i++)
            {
                float angle = Range(random, 0f, Mathf.PI * 2f);
                var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                var foot = direction * Range(random, 0f, 0.25f) * s.x;
                Vector3 tip = foot + direction * s.x * Range(random, 0.1f, 0.3f) + Vector3.up * s.y * Range(random, 0.6f, 1f);
                b.Tint(i % 2 == 0 ? main : accent).Rod(foot, tip, s.x * Range(random, 0.08f, 0.13f), 5, false, 0.005f);
            }
        }

        // ------------------------------------------------------------------ buildings

        private static void Building(MeshBuilder b, Vector3 s, GroundSwatch walls, GroundSwatch roof, System.Random random, bool lit)
        {
            b.Tint(walls).Box(new Vector3(0f, s.y * 0.45f, 0f), new Vector3(s.x, s.y * 0.9f, s.z));
            b.Tint(roof).Box(new Vector3(0f, s.y * 0.92f, 0f), new Vector3(s.x - 0.12f, s.y * 0.06f, s.z - 0.12f));
            b.Tint(walls).Box(new Vector3(0f, s.y * 0.93f, s.z * 0.5f - 0.05f), new Vector3(s.x, s.y * 0.1f, 0.1f));
            b.Box(new Vector3(0f, s.y * 0.93f, -s.z * 0.5f + 0.05f), new Vector3(s.x, s.y * 0.1f, 0.1f));
            b.Box(new Vector3(s.x * 0.5f - 0.05f, s.y * 0.93f, 0f), new Vector3(0.1f, s.y * 0.1f, s.z));
            b.Box(new Vector3(-s.x * 0.5f + 0.05f, s.y * 0.93f, 0f), new Vector3(0.1f, s.y * 0.1f, s.z));
            int units = 1 + random.Next(3);
            for (int i = 0; i < units; i++)
            {
                var at = new Vector3(Range(random, -0.3f, 0.3f) * s.x, s.y * 0.95f, Range(random, -0.3f, 0.3f) * s.z);
                float size = Mathf.Min(s.x, s.z) * Range(random, 0.12f, 0.2f);
                b.Tint(i == 0 ? GroundSwatch.Metal : GroundSwatch.ConcreteDark).Box(at + Vector3.up * size * 0.3f, new Vector3(size, size * 0.6f, size * 1.3f));
            }
            if (lit)
            {
                b.Tint(GroundSwatch.Window);
                for (int side = -1; side <= 1; side += 2)
                {
                    for (float row = 0.3f; row < 0.85f; row += 0.25f)
                    {
                        b.Box(new Vector3(0f, s.y * row, side * (s.z * 0.5f + 0.01f)), new Vector3(s.x * 0.8f, s.y * 0.08f, 0.02f));
                        b.Box(new Vector3(side * (s.x * 0.5f + 0.01f), s.y * row, 0f), new Vector3(0.02f, s.y * 0.08f, s.z * 0.8f));
                    }
                }
                if (random.NextDouble() < 0.6)
                {
                    b.Tint(GroundSwatch.Lamp).Box(new Vector3(Range(random, -0.3f, 0.3f) * s.x, s.y * 0.96f, Range(random, -0.3f, 0.3f) * s.z), new Vector3(0.25f, 0.03f, 0.25f));
                }
            }
        }


        private static void Hangar(MeshBuilder b, Vector3 s, GroundSwatch main, GroundSwatch accent)
        {
            b.Push(new Vector3(0f, 0f, -s.z * 0.5f), Quaternion.Euler(90f, 0f, 0f), new Vector3(s.x * 0.5f, s.z, s.y));
            b.Tint(main).Cylinder(Vector3.zero, 1f, 1f, 1f, 14, false, true, true, 0f);
            b.Pop();
            b.Tint(accent).Box(new Vector3(0f, s.y * 0.35f, s.z * 0.5f + 0.02f), new Vector3(s.x * 0.6f, s.y * 0.7f, 0.05f));
            b.Tint(GroundSwatch.ConcreteDark).Box(new Vector3(0f, 0.01f, 0f), new Vector3(s.x + 0.1f, 0.02f, s.z + 0.1f));
        }


        private static void Tent(MeshBuilder b, Vector3 s, GroundSwatch main)
        {
            var a = new Vector3(-s.x * 0.5f, 0f, -s.z * 0.5f);
            var c = new Vector3(s.x * 0.5f, 0f, -s.z * 0.5f);
            var d = new Vector3(s.x * 0.5f, 0f, s.z * 0.5f);
            var e = new Vector3(-s.x * 0.5f, 0f, s.z * 0.5f);
            var r1 = new Vector3(0f, s.y, -s.z * 0.5f);
            var r2 = new Vector3(0f, s.y, s.z * 0.5f);
            var inside = new Vector3(0f, s.y * 0.3f, 0f);
            b.Tint(main).Quad(a, e, r2, r1, inside);
            b.Tint(GroundSwatch.OliveDark).Quad(c, d, r2, r1, inside);
            b.Tint(main).Triangle(a, c, r1, inside);
            b.Triangle(e, d, r2, inside);
        }


        private static void StorageTank(MeshBuilder b, Vector3 s, GroundSwatch main, GroundSwatch accent)
        {
            float r = s.x * 0.5f;
            b.Tint(main).Cylinder(Vector3.zero, r, r, s.y * 0.85f, 16, true);
            b.Tint(accent).Cylinder(new Vector3(0f, s.y * 0.55f, 0f), r * 1.01f, r * 1.01f, s.y * 0.12f, 16, true, false, false);
            b.Tint(main).Sphere(new Vector3(0f, s.y * 0.85f, 0f), new Vector3(r, s.y * 0.15f, r), 4, 16, true, 0.5f);
            b.Tint(GroundSwatch.PipeDark).Box(new Vector3(r * 0.6f, s.y * 0.96f, 0f), new Vector3(r * 0.25f, 0.06f, r * 0.25f));
        }


        private static void Dome(MeshBuilder b, Vector3 s, GroundSwatch main, GroundSwatch accent)
        {
            b.Tint(accent).Cylinder(Vector3.zero, s.x * 0.52f, s.x * 0.5f, s.y * 0.12f, 18);
            b.Tint(main).Sphere(new Vector3(0f, s.y * 0.12f, 0f), new Vector3(s.x * 0.47f, s.y * 0.88f, s.z * 0.47f), 6, 18, false, 0.5f);
            b.Tint(GroundSwatch.Window).Box(new Vector3(0f, s.y * 0.25f, s.z * 0.46f), new Vector3(s.x * 0.2f, s.y * 0.16f, 0.06f));
        }


        private static void PipeRun(MeshBuilder b, Vector3 s, GroundSwatch main, GroundSwatch accent)
        {
            float r = Mathf.Min(0.18f, s.y * 0.35f);
            for (int i = -1; i <= 1; i += 2)
            {
                float x = i * r * 1.3f;
                b.Tint(i < 0 ? main : accent).Rod(new Vector3(x, s.y - r, -s.z * 0.5f), new Vector3(x, s.y - r, s.z * 0.5f), r, 8);
            }
            int supports = Mathf.Max(2, Mathf.RoundToInt(s.z / 2.5f));
            for (int i = 0; i < supports; i++)
            {
                float z = ((i + 0.5f) / supports - 0.5f) * s.z;
                b.Tint(GroundSwatch.PipeDark).Box(new Vector3(0f, (s.y - r) * 0.5f, z), new Vector3(r * 4f, s.y - r, 0.12f));
            }
        }


        private static void Antenna(MeshBuilder b, Vector3 s, GroundSwatch main, GroundSwatch accent)
        {
            b.Tint(GroundSwatch.ConcreteDark).Box(new Vector3(0f, 0.05f, 0f), new Vector3(s.x * 0.5f, 0.1f, s.z * 0.5f));
            b.Tint(main).Rod(Vector3.zero, new Vector3(0f, s.y, 0f), 0.05f, 5);
            for (int i = 0; i < 3; i++)
            {
                float angle = i * Mathf.PI * 2f / 3f;
                var foot = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * s.x * 0.45f;
                b.Rod(foot, new Vector3(0f, s.y * 0.7f, 0f), 0.02f, 3);
            }
            b.Tint(accent).Sphere(new Vector3(0f, s.y, 0f), 0.08f, 3, 6);
        }


        private static void Barrels(MeshBuilder b, Vector3 s, GroundSwatch main, GroundSwatch accent, System.Random random)
        {
            int count = 3 + random.Next(4);
            float r = 0.2f;
            for (int i = 0; i < count; i++)
            {
                var at = new Vector3((i % 3 - 1) * r * 2.1f, 0f, (i / 3 - 0.5f) * r * 2.1f);
                b.Tint(random.NextDouble() < 0.3 ? accent : main).Cylinder(at, r, r, Mathf.Min(s.y, 0.45f), 8, true);
            }
        }


        private static void Sandbags(MeshBuilder b, Vector3 s, GroundSwatch main, System.Random random)
        {
            int count = Mathf.Max(2, Mathf.RoundToInt(s.x / 0.5f));
            for (int row = 0; row < 2; row++)
            {
                for (int i = 0; i < count - row; i++)
                {
                    float t = (i + 0.5f + row * 0.5f) / count - 0.5f;
                    b.Tint(row == 0 ? main : GroundSwatch.SandDark).Blob(new Vector3(t * s.x, s.y * (0.22f + row * 0.4f), 0f),
                        new Vector3(s.x / count * 0.55f, s.y * 0.22f, s.z * 0.5f), 0, 0.15f, random.Next());
                }
            }
        }


        private static void Fence(MeshBuilder b, Vector3 s, GroundSwatch main)
        {
            int posts = Mathf.Max(2, Mathf.RoundToInt(s.x / 1.5f) + 1);
            b.Tint(main);
            for (int i = 0; i < posts; i++)
            {
                float x = (i / (posts - 1f) - 0.5f) * s.x;
                b.Box(new Vector3(x, s.y * 0.5f, 0f), new Vector3(0.07f, s.y, 0.07f));
            }
            b.Box(new Vector3(0f, s.y * 0.85f, 0f), new Vector3(s.x, 0.04f, 0.04f));
            b.Box(new Vector3(0f, s.y * 0.45f, 0f), new Vector3(s.x, 0.04f, 0.04f));
        }


        private static void Container(MeshBuilder b, Vector3 s, GroundSwatch main, GroundSwatch accent)
        {
            b.Tint(main).Box(new Vector3(0f, s.y * 0.5f, 0f), s);
            b.Tint(accent);
            int ribs = Mathf.Max(2, Mathf.RoundToInt(s.z / 0.6f));
            for (int i = 0; i < ribs; i++)
            {
                float z = ((i + 0.5f) / ribs - 0.5f) * s.z;
                b.Box(new Vector3(0f, s.y + 0.005f, z), new Vector3(s.x * 0.96f, 0.02f, 0.06f));
            }
        }


        private static void Solar(MeshBuilder b, Vector3 s, GroundSwatch main, GroundSwatch accent)
        {
            b.Tint(accent).Box(new Vector3(0f, s.y * 0.3f, 0f), new Vector3(0.08f, s.y * 0.6f, 0.08f));
            b.Push(new Vector3(0f, s.y * 0.75f, 0f), Quaternion.Euler(-20f, 0f, 0f));
            b.Tint(main).Box(Vector3.zero, new Vector3(s.x, 0.04f, s.z));
            b.Tint(GroundSwatch.Metal).Box(new Vector3(0f, 0.025f, 0f), new Vector3(s.x, 0.02f, 0.04f));
            b.Box(new Vector3(0f, 0.025f, 0f), new Vector3(0.04f, 0.02f, s.z));
            b.Pop();
        }


        private static void Lamp(MeshBuilder b, Vector3 s, GroundSwatch main, GroundSwatch accent)
        {
            b.Tint(main).Rod(Vector3.zero, new Vector3(0f, s.y, 0f), 0.04f, 5);
            b.Rod(new Vector3(0f, s.y, 0f), new Vector3(0f, s.y, s.z * 0.5f), 0.03f, 4);
            b.Tint(accent).Box(new Vector3(0f, s.y - 0.02f, s.z * 0.5f), new Vector3(0.14f, 0.06f, 0.22f));
        }


        private static void Car(MeshBuilder b, Vector3 s, GroundSwatch main)
        {
            b.Tint(GroundSwatch.Rubber).Box(new Vector3(0f, s.y * 0.2f, 0f), new Vector3(s.x * 1.02f, s.y * 0.3f, s.z * 0.8f));
            b.Tint(main).BeveledBox(new Vector3(0f, s.y * 0.4f, 0f), new Vector3(s.x, s.y * 0.4f, s.z), 0.06f);
            b.Tint(GroundSwatch.Glass).BeveledBox(new Vector3(0f, s.y * 0.72f, -s.z * 0.05f), new Vector3(s.x * 0.8f, s.y * 0.3f, s.z * 0.5f), 0.05f);
            b.Tint(main).Box(new Vector3(0f, s.y * 0.88f, -s.z * 0.05f), new Vector3(s.x * 0.7f, s.y * 0.04f, s.z * 0.38f));
        }


        private static void Bridge(MeshBuilder b, Vector3 s, GroundSwatch main, GroundSwatch accent)
        {
            b.Tint(GroundSwatch.Asphalt).Box(new Vector3(0f, s.y - 0.06f, 0f), new Vector3(s.x - 0.4f, 0.12f, s.z));
            b.Tint(main).Box(new Vector3(0f, s.y - 0.2f, 0f), new Vector3(s.x, 0.16f, s.z));
            for (int side = -1; side <= 1; side += 2)
            {
                b.Tint(accent).Box(new Vector3(side * (s.x * 0.5f - 0.12f), s.y + 0.08f, 0f), new Vector3(0.14f, 0.2f, s.z));
            }
            b.Tint(GroundSwatch.White);
            for (float z = -s.z * 0.5f + 0.6f; z < s.z * 0.5f - 0.5f; z += 2f)
            {
                b.Box(new Vector3(0f, s.y + 0.005f, z + 0.5f), new Vector3(0.14f, 0.01f, 1f));
            }
            b.Tint(main);
            for (int i = -1; i <= 1; i += 2)
            {
                b.Box(new Vector3(0f, (s.y - 0.28f) * 0.5f, i * s.z * 0.25f), new Vector3(s.x * 0.5f, s.y - 0.28f, 0.5f));
            }
        }


        private static void Vent(MeshBuilder b, Vector3 s, GroundSwatch main, GroundSwatch accent)
        {
            b.Tint(main).BeveledBox(new Vector3(0f, s.y * 0.4f, 0f), new Vector3(s.x, s.y * 0.8f, s.z), 0.06f);
            b.Tint(GroundSwatch.Black).Cylinder(new Vector3(0f, s.y * 0.8f, 0f), Mathf.Min(s.x, s.z) * 0.35f, Mathf.Min(s.x, s.z) * 0.35f, 0.02f, 12);
            b.Tint(accent);
            for (int i = 0; i < 4; i++)
            {
                float angle = i * 45f;
                b.Push(new Vector3(0f, s.y * 0.83f, 0f), Quaternion.Euler(0f, angle, 0f));
                b.Box(Vector3.zero, new Vector3(Mathf.Min(s.x, s.z) * 0.68f, 0.03f, 0.05f));
                b.Pop();
            }
        }


        private static void Tower(MeshBuilder b, Vector3 s, GroundSwatch main, GroundSwatch accent)
        {
            b.Tint(main).BeveledBox(new Vector3(0f, s.y * 0.35f, 0f), new Vector3(s.x, s.y * 0.7f, s.z), 0.05f);
            b.Tint(GroundSwatch.Glass).BeveledBox(new Vector3(0f, s.y * 0.82f, 0f), new Vector3(s.x * 1.15f, s.y * 0.24f, s.z * 1.15f), 0.04f);
            b.Tint(accent).Box(new Vector3(0f, s.y * 0.97f, 0f), new Vector3(s.x * 1.2f, s.y * 0.06f, s.z * 1.2f));
            b.Tint(GroundSwatch.GlowRed).Sphere(new Vector3(0f, s.y, 0f), 0.06f, 3, 6);
        }


        private static void Crates(MeshBuilder b, Vector3 s, GroundSwatch main, GroundSwatch accent, System.Random random)
        {
            int count = 2 + random.Next(3);
            for (int i = 0; i < count; i++)
            {
                float size = Range(random, 0.35f, 0.5f) * Mathf.Min(s.x, s.z);
                var at = new Vector3(Range(random, -0.5f, 0.5f) * (s.x - size), 0f, Range(random, -0.5f, 0.5f) * (s.z - size));
                float h = Mathf.Min(s.y, size);
                b.Push(at + Vector3.up * h * 0.5f, Quaternion.Euler(0f, Range(random, -25f, 25f), 0f));
                b.Tint(i % 2 == 0 ? main : accent).BeveledBox(Vector3.zero, new Vector3(size, h, size), 0.03f);
                b.Pop();
            }
        }


        private static void Spire(MeshBuilder b, Vector3 s, GroundSwatch main, GroundSwatch accent, System.Random random)
        {
            int count = 2 + random.Next(3);
            for (int i = 0; i < count; i++)
            {
                var at = new Vector3(Range(random, -0.3f, 0.3f) * s.x, 0f, Range(random, -0.3f, 0.3f) * s.z);
                float r = Range(random, 0.18f, 0.3f) * Mathf.Min(s.x, s.z);
                b.Tint(i == 0 ? main : accent).Cylinder(at, r, r * 0.25f, s.y * Range(random, 0.55f, 1f), 5, false, true, true, Range(random, 0f, 1f));
            }
        }


        private static void CoolingTower(MeshBuilder b, Vector3 s, GroundSwatch main, GroundSwatch accent)
        {
            float r = s.x * 0.5f;
            b.Tint(main).Cylinder(Vector3.zero, r, r * 0.72f, s.y * 0.6f, 18, true, true, false);
            b.Cylinder(new Vector3(0f, s.y * 0.6f, 0f), r * 0.72f, r * 0.8f, s.y * 0.4f, 18, true, false, false);
            b.Tint(accent).Cylinder(new Vector3(0f, s.y * 0.3f, 0f), r * 0.72f, r * 0.72f, 0.02f, 18);
        }

        // ------------------------------------------------------------------ ground units

        /// <summary>The model of a ground unit (see <see cref="UnitModel"/>); null for a unit without one.</summary>
        public static UnitModel GroundUnit(StrikeUnit unit)
        {
            switch (unit)
            {
                case StrikeUnit.Turret: return GunTurret();
                case StrikeUnit.Flak: return FlakSite();
                case StrikeUnit.Tank: return Tank();
                case StrikeUnit.Truck: return Truck();
                case StrikeUnit.Bunker: return Bunker();
                case StrikeUnit.FuelTank: return FuelTank();
                case StrikeUnit.Radar: return Radar();
                case StrikeUnit.Gunboat: return Gunboat();
                case StrikeUnit.Hut: return Hut();
                case StrikeUnit.LaserTower: return LaserTower();
                case StrikeUnit.Depot: return Depot();
                case StrikeUnit.Crate: return SupplyCrate();
                default: return null;
            }
        }


        private static UnitModel GunTurret()
        {
            var m = new UnitModel(0.78f, 1.15f) { Turret = UnitModel.Part(), TurretPivot = new Vector3(0f, 0.46f, 0f) };
            MeshBuilder b = m.Body;
            b.Tint(GroundSwatch.ConcreteDark).Cylinder(Vector3.zero, 1.05f, 0.92f, 0.22f, 8, false, true, true, Mathf.PI / 8f);
            b.Tint(GroundSwatch.Concrete).Cylinder(new Vector3(0f, 0.22f, 0f), 0.92f, 0.86f, 0.03f, 8, false, false, true, Mathf.PI / 8f);
            b.Tint(UnitMark);
            for (int i = 0; i < 4; i++)
            {
                float angle = i * Mathf.PI * 0.5f + Mathf.PI * 0.25f;
                b.Box(new Vector3(Mathf.Cos(angle) * 0.72f, 0.26f, Mathf.Sin(angle) * 0.72f), new Vector3(0.18f, 0.02f, 0.18f));
            }
            b.Tint(UnitMetal).Cylinder(new Vector3(0f, 0.25f, 0f), 0.55f, 0.5f, 0.12f, 12);
            MeshBuilder t = m.Turret;
            t.Tint(UnitHull).Sphere(Vector3.zero, new Vector3(0.5f, 0.3f, 0.52f), 5, 12, false, 0.55f);
            t.Tint(UnitDark).BeveledBox(new Vector3(0f, 0.08f, 0.38f), new Vector3(0.5f, 0.2f, 0.3f), 0.05f);
            t.Tint(UnitMetal);
            for (int side = -1; side <= 1; side += 2)
            {
                t.Rod(new Vector3(side * 0.13f, 0.1f, 0.45f), new Vector3(side * 0.13f, 0.1f, 1.12f), 0.055f, 6);
                t.Cylinder(new Vector3(side * 0.13f, 0.1f, 1.02f), 0.075f, 0.075f, 0.12f, 6);
                m.Muzzles.Add(new Vector3(side * 0.13f, 0.1f, 1.15f));
            }
            t.Tint(UnitMark).Box(new Vector3(0f, 0.3f, -0.1f), new Vector3(0.62f, 0.03f, 0.14f));
            return m;
        }


        private static UnitModel FlakSite()
        {
            var m = new UnitModel(0.72f, 1.35f) { Turret = UnitModel.Part(), TurretPivot = new Vector3(0f, 0.2f, 0f) };
            MeshBuilder b = m.Body;
            b.Tint(GroundSwatch.ConcreteDark).Cylinder(Vector3.zero, 1.25f, 1.2f, 0.06f, 14);
            var random = new System.Random(1701);
            int bags = 14;
            for (int i = 0; i < bags; i++)
            {
                float angle = (i + 0.5f) * Mathf.PI * 2f / bags;
                if (Mathf.Abs(Mathf.DeltaAngle(angle * Mathf.Rad2Deg, 270f)) < 22f)
                {
                    continue;
                }
                var at = new Vector3(Mathf.Cos(angle) * 1.08f, 0.16f, Mathf.Sin(angle) * 1.08f);
                b.Push(at, Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f));
                b.Tint(i % 2 == 0 ? GroundSwatch.Khaki : GroundSwatch.Canvas).Blob(Vector3.zero, new Vector3(0.14f, 0.16f, 0.3f), 0, 0.12f, random.Next());
                b.Pop();
            }
            MeshBuilder t = m.Turret;
            t.Tint(UnitMetal).Cylinder(Vector3.zero, 0.42f, 0.38f, 0.14f, 10);
            t.Tint(UnitHull).BeveledBox(new Vector3(0f, 0.28f, 0.05f), new Vector3(0.78f, 0.26f, 0.7f), 0.05f);
            for (int side = -1; side <= 1; side += 2)
            {
                for (int row = 0; row < 2; row++)
                {
                    var from = new Vector3(side * (0.14f + row * 0.2f), 0.38f, -0.2f);
                    var to = from + new Vector3(0f, 0.12f, 1f);
                    t.Tint(UnitDark).Rod(from, to, 0.085f, 6);
                    t.Tint(UnitMark).Cylinder(to - new Vector3(0f, 0.012f, 0.1f), 0.07f, 0.07f, 0.03f, 6);
                }
                m.Muzzles.Add(new Vector3(side * 0.24f, 0.5f, 0.82f));
            }
            return m;
        }


        private static UnitModel Tank()
        {
            var m = new UnitModel(0.8f, 1.25f) { Turret = UnitModel.Part(), TurretPivot = new Vector3(0f, 0.58f, -0.12f) };
            MeshBuilder b = m.Body;
            for (int side = -1; side <= 1; side += 2)
            {
                b.Tint(GroundSwatch.Rubber).BeveledBox(new Vector3(side * 0.6f, 0.17f, 0f), new Vector3(0.34f, 0.34f, 2.2f), 0.09f);
                b.Tint(UnitDark);
                for (int i = 0; i < 9; i++)
                {
                    b.Box(new Vector3(side * 0.6f, 0.345f, -1f + i * 0.25f), new Vector3(0.34f, 0.01f, 0.06f));
                }
                b.Tint(UnitHull).Box(new Vector3(side * 0.6f, 0.37f, 0f), new Vector3(0.38f, 0.04f, 2.05f));
            }
            b.Tint(UnitHull).BeveledBox(new Vector3(0f, 0.3f, 0f), new Vector3(0.9f, 0.3f, 2f), 0.07f);
            b.Tint(UnitDark).Box(new Vector3(0f, 0.455f, -0.78f), new Vector3(0.7f, 0.02f, 0.36f));
            b.Tint(UnitMark).Box(new Vector3(0f, 0.455f, 0.82f), new Vector3(0.62f, 0.02f, 0.12f));
            MeshBuilder t = m.Turret;
            t.Tint(UnitHull).BeveledBox(new Vector3(0f, 0f, 0f), new Vector3(0.82f, 0.24f, 1f), 0.1f);
            t.Tint(UnitDark).Cylinder(new Vector3(0.2f, 0.12f, -0.2f), 0.13f, 0.12f, 0.06f, 8);
            t.Tint(UnitMetal).Rod(new Vector3(0f, 0.02f, 0.45f), new Vector3(0f, 0.02f, 1.52f), 0.065f, 7);
            t.Cylinder(new Vector3(0f, 0.02f, 1.38f), 0.085f, 0.085f, 0.14f, 7);
            t.Tint(UnitMark).Box(new Vector3(0f, 0.125f, 0.2f), new Vector3(0.5f, 0.01f, 0.1f));
            m.Muzzles.Add(new Vector3(0f, 0.02f, 1.55f));
            return m;
        }


        private static UnitModel Truck()
        {
            var m = new UnitModel(0.66f, 0.95f);
            MeshBuilder b = m.Body;
            b.Tint(GroundSwatch.Rubber);
            foreach (float z in new[] { 0.62f, -0.2f, -0.62f })
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    b.Push(new Vector3(side * 0.37f, 0.14f, z), Quaternion.Euler(0f, 0f, 90f));
                    b.Cylinder(new Vector3(0f, -0.07f, 0f), 0.14f, 0.14f, 0.14f, 8);
                    b.Pop();
                }
            }
            b.Tint(UnitDark).Box(new Vector3(0f, 0.22f, 0f), new Vector3(0.6f, 0.1f, 1.9f));
            b.Tint(UnitHull).BeveledBox(new Vector3(0f, 0.38f, 0.66f), new Vector3(0.72f, 0.36f, 0.5f), 0.06f);
            b.Tint(GroundSwatch.Glass).Box(new Vector3(0f, 0.46f, 0.92f), new Vector3(0.6f, 0.16f, 0.03f));
            b.Tint(GroundSwatch.Canvas).BeveledBox(new Vector3(0f, 0.42f, -0.3f), new Vector3(0.8f, 0.46f, 1.12f), 0.12f);
            b.Tint(GroundSwatch.Khaki);
            for (int i = 0; i < 4; i++)
            {
                b.Box(new Vector3(0f, 0.655f, -0.72f + i * 0.28f), new Vector3(0.7f, 0.01f, 0.04f));
            }
            return m;
        }


        private static UnitModel Bunker()
        {
            var m = new UnitModel(0.62f, 1.8f);
            MeshBuilder b = m.Body;
            var outline = new List<Vector2>();
            for (int i = 0; i < 8; i++)
            {
                float angle = (i + 0.5f) * Mathf.PI / 4f;
                outline.Add(new Vector2(Mathf.Cos(angle) * 1.75f, Mathf.Sin(angle) * 1.55f));
            }
            b.Push(new Vector3(0f, 0.18f, 0f), Quaternion.Euler(90f, 0f, 0f));
            b.Tint(GroundSwatch.ConcreteDark).Prism(outline, 0.36f);
            b.Pop();
            b.Tint(UnitHull).Sphere(new Vector3(0f, 0.36f, 0f), new Vector3(1.45f, 0.26f, 1.28f), 4, 8, false, 0.5f);
            b.Tint(GroundSwatch.Black);
            for (int i = -1; i <= 1; i++)
            {
                float angle = (270f + i * 32f) * Mathf.Deg2Rad;
                var at = new Vector3(Mathf.Cos(angle) * 1.52f, 0.28f, Mathf.Sin(angle) * 1.35f);
                b.Push(at, Quaternion.Euler(0f, -(270f + i * 32f) + 90f, 0f));
                b.Box(Vector3.zero, new Vector3(0.5f, 0.1f, 0.1f));
                b.Pop();
                var tip = at + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 0.32f;
                b.Tint(UnitMetal).Rod(at, tip, 0.06f, 6);
                b.Tint(GroundSwatch.Black);
                m.Muzzles.Add(tip);
            }
            b.Tint(UnitHull).Box(new Vector3(0f, 0.63f, 0f), new Vector3(0.9f, 0.04f, 0.6f));
            b.Tint(UnitMark).Box(new Vector3(0f, 0.655f, 0f), new Vector3(0.5f, 0.02f, 0.2f));
            b.Tint(GroundSwatch.Khaki);
            var random = new System.Random(4242);
            for (int i = 0; i < 6; i++)
            {
                float angle = (60f + i * 12f) * Mathf.Deg2Rad;
                b.Blob(new Vector3(Mathf.Cos(angle) * 1.55f, 0.1f, Mathf.Sin(angle) * 1.4f), new Vector3(0.24f, 0.12f, 0.16f), 0, 0.15f, random.Next());
            }
            return m;
        }


        private static UnitModel FuelTank()
        {
            var m = new UnitModel(0.86f, 1.2f);
            MeshBuilder b = m.Body;
            b.Tint(GroundSwatch.ConcreteDark).Cylinder(Vector3.zero, 1.12f, 1.1f, 0.06f, 16);
            b.Tint(GroundSwatch.ConcreteLight).Cylinder(new Vector3(0f, 0.06f, 0f), 0.92f, 0.92f, 0.58f, 18, true);
            b.Tint(GroundSwatch.EnemyRed).Cylinder(new Vector3(0f, 0.36f, 0f), 0.925f, 0.925f, 0.14f, 18, true, false, false);
            b.Tint(GroundSwatch.ConcreteLight).Sphere(new Vector3(0f, 0.64f, 0f), new Vector3(0.92f, 0.2f, 0.92f), 4, 18, true, 0.5f);
            b.Tint(GroundSwatch.Hazard).Cylinder(new Vector3(0f, 0.8f, 0f), 0.3f, 0.3f, 0.04f, 12);
            b.Tint(GroundSwatch.HazardDark);
            for (int i = 0; i < 3; i++)
            {
                b.Push(new Vector3(0f, 0.845f, 0f), Quaternion.Euler(0f, i * 60f, 0f));
                b.Box(Vector3.zero, new Vector3(0.58f, 0.01f, 0.07f));
                b.Pop();
            }
            b.Tint(GroundSwatch.PipeDark).Rod(new Vector3(0.9f, 0.12f, 0f), new Vector3(1.12f, 0.12f, 0f), 0.07f, 6);
            b.Rod(new Vector3(0.6f, 0.72f, 0.3f), new Vector3(0.93f, 0.2f, 0.3f), 0.03f, 4);
            return m;
        }


        private static UnitModel Radar()
        {
            var m = new UnitModel(0.9f, 1.3f) { Spinner = UnitModel.Part(), SpinnerPivot = new Vector3(0f, 0.62f, 0f) };
            MeshBuilder b = m.Body;
            b.Tint(GroundSwatch.ConcreteDark).Box(new Vector3(0f, 0.03f, 0f), new Vector3(2.2f, 0.06f, 2.2f));
            b.Tint(UnitHull).BeveledBox(new Vector3(-0.35f, 0.22f, -0.3f), new Vector3(1.1f, 0.34f, 1f), 0.05f);
            b.Tint(UnitMark).Box(new Vector3(-0.35f, 0.395f, -0.3f), new Vector3(0.7f, 0.01f, 0.18f));
            b.Tint(UnitMetal).Cylinder(new Vector3(0.35f, 0f, 0.35f), 0.2f, 0.12f, 0.62f, 8);
            b.Tint(GroundSwatch.Rubber).Box(new Vector3(0.7f, 0.12f, -0.6f), new Vector3(0.4f, 0.24f, 0.4f));
            m.SpinnerPivot = new Vector3(0.35f, 0.62f, 0.35f);
            MeshBuilder s = m.Spinner;
            s.Push(new Vector3(0f, 0.08f, 0f), Quaternion.Euler(28f, 0f, 0f));
            s.Tint(GroundSwatch.White).Sphere(new Vector3(0f, 0.25f, 0f), new Vector3(0.95f, 0.25f, 0.62f), 3, 14, false, 0.45f);
            s.Tint(GroundSwatch.Metal).Rod(new Vector3(0f, 0.05f, 0f), new Vector3(0f, 0.5f, 0.1f), 0.03f, 4);
            s.Tint(UnitMark).Sphere(new Vector3(0f, 0.5f, 0.1f), 0.07f, 3, 6);
            s.Pop();
            s.Tint(UnitMetal).Cylinder(Vector3.zero, 0.14f, 0.14f, 0.1f, 8);
            return m;
        }


        private static UnitModel Gunboat()
        {
            var m = new UnitModel(0.66f, 1.3f) { Turret = UnitModel.Part(), TurretPivot = new Vector3(0f, 0.44f, 0.62f) };
            MeshBuilder b = m.Body;
            var outline = new List<Vector2>
            {
                new Vector2(0f, 1.55f), new Vector2(0.38f, 1.05f), new Vector2(0.56f, 0.3f), new Vector2(0.56f, -1.2f),
                new Vector2(0.44f, -1.45f), new Vector2(-0.44f, -1.45f), new Vector2(-0.56f, -1.2f), new Vector2(-0.56f, 0.3f),
                new Vector2(-0.38f, 1.05f)
            };
            b.Push(new Vector3(0f, 0.16f, 0f), Quaternion.Euler(90f, 0f, 0f));
            b.Tint(GroundSwatch.BoatGrey).Prism(outline, 0.32f);
            b.Pop();
            var deck = new List<Vector2>();
            foreach (Vector2 p in outline)
            {
                deck.Add(p * 0.9f);
            }
            b.Push(new Vector3(0f, 0.33f, 0f), Quaternion.Euler(90f, 0f, 0f));
            b.Tint(GroundSwatch.ConcreteDark).Prism(deck, 0.02f);
            b.Pop();
            b.Tint(GroundSwatch.BoatGrey).BeveledBox(new Vector3(0f, 0.46f, -0.35f), new Vector3(0.66f, 0.26f, 0.8f), 0.05f);
            b.Tint(GroundSwatch.Glass).Box(new Vector3(0f, 0.52f, 0.06f), new Vector3(0.56f, 0.1f, 0.02f));
            b.Tint(UnitMetal).Rod(new Vector3(0f, 0.59f, -0.45f), new Vector3(0f, 0.66f, -0.45f), 0.03f, 4);
            b.Tint(UnitMark).Box(new Vector3(0f, 0.6f, -0.35f), new Vector3(0.4f, 0.01f, 0.4f));
            b.Tint(GroundSwatch.Black).Box(new Vector3(0f, 0.35f, -1.2f), new Vector3(0.5f, 0.04f, 0.2f));
            MeshBuilder t = m.Turret;
            t.Tint(UnitHull).Cylinder(new Vector3(0f, -0.1f, 0f), 0.26f, 0.22f, 0.18f, 10);
            t.Tint(UnitMetal);
            for (int side = -1; side <= 1; side += 2)
            {
                t.Rod(new Vector3(side * 0.08f, 0f, 0.1f), new Vector3(side * 0.08f, 0f, 0.62f), 0.04f, 6);
                m.Muzzles.Add(new Vector3(side * 0.08f, 0f, 0.64f));
            }
            return m;
        }


        private static UnitModel Hut()
        {
            var m = new UnitModel(0.76f, 1.2f);
            MeshBuilder b = m.Body;
            b.Tint(GroundSwatch.Tan).Box(new Vector3(0f, 0.22f, 0f), new Vector3(1.7f, 0.44f, 1.4f));
            b.Tint(GroundSwatch.WoodDark).Box(new Vector3(0.4f, 0.18f, 0.71f), new Vector3(0.36f, 0.34f, 0.02f));
            var a = new Vector3(-0.95f, 0.42f, -0.82f);
            var c = new Vector3(0.95f, 0.42f, -0.82f);
            var d = new Vector3(0.95f, 0.42f, 0.82f);
            var e = new Vector3(-0.95f, 0.42f, 0.82f);
            var r1 = new Vector3(-0.95f, 0.76f, 0f);
            var r2 = new Vector3(0.95f, 0.76f, 0f);
            var inside = new Vector3(0f, 0.5f, 0f);
            b.Tint(GroundSwatch.Rust).Quad(a, c, r2, r1, inside);
            b.Tint(GroundSwatch.Clay).Quad(e, d, r2, r1, inside);
            b.Tint(GroundSwatch.Tan).Triangle(a, e, r1, inside);
            b.Triangle(c, d, r2, inside);
            b.Tint(GroundSwatch.RoofRed).Box(new Vector3(0f, 0.765f, 0f), new Vector3(1.96f, 0.03f, 0.12f));
            for (int i = 0; i < 7; i++)
            {
                float x = -0.84f + i * 0.28f;
                for (int side = -1; side <= 1; side += 2)
                {
                    b.Tint(side < 0 ? GroundSwatch.Brick : GroundSwatch.Rust);
                    b.Push(new Vector3(x, 0.6f, side * 0.41f), Quaternion.Euler(side * -22.5f, 0f, 0f));
                    b.Box(Vector3.zero, new Vector3(0.05f, 0.02f, 0.86f));
                    b.Pop();
                }
            }
            b.Tint(GroundSwatch.StoneDark).Box(new Vector3(-0.5f, 0.78f, 0.2f), new Vector3(0.18f, 0.2f, 0.18f));
            b.Tint(GroundSwatch.Wood).Box(new Vector3(-1.15f, 0.12f, 0.35f), new Vector3(0.36f, 0.24f, 0.36f));
            return m;
        }


        private static UnitModel LaserTower()
        {
            var m = new UnitModel(0.92f, 1.3f) { Glow = new Vector3(0f, 0.86f, 0f) };
            MeshBuilder b = m.Body;
            b.Tint(GroundSwatch.PlateDark).BeveledBox(new Vector3(0f, 0.14f, 0f), new Vector3(1.9f, 0.28f, 1.9f), 0.12f);
            b.Tint(GroundSwatch.Hazard);
            for (int i = 0; i < 4; i++)
            {
                b.Push(new Vector3(0f, 0.285f, 0f), Quaternion.Euler(0f, i * 90f, 0f));
                b.Box(new Vector3(0f, 0f, 0.82f), new Vector3(1.2f, 0.01f, 0.1f));
                b.Pop();
            }
            b.Tint(UnitMetal).Cylinder(new Vector3(0f, 0.28f, 0f), 0.55f, 0.36f, 0.42f, 8);
            b.Tint(UnitMark).Cylinder(new Vector3(0f, 0.5f, 0f), 0.46f, 0.44f, 0.05f, 8, false, false, false);
            b.Tint(GroundSwatch.Plate).Cylinder(new Vector3(0f, 0.7f, 0f), 0.36f, 0.3f, 0.06f, 8);
            for (int i = 0; i < 4; i++)
            {
                float angle = i * Mathf.PI * 0.5f + Mathf.PI * 0.25f;
                var foot = new Vector3(Mathf.Cos(angle) * 0.34f, 0.72f, Mathf.Sin(angle) * 0.34f);
                b.Tint(UnitDark).Rod(foot, new Vector3(foot.x * 0.4f, 0.9f, foot.z * 0.4f), 0.04f, 4);
            }
            b.Tint(GroundSwatch.GlowRed).Sphere(new Vector3(0f, 0.82f, 0f), new Vector3(0.14f, 0.1f, 0.14f), 3, 8, false);
            m.Muzzles.Add(new Vector3(0f, 0.86f, 0f));
            return m;
        }


        private static UnitModel Depot()
        {
            var m = new UnitModel(0.9f, 2.6f);
            MeshBuilder b = m.Body;
            b.Tint(GroundSwatch.ConcreteDark).Box(new Vector3(0f, 0.03f, 0f), new Vector3(5f, 0.06f, 4.2f));
            b.Tint(GroundSwatch.Hazard);
            for (int i = 0; i < 10; i++)
            {
                b.Box(new Vector3(-2.25f + i * 0.5f, 0.065f, -2.02f), new Vector3(0.24f, 0.01f, 0.12f));
            }
            var tanks = new[] { new Vector3(-1.35f, 0f, 0.8f), new Vector3(0.25f, 0f, 1.05f), new Vector3(1.6f, 0f, 0.7f) };
            float[] radii = { 0.8f, 0.72f, 0.62f };
            for (int i = 0; i < tanks.Length; i++)
            {
                float r = radii[i];
                b.Tint(GroundSwatch.ConcreteLight).Cylinder(tanks[i] + Vector3.up * 0.06f, r, r, 0.58f, 16, true);
                b.Tint(GroundSwatch.EnemyRed).Cylinder(tanks[i] + Vector3.up * 0.34f, r * 1.01f, r * 1.01f, 0.12f, 16, true, false, false);
                b.Tint(GroundSwatch.ConcreteLight).Sphere(tanks[i] + Vector3.up * 0.64f, new Vector3(r, 0.18f, r), 3, 16, true, 0.5f);
                b.Tint(GroundSwatch.Hazard).Cylinder(tanks[i] + Vector3.up * 0.8f, r * 0.3f, r * 0.3f, 0.03f, 10);
            }
            b.Tint(GroundSwatch.PipeDark).Rod(new Vector3(-1.35f, 0.25f, 0.8f), new Vector3(1.6f, 0.25f, 0.7f), 0.07f, 6);
            b.Rod(new Vector3(0.25f, 0.25f, 1.05f), new Vector3(0.25f, 0.25f, -0.6f), 0.07f, 6);
            b.Tint(UnitHull).BeveledBox(new Vector3(-0.9f, 0.3f, -1.1f), new Vector3(2.2f, 0.6f, 1.3f), 0.05f);
            b.Tint(UnitDark).Box(new Vector3(-0.9f, 0.61f, -1.1f), new Vector3(2f, 0.02f, 1.1f));
            b.Tint(UnitMark).Box(new Vector3(-0.9f, 0.625f, -1.1f), new Vector3(0.9f, 0.01f, 0.3f));
            b.Tint(GroundSwatch.Wood).Box(new Vector3(1.4f, 0.18f, -1.2f), new Vector3(0.5f, 0.36f, 0.5f));
            b.Box(new Vector3(1.95f, 0.15f, -1.05f), new Vector3(0.4f, 0.3f, 0.4f));
            b.Tint(GroundSwatch.EnemyRed).Cylinder(new Vector3(1.5f, 0f, -0.45f), 0.18f, 0.18f, 0.4f, 8);
            b.Cylinder(new Vector3(1.9f, 0f, -0.45f), 0.18f, 0.18f, 0.4f, 8);
            return m;
        }


        private static UnitModel SupplyCrate()
        {
            var m = new UnitModel(0.6f, 0.8f);
            MeshBuilder b = m.Body;
            b.Tint(GroundSwatch.Hazard).BeveledBox(new Vector3(0f, 0.28f, 0f), new Vector3(1.1f, 0.56f, 1.1f), 0.05f);
            b.Tint(GroundSwatch.HazardDark);
            b.Box(new Vector3(0f, 0.565f, 0f), new Vector3(1.0f, 0.01f, 0.16f));
            b.Box(new Vector3(0f, 0.565f, 0f), new Vector3(0.16f, 0.01f, 1.0f));
            b.Tint(GroundSwatch.White).Box(new Vector3(0f, 0.575f, 0f), new Vector3(0.3f, 0.01f, 0.3f));
            b.Tint(GroundSwatch.Wood);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Box(new Vector3(side * 0.56f, 0.28f, 0f), new Vector3(0.04f, 0.5f, 0.9f));
                b.Box(new Vector3(0f, 0.28f, side * 0.56f), new Vector3(0.9f, 0.5f, 0.04f));
            }
            return m;
        }
    }
}
