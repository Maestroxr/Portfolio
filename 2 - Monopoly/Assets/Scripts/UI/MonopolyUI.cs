using System.Collections;
using Gamebox;
using Gamebox.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Monopoly
{
    /// <summary>
    /// The interface of the game on top of the shared <see cref="GameUI"/> menu fields. One menu panel serves as the
    /// title screen (Play, Continue, House Rules, Exit) and as the pause menu (Resume, Save, Load, New Game, Main Menu);
    /// in play the HUD shows the four player panels, the action panel in the middle of the board, the news feed and
    /// the match info, and the popups (title deeds, cards, auctions, the property manager, trades, results).
    /// </summary>
    public class MonopolyUI : GameUI
    {
        [Header("Screens")]
        [SerializeField] private RectTransform hud;
        [SerializeField] private CanvasGroup hudGroup;
        [SerializeField] private Image dim;
        [SerializeField] private GameObject titleLogo;
        [SerializeField] private TMP_Text menuTitle;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private TMP_Text startLabel;
        [SerializeField] private TMP_Text loadLabel;
        [SerializeField] private TMP_Text footer;

        [Header("HUD")]
        [SerializeField] private PlayerUI[] players = new PlayerUI[4];
        [SerializeField] private RectTransform leftColumn;
        [SerializeField] private RectTransform rightColumn;
        [SerializeField] private TMP_Text modeText;
        [SerializeField] private TMP_Text roundText;
        [SerializeField] private GameObject potRow;
        [SerializeField] private TMP_Text potText;
        [SerializeField] private TMP_Text bankText;
        [SerializeField] private Button soundButton;
        [SerializeField] private TMP_Text soundIcon;
        [SerializeField] private Button musicButton;
        [SerializeField] private TMP_Text musicIcon;
        [SerializeField] private RectTransform banner;
        [SerializeField] private TMP_Text bannerText;
        [SerializeField] private Image bannerBackground;

        [Header("Parts")]
        [SerializeField] private ActionPanel actions;
        [SerializeField] private DeedCardUI deed;
        [SerializeField] private CardRevealUI cards;
        [SerializeField] private AuctionUI auction;
        [SerializeField] private ManageUI manage;
        [SerializeField] private TradeUI trade;
        [SerializeField] private TradeOfferUI offer;
        [SerializeField] private SetupUI setup;
        [SerializeField] private ResultsUI results;
        [SerializeField] private Toasts toasts;
        [SerializeField] private FloatingTexts floating;
        [SerializeField] private Sprite[] tokenSprites = new Sprite[0];
        [SerializeField] private MonopolyAudio sound;
        [SerializeField] private CameraRig cameraRig;

        private Coroutine bannerRoutine;
        private float dimTarget;
        private Vector2 lastCanvasSize;

        public MonopolyGameManager Monopoly => Manager as MonopolyGameManager;
        public ActionPanel Actions => actions;
        public DeedCardUI Deed => deed;
        public CardRevealUI Cards => cards;
        public AuctionUI Auction => auction;
        public ManageUI Manage => manage;
        public TradeUI Trade => trade;
        public TradeOfferUI Offer => offer;
        public SetupUI Setup => setup;
        public ResultsUI Results => results;
        public Toasts Toasts => toasts;
        public FloatingTexts Floating => floating;
        public MonopolyAudio Sound => sound;

        public Sprite TokenSprite(int token)
        {
            return token >= 0 && token < tokenSprites.Length ? tokenSprites[token] : null;
        }

        public PlayerUI Panel(int seat)
        {
            return seat >= 0 && seat < players.Length ? players[seat] : null;
        }

        protected override void Awake()
        {
            base.Awake();
            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(() => Monopoly?.ReturnToTitle());
            }
            if (soundButton != null)
            {
                soundButton.onClick.AddListener(ToggleSound);
            }
            if (musicButton != null)
            {
                musicButton.onClick.AddListener(ToggleMusic);
            }
            foreach (Button button in GetComponentsInChildren<Button>(true))
            {
                button.onClick.AddListener(() => sound?.Play(Sfx.Click));
            }
            if (banner != null)
            {
                banner.gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            PaintSoundButtons();
        }

        // ------------------------------------------------------------------ screens

        protected override void OnStartNewGameClicked()
        {
            ShowSetup();
        }

        public void ShowSetup()
        {
            HideSettings();
            if (MenuRoot != null)
            {
                MenuRoot.gameObject.SetActive(false);
            }
            results?.HideNow();
            setup?.Show(Monopoly != null ? Monopoly.ModeList : null, Monopoly != null ? Monopoly.Progress : null, TokenSprite,
                (chosen, mode) => Monopoly?.BeginMatch(chosen, mode), ShowMenu);
        }

        public override void UpdateGameState(GameState state)
        {
            base.UpdateGameState(state);
            bool title = state.BaseState == BaseGameState.Initialization;
            bool paused = state.BaseState == BaseGameState.Paused;
            bool over = state.BaseState == BaseGameState.GameOver || state.BaseState == BaseGameState.Victory;
            if (hud != null)
            {
                hud.gameObject.SetActive(!title);
            }
            if (titleLogo != null)
            {
                titleLogo.SetActive(title);
            }
            if (menuTitle != null)
            {
                menuTitle.gameObject.SetActive(paused);
                menuTitle.text = "PAUSED";
            }
            SetButtonVisible(ReturnToGame, paused);
            SetButtonVisible(SaveGame, paused);
            if (mainMenuButton != null)
            {
                mainMenuButton.gameObject.SetActive(paused);
            }
            if (startLabel != null)
            {
                startLabel.text = title ? "PLAY" : "NEW GAME";
            }
            if (loadLabel != null)
            {
                loadLabel.text = title ? "CONTINUE" : "LOAD GAME";
            }
            if (footer != null)
            {
                footer.gameObject.SetActive(title);
            }
            if (over && MenuRoot != null)
            {
                MenuRoot.gameObject.SetActive(false);
            }
            if (!title && setup != null && setup.IsOpen)
            {
                setup.HideNow();
            }
            if (!over)
            {
                results?.HideNow();
            }
            dimTarget = paused ? 0.55f : title ? 0.12f : 0f;
            cameraRig?.SetMode(title ? CameraRig.Mode.Title : CameraRig.Mode.Overview);
            sound?.PlayMusic(!title);
            if (hudGroup != null)
            {
                hudGroup.interactable = !paused;
            }
        }

        private static void SetButtonVisible(Button button, bool visible)
        {
            if (button != null)
            {
                button.gameObject.SetActive(visible);
            }
        }

        public override bool HandleBack()
        {
            if (base.HandleBack())
            {
                return true;
            }
            if (setup != null && setup.IsOpen)
            {
                setup.GoBack();
                return true;
            }
            if (trade != null && trade.IsOpen)
            {
                trade.Close();
                return true;
            }
            if (manage != null && manage.IsOpen)
            {
                manage.Close();
                return true;
            }
            if (deed != null && deed.IsOpen && !deed.IsOffer)
            {
                deed.Close();
                return true;
            }
            return false;
        }

        /// <summary>Closes every popup (a new match, a loaded one, back to the title).</summary>
        public void CloseAll()
        {
            deed?.HideNow();
            cards?.HideNow();
            auction?.HideNow();
            manage?.HideNow();
            trade?.HideNow();
            offer?.HideNow();
            results?.HideNow();
            actions?.Hide();
            toasts?.Clear();
            if (banner != null)
            {
                banner.gameObject.SetActive(false);
            }
        }

        // ------------------------------------------------------------------ HUD

        public void BindPlayers(MonopolyMatch match)
        {
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i] == null)
                {
                    continue;
                }
                if (i < match.players.Count)
                {
                    players[i].Bind(match.players[i], TokenSprite(match.players[i].token));
                    players[i].Refresh(match);
                }
                else
                {
                    players[i].gameObject.SetActive(false);
                }
            }
        }

        public void RefreshPlayers(MonopolyMatch match)
        {
            foreach (PlayerUI panel in players)
            {
                if (panel != null && panel.gameObject.activeSelf)
                {
                    panel.Refresh(match);
                }
            }
            if (bankText != null && match != null)
            {
                bankText.text = match.rules.limitedBuildings ? $"{Icons.House} {match.housesLeft}   {Icons.Hotel} {match.hotelsLeft}" : "";
            }
        }

        public void SetTurn(int seat)
        {
            for (int i = 0; i < players.Length; i++)
            {
                players[i]?.SetTurn(i == seat);
            }
        }

        public void SetMatchInfo(string mode, int round, int limit)
        {
            if (modeText != null)
            {
                modeText.text = mode;
            }
            if (roundText != null)
            {
                roundText.text = limit > 0 ? $"Round {Mathf.Min(round, limit)} of {limit}" : $"Round {round}";
            }
        }

        public void SetPot(bool jackpot, int amount)
        {
            if (potRow != null)
            {
                potRow.SetActive(jackpot);
            }
            if (potText != null)
            {
                potText.text = MonopolyStyle.Money(amount);
            }
        }

        /// <summary>A big word across the board (DOUBLES!, JACKPOT!, GO TO JAIL!).</summary>
        public void Banner(string text, Color color, float seconds = 1.2f)
        {
            if (banner == null)
            {
                return;
            }
            if (bannerRoutine != null)
            {
                StopCoroutine(bannerRoutine);
            }
            bannerRoutine = StartCoroutine(ShowBanner(text, color, seconds));
        }

        private IEnumerator ShowBanner(string text, Color color, float seconds)
        {
            banner.gameObject.SetActive(true);
            banner.SetAsLastSibling();
            bannerText.text = text;
            if (bannerBackground != null)
            {
                bannerBackground.color = color;
            }
            var groupOfBanner = banner.GetComponent<CanvasGroup>();
            yield return Tween.Run(0.3f, t =>
            {
                float s = Tween.OutBack(t, 2.5f);
                banner.localScale = new Vector3(s, s, 1f);
                banner.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-8f, -3f, t));
                if (groupOfBanner != null)
                {
                    groupOfBanner.alpha = Mathf.Clamp01(t * 3f);
                }
            }, true);
            yield return Tween.Wait(seconds, true);
            yield return Tween.Run(0.25f, t =>
            {
                banner.localScale = new Vector3(1f + 0.3f * t, 1f + 0.3f * t, 1f);
                if (groupOfBanner != null)
                {
                    groupOfBanner.alpha = 1f - t;
                }
            }, true);
            banner.gameObject.SetActive(false);
            bannerRoutine = null;
        }

        public void Toast(string text, Color accent, string icon = null)
        {
            toasts?.Show(text, accent, icon);
        }

        // ------------------------------------------------------------------ sound switches

        private void ToggleSound()
        {
            if (sound == null)
            {
                return;
            }
            sound.SetSound(!sound.SoundOn);
            PaintSoundButtons();
        }

        private void ToggleMusic()
        {
            if (sound == null)
            {
                return;
            }
            sound.SetMusic(!sound.MusicOn);
            PaintSoundButtons();
        }

        private void PaintSoundButtons()
        {
            if (sound == null)
            {
                return;
            }
            if (soundIcon != null)
            {
                soundIcon.text = sound.SoundOn ? Icons.SoundOn : Icons.SoundOff;
                soundIcon.color = sound.SoundOn ? MonopolyStyle.Ink : MonopolyStyle.Muted;
            }
            if (musicIcon != null)
            {
                musicIcon.color = sound.MusicOn ? MonopolyStyle.Ink : MonopolyStyle.Muted;
            }
        }

        // ------------------------------------------------------------------ layout

        private void Update()
        {
            if (dim != null)
            {
                Color c = dim.color;
                c.a = Mathf.MoveTowards(c.a, dimTarget, Time.unscaledDeltaTime * 2f);
                dim.color = c;
                dim.raycastTarget = c.a > 0.3f;
            }
            UpdatePlayArea();
        }

        /// <summary>Tells the camera how much of the screen the player columns leave for the board.</summary>
        private void UpdatePlayArea()
        {
            if (cameraRig == null || Scaler == null || leftColumn == null || rightColumn == null)
            {
                return;
            }
            var canvas = (RectTransform)Scaler.transform;
            Vector2 size = canvas.rect.size;
            if (size == lastCanvasSize || size.x <= 0f)
            {
                return;
            }
            lastCanvasSize = size;
            float left = (leftColumn.rect.width + ColumnInset(leftColumn)) / size.x;
            float right = (rightColumn.rect.width + ColumnInset(rightColumn)) / size.x;
            cameraRig.SetPlayArea(new Vector4(left + 0.005f, 0.015f, 1f - right - 0.005f, 0.985f));
        }

        private static float ColumnInset(RectTransform column)
        {
            return Mathf.Abs(column.anchoredPosition.x) + 8f;
        }
    }
}
