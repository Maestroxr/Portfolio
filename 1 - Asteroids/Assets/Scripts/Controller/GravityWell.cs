using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// A black hole that opens for a while. It pulls the ship, the rocks and the pickups toward it (harder the closer
    /// they are), swallows whatever reaches its core and grinds down the hull of a ship that gets too close.
    /// </summary>
    public class GravityWell : SpaceBody
    {
        [SerializeField] internal float pullRadius = 9f;
        [SerializeField] internal float strength = 22f;
        [SerializeField] internal float coreRadius = 0.9f;
        [Tooltip("Hull damage per second to a ship inside the core.")]
        [SerializeField] internal float coreDamage = 45f;
        [SerializeField] internal float growTime = 1.2f;
        [SerializeField] internal Transform disk;
        [SerializeField] internal float diskSpeed = 90f;
        [SerializeField] internal AudioSource hum;

        private Vector3 restScale = Vector3.one;
        private float damageTimer;

        /// <summary>0 while opening and closing, 1 at full strength.</summary>
        public float Strength01 { get; private set; }


        private void Awake()
        {
            if (visual != null)
            {
                restScale = visual.localScale;
            }
        }


        public override void OnSpawned()
        {
            base.OnSpawned();
            wraps = true;
            Velocity = Vector2.zero;
            Strength01 = 0f;
            damageTimer = 0f;
            if (visual != null)
            {
                visual.localScale = Vector3.zero;
            }
            if (hum != null)
            {
                hum.Play();
            }
        }


        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);
            if (!InPlay)
            {
                return;
            }
            float open = Mathf.Clamp01(Age / growTime);
            float close = lifetime > 0f ? Mathf.Clamp01((lifetime - Age) / growTime) : 1f;
            Strength01 = Mathf.SmoothStep(0f, 1f, Mathf.Min(open, close));
            if (visual != null)
            {
                visual.localScale = restScale * Strength01;
            }
            if (disk != null)
            {
                disk.localRotation = Quaternion.Euler(0f, 0f, -diskSpeed * deltaTime) * disk.localRotation;
            }
            if (hum != null)
            {
                hum.volume = 0.6f * Strength01;
            }
        }


        /// <summary>Acceleration toward the well felt at <paramref name="position"/>.</summary>
        public Vector2 Pull(Vector2 position)
        {
            Vector2 delta = Position - position;
            float distance = delta.magnitude;
            if (distance > pullRadius || distance < 0.001f)
            {
                return Vector2.zero;
            }
            float falloff = 1f - distance / pullRadius;
            return delta / distance * strength * falloff * falloff * Strength01;
        }


        /// <summary>Whether <paramref name="body"/> has reached the core and disappears.</summary>
        public bool Swallows(SpaceBody body)
        {
            if (body is Boss || body is Comet || body is Shot || Strength01 < 0.5f)
            {
                return false;
            }
            return (body.Position - Position).sqrMagnitude < coreRadius * coreRadius;
        }


        public void Consume(SpaceBody body)
        {
            Field?.Effects?.Spark(body.Position, new Color(0.7f, 0.5f, 1f), 1.2f);
            if (body is Shootable shootable && shootable.IsAlive)
            {
                shootable.TakeHit(new DamageInfo(999f, (Position - body.Position).normalized, body.Position, DamageSource.Hazard, false));
                return;
            }
            body.Despawn();
        }


        /// <summary>Hurts a ship inside the core.</summary>
        public void AffectPlayer(AsteroidsPlayer player, float deltaTime)
        {
            float reach = coreRadius + player.Radius;
            if ((player.Position - Position).sqrMagnitude > reach * reach || Strength01 < 0.5f)
            {
                return;
            }
            damageTimer -= deltaTime;
            if (damageTimer <= 0f)
            {
                damageTimer = 0.25f;
                player.TakeDamage(new DamageInfo(coreDamage * 0.25f, (player.Position - Position).normalized, Position, DamageSource.Hazard, false), true);
            }
        }


        protected override void OnDespawned()
        {
            base.OnDespawned();
            if (hum != null)
            {
                hum.Stop();
            }
        }
    }
}
