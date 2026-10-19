using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.DeathRace.EditorTools
{
    internal static partial class CarMeshes
    {
        /// <summary>
        /// The light of one headlight on the road: a fan from the lamp (at the origin) 11 m forward, widening to 4 m,
        /// in rows so the beam texture is not bent across its two triangles.
        /// </summary>
        public static Mesh BeamMesh()
        {
            const int rows = 10;
            const float length = 11f;
            var positions = new List<Vector3>();
            var uvs = new List<Vector2>();
            var normals = new List<Vector3>();
            var triangles = new List<int>();
            for (int r = 0; r <= rows; r++)
            {
                float v = r / (float)rows;
                float half = Mathf.Lerp(0.3f, 2.1f, Mathf.Pow(v, 0.85f));
                for (int c = 0; c <= 2; c++)
                {
                    float u = c * 0.5f;
                    positions.Add(new Vector3((u - 0.5f) * 2f * half, 0f, v * length));
                    uvs.Add(new Vector2(u, v));
                    normals.Add(Vector3.up);
                }
            }
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < 2; c++)
                {
                    int a = r * 3 + c;
                    int b = a + 1;
                    int d = a + 3;
                    int e = d + 1;
                    triangles.AddRange(new[] { a, d, e, a, e, b });
                }
            }
            return Build("Beam", positions, normals, uvs, null, triangles);
        }

        /// <summary>A flat quad on the ground, centred, <paramref name="width"/> across and <paramref name="length"/> along z.</summary>
        public static Mesh GroundQuad(float width, float length)
        {
            float x = width * 0.5f;
            float z = length * 0.5f;
            var positions = new List<Vector3> { new Vector3(-x, 0f, -z), new Vector3(x, 0f, -z), new Vector3(x, 0f, z), new Vector3(-x, 0f, z) };
            var normals = new List<Vector3> { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            var uvs = new List<Vector2> { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
            var triangles = new List<int> { 0, 2, 1, 0, 3, 2 };
            return Build("GroundQuad", positions, normals, uvs, null, triangles);
        }

        /// <summary>
        /// A nitro flame along +z from its base at the origin: a blue outer cone and a white core, the colours in the
        /// vertices fading out towards the tips (drawn additive, both sides).
        /// </summary>
        public static Mesh FlameMesh()
        {
            return FlameMesh("NitroFlame", 0.17f, 1.5f, new Color(0.2f, 0.45f, 1f, 1f), new Color(0.35f, 0.15f, 1f, 0f),
                new Color(1f, 1f, 1f, 1f), new Color(0.55f, 0.85f, 1f, 0f));
        }

        /// <summary>The flame of a rocket motor: orange outside, a yellow-white core.</summary>
        public static Mesh RocketFlameMesh()
        {
            return FlameMesh("RocketFlame", 0.1f, 0.85f, new Color(1f, 0.55f, 0.15f, 0.9f), new Color(1f, 0.2f, 0.05f, 0f),
                new Color(1f, 0.95f, 0.75f, 1f), new Color(1f, 0.7f, 0.3f, 0f));
        }

        private static Mesh FlameMesh(string name, float radius, float length, Color outerBase, Color outerTip, Color coreBase, Color coreTip)
        {
            var positions = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var colors = new List<Color>();
            var triangles = new List<int>();
            Cone(positions, normals, uvs, colors, triangles, radius, length, outerBase, outerTip);
            Cone(positions, normals, uvs, colors, triangles, radius * 0.5f, length * 0.57f, coreBase, coreTip);
            return Build(name, positions, normals, uvs, colors, triangles);
        }

        private static void Cone(List<Vector3> positions, List<Vector3> normals, List<Vector2> uvs, List<Color> colors, List<int> triangles,
            float radius, float length, Color baseColor, Color tipColor)
        {
            const int segments = 10;
            int start = positions.Count;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                var radial = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                positions.Add(radial * radius);
                normals.Add(radial);
                uvs.Add(new Vector2(0.5f, 0.5f));
                colors.Add(baseColor);
            }
            positions.Add(new Vector3(0f, 0f, length));
            normals.Add(Vector3.forward);
            uvs.Add(new Vector2(0.5f, 0.5f));
            colors.Add(tipColor);
            int tip = positions.Count - 1;
            for (int i = 0; i < segments; i++)
            {
                triangles.AddRange(new[] { start + i, start + (i + 1) % segments, tip });
            }
        }

        private static Mesh Build(string name, List<Vector3> positions, List<Vector3> normals, List<Vector2> uvs, List<Color> colors, List<int> triangles)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(positions);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            if (colors != null)
            {
                mesh.SetColors(colors);
            }
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
