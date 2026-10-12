using System.Collections.Generic;

namespace Portfolio.BoardGames.Tests
{
    /// <summary>
    /// Backgammon positions for the tests, written in each side's own point numbers ("w 6 3": three of White's checkers on
    /// White's 6-point; 25 is the bar; checkers not placed are borne off), and a plain reference of the rules: every order
    /// of the dice tried on the board itself, kept to the plays that use the most dice (the larger die when only one can
    /// be played), to check the generator of the game against.
    /// </summary>
    public static class BackgammonPositions
    {
        /// <summary>A game with the checkers given; with dice it waits for <paramref name="toAct"/>'s play, without for its roll.</summary>
        public static BackgammonGame Game(Side toAct, int die1, int die2, params string[] checkers)
        {
            var game = new BackgammonGame
            {
                cube = true,
                sideToAct = (int)toAct,
                phase = (int)(die1 > 0 ? BackgammonPhase.Moving : BackgammonPhase.Rolling),
                die1 = die1,
                die2 = die2
            };
            var placed = new int[2];
            foreach (string entry in checkers)
            {
                string[] parts = entry.Split(' ');
                Side side = parts[0] == "w" ? Side.Light : Side.Dark;
                int point = int.Parse(parts[1]);
                int count = int.Parse(parts[2]);
                if (point == 25)
                {
                    game.bar[(int)side] += count;
                }
                else
                {
                    int index = BackgammonGame.PointIndex(side, point);
                    if (game.board[index] != 0 && (game.board[index] > 0) != (side == Side.Light))
                    {
                        throw new System.ArgumentException("both sides on one point: " + entry);
                    }
                    game.board[index] += side == Side.Light ? count : -count;
                }
                placed[(int)side] += count;
            }
            game.off[0] = BackgammonGame.CheckersPerSide - placed[0];
            game.off[1] = BackgammonGame.CheckersPerSide - placed[1];
            return game;
        }

        /// <summary>The place a move names for <paramref name="side"/>'s own point (25 the bar, 0 off).</summary>
        public static int Place(Side side, int ownPoint)
        {
            return ownPoint >= 25 ? BackgammonGame.Bar : ownPoint <= 0 ? BackgammonGame.Off : BackgammonGame.PointIndex(side, ownPoint);
        }

        /// <summary>The numbers of a move command from pairs of <paramref name="side"/>'s own points.</summary>
        public static int[] Path(Side side, params int[] ownPoints)
        {
            var path = new int[ownPoints.Length];
            for (int i = 0; i < ownPoints.Length; i++)
            {
                path[i] = Place(side, ownPoints[i]);
            }
            return path;
        }

        public static CheckerMove Move(Side side, int from, int to, int die)
        {
            return new CheckerMove(Place(side, from), Place(side, to), die);
        }

        /// <summary>The checkers of a game as text, to compare positions.</summary>
        public static string Key(BackgammonGame game)
        {
            return string.Join(",", game.board) + "|" + game.bar[0] + "," + game.bar[1] + "|" + game.off[0] + "," + game.off[1];
        }

        /// <summary>The positions the legal plays of the game lead to, sorted.</summary>
        public static List<string> Results(BackgammonGame game)
        {
            var keys = new List<string>();
            foreach (BackgammonPlay play in game.LegalPlays())
            {
                keys.Add(Key(game.Preview(play.moves)));
            }
            keys.Sort(System.StringComparer.Ordinal);
            return keys;
        }

        /// <summary>The positions the rules allow for the game's dice, by the plain reference, sorted.</summary>
        public static List<string> ReferenceResults(BackgammonGame game)
        {
            Side side = game.SideToAct;
            int[] dice = game.DiceToPlay();
            var leaves = new List<Leaf>();
            var used = new bool[dice.Length];
            Explore(game.Clone(), side, dice, used, 0, 0, leaves);
            int most = 0;
            foreach (Leaf leaf in leaves)
            {
                most = leaf.Used > most ? leaf.Used : most;
            }
            int larger = game.die1 > game.die2 ? game.die1 : game.die2;
            bool largerPlayable = false;
            if (!game.IsDouble && most == 1)
            {
                foreach (Leaf leaf in leaves)
                {
                    largerPlayable |= leaf.Used == 1 && leaf.FirstDie == larger;
                }
            }
            var keys = new SortedSet<string>(System.StringComparer.Ordinal);
            foreach (Leaf leaf in leaves)
            {
                if (leaf.Used == most && (!largerPlayable || leaf.FirstDie == larger))
                {
                    keys.Add(leaf.Key);
                }
            }
            return new List<string>(keys);
        }

        private struct Leaf
        {
            public string Key;
            public int Used;
            public int FirstDie;
        }

        private static void Explore(BackgammonGame board, Side side, int[] dice, bool[] used, int count, int firstDie, List<Leaf> leaves)
        {
            bool moved = false;
            for (int d = 0; d < dice.Length; d++)
            {
                bool same = false;
                for (int e = 0; e < d; e++)
                {
                    same |= !used[e] && dice[e] == dice[d];
                }
                if (used[d] || same)
                {
                    continue;
                }
                for (int from = 0; from <= BackgammonGame.Bar; from++)
                {
                    if (!TryMove(board, side, from, dice[d], out int to))
                    {
                        continue;
                    }
                    moved = true;
                    BackgammonGame next = board.Clone();
                    Apply(next, side, from, to);
                    used[d] = true;
                    Explore(next, side, dice, used, count + 1, count == 0 ? dice[d] : firstDie, leaves);
                    used[d] = false;
                }
            }
            if (!moved)
            {
                leaves.Add(new Leaf { Key = Key(board), Used = count, FirstDie = firstDie });
            }
        }

        /// <summary>The rules of one checker's move, written plainly on the board's indices.</summary>
        public static bool TryMove(BackgammonGame board, Side side, int from, int die, out int to)
        {
            to = -1;
            int s = (int)side;
            int sign = side == Side.Light ? 1 : -1;
            if (board.bar[s] > 0 && from != BackgammonGame.Bar)
            {
                return false;
            }
            int target;
            if (from == BackgammonGame.Bar)
            {
                if (board.bar[s] == 0)
                {
                    return false;
                }
                target = side == Side.Light ? 24 - die : die - 1;
            }
            else
            {
                if (board.board[from] * sign <= 0)
                {
                    return false;
                }
                target = from + BackgammonGame.Direction(side) * die;
            }
            if (target >= 0 && target < 24)
            {
                if (board.board[target] * sign <= -2)
                {
                    return false;
                }
                to = target;
                return true;
            }
            for (int i = 0; i < 24; i++)
            {
                bool home = side == Side.Light ? i < 6 : i >= 18;
                if (!home && board.board[i] * sign > 0)
                {
                    return false;
                }
            }
            bool exact = side == Side.Light ? target == -1 : target == 24;
            if (!exact)
            {
                // A larger die only from the point farthest from off.
                if (side == Side.Light)
                {
                    for (int i = from + 1; i < 6; i++)
                    {
                        if (board.board[i] > 0)
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    for (int i = 18; i < from; i++)
                    {
                        if (board.board[i] < 0)
                        {
                            return false;
                        }
                    }
                }
            }
            to = BackgammonGame.Off;
            return true;
        }

        public static void Apply(BackgammonGame board, Side side, int from, int to)
        {
            int s = (int)side;
            int sign = side == Side.Light ? 1 : -1;
            if (from == BackgammonGame.Bar)
            {
                board.bar[s]--;
            }
            else
            {
                board.board[from] -= sign;
            }
            if (to == BackgammonGame.Off)
            {
                board.off[s]++;
                return;
            }
            if (board.board[to] == -sign)
            {
                board.board[to] = 0;
                board.bar[1 - s]++;
            }
            board.board[to] += sign;
        }

        /// <summary>Every sequence of single moves <see cref="BackgammonGame.NextMoves"/> offers until it offers nothing more.</summary>
        public static List<List<CheckerMove>> StepSequences(BackgammonGame game)
        {
            var sequences = new List<List<CheckerMove>>();
            Steps(game, new List<CheckerMove>(), sequences);
            return sequences;
        }

        private static void Steps(BackgammonGame game, List<CheckerMove> done, List<List<CheckerMove>> sequences)
        {
            IReadOnlyList<CheckerMove> next = game.NextMoves(done);
            if (next.Count == 0)
            {
                sequences.Add(new List<CheckerMove>(done));
                return;
            }
            foreach (CheckerMove move in next)
            {
                done.Add(move);
                Steps(game, done, sequences);
                done.RemoveAt(done.Count - 1);
            }
        }

        public static int[] PathOf(IReadOnlyList<CheckerMove> moves)
        {
            return new BackgammonPlay(moves).Path();
        }
    }
}
