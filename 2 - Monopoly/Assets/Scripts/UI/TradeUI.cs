using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Monopoly
{
    /// <summary>
    /// Puts a trade together: pick another player, tick the properties going each way, add cash and Get Out of Jail
    /// Free cards, and propose. The rules (no buildings on a traded set, enough cash for the mortgage interest) are
    /// checked as the offer changes; a computer player answers at once, a human one is asked on the same screen (in an
    /// online match on their own).
    /// </summary>
    public class TradeUI : Popup
    {
        [Serializable]
        public class PartnerButton
        {
            public Button button;
            public Image badge;
            public Image token;
            public TMP_Text name;
            public Image frame;
        }

        [SerializeField] private TMP_Text title;
        [SerializeField] private PartnerButton[] partners = new PartnerButton[0];
        [SerializeField] private TMP_Text giveHeader;
        [SerializeField] private TMP_Text getHeader;
        [SerializeField] private RectTransform giveList;
        [SerializeField] private RectTransform getList;
        [SerializeField] private TradeItem template;
        [SerializeField] private TMP_Text giveCashText;
        [SerializeField] private TMP_Text getCashText;
        [SerializeField] private Button[] giveCashButtons = new Button[4];
        [SerializeField] private Button[] getCashButtons = new Button[4];
        [SerializeField] private Toggle giveCard;
        [SerializeField] private Toggle getCard;
        [SerializeField] private TMP_Text status;
        [SerializeField] private Button proposeButton;
        [SerializeField] private Button cancelButton;

        private static readonly int[] Steps = { -50, -10, 10, 50 };

        private readonly List<TradeItem> giveItems = new List<TradeItem>();
        private readonly List<TradeItem> getItems = new List<TradeItem>();
        private readonly List<int> partnerSeats = new List<int>();
        private MonopolyMatch match;
        private IMonopolyCommands controller;
        private Func<int, Sprite> tokens;
        private int seat;
        private int partner = -1;
        private bool online;
        private int giveCash;
        private int getCash;

        private void Awake()
        {
            if (template != null)
            {
                template.gameObject.SetActive(false);
            }
            for (int i = 0; i < partners.Length; i++)
            {
                int index = i;
                partners[i].button.onClick.AddListener(() => SelectPartner(index));
            }
            for (int i = 0; i < giveCashButtons.Length && i < Steps.Length; i++)
            {
                int step = Steps[i];
                giveCashButtons[i].onClick.AddListener(() => ChangeCash(ref giveCash, step, true));
            }
            for (int i = 0; i < getCashButtons.Length && i < Steps.Length; i++)
            {
                int step = Steps[i];
                getCashButtons[i].onClick.AddListener(() => ChangeCash(ref getCash, step, false));
            }
            if (giveCard != null)
            {
                giveCard.onValueChanged.AddListener(_ => Validate());
            }
            if (getCard != null)
            {
                getCard.onValueChanged.AddListener(_ => Validate());
            }
            if (proposeButton != null)
            {
                proposeButton.onClick.AddListener(Propose);
            }
            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(Close);
            }
        }

        /// <summary>Opens the window for <paramref name="proposer"/>; <paramref name="remote"/> when the other people play on devices of their own.</summary>
        public void Show(MonopolyMatch value, int proposer, IMonopolyCommands owner, Func<int, Sprite> tokenSprites, bool remote = false)
        {
            match = value;
            seat = proposer;
            controller = owner;
            tokens = tokenSprites;
            online = remote;
            partnerSeats.Clear();
            partnerSeats.AddRange(match.players.Where(p => !p.bankrupt && p.index != seat).Select(p => p.index));
            for (int i = 0; i < partners.Length; i++)
            {
                bool used = i < partnerSeats.Count;
                partners[i].button.gameObject.SetActive(used);
                if (!used)
                {
                    continue;
                }
                PlayerState other = match.players[partnerSeats[i]];
                partners[i].badge.color = MonopolyStyle.PlayerColor(other.color);
                partners[i].token.sprite = tokens?.Invoke(other.token);
                partners[i].name.text = other.name;
            }
            if (title != null)
            {
                title.text = $"Trade: {MonopolyStyle.Named(match.players[seat])}";
            }
            Open();
            SelectPartner(0);
        }

        private void SelectPartner(int index)
        {
            if (index < 0 || index >= partnerSeats.Count)
            {
                return;
            }
            partner = partnerSeats[index];
            for (int i = 0; i < partners.Length; i++)
            {
                if (partners[i].frame != null)
                {
                    partners[i].frame.color = i == index ? MonopolyStyle.Tint(MonopolyStyle.PlayerColor(match.players[partnerSeats[Math.Min(i, partnerSeats.Count - 1)]].color), 0.6f) : Color.white;
                }
            }
            giveCash = 0;
            getCash = 0;
            Fill(giveItems, giveList, match.TradableProperties(seat).ToList());
            Fill(getItems, getList, match.TradableProperties(partner).ToList());
            PlayerState me = match.players[seat];
            PlayerState them = match.players[partner];
            if (giveHeader != null)
            {
                giveHeader.text = $"You give ({MonopolyStyle.Money(me.cash)})";
            }
            if (getHeader != null)
            {
                getHeader.text = $"{MonopolyStyle.Named(them)} {MonopolyStyle.Verb(them, "gives")} ({MonopolyStyle.Money(them.cash)})";
            }
            if (giveCard != null)
            {
                giveCard.isOn = false;
                giveCard.gameObject.SetActive(me.jailCards.Count > 0);
            }
            if (getCard != null)
            {
                getCard.isOn = false;
                getCard.gameObject.SetActive(them.jailCards.Count > 0);
            }
            Validate();
        }

        private void Fill(List<TradeItem> items, RectTransform list, List<int> spaces)
        {
            spaces = spaces.OrderBy(space => Array.IndexOf(MonopolyStyle.Groups, match.Space(space).group)).ThenBy(space => space).ToList();
            while (items.Count < spaces.Count)
            {
                TradeItem item = Instantiate(template, list);
                items.Add(item);
            }
            for (int i = 0; i < items.Count; i++)
            {
                bool used = i < spaces.Count;
                items[i].gameObject.SetActive(used);
                if (used)
                {
                    items[i].Bind(match, spaces[i], Validate);
                }
            }
        }

        private void ChangeCash(ref int amount, int step, bool mine)
        {
            int limit = match.players[mine ? seat : partner].cash;
            amount = Mathf.Clamp(amount + step, 0, limit);
            Validate();
        }

        private TradeOffer Build()
        {
            var offer = new TradeOffer { from = seat, to = partner, giveCash = giveCash, getCash = getCash };
            offer.giveSpaces.AddRange(giveItems.Where(i => i.gameObject.activeSelf && i.IsOn).Select(i => i.Space));
            offer.getSpaces.AddRange(getItems.Where(i => i.gameObject.activeSelf && i.IsOn).Select(i => i.Space));
            offer.giveJailCards = giveCard != null && giveCard.isOn ? 1 : 0;
            offer.getJailCards = getCard != null && getCard.isOn ? 1 : 0;
            return offer;
        }

        private void Validate()
        {
            if (match == null || partner < 0)
            {
                return;
            }
            if (giveCashText != null)
            {
                giveCashText.text = MonopolyStyle.Money(giveCash);
            }
            if (getCashText != null)
            {
                getCashText.text = MonopolyStyle.Money(getCash);
            }
            TradeOffer offer = Build();
            bool ok = match.CanTrade(offer, out string reason);
            if (proposeButton != null)
            {
                proposeButton.interactable = ok;
            }
            if (status != null)
            {
                status.text = ok ? (match.players[partner].bot ? $"{match.players[partner].name} weighs every deal by the sets it makes."
                    : online ? $"{match.players[partner].name} answers on their own device." : "Hand the device over for an answer.") : reason;
                status.color = ok ? MonopolyStyle.Muted : MonopolyStyle.Red;
            }
        }

        private void Propose()
        {
            TradeOffer offer = Build();
            if (!match.CanTrade(offer, out _))
            {
                return;
            }
            Close();
            controller.ProposeTrade(offer);
        }
    }
}
