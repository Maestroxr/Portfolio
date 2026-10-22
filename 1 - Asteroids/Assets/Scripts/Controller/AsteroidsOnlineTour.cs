using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Gamebox;
using Gamebox.Online;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// Development players only. Started with <c>-asteroids-online &lt;role&gt; &lt;folder&gt; [name]</c>, it flies an
    /// online mission by itself and saves a log and screenshots into the folder, then quits (<see cref="OnlineTour"/>):
    /// the <c>host</c> opens a room and starts it once somebody joined and is ready, <c>join</c> joins the first room it
    /// sees, and both ships are flown by the <see cref="AsteroidsAutopilot"/> (the <see cref="StrikeAutopilot"/> in a planet
    /// strike mission). The screenshots of a mission are taken the same number of seconds after it began on both clients,
    /// and a line with the bodies of the playfield goes into the log with each (in a strike mission with the scroll, the
    /// money and the energy), so the two can be compared. <c>-asteroids-level &lt;index&gt;</c> picks the mission (the
    /// flat index: the strike missions follow the field ones), <c>-asteroids-lives &lt;n&gt;</c> the ships per pilot,
    /// <c>-asteroids-difficulty &lt;0..2&gt;</c> the strike difficulty of the room, <c>-asteroids-skip-to-boss
    /// &lt;seconds&gt;</c> makes the simulator jump its scroll to the boss that long after the mission began, and
    /// <c>-asteroids-leave &lt;seconds&gt;</c> makes this pilot leave the match that long after it began. The host's room
    /// takes four pilots (a guest may bring players to its device), or as many as <c>-gamebox-room-players</c> says. A won strike
    /// mission banks the pilot, and both players share the saved progress: the strike pilot is put back as it was when
    /// the tour ends. Run the players with <c>-gamebox-identity</c> to tell them apart, and <c>-gamebox-server</c> /
    /// <c>-gamebox-database</c> for a test server. For the armory in co-op, <c>-asteroids-kit &lt;item&gt;[+&lt;item&gt;...]</c>
    /// gives this pilot's strike loadout the items of a kit before the match (in memory only, like the <c>kit:</c> step of
    /// <see cref="AsteroidsTour"/>), and <c>-asteroids-weapon &lt;type&gt;[:&lt;level&gt;]</c> arms this pilot's ship with that
    /// field weapon (default the top level) for the whole match.
    ///
    /// The roles of the base tour work too: <c>hostprivate</c> / <c>joinprivate</c> play a private room entered with its
    /// code, and <c>hostparty</c> / <c>joinparty</c> bring other players to the device first (the companions, and guests
    /// with <c>-gamebox-guests</c>), whose ships the manager flies here as a party: the tour puts the autopilot on every ship
    /// flown at this device, and the log names the pilot of every seat.
    ///
    /// The shared screen: <c>hostscreen</c> opens the room as its screen, waits for two ready guests, starts the mission
    /// and flies nothing itself (the screen flies the seats of the remotes from their pads); <c>joinremote</c> joins as a
    /// remote control and plays through the pad: the autopilot reads the stand-in of its own ship (which the screen
    /// flies) and the tour feeds what it wants to <see cref="OnlineGameController.SetPadState"/> every frame;
    /// <c>joinown</c> joins the screen's room and plays on its own device, as <c>join</c> does. <c>-asteroids-switch
    /// &lt;seconds&gt;</c> makes a remote switch to playing on its own device that long after the mission began (the
    /// autopilot takes the scene's ship), and <c>-asteroids-switch-back &lt;seconds&gt;</c> makes it a remote again.
    /// </summary>
    public class AsteroidsOnlineTour : OnlineTour
    {
        private const string Argument = "-asteroids-online";
        private const string LevelArgument = "-asteroids-level";
        private const string LivesArgument = "-asteroids-lives";
        private const string LeaveArgument = "-asteroids-leave";
        private const string DifficultyArgument = "-asteroids-difficulty";
        private const string SkipToBossArgument = "-asteroids-skip-to-boss";
        private const string KitArgument = "-asteroids-kit";
        private const string WeaponArgument = "-asteroids-weapon";
        private const string SwitchArgument = "-asteroids-switch";
        private const string SwitchBackArgument = "-asteroids-switch-back";

        private static readonly float[] ShotTimes = { 4f, 12f, 24f, 40f, 60f, 90f };

        /// <summary>A strike mission runs longer (the scroll alone takes two minutes and more).</summary>
        private static readonly float[] StrikeShotTimes = { 4f, 12f, 24f, 40f, 60f, 90f, 120f, 150f, 180f, 240f };

        private AsteroidsGameManager manager;
        private bool pilotSaved;
        private bool hadPilot;
        private StrikeLoadout savedPilot;
        private bool pilotsOn;
        private RemoteShip padShip;
        private Behaviour padPilot;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Launch()
        {
            Bootstrap<AsteroidsOnlineTour>(Argument);
        }

        private bool Ended => manager.State.Is(BaseGameState.Victory) || manager.State.Is(BaseGameState.GameOver) || !manager.InSession;

        private IEnumerator Start()
        {
            OpenLog(true);
            while ((manager = FindAnyObjectByType<AsteroidsGameManager>()) == null || manager.Progress == null)
            {
                yield return null;
            }
            yield return new WaitForSeconds(1f);
            SavePilot();
            GiveKit();
            if (manager.online == null)
            {
                yield return Fail("the scene has no online controller");
                yield break;
            }
            yield return Shot("00_missions");
            yield return LogIn(manager.online);
            if (!LoggedIn)
            {
                yield return Fail("no login");
                yield break;
            }
            yield return new WaitForSeconds(0.5f);

            if (Hosting)
            {
                string options = RoomOptions.Write(CoopRules.LivesKey, NumberArgument(LivesArgument, 5),
                    CoopRules.DifficultyKey, NumberArgument(DifficultyArgument, (int)CoopRules.DefaultDifficulty), CoopRules.MissionsKey, manager.LevelCount);
                // A party's players take seats of their own: the host's besides the guest the room waits for, and a guest may
                // bring players to its device too, so the room takes as many pilots as a room can (-gamebox-room-players
                // says otherwise).
                yield return HostRoom("Tour room", NumberArgument(LevelArgument, 0), options, (byte)Mathf.Max(CoopRules.MaxPilots, 2 + PartyExtras));
                yield return Shot("01_room");
                // A shared screen holds no seat: the room needs two players at other devices.
                yield return WaitForGuests(AsScreen ? 2 : 1, 180f);
                yield return new WaitForSeconds(0.5f);
                yield return Shot("02_everybody_ready");
                StartRoom();
            }
            else
            {
                yield return JoinFirstRoom(180f);
                yield return new WaitForSeconds(0.7f);
                yield return Shot("02_room");
            }

            yield return WaitFor(() => manager.IsCoop && manager.IsGameRunning, 60f, "the mission");
            if (!manager.IsCoop)
            {
                yield return Fail("the mission did not start");
                yield break;
            }
            bool strike = manager.IsStrike;
            Note($"mission {manager.LevelIndex} started, simulates: {manager.Simulates}, playfield {manager.Playground.HalfSize}" +
                 (strike ? $", strike at {manager.MissionDifficulty}, wraps {manager.Playground.Wraps}, pilot money {manager.Pilot.Money}" : string.Empty));
            Note($"pilots at this device: {DescribeSeats()}");
            yield return Shot("03_briefing");
            yield return WaitFor(() => manager.IsMissionActive, 20f, "the countdown");
            // The screen flies nothing of its own (the remotes' pads fly their seats); a remote flies its own seat only
            // while it plays on its own device.
            List<Behaviour> pilots = AsScreen ? new List<Behaviour>() : Autopilots(strike);
            SetPilots(pilots, !AsScreen && manager.online.DrivesOwnSeat);
            (WeaponType type, int level)? weapon = strike ? null : WeaponFromArguments();
            if (weapon.HasValue)
            {
                manager.Ship.Weapons.Set(weapon.Value.type, weapon.Value.level);
                Note($"weapon {manager.Ship.Weapons.Type} {manager.Ship.Weapons.Level}");
            }

            // The same moments on both clients: seconds since the mission went live.
            float[] times = strike ? StrikeShotTimes : ShotTimes;
            float leaveAfter = NumberArgument(LeaveArgument, -1f);
            float skipAfter = strike ? NumberArgument(SkipToBossArgument, -1f) : -1f;
            float switchAfter = NumberArgument(SwitchArgument, -1f);
            float switchBackAfter = NumberArgument(SwitchBackArgument, -1f);
            float switchShotAt = -1f;
            string switchShot = null;
            float began = Time.realtimeSinceStartup;
            int shot = 0;
            while (!Ended)
            {
                float elapsed = Time.realtimeSinceStartup - began;
                if (weapon.HasValue && manager.Ship.IsAlive && manager.Ship.Weapons.Type != weapon.Value.type)
                {
                    // A crate picked up on the way switched weapons: the run is about this one.
                    manager.Ship.Weapons.Set(weapon.Value.type, weapon.Value.level);
                }
                if (!AsScreen)
                {
                    // The autopilot flies the scene's ship while this device flies its own seat; otherwise the pad does.
                    bool drivesOwn = manager.online.DrivesOwnSeat;
                    if (drivesOwn != pilotsOn)
                    {
                        SetPilots(pilots, drivesOwn);
                        Note($"t+{elapsed:0.0}s this device {(drivesOwn ? "flies its own seat" : "flies nothing: the screen flies its seat")}; {DescribeSeats()}");
                    }
                    if (!drivesOwn && manager.online.IsRemote)
                    {
                        FeedPad(strike);
                    }
                }
                if (shot < times.Length && elapsed >= times[shot])
                {
                    Note($"t+{times[shot]:0}s {Describe()}");
                    yield return Shot($"04_flying_{shot + 1}");
                    shot++;
                }
                if (switchAfter >= 0f && elapsed >= switchAfter)
                {
                    switchAfter = -1f;
                    Note("switching to this device");
                    manager.online.SetRemoteMode(false, error => Note("own device: " + (error ?? "ok")));
                    switchShotAt = elapsed + 2f;
                    switchShot = "04_own_device";
                }
                if (switchBackAfter >= 0f && elapsed >= switchBackAfter)
                {
                    switchBackAfter = -1f;
                    Note("switching back to the remote control");
                    manager.online.SetRemoteMode(true, error => Note("remote control: " + (error ?? "ok")));
                    switchShotAt = elapsed + 2f;
                    switchShot = "04_remote_again";
                }
                if (switchShotAt >= 0f && elapsed >= switchShotAt)
                {
                    switchShotAt = -1f;
                    Note($"t+{elapsed:0.0}s {Describe()}");
                    yield return Shot(switchShot);
                }
                if (skipAfter >= 0f && elapsed >= skipAfter)
                {
                    // Only the simulator runs the scroll; the others follow it there.
                    skipAfter = -1f;
                    if (manager.Simulates)
                    {
                        Note($"skipping to the boss at scroll {manager.ScrollDistance:0.0} m");
                        manager.SkipToBoss();
                    }
                }
                if (leaveAfter > 0f && elapsed >= leaveAfter)
                {
                    Note("leaving the match");
                    manager.online.LeaveMatch();
                    yield return new WaitForSeconds(2f);
                    yield return Shot("05_left");
                    Note("done");
                    Quit();
                    yield break;
                }
                if (elapsed > 600f)
                {
                    Note("TIMEOUT waiting for the end of the mission");
                    break;
                }
                yield return null;
            }
            SetPilots(pilots, false);
            Note($"ended after {Time.realtimeSinceStartup - began:0.0}s in state {manager.State}; {Describe()}");
            yield return new WaitForSeconds(2.5f);
            yield return Shot("05_results");
            Note("standings: " + manager.online.DescribeStandings().Replace("\n", " | "));
            if (strike)
            {
                Note($"pilot money after the mission {manager.Pilot.Money} (before {(savedPilot != null ? savedPilot.Money : manager.Pilot.Money)})");
                Note($"pilot kit after the mission: {AsteroidsTour.Describe(manager.Pilot)} (before: {AsteroidsTour.Describe(savedPilot)})");
                if (manager.online.LastRemoteLoadout != null)
                {
                    Note($"the screen's last word on this seat's kit: {AsteroidsTour.Describe(manager.online.LastRemoteLoadout)}");
                }
            }

            if (AsScreen)
            {
                // Nobody presses anything on a shared screen: it goes back to the room by itself after the results.
                yield return WaitFor(() => !manager.InSession, OnlineGameController.ScreenResultsSeconds + 5f, "the screen's own return to the room");
            }
            else
            {
                manager.ReturnToMissionSelect();
            }
            yield return new WaitForSeconds(1.5f);
            yield return Shot("06_back_in_the_room");
            Note($"back in the room: in session {manager.InSession}, playfield fixed {manager.Playground.IsFixed}, stand-ins {manager.RemoteShips.Count}, " +
                 $"wingmen {manager.Field.Wingmen.Count}, ship seat {manager.Ship.Seat}");
            // The guest leaves first, so the host still sees the room lose a member.
            yield return new WaitForSeconds(Hosting ? 3f : 0.5f);
            Server.LeaveRoom();
            yield return new WaitForSeconds(1f);
            RestorePilot();
            Note("done");
            Quit();
        }


        private void OnApplicationQuit()
        {
            RestorePilot();
        }


        /// <summary>Gives the pilot the kit of <c>-asteroids-kit</c>, if any (after <see cref="SavePilot"/>, so the tour puts it back).</summary>
        private void GiveKit()
        {
            string kit = TextArgument(KitArgument);
            if (string.IsNullOrEmpty(kit))
            {
                return;
            }
            foreach (string unknown in AsteroidsTour.GiveKit(manager.Pilot, kit))
            {
                Note($"kit: no item named {unknown}");
            }
            Note($"kit {kit}: pilot {AsteroidsTour.Describe(manager.Pilot)}");
        }


        /// <summary>The field weapon of <c>-asteroids-weapon &lt;type&gt;[:&lt;level&gt;]</c>, or null.</summary>
        private (WeaponType type, int level)? WeaponFromArguments()
        {
            string text = TextArgument(WeaponArgument);
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }
            string[] parts = text.Split(':');
            if (!System.Enum.TryParse(parts[0], true, out WeaponType type) || !System.Enum.IsDefined(typeof(WeaponType), type))
            {
                Note($"weapon: no weapon named {parts[0]}");
                return null;
            }
            int level = parts.Length > 1 && int.TryParse(parts[1], out int parsed) ? Mathf.Clamp(parsed, 1, WeaponRules.MaxLevel) : WeaponRules.MaxLevel;
            return (type, level);
        }


        /// <summary>The text after a command line argument, or null.</summary>
        private static string TextArgument(string name)
        {
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name)
                {
                    return args[i + 1];
                }
            }
            return null;
        }


        /// <summary>
        /// The autopilot of every ship flown at this device (the scene's ship, and a party's wingmen), switched off: the
        /// asteroid autopilot, or the strike autopilot, added when the ship has none yet.
        /// </summary>
        private List<Behaviour> Autopilots(bool strike)
        {
            var ships = new List<AsteroidsPlayer> { manager.Ship };
            if (manager.IsParty)
            {
                foreach (AsteroidsGameManager.LocalPilot pilot in manager.LocalPilots)
                {
                    if (pilot.Wingman && pilot.Ship != null)
                    {
                        ships.Add(pilot.Ship);
                    }
                }
            }
            var pilots = new List<Behaviour>();
            foreach (AsteroidsPlayer ship in ships)
            {
                Behaviour pilot = strike ? StrikePilot(ship) : ship.GetComponent<AsteroidsAutopilot>();
                if (pilot != null)
                {
                    pilots.Add(pilot);
                }
            }
            return pilots;
        }


        /// <summary>Switches the autopilots of the ships flown at this device on or off.</summary>
        private void SetPilots(List<Behaviour> pilots, bool on)
        {
            pilotsOn = on;
            foreach (Behaviour pilot in pilots)
            {
                if (pilot != null)
                {
                    pilot.enabled = on;
                }
            }
        }


        /// <summary>
        /// A remote control's frame: the autopilot reads the stand-in of this device's own ship (which the screen flies
        /// from this pad) and what it wants goes out as the pad's stick and buttons. Without a stand-in yet the pad only
        /// holds fire.
        /// </summary>
        private void FeedPad(bool strike)
        {
            RemoteShip own = manager.RemoteShipAt(manager.online.LocalSeat);
            if (own == null)
            {
                manager.online.SetPadState(Vector2.zero, Vector2.zero, RemotePadPacket.Bit(LocalCoopRules.Fire), 0);
                return;
            }
            if (padShip != own || padPilot == null)
            {
                padShip = own;
                padPilot = strike ? StrikePilot(own.Player) : own.GetComponent<AsteroidsAutopilot>();
                if (padPilot != null)
                {
                    // Read by the tour only: the stand-in is never simulated here.
                    padPilot.enabled = false;
                }
                // A stand-in knows no playfield (it is only moved); the autopilot looks at the one of this device.
                if (own.Player.Field == null)
                {
                    own.Player.Field = manager.Field;
                }
            }
            if (!(padPilot is IShipInput input))
            {
                return;
            }
            input.Read(own.Player, Time.deltaTime);
            RemotePadPacket.From(input, strike, out Vector2 stick, out uint held, out uint pressed);
            manager.online.SetPadState(stick, Vector2.zero, held, pressed);
        }


        /// <summary>The strike autopilot of <paramref name="ship"/>; added when the ship has none yet.</summary>
        private static Behaviour StrikePilot(AsteroidsPlayer ship)
        {
            StrikeAutopilot pilot = ship.GetComponentInChildren<StrikeAutopilot>(true);
            if (pilot == null)
            {
                pilot = ship.gameObject.AddComponent<StrikeAutopilot>();
                pilot.enabled = false;
            }
            return pilot;
        }


        /// <summary>Remembers the saved strike pilot, which a won mission changes (both players of a tour share the progress).</summary>
        private void SavePilot()
        {
            AsteroidsProgress progress = manager != null ? manager.Progress : null;
            if (progress == null || pilotSaved)
            {
                return;
            }
            pilotSaved = true;
            hadPilot = progress.HasPilot;
            savedPilot = progress.LoadPilot();
        }


        /// <summary>Puts the strike pilot back as it was before the tour (or forgets it when there was none).</summary>
        private void RestorePilot()
        {
            AsteroidsProgress progress = manager != null ? manager.Progress : null;
            if (progress == null || !pilotSaved)
            {
                return;
            }
            pilotSaved = false;
            if (hadPilot)
            {
                progress.SavePilot(savedPilot);
            }
            else
            {
                progress.ResetPilot();
            }
            Note($"strike pilot restored ({(hadPilot ? $"money {savedPilot.Money}" : "none saved")})");
        }


        /// <summary>The seats flown at this device and by whom: the device's own first (none on the shared screen or a remote control).</summary>
        private string DescribeSeats()
        {
            AsteroidsOnlineController online = manager.online;
            List<byte> seats = online.PartySeats();
            string party = manager.IsParty
                ? string.Join(", ", manager.LocalPilots.Select(pilot => $"seat {pilot.Seat.Index} {pilot.Seat.Name} ({(pilot.Wingman ? "wingman" : "scene ship")}, " +
                    $"controls {(pilot.Ship.OwnControls is LocalShipInput input && input.Controls != null ? "set " + online.DrivenIndexOf((byte)pilot.Seat.Index) : "none")})"))
                : online.DrivesOwnSeat ? $"seat {online.LocalSeat} alone" : "none";
            string device = online.IsScreen ? "the shared screen" : online.IsRemote ? $"a remote control of seat {online.LocalSeat}" : $"a device of seat {online.LocalSeat}";
            return $"{seats.Count} ({party}) on {device}; stand-ins for seats [{string.Join(", ", manager.RemoteShips.Select(remote => remote.Seat))}]";
        }


        /// <summary>What this client has in its playfield right now, for comparing it with the other one.</summary>
        private string Describe()
        {
            SpaceField field = manager.Field;
            AsteroidsPlayer ship = manager.Ship;
            string ships = string.Join(", ", manager.RemoteShips.Select(remote =>
                $"{remote.Player.DisplayName} (seat {remote.Seat}) at ({remote.transform.position.x:0.0}, {remote.transform.position.y:0.0}) {(remote.Flying ? "flying" : "down")}"));
            string strike = string.Empty;
            if (manager.IsStrike)
            {
                float progress = manager.Simulates ? manager.ScrollProgress : manager.CoopScrollProgress;
                strike = $"scroll {manager.ScrollDistance:0.0} m ({progress:P0}), energy {ship.Health:0}, shield {ship.Shield:0}, " +
                         $"hostiles {manager.HostilesDestroyed}/{manager.HostilesEntered}, ";
            }
            string party = string.Empty;
            if (manager.IsParty)
            {
                party = "party: " + string.Join("; ", manager.LocalPilots.Select(pilot =>
                    $"seat {pilot.Seat.Index} {pilot.Seat.Name} {(pilot.Ship.IsAlive ? "flying" : "down")} at ({pilot.Ship.Position.x:0.0}, {pilot.Ship.Position.y:0.0}), " +
                    $"score {pilot.Record?.Score.Score}, kills {pilot.Record?.Score.Kills}, ships {pilot.Record?.Lives}{(pilot.Record != null && pilot.Record.Out ? ", OUT" : "")}")) + "; ";
            }
            if (manager.online.IsRemote && !manager.online.DrivesOwnSeat)
            {
                party += $"remote status [{manager.online.RemoteStatus}]; ";
            }
            return $"{strike}score {manager.Scoring.Score}, lives {manager.Lives}, ship ({ship.Position.x:0.0}, {ship.Position.y:0.0}) {(ship.IsAlive ? "alive" : "down")}; " +
                   $"{party}others: [{ships}]; field: targets {field.Targets.Count}, enemy shots {field.EnemyShots.Count}, pickups {field.Rewards.Count}, " +
                   $"own and ghost shots {field.PlayerShots.Count}; net: {manager.online.Replication.Describe()}";
        }
    }
}
