using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Gamebox;
using Gamebox.Online;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// Development players only. Started with <c>-deathrace-online &lt;role&gt; &lt;folder&gt; [name]</c>, it runs an online
    /// race by itself and saves a log and screenshots into the folder, then quits (BaseGame's <see cref="OnlineTour"/>): the
    /// <c>host</c> opens a room (track <c>-deathrace-online-track</c>, default 0, with <c>-deathrace-online-laps</c> laps,
    /// default 2, in the Viper) and starts it once its guests (<c>-deathrace-online-guests</c>, default 1) are in and ready;
    /// <c>join</c> joins the first room it sees. The roles of the base tour play private rooms (<c>hostprivate</c>,
    /// <c>joinprivate</c>) and parties (<c>hostparty</c>, <c>joinparty</c>: <c>-gamebox-companions</c> and
    /// <c>-gamebox-guests</c> players join at the device first and race in split screen). Every car of this device is driven
    /// by the computer (<see cref="AiDriver"/>, of <c>-deathrace-online-skill</c>, default 0.85), so the race plays itself; the log has what the online controller noted
    /// (pickups and mines claimed, wrecks, the results reported), every action of the room, the cars every few seconds
    /// (where this device's are, how fresh the poses of the others are), the standings the room sent (a STANDINGS line to
    /// compare between the players) and anything that went wrong. The host opens the match menu once mid race.
    /// </summary>
    public class DeathRaceOnlineTour : OnlineTour
    {
        private const string Argument = "-deathrace-online";
        private const string TrackArgument = "-deathrace-online-track";
        private const string LapsArgument = "-deathrace-online-laps";
        private const string OthersArgument = "-deathrace-online-guests";
        private const string SkillArgument = "-deathrace-online-skill";
        private const string StopArgument = "-deathrace-online-stop";

        private const float RacePatience = 420f;

        private DeathRaceManager manager;
        private DeathRaceOnlineController online;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Launch()
        {
            Bootstrap<DeathRaceOnlineTour>(Argument);
        }

        private bool Ended => online.LastStandings != null && manager.State != null
            && (manager.State.Is(BaseGameState.Victory) || manager.State.Is(BaseGameState.GameOver));

        private IEnumerator Start()
        {
            OpenLog(true);
            while ((manager = FindAnyObjectByType<DeathRaceManager>()) == null || manager.State == null)
            {
                yield return null;
            }
            yield return new WaitForSeconds(2f);
            yield return Shot("00_menu");
            online = manager.OnlineRaces;
            if (online == null)
            {
                yield return Fail("the scene has no online controller");
                yield break;
            }
            online.Noted += line => Note("online: " + line);
            yield return LogIn(online);
            if (!LoggedIn)
            {
                yield return Fail("no login");
                yield break;
            }
            LogActions(KindName, () => $"t {(online.OnlineRace != null ? online.OnlineRace.Time : 0f):0.00}");
            yield return new WaitForSeconds(0.5f);
            yield return Shot("01_lobby");

            if (Hosting)
            {
                IReadOnlyList<RoomLevelChoice> choices = online.LevelChoices;
                int wanted = NumberArgument(TrackArgument, choices.Count > 0 ? choices[0].Index : 0);
                int track = choices.Any(choice => choice.Index == wanted) ? wanted : choices[0].Index;
                int laps = Mathf.Clamp(NumberArgument(LapsArgument, 2), 1, OnlineRaceRules.MaxLaps);
                string options = online.ComposeOptions(track, RoomOptions.Write(OnlineRaceRules.LapsOption, laps, OnlineRaceRules.CarOption, (int)CarModel.Viper));
                Note($"opening a room of 4 on track {track} with options {options}");
                yield return HostRoom("Tour race", track, options, 4);
                if (!Server.InRoom)
                {
                    yield return Fail("no room");
                    yield break;
                }
                yield return Shot("02_room");
                yield return WaitForGuests(Mathf.Max(1, NumberArgument(OthersArgument, 1)), 180f);
                yield return new WaitForSeconds(0.5f);
                yield return Shot("03_everybody_ready");
                StartRoom();
            }
            else
            {
                yield return JoinFirstRoom(180f);
                if (!Server.InRoom)
                {
                    yield return Fail("no room to join");
                    yield break;
                }
                yield return new WaitForSeconds(0.7f);
                yield return Shot("02_room");
            }

            yield return Race();
            // The guest leaves first, so the host still sees the room lose a member.
            yield return new WaitForSeconds(Hosting ? 3f : 0.5f);
            Server.LeaveRoom();
            yield return new WaitForSeconds(1f);
            Note("done");
            Quit();
        }

        /// <summary>A race of the room, from its start to the way back into the room.</summary>
        private IEnumerator Race()
        {
            yield return WaitFor(() => online.OnlineRace != null && manager.IsRacing, 90f, "the race");
            Race race = online.OnlineRace;
            if (race == null)
            {
                yield return Fail("the race did not start");
                yield break;
            }
            PilotLocalCars(race);
            Note($"racing on {race.Track.Layout.Name}, {race.Laps} laps, seed {race.Setup.Seed}: "
                + string.Join(", ", race.Cars.Select(car => $"seat {car.Entrant.RoomSeat} {car.Entrant.Name} {car.Entrant.Driver} grid P{car.Racer.Place}")));
            yield return new WaitForSeconds(RaceDirector.GridSeconds + 1f);
            yield return Shot("04_countdown");

            float start = Time.unscaledTime;
            float nextReport = start + 4f;
            float[] shots = { 10f, 25f, 45f };
            int shot = 0;
            bool matchMenuShown = false;
            float stopAt = NumberArgument(StopArgument, -1f);
            while (!Ended && Time.unscaledTime - start < RacePatience)
            {
                Race current = online.OnlineRace;
                float raceTime = current != null ? current.Time : 0f;
                if (shot < shots.Length && raceTime > shots[shot])
                {
                    shot++;
                    yield return Shot($"{4 + shot:00}_racing");
                }
                if (Hosting && !matchMenuShown && raceTime > 18f && manager.IsRacing)
                {
                    // A race does not pause: what pauses a game alone opens the match menu, and the cars race on.
                    matchMenuShown = true;
                    manager.ActiveController.TransitionState(BaseGameState.Paused);
                    yield return new WaitForSeconds(0.6f);
                    yield return Shot("08_match_menu");
                    Note($"match menu: state {manager.State}, time scale {Time.timeScale}, race clock {(online.OnlineRace != null ? online.OnlineRace.Time : 0f):0.0}");
                    manager.ActiveController.TransitionState(BaseGameState.Running);
                }
                if (stopAt > 0f && raceTime > stopAt)
                {
                    // This device's cars pull over and wait: the others finish, and the time after the first finisher runs out.
                    stopAt = -1f;
                    foreach (Car car in online.LocalCars.Values)
                    {
                        if (car != null)
                        {
                            car.Driver = ScriptedDriver.Constant(new CarInput { Throttle = -1f });
                        }
                    }
                    Note($"the cars here stop at t {raceTime:0.0}");
                }
                if (Time.unscaledTime >= nextReport)
                {
                    nextReport += 5f;
                    Progress();
                }
                yield return null;
            }
            if (!Ended)
            {
                Note("TIMEOUT waiting for the standings");
                Progress();
                yield return Shot("09_timeout");
                yield break;
            }
            yield return new WaitForSeconds(2.5f);
            yield return Shot("09_standings");
            Report();
            online.BackToRoom();
            yield return new WaitForSeconds(1.5f);
            yield return Shot("10_back_in_the_room");
            Note($"racing: {manager.IsRacing}; in a session: {manager.InSession}; in a room: {Server.InRoom}");
        }

        /// <summary>Every car of this device gets a computer driver, so the race drives itself.</summary>
        private void PilotLocalCars(Race race)
        {
            foreach (KeyValuePair<byte, Car> local in online.LocalCars)
            {
                if (local.Value != null)
                {
                    local.Value.Driver = new AiDriver(Mathf.Clamp01(NumberArgument(SkillArgument, 0.85f)), 9000 + local.Key);
                }
            }
            Note($"cars at this device: {string.Join(", ", online.LocalCars.Keys)}; of the others: {string.Join(", ", online.RemoteCars.Keys)}");
        }

        /// <summary>Where every car is: this device's by their own physics, the others' by their poses.</summary>
        private void Progress()
        {
            Race race = online.OnlineRace;
            if (race == null)
            {
                Note("no race");
                return;
            }
            var parts = new List<string>();
            foreach (KeyValuePair<byte, Car> local in online.LocalCars)
            {
                Car car = local.Value;
                if (car != null && car.Racer != null)
                {
                    parts.Add($"seat {local.Key} here: P{car.Racer.Place} lap {car.Racer.Lap} {car.Racer.Progress:0} m health {(car.Health != null ? car.Health.Health01 : 0f):0.00} "
                        + $"{(car.IsWrecked ? "WRECKED" : car.HasFinished ? "finished" : "racing")} kills {car.Racer.Kills}");
                }
            }
            foreach (KeyValuePair<byte, RemoteCar> remote in online.RemoteCars)
            {
                RemoteCar car = remote.Value;
                if (car != null && car.Car != null)
                {
                    float age = car.LastPoseTime >= 0f ? Time.unscaledTime - car.LastPoseTime : -1f;
                    parts.Add($"seat {remote.Key} remote: P{car.Car.Racer.Place} {car.Car.Racer.Progress:0} m at ({car.transform.position.x:0},{car.transform.position.z:0}) "
                        + $"pose {age:0.00} s old, health {car.ReportedHealth:0.00}, state {car.Driver.State}{(car.Car.IsWrecked ? " WRECKED" : "")}"
                        + (car.gameObject.activeSelf ? "" : " (off the track)"));
                }
            }
            Note($"t {race.Time:0.0} {race.Phase} claims {online.ClaimsSeen}: {string.Join(" | ", parts)}");
        }

        /// <summary>The standings the room sent, and what the server knows of every racer.</summary>
        private void Report()
        {
            RaceResult standings = online.LastStandings;
            Note("STANDINGS " + DeathRaceOnlineController.Describe(standings));
            foreach (RoomMemberInfo member in Server.Members.OrderBy(member => member.Place))
            {
                Note($"  member seat {member.Seat} {Server.NameOf(member.Identity)}: place {member.Place}, score {member.Score}{(Server.IsLocal(member) ? " (here)" : "")}");
            }
            foreach (var racer in online.Racers.Values.OrderBy(racer => racer.Seat))
            {
                Note($"  racer seat {racer.Seat}: done {racer.Done}, finished {racer.Finished}, wrecked {racer.Wrecked}, {racer.TimeMs} ms, {racer.ProgressCm} cm, "
                    + $"best lap {racer.BestLapMs} ms, kills {racer.Kills}, killed by {racer.KilledBy}");
            }
            bool everyoneDone = online.Racers.Values.All(racer => racer.Done);
            Note(everyoneDone ? "every racer reported" : "FAIL a racer never reported");
        }

        private static string KindName(uint kind)
        {
            switch (kind)
            {
                case OnlineRaceRules.MineAction: return "mine";
                case OnlineRaceRules.RocketAction: return "rocket";
                case OnlineRaceRules.WreckAction: return "wreck";
                default: return "kind " + kind;
            }
        }
    }
}
