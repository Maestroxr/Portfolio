namespace Portfolio.Asteroids
{
    /// <summary>A spare ship.</summary>
    public class LifeReward : Reward
    {
        public override void Award(AsteroidsPlayer player)
        {
            player.AwardLife();
        }
    }
}
