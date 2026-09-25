using Gamebox;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// A pooled visual effect: particle systems (sized, sped up and tinted per use), an optional light flash and an
    /// optional expanding ring. Goes back to its pool when its duration is over.
    /// </summary>
    public class PooledEffect : MonoBehaviour
    {
        [SerializeField] internal ParticleSystem[] systems = new ParticleSystem[0];
        [Tooltip("Systems whose start colour follows the tint of each use.")]
        [SerializeField] internal ParticleSystem[] tinted = new ParticleSystem[0];
        [SerializeField] internal Light flash;
        [SerializeField] internal float flashIntensity = 8f;
        [SerializeField] internal float flashTime = 0.3f;
        [SerializeField] internal Renderer ring;
        [SerializeField] internal float ringTime = 0.5f;
        [SerializeField] internal float ringGrowth = 1f;
        [SerializeField] internal float duration = 2f;
        [Tooltip("Whether bigger uses also make faster particles (explosions) or only bigger ones (sparks).")]
        [SerializeField] internal bool scaleSpeed = true;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private float[] baseSizes;
        private float[] baseSpeeds;
        private float[] baseRadii;
        private float age;
        private float scale = 1f;
        private float flashRange;
        private Color tint = Color.white;
        private MaterialPropertyBlock block;

        internal EffectPool Pool { get; set; }

        public bool Playing => gameObject.activeSelf && age < duration;


        private void Awake()
        {
            int count = systems.Length;
            baseSizes = new float[count];
            baseSpeeds = new float[count];
            baseRadii = new float[count];
            for (int i = 0; i < count; i++)
            {
                if (systems[i] == null)
                {
                    continue;
                }
                ParticleSystem.MainModule main = systems[i].main;
                baseSizes[i] = main.startSizeMultiplier;
                baseSpeeds[i] = main.startSpeedMultiplier;
                ParticleSystem.ShapeModule shape = systems[i].shape;
                baseRadii[i] = shape.enabled ? shape.radius : 0f;
            }
            if (flash != null)
            {
                flashRange = flash.range;
            }
        }


        /// <summary>Plays the effect at <paramref name="position"/>, <paramref name="size"/> times its authored size.</summary>
        public void Play(Vector3 position, float size, Color color)
        {
            transform.position = position;
            transform.rotation = Quaternion.identity;
            age = 0f;
            scale = Mathf.Max(0.05f, size);
            tint = color;
            for (int i = 0; i < systems.Length; i++)
            {
                ParticleSystem system = systems[i];
                if (system == null)
                {
                    continue;
                }
                system.Clear(false);
                ParticleSystem.MainModule main = system.main;
                main.startSizeMultiplier = baseSizes[i] * scale;
                main.startSpeedMultiplier = baseSpeeds[i] * (scaleSpeed ? Mathf.Sqrt(scale) : 1f);
                if (baseRadii[i] > 0f)
                {
                    ParticleSystem.ShapeModule shape = system.shape;
                    shape.radius = baseRadii[i] * scale;
                }
            }
            foreach (ParticleSystem system in tinted)
            {
                if (system == null)
                {
                    continue;
                }
                ParticleSystem.MainModule main = system.main;
                main.startColor = new ParticleSystem.MinMaxGradient(color, Color.Lerp(color, Color.white, 0.45f));
            }
            foreach (ParticleSystem system in systems)
            {
                if (system != null)
                {
                    system.Play(false);
                }
            }
            if (flash != null)
            {
                flash.enabled = true;
                flash.color = Color.Lerp(color, Color.white, 0.35f);
                flash.range = flashRange * Mathf.Sqrt(scale);
                flash.intensity = flashIntensity;
            }
            UpdateRing();
        }


        /// <summary>Points the effect along <paramref name="direction"/> in the plane (for trails and jets).</summary>
        public void Face(Vector2 direction)
        {
            if (direction.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
            }
        }


        private void Update()
        {
            age += Time.deltaTime;
            if (flash != null && flash.enabled)
            {
                float t = flashTime > 0f ? Mathf.Clamp01(age / flashTime) : 1f;
                flash.intensity = flashIntensity * (1f - t) * (1f - t);
                if (t >= 1f)
                {
                    flash.enabled = false;
                }
            }
            UpdateRing();
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
            }
        }


        private void UpdateRing()
        {
            if (ring == null)
            {
                return;
            }
            float t = ringTime > 0f ? Mathf.Clamp01(age / ringTime) : 1f;
            bool visible = t < 1f;
            ring.enabled = visible;
            if (!visible)
            {
                return;
            }
            float eased = Tween.OutCubic(t);
            float size = scale * ringGrowth * Mathf.Lerp(0.15f, 1f, eased);
            ring.transform.localScale = new Vector3(size, size, 1f);
            block ??= new MaterialPropertyBlock();
            block.Clear();
            Color color = tint * 2f;
            color.a = (1f - t) * (1f - t);
            block.SetColor(BaseColorId, color);
            ring.SetPropertyBlock(block);
        }
    }
}
