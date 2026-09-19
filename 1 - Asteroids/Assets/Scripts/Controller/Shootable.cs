using System;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// Base class of everything the ship can shoot: asteroids, mines, supply pods, enemies and bosses. Has hull points,
    /// flashes and recoils when hit, and when destroyed tells its field (which scores it and drops its loot) before the
    /// derived class breaks, explodes or drops something of its own.
    /// </summary>
    public class Shootable : SpaceBody
    {
        public delegate void ShootableShot(Shootable wasShot, Shot shot);

        /// <summary>Raised when the shootable is destroyed; the shot is null when something else destroyed it.</summary>
        public event ShootableShot OnShotEvent;

        public event Action<Shootable, DamageInfo> Destroyed;

        [SerializeField] internal float maxHealth = 1f;
        [SerializeField] internal int score = 10;
        [Tooltip("Hull damage the ship takes when it flies into this.")]
        [SerializeField] internal float contactDamage = 20f;
        [SerializeField] internal bool invulnerable;
        [Tooltip("Renderers that flash when hit.")]
        [SerializeField] internal Renderer[] flashRenderers = new Renderer[0];
        [SerializeField] internal Color flashColor = new Color(1f, 0.85f, 0.7f);

        private static MaterialPropertyBlock flashBlock;
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        private float flash;
        private float punch;
        private float ramCooldown;
        private Vector3 restScale = Vector3.one;
        private bool flashing;

        /// <summary>Hull points left; set directly only when restoring a saved game.</summary>
        public float Health { get; protected internal set; }

        public float MaxHealth => maxHealth;

        public float HealthFraction => maxHealth > 0f ? Mathf.Clamp01(Health / maxHealth) : 0f;

        public bool IsAlive => InPlay && Health > 0f;

        public virtual int Score => score;

        public virtual float ContactDamage => contactDamage;

        public bool Invulnerable
        {
            get => invulnerable;
            set => invulnerable = value;
        }

        /// <summary>Drop table rolled when the ship destroys this; null drops nothing.</summary>
        public Loot Loot { get; set; }

        /// <summary>Chance that <see cref="Loot"/> drops anything.</summary>
        public float LootChance { get; set; }

        /// <summary>Whether the current wave waits for this to be destroyed.</summary>
        public bool CountsForWave { get; set; }


        public override void OnSpawned()
        {
            base.OnSpawned();
            Health = maxHealth;
            flash = 0f;
            punch = 0f;
            ramCooldown = 0f;
            if (visual != null)
            {
                restScale = visual.localScale;
            }
            ClearFlash();
        }


        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);
            ramCooldown -= deltaTime;
            Animate(deltaTime);
        }


        /// <summary>Applies <paramref name="hit"/>. Returns true when it destroyed the shootable.</summary>
        public virtual bool TakeHit(DamageInfo hit)
        {
            if (!IsAlive || invulnerable || hit.Amount <= 0f)
            {
                return false;
            }
            Health -= hit.Amount;
            OnHit(hit);
            if (Field != null)
            {
                Field.NotifyHit(this, hit);
            }
            if (Health <= 0f)
            {
                Health = 0f;
                Die(hit);
                return true;
            }
            flash = 1f;
            punch = 1f;
            return false;
        }


        /// <summary>Kept for the original call sites: one hit by <paramref name="shot"/>.</summary>
        public virtual void WasShot(Shot shot)
        {
            float damage = shot != null ? shot.Damage : maxHealth;
            Vector2 direction = shot != null && shot.Velocity.sqrMagnitude > 0.01f ? shot.Velocity.normalized : Vector2.up;
            TakeHit(new DamageInfo(damage, direction, Position, DamageSource.PlayerShot, true, shot));
        }


        /// <summary>A hit that did not destroy it: nudges it along the hit.</summary>
        protected virtual void OnHit(DamageInfo hit)
        {
            Velocity += hit.Direction * (0.35f / Mathf.Max(0.6f, radius));
        }


        protected virtual void Die(DamageInfo hit)
        {
            if (Field != null)
            {
                Field.NotifyDestroyed(this, hit);
            }
            Destroyed?.Invoke(this, hit);
            OnShotEvent?.Invoke(this, hit.Shot);
            OnDestroyed(hit);
            Despawn();
        }


        /// <summary>What happens when it is destroyed: break apart, explode, drop something.</summary>
        protected virtual void OnDestroyed(DamageInfo hit)
        {
        }


        /// <summary>
        /// The ship flew into it (<paramref name="direction"/> points from the ship to this). A dashing ship rams hard.
        /// </summary>
        public virtual void OnRammed(AsteroidsPlayer player, Vector2 direction, bool dashing)
        {
            if (ramCooldown > 0f)
            {
                return;
            }
            ramCooldown = 0.3f;
            TakeHit(new DamageInfo(dashing ? 3f : 2f, direction, Position - direction * radius, DamageSource.Collision, true));
        }


        protected override void OnDespawned()
        {
            base.OnDespawned();
            ClearFlash();
            if (visual != null)
            {
                visual.localScale = restScale;
            }
            OnShotEvent = null;
            Destroyed = null;
            Loot = null;
            LootChance = 0f;
            CountsForWave = false;
        }


        private void Animate(float deltaTime)
        {
            if (punch > 0f && visual != null)
            {
                punch = Mathf.Max(0f, punch - deltaTime * 9f);
                visual.localScale = restScale * (1f + 0.12f * punch * punch);
            }
            if (flash > 0f)
            {
                flash = Mathf.Max(0f, flash - deltaTime * 10f);
                SetFlash(flash);
            }
            else if (flashing)
            {
                ClearFlash();
            }
        }


        private void SetFlash(float amount)
        {
            if (flashRenderers == null || flashRenderers.Length == 0)
            {
                return;
            }
            flashBlock ??= new MaterialPropertyBlock();
            flashBlock.Clear();
            flashBlock.SetColor(EmissionId, flashColor * (amount * 3f));
            foreach (Renderer target in flashRenderers)
            {
                if (target != null)
                {
                    target.SetPropertyBlock(flashBlock);
                }
            }
            flashing = true;
        }


        private void ClearFlash()
        {
            if (!flashing || flashRenderers == null)
            {
                flashing = false;
                return;
            }
            foreach (Renderer target in flashRenderers)
            {
                if (target != null)
                {
                    target.SetPropertyBlock(null);
                }
            }
            flashing = false;
        }
    }
}
