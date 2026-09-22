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

        /// <summary>The closest ship that is alive (there are several in a shared mission), or null.</summary>
        protected AsteroidsPlayer Target => Field != null ? Field.NearestShip(Position) : null;


        public override void OnSpawned()
        {
            base.OnSpawned();
            fireTimer = fireInterval * Random.Range(0.6f, 1.2f);
        }


        /// <summary>Counts the fire timer down; true when it is time to fire again.</summary>
        protected bool ReadyToFire(float deltaTime)
        {
            if (fireInterval <= 0f)
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
