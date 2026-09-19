using System;
using System.Collections.Generic;
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
    /// <see cref="GameUI"/> serves as the pause menu and hosts the settings panel.
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
            "Tip: coins in an arc show you when to jump.",
            "Tip: press DOWN in the air to dive and slide as you land.",
            "Tip: ramps lead onto the wagons - the safest lane is often up top.",
            "Tip: a shield soaks up one crash.",
            "Tip: switching lanes into the side of an obstacle only bounces you back.",
            "Tip: super jump lets you hop onto wagons without a ramp."
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

        [Header("Sprites")]
        [SerializeField] internal Sprite starFull;
        [SerializeField] internal Sprite starEmpty;
        [SerializeField] internal Sprite heartFull;
        [SerializeField] internal Sprite heartEmpty;

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

        private RunnerGameManager Runner => Manager as RunnerGameManager;

        protected override void Awake()
        {
            base.Awake();
            Listen(playButton, () => Runner?.PlaySelectedLevel());
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
                SetLabel(resetProgressLabel, "Reset progress");
                Runner?.ResetProgress();
                return;
            }
            resetConfirmUntil = Time.unscaledTime + 3f;
            SetLabel(resetProgressLabel, MobilePlatform.Pick("Click again to reset", "Tap again to reset"));
        }

        #endregion

        #region Level select

        public void ShowLevelSelect(List<LevelSummary> levels, int selected, int totalStars, int maxStars)
        {
            summaries.Clear();
            summaries.AddRange(levels);
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
                    card.Show(summaries[i], summaries[i].Index == selected, starFull, starEmpty);
                }
            }
            SetLabel(starsTotal, $"{totalStars} / {maxStars}");
            LevelSummary summary = summaries.Find(level => level.Index == selected);
            ShowDetails(summary);
            curtainAlpha = 0.55f;
        }

        private void ShowDetails(LevelSummary summary)
        {
            SetLabel(detailTitle, summary.Endless ? summary.Title : $"{summary.Index + 1}. {summary.Title}");
            SetLabel(detailWorld, summary.World);
            if (detailWorld != null)
            {
                detailWorld.color = Color.Lerp(summary.Accent, Color.white, 0.35f);
            }
            SetLabel(detailDescription, summary.Description);
            if (summary.Endless)
            {
                string best = summary.BestDistance > 0f ? $"Best run: <b>{summary.BestDistance:0} m</b>  ({summary.BestScore} points)" : "No record yet - set one!";
                SetLabel(detailGoals, $"Run as far as you can.\nThe world changes as you go.\n{best}");
            }
            else
            {
                SetLabel(detailGoals,
                    $"- Reach the finish ({summary.Length:0} m)\n- Collect {summary.CoinGoal} coins\n- Finish without a scratch");
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
            SetLabel(playLabel, summary.Unlocked ? "PLAY" : "LOCKED");
        }

        #endregion

        #region HUD

        public void BeginRun(string title, int levelIndex, bool endless, int maxHearts, float lengthOrBest)
        {
            endlessRun = endless;
            runLength = lengthOrBest;
            SetLabel(levelText, endless ? "ENDLESS RUN" : $"LEVEL {levelIndex + 1}  -  {title.ToUpperInvariant()}");
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
                    SetLabel(distanceText, runLength > 0f ? $"{meters} m  <size=60%>best {runLength:0} m</size>" : $"{meters} m");
                }
                else
                {
                    SetLabel(distanceText, $"{meters} m");
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
            toastText.text = text;
            toastText.color = Color.Lerp(color, Color.white, 0.25f);
            toastText.gameObject.SetActive(true);
            toastTime = 0f;
        }

        public void ShowHint(string text)
        {
            SetLabel(hintText, text);
            hintTime = 0f;
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
            if (result.Endless)
            {
                SetLabel(resultTitle, result.NewBest ? "NEW RECORD!" : "RUN OVER");
                SetLabel(resultSubtitle, "Endless Run");
                SetLabel(resultStats, $"Distance   <b>{result.Distance:0} m</b>\nCoins   <b>{result.Coins}</b>\nScore   <b>{result.Score}</b>");
                SetLabel(resultGoals, result.BestDistance > 0f ? $"Best run: {result.BestDistance:0} m" : string.Empty);
                resultStarCount = 0;
            }
            else if (result.Victory)
            {
                SetLabel(resultTitle, "LEVEL COMPLETE!");
                SetLabel(resultSubtitle, result.LevelTitle);
                SetLabel(resultStats, $"Coins   <b>{result.Coins}</b>\nScore   <b>{result.Score}</b>{(result.NewBest ? "  <color=#FFD84A>best!</color>" : string.Empty)}");
                SetLabel(resultGoals,
                    Goal(true, "Reached the finish") + "\n" +
                    Goal(result.CoinGoalReached, $"Collected {result.CoinGoal} coins") + "\n" +
                    Goal(result.Flawless, "Finished without a scratch"));
                resultStarCount = result.Stars;
            }
            else
            {
                SetLabel(resultTitle, "OUCH!");
                SetLabel(resultSubtitle, result.LevelTitle);
                SetLabel(resultStats, $"You made it <b>{result.Distance:0} m</b>\nCoins   <b>{result.Coins}</b>");
                SetLabel(resultGoals, Tips[UnityEngine.Random.Range(0, Tips.Length)]);
                resultStarCount = 0;
            }
            if (resultStarsRoot != null)
            {
                resultStarsRoot.gameObject.SetActive(!result.Endless);
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
                countdownText.transform.localScale = Vector3.one * Mathf.Lerp(1.7f, 1f, EaseOut(Mathf.Clamp01(t * 2.5f)));
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
                toastText.rectTransform.anchoredPosition = new Vector2(0f, 150f + EaseOut(Mathf.Clamp01(t * 3f)) * 40f);
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

            if (resetConfirmUntil > 0f && Time.unscaledTime > resetConfirmUntil)
            {
                resetConfirmUntil = -1f;
                SetLabel(resetProgressLabel, "Reset progress");
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
                float pop = age < 0.25f ? Mathf.Lerp(1.6f, 1f, EaseOut(age / 0.25f)) : 1f;
                resultStars[i].transform.localScale = Vector3.one * pop;
            }
            if (resultsTime > 3f)
            {
                resultsTime = -1f;
            }
        }

        private static float EaseOut(float t)
        {
            t = Mathf.Clamp01(t);
            return 1f - (1f - t) * (1f - t) * (1f - t);
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
