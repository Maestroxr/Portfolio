using SpacetimeDB;

/// <summary>
/// The server module of Monopoly: the Gamebox base server (users and login, rooms with their members, the action
/// log and its clock, the seats of a table; see the BaseServer folder of this project) plus what this file adds. Both
/// declare the same partial class, so the tables, reducers and helpers of the base server are in reach here.
///
/// An online match is played in step. Every client runs the rules of the game (the MonopolyMatch of the Unity
/// project) and sends what its player does as an action of the base server's log; the server puts the actions of
/// a room in one order and stamps each with a random number, which the clients seed the dice of the action with.
/// So this module knows no rules of Monopoly. It seats the table when a game starts (the base server's
/// <see cref="RoomSeat"/>: the members of the room in seat order with the token each likes, then the computer players
/// the options of the room ask for), checks who may act for which seat (a member for their own, the host for the
/// computer players, whose moves the host's client works out), keeps the clock (when it runs out every client makes
/// the default move for whoever kept the table waiting), hands the seat of a player who leaves to the computer, and
/// keeps statistics.
///
/// After a change: publish the module, then generate the client bindings (Gamebox > Server in the Unity editor,
/// or <c>spacetime publish</c> and <c>spacetime generate</c> in the project folder, which read spacetime.json).
/// </summary>
public static partial class Module
{
    public const byte MaxSeats = 4;
    public const int MaxComputerPlayers = 2;
    /// <summary>Tokens a player can move (the client's MonopolyStyle.TokenNames).</summary>
    public const byte TokenCount = 8;
    /// <summary>Game modes are levels of the room; the client knows which ones there are.</summary>
    public const int MaxModes = 16;

    public const int DefaultTurnSeconds = 45;
    public const int MinTurnSeconds = 10;
    public const int MaxTurnSeconds = 600;

    // The numbers of the client's CommandKind enum the server has to tell apart: the commands from
    // FirstManagementCommand on manage property between moves. The rules' own SeatToComputer (18) only ever arrives as
    // the base server's SeatToComputerAction, so everything from it on is no command of a player.
    public const uint FirstManagementCommand = 12;
    public const uint CommandLimit = 18;

    /// <summary>The names computer players take, in this order (the client's MatchSetup.BotNames).</summary>
    private static readonly string[] BotNames = { "Ada", "Max", "Lulu", "Otto", "Mia", "Leo", "Zoe", "Hugo" };

    // ---------------------------------------------------------------------------------------------------
    // Tables
    // ---------------------------------------------------------------------------------------------------

    /// <summary>What a player did in online matches over time.</summary>
    [SpacetimeDB.Table(Accessor = "MonopolyStats", Public = true)]
    public partial struct MonopolyStats
    {
        [SpacetimeDB.PrimaryKey]
        public Identity Player;

        public uint Games;
        public uint Wins;
        public long BestNetWorth;
    }

    // ---------------------------------------------------------------------------------------------------
    // Extension points of the base server
    // ---------------------------------------------------------------------------------------------------

    static partial void ConfigureRoom(ReducerContext ctx, ref Room room, ref string? error)
    {
        room.MinPlayers = 2;
        room.MaxPlayers = Math.Clamp(room.MaxPlayers, (byte)2, MaxSeats);
        room.TurnSeconds = ClockOption(room.Options, DefaultTurnSeconds, MinTurnSeconds, MaxTurnSeconds);
        if (room.Level < 0 || room.Level >= MaxModes)
        {
            error = "There is no such game mode.";
        }
    }

    static partial void OnRoomStarted(ReducerContext ctx, Room room)
    {
        // The avatar of the profile is the token a player likes to move; the next free one when it is taken.
        var tokens = new HashSet<byte>();
        SeatTable(ctx, room, MaxSeats, MaxComputerPlayers, BotNames,
            (seat, user) => TakeToken(tokens, (byte)((user?.Avatar ?? seat) % TokenCount)));
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
        // Building, mortgaging and trading do not answer what the match waits for: the clock runs on.
        decisive = action.Kind < FirstManagementCommand;
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
        // The computer plays the seats of those who left: the match goes on while one member is still at the table.
        keep = membersLeft > 0 && HasComputerSeat(ctx, room.Id);
    }

    static partial void OnRoomFinished(ReducerContext ctx, Room room)
    {
        // The members take the places of their seats at the table, a computer player that won the match before them all,
        // and their scores go back to the net worth their clients reported.
        var winner = RankTable(ctx, room);
        foreach (var member in MembersOf(ctx, room.Id))
        {
            var stats = ctx.Db.MonopolyStats.Player.Find(member.Identity) ?? ctx.Db.MonopolyStats.Insert(new MonopolyStats { Player = member.Identity });
            stats.Games++;
            if (member.Identity == winner)
            {
                stats.Wins++;
            }
            stats.BestNetWorth = Math.Max(stats.BestNetWorth, member.Score);
            ctx.Db.MonopolyStats.Player.Update(stats);
        }
    }

    static partial void OnUserDeleted(ReducerContext ctx, User user)
    {
        ctx.Db.MonopolyStats.Player.Delete(user.Identity);
    }

    // ---------------------------------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------------------------------

    /// <summary>The wanted token, or the next one nobody at the table has.</summary>
    private static byte TakeToken(HashSet<byte> taken, byte wanted)
    {
        var token = (byte)(wanted % TokenCount);
        while (taken.Contains(token) && taken.Count < TokenCount)
        {
            token = (byte)((token + 1) % TokenCount);
        }
        taken.Add(token);
        return token;
    }
}
