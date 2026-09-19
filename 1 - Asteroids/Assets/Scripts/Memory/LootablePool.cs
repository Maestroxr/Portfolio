using Gamebox;

namespace Portfolio.Asteroids
{
    /// <summary>Pool of supply pods.</summary>
    public class LootablePool : PrefabPool<Lootable>, IBodyPool
    {
        public void Release(SpaceBody body)
        {
            Recycle(body as Lootable);
        }
    }
}
