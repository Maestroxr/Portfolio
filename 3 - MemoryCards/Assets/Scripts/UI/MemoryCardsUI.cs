using System.Collections.Generic;
using System.Globalization;
using Gamebox;
using Gamebox.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Portfolio.MemoryCards
{
    /// <summary>
    /// Interface of the Memory Cards module: the level select (worlds, level cards, details), the HUD of a round and
    /// the results screen. The menu of the base <see cref="GameUI"/> is the pause panel (resume, restart, save, load,
    /// settings, quit) and hosts the settings panel of free play. Animations run on unscaled time.
    /// </summary>
    public class MemoryCardsUI : GameUI
    {
        private sealed class Popup
        {
            public TMP_Text text;
            public Vector2 start;
            public float age;
            public float life;
        }

        private static readonly string[] Tips =
        {
            "Tip: flip cards you have not seen yet before guessing.",
            "Tip: matches in a row build a combo worth up to x5 points.",
            "Tip: click anywhere after a mistake to flip the cards back sooner.",
            "Tip: a wild card clears every card of the animal you pair it with.",
            "Tip: remember where the bombs are - they stay put until a shuffle.",
            "Tip: in a parade, keep finding pairs even when they are not next."
        };

        [Header("Screens")]
        [SerializeField] internal CanvasGroup titleScreen;
        [SerializeField] internal CanvasGroup hudScreen;
        [SerializeField] internal CanvasGroup resultsScreen;
        [SerializeField] internal Image curtain;
        [SerializeField] internal Image flash;

        [Header("Level select")]
        [SerializeField] internal RectTransform logo;
        [SerializeField] internal RectTransform[] titleMascots = new RectTransform[0];
        [SerializeField] internal WorldTab[] worldTabs = new WorldTab[0];
        [SerializeField] internal LevelCard[] levelCards = new LevelCard[0];
        [Tooltip("Distance between the centres of two level cards.")]
        [SerializeField] internal Vector2 levelSpacing = new Vector2(250f, 320f);
        [SerializeField] internal int levelsPerRow = 3;
        [SerializeField] internal Image detailHeader;
        [SerializeField] internal TMP_Text detailWorld;
        [SerializeField] internal TMP_Text detailTitle;
        [SerializeField] internal Image detailModeIcon;
        [SerializeField] internal TMP_Text detailMode;
        [SerializeField] internal TMP_Text detailDescription;
        [SerializeField] internal GameObject detailNewRoot;
        [SerializeField] internal TMP_Text detailNew;
        [SerializeField] internal TMP_Text detailBoard;
        [SerializeField] internal GameObject detailGoalsRoot;
        [SerializeField] internal Image[] detailStars = new Image[0];
        [SerializeField] internal TMP_Text[] detailGoals = new TMP_Text[0];
        [SerializeField] internal TMP_Text detailBest;
        [SerializeField] internal Button playButton;
        [SerializeField] internal TMP_Text playLabel;
        [SerializeField] internal Button continueButton;
        [SerializeField] internal Button titleSettingsButton;
        [SerializeField] internal Button titleExitButton;
        [SerializeField] internal Button resetProgressButton;
        [SerializeField] internal TMP_Text resetProgressLabel;
        [SerializeField] internal TMP_Text starsTotal;
        [SerializeField] internal TMP_Text lifetimeText;

        [Header("HUD")]
        [SerializeField] internal TMP_Text levelText;
        [SerializeField] internal Image modeIcon;
        [SerializeField] internal TMP_Text modeText;
        [SerializeField] internal TMP_Text scoreText;
        [SerializeField] internal RectTransform comboBadge;
        [SerializeField] internal TMP_Text comboText;
        [SerializeField] internal GameObject timerRoot;
        [SerializeField] internal TMP_Text timerText;
        [SerializeField] internal Image timerIcon;
        [SerializeField] internal GameObject heartsRoot;
        [SerializeField] internal Image[] hearts = new Image[0];
        [SerializeField] internal GameObject movesRoot;
        [SerializeField] internal TMP_Text movesText;
        [SerializeField] internal GameObject setsRoot;
        [SerializeField] internal TMP_Text setsText;
        [SerializeField] internal GameObject paradeRoot;
        [SerializeField] internal Image[] paradeSlots = new Image[0];
        [SerializeField] internal Button pauseButton;
        [SerializeField] internal CanvasGroup bannerGroup;
        [SerializeField] internal TMP_Text bannerText;
        [SerializeField] internal TMP_Text bannerSub;
        [SerializeField] internal GameObject memorizeRoot;
        [SerializeField] internal Image memorizeFill;
        [SerializeField] internal RectTransform popupRoot;
        [SerializeField] internal TMP_Text popupTemplate;
        [SerializeField] internal CanvasGroup tipGroup;
        [SerializeField] internal TMP_Text tipText;

        [Header("Results")]
        [SerializeField] internal RectTransform resultPanel;
        [SerializeField] internal Image resultHeader;
        [SerializeField] internal TMP_Text resultTitle;
        [SerializeField] internal TMP_Text resultSubtitle;
        [SerializeField] internal RectTransform resultStarsRoot;
        [SerializeField] internal Image[] resultStars = new Image[0];
        [SerializeField] internal TMP_Text resultStats;
        [SerializeField] internal TMP_Text[] resultGoals = new TMP_Text[0];
        [SerializeField] internal Image[] resultGoalIcons = new Image[0];
        [SerializeField] internal TMP_Text resultBest;
        [SerializeField] internal Button nextButton;
        [SerializeField] internal Button retryButton;
        [SerializeField] internal Button levelsButton;

        [Header("Pause")]
        [SerializeField] internal Button levelSelectButton;

        [Header("Versus")]
        [Tooltip("Steps through the number of players at this device: one, or a versus game for two to four.")]
        [SerializeField] internal Button playersButton;
        [SerializeField] internal TMP_Text playersLabel;
        [SerializeField] internal Button onlineButton;
        [SerializeField] internal VersusHud versusHud;
        [Tooltip("What the HUD shows only in a game for one: the score and the clock.")]
        [SerializeField] internal GameObject[] soloWidgets = new GameObject[0];

        [Header("Sprites")]
        [SerializeField] internal Sprite starFull;
        [SerializeField] internal Sprite starEmpty;
        [SerializeField] internal Sprite heartFull;
        [SerializeField] internal Sprite heartEmpty;
        [SerializeField] internal Sprite goalDone;
        [SerializeField] internal Sprite goalMissed;
        [Tooltip("Icons of the level modes, in LevelMode order.")]
        [SerializeField] internal Sprite[] modeIcons = new Sprite[0];

        private BaseGameState shownState = BaseGameState.Initialization;
        private readonly List<LevelSummary> levels = new List<LevelSummary>();
        private readonly List<Popup> popups = new List<Popup>();
        private readonly List<TMP_Text> popupPool = new List<TMP_Text>();
        private RoundHud hud;
        private int shownScore = -1;
        private int shownCombo = -1;
        private int shownClock = -1;
        private int shownHearts = -1;
        private int shownMoves = -1;
        private int shownSets = -1;
        private float comboPunch;
        private float heartShake;
        private float timerPulse;
        private float bannerTime = -1f;
        private float bannerLength;
        private float tipTime = -1f;
        private float flashAlpha;
        private Color flashColor = Color.white;
        private float curtainAlpha;
        private float resultsTime = -1f;
        private int resultStarCount;
        private int starsPopped;
        private float resetConfirmUntil = -1f;
        private float titleTime;
        private float resultStatsSize;
        private bool resultButtonsKnown;
        private Vector2 levelsRest;
        private Vector2 retryRest;
        private Vector2 heartsRest;
        private Vector2 logoRest;

        private MemoryCardsGameManager Cards => Manager as MemoryCardsGameManager;

        protected override void Awake()
        {
            base.Awake();
            Listen(playButton, () => Cards?.PlaySelectedLevel());
            Listen(continueButton, () => Cards?.LoadGame());
            Listen(titleSettingsButton, () => { Cards?.PlayClick(); ShowSettings(); });
            Listen(titleExitButton, () => GameManager?.ExitGame());
            Listen(resetProgressButton, OnResetProgress);
            Listen(pauseButton, OnPauseClicked);
            Listen(nextButton, () => Cards?.PlayNextLevel());
            Listen(retryButton, () => Cards?.RetryLevel());
            Listen(levelsButton, () => Cards?.ReturnToLevelSelect());
            Listen(levelSelectButton, () => Cards?.ReturnToLevelSelect());
            Listen(playersButton, () => Cards?.CyclePlayers());
            Listen(onlineButton, () => Cards?.OpenOnline());
            // The shared menu buttons are wired by the base class; they only need their click sound here.
            foreach (Button button in new[] { StartNewGame, ReturnToGame, SaveGame, LoadGame, SettingsButton, ExitButton })
            {
                Listen(button, () => Cards?.PlayClick());
            }
            foreach (WorldTab tab in worldTabs)
            {
                if (tab != null)
                {
                    tab.Clicked += world => Cards?.SelectWorld(world);
                }
            }
            foreach (LevelCard card in levelCards)
            {
                if (card != null)
                {
                    card.Clicked += level => Cards?.SelectLevel(level);
                }
            }
            if (heartsRoot != null)
            {
                heartsRest = ((RectTransform)heartsRoot.transform).anchoredPosition;
            }
            if (logo != null)
            {
                logoRest = logo.anchoredPosition;
            }
            if (popupTemplate != null)
            {
                popupTemplate.gameObject.SetActive(false);
            }
            SetAlpha(flash, 0f);
            SetAlpha(curtain, 0f);
            if (bannerGroup != null)
            {
                bannerGroup.alpha = 0f;
            }
            if (tipGroup != null)
            {
                tipGroup.alpha = 0f;
            }
            if (memorizeRoot != null)
            {
                memorizeRoot.SetActive(false);
            }
        }

        private static void Listen(Button button, UnityAction action)
        {
            if (button != null)
            {
                button.onClick.AddListener(action);
            }
        }

        #region Screens

        public override void UpdateGameState(GameState state)
        {
            shownState = state.BaseState;
            switch (state.BaseState)
            {
                case BaseGameState.Initialization:
                    HideMenu();
                    ShowScreen(titleScreen);
                    break;
                case BaseGameState.Running:
                    HideMenu();
                    ShowScreen(hudScreen);
                    break;
                case BaseGameState.Paused:
                    SetInteractable(ReturnToGame, true);
                    SetInteractable(SaveGame, true);
                    ShowScreen(hudScreen);
                    ShowMenu();
                    break;
                default:
                    HideMenu();
                    ShowScreen(resultsScreen);
                    break;
            }
        }

        public override void ShowSettings()
        {
            base.ShowSettings();
            if (shownState != BaseGameState.Paused)
            {
                ShowScreen(null);
            }
        }

        public override void HideSettings()
        {
            Cards?.PlayClick();
            if (shownState == BaseGameState.Paused)
            {
                ShowMenu();
                return;
            }
            HideMenu();
            ShowScreen(shownState == BaseGameState.Initialization ? titleScreen : resultsScreen);
            if (shownState == BaseGameState.Initialization)
            {
                Cards?.RefreshLevelSelect();
            }
        }

        private void ShowScreen(CanvasGroup screen)
        {
            SetScreen(titleScreen, screen == titleScreen);
            SetScreen(hudScreen, screen == hudScreen);
            SetScreen(resultsScreen, screen == resultsScreen);
        }

        private static void SetScreen(CanvasGroup screen, bool visible)
        {
            if (screen != null && screen.gameObject.activeSelf != visible)
            {
                screen.gameObject.SetActive(visible);
            }
        }

        protected override void OnPauseClicked()
        {
            Cards?.PlayClick();
            base.OnPauseClicked();
        }

        private void OnResetProgress()
        {
            if (Time.unscaledTime < resetConfirmUntil)
            {
                resetConfirmUntil = -1f;
                SetLabel(resetProgressLabel, "Reset progress");
                Cards?.ResetProgress();
                return;
            }
            Cards?.PlayClick();
            resetConfirmUntil = Time.unscaledTime + 3f;
            SetLabel(resetProgressLabel, MobilePlatform.Pick("Click again to reset", "Tap again to reset"));
        }

        /// <summary>Fades a curtain in the world colour over everything; it clears by itself.</summary>
        public void Curtain(Color color)
        {
            if (curtain == null)
            {
                return;
            }
            color.a = 1f;
            curtain.color = color;
            curtainAlpha = 1f;
            SetAlpha(curtain, 1f);
        }

        #endregion

        #region Level select

        public void ShowLevelSelect(List<WorldSummary> worlds, int selectedWorld, List<LevelSummary> worldLevels, int selectedLevel,
            int totalStars, int maxStars, int setsFound, bool canContinue)
        {
            for (int i = 0; i < worldTabs.Length; i++)
            {
                WorldTab tab = worldTabs[i];
                if (tab == null)
                {
                    continue;
                }
                bool used = i < worlds.Count;
                tab.gameObject.SetActive(used);
                if (used)
                {
                    tab.Show(worlds[i], worlds[i].Index == selectedWorld);
                }
            }
            levels.Clear();
            levels.AddRange(worldLevels);
            int perRow = Mathf.Max(1, levelsPerRow);
            int rows = Mathf.Max(1, Mathf.CeilToInt(levels.Count / (float)perRow));
            for (int i = 0; i < levelCards.Length; i++)
            {
                LevelCard card = levelCards[i];
                if (card == null)
                {
                    continue;
                }
                bool used = i < levels.Count;
                card.gameObject.SetActive(used);
                if (!used)
                {
                    continue;
                }
                card.Show(levels[i], levels[i].Index == selectedLevel, starFull, starEmpty);
                // Rows of up to three cards, every row centred.
                int row = i / perRow;
                int column = i % perRow;
                int inRow = Mathf.Min(perRow, levels.Count - row * perRow);
                var position = new Vector2((column - (inRow - 1) * 0.5f) * levelSpacing.x, ((rows - 1) * 0.5f - row) * levelSpacing.y);
                ((RectTransform)card.transform).anchoredPosition = position;
            }
            SetLabel(starsTotal, $"{totalStars} / {maxStars}");
            SetLabel(lifetimeText, setsFound > 0 ? $"{setsFound.ToString("N0", CultureInfo.InvariantCulture)} sets found" : "Welcome!");
            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(canContinue);
            }
            LevelSummary selected = levels.Find(level => level.Index == selectedLevel);
            if (selected.Title == null && levels.Count > 0)
            {
                selected = levels[0];
            }
            ShowDetails(selected);
        }

        private void ShowDetails(LevelSummary level)
        {
            if (detailHeader != null)
            {
                detailHeader.color = level.Accent;
            }
            SetLabel(detailWorld, level.World != null ? level.World.ToUpperInvariant() : string.Empty);
            SetLabel(detailTitle, level.Kind == LevelKind.Campaign ? $"{level.Number}. {level.Title}" : level.Title);
            SetLabel(detailMode, ModeName(level.Mode));
            if (detailModeIcon != null)
            {
                detailModeIcon.sprite = ModeIcon(level.Mode);
                detailModeIcon.color = level.AccentDark;
            }
            if (detailMode != null)
            {
                detailMode.color = level.AccentDark;
            }
            SetLabel(detailDescription, level.Description);
            bool isNew = !string.IsNullOrEmpty(level.Introduces);
            if (detailNewRoot != null)
            {
                detailNewRoot.SetActive(isNew);
            }
            SetLabel(detailNew, isNew ? $"NEW: {level.Introduces}" : string.Empty);
            SetLabel(detailBoard, string.IsNullOrEmpty(level.Twists) ? level.Board : $"{level.Board}   <color=#00000066>|</color>   {level.Twists}");
            bool campaign = level.Kind == LevelKind.Campaign;
            if (detailGoalsRoot != null)
            {
                detailGoalsRoot.SetActive(true);
            }
            for (int i = 0; i < detailGoals.Length; i++)
            {
                string goal = level.Goals != null && i < level.Goals.Length ? level.Goals[i] : string.Empty;
                if (detailGoals[i] != null)
                {
                    detailGoals[i].gameObject.SetActive(!string.IsNullOrEmpty(goal));
                    detailGoals[i].text = goal;
                }
                if (i < detailStars.Length && detailStars[i] != null)
                {
                    detailStars[i].gameObject.SetActive(campaign && !string.IsNullOrEmpty(goal));
                    detailStars[i].sprite = i < level.Stars ? starFull : starEmpty;
                }
            }
            if (level.Kind == LevelKind.Endless)
            {
                SetLabel(detailBest, level.EndlessBoards > 0
                    ? $"Best run: <b>{level.EndlessBoards}</b> boards, <b>{level.EndlessScore.ToString("N0", CultureInfo.InvariantCulture)}</b> points"
                    : "No record yet - set one!");
            }
            else if (campaign)
            {
                SetLabel(detailBest, level.BestScore > 0 ? $"Best score: <b>{level.BestScore.ToString("N0", CultureInfo.InvariantCulture)}</b>" : string.Empty);
            }
            else
            {
                SetLabel(detailBest, "Change the rules in Settings.");
            }
            if (playButton != null)
            {
                playButton.interactable = level.Unlocked;
            }
            SetLabel(playLabel, level.Unlocked ? "PLAY" : "LOCKED");
            if (!level.Unlocked && !string.IsNullOrEmpty(level.LockReason))
            {
                SetLabel(detailBest, level.LockReason);
            }
        }

        public static string ModeName(LevelMode mode)
        {
            switch (mode)
            {
                case LevelMode.TimeAttack: return "TIME ATTACK";
                case LevelMode.Survival: return "SURVIVAL";
                case LevelMode.MoveLimit: return "MOVE LIMIT";
                case LevelMode.Parade: return "PARADE";
                case LevelMode.Endless: return "ENDLESS";
                case LevelMode.FreePlay: return "FREE PLAY";
                default: return "CLASSIC";
            }
        }

        private Sprite ModeIcon(LevelMode mode)
        {
            int index = (int)mode;
            return modeIcons != null && index < modeIcons.Length ? modeIcons[index] : null;
        }

        #endregion

        #region HUD

        public void BeginRound(RoundHud setup)
        {
            hud = setup;
            SetLabel(levelText, setup.Title);
            SetLabel(modeText, setup.Versus ? setup.VersusLabel : setup.Endless ? $"BOARD {setup.Board}" : ModeName(setup.Mode));
            if (modeIcon != null)
            {
                modeIcon.sprite = ModeIcon(setup.Mode);
            }
            if (heartsRoot != null)
            {
                heartsRoot.SetActive(setup.Hearts > 0);
            }
            if (movesRoot != null)
            {
                movesRoot.SetActive(setup.MoveLimit > 0);
            }
            if (paradeRoot != null)
            {
                paradeRoot.SetActive(setup.Parade);
            }
            if (setsRoot != null)
            {
                setsRoot.SetActive(!setup.Parade && !setup.Versus);
            }
            // The scoreboard of a versus game takes the place of the score and the clock of a game for one.
            foreach (GameObject widget in soloWidgets)
            {
                if (widget != null)
                {
                    widget.SetActive(!setup.Versus);
                }
            }
            if (setup.Versus && comboBadge != null)
            {
                comboBadge.gameObject.SetActive(false);
            }
            if (versusHud != null && !setup.Versus)
            {
                versusHud.Hide();
            }
            shownScore = shownCombo = shownClock = shownHearts = shownMoves = shownSets = -1;
            bannerTime = -1f;
            tipTime = -1f;
            if (bannerGroup != null)
            {
                bannerGroup.alpha = 0f;
            }
            if (tipGroup != null)
            {
                tipGroup.alpha = 0f;
            }
            HideMemorize();
            ClearPopups();
        }

        /// <summary>The number of players the play button starts a game for; the button is off for levels only one can play.</summary>
        public void ShowPlayers(int players, bool allowed)
        {
            SetLabel(playersLabel, players <= 1 ? "1 PLAYER" : $"{players} PLAYERS");
            if (playersButton != null)
            {
                playersButton.interactable = allowed;
            }
        }

        /// <summary>The players of a versus game that starts. <paramref name="localSeat"/> is "you" online; -1 on one device.</summary>
        public void ShowVersus(IReadOnlyList<VersusSeat> seats, int localSeat)
        {
            versusHud?.Show(seats, localSeat);
        }

        public void UpdateVersus(IReadOnlyList<VersusSeat> seats, int currentSeat, float turnFraction, float secondsLeft)
        {
            versusHud?.Refresh(seats, currentSeat, turnFraction, secondsLeft);
        }

        /// <summary>Where the points of <paramref name="seat"/> show on the scoreboard, for popups.</summary>
        public Vector3 VersusChipPosition(int seat)
        {
            return versusHud != null ? versusHud.ChipPosition(seat) : transform.position;
        }

        public Color VersusSeatColor(int seat)
        {
            return versusHud != null ? versusHud.SeatColor(seat) : Color.white;
        }

        public void UpdateHud(int score, int combo, float clock, int heartsLeft, int movesLeft, int sets, int totalSets)
        {
            if (hud.Versus)
            {
                return;
            }
            if (score != shownScore)
            {
                shownScore = score;
                SetLabel(scoreText, score.ToString("N0", CultureInfo.InvariantCulture));
            }
            if (combo != shownCombo)
            {
                if (combo > shownCombo && combo >= 2)
                {
                    comboPunch = 1f;
                }
                shownCombo = combo;
                if (comboBadge != null)
                {
                    comboBadge.gameObject.SetActive(combo >= 2);
                }
                SetLabel(comboText, $"x{Mathf.Min(combo, MemoryRound.MaxComboMultiplier)}");
            }
            int seconds = hud.Countdown ? Mathf.CeilToInt(clock) : Mathf.FloorToInt(clock);
            if (seconds != shownClock)
            {
                shownClock = seconds;
                SetLabel(timerText, FormatTime(seconds));
                bool low = hud.Countdown && seconds <= 10;
                if (low && seconds > 0)
                {
                    timerPulse = 1f;
                }
                if (timerText != null)
                {
                    timerText.color = low ? new Color(1f, 0.35f, 0.3f) : Color.white;
                }
            }
            if (heartsLeft != shownHearts)
            {
                shownHearts = heartsLeft;
                for (int i = 0; i < hearts.Length; i++)
                {
                    if (hearts[i] == null)
                    {
                        continue;
                    }
                    hearts[i].gameObject.SetActive(i < hud.Hearts);
                    hearts[i].sprite = i < heartsLeft ? heartFull : heartEmpty;
                }
            }
            if (movesLeft != shownMoves)
            {
                shownMoves = movesLeft;
                SetLabel(movesText, movesLeft.ToString());
                if (movesText != null)
                {
                    movesText.color = movesLeft <= 3 ? new Color(1f, 0.35f, 0.3f) : Color.white;
                }
            }
            if (sets != shownSets)
            {
                shownSets = sets;
                SetLabel(setsText, $"{sets}<size=70%><color=#FFFFFFAA>/{totalSets}</color></size>");
            }
        }

        /// <summary>Shows the next animals of the parade, the first one highlighted.</summary>
        public void SetParade(IList<Sprite> upcoming)
        {
            for (int i = 0; i < paradeSlots.Length; i++)
            {
                Image slot = paradeSlots[i];
                if (slot == null)
                {
                    continue;
                }
                bool used = upcoming != null && i < upcoming.Count && upcoming[i] != null;
                slot.transform.parent.gameObject.SetActive(used);
                if (used)
                {
                    slot.sprite = upcoming[i];
                }
            }
        }

        public void ShowBanner(string text, string sub, Color color, float duration = 1.3f)
        {
            if (bannerText == null)
            {
                return;
            }
            bannerText.text = text;
            bannerText.color = color;
            SetLabel(bannerSub, sub ?? string.Empty);
            bannerTime = 0f;
            bannerLength = duration;
        }

        /// <summary>Words a tip for the way the game is played: clicks become taps on phones and tablets.</summary>
        private static string ForInput(string text)
        {
            return MobilePlatform.UsesTouch && text != null ? text.Replace("Click", "Tap").Replace("click", "tap") : text;
        }

        public void ShowTip(string text)
        {
            text = ForInput(text);
            if (tipText == null || string.IsNullOrEmpty(text))
            {
                return;
            }
            tipText.text = text;
            tipTime = 0f;
        }

        public void ShowMemorize(float fraction)
        {
            if (memorizeRoot != null && !memorizeRoot.activeSelf)
            {
                memorizeRoot.SetActive(true);
            }
            if (memorizeFill != null)
            {
                memorizeFill.fillAmount = Mathf.Clamp01(fraction);
            }
        }

        public void HideMemorize()
        {
            if (memorizeRoot != null)
            {
                memorizeRoot.SetActive(false);
            }
        }

        /// <summary>Floating text (points, time, combo) that rises from <paramref name="worldPosition"/>.</summary>
        public void ShowPopup(Vector3 worldPosition, string text, Color color, float size = 46f)
        {
            if (popupRoot == null || popupTemplate == null)
            {
                return;
            }
            TMP_Text label;
            if (popupPool.Count > 0)
            {
                label = popupPool[popupPool.Count - 1];
                popupPool.RemoveAt(popupPool.Count - 1);
            }
            else
            {
                label = Instantiate(popupTemplate, popupRoot);
            }
            label.gameObject.SetActive(true);
            label.text = text;
            label.color = color;
            label.fontSize = size;
            label.transform.SetAsLastSibling();
            Vector2 start = popupRoot.InverseTransformPoint(worldPosition);
            popups.Add(new Popup { text = label, start = start, life = 1.1f });
            label.rectTransform.anchoredPosition = start;
        }

        public void FlashScreen(Color color, float strength = 0.45f)
        {
            flashColor = color;
            flashAlpha = strength;
        }

        public void HeartLost()
        {
            heartShake = 1f;
        }

        public override void UpdateScore(float score)
        {
            // The HUD shows the score; the menu has no score text.
        }

        public override void UpdateLevel(int level)
        {
        }

        public override void UpdateTimer(float timer)
        {
        }

        public static string FormatTime(float seconds)
        {
            int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{total / 60}:{total % 60:00}";
        }

        private void ClearPopups()
        {
            foreach (Popup popup in popups)
            {
                popup.text.gameObject.SetActive(false);
                popupPool.Add(popup.text);
            }
            popups.Clear();
        }

        #endregion

        #region Results

        public void ShowResults(RoundResult result)
        {
            if (resultHeader != null)
            {
                resultHeader.color = result.Victory ? result.Accent : new Color(0.55f, 0.5f, 0.62f);
            }
            if (retryButton != null)
            {
                // The host starts the next game of an online room from the lobby.
                retryButton.gameObject.SetActive(!result.Online);
            }
            if (result.Versus)
            {
                ShowVersusResults(result);
                return;
            }
            FitResultStats(false);
            PlaceResultButtons(false);
            string title;
            if (result.Kind == LevelKind.Endless)
            {
                title = result.NewBest ? "NEW RECORD!" : "TIME'S UP!";
            }
            else if (result.Victory)
            {
                title = result.Stars >= 3 ? "PERFECT!" : result.Stars == 2 ? "GREAT JOB!" : "CLEARED!";
            }
            else
            {
                switch (result.End)
                {
                    case RoundEnd.OutOfHearts: title = "OUT OF HEARTS"; break;
                    case RoundEnd.OutOfMoves: title = "OUT OF MOVES"; break;
                    default: title = "TIME'S UP!"; break;
                }
            }
            SetLabel(resultTitle, title);
            SetLabel(resultSubtitle, result.LevelTitle);

            string score = result.Score.ToString("N0", CultureInfo.InvariantCulture);
            string bonus = result.Bonus > 0 ? $"  <size=70%><color=#2E9E4F>(+{result.Bonus.ToString("N0", CultureInfo.InvariantCulture)} bonus)</color></size>" : string.Empty;
            string time = FormatTime(result.Time);
            if (result.Kind == LevelKind.Endless)
            {
                SetLabel(resultStats, $"Boards cleared   <b>{result.Boards}</b>\nScore   <b>{score}</b>\nBest streak   <b>{result.BestCombo}</b>");
            }
            else
            {
                SetLabel(resultStats, $"Score   <b>{score}</b>{bonus}\n{(result.Countdown ? "Time left" : "Time")}   <b>{time}</b>      Mistakes   <b>{result.Mistakes}</b>\nMoves   <b>{result.Moves}</b>      Best streak   <b>{result.BestCombo}</b>");
            }
            bool campaign = result.Kind == LevelKind.Campaign;
            for (int i = 0; i < resultGoals.Length; i++)
            {
                string goal = result.Goals != null && i < result.Goals.Length ? result.Goals[i] : null;
                bool show = campaign && !string.IsNullOrEmpty(goal);
                bool reached = result.GoalsReached != null && i < result.GoalsReached.Length && result.GoalsReached[i];
                if (resultGoals[i] != null)
                {
                    resultGoals[i].gameObject.SetActive(show);
                    resultGoals[i].text = goal;
                    resultGoals[i].color = reached ? new Color(0.13f, 0.5f, 0.24f) : new Color(0.35f, 0.35f, 0.42f, 0.8f);
                }
                if (i < resultGoalIcons.Length && resultGoalIcons[i] != null)
                {
                    resultGoalIcons[i].gameObject.SetActive(show);
                    resultGoalIcons[i].sprite = reached ? goalDone : goalMissed;
                }
            }
            if (result.Kind == LevelKind.Endless)
            {
                SetLabel(resultBest, result.NewBest ? "You beat your best run!" : $"Best: {result.Best.ToString("N0", CultureInfo.InvariantCulture)} points");
            }
            else if (result.Victory && campaign)
            {
                SetLabel(resultBest, result.NewBest ? "New best score!" : $"Best: {result.Best.ToString("N0", CultureInfo.InvariantCulture)}");
            }
            else if (!result.Victory)
            {
                SetLabel(resultBest, ForInput(Tips[Random.Range(0, Tips.Length)]));
            }
            else
            {
                SetLabel(resultBest, string.Empty);
            }
            if (resultStarsRoot != null)
            {
                resultStarsRoot.gameObject.SetActive(campaign);
            }
            foreach (Image star in resultStars)
            {
                if (star != null)
                {
                    star.sprite = starEmpty;
                    star.transform.localScale = Vector3.one;
                }
            }
            resultStarCount = campaign && result.Victory ? result.Stars : 0;
            starsPopped = 0;
            resultsTime = 0f;
            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(result.HasNext);
            }
            if (resultPanel != null)
            {
                resultPanel.localScale = Vector3.one * 0.6f;
            }
        }

        /// <summary>The standings of a versus game shrink to fit (four players with long names); the stats of a game for one keep their size.</summary>
        private void FitResultStats(bool standings)
        {
            if (resultStats == null)
            {
                return;
            }
            if (resultStatsSize <= 0f)
            {
                resultStatsSize = resultStats.fontSize;
            }
            resultStats.enableAutoSizing = standings;
            resultStats.fontSizeMax = resultStatsSize;
            resultStats.fontSizeMin = 16f;
            resultStats.fontSize = resultStatsSize;
            resultStats.textWrappingMode = standings ? TextWrappingModes.NoWrap : TextWrappingModes.Normal;
        }

        /// <summary>
        /// The results of a game for one keep their buttons where the scene has them, with room for the next level on
        /// the right. A versus game has no next level: what is left (the levels, and the rematch at one device) sits in
        /// the middle.
        /// </summary>
        private void PlaceResultButtons(bool centred)
        {
            if (levelsButton == null || retryButton == null)
            {
                return;
            }
            var levels = (RectTransform)levelsButton.transform;
            var retry = (RectTransform)retryButton.transform;
            if (!resultButtonsKnown)
            {
                resultButtonsKnown = true;
                levelsRest = levels.anchoredPosition;
                retryRest = retry.anchoredPosition;
            }
            float half = retryButton.gameObject.activeSelf ? (retryRest.x - levelsRest.x) * 0.5f : 0f;
            levels.anchoredPosition = centred ? new Vector2(-half, levelsRest.y) : levelsRest;
            retry.anchoredPosition = centred ? new Vector2(half, retryRest.y) : retryRest;
        }

        /// <summary>The results of a versus game: who won, and the standings in place of the stars and the goals.</summary>
        private void ShowVersusResults(RoundResult result)
        {
            SetLabel(resultTitle, result.VersusTitle);
            SetLabel(resultSubtitle, result.LevelTitle);
            SetLabel(resultStats, result.VersusStandings);
            FitResultStats(true);
            PlaceResultButtons(true);
            SetLabel(resultBest, result.Online ? "Back to the room for another game." : string.Empty);
            foreach (TMP_Text goal in resultGoals)
            {
                if (goal != null)
                {
                    goal.gameObject.SetActive(false);
                }
            }
            foreach (Image icon in resultGoalIcons)
            {
                if (icon != null)
                {
                    icon.gameObject.SetActive(false);
                }
            }
            if (resultStarsRoot != null)
            {
                resultStarsRoot.gameObject.SetActive(false);
            }
            resultStarCount = 0;
            starsPopped = 0;
            resultsTime = 0f;
            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(false);
            }
            if (resultPanel != null)
            {
                resultPanel.localScale = Vector3.one * 0.6f;
            }
        }

        #endregion

        #region Animation

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            titleTime += dt;

            if (logo != null && titleScreen != null && titleScreen.gameObject.activeInHierarchy)
            {
                logo.anchoredPosition = logoRest + new Vector2(0f, Mathf.Sin(titleTime * 1.6f) * 6f);
                logo.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(titleTime * 1.1f) * 1.5f);
                for (int i = 0; i < titleMascots.Length; i++)
                {
                    if (titleMascots[i] == null)
                    {
                        continue;
                    }
                    float hop = Mathf.Max(0f, Mathf.Sin(titleTime * 2.4f + i * 1.3f));
                    titleMascots[i].localScale = new Vector3(1f + hop * 0.04f, 1f - hop * 0.04f + hop * hop * 0.1f, 1f);
                    titleMascots[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(titleTime * 1.7f + i) * 7f);
                }
            }

            comboPunch = Mathf.MoveTowards(comboPunch, 0f, dt * 3f);
            if (comboBadge != null)
            {
                comboBadge.localScale = Vector3.one * (1f + comboPunch * 0.45f);
                comboBadge.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(comboPunch * 20f) * 10f * comboPunch);
            }

            timerPulse = Mathf.MoveTowards(timerPulse, 0f, dt * 2.5f);
            if (timerRoot != null)
            {
                timerRoot.transform.localScale = Vector3.one * (1f + timerPulse * 0.18f);
            }

            heartShake = Mathf.MoveTowards(heartShake, 0f, dt * 2.5f);
            if (heartsRoot != null)
            {
                ((RectTransform)heartsRoot.transform).anchoredPosition = heartsRest + new Vector2(Mathf.Sin(Time.unscaledTime * 55f) * 10f * heartShake, 0f);
            }

            if (bannerGroup != null && bannerTime >= 0f)
            {
                bannerTime += dt;
                float t = bannerTime / Mathf.Max(0.1f, bannerLength);
                float appear = Mathf.Clamp01(bannerTime / 0.18f);
                bannerGroup.alpha = t < 0.8f ? appear : Mathf.Clamp01(1f - (t - 0.8f) / 0.2f);
                bannerGroup.transform.localScale = Vector3.one * Mathf.Lerp(1.6f, 1f, Tween.OutBack(appear));
                if (t >= 1f)
                {
                    bannerTime = -1f;
                    bannerGroup.alpha = 0f;
                }
            }

            if (tipGroup != null && tipTime >= 0f)
            {
                tipTime += dt;
                tipGroup.alpha = tipTime < 0.3f ? tipTime / 0.3f : tipTime < 5f ? 1f : Mathf.Clamp01(1f - (tipTime - 5f) / 0.6f);
                if (tipTime > 5.6f)
                {
                    tipTime = -1f;
                    tipGroup.alpha = 0f;
                }
            }

            for (int i = popups.Count - 1; i >= 0; i--)
            {
                Popup popup = popups[i];
                popup.age += dt;
                float t = popup.age / popup.life;
                popup.text.rectTransform.anchoredPosition = popup.start + new Vector2(0f, Tween.OutCubic(t) * 55f);
                popup.text.alpha = t < 0.7f ? 1f : 1f - (t - 0.7f) / 0.3f;
                popup.text.transform.localScale = Vector3.one * (t < 0.15f ? Mathf.Lerp(0.5f, 1.15f, t / 0.15f) : Mathf.Lerp(1.15f, 1f, (t - 0.15f) / 0.85f));
                if (t >= 1f)
                {
                    popup.text.gameObject.SetActive(false);
                    popupPool.Add(popup.text);
                    popups.RemoveAt(i);
                }
            }

            flashAlpha = Mathf.MoveTowards(flashAlpha, 0f, dt * 2.4f);
            if (flash != null)
            {
                Color color = flashColor;
                color.a = flashAlpha;
                flash.color = color;
                flash.enabled = flashAlpha > 0.001f;
            }

            curtainAlpha = Mathf.MoveTowards(curtainAlpha, 0f, dt * 2.8f);
            SetAlpha(curtain, curtainAlpha);

            if (resetConfirmUntil > 0f && Time.unscaledTime > resetConfirmUntil)
            {
                resetConfirmUntil = -1f;
                SetLabel(resetProgressLabel, "Reset progress");
            }

            AnimateResults(dt);
        }

        private void AnimateResults(float dt)
        {
            if (resultsTime < 0f)
            {
                return;
            }
            resultsTime += dt;
            if (resultPanel != null)
            {
                float t = Mathf.Clamp01(resultsTime / 0.35f);
                resultPanel.localScale = Vector3.one * Mathf.LerpUnclamped(0.6f, 1f, Tween.OutBack(t));
            }
            while (starsPopped < resultStarCount && starsPopped < resultStars.Length && resultsTime > 0.55f + starsPopped * 0.38f)
            {
                Image star = resultStars[starsPopped];
                if (star != null)
                {
                    star.sprite = starFull;
                }
                Cards?.PlayStar(starsPopped, star != null ? star.transform.position : Vector3.zero);
                starsPopped++;
            }
            for (int i = 0; i < resultStars.Length; i++)
            {
                if (resultStars[i] == null || i >= starsPopped)
                {
                    continue;
                }
                float age = resultsTime - (0.55f + i * 0.38f);
                float pop = age < 0.3f ? Mathf.Lerp(1.8f, 1f, Tween.OutBack(age / 0.3f)) : 1f;
                resultStars[i].transform.localScale = Vector3.one * pop;
                resultStars[i].transform.localRotation = Quaternion.Euler(0f, 0f, age < 0.3f ? (1f - age / 0.3f) * 40f : 0f);
            }
            if (resultsTime > 3f)
            {
                resultsTime = -1f;
            }
        }

        #endregion

        private static void SetLabel(TMP_Text label, string text)
        {
            if (label != null)
            {
                label.text = text;
            }
        }

        private static void SetAlpha(Graphic graphic, float alpha)
        {
            if (graphic == null)
            {
                return;
            }
            Color color = graphic.color;
            if (!Mathf.Approximately(color.a, alpha))
            {
                color.a = alpha;
                graphic.color = color;
            }
            bool visible = alpha > 0.001f;
            if (graphic.enabled != visible)
            {
                graphic.enabled = visible;
            }
        }
    }
}
