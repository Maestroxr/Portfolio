using System.Collections;
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
    /// <c>-asteroids-leave &lt;seconds&gt;</c> makes this pilot leave the match that long after it began. A won strike
    /// mission banks the pilot, and both players share the saved progress: the strike pilot is put back as it was when
    /// the tour ends. Run the players with <c>-gamebox-identity</c> to tell them apart, and <c>-gamebox-server</c> /
    /// <c>-gamebox-database</c> for a test server.
    /// </summary>
    public class AsteroidsOnlineTour : OnlineTour
    {
        private const string Argument = "-asteroids-online";
        private const string LevelArgument = "-asteroids-level";
        private const string LivesArgument = "-asteroids-lives";
        private const string LeaveArgument = "-asteroids-leave";
        private const string DifficultyArgument = "-asteroids-difficulty";
        private const string SkipToBossArgument = "-asteroids-skip-to-boss";

        private static readonly float[] ShotTimes = { 4f, 12f, 24f, 40f, 60f, 90f };

        /// <summary>A strike mission runs longer (the scroll alone takes two minutes and more).</summary>
        private static readonly float[] StrikeShotTimes = { 4f, 12f, 24f, 40f, 60f, 90f, 120f, 150f, 180f, 240f };

        private AsteroidsGameManager manager;
        private bool pilotSaved;
        private bool hadPilot;
        private StrikeLoadout savedPilot;

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
                yield return HostRoom("Tour room", NumberArgument(LevelArgument, 0), options, 2);
                yield return Shot("01_room");
                yield return WaitForGuests(1, 180f);
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
            yield return Shot("03_briefing");
            yield return WaitFor(() => manager.IsMissionActive, 20f, "the countdown");
            Behaviour pilot = strike ? StrikePilot() : manager.Ship.GetComponent<AsteroidsAutopilot>();
            if (pilot != null)
            {
                pilot.enabled = true;
            }

            // The same moments on both clients: seconds since the mission went live.
            float[] times = strike ? StrikeShotTimes : ShotTimes;
            float leaveAfter = NumberArgument(LeaveArgument, -1f);
            float skipAfter = strike ? NumberArgument(SkipToBossArgument, -1f) : -1f;
            float began = Time.realtimeSinceStartup;
            int shot = 0;
            while (!Ended)
            {
                float elapsed = Time.realtimeSinceStartup - began;
                if (shot < times.Length && elapsed >= times[shot])
                {
                    Note($"t+{times[shot]:0}s {Describe()}");
                    yield return Shot($"04_flying_{shot + 1}");
                    shot++;
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
            if (pilot != null)
            {
                pilot.enabled = false;
            }
            Note($"ended after {Time.realtimeSinceStartup - began:0.0}s in state {manager.State}; {Describe()}");
            yield return new WaitForSeconds(2.5f);
            yield return Shot("05_results");
            Note("standings: " + manager.online.DescribeStandings().Replace("\n", " | "));
            if (strike)
            {
                Note($"pilot money after the mission {manager.Pilot.Money} (before {(savedPilot != null ? savedPilot.Money : manager.Pilot.Money)})");
            }

            manager.ReturnToMissionSelect();
            yield return new WaitForSeconds(1.5f);
            yield return Shot("06_back_in_the_room");
            Note($"back in the room: in session {manager.InSession}, playfield fixed {manager.Playground.IsFixed}, stand-ins {manager.RemoteShips.Count}");
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


        /// <summary>The strike autopilot of the local ship; added when the ship prefab has none yet.</summary>
        private Behaviour StrikePilot()
        {
            AsteroidsPlayer ship = manager.Ship;
            var pilot = ship.GetComponent<StrikeAutopilot>();
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

        /// <summary>What this client has in its playfield right now, for comparing it with the other one.</summary>
        private string Describe()
        {
            SpaceField field = manager.Field;
            AsteroidsPlayer ship = manager.Ship;
            string ships = string.Join(", ", manager.RemoteShips.Select(remote =>
                $"{remote.Player.DisplayName} at ({remote.transform.position.x:0.0}, {remote.transform.position.y:0.0}) {(remote.Flying ? "flying" : "down")}"));
            string strike = string.Empty;
            if (manager.IsStrike)
            {
                float progress = manager.Simulates ? manager.ScrollProgress : manager.CoopScrollProgress;
                strike = $"scroll {manager.ScrollDistance:0.0} m ({progress:P0}), energy {ship.Health:0}, shield {ship.Shield:0}, " +
                         $"hostiles {manager.HostilesDestroyed}/{manager.HostilesEntered}, ";
            }
            return $"{strike}score {manager.Scoring.Score}, lives {manager.Lives}, ship ({ship.Position.x:0.0}, {ship.Position.y:0.0}) {(ship.IsAlive ? "alive" : "down")}; " +
                   $"others: [{ships}]; field: targets {field.Targets.Count}, enemy shots {field.EnemyShots.Count}, pickups {field.Rewards.Count}, " +
                   $"own and ghost shots {field.PlayerShots.Count}; net: {manager.online.Replication.Describe()}";
        }
    }
}
