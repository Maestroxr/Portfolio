using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Portfolio.Heroes
{
    /// <summary>
    /// The shroud over the land a player has not explored yet. A texture holds what is hidden (a few texels a cell,
    /// blurred at the edges) and a quad in front of the camera draws it over the scene by the world position of every
    /// pixel (shader Heroes/FogOfWar), so tall things in the dark are hidden where they stand.
    /// </summary>
    public sealed class FogOfWar : MonoBehaviour
    {
        public const int Layer = 2;

        [SerializeField] private Material material;
        [SerializeField] private float texel = 0.5f;

        private Texture2D texture;
        private Texture2D noise;
        private Color32[] pixels;
        private byte[] hidden;
        private byte[] blurred;
        private int width;
        private int height;
        private HexLayout layout;
        private MeshRenderer quad;
        private Material instance;
        private bool dirty;
        private PlayerState player;
        private readonly HashSet<int> lifted = new HashSet<int>();

        public bool Visible
        {
            get => quad != null && quad.gameObject.activeSelf;
            set
            {
                if (quad != null)
                {
                    quad.gameObject.SetActive(value);
                }
            }
        }

        /// <summary>Lays the fog over the map. The material comes from the art, since nothing puts it in the scene.</summary>
        public void Setup(HexLayout mapLayout, Camera view, Material fog)
        {
            if (fog != null)
            {
                material = fog;
            }
            layout = mapLayout;
            width = Mathf.CeilToInt(layout.TerrainSize.x / texel);
            height = Mathf.CeilToInt(layout.TerrainSize.y / texel);
            if (texture != null)
            {
                Destroy(texture);
            }
            texture = new Texture2D(width, height, TextureFormat.R8, false, true)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                name = "Fog of War"
            };
            pixels = new Color32[width * height];
            hidden = new byte[width * height];
            blurred = new byte[width * height];
            if (noise == null)
            {
                noise = MakeNoise(128);
            }
            if (instance == null && material != null)
            {
                instance = new Material(material);
            }
            if (instance != null)
            {
                instance.SetTexture("_FogTex", texture);
                instance.SetTexture("_NoiseTex", noise);
                instance.SetVector("_FogRect", new Vector4(0f, 0f, 1f / (width * texel), 1f / (height * texel)));
            }
            if (instance == null)
            {
                // Without a material the screen would be covered by an untextured quad.
                Debug.LogWarning("Heroes: the fog of war has no material; the map is shown without it.");
                return;
            }
            if (quad == null)
            {
                var go = new GameObject("Fog of War", typeof(MeshFilter), typeof(MeshRenderer)) { layer = Layer };
                go.transform.SetParent(view.transform, false);
                go.transform.localPosition = new Vector3(0f, 0f, view.nearClipPlane + 0.05f);
                go.GetComponent<MeshFilter>().sharedMesh = ScreenQuad();
                quad = go.GetComponent<MeshRenderer>();
                quad.shadowCastingMode = ShadowCastingMode.Off;
                quad.receiveShadows = false;
                quad.lightProbeUsage = LightProbeUsage.Off;
                quad.reflectionProbeUsage = ReflectionProbeUsage.Off;
            }
            quad.sharedMaterial = instance;
            dirty = true;
        }

        private static Mesh ScreenQuad()
        {
            var mesh = new Mesh { name = "Fog Quad" };
            mesh.vertices = new[] { new Vector3(-1f, -1f, 0f), new Vector3(1f, -1f, 0f), new Vector3(1f, 1f, 0f), new Vector3(-1f, 1f, 0f) };
            mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            // Never culled: the shader places it over the screen, not where the mesh is.
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100000f);
            return mesh;
        }

        private static Texture2D MakeNoise(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.R8, false, true) { wrapMode = TextureWrapMode.Repeat, name = "Fog Clouds" };
            var data = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Tiling noise: sample a torus of Perlin noise.
                    float u = x / (float)size * Mathf.PI * 2f;
                    float v = y / (float)size * Mathf.PI * 2f;
                    float n = Mathf.PerlinNoise(Mathf.Cos(u) * 1.5f + 5f, Mathf.Sin(u) * 1.5f + Mathf.Cos(v) * 1.5f + 5f) * 0.65f
                              + Mathf.PerlinNoise(Mathf.Sin(v) * 3f + 11f, Mathf.Cos(u) * 3f + 17f) * 0.35f;
                    byte b = (byte)Mathf.Clamp(n * 255f, 0f, 255f);
                    data[y * size + x] = new Color32(b, b, b, 255);
                }
            }
            tex.SetPixels32(data);
            tex.Apply(false, true);
            return tex;
        }

        /// <summary>Shows what <paramref name="viewer"/> has explored (null: everything, for the end of a game).</summary>
        public void Show(PlayerState viewer)
        {
            player = viewer;
            dirty = true;
        }

        /// <summary>The player whose explored land the shroud leaves clear (null: everything).</summary>
        public PlayerState Viewer => player;

        /// <summary>
        /// Clears the shroud over <paramref name="cells"/> whoever is looking, whatever they have explored: the field of a
        /// battle fought on the map, whose stacks line up at its edges past where their hero has seen. Null puts the
        /// shroud back over them.
        /// </summary>
        public void Lift(IEnumerable<int> cells)
        {
            lifted.Clear();
            if (cells != null)
            {
                foreach (int cell in cells)
                {
                    lifted.Add(cell);
                }
            }
            dirty = true;
        }

        public void MarkDirty()
        {
            dirty = true;
        }

        /// <summary>Whether the shroud lies over a point of the map as it is drawn now (for the development tours).</summary>
        public bool Shrouds(Vector3 world)
        {
            if (hidden == null)
            {
                return false;
            }
            int x = Mathf.FloorToInt(world.x / texel);
            int y = Mathf.FloorToInt(world.z / texel);
            return x >= 0 && y >= 0 && x < width && y < height && hidden[y * width + x] > 127;
        }

        private void LateUpdate()
        {
            if (dirty && texture != null)
            {
                dirty = false;
                Rebuild();
            }
        }

        private void Rebuild()
        {
            for (int y = 0; y < height; y++)
            {
                float z = (y + 0.5f) * texel;
                for (int x = 0; x < width; x++)
                {
                    float wx = (x + 0.5f) * texel;
                    int cell = layout.CellAt(new Vector3(wx, 0f, z));
                    byte value;
                    if (cell < 0)
                    {
                        value = 0;
                    }
                    else
                    {
                        value = player == null || player.Explored(cell) || lifted.Contains(cell) ? (byte)0 : (byte)255;
                    }
                    hidden[y * width + x] = value;
                }
            }
            // Two passes of a small blur: the shroud's edge is soft, like clouds.
            Blur(hidden, blurred);
            Blur(blurred, hidden);
            for (int i = 0; i < hidden.Length; i++)
            {
                pixels[i] = new Color32(hidden[i], 0, 0, 255);
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
        }

        private void Blur(byte[] from, byte[] to)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int sum = 0;
                    int count = 0;
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int yy = y + dy;
                        if (yy < 0 || yy >= height)
                        {
                            continue;
                        }
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int xx = x + dx;
                            if (xx < 0 || xx >= width)
                            {
                                continue;
                            }
                            sum += from[yy * width + xx];
                            count++;
                        }
                    }
                    to[y * width + x] = (byte)(sum / count);
                }
            }
        }

        private void OnDestroy()
        {
            if (texture != null)
            {
                Destroy(texture);
            }
            if (noise != null)
            {
                Destroy(noise);
            }
            if (instance != null)
            {
                Destroy(instance);
            }
            // The quad hangs under the camera, not under the map, so it would outlive the map it covered.
            if (quad != null)
            {
                MeshFilter filter = quad.GetComponent<MeshFilter>();
                if (filter != null && filter.sharedMesh != null)
                {
                    Destroy(filter.sharedMesh);
                }
                Destroy(quad.gameObject);
            }
        }
    }
}
