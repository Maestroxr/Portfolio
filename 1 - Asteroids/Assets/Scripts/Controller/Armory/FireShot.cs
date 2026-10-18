using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// A patch of burning ground left by a napalm canister: it lies on the ground (the scroll carries it down), and every
    /// <see cref="TickInterval"/> seconds of its lifetime it burns every target of its <see cref="burns"/> layers that it
    /// touches for its <see cref="Shot.Damage"/>. The field's shot collisions never hit with it (its reach is none): it
    /// does its own burning. Its flames die down over the last moments. A ghost (another pilot's fire) only shows.
    /// </summary>
    public class FireShot : Shot
    {
        /// <summary>Seconds between two burns of everything the fire touches.</summary>
        public const float TickInterval = 0.25f;

        [Header("Fire")]
        [Tooltip("The layers the fire burns.")]
        [SerializeField] internal Altitude burns = Altitude.Ground;
        [Tooltip("The flames, which die down at the end.")]
        [SerializeField] internal ParticleSystem flames;
        [Tooltip("Seconds over which the flames die down at the end of the lifetime.")]
        [SerializeField] internal float fadeTime = 0.6f;

        private static readonly List<Shootable> Burning = new List<Shootable>(16);
        private float nextBurn;
        private float baseRate = -1f;


        /// <summary>Whether a target of <paramref name="targetRadius"/> at <paramref name="target"/> is in a fire of <paramref name="fireRadius"/> at <paramref name="fire"/>.</summary>
        public static bool Touches(Vector2 fire, float fireRadius, Vector2 target, float targetRadius)
        {
            float reach = fireRadius + targetRadius * 0.5f;
            return (target - fire).sqrMagnitude <= reach * reach;
        }


        public override void OnSpawned()
        {
            base.OnSpawned();
            nextBurn = 0.05f;
            if (flames != null)
            {
                ParticleSystem.EmissionModule emission = flames.emission;
                if (baseRate < 0f)
                {
                    baseRate = emission.rateOverTimeMultiplier;
                }
                emission.rateOverTimeMultiplier = baseRate;
            }
        }


        public override void Tick(float deltaTime)
        {
            // The fire stays where it fell: the ground carries it.
            Velocity = Field != null ? Field.ScrollVelocity : Vector2.zero;
            base.Tick(deltaTime);
            if (!InPlay)
            {
                return;
            }
            DieDown();
            nextBurn -= deltaTime;
            if (nextBurn > 0f)
            {
                return;
            }
            nextBurn += TickInterval;
            if (!IsGhost)
            {
                Burn();
            }
        }


        private void Burn()
        {
            SpaceField field = Field;
            if (field == null || damage <= 0f)
            {
                return;
            }
            int? seat = FiredBy != null ? FiredBy.HitSeat : null;
            IReadOnlyList<Shootable> targets = field.Targets;
            Burning.Clear();
            for (int i = 0; i < targets.Count; i++)
            {
                Shootable target = targets[i];
                if (target != null && target.IsAlive && (target.Altitude & burns) != 0 && Touches(Position, radius, target.Position, target.Radius) &&
                    field.IsTargetable(target))
                {
                    Burning.Add(target);
                }
            }
            foreach (Shootable target in Burning)
            {
                Vector2 direction = (target.Position - Position).sqrMagnitude > 0.0001f ? (target.Position - Position).normalized : Vector2.up;
                target.TakeHit(new DamageInfo(damage, direction, target.Position, DamageSource.PlayerShot, true, this) { Seat = seat });
            }
            Burning.Clear();
        }


        private void DieDown()
        {
            if (flames == null || baseRate < 0f || Lifetime <= 0f)
            {
                return;
            }
            float left = Lifetime - Age;
            ParticleSystem.EmissionModule emission = flames.emission;
            emission.rateOverTimeMultiplier = baseRate * Mathf.Clamp01(left / Mathf.Max(0.01f, fadeTime));
        }
    }
}
