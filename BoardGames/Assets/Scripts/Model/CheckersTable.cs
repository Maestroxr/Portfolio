using System.Collections.Generic;
using Gamebox.Lockstep;

namespace Portfolio.Checkers
{
    /// <summary>
    /// One game of draughts played in step: the two seats of the setup, the game they play, and the commands the log
    /// hands it. A move is taken only from the seat whose turn it is and only along a legal path; when the clock runs
    /// out, the Easy computer moves for whoever kept the table waiting, the same way on every device.
    /// </summary>
    public sealed class CheckersTable : LockstepTable<CheckersCommand>
    {
        /// <summary>A new game of <c>setup.variant</c>.</summary>
        public CheckersTable(TableSetup setup, uint seed) : base(seed)
        {
            Setup = setup.Clone();
            Setup.variant = (int)VariantRules.Of(setup.variant).Variant;
            Game = new CheckersGame((Variant)Setup.variant);
        }

        /// <summary>A saved game goes on; the seats of <paramref name="setup"/> may rename or retake those of the save.</summary>
        public CheckersTable(TableSetup setup, CheckersGame saved, uint seed) : base(seed)
        {
            Setup = setup.Clone();
            Game = saved.Clone();
            Setup.variant = Game.variant;
        }

        public TableSetup Setup { get; }

        public CheckersGame Game { get; }

        /// <summary>The seat whose turn it is; -1 once the game is over.</summary>
        public int SeatToMove => Game.IsOver ? -1 : Setup.SeatOf(Game.SideToMove);

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
        public CheckersCommand LastCommand { get; private set; }

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

        protected override bool Accept(CheckersCommand command)
        {
            LastCommand = null;
            if (command == null || !SeatValid(command.seat))
            {
                return false;
            }
            bool taken;
            switch (command.Kind)
            {
                case CommandKind.Move:
                {
                    if (command.seat != SeatToMove)
                    {
                        return false;
                    }
                    Move move = Game.FindLegal(command.path);
                    taken = move != null && Game.Play(move);
                    break;
                }
                case CommandKind.Resign:
                    taken = !Game.IsOver && Game.Resign(Setup.SideOf(command.seat));
                    break;
                case CommandKind.SeatToComputer:
                    taken = SeatToComputer(command.seat, command.level);
                    break;
                default:
                    taken = false;
                    break;
            }
            if (taken)
            {
                LastCommand = command;
            }
            return taken;
        }

        protected override void DefaultMoves(List<CheckersCommand> made)
        {
            int seat = SeatToMove;
            if (seat < 0)
            {
                return;
            }
            Move move = CheckersAI.Choose(Game, 0, Random);
            if (move == null)
            {
                return;
            }
            CheckersCommand command = CheckersCommand.MoveAlong(seat, move);
            if (Accept(command))
            {
                made.Add(command);
            }
        }

        protected override bool SeatToComputer(int seat, int level)
        {
            if (!SeatValid(seat))
            {
                return false;
            }
            TableSeat taken = Setup.seats[seat];
            taken.computer = true;
            taken.level = level < 0 ? 0 : level >= CheckersAI.Levels ? CheckersAI.Levels - 1 : level;
            return true;
        }

        private bool SeatValid(int seat)
        {
            return seat >= 0 && seat < Setup.seats.Length && Setup.seats[seat] != null;
        }
    }
}
