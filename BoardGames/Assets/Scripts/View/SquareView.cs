using UnityEngine;

namespace Portfolio.Checkers
{
    /// <summary>
    /// One square of the 3D board: its face in the light or dark square material, a highlight quad over it that glows in
    /// a colour (the selected piece, the last move), and a dot in the middle that marks a square a move may end on.
    /// Both marks are tinted through a material property block on <c>_BaseColor</c>, so every square shares one highlight
    /// material.
    /// </summary>
    public sealed class SquareView : MonoBehaviour
    {
        [SerializeField] private int index;
        [SerializeField] private MeshRenderer face;
        [SerializeField] private MeshRenderer highlight;
        [SerializeField] private MeshRenderer dot;

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        private MaterialPropertyBlock block;
        private Vector3 dotScale = Vector3.zero;

        public int Index => index;

        public bool IsDark { get; private set; }

        /// <summary>Makes the square number <paramref name="index"/>, dark or light, in <paramref name="material"/> (kept when null).</summary>
        public void Set(int index, bool dark, Material material)
        {
            this.index = index;
            IsDark = dark;
            name = "Square " + index;
            if (face != null && material != null)
            {
                face.sharedMaterial = material;
            }
            Highlight(null, 1f);
            Dot(false);
        }

        /// <summary>The material of the highlight quad and the dot (an unlit transparent one the theme gives).</summary>
        public void SetHighlightMaterial(Material material)
        {
            if (material == null)
            {
                return;
            }
            if (highlight != null)
            {
                highlight.sharedMaterial = material;
            }
            if (dot != null)
            {
                dot.sharedMaterial = material;
            }
        }

        /// <summary>Glows in <paramref name="color"/> times <paramref name="strength"/>; null turns the glow off.</summary>
        public void Highlight(Color? color, float strength)
        {
            if (highlight == null)
            {
                return;
            }
            highlight.gameObject.SetActive(color.HasValue);
            if (color.HasValue)
            {
                Tint(highlight, color.Value, strength);
            }
        }

        /// <summary>Shows or hides the dot in the colour it had.</summary>
        public void Dot(bool on)
        {
            if (dot == null)
            {
                return;
            }
            dot.gameObject.SetActive(on);
        }

        /// <summary>Shows the dot in <paramref name="color"/>, <paramref name="scale"/> times its usual size.</summary>
        public void Dot(Color color, float scale, float strength = 1f)
        {
            if (dot == null)
            {
                return;
            }
            if (dotScale == Vector3.zero)
            {
                dotScale = dot.transform.localScale;
            }
            dot.transform.localScale = dotScale * scale;
            dot.gameObject.SetActive(true);
            Tint(dot, color, strength);
        }

        private void Tint(MeshRenderer renderer, Color color, float strength)
        {
            block ??= new MaterialPropertyBlock();
            Color c = color * strength;
            c.a = Mathf.Clamp01(color.a * strength);
            block.SetColor(BaseColor, c);
            renderer.SetPropertyBlock(block);
        }
    }
}
