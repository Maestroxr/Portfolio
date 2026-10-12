using Gamebox;
using Gamebox.Online;
using Gamebox.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.BoardGames
{
    /// <summary>
    /// The three tabs over the title's menu that pick the game the title shows and its buttons play: Checkers, Chess and
    /// Backgammon (keys 1, 2 and 3 too). They sit on a bar in the colour of the menu's window; the selected tab is filled
    /// with the look's accent colour, the others are only outlined. The tabs show on the title alone, while the menu's
    /// buttons are on the screen and no window (the campaign, the local setup, the saved games, the lobby, the settings)
    /// covers them; a click asks the manager to switch (<see cref="BoardGamesManager.SelectMode"/>), and the board
    /// behind the menu changes with it.
    /// </summary>
    public sealed class ModeTabs : MonoBehaviour
    {
        [SerializeField] private BoardGamesManager manager;
        [SerializeField] private BoardGamesUI ui;
        [SerializeField] private BoardGamesAudio sound;
        [Tooltip("The row of tabs, shown and hidden as a whole.")]
        [SerializeField] private GameObject row;
        [Tooltip("The bar under the tabs, in the colour of the menu's window.")]
        [SerializeField] private Image bar;
        [Tooltip("The tabs by game: Checkers, Chess, Backgammon.")]
        [SerializeField] private Button[] tabs = new Button[3];
        [Tooltip("The fill of each tab: the accent colour on the selected one, clear on the others.")]
        [SerializeField] private Image[] fills = new Image[3];
        [Tooltip("The outline of each tab, shown on the tabs not selected.")]
        [SerializeField] private Image[] frames = new Image[3];
        [SerializeField] private TMP_Text[] labels = new TMP_Text[3];
        [Header("Colours when the look has none")]
        [SerializeField] private Color accent = new Color(0.12f, 0.35f, 0.21f);
        [SerializeField] private Color barColor = new Color(0.96f, 0.92f, 0.84f);
        [SerializeField] private Color ink = new Color(0.16f, 0.14f, 0.13f);

        private OnlineLobbyUI lobby;
        private bool lobbySearched;
        private int paintedMode = -1;
        private Color paintedAccent;
        private Color paintedBar;
        private Color paintedInk;

        private void Awake()
        {
            for (int i = 0; i < tabs.Length; i++)
            {
                var mode = (GameMode)i;
                if (tabs[i] != null)
                {
                    tabs[i].onClick.AddListener(() => Select(mode));
                }
            }
            if (row != null)
            {
                row.SetActive(false);
            }
        }

        private void Update()
        {
            bool visible = ShouldShow();
            if (row != null && row.activeSelf != visible)
            {
                row.SetActive(visible);
                paintedMode = -1;
            }
            if (!visible)
            {
                return;
            }
            for (int i = 0; i < tabs.Length; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
                {
                    Select((GameMode)i);
                }
            }
            if (paintedMode != (int)manager.TitleMode || !Painted)
            {
                Paint(manager.TitleMode);
            }
        }

        /// <summary>A tab or its key: the title shows that game now.</summary>
        public void Select(GameMode mode)
        {
            if (!ShouldShow())
            {
                return;
            }
            if (mode != manager.TitleMode)
            {
                // The manager clicks when the game changes.
                manager.SelectMode(mode);
            }
            else
            {
                sound?.Play(ClipKind.Click);
            }
            Paint(manager.TitleMode);
        }

        /// <summary>Whether the tabs belong on the screen now: the title's menu with nothing over it.</summary>
        private bool ShouldShow()
        {
            if (manager == null || ui == null || manager.Table != null || !ui.OnTitle || !ui.IsMenuShown)
            {
                return false;
            }
            foreach (OverlayWindow window in manager.GetComponents<OverlayWindow>())
            {
                if (window.IsOpen)
                {
                    return false;
                }
            }
            OnlineLobbyUI shownLobby = Lobby;
            return shownLobby == null || !shownLobby.IsOpen;
        }

        private OnlineLobbyUI Lobby
        {
            get
            {
                if (lobby == null && !lobbySearched)
                {
                    lobbySearched = true;
                    lobby = FindAnyObjectByType<OnlineLobbyUI>(FindObjectsInactive.Include);
                }
                return lobby;
            }
        }

        private MenuSkin Skin
        {
            get
            {
                BoardGamesTheme look = manager != null ? manager.Look : null;
                return look != null ? look.Menu : null;
            }
        }

        /// <summary>The look's accent colour (the fill of the menu's sliders), or the built one.</summary>
        private Color Accent => Skin != null && MenuSkin.IsSet(Skin.fillColor) ? Skin.fillColor : accent;

        /// <summary>The colour of the menu's window, or the built one.</summary>
        private Color Bar => Skin != null && MenuSkin.IsSet(Skin.windowColor) ? Skin.windowColor : barColor;

        /// <summary>The colour of the menu's words on its window, or the built one.</summary>
        private Color Ink => Skin != null && MenuSkin.IsSet(Skin.textColor) ? Skin.textColor : ink;

        /// <summary>Whether the tabs wear the look's colours now.</summary>
        private bool Painted => paintedAccent == Accent && paintedBar == Bar && paintedInk == Ink;

        /// <summary>Fills the selected tab with the accent colour and leaves the others outlined, on the bar.</summary>
        private void Paint(GameMode selected)
        {
            paintedMode = (int)selected;
            paintedAccent = Accent;
            paintedBar = Bar;
            paintedInk = Ink;
            Color fill = new Color(paintedAccent.r, paintedAccent.g, paintedAccent.b, 1f);
            // Words on the accent: white on a dark one, the darker of the bar and the ink on a light one.
            Color dark = Luminance(paintedBar) < Luminance(paintedInk) ? paintedBar : paintedInk;
            Color selectedText = Luminance(fill) > 0.55f ? new Color(dark.r, dark.g, dark.b, 1f) : Color.white;
            if (bar != null)
            {
                bar.color = new Color(paintedBar.r, paintedBar.g, paintedBar.b, 0.94f);
            }
            for (int i = 0; i < tabs.Length; i++)
            {
                bool on = i == (int)selected;
                if (i < fills.Length && fills[i] != null)
                {
                    fills[i].color = on ? fill : Color.clear;
                }
                if (i < frames.Length && frames[i] != null)
                {
                    frames[i].color = new Color(paintedInk.r, paintedInk.g, paintedInk.b, on ? 0f : 0.35f);
                }
                if (i < labels.Length && labels[i] != null)
                {
                    labels[i].color = on ? selectedText : paintedInk;
                }
            }
        }

        private static float Luminance(Color color)
        {
            return 0.2126f * color.r + 0.7152f * color.g + 0.0722f * color.b;
        }
    }
}
