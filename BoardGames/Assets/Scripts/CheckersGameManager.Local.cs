using Gamebox;

namespace Portfolio.Checkers
{
    /// <summary>
    /// Games at this device through the shared local play setup: two people taking turns, or one against the computer
    /// (<see cref="CheckersLocalRules"/>). The setup window and the quick game both build a <see cref="LocalMatch"/> and
    /// begin it; <see cref="OnLocalMatchBegun"/> turns it into the table of the next game and starts it through the
    /// controller, the same path a campaign level takes.
    /// </summary>
    public partial class CheckersGameManager
    {
        private CheckersLocalRules localRules;

        /// <summary>The rules of the setup window, with the seats in the colours of the look shown.</summary>
        public override LocalPlayRules LocalPlay
        {
            get
            {
                localRules ??= new CheckersLocalRules();
                localRules.Refresh(Look);
                return localRules;
            }
        }

        /// <summary>The setup window's Start (or the quick game): the table is built from the seats and the options, and the game starts.</summary>
        protected override void OnLocalMatchBegun(LocalMatch match)
        {
            if (InSession || match == null)
            {
                EndLocalMatch();
                return;
            }
            string problem = match.Problem();
            if (problem != null)
            {
                UI?.UpdateError(L.T(problem));
                EndLocalMatch();
                return;
            }
            if (table != null)
            {
                EndTable();
            }
            pendingSetup = CheckersLocalRules.SetupOf(match);
            pendingSaved = null;
            pendingLevel = -1;
            controller.PrepareGame(CurrentLevel ?? LevelData.Create(0));
        }

        /// <summary>The seats of the local match are let go with the table (see <see cref="EndTable"/>); nothing more to tear down.</summary>
        protected override void OnLocalMatchEnded(LocalMatch match)
        {
        }

        /// <summary>The menu's Play vs Computer: one person against the computer, with the rules and the level of the settings.</summary>
        public void StartQuickGame()
        {
            if (table != null || InSession || IsLocalSetupOpen)
            {
                return;
            }
            var match = new LocalMatch(GameType.Checkers, LocalPlay) { Count = 2, HandOver = false };
            match[0].Kind = SeatKind.Human;
            match[0].Name = L.F("Player {0}", 1);
            match[1].Kind = SeatKind.Computer;
            match[1].Name = L.T("Computer");
            match[1].Level = Options != null ? Options.computerLevel : 1;
            match.SetOption(CheckersLocalRules.VariantOption, Options != null ? Options.variant : 0);
            match.SetOption(CheckersLocalRules.FirstOption, 0);
            match.SetOption(CheckersLocalRules.ClockOption, 0);
            sound?.Play(ClipKind.Click);
            BeginLocalMatch(match);
        }
    }
}
