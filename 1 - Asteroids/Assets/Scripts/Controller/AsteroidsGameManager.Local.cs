using System;
using System.Collections.Generic;
using Gamebox;
using UnityEngine;
using static Portfolio.Asteroids.AsteroidsText;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The local co-op half of the manager: two to four pilots at this device fly the mission selected on the title
    /// together, each in a ship of their own on the one screen and with controls of their own (the shared local play setup,
    /// <see cref="AsteroidsLocalRules"/>). Seat 1 flies the scene's ship, the others fly wingmen made from the same prefab
    /// (<see cref="SpaceField.Wingmen"/>), which the world treats exactly like it. As in an online room every pilot has a
    /// score and ships of their own (<see cref="LocalSquad"/>), a pilot out of ships is out, the mission fails when every
    /// pilot is out and is won as a solo mission is; the HUD lists the pilots instead of one ship's status, and the results
    /// rank them. A local mission writes no stars, records or campaign progress and cannot be saved. In a planet strike
    /// every pilot flies a copy of the saved pilot's loadout at the difficulty of the setup; the kit is only lent: when the
    /// mission is won and a ship lands, the money the squad earned is paid once into the saved pilot's wallet, and nothing
    /// else of the saved pilot changes.
    /// </summary>
    public partial class AsteroidsGameManager
    {
        /// <summary>A pilot of a local co-op mission at this device: the seat, the record, the ship and its controls.</summary>
        internal sealed class LocalPilot
        {
            public LocalSeat Seat;
            public SquadPilot Record;
            public AsteroidsPlayer Ship;

            /// <summary>The ship was made for the match (a wingman); seat 1 flies the scene's ship.</summary>
            public bool Wingman;

            /// <summary>Strike: the loadout the pilot flies with (a copy of the saved pilot's).</summary>
            public StrikeLoadout Working;

            /// <summary>Seconds until the pilot's next ship warps in; below zero when none is on the way.</summary>
            public float RespawnIn = -1f;

            public int LastMultiplier = 1;
            public Vector2 FlyOffFrom;

            // What the manager listens to on the ship, to stop listening when the match ends.
            public Action<AsteroidsPlayer> Destroyed;
            public Action<DamageInfo> Damaged;
            public Action<PointReward> Crystal;
            public Action Life;
            public Action<int> Volley;
            public Action WeaponChanged;
            public StrikeGunnery Gunnery;
        }


        private readonly List<LocalPilot> localPilots = new List<LocalPilot>();
        private readonly List<PilotStatus> localStatus = new List<PilotStatus>();
        private AsteroidsLocalRules localRules;
        private LocalSquad squad;
        private int localLifeBonus;

        public override LocalPlayRules LocalPlay => localRules ??= new AsteroidsLocalRules { MissionProblem = LocalMissionProblem };

        /// <summary>A local co-op match is on: its missions are flown by the pilots at this device (or their results show).</summary>
        internal bool IsLocal => InLocalMatch && localPilots.Count > 0;

        /// <summary>The pilots of the local co-op mission and their scores and ships (tours and tests); null outside one.</summary>
        internal LocalSquad Squad => IsLocal ? squad : null;

        /// <summary>The pilots of the local co-op match at this device, by seat (tours and tests).</summary>
        internal IReadOnlyList<LocalPilot> LocalPilots => localPilots;


        #region Menu

        /// <summary>
        /// The Local Play button of the mission select: the shared setup for the mission selected on the title, which has to
        /// be open (as the launch button wants it). Not over the online lobby or inside a room.
        /// </summary>
        public override void OpenLocalPlay()
        {
            if (phase != MissionPhase.Menu || IsLobbyOpen || InSession)
            {
                return;
            }
            string problem = LocalMissionProblem();
            if (problem != null)
            {
                ui?.UpdateError(Mission != null && !IsUnlocked(LevelIndex) ? LockReason(LevelIndex) : T(problem));
                sounds?.Denied();
                return;
            }
            sounds?.Click();
            ((AsteroidsLocalRules)LocalPlay).ForMode(Mission.Mode);
            base.OpenLocalPlay();
        }


        /// <summary>Why the selected mission cannot be flown together now (in English), or null.</summary>
        private string LocalMissionProblem()
        {
            if (menuMode == MissionMode.Strike && !IsStrike || Mission == null || !IsUnlocked(LevelIndex))
            {
                return "Pick an open mission on the title first.";
            }
            return null;
        }

        #endregion


        #region The match

        /// <summary>The setup's Start: a ship for every pilot, then the mission selected on the title.</summary>
        protected override void OnLocalMatchBegun(LocalMatch match)
        {
            if (phase != MissionPhase.Menu || ship == null || field == null || LocalMissionProblem() != null)
            {
                EndLocalMatch();
                return;
            }
            CreateLocalPilots(match);
            PlayMission(LevelIndex);
        }


        /// <summary>The match is over (back to the mission select): the wingmen go and the scene's ship is the player's again.</summary>
        protected override void OnLocalMatchEnded(LocalMatch match)
        {
            foreach (LocalPilot pilot in localPilots)
            {
                Release(pilot);
            }
            localPilots.Clear();
            squad = null;
            if (ship != null)
            {
                ship.Assign(-1, PlayerControl.Local);
            }
            RefreshActivePlayers();
            ui?.ShowPilots(null);
            ui?.ShowLocalHud(false);
            StrikeScreens?.ShowLocalHud(false);
        }


        /// <summary>
        /// The pilots of <paramref name="match"/>: seat 1 takes the scene's ship, every other seat a wingman made from the
        /// ship prefab (the online stand-ins' prefab, else a copy of the scene's ship), each with the seat's controls, colour
        /// and name. The ships wait inactive until the mission puts them in place.
        /// </summary>
        private void CreateLocalPilots(LocalMatch match)
        {
            AsteroidsPlayer prefab = online != null && online.shipPrefab != null ? online.shipPrefab : ship;
            var active = new List<IPlayer>();
            foreach (LocalSeat seat in match.Players)
            {
                AsteroidsPlayer flown = ship;
                bool wingman = localPilots.Count > 0;
                if (wingman)
                {
                    flown = Instantiate(prefab, ship.transform.parent);
                    flown.name = $"Ship of {seat.Name}";
                    flown.IsWingman = true;
                    flown.Field = field;
                    flown.TouchControls = null;
                    // The autopilots fly only when a tour switches them on.
                    if (flown.TryGetComponent(out AsteroidsAutopilot autopilot))
                    {
                        autopilot.enabled = false;
                    }
                    StrikeAutopilot strikePilot = flown.GetComponentInChildren<StrikeAutopilot>(true);
                    if (strikePilot != null)
                    {
                        strikePilot.enabled = false;
                    }
                    RegisterPlayer(flown);
                    if (hangar != null && hangar.Length > 0 && hangar[SelectedHullIndex] != null)
                    {
                        flown.ApplyHull(hangar[SelectedHullIndex]);
                    }
                    flown.gameObject.SetActive(false);
                    field.AddWingman(flown);
                }
                flown.Assign(seat.Index, PlayerControl.Local, seat.Name);
                flown.OwnControls = new LocalShipInput(seat.Controls);
                PilotTag.Show(flown.gameObject, seat.Name, seat.Color);
                var pilot = new LocalPilot { Seat = seat, Ship = flown, Wingman = wingman };
                Listen(pilot);
                localPilots.Add(pilot);
                active.Add(flown);
            }
            ActivePlayers = active;
        }


        /// <summary>The manager hears what happens to the pilot's ship (the scene ship's own handlers stand aside meanwhile).</summary>
        private void Listen(LocalPilot pilot)
        {
            AsteroidsPlayer flown = pilot.Ship;
            pilot.Destroyed = lost => LocalShipDestroyed(pilot);
            pilot.Damaged = hit => LocalShipDamaged(pilot, hit);
            pilot.Crystal = crystal => LocalCrystal(pilot, crystal);
            pilot.Life = () => pilot.Record?.AddLife(AsteroidSettings.LivesLimit);
            pilot.Volley = count => pilot.Record?.Score.ShotFired(count);
            pilot.WeaponChanged = () => LocalWeaponChanged(pilot);
            flown.Destroyed += pilot.Destroyed;
            flown.Damaged += pilot.Damaged;
            flown.CrystalCollected += pilot.Crystal;
            flown.LifeAwarded += pilot.Life;
            flown.VolleyFired += pilot.Volley;
            flown.Weapons.Changed += pilot.WeaponChanged;
            if (pilot.Wingman)
            {
                flown.PowerUpChanged += OnPowerUpChanged;
            }
        }


        /// <summary>The pilot's ship is let go: a wingman is destroyed, the scene's ship gets its own look and controls back.</summary>
        private void Release(LocalPilot pilot)
        {
            AsteroidsPlayer flown = pilot.Ship;
            if (flown == null)
            {
                return;
            }
            flown.Destroyed -= pilot.Destroyed;
            flown.Damaged -= pilot.Damaged;
            flown.CrystalCollected -= pilot.Crystal;
            flown.LifeAwarded -= pilot.Life;
            flown.VolleyFired -= pilot.Volley;
            flown.Weapons.Changed -= pilot.WeaponChanged;
            ForgetGunnery(pilot);
            if (!pilot.Wingman)
            {
                flown.OwnControls = null;
                PilotTag.Remove(flown.gameObject);
                return;
            }
            flown.PowerUpChanged -= OnPowerUpChanged;
            field?.RemoveWingman(flown);
            PlayerList.Remove(flown);
            Destroy(flown.gameObject);
        }


        private void ForgetGunnery(LocalPilot pilot)
        {
            if (pilot.Gunnery != null)
            {
                pilot.Gunnery.WeaponLost -= OnWeaponLost;
                pilot.Gunnery = null;
            }
        }

        #endregion


        #region The mission

        /// <summary>
        /// The local part of preparing a mission (after the scene's ship was reset for it): a fresh squad, every wingman
        /// reset the same way, every ship at its start (on a circle in the asteroid field, in a row at the bottom of a strike),
        /// the HUD of the pilots instead of one ship's.
        /// </summary>
        private void PrepareLocalMission(bool strike)
        {
            var seats = new List<LocalSeat>();
            foreach (LocalPilot pilot in localPilots)
            {
                seats.Add(pilot.Seat);
            }
            squad = new LocalSquad(seats, strike ? 1 : LocalCoopRules.Lives(LocalMatch));
            lives = squad.Pilots[0].Lives;
            localLifeBonus = 0;
            int count = localPilots.Count;
            Vector2 halfSize = Playground != null ? Playground.HalfSize : StrikeRules.HalfSize;
            AsteroidSettings settings = AsteroidSettings;
            for (int i = 0; i < count; i++)
            {
                LocalPilot pilot = localPilots[i];
                pilot.Record = squad.Pilots[i];
                pilot.RespawnIn = -1f;
                pilot.LastMultiplier = 1;
                ForgetGunnery(pilot);
                AsteroidsPlayer flown = pilot.Ship;
                if (strike)
                {
                    if (pilot.Wingman)
                    {
                        pilot.Working = Pilot.Clone();
                        pilot.Working.Difficulty = MissionDifficulty;
                        pilot.Working.EnsureLaunchEnergy();
                        flown.ResetForStrike(pilot.Working);
                    }
                    else
                    {
                        pilot.Working = working;
                    }
                    flown.Position = FieldMath.RowPoint(i, count, halfSize);
                    // The scene ship's gunnery is followed by PrepareStrike.
                    if (pilot.Wingman && flown.Strike != null)
                    {
                        pilot.Gunnery = flown.Strike;
                        pilot.Gunnery.WeaponLost += OnWeaponLost;
                    }
                }
                else
                {
                    pilot.Working = null;
                    if (pilot.Wingman)
                    {
                        flown.ResetForMission(settings != null ? settings.HullStrength : 100f);
                    }
                    flown.Position = FieldMath.StartPoint(i, count, CoopRules.StartRadius);
                }
                flown.Points = 0;
            }
            ui?.ShowLocalHud(true);
            StrikeScreens?.ShowLocalHud(true);
        }


        /// <summary>One frame of every ship flown at this device: the scene's ship and the wingmen.</summary>
        private void SimulateShips(float deltaTime, bool controls = true)
        {
            ship.Simulate(deltaTime, controls);
            if (!IsLocal)
            {
                return;
            }
            foreach (LocalPilot pilot in localPilots)
            {
                if (pilot.Wingman)
                {
                    pilot.Ship.Simulate(deltaTime, controls);
                }
            }
        }


        /// <summary>Whether a ship flown at this device has the chrono field on (it slows the world for everybody).</summary>
        private bool ChronoActive
        {
            get
            {
                if (ship.IsPowerUpActive(PowerUpType.Chrono))
                {
                    return true;
                }
                foreach (LocalPilot pilot in localPilots)
                {
                    if (pilot.Wingman && pilot.Ship.IsAlive && pilot.Ship.IsPowerUpActive(PowerUpType.Chrono))
                    {
                        return true;
                    }
                }
                return false;
            }
        }


        /// <summary>The combo of every pilot runs down; a pilot whose ship was lost gets the next one once its wait is over.</summary>
        private void UpdateLocalPilots(float deltaTime)
        {
            foreach (LocalPilot pilot in localPilots)
            {
                ScoreKeeper keeper = pilot.Record.Score;
                keeper.Tick(deltaTime);
                if (keeper.Multiplier < pilot.LastMultiplier)
                {
                    pilot.LastMultiplier = keeper.Multiplier;
                }
                if (pilot.RespawnIn < 0f || pilot.Record.Out)
                {
                    continue;
                }
                pilot.RespawnIn -= deltaTime;
                if (pilot.RespawnIn <= 0f && phase == MissionPhase.Playing)
                {
                    pilot.RespawnIn = -1f;
                    Vector2 position = SafeSpawnPoint();
                    pilot.Ship.Respawn(position);
                    field.MakeRoom(position, pilot.Ship.HitSeat);
                }
            }
        }


        /// <summary>The pilot whose ship is <paramref name="flown"/>; null for any other ship.</summary>
        private LocalPilot LocalPilotOf(AsteroidsPlayer flown)
        {
            foreach (LocalPilot pilot in localPilots)
            {
                if (pilot.Ship == flown && flown != null)
                {
                    return pilot;
                }
            }
            return null;
        }


        /// <summary>The pilot a hit counts for: the wingman of its seat, else the pilot of the scene's ship.</summary>
        private LocalPilot LocalPilotOf(DamageInfo hit)
        {
            if (hit.Seat.HasValue)
            {
                foreach (LocalPilot pilot in localPilots)
                {
                    if (pilot.Wingman && pilot.Seat.Index == hit.Seat.Value)
                    {
                        return pilot;
                    }
                }
            }
            return localPilots.Count > 0 ? localPilots[0] : null;
        }


        /// <summary>Whether a ship flown at this device is still in one piece (a strike mission lands if one is).</summary>
        private bool AnyLocalShipAlive
        {
            get
            {
                foreach (LocalPilot pilot in localPilots)
                {
                    if (pilot.Ship != null && pilot.Ship.IsAlive)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        #endregion


        #region What happens to the pilots

        private void LocalShipDamaged(LocalPilot pilot, DamageInfo hit)
        {
            if (IsStrike)
            {
                CountStrikeDamage(hit);
            }
            pilot.Record?.Score.BreakCombo();
            pilot.LastMultiplier = 1;
            cameraRig?.Hurt(0.5f);
            cameraRig?.Shake(0.3f);
        }


        /// <summary>A ship of a pilot was destroyed: the next one warps in after a while, or the pilot is out of the mission.</summary>
        private void LocalShipDestroyed(LocalPilot pilot)
        {
            if (!IsMissionActive || pilot.Record == null)
            {
                return;
            }
            livesLost++;
            bool back = pilot.Record.LoseShip(IsStrike);
            if (squad.AllOut)
            {
                Lose();
                return;
            }
            Color color = pilot.Seat.Color;
            if (back)
            {
                pilot.RespawnIn = respawnDelay;
                ui?.Toast(F("{0}: SHIP LOST - {1} LEFT", pilot.Seat.Name, pilot.Record.Lives), color);
            }
            else
            {
                ui?.Toast(F("{0} IS OUT", pilot.Seat.Name), color);
                sounds?.Denied();
            }
        }


        private void LocalCrystal(LocalPilot pilot, PointReward crystal)
        {
            if (pilot.Record == null)
            {
                return;
            }
            pilot.Record.Score.AddCrystal();
            pilot.Record.Score.Add(crystal.PointsAward);
            SyncScore();
            objective?.CrystalCollected();
            effects?.Popup(crystal.Position, $"+{crystal.PointsAward}", pilot.Seat.Color, 0.75f);
        }


        private void LocalWeaponChanged(LocalPilot pilot)
        {
            if (!IsMissionActive || pilot.Ship == null)
            {
                return;
            }
            ShipWeapons weapons = pilot.Ship.Weapons;
            ui?.Toast(F("{0}: {1} LV {2}", pilot.Seat.Name, T(WeaponRules.ShortTitle(weapons.Type)), weapons.Level), WeaponRules.Tint(weapons.Type));
        }


        /// <summary>A kill of the asteroid field scores for the pilot it counts for, with that pilot's own combo.</summary>
        private void ScoreLocalKill(Shootable target, LocalPilot pilot)
        {
            if (pilot == null || pilot.Record == null)
            {
                return;
            }
            ScoreKeeper keeper = pilot.Record.Score;
            int points = keeper.AddKill(target.Score);
            SyncScore();
            Color color = pilot.Seat.Color;
            effects?.Popup(target.Position, points.ToString(), color, target is Boss ? 2.2f : target.Radius > 1.2f ? 1.1f : 0.9f);
            if (keeper.Multiplier > pilot.LastMultiplier)
            {
                pilot.LastMultiplier = keeper.Multiplier;
                ui?.Toast(F("{0}: COMBO x{1}", pilot.Seat.Name, keeper.Multiplier), color);
                sounds?.Combo(keeper.Multiplier);
            }
        }


        /// <summary>The ScoreKeeper money and points of <paramref name="earner"/>'s pilot go to (the scene ship's pilot for anybody else).</summary>
        private ScoreKeeper LocalScoreOf(AsteroidsPlayer earner)
        {
            LocalPilot pilot = LocalPilotOf(earner) ?? (localPilots.Count > 0 ? localPilots[0] : null);
            return pilot != null && pilot.Record != null ? pilot.Record.Score : score;
        }

        #endregion


        #region The HUD and the results

        /// <summary>The pilots list of the HUD: every pilot's name, score, ships left (asteroid field) and hull or energy.</summary>
        private void ShowLocalPilots()
        {
            localStatus.Clear();
            bool strike = IsStrike;
            foreach (LocalPilot pilot in localPilots)
            {
                AsteroidsPlayer flown = pilot.Ship;
                float gauge = 0f;
                if (flown != null && flown.IsAlive)
                {
                    gauge = strike && pilot.Working != null ? pilot.Working.Energy / StrikeRules.MaxEnergy
                        : flown.MaxHealth > 0f ? flown.Health / flown.MaxHealth : 0f;
                }
                localStatus.Add(new PilotStatus
                {
                    Seat = pilot.Seat.Index,
                    Name = pilot.Seat.Name,
                    Score = pilot.Record.Score.Score,
                    Flying = !pilot.Record.Out,
                    Local = false,
                    Lives = strike ? (int?)null : pilot.Record.Lives,
                    Gauge = Mathf.Clamp01(gauge)
                });
            }
            ui?.ShowPilots(localStatus);
        }


        /// <summary>
        /// The results of a local asteroid field mission: how it ended, the squad's numbers and every pilot by place. No
        /// stars, records or progress; Retry flies it again with the same pilots.
        /// </summary>
        private void ShowLocalResults(bool victory)
        {
            if (IsStrike)
            {
                ShowLocalStrikeResults(victory);
                return;
            }
            phase = MissionPhase.Results;
            AsteroidsLevel mission = Mission;
            var result = new MissionResult
            {
                Title = mission != null ? mission.Title : "Mission",
                Victory = victory,
                Endless = mission != null && mission.IsEndless,
                Local = true,
                Standings = squad.Describe(pilot => pilot.Score.Score.ToString("N0"), pilot => F("{0} destroyed", pilot.Score.Kills)),
                Score = squad.TeamScore,
                LifeBonus = victory ? localLifeBonus : 0,
                Kills = squad.Kills,
                Accuracy = squad.Accuracy,
                MaxCombo = squad.MaxCombo,
                Crystals = squad.Crystals,
                Time = missionTime,
                Wave = director != null ? director.WaveNumber : 0
            };
            TransitionState(victory ? BaseGameState.Victory : BaseGameState.GameOver);
            ui?.ShowResults(result);
        }


        /// <summary>
        /// The results of a local planet strike: when a ship landed after the win, the squad's money was paid into the saved
        /// pilot's wallet (<see cref="Land"/>); the standings rank the pilots by the money each earned.
        /// </summary>
        private void ShowLocalStrikeResults(bool victory)
        {
            phase = MissionPhase.Results;
            if (victory)
            {
                // A win whose fly-off was cut short still lands the ships.
                Land();
            }
            bool won = victory && landed;
            var result = new StrikeResult
            {
                Title = Mission != null ? Mission.Title : "Mission",
                Victory = won,
                Money = squad.TeamScore,
                Wallet = Pilot.Money,
                HostilesEntered = hostilesEntered,
                HostilesDestroyed = hostilesDestroyed,
                DamageTaken = damageTaken,
                Time = missionTime,
                Local = true,
                Standings = squad.Describe(pilot => $"${pilot.Score.Score:N0}")
            };
            ReleaseScroll();
            DiscardStrikeWorking();
            sounds?.SetBossAlarm(false);
            sounds?.SetBeam(false);
            TransitionState(won ? BaseGameState.Victory : BaseGameState.GameOver);
            StrikeScreens?.ShowResults(result);
        }

        #endregion
    }
}
