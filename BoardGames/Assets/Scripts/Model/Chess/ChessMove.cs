using System;

namespace Portfolio.BoardGames
{
    /// <summary>
    /// One chess move as the generator makes it: from and to (squares 0 to 63, <c>rank * 8 + file</c>, a1 = 0), the
    /// piece a pawn becomes, what it takes and where (en passant takes beside the target square), the castling, en
    /// passant and double step flags, and once played its algebraic notation with the check and mate marks.
    /// </summary>
    [Serializable]
    public sealed class ChessMove
    {
        public const int CastleKing = 1;
        public const int CastleQueen = 2;
        public const int EnPassantFlag = 4;
        public const int DoublePush = 8;
        public const int CheckFlag = 16;
        public const int MateFlag = 32;

        public int from;
        public int to;
        /// <summary>The moving piece (a cell value).</summary>
        public int piece;
        /// <summary>The <see cref="ChessPieceKind"/> a pawn becomes on the last rank; 0 otherwise.</summary>
        public int promotion;
        /// <summary>The square of the piece taken (beside <see cref="to"/> en passant); -1 for none.</summary>
        public int captured = -1;
        /// <summary>The piece taken (a cell value); 0 for none.</summary>
        public int capturedPiece;
        public int flags;
        /// <summary>Standard algebraic notation ("Nf3", "exd6", "O-O", "e8=Q+"), written when the move is played.</summary>
        public string san = "";

        public bool IsCapture => captured >= 0;

        public bool IsCastle => (flags & (CastleKing | CastleQueen)) != 0;

        public bool IsEnPassant => (flags & EnPassantFlag) != 0;

        public bool IsPromotion => promotion != 0;

        public bool GivesCheck => (flags & CheckFlag) != 0;

        public bool GivesMate => (flags & MateFlag) != 0;

        /// <summary>Where the rook of a castling stands before it; -1 for any other move.</summary>
        public int RookFrom => (flags & CastleKing) != 0 ? from + 3 : (flags & CastleQueen) != 0 ? from - 4 : -1;

        /// <summary>Where the rook of a castling lands; -1 for any other move.</summary>
        public int RookTo => (flags & CastleKing) != 0 ? from + 1 : (flags & CastleQueen) != 0 ? from - 1 : -1;

        /// <summary>The numbers of the move command: <c>{ from, to, promotion }</c>.</summary>
        public int[] Path()
        {
            return new[] { from, to, promotion };
        }

        public bool SameAs(ChessMove other)
        {
            return other != null && other.from == from && other.to == to && other.promotion == promotion;
        }

        /// <summary>The move in coordinates ("e2e4", "e7e8q").</summary>
        public string Uci
        {
            get
            {
                string text = SquareName(from) + SquareName(to);
                return promotion != 0 ? text + char.ToLowerInvariant(ChessPieces.Letter((ChessPieceKind)promotion)) : text;
            }
        }

        public ChessMove Clone()
        {
            return (ChessMove)MemberwiseClone();
        }

        public override string ToString()
        {
            return string.IsNullOrEmpty(san) ? Uci : san;
        }

        /// <summary>"a1" to "h8".</summary>
        public static string SquareName(int square)
        {
            if (square < 0 || square > 63)
            {
                return "-";
            }
            return ((char)('a' + square % 8)).ToString() + (char)('1' + square / 8);
        }
    }
}
