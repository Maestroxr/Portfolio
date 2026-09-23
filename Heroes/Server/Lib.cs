using SpacetimeDB;

/// <summary>
/// The server module of Heroes: the Gamebox base server (users and login, rooms with their members, the action log
/// and its clock, the seats of a table; see the BaseServer folder of this project) plus what this file adds. Both
/// declare the same partial class, so the tables, reducers and helpers of the base server are in reach here.
///
/// A scenario is played in step. Every client lays out the same map from the seed of the room and runs the same rules
/// (the HeroesGame of the Unity project); what a player does is sent as an action of the log, and the server puts the
/// actions of a room in one order and stamps each with a random number that the clients seed the chances of that
/// action with. So this module knows no rules of Heroes. It seats the table when a scenario starts (the base server's
/// <see cref="RoomSeat"/>: the members of the room in seat order, each leading the next faction along, then the
/// computer players the options of the room ask for), checks who may act for which seat (a member for their own, the
/// host for the computer players and for the wandering armies of the wilds, whose moves the host's client works out),
/// keeps the clock (when it runs out every client makes the default move for whoever kept the game waiting), hands the
/// seat of a player who leaves to the computer, and keeps statistics.
///
/// After a change: publish the module, then generate the client bindings (Gamebox > Server in the Unity editor, or
/// <c>spacetime publish</c> and <c>spacetime generate</c> in the project folder, which read spacetime.json).
/// </summary>
public static partial class Module
{
    public const byte MaxSeats = 4;
    public const int MaxComputerPlayers = 3;
    /// <summary>The factions a seat can lead (the client's Faction enum, without the neutral one).</summary>
    public const byte FactionCount = 3;
    /// <summary>Scenarios are levels of the room; -1 is a map made up from the room's seed.</summary>
    public const int MaxScenarios = 32;

    public const int DefaultTurnSeconds = 120;
    public const int MinTurnSeconds = 20;
    public const int MaxTurnSeconds = 900;

    // The numbers of the client's CommandKind enum (Model/Commands.cs): the commands on the map run from 0 to
    // LastMapCommand, those of a battle from FirstBattleCommand to LastBattleCommand, and nothing else is a command of
    // a player. The rules' own SeatToComputer (40) only ever arrives as the base server's SeatToComputerAction. The
    // client's LockstepGame keeps the same list of the commands that restart the clock (RestartsClock).
    public const uint EndTurnCommand = 1;
    public const uint ChooseCommand = 7;
    public const uint LastMapCommand = 10;
    public const uint FirstBattleCommand = 20;
    public const uint LastBattleCommand = 26;

    /// <summary>The names computer players take, in this order.</summary>
    private static readonly string[] BotNames = { "Aldric", "Mareth", "Ysolde", "Korrin", "Brannoc", "Selka" };

    // ---------------------------------------------------------------------------------------------------
    // Tables
    // ---------------------------------------------------------------------------------------------------

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
        room.TurnSeconds = ClockOption(room.Options, DefaultTurnSeconds, MinTurnSeconds, MaxTurnSeconds);
        if (room.Level >= MaxScenarios)
        {
            error = "There is no such scenario.";
        }
    }

    static partial void OnRoomStarted(ReducerContext ctx, Room room)
    {
        // Every seat leads the next faction along, so a table is never all of one kind.
        SeatTable(ctx, room, MaxSeats, MaxComputerPlayers, BotNames, (seat, user) => (byte)(seat % FactionCount));
        StartActionClock(ctx, room);
    }

    static partial void ValidateAction(ReducerContext ctx, Room room, RoomMember member, ref RoomAction action, ref bool decisive, ref string? error)
    {
        // A kind the client does not know changes nothing in the game, and must not keep the clock from running out.
        var battle = action.Kind >= FirstBattleCommand && action.Kind <= LastBattleCommand;
        if (action.Kind > LastMapCommand && !battle)
        {
            error = "There is no such command.";
            return;
        }
        // The wandering armies of the wilds belong to nobody and only ever fight: in a battle against them the host's
        // client moves them.
        if (action.Seat == WorldSeat && !battle)
        {
            error = "The wilds only fight.";
            return;
        }
        error = CheckSeatAction(ctx, room, member, action.Seat, worldSeat: true);
        if (error != null)
        {
            return;
        }
        // The clock is a clock of turns: what a player does on the map within a turn does not start it again, ending
        // the turn, answering a choice and every move in a battle do. The host's client never runs out of time for the
        // computer players and the wilds, which may take a while to watch.
        var computer = action.Seat == WorldSeat || SeatAt(ctx, room.Id, action.Seat) is { Kind: SeatComputer };
        decisive = computer || action.Kind == EndTurnCommand || action.Kind == ChooseCommand || battle;
    }

    static partial void OnTurnTimedOut(ReducerContext ctx, Room room, RoomTurn turn, ref bool handled)
    {
        // Every client makes the default move for whoever kept the game waiting, at this place in the log.
        AppendTimeout(ctx, room);
        handled = true;
    }

    static partial void OnMemberLeft(ReducerContext ctx, Room room, RoomMember member)
    {
        // The computer leads the realm on.
        HandSeatToComputer(ctx, room, member);
    }

    static partial void KeepPlaying(ReducerContext ctx, Room room, int membersLeft, ref bool keep)
    {
        // The computer leads the realms of those who left: the scenario goes on while one member is still at the table.
        keep = membersLeft > 0 && HasComputerSeat(ctx, room.Id);
    }

    static partial void OnRoomFinished(ReducerContext ctx, Room room)
    {
        // The members take the places of their realms at the table, a computer player that won the scenario before them
        // all, and their scores go back to what their realms were worth, which is what the standings show.
        var winner = RankTable(ctx, room);
        foreach (var member in MembersOf(ctx, room.Id))
        {
            var stats = ctx.Db.HeroesStats.Player.Find(member.Identity) ?? ctx.Db.HeroesStats.Insert(new HeroesStats { Player = member.Identity });
            stats.Games++;
            if (member.Identity == winner)
            {
                stats.Wins++;
            }
            stats.BestRealm = Math.Max(stats.BestRealm, member.Score);
            ctx.Db.HeroesStats.Player.Update(stats);
        }
    }

    static partial void OnUserDeleted(ReducerContext ctx, User user)
    {
        ctx.Db.HeroesStats.Player.Delete(user.Identity);
    }
}
