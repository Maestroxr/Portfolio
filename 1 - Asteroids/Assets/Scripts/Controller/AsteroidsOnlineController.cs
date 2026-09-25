using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Gamebox.Online;
using Portfolio.Asteroids.Server;
using SpacetimeDB;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// Online controller of the Asteroids module: missions flown together with pilots on other devices, in a room of the
    /// game's SpacetimeDB database. Rooms, ready and start, the poses of the ships and the scores come from the base
    /// <see cref="OnlineGameController"/> and the base server; this class adds what is Asteroids. A room plays a mission
    /// of the campaign with a number of ships per pilot. When it starts, the client of the host simulates the world and
    /// the others show it (<see cref="FieldReplication"/>, which this controller owns and feeds from the connection);
    /// every client flies its own ship, sends its pose, and shows the others as stand-ins (<see cref="RemoteShip"/>).
    /// The manager plays the mission in its co-op mode (AsteroidsGameManager.Coop.cs) and hears from here how far the
    /// simulator got and how the server says the mission ended.
    /// </summary>
    public class AsteroidsOnlineController : OnlineGameController
    {
        private const float StatusInterval = 0.5f;
        private const float ScoreInterval = 1.5f;

        [Tooltip("The ship prefab: the stand-ins of the other pilots are instances of it.")]
        [SerializeField] internal AsteroidsPlayer shipPrefab;

        private readonly Dictionary<Identity, RemoteShip> ships = new Dictionary<Identity, RemoteShip>();
        private readonly List<RoomLevelChoice> levelChoices = new List<RoomLevelChoice>();
        private FieldReplication replication;
        private RoomOptionSpec[] optionSpecs;
        private bool missionChanged;
        private float nextStatus;
        private float nextScore;
        private long reportedScore = -1;
        private string reportedStatus;
        private int reportedWave = -1;
        private int reportedCleared = -1;

        public AsteroidsGameManager Asteroids => BaseManager as AsteroidsGameManager;

        /// <summary>Keeps the playfields of the running mission in step; idle outside of one.</summary>
        public FieldReplication Replication => replication;

        private GameServerClient Client => Server as GameServerClient;

        public override int MaxPlayersLimit => CoopRules.MaxPilots;

        public override int MinPlayersLimit => CoopRules.MinPilots;

        /// <summary>
        /// The missions the host has unlocked, the endless one included. The list is made when the lobby first asks and
        /// stays as it is, because the lobby keeps the names it was built with.
        /// </summary>
        public override IReadOnlyList<RoomLevelChoice> LevelChoices
        {
            get
            {
                AsteroidsGameManager manager = Asteroids;
                if (levelChoices.Count > 0 || manager == null)
                {
                    return levelChoices;
                }
                int number = 0;
                for (int i = 0; i < manager.LevelCount; i++)
                {
                    AsteroidsLevel mission = manager.AsteroidsCampaign != null ? manager.AsteroidsCampaign.Mission(i) : null;
                    if (mission == null)
                    {
                        continue;
                    }
                    if (!mission.IsEndless)
                    {
                        number++;
                    }
                    if (i == 0 || manager.IsUnlocked(i))
                    {
                        levelChoices.Add(new RoomLevelChoice(i, mission.IsEndless ? $"{mission.Title} (endless)" : $"{number}. {mission.Title}"));
                    }
                }
                return levelChoices;
            }
        }

        public override IReadOnlyList<RoomOptionSpec> OptionSpecs =>
            optionSpecs ??= new[] { new RoomOptionSpec(CoopRules.LivesKey, "Ships per pilot", CoopRules.LivesChoices, null, 2) };


        /// <summary>The options of a room: the ships the host picked, and how many missions there are for the server to check the level against.</summary>
        public override string ComposeOptions(int level, string picked)
        {
            int lives = CoopRules.Lives(picked);
            return RoomOptions.Write(CoopRules.LivesKey, lives, CoopRules.MissionsKey, Asteroids != null ? Asteroids.LevelCount : 1);
        }


        public override string DescribeRoom(RoomInfo room)
        {
            AsteroidsGameManager manager = Asteroids;
            AsteroidsLevel mission = manager != null && manager.AsteroidsCampaign != null ? manager.AsteroidsCampaign.Mission(room.Level) : null;
            string title = mission != null ? mission.Title : $"Mission {room.Level + 1}";
            int lives = CoopRules.Lives(room.Options);
            return $"{title}, {lives} {(lives == 1 ? "ship" : "ships")} each";
        }


        protected override void Start()
        {
            base.Start();
            if (Asteroids != null)
            {
                replication = new FieldReplication(Asteroids);
            }
            if (Client != null)
            {
                Client.ConnectionOpened += Bind;
            }
        }


        protected override void OnDestroy()
        {
            if (Client != null)
            {
                Client.ConnectionOpened -= Bind;
            }
            replication?.End();
            base.OnDestroy();
        }


        protected override void Update()
        {
            base.Update();
            if (replication != null && replication.IsActive && !InCharge)
            {
                // The game went back to its menu: the playfield is on its own again.
                replication.End();
            }
        }


        /// <summary>
        /// The rows and events of the game's tables arrive in any order within a frame: the callbacks only pass them on as
        /// notes, and <see cref="OnServerTick"/> has them applied.
        /// </summary>
        private void Bind(DbConnection connection)
        {
            connection.Db.Mission.OnInsert += (context, row) => missionChanged = true;
            connection.Db.Mission.OnUpdate += (context, previous, row) => missionChanged = true;
            connection.Db.FieldBody.OnInsert += (context, row) => replication?.BodyInserted(row);
            connection.Db.FieldBody.OnUpdate += (context, previous, row) => replication?.BodyUpdated(previous, row);
            connection.Db.FieldBody.OnDelete += (context, row) => replication?.BodyDeleted(row);
            connection.Db.BodyGone.OnInsert += (context, row) => replication?.BodiesGone(row);
            connection.Db.FieldSignals.OnInsert += (context, row) => replication?.SignalsArrived(row);
            connection.Db.ShotVolley.OnInsert += (context, row) => replication?.VolleyArrived(row);
            connection.Db.HitReport.OnInsert += (context, row) => replication?.HitsArrived(row);
            connection.Db.ShipSignal.OnInsert += (context, row) => replication?.ShipSignalArrived(row);
        }


        protected override IEnumerable<string> RoomQueries(RoomInfo room)
        {
            yield return $"SELECT * FROM mission WHERE room_id = {room.Id}";
            yield return $"SELECT * FROM field_body WHERE room_id = {room.Id}";
            yield return $"SELECT * FROM body_gone WHERE room_id = {room.Id}";
            yield return $"SELECT * FROM field_signals WHERE room_id = {room.Id}";
            yield return $"SELECT * FROM shot_volley WHERE room_id = {room.Id}";
            yield return $"SELECT * FROM hit_report WHERE room_id = {room.Id}";
            yield return $"SELECT * FROM ship_signal WHERE room_id = {room.Id}";
        }


        // ------------------------------------------------------------------------------------------------
        // The mission of the room
        // ------------------------------------------------------------------------------------------------

        /// <summary>The host started the mission: the manager gets its co-op setup, then loads the level and starts; the other pilots get their stand-ins.</summary>
        protected override void OnRoomStarted(RoomInfo room)
        {
            AsteroidsGameManager manager = Asteroids;
            DbConnection connection = Client != null ? Client.Connection : null;
            Mission mission = connection != null ? connection.Db.Mission.RoomId.Find(room.Id) : null;
            if (manager == null || replication == null || mission == null || !Server.Identity.HasValue)
            {
                Report("The mission of the room did not arrive.");
                return;
            }
            bool simulates = mission.Simulator == Server.Identity.Value;
            int slot = 0;
            for (int i = 0; i < Server.Members.Count; i++)
            {
                if (Server.IsLocal(Server.Members[i]))
                {
                    slot = i;
                }
            }
            replication.Begin(connection, room.Id, LocalSeat, simulates);
            manager.PrepareCoop(new AsteroidsGameManager.CoopSetup
            {
                Simulates = simulates,
                LocalSeat = LocalSeat,
                Slot = slot,
                Pilots = Server.Members.Count,
                HalfSize = new Vector2(mission.HalfWidth, mission.HalfHeight),
                Lives = (int)mission.Lives
            });
            missionChanged = false;
            reportedScore = -1;
            reportedStatus = null;
            reportedWave = -1;
            reportedCleared = -1;
            nextStatus = 0f;
            nextScore = 0f;
            base.OnRoomStarted(room);
            SyncShips();
            ShowPilots();
        }


        /// <summary>The server ended the mission: the manager plays the end it names, and the results list the places.</summary>
        protected override void OnRoomFinished(RoomInfo room)
        {
            if (!InCharge || Asteroids == null)
            {
                return;
            }
            Mission mission = Client != null && Client.Connection != null ? Client.Connection.Db.Mission.RoomId.Find(room.Id) : null;
            var outcome = mission != null ? (MissionOutcome)mission.Outcome : MissionOutcome.Failed;
            Asteroids.CoopOutcome(outcome == MissionOutcome.Running ? MissionOutcome.Failed : outcome);
            Asteroids.CoopStandingsChanged();
        }


        protected override void OnRoomLeft(RoomInfo room)
        {
            replication?.End();
            ships.Clear();
        }


        protected override void OnMembersChanged()
        {
            if (!InCharge)
            {
                return;
            }
            SyncShips();
            ShowPilots();
            Asteroids?.CoopStandingsChanged();
        }


        protected override void OnPoseChanged(RoomPoseInfo pose)
        {
            if (InCharge && ships.TryGetValue(pose.Identity, out RemoteShip ship) && ship != null)
            {
                ship.Pose(pose);
            }
        }


        protected override void OnServerTick()
        {
            AsteroidsGameManager manager = Asteroids;
            if (!InCharge || manager == null || replication == null)
            {
                return;
            }
            if (missionChanged)
            {
                missionChanged = false;
                FollowMission(manager);
            }
            replication.Tick();
            SendLocalPose(manager);
            float now = Time.unscaledTime;
            if (now >= nextStatus)
            {
                nextStatus = now + StatusInterval;
                PublishMission(manager);
                ShowPilots();
            }
            if (now >= nextScore && IsPlaying && manager.Scoring.Score != reportedScore && Server.LocalMember != null && Server.LocalMember.Playing)
            {
                ReportScoreNow(manager.Scoring.Score);
            }
        }


        /// <summary>The mission row changed: the HUD of a guest follows the simulator, and a victory starts here as it is declared.</summary>
        private void FollowMission(AsteroidsGameManager manager)
        {
            RoomInfo room = Room;
            Mission mission = room != null && Client != null && Client.Connection != null ? Client.Connection.Db.Mission.RoomId.Find(room.Id) : null;
            if (mission == null)
            {
                return;
            }
            manager.CoopReported((int)mission.Wave, (int)mission.WavesCleared, mission.Status, mission.Progress);
            if ((MissionOutcome)mission.Outcome == MissionOutcome.Victory)
            {
                manager.CoopOutcome(MissionOutcome.Victory);
            }
        }


        /// <summary>The simulator tells the others how far the mission got, when that changed.</summary>
        private void PublishMission(AsteroidsGameManager manager)
        {
            if (!replication.Simulates || !manager.CoopReport(out int wave, out int cleared, out string status, out float progress))
            {
                return;
            }
            if (wave == reportedWave && cleared == reportedCleared && status == reportedStatus)
            {
                return;
            }
            reportedWave = wave;
            reportedCleared = cleared;
            reportedStatus = status;
            Client.Connection.Reducers.ReportMission((uint)Mathf.Max(0, wave), (uint)Mathf.Max(0, cleared), status, progress);
        }


        private void SendLocalPose(AsteroidsGameManager manager)
        {
            AsteroidsPlayer ship = manager.Ship;
            if (ship == null)
            {
                return;
            }
            var pose = new ShipPose
            {
                Alive = ship.IsAlive,
                Thrusting = ship.IsAlive && ship.Simulation.IsThrusting,
                Dashing = ship.IsAlive && ship.IsDashing,
                Invulnerable = ship.IsAlive && ship.InvulnerableTime > 0f,
                Magnet = ship.IsAlive && ship.IsPowerUpActive(PowerUpType.Magnet),
                Drones = ship.IsAlive && ship.IsPowerUpActive(PowerUpType.Drones),
                Hull = manager.LocalHullIndex,
                Health = ship.MaxHealth > 0f ? Mathf.Clamp01(ship.Health / ship.MaxHealth) : 0f,
                Shield = ship.MaxShield > 0f ? Mathf.Clamp01(ship.Shield / ship.MaxShield) : 0f
            };
            SendPose(ship.Position, ship.Velocity, ship.transform.eulerAngles.z, pose.PackState(), pose.PackValue());
        }


        // ------------------------------------------------------------------------------------------------
        // For the manager
        // ------------------------------------------------------------------------------------------------

        /// <summary>The simulator's objective is complete: the server declares the victory and finishes the room a moment later.</summary>
        internal void MissionWon()
        {
            if (IsPlaying && Client != null && Client.Connection != null)
            {
                Client.Connection.Reducers.CompleteMission();
            }
        }


        /// <summary>The local pilot is out of ships: the others fly on, and the mission ends when nobody does.</summary>
        internal void PilotOut(long score)
        {
            if (IsPlaying)
            {
                reportedScore = score;
                Server.FinishPlaying(score);
            }
        }


        internal void ReportScoreNow(long score)
        {
            if (!IsPlaying || Server.LocalMember == null || !Server.LocalMember.Playing)
            {
                return;
            }
            reportedScore = score;
            nextScore = Time.unscaledTime + ScoreInterval;
            Server.ReportScore(score);
        }


        /// <summary>The pilots of the room by place (by score while there are no places), a line each, for the results.</summary>
        internal string DescribeStandings()
        {
            var members = new List<RoomMemberInfo>(Server.Members);
            long Score(RoomMemberInfo member) => Server.IsLocal(member) && Asteroids != null && member.Place == 0 ? Asteroids.Scoring.Score : member.Score;
            int[] places;
            if (members.TrueForAll(member => member.Place > 0))
            {
                Standings.SortByPlace(members, member => member.Place, member => member.Seat);
                places = members.ConvertAll(member => (int)member.Place).ToArray();
            }
            else
            {
                places = Standings.Rank(members, Score, member => member.Seat);
            }
            var text = new StringBuilder();
            for (int i = 0; i < members.Count; i++)
            {
                RoomMemberInfo member = members[i];
                text.Append(i > 0 ? "\n" : string.Empty).Append(Standings.Line(places[i], NameOf(member), Server.IsLocal(member),
                    Score(member).ToString("N0", CultureInfo.InvariantCulture), CoopRules.SeatColor(member.Seat)));
            }
            return text.ToString();
        }


        // ------------------------------------------------------------------------------------------------
        // The other pilots
        // ------------------------------------------------------------------------------------------------

        /// <summary>A stand-in for every other member of the room; the ones of members who left go.</summary>
        private void SyncShips()
        {
            AsteroidsGameManager manager = Asteroids;
            if (manager == null || shipPrefab == null)
            {
                return;
            }
            var present = new HashSet<Identity>();
            IReadOnlyList<RoomMemberInfo> members = Server.Members;
            for (int i = 0; i < members.Count; i++)
            {
                RoomMemberInfo member = members[i];
                if (Server.IsLocal(member))
                {
                    continue;
                }
                present.Add(member.Identity);
                if (ships.TryGetValue(member.Identity, out RemoteShip known) && known != null)
                {
                    continue;
                }
                AsteroidsPlayer instance = Instantiate(shipPrefab, manager.Ship != null ? manager.Ship.transform.parent : null);
                var remote = instance.gameObject.AddComponent<RemoteShip>();
                remote.Setup(manager.Field, member.Seat, NameOf(member), manager.Hangar, manager.MissionHullStrength,
                    FieldMath.StartPoint(i, members.Count, CoopRules.StartRadius));
                ships[member.Identity] = remote;
                manager.AddRemoteShip(remote);
                if (Server.Poses.TryGetValue(member.Identity, out RoomPoseInfo pose))
                {
                    remote.Pose(pose);
                }
            }
            var gone = new List<Identity>();
            foreach (KeyValuePair<Identity, RemoteShip> entry in ships)
            {
                if (!present.Contains(entry.Key))
                {
                    gone.Add(entry.Key);
                }
            }
            foreach (Identity identity in gone)
            {
                manager.RemoveRemoteShip(ships[identity]);
                ships.Remove(identity);
            }
        }


        private void ShowPilots()
        {
            AsteroidsGameManager manager = Asteroids;
            if (manager == null)
            {
                return;
            }
            var pilots = new List<AsteroidsGameManager.PilotStatus>();
            foreach (RoomMemberInfo member in Server.Members)
            {
                bool local = Server.IsLocal(member);
                pilots.Add(new AsteroidsGameManager.PilotStatus
                {
                    Seat = member.Seat,
                    Name = NameOf(member),
                    Score = local ? manager.Scoring.Score : member.Score,
                    Flying = member.Playing || (Room != null && !Room.IsPlaying),
                    Local = local
                });
            }
            manager.ShowPilots(pilots);
        }
    }
}
