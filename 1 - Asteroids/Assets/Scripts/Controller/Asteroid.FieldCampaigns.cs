using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The rocks of the field campaigns (Asteroid.FieldCampaigns.cs): iron rings and sparks as it shrugs off half of a
    /// shot (<see cref="AsteroidRules.DamageTaken"/>), and a small hydra rock swells and grows back into a medium one
    /// when nobody destroys it in time. The simulator decides the regrowth; a puppet only swells, and the medium rock
    /// arrives from the simulator.
    /// </summary>
    public partial class Asteroid
    {
        [Tooltip("The kind's own sound: a shot glancing off iron, a hydra rock growing back.")]
        [SerializeField] internal AudioClip kindClip;

        private float regrow;
        private float lastGlance;


        public override void OnSpawned()
        {
            base.OnSpawned();
            regrow = 0f;
            transform.localScale = Vector3.one;
        }


        /// <summary>Applies the hit with what the rock's kind makes of it; a puppet passes the hit on as it came.</summary>
        public override bool TakeHit(DamageInfo hit)
        {
            if (!IsPuppet && IsAlive && !Invulnerable)
            {
                float taken = AsteroidRules.DamageTaken(kind, hit.Source, hit.Amount);
                if (taken < hit.Amount)
                {
                    Glance(hit);
                }
                hit.Amount = taken;
            }
            else if (IsPuppet && kind == AsteroidKind.Metal && hit.Source == DamageSource.PlayerShot)
            {
                Glance(hit);
            }
            return base.TakeHit(hit);
        }


        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);
            if (!InPlay || !AsteroidRules.Regrows(kind, Size))
            {
                return;
            }
            regrow += deltaTime;
            float warning = regrow - (AsteroidRules.HydraRegrowSeconds - AsteroidRules.HydraWarningSeconds);
            if (warning > 0f)
            {
                // It swells and throbs before it grows back: the last chance to finish it.
                float swell = Mathf.Clamp01(warning / AsteroidRules.HydraWarningSeconds);
                transform.localScale = Vector3.one * (1f + swell * (0.35f + 0.08f * Mathf.Sin(Age * 18f)));
            }
            if (IsPuppet || regrow < AsteroidRules.HydraRegrowSeconds)
            {
                return;
            }
            Regrow();
        }


        /// <summary>A small hydra rock becomes a medium one again, unless there are too many hydra rocks already.</summary>
        private void Regrow()
        {
            SpawnService spawner = Field != null ? Field.Spawner : null;
            if (spawner == null)
            {
                return;
            }
            if (Field.CountTargets(target => target is Asteroid rock && rock.Kind == AsteroidKind.Hydra) >= AsteroidRules.HydraRegrowCap)
            {
                regrow = AsteroidRules.HydraRegrowSeconds - AsteroidRules.HydraWarningSeconds;
                transform.localScale = Vector3.one;
                return;
            }
            Asteroid grown = spawner.SpawnAsteroid(kind, AsteroidSize.Medium, Position, Velocity, CountsForWave);
            if (grown != null)
            {
                grown.Velocity = Velocity * 0.6f;
                grown.WarpedIn = true;
                Field.Effects?.WarpIn(Position, grown.Radius * 1.5f, AsteroidRules.Tint(kind));
                Field.Sounds?.Play(kindClip, 0.5f, 0.6f, 0.05f, 0.2f);
            }
            Exit = ExitReason.Expired;
            Despawn();
        }


        /// <summary>A shot glancing off iron: a bright spark and a ring, now and then.</summary>
        private void Glance(DamageInfo hit)
        {
            if (Field == null || Age - lastGlance < 0.06f && lastGlance > 0f)
            {
                return;
            }
            lastGlance = Age;
            Field.Effects?.Spark(hit.Point, new Color(0.85f, 0.92f, 1f), 0.9f);
            if (kindClip != null && kind == AsteroidKind.Metal)
            {
                Field.Sounds?.Play(kindClip, 0.35f, Random.Range(0.9f, 1.15f), 0.08f, 0.07f);
            }
        }


        public override void OnRammed(AsteroidsPlayer player, Vector2 direction, bool dashing)
        {
            base.OnRammed(player, direction, dashing);
            if (kind == AsteroidKind.Metal && InPlay && !IsPuppet)
            {
                // Iron does not break on a ram: it is knocked clear, so it cannot grind the ship hit after hit.
                Velocity += direction * 3f;
            }
        }


        protected override void OnDespawned()
        {
            base.OnDespawned();
            regrow = 0f;
            transform.localScale = Vector3.one;
        }
    }
}
