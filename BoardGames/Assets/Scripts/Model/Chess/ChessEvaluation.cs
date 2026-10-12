namespace Portfolio.BoardGames
{
    /// <summary>
    /// What the computer thinks of a chess position, in centipawns for the side to move: the pieces' values and where
    /// they stand (a middle game and an end game table per kind, blended by the material left), the bishop pair, rooks
    /// on open files, passed, doubled and isolated pawns, the pawns in front of a castled king while queens and rooks
    /// are about, and in a won ending the losing king driven to the edge. The values and squares are summed by
    /// <see cref="ChessPosition"/> as moves are made; the rest is worked out here from its bit boards. Whole numbers
    /// only, the same everywhere.
    /// </summary>
    internal static class ChessEvaluation
    {
        /// <summary>The weight of all the pieces at the start (knight and bishop 1, rook 2, queen 4): the middle game end of the blend.</summary>
        public const int FullPhase = 24;

        private const int BishopPairMiddle = 30;
        private const int BishopPairEnd = 50;
        private const int RookOpenMiddle = 25;
        private const int RookOpenEnd = 10;
        private const int RookHalfOpenMiddle = 12;
        private const int RookHalfOpenEnd = 6;
        private const int DoubledMiddle = 10;
        private const int DoubledEnd = 15;
        private const int IsolatedMiddle = 10;
        private const int IsolatedEnd = 12;

        /// <summary>What each kind weighs in the blend of middle and end game.</summary>
        public static readonly int[] PhaseOf = { 0, 0, 1, 1, 2, 4, 0 };

        /// <summary>
        /// Value plus table square for each piece, signed (White's positive, Black's negative), by slot and square:
        /// index <c>(cell + 6) * 64 + square</c>.
        /// </summary>
        public static readonly int[] Middle = new int[13 * 64];
        public static readonly int[] End = new int[13 * 64];

        /// <summary>Passed pawn bonus by how far the pawn has come (its rank from its own side).</summary>
        private static readonly int[] passedMiddle = { 0, 5, 8, 12, 20, 35, 55, 0 };
        private static readonly int[] passedEnd = { 0, 10, 15, 25, 40, 65, 100, 0 };

        private static readonly ulong[] fileBits = new ulong[8];
        /// <summary>The files beside each file.</summary>
        private static readonly ulong[] besideBits = new ulong[8];
        /// <summary>The squares in front of a pawn on its own file and the two beside it, by side and square: no enemy pawn there makes it passed.</summary>
        private static readonly ulong[] passedBits = new ulong[2 * 64];

        // The tables are drawn as White sees the board: the first row is the eighth rank, a8 to h8.
        private static readonly int[] pawnMiddle =
        {
            0, 0, 0, 0, 0, 0, 0, 0,
            50, 50, 50, 50, 50, 50, 50, 50,
            10, 10, 20, 30, 30, 20, 10, 10,
            5, 5, 10, 25, 25, 10, 5, 5,
            0, 0, 0, 20, 20, 0, 0, 0,
            5, -5, -10, 0, 0, -10, -5, 5,
            5, 10, 10, -20, -20, 10, 10, 5,
            0, 0, 0, 0, 0, 0, 0, 0
        };

        private static readonly int[] pawnEnd =
        {
            0, 0, 0, 0, 0, 0, 0, 0,
            80, 80, 80, 80, 80, 80, 80, 80,
            50, 50, 50, 50, 50, 50, 50, 50,
            30, 30, 30, 30, 30, 30, 30, 30,
            15, 15, 15, 15, 15, 15, 15, 15,
            5, 5, 5, 5, 5, 5, 5, 5,
            0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0
        };

        private static readonly int[] knightTable =
        {
            -50, -40, -30, -30, -30, -30, -40, -50,
            -40, -20, 0, 0, 0, 0, -20, -40,
            -30, 0, 10, 15, 15, 10, 0, -30,
            -30, 5, 15, 20, 20, 15, 5, -30,
            -30, 0, 15, 20, 20, 15, 0, -30,
            -30, 5, 10, 15, 15, 10, 5, -30,
            -40, -20, 0, 5, 5, 0, -20, -40,
            -50, -40, -30, -30, -30, -30, -40, -50
        };

        private static readonly int[] bishopTable =
        {
            -20, -10, -10, -10, -10, -10, -10, -20,
            -10, 0, 0, 0, 0, 0, 0, -10,
            -10, 0, 5, 10, 10, 5, 0, -10,
            -10, 5, 5, 10, 10, 5, 5, -10,
            -10, 0, 10, 10, 10, 10, 0, -10,
            -10, 10, 10, 10, 10, 10, 10, -10,
            -10, 5, 0, 0, 0, 0, 5, -10,
            -20, -10, -10, -10, -10, -10, -10, -20
        };

        private static readonly int[] rookMiddle =
        {
            0, 0, 0, 0, 0, 0, 0, 0,
            5, 10, 10, 10, 10, 10, 10, 5,
            -5, 0, 0, 0, 0, 0, 0, -5,
            -5, 0, 0, 0, 0, 0, 0, -5,
            -5, 0, 0, 0, 0, 0, 0, -5,
            -5, 0, 0, 0, 0, 0, 0, -5,
            -5, 0, 0, 0, 0, 0, 0, -5,
            0, 0, 0, 5, 5, 0, 0, 0
        };

        private static readonly int[] rookEnd =
        {
            5, 5, 5, 5, 5, 5, 5, 5,
            10, 10, 10, 10, 10, 10, 10, 10,
            0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0
        };

        private static readonly int[] queenTable =
        {
            -20, -10, -10, -5, -5, -10, -10, -20,
            -10, 0, 0, 0, 0, 0, 0, -10,
            -10, 0, 5, 5, 5, 5, 0, -10,
            -5, 0, 5, 5, 5, 5, 0, -5,
            0, 0, 5, 5, 5, 5, 0, -5,
            -10, 5, 5, 5, 5, 5, 0, -10,
            -10, 0, 5, 0, 0, 0, 0, -10,
            -20, -10, -10, -5, -5, -10, -10, -20
        };

        private static readonly int[] kingMiddle =
        {
            -30, -40, -40, -50, -50, -40, -40, -30,
            -30, -40, -40, -50, -50, -40, -40, -30,
            -30, -40, -40, -50, -50, -40, -40, -30,
            -30, -40, -40, -50, -50, -40, -40, -30,
            -20, -30, -30, -40, -40, -30, -30, -20,
            -10, -20, -20, -20, -20, -20, -20, -10,
            20, 20, 0, 0, 0, 0, 20, 20,
            20, 30, 10, 0, 0, 10, 30, 20
        };

        private static readonly int[] kingEnd =
        {
            -50, -40, -30, -20, -20, -30, -40, -50,
            -30, -20, -10, 0, 0, -10, -20, -30,
            -30, -10, 20, 30, 30, 20, -10, -30,
            -30, -10, 30, 40, 40, 30, -10, -30,
            -30, -10, 30, 40, 40, 30, -10, -30,
            -30, -10, 20, 30, 30, 20, -10, -30,
            -30, -30, 0, 0, 0, 0, -30, -30,
            -50, -30, -30, -30, -30, -30, -30, -50
        };

        static ChessEvaluation()
        {
            int[][] middleTables = { null, pawnMiddle, knightTable, bishopTable, rookMiddle, queenTable, kingMiddle };
            int[][] endTables = { null, pawnEnd, knightTable, bishopTable, rookEnd, queenTable, kingEnd };
            for (int kind = 1; kind <= 6; kind++)
            {
                int value = ChessPieces.Value((ChessPieceKind)kind);
                for (int square = 0; square < 64; square++)
                {
                    int file = square & 7;
                    int rank = square >> 3;
                    int white = (7 - rank) * 8 + file;
                    int black = rank * 8 + file;
                    Middle[(6 + kind) * 64 + square] = value + middleTables[kind][white];
                    End[(6 + kind) * 64 + square] = value + endTables[kind][white];
                    Middle[(6 - kind) * 64 + square] = -(value + middleTables[kind][black]);
                    End[(6 - kind) * 64 + square] = -(value + endTables[kind][black]);
                }
            }
            for (int file = 0; file < 8; file++)
            {
                fileBits[file] = 0x0101010101010101UL << file;
            }
            for (int file = 0; file < 8; file++)
            {
                besideBits[file] = (file > 0 ? fileBits[file - 1] : 0UL) | (file < 7 ? fileBits[file + 1] : 0UL);
            }
            for (int square = 0; square < 64; square++)
            {
                int file = square & 7;
                int rank = square >> 3;
                ulong span = fileBits[file] | besideBits[file];
                ulong above = rank < 7 ? ~0UL << ((rank + 1) * 8) : 0UL;
                ulong below = rank > 0 ? ~0UL >> ((8 - rank) * 8) : 0UL;
                passedBits[square] = span & above;
                passedBits[64 + square] = span & below;
            }
        }

        /// <summary>The position for its side to move in centipawns: positive when that side stands better.</summary>
        public static int Evaluate(ChessPosition position)
        {
            ulong[] bits = position.pieceBits;
            ulong whitePawns = bits[6 + (int)ChessPieceKind.Pawn];
            ulong blackPawns = bits[6 - (int)ChessPieceKind.Pawn];
            int whiteMaterial = position.material[0];
            int blackMaterial = position.material[1];
            if ((whitePawns | blackPawns) == 0UL && whiteMaterial < 500 && blackMaterial < 500)
            {
                // A minor piece or less a side: nobody can win.
                return 0;
            }
            int middleScore = position.middleScore;
            int endScore = position.endScore;
            if (Several(bits[6 + (int)ChessPieceKind.Bishop]))
            {
                middleScore += BishopPairMiddle;
                endScore += BishopPairEnd;
            }
            if (Several(bits[6 - (int)ChessPieceKind.Bishop]))
            {
                middleScore -= BishopPairMiddle;
                endScore -= BishopPairEnd;
            }
            ulong pawns = whitePawns;
            while (pawns != 0UL)
            {
                ulong lowest = pawns & (0UL - pawns);
                pawns ^= lowest;
                int square = ChessPosition.BitIndex(lowest);
                if ((passedBits[square] & blackPawns) == 0UL)
                {
                    middleScore += passedMiddle[square >> 3];
                    endScore += passedEnd[square >> 3];
                }
                if ((besideBits[square & 7] & whitePawns) == 0UL)
                {
                    middleScore -= IsolatedMiddle;
                    endScore -= IsolatedEnd;
                }
            }
            pawns = blackPawns;
            while (pawns != 0UL)
            {
                ulong lowest = pawns & (0UL - pawns);
                pawns ^= lowest;
                int square = ChessPosition.BitIndex(lowest);
                if ((passedBits[64 + square] & whitePawns) == 0UL)
                {
                    middleScore -= passedMiddle[7 - (square >> 3)];
                    endScore -= passedEnd[7 - (square >> 3)];
                }
                if ((besideBits[square & 7] & blackPawns) == 0UL)
                {
                    middleScore += IsolatedMiddle;
                    endScore += IsolatedEnd;
                }
            }
            for (int file = 0; file < 8; file++)
            {
                int extra = Count(whitePawns & fileBits[file]) - 1;
                if (extra > 0)
                {
                    middleScore -= DoubledMiddle * extra;
                    endScore -= DoubledEnd * extra;
                }
                extra = Count(blackPawns & fileBits[file]) - 1;
                if (extra > 0)
                {
                    middleScore += DoubledMiddle * extra;
                    endScore += DoubledEnd * extra;
                }
            }
            ulong rooks = bits[6 + (int)ChessPieceKind.Rook];
            while (rooks != 0UL)
            {
                ulong lowest = rooks & (0UL - rooks);
                rooks ^= lowest;
                ulong file = fileBits[ChessPosition.BitIndex(lowest) & 7];
                if ((file & whitePawns) == 0UL)
                {
                    bool open = (file & blackPawns) == 0UL;
                    middleScore += open ? RookOpenMiddle : RookHalfOpenMiddle;
                    endScore += open ? RookOpenEnd : RookHalfOpenEnd;
                }
            }
            rooks = bits[6 - (int)ChessPieceKind.Rook];
            while (rooks != 0UL)
            {
                ulong lowest = rooks & (0UL - rooks);
                rooks ^= lowest;
                ulong file = fileBits[ChessPosition.BitIndex(lowest) & 7];
                if ((file & blackPawns) == 0UL)
                {
                    bool open = (file & whitePawns) == 0UL;
                    middleScore -= open ? RookOpenMiddle : RookHalfOpenMiddle;
                    endScore -= open ? RookOpenEnd : RookHalfOpenEnd;
                }
            }
            int phase = position.phase > FullPhase ? FullPhase : position.phase;
            int whiteKing = position.kings[0];
            int blackKing = position.kings[1];
            if (phase > 0)
            {
                middleScore += Shelter(position.cells, whiteKing, 0) - Shelter(position.cells, blackKing, 1);
            }
            if (whitePawns == 0UL || blackPawns == 0UL)
            {
                endScore += MopUp(whiteMaterial, blackMaterial, blackPawns, whiteKing, blackKing);
                endScore -= MopUp(blackMaterial, whiteMaterial, whitePawns, blackKing, whiteKing);
            }
            int score = (middleScore * phase + endScore * (FullPhase - phase)) / FullPhase;
            return position.side == 0 ? score : -score;
        }

        /// <summary>Whether more than one bit is set.</summary>
        private static bool Several(ulong set)
        {
            return (set & (set - 1UL)) != 0UL;
        }

        private static int Count(ulong set)
        {
            int count = 0;
            while (set != 0UL)
            {
                set &= set - 1UL;
                count++;
            }
            return count;
        }

        /// <summary>The pawns in front of a king on its first two ranks: a pawn one step ahead counts most, a missing one costs.</summary>
        private static int Shelter(sbyte[] cells, int king, int side)
        {
            if (king < 0)
            {
                return 0;
            }
            int rank = king >> 3;
            int file = king & 7;
            int forward = side == 0 ? 1 : -1;
            int ownRank = side == 0 ? rank : 7 - rank;
            if (ownRank > 1)
            {
                return -20;
            }
            sbyte pawn = (sbyte)(side == 0 ? 1 : -1);
            int shelter = 0;
            for (int f = file - 1; f <= file + 1; f++)
            {
                if (f < 0 || f > 7)
                {
                    continue;
                }
                int one = rank + forward;
                int two = rank + 2 * forward;
                if (one >= 0 && one < 8 && cells[one * 8 + f] == pawn)
                {
                    shelter += 12;
                }
                else if (two >= 0 && two < 8 && cells[two * 8 + f] == pawn)
                {
                    shelter += 6;
                }
                else
                {
                    shelter -= 10;
                }
            }
            return shelter;
        }

        /// <summary>
        /// For a side well ahead against a side with no pawns: the losing king pushed to the edge and the kings close
        /// together, so the search finds the way to mate.
        /// </summary>
        private static int MopUp(int strongMaterial, int weakMaterial, ulong weakPawns, int strongKing, int weakKing)
        {
            if (weakPawns != 0UL || strongMaterial < weakMaterial + 400 || strongKing < 0 || weakKing < 0)
            {
                return 0;
            }
            int weakFile = weakKing & 7;
            int weakRank = weakKing >> 3;
            int fromCentre = (weakFile < 4 ? 3 - weakFile : weakFile - 4) + (weakRank < 4 ? 3 - weakRank : weakRank - 4);
            int fileGap = weakFile - (strongKing & 7);
            int rankGap = weakRank - (strongKing >> 3);
            int apart = (fileGap < 0 ? -fileGap : fileGap) + (rankGap < 0 ? -rankGap : rankGap);
            return 10 * fromCentre + 4 * (14 - apart);
        }
    }
}
