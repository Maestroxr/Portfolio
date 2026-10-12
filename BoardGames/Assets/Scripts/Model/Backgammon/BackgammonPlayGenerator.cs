namespace Portfolio.BoardGames
{
    /// <summary>
    /// Every distinct whole play of a roll, worked out on the mover's own view of the board: two arrays of 26 counts,
    /// each side in its own point numbers (0 borne off, 1 to 24 its 1-point to 24-point, 25 the bar), so a checker of
    /// the mover on its point p stands on the opponent's point 25 - p. The search goes depth first over the dice (both
    /// orders of two different dice; for a double the four moves take their checkers from points that never rise, which
    /// reaches every position once), keeps only the plays that use the most dice, and of a single die the larger when
    /// it can be played. Plays that end in the same position are one play: the first found stays. The order is fixed
    /// (the larger die first, the bar first, then from the farthest point), so every device lists the same plays.
    /// One generator is reused for many rolls without allocating; it changes the arrays it is given only while it works.
    /// </summary>
    internal sealed class BackgammonPlayGenerator
    {
        public const int Slots = 26;
        public const int BarPoint = 25;
        public const int OffPoint = 0;

        private uint[] plays = new uint[64];
        private byte[] counts = new byte[64];

        private ulong[] keyA = new ulong[1024];
        private ulong[] keyB = new ulong[1024];
        private int[] slotStamp = new int[1024];
        private int stamp;
        private int tableMask = 1023;

        private int[] me;
        private int[] them;
        private readonly int[] dice = new int[4];
        private int diceCount;
        private bool isDouble;
        private int outside;
        private ulong currentA;
        private ulong currentB;

        /// <summary>The plays found by the last <see cref="Generate"/>.</summary>
        public int Count { get; private set; }

        /// <summary>How many dice each play of the last <see cref="Generate"/> uses (0 when the roll allows nothing).</summary>
        public int DiceUsed { get; private set; }

        /// <summary>
        /// Lists the plays of <paramref name="die1"/> and <paramref name="die2"/> for the side whose checkers are
        /// <paramref name="mine"/>. When nothing can be played there is one empty play.
        /// </summary>
        public void Generate(int[] mine, int[] theirs, int die1, int die2)
        {
            me = mine;
            them = theirs;
            Count = 0;
            DiceUsed = 0;
            NewTable();
            outside = 0;
            for (int p = 7; p <= BarPoint; p++)
            {
                outside += me[p];
            }
            currentA = 0UL;
            currentB = 0UL;
            for (int p = 0; p < Slots; p++)
            {
                AddUnit(p, me[p]);
            }
            isDouble = die1 == die2;
            if (isDouble)
            {
                diceCount = 4;
                dice[0] = dice[1] = dice[2] = dice[3] = die1;
                Search(0, 0u, BarPoint);
            }
            else
            {
                int high = die1 > die2 ? die1 : die2;
                int low = die1 > die2 ? die2 : die1;
                diceCount = 2;
                dice[0] = high;
                dice[1] = low;
                Search(0, 0u, BarPoint);
                dice[0] = low;
                dice[1] = high;
                Search(0, 0u, BarPoint);
                if (DiceUsed == 1)
                {
                    KeepLargerDie(high);
                }
            }
            me = null;
            them = null;
        }

        public int MoveCount(int play)
        {
            return counts[play];
        }

        /// <summary>The point (own numbers, 25 the bar) the <paramref name="move"/>-th checker of a play starts from.</summary>
        public int From(int play, int move)
        {
            return (int)((plays[play] >> (8 * move)) & 31u);
        }

        public int Die(int play, int move)
        {
            return (int)((plays[play] >> (8 * move + 5)) & 7u);
        }

        /// <summary>The point (own numbers, 0 off) the <paramref name="move"/>-th checker of a play ends on.</summary>
        public int To(int play, int move)
        {
            int to = From(play, move) - Die(play, move);
            return to < 0 ? 0 : to;
        }

        /// <summary>Makes a play of the list on <paramref name="mine"/> and <paramref name="theirs"/> (hits included).</summary>
        public void Apply(int play, int[] mine, int[] theirs)
        {
            int n = counts[play];
            for (int i = 0; i < n; i++)
            {
                MakeMove(mine, theirs, From(play, i), To(play, i));
            }
        }

        /// <summary>Moves one checker from <paramref name="from"/> to <paramref name="to"/>, sending a lone opposing checker there to the bar.</summary>
        public static void MakeMove(int[] mine, int[] theirs, int from, int to)
        {
            mine[from]--;
            mine[to]++;
            if (to >= 1 && to <= 24 && theirs[BarPoint - to] == 1)
            {
                theirs[BarPoint - to] = 0;
                theirs[BarPoint]++;
            }
        }

        /// <summary>
        /// Whether the checker on <paramref name="from"/> may move by <paramref name="die"/> now (the bar first, an open
        /// point, bearing off only with every checker home and a larger die only from the highest point); the point it
        /// reaches comes out in <paramref name="to"/> (0 off).
        /// </summary>
        public static bool CanMove(int[] mine, int[] theirs, int from, int die, out int to)
        {
            to = from - die;
            if (from < 1 || from > BarPoint || die < 1 || die > 6 || mine[from] <= 0)
            {
                return false;
            }
            if (mine[BarPoint] > 0 && from != BarPoint)
            {
                return false;
            }
            if (to >= 1)
            {
                return theirs[BarPoint - to] < 2;
            }
            for (int p = 7; p <= BarPoint; p++)
            {
                if (mine[p] > 0)
                {
                    return false;
                }
            }
            if (to < 0)
            {
                for (int p = from + 1; p <= 6; p++)
                {
                    if (mine[p] > 0)
                    {
                        return false;
                    }
                }
            }
            to = 0;
            return true;
        }

        private void Search(int step, uint packed, int maxFrom)
        {
            bool moved = false;
            if (step < diceCount)
            {
                int die = dice[step];
                if (me[BarPoint] > 0)
                {
                    moved = TryFrom(BarPoint, die, step, packed);
                }
                else
                {
                    int top = isDouble && maxFrom < 24 ? maxFrom : 24;
                    for (int p = top; p >= 1; p--)
                    {
                        if (me[p] > 0 && TryFrom(p, die, step, packed))
                        {
                            moved = true;
                        }
                    }
                }
            }
            if (!moved)
            {
                Record(step, packed);
            }
        }

        private bool TryFrom(int from, int die, int step, uint packed)
        {
            int to = from - die;
            bool hit = false;
            if (to >= 1)
            {
                int theirs = them[BarPoint - to];
                if (theirs >= 2)
                {
                    return false;
                }
                hit = theirs == 1;
            }
            else
            {
                if (outside > 0)
                {
                    return false;
                }
                if (to < 0)
                {
                    for (int p = from + 1; p <= 6; p++)
                    {
                        if (me[p] > 0)
                        {
                            return false;
                        }
                    }
                }
                to = 0;
            }
            bool cameHome = from > 6 && to <= 6;
            me[from]--;
            me[to]++;
            AddUnit(from, -1);
            AddUnit(to, 1);
            if (cameHome)
            {
                outside--;
            }
            ulong hitBit = 0UL;
            if (hit)
            {
                them[BarPoint - to] = 0;
                them[BarPoint]++;
                hitBit = 1UL << (39 + to);
                currentB |= hitBit;
            }
            Search(step + 1, packed | ((uint)(from | (die << 5)) << (8 * step)), from);
            if (hit)
            {
                currentB &= ~hitBit;
                them[BarPoint]--;
                them[BarPoint - to] = 1;
            }
            if (cameHome)
            {
                outside++;
            }
            AddUnit(to, -1);
            AddUnit(from, 1);
            me[to]--;
            me[from]++;
            return true;
        }

        /// <summary>
        /// The position key: the mover's 26 counts as nibbles (A holds points 0 to 15, B 16 to 25) and, above them in B,
        /// a bit per point where a lone opposing checker was hit: together they are the whole position a play leaves.
        /// </summary>
        private void AddUnit(int point, int amount)
        {
            if (point < 16)
            {
                currentA = (ulong)((long)currentA + ((long)amount << (4 * point)));
            }
            else
            {
                currentB = (ulong)((long)currentB + ((long)amount << (4 * (point - 16))));
            }
        }

        private void Record(int used, uint packed)
        {
            if (used < DiceUsed)
            {
                return;
            }
            if (used > DiceUsed)
            {
                DiceUsed = used;
                Count = 0;
                NewTable();
            }
            if (!Insert(currentA, currentB))
            {
                return;
            }
            if (Count == plays.Length)
            {
                System.Array.Resize(ref plays, Count * 2);
                System.Array.Resize(ref counts, Count * 2);
            }
            plays[Count] = packed;
            counts[Count] = (byte)used;
            Count++;
        }

        /// <summary>Only one die could be played: keeps the plays of the larger when there are any.</summary>
        private void KeepLargerDie(int high)
        {
            bool any = false;
            for (int i = 0; i < Count && !any; i++)
            {
                any = Die(i, 0) == high;
            }
            if (!any)
            {
                return;
            }
            int kept = 0;
            for (int i = 0; i < Count; i++)
            {
                if (Die(i, 0) == high)
                {
                    plays[kept] = plays[i];
                    counts[kept] = counts[i];
                    kept++;
                }
            }
            Count = kept;
        }

        private void NewTable()
        {
            stamp++;
            if (stamp == int.MaxValue)
            {
                stamp = 1;
                System.Array.Clear(slotStamp, 0, slotStamp.Length);
            }
        }

        /// <summary>Adds a position key to the open addressing table; false when it is there already.</summary>
        private bool Insert(ulong a, ulong b)
        {
            if ((Count + 1) * 2 > slotStamp.Length)
            {
                Grow();
            }
            int slot = Slot(a, b);
            while (slotStamp[slot] == stamp)
            {
                if (keyA[slot] == a && keyB[slot] == b)
                {
                    return false;
                }
                slot = (slot + 1) & tableMask;
            }
            slotStamp[slot] = stamp;
            keyA[slot] = a;
            keyB[slot] = b;
            return true;
        }

        private int Slot(ulong a, ulong b)
        {
            ulong hash = a * 0x9E3779B97F4A7C15UL ^ (b + 0x632BE59BD9B4E019UL) * 0xC2B2AE3D27D4EB4FUL;
            hash ^= hash >> 29;
            return (int)(hash & (ulong)tableMask);
        }

        private void Grow()
        {
            ulong[] oldA = keyA;
            ulong[] oldB = keyB;
            int[] oldStamp = slotStamp;
            int oldCurrent = stamp;
            int size = slotStamp.Length * 2;
            keyA = new ulong[size];
            keyB = new ulong[size];
            slotStamp = new int[size];
            tableMask = size - 1;
            stamp = 1;
            for (int i = 0; i < oldStamp.Length; i++)
            {
                if (oldStamp[i] == oldCurrent)
                {
                    int slot = Slot(oldA[i], oldB[i]);
                    while (slotStamp[slot] == stamp)
                    {
                        slot = (slot + 1) & tableMask;
                    }
                    slotStamp[slot] = stamp;
                    keyA[slot] = oldA[i];
                    keyB[slot] = oldB[i];
                }
            }
        }
    }
}
