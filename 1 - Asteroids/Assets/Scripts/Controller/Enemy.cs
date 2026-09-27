using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// Base class of the enemies: a shootable with a fire timer and helpers to aim at the ship. Destroyed enemies go
    /// up in a fireball and drop what their drop table gives.
    /// </summary>
    public abstract class Enemy : Shootable
    {
        [SerializeField] internal float fireInterval = 1.6f;
        [SerializeField] internal float shotSpeed = 9f;
        [Tooltip("Largest aiming error in degrees.")]
        [SerializeField] internal float aimError = 10f;
        [SerializeField] internal EnemyShotKind shotKind = EnemyShotKind.Plasma;
        [SerializeField] internal Color explosionTint = new Color(1f, 0.55f, 0.25f);
        [SerializeField] internal float explosionScale = 1.4f;

        protected float fireTimer;

        private bool warningShown;

        /// <summary>The closest ship that is alive (there are several in a shared mission), or null.</summary>
        protected AsteroidsPlayer Target => Field != null ? Field.NearestShip(Position) : null;

        /// <summary>Keeps the enemy from firing (cleared when it spawns).</summary>
        public bool ShotsDisabled { get; set; }


        public override void OnSpawned()
        {
            base.OnSpawned();
            ShotsDisabled = false;
            fireTimer = fireInterval * Random.Range(0.6f, 1.2f);
            warningShown = false;
        }


        /// <summary>
        /// Counts the fire timer down; true when it is time to fire again. In a strike mission (the playground does not
        /// wrap) an enemy fires only while it is on screen.
        /// </summary>
        protected bool ReadyToFire(float deltaTime)
        {
            if (fireInterval <= 0f || ShotsDisabled)
            {
                return false;
            }
            Playground playground = Field != null ? Field.Playground : null;
            if (playground != null && !playground.Wraps && !playground.IsInside(Position, 0.3f))
            {
                return false;
            }
            fireTimer -= deltaTime;
            if (fireTimer > 0f)
            {
                return false;
            }
            fireTimer += fireInterval * Random.Range(0.8f, 1.2f);
            return true;
        }


        /// <summary>Direction to the ship (across the wrapping edges) with a random error, or a random direction.</summary>
        protected Vector2 AimAtShip(float errorDegrees)
        {
            AsteroidsPlayer target = Target;
            Vector2 direction = target != null && Field.Playground != null
                ? Field.Playground.Delta(Position, target.Position).normalized
                : Random.insideUnitCircle.normalized;
            float error = Random.Range(-errorDegrees, errorDegrees);
            return Quaternion.Euler(0f, 0f, error) * direction;
        }


        protected void Fire(Vector2 direction)
        {
            if (Field == null || Field.Spawner == null)
            {
                return;
            }
            Field.Spawner.FireEnemyShot(shotKind, Position + direction * (radius + 0.2f), direction * shotSpeed);
            if (Field.Sounds != null)
            {
                Field.Sounds.EnemyShot();
            }
        }


        // ------------------------------------------------------------------ strike

        /// <summary>
        /// Whether a strike unit may fire now: its shots are not disabled and it is on screen (in the asteroid field, where the
        /// playground wraps, anywhere).
        /// </summary>
        protected bool CanFireStrike
        {
            get
            {
                if (ShotsDisabled || Field == null)
                {
                    return false;
                }
                Playground playground = Field.Playground;
                return playground == null || playground.Wraps || playground.IsInside(Position, 0.3f);
            }
        }


        /// <summary>
        /// Whether a strike unit warns of its next attack right now (a laser tower's glow, an interceptor waiting to come
        /// in). The simulator reports it with the body (<see cref="BodyFlags.Warning"/>), so every pilot gets the warning.
        /// </summary>
        internal virtual bool IsTelegraphing => false;


        /// <summary>
        /// A puppet shows the warning its simulator reports. By default a warning that starts marks the edge where the
        /// enemy will come in, with the warning sound (an interceptor waiting off screen); a unit with a glow shows that.
        /// </summary>
        internal virtual void ShowWarning(bool warning)
        {
            if (warning && !warningShown && Field != null && Field.Playground != null)
            {
                Field.Effects?.Telegraph(Field.Playground.Clamp(Position, 1f), radius * 1.5f, explosionTint);
                Field.Sounds?.Warning();
            }
            warningShown = warning;
        }


        /// <summary>
        /// Where a shot leaves <paramref name="muzzle"/> in the plane of the playfield. A muzzle under a depth-placed visual
        /// (a ground unit's, see <see cref="DepthLayer"/>) is drawn scaled away from the camera, so its offset from the
        /// anchor is scaled back; the unit's own position when there is no muzzle.
        /// </summary>
        protected Vector2 MuzzlePoint(Transform muzzle)
        {
            if (muzzle == null)
            {
                return Position;
            }
            DepthAnchor anchor = muzzle.GetComponentInParent<DepthAnchor>();
            if (anchor == null)
            {
                Vector3 world = muzzle.position;
                return new Vector2(world.x, world.y);
            }
            float scale = anchor.transform.lossyScale.x;
            Vector3 offset = muzzle.position - anchor.transform.position;
            return Position + new Vector2(offset.x, offset.y) / (Mathf.Abs(scale) > 0.001f ? scale : 1f);
        }


        /// <summary>
        /// Fires one volley of a strike attack (design 1.4): <paramref name="shots"/> shots of <paramref name="kind"/> from
        /// <paramref name="muzzles"/> in turn (side by side from the centre when there are none). Aimed patterns fly along
        /// <paramref name="aim"/> when given (a turret's heading), else at the nearest pilot; the others straight down the
        /// screen, fanned for Angled and Spread. A laser is a beam that moves with its gun (<paramref name="carrier"/>, the
        /// gun's own velocity on screen); sky mines drift down. Only the simulator fires.
        /// </summary>
        protected void FireStrikeVolley(AttackPattern pattern, EnemyShotKind kind, int shots, Transform[] muzzles, Vector2? aim, Vector2 carrier)
        {
            SpawnService spawner = Field != null ? Field.Spawner : null;
            if (spawner == null || pattern == AttackPattern.None || IsPuppet)
            {
                return;
            }
            if (pattern == AttackPattern.Laser)
            {
                kind = EnemyShotKind.Beam;
            }
            shots = Mathf.Max(1, shots);
            int muzzleCount = muzzles != null ? muzzles.Length : 0;
            EnemyShotInfo info = StrikeUnitRules.Shot(kind);
            float speed = info.Speed > 0f ? info.Speed : shotSpeed;
            AsteroidsPlayer target = StrikeGun.Aims(pattern) && !aim.HasValue ? Target : null;
            for (int i = 0; i < shots; i++)
            {
                Transform muzzle = muzzleCount > 0 ? muzzles[i % muzzleCount] : null;
                Vector2 origin = muzzle != null ? MuzzlePoint(muzzle) : Position + new Vector2(StrikeGun.Lateral(i, shots, radius), 0f);
                if (pattern == AttackPattern.Laser)
                {
                    spawner.FireEnemyShot(kind, origin, carrier);
                    continue;
                }
                Vector2 direction = Vector2.down;
                if (StrikeGun.Aims(pattern))
                {
                    if (aim.HasValue && aim.Value.sqrMagnitude > 0.0001f)
                    {
                        direction = aim.Value.normalized;
                    }
                    else if (target != null && (target.Position - origin).sqrMagnitude > 0.0001f)
                    {
                        direction = (target.Position - origin).normalized;
                    }
                    direction = StrikeGun.Rotate(direction, Random.Range(-aimError, aimError));
                }
                direction = StrikeGun.Rotate(direction, StrikeGun.FanAngle(pattern, i, shots));
                if (muzzle == null)
                {
                    origin += direction * (radius * 0.5f);
                }
                spawner.FireEnemyShot(kind, origin, direction * speed);
                if (muzzle != null)
                {
                    Field.Effects?.Muzzle(origin);
                }
            }
            if (Field.Sounds == null)
            {
                return;
            }
            switch (pattern)
            {
                case AttackPattern.Laser:
                    Field.Sounds.LaserZap();
                    break;
                case AttackPattern.Rockets:
                    Field.Sounds.StrikeMissileLaunch();
                    break;
                default:
                    Field.Sounds.EnemyShot();
                    break;
            }
        }


        /// <summary>Turns the root to face along <paramref name="direction"/> (the model's nose is +Y).</summary>
        protected void Face(Vector2 direction, float deltaTime, float degreesPerSecond)
        {
            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            Quaternion target = Quaternion.Euler(0f, 0f, angle);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, degreesPerSecond * deltaTime);
        }


        protected override void OnDestroyed(DamageInfo hit)
        {
            if (Field == null)
            {
                return;
            }
            if (Field.Effects != null)
            {
                Field.Effects.Explosion(Position, explosionScale, explosionTint);
            }
            if (Field.Sounds != null)
            {
                Field.Sounds.EnemyExplode();
            }
            if (Field.CameraRig != null)
            {
                Field.CameraRig.Shake(0.25f);
            }
        }
    }
}
