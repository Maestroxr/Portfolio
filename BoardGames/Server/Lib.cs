using SpacetimeDB;

/// <summary>
/// The server module of Board Games (checkers, chess and backgammon): the Gamebox base server (users and login, rooms with their members, the action log
/// and its clock, the seats of a table, tournaments; see the BaseServer folder of this project) plus what this file
/// adds. Both declare the same partial class, so the tables, reducers and helpers of the base server are in reach here.
///
/// An online game is played in step: every client runs the rules of the game and sends what its player does as an
/// action of the base server's log; the server puts the actions of a room in one order. This module knows no rules of
/// the games. It seats the table of two when a game starts (a member per seat, the computer in the second seat when the
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
    /// <summary>
    /// The rule sets are levels of the room (the client's RuleSet): 0 English, 1 Russian and 2 International draughts,
    /// 3 chess, 4 backgammon.
    /// </summary>
    public const int RuleSetCount = 5;

    public const int DefaultTurnSeconds = 60;
    public const int MinTurnSeconds = 10;
    public const int MaxTurnSeconds = 600;

    /// <summary>
    /// The kinds of command (the client's CommandKind): 0 Move, 1 Resign, 2 SeatToComputer, 3 Roll, 4 Double, 5 Take,
    /// 6 Drop. Every kind below the limit but SeatToComputer may come from a player; SeatToComputer only ever arrives as
    /// the base server's own action.
    /// </summary>
    public const uint CommandLimit = 7;

    /// <summary>The kind a player may never send: the computer taking a seat over.</summary>
    public const uint SeatToComputerKind = 2;

    /// <summary>The first of backgammon's own kinds (Roll; then Double, Take, Drop): no other rule set sends them.</summary>
    public const uint RollKind = 3;

    /// <summary>The room level of backgammon.</summary>
    public const int BackgammonRuleSet = 4;

    /// <summary>A move names 2 to 32 numbers from 0 to 99 (the client's BoardCommand.MaxPathLength and MaxNumber).</summary>
    public const int MaxMoveNumbers = 32;
    public const int MaxMoveNumber = 99;

    /// <summary>The names computer players take, in this order.</summary>
    private static readonly string[] BotNames = { "Ada", "Max" };

    // ---------------------------------------------------------------------------------------------------
    // Tables
    // ---------------------------------------------------------------------------------------------------

    /// <summary>What a player did in online games over time.</summary>
    [SpacetimeDB.Table(Accessor = "BoardGamesStats", Public = true)]
    public partial struct BoardGamesStats
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
            error = "Board games are played by two.";
            return;
        }
        room.MaxPlayers = MaxSeats;
        room.TurnSeconds = ClockOption(room.Options, DefaultTurnSeconds, MinTurnSeconds, MaxTurnSeconds);
        if (room.Level < 0 || room.Level >= RuleSetCount)
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
        if (action.Kind >= CommandLimit || action.Kind == SeatToComputerKind)
        {
            error = "There is no such command.";
            return;
        }
        // The rules are the clients', but what can never be a command of the room's game is refused here: such an action
        // would still start the clock again, so a player could keep the table waiting without ever timing out.
        if (action.Kind >= RollKind && room.Level != BackgammonRuleSet)
        {
            error = "Only backgammon rolls dice or doubles.";
            return;
        }
        if (!PayloadFits(action.Kind, action.Payload))
        {
            error = "The command is not well formed.";
            return;
        }
        error = CheckSeatAction(ctx, room, member, action.Seat);
        // Every command answers what the table waits for (a move, a roll, the cube, giving up): the clock starts again.
        decisive = true;
    }

    /// <summary>Whether a payload has the shape its kind writes: a move's numbers parted by commas, nothing for the others.</summary>
    private static bool PayloadFits(uint kind, string? payload)
    {
        if (kind != 0)
        {
            return string.IsNullOrEmpty(payload);
        }
        if (string.IsNullOrEmpty(payload))
        {
            return false;
        }
        string[] parts = payload.Split(',');
        if (parts.Length < 2 || parts.Length > MaxMoveNumbers)
        {
            return false;
        }
        foreach (string part in parts)
        {
            if (part.Length == 0 || part.Length > 2)
            {
                return false;
            }
            foreach (char c in part)
            {
                if (c < '0' || c > '9')
                {
                    return false;
                }
            }
        }
        return true;
    }

    static partial void OnTurnTimedOut(ReducerContext ctx, Room room, RoomTurn turn, ref bool handled)
    {
        // Every client makes the default commands for whoever kept the table waiting, at this place in the log.
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
            var stats = ctx.Db.BoardGamesStats.Player.Find(member.Identity) ?? ctx.Db.BoardGamesStats.Insert(new BoardGamesStats { Player = member.Identity });
            stats.Games++;
            if (member.Identity == winner)
            {
                stats.Wins++;
            }
            ctx.Db.BoardGamesStats.Player.Update(stats);
        }
    }

    static partial void OnUserDeleted(ReducerContext ctx, User user)
    {
        ctx.Db.BoardGamesStats.Player.Delete(user.Identity);
    }
}
