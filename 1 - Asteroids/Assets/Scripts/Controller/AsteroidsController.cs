using Gamebox;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// Local controller of the Asteroids module: starting a level resets the playfield through
    /// <see cref="AsteroidsGameManager"/> and state changes are applied to it directly.
    /// </summary>
    public class AsteroidsController : OfflineGameController
    {
        public AsteroidsGameManager Asteroids => BaseManager as AsteroidsGameManager;
    }
}
