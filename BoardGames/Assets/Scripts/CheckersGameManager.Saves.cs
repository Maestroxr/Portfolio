using Gamebox;

namespace Portfolio.Checkers
{
    /// <summary>
    /// Named saves: a game at this device saves itself after every move of a person (the autosave) and on the way to
    /// the title, and the player saves it by name from the menu; Load Game continues the latest. A saved game holds
    /// the table it was set up with, the position and the campaign level (<see cref="SaveData"/>). A game online can be
    /// saved by name too (every device holds the whole table), and a room can play it on later.
    /// </summary>
    public partial class CheckersGameManager
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
            var data = new SaveData { setup = Setup.Clone(), game = Game.Clone(), campaignLevel = campaignLevel };
            string summary = L.F("{0}, move {1}: {2} vs {3}", L.T(Setup.Rules.NameKey), Game.ply / 2 + 1, NameOf(0), NameOf(1));
            return new SaveContent { Data = data, Level = Setup.variant, Summary = summary };
        }

        /// <summary>Whether a save holds a whole game: two seats and a board of the variant's size.</summary>
        private static bool SaveIsWhole(SaveData data)
        {
            if (data == null || data.game == null || data.game.board == null || data.game.board.cells == null || data.setup == null || data.setup.seats == null || data.setup.seats.Length != 2)
            {
                return false;
            }
            VariantRules rules = VariantRules.Of(data.game.variant);
            return data.game.board.size == rules.Size && data.game.board.cells.Length == rules.Squares && data.setup.seats[0] != null && data.setup.seats[1] != null;
        }

        protected override string RestoreSave(SavedGame save)
        {
            if (InSession || IsOnlineGame)
            {
                return L.T("A saved game cannot be loaded during an online game.");
            }
            SaveData data = save != null ? save.Read<SaveData>() : null;
            if (!SaveIsWhole(data))
            {
                return L.T("The saved game is incomplete.");
            }
            if (data.game.IsOver)
            {
                return L.T("The saved game is already over.");
            }
            if (table != null)
            {
                EndTable();
                EndLocalMatch();
            }
            data.setup.variant = data.game.variant;
            int level = campaign != null && data.campaignLevel >= 0 && data.campaignLevel < campaign.Count ? data.campaignLevel : -1;
            pendingSetup = data.setup;
            pendingSaved = data.game;
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
