namespace Portfolio.BoardGames
{
    /// <summary>
    /// One game of chess played in step. A move is taken only from the seat to move and only when it is legal; when the
    /// clock runs out, the Easy computer moves for whoever kept the table waiting, the same way on every device.
    /// </summary>
    public sealed class ChessTable : BoardTable
    {
        /// <summary>A new game, or <paramref name="saved"/> going on.</summary>
        public ChessTable(TableSetup setup, uint seed, ChessGame saved = null) : base(setup, seed)
        {
            Chess = saved != null ? saved.Clone() : ChessGame.NewGame();
            Setup.ruleSet = (int)RuleSet.Chess;
        }

        public ChessGame Chess { get; }

        public override IBoardGame Game => Chess;

        protected override bool AcceptPlay(BoardCommand command)
        {
            if (command.Kind != CommandKind.Move || command.seat != SeatToAct)
            {
                return false;
            }
            ChessMove move = Chess.FindLegal(command.path);
            return move != null && Chess.Play(move);
        }

        protected override BoardCommand DefaultCommand(int seat)
        {
            return ChessAI.Choose(Chess, seat, 0, Random);
        }
    }
}
