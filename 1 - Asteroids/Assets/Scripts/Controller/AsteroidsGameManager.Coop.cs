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
    /// pilot without ships watches the others, nothing pauses and nothing is saved, and the campaign progress stays
    /// as it is. Keeping the copies of the world in step is not done here but in <see cref="FieldReplication"/>.
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

        /// <summary>The mission is flown with other pilots.</summary>
        internal bool IsCoop => coop != null && InSession;

        /// <summary>This client runs the waves and judges the objective: always, except as a guest of a shared mission.</summary>
        internal bool Simulates => !IsCoop || coop.Simulates;

        /// <summary>The HUD shows the objective as the simulator reports it.</summary>
        private bool FollowsSimulator => IsCoop && !coop.Simulates;

        /// <summary>The lobby lies over the mission select, which must not react to keys meant for it.</summary>
        internal bool IsLobbyOpen => online != null && online.LobbyUI != null && online.LobbyUI.IsOpen;

        internal IReadOnlyList<RemoteShip> RemoteShips => remoteShips;


        #region Menu

        /// <summary>The multiplayer button of the mission select: opens the lobby of the game's server.</summary>
        public void OpenOnline()
        {
            if (phase != MissionPhase.Menu)
            {
                return;
            }
            sounds?.Click();
            if (online == null)
            {
                UI?.UpdateError("Online play is not set up in this scene.");
                return;
            }
            online.OpenLobby();
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
            FitPlayfield();
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

        /// <summary>The hull strength of the mission, which the stand-ins need to show how much of it is left.</summary>
        internal float MissionHullStrength => AsteroidSettings != null ? AsteroidSettings.HullStrength : 100f;


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
                ui?.ShowStandings(online.DescribeStandings());
            }
        }

        #endregion


        #region The end of a shared mission

        /// <summary>The mission is won. The simulator tells the server; every pilot reports the score with the bonus for the ships left.</summary>
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
            ui?.Announce("OUT OF SHIPS", "Watching the other pilots", new Color(1f, 0.35f, 0.3f));
            sounds?.GameOver();
            online?.PilotOut(score.Score);
        }


        private void FailCoop(string title, string subtitle)
        {
            phase = MissionPhase.Defeat;
            phaseTime = 0f;
            director?.Stop();
            spawner?.ClearPending();
            ui?.Announce(title, subtitle, new Color(1f, 0.35f, 0.3f));
            sounds?.GameOver();
        }


        /// <summary>The results of a shared mission: how it ended, this pilot's numbers and everybody's scores. No stars, no records.</summary>
        private void ShowCoopResults(bool victory)
        {
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

        #endregion
    }
}
