using System.Collections;
using Gamebox;
using UnityEngine;

namespace Portfolio.BoardGames
{
    /// <summary>
    /// One piece on the 3D board: a disc in its side's material, a crown on top once it is a king, and a soft contact
    /// shadow under it. The board view moves it along a path, lifts it off the board when it is taken, and crowns it;
    /// the coroutines here are the steps of those animations.
    /// </summary>
    public sealed class PieceView : MonoBehaviour
    {
        [SerializeField] private MeshRenderer disc;
        [SerializeField] private GameObject crown;
        [SerializeField] private Transform shadow;

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        private MaterialPropertyBlock block;
        private Vector3 baseScale = Vector3.one;
        private bool scaleKnown;

        /// <summary>The piece shown, as the model codes it (<see cref="Pieces"/>).</summary>
        public sbyte Piece { get; private set; }

        /// <summary>The square the piece stands on (or is about to reach).</summary>
        public int Square { get; set; } = -1;

        public bool IsKing => Pieces.IsKing(Piece);

        /// <summary>Shows <paramref name="piece"/> in <paramref name="material"/> (kept when null) standing at <paramref name="at"/>.</summary>
        public void Show(sbyte piece, Material material, Vector3 at)
        {
            RememberScale();
            Piece = piece;
            name = Pieces.IsKing(piece) ? "King" : "Man";
            if (disc != null && material != null)
            {
                disc.sharedMaterial = material;
            }
            transform.position = at;
            transform.localScale = baseScale;
            if (shadow != null)
            {
                shadow.gameObject.SetActive(true);
                shadow.localScale = Vector3.one;
            }
            SetKing(Pieces.IsKing(piece));
        }

        /// <summary>The material of the crown (kept when null).</summary>
        public void SetCrownMaterial(Material material)
        {
            if (crown == null || material == null)
            {
                return;
            }
            foreach (MeshRenderer renderer in crown.GetComponentsInChildren<MeshRenderer>(true))
            {
                renderer.sharedMaterial = material;
            }
        }

        /// <summary>The material of the contact shadow (kept when null).</summary>
        public void SetShadowMaterial(Material material)
        {
            if (shadow == null || material == null)
            {
                return;
            }
            var renderer = shadow.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        /// <summary>Puts the crown on or takes it off; the piece code follows.</summary>
        public void SetKing(bool king)
        {
            if (crown != null)
            {
                crown.SetActive(king);
            }
            Side side = Pieces.SideOf(Piece);
            Piece = king ? Pieces.King(side) : Pieces.Man(side);
        }

        /// <summary>Slides to <paramref name="to"/> over <paramref name="seconds"/>, hopping <paramref name="hop"/> high on the way.</summary>
        public IEnumerator MoveTo(Vector3 to, float seconds, float hop)
        {
            Vector3 from = transform.position;
            yield return Tween.Run(seconds, t =>
            {
                float eased = Tween.OutCubic(t);
                transform.position = hop > 0f ? Tween.Arc(from, to, hop, eased) : Vector3.Lerp(from, to, eased);
            });
            transform.position = to;
        }

        /// <summary>Rises <paramref name="height"/> over <paramref name="seconds"/>, the shadow fading as it goes.</summary>
        public IEnumerator Lift(float height, float seconds)
        {
            Vector3 from = transform.position;
            Vector3 to = from + Vector3.up * height;
            yield return Tween.Run(seconds, t =>
            {
                float eased = Tween.OutQuad(t);
                transform.position = Vector3.Lerp(from, to, eased);
                ShadowAlpha(1f - eased);
            });
        }

        /// <summary>Shrinks away over <paramref name="seconds"/> (a taken piece leaves the board); the board recycles it after.</summary>
        public IEnumerator Fade(float seconds)
        {
            RememberScale();
            Vector3 from = transform.localScale;
            yield return Tween.Run(seconds, t =>
            {
                float eased = Tween.InQuad(t);
                transform.localScale = Vector3.Lerp(from, Vector3.zero, eased);
                ShadowAlpha(1f - eased);
            });
            if (shadow != null)
            {
                shadow.gameObject.SetActive(false);
            }
        }

        /// <summary>The crown pops onto the piece (a promotion).</summary>
        public IEnumerator Crown(float seconds)
        {
            SetKing(true);
            if (crown == null)
            {
                yield break;
            }
            Vector3 scale = crown.transform.localScale;
            yield return Tween.Run(seconds, t =>
            {
                crown.transform.localScale = scale * Tween.OutBack(t, 2.2f);
            });
            crown.transform.localScale = scale;
        }

        private void RememberScale()
        {
            if (!scaleKnown)
            {
                scaleKnown = true;
                baseScale = transform.localScale;
            }
        }

        private void ShadowAlpha(float alpha)
        {
            if (shadow == null)
            {
                return;
            }
            var renderer = shadow.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                return;
            }
            block ??= new MaterialPropertyBlock();
            Color c = renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty(BaseColor) ? renderer.sharedMaterial.GetColor(BaseColor) : Color.black;
            c.a *= Mathf.Clamp01(alpha);
            block.SetColor(BaseColor, c);
            renderer.SetPropertyBlock(block);
        }
    }
}
