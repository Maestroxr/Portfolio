using Gamebox;

namespace Portfolio.BoardGames
{
    /// <summary>
    /// The local controller of the Board Games module: it takes the commands of the people at this device (a move, a
    /// roll, the cube, giving up), checks that the seat may act now and gives them to the manager, which applies them to
    /// the table. Starting a game goes through the base <see cref="OfflineGameController"/> flow into
    /// <see cref="BoardGamesManager.StartGame"/>. In an online game the commands go to the server instead, through
    /// <see cref="BoardGamesOnlineController"/>.
    /// </summary>
    public sealed class BoardGamesController : OfflineGameController, IBoardCommands
    {
        public BoardGamesManager Games => BaseManager as BoardGamesManager;

        /// <summary>Whether <paramref name="seat"/> is a person at this device the table waits for right now.</summary>
        private bool Deciding(int seat)
        {
            BoardGamesManager manager = Games;
            BoardTable table = manager != null ? manager.Table : null;
            return table != null && !manager.IsOnlineGame && manager.IsGameRunning && manager.WaitingForHuman && table.SeatToMove == seat
                && !table.IsComputer(seat);
        }

        public bool Send(BoardCommand command)
        {
            if (command == null || command.Kind == CommandKind.Resign || command.Kind == CommandKind.SeatToComputer || !Deciding(command.seat))
            {
                return false;
            }
            // False when the rules refused it: the person's input goes back to how it was.
            return Games.ApplyLocalCommand(command);
        }

        public bool Move(int seat, Move move)
        {
            return move != null && Send(BoardCommand.MoveAlong(seat, move));
        }

        public bool Resign(int seat)
        {
            BoardGamesManager manager = Games;
            BoardTable table = manager != null ? manager.Table : null;
            if (table == null || manager.IsOnlineGame || !manager.IsGameRunning || table.IsOver || seat < 0 || seat > 1 || table.IsComputer(seat))
            {
                return false;
            }
            Games.ApplyLocalCommand(BoardCommand.Resign(seat));
            return true;
        }
    }
}
