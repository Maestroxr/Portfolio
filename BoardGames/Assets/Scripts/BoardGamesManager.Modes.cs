using Gamebox;
using UnityEngine;

namespace Portfolio.BoardGames
{
    /// <summary>
    /// What the three games share at the manager: which game the table plays (or the title shows), the game of the
    /// table by its own type, and the one way a decision of a person here reaches the table (<see cref="TrySend"/>).
    /// Each game's own half of the manager is a file of its own (BoardGamesManager.Checkers.cs, .Chess.cs,
    /// .Backgammon.cs) with the same hooks, prefixed by the game: the shared code calls the hooks of the game played
    /// through the dispatchers below, always on the main thread.
    /// </summary>
    public partial class BoardGamesManager
    {
        /// <summary>The game the table plays, or the title shows between games.</summary>
        public GameMode Mode => table != null ? table.Mode : TitleMode;

        /// <summary>The game the title shows and its buttons play (the settings remember it).</summary>
        public GameMode TitleMode => (GameMode)Mathf.Clamp(Options != null ? Options.mode : 0, 0, RuleSets.Modes.Length - 1);

        /// <summary>The game of the table, whichever it is; null between games.</summary>
        public IBoardGame Current => table != null ? table.Game : null;

        /// <summary>The draughts game of the table, or null.</summary>
        public CheckersGame CheckersGame => (table as CheckersTable)?.Checkers;

        /// <summary>The chess game of the table, or null.</summary>
        public ChessGame ChessGame => (table as ChessTable)?.Chess;

        /// <summary>The backgammon game of the table, or null.</summary>
        public BackgammonGame BackgammonGame => (table as BackgammonTable)?.Backgammon;

        /// <summary>Where the title's game is remembered between sessions, whichever settings are active.</summary>
        private string TitleModeKey => $"{GameIdentifier}.TitleMode";

        /// <summary>
        /// Sends a decision of the person to act here (a move, a roll, the cube) when the table waits for them: straight
        /// to the table offline, to the room online (the board shows it when the server echoes it). The way every game's
        /// input and the tours act.
        /// </summary>
        public bool TrySend(BoardCommand command)
        {
            if (!WaitingForHuman || table == null || command == null || command.seat != table.SeatToAct)
            {
                return false;
            }
            bool sent = Commands != null && Commands.Send(command);
            if (sent && IsOnlineGame)
            {
                // The command is on its way to the server; what it did shows when it comes back.
                version++;
                WaitingForHuman = false;
                ClearSelection();
                UpdateHighlights();
                RaiseChanged();
            }
            else if (!sent)
            {
                sound?.Play(ClipKind.Click);
            }
            return sent;
        }

        // ------------------------------------------------------------------ input and the title

        /// <summary>What lies under <paramref name="ray"/> for the game shown: a square, a point of the backgammon board, or -1.</summary>
        public int PickTarget(Ray ray)
        {
            switch (Mode)
            {
                case GameMode.Chess:
                    return ChessPick(ray);
                case GameMode.Backgammon:
                    return BackgammonPick(ray);
                default:
                    return CheckersPick(ray);
            }
        }

        /// <summary>A click or tap on <paramref name="target"/> (-1 for nothing): the game played reacts while it waits for a person here.</summary>
        public void TargetClicked(int target)
        {
            if (!WaitingForHuman || table == null || table.IsOver)
            {
                return;
            }
            switch (Mode)
            {
                case GameMode.Chess:
                    ChessClicked(target);
                    break;
                case GameMode.Backgammon:
                    BackgammonClicked(target);
                    break;
                default:
                    CheckersClicked(target);
                    break;
            }
        }

        /// <summary>The pointer is over <paramref name="target"/> (-1 for none); only -1 arrives while nobody here is to act.</summary>
        public void TargetHovered(int target)
        {
            if (table == null || (!WaitingForHuman && target >= 0))
            {
                return;
            }
            switch (Mode)
            {
                case GameMode.Chess:
                    ChessHovered(target);
                    break;
                case GameMode.Backgammon:
                    BackgammonHovered(target);
                    break;
                default:
                    CheckersHovered(target);
                    break;
            }
        }

        /// <summary>
        /// The title's mode tabs: the title shows <paramref name="mode"/> from now on, and its buttons play it. The
        /// settings remember the choice. Only on the title.
        /// </summary>
        public void SelectMode(GameMode mode)
        {
            BoardGamesSettings options = Options;
            if (table != null || InSession || options == null)
            {
                return;
            }
            int index = Mathf.Clamp((int)mode, 0, RuleSets.Modes.Length - 1);
            if (options.mode == index)
            {
                return;
            }
            options.mode = index;
            RememberTitleMode(index);
            sound?.Play(ClipKind.Click);
            ShowIdleBoard();
            RaiseChanged();
        }

        /// <summary>Writes the title's game to the disk, and into the custom settings when they are the active ones.</summary>
        private void RememberTitleMode(int index)
        {
            IStorageStrategy disk = Disk;
            if (disk == null)
            {
                return;
            }
            disk.SetInt(TitleModeKey, index);
            if (!UsingDefaultSettings && CustomSettings is BoardGamesSettings custom && custom.AreSettingsValid(out _))
            {
                custom.mode = index;
                custom.SaveSettings(disk, CustomSettingsKey);
            }
        }

        /// <summary>At start: the title shows the game it showed last time.</summary>
        private void RecallTitleMode()
        {
            IStorageStrategy disk = Disk;
            BoardGamesSettings options = Options;
            if (disk != null && options != null && disk.DoesKeyExist(TitleModeKey))
            {
                options.mode = Mathf.Clamp(disk.GetInt(TitleModeKey, options.mode), 0, RuleSets.Modes.Length - 1);
            }
        }

        /// <summary>The second line of a seat's panel, by the game played (pieces taken, material, pips...).</summary>
        public string SeatLine(int seat)
        {
            if (table == null)
            {
                return "";
            }
            switch (Mode)
            {
                case GameMode.Chess:
                    return ChessSeatLine(seat) ?? "";
                case GameMode.Backgammon:
                    return BackgammonSeatLine(seat) ?? "";
                default:
                    return CheckersSeatLine(seat) ?? "";
            }
        }

        /// <summary>The side a seat plays, in the words of the game and the language shown (Light/Dark, White/Black).</summary>
        public string SideName(int seat)
        {
            TableSetup setup = Setup;
            if (setup == null || seat < 0 || seat >= setup.seats.Length)
            {
                return "";
            }
            return L.T(Sides.NameKey(setup.SideOf(seat), Mode));
        }

        // ------------------------------------------------------------------ the hooks of the game played

        /// <summary>The table's game started (new or saved): it shows its position at once and frames the camera for play.</summary>
        private void GameBegin()
        {
            switch (Mode)
            {
                case GameMode.Chess:
                    ChessBegin();
                    break;
                case GameMode.Backgammon:
                    BackgammonBegin();
                    break;
                default:
                    CheckersBegin();
                    break;
            }
        }

        /// <summary><paramref name="mode"/> hides its objects, stops its animations and forgets its input state.</summary>
        private void GameEnd(GameMode mode)
        {
            switch (mode)
            {
                case GameMode.Chess:
                    ChessEnd();
                    break;
                case GameMode.Backgammon:
                    BackgammonEnd();
                    break;
                default:
                    CheckersEnd();
                    break;
            }
        }

        /// <summary>Every game but <paramref name="keep"/> ends; the square board hides when backgammon is to show.</summary>
        private void EndGamesExcept(GameMode keep)
        {
            foreach (GameMode mode in RuleSets.Modes)
            {
                if (mode != keep)
                {
                    GameEnd(mode);
                }
            }
            if (keep == GameMode.Backgammon)
            {
                board?.SetVisible(false);
            }
        }

        /// <summary>The title's game shows its starting position behind the menu.</summary>
        private void GameShowIdle(GameMode mode)
        {
            switch (mode)
            {
                case GameMode.Chess:
                    ChessShowIdle();
                    break;
                case GameMode.Backgammon:
                    BackgammonShowIdle();
                    break;
                default:
                    CheckersShowIdle();
                    break;
            }
        }

        /// <summary>The camera frames the game played from <paramref name="bottom"/>'s side.</summary>
        private void GameFrame(Side bottom, bool immediate)
        {
            switch (Mode)
            {
                case GameMode.Chess:
                    ChessFrame(bottom, immediate);
                    break;
                case GameMode.Backgammon:
                    BackgammonFrame(bottom, immediate);
                    break;
                default:
                    CheckersFrame(bottom, immediate);
                    break;
            }
        }

        /// <summary>The director waits for the person at <paramref name="seat"/> here from now on.</summary>
        private void GameTurnStarted(int seat)
        {
            switch (Mode)
            {
                case GameMode.Chess:
                    ChessTurnStarted(seat);
                    break;
                case GameMode.Backgammon:
                    BackgammonTurnStarted(seat);
                    break;
                default:
                    CheckersTurnStarted(seat);
                    break;
            }
        }

        /// <summary>The table took <paramref name="command"/> (a command of the game itself): the game shows it.</summary>
        private void GameEntry(BoardCommand command)
        {
            switch (Mode)
            {
                case GameMode.Chess:
                    ChessEntry(command);
                    break;
                case GameMode.Backgammon:
                    BackgammonEntry(command);
                    break;
                default:
                    CheckersEntry(command);
                    break;
            }
        }

        /// <summary>The game played forgets its selection and partial moves (and the hint shown with them).</summary>
        private void ClearSelection()
        {
            hint = null;
            switch (Mode)
            {
                case GameMode.Chess:
                    ChessClearSelection();
                    break;
                case GameMode.Backgammon:
                    BackgammonClearSelection();
                    break;
                default:
                    CheckersClearSelection();
                    break;
            }
        }

        /// <summary>The game played redraws its marks (no game on the table: nothing to mark).</summary>
        private void UpdateHighlights()
        {
            if (table == null)
            {
                return;
            }
            switch (Mode)
            {
                case GameMode.Chess:
                    ChessHighlights();
                    break;
                case GameMode.Backgammon:
                    BackgammonHighlights();
                    break;
                default:
                    CheckersHighlights();
                    break;
            }
        }

        /// <summary>Whether the views of the game played still animate: the director and the local clock wait for them.</summary>
        private bool GameBusy()
        {
            switch (Mode)
            {
                case GameMode.Chess:
                    return ChessBusy();
                case GameMode.Backgammon:
                    return BackgammonBusy();
                default:
                    return CheckersBusy();
            }
        }

        /// <summary>Every game takes the look (each keeps it for when it shows).</summary>
        private void GamesApplyLook(BoardGamesTheme theme)
        {
            CheckersApplyLook(theme);
            ChessApplyLook(theme);
            BackgammonApplyLook(theme);
        }

        /// <summary>The line under the result's title for an end by the rules (not resigned, not called off).</summary>
        private string GameResultDetail(int winnerSeat)
        {
            switch (Mode)
            {
                case GameMode.Chess:
                    return ChessResultDetail(winnerSeat);
                case GameMode.Backgammon:
                    return BackgammonResultDetail(winnerSeat);
                default:
                    return CheckersResultDetail(winnerSeat);
            }
        }

        /// <summary>The game's own words for whose turn it is, or null for the shared ones.</summary>
        private string GameTurnWords(int seat)
        {
            switch (Mode)
            {
                case GameMode.Chess:
                    return ChessTurnWords(seat);
                case GameMode.Backgammon:
                    return BackgammonTurnWords(seat);
                default:
                    return CheckersTurnWords(seat);
            }
        }
    }
}
