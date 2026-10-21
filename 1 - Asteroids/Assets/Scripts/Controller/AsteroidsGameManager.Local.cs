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
    ///
    /// The same squad flies a party of an online room (<see cref="IsParty"/>): when players at this device hold seats of
    /// their own in the room (the companions of the base server), the device's player flies the scene's ship and every
    /// companion a wingman, each pilot with the seat of the room, its colour (the one the other devices show), the controls
    /// the lobby gave them (<see cref="Gamebox.Online.OnlineGameController.ControlsOf"/>), a score and ships of their own.
    /// The shared mission goes on as AsteroidsGameManager.Coop.cs says: a pilot out of ships is done in the room, and this
    /// device watches once every pilot at it is out. One pilot alone at the device flies as in any shared mission.
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

            /// <summary>Where the pilot starts among every pilot of the mission (a party's place among the pilots of the room).</summary>
            public int Slot;

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

        /// <summary>
        /// Several pilots of an online room fly at this device: the device's player and the companions who play through its
        /// connection, each in a ship of their own (<see cref="CoopSetup.Party"/>).
        /// </summary>
        internal bool IsParty => IsCoop && localPilots.Count > 0;

        /// <summary>Several pilots fly at this device, each with a ship, a score and ships of their own: local co-op, or a party of a room.</summary>
        internal bool FliesSquad => IsLocal || IsParty;

        /// <summary>The pilots at this device and their scores and ships (tours and tests); null when one pilot flies alone.</summary>
        internal LocalSquad Squad => FliesSquad ? squad : null;

        /// <summary>The pilots of the local co-op match (or of the party of a room) at this device, by seat (tours and tests).</summary>
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
            ReleaseLocalPilots();
        }


        /// <summary>
        /// The pilots of <paramref name="match"/>: seat 1 takes the scene's ship, every other seat a wingman made from the
        /// ship prefab (the online stand-ins' prefab, else a copy of the scene's ship), each with the seat's controls, colour
        /// and name. The ships wait inactive until the mission puts them in place.
        /// </summary>
        private void CreateLocalPilots(LocalMatch match)
        {
            int slot = 0;
            foreach (LocalSeat seat in match.Players)
            {
                AddLocalPilot(seat, seat.Controls, slot++);
            }
            RefreshActivePlayers();
        }


        /// <summary>
        /// The pilots of a party of an online room (<paramref name="party"/>, the device's player first): the device's player
        /// takes the scene's ship and every companion a wingman, each by the seat of the room, in that seat's colour (as the
        /// other devices show the ship), with the controls the lobby gave the seat. On a touch screen the device's player
        /// keeps the touch controls.
        /// </summary>
        private void CreatePartyPilots(IReadOnlyList<CoopPilot> party)
        {
            foreach (CoopPilot member in party)
            {
                var seat = new LocalSeat(member.Seat) { Name = member.Name, Color = CoopRules.SeatColor(member.Seat) };
                AddLocalPilot(seat, member.Controls, member.Slot);
            }
            if (MobilePlatform.UsesTouch && localPilots.Count > 0 && !SceneShipParked)
            {
                localPilots[0].Ship.OwnControls = null;
            }
            RefreshActivePlayers();
        }


        /// <summary>
        /// A pilot for <paramref name="seat"/>, flown with <paramref name="controls"/>: the first takes the scene's ship, the
        /// others a wingman made from the ship prefab (the online stand-ins' prefab, else a copy of the scene's ship) that
        /// waits inactive until the mission puts it in place.
        /// </summary>
        private void AddLocalPilot(LocalSeat seat, PlayerControls controls, int slot)
        {
            AsteroidsPlayer prefab = online != null && online.shipPrefab != null ? online.shipPrefab : ship;
            AsteroidsPlayer flown = ship;
            // The scene's ship is the device's own seat; when nobody flies that here (the shared screen), every pilot is a wingman.
            bool wingman = localPilots.Count > 0 || SceneShipParked;
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
            flown.OwnControls = new LocalShipInput(controls);
            PilotTag.Show(flown.gameObject, seat.Name, seat.Color);
            var pilot = new LocalPilot { Seat = seat, Ship = flown, Wingman = wingman, Slot = slot };
            Listen(pilot);
            localPilots.Add(pilot);
        }


        /// <summary>Every pilot at this device is let go (the match or the session is over): the wingmen go, the scene's ship is the player's again.</summary>
        private void ReleaseLocalPilots()
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
        /// reset the same way, every ship at its start (on a circle in the asteroid field, in a row at the bottom of a strike;
        /// a party's ships at their places among the pilots of the room), the HUD of the pilots instead of one ship's. The
        /// device's own pilot of a party keeps the mission's score keeper, so what the shared mission says about this device's
        /// player stays as it is alone.
        /// </summary>
        private void PrepareLocalMission(bool strike)
        {
            var seats = new List<LocalSeat>();
            foreach (LocalPilot pilot in localPilots)
            {
                seats.Add(pilot.Seat);
            }
            bool party = IsParty;
            int ships = strike ? 1 : party ? Mathf.Clamp(coop.Lives, 1, AsteroidSettings.LivesLimit) : LocalCoopRules.Lives(LocalMatch);
            // On the shared screen no pilot is the device's own: every pilot keeps a score of their own.
            squad = new LocalSquad(seats, ships, party && !SceneShipParked ? score : null);
            if (squad.Count > 0)
            {
                lives = squad.Pilots[0].Lives;
            }
            localLifeBonus = 0;
            int count = localPilots.Count;
            int places = party ? Mathf.Max(count, coop.Pilots) : count;
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
                    flown.Position = FieldMath.RowPoint(pilot.Slot, places, halfSize);
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
                    flown.Position = FieldMath.StartPoint(pilot.Slot, places, CoopRules.StartRadius);
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
            if (!FliesSquad)
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
                // The mission's own keeper (a party's device pilot) runs down with the mission already.
                if (!ReferenceEquals(keeper, score))
                {
                    keeper.Tick(deltaTime);
                }
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


        /// <summary>
        /// The pilot a hit counts for: the wingman of its seat, else the pilot of the scene's ship. In a party a seat that is
        /// no wingman's belongs to a pilot of another device: the hit counts for nobody here.
        /// </summary>
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
                if (IsParty)
                {
                    return null;
                }
            }
            return localPilots.Count > 0 ? localPilots[0] : null;
        }


        /// <summary>The pilot at this device who flies room seat <paramref name="seat"/> in a party; null for any other seat.</summary>
        private LocalPilot PartyPilotAt(int seat)
        {
            if (!IsParty)
            {
                return null;
            }
            foreach (LocalPilot pilot in localPilots)
            {
                if (pilot.Seat.Index == seat)
                {
                    return pilot;
                }
            }
            return null;
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


        /// <summary>
        /// A ship of a pilot was destroyed: the next one warps in after a while, or the pilot is out of the mission. In a
        /// party a pilot who is out is done in the room (with their score), and the others fly on.
        /// </summary>
        private void LocalShipDestroyed(LocalPilot pilot)
        {
            if (!IsMissionActive || pilot.Record == null)
            {
                return;
            }
            livesLost++;
            bool back = pilot.Record.LoseShip(IsStrike);
            if (!back && IsParty)
            {
                online?.PilotOut((byte)pilot.Seat.Index, pilot.Record.Score.Score);
            }
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
                localStatus.Add(new PilotStatus
                {
                    Seat = pilot.Seat.Index,
                    Name = pilot.Seat.Name,
                    Score = pilot.Record.Score.Score,
                    Flying = !pilot.Record.Out,
                    Local = false,
                    Lives = strike ? (int?)null : pilot.Record.Lives,
                    Gauge = GaugeOf(pilot, strike)
                });
            }
            ui?.ShowPilots(localStatus);
        }


        /// <summary>
        /// What the pilots list of a shared mission shows for room seat <paramref name="seat"/> when a pilot of this device's
        /// party flies it: the score, whether the pilot is still in, ships left (asteroid field) and hull or energy. False,
        /// and <paramref name="status"/> as it was, for any other seat.
        /// </summary>
        internal bool PartyPilotStatus(int seat, ref PilotStatus status)
        {
            LocalPilot pilot = PartyPilotAt(seat);
            if (pilot == null || pilot.Record == null)
            {
                return false;
            }
            bool strike = IsStrike;
            status.Score = pilot.Record.Score.Score;
            status.Flying = !pilot.Record.Out;
            status.Lives = strike ? (int?)null : pilot.Record.Lives;
            status.Gauge = GaugeOf(pilot, strike);
            return true;
        }


        /// <summary>The hull of the pilot's ship (a strike ship's energy), 0 to 1; 0 while the ship is down.</summary>
        private static float GaugeOf(LocalPilot pilot, bool strike)
        {
            AsteroidsPlayer flown = pilot.Ship;
            float gauge = 0f;
            if (flown != null && flown.IsAlive)
            {
                gauge = strike && pilot.Working != null ? pilot.Working.Energy / StrikeRules.MaxEnergy
                    : flown.MaxHealth > 0f ? flown.Health / flown.MaxHealth : 0f;
            }
            return Mathf.Clamp01(gauge);
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
