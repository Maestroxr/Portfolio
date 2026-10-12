using System;
using Gamebox.Lockstep;

namespace Portfolio.BoardGames
{
    /// <summary>
    /// The computer's backgammon: every legal play of the dice weighed by a position evaluation (deeper at higher levels),
    /// and the doubling cube offered, taken or dropped by an estimate of the chances. Deterministic for a given game,
    /// level and random numbers.
    /// <list type="bullet">
    /// <item>Easy: a beginner. It likes hitting, making points and bearing off, minds its blots only some of the time, and
    /// adds a little chance to every play; it never doubles and takes unless the game is hopeless.</item>
    /// <item>Normal: the full evaluation of <see cref="BackgammonEvaluation"/> on every play; it doubles and takes by the
    /// chances it reads from the position (the Keith count in a race).</item>
    /// <item>Hard: Normal's evaluation, then for the best few plays every roll of the opponent and the opponent's best
    /// answer to it, averaged; its chances before the cube look one roll ahead.</item>
    /// </list>
    /// </summary>
    public static class BackgammonAI
    {
        public const int Levels = 3;

        /// <summary>How many of the best plays by the evaluation Hard looks at two plies deep.</summary>
        public const int HardCandidates = 6;

        /// <summary>
        /// The command for <paramref name="seat"/>, the seat the game waits for: Roll (or Double first when worth it and
        /// allowed), the play of the dice (Move), or Take / Drop for a cube offered. Null when the game waits for nothing.
        /// </summary>
        public static BoardCommand Choose(BackgammonGame game, int seat, int level, IRandom random)
        {
            if (game == null || game.IsOver)
            {
                return null;
            }
            switch (game.Phase)
            {
                case BackgammonPhase.Opening:
                    return BoardCommand.Roll(seat);
                case BackgammonPhase.Rolling:
                    return ShouldDouble(game, level) ? BoardCommand.Double(seat) : BoardCommand.Roll(seat);
                case BackgammonPhase.Moving:
                {
                    BackgammonPlay play = ChoosePlay(game, level, random);
                    return play != null ? BoardCommand.Play(seat, play.Path()) : null;
                }
                case BackgammonPhase.Answering:
                    return ShouldTake(game, level) ? BoardCommand.Take(seat) : BoardCommand.Drop(seat);
                default:
                    return null;
            }
        }

        /// <summary>The play the computer picks for the dice of the turn (the moving phase); never null in that phase.</summary>
        public static BackgammonPlay ChoosePlay(BackgammonGame game, int level, IRandom random)
        {
            if (game == null || game.IsOver || game.Phase != BackgammonPhase.Moving)
            {
                return null;
            }
            Side side = game.SideToAct;
            var thinker = new Thinker();
            game.LoadSides(side, thinker.Mine, thinker.Theirs);
            BackgammonPlayGenerator plays = thinker.Plays;
            plays.Generate(thinker.Mine, thinker.Theirs, game.die1, game.die2);
            int pick;
            if (plays.Count <= 1)
            {
                pick = 0;
            }
            else if (level <= 0)
            {
                pick = thinker.Easy(random);
            }
            else if (level == 1)
            {
                pick = thinker.Best();
            }
            else
            {
                pick = thinker.LookAhead();
            }
            return BackgammonGame.PlayOf(plays, pick, side);
        }

        /// <summary>
        /// Whether the computer at <paramref name="level"/> offers the cube now (only when the side to act may): with the
        /// chances between its double point and too good (a gammon in sight), or past it when no gammon is. Easy never doubles.
        /// </summary>
        public static bool ShouldDouble(BackgammonGame game, int level)
        {
            if (level <= 0 || game == null || !game.CanDouble(game.SideToAct))
            {
                return false;
            }
            Side side = game.SideToAct;
            var thinker = new Thinker();
            game.LoadSides(side, thinker.Mine, thinker.Theirs);
            bool owned = game.cubeOwner == (int)side;
            bool gammon = BackgammonEvaluation.GammonChance(thinker.Mine, thinker.Theirs);
            if (!BackgammonEvaluation.HasContact(thinker.Mine, thinker.Theirs))
            {
                // The Keith count: double within 4 (redouble within 3); far ahead it is still a double, as long as
                // the other side is not likely to lose a gammon by playing on.
                int chance = BackgammonEvaluation.RacePermille(thinker.Mine, thinker.Theirs);
                return chance >= (owned ? 725 : 680) && !gammon;
            }
            int win = level >= 2 ? thinker.WinBeforeRoll() : BackgammonEvaluation.WinPermille(thinker.Mine, thinker.Theirs);
            int low = level >= 2 ? (owned ? 700 : 680) : (owned ? 720 : 700);
            int high = level >= 2 ? 850 : 900;
            if (win < low)
            {
                return false;
            }
            return win <= high || !gammon;
        }

        /// <summary>
        /// Whether the computer at <paramref name="level"/> takes the cube offered to the side to act: when its chances are
        /// at least a quarter for Hard (22 percent for Normal, the Keith take point in a race), or not hopeless for Easy.
        /// </summary>
        public static bool ShouldTake(BackgammonGame game, int level)
        {
            if (game == null || game.IsOver || game.Phase != BackgammonPhase.Answering)
            {
                return false;
            }
            int doublerChance = WinChance(game, Sides.Other(game.SideToAct), level);
            int takerChance = 1000 - doublerChance;
            var thinker = new Thinker();
            game.LoadSides(game.SideToAct, thinker.Mine, thinker.Theirs);
            int need;
            if (level <= 0)
            {
                need = 120;
            }
            else if (!BackgammonEvaluation.HasContact(thinker.Mine, thinker.Theirs))
            {
                need = 240;
            }
            else
            {
                need = level == 1 ? 220 : 250;
            }
            return takerChance >= need;
        }

        /// <summary>
        /// The chances, in thousandths, that <paramref name="roller"/> wins when it is about to roll: Easy and Normal read
        /// them from the position, Hard averages its best play over the 36 rolls.
        /// </summary>
        public static int WinChance(BackgammonGame game, Side roller, int level)
        {
            if (game == null)
            {
                return 500;
            }
            if (game.IsOver)
            {
                return game.Winner == roller ? 1000 : game.Winner.HasValue ? 0 : 500;
            }
            var thinker = new Thinker();
            game.LoadSides(roller, thinker.Mine, thinker.Theirs);
            if (level >= 2 && BackgammonEvaluation.HasContact(thinker.Mine, thinker.Theirs))
            {
                return thinker.WinBeforeRoll();
            }
            return BackgammonEvaluation.WinPermille(thinker.Mine, thinker.Theirs);
        }

        /// <summary>
        /// The arrays and generators one decision thinks with: the position of the side to act (its own numbers), the
        /// plays of its roll, and the opponent's plays of every reply roll. Nothing in the loops allocates.
        /// </summary>
        private sealed class Thinker
        {
            public readonly int[] Mine = new int[BackgammonPlayGenerator.Slots];
            public readonly int[] Theirs = new int[BackgammonPlayGenerator.Slots];
            public readonly BackgammonPlayGenerator Plays = new BackgammonPlayGenerator();
            private readonly BackgammonPlayGenerator replies = new BackgammonPlayGenerator();
            private readonly int[] afterMine = new int[BackgammonPlayGenerator.Slots];
            private readonly int[] afterTheirs = new int[BackgammonPlayGenerator.Slots];
            private readonly int[] replyMine = new int[BackgammonPlayGenerator.Slots];
            private readonly int[] replyTheirs = new int[BackgammonPlayGenerator.Slots];

            /// <summary>The beginner's pick: hits, points and checkers off, blots minded only some of the time, some chance.</summary>
            public int Easy(IRandom random)
            {
                bool mindBlots = random == null || random.Range(0, 100) >= 35;
                int barBefore = Theirs[BackgammonPlayGenerator.BarPoint];
                int best = 0;
                int bestScore = int.MinValue;
                for (int i = 0; i < Plays.Count; i++)
                {
                    After(i);
                    int score = (afterTheirs[BackgammonPlayGenerator.BarPoint] - barBefore) * 60 + afterMine[0] * 25;
                    for (int p = 1; p <= 24; p++)
                    {
                        if (afterMine[p] >= 2)
                        {
                            score += p <= 6 ? 18 : 10;
                        }
                        else if (afterMine[p] == 1 && mindBlots)
                        {
                            score -= 12;
                        }
                    }
                    if (random != null)
                    {
                        score += random.Range(0, 30);
                    }
                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = i;
                    }
                }
                return best;
            }

            /// <summary>The play with the best evaluation (the first of equals).</summary>
            public int Best()
            {
                int best = 0;
                int bestScore = int.MinValue;
                for (int i = 0; i < Plays.Count; i++)
                {
                    After(i);
                    int score = BackgammonEvaluation.Evaluate(afterMine, afterTheirs);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = i;
                    }
                }
                return best;
            }

            /// <summary>
            /// The best few plays by the evaluation, each judged by the opponent's best answer to every roll, averaged
            /// over the 36; the first of equals by the evaluation wins a tie.
            /// </summary>
            public int LookAhead()
            {
                int count = Plays.Count;
                int keep = count < HardCandidates ? count : HardCandidates;
                var candidates = new int[keep];
                var scores = new int[keep];
                int found = 0;
                for (int i = 0; i < count; i++)
                {
                    After(i);
                    int score = BackgammonEvaluation.Evaluate(afterMine, afterTheirs);
                    // Insertion into the short list, best first, earlier plays ahead of equal later ones.
                    int at = found;
                    while (at > 0 && scores[at - 1] < score)
                    {
                        at--;
                    }
                    if (at >= keep)
                    {
                        continue;
                    }
                    int last = found < keep ? found : keep - 1;
                    for (int j = last; j > at; j--)
                    {
                        candidates[j] = candidates[j - 1];
                        scores[j] = scores[j - 1];
                    }
                    candidates[at] = i;
                    scores[at] = score;
                    if (found < keep)
                    {
                        found++;
                    }
                }
                int best = candidates[0];
                long bestValue = long.MinValue;
                for (int c = 0; c < found; c++)
                {
                    After(candidates[c]);
                    long value;
                    if (afterMine[0] >= BackgammonGame.CheckersPerSide)
                    {
                        value = (long)BackgammonEvaluation.WonScore * BackgammonEvaluation.Multiplier(afterTheirs) * 36;
                    }
                    else
                    {
                        value = -ReplyTotal(afterTheirs, afterMine);
                    }
                    if (value > bestValue)
                    {
                        bestValue = value;
                        best = candidates[c];
                    }
                }
                return best;
            }

            /// <summary>
            /// The roller's chances before its roll, one roll ahead: for each of the 36 rolls the chances after its best
            /// play, averaged.
            /// </summary>
            public int WinBeforeRoll()
            {
                long total = 0;
                for (int r = 0; r < 21; r++)
                {
                    Plays.Generate(Mine, Theirs, BackgammonEvaluation.RollA[r], BackgammonEvaluation.RollB[r]);
                    int best = 0;
                    for (int i = 0; i < Plays.Count; i++)
                    {
                        After(i);
                        int score = BackgammonEvaluation.Evaluate(afterMine, afterTheirs);
                        int chance = BackgammonEvaluation.WinPermilleAfter(afterMine, afterTheirs, score);
                        if (i == 0 || chance > best)
                        {
                            best = chance;
                        }
                    }
                    total += BackgammonEvaluation.RollWeight[r] * best;
                }
                return (int)(total / 36);
            }

            /// <summary>
            /// The opponent to roll in a position (<paramref name="roller"/>'s checkers against <paramref name="other"/>'s):
            /// the sum over the 36 rolls of the evaluation of its best answer, for the opponent.
            /// </summary>
            private long ReplyTotal(int[] roller, int[] other)
            {
                long total = 0;
                for (int r = 0; r < 21; r++)
                {
                    replies.Generate(roller, other, BackgammonEvaluation.RollA[r], BackgammonEvaluation.RollB[r]);
                    int best = int.MinValue;
                    for (int j = 0; j < replies.Count; j++)
                    {
                        Array.Copy(roller, replyMine, BackgammonPlayGenerator.Slots);
                        Array.Copy(other, replyTheirs, BackgammonPlayGenerator.Slots);
                        replies.Apply(j, replyMine, replyTheirs);
                        int score = BackgammonEvaluation.Evaluate(replyMine, replyTheirs);
                        if (score > best)
                        {
                            best = score;
                        }
                    }
                    total += (long)BackgammonEvaluation.RollWeight[r] * best;
                }
                return total;
            }

            /// <summary>The position after play <paramref name="index"/> of <see cref="Plays"/>, into the after arrays.</summary>
            private void After(int index)
            {
                Array.Copy(Mine, afterMine, BackgammonPlayGenerator.Slots);
                Array.Copy(Theirs, afterTheirs, BackgammonPlayGenerator.Slots);
                Plays.Apply(index, afterMine, afterTheirs);
            }
        }
    }
}
