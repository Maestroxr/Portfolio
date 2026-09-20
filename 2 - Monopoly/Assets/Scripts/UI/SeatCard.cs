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
                    seat.name = string.IsNullOrWhiteSpace(text) ? DefaultName() : text.Trim();
                    nameField.SetTextWithoutNotify(seat.name);
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
            nameField.SetTextWithoutNotify(seat.name);
            Paint();
        }

        public SeatSetup Seat => seat;

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
            nameField.SetTextWithoutNotify(seat.name);
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
            token.sprite = tokens?.Invoke(seat.token);
            tokenName.text = MonopolyStyle.TokenNames[Mathf.Clamp(seat.token, 0, MonopolyStyle.TokenCount - 1)];
            for (int i = 0; i < kindBackgrounds.Length; i++)
            {
                bool on = (int)seat.kind == i;
                kindBackgrounds[i].color = on ? (i == 2 ? MonopolyStyle.Muted : color) : Color.white;
                TMP_Text label = kinds[i].GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    label.color = on ? Color.white : MonopolyStyle.Ink;
                }
            }
            levelRow.SetActive(seat.kind == SeatKind.Computer);
            for (int i = 0; i < levelBackgrounds.Length; i++)
            {
                bool on = (int)seat.level == i;
                levelBackgrounds[i].color = on ? MonopolyStyle.Ink : Color.white;
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
