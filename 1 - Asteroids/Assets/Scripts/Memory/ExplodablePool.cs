using Gamebox;

namespace Portfolio.Asteroids
{
    /// <summary>Pool of one kind of explosive (proximity mines, cluster bombs).</summary>
    public class ExplodablePool : PrefabPool<Explodable>, IBodyPool
    {
        public void Release(SpaceBody body)
        {
            Recycle(body as Explodable);
        }
    }
}
