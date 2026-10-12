using System;

namespace Portfolio.BoardGames
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
    /// The table of one game as all three ways of playing set it up: the rule set, the two seats, which seat plays the
    /// side that moves first (Light: White in chess and backgammon), the clock of a local game and the backgammon
    /// options.
    /// </summary>
    [Serializable]
    public sealed class TableSetup
    {
        /// <summary>The <see cref="BoardGames.RuleSet"/> played, as its number.</summary>
        public int ruleSet;
        public TableSeat[] seats = { new TableSeat(), new TableSeat() };
        /// <summary>The seat that plays the rule set's first mover (<see cref="RuleSets.FirstMover"/>); the other seat plays the other side.</summary>
        public int firstSeat;
        /// <summary>Seconds per decision in a game at this device (0 = no clock); online the room's clock is the server's.</summary>
        public int clockSeconds;
        /// <summary>Backgammon: whether the doubling cube is in play.</summary>
        public bool doublingCube = true;
        /// <summary>
        /// Backgammon: whether the opening roll decides who starts (each side throws one die, the higher plays both); when
        /// false, the side of <see cref="firstSeat"/> starts with an ordinary roll.
        /// </summary>
        public bool openingRoll = true;

        public RuleSet RuleSet => RuleSets.Of(ruleSet);

        public GameMode Mode => RuleSets.ModeOf(RuleSet);

        /// <summary>The draughts rules of a draughts table (English for the other games).</summary>
        public VariantRules Rules => VariantRules.Of(RuleSets.CheckersVariant(RuleSet));

        /// <summary>The side <paramref name="seat"/> plays: the first mover for <see cref="firstSeat"/>, the other side for the other seat.</summary>
        public Side SideOf(int seat)
        {
            Side first = RuleSets.FirstMover(RuleSet);
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

        public TableSetup Clone()
        {
            var copy = new TableSetup
            {
                ruleSet = ruleSet,
                firstSeat = firstSeat,
                clockSeconds = clockSeconds,
                doublingCube = doublingCube,
                openingRoll = openingRoll,
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
