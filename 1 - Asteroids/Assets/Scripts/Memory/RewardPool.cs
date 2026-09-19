using Gamebox;

namespace Portfolio.Asteroids
{
    /// <summary>Pool of one pickup (a crystal, a shield cell, a weapon crate...).</summary>
    public class RewardPool : PrefabPool<Reward>, IBodyPool
    {
        public void Release(SpaceBody body)
        {
            Recycle(body as Reward);
        }
    }
}
