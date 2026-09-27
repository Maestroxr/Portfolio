using System.Collections.Generic;
using Portfolio.Asteroids.Server;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// Keeps the playfields of a shared mission in step, through the game's tables on the server (Server/Lib.cs). It is
    /// the <see cref="IFieldLink"/> of the <see cref="SpaceField"/> while the mission lasts, owned by the
    /// <see cref="AsteroidsOnlineController"/>.
    ///
    /// On the client that simulates the world, every body that enters the field gets a net id and is announced (a
    /// <c>FieldBody</c> row), bodies that steer, were pushed or were hit are reported again a few times a second (the
    /// ones that fly straight only now and then), and bodies that leave go with the reason. All of it travels in one
    /// call per update, so a rock and its fragments change hands together. Blasts and comet warnings go along as signals.
    /// The simulator also applies what the other pilots report: their hits, their nova bombs, their claims on pickups.
    ///
    /// On the other clients the rows become puppets: the same prefab from the same pool, flying on by itself between
    /// reports. What the local ship does to a puppet is sent to the simulator instead of being decided here.
    ///
    /// Every client reports the shots of its own ship, which the others show as ghosts. The callbacks of the connection
    /// only take notes; <see cref="Tick"/> works through them once a frame, in an order that holds: bodies arrive
    /// before they are moved, are moved before they leave.
    /// </summary>
    public class FieldReplication : IFieldLink
    {
        /// <summary>Numbers of <c>HitInfo.Source</c> that are not a <see cref="DamageSource"/>.</summary>
        private const byte RamSource = 100;
        private const byte DashRamSource = 101;
        private const byte AbsorbedSource = 102;

        private const byte NoSeat = byte.MaxValue;

        /// <summary>Updates of the playfield the simulator sends per second at most.</summary>
        private const float SyncRate = 20f;

        /// <summary>A body whose velocity differs this much from what was reported last steers (or was pushed).</summary>
        private const float SteerTolerance = 0.35f;

        /// <summary>Seconds between two reports of a steering body.</summary>
        private const float SteerInterval = 0.12f;

        /// <summary>Seconds after which a body that flies straight is reported again, which keeps small differences small.</summary>
        private const float Heartbeat = 3f;

        /// <summary>Seconds between two scroll signals of the simulator while the scroll keeps its speed.</summary>
        private const float ScrollInterval = 0.25f;

        /// <summary>A change of the scroll speed (m/s) this big is signalled at once, as is a stop.</summary>
        private const float ScrollJump = 0.5f;

        /// <summary>What the simulator told the others about a body last.</summary>
        private sealed class Track
        {
            public Vector2 Velocity;
            public float SentAt;
            public uint Flags;
            public float Health;
            public bool Dirty;
        }

        private readonly AsteroidsGameManager manager;
        private readonly SpaceField field;
        private readonly SpawnService spawner;
        private readonly BodyRegistry<SpaceBody> bodies = new BodyRegistry<SpaceBody>();

        // The simulator's side.
        private readonly List<SpaceBody> entering = new List<SpaceBody>();
        private readonly Dictionary<SpaceBody, Track> tracks = new Dictionary<SpaceBody, Track>();
        private readonly List<BodyExit> exits = new List<BodyExit>();
        private readonly List<FieldSignal> signals = new List<FieldSignal>();

        // What arrived since the last tick.
        private readonly List<FieldBody> arrivedBodies = new List<FieldBody>();
        private readonly List<FieldBody> movedBodies = new List<FieldBody>();
        private readonly List<FieldBody> claimedBodies = new List<FieldBody>();
        private readonly List<uint> deletedBodies = new List<uint>();
        private readonly List<BodyExit> arrivedExits = new List<BodyExit>();
        private readonly List<FieldSignal> arrivedSignals = new List<FieldSignal>();
        private readonly List<ShotVolley> arrivedVolleys = new List<ShotVolley>();
        private readonly List<HitReport> arrivedHits = new List<HitReport>();
        private readonly List<ShipSignal> arrivedShipSignals = new List<ShipSignal>();

        // Parts of strike bosses announced before their boss arrived here, by net id: bound when it does.
        private readonly Dictionary<uint, FieldBody> pendingParts = new Dictionary<uint, FieldBody>();
        private readonly List<uint> boundParts = new List<uint>();

        // What the local ship did since the last report, with the moment each shot was fired.
        private readonly List<ShotInfo> shots = new List<ShotInfo>();
        private readonly List<float> shotTimes = new List<float>();
        private readonly List<HitInfo> hits = new List<HitInfo>();

        private DbConnection connection;
        private ulong roomId;
        private int localSeat;
        private float nextSync;
        private float nextReport;
        private float nextScroll;
        private float signalledScrollSpeed = -1f;

        public FieldReplication(AsteroidsGameManager manager)
        {
            this.manager = manager;
            field = manager.Field;
            spawner = manager.spawner;
        }

        /// <summary>A shared mission runs and this is the link of its playfield.</summary>
        public bool IsActive { get; private set; }

        public bool Simulates { get; private set; }

        /// <summary>Bodies with a net id: announced ones on the simulator, puppets elsewhere.</summary>
        public int BodyCount => bodies.Count;


        // ------------------------------------------------------------------ the mission

        /// <summary>
        /// The mission of the room starts. A client that does not simulate takes over the bodies that are there already
        /// (none, unless it is late).
        /// </summary>
        public void Begin(DbConnection openConnection, ulong room, int seat, bool simulates)
        {
            End();
            connection = openConnection;
            roomId = room;
            localSeat = seat;
            Simulates = simulates;
            IsActive = true;
            nextSync = 0f;
            nextReport = 0f;
            nextScroll = 0f;
            signalledScrollSpeed = -1f;
            field.Link = this;
            if (simulates)
            {
                field.TargetHit += OnTargetHit;
                spawner.CometIncoming += OnCometIncoming;
                return;
            }
            foreach (FieldBody row in connection.Db.FieldBody.RoomId.Filter(room))
            {
                arrivedBodies.Add(row);
            }
        }


        /// <summary>The mission is over or the pilot left it: the field is on its own again.</summary>
        public void End()
        {
            if (!IsActive)
            {
                return;
            }
            IsActive = false;
            if (Simulates)
            {
                field.TargetHit -= OnTargetHit;
                spawner.CometIncoming -= OnCometIncoming;
            }
            if (ReferenceEquals(field.Link, this))
            {
                field.Link = null;
            }
            connection = null;
            bodies.Clear();
            entering.Clear();
            tracks.Clear();
            exits.Clear();
            signals.Clear();
            arrivedBodies.Clear();
            movedBodies.Clear();
            claimedBodies.Clear();
            deletedBodies.Clear();
            arrivedExits.Clear();
            arrivedSignals.Clear();
            arrivedVolleys.Clear();
            arrivedHits.Clear();
            arrivedShipSignals.Clear();
            pendingParts.Clear();
            shots.Clear();
            shotTimes.Clear();
            hits.Clear();
        }


        /// <summary>Once a frame, after the connection delivered what arrived: applies it, then sends what happened here.</summary>
        public void Tick()
        {
            if (!IsActive || connection == null)
            {
                return;
            }
            if (Simulates)
            {
                ApplyClaims();
                ApplyHitReports();
            }
            else
            {
                ApplyArrivals();
                BindPendingParts();
                ApplyMoves();
                ApplyClaims();
                ApplyExits();
                ApplySignals();
            }
            ApplyShipSignals();
            ApplyVolleys();
            ClearArrived();

            if (Simulates && Time.unscaledTime >= nextSync)
            {
                SendField();
            }
            // The guns of a strike ship fire many times a second: what they did goes out a few times a second at most.
            float now = Time.unscaledTime;
            if (now >= nextReport && (shots.Count > 0 || hits.Count > 0))
            {
                nextReport = now + 1f / SyncRate;
                SendReports(Time.time);
            }
        }


        /// <summary>
        /// The simulator sends what changed right now instead of at the next update: before it declares the victory, so
        /// the boss's exit reaches the others before the verdict.
        /// </summary>
        public void Flush()
        {
            if (IsActive && Simulates && connection != null)
            {
                SendField();
            }
        }


        // ------------------------------------------------------------------ notes of the connection

        public void BodyInserted(FieldBody row)
        {
            if (IsActive && !Simulates && row.RoomId == roomId)
            {
                arrivedBodies.Add(row);
            }
        }

        public void BodyUpdated(FieldBody previous, FieldBody row)
        {
            if (!IsActive || row.RoomId != roomId)
            {
                return;
            }
            if (row.ClaimedBy != previous.ClaimedBy)
            {
                claimedBodies.Add(row);
            }
            else if (!Simulates)
            {
                movedBodies.Add(row);
            }
        }

        public void BodyDeleted(FieldBody row)
        {
            if (IsActive && !Simulates && row.RoomId == roomId)
            {
                deletedBodies.Add(row.NetId);
            }
        }

        public void BodiesGone(BodyGone gone)
        {
            if (IsActive && !Simulates && gone.RoomId == roomId)
            {
                arrivedExits.AddRange(gone.Exits);
            }
        }

        public void SignalsArrived(FieldSignals arrived)
        {
            if (IsActive && !Simulates && arrived.RoomId == roomId)
            {
                arrivedSignals.AddRange(arrived.Signals);
            }
        }

        public void VolleyArrived(ShotVolley volley)
        {
            if (IsActive && volley.RoomId == roomId && volley.Seat != localSeat)
            {
                arrivedVolleys.Add(volley);
            }
        }

        public void HitsArrived(HitReport report)
        {
            if (IsActive && Simulates && report.RoomId == roomId && report.Seat != localSeat)
            {
                arrivedHits.Add(report);
            }
        }

        public void ShipSignalArrived(ShipSignal signal)
        {
            if (IsActive && signal.RoomId == roomId && signal.Seat != localSeat)
            {
                arrivedShipSignals.Add(signal);
            }
        }


        // ------------------------------------------------------------------ the link of the field

        public void BodyAdded(SpaceBody body)
        {
            if (!IsActive || !Simulates || body.IsPuppet || (body is Shot shot && (!shot.IsEnemy || shot.IsGhost)))
            {
                return;
            }
            // Announced with the next update: the spawner may still be placing it.
            entering.Add(body);
        }


        public void BodyRemoved(SpaceBody body)
        {
            if (!IsActive || !bodies.Remove(body, out uint id))
            {
                return;
            }
            if (!Simulates)
            {
                return;
            }
            tracks.Remove(body);
            exits.Add(new BodyExit(id, (byte)body.Exit, ExitSeatOf(body, localSeat, IsStrikeField)));
        }


        /// <summary>
        /// The seat an exit names: the pilot on another device who destroyed or collected the body, else nobody. In a
        /// strike mission a kill of the simulator's own pilot names its seat, so the others count it among the hostiles
        /// destroyed (the seat keeps them from paying for it).
        /// </summary>
        internal static byte ExitSeatOf(SpaceBody body, int simulatorSeat, bool strike)
        {
            if (body.ExitSeat.HasValue)
            {
                return (byte)body.ExitSeat.Value;
            }
            bool ownKill = strike && body.Exit == ExitReason.Destroyed && body.ExitByPlayer;
            return ownKill && simulatorSeat >= 0 && simulatorSeat < NoSeat ? (byte)simulatorSeat : NoSeat;
        }


        public void BlastSetOff(Blast blast)
        {
            if (!IsActive || !Simulates)
            {
                return;
            }
            signals.Add(new FieldSignal((byte)FieldSignalKind.Blast, blast.Center.x, blast.Center.y, 0f, 0f, blast.Radius, blast.PlayerDamage,
                blast.Push, BodyCodec.PackColor(blast.Tint)));
        }


        public void PuppetHit(Shootable puppet, DamageInfo hit)
        {
            if (IsActive && bodies.TryGetId(puppet, out uint id))
            {
                hits.Add(new HitInfo(id, hit.Amount, hit.Direction.x, hit.Direction.y, hit.Point.x, hit.Point.y, (byte)hit.Source));
            }
        }


        public void PuppetRammed(Shootable puppet, Vector2 direction, bool dashing)
        {
            if (IsActive && bodies.TryGetId(puppet, out uint id))
            {
                hits.Add(new HitInfo(id, 0f, direction.x, direction.y, puppet.Position.x, puppet.Position.y, dashing ? DashRamSource : RamSource));
            }
        }


        public void PuppetShotAbsorbed(Shot puppet)
        {
            if (IsActive && bodies.TryGetId(puppet, out uint id))
            {
                hits.Add(new HitInfo(id, 0f, 0f, 0f, puppet.Position.x, puppet.Position.y, AbsorbedSource));
            }
        }


        public void RewardTouched(Reward reward)
        {
            if (IsActive && connection != null && bodies.TryGetId(reward, out uint id))
            {
                connection.Reducers.ClaimBody(id);
                return;
            }
            // Not announced yet: the ship reaches for it again next frame.
            reward.Claimed = false;
        }


        public void ShotFired(Shot shot, byte kind, int level)
        {
            if (IsActive)
            {
                shots.Add(new ShotInfo(kind, (byte)Mathf.Clamp(level, 0, 255), shot.Position.x, shot.Position.y, shot.Velocity.x, shot.Velocity.y, shot.Lifetime));
                shotTimes.Add(Time.time);
            }
        }


        public void NovaFired(Vector2 center, float damage, float bossDamage)
        {
            if (IsActive && connection != null)
            {
                connection.Reducers.SignalShip((byte)ShipSignalKind.Nova, center.x, center.y, damage, bossDamage);
            }
        }


        public void RoomWanted(Vector2 center)
        {
            if (IsActive && connection != null)
            {
                connection.Reducers.SignalShip((byte)ShipSignalKind.MakeRoom, center.x, center.y, 0f, 0f);
            }
        }


        /// <summary>
        /// The simulator's scroll, which may be reported every frame: it goes out as a signal four times a second, and at
        /// once when the speed jumps or the scroll stops or starts.
        /// </summary>
        public void ScrollReported(float distance, float speed)
        {
            if (!IsActive || !Simulates)
            {
                return;
            }
            float now = Time.unscaledTime;
            bool jumped = signalledScrollSpeed < 0f || Mathf.Abs(speed - signalledScrollSpeed) >= ScrollJump ||
                          (speed <= 0f) != (signalledScrollSpeed <= 0f);
            if (now < nextScroll && !jumped)
            {
                return;
            }
            nextScroll = now + ScrollInterval;
            signalledScrollSpeed = Mathf.Max(0f, speed);
            signals.Add(new FieldSignal((byte)FieldSignalKind.Scroll, distance, speed, 0f, 0f, 0f, 0f, 0f, 0u));
        }


        // ------------------------------------------------------------------ the simulator sends

        private void OnTargetHit(Shootable target, DamageInfo hit)
        {
            if (tracks.TryGetValue(target, out Track track))
            {
                track.Dirty = true;
            }
        }


        private void OnCometIncoming(Vector2 from, Vector2 direction, float delay)
        {
            if (IsActive)
            {
                signals.Add(new FieldSignal((byte)FieldSignalKind.CometWarning, from.x, from.y, direction.x, direction.y, delay, 0f, 0f, 0u));
            }
        }


        private void SendField()
        {
            float now = Time.unscaledTime;
            var spawns = new List<BodySpawn>();
            foreach (SpaceBody body in entering)
            {
                if (body == null || !body.InPlay || !Announces(body, out BodyKind kind, out int variant))
                {
                    continue;
                }
                uint id = bodies.Register(body);
                uint flags = FlagsOf(body) | (body.WarpedIn ? (uint)BodyFlags.WarpIn : 0u);
                float health = HealthOf(body);
                float timer = body.Lifetime > 0f ? Mathf.Max(0f, body.Lifetime - body.Age) : 0f;
                spawns.Add(new BodySpawn(id, (byte)kind, variant, body.Position.x, body.Position.y, body.Velocity.x, body.Velocity.y,
                    HeadingOf(body), health, timer, flags));
                tracks[body] = new Track { Velocity = body.Velocity, SentAt = now, Flags = FlagsOf(body), Health = health };
            }
            entering.Clear();

            var moves = new List<BodyMove>();
            float tolerance = SteerTolerance * SteerTolerance;
            foreach (KeyValuePair<uint, SpaceBody> entry in bodies.Entries)
            {
                SpaceBody body = entry.Value;
                if (!tracks.TryGetValue(body, out Track track) || track.SentAt >= now)
                {
                    continue;
                }
                uint flags = FlagsOf(body);
                float health = HealthOf(body);
                float since = now - track.SentAt;
                bool changed = track.Dirty || flags != track.Flags || Mathf.Abs(health - track.Health) > 0.001f;
                bool steered = (body.Velocity - Predicted(body, track.Velocity, since)).sqrMagnitude > tolerance && since >= SteerInterval;
                if (!changed && !steered && since < Heartbeat)
                {
                    continue;
                }
                moves.Add(new BodyMove(entry.Key, body.Position.x, body.Position.y, body.Velocity.x, body.Velocity.y, HeadingOf(body), health, flags));
                track.Velocity = body.Velocity;
                track.SentAt = now;
                track.Flags = flags;
                track.Health = health;
                track.Dirty = false;
            }

            if (spawns.Count == 0 && moves.Count == 0 && exits.Count == 0 && signals.Count == 0)
            {
                return;
            }
            // The server takes at most MaxBatch of each per call and rejects a longer call as a whole, which would leave
            // the others without these bodies for good: a busy moment of a strike mission goes out in several calls.
            int calls = Mathf.Max(Mathf.Max(BodyCodec.BatchCount(spawns.Count), BodyCodec.BatchCount(moves.Count)),
                Mathf.Max(BodyCodec.BatchCount(exits.Count), BodyCodec.BatchCount(signals.Count)));
            for (int call = 0; call < calls; call++)
            {
                connection.Reducers.SyncField(BodyCodec.Batch(spawns, call), BodyCodec.Batch(moves, call), BodyCodec.Batch(exits, call),
                    BodyCodec.Batch(signals, call));
            }
            exits.Clear();
            signals.Clear();
            nextSync = now + 1f / SyncRate;
        }


        /// <summary>
        /// The velocity the others' puppet of <paramref name="body"/> has by itself <paramref name="since"/> seconds after it
        /// was sent <paramref name="sent"/>: a shot that speeds up does so on every client (along its course, up to its top
        /// speed), so only a change of course is steering. Everything else keeps the velocity it was sent.
        /// </summary>
        internal static Vector2 Predicted(SpaceBody body, Vector2 sent, float since)
        {
            if (!(body is Shot shot) || shot.acceleration <= 0f || since <= 0f)
            {
                return sent;
            }
            float speed = sent.magnitude;
            if (speed <= 0.001f)
            {
                return sent;
            }
            float expected = speed + shot.acceleration * since;
            if (shot.maxSpeed > 0f)
            {
                expected = Mathf.Min(expected, shot.maxSpeed);
            }
            return sent / speed * expected;
        }


        /// <summary>
        /// Whether the others are told about <paramref name="body"/>, and as what. A part of a strike boss names its boss by
        /// net id (the boss gets its id here if it has none yet), so the others can find the part on their boss.
        /// </summary>
        private bool Announces(SpaceBody body, out BodyKind kind, out int variant)
        {
            if (body is BossPart part)
            {
                kind = BodyKind.BossPart;
                variant = 0;
                StrikeBoss boss = part.Boss;
                if (boss == null || !boss.InPlay || !spawner.Describe(boss, out _, out _))
                {
                    return false;
                }
                variant = BodyCodec.PackPart(bodies.Register(boss), part.Index);
                return true;
            }
            return spawner.Describe(body, out kind, out variant);
        }


        internal static uint FlagsOf(SpaceBody body)
        {
            switch (body)
            {
                case Mine mine:
                    return (uint)(mine.IsArmed ? BodyFlags.Armed : BodyFlags.None);
                case Boss boss:
                    return (uint)boss.StateFlags;
                case Enemy enemy:
                    // An armoured part of a strike boss (or any enemy that cannot be hurt now) must not look hittable elsewhere,
                    // and the warning of a strike unit's attack shows everywhere.
                    return (uint)((enemy.Invulnerable ? BodyFlags.Shielded : BodyFlags.None) |
                                  (enemy.IsTelegraphing ? BodyFlags.Warning : BodyFlags.None));
                default:
                    return 0u;
            }
        }


        /// <summary>Where a body points: a ground unit's heading (its hull may be turned apart from the root), else the root's.</summary>
        private static float HeadingOf(SpaceBody body)
        {
            return body is GroundUnit ground ? ground.Heading : body.transform.eulerAngles.z;
        }


        /// <summary>
        /// Sends the shots and hits of the local ship. A shot waited for up to one report: it is sent where it has got to
        /// by now, with the time it has left, so the ghosts elsewhere do not trail behind.
        /// </summary>
        private void SendReports(float time)
        {
            if (shots.Count > 0)
            {
                for (int i = 0; i < shots.Count; i++)
                {
                    float age = i < shotTimes.Count ? Mathf.Max(0f, time - shotTimes[i]) : 0f;
                    ShotInfo shot = shots[i];
                    if (age > 0f)
                    {
                        shots[i] = new ShotInfo(shot.Kind, shot.Level, shot.X + shot.VelocityX * age, shot.Y + shot.VelocityY * age,
                            shot.VelocityX, shot.VelocityY, shot.Lifetime > 0f ? Mathf.Max(0.01f, shot.Lifetime - age) : shot.Lifetime);
                    }
                }
                for (int call = 0; call < BodyCodec.BatchCount(shots.Count); call++)
                {
                    connection.Reducers.FireShots(BodyCodec.Batch(shots, call));
                }
                shots.Clear();
                shotTimes.Clear();
            }
            if (hits.Count > 0)
            {
                for (int call = 0; call < BodyCodec.BatchCount(hits.Count); call++)
                {
                    connection.Reducers.ReportHits(BodyCodec.Batch(hits, call));
                }
                hits.Clear();
            }
        }


        private static float HealthOf(SpaceBody body)
        {
            return body is Shootable shootable ? shootable.Health : 0f;
        }


        // ------------------------------------------------------------------ the simulator applies

        private void ApplyHitReports()
        {
            foreach (HitReport report in arrivedHits)
            {
                RemoteShip pilot = manager.RemoteShipAt(report.Seat);
                foreach (HitInfo hit in report.Hits)
                {
                    if (!bodies.TryGet(hit.NetId, out SpaceBody body) || !body.InPlay)
                    {
                        continue;
                    }
                    var direction = new Vector2(hit.DirectionX, hit.DirectionY);
                    if (hit.Source == AbsorbedSource)
                    {
                        (body as Shot)?.Impact();
                        continue;
                    }
                    if (!(body is Shootable target) || !target.IsAlive)
                    {
                        continue;
                    }
                    if ((hit.Source == RamSource || hit.Source == DashRamSource) && IsStrikeField)
                    {
                        // Strike: a rammed aircraft takes the ram damage of the tick and is not pushed.
                        target.TakeHit(new DamageInfo(StrikeRules.RamUnitDamage, direction, target.Position, DamageSource.Collision, true) { Seat = report.Seat });
                        continue;
                    }
                    if (hit.Source == RamSource || hit.Source == DashRamSource)
                    {
                        bool dashing = hit.Source == DashRamSource;
                        if (pilot != null)
                        {
                            target.OnRammed(pilot.Player, direction, dashing);
                        }
                        else
                        {
                            target.TakeHit(new DamageInfo(dashing ? 3f : 2f, direction, target.Position, DamageSource.Collision, true) { Seat = report.Seat });
                        }
                        if (!dashing && target.InPlay)
                        {
                            target.Velocity += direction * (1.5f / Mathf.Max(0.5f, target.Radius));
                        }
                        continue;
                    }
                    target.TakeHit(new DamageInfo(hit.Amount, direction, new Vector2(hit.PointX, hit.PointY), (DamageSource)hit.Source, true) { Seat = report.Seat });
                }
            }
        }


        /// <summary>The server gave a pickup to a pilot: the local ship gets what it holds, anybody else's is taken off the field.</summary>
        private void ApplyClaims()
        {
            foreach (FieldBody row in claimedBodies)
            {
                if (row.ClaimedBy == NoSeat || !bodies.TryGet(row.NetId, out SpaceBody body) || !(body is Reward reward) || !reward.InPlay)
                {
                    continue;
                }
                if (row.ClaimedBy == localSeat)
                {
                    field.GrantReward(reward);
                    continue;
                }
                manager.CoopPickupTaken(reward);
                reward.CollectedElsewhere(row.ClaimedBy);
            }
        }


        // ------------------------------------------------------------------ the others apply

        private void ApplyArrivals()
        {
            foreach (FieldBody row in arrivedBodies)
            {
                if (bodies.Contains(row.NetId))
                {
                    movedBodies.Add(row);
                    continue;
                }
                var kind = (BodyKind)row.Kind;
                if (kind == BodyKind.BossPart)
                {
                    // Never spawned: the part is already on its boss, which may arrive in this tick or a later one.
                    pendingParts[row.NetId] = row;
                    continue;
                }
                SpaceBody puppet = spawner.SpawnPuppet(kind, row.Variant, row.Timer);
                if (puppet == null)
                {
                    continue;
                }
                bodies.Bind(row.NetId, puppet);
                puppet.Position = new Vector2(row.X, row.Y);
                puppet.Velocity = new Vector2(row.VelocityX, row.VelocityY);
                if (puppet is EnemyBeam beam && row.Timer > 0f)
                {
                    // A beam burns for the time the simulator's had left, on its own clock: not until its exit arrives.
                    beam.BurnFor(row.Timer);
                }
                ShowState(puppet, row, true);
                spawner.PlayEntrance(puppet, kind, BodyCodec.Has((BodyFlags)row.Flags, BodyFlags.WarpIn));
                if (puppet is Boss boss)
                {
                    manager.CoopBossArrived(boss);
                }
            }
        }


        /// <summary>
        /// Binds the announced parts of strike bosses to the parts of their boss's puppet (the boss put them into the
        /// field itself). A part whose boss is not here yet waits; its own exit takes it off the list.
        /// </summary>
        private void BindPendingParts()
        {
            if (pendingParts.Count == 0)
            {
                return;
            }
            boundParts.Clear();
            foreach (KeyValuePair<uint, FieldBody> entry in pendingParts)
            {
                BodyCodec.UnpackPart(entry.Value.Variant, out uint bossId, out int index);
                if (!bodies.TryGet(bossId, out SpaceBody body) || !(body is StrikeBoss boss) || !boss.InPlay || index >= boss.Parts.Count)
                {
                    continue;
                }
                BossPart part = boss.Parts[index];
                if (part == null || !part.InPlay || !bodies.Bind(entry.Key, part))
                {
                    continue;
                }
                ShowState(part, entry.Value, true);
                boundParts.Add(entry.Key);
            }
            foreach (uint id in boundParts)
            {
                pendingParts.Remove(id);
            }
        }


        private void ApplyMoves()
        {
            foreach (FieldBody row in movedBodies)
            {
                if (bodies.TryGet(row.NetId, out SpaceBody puppet) && puppet.InPlay)
                {
                    // A boss part sits where its boss holds it: only its hull and armour follow the simulator.
                    if (!(puppet is BossPart))
                    {
                        puppet.Correct(new Vector2(row.X, row.Y), new Vector2(row.VelocityX, row.VelocityY));
                    }
                    ShowState(puppet, row, false);
                }
                else if (pendingParts.ContainsKey(row.NetId))
                {
                    pendingParts[row.NetId] = row;
                }
            }
        }


        /// <summary>
        /// What shows of a body next to where it is: its hull, its heading where it has one, a tripped mine, the state of a
        /// boss, the armour of an enemy (a strike boss part behind a lower tier), the warning of a strike unit's attack.
        /// </summary>
        private static void ShowState(SpaceBody puppet, FieldBody row, bool first)
        {
            if (puppet is Shootable shootable)
            {
                if (first)
                {
                    shootable.Health = row.Health;
                }
                else
                {
                    shootable.ShowHealth(row.Health);
                }
            }
            var flags = (BodyFlags)row.Flags;
            switch (puppet)
            {
                case Mine mine when BodyCodec.Has(flags, BodyFlags.Armed):
                    mine.Arm();
                    break;
                case Boss boss:
                    boss.ShowState(flags);
                    boss.transform.rotation = Quaternion.Euler(0f, 0f, row.Heading);
                    break;
                case Comet comet:
                    comet.transform.rotation = Quaternion.Euler(0f, 0f, row.Heading);
                    break;
                case GroundUnit ground:
                    ground.Invulnerable = BodyCodec.Has(flags, BodyFlags.Shielded);
                    ground.Heading = row.Heading;
                    ground.transform.rotation = Quaternion.Euler(0f, 0f, row.Heading);
                    ground.ShowWarning(BodyCodec.Has(flags, BodyFlags.Warning));
                    break;
                case Enemy enemy:
                    enemy.Invulnerable = BodyCodec.Has(flags, BodyFlags.Shielded);
                    enemy.ShowWarning(BodyCodec.Has(flags, BodyFlags.Warning));
                    break;
            }
        }


        private void ApplyExits()
        {
            foreach (BodyExit exit in arrivedExits)
            {
                if (pendingParts.Remove(exit.NetId) || !bodies.Remove(exit.NetId, out SpaceBody puppet) || !puppet.InPlay)
                {
                    continue;
                }
                bool mine = exit.Seat == localSeat;
                switch ((ExitReason)exit.Reason)
                {
                    case ExitReason.Destroyed when puppet is Shootable shootable:
                        shootable.PlayDestroyed(new DamageInfo(0f, Vector2.zero, puppet.Position, DamageSource.PlayerShot, exit.Seat != NoSeat)
                        {
                            Seat = mine || exit.Seat == NoSeat ? (int?)null : exit.Seat
                        });
                        break;
                    case ExitReason.Collected when puppet is Reward reward:
                        reward.CollectedElsewhere(exit.Seat);
                        break;
                    case ExitReason.Impact when puppet is Shot shot:
                        shot.PlayImpact();
                        break;
                    default:
                        puppet.Despawn();
                        break;
                }
            }
            // A row that went without a word (the room was cleared) takes its puppet along.
            foreach (uint id in deletedBodies)
            {
                if (!pendingParts.Remove(id) && bodies.Remove(id, out SpaceBody puppet) && puppet.InPlay)
                {
                    puppet.Despawn();
                }
            }
        }


        private void ApplySignals()
        {
            foreach (FieldSignal signal in arrivedSignals)
            {
                switch ((FieldSignalKind)signal.Kind)
                {
                    case FieldSignalKind.Blast:
                        field.ShowBlast(new Blast
                        {
                            Center = new Vector2(signal.X, signal.Y),
                            Radius = signal.Radius,
                            PlayerDamage = signal.Damage,
                            Push = signal.Push,
                            Tint = BodyCodec.UnpackColor(signal.Color)
                        });
                        break;
                    case FieldSignalKind.CometWarning:
                        spawner.ShowCometWarning(new Vector2(signal.X, signal.Y), new Vector2(signal.DirectionX, signal.DirectionY), signal.Radius);
                        break;
                    case FieldSignalKind.Scroll:
                        manager.CoopScrollReported(signal.X, signal.Y);
                        break;
                }
            }
        }


        // ------------------------------------------------------------------ everybody applies

        private void ApplyShipSignals()
        {
            foreach (ShipSignal signal in arrivedShipSignals)
            {
                var at = new Vector2(signal.X, signal.Y);
                switch ((ShipSignalKind)signal.Kind)
                {
                    case ShipSignalKind.Nova when IsStrikeField:
                        // Another pilot's megabomb: every screen flashes, since every screen loses its enemy shots.
                        field.Effects?.MegabombFlash();
                        field.Sounds?.Megabomb();
                        field.CameraRig?.Shake(0.3f);
                        if (Simulates)
                        {
                            field.Nova(at, signal.A, signal.B, signal.Seat);
                        }
                        break;
                    case ShipSignalKind.Nova:
                        field.Effects?.Nova(at);
                        field.Sounds?.Nova();
                        field.CameraRig?.Shake(0.4f);
                        if (Simulates)
                        {
                            field.Nova(at, signal.A, signal.B, signal.Seat);
                        }
                        break;
                    case ShipSignalKind.MakeRoom:
                        if (Simulates)
                        {
                            field.MakeRoom(at, signal.Seat);
                        }
                        break;
                }
            }
        }


        private void ApplyVolleys()
        {
            foreach (ShotVolley volley in arrivedVolleys)
            {
                bool sounded = false;
                foreach (ShotInfo shot in volley.Shots)
                {
                    spawner.FireGhostShot(shot.Kind, shot.Level, new Vector2(shot.X, shot.Y), new Vector2(shot.VelocityX, shot.VelocityY), shot.Lifetime);
                    if (!sounded && shot.Kind != BodyCodec.DroneShot)
                    {
                        sounded = true;
                        PlayVolley(shot.Kind);
                    }
                }
            }
        }


        /// <summary>The sound of another pilot's volley: the weapon of the field, or the kind of a strike projectile.</summary>
        private void PlayVolley(byte kind)
        {
            AsteroidsAudio sounds = field.Sounds;
            if (sounds == null)
            {
                return;
            }
            if (kind < SpawnService.StrikeGhostBase)
            {
                sounds.Fire((WeaponType)kind);
                return;
            }
            switch ((StrikeShotKind)(kind - SpawnService.StrikeGhostBase))
            {
                case StrikeShotKind.Bullet:
                case StrikeShotKind.MiniGunRound:
                    sounds.MachineGun();
                    break;
                case StrikeShotKind.Bomb:
                    sounds.BombDrop();
                    break;
                case StrikeShotKind.PlasmaBolt:
                case StrikeShotKind.Pulse:
                case StrikeShotKind.DisrupterOrb:
                    sounds.Fire(WeaponType.Blaster);
                    break;
                default:
                    sounds.StrikeMissileLaunch();
                    break;
            }
        }


        private void ClearArrived()
        {
            arrivedBodies.Clear();
            movedBodies.Clear();
            claimedBodies.Clear();
            deletedBodies.Clear();
            arrivedExits.Clear();
            arrivedSignals.Clear();
            arrivedVolleys.Clear();
            arrivedHits.Clear();
            arrivedShipSignals.Clear();
        }


        /// <summary>A line about the bodies with a net id, for comparing the clients of a test run.</summary>
        public string Describe()
        {
            int rocks = 0;
            int enemies = 0;
            int shotCount = 0;
            int pickups = 0;
            int air = 0;
            int ground = 0;
            int parts = 0;
            Vector2 sum = Vector2.zero;
            foreach (KeyValuePair<uint, SpaceBody> entry in bodies.Entries)
            {
                switch (entry.Value)
                {
                    case Asteroid asteroid:
                        rocks++;
                        sum += asteroid.Position;
                        break;
                    case StrikeAircraft _:
                        air++;
                        break;
                    case GroundUnit _:
                        ground++;
                        break;
                    case BossPart _:
                        parts++;
                        break;
                    case Shot _:
                        shotCount++;
                        break;
                    case Reward _:
                        pickups++;
                        break;
                    default:
                        enemies++;
                        break;
                }
            }
            if (IsStrikeField)
            {
                return $"bodies {bodies.Count} (aircraft {air}, ground units {ground}, boss parts {parts} + {pendingParts.Count} waiting, " +
                       $"other enemies {enemies}, enemy shots {shotCount}, pickups {pickups}), scroll {field.ScrollDistance:0.0} m at {field.ScrollSpeed:0.00} m/s";
            }
            return $"bodies {bodies.Count} (rocks {rocks}, hazards and enemies {enemies}, enemy shots {shotCount}, pickups {pickups}), rocks centre ({sum.x / Mathf.Max(1, rocks):0.0}, {sum.y / Mathf.Max(1, rocks):0.0})";
        }


        /// <summary>The playfield is a strike mission's: it does not wrap, rams do not push, a nova is a megabomb.</summary>
        private bool IsStrikeField => field.Playground != null && !field.Playground.Wraps;
    }
}
