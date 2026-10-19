using System;
using Gamebox;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// Online races: the menu's Play Online opens the lobby of the game's server (the scene's
    /// <see cref="DeathRaceOnlineController"/>, the manager's <see cref="BaseGameManager.OnlineLobby"/>). When the host of a
    /// room starts, the controller builds the room's race and runs it through <see cref="BeginRace"/> like any other,
    /// with the people at this device as people and everybody else as cars of other devices. The race ends here when the
    /// people at this device are done (the director's end); the standings wait for the room, which ranks every racer when
    /// the last one reported, and come back through <see cref="ShowOnlineResults"/>.
    /// </summary>
    public partial class DeathRaceManager
    {
        /// <summary>The online controller of the scene, or null.</summary>
        public DeathRaceOnlineController OnlineRaces => online as DeathRaceOnlineController;

        /// <summary>The menu's Play Online: the lobby, unless a race of people is on here.</summary>
        public override void OpenOnline()
        {
            if (IsRacing && !InSession)
            {
                return;
            }
            Sounds?.Click();
            base.OpenOnline();
        }

        /// <summary>
        /// The people at this device are done with the online race (over the line, or wrecked) and the cars of the others
        /// had their moment: the cars cruise on behind the standings, which the room sends when everybody reported.
        /// </summary>
        partial void OnOnlineRaceFinished(RaceResult result)
        {
        }

        /// <summary>The standings of an online race as the room ranked it, then back to the room.</summary>
        public void ShowOnlineResults(RaceResult standings, Action backToRoom)
        {
            if (standings == null)
            {
                backToRoom?.Invoke();
                return;
            }
            ResultsWindow.For(this).ShowOnline(standings, backToRoom);
        }
    }
}
