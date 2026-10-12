using System;
using System.Collections.Generic;
using Gamebox.Lockstep;

namespace Portfolio.BoardGames
{
    /// <summary>What a backgammon game waits for.</summary>
    public enum BackgammonPhase
    {
        /// <summary>The opening roll: each side throws one die (the side to act sends the roll), the higher plays both.</summary>
        Opening = 0,
        /// <summary>The side to act rolls, or offers the doubling cube first.</summary>
        Rolling = 1,
        /// <summary>The dice are thrown: the side to act plays them.</summary>
        Moving = 2,
        /// <summary>The cube was offered: the side to act takes it or drops it.</summary>
        Answering = 3,
        Over = 4
    }

    /// <summary>
    /// One game of backgammon. <see cref="board"/> holds the checkers of the 24 points, positive White's (Light), negative
    /// Black's (Dark). White moves from point 23 down to point 0 and bears off below it (White's home board is 0 to 5,
    /// White's n-point is index n - 1, it enters from the bar on 24 - die); Black moves up from 0 and bears off above 23
    /// (home board 18 to 23, Black's n-point is index 24 - n, it enters on die - 1). A turn is a roll and a play; the play
    /// is one command with every checker moved, legal only when it uses as many dice as the position allows (both, or
    /// the larger when only one can be played; four of a double when possible). When a roll allows nothing the turn
    /// passes at once. The cube may be offered before a roll by the side that owns it (or either while it is in the
    /// middle); the other side takes (the game goes on at twice the stake, the taker owns the cube) or drops (the doubler
    /// wins the stake as it was). A finished game is worth the cube times 1, 2 for a gammon (the loser bore off no checker)
    /// or 3 for a backgammon (also a checker of the loser on the bar or in the winner's home board).
    /// </summary>
    [Serializable]
    public sealed class BackgammonGame : IBoardGame
    {
        public const int PointCount = 24;
        /// <summary>The bar, as the point a move starts from.</summary>
        public const int Bar = 24;
        /// <summary>Off the board, as the point a move ends on.</summary>
        public const int Off = 25;
        public const int CheckersPerSide = 15;
        public const int MaxCube = 64;
        /// <summary>The pips of fifteen checkers on the bar: the most a side can have to travel.</summary>
        public const int MaxPips = 375;

        public int[] board = new int[PointCount];
        /// <summary>Checkers on the bar, Light's then Dark's.</summary>
        public int[] bar = new int[2];
        /// <summary>Checkers borne off, Light's then Dark's.</summary>
        public int[] off = new int[2];
        public int sideToAct;
        public int phase;
        /// <summary>The dice of the turn (0 before the first roll); after the opening roll die1 was Light's and die2 Dark's.</summary>
        public int die1;
        public int die2;
        /// <summary>Whether the doubling cube is in play.</summary>
        public bool cube;
        public int cubeValue = 1;
        /// <summary>-1 while the cube is in the middle, else the side that owns it.</summary>
        public int cubeOwner = -1;
        public int turn;
        public int result;
        public int endReason;
        /// <summary>What the finished game is worth to the winner (the cube times 1, 2 or 3); 0 while it is on.</summary>
        public int score;
        /// <summary>The checkers of the last play, <c>{ from, to, ... }</c> (for the board to show), and who played it (-1 none).</summary>
        public int[] lastPlay = new int[0];
        public int lastSide = -1;

        [NonSerialized]
        private List<BackgammonPlay> legal;
        [NonSerialized]
        private uint legalSum;
        [NonSerialized]
        private int legalDice;
        [NonSerialized]
        private int legalRequiredDie;

        /// <summary>For the serializers; the fields are set by them.</summary>
        public BackgammonGame()
        {
        }

        /// <summary>
        /// The starting position. With <paramref name="openingRoll"/> the game begins with the opening roll; without it
        /// <paramref name="firstMover"/> starts with an ordinary roll.
        /// </summary>
        public static BackgammonGame NewGame(bool cube, bool openingRoll, Side firstMover)
        {
            var game = new BackgammonGame
            {
                cube = cube,
                sideToAct = (int)firstMover,
                phase = (int)(openingRoll ? BackgammonPhase.Opening : BackgammonPhase.Rolling)
            };
            foreach (Side side in new[] { Side.Light, Side.Dark })
            {
                int sign = Sign(side);
                game.board[PointIndex(side, 24)] = 2 * sign;
                game.board[PointIndex(side, 13)] = 5 * sign;
                game.board[PointIndex(side, 8)] = 3 * sign;
                game.board[PointIndex(side, 6)] = 5 * sign;
            }
            return game;
        }

        public RuleSet RuleSet => RuleSet.Backgammon;

        public Side SideToAct => sideToAct == (int)Side.Dark ? Side.Dark : Side.Light;

        public BackgammonPhase Phase => (BackgammonPhase)phase;

        public GameResult Result => (GameResult)result;

        public EndReason Reason => (EndReason)endReason;

        public bool IsOver => result != (int)GameResult.Playing;

        public Side? Winner => Result == GameResult.LightWins ? Side.Light : Result == GameResult.DarkWins ? (Side?)Side.Dark : null;

        public int Turn => turn;

        public int Points => score;

        /// <summary>
        /// What the finished game was worth before the cube: 1 a single game (also a dropped cube or a game called off),
        /// 2 a gammon, 3 a backgammon; 0 while the game is on and on a draw.
        /// </summary>
        public int ResultMultiplier => IsOver && score > 0 ? score / (cubeValue > 0 ? cubeValue : 1) : 0;

        /// <summary>Whether the dice of the turn are a double.</summary>
        public bool IsDouble => die1 != 0 && die1 == die2;

        /// <summary>The index on <see cref="board"/> of <paramref name="side"/>'s <paramref name="ownPoint"/>-point (1 to 24).</summary>
        public static int PointIndex(Side side, int ownPoint)
        {
            return side == Side.Light ? ownPoint - 1 : PointCount - ownPoint;
        }

        /// <summary>
        /// The point number <paramref name="side"/> calls a place by: 1 to 24 for the indices 0 to 23, 25 for the
        /// <see cref="Bar"/> and 0 for <see cref="Off"/> (the pips a checker there still has to travel).
        /// </summary>
        public static int OwnPoint(Side side, int index)
        {
            if (index == Bar)
            {
                return BackgammonPlayGenerator.BarPoint;
            }
            if (index < 0 || index >= PointCount)
            {
                return BackgammonPlayGenerator.OffPoint;
            }
            return side == Side.Light ? index + 1 : PointCount - index;
        }

        /// <summary>The place of <paramref name="side"/>'s own point number (0 off, 25 the bar) as a move names it.</summary>
        private static int PlaceOf(Side side, int ownPoint)
        {
            if (ownPoint >= BackgammonPlayGenerator.BarPoint)
            {
                return Bar;
            }
            return ownPoint <= 0 ? Off : PointIndex(side, ownPoint);
        }

        private static int Sign(Side side)
        {
            return side == Side.Light ? 1 : -1;
        }

        /// <summary>The dice the turn's play may use: two numbers, or four of a double; empty outside <see cref="BackgammonPhase.Moving"/>.</summary>
        public int[] DiceToPlay()
        {
            if (Phase != BackgammonPhase.Moving || IsOver)
            {
                return new int[0];
            }
            return IsDouble ? new[] { die1, die1, die1, die1 } : new[] { die1, die2 };
        }

        /// <summary>The dice of the turn still unused after <paramref name="done"/> (each move gives up the die it names).</summary>
        public int[] DiceLeft(IReadOnlyList<CheckerMove> done)
        {
            var left = new List<int>(DiceToPlay());
            if (done != null)
            {
                for (int i = 0; i < done.Count; i++)
                {
                    left.Remove(done[i].die);
                }
            }
            return left.ToArray();
        }

        /// <summary>Checkers of <paramref name="side"/> on <paramref name="point"/> (0 to 23).</summary>
        public int CheckersAt(int point, Side side)
        {
            if (point < 0 || point >= PointCount)
            {
                return 0;
            }
            int n = board[point] * Sign(side);
            return n > 0 ? n : 0;
        }

        /// <summary>The pips <paramref name="side"/> still has to travel: every checker's distance to off, 25 from the bar.</summary>
        public int PipCount(Side side)
        {
            int pips = bar[(int)side] * 25;
            for (int p = 1; p <= PointCount; p++)
            {
                pips += CheckersAt(PointIndex(side, p), side) * p;
            }
            return pips;
        }

        /// <summary>Whether <paramref name="side"/> may offer the cube now: its turn to roll, the cube in play, in the middle or its own, below 64.</summary>
        public bool CanDouble(Side side)
        {
            return !IsOver && cube && Phase == BackgammonPhase.Rolling && SideToAct == side
                && (cubeOwner < 0 || cubeOwner == (int)side) && cubeValue < MaxCube;
        }

        /// <summary>
        /// Throws the dice: in <see cref="BackgammonPhase.Opening"/> <paramref name="a"/> is Light's die and
        /// <paramref name="b"/> Dark's (they must differ; the higher side plays both), in <see cref="BackgammonPhase.Rolling"/>
        /// both are the side to act's. When the roll allows no move the turn passes at once. False when it is not the time.
        /// </summary>
        public bool Roll(int a, int b)
        {
            if (IsOver || a < 1 || a > 6 || b < 1 || b > 6)
            {
                return false;
            }
            switch (Phase)
            {
                case BackgammonPhase.Opening:
                    if (a == b)
                    {
                        return false;
                    }
                    sideToAct = (int)(a > b ? Side.Light : Side.Dark);
                    break;
                case BackgammonPhase.Rolling:
                    break;
                default:
                    return false;
            }
            die1 = a;
            die2 = b;
            phase = (int)BackgammonPhase.Moving;
            turn++;
            legal = null;
            IReadOnlyList<BackgammonPlay> plays = LegalPlays();
            if (plays.Count == 1 && plays[0].Count == 0)
            {
                // Nothing can be played: the turn passes at once.
                lastPlay = new int[0];
                lastSide = sideToAct;
                EndTurn();
            }
            return true;
        }

        /// <summary>Every distinct whole play the dice allow now (one empty play when there is none); in the moving phase only.</summary>
        public IReadOnlyList<BackgammonPlay> LegalPlays()
        {
            if (Phase != BackgammonPhase.Moving || IsOver)
            {
                return new List<BackgammonPlay>();
            }
            uint sum = Checksum();
            if (legal != null && legalSum == sum)
            {
                return legal;
            }
            Side side = SideToAct;
            var mine = new int[BackgammonPlayGenerator.Slots];
            var theirs = new int[BackgammonPlayGenerator.Slots];
            LoadSides(side, mine, theirs);
            var generator = new BackgammonPlayGenerator();
            generator.Generate(mine, theirs, die1, die2);
            var plays = new List<BackgammonPlay>(generator.Count);
            for (int i = 0; i < generator.Count; i++)
            {
                plays.Add(PlayOf(generator, i, side));
            }
            legal = plays;
            legalSum = sum;
            legalDice = generator.DiceUsed;
            legalRequiredDie = !IsDouble && generator.DiceUsed == 1 ? generator.Die(0, 0) : 0;
            return legal;
        }

        /// <summary>A play of <paramref name="generator"/>'s list as the board names its moves.</summary>
        internal static BackgammonPlay PlayOf(BackgammonPlayGenerator generator, int index, Side side)
        {
            int n = generator.MoveCount(index);
            var play = new BackgammonPlay { moves = new CheckerMove[n] };
            for (int i = 0; i < n; i++)
            {
                play.moves[i] = new CheckerMove(PlaceOf(side, generator.From(index, i)), PlaceOf(side, generator.To(index, i)), generator.Die(index, i));
            }
            return play;
        }

        /// <summary>
        /// The single moves that may follow <paramref name="done"/> (checkers already moved this turn, in order) so that
        /// the play can still become legal; empty when <paramref name="done"/> is a whole play or no legal play starts so.
        /// A checker that can bear off with either die is offered once, with the smallest die that keeps the play legal.
        /// </summary>
        public IReadOnlyList<CheckerMove> NextMoves(IReadOnlyList<CheckerMove> done)
        {
            var next = new List<CheckerMove>();
            if (Phase != BackgammonPhase.Moving || IsOver)
            {
                return next;
            }
            Steps steps = NewSteps();
            int count = done != null ? done.Count : 0;
            if (count < steps.MaxDice)
            {
                steps.Walk(done ?? new CheckerMove[0], 0, 0, next);
            }
            return next;
        }

        /// <summary>Whether <paramref name="done"/> is a whole legal play.</summary>
        public bool IsCompletePlay(IReadOnlyList<CheckerMove> done)
        {
            if (Phase != BackgammonPhase.Moving || IsOver)
            {
                return false;
            }
            Steps steps = NewSteps();
            int count = done != null ? done.Count : 0;
            return count == steps.MaxDice && steps.Walk(done ?? new CheckerMove[0], 0, 0, null);
        }

        /// <summary>
        /// The legal play of a command's numbers <c>{ from, to, ... }</c> (in an order that can be played), or null. The
        /// order given is tried first, then the others; the dice each move uses are worked out.
        /// </summary>
        public BackgammonPlay FindPlay(IReadOnlyList<int> path)
        {
            if (Phase != BackgammonPhase.Moving || IsOver || path == null || path.Count % 2 != 0 || path.Count > 8)
            {
                return null;
            }
            int n = path.Count / 2;
            var moves = new CheckerMove[n];
            for (int i = 0; i < n; i++)
            {
                int from = path[i * 2];
                int to = path[i * 2 + 1];
                if (from < 0 || from > Bar || to < 0 || to > Off || to == Bar)
                {
                    return null;
                }
                moves[i] = new CheckerMove(from, to, 0);
            }
            Steps steps = NewSteps();
            if (n != steps.MaxDice)
            {
                return null;
            }
            if (n == 0)
            {
                return new BackgammonPlay();
            }
            var order = new int[n];
            for (int i = 0; i < n; i++)
            {
                order[i] = i;
            }
            var chosen = new CheckerMove[n];
            do
            {
                if (steps.Assign(moves, order, 0, 0, chosen))
                {
                    return new BackgammonPlay(chosen);
                }
            }
            while (NextPermutation(order));
            return null;
        }

        /// <summary>Plays a whole play given by a command's numbers; false and nothing changes when it is not legal now.</summary>
        public bool Play(IReadOnlyList<int> path)
        {
            BackgammonPlay play = FindPlay(path);
            if (play == null)
            {
                return false;
            }
            Side side = SideToAct;
            for (int i = 0; i < play.Count; i++)
            {
                MoveChecker(side, play.moves[i].from, play.moves[i].to);
            }
            lastPlay = play.Path();
            lastSide = (int)side;
            turn++;
            legal = null;
            if (off[(int)side] >= CheckersPerSide)
            {
                Finish(side, EndReason.BorneOff, cubeValue * Multiplier(Sides.Other(side)));
            }
            else
            {
                EndTurn();
            }
            return true;
        }

        /// <summary><paramref name="side"/> offers the cube (see <see cref="CanDouble"/>); the other side then answers.</summary>
        public bool Double(Side side)
        {
            if (!CanDouble(side))
            {
                return false;
            }
            phase = (int)BackgammonPhase.Answering;
            sideToAct = (int)Sides.Other(side);
            turn++;
            return true;
        }

        /// <summary><paramref name="side"/> takes the cube offered: the stake doubles, the cube is theirs, the doubler rolls.</summary>
        public bool Take(Side side)
        {
            if (IsOver || Phase != BackgammonPhase.Answering || SideToAct != side)
            {
                return false;
            }
            cubeValue *= 2;
            cubeOwner = (int)side;
            sideToAct = (int)Sides.Other(side);
            phase = (int)BackgammonPhase.Rolling;
            turn++;
            return true;
        }

        /// <summary><paramref name="side"/> refuses the cube: the doubler wins the stake as it was.</summary>
        public bool Drop(Side side)
        {
            if (IsOver || Phase != BackgammonPhase.Answering || SideToAct != side)
            {
                return false;
            }
            turn++;
            Finish(Sides.Other(side), EndReason.DoubleDeclined, cubeValue);
            return true;
        }

        /// <summary>
        /// <paramref name="side"/> gives up: the other side wins <see cref="ResignValue"/> (a single game, or a gammon or
        /// backgammon once the other side bears off and the resigner bore off nothing), times the cube.
        /// </summary>
        public bool Resign(Side side)
        {
            if (IsOver)
            {
                return false;
            }
            int points = ResignValue(side);
            turn++;
            Finish(Sides.Other(side), EndReason.Resigned, points);
            return true;
        }

        /// <summary>
        /// What <paramref name="side"/> would give the other side by resigning now: the cube times 1; times 2 or 3 (what a
        /// gammon or a backgammon would be worth) once every checker of the other side is home, so it bears off, while
        /// <paramref name="side"/> bore off none. Earlier the gammon is only a threat, and giving up costs a single game.
        /// </summary>
        public int ResignValue(Side side)
        {
            return cubeValue * (AllHome(Sides.Other(side)) ? Multiplier(side) : 1);
        }

        /// <summary>Whether every checker of <paramref name="side"/> still on the board is in its home board (none on the bar).</summary>
        private bool AllHome(Side side)
        {
            if (bar[(int)side] > 0)
            {
                return false;
            }
            for (int point = 7; point <= PointCount; point++)
            {
                if (CheckersAt(PointIndex(side, point), side) > 0)
                {
                    return false;
                }
            }
            return true;
        }

        public void CallOff(Side? winner)
        {
            if (winner.HasValue)
            {
                Finish(winner.Value, EndReason.CalledOff, cubeValue);
            }
            else
            {
                result = (int)GameResult.Draw;
                endReason = (int)EndReason.CalledOff;
                score = 0;
                phase = (int)BackgammonPhase.Over;
                legal = null;
            }
        }

        /// <summary>A copy with <paramref name="moves"/> made on the board (no dice used, no turn change): a play in progress as the board shows it.</summary>
        public BackgammonGame Preview(IReadOnlyList<CheckerMove> moves)
        {
            BackgammonGame copy = Clone();
            if (moves == null)
            {
                return copy;
            }
            Side side = SideToAct;
            for (int i = 0; i < moves.Count; i++)
            {
                if (copy.CanShow(side, moves[i].from, moves[i].to))
                {
                    copy.MoveChecker(side, moves[i].from, moves[i].to);
                }
            }
            return copy;
        }

        public BackgammonGame Clone()
        {
            return new BackgammonGame
            {
                board = (int[])board.Clone(),
                bar = (int[])bar.Clone(),
                off = (int[])off.Clone(),
                sideToAct = sideToAct,
                phase = phase,
                die1 = die1,
                die2 = die2,
                cube = cube,
                cubeValue = cubeValue,
                cubeOwner = cubeOwner,
                turn = turn,
                result = result,
                endReason = endReason,
                score = score,
                lastPlay = lastPlay != null ? (int[])lastPlay.Clone() : new int[0],
                lastSide = lastSide
            };
        }

        public IBoardGame CloneGame()
        {
            return Clone();
        }

        /// <summary>Everything the rules look at, folded: the checkers, whose decision it is, the dice, the cube, the result, the last play.</summary>
        public uint Checksum()
        {
            var sum = new StateChecksum();
            sum.Add(board);
            sum.Add(bar);
            sum.Add(off);
            sum.Add(sideToAct).Add(phase).Add(die1).Add(die2);
            sum.Add(cube).Add(cubeValue).Add(cubeOwner);
            sum.Add(turn).Add(result).Add(endReason).Add(score);
            sum.Add(lastPlay ?? new int[0]).Add(lastSide);
            return sum.Value;
        }

        /// <summary>Higher is better: checkers borne off and pips saved (for rankings and the room's early end).</summary>
        public int Material(Side side)
        {
            return off[(int)side] * 10 + MaxPips - PipCount(side);
        }

        /// <summary>The direction <paramref name="side"/> moves along the indices: -1 for Light, +1 for Dark.</summary>
        public static int Direction(Side side)
        {
            return side == Side.Light ? -1 : 1;
        }

        /// <summary>
        /// The checkers of <paramref name="side"/> into <paramref name="mine"/> and of the other side into
        /// <paramref name="theirs"/>, each in its own numbers (26 counts: 0 off, 1 to 24 the points, 25 the bar).
        /// </summary>
        internal void LoadSides(Side side, int[] mine, int[] theirs)
        {
            Side other = Sides.Other(side);
            for (int p = 1; p <= PointCount; p++)
            {
                mine[p] = CheckersAt(PointIndex(side, p), side);
                theirs[p] = CheckersAt(PointIndex(other, p), other);
            }
            mine[BackgammonPlayGenerator.BarPoint] = bar[(int)side];
            mine[0] = off[(int)side];
            theirs[BackgammonPlayGenerator.BarPoint] = bar[(int)other];
            theirs[0] = off[(int)other];
        }

        /// <summary>What a game won now by the other side would be worth from <paramref name="loser"/>'s checkers: 1, 2 or 3.</summary>
        private int Multiplier(Side loser)
        {
            if (off[(int)loser] > 0)
            {
                return 1;
            }
            if (bar[(int)loser] > 0)
            {
                return 3;
            }
            Side winner = Sides.Other(loser);
            for (int p = 1; p <= 6; p++)
            {
                if (CheckersAt(PointIndex(winner, p), loser) > 0)
                {
                    return 3;
                }
            }
            return 2;
        }

        private void Finish(Side winner, EndReason reason, int points)
        {
            result = (int)(winner == Side.Light ? GameResult.LightWins : GameResult.DarkWins);
            endReason = (int)reason;
            score = points;
            phase = (int)BackgammonPhase.Over;
            legal = null;
        }

        private void EndTurn()
        {
            sideToAct = (int)Sides.Other(SideToAct);
            phase = (int)BackgammonPhase.Rolling;
            legal = null;
        }

        /// <summary>Moves one checker of <paramref name="side"/>, sending a lone opposing checker on the point it reaches to the bar.</summary>
        private void MoveChecker(Side side, int from, int to)
        {
            int s = (int)side;
            int sign = Sign(side);
            if (from == Bar)
            {
                bar[s]--;
            }
            else
            {
                board[from] -= sign;
            }
            if (to == Off)
            {
                off[s]++;
                return;
            }
            if (board[to] == -sign)
            {
                board[to] = 0;
                bar[1 - s]++;
            }
            board[to] += sign;
        }

        /// <summary>Whether a move can be shown on the board: a checker of the side where it starts, a point not held by the other side.</summary>
        private bool CanShow(Side side, int from, int to)
        {
            bool hasChecker = from == Bar ? bar[(int)side] > 0 : CheckersAt(from, side) > 0;
            if (!hasChecker || to == Bar || to < 0 || to > Off)
            {
                return false;
            }
            return to == Off || CheckersAt(to, Sides.Other(side)) < 2;
        }

        private Steps NewSteps()
        {
            LegalPlays();
            var steps = new Steps(SideToAct, legalDice, legalRequiredDie);
            LoadSides(SideToAct, steps.Mine, steps.Theirs);
            int[] dice = DiceToPlay();
            for (int i = 0; i < dice.Length; i++)
            {
                steps.DiceLeft[dice[i]]++;
            }
            return steps;
        }

        private static bool NextPermutation(int[] order)
        {
            int i = order.Length - 2;
            while (i >= 0 && order[i] >= order[i + 1])
            {
                i--;
            }
            if (i < 0)
            {
                return false;
            }
            int j = order.Length - 1;
            while (order[j] <= order[i])
            {
                j--;
            }
            (order[i], order[j]) = (order[j], order[i]);
            Array.Reverse(order, i + 1, order.Length - i - 1);
            return true;
        }

        /// <summary>
        /// A play put together one checker at a time, for the step by step input: the moves so far checked against the
        /// dice (any die that fits), and the moves that can still lead to a legal play (as many dice as the roll allows,
        /// the larger die when only one can be played).
        /// </summary>
        private sealed class Steps
        {
            public readonly int[] Mine = new int[BackgammonPlayGenerator.Slots];
            public readonly int[] Theirs = new int[BackgammonPlayGenerator.Slots];
            /// <summary>Unused dice by their number (1 to 6).</summary>
            public readonly int[] DiceLeft = new int[7];
            public readonly int MaxDice;
            private readonly Side side;
            private readonly int requiredDie;

            public Steps(Side side, int maxDice, int requiredDie)
            {
                this.side = side;
                MaxDice = maxDice;
                this.requiredDie = requiredDie;
            }

            /// <summary>
            /// Follows <paramref name="done"/> from move <paramref name="index"/> on, every die that fits; at its end adds
            /// the moves that can follow to <paramref name="next"/>, or (with no list) tells whether the play is whole.
            /// </summary>
            public bool Walk(IReadOnlyList<CheckerMove> done, int index, int firstDie, List<CheckerMove> next)
            {
                if (index == done.Count)
                {
                    if (next == null)
                    {
                        return IsWhole(index, firstDie);
                    }
                    Collect(index, firstDie, next);
                    return false;
                }
                CheckerMove move = done[index];
                if (move.from < 0 || move.from > Bar || move.to < 0 || move.to > Off || move.to == Bar)
                {
                    return false;
                }
                int from = OwnPoint(side, move.from);
                int to = OwnPoint(side, move.to);
                for (int die = 1; die <= 6; die++)
                {
                    if (DiceLeft[die] == 0 || !BackgammonPlayGenerator.CanMove(Mine, Theirs, from, die, out int reached) || reached != to)
                    {
                        continue;
                    }
                    bool hit = Make(from, to);
                    DiceLeft[die]--;
                    bool whole = Walk(done, index + 1, index == 0 ? die : firstDie, next);
                    DiceLeft[die]++;
                    Unmake(from, to, hit);
                    if (whole && next == null)
                    {
                        return true;
                    }
                }
                return false;
            }

            /// <summary>Gives the dice to <paramref name="moves"/> in <paramref name="order"/>; true with them in <paramref name="chosen"/> when the play is whole.</summary>
            public bool Assign(CheckerMove[] moves, int[] order, int index, int firstDie, CheckerMove[] chosen)
            {
                if (index == order.Length)
                {
                    return IsWhole(index, firstDie);
                }
                CheckerMove move = moves[order[index]];
                int from = OwnPoint(side, move.from);
                int to = OwnPoint(side, move.to);
                for (int die = 1; die <= 6; die++)
                {
                    if (DiceLeft[die] == 0 || !BackgammonPlayGenerator.CanMove(Mine, Theirs, from, die, out int reached) || reached != to)
                    {
                        continue;
                    }
                    bool hit = Make(from, to);
                    DiceLeft[die]--;
                    chosen[index] = new CheckerMove(move.from, move.to, die);
                    bool whole = Assign(moves, order, index + 1, index == 0 ? die : firstDie, chosen);
                    DiceLeft[die]++;
                    Unmake(from, to, hit);
                    if (whole)
                    {
                        return true;
                    }
                }
                return false;
            }

            private bool IsWhole(int used, int firstDie)
            {
                return used == MaxDice && (requiredDie == 0 || firstDie == requiredDie);
            }

            private void Collect(int used, int firstDie, List<CheckerMove> next)
            {
                for (int from = BackgammonPlayGenerator.BarPoint; from >= 1; from--)
                {
                    if (Mine[from] == 0)
                    {
                        continue;
                    }
                    for (int die = 1; die <= 6; die++)
                    {
                        if (DiceLeft[die] == 0 || !BackgammonPlayGenerator.CanMove(Mine, Theirs, from, die, out int to))
                        {
                            continue;
                        }
                        bool hit = Make(from, to);
                        DiceLeft[die]--;
                        bool fits = CanFinish(used + 1, used == 0 ? die : firstDie);
                        DiceLeft[die]++;
                        Unmake(from, to, hit);
                        if (!fits)
                        {
                            continue;
                        }
                        var move = new CheckerMove(PlaceOf(side, from), PlaceOf(side, to), die);
                        bool known = false;
                        for (int i = 0; i < next.Count && !known; i++)
                        {
                            known = next[i].SamePath(move);
                        }
                        if (!known)
                        {
                            next.Add(move);
                        }
                    }
                }
            }

            private bool CanFinish(int used, int firstDie)
            {
                if (used >= MaxDice)
                {
                    return IsWhole(used, firstDie);
                }
                for (int die = 1; die <= 6; die++)
                {
                    if (DiceLeft[die] == 0)
                    {
                        continue;
                    }
                    for (int from = BackgammonPlayGenerator.BarPoint; from >= 1; from--)
                    {
                        if (Mine[from] == 0 || !BackgammonPlayGenerator.CanMove(Mine, Theirs, from, die, out int to))
                        {
                            continue;
                        }
                        bool hit = Make(from, to);
                        DiceLeft[die]--;
                        bool fits = CanFinish(used + 1, used == 0 ? die : firstDie);
                        DiceLeft[die]++;
                        Unmake(from, to, hit);
                        if (fits)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }

            private bool Make(int from, int to)
            {
                bool hit = to >= 1 && Theirs[BackgammonPlayGenerator.BarPoint - to] == 1;
                Mine[from]--;
                Mine[to]++;
                if (hit)
                {
                    Theirs[BackgammonPlayGenerator.BarPoint - to] = 0;
                    Theirs[BackgammonPlayGenerator.BarPoint]++;
                }
                return hit;
            }

            private void Unmake(int from, int to, bool hit)
            {
                if (hit)
                {
                    Theirs[BackgammonPlayGenerator.BarPoint]--;
                    Theirs[BackgammonPlayGenerator.BarPoint - to] = 1;
                }
                Mine[to]--;
                Mine[from]++;
            }
        }
    }
}
