using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The shadow an air unit or a ship casts on the ground (on a "Shadow" child: a flattened copy of the model in the
    /// theme's shadow colour). Hidden unless the terrain is shown; placed at the body's position plus
    /// <see cref="StrikeRules.ShadowOffset"/> on the ground depth, turned like the visual. The theme's colour comes from a
    /// material shared by every shadow of that theme, not from a property block: a renderer with a block drops out of
    /// the SRP Batcher.
    /// </summary>
    public class DropShadow : MonoBehaviour
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        /// <summary>The runtime copies of the shadow materials, one per material and colour (a handful per theme).</summary>
        private static readonly Dictionary<(Material, Color), Material> Tinted = new Dictionary<(Material, Color), Material>();

        [Tooltip("The model whose turn the shadow copies; the parent when empty.")]
        [SerializeField] internal Transform visual;
        [Tooltip("The shadow's renderers (shown only over the terrain).")]
        [SerializeField] internal Renderer[] renderers = new Renderer[0];

        private SpaceBody body;
        private AsteroidsPlayer player;
        private Material[] sources;
        private Color tint = new Color(-1f, 0f, 0f, 0f);
        private bool visible = true;


        private void Awake()
        {
            body = GetComponentInParent<SpaceBody>();
            player = body == null ? GetComponentInParent<AsteroidsPlayer>() : null;
        }


        private void OnEnable()
        {
            // Nothing shows until the first placement decides.
            SetVisible(false);
        }


        private void LateUpdate()
        {
            SpaceField field = body != null ? body.Field : player != null ? player.Field : null;
            StrikeTerrain terrain = field != null ? field.Terrain : null;
            Transform owner = transform.parent;
            Transform model = visual != null ? visual : owner;
            bool show = terrain != null && terrain.IsShown && owner != null && (model == null || model.gameObject.activeInHierarchy) &&
                (player == null || player.IsAlive);
            SetVisible(show);
            if (!show)
            {
                return;
            }
            Vector3 position = owner.position;
            DepthLayer.Place(transform, new Vector2(position.x, position.y) + StrikeRules.ShadowOffset, StrikeRules.GroundDepth);
            if (model != null)
            {
                transform.rotation = model.rotation;
            }
            Tint(terrain.ShadowColor);
        }


        private void SetVisible(bool show)
        {
            if (visible == show)
            {
                return;
            }
            visible = show;
            foreach (Renderer shadow in renderers)
            {
                if (shadow != null)
                {
                    shadow.enabled = show;
                }
            }
        }


        private void Tint(Color color)
        {
            if (color == tint)
            {
                return;
            }
            tint = color;
            if (sources == null)
            {
                sources = new Material[renderers.Length];
                for (int i = 0; i < renderers.Length; i++)
                {
                    sources[i] = renderers[i] != null ? renderers[i].sharedMaterial : null;
                }
            }
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && sources[i] != null)
                {
                    renderers[i].sharedMaterial = TintedMaterial(sources[i], color);
                }
            }
        }


        /// <summary>
        /// The copy of <paramref name="source"/> in <paramref name="color"/>, made once and shared by every shadow that asks
        /// for it. The source material (an asset) is never changed.
        /// </summary>
        internal static Material TintedMaterial(Material source, Color color)
        {
            if (source == null)
            {
                return null;
            }
            if (Tinted.TryGetValue((source, color), out Material tinted) && tinted != null)
            {
                return tinted;
            }
            tinted = new Material(source) { name = $"{source.name} (tinted)" };
            if (tinted.HasProperty(BaseColor))
            {
                tinted.SetColor(BaseColor, color);
            }
            if (tinted.HasProperty(ColorId))
            {
                tinted.SetColor(ColorId, color);
            }
            Tinted[(source, color)] = tinted;
            return tinted;
        }
    }
}
