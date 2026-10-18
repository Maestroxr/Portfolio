using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The Ion Wave: a crescent that widens from <see cref="startRadius"/> to <see cref="Growth"/> over its flight (its hit
    /// radius and its picture together) and passes through as many targets as its pierce allows.
    /// </summary>
    public class WaveShot : Shot
    {
        [Header("Wave")]
        [Tooltip("Hit radius (m) at the start of the flight; the picture is drawn for it.")]
        [SerializeField] internal float startRadius = 0.4f;
        [Tooltip("Hit radius (m) at the end of the flight.")]
        [SerializeField] internal float growth = 1.5f;

        private Vector3 restScale = Vector3.one;
        private bool restKnown;

        /// <summary>The hit radius (m) the wave reaches by the end of its flight.</summary>
        public float Growth
        {
            get => growth;
            set => growth = Mathf.Max(startRadius, value);
        }


        /// <summary>The hit radius of a wave from <paramref name="start"/> to <paramref name="end"/> at <paramref name="share"/> (0 to 1) of its flight.</summary>
        public static float RadiusAt(float start, float end, float share)
        {
            // Quick at first, then slower: the crescent opens up as it leaves the gun.
            float left = 1f - Mathf.Clamp01(share);
            return Mathf.Lerp(start, end, 1f - left * left);
        }


        public override void OnSpawned()
        {
            if (!restKnown && visual != null)
            {
                restScale = visual.localScale;
                restKnown = true;
            }
            base.OnSpawned();
            Grow();
        }


        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);
            if (InPlay)
            {
                Grow();
            }
        }


        private void Grow()
        {
            radius = RadiusAt(startRadius, growth, Lifetime > 0f ? Age / Lifetime : 0f);
            if (visual != null)
            {
                // Wider much faster than deeper: a crescent, not a ball.
                float wide = radius / Mathf.Max(0.01f, startRadius);
                visual.localScale = Vector3.Scale(restScale, new Vector3(wide, Mathf.Sqrt(wide), 1f));
            }
        }


        protected override void OnDespawned()
        {
            base.OnDespawned();
            radius = startRadius;
            if (visual != null && restKnown)
            {
                visual.localScale = restScale;
            }
        }
    }
}
