using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Portfolio.Monopoly.EditorTools
{
    /// <summary>Collects vertices and triangles of a procedural mesh; parts are appended with a transform.</summary>
    internal sealed class MeshBuilder
    {
        public readonly List<Vector3> Vertices = new List<Vector3>();
        public readonly List<Vector3> Normals = new List<Vector3>();
        public readonly List<Vector2> Uvs = new List<Vector2>();
        public readonly List<int> Triangles = new List<int>();

        public int Add(Vector3 position, Vector3 normal, Vector2 uv)
        {
            Vertices.Add(position);
            Normals.Add(normal);
            Uvs.Add(uv);
            return Vertices.Count - 1;
        }

        public void Triangle(int a, int b, int c)
        {
            Triangles.Add(a);
            Triangles.Add(b);
            Triangles.Add(c);
        }

        public void Quad(int a, int b, int c, int d)
        {
            Triangle(a, b, c);
            Triangle(a, c, d);
        }

        public MeshBuilder Append(MeshBuilder other, Matrix4x4 transform)
        {
            int offset = Vertices.Count;
            Matrix4x4 normalMatrix = transform.inverse.transpose;
            for (int i = 0; i < other.Vertices.Count; i++)
            {
                Vertices.Add(transform.MultiplyPoint3x4(other.Vertices[i]));
                Normals.Add(normalMatrix.MultiplyVector(other.Normals[i]).normalized);
                Uvs.Add(other.Uvs[i]);
            }
            bool flip = transform.determinant < 0f;
            for (int i = 0; i < other.Triangles.Count; i += 3)
            {
                if (flip)
                {
                    Triangle(other.Triangles[i] + offset, other.Triangles[i + 2] + offset, other.Triangles[i + 1] + offset);
                }
                else
                {
                    Triangle(other.Triangles[i] + offset, other.Triangles[i + 1] + offset, other.Triangles[i + 2] + offset);
                }
            }
            return this;
        }

        public MeshBuilder Append(MeshBuilder other)
        {
            return Append(other, Matrix4x4.identity);
        }

        public Bounds Bounds()
        {
            if (Vertices.Count == 0)
            {
                return new Bounds();
            }
            var bounds = new Bounds(Vertices[0], Vector3.zero);
            foreach (Vector3 v in Vertices)
            {
                bounds.Encapsulate(v);
            }
            return bounds;
        }

        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh { name = name };
            if (Vertices.Count > 65000)
            {
                mesh.indexFormat = IndexFormat.UInt32;
            }
            mesh.SetVertices(Vertices);
            mesh.SetNormals(Normals);
            mesh.SetUVs(0, Uvs);
            mesh.SetTriangles(Triangles, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }
    }

    /// <summary>Procedural shapes for the tokens, dice, houses, hotels and board pieces.</summary>
    internal static class MeshFactory
    {
        // ------------------------------------------------------------------ extrusion

        /// <summary>
        /// Extrudes a counter-clockwise outline (x/y) along z: <paramref name="depth"/> thick, centred on z = 0, with a
        /// rounded bevel of <paramref name="bevel"/> on both faces so the edges catch the light like a cast metal piece.
        /// </summary>
        public static MeshBuilder Extrude(IList<Vector2> outline, float depth, float bevel, int bevelSteps = 3)
        {
            var mesh = new MeshBuilder();
            int n = outline.Count;
            bevel = Mathf.Min(bevel, depth * 0.45f);
            Vector2[] normals = OutlineNormals(outline, out bool[] sharp);
            float half = depth * 0.5f;

            // Rings from the side wall towards each face: inset grows as the bevel turns to face the viewer.
            var rings = new List<(Vector2[] points, float z, float angle)>();
            for (int s = bevelSteps; s >= 0; s--)
            {
                float angle = Mathf.PI * 0.5f * s / bevelSteps;
                float inset = bevel * (1f - Mathf.Cos(angle));
                rings.Add((Offset(outline, normals, -inset), -(half - bevel) - bevel * Mathf.Sin(angle), -angle));
            }
            for (int s = 0; s <= bevelSteps; s++)
            {
                float angle = Mathf.PI * 0.5f * s / bevelSteps;
                float inset = bevel * (1f - Mathf.Cos(angle));
                rings.Add((Offset(outline, normals, -inset), half - bevel + bevel * Mathf.Sin(angle), angle));
            }

            float perimeter = 0f;
            var along = new float[n + 1];
            for (int i = 0; i < n; i++)
            {
                along[i] = perimeter;
                perimeter += (outline[(i + 1) % n] - outline[i]).magnitude;
            }
            along[n] = perimeter;

            for (int r = 0; r < rings.Count - 1; r++)
            {
                var a = rings[r];
                var b = rings[r + 1];
                for (int i = 0; i < n; i++)
                {
                    int j = (i + 1) % n;
                    // Hard corners keep the edge's own normal; smooth ones share the averaged normal.
                    Vector2 ni = sharp[i] ? EdgeNormal(outline, i) : normals[i];
                    Vector2 nj = sharp[j] ? EdgeNormal(outline, i) : normals[j];
                    int v0 = mesh.Add(new Vector3(a.points[i].x, a.points[i].y, a.z), BevelNormal(ni, a.angle), new Vector2(along[i] / perimeter, r / (float)rings.Count));
                    int v1 = mesh.Add(new Vector3(a.points[j].x, a.points[j].y, a.z), BevelNormal(nj, a.angle), new Vector2(along[i + 1] / perimeter, r / (float)rings.Count));
                    int v2 = mesh.Add(new Vector3(b.points[j].x, b.points[j].y, b.z), BevelNormal(nj, b.angle), new Vector2(along[i + 1] / perimeter, (r + 1) / (float)rings.Count));
                    int v3 = mesh.Add(new Vector3(b.points[i].x, b.points[i].y, b.z), BevelNormal(ni, b.angle), new Vector2(along[i] / perimeter, (r + 1) / (float)rings.Count));
                    mesh.Quad(v0, v1, v2, v3);
                }
            }

            // The two faces.
            Vector2[] face = rings[rings.Count - 1].points;
            List<int> triangles = Triangulate(face);
            Bounds2(face, out Vector2 min, out Vector2 size);
            int front = mesh.Vertices.Count;
            foreach (Vector2 p in face)
            {
                mesh.Add(new Vector3(p.x, p.y, half), Vector3.forward, (p - min) / Mathf.Max(size.x, size.y));
            }
            int back = mesh.Vertices.Count;
            foreach (Vector2 p in face)
            {
                mesh.Add(new Vector3(p.x, p.y, -half), Vector3.back, (p - min) / Mathf.Max(size.x, size.y));
            }
            // Unity's front faces wind clockwise as seen: the +z face shows the outline mirrored, so it keeps the
            // counter-clockwise order, and the -z face turns it around.
            for (int i = 0; i < triangles.Count; i += 3)
            {
                mesh.Triangle(front + triangles[i], front + triangles[i + 1], front + triangles[i + 2]);
                mesh.Triangle(back + triangles[i], back + triangles[i + 2], back + triangles[i + 1]);
            }
            return mesh;
        }

        private static Vector3 BevelNormal(Vector2 outward, float angle)
        {
            return new Vector3(outward.x * Mathf.Cos(angle), outward.y * Mathf.Cos(angle), Mathf.Sin(angle)).normalized;
        }

        private static Vector2 EdgeNormal(IList<Vector2> outline, int i)
        {
            Vector2 edge = outline[(i + 1) % outline.Count] - outline[i];
            return new Vector2(edge.y, -edge.x).normalized;
        }

        /// <summary>Outward vertex normals of a counter-clockwise outline, and which corners are sharp.</summary>
        private static Vector2[] OutlineNormals(IList<Vector2> outline, out bool[] sharp)
        {
            int n = outline.Count;
            var result = new Vector2[n];
            sharp = new bool[n];
            for (int i = 0; i < n; i++)
            {
                Vector2 before = EdgeNormal(outline, (i - 1 + n) % n);
                Vector2 after = EdgeNormal(outline, i);
                result[i] = (before + after).normalized;
                if (result[i].sqrMagnitude < 0.01f)
                {
                    result[i] = after;
                }
                sharp[i] = Vector2.Dot(before, after) < 0.6f;
            }
            return result;
        }

        /// <summary>Moves every point along its normal (negative: inwards), with mitred corners kept in check.</summary>
        private static Vector2[] Offset(IList<Vector2> outline, Vector2[] normals, float distance)
        {
            int n = outline.Count;
            var result = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                Vector2 before = EdgeNormal(outline, (i - 1 + n) % n);
                float cos = Mathf.Max(0.35f, Vector2.Dot(normals[i], before));
                result[i] = outline[i] + normals[i] * (distance / cos);
            }
            return result;
        }

        private static void Bounds2(IList<Vector2> points, out Vector2 min, out Vector2 size)
        {
            min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            foreach (Vector2 p in points)
            {
                min = Vector2.Min(min, p);
                max = Vector2.Max(max, p);
            }
            size = max - min;
        }

        /// <summary>Ear clipping of a simple counter-clockwise polygon; returns index triples (counter-clockwise).</summary>
        public static List<int> Triangulate(IList<Vector2> polygon)
        {
            var result = new List<int>();
            var remaining = new List<int>();
            for (int i = 0; i < polygon.Count; i++)
            {
                remaining.Add(i);
            }
            int guard = polygon.Count * polygon.Count;
            while (remaining.Count > 3 && guard-- > 0)
            {
                bool clipped = false;
                for (int i = 0; i < remaining.Count; i++)
                {
                    int a = remaining[(i - 1 + remaining.Count) % remaining.Count];
                    int b = remaining[i];
                    int c = remaining[(i + 1) % remaining.Count];
                    if (!IsEar(polygon, remaining, a, b, c))
                    {
                        continue;
                    }
                    result.Add(a);
                    result.Add(b);
                    result.Add(c);
                    remaining.RemoveAt(i);
                    clipped = true;
                    break;
                }
                if (!clipped)
                {
                    // A degenerate spot (collinear points): drop the flattest corner and go on.
                    remaining.RemoveAt(FlattestCorner(polygon, remaining));
                }
            }
            if (remaining.Count == 3)
            {
                result.Add(remaining[0]);
                result.Add(remaining[1]);
                result.Add(remaining[2]);
            }
            return result;
        }

        private static bool IsEar(IList<Vector2> polygon, List<int> remaining, int a, int b, int c)
        {
            Vector2 pa = polygon[a], pb = polygon[b], pc = polygon[c];
            float cross = (pb.x - pa.x) * (pc.y - pa.y) - (pb.y - pa.y) * (pc.x - pa.x);
            if (cross <= 1e-7f)
            {
                return false;
            }
            foreach (int i in remaining)
            {
                if (i == a || i == b || i == c)
                {
                    continue;
                }
                if (InTriangle(polygon[i], pa, pb, pc))
                {
                    return false;
                }
            }
            return true;
        }

        private static int FlattestCorner(IList<Vector2> polygon, List<int> remaining)
        {
            int best = 0;
            float flattest = float.MaxValue;
            for (int i = 0; i < remaining.Count; i++)
            {
                Vector2 pa = polygon[remaining[(i - 1 + remaining.Count) % remaining.Count]];
                Vector2 pb = polygon[remaining[i]];
                Vector2 pc = polygon[remaining[(i + 1) % remaining.Count]];
                float cross = Mathf.Abs((pb.x - pa.x) * (pc.y - pa.y) - (pb.y - pa.y) * (pc.x - pa.x));
                if (cross < flattest)
                {
                    flattest = cross;
                    best = i;
                }
            }
            return best;
        }

        private static bool InTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);
            float d2 = (p.x - c.x) * (b.y - c.y) - (b.x - c.x) * (p.y - c.y);
            float d3 = (p.x - a.x) * (c.y - a.y) - (c.x - a.x) * (p.y - a.y);
            bool negative = d1 < 0 || d2 < 0 || d3 < 0;
            bool positive = d1 > 0 || d2 > 0 || d3 > 0;
            return !(negative && positive);
        }

        // ------------------------------------------------------------------ lathe and primitives

        /// <summary>
        /// Turns a profile around the y axis. The profile runs as (radius, height) points, usually from the axis at the
        /// bottom to the axis at the top; corners sharper than about 40 degrees stay crisp.
        /// </summary>
        public static MeshBuilder Lathe(IList<Vector2> profile, int segments)
        {
            var mesh = new MeshBuilder();
            int n = profile.Count;
            var tangents = new Vector2[n - 1];
            float length = 0f;
            var distance = new float[n];
            for (int i = 0; i < n - 1; i++)
            {
                Vector2 d = profile[i + 1] - profile[i];
                tangents[i] = d.normalized;
                distance[i] = length;
                length += d.magnitude;
            }
            distance[n - 1] = length;
            for (int i = 0; i < n - 1; i++)
            {
                Vector2 p0 = profile[i];
                Vector2 p1 = profile[i + 1];
                Vector2 n0 = SegmentNormal(tangents, i, i, true);
                Vector2 n1 = SegmentNormal(tangents, i, i + 1, false);
                int start = mesh.Vertices.Count;
                for (int s = 0; s <= segments; s++)
                {
                    float angle = 2f * Mathf.PI * s / segments;
                    float cos = Mathf.Cos(angle), sin = Mathf.Sin(angle);
                    mesh.Add(new Vector3(p0.x * cos, p0.y, p0.x * sin), new Vector3(n0.x * cos, n0.y, n0.x * sin).normalized, new Vector2(s / (float)segments, distance[i] / length));
                    mesh.Add(new Vector3(p1.x * cos, p1.y, p1.x * sin), new Vector3(n1.x * cos, n1.y, n1.x * sin).normalized, new Vector2(s / (float)segments, distance[i + 1] / length));
                }
                for (int s = 0; s < segments; s++)
                {
                    int a = start + s * 2;
                    int b = a + 1;
                    int c = a + 3;
                    int d = a + 2;
                    mesh.Quad(a, b, c, d);
                }
            }
            return mesh;
        }

        /// <summary>The outward normal of a profile segment at one of its ends, smoothed with the neighbour when the turn is gentle.</summary>
        private static Vector2 SegmentNormal(Vector2[] tangents, int segment, int point, bool start)
        {
            Vector2 own = new Vector2(tangents[segment].y, -tangents[segment].x);
            int neighbour = start ? segment - 1 : segment + 1;
            if (neighbour < 0 || neighbour >= tangents.Length)
            {
                return own;
            }
            Vector2 other = new Vector2(tangents[neighbour].y, -tangents[neighbour].x);
            return Vector2.Dot(own, other) > 0.75f ? (own + other).normalized : own;
        }

        /// <summary>A cylinder around the y axis with rounded rims, from y = 0 to <paramref name="height"/>.</summary>
        public static MeshBuilder Cylinder(float radius, float height, float rim, int segments)
        {
            rim = Mathf.Min(rim, Mathf.Min(radius, height * 0.5f) * 0.9f);
            var profile = new List<Vector2> { new Vector2(0f, 0f) };
            AddArc(profile, new Vector2(radius - rim, rim), rim, -90f, 0f, 3);
            AddArc(profile, new Vector2(radius - rim, height - rim), rim, 0f, 90f, 3);
            profile.Add(new Vector2(0f, height));
            return Lathe(profile, segments);
        }

        private static void AddArc(List<Vector2> points, Vector2 center, float radius, float from, float to, int steps)
        {
            for (int i = 0; i <= steps; i++)
            {
                float angle = Mathf.Lerp(from, to, i / (float)steps) * Mathf.Deg2Rad;
                points.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
        }

        public static MeshBuilder Ellipsoid(Vector3 radii, int longitude = 24, int latitude = 16)
        {
            var mesh = new MeshBuilder();
            for (int y = 0; y <= latitude; y++)
            {
                float v = y / (float)latitude;
                float theta = v * Mathf.PI;
                for (int x = 0; x <= longitude; x++)
                {
                    float u = x / (float)longitude;
                    float phi = u * 2f * Mathf.PI;
                    var unit = new Vector3(Mathf.Sin(theta) * Mathf.Cos(phi), -Mathf.Cos(theta), Mathf.Sin(theta) * Mathf.Sin(phi));
                    Vector3 p = Vector3.Scale(unit, radii);
                    Vector3 normal = new Vector3(unit.x / radii.x, unit.y / radii.y, unit.z / radii.z).normalized;
                    mesh.Add(p, normal, new Vector2(u, v));
                }
            }
            for (int y = 0; y < latitude; y++)
            {
                for (int x = 0; x < longitude; x++)
                {
                    int a = y * (longitude + 1) + x;
                    int b = a + longitude + 1;
                    mesh.Quad(a, b, b + 1, a + 1);
                }
            }
            return mesh;
        }

        public static MeshBuilder Cone(float radius, float height, int segments)
        {
            var profile = new List<Vector2> { new Vector2(0f, 0f), new Vector2(radius, 0f), new Vector2(radius * 0.02f, height), new Vector2(0f, height) };
            return Lathe(profile, segments);
        }

        public static MeshBuilder Torus(float major, float minor, int segments, int sides)
        {
            var profile = new List<Vector2>();
            for (int i = 0; i <= sides; i++)
            {
                float angle = -Mathf.PI * 0.5f + 2f * Mathf.PI * i / sides;
                profile.Add(new Vector2(major + Mathf.Cos(angle) * minor, Mathf.Sin(angle) * minor));
            }
            return Lathe(profile, segments);
        }

        /// <summary>A box with flat faces, centred on <paramref name="center"/>.</summary>
        public static MeshBuilder Box(Vector3 center, Vector3 size)
        {
            var mesh = new MeshBuilder();
            Vector3 h = size * 0.5f;
            Vector3[] normals = { Vector3.up, Vector3.down, Vector3.right, Vector3.left, Vector3.forward, Vector3.back };
            foreach (Vector3 normal in normals)
            {
                Vector3 up = Mathf.Abs(normal.y) > 0.5f ? Vector3.forward : Vector3.up;
                Vector3 right = Vector3.Cross(normal, up);
                Vector3 c = center + Vector3.Scale(normal, h);
                Vector3 r = right * Vector3.Scale(Abs(right), h).magnitude;
                Vector3 u = up * Vector3.Scale(Abs(up), h).magnitude;
                int a = mesh.Add(c - r - u, normal, new Vector2(0f, 0f));
                int b = mesh.Add(c + r - u, normal, new Vector2(1f, 0f));
                int d = mesh.Add(c + r + u, normal, new Vector2(1f, 1f));
                int e = mesh.Add(c - r + u, normal, new Vector2(0f, 1f));
                mesh.Quad(a, e, d, b);
            }
            return mesh;
        }

        private static Vector3 Abs(Vector3 v)
        {
            return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        }

        /// <summary>A gabled roof: a triangular prism over a <paramref name="width"/> by <paramref name="depth"/> base, ridge along x.</summary>
        public static MeshBuilder Roof(float width, float depth, float height, float overhang)
        {
            var mesh = new MeshBuilder();
            float w = width * 0.5f + overhang;
            float d = depth * 0.5f + overhang;
            var ridgeL = new Vector3(-w, height, 0f);
            var ridgeR = new Vector3(w, height, 0f);
            var frontL = new Vector3(-w, 0f, -d);
            var frontR = new Vector3(w, 0f, -d);
            var backL = new Vector3(-w, 0f, d);
            var backR = new Vector3(w, 0f, d);
            Vector3 frontNormal = Vector3.Cross(frontR - frontL, ridgeL - frontL).normalized;
            if (frontNormal.z > 0f)
            {
                frontNormal = -frontNormal;
            }
            Vector3 backNormal = new Vector3(frontNormal.x, frontNormal.y, -frontNormal.z);
            int a = mesh.Add(frontL, frontNormal, new Vector2(0f, 0f));
            int b = mesh.Add(frontR, frontNormal, new Vector2(1f, 0f));
            int c = mesh.Add(ridgeR, frontNormal, new Vector2(1f, 1f));
            int e = mesh.Add(ridgeL, frontNormal, new Vector2(0f, 1f));
            mesh.Quad(a, e, c, b);
            a = mesh.Add(backR, backNormal, new Vector2(0f, 0f));
            b = mesh.Add(backL, backNormal, new Vector2(1f, 0f));
            c = mesh.Add(ridgeL, backNormal, new Vector2(1f, 1f));
            e = mesh.Add(ridgeR, backNormal, new Vector2(0f, 1f));
            mesh.Quad(a, e, c, b);
            // Gable ends.
            a = mesh.Add(frontL, Vector3.left, Vector2.zero);
            b = mesh.Add(backL, Vector3.left, Vector2.right);
            c = mesh.Add(ridgeL, Vector3.left, Vector2.one);
            mesh.Triangle(a, c, b);
            a = mesh.Add(backR, Vector3.right, Vector2.zero);
            b = mesh.Add(frontR, Vector3.right, Vector2.right);
            c = mesh.Add(ridgeR, Vector3.right, Vector2.one);
            mesh.Triangle(a, c, b);
            // Underside.
            a = mesh.Add(frontL, Vector3.down, Vector2.zero);
            b = mesh.Add(frontR, Vector3.down, Vector2.right);
            c = mesh.Add(backR, Vector3.down, Vector2.one);
            e = mesh.Add(backL, Vector3.down, Vector2.up);
            mesh.Quad(a, b, c, e);
            return mesh;
        }

        /// <summary>
        /// A cube with rounded edges and corners (<paramref name="size"/> across, corners of <paramref name="radius"/>),
        /// each face mapped onto its cell of a 3 by 2 atlas: +Y, +Z, +X on the top row, -X, -Z, -Y on the bottom row.
        /// </summary>
        public static MeshBuilder RoundedCube(float size, float radius, int grid)
        {
            var mesh = new MeshBuilder();
            float half = size * 0.5f;
            float inner = half - radius;
            Vector3[] normals = { Vector3.up, Vector3.forward, Vector3.right, Vector3.left, Vector3.back, Vector3.down };
            for (int f = 0; f < normals.Length; f++)
            {
                Vector3 normal = normals[f];
                Vector3 up = Mathf.Abs(normal.y) > 0.5f ? (normal.y > 0f ? Vector3.forward : Vector3.back) : Vector3.up;
                Vector3 right = Vector3.Cross(normal, up);
                var cell = new Vector2((f % 3) / 3f, f < 3 ? 0.5f : 0f);
                int start = mesh.Vertices.Count;
                for (int y = 0; y <= grid; y++)
                {
                    for (int x = 0; x <= grid; x++)
                    {
                        float s = x / (float)grid;
                        float t = y / (float)grid;
                        Vector3 flat = normal * half + right * ((s - 0.5f) * size) + up * ((t - 0.5f) * size);
                        Vector3 clamped = new Vector3(Mathf.Clamp(flat.x, -inner, inner), Mathf.Clamp(flat.y, -inner, inner), Mathf.Clamp(flat.z, -inner, inner));
                        Vector3 offset = flat - clamped;
                        Vector3 n = offset.sqrMagnitude > 1e-8f ? offset.normalized : normal;
                        Vector3 position = clamped + n * radius;
                        var uv = new Vector2(cell.x + (0.02f + s * 0.96f) / 3f, cell.y + (0.02f + t * 0.96f) / 2f);
                        mesh.Add(position, n, uv);
                    }
                }
                for (int y = 0; y < grid; y++)
                {
                    for (int x = 0; x < grid; x++)
                    {
                        int a = start + y * (grid + 1) + x;
                        int b = a + 1;
                        int c = a + grid + 2;
                        int d = a + grid + 1;
                        mesh.Quad(a, d, c, b);
                    }
                }
            }
            return mesh;
        }

        /// <summary>A flat rectangle facing up (y), <paramref name="width"/> along x and <paramref name="depth"/> along z.</summary>
        public static MeshBuilder Plane(float width, float depth, Rect uv)
        {
            var mesh = new MeshBuilder();
            float w = width * 0.5f;
            float d = depth * 0.5f;
            int a = mesh.Add(new Vector3(-w, 0f, -d), Vector3.up, new Vector2(uv.xMin, uv.yMin));
            int b = mesh.Add(new Vector3(w, 0f, -d), Vector3.up, new Vector2(uv.xMax, uv.yMin));
            int c = mesh.Add(new Vector3(w, 0f, d), Vector3.up, new Vector2(uv.xMax, uv.yMax));
            int e = mesh.Add(new Vector3(-w, 0f, d), Vector3.up, new Vector2(uv.xMin, uv.yMax));
            mesh.Quad(a, e, c, b);
            return mesh;
        }

        public static Matrix4x4 TRS(Vector3 position, Vector3 euler, Vector3 scale)
        {
            return Matrix4x4.TRS(position, Quaternion.Euler(euler), scale);
        }
    }
}
