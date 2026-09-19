using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>A crystal: points, and one step toward a collection objective.</summary>
    public class PointReward : Reward
    {
        [field: SerializeField]
        public int PointsAward { get; private set; } = 50;


        public override void Award(AsteroidsPlayer player)
        {
            player.AwardPoints(this);
        }
    }
}
