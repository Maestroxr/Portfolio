using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Gamebox;
using Gamebox.UI;
using UnityEngine;
using static Portfolio.MemoryCards.MemoryCardsText;

namespace Portfolio.MemoryCards
{
    /// <summary>
    /// Memory Cards game module. Runs the level select, deals the board, shows it for the memorize phase, passes every
    /// card click to the <see cref="MemoryRound"/> rules engine and animates what it answers (matches, mistakes, special
    /// cards, shuffles), chains the boards of the endless run, keeps the campaign progress and saves a game in progress.
    /// Menu, pause and settings come from <see cref="BaseGameManager"/>. Games for several players, at one device or
    /// online, are in MemoryCardsGameManager.Versus.cs.
    /// </summary>
    public partial class MemoryCardsGameManager : BaseGameManager
    {
        private enum Phase
        {
            Menu,
            Dealing,
            Preview,
            Playing,
            Busy,
            Finished
        }

        private const int SaveVersion = 2;

        #region Field Members
        /// The reason for using the concrete classes which implement the IStorage and ICache interfaces is
        /// ease of use for assignability in the Unity editor.
        [SerializeField] private FlippableCache CardsCacheBehaviour;
        [SerializeField] private MemoryCardsSettings defaultSettings, customSettings, currentGameSettings;
        [SerializeField] private MemoryCardsUI gameUI;
        [SerializeField] private MemoryCardsController controller;
        [SerializeField] private MemoryCardsCampaign campaign;
        [SerializeField] private MemoryCardsSettings settings;
        [SerializeField] internal MemoryCardsPlayer player;
        [SerializeField] internal CardBoard board;
        [SerializeField] internal AmbientBackdrop backdrop;
        [SerializeField] internal UIParticles particles;
        [SerializeField] internal CardsAudio sounds;
        [Tooltip("Faces of the special cards in CardKind order: wild, bomb, clock, peek.")]
        [SerializeField] internal Sprite[] specialFaces = new Sprite[4];
        [SerializeField] internal Sprite defaultCardBack;

        [Header("Timing")]
        [SerializeField] internal float dealStagger = 0.035f;
        [SerializeField] internal float dealDuration = 0.42f;
        [SerializeField] internal float flipTime = 0.26f;
        [SerializeField] internal float shuffleDuration = 0.7f;
        [SerializeField] internal float previewStagger = 0.012f;

        [Header("Colours")]
        [SerializeField] internal Color goodColor = new Color(0.35f, 0.85f, 0.4f);
        [SerializeField] internal Color badColor = new Color(1f, 0.38f, 0.32f);
        [SerializeField] internal Color gold = new Color(1f, 0.84f, 0.25f);
        #endregion

        private ICache<Flippable> cardsCache;
        private MemoryCardsProgress progress;
        private readonly List<Flippable> views = new List<Flippable>();
        private readonly HashSet<Flippable> listening = new HashSet<Flippable>();
        private readonly Dictionary<int, LevelData> levelData = new Dictionary<int, LevelData>();
        private readonly System.Random random = new System.Random();
        private MemoryRound round;
        private RoundRules rules;
        private IReadOnlyList<Sprite> animalPool;
        private CardWorld world;
        private MemoryCardsTheme look;
        private Phase phase = Phase.Menu;
        private int selectedWorld;
        private int selectedLevel;
        private bool menuVisited;
        private float unflipTimer = -1f;
        private bool pendingShuffle;
        private float shake;
        private Vector2 boardRest;
        private int lastTick = -1;
        private bool freePlayCustom;
        private int endlessBoard;
        private int runSets;
        private int runBestCombo;
        private float runTime;

        public override IGameController Controller => controller;
        public override IGameUI UI => gameUI;
        public override ICampaign Campaign => campaign;
        public override IGameSettings DefaultSettings => defaultSettings;
        public override IGameSettings CustomSettings => customSettings;

        public override IGameSettings Settings
        {
            get => settings != null ? settings : defaultSettings;
            set => settings = value as MemoryCardsSettings;
        }

        /// <summary>The clock is part of the round (it counts down or up); the base timer is not used.</summary>
        protected override bool UsesTimer => false;

        /// <summary>The settings free play uses: the default or the custom rules.</summary>
        public MemoryCardsSettings ActiveSettings => Settings as MemoryCardsSettings;

        public MemoryCardsPlayer Player => player;

        public MemoryCardsLevel CardsLevel => Level as MemoryCardsLevel;

        public int LevelCount => campaign != null ? campaign.Count : 0;

        public MemoryCardsProgress Progress => progress;

        internal MemoryRound Round => round;

        /// <summary>The theme the game shows, or null when it has none.</summary>
        internal MemoryCardsTheme Look => look;

        /// <summary>The deck the boards are dealt from and saved games refer to: the theme's, else the campaign's.</summary>
        private IReadOnlyList<Sprite> Deck => campaign != null ? campaign.Deck(look) : null;

        /// <summary>The back of the cards of a level without a world.</summary>
        private Sprite DefaultBack => look != null && look.Card.defaultBack != null ? look.Card.defaultBack : defaultCardBack;

        /// <summary>The back of the cards of the current world.</summary>
        private Sprite CardBack => world != null && world.cardBack != null ? world.cardBack : DefaultBack;

        private Color Good => look != null ? look.Colors.good : goodColor;

        private Color Bad => look != null ? look.Colors.bad : badColor;

        private Color Gold => look != null ? look.Colors.gold : gold;

        private Color Light => look != null ? look.Colors.light : Color.white;

        /// <summary>What the theme calls the faces of the deck (and one of them).</summary>
        private string FacesWord => look != null && !string.IsNullOrEmpty(look.Say.faces) ? look.Say.faces : "animals";

        private string FaceWord => look != null && !string.IsNullOrEmpty(look.Say.face) ? look.Say.face : "animal";

        /// <summary>The title of the level at <paramref name="index"/> in the words of the theme.</summary>
        private string TitleOf(int index)
        {
            MemoryCardsLevel level = campaign != null ? campaign.Level(index) : null;
            string own = level != null ? level.Title : string.Empty;
            return T(look != null ? look.Say.Title(index, own) : own);
        }

        private string DescriptionOf(int index)
        {
            MemoryCardsLevel level = campaign != null ? campaign.Level(index) : null;
            string own = level != null ? level.Description : string.Empty;
            return T(look != null ? look.Say.Description(index, own) : own);
        }

        private string TipOf(int index)
        {
            MemoryCardsLevel level = campaign != null ? campaign.Level(index) : null;
            string own = level != null ? level.Tip : string.Empty;
            return T(look != null ? look.Say.Tip(index, own) : own);
        }

        /// <summary>The title of the level played, numbered in the campaign, in the language shown.</summary>
        private string RoundTitle()
        {
            MemoryCardsLevel level = CardsLevel;
            return level != null && level.IsCampaign ? $"{CampaignNumber(LevelIndex)}. {TitleOf(LevelIndex)}" : TitleOf(LevelIndex);
        }

        /// <summary>Seconds won or lost, for a popup: "+2s", "-3s".</summary>
        private static string Seconds(float seconds, bool gained)
        {
            string amount = Mathf.Abs(seconds).ToString("0.#", CultureInfo.InvariantCulture);
            return gained ? F("+{0}s", amount) : F("-{0}s", amount);
        }

        /// <summary>
        /// The language changed: the level select is drawn again, or the title of the round in play (the banners and tips
        /// that show pass by themselves).
        /// </summary>
        protected override void OnLanguageChanged()
        {
            if (phase == Phase.Menu)
            {
                RefreshLevelSelect();
            }
            else if (round != null && CardsLevel != null)
            {
                gameUI?.RetitleRound(RoundTitle(), versus != null ? VersusLabel() : null);
            }
        }

        /// <summary>Whether cards can be clicked right now.</summary>
        internal bool IsPlaying => IsGameRunning && phase == Phase.Playing && round != null && !round.IsOver;

        /// <summary>The step of the round the game is in (menu, dealing, preview, playing, busy, finished).</summary>
        internal string CurrentPhase => phase.ToString();

        internal MemoryCardsUI CardsUI => gameUI;

        internal Flippable ViewOf(MemoryCard card)
        {
            return card != null && card.Id >= 0 && card.Id < views.Count ? views[card.Id] : null;
        }


        protected override void Awake()
        {
            base.Awake();
            gameUI?.ClearError();
            if (null == StorageBehaviour || null == CardsCacheBehaviour)
            {
                string error = "Storage or cache reference is missing.";
                gameUI?.UpdateError(error);
                throw new ArgumentNullException(error);
            }
            cardsCache = CardsCacheBehaviour;
            RegisterPlayer(player);
            if (board != null)
            {
                boardRest = board.Rect.anchoredPosition;
            }
        }


        protected override void Start()
        {
            progress = new MemoryCardsProgress(Disk, Type);
            look = ThemeAs<MemoryCardsTheme>();
            ApplyLook();
            base.Start();
        }


        /// <summary>
        /// The theme changed: the interface, the particles and the backdrop take the new look at once, and so does the
        /// level select. A board in play keeps its faces until the next deal; only the backs and frames of its cards change.
        /// </summary>
        protected override void OnThemeChanged(GameTheme theme)
        {
            look = theme as MemoryCardsTheme;
            ApplyLook();
            if (phase == Phase.Menu)
            {
                ApplyWorld(selectedWorld, false);
                RefreshLevelSelect();
            }
            else if (CardsLevel != null)
            {
                ApplyWorld(CardsLevel.World, false);
                Sprite back = CardBack;
                foreach (Flippable view in views)
                {
                    view.ApplyLook(look);
                    view.SetBack(back);
                }
            }
        }


        /// <summary>Gives the interface, the particles and the camera the sprites and colours of the theme.</summary>
        private void ApplyLook()
        {
            gameUI?.ApplyTheme(look);
            particles?.ApplyLook(look != null ? look.Particles : null);
            if (look != null && Camera.main != null)
            {
                Camera.main.backgroundColor = look.Colors.sky;
            }
        }


        /// <summary>The world at <paramref name="index"/> in the look of the theme.</summary>
        private CardWorld WorldOf(int index)
        {
            return campaign != null ? campaign.WorldOf(index, look) : null;
        }


        /// <summary>The face of a special card: the theme's, else the one the scene builder set.</summary>
        private Sprite SpecialFace(CardKind kind)
        {
            Sprite themed = look != null ? look.Special.Of(kind) : null;
            if (themed != null)
            {
                return themed;
            }
            int special = (int)kind - 1;
            return specialFaces != null && special >= 0 && special < specialFaces.Length ? specialFaces[special] : null;
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
            UpdateShake();
            if (!IsGameRunning || round == null)
            {
                return;
            }
            float deltaTime = Time.deltaTime;
            if (unflipTimer >= 0f)
            {
                unflipTimer -= deltaTime;
                if (unflipTimer < 0f)
                {
                    HideMismatch();
                }
            }
            if (phase == Phase.Playing && !round.IsOver)
            {
                round.Tick(deltaTime);
                if (round.HasCountdown)
                {
                    int second = Mathf.CeilToInt(round.Clock);
                    if (second <= 5 && second > 0 && second != lastTick)
                    {
                        lastTick = second;
                        sounds?.Play(sounds.tick, 0.7f, 1.1f);
                    }
                }
                if (round.IsOver)
                {
                    Finish();
                }
            }
            UpdateVersus(deltaTime);
            RefreshHud();
        }


        #region Level select

        private void EnterMenu()
        {
            StopFlow();
            phase = Phase.Menu;
            ClearBoard();
            ClearVersus();
            particles?.Clear();
            round = null;
            unflipTimer = -1f;
            pendingShuffle = false;
            if (campaign == null || LevelCount == 0)
            {
                UI?.UpdateError(T("The Memory Cards campaign has no levels."));
                return;
            }
            bool first = !menuVisited;
            if (first)
            {
                menuVisited = true;
                selectedWorld = Mathf.Clamp(progress.SelectedWorld, 0, Mathf.Max(0, campaign.WorldCount - 1));
                selectedLevel = SuggestedLevel(selectedWorld);
            }
            else
            {
                selectedLevel = Mathf.Clamp(LevelIndex, 0, LevelCount - 1);
                selectedWorld = campaign.Level(selectedLevel) != null ? campaign.Level(selectedLevel).World : selectedWorld;
            }
            ApplyWorld(selectedWorld, first);
            sounds?.PlayMusic(sounds.menuMusic);
            RefreshLevelSelect();
        }


        /// <summary>The first open level of <paramref name="worldIndex"/> without stars, else its last open level.</summary>
        private int SuggestedLevel(int worldIndex)
        {
            List<int> levels = campaign.LevelsOf(worldIndex);
            if (levels.Count == 0)
            {
                return 0;
            }
            int suggested = levels[0];
            foreach (int index in levels)
            {
                MemoryCardsLevel level = campaign.Level(index);
                if (!campaign.IsUnlocked(index, progress))
                {
                    continue;
                }
                suggested = index;
                if (level.IsCampaign && progress.Stars(index) == 0)
                {
                    break;
                }
            }
            return suggested;
        }


        public void SelectWorld(int worldIndex)
        {
            if (phase != Phase.Menu || campaign == null || worldIndex < 0 || worldIndex >= campaign.WorldCount)
            {
                return;
            }
            bool open = campaign.IsWorldUnlocked(worldIndex, progress);
            sounds?.Play(open ? sounds.select : sounds.locked, 0.8f);
            selectedWorld = worldIndex;
            progress.SelectedWorld = worldIndex;
            selectedLevel = SuggestedLevel(worldIndex);
            ApplyWorld(worldIndex, false);
            RefreshLevelSelect();
        }


        public void SelectLevel(int index)
        {
            if (phase != Phase.Menu || index < 0 || index >= LevelCount)
            {
                return;
            }
            bool open = campaign.IsUnlocked(index, progress);
            sounds?.Play(open ? sounds.select : sounds.locked, 0.7f, open ? 1f : 0.9f);
            selectedLevel = index;
            RefreshLevelSelect();
        }


        public void PlaySelectedLevel()
        {
            if (phase != Phase.Menu)
            {
                return;
            }
            if (!campaign.IsUnlocked(selectedLevel, progress))
            {
                sounds?.Play(sounds.locked);
                UI?.UpdateError(LockReason(selectedLevel));
                return;
            }
            PlayClick();
            PlayLevel(selectedLevel);
        }


        public void RetryLevel()
        {
            PlayClick();
            PlayLevel(LevelIndex);
        }


        public void PlayNextLevel()
        {
            PlayClick();
            int next = campaign.NextLevel(LevelIndex);
            if (next >= 0 && campaign.IsUnlocked(next, progress))
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
            PlayClick();
            TransitionState(BaseGameState.Initialization);
        }


        /// <summary>Forgets every star and record (the level select asks twice first).</summary>
        public void ResetProgress()
        {
            progress.ResetAll(LevelCount);
            menuVisited = false;
            sounds?.Play(sounds.back);
            TransitionState(BaseGameState.Initialization);
        }


        public void PlayClick()
        {
            sounds?.Play(sounds.click, 0.8f);
        }


        /// <summary>A star of the results pops in at <paramref name="worldPosition"/>.</summary>
        public void PlayStar(int index, Vector3 worldPosition)
        {
            sounds?.Play(sounds.star, 0.9f, 1f + index * 0.12f);
            if (particles != null)
            {
                particles.Burst(particles.ToLocal(worldPosition), 14, particles.star, new[] { Gold, Color.white }, 520f, 34f, 0.7f, -200f);
            }
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


        /// <summary>Shows the level select for the selected world and level.</summary>
        internal void RefreshLevelSelect()
        {
            if (gameUI == null || campaign == null || phase != Phase.Menu)
            {
                return;
            }
            var worlds = new List<WorldSummary>();
            for (int w = 0; w < campaign.WorldCount; w++)
            {
                MemoryCardsCampaign.World entry = campaign.GetWorld(w);
                CardWorld theme = WorldOf(w);
                int stars = 0;
                int maxStars = 0;
                foreach (int index in campaign.LevelsOf(w))
                {
                    if (campaign.IsCampaignLevel(index))
                    {
                        stars += progress.Stars(index);
                        maxStars += campaign.MaxStarsPerLevel;
                    }
                }
                worlds.Add(new WorldSummary
                {
                    Index = w,
                    Title = T(theme != null ? theme.displayName : entry.title),
                    Tagline = theme != null ? T(theme.tagline) : string.Empty,
                    Unlocked = campaign.IsWorldUnlocked(w, progress),
                    StarsRequired = entry.starsRequired,
                    Stars = stars,
                    MaxStars = maxStars,
                    Mascot = theme != null ? theme.mascot : null,
                    Accent = theme != null ? theme.accent : Color.white,
                    AccentDark = theme != null ? theme.accentDark : Color.gray
                });
            }
            var levels = new List<LevelSummary>();
            foreach (int index in campaign.LevelsOf(selectedWorld))
            {
                levels.Add(Summarize(index));
            }
            gameUI.ShowLevelSelect(worlds, selectedWorld, levels, selectedLevel, campaign.TotalStars(progress), campaign.MaxStars,
                progress.SetsFound, DoesSaveGameExist());
            RefreshPlayers();
        }


        private LevelSummary Summarize(int index)
        {
            MemoryCardsLevel level = campaign.Level(index);
            CardWorld theme = WorldOf(level.World);
            RoundRules levelRules = level.IsFreePlay ? ActiveSettings?.ToRules() : level.IsEndless ? null : level.Settings?.ToRules();
            var summary = new LevelSummary
            {
                Index = index,
                Number = CampaignNumber(index),
                Title = TitleOf(index),
                Description = DescriptionOf(index),
                Introduces = T(level.Introduces),
                Kind = level.Kind,
                Mode = level.IsFreePlay ? LevelMode.FreePlay : level.Mode,
                Twists = levelRules != null ? string.Join(", ", MemoryCardsLevel.Twists(levelRules).ConvertAll(twist => T(twist))) : T("More twists every board"),
                Board = levelRules != null ? BoardText(levelRules) : T("Growing boards"),
                Unlocked = campaign.IsUnlocked(index, progress),
                Stars = level.IsCampaign ? progress.Stars(index) : 0,
                BestScore = progress.BestScore(index),
                EndlessBoards = progress.EndlessBestBoards,
                EndlessScore = progress.EndlessBestScore,
                CardBack = theme != null ? (theme.levelCardBack != null ? theme.levelCardBack : theme.cardBack) : DefaultBack,
                Accent = theme != null ? theme.accent : Color.white,
                AccentDark = theme != null ? theme.accentDark : Color.gray,
                World = theme != null ? T(theme.displayName) : string.Empty
            };
            summary.LockReason = summary.Unlocked ? string.Empty : LockReason(index);
            if (level.IsCampaign)
            {
                bool countdown = levelRules != null && levelRules.HasTimeLimit;
                summary.Goals = Goals(level.Goals, countdown);
            }
            else if (level.IsEndless)
            {
                summary.Goals = new[] { T("Clear board after board"), T("Every cleared board adds time"), T("Twists join as you go") };
            }
            else
            {
                summary.Goals = new[] { UsingDefaultSettings ? T("Playing the default rules") : T("Playing your custom rules") };
            }
            return summary;
        }


        private static string BoardText(RoundRules boardRules)
        {
            switch (boardRules.MatchSize)
            {
                case 2:
                    return F("{0}x{1} board, {2} pairs", boardRules.Columns, boardRules.Rows, boardRules.Sets);
                case 3:
                    return F("{0}x{1} board, {2} triplets", boardRules.Columns, boardRules.Rows, boardRules.Sets);
                default:
                    return F("{0}x{1} board, {2} sets of {3}", boardRules.Columns, boardRules.Rows, boardRules.Sets, boardRules.MatchSize);
            }
        }


        private int CampaignNumber(int index)
        {
            int number = 0;
            for (int i = 0; i <= index && i < LevelCount; i++)
            {
                if (campaign.IsCampaignLevel(i))
                {
                    number++;
                }
            }
            return campaign.IsCampaignLevel(index) ? number : 0;
        }


        private string LockReason(int index)
        {
            MemoryCardsLevel level = campaign.Level(index);
            if (level == null)
            {
                return string.Empty;
            }
            if (level.IsEndless)
            {
                return F("Complete {0} levels to open the endless run.", campaign.endlessAfter);
            }
            int needed = campaign.StarsRequired(index);
            int stars = campaign.TotalStars(progress);
            if (stars < needed)
            {
                CardWorld theme = WorldOf(level.World);
                return F("Collect {0} stars to open {1} ({2} so far).", needed, theme != null ? T(theme.displayName) : T("this world"), stars);
            }
            return T("Clear the level before to unlock this one.");
        }


        private void ApplyWorld(int worldIndex, bool instant)
        {
            CardWorld theme = WorldOf(worldIndex);
            if (theme == null && campaign != null)
            {
                theme = WorldOf(0);
            }
            world = theme;
            backdrop?.Apply(theme, instant);
        }

        #endregion


        #region Round

        public override void LoadLevel(int level)
        {
            LevelIndex = Mathf.Clamp(level, 0, Mathf.Max(0, LevelCount - 1));
            CurrentLevel = LevelDataFor(LevelIndex);
            UpdateLevel();
        }


        /// <summary>Starts the loaded level: resolves its rules, deals the board and counts in.</summary>
        public override void StartGame()
        {
            MemoryCardsLevel level = CardsLevel;
            if (level == null)
            {
                UI?.UpdateError(T("The Memory Cards campaign has no level to play."));
                return;
            }
            SetUpVersus(level);
            if (!PrepareRules(level, out string error))
            {
                ClearVersus();
                UI?.UpdateError(F("Cannot start the game: {0}", RuleMessage(error)));
                return;
            }
            StopFlow();
            ClearBoard();
            particles?.Clear();
            unflipTimer = -1f;
            pendingShuffle = false;
            endlessBoard = level.IsEndless ? 1 : 0;
            runSets = 0;
            runBestCombo = 0;
            runTime = 0f;
            player?.ResetMatches();
            ApplyWorld(level.World, false);
            gameUI?.Curtain(world != null ? world.skyTop : Color.white);
            TransitionState(BaseGameState.Running);
            sounds?.PlayMusic(sounds.playMusic);
            BeginBoard(IsOnlineVersus ? MirrorDeal() : DealBoard(), 0, 0);
        }


        private bool PrepareRules(MemoryCardsLevel level, out string error)
        {
            freePlayCustom = false;
            if (level.IsEndless)
            {
                rules = EndlessRules.ForBoard(1);
                animalPool = Deck;
            }
            else if (level.IsFreePlay)
            {
                MemoryCardsSettings source = defaultSettings;
                if (!UsingDefaultSettings && customSettings != null && currentGameSettings != null)
                {
                    try
                    {
                        currentGameSettings.CopySettings(customSettings);
                        UI?.SettingsUI?.CopyToSettings(currentGameSettings);
                    }
                    catch (FormatException formatException)
                    {
                        error = formatException.Message;
                        return false;
                    }
                    source = currentGameSettings;
                    freePlayCustom = true;
                }
                if (source == null)
                {
                    error = T("free play has no settings");
                    return false;
                }
                settings = source;
                rules = source.ToRules();
                animalPool = campaign.AnimalPool(level, source, look);
            }
            else
            {
                if (level.Settings == null)
                {
                    error = F("level {0} has no settings", T(level.Title));
                    return false;
                }
                rules = level.Settings.ToRules();
                animalPool = campaign.AnimalPool(level, null, look);
            }
            if (versus != null)
            {
                rules = VersusRules(rules);
            }
            if (IsOnlineVersus)
            {
                // The server dealt the board already; its rules are the ones that count.
                error = "OK";
                return true;
            }
            return rules.IsValid(animalPool.Count, out error);
        }


        private Deal DealBoard()
        {
            return Dealer.Create(rules, animalPool.Count, random);
        }


        /// <summary>Lays out a dealt board and plays it in: deal, memorize, go.</summary>
        private void BeginBoard(Deal deal, int score, int combo)
        {
            round = new MemoryRound(rules, deal, score, combo);
            lastTick = -1;
            SpawnViews(deal);
            MemoryCardsLevel level = CardsLevel;
            var setup = new RoundHud
            {
                Title = RoundTitle(),
                Mode = level.IsFreePlay ? LevelMode.FreePlay : MemoryCardsLevel.ModeOf(level.Kind, rules),
                Countdown = rules.HasTimeLimit,
                Hearts = rules.Hearts,
                MoveLimit = rules.MoveLimit,
                Sets = round.Sets,
                MatchSize = rules.MatchSize,
                Parade = rules.Parade,
                Endless = level.IsEndless,
                Board = endlessBoard,
                Accent = world != null ? world.accent : Color.white
            };
            gameUI?.BeginRound(versus != null ? VersusHudSetup(setup) : setup);
            if (versus != null)
            {
                gameUI?.ShowVersus(versus.Seats, localSeat);
            }
            RefreshHud();
            RefreshParade();
            StartFlow(DealRoutine(level));
        }


        private void SpawnViews(Deal deal)
        {
            ClearBoard();
            if (board != null)
            {
                board.Layout(rules.Columns, rules.Rows);
            }
            Sprite back = CardBack;
            foreach (Flippable view in cardsCache.Deploy(deal.Cards.Count))
            {
                views.Add(view);
            }
            for (int i = 0; i < deal.Cards.Count; i++)
            {
                MemoryCard card = deal.Cards[i];
                Flippable view = views[i];
                if (board != null)
                {
                    view.transform.SetParent(board.Rect, false);
                }
                view.ApplyLook(look);
                view.Setup(i, back, FaceOf(card, deal), card.Frozen, board != null ? board.CardSize : new Vector2(150f, 200f), SlotPosition(card.Slot));
                view.Interactable = false;
                if (listening.Add(view))
                {
                    view.Clicked += OnCardClicked;
                }
            }
        }


        private Vector2 SlotPosition(int slot)
        {
            return board != null ? board.SlotPosition(slot) : Vector2.zero;
        }


        private Sprite FaceOf(MemoryCard card, Deal deal)
        {
            if (card.Kind == CardKind.Animal)
            {
                // An online board is dealt by the server: a face is unknown until its card turns.
                if (card.Animal < 0 || card.Animal >= deal.Animals.Count)
                {
                    return null;
                }
                int pool = deal.Animals[card.Animal];
                return pool >= 0 && pool < animalPool.Count ? animalPool[pool] : null;
            }
            return SpecialFace(card.Kind);
        }


        private Sprite AnimalSprite(int roundAnimal)
        {
            if (round == null || roundAnimal < 0 || roundAnimal >= round.Animals.Count)
            {
                return null;
            }
            int pool = round.Animals[roundAnimal];
            return pool >= 0 && pool < animalPool.Count ? animalPool[pool] : null;
        }


        private IEnumerator DealRoutine(MemoryCardsLevel level)
        {
            phase = Phase.Dealing;
            SetCardsInteractable(false);
            sounds?.Play(sounds.shuffle, 0.8f);
            Vector2 deck = board != null ? board.DeckPosition : Vector2.down * 600f;
            for (int i = 0; i < views.Count; i++)
            {
                views[i].DealFrom(deck, 0.15f + i * dealStagger, dealDuration);
            }
            float total = 0.15f + (views.Count - 1) * dealStagger + dealDuration;
            float time = 0f;
            int played = 0;
            while (time < total)
            {
                time += Time.deltaTime;
                while (played < views.Count && time >= 0.15f + played * dealStagger)
                {
                    if (played % 3 == 0)
                    {
                        sounds?.PlayAny(sounds.deals, 0.45f);
                    }
                    played++;
                }
                yield return null;
            }

            if (rules.PreviewTime > 0f)
            {
                phase = Phase.Preview;
                // The banner shows before the cards turn, so it never hides a card that is face up.
                gameUI?.ShowBanner(T("MEMORIZE!"), F("Remember where the {0} are", T(FacesWord)), Light, 1f);
                yield return new WaitForSeconds(0.9f);
                if (onlineBoard != null && onlineBoard.Preview != null)
                {
                    // The server shows the board to everybody once, for this.
                    foreach (OnlineCard face in onlineBoard.Preview)
                    {
                        Learn(face);
                    }
                }
                sounds?.Play(sounds.fan, 0.8f);
                for (int i = 0; i < views.Count; i++)
                {
                    views[i].FlipUp(i * previewStagger);
                }
                float shown = 0f;
                while (shown < rules.PreviewTime)
                {
                    shown += Time.deltaTime;
                    gameUI?.ShowMemorize(1f - shown / rules.PreviewTime);
                    yield return null;
                }
                gameUI?.HideMemorize();
                sounds?.Play(sounds.fan, 0.6f, 0.9f);
                for (int i = 0; i < views.Count; i++)
                {
                    views[i].FlipDown(i * previewStagger);
                }
                yield return new WaitForSeconds(flipTime + views.Count * previewStagger);
            }

            string title = level.IsEndless ? F("BOARD {0}", endlessBoard) : T("GO!");
            string sub = level.IsEndless ? null : level.IsCampaign && !string.IsNullOrEmpty(level.Introduces) ? F("New: {0}", T(level.Introduces)) : null;
            gameUI?.ShowBanner(title, sub, world != null ? Color.Lerp(world.accent, Light, 0.2f) : Light, 1f);
            sounds?.Play(sounds.go, 0.8f);
            if (versus != null)
            {
                gameUI?.ShowTip(IsOnlineVersus ? T("A set scores and lets you go again. A mistake passes the turn.")
                    : T("Take turns: a set scores and lets you go again, a mistake passes the turn."));
            }
            else if (level.IsCampaign && !string.IsNullOrEmpty(TipOf(LevelIndex)) && progress.Stars(LevelIndex) == 0)
            {
                gameUI?.ShowTip(TipOf(LevelIndex));
            }
            else if (rules.Parade && endlessBoard <= 1)
            {
                gameUI?.ShowTip(F("Match the {0} in the order the parade at the top shows.", T(FacesWord)));
            }
            phase = Phase.Playing;
            SetCardsInteractable(true);
            if (IsOnlineVersus)
            {
                RefreshVersusInput();
            }
            else if (versus != null)
            {
                After(1f, AnnounceTurn);
            }
        }


        private void OnCardClicked(Flippable view)
        {
            if (!IsPlaying || view == null || view.Index < 0 || view.Index >= round.Cards.Count)
            {
                return;
            }
            if (IsOnlineVersus)
            {
                RequestOnlineFlip(view);
                return;
            }
            MemoryCard card = round.Cards[view.Index];
            if (round.MismatchShowing && (versus != null || card.State == CardState.Revealed || pendingShuffle))
            {
                // Clicking during a mistake flips it back at once (and runs a pending shuffle first). In a versus game
                // that is all the click does: the next card belongs to the next player.
                HideMismatch();
                return;
            }
            actingSeat = versus != null ? versus.Current : -1;
            FlipResult result = round.Flip(card);
            if (result.Ignored)
            {
                return;
            }
            PlayFlip(view, result, versus != null ? BookLocalFlip(result).Points : result.Points);
        }


        /// <summary>
        /// Plays what a flip did. <paramref name="points"/> is what the flip was worth to the player who made it, which
        /// in a versus game is not always what the board counted (a bomb takes no more than the player has).
        /// </summary>
        private void PlayFlip(Flippable view, FlipResult result, int points)
        {
            result.Points = points;
            Acting?.RegisterFlip();
            if (result.FlippedBack.Count > 0)
            {
                unflipTimer = -1f;
                foreach (MemoryCard back in result.FlippedBack)
                {
                    ViewOf(back)?.FlipDown();
                }
            }
            switch (result.Outcome)
            {
                case FlipOutcome.Cracked:
                    view.Crack();
                    sounds?.Play(sounds.crack, 0.9f, UnityEngine.Random.Range(0.95f, 1.1f));
                    particles?.Shards(ParticlePosition(view));
                    break;
                case FlipOutcome.Revealed:
                    view.FlipUp();
                    sounds?.PlayAny(sounds.flips, 0.8f);
                    if (world != null)
                    {
                        view.Highlight(Color.Lerp(world.accent, Color.white, 0.3f));
                    }
                    break;
                case FlipOutcome.Matched:
                case FlipOutcome.WildMatched:
                    OnMatch(view, result);
                    break;
                case FlipOutcome.Mismatched:
                case FlipOutcome.WrongOrder:
                    OnMistake(view, result);
                    break;
                case FlipOutcome.Bomb:
                    OnBomb(view, result);
                    break;
                case FlipOutcome.Clock:
                    OnClock(view, result);
                    break;
                case FlipOutcome.Peek:
                    OnPeek(view, result);
                    break;
            }
            RefreshHud();
            RefreshParade();
            if (round.IsOver)
            {
                Finish();
            }
        }


        private void OnMatch(Flippable view, FlipResult result)
        {
            view.FlipUp();
            sounds?.PlayAny(sounds.flips, 0.8f);
            float delay = flipTime * 0.9f;
            int order = 0;
            foreach (MemoryCard card in result.Cards)
            {
                Flippable matched = ViewOf(card);
                if (matched == null)
                {
                    continue;
                }
                if (!matched.IsFaceUp && matched != view)
                {
                    matched.FlipUp(order * 0.06f);
                }
                matched.ShowMatched(delay + order * 0.06f);
                order++;
            }
            int combo = result.Combo;
            bool wild = result.Outcome == FlipOutcome.WildMatched;
            After(delay, () =>
            {
                sounds?.PlayMatch(combo);
                if (wild)
                {
                    sounds?.Play(sounds.wild);
                }
                foreach (MemoryCard card in result.Cards)
                {
                    Flippable matched = ViewOf(card);
                    if (matched != null)
                    {
                        Vector2 at = ParticlePosition(matched);
                        if (wild)
                        {
                            particles?.Rainbow(at);
                        }
                        else
                        {
                            particles?.Sparkle(at, world != null ? world.accent : Gold);
                        }
                    }
                }
                gameUI?.ShowPopup(view.transform.position, $"+{result.Points}", PointsColor, combo >= 2 ? 54f : 46f);
                if (result.TimeChange > 0f)
                {
                    gameUI?.ShowPopup(view.transform.position + Vector3.down * 46f, Seconds(result.TimeChange, true), Good, 34f);
                }
            });
            if (wild)
            {
                gameUI?.ShowBanner(T("WILD!"), F("Every {0} found", AnimalName(result.Cards)), look != null ? look.Colors.wild : new Color(0.85f, 0.55f, 1f), 1.2f);
            }
            else if (combo >= 2)
            {
                string label = F("COMBO x{0}!", Mathf.Min(combo, MemoryRound.MaxComboMultiplier));
                Color comboColor = look != null ? look.Colors.combo : new Color(1f, 0.55f, 0.85f);
                After(delay + 0.15f, () => gameUI?.ShowPopup(view.transform.position + Vector3.down * 92f, label, comboColor, 36f));
            }
            Acting?.RegisterMatch();
            runSets++;
            runBestCombo = Mathf.Max(runBestCombo, round.BestCombo);
        }


        private string AnimalName(List<MemoryCard> cards)
        {
            foreach (MemoryCard card in cards)
            {
                if (card.IsAnimal)
                {
                    return Pretty(AnimalSprite(card.Animal));
                }
            }
            return T(FaceWord);
        }


        /// <summary>The name of a face in the language shown ("cow"), or the theme's word for a face.</summary>
        private string Pretty(Sprite sprite)
        {
            return FaceName(sprite, T(FaceWord));
        }


        private void OnMistake(Flippable view, FlipResult result)
        {
            view.FlipUp();
            sounds?.PlayAny(sounds.flips, 0.8f);
            foreach (MemoryCard card in result.Cards)
            {
                ViewOf(card)?.ShowMistake(flipTime);
            }
            bool wrongOrder = result.Outcome == FlipOutcome.WrongOrder;
            After(flipTime * 0.8f, () => sounds?.Play(wrongOrder ? sounds.wrongOrder : sounds.mismatch, 0.8f));
            // Online the server turns a mistake back, for everybody at the same moment.
            unflipTimer = IsOnlineVersus ? -1f : flipTime + Mathf.Max(0.2f, rules.UnflipDelay);
            pendingShuffle = result.Shuffle && !round.IsOver;
            if (wrongOrder)
            {
                string next = Pretty(AnimalSprite(round.ParadeTarget));
                gameUI?.ShowBanner(T("WRONG ORDER!"), F("The parade wants the {0} next", next), Bad, 1.4f);
            }
            if (result.HeartLost)
            {
                gameUI?.HeartLost();
                After(flipTime, () => sounds?.Play(sounds.heart, 0.8f));
            }
            Acting?.RegisterMistake();
        }


        private void OnBomb(Flippable view, FlipResult result)
        {
            view.FlipUp();
            float delay = flipTime + 0.25f;
            view.Explode(delay);
            After(delay, () =>
            {
                sounds?.Play(sounds.bomb);
                particles?.Explosion(ParticlePosition(view));
                shake = 1f;
                gameUI?.FlashScreen(new Color(1f, 0.55f, 0.2f), 0.28f);
                string time = round.HasCountdown ? Seconds(-result.TimeChange, false) : Seconds(result.TimeChange, true);
                if (Mathf.Abs(result.TimeChange) > 0.01f)
                {
                    gameUI?.ShowPopup(view.transform.position, time, Bad, 50f);
                }
                if (result.Points < 0)
                {
                    gameUI?.ShowPopup(view.transform.position + Vector3.down * 50f, result.Points.ToString(), Bad, 38f);
                }
                if (result.HeartLost)
                {
                    gameUI?.HeartLost();
                    sounds?.Play(sounds.heart, 0.8f);
                }
            });
            gameUI?.ShowBanner(T("BOOM!"), result.HeartLost ? T("A bomb cost you a heart") : versus != null ? T("The turn is over") : T("Watch out for bombs"), Bad, 1.1f);
        }


        private void OnClock(Flippable view, FlipResult result)
        {
            view.FlipUp();
            float delay = flipTime + 0.3f;
            view.Vanish(delay);
            After(delay, () =>
            {
                sounds?.Play(sounds.clock);
                particles?.Puff(ParticlePosition(view), Good);
                string time = round.HasCountdown ? Seconds(result.TimeChange, true) : Seconds(-result.TimeChange, false);
                gameUI?.ShowPopup(view.transform.position, time, Good, 50f);
            });
        }


        private void OnPeek(Flippable view, FlipResult result)
        {
            view.FlipUp();
            view.Vanish(flipTime + 0.3f);
            StartFlow(PeekRoutine(view));
        }


        private IEnumerator PeekRoutine(Flippable view)
        {
            phase = Phase.Busy;
            yield return new WaitForSeconds(flipTime + 0.25f);
            sounds?.Play(sounds.peek);
            particles?.Puff(ParticlePosition(view), new Color(0.75f, 0.55f, 1f));
            gameUI?.ShowBanner(T("PEEK!"), T("Take a good look"), look != null ? look.Colors.peek : new Color(0.8f, 0.65f, 1f), 1.2f);
            var shown = new List<Flippable>();
            foreach (MemoryCard card in round.Cards)
            {
                if (card.State == CardState.Hidden)
                {
                    Flippable hidden = ViewOf(card);
                    if (hidden != null)
                    {
                        hidden.FlipUp(shown.Count * previewStagger);
                        shown.Add(hidden);
                    }
                }
            }
            yield return new WaitForSeconds(flipTime + rules.PeekTime);
            for (int i = 0; i < shown.Count; i++)
            {
                shown[i].FlipDown(i * previewStagger);
            }
            yield return new WaitForSeconds(flipTime + shown.Count * previewStagger);
            phase = Phase.Playing;
        }


        private void HideMismatch()
        {
            unflipTimer = -1f;
            if (round == null)
            {
                return;
            }
            foreach (MemoryCard card in round.HideMismatch())
            {
                ViewOf(card)?.FlipDown();
            }
            MismatchHidden();
            if (pendingShuffle && !round.IsOver)
            {
                pendingShuffle = false;
                StartFlow(ShuffleRoutine());
            }
        }


        private IEnumerator ShuffleRoutine()
        {
            phase = Phase.Busy;
            yield return new WaitForSeconds(flipTime + 0.1f);
            sounds?.Play(sounds.shuffle);
            gameUI?.ShowBanner(T("SHUFFLE!"), look != null && !string.IsNullOrEmpty(look.Say.shuffle) ? T(look.Say.shuffle) : T("The critters are on the move"), Light, 1.2f);
            List<MemoryCard> moved = round.Shuffle(random);
            for (int i = 0; i < moved.Count; i++)
            {
                ViewOf(moved[i])?.MoveTo(SlotPosition(moved[i].Slot), i * 0.025f, shuffleDuration);
            }
            yield return new WaitForSeconds(shuffleDuration + moved.Count * 0.025f);
            if (!round.IsOver)
            {
                phase = Phase.Playing;
            }
        }


        private void Finish()
        {
            if (phase == Phase.Finished || round == null)
            {
                return;
            }
            phase = Phase.Finished;
            unflipTimer = -1f;
            pendingShuffle = false;
            SetCardsInteractable(false);
            StartFlow(FinishRoutine());
        }


        private IEnumerator FinishRoutine()
        {
            if (versus != null)
            {
                yield return VersusFinishRoutine();
                yield break;
            }
            MemoryCardsLevel level = CardsLevel;
            yield return new WaitForSeconds(flipTime + 0.45f);
            runTime += round.Elapsed;
            runBestCombo = Mathf.Max(runBestCombo, round.BestCombo);
            if (round.IsCleared && level.IsEndless)
            {
                float bonus = EndlessRules.ClearBonus(endlessBoard);
                gameUI?.ShowBanner(T("BOARD CLEAR!"), F("+{0}s on the clock", bonus.ToString("0", CultureInfo.InvariantCulture)), Gold, 1.3f);
                sounds?.Play(sounds.record, 0.9f);
                CelebrateCards();
                particles?.Confetti(60, ConfettiColors());
                yield return new WaitForSeconds(1.3f);
                for (int i = 0; i < views.Count; i++)
                {
                    views[i].Leave(board != null ? board.ExitPosition(round.Cards[i].Slot) : Vector2.down * 1200f, i * 0.01f, 0.5f);
                }
                sounds?.Play(sounds.shuffle, 0.7f);
                yield return new WaitForSeconds(0.65f);
                float clock = round.Clock + bonus;
                int score = round.Score;
                int combo = round.Combo;
                endlessBoard++;
                rules = EndlessRules.ForBoard(endlessBoard);
                rules.TimeLimit = clock;
                BeginBoard(DealBoard(), score, combo);
                yield break;
            }

            if (round.IsCleared)
            {
                int bonus = round.ApplyFinishBonus();
                RefreshHud();
                CelebrateCards();
                particles?.Confetti(110, ConfettiColors());
                sounds?.Play(sounds.victory);
                gameUI?.ShowBanner(T("CLEARED!"), bonus > 0 ? F("+{0} bonus points", N(bonus)) : null, Gold, 1.6f);
                yield return new WaitForSeconds(1.9f);
                EndGame(true, bonus);
            }
            else
            {
                string title;
                switch (round.End)
                {
                    case RoundEnd.OutOfHearts: title = T("OUT OF HEARTS!"); break;
                    case RoundEnd.OutOfMoves: title = T("OUT OF MOVES!"); break;
                    default: title = T("TIME'S UP!"); break;
                }
                gameUI?.ShowBanner(title, null, Bad, 1.6f);
                sounds?.Play(sounds.gameOver);
                sounds?.StopMusic();
                int order = 0;
                foreach (MemoryCard card in round.Cards)
                {
                    Flippable view = ViewOf(card);
                    if (view == null || !view.gameObject.activeSelf)
                    {
                        continue;
                    }
                    if (card.State == CardState.Hidden || card.State == CardState.Revealed)
                    {
                        if (!view.IsFaceUp)
                        {
                            view.FlipUp(0.3f + order * 0.02f);
                        }
                        view.Wilt(0.5f + order * 0.02f);
                        order++;
                    }
                }
                yield return new WaitForSeconds(2f);
                EndGame(false, 0);
            }
        }


        private void CelebrateCards()
        {
            if (board == null)
            {
                return;
            }
            foreach (MemoryCard card in round.Cards)
            {
                Flippable view = ViewOf(card);
                if (view != null && view.gameObject.activeSelf)
                {
                    int column = card.Slot % board.Columns;
                    int row = card.Slot / board.Columns;
                    view.Celebrate((column + row) * 0.06f);
                }
            }
        }


        private Color[] ConfettiColors()
        {
            Color accent = world != null ? world.accent : Gold;
            if (look != null && look.Colors.celebration != null && look.Colors.celebration.Count > 0)
            {
                var colors = new List<Color> { accent };
                colors.AddRange(look.Colors.celebration);
                return colors.ToArray();
            }
            return new[] { accent, Gold, Color.white, new Color(0.4f, 0.8f, 1f), new Color(1f, 0.45f, 0.6f), new Color(0.5f, 0.9f, 0.45f) };
        }


        private void EndGame(bool victory, int bonus)
        {
            MemoryCardsLevel level = CardsLevel;
            int score = round.Score;
            var result = new RoundResult
            {
                LevelTitle = RoundTitle(),
                Kind = level.Kind,
                Victory = victory,
                End = round.End,
                Score = score,
                Bonus = bonus,
                Time = level.IsEndless ? runTime : round.Clock,
                Countdown = round.HasCountdown,
                Mistakes = round.Mistakes,
                Moves = round.Moves,
                BestCombo = runBestCombo,
                Boards = Mathf.Max(0, endlessBoard - 1),
                Accent = world != null ? world.accent : Gold
            };
            if (level.IsEndless)
            {
                result.NewBest = progress.RecordEndless(result.Boards, score);
                result.Best = progress.EndlessBestScore;
            }
            else if (level.IsCampaign)
            {
                bool countdown = rules.HasTimeLimit;
                result.Goals = Goals(level.Goals, countdown);
                result.GoalsReached = new[] { round.IsCleared, level.Goals.SecondStar(round), level.Goals.ThirdStar(round) };
                if (victory)
                {
                    result.Stars = level.Goals.Stars(round);
                    result.NewBest = progress.RecordLevel(LevelIndex, result.Stars, score);
                }
                result.Best = progress.BestScore(LevelIndex);
                int next = campaign.NextLevel(LevelIndex);
                result.HasNext = victory && next >= 0 && campaign.IsUnlocked(next, progress);
            }
            progress.RecordGame(runSets, runBestCombo);
            gameUI?.ShowResults(result);
            sounds?.PlayMusic(sounds.menuMusic);
            TransitionState(victory ? BaseGameState.Victory : BaseGameState.GameOver);
        }

        #endregion


        #region View helpers

        private void RefreshHud()
        {
            if (round == null)
            {
                return;
            }
            if (versus == null)
            {
                PlayerScore = round.Score;
            }
            gameUI?.UpdateHud(round.Score, round.Combo, round.Clock, round.HeartsLeft, round.MovesLeft, round.MatchedSets, round.Sets);
        }


        private void RefreshParade()
        {
            if (round == null || !rules.Parade || gameUI == null)
            {
                return;
            }
            var upcoming = new List<Sprite>();
            for (int i = round.ParadeIndex; i < round.ParadeOrder.Count && upcoming.Count < 4; i++)
            {
                int animal = round.ParadeOrder[i];
                if (!round.IsAnimalMatched(animal))
                {
                    upcoming.Add(AnimalSprite(animal));
                }
            }
            gameUI.SetParade(upcoming);
        }


        private void SetCardsInteractable(bool interactable)
        {
            foreach (Flippable view in views)
            {
                view.Interactable = interactable;
            }
        }


        private Vector2 ParticlePosition(Flippable view)
        {
            return particles != null ? particles.ToLocal(view.transform.position) : Vector2.zero;
        }


        private void ClearBoard()
        {
            foreach (Flippable view in views)
            {
                if (view != null)
                {
                    cardsCache.Undeploy(view);
                }
            }
            views.Clear();
        }


        private void UpdateShake()
        {
            if (board == null)
            {
                return;
            }
            if (shake <= 0f)
            {
                return;
            }
            shake = Mathf.MoveTowards(shake, 0f, Time.deltaTime * 2.2f);
            Vector2 offset = shake > 0f ? UnityEngine.Random.insideUnitCircle * (22f * shake * shake) : Vector2.zero;
            board.Rect.anchoredPosition = boardRest + offset;
        }


        private void StartFlow(IEnumerator routine)
        {
            StartCoroutine(routine);
        }


        /// <summary>Stops every running sequence and delayed effect of the round.</summary>
        private void StopFlow()
        {
            StopAllCoroutines();
            shake = 0f;
            if (board != null)
            {
                board.Rect.anchoredPosition = boardRest;
            }
        }


        private void After(float delay, Action action)
        {
            StartCoroutine(AfterRoutine(delay, action));
        }


        private static IEnumerator AfterRoutine(float delay, Action action)
        {
            yield return new WaitForSeconds(delay);
            action();
        }


        /// <summary>Clicks a card as the player would (used by the autopilot).</summary>
        internal void ClickCard(Flippable view)
        {
            OnCardClicked(view);
        }

        #endregion


        #region Saving/Loading

        public override bool DoesSaveGameExist()
        {
            IStorageStrategy disk = Disk;
            return disk != null && disk.DoesKeyExist(SaveKey("Cards")) && disk.DoesKeyExist(SaveKey("Version"))
                   && disk.GetInt(SaveKey("Version")) == SaveVersion;
        }


        /// <summary>
        /// Saves the round in progress: the level, the cards (kind, animal, state, ice, slot), the animals by their
        /// index in the campaign, the parade and every counter. A mistake still showing is saved flipped back. Free play
        /// with custom rules saves the rules with the game.
        /// </summary>
        public override void SaveGame()
        {
            IStorageStrategy disk = Disk;
            if (disk == null)
            {
                return;
            }
            if (!State.Is(BaseGameState.Paused))
            {
                UI?.UpdateError(MobilePlatform.Pick(T("Pause the game (Escape) to save it."), T("Pause the game to save it.")));
                return;
            }
            if (round == null || round.IsOver || phase == Phase.Finished)
            {
                UI?.UpdateError(T("There is no game in progress to save."));
                return;
            }
            if (versus != null)
            {
                UI?.UpdateError(T("A game for several players cannot be saved."));
                return;
            }
            var animals = new StringBuilder();
            IReadOnlyList<Sprite> deck = Deck;
            for (int i = 0; i < round.Animals.Count; i++)
            {
                Sprite sprite = round.Animals[i] < animalPool.Count ? animalPool[round.Animals[i]] : null;
                int global = -1;
                for (int j = 0; j < deck.Count; j++)
                {
                    if (deck[j] == sprite)
                    {
                        global = j;
                        break;
                    }
                }
                if (global < 0)
                {
                    UI?.UpdateError(T("Cannot save: an animal of this board is not part of the campaign."));
                    return;
                }
                animals.Append(i > 0 ? "," : string.Empty).Append(global);
            }
            var cards = new StringBuilder();
            for (int i = 0; i < round.Cards.Count; i++)
            {
                MemoryCard card = round.Cards[i];
                CardState state = card.State == CardState.Revealed ? CardState.Hidden : card.State;
                cards.Append(i > 0 ? ";" : string.Empty)
                    .Append((int)card.Kind).Append(',').Append(card.Animal).Append(',').Append((int)state).Append(',')
                    .Append(card.Frozen ? 1 : 0).Append(',').Append(card.Slot);
            }
            disk.SetInt(SaveKey("Version"), SaveVersion);
            disk.SetInt(SaveKey("Level"), LevelIndex);
            disk.SetInt(SaveKey("Board"), endlessBoard);
            disk.SetBool(SaveKey("Custom"), freePlayCustom);
            if (freePlayCustom && currentGameSettings != null)
            {
                currentGameSettings.SaveSettings(disk, SaveKey("Rules."));
            }
            disk.SetFloat(SaveKey("TimeLimit"), rules.TimeLimit);
            disk.SetString(SaveKey("Animals"), animals.ToString());
            disk.SetString(SaveKey("Parade"), string.Join(",", round.ParadeOrder));
            disk.SetString(SaveKey("Cards"), cards.ToString());
            disk.SetInt(SaveKey("MatchedSets"), round.MatchedSets);
            disk.SetInt(SaveKey("Score"), round.Score);
            disk.SetInt(SaveKey("Combo"), round.Combo);
            disk.SetInt(SaveKey("BestCombo"), round.BestCombo);
            disk.SetInt(SaveKey("Mistakes"), round.Mistakes);
            disk.SetInt(SaveKey("Moves"), round.Moves);
            disk.SetInt(SaveKey("Hearts"), round.HeartsLeft);
            disk.SetInt(SaveKey("BombsHit"), round.BombsHit);
            disk.SetInt(SaveKey("ParadeIndex"), round.ParadeIndex);
            disk.SetInt(SaveKey("ShuffleCount"), round.MistakesSinceShuffle);
            disk.SetFloat(SaveKey("Clock"), round.Clock);
            disk.SetFloat(SaveKey("Elapsed"), round.Elapsed);
            disk.SetInt(SaveKey("RunSets"), runSets);
            disk.SetInt(SaveKey("RunCombo"), runBestCombo);
            disk.SetFloat(SaveKey("RunTime"), runTime);
            if (!PersistSavedGame())
            {
                return;
            }
            UI?.EnableLoad();
            UI?.UpdateError(T("Game saved."));
        }


        public override void LoadGame()
        {
            IStorageStrategy disk = Disk;
            if (disk == null || !DoesSaveGameExist())
            {
                UI?.UpdateError(T("There is no saved game to load."));
                return;
            }
            int levelIndex = disk.GetInt(SaveKey("Level"));
            if (levelIndex < 0 || levelIndex >= LevelCount)
            {
                UI?.UpdateError(T("The saved game belongs to a level that no longer exists."));
                return;
            }
            LoadLevel(levelIndex);
            MemoryCardsLevel level = CardsLevel;
            freePlayCustom = false;
            if (level.IsEndless)
            {
                endlessBoard = Mathf.Max(1, disk.GetInt(SaveKey("Board")));
                rules = EndlessRules.ForBoard(endlessBoard);
                rules.TimeLimit = disk.GetFloat(SaveKey("TimeLimit"));
            }
            else if (level.IsFreePlay)
            {
                MemoryCardsSettings source = defaultSettings;
                if (disk.DoesKeyExist(SaveKey("Custom")) && disk.GetBool(SaveKey("Custom")) && currentGameSettings != null)
                {
                    currentGameSettings.CopySettings(defaultSettings);
                    currentGameSettings.LoadSettings(disk, SaveKey("Rules."));
                    source = currentGameSettings;
                    freePlayCustom = true;
                }
                rules = source.ToRules();
                endlessBoard = 0;
            }
            else
            {
                rules = level.Settings.ToRules();
                endlessBoard = 0;
            }

            Deal deal;
            try
            {
                deal = ReadDeal(disk.GetString(SaveKey("Animals")), disk.GetString(SaveKey("Parade")), disk.GetString(SaveKey("Cards")));
            }
            catch (FormatException formatException)
            {
                UI?.UpdateError(F("The saved game is damaged: {0}", formatException.Message));
                return;
            }
            if (deal.Cards.Count != rules.Cards)
            {
                UI?.UpdateError(T("The saved game does not fit the level any more."));
                return;
            }

            StopFlow();
            ClearBoard();
            particles?.Clear();
            unflipTimer = -1f;
            pendingShuffle = false;
            animalPool = Deck;
            player?.ResetMatches();
            runSets = disk.GetInt(SaveKey("RunSets"));
            runBestCombo = disk.GetInt(SaveKey("RunCombo"));
            runTime = disk.GetFloat(SaveKey("RunTime"));
            ApplyWorld(level.World, false);
            gameUI?.Curtain(world != null ? world.skyTop : Color.white);
            TransitionState(BaseGameState.Running);
            sounds?.PlayMusic(sounds.playMusic);

            round = new MemoryRound(rules, deal);
            round.Restore(disk.GetInt(SaveKey("MatchedSets")), disk.GetInt(SaveKey("Score")), disk.GetInt(SaveKey("Combo")),
                disk.GetInt(SaveKey("BestCombo")), disk.GetInt(SaveKey("Mistakes")), disk.GetInt(SaveKey("Moves")),
                disk.GetInt(SaveKey("Hearts")), disk.GetFloat(SaveKey("Clock")), disk.GetFloat(SaveKey("Elapsed")),
                disk.GetInt(SaveKey("ParadeIndex")), disk.GetInt(SaveKey("ShuffleCount")), disk.GetInt(SaveKey("BombsHit")));
            lastTick = -1;
            SpawnViews(deal);
            foreach (MemoryCard card in round.Cards)
            {
                ViewOf(card)?.SetStateInstant(card.State, card.Frozen);
            }
            gameUI?.BeginRound(new RoundHud
            {
                Title = RoundTitle(),
                Mode = level.IsFreePlay ? LevelMode.FreePlay : MemoryCardsLevel.ModeOf(level.Kind, rules),
                Countdown = rules.HasTimeLimit,
                Hearts = rules.Hearts,
                MoveLimit = rules.MoveLimit,
                Sets = round.Sets,
                MatchSize = rules.MatchSize,
                Parade = rules.Parade,
                Endless = level.IsEndless,
                Board = endlessBoard,
                Accent = world != null ? world.accent : Color.white
            });
            RefreshHud();
            RefreshParade();
            StartFlow(ResumeRoutine());
        }


        private IEnumerator ResumeRoutine()
        {
            phase = Phase.Busy;
            SetCardsInteractable(false);
            gameUI?.ShowBanner(T("READY?"), T("Your saved game is back"), Light, 1.1f);
            yield return new WaitForSeconds(1.1f);
            gameUI?.ShowBanner(T("GO!"), null, world != null ? world.accent : Color.white, 0.8f);
            sounds?.Play(sounds.go, 0.8f);
            phase = Phase.Playing;
            SetCardsInteractable(true);
        }


        private static Deal ReadDeal(string animals, string parade, string cards)
        {
            var deal = new Deal();
            foreach (string value in Split(animals, ','))
            {
                deal.Animals.Add(int.Parse(value, CultureInfo.InvariantCulture));
            }
            foreach (string value in Split(parade, ','))
            {
                deal.Parade.Add(int.Parse(value, CultureInfo.InvariantCulture));
            }
            var slots = new HashSet<int>();
            string[] entries = Split(cards, ';');
            for (int i = 0; i < entries.Length; i++)
            {
                string[] parts = entries[i].Split(',');
                if (parts.Length != 5)
                {
                    throw new FormatException($"card {i} has {parts.Length} values");
                }
                var card = new MemoryCard
                {
                    Id = i,
                    Kind = (CardKind)int.Parse(parts[0], CultureInfo.InvariantCulture),
                    Animal = int.Parse(parts[1], CultureInfo.InvariantCulture),
                    State = (CardState)int.Parse(parts[2], CultureInfo.InvariantCulture),
                    Frozen = parts[3] == "1",
                    Slot = int.Parse(parts[4], CultureInfo.InvariantCulture)
                };
                if (card.Slot < 0 || card.Slot >= entries.Length || !slots.Add(card.Slot))
                {
                    throw new FormatException($"card {i} lies in slot {card.Slot}");
                }
                if (card.Kind == CardKind.Animal && (card.Animal < 0 || card.Animal >= deal.Animals.Count))
                {
                    throw new FormatException($"card {i} shows animal {card.Animal}");
                }
                deal.Cards.Add(card);
            }
            return deal;
        }


        private static string[] Split(string text, char separator)
        {
            return string.IsNullOrEmpty(text) ? new string[0] : text.Split(separator);
        }

        #endregion
    }
}
