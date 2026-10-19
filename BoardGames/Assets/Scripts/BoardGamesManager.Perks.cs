using System.Collections;
using System.Collections.Generic;
using Gamebox;
using Gamebox.Lockstep;
using UnityEngine;

namespace Portfolio.BoardGames
{
    /// <summary>
    /// The perks of the shop (<see cref="BoardGamesShop"/>), for the one person playing the computer at this device
    /// only: never online, where every device must agree on the table, nor between two people here. The Hint Book marks
    /// the move the computer would play at its strongest for the person to act (<see cref="RequestHint"/>): the hint
    /// thinks on a worker thread as the computer does, and shows as a selection, the piece to move picked up and its
    /// target lit, so a click on the target plays it. Take Back (<see cref="TakeBack"/>) restores the copy of the game
    /// taken before the person's last move (the computer's reply goes with it), the way a saved game is loaded.
    /// </summary>
    public partial class BoardGamesManager
    {
        /// <summary>The game before each move of the person against the computer, the latest last.</summary>
        private readonly List<IBoardGame> takeBacks = new List<IBoardGame>();
        private Coroutine hinting;
        /// <summary>The move the hint marks on the board, or null.</summary>
        private BoardCommand hint;

        /// <summary>Whether the Hint button is on: the Hint Book is owned and the person against the computer is to act.</summary>
        public bool HintOffered => BoardGamesShop.HasHintBook && IsGameRunning && WaitingForHuman && PersonVsComputer() >= 0;

        /// <summary>
        /// Whether the Take Back button is on: Take Back is owned, the person against the computer has moved, and the
        /// table is still (their turn, or the computer thinking; not while the pieces fly).
        /// </summary>
        public bool TakeBackOffered => BoardGamesShop.HasTakeBack && IsGameRunning && takeBacks.Count > 0 && PersonVsComputer() >= 0
            && !table.IsOver && !GameBusy() && (WaitingForHuman || table.IsComputer(table.SeatToAct));

        /// <summary>The move the hint marks, for the tours and the tests; null while none is shown.</summary>
        public BoardCommand ShownHint => hint;

        /// <summary>The seat of the one person playing the computer at this device, or -1 (online, two people here, or no game).</summary>
        private int PersonVsComputer()
        {
            return table != null && !IsOnlineGame ? SingleHumanSeat() : -1;
        }

        /// <summary>A table begins or ends: the hint and the games kept for a take back belong to the table that was.</summary>
        private void ForgetPerks()
        {
            StopHint();
            takeBacks.Clear();
        }

        // ------------------------------------------------------------------ the hint

        /// <summary>The Hint button: the computer at its strongest thinks for the person, and the board marks its move.</summary>
        public void RequestHint()
        {
            if (!HintOffered || hinting != null)
            {
                return;
            }
            hinting = StartCoroutine(FindHint(table.SeatToAct, version));
        }

        private void StopHint()
        {
            if (hinting != null)
            {
                StopCoroutine(hinting);
                hinting = null;
            }
            hint = null;
        }

        /// <summary>
        /// Works the hint out on a copy of the game, on a worker thread (on WebGL, which has none, at once), and shows
        /// it when the table still waits for the same decision.
        /// </summary>
        private IEnumerator FindHint(int seat, int at)
        {
            IBoardGame copy = table.Game.CloneGame();
            var thinking = new SystemRandom((int)(NewSeed() & int.MaxValue));
            BoardCommand found = null;
#if UNITY_WEBGL && !UNITY_EDITOR
            found = BoardAI.Hint(copy, seat, thinking);
#else
            System.Threading.Tasks.Task<BoardCommand> task = System.Threading.Tasks.Task.Run(() => BoardAI.Hint(copy, seat, thinking));
            while (!task.IsCompleted)
            {
                if (!Same(at))
                {
                    hinting = null;
                    yield break;
                }
                yield return null;
            }
            if (task.IsFaulted)
            {
                Debug.LogException(task.Exception != null ? task.Exception.GetBaseException() : new System.Exception("the hint failed"));
            }
            else
            {
                found = task.Result;
            }
#endif
            hinting = null;
            if (Same(at) && WaitingForHuman && table.SeatToAct == seat)
            {
                ShowHint(found);
            }
            yield break;
        }

        /// <summary>
        /// The board marks <paramref name="found"/>: a move as a selection (the piece picked up, its target lit); a
        /// backgammon decision that is no move (a roll, the cube) as a line of words.
        /// </summary>
        private void ShowHint(BoardCommand found)
        {
            hint = null;
            switch (found != null ? found.Kind : CommandKind.Resign)
            {
                case CommandKind.Move when found.path != null && found.path.Length >= 2:
                    hint = found;
                    GameShowHint(found.path[0]);
                    break;
                case CommandKind.Roll:
                    ui?.Toast(L.T("Hint: roll the dice."));
                    break;
                case CommandKind.Double:
                    ui?.Toast(L.T("Hint: offer the cube."));
                    break;
                case CommandKind.Take:
                    ui?.Toast(L.T("Hint: take the cube."));
                    break;
                case CommandKind.Drop:
                    ui?.Toast(L.T("Hint: drop the cube."));
                    break;
                default:
                    ui?.Toast(L.T("No hint right now."));
                    break;
            }
            sound?.Play(ClipKind.Click);
            UpdateHighlights();
            RaiseChanged();
        }

        /// <summary>The game played picks the piece at <paramref name="from"/> up, as a click on it would, so its marks show the hint.</summary>
        private void GameShowHint(int from)
        {
            switch (Mode)
            {
                case GameMode.Chess:
                {
                    ChessGame game = ChessGame;
                    if (game != null && from >= 0 && from < 64)
                    {
                        chessSelected = from;
                        chessTargets.Clear();
                        chessTargets.AddRange(game.LegalFrom(from));
                    }
                    break;
                }
                case GameMode.Backgammon:
                    // A play begun by hand flies back first: the hint is for the whole turn.
                    while (BackgammonCanUndo && BackgammonUndo())
                    {
                    }
                    bgSelected = from;
                    break;
                default:
                {
                    CheckersGame game = CheckersGame;
                    if (game != null && game.board != null && from >= 0 && from < game.board.cells.Length)
                    {
                        Selected = from;
                        SelectedMoves = game.LegalFrom(from);
                        PartialPath = null;
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// The square or point the hint's move ends on (the first checker's in backgammon) while the hint's piece is the
        /// one picked up (<paramref name="selected"/>), else -1. The games light it as the square under the pointer, and
        /// mark it even when the setting hides the legal moves.
        /// </summary>
        private int HintTargetFor(int selected)
        {
            if (hint == null || hint.path == null || hint.path.Length < 2 || selected < 0 || hint.path[0] != selected)
            {
                return -1;
            }
            return Mode == GameMode.Checkers ? hint.path[hint.path.Length - 1] : hint.path[1];
        }

        /// <summary>The hint's draughts move alone, to mark when the setting hides the legal moves; empty without one.</summary>
        private IReadOnlyList<Move> CheckersHintMoves(CheckersGame game)
        {
            Move move = hint != null && game != null ? game.FindLegal(hint.path) : null;
            return move != null ? new[] { move } : NoCheckersMoves;
        }

        /// <summary>Whether one of <paramref name="moves"/> ends on <paramref name="square"/>.</summary>
        private static bool EndsOn(IReadOnlyList<Move> moves, int square)
        {
            foreach (Move move in moves)
            {
                if (move != null && move.path != null && move.path.Length >= 2 && move.To == square)
                {
                    return true;
                }
            }
            return false;
        }

        // ------------------------------------------------------------------ taking back

        /// <summary>
        /// A copy of the game before <paramref name="command"/> when it is a move of the person against the computer
        /// (what a take back restores), else null. Kept whether or not Take Back is owned, so one bought during a game
        /// works at once.
        /// </summary>
        private IBoardGame BeforeTakeBack(BoardCommand command)
        {
            return command.Kind == CommandKind.Move && command.seat == PersonVsComputer() ? table.Game.CloneGame() : null;
        }

        /// <summary>
        /// The Take Back button: the game goes back to before the person's last move (the computer's reply goes with
        /// it), as if that position had been loaded, and it is the person's turn again.
        /// </summary>
        public bool TakeBack()
        {
            if (!TakeBackOffered)
            {
                return false;
            }
            IBoardGame before = takeBacks[takeBacks.Count - 1];
            takeBacks.RemoveAt(takeBacks.Count - 1);
            var earlier = new List<IBoardGame>(takeBacks);
            // Whatever the director or the hint was doing is for the table that was.
            version++;
            BeginTable(Setup.Clone(), NewSeed(), before, campaignLevel);
            takeBacks.AddRange(earlier);
            Autosave();
            ui?.Toast(L.T("The move was taken back."));
            RaiseChanged();
            return true;
        }
    }
}
