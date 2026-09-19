namespace Portfolio.Asteroids
{
    /// <summary>A nova bomb charge.</summary>
    public class BombReward : Reward
    {
        public override void Award(AsteroidsPlayer player)
        {
            player.AddBomb();
        }
    }
}
