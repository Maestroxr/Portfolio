using System;
using System.Collections.Generic;
using Gamebox.Online;
using Gamebox;
using Gamebox.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Portfolio.EndlessRunner
{
    /// <summary>
    /// Interface of the Endless Runner: the level select, the HUD of a run and the results screen. The shared menu of
    /// <see cref="GameUI"/> serves as the pause menu and hosts the settings panel. A race against the runners of an
    /// online room adds the scoreboard of the <see cref="RaceHud"/> and shows the standings as its results.
    /// </summary>
    public class RunnerUI : GameUI
    {
        [Serializable]
        internal class PowerUpSlot
        {
            public PowerUpType type;
            public GameObject root;
            public Image fill;
        }

        private static readonly string[] Tips =
        {
            RunnerText.Key("Tip: coins in an arc show you when to jump."),
            RunnerText.Key("Tip: press DOWN in the air to dive and slide as you land."),
            RunnerText.Key("Tip: ramps lead onto the wagons - the safest lane is often up top."),
            RunnerText.Key("Tip: a shield soaks up one crash."),
            RunnerText.Key("Tip: switching lanes into the side of an obstacle only bounces you back."),
            RunnerText.Key("Tip: super jump lets you hop onto wagons without a ramp.")
        };

        [Header("Screens")]
        [SerializeField] internal CanvasGroup titleScreen;
        [SerializeField] internal CanvasGroup hudScreen;
        [SerializeField] internal CanvasGroup resultsScreen;
        [SerializeField] internal Image curtain;

        [Header("Level select")]
        [SerializeField] internal LevelCard[] levelCards = new LevelCard[0];
        [SerializeField] internal TMP_Text detailTitle;
        [SerializeField] internal TMP_Text detailWorld;
        [SerializeField] internal TMP_Text detailDescription;
        [SerializeField] internal TMP_Text detailGoals;
        [SerializeField] internal Image[] detailStars = new Image[0];
        [SerializeField] internal TMP_Text starsTotal;
        [SerializeField] internal Button playButton;
        [SerializeField] internal TMP_Text playLabel;
        [SerializeField] internal Button multiplayerButton;
        [SerializeField] internal Button titleSettingsButton;
        [SerializeField] internal Button titleExitButton;
        [SerializeField] internal Button resetProgressButton;
        [SerializeField] internal TMP_Text resetProgressLabel;

        [Header("HUD")]
        [SerializeField] internal TMP_Text coinsText;
        [SerializeField] internal RectTransform coinsIcon;
        [SerializeField] internal TMP_Text scoreText;
        [SerializeField] internal TMP_Text distanceText;
        [SerializeField] internal TMP_Text levelText;
        [SerializeField] internal GameObject progressRoot;
        [SerializeField] internal Image progressFill;
        [SerializeField] internal RectTransform progressMarker;
        [SerializeField] internal RectTransform heartsRoot;
        [SerializeField] internal Image[] hearts = new Image[0];
        [SerializeField] internal PowerUpSlot[] powerUpSlots = new PowerUpSlot[0];
        [SerializeField] internal GameObject multiplierBadge;
        [SerializeField] internal Button pauseButton;
        [SerializeField] internal TMP_Text countdownText;
        [SerializeField] internal TMP_Text toastText;
        [SerializeField] internal CanvasGroup hintGroup;
        [SerializeField] internal TMP_Text hintText;
        [SerializeField] internal Image damageFlash;
        [SerializeField] internal RaceHud raceHud;

        [Header("Results")]
        [SerializeField] internal TMP_Text resultTitle;
        [SerializeField] internal TMP_Text resultSubtitle;
        [SerializeField] internal TMP_Text resultStats;
        [SerializeField] internal TMP_Text resultGoals;
        [SerializeField] internal RectTransform resultStarsRoot;
        [SerializeField] internal Image[] resultStars = new Image[0];
        [SerializeField] internal Button nextButton;
        [SerializeField] internal Button retryButton;
        [SerializeField] internal Button levelsButton;
        [SerializeField] internal TMP_Text levelsLabel;

        [Header("Sprites")]
        [SerializeField] internal Sprite starFull;
        [SerializeField] internal Sprite starEmpty;
        [SerializeField] internal Sprite heartFull;
        [SerializeField] internal Sprite heartEmpty;

        [Header("Theme")]
        [SerializeField] internal TMP_Text logoEndless;
        [SerializeField] internal TMP_Text logoRunner;
        [Tooltip("The film grain over the screen; it flickers while its colour is visible.")]
        [SerializeField] internal Image grain;
        [Tooltip("The shared menu's re-skinning, applied before the pause menu buttons get their colours.")]
        [SerializeField] internal ThemedMenu themedMenu;

        private BaseGameState shownState = BaseGameState.Initialization;
        private readonly List<LevelSummary> summaries = new List<LevelSummary>();
        private bool endlessRun;
        private float runLength;
        private int shownCoins = -1;
        private int shownGoal = -1;
        private int shownScore = -1;
        private int shownDistance = -1;
        private int shownHearts = -1;
        private int shownMaxHearts = -1;
        private float coinPunch;
        private float countdownTime = -1f;
        private float toastTime = -1f;
        private float hintTime = -1f;
        private float flash;
        private float heartShake;
        private float curtainAlpha;
        private float resultsTime = -1f;
        private int resultStarCount;
        private int starsPopped;
        private float resetConfirmUntil = -1f;
        private Vector2 heartsRest;
        private float resultStatsSize = 36f;
        private RunnerGameTheme look;
        // What the screens show, kept to write it again in another language.
        private int shownSelected = -1;
        private int shownTotalStars;
        private int shownMaxStars;
        private string runTitle = string.Empty;
        private int runLevelIndex;
        private RunResult shownResult;
        private bool hasResult;
        private int tipIndex;

        private RunnerGameManager Runner => Manager as RunnerGameManager;

        /// <summary>The theme the interface is drawn in, once the manager applied one.</summary>
        public RunnerGameTheme Look => look;

        protected override void Awake()
        {
            base.Awake();
            Listen(playButton, () => Runner?.PlaySelectedLevel());
            Listen(multiplayerButton, () => Runner?.OpenOnline());
            Listen(titleSettingsButton, ShowSettings);
            Listen(titleExitButton, () => GameManager?.ExitGame());
            Listen(resetProgressButton, OnResetProgress);
            Listen(pauseButton, OnPauseClicked);
            Listen(nextButton, () => Runner?.PlayNextLevel());
            Listen(retryButton, () => Runner?.RetryLevel());
            Listen(levelsButton, () => Runner?.ReturnToLevelSelect());
            foreach (LevelCard card in levelCards)
            {
                if (card != null)
                {
                    card.Clicked += index => Runner?.SelectLevel(index);
                }
            }
            if (heartsRoot != null)
            {
                heartsRest = heartsRoot.anchoredPosition;
            }
            if (resultStats != null)
            {
                resultStatsSize = resultStats.fontSize;
            }
            SetAlpha(damageFlash, 0f);
            SetAlpha(curtain, 0f);
            if (countdownText != null)
            {
                countdownText.gameObject.SetActive(false);
            }
            if (toastText != null)
            {
                toastText.gameObject.SetActive(false);
            }
            if (hintGroup != null)
            {
                hintGroup.alpha = 0f;
            }
            if (raceHud != null)
            {
                raceHud.Hide();
            }
        }

        private static void Listen(Button button, UnityAction action)
        {
            if (button != null)
            {
                button.onClick.AddListener(action);
            }
        }

        #region Theme

        /// <summary>
        /// Draws the interface in the look of <paramref name="theme"/>: the star and heart sprites, the gradients of
        /// the title, the pause menu's skin and the colours of its buttons, and the fonts of the settings fields. The
        /// panels, icons and texts of the canvas follow the theme by themselves (they carry themed components); the
        /// level select is drawn again when the manager shows it.
        /// </summary>
        public void ApplyTheme(RunnerGameTheme theme)
        {
            if (theme == null)
            {
                return;
            }
            look = theme;
            InterfaceSprites sprites = theme.Sprites;
            InterfaceColors colors = theme.Colors;
            starFull = sprites.star != null ? sprites.star : starFull;
            starEmpty = sprites.starEmpty != null ? sprites.starEmpty : starEmpty;
            heartFull = sprites.heart != null ? sprites.heart : heartFull;
            heartEmpty = sprites.heartEmpty != null ? sprites.heartEmpty : heartEmpty;
            Gradient(logoEndless, colors.logoEndlessTop, colors.logoEndlessBottom);
            Gradient(logoRunner, colors.logoRunnerTop, colors.logoRunnerBottom);
            if (themedMenu != null)
            {
                themedMenu.Refresh();
            }
            foreach (Button button in GetComponentsInChildren<Button>(true))
            {
                // The skin draws every button in the theme's button shape; the pause menu's four get their own colours,
                // the rest (the settings panel's, the arrows of the choices) the theme's neutral one.
                if (button != PauseButton)
                {
                    Tint(button, colors.menuSettings);
                }
            }
            Tint(ReturnToGame, colors.menuResume);
            Tint(StartNewGame, colors.menuRestart);
            Tint(SettingsButton, colors.menuSettings);
            Tint(ExitButton, colors.menuQuit);
            foreach (TMP_InputField field in GetComponentsInChildren<TMP_InputField>(true))
            {
                theme.Fonts.Apply(field.textComponent, TextRole.Body);
                theme.Fonts.Apply(field.placeholder as TMP_Text, TextRole.Body);
            }
            StyleSettingsPanel(colors);
            Transform input = MenuSkin.Find(transform, "InputPanel");
            if (input != null)
            {
                // The labels and values of the settings rows, which the skin does not reach in this game's layout.
                foreach (TMP_Text text in input.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (text.GetComponentInParent<TMP_InputField>(true) == null && text.GetComponentInParent<Button>(true) == null)
                    {
                        theme.Fonts.Apply(text, TextRole.Body);
                        if (MenuSkin.IsSet(theme.Menu.textColor))
                        {
                            text.color = text.name == "Value" && MenuSkin.IsSet(theme.Menu.valueColor) ? theme.Menu.valueColor : theme.Menu.textColor;
                        }
                    }
                }
            }
            shownHearts = shownMaxHearts = -1;
        }

        /// <summary><paramref name="key"/> in the words of the theme the interface is drawn in, in the language shown.</summary>
        private string Say(string key)
        {
            return RunnerText.Say(look, key);
        }

        private string SayF(string key, params object[] args)
        {
            return RunnerText.SayF(look, key, args);
        }

        /// <summary>
        /// The settings panel's own parts the menu skin does not reach: the boxes and words of the input fields and the
        /// legacy words of its buttons. Their own colours are kept the first time, and come back with a theme that
        /// leaves these colours clear.
        /// </summary>
        private void StyleSettingsPanel(InterfaceColors colors)
        {
            foreach (TMP_InputField field in GetComponentsInChildren<TMP_InputField>(true))
            {
                Recolor(field.targetGraphic, colors.field);
                Recolor(field.textComponent, colors.fieldText);
            }
            foreach (Text words in GetComponentsInChildren<Text>(true))
            {
                Button owner = words.GetComponentInParent<Button>(true);
                // The custom settings button's words are green or red by the state of the settings: the panel's own.
                if (owner != null && owner.name != "Custom Settings")
                {
                    Recolor(words, colors.menuText);
                }
            }
        }

        private readonly Dictionary<Graphic, Color> ownColors = new Dictionary<Graphic, Color>();

        private void Recolor(Graphic graphic, Color color)
        {
            if (graphic == null)
            {
                return;
            }
            if (!ownColors.TryGetValue(graphic, out Color own))
            {
                own = graphic.color;
                ownColors[graphic] = own;
            }
            graphic.color = MenuSkin.IsSet(color) ? color : own;
        }

        private static void Gradient(TMP_Text text, Color top, Color bottom)
        {
            if (text == null)
            {
                return;
            }
            text.enableVertexGradient = true;
            text.colorGradient = new VertexGradient(top, top, bottom, bottom);
        }

        private static void Tint(Button button, Color color)
        {
            if (button != null && button.targetGraphic != null)
            {
                button.targetGraphic.color = color;
            }
        }

        #endregion

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
                    SetInteractable(SaveGame, false);
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
            if (shownState == BaseGameState.Paused)
            {
                ShowMenu();
                return;
            }
            HideMenu();
            ShowScreen(shownState == BaseGameState.Initialization ? titleScreen : resultsScreen);
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

        private void OnResetProgress()
        {
            if (Time.unscaledTime < resetConfirmUntil)
            {
                resetConfirmUntil = -1f;
                WriteResetLabel();
                Runner?.ResetProgress();
                return;
            }
            resetConfirmUntil = Time.unscaledTime + 3f;
            WriteResetLabel();
        }

        private void WriteResetLabel()
        {
            SetLabel(resetProgressLabel, resetConfirmUntil > 0f
                ? MobilePlatform.Pick(RunnerText.T("Click again to reset"), RunnerText.T("Tap again to reset"))
                : RunnerText.T("Reset progress"));
        }

        /// <summary>
        /// The language changed: the level select, the line over the HUD, the results and the scoreboard are written
        /// again (the fixed words of the canvas follow by themselves through its LocalizedTexts).
        /// </summary>
        public override void RefreshTexts()
        {
            base.RefreshTexts();
            WriteResetLabel();
            if (summaries.Count > 0)
            {
                WriteLevelSelect();
            }
            WriteRunTitle();
            shownDistance = -1;
            if (hasResult)
            {
                WriteResults(shownResult);
            }
            if (raceHud != null)
            {
                raceHud.Redraw();
            }
        }

        #endregion

        #region Level select

        public void ShowLevelSelect(List<LevelSummary> levels, int selected, int totalStars, int maxStars)
        {
            summaries.Clear();
            summaries.AddRange(levels);
            shownSelected = selected;
            shownTotalStars = totalStars;
            shownMaxStars = maxStars;
            WriteLevelSelect();
            curtainAlpha = 0.55f;
        }

        private void WriteLevelSelect()
        {
            int selected = shownSelected;
            for (int i = 0; i < levelCards.Length; i++)
            {
                LevelCard card = levelCards[i];
                if (card == null)
                {
                    continue;
                }
                bool used = i < summaries.Count;
                card.gameObject.SetActive(used);
                if (used)
                {
                    card.Show(summaries[i], summaries[i].Index == selected, starFull, starEmpty, look != null ? look.Colors.locked : (Color?)null);
                }
            }
            SetLabel(starsTotal, $"{shownTotalStars} / {shownMaxStars}");
            WriteResetLabel();
            LevelSummary summary = summaries.Find(level => level.Index == selected);
            ShowDetails(summary);
        }

        private void ShowDetails(LevelSummary summary)
        {
            string title = RunnerText.T(summary.Title);
            SetLabel(detailTitle, summary.Endless ? title : $"{summary.Index + 1}. {title}");
            SetLabel(detailWorld, RunnerText.T(summary.World));
            if (detailWorld != null)
            {
                detailWorld.color = Color.Lerp(summary.Accent, Color.white, 0.35f);
            }
            SetLabel(detailDescription, Say(summary.Description));
            if (summary.Endless)
            {
                string best = summary.BestDistance > 0f
                    ? RunnerText.F("Best run: <b>{0:0} m</b>  ({1} points)", summary.BestDistance, summary.BestScore)
                    : RunnerText.T("No record yet - set one!");
                SetLabel(detailGoals, SayF("Run as far as you can.\nThe world changes as you go.\n{0}", best));
            }
            else
            {
                SetLabel(detailGoals,
                    SayF("- Reach the finish ({0:0} m)\n- Collect {1} coins\n- Finish without a scratch", summary.Length, summary.CoinGoal));
            }
            for (int i = 0; i < detailStars.Length; i++)
            {
                if (detailStars[i] == null)
                {
                    continue;
                }
                detailStars[i].gameObject.SetActive(!summary.Endless);
                detailStars[i].sprite = i < summary.Stars ? starFull : starEmpty;
            }
            if (playButton != null)
            {
                playButton.interactable = summary.Unlocked;
            }
            SetLabel(playLabel, summary.Unlocked ? RunnerText.T("PLAY") : RunnerText.T("LOCKED"));
        }

        #endregion

        #region HUD

        public void BeginRun(string title, int levelIndex, bool endless, int maxHearts, float lengthOrBest)
        {
            endlessRun = endless;
            runLength = lengthOrBest;
            runTitle = title ?? string.Empty;
            runLevelIndex = levelIndex;
            hasResult = false;
            WriteRunTitle();
            if (progressRoot != null)
            {
                progressRoot.SetActive(!endless);
            }
            shownCoins = shownGoal = shownScore = shownDistance = shownHearts = shownMaxHearts = -1;
            countdownTime = -1f;
            toastTime = -1f;
            hintTime = -1f;
            flash = 0f;
            SetAlpha(damageFlash, 0f);
            if (toastText != null)
            {
                toastText.gameObject.SetActive(false);
            }
            if (hintGroup != null)
            {
                hintGroup.alpha = 0f;
            }
            foreach (PowerUpSlot slot in powerUpSlots)
            {
                if (slot != null && slot.root != null)
                {
                    slot.root.SetActive(false);
                }
            }
            UpdateHearts(maxHearts, maxHearts);
        }

        /// <summary>The line over the HUD: the level's number and name (English in the argument of <see cref="BeginRun"/>).</summary>
        private void WriteRunTitle()
        {
            SetLabel(levelText, endlessRun
                ? RunnerText.T("ENDLESS RUN")
                : RunnerText.F("LEVEL {0}  -  {1}", runLevelIndex + 1, RunnerText.T(runTitle).ToUpperInvariant()));
        }

        public void UpdateRun(int coins, int coinGoal, int score, float distance, float progress01, int heartsLeft, int maxHearts, bool multiplier)
        {
            if (coins != shownCoins || coinGoal != shownGoal)
            {
                shownCoins = coins;
                shownGoal = coinGoal;
                if (coinsText != null)
                {
                    coinsText.text = coinGoal > 0 ? $"{coins}<size=55%><color=#FFFFFFAA> / {coinGoal}</color></size>" : coins.ToString();
                    coinsText.color = coinGoal > 0 && coins >= coinGoal ? new Color(0.55f, 1f, 0.45f) : Color.white;
                }
            }
            if (score != shownScore)
            {
                shownScore = score;
                SetLabel(scoreText, score.ToString());
            }
            int meters = Mathf.FloorToInt(distance);
            if (meters != shownDistance)
            {
                shownDistance = meters;
                if (endlessRun)
                {
                    SetLabel(distanceText, runLength > 0f
                        ? RunnerText.F("{0} m  <size=60%>best {1:0} m</size>", meters, runLength)
                        : RunnerText.F("{0} m", meters));
                }
                else
                {
                    SetLabel(distanceText, RunnerText.F("{0} m", meters));
                }
            }
            if (progressFill != null)
            {
                progressFill.fillAmount = progress01;
            }
            if (progressMarker != null && progressMarker.parent is RectTransform bar)
            {
                progressMarker.anchoredPosition = new Vector2(bar.rect.width * progress01, progressMarker.anchoredPosition.y);
            }
            UpdateHearts(heartsLeft, maxHearts);
            if (multiplierBadge != null && multiplierBadge.activeSelf != multiplier)
            {
                multiplierBadge.SetActive(multiplier);
            }
        }

        private void UpdateHearts(int heartsLeft, int maxHearts)
        {
            if (heartsLeft == shownHearts && maxHearts == shownMaxHearts)
            {
                return;
            }
            shownHearts = heartsLeft;
            shownMaxHearts = maxHearts;
            for (int i = 0; i < hearts.Length; i++)
            {
                if (hearts[i] == null)
                {
                    continue;
                }
                hearts[i].gameObject.SetActive(i < maxHearts);
                hearts[i].sprite = i < heartsLeft ? heartFull : heartEmpty;
            }
        }

        public void SetPowerUp(PowerUpType type, float fraction)
        {
            foreach (PowerUpSlot slot in powerUpSlots)
            {
                if (slot == null || slot.type != type)
                {
                    continue;
                }
                bool active = fraction > 0f;
                if (slot.root != null && slot.root.activeSelf != active)
                {
                    slot.root.SetActive(active);
                }
                if (slot.fill != null)
                {
                    slot.fill.fillAmount = fraction;
                }
            }
        }

        public void ShowCountdown(string text)
        {
            if (countdownText == null)
            {
                return;
            }
            countdownText.text = text;
            countdownText.gameObject.SetActive(true);
            countdownTime = 0f;
        }

        public void Toast(string text, Color color)
        {
            if (toastText == null)
            {
                return;
            }
            // The words are the caller's, in the language and the theme's words already (see RunnerText.Say).
            toastText.text = text;
            toastText.color = Color.Lerp(color, Color.white, 0.25f);
            toastText.gameObject.SetActive(true);
            toastTime = 0f;
        }

        /// <summary>A hint over the track for a while: <paramref name="key"/> is its English (see <see cref="RunnerText.Say(string)"/>).</summary>
        public void ShowHint(string key)
        {
            SetLabel(hintText, Say(key));
            hintTime = 0f;
        }

        /// <summary>A line in the place of the hints that stays until the next hint or run: what the player is waiting for.</summary>
        public void ShowNotice(string text)
        {
            SetLabel(hintText, text);
            hintTime = -1f;
            if (hintGroup != null)
            {
                hintGroup.alpha = 1f;
            }
        }

        /// <summary>Shows the scoreboard of a race of <paramref name="runners"/>.</summary>
        public void ShowRace(int runners)
        {
            if (raceHud != null)
            {
                raceHud.Show(runners);
            }
        }

        /// <summary>The runners of the race in the order of their places.</summary>
        public void UpdateRace(IReadOnlyList<Racer> ranking, Color[] colors)
        {
            if (raceHud != null && raceHud.gameObject.activeSelf)
            {
                raceHud.Refresh(ranking, colors);
            }
        }

        public void HideRace()
        {
            if (raceHud != null)
            {
                raceHud.Hide();
            }
        }

        public void PunchCoins()
        {
            coinPunch = 1f;
        }

        public void HeartLost(int heartsLeft, int maxHearts)
        {
            heartShake = 1f;
            flash = 0.55f;
            UpdateHearts(heartsLeft, maxHearts);
        }

        public override void UpdateScore(float score)
        {
            // The HUD shows the score; the shared menu's score text is not used by this game.
        }

        public override void UpdateLevel(int level)
        {
        }

        #endregion

        #region Results

        public void ShowResults(RunResult result)
        {
            shownResult = result;
            hasResult = true;
            tipIndex = UnityEngine.Random.Range(0, Tips.Length);
            WriteResults(result);
            if (resultStarsRoot != null)
            {
                resultStarsRoot.gameObject.SetActive(!result.Endless && !result.Online);
            }
            foreach (Image star in resultStars)
            {
                if (star != null)
                {
                    star.sprite = starEmpty;
                    star.transform.localScale = Vector3.one;
                }
            }
            starsPopped = 0;
            resultsTime = 0f;
            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(result.HasNextLevel);
            }
        }

        /// <summary>The words of the results screen, in the language shown and the theme's words.</summary>
        private void WriteResults(RunResult result)
        {
            SetLabel(levelsLabel, result.Online ? RunnerText.T("Room") : RunnerText.T("Levels"));
            if (retryButton != null)
            {
                retryButton.gameObject.SetActive(!result.Online);
            }
            if (resultStats != null)
            {
                // The standings are a line a runner, however long the names are.
                resultStats.enableAutoSizing = result.Online;
                resultStats.textWrappingMode = result.Online ? TextWrappingModes.NoWrap : TextWrappingModes.Normal;
                if (result.Online)
                {
                    resultStats.fontSizeMax = resultStatsSize;
                    resultStats.fontSizeMin = 16f;
                }
                else
                {
                    resultStats.fontSize = resultStatsSize;
                }
            }
            string levelTitle = RunnerText.T(result.LevelTitle);
            if (result.Online)
            {
                // A race: the places are the server's, and the next one starts from the room.
                SetLabel(resultTitle, result.Runners < 2 ? RunnerText.T("RUN OVER") : result.Place == 1 ? RunnerText.T("YOU WIN!")
                    : RunnerText.F("{0} PLACE", Standings.Ordinal(result.Place).ToUpperInvariant(), result.Place));
                SetLabel(resultSubtitle, result.Runners < 2 ? RunnerText.F("{0} - online", levelTitle) : RunnerText.F("{0} - race of {1}", levelTitle, result.Runners));
                SetLabel(resultStats, result.Standings);
                SetLabel(resultGoals, SayF("You ran <b>{0:0} m</b> and took <b>{1}</b> coins.\nThe host starts the next race from the room.", result.Distance, result.Coins));
                resultStarCount = 0;
            }
            else if (result.Endless)
            {
                SetLabel(resultTitle, result.NewBest ? RunnerText.T("NEW RECORD!") : RunnerText.T("RUN OVER"));
                SetLabel(resultSubtitle, RunnerText.T("Endless Run"));
                SetLabel(resultStats, SayF("Distance   <b>{0:0} m</b>\nCoins   <b>{1}</b>\nScore   <b>{2}</b>", result.Distance, result.Coins, result.Score));
                SetLabel(resultGoals, result.BestDistance > 0f ? RunnerText.F("Best run: {0:0} m", result.BestDistance) : string.Empty);
                resultStarCount = 0;
            }
            else if (result.Victory)
            {
                SetLabel(resultTitle, RunnerText.T("LEVEL COMPLETE!"));
                SetLabel(resultSubtitle, levelTitle);
                SetLabel(resultStats, SayF("Coins   <b>{0}</b>\nScore   <b>{1}</b>", result.Coins, result.Score)
                    + (result.NewBest ? $"  <color=#FFD84A>{RunnerText.T("best!")}</color>" : string.Empty));
                SetLabel(resultGoals,
                    Goal(true, Say("Reached the finish")) + "\n" +
                    Goal(result.CoinGoalReached, SayF("Collected {0} coins", result.CoinGoal)) + "\n" +
                    Goal(result.Flawless, Say("Finished without a scratch")));
                resultStarCount = result.Stars;
            }
            else
            {
                SetLabel(resultTitle, RunnerText.T("OUCH!"));
                SetLabel(resultSubtitle, levelTitle);
                SetLabel(resultStats, SayF("You made it <b>{0:0} m</b>\nCoins   <b>{1}</b>", result.Distance, result.Coins));
                SetLabel(resultGoals, Say(Tips[Mathf.Clamp(tipIndex, 0, Tips.Length - 1)]));
                resultStarCount = 0;
            }
        }

        /// <summary>The standings of a race again, in the language shown now.</summary>
        public void UpdateStandings(string standings)
        {
            if (hasResult && shownResult.Online)
            {
                shownResult.Standings = standings;
                WriteResults(shownResult);
            }
        }

        private static string Goal(bool reached, string text)
        {
            return reached ? $"<color=#8CFF6A>+ {text}</color>" : $"<color=#FFFFFF77>- {text}</color>";
        }

        #endregion

        #region Animation

        private void Update()
        {
            float deltaTime = Time.unscaledDeltaTime;

            coinPunch = Mathf.MoveTowards(coinPunch, 0f, deltaTime * 5f);
            if (coinsIcon != null)
            {
                coinsIcon.localScale = Vector3.one * (1f + coinPunch * 0.35f);
            }

            if (countdownText != null && countdownTime >= 0f)
            {
                countdownTime += deltaTime;
                float t = countdownTime / 0.75f;
                countdownText.transform.localScale = Vector3.one * Mathf.Lerp(1.7f, 1f, Tween.OutCubic(Mathf.Clamp01(t * 2.5f)));
                countdownText.alpha = t < 0.6f ? 1f : Mathf.Clamp01(1f - (t - 0.6f) / 0.4f);
                if (t >= 1f)
                {
                    countdownTime = -1f;
                    countdownText.gameObject.SetActive(false);
                }
            }

            if (toastText != null && toastTime >= 0f)
            {
                toastTime += deltaTime;
                float t = toastTime / 2.4f;
                toastText.rectTransform.anchoredPosition = new Vector2(0f, 150f + Tween.OutCubic(Mathf.Clamp01(t * 3f)) * 40f);
                toastText.alpha = t < 0.75f ? Mathf.Clamp01(toastTime * 6f) : Mathf.Clamp01(1f - (t - 0.75f) / 0.25f);
                if (t >= 1f)
                {
                    toastTime = -1f;
                    toastText.gameObject.SetActive(false);
                }
            }

            if (hintGroup != null && hintTime >= 0f)
            {
                hintTime += deltaTime;
                hintGroup.alpha = hintTime < 0.3f ? hintTime / 0.3f : hintTime < 4f ? 1f : Mathf.Clamp01(1f - (hintTime - 4f) / 0.5f);
                if (hintTime > 4.5f)
                {
                    hintTime = -1f;
                    hintGroup.alpha = 0f;
                }
            }

            flash = Mathf.MoveTowards(flash, 0f, deltaTime * 1.2f);
            SetAlpha(damageFlash, flash);

            heartShake = Mathf.MoveTowards(heartShake, 0f, deltaTime * 2.5f);
            if (heartsRoot != null)
            {
                heartsRoot.anchoredPosition = heartsRest + new Vector2(Mathf.Sin(Time.unscaledTime * 60f) * 10f * heartShake, 0f);
            }

            curtainAlpha = Mathf.MoveTowards(curtainAlpha, 0f, deltaTime * 2.5f);
            SetAlpha(curtain, curtainAlpha);

            if (grain != null && grain.color.a > 0.001f && Time.frameCount % 3 == 0)
            {
                // Film grain: the tiled noise jumps a few pixels every few frames.
                grain.rectTransform.anchoredPosition = new Vector2(UnityEngine.Random.Range(-12f, 12f), UnityEngine.Random.Range(-12f, 12f));
            }

            if (resetConfirmUntil > 0f && Time.unscaledTime > resetConfirmUntil)
            {
                resetConfirmUntil = -1f;
                WriteResetLabel();
            }

            AnimateResults(deltaTime);
        }

        private void AnimateResults(float deltaTime)
        {
            if (resultsTime < 0f)
            {
                return;
            }
            resultsTime += deltaTime;
            while (starsPopped < resultStarCount && starsPopped < resultStars.Length && resultsTime > 0.5f + starsPopped * 0.4f)
            {
                Image star = resultStars[starsPopped];
                if (star != null)
                {
                    star.sprite = starFull;
                }
                starsPopped++;
                if (Runner != null && Runner.sounds != null)
                {
                    Runner.sounds.Play(Runner.sounds.star);
                }
            }
            for (int i = 0; i < resultStars.Length; i++)
            {
                if (resultStars[i] == null || i >= starsPopped)
                {
                    continue;
                }
                float age = resultsTime - (0.5f + i * 0.4f);
                float pop = age < 0.25f ? Mathf.Lerp(1.6f, 1f, Tween.OutCubic(age / 0.25f)) : 1f;
                resultStars[i].transform.localScale = Vector3.one * pop;
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
