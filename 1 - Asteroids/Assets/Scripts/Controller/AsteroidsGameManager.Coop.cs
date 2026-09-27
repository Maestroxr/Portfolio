using System.Collections.Generic;
using Gamebox;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The co-op half of the manager: a mission flown together with pilots on other devices, in a room of the game's
    /// server (<see cref="AsteroidsOnlineController"/>). Every client flies its own ship with its own lives and score
    /// against its own copy of the world. The client of the room's host simulates that world as in the single player
    /// game (waves, spawns, the objective, the boss); on the others the manager runs without a wave director, shows
    /// what the simulator reports and follows its verdict. The playfield has the same fixed size for everybody, a
    /// pilot without ships watches the others, nothing pauses and the campaign progress stays as it is. The one thing a
    /// shared mission saves is the strike pilot: a won planet strike banks the loadout of a pilot who landed (money
    /// included), like a solo one; any other end discards it. In a strike mission the guests' ground scrolls as the
    /// simulator reports it (<see cref="CoopScrollReported"/>). Keeping the copies of the world in step is not done
    /// here but in <see cref="FieldReplication"/>.
    /// </summary>
    public partial class AsteroidsGameManager
    {
        /// <summary>What the online controller hands over for the mission that is about to start.</summary>
        internal sealed class CoopSetup
        {
            /// <summary>This client simulates the world (the host of the room).</summary>
            public bool Simulates;
            public int LocalSeat;
            /// <summary>Place of the local pilot among the pilots, by seat, for the start positions.</summary>
            public int Slot;
            public int Pilots;
            public Vector2 HalfSize;
            public int Lives;
            /// <summary>Strike: the room's difficulty, which wins for this room without changing the pilot's own.</summary>
            public StrikeDifficulty Difficulty = StrikeDifficulty.Veteran;
        }


        /// <summary>A line of the pilots list on the HUD.</summary>
        internal struct PilotStatus
        {
            public int Seat;
            public string Name;
            public long Score;
            public bool Flying;
            public bool Local;
        }


        [SerializeField] internal AsteroidsOnlineController online;

        private readonly List<RemoteShip> remoteShips = new List<RemoteShip>();
        private CoopSetup coop;
        private string coopStatus = string.Empty;
        private float coopProgress;
        private int coopWave;
        private int coopWavesCleared;

        // A guest's strike scroll: what the simulator reported last and when it arrived.
        private bool coopScrollKnown;
        private float coopScrollFollowed;
        private float coopScrollDistance;
        private float coopScrollSpeed;
        private float coopScrollAt;
        private int coopScrollFrame = -1;

        // The co-op strike results on show, shown again when the places come in.
        private StrikeResult? coopStrikeResult;

        /// <summary>A guest's ground this far (m) from where the simulator's is jumps there at once instead of catching up.</summary>
        private const float CoopScrollSnap = 4f;

        /// <summary>How fast a guest's ground closes a small gap to the simulator's, per second.</summary>
        private const float CoopScrollEase = 2f;

        /// <summary>A report older than this (s) is not carried further: the simulator went quiet.</summary>
        private const float CoopScrollLead = 1f;

        /// <summary>The mission is flown with other pilots.</summary>
        internal bool IsCoop => coop != null && InSession;

        /// <summary>This client runs the waves and judges the objective: always, except as a guest of a shared mission.</summary>
        internal bool Simulates => !IsCoop || coop.Simulates;

        /// <summary>The HUD shows the objective as the simulator reports it.</summary>
        private bool FollowsSimulator => IsCoop && !coop.Simulates;

        /// <summary>A guest of a shared strike mission: its ground scrolls as the simulator reports, not by a director of its own.</summary>
        internal bool FollowsScroll => FollowsSimulator && IsStrike;

        /// <summary>How far a guest's strike mission got toward its boss, 0 to 1, from the scroll it follows.</summary>
        internal float CoopScrollProgress => Mission is StrikeLevel strike && strike.BossAt > 0f && field != null
            ? Mathf.Clamp01(field.ScrollDistance / strike.BossAt)
            : 0f;

        /// <summary>
        /// The victory of a shared strike mission is on (the free flight and the fly-off): kills and pickups still pay,
        /// and the money goes to the server at once, since the room finishes two seconds after the win.
        /// </summary>
        internal bool InCoopVictoryWindow => IsCoop && IsStrike && IsGameRunning && phase == MissionPhase.Victory;

        /// <summary>The lobby lies over the mission select, which must not react to keys meant for it.</summary>
        internal bool IsLobbyOpen => online != null && online.LobbyUI != null && online.LobbyUI.IsOpen;

        internal IReadOnlyList<RemoteShip> RemoteShips => remoteShips;


        #region Menu

        protected override IOnlineLobby OnlineLobby => online;

        /// <summary>The multiplayer button of the mission select: opens the lobby of the game's server.</summary>
        public override void OpenOnline()
        {
            if (phase != MissionPhase.Menu)
            {
                return;
            }
            sounds?.Click();
            base.OpenOnline();
        }

        #endregion


        #region Setting up

        /// <summary>The online controller hands over the mission of the room that is about to start.</summary>
        internal void PrepareCoop(CoopSetup setup)
        {
            coop = setup;
            coopStatus = string.Empty;
            coopProgress = 0f;
            coopWave = 0;
            coopWavesCleared = 0;
            coopScrollKnown = false;
            coopScrollFollowed = 0f;
            coopScrollDistance = 0f;
            coopScrollSpeed = 0f;
            coopScrollAt = 0f;
            coopScrollFrame = -1;
            coopStrikeResult = null;
            ClearRemoteShips();
            if (spawner != null)
            {
                spawner.BossCatalog = BossCatalog();
            }
        }


        /// <summary>A session took charge or gave the game back: without one, nothing of the shared mission stays.</summary>
        protected override void OnSessionChanged()
        {
            if (InSession)
            {
                return;
            }
            coop = null;
            ClearRemoteShips();
            ui?.ShowPilots(null);
            // The pilot left the room before landing (or the room went away): the working copy of a strike mission is
            // never banked. A copy that was banked on the results is gone already.
            DiscardCoopPilot();
            coopStrikeResult = null;
            RestoreFieldWorld();
        }


        /// <summary>
        /// The playfield of a shared mission has the size the server gave it, and the camera backs off until it fits
        /// the screen; otherwise the playfield is what the camera shows.
        /// </summary>
        private void FitPlayfield()
        {
            if (Playground == null)
            {
                return;
            }
            if (IsCoop)
            {
                Playground.Fix(coop.HalfSize);
            }
            else if (Playground.IsFixed)
            {
                Playground.Release();
            }
            else
            {
                return;
            }
            cameraRig?.Place();
        }


        /// <summary>Every boss of the campaign once, in the order of the missions: the same list on every client.</summary>
        private List<Boss> BossCatalog()
        {
            var bosses = new List<Boss>();
            for (int i = 0; i < LevelCount; i++)
            {
                AsteroidsLevel mission = MissionAt(i);
                if (mission == null)
                {
                    continue;
                }
                if (mission.BossPrefab != null && !bosses.Contains(mission.BossPrefab))
                {
                    bosses.Add(mission.BossPrefab);
                }
                foreach (Boss rotating in mission.bossRotation ?? new Boss[0])
                {
                    if (rotating != null && !bosses.Contains(rotating))
                    {
                        bosses.Add(rotating);
                    }
                }
            }
            return bosses;
        }

        #endregion


        #region The other pilots

        /// <summary>The stand-in of another pilot's ship joins the mission.</summary>
        internal void AddRemoteShip(RemoteShip remote)
        {
            if (remote == null || remoteShips.Contains(remote))
            {
                return;
            }
            remoteShips.Add(remote);
            RegisterPlayer(remote.Player);
            field?.AddShip(remote.Player);
            RefreshActivePlayers();
        }


        /// <summary>The pilot left: their stand-in goes.</summary>
        internal void RemoveRemoteShip(RemoteShip remote)
        {
            if (remote == null || !remoteShips.Remove(remote))
            {
                return;
            }
            field?.RemoveShip(remote.Player);
            PlayerList.Remove(remote.Player);
            Destroy(remote.gameObject);
            RefreshActivePlayers();
        }


        internal RemoteShip RemoteShipAt(int seat)
        {
            foreach (RemoteShip remote in remoteShips)
            {
                if (remote != null && remote.Seat == seat)
                {
                    return remote;
                }
            }
            return null;
        }


        /// <summary>The ship of the hangar the local pilot flies, which the others have to show.</summary>
        internal int LocalHullIndex => SelectedHullIndex;

        /// <summary>The ships of the hangar, for the stand-ins of the other pilots.</summary>
        internal PlayerSettings[] Hangar => hangar;

        /// <summary>
        /// The hull strength of the mission, which the stand-ins need to show how much of it is left: a strike ship's
        /// energy, otherwise the asteroid settings' hull.
        /// </summary>
        internal float MissionHullStrength => IsStrike ? StrikeRules.MaxEnergy : AsteroidSettings != null ? AsteroidSettings.HullStrength : 100f;


        internal void ShowPilots(List<PilotStatus> pilots)
        {
            ui?.ShowPilots(IsCoop ? pilots : null);
        }


        private void ClearRemoteShips()
        {
            for (int i = remoteShips.Count - 1; i >= 0; i--)
            {
                RemoveRemoteShip(remoteShips[i]);
            }
        }


        private void RefreshActivePlayers()
        {
            var active = new List<IPlayer> { ship };
            foreach (RemoteShip remote in remoteShips)
            {
                active.Add(remote.Player);
            }
            ActivePlayers = active;
        }

        #endregion


        #region Following the simulator

        /// <summary>How far the mission got, for the simulator to tell the others. False outside of a running mission.</summary>
        internal bool CoopReport(out int wave, out int wavesCleared, out string status, out float progress)
        {
            if (IsStrike)
            {
                // Strike: the row's wave counts the hostiles that came on screen, and nothing is ever cleared.
                wave = HostilesEntered;
                wavesCleared = 0;
                progress = ScrollProgress;
                status = StrikeStatus(progress);
                return IsCoop && coop.Simulates && IsMissionActive;
            }
            wave = director != null ? director.WaveNumber : 0;
            wavesCleared = objective != null ? objective.WavesCleared : 0;
            status = objective != null ? objective.Status(Mathf.Max(1, wave)) : string.Empty;
            progress = objective != null ? objective.Progress : 0f;
            return IsCoop && coop.Simulates && objective != null && IsMissionActive;
        }


        /// <summary>The simulator reported how far the mission got: the HUD follows, and a cleared wave pays its bonus here too.</summary>
        internal void CoopReported(int wave, int wavesCleared, string status, float progress)
        {
            if (!FollowsSimulator)
            {
                return;
            }
            coopStatus = status ?? string.Empty;
            coopProgress = progress;
            if (IsStrike)
            {
                // No waves in a strike mission: no bonus and no announcements, only the hostiles that came on screen.
                coopWave = Mathf.Max(coopWave, wave);
                return;
            }
            if (wavesCleared > coopWavesCleared && IsMissionActive)
            {
                int bonus = Mathf.RoundToInt(waveBonus * wavesCleared);
                score.Add(bonus);
                SyncScore();
                ui?.Announce("WAVE CLEARED", $"+{bonus}", new Color(0.5f, 1f, 0.65f));
                sounds?.WaveClear();
            }
            coopWavesCleared = Mathf.Max(coopWavesCleared, wavesCleared);
            if (wave > coopWave && wave > 1 && IsMissionActive)
            {
                ui?.Announce($"WAVE {wave}", string.Empty, Mission != null && Mission.Theme != null ? Mission.Theme.Accent : Color.cyan);
                sounds?.WaveStart();
            }
            coopWave = Mathf.Max(coopWave, wave);
        }


        /// <summary>What the simulator's strike mission says about itself on the others' HUD.</summary>
        private string StrikeStatus(float progress)
        {
            ScrollDirector run = scroll;
            if (run != null && (run.State == ScrollDirector.Stage.BossApproach || run.State == ScrollDirector.Stage.Boss))
            {
                return "Destroy the target";
            }
            if (run != null && run.State == ScrollDirector.Stage.Done)
            {
                return "Target destroyed";
            }
            return $"Target in {Mathf.RoundToInt((1f - Mathf.Clamp01(progress)) * 100f)}%";
        }


        /// <summary>
        /// The simulator reported its strike scroll (<see cref="IFieldLink.ScrollReported"/>, a few times a second): the
        /// ground of a guest follows it, from the first report on (until then it stands still).
        /// </summary>
        internal void CoopScrollReported(float distance, float speed)
        {
            if (!FollowsScroll || field == null)
            {
                return;
            }
            coopScrollDistance = distance;
            coopScrollSpeed = Mathf.Max(0f, speed);
            coopScrollAt = Time.time;
            if (!coopScrollKnown)
            {
                coopScrollKnown = true;
                coopScrollFollowed = distance;
            }
        }


        /// <summary>
        /// A frame of a guest's strike scroll, in every phase of the mission (the briefing too): the ground moves on at
        /// the reported speed and closes a small gap to where the simulator's is by now; it never runs backwards unless
        /// the gap is big. The distance is kept here and written to the field every frame (whatever else moved it in
        /// between), and <see cref="SpaceField.ScrollSpeed"/> is what the ground did, so the ground units that move with
        /// it stay on their ground. Runs once a frame, whoever calls it first (the online controller calls it every frame;
        /// the strike mission may call it before it ticks the field).
        /// </summary>
        internal void FollowCoopScroll(float deltaTime)
        {
            if (!FollowsScroll || field == null || !IsGameRunning || Time.frameCount == coopScrollFrame)
            {
                return;
            }
            coopScrollFrame = Time.frameCount;
            if (!coopScrollKnown || deltaTime <= 0f)
            {
                return;
            }
            float expected = coopScrollDistance + coopScrollSpeed * Mathf.Min(Time.time - coopScrollAt, CoopScrollLead);
            float error = expected - (coopScrollFollowed + coopScrollSpeed * deltaTime);
            float speed;
            if (Mathf.Abs(error) > CoopScrollSnap)
            {
                coopScrollFollowed = expected;
                speed = coopScrollSpeed;
            }
            else
            {
                float step = Mathf.Max(0f, coopScrollSpeed * deltaTime + error * Mathf.Min(1f, CoopScrollEase * deltaTime));
                coopScrollFollowed += step;
                speed = step / deltaTime;
            }
            field.ScrollDistance = coopScrollFollowed;
            field.ScrollSpeed = speed;
            StrikeTerrain terrain = field.Terrain;
            if (terrain != null && terrain.IsShown)
            {
                terrain.SetDistance(coopScrollFollowed);
            }
        }


        /// <summary>The boss the simulator sent arrived here as a puppet.</summary>
        internal void CoopBossArrived(Boss arrived)
        {
            if (FollowsSimulator && arrived != null)
            {
                OnBossArrived(arrived);
            }
        }


        /// <summary>A pilot on another device collected a pickup: their crystals count for the objective of the mission too.</summary>
        internal void CoopPickupTaken(Reward reward)
        {
            if (reward is PointReward && IsCoop && coop.Simulates)
            {
                objective?.CrystalCollected();
            }
        }


        /// <summary>The server says how the mission ended (or that it is won, a moment before the room finishes).</summary>
        internal void CoopOutcome(MissionOutcome outcome)
        {
            if (!IsCoop || !IsGameRunning || phase == MissionPhase.Victory || phase == MissionPhase.Defeat || phase == MissionPhase.Results)
            {
                return;
            }
            switch (outcome)
            {
                case MissionOutcome.Victory:
                    Win();
                    break;
                case MissionOutcome.Failed:
                    FailCoop("MISSION FAILED", "Every ship was lost");
                    break;
                case MissionOutcome.Abandoned:
                    FailCoop("MISSION ABORTED", "The host left the mission");
                    break;
            }
        }


        /// <summary>The places of the pilots changed while the results show.</summary>
        internal void CoopStandingsChanged()
        {
            if (IsCoop && phase == MissionPhase.Results && online != null)
            {
                if (IsStrike)
                {
                    ShowCoopStrikeStandings(online.DescribeStandings());
                    return;
                }
                ui?.ShowStandings(online.DescribeStandings());
            }
        }

        #endregion


        #region The end of a shared mission

        /// <summary>
        /// The mission is won. The simulator tells the server (after sending what changed, so the others see the boss go
        /// before the verdict); every pilot reports the score with the bonus for the ships left.
        /// </summary>
        private void CoopWon()
        {
            if (online == null)
            {
                return;
            }
            if (coop.Simulates)
            {
                online.MissionWon();
            }
            online.ReportScoreNow(score.Score);
        }


        /// <summary>Out of ships: the others fly on. The mission ends for everybody when the last pilot is out.</summary>
        private void WatchOthers()
        {
            phase = MissionPhase.Watching;
            phaseTime = 0f;
            ui?.Announce(IsStrike ? "SHIP DESTROYED" : "OUT OF SHIPS", "Watching the other pilots", new Color(1f, 0.35f, 0.3f));
            sounds?.GameOver();
            online?.PilotOut(score.Score);
        }


        /// <summary>
        /// The server ended the mission without a victory (every ship lost, or the host left): a strike pilot is not banked,
        /// and the loops of the fight (a boss's alarm, a beam) stop, as when a solo strike is lost.
        /// </summary>
        private void FailCoop(string title, string subtitle)
        {
            phase = MissionPhase.Defeat;
            phaseTime = 0f;
            director?.Stop();
            scroll?.Stop();
            spawner?.ClearPending();
            DiscardCoopPilot();
            if (IsStrike)
            {
                sounds?.SetBossAlarm(false);
                sounds?.SetBeam(false);
            }
            ui?.Announce(title, subtitle, new Color(1f, 0.35f, 0.3f));
            sounds?.GameOver();
        }


        /// <summary>The results of a shared mission: how it ended, this pilot's numbers and everybody's scores. No stars, no records.</summary>
        private void ShowCoopResults(bool victory)
        {
            if (IsStrike)
            {
                ShowCoopStrikeResults(victory);
                return;
            }
            phase = MissionPhase.Results;
            AsteroidsLevel mission = Mission;
            var result = new MissionResult
            {
                Title = mission != null ? mission.Title : "Mission",
                Victory = victory,
                Endless = false,
                Coop = true,
                Standings = online != null ? online.DescribeStandings() : string.Empty,
                Score = score.Score,
                LifeBonus = victory ? lives * lifeBonus : 0,
                Kills = score.Kills,
                Accuracy = score.Accuracy,
                MaxCombo = score.MaxCombo,
                Crystals = score.Crystals,
                Time = missionTime,
                Wave = director != null ? director.WaveNumber : 0
            };
            TransitionState(victory ? BaseGameState.Victory : BaseGameState.GameOver);
            ui?.ShowResults(result);
        }


        /// <summary>
        /// The results of a shared strike mission. A pilot whose ship landed after the victory banks the working copy with
        /// the money of the mission (the difficulty of the room does not become theirs); a pilot who was down, and any
        /// defeat, keep the saved pilot as it was. No stars or records, and no Supply Room from here.
        /// </summary>
        private void ShowCoopStrikeResults(bool victory)
        {
            phase = MissionPhase.Results;
            int earned = (int)Mathf.Clamp(score.Score, 0, int.MaxValue);
            bool landed = victory && ship != null && ship.IsAlive && working != null;
            if (landed)
            {
                BankCoopPilot(earned);
            }
            else
            {
                DiscardCoopPilot();
            }
            AsteroidsLevel mission = Mission;
            coopStrikeResult = new StrikeResult
            {
                Title = mission != null ? mission.Title : "Mission",
                Victory = victory,
                Stars = 0,
                Money = landed ? earned : 0,
                Wallet = Pilot.Money,
                HostilesEntered = FollowsSimulator ? Mathf.Max(HostilesEntered, coopWave) : HostilesEntered,
                HostilesDestroyed = HostilesDestroyed,
                DamageTaken = DamageTaken,
                Time = missionTime,
                HasNext = false,
                Coop = true,
                Standings = online != null ? online.DescribeStandings() : string.Empty
            };
            // The field stands still from here: no loop of the fight may play on over the results.
            sounds?.SetBossAlarm(false);
            sounds?.SetBeam(false);
            TransitionState(victory ? BaseGameState.Victory : BaseGameState.GameOver);
            ui?.Strike?.ShowResults(coopStrikeResult.Value);
        }


        /// <summary>The places changed while the strike results show: they show again with the new standings.</summary>
        private void ShowCoopStrikeStandings(string standings)
        {
            if (!coopStrikeResult.HasValue || coopStrikeResult.Value.Standings == standings)
            {
                return;
            }
            StrikeResult result = coopStrikeResult.Value;
            result.Standings = standings;
            coopStrikeResult = result;
            ui?.Strike?.ShowResults(result);
        }


        /// <summary>The saved pilot becomes the working copy of the won mission plus its money, keeping the pilot's own difficulty.</summary>
        private void BankCoopPilot(int earned)
        {
            if (working == null)
            {
                return;
            }
            StrikeLoadout saved = Pilot;
            StrikeDifficulty own = saved.Difficulty;
            working.Money = StrikeRules.AddToWallet(working.Money, earned);
            saved.CopyFrom(working);
            saved.Difficulty = own;
            progress?.SavePilot(saved);
            working = null;
        }


        /// <summary>The working copy of a shared strike mission is dropped: the saved pilot stays as it was.</summary>
        private void DiscardCoopPilot()
        {
            working = null;
        }

        #endregion
    }
}
