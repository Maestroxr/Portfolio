using Gamebox;

namespace Portfolio.EndlessRunner
{
    /// <summary>Local controller of the Endless Runner module; starting a level starts a new run.</summary>
    public class RunnerController : OfflineGameController
    {
        public RunnerGameManager Runner => BaseManager as RunnerGameManager;
    }
}
