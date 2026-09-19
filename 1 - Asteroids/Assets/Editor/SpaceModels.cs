using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Asteroids.EditorTools
{
    /// <summary>
    /// The procedural models of the Asteroids module, built with <see cref="MeshBuilder"/> and coloured through the
    /// palette: saucers, supply pods, drones, crystals, the frame of a pickup, ice and void-crystal asteroids, and the
    /// flat meshes of flames and rings. Models are built with +Y toward the camera and +Z as their nose; prefabs turn
    /// them with a -90 degree rotation about X so the nose points up the screen.
    /// </summary>
    internal static class SpaceModels
    {
        /// <summary>A flying saucer: lens-shaped hull, rim band, glass dome, underside glow. Its rim lights are <see cref="SaucerLights"/>.</summary>
        public static MeshBuilder Saucer(bool scout)
        {
            var b = new MeshBuilder();
            Swatch hull = scout ? Swatch.DarkRed : Swatch.Hull;
            Swatch lower = scout ? Swatch.HullDark : Swatch.Panel;
            b.Color(lower);
            b.Cylinder(new Vector3(0f, -0.2f, 0f), 0.42f, 1f, 0.2f, 24, true, true, false);
            b.Color(hull);
            b.Cylinder(Vector3.zero, 1f, 0.62f, 0.2f, 24, true, false, true);
            b.Color(Swatch.Trim);
            b.Torus(Vector3.zero, 1f, 0.055f, 32, 6);
            b.Color(scout ? Swatch.GlowRed : Swatch.GlowCyan);
            b.Sphere(new Vector3(0f, 0.18f, 0f), new Vector3(0.42f, 0.36f, 0.42f), 8, 16, true, 0.5f);
            b.Color(scout ? Swatch.GlowOrange : Swatch.GlowGreen);
            b.Torus(new Vector3(0f, -0.2f, 0f), 0.42f, 0.05f, 20, 6);
            b.Color(Swatch.Gunmetal);
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f + 22.5f;
                b.Push(Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, 0.12f, 0.8f), Quaternion.Euler(-28f, angle, 0f));
                b.BeveledBox(Vector3.zero, new Vector3(0.22f, 0.05f, 0.18f), 0.02f);
                b.Pop();
            }
            return b;
        }

        /// <summary>The ring of lights around a saucer's rim (spins on its own).</summary>
        public static MeshBuilder SaucerLights(bool scout, int count = 12)
        {
            var b = new MeshBuilder();
            for (int i = 0; i < count; i++)
            {
                float angle = i * Mathf.PI * 2f / count;
                b.Color(i % 2 == 0 ? (scout ? Swatch.GlowOrange : Swatch.GlowYellow) : (scout ? Swatch.GlowRed : Swatch.GlowCyan));
                b.Sphere(new Vector3(Mathf.Cos(angle) * 1.02f, 0.02f, Mathf.Sin(angle) * 1.02f), 0.065f, 4, 6, false);
            }
            return b;
        }

        /// <summary>A supply capsule lying along +Z with glowing bands and fins.</summary>
        public static MeshBuilder SupplyPod()
        {
            var b = new MeshBuilder();
            b.Push(Vector3.zero, Quaternion.Euler(90f, 0f, 0f));
            b.Color(Swatch.White);
            b.Cylinder(new Vector3(0f, -0.5f, 0f), 0.36f, 0.36f, 1f, 16, true, false, false);
            b.Color(Swatch.HullDark);
            b.Sphere(new Vector3(0f, 0.5f, 0f), 0.36f, 6, 16, true);
            b.Sphere(new Vector3(0f, -0.5f, 0f), 0.36f, 6, 16, true);
            b.Color(Swatch.GlowGreen);
            b.Cylinder(new Vector3(0f, 0.2f, 0f), 0.375f, 0.375f, 0.08f, 16, true, false, false);
            b.Cylinder(new Vector3(0f, -0.28f, 0f), 0.375f, 0.375f, 0.08f, 16, true, false, false);
            b.Color(Swatch.Trim);
            for (int i = 0; i < 4; i++)
            {
                b.Push(Vector3.zero, Quaternion.Euler(0f, i * 90f + 45f, 0f));
                b.BeveledBox(new Vector3(0f, -0.6f, 0.42f), new Vector3(0.06f, 0.35f, 0.2f), 0.02f);
                b.Pop();
            }
            b.Pop();
            b.Color(Swatch.Hazard);
            b.BeveledBox(new Vector3(0f, 0.37f, 0f), new Vector3(0.3f, 0.02f, 0.3f), 0.01f);
            return b;
        }

        /// <summary>A small wing drone: dark core, trim ring and a glowing eye.</summary>
        public static MeshBuilder Drone()
        {
            var b = new MeshBuilder();
            b.Color(Swatch.HullDark);
            b.Sphere(Vector3.zero, 0.2f, 6, 10, true);
            b.Color(Swatch.Chrome);
            b.Torus(Vector3.zero, 0.27f, 0.04f, 16, 6);
            b.Color(Swatch.GlowGreen);
            b.Sphere(new Vector3(0f, 0.12f, 0.12f), 0.07f, 4, 8, true);
            b.Color(Swatch.Trim);
            b.BeveledBox(new Vector3(0.32f, 0f, -0.05f), new Vector3(0.14f, 0.04f, 0.2f), 0.015f);
            b.BeveledBox(new Vector3(-0.32f, 0f, -0.05f), new Vector3(0.14f, 0.04f, 0.2f), 0.015f);
            return b;
        }

        /// <summary>A cut crystal (a stretched bipyramid), the collectible that ore rocks drop.</summary>
        public static MeshBuilder Gem(Swatch facet, Swatch light)
        {
            var b = new MeshBuilder();
            const int sides = 6;
            var ring = new Vector3[sides];
            for (int i = 0; i < sides; i++)
            {
                float angle = i * Mathf.PI * 2f / sides;
                ring[i] = new Vector3(Mathf.Cos(angle) * 0.3f, 0.08f, Mathf.Sin(angle) * 0.3f);
            }
            var top = new Vector3(0f, 0.5f, 0f);
            var bottom = new Vector3(0f, -0.5f, 0f);
            for (int i = 0; i < sides; i++)
            {
                int next = (i + 1) % sides;
                b.Color(i % 2 == 0 ? facet : light);
                b.Triangle(ring[i], ring[next], top, Vector3.zero);
                b.Color(i % 2 == 0 ? light : facet);
                b.Triangle(ring[i], ring[next], bottom, Vector3.zero);
            }
            return b;
        }

        /// <summary>The hexagonal frame around a pickup's glowing core.</summary>
        public static MeshBuilder PickupFrame()
        {
            var b = new MeshBuilder();
            const int sides = 6;
            b.Color(Swatch.Chrome);
            for (int i = 0; i < sides; i++)
            {
                float a0 = (i + 0.5f) * Mathf.PI * 2f / sides;
                float a1 = (i + 1.5f) * Mathf.PI * 2f / sides;
                var p0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)) * 0.62f;
                var p1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * 0.62f;
                b.Rod(p0, p1, 0.045f, 6, false);
            }
            b.Color(Swatch.GlowWhite);
            for (int i = 0; i < sides; i++)
            {
                float angle = (i + 0.5f) * Mathf.PI * 2f / sides;
                b.Sphere(new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 0.62f, 0.07f, 4, 6, false);
            }
            return b;
        }

        /// <summary>
        /// A chunk of ice: a jagged, faceted body in three shades with a few crystal spikes, about one unit in radius.
        /// </summary>
        public static MeshBuilder IceChunk(int seed)
        {
            var b = new MeshBuilder();
            var random = new System.Random(seed);
            b.Color(Swatch.Ice);
            FacetedBlob(b, new Vector3(0.85f, 0.78f, 0.82f), 1, 0.2f, seed, random, Swatch.IceLight, Swatch.Ice, Swatch.IceDeep);
            int spikes = 3 + random.Next(3);
            for (int i = 0; i < spikes; i++)
            {
                Vector3 direction = RandomDirection(random);
                float length = 0.35f + (float)random.NextDouble() * 0.35f;
                Spike(b, direction * 0.55f, direction, 0.12f + (float)random.NextDouble() * 0.08f, length, i % 2 == 0 ? Swatch.IceLight : Swatch.GlowIce, Swatch.IceLight);
            }
            return b;
        }

        /// <summary>A void crystal: a dark rock core bristling with glowing violet and magenta crystals.</summary>
        public static MeshBuilder CrystalRock(int seed)
        {
            var b = new MeshBuilder();
            var random = new System.Random(seed);
            FacetedBlob(b, new Vector3(0.7f, 0.65f, 0.68f), 1, 0.25f, seed, random, Swatch.CrystalCore, Swatch.Basalt, Swatch.CrystalDark);
            int spikes = 6 + random.Next(3);
            for (int i = 0; i < spikes; i++)
            {
                Vector3 direction = RandomDirection(random);
                float length = 0.45f + (float)random.NextDouble() * 0.45f;
                Swatch body = i % 3 == 0 ? Swatch.GlowMagenta : Swatch.CrystalDark;
                Swatch tip = i % 2 == 0 ? Swatch.GlowViolet : Swatch.GlowMagenta;
                Spike(b, direction * 0.45f, direction, 0.1f + (float)random.NextDouble() * 0.09f, length, body, tip);
            }
            return b;
        }

        /// <summary>A jittered, subdivided icosahedron whose faces get one of three shades.</summary>
        private static void FacetedBlob(MeshBuilder b, Vector3 radii, int subdivisions, float jitter, int seed, System.Random random,
            Swatch light, Swatch mid, Swatch dark)
        {
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

                int Middle(int a, int c)
                {
                    long key = a < c ? ((long)a << 32) + c : ((long)c << 32) + a;
                    if (cache.TryGetValue(key, out int existing))
                    {
                        return existing;
                    }
                    vertices.Add(((vertices[a] + vertices[c]) * 0.5f).normalized);
                    cache[key] = vertices.Count - 1;
                    return vertices.Count - 1;
                }

                for (int f = 0; f < faces.Count; f += 3)
                {
                    int a = faces[f];
                    int c = faces[f + 1];
                    int d = faces[f + 2];
                    int ac = Middle(a, c);
                    int cd = Middle(c, d);
                    int da = Middle(d, a);
                    next.AddRange(new[] { a, ac, da, c, cd, ac, d, da, cd, ac, cd, da });
                }
                faces = next;
            }
            var placed = new Vector3[vertices.Count];
            var shape = new System.Random(seed * 7 + 3);
            for (int i = 0; i < vertices.Count; i++)
            {
                float scale = 1f + ((float)shape.NextDouble() * 2f - 1f) * jitter;
                placed[i] = Vector3.Scale(vertices[i] * scale, radii);
            }
            for (int f = 0; f < faces.Count; f += 3)
            {
                int roll = random.Next(3);
                b.Color(roll == 0 ? light : roll == 1 ? mid : dark);
                b.Triangle(placed[faces[f]], placed[faces[f + 1]], placed[faces[f + 2]], Vector3.zero);
            }
        }

        /// <summary>A hexagonal crystal prism growing along <paramref name="direction"/> with a pointed tip.</summary>
        private static void Spike(MeshBuilder b, Vector3 root, Vector3 direction, float radius, float length, Swatch body, Swatch tip)
        {
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, direction);
            b.Push(root, rotation);
            b.Color(body);
            b.Cylinder(Vector3.zero, radius, radius * 0.85f, length, 6, false, false, false);
            b.Color(tip);
            b.Cylinder(new Vector3(0f, length, 0f), radius * 0.85f, 0f, radius * 1.6f, 6, false, false, false);
            b.Pop();
        }

        private static Vector3 RandomDirection(System.Random random)
        {
            float z = (float)random.NextDouble() * 2f - 1f;
            float angle = (float)random.NextDouble() * Mathf.PI * 2f;
            float r = Mathf.Sqrt(1f - z * z);
            return new Vector3(r * Mathf.Cos(angle), z, r * Mathf.Sin(angle));
        }

        /// <summary>A quad in the XY plane from y = 0 down to y = -1 (an engine flame hanging from its nozzle).</summary>
        public static Mesh FlameQuad()
        {
            var mesh = new Mesh { name = "FlameQuad" };
            mesh.SetVertices(new List<Vector3> { new Vector3(-0.5f, 0f, 0f), new Vector3(0.5f, 0f, 0f), new Vector3(0.5f, -1f, 0f), new Vector3(-0.5f, -1f, 0f) });
            mesh.SetUVs(0, new List<Vector2> { new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f), new Vector2(0f, 0f) });
            mesh.SetNormals(new List<Vector3> { Vector3.back, Vector3.back, Vector3.back, Vector3.back });
            mesh.SetTriangles(new[] { 0, 1, 2, 0, 2, 3 }, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>A flat ring (annulus) in the XZ plane with u running from the inner to the outer edge.</summary>
        public static Mesh Annulus(float inner, float outer, int segments)
        {
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var normals = new List<Vector3>();
            var triangles = new List<int>();
            for (int i = 0; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                vertices.Add(direction * inner);
                vertices.Add(direction * outer);
                uvs.Add(new Vector2(0f, i / (float)segments));
                uvs.Add(new Vector2(1f, i / (float)segments));
                normals.Add(Vector3.up);
                normals.Add(Vector3.up);
                if (i < segments)
                {
                    int a = i * 2;
                    triangles.AddRange(new[] { a, a + 2, a + 1, a + 1, a + 2, a + 3 });
                }
            }
            var mesh = new Mesh { name = "Annulus" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>A copy of <paramref name="source"/> centred on its bounds and scaled to a largest half extent of one.</summary>
        public static Mesh Normalized(Mesh source, string name, float extra = 1f)
        {
            Mesh mesh = Object.Instantiate(source);
            mesh.name = name;
            Bounds bounds = source.bounds;
            float scale = extra / Mathf.Max(0.0001f, Mathf.Max(bounds.extents.x, Mathf.Max(bounds.extents.y, bounds.extents.z)));
            Vector3[] vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = (vertices[i] - bounds.center) * scale;
            }
            mesh.vertices = vertices;
            mesh.RecalculateBounds();
            if (mesh.normals == null || mesh.normals.Length == 0)
            {
                mesh.RecalculateNormals();
            }
            mesh.RecalculateTangents();
            return mesh;
        }
    }
}
