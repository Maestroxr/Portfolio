namespace Portfolio.Asteroids
{
    /// <summary>A pool a <see cref="SpaceBody"/> returns to when it leaves play.</summary>
    public interface IBodyPool
    {
        void Release(SpaceBody body);
    }
}
