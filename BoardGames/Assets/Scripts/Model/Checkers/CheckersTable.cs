namespace Portfolio.BoardGames
{
    /// <summary>
    /// One game of draughts played in step. A move is taken only from the seat whose turn it is and only along a legal
    /// path; when the clock runs out, the Easy computer moves for whoever kept the table waiting, the same way on every
    /// device.
    /// </summary>
    public sealed class CheckersTable : BoardTable
    {
        /// <summary>A new game of the setup's draughts rule set, or <paramref name="saved"/> going on (the setup's seats may rename or retake those of the save).</summary>
        public CheckersTable(TableSetup setup, uint seed, CheckersGame saved = null) : base(setup, seed)
        {
            Checkers = saved != null ? saved.Clone() : new CheckersGame(RuleSets.CheckersVariant(Setup.RuleSet));
            Setup.ruleSet = (int)Checkers.RuleSet;
        }

        public CheckersGame Checkers { get; }

        public override IBoardGame Game => Checkers;

        protected override bool AcceptPlay(BoardCommand command)
        {
            if (command.Kind != CommandKind.Move || command.seat != SeatToAct)
            {
                return false;
            }
            Move move = Checkers.FindLegal(command.path);
            return move != null && Checkers.Play(move);
        }

        protected override BoardCommand DefaultCommand(int seat)
        {
            Move move = CheckersAI.Choose(Checkers, 0, Random);
            return move != null ? BoardCommand.MoveAlong(seat, move) : null;
        }
    }
}
