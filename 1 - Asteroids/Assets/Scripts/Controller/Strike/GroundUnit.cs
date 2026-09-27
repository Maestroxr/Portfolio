using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// A ground unit of the strike mode: stands or drives on the ground and moves with it (its velocity is its own motion
    /// relative to the ground; the scroll is added in its tick, on the simulator and on puppets), aims its turret at the
    /// nearest pilot and fires its pattern while on screen. It is flown over, never rammed, and leaves the field only
    /// below the bottom edge. When destroyed it leaves a crater, may explode into a chain blast that hurts ground units,
    /// and drops its event's pickup.
    ///
    /// The root turns to the heading (0 = nose up the screen, 90 = left); a turret's barrel points along the nose at rest
    /// and turns about the screen's axis.
    /// </summary>
    public class GroundUnit : Enemy
    {
        [Header("Ground unit")]
        [SerializeField] internal StrikeUnit unit = StrikeUnit.Turret;
        [Tooltip("Turns to aim at the nearest pilot; null for units without a turret.")]
        [SerializeField] internal Transform turret;
        [Tooltip("Degrees per second the turret turns.")]
        [SerializeField] internal float turretTurnRate = 120f;
        [Tooltip("Where the shots leave; the centre when empty.")]
        [SerializeField] internal Transform[] muzzles = new Transform[0];
        [SerializeField] internal AttackPattern pattern = AttackPattern.Aimed;
        [Tooltip("Volleys per attack.")]
        [SerializeField] internal int burst = 1;
        [Tooltip("Seconds between the volleys of a burst.")]
        [SerializeField] internal float burstGap = 0.2f;
        [Tooltip("Seconds between attacks.")]
        [SerializeField] internal float interval = 1.6f;
        [Tooltip("Explodes when destroyed and damages the ground units around it (fuel tanks, depots).")]
        [SerializeField] internal bool explodes;
        [SerializeField] internal float blastRadius = 3.5f;
        [SerializeField] internal float blastDamage = 12f;
        [Tooltip("Size of the crater or rubble left on the ground.")]
        [SerializeField] internal float craterSize = 1.8f;
        [Tooltip("Spins all the time (a radar dish); null for none.")]
        [SerializeField] internal Transform spinner;
        [SerializeField] internal float spinSpeed = 90f;
        [Tooltip("Shown while the unit warns of its attack (the laser tower's glow); null for none.")]
        [SerializeField] internal GameObject telegraph;

        /// <summary>Seconds of glow before a laser fires when the unit table gives none.</summary>
        private const float LaserTelegraph = 0.6f;

        private readonly StrikeGun gun = new StrikeGun();
        private float drive;
        private float turretAngle;
        private bool hasTurretRest;
        private Quaternion turretRest = Quaternion.identity;

        public StrikeUnit Unit => unit;

        /// <summary>The event the unit was spawned for; null for a puppet.</summary>
        public StrikeEvent Event { get; private set; }

        /// <summary>Degrees the unit points (0 = up the screen, 90 = left), as replicated.</summary>
        public float Heading { get; set; }

        /// <summary>Meters per second it drives along its heading, relative to the ground, once it is on screen (0 = parked).</summary>
        public float Drive => drive;

        /// <summary>Degrees the turret is turned from the nose.</summary>
        public float TurretAngle => turretAngle;

        /// <summary>Whether the unit is warning of its next attack (the laser tower's glow).</summary>
        public bool IsWarning => gun.IsWarning;

        /// <summary>The glow before a laser travels with the unit, so a puppet glows while its simulator's unit does.</summary>
        internal override bool IsTelegraphing => IsWarning;

        /// <summary>The gun's clock (for tests and tours).</summary>
        internal StrikeGun Gun => gun;


        private void Awake()
        {
            altitude = Altitude.Ground;
        }


        /// <summary>Puts member <paramref name="member"/> of <paramref name="spawn"/> at <paramref name="position"/> with its heading and drive.</summary>
        public void Place(StrikeEvent spawn, int member, Vector2 position)
        {
            Event = spawn;
            Position = position;
            Heading = spawn != null ? spawn.heading : 0f;
            drive = spawn != null ? spawn.drive : 0f;
            transform.rotation = Quaternion.Euler(0f, 0f, Heading);
            // Drivers start once they are on screen, so a column comes into view as it was placed.
            Velocity = Vector2.zero;
            turretAngle = 0f;
        }


        public override void OnSpawned()
        {
            base.OnSpawned();
            altitude = Altitude.Ground;
            if (!hasTurretRest && turret != null)
            {
                turretRest = turret.localRotation;
                hasTurretRest = true;
            }
            StrikeUnitInfo info = StrikeUnitRules.Info(unit);
            float warning = pattern == AttackPattern.Laser ? (info.Telegraph > 0f ? info.Telegraph : LaserTelegraph) : 0f;
            gun.Configure(pattern, shotKind, burst, burstGap, interval, info.AlternatePattern, info.AlternateShotKind, warning);
            gun.Restart(interval * Random.Range(0.3f, 0.8f));
            ShowTelegraph(false);
            if (IsPuppet)
            {
                Event = null;
                drive = 0f;
                // A puppet's heading comes with its row (the root's rotation).
                Heading = transform.eulerAngles.z;
            }
            AimTurret(0f, true);
        }


        public override void Tick(float deltaTime)
        {
            if (!IsPuppet && drive != 0f && HasEntered)
            {
                Velocity = StrikeGun.HeadingDirection(Heading) * drive;
            }
            base.Tick(deltaTime);
            if (!InPlay)
            {
                return;
            }
            if (Field != null)
            {
                // The ground carries it: its velocity is only its own motion on the ground.
                Position += Field.ScrollVelocity * deltaTime;
            }
            if (spinner != null)
            {
                spinner.localRotation = Quaternion.Euler(0f, 0f, spinSpeed * deltaTime) * spinner.localRotation;
            }
            if (IsPuppet)
            {
                // The simulator turns the root to the heading it reports.
                Heading = transform.eulerAngles.z;
            }
            AimTurret(deltaTime, false);
            if (IsPuppet)
            {
                return;
            }
            bool fire = gun.Tick(deltaTime, CanFireStrike);
            ShowTelegraph(gun.IsWarning);
            if (fire)
            {
                int shots = StrikeUnitRules.Info(unit).Shots;
                if (shots <= 0)
                {
                    shots = muzzles != null && muzzles.Length > 0 ? muzzles.Length : 1;
                }
                Vector2? aim = turret != null && StrikeGun.Aims(gun.CurrentPattern) ? StrikeGun.HeadingDirection(Heading + turretAngle) : (Vector2?)null;
                Vector2 carrier = Velocity + (Field != null ? Field.ScrollVelocity : Vector2.zero);
                FireStrikeVolley(gun.CurrentPattern, gun.CurrentKind, shots, muzzles, aim, carrier);
            }
        }


        /// <summary>Turns the turret toward the nearest pilot (at once when <paramref name="snap"/>); puppets aim too, it only shows.</summary>
        private void AimTurret(float deltaTime, bool snap)
        {
            if (turret == null || !hasTurretRest)
            {
                return;
            }
            AsteroidsPlayer ship = Field != null && !snap ? Field.NearestShip(Position) : null;
            float wanted = 0f;
            if (ship != null && (ship.Position - Position).sqrMagnitude > 0.0001f)
            {
                wanted = Mathf.DeltaAngle(Heading, StrikeGun.HeadingOf(ship.Position - Position));
            }
            turretAngle = snap ? wanted : Mathf.MoveTowardsAngle(turretAngle, wanted, turretTurnRate * deltaTime);
            Transform parent = turret.parent;
            Quaternion rest = (parent != null ? parent.rotation : Quaternion.identity) * turretRest;
            turret.rotation = Quaternion.AngleAxis(turretAngle, Vector3.forward) * rest;
        }


        private void ShowTelegraph(bool show)
        {
            if (telegraph != null && telegraph.activeSelf != show)
            {
                telegraph.SetActive(show);
            }
        }


        /// <summary>A puppet glows while the simulator reports its unit warning (it has no clock of its own).</summary>
        internal override void ShowWarning(bool warning)
        {
            ShowTelegraph(warning);
        }


        /// <summary>A ground unit leaves only below the bottom edge; one that drove off a side scrolls down there in time.</summary>
        protected override bool HasLeft(Playground playground)
        {
            return Position.y < playground.Bottom - radius - 1.5f;
        }


        /// <summary>A ground unit is not nudged by hits: its velocity is its drive.</summary>
        protected override void OnHit(DamageInfo hit)
        {
        }


        /// <summary>First on screen: one more hostile that entered (for the kill star).</summary>
        protected override void OnEnteredScreen()
        {
            Field?.NoteEntered(this);
        }


        /// <summary>
        /// A ground explosion that moves with the ground, a boom and a crater (on puppets too: effects and decals only); on
        /// the simulator its drops and, for fuel tanks and depots, the chain blast that hurts the ground units around it.
        /// </summary>
        protected override void OnDestroyed(DamageInfo hit)
        {
            ShowTelegraph(false);
            if (Field == null)
            {
                return;
            }
            Field.Effects?.GroundExplosion(Position, explosionScale);
            Field.Sounds?.GroundBoom(explosionScale);
            Field.CameraRig?.Shake(explodes ? 0.35f : 0.15f);
            if (Field.Terrain != null)
            {
                Field.Terrain.AddCrater(Position, craterSize);
            }
            if (IsPuppet)
            {
                return;
            }
            Field.Spawner?.DropStrikeSpoils(Event, false, Position);
            if (explodes && blastRadius > 0f)
            {
                Field.Explode(new Blast
                {
                    Center = Position,
                    Radius = blastRadius,
                    Damage = blastDamage,
                    PlayerDamage = 0f,
                    Push = 0f,
                    ByPlayer = hit.ByPlayer,
                    Seat = hit.ByPlayer ? hit.Seat : null,
                    Source = this,
                    Tint = explosionTint,
                    Layers = Altitude.Ground
                });
            }
        }


        protected override void OnDespawned()
        {
            base.OnDespawned();
            Event = null;
            drive = 0f;
            ShowTelegraph(false);
        }
    }
}
