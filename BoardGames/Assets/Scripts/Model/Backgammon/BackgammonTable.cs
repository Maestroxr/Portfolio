namespace Portfolio.BoardGames
{
    /// <summary>
    /// One game of backgammon played in step. The dice come from the table's random numbers, reseeded by the stamp the
    /// server put on the roll, so every device throws the same and nobody knows the dice before the server spoke. A
    /// decision is taken only from the seat the game waits for. When the clock runs out the turn is finished for that
    /// seat: it rolls (never doubles) and the Easy computer plays the dice; an offered cube is taken.
    /// </summary>
    public sealed class BackgammonTable : BoardTable
    {
        /// <summary>A new game with the setup's backgammon options, or <paramref name="saved"/> going on.</summary>
        public BackgammonTable(TableSetup setup, uint seed, BackgammonGame saved = null) : base(setup, seed)
        {
            Backgammon = saved != null
                ? saved.Clone()
                : BackgammonGame.NewGame(Setup.doublingCube, Setup.openingRoll, Setup.openingRoll ? Side.Light : RuleSets.FirstMover(RuleSet.Backgammon));
            Setup.ruleSet = (int)RuleSet.Backgammon;
        }

        public BackgammonGame Backgammon { get; }

        public override IBoardGame Game => Backgammon;

        protected override bool AcceptPlay(BoardCommand command)
        {
            if (command.seat != SeatToAct)
            {
                return false;
            }
            Side side = Setup.SideOf(command.seat);
            switch (command.Kind)
            {
                case CommandKind.Roll:
                    return Throw();
                case CommandKind.Move:
                    return Backgammon.Phase == BackgammonPhase.Moving && Backgammon.Play(command.path);
                case CommandKind.Double:
                    return Backgammon.Double(side);
                case CommandKind.Take:
                    return Backgammon.Take(side);
                case CommandKind.Drop:
                    return Backgammon.Drop(side);
                default:
                    return false;
            }
        }

        /// <summary>The dice of a roll, from the random numbers of this entry; the opening roll throws again on a tie.</summary>
        private bool Throw()
        {
            BackgammonPhase phase = Backgammon.Phase;
            if (phase != BackgammonPhase.Opening && phase != BackgammonPhase.Rolling)
            {
                return false;
            }
            int a = Random.Range(1, 7);
            int b = Random.Range(1, 7);
            while (phase == BackgammonPhase.Opening && a == b)
            {
                a = Random.Range(1, 7);
                b = Random.Range(1, 7);
            }
            return Backgammon.Roll(a, b);
        }

        protected override BoardCommand DefaultCommand(int seat)
        {
            switch (Backgammon.Phase)
            {
                case BackgammonPhase.Opening:
                case BackgammonPhase.Rolling:
                    return BoardCommand.Roll(seat);
                case BackgammonPhase.Answering:
                    return BoardCommand.Take(seat);
                case BackgammonPhase.Moving:
                {
                    BackgammonPlay play = BackgammonAI.ChoosePlay(Backgammon, 0, Random);
                    return play != null ? BoardCommand.Play(seat, play.Path()) : null;
                }
                default:
                    return null;
            }
        }
    }
}
