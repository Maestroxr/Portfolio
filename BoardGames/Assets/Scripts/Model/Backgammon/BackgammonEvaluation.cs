namespace Portfolio.BoardGames
{
    /// <summary>
    /// What a backgammon position is worth, in tenths of a pip, for the side that has just played while the other side
    /// is to roll; both sides in their own numbers (see <see cref="BackgammonPlayGenerator"/>). While the checkers can
    /// still meet it weighs the race, the points made and their places, primes in front of the opponent's back checkers,
    /// the home board against checkers on the bar, the checkers left back, and every blot by the rolls of the 36 that
    /// hit it (blocked points and the bar taken into account) times what the hit would cost. Once contact is over it
    /// is a race counted the Keith way. The chances of winning come from the same numbers. Whole numbers only, so every
    /// device judges alike.
    /// </summary>
    internal static class BackgammonEvaluation
    {
        /// <summary>The score of a game already won, times what it is worth (1 to 3).</summary>
        public const int WonScore = 30000;

        /// <summary>The 21 different rolls (each non-double counts twice of the 36).</summary>
        public static readonly int[] RollA = new int[21];
        public static readonly int[] RollB = new int[21];
        public static readonly int[] RollWeight = new int[21];

        /// <summary>What a made point is worth by its place, in the owner's numbers (index 1 to 24).</summary>
        private static readonly int[] PointValue =
        {
            0, 0, 5, 15, 35, 55, 60, 50, 30, 15, 10, 8, 5, 5, 5, 5, 5, 5, 15, 20, 40, 35, 20, 10, 5
        };

        /// <summary>What a prime in front of opposing checkers is worth by its length.</summary>
        private static readonly int[] PrimeBonus = { 0, 0, 10, 30, 70, 140, 260 };

        static BackgammonEvaluation()
        {
            int i = 0;
            for (int a = 1; a <= 6; a++)
            {
                for (int b = a; b <= 6; b++)
                {
                    RollA[i] = a;
                    RollB[i] = b;
                    RollWeight[i] = a == b ? 1 : 2;
                    i++;
                }
            }
        }

        /// <summary>The position for <paramref name="me"/>, who has just played; <paramref name="them"/> rolls next.</summary>
        public static int Evaluate(int[] me, int[] them)
        {
            if (me[0] >= BackgammonGame.CheckersPerSide)
            {
                return WonScore * Multiplier(them);
            }
            if (them[0] >= BackgammonGame.CheckersPerSide)
            {
                return -WonScore * Multiplier(me);
            }
            if (!HasContact(me, them))
            {
                return Race(me, them);
            }
            int score = (Pips(them) - Pips(me)) * 10 - 40;
            score += Structure(me, them) - Structure(them, me);
            score -= BlotRisk(me, them);
            return score;
        }

        /// <summary>A race: the Keith counts compared, the side to roll a little ahead for being on roll.</summary>
        public static int Race(int[] me, int[] them)
        {
            return (Keith(them) - Keith(me)) * 10 - 40 - CheckersLeft(me) * 5 + CheckersLeft(them) * 5;
        }

        /// <summary>What a finished game is worth from the loser's checkers: 1, 2 for a gammon, 3 for a backgammon.</summary>
        public static int Multiplier(int[] loser)
        {
            if (loser[0] > 0)
            {
                return 1;
            }
            int back = loser[BackgammonPlayGenerator.BarPoint];
            for (int p = 19; p <= 24; p++)
            {
                back += loser[p];
            }
            return back > 0 ? 3 : 2;
        }

        /// <summary>Whether some checker of one side still has to pass a checker of the other.</summary>
        public static bool HasContact(int[] me, int[] them)
        {
            return Rearmost(me) + Rearmost(them) > 25;
        }

        /// <summary>The highest point (25 the bar) a checker of the side stands on; 0 when all are off.</summary>
        public static int Rearmost(int[] side)
        {
            for (int p = 25; p >= 1; p--)
            {
                if (side[p] > 0)
                {
                    return p;
                }
            }
            return 0;
        }

        public static int Pips(int[] side)
        {
            int pips = 0;
            for (int p = 1; p <= 25; p++)
            {
                pips += side[p] * p;
            }
            return pips;
        }

        public static int CheckersLeft(int[] side)
        {
            return BackgammonGame.CheckersPerSide - side[0];
        }

        /// <summary>
        /// The Keith count: the pips, plus 2 for each checker beyond one on the 1-point, 1 for each beyond one on the
        /// 2-point and beyond three on the 3-point, and 1 for each empty 4, 5 or 6-point.
        /// </summary>
        public static int Keith(int[] side)
        {
            int count = Pips(side);
            if (side[1] > 1)
            {
                count += 2 * (side[1] - 1);
            }
            if (side[2] > 1)
            {
                count += side[2] - 1;
            }
            if (side[3] > 3)
            {
                count += side[3] - 3;
            }
            if (CheckersLeft(side) > 3)
            {
                for (int p = 4; p <= 6; p++)
                {
                    if (side[p] == 0)
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        /// <summary>
        /// The chances, in thousandths, that <paramref name="roller"/> (about to roll) wins: the Keith counts in a race
        /// (the roller's count raised by a seventh; even counts after that are about 81 percent), the evaluation otherwise.
        /// </summary>
        public static int WinPermille(int[] roller, int[] other)
        {
            if (roller[0] >= BackgammonGame.CheckersPerSide)
            {
                return 1000;
            }
            if (other[0] >= BackgammonGame.CheckersPerSide)
            {
                return 0;
            }
            if (!HasContact(roller, other))
            {
                return RacePermille(roller, other);
            }
            return Softsign(-Evaluate(other, roller), 250);
        }

        /// <summary>The chances of <paramref name="me"/>, who has just played, with <paramref name="score"/> its evaluation.</summary>
        public static int WinPermilleAfter(int[] me, int[] them, int score)
        {
            if (!HasContact(me, them) && me[0] < BackgammonGame.CheckersPerSide && them[0] < BackgammonGame.CheckersPerSide)
            {
                return 1000 - RacePermille(them, me);
            }
            return Softsign(score, 250);
        }

        /// <summary>
        /// The race chances of the roller: the Keith double point (the roller's raised count at most 4 above the other's)
        /// comes out at 68 percent, the take point (2 above) at 76.
        /// </summary>
        public static int RacePermille(int[] roller, int[] other)
        {
            long diff7 = 7L * Keith(other) - 8L * Keith(roller);
            long num = diff7 * 100 + 4312;
            long abs = num < 0 ? -num : num;
            return (int)(500 + 500 * num / (2688 + abs));
        }

        /// <summary>A smooth step from 0 to 1000 through 500 at 0, a half way (750) at <paramref name="scale"/>.</summary>
        public static int Softsign(int value, int scale)
        {
            long v = value;
            long abs = v < 0 ? -v : v;
            return (int)(500 + 500 * v / (scale + abs));
        }

        /// <summary>
        /// Whether <paramref name="other"/> may well lose a gammon to <paramref name="roller"/>: nothing borne off and, in a
        /// race, more pips to bring every checker home and one off than the roller needs to finish.
        /// </summary>
        public static bool GammonChance(int[] roller, int[] other)
        {
            if (other[0] > 0)
            {
                return false;
            }
            if (HasContact(roller, other))
            {
                return true;
            }
            int need = 0;
            for (int p = 7; p <= 25; p++)
            {
                need += other[p] * (p - 6);
            }
            int lowest = 6;
            for (int p = 1; p <= 6; p++)
            {
                if (other[p] > 0)
                {
                    lowest = p;
                    break;
                }
            }
            need += lowest;
            return need >= Keith(roller);
        }

        /// <summary>What the points, primes, home board and back checkers of <paramref name="a"/> are worth against <paramref name="b"/>.</summary>
        private static int Structure(int[] a, int[] b)
        {
            int score = 0;
            int home = 0;
            for (int p = 1; p <= 24; p++)
            {
                int n = a[p];
                if (n >= 2)
                {
                    score += PointValue[p];
                    if (p <= 6)
                    {
                        home++;
                    }
                    if (n > 3)
                    {
                        score -= (n - 3) * 8;
                    }
                }
            }
            // A prime counts in front of the opposing checker farthest back (in a's numbers it stands on 25 - its point).
            int behind = 25 - Rearmost(b);
            int run = 0;
            int best = 0;
            for (int p = behind + 1; p <= 24; p++)
            {
                if (a[p] >= 2)
                {
                    run++;
                    if (run > best)
                    {
                        best = run;
                    }
                }
                else
                {
                    run = 0;
                }
            }
            score += PrimeBonus[best > 6 ? 6 : best];
            // Opposing checkers on the bar against this home board.
            score += b[BackgammonPlayGenerator.BarPoint] * (30 + home * home * 8);
            // Checkers still in the opponent's home board (or on the bar), safer on an anchor.
            int back = a[BackgammonPlayGenerator.BarPoint];
            bool anchor = false;
            for (int p = 19; p <= 24; p++)
            {
                back += a[p];
                anchor |= a[p] >= 2;
            }
            if (back > 0)
            {
                int theirHome = 0;
                for (int p = 1; p <= 6; p++)
                {
                    if (b[p] >= 2)
                    {
                        theirHome++;
                    }
                }
                int penalty = back * (6 + theirHome * 6);
                score -= anchor ? penalty / 2 : penalty;
            }
            return score;
        }

        /// <summary>
        /// What <paramref name="me"/>'s blots are likely to cost: over the 36 rolls of <paramref name="them"/>, the worst
        /// hit each roll allows (the pips the checker loses plus the trouble of coming in against their home board).
        /// </summary>
        public static int BlotRisk(int[] me, int[] them)
        {
            int blots = 0;
            for (int p = 1; p <= 24; p++)
            {
                if (me[p] == 1)
                {
                    blots++;
                }
            }
            if (blots == 0)
            {
                return 0;
            }
            int theirHome = 0;
            for (int p = 1; p <= 6; p++)
            {
                if (them[p] >= 2)
                {
                    theirHome++;
                }
            }
            int barCost = 40 + 25 * theirHome + (theirHome >= 5 ? 120 : 0);
            int total = 0;
            for (int r = 0; r < 21; r++)
            {
                int a = RollA[r];
                int b = RollB[r];
                // The blot nearest my home loses the most pips: the first one the roll hits is the worst.
                for (int p = 1; p <= 24; p++)
                {
                    if (me[p] == 1 && CanHit(me, them, 25 - p, a, b))
                    {
                        total += RollWeight[r] * ((25 - p) * 10 + barCost);
                        break;
                    }
                }
            }
            return total / 36;
        }

        /// <summary>The rolls of the 36 with which <paramref name="them"/> could hit some blot of <paramref name="me"/>.</summary>
        public static int Shots(int[] me, int[] them)
        {
            int shots = 0;
            for (int r = 0; r < 21; r++)
            {
                for (int p = 1; p <= 24; p++)
                {
                    if (me[p] == 1 && CanHit(me, them, 25 - p, RollA[r], RollB[r]))
                    {
                        shots += RollWeight[r];
                        break;
                    }
                }
            }
            return shots;
        }

        /// <summary>
        /// Whether <paramref name="them"/>, rolling <paramref name="a"/> and <paramref name="b"/>, can land on its point
        /// <paramref name="target"/> (where a blot of <paramref name="me"/> stands): directly, by a combination over open
        /// points, or from the bar once the checkers there are in.
        /// </summary>
        public static bool CanHit(int[] me, int[] them, int target, int a, int b)
        {
            int bar = them[BackgammonPlayGenerator.BarPoint];
            if (a != b)
            {
                if (bar == 0)
                {
                    return Chain(me, them, target, a, 1) || Chain(me, them, target, b, 1) || Combination(me, them, target, a, b);
                }
                bool inA = Open(me, 25 - a);
                bool inB = Open(me, 25 - b);
                if (bar == 1)
                {
                    // One die brings the checker in, the other is free (for the same checker or any other).
                    if (inA && (25 - a == target || 25 - a - b == target || Chain(me, them, target, b, 1)))
                    {
                        return true;
                    }
                    return inB && (25 - b == target || 25 - b - a == target || Chain(me, them, target, a, 1));
                }
                return (inA && 25 - a == target) || (inB && 25 - b == target);
            }
            if (bar > 0)
            {
                if (!Open(me, 25 - a))
                {
                    return false;
                }
                if (25 - a == target)
                {
                    return true;
                }
                int free = bar >= 4 ? 0 : 4 - bar;
                int point = 25 - a;
                for (int n = 1; n <= free; n++)
                {
                    point -= a;
                    if (point == target)
                    {
                        return true;
                    }
                    if (point < target || !Open(me, point))
                    {
                        break;
                    }
                }
                return free > 0 && Chain(me, them, target, a, free);
            }
            return Chain(me, them, target, a, 4);
        }

        /// <summary>A checker of <paramref name="them"/> that reaches the target in at most <paramref name="steps"/> steps of <paramref name="die"/>.</summary>
        private static bool Chain(int[] me, int[] them, int target, int die, int steps)
        {
            int point = target;
            for (int n = 1; n <= steps; n++)
            {
                point += die;
                if (point > 24)
                {
                    return false;
                }
                if (them[point] > 0)
                {
                    return true;
                }
                if (!Open(me, point))
                {
                    return false;
                }
            }
            return false;
        }

        private static bool Combination(int[] me, int[] them, int target, int a, int b)
        {
            int from = target + a + b;
            return from <= 24 && them[from] > 0 && (Open(me, target + a) || Open(me, target + b));
        }

        /// <summary>Whether <paramref name="point"/> in the hitter's numbers is free of a point made by <paramref name="me"/>.</summary>
        private static bool Open(int[] me, int point)
        {
            return me[25 - point] < 2;
        }
    }
}
