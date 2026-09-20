using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Monopoly
{
    /// <summary>
    /// A title deed: the property's colour band and name, its rent table (the row that applies now stands out), building
    /// and mortgage prices, and who owns it. Shown when a player lands on a property for sale (with Buy and Auction) and
    /// when a space of the board is tapped.
    /// </summary>
    public class DeedCardUI : Popup
    {
        [SerializeField] private Image header;
        [SerializeField] private TMP_Text caption;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text cityText;
        [SerializeField] private TMP_Text iconText;
        [SerializeField] private TMP_Text labels;
        [SerializeField] private TMP_Text values;
        [SerializeField] private TMP_Text footer;
        [SerializeField] private TMP_Text ownerText;
        [SerializeField] private GameObject mortgagedStamp;
        [SerializeField] private Button buyButton;
        [SerializeField] private TMP_Text buyLabel;
        [SerializeField] private Button auctionButton;
        [SerializeField] private TMP_Text auctionLabel;
        [SerializeField] private Button closeButton;
        [Tooltip("Opens the property manager, to raise the price by mortgaging or selling buildings.")]
        [SerializeField] private Button manageButton;
        [Tooltip("How much shorter the card is without the Buy and Auction buttons (its top edge stays put).")]
        [SerializeField] private float buttonRowHeight = 92f;

        private Action onBuy;
        private Action onDecline;
        private Action onManage;
        private float fullHeight;
        private float fullY;

        public int Space { get; private set; } = -1;

        /// <summary>Whether the card offers the property for sale (a decision is pending).</summary>
        public bool IsOffer { get; private set; }

        private void Awake()
        {
            if (buyButton != null)
            {
                buyButton.onClick.AddListener(() => onBuy?.Invoke());
            }
            if (auctionButton != null)
            {
                auctionButton.onClick.AddListener(() => onDecline?.Invoke());
            }
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }
            if (manageButton != null)
            {
                manageButton.onClick.AddListener(() => onManage?.Invoke());
            }
        }

        private void Update()
        {
            // Keys for the offer: Space or B buys, A auctions (or passes), M manages. Only while no popup covers the card.
            if (!IsOffer || !IsOpen || Time.timeScale <= 0f || Covered())
            {
                return;
            }
            if ((Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.B)) && buyButton != null && buyButton.interactable)
            {
                onBuy?.Invoke();
            }
            else if (Input.GetKeyDown(KeyCode.A) && auctionButton != null && auctionButton.gameObject.activeSelf)
            {
                onDecline?.Invoke();
            }
            else if (Input.GetKeyDown(KeyCode.M) && manageButton != null && manageButton.gameObject.activeSelf)
            {
                onManage?.Invoke();
            }
        }

        /// <summary>Whether another popup is open on top of this one.</summary>
        private bool Covered()
        {
            Transform parent = transform.parent;
            for (int i = transform.GetSiblingIndex() + 1; parent != null && i < parent.childCount; i++)
            {
                if (parent.GetChild(i).gameObject.activeSelf)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Shows a space for information only.</summary>
        public void ShowInfo(MonopolyMatch match, int space)
        {
            IsOffer = false;
            onManage = null;
            Fill(match, space);
            SetButtons(false, false, false, null);
            if (closeButton != null)
            {
                closeButton.gameObject.SetActive(true);
            }
            Open();
        }

        /// <summary>
        /// Offers the property the current player landed on: buy it, or auction it (leave it when auctions are off), with
        /// <paramref name="manage"/> (when not null) to raise the money first. Showing the same offer again updates the
        /// card in place.
        /// </summary>
        public void ShowOffer(MonopolyMatch match, int space, Action buy, Action decline, Action manage = null)
        {
            bool reopen = !IsOpen || !IsOffer || Space != space;
            IsOffer = true;
            onBuy = buy;
            onDecline = decline;
            onManage = manage;
            Fill(match, space);
            SpaceData data = match.Space(space);
            bool affordable = match.CanAffordPurchase;
            SetButtons(true, affordable, true, match.rules.auctions ? "Auction" : "Pass");
            if (buyLabel != null)
            {
                buyLabel.text = affordable ? $"Buy {MonopolyStyle.Money(data.price)}" : $"Need {MonopolyStyle.Money(data.price)}";
            }
            int buyer = match.Decider;
            if (!affordable && ownerText != null && buyer >= 0)
            {
                int missing = data.price - match.players[buyer].cash;
                ownerText.text = $"For sale: <b>{MonopolyStyle.Money(data.price)}</b>  •  <color=#E4002B>{MonopolyStyle.Money(missing)} short</color>";
            }
            if (closeButton != null)
            {
                closeButton.gameObject.SetActive(false);
            }
            if (reopen)
            {
                Open();
            }
        }

        public override void Close()
        {
            IsOffer = false;
            base.Close();
        }

        private void SetButtons(bool offer, bool canBuy, bool canDecline, string declineLabel)
        {
            if (card != null)
            {
                if (fullHeight <= 0f)
                {
                    fullHeight = card.sizeDelta.y;
                    fullY = card.anchoredPosition.y;
                }
                float trim = offer ? 0f : buttonRowHeight;
                card.sizeDelta = new Vector2(card.sizeDelta.x, fullHeight - trim);
                card.anchoredPosition = new Vector2(card.anchoredPosition.x, fullY + trim * 0.5f);
            }
            if (buyButton != null)
            {
                buyButton.gameObject.SetActive(offer);
                buyButton.interactable = canBuy;
            }
            if (auctionButton != null)
            {
                auctionButton.gameObject.SetActive(offer && canDecline);
            }
            if (manageButton != null)
            {
                manageButton.gameObject.SetActive(offer && onManage != null);
            }
            if (auctionLabel != null && declineLabel != null)
            {
                auctionLabel.text = declineLabel;
            }
        }

        private void Fill(MonopolyMatch match, int space)
        {
            Space = space;
            SpaceData data = match.Space(space);
            DeedState deed = match.Deed(space);
            bool street = data.kind == SpaceKind.Street;
            Color band = street ? MonopolyStyle.GroupColor(data.group) : MonopolyStyle.Ink;
            if (header != null)
            {
                header.color = band;
            }
            Color headerText = street ? MonopolyStyle.GroupTextColor(data.group) : Color.white;
            if (caption != null)
            {
                caption.text = data.IsProperty ? "TITLE DEED" : data.kind == SpaceKind.Tax ? "TAX" : "SPACE";
                caption.color = MonopolyStyle.WithAlpha(headerText, 0.8f);
            }
            if (nameText != null)
            {
                nameText.text = data.name.ToUpperInvariant();
                nameText.color = headerText;
            }
            if (cityText != null)
            {
                cityText.text = data.kind == SpaceKind.Railroad ? $"{data.city} station" : data.city;
                cityText.color = MonopolyStyle.WithAlpha(headerText, 0.85f);
            }
            if (iconText != null)
            {
                iconText.gameObject.SetActive(!street);
                iconText.text = Icons.ForSpace(data);
            }

            var left = new StringBuilder();
            var right = new StringBuilder();
            int now = data.IsProperty && deed.Owned && !deed.mortgaged ? match.Rent(space, 7) : -1;
            switch (data.kind)
            {
                case SpaceKind.Street:
                {
                    bool set = deed.Owned && match.OwnsGroup(deed.owner, data.group);
                    Row(left, right, "Rent", data.rent[0], deed.Owned && deed.houses == 0 && !set);
                    Row(left, right, "Rent with the color set", data.rent[0] * 2, deed.Owned && deed.houses == 0 && set);
                    for (int h = 1; h <= 4; h++)
                    {
                        Row(left, right, h == 1 ? "With 1 house" : $"With {h} houses", data.rent[h], deed.houses == h);
                    }
                    Row(left, right, "With a hotel", data.rent[5], deed.houses == MonopolyMatch.Hotel);
                    if (footer != null)
                    {
                        footer.text = $"Houses {MonopolyStyle.Money(data.houseCost)} each  •  Hotels {MonopolyStyle.Money(data.houseCost)} plus {match.HousesForHotel} houses\n" +
                            $"Mortgage value {MonopolyStyle.Money(data.MortgageValue)}";
                    }
                    break;
                }
                case SpaceKind.Railroad:
                {
                    int owned = deed.Owned ? match.CountOwned(deed.owner, ColorGroup.Railroad) : 0;
                    for (int n = 1; n <= 4; n++)
                    {
                        Row(left, right, n == 1 ? "Rent" : $"With {n} stations", data.rent[n - 1], owned == n && !deed.mortgaged);
                    }
                    if (footer != null)
                    {
                        footer.text = $"Mortgage value {MonopolyStyle.Money(data.MortgageValue)}";
                    }
                    break;
                }
                case SpaceKind.Utility:
                {
                    int owned = deed.Owned ? match.CountOwned(deed.owner, ColorGroup.Utility) : 0;
                    Row(left, right, "One utility owned", $"{data.rent[0]} × dice", owned == 1 && !deed.mortgaged);
                    Row(left, right, "Both utilities owned", $"{data.rent[1]} × dice", owned == 2 && !deed.mortgaged);
                    if (footer != null)
                    {
                        footer.text = $"Mortgage value {MonopolyStyle.Money(data.MortgageValue)}";
                    }
                    break;
                }
                default:
                    left.Append(Describe(match, data));
                    if (footer != null)
                    {
                        footer.text = "";
                    }
                    break;
            }
            if (labels != null)
            {
                labels.text = left.ToString();
            }
            if (values != null)
            {
                values.text = right.ToString();
            }
            if (mortgagedStamp != null)
            {
                mortgagedStamp.SetActive(deed.mortgaged);
            }
            if (ownerText != null)
            {
                if (!data.IsProperty)
                {
                    ownerText.text = "";
                }
                else if (!deed.Owned)
                {
                    ownerText.text = $"For sale: <b>{MonopolyStyle.Money(data.price)}</b>";
                }
                else
                {
                    PlayerState owner = match.players[deed.owner];
                    string rent = deed.mortgaged ? "mortgaged, no rent" : data.kind == SpaceKind.Utility ? "rent by the dice" : $"rent now {MonopolyStyle.Money(now)}";
                    ownerText.text = $"Owned by {MonopolyStyle.NamedObject(owner)}  •  {rent}";
                }
            }
        }

        private static void Row(StringBuilder left, StringBuilder right, string label, int amount, bool current)
        {
            Row(left, right, label, MonopolyStyle.Money(amount), current);
        }

        private static void Row(StringBuilder left, StringBuilder right, string label, string value, bool current)
        {
            if (current)
            {
                left.Append("<color=#E4002B><b>").Append(label).Append("</b></color>\n");
                right.Append("<color=#E4002B><b>").Append(value).Append("</b></color>\n");
            }
            else
            {
                left.Append(label).Append('\n');
                right.Append(value).Append('\n');
            }
        }

        private static string Describe(MonopolyMatch match, SpaceData data)
        {
            switch (data.kind)
            {
                case SpaceKind.Go:
                    return $"Collect {MonopolyStyle.Money(match.rules.salary)} every time you pass GO." +
                        (match.rules.doubleSalaryOnGo ? $"\nLand right on it for {MonopolyStyle.Money(match.rules.salary * 2)}!" : "");
                case SpaceKind.Chance:
                    return "Draw a Chance card: trips, windfalls and surprises.";
                case SpaceKind.CommunityChest:
                    return "Draw a Community Chest card: mostly money matters.";
                case SpaceKind.Tax:
                    return $"Pay {MonopolyStyle.Money(data.tax)} " + (match.rules.freeParkingJackpot ? "into the Free Parking pot." : "to the bank.");
                case SpaceKind.Jail:
                    return $"Just visiting, unless you were sent here.\nGet out with doubles, a card, or a {MonopolyStyle.Money(match.rules.jailFine)} fine.";
                case SpaceKind.FreeParking:
                    return match.rules.freeParkingJackpot ? $"Jackpot! Land here to win the pot: {MonopolyStyle.Money(match.pot)}." : "A free rest. Nothing happens here.";
                case SpaceKind.GoToJail:
                    return "Go directly to jail. Do not pass GO, do not collect the salary.";
                default:
                    return "";
            }
        }
    }
}
