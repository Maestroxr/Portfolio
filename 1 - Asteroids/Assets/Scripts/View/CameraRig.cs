using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The camera straight above the playfield. Places itself so that it shows exactly the playground's height, shakes
    /// with trauma (hits, explosions, the nova), punches in slightly for big moments and drives the post-processing
    /// pulses: chromatic aberration and a red vignette when the ship is hurt, a lens kick for the nova.
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        [SerializeField] internal Camera view;
        [SerializeField] internal Playground playground;
        [SerializeField] internal Volume volume;
        [SerializeField] internal float fieldOfView = 40f;
        [SerializeField] internal float maxShake = 0.45f;
        [SerializeField] internal float maxRoll = 1.2f;

        private float trauma;
        private float pulse;
        private float hurt;
        private float seed;
        private Vector3 home;
        private ChromaticAberration aberration;
        private Vignette vignette;
        private LensDistortion lens;
        private float baseAberration;
        private float baseVignette;
        private Color baseVignetteColor;
        private float placedAspect;

        public Camera View => view;


        private void Awake()
        {
            seed = Random.value * 100f;
            Place();
            if (volume != null && volume.profile != null)
            {
                // Instantiates a runtime copy of the profile, so the pulses never touch the asset.
                VolumeProfile profile = volume.profile;
                profile.TryGet(out aberration);
                profile.TryGet(out vignette);
                profile.TryGet(out lens);
                if (aberration != null)
                {
                    baseAberration = aberration.intensity.value;
                }
                if (vignette != null)
                {
                    baseVignette = vignette.intensity.value;
                    baseVignetteColor = vignette.color.value;
                }
            }
        }


        /// <summary>
        /// Puts the camera at the distance where the playfield fills its height. A playfield with a size of its own (a
        /// mission shared with other pilots) has to fit as a whole, so on a narrow screen the camera backs off further.
        /// </summary>
        public void Place()
        {
            if (view == null)
            {
                return;
            }
            float half = playground != null ? playground.HalfHeight : 10f;
            if (playground != null && playground.IsFixed)
            {
                Vector2 size = playground.HalfSize;
                half = Mathf.Max(size.y, size.x / Mathf.Max(0.1f, view.aspect));
            }
            placedAspect = view.aspect;
            view.orthographic = false;
            view.fieldOfView = fieldOfView;
            float distance = half / Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad);
            home = new Vector3(0f, 0f, -distance);
            view.transform.localPosition = home;
            view.transform.localRotation = Quaternion.identity;
        }


        /// <summary>Adds trauma (0 to 1); the shake grows with its square.</summary>
        public void Shake(float amount)
        {
            trauma = Mathf.Clamp01(trauma + amount);
        }


        /// <summary>A lens kick for the nova and other big moments.</summary>
        public void Pulse(float amount)
        {
            pulse = Mathf.Clamp01(pulse + amount);
        }


        /// <summary>The ship was hit: aberration and a red vignette.</summary>
        public void Hurt(float amount)
        {
            hurt = Mathf.Clamp01(hurt + amount);
        }


        private void LateUpdate()
        {
            if (view != null && playground != null && playground.IsFixed && !Mathf.Approximately(view.aspect, placedAspect))
            {
                // The window changed shape while the playfield cannot.
                Place();
            }
            float deltaTime = Time.unscaledDeltaTime;
            float time = Time.unscaledTime * 22f;
            float shake = trauma * trauma;
            if (view != null)
            {
                var offset = new Vector3(
                    (Mathf.PerlinNoise(seed, time) - 0.5f) * 2f,
                    (Mathf.PerlinNoise(seed + 7f, time) - 0.5f) * 2f,
                    0f) * (maxShake * shake);
                float roll = (Mathf.PerlinNoise(seed + 13f, time) - 0.5f) * 2f * maxRoll * shake;
                float punch = pulse * pulse * 0.6f;
                view.transform.localPosition = home + offset + new Vector3(0f, 0f, punch);
                view.transform.localRotation = Quaternion.Euler(0f, 0f, roll);
            }
            trauma = Mathf.Max(0f, trauma - deltaTime * 1.4f);
            pulse = Mathf.Max(0f, pulse - deltaTime * 1.8f);
            hurt = Mathf.Max(0f, hurt - deltaTime * 2.2f);
            if (aberration != null)
            {
                aberration.intensity.Override(Mathf.Clamp01(baseAberration + hurt * 0.8f + pulse * 0.6f));
            }
            if (vignette != null)
            {
                vignette.intensity.Override(Mathf.Clamp01(baseVignette + hurt * 0.25f));
                vignette.color.Override(Color.Lerp(baseVignetteColor, new Color(0.7f, 0f, 0.05f), hurt));
            }
            if (lens != null)
            {
                lens.intensity.Override(-0.35f * pulse * pulse);
            }
        }
    }
}
