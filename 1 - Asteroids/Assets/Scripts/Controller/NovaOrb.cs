using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// A nova orb (<see cref="EnemyShotKind.NovaOrb"/>): a slow, swelling ball of plasma that drifts after the nearest
    /// ship and, when its time runs out, bursts into a ring of plasma bolts. It hurts what it touches like any shot; a
    /// nova bomb clears it. Only the simulator bursts it (the ring arrives elsewhere as shots of its own); a puppet only
    /// swells and flashes as it goes.
    /// </summary>
    public class NovaOrb : Shot
    {
        [SerializeField] internal int ring = 10;
        [SerializeField] internal float ringSpeed = 7f;
        [SerializeField] internal EnemyShotKind ringShot = EnemyShotKind.Plasma;
        [Tooltip("Grows as the burst comes closer.")]
        [SerializeField] internal Transform core;
        [SerializeField] internal AudioClip burstClip;

        private Vector3 coreScale = Vector3.one;


        protected override void Awake()
        {
            base.Awake();
            if (core != null)
            {
                coreScale = core.localScale;
            }
        }


        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);
            if (!InPlay || core == null)
            {
                return;
            }
            float ripe = lifetime > 0f ? Mathf.Clamp01(Age / lifetime) : 0f;
            core.localScale = coreScale * (0.8f + 0.5f * ripe + 0.12f * ripe * Mathf.Sin(Age * Mathf.Lerp(6f, 40f, ripe)));
        }


        /// <summary>Its time ran out: the ring of plasma (the simulator fires it; the expiry happens nowhere else).</summary>
        protected override void Expire()
        {
            SpawnService spawner = Field != null ? Field.Spawner : null;
            if (spawner != null)
            {
                float offset = Random.Range(0f, 360f);
                int count = Mathf.Max(1, ring);
                for (int i = 0; i < count; i++)
                {
                    float angle = (offset + 360f * i / count) * Mathf.Deg2Rad;
                    var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    spawner.FireEnemyShot(ringShot, Position + direction * radius, direction * ringSpeed);
                }
                Field.Effects?.Explosion(Position, radius * 1.6f, impactTint);
                Field.Sounds?.Play(burstClip, 0.6f, 1f, 0.06f, 0.08f);
            }
            base.Expire();
        }


        protected override void OnDespawned()
        {
            // A puppet that lived out its time burst on the simulator: the flash shows here too.
            if (IsPuppet && Field != null && lifetime > 0f && Age >= lifetime * 0.85f)
            {
                Field.Effects?.Explosion(Position, radius * 1.6f, impactTint);
                Field.Sounds?.Play(burstClip, 0.6f, 1f, 0.06f, 0.08f);
            }
            base.OnDespawned();
        }
    }
}
