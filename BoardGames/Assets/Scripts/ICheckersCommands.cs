namespace Portfolio.Checkers
{
    /// <summary>
    /// What a person at this device may do at the table: move a piece or give up. The offline controller applies the
    /// command to the table at once; the online controller sends it to the room and the table takes it when the server
    /// echoes it. Both answer false when the seat may not act now.
    /// </summary>
    public interface ICheckersCommands
    {
        bool Move(int seat, Move move);

        bool Resign(int seat);
    }
}
