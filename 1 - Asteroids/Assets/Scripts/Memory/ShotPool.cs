using Gamebox;

namespace Portfolio.Asteroids
{
    /// <summary>Pool of one kind of projectile (a blaster bolt, a missile, an enemy plasma ball...).</summary>
    public class ShotPool : PrefabPool<Shot>, IBodyPool
    {
        public void Release(SpaceBody body)
        {
            Recycle(body as Shot);
        }
    }
}
