using System.Collections.Generic;

namespace Portfolio.BoardGames
{
    /// <summary>
    /// The legal moves of a position under one of the three rule sets: every capture sequence when a capture is
    /// possible (only the longest ones under the majority rule), the plain steps and slides otherwise. The moves come
    /// out in the same order on every device: by the square they start from, then by their path.
    /// </summary>
    public static class MoveGenerator
    {
        // The four diagonal directions, in the order the moves are tried: towards higher rows first, left before right.
        private static readonly int[] RowSteps = { 1, 1, -1, -1 };
        private static readonly int[] ColSteps = { -1, 1, -1, 1 };

        /// <summary>Every legal move of <paramref name="side"/>, in a deterministic order; empty when it has none.</summary>
        public static List<Move> Legal(Board board, Side side, VariantRules rules)
        {
            var moves = new List<Move>();
            var walker = new Walker(board, side, rules, moves);
            walker.Captures();
            if (moves.Count == 0)
            {
                walker.PlainMoves();
            }
            else if (rules.MajorityCapture)
            {
                int most = 0;
                for (int i = 0; i < moves.Count; i++)
                {
                    if (moves[i].Jumps > most)
                    {
                        most = moves[i].Jumps;
                    }
                }
                moves.RemoveAll(move => move.Jumps < most);
            }
            moves.Sort(Compare);
            return moves;
        }

        /// <summary>Whether <paramref name="side"/> has a move at all, without listing them.</summary>
        public static bool HasAnyMove(Board board, Side side, VariantRules rules)
        {
            int size = board.size;
            int forward = VariantRules.Forward(side);
            sbyte[] cells = board.cells;
            for (int square = 0; square < cells.Length; square++)
            {
                sbyte piece = cells[square];
                if (!Pieces.Belongs(piece, side))
                {
                    continue;
                }
                bool king = Pieces.IsKing(piece);
                int row = square / size;
                int col = square % size;
                for (int d = 0; d < 4; d++)
                {
                    int dr = RowSteps[d];
                    int dc = ColSteps[d];
                    int r = row + dr;
                    int c = col + dc;
                    if (r < 0 || r >= size || c < 0 || c >= size)
                    {
                        continue;
                    }
                    sbyte next = cells[r * size + c];
                    bool forwards = dr == forward;
                    if (next == Pieces.None)
                    {
                        // A step; a jump needs an adjacent enemy, so an empty neighbour gives a man nothing backward.
                        if (king || forwards)
                        {
                            return true;
                        }
                        continue;
                    }
                    // The neighbour is a piece: a jump over it, if the rules let this piece capture that way. A flying
                    // king's slide would stop here too, so the short check covers it.
                    if (!king && !forwards && !rules.MenCaptureBackward)
                    {
                        continue;
                    }
                    if (Pieces.Belongs(next, side))
                    {
                        continue;
                    }
                    int landRow = r + dr;
                    int landCol = c + dc;
                    if (landRow >= 0 && landRow < size && landCol >= 0 && landCol < size
                        && cells[landRow * size + landCol] == Pieces.None)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>Makes <paramref name="move"/> on <paramref name="board"/> without asking whether it is legal.</summary>
        public static void Apply(Board board, Move move, VariantRules rules)
        {
            sbyte piece = board.cells[move.From];
            board.cells[move.From] = Pieces.None;
            if (move.captured != null)
            {
                for (int i = 0; i < move.captured.Length; i++)
                {
                    board.cells[move.captured[i]] = Pieces.None;
                }
            }
            board.cells[move.To] = move.promotes ? Pieces.King(Pieces.SideOf(piece)) : piece;
        }

        private static int Compare(Move a, Move b)
        {
            if (a.From != b.From)
            {
                return a.From.CompareTo(b.From);
            }
            int shared = a.path.Length < b.path.Length ? a.path.Length : b.path.Length;
            for (int i = 1; i < shared; i++)
            {
                if (a.path[i] != b.path[i])
                {
                    return a.path[i].CompareTo(b.path[i]);
                }
            }
            return a.path.Length.CompareTo(b.path.Length);
        }

        /// <summary>
        /// Walks the capture sequences of one side: the moving piece's origin counts as empty, the pieces it has taken
        /// stay where they are (never jumped twice, never slid over) until the sequence is over.
        /// </summary>
        private sealed class Walker
        {
            private readonly sbyte[] cells;
            private readonly int size;
            private readonly Side side;
            private readonly VariantRules rules;
            private readonly List<Move> moves;
            private readonly bool[] taken;
            private readonly int[] path;
            private readonly int[] captured;
            private int steps;
            private int origin;
            private bool startedAsMan;

            public Walker(Board board, Side side, VariantRules rules, List<Move> moves)
            {
                cells = board.cells;
                size = board.size;
                this.side = side;
                this.rules = rules;
                this.moves = moves;
                taken = new bool[cells.Length];
                path = new int[cells.Length / 2 + 2];
                captured = new int[cells.Length / 2 + 1];
            }

            public void Captures()
            {
                for (int square = 0; square < cells.Length; square++)
                {
                    sbyte piece = cells[square];
                    if (!Pieces.Belongs(piece, side))
                    {
                        continue;
                    }
                    origin = square;
                    startedAsMan = Pieces.IsMan(piece);
                    steps = 0;
                    path[0] = square;
                    Jumps(square, !startedAsMan);
                }
            }

            public void PlainMoves()
            {
                int forward = VariantRules.Forward(side);
                for (int square = 0; square < cells.Length; square++)
                {
                    sbyte piece = cells[square];
                    if (!Pieces.Belongs(piece, side))
                    {
                        continue;
                    }
                    bool king = Pieces.IsKing(piece);
                    int row = square / size;
                    int col = square % size;
                    for (int d = 0; d < 4; d++)
                    {
                        int dr = RowSteps[d];
                        int dc = ColSteps[d];
                        if (!king && dr != forward)
                        {
                            continue;
                        }
                        int r = row + dr;
                        int c = col + dc;
                        while (r >= 0 && r < size && c >= 0 && c < size && cells[r * size + c] == Pieces.None)
                        {
                            int to = r * size + c;
                            moves.Add(Move.Plain(square, to, !king && rules.IsCrowningRow(side, r)));
                            if (!king || !rules.FlyingKings)
                            {
                                break;
                            }
                            r += dr;
                            c += dc;
                        }
                    }
                }
            }

            private bool Empty(int square)
            {
                return square == origin || cells[square] == Pieces.None;
            }

            private bool Victim(int square)
            {
                return !taken[square] && square != origin && Pieces.Belongs(cells[square], Sides.Other(side));
            }

            /// <summary>Tries every further jump from <paramref name="square"/>; records the sequence when there is none.</summary>
            private void Jumps(int square, bool king)
            {
                bool any = false;
                int row = square / size;
                int col = square % size;
                int forward = VariantRules.Forward(side);
                for (int d = 0; d < 4; d++)
                {
                    int dr = RowSteps[d];
                    int dc = ColSteps[d];
                    if (!king && dr != forward && !rules.MenCaptureBackward)
                    {
                        continue;
                    }
                    int r = row + dr;
                    int c = col + dc;
                    if (king && rules.FlyingKings)
                    {
                        while (r >= 0 && r < size && c >= 0 && c < size && Empty(r * size + c))
                        {
                            r += dr;
                            c += dc;
                        }
                        if (r < 0 || r >= size || c < 0 || c >= size)
                        {
                            continue;
                        }
                        int victim = r * size + c;
                        if (!Victim(victim))
                        {
                            continue;
                        }
                        // The king may land on any empty square beyond the piece, but a capture goes on while it can: when
                        // a landing square allows another jump, the squares that allow none are not stops.
                        r += dr;
                        c += dc;
                        var goesOn = new List<int>();
                        var stops = new List<int>();
                        taken[victim] = true;
                        while (r >= 0 && r < size && c >= 0 && c < size && Empty(r * size + c))
                        {
                            int landing = r * size + c;
                            (HasJump(landing, true) ? goesOn : stops).Add(landing);
                            r += dr;
                            c += dc;
                        }
                        taken[victim] = false;
                        foreach (int landing in goesOn.Count > 0 ? goesOn : stops)
                        {
                            any = true;
                            Land(victim, landing, true);
                        }
                    }
                    else
                    {
                        int landRow = r + dr;
                        int landCol = c + dc;
                        if (landRow < 0 || landRow >= size || landCol < 0 || landCol >= size)
                        {
                            continue;
                        }
                        int victim = r * size + c;
                        int landing = landRow * size + landCol;
                        if (!Victim(victim) || !Empty(landing))
                        {
                            continue;
                        }
                        any = true;
                        bool crowned = king || (rules.PromoteDuringCapture && rules.IsCrowningRow(side, landRow));
                        Land(victim, landing, crowned);
                    }
                }
                if (!any && steps > 0)
                {
                    Record(square, king);
                }
            }

            /// <summary>Whether a piece standing on <paramref name="square"/> could jump from there, with the pieces taken so far in place.</summary>
            private bool HasJump(int square, bool king)
            {
                int row = square / size;
                int col = square % size;
                int forward = VariantRules.Forward(side);
                for (int d = 0; d < 4; d++)
                {
                    int dr = RowSteps[d];
                    int dc = ColSteps[d];
                    if (!king && dr != forward && !rules.MenCaptureBackward)
                    {
                        continue;
                    }
                    int r = row + dr;
                    int c = col + dc;
                    if (king && rules.FlyingKings)
                    {
                        while (r >= 0 && r < size && c >= 0 && c < size && Empty(r * size + c))
                        {
                            r += dr;
                            c += dc;
                        }
                        if (r < 0 || r >= size || c < 0 || c >= size || !Victim(r * size + c))
                        {
                            continue;
                        }
                        r += dr;
                        c += dc;
                        if (r >= 0 && r < size && c >= 0 && c < size && Empty(r * size + c))
                        {
                            return true;
                        }
                    }
                    else
                    {
                        int landRow = r + dr;
                        int landCol = c + dc;
                        if (landRow >= 0 && landRow < size && landCol >= 0 && landCol < size && Victim(r * size + c) && Empty(landRow * size + landCol))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }

            private void Land(int victim, int landing, bool king)
            {
                taken[victim] = true;
                captured[steps] = victim;
                steps++;
                path[steps] = landing;
                Jumps(landing, king);
                steps--;
                taken[victim] = false;
            }

            private void Record(int square, bool king)
            {
                var moveFile = new int[steps + 1];
                var took = new int[steps];
                for (int i = 0; i <= steps; i++)
                {
                    moveFile[i] = path[i];
                }
                for (int i = 0; i < steps; i++)
                {
                    took[i] = captured[i];
                }
                bool promotes = startedAsMan && (king || rules.IsCrowningRow(side, square / size));
                moves.Add(Move.Jump(moveFile, took, promotes));
            }
        }
    }
}
