using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.BoardGames
{
    /// <summary>
    /// The chess half of the manager: the pieces on the shared squares (<see cref="ChessPiecesView"/>), the clicks that
    /// pick a piece up and put it down (a pawn reaching the last rank asks for its new piece through
    /// <see cref="ChessHud"/>), the marks of the selection, the legal targets, the last move and a king in check, the
    /// sounds and the "Check!" toast, and the words of a chess result and of the seat panels. The shared code calls
    /// these hooks while the table (or the title) is chess.
    /// </summary>
    public partial class BoardGamesManager
    {
        /// <summary>The points of the pieces in the seat panels' material count: pawn 1, knight 3, bishop 3, rook 5, queen 9.</summary>
        private static readonly int[] ChessPoints = { 0, 1, 3, 3, 5, 9, 0 };

        [Header("Chess")]
        [SerializeField] private ChessPiecesView chessPieces;
        [SerializeField] private ChessHud chessHud;

        /// <summary>The legal moves of the selected piece (a promotion counts four times, once per piece).</summary>
        private readonly List<ChessMove> chessTargets = new List<ChessMove>();
        private int chessSelected = -1;
        private int chessHovered = -1;
        /// <summary>The pawn move waiting for the person to pick its piece, or null.</summary>
        private ChessMove chessPromotion;
        /// <summary>Counts the shows of the pieces, so an animation's late callback knows the table it belonged to is gone.</summary>
        private int chessShown;

        /// <summary>The square of the piece the person picked up, or -1 (for the tours).</summary>
        public int ChessSelection => chessSelected;

        /// <summary>Whether the promotion picker waits for the person (for the tours).</summary>
        public bool ChessChoosingPromotion => chessPromotion != null;

        /// <summary>The promotion picker's choice made from code (the tours); false when nothing waits for one.</summary>
        public bool ChessChoosePromotion(ChessPieceKind kind)
        {
            if (chessPromotion == null)
            {
                return false;
            }
            chessHud?.Hide();
            ChessPromote(kind);
            return true;
        }

        // ------------------------------------------------------------------ the hooks

        private void ChessBegin()
        {
            ChessGame game = ChessGame;
            ChessForget();
            chessShown++;
            if (board != null)
            {
                board.ShowSquares(8);
                board.SetVisible(true);
            }
            chessPieces?.Show(game);
            ChessFrame(HomeSide(), true);
        }

        /// <summary>The pieces go and the input is forgotten; the squares stay (checkers shows them, backgammon hides them).</summary>
        private void ChessEnd()
        {
            ChessForget();
            chessShown++;
            chessPieces?.Clear();
            board?.SetSquareMarks(null);
        }

        /// <summary>The title: the starting position behind the menu.</summary>
        private void ChessShowIdle()
        {
            ChessForget();
            chessShown++;
            if (board != null)
            {
                board.ShowSquares(8);
                board.SetVisible(true);
                board.SetSquareMarks(null);
            }
            chessPieces?.Show(ChessGame.NewGame());
            ChessFrame(Side.Light, true);
            cameraRig?.SetTitle(true, false);
        }

        private void ChessFrame(Side bottom, bool immediate)
        {
            cameraRig?.Frame(8, bottom, immediate);
        }

        /// <summary>The piece under the ray (the pieces stand tall: a click on a head picks that piece), else the square under it.</summary>
        private int ChessPick(Ray ray)
        {
            int square = chessPieces != null && ChessGame != null ? chessPieces.SquareAt(ray) : -1;
            if (square >= 0)
            {
                return square;
            }
            return board != null ? board.SquareAt(ray) : -1;
        }

        /// <summary>
        /// A click on a square: an own piece that can move is picked up (again: put back), a marked square of the piece
        /// picked up plays the move there (a promotion asks for the piece first), anything else lets the piece go.
        /// </summary>
        private void ChessClicked(int target)
        {
            ChessGame game = ChessGame;
            if (!WaitingForHuman || game == null || table == null || table.IsOver)
            {
                return;
            }
            if (chessPromotion != null)
            {
                chessHud?.Hide();
                ChessCancelPromotion();
            }
            if (target < 0 || target >= 64)
            {
                ChessDrop();
                return;
            }
            if (chessSelected >= 0 && target != chessSelected)
            {
                ChessMove move = ChessTargetAt(target);
                if (move != null)
                {
                    if (move.IsPromotion)
                    {
                        ChessAskPromotion(move);
                    }
                    else
                    {
                        ChessSend(move);
                    }
                    return;
                }
            }
            Side side = Setup.SideOf(table.SeatToAct);
            sbyte piece = game[target];
            if (ChessPieces.Belongs(piece, side) && target != chessSelected)
            {
                IReadOnlyList<ChessMove> moves = game.LegalFrom(target);
                if (moves.Count > 0)
                {
                    chessSelected = target;
                    chessTargets.Clear();
                    chessTargets.AddRange(moves);
                    sound?.Play(ClipKind.Click);
                    ChessHighlights();
                    RaiseChanged();
                    return;
                }
                ui?.Toast(game.InCheck ? L.T("Your king is in check.") : L.T("That piece cannot move."));
            }
            sound?.Play(ClipKind.Click);
            ChessDrop();
        }

        private void ChessHovered(int target)
        {
            if (chessHovered == target)
            {
                return;
            }
            chessHovered = target;
            ChessHighlights();
        }

        /// <summary>A person's turn begins: nothing picked up, the pieces that can move marked.</summary>
        private void ChessTurnStarted(int seat)
        {
            ChessForget();
        }

        /// <summary>A move was taken (from any seat): its sound, the "Check!" toast, and the pieces play it (the director waits for them).</summary>
        private void ChessEntry(BoardCommand command)
        {
            ChessGame game = ChessGame;
            ChessMove move = game?.LastMove;
            if (command == null || command.Kind != CommandKind.Move || move == null)
            {
                return;
            }
            chessHud?.Hide();
            chessPromotion = null;
            sound?.Play(move.IsPromotion ? ClipKind.King : move.IsCapture ? ClipKind.Capture : ClipKind.Move);
            bool check = move.GivesCheck || move.GivesMate;
            if (chessPieces != null)
            {
                // The check sounds when the piece has landed, unless the table went away meanwhile.
                int shown = chessShown;
                chessPieces.Animate(move, game, check ? () =>
                {
                    if (shown == chessShown)
                    {
                        sound?.Play(ClipKind.Check);
                    }
                } : null);
            }
            else if (check)
            {
                sound?.Play(ClipKind.Check);
            }
            if (check && !game.IsOver)
            {
                ui?.Toast(L.T("Check!"));
            }
        }

        private void ChessClearSelection()
        {
            chessSelected = -1;
            chessTargets.Clear();
            if (chessPromotion != null)
            {
                chessPromotion = null;
                chessHud?.Hide();
            }
        }

        /// <summary>
        /// The marks on the squares: the last move, a king in check, and while a person here is to move the piece picked
        /// up with its targets (captures in their own colour), the pieces that can move while none is, and the target
        /// under the pointer.
        /// </summary>
        private void ChessHighlights()
        {
            ChessGame game = ChessGame;
            if (board == null || game == null)
            {
                return;
            }
            var marks = new SquareMarks();
            ChessMove last = game.LastMove;
            if (last != null)
            {
                marks.lastFrom = last.from;
                marks.lastTo = last.to;
            }
            if (game.InCheck)
            {
                marks.check = game.KingSquare(game.SideToMove);
            }
            if (WaitingForHuman && !game.IsOver)
            {
                bool showMoves = Options == null || Options.showMoves;
                marks.selected = chessSelected;
                marks.hovered = chessPromotion != null ? chessPromotion.to : chessHovered;
                if (showMoves)
                {
                    foreach (ChessMove move in chessTargets)
                    {
                        List<int> list = move.IsCapture ? marks.captures : marks.targets;
                        if (!list.Contains(move.to))
                        {
                            list.Add(move.to);
                        }
                    }
                    if (chessSelected < 0)
                    {
                        foreach (ChessMove move in game.Legal())
                        {
                            if (!marks.movable.Contains(move.from))
                            {
                                marks.movable.Add(move.from);
                            }
                        }
                    }
                }
            }
            board.SetSquareMarks(marks);
            // The dot of a capture hides under the piece taken: that piece glows instead.
            chessPieces?.MarkTakeable(marks.captures, Look != null ? Look.captureColor : new Color(1f, 0.45f, 0.3f, 0.9f), marks.hovered);
        }

        private bool ChessBusy()
        {
            return chessPieces != null && chessPieces.IsAnimating;
        }

        private void ChessApplyLook(BoardGamesTheme theme)
        {
            chessPieces?.ApplyLook(theme);
            chessHud?.ApplyLook(theme);
        }

        private string ChessResultDetail(int winnerSeat)
        {
            ChessGame game = ChessGame;
            switch (game != null ? game.Reason : EndReason.None)
            {
                case EndReason.Checkmate:
                    return L.T("Checkmate.");
                case EndReason.Stalemate:
                    return L.F("Stalemate: {0} cannot move but is not in check.", NameOf(Setup.SeatOf(game.SideToMove)));
                case EndReason.Repetition:
                    return L.T("The same position came up three times.");
                case EndReason.FiftyMoves:
                    return L.T("Fifty moves without a capture or a pawn move.");
                case EndReason.InsufficientMaterial:
                    return L.T("Neither side has the pieces left to checkmate.");
                default:
                    return "";
            }
        }

        /// <summary>How many pieces the seat has taken, and "+3" when it is ahead in material (pawn 1, knight and bishop 3, rook 5, queen 9).</summary>
        private string ChessSeatLine(int seat)
        {
            ChessGame game = ChessGame;
            TableSetup setup = Setup;
            if (game == null || game.cells == null || setup == null || seat < 0 || seat >= setup.seats.Length)
            {
                return "";
            }
            Side side = setup.SideOf(seat);
            int taken = 0;
            if (game.captured != null)
            {
                foreach (int piece in game.captured)
                {
                    if (piece != 0 && ChessPieces.SideOf((sbyte)piece) != side)
                    {
                        taken++;
                    }
                }
            }
            int advantage = 0;
            foreach (sbyte piece in game.cells)
            {
                if (piece != ChessPieces.None)
                {
                    int points = ChessPoints[(int)ChessPieces.KindOf(piece)];
                    advantage += ChessPieces.SideOf(piece) == side ? points : -points;
                }
            }
            string line = L.F("{0} taken", taken);
            return advantage > 0 ? $"{line}  +{advantage}" : line;
        }

        private string ChessTurnWords(int seat)
        {
            return null;
        }

        // ------------------------------------------------------------------ input

        /// <summary>The move of the piece picked up that ends on <paramref name="square"/> (any of a promotion's four), or null.</summary>
        private ChessMove ChessTargetAt(int square)
        {
            foreach (ChessMove move in chessTargets)
            {
                if (move.to == square)
                {
                    return move;
                }
            }
            return null;
        }

        /// <summary>A pawn reaches the last rank: the picker asks for its piece (with no picker in the scene it becomes a queen).</summary>
        private void ChessAskPromotion(ChessMove move)
        {
            if (chessHud == null)
            {
                ChessMove queen = ChessGame.FindLegal(move.from, move.to, (int)ChessPieceKind.Queen);
                if (queen != null)
                {
                    ChessSend(queen);
                }
                return;
            }
            chessPromotion = move;
            sound?.Play(ClipKind.Click);
            ChessHighlights();
            chessHud.ShowPromotion(ChessPromote, ChessCancelPromotion);
            RaiseChanged();
        }

        private void ChessPromote(ChessPieceKind kind)
        {
            ChessMove pending = chessPromotion;
            chessPromotion = null;
            ChessGame game = ChessGame;
            if (pending == null || game == null || !WaitingForHuman)
            {
                return;
            }
            ChessMove move = game.FindLegal(pending.from, pending.to, (int)kind);
            if (move != null)
            {
                ChessSend(move);
            }
        }

        /// <summary>The picker was dismissed: the pawn stays picked up, so another square can be chosen.</summary>
        private void ChessCancelPromotion()
        {
            if (chessPromotion == null)
            {
                return;
            }
            chessPromotion = null;
            ChessHighlights();
            RaiseChanged();
        }

        private void ChessSend(ChessMove move)
        {
            if (table == null || move == null)
            {
                return;
            }
            TrySend(BoardCommand.Play(table.SeatToAct, move.Path()));
        }

        /// <summary>Puts the piece picked up back.</summary>
        private void ChessDrop()
        {
            chessSelected = -1;
            chessTargets.Clear();
            ChessHighlights();
            RaiseChanged();
        }

        /// <summary>Forgets the piece picked up, the pointer and a pending promotion, and closes the picker.</summary>
        private void ChessForget()
        {
            chessSelected = -1;
            chessTargets.Clear();
            chessHovered = -1;
            chessPromotion = null;
            chessHud?.Hide();
        }
    }
}
