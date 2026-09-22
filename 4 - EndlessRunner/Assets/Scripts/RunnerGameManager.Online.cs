using System.Collections.Generic;
using System.Text;
using Gamebox;
using Gamebox.Online;
using UnityEngine;

namespace Portfolio.EndlessRunner
{
    /// <summary>
    /// The online half of the manager: a race against the other runners of a room (<see cref="RunnerOnlineController"/>).
    /// Everybody runs the same track on their own device, laid out from the level's own settings and the seed of the
    /// room, and sees the others as ghosts that follow the poses they send. The coins are shared: touching one hides it
    /// and asks the server for it, and it counts when the server says it is ours, while the coins the others got leave
    /// the track as their ghosts reach them. A runner who is over the line or out of hearts reports the run and watches
    /// the ones still running, until the server has the places and the results show the standings.
    /// </summary>
    public partial class RunnerGameManager
    {
        /// <summary>What the online controller knows about the race that is about to start.</summary>
        internal sealed class RaceSetup
        {
            /// <summary>The seed of the room, which lays out the endless run the same for everybody.</summary>
            public int Seed;
            public int LocalSeat;
            /// <summary>The runners by seat, the local one included; their order is the order at the start line.</summary>
            public readonly List<Racer> Racers = new List<Racer>();
        }

        /// <summary>A member of the room as the server has it while a race runs, and when it is over.</summary>
        internal struct RaceMember
        {
            public int Seat;
            public long Score;
            public int Place;
            /// <summary>Still running; false once the run is over.</summary>
            public bool Playing;
        }

        /// <summary>A coin another runner got that waits for the ghost of that runner to pick it up.</summary>
        private struct LeavingPiece
        {
            public int Id;
            public Collidable Piece;
            public int Seat;
            public float Until;
        }

        /// <summary>Lane and meters behind the line of the runners of a race, by their place at the start.</summary>
        private static readonly int[] StartLanes = { 0, -1, 1, 0 };
        private static readonly float[] StartSetbacks = { 0f, 0f, 0f, 2.5f };
        /// <summary>Ghosts are shown a little beside their lane, so runners in one lane do not hide each other.</summary>
        private static readonly float[] GhostSideOffsets = { 0.35f, -0.35f, 0.7f, -0.7f };
        private const float LeavingTimeout = 0.6f;

        [Header("Online")]
        [SerializeField] internal RunnerOnlineController online;
        [Tooltip("The runner prefab: the ghosts of the other runners of a race are made from it.")]
        [SerializeField] internal GameObject ghostPrefab;
        [SerializeField] internal Material ghostLabelMaterial;
        [Tooltip("Colours of the runners of a race, by their place at the start.")]
        [SerializeField] internal Color[] racerColors =
        {
            new Color(0.3f, 0.65f, 1f), new Color(1f, 0.55f, 0.25f), new Color(0.45f, 0.9f, 0.4f), new Color(0.8f, 0.5f, 1f)
        };
        [Tooltip("The most track kept behind the runner for a slower ghost to run on, in meters.")]
        [SerializeField] internal float raceKeepBehind = 90f;
        [Tooltip("Seconds between the scores the runner reports to the room during a race.")]
        [SerializeField] internal float scoreReportInterval = 1f;

        private readonly CoinClaims claims = new CoinClaims();
        private readonly Dictionary<int, RunnerGhost> ghosts = new Dictionary<int, RunnerGhost>();
        private readonly List<Racer> racers = new List<Racer>();
        private readonly List<Racer> ranking = new List<Racer>();
        private readonly List<LeavingPiece> leaving = new List<LeavingPiece>();
        private RaceSetup race;
        private bool raceFinished;
        private bool runEnded;
        private int startedRunners;
        private float nextScoreReport;
        private long reportedScore = -1;
        private string watchNotice;

        /// <summary>A race with the runners of a room is on: from its start until the player is back in the menu.</summary>
        internal bool InRace => race != null;

        /// <summary>The runners of the race, the local one included.</summary>
        internal IReadOnlyList<Racer> Racers => racers;

        /// <summary>Who got which coin of the race.</summary>
        internal CoinClaims Claims => claims;

        /// <summary>The seat of the local runner in the race, or -1.</summary>
        internal int LocalSeat => race != null ? race.LocalSeat : -1;

        /// <summary>The local run of the race is over and the player watches the others.</summary>
        internal bool IsWatching => phase == RunPhase.Watching;


        #region Menu

        /// <summary>The multiplayer button of the level select: opens the lobby of the game's server.</summary>
        public void OpenOnline()
        {
            if (phase != RunPhase.Menu)
            {
                return;
            }
            sounds?.Play(sounds.click);
            if (online == null)
            {
                UI?.UpdateError("Online play is not set up in this scene.");
                return;
            }
            online.OpenLobby();
        }


        /// <summary>
        /// Coin value of the track of a level as a race runs it (the level's own settings and seed): what the host tells
        /// the server about the track. 0 for the endless run.
        /// </summary>
        internal int RaceCoins(int index)
        {
            if (!(campaign != null && campaign[index] is RunnerLevel level) || track == null || runner == null)
            {
                return 0;
            }
            return track.CoinsOf(level, level.Settings, level.Seed, runner.Gravity);
        }

        #endregion


        #region Setting up

        /// <summary>The online controller hands over the race that is about to start; the base controller starts the level next.</summary>
        internal void PrepareRace(RaceSetup setup)
        {
            ClearRace();
            race = setup;
            for (int i = 0; i < setup.Racers.Count; i++)
            {
                Racer racer = setup.Racers[i];
                racer.Slot = i;
                racers.Add(racer);
            }
        }


        /// <summary>A session took charge of the game, or gave it back: a race does not outlive its session.</summary>
        protected override void OnSessionChanged()
        {
            if (!InSession)
            {
                ClearRace();
            }
        }


        /// <summary>The seed of the track: the level's own, and for the endless run one of the room, or of the dice alone.</summary>
        private int TrackSeed(RunnerLevel level)
        {
            if (!level.IsEndless)
            {
                return level.Seed;
            }
            return race != null ? race.Seed : Random.Range(1, int.MaxValue);
        }


        private int LocalSlot
        {
            get
            {
                Racer local = race != null ? racers.Find(racer => racer.Local) : null;
                return local != null ? local.Slot : 0;
            }
        }


        private static int StartLane(int slot)
        {
            return StartLanes[Mathf.Clamp(slot, 0, StartLanes.Length - 1)];
        }


        /// <summary>Where a runner stands at the start: alone on the line, side by side in a race.</summary>
        private Vector3 StartPositionOf(int slot)
        {
            float setback = StartSetbacks[Mathf.Clamp(slot, 0, StartSetbacks.Length - 1)];
            float laneWidth = runner != null ? runner.LaneWidth : LayoutBuilder.LaneWidth;
            return StartPosition + new Vector3(StartLane(slot) * laneWidth, 0f, -setback);
        }


        private Color RacerColor(int slot)
        {
            return racerColors != null && racerColors.Length > 0 ? racerColors[Mathf.Abs(slot) % racerColors.Length] : Color.white;
        }


        /// <summary>The run of a race begins: the ghosts of the others line up beside the runner, and the race HUD shows.</summary>
        private void BeginRace()
        {
            raceFinished = false;
            runEnded = false;
            startedRunners = racers.Count;
            reportedScore = -1;
            nextScoreReport = Time.unscaledTime + scoreReportInterval;
            watchNotice = null;
            claims.Clear();
            leaving.Clear();
            runnerCamera.Watch(null);

            var active = new List<IPlayer>();
            foreach (Racer racer in racers)
            {
                racer.Distance = 0f;
                racer.Coins = 0;
                racer.Score = 0;
                racer.Done = false;
                racer.Place = 0;
                if (racer.Local)
                {
                    runner.Assign(racer.Seat, PlayerControl.Local, racer.Name);
                    active.Add(runner);
                    continue;
                }
                RunnerGhost ghost = AddGhost(racer);
                if (ghost != null)
                {
                    active.Add(ghost);
                }
            }
            ActivePlayers = active;
            ui?.ShowRace(racers.Count);
            RefreshRaceHud();
        }


        private RunnerGhost AddGhost(Racer racer)
        {
            if (ghostPrefab == null || ghosts.ContainsKey(racer.Seat))
            {
                return null;
            }
            float side = GhostSideOffsets[Mathf.Clamp(racer.Slot, 0, GhostSideOffsets.Length - 1)];
            RunnerGhost ghost = RunnerGhost.Create(ghostPrefab, runner.transform.parent, racer.Name, RacerColor(racer.Slot), ghostLabelMaterial, side);
            ghost.Assign(racer.Seat, PlayerControl.Remote, racer.Name);
            ghost.PlaceAt(StartPositionOf(racer.Slot));
            RegisterPlayer(ghost);
            ghosts[racer.Seat] = ghost;
            return ghost;
        }


        private void RemoveGhost(int seat)
        {
            if (!ghosts.TryGetValue(seat, out RunnerGhost ghost))
            {
                return;
            }
            ghosts.Remove(seat);
            if (ghost != null)
            {
                PlayerList.Remove(ghost);
                ActivePlayers.Remove(ghost);
                Destroy(ghost.gameObject);
            }
        }


        /// <summary>Forgets the race: the ghosts go, the coins are nobody's, and the runner is the only player again.</summary>
        private void ClearRace()
        {
            foreach (int seat in new List<int>(ghosts.Keys))
            {
                RemoveGhost(seat);
            }
            ghosts.Clear();
            racers.Clear();
            ranking.Clear();
            leaving.Clear();
            claims.Clear();
            bool wasRacing = race != null;
            race = null;
            raceFinished = false;
            runEnded = false;
            watchNotice = null;
            if (!wasRacing)
            {
                return;
            }
            if (phase == RunPhase.Watching)
            {
                phase = RunPhase.Menu;
            }
            // The track of the race was laid out with the level's settings; the menu shows the player's own again.
            trackReady = false;
            runnerCamera?.Watch(null);
            ui?.HideRace();
            if (runner != null)
            {
                runner.Assign(-1, PlayerControl.Local);
                ActivePlayers = new List<IPlayer> { runner };
            }
        }

        #endregion


        #region Running

        /// <summary>
        /// How much track is kept behind the runner: more in a race while a slower runner is back there, so the ghost has
        /// ground under its feet and coins ahead of it when the camera turns to it.
        /// </summary>
        private float KeepBehind(float z)
        {
            float behind = track.keepBehind;
            foreach (RunnerGhost ghost in ghosts.Values)
            {
                if (ghost != null && ghost.HasPose && !ghost.LatestState.Done)
                {
                    behind = Mathf.Max(behind, z - ghost.transform.position.z + track.keepBehind);
                }
            }
            return Mathf.Min(behind, Mathf.Max(track.keepBehind, raceKeepBehind));
        }


        /// <summary>Spawns and recycles the track around the runner at <paramref name="z"/>.</summary>
        private void UpdateTrackAround(float z)
        {
            if (race == null)
            {
                track.UpdateTrack(z, track.keepBehind);
                return;
            }
            float behind = KeepBehind(z);
            track.UpdateTrack(z, behind, behind);
        }


        /// <summary>Every frame of a race: the pose goes out, the coins of the others leave the track, the HUD follows.</summary>
        private void UpdateRace()
        {
            if (race == null)
            {
                return;
            }
            SendRacePose();
            UpdateLeavingPieces();
            if (phase == RunPhase.Running && online != null && Time.unscaledTime >= nextScoreReport)
            {
                nextScoreReport = Time.unscaledTime + scoreReportInterval;
                long score = Mathf.FloorToInt(PlayerScore);
                if (score != reportedScore)
                {
                    reportedScore = score;
                    online.ReportScore(score);
                }
            }
            RefreshRaceHud();
        }


        private void SendRacePose()
        {
            if (online == null || runEnded)
            {
                return;
            }
            var state = new RunnerPoseState
            {
                Running = runner.IsRunning,
                Grounded = runner.IsGrounded,
                Sliding = runner.IsSliding,
                Invulnerable = runner.IsInvulnerable,
                LaneChange = runner.LaneChangeDirection,
                Launched = runner.IsLaunched,
                SuperJump = runner.SuperJump,
                Shield = IsPowerUpActive(PowerUpType.Shield),
                Magnet = IsPowerUpActive(PowerUpType.Magnet),
                Dead = phase == RunPhase.Dying,
                Finished = phase == RunPhase.Finishing && !runner.IsRunning
            };
            // A runner on the ground is pressed onto it; the others need not know.
            var velocity = new Vector3(runner.LaneChangeDirection * runner.SideSpeed, runner.IsGrounded ? 0f : runner.VerticalSpeed,
                runner.IsRunning ? runner.Speed : 0f);
            online.SendRunnerPose(runner.transform.position, velocity, state, hearts);
        }


        /// <summary>The local runner touched a coin of a race: it is gone at once, and counts when the server says it is ours.</summary>
        private void ClaimCoin(Coin coin)
        {
            int multiplier = IsPowerUpActive(PowerUpType.Multiplier) ? 2 : 1;
            int piece = coin.PlacementId;
            Vector3 position = coin.transform.position;
            if (coin.IsGem)
            {
                effects?.Gem(position);
            }
            else
            {
                effects?.Coin(position);
            }
            sounds?.Coin(coin.IsGem);
            track.Recycle(coin);
            if (piece >= 0 && !runEnded && claims.Request(piece, coin.Value, multiplier))
            {
                online?.Claim(piece, coin.Value);
            }
        }


        /// <summary>The server gave a coin of the race to the runner at <paramref name="seat"/>.</summary>
        internal void RaceClaim(int piece, int seat, int value)
        {
            if (race == null)
            {
                return;
            }
            ClaimResult result = claims.Settle(piece, seat, value, race.LocalSeat);
            switch (result.Outcome)
            {
                case ClaimOutcome.Awarded:
                    coins += result.Coins;
                    coinPoints += result.Coins * pointsPerCoin;
                    PlayerScore = Mathf.Floor(distance) + coinPoints;
                    ui?.PunchCoins();
                    track.Take(piece);
                    break;
                case ClaimOutcome.Lost:
                    track.Take(piece);
                    break;
                case ClaimOutcome.Remote:
                    TakeForGhost(piece, seat);
                    break;
            }
        }


        /// <summary>
        /// Another runner got a coin. When it is out on the track here with the ghost of that runner still coming, it waits
        /// for the ghost, out of everybody's reach; otherwise it goes now, or never appears.
        /// </summary>
        private void TakeForGhost(int piece, int seat)
        {
            if (track.TryGetPiece(piece, out TrackPiece other) && !(other is Coin))
            {
                // Every device lays out the same track, so this one did not: another version of the game, or of a level.
                Debug.LogWarning($"A runner took piece {piece} as a coin, which is {other.name} here: the tracks of this race differ.", this);
                return;
            }
            if (track.TryGetPiece(piece, out TrackPiece live) && live is Collidable pickup && !pickup.Collected
                && ghosts.TryGetValue(seat, out RunnerGhost ghost) && ghost != null && ghost.transform.position.z < live.transform.position.z)
            {
                pickup.MarkTaken();
                leaving.Add(new LeavingPiece { Id = piece, Piece = pickup, Seat = seat, Until = Time.unscaledTime + LeavingTimeout });
                return;
            }
            track.Take(piece);
        }


        private void UpdateLeavingPieces()
        {
            for (int i = leaving.Count - 1; i >= 0; i--)
            {
                LeavingPiece item = leaving[i];
                bool there = !ghosts.TryGetValue(item.Seat, out RunnerGhost ghost) || ghost == null
                    || ghost.transform.position.z >= item.Piece.transform.position.z - 0.5f;
                if (!there && Time.unscaledTime < item.Until && item.Piece.Live && item.Piece.PlacementId == item.Id)
                {
                    continue;
                }
                if (item.Piece.Live && item.Piece.PlacementId == item.Id)
                {
                    effects?.Coin(item.Piece.transform.position);
                }
                track.Take(item.Id);
                leaving.RemoveAt(i);
            }
        }


        /// <summary>A pose of another runner of the race arrived.</summary>
        internal void RacePose(int seat, RoomPoseInfo pose)
        {
            if (race != null && ghosts.TryGetValue(seat, out RunnerGhost ghost) && ghost != null)
            {
                ghost.Receive(pose);
            }
        }


        /// <summary>
        /// The members of the room as the server has them: their scores, who is done, and who is gone. A runner who left
        /// the room leaves the race.
        /// </summary>
        internal void RaceMembers(IReadOnlyList<RaceMember> members)
        {
            if (race == null)
            {
                return;
            }
            for (int i = racers.Count - 1; i >= 0; i--)
            {
                Racer racer = racers[i];
                int index = IndexOfSeat(members, racer.Seat);
                if (index < 0)
                {
                    if (!racer.Local)
                    {
                        racers.RemoveAt(i);
                        RemoveGhost(racer.Seat);
                        if (IsGameRunning)
                        {
                            ui?.Toast($"{racer.Name} left the race", RacerColor(racer.Slot));
                        }
                    }
                    continue;
                }
                RaceMember member = members[index];
                racer.Reported = member.Score;
                racer.Done |= !member.Playing;
                if (member.Place > 0)
                {
                    racer.Place = member.Place;
                }
            }
        }


        private static int IndexOfSeat(IReadOnlyList<RaceMember> members, int seat)
        {
            for (int i = 0; i < members.Count; i++)
            {
                if (members[i].Seat == seat)
                {
                    return i;
                }
            }
            return -1;
        }


        /// <summary>
        /// Brings the runners of the race up to date and shows them by their places. While the race runs a runner is worth
        /// what is known of it here (how far its ghost is, the coins the server gave it), or what it reported if that is
        /// more; when the race is over the scores and places are the server's.
        /// </summary>
        private void RefreshRaceHud()
        {
            if (race == null || ui == null)
            {
                return;
            }
            foreach (Racer racer in racers)
            {
                racer.Coins = claims.CoinsOf(racer.Seat);
                if (racer.Local)
                {
                    racer.Distance = distance;
                    racer.Score = raceFinished ? racer.Reported : Mathf.FloorToInt(PlayerScore);
                    racer.Done |= runEnded;
                    continue;
                }
                if (ghosts.TryGetValue(racer.Seat, out RunnerGhost ghost) && ghost != null && ghost.HasPose)
                {
                    // A runner jogs on behind the finish line; the run is as long as the track.
                    racer.Distance = Mathf.Max(racer.Distance, Mathf.Min(ghost.Distance, track.FinishZ));
                    racer.Done |= ghost.LatestState.Done;
                }
                long seen = RaceStandings.Score(racer.Distance, racer.Coins, pointsPerCoin);
                racer.Score = raceFinished ? racer.Reported : System.Math.Max(racer.Reported, seen);
            }
            ranking.Clear();
            ranking.AddRange(racers);
            if (raceFinished)
            {
                ranking.Sort((a, b) => a.Place != b.Place ? a.Place.CompareTo(b.Place) : a.Seat.CompareTo(b.Seat));
            }
            else
            {
                RaceStandings.Rank(ranking);
            }
            ui.UpdateRace(ranking, racerColors);
        }

        #endregion


        #region Finishing

        /// <summary>
        /// The local run of a race is over, across the line or out of hearts: the server hears how it went, and the player
        /// watches the runners who are still out there until the race is over.
        /// </summary>
        private void EndRaceRun(bool victory)
        {
            RunnerLevel level = RunnerLevel;
            PlayerScore = Mathf.Floor(distance) + coinPoints;
            int score = Mathf.FloorToInt(PlayerScore);
            if (level != null && level.IsEndless)
            {
                // The record of the endless run is about the distance, whoever else was on the track.
                progress.RecordEndless(distance, score);
            }
            runEnded = true;
            runner.StopRun();
            online?.FinishRun(distance, score);
            phase = RunPhase.Watching;
            phaseTime = 0f;
            if (raceFinished)
            {
                ShowRaceResults();
            }
        }


        /// <summary>Watching the others: the camera chases the runner in front, and the track is laid out around that one.</summary>
        private void UpdateWatching()
        {
            RunnerGhost ghost = GhostToWatch();
            runnerCamera.Watch(ghost);
            string notice = "Waiting for the results...";
            if (ghost != null)
            {
                float z = ghost.transform.position.z;
                if (z - track.keepBehind < track.RecycledUntil - LayoutBuilder.TileLength)
                {
                    track.Rewind(z);
                }
                track.UpdateTrack(z, track.keepBehind, track.keepBehind);
                UpdateWorld(z);
                notice = $"Watching {ghost.DisplayName} - the results follow when everybody is done";
            }
            // A last runner's results are there within a moment; no need to announce the wait for them.
            if (phaseTime > 0.75f && notice != watchNotice)
            {
                watchNotice = notice;
                ui?.ShowNotice(notice);
            }
        }


        /// <summary>The z the track and the world follow: the runner's, or that of the ghost the player watches.</summary>
        private float FocusZ()
        {
            RunnerGhost watched = phase == RunPhase.Watching && runnerCamera != null ? runnerCamera.Watched : null;
            return watched != null ? watched.transform.position.z : runner.transform.position.z;
        }


        /// <summary>The ghost in the picture stays there while it runs; after that the runner in front of the rest.</summary>
        private RunnerGhost GhostToWatch()
        {
            RunnerGhost current = runnerCamera.Watched;
            if (current != null && !current.LatestState.Done)
            {
                return current;
            }
            RunnerGhost best = null;
            foreach (RunnerGhost ghost in ghosts.Values)
            {
                if (ghost == null || !ghost.HasPose || ghost.LatestState.Done)
                {
                    continue;
                }
                if (best == null || ghost.Distance > best.Distance)
                {
                    best = ghost;
                }
            }
            return best != null ? best : current;
        }


        /// <summary>The server finished the race: the members have their places.</summary>
        internal void RaceFinished()
        {
            if (race == null || raceFinished)
            {
                return;
            }
            raceFinished = true;
            if (phase == RunPhase.Watching)
            {
                ShowRaceResults();
            }
            else if (phase != RunPhase.Menu && IsGameRunning)
            {
                // The race was ended for everybody while this runner was still out there.
                runEnded = true;
                runner.StopRun();
                ClearPowerUps();
                sounds?.StopMusic();
                ShowRaceResults();
            }
        }


        private void ShowRaceResults()
        {
            phase = RunPhase.Menu;
            RefreshRaceHud();
            RunnerLevel level = RunnerLevel;
            Racer local = racers.Find(racer => racer.Local);
            int place = local != null && local.Place > 0 ? local.Place : Mathf.Max(1, racers.Count);
            var standings = new StringBuilder();
            foreach (Racer racer in ranking)
            {
                string who = racer.Local ? $"{racer.Name} (you)" : racer.Name;
                string color = ColorUtility.ToHtmlStringRGB(Color.Lerp(RacerColor(racer.Slot), Color.white, 0.3f));
                standings.Append(standings.Length > 0 ? "\n" : string.Empty)
                    .Append($"{RaceStandings.Ordinal(Mathf.Max(1, racer.Place))}   <color=#{color}><b>{who}</b></color>   {racer.Score}")
                    .Append($"   <size=70%>{racer.Distance:0} m, {racer.Coins} coins</size>");
            }
            ui?.ShowResults(new RunResult
            {
                LevelTitle = level != null ? level.Title : string.Empty,
                Victory = place == 1,
                Endless = level != null && level.IsEndless,
                Coins = coins,
                Distance = distance,
                Score = local != null ? (int)local.Score : Mathf.FloorToInt(PlayerScore),
                Online = true,
                Place = place,
                // Whoever left on the way was beaten all the same.
                Runners = Mathf.Max(startedRunners, racers.Count),
                Standings = standings.ToString()
            });
            TransitionState(place == 1 ? BaseGameState.Victory : BaseGameState.GameOver);
        }

        #endregion
    }
}
