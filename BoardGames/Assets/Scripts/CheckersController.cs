using Gamebox;

namespace Portfolio.Checkers
{
    /// <summary>
    /// The local controller of the Checkers module: it takes the commands of the people at this device (a move, giving
    /// up), checks that the seat may act now and gives them to the manager, which applies them to the table. Starting a
    /// game goes through the base <see cref="OfflineGameController"/> flow into <see cref="CheckersGameManager.StartGame"/>.
    /// In an online game the commands go to the server instead, through <see cref="CheckersOnlineController"/>.
    /// </summary>
    public sealed class CheckersController : OfflineGameController, ICheckersCommands
    {
        public CheckersGameManager Checkers => BaseManager as CheckersGameManager;

        /// <summary>Whether <paramref name="seat"/> is a person at this device the table waits for right now.</summary>
        private bool Deciding(int seat)
        {
            CheckersGameManager manager = Checkers;
            CheckersTable table = manager != null ? manager.Table : null;
            return table != null && !manager.IsOnlineGame && manager.IsGameRunning && manager.WaitingForHuman && table.SeatToMove == seat
                && !table.IsComputer(seat);
        }

        public bool Move(int seat, Move move)
        {
            if (move == null || !Deciding(seat))
            {
                return false;
            }
            Checkers.ApplyLocalCommand(CheckersCommand.MoveAlong(seat, move));
            return true;
        }

        public bool Resign(int seat)
        {
            CheckersGameManager manager = Checkers;
            CheckersTable table = manager != null ? manager.Table : null;
            if (table == null || manager.IsOnlineGame || !manager.IsGameRunning || table.IsOver || seat < 0 || seat > 1 || table.IsComputer(seat))
            {
                return false;
            }
            Checkers.ApplyLocalCommand(CheckersCommand.Resign(seat));
            return true;
        }
    }
}
