using System.Collections.Generic;
using Gamebox.Lockstep;

namespace Portfolio.BoardGames
{
    /// <summary>
    /// The computer's chess: alpha-beta search with iterative deepening and a quiescence search, within a budget of
    /// positions per level (never the clock), so the same game, level and random numbers give the same move anywhere.
    /// Each iteration searches a ply deeper, the best move of the last one first; when the budget runs out, the best
    /// move of the last complete iteration is played (or the partial one's, when it already beat it). Easy looks two
    /// plies ahead at most and picks at random among the moves within <see cref="EasyMargin"/> of its best.
    /// </summary>
    public static class ChessAI
    {
        public const int Levels = 3;

        /// <summary>How far below its best move (centipawns) Easy still picks a move at random.</summary>
        public const int EasyMargin = 60;

        private static readonly int[] budgets = { 2500, 40000, 250000 };
        private static readonly int[] depths = { 2, 48, 48 };
        /// <summary>The size of the table of searched positions per level, as a power of two.</summary>
        private static readonly int[] tableBits = { 12, 16, 17 };

        /// <summary>The move command for <paramref name="seat"/> (the seat to move); null when there is no legal move.</summary>
        public static BoardCommand Choose(ChessGame game, int seat, int level, IRandom random)
        {
            ChessMove move = ChooseMove(game, level, random);
            return move != null ? BoardCommand.Play(seat, move.Path()) : null;
        }

        /// <summary>The move the computer plays at <paramref name="level"/>; null when there is none.</summary>
        public static ChessMove ChooseMove(ChessGame game, int level, IRandom random)
        {
            ChessThought thought = Think(game, level, random);
            return thought != null ? thought.move : null;
        }

        /// <summary>Positions the search may visit per move at a level.</summary>
        public static int NodeBudget(int level)
        {
            return budgets[Clamp(level)];
        }

        /// <summary>The deepest iteration of a level (the budget usually ends the search first).</summary>
        public static int MaxDepth(int level)
        {
            return depths[Clamp(level)];
        }

        /// <summary>
        /// The move the computer plays at <paramref name="level"/> with what the search found on the way: its score,
        /// the depth of the last complete iteration and the positions visited. Null when there is no legal move. It
        /// changes nothing of <paramref name="game"/> but its cached legal moves.
        /// </summary>
        public static ChessThought Think(ChessGame game, int level, IRandom random)
        {
            if (game == null || game.IsOver)
            {
                return null;
            }
            IReadOnlyList<ChessMove> legal = game.Legal();
            if (legal.Count == 0)
            {
                return null;
            }
            level = Clamp(level);
            if (legal.Count == 1)
            {
                return new ChessThought { move = legal[0] };
            }
            var search = new ChessSearch(game, budgets[level], tableBits[level]);
            bool easy = level == 0;
            int bestMove = search.RootMove(0);
            int bestScore = 0;
            int reached = 0;
            int[] easyMoves = null;
            int[] easyScores = null;
            for (int depth = 1; depth <= depths[level]; depth++)
            {
                int index = search.Iterate(depth, easy ? EasyMargin : -1, out int score);
                if (search.Stopped)
                {
                    // A move of the unfinished iteration that beat the previous best (searched first) is better still;
                    // the first iteration's best so far beats the moves nobody looked at. Easy keeps the scores it picks
                    // from, those of the last whole iteration.
                    if (index >= 0 && (reached == 0 || (index > 0 && !easy)))
                    {
                        bestMove = search.RootMove(index);
                        bestScore = score;
                    }
                    break;
                }
                reached = depth;
                bestMove = search.RootMove(0);
                bestScore = score;
                if (easy)
                {
                    easyMoves = new int[search.RootCount];
                    easyScores = new int[search.RootCount];
                    for (int i = 0; i < easyMoves.Length; i++)
                    {
                        easyMoves[i] = search.RootMove(i);
                        easyScores[i] = search.RootScore(i);
                    }
                }
                // A mate found within the depth searched is the nearest there is, for either side.
                if (score >= ChessSearch.MateBound && ChessSearch.Mate - score <= depth)
                {
                    break;
                }
                if (score <= -ChessSearch.MateBound && ChessSearch.Mate + score <= depth)
                {
                    break;
                }
            }
            if (easy && random != null && easyMoves != null)
            {
                var near = new List<int>();
                for (int i = 0; i < easyMoves.Length; i++)
                {
                    if (easyScores[i] >= bestScore - EasyMargin)
                    {
                        near.Add(i);
                    }
                }
                int pick = near[random.Range(0, near.Count)];
                bestMove = easyMoves[pick];
                bestScore = easyScores[pick];
            }
            return new ChessThought
            {
                move = Find(legal, bestMove),
                score = bestScore,
                depth = reached,
                nodes = search.Nodes
            };
        }

        private static ChessMove Find(IReadOnlyList<ChessMove> legal, int move)
        {
            int from = ChessPosition.From(move);
            int to = ChessPosition.To(move);
            int promotion = ChessPosition.Promotion(move);
            for (int i = 0; i < legal.Count; i++)
            {
                if (legal[i].from == from && legal[i].to == to && legal[i].promotion == promotion)
                {
                    return legal[i];
                }
            }
            return legal[0];
        }

        private static int Clamp(int level)
        {
            return level < 0 ? 0 : level >= Levels ? Levels - 1 : level;
        }
    }

    /// <summary>What one search of the chess computer came to: the move, its score for the mover, how deep and how many positions.</summary>
    public sealed class ChessThought
    {
        public ChessMove move;
        /// <summary>Centipawns for the side to move; beyond 29000 a mate (30000 less the plies to it).</summary>
        public int score;
        /// <summary>The depth of the last complete iteration (0 when the move was forced or the first one did not finish).</summary>
        public int depth;
        public int nodes;
    }
}
