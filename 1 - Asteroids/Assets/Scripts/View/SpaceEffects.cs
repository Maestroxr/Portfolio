using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// Plays the pooled effects of the game: explosions and debris, hit sparks, pickup bursts, warp-ins, shockwaves,
    /// the nova, telegraphs and the floating score numbers, and those of the strike mode (ground explosions that move
    /// with the scroll, the megabomb's flash, muzzle flashes).
    /// </summary>
    public class SpaceEffects : MonoBehaviour
    {
        [SerializeField] internal EffectPool explosion;
        [SerializeField] internal EffectPool rockDebris;
        [SerializeField] internal EffectPool iceShards;
        [SerializeField] internal EffectPool crystalShards;
        [SerializeField] internal EffectPool spark;
        [SerializeField] internal EffectPool pickup;
        [SerializeField] internal EffectPool warpIn;
        [SerializeField] internal EffectPool shockwave;
        [SerializeField] internal EffectPool nova;
        [SerializeField] internal EffectPool shipExplosion;
        [SerializeField] internal EffectPool dashTrail;
        [SerializeField] internal EffectPool telegraph;
        [SerializeField] internal PopupPool popups;
        [Header("Strike")]
        [SerializeField] internal EffectPool groundExplosion;
        [SerializeField] internal EffectPool megabombFlash;
        [SerializeField] internal EffectPool muzzle;
        [Tooltip("Effects played per frame at most; more are skipped so a chain reaction cannot flood the frame.")]
        [SerializeField] internal int budgetPerFrame = 24;

        private int frame;
        private int playedThisFrame;

        /// <summary>The playfield the effects play over (set by the field): ground effects read its scroll.</summary>
        internal SpaceField Field { get; set; }


        public void Explosion(Vector2 position, float scale, Color tint)
        {
            Play(explosion, position, scale, tint);
        }


        /// <summary>The breakup of an asteroid: debris of its kind, dust and a small flash.</summary>
        public void AsteroidBurst(Vector2 position, float radius, AsteroidKind kind)
        {
            Color tint = AsteroidRules.Tint(kind);
            switch (kind)
            {
                case AsteroidKind.Ice:
                    Play(iceShards, position, radius, tint);
                    Play(spark, position, radius * 1.2f, tint);
                    break;
                case AsteroidKind.Crystal:
                    Play(crystalShards, position, radius, tint);
                    Play(explosion, position, radius * 0.6f, tint);
                    break;
                case AsteroidKind.Magma:
                    Play(rockDebris, position, radius, new Color(0.35f, 0.25f, 0.22f));
                    break;
                default:
                    Play(rockDebris, position, radius, kind == AsteroidKind.Ore ? new Color(0.55f, 0.45f, 0.35f) : new Color(0.45f, 0.43f, 0.42f));
                    Play(explosion, position, radius * 0.45f, tint);
                    break;
            }
        }


        public void Spark(Vector2 position, Color tint, float scale)
        {
            Play(spark, position, scale, tint);
        }


        public void Pickup(Vector2 position, Color tint)
        {
            Play(pickup, position, 1f, tint);
        }


        public void WarpIn(Vector2 position, float radius, Color tint)
        {
            Play(warpIn, position, radius, tint);
        }


        public void Shockwave(Vector2 position, float radius, Color tint)
        {
            Play(shockwave, position, radius, tint);
        }


        public void Nova(Vector2 position)
        {
            Play(nova, position, 1f, new Color(0.55f, 0.85f, 1f));
        }


        public void ShipExplosion(Vector2 position, Color tint)
        {
            Play(shipExplosion, position, 1f, tint, true);
        }


        public void DashTrail(Vector2 position, Vector2 forward, Color tint)
        {
            PooledEffect effect = Play(dashTrail, position, 1f, tint);
            if (effect != null)
            {
                effect.Face(forward);
            }
        }


        /// <summary>A warning flash before a boss charges.</summary>
        public void Telegraph(Vector2 position, float radius, Color tint)
        {
            Play(telegraph, position, radius, tint);
        }


        /// <summary>An explosion on the ground: it moves down with the scroll.</summary>
        public void GroundExplosion(Vector2 at, float size)
        {
            PooledEffect effect = Play(groundExplosion, at, size, new Color(1f, 0.6f, 0.25f));
            if (effect != null && Field != null)
            {
                effect.Drift = Field.ScrollVelocity;
            }
        }


        /// <summary>The white flash of a megabomb over the whole screen.</summary>
        public void MegabombFlash()
        {
            Play(megabombFlash, Vector2.zero, 1f, Color.white, true);
        }


        /// <summary>A muzzle flash at <paramref name="at"/>.</summary>
        public void Muzzle(Vector2 at)
        {
            Play(muzzle, at, 1f, new Color(1f, 0.85f, 0.5f));
        }


        /// <summary>A floating number (or word) at <paramref name="position"/>.</summary>
        public void Popup(Vector2 position, string text, Color tint, float scale = 1f)
        {
            if (popups == null || !popups.CanDeploy)
            {
                return;
            }
            ScorePopup popup = popups.Deploy();
            popup.Pool = popups;
            popup.Show(position, text, tint, scale);
        }


        private PooledEffect Play(EffectPool pool, Vector2 position, float scale, Color tint, bool important = false)
        {
            if (pool == null || !pool.CanDeploy)
            {
                return null;
            }
            if (frame != Time.frameCount)
            {
                frame = Time.frameCount;
                playedThisFrame = 0;
            }
            if (!important && playedThisFrame >= budgetPerFrame)
            {
                return null;
            }
            playedThisFrame++;
            PooledEffect effect = pool.Deploy();
            effect.Pool = pool;
            effect.Play(new Vector3(position.x, position.y, 0f), scale, tint);
            return effect;
        }
    }
}
