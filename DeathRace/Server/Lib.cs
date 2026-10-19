using Portfolio.DeathRace;
using SpacetimeDB;

/// <summary>
/// The server module of Death Race: the Gamebox base server (users and login, rooms with their members, the action log
/// and the poses of the players of a real time game; see the BaseServer folder of this project) plus the race itself.
/// Both declare the same partial class, so the tables, reducers and helpers of the base server are in reach here.
///
/// A race is run on the clients, like the Endless Runner's: every device builds the room's track (its level) from the
/// same seed, drives its own cars and sends their poses, which the others show as remote cars. What crosses between
/// the devices goes through here:
/// <list type="bullet">
/// <item>Mines, rockets and wrecks are actions of the room's log (kinds in <see cref="OnlineRaceRules"/>, checked by
/// <c>ValidateAction</c>), so every device lays every mine and launches every rocket. A wreck is judged by the device of
/// the car that was wrecked; it names the killer, who is credited here (<see cref="RaceRacer.Kills"/>).</item>
/// <item>A pickup, and a mine that a car drove into, belong to the first device that claims them
/// (<see cref="ClaimPickup"/>, <see cref="ClaimMine"/>); the <see cref="RaceClaim"/> row tells every device.</item>
/// <item>A racer who crossed the line for the last time, was wrecked, or was still running when the race ended reports
/// it with <see cref="FinishRace"/>; the result becomes a score (<see cref="OnlineRaceRules.Score"/>) and the base server
/// ranks the room by it when the last racer is done.</item>
/// </list>
/// The server does not know the tracks of the game, so the host's device writes the track's laps and length into the
/// options of the room, and the server holds the results to them. Several people may race from one device (the
/// companions of the base server): its connection acts for each of them, so the reducers name the seat they are about
/// (<see cref="NoSeat"/> is the caller's own).
///
/// After a change: publish the module, then generate the client bindings (Gamebox > Server in the Unity editor, or
/// <c>spacetime publish</c> and <c>spacetime generate</c> in the project folder, which read spacetime.json).
/// </summary>
public static partial class Module
{
    public const byte MaxRacers = 4;

    // ---------------------------------------------------------------------------------------------------
    // Tables
    // ---------------------------------------------------------------------------------------------------

    /// <summary>The race of a room that plays, as the host's device described it. Private: only reducers read it.</summary>
    [SpacetimeDB.Table(Accessor = "RaceTrack")]
    public partial struct RaceTrack
    {
        [SpacetimeDB.PrimaryKey]
        public ulong RoomId;

        public int Track;
        public int Laps;

        /// <summary>The length of a lap, in metres.</summary>
        public int Length;

        public int Car;
    }

    /// <summary>
    /// One racer of a running race: how their race went as far as the server knows. Every device reads the rows of its
    /// room for the standings (the kills, the times, who wrecked whom).
    /// </summary>
    [SpacetimeDB.Table(Accessor = "RaceRacer", Public = true)]
    public partial struct RaceRacer
    {
        [SpacetimeDB.PrimaryKey]
        public Identity Player;

        [SpacetimeDB.Index.BTree]
        public ulong RoomId;

        public byte Seat;

        /// <summary>Reported with <see cref="FinishRace"/>: the racer is done.</summary>
        public bool Done;

        public bool Finished;
        public bool Wrecked;

        /// <summary>The finishing time (or the time of the wreck, or of the end), in milliseconds.</summary>
        public uint TimeMs;

        /// <summary>How far the racer got, in centimetres along the track.</summary>
        public uint ProgressCm;

        public uint BestLapMs;

        /// <summary>The cars this racer wrecked, as their devices said.</summary>
        public byte Kills;

        /// <summary>The seat of the racer who wrecked this one, or <see cref="NoSeat"/>.</summary>
        public byte KilledBy;

        /// <summary>The score of the result (<see cref="OnlineRaceRules.Score"/>), 0 until <see cref="Done"/>.</summary>
        public long Score;
    }

    /// <summary>
    /// A pickup or a mine of a race that a device claimed first (<see cref="OnlineRaceRules.ClaimKey"/>). The devices take
    /// the pickup off the road, or blow the mine up, when the row arrives.
    /// </summary>
    [SpacetimeDB.Table(Accessor = "RaceClaim", Public = true)]
    public partial struct RaceClaim
    {
        [SpacetimeDB.PrimaryKey]
        public ulong Key;

        [SpacetimeDB.Index.BTree]
        public ulong RoomId;

        /// <summary><see cref="OnlineRaceRules.PickupClaim"/> or <see cref="OnlineRaceRules.MineClaim"/>.</summary>
        public byte Kind;

        /// <summary>The pickup and its generation, or the mine (<see cref="OnlineRaceRules.PickupItem"/>, <see cref="OnlineRaceRules.MineItem"/>).</summary>
        public uint Item;

        /// <summary>The seat of the car that got it.</summary>
        public byte Seat;

        public Identity Owner;
    }

    /// <summary>What a player did in online races over time.</summary>
    [SpacetimeDB.Table(Accessor = "DeathRaceStats", Public = true)]
    public partial struct DeathRaceStats
    {
        [SpacetimeDB.PrimaryKey]
        public Identity Player;

        public uint Races;

        /// <summary>Races won against somebody.</summary>
        public uint Wins;

        /// <summary>Races driven to the finish line.</summary>
        public uint Finishes;

        /// <summary>Cars wrecked.</summary>
        public uint Kills;

        /// <summary>Times wrecked.</summary>
        public uint Wrecks;

        /// <summary>The fastest lap ever driven online, in milliseconds (0 = none yet).</summary>
        public uint BestLapMs;
    }

    // ---------------------------------------------------------------------------------------------------
    // The race of a room
    // ---------------------------------------------------------------------------------------------------

    /// <summary>The race a room plays, as the host's device wrote it into the options of the room.</summary>
    private readonly struct RaceFacts
    {
        public readonly int Laps, TrackLaps, Length, Car;

        public RaceFacts(string? options)
        {
            Laps = Option(options, OnlineRaceRules.LapsOption, 0, 0, OnlineRaceRules.MaxLaps);
            TrackLaps = Option(options, OnlineRaceRules.TrackLapsOption, -1, -1, int.MaxValue);
            Length = Option(options, OnlineRaceRules.LengthOption, -1, -1, int.MaxValue);
            Car = Option(options, OnlineRaceRules.CarOption, -1, -1, int.MaxValue);
        }

        public int RaceLaps => OnlineRaceRules.LapsOf(Laps, TrackLaps);

        public string? Error(int level)
        {
            if (level < 0 || level >= OnlineRaceRules.MaxTracks)
            {
                return "There is no such track.";
            }
            if (TrackLaps < OnlineRaceRules.MinLaps || TrackLaps > OnlineRaceRules.MaxLaps)
            {
                return "The room does not say how many laps its track has.";
            }
            if (Length < OnlineRaceRules.MinLength || Length > OnlineRaceRules.MaxLength)
            {
                return $"A lap is {OnlineRaceRules.MinLength} to {OnlineRaceRules.MaxLength} metres long.";
            }
            if (Car < 0 || Car >= OnlineRaceRules.CarCount)
            {
                return "There is no such car.";
            }
            return null;
        }
    }

    // ---------------------------------------------------------------------------------------------------
    // Reducers
    // ---------------------------------------------------------------------------------------------------

    /// <summary>
    /// A car at the caller's device drove over a pickup: the car at <paramref name="seat"/> (<see cref="NoSeat"/> for the
    /// caller's own) and the pickup's <paramref name="index"/> in the race, taken <paramref name="generation"/> times before.
    /// The first claim gets it; a later one is not an error, only late, and so is one that arrives after the race.
    /// </summary>
    [SpacetimeDB.Reducer]
    public static void ClaimPickup(ReducerContext ctx, byte seat, uint index, uint generation)
    {
        if (index > 0xFFF || generation > 0xFFFF)
        {
            throw new Exception("There is no such pickup.");
        }
        Claim(ctx, seat, OnlineRaceRules.PickupClaim, OnlineRaceRules.PickupItem((int)index, (int)generation));
    }

    /// <summary>
    /// A car at the caller's device set off a mine: the mine numbered <paramref name="mineId"/> of the car at
    /// <paramref name="ownerSeat"/>. The first claim is the one that counts; every device blows its copy up.
    /// </summary>
    [SpacetimeDB.Reducer]
    public static void ClaimMine(ReducerContext ctx, byte seat, byte ownerSeat, uint mineId)
    {
        if (mineId > 0xFFFF)
        {
            throw new Exception("There is no such mine.");
        }
        Claim(ctx, seat, OnlineRaceRules.MineClaim, OnlineRaceRules.MineItem(ownerSeat, (int)mineId));
    }

    /// <summary>
    /// The racer at <paramref name="seat"/> of the caller's room (<see cref="NoSeat"/> for the caller's own) is done: over
    /// the line for the last time, wrecked, or still running when the race ended. The result is held to what the track
    /// allows (nobody laps faster than <see cref="OnlineRaceRules.FastestSpeed"/>, nobody covers more than the race),
    /// becomes the racer's score, and the race ends as the base server's <see cref="FinishPlaying"/> ends it: when the
    /// last racer is done.
    /// </summary>
    [SpacetimeDB.Reducer]
    public static void FinishRace(ReducerContext ctx, byte seat, bool finished, bool wrecked, uint timeMs, uint progressCm, uint bestLapMs)
    {
        var member = RequirePlayingActor(ctx, seat, out var room);
        if (ctx.Db.RaceTrack.RoomId.Find(room.Id) is not { } track || ctx.Db.RaceRacer.Player.Find(member.Identity) is not { } racer)
        {
            throw new Exception("This room runs no race.");
        }
        finished &= !wrecked;
        long fastest = OnlineRaceRules.FastestTimeMs(track.Laps, track.Length);
        if (finished && timeMs < fastest)
        {
            throw new Exception("Nobody drives that fast.");
        }
        long most = OnlineRaceRules.MostProgressCm(track.Laps, track.Length);
        progressCm = (uint)Math.Min(progressCm, most);
        if (bestLapMs > 0 && bestLapMs < OnlineRaceRules.FastestTimeMs(1, track.Length))
        {
            bestLapMs = 0;
        }
        racer.Done = true;
        racer.Finished = finished;
        racer.Wrecked = wrecked;
        racer.TimeMs = timeMs;
        racer.ProgressCm = progressCm;
        racer.BestLapMs = bestLapMs;
        racer.Score = OnlineRaceRules.Score(finished, wrecked, timeMs, progressCm);
        ctx.Db.RaceRacer.Player.Update(racer);
        FinishPlaying(ctx, member.Seat, racer.Score);
    }

    // ---------------------------------------------------------------------------------------------------
    // Extension points of the base server
    // ---------------------------------------------------------------------------------------------------

    static partial void ConfigureRoom(ReducerContext ctx, ref Room room, ref string? error)
    {
        // Whoever is left races on: a race does not end because somebody went home.
        room.MinPlayers = 1;
        room.MaxPlayers = Math.Clamp(room.MaxPlayers, (byte)2, MaxRacers);
        room.TurnSeconds = 0;
        error = new RaceFacts(room.Options).Error(room.Level);
    }

    static partial void OnRoomStarted(ReducerContext ctx, Room room)
    {
        var facts = new RaceFacts(room.Options);
        ctx.Db.RaceTrack.Insert(new RaceTrack
        {
            RoomId = room.Id,
            Track = room.Level,
            Laps = facts.RaceLaps,
            Length = facts.Length,
            Car = facts.Car,
        });
        foreach (var member in MembersOf(ctx, room.Id))
        {
            ctx.Db.RaceRacer.Player.Delete(member.Identity);
            ctx.Db.RaceRacer.Insert(new RaceRacer
            {
                Player = member.Identity,
                RoomId = room.Id,
                Seat = member.Seat,
                KilledBy = NoSeat,
            });
        }
    }

    /// <summary>
    /// The actions of a race: a mine, a rocket or a wreck of a car the sender drives (their own seat or a companion's), with
    /// a payload that reads. A wreck credits the killer it names with a kill and marks the wrecked racer.
    /// </summary>
    static partial void ValidateAction(ReducerContext ctx, Room room, RoomMember member, ref RoomAction action, ref bool decisive, ref string? error)
    {
        decisive = false;
        if (action.Seat == NoSeat)
        {
            action.Seat = member.Seat;
        }
        if (action.Seat != member.Seat && (MemberAt(ctx, room.Id, action.Seat) is not { } other || DeviceOf(ctx, other.Identity) != ctx.Sender))
        {
            error = "This car is not yours to drive.";
            return;
        }
        switch (action.Kind)
        {
            case OnlineRaceRules.MineAction:
                if (!OnlineRaceRules.TryDecodeMine(action.Payload, out _))
                {
                    error = "This mine does not read.";
                }
                break;
            case OnlineRaceRules.RocketAction:
                if (!OnlineRaceRules.TryDecodeRocket(action.Payload, out _))
                {
                    error = "This rocket does not read.";
                }
                break;
            case OnlineRaceRules.WreckAction:
                if (!OnlineRaceRules.TryDecodeWreck(action.Payload, out int killer))
                {
                    error = "This wreck does not read.";
                    return;
                }
                CreditWreck(ctx, room, action.Seat, killer);
                break;
            default:
                error = "A race has no such action.";
                break;
        }
    }

    /// <summary>
    /// Scores come from <see cref="FinishRace"/>, which works them out from the result: a final score has to be the one it
    /// wrote, and a score reported while the race runs can only say how far the racer got.
    /// </summary>
    static partial void ValidateScore(ReducerContext ctx, Room room, RoomMember member, bool final, ref long score, ref string? error)
    {
        if (!final)
        {
            score = Math.Clamp(score, 0, OnlineRaceRules.MostRunningScore);
            return;
        }
        if (ctx.Db.RaceRacer.Player.Find(member.Identity) is not { Done: true } racer || racer.Score != score)
        {
            error = "A race is finished with finish_race.";
        }
    }

    static partial void OnRoomFinished(ReducerContext ctx, Room room)
    {
        var racers = ctx.Db.RaceRacer.RoomId.Filter(room.Id).ToList();
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
            foreach (var racer in racers)
            {
                if (racer.Player != member.Identity)
                {
                    continue;
                }
                stats.Kills += racer.Kills;
                stats.Finishes += racer.Finished ? 1u : 0u;
                stats.Wrecks += racer.Wrecked ? 1u : 0u;
                if (racer.BestLapMs > 0 && (stats.BestLapMs == 0 || racer.BestLapMs < stats.BestLapMs))
                {
                    stats.BestLapMs = racer.BestLapMs;
                }
            }
            ctx.Db.DeathRaceStats.Player.Update(stats);
        }
    }

    static partial void OnRoomCleared(ReducerContext ctx, Room room)
    {
        ctx.Db.RaceClaim.RoomId.Delete(room.Id);
        ctx.Db.RaceRacer.RoomId.Delete(room.Id);
        ctx.Db.RaceTrack.RoomId.Delete(room.Id);
    }

    static partial void OnUserDeleted(ReducerContext ctx, User user)
    {
        ctx.Db.DeathRaceStats.Player.Delete(user.Identity);
        ctx.Db.RaceRacer.Player.Delete(user.Identity);
    }

    /// <summary>A guest who signed in: their stats go to the account (counts add up, the best lap keeps the faster).</summary>
    static partial void OnUserMerged(ReducerContext ctx, Identity from, Identity to)
    {
        if (ctx.Db.DeathRaceStats.Player.Find(from) is not { } guest)
        {
            return;
        }
        ctx.Db.DeathRaceStats.Player.Delete(from);
        if (ctx.Db.DeathRaceStats.Player.Find(to) is { } account)
        {
            account.Races += guest.Races;
            account.Wins += guest.Wins;
            account.Finishes += guest.Finishes;
            account.Kills += guest.Kills;
            account.Wrecks += guest.Wrecks;
            if (guest.BestLapMs > 0 && (account.BestLapMs == 0 || guest.BestLapMs < account.BestLapMs))
            {
                account.BestLapMs = guest.BestLapMs;
            }
            ctx.Db.DeathRaceStats.Player.Update(account);
        }
        else
        {
            guest.Player = to;
            ctx.Db.DeathRaceStats.Insert(guest);
        }
    }

    // ---------------------------------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------------------------------

    /// <summary>The claim of an item of the caller's race for the car at <paramref name="seat"/>: the first one is kept.</summary>
    private static void Claim(ReducerContext ctx, byte seat, byte kind, uint item)
    {
        if (ActingMember(ctx, seat) is not { } member || ctx.Db.RaceTrack.RoomId.Find(member.RoomId) is null)
        {
            return;
        }
        var key = OnlineRaceRules.ClaimKey(member.RoomId, kind, item);
        if (ctx.Db.RaceClaim.Key.Find(key) is not null)
        {
            return;
        }
        ctx.Db.RaceClaim.Insert(new RaceClaim
        {
            Key = key,
            RoomId = member.RoomId,
            Kind = kind,
            Item = item,
            Seat = member.Seat,
            Owner = member.Identity,
        });
    }

    /// <summary>The car at <paramref name="seat"/> was wrecked, by the car at <paramref name="killer"/> (-1: by nobody).</summary>
    private static void CreditWreck(ReducerContext ctx, Room room, byte seat, int killer)
    {
        if (MemberAt(ctx, room.Id, seat) is not { } victim || ctx.Db.RaceRacer.Player.Find(victim.Identity) is not { } wrecked || wrecked.KilledBy != NoSeat
            || wrecked.Wrecked)
        {
            return;
        }
        wrecked.Wrecked = true;
        if (killer >= 0 && killer != seat && MemberAt(ctx, room.Id, (byte)killer) is { } credited
            && ctx.Db.RaceRacer.Player.Find(credited.Identity) is { } racer)
        {
            wrecked.KilledBy = (byte)killer;
            racer.Kills = (byte)Math.Min(byte.MaxValue - 1, racer.Kills + 1);
            ctx.Db.RaceRacer.Player.Update(racer);
        }
        ctx.Db.RaceRacer.Player.Update(wrecked);
    }

    private static DeathRaceStats StatsOf(ReducerContext ctx, Identity player) =>
        ctx.Db.DeathRaceStats.Player.Find(player) ?? ctx.Db.DeathRaceStats.Insert(new DeathRaceStats { Player = player });
}
