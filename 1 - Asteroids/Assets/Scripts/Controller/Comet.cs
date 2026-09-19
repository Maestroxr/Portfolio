using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// A comet streaking across the playfield after a warning. Nothing stops it: shots burn up on it, asteroids in its
    /// way are smashed, and the ship takes a heavy hit if it is caught in the path.
    /// </summary>
    public class Comet : SpaceBody
    {
        [SerializeField] internal float damage = 45f;
        [SerializeField] internal float speed = 19f;
        [SerializeField] internal ParticleSystem trail;

        private bool hitShip;

        public float Speed => speed;


        public override void OnSpawned()
        {
            base.OnSpawned();
            wraps = false;
            hitShip = false;
            if (trail != null)
            {
                trail.Clear(true);
                trail.Play(true);
            }
        }


        /// <summary>Starts the run from <paramref name="from"/> along <paramref name="direction"/>.</summary>
        public void Launch(Vector2 from, Vector2 direction)
        {
            Position = from;
            Velocity = direction.normalized * speed;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }


        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);
            if (!InPlay || Field == null)
            {
                return;
            }
            var targets = Field.Targets;
            for (int i = 0; i < targets.Count; i++)
            {
                Shootable target = targets[i];
                if (!target.IsAlive || target is Boss)
                {
                    continue;
                }
                float reach = radius + target.Radius;
                Vector2 delta = target.Position - Position;
                if (delta.sqrMagnitude <= reach * reach)
                {
                    target.TakeHit(new DamageInfo(99f, Velocity.normalized, target.Position, DamageSource.Comet, false));
                }
            }
        }


        /// <summary>The ship is in the comet's path.</summary>
        public void HitPlayer(AsteroidsPlayer player)
        {
            if (hitShip)
            {
                return;
            }
            Vector2 away = (player.Position - Position).normalized;
            if (player.TakeDamage(new DamageInfo(damage, away, Position, DamageSource.Comet, false)))
            {
                hitShip = true;
                player.Push(away * 9f + Velocity.normalized * 4f);
            }
        }


        protected override void OnDespawned()
        {
            base.OnDespawned();
            if (trail != null)
            {
                trail.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }
}
