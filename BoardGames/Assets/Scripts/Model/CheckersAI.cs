using System.Collections.Generic;
using Gamebox.Lockstep;

namespace Portfolio.Checkers
{
    /// <summary>
    /// The computer player: negamax with alpha-beta over copies of the game, deepened a ply at a time until the level's
    /// depth or its budget of positions is reached (the last whole iteration counts). Deterministic for a given game,
    /// level and random, so the lockstep timeouts can run it on every device with the table's seeded random; only Easy
    /// draws on the random, to pick among the moves close to its best.
    /// </summary>
    public static class CheckersAI
    {
        /// <summary>0 Easy, 1 Normal, 2 Hard: the order of LocalPlayRules.ComputerLevels.</summary>
        public const int Levels = 3;

        /// <summary>The value of a won game, less the plies it took.</summary>
        public const int WinValue = 100000;

        /// <summary>How far below its best move Easy still picks a move at random.</summary>
        public const int EasyMargin = 15;

        private static readonly int[] budgets = { 1500, 25000, 150000 };
        private static readonly int[] depths = { 2, 4, 6 };

        /// <summary>The positions a search of <paramref name="level"/> may look at before it stops.</summary>
        public static int NodeBudget(int level)
        {
            return budgets[Clamp(level)];
        }

        /// <summary>The deepest ply <paramref name="level"/> searches.</summary>
        public static int MaxDepth(int level)
        {
            return depths[Clamp(level)];
        }

        /// <summary>The move the computer makes at <paramref name="level"/>; null only when there is no legal move.</summary>
        public static Move Choose(CheckersGame game, int level, IRandom random)
        {
            if (game == null || game.IsOver)
            {
                return null;
            }
            IReadOnlyList<Move> legal = game.Legal();
            if (legal.Count == 0)
            {
                return null;
            }
            if (legal.Count == 1)
            {
                return legal[0];
            }
            level = Clamp(level);
            var search = new Search(NodeBudget(level));
            bool fullWindow = level == 0;
            Move best = legal[0];
            int[] bestScores = null;
            for (int depth = 1; depth <= depths[level]; depth++)
            {
                var scores = new int[legal.Count];
                Move pick = search.Root(game, legal, depth, fullWindow, scores);
                if (search.Exhausted)
                {
                    break;
                }
                best = pick;
                bestScores = scores;
            }
            if (level == 0 && random != null && bestScores != null)
            {
                int top = int.MinValue;
                for (int i = 0; i < bestScores.Length; i++)
                {
                    if (bestScores[i] > top)
                    {
                        top = bestScores[i];
                    }
                }
                var near = new List<int>();
                for (int i = 0; i < bestScores.Length; i++)
                {
                    if (bestScores[i] >= top - EasyMargin)
                    {
                        near.Add(i);
                    }
                }
                return legal[near[random.Range(0, near.Count)]];
            }
            return best;
        }

        /// <summary>
        /// How good the position is for <paramref name="forSide"/>: material and placement, plus the mobility of the side
        /// to move (the one term the search leaves out below the root); a finished game is worth +-WinValue less the plies.
        /// </summary>
        public static int Evaluate(CheckersGame game, Side forSide)
        {
            if (game.IsOver)
            {
                return Terminal(game, forSide);
            }
            int mobility = game.Legal().Count;
            return Static(game, forSide) + (game.SideToMove == forSide ? mobility : -mobility);
        }

        private static int Clamp(int level)
        {
            return level < 0 ? 0 : level >= Levels ? Levels - 1 : level;
        }

        private static int Terminal(CheckersGame game, Side forSide)
        {
            Side? winner = game.Winner;
            if (!winner.HasValue)
            {
                return 0;
            }
            return winner.Value == forSide ? WinValue - game.ply : -(WinValue - game.ply);
        }

        /// <summary>Material and placement alone: 100 a man, 300 a king (250 English), advance, back rank, centre.</summary>
        private static int Static(CheckersGame game, Side forSide)
        {
            VariantRules rules = game.Rules;
            int size = rules.Size;
            int kingValue = rules.Variant == Variant.English ? 250 : 300;
            bool backRankBonus = rules.Variant != Variant.International;
            sbyte[] cells = game.board.cells;
            int total = 0;
            for (int square = 0; square < cells.Length; square++)
            {
                sbyte piece = cells[square];
                if (piece == Pieces.None)
                {
                    continue;
                }
                int row = square / size;
                int col = square % size;
                Side side = Pieces.SideOf(piece);
                int value;
                if (Pieces.IsKing(piece))
                {
                    value = kingValue;
                }
                else
                {
                    value = 100;
                    int home = side == Side.Light ? 0 : size - 1;
                    int advanced = side == Side.Light ? row : size - 1 - row;
                    value += 2 * advanced;
                    if (backRankBonus && row == home)
                    {
                        value += 6;
                    }
                }
                if (row >= 2 && row <= size - 3 && col >= 2 && col <= size - 3)
                {
                    value += 3;
                }
                total += side == forSide ? value : -value;
            }
            return total;
        }

        /// <summary>One search with one budget of positions; once the budget is spent every answer is void.</summary>
        private sealed class Search
        {
            private readonly int budget;
            private int nodes;

            public Search(int budget)
            {
                this.budget = budget;
            }

            public bool Exhausted { get; private set; }

            /// <summary>
            /// Scores every root move to <paramref name="depth"/> plies and returns the best, the first in generation
            /// order on a tie. With <paramref name="fullWindow"/> every root move gets its exact score (for Easy's pick).
            /// </summary>
            public Move Root(CheckersGame game, IReadOnlyList<Move> legal, int depth, bool fullWindow, int[] scores)
            {
                int alpha = -WinValue - 1;
                int beta = WinValue + 1;
                int best = int.MinValue;
                Move pick = legal[0];
                for (int i = 0; i < legal.Count; i++)
                {
                    CheckersGame child = game.Clone();
                    child.Play(legal[i]);
                    if (++nodes >= budget)
                    {
                        Exhausted = true;
                        return pick;
                    }
                    int score = -Negamax(child, depth - 1, -beta, -alpha);
                    if (Exhausted)
                    {
                        return pick;
                    }
                    scores[i] = score;
                    if (score > best)
                    {
                        best = score;
                        pick = legal[i];
                    }
                    if (!fullWindow && score > alpha)
                    {
                        alpha = score;
                    }
                }
                return pick;
            }

            private int Negamax(CheckersGame game, int depth, int alpha, int beta)
            {
                if (game.IsOver)
                {
                    return Terminal(game, game.SideToMove);
                }
                if (depth <= 0)
                {
                    return Static(game, game.SideToMove);
                }
                IReadOnlyList<Move> moves = game.Legal();
                int best = int.MinValue;
                for (int i = 0; i < moves.Count; i++)
                {
                    CheckersGame child = game.Clone();
                    child.Play(moves[i]);
                    if (++nodes >= budget)
                    {
                        Exhausted = true;
                        return 0;
                    }
                    int score = -Negamax(child, depth - 1, -beta, -alpha);
                    if (Exhausted)
                    {
                        return 0;
                    }
                    if (score > best)
                    {
                        best = score;
                    }
                    if (score > alpha)
                    {
                        alpha = score;
                    }
                    if (alpha >= beta)
                    {
                        break;
                    }
                }
                return best;
            }
        }
    }
}
