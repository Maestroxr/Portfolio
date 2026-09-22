using SpacetimeDB;

/// <summary>
/// The server module of Asteroids: the Gamebox base server (users and login, rooms with their members, the poses
/// of the players; see the BaseServer folder of this project) plus the co-op missions of this game. Both declare
/// the same partial class, so the tables, reducers and helpers of the base server are in reach here.
///
/// A mission is not simulated here. The client of one pilot, the simulator (the host of the room), runs the
/// waves, the rocks, the enemies and the boss, and publishes every body of its playfield as a
/// <see cref="FieldBody"/> row; the other clients show those bodies as puppets. Every client flies its own ship
/// (the poses of the base server) and decides what hurts it. What a pilot does to the shared world goes to the
/// simulator as an event: the shots they fire, the hits they land, their nova bombs. The server relays, keeps
/// the bodies for whoever needs them, checks who may write what, gives a pickup to the first pilot who claims
/// it, and ends the mission: won when the simulator says so, lost when no pilot flies any more (the base
/// server finishes the room then), abandoned when the simulator leaves.
///
/// After a change: publish the module, then generate the client bindings (Gamebox > Server in the Unity editor,
/// or <c>spacetime publish</c> and <c>spacetime generate</c> in the project folder, which read spacetime.json).
/// </summary>
public static partial class Module
{
    /// <summary>The playfield of a shared mission: the same for every screen, 16:9 at the height of the single player game.</summary>
    public const float FieldHalfHeight = 10f;
    public const float FieldHalfWidth = FieldHalfHeight * 16f / 9f;

    // The numbers of the client's MissionOutcome and BodyKind enums.
    public const byte OutcomeRunning = 0;
    public const byte OutcomeVictory = 1;
    public const byte OutcomeFailed = 2;
    public const byte OutcomeAbandoned = 3;

    public const byte KindReward = 10;

    /// <summary>Seconds between the victory and the end of the room, for the pilots to report their final scores.</summary>
    private const double VictorySeconds = 2.0;

    private const int MaxBatch = 512;

    // ---------------------------------------------------------------------------------------------------
    // Tables
    // ---------------------------------------------------------------------------------------------------

    /// <summary>The mission of a room that plays: who simulates it, its playfield, and how far the pilots got.</summary>
    [SpacetimeDB.Table(Accessor = "Mission", Public = true)]
    public partial struct Mission
    {
        [SpacetimeDB.PrimaryKey]
        public ulong RoomId;

        public int Level;

        /// <summary>The member whose client simulates the world. The mission ends when they leave.</summary>
        public Identity Simulator;

        public float HalfWidth;
        public float HalfHeight;

        /// <summary>Ships every pilot starts with.</summary>
        public uint Lives;

        public uint Wave;
        public uint WavesCleared;

        /// <summary>The objective with its progress as the simulator's HUD words it, for the HUD of the others.</summary>
        public string Status;
        public float Progress;

        public byte Outcome;
    }

    /// <summary>
    /// One body of the simulator's playfield: a rock, a hazard, an enemy, an enemy shot, a pickup, a boss. What the
    /// kind, the variant and the flags mean is the client's business (BodyCodec). Bodies that fly straight are
    /// written once; the simulator updates the ones that steer or were pushed.
    /// </summary>
    [SpacetimeDB.Table(Accessor = "FieldBody", Public = true)]
    public partial struct FieldBody
    {
        /// <summary>The room and the net id in one number, see <see cref="BodyKey"/>.</summary>
        [SpacetimeDB.PrimaryKey]
        public ulong Key;

        [SpacetimeDB.Index.BTree]
        public ulong RoomId;

        /// <summary>Counted by the simulator, from 1, for every game of the room.</summary>
        public uint NetId;

        public byte Kind;
        public int Variant;

        public float X;
        public float Y;
        public float VelocityX;
        public float VelocityY;

        /// <summary>Heading of the body in degrees.</summary>
        public float Heading;

        public float Health;

        /// <summary>Seconds the body has left when that matters for its looks: a black hole closing, a pickup fading.</summary>
        public float Timer;

        public uint Flags;

        /// <summary>The seat of the pilot who claimed the pickup first, or <see cref="NoSeat"/>.</summary>
        public byte ClaimedBy;
    }

    /// <summary>The bodies that left the playfield with one update of the simulator, and why: an event, never stored.</summary>
    [SpacetimeDB.Table(Accessor = "BodyGone", Public = true, Event = true)]
    public partial struct BodyGone
    {
        public ulong RoomId;
        public List<BodyExit> Exits;
    }

    /// <summary>Moments of the simulator's playfield every client plays: blasts (which hurt the ships near them), comet warnings.</summary>
    [SpacetimeDB.Table(Accessor = "FieldSignals", Public = true, Event = true)]
    public partial struct FieldSignals
    {
        public ulong RoomId;
        public List<FieldSignal> Signals;
    }

    /// <summary>The shots a pilot fired in one frame, for the other clients to show.</summary>
    [SpacetimeDB.Table(Accessor = "ShotVolley", Public = true, Event = true)]
    public partial struct ShotVolley
    {
        public ulong RoomId;
        public byte Seat;
        public List<ShotInfo> Shots;
    }

    /// <summary>What a pilot's shots, blasts and ship hit on their copy of the playfield, for the simulator to apply.</summary>
    [SpacetimeDB.Table(Accessor = "HitReport", Public = true, Event = true)]
    public partial struct HitReport
    {
        public ulong RoomId;
        public byte Seat;
        public List<HitInfo> Hits;
    }

    /// <summary>Something a pilot did that is more than a pose: a nova bomb, the shockwave of a new ship.</summary>
    [SpacetimeDB.Table(Accessor = "ShipSignal", Public = true, Event = true)]
    public partial struct ShipSignal
    {
        public ulong RoomId;
        public byte Seat;
        public byte Kind;
        public float X;
        public float Y;
        public float A;
        public float B;
    }

    /// <summary>Ends the room a moment after the victory. Private: SpacetimeDB calls <see cref="CloseMission"/> with the row.</summary>
    [SpacetimeDB.Table(Accessor = "MissionTimer", Scheduled = nameof(CloseMission), ScheduledAt = nameof(ScheduledAt))]
    public partial struct MissionTimer
    {
        [SpacetimeDB.PrimaryKey]
        [SpacetimeDB.AutoInc]
        public ulong ScheduledId;

        public ScheduleAt ScheduledAt;

        [SpacetimeDB.Index.BTree]
        public ulong RoomId;

        public uint Round;
    }

    /// <summary>What a pilot did in shared missions over time.</summary>
    [SpacetimeDB.Table(Accessor = "PilotStats", Public = true)]
    public partial struct PilotStats
    {
        [SpacetimeDB.PrimaryKey]
        public Identity Player;

        public uint Missions;
        public uint Victories;
        public long BestScore;
    }

    // ---------------------------------------------------------------------------------------------------
    // Types
    // ---------------------------------------------------------------------------------------------------

    /// <summary>A body that entered the playfield.</summary>
    [SpacetimeDB.Type]
    public partial struct BodySpawn
    {
        public uint NetId;
        public byte Kind;
        public int Variant;
        public float X;
        public float Y;
        public float VelocityX;
        public float VelocityY;
        public float Heading;
        public float Health;
        public float Timer;
        public uint Flags;
    }

    /// <summary>Where a body is now, for the ones that steer, were pushed or were hit.</summary>
    [SpacetimeDB.Type]
    public partial struct BodyMove
    {
        public uint NetId;
        public float X;
        public float Y;
        public float VelocityX;
        public float VelocityY;
        public float Heading;
        public float Health;
        public uint Flags;
    }

    /// <summary>A body that left the playfield: destroyed, expired or collected, and by which seat.</summary>
    [SpacetimeDB.Type]
    public partial struct BodyExit
    {
        public uint NetId;
        public byte Reason;
        public byte Seat;
    }

    [SpacetimeDB.Type]
    public partial struct FieldSignal
    {
        public byte Kind;
        public float X;
        public float Y;
        public float DirectionX;
        public float DirectionY;
        public float Radius;
        public float Damage;
        public float Push;
        /// <summary>The tint of the moment as 0xRRGGBB.</summary>
        public uint Color;
    }

    [SpacetimeDB.Type]
    public partial struct ShotInfo
    {
        public byte Kind;
        public byte Level;
        public float X;
        public float Y;
        public float VelocityX;
        public float VelocityY;
        public float Lifetime;
    }

    [SpacetimeDB.Type]
    public partial struct HitInfo
    {
        public uint NetId;
        public float Amount;
        public float DirectionX;
        public float DirectionY;
        public float PointX;
        public float PointY;
        public byte Source;
    }

    // ---------------------------------------------------------------------------------------------------
    // Reducers of the simulator
    // ---------------------------------------------------------------------------------------------------

    /// <summary>
    /// One update of the simulator's playfield: the bodies that entered it, the ones that moved another way, the
    /// ones that left, and the moments to play. In one call, so a rock and its fragments change hands together.
    /// </summary>
    [SpacetimeDB.Reducer]
    public static void SyncField(ReducerContext ctx, List<BodySpawn> spawns, List<BodyMove> moves, List<BodyExit> exits, List<FieldSignal> signals)
    {
        if (SimulatedMission(ctx) is not { } mission)
        {
            return;
        }
        if (spawns.Count > MaxBatch || moves.Count > MaxBatch || exits.Count > MaxBatch || signals.Count > MaxBatch)
        {
            throw new Exception("This update of the playfield is too long.");
        }
        var roomId = mission.RoomId;
        foreach (var spawn in spawns)
        {
            var key = BodyKey(roomId, spawn.NetId);
            if (ctx.Db.FieldBody.Key.Find(key) is not null)
            {
                continue;
            }
            ctx.Db.FieldBody.Insert(new FieldBody
            {
                Key = key,
                RoomId = roomId,
                NetId = spawn.NetId,
                Kind = spawn.Kind,
                Variant = spawn.Variant,
                X = spawn.X,
                Y = spawn.Y,
                VelocityX = spawn.VelocityX,
                VelocityY = spawn.VelocityY,
                Heading = spawn.Heading,
                Health = spawn.Health,
                Timer = spawn.Timer,
                Flags = spawn.Flags,
                ClaimedBy = NoSeat,
            });
        }
        foreach (var move in moves)
        {
            if (ctx.Db.FieldBody.Key.Find(BodyKey(roomId, move.NetId)) is not { } body)
            {
                continue;
            }
            body.X = move.X;
            body.Y = move.Y;
            body.VelocityX = move.VelocityX;
            body.VelocityY = move.VelocityY;
            body.Heading = move.Heading;
            body.Health = move.Health;
            body.Flags = move.Flags;
            ctx.Db.FieldBody.Key.Update(body);
        }
        foreach (var exit in exits)
        {
            ctx.Db.FieldBody.Key.Delete(BodyKey(roomId, exit.NetId));
        }
        if (exits.Count > 0)
        {
            ctx.Db.BodyGone.Insert(new BodyGone { RoomId = roomId, Exits = exits });
        }
        if (signals.Count > 0)
        {
            ctx.Db.FieldSignals.Insert(new FieldSignals { RoomId = roomId, Signals = signals });
        }
    }

    /// <summary>The simulator tells how far the mission got, for the HUD of the other pilots.</summary>
    [SpacetimeDB.Reducer]
    public static void ReportMission(ReducerContext ctx, uint wave, uint wavesCleared, string status, float progress)
    {
        if (SimulatedMission(ctx) is not { } mission)
        {
            return;
        }
        status ??= "";
        mission.Wave = wave;
        mission.WavesCleared = wavesCleared;
        mission.Status = status.Length > 64 ? status[..64] : status;
        mission.Progress = Math.Clamp(progress, 0f, 1f);
        ctx.Db.Mission.RoomId.Update(mission);
    }

    /// <summary>
    /// The simulator's objective is complete: the mission is won. The room finishes a moment later, so every
    /// pilot's final score (with the bonus for the ships they have left) counts for the places.
    /// </summary>
    [SpacetimeDB.Reducer]
    public static void CompleteMission(ReducerContext ctx)
    {
        if (SimulatedMission(ctx) is not { Outcome: OutcomeRunning } mission)
        {
            return;
        }
        var room = ctx.Db.Room.Id.Find(mission.RoomId) ?? throw new Exception("This room is gone.");
        mission.Outcome = OutcomeVictory;
        ctx.Db.Mission.RoomId.Update(mission);
        ctx.Db.MissionTimer.RoomId.Delete(room.Id);
        ctx.Db.MissionTimer.Insert(new MissionTimer
        {
            ScheduledAt = new ScheduleAt.Time(ctx.Timestamp + TimeDuration.FromSeconds(VictorySeconds)),
            RoomId = room.Id,
            Round = room.Round,
        });
    }

    /// <summary>Called by SpacetimeDB a moment after the victory: the room finishes and the pilots get their places.</summary>
    [SpacetimeDB.Reducer]
    public static void CloseMission(ReducerContext ctx, MissionTimer timer)
    {
        if (ctx.Sender != ctx.DatabaseIdentity)
        {
            throw new Exception("Only the server ends a mission.");
        }
        if (ctx.Db.Room.Id.Find(timer.RoomId) is { State: RoomState.Playing } room && room.Round == timer.Round)
        {
            FinishRoom(ctx, room);
        }
    }

    // ---------------------------------------------------------------------------------------------------
    // Reducers of every pilot
    // ---------------------------------------------------------------------------------------------------

    /// <summary>The shots the caller fired this frame; the other clients show them, the caller's client decides what they hit.</summary>
    [SpacetimeDB.Reducer]
    public static void FireShots(ReducerContext ctx, List<ShotInfo> shots)
    {
        if (FlyingMember(ctx) is not { } member || shots.Count == 0 || shots.Count > MaxBatch)
        {
            return;
        }
        ctx.Db.ShotVolley.Insert(new ShotVolley { RoomId = member.RoomId, Seat = member.Seat, Shots = shots });
    }

    /// <summary>What the caller hit on their copy of the playfield. The simulator applies it to the real bodies.</summary>
    [SpacetimeDB.Reducer]
    public static void ReportHits(ReducerContext ctx, List<HitInfo> hits)
    {
        if (FlyingMember(ctx) is not { } member || hits.Count == 0 || hits.Count > MaxBatch)
        {
            return;
        }
        ctx.Db.HitReport.Insert(new HitReport { RoomId = member.RoomId, Seat = member.Seat, Hits = hits });
    }

    /// <summary>A nova bomb or the like of the caller, see the client's ShipSignalKind.</summary>
    [SpacetimeDB.Reducer]
    public static void SignalShip(ReducerContext ctx, byte kind, float x, float y, float a, float b)
    {
        if (FlyingMember(ctx) is not { } member)
        {
            return;
        }
        ctx.Db.ShipSignal.Insert(new ShipSignal { RoomId = member.RoomId, Seat = member.Seat, Kind = kind, X = x, Y = y, A = a, B = b });
    }

    /// <summary>
    /// The caller's ship touched a pickup. The first claim marks the row: that pilot's client applies the pickup,
    /// and the simulator takes the body out of the playfield. A later claim changes nothing.
    /// </summary>
    [SpacetimeDB.Reducer]
    public static void ClaimBody(ReducerContext ctx, uint netId)
    {
        if (FlyingMember(ctx) is not { } member
            || ctx.Db.FieldBody.Key.Find(BodyKey(member.RoomId, netId)) is not { Kind: KindReward, ClaimedBy: NoSeat } body)
        {
            return;
        }
        body.ClaimedBy = member.Seat;
        ctx.Db.FieldBody.Key.Update(body);
    }

    // ---------------------------------------------------------------------------------------------------
    // Extension points of the base server
    // ---------------------------------------------------------------------------------------------------

    static partial void ConfigureRoom(ReducerContext ctx, ref Room room, ref string? error)
    {
        // One pilot is enough to fly: the last one left goes on alone.
        room.MinPlayers = 1;
        room.MaxPlayers = Math.Clamp(room.MaxPlayers, (byte)2, (byte)4);
        room.TurnSeconds = 0;
        // The server does not know the campaign; the host's client says how many missions it has.
        var missions = Option(room.Options, "missions", 13, 1, 1000);
        if (room.Level < 0 || room.Level >= missions)
        {
            error = "There is no such mission.";
        }
    }

    static partial void OnRoomStarted(ReducerContext ctx, Room room)
    {
        ctx.Db.Mission.Insert(new Mission
        {
            RoomId = room.Id,
            Level = room.Level,
            Simulator = room.Host,
            HalfWidth = FieldHalfWidth,
            HalfHeight = FieldHalfHeight,
            Lives = (uint)Option(room.Options, "lives", 3, 1, 9),
            Wave = 0,
            WavesCleared = 0,
            Status = "",
            Progress = 0f,
            Outcome = OutcomeRunning,
        });
    }

    static partial void OnMemberLeft(ReducerContext ctx, Room room, RoomMember member)
    {
        // Nobody else has the world: without its simulator the mission is over for everybody.
        if (room.State == RoomState.Playing && ctx.Db.Mission.RoomId.Find(room.Id) is { Outcome: OutcomeRunning } mission
            && mission.Simulator == member.Identity)
        {
            mission.Outcome = OutcomeAbandoned;
            ctx.Db.Mission.RoomId.Update(mission);
            FinishRoom(ctx, room);
        }
    }

    static partial void OnRoomFinished(ReducerContext ctx, Room room)
    {
        var victory = false;
        if (ctx.Db.Mission.RoomId.Find(room.Id) is { } mission)
        {
            if (mission.Outcome == OutcomeRunning)
            {
                // Finished by the base server: no pilot flies any more, or the host ended the game.
                mission.Outcome = OutcomeFailed;
                ctx.Db.Mission.RoomId.Update(mission);
            }
            victory = mission.Outcome == OutcomeVictory;
        }
        ctx.Db.MissionTimer.RoomId.Delete(room.Id);
        foreach (var member in MembersOf(ctx, room.Id))
        {
            var stats = ctx.Db.PilotStats.Player.Find(member.Identity) ?? ctx.Db.PilotStats.Insert(new PilotStats { Player = member.Identity });
            stats.Missions++;
            if (victory)
            {
                stats.Victories++;
            }
            stats.BestScore = Math.Max(stats.BestScore, member.Score);
            ctx.Db.PilotStats.Player.Update(stats);
        }
    }

    static partial void OnRoomCleared(ReducerContext ctx, Room room)
    {
        ctx.Db.MissionTimer.RoomId.Delete(room.Id);
        ctx.Db.FieldBody.RoomId.Delete(room.Id);
        ctx.Db.Mission.RoomId.Delete(room.Id);
    }

    static partial void OnUserDeleted(ReducerContext ctx, User user)
    {
        ctx.Db.PilotStats.Player.Delete(user.Identity);
    }

    // ---------------------------------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------------------------------

    /// <summary>The key of a body: the room in the upper half, the net id in the lower.</summary>
    public static ulong BodyKey(ulong roomId, uint netId) => (roomId << 32) | netId;

    /// <summary>
    /// The running mission the caller simulates. Null when there is none (an update that arrives after the end is
    /// no error); throws when somebody else simulates it.
    /// </summary>
    private static Mission? SimulatedMission(ReducerContext ctx)
    {
        if (ctx.Db.RoomMember.Identity.Find(ctx.Sender) is not { } member
            || ctx.Db.Room.Id.Find(member.RoomId) is not { State: RoomState.Playing }
            || ctx.Db.Mission.RoomId.Find(member.RoomId) is not { } mission)
        {
            return null;
        }
        if (mission.Simulator != ctx.Sender)
        {
            throw new Exception("Only the pilot who simulates the mission can do that.");
        }
        return mission;
    }

    /// <summary>The caller's membership while they fly in a running mission, or null: what comes late is no error.</summary>
    private static RoomMember? FlyingMember(ReducerContext ctx)
    {
        if (ctx.Db.RoomMember.Identity.Find(ctx.Sender) is not { Playing: true } member
            || ctx.Db.Room.Id.Find(member.RoomId) is not { State: RoomState.Playing })
        {
            return null;
        }
        return member;
    }
}
