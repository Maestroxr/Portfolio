using System;
using System.Collections.Generic;
using Gamebox;
using Gamebox.Online;
using Gamebox.Racing;
using Portfolio.DeathRace.Server;
using SpacetimeDB;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// Online races, with people on other devices, in a room of the game's SpacetimeDB database (Server/Lib.cs). Rooms,
    /// ready and start, the poses, the action log and the places at the end are the base server's and the base
    /// <see cref="OnlineGameController"/>'s; this class adds the race. A room races a track (its level) with a number of
    /// laps and one car for everybody. When the host starts it, every device builds the same race from the room
    /// (<see cref="OnlineRaceSetup.Build"/>): the people at this device (the device's player and its companions, each on their own
    /// controls and in their own view) drive real cars, everybody else is a <see cref="RemoteCar"/> that follows the poses
    /// their device sends (about 15 a second: the place, the speed, the heading, the gun, the nitro, the brakes, the
    /// wreck and the health).
    ///
    /// What one car does to another is judged by the device of the car it is done to: a remote car's gun fires here
    /// too and its rounds hurt the cars of this device; mines and rockets are actions of the room, so every device lays
    /// every mine and launches every rocket, and each device's cars take their own blasts. A pickup belongs to the first
    /// device that claims it; a mine goes off at once for the car that drove into it, and its claim tells the other
    /// devices to blow their copy up. A car of this device that crosses the line for the last time, is wrecked (its wreck
    /// names the killer, who gets the kill), or is still running when the time after the first finisher runs out
    /// reports its result (<see cref="OnlineRaceRules.Score"/>); the server ranks the room when the last racer did, and
    /// the standings show (the places are the server's, with every racer's kills, time and best lap).
    /// </summary>
    public class DeathRaceOnlineController : OnlineGameController
    {
        /// <summary>The time the others have once the first car is over the line, in seconds.</summary>
        public const float GraceSeconds = 30f;

        /// <summary>How often the racers of this device say how far they got (what an early end ranks by).</summary>
        public const float ScoreInterval = 2f;

        /// <summary>How long the host waits after the grace for a device that never reported before it ends the race.</summary>
        public const float EndPatience = 15f;

        private static readonly string[] LapValues = { "0", "2", "3", "4", "5", "6", "8" };
        private static readonly string[] LapLabels = { "Track's own", "2", "3", "4", "5", "6", "8" };

        private readonly List<RaceClaim> arrivedClaims = new List<RaceClaim>();
        private readonly Dictionary<Identity, RaceRacer> racers = new Dictionary<Identity, RaceRacer>();
        private readonly Dictionary<Identity, byte> seats = new Dictionary<Identity, byte>();
        private readonly Dictionary<byte, Car> cars = new Dictionary<byte, Car>();
        private readonly Dictionary<byte, Car> localCars = new Dictionary<byte, Car>();
        private readonly Dictionary<byte, RemoteCar> remotes = new Dictionary<byte, RemoteCar>();
        private readonly Dictionary<uint, Mine> mines = new Dictionary<uint, Mine>();
        private readonly Dictionary<Mine, uint> mineItems = new Dictionary<Mine, uint>();
        private readonly HashSet<uint> pendingPickups = new HashSet<uint>();
        private readonly HashSet<int> takenWhileHidden = new HashSet<int>();
        private readonly HashSet<byte> reported = new HashSet<byte>();
        private readonly List<Mine> touched = new List<Mine>();
        private readonly List<int> returned = new List<int>();
        private int[] pickupGenerations = new int[0];
        private RaceSetup pendingSetup;
        private Race race;
        private float graceEnds = -1f;
        private float nextScoreReport;
        private bool endAsked;
        private List<RoomLevelChoice> levelChoices;
        private string choicesLanguage;

        public DeathRaceManager Game => BaseManager as DeathRaceManager;

        private GameServerClient Client => Server as GameServerClient;

        /// <summary>The online race being run, or null.</summary>
        public Race OnlineRace => race;

        /// <summary>The setup of the online race (or of the last one).</summary>
        public RaceSetup LastSetup { get; private set; }

        /// <summary>The standings of the last race of the room, as they were shown.</summary>
        public RaceResult LastStandings { get; private set; }

        /// <summary>What the server knows of the racers of the room's race, by player.</summary>
        public IReadOnlyDictionary<Identity, RaceRacer> Racers => racers;

        /// <summary>When the time after the first finisher runs out (unscaled time), or -1.</summary>
        public float GraceEnds => graceEnds;

        /// <summary>The cars of this device in the online race, by room seat.</summary>
        public IReadOnlyDictionary<byte, Car> LocalCars => localCars;

        /// <summary>The cars of the other devices, by room seat.</summary>
        public IReadOnlyDictionary<byte, RemoteCar> RemoteCars => remotes;

        /// <summary>Claims answered by the server this race: pickups taken, mines set off (for the tours).</summary>
        public int ClaimsSeen { get; private set; }

        /// <summary>A line about what happened online (for the tours' logs).</summary>
        public event Action<string> Noted;

        // ------------------------------------------------------------------------------------------------
        // The lobby: tracks, options, rooms
        // ------------------------------------------------------------------------------------------------

        /// <summary>Every track, by its name in the language shown.</summary>
        public override IReadOnlyList<RoomLevelChoice> AllLevelChoices
        {
            get
            {
                if (levelChoices != null && choicesLanguage == GameLanguages.Code)
                {
                    return levelChoices;
                }
                choicesLanguage = GameLanguages.Code;
                levelChoices = new List<RoomLevelChoice>();
                for (int i = 0; i < TrackLayouts.Count; i++)
                {
                    TrackLayout layout = TrackLayouts.Of(i);
                    levelChoices.Add(new RoomLevelChoice(i, L.F("{0} ({1} laps)", L.Data(layout.Name), layout.Laps)));
                }
                return levelChoices;
            }
        }

        /// <summary>A room races the tracks opened online or at this device (the arena of the Showdown once the champion is beaten).</summary>
        public override bool IsUnlockedOnline(int level)
        {
            DeathRaceManager game = Game;
            if (game != null && game.Tracks != null && game.Tracks.IsUnlocked(level, game.Progress))
            {
                return true;
            }
            return base.IsUnlockedOnline(level);
        }

        public override IReadOnlyList<RoomOptionSpec> OptionSpecs
        {
            get
            {
                var carValues = new string[CarSpecs.Count];
                var carLabels = new string[CarSpecs.Count];
                for (int i = 0; i < CarSpecs.Count; i++)
                {
                    carValues[i] = i.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    carLabels[i] = CarSpecs.All[i].Name;
                }
                return new[]
                {
                    new RoomOptionSpec(OnlineRaceRules.LapsOption, "Laps", LapValues, LapLabels, 0),
                    new RoomOptionSpec(OnlineRaceRules.CarOption, "Car", carValues, carLabels, (int)DeathRaceLocalRules.DefaultCar)
                };
            }
        }

        public override int MaxPlayersLimit => DeathRaceLocalRules.GridSize;

        /// <summary>
        /// The options of a room: the host's laps and car, plus the track's own laps and its length for the server, which
        /// knows no tracks: it holds the times and distances the racers report to them.
        /// </summary>
        public override string ComposeOptions(int level, string picked)
        {
            return OnlineRaceSetup.OptionsFor(level, picked);
        }

        /// <summary>"Dust Bowl, 4 laps, Viper".</summary>
        public override string DescribeRoom(RoomInfo room)
        {
            TrackLayout layout = TrackLayouts.Of(room.Level);
            int laps = OnlineRaceRules.LapsOf(room.Option(OnlineRaceRules.LapsOption, 0), layout.Laps);
            int car = Mathf.Clamp(room.Option(OnlineRaceRules.CarOption, (int)DeathRaceLocalRules.DefaultCar), 0, CarSpecs.Count - 1);
            return string.Join(", ", L.Data(layout.Name), L.Laps(laps), L.Data(CarSpecs.All[car].Name));
        }

        protected override CampaignProgress CreateOnlineProgress(IStorageStrategy storage)
        {
            return new DeathRaceProgress(storage);
        }

        protected override void Start()
        {
            base.Start();
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
            base.OnDestroy();
        }

        /// <summary>Claims and racers arrive in the middle of whatever else the server sent: the claims wait for the tick.</summary>
        private void Bind(DbConnection connection)
        {
            connection.Db.RaceClaim.OnInsert += (context, row) => arrivedClaims.Add(row);
            connection.Db.RaceRacer.OnInsert += (context, row) => racers[row.Player] = row;
            connection.Db.RaceRacer.OnUpdate += (context, old, row) => racers[row.Player] = row;
            connection.Db.RaceRacer.OnDelete += (context, row) => racers.Remove(row.Player);
        }

        protected override IEnumerable<string> RoomQueries(RoomInfo room)
        {
            yield return $"SELECT * FROM race_claim WHERE room_id = {room.Id}";
            yield return $"SELECT * FROM race_racer WHERE room_id = {room.Id}";
        }

        // ------------------------------------------------------------------------------------------------
        // The race of the room
        // ------------------------------------------------------------------------------------------------

        /// <summary>The host started the race: it is built here from the room, then the base class takes charge and starts it.</summary>
        protected override void OnRoomStarted(RoomInfo room)
        {
            DeathRaceManager game = Game;
            if (game == null || game.Director == null)
            {
                Report(L.T("This scene has no race to run."));
                return;
            }
            Clear();
            foreach (RoomMemberInfo member in Server.Members)
            {
                seats[member.Identity] = member.Seat;
            }
            DeathRaceTheme theme = game.Look;
            var members = new List<OnlineRaceSetup.RoomRacer>();
            foreach (RoomMemberInfo member in Server.Members)
            {
                bool local = Server.IsLocal(member);
                members.Add(new OnlineRaceSetup.RoomRacer
                {
                    Seat = member.Seat,
                    Name = NameOf(member),
                    Local = local,
                    LocalIndex = local ? LocalIndexOf(member.Seat) : -1,
                    Controls = local ? ControlsOf(member.Seat) : null
                });
            }
            pendingSetup = OnlineRaceSetup.Build(room.Level, room.Options, room.Seed, members,
                seat => theme != null ? theme.SeatColor(seat) : LocalPlayRules.DefaultColors[seat % LocalPlayRules.DefaultColors.Length]);
            base.OnRoomStarted(room);
        }

        /// <summary>The base class has the manager start the room's level: here that is the race built for it.</summary>
        protected override void ApplyPrepared(int level, bool start)
        {
            DeathRaceManager game = Game;
            if (!start || pendingSetup == null || game == null)
            {
                base.ApplyPrepared(level, start);
                return;
            }
            RaceSetup setup = pendingSetup;
            pendingSetup = null;
            LastSetup = setup;
            RaceDirector director = game.Director;
            director.DriverFor = entrant => entrant.Driver == DriverKind.Remote ? new RemoteDriver() : null;
            director.CarSpawned += OnCarSpawned;
            try
            {
                game.BeginRace(setup);
            }
            finally
            {
                director.DriverFor = null;
                director.CarSpawned -= OnCarSpawned;
            }
            race = director.Current;
            if (race == null)
            {
                return;
            }
            if (race.Pickups != null)
            {
                race.Pickups.Claim = ClaimPickup;
                pickupGenerations = new int[race.Pickups.Pickups.Count];
            }
            nextScoreReport = Time.unscaledTime + ScoreInterval;
            Note($"race started: track {setup.Track}, {setup.Laps} laps, {setup.Entrants.Count} cars, {localCars.Count} here");
        }

        private void OnCarSpawned(Car car)
        {
            RaceEntrant entrant = car.Entrant;
            if (entrant == null || entrant.RoomSeat < 0)
            {
                return;
            }
            var seat = (byte)entrant.RoomSeat;
            cars[seat] = car;
            if (entrant.Driver == DriverKind.Remote)
            {
                remotes[seat] = RemoteCar.Attach(car, car.Driver as RemoteDriver ?? new RemoteDriver(), seat);
                return;
            }
            localCars[seat] = car;
            if (car.Weapons != null)
            {
                car.Weapons.MineDropped += mine => OnLocalMine(seat, mine);
                car.Weapons.RocketFired += rocket => OnLocalRocket(seat, rocket);
            }
            car.Wrecked += (wrecked, cause) => OnLocalWrecked(seat, wrecked, cause);
            car.Happened += happened =>
            {
                if (happened.Kind == CarEventKind.Finished)
                {
                    StartGrace();
                    ReportResult(seat, car);
                }
            };
        }

        /// <summary>The race of the room is not this device's any more (left, aborted, a new one): everything of it goes.</summary>
        private void Clear()
        {
            race = null;
            pendingSetup = null;
            cars.Clear();
            localCars.Clear();
            remotes.Clear();
            mines.Clear();
            mineItems.Clear();
            pendingPickups.Clear();
            takenWhileHidden.Clear();
            reported.Clear();
            arrivedClaims.Clear();
            pickupGenerations = new int[0];
            graceEnds = -1f;
            endAsked = false;
            ClaimsSeen = 0;
        }

        private bool RaceIsOn => race != null && Game != null && Game.Director != null && Game.Director.Current == race && InCharge;

        protected override void Update()
        {
            base.Update();
            if (race != null && !RaceIsOn)
            {
                Clear();
                return;
            }
            if (!RaceIsOn)
            {
                return;
            }
            SendPoses();
            ReportScores();
            TakeReturnedPickups();
            if (graceEnds >= 0f && Time.unscaledTime >= graceEnds)
            {
                foreach (KeyValuePair<byte, Car> local in localCars)
                {
                    ReportResult(local.Key, local.Value);
                }
                if (IsHost && !endAsked && Room != null && Room.IsPlaying && Time.unscaledTime >= graceEnds + EndPatience)
                {
                    // A device that never said how its race ended (it hung, it lost the connection) does not hold the room.
                    endAsked = true;
                    Note("the host ends the race: a racer never reported");
                    Server.EndRoom(Failed);
                }
            }
        }

        private void FixedUpdate()
        {
            if (RaceIsOn && race.IsRunning)
            {
                WatchMines();
            }
        }

        // ------------------------------------------------------------------------------------------------
        // What goes out
        // ------------------------------------------------------------------------------------------------

        /// <summary>The seat a call for the car at <paramref name="seat"/> names: none for the device's own (the caller), else the companion's.</summary>
        private byte SeatArgument(byte seat)
        {
            return seat == LocalSeat ? RoomTurnInfo.NoSeat : seat;
        }

        private void SendPoses()
        {
            foreach (KeyValuePair<byte, Car> local in localCars)
            {
                SendPoseOf(local.Key, local.Value);
            }
        }

        /// <summary>
        /// The pose of the car of this device at <paramref name="seat"/>: at the pose rate, and at once when its bits
        /// changed. A racer who reported the end of their race sends no more (the server takes no poses from them), so the
        /// last pose goes out before the report: the others see the car finished or wrecked.
        /// </summary>
        private void SendPoseOf(byte seat, Car car)
        {
            if (car == null || car.Body == null || reported.Contains(seat))
            {
                return;
            }
            bool firing = car.Weapons != null && car.Weapons.Firing && car.WeaponsLive;
            bool nitro = car.Weapons != null && car.Weapons.NitroActive;
            uint state = OnlineRaceRules.PackState(firing, nitro, car.Body.Braking, car.IsWrecked, car.HasFinished);
            int health = OnlineRaceRules.PackHealth(car.Health != null && !car.IsWrecked ? car.Health.Health01 : 0f);
            Vector3 position = car.Body.Rigidbody != null ? car.Body.Rigidbody.position : car.transform.position;
            SendPose(SeatArgument(seat), position, car.Body.Velocity, car.transform.eulerAngles.y, state, health);
        }

        /// <summary>Every few seconds the racers still going say how far they got: an early end of the room ranks by it.</summary>
        private void ReportScores()
        {
            if (Time.unscaledTime < nextScoreReport || !race.IsRunning)
            {
                return;
            }
            nextScoreReport = Time.unscaledTime + ScoreInterval;
            foreach (KeyValuePair<byte, Car> local in localCars)
            {
                Car car = local.Value;
                if (car == null || car.Racer == null || reported.Contains(local.Key))
                {
                    continue;
                }
                ReportScore(SeatArgument(local.Key), OnlineRaceRules.Score(false, car.IsWrecked, 0, Centimetres(car.Racer.Progress)));
            }
        }

        private void OnLocalMine(byte seat, Mine mine)
        {
            if (mine == null || !RaceIsOn)
            {
                return;
            }
            // Online a mine goes off through this controller only (WatchMines and the claims), the same on every device.
            mine.Authoritative = false;
            uint item = OnlineRaceRules.MineItem(seat, mine.Id);
            Register(item, mine);
            Vector3 at = mine.Position;
            Server.SubmitAction(seat, OnlineRaceRules.MineAction,
                OnlineRaceRules.EncodeMine(new OnlineRaceRules.MineDrop { Id = mine.Id, X = at.x, Z = at.z, Yaw = mine.transform.eulerAngles.y }), Failed);
        }

        private void OnLocalRocket(byte seat, Rocket rocket)
        {
            if (rocket == null || !RaceIsOn)
            {
                return;
            }
            Vector3 at = rocket.Position;
            Vector3 velocity = rocket.Velocity;
            Vector3 direction = new Vector3(velocity.x, 0f, velocity.z).normalized;
            Server.SubmitAction(seat, OnlineRaceRules.RocketAction, OnlineRaceRules.EncodeRocket(new OnlineRaceRules.RocketShot
            {
                X = at.x, Y = at.y, Z = at.z, DirectionX = direction.x, DirectionZ = direction.z, Speed = velocity.magnitude
            }), Failed);
        }

        /// <summary>A car of this device was wrecked: the room hears who did it, and the racer is done.</summary>
        private void OnLocalWrecked(byte seat, Car car, DamageInfo cause)
        {
            if (!RaceIsOn)
            {
                return;
            }
            int killer = cause.Attacker != null && cause.Attacker != car && cause.Attacker.Entrant != null ? cause.Attacker.Entrant.RoomSeat : -1;
            Server.SubmitAction(seat, OnlineRaceRules.WreckAction, OnlineRaceRules.EncodeWreck(killer), Failed);
            Note($"seat {seat} wrecked by {(killer >= 0 ? "seat " + killer : "nobody")}");
            ReportResult(seat, car);
        }

        /// <summary>The car of this device at <paramref name="seat"/> is done (finished, wrecked, or out of time): its result goes to the room once.</summary>
        private void ReportResult(byte seat, Car car)
        {
            if (car == null || car.Racer == null || !reported.Add(seat) || Client == null || Client.Connection == null || !IsPlaying)
            {
                return;
            }
            // The last pose first (finished or wrecked), while the server still takes poses of this racer.
            reported.Remove(seat);
            SendPoseOf(seat, car);
            reported.Add(seat);
            Racer racer = car.Racer;
            bool finished = car.HasFinished && !car.IsWrecked;
            bool wrecked = car.IsWrecked;
            float time = finished ? racer.FinishTime : wrecked && racer.OutTime >= 0f ? racer.OutTime : Mathf.Max(0f, race.Time);
            uint timeMs = (uint)Mathf.Max(0, Mathf.RoundToInt(time * 1000f));
            uint progress = (uint)Mathf.Max(0L, Centimetres(racer.Progress));
            uint bestLap = (uint)Mathf.Max(0, Mathf.RoundToInt(racer.BestLap * 1000f));
            Client.Connection.Reducers.FinishRace(SeatArgument(seat), finished, wrecked, timeMs, progress, bestLap);
            Note($"seat {seat} reports: {(finished ? "finished" : wrecked ? "wrecked" : "running")} {time:0.00} s, {racer.Progress:0} m, best lap {racer.BestLap:0.00} s, {racer.Kills} kills");
        }

        private static long Centimetres(float metres)
        {
            return (long)Math.Round(Math.Max(0f, metres) * 100.0);
        }

        /// <summary>Once somebody is over the line the others have <see cref="GraceSeconds"/> to finish.</summary>
        private void StartGrace()
        {
            if (graceEnds < 0f)
            {
                graceEnds = Time.unscaledTime + GraceSeconds;
                Note($"first finisher: the others have {GraceSeconds:0} s");
            }
        }

        // ------------------------------------------------------------------------------------------------
        // Pickups and mines
        // ------------------------------------------------------------------------------------------------

        /// <summary>A car drove over a pickup: one of this device's asks the room for it (the answer gives it); the others' devices ask for theirs.</summary>
        private bool ClaimPickup(Pickup pickup, Car car)
        {
            if (!RaceIsOn || pickup == null || car == null || car.Entrant == null || car.Entrant.Driver != DriverKind.Human || car.Entrant.RoomSeat < 0)
            {
                return false;
            }
            int index = pickup.Index;
            int generation = index >= 0 && index < pickupGenerations.Length ? pickupGenerations[index] : 0;
            uint item = OnlineRaceRules.PickupItem(index, generation);
            if (pendingPickups.Add(item) && Client != null && Client.Connection != null)
            {
                Client.Connection.Reducers.ClaimPickup(SeatArgument((byte)car.Entrant.RoomSeat), (uint)index, (uint)generation);
            }
            return false;
        }

        /// <summary>
        /// The mines that a car of this device drove into: armed mines within their trigger of the car's box. Each goes off
        /// here at once (the blast lands where the car is), and its claim tells the other devices.
        /// </summary>
        private void WatchMines()
        {
            touched.Clear();
            foreach (Mine mine in Mine.Active)
            {
                if (mine == null || !mine.Armed || race.Root == null || !mine.transform.IsChildOf(race.Root))
                {
                    continue;
                }
                Vector3 trigger = mine.Position + Vector3.up * 0.6f;
                foreach (KeyValuePair<byte, Car> local in localCars)
                {
                    Car car = local.Value;
                    if (car == null || car.IsWrecked || car.Body == null || car.Body.Collider == null)
                    {
                        continue;
                    }
                    if ((car.Body.Collider.ClosestPoint(trigger) - trigger).sqrMagnitude <= CombatRules.MineTriggerRadius * CombatRules.MineTriggerRadius)
                    {
                        touched.Add(mine);
                        if (mineItems.TryGetValue(mine, out uint item) && Client != null && Client.Connection != null)
                        {
                            Client.Connection.Reducers.ClaimMine(SeatArgument(local.Key), (byte)OnlineRaceRules.MineOwnerOf(item), (uint)OnlineRaceRules.MineIdOf(item));
                        }
                        break;
                    }
                }
            }
            foreach (Mine mine in touched)
            {
                Forget(mine);
                mine.Detonate();
            }
        }

        private void Register(uint item, Mine mine)
        {
            mines[item] = mine;
            mineItems[mine] = item;
        }

        private void Forget(Mine mine)
        {
            if (mine != null && mineItems.TryGetValue(mine, out uint item))
            {
                mineItems.Remove(mine);
                mines.Remove(item);
            }
        }

        /// <summary>A pickup somebody claimed while it was still away here goes again the moment it is back.</summary>
        private void TakeReturnedPickups()
        {
            if (takenWhileHidden.Count == 0)
            {
                return;
            }
            returned.Clear();
            PickupField field = race.Pickups;
            foreach (int index in takenWhileHidden)
            {
                Pickup pickup = field != null && index < field.Pickups.Count ? field.Pickups[index] : null;
                if (pickup == null || pickup.Available)
                {
                    field?.Take(index);
                    returned.Add(index);
                }
            }
            foreach (int index in returned)
            {
                takenWhileHidden.Remove(index);
            }
        }

        /// <summary>The answers of the room: who got a pickup, which mine went off.</summary>
        protected override void OnServerTick()
        {
            if (arrivedClaims.Count == 0)
            {
                return;
            }
            RoomInfo room = Room;
            if (RaceIsOn && room != null)
            {
                foreach (RaceClaim claim in arrivedClaims)
                {
                    if (claim.RoomId == room.Id)
                    {
                        TakeClaim(claim);
                    }
                }
            }
            // Without a race the rows are those of a race that was over before the player came to the room.
            arrivedClaims.Clear();
        }

        private void TakeClaim(RaceClaim claim)
        {
            ClaimsSeen++;
            if (claim.Kind == OnlineRaceRules.MineClaim)
            {
                if (mines.TryGetValue(claim.Item, out Mine mine))
                {
                    Forget(mine);
                    if (mine != null)
                    {
                        mine.Detonate();
                    }
                }
                Note($"mine {OnlineRaceRules.MineOwnerOf(claim.Item)}/{OnlineRaceRules.MineIdOf(claim.Item)} set off by seat {claim.Seat}");
                return;
            }
            PickupField field = race.Pickups;
            int index = OnlineRaceRules.PickupIndexOf(claim.Item);
            int generation = OnlineRaceRules.PickupGenerationOf(claim.Item);
            if (field == null || index >= field.Pickups.Count || index >= pickupGenerations.Length || generation < pickupGenerations[index])
            {
                return;
            }
            pickupGenerations[index] = generation + 1;
            pendingPickups.Remove(claim.Item);
            Pickup pickup = field.Pickups[index];
            cars.TryGetValue(claim.Seat, out Car car);
            if (pickup == null)
            {
                return;
            }
            if (!pickup.Available)
            {
                // Not back here yet (its time to return runs from when this device heard of the last claim): when it is, it goes.
                takenWhileHidden.Add(index);
            }
            else if (car != null)
            {
                field.Grant(index, car);
            }
            else
            {
                field.Take(index);
            }
            Note($"pickup {index}#{generation} ({pickup.Kind}) to seat {claim.Seat}");
        }

        // ------------------------------------------------------------------------------------------------
        // What comes in
        // ------------------------------------------------------------------------------------------------

        /// <summary>The mines, rockets and wrecks of the other devices' cars (this device's own were done when they happened).</summary>
        protected override void OnActionReceived(RoomActionInfo action)
        {
            if (!RaceIsOn || IsLocalSeat(action.Seat) || !cars.TryGetValue(action.Seat, out Car owner))
            {
                return;
            }
            DeathRaceContent content = Game.Content;
            switch (action.Kind)
            {
                case OnlineRaceRules.MineAction:
                    if (OnlineRaceRules.TryDecodeMine(action.Payload, out OnlineRaceRules.MineDrop drop))
                    {
                        var at = new Vector3(drop.X, 0f, drop.Z);
                        Mine mine = Mine.Spawn(race, content, at, Quaternion.Euler(0f, drop.Yaw, 0f), owner, drop.Id);
                        mine.Authoritative = false;
                        Register(OnlineRaceRules.MineItem(action.Seat, drop.Id), mine);
                        race.Audio?.MineDrop(at);
                    }
                    break;
                case OnlineRaceRules.RocketAction:
                    if (OnlineRaceRules.TryDecodeRocket(action.Payload, out OnlineRaceRules.RocketShot shot))
                    {
                        var at = new Vector3(shot.X, shot.Y, shot.Z);
                        Rocket rocket = Rocket.Launch(race, content, at, new Vector3(shot.DirectionX, 0f, shot.DirectionZ), shot.Speed, owner);
                        race.Effects?.Muzzle(rocket.transform);
                        race.Audio?.RocketLaunch(at);
                    }
                    break;
                case OnlineRaceRules.WreckAction:
                    if (OnlineRaceRules.TryDecodeWreck(action.Payload, out int killerSeat) && remotes.TryGetValue(action.Seat, out RemoteCar remote))
                    {
                        Car killer = killerSeat >= 0 && cars.TryGetValue((byte)killerSeat, out Car found) ? found : null;
                        remote.WreckBy(killer);
                        Note($"seat {action.Seat} wrecked by {(killerSeat >= 0 ? "seat " + killerSeat : "nobody")} (remote)");
                    }
                    break;
            }
        }

        protected override void OnPoseChanged(RoomPoseInfo pose)
        {
            if (!RaceIsOn || !seats.TryGetValue(pose.Identity, out byte seat) || !remotes.TryGetValue(seat, out RemoteCar remote) || remote == null)
            {
                return;
            }
            remote.Add(pose);
            if (pose.Has(OnlineRaceRules.FinishedBit))
            {
                StartGrace();
            }
        }

        /// <summary>A member who left the race leaves the track: their car is taken off it.</summary>
        protected override void OnMembersChanged()
        {
            if (!RaceIsOn)
            {
                return;
            }
            var present = new HashSet<byte>();
            foreach (RoomMemberInfo member in Server.Members)
            {
                present.Add(member.Seat);
            }
            foreach (KeyValuePair<byte, RemoteCar> remote in remotes)
            {
                if (!present.Contains(remote.Key) && remote.Value != null && remote.Value.gameObject.activeSelf)
                {
                    remote.Value.gameObject.SetActive(false);
                    Note($"seat {remote.Key} left the race");
                }
            }
        }

        /// <summary>The room ranked the race: the standings, then back to the room.</summary>
        protected override void OnRoomFinished(RoomInfo room)
        {
            if (!InCharge)
            {
                return;
            }
            foreach (KeyValuePair<byte, Car> local in localCars)
            {
                reported.Add(local.Key);
            }
            LastStandings = Standings();
            bool won = false;
            foreach (RoomMemberInfo member in Server.LocalMembers)
            {
                won |= member.Place == 1;
            }
            Note("room finished: " + Describe(LastStandings));
            ApplyState(won ? BaseGameState.Victory : BaseGameState.GameOver);
            Game?.ShowOnlineResults(LastStandings, BackToRoom);
        }

        /// <summary>The standings of the race as the room ranked it, with what the server knows of every racer.</summary>
        public RaceResult Standings()
        {
            var result = new RaceResult { Setup = LastSetup };
            var placed = new HashSet<byte>();
            var members = new List<RoomMemberInfo>(Server.Members);
            members.Sort((a, b) => a.Place != b.Place ? a.Place.CompareTo(b.Place) : a.Seat.CompareTo(b.Seat));
            foreach (RoomMemberInfo member in members)
            {
                placed.Add(member.Seat);
                result.Racers.Add(ResultOf(member.Seat, NameOf(member), (int)member.Place, Server.IsLocal(member), member.Identity));
            }
            // Who left during the race comes after everybody who saw it to the end.
            if (LastSetup != null)
            {
                foreach (RaceEntrant entrant in LastSetup.Entrants)
                {
                    if (entrant.RoomSeat >= 0 && !placed.Contains((byte)entrant.RoomSeat))
                    {
                        result.Racers.Add(ResultOf((byte)entrant.RoomSeat, entrant.Name, result.Racers.Count + 1, false, null));
                    }
                }
            }
            return result;
        }

        private RacerResult ResultOf(byte seat, string name, int place, bool local, Identity? identity)
        {
            RaceEntrant entrant = null;
            if (LastSetup != null)
            {
                foreach (RaceEntrant candidate in LastSetup.Entrants)
                {
                    if (candidate.RoomSeat == seat)
                    {
                        entrant = candidate;
                    }
                }
            }
            cars.TryGetValue(seat, out Car car);
            var result = new RacerResult
            {
                Seat = entrant != null ? entrant.Seat : seat,
                Name = name,
                Color = entrant != null ? entrant.Color : Color.white,
                Driver = local ? DriverKind.Human : DriverKind.Remote,
                Place = Mathf.Max(1, place),
                HealthLeft = car != null && car.Health != null && !car.IsWrecked ? car.Health.Health01 : 0f,
                CashPicked = car != null && car.Racer != null ? car.Racer.CashPicked : 0,
                LapsCompleted = car != null && car.Racer != null ? car.Racer.LapsCompleted : 0
            };
            if (identity.HasValue && racers.TryGetValue(identity.Value, out RaceRacer row))
            {
                result.Finished = row.Finished;
                result.Wrecked = row.Wrecked;
                result.Time = row.TimeMs / 1000f;
                result.BestLap = row.BestLapMs / 1000f;
                result.Kills = row.Kills;
            }
            else if (car != null && car.Racer != null)
            {
                result.Wrecked = car.IsWrecked;
                result.BestLap = car.Racer.BestLap;
            }
            return result;
        }

        /// <summary>"1. Ann finished 82.10 s 1 kill | 2. ..." for the logs.</summary>
        public static string Describe(RaceResult result)
        {
            var parts = new List<string>();
            foreach (RacerResult racer in result.Racers)
            {
                string how = racer.Finished ? $"finished {racer.Time:0.00} s" : racer.Wrecked ? "wrecked" : "running";
                parts.Add($"{racer.Place}. {racer.Name} {how}, {racer.Kills} kills, best {racer.BestLap:0.00}");
            }
            return string.Join(" | ", parts);
        }

        /// <summary>The standings' way back: the room, where the host can start the next race.</summary>
        public void BackToRoom()
        {
            Clear();
            ReturnToMenu();
        }

        public override void LeaveMatch()
        {
            Clear();
            base.LeaveMatch();
        }

        private void Note(string message)
        {
            Noted?.Invoke(message);
        }

        /// <summary>The server refused a call of the race: the player hears why, the log keeps it.</summary>
        private void Failed(string error)
        {
            if (error != null)
            {
                Note("refused: " + error);
                Report(error);
            }
        }
    }
}
