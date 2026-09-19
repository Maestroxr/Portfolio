using Gamebox;

namespace Portfolio.Asteroids
{
    /// <summary>Pool of one kind of enemy (saucers, scouts, wasps).</summary>
    public class EnemyPool : PrefabPool<Enemy>, IBodyPool
    {
        public void Release(SpaceBody body)
        {
            Recycle(body as Enemy);
        }
    }
}
