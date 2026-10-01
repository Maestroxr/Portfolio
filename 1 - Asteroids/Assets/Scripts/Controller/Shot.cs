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

        /// <summary>
        /// A ghost shows the shot of a pilot on another device (a shared mission): it flies and stops like the real one,
        /// but hurts nothing. What the real shot hit is decided on the client of the pilot who fired it.
        /// </summary>
        public bool IsGhost { get; internal set; }

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

        [Header("Strike")]
        [Tooltip("The layers the shot hits (strike: air, ground or both); its blasts hurt the same layers.")]
        [SerializeField] internal Altitude reach = Altitude.Both;
        [Tooltip("Meters per second squared the shot speeds up by along its course.")]
        [SerializeField] internal float acceleration;
        [Tooltip("Top speed; zero for none.")]
        [SerializeField] internal float maxSpeed;
        [Tooltip("Side-to-side wobble in meters; zero flies straight.")]
        [SerializeField] internal float wobble;
        [Tooltip("Bursts into a blast on the ground when its lifetime runs out (bombs).")]
        [SerializeField] internal bool burstOnExpire;
        [SerializeField] internal float burstRadius = 2.2f;
        [SerializeField] internal float burstDamage = 50f;

        /// <summary>Wobbles per second.</summary>
        private const float WobbleRate = 0.8f;

        private readonly List<Shootable> hits = new List<Shootable>(4);
        private Shootable homingTarget;
        private float retarget;
        private float wobbleOffset;

        public bool IsEnemy => enemy;

        /// <summary>The seat the shot's hits count for (<see cref="AsteroidsPlayer.HitSeat"/> of the ship that fired it).</summary>
        private int? Seat => FiredBy != null ? FiredBy.HitSeat : null;

        /// <summary>
        /// The layers the shot can hit now. Starts as the prefab's <see cref="reach"/>; a spawner may change it before the
        /// shot is added to the field, and it goes back to the prefab's when the shot returns to its pool.
        /// </summary>
        public Altitude Reach { get; set; } = Altitude.Both;

        /// <summary>Whether the shot ends when it touches the ship; a beam stays and hurts in its own tick instead.</summary>
        internal virtual bool StopsOnHit => true;

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


        protected virtual void Awake()
        {
            Reach = reach;
        }


        /// <summary>Whether the shot touches a circle of <paramref name="radius"/> at <paramref name="center"/> (the ship).</summary>
        internal virtual bool Touches(Vector2 center, float radius)
        {
            float touch = this.radius + radius;
            return (Position - center).sqrMagnitude <= touch * touch;
        }


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
            wobbleOffset = 0f;
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
            Accelerate(deltaTime);
            base.Tick(deltaTime);
            if (InPlay)
            {
                Wobble();
                Align();
            }
        }


        /// <summary>Speeds the shot up along its course toward its top speed (both zero for the field's shots).</summary>
        private void Accelerate(float deltaTime)
        {
            if (acceleration <= 0f && maxSpeed <= 0f)
            {
                return;
            }
            float speed = Velocity.magnitude;
            Vector2 direction = speed > 0.001f ? Velocity / speed : (Vector2)transform.up;
            if (acceleration > 0f)
            {
                speed += acceleration * deltaTime;
            }
            if (maxSpeed > 0f)
            {
                speed = Mathf.Min(speed, maxSpeed);
            }
            Velocity = direction * speed;
        }


        /// <summary>Sways the shot across its course by up to <see cref="wobble"/> meters.</summary>
        private void Wobble()
        {
            if (wobble <= 0f || Velocity.sqrMagnitude < 0.0001f)
            {
                return;
            }
            Vector2 forward = Velocity.normalized;
            var side = new Vector2(forward.y, -forward.x);
            float offset = Mathf.Sin(Age * WobbleRate * 2f * Mathf.PI) * wobble;
            Position += side * (offset - wobbleOffset);
            wobbleOffset = offset;
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
                // A puppet follows the course the simulator reports instead of picking a ship itself.
                AsteroidsPlayer ship = IsPuppet ? null : Field.NearestShip(Position);
                if (ship == null)
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
                    Altitude layers = Reach;
                    homingTarget = Field.NearestTarget(from + heading * homingRange * 0.4f, homingRange,
                        target => (target.Altitude & layers) != 0 && Vector2.Dot((target.Position - from).normalized, heading) > -0.2f);
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


        /// <summary>
        /// Whether the shot may still hit <paramref name="target"/>: it reaches the target's layer and never hits the same
        /// target twice.
        /// </summary>
        public bool CanHit(Shootable target)
        {
            return InPlay && hits.Count < pierce && (target.Altitude & Reach) != 0 && !hits.Contains(target);
        }


        /// <summary>Hits <paramref name="target"/>: damages it, bursts if the shot carries a blast, and ends when spent.</summary>
        public void Hit(Shootable target)
        {
            hits.Add(target);
            Vector2 direction = Velocity.sqrMagnitude > 0.01f ? Velocity.normalized : Vector2.up;
            Vector2 point = target.Position - direction * target.Radius * 0.8f;
            if (!IsGhost)
            {
                target.TakeHit(new DamageInfo(damage, direction, point, DamageSource.PlayerShot, true, this) { Seat = Seat });
            }
            ShotHitEvent?.Invoke(this, target.gameObject);
            if (Field != null && Field.Effects != null)
            {
                Field.Effects.Spark(point, impactTint, 1f);
            }
            if (blastRadius > 0f && IsGhost)
            {
                Field?.Effects?.Explosion(point, blastRadius * 0.55f, impactTint);
            }
            else if (blastRadius > 0f && Field != null)
            {
                Field.Explode(new Blast
                {
                    Center = point,
                    Radius = blastRadius,
                    Damage = damage * 0.6f,
                    PlayerDamage = 0f,
                    Push = 2.5f,
                    ByPlayer = true,
                    Seat = Seat,
                    Source = target,
                    Tint = impactTint,
                    Layers = Reach
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
            if (IsPuppet && InPlay && Field != null && Field.Link != null)
            {
                // The real shot flies on in the simulator's field until it hears that this ship stopped it.
                Field.Link.PuppetShotAbsorbed(this);
            }
            Exit = ExitReason.Impact;
            Despawn();
        }


        /// <summary>The shot a puppet stands for stopped against something on another device: the spark shows here too.</summary>
        internal void PlayImpact()
        {
            if (Field != null && Field.Effects != null)
            {
                Field.Effects.Spark(Position, impactTint, 0.8f);
            }
            Despawn();
        }


        protected override void Expire()
        {
            ShotHitEvent?.Invoke(this, null);
            if (burstOnExpire && Field != null)
            {
                Burst();
            }
            else if (blastRadius > 0f && Field != null && Field.Effects != null)
            {
                Field.Effects.Spark(Position, impactTint, 1.2f);
            }
            Despawn();
        }


        /// <summary>A bomb reaches the ground: a blast that hurts only ground targets (a ghost's only shows).</summary>
        private void Burst()
        {
            if (IsGhost || IsPuppet)
            {
                Field.Effects?.Explosion(Position, burstRadius * 0.55f, impactTint);
                return;
            }
            Field.Explode(new Blast
            {
                Center = Position,
                Radius = burstRadius,
                Damage = burstDamage,
                PlayerDamage = 0f,
                Push = 0f,
                ByPlayer = !enemy,
                Seat = enemy ? null : Seat,
                Tint = impactTint,
                Layers = Altitude.Ground
            });
        }


        protected override void OnDespawned()
        {
            base.OnDespawned();
            ShotHitEvent = null;
            FiredBy = null;
            IsGhost = false;
            homingTarget = null;
            Reach = reach;
            wobbleOffset = 0f;
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
