using SpacetimeDB;

/// <summary>
/// The server module of Checkers: the Gamebox base server (users and login, rooms with their members, the action log
/// and its clock, the seats of a table, tournaments; see the BaseServer folder of this project) plus what this file
/// adds. Both declare the same partial class, so the tables, reducers and helpers of the base server are in reach here.
///
/// An online game is played in step: every client runs the rules of the game and sends what its player does as an
/// action of the base server's log; the server puts the actions of a room in one order. This module knows no rules of
/// Checkers. It seats the table of two when a game starts (a member per seat, the computer in the second seat when the
/// room asks for an opponent), checks who may act for which seat, keeps the clock, hands the seat of a player who
/// leaves to the computer and keeps statistics.
///
/// After a change: publish the module, then generate the client bindings (Gamebox > Server in the Unity editor, or
/// <c>spacetime publish</c> and <c>spacetime generate</c> in the project folder, which read spacetime.json).
/// </summary>
public static partial class Module
{
    public const byte MaxSeats = 2;
    public const int MaxComputerPlayers = 1;
    /// <summary>The rule sets are levels of the room: English, Russian, International (the client's Variant).</summary>
    public const int VariantCount = 3;

    public const int DefaultTurnSeconds = 60;
    public const int MinTurnSeconds = 10;
    public const int MaxTurnSeconds = 600;

    /// <summary>The commands a player can send (the client's CommandKind): Move and Resign. SeatToComputer only ever arrives as the base server's own action.</summary>
    public const uint CommandLimit = 2;

    /// <summary>The names computer players take, in this order.</summary>
    private static readonly string[] BotNames = { "Ada", "Max" };

    // ---------------------------------------------------------------------------------------------------
    // Tables
    // ---------------------------------------------------------------------------------------------------

    /// <summary>What a player did in online games over time.</summary>
    [SpacetimeDB.Table(Accessor = "CheckersStats", Public = true)]
    public partial struct CheckersStats
    {
        [SpacetimeDB.PrimaryKey]
        public Identity Player;

        public uint Games;
        public uint Wins;
    }

    // ---------------------------------------------------------------------------------------------------
    // Extension points of the base server
    // ---------------------------------------------------------------------------------------------------

    static partial void ConfigureRoom(ReducerContext ctx, ref Room room, ref string? error)
    {
        // One person is enough when the computer takes the other seat.
        room.MinPlayers = Option(room.Options, ComputersOption, 0, 0, MaxComputerPlayers) >= 1 ? (byte)1 : (byte)2;
        if (room.MaxPlayers > MaxSeats)
        {
            error = "Checkers is played by two.";
            return;
        }
        room.MaxPlayers = MaxSeats;
        room.TurnSeconds = ClockOption(room.Options, DefaultTurnSeconds, MinTurnSeconds, MaxTurnSeconds);
        if (room.Level < 0 || room.Level >= VariantCount)
        {
            error = "There is no such rule set.";
        }
    }

    static partial void OnRoomStarted(ReducerContext ctx, Room room)
    {
        SeatTable(ctx, room, MaxSeats, MaxComputerPlayers, BotNames, (seat, user) => seat);
        StartActionClock(ctx, room);
    }

    static partial void ValidateAction(ReducerContext ctx, Room room, RoomMember member, ref RoomAction action, ref bool decisive, ref string? error)
    {
        if (action.Kind >= CommandLimit)
        {
            error = "There is no such command.";
            return;
        }
        error = CheckSeatAction(ctx, room, member, action.Seat);
        // Every command answers what the table waits for: a move or giving up.
        decisive = true;
    }

    static partial void OnTurnTimedOut(ReducerContext ctx, Room room, RoomTurn turn, ref bool handled)
    {
        // Every client makes the default move for whoever kept the table waiting, at this place in the log.
        AppendTimeout(ctx, room);
        handled = true;
    }

    static partial void OnMemberLeft(ReducerContext ctx, Room room, RoomMember member)
    {
        // The computer plays the seat on.
        HandSeatToComputer(ctx, room, member);
    }

    static partial void KeepPlaying(ReducerContext ctx, Room room, int membersLeft, ref bool keep)
    {
        // The computer plays the seat of the one who left: the game goes on while a member is still at the table.
        keep = membersLeft > 0 && HasComputerSeat(ctx, room.Id);
    }

    static partial void OnRoomFinished(ReducerContext ctx, Room room)
    {
        // The members take the places of their seats at the table, a computer player that won before them all.
        var winner = RankTable(ctx, room);
        foreach (var member in MembersOf(ctx, room.Id))
        {
            var stats = ctx.Db.CheckersStats.Player.Find(member.Identity) ?? ctx.Db.CheckersStats.Insert(new CheckersStats { Player = member.Identity });
            stats.Games++;
            if (member.Identity == winner)
            {
                stats.Wins++;
            }
            ctx.Db.CheckersStats.Player.Update(stats);
        }
    }

    static partial void OnUserDeleted(ReducerContext ctx, User user)
    {
        ctx.Db.CheckersStats.Player.Delete(user.Identity);
    }
}
