using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// One bolt of lightning between two points (a jump of the Arc Caster's or the arc lightning's): a jagged line with a
    /// wide soft glow under it, struck again with new kinks every few hundredths of a second, fading out in a fraction of a
    /// second, then back to its pool.
    /// </summary>
    public class LightningArc : MonoBehaviour
    {
        [Tooltip("The bright core of the bolt.")]
        [SerializeField] internal LineRenderer core;
        [Tooltip("A wider, dimmer line under the core.")]
        [SerializeField] internal LineRenderer glow;
        [SerializeField] internal float duration = 0.2f;
        [Tooltip("Largest sideways kink of the bolt, as a share of its length (at most 0.6 m).")]
        [SerializeField] internal float jag = 0.18f;
        [SerializeField] internal float coreWidth = 0.14f;
        [SerializeField] internal float glowWidth = 0.6f;
        [Tooltip("Seconds between two strikes with new kinks.")]
        [SerializeField] internal float restrike = 0.05f;
        [Tooltip("Depth of the lines (negative is toward the camera).")]
        [SerializeField] internal float depth = -0.15f;

        private Vector2 from;
        private Vector2 to;
        private Color tint = Color.white;
        private float age;
        private float sinceStrike;
        private int seed;

        internal LightningPool Pool { get; set; }


        /// <summary>Strikes from <paramref name="start"/> to <paramref name="end"/> in <paramref name="color"/>.</summary>
        public void Strike(Vector2 start, Vector2 end, Color color)
        {
            from = start;
            to = end;
            tint = color;
            age = 0f;
            sinceStrike = 0f;
            seed = Random.Range(0, 10000);
            transform.position = new Vector3(start.x, start.y, depth);
            Draw(1f);
        }


        private void Update()
        {
            age += Time.deltaTime;
            if (age >= duration)
            {
                if (Pool != null)
                {
                    Pool.Recycle(this);
                }
                else
                {
                    gameObject.SetActive(false);
                }
                return;
            }
            sinceStrike += Time.deltaTime;
            if (sinceStrike >= restrike)
            {
                sinceStrike = 0f;
                seed++;
            }
            float fade = 1f - age / duration;
            Draw(fade * fade);
        }


        private void Draw(float alpha)
        {
            Vector2 along = to - from;
            float length = along.magnitude;
            Vector2 side = length > 0.001f ? new Vector2(-along.y, along.x) / length : Vector2.right;
            float kink = Mathf.Min(0.6f, length * jag);
            // The core is the tint lightened toward white, opaque enough to show over bright ground; the glow is the tint.
            Shape(core, along, side, kink, coreWidth, new Color(Mathf.Lerp(tint.r, 1f, 0.4f), Mathf.Lerp(tint.g, 1f, 0.4f), Mathf.Lerp(tint.b, 1f, 0.4f), alpha));
            Shape(glow, along, side, kink, glowWidth, new Color(tint.r, tint.g, tint.b, alpha * 0.55f));
        }


        private void Shape(LineRenderer line, Vector2 along, Vector2 side, float kink, float width, Color color)
        {
            if (line == null)
            {
                return;
            }
            int points = Mathf.Max(2, line.positionCount);
            line.useWorldSpace = true;
            for (int i = 0; i < points; i++)
            {
                float t = i / (float)(points - 1);
                // The ends stay put; the kinks are biggest in the middle.
                float offset = i == 0 || i == points - 1 ? 0f : (Hash(seed, i) * 2f - 1f) * kink * Mathf.Sin(t * Mathf.PI);
                Vector2 point = from + along * t + side * offset;
                line.SetPosition(i, new Vector3(point.x, point.y, depth));
            }
            line.widthMultiplier = width;
            line.startColor = color;
            line.endColor = color;
            line.enabled = true;
        }


        private static float Hash(int seed, int index)
        {
            uint h = (uint)(seed * 73856093) ^ (uint)(index * 19349663);
            h ^= h >> 13;
            h *= 0x5bd1e995;
            h ^= h >> 15;
            return (h & 0xFFFF) / 65535f;
        }
    }
}
