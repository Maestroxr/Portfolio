using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>A repair kit: restores hull points.</summary>
    public class HealthReward : Reward
    {
        [field: SerializeField]
        public float HealthAward { get; private set; } = 40f;


        public override void Award(AsteroidsPlayer player)
        {
            player.AwardHealth(this);
        }
    }
}
