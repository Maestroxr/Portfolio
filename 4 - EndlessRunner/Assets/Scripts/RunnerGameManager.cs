using System.Collections.Generic;
using Gamebox;
using Gamebox.UI;
using UnityEngine;

namespace Portfolio.EndlessRunner
{
    /// <summary>What the level select shows about a level.</summary>
    public struct LevelSummary
    {
        public int Index;
        public string Title;
        public string Description;
        public string World;
        public bool Endless;
        public bool Unlocked;
        public int Stars;
        public float Length;
        public int CoinGoal;
        public int BestScore;
        public float BestDistance;
        public Color Accent;
    }


    /// <summary>What the results screen shows about a finished run.</summary>
    public struct RunResult
    {
        public string LevelTitle;
        public bool Victory;
        public bool Endless;
        public int Stars;
        public bool CoinGoalReached;
        public bool Flawless;
        public int Coins;
        public int CoinGoal;
        public float Distance;
        public int Score;
        public bool NewBest;
        public float BestDistance;
        public bool HasNextLevel;

        /// <summary>The run was a race against the runners of a room: the results are the standings.</summary>
        public bool Online;
        /// <summary>The place of the local runner in the race, from 1.</summary>
        public int Place;
        public int Runners;
        /// <summary>The runners of the race by place, a line each.</summary>
        public string Standings;

        /// <summary>The run was a race of several runners at this device: the results are the standings.</summary>
        public bool Local;
        /// <summary>The name of the winner of a local race in the colour of the seat, or null when the first place is shared.</summary>
        public string Winner;
    }


    /// <summary>
    /// Endless Runner game module. Runs the level select, the countdown, the run itself (track, pickups, power-ups,
    /// hearts), the finish and the results, and keeps the campaign progress. The menu, pause and state flow come from
    /// <see cref="BaseGameManager"/>. What belongs to the runner (hearts, coins, power-ups, distance) is its
    /// <see cref="RunnerRun"/>. Races against the runners of an online room are in RunnerGameManager.Online.cs, races of
    /// several runners at this device in split screen in RunnerGameManager.Local.cs.
    /// </summary>
    public partial class RunnerGameManager : BaseGameManager
    {
        #region Field Members
        [SerializeField] private RunnerSettings runnerSettings;
        [SerializeField] private RunnerController controller;
        [SerializeField] private RunnerUI ui;
        [SerializeField] private Campaign campaign;
        [SerializeField] internal RunnerPlayer runner;
        [SerializeField] internal TrackGenerator track;
        [SerializeField] internal ThemeController themes;
        [SerializeField] internal RunnerCamera runnerCamera;
        [SerializeField] internal RunnerEffects effects;
        [SerializeField] internal RunnerAudio sounds;

        [Header("Run")]
        [SerializeField] internal float countdownTime = 2.4f;
        [SerializeField] internal float invulnerableTime = 1.6f;
        [SerializeField] internal float crashSlowdown = 0.55f;
        [SerializeField] internal int pointsPerCoin = 10;
        [SerializeField] internal float magnetRadius = 7.5f;
        [SerializeField] internal float magnetPull = 28f;
        [Tooltip("Seconds each power-up lasts, in PowerUpType order: magnet, shield, double coins, super jump.")]
        [SerializeField] internal float[] powerUpDurations = { 10f, 15f, 12f, 10f };
        [SerializeField] internal Color[] powerUpColors =
        {
            new Color(1f, 0.3f, 0.3f), new Color(0.3f, 0.75f, 1f), new Color(0.75f, 0.4f, 1f), new Color(0.4f, 1f, 0.45f)
        };

        private RunnerSettings customSettings;
        private RunnerSettings activeSettings;
        private CampaignProgress progress;
        private readonly Dictionary<int, LevelData> levelData = new Dictionary<int, LevelData>();
        // The phase of the game; a local race keeps the phase of each runner in its run.
        private RunPhase phase = RunPhase.Menu;
        private float phaseTime;
        private readonly Countdown countdown = new Countdown();
        // The run of the runner of the scene: the game alone, an online race, the first seat of a local race.
        private RunnerRun main;
        private int coinGoal;
        private bool trackReady;
        private int trackLevel = -1;
        private bool menuVisited;
        #endregion

        public override IGameController Controller => controller;
        public override IGameUI UI => ui;
        public override ICampaign Campaign => campaign;
        public override IGameSettings DefaultSettings => runnerSettings;
        public override IGameSettings CustomSettings => customSettings != null ? customSettings : (customSettings = CreateCustomSettings());

        public override IGameSettings Settings
        {
            get => activeSettings != null ? activeSettings : runnerSettings;
            set => activeSettings = value as RunnerSettings;
        }

        protected override bool UsesTimer => false;

        /// <summary>
        /// The settings of the run: the player's choice (the level's own, or the custom ones), and always the level's own in
        /// a session, where everybody has to run the same track at the same pace.
        /// </summary>
        public RunnerSettings RunnerSettings => InSession ? runnerSettings : Settings as RunnerSettings ?? runnerSettings;

        public RunnerPlayer Player => runner;

        public RunnerLevel RunnerLevel => Level as RunnerLevel;

        public int LevelCount => campaign != null ? campaign.Count : 0;

        public int Coins => Main.Coins;

        public float Distance => Main.Distance;

        public bool IsPowerUpActive(PowerUpType type)
        {
            return Main.IsPowerUpActive(type);
        }

        /// <summary>The run of the runner of the scene.</summary>
        internal RunnerRun Main => main ?? (main = new RunnerRun(runner, runnerCamera));

        private static Vector3 StartPosition => new Vector3(0f, 0.05f, 0f);


        private RunnerSettings CreateCustomSettings()
        {
            RunnerSettings copy = runnerSettings != null ? Instantiate(runnerSettings) : ScriptableObject.CreateInstance<RunnerSettings>();
            copy.name = $"{(runnerSettings != null ? runnerSettings.name : "RunnerSettings")} (custom)";
            return copy;
        }


        protected override void Awake()
        {
            base.Awake();
            RegisterPlayer(runner);
        }


        protected override void Start()
        {
            progress = new CampaignProgress(Disk, Type);
            if (track != null)
            {
                track.HintReached += OnHintReached;
            }
            ApplyLook(ThemeAs<RunnerGameTheme>());
            base.Start();
        }


        /// <summary>
        /// The game got another theme. The interface and the effects change at once (the runner and the canvas follow
        /// the theme by themselves); the track behind the level select is laid out again with the new pieces, scenery,
        /// worlds and sky, and a run that is on keeps its track and world until the next one.
        /// </summary>
        protected override void OnThemeChanged(GameTheme theme)
        {
            base.OnThemeChanged(theme);
            var look = theme as RunnerGameTheme;
            if (look == null)
            {
                return;
            }
            ApplyLook(look);
            if (InLocalRace)
            {
                foreach (RunnerRun run in localRace.Runs)
                {
                    run.Hud?.ApplyTheme(look);
                }
            }
            if (phase == RunPhase.Menu && State != null && State.Is(BaseGameState.Initialization))
            {
                PreviewLevel(LevelIndex, true);
            }
        }


        /// <summary>
        /// The language changed: the interface wrote its screens again (RunnerUI.RefreshTexts); the standings of a
        /// race's results and the HUDs of a local race are written here, where the racers are.
        /// </summary>
        protected override void OnLanguageChanged()
        {
            base.OnLanguageChanged();
            if (raceResultsShown)
            {
                ui?.UpdateStandings(RaceStandingsText());
            }
            if (InLocalRace)
            {
                foreach (RunnerRun run in localRace.Runs)
                {
                    run.Hud?.Redraw();
                }
                if (localResultsShown)
                {
                    ui?.UpdateStandings(LocalStandingsText());
                }
            }
        }


        private void ApplyLook(RunnerGameTheme look)
        {
            if (look == null)
            {
                return;
            }
            ui?.ApplyTheme(look);
            effects?.ApplyTheme(look.Particles);
        }


        /// <summary>The colour of a power-up in the theme, or the manager's own when there is none.</summary>
        private Color PowerUpColor(PowerUpType type)
        {
            RunnerGameTheme look = ThemeAs<RunnerGameTheme>();
            int index = (int)type;
            return look != null ? look.Colors.PowerUp(type) : index < powerUpColors.Length ? powerUpColors[index] : Color.white;
        }


        protected override void OnDestroy()
        {
            // The glows face the main camera again in the next scene.
            Billboard.FaceEveryCamera = false;
            StepAsideInViews(false);
            if (track != null)
            {
                track.HintReached -= OnHintReached;
            }
            base.OnDestroy();
        }


        public override void TransitionState(GameState state)
        {
            base.TransitionState(state);
            switch (state.BaseState)
            {
                case BaseGameState.Initialization:
                    EnterMenu();
                    break;
                case BaseGameState.Paused:
                    sounds?.Duck(true);
                    break;
                case BaseGameState.Running:
                    sounds?.Duck(false);
                    break;
            }
        }


        protected override void Update()
        {
            base.Update();
            if (!IsGameRunning || runner == null || track == null)
            {
                return;
            }
            float deltaTime = Time.deltaTime;
            phaseTime += deltaTime;
            switch (phase)
            {
                case RunPhase.Countdown:
                    UpdateCountdown();
                    break;
                case RunPhase.Running:
                    if (InLocalRace)
                    {
                        UpdateLocalRace(deltaTime);
                    }
                    else
                    {
                        UpdateRun(deltaTime);
                    }
                    break;
                case RunPhase.Finishing:
                    UpdateTrackAround(runner.transform.position.z);
                    UpdateFinish();
                    break;
                case RunPhase.Dying:
                    if (phaseTime > 1.8f)
                    {
                        EndRun(false);
                    }
                    break;
                case RunPhase.Watching:
                    UpdateWatching();
                    break;
            }
            UpdateRace();
            track.Tick(deltaTime, FocusZ());
        }


        #region Menu

        private void EnterMenu()
        {
            // Back in the menu, a local race is over: the other runners and their views go.
            EndLocalMatch();
            phase = RunPhase.Menu;
            ClearPowerUps(Main);
            int level = LevelIndex;
            if (!menuVisited)
            {
                menuVisited = true;
                level = SuggestedLevel();
            }
            PreviewLevel(level, !trackReady || trackLevel != level);
            if (sounds != null)
            {
                sounds.PlayMusic(sounds.menuMusic);
            }
        }


        /// <summary>The first campaign level that is open but has no stars yet, or the last open one.</summary>
        private int SuggestedLevel()
        {
            int suggested = 0;
            for (int i = 0; i < LevelCount; i++)
            {
                if (!(campaign[i] is RunnerLevel level) || level.IsEndless || !progress.IsUnlocked(i))
                {
                    continue;
                }
                suggested = i;
                if (progress.Stars(i) == 0)
                {
                    break;
                }
            }
            return suggested;
        }


        /// <summary>Selects a level in the level select and shows it behind the menu.</summary>
        public void SelectLevel(int index)
        {
            if (phase != RunPhase.Menu || index < 0 || index >= LevelCount)
            {
                return;
            }
            sounds?.Play(sounds.click, 0.6f);
            PreviewLevel(index, index != trackLevel);
        }


        private void PreviewLevel(int index, bool rebuild)
        {
            if (LevelCount == 0)
            {
                return;
            }
            LoadLevel(index);
            ApplySettings();
            if (rebuild || !trackReady)
            {
                BuildTrack();
            }
            runner.ResetToStart(StartPosition);
            runnerCamera.SetMode(RunnerCamera.Mode.Menu, true);
            if (ui != null)
            {
                ui.ShowLevelSelect(BuildSummaries(), LevelIndex, progress.TotalStars(LevelCount), MaxStars());
            }
        }


        /// <summary>Starts the level selected in the level select.</summary>
        public void PlaySelectedLevel()
        {
            if (phase != RunPhase.Menu)
            {
                return;
            }
            RunnerLevel level = RunnerLevel;
            if (level == null || (!level.IsEndless && !progress.IsUnlocked(LevelIndex)))
            {
                UI?.UpdateError(RunnerText.T("Finish the previous level to unlock this one."));
                return;
            }
            sounds?.Play(sounds.click);
            PlayLevel(LevelIndex);
        }


        public void RetryLevel()
        {
            PlayLevel(LevelIndex);
        }


        public void PlayNextLevel()
        {
            int next = LevelIndex + 1;
            if (next < LevelCount && progress.IsUnlocked(next))
            {
                PlayLevel(next);
            }
            else
            {
                ReturnToLevelSelect();
            }
        }


        public void ReturnToLevelSelect()
        {
            TransitionState(BaseGameState.Initialization);
        }


        /// <summary>Clears every star and record after asking the player; used by the level select.</summary>
        public void ResetProgress()
        {
            progress.Reset(LevelCount);
            menuVisited = false;
            TransitionState(BaseGameState.Initialization);
        }


        /// <summary>Plays a level of the campaign, open or not: the level select checks that first (the tours do not).</summary>
        internal void PlayLevel(int index)
        {
            if (controller != null)
            {
                controller.PrepareGame(LevelDataFor(index));
            }
            else
            {
                LoadLevel(index);
                StartGame();
            }
        }


        private LevelData LevelDataFor(int index)
        {
            if (!levelData.TryGetValue(index, out LevelData data) || data == null)
            {
                data = LevelData.Create(index);
                levelData[index] = data;
            }
            return data;
        }


        private List<LevelSummary> BuildSummaries()
        {
            var summaries = new List<LevelSummary>();
            for (int i = 0; i < LevelCount; i++)
            {
                if (!(campaign[i] is RunnerLevel level))
                {
                    continue;
                }
                summaries.Add(new LevelSummary
                {
                    Index = i,
                    Title = RunnerGameTheme.TitleFor(level),
                    Description = RunnerGameTheme.DescriptionFor(level),
                    World = level.Theme != null ? level.Theme.displayName : string.Empty,
                    Endless = level.IsEndless,
                    Unlocked = level.IsEndless || progress.IsUnlocked(i),
                    Stars = progress.Stars(i),
                    Length = level.Length,
                    CoinGoal = i == trackLevel ? coinGoal : 0,
                    BestScore = level.IsEndless ? progress.EndlessBestScore : progress.BestScore(i),
                    BestDistance = progress.EndlessBestDistance,
                    Accent = level.Theme != null ? level.Theme.accent : Color.white
                });
            }
            return summaries;
        }


        private int MaxStars()
        {
            int count = 0;
            for (int i = 0; i < LevelCount; i++)
            {
                if (campaign[i] is RunnerLevel level && !level.IsEndless)
                {
                    count += 3;
                }
            }
            return count;
        }

        #endregion


        #region Run

        /// <summary>Resets the run, puts the runner on the start line and counts down.</summary>
        public override void StartGame()
        {
            RunnerLevel level = RunnerLevel;
            if (level == null || runner == null || track == null)
            {
                UI?.UpdateError(RunnerText.T("The Endless Runner scene is missing its level, runner or track."));
                return;
            }
            ApplySettings();
            // The track of the level select may be laid out with the player's custom settings; a session runs the level's own.
            if (InSession || !trackReady || trackLevel != LevelIndex || level.IsEndless)
            {
                BuildTrack();
            }
            trackReady = false;
            ResetRun();
            if (InLocalRace)
            {
                LineUpLocalRunners();
            }
            else
            {
                int slot = LocalSlot;
                runner.ResetToStart(StartPositionOf(slot), StartLane(slot));
                runnerCamera.SetMode(RunnerCamera.Mode.Chase);
            }
            phase = RunPhase.Countdown;
            phaseTime = 0f;
            countdown.Restart();
            TransitionState(BaseGameState.Running);
            if (sounds != null)
            {
                sounds.PlayMusic(sounds.runMusic);
            }
            ui?.BeginRun(RunnerGameTheme.TitleFor(level), LevelIndex, level.IsEndless, Main.MaxHearts, level.IsEndless ? progress.EndlessBestDistance : level.Length);
            RefreshHud();
            if (race != null)
            {
                BeginRace();
            }
            if (InLocalRace)
            {
                BeginLocalRace();
            }
        }


        public override void LoadLevel(int level)
        {
            LevelIndex = Mathf.Clamp(level, 0, Mathf.Max(0, LevelCount - 1));
            CurrentLevel = LevelDataFor(LevelIndex);
            if (Level is RunnerLevel runnerLevel && runnerLevel.Settings != null)
            {
                runnerSettings = runnerLevel.Settings;
                if (UsingDefaultSettings)
                {
                    Settings = runnerSettings;
                }
            }
            UpdateLevel();
        }


        private void ApplySettings()
        {
            runner?.ApplySettings(RunnerSettings);
            ApplyLocalSettings();
        }


        private void BuildTrack()
        {
            RunnerLevel level = RunnerLevel;
            if (level == null || track == null)
            {
                return;
            }
            track.Build(level, RunnerSettings, TrackSeed(level), runner.Gravity);
            track.UpdateTrack(0f, 110f);
            // The sky and the world come with the track, in the look it was laid out in.
            if (track.Look != null)
            {
                themes?.SetSky(track.Look.Sky);
            }
            themes?.Apply(track.ThemeAt(0f), true);
            // The coins of a race are shared, so there is no goal of one's own to reach.
            coinGoal = level.IsEndless || race != null || InLocalRace ? 0 : Mathf.Max(1, Mathf.CeilToInt(track.LevelCoins * level.CoinGoal));
            trackReady = true;
            trackLevel = LevelIndex;
        }


        private void ResetRun()
        {
            int hearts = Mathf.Clamp(RunnerSettings.Hearts, 1, RunnerSettings.HeartLimit);
            foreach (RunnerRun run in Runs)
            {
                run.Reset(hearts);
                ClearPowerUps(run);
            }
            ResetScore();
        }


        private void UpdateCountdown()
        {
            if (!countdown.Advance(phaseTime, countdownTime, out string label))
            {
                return;
            }
            ui?.ShowCountdown(label == Countdown.GoLabel ? RunnerText.T(Countdown.GoLabel) : label);
            if (!countdown.IsDone)
            {
                sounds?.Play(sounds.countdown, 0.8f);
                return;
            }
            sounds?.Play(sounds.go);
            phase = RunPhase.Running;
            phaseTime = 0f;
            foreach (RunnerRun run in Runs)
            {
                run.Enter(RunPhase.Running);
                run.Runner.BeginRun();
            }
        }


        private void UpdateRun(float deltaTime)
        {
            RunnerRun run = Main;
            float z = runner.transform.position.z;
            run.RunTo(z);
            runner.SetTargetSpeed(RunnerSettings.SpeedAtDistance(z));
            UpdateTrackAround(z);
            track.CheckHints(z);
            UpdatePickups(run, deltaTime);
            UpdateMovingObstacles(run);
            UpdatePowerUps(run, deltaTime);
            UpdateWorld(z);
            PlayerScore = run.Score;
            UpdateScore();
            RefreshHud();
            if (RunnerLevel.IsEndless && !run.RecordAnnounced && progress.EndlessBestDistance > 50f && run.Distance > progress.EndlessBestDistance)
            {
                run.RecordAnnounced = true;
                RunnerGameTheme look = ThemeAs<RunnerGameTheme>();
                ui?.Toast(RunnerText.T("NEW RECORD!"), look != null ? look.Colors.accent : new Color(1f, 0.85f, 0.2f));
                sounds?.Play(sounds.star);
            }
            if (z >= track.FinishZ)
            {
                BeginFinish();
            }
        }


        private void RefreshHud()
        {
            if (ui == null)
            {
                return;
            }
            RunnerRun run = Main;
            ui.UpdateRun(run.Coins, coinGoal, Mathf.FloorToInt(PlayerScore), run.Distance, RunProgress(run), run.Hearts, run.MaxHearts,
                run.IsPowerUpActive(PowerUpType.Multiplier));
            for (int i = 0; i < PowerUps.Count; i++)
            {
                ui.SetPowerUp((PowerUpType)i, PowerUpFraction(run, (PowerUpType)i));
            }
        }


        /// <summary>How far along the level <paramref name="run"/> is, from 0 to 1; 0 on the endless run.</summary>
        private float RunProgress(RunnerRun run)
        {
            RunnerLevel level = RunnerLevel;
            return level != null && !level.IsEndless ? Mathf.Clamp01(run.Distance / Mathf.Max(1f, track.FinishZ)) : 0f;
        }


        /// <summary>What is left of a power-up of <paramref name="run"/>, from 1 when it was picked up to 0 when it is gone.</summary>
        private float PowerUpFraction(RunnerRun run, PowerUpType type)
        {
            int index = (int)type;
            float duration = index < powerUpDurations.Length ? powerUpDurations[index] : 10f;
            float left = run.PowerUpLeft(type);
            return left > 0f ? left / duration : 0f;
        }


        /// <summary>Swept test of every live pickup against the runner of <paramref name="run"/>, plus the magnet's pull.</summary>
        private void UpdatePickups(RunnerRun run, float deltaTime)
        {
            RunnerPlayer runner = run.Runner;
            Vector3 from = runner.PreviousPosition;
            Vector3 to = runner.transform.position;
            float height = runner.Height;
            bool magnet = run.IsPowerUpActive(PowerUpType.Magnet);
            Vector3 chest = to + Vector3.up * (height * 0.55f);
            IReadOnlyList<Collidable> pickups = track.Pickups;
            for (int i = 0; i < pickups.Count; i++)
            {
                Collidable pickup = pickups[i];
                if (!pickup.Live || pickup.Collected)
                {
                    continue;
                }
                Vector3 position = pickup.transform.position;
                if (magnet && pickup.Magnetic)
                {
                    Vector3 toRunner = chest - position;
                    float distanceToRunner = toRunner.magnitude;
                    if (distanceToRunner < magnetRadius || (position.z < to.z + 14f && position.z > to.z - 2f && Mathf.Abs(toRunner.x) < magnetRadius))
                    {
                        float pull = magnetPull * (1.5f - Mathf.Clamp01(distanceToRunner / magnetRadius)) * deltaTime;
                        position = Vector3.MoveTowards(position, chest, pull + runner.Speed * deltaTime);
                        pickup.transform.position = position;
                    }
                }
                if (position.z < from.z - 3f || position.z > to.z + 3f)
                {
                    continue;
                }
                if (Touches(pickup, position, from, to, height))
                {
                    pickup.Touch(this, runner);
                }
            }
        }


        private static bool Touches(Collidable pickup, Vector3 position, Vector3 from, Vector3 to, float height)
        {
            float span = to.z - from.z;
            float t = span > 0.0001f ? Mathf.Clamp01((position.z - from.z) / span) : 1f;
            Vector3 body = Vector3.Lerp(from, to, t);
            float dx = position.x - body.x;
            float dz = position.z - body.z;
            float y = Mathf.Clamp(position.y, body.y + 0.2f, body.y + Mathf.Max(0.3f, height - 0.1f));
            float dy = position.y - y;
            float reach = pickup.Radius + 0.45f;
            return dx * dx + dy * dy + dz * dz <= reach * reach;
        }


        /// <summary>Carts move into the runner on their own, which the controller's sweep does not always see.</summary>
        private void UpdateMovingObstacles(RunnerRun run)
        {
            RunnerPlayer runner = run.Runner;
            Vector3 feet = runner.transform.position;
            float height = runner.Height;
            var runnerBounds = new Bounds(feet + Vector3.up * (height * 0.5f), new Vector3(0.6f, height, 0.6f));
            IReadOnlyList<Obstacle> obstacles = track.Obstacles;
            for (int i = 0; i < obstacles.Count; i++)
            {
                Obstacle obstacle = obstacles[i];
                if (!obstacle.Live || obstacle.IsKnocked || !obstacle.IsMoving)
                {
                    continue;
                }
                Bounds bounds = obstacle.GetBounds();
                if (bounds.Intersects(runnerBounds) && feet.y < bounds.max.y - 0.2f)
                {
                    PlayerCrashed(runner, obstacle, bounds.ClosestPoint(runnerBounds.center));
                    return;
                }
            }
        }


        private static void UpdatePowerUps(RunnerRun run, float deltaTime)
        {
            run.TickPowerUps(deltaTime);
            RunnerPlayer runner = run.Runner;
            runner.SuperJump = run.IsPowerUpActive(PowerUpType.SuperJump);
            runner.SetAuras(run.IsPowerUpActive(PowerUpType.Shield), run.IsPowerUpActive(PowerUpType.Magnet), runner.SuperJump);
        }


        private static void ClearPowerUps(RunnerRun run)
        {
            run.ClearPowerUps();
            if (run.Runner != null)
            {
                run.Runner.SuperJump = false;
                run.Runner.SetAuras(false, false, false);
            }
        }


        /// <summary>Endless runs move through the worlds; the environment follows the track's theme.</summary>
        private void UpdateWorld(float z)
        {
            RunnerTheme theme = track.ThemeAt(z + 20f);
            if (themes != null && theme != null && theme != themes.Theme)
            {
                themes.Apply(theme, false);
                ui?.Toast(RunnerText.SayF("Welcome to {0}!", RunnerText.T(theme.displayName)), theme.accent);
            }
        }


        private void BeginFinish()
        {
            phase = RunPhase.Finishing;
            phaseTime = 0f;
            runner.SetTargetSpeed(3f);
            runnerCamera.SetMode(RunnerCamera.Mode.Finish);
            effects?.Confetti(runner.transform.position + new Vector3(0f, 5f, 6f));
            sounds?.StopMusic();
            sounds?.Play(sounds.victory);
            ui?.ShowCountdown(RunnerText.T("FINISH!"));
            ClearPowerUps(Main);
        }


        private void UpdateFinish()
        {
            if (runner.IsRunning && phaseTime > 1.3f)
            {
                runner.StopRun();
                runner.animator?.Celebrate();
                effects?.Confetti(runner.transform.position + new Vector3(0f, 5f, 2f));
            }
            if (phaseTime > 3.4f)
            {
                EndRun(true);
            }
        }


        /// <summary>The runner of <paramref name="run"/> is out of hearts.</summary>
        private void Die(RunnerRun run)
        {
            if (InLocalRace)
            {
                KnockOut(run);
                return;
            }
            phase = RunPhase.Dying;
            phaseTime = 0f;
            runner.StopRun();
            runner.animator?.Die();
            runnerCamera.SetMode(RunnerCamera.Mode.Crash);
            sounds?.StopMusic();
            sounds?.Play(sounds.gameOver);
            ClearPowerUps(Main);
        }


        private void EndRun(bool victory)
        {
            if (race != null)
            {
                EndRaceRun(victory);
                return;
            }
            RunnerLevel level = RunnerLevel;
            RunnerRun run = Main;
            int score = Mathf.FloorToInt(PlayerScore);
            var result = new RunResult
            {
                LevelTitle = RunnerGameTheme.TitleFor(level),
                Victory = victory,
                Endless = level.IsEndless,
                Coins = run.Coins,
                CoinGoal = coinGoal,
                Distance = run.Distance,
                Score = score,
                CoinGoalReached = run.Coins >= coinGoal,
                Flawless = run.HeartsLost == 0
            };
            if (level.IsEndless)
            {
                result.NewBest = progress.RecordEndless(run.Distance, score);
                result.BestDistance = progress.EndlessBestDistance;
            }
            else if (victory)
            {
                result.Stars = 1 + (result.CoinGoalReached ? 1 : 0) + (result.Flawless ? 1 : 0);
                result.NewBest = progress.RecordLevel(LevelIndex, result.Stars, score);
            }
            int next = LevelIndex + 1;
            result.HasNextLevel = victory && next < LevelCount && progress.IsUnlocked(next);
            phase = RunPhase.Menu;
            runner.StopRun();
            ui?.ShowResults(result);
            TransitionState(victory ? BaseGameState.Victory : BaseGameState.GameOver);
        }

        #endregion


        #region Runner events

        /// <summary>The run of <paramref name="player"/>: the runner of the scene's, or that of a runner of a local race.</summary>
        private RunnerRun RunOf(RunnerPlayer player)
        {
            return InLocalRace ? localRace.RunOf(player) : Main;
        }


        /// <summary>Whether the runner of <paramref name="run"/> is out on the track: what happens to it counts.</summary>
        private bool IsRunning(RunnerRun run)
        {
            return run != null && phase == RunPhase.Running && (!InLocalRace || run.Phase == RunPhase.Running);
        }


        internal void CollectCoin(Coin coin, RunnerPlayer player)
        {
            if (race != null)
            {
                ClaimCoin(coin);
                return;
            }
            RunnerRun run = RunOf(player);
            if (run == null)
            {
                return;
            }
            run.Collect(coin.Value, pointsPerCoin);
            Vector3 position = coin.transform.position;
            if (coin.IsGem)
            {
                effects?.Gem(position);
            }
            else
            {
                effects?.Coin(position);
            }
            sounds?.Coin(coin.IsGem);
            if (run.Hud != null)
            {
                run.Hud.PunchCoins();
            }
            else
            {
                ui?.PunchCoins();
            }
            track.Recycle(coin);
        }


        internal void CollectPowerUp(PowerUpPickup pickup, RunnerPlayer player)
        {
            RunnerRun run = RunOf(player);
            if (run == null)
            {
                return;
            }
            int index = (int)pickup.Type;
            run.GivePowerUp(pickup.Type, index < powerUpDurations.Length ? powerUpDurations[index] : 10f);
            Color color = PowerUpColor(pickup.Type);
            effects?.PowerUp(pickup.transform.position, color);
            sounds?.Play(sounds.powerUp);
            Toast(run, $"{RunnerText.Say(PowerUps.Title(pickup.Type))}!\n<size=60%>{RunnerText.Say(PowerUps.Description(pickup.Type))}</size>", color);
            track.Recycle(pickup);
            UpdatePowerUps(run, 0f);
        }


        internal void Launch(JumpPad pad, RunnerPlayer player)
        {
            player.Launch(pad.LaunchHeight);
            effects?.Bounce(pad.transform.position + Vector3.up * 0.2f);
            sounds?.Play(sounds.bounce);
        }


        /// <summary>The runner ran into the front of an obstacle.</summary>
        internal void PlayerCrashed(RunnerPlayer runner, Obstacle obstacle, Vector3 point)
        {
            RunnerRun run = RunOf(runner);
            if (!IsRunning(run) || obstacle == null || obstacle.IsKnocked)
            {
                return;
            }
            RemoveAttached(obstacle);
            effects?.Crash(point);
            run.Camera.Shake(0.7f);
            if (runner.IsInvulnerable)
            {
                obstacle.Knock(point, runner.Speed);
                sounds?.Play(sounds.bump);
                return;
            }
            if (run.IsPowerUpActive(PowerUpType.Shield))
            {
                run.GivePowerUp(PowerUpType.Shield, 0f);
                obstacle.Knock(point, runner.Speed);
                effects?.ShieldBreak(runner.transform.position + Vector3.up);
                sounds?.Play(sounds.shieldBreak);
                runner.MakeInvulnerable(0.8f);
                Toast(run, RunnerText.Say("Shield saved you!"), PowerUpColor(PowerUpType.Shield));
                UpdatePowerUps(run, 0f);
                return;
            }
            sounds?.Play(sounds.crash);
            if (LoseHeart(run))
            {
                Die(run);
                return;
            }
            obstacle.Knock(point, runner.Speed);
            runner.Slow(crashSlowdown);
            runner.MakeInvulnerable(invulnerableTime);
            runner.animator?.Stumble();
        }


        /// <summary>The runner switched lanes into the side of an obstacle: it bounces back, no harm done.</summary>
        internal void PlayerBumped(RunnerPlayer runner, Obstacle obstacle, Vector3 point)
        {
            RunnerRun run = RunOf(runner);
            if (!IsRunning(run))
            {
                return;
            }
            runner.BumpBack();
            run.Camera.Shake(0.25f);
            sounds?.Play(sounds.bump, 0.7f);
        }


        /// <summary>The runner fell into a chasm: it costs a heart and the runner is put back on the far side.</summary>
        internal void PlayerFell(RunnerPlayer runner)
        {
            RunnerRun run = RunOf(runner);
            if (!IsRunning(run))
            {
                return;
            }
            Vector3 position = runner.transform.position;
            RunnerTheme theme = track.ThemeAt(position.z);
            effects?.Splash(new Vector3(position.x, -0.5f, position.z), theme != null && theme.chasmFill != null ? theme.chasmFill.color : Color.white);
            sounds?.Play(sounds.splash);
            if (LoseHeart(run))
            {
                Die(run);
                return;
            }
            float respawnZ = track.TryGetGap(position.z, out Vector2 gap) ? gap.y + 1.5f : position.z + 3f;
            runner.Respawn(new Vector3(runner.Lane * runner.LaneWidth, 0.05f, respawnZ));
            runner.Slow(0.6f);
            runner.MakeInvulnerable(2f);
            run.Camera.Shake(0.4f);
        }


        internal void PlayerJumped(RunnerPlayer runner)
        {
            sounds?.Play(sounds.jump, 0.7f);
        }


        internal void PlayerSlid(RunnerPlayer runner)
        {
            sounds?.Play(sounds.slide, 0.7f);
        }


        internal void PlayerChangedLane(RunnerPlayer runner)
        {
            sounds?.Play(sounds.whoosh, 0.35f);
        }


        internal void PlayerLanded(RunnerPlayer runner, float impactSpeed)
        {
            if (impactSpeed < 6f || runner == null)
            {
                return;
            }
            RunnerTheme theme = track != null ? track.ThemeAt(runner.transform.position.z) : null;
            effects?.Land(runner.transform.position + Vector3.up * 0.1f, theme != null ? theme.dustColor : Color.white);
            sounds?.Play(sounds.land, Mathf.Clamp01(impactSpeed / 20f));
        }


        /// <summary>The runner of <paramref name="run"/> loses a heart; true when it was the last one.</summary>
        private bool LoseHeart(RunnerRun run)
        {
            bool last = run.LoseHeart();
            if (run.Hud != null)
            {
                run.Hud.HeartLost();
            }
            else
            {
                ui?.HeartLost(run.Hearts, run.MaxHearts);
            }
            return last;
        }


        /// <summary>A word for the player of <paramref name="run"/>: over its own view in a local race.</summary>
        private void Toast(RunnerRun run, string text, Color color)
        {
            if (run.Hud != null)
            {
                run.Hud.Toast(text, color);
            }
            else
            {
                ui?.Toast(text, color);
            }
        }


        private void RemoveAttached(TrackPiece piece)
        {
            if (piece.Attached.Count == 0)
            {
                return;
            }
            var attached = new List<TrackPiece>(piece.Attached);
            piece.Attached.Clear();
            foreach (TrackPiece item in attached)
            {
                track.Recycle(item);
            }
        }


        private void OnHintReached(string hint)
        {
            // The hints teach a runner alone; a local race shares the screen and leaves them out.
            if (!InLocalRace && (phase == RunPhase.Running || phase == RunPhase.Countdown))
            {
                ui?.ShowHint(hint);
            }
        }

        #endregion


        public override bool DoesSaveGameExist()
        {
            return false;
        }


        public override void SaveGame()
        {
            UI?.UpdateError(RunnerText.T("Endless Runner saves your stars and records automatically."));
        }


        public override void LoadGame()
        {
            UI?.UpdateError(RunnerText.T("Endless Runner does not support loading a run."));
        }
    }
}
