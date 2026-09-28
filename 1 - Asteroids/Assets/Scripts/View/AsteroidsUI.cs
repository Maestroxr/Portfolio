using System;
using System.Collections.Generic;
using Gamebox.Online;
using Gamebox;
using Gamebox.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// Interface of the Asteroids module: the mission select (sector map, mission briefing, stars) with the hangar, the
    /// HUD of a mission (score and combo, objective, hull and shield, lives, weapon, bombs, power-ups, boss bar, comet
    /// warnings, announcements) and the results screen. The shared menu of <see cref="GameUI"/> serves as the pause
    /// menu and hosts the settings panel.
    /// </summary>
    public class AsteroidsUI : GameUI
    {
        [Serializable]
        internal class PowerUpSlot
        {
            public PowerUpType type;
            public GameObject root;
            public Image fill;
        }

        /// <summary>A line of the pilots list of a shared mission.</summary>
        [Serializable]
        internal class PilotSlot
        {
            public GameObject root;
            public Image chip;
            public TMP_Text nameText;
            public TMP_Text scoreText;
        }

        [Serializable]
        internal class WarningMarker
        {
            public RectTransform arrow;
            public Image lane;
            [NonSerialized] public float time = -1f;
            [NonSerialized] public float duration;
        }

        [Header("Screens")]
        [SerializeField] internal CanvasGroup titleScreen;
        [SerializeField] internal CanvasGroup hudScreen;
        [SerializeField] internal CanvasGroup resultsScreen;
        [SerializeField] internal CanvasGroup hangarScreen;
        [SerializeField] internal Image curtain;
        [SerializeField] internal Camera worldCamera;

        [Header("Mission select")]
        [SerializeField] internal MissionNode[] missionNodes = new MissionNode[0];
        [SerializeField] internal TMP_Text[] sectorTitles = new TMP_Text[0];
        [SerializeField] internal TMP_Text[] sectorGates = new TMP_Text[0];
        [SerializeField] internal Image detailAccent;
        [SerializeField] internal TMP_Text detailSector;
        [SerializeField] internal TMP_Text detailTitle;
        [SerializeField] internal TMP_Text detailDescription;
        [SerializeField] internal TMP_Text detailObjective;
        [SerializeField] internal TMP_Text detailNew;
        [SerializeField] internal TMP_Text detailBest;
        [SerializeField] internal Image[] detailStars = new Image[0];
        [SerializeField] internal Button launchButton;
        [SerializeField] internal TMP_Text launchLabel;
        [SerializeField] internal Button continueButton;
        [SerializeField] internal TMP_Text starsTotal;
        [SerializeField] internal Button hangarButton;
        [SerializeField] internal Button titleSettingsButton;
        [SerializeField] internal Button titleExitButton;
        [SerializeField] internal Button onlineButton;
        [SerializeField] internal Button resetProgressButton;
        [SerializeField] internal TMP_Text resetProgressLabel;

        [Header("Hangar")]
        [SerializeField] internal ShipCard[] shipCards = new ShipCard[0];
        [SerializeField] internal Sprite[] shipPreviews = new Sprite[0];
        [SerializeField] internal Button hangarBackButton;

        [Header("HUD")]
        [SerializeField] internal TMP_Text scoreText;
        [SerializeField] internal TMP_Text multiplierText;
        [SerializeField] internal Image comboFill;
        [SerializeField] internal TMP_Text missionText;
        [SerializeField] internal TMP_Text objectiveText;
        [SerializeField] internal Image objectiveFill;
        [SerializeField] internal Image hullFill;
        [SerializeField] internal Image shieldFill;
        [SerializeField] internal Image dashFill;
        [SerializeField] internal Image[] lifeIcons = new Image[0];
        [SerializeField] internal Image weaponIcon;
        [SerializeField] internal TMP_Text weaponText;
        [SerializeField] internal Image[] weaponPips = new Image[0];
        [SerializeField] internal Image[] bombIcons = new Image[0];
        [SerializeField] internal Sprite[] weaponSprites = new Sprite[0];
        [SerializeField] internal PowerUpSlot[] powerUpSlots = new PowerUpSlot[0];
        [SerializeField] internal GameObject bossBar;
        [SerializeField] internal TMP_Text bossName;
        [SerializeField] internal Image bossFill;
        [SerializeField] internal Button pauseButton;
        [SerializeField] internal TMP_Text countdownText;
        [SerializeField] internal CanvasGroup announceGroup;
        [SerializeField] internal TMP_Text announceTitle;
        [SerializeField] internal TMP_Text announceSubtitle;
        [SerializeField] internal TMP_Text toastText;
        [SerializeField] internal CanvasGroup hintGroup;
        [SerializeField] internal TMP_Text hintText;
        [SerializeField] internal CanvasGroup briefingGroup;
        [SerializeField] internal TMP_Text briefingSector;
        [SerializeField] internal TMP_Text briefingTitle;
        [SerializeField] internal TMP_Text briefingObjective;
        [SerializeField] internal Image damageFlash;
        [SerializeField] internal WarningMarker[] warnings = new WarningMarker[0];
        [Tooltip("The pilots of a mission flown with others: a line each, hidden otherwise.")]
        [SerializeField] internal GameObject pilotsPanel;
        [SerializeField] internal PilotSlot[] pilotSlots = new PilotSlot[0];

        [Header("Touch")]
        [Tooltip("The on-screen stick and buttons, shown when the game is played by touch.")]
        [SerializeField] internal ShipTouchControls shipControls;

        [Header("Results")]
        [SerializeField] internal TMP_Text resultTitle;
        [SerializeField] internal TMP_Text resultSubtitle;
        [SerializeField] internal TMP_Text resultScore;
        [SerializeField] internal TMP_Text resultStats;
        [SerializeField] internal TMP_Text resultGoals;
        [SerializeField] internal Image[] resultStars = new Image[0];
        [SerializeField] internal Button nextButton;
        [SerializeField] internal TMP_Text nextLabel;
        [SerializeField] internal Button retryButton;
        [SerializeField] internal Button missionsButton;
        [SerializeField] internal TMP_Text missionsLabel;

        [Header("Sprites")]
        [SerializeField] internal Sprite starFull;
        [SerializeField] internal Sprite starEmpty;

        /// <summary>The full star: the active theme's, else the scene's.</summary>
        internal Sprite StarFull => Themed(theme => theme.Interface.starFull, starFull);

        /// <summary>The empty star: the active theme's, else the scene's.</summary>
        internal Sprite StarEmpty => Themed(theme => theme.Interface.starEmpty, starEmpty);

        /// <summary>The HUD icon of <paramref name="weapon"/>: the active theme's, else the scene's; null without either.</summary>
        internal Sprite WeaponSprite(WeaponType weapon)
        {
            int index = (int)weapon;
            return Themed(theme => theme.Weapon(weapon), index >= 0 && index < weaponSprites.Length ? weaponSprites[index] : null);
        }

        /// <summary>The hangar picture of <paramref name="ship"/>: the active theme's look of the hull, else the scene's; null without either.</summary>
        internal Sprite Preview(HangarShip ship)
        {
            AsteroidsTheme.ShipLook look = AsteroidsThemes.Ship(ship.Name);
            if (look != null && look.preview != null)
            {
                return look.preview;
            }
            return ship.Index >= 0 && ship.Index < shipPreviews.Length ? shipPreviews[ship.Index] : null;
        }

        /// <summary><paramref name="pick"/> of the active theme when there is one and it has the sprite, else <paramref name="fallback"/>.</summary>
        internal static Sprite Themed(System.Func<AsteroidsTheme, Sprite> pick, Sprite fallback)
        {
            AsteroidsTheme theme = AsteroidsThemes.Active;
            Sprite sprite = theme != null ? pick(theme) : null;
            return sprite != null ? sprite : fallback;
        }

        /// <summary>The tint transitions of the game's buttons: lit up on hover, darkened when pressed, greyed when disabled.</summary>
        public static ColorBlock ButtonColors
        {
            get
            {
                ColorBlock colors = ColorBlock.defaultColorBlock;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
                colors.selectedColor = Color.white;
                colors.pressedColor = new Color(0.75f, 0.75f, 0.8f, 1f);
                colors.disabledColor = new Color(0.45f, 0.45f, 0.5f, 0.7f);
                colors.colorMultiplier = 1.2f;
                colors.fadeDuration = 0.08f;
                return colors;
            }
        }

        [Header("Strike")]
        [Tooltip("The strike mode's screens on the same canvas.")]
        [SerializeField] internal StrikeUI strike;
        [Tooltip("The ASTEROID FIELD tab of the mission select.")]
        [SerializeField] internal Button fieldTab;
        [Tooltip("The PLANET STRIKE tab of the mission select.")]
        [SerializeField] internal Button strikeTab;
        [Tooltip("Parts of the mission select that belong to the asteroid field (hidden on the strike tab).")]
        [SerializeField] internal GameObject[] fieldMenuParts = new GameObject[0];
        [Tooltip("Parts of the HUD that belong to the asteroid field (hidden in a strike mission).")]
        [SerializeField] internal GameObject[] fieldHudParts = new GameObject[0];
        [Tooltip("The field's results panel (hidden while the strike results show).")]
        [SerializeField] internal GameObject[] fieldResultParts = new GameObject[0];

        private readonly List<MissionSummary> summaries = new List<MissionSummary>();
        private readonly List<HangarShip> ships = new List<HangarShip>();
        private BaseGameState shownState = BaseGameState.Initialization;
        private int selectedMission;
        private Color accent = new Color(0.3f, 0.85f, 1f);
        private int shownScore = -1;
        private int shownMultiplier = -1;
        private string shownObjective;
        private int shownLives = -1;
        private WeaponType shownWeapon = (WeaponType)(-1);
        private int shownLevel = -1;
        private int shownBombs = -1;
        private float countdownTime = -1f;
        private float announceTime = -1f;
        private float toastTime = -1f;
        private float hintTime = -1f;
        private float briefingTime = -1f;
        private float flash;
        private float scorePunch;
        private float resultsTime = -1f;
        private int resultStarCount;
        private int starsPopped;
        private float resetConfirmUntil = -1f;

        private AsteroidsGameManager Asteroids => Manager as AsteroidsGameManager;

        /// <summary>The strike mode's screens; null in a scene built without them.</summary>
        public StrikeUI Strike => strike;

        private AsteroidsAudio Sounds => Asteroids != null ? Asteroids.sounds : null;

        /// <summary>The manager, for the strike screens.</summary>
        internal AsteroidsGameManager Game => Asteroids;

        /// <summary>The game's sounds, for the strike screens.</summary>
        internal AsteroidsAudio Audio => Sounds;

        /// <summary>The mode the mission select and the HUD show.</summary>
        public MissionMode ShownMode { get; private set; } = MissionMode.Field;


        protected override void Awake()
        {
            base.Awake();
            Listen(launchButton, () => Asteroids?.LaunchSelectedMission());
            Listen(continueButton, () => GameManager?.LoadGame());
            Listen(hangarButton, ShowHangar);
            Listen(hangarBackButton, HideHangar);
            Listen(titleSettingsButton, ShowSettings);
            Listen(titleExitButton, () => Asteroids?.QuitToLauncher());
            Listen(onlineButton, () => Asteroids?.OpenOnline());
            Listen(resetProgressButton, OnResetProgress);
            Listen(pauseButton, OnPauseClicked);
            Listen(nextButton, () => Asteroids?.PlayNextMission());
            Listen(retryButton, () => Asteroids?.RetryMission());
            Listen(missionsButton, () => Asteroids?.ReturnToMissionSelect());
            Listen(fieldTab, () => Asteroids?.SelectMode(MissionMode.Field));
            Listen(strikeTab, () => Asteroids?.SelectMode(MissionMode.Strike));
            foreach (MissionNode node in missionNodes)
            {
                if (node != null)
                {
                    node.Clicked += index => Asteroids?.SelectMission(index);
                }
            }
            foreach (ShipCard card in shipCards)
            {
                if (card != null)
                {
                    card.Clicked += index => Asteroids?.SelectShip(index);
                }
            }
            SetAlpha(damageFlash, 0f);
            SetAlpha(curtain, 0f);
            HideText(countdownText);
            HideText(toastText);
            SetGroup(hintGroup, 0f);
            SetGroup(announceGroup, 0f);
            SetGroup(briefingGroup, 0f);
            if (hangarScreen != null)
            {
                hangarScreen.gameObject.SetActive(false);
            }
            if (bossBar != null)
            {
                bossBar.SetActive(false);
            }
            foreach (WarningMarker marker in warnings)
            {
                SetWarningVisible(marker, false);
            }
            ShowPilots(null);
        }


        private void Update()
        {
            if (Asteroids != null && (Asteroids.IsLobbyOpen || Asteroids.InSession))
            {
                // The keys below restart, save, load and launch single player missions.
                Animate(Time.unscaledDeltaTime);
                return;
            }
            if (Input.GetKeyDown(KeyCode.F1) && StartNewGame != null && StartNewGame.interactable)
            {
                StartNewGame.onClick.Invoke();
            }
            // A strike mission cannot be saved, and the strike tab has nothing to continue.
            bool strikeShown = ShownMode == MissionMode.Strike || (Asteroids != null && Asteroids.IsStrike);
            if (Input.GetKeyDown(KeyCode.F4) && SaveGame != null && !strikeShown)
            {
                SaveGame.onClick.Invoke();
            }
            if (Input.GetKeyDown(KeyCode.F5) && LoadGame != null && !strikeShown)
            {
                LoadGame.onClick.Invoke();
            }
            if (shownState == BaseGameState.Initialization && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) &&
                (hangarScreen == null || !hangarScreen.gameObject.activeSelf) && (strike == null || !strike.IsSupplyOpen))
            {
                Asteroids?.LaunchSelectedMission();
            }
            Animate(Time.unscaledDeltaTime);
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
            if (strike != null)
            {
                strike.UpdateGameState(state);
            }
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
                    // A strike mission is neither saved nor loaded in flight.
                    bool strikeMission = Asteroids != null && Asteroids.IsStrike;
                    SetVisible(SaveGame, !strikeMission);
                    SetVisible(LoadGame, !strikeMission);
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
            if (screen != titleScreen && hangarScreen != null)
            {
                hangarScreen.gameObject.SetActive(false);
            }
            curtainAlpha = 0.6f;
        }


        private float curtainAlpha;


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
                Asteroids?.ResetProgress();
                return;
            }
            resetConfirmUntil = Time.unscaledTime + 3f;
            SetLabel(resetProgressLabel, MobilePlatform.Pick("Click again to reset", "Tap again to reset"));
        }


        /// <summary>Back closes the settings, then the hangar, before the manager pauses or leaves.</summary>
        public override bool HandleBack()
        {
            if (!IsSettingsShown && hangarScreen != null && hangarScreen.gameObject.activeInHierarchy)
            {
                HideHangar();
                return true;
            }
            if (!IsSettingsShown && strike != null && strike.HandleBack())
            {
                return true;
            }
            return base.HandleBack();
        }


        internal void ShowHangar()
        {
            if (hangarScreen == null)
            {
                return;
            }
            Sounds?.Click();
            hangarScreen.gameObject.SetActive(true);
            ShowShips();
        }


        internal void HideHangar()
        {
            if (hangarScreen == null)
            {
                return;
            }
            Sounds?.Click();
            hangarScreen.gameObject.SetActive(false);
        }


        /// <summary>
        /// Shows the asteroid field or the planet strike: the tab of the mission select, the field's own parts of the
        /// mission select and the HUD (the strike screens bring their own), and the strike screens' mode.
        /// </summary>
        public void ShowMode(MissionMode mode)
        {
            ShownMode = mode;
            bool field = mode == MissionMode.Field;
            SetActive(fieldMenuParts, field);
            SetActive(fieldHudParts, field);
            if (!field && continueButton != null)
            {
                continueButton.gameObject.SetActive(false);
            }
            SetTab(fieldTab, field, new Color(0.3f, 0.6f, 1f));
            SetTab(strikeTab, !field, new Color(1f, 0.6f, 0.25f));
            if (strike != null)
            {
                strike.ShowMode(mode);
            }
        }


        /// <summary>Shows or hides the field's results panel (the strike results lie in its place).</summary>
        internal void ShowFieldResults(bool visible)
        {
            SetActive(fieldResultParts, visible);
        }


        private static void SetTab(Button tab, bool selected, Color color)
        {
            if (tab == null || tab.targetGraphic == null)
            {
                return;
            }
            tab.targetGraphic.color = selected ? color : new Color(0.16f, 0.26f, 0.4f, 0.95f);
            var label = tab.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.color = selected ? Color.white : new Color(0.7f, 0.8f, 0.92f);
            }
        }


        private static void SetActive(GameObject[] parts, bool active)
        {
            foreach (GameObject part in parts)
            {
                if (part != null && part.activeSelf != active)
                {
                    part.SetActive(active);
                }
            }
        }


        private static void SetVisible(Button button, bool visible)
        {
            if (button != null && button.gameObject.activeSelf != visible)
            {
                button.gameObject.SetActive(visible);
            }
        }

        #endregion


        #region Mission select

        public void ShowMissionSelect(List<MissionSummary> missions, int selected, int totalStars, int maxStars, List<HangarShip> hangar, bool canContinue)
        {
            summaries.Clear();
            summaries.AddRange(missions);
            ships.Clear();
            ships.AddRange(hangar);
            selectedMission = selected;
            for (int i = 0; i < missionNodes.Length; i++)
            {
                if (missionNodes[i] == null)
                {
                    continue;
                }
                bool used = i < summaries.Count;
                missionNodes[i].gameObject.SetActive(used);
                if (used)
                {
                    missionNodes[i].Show(summaries[i], summaries[i].Index == selected, StarFull, StarEmpty);
                }
            }
            ShowSectorHeaders(totalStars);
            SetLabel(starsTotal, $"{totalStars} / {maxStars}");
            MissionSummary current = summaries.Find(summary => summary.Index == selected);
            ShowDetails(current);
            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(canContinue);
            }
            if (hangarScreen != null && hangarScreen.gameObject.activeSelf)
            {
                ShowShips();
            }
        }


        private void ShowSectorHeaders(int totalStars)
        {
            AsteroidsCampaign campaign = Asteroids != null ? Asteroids.AsteroidsCampaign : null;
            for (int i = 0; i < sectorTitles.Length; i++)
            {
                AsteroidsCampaign.Sector sector = campaign != null ? campaign.GetSector(i) : null;
                if (sectorTitles[i] != null)
                {
                    sectorTitles[i].text = sector != null ? sector.title.ToUpperInvariant() : string.Empty;
                    if (sector != null && sector.theme != null)
                    {
                        sectorTitles[i].color = sector.theme.Accent;
                    }
                }
                if (i < sectorGates.Length && sectorGates[i] != null)
                {
                    int required = sector != null ? sector.starsRequired : 0;
                    sectorGates[i].text = required <= 0 ? "OPEN" : totalStars >= required ? $"{required} STARS - OPEN" : $"NEEDS {required} STARS";
                    sectorGates[i].color = required <= 0 || totalStars >= required ? new Color(0.6f, 1f, 0.7f, 0.8f) : new Color(1f, 0.75f, 0.35f);
                }
            }
        }


        private void ShowDetails(MissionSummary summary)
        {
            accent = summary.Accent.a > 0f ? summary.Accent : accent;
            if (detailAccent != null)
            {
                detailAccent.color = accent;
            }
            SetLabel(detailSector, summary.Endless ? "DEEP FIELD - ENDLESS" : $"{summary.SectorTitle.ToUpperInvariant()}  -  MISSION {summary.Number}");
            if (detailSector != null)
            {
                detailSector.color = accent;
            }
            SetLabel(detailTitle, summary.Title);
            SetLabel(detailDescription, summary.Description);
            SetLabel(detailObjective, $"<color=#9FE8FF>OBJECTIVE</color>  {summary.Objective}");
            SetLabel(detailNew, string.IsNullOrEmpty(summary.Introduces) ? string.Empty : $"<color=#FFD45E>NEW</color>  {summary.Introduces}");
            if (summary.Endless)
            {
                SetLabel(detailBest, summary.BestScore > 0 ? $"RECORD  {summary.BestScore:N0}  -  WAVE {summary.BestWave}" : "No record yet - how far can you get?");
            }
            else
            {
                string best = summary.BestScore > 0 ? $"BEST  {summary.BestScore:N0}" : "Not flown yet";
                SetLabel(detailBest, $"{best}     <color=#FFFFFF88>3rd star at {summary.ScoreGoal:N0}</color>");
            }
            for (int i = 0; i < detailStars.Length; i++)
            {
                if (detailStars[i] == null)
                {
                    continue;
                }
                detailStars[i].gameObject.SetActive(!summary.Endless);
                detailStars[i].sprite = i < summary.Stars ? StarFull : StarEmpty;
            }
            if (launchButton != null)
            {
                launchButton.interactable = summary.Unlocked;
            }
            SetLabel(launchLabel, summary.Unlocked ? "LAUNCH" : "LOCKED");
            if (!summary.Unlocked)
            {
                SetLabel(detailNew, $"<color=#FFB24D>{summary.LockReason}</color>");
            }
        }


        private void ShowShips()
        {
            for (int i = 0; i < shipCards.Length; i++)
            {
                if (shipCards[i] == null)
                {
                    continue;
                }
                bool used = i < ships.Count;
                shipCards[i].gameObject.SetActive(used);
                if (!used)
                {
                    continue;
                }
                shipCards[i].Show(ships[i], accent);
                Sprite preview = Preview(ships[i]);
                if (shipCards[i].preview != null && preview != null)
                {
                    shipCards[i].preview.sprite = preview;
                }
            }
        }

        #endregion


        #region HUD

        public void BeginMission(AsteroidsLevel mission, string briefing, int lives, string sectorTitle)
        {
            accent = mission.Theme != null ? mission.Theme.Accent : accent;
            SetLabel(missionText, mission.IsEndless ? "ENDLESS SURVIVAL" : mission.Title.ToUpperInvariant());
            SetLabel(briefingSector, sectorTitle.ToUpperInvariant());
            if (briefingSector != null)
            {
                briefingSector.color = accent;
            }
            SetLabel(briefingTitle, mission.Title.ToUpperInvariant());
            SetLabel(briefingObjective, briefing);
            briefingTime = 0f;
            shownScore = -1;
            shownMultiplier = -1;
            shownObjective = null;
            shownLives = -1;
            shownWeapon = (WeaponType)(-1);
            shownLevel = -1;
            shownBombs = -1;
            HideBoss();
            HideText(toastText);
            SetGroup(hintGroup, 0f);
            hintTime = -1f;
            foreach (WarningMarker marker in warnings)
            {
                marker.time = -1f;
                SetWarningVisible(marker, false);
            }
            foreach (PowerUpSlot slot in powerUpSlots)
            {
                if (slot != null && slot.root != null)
                {
                    slot.root.SetActive(false);
                }
            }
            if (objectiveFill != null)
            {
                objectiveFill.color = accent;
            }
        }


        public void UpdateHud(HudState hud)
        {
            if (hud.Score != shownScore)
            {
                if (hud.Score > shownScore && shownScore >= 0)
                {
                    scorePunch = 1f;
                }
                shownScore = hud.Score;
                SetLabel(scoreText, hud.Score.ToString("N0"));
            }
            if (hud.Multiplier != shownMultiplier)
            {
                shownMultiplier = hud.Multiplier;
                if (multiplierText != null)
                {
                    multiplierText.gameObject.SetActive(hud.Multiplier > 1);
                    multiplierText.text = $"x{hud.Multiplier}";
                    multiplierText.color = hud.Multiplier >= 4 ? new Color(1f, 0.45f, 0.9f) : new Color(1f, 0.85f, 0.3f);
                }
            }
            if (comboFill != null)
            {
                comboFill.fillAmount = hud.ComboTime;
            }
            if (hud.Objective != shownObjective)
            {
                shownObjective = hud.Objective;
                SetLabel(objectiveText, hud.Objective);
            }
            if (objectiveFill != null)
            {
                objectiveFill.fillAmount = Mathf.MoveTowards(objectiveFill.fillAmount, hud.ObjectiveProgress, Time.unscaledDeltaTime * 2f);
            }
            if (hullFill != null)
            {
                hullFill.fillAmount = hud.Hull;
                hullFill.color = hud.Hull > 0.6f ? new Color(0.35f, 1f, 0.55f) : hud.Hull > 0.3f ? new Color(1f, 0.82f, 0.25f) : new Color(1f, 0.3f, 0.25f);
            }
            if (shieldFill != null)
            {
                shieldFill.fillAmount = hud.Shield;
            }
            if (dashFill != null)
            {
                dashFill.fillAmount = 1f - hud.DashRecharge;
                dashFill.color = hud.DashRecharge <= 0f ? new Color(0.5f, 0.9f, 1f) : new Color(0.35f, 0.45f, 0.6f);
            }
            if (shipControls != null)
            {
                shipControls.ShowState(hud.DashRecharge, hud.Bombs);
            }
            if (hud.Lives != shownLives)
            {
                shownLives = hud.Lives;
                for (int i = 0; i < lifeIcons.Length; i++)
                {
                    if (lifeIcons[i] != null)
                    {
                        lifeIcons[i].gameObject.SetActive(i < hud.Lives);
                    }
                }
            }
            if (hud.Weapon != shownWeapon || hud.WeaponLevel != shownLevel)
            {
                shownWeapon = hud.Weapon;
                shownLevel = hud.WeaponLevel;
                Color tint = WeaponRules.Tint(hud.Weapon);
                SetLabel(weaponText, WeaponRules.ShortTitle(hud.Weapon));
                if (weaponText != null)
                {
                    weaponText.color = tint;
                }
                Sprite weaponSprite = WeaponSprite(hud.Weapon);
                if (weaponIcon != null && weaponSprite != null)
                {
                    weaponIcon.sprite = weaponSprite;
                }
                if (shipControls != null && weaponSprite != null)
                {
                    shipControls.ShowWeapon(weaponSprite);
                }
                for (int i = 0; i < weaponPips.Length; i++)
                {
                    if (weaponPips[i] != null)
                    {
                        weaponPips[i].color = i < hud.WeaponLevel ? tint : new Color(1f, 1f, 1f, 0.15f);
                    }
                }
            }
            if (hud.Bombs != shownBombs)
            {
                shownBombs = hud.Bombs;
                for (int i = 0; i < bombIcons.Length; i++)
                {
                    if (bombIcons[i] != null)
                    {
                        bombIcons[i].color = i < hud.Bombs ? Color.white : new Color(1f, 1f, 1f, 0.15f);
                    }
                }
            }
            if (bossBar != null)
            {
                if (bossBar.activeSelf != hud.BossActive)
                {
                    bossBar.SetActive(hud.BossActive);
                }
                if (hud.BossActive && bossFill != null)
                {
                    bossFill.fillAmount = Mathf.MoveTowards(bossFill.fillAmount, hud.BossHealth, Time.unscaledDeltaTime * 1.5f);
                }
            }
        }


        public void SetPowerUp(PowerUpType type, float fraction)
        {
            foreach (PowerUpSlot slot in powerUpSlots)
            {
                if (slot == null || slot.type != type || slot.root == null)
                {
                    continue;
                }
                bool active = fraction > 0f;
                if (slot.root.activeSelf != active)
                {
                    slot.root.SetActive(active);
                }
                if (active && slot.fill != null)
                {
                    slot.fill.fillAmount = Mathf.Clamp01(fraction);
                }
            }
        }


        public void ShowBoss(string name)
        {
            SetLabel(bossName, name.ToUpperInvariant());
            if (bossFill != null)
            {
                bossFill.fillAmount = 0f;
            }
            if (bossBar != null)
            {
                bossBar.SetActive(true);
            }
            Announce("WARNING", name.ToUpperInvariant(), new Color(1f, 0.3f, 0.25f));
        }


        public void HideBoss()
        {
            if (bossBar != null)
            {
                bossBar.SetActive(false);
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
            countdownText.color = text == "GO!" ? new Color(0.5f, 1f, 0.6f) : accent;
            countdownTime = 0f;
        }


        public void Announce(string title, string subtitle, Color color)
        {
            SetLabel(announceTitle, title);
            SetLabel(announceSubtitle, subtitle);
            if (announceTitle != null)
            {
                announceTitle.color = color;
            }
            announceTime = 0f;
        }


        public void Toast(string text, Color color)
        {
            if (toastText == null)
            {
                return;
            }
            toastText.text = text;
            toastText.color = color;
            toastText.gameObject.SetActive(true);
            toastTime = 0f;
        }


        public void ShowHint(string text)
        {
            SetLabel(hintText, text);
            hintTime = 0f;
        }


        public void DamageFlash()
        {
            flash = 0.55f;
        }


        /// <summary>
        /// The pilots of a mission flown with others, by seat: who they are, what they scored and whether they still fly.
        /// Null hides the list.
        /// </summary>
        internal void ShowPilots(List<AsteroidsGameManager.PilotStatus> pilots)
        {
            bool show = pilots != null && pilots.Count > 0;
            if (pilotsPanel != null && pilotsPanel.activeSelf != show)
            {
                pilotsPanel.SetActive(show);
            }
            if (show && pilotsPanel != null && pilotSlots.Length > 0 && pilotSlots[0] != null && pilotSlots[0].root != null)
            {
                // The panel is as high as the lines it shows.
                var panelRect = (RectTransform)pilotsPanel.transform;
                float rowHeight = ((RectTransform)pilotSlots[0].root.transform).sizeDelta.y;
                panelRect.sizeDelta = new Vector2(panelRect.sizeDelta.x, 24f + rowHeight * Mathf.Min(pilots.Count, pilotSlots.Length));
            }
            for (int i = 0; i < pilotSlots.Length; i++)
            {
                PilotSlot slot = pilotSlots[i];
                if (slot == null || slot.root == null)
                {
                    continue;
                }
                bool used = show && i < pilots.Count;
                if (slot.root.activeSelf != used)
                {
                    slot.root.SetActive(used);
                }
                if (!used)
                {
                    continue;
                }
                AsteroidsGameManager.PilotStatus pilot = pilots[i];
                Color color = CoopRules.SeatColor(pilot.Seat);
                if (slot.chip != null)
                {
                    slot.chip.color = pilot.Flying ? color : new Color(color.r, color.g, color.b, 0.25f);
                }
                if (slot.nameText != null)
                {
                    slot.nameText.text = Standings.Who(pilot.Name, pilot.Local);
                    slot.nameText.color = pilot.Flying ? Color.white : new Color(1f, 1f, 1f, 0.45f);
                }
                if (slot.scoreText != null)
                {
                    slot.scoreText.text = pilot.Flying ? pilot.Score.ToString("N0") : $"{pilot.Score:N0}  LOST";
                    slot.scoreText.color = pilot.Flying ? color : new Color(1f, 0.45f, 0.4f, 0.9f);
                }
            }
        }


        /// <summary>A comet will enter at <paramref name="from"/> heading along <paramref name="direction"/>: shows where and its lane.</summary>
        public void ShowCometWarning(Vector2 from, Vector2 direction, float delay)
        {
            WarningMarker marker = null;
            foreach (WarningMarker candidate in warnings)
            {
                if (candidate.time < 0f)
                {
                    marker = candidate;
                    break;
                }
            }
            if (marker == null || worldCamera == null)
            {
                return;
            }
            Vector3 viewport = worldCamera.WorldToViewportPoint(new Vector3(from.x, from.y, 0f));
            var anchor = new Vector2(Mathf.Clamp(viewport.x, 0.035f, 0.965f), Mathf.Clamp(viewport.y, 0.06f, 0.94f));
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            if (marker.arrow != null)
            {
                marker.arrow.anchorMin = anchor;
                marker.arrow.anchorMax = anchor;
                marker.arrow.anchoredPosition = Vector2.zero;
                marker.arrow.localRotation = Quaternion.Euler(0f, 0f, angle);
            }
            if (marker.lane != null)
            {
                RectTransform lane = marker.lane.rectTransform;
                lane.anchorMin = anchor;
                lane.anchorMax = anchor;
                lane.anchoredPosition = Vector2.zero;
                lane.localRotation = Quaternion.Euler(0f, 0f, angle);
            }
            marker.time = 0f;
            marker.duration = delay + 0.6f;
            SetWarningVisible(marker, true);
        }

        #endregion


        #region Results

        public void ShowResults(MissionResult result)
        {
            ShowFieldResults(true);
            if (strike != null)
            {
                strike.HideResults();
            }
            // A shared mission goes back to its room, where the host starts the next one.
            SetLabel(missionsLabel, result.Coop ? "Room" : "Missions");
            if (retryButton != null)
            {
                retryButton.gameObject.SetActive(!result.Coop);
            }
            SetLabel(resultTitle, result.Endless ? "RUN OVER" : result.Victory ? "MISSION COMPLETE" : "MISSION FAILED");
            if (resultTitle != null)
            {
                resultTitle.color = result.Victory || result.Endless ? new Color(0.45f, 1f, 0.65f) : new Color(1f, 0.4f, 0.35f);
            }
            SetLabel(resultSubtitle, result.Title.ToUpperInvariant());
            SetLabel(resultScore, (result.NewBest ? "<color=#FFD24A>NEW BEST!</color>  " : string.Empty) + result.Score.ToString("N0"));
            string accuracy = $"{Mathf.RoundToInt(result.Accuracy * 100f)}%";
            string time = MissionObjective.FormatTime(result.Time);
            string stats = $"Destroyed  <b>{result.Kills}</b>     Accuracy  <b>{accuracy}</b>\nBest combo  <b>{result.MaxCombo}</b>     Crystals  <b>{result.Crystals}</b>     Time  <b>{time}</b>";
            if (result.LifeBonus > 0)
            {
                stats += $"\nShips left bonus  <b>+{result.LifeBonus:N0}</b>";
            }
            if (result.Endless)
            {
                stats += $"\nWave reached  <b>{result.Wave}</b>     Record  <b>wave {result.BestWave}</b>";
            }
            SetLabel(resultStats, stats);
            if (result.Coop)
            {
                ShowStandings(result.Standings);
            }
            else if (result.Endless)
            {
                SetLabel(resultGoals, "Endless runs keep your best score and wave.");
            }
            else
            {
                SetLabel(resultGoals,
                    Goal(result.Victory, "Complete the mission") + "\n" +
                    Goal(result.Victory && result.Flawless, "Lose no ships") + "\n" +
                    Goal(result.Victory && result.ScoreGoalReached, $"Score {result.ScoreGoal:N0}"));
            }
            resultStarCount = result.Stars;
            starsPopped = 0;
            resultsTime = 0f;
            foreach (Image star in resultStars)
            {
                if (star != null)
                {
                    star.sprite = StarEmpty;
                    star.transform.localScale = Vector3.one;
                    star.gameObject.SetActive(!result.Endless && !result.Coop);
                }
            }
            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(result.HasNext);
                nextButton.interactable = !result.NextLocked;
            }
            SetLabel(nextLabel, result.NextLocked ? "LOCKED" : "NEXT");
            if (result.NextLocked && result.HasNext)
            {
                SetLabel(resultGoals, (resultGoals != null ? resultGoals.text : string.Empty) + $"\n<color=#FFB24D>{result.NextLockReason}</color>");
            }
            flash = 0f;
        }


        /// <summary>The pilots of a shared mission by place, where a single player mission lists its goals.</summary>
        public void ShowStandings(string standings)
        {
            SetLabel(resultGoals, standings);
        }


        private static string Goal(bool reached, string text)
        {
            return reached ? $"<color=#8CFF6A>+ {text}</color>" : $"<color=#FFFFFF66>- {text}</color>";
        }

        #endregion


        #region Animation

        private void Animate(float deltaTime)
        {
            if (countdownText != null && countdownTime >= 0f)
            {
                countdownTime += deltaTime;
                float t = countdownTime / 0.8f;
                countdownText.transform.localScale = Vector3.one * Mathf.Lerp(1.8f, 1f, Tween.OutCubic(Mathf.Clamp01(t * 2.5f)));
                countdownText.alpha = t < 0.6f ? 1f : Mathf.Clamp01(1f - (t - 0.6f) / 0.4f);
                if (t >= 1f)
                {
                    countdownTime = -1f;
                    countdownText.gameObject.SetActive(false);
                }
            }

            if (announceGroup != null && announceTime >= 0f)
            {
                announceTime += deltaTime;
                float t = announceTime;
                announceGroup.alpha = t < 0.2f ? t / 0.2f : t < 1.8f ? 1f : Mathf.Clamp01(1f - (t - 1.8f) / 0.5f);
                announceGroup.transform.localScale = Vector3.one * (t < 0.2f ? Mathf.Lerp(1.25f, 1f, Tween.OutCubic(t / 0.2f)) : 1f);
                if (t > 2.3f)
                {
                    announceTime = -1f;
                    announceGroup.alpha = 0f;
                }
            }

            if (briefingGroup != null && briefingTime >= 0f)
            {
                briefingTime += deltaTime;
                float t = briefingTime;
                briefingGroup.alpha = t < 0.3f ? t / 0.3f : t < 2.6f ? 1f : Mathf.Clamp01(1f - (t - 2.6f) / 0.5f);
                if (t > 3.1f)
                {
                    briefingTime = -1f;
                    briefingGroup.alpha = 0f;
                }
            }

            if (toastText != null && toastTime >= 0f)
            {
                toastTime += deltaTime;
                float t = toastTime / 2f;
                toastText.rectTransform.anchoredPosition = new Vector2(0f, -210f - Tween.OutCubic(Mathf.Clamp01(t * 3f)) * 20f);
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
                hintGroup.alpha = hintTime < 0.3f ? hintTime / 0.3f : hintTime < 5f ? 1f : Mathf.Clamp01(1f - (hintTime - 5f) / 0.5f);
                if (hintTime > 5.5f)
                {
                    hintTime = -1f;
                    hintGroup.alpha = 0f;
                }
            }

            foreach (WarningMarker marker in warnings)
            {
                if (marker.time < 0f)
                {
                    continue;
                }
                marker.time += deltaTime;
                float blink = Mathf.Repeat(marker.time * 5f, 1f) < 0.6f ? 1f : 0.35f;
                if (marker.arrow != null && marker.arrow.TryGetComponent(out Image arrow))
                {
                    SetAlpha(arrow, blink);
                }
                if (marker.lane != null)
                {
                    SetAlpha(marker.lane, 0.28f * blink);
                }
                if (marker.time >= marker.duration)
                {
                    marker.time = -1f;
                    SetWarningVisible(marker, false);
                }
            }

            scorePunch = Mathf.MoveTowards(scorePunch, 0f, deltaTime * 6f);
            if (scoreText != null)
            {
                scoreText.transform.localScale = Vector3.one * (1f + scorePunch * 0.12f);
            }
            if (multiplierText != null && multiplierText.gameObject.activeSelf)
            {
                multiplierText.transform.localScale = Vector3.one * (1f + 0.08f * Mathf.Sin(Time.unscaledTime * 10f));
            }

            flash = Mathf.MoveTowards(flash, 0f, deltaTime * 1.4f);
            SetAlpha(damageFlash, flash);

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
            while (starsPopped < resultStarCount && starsPopped < resultStars.Length && resultsTime > 0.6f + starsPopped * 0.45f)
            {
                Image star = resultStars[starsPopped];
                if (star != null)
                {
                    star.sprite = StarFull;
                }
                Sounds?.Star(starsPopped);
                starsPopped++;
            }
            for (int i = 0; i < resultStars.Length; i++)
            {
                if (resultStars[i] == null || i >= starsPopped)
                {
                    continue;
                }
                float age = resultsTime - (0.6f + i * 0.45f);
                float pop = age < 0.25f ? Mathf.Lerp(1.7f, 1f, Tween.OutCubic(age / 0.25f)) : 1f;
                resultStars[i].transform.localScale = Vector3.one * pop;
            }
            if (resultsTime > 3f)
            {
                resultsTime = -1f;
            }
        }

        #endregion


        public override void UpdateLevel(int level)
        {
            // The mission select and the HUD show the mission by name.
        }


        public override void UpdateScore(float score)
        {
            // The HUD shows the score through UpdateHud.
        }


        private static void SetWarningVisible(WarningMarker marker, bool visible)
        {
            if (marker == null)
            {
                return;
            }
            if (marker.arrow != null)
            {
                marker.arrow.gameObject.SetActive(visible);
            }
            if (marker.lane != null)
            {
                marker.lane.gameObject.SetActive(visible);
            }
        }


        private static void SetLabel(TMP_Text label, string text)
        {
            if (label != null)
            {
                label.text = text;
            }
        }


        private static void HideText(TMP_Text text)
        {
            if (text != null)
            {
                text.gameObject.SetActive(false);
            }
        }


        private static void SetGroup(CanvasGroup group, float alpha)
        {
            if (group != null)
            {
                group.alpha = alpha;
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
