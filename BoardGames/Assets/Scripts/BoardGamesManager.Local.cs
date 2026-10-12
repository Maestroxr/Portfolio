using Gamebox;

namespace Portfolio.BoardGames
{
    /// <summary>
    /// Games at this device through the shared local play setup: two people taking turns, or one against the computer,
    /// in any of the three games (<see cref="BoardGamesLocalRules"/>). The setup window and the quick game both build a <see cref="LocalMatch"/> and
    /// begin it; <see cref="OnLocalMatchBegun"/> turns it into the table of the next game and starts it through the
    /// controller, the same path a campaign level takes.
    /// </summary>
    public partial class BoardGamesManager
    {
        private BoardGamesLocalRules localRules;

        /// <summary>The rules of the setup window, with the seats in the colours of the look shown.</summary>
        public override LocalPlayRules LocalPlay
        {
            get
            {
                localRules ??= new BoardGamesLocalRules();
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
            pendingSetup = BoardGamesLocalRules.SetupOf(match);
            pendingSaved = null;
            pendingLevel = -1;
            controller.PrepareGame(CurrentLevel ?? LevelData.Create(0));
        }

        /// <summary>The seats of the local match are let go with the table (see <see cref="EndTable"/>); nothing more to tear down.</summary>
        protected override void OnLocalMatchEnded(LocalMatch match)
        {
        }

        /// <summary>
        /// Before the setup window opens: the setup it recalls plays the title's game (the draughts rules of the
        /// settings, the cube of the settings) unless it already plays a rule set of that game.
        /// </summary>
        private void PreselectLocalSetup()
        {
            LocalPlayRules rules = LocalPlay;
            BoardGamesSettings options = Options;
            LocalMatch remembered = LocalMatch.Recall(GameType.BoardGames, rules);
            RuleSet ruleSet = RuleSets.Of(TitleMode, options != null ? options.variant : 0);
            if (BoardGamesLocalRules.Preselect(remembered, ruleSet, options == null || options.doublingCube))
            {
                remembered.Remember();
            }
        }

        /// <summary>
        /// The menu's Play vs Computer: one person against the computer in the title's game, with the settings' draughts
        /// rules, cube and level. The person plays Light (White) and the rules decide who starts.
        /// </summary>
        public void StartQuickGame()
        {
            if (table != null || InSession || IsLocalSetupOpen)
            {
                return;
            }
            var match = new LocalMatch(GameType.BoardGames, LocalPlay) { Count = 2, HandOver = false };
            match[0].Kind = SeatKind.Human;
            match[0].Name = L.F("Player {0}", 1);
            match[1].Kind = SeatKind.Computer;
            match[1].Name = L.T("Computer");
            match[1].Level = Options != null ? Options.computerLevel : 1;
            match.SetOption(BoardGamesLocalRules.RuleSetOption, (int)RuleSets.Of(TitleMode, Options != null ? Options.variant : 0));
            match.SetOption(BoardGamesLocalRules.FirstOption, 0);
            match.SetOption(BoardGamesLocalRules.ClockOption, 0);
            match.SetOption(BoardGamesLocalRules.CubeOption, Options == null || Options.doublingCube ? 0 : 1);
            sound?.Play(ClipKind.Click);
            BeginLocalMatch(match);
        }
    }
}
