using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The spark of the Arc Caster (asteroid field) and of the arc lightning (strike): a fast bolt whose hit lets lightning
    /// jump on to <see cref="Chains"/> more targets, each the nearest one on the shot's layers within
    /// <see cref="ChainRange"/> of the last that the lightning has not struck yet, every jump <see cref="JumpFalloff"/> of
    /// the one before. A ghost (another pilot's spark) draws the same lightning between this client's targets and hurts
    /// nothing; a hit on a puppet goes to the simulator like any other.
    /// </summary>
    public class ArcShot : Shot
    {
        /// <summary>Every jump does this share of the damage of the one before.</summary>
        public const float JumpFalloff = 0.85f;

        [Header("Arc")]
        [Tooltip("How many more targets the lightning jumps to from the one the spark hits.")]
        [SerializeField] internal int chains = 3;
        [Tooltip("How far (m) the lightning jumps from one target to the next.")]
        [SerializeField] internal float chainRange = 5.5f;
        [Tooltip("Damage of the first jump; every further jump does 15% less.")]
        [SerializeField] internal float chainDamage = 1f;
        [SerializeField] internal Color arcTint = new Color(0.7f, 0.55f, 1f);

        private static readonly List<Shootable> Struck = new List<Shootable>(8);

        public int Chains
        {
            get => chains;
            set => chains = Mathf.Max(0, value);
        }

        public float ChainRange
        {
            get => chainRange;
            set => chainRange = Mathf.Max(0f, value);
        }

        public float ChainDamage
        {
            get => chainDamage;
            set => chainDamage = Mathf.Max(0f, value);
        }


        /// <summary>The damage of jump <paramref name="jump"/> (0 is the first) when the first does <paramref name="first"/>.</summary>
        public static float JumpDamage(float first, int jump)
        {
            return first * Mathf.Pow(JumpFalloff, Mathf.Max(0, jump));
        }


        public override void Hit(Shootable target)
        {
            // The lightning leaves before the spark's own hit, which may end the shot.
            if (InPlay && Field != null && chains > 0 && target != null)
            {
                Chain(target);
            }
            base.Hit(target);
        }


        private void Chain(Shootable first)
        {
            SpaceField field = Field;
            Altitude layers = Reach;
            int? seat = FiredBy != null ? FiredBy.HitSeat : null;
            Struck.Clear();
            Struck.Add(first);
            Shootable from = first;
            field.Effects?.Lightning(Position, first.Position, arcTint);
            for (int jump = 0; jump < chains; jump++)
            {
                Vector2 at = from.Position;
                Shootable next = field.NearestTarget(at, chainRange, candidate => (candidate.Altitude & layers) != 0 && !Struck.Contains(candidate));
                if (next == null)
                {
                    break;
                }
                Struck.Add(next);
                Vector2 to = next.Position;
                field.Effects?.Lightning(at, to, arcTint);
                float damage = JumpDamage(chainDamage, jump);
                if (!IsGhost && damage > 0f)
                {
                    Vector2 direction = (to - at).sqrMagnitude > 0.0001f ? (to - at).normalized : Vector2.up;
                    next.TakeHit(new DamageInfo(damage, direction, to - direction * next.Radius * 0.5f, DamageSource.PlayerShot, true, this) { Seat = seat });
                }
                from = next;
            }
            if (Struck.Count > 1)
            {
                field.Sounds?.Lightning();
            }
            Struck.Clear();
        }
    }
}
