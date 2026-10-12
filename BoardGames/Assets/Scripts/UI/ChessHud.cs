using System;
using Gamebox;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.BoardGames
{
    /// <summary>
    /// The chess part of the HUD ("Hud/SafeArea/Parts/ChessHud"): the promotion picker. When a person moves a pawn to
    /// the last rank it asks which piece the pawn becomes: four buttons with the pieces' names (keys Q, R, B and N
    /// too). Escape, the back button or a click anywhere else cancels, and the pawn stays where it was.
    /// </summary>
    public sealed class ChessHud : MonoBehaviour
    {
        [SerializeField] private BoardGamesManager manager;
        [Tooltip("The picker: a screen wide catcher of stray clicks with the panel on it (inactive while closed).")]
        [SerializeField] private GameObject picker;
        [SerializeField] private Button blocker;
        [SerializeField] private Image panel;
        [SerializeField] private TMP_Text title;
        [Tooltip("Queen, rook, bishop and knight, in the order of ChessPieces.Promotions.")]
        [SerializeField] private Button[] buttons = new Button[4];
        [SerializeField] private TMP_Text[] labels = new TMP_Text[4];

        private static readonly KeyCode[] Keys = { KeyCode.Q, KeyCode.R, KeyCode.B, KeyCode.N };

        private Action<ChessPieceKind> chosen;
        private Action cancelled;
        private Func<bool> backHandler;

        /// <summary>Whether the picker is up, waiting for a choice.</summary>
        public bool IsOpen => picker != null && picker.activeSelf;

        private void Awake()
        {
            for (int i = 0; i < buttons.Length && i < ChessPieces.Promotions.Length; i++)
            {
                int index = i;
                if (buttons[i] != null)
                {
                    buttons[i].onClick.AddListener(() => Choose(index));
                }
            }
            if (blocker != null)
            {
                blocker.onClick.AddListener(Cancel);
            }
            if (picker != null)
            {
                picker.SetActive(false);
            }
        }

        private void OnEnable()
        {
            backHandler ??= HandleBack;
            manager?.AddBackHandler(backHandler);
        }

        private void OnDisable()
        {
            if (backHandler != null)
            {
                manager?.RemoveBackHandler(backHandler);
            }
            // The HUD went away (the menu, the results): a pending choice is dropped.
            if (IsOpen)
            {
                Cancel();
            }
        }

        private void Update()
        {
            if (!IsOpen)
            {
                return;
            }
            for (int i = 0; i < Keys.Length; i++)
            {
                if (Input.GetKeyDown(Keys[i]))
                {
                    Choose(i);
                    return;
                }
            }
        }

        /// <summary>Opens the picker; <paramref name="chosen"/> hears the piece picked, <paramref name="cancelled"/> a cancel. Exactly one of them is called.</summary>
        public void ShowPromotion(Action<ChessPieceKind> chosen, Action cancelled)
        {
            if (picker == null)
            {
                cancelled?.Invoke();
                return;
            }
            this.chosen = chosen;
            this.cancelled = cancelled;
            RefreshTexts();
            picker.SetActive(true);
        }

        /// <summary>Closes the picker without calling anybody back (the game moved on).</summary>
        public void Hide()
        {
            chosen = null;
            cancelled = null;
            if (picker != null)
            {
                picker.SetActive(false);
            }
        }

        /// <summary>Writes the words in the language shown.</summary>
        public void RefreshTexts()
        {
            if (title != null)
            {
                title.text = L.T("Promote the pawn to:");
            }
            for (int i = 0; i < labels.Length && i < ChessPieces.Promotions.Length; i++)
            {
                if (labels[i] != null)
                {
                    labels[i].text = L.T(ChessPieces.NameKey(ChessPieces.Promotions[i]));
                }
            }
        }

        /// <summary>The colours of the look's menu: its window, its words, its buttons.</summary>
        public void ApplyLook(BoardGamesTheme theme)
        {
            MenuSkin menu = theme != null ? theme.Menu : null;
            if (menu == null)
            {
                return;
            }
            if (panel != null)
            {
                Color window = menu.windowColor;
                window.a = 0.96f;
                panel.color = window;
            }
            if (title != null)
            {
                title.color = menu.textColor;
            }
            foreach (TMP_Text label in labels)
            {
                if (label != null)
                {
                    label.color = menu.buttonTextColor;
                }
            }
        }

        private bool HandleBack()
        {
            if (!IsOpen)
            {
                return false;
            }
            Cancel();
            return true;
        }

        private void Choose(int index)
        {
            Action<ChessPieceKind> callback = chosen;
            Hide();
            if (index >= 0 && index < ChessPieces.Promotions.Length)
            {
                callback?.Invoke(ChessPieces.Promotions[index]);
            }
        }

        private void Cancel()
        {
            Action callback = cancelled;
            Hide();
            callback?.Invoke();
        }
    }
}
