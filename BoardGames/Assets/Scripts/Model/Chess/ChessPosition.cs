namespace Portfolio.BoardGames
{
    /// <summary>
    /// The board the move generator and the computer work on: the 64 cells, the side to move, the castling rights, the
    /// en passant square, a bit board per piece, a 64 bit key of the position and the running sums of the evaluation
    /// (piece values and squares, the material left), changed in place by <see cref="Make"/> and put back by
    /// <see cref="Unmake"/>, with moves packed in whole numbers so a search allocates nothing.
    /// <para>
    /// A move is <c>from | to &lt;&lt; 6 | promotion &lt;&lt; 12 | flags &lt;&lt; 15 | captured kind &lt;&lt; 19 | moving
    /// kind &lt;&lt; 22</c>, the flags those of <see cref="ChessMove"/> (castling, en passant, double step). The en
    /// passant square is kept only while an enemy pawn stands beside the pawn that stepped twice, so the key of a
    /// position does not depend on a capture nobody can make. A piece's slot in the per piece arrays is its cell value
    /// plus 6 (0 Black's king to 12 White's king).
    /// </para>
    /// </summary>
    internal sealed class ChessPosition
    {
        /// <summary>The most moves a position may have (218 is the known record).</summary>
        public const int MaxMoves = 256;

        /// <summary>The most moves made on top of the loaded position.</summary>
        public const int MaxDepth = 128;

        public const int FlagCastleKing = ChessMove.CastleKing;
        public const int FlagCastleQueen = ChessMove.CastleQueen;
        public const int FlagEnPassant = ChessMove.EnPassantFlag;
        public const int FlagDoublePush = ChessMove.DoublePush;

        private const int Pawn = (int)ChessPieceKind.Pawn;
        private const int Knight = (int)ChessPieceKind.Knight;
        private const int Bishop = (int)ChessPieceKind.Bishop;
        private const int Rook = (int)ChessPieceKind.Rook;
        private const int Queen = (int)ChessPieceKind.Queen;
        private const int King = (int)ChessPieceKind.King;

        /// <summary>A de Bruijn number and its table: the square of a single set bit by one multiplication.</summary>
        private const ulong DeBruijn = 0x03F79D71B4CB0A89UL;

        /// <summary>The squares a knight jumps to, 8 slots per square, <see cref="knightCount"/> of them used.</summary>
        private static readonly int[] knightTargets = new int[64 * 8];
        private static readonly int[] knightCount = new int[64];
        private static readonly int[] kingTargets = new int[64 * 8];
        private static readonly int[] kingCount = new int[64];

        /// <summary>
        /// The squares along each of the 8 directions from each square, nearest first, 7 slots per ray: directions 0 to 3
        /// are the rook's (up, down, right, left), 4 to 7 the bishop's.
        /// </summary>
        private static readonly int[] rays = new int[64 * 8 * 7];
        private static readonly int[] rayLength = new int[64 * 8];

        /// <summary>The squares of each ray as a bit board, and whether its squares go up from the start (the nearest blocker is then the lowest bit).</summary>
        private static readonly ulong[] rayBits = new ulong[64 * 8];
        private static readonly bool[] rayUp = { true, false, true, false, true, true, false, false };
        private static readonly ulong[] straightBits = new ulong[64];
        private static readonly ulong[] diagonalBits = new ulong[64];
        private static readonly ulong[] knightBits = new ulong[64];
        private static readonly ulong[] kingBits = new ulong[64];

        /// <summary>The squares a pawn of each side attacks a square from, by side and square.</summary>
        private static readonly ulong[] pawnAttackers = new ulong[2 * 64];

        /// <summary>The castling rights that survive a move from or to each square (a king or rook leaving home, a rook taken).</summary>
        internal static readonly int[] CastleMask = new int[64];

        /// <summary>Whether two squares share a rank, file or diagonal: only a piece standing in line with its king can be pinned.</summary>
        private static readonly bool[] aligned = new bool[64 * 64];

        private static readonly int[] bitIndex = new int[64];

        /// <summary>Centipawns by kind, as <see cref="ChessPieces.Value"/>, for the running material sums.</summary>
        private static readonly int[] values = { 0, 100, 320, 330, 500, 900, 0 };

        private static readonly ulong[] pieceKeys = new ulong[13 * 64];
        private static readonly ulong[] castleKeys = new ulong[16];
        private static readonly ulong[] enPassantKeys = new ulong[8];
        private static readonly ulong sideKey;

        public readonly sbyte[] cells = new sbyte[64];
        /// <summary>0 White, 1 Black.</summary>
        public int side;
        public int castling;
        /// <summary>The square passed over by the last double step while an enemy pawn stands beside it; -1 otherwise.</summary>
        public int enPassant = -1;
        public int halfmove;
        /// <summary>The kings' squares, by side.</summary>
        public readonly int[] kings = new int[2];
        /// <summary>The squares each side's pieces stand on, a bit per square (bit 0 = a1).</summary>
        public readonly ulong[] occupied = new ulong[2];
        /// <summary>The squares of each piece, by slot (cell value plus 6).</summary>
        public readonly ulong[] pieceBits = new ulong[13];
        public ulong key;
        /// <summary>Moves made since the position was loaded (the depth of the undo stack).</summary>
        public int depth;

        /// <summary>The middle game sum of piece values and squares, White's less Black's (see <see cref="ChessEvaluation"/>).</summary>
        public int middleScore;
        /// <summary>The end game sum, White's less Black's.</summary>
        public int endScore;
        /// <summary>The weight of the pieces left (knight and bishop 1, rook 2, queen 4): 24 at the start.</summary>
        public int phase;
        /// <summary>Centipawns of each side's pieces, the king not counted.</summary>
        public readonly int[] material = new int[2];

        private readonly int[] undoMove = new int[MaxDepth];
        private readonly sbyte[] undoCaptured = new sbyte[MaxDepth];
        private readonly int[] undoCastling = new int[MaxDepth];
        private readonly int[] undoEnPassant = new int[MaxDepth];
        private readonly int[] undoHalfmove = new int[MaxDepth];
        private readonly ulong[] undoKey = new ulong[MaxDepth];
        private readonly int[] undoMiddle = new int[MaxDepth];
        private readonly int[] undoEnd = new int[MaxDepth];
        private readonly int[] undoPhase = new int[MaxDepth];
        private readonly int[] undoMaterial = new int[MaxDepth * 2];

        static ChessPosition()
        {
            int[] knightFiles = { 1, 2, 2, 1, -1, -2, -2, -1 };
            int[] knightRanks = { 2, 1, -1, -2, -2, -1, 1, 2 };
            int[] stepFiles = { 0, 0, 1, -1, 1, -1, 1, -1 };
            int[] stepRanks = { 1, -1, 0, 0, 1, 1, -1, -1 };
            for (int square = 0; square < 64; square++)
            {
                int file = square & 7;
                int rank = square >> 3;
                for (int i = 0; i < 8; i++)
                {
                    int f = file + knightFiles[i];
                    int r = rank + knightRanks[i];
                    if (f >= 0 && f < 8 && r >= 0 && r < 8)
                    {
                        knightTargets[square * 8 + knightCount[square]++] = r * 8 + f;
                        knightBits[square] |= 1UL << (r * 8 + f);
                    }
                    f = file + stepFiles[i];
                    r = rank + stepRanks[i];
                    if (f >= 0 && f < 8 && r >= 0 && r < 8)
                    {
                        kingTargets[square * 8 + kingCount[square]++] = r * 8 + f;
                        kingBits[square] |= 1UL << (r * 8 + f);
                    }
                    int ray = square * 8 + i;
                    while (f >= 0 && f < 8 && r >= 0 && r < 8)
                    {
                        rays[ray * 7 + rayLength[ray]++] = r * 8 + f;
                        rayBits[ray] |= 1UL << (r * 8 + f);
                        if (i < 4)
                        {
                            straightBits[square] |= 1UL << (r * 8 + f);
                        }
                        else
                        {
                            diagonalBits[square] |= 1UL << (r * 8 + f);
                        }
                        f += stepFiles[i];
                        r += stepRanks[i];
                    }
                }
                CastleMask[square] = 15;
                if (rank > 0 && file > 0)
                {
                    pawnAttackers[square] |= 1UL << (square - 9);
                }
                if (rank > 0 && file < 7)
                {
                    pawnAttackers[square] |= 1UL << (square - 7);
                }
                if (rank < 7 && file > 0)
                {
                    pawnAttackers[64 + square] |= 1UL << (square + 7);
                }
                if (rank < 7 && file < 7)
                {
                    pawnAttackers[64 + square] |= 1UL << (square + 9);
                }
            }
            CastleMask[0] = 15 & ~ChessGame.CastleWhiteQueen;
            CastleMask[7] = 15 & ~ChessGame.CastleWhiteKing;
            CastleMask[4] = 15 & ~(ChessGame.CastleWhiteKing | ChessGame.CastleWhiteQueen);
            CastleMask[56] = 15 & ~ChessGame.CastleBlackQueen;
            CastleMask[63] = 15 & ~ChessGame.CastleBlackKing;
            CastleMask[60] = 15 & ~(ChessGame.CastleBlackKing | ChessGame.CastleBlackQueen);
            for (int a = 0; a < 64; a++)
            {
                for (int b = 0; b < 64; b++)
                {
                    int files = (a & 7) - (b & 7);
                    int ranks = (a >> 3) - (b >> 3);
                    aligned[a * 64 + b] = a != b && (files == 0 || ranks == 0 || files == ranks || files == -ranks);
                }
            }
            for (int i = 0; i < 64; i++)
            {
                bitIndex[((1UL << i) * DeBruijn) >> 58] = i;
            }

            // The keys come from a fixed seed with whole number arithmetic (splitmix64): the same on every device.
            ulong seed = 0x2545F4914F6CDD1DUL;
            for (int i = 0; i < pieceKeys.Length; i++)
            {
                pieceKeys[i] = NextKey(ref seed);
            }
            for (int i = 0; i < castleKeys.Length; i++)
            {
                castleKeys[i] = NextKey(ref seed);
            }
            for (int i = 0; i < enPassantKeys.Length; i++)
            {
                enPassantKeys[i] = NextKey(ref seed);
            }
            sideKey = NextKey(ref seed);
        }

        private static ulong NextKey(ref ulong state)
        {
            state += 0x9E3779B97F4A7C15UL;
            ulong z = state;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        public static int From(int move)
        {
            return move & 63;
        }

        public static int To(int move)
        {
            return (move >> 6) & 63;
        }

        /// <summary>The <see cref="ChessPieceKind"/> a pawn becomes; 0 for none.</summary>
        public static int Promotion(int move)
        {
            return (move >> 12) & 7;
        }

        public static int Flags(int move)
        {
            return (move >> 15) & 15;
        }

        /// <summary>The kind of the piece taken (a pawn en passant); 0 for none.</summary>
        public static int CapturedKind(int move)
        {
            return (move >> 19) & 7;
        }

        public static int MovingKind(int move)
        {
            return (move >> 22) & 7;
        }

        /// <summary>Neither a capture nor a promotion.</summary>
        public static bool IsQuiet(int move)
        {
            return (move & ((7 << 19) | (7 << 12))) == 0;
        }

        /// <summary>The square of the only bit set in <paramref name="bit"/>.</summary>
        public static int BitIndex(ulong bit)
        {
            return bitIndex[(bit * DeBruijn) >> 58];
        }

        private static int Encode(int from, int to, int promotion, int flags, int capturedKind, int movingKind)
        {
            return from | (to << 6) | (promotion << 12) | (flags << 15) | (capturedKind << 19) | (movingKind << 22);
        }

        /// <summary>
        /// The key of a position worked out from scratch: the pieces on their squares, the side to move, the castling
        /// rights and the file of the en passant square (-1 for none).
        /// </summary>
        public static ulong KeyOf(sbyte[] board, int sideToMove, int castlingRights, int enPassantFile)
        {
            ulong hash = 0UL;
            for (int square = 0; square < 64; square++)
            {
                if (board[square] != 0)
                {
                    hash ^= pieceKeys[(board[square] + 6) * 64 + square];
                }
            }
            hash ^= castleKeys[castlingRights & 15];
            if (enPassantFile >= 0)
            {
                hash ^= enPassantKeys[enPassantFile & 7];
            }
            if (sideToMove != 0)
            {
                hash ^= sideKey;
            }
            return hash;
        }

        /// <summary>Takes the position of <paramref name="game"/> (its moves made so far are not undone by <see cref="Unmake"/>).</summary>
        public void Load(ChessGame game)
        {
            Load(game.cells, game.sideToMove, game.castling, game.enPassant, game.halfmoveClock);
        }

        public void Load(sbyte[] board, int sideToMove, int castlingRights, int enPassantSquare, int halfmoveClock)
        {
            kings[0] = -1;
            kings[1] = -1;
            occupied[0] = 0UL;
            occupied[1] = 0UL;
            for (int i = 0; i < pieceBits.Length; i++)
            {
                pieceBits[i] = 0UL;
            }
            middleScore = 0;
            endScore = 0;
            phase = 0;
            material[0] = 0;
            material[1] = 0;
            for (int square = 0; square < 64; square++)
            {
                sbyte piece = board != null && board.Length == 64 ? board[square] : (sbyte)0;
                cells[square] = piece;
                if (piece == 0)
                {
                    continue;
                }
                int kind = piece > 0 ? piece : -piece;
                int owner = piece > 0 ? 0 : 1;
                occupied[owner] |= 1UL << square;
                pieceBits[piece + 6] |= 1UL << square;
                middleScore += ChessEvaluation.Middle[(piece + 6) * 64 + square];
                endScore += ChessEvaluation.End[(piece + 6) * 64 + square];
                phase += ChessEvaluation.PhaseOf[kind];
                material[owner] += values[kind];
                if (kind == King)
                {
                    kings[owner] = square;
                }
            }
            side = sideToMove == 1 ? 1 : 0;
            castling = castlingRights & 15;
            halfmove = halfmoveClock;
            enPassant = UsableEnPassant(cells, side, enPassantSquare) ? enPassantSquare : -1;
            depth = 0;
            key = KeyOf(cells, side, castling, enPassant >= 0 ? enPassant & 7 : -1);
        }

        /// <summary>
        /// Whether <paramref name="square"/> is an en passant square <paramref name="sideToMove"/> could take on: on the
        /// right rank, empty, the pawn that stepped over it in front of it, and a pawn of the side to move beside that one.
        /// </summary>
        public static bool UsableEnPassant(sbyte[] board, int sideToMove, int square)
        {
            if (square < 0 || square > 63 || board[square] != 0)
            {
                return false;
            }
            int rank = square >> 3;
            int file = square & 7;
            if (sideToMove == 0)
            {
                if (rank != 5 || board[square - 8] != -Pawn || board[square + 8] != 0)
                {
                    return false;
                }
                int pawn = square - 8;
                return (file > 0 && board[pawn - 1] == Pawn) || (file < 7 && board[pawn + 1] == Pawn);
            }
            if (rank != 2 || board[square + 8] != Pawn || board[square - 8] != 0)
            {
                return false;
            }
            int stepped = square + 8;
            return (file > 0 && board[stepped - 1] == -Pawn) || (file < 7 && board[stepped + 1] == -Pawn);
        }

        public bool InCheck()
        {
            int king = kings[side];
            return king >= 0 && AttackedBy(king, side ^ 1);
        }

        /// <summary>
        /// Whether the pseudo-legal <paramref name="move"/> of the side to move needs <see cref="MoverIsSafe"/> after it
        /// is made: in check, a king move, en passant (two pawns leave a rank), or a piece in line with its king (it may
        /// be pinned). Any other move cannot uncover the king.
        /// </summary>
        public bool NeedsSafetyTest(int move, bool inCheck)
        {
            if (inCheck)
            {
                return true;
            }
            int king = kings[side];
            return king < 0 || MovingKind(move) == King || (Flags(move) & FlagEnPassant) != 0 || aligned[king * 64 + (move & 63)];
        }

        /// <summary>Whether the side that just moved left its own king safe (the test of a pseudo-legal move after <see cref="Make"/>).</summary>
        public bool MoverIsSafe()
        {
            int king = kings[side ^ 1];
            return king < 0 || !AttackedBy(king, side);
        }

        /// <summary>
        /// <see cref="Attacked"/> on this board by its bit boards: pawns, knights and kings by mask, a slider only when
        /// one stands on a line through the square, then by the nearest piece of each ray.
        /// </summary>
        public bool AttackedBy(int square, int by)
        {
            ulong[] bits = pieceBits;
            int sign = by == 0 ? 1 : -1;
            if ((pawnAttackers[by * 64 + square] & bits[6 + Pawn * sign]) != 0UL
                || (knightBits[square] & bits[6 + Knight * sign]) != 0UL
                || (kingBits[square] & bits[6 + King * sign]) != 0UL)
            {
                return true;
            }
            ulong all = occupied[0] | occupied[1];
            ulong queens = bits[6 + Queen * sign];
            ulong straight = bits[6 + Rook * sign] | queens;
            if ((straightBits[square] & straight) != 0UL)
            {
                for (int direction = 0; direction < 4; direction++)
                {
                    ulong blockers = rayBits[square * 8 + direction] & all;
                    if (blockers != 0UL && (straight & (1UL << Nearest(blockers, direction))) != 0UL)
                    {
                        return true;
                    }
                }
            }
            ulong diagonal = bits[6 + Bishop * sign] | queens;
            if ((diagonalBits[square] & diagonal) != 0UL)
            {
                for (int direction = 4; direction < 8; direction++)
                {
                    ulong blockers = rayBits[square * 8 + direction] & all;
                    if (blockers != 0UL && (diagonal & (1UL << Nearest(blockers, direction))) != 0UL)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>The square of the blocker nearest the start of a ray going in <paramref name="direction"/>.</summary>
        private static int Nearest(ulong blockers, int direction)
        {
            if (rayUp[direction])
            {
                return BitIndex(blockers & (0UL - blockers));
            }
            blockers |= blockers >> 1;
            blockers |= blockers >> 2;
            blockers |= blockers >> 4;
            blockers |= blockers >> 8;
            blockers |= blockers >> 16;
            blockers |= blockers >> 32;
            return BitIndex(blockers ^ (blockers >> 1));
        }

        /// <summary>Whether a piece of side <paramref name="by"/> (0 White, 1 Black) attacks <paramref name="square"/> on <paramref name="board"/>.</summary>
        public static bool Attacked(sbyte[] board, int square, int by)
        {
            int sign = by == 0 ? 1 : -1;
            int file = square & 7;
            if (by == 0)
            {
                if (square >= 8)
                {
                    if (file > 0 && board[square - 9] == Pawn)
                    {
                        return true;
                    }
                    if (file < 7 && board[square - 7] == Pawn)
                    {
                        return true;
                    }
                }
            }
            else if (square < 56)
            {
                if (file > 0 && board[square + 7] == -Pawn)
                {
                    return true;
                }
                if (file < 7 && board[square + 9] == -Pawn)
                {
                    return true;
                }
            }
            sbyte knight = (sbyte)(Knight * sign);
            int start = square * 8;
            int count = knightCount[square];
            for (int i = 0; i < count; i++)
            {
                if (board[knightTargets[start + i]] == knight)
                {
                    return true;
                }
            }
            sbyte king = (sbyte)(King * sign);
            count = kingCount[square];
            for (int i = 0; i < count; i++)
            {
                if (board[kingTargets[start + i]] == king)
                {
                    return true;
                }
            }
            sbyte rook = (sbyte)(Rook * sign);
            sbyte bishop = (sbyte)(Bishop * sign);
            sbyte queen = (sbyte)(Queen * sign);
            for (int direction = 0; direction < 8; direction++)
            {
                int ray = square * 8 + direction;
                int length = rayLength[ray];
                int offset = ray * 7;
                sbyte straight = direction < 4 ? rook : bishop;
                for (int i = 0; i < length; i++)
                {
                    sbyte piece = board[rays[offset + i]];
                    if (piece != 0)
                    {
                        if (piece == straight || piece == queen)
                        {
                            return true;
                        }
                        break;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Writes the pseudo-legal moves of the side to move into <paramref name="buffer"/> from <paramref name="start"/>
        /// and returns where they end: every move whose only fault may be leaving the own king attacked (castling is
        /// checked for the king's start and passing squares here). <paramref name="noisyOnly"/> keeps only captures and
        /// promotions to a queen. The moves come square by square from a1, the same order every time.
        /// </summary>
        public int Generate(int[] buffer, int start, bool noisyOnly)
        {
            int count = start;
            int us = side;
            int sign = us == 0 ? 1 : -1;
            ulong own = occupied[us];
            while (own != 0UL)
            {
                ulong lowest = own & (0UL - own);
                own ^= lowest;
                int from = BitIndex(lowest);
                switch (cells[from] * sign)
                {
                    case Pawn:
                        count = PawnMoves(buffer, count, from, noisyOnly);
                        break;
                    case Knight:
                        count = StepMoves(buffer, count, from, Knight, knightBits[from], noisyOnly);
                        break;
                    case King:
                        count = StepMoves(buffer, count, from, King, kingBits[from], noisyOnly);
                        if (!noisyOnly)
                        {
                            count = CastleMoves(buffer, count, from);
                        }
                        break;
                    case Bishop:
                        count = SlideMoves(buffer, count, from, Bishop, 4, 8, noisyOnly);
                        break;
                    case Rook:
                        count = SlideMoves(buffer, count, from, Rook, 0, 4, noisyOnly);
                        break;
                    case Queen:
                        count = SlideMoves(buffer, count, from, Queen, 0, 8, noisyOnly);
                        break;
                }
            }
            return count;
        }

        private int PawnMoves(int[] buffer, int count, int from, bool noisyOnly)
        {
            int us = side;
            int forward = us == 0 ? 8 : -8;
            int startRank = us == 0 ? 1 : 6;
            int lastRank = us == 0 ? 7 : 0;
            int rank = from >> 3;
            int file = from & 7;
            int to = from + forward;
            bool promotes = (to >> 3) == lastRank;
            if (cells[to] == 0)
            {
                if (promotes)
                {
                    count = Promotions(buffer, count, from, to, 0, noisyOnly);
                }
                else if (!noisyOnly)
                {
                    buffer[count++] = Encode(from, to, 0, 0, 0, Pawn);
                    int twice = to + forward;
                    if (rank == startRank && cells[twice] == 0)
                    {
                        buffer[count++] = Encode(from, twice, 0, FlagDoublePush, 0, Pawn);
                    }
                }
            }
            for (int step = -1; step <= 1; step += 2)
            {
                if ((step < 0 && file == 0) || (step > 0 && file == 7))
                {
                    continue;
                }
                int target = to + step;
                int victim = cells[target];
                if ((us == 0 && victim < 0) || (us == 1 && victim > 0))
                {
                    int kind = victim < 0 ? -victim : victim;
                    if (promotes)
                    {
                        count = Promotions(buffer, count, from, target, kind, noisyOnly);
                    }
                    else
                    {
                        buffer[count++] = Encode(from, target, 0, 0, kind, Pawn);
                    }
                }
                else if (target == enPassant && victim == 0)
                {
                    buffer[count++] = Encode(from, target, 0, FlagEnPassant, Pawn, Pawn);
                }
            }
            return count;
        }

        private static int Promotions(int[] buffer, int count, int from, int to, int capturedKind, bool queenOnly)
        {
            buffer[count++] = Encode(from, to, Queen, 0, capturedKind, Pawn);
            if (!queenOnly)
            {
                buffer[count++] = Encode(from, to, Rook, 0, capturedKind, Pawn);
                buffer[count++] = Encode(from, to, Bishop, 0, capturedKind, Pawn);
                buffer[count++] = Encode(from, to, Knight, 0, capturedKind, Pawn);
            }
            return count;
        }

        private int StepMoves(int[] buffer, int count, int from, int kind, ulong targets, bool noisyOnly)
        {
            targets &= noisyOnly ? occupied[side ^ 1] : ~occupied[side];
            while (targets != 0UL)
            {
                ulong lowest = targets & (0UL - targets);
                targets ^= lowest;
                int to = BitIndex(lowest);
                int victim = cells[to];
                buffer[count++] = Encode(from, to, 0, 0, victim < 0 ? -victim : victim, kind);
            }
            return count;
        }

        private int SlideMoves(int[] buffer, int count, int from, int kind, int firstDirection, int endDirection, bool noisyOnly)
        {
            ulong all = occupied[0] | occupied[1];
            ulong enemies = occupied[side ^ 1];
            for (int direction = firstDirection; direction < endDirection; direction++)
            {
                int ray = from * 8 + direction;
                ulong blockers = rayBits[ray] & all;
                int blocker = blockers != 0UL ? Nearest(blockers, direction) : -1;
                if (!noisyOnly)
                {
                    int length = rayLength[ray];
                    int offset = ray * 7;
                    for (int i = 0; i < length; i++)
                    {
                        int to = rays[offset + i];
                        if (to == blocker)
                        {
                            break;
                        }
                        buffer[count++] = Encode(from, to, 0, 0, 0, kind);
                    }
                }
                if (blocker >= 0 && (enemies & (1UL << blocker)) != 0UL)
                {
                    int victim = cells[blocker];
                    buffer[count++] = Encode(from, blocker, 0, 0, victim < 0 ? -victim : victim, kind);
                }
            }
            return count;
        }

        private int CastleMoves(int[] buffer, int count, int from)
        {
            int us = side;
            int home = us == 0 ? 4 : 60;
            if (from != home)
            {
                return count;
            }
            int kingRight = us == 0 ? ChessGame.CastleWhiteKing : ChessGame.CastleBlackKing;
            int queenRight = us == 0 ? ChessGame.CastleWhiteQueen : ChessGame.CastleBlackQueen;
            if ((castling & (kingRight | queenRight)) == 0)
            {
                return count;
            }
            sbyte rook = (sbyte)(us == 0 ? Rook : -Rook);
            int them = us ^ 1;
            if (AttackedBy(home, them))
            {
                return count;
            }
            if ((castling & kingRight) != 0 && cells[home + 1] == 0 && cells[home + 2] == 0 && cells[home + 3] == rook
                && !AttackedBy(home + 1, them))
            {
                buffer[count++] = Encode(home, home + 2, 0, FlagCastleKing, 0, King);
            }
            if ((castling & queenRight) != 0 && cells[home - 1] == 0 && cells[home - 2] == 0 && cells[home - 3] == 0
                && cells[home - 4] == rook && !AttackedBy(home - 1, them))
            {
                buffer[count++] = Encode(home, home - 2, 0, FlagCastleQueen, 0, King);
            }
            return count;
        }

        /// <summary>Plays a pseudo-legal move of the side to move; <see cref="Unmake"/> takes it back.</summary>
        public void Make(int move)
        {
            int from = move & 63;
            int to = (move >> 6) & 63;
            int promotion = (move >> 12) & 7;
            int flags = (move >> 15) & 15;
            int us = side;
            int them = us ^ 1;
            sbyte piece = cells[from];
            undoMove[depth] = move;
            undoCastling[depth] = castling;
            undoEnPassant[depth] = enPassant;
            undoHalfmove[depth] = halfmove;
            undoKey[depth] = key;
            undoMiddle[depth] = middleScore;
            undoEnd[depth] = endScore;
            undoPhase[depth] = phase;
            undoMaterial[depth * 2] = material[0];
            undoMaterial[depth * 2 + 1] = material[1];
            ulong hash = key;
            if (enPassant >= 0)
            {
                hash ^= enPassantKeys[enPassant & 7];
            }
            hash ^= castleKeys[castling];
            int takenSquare = (flags & FlagEnPassant) != 0 ? (us == 0 ? to - 8 : to + 8) : to;
            sbyte taken = cells[takenSquare];
            undoCaptured[depth] = taken;
            if (taken != 0)
            {
                int slot = (taken + 6) * 64 + takenSquare;
                int kind = taken > 0 ? taken : -taken;
                hash ^= pieceKeys[slot];
                cells[takenSquare] = 0;
                occupied[them] ^= 1UL << takenSquare;
                pieceBits[taken + 6] ^= 1UL << takenSquare;
                middleScore -= ChessEvaluation.Middle[slot];
                endScore -= ChessEvaluation.End[slot];
                phase -= ChessEvaluation.PhaseOf[kind];
                material[them] -= values[kind];
            }
            sbyte placed = piece;
            if (promotion != 0)
            {
                placed = (sbyte)(us == 0 ? promotion : -promotion);
                phase += ChessEvaluation.PhaseOf[promotion];
                material[us] += values[promotion] - values[Pawn];
            }
            int fromSlot = (piece + 6) * 64 + from;
            int toSlot = (placed + 6) * 64 + to;
            hash ^= pieceKeys[fromSlot] ^ pieceKeys[toSlot];
            middleScore += ChessEvaluation.Middle[toSlot] - ChessEvaluation.Middle[fromSlot];
            endScore += ChessEvaluation.End[toSlot] - ChessEvaluation.End[fromSlot];
            cells[from] = 0;
            cells[to] = placed;
            occupied[us] ^= (1UL << from) | (1UL << to);
            pieceBits[piece + 6] ^= 1UL << from;
            pieceBits[placed + 6] ^= 1UL << to;
            if ((flags & (FlagCastleKing | FlagCastleQueen)) != 0)
            {
                int rookFrom = (flags & FlagCastleKing) != 0 ? from + 3 : from - 4;
                int rookTo = (flags & FlagCastleKing) != 0 ? from + 1 : from - 1;
                sbyte rook = cells[rookFrom];
                int rookFromSlot = (rook + 6) * 64 + rookFrom;
                int rookToSlot = (rook + 6) * 64 + rookTo;
                cells[rookFrom] = 0;
                cells[rookTo] = rook;
                occupied[us] ^= (1UL << rookFrom) | (1UL << rookTo);
                pieceBits[rook + 6] ^= (1UL << rookFrom) | (1UL << rookTo);
                hash ^= pieceKeys[rookFromSlot] ^ pieceKeys[rookToSlot];
                middleScore += ChessEvaluation.Middle[rookToSlot] - ChessEvaluation.Middle[rookFromSlot];
                endScore += ChessEvaluation.End[rookToSlot] - ChessEvaluation.End[rookFromSlot];
            }
            if (piece == King || piece == -King)
            {
                kings[us] = to;
            }
            castling &= CastleMask[from] & CastleMask[to];
            hash ^= castleKeys[castling];
            enPassant = -1;
            if ((flags & FlagDoublePush) != 0)
            {
                sbyte enemyPawn = (sbyte)(us == 0 ? -Pawn : Pawn);
                int file = to & 7;
                if ((file > 0 && cells[to - 1] == enemyPawn) || (file < 7 && cells[to + 1] == enemyPawn))
                {
                    enPassant = (from + to) >> 1;
                    hash ^= enPassantKeys[file];
                }
            }
            halfmove = taken != 0 || piece == Pawn || piece == -Pawn ? 0 : halfmove + 1;
            side = them;
            key = hash ^ sideKey;
            depth++;
        }

        /// <summary>Takes back the move <see cref="Make"/> played last.</summary>
        public void Unmake()
        {
            depth--;
            int move = undoMove[depth];
            int from = move & 63;
            int to = (move >> 6) & 63;
            int promotion = (move >> 12) & 7;
            int flags = (move >> 15) & 15;
            side ^= 1;
            int us = side;
            sbyte placed = cells[to];
            sbyte piece = promotion != 0 ? (sbyte)(us == 0 ? Pawn : -Pawn) : placed;
            cells[to] = 0;
            cells[from] = piece;
            occupied[us] ^= (1UL << from) | (1UL << to);
            pieceBits[placed + 6] ^= 1UL << to;
            pieceBits[piece + 6] ^= 1UL << from;
            int takenSquare = (flags & FlagEnPassant) != 0 ? (us == 0 ? to - 8 : to + 8) : to;
            sbyte taken = undoCaptured[depth];
            if (taken != 0)
            {
                cells[takenSquare] = taken;
                occupied[us ^ 1] ^= 1UL << takenSquare;
                pieceBits[taken + 6] ^= 1UL << takenSquare;
            }
            if ((flags & (FlagCastleKing | FlagCastleQueen)) != 0)
            {
                int rookFrom = (flags & FlagCastleKing) != 0 ? from + 3 : from - 4;
                int rookTo = (flags & FlagCastleKing) != 0 ? from + 1 : from - 1;
                sbyte rook = cells[rookTo];
                cells[rookFrom] = rook;
                cells[rookTo] = 0;
                occupied[us] ^= (1UL << rookFrom) | (1UL << rookTo);
                pieceBits[rook + 6] ^= (1UL << rookFrom) | (1UL << rookTo);
            }
            if (piece == King || piece == -King)
            {
                kings[us] = from;
            }
            castling = undoCastling[depth];
            enPassant = undoEnPassant[depth];
            halfmove = undoHalfmove[depth];
            key = undoKey[depth];
            middleScore = undoMiddle[depth];
            endScore = undoEnd[depth];
            phase = undoPhase[depth];
            material[0] = undoMaterial[depth * 2];
            material[1] = undoMaterial[depth * 2 + 1];
        }

        /// <summary>Passes the move to the other side (the computer's null move); <see cref="UnmakeNull"/> takes it back.</summary>
        public void MakeNull()
        {
            undoMove[depth] = 0;
            undoEnPassant[depth] = enPassant;
            undoHalfmove[depth] = halfmove;
            undoKey[depth] = key;
            ulong hash = key;
            if (enPassant >= 0)
            {
                hash ^= enPassantKeys[enPassant & 7];
            }
            enPassant = -1;
            // A repetition across a passed move is no repetition: the search looks no further back than here.
            halfmove = 0;
            side ^= 1;
            key = hash ^ sideKey;
            depth++;
        }

        public void UnmakeNull()
        {
            depth--;
            side ^= 1;
            enPassant = undoEnPassant[depth];
            halfmove = undoHalfmove[depth];
            key = undoKey[depth];
        }

        /// <summary>
        /// What a capture wins or loses once both sides have taken back and forth on its square with their least
        /// valuable pieces, each stopping when going on would cost it (static exchange evaluation), in centipawns.
        /// </summary>
        public int StaticExchange(int move, int[] gains)
        {
            int from = move & 63;
            int to = (move >> 6) & 63;
            gains[0] = values[CapturedKind(move)];
            int onSquare = MovingKind(move) == King ? 20000 : values[MovingKind(move)];
            ulong removed = 1UL << from;
            int capturer = side ^ 1;
            int depthReached = 0;
            while (depthReached < 31)
            {
                int square = LeastValuableAttacker(to, capturer, removed);
                if (square < 0)
                {
                    break;
                }
                depthReached++;
                gains[depthReached] = onSquare - gains[depthReached - 1];
                if (-gains[depthReached - 1] < 0 && gains[depthReached] < 0)
                {
                    break;
                }
                int kind = cells[square] < 0 ? -cells[square] : cells[square];
                onSquare = kind == King ? 20000 : values[kind];
                removed |= 1UL << square;
                capturer ^= 1;
            }
            while (depthReached > 0)
            {
                int stand = -gains[depthReached - 1];
                gains[depthReached - 1] = -(stand > gains[depthReached] ? stand : gains[depthReached]);
                depthReached--;
            }
            return gains[0];
        }

        /// <summary>The square of the cheapest piece of <paramref name="by"/> that attacks <paramref name="square"/>, the pieces on <paramref name="removed"/> gone; -1 for none.</summary>
        private int LeastValuableAttacker(int square, int by, ulong removed)
        {
            int sign = by == 0 ? 1 : -1;
            int file = square & 7;
            sbyte pawn = (sbyte)(Pawn * sign);
            int behind = by == 0 ? square - 8 : square + 8;
            if (behind >= 0 && behind < 64)
            {
                if (file > 0 && cells[behind - 1] == pawn && (removed & (1UL << (behind - 1))) == 0UL)
                {
                    return behind - 1;
                }
                if (file < 7 && cells[behind + 1] == pawn && (removed & (1UL << (behind + 1))) == 0UL)
                {
                    return behind + 1;
                }
            }
            sbyte knight = (sbyte)(Knight * sign);
            int start = square * 8;
            int count = knightCount[square];
            for (int i = 0; i < count; i++)
            {
                int target = knightTargets[start + i];
                if (cells[target] == knight && (removed & (1UL << target)) == 0UL)
                {
                    return target;
                }
            }
            int bishop = -1;
            int rook = -1;
            int queen = -1;
            for (int direction = 0; direction < 8; direction++)
            {
                int ray = square * 8 + direction;
                int length = rayLength[ray];
                int offset = ray * 7;
                for (int i = 0; i < length; i++)
                {
                    int target = rays[offset + i];
                    int piece = cells[target];
                    if (piece == 0 || (removed & (1UL << target)) != 0UL)
                    {
                        continue;
                    }
                    piece *= sign;
                    if (piece == Queen)
                    {
                        queen = target;
                    }
                    else if (direction < 4 ? piece == Rook : piece == Bishop)
                    {
                        if (direction < 4)
                        {
                            rook = target;
                        }
                        else
                        {
                            bishop = target;
                        }
                    }
                    break;
                }
            }
            if (bishop >= 0)
            {
                return bishop;
            }
            if (rook >= 0)
            {
                return rook;
            }
            if (queen >= 0)
            {
                return queen;
            }
            sbyte king = (sbyte)(King * sign);
            count = kingCount[square];
            for (int i = 0; i < count; i++)
            {
                int target = kingTargets[start + i];
                if (cells[target] == king && (removed & (1UL << target)) == 0UL)
                {
                    return target;
                }
            }
            return -1;
        }

        /// <summary>The key of the position <paramref name="back"/> moves ago (1 = before the last move made).</summary>
        public ulong KeyBefore(int back)
        {
            return undoKey[depth - back];
        }

        /// <summary>The pseudo-legal moves that are legal, written into <paramref name="buffer"/> from 0; returns how many.</summary>
        public int Legal(int[] buffer)
        {
            bool inCheck = InCheck();
            int end = Generate(buffer, 0, false);
            int count = 0;
            for (int i = 0; i < end; i++)
            {
                int move = buffer[i];
                bool safe = true;
                if (NeedsSafetyTest(move, inCheck))
                {
                    Make(move);
                    safe = MoverIsSafe();
                    Unmake();
                }
                if (safe)
                {
                    buffer[count++] = move;
                }
            }
            return count;
        }

        /// <summary>Leaf positions <paramref name="plies"/> moves deep: the count perft compares with the known numbers.</summary>
        public long Perft(int plies, int[] buffer)
        {
            if (plies <= 0)
            {
                return 1;
            }
            bool inCheck = InCheck();
            int start = depth * MaxMoves;
            int end = Generate(buffer, start, false);
            long nodes = 0;
            for (int i = start; i < end; i++)
            {
                int move = buffer[i];
                bool test = NeedsSafetyTest(move, inCheck);
                if (plies == 1 && !test)
                {
                    nodes++;
                    continue;
                }
                Make(move);
                if (!test || MoverIsSafe())
                {
                    nodes += plies == 1 ? 1 : Perft(plies - 1, buffer);
                }
                Unmake();
            }
            return nodes;
        }
    }
}
