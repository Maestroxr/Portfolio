using System.Collections.Generic;
using Gamebox;
using Gamebox.Online;
using Portfolio.EndlessRunner.Server;
using SpacetimeDB;
using UnityEngine;

namespace Portfolio.EndlessRunner
{
    /// <summary>
    /// Online controller of the Endless Runner module: races with players on other devices, in a room of the game's
    /// SpacetimeDB database. Rooms, ready and start, the poses of the runners and the places at the end come from the
    /// base <see cref="OnlineGameController"/> and the base server; this class adds what is Endless Runner. A room runs a
    /// level of the campaign or the endless run, whose length and coins the host's client writes into the options of
    /// the room for the server (Server/Lib.cs), which hands out the coins of the track: a claim row says who got one.
    /// The manager runs the race itself (RunnerGameManager.Online.cs); this class passes on what arrives and what goes.
    /// Several people may race from this device (the companions of the base: players who signed in here, or guests, and
    /// play through its connection): every call for a runner names its seat, and the manager runs each of them in a part
    /// of the screen on a set of controls of their own (<see cref="OnlineGameController.ControlsOf"/>). A room that plays
    /// on a shared screen runs the same way: the screen drives the seats of its remote controls from the controls that
    /// arrive over the network, tells each remote how its runner is doing (<see cref="RunnerRemoteStatus"/>), and the
    /// remote shows the status and a pad (<see cref="RunnerRemotePanel"/>) instead of the game. A player who switches
    /// between the remote and their own device mid-race hands the seat over (<see cref="OnSeatDriverChanged"/>).
    /// </summary>
    public class RunnerOnlineController : OnlineGameController
    {
        private readonly List<CoinClaim> arrivedClaims = new List<CoinClaim>();
        private readonly Dictionary<Identity, int> seats = new Dictionary<Identity, int>();
        private readonly List<RunnerGameManager.RaceMember> members = new List<RunnerGameManager.RaceMember>();
        private readonly Dictionary<int, int> trackCoins = new Dictionary<int, int>();
        private List<RoomLevelChoice> levelChoices;
        private RunnerGameTheme choicesTheme;
        private string choicesLanguage;

        public RunnerGameManager Runner => BaseManager as RunnerGameManager;

        private GameServerClient Client => Server as GameServerClient;

        /// <summary>An endless run is open to everybody; a level of the campaign once the one before was finished online.</summary>
        public override bool IsUnlockedOnline(int level)
        {
            ICampaign campaign = BaseManager != null ? BaseManager.Campaign : null;
            return campaign != null && level >= 0 && level < campaign.Count && campaign[level] is RunnerLevel { IsEndless: true } || base.IsUnlockedOnline(level);
        }

        /// <summary>The levels of the campaign and the endless runs (a room runs the endless ones and those unlocked online).</summary>
        public override IReadOnlyList<RoomLevelChoice> AllLevelChoices
        {
            get
            {
                // The names are the active theme's, in the language shown; they are made again when either changed.
                if (levelChoices != null && choicesTheme == RunnerGameTheme.Active && choicesLanguage == GameLanguages.Code)
                {
                    return levelChoices;
                }
                choicesTheme = RunnerGameTheme.Active;
                choicesLanguage = GameLanguages.Code;
                levelChoices = new List<RoomLevelChoice>();
                ICampaign campaign = BaseManager != null ? BaseManager.Campaign : null;
                for (int i = 0; campaign != null && i < campaign.Count; i++)
                {
                    if (campaign[i] is RunnerLevel level)
                    {
                        string title = RunnerText.TitleOf(level);
                        levelChoices.Add(new RoomLevelChoice(i, level.IsEndless ? title : RunnerText.F("{0}. {1} ({2:0} m)", i + 1, title, level.Length)));
                    }
                }
                return levelChoices;
            }
        }

        /// <summary>
        /// The options of a room: the track of the level in numbers, for the server, which does not know the levels of
        /// the game. It holds the runners to them: nobody runs further than the track is long or takes more coins than lie
        /// on it.
        /// </summary>
        public override string ComposeOptions(int level, string picked)
        {
            ICampaign campaign = BaseManager != null ? BaseManager.Campaign : null;
            if (!(campaign != null && campaign[level] is RunnerLevel track))
            {
                return picked;
            }
            if (!trackCoins.TryGetValue(level, out int coins))
            {
                coins = Runner != null ? Runner.RaceCoins(level) : 0;
                trackCoins[level] = coins;
            }
            return RoomOptions.Write("level", level, "length", track.IsEndless ? 0 : Mathf.RoundToInt(track.Length), "coins", coins);
        }

        public override string DescribeRoom(RoomInfo room)
        {
            int coins = room.Option("coins", 0);
            return coins > 0 ? RunnerText.SayF("{0}, {1} coins", base.DescribeRoom(room), coins) : base.DescribeRoom(room);
        }

        /// <summary>The runner at this device at <paramref name="seat"/> touched a coin: the server says who gets it, with a claim row for everybody.</summary>
        public void Claim(int seat, int piece, int value)
        {
            if (IsPlaying && Client != null && Client.Connection != null)
            {
                Client.Connection.Reducers.ClaimPiece(SeatArgument(seat), (uint)piece, (byte)value);
            }
        }

        /// <summary>
        /// The pose of the runner at this device at <paramref name="seat"/>, every frame of a race; the base class sends it
        /// at the pose rate of each seat.
        /// </summary>
        public void SendRunnerPose(int seat, Vector3 position, Vector3 velocity, RunnerPoseState state, int hearts)
        {
            SendPose(SeatArgument(seat), position, velocity, 0f, state.Pack(), hearts);
        }

        /// <summary>The score so far of the runner at this device at <paramref name="seat"/>, for the scoreboards of the others.</summary>
        public void ReportScore(int seat, long score)
        {
            base.ReportScore(SeatArgument(seat), score);
        }

        /// <summary>The run of the runner at this device at <paramref name="seat"/> is over: the race finishes when the last runner says so.</summary>
        public void FinishRun(int seat, float distance, long score)
        {
            if (IsPlaying && Client != null && Client.Connection != null)
            {
                Client.Connection.Reducers.FinishRun(SeatArgument(seat), (uint)Mathf.Max(0, Mathf.FloorToInt(distance)), score);
            }
        }

        /// <summary>
        /// The seat a call for the runner at <paramref name="seat"/> names: none for the device's own runner, who is the
        /// caller (<see cref="RoomTurnInfo.NoSeat"/>, as a race with one runner here always said it), else the seat of the
        /// companion it is for.
        /// </summary>
        private byte SeatArgument(int seat)
        {
            return seat < 0 || seat >= RoomTurnInfo.NoSeat || seat == LocalSeat ? RoomTurnInfo.NoSeat : (byte)seat;
        }

        protected override void Start()
        {
            base.Start();
            OnlineProgressChanged += () => levelChoices = null;
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

        /// <summary>
        /// Claims arrive in the middle of whatever else the server sent this frame, so they wait for the tick. The claims
        /// of a race are deleted when the next one starts; one that comes and goes within a frame was never there.
        /// </summary>
        private void Bind(DbConnection connection)
        {
            connection.Db.CoinClaim.OnInsert += (context, row) => arrivedClaims.Add(row);
            connection.Db.CoinClaim.OnDelete += (context, row) => arrivedClaims.RemoveAll(claim => claim.Key == row.Key);
        }

        protected override IEnumerable<string> RoomQueries(RoomInfo room)
        {
            yield return $"SELECT * FROM coin_claim WHERE room_id = {room.Id}";
        }

        /// <summary>
        /// The host started the race: the manager gets the runners and the seed, then loads the level and starts. The
        /// runners driven at this device come with their seats, the device's own first (on the shared screen, the seats
        /// of its remotes); when their runs are explicit (several here, or a remote's seat), each with its controls: the
        /// ones the local play of the game binds (the device's player on the first set), or the packets of a remote.
        /// </summary>
        protected override void OnRoomStarted(RoomInfo room)
        {
            RunnerGameManager manager = Runner;
            if (manager == null)
            {
                Report(RunnerText.T("This scene has no runner to race with."));
                return;
            }
            seats.Clear();
            var setup = new RunnerGameManager.RaceSetup
            {
                // A seed of the room fits a System.Random as long as it is positive.
                Seed = Mathf.Max(1, (int)(room.Seed & int.MaxValue)),
                LocalSeat = Server.LocalSeat,
                Screen = IsScreen
            };
            foreach (RoomMemberInfo member in Server.Members)
            {
                seats[member.Identity] = member.Seat;
                setup.Racers.Add(new Racer { Seat = member.Seat, Name = NameOf(member), Local = DrivesSeat(member.Seat) });
            }
            var driven = new List<byte>(DrivenSeats);
            driven.Sort((a, b) => DrivenIndexOf(a).CompareTo(DrivenIndexOf(b)));
            foreach (byte seat in driven)
            {
                setup.DrivenSeats.Add(seat);
            }
            if (setup.IsParty)
            {
                foreach (byte seat in driven)
                {
                    setup.Controls[seat] = ControlsOf(seat);
                }
            }
            manager.PrepareRace(setup);
            base.OnRoomStarted(room);
            PassMembers();
        }


        /// <summary>
        /// Mid-race, a seat is driven here from now on (a remote control of this screen, or the player of this device
        /// back from their remote) or no longer: the manager turns its ghost into a run from its last pose, or back.
        /// The status the screen last sent this device carries the power-ups its runner had.
        /// </summary>
        protected override void OnSeatDriverChanged(byte seat, bool drivenHere)
        {
            RunnerGameManager manager = Runner;
            if (manager == null || !manager.InRace)
            {
                return;
            }
            RoomMemberInfo member = Server.MemberAt(seat);
            RoomPoseInfo? lastPose = member != null && Server.Poses.TryGetValue(member.Identity, out RoomPoseInfo pose) ? pose : (RoomPoseInfo?)null;
            manager.SeatDriverChanged(seat, drivenHere, drivenHere ? ControlsOf(seat) : null, lastPose, drivenHere && !IsScreen ? RemoteStatus : null,
                member != null && member.Playing, member == null);
        }


        /// <summary>The remote panel of a runner: how the runner is doing, as the screen says, and the pad that moves it.</summary>
        protected override void BuildRemotePanel(RectTransform content, RemotePanelKit kit)
        {
            RunnerRemotePanel.Build(kit, this);
        }


        /// <summary>The panel went (the player plays on this device): the line it wrote in the frame goes with it.</summary>
        protected override void OnPresentationChanged()
        {
            if (!IsRemote)
            {
                RemoteUI?.SetStatusLine(null);
            }
        }

        protected override void OnServerTick()
        {
            if (arrivedClaims.Count == 0)
            {
                return;
            }
            RunnerGameManager manager = Runner;
            RoomInfo room = Room;
            if (manager != null && manager.InRace && room != null)
            {
                foreach (CoinClaim claim in arrivedClaims)
                {
                    if (claim.RoomId == room.Id)
                    {
                        manager.RaceClaim((int)claim.Piece, claim.Seat, claim.Value);
                    }
                }
            }
            // Without a race the rows are those of a race that was over before the player came to the room.
            arrivedClaims.Clear();
        }

        protected override void OnPoseChanged(RoomPoseInfo pose)
        {
            if (seats.TryGetValue(pose.Identity, out int seat))
            {
                Runner?.RacePose(seat, pose);
            }
        }

        protected override void OnMembersChanged()
        {
            PassMembers();
        }

        /// <summary>The results of the manager show the standings; the places are the server's.</summary>
        protected override Gamebox.CampaignProgress CreateOnlineProgress(IStorageStrategy storage)
        {
            return new CampaignProgress(storage, GameType.EndlessRunner);
        }

        protected override void OnRoomFinished(RoomInfo room)
        {
            if (!InCharge)
            {
                return;
            }
            PassMembers();
            Runner?.RaceFinished();
        }

        /// <summary>The scores and places are the server's; a runner whose member is gone leaves the race.</summary>
        private void PassMembers()
        {
            RunnerGameManager manager = Runner;
            if (manager == null || !manager.InRace || !InCharge)
            {
                return;
            }
            members.Clear();
            foreach (RoomMemberInfo member in Server.Members)
            {
                members.Add(new RunnerGameManager.RaceMember
                {
                    Seat = member.Seat,
                    Score = member.Score,
                    Place = (int)member.Place,
                    Playing = member.Playing
                });
            }
            manager.RaceMembers(members);
        }
    }
}
