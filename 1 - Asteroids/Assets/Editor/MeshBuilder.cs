using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Portfolio.Asteroids.EditorTools
{
    /// <summary>
    /// Builds low-poly meshes out of primitives. Faces are flat shaded unless a primitive asks for smooth normals; the
    /// winding of every face is fixed up from a point inside the shape, so primitives can be written without caring
    /// about vertex order. Each face samples the current palette swatch, or a planar projection for textured meshes.
    /// </summary>
    internal sealed class MeshBuilder
    {
        private readonly List<Vector3> positions = new List<Vector3>();
        private readonly List<Vector3> normals = new List<Vector3>();
        private readonly List<Vector2> uvs = new List<Vector2>();
        private readonly List<List<int>> submeshes = new List<List<int>> { new List<int>() };
        private readonly Stack<Matrix4x4> stack = new Stack<Matrix4x4>();
        private Matrix4x4 matrix = Matrix4x4.identity;
        private Matrix4x4 normalMatrix = Matrix4x4.identity;
        private Vector2 uv = new Vector2(0.5f, 0.5f);
        private int submesh;
        private bool projected;
        private Vector3 projectionU;
        private Vector3 projectionV;

        public int VertexCount => positions.Count;

        public MeshBuilder Color(Swatch swatch)
        {
            uv = Palette.UV(swatch);
            projected = false;
            return this;
        }

        /// <summary>Faces get UVs from their position: u = dot(p, axisU), v = dot(p, axisV), in builder space.</summary>
        public MeshBuilder Project(Vector3 axisU, Vector3 axisV)
        {
            projected = true;
            projectionU = axisU;
            projectionV = axisV;
            return this;
        }

        public MeshBuilder Submesh(int index)
        {
            while (submeshes.Count <= index)
            {
                submeshes.Add(new List<int>());
            }
            submesh = index;
            return this;
        }

        public void Push(Vector3 translation, Quaternion rotation, Vector3 scale)
        {
            stack.Push(matrix);
            matrix = matrix * Matrix4x4.TRS(translation, rotation, scale);
            normalMatrix = matrix.inverse.transpose;
        }

        public void Push(Vector3 translation, Quaternion rotation)
        {
            Push(translation, rotation, Vector3.one);
        }

        public void Push(Vector3 translation)
        {
            Push(translation, Quaternion.identity, Vector3.one);
        }

        public void Pop()
        {
            matrix = stack.Pop();
            normalMatrix = matrix.inverse.transpose;
        }

        // ------------------------------------------------------------------ faces

        /// <summary>A flat triangle facing away from <paramref name="inside"/> (local coordinates).</summary>
        public void Triangle(Vector3 a, Vector3 b, Vector3 c, Vector3 inside)
        {
            Vector3 wa = matrix.MultiplyPoint3x4(a);
            Vector3 wb = matrix.MultiplyPoint3x4(b);
            Vector3 wc = matrix.MultiplyPoint3x4(c);
            Vector3 wi = matrix.MultiplyPoint3x4(inside);
            Vector3 normal = Vector3.Cross(wb - wa, wc - wa);
            if (normal.sqrMagnitude < 1e-14f)
            {
                return;
            }
            if (Vector3.Dot(normal, (wa + wb + wc) / 3f - wi) < 0f)
            {
                (wb, wc) = (wc, wb);
                normal = -normal;
            }
            AddFlat(wa, wb, wc, normal.normalized, a, b, c);
        }

        /// <summary>A flat triangle whose normal points along <paramref name="outward"/> (local direction).</summary>
        public void TriangleFacing(Vector3 a, Vector3 b, Vector3 c, Vector3 outward)
        {
            Vector3 wa = matrix.MultiplyPoint3x4(a);
            Vector3 wb = matrix.MultiplyPoint3x4(b);
            Vector3 wc = matrix.MultiplyPoint3x4(c);
            Vector3 normal = Vector3.Cross(wb - wa, wc - wa);
            if (normal.sqrMagnitude < 1e-14f)
            {
                return;
            }
            if (Vector3.Dot(normal, normalMatrix.MultiplyVector(outward)) < 0f)
            {
                (wb, wc) = (wc, wb);
                (b, c) = (c, b);
                normal = -normal;
            }
            AddFlat(wa, wb, wc, normal.normalized, a, b, c);
        }

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 inside)
        {
            Triangle(a, b, c, inside);
            Triangle(a, c, d, inside);
        }

        public void QuadFacing(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 outward)
        {
            TriangleFacing(a, b, c, outward);
            TriangleFacing(a, c, d, outward);
        }

        private void AddFlat(Vector3 a, Vector3 b, Vector3 c, Vector3 normal, Vector3 la, Vector3 lb, Vector3 lc)
        {
            int start = positions.Count;
            positions.Add(a);
            positions.Add(b);
            positions.Add(c);
            normals.Add(normal);
            normals.Add(normal);
            normals.Add(normal);
            if (projected)
            {
                uvs.Add(ProjectUV(a));
                uvs.Add(ProjectUV(b));
                uvs.Add(ProjectUV(c));
            }
            else
            {
                uvs.Add(uv);
                uvs.Add(uv);
                uvs.Add(uv);
            }
            List<int> indices = submeshes[submesh];
            indices.Add(start);
            indices.Add(start + 1);
            indices.Add(start + 2);
        }

        private Vector2 ProjectUV(Vector3 worldPosition)
        {
            return new Vector2(Vector3.Dot(worldPosition, projectionU), Vector3.Dot(worldPosition, projectionV));
        }

        private int SmoothVertex(Vector3 localPosition, Vector3 localNormal)
        {
            Vector3 position = matrix.MultiplyPoint3x4(localPosition);
            positions.Add(position);
            normals.Add(normalMatrix.MultiplyVector(localNormal).normalized);
            uvs.Add(projected ? ProjectUV(position) : uv);
            return positions.Count - 1;
        }

        private void SmoothTriangle(int a, int b, int c)
        {
            Vector3 face = Vector3.Cross(positions[b] - positions[a], positions[c] - positions[a]);
            if (face.sqrMagnitude < 1e-14f)
            {
                return;
            }
            List<int> indices = submeshes[submesh];
            if (Vector3.Dot(face, normals[a] + normals[b] + normals[c]) < 0f)
            {
                indices.Add(a);
                indices.Add(c);
                indices.Add(b);
            }
            else
            {
                indices.Add(a);
                indices.Add(b);
                indices.Add(c);
            }
        }

        // ------------------------------------------------------------------ primitives

        public void Box(Vector3 center, Vector3 size)
        {
            Vector3 half = size * 0.5f;
            for (int axis = 0; axis < 3; axis++)
            {
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    Vector3 normal = Vector3.zero;
                    normal[axis] = sign;
                    Vector3 u = Vector3.zero;
                    u[(axis + 1) % 3] = half[(axis + 1) % 3];
                    Vector3 v = Vector3.zero;
                    v[(axis + 2) % 3] = half[(axis + 2) % 3];
                    Vector3 face = center + Vector3.Scale(normal, half);
                    Quad(face - u - v, face + u - v, face + u + v, face - u + v, center);
                }
            }
        }

        /// <summary>A box with chamfered edges and corners.</summary>
        public void BeveledBox(Vector3 center, Vector3 size, float bevel)
        {
            Vector3 half = size * 0.5f;
            bevel = Mathf.Min(bevel, Mathf.Min(half.x, Mathf.Min(half.y, half.z)) * 0.9f);
            if (bevel <= 0.0001f)
            {
                Box(center, size);
                return;
            }
            Vector3 inner = half - Vector3.one * bevel;

            Vector3 Corner(Vector3 signs, int axis)
            {
                Vector3 point = Vector3.Scale(signs, inner);
                point[axis] = signs[axis] * half[axis];
                return center + point;
            }

            for (int axis = 0; axis < 3; axis++)
            {
                int u = (axis + 1) % 3;
                int v = (axis + 2) % 3;
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    Vector3 s1 = Signs(axis, sign, u, -1, v, -1);
                    Vector3 s2 = Signs(axis, sign, u, 1, v, -1);
                    Vector3 s3 = Signs(axis, sign, u, 1, v, 1);
                    Vector3 s4 = Signs(axis, sign, u, -1, v, 1);
                    Quad(Corner(s1, axis), Corner(s2, axis), Corner(s3, axis), Corner(s4, axis), center);
                }
            }
            for (int a = 0; a < 3; a++)
            {
                for (int b = a + 1; b < 3; b++)
                {
                    int c = 3 - a - b;
                    for (int sa = -1; sa <= 1; sa += 2)
                    {
                        for (int sb = -1; sb <= 1; sb += 2)
                        {
                            Vector3 low = Signs(a, sa, b, sb, c, -1);
                            Vector3 high = Signs(a, sa, b, sb, c, 1);
                            Quad(Corner(low, a), Corner(high, a), Corner(high, b), Corner(low, b), center);
                        }
                    }
                }
            }
            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        var signs = new Vector3(x, y, z);
                        Triangle(Corner(signs, 0), Corner(signs, 1), Corner(signs, 2), center);
                    }
                }
            }
        }

        private static Vector3 Signs(int axisA, int signA, int axisB, int signB, int axisC, int signC)
        {
            Vector3 signs = Vector3.zero;
            signs[axisA] = signA;
            signs[axisB] = signB;
            signs[axisC] = signC;
            return signs;
        }

        /// <summary>A frustum along +Y from <paramref name="baseCenter"/>; a top radius of zero makes a cone.</summary>
        public void Cylinder(Vector3 baseCenter, float bottomRadius, float topRadius, float height, int segments,
            bool smooth = false, bool capBottom = true, bool capTop = true, float startAngle = 0f)
        {
            segments = Mathf.Max(3, segments);
            Vector3 top = baseCenter + Vector3.up * height;
            Vector3 inside = baseCenter + Vector3.up * (height * 0.5f);
            float slope = height > 0.0001f ? (bottomRadius - topRadius) / height : 0f;
            var bottomRing = new Vector3[segments];
            var topRing = new Vector3[segments];
            var directions = new Vector3[segments];
            for (int i = 0; i < segments; i++)
            {
                float angle = startAngle + i * Mathf.PI * 2f / segments;
                var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                directions[i] = direction;
                bottomRing[i] = baseCenter + direction * bottomRadius;
                topRing[i] = top + direction * topRadius;
            }
            if (smooth)
            {
                var bottomIndex = new int[segments];
                var topIndex = new int[segments];
                for (int i = 0; i < segments; i++)
                {
                    Vector3 normal = (directions[i] + Vector3.up * slope).normalized;
                    bottomIndex[i] = SmoothVertex(bottomRing[i], normal);
                    topIndex[i] = SmoothVertex(topRing[i], normal);
                }
                for (int i = 0; i < segments; i++)
                {
                    int next = (i + 1) % segments;
                    SmoothTriangle(bottomIndex[i], bottomIndex[next], topIndex[next]);
                    if (topRadius > 0.0001f)
                    {
                        SmoothTriangle(bottomIndex[i], topIndex[next], topIndex[i]);
                    }
                }
            }
            else
            {
                for (int i = 0; i < segments; i++)
                {
                    int next = (i + 1) % segments;
                    Quad(bottomRing[i], bottomRing[next], topRing[next], topRing[i], inside);
                }
            }
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                if (capBottom && bottomRadius > 0.0001f)
                {
                    TriangleFacing(baseCenter, bottomRing[i], bottomRing[next], Vector3.down);
                }
                if (capTop && topRadius > 0.0001f)
                {
                    TriangleFacing(top, topRing[i], topRing[next], Vector3.up);
                }
            }
        }

        /// <summary>A cylinder between two points.</summary>
        public void Rod(Vector3 from, Vector3 to, float radius, int segments, bool smooth = true, float endRadius = -1f)
        {
            Vector3 axis = to - from;
            float length = axis.magnitude;
            if (length < 0.0001f)
            {
                return;
            }
            Push(from, Quaternion.FromToRotation(Vector3.up, axis / length));
            Cylinder(Vector3.zero, radius, endRadius < 0f ? radius : endRadius, length, segments, smooth);
            Pop();
        }

        /// <summary>
        /// An ellipsoid (UV sphere). Smooth or faceted. A <paramref name="coverage"/> below 1 keeps only the top of it
        /// (0.5 is a dome) and closes the cut with a flat bottom.
        /// </summary>
        public void Sphere(Vector3 center, Vector3 radii, int rings, int segments, bool smooth = true, float coverage = 1f)
        {
            rings = Mathf.Max(2, rings);
            segments = Mathf.Max(3, segments);
            coverage = Mathf.Clamp(coverage, 0.05f, 1f);
            bool cut = coverage < 0.999f;
            var grid = new Vector3[rings + 1, segments];
            var normalGrid = new Vector3[rings + 1, segments];
            for (int r = 0; r <= rings; r++)
            {
                float theta = Mathf.PI * coverage * r / rings;
                for (int s = 0; s < segments; s++)
                {
                    float phi = Mathf.PI * 2f * s / segments;
                    var unit = new Vector3(Mathf.Sin(theta) * Mathf.Cos(phi), Mathf.Cos(theta), Mathf.Sin(theta) * Mathf.Sin(phi));
                    grid[r, s] = center + Vector3.Scale(unit, radii);
                    normalGrid[r, s] = new Vector3(unit.x / Mathf.Max(radii.x, 1e-4f), unit.y / Mathf.Max(radii.y, 1e-4f), unit.z / Mathf.Max(radii.z, 1e-4f)).normalized;
                }
            }
            if (smooth)
            {
                var index = new int[rings + 1, segments];
                for (int r = 0; r <= rings; r++)
                {
                    for (int s = 0; s < segments; s++)
                    {
                        index[r, s] = SmoothVertex(grid[r, s], normalGrid[r, s]);
                    }
                }
                for (int r = 0; r < rings; r++)
                {
                    for (int s = 0; s < segments; s++)
                    {
                        int next = (s + 1) % segments;
                        if (r > 0)
                        {
                            SmoothTriangle(index[r, s], index[r, next], index[r + 1, next]);
                        }
                        if (r < rings - 1 || cut)
                        {
                            SmoothTriangle(index[r, s], index[r + 1, next], index[r + 1, s]);
                        }
                    }
                }
            }
            else
            {
                for (int r = 0; r < rings; r++)
                {
                    for (int s = 0; s < segments; s++)
                    {
                        int next = (s + 1) % segments;
                        if (r == 0)
                        {
                            Triangle(grid[0, s], grid[1, s], grid[1, next], center);
                        }
                        else if (r == rings - 1 && !cut)
                        {
                            Triangle(grid[r, s], grid[r, next], grid[r + 1, s], center);
                        }
                        else
                        {
                            Quad(grid[r, s], grid[r, next], grid[r + 1, next], grid[r + 1, s], center);
                        }
                    }
                }
            }
            if (cut)
            {
                Vector3 capCenter = Vector3.zero;
                for (int s = 0; s < segments; s++)
                {
                    capCenter += grid[rings, s];
                }
                capCenter /= segments;
                for (int s = 0; s < segments; s++)
                {
                    TriangleFacing(capCenter, grid[rings, s], grid[rings, (s + 1) % segments], Vector3.down);
                }
            }
        }

        public void Sphere(Vector3 center, float radius, int rings = 8, int segments = 12, bool smooth = true)
        {
            Sphere(center, Vector3.one * radius, rings, segments, smooth);
        }

        /// <summary>
        /// Clips a polygon to a rectangle (Sutherland-Hodgman); used to cut stripes to the board they are painted on.
        /// </summary>
        public static List<Vector2> Clip(IList<Vector2> polygon, Rect rect)
        {
            var output = new List<Vector2>(polygon);
            for (int edge = 0; edge < 4 && output.Count > 0; edge++)
            {
                var input = new List<Vector2>(output);
                output.Clear();
                for (int i = 0; i < input.Count; i++)
                {
                    Vector2 current = input[i];
                    Vector2 previous = input[(i + input.Count - 1) % input.Count];
                    bool currentIn = Inside(current, rect, edge);
                    bool previousIn = Inside(previous, rect, edge);
                    if (currentIn)
                    {
                        if (!previousIn)
                        {
                            output.Add(Intersect(previous, current, rect, edge));
                        }
                        output.Add(current);
                    }
                    else if (previousIn)
                    {
                        output.Add(Intersect(previous, current, rect, edge));
                    }
                }
            }
            return output;
        }

        private static bool Inside(Vector2 point, Rect rect, int edge)
        {
            switch (edge)
            {
                case 0: return point.x >= rect.xMin;
                case 1: return point.x <= rect.xMax;
                case 2: return point.y >= rect.yMin;
                default: return point.y <= rect.yMax;
            }
        }

        private static Vector2 Intersect(Vector2 a, Vector2 b, Rect rect, int edge)
        {
            float t;
            switch (edge)
            {
                case 0: t = (rect.xMin - a.x) / (b.x - a.x); break;
                case 1: t = (rect.xMax - a.x) / (b.x - a.x); break;
                case 2: t = (rect.yMin - a.y) / (b.y - a.y); break;
                default: t = (rect.yMax - a.y) / (b.y - a.y); break;
            }
            return Vector2.Lerp(a, b, t);
        }

        /// <summary>Outline of a star with <paramref name="points"/> tips, first tip pointing up.</summary>
        public static Vector2[] StarOutline(int points, float outer, float inner)
        {
            var outline = new Vector2[points * 2];
            for (int i = 0; i < outline.Length; i++)
            {
                float angle = Mathf.PI * 0.5f + Mathf.PI * i / points;
                float radius = i % 2 == 0 ? outer : inner;
                outline[i] = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
            }
            return outline;
        }

        /// <summary>
        /// A faceted, slightly lumpy blob (a subdivided icosahedron with jittered vertices): foliage, rocks, clouds.
        /// </summary>
        public void Blob(Vector3 center, Vector3 radii, int subdivisions, float jitter, int seed)
        {
            var random = new System.Random(seed);
            float t = (1f + Mathf.Sqrt(5f)) / 2f;
            var vertices = new List<Vector3>
            {
                new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1)
            };
            for (int i = 0; i < vertices.Count; i++)
            {
                vertices[i] = vertices[i].normalized;
            }
            var faces = new List<int>
            {
                0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
                3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1
            };
            for (int level = 0; level < subdivisions; level++)
            {
                var cache = new Dictionary<long, int>();
                var next = new List<int>();

                int Middle(int a, int b)
                {
                    long key = a < b ? ((long)a << 32) + b : ((long)b << 32) + a;
                    if (cache.TryGetValue(key, out int existing))
                    {
                        return existing;
                    }
                    vertices.Add(((vertices[a] + vertices[b]) * 0.5f).normalized);
                    cache[key] = vertices.Count - 1;
                    return vertices.Count - 1;
                }

                for (int f = 0; f < faces.Count; f += 3)
                {
                    int a = faces[f];
                    int b = faces[f + 1];
                    int c = faces[f + 2];
                    int ab = Middle(a, b);
                    int bc = Middle(b, c);
                    int ca = Middle(c, a);
                    next.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
                }
                faces = next;
            }
            var placed = new Vector3[vertices.Count];
            for (int i = 0; i < vertices.Count; i++)
            {
                float scale = 1f + ((float)random.NextDouble() * 2f - 1f) * jitter;
                placed[i] = center + Vector3.Scale(vertices[i] * scale, radii);
            }
            for (int f = 0; f < faces.Count; f += 3)
            {
                Triangle(placed[faces[f]], placed[faces[f + 1]], placed[faces[f + 2]], center);
            }
        }

        /// <summary>A torus around +Y in the XZ plane, or part of one.</summary>
        public void Torus(Vector3 center, float majorRadius, float minorRadius, int majorSegments, int minorSegments,
            float arcDegrees = 360f, bool smooth = true, float startDegrees = 0f)
        {
            bool closed = arcDegrees >= 359.9f;
            int ringCount = closed ? majorSegments : majorSegments + 1;
            var index = new int[ringCount, minorSegments];
            var grid = new Vector3[ringCount, minorSegments];
            var ringCenters = new Vector3[ringCount];
            for (int i = 0; i < ringCount; i++)
            {
                float u = (startDegrees + arcDegrees * i / majorSegments) * Mathf.Deg2Rad;
                var radial = new Vector3(Mathf.Cos(u), 0f, Mathf.Sin(u));
                ringCenters[i] = center + radial * majorRadius;
                for (int j = 0; j < minorSegments; j++)
                {
                    float v = Mathf.PI * 2f * j / minorSegments;
                    Vector3 normal = radial * Mathf.Cos(v) + Vector3.up * Mathf.Sin(v);
                    grid[i, j] = ringCenters[i] + normal * minorRadius;
                    if (smooth)
                    {
                        index[i, j] = SmoothVertex(grid[i, j], normal);
                    }
                }
            }
            for (int i = 0; i < majorSegments; i++)
            {
                int nextRing = (i + 1) % ringCount;
                for (int j = 0; j < minorSegments; j++)
                {
                    int next = (j + 1) % minorSegments;
                    if (smooth)
                    {
                        SmoothTriangle(index[i, j], index[nextRing, j], index[nextRing, next]);
                        SmoothTriangle(index[i, j], index[nextRing, next], index[i, next]);
                    }
                    else
                    {
                        Vector3 inside = (ringCenters[i] + ringCenters[nextRing]) * 0.5f;
                        Quad(grid[i, j], grid[nextRing, j], grid[nextRing, next], grid[i, next], inside);
                    }
                }
            }
        }

        /// <summary>A ramp: rises from the ground at z = 0 to <paramref name="height"/> at z = <paramref name="length"/>.</summary>
        public void Wedge(float width, float height, float length)
        {
            float x = width * 0.5f;
            var inside = new Vector3(0f, height / 3f, length * 2f / 3f);
            var a = new Vector3(-x, 0f, 0f);
            var b = new Vector3(x, 0f, 0f);
            var c = new Vector3(x, 0f, length);
            var d = new Vector3(-x, 0f, length);
            var e = new Vector3(x, height, length);
            var f = new Vector3(-x, height, length);
            Quad(a, b, e, f, inside);
            Quad(d, c, e, f, inside);
            Quad(a, b, c, d, inside);
            Triangle(b, c, e, inside);
            Triangle(a, d, f, inside);
        }

        /// <summary>A polygon in the XY plane extruded along Z (depth centred on z = 0). Works for star-shaped outlines.</summary>
        public void Prism(IList<Vector2> outline, float depth, Vector3 offset = default)
        {
            int count = outline.Count;
            Vector2 centroid = Vector2.zero;
            float area = 0f;
            for (int i = 0; i < count; i++)
            {
                centroid += outline[i];
                Vector2 p = outline[i];
                Vector2 q = outline[(i + 1) % count];
                area += p.x * q.y - q.x * p.y;
            }
            centroid /= count;
            float half = depth * 0.5f;
            float winding = area >= 0f ? 1f : -1f;
            var front = new Vector3(centroid.x, centroid.y, half) + offset;
            var back = new Vector3(centroid.x, centroid.y, -half) + offset;
            for (int i = 0; i < count; i++)
            {
                Vector2 p = outline[i];
                Vector2 q = outline[(i + 1) % count];
                var pf = new Vector3(p.x, p.y, half) + offset;
                var qf = new Vector3(q.x, q.y, half) + offset;
                var pb = new Vector3(p.x, p.y, -half) + offset;
                var qb = new Vector3(q.x, q.y, -half) + offset;
                TriangleFacing(front, pf, qf, Vector3.forward);
                TriangleFacing(back, pb, qb, Vector3.back);
                Vector2 edge = q - p;
                var outward = new Vector3(edge.y, -edge.x, 0f) * winding;
                QuadFacing(pf, qf, qb, pb, outward);
            }
        }

        /// <summary>A tube following a path (a spring, a handle). Smooth shaded, open ends.</summary>
        public void Tube(IList<Vector3> path, float radius, int sides)
        {
            if (path.Count < 2)
            {
                return;
            }
            var rings = new int[path.Count, sides];
            Vector3 reference = Vector3.up;
            for (int i = 0; i < path.Count; i++)
            {
                Vector3 tangent = (path[Mathf.Min(i + 1, path.Count - 1)] - path[Mathf.Max(i - 1, 0)]).normalized;
                if (Mathf.Abs(Vector3.Dot(tangent, reference)) > 0.95f)
                {
                    reference = Vector3.right;
                }
                Vector3 side = Vector3.Cross(tangent, reference).normalized;
                Vector3 up = Vector3.Cross(side, tangent).normalized;
                for (int j = 0; j < sides; j++)
                {
                    float angle = Mathf.PI * 2f * j / sides;
                    Vector3 normal = side * Mathf.Cos(angle) + up * Mathf.Sin(angle);
                    rings[i, j] = SmoothVertex(path[i] + normal * radius, normal);
                }
            }
            for (int i = 0; i < path.Count - 1; i++)
            {
                for (int j = 0; j < sides; j++)
                {
                    int next = (j + 1) % sides;
                    SmoothTriangle(rings[i, j], rings[i + 1, j], rings[i + 1, next]);
                    SmoothTriangle(rings[i, j], rings[i + 1, next], rings[i, next]);
                }
            }
        }

        /// <summary>A flat quad, visible from <paramref name="outward"/>.</summary>
        public void Plane(Vector3 center, Vector3 axisU, Vector3 axisV, Vector3 outward)
        {
            QuadFacing(center - axisU - axisV, center + axisU - axisV, center + axisU + axisV, center - axisU + axisV, outward);
        }

        // ------------------------------------------------------------------ output

        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh { name = name };
            Fill(mesh);
            return mesh;
        }

        /// <summary>Writes the geometry into <paramref name="mesh"/>, replacing whatever it held.</summary>
        public void Fill(Mesh mesh)
        {
            mesh.Clear();
            mesh.indexFormat = positions.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(positions);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            int used = submeshes.Count;
            while (used > 1 && submeshes[used - 1].Count == 0)
            {
                used--;
            }
            mesh.subMeshCount = used;
            for (int i = 0; i < used; i++)
            {
                mesh.SetTriangles(submeshes[i], i, false);
            }
            mesh.RecalculateBounds();
        }
    }
}
