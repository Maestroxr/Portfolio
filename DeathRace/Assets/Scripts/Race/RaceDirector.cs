using System;
using System.Collections.Generic;
using Gamebox;
using Gamebox.Racing;
using Gamebox.UI;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// Runs races. <see cref="Begin"/> builds everything of a setup under one root object: the track and its racing line,
    /// the effects, the cars on the grid with their drivers and lap trackers, the pickups, a camera for every person at
    /// this device (the scene's camera for the first, copies for the others in split screen; the overview when nobody
    /// races, like the computer cars behind the menu) and a HUD over each view. Then the race runs on the physics clock:
    /// a moment on the grid, the countdown (the cars held until the green light), the race (places every step, laps,
    /// best laps, finishes, wrecks and kills told to the cars), and the end of <see cref="RaceRules"/>, which draws up
    /// the <see cref="RaceResult"/> and raises <see cref="Finished"/>. The race stays on screen after that (the cars
    /// cruise) until the next <see cref="Begin"/> or <see cref="Abort"/> takes it all down. Pausing is the game's time
    /// scale: the race clock is the physics clock, which stops with it.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class RaceDirector : MonoBehaviour
    {
        /// <summary>The moment on the grid before the count starts: the views settle, the HUD appears.</summary>
        public const float GridSeconds = 1.2f;

        /// <summary>The count from "3" to the green light.</summary>
        public const float CountdownSeconds = 3f;

        /// <summary>How long "GO!" stays up after the green light.</summary>
        public const float GoShownSeconds = 1f;

        /// <summary>The sorting order of the HUD canvas: under the menus (100) and the windows (400 and up).</summary>
        public const int HudSortingOrder = 10;

        [SerializeField] protected DeathRaceManager manager;

        private readonly List<RaceCamera> cameras = new List<RaceCamera>();
        private readonly List<RaceHud> huds = new List<RaceHud>();
        private readonly List<RaceRules.Runner> runners = new List<RaceRules.Runner>();
        private readonly Countdown countdown = new Countdown();
        private RaceCamera mainView;
        private float finishingSince;
        private float timeLimit;
        private int finishedCount;
        private bool goShown;

        public Race Current { get; protected set; }

        /// <summary>Whether a race (attract mode included) is on.</summary>
        public bool IsRacing => Current != null && Current.Phase != RacePhase.Over;

        /// <summary>The race clock: seconds since the green light (negative before it), 0 without a race.</summary>
        public float Time => Current != null ? Current.Time : 0f;

        /// <summary>The cameras of the race on.</summary>
        public IReadOnlyList<RaceCamera> Cameras => cameras;

        /// <summary>The HUDs of the race on (one per person at this device).</summary>
        public IReadOnlyList<RaceHud> Huds => huds;

        public DeathRaceManager Manager => manager;

        /// <summary>
        /// Chooses the driver of a car before the default does (null = the default: the controls for a person, the
        /// computer's driver for the computer, standing still for a remote player). Online play hands remote cars to the
        /// network here.
        /// </summary>
        public Func<RaceEntrant, ICarDriver> DriverFor { get; set; }

        public event Action<Race> Started;
        public event Action<RaceResult> Finished;

        /// <summary>A car of the race being built is on the grid (before <see cref="Started"/>).</summary>
        public event Action<Car> CarSpawned;

        public virtual void Begin(RaceSetup setup)
        {
            Abort();
            if (setup == null)
            {
                return;
            }
            DeathRaceTheme theme = manager != null ? manager.Look : null;
            DeathRaceContent content = manager != null ? manager.Content : null;
            DeathRaceSettings settings = manager != null ? manager.Options : null;
            TrackLayout layout = TrackLayouts.Of(setup.Track);
            if (setup.Laps <= 0)
            {
                setup.Laps = layout.Laps;
            }

            Transform root = new GameObject($"Race ({setup.Mode}, {layout.Name})").transform;
            Track track = TrackBuilder.Build(layout, theme, content, root);
            if (manager != null)
            {
                // The haze and the colour behind the world take the biome's.
                manager.ShowBiome(layout.Biome);
            }
            var race = new Race(setup, track, root)
            {
                Line = RacingLine.Build(track.Path, track.HalfWidthAt),
                Audio = manager != null ? manager.Sounds : null,
                Phase = RacePhase.Grid,
                Time = -(GridSeconds + CountdownSeconds)
            };
            race.Effects = root.gameObject.AddComponent<RaceEffects>();
            race.Effects.Setup(content, theme);
            Current = race;
            finishedCount = 0;
            finishingSince = 0f;
            goShown = false;
            countdown.Restart();

            SpawnCars(race, content, theme);
            timeLimit = race.HasHumans ? 0f : RaceRules.TimeLimit(track.Path.Length, setup.Laps);
            race.Pickups = PickupField.Spawn(race, root);
            BuildViews(race, theme, settings);
            ApplySettings(settings);
            RaiseStarted(race);
        }

        /// <summary>Tears the race down without a result (leaving to the menu, or before the next race).</summary>
        public virtual void Abort()
        {
            Race race = Current;
            Current = null;
            foreach (RaceHud hud in huds)
            {
                if (hud != null)
                {
                    hud.Remove();
                }
            }
            huds.Clear();
            cameras.Clear();
            if (mainView != null)
            {
                mainView.Release();
            }
            if (race == null)
            {
                return;
            }
            race.Phase = RacePhase.Over;
            race.Cameras.Clear();
            if (race.Root != null)
            {
                // Off at once (its colliders must not meet the next race's in this frame), gone at the end of the frame.
                race.Root.gameObject.SetActive(false);
                Destroy(race.Root.gameObject);
            }
        }

        /// <summary>The look changed during a race: the cars, the track, the effects and the HUDs take it.</summary>
        public virtual void Restyle(DeathRaceTheme theme)
        {
            Race race = Current;
            if (race == null)
            {
                return;
            }
            foreach (Car car in race.Cars)
            {
                if (car != null && car.View != null)
                {
                    car.View.ApplyTheme(theme, car.Color);
                    if (car.IsWrecked)
                    {
                        car.View.SetWrecked(true);
                    }
                }
            }
            if (race.Track != null)
            {
                race.Track.Restyle(theme);
            }
            if (race.Effects != null)
            {
                race.Effects.Setup(manager != null ? manager.Content : null, theme);
            }
            foreach (RaceHud hud in huds)
            {
                if (hud != null)
                {
                    hud.Restyle(theme);
                }
            }
        }

        /// <summary>The language changed: the HUDs write their words again.</summary>
        public virtual void RefreshTexts()
        {
            DeathRaceTheme theme = manager != null ? manager.Look : null;
            foreach (RaceHud hud in huds)
            {
                if (hud != null)
                {
                    hud.Restyle(theme);
                }
            }
        }

        /// <summary>The settings changed (camera distance, screen shake, minimap).</summary>
        public virtual void ApplySettings(DeathRaceSettings settings)
        {
            if (settings == null)
            {
                return;
            }
            CameraShake.Enabled = settings.screenShake;
            foreach (RaceCamera view in cameras)
            {
                if (view != null)
                {
                    view.SetDistance(settings.cameraDistance);
                }
            }
            foreach (RaceHud hud in huds)
            {
                if (hud != null)
                {
                    hud.ApplySettings(settings);
                }
            }
        }

        protected void RaiseStarted(Race race)
        {
            Started?.Invoke(race);
        }

        protected void RaiseFinished(RaceResult result)
        {
            Finished?.Invoke(result);
        }

        // ------------------------------------------------------------------------------------------------
        // Building
        // ------------------------------------------------------------------------------------------------

        private void SpawnCars(Race race, DeathRaceContent content, DeathRaceTheme theme)
        {
            RaceSetup setup = race.Setup;
            Track track = race.Track;
            List<RaceEntrant> grid = RaceRules.GridOrder(setup);
            float volume = setup.Mode == RaceMode.Attract ? 0.3f : 1f;
            for (int slot = 0; slot < grid.Count; slot++)
            {
                RaceEntrant entrant = grid[slot];
                Pose pose = track.GridSlot(slot);
                ICarDriver driver = DriverFor?.Invoke(entrant) ?? DefaultDriver(entrant, setup);
                Car car = CarFactory.Spawn(entrant, pose, content, theme, race, driver, race.Root, volume);
                car.GameManager = manager;
                var tracker = new LapTracker(track.Path, setup.Laps, track.LineDistance, RacePath.ToPlane(pose.position));
                var racer = new Racer(entrant.Seat, tracker) { Place = slot + 1 };
                car.AttachRacer(racer);
                tracker.LapCompleted += lap => OnLap(car, lap);
                tracker.Completed += () => OnFinished(car);
                car.Wrecked += OnWrecked;
                race.Cars.Add(car);
                CarSpawned?.Invoke(car);
            }
        }

        /// <summary>The driver a car gets unless <see cref="DriverFor"/> chose one.</summary>
        public static ICarDriver DefaultDriver(RaceEntrant entrant, RaceSetup setup)
        {
            switch (entrant.Driver)
            {
                case DriverKind.Human:
                    return entrant.Controls is PlayerControls controls ? new HumanDriver(controls) : new HumanDriver();
                case DriverKind.Computer:
                    return new AiDriver(entrant.Skill, unchecked(setup.Seed * 31 + entrant.Seat * 7919 + 17));
                default:
                    return new ScriptedDriver();
            }
        }

        /// <summary>A view and a HUD for every person at this device; the overview when there is none.</summary>
        private void BuildViews(Race race, DeathRaceTheme theme, DeathRaceSettings settings)
        {
            Camera main = manager != null && manager.MainCamera != null ? manager.MainCamera : Camera.main;
            if (main == null)
            {
                Debug.LogWarning("Death Race: no camera to show the race with.");
                return;
            }
            mainView = main.GetComponent<RaceCamera>();
            if (mainView == null)
            {
                mainView = main.gameObject.AddComponent<RaceCamera>();
            }

            var people = new List<Car>();
            foreach (Car car in race.Cars)
            {
                if (car.IsHuman)
                {
                    people.Add(car);
                }
            }
            people.Sort((a, b) => ViewOrder(a).CompareTo(ViewOrder(b)));
            if (people.Count == 0)
            {
                mainView.Overview(race, new Rect(0f, 0f, 1f, 1f), false);
                AddView(race, mainView);
                return;
            }

            RectTransform hudRoot = OverlayKit.Canvas(race.Root, "HUD", HudSortingOrder).GetComponent<RectTransform>();
            for (int i = 0; i < people.Count; i++)
            {
                Rect viewport = SplitScreen.Viewport(i, people.Count);
                RaceCamera view = i == 0 ? mainView : CopyView(main, race.Root, $"Camera {i + 1}");
                view.Follow(people[i], race, viewport);
                AddView(race, view);
                huds.Add(RaceHud.Create(hudRoot, viewport, people[i], race, theme, settings));
            }
            Rect? spare = SplitScreen.Spare(people.Count);
            if (spare.HasValue)
            {
                RaceCamera map = CopyView(main, race.Root, "Camera Map");
                map.Overview(race, spare.Value, true);
                AddView(race, map);
            }
        }

        private void AddView(Race race, RaceCamera view)
        {
            cameras.Add(view);
            race.Cameras.Add(view);
        }

        private static int ViewOrder(Car car)
        {
            return car.Entrant.Viewport >= 0 ? car.Entrant.Viewport : 100 + car.Entrant.Seat;
        }

        /// <summary>A copy of the scene's camera for another person's view (only the scene's camera listens).</summary>
        private static RaceCamera CopyView(Camera main, Transform parent, string viewName)
        {
            GameObject copy = Instantiate(main.gameObject, parent);
            copy.name = viewName;
            copy.tag = "Untagged";
            AudioListener listener = copy.GetComponent<AudioListener>();
            if (listener != null)
            {
                Destroy(listener);
            }
            RaceCamera view = copy.GetComponent<RaceCamera>();
            return view != null ? view : copy.AddComponent<RaceCamera>();
        }

        // ------------------------------------------------------------------------------------------------
        // Running
        // ------------------------------------------------------------------------------------------------

        private void FixedUpdate()
        {
            Race race = Current;
            if (race == null)
            {
                return;
            }
            race.Time += UnityEngine.Time.fixedDeltaTime;
            switch (race.Phase)
            {
                case RacePhase.Grid:
                    if (race.Time >= -CountdownSeconds)
                    {
                        race.Phase = RacePhase.Countdown;
                    }
                    break;
                case RacePhase.Countdown:
                    if (race.Time >= 0f)
                    {
                        race.Phase = RacePhase.Racing;
                    }
                    break;
                case RacePhase.Racing:
                case RacePhase.Finishing:
                    UpdatePlaces(race);
                    CheckEnd(race);
                    break;
            }
        }

        private void Update()
        {
            Race race = Current;
            if (race == null)
            {
                return;
            }
            if (race.Phase == RacePhase.Countdown || (race.Phase == RacePhase.Racing && !goShown))
            {
                if (countdown.Advance(race.Time + CountdownSeconds, CountdownSeconds, out string label))
                {
                    race.CountdownLabel = label;
                    PlayUi(countdown.IsDone ? Sounds?.go : Sounds?.countdown, race);
                }
            }
            if (!goShown && race.Time >= GoShownSeconds && countdown.IsDone)
            {
                goShown = true;
                race.CountdownLabel = null;
            }
        }

        private RaceSounds Sounds => manager != null && manager.Content != null ? manager.Content.sounds : null;

        /// <summary>A sound of the race's interface, for the people racing (the cars behind the menu are quiet).</summary>
        private void PlayUi(AudioClip clip, Race race)
        {
            if (clip != null && race.Audio != null && race.Setup.Mode != RaceMode.Attract)
            {
                race.Audio.PlayUi(clip);
            }
        }

        private static void UpdatePlaces(Race race)
        {
            List<Car> order = race.Standings();
            for (int i = 0; i < order.Count; i++)
            {
                if (order[i].Racer != null)
                {
                    order[i].Racer.Place = i + 1;
                }
            }
        }

        private void CheckEnd(Race race)
        {
            runners.Clear();
            foreach (Car car in race.Cars)
            {
                runners.Add(new RaceRules.Runner { Human = car.IsHuman, Finished = car.HasFinished, Out = car.IsWrecked });
            }
            RacePhase next = RaceRules.Next(race.Phase, runners, race.Time, ref finishingSince, timeLimit);
            if (next == RacePhase.Over)
            {
                End(race);
            }
            else
            {
                race.Phase = next;
            }
        }

        /// <summary>The race is over: the result is drawn up and handed on; the cars still running cruise on.</summary>
        private void End(Race race)
        {
            race.Phase = RacePhase.Over;
            UpdatePlaces(race);
            var standings = new List<RaceRules.Standing>(race.Cars.Count);
            foreach (Car car in race.Cars)
            {
                Racer racer = car.Racer;
                standings.Add(new RaceRules.Standing
                {
                    Entrant = car.Entrant,
                    Finished = car.HasFinished,
                    Out = car.IsWrecked,
                    FinishTime = racer != null ? racer.FinishTime : 0f,
                    OutTime = racer != null ? racer.OutTime : -1f,
                    Progress = racer != null ? racer.Progress : 0f,
                    BestLap = racer != null ? racer.BestLap : 0f,
                    LapsCompleted = racer != null ? racer.LapsCompleted : 0,
                    Kills = racer != null ? racer.Kills : 0,
                    CashPicked = racer != null ? racer.CashPicked : 0,
                    HealthLeft = car.Health != null && !car.IsWrecked ? car.Health.Health01 : 0f,
                    MinesUsed = car.Weapons != null ? car.Weapons.MinesUsed : 0,
                    RocketsUsed = car.Weapons != null ? car.Weapons.RocketsUsed : 0,
                    NitroUsed = car.Weapons != null ? car.Weapons.NitroUsed : 0
                });
                if (!car.IsWrecked && !car.HasFinished)
                {
                    StartCruise(car);
                }
            }
            RaceResult result = RaceRules.Result(race.Setup, standings, race.Time);
            Finished?.Invoke(result);
        }

        /// <summary>A car that is done racing is driven on gently by the computer, its weapons quiet.</summary>
        private static void StartCruise(Car car)
        {
            if (car.Driver is AiDriver computer)
            {
                computer.Cruise = true;
            }
            else if (car.Driver is HumanDriver || car.Driver == null)
            {
                car.Driver = new AiDriver(0.7f, car.Entrant.Seat + 101) { Cruise = true };
            }
        }

        // ------------------------------------------------------------------------------------------------
        // What happens to the cars
        // ------------------------------------------------------------------------------------------------

        private void OnLap(Car car, int lap)
        {
            Race race = Current;
            if (race == null || car.Racer == null || car.Racer.Tracker == null)
            {
                return;
            }
            IReadOnlyList<float> times = car.Racer.Tracker.LapTimes;
            float time = lap - 1 < times.Count ? times[lap - 1] : 0f;
            if (lap < race.Laps)
            {
                car.Raise(new CarEvent(CarEventKind.Lap, lap));
                if (car.IsHuman)
                {
                    PlayUi(Sounds?.lap, race);
                }
            }
            if (time > 0f && (race.BestLap <= 0f || time < race.BestLap - 1e-4f))
            {
                race.BestLap = time;
                race.BestLapCar = car;
                car.Raise(new CarEvent(CarEventKind.BestLap, lap) { Value = time });
            }
        }

        private void OnFinished(Car car)
        {
            Race race = Current;
            if (race == null)
            {
                return;
            }
            finishedCount++;
            if (car.Racer != null)
            {
                car.Racer.Place = finishedCount;
            }
            car.Raise(new CarEvent(CarEventKind.Finished, finishedCount));
            if (car.IsHuman)
            {
                PlayUi(Sounds?.finish, race);
            }
            StartCruise(car);
        }

        private void OnWrecked(Car car, DamageInfo cause)
        {
            Race race = Current;
            if (race == null || !race.Cars.Contains(car))
            {
                return;
            }
            if (car.Racer != null)
            {
                car.Racer.OutTime = Mathf.Max(0f, race.Time);
            }
            Car killer = cause.Attacker != null && cause.Attacker != car ? cause.Attacker : null;
            if (killer != null && killer.Racer != null && race.Cars.Contains(killer))
            {
                killer.Racer.Kills++;
                killer.Raise(new CarEvent(CarEventKind.Kill, killer.Racer.Kills, car));
            }
            car.Raise(new CarEvent(CarEventKind.Wrecked, 0, killer));
            if (car.IsHuman)
            {
                PlayUi(Sounds?.wreckJingle, race);
            }
        }

        private void OnDestroy()
        {
            Abort();
        }
    }
}
