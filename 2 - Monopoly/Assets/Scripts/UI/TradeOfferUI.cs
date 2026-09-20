using System;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Monopoly
{
    /// <summary>A trade offered to a human player: what they would get and give, with Accept and Decline.</summary>
    public class TradeOfferUI : Popup
    {
        [SerializeField] private Image fromBadge;
        [SerializeField] private Image fromToken;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text getText;
        [SerializeField] private TMP_Text giveText;
        [SerializeField] private Button acceptButton;
        [SerializeField] private Button declineButton;

        private Action<bool> answer;

        private void Awake()
        {
            acceptButton.onClick.AddListener(() => Answer(true));
            declineButton.onClick.AddListener(() => Answer(false));
        }

        /// <summary>Shows <paramref name="offer"/> to its recipient.</summary>
        public void Show(MonopolyMatch match, TradeOffer offer, Sprite token, Action<bool> onAnswer)
        {
            answer = onAnswer;
            PlayerState from = match.players[offer.from];
            PlayerState to = match.players[offer.to];
            if (fromBadge != null)
            {
                fromBadge.color = MonopolyStyle.PlayerColor(from.color);
            }
            if (fromToken != null)
            {
                fromToken.sprite = token;
            }
            if (title != null)
            {
                title.text = $"{MonopolyStyle.Named(from)} {MonopolyStyle.Verb(from, "offers")} {MonopolyStyle.NamedObject(to)} a trade";
            }
            if (getText != null)
            {
                getText.text = Describe(match, offer.giveSpaces, offer.giveCash, offer.giveJailCards);
            }
            if (giveText != null)
            {
                giveText.text = Describe(match, offer.getSpaces, offer.getCash, offer.getJailCards);
            }
            Open();
        }

        private void Answer(bool accepted)
        {
            Action<bool> callback = answer;
            answer = null;
            Close();
            callback?.Invoke(accepted);
        }

        private static string Describe(MonopolyMatch match, System.Collections.Generic.List<int> spaces, int cash, int cards)
        {
            var text = new StringBuilder();
            foreach (int space in spaces.OrderBy(s => s))
            {
                SpaceData data = match.Space(space);
                string mortgaged = match.Deed(space).mortgaged ? " <size=80%><color=#E4002B>(mortgaged)</color></size>" : "";
                text.Append($"<color={MonopolyStyle.ColorTag(MonopolyStyle.GroupColor(data.group))}>{Icons.Square}</color> {data.name}{mortgaged}\n");
            }
            if (cash > 0)
            {
                text.Append($"{Icons.Coins} {MonopolyStyle.Money(cash)}\n");
            }
            if (cards > 0)
            {
                text.Append($"{Icons.Ticket} Get Out of Jail Free\n");
            }
            return text.Length == 0 ? "<color=#8C96A5>Nothing</color>" : text.ToString().TrimEnd('\n');
        }
    }
}
