namespace Portfolio.BoardGames
{
    /// <summary>The kinds of chess piece; a cell holds the kind, positive for White (Light) and negative for Black (Dark).</summary>
    public enum ChessPieceKind : byte
    {
        None = 0,
        Pawn = 1,
        Knight = 2,
        Bishop = 3,
        Rook = 4,
        Queen = 5,
        King = 6
    }

    /// <summary>
    /// The cells of a chess board: 0 empty, <c>+kind</c> a White piece, <c>-kind</c> a Black one (see
    /// <see cref="ChessPieceKind"/>). What a piece is worth, its English name and its letter.
    /// </summary>
    public static class ChessPieces
    {
        public const sbyte None = 0;

        public static readonly ChessPieceKind[] Promotions = { ChessPieceKind.Queen, ChessPieceKind.Rook, ChessPieceKind.Bishop, ChessPieceKind.Knight };

        public static ChessPieceKind KindOf(sbyte piece)
        {
            int kind = piece < 0 ? -piece : piece;
            return kind >= 1 && kind <= 6 ? (ChessPieceKind)kind : ChessPieceKind.None;
        }

        public static Side SideOf(sbyte piece)
        {
            return piece < 0 ? Side.Dark : Side.Light;
        }

        public static bool Belongs(sbyte piece, Side side)
        {
            return piece != None && SideOf(piece) == side;
        }

        public static sbyte Make(Side side, ChessPieceKind kind)
        {
            return (sbyte)(side == Side.Light ? (int)kind : -(int)kind);
        }

        /// <summary>Centipawns: pawn 100, knight 320, bishop 330, rook 500, queen 900, king 0.</summary>
        public static int Value(ChessPieceKind kind)
        {
            switch (kind)
            {
                case ChessPieceKind.Pawn:
                    return 100;
                case ChessPieceKind.Knight:
                    return 320;
                case ChessPieceKind.Bishop:
                    return 330;
                case ChessPieceKind.Rook:
                    return 500;
                case ChessPieceKind.Queen:
                    return 900;
                default:
                    return 0;
            }
        }

        /// <summary>The English name (a key of the localization table).</summary>
        public static string NameKey(ChessPieceKind kind)
        {
            switch (kind)
            {
                case ChessPieceKind.Pawn:
                    return "Pawn";
                case ChessPieceKind.Knight:
                    return "Knight";
                case ChessPieceKind.Bishop:
                    return "Bishop";
                case ChessPieceKind.Rook:
                    return "Rook";
                case ChessPieceKind.Queen:
                    return "Queen";
                case ChessPieceKind.King:
                    return "King";
                default:
                    return "";
            }
        }

        /// <summary>The letter of algebraic notation: P N B R Q K (upper case; FEN writes Black's in lower case).</summary>
        public static char Letter(ChessPieceKind kind)
        {
            switch (kind)
            {
                case ChessPieceKind.Pawn:
                    return 'P';
                case ChessPieceKind.Knight:
                    return 'N';
                case ChessPieceKind.Bishop:
                    return 'B';
                case ChessPieceKind.Rook:
                    return 'R';
                case ChessPieceKind.Queen:
                    return 'Q';
                case ChessPieceKind.King:
                    return 'K';
                default:
                    return '?';
            }
        }
    }
}
