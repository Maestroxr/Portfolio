using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>A shield cell: recharges the shield that absorbs hits before the hull.</summary>
    public class ShieldReward : Reward
    {
        [field: SerializeField]
        public float ShieldAward { get; private set; } = 100f;


        public override void Award(AsteroidsPlayer player)
        {
            player.RestoreShield(ShieldAward);
        }
    }
}
