using SpacetimeDB;

/// <summary>
/// The server module of Monopoly: the Gamebox base server (users and login, rooms with their members, the action
/// log and its clock; see the BaseServer folder of this project) plus what this file adds. Both declare the same
/// partial class, so the tables, reducers and helpers of the base server are in reach here.
///
/// An online match is played in step. Every client runs the rules of the game (the MonopolyMatch of the Unity
/// project) and sends what its player does as an action of the base server's log; the server puts the actions of
/// a room in one order and stamps each with a random number, which the clients seed the dice of the action with.
/// So this module knows no rules of Monopoly. It seats the table when a game starts (<see cref="MonopolySeat"/>:
/// the members of the room in seat order, then the computer players the options of the room ask for), checks who
/// may act for which seat (a member for their own, the host for the computer players, whose moves the host's
/// client works out), keeps the clock (when it runs out every client makes the default move for whoever kept the
/// table waiting), hands the seat of a player who leaves to the computer, and keeps statistics.
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

    public const byte SeatHuman = 0;
    public const byte SeatComputer = 1;

    // The numbers of the client's CommandKind enum the server has to tell apart: the commands from
    // FirstManagementCommand on manage property between moves, the last one is the server's own.
    public const uint FirstManagementCommand = 12;
    public const uint SeatToComputerCommand = 18;

    /// <summary>The names computer players take, in this order (the client's MatchSetup.BotNames).</summary>
    private static readonly string[] BotNames = { "Ada", "Max", "Lulu", "Otto", "Mia", "Leo", "Zoe", "Hugo" };

    // ---------------------------------------------------------------------------------------------------
    // Tables
    // ---------------------------------------------------------------------------------------------------

    /// <summary>
    /// A seat at the table of a room that plays: every client sets the match up from these rows, so all of them
    /// seat the same players in the same order with the same tokens.
    /// </summary>
    [SpacetimeDB.Table(Accessor = "MonopolySeat", Public = true)]
    public partial struct MonopolySeat
    {
        [SpacetimeDB.PrimaryKey]
        [SpacetimeDB.AutoInc]
        public ulong Id;

        [SpacetimeDB.Index.BTree]
        public ulong RoomId;

        /// <summary>Place in the order of play, from 0: the seat the actions of the log name.</summary>
        public byte Seat;

        /// <summary><see cref="SeatHuman"/> or <see cref="SeatComputer"/>.</summary>
        public byte Kind;

        /// <summary>The member who plays the seat; the identity of the module for a computer player.</summary>
        public Identity Player;

        public string Name;

        /// <summary>The token the seat moves, as its index in the client's list.</summary>
        public byte Token;

        /// <summary>How well the computer plays the seat (0 easy, 1 normal, 2 hard): from the start, or once its member left.</summary>
        public byte BotLevel;
    }

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
        // 0 plays without a clock; a clock shorter than the moves take to watch would play the game by itself.
        var seconds = Option(room.Options, "turn", DefaultTurnSeconds, 0, MaxTurnSeconds);
        room.TurnSeconds = (uint)(seconds > 0 ? Math.Max(seconds, MinTurnSeconds) : 0);
        if (room.Level < 0 || room.Level >= MaxModes)
        {
            error = "There is no such game mode.";
        }
    }

    static partial void OnRoomStarted(ReducerContext ctx, Room room)
    {
        var members = MembersOf(ctx, room.Id);
        var level = (byte)Option(room.Options, "botlevel", 1, 0, 2);
        // The computer players take the seats the members leave free.
        var computers = Math.Min(Option(room.Options, "bots", 0, 0, MaxComputerPlayers), MaxSeats - members.Count);
        var tokens = new HashSet<byte>();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        byte seat = 0;
        foreach (var member in members)
        {
            if (seat >= MaxSeats)
            {
                break;
            }
            var user = FindUser(ctx, member.Identity);
            var name = user?.Name ?? $"Player {seat + 1}";
            names.Add(name);
            ctx.Db.MonopolySeat.Insert(new MonopolySeat
            {
                RoomId = room.Id,
                Seat = seat,
                Kind = SeatHuman,
                Player = member.Identity,
                Name = name,
                // The avatar of the profile is the token a player likes to move; the next free one when it is taken.
                Token = TakeToken(tokens, (byte)((user?.Avatar ?? seat) % TokenCount)),
                BotLevel = level,
            });
            seat++;
        }
        for (var i = 0; i < computers; i++)
        {
            var name = BotNames.FirstOrDefault(candidate => !names.Contains(candidate)) ?? $"Computer {i + 1}";
            names.Add(name);
            ctx.Db.MonopolySeat.Insert(new MonopolySeat
            {
                RoomId = room.Id,
                Seat = seat,
                Kind = SeatComputer,
                Player = ctx.DatabaseIdentity,
                Name = name,
                Token = TakeToken(tokens, seat),
                BotLevel = level,
            });
            seat++;
        }
        if (room.TurnSeconds > 0)
        {
            // The clock of the action log: no seat, because only the clients know who the match waits for.
            BeginTurn(ctx, room, NoSeat);
        }
    }

    static partial void ValidateAction(ReducerContext ctx, Room room, RoomMember member, ref RoomAction action, ref bool decisive, ref string? error)
    {
        if (action.Kind >= SeatToComputerCommand)
        {
            error = "There is no such command.";
            return;
        }
        if (SeatAt(ctx, room.Id, action.Seat) is not { } seat)
        {
            error = "There is no such seat at the table.";
            return;
        }
        // A member plays their own seat; the host's client plays the computer players.
        var allowed = seat.Kind == SeatHuman ? seat.Player == member.Identity : room.Host == member.Identity;
        if (!allowed)
        {
            error = "This seat is not yours to play.";
            return;
        }
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
        if (room.State != RoomState.Playing)
        {
            return;
        }
        var seats = ctx.Db.MonopolySeat.RoomId.Filter(room.Id).ToList();
        foreach (var seat in seats)
        {
            if (seat.Kind != SeatHuman || seat.Player != member.Identity)
            {
                continue;
            }
            // The computer plays the seat on. The action tells every client at the same place in the log; the
            // host's client (the base server already passed the room on when the host left) makes its moves.
            var computer = seat;
            computer.Kind = SeatComputer;
            computer.Player = ctx.DatabaseIdentity;
            ctx.Db.MonopolySeat.Id.Update(computer);
            AppendAction(ctx, room, new RoomAction
            {
                Sender = ctx.DatabaseIdentity,
                Seat = seat.Seat,
                Kind = SeatToComputerCommand,
                Payload = seat.BotLevel.ToString(),
            });
            Log.Info($"Room {room.Id}: the computer plays on for {seat.Name}.");
            break;
        }
    }

    static partial void OnRoomFinished(ReducerContext ctx, Room room)
    {
        // The score of a member is the net worth their client reported.
        foreach (var member in MembersOf(ctx, room.Id))
        {
            var stats = ctx.Db.MonopolyStats.Player.Find(member.Identity) ?? ctx.Db.MonopolyStats.Insert(new MonopolyStats { Player = member.Identity });
            stats.Games++;
            if (member.Place == 1)
            {
                stats.Wins++;
            }
            stats.BestNetWorth = Math.Max(stats.BestNetWorth, member.Score);
            ctx.Db.MonopolyStats.Player.Update(stats);
        }
    }

    static partial void OnRoomCleared(ReducerContext ctx, Room room)
    {
        ctx.Db.MonopolySeat.RoomId.Delete(room.Id);
    }

    static partial void OnUserDeleted(ReducerContext ctx, User user)
    {
        ctx.Db.MonopolyStats.Player.Delete(user.Identity);
    }

    // ---------------------------------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------------------------------

    private static MonopolySeat? SeatAt(ReducerContext ctx, ulong roomId, byte seat)
    {
        foreach (var row in ctx.Db.MonopolySeat.RoomId.Filter(roomId))
        {
            if (row.Seat == seat)
            {
                return row;
            }
        }
        return null;
    }

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
