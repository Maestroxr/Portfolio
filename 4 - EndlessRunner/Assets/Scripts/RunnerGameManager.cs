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
    }


    /// <summary>
    /// Endless Runner game module. Runs the level select, the countdown, the run itself (track, pickups, power-ups,
    /// hearts), the finish and the results, and keeps the campaign progress. The menu, pause and state flow come from
    /// <see cref="BaseGameManager"/>. Races against the runners of an online room are in RunnerGameManager.Online.cs.
    /// </summary>
    public partial class RunnerGameManager : BaseGameManager
    {
        private enum RunPhase
        {
            Menu,
            Countdown,
            Running,
            Finishing,
            Dying,
            /// <summary>The local run of a race is over; the player watches the runners who are still out there.</summary>
            Watching
        }

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
        private readonly float[] powerUpTimers = new float[PowerUps.Count];
        private RunPhase phase = RunPhase.Menu;
        private float phaseTime;
        private int countdownStep;
        private int coins;
        private float coinPoints;
        private float distance;
        private int hearts;
        private int maxHearts;
        private int heartsLost;
        private int coinGoal;
        private bool trackReady;
        private int trackLevel = -1;
        private bool recordAnnounced;
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

        public int Coins => coins;

        public float Distance => distance;

        public bool IsPowerUpActive(PowerUpType type)
        {
            return powerUpTimers[(int)type] > 0f;
        }

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
            base.Start();
        }


        protected override void OnDestroy()
        {
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
                    UpdateRun(deltaTime);
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
            phase = RunPhase.Menu;
            ClearPowerUps();
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
                UI?.UpdateError("Finish the previous level to unlock this one.");
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


        private void PlayLevel(int index)
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
                    Title = level.Title,
                    Description = level.Description,
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
                UI?.UpdateError("The Endless Runner scene is missing its level, runner or track.");
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
            int slot = LocalSlot;
            runner.ResetToStart(StartPositionOf(slot), StartLane(slot));
            runnerCamera.SetMode(RunnerCamera.Mode.Chase);
            phase = RunPhase.Countdown;
            phaseTime = 0f;
            countdownStep = -1;
            TransitionState(BaseGameState.Running);
            if (sounds != null)
            {
                sounds.PlayMusic(sounds.runMusic);
            }
            ui?.BeginRun(level.Title, LevelIndex, level.IsEndless, maxHearts, level.IsEndless ? progress.EndlessBestDistance : level.Length);
            RefreshHud();
            if (race != null)
            {
                BeginRace();
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
            themes?.Apply(level.ThemeAt(0f), true);
            // The coins of a race are shared, so there is no goal of one's own to reach.
            coinGoal = level.IsEndless || race != null ? 0 : Mathf.Max(1, Mathf.CeilToInt(track.LevelCoins * level.CoinGoal));
            trackReady = true;
            trackLevel = LevelIndex;
        }


        private void ResetRun()
        {
            coins = 0;
            coinPoints = 0f;
            distance = 0f;
            maxHearts = Mathf.Clamp(RunnerSettings.Hearts, 1, RunnerSettings.HeartLimit);
            hearts = maxHearts;
            heartsLost = 0;
            recordAnnounced = false;
            ClearPowerUps();
            ResetScore();
        }


        private void UpdateCountdown()
        {
            float stepTime = countdownTime / 3f;
            int step = Mathf.FloorToInt(phaseTime / stepTime);
            if (step == countdownStep)
            {
                return;
            }
            countdownStep = step;
            if (step < 3)
            {
                ui?.ShowCountdown((3 - step).ToString());
                sounds?.Play(sounds.countdown, 0.8f);
                return;
            }
            ui?.ShowCountdown("GO!");
            sounds?.Play(sounds.go);
            phase = RunPhase.Running;
            phaseTime = 0f;
            runner.BeginRun();
        }


        private void UpdateRun(float deltaTime)
        {
            float z = runner.transform.position.z;
            distance = Mathf.Max(distance, z);
            runner.SetTargetSpeed(RunnerSettings.SpeedAtDistance(z));
            UpdateTrackAround(z);
            track.CheckHints(z);
            UpdatePickups(deltaTime);
            UpdateMovingObstacles();
            UpdatePowerUps(deltaTime);
            UpdateWorld(z);
            PlayerScore = Mathf.Floor(distance) + coinPoints;
            UpdateScore();
            RefreshHud();
            if (RunnerLevel.IsEndless && !recordAnnounced && progress.EndlessBestDistance > 50f && distance > progress.EndlessBestDistance)
            {
                recordAnnounced = true;
                ui?.Toast("NEW RECORD!", new Color(1f, 0.85f, 0.2f));
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
            RunnerLevel level = RunnerLevel;
            float progress01 = level != null && !level.IsEndless ? Mathf.Clamp01(distance / Mathf.Max(1f, track.FinishZ)) : 0f;
            ui.UpdateRun(coins, coinGoal, Mathf.FloorToInt(PlayerScore), distance, progress01, hearts, maxHearts,
                IsPowerUpActive(PowerUpType.Multiplier));
            for (int i = 0; i < PowerUps.Count; i++)
            {
                float duration = i < powerUpDurations.Length ? powerUpDurations[i] : 10f;
                ui.SetPowerUp((PowerUpType)i, powerUpTimers[i] > 0f ? powerUpTimers[i] / duration : 0f);
            }
        }


        /// <summary>Swept test of every live pickup against the runner, plus the magnet's pull.</summary>
        private void UpdatePickups(float deltaTime)
        {
            Vector3 from = runner.PreviousPosition;
            Vector3 to = runner.transform.position;
            float height = runner.Height;
            bool magnet = IsPowerUpActive(PowerUpType.Magnet);
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
        private void UpdateMovingObstacles()
        {
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
                    PlayerCrashed(obstacle, bounds.ClosestPoint(runnerBounds.center));
                    return;
                }
            }
        }


        private void UpdatePowerUps(float deltaTime)
        {
            for (int i = 0; i < powerUpTimers.Length; i++)
            {
                if (powerUpTimers[i] <= 0f)
                {
                    continue;
                }
                powerUpTimers[i] = Mathf.Max(0f, powerUpTimers[i] - deltaTime);
            }
            runner.SuperJump = IsPowerUpActive(PowerUpType.SuperJump);
            runner.SetAuras(IsPowerUpActive(PowerUpType.Shield), IsPowerUpActive(PowerUpType.Magnet), runner.SuperJump);
        }


        private void ClearPowerUps()
        {
            for (int i = 0; i < powerUpTimers.Length; i++)
            {
                powerUpTimers[i] = 0f;
            }
            if (runner != null)
            {
                runner.SuperJump = false;
                runner.SetAuras(false, false, false);
            }
        }


        /// <summary>Endless runs move through the worlds; the environment follows the track's theme.</summary>
        private void UpdateWorld(float z)
        {
            RunnerTheme theme = track.ThemeAt(z + 20f);
            if (themes != null && theme != null && theme != themes.Theme)
            {
                themes.Apply(theme, false);
                ui?.Toast($"Welcome to {theme.displayName}!", theme.accent);
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
            ui?.ShowCountdown("FINISH!");
            ClearPowerUps();
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


        private void Die()
        {
            phase = RunPhase.Dying;
            phaseTime = 0f;
            runner.StopRun();
            runner.animator?.Die();
            runnerCamera.SetMode(RunnerCamera.Mode.Crash);
            sounds?.StopMusic();
            sounds?.Play(sounds.gameOver);
            ClearPowerUps();
        }


        private void EndRun(bool victory)
        {
            if (race != null)
            {
                EndRaceRun(victory);
                return;
            }
            RunnerLevel level = RunnerLevel;
            int score = Mathf.FloorToInt(PlayerScore);
            var result = new RunResult
            {
                LevelTitle = level.Title,
                Victory = victory,
                Endless = level.IsEndless,
                Coins = coins,
                CoinGoal = coinGoal,
                Distance = distance,
                Score = score,
                CoinGoalReached = coins >= coinGoal,
                Flawless = heartsLost == 0
            };
            if (level.IsEndless)
            {
                result.NewBest = progress.RecordEndless(distance, score);
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

        internal void CollectCoin(Coin coin)
        {
            if (race != null)
            {
                ClaimCoin(coin);
                return;
            }
            int multiplier = IsPowerUpActive(PowerUpType.Multiplier) ? 2 : 1;
            coins += coin.Value * multiplier;
            coinPoints += coin.Value * pointsPerCoin * multiplier;
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
            ui?.PunchCoins();
            track.Recycle(coin);
        }


        internal void CollectPowerUp(PowerUpPickup pickup)
        {
            int index = (int)pickup.Type;
            powerUpTimers[index] = index < powerUpDurations.Length ? powerUpDurations[index] : 10f;
            Color color = index < powerUpColors.Length ? powerUpColors[index] : Color.white;
            effects?.PowerUp(pickup.transform.position, color);
            sounds?.Play(sounds.powerUp);
            ui?.Toast($"{PowerUps.Title(pickup.Type)}!\n<size=60%>{PowerUps.Description(pickup.Type)}</size>", color);
            track.Recycle(pickup);
            UpdatePowerUps(0f);
        }


        internal void Launch(JumpPad pad)
        {
            runner.Launch(pad.LaunchHeight);
            effects?.Bounce(pad.transform.position + Vector3.up * 0.2f);
            sounds?.Play(sounds.bounce);
        }


        /// <summary>The runner ran into the front of an obstacle.</summary>
        internal void PlayerCrashed(Obstacle obstacle, Vector3 point)
        {
            if (phase != RunPhase.Running || obstacle == null || obstacle.IsKnocked)
            {
                return;
            }
            RemoveAttached(obstacle);
            effects?.Crash(point);
            runnerCamera.Shake(0.7f);
            if (runner.IsInvulnerable)
            {
                obstacle.Knock(point, runner.Speed);
                sounds?.Play(sounds.bump);
                return;
            }
            if (IsPowerUpActive(PowerUpType.Shield))
            {
                powerUpTimers[(int)PowerUpType.Shield] = 0f;
                obstacle.Knock(point, runner.Speed);
                effects?.ShieldBreak(runner.transform.position + Vector3.up);
                sounds?.Play(sounds.shieldBreak);
                runner.MakeInvulnerable(0.8f);
                ui?.Toast("Shield saved you!", powerUpColors[(int)PowerUpType.Shield]);
                UpdatePowerUps(0f);
                return;
            }
            sounds?.Play(sounds.crash);
            LoseHeart();
            if (hearts <= 0)
            {
                Die();
                return;
            }
            obstacle.Knock(point, runner.Speed);
            runner.Slow(crashSlowdown);
            runner.MakeInvulnerable(invulnerableTime);
            runner.animator?.Stumble();
        }


        /// <summary>The runner switched lanes into the side of an obstacle: it bounces back, no harm done.</summary>
        internal void PlayerBumped(Obstacle obstacle, Vector3 point)
        {
            if (phase != RunPhase.Running)
            {
                return;
            }
            runner.BumpBack();
            runnerCamera.Shake(0.25f);
            sounds?.Play(sounds.bump, 0.7f);
        }


        /// <summary>The runner fell into a chasm: it costs a heart and the runner is put back on the far side.</summary>
        internal void PlayerFell()
        {
            if (phase != RunPhase.Running)
            {
                return;
            }
            Vector3 position = runner.transform.position;
            RunnerTheme theme = track.ThemeAt(position.z);
            effects?.Splash(new Vector3(position.x, -0.5f, position.z), theme != null && theme.chasmFill != null ? theme.chasmFill.color : Color.white);
            sounds?.Play(sounds.splash);
            LoseHeart();
            if (hearts <= 0)
            {
                Die();
                return;
            }
            float respawnZ = track.TryGetGap(position.z, out Vector2 gap) ? gap.y + 1.5f : position.z + 3f;
            runner.Respawn(new Vector3(runner.Lane * runner.LaneWidth, 0.05f, respawnZ));
            runner.Slow(0.6f);
            runner.MakeInvulnerable(2f);
            runnerCamera.Shake(0.4f);
        }


        internal void PlayerJumped()
        {
            sounds?.Play(sounds.jump, 0.7f);
        }


        internal void PlayerSlid()
        {
            sounds?.Play(sounds.slide, 0.7f);
        }


        internal void PlayerChangedLane()
        {
            sounds?.Play(sounds.whoosh, 0.35f);
        }


        internal void PlayerLanded(float impactSpeed)
        {
            if (impactSpeed < 6f || runner == null)
            {
                return;
            }
            RunnerTheme theme = track != null ? track.ThemeAt(runner.transform.position.z) : null;
            effects?.Land(runner.transform.position + Vector3.up * 0.1f, theme != null ? theme.dustColor : Color.white);
            sounds?.Play(sounds.land, Mathf.Clamp01(impactSpeed / 20f));
        }


        private void LoseHeart()
        {
            hearts = Mathf.Max(0, hearts - 1);
            heartsLost++;
            ui?.HeartLost(hearts, maxHearts);
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
            if (phase == RunPhase.Running || phase == RunPhase.Countdown)
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
            UI?.UpdateError("Endless Runner saves your stars and records automatically.");
        }


        public override void LoadGame()
        {
            UI?.UpdateError("Endless Runner does not support loading a run.");
        }
    }
}
