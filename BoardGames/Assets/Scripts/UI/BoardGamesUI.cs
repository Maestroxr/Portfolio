using System.Collections;
using Gamebox;
using Gamebox.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.BoardGames
{
    /// <summary>
    /// The interface of the games on top of the shared <see cref="GameUI"/> menu. The one menu panel is the title
    /// (Campaign, Play vs Computer, Two Players, Play Online, Load Game, Settings, Exit), which plays the game the mode
    /// tabs over it picked (<see cref="ModeTabs"/>), and the pause menu (Resume, Save Game, Load Game, Settings, Exit).
    /// In play the HUD shows the two seat panels (name, side, the game's own line), whose turn it is, the clock and a
    /// line of toasts, the same for checkers, chess and backgammon (each game adds its own HUD part); a finished game
    /// shows the result panel with Play again and Menu (online: Back to the room and Leave the match). Everything the
    /// manager changes is redrawn on its <see cref="BoardGamesManager.Changed"/>.
    /// </summary>
    public sealed class BoardGamesUI : GameUI
    {
        /// <summary>The player's avatar on this device while the menu is up: In the top left corner, beside the games' tabs.</summary>
        protected override (Vector2 anchor, Vector2 offset)? AvatarPlace => (new Vector2(0f, 1f), new Vector2(24f, -24f));

        [Header("HUD")]
        [SerializeField] private GameObject hud;
        [SerializeField] private SeatPanel[] seatPanels = new SeatPanel[2];
        [SerializeField] private TMP_Text turnText;
        [SerializeField] private Image clockFill;
        [SerializeField] private TMP_Text clockText;
        [SerializeField] private TMP_Text toastText;

        [Header("Result")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TMP_Text resultTitle;
        [SerializeField] private TMP_Text resultDetail;
        [Tooltip("The row of the three stars of a campaign game (hidden for every other game).")]
        [SerializeField] private GameObject starsRow;
        [SerializeField] private Image[] starImages = new Image[3];
        [SerializeField] private Color starLit = new Color(0.12f, 0.35f, 0.21f);
        [SerializeField] private Color starDim = new Color(0.16f, 0.14f, 0.13f, 0.18f);
        [Tooltip("The star picture: the result panel's stars and the campaign window's.")]
        [SerializeField] private Sprite starSprite;
        [SerializeField] private Button againButton;
        [SerializeField] private Button menuButton;
        [SerializeField] private Button resignButton;

        [Header("Menu buttons")]
        [Tooltip("The campaign button: StartNewGame of the shared menu, labelled Campaign.")]
        [SerializeField] private Button campaignButton;
        [SerializeField] private Button quickGameButton;
        [SerializeField] private Button localPlayButton;
        [SerializeField] private Button onlineButton;

        private const float ResignConfirmSeconds = 3f;

        private BoardGamesManager subscribed;
        private CameraRig cameraRig;
        private BoardGamesAudio sound;
        private Coroutine toasting;
        private float resignAskedAt = -100f;
        private bool onTitle = true;
        private bool over;
        private int shownClockSeconds = -1;

        public BoardGamesManager Games => Manager as BoardGamesManager;

        /// <summary>The star picture of the interface, for the campaign window's rows.</summary>
        public Sprite StarSprite => starSprite;

        /// <summary>Whether the title is on the screen (no game, not paused, no result).</summary>
        public bool OnTitle => onTitle;

        /// <summary>Whether the menu's buttons are on the screen (not hidden, not covered by the settings panel).</summary>
        public bool IsMenuShown => MenuButtonsUI != null ? MenuButtonsUI.gameObject.activeInHierarchy : MenuRoot != null && MenuRoot.gameObject.activeInHierarchy;

        protected override void Awake()
        {
            base.Awake();
            if (campaignButton == null)
            {
                campaignButton = StartNewGame;
            }
            quickGameButton = quickGameButton != null ? quickGameButton : FindMenuButton("QuickGame");
            localPlayButton = localPlayButton != null ? localPlayButton : FindMenuButton("LocalPlay");
            onlineButton = onlineButton != null ? onlineButton : FindMenuButton("PlayOnline");
            if (quickGameButton != null)
            {
                quickGameButton.onClick.AddListener(() => Games?.StartQuickGame());
            }
            if (localPlayButton != null)
            {
                localPlayButton.onClick.AddListener(() => Games?.OpenLocalPlay());
            }
            if (onlineButton != null)
            {
                onlineButton.onClick.AddListener(() => Games?.OpenOnline());
            }
            if (againButton != null)
            {
                againButton.onClick.AddListener(() => Games?.PlayAgain());
            }
            if (menuButton != null)
            {
                menuButton.onClick.AddListener(() => Games?.ReturnToTitle());
            }
            if (resignButton != null)
            {
                resignButton.onClick.AddListener(OnResignClicked);
            }
            foreach (Button button in GetComponentsInChildren<Button>(true))
            {
                button.onClick.AddListener(() => Sound?.Play(ClipKind.Click));
            }
            if (resultPanel != null)
            {
                resultPanel.SetActive(false);
            }
            if (toastText != null)
            {
                toastText.text = "";
            }
            if (hud != null)
            {
                hud.SetActive(false);
            }
        }

        private void OnEnable()
        {
            Subscribe();
            GameThemes.Changed += OnLookChanged;
        }

        private void Start()
        {
            Subscribe();
            RefreshHud();
        }

        private void OnDisable()
        {
            GameThemes.Changed -= OnLookChanged;
            if (subscribed != null)
            {
                subscribed.Changed -= RefreshHud;
                subscribed = null;
            }
        }

        /// <summary>
        /// The menu's skin restyles every button of the menu when the look changes, the language button's flag among
        /// them (it becomes the button sprite): the flag is shown again once the skin is on.
        /// </summary>
        private void OnLookChanged(GameType game, GameTheme theme)
        {
            if (game == GameType.BoardGames && isActiveAndEnabled)
            {
                StartCoroutine(RestoreFlags());
            }
        }

        private IEnumerator RestoreFlags()
        {
            yield return null;
            foreach (LanguageButton language in GetComponentsInChildren<LanguageButton>(true))
            {
                if (language.Flag != null)
                {
                    language.Flag.type = Image.Type.Simple;
                }
                language.Show();
            }
        }

        private void Subscribe()
        {
            BoardGamesManager manager = Games;
            if (manager == null || subscribed == manager)
            {
                return;
            }
            if (subscribed != null)
            {
                subscribed.Changed -= RefreshHud;
            }
            subscribed = manager;
            manager.Changed += RefreshHud;
        }

        private BoardGamesAudio Sound => sound != null ? sound : sound = FindAnyObjectByType<BoardGamesAudio>(FindObjectsInactive.Include);

        private CameraRig Rig => cameraRig != null ? cameraRig : cameraRig = FindAnyObjectByType<CameraRig>(FindObjectsInactive.Include);

        /// <summary>A button of the shared menu by its object name, under the menu buttons; null when the menu has none.</summary>
        private Button FindMenuButton(string buttonName)
        {
            Transform parent = MenuButtonsUI != null ? MenuButtonsUI : MenuRoot != null ? MenuRoot : transform;
            foreach (Button button in parent.GetComponentsInChildren<Button>(true))
            {
                if (button.name == buttonName)
                {
                    return button;
                }
            }
            return null;
        }

        // ------------------------------------------------------------------ screens

        public override void UpdateGameState(GameState state)
        {
            base.UpdateGameState(state);
            bool title = state.BaseState == BaseGameState.Initialization;
            bool paused = state.BaseState == BaseGameState.Paused;
            over = state.BaseState == BaseGameState.GameOver || state.BaseState == BaseGameState.Victory;
            onTitle = title;
            BoardGamesManager manager = Games;
            bool gameOn = manager != null && manager.Table != null;
            if (hud != null)
            {
                // The menu over a paused game covers the HUD's corners, and nothing of the HUD may be pressed then.
                hud.SetActive(!title && !paused);
            }
            SetVisible(campaignButton, title);
            SetVisible(quickGameButton, title);
            SetVisible(localPlayButton, title);
            SetVisible(onlineButton, title);
            SetVisible(ReturnToGame, paused && gameOn);
            SetVisible(SaveGame, paused && gameOn && manager.SaveRefusal == null);
            SetInteractable(SaveGame, paused && gameOn && manager.SaveRefusal == null);
            if (over && MenuRoot != null)
            {
                MenuRoot.gameObject.SetActive(false);
            }
            if (resultPanel != null)
            {
                resultPanel.SetActive(over);
            }
            if (over)
            {
                RefreshResult();
            }
            // A toast belongs to the screen it was shown on; the result panel says how the game ended itself.
            if (toasting != null)
            {
                StopCoroutine(toasting);
                toasting = null;
                if (toastText != null)
                {
                    toastText.text = "";
                }
            }
            resignAskedAt = -100f;
            Rig?.SetTitle(title, false);
            RefreshHud();
        }

        /// <summary>Campaign: the campaign window in place of a new game.</summary>
        protected override void OnStartNewGameClicked()
        {
            BoardGamesManager manager = Games;
            if (manager == null)
            {
                base.OnStartNewGameClicked();
                return;
            }
            CampaignWindow.For(manager).Show();
        }

        public override bool HandleBack()
        {
            if (base.HandleBack())
            {
                return true;
            }
            return false;
        }

        /// <summary>The language changed: the words the interface wrote with code are written again.</summary>
        public override void RefreshTexts()
        {
            base.RefreshTexts();
            if (over)
            {
                RefreshResult();
            }
            // The seat panels' words come from the manager, written again by the HUD's redraw.
            shownClockSeconds = -1;
            RefreshHud();
        }

        // ------------------------------------------------------------------ HUD

        /// <summary>Redraws the HUD from the manager: seats, whose turn it is, the resign button.</summary>
        private void RefreshHud()
        {
            BoardGamesManager manager = Games;
            if (manager == null)
            {
                return;
            }
            BoardTable table = manager.Table;
            TableSetup setup = manager.Setup;
            IBoardGame game = manager.Current;
            BoardGamesTheme look = manager.Look;
            bool gameOn = table != null && setup != null && game != null;
            for (int seat = 0; seat < seatPanels.Length; seat++)
            {
                SeatPanel panel = seatPanels[seat];
                if (panel == null)
                {
                    continue;
                }
                panel.gameObject.SetActive(gameOn);
                if (!gameOn || seat >= setup.seats.Length)
                {
                    continue;
                }
                Side side = setup.SideOf(seat);
                TableSeat tableSeat = setup.seats[seat];
                string name = tableSeat != null && !string.IsNullOrEmpty(tableSeat.name) ? tableSeat.name : L.F("Player {0}", seat + 1);
                if (tableSeat != null && tableSeat.computer && string.IsNullOrEmpty(tableSeat.name))
                {
                    name = L.T("Computer");
                }
                Color color = look != null ? look.ColorOf(side) : side == Side.Light ? Color.white : new Color(0.2f, 0.2f, 0.2f);
                // Online, the player at this device is "(you)" and a player who joined at this device is "(here)".
                bool you = manager.IsOwnSeat(seat);
                bool here = !you && manager.IsOnlineGame && manager.PlayedHere(seat);
                panel.Bind(name, color, manager.SideName(seat), you, here);
                panel.SetLine(manager.SeatLine(seat));
                panel.SetTurn(!game.IsOver && table.SeatToAct == seat);
            }
            if (turnText != null)
            {
                turnText.text = gameOn && !game.IsOver ? manager.TurnWords ?? "" : "";
            }
            bool canResign = gameOn && !game.IsOver && !over && manager.IsGameRunning && (!manager.IsOnlineGame || manager.LocalSeat >= 0);
            SetVisible(resignButton, canResign);
            if (canResign && Time.unscaledTime - resignAskedAt > ResignConfirmSeconds)
            {
                SetLabel(resignButton, L.T("Resign"));
            }
            UpdateClock(true);
        }

        private void Update()
        {
            if (!onTitle)
            {
                UpdateClock(false);
            }
            if (resignAskedAt > -100f && Time.unscaledTime - resignAskedAt > ResignConfirmSeconds)
            {
                resignAskedAt = -100f;
                SetLabel(resignButton, L.T("Resign"));
            }
        }

        /// <summary>The clock of the seat to move: a bar that empties and the seconds left; hidden without a clock.</summary>
        private void UpdateClock(bool force)
        {
            BoardGamesManager manager = Games;
            IBoardGame game = manager != null ? manager.Current : null;
            bool shown = game != null && !game.IsOver && manager.ClockRemaining >= 0f;
            if (clockFill != null)
            {
                Transform holder = clockFill.transform.parent != null ? clockFill.transform.parent : clockFill.transform;
                if (holder.gameObject.activeSelf != shown)
                {
                    holder.gameObject.SetActive(shown);
                }
                if (shown)
                {
                    clockFill.fillAmount = Mathf.Clamp01(manager.ClockFraction);
                    clockFill.color = manager.ClockFraction < 0.2f ? new Color(1f, 0.4f, 0.3f) : Color.white;
                }
            }
            if (clockText != null)
            {
                if (clockText.gameObject.activeSelf != shown)
                {
                    clockText.gameObject.SetActive(shown);
                }
                if (shown)
                {
                    int seconds = Mathf.Max(0, Mathf.CeilToInt(manager.ClockRemaining));
                    if (force || seconds != shownClockSeconds)
                    {
                        shownClockSeconds = seconds;
                        clockText.text = seconds >= 60 ? $"{seconds / 60}:{seconds % 60:00}" : seconds.ToString();
                    }
                }
            }
        }

        /// <summary>A line of words over the board for three seconds.</summary>
        public void Toast(string words)
        {
            if (toastText == null)
            {
                return;
            }
            if (toasting != null)
            {
                StopCoroutine(toasting);
            }
            toasting = StartCoroutine(ShowToast(words));
        }

        private IEnumerator ShowToast(string words)
        {
            toastText.text = words ?? "";
            toastText.gameObject.SetActive(true);
            Color color = toastText.color;
            color.a = 1f;
            toastText.color = color;
            yield return Tween.Wait(2.4f, true);
            yield return Tween.Run(0.6f, t =>
            {
                Color c = toastText.color;
                c.a = 1f - t;
                toastText.color = c;
            }, true);
            toastText.text = "";
            color.a = 1f;
            toastText.color = color;
            toasting = null;
        }

        // ------------------------------------------------------------------ result

        private void RefreshResult()
        {
            BoardGamesManager manager = Games;
            if (manager == null)
            {
                return;
            }
            bool online = manager.IsOnlineGame;
            if (resultTitle != null)
            {
                resultTitle.text = manager.ResultTitle ?? "";
            }
            if (resultDetail != null)
            {
                resultDetail.text = manager.ResultDetail ?? "";
            }
            if (starsRow != null)
            {
                bool campaign = manager.CampaignLevel >= 0;
                starsRow.SetActive(campaign);
                if (campaign)
                {
                    int stars = Mathf.Clamp(manager.StarsEarned, 0, starImages.Length);
                    for (int i = 0; i < starImages.Length; i++)
                    {
                        if (starImages[i] != null)
                        {
                            starImages[i].color = i < stars ? starLit : starDim;
                        }
                    }
                }
            }
            SetLabel(againButton, online ? L.T("Back to the room") : L.T("Play again"));
            SetLabel(menuButton, online ? L.T("Leave the match") : L.T("Menu"));
        }

        // ------------------------------------------------------------------ resign

        /// <summary>The first press asks, a second press within three seconds resigns.</summary>
        private void OnResignClicked()
        {
            BoardGamesManager manager = Games;
            if (manager == null || manager.Table == null)
            {
                return;
            }
            if (Time.unscaledTime - resignAskedAt <= ResignConfirmSeconds)
            {
                resignAskedAt = -100f;
                SetLabel(resignButton, L.T("Resign"));
                manager.ResignLocal();
                return;
            }
            resignAskedAt = Time.unscaledTime;
            SetLabel(resignButton, L.T("Resign?"));
        }

        // ------------------------------------------------------------------ helpers

        private static void SetVisible(Button button, bool visible)
        {
            if (button != null && button.gameObject.activeSelf != visible)
            {
                button.gameObject.SetActive(visible);
            }
        }

        private static void SetLabel(Button button, string words)
        {
            if (button == null)
            {
                return;
            }
            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = words;
            }
        }
    }
}
