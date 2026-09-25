using System;
using System.Collections.Generic;
using Gamebox;
using Gamebox.UI;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// Asteroids game module. Runs the mission select and the hangar, the briefing countdown, the mission itself (the
    /// ship, the waves, hazards and bosses, scoring and combos, pickups and lives), the victory or defeat, the results
    /// and the campaign progress. The menu, pause, settings and level flow come from <see cref="BaseGameManager"/>;
    /// the campaign is an <see cref="AsteroidsCampaign"/> and the progress an <see cref="AsteroidsProgress"/>.
    /// Missions flown together with other pilots online are in AsteroidsGameManager.Coop.cs.
    /// </summary>
    public partial class AsteroidsGameManager : BaseGameManager
    {
        private enum MissionPhase
        {
            Menu,
            Briefing,
            Playing,
            Respawning,
            /// <summary>Out of ships in a shared mission: the others fly on, and this pilot watches.</summary>
            Watching,
            Victory,
            Defeat,
            Results
        }

        public delegate void StateChanged(BaseGameState state);
        public event StateChanged StateChangedEvent;

        #region Field Members
        [SerializeField] private AsteroidSettings asteroidSettings;
        [SerializeField] private AsteroidsController controller;
        [SerializeField] private AsteroidsUI ui;
        [SerializeField] private Campaign campaign;
        public Playground Playground;

        [SerializeField] internal SpaceField field;
        [SerializeField] internal SpawnService spawner;
        [SerializeField] internal SpaceBackdrop backdrop;
        [SerializeField] internal CameraRig cameraRig;
        [SerializeField] internal SpaceEffects effects;
        [SerializeField] internal AsteroidsAudio sounds;
        [SerializeField] internal AsteroidsPlayer ship;
        [Tooltip("The ships of the hangar; the first one is always available.")]
        [SerializeField] internal PlayerSettings[] hangar = new PlayerSettings[0];

        [Header("Mission")]
        [SerializeField] internal float countdownTime = 2.4f;
        [SerializeField] internal float respawnDelay = 1.8f;
        [SerializeField] internal int lifeBonus = 1000;
        [SerializeField] internal int menuAsteroids = 7;
        [SerializeField] internal float chronoTimeScale = 0.4f;
        [SerializeField] internal float waveBonus = 250f;

        private AsteroidSettings customSettings;
        private AsteroidSettings activeSettings;
        private AsteroidsProgress progress;
        private readonly Dictionary<int, LevelData> levelData = new Dictionary<int, LevelData>();
        private readonly HashSet<HazardKind> hazardsSeen = new HashSet<HazardKind>();
        private readonly ScoreKeeper score = new ScoreKeeper();
        private WaveDirector director;
        private MissionObjective objective;
        private MissionPhase phase = MissionPhase.Menu;
        private float phaseTime;
        private float missionTime;
        private readonly Countdown countdown = new Countdown();
        private int lives;
        private int livesLost;
        private int hintIndex;
        private float hintTimer;
        private Boss boss;
        private int lastMultiplier = 1;
        private bool menuVisited;
        private bool recordAnnounced;
        #endregion

        private const int SaveVersion = 2;

        public override IGameController Controller => controller;
        public override IGameUI UI => ui;
        public override ICampaign Campaign => campaign;
        public override IGameSettings DefaultSettings => asteroidSettings;
        public override IGameSettings CustomSettings => customSettings != null ? customSettings : (customSettings = CreateCustomSettings());

        public override IGameSettings Settings
        {
            get => activeSettings != null ? activeSettings : asteroidSettings;
            set => activeSettings = value as AsteroidSettings;
        }

        protected override bool UsesTimer => false;

        /// <summary>The asteroid settings in use (custom or default).</summary>
        public AsteroidSettings AsteroidSettings => Settings as AsteroidSettings ?? asteroidSettings;

        public AsteroidsCampaign AsteroidsCampaign => campaign as AsteroidsCampaign;

        public AsteroidsLevel Mission => Level as AsteroidsLevel;

        public int LevelCount => campaign != null ? campaign.Count : 0;

        public List<AsteroidsPlayer> AsteroidPlayers => new List<AsteroidsPlayer> { ship };

        public AsteroidsPlayer Ship => ship;

        public SpaceField Field => field;

        public ScoreKeeper Scoring => score;

        public WaveDirector Director => director;

        public MissionObjective Objective => objective;

        public AsteroidsProgress Progress => progress;

        public int Lives => lives;

        /// <summary>Whether a mission is being flown (after the countdown, before the results).</summary>
        public bool IsMissionActive => IsGameRunning && (phase == MissionPhase.Playing || phase == MissionPhase.Respawning || phase == MissionPhase.Watching);

        /// <summary>Kept from the original: where pickups were parented. Pickups now live in the playfield.</summary>
        public Transform RewardParent => field != null ? field.transform : transform;


        private AsteroidSettings CreateCustomSettings()
        {
            AsteroidSettings copy = asteroidSettings != null ? Instantiate(asteroidSettings) : ScriptableObject.CreateInstance<AsteroidSettings>();
            copy.name = $"{(asteroidSettings != null ? asteroidSettings.name : "AsteroidSettings")} (custom)";
            return copy;
        }


        #region Lifecycle

        protected override void Awake()
        {
            base.Awake();
            RegisterPlayer(ship);
            if (field != null)
            {
                field.Player = ship;
            }
            if (ship != null)
            {
                ship.Field = field;
                ship.TouchControls = ui != null ? ui.shipControls : null;
                ship.Destroyed += OnShipDestroyed;
                ship.Damaged += OnShipDamaged;
                ship.CrystalCollected += OnCrystalCollected;
                ship.LifeAwarded += OnLifeAwarded;
                ship.VolleyFired += OnVolleyFired;
                ship.PowerUpChanged += OnPowerUpChanged;
                ship.Weapons.Changed += OnWeaponChanged;
            }
            if (field != null)
            {
                field.TargetDestroyed += OnTargetDestroyed;
                field.RewardCollected += OnRewardCollected;
                field.Exploded += OnExploded;
                field.ShotLanded += OnShotLanded;
            }
            if (spawner != null)
            {
                spawner.CometIncoming += OnCometIncoming;
                spawner.HazardSpawned += OnHazardSpawned;
            }
        }


        protected override void Start()
        {
            progress = new AsteroidsProgress(Disk, Type);
            ApplySelectedHull();
            base.Start();
            if (asteroidSettings == null)
            {
                UI?.UpdateError("Asteroid settings are missing.");
            }
            else if (!asteroidSettings.AreSettingsValid(out string error))
            {
                UI?.UpdateError($"Asteroid settings error: {error}.");
            }
        }


        protected override void OnDestroy()
        {
            if (ship != null)
            {
                ship.Destroyed -= OnShipDestroyed;
                ship.Damaged -= OnShipDamaged;
                ship.CrystalCollected -= OnCrystalCollected;
                ship.LifeAwarded -= OnLifeAwarded;
                ship.VolleyFired -= OnVolleyFired;
                ship.PowerUpChanged -= OnPowerUpChanged;
                ship.Weapons.Changed -= OnWeaponChanged;
            }
            if (field != null)
            {
                field.TargetDestroyed -= OnTargetDestroyed;
                field.RewardCollected -= OnRewardCollected;
                field.Exploded -= OnExploded;
                field.ShotLanded -= OnShotLanded;
            }
            if (spawner != null)
            {
                spawner.CometIncoming -= OnCometIncoming;
                spawner.HazardSpawned -= OnHazardSpawned;
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
            StateChangedEvent?.Invoke(state.BaseState);
        }


        protected override void Update()
        {
            base.Update();
            float deltaTime = Time.deltaTime;
            if (State != null && State.Is(BaseGameState.Initialization))
            {
                // The mission select floats over a quiet asteroid field.
                field?.Tick(deltaTime);
                return;
            }
            if (!IsGameRunning || ship == null || field == null)
            {
                return;
            }
            phaseTime += deltaTime;
            switch (phase)
            {
                case MissionPhase.Briefing:
                    UpdateCountdown();
                    ship.Simulate(deltaTime);
                    field.Tick(deltaTime);
                    break;
                case MissionPhase.Playing:
                case MissionPhase.Respawning:
                case MissionPhase.Watching:
                    UpdateMission(deltaTime);
                    break;
                case MissionPhase.Victory:
                    field.WorldTimeScale = Mathf.MoveTowards(field.WorldTimeScale, 0.35f, deltaTime);
                    ship.Simulate(deltaTime, false);
                    field.Tick(deltaTime);
                    if (phaseTime > 2.6f)
                    {
                        ShowResults(true);
                    }
                    break;
                case MissionPhase.Defeat:
                    field.WorldTimeScale = Mathf.MoveTowards(field.WorldTimeScale, 0.5f, deltaTime);
                    field.Tick(deltaTime);
                    if (phaseTime > 2.4f)
                    {
                        ShowResults(false);
                    }
                    break;
            }
            RefreshHud();
        }

        #endregion


        #region Mission select

        private void EnterMenu()
        {
            phase = MissionPhase.Menu;
            director?.Stop();
            spawner?.ClearPending();
            if (field != null)
            {
                field.Clear();
                field.WorldTimeScale = 1f;
            }
            if (ship != null)
            {
                ship.gameObject.SetActive(false);
            }
            boss = null;
            int level = LevelIndex;
            if (!menuVisited)
            {
                menuVisited = true;
                level = SuggestedMission();
            }
            PreviewMission(level);
            SpawnMenuField();
            sounds?.PlayMusic(sounds.MenuMusic);
        }


        /// <summary>The first open mission without stars, or the last open one.</summary>
        private int SuggestedMission()
        {
            int suggested = 0;
            for (int i = 0; i < LevelCount; i++)
            {
                AsteroidsLevel mission = MissionAt(i);
                if (mission == null || mission.IsEndless || !IsUnlocked(i))
                {
                    continue;
                }
                suggested = i;
                if (progress == null || progress.Stars(i) == 0)
                {
                    break;
                }
            }
            return suggested;
        }


        /// <summary>Selects a mission in the mission select and shows its sector behind the menu.</summary>
        public void SelectMission(int index)
        {
            if (phase != MissionPhase.Menu || index < 0 || index >= LevelCount)
            {
                return;
            }
            sounds?.Click();
            PreviewMission(index);
        }


        private void PreviewMission(int index)
        {
            if (LevelCount == 0)
            {
                return;
            }
            LoadLevel(index);
            AsteroidsLevel mission = Mission;
            if (mission != null && backdrop != null)
            {
                backdrop.Apply(mission.ThemeForWave(1), backdrop.Theme == null);
            }
            ui?.ShowMissionSelect(BuildSummaries(), LevelIndex, TotalStars, MaxStars, BuildHangar(), DoesSaveGameExist());
        }


        private void SpawnMenuField()
        {
            if (spawner == null)
            {
                return;
            }
            AsteroidsLevel mission = Mission;
            spawner.Configure(AsteroidSettings, mission);
            WaveSpec look = mission != null && mission.Waves.Length > 0 ? mission.Waves[mission.Waves.Length - 1] : null;
            for (int i = 0; i < menuAsteroids; i++)
            {
                AsteroidKind kind = AsteroidKind.Rock;
                if (look != null && look.AsteroidCount > 0)
                {
                    int roll = Random.Range(0, look.AsteroidCount);
                    foreach (AsteroidKind candidate in (AsteroidKind[])Enum.GetValues(typeof(AsteroidKind)))
                    {
                        roll -= look.Count(candidate);
                        if (roll < 0)
                        {
                            kind = candidate;
                            break;
                        }
                    }
                }
                var size = (AsteroidSize)Random.Range(1, 3);
                Vector2 position = Playground != null
                    ? new Vector2(Random.Range(-Playground.HalfSize.x, Playground.HalfSize.x), Random.Range(-Playground.HalfSize.y, Playground.HalfSize.y))
                    : Random.insideUnitCircle * 10f;
                Asteroid asteroid = spawner.SpawnAsteroid(kind, size, position, Random.insideUnitCircle.normalized, false);
                if (asteroid != null)
                {
                    asteroid.Velocity *= 0.5f;
                }
            }
        }


        /// <summary>Starts the mission selected in the mission select.</summary>
        public void LaunchSelectedMission()
        {
            if (phase != MissionPhase.Menu || IsLobbyOpen)
            {
                return;
            }
            if (Mission == null || !IsUnlocked(LevelIndex))
            {
                ui?.UpdateError(LockReason(LevelIndex));
                sounds?.Denied();
                return;
            }
            sounds?.Click();
            PlayMission(LevelIndex);
        }


        public void RetryMission()
        {
            PlayMission(LevelIndex);
        }


        public void PlayNextMission()
        {
            int next = AsteroidsCampaign != null ? AsteroidsCampaign.NextMission(LevelIndex) : LevelIndex + 1;
            if (next >= 0 && next < LevelCount && IsUnlocked(next))
            {
                PlayMission(next);
            }
            else
            {
                if (next >= 0 && next < LevelCount)
                {
                    LevelIndex = next;
                }
                ReturnToMissionSelect();
            }
        }


        public void ReturnToMissionSelect()
        {
            TransitionState(BaseGameState.Initialization);
        }


        /// <summary>Picks a ship of the hangar if it is unlocked.</summary>
        public void SelectShip(int index)
        {
            if (index < 0 || index >= hangar.Length || hangar[index] == null)
            {
                return;
            }
            if (TotalStars < hangar[index].StarsToUnlock)
            {
                ui?.UpdateError($"{hangar[index].DisplayName} needs {hangar[index].StarsToUnlock} stars.");
                sounds?.Denied();
                return;
            }
            progress.SelectedShip = index;
            ApplySelectedHull();
            sounds?.Click();
            ui?.ShowMissionSelect(BuildSummaries(), LevelIndex, TotalStars, MaxStars, BuildHangar(), DoesSaveGameExist());
        }


        /// <summary>Forgets every star, record and the hangar choice.</summary>
        public void ResetProgress()
        {
            progress.ResetAll(LevelCount);
            ApplySelectedHull();
            menuVisited = false;
            TransitionState(BaseGameState.Initialization);
        }


        /// <summary>Leaves the game for the launcher's main menu.</summary>
        public void QuitToLauncher()
        {
            base.ExitGame();
        }


        /// <summary>The shared menu's exit leaves a mission for the mission select, and the mission select for the launcher.</summary>
        public override void ExitGame()
        {
            if (phase == MissionPhase.Menu)
            {
                base.ExitGame();
                return;
            }
            ReturnToMissionSelect();
        }


        public bool IsUnlocked(int index)
        {
            if (campaign == null)
            {
                return false;
            }
            return campaign.IsUnlocked(index, progress);
        }


        private string LockReason(int index)
        {
            AsteroidsLevel mission = MissionAt(index);
            if (mission == null)
            {
                return "No such mission.";
            }
            if (IsUnlocked(index))
            {
                return string.Empty;
            }
            AsteroidsCampaign sectors = AsteroidsCampaign;
            if (mission.IsEndless)
            {
                return "Beat the first sector's boss to unlock the endless mission.";
            }
            int required = sectors != null ? sectors.StarsRequired(index) : 0;
            if (required > TotalStars)
            {
                return $"Collect {required} stars to enter this sector ({TotalStars} so far).";
            }
            return "Complete the previous mission to unlock this one.";
        }


        private int TotalStars => campaign != null && progress != null ? campaign.TotalStars(progress) : 0;

        private int MaxStars => campaign != null ? campaign.MaxStars : 0;


        private List<MissionSummary> BuildSummaries()
        {
            var summaries = new List<MissionSummary>();
            AsteroidsCampaign sectors = AsteroidsCampaign;
            int number = 0;
            for (int i = 0; i < LevelCount; i++)
            {
                AsteroidsLevel mission = MissionAt(i);
                if (mission == null)
                {
                    continue;
                }
                if (!mission.IsEndless)
                {
                    number++;
                }
                AsteroidsCampaign.Sector sector = sectors != null ? sectors.GetSector(mission.Sector) : null;
                SectorTheme theme = mission.Theme;
                var objectiveInfo = new MissionObjective(mission.Objective, mission.ObjectiveTarget, mission.Waves.Length,
                    mission.BossPrefab != null ? mission.BossPrefab.DisplayName : null);
                summaries.Add(new MissionSummary
                {
                    Index = i,
                    Number = mission.IsEndless ? 0 : number,
                    Title = mission.Title,
                    Description = mission.Description,
                    Introduces = mission.Introduces,
                    Objective = objectiveInfo.Briefing,
                    Sector = mission.Sector,
                    SectorTitle = sector != null && !string.IsNullOrEmpty(sector.title) ? sector.title : theme != null ? theme.Title : string.Empty,
                    Accent = theme != null ? theme.Accent : Color.cyan,
                    Unlocked = IsUnlocked(i),
                    LockReason = LockReason(i),
                    Stars = progress != null ? progress.Stars(i) : 0,
                    BestScore = mission.IsEndless ? (progress != null ? progress.EndlessBestScore : 0) : (progress != null ? progress.BestScore(i) : 0),
                    ScoreGoal = mission.ScoreGoal,
                    Endless = mission.IsEndless,
                    BestWave = progress != null ? progress.EndlessBestWave : 0,
                    Boss = mission.Objective == LevelObjective.Boss
                });
            }
            return summaries;
        }


        private List<HangarShip> BuildHangar()
        {
            var ships = new List<HangarShip>();
            float maxSpeed = 1f, maxTurn = 1f, maxHull = 1f, maxRate = 1f;
            foreach (PlayerSettings hull in hangar)
            {
                if (hull == null)
                {
                    continue;
                }
                maxSpeed = Mathf.Max(maxSpeed, hull.MovementSpeed);
                maxTurn = Mathf.Max(maxTurn, hull.RotationSpeed);
                maxHull = Mathf.Max(maxHull, hull.HullMultiplier * 100f + hull.ShieldCapacity * 0.5f);
                maxRate = Mathf.Max(maxRate, hull.FireRateMultiplier);
            }
            int selected = SelectedHullIndex;
            for (int i = 0; i < hangar.Length; i++)
            {
                PlayerSettings hull = hangar[i];
                if (hull == null)
                {
                    continue;
                }
                ships.Add(new HangarShip
                {
                    Index = i,
                    Name = hull.DisplayName,
                    Description = hull.Description,
                    Unlocked = TotalStars >= hull.StarsToUnlock,
                    StarsToUnlock = hull.StarsToUnlock,
                    Selected = i == selected,
                    Speed = hull.MovementSpeed / maxSpeed,
                    Handling = hull.RotationSpeed / maxTurn,
                    Hull = (hull.HullMultiplier * 100f + hull.ShieldCapacity * 0.5f) / maxHull,
                    FireRate = hull.FireRateMultiplier / maxRate
                });
            }
            return ships;
        }


        private int SelectedHullIndex
        {
            get
            {
                int index = progress != null ? progress.SelectedShip : 0;
                if (index < 0 || index >= hangar.Length || hangar[index] == null || TotalStars < hangar[index].StarsToUnlock)
                {
                    return 0;
                }
                return index;
            }
        }


        private void ApplySelectedHull()
        {
            if (ship == null || hangar == null || hangar.Length == 0)
            {
                return;
            }
            PlayerSettings hull = hangar[SelectedHullIndex];
            if (hull != null)
            {
                ship.ApplyHull(hull);
            }
        }


        private void PlayMission(int index)
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


        private AsteroidsLevel MissionAt(int index)
        {
            return campaign != null ? campaign[index] as AsteroidsLevel : null;
        }

        #endregion


        #region Mission

        public override void LoadLevel(int level)
        {
            LevelIndex = Mathf.Clamp(level, 0, Mathf.Max(0, LevelCount - 1));
            CurrentLevel = LevelDataFor(LevelIndex);
            if (Level is AsteroidsLevel asteroidsLevel && asteroidsLevel.Settings != null)
            {
                asteroidSettings = asteroidsLevel.Settings;
                if (UsingDefaultSettings)
                {
                    Settings = asteroidSettings;
                }
            }
            UpdateLevel();
        }


        /// <summary>Resets the playfield and the ship and counts down to the first wave.</summary>
        public override void StartGame()
        {
            if (!PrepareMission())
            {
                return;
            }
            phase = MissionPhase.Briefing;
            phaseTime = 0f;
            countdown.Restart();
            TransitionState(BaseGameState.Running);
        }


        /// <summary>Everything a mission needs before the first wave. False when the scene or the level is incomplete.</summary>
        private bool PrepareMission()
        {
            AsteroidsLevel mission = Mission;
            if (mission == null || ship == null || field == null || spawner == null)
            {
                UI?.UpdateError("The Asteroids scene is missing its mission, ship or playfield.");
                return false;
            }
            AsteroidSettings settings = AsteroidSettings;
            director?.Stop();
            field.Clear();
            field.WorldTimeScale = 1f;
            FitPlayfield();
            spawner.Configure(settings, mission);
            backdrop?.Apply(mission.ThemeForWave(1), false);

            ApplySelectedHull();
            ship.ResetForMission(settings != null ? settings.HullStrength : 100f);
            lives = settings != null ? Mathf.Clamp(settings.Lives, 1, AsteroidSettings.LivesLimit) : 3;
            if (IsCoop)
            {
                // Every pilot starts at a place of their own, with the ships the room gives them.
                ship.Position = FieldMath.StartPoint(coop.Slot, coop.Pilots, CoopRules.StartRadius);
                lives = Mathf.Clamp(coop.Lives, 1, AsteroidSettings.LivesLimit);
            }
            livesLost = 0;
            score.Reset();
            ResetScore();
            lastMultiplier = 1;
            missionTime = 0f;
            boss = null;
            recordAnnounced = false;
            hazardsSeen.Clear();
            hintIndex = 0;
            hintTimer = 1.2f;

            objective = new MissionObjective(mission.Objective, mission.ObjectiveTarget, mission.Waves.Length,
                mission.BossPrefab != null ? mission.BossPrefab.DisplayName : null);
            director = new WaveDirector(mission, settings, spawner, new System.Random(Random.Range(1, int.MaxValue)));
            director.WaveStarted += OnWaveStarted;
            director.WaveCleared += OnWaveCleared;
            director.BossArrived += OnBossArrived;
            director.AllWavesCleared += OnAllWavesCleared;

            sounds?.PlayMusic(sounds.BattleMusic);
            ui?.BeginMission(mission, objective.Briefing, lives, SectorTitleOf(mission));
            return true;
        }


        private string SectorTitleOf(AsteroidsLevel mission)
        {
            AsteroidsCampaign.Sector sector = AsteroidsCampaign != null ? AsteroidsCampaign.GetSector(mission.Sector) : null;
            if (mission.IsEndless)
            {
                return "DEEP FIELD";
            }
            return sector != null && !string.IsNullOrEmpty(sector.title) ? sector.title : mission.Theme != null ? mission.Theme.Title : string.Empty;
        }


        private void UpdateCountdown()
        {
            if (!countdown.Advance(phaseTime, countdownTime, out string label))
            {
                return;
            }
            ui?.ShowCountdown(label);
            if (!countdown.IsDone)
            {
                sounds?.Countdown();
                return;
            }
            sounds?.Go();
            phase = MissionPhase.Playing;
            phaseTime = 0f;
            if (Simulates)
            {
                director.Begin(1, true);
            }
        }


        private void UpdateMission(float deltaTime)
        {
            missionTime += deltaTime;
            // A world shared with other pilots keeps its pace: no chrono field there.
            float chrono = !IsCoop && ship.IsPowerUpActive(PowerUpType.Chrono) ? chronoTimeScale : 1f;
            field.WorldTimeScale = Mathf.MoveTowards(field.WorldTimeScale, chrono, deltaTime * 2f);
            ship.Simulate(deltaTime);
            if (Simulates)
            {
                spawner.Tick(deltaTime * field.WorldTimeScale);
                director.Tick(deltaTime * field.WorldTimeScale);
            }
            field.Tick(deltaTime);
            score.Tick(deltaTime);
            if (score.Multiplier < lastMultiplier)
            {
                lastMultiplier = score.Multiplier;
            }
            if (objective.Type == LevelObjective.Survive)
            {
                objective.Survive(deltaTime);
            }
            UpdateHints(deltaTime);

            if (phase == MissionPhase.Respawning && phaseTime >= respawnDelay)
            {
                RespawnShip();
            }
            // In a shared mission the objective is the simulator's to judge, also while its own pilot only watches.
            if ((phase == MissionPhase.Playing || phase == MissionPhase.Watching) && objective.IsComplete && Simulates)
            {
                Win();
            }
            if (Mission != null && Mission.IsEndless && !recordAnnounced && progress.EndlessBestScore > 1000 && score.Score > progress.EndlessBestScore)
            {
                recordAnnounced = true;
                ui?.Toast("NEW RECORD!", new Color(1f, 0.85f, 0.25f));
                sounds?.Star(2);
            }
        }


        private void UpdateHints(float deltaTime)
        {
            AsteroidsLevel mission = Mission;
            if (mission == null || hintIndex >= mission.HintCount)
            {
                return;
            }
            hintTimer -= deltaTime;
            if (hintTimer > 0f)
            {
                return;
            }
            ui?.ShowHint(mission.Hint(hintIndex, MobilePlatform.UsesTouch));
            hintIndex++;
            hintTimer = 6.5f;
        }


        private void RefreshHud()
        {
            if (ui == null || ship == null || objective == null || phase == MissionPhase.Menu || phase == MissionPhase.Results)
            {
                return;
            }
            var hud = new HudState
            {
                Score = score.Score,
                Multiplier = score.Multiplier,
                ComboTime = score.Combo > 0 ? score.ComboTime / ScoreKeeper.ComboWindow : 0f,
                Objective = FollowsSimulator ? coopStatus : objective.Status(director != null ? director.WaveNumber : 1),
                ObjectiveProgress = FollowsSimulator ? coopProgress : objective.Progress,
                Hull = ship.MaxHealth > 0f ? Mathf.Clamp01(ship.Health / ship.MaxHealth) : 0f,
                Shield = ship.MaxShield > 0f ? Mathf.Clamp01(ship.Shield / ship.MaxShield) : 0f,
                Lives = lives,
                Weapon = ship.Weapons.Type,
                WeaponLevel = ship.Weapons.Level,
                Bombs = ship.Bombs,
                DashRecharge = ship.Simulation.DashRecharge,
                BossActive = boss != null && boss.InPlay,
                BossName = boss != null ? boss.DisplayName : string.Empty,
                BossHealth = boss != null ? boss.HealthFraction : 0f
            };
            for (int i = 0; i < PowerUps.Count; i++)
            {
                var type = (PowerUpType)i;
                ui.SetPowerUp(type, ship.PowerUpTime(type) / PowerUps.Duration(type));
            }
            ui.UpdateHud(hud);
        }


        private void Win()
        {
            phase = MissionPhase.Victory;
            phaseTime = 0f;
            director?.Stop();
            spawner?.ClearPending();
            int bonus = lives * lifeBonus;
            if (bonus > 0 && Mission != null && !Mission.IsEndless)
            {
                score.Add(bonus);
                SyncScore();
            }
            ui?.Announce(Mission != null && Mission.Objective == LevelObjective.Boss ? "SECTOR SECURED" : "MISSION COMPLETE", string.Empty, new Color(0.45f, 1f, 0.6f));
            sounds?.Victory();
            cameraRig?.Pulse(0.6f);
            ClearFieldWithFlair();
            if (IsCoop)
            {
                CoopWon();
            }
        }


        private void Lose()
        {
            if (IsCoop)
            {
                WatchOthers();
                return;
            }
            phase = MissionPhase.Defeat;
            phaseTime = 0f;
            director?.Stop();
            spawner?.ClearPending();
            bool endless = Mission != null && Mission.IsEndless;
            ui?.Announce(endless ? "SHIP LOST" : "MISSION FAILED", endless ? $"Reached wave {director?.WaveNumber ?? 0}" : string.Empty, new Color(1f, 0.35f, 0.3f));
            sounds?.GameOver();
        }


        /// <summary>Everything left in the playfield blows up (without points) as the mission ends.</summary>
        private void ClearFieldWithFlair()
        {
            var bodies = new List<SpaceBody>(field.Bodies);
            foreach (SpaceBody body in bodies)
            {
                if (!body.InPlay)
                {
                    continue;
                }
                switch (body)
                {
                    case Shootable shootable when !(shootable is Boss):
                        effects?.Explosion(shootable.Position, Mathf.Max(0.6f, shootable.Radius), new Color(0.6f, 0.9f, 1f));
                        shootable.Despawn();
                        break;
                    case Shot shot when shot.IsEnemy:
                        shot.Impact();
                        break;
                }
            }
        }


        private void ShowResults(bool victory)
        {
            if (IsCoop)
            {
                ShowCoopResults(victory);
                return;
            }
            phase = MissionPhase.Results;
            AsteroidsLevel mission = Mission;
            bool endless = mission != null && mission.IsEndless;
            bool flawless = livesLost == 0;
            bool goal = mission != null && score.Score >= mission.ScoreGoal;
            int stars = victory && !endless ? 1 + (flawless ? 1 : 0) + (goal ? 1 : 0) : 0;
            bool newBest;
            if (endless)
            {
                newBest = progress.RecordEndless(director != null ? director.WaveNumber : 0, score.Score);
            }
            else
            {
                newBest = victory && progress.RecordLevel(LevelIndex, stars, score.Score);
                if (!victory)
                {
                    newBest = false;
                }
            }
            int next = AsteroidsCampaign != null ? AsteroidsCampaign.NextMission(LevelIndex) : -1;
            var result = new MissionResult
            {
                Title = mission != null ? mission.Title : "Mission",
                Victory = victory,
                Endless = endless,
                Stars = stars,
                Flawless = flawless,
                ScoreGoalReached = goal,
                Score = score.Score,
                ScoreGoal = mission != null ? mission.ScoreGoal : 0,
                LifeBonus = victory && !endless ? lives * lifeBonus : 0,
                NewBest = newBest,
                Kills = score.Kills,
                Accuracy = score.Accuracy,
                MaxCombo = score.MaxCombo,
                Crystals = score.Crystals,
                Time = missionTime,
                Wave = director != null ? director.WaveNumber : 0,
                BestWave = progress.EndlessBestWave,
                HasNext = !endless && next >= 0,
                NextLocked = next >= 0 && !IsUnlocked(next),
                NextLockReason = next >= 0 ? LockReason(next) : string.Empty
            };
            TransitionState(victory ? BaseGameState.Victory : BaseGameState.GameOver);
            ui?.ShowResults(result);
        }

        #endregion


        #region Events

        private void OnWaveStarted(int wave, WaveSpec spec)
        {
            if (Mission == null)
            {
                return;
            }
            string title = !string.IsNullOrEmpty(spec.title) ? spec.title.ToUpperInvariant() : $"WAVE {wave}";
            string subtitle = director.WaveCount > 0 && Mission.Objective == LevelObjective.ClearWaves ? $"{wave} of {director.WaveCount}" : string.Empty;
            if (wave > 1 || !string.IsNullOrEmpty(spec.title))
            {
                ui?.Announce(title, subtitle, Mission.Theme != null ? Mission.Theme.Accent : Color.cyan);
                sounds?.WaveStart();
            }
            if (Mission.IsEndless)
            {
                SectorTheme theme = Mission.ThemeForWave(wave);
                if (backdrop != null && theme != backdrop.Theme)
                {
                    backdrop.Apply(theme, false);
                    ui?.Toast($"ENTERING {theme.Title.ToUpperInvariant()}", theme.Accent);
                }
            }
        }


        private void OnWaveCleared(int wave)
        {
            objective?.WaveCleared();
            int bonus = Mathf.RoundToInt(waveBonus * wave);
            score.Add(bonus);
            SyncScore();
            if (Mission != null && Mission.Objective == LevelObjective.ClearWaves && objective.IsComplete)
            {
                return;
            }
            ui?.Announce("WAVE CLEARED", $"+{bonus}", new Color(0.5f, 1f, 0.65f));
            sounds?.WaveClear();
        }


        private void OnAllWavesCleared()
        {
            if (Mission != null && Mission.Objective == LevelObjective.Boss)
            {
                ui?.Announce("WARNING", "Massive signature approaching", new Color(1f, 0.3f, 0.25f));
                sounds?.Warning();
            }
        }


        private void OnBossArrived(Boss arrived)
        {
            boss = arrived;
            boss.Defeated += OnBossDefeated;
            boss.PhaseChanged += OnBossPhase;
            ui?.ShowBoss(boss.DisplayName);
            sounds?.PlayMusic(sounds.BossMusic);
            sounds?.BossRoar(1f);
            cameraRig?.Shake(0.4f);
        }


        private void OnBossPhase(Boss source, int newPhase)
        {
            ui?.Toast(newPhase >= 2 ? "BOSS ENRAGED!" : "BOSS SHIELDS DOWN - IT'S ANGRY!", new Color(1f, 0.45f, 0.3f));
        }


        private void OnBossDefeated(Boss defeated)
        {
            if (defeated != boss)
            {
                return;
            }
            boss = null;
            ui?.HideBoss();
            objective?.Defeat();
            director?.BossDefeated();
            sounds?.PlayMusic(sounds.BattleMusic);
            if (Mission != null && Mission.IsEndless)
            {
                ui?.Announce("BOSS DESTROYED", string.Empty, new Color(1f, 0.85f, 0.3f));
            }
        }


        private void OnTargetDestroyed(Shootable target, DamageInfo hit)
        {
            if (!hit.ByPlayer || !IsMissionActiveOrEnding)
            {
                return;
            }
            // The kill of a pilot on another device scores on their device; what it drops is the same for everybody.
            if (!hit.Seat.HasValue)
            {
                ScoreKill(target);
            }
            if (target.Loot != null && target.LootChance > 0f && !(target is Lootable))
            {
                spawner?.DropLoot(target.Loot, target.LootChance, target.Position);
            }
        }


        private void ScoreKill(Shootable target)
        {
            int points = score.AddKill(target.Score);
            SyncScore();
            Color color = score.Multiplier >= 4 ? new Color(1f, 0.45f, 0.9f) : score.Multiplier >= 2 ? new Color(1f, 0.85f, 0.3f) : Color.white;
            effects?.Popup(target.Position, points.ToString(), color, target is Boss ? 2.2f : target.Radius > 1.2f ? 1.1f : 0.9f);
            if (score.Multiplier > lastMultiplier)
            {
                lastMultiplier = score.Multiplier;
                ui?.Toast($"COMBO x{score.Multiplier}", color);
                sounds?.Combo(score.Multiplier);
            }
        }


        private bool IsMissionActiveOrEnding => IsMissionActive;


        private void OnExploded(Blast blast)
        {
            effects?.Explosion(blast.Center, blast.Radius * 0.55f, blast.Tint);
            effects?.Shockwave(blast.Center, blast.Radius, blast.Tint);
            sounds?.Explosion(blast.Radius / 2.5f);
            cameraRig?.Shake(Mathf.Clamp(0.1f + blast.Radius * 0.05f, 0.1f, 0.5f));
        }


        private void OnShotLanded(Shot shot)
        {
            score.ShotLanded();
        }


        private void OnVolleyFired(int count)
        {
            score.ShotFired(count);
        }


        private void OnRewardCollected(Reward reward)
        {
            if (reward is PointReward)
            {
                return;
            }
            ui?.Toast(reward.Title.ToUpperInvariant(), reward.Color);
        }


        private void OnCrystalCollected(PointReward crystal)
        {
            score.AddCrystal();
            score.Add(crystal.PointsAward);
            SyncScore();
            objective?.CrystalCollected();
            effects?.Popup(crystal.Position, $"+{crystal.PointsAward}", new Color(0.5f, 0.95f, 1f), 0.75f);
        }


        private void OnLifeAwarded()
        {
            lives = Mathf.Min(AsteroidSettings.LivesLimit, lives + 1);
        }


        private void OnPowerUpChanged(PowerUpType type, bool active)
        {
            if (active)
            {
                cameraRig?.Pulse(0.3f);
            }
        }


        private void OnWeaponChanged()
        {
            if (!IsMissionActive || ship == null)
            {
                return;
            }
            ui?.Toast($"{WeaponRules.ShortTitle(ship.Weapons.Type)} LV {ship.Weapons.Level}", WeaponRules.Tint(ship.Weapons.Type));
        }


        private void OnShipDamaged(DamageInfo hit)
        {
            score.BreakCombo();
            lastMultiplier = 1;
            cameraRig?.Hurt(0.7f);
            cameraRig?.Shake(0.35f);
            ui?.DamageFlash();
        }


        private void OnShipDestroyed(AsteroidsPlayer lost)
        {
            if (!IsMissionActive)
            {
                return;
            }
            lives--;
            livesLost++;
            score.BreakCombo();
            if (lives <= 0)
            {
                Lose();
                return;
            }
            phase = MissionPhase.Respawning;
            phaseTime = 0f;
            ui?.Toast($"SHIP LOST - {lives} LEFT", new Color(1f, 0.4f, 0.35f));
        }


        private void RespawnShip()
        {
            Vector2 position = SafeSpawnPoint();
            ship.Respawn(position);
            phase = MissionPhase.Playing;
            phaseTime = 0f;
            // Give the new ship a little room.
            field.MakeRoom(position);
        }


        private Vector2 SafeSpawnPoint()
        {
            Vector2 best = Vector2.zero;
            float bestClearance = Clearance(best);
            for (int i = 0; i < 20 && bestClearance < 5f; i++)
            {
                Vector2 candidate = Playground != null
                    ? new Vector2(Random.Range(-Playground.HalfSize.x * 0.7f, Playground.HalfSize.x * 0.7f), Random.Range(-Playground.HalfSize.y * 0.7f, Playground.HalfSize.y * 0.7f))
                    : Random.insideUnitCircle * 6f;
                float clearance = Clearance(candidate);
                if (clearance > bestClearance)
                {
                    bestClearance = clearance;
                    best = candidate;
                }
            }
            return best;
        }


        private float Clearance(Vector2 point)
        {
            float clearance = float.MaxValue;
            var targets = field.Targets;
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i].IsAlive)
                {
                    clearance = Mathf.Min(clearance, (targets[i].Position - point).magnitude - targets[i].Radius);
                }
            }
            return clearance;
        }


        private void OnCometIncoming(Vector2 from, Vector2 direction, float delay)
        {
            ui?.ShowCometWarning(from, direction, delay);
        }


        private void OnHazardSpawned(HazardKind kind, SpaceBody body)
        {
            if (!hazardsSeen.Add(kind) || !IsMissionActive)
            {
                return;
            }
            switch (kind)
            {
                case HazardKind.Mine:
                    ui?.Toast("PROXIMITY MINE - shoot it from afar", new Color(0.4f, 0.85f, 1f));
                    break;
                case HazardKind.ClusterBomb:
                    ui?.Toast("CLUSTER BOMB - get clear of the ring!", new Color(1f, 0.55f, 0.2f));
                    break;
                case HazardKind.GravityWell:
                    ui?.Toast("BLACK HOLE - don't get pulled in", new Color(0.75f, 0.55f, 1f));
                    break;
                case HazardKind.Saucer:
                    ui?.Toast("HOSTILE SAUCER", new Color(1f, 0.4f, 0.4f));
                    break;
                case HazardKind.Wasp:
                    ui?.Toast("ALIEN WASPS INBOUND", new Color(0.8f, 0.5f, 1f));
                    break;
                case HazardKind.SupplyPod:
                    ui?.Toast("SUPPLY POD - shoot it open", new Color(0.45f, 1f, 0.6f));
                    break;
            }
        }


        private void SyncScore()
        {
            PlayerScore = score.Score;
            if (ship != null)
            {
                ship.Points = score.Score;
            }
            UpdateScore();
        }

        #endregion


        #region Save and load

        public override bool DoesSaveGameExist()
        {
            IStorageStrategy disk = Disk;
            return disk != null && disk.DoesKeyExist(SaveKey("Version")) && disk.GetInt(SaveKey("Version")) == SaveVersion;
        }


        /// <summary>Saves the mission in flight: the wave, the score and lives, the ship and every asteroid.</summary>
        public override void SaveGame()
        {
            IStorageStrategy disk = Disk;
            if (InSession)
            {
                UI?.UpdateError("A mission flown with other pilots cannot be saved.");
                return;
            }
            if (disk == null || !(State.Is(BaseGameState.Running) || State.Is(BaseGameState.Paused)) ||
                !(phase == MissionPhase.Playing || phase == MissionPhase.Respawning) || director == null)
            {
                UI?.UpdateError("There is no mission in flight to save.");
                return;
            }
            disk.SetInt(SaveKey("Version"), SaveVersion);
            disk.SetInt(SaveKey("Level"), LevelIndex);
            disk.SetInt(SaveKey("Wave"), Mathf.Max(1, director.WaveNumber));
            disk.SetBool(SaveKey("BossStage"), director.State == WaveDirector.Stage.Boss);
            disk.SetInt(SaveKey("Score"), score.Score);
            disk.SetInt(SaveKey("Lives"), Mathf.Max(1, lives));
            disk.SetInt(SaveKey("LivesLost"), livesLost);
            disk.SetFloat(SaveKey("Time"), missionTime);
            disk.SetInt(SaveKey("WavesCleared"), objective.WavesCleared);
            disk.SetInt(SaveKey("Crystals"), objective.Crystals);
            disk.SetFloat(SaveKey("Survived"), objective.Survived);
            disk.SetFloat(SaveKey("Hull"), ship.IsAlive ? ship.Health : ship.MaxHealth);
            disk.SetFloat(SaveKey("Shield"), ship.Shield);
            disk.SetInt(SaveKey("Weapon"), (int)ship.Weapons.Type);
            disk.SetInt(SaveKey("WeaponLevel"), ship.Weapons.Level);
            disk.SetInt(SaveKey("Bombs"), ship.Bombs);
            disk.SetFloat(SaveKey("ShipX"), ship.Position.x);
            disk.SetFloat(SaveKey("ShipY"), ship.Position.y);
            disk.SetFloat(SaveKey("ShipAngle"), ship.transform.eulerAngles.z);
            var asteroids = new List<Asteroid>();
            foreach (Shootable target in field.Targets)
            {
                if (target is Asteroid asteroid && asteroid.IsAlive)
                {
                    asteroids.Add(asteroid);
                }
            }
            disk.SetInt(SaveKey("Asteroids"), asteroids.Count);
            for (int i = 0; i < asteroids.Count; i++)
            {
                Asteroid asteroid = asteroids[i];
                string key = SaveKey($"A{i}.");
                disk.SetInt(key + "Kind", (int)asteroid.Kind);
                disk.SetInt(key + "Size", (int)asteroid.Size);
                disk.SetFloat(key + "X", asteroid.Position.x);
                disk.SetFloat(key + "Y", asteroid.Position.y);
                disk.SetFloat(key + "VX", asteroid.Velocity.x);
                disk.SetFloat(key + "VY", asteroid.Velocity.y);
                disk.SetFloat(key + "Health", asteroid.Health);
                disk.SetBool(key + "Wave", asteroid.CountsForWave);
            }
            if (!PersistSavedGame())
            {
                return;
            }
            UI?.EnableLoad();
            ui?.Toast("MISSION SAVED", new Color(0.5f, 0.9f, 1f));
        }


        /// <summary>Continues the saved mission where it was saved.</summary>
        public override void LoadGame()
        {
            IStorageStrategy disk = Disk;
            if (disk == null || !DoesSaveGameExist())
            {
                UI?.UpdateError("There is no saved game to load.");
                return;
            }
            int level = disk.GetInt(SaveKey("Level"));
            if (level < 0 || level >= LevelCount)
            {
                UI?.UpdateError("The saved mission no longer exists.");
                return;
            }
            LoadLevel(level);
            if (!PrepareMission())
            {
                return;
            }
            score.Reset(disk.GetInt(SaveKey("Score")));
            SyncScore();
            lives = Mathf.Clamp(disk.GetInt(SaveKey("Lives")), 1, AsteroidSettings.LivesLimit);
            livesLost = disk.GetInt(SaveKey("LivesLost"));
            missionTime = disk.GetFloat(SaveKey("Time"));
            objective.Restore(disk.GetInt(SaveKey("WavesCleared")), disk.GetInt(SaveKey("Crystals")), disk.GetFloat(SaveKey("Survived")));
            ship.Weapons.Set((WeaponType)disk.GetInt(SaveKey("Weapon")), disk.GetInt(SaveKey("WeaponLevel")));
            ship.Health = Mathf.Max(1f, disk.GetFloat(SaveKey("Hull")));
            ship.RestoreShield(disk.GetFloat(SaveKey("Shield")) - ship.Shield);
            for (int b = ship.Bombs; b < disk.GetInt(SaveKey("Bombs")); b++)
            {
                ship.AddBomb();
            }
            ship.Position = new Vector2(disk.GetFloat(SaveKey("ShipX")), disk.GetFloat(SaveKey("ShipY")));
            ship.transform.rotation = Quaternion.Euler(0f, 0f, disk.GetFloat(SaveKey("ShipAngle")));
            int count = disk.GetInt(SaveKey("Asteroids"));
            for (int i = 0; i < count; i++)
            {
                string key = SaveKey($"A{i}.");
                var kind = (AsteroidKind)disk.GetInt(key + "Kind");
                var size = (AsteroidSize)disk.GetInt(key + "Size");
                var position = new Vector2(disk.GetFloat(key + "X"), disk.GetFloat(key + "Y"));
                Asteroid asteroid = spawner.SpawnAsteroid(kind, size, position, Vector2.up, disk.GetBool(key + "Wave"));
                if (asteroid != null)
                {
                    asteroid.Velocity = new Vector2(disk.GetFloat(key + "VX"), disk.GetFloat(key + "VY"));
                    asteroid.Health = Mathf.Min(asteroid.MaxHealth, disk.GetFloat(key + "Health"));
                }
            }
            director.Resume(disk.GetInt(SaveKey("Wave")), disk.GetBool(SaveKey("BossStage")));
            phase = MissionPhase.Playing;
            phaseTime = 0f;
            TransitionState(BaseGameState.Running);
            ui?.ShowCountdown("GO!");
            ui?.Toast("MISSION RESUMED", new Color(0.5f, 0.9f, 1f));
        }

        #endregion
    }
}
