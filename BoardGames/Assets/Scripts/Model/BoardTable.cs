using System.Collections.Generic;
using Gamebox.Lockstep;

namespace Portfolio.BoardGames
{
    /// <summary>
    /// One game played in step, whatever the game: the two seats of the setup, the game they play and the commands the
    /// log hands it. What every game shares is here: giving up, the computer taking a seat over, who the table waits
    /// for, who won, the checksum. A command of the game itself (a move, a roll, the cube) is the subclass's to judge
    /// (<see cref="AcceptPlay"/>), and when the clock runs out the subclass names the default command for whoever kept
    /// the table waiting (<see cref="DefaultCommand"/>), worked out the same way on every device.
    /// </summary>
    public abstract class BoardTable : LockstepTable<BoardCommand>
    {
        /// <summary>The most commands a time-out makes for one seat (a backgammon turn is a roll and a play).</summary>
        private const int MaxDefaultCommands = 4;

        protected BoardTable(TableSetup setup, uint seed) : base(seed)
        {
            Setup = setup.Clone();
        }

        /// <summary>The table of <paramref name="setup"/>'s rule set: a new game, or <paramref name="saved"/> going on.</summary>
        public static BoardTable Create(TableSetup setup, uint seed, IBoardGame saved = null)
        {
            switch (saved != null ? RuleSets.ModeOf(saved.RuleSet) : setup.Mode)
            {
                case GameMode.Chess:
                    return new ChessTable(setup, seed, saved as ChessGame);
                case GameMode.Backgammon:
                    return new BackgammonTable(setup, seed, saved as BackgammonGame);
                default:
                    return new CheckersTable(setup, seed, saved as CheckersGame);
            }
        }

        public TableSetup Setup { get; }

        /// <summary>The game of the table; each subclass also has it by its own type.</summary>
        public abstract IBoardGame Game { get; }

        public RuleSet RuleSet => Game.RuleSet;

        public GameMode Mode => RuleSets.ModeOf(Game.RuleSet);

        /// <summary>The seat whose decision the game waits for; -1 once it is over.</summary>
        public int SeatToAct => Game.IsOver ? -1 : Setup.SeatOf(Game.SideToAct);

        /// <summary>The same as <see cref="SeatToAct"/>: the seat to move in draughts and chess.</summary>
        public int SeatToMove => SeatToAct;

        public bool IsComputer(int seat)
        {
            return SeatValid(seat) && Setup.seats[seat].computer;
        }

        public int LevelOf(int seat)
        {
            return SeatValid(seat) ? Setup.seats[seat].level : 0;
        }

        /// <summary>The seat that won; -1 while the game is on and on a draw.</summary>
        public int WinnerSeat
        {
            get
            {
                Side? winner = Game.Winner;
                return winner.HasValue ? Setup.SeatOf(winner.Value) : -1;
            }
        }

        public override bool IsOver => Game.IsOver;

        /// <summary>The command the last Accept took; null after BeforeEntry and after a refusal.</summary>
        public BoardCommand LastCommand { get; private set; }

        /// <summary>The game's checksum folded with the seats' computer flags and levels.</summary>
        public override uint Checksum()
        {
            var sum = new StateChecksum();
            sum.Add(Game.Checksum());
            for (int i = 0; i < Setup.seats.Length; i++)
            {
                sum.Add(Setup.seats[i].computer).Add(Setup.seats[i].level);
            }
            return sum.Value;
        }

        protected override void BeforeEntry()
        {
            LastCommand = null;
        }

        protected sealed override bool Accept(BoardCommand command)
        {
            LastCommand = null;
            if (command == null || !SeatValid(command.seat))
            {
                return false;
            }
            bool taken;
            switch (command.Kind)
            {
                case CommandKind.Resign:
                    taken = !Game.IsOver && Game.Resign(Setup.SideOf(command.seat));
                    break;
                case CommandKind.SeatToComputer:
                    taken = SeatToComputer(command.seat, command.level);
                    break;
                default:
                    taken = !Game.IsOver && AcceptPlay(command);
                    break;
            }
            if (taken)
            {
                LastCommand = command;
            }
            return taken;
        }

        /// <summary>
        /// A command of the game itself (anything but giving up and the computer taking a seat): taken only from the
        /// seat the game waits for and only when the rules allow it now. The game is not over when this is called.
        /// </summary>
        protected abstract bool AcceptPlay(BoardCommand command);

        /// <summary>
        /// The command the clock makes for <paramref name="seat"/> (the one the game waits for): the Easy computer's
        /// choice, from the table's <see cref="LockstepTable{TCommand}.Random"/>, so every device makes the same. Null
        /// when there is none.
        /// </summary>
        protected abstract BoardCommand DefaultCommand(int seat);

        protected sealed override void DefaultMoves(List<BoardCommand> made)
        {
            int seat = SeatToAct;
            // The clock ran out for this seat: it makes the decisions of its turn until the game waits for somebody else.
            for (int i = 0; i < MaxDefaultCommands && seat >= 0 && SeatToAct == seat; i++)
            {
                BoardCommand command = DefaultCommand(seat);
                if (command == null || !Accept(command))
                {
                    return;
                }
                made.Add(command);
            }
        }

        protected sealed override bool SeatToComputer(int seat, int level)
        {
            if (!SeatValid(seat))
            {
                return false;
            }
            TableSeat taken = Setup.seats[seat];
            taken.computer = true;
            taken.level = level < 0 ? 0 : level >= RuleSets.ComputerLevels ? RuleSets.ComputerLevels - 1 : level;
            return true;
        }

        protected bool SeatValid(int seat)
        {
            return seat >= 0 && seat < Setup.seats.Length && Setup.seats[seat] != null;
        }
    }
}
