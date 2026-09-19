using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// A projectile: the ship's bolts, laser lances, scatter pellets and missiles, and the plasma, shrapnel and shards
    /// fired at the ship. Flies straight (or homes in, when it has a turn rate), passes through as many targets as its
    /// pierce allows, can burst into a small blast, and fades out at the end of its lifetime.
    /// Register to <see cref="ShotHitEvent"/> to know when a shot hit something.
    /// </summary>
    public class Shot : SpaceBody
    {
        public delegate void ShotHit(Shot shot, GameObject objectHit);

        public event ShotHit ShotHitEvent;

        /// <summary>The ship that fired the shot; null for enemy fire.</summary>
        public AsteroidsPlayer FiredBy;

        /// <summary>Kept from the original: seconds the shot flies.</summary>
        public float TimeAlive
        {
            get => lifetime;
            set => lifetime = value;
        }

        [SerializeField] internal bool enemy;
        [SerializeField] internal float damage = 1f;
        [SerializeField] internal int pierce = 1;
        [SerializeField] internal float blastRadius;
        [Tooltip("Degrees per second the shot turns toward its target; zero flies straight.")]
        [SerializeField] internal float homing;
        [Tooltip("Seconds of homing before the shot flies straight.")]
        [SerializeField] internal float homingTime = 3f;
        [SerializeField] internal float homingRange = 12f;
        [SerializeField] internal bool alignToVelocity = true;
        [SerializeField] internal TrailRenderer trail;
        [SerializeField] internal ParticleSystem exhaust;
        [SerializeField] internal Color impactTint = new Color(0.5f, 0.9f, 1f);

        private readonly List<Shootable> hits = new List<Shootable>(4);
        private Shootable homingTarget;
        private float retarget;

        public bool IsEnemy => enemy;

        public float Damage
        {
            get => damage;
            set => damage = value;
        }

        public int Pierce
        {
            get => pierce;
            set => pierce = Mathf.Max(1, value);
        }

        public float BlastRadius
        {
            get => blastRadius;
            set => blastRadius = value;
        }

        public int HitCount => hits.Count;


        /// <summary>Kept from the original: restarts the lifetime.</summary>
        public void StartTimeAlive()
        {
            OnSpawned();
        }


        public override void OnSpawned()
        {
            base.OnSpawned();
            wraps = false;
            hits.Clear();
            homingTarget = null;
            retarget = 0f;
            Align();
            if (trail != null)
            {
                trail.Clear();
                trail.emitting = true;
            }
            if (exhaust != null)
            {
                exhaust.Clear(true);
                exhaust.Play(true);
            }
        }


        public override void Tick(float deltaTime)
        {
            if (homing > 0f && Age < homingTime)
            {
                Home(deltaTime);
            }
            base.Tick(deltaTime);
            if (InPlay)
            {
                Align();
            }
        }


        private void Home(float deltaTime)
        {
            if (Field == null)
            {
                return;
            }
            Vector2 goal;
            if (enemy)
            {
                AsteroidsPlayer ship = Field.Player;
                if (ship == null || !ship.IsAlive)
                {
                    return;
                }
                goal = ship.Position;
            }
            else
            {
                retarget -= deltaTime;
                if (homingTarget == null || !homingTarget.IsAlive || retarget <= 0f)
                {
                    retarget = 0.25f;
                    Vector2 heading = Velocity.sqrMagnitude > 0.01f ? Velocity.normalized : Vector2.up;
                    Vector2 from = Position;
                    homingTarget = Field.NearestTarget(from + heading * homingRange * 0.4f, homingRange,
                        target => Vector2.Dot((target.Position - from).normalized, heading) > -0.2f);
                }
                if (homingTarget == null)
                {
                    return;
                }
                goal = homingTarget.Position;
            }
            float speed = Velocity.magnitude;
            if (speed < 0.01f)
            {
                return;
            }
            Vector2 current = Velocity / speed;
            Vector2 wanted = (goal - Position).normalized;
            float angle = Vector2.SignedAngle(current, wanted);
            float turn = Mathf.Clamp(angle, -homing * deltaTime, homing * deltaTime);
            Velocity = (Vector2)(Quaternion.Euler(0f, 0f, turn) * current) * speed;
        }


        private void Align()
        {
            if (alignToVelocity && Velocity.sqrMagnitude > 0.0001f)
            {
                float angle = Mathf.Atan2(Velocity.y, Velocity.x) * Mathf.Rad2Deg - 90f;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }


        /// <summary>Whether the shot may still hit <paramref name="target"/> (it never hits the same target twice).</summary>
        public bool CanHit(Shootable target)
        {
            return InPlay && hits.Count < pierce && !hits.Contains(target);
        }


        /// <summary>Hits <paramref name="target"/>: damages it, bursts if the shot carries a blast, and ends when spent.</summary>
        public void Hit(Shootable target)
        {
            hits.Add(target);
            Vector2 direction = Velocity.sqrMagnitude > 0.01f ? Velocity.normalized : Vector2.up;
            Vector2 point = target.Position - direction * target.Radius * 0.8f;
            target.TakeHit(new DamageInfo(damage, direction, point, DamageSource.PlayerShot, true, this));
            ShotHitEvent?.Invoke(this, target.gameObject);
            if (Field != null && Field.Effects != null)
            {
                Field.Effects.Spark(point, impactTint, 1f);
            }
            if (blastRadius > 0f && Field != null)
            {
                Field.Explode(new Blast
                {
                    Center = point,
                    Radius = blastRadius,
                    Damage = damage * 0.6f,
                    PlayerDamage = 0f,
                    Push = 2.5f,
                    ByPlayer = true,
                    Source = target,
                    Tint = impactTint
                });
            }
            if (hits.Count >= pierce)
            {
                Despawn();
            }
        }


        /// <summary>The shot stops against something (the ship, a shield, a nova) with a small spark.</summary>
        public void Impact()
        {
            if (Field != null && Field.Effects != null)
            {
                Field.Effects.Spark(Position, impactTint, 0.8f);
            }
            ShotHitEvent?.Invoke(this, null);
            Despawn();
        }


        protected override void Expire()
        {
            ShotHitEvent?.Invoke(this, null);
            if (blastRadius > 0f && Field != null && Field.Effects != null)
            {
                Field.Effects.Spark(Position, impactTint, 1.2f);
            }
            Despawn();
        }


        protected override void OnDespawned()
        {
            base.OnDespawned();
            ShotHitEvent = null;
            FiredBy = null;
            homingTarget = null;
            if (trail != null)
            {
                trail.emitting = false;
                trail.Clear();
            }
            if (exhaust != null)
            {
                exhaust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }
}
