using System.Collections;
using System.Collections.Generic;
using Gamebox;
using UnityEngine;

namespace Portfolio.Monopoly
{
    /// <summary>
    /// A seat's token on the board: a pewter figure (one of the <see cref="MonopolyStyle.TokenNames"/>) on a base in the
    /// seat's colour. It hops from space to space, flies to jail, shows a ring while it is the seat's turn and topples
    /// when the seat goes bankrupt. The rules live in <see cref="MonopolyMatch"/>; this is the piece that moves.
    /// </summary>
    public class MonopolyPlayer : PlayerBase
    {
        [SerializeField] private Transform body;
        [SerializeField] private MeshFilter figure;
        [SerializeField] private Renderer baseRenderer;
        [SerializeField] private Renderer ring;
        [SerializeField] private Mesh[] tokenMeshes = new Mesh[0];
        [SerializeField] private Material[] seatMaterials = new Material[0];
        [Tooltip("Glowing (unlit) materials of the turn ring, by seat colour.")]
        [SerializeField] private Material[] ringMaterials = new Material[0];
        [SerializeField] private float hopHeight = 0.45f;
        [Tooltip("A soft round shadow under the token; it stays on the board while the token jumps.")]
        [SerializeField] private Transform shadow;
        [SerializeField] private float groundHeight = 0.3f;

        private int token;
        private int colorIndex;
        private bool facingRight = true;
        private bool myTurn;
        private float bobPhase;
        private Coroutine moving;

        public int Token => token;

        public bool IsMoving { get; private set; }

        /// <summary>Called by the manager for a new match or a loaded one: the figure, the seat colour of the base and ring.</summary>
        public void Configure(int tokenIndex, int color, bool visible)
        {
            token = Mathf.Clamp(tokenIndex, 0, Mathf.Max(0, tokenMeshes.Length - 1));
            colorIndex = color;
            if (figure != null && tokenMeshes.Length > 0)
            {
                figure.sharedMesh = tokenMeshes[token];
            }
            if (baseRenderer != null && colorIndex >= 0 && colorIndex < seatMaterials.Length)
            {
                baseRenderer.sharedMaterial = seatMaterials[colorIndex];
            }
            if (ring != null && colorIndex >= 0 && colorIndex < ringMaterials.Length)
            {
                ring.sharedMaterial = ringMaterials[colorIndex];
            }
            gameObject.SetActive(visible);
            if (body != null)
            {
                body.localPosition = Vector3.zero;
                body.localRotation = Quaternion.identity;
                body.localScale = Vector3.one;
            }
            SetTurn(false);
        }

        /// <summary>Puts the token on a spot at once.</summary>
        public void PlaceAt(Vector3 position)
        {
            StopMoving();
            transform.position = position;
            ApplyFacing();
        }

        public void SetTurn(bool value)
        {
            myTurn = value;
            if (ring != null)
            {
                ring.gameObject.SetActive(value);
            }
        }

        /// <summary>Hops through <paramref name="path"/> (one spot per space), <paramref name="hopTime"/> seconds a hop.</summary>
        public IEnumerator Hop(IList<Vector3> path, float hopTime, System.Action<int> landedOn = null)
        {
            StopMoving();
            IsMoving = true;
            for (int i = 0; i < path.Count; i++)
            {
                Vector3 from = transform.position;
                Vector3 to = path[i];
                Face(to - from);
                float time = 0f;
                while (time < hopTime)
                {
                    float t = time / hopTime;
                    transform.position = Tween.Arc(from, to, hopHeight, t);
                    SquashBody(t);
                    yield return null;
                    time += Time.deltaTime;
                }
                transform.position = to;
                SquashBody(1f);
                landedOn?.Invoke(i);
            }
            IsMoving = false;
        }

        /// <summary>A long flight to <paramref name="to"/> (to jail, or where a card sends the token).</summary>
        public IEnumerator Fly(Vector3 to, float duration, float height = 2.2f)
        {
            StopMoving();
            IsMoving = true;
            Vector3 from = transform.position;
            Face(to - from);
            float time = 0f;
            while (time < duration)
            {
                float t = Tween.InOutSine(time / duration);
                transform.position = Tween.Arc(from, to, height, t);
                if (body != null)
                {
                    body.localRotation = Quaternion.AngleAxis(360f * t, Vector3.up);
                }
                yield return null;
                time += Time.deltaTime;
            }
            transform.position = to;
            if (body != null)
            {
                body.localRotation = Quaternion.identity;
            }
            ApplyFacing();
            IsMoving = false;
        }

        /// <summary>Slides the token to another spot of the same space (when tokens make room for each other).</summary>
        public IEnumerator Shuffle(Vector3 to, float duration)
        {
            Vector3 from = transform.position;
            float time = 0f;
            while (time < duration)
            {
                transform.position = Vector3.Lerp(from, to, Tween.OutCubic(time / duration));
                yield return null;
                time += Time.deltaTime;
            }
            transform.position = to;
        }

        /// <summary>The token topples over and sinks into the board.</summary>
        public IEnumerator Topple()
        {
            SetTurn(false);
            if (body == null)
            {
                gameObject.SetActive(false);
                yield break;
            }
            float time = 0f;
            while (time < 1.1f)
            {
                float t = time / 1.1f;
                body.localRotation = Quaternion.AngleAxis(-90f * Tween.OutBack(Mathf.Min(1f, t * 1.6f), 1.2f), Vector3.forward);
                body.localPosition = new Vector3(0f, -0.6f * Tween.InQuad(Mathf.Max(0f, t - 0.5f) * 2f), 0f);
                yield return null;
                time += Time.deltaTime;
            }
            gameObject.SetActive(false);
        }

        private void Face(Vector3 direction)
        {
            // The camera looks along +z: the figures show their profile, so they only turn to face left or right.
            if (Mathf.Abs(direction.x) > 0.05f)
            {
                facingRight = direction.x > 0f;
            }
            ApplyFacing();
        }

        private void ApplyFacing()
        {
            transform.rotation = Quaternion.Euler(0f, facingRight ? 0f : 180f, 0f);
        }

        private void SquashBody(float t)
        {
            if (body == null)
            {
                return;
            }
            // Stretch in the air, squash on landing.
            float stretch = t < 0.85f ? 1f + 0.12f * Mathf.Sin(t / 0.85f * Mathf.PI) : 1f - 0.18f * Mathf.Sin((t - 0.85f) / 0.15f * Mathf.PI);
            body.localScale = new Vector3(1f / Mathf.Sqrt(stretch), stretch, 1f / Mathf.Sqrt(stretch));
        }

        private void StopMoving()
        {
            if (moving != null)
            {
                StopCoroutine(moving);
                moving = null;
            }
            IsMoving = false;
        }

        private void LateUpdate()
        {
            if (shadow == null)
            {
                return;
            }
            Vector3 p = transform.position;
            float height = Mathf.Max(0f, p.y - groundHeight) + (body != null ? body.localPosition.y : 0f);
            shadow.position = new Vector3(p.x, groundHeight + 0.004f, p.z);
            shadow.rotation = Quaternion.identity;
            float size = Mathf.Lerp(0.72f, 0.4f, Mathf.Clamp01(height / 2f));
            shadow.localScale = new Vector3(size, 1f, size);
        }

        private void Update()
        {
            if (body == null || IsMoving)
            {
                return;
            }
            if (myTurn)
            {
                // A gentle bob while the seat is to play.
                bobPhase += Time.deltaTime * 3.2f;
                body.localPosition = new Vector3(0f, 0.04f + 0.04f * Mathf.Sin(bobPhase), 0f);
                if (ring != null)
                {
                    ring.transform.localRotation = Quaternion.AngleAxis(Time.time * 60f, Vector3.up);
                }
            }
            else if (body.localPosition.y > 0f)
            {
                body.localPosition = Vector3.MoveTowards(body.localPosition, Vector3.zero, Time.deltaTime);
            }
        }
    }
}
