using Gamebox;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// The controller of a race at this device: the menu's requests go to the manager directly (Prepare loads the track
    /// and starts a quick race on it). Online play has its own controller.
    /// </summary>
    public sealed class DeathRaceController : OfflineGameController
    {
        public DeathRaceManager Manager => BaseManager as DeathRaceManager;
    }
}
