using System.Collections.Generic;
using Gamebox;
using Gamebox.Online;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The shared screen and the remote controls, in the manager (the online controller drives it from
    /// AsteroidsOnlineController.Remotes.cs). The scene's ship is the device's own seat only while that seat is flown
    /// here: on the shared screen, which holds no seat, and on a device whose player plays through a remote control, the
    /// ship is parked (<see cref="SceneShipParked"/>: inactive, never simulated, never sent). Every other seat flown here
    /// is a wingman of the party machinery (AsteroidsGameManager.Local.cs): the companions of a device as before, and on
    /// the screen the seats of the remotes, each flown by <see cref="LocalShipInput"/> over the controls that arrive from
    /// the remote. The seats flown here change in the middle of a mission when a remote switches to playing on their own
    /// device or back: the screen lets a wingman go (<see cref="ReleaseDrivenPilot"/>) or makes one from the seat's last
    /// pose (<see cref="AddDrivenPilot"/>), and the device takes its own seat over (<see cref="TakeOwnSeat"/>) or hands it
    /// to the screen (<see cref="GiveUpOwnSeat"/>). A seed (<see cref="SeatSeed"/>) is the seat's last pose, which
    /// carries the ships left, the weapon and the bombs (<see cref="ShipPose"/>), the score the server has for the seat,
    /// and in a strike mission the loadout the other side wrote down (<see cref="StrikeLoadoutText"/>).
    /// </summary>
    public partial class AsteroidsGameManager
    {
        /// <summary>What a seat is seeded with when this device starts flying it in the middle of a mission.</summary>
        internal struct SeatSeed
        {
            /// <summary>A pose of the seat is known (else the ship starts fresh at its place).</summary>
            public bool HasPose;
            public Vector2 Position;
            public Vector2 Velocity;
            public float Heading;
            public ShipPose Pose;
            /// <summary>The score the server has for the seat (the money earned in a strike mission).</summary>
            public long Score;
            /// <summary>Strike: the loadout the seat flies with, as the other side wrote it down; null for none known.</summary>
            public StrikeLoadout Loadout;

            /// <summary>A seed from the last pose of the seat (<see cref="ServerClient.Poses"/>), or none.</summary>
            public static SeatSeed From(RoomPoseInfo? pose, long score, StrikeLoadout loadout = null)
            {
                var seed = new SeatSeed { Score = score, Loadout = loadout };
                if (pose.HasValue)
                {
                    seed.HasPose = true;
                    seed.Position = pose.Value.Position;
                    seed.Velocity = pose.Value.Velocity;
                    seed.Heading = pose.Value.Heading;
                    seed.Pose = ShipPose.Unpack(pose.Value.State, pose.Value.Value);
                }
                return seed;
            }
        }


        /// <summary>
        /// The scene's ship is not flown here: this device holds no seat in the shared mission (the shared screen) or its
        /// player plays through a remote control, so the ship waits inactive while the screen flies the seat. Known from
        /// the co-op setup on (<see cref="PrepareCoop"/> makes the party before the session takes charge).
        /// </summary>
        internal bool SceneShipParked => coop != null && !coop.DrivesOwnSeat;

        /// <summary>This device is the shared screen of the room: it holds no seat and flies the seats of the remotes.</summary>
        internal bool IsScreenDevice => IsCoop && coop.LocalSeat == DeviceSeats.NoSeat;


        /// <summary>The scene's ship waits inactive (its seat is flown elsewhere, or nobody holds it here).</summary>
        private void ParkSceneShip()
        {
            if (ship == null)
            {
                return;
            }
            ship.Simulation?.Stop();
            ship.gameObject.SetActive(false);
        }


        /// <summary>
        /// The device's player takes their seat over from the shared screen in the middle of the mission: the scene's ship
        /// flies again from the seat's last pose, with the ships, weapon, bombs and score the seat had there (a strike
        /// mission takes the loadout the screen wrote down into the working copy).
        /// </summary>
        internal void TakeOwnSeat(SeatSeed seed)
        {
            if (!IsCoop || ship == null || coop.DrivesOwnSeat)
            {
                return;
            }
            coop.DrivesOwnSeat = true;
            ship.gameObject.SetActive(true);
            bool strike = IsStrike;
            if (strike && seed.Loadout != null && working != null)
            {
                StrikeDifficulty difficulty = working.Difficulty;
                working.CopyFrom(seed.Loadout);
                working.Difficulty = difficulty;
            }
            ApplySeed(ship, seed, strike && seed.Loadout == null ? working : null, out bool alive, out int shipsLeft);
            lives = strike ? 1 : seed.HasPose ? shipsLeft : Mathf.Clamp(coop.Lives, 1, AsteroidSettings.LivesLimit);
            score.Reset((int)Mathf.Clamp(seed.Score, 0, int.MaxValue));
            SyncScore();
            ui?.ShowLocalHud(false);
            StrikeScreens?.ShowLocalHud(false);
            if (!IsMissionActive)
            {
                return;
            }
            if (alive)
            {
                if (phase != MissionPhase.Playing)
                {
                    phase = MissionPhase.Playing;
                    phaseTime = 0f;
                }
            }
            else if (!strike && lives > 0)
            {
                // The ship was lost a moment ago: the next one warps in here, as it would have on the screen.
                phase = MissionPhase.Respawning;
                phaseTime = 0f;
            }
            else
            {
                phase = MissionPhase.Watching;
                phaseTime = 0f;
            }
        }


        /// <summary>
        /// The device's player hands their seat to the shared screen in the middle of the mission: the scene's ship parks,
        /// and the screen flies the seat on from the last pose this device sent.
        /// </summary>
        internal void GiveUpOwnSeat()
        {
            if (!IsCoop || !coop.DrivesOwnSeat)
            {
                return;
            }
            coop.DrivesOwnSeat = false;
            ParkSceneShip();
            ui?.ShowLocalHud(true);
            StrikeScreens?.ShowLocalHud(true);
            if (phase == MissionPhase.Respawning)
            {
                // The screen brings the next ship in.
                phase = MissionPhase.Playing;
                phaseTime = 0f;
            }
        }


        /// <summary>
        /// The shared screen starts flying room seat <paramref name="seat"/> in the middle of the mission (its remote came
        /// back to the remote control): a wingman for <paramref name="name"/> at place <paramref name="slot"/> among the
        /// pilots, flown with <paramref name="controls"/> and seeded from <paramref name="seed"/>. False when the seat is
        /// flown here already or nothing can be made.
        /// </summary>
        internal bool AddDrivenPilot(int seat, string name, int slot, PlayerControls controls, SeatSeed seed)
        {
            if (!IsCoop || ship == null || field == null || PartyPilotAt(seat) != null)
            {
                return false;
            }
            bool strike = IsStrike;
            var local = new LocalSeat(seat) { Name = name, Color = CoopRules.SeatColor(seat) };
            AddLocalPilot(local, controls, slot);
            LocalPilot pilot = localPilots[localPilots.Count - 1];
            squad ??= new LocalSquad(new List<LocalSeat>(), 1);
            AsteroidsPlayer flown = pilot.Ship;
            pilot.RespawnIn = -1f;
            pilot.LastMultiplier = 1;
            StrikeLoadout fromPose = null;
            if (strike)
            {
                pilot.Working = seed.Loadout ?? Pilot.Clone();
                pilot.Working.Difficulty = MissionDifficulty;
                if (seed.Loadout == null)
                {
                    pilot.Working.EnsureLaunchEnergy();
                    fromPose = pilot.Working;
                }
                flown.ResetForStrike(pilot.Working);
                pilot.Gunnery = flown.Strike;
                if (pilot.Gunnery != null)
                {
                    pilot.Gunnery.WeaponLost += OnWeaponLost;
                }
            }
            else
            {
                AsteroidSettings settings = AsteroidSettings;
                flown.ResetForMission(settings != null ? settings.HullStrength : 100f);
            }
            flown.Points = 0;
            int places = Mathf.Max(localPilots.Count, coop.Pilots);
            Vector2 halfSize = Playground != null ? Playground.HalfSize : StrikeRules.HalfSize;
            flown.Position = strike ? FieldMath.RowPoint(slot, places, halfSize) : FieldMath.StartPoint(slot, places, CoopRules.StartRadius);
            ApplySeed(flown, seed, fromPose, out bool alive, out int shipsLeft);
            int ships = strike ? 1 : seed.HasPose ? Mathf.Max(1, shipsLeft) : Mathf.Clamp(coop.Lives, 1, AsteroidSettings.LivesLimit);
            pilot.Record = squad.Add(local, ships, (int)Mathf.Clamp(seed.Score, 0, int.MaxValue));
            if (!alive && !strike)
            {
                // The ship was lost a moment ago: the next one warps in here.
                pilot.RespawnIn = respawnDelay;
            }
            RefreshActivePlayers();
            ui?.ShowLocalHud(true);
            StrikeScreens?.ShowLocalHud(true);
            if (phase == MissionPhase.Watching && IsMissionActive)
            {
                phase = MissionPhase.Playing;
                phaseTime = 0f;
            }
            return true;
        }


        /// <summary>
        /// The shared screen stops flying room seat <paramref name="seat"/> (its player plays on their own device from now
        /// on): the wingman goes; the online controller shows the seat as a stand-in instead. False for a seat not flown here.
        /// </summary>
        internal bool ReleaseDrivenPilot(int seat)
        {
            LocalPilot pilot = PartyPilotAt(seat);
            if (pilot == null)
            {
                return false;
            }
            Release(pilot);
            localPilots.Remove(pilot);
            squad?.Remove(seat);
            RefreshActivePlayers();
            return true;
        }


        /// <summary>
        /// The loadout the remote of room seat <paramref name="seat"/> flies a strike mission with arrived: the wingman of
        /// the seat flies with it from now on (the difficulty stays the room's). Nothing happens for a seat not flown here.
        /// </summary>
        internal void ApplyDrivenLoadout(int seat, StrikeLoadout loadout)
        {
            LocalPilot pilot = PartyPilotAt(seat);
            if (pilot == null || pilot.Working == null || loadout == null || !IsStrike)
            {
                return;
            }
            StrikeDifficulty difficulty = pilot.Working.Difficulty;
            pilot.Working.CopyFrom(loadout);
            pilot.Working.Difficulty = difficulty;
        }


        /// <summary>
        /// What the shared screen tells the remote of room seat <paramref name="seat"/> about its seat
        /// (<see cref="RemoteSeatStatus"/>); null for a seat not flown here.
        /// </summary>
        internal string DrivenSeatStatus(int seat)
        {
            LocalPilot pilot = PartyPilotAt(seat);
            if (pilot == null || pilot.Record == null || pilot.Ship == null)
            {
                return null;
            }
            AsteroidsPlayer flown = pilot.Ship;
            bool flying = !pilot.Record.Out && flown.IsAlive;
            long points = pilot.Record.Score.Score;
            if (IsStrike)
            {
                StrikeLoadout loadout = pilot.Working;
                string special = loadout != null && loadout.HasSpecial ? StrikeUI.ShortName(loadout.Special) : null;
                return RemoteSeatStatus.WriteStrike(points, loadout != null ? loadout.Energy : flown.Health, loadout != null ? loadout.PhaseShields : 0,
                    loadout != null ? loadout.Megabombs : flown.Bombs, special, flying, StrikeLoadoutText.Write(loadout));
            }
            float hull = flown.MaxHealth > 0f ? flown.Health / flown.MaxHealth : 0f;
            float shield = flown.MaxShield > 0f ? flown.Shield / flown.MaxShield : 0f;
            return RemoteSeatStatus.WriteField(pilot.Record.Lives, points, flown.Weapons.Type, flown.Weapons.Level, flown.Bombs, hull, shield, flying);
        }


        /// <summary>
        /// Puts <paramref name="flown"/> where <paramref name="seed"/> says, flying as it did: position, velocity, heading,
        /// alive or lost, hull and shield, the magnet and the drones, the weapon and the bombs. In a strike mission
        /// <paramref name="loadout"/> (when given) takes the energy, the shield points and the megabombs of the pose, else
        /// the loadout the ship flies with already is the truth. Without a pose the ship stays as it was reset.
        /// </summary>
        private static void ApplySeed(AsteroidsPlayer flown, SeatSeed seed, StrikeLoadout loadout, out bool alive, out int shipsLeft)
        {
            alive = true;
            shipsLeft = 0;
            if (!seed.HasPose || flown == null)
            {
                return;
            }
            ShipPose pose = seed.Pose;
            alive = pose.Alive;
            shipsLeft = pose.Lives;
            flown.Position = seed.Position;
            flown.transform.rotation = Quaternion.Euler(0f, 0f, seed.Heading);
            flown.Simulation.Velocity = seed.Velocity;
            flown.Mirror(pose, flown.MaxHealth);
            if (flown.Mode == MissionMode.Strike)
            {
                if (loadout != null)
                {
                    loadout.Energy = Mathf.Clamp(pose.Health * StrikeRules.MaxEnergy, 0f, StrikeRules.MaxEnergy);
                    if (loadout.PhaseShields > 0)
                    {
                        loadout.ShieldPoints = Mathf.Clamp(pose.Shield * StrikeRules.PhaseShieldPoints, 0f, StrikeRules.PhaseShieldPoints);
                    }
                    loadout.Megabombs = pose.Bombs;
                    loadout.NotifyChanged();
                }
                else
                {
                    flown.SyncFromLoadout();
                }
            }
            else
            {
                flown.Weapons.Set((WeaponType)Mathf.Clamp(pose.Weapon, 0, (int)WeaponType.Wave), Mathf.Clamp(pose.WeaponLevel, 1, WeaponRules.MaxLevel));
                flown.SetBombs(pose.Bombs);
            }
            if (!alive)
            {
                flown.gameObject.SetActive(false);
            }
        }
    }
}
