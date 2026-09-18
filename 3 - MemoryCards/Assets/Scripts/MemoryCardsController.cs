using Gamebox;

namespace Portfolio.MemoryCards
{
    /// <summary>
    /// Local controller of the Memory Cards module: preparing a level deals a new hand through
    /// <see cref="MemoryCardsGameManager.StartGame"/>.
    /// </summary>
    public class MemoryCardsController : OfflineGameController
    {
        public MemoryCardsGameManager MemoryCards => BaseManager as MemoryCardsGameManager;
    }
}
