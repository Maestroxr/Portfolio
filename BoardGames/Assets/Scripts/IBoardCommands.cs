namespace Portfolio.BoardGames
{
    /// <summary>
    /// What a person at this device may do at the table: a decision of the game (a move, a roll, the cube) or giving
    /// up. The offline controller applies the command to the table at once; the online controller sends it to the room
    /// and the table takes it when the server echoes it. Both answer false when the seat may not act now.
    /// </summary>
    public interface IBoardCommands
    {
        /// <summary>A command of the game itself (any kind but Resign and SeatToComputer) for the seat the table waits for.</summary>
        bool Send(BoardCommand command);

        /// <summary>A draughts move (the same as sending its <see cref="BoardCommand.MoveAlong"/>).</summary>
        bool Move(int seat, Move move);

        bool Resign(int seat);
    }
}
