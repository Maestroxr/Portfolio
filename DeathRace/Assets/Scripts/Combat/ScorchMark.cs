using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// A burnt patch on the ground where something blew up. It stays for a while, then fades away so the track does not
    /// end up black; the effects keep only so many and reuse the oldest.
    /// </summary>
    internal sealed class ScorchMark : MonoBehaviour
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static Mesh quad;
        private Renderer[] renderers = new Renderer[0];
        private Color[] colors = new Color[0];
        private MaterialPropertyBlock block;
        private Color color;
        private float age;
        private float life;
        private float fade;

        /// <summary>When it was placed (the oldest is reused first).</summary>
        public float Placed { get; private set; }

        public bool Busy => gameObject.activeSelf;

        public static ScorchMark Create(Transform parent, GameObject prefab, Material material)
        {
            GameObject go;
            if (prefab != null)
            {
                go = Instantiate(prefab, parent, false);
                go.name = "Scorch";
            }
            else
            {
                go = new GameObject("Scorch");
                go.transform.SetParent(parent, false);
                go.AddComponent<MeshFilter>().sharedMesh = Quad;
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            foreach (Collider collider in go.GetComponentsInChildren<Collider>(true))
            {
                DestroyImmediate(collider);
            }
            ScorchMark mark = go.AddComponent<ScorchMark>();
            mark.renderers = go.GetComponentsInChildren<Renderer>(true);
            // The mark keeps its material's own colour and only fades it.
            mark.colors = new Color[mark.renderers.Length];
            for (int i = 0; i < mark.renderers.Length; i++)
            {
                Material shared = mark.renderers[i].sharedMaterial;
                mark.colors[i] = shared == null ? Color.white : shared.HasProperty(BaseColorId) ? shared.GetColor(BaseColorId) : shared.HasProperty(ColorId) ? shared.color : Color.white;
            }
            go.SetActive(false);
            return mark;
        }

        /// <summary>A flat 1 m square facing up, its texture the right way round from above.</summary>
        private static Mesh Quad
        {
            get
            {
                if (quad == null)
                {
                    quad = new Mesh { name = "Ground Quad", hideFlags = HideFlags.DontSave };
                    quad.vertices = new[] { new Vector3(-0.5f, 0f, -0.5f), new Vector3(-0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, -0.5f) };
                    quad.uv = new[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) };
                    quad.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
                    quad.triangles = new[] { 0, 1, 2, 0, 2, 3 };
                    quad.RecalculateBounds();
                }
                return quad;
            }
        }

        public void Place(Vector3 position, float size, float yaw, Color tint, float seconds, float fadeSeconds)
        {
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            transform.localScale = new Vector3(size, 1f, size);
            color = tint;
            life = seconds;
            fade = Mathf.Max(0.1f, fadeSeconds);
            age = 0f;
            Placed = Time.time;
            gameObject.SetActive(true);
            Apply(0f);
        }

        private void Update()
        {
            age += Time.deltaTime;
            if (age >= life + fade)
            {
                gameObject.SetActive(false);
                return;
            }
            float alpha = age < 0.15f ? age / 0.15f : age > life ? 1f - (age - life) / fade : 1f;
            Apply(alpha);
        }

        private void Apply(float alpha)
        {
            block ??= new MaterialPropertyBlock();
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }
                Color shown = colors[i] * color;
                shown.a *= Mathf.Clamp01(alpha);
                renderer.GetPropertyBlock(block);
                block.SetColor(ColorId, shown);
                block.SetColor(BaseColorId, shown);
                renderer.SetPropertyBlock(block);
            }
        }
    }
}
