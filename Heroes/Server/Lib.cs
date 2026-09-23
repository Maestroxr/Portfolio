using SpacetimeDB;

/// <summary>
/// The server module of Heroes: the Gamebox base server (users and login, rooms with their members, the action log
/// and its clock; see the BaseServer folder of this project) plus what this file adds. Both declare the same partial
/// class, so the tables, reducers and helpers of the base server are in reach here.
///
/// A scenario is played in step. Every client lays out the same map from the seed of the room and runs the same rules
/// (the HeroesGame of the Unity project); what a player does is sent as an action of the log, and the server puts the
/// actions of a room in one order and stamps each with a random number that the clients seed the chances of that
/// action with. So this module knows no rules of Heroes. It seats the table when a scenario starts
/// (<see cref="HeroesSeat"/>: the members of the room in seat order with the faction each chose, then the computer
/// players the options of the room ask for), checks who may act for which seat (a member for their own, the host for
/// the computer players, whose moves the host's client works out), keeps the clock (when it runs out every client
/// makes the default move for whoever kept the game waiting), hands the seat of a player who leaves to the computer,
/// and keeps statistics.
///
/// After a change: publish the module, then generate the client bindings (Gamebox > Server in the Unity editor, or
/// <c>spacetime publish</c> and <c>spacetime generate</c> in the project folder, which read spacetime.json).
/// </summary>
public static partial class Module
{
    public const byte MaxSeats = 4;
    public const int MaxComputerPlayers = 3;
    /// <summary>The factions a seat can play (the client's Faction enum, without the neutral one).</summary>
    public const byte FactionCount = 3;
    /// <summary>Scenarios are levels of the room; -1 is a map made up from the room's seed.</summary>
    public const int MaxScenarios = 32;

    public const int DefaultTurnSeconds = 120;
    public const int MinTurnSeconds = 20;
    public const int MaxTurnSeconds = 900;

    public const byte SeatHuman = 0;
    public const byte SeatComputer = 1;

    /// <summary>The numbers of the client's CommandKind enum: the last one is the server's own.</summary>
    public const uint SeatToComputerCommand = 40;

    /// <summary>
    /// The seat the wandering armies of the wilds act under. They belong to no player, so in a battle against them
    /// the host's client moves them, as it moves the computer players.
    /// </summary>
    public const byte WildsSeat = 254;

    /// <summary>The names computer players take, in this order.</summary>
    private static readonly string[] BotNames = { "Aldric", "Mareth", "Ysolde", "Korrin", "Brannoc", "Selka" };

    // ---------------------------------------------------------------------------------------------------
    // Tables
    // ---------------------------------------------------------------------------------------------------

    /// <summary>
    /// A seat in a room that plays: every client seats the same players in the same order with the same factions,
    /// so the map the generator lays out from the room's seed comes out the same for all of them.
    /// </summary>
    [SpacetimeDB.Table(Accessor = "HeroesSeat", Public = true)]
    public partial struct HeroesSeat
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

        /// <summary>The faction the seat leads, as its index in the client's list.</summary>
        public byte Faction;

        /// <summary>How well the computer plays the seat (0 easy, 1 normal, 2 hard): from the start, or once its member left.</summary>
        public byte BotLevel;
    }

    /// <summary>What a player did in online games over time.</summary>
    [SpacetimeDB.Table(Accessor = "HeroesStats", Public = true)]
    public partial struct HeroesStats
    {
        [SpacetimeDB.PrimaryKey]
        public Identity Player;

        public uint Games;
        public uint Wins;
        /// <summary>The largest realm the player ever held, as their client counted it.</summary>
        public long BestRealm;
    }

    // ---------------------------------------------------------------------------------------------------
    // Extension points of the base server
    // ---------------------------------------------------------------------------------------------------

    static partial void ConfigureRoom(ReducerContext ctx, ref Room room, ref string? error)
    {
        room.MinPlayers = 2;
        room.MaxPlayers = Math.Clamp(room.MaxPlayers, (byte)2, MaxSeats);
        // 0 plays without a clock; a clock shorter than a turn of this game takes would play it by itself.
        var seconds = Option(room.Options, "turn", DefaultTurnSeconds, 0, MaxTurnSeconds);
        room.TurnSeconds = (uint)(seconds > 0 ? Math.Max(seconds, MinTurnSeconds) : 0);
        if (room.Level >= MaxScenarios)
        {
            error = "There is no such scenario.";
        }
    }

    static partial void OnRoomStarted(ReducerContext ctx, Room room)
    {
        var members = MembersOf(ctx, room.Id);
        var level = (byte)Option(room.Options, "botlevel", 1, 0, 2);
        // The computer players take the seats the members leave free.
        var computers = Math.Min(Option(room.Options, "bots", 0, 0, MaxComputerPlayers), MaxSeats - members.Count);
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
            ctx.Db.HeroesSeat.Insert(new HeroesSeat
            {
                RoomId = room.Id,
                Seat = seat,
                Kind = SeatHuman,
                Player = member.Identity,
                Name = name,
                // Every seat leads the next faction along, so a table is never all of one kind.
                Faction = (byte)(seat % FactionCount),
                BotLevel = level,
            });
            seat++;
        }
        for (var i = 0; i < computers; i++)
        {
            var name = BotNames.FirstOrDefault(candidate => !names.Contains(candidate)) ?? $"Computer {i + 1}";
            names.Add(name);
            ctx.Db.HeroesSeat.Insert(new HeroesSeat
            {
                RoomId = room.Id,
                Seat = seat,
                Kind = SeatComputer,
                Player = ctx.DatabaseIdentity,
                Name = name,
                Faction = (byte)(seat % FactionCount),
                BotLevel = level,
            });
            seat++;
        }
        if (room.TurnSeconds > 0)
        {
            // The clock of the action log: no seat, because only the clients know who the game waits for.
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
        if (action.Seat == WildsSeat)
        {
            // The wandering armies of a battle: the host's client works their moves out for everybody.
            if (room.Host != member.Identity)
            {
                error = "Only the host moves the armies of the wilds.";
                return;
            }
            decisive = true;
            return;
        }
        if (SeatAt(ctx, room.Id, action.Seat) is not { } seat)
        {
            error = "There is no such seat in this game.";
            return;
        }
        // A member plays their own seat; the host's client plays the computer players.
        var allowed = seat.Kind == SeatHuman ? seat.Player == member.Identity : room.Host == member.Identity;
        if (!allowed)
        {
            error = "This seat is not yours to play.";
            return;
        }
        // Every command of this game answers what it waits for, so the clock starts again after each of them.
        decisive = true;
    }

    static partial void OnTurnTimedOut(ReducerContext ctx, Room room, RoomTurn turn, ref bool handled)
    {
        // Every client makes the default move for whoever kept the game waiting, at this place in the log.
        AppendTimeout(ctx, room);
        handled = true;
    }

    static partial void OnMemberLeft(ReducerContext ctx, Room room, RoomMember member)
    {
        if (room.State != RoomState.Playing)
        {
            return;
        }
        foreach (var seat in ctx.Db.HeroesSeat.RoomId.Filter(room.Id).ToList())
        {
            if (seat.Kind != SeatHuman || seat.Player != member.Identity)
            {
                continue;
            }
            // The computer leads the realm on. The action tells every client at the same place in the log; the
            // host's client (the base server already passed the room on when the host left) makes its moves.
            var computer = seat;
            computer.Kind = SeatComputer;
            computer.Player = ctx.DatabaseIdentity;
            ctx.Db.HeroesSeat.Id.Update(computer);
            AppendAction(ctx, room, new RoomAction
            {
                Sender = ctx.DatabaseIdentity,
                Seat = seat.Seat,
                Kind = SeatToComputerCommand,
                Payload = $"{SeatToComputerCommand},{seat.Seat},{seat.BotLevel},0,0,0,0",
            });
            Log.Info($"Room {room.Id}: the computer leads {seat.Name}'s realm on.");
            break;
        }
    }

    static partial void OnRoomFinished(ReducerContext ctx, Room room)
    {
        // The score of a member is what their client reported their realm was worth.
        foreach (var member in MembersOf(ctx, room.Id))
        {
            var stats = ctx.Db.HeroesStats.Player.Find(member.Identity) ?? ctx.Db.HeroesStats.Insert(new HeroesStats { Player = member.Identity });
            stats.Games++;
            if (member.Place == 1)
            {
                stats.Wins++;
            }
            stats.BestRealm = Math.Max(stats.BestRealm, member.Score);
            ctx.Db.HeroesStats.Player.Update(stats);
        }
    }

    static partial void OnRoomCleared(ReducerContext ctx, Room room)
    {
        ctx.Db.HeroesSeat.RoomId.Delete(room.Id);
    }

    static partial void OnUserDeleted(ReducerContext ctx, User user)
    {
        ctx.Db.HeroesStats.Player.Delete(user.Identity);
    }

    // ---------------------------------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------------------------------

    private static HeroesSeat? SeatAt(ReducerContext ctx, ulong roomId, byte seat)
    {
        foreach (var row in ctx.Db.HeroesSeat.RoomId.Filter(roomId))
        {
            if (row.Seat == seat)
            {
                return row;
            }
        }
        return null;
    }
}
