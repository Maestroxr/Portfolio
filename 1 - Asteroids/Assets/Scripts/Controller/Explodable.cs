using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// Something that blows up: when it is destroyed or rammed it sets off a blast that damages every asteroid, mine and
    /// enemy around it (and the ship, if it is too close). Blasts set off other explosives, so one well placed shot
    /// can clear a crowded field. <see cref="Mine"/> and <see cref="ClusterBomb"/> add their own fuses.
    /// </summary>
    public class Explodable : Shootable
    {
        [Tooltip("Blast radius as a multiple of the mission's explosion radius setting.")]
        [SerializeField] internal float blastScale = 1f;
        [Tooltip("Damage to asteroids and enemies at the centre of the blast.")]
        [SerializeField] internal float blastDamage = 4f;
        [Tooltip("Hull damage to the ship at the centre of the blast.")]
        [SerializeField] internal float playerBlastDamage = 35f;
        [SerializeField] internal float blastPush = 6f;
        [SerializeField] internal Color blastTint = new Color(0.5f, 0.8f, 1f);

        private bool detonated;
        private int? blastSeat;

        /// <summary>Kept from the original: the blast radius in meters.</summary>
        public float BlastRadius
        {
            get
            {
                float setting = Field != null && Field.Spawner != null ? Field.Spawner.ExplosionRadius : 3f;
                return setting * blastScale;
            }
        }

        public bool HasDetonated => detonated;


        public override void OnSpawned()
        {
            base.OnSpawned();
            detonated = false;
            blastSeat = null;
        }


        /// <summary>Blows up now. <paramref name="byPlayer"/> decides whether what the blast destroys scores.</summary>
        public virtual void Detonate(bool byPlayer)
        {
            if (detonated || !InPlay || IsPuppet)
            {
                return;
            }
            detonated = true;
            if (Health > 0f)
            {
                // Destroyed by its own fuse: tell the field so the kill is counted (without points), then blow.
                Health = 0f;
                Exit = ExitReason.Destroyed;
                if (Field != null)
                {
                    Field.NotifyDestroyed(this, new DamageInfo(0f, Vector2.zero, Position, DamageSource.Hazard, false));
                }
            }
            Blow(byPlayer);
            Despawn();
        }


        protected override void OnDestroyed(DamageInfo hit)
        {
            if (detonated || IsPuppet)
            {
                // The blast of a puppet comes from the simulator, like everything it sets off.
                return;
            }
            detonated = true;
            blastSeat = hit.ByPlayer ? hit.Seat : null;
            Blow(hit.ByPlayer);
        }


        public override void OnRammed(AsteroidsPlayer player, Vector2 direction, bool dashing)
        {
            Detonate(false);
        }


        /// <summary>The blast itself; derived classes add shrapnel.</summary>
        protected virtual void Blow(bool byPlayer)
        {
            if (Field == null)
            {
                return;
            }
            Field.Explode(new Blast
            {
                Center = Position,
                Radius = BlastRadius,
                Damage = blastDamage,
                PlayerDamage = playerBlastDamage,
                Push = blastPush,
                ByPlayer = byPlayer,
                Seat = byPlayer ? blastSeat : null,
                Source = this,
                Tint = blastTint
            });
        }
    }
}
