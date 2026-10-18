using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// A shield generator of the strike mode: an unarmed ground unit whose dome (<see cref="StrikeCampaignRules.ShieldRadius"/>)
    /// keeps the ground units under it from harm while it stands. On the simulator it marks them invulnerable every frame
    /// (another generator's dome counts too; generators never cover each other), and lets them go when it dies or leaves; the
    /// flag reaches the other pilots' puppets with the bodies. The dome shows on every client, so the pilots see what is
    /// covered: shoot the generator first.
    /// </summary>
    public class ShieldGenerator : GroundUnit
    {
        [Header("Shield generator")]
        [Tooltip("The dome over the covered ground (scaled to the shield radius by the builder); pulses while it stands.")]
        [SerializeField] internal Transform dome;

        /// <summary>The generators in play on the simulator (any field: a unit is covered by one of its own field only).</summary>
        private static readonly List<ShieldGenerator> Active = new List<ShieldGenerator>();

        private Vector3 domeScale = Vector3.one;
        private bool hasDomeScale;

        /// <summary>Whether this generator's dome covers <paramref name="unit"/>.</summary>
        public bool Covers(GroundUnit unit)
        {
            return unit != null && unit != this && !(unit is ShieldGenerator) && IsAlive && unit.Field == Field &&
                   StrikeCampaignRules.InsideShield(Position, unit.Position, unit.Radius);
        }


        /// <summary>Whether a generator in play covers <paramref name="unit"/>.</summary>
        public static bool IsCovered(GroundUnit unit)
        {
            foreach (ShieldGenerator generator in Active)
            {
                if (generator != null && generator.Covers(unit))
                {
                    return true;
                }
            }
            return false;
        }


        public override void OnSpawned()
        {
            if (!hasDomeScale && dome != null)
            {
                domeScale = dome.localScale;
                hasDomeScale = true;
            }
            base.OnSpawned();
            if (!IsPuppet && !Active.Contains(this))
            {
                Active.Add(this);
            }
            ShowDome(true);
        }


        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);
            if (!InPlay)
            {
                return;
            }
            if (dome != null && hasDomeScale)
            {
                dome.localScale = domeScale * (1f + 0.025f * Mathf.Sin(Age * 4f));
            }
            if (!IsPuppet)
            {
                Shield();
            }
        }


        /// <summary>Marks the ground units near the dome covered or not, as the generators in play decide.</summary>
        private void Shield()
        {
            if (Field == null)
            {
                return;
            }
            float reach = StrikeCampaignRules.ShieldRadius + 6f;
            IReadOnlyList<Shootable> targets = Field.Targets;
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] is GroundUnit unit && !(unit is ShieldGenerator) && unit.IsAlive && (unit.Position - Position).sqrMagnitude < reach * reach)
                {
                    unit.Invulnerable = IsCovered(unit);
                }
            }
        }


        /// <summary>The dome falls: what it covered can be hurt again unless another dome still covers it.</summary>
        private void Uncover()
        {
            Active.Remove(this);
            if (Field == null || IsPuppet)
            {
                return;
            }
            IReadOnlyList<Shootable> targets = Field.Targets;
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] is GroundUnit unit && !(unit is ShieldGenerator) && unit.Invulnerable)
                {
                    unit.Invulnerable = IsCovered(unit);
                }
            }
        }


        private void ShowDome(bool show)
        {
            if (dome != null && dome.gameObject.activeSelf != show)
            {
                dome.gameObject.SetActive(show);
            }
        }


        protected override void OnDestroyed(DamageInfo hit)
        {
            ShowDome(false);
            Uncover();
            base.OnDestroyed(hit);
        }


        protected override void OnDespawned()
        {
            Uncover();
            ShowDome(false);
            base.OnDespawned();
        }
    }
}
