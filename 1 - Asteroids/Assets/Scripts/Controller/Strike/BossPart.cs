using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// A part of a <see cref="StrikeBoss"/>: a turret, launcher or module with its own hull and guns. It stays where it
    /// sits on the boss (rings rotate its parent), is never nudged or pushed, is armoured while a part of a lower tier
    /// lives, and dies on its own. On the other pilots' clients it is bound to the simulator's part by its index; its
    /// hull and flags come through the boss's replication, never through <see cref="Correct"/>.
    ///
    /// Its root must sit under the boss's root (or a ring under it) in the plane of the playfield, never under a depth
    /// anchor: a part of a ground boss carries its own "Depth" -> "Visual".
    /// </summary>
    public class BossPart : Enemy
    {
        [Header("Boss part")]
        [Tooltip("Armoured while a part of a lower tier lives.")]
        [SerializeField] internal int tier;
        [SerializeField] internal AttackPattern pattern = AttackPattern.Aimed;
        [SerializeField] internal int burst = 1;
        [SerializeField] internal float burstGap = 0.15f;
        [SerializeField] internal float interval = 2f;
        [Tooltip("Turns to aim at the nearest pilot; null for none.")]
        [SerializeField] internal Transform turret;
        [Tooltip("Where the shots leave; the centre when empty.")]
        [SerializeField] internal Transform[] muzzles = new Transform[0];

        /// <summary>Seconds of warning before a laser part fires.</summary>
        private const float LaserTelegraph = 0.6f;

        /// <summary>Degrees per second a part's turret turns.</summary>
        private const float TurretTurnRate = 90f;

        private readonly StrikeGun gun = new StrikeGun();
        private bool hasRest;
        private Vector3 restPosition;
        private Quaternion turretRest = Quaternion.identity;
        private float turretAngle;

        /// <summary>Its place in the boss's parts (set by <see cref="StrikeBoss.AttachParts"/>).</summary>
        internal int index;

        /// <summary>The boss the part belongs to.</summary>
        public StrikeBoss Boss { get; internal set; }

        public int Index => index;

        public int Tier => tier;

        /// <summary>The gun's clock (for tests and tours).</summary>
        internal StrikeGun Gun => gun;


        public override void OnSpawned()
        {
            if (!hasRest)
            {
                restPosition = transform.localPosition;
                turretRest = turret != null ? turret.localRotation : Quaternion.identity;
                hasRest = true;
            }
            transform.localPosition = restPosition;
            base.OnSpawned();
            wraps = false;
            Velocity = Vector2.zero;
            turretAngle = 0f;
            float scale = Field != null && Field.Spawner != null ? Field.Spawner.BossBurstScale : 1f;
            int shots = Mathf.Max(1, Mathf.RoundToInt(burst * scale));
            gun.Configure(pattern, shotKind, shots, burstGap, interval, AttackPattern.None, shotKind,
                pattern == AttackPattern.Laser ? LaserTelegraph : 0f);
            gun.Restart(interval * Random.Range(0.4f, 1f));
        }


        public override void Tick(float deltaTime)
        {
            // It sits where it was built on the boss; the boss (and a ring) move it.
            Velocity = Vector2.zero;
            if (hasRest)
            {
                transform.localPosition = restPosition;
            }
            base.Tick(deltaTime);
            if (!InPlay)
            {
                return;
            }
            AimTurret(deltaTime);
            bool fighting = Boss != null && Boss.InPlay && Boss.HasEntered && !Boss.IsDying;
            if (IsPuppet || !fighting)
            {
                return;
            }
            if (gun.Tick(deltaTime, CanFireStrike))
            {
                int shots = muzzles != null && muzzles.Length > 0 ? muzzles.Length : 1;
                Vector2? aim = turret != null && StrikeGun.Aims(gun.CurrentPattern) ? TurretDirection() : (Vector2?)null;
                Vector2 carrier = Boss.Velocity + (Boss.IsGroundBoss && Field != null ? Field.ScrollVelocity : Vector2.zero);
                FireStrikeVolley(gun.CurrentPattern, gun.CurrentKind, shots, muzzles, aim, carrier);
            }
        }


        /// <summary>The world heading of the turret's barrel (0 = up the screen).</summary>
        private Vector2 TurretDirection()
        {
            Transform parent = turret.parent;
            float baseHeading = parent != null ? parent.eulerAngles.z : 0f;
            return StrikeGun.HeadingDirection(baseHeading + turretAngle);
        }


        /// <summary>
        /// Turns the turret toward the nearest pilot; its barrel points up its parent's +Y at rest (on puppets too: it only
        /// shows).
        /// </summary>
        private void AimTurret(float deltaTime)
        {
            if (turret == null)
            {
                return;
            }
            AsteroidsPlayer ship = Field != null ? Field.NearestShip(Position) : null;
            Transform parent = turret.parent;
            float baseHeading = parent != null ? parent.eulerAngles.z : 0f;
            float wanted = turretAngle;
            if (ship != null && (ship.Position - Position).sqrMagnitude > 0.0001f)
            {
                wanted = Mathf.DeltaAngle(baseHeading, StrikeGun.HeadingOf(ship.Position - Position));
            }
            turretAngle = Mathf.MoveTowardsAngle(turretAngle, wanted, TurretTurnRate * deltaTime);
            Quaternion rest = (parent != null ? parent.rotation : Quaternion.identity) * turretRest;
            turret.rotation = Quaternion.AngleAxis(turretAngle, Vector3.forward) * rest;
        }


        /// <summary>A part's place and state come with its boss: the simulator's corrections are not applied.</summary>
        internal override void Correct(Vector2 position, Vector2 velocity, float snapDistance = 3f)
        {
        }


        /// <summary>A part leaves with its boss, never on its own.</summary>
        protected override bool HasLeft(Playground playground)
        {
            return false;
        }


        /// <summary>A part is never nudged by hits.</summary>
        protected override void OnHit(DamageInfo hit)
        {
        }


        /// <summary>Flying into a part hurts only the ship (the field does that).</summary>
        public override void OnRammed(AsteroidsPlayer player, Vector2 direction, bool dashing)
        {
        }


        /// <summary>
        /// It goes up on its own: an explosion (on the ground for a ground boss's part), and on the simulator the boss hears
        /// of it (armour, phases, a coreless boss's end).
        /// </summary>
        protected override void OnDestroyed(DamageInfo hit)
        {
            if (Field != null)
            {
                if (altitude == Altitude.Ground)
                {
                    Field.Effects?.GroundExplosion(Position, explosionScale);
                    Field.Sounds?.GroundBoom(explosionScale);
                }
                else
                {
                    Field.Effects?.Explosion(Position, explosionScale, explosionTint);
                    Field.Sounds?.EnemyExplode();
                }
                Field.CameraRig?.Shake(0.35f);
            }
            if (!IsPuppet && Boss != null)
            {
                Boss.PartDestroyed(this, hit);
            }
        }


        protected override void OnDespawned()
        {
            base.OnDespawned();
            if (hasRest)
            {
                transform.localPosition = restPosition;
            }
        }
    }
}
