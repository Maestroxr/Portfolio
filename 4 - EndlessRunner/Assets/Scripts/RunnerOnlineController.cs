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
    /// </summary>
    public class RunnerOnlineController : OnlineGameController
    {
        private readonly List<CoinClaim> arrivedClaims = new List<CoinClaim>();
        private readonly Dictionary<Identity, int> seats = new Dictionary<Identity, int>();
        private readonly List<RunnerGameManager.RaceMember> members = new List<RunnerGameManager.RaceMember>();
        private readonly Dictionary<int, int> trackCoins = new Dictionary<int, int>();
        private List<RoomLevelChoice> levelChoices;

        public RunnerGameManager Runner => BaseManager as RunnerGameManager;

        private GameServerClient Client => Server as GameServerClient;

        /// <summary>The levels of the campaign and the endless run; everybody may run any of them with friends.</summary>
        public override IReadOnlyList<RoomLevelChoice> LevelChoices
        {
            get
            {
                if (levelChoices != null)
                {
                    return levelChoices;
                }
                levelChoices = new List<RoomLevelChoice>();
                ICampaign campaign = BaseManager != null ? BaseManager.Campaign : null;
                for (int i = 0; campaign != null && i < campaign.Count; i++)
                {
                    if (campaign[i] is RunnerLevel level)
                    {
                        levelChoices.Add(new RoomLevelChoice(i, level.IsEndless ? level.Title : $"{i + 1}. {level.Title} ({level.Length:0} m)"));
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
            return coins > 0 ? $"{base.DescribeRoom(room)}, {coins} coins" : base.DescribeRoom(room);
        }

        /// <summary>The local runner touched a coin: the server says who gets it, with a claim row for everybody.</summary>
        public void Claim(int piece, int value)
        {
            if (IsPlaying && Client != null && Client.Connection != null)
            {
                Client.Connection.Reducers.ClaimPiece((uint)piece, (byte)value);
            }
        }

        /// <summary>The pose of the local runner, every frame of a race; the base class sends it at the pose rate.</summary>
        public void SendRunnerPose(Vector3 position, Vector3 velocity, RunnerPoseState state, int hearts)
        {
            SendPose(position, velocity, 0f, state.Pack(), hearts);
        }

        /// <summary>The score of the local runner so far, for the scoreboards of the others.</summary>
        public void ReportScore(long score)
        {
            if (IsPlaying)
            {
                Server.ReportScore(score);
            }
        }

        /// <summary>The local run is over: the race finishes when the last runner says so.</summary>
        public void FinishRun(float distance, long score)
        {
            if (IsPlaying && Client != null && Client.Connection != null)
            {
                Client.Connection.Reducers.FinishRun((uint)Mathf.Max(0, Mathf.FloorToInt(distance)), score);
            }
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

        /// <summary>The host started the race: the manager gets the runners and the seed, then loads the level and starts.</summary>
        protected override void OnRoomStarted(RoomInfo room)
        {
            RunnerGameManager manager = Runner;
            if (manager == null)
            {
                Report("This scene has no runner to race with.");
                return;
            }
            seats.Clear();
            var setup = new RunnerGameManager.RaceSetup
            {
                // A seed of the room fits a System.Random as long as it is positive.
                Seed = Mathf.Max(1, (int)(room.Seed & int.MaxValue)),
                LocalSeat = Server.LocalSeat
            };
            foreach (RoomMemberInfo member in Server.Members)
            {
                seats[member.Identity] = member.Seat;
                setup.Racers.Add(new Racer { Seat = member.Seat, Name = NameOf(member), Local = Server.IsLocal(member) });
            }
            manager.PrepareRace(setup);
            base.OnRoomStarted(room);
            PassMembers();
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
