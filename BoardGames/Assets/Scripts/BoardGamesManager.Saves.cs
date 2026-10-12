using Gamebox;

namespace Portfolio.BoardGames
{
    /// <summary>
    /// Named saves: a game at this device saves itself after every decision of a person (the autosave) and on the way
    /// to the title, and the player saves it by name from the menu; Load Game continues the latest. A saved game holds
    /// the table it was set up with, the position of its game (draughts, chess or backgammon) and the campaign level
    /// (<see cref="SaveData"/>). A game online can be saved by name too (every device holds the whole table), and a room
    /// can play it on later.
    /// </summary>
    public partial class BoardGamesManager
    {
        protected override bool OffersNamedSaves => true;

        public override string SaveRefusal => table == null || table.IsOver ? L.T("There is no game to save.") : null;

        public override bool DoesSaveGameExist()
        {
            return HasSavedGames;
        }

        /// <summary>The menu's Save Game: the game saves itself, and says so (or why it cannot). Not online: the match menu saves by name there.</summary>
        public override void SaveGame()
        {
            if (IsOnlineGame)
            {
                return;
            }
            string refusal = SaveRefusal;
            if (refusal != null)
            {
                UI?.UpdateError(refusal);
                return;
            }
            if (Autosave())
            {
                ui?.Toast(L.T("Game saved."));
            }
        }

        /// <summary>Continue: the game saved last, by the player or by itself.</summary>
        public override void LoadGame()
        {
            ContinueLatest();
        }

        protected override SaveContent CaptureSave()
        {
            SaveData data = SaveData.Of(Setup, table.Game, campaignLevel);
            return new SaveContent { Data = data, Level = data.setup.ruleSet, Summary = SummaryOf(data.setup, table.Game) };
        }

        /// <summary>"English draughts, move 12: Ada vs Bob"; backgammon counts its turns instead ("Backgammon, turn 9: ...").</summary>
        private string SummaryOf(TableSetup setup, IBoardGame game)
        {
            string rules = L.T(RuleSets.NameKey(setup.RuleSet));
            switch (game)
            {
                case BackgammonGame backgammon:
                    return L.F("{0}, turn {1}: {2} vs {3}", rules, backgammon.Turn / 2 + 1, NameOf(0), NameOf(1));
                case ChessGame chess:
                    return L.F("{0}, move {1}: {2} vs {3}", rules, chess.ply / 2 + 1, NameOf(0), NameOf(1));
                case CheckersGame checkers:
                    return L.F("{0}, move {1}: {2} vs {3}", rules, checkers.ply / 2 + 1, NameOf(0), NameOf(1));
                default:
                    return L.F("{0}, move {1}: {2} vs {3}", rules, game.Turn + 1, NameOf(0), NameOf(1));
            }
        }

        protected override string RestoreSave(SavedGame save)
        {
            if (InSession || IsOnlineGame)
            {
                return L.T("A saved game cannot be loaded during an online game.");
            }
            SaveData data = save != null ? save.Read<SaveData>() : null;
            if (data == null || !data.IsWhole())
            {
                return L.T("The saved game is incomplete.");
            }
            IBoardGame game = data.Game;
            if (game.IsOver)
            {
                return L.T("The saved game is already over.");
            }
            if (table != null)
            {
                EndTable();
                EndLocalMatch();
            }
            data.setup.ruleSet = (int)game.RuleSet;
            int level = campaign != null && data.campaignLevel >= 0 && data.campaignLevel < campaign.Count ? data.campaignLevel : -1;
            pendingSetup = data.setup;
            pendingSaved = game;
            pendingLevel = level;
            controller.PrepareGame(LevelData.Create(level >= 0 ? level : LevelIndex));
            if (table == null)
            {
                pendingSetup = null;
                pendingSaved = null;
                pendingLevel = -1;
                return L.T("The saved game could not be started.");
            }
            ui?.Toast(L.T("Welcome back! The game goes on."));
            return null;
        }
    }
}
