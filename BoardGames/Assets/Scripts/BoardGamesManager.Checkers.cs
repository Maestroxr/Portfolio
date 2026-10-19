using System;
using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.BoardGames
{
    /// <summary>
    /// The draughts half of the manager: the board view with its men and kings, the clicks that select a piece and play
    /// a move (a capture with several routes is picked square by square), the marks of the legal moves, and the words of
    /// a draughts result. The shared code calls these hooks while the table (or the title) is checkers.
    /// </summary>
    public partial class BoardGamesManager
    {
        private static readonly IReadOnlyList<Move> NoCheckersMoves = new List<Move>();

        private int checkersHovered = -1;

        /// <summary>The selected square, or -1.</summary>
        public int Selected { get; private set; } = -1;

        /// <summary>The legal moves of the selected piece (empty when none is selected).</summary>
        public IReadOnlyList<Move> SelectedMoves { get; private set; } = NoCheckersMoves;

        /// <summary>The squares clicked so far of a capture with several routes (the first is <see cref="Selected"/>), or null.</summary>
        public int[] PartialPath { get; private set; }

        /// <summary>Raised after a draughts move was applied: the board animates it, and the director waits for it.</summary>
        public event Action<Move> MovePlayed;

        // ------------------------------------------------------------------ the hooks

        private void CheckersBegin()
        {
            CheckersGame game = CheckersGame;
            if (game == null)
            {
                return;
            }
            CheckersFrame(HomeSide(), true);
            board?.Show(game, true);
        }

        /// <summary>The draughts pieces go (the squares stay for chess; backgammon hides the board) and the selection is forgotten.</summary>
        private void CheckersEnd()
        {
            CheckersClearSelection();
            checkersHovered = -1;
            if (board != null)
            {
                board.ClearPieces();
                board.SetSquareMarks(null);
            }
        }

        /// <summary>The title: an idle starting position of the quick game's draughts rules behind the menu.</summary>
        private void CheckersShowIdle()
        {
            if (board == null)
            {
                return;
            }
            var idle = new CheckersGame(VariantRules.Of(Options != null ? Options.variant : 0).Variant);
            board.Show(idle, true);
            board.SetHighlights(-1, NoCheckersMoves, null, -1, null);
            CheckersFrame(Side.Light, true);
            cameraRig?.SetTitle(true, false);
        }

        private void CheckersFrame(Side bottom, bool immediate)
        {
            CheckersGame game = CheckersGame;
            int size = game != null && game.board != null ? game.board.size : VariantRules.Of(Options != null ? Options.variant : 0).Size;
            cameraRig?.Frame(size, bottom, immediate);
        }

        private int CheckersPick(Ray ray)
        {
            return board != null ? board.SquareAt(ray) : -1;
        }

        private void CheckersClicked(int target)
        {
            SquareClicked(target);
        }

        private void CheckersHovered(int target)
        {
            SquareHovered(target);
        }

        /// <summary>Nothing to reset: every entry cleared the selection, and the highlights that follow show the pieces that may move.</summary>
        private void CheckersTurnStarted(int seat)
        {
        }

        private void CheckersEntry(BoardCommand command)
        {
            CheckersGame game = CheckersGame;
            if (command != null && command.Kind == CommandKind.Move && game != null)
            {
                MoveTaken(game.lastMove);
            }
        }

        private void CheckersClearSelection()
        {
            Selected = -1;
            SelectedMoves = NoCheckersMoves;
            PartialPath = null;
        }

        private void CheckersHighlights()
        {
            CheckersGame game = CheckersGame;
            if (board == null || game == null)
            {
                return;
            }
            bool showMoves = Options != null && Options.showMoves;
            // The hint's move is marked whatever the setting, and lit unless the pointer is on a target of its own.
            int hinted = WaitingForHuman ? HintTargetFor(Selected) : -1;
            IReadOnlyList<Move> shown = showMoves ? SelectedMoves : hinted >= 0 ? CheckersHintMoves(game) : NoCheckersMoves;
            int hovered = WaitingForHuman ? checkersHovered : -1;
            if (hinted >= 0 && !EndsOn(shown, hovered))
            {
                hovered = hinted;
            }
            IReadOnlyList<int> movable = showMoves && WaitingForHuman && Selected < 0 ? MovableSquares(game) : null;
            board.SetHighlights(Selected, shown, game.lastMove, hovered, PartialPath, movable);
        }

        private bool CheckersBusy()
        {
            return board != null && board.IsAnimating;
        }

        /// <summary>The board and its pieces take the look in the shared <see cref="ApplyLook"/> (chess plays on the same squares).</summary>
        private void CheckersApplyLook(BoardGamesTheme theme)
        {
        }

        private string CheckersResultDetail(int winnerSeat)
        {
            switch (CheckersGame != null ? CheckersGame.Reason : EndReason.None)
            {
                case EndReason.NoMoves:
                    return winnerSeat >= 0 ? L.F("{0} has no moves left.", NameOf(1 - winnerSeat)) : L.T("Nobody can move.");
                case EndReason.DrawByPlies:
                    return L.T("Fifty moves without a capture or a man moving.");
                default:
                    return "";
            }
        }

        /// <summary>How many pieces the seat has taken.</summary>
        private string CheckersSeatLine(int seat)
        {
            CheckersGame game = CheckersGame;
            TableSetup setup = Setup;
            if (game == null || setup == null || seat < 0 || seat >= setup.seats.Length)
            {
                return "";
            }
            int side = (int)setup.SideOf(seat);
            int taken = game.capturedCount != null && side < game.capturedCount.Length ? game.capturedCount[side] : 0;
            return L.F("{0} taken", taken);
        }

        private string CheckersTurnWords(int seat)
        {
            return null;
        }

        // ------------------------------------------------------------------ input

        /// <summary>A click or tap on a square: selects a piece, plays a move, or picks the route of a capture step by step.</summary>
        public void SquareClicked(int square)
        {
            CheckersGame game = CheckersGame;
            if (!WaitingForHuman || table == null || table.IsOver || game == null || square < 0 || square >= game.board.cells.Length)
            {
                return;
            }
            if (Selected >= 0 && square != Selected)
            {
                int[] prefix = PartialPath ?? new[] { Selected };
                var withPrefix = new List<Move>();
                foreach (Move move in SelectedMoves)
                {
                    if (StartsWith(move.path, prefix))
                    {
                        withPrefix.Add(move);
                    }
                }
                var ending = new List<Move>();
                var stepping = new List<Move>();
                foreach (Move move in withPrefix)
                {
                    if (move.To == square)
                    {
                        ending.Add(move);
                    }
                    if (move.path.Length > prefix.Length && move.path[prefix.Length] == square)
                    {
                        stepping.Add(move);
                    }
                }
                if (ending.Count == 1 && (stepping.Count <= 1 || ending[0].path.Length == prefix.Length + 1))
                {
                    TryPlay(ending[0]);
                    return;
                }
                if (stepping.Count > 0)
                {
                    var extended = new int[prefix.Length + 1];
                    prefix.CopyTo(extended, 0);
                    extended[prefix.Length] = square;
                    PartialPath = extended;
                    if (stepping.Count == 1 && stepping[0].path.Length == extended.Length)
                    {
                        TryPlay(stepping[0]);
                        return;
                    }
                    sound?.Play(ClipKind.Click);
                    CheckersHighlights();
                    RaiseChanged();
                    return;
                }
                if (ending.Count > 1)
                {
                    PartialPath = new[] { Selected };
                    ui?.Toast(L.T("Several routes end there: click the squares of the route."));
                    CheckersHighlights();
                    RaiseChanged();
                    return;
                }
            }
            Side side = Setup.SideOf(table.SeatToMove);
            sbyte piece = game.board[square];
            if (piece != Pieces.None && Pieces.Belongs(piece, side))
            {
                IReadOnlyList<Move> moves = game.LegalFrom(square);
                if (moves.Count > 0)
                {
                    Selected = square;
                    SelectedMoves = moves;
                    PartialPath = null;
                    sound?.Play(ClipKind.Click);
                    CheckersHighlights();
                    RaiseChanged();
                    return;
                }
                // The piece cannot move now; most often because another piece must capture.
                if (MustCapture(game))
                {
                    ui?.Toast(L.T("A capture is mandatory: another piece must jump."));
                }
                sound?.Play(ClipKind.Click);
            }
            CheckersClearSelection();
            CheckersHighlights();
            RaiseChanged();
        }

        /// <summary>The pointer is over a square (-1 for none): the legal destination under it lights up.</summary>
        public void SquareHovered(int square)
        {
            if (checkersHovered == square)
            {
                return;
            }
            checkersHovered = square;
            CheckersHighlights();
        }

        /// <summary>Plays <paramref name="move"/> for the person to move when it is legal now: the way the tours move.</summary>
        public bool TryPlay(Move move)
        {
            CheckersGame game = CheckersGame;
            if (!WaitingForHuman || table == null || game == null || move == null || game.FindLegal(move.path) == null)
            {
                return false;
            }
            return TrySend(BoardCommand.MoveAlong(table.SeatToAct, move));
        }

        /// <summary>Whether the side to move has to capture (every legal move is a capture then).</summary>
        private static bool MustCapture(CheckersGame game)
        {
            foreach (Move move in game.Legal())
            {
                if (move != null && move.IsCapture)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>The squares of the pieces that may move now, each once: shown faintly while nothing is selected.</summary>
        private static List<int> MovableSquares(CheckersGame game)
        {
            var squares = new List<int>();
            foreach (Move move in game.Legal())
            {
                if (move != null && !squares.Contains(move.From))
                {
                    squares.Add(move.From);
                }
            }
            return squares;
        }

        private static bool StartsWith(int[] path, int[] prefix)
        {
            if (path == null || prefix == null || path.Length < prefix.Length)
            {
                return false;
            }
            for (int i = 0; i < prefix.Length; i++)
            {
                if (path[i] != prefix[i])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>A move was applied: its sound, the board plays it (the director waits for the animation), and the game hears of it.</summary>
        private void MoveTaken(Move move)
        {
            CheckersGame game = CheckersGame;
            if (move == null || game == null)
            {
                return;
            }
            sound?.Play(move.promotes ? ClipKind.King : move.IsCapture ? ClipKind.Capture : ClipKind.Move);
            board?.Animate(move, game, null);
            MovePlayed?.Invoke(move);
        }
    }
}
