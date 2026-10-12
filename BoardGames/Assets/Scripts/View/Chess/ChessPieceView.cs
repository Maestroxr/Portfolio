using System.Collections;
using Gamebox;
using UnityEngine;

namespace Portfolio.BoardGames
{
    /// <summary>
    /// One chess piece on the 3D board: the mesh of its kind with the body and trim materials of its side, a soft
    /// contact shadow that stays on the board while the piece hops, and a glow around its foot when a move may take it.
    /// Pieces face the player of their side (the knights look to that player's left, a little towards them). The
    /// coroutines here are the steps of the animations the pieces view plays: a slide or a hop, the lift and fade of a
    /// taken piece, the pop of a promotion.
    /// </summary>
    public sealed class ChessPieceView : MonoBehaviour
    {
        [SerializeField] private MeshFilter filter;
        [SerializeField] private MeshRenderer body;
        [SerializeField] private Transform shadow;
        [Tooltip("A quad under the piece in the highlight material, tinted when a move may take the piece (inactive otherwise).")]
        [SerializeField] private MeshRenderer glow;

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        /// <summary>The body and trim materials handed to the renderer (one array, reused).</summary>
        private readonly Material[] materials = new Material[2];
        private MaterialPropertyBlock block;
        private MaterialPropertyBlock glowBlock;
        private ChessLook look;

        /// <summary>The piece shown, as the model codes it (<see cref="ChessPieces"/>).</summary>
        public sbyte Piece { get; private set; }

        /// <summary>The square the piece stands on (or is about to reach).</summary>
        public int Square { get; set; } = -1;

        public ChessPieceKind Kind => ChessPieces.KindOf(Piece);

        public Side Side => ChessPieces.SideOf(Piece);

        /// <summary>
        /// Takes the meshes and materials of <paramref name="look"/>, the contact shadow's and the glow's material (each
        /// kept when null), for what it shows now and later.
        /// </summary>
        public void SetLook(ChessLook look, Material shadowMaterial, Material glowMaterial)
        {
            this.look = look ?? this.look;
            Dress();
            if (shadow != null && shadowMaterial != null)
            {
                var renderer = shadow.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = shadowMaterial;
                }
            }
            if (glow != null && glowMaterial != null)
            {
                glow.sharedMaterial = glowMaterial;
            }
        }

        /// <summary>Shows <paramref name="piece"/> standing at <paramref name="at"/> on a board turned by <paramref name="boardRotation"/>, without a glow.</summary>
        public void Show(sbyte piece, Vector3 at, Quaternion boardRotation)
        {
            Piece = piece;
            name = (Side == Side.Light ? "White " : "Black ") + ChessPieces.NameKey(Kind);
            Dress();
            transform.SetPositionAndRotation(at, boardRotation * Facing());
            transform.localScale = Vector3.one;
            if (shadow != null)
            {
                shadow.gameObject.SetActive(true);
                shadow.localPosition = new Vector3(0f, 0.003f, 0f);
                shadow.localScale = ShadowScale;
                ShadowAlpha(1f);
            }
            Glow(null, 1f);
        }

        /// <summary>Glows around the foot in <paramref name="color"/> times <paramref name="strength"/>; null turns the glow off.</summary>
        public void Glow(Color? color, float strength)
        {
            if (glow == null)
            {
                return;
            }
            glow.gameObject.SetActive(color.HasValue);
            if (!color.HasValue)
            {
                return;
            }
            glowBlock ??= new MaterialPropertyBlock();
            Color c = color.Value * strength;
            c.a = Mathf.Clamp01(color.Value.a * strength);
            glowBlock.SetColor(BaseColor, c);
            glow.SetPropertyBlock(glowBlock);
        }

        /// <summary>
        /// Whether <paramref name="ray"/> meets the piece (a cylinder a little thinner than its foot, as tall as the piece),
        /// and how far along the ray.
        /// </summary>
        public bool Hit(Ray ray, out float distance)
        {
            distance = 0f;
            Mesh mesh = filter != null ? filter.sharedMesh : null;
            float height = mesh != null ? mesh.bounds.max.y : 0.6f;
            float radius = (mesh != null ? Mathf.Max(mesh.bounds.extents.x, mesh.bounds.extents.z) : 0.3f) * 0.6f;
            Vector3 o = transform.InverseTransformPoint(ray.origin);
            Vector3 d = transform.InverseTransformDirection(ray.direction);
            float enter = float.NegativeInfinity;
            float exit = float.PositiveInfinity;
            float a = d.x * d.x + d.z * d.z;
            float c = o.x * o.x + o.z * o.z - radius * radius;
            if (a < 1e-8f)
            {
                if (c > 0f)
                {
                    return false;
                }
            }
            else
            {
                float b = 2f * (o.x * d.x + o.z * d.z);
                float discriminant = b * b - 4f * a * c;
                if (discriminant < 0f)
                {
                    return false;
                }
                float root = Mathf.Sqrt(discriminant);
                enter = (-b - root) / (2f * a);
                exit = (-b + root) / (2f * a);
            }
            if (Mathf.Abs(d.y) < 1e-8f)
            {
                if (o.y < 0f || o.y > height)
                {
                    return false;
                }
            }
            else
            {
                float t0 = -o.y / d.y;
                float t1 = (height - o.y) / d.y;
                enter = Mathf.Max(enter, Mathf.Min(t0, t1));
                exit = Mathf.Min(exit, Mathf.Max(t0, t1));
            }
            if (enter > exit || exit < 0f)
            {
                return false;
            }
            distance = Mathf.Max(enter, 0f);
            return true;
        }

        /// <summary>
        /// Slides to <paramref name="to"/> over <paramref name="seconds"/>, rising <paramref name="hop"/> on the way (a
        /// knight's jump); the shadow stays on the board, smaller and fainter the higher the piece is.
        /// </summary>
        public IEnumerator MoveTo(Vector3 to, float seconds, float hop)
        {
            Vector3 from = transform.position;
            yield return Tween.Run(seconds, t =>
            {
                float eased = hop > 0.2f ? Tween.InOutCubic(t) : Tween.OutCubic(t);
                Vector3 point = hop > 0f ? Tween.Arc(from, to, hop, eased) : Vector3.Lerp(from, to, eased);
                transform.position = point;
                KeepShadowDown(point.y - Mathf.Lerp(from.y, to.y, eased));
            });
            transform.position = to;
            KeepShadowDown(0f);
        }

        /// <summary>Rises <paramref name="height"/> over <paramref name="seconds"/>, the shadow fading as it goes.</summary>
        public IEnumerator Lift(float height, float seconds)
        {
            Glow(null, 1f);
            Vector3 from = transform.position;
            Vector3 to = from + Vector3.up * height;
            yield return Tween.Run(seconds, t =>
            {
                float eased = Tween.OutQuad(t);
                transform.position = Vector3.Lerp(from, to, eased);
                KeepShadowDown(transform.position.y - from.y);
                ShadowAlpha(1f - eased);
            });
        }

        /// <summary>Shrinks away over <paramref name="seconds"/> (a taken piece leaves the board); the view recycles it after.</summary>
        public IEnumerator Fade(float seconds)
        {
            Glow(null, 1f);
            Vector3 from = transform.localScale;
            yield return Tween.Run(seconds, t =>
            {
                transform.localScale = Vector3.Lerp(from, Vector3.zero, Tween.InQuad(t));
            });
            if (shadow != null)
            {
                shadow.gameObject.SetActive(false);
            }
        }

        /// <summary>A promotion: the pawn shrinks into the board and <paramref name="piece"/> pops up in its place.</summary>
        public IEnumerator Promote(sbyte piece, float seconds)
        {
            Vector3 scale = Vector3.one;
            yield return Tween.Run(seconds * 0.35f, t =>
            {
                transform.localScale = new Vector3(1f + 0.15f * t, 1f - t, 1f + 0.15f * t);
            });
            Piece = piece;
            name = (Side == Side.Light ? "White " : "Black ") + ChessPieces.NameKey(Kind);
            Dress();
            transform.rotation = transform.parent != null ? transform.parent.rotation * Facing() : Facing();
            yield return Tween.Run(seconds * 0.65f, t =>
            {
                float pop = Tween.OutBack(t, 2.4f);
                transform.localScale = new Vector3(scale.x, scale.y * pop, scale.z);
            });
            transform.localScale = scale;
        }

        /// <summary>The turn of the piece on the board: towards the player of its side; a knight shows its profile, looking to that player's left.</summary>
        private Quaternion Facing()
        {
            float turn = look != null ? look.knightTurn : 18f;
            if (Kind == ChessPieceKind.Knight)
            {
                // The knight's mesh looks along +x: 180 turns it to White's left, 0 to Black's left.
                return Quaternion.Euler(0f, (Side == Side.Light ? 180f : 0f) - turn, 0f);
            }
            return Quaternion.Euler(0f, Side == Side.Light ? 0f : 180f, 0f);
        }

        private Vector3 ShadowScale
        {
            get
            {
                Mesh mesh = filter != null ? filter.sharedMesh : null;
                float radius = mesh != null ? Mathf.Max(0.2f, Mathf.Min(mesh.bounds.extents.x, mesh.bounds.extents.z)) : 0.3f;
                float size = radius * 2.6f;
                return new Vector3(size, 1f, size);
            }
        }

        private void Dress()
        {
            if (look == null)
            {
                return;
            }
            Mesh mesh = look.MeshOf(Kind);
            if (filter != null && mesh != null)
            {
                filter.sharedMesh = mesh;
            }
            if (body != null)
            {
                Material bodyMaterial = look.BodyOf(Side);
                if (bodyMaterial != null)
                {
                    materials[0] = bodyMaterial;
                    materials[1] = look.TrimOf(Side);
                    body.sharedMaterials = materials;
                }
            }
            if (shadow != null)
            {
                shadow.localScale = ShadowScale;
            }
        }

        /// <summary>Keeps the shadow on the board while the piece is <paramref name="lift"/> above it.</summary>
        private void KeepShadowDown(float lift)
        {
            if (shadow == null)
            {
                return;
            }
            lift = Mathf.Max(0f, lift);
            shadow.localPosition = new Vector3(0f, 0.003f - lift, 0f);
            shadow.localScale = ShadowScale * (1f + lift * 0.6f);
            ShadowAlpha(1f / (1f + lift * 4f));
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
