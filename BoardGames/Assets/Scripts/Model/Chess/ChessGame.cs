using System;
using System.Collections.Generic;
using Gamebox.Lockstep;

namespace Portfolio.BoardGames
{
    /// <summary>
    /// One game of chess from its first position to its end. Squares are <c>rank * 8 + file</c> (a1 = 0, h1 = 7,
    /// a8 = 56), rank 0 White's first rank, which is row 0 of the board view (Light's side). Everything the rules look
    /// at is in the public fields: the cells, the side to move, the castling rights, the en passant square, the clocks
    /// of the fifty move rule and the keys of the positions since the last capture or pawn move (for repetitions).
    /// The game ends by itself on checkmate, stalemate, the third repetition of a position, a hundred plies without a
    /// capture or a pawn move, and when neither side has the pieces left to mate.
    /// </summary>
    [Serializable]
    public sealed class ChessGame : IBoardGame
    {
        public const int CastleWhiteKing = 1;
        public const int CastleWhiteQueen = 2;
        public const int CastleBlackKing = 4;
        public const int CastleBlackQueen = 8;

        /// <summary>Plies without a capture or a pawn move that end the game in a draw.</summary>
        public const int FiftyMovePlies = 100;

        public sbyte[] cells = new sbyte[64];
        public int sideToMove;
        /// <summary>The castling rights: <see cref="CastleWhiteKing"/> and the others, or-ed.</summary>
        public int castling;
        /// <summary>The square a pawn passed over with its double step on the last move; -1 for none.</summary>
        public int enPassant = -1;
        /// <summary>Plies since the last capture or pawn move (the fifty move rule ends the game at 100).</summary>
        public int halfmoveClock;
        public int fullmove = 1;
        public int ply;
        public int result;
        public int endReason;
        /// <summary>The move played last; after loading a save without one it is an empty move (from == to), see <see cref="LastMove"/>.</summary>
        public ChessMove lastMove;
        /// <summary>The pieces taken so far, in order (cell values: positive White's, negative Black's).</summary>
        public List<int> captured = new List<int>();
        /// <summary>Two numbers per position since the last capture or pawn move: the keys threefold repetition compares.</summary>
        public List<int> positions = new List<int>();

        [NonSerialized]
        private List<ChessMove> legal;

        /// <summary>For the serializers; the fields are set by them.</summary>
        public ChessGame()
        {
        }

        /// <summary>The starting position, White to move.</summary>
        public static ChessGame NewGame()
        {
            return FromFen(ChessNotation.StartFen);
        }

        /// <summary>A position from Forsyth-Edwards notation; throws <see cref="FormatException"/> on a bad one.</summary>
        public static ChessGame FromFen(string fen)
        {
            var game = new ChessGame();
            ChessNotation.ReadFen(fen, game);
            game.legal = ChessMoveGenerator.Legal(game);
            game.RememberPosition();
            game.Judge();
            return game;
        }

        public string ToFen()
        {
            return ChessNotation.WriteFen(this);
        }

        public RuleSet RuleSet => RuleSet.Chess;

        public Side SideToMove => sideToMove == (int)Side.Dark ? Side.Dark : Side.Light;

        public Side SideToAct => SideToMove;

        public GameResult Result => (GameResult)result;

        public EndReason Reason => (EndReason)endReason;

        public bool IsOver => result != (int)GameResult.Playing;

        public Side? Winner => Result == GameResult.LightWins ? Side.Light : Result == GameResult.DarkWins ? (Side?)Side.Dark : null;

        public int Turn => ply;

        public int Points => IsOver && Winner.HasValue ? 1 : 0;

        /// <summary>The move played last, or null.</summary>
        public ChessMove LastMove => lastMove != null && lastMove.from != lastMove.to ? lastMove : null;

        public sbyte this[int square] => square >= 0 && square < 64 ? cells[square] : ChessPieces.None;

        /// <summary>Whether the side to move is in check.</summary>
        public bool InCheck
        {
            get
            {
                int king = KingSquare(SideToMove);
                return king >= 0 && ChessPosition.Attacked(cells, king, (int)Sides.Other(SideToMove));
            }
        }

        /// <summary>The square of <paramref name="side"/>'s king, or -1.</summary>
        public int KingSquare(Side side)
        {
            sbyte king = ChessPieces.Make(side, ChessPieceKind.King);
            for (int square = 0; square < 64; square++)
            {
                if (cells[square] == king)
                {
                    return square;
                }
            }
            return -1;
        }

        /// <summary>The legal moves of the side to move (each promotion choice a move of its own), worked out once per position; none once over.</summary>
        public IReadOnlyList<ChessMove> Legal()
        {
            if (legal == null)
            {
                legal = IsOver ? new List<ChessMove>() : ChessMoveGenerator.Legal(this);
            }
            return legal;
        }

        /// <summary>The legal moves of the piece on <paramref name="square"/>; empty when it has none.</summary>
        public IReadOnlyList<ChessMove> LegalFrom(int square)
        {
            var from = new List<ChessMove>();
            IReadOnlyList<ChessMove> moves = Legal();
            for (int i = 0; i < moves.Count; i++)
            {
                if (moves[i].from == square)
                {
                    from.Add(moves[i]);
                }
            }
            return from;
        }

        /// <summary>The legal move from, to and with this promotion (0 for none), or null.</summary>
        public ChessMove FindLegal(int from, int to, int promotion = 0)
        {
            IReadOnlyList<ChessMove> moves = Legal();
            for (int i = 0; i < moves.Count; i++)
            {
                ChessMove move = moves[i];
                if (move.from == from && move.to == to && move.promotion == promotion)
                {
                    return move;
                }
            }
            return null;
        }

        /// <summary>The legal move of a command's numbers <c>{ from, to, promotion }</c>, or null.</summary>
        public ChessMove FindLegal(IReadOnlyList<int> path)
        {
            if (path == null || path.Count < 2 || path.Count > 3)
            {
                return null;
            }
            return FindLegal(path[0], path[1], path.Count > 2 ? path[2] : 0);
        }

        /// <summary>The legal move written <paramref name="san"/> in algebraic notation ("Nf3", "exd6", "O-O", "e8=Q"), or null.</summary>
        public ChessMove FindSan(string san)
        {
            string bare = ChessNotation.Bare(san);
            if (bare.Length == 0)
            {
                return null;
            }
            IReadOnlyList<ChessMove> moves = Legal();
            for (int i = 0; i < moves.Count; i++)
            {
                if (ChessNotation.SanBase(moves[i], moves) == bare)
                {
                    return moves[i];
                }
            }
            return null;
        }

        /// <summary>The algebraic notation a legal move would get, check marks included; "" for a move that is not legal.</summary>
        public string SanOf(ChessMove move)
        {
            if (move == null || FindLegal(move.from, move.to, move.promotion) == null)
            {
                return "";
            }
            ChessGame copy = Clone();
            ChessMove played = move.Clone();
            return copy.Play(played) ? played.san : "";
        }

        /// <summary>Whether the move from <paramref name="from"/> to <paramref name="to"/> is a legal pawn move to the last rank, so the player picks a piece.</summary>
        public bool NeedsPromotion(int from, int to)
        {
            IReadOnlyList<ChessMove> moves = Legal();
            for (int i = 0; i < moves.Count; i++)
            {
                if (moves[i].from == from && moves[i].to == to && moves[i].promotion != 0)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Plays <paramref name="move"/> when it is legal (writes its notation and flags); false and nothing changes
        /// otherwise. Only the move's from, to and promotion are read; the rest of it is filled in from the legal move.
        /// </summary>
        public bool Play(ChessMove move)
        {
            if (move == null || IsOver)
            {
                return false;
            }
            ChessMove chosen = FindLegal(move.from, move.to, move.promotion);
            if (chosen == null)
            {
                return false;
            }
            ChessMove played = chosen.Clone();
            played.san = ChessNotation.SanBase(chosen, Legal());
            Apply(played);
            legal = ChessMoveGenerator.Legal(this);
            if (InCheck)
            {
                if (legal.Count == 0)
                {
                    played.flags |= ChessMove.CheckFlag | ChessMove.MateFlag;
                    played.san += "#";
                }
                else
                {
                    played.flags |= ChessMove.CheckFlag;
                    played.san += "+";
                }
            }
            lastMove = played;
            RememberPosition();
            Judge();
            if (!ReferenceEquals(move, played))
            {
                move.piece = played.piece;
                move.captured = played.captured;
                move.capturedPiece = played.capturedPiece;
                move.flags = played.flags;
                move.san = played.san;
            }
            return true;
        }

        public bool Resign(Side side)
        {
            if (IsOver)
            {
                return false;
            }
            result = (int)(side == Side.Light ? GameResult.DarkWins : GameResult.LightWins);
            endReason = (int)EndReason.Resigned;
            legal = null;
            return true;
        }

        public void CallOff(Side? winner)
        {
            if (winner.HasValue)
            {
                result = (int)(winner.Value == Side.Light ? GameResult.LightWins : GameResult.DarkWins);
            }
            else
            {
                result = (int)GameResult.Draw;
            }
            endReason = (int)EndReason.CalledOff;
            legal = null;
        }

        public ChessGame Clone()
        {
            return new ChessGame
            {
                cells = cells != null ? (sbyte[])cells.Clone() : new sbyte[64],
                sideToMove = sideToMove,
                castling = castling,
                enPassant = enPassant,
                halfmoveClock = halfmoveClock,
                fullmove = fullmove,
                ply = ply,
                result = result,
                endReason = endReason,
                lastMove = lastMove != null ? lastMove.Clone() : null,
                captured = captured != null ? new List<int>(captured) : new List<int>(),
                positions = positions != null ? new List<int>(positions) : new List<int>()
            };
        }

        public IBoardGame CloneGame()
        {
            return Clone();
        }

        /// <summary>
        /// Everything the rules look at, folded: the cells, the side to move, the castling rights, the en passant
        /// square, the clocks, the result, the position keys and the pieces taken.
        /// </summary>
        public uint Checksum()
        {
            var sum = new StateChecksum();
            sum.Add(cells.Length);
            for (int i = 0; i < cells.Length; i++)
            {
                sum.Add(cells[i]);
            }
            sum.Add(sideToMove).Add(castling).Add(enPassant).Add(halfmoveClock).Add(fullmove).Add(ply).Add(result).Add(endReason);
            sum.Add(positions ?? new List<int>());
            sum.Add(captured ?? new List<int>());
            return sum.Value;
        }

        /// <summary>What <paramref name="side"/> has on the board, in centipawns (<see cref="ChessPieces.Value"/>), the king not counted.</summary>
        public int Material(Side side)
        {
            int total = 0;
            for (int square = 0; square < 64; square++)
            {
                sbyte piece = cells[square];
                if (ChessPieces.Belongs(piece, side))
                {
                    total += ChessPieces.Value(ChessPieces.KindOf(piece));
                }
            }
            return total;
        }

        /// <summary>How many times the position on the board has stood with the same side to move, castling rights and en passant capture (1 the first time).</summary>
        public int RepetitionCount()
        {
            if (positions == null || positions.Count < 2)
            {
                return 1;
            }
            int last = positions.Count - 2;
            int low = positions[last];
            int high = positions[last + 1];
            int count = 0;
            for (int i = 0; i + 1 < positions.Count; i += 2)
            {
                if (positions[i] == low && positions[i + 1] == high)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Whether neither side can ever mate: only kings, a single knight or bishop, or bishops (of either side) all
        /// on squares of one colour.
        /// </summary>
        public bool InsufficientMaterial()
        {
            int knights = 0;
            int lightBishops = 0;
            int darkBishops = 0;
            for (int square = 0; square < 64; square++)
            {
                switch (ChessPieces.KindOf(cells[square]))
                {
                    case ChessPieceKind.Pawn:
                    case ChessPieceKind.Rook:
                    case ChessPieceKind.Queen:
                        return false;
                    case ChessPieceKind.Knight:
                        knights++;
                        break;
                    case ChessPieceKind.Bishop:
                        // a1 is a dark square: file + rank even.
                        if (((File(square) + Rank(square)) & 1) == 0)
                        {
                            darkBishops++;
                        }
                        else
                        {
                            lightBishops++;
                        }
                        break;
                }
            }
            if (knights + lightBishops + darkBishops <= 1)
            {
                return true;
            }
            return knights == 0 && (lightBishops == 0 || darkBishops == 0);
        }

        public static int Square(int file, int rank)
        {
            return rank * 8 + file;
        }

        public static int File(int square)
        {
            return square & 7;
        }

        public static int Rank(int square)
        {
            return square >> 3;
        }

        /// <summary>The two numbers of the position's key: the pieces, the side to move, the castling rights and the file of a legal en passant capture.</summary>
        internal static void KeyOf(ChessGame game, IReadOnlyList<ChessMove> legalMoves, out int low, out int high)
        {
            int enPassantFile = -1;
            for (int i = 0; i < legalMoves.Count; i++)
            {
                if (legalMoves[i].IsEnPassant)
                {
                    enPassantFile = File(legalMoves[i].to);
                    break;
                }
            }
            ulong key = ChessPosition.KeyOf(game.cells, game.sideToMove, game.castling, enPassantFile);
            low = (int)(uint)key;
            high = (int)(uint)(key >> 32);
        }

        /// <summary>Moves the pieces of a legal move and keeps the clocks, rights and pieces taken.</summary>
        private void Apply(ChessMove move)
        {
            captured = captured ?? new List<int>();
            positions = positions ?? new List<int>();
            Side mover = SideToMove;
            sbyte piece = cells[move.from];
            bool irreversible = move.IsCapture || ChessPieces.KindOf(piece) == ChessPieceKind.Pawn;
            if (move.IsCapture)
            {
                captured.Add(cells[move.captured]);
                cells[move.captured] = ChessPieces.None;
            }
            cells[move.from] = ChessPieces.None;
            cells[move.to] = move.promotion != 0 ? ChessPieces.Make(mover, (ChessPieceKind)move.promotion) : piece;
            if (move.IsCastle)
            {
                cells[move.RookTo] = cells[move.RookFrom];
                cells[move.RookFrom] = ChessPieces.None;
            }
            castling &= ChessPosition.CastleMask[move.from] & ChessPosition.CastleMask[move.to];
            enPassant = (move.flags & ChessMove.DoublePush) != 0 ? (move.from + move.to) / 2 : -1;
            halfmoveClock = irreversible ? 0 : halfmoveClock + 1;
            if (mover == Side.Dark)
            {
                fullmove++;
            }
            sideToMove = (int)Sides.Other(mover);
            ply++;
            if (irreversible)
            {
                // No position before a capture or a pawn move can stand again.
                positions.Clear();
            }
        }

        /// <summary>Adds the key of the position on the board to <see cref="positions"/> (the legal moves already worked out).</summary>
        private void RememberPosition()
        {
            if (positions == null)
            {
                positions = new List<int>();
            }
            KeyOf(this, legal, out int low, out int high);
            positions.Add(low);
            positions.Add(high);
        }

        /// <summary>Ends the game when the rules end it: checkmate, stalemate, insufficient material, repetition, fifty moves.</summary>
        private void Judge()
        {
            if (IsOver)
            {
                return;
            }
            if (legal.Count == 0)
            {
                if (InCheck)
                {
                    result = (int)(SideToMove == Side.Light ? GameResult.DarkWins : GameResult.LightWins);
                    endReason = (int)EndReason.Checkmate;
                }
                else
                {
                    result = (int)GameResult.Draw;
                    endReason = (int)EndReason.Stalemate;
                }
            }
            else if (InsufficientMaterial())
            {
                result = (int)GameResult.Draw;
                endReason = (int)EndReason.InsufficientMaterial;
            }
            else if (RepetitionCount() >= 3)
            {
                result = (int)GameResult.Draw;
                endReason = (int)EndReason.Repetition;
            }
            else if (halfmoveClock >= FiftyMovePlies)
            {
                result = (int)GameResult.Draw;
                endReason = (int)EndReason.FiftyMoves;
            }
            if (IsOver)
            {
                legal = new List<ChessMove>();
            }
        }
    }
}
