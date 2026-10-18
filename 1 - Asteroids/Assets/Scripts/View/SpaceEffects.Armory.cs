using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The effects of the armory's weapons: the bolts of lightning of the Arc Caster and the arc lightning (each its own
    /// pooled line, see <see cref="LightningArc"/>) and the burst of a flak shell or canister.
    /// </summary>
    public partial class SpaceEffects
    {
        /// <summary>Bolts of lightning struck per frame at most (a long chain in a crowd still shows its first jumps).</summary>
        private const int LightningPerFrame = 12;

        [Header("Armory")]
        [SerializeField] internal LightningPool lightning;
        [SerializeField] internal EffectPool flakBurst;

        private int lightningFrame;
        private int lightningThisFrame;


        /// <summary>A bolt of lightning from <paramref name="from"/> to <paramref name="to"/>.</summary>
        public void Lightning(Vector2 from, Vector2 to, Color tint)
        {
            if (lightning == null || !lightning.CanDeploy)
            {
                return;
            }
            if (lightningFrame != Time.frameCount)
            {
                lightningFrame = Time.frameCount;
                lightningThisFrame = 0;
            }
            if (lightningThisFrame >= LightningPerFrame)
            {
                return;
            }
            lightningThisFrame++;
            LightningArc arc = lightning.Deploy();
            arc.Pool = lightning;
            arc.Strike(from, to, tint);
        }


        /// <summary>The burst of a flak shell or canister at <paramref name="at"/>, <paramref name="size"/> times the authored size.</summary>
        public void FlakBurst(Vector2 at, float size, Color tint)
        {
            PooledEffect effect = Play(flakBurst, at, size, tint);
            if (effect != null && Field != null && Field.Playground != null && !Field.Playground.Wraps)
            {
                // Over the ground of a strike mission the smoke drifts down with the scroll.
                effect.Drift = Field.ScrollVelocity * 0.5f;
            }
        }
    }
}
