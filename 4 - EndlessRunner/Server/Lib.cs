using SpacetimeDB;

/// <summary>
/// The server module of Endless Runner: the Gamebox base server (users and login, rooms with their members, the
/// poses of the players of a real time game; see the BaseServer folder of this project) plus the game itself.
/// Both declare the same partial class, so the tables, reducers and helpers of the base server are in reach here.
///
/// A race is run on the clients. Every client lays out the same track from the level and the seed of the room,
/// runs it on its own and sends the pose of its runner, which the others show as a ghost. What the runners share
/// is the coins of the track: a coin belongs to the runner who claims it first with <see cref="ClaimPiece"/>, and
/// the <see cref="CoinClaim"/> row tells every client that it is gone and who got it. A runner who is over the line
/// or out of hearts calls <see cref="FinishRun"/>; the base server ranks the runners by score when the last one is
/// done. The server does not know the levels of the game, so the host's client writes what matters of the track
/// into the options of the room (its length and the coins on it), and the server holds the runners to that.
///
/// After a change: publish the module, then generate the client bindings (Gamebox > Server in the Unity editor,
/// or <c>spacetime publish</c> and <c>spacetime generate</c> in the project folder, which read spacetime.json).
/// </summary>
public static partial class Module
{
    public const byte MaxRunners = 4;

    /// <summary>The most a piece of the track is worth: a gem, which counts as five coins.</summary>
    public const byte MaxPieceValue = 5;

    // The numbers of the client's RunnerGameManager: a meter is a point, a coin is ten, twice that with double coins.
    public const int PointsPerCoin = 10;
    public const int MaxCoinMultiplier = 2;

    public const int MaxLevels = 64;
    public const int MinTrackLength = 60;
    public const int MaxTrackLength = 100_000;
    public const int MaxTrackCoins = 1_000_000;

    /// <summary>What a runner can be over the finish line when the run ends: the track is measured, a frame is not.</summary>
    private const uint FinishMargin = 50;

    // ---------------------------------------------------------------------------------------------------
    // Tables
    // ---------------------------------------------------------------------------------------------------

    /// <summary>The race of a room that plays: the track as the host described it. Private: only reducers see it.</summary>
    [SpacetimeDB.Table(Accessor = "RunnerRace")]
    public partial struct RunnerRace
    {
        [SpacetimeDB.PrimaryKey]
        public ulong RoomId;

        public int Level;

        /// <summary>Meters to the finish line; 0 for the endless run.</summary>
        public uint Length;

        /// <summary>Coin value of the whole track; 0 when nobody can know it (the endless run).</summary>
        public uint TrackCoins;

        /// <summary>Coin value the runners claimed so far.</summary>
        public uint ClaimedCoins;
    }

    /// <summary>
    /// A coin or gem of the track that a runner took. The clients take the piece off their track when the row
    /// arrives, and the owner counts it.
    /// </summary>
    [SpacetimeDB.Table(Accessor = "CoinClaim", Public = true)]
    public partial struct CoinClaim
    {
        /// <summary>The room and the piece in one number (<see cref="ClaimKey"/>): a piece of a room is claimed once.</summary>
        [SpacetimeDB.PrimaryKey]
        public ulong Key;

        [SpacetimeDB.Index.BTree]
        public ulong RoomId;

        /// <summary>The id of the piece in the layout of the track, which is the same on every client.</summary>
        public uint Piece;

        public Identity Owner;

        public byte Seat;

        /// <summary>Coin value of the piece: 1 for a coin, 5 for a gem.</summary>
        public byte Value;
    }

    /// <summary>What a player did in races over time.</summary>
    [SpacetimeDB.Table(Accessor = "RunnerStats", Public = true)]
    public partial struct RunnerStats
    {
        [SpacetimeDB.PrimaryKey]
        public Identity Player;

        public uint Races;

        /// <summary>Races won against somebody.</summary>
        public uint Wins;

        /// <summary>Coin value collected in all races.</summary>
        public ulong Coins;

        /// <summary>The longest run in meters.</summary>
        public uint BestDistance;

        public long BestScore;
    }

    // ---------------------------------------------------------------------------------------------------
    // The track of a room
    // ---------------------------------------------------------------------------------------------------

    /// <summary>The track a room runs, as the host's client wrote it into the options of the room.</summary>
    private readonly struct TrackFacts
    {
        public readonly int Level, Length, Coins;

        public TrackFacts(string? options)
        {
            Level = Option(options, "level", -1, -1, int.MaxValue);
            Length = Option(options, "length", -1, -1, int.MaxValue);
            Coins = Option(options, "coins", -1, -1, int.MaxValue);
        }

        public bool Endless => Length == 0;

        public string? Error(int level)
        {
            if (level < 0 || level >= MaxLevels)
            {
                return "There is no such level.";
            }
            if (Level != level)
            {
                // Options written for another level would hold the runners to the wrong track.
                return "The room does not describe the track of its level.";
            }
            if (Length != 0 && (Length < MinTrackLength || Length > MaxTrackLength))
            {
                return $"A track is {MinTrackLength} to {MaxTrackLength} meters long, or endless.";
            }
            if (Coins < 0 || Coins > MaxTrackCoins || (Endless && Coins != 0))
            {
                return "The coins of the track do not add up.";
            }
            return null;
        }
    }

    // ---------------------------------------------------------------------------------------------------
    // Reducers
    // ---------------------------------------------------------------------------------------------------

    /// <summary>
    /// A runner touched a coin or gem. The first claim of a piece gets it; a later one is not an error, only late,
    /// and so is a claim that arrives when the race is over or the runner is out.
    /// </summary>
    [SpacetimeDB.Reducer]
    public static void ClaimPiece(ReducerContext ctx, uint piece, byte value)
    {
        if (value == 0 || value > MaxPieceValue)
        {
            throw new Exception($"A piece is worth 1 to {MaxPieceValue} coins.");
        }
        if (ctx.Db.RoomMember.Identity.Find(ctx.Sender) is not { Playing: true } member
            || ctx.Db.Room.Id.Find(member.RoomId) is not { State: RoomState.Playing } room
            || ctx.Db.RunnerRace.RoomId.Find(room.Id) is not { } race)
        {
            return;
        }
        var key = ClaimKey(room.Id, piece);
        if (ctx.Db.CoinClaim.Key.Find(key) is not null)
        {
            return;
        }
        if (race.TrackCoins > 0 && race.ClaimedCoins + value > race.TrackCoins)
        {
            throw new Exception("There are no more coins on this track.");
        }
        ctx.Db.CoinClaim.Insert(new CoinClaim
        {
            Key = key,
            RoomId = room.Id,
            Piece = piece,
            Owner = ctx.Sender,
            Seat = member.Seat,
            Value = value,
        });
        race.ClaimedCoins += value;
        ctx.Db.RunnerRace.RoomId.Update(race);
    }

    /// <summary>
    /// A runner is done: over the finish line, or out of hearts. The distance is kept for the records, the score
    /// is held to what the distance and the coins of the runner can be worth, and the run ends as the base server's
    /// <see cref="FinishPlaying"/> ends it: the race finishes when the last runner is done.
    /// </summary>
    [SpacetimeDB.Reducer]
    public static void FinishRun(ReducerContext ctx, uint distance, long score)
    {
        var member = RequirePlayingMember(ctx, out var room);
        if (ctx.Db.RunnerRace.RoomId.Find(room.Id) is { Length: > 0 } race)
        {
            distance = Math.Min(distance, race.Length + FinishMargin);
        }
        long coins = 0;
        foreach (var claim in ctx.Db.CoinClaim.RoomId.Filter(room.Id))
        {
            if (claim.Owner == member.Identity)
            {
                coins += claim.Value;
            }
        }
        score = Math.Clamp(score, 0, distance + coins * PointsPerCoin * MaxCoinMultiplier);

        var stats = StatsOf(ctx, member.Identity);
        if (distance > stats.BestDistance)
        {
            stats.BestDistance = distance;
            ctx.Db.RunnerStats.Player.Update(stats);
        }
        FinishPlaying(ctx, score);
    }

    // ---------------------------------------------------------------------------------------------------
    // Extension points of the base server
    // ---------------------------------------------------------------------------------------------------

    static partial void ConfigureRoom(ReducerContext ctx, ref Room room, ref string? error)
    {
        // Whoever is left runs on: a race does not end because somebody went home.
        room.MinPlayers = 1;
        room.MaxPlayers = Math.Clamp(room.MaxPlayers, (byte)2, MaxRunners);
        room.TurnSeconds = 0;
        error = new TrackFacts(room.Options).Error(room.Level);
    }

    static partial void OnRoomStarted(ReducerContext ctx, Room room)
    {
        var track = new TrackFacts(room.Options);
        ctx.Db.RunnerRace.Insert(new RunnerRace
        {
            RoomId = room.Id,
            Level = room.Level,
            Length = (uint)track.Length,
            TrackCoins = (uint)track.Coins,
        });
    }

    static partial void OnRoomFinished(ReducerContext ctx, Room room)
    {
        var claims = ctx.Db.CoinClaim.RoomId.Filter(room.Id).ToList();
        var members = MembersOf(ctx, room.Id);
        foreach (var member in members)
        {
            var stats = StatsOf(ctx, member.Identity);
            stats.Races++;
            // Winning takes somebody to beat.
            if (member.Place == 1 && members.Count > 1)
            {
                stats.Wins++;
            }
            foreach (var claim in claims)
            {
                if (claim.Owner == member.Identity)
                {
                    stats.Coins += claim.Value;
                }
            }
            stats.BestScore = Math.Max(stats.BestScore, member.Score);
            ctx.Db.RunnerStats.Player.Update(stats);
        }
    }

    static partial void OnRoomCleared(ReducerContext ctx, Room room)
    {
        ctx.Db.CoinClaim.RoomId.Delete(room.Id);
        ctx.Db.RunnerRace.RoomId.Delete(room.Id);
    }

    static partial void OnUserDeleted(ReducerContext ctx, User user)
    {
        ctx.Db.RunnerStats.Player.Delete(user.Identity);
    }

    // ---------------------------------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------------------------------

    /// <summary>The key of a claim: the room in the upper half, the piece in the lower one.</summary>
    public static ulong ClaimKey(ulong roomId, uint piece) => (roomId << 32) | piece;

    private static RunnerStats StatsOf(ReducerContext ctx, Identity player) =>
        ctx.Db.RunnerStats.Player.Find(player) ?? ctx.Db.RunnerStats.Insert(new RunnerStats { Player = player });
}
