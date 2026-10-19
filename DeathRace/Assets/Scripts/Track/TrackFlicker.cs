using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>Makes a fire's light breathe and flicker (Perlin noise, each fire on its own phase).</summary>
    [RequireComponent(typeof(Light))]
    public sealed class TrackFlicker : MonoBehaviour
    {
        [SerializeField] private float baseIntensity = 3f;
        [SerializeField] private float amount = 0.35f;
        [SerializeField] private float speed = 7f;

        private Light glow;
        private float phase;

        public void Setup(float intensity, float seed)
        {
            baseIntensity = intensity;
            phase = seed;
        }

        private void Awake()
        {
            glow = GetComponent<Light>();
        }

        private void Update()
        {
            float noise = Mathf.PerlinNoise(phase, Time.time * speed) * 2f - 1f;
            glow.intensity = baseIntensity * (1f + noise * amount);
        }
    }
}
