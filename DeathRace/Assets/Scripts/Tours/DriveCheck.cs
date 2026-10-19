using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Gamebox;
using Gamebox.UI;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// Development players only: the driving check. Started with <c>-deathrace-drivecheck &lt;folder&gt;</c>, it runs quick
    /// races that drive themselves (the person's seat is handed to a computer driver) and writes what happens to
    /// <c>&lt;folder&gt;/drive.log</c>: every lap time, finish, kill and wreck, every time a driver got stuck, the standings
    /// every ten seconds, how hard each car fought (rounds, mines and rockets used, damage dealt) and the result; it
    /// saves a screenshot every few seconds, and quits when the races are done (or time runs out).
    ///
    /// Options: <c>-deathrace-drivecheck-track &lt;i&gt;[,&lt;j&gt;...]|all</c> (default 0), <c>-deathrace-drivecheck-car
    /// &lt;model&gt;</c> (the person's car), <c>-deathrace-drivecheck-skill &lt;0..1&gt;</c> (of the driver in the person's seat,
    /// default 0.8), <c>-deathrace-drivecheck-rivals &lt;0..1&gt;</c> (the computer cars' skill), <c>-deathrace-drivecheck-rivalcar
    /// &lt;model&gt;</c> (the computer cars' car), <c>-deathrace-drivecheck-laps
    /// &lt;n&gt;</c>, <c>-deathrace-drivecheck-speed &lt;x&gt;</c> (time scale, at most 3), <c>-deathrace-drivecheck-shots
    /// &lt;seconds&gt;</c> (between screenshots, default 6, 0 = none), <c>-deathrace-drivecheck-timeout &lt;seconds&gt;</c> (per race,
    /// default 480), <c>-deathrace-drivecheck-attract &lt;seconds&gt;</c> (watch the cars behind the menu first),
    /// <c>-deathrace-drivecheck-split &lt;2..4&gt;</c> (that many people in split screen, all driven by the computer).
    /// </summary>
    public sealed class DriveCheck : MonoBehaviour
    {
        public const string Argument = "-deathrace-drivecheck";
        public const string TrackArgument = "-deathrace-drivecheck-track";
        public const string CarArgument = "-deathrace-drivecheck-car";
        public const string SkillArgument = "-deathrace-drivecheck-skill";
        public const string RivalsArgument = "-deathrace-drivecheck-rivals";
        public const string RivalCarArgument = "-deathrace-drivecheck-rivalcar";
        public const string LapsArgument = "-deathrace-drivecheck-laps";
        public const string SpeedArgument = "-deathrace-drivecheck-speed";
        public const string ShotsArgument = "-deathrace-drivecheck-shots";
        public const string TimeoutArgument = "-deathrace-drivecheck-timeout";
        public const string AttractArgument = "-deathrace-drivecheck-attract";
        public const string SplitArgument = "-deathrace-drivecheck-split";

        private const float StandingsEvery = 10f;

        private readonly Dictionary<Car, CarLog> cars = new Dictionary<Car, CarLog>();
        private string folder;
        private StreamWriter log;
        private DeathRaceManager manager;
        private RaceResult result;
        private int shots;
        private int errors;
        private int stuck;
        private float speed = 1f;
        private float pilotSkill = 0.8f;
        private float rivalSkill = -1f;

        /// <summary>What the check counts of one car during a race.</summary>
        private sealed class CarLog
        {
            public float TopSpeed;
            public float SpeedSum;
            public int Samples;
            public float OffRoad;
            public int WallHits;
            public int CarHits;
            public float DamageDealt;
            public float DamageTaken;
            public int Stuck;
            public float Firing;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Launch()
        {
            if (Application.isEditor || !Debug.isDebugBuild)
            {
                return;
            }
            string folder = MobilePlatform.ArgumentValue(Argument);
            if (string.IsNullOrEmpty(folder) || FindAnyObjectByType<DriveCheck>() != null)
            {
                return;
            }
            var holder = new GameObject("DriveCheck");
            DontDestroyOnLoad(holder);
            holder.AddComponent<DriveCheck>().folder = folder;
        }

        private IEnumerator Start()
        {
            Directory.CreateDirectory(folder);
            log = new StreamWriter(Path.Combine(folder, "drive.log"), false) { AutoFlush = true };
            Application.logMessageReceived += OnLog;
            Application.runInBackground = true;
            speed = Mathf.Clamp(Number(SpeedArgument, 1f), 0.25f, 3f);
            pilotSkill = Mathf.Clamp01(Number(SkillArgument, 0.8f));
            rivalSkill = Number(RivalsArgument, -1f);
            float shotEvery = Number(ShotsArgument, 6f);
            float timeout = Mathf.Max(30f, Number(TimeoutArgument, 480f));
            float attract = Number(AttractArgument, 0f);
            int split = Mathf.Clamp((int)Number(SplitArgument, 1f), 1, 4);
            Note($"drivecheck: speed {speed}, pilot skill {pilotSkill}, rivals {(rivalSkill < 0f ? "as set up" : rivalSkill.ToString("0.00"))}, screen {Screen.width}x{Screen.height}, split {split}");

            float waited = 0f;
            while (manager == null && waited < 30f)
            {
                manager = FindAnyObjectByType<DeathRaceManager>();
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            if (manager == null || manager.Director == null)
            {
                Note("FAIL no Death Race manager or director");
                Quit();
                yield break;
            }
            yield return new WaitForSecondsRealtime(1.5f);

            if (attract > 0f)
            {
                yield return WatchAttract(attract, shotEvery > 0f ? Mathf.Min(shotEvery, 4f) : 0f);
            }
            foreach (int track in Tracks(MobilePlatform.ArgumentValue(TrackArgument)))
            {
                yield return Race(track, split, shotEvery, timeout);
            }
            Time.timeScale = 1f;
            Note($"drivecheck done: {shots} screenshots, {stuck} stuck events, {errors} errors");
            Quit();
        }

        /// <summary>The cars behind the menu, for a while.</summary>
        private IEnumerator WatchAttract(float seconds, float shotEvery)
        {
            Note($"attract: watching {seconds:0} s");
            float start = Time.unscaledTime;
            float nextShot = start + 2f;
            while (Time.unscaledTime - start < seconds)
            {
                Race race = manager.CurrentRace;
                if (shotEvery > 0f && Time.unscaledTime >= nextShot)
                {
                    nextShot += shotEvery;
                    Shot($"attract_{(race != null ? race.Setup.Track : -1)}");
                }
                yield return null;
            }
            Race current = manager.CurrentRace;
            Note($"attract: {(current != null ? $"{current.Setup.Mode} on {current.Track.Layout.Name}, {current.Phase}, t {current.Time:0.0}" : "no race")}");
        }

        /// <summary>One quick race on <paramref name="track"/>, driven by the computer in every seat.</summary>
        private IEnumerator Race(int track, int split, float shotEvery, float timeout)
        {
            RaceSetup setup = manager.QuickRaceSetup(track);
            string car = MobilePlatform.ArgumentValue(CarArgument);
            if (!string.IsNullOrEmpty(car))
            {
                CarModel model = ParseCar(car);
                setup.Entrants[0].Loadout = new CarLoadout { model = model, mines = 3, rockets = 2, nitro = 3 };
            }
            int laps = (int)Number(LapsArgument, 0f);
            if (laps > 0)
            {
                setup.Laps = laps;
            }
            string rivalCar = MobilePlatform.ArgumentValue(RivalCarArgument);
            for (int i = 1; i < setup.Entrants.Count; i++)
            {
                RaceEntrant entrant = setup.Entrants[i];
                if (rivalSkill >= 0f)
                {
                    entrant.Skill = Mathf.Clamp01(rivalSkill);
                }
                if (!string.IsNullOrEmpty(rivalCar))
                {
                    entrant.Loadout = new CarLoadout { model = ParseCar(rivalCar), mines = 2, rockets = 1, nitro = 2 };
                }
                if (i < split)
                {
                    entrant.Driver = DriverKind.Human;
                    entrant.Viewport = i;
                }
            }
            if (split > 1)
            {
                setup.Mode = RaceMode.Local;
                setup.Entrants[0].Viewport = 0;
            }

            cars.Clear();
            result = null;
            manager.Director.Started += OnStarted;
            manager.Director.Finished += OnFinished;
            manager.BeginRace(setup);
            Time.timeScale = speed;
            Race race = manager.CurrentRace;
            if (race == null)
            {
                Note($"FAIL race on track {track} did not start");
                manager.Director.Started -= OnStarted;
                manager.Director.Finished -= OnFinished;
                yield break;
            }
            Note($"race: track {track} {race.Track.Layout.Name} ({race.Track.Path.Length:0} m, {race.Laps} laps, line {race.Line.Length:0} m), {race.Setup.Mode}, {race.Setup.Difficulty}");
            foreach (Car each in race.Cars)
            {
                Note($"  seat {each.Entrant.Seat} {each.Entrant.Name}: {each.Entrant.Driver} {each.Loadout.model} skill {each.Entrant.Skill:0.00}, grid P{each.Racer.Place}");
            }

            float start = Time.unscaledTime;
            float nextShot = start + 1f;
            float nextStandings = start + StandingsEvery;
            int frames = 0;
            while (result == null && Time.unscaledTime - start < timeout)
            {
                frames++;
                Sample(race);
                if (shotEvery > 0f && Time.unscaledTime >= nextShot)
                {
                    nextShot += shotEvery / Mathf.Max(1f, speed);
                    Shot($"race{track}_{race.Phase}_{Mathf.Max(0f, race.Time):000}");
                }
                if (Time.unscaledTime >= nextStandings)
                {
                    nextStandings += StandingsEvery;
                    Note($"t {race.Time:0.0} {race.Phase} {Standings(race)}");
                }
                if (Time.timeScale > 0f && Mathf.Abs(Time.timeScale - speed) > 0.01f && race.Phase != RacePhase.Over)
                {
                    Time.timeScale = speed;
                }
                yield return null;
            }
            float seconds = Time.unscaledTime - start;
            Note($"frames: {frames} in {seconds:0.0} s real ({frames / Mathf.Max(0.1f, seconds):0} fps), race clock {race.Time:0.0} s");
            if (result == null)
            {
                Note($"TIMEOUT race on track {track} after {timeout:0} s at {race.Phase}: {Standings(race)}");
            }
            else
            {
                foreach (RacerResult racer in result.Racers)
                {
                    Note($"  P{racer.Place} seat {racer.Seat} {racer.Name}: {(racer.Finished ? "finished" : racer.Wrecked ? "WRECKED" : "running")} " +
                         $"time {racer.Time:0.00} best {racer.BestLap:0.00} laps {racer.LapsCompleted} kills {racer.Kills} cash {racer.CashPicked} " +
                         $"health {racer.HealthLeft:0.00} mines {racer.MinesUsed} rockets {racer.RocketsUsed} nitro {racer.NitroUsed}");
                }
            }
            foreach (KeyValuePair<Car, CarLog> pair in cars)
            {
                Car each = pair.Key;
                CarLog counts = pair.Value;
                if (each == null)
                {
                    continue;
                }
                string driver = each.Driver is AiDriver computer ? $"stuck {computer.StuckCount} put back {computer.Respawns}" : "";
                Note($"  seat {each.Entrant.Seat}: top {counts.TopSpeed:0.0} m/s, average {counts.SpeedSum / Mathf.Max(1, counts.Samples):0.0}, off road {counts.OffRoad:0.0} s, " +
                     $"walls {counts.WallHits}, cars {counts.CarHits}, firing {counts.Firing:0.0} s, dealt {counts.DamageDealt:0}, taken {counts.DamageTaken:0}, {driver}");
            }
            yield return new WaitForSecondsRealtime(2.5f);
            Shot($"race{track}_result");
            manager.Director.Started -= OnStarted;
            manager.Director.Finished -= OnFinished;
            Time.timeScale = 1f;
            manager.LeaveRace();
            // The results window would stay over the next race: the check closes it as the player would.
            foreach (OverlayWindow window in FindObjectsByType<OverlayWindow>(FindObjectsInactive.Exclude))
            {
                window.Hide();
            }
            yield return new WaitForSecondsRealtime(1.5f);
        }

        /// <summary>The race was built: the people's seats are handed to computer drivers, and every car is watched.</summary>
        private void OnStarted(Race race)
        {
            foreach (Car car in race.Cars)
            {
                var counts = new CarLog();
                cars[car] = counts;
                if (car.IsHuman)
                {
                    car.Driver = new AiDriver(pilotSkill, 4242 + car.Entrant.Seat);
                }
                if (car.Driver is AiDriver computer)
                {
                    computer.Stuck += (stuckCar, what) =>
                    {
                        stuck++;
                        counts.Stuck++;
                        Note($"STUCK t {race.Time:0.0} seat {stuckCar.Entrant.Seat} {what} at {stuckCar.TrackDistance:0} m (offset {stuckCar.TrackOffset:0.0}, speed {stuckCar.Speed:0.0})");
                    };
                }
                Car watched = car;
                car.Happened += happened => OnHappened(race, watched, happened);
                car.Wrecked += (wreck, cause) => Note($"WRECK t {race.Time:0.0} seat {wreck.Entrant.Seat} by {(cause.Attacker != null ? cause.Attacker.Entrant.Seat.ToString() : "-")} ({cause.Source})");
                if (car.Body != null)
                {
                    car.Body.Collided += (collision, impact) =>
                    {
                        if (impact < 3f)
                        {
                            return;
                        }
                        if (collision.collider.gameObject.layer == DeathRaceLayers.Wall)
                        {
                            counts.WallHits++;
                        }
                        else if (collision.collider.GetComponentInParent<Car>() != null)
                        {
                            counts.CarHits++;
                        }
                    };
                }
                if (car.Health != null)
                {
                    car.Health.Damaged += info =>
                    {
                        counts.DamageTaken += info.Amount;
                        if (info.Attacker != null && cars.TryGetValue(info.Attacker, out CarLog attacker))
                        {
                            attacker.DamageDealt += info.Amount;
                        }
                    };
                }
            }
        }

        private void OnHappened(Race race, Car car, CarEvent happened)
        {
            switch (happened.Kind)
            {
                case CarEventKind.Lap:
                    IReadOnlyList<float> times = car.Racer.Tracker.LapTimes;
                    Note($"LAP t {race.Time:0.0} seat {car.Entrant.Seat} lap {happened.Amount} in {(times.Count > 0 ? times[times.Count - 1] : 0f):0.00} s, P{car.Racer.Place}");
                    break;
                case CarEventKind.BestLap:
                    Note($"BEST t {race.Time:0.0} seat {car.Entrant.Seat} {happened.Value:0.00} s");
                    break;
                case CarEventKind.Finished:
                    Note($"FINISH t {race.Time:0.0} seat {car.Entrant.Seat} P{happened.Amount} time {car.Racer.FinishTime:0.00}");
                    break;
                case CarEventKind.Kill:
                    Note($"KILL t {race.Time:0.0} seat {car.Entrant.Seat} wrecked seat {(happened.Other != null ? happened.Other.Entrant.Seat : -1)}");
                    break;
            }
        }

        private void OnFinished(RaceResult finished)
        {
            result = finished;
            Note($"RESULT {finished.Setup.Mode} track {finished.Setup.Track}: {finished.Racers.Count} cars");
        }

        /// <summary>The speeds and the time off the road of every car.</summary>
        private void Sample(Race race)
        {
            if (!race.IsRunning)
            {
                return;
            }
            float deltaTime = Time.deltaTime;
            foreach (Car car in race.Cars)
            {
                if (car == null || car.IsWrecked || car.HasFinished || !cars.TryGetValue(car, out CarLog counts))
                {
                    continue;
                }
                float speedNow = car.Body != null ? car.Body.Speed : 0f;
                counts.TopSpeed = Mathf.Max(counts.TopSpeed, speedNow);
                counts.SpeedSum += speedNow;
                counts.Samples++;
                if (car.Body != null && car.Body.OffRoad)
                {
                    counts.OffRoad += deltaTime;
                }
                if (car.Weapons != null && car.Weapons.Firing)
                {
                    counts.Firing += deltaTime;
                }
            }
        }

        private static string Standings(Race race)
        {
            var text = new StringBuilder();
            foreach (Car car in race.Standings())
            {
                string state = car.Driver is AiDriver computer ? computer.State : "?";
                text.Append($"| P{car.Racer.Place} s{car.Entrant.Seat} lap {car.Racer.Lap} {car.Racer.Progress:0}m {car.Speed:0}m/s hp {(car.Health != null ? car.Health.Health01 : 1f):0.00} {state}{(car.IsWrecked ? " WRECKED" : "")} ");
            }
            return text.ToString();
        }

        private static IEnumerable<int> Tracks(string plan)
        {
            if (string.IsNullOrEmpty(plan))
            {
                yield return 0;
                yield break;
            }
            if (plan.Trim().Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                for (int i = 0; i < TrackLayouts.Count; i++)
                {
                    yield return i;
                }
                yield break;
            }
            foreach (string part in plan.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (int.TryParse(part.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int index))
                {
                    yield return Mathf.Clamp(index, 0, TrackLayouts.Count - 1);
                }
            }
        }

        private static CarModel ParseCar(string text)
        {
            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int index))
            {
                return (CarModel)Mathf.Clamp(index, 0, CarSpecs.Count - 1);
            }
            return Enum.TryParse(text, true, out CarModel model) ? model : CarModel.Viper;
        }

        private void Shot(string label)
        {
            shots++;
            string file = Path.Combine(folder, $"{shots:000}_{label}.png");
            ScreenCapture.CaptureScreenshot(file);
        }

        private static float Number(string name, float fallback)
        {
            string value = MobilePlatform.ArgumentValue(name);
            return !string.IsNullOrEmpty(value) && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float number) ? number : fallback;
        }

        private void OnLog(string message, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                errors++;
                Note($"ERROR {type}: {message}\n{stackTrace}");
            }
            else if (type == LogType.Warning)
            {
                Note($"warning: {message}");
            }
        }

        private void Note(string message)
        {
            log?.WriteLine($"{Time.realtimeSinceStartup:0.0} {message}");
        }

        private void Quit()
        {
            Application.logMessageReceived -= OnLog;
            log?.Dispose();
            log = null;
            Application.Quit();
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= OnLog;
            log?.Dispose();
            log = null;
        }
    }
}
