using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Portfolio.BoardGames
{
    /// <summary>
    /// The written forms of chess: Forsyth-Edwards notation for a position (read and written) and standard algebraic
    /// notation for a move ("Nbd7", "exd6", "e8=Q+", "O-O-O#").
    /// </summary>
    public static class ChessNotation
    {
        /// <summary>The starting position.</summary>
        public const string StartFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        /// <summary>
        /// The algebraic notation of <paramref name="move"/>, one of <paramref name="legal"/> (the legal moves of the
        /// position it is played in), without the check marks: the piece letter (none for a pawn), the file, rank or
        /// square it leaves when another piece of its kind could also go there, "x" for a capture (a pawn names its
        /// file), the target square and "=Q" for a promotion; "O-O" and "O-O-O" for castling.
        /// </summary>
        public static string SanBase(ChessMove move, IReadOnlyList<ChessMove> legal)
        {
            if ((move.flags & ChessMove.CastleKing) != 0)
            {
                return "O-O";
            }
            if ((move.flags & ChessMove.CastleQueen) != 0)
            {
                return "O-O-O";
            }
            var text = new StringBuilder(8);
            ChessPieceKind kind = ChessPieces.KindOf((sbyte)move.piece);
            string target = ChessMove.SquareName(move.to);
            if (kind == ChessPieceKind.Pawn)
            {
                if (move.IsCapture)
                {
                    text.Append((char)('a' + ChessGame.File(move.from))).Append('x');
                }
                text.Append(target);
                if (move.promotion != 0)
                {
                    text.Append('=').Append(ChessPieces.Letter((ChessPieceKind)move.promotion));
                }
                return text.ToString();
            }
            text.Append(ChessPieces.Letter(kind));
            bool rival = false;
            bool sameFile = false;
            bool sameRank = false;
            if (legal != null)
            {
                for (int i = 0; i < legal.Count; i++)
                {
                    ChessMove other = legal[i];
                    if (other.to != move.to || other.from == move.from || other.piece != move.piece)
                    {
                        continue;
                    }
                    rival = true;
                    if (ChessGame.File(other.from) == ChessGame.File(move.from))
                    {
                        sameFile = true;
                    }
                    if (ChessGame.Rank(other.from) == ChessGame.Rank(move.from))
                    {
                        sameRank = true;
                    }
                }
            }
            if (rival)
            {
                if (!sameFile)
                {
                    text.Append((char)('a' + ChessGame.File(move.from)));
                }
                else if (!sameRank)
                {
                    text.Append((char)('1' + ChessGame.Rank(move.from)));
                }
                else
                {
                    text.Append(ChessMove.SquareName(move.from));
                }
            }
            if (move.IsCapture)
            {
                text.Append('x');
            }
            text.Append(target);
            return text.ToString();
        }

        /// <summary>A move's notation stripped of the marks that do not tell moves apart (check, mate, "!" and "?").</summary>
        public static string Bare(string san)
        {
            if (string.IsNullOrEmpty(san))
            {
                return "";
            }
            int end = san.Length;
            while (end > 0)
            {
                char c = san[end - 1];
                if (c != '+' && c != '#' && c != '!' && c != '?')
                {
                    break;
                }
                end--;
            }
            string bare = san.Substring(0, end).Trim();
            // Castling is also written with zeros.
            if (bare == "0-0")
            {
                return "O-O";
            }
            return bare == "0-0-0" ? "O-O-O" : bare;
        }

        /// <summary>The position of <paramref name="game"/> in Forsyth-Edwards notation.</summary>
        public static string WriteFen(ChessGame game)
        {
            var text = new StringBuilder(90);
            for (int rank = 7; rank >= 0; rank--)
            {
                int empty = 0;
                for (int file = 0; file < 8; file++)
                {
                    sbyte piece = game.cells[ChessGame.Square(file, rank)];
                    if (piece == ChessPieces.None)
                    {
                        empty++;
                        continue;
                    }
                    if (empty > 0)
                    {
                        text.Append((char)('0' + empty));
                        empty = 0;
                    }
                    char letter = ChessPieces.Letter(ChessPieces.KindOf(piece));
                    text.Append(piece > 0 ? letter : char.ToLowerInvariant(letter));
                }
                if (empty > 0)
                {
                    text.Append((char)('0' + empty));
                }
                if (rank > 0)
                {
                    text.Append('/');
                }
            }
            text.Append(game.sideToMove == (int)Side.Dark ? " b " : " w ");
            if (game.castling == 0)
            {
                text.Append('-');
            }
            else
            {
                if ((game.castling & ChessGame.CastleWhiteKing) != 0)
                {
                    text.Append('K');
                }
                if ((game.castling & ChessGame.CastleWhiteQueen) != 0)
                {
                    text.Append('Q');
                }
                if ((game.castling & ChessGame.CastleBlackKing) != 0)
                {
                    text.Append('k');
                }
                if ((game.castling & ChessGame.CastleBlackQueen) != 0)
                {
                    text.Append('q');
                }
            }
            text.Append(' ').Append(game.enPassant >= 0 ? ChessMove.SquareName(game.enPassant) : "-");
            text.Append(' ').Append(game.halfmoveClock.ToString(CultureInfo.InvariantCulture));
            text.Append(' ').Append(game.fullmove.ToString(CultureInfo.InvariantCulture));
            return text.ToString();
        }

        /// <summary>
        /// Sets the position fields of <paramref name="game"/> from <paramref name="fen"/> (four to six fields; the clocks
        /// default to 0 and 1). Throws <see cref="FormatException"/> when it is not a position: a bad field, not one king
        /// a side, a pawn on the first or last rank, the side that just moved in check. Castling rights without the king
        /// and rook at home and an en passant square no pawn passed over are dropped.
        /// </summary>
        internal static void ReadFen(string fen, ChessGame game)
        {
            if (string.IsNullOrWhiteSpace(fen))
            {
                throw new FormatException("Empty FEN");
            }
            string[] fields = fen.Trim().Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length < 4 || fields.Length > 6)
            {
                throw new FormatException("A FEN has four to six fields: " + fen);
            }
            var cells = new sbyte[64];
            string[] ranks = fields[0].Split('/');
            if (ranks.Length != 8)
            {
                throw new FormatException("A FEN board has eight ranks: " + fields[0]);
            }
            int whiteKings = 0;
            int blackKings = 0;
            for (int row = 0; row < 8; row++)
            {
                int rank = 7 - row;
                int file = 0;
                foreach (char c in ranks[row])
                {
                    if (c >= '1' && c <= '8')
                    {
                        file += c - '0';
                        if (file > 8)
                        {
                            throw new FormatException("Too many squares on a rank: " + ranks[row]);
                        }
                        continue;
                    }
                    ChessPieceKind kind = KindOfLetter(char.ToUpperInvariant(c));
                    if (kind == ChessPieceKind.None || file >= 8)
                    {
                        throw new FormatException("Bad rank: " + ranks[row]);
                    }
                    Side side = char.IsUpper(c) ? Side.Light : Side.Dark;
                    if (kind == ChessPieceKind.Pawn && (rank == 0 || rank == 7))
                    {
                        throw new FormatException("A pawn on the first or last rank: " + fields[0]);
                    }
                    if (kind == ChessPieceKind.King)
                    {
                        if (side == Side.Light)
                        {
                            whiteKings++;
                        }
                        else
                        {
                            blackKings++;
                        }
                    }
                    cells[ChessGame.Square(file, rank)] = ChessPieces.Make(side, kind);
                    file++;
                }
                if (file != 8)
                {
                    throw new FormatException("A rank of the wrong length: " + ranks[row]);
                }
            }
            if (whiteKings != 1 || blackKings != 1)
            {
                throw new FormatException("Each side needs one king: " + fields[0]);
            }
            int sideToMove;
            switch (fields[1])
            {
                case "w":
                    sideToMove = (int)Side.Light;
                    break;
                case "b":
                    sideToMove = (int)Side.Dark;
                    break;
                default:
                    throw new FormatException("The side to move is w or b: " + fields[1]);
            }
            int castling = 0;
            if (fields[2] != "-")
            {
                foreach (char c in fields[2])
                {
                    int right;
                    switch (c)
                    {
                        case 'K':
                            right = ChessGame.CastleWhiteKing;
                            break;
                        case 'Q':
                            right = ChessGame.CastleWhiteQueen;
                            break;
                        case 'k':
                            right = ChessGame.CastleBlackKing;
                            break;
                        case 'q':
                            right = ChessGame.CastleBlackQueen;
                            break;
                        default:
                            throw new FormatException("Bad castling rights: " + fields[2]);
                    }
                    castling |= right;
                }
            }
            castling &= PossibleCastling(cells);
            int enPassant = -1;
            if (fields[3] != "-")
            {
                string square = fields[3];
                if (square.Length != 2 || square[0] < 'a' || square[0] > 'h' || square[1] < '1' || square[1] > '8')
                {
                    throw new FormatException("Bad en passant square: " + square);
                }
                enPassant = ChessGame.Square(square[0] - 'a', square[1] - '1');
                if (ChessGame.Rank(enPassant) != (sideToMove == (int)Side.Light ? 5 : 2))
                {
                    throw new FormatException("An en passant square is on the third or sixth rank behind the pawn that moved: " + square);
                }
                if (!PassedOver(cells, sideToMove, enPassant))
                {
                    enPassant = -1;
                }
            }
            int halfmove = 0;
            int fullmove = 1;
            if (fields.Length > 4 && !int.TryParse(fields[4], NumberStyles.None, CultureInfo.InvariantCulture, out halfmove))
            {
                throw new FormatException("Bad halfmove clock: " + fields[4]);
            }
            if (fields.Length > 5)
            {
                if (!int.TryParse(fields[5], NumberStyles.None, CultureInfo.InvariantCulture, out fullmove))
                {
                    throw new FormatException("Bad move number: " + fields[5]);
                }
                if (fullmove < 1)
                {
                    fullmove = 1;
                }
            }
            int waiting = sideToMove ^ 1;
            for (int square = 0; square < 64; square++)
            {
                if (cells[square] == ChessPieces.Make((Side)waiting, ChessPieceKind.King) && ChessPosition.Attacked(cells, square, sideToMove))
                {
                    throw new FormatException("The side that just moved is in check: " + fen);
                }
            }
            game.cells = cells;
            game.sideToMove = sideToMove;
            game.castling = castling;
            game.enPassant = enPassant;
            game.halfmoveClock = halfmove;
            game.fullmove = fullmove;
        }

        /// <summary>The castling rights the placement allows: the king and the rook on their starting squares.</summary>
        internal static int PossibleCastling(sbyte[] cells)
        {
            int rights = 0;
            sbyte whiteKing = ChessPieces.Make(Side.Light, ChessPieceKind.King);
            sbyte whiteRook = ChessPieces.Make(Side.Light, ChessPieceKind.Rook);
            sbyte blackKing = ChessPieces.Make(Side.Dark, ChessPieceKind.King);
            sbyte blackRook = ChessPieces.Make(Side.Dark, ChessPieceKind.Rook);
            if (cells[4] == whiteKing)
            {
                if (cells[7] == whiteRook)
                {
                    rights |= ChessGame.CastleWhiteKing;
                }
                if (cells[0] == whiteRook)
                {
                    rights |= ChessGame.CastleWhiteQueen;
                }
            }
            if (cells[60] == blackKing)
            {
                if (cells[63] == blackRook)
                {
                    rights |= ChessGame.CastleBlackKing;
                }
                if (cells[56] == blackRook)
                {
                    rights |= ChessGame.CastleBlackQueen;
                }
            }
            return rights;
        }

        /// <summary>Whether a pawn of the side that just moved can have stepped twice over the empty <paramref name="square"/>.</summary>
        private static bool PassedOver(sbyte[] cells, int sideToMove, int square)
        {
            if (cells[square] != ChessPieces.None)
            {
                return false;
            }
            if (sideToMove == (int)Side.Light)
            {
                return cells[square - 8] == ChessPieces.Make(Side.Dark, ChessPieceKind.Pawn) && cells[square + 8] == ChessPieces.None;
            }
            return cells[square + 8] == ChessPieces.Make(Side.Light, ChessPieceKind.Pawn) && cells[square - 8] == ChessPieces.None;
        }

        private static ChessPieceKind KindOfLetter(char letter)
        {
            switch (letter)
            {
                case 'P':
                    return ChessPieceKind.Pawn;
                case 'N':
                    return ChessPieceKind.Knight;
                case 'B':
                    return ChessPieceKind.Bishop;
                case 'R':
                    return ChessPieceKind.Rook;
                case 'Q':
                    return ChessPieceKind.Queen;
                case 'K':
                    return ChessPieceKind.King;
                default:
                    return ChessPieceKind.None;
            }
        }
    }
}
