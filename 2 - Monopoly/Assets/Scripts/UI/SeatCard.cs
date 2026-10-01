using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Monopoly
{
    /// <summary>A seat of the new game screen: who plays it (human, computer or nobody), the name, the token and the computer's level.</summary>
    public class SeatCard : MonoBehaviour
    {
        [SerializeField] private Image band;
        [SerializeField] private Image badge;
        [SerializeField] private Image token;
        [SerializeField] private TMP_Text tokenName;
        [SerializeField] private TMP_InputField nameField;
        [SerializeField] private Button previous;
        [SerializeField] private Button next;
        [SerializeField] private Button[] kinds = new Button[3];
        [SerializeField] private Image[] kindBackgrounds = new Image[3];
        [SerializeField] private Button[] levels = new Button[3];
        [SerializeField] private Image[] levelBackgrounds = new Image[3];
        [SerializeField] private GameObject levelRow;
        [SerializeField] private CanvasGroup content;

        private SeatSetup seat;
        private int index;
        private Func<int, Sprite> tokens;
        private Action<SeatCard, int> changeToken;
        private Action changed;

        public int Index => index;

        private void Awake()
        {
            previous.onClick.AddListener(() => changeToken?.Invoke(this, -1));
            next.onClick.AddListener(() => changeToken?.Invoke(this, 1));
            for (int i = 0; i < kinds.Length; i++)
            {
                var kind = (SeatKind)i;
                kinds[i].onClick.AddListener(() => SetKind(kind));
            }
            for (int i = 0; i < levels.Length; i++)
            {
                var level = (BotLevel)i;
                levels[i].onClick.AddListener(() =>
                {
                    if (seat != null)
                    {
                        seat.level = level;
                        Paint();
                        changed?.Invoke();
                    }
                });
            }
            nameField.onEndEdit.AddListener(text =>
            {
                if (seat != null)
                {
                    // A default name shown in another language keeps its English (the grammar of "You" hangs on it).
                    string typed = text != null ? text.Trim() : "";
                    if (typed != MonopolyStyle.DisplayName(seat.name))
                    {
                        seat.name = string.IsNullOrWhiteSpace(typed) ? DefaultName() : typed;
                    }
                    ShowName(MonopolyStyle.DisplayName(seat.name));
                    changed?.Invoke();
                }
            });
        }

        public void Bind(int seatIndex, SeatSetup value, Func<int, Sprite> tokenSprites, Action<SeatCard, int> onToken, Action onChanged)
        {
            index = seatIndex;
            seat = value;
            tokens = tokenSprites;
            changeToken = onToken;
            changed = onChanged;
            Color color = MonopolyStyle.PlayerColor(seatIndex);
            band.color = color;
            badge.color = color;
            ShowName(MonopolyStyle.DisplayName(seat.name));
            Paint();
        }

        public SeatSetup Seat => seat;

        /// <summary>
        /// Shows a name in the field. An input field is not laid out right to left by the shared text support, so a
        /// Hebrew name (a default name in Hebrew, or one typed in Hebrew) turns TMP's right-to-left mode on itself.
        /// </summary>
        private void ShowName(string name)
        {
            nameField.SetTextWithoutNotify(name);
            bool rightToLeft = Gamebox.Bidi.HasRightToLeft(name);
            if (nameField.textComponent != null)
            {
                nameField.textComponent.isRightToLeftText = rightToLeft;
            }
        }

        private void SetKind(SeatKind kind)
        {
            if (seat == null || seat.kind == kind)
            {
                return;
            }
            SeatKind previousKind = seat.kind;
            seat.kind = kind;
            // A computer player gets a computer name, a human player keeps what was typed.
            if (kind == SeatKind.Computer && previousKind == SeatKind.Human && (seat.name.StartsWith("Player") || MatchSetup.IsYou(seat.name)))
            {
                seat.name = MatchSetup.BotNames[index % MatchSetup.BotNames.Length];
            }
            else if (kind == SeatKind.Human && previousKind == SeatKind.Computer && Array.IndexOf(MatchSetup.BotNames, seat.name) >= 0)
            {
                seat.name = $"Player {index + 1}";
            }
            ShowName(MonopolyStyle.DisplayName(seat.name));
            Paint();
            changed?.Invoke();
        }

        private string DefaultName()
        {
            return seat.kind == SeatKind.Computer ? MatchSetup.BotNames[index % MatchSetup.BotNames.Length] : $"Player {index + 1}";
        }

        public void Paint()
        {
            if (seat == null)
            {
                return;
            }
            Color color = MonopolyStyle.PlayerColor(index);
            band.color = color;
            badge.color = color;
            token.sprite = tokens?.Invoke(seat.token);
            token.color = MonopolyStyle.TextOn(color);
            tokenName.text = MonopolyStyle.TokenName(seat.token);
            for (int i = 0; i < kindBackgrounds.Length; i++)
            {
                bool on = (int)seat.kind == i;
                kindBackgrounds[i].color = on ? (i == 2 ? MonopolyStyle.Muted : color) : MonopolyStyle.Paper;
                TMP_Text label = kinds[i].GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    label.color = on ? MonopolyStyle.TextOn(kindBackgrounds[i].color) : MonopolyStyle.Ink;
                }
            }
            levelRow.SetActive(seat.kind == SeatKind.Computer);
            for (int i = 0; i < levelBackgrounds.Length; i++)
            {
                bool on = (int)seat.level == i;
                levelBackgrounds[i].color = on ? MonopolyStyle.Plate : MonopolyStyle.Paper;
                TMP_Text label = levels[i].GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    label.color = on ? Color.white : MonopolyStyle.Ink;
                }
            }
            bool active = seat.kind != SeatKind.Off;
            if (content != null)
            {
                content.alpha = active ? 1f : 0.45f;
            }
            nameField.interactable = active;
            previous.interactable = active;
            next.interactable = active;
        }
    }
}
