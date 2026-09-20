using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Monopoly
{
    /// <summary>
    /// The auction of a property: the lot, the highest bid and bidder, every player's status, and the bid buttons of the
    /// human player whose turn it is to bid (computer players bid on their own).
    /// </summary>
    public class AuctionUI : Popup
    {
        [Serializable]
        public class BidderSlot
        {
            public GameObject root;
            public Image badge;
            public Image token;
            public TMP_Text name;
            public TMP_Text status;
            public Image frame;
        }

        [SerializeField] private Image band;
        [SerializeField] private TMP_Text lotName;
        [SerializeField] private TMP_Text lotPrice;
        [SerializeField] private TMP_Text bidText;
        [SerializeField] private TMP_Text leaderText;
        [SerializeField] private TMP_Text turnText;
        [SerializeField] private BidderSlot[] slots = new BidderSlot[0];
        [SerializeField] private Button[] raiseButtons = new Button[0];
        [SerializeField] private TMP_Text[] raiseLabels = new TMP_Text[0];
        [SerializeField] private Button passButton;
        [SerializeField] private GameObject humanControls;

        private static readonly int[] Raises = { 10, 50, 100 };

        private Action<int> onBid;
        private Action onPass;
        private int minimum;
        private int highBid;
        private bool anyBid;

        private void Awake()
        {
            for (int i = 0; i < raiseButtons.Length; i++)
            {
                int raise = i < Raises.Length ? Raises[i] : 10;
                raiseButtons[i].onClick.AddListener(() => onBid?.Invoke(anyBid ? highBid + raise : Math.Max(minimum, raise)));
            }
            if (passButton != null)
            {
                passButton.onClick.AddListener(() => onPass?.Invoke());
            }
        }

        /// <summary>Shows the auction as it stands; <paramref name="human"/> is the human bidder to take a bid from, or -1.</summary>
        public void Refresh(MonopolyMatch match, Func<int, Sprite> tokens, int human, Action<int> bid, Action pass)
        {
            AuctionState auction = match.auction;
            if (!auction.Running)
            {
                return;
            }
            onBid = bid;
            onPass = pass;
            SpaceData lot = match.Space(auction.space);
            minimum = auction.MinimumBid;
            highBid = auction.highBid;
            anyBid = auction.highBidder >= 0;
            if (band != null)
            {
                band.color = lot.kind == SpaceKind.Street ? MonopolyStyle.GroupColor(lot.group) : MonopolyStyle.Ink;
            }
            if (lotName != null)
            {
                lotName.text = lot.name.ToUpperInvariant();
                lotName.color = lot.kind == SpaceKind.Street ? MonopolyStyle.GroupTextColor(lot.group) : Color.white;
            }
            if (lotPrice != null)
            {
                lotPrice.text = $"List price {MonopolyStyle.Money(lot.price)}";
            }
            if (bidText != null)
            {
                bidText.text = anyBid ? MonopolyStyle.Money(auction.highBid) : "No bids yet";
            }
            if (leaderText != null)
            {
                PlayerState leader = anyBid ? match.players[auction.highBidder] : null;
                leaderText.text = anyBid ? $"{MonopolyStyle.Named(leader)} {MonopolyStyle.Verb(leader, "leads")}" :$"Opening bid {MonopolyStyle.Money(MonopolyMatch.MinimumBid)}";
            }
            int bidder = auction.Bidder;
            if (turnText != null)
            {
                PlayerState player = bidder >= 0 ? match.players[bidder] : null;
                turnText.text = player == null ? ""
                    : human >= 0 ? MonopolyStyle.IsYou(player) ? "Your bid" : $"Your bid, {MonopolyStyle.Named(player)}"
                    : $"{MonopolyStyle.Named(player)} {MonopolyStyle.Verb(player, "is")} thinking...";
            }
            for (int i = 0; i < slots.Length; i++)
            {
                BidderSlot slot = slots[i];
                bool used = i < match.players.Count;
                slot.root.SetActive(used);
                if (!used)
                {
                    continue;
                }
                PlayerState player = match.players[i];
                bool inAuction = auction.bidders.Contains(i);
                slot.badge.color = MonopolyStyle.PlayerColor(match.players[i].color);
                slot.token.sprite = tokens?.Invoke(player.token);
                slot.name.text = player.name;
                slot.status.text = player.bankrupt ? "Out of the game" : i == auction.highBidder ? "Leading" : inAuction ? (i == bidder ? "Bidding" : "In") : "Passed";
                slot.status.color = i == auction.highBidder ? MonopolyStyle.Green : inAuction ? MonopolyStyle.Ink : MonopolyStyle.Muted;
                if (slot.frame != null)
                {
                    slot.frame.color = i == bidder ? MonopolyStyle.Tint(MonopolyStyle.PlayerColor(match.players[i].color), 0.75f) : MonopolyStyle.Panel;
                }
            }
            if (humanControls != null)
            {
                humanControls.SetActive(human >= 0);
            }
            if (human >= 0)
            {
                int cash = match.players[human].cash;
                for (int i = 0; i < raiseButtons.Length; i++)
                {
                    int raise = i < Raises.Length ? Raises[i] : 10;
                    int amount = anyBid ? highBid + raise : Math.Max(minimum, raise);
                    raiseButtons[i].interactable = amount <= cash;
                    if (i < raiseLabels.Length && raiseLabels[i] != null)
                    {
                        raiseLabels[i].text = MonopolyStyle.Money(amount);
                    }
                }
            }
            if (!IsOpen)
            {
                Open();
            }
        }
    }
}
