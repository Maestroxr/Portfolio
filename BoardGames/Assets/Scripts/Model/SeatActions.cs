namespace Portfolio.BoardGames
{
    /// <summary>
    /// What a seat may do at a table right now, read from the rules alone: whether the table waits for its decision,
    /// which of backgammon's buttons apply (the roll, the cube, the answer to an offered cube, the checkers of a roll)
    /// and whether it may still give up. The HUD at the table and the remote control of a shared screen's room show
    /// the same buttons from it, and a tour or a test asks it instead of the interface.
    /// </summary>
    public readonly struct SeatActions
    {
        /// <summary>Nothing: no table, a seat the table has not, the computer's seat, or a game that is over.</summary>
        public static readonly SeatActions None = default;

        private SeatActions(bool toAct, bool canRoll, bool canDouble, bool answering, bool moving, bool mayResign)
        {
            ToAct = toAct;
            CanRoll = canRoll;
            CanDouble = canDouble;
            Answering = answering;
            Moving = moving;
            MayResign = mayResign;
        }

        /// <summary>The table waits for a decision of the seat.</summary>
        public bool ToAct { get; }

        /// <summary>Backgammon: the seat may roll the dice now (the opening roll too).</summary>
        public bool CanRoll { get; }

        /// <summary>Backgammon: the seat may offer the cube now (before rolling, the cube in the middle or its own).</summary>
        public bool CanDouble { get; }

        /// <summary>Backgammon: the seat answers an offered cube (Take or Drop).</summary>
        public bool Answering { get; }

        /// <summary>Backgammon: the seat moves the checkers of its roll.</summary>
        public bool Moving { get; }

        /// <summary>The seat may give up: a person's seat of a game that is not over, whether or not it is to act.</summary>
        public bool MayResign { get; }

        /// <summary>Whether any of backgammon's buttons (Roll, Double, Take, Drop) applies.</summary>
        public bool AnyBackgammonButton => CanRoll || CanDouble || Answering;

        /// <summary>
        /// The actions of <paramref name="seat"/> at <paramref name="table"/> now: <see cref="None"/> without a table, for
        /// a seat the table has not and for a seat the computer plays.
        /// </summary>
        public static SeatActions Of(BoardTable table, int seat)
        {
            if (table == null || seat < 0 || seat >= table.Setup.seats.Length || table.IsComputer(seat))
            {
                return None;
            }
            bool over = table.Game.IsOver;
            bool toAct = !over && table.SeatToAct == seat;
            var backgammon = table.Game as BackgammonGame;
            BackgammonPhase phase = backgammon != null ? backgammon.Phase : BackgammonPhase.Over;
            bool canRoll = toAct && backgammon != null && (phase == BackgammonPhase.Opening || phase == BackgammonPhase.Rolling);
            bool canDouble = toAct && backgammon != null && backgammon.CanDouble(backgammon.SideToAct);
            bool answering = toAct && backgammon != null && phase == BackgammonPhase.Answering;
            bool moving = toAct && backgammon != null && phase == BackgammonPhase.Moving;
            return new SeatActions(toAct, canRoll, canDouble, answering, moving, !over);
        }

        public override string ToString()
        {
            return ToAct
                ? $"to act{(CanRoll ? ", roll" : "")}{(CanDouble ? ", double" : "")}{(Answering ? ", take or drop" : "")}{(Moving ? ", moving" : "")}"
                : MayResign ? "waiting" : "nothing";
        }
    }
}
