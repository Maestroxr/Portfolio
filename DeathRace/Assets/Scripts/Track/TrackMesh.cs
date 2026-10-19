using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// A mesh being built in code for the track (road strips, barriers, set pieces): vertices with their own normal and
    /// uv, and triangles that are turned to face the way asked, so a builder never has to think about winding.
    /// </summary>
    internal sealed class TrackMesh
    {
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<Vector3> normals = new List<Vector3>();
        private readonly List<Vector2> uvs = new List<Vector2>();
        private readonly List<int> triangles = new List<int>();

        public int VertexCount => vertices.Count;
        public bool IsEmpty => triangles.Count == 0;

        public int Vertex(Vector3 position, Vector3 normal, Vector2 uv)
        {
            vertices.Add(position);
            normals.Add(normal);
            uvs.Add(uv);
            return vertices.Count - 1;
        }

        /// <summary>A triangle whose front faces <paramref name="facing"/> (Unity draws clockwise fronts).</summary>
        public void Triangle(int a, int b, int c, Vector3 facing)
        {
            Vector3 normal = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);
            triangles.Add(a);
            if (Vector3.Dot(normal, facing) >= 0f)
            {
                triangles.Add(b);
                triangles.Add(c);
            }
            else
            {
                triangles.Add(c);
                triangles.Add(b);
            }
        }

        /// <summary>A quad a-b-c-d (in order round its edge) whose front faces <paramref name="facing"/>.</summary>
        public void Quad(int a, int b, int c, int d, Vector3 facing)
        {
            Triangle(a, b, c, facing);
            Triangle(a, c, d, facing);
        }

        /// <summary>
        /// A strip between two rows of points facing up (a road, a verge, a curb): row <paramref name="a"/> and row
        /// <paramref name="b"/> side by side, with their uvs (a closed loop repeats its first pair at the end).
        /// </summary>
        public void Strip(IReadOnlyList<Vector3> a, IReadOnlyList<Vector3> b, IReadOnlyList<Vector2> uvA, IReadOnlyList<Vector2> uvB)
        {
            int count = a.Count;
            int first = vertices.Count;
            for (int i = 0; i < count; i++)
            {
                Vertex(a[i], Vector3.up, uvA[i]);
                Vertex(b[i], Vector3.up, uvB[i]);
            }
            for (int i = 0; i + 1 < count; i++)
            {
                int v = first + i * 2;
                Quad(v, v + 2, v + 3, v + 1, Vector3.up);
            }
        }

        /// <summary>An axis-aligned box (before <paramref name="rotation"/>) standing on <paramref name="foot"/>, flat shaded.</summary>
        public void Box(Vector3 foot, Vector3 size, Quaternion rotation, float uvScale = 0.25f)
        {
            Vector3 half = size * 0.5f;
            Vector3 centre = foot + rotation * new Vector3(0f, half.y, 0f);
            Vector3[] axes = { Vector3.right, Vector3.up, Vector3.forward };
            for (int axis = 0; axis < 3; axis++)
            {
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    Vector3 normal = axes[axis] * sign;
                    Vector3 u = axes[(axis + 1) % 3];
                    Vector3 v = axes[(axis + 2) % 3];
                    Vector3 faceCentre = Vector3.Scale(normal, half);
                    Vector3 du = Vector3.Scale(u, half);
                    Vector3 dv = Vector3.Scale(v, half);
                    float su = du.magnitude * 2f * uvScale;
                    float sv = dv.magnitude * 2f * uvScale;
                    Vector3 worldNormal = rotation * normal;
                    int a = Vertex(centre + rotation * (faceCentre - du - dv), worldNormal, new Vector2(0f, 0f));
                    int b = Vertex(centre + rotation * (faceCentre + du - dv), worldNormal, new Vector2(su, 0f));
                    int c = Vertex(centre + rotation * (faceCentre + du + dv), worldNormal, new Vector2(su, sv));
                    int d = Vertex(centre + rotation * (faceCentre - du + dv), worldNormal, new Vector2(0f, sv));
                    Quad(a, b, c, d, worldNormal);
                }
            }
        }

        /// <summary>A flat-shaded upright cylinder (or a cone when <paramref name="topRadius"/> is 0) standing on <paramref name="foot"/>.</summary>
        public void Cylinder(Vector3 foot, float radius, float topRadius, float height, int sides, Quaternion rotation, bool caps = true)
        {
            Vector3 up = rotation * Vector3.up;
            for (int i = 0; i < sides; i++)
            {
                float a0 = i * Mathf.PI * 2f / sides;
                float a1 = (i + 1) * Mathf.PI * 2f / sides;
                Vector3 d0 = rotation * new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0));
                Vector3 d1 = rotation * new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));
                Vector3 b0 = foot + d0 * radius;
                Vector3 b1 = foot + d1 * radius;
                Vector3 t0 = foot + up * height + d0 * topRadius;
                Vector3 t1 = foot + up * height + d1 * topRadius;
                Vector3 side = (d0 + d1).normalized * height + up * (radius - topRadius);
                Vector3 normal = side.normalized;
                float u0 = i / (float)sides;
                float u1 = (i + 1) / (float)sides;
                int a = Vertex(b0, normal, new Vector2(u0, 0f));
                int b = Vertex(b1, normal, new Vector2(u1, 0f));
                int c = Vertex(t1, normal, new Vector2(u1, 1f));
                if (topRadius > 1e-4f)
                {
                    int d = Vertex(t0, normal, new Vector2(u0, 1f));
                    Quad(a, b, c, d, normal);
                }
                else
                {
                    Triangle(a, b, c, normal);
                }
                if (caps && topRadius > 1e-4f)
                {
                    Vector3 top = foot + up * height;
                    int ct = Vertex(top, up, new Vector2(0.5f, 0.5f));
                    int c0 = Vertex(t0, up, new Vector2(0.5f + d0.x * 0.5f, 0.5f + d0.z * 0.5f));
                    int c1 = Vertex(t1, up, new Vector2(0.5f + d1.x * 0.5f, 0.5f + d1.z * 0.5f));
                    Triangle(ct, c0, c1, up);
                }
            }
        }

        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh { name = name };
            if (vertices.Count > 65000)
            {
                mesh.indexFormat = IndexFormat.UInt32;
            }
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
