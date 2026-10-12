using System;

namespace Portfolio.Checkers
{
    /// <summary>Who sits at one seat of the table: a name, and whether the computer plays it, at which level.</summary>
    [Serializable]
    public sealed class TableSeat
    {
        public string name = "";
        public bool computer;
        public int level = 1;

        public TableSeat Clone()
        {
            return new TableSeat { name = name, computer = computer, level = level };
        }
    }

    /// <summary>
    /// The table of one game as all three ways of playing set it up: the variant, the two seats, which seat plays the
    /// variant's first mover, and the clock of a local game.
    /// </summary>
    [Serializable]
    public sealed class TableSetup
    {
        public int variant;
        public TableSeat[] seats = { new TableSeat(), new TableSeat() };
        public int firstSeat;
        public int clockSeconds;

        /// <summary>The side <paramref name="seat"/> plays: the first mover for <see cref="firstSeat"/>, the other side for the other seat.</summary>
        public Side SideOf(int seat)
        {
            Side first = Rules.FirstMover;
            return seat == firstSeat ? first : Sides.Other(first);
        }

        public int SeatOf(Side side)
        {
            return SideOf(0) == side ? 0 : 1;
        }

        /// <summary>Seats not played by the computer.</summary>
        public int Humans
        {
            get
            {
                int humans = 0;
                for (int i = 0; i < seats.Length; i++)
                {
                    if (seats[i] != null && !seats[i].computer)
                    {
                        humans++;
                    }
                }
                return humans;
            }
        }

        public VariantRules Rules => VariantRules.Of(variant);

        public TableSetup Clone()
        {
            var copy = new TableSetup
            {
                variant = variant,
                firstSeat = firstSeat,
                clockSeconds = clockSeconds,
                seats = new TableSeat[seats.Length]
            };
            for (int i = 0; i < seats.Length; i++)
            {
                copy.seats[i] = seats[i] == null ? new TableSeat() : seats[i].Clone();
            }
            return copy;
        }
    }
}
