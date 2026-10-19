using System.Collections.Generic;
using Gamebox;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// Split screen: two to four people at this device, computer cars filling the grid (BaseGame's local play). The menu's
    /// Split Screen button opens the shared setup with <see cref="DeathRaceLocalRules"/> (the tracks this device has
    /// opened, the laps, the car, the computer cars); its Start hands the match here, which builds the race
    /// (<see cref="DeathRaceLocalRules.BuildSetup"/>) and runs it like every other race: the director gives every person
    /// a view of their own and a HUD over it. When it is over the results show the places with Race Again (the same
    /// people, a new race) and Menu, which ends the match; leaving the race from the pause menu ends it too. A split
    /// screen race records the stars and best laps of the track like any race at this device.
    /// </summary>
    public partial class DeathRaceManager
    {
        private DeathRaceLocalRules localRules;
        private bool watchingLocalState;

        /// <summary>The rules of a split screen race (made again when the setup opens, so newly opened tracks are in).</summary>
        public override LocalPlayRules LocalPlay => localRules ??= CreateLocalRules();

        /// <summary>The split screen race being run or just run, or null.</summary>
        public RaceSetup LocalRace { get; private set; }

        /// <summary>The menu's Split Screen: the setup, unless a race of people or an online game is on.</summary>
        public override void OpenLocalPlay()
        {
            if (IsRacing || InSession)
            {
                return;
            }
            localRules = CreateLocalRules();
            Sounds?.Click();
            base.OpenLocalPlay();
        }

        /// <summary>The setup's Start: the race of the match.</summary>
        protected override void OnLocalMatchBegun(LocalMatch match)
        {
            if (!watchingLocalState)
            {
                watchingLocalState = true;
                StateEntered += OnLocalStateEntered;
            }
            BeginLocalRace(match);
        }

        /// <summary>The match is over (Menu on the results, or the race was left).</summary>
        protected override void OnLocalMatchEnded(LocalMatch match)
        {
            LocalRace = null;
            if (watchingLocalState)
            {
                watchingLocalState = false;
                StateEntered -= OnLocalStateEntered;
            }
        }

        /// <summary>Builds and starts a race of <paramref name="match"/>, with a fresh seed (a mixed grid draws its cars again).</summary>
        public void BeginLocalRace(LocalMatch match)
        {
            DeathRaceLocalRules rules = match != null ? match.Rules as DeathRaceLocalRules : null;
            if (rules == null)
            {
                return;
            }
            int seed = System.Environment.TickCount & 0x7FFFFFFF;
            LocalRace = rules.BuildSetup(match, seed, L.Data, DriverColor);
            BeginRace(LocalRace);
        }

        /// <summary>The split screen race is over: the places, then the same people again or the menu.</summary>
        partial void OnLocalRaceFinished(RaceResult result)
        {
            if (!InLocalMatch)
            {
                return;
            }
            bool personWon = false;
            foreach (RacerResult racer in result.Racers)
            {
                personWon |= racer.Driver == DriverKind.Human && racer.Place == 1 && !racer.Wrecked;
            }
            TransitionState(personWon ? BaseGameState.Victory : BaseGameState.GameOver);
            ResultsWindow.For(this).ShowLocal(result, () => BeginLocalRace(LocalMatch), () => TransitionState(BaseGameState.Initialization));
        }

        /// <summary>Back on the menu (the results' Menu, or the pause menu's way out): the split screen match ends.</summary>
        private void OnLocalStateEntered(GameState state)
        {
            if (state != null && state.BaseState == BaseGameState.Initialization && InLocalMatch)
            {
                EndLocalMatch();
            }
        }

        /// <summary>The rules with the tracks this device has opened and the look's seat colours.</summary>
        private DeathRaceLocalRules CreateLocalRules()
        {
            return new DeathRaceLocalRules(OpenTracks(), SeatColors());
        }

        /// <summary>The tracks a race at this device may run: those the campaign has opened on this device's progress.</summary>
        public List<int> OpenTracks()
        {
            var open = new List<int>();
            for (int track = 0; track < TrackLayouts.Count; track++)
            {
                if (campaign == null || campaign.IsUnlocked(track, Progress))
                {
                    open.Add(track);
                }
            }
            return open;
        }

        private Color[] SeatColors()
        {
            DeathRaceTheme theme = Look;
            if (theme == null || theme.seatColors == null || theme.seatColors.Length == 0)
            {
                return null;
            }
            var colors = new Color[DeathRaceLocalRules.GridSize];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = theme.SeatColor(i);
            }
            return colors;
        }
    }
}
