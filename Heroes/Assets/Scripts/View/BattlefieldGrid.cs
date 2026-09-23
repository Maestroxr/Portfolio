using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Portfolio.Heroes
{
    /// <summary>
    /// The hexagons of a battlefield of its own, drawn as meshes just over the flat ground: thin soft edged lines for
    /// every cell, fills for the cells painted for the stack in hand (where it may go, what it may strike), and a bright
    /// ring round the cell under the pointer. The lines are built once; the fills and the ring are rebuilt only when
    /// what is painted changes.
    /// </summary>
    public sealed class BattlefieldGrid : MonoBehaviour
    {
        /// <summary>How high over the ground the grid floats, so the ground never shows through it.</summary>
        public const float Lift = 0.035f;

        private const float LineWidth = 0.07f;
        private const float MarkWidth = 0.14f;

        private readonly Dictionary<int, Color> painted = new Dictionary<int, Color>();
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<Color> colors = new List<Color>();
        private readonly List<Vector2> uvs = new List<Vector2>();
        private readonly List<int> triangles = new List<int>();
        private HexGrid field;
        private float radius;
        private MeshRenderer lines;
        private MeshRenderer fills;
        private MeshRenderer marks;
        private Mesh lineMesh;
        private Mesh fillMesh;
        private Mesh markMesh;
        private readonly List<Material> materials = new List<Material>();
        private Texture2D profile;
        private int marked = -1;
        private Color markColor;
        private bool dirty;

        /// <summary>
        /// Builds the lines of every cell of <paramref name="grid"/> (cells of <paramref name="size"/> meters, the corner of
        /// the grid at this object's origin), drawn with a copy of <paramref name="source"/> (a blended particle material).
        /// </summary>
        public void Setup(HexGrid grid, float size, Material source, Color lineColor)
        {
            field = grid;
            radius = size;
            profile = Profile();
            lines = Layer("Lines", source, 2950, out lineMesh);
            fills = Layer("Fills", source, 2951, out fillMesh);
            marks = Layer("Marks", source, 2952, out markMesh);
            BuildLines(lineColor);
        }

        public bool Visible
        {
            get => lines != null && lines.enabled;
            set
            {
                if (lines != null)
                {
                    lines.enabled = value;
                    fills.enabled = value;
                    marks.enabled = value;
                }
            }
        }

        /// <summary>The middle of a cell, on the ground (y 0 of this object).</summary>
        public Vector3 Center(int cell)
        {
            field.Center(cell, out double x, out double y);
            return transform.TransformPoint(new Vector3((float)x * radius, 0f, (float)y * radius));
        }

        public void Paint(int cell, Color color)
        {
            if (field == null || !field.Valid(cell))
            {
                return;
            }
            painted[cell] = color;
            dirty = true;
        }

        public void Unpaint(int cell)
        {
            if (painted.Remove(cell))
            {
                dirty = true;
            }
        }

        public void Clear()
        {
            if (painted.Count > 0)
            {
                painted.Clear();
                dirty = true;
            }
        }

        /// <summary>Rings a cell (-1: none) in <paramref name="color"/>.</summary>
        public void Mark(int cell, Color color)
        {
            if (cell == marked && color == markColor)
            {
                return;
            }
            marked = field != null && field.Valid(cell) ? cell : -1;
            markColor = color;
            BuildMark();
        }

        private void LateUpdate()
        {
            if (dirty)
            {
                dirty = false;
                BuildFills();
            }
        }

        // ------------------------------------------------------------------ meshes

        private MeshRenderer Layer(string name, Material source, int queue, out Mesh mesh)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(transform, false);
            mesh = new Mesh { name = $"Battle Grid {name}" };
            mesh.MarkDynamic();
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            Material material = source != null ? new Material(source) : new Material(Shader.Find("Sprites/Default"));
            material.name = $"Battle Grid {name}";
            material.mainTexture = profile;
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", profile);
            }
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", Color.white);
            }
            if (material.HasProperty("_Cull"))
            {
                // Seen from above whichever way the triangles wind.
                material.SetFloat("_Cull", 0f);
            }
            // Before every other see-through thing, so sparks and numbers are never painted over by a cell.
            material.renderQueue = queue;
            materials.Add(material);
            renderer.sharedMaterial = material;
            return renderer;
        }

        /// <summary>A strip of alpha across a line: solid in the middle, soft at both edges, so thin lines do not shimmer.</summary>
        private static Texture2D Profile()
        {
            const int size = 32;
            var texture = new Texture2D(4, size, TextureFormat.RGBA32, true) { name = "Battle Grid Line", wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[4 * size];
            for (int y = 0; y < size; y++)
            {
                float v = (y + 0.5f) / size;
                float edge = Mathf.Abs(v - 0.5f) * 2f;
                float alpha = Mathf.Clamp01((1f - edge) / 0.45f);
                alpha = alpha * alpha * (3f - 2f * alpha);
                for (int x = 0; x < 4; x++)
                {
                    pixels[y * 4 + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(true);
            return texture;
        }

        private Vector3 LocalCenter(int cell)
        {
            field.Center(cell, out double x, out double y);
            return new Vector3((float)x * radius, Lift, (float)y * radius);
        }

        private Vector3 Corner(Vector3 center, int k, float scale)
        {
            float angle = (30f + 60f * k) * Mathf.Deg2Rad;
            return center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (radius * scale);
        }

        private void BuildLines(Color color)
        {
            Begin();
            var seen = new HashSet<long>();
            for (int cell = 0; cell < field.Count; cell++)
            {
                Vector3 center = LocalCenter(cell);
                for (int k = 0; k < 6; k++)
                {
                    Vector3 a = Corner(center, k, 1f);
                    Vector3 b = Corner(center, k + 1, 1f);
                    Vector3 middle = (a + b) * 0.5f;
                    long key = (long)Mathf.RoundToInt(middle.x * 20f) * 1000003L + Mathf.RoundToInt(middle.z * 20f);
                    if (seen.Add(key))
                    {
                        Segment(a, b, LineWidth, color);
                    }
                }
            }
            Finish(lineMesh);
        }

        private void BuildFills()
        {
            Begin();
            foreach (KeyValuePair<int, Color> paint in painted)
            {
                Vector3 center = LocalCenter(paint.Key);
                Color inner = paint.Value;
                inner.a *= 0.55f;
                int first = vertices.Count;
                Add(center, inner, 0.5f);
                for (int k = 0; k < 6; k++)
                {
                    Add(Corner(center, k, 0.93f), paint.Value, 0.5f);
                }
                for (int k = 0; k < 6; k++)
                {
                    triangles.Add(first);
                    triangles.Add(first + 1 + (k + 1) % 6);
                    triangles.Add(first + 1 + k);
                }
            }
            Finish(fillMesh);
        }

        private void BuildMark()
        {
            Begin();
            if (marked >= 0)
            {
                Vector3 center = LocalCenter(marked) + Vector3.up * 0.005f;
                for (int k = 0; k < 6; k++)
                {
                    Segment(Corner(center, k, 0.9f), Corner(center, k + 1, 0.9f), MarkWidth, markColor, true);
                }
            }
            Finish(markMesh);
        }

        private void Begin()
        {
            vertices.Clear();
            colors.Clear();
            uvs.Clear();
            triangles.Clear();
        }

        private void Finish(Mesh mesh)
        {
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
        }

        private void Add(Vector3 position, Color color, float v)
        {
            vertices.Add(position);
            colors.Add(color);
            uvs.Add(new Vector2(0.5f, v));
        }

        /// <summary>A quad along a to b, <paramref name="width"/> across; <paramref name="joined"/> runs it past both ends so the corners of a ring close.</summary>
        private void Segment(Vector3 a, Vector3 b, float width, Color color, bool joined = false)
        {
            Vector3 along = (b - a).normalized;
            Vector3 across = Vector3.Cross(along, Vector3.up) * (width * 0.5f);
            if (joined)
            {
                a -= along * (width * 0.29f);
                b += along * (width * 0.29f);
            }
            int first = vertices.Count;
            Add(a - across, color, 0f);
            Add(a + across, color, 1f);
            Add(b + across, color, 1f);
            Add(b - across, color, 0f);
            triangles.Add(first);
            triangles.Add(first + 1);
            triangles.Add(first + 2);
            triangles.Add(first);
            triangles.Add(first + 2);
            triangles.Add(first + 3);
        }

        private void OnDestroy()
        {
            foreach (Material material in materials)
            {
                if (material != null)
                {
                    Destroy(material);
                }
            }
            foreach (Mesh mesh in new[] { lineMesh, fillMesh, markMesh })
            {
                if (mesh != null)
                {
                    Destroy(mesh);
                }
            }
            if (profile != null)
            {
                Destroy(profile);
            }
        }
    }
}
