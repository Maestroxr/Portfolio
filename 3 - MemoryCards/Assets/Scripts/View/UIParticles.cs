using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.MemoryCards
{
    /// <summary>
    /// Particles drawn as UI images, so they live on the overlay canvas with the cards: sparkles of a match, confetti
    /// of a cleared board, smoke of a bomb, shards of ice. A fixed pool of images is reused; the oldest particle makes
    /// way when the pool runs out.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class UIParticles : MonoBehaviour
    {
        private sealed class Particle
        {
            public RectTransform rect;
            public Image image;
            public Vector2 position;
            public Vector2 velocity;
            public float age;
            public float life;
            public float size;
            public float spin;
            public float angle;
            public float gravity;
            public float drag;
            public float sway;
            public float swayPhase;
            public bool shrink;
            public Color color;
        }

        [SerializeField] internal Image template;
        [SerializeField] internal int capacity = 220;
        [SerializeField] internal Sprite spark;
        [SerializeField] internal Sprite star;
        [SerializeField] internal Sprite circle;
        [SerializeField] internal Sprite confetti;
        [SerializeField] internal Sprite shard;

        private readonly List<Particle> pool = new List<Particle>();
        private readonly List<Particle> live = new List<Particle>();
        private RectTransform rect;

        public RectTransform Rect => rect != null ? rect : rect = (RectTransform)transform;

        private void Awake()
        {
            if (template != null)
            {
                template.gameObject.SetActive(false);
                template.raycastTarget = false;
            }
        }

        /// <summary>A world position (of a card, say) in the particle layer's local space.</summary>
        public Vector2 ToLocal(Vector3 worldPosition)
        {
            return Rect.InverseTransformPoint(worldPosition);
        }

        /// <summary>Particles bursting out of <paramref name="position"/> in every direction.</summary>
        public void Burst(Vector2 position, int count, Sprite sprite, Color[] colors, float speed, float size, float life, float gravity = 0f, float drag = 1.5f)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Particle particle = Spawn(sprite, Pick(colors));
                if (particle == null)
                {
                    return;
                }
                particle.position = position + direction * Random.Range(0f, size * 0.5f);
                particle.velocity = direction * Random.Range(speed * 0.35f, speed);
                particle.size = Random.Range(size * 0.55f, size);
                particle.life = Random.Range(life * 0.7f, life);
                particle.gravity = gravity;
                particle.drag = drag;
                particle.spin = Random.Range(-360f, 360f);
                particle.shrink = true;
            }
        }

        /// <summary>Sparkles of a completed set.</summary>
        public void Sparkle(Vector2 position, Color accent)
        {
            Burst(position, 10, star != null ? star : spark, new[] { accent, Color.white, new Color(1f, 0.9f, 0.4f) }, 520f, 30f, 0.7f, -300f);
            Burst(position, 6, spark, new[] { Color.white }, 300f, 22f, 0.45f);
        }

        /// <summary>A rainbow burst for the wild card.</summary>
        public void Rainbow(Vector2 position)
        {
            var colors = new[]
            {
                new Color(1f, 0.35f, 0.35f), new Color(1f, 0.7f, 0.2f), new Color(1f, 0.95f, 0.3f),
                new Color(0.4f, 0.9f, 0.4f), new Color(0.35f, 0.7f, 1f), new Color(0.75f, 0.45f, 1f)
            };
            Burst(position, 34, star != null ? star : spark, colors, 900f, 34f, 1f, -500f, 1.2f);
        }

        /// <summary>Smoke and sparks of a bomb.</summary>
        public void Explosion(Vector2 position)
        {
            Burst(position, 14, circle, new[] { new Color(0.35f, 0.33f, 0.38f, 0.85f), new Color(0.55f, 0.52f, 0.56f, 0.8f) }, 260f, 90f, 0.9f, 120f, 2.5f);
            Burst(position, 18, spark, new[] { new Color(1f, 0.75f, 0.2f), new Color(1f, 0.4f, 0.15f), Color.white }, 900f, 26f, 0.55f, -600f);
        }

        /// <summary>Shards of cracked ice.</summary>
        public void Shards(Vector2 position)
        {
            Burst(position, 12, shard != null ? shard : spark, new[] { new Color(0.8f, 0.95f, 1f), new Color(0.6f, 0.85f, 1f), Color.white }, 520f, 26f, 0.6f, -900f, 1f);
        }

        /// <summary>A few soft puffs (a clock or peek card used up).</summary>
        public void Puff(Vector2 position, Color color)
        {
            Burst(position, 10, circle, new[] { color, Color.Lerp(color, Color.white, 0.5f) }, 240f, 40f, 0.6f, 0f, 3f);
        }

        /// <summary>Confetti raining from the top edge over the whole layer.</summary>
        public void Confetti(int count, Color[] colors)
        {
            Rect area = Rect.rect;
            for (int i = 0; i < count; i++)
            {
                Particle particle = Spawn(confetti != null ? confetti : spark, Pick(colors));
                if (particle == null)
                {
                    return;
                }
                particle.position = new Vector2(Random.Range(area.xMin, area.xMax), area.yMax + Random.Range(10f, 260f));
                particle.velocity = new Vector2(Random.Range(-80f, 80f), Random.Range(-420f, -160f));
                particle.size = Random.Range(16f, 30f);
                particle.life = Random.Range(2.6f, 3.6f);
                particle.gravity = 120f;
                particle.drag = 0.2f;
                particle.spin = Random.Range(-540f, 540f);
                particle.sway = Random.Range(40f, 120f);
                particle.swayPhase = Random.Range(0f, 6f);
                particle.shrink = false;
            }
        }

        public void Clear()
        {
            foreach (Particle particle in live)
            {
                particle.image.enabled = false;
                pool.Add(particle);
            }
            live.Clear();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f)
            {
                return;
            }
            for (int i = live.Count - 1; i >= 0; i--)
            {
                Particle particle = live[i];
                particle.age += dt;
                if (particle.age >= particle.life)
                {
                    particle.image.enabled = false;
                    live.RemoveAt(i);
                    pool.Add(particle);
                    continue;
                }
                particle.velocity.y -= particle.gravity * dt;
                particle.velocity *= Mathf.Max(0f, 1f - particle.drag * dt);
                particle.position += particle.velocity * dt;
                particle.angle += particle.spin * dt;
                float t = particle.age / particle.life;
                Vector2 shown = particle.position;
                if (particle.sway > 0f)
                {
                    shown.x += Mathf.Sin(particle.age * 3f + particle.swayPhase) * particle.sway;
                }
                particle.rect.anchoredPosition = shown;
                particle.rect.localRotation = Quaternion.Euler(0f, 0f, particle.angle);
                float scale = particle.shrink ? 1f - t * t : 1f;
                particle.rect.sizeDelta = new Vector2(particle.size, particle.size) * scale;
                Color color = particle.color;
                color.a *= t > 0.7f ? 1f - (t - 0.7f) / 0.3f : 1f;
                particle.image.color = color;
            }
        }

        private Particle Spawn(Sprite sprite, Color color)
        {
            Particle particle;
            if (pool.Count > 0)
            {
                particle = pool[pool.Count - 1];
                pool.RemoveAt(pool.Count - 1);
            }
            else if (live.Count + pool.Count < capacity)
            {
                particle = Create();
                if (particle == null)
                {
                    return null;
                }
            }
            else
            {
                particle = live[0];
                live.RemoveAt(0);
            }
            particle.age = 0f;
            particle.angle = Random.Range(0f, 360f);
            particle.sway = 0f;
            particle.color = color;
            particle.image.sprite = sprite;
            particle.image.color = color;
            particle.image.enabled = true;
            particle.rect.SetAsLastSibling();
            live.Add(particle);
            return particle;
        }

        private Particle Create()
        {
            if (template == null)
            {
                return null;
            }
            Image image = Instantiate(template, Rect);
            image.gameObject.SetActive(true);
            image.raycastTarget = false;
            var particleRect = image.rectTransform;
            particleRect.anchorMin = particleRect.anchorMax = new Vector2(0.5f, 0.5f);
            particleRect.pivot = new Vector2(0.5f, 0.5f);
            return new Particle { rect = particleRect, image = image };
        }

        private static Color Pick(Color[] colors)
        {
            return colors == null || colors.Length == 0 ? Color.white : colors[Random.Range(0, colors.Length)];
        }
    }
}
