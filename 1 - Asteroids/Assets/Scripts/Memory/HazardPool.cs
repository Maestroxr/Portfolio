using Gamebox;

namespace Portfolio.Asteroids
{
    /// <summary>Pool of one hazard that cannot be shot down (comets, gravity wells).</summary>
    public class HazardPool : PrefabPool<SpaceBody>, IBodyPool
    {
        public void Release(SpaceBody body)
        {
            Recycle(body);
        }
    }
}
