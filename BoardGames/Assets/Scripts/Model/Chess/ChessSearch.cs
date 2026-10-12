namespace Portfolio.BoardGames
{
    /// <summary>
    /// One search of the chess computer over a <see cref="ChessPosition"/> it plays moves on and takes them back:
    /// negamax with alpha-beta (principal variation search), a table of positions already searched, a null move and
    /// late move reductions to look deeper, check extensions, and a quiescence search of captures and queen promotions
    /// at the leaves. Moves are tried in order: the table's best, captures by most valuable victim and least valuable
    /// attacker, the killer moves of the ply, then the quiet moves that cut off before. It counts every position it
    /// visits (the quiescence ones too) and stops once its budget is spent; every array is made up front, so the search
    /// itself allocates nothing, and nothing in it depends on the clock.
    /// </summary>
    internal sealed class ChessSearch
    {
        /// <summary>The deepest ply the search reaches, extensions and quiescence included.</summary>
        public const int MaxPly = 96;

        public const int Infinity = 32000;
        public const int Mate = 30000;

        /// <summary>Scores beyond this are mates, counted in plies from the root.</summary>
        public const int MateBound = Mate - MaxPly - 4;

        private const int FlagExact = 0;
        private const int FlagLower = 1;
        private const int FlagUpper = 2;

        private const int OrderTable = 4000000;
        private const int OrderNoisy = 2000000;
        private const int OrderKiller = 1500000;
        private const int OrderSecondKiller = 1400000;
        private const int OrderBadCapture = 1200000;
        private const int HistoryLimit = 1000000;

        private readonly ChessPosition position = new ChessPosition();
        private readonly int budget;
        private readonly int[] moves = new int[MaxPly * ChessPosition.MaxMoves];
        private readonly int[] order = new int[MaxPly * ChessPosition.MaxMoves];
        private readonly int[] killers = new int[MaxPly * 2];
        private readonly int[] history = new int[2 * 64 * 64];
        private readonly int[] gains = new int[32];
        /// <summary>The keys of the game's positions before the root, oldest first (for repetitions across the root).</summary>
        private readonly ulong[] earlier;
        private readonly int tableMask;
        private readonly ulong[] tableKeys;
        private readonly int[] tableMoves;
        private readonly int[] tableData;

        /// <summary>The legal moves of the root, best first after each iteration, and their scores from it.</summary>
        private readonly int[] rootMoves = new int[ChessPosition.MaxMoves];
        private readonly int[] rootScores = new int[ChessPosition.MaxMoves];
        private readonly int[] iterationScores = new int[ChessPosition.MaxMoves];
        private int rootCount;

        public ChessSearch(ChessGame game, int budget, int tableBits)
        {
            this.budget = budget;
            position.Load(game);
            int pairs = game.positions != null ? game.positions.Count / 2 : 0;
            // The last pair is the root position itself.
            earlier = new ulong[pairs > 0 ? pairs - 1 : 0];
            for (int i = 0; i < earlier.Length; i++)
            {
                earlier[i] = (uint)game.positions[2 * i] | ((ulong)(uint)game.positions[2 * i + 1] << 32);
            }
            int size = 1 << tableBits;
            tableMask = size - 1;
            tableKeys = new ulong[size];
            tableMoves = new int[size];
            tableData = new int[size];
            rootCount = position.Legal(rootMoves);
            for (int i = 0; i < rootCount; i++)
            {
                rootScores[i] = OrderScore(rootMoves[i], 0, 0);
            }
            SortRoot();
        }

        /// <summary>Positions visited so far.</summary>
        public int Nodes { get; private set; }

        /// <summary>Whether the budget ran out (the last iteration is then incomplete).</summary>
        public bool Stopped { get; private set; }

        public int RootCount => rootCount;

        public int RootMove(int index)
        {
            return rootMoves[index];
        }

        public int RootScore(int index)
        {
            return rootScores[index];
        }

        /// <summary>The static evaluation of the position on the board (the root between iterations), for the side to move.</summary>
        public int Evaluate()
        {
            return ChessEvaluation.Evaluate(position);
        }

        /// <summary>
        /// Searches every root move <paramref name="depth"/> plies deep. With a <paramref name="margin"/> of 0 or more,
        /// each move's window starts that far below the best score so far, so every score within the margin of the best
        /// is exact (Easy picks among them); with a negative one it is plain alpha-beta, and the scores of the moves that
        /// do not beat the best are only bounds. Returns the index of the best root move, or -1 when the budget ran out
        /// before the first move was searched. When the iteration completes, the root moves are put in order, best
        /// first. <paramref name="best"/> is the best score found.
        /// </summary>
        public int Iterate(int depth, int margin, out int best)
        {
            bool exact = margin >= 0;
            int alpha = -Infinity;
            int beta = Infinity;
            best = -Infinity;
            int bestIndex = -1;
            int[] scores = iterationScores;
            int done = 0;
            for (int i = 0; i < rootCount; i++)
            {
                int move = rootMoves[i];
                position.Make(move);
                int score;
                if (exact)
                {
                    int floor = best > -Infinity + margin + 1 ? best - margin - 1 : -Infinity;
                    score = -Negamax(depth - 1, -Infinity, -floor, 1, true);
                }
                else if (i == 0)
                {
                    score = -Negamax(depth - 1, -beta, -alpha, 1, true);
                }
                else
                {
                    score = -Negamax(depth - 1, -alpha - 1, -alpha, 1, true);
                    if (score > alpha && !Stopped)
                    {
                        score = -Negamax(depth - 1, -beta, -alpha, 1, true);
                    }
                }
                position.Unmake();
                if (Stopped)
                {
                    break;
                }
                scores[i] = score;
                done++;
                if (score > best)
                {
                    best = score;
                    bestIndex = i;
                    if (score > alpha)
                    {
                        alpha = score;
                    }
                }
            }
            if (Stopped)
            {
                return done > 0 ? bestIndex : -1;
            }
            for (int i = 0; i < rootCount; i++)
            {
                rootScores[i] = scores[i];
            }
            // The best first, then the others by their scores (ties keep their order).
            rootScores[bestIndex] = Infinity;
            SortRoot();
            rootScores[0] = best;
            return 0;
        }

        private void SortRoot()
        {
            for (int i = 1; i < rootCount; i++)
            {
                int move = rootMoves[i];
                int score = rootScores[i];
                int j = i - 1;
                while (j >= 0 && rootScores[j] < score)
                {
                    rootMoves[j + 1] = rootMoves[j];
                    rootScores[j + 1] = rootScores[j];
                    j--;
                }
                rootMoves[j + 1] = move;
                rootScores[j + 1] = score;
            }
        }

        private int Negamax(int depth, int alpha, int beta, int ply, bool allowNull)
        {
            if (Nodes >= budget)
            {
                Stopped = true;
                return 0;
            }
            bool inCheck = position.InCheck();
            if (depth <= 0 && !inCheck)
            {
                return Quiesce(alpha, beta, ply);
            }
            Nodes++;
            if (position.halfmove >= ChessGame.FiftyMovePlies || IsRepetition())
            {
                return 0;
            }
            if (ply >= MaxPly - 2)
            {
                return Evaluate();
            }
            // No mate found further away can beat one already found nearer the root.
            int mating = Mate - ply;
            if (mating < beta)
            {
                beta = mating;
                if (alpha >= beta)
                {
                    return beta;
                }
            }
            int mated = -Mate + ply;
            if (mated > alpha)
            {
                alpha = mated;
                if (alpha >= beta)
                {
                    return alpha;
                }
            }
            if (inCheck)
            {
                depth = (depth < 0 ? 0 : depth) + 1;
            }
            bool principal = beta - alpha > 1;
            int slot = (int)(position.key & (ulong)tableMask);
            int tableMove = 0;
            if (tableKeys[slot] == position.key)
            {
                tableMove = tableMoves[slot];
                int data = tableData[slot];
                int storedDepth = (data >> 16) & 255;
                if (!principal && storedDepth >= depth)
                {
                    int stored = FromTable((data & 0xFFFF) - 32768, ply);
                    int flag = (data >> 24) & 3;
                    if (flag == FlagExact || (flag == FlagLower && stored >= beta) || (flag == FlagUpper && stored <= alpha))
                    {
                        return stored;
                    }
                }
            }
            if (allowNull && !principal && !inCheck && depth >= 3 && HasPieces() && Evaluate() >= beta)
            {
                position.MakeNull();
                int reduction = depth > 6 ? 3 : 2;
                int score = -Negamax(depth - 1 - reduction, -beta, -beta + 1, ply + 1, false);
                position.UnmakeNull();
                if (Stopped)
                {
                    return 0;
                }
                if (score >= beta)
                {
                    return score >= MateBound ? beta : score;
                }
            }
            int start = ply * ChessPosition.MaxMoves;
            int end = position.Generate(moves, start, false);
            for (int i = start; i < end; i++)
            {
                order[i] = moves[i] == tableMove ? OrderTable : OrderScore(moves[i], ply, position.side);
            }
            int originalAlpha = alpha;
            int best = -Infinity;
            int bestMove = 0;
            int legal = 0;
            int killer = killers[ply * 2];
            int secondKiller = killers[ply * 2 + 1];
            for (int i = start; i < end; i++)
            {
                int move = PickNext(i, end);
                bool test = position.NeedsSafetyTest(move, inCheck);
                position.Make(move);
                if (test && !position.MoverIsSafe())
                {
                    position.Unmake();
                    continue;
                }
                legal++;
                int score;
                if (legal == 1)
                {
                    score = -Negamax(depth - 1, -beta, -alpha, ply + 1, true);
                }
                else
                {
                    int reduction = 0;
                    if (depth >= 3 && legal > 3 && !inCheck && ChessPosition.IsQuiet(move) && move != killer && move != secondKiller
                        && !position.InCheck())
                    {
                        reduction = legal > 8 && depth > 4 ? 2 : 1;
                    }
                    score = -Negamax(depth - 1 - reduction, -alpha - 1, -alpha, ply + 1, true);
                    if (score > alpha && reduction > 0 && !Stopped)
                    {
                        score = -Negamax(depth - 1, -alpha - 1, -alpha, ply + 1, true);
                    }
                    if (score > alpha && score < beta && !Stopped)
                    {
                        score = -Negamax(depth - 1, -beta, -alpha, ply + 1, true);
                    }
                }
                position.Unmake();
                if (Stopped)
                {
                    return 0;
                }
                if (score > best)
                {
                    best = score;
                    bestMove = move;
                    if (score > alpha)
                    {
                        alpha = score;
                        if (alpha >= beta)
                        {
                            if (ChessPosition.IsQuiet(move))
                            {
                                Remember(move, depth, ply);
                            }
                            break;
                        }
                    }
                }
            }
            if (legal == 0)
            {
                return inCheck ? -Mate + ply : 0;
            }
            int bound = best <= originalAlpha ? FlagUpper : best >= beta ? FlagLower : FlagExact;
            tableKeys[slot] = position.key;
            tableMoves[slot] = bestMove;
            tableData[slot] = (ToTable(best, ply) + 32768) | ((depth > 255 ? 255 : depth) << 16) | (bound << 24);
            return best;
        }

        private int Quiesce(int alpha, int beta, int ply)
        {
            if (Nodes >= budget)
            {
                Stopped = true;
                return 0;
            }
            Nodes++;
            int stand = Evaluate();
            if (ply >= MaxPly - 1 || stand >= beta)
            {
                return stand;
            }
            if (stand > alpha)
            {
                alpha = stand;
            }
            int start = ply * ChessPosition.MaxMoves;
            int end = position.Generate(moves, start, true);
            int count = start;
            for (int i = start; i < end; i++)
            {
                int move = moves[i];
                int victim = ChessPieces.Value((ChessPieceKind)ChessPosition.CapturedKind(move));
                int promotion = ChessPosition.Promotion(move);
                // Hopeless captures: even the piece taken (and 200 more) leaves the side below alpha.
                if (promotion == 0 && stand + victim + 200 <= alpha)
                {
                    continue;
                }
                // A piece taking something cheaper that the exchanges on the square lose.
                if (promotion == 0 && LosesMaterial(move))
                {
                    continue;
                }
                moves[count] = move;
                order[count] = NoisyOrder(move);
                count++;
            }
            int best = stand;
            bool inCheck = count > start && position.InCheck();
            for (int i = start; i < count; i++)
            {
                int move = PickNext(i, count);
                bool test = position.NeedsSafetyTest(move, inCheck);
                position.Make(move);
                if (test && !position.MoverIsSafe())
                {
                    position.Unmake();
                    continue;
                }
                int score = -Quiesce(-beta, -alpha, ply + 1);
                position.Unmake();
                if (Stopped)
                {
                    return 0;
                }
                if (score > best)
                {
                    best = score;
                    if (score > alpha)
                    {
                        alpha = score;
                        if (alpha >= beta)
                        {
                            break;
                        }
                    }
                }
            }
            return best;
        }

        /// <summary>Whether a capture by a piece worth more than its victim loses material once the exchanges on the square are done.</summary>
        private bool LosesMaterial(int move)
        {
            int attacker = ChessPieces.Value((ChessPieceKind)ChessPosition.MovingKind(move));
            int victim = ChessPieces.Value((ChessPieceKind)ChessPosition.CapturedKind(move));
            return attacker > victim && position.StaticExchange(move, gains) < 0;
        }

        /// <summary>Most valuable victim first, then least valuable attacker, promotions by the piece they make.</summary>
        private static int NoisyOrder(int move)
        {
            return OrderNoisy + ChessPieces.Value((ChessPieceKind)ChessPosition.CapturedKind(move)) * 8
                + ChessPieces.Value((ChessPieceKind)ChessPosition.Promotion(move)) - ChessPosition.MovingKind(move);
        }

        /// <summary>Swaps the best ordered move of <c>[index, end)</c> to <paramref name="index"/> and returns it.</summary>
        private int PickNext(int index, int end)
        {
            int bestIndex = index;
            int bestOrder = order[index];
            for (int i = index + 1; i < end; i++)
            {
                if (order[i] > bestOrder)
                {
                    bestOrder = order[i];
                    bestIndex = i;
                }
            }
            int move = moves[bestIndex];
            if (bestIndex != index)
            {
                moves[bestIndex] = moves[index];
                order[bestIndex] = order[index];
                moves[index] = move;
                order[index] = bestOrder;
            }
            return move;
        }

        private int OrderScore(int move, int ply, int side)
        {
            int captured = ChessPosition.CapturedKind(move);
            int promotion = ChessPosition.Promotion(move);
            if (captured != 0 || promotion != 0)
            {
                // A capture that loses material comes after the killers.
                if (promotion == 0 && LosesMaterial(move))
                {
                    return OrderBadCapture + NoisyOrder(move) - OrderNoisy;
                }
                return NoisyOrder(move);
            }
            if (move == killers[ply * 2])
            {
                return OrderKiller;
            }
            if (move == killers[ply * 2 + 1])
            {
                return OrderSecondKiller;
            }
            return history[side * 4096 + ChessPosition.From(move) * 64 + ChessPosition.To(move)];
        }

        /// <summary>A quiet move that cut the search off: a killer of its ply and a point in the history of its squares.</summary>
        private void Remember(int move, int depth, int ply)
        {
            if (killers[ply * 2] != move)
            {
                killers[ply * 2 + 1] = killers[ply * 2];
                killers[ply * 2] = move;
            }
            int index = position.side * 4096 + ChessPosition.From(move) * 64 + ChessPosition.To(move);
            history[index] += depth * depth;
            if (history[index] > HistoryLimit)
            {
                for (int i = 0; i < history.Length; i++)
                {
                    history[i] >>= 1;
                }
            }
        }

        /// <summary>
        /// Whether the position stood before since the last capture or pawn move, on the way from the root or in the
        /// game before it: the search takes a repetition as a draw.
        /// </summary>
        private bool IsRepetition()
        {
            ulong key = position.key;
            int reach = position.halfmove;
            int inSearch = reach < position.depth ? reach : position.depth;
            for (int back = 4; back <= inSearch; back += 2)
            {
                if (position.KeyBefore(back) == key)
                {
                    return true;
                }
            }
            if (reach <= position.depth)
            {
                return false;
            }
            for (int i = earlier.Length - 1; i >= 0; i--)
            {
                int back = position.depth + earlier.Length - i;
                if (back > reach)
                {
                    break;
                }
                if ((back & 1) == 0 && earlier[i] == key)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Whether the side to move has a piece other than pawns and the king (a null move is unsafe without one: zugzwang).</summary>
        private bool HasPieces()
        {
            ulong[] bits = position.pieceBits;
            if (position.side == 0)
            {
                return (bits[6 + 2] | bits[6 + 3] | bits[6 + 4] | bits[6 + 5]) != 0UL;
            }
            return (bits[6 - 2] | bits[6 - 3] | bits[6 - 4] | bits[6 - 5]) != 0UL;
        }


        private static int ToTable(int score, int ply)
        {
            if (score >= MateBound)
            {
                return score + ply;
            }
            return score <= -MateBound ? score - ply : score;
        }

        private static int FromTable(int score, int ply)
        {
            if (score >= MateBound)
            {
                return score - ply;
            }
            return score <= -MateBound ? score + ply : score;
        }
    }
}
