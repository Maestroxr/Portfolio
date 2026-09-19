using Gamebox;

namespace Portfolio.Asteroids
{
    /// <summary>Pool of the asteroids of one kind; every size and shape of that kind comes from the same prefab.</summary>
    public class AsteroidPool : PrefabPool<Asteroid>, IBodyPool
    {
        public void Release(SpaceBody body)
        {
            Recycle(body as Asteroid);
        }
    }
}
