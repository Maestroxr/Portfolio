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
            SetButtons(true, affordable, true, match.rules.auctions ? L.T("Auction") : L.T("Pass"));
            if (buyLabel != null)
            {
                buyLabel.text = affordable ? L.F("Buy {0}", MonopolyStyle.Money(data.price)) : L.F("Need {0}", MonopolyStyle.Money(data.price));
            }
            int buyer = match.Decider;
            if (!affordable && ownerText != null && buyer >= 0)
            {
                int missing = data.price - match.players[buyer].cash;
                ownerText.text = L.F("For sale: {0}", $"<b>{MonopolyStyle.Money(data.price)}</b>") + $"  •  <color={MonopolyStyle.RedTag}>{L.F("{0} short", MonopolyStyle.Money(missing))}</color>";
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

        private bool mirrored;

        /// <summary>
        /// Right to left, the rent labels sit on the right and the amounts on the left (each text's alignment mirrors by
        /// itself, see <see cref="Gamebox.RightToLeftText"/>; the columns are swapped here).
        /// </summary>
        private void MirrorColumns()
        {
            bool rightToLeft = Gamebox.GameLanguages.IsRightToLeft;
            if (rightToLeft == mirrored)
            {
                return;
            }
            mirrored = rightToLeft;
            foreach (TMP_Text column in new[] { labels, values })
            {
                if (column == null)
                {
                    continue;
                }
                RectTransform rect = column.rectTransform;
                Vector2 min = rect.anchorMin;
                Vector2 max = rect.anchorMax;
                rect.anchorMin = new Vector2(1f - max.x, min.y);
                rect.anchorMax = new Vector2(1f - min.x, max.y);
                rect.pivot = new Vector2(1f - rect.pivot.x, rect.pivot.y);
                rect.anchoredPosition = new Vector2(-rect.anchoredPosition.x, rect.anchoredPosition.y);
            }
        }

        private void Fill(MonopolyMatch match, int space)
        {
            MirrorColumns();
            Space = space;
            SpaceData data = match.Space(space);
            DeedState deed = match.Deed(space);
            bool street = data.kind == SpaceKind.Street;
            Color band = street ? MonopolyStyle.GroupColor(data.group) : MonopolyStyle.Plate;
            if (header != null)
            {
                header.color = band;
            }
            Color headerText = street ? MonopolyStyle.GroupTextColor(data.group) : Color.white;
            if (caption != null)
            {
                caption.text = data.IsProperty ? L.T("TITLE DEED") : data.kind == SpaceKind.Tax ? L.T("TAX") : L.T("SPACE");
                caption.color = MonopolyStyle.WithAlpha(headerText, 0.8f);
            }
            if (nameText != null)
            {
                nameText.text = MonopolyStyle.SpaceName(data).ToUpperInvariant();
                nameText.color = headerText;
            }
            if (cityText != null)
            {
                cityText.text = data.kind == SpaceKind.Railroad ? L.F("{0} station", MonopolyStyle.CityName(data)) : MonopolyStyle.CityName(data);
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
                    Row(left, right, L.T("Rent"), data.rent[0], deed.Owned && deed.houses == 0 && !set);
                    Row(left, right, L.T("Rent with the color set"), data.rent[0] * 2, deed.Owned && deed.houses == 0 && set);
                    for (int h = 1; h <= 4; h++)
                    {
                        Row(left, right, h == 1 ? L.T("With 1 house") : L.F("With {0} houses", h), data.rent[h], deed.houses == h);
                    }
                    Row(left, right, L.T("With a hotel"), data.rent[5], deed.houses == MonopolyMatch.Hotel);
                    if (footer != null)
                    {
                        footer.text = L.F("Houses {0} each  •  Hotels {0} plus {1} houses", MonopolyStyle.Money(data.houseCost), match.HousesForHotel) + "\n" +
                            L.F("Mortgage value {0}", MonopolyStyle.Money(data.MortgageValue));
                    }
                    break;
                }
                case SpaceKind.Railroad:
                {
                    int owned = deed.Owned ? match.CountOwned(deed.owner, ColorGroup.Railroad) : 0;
                    for (int n = 1; n <= 4; n++)
                    {
                        Row(left, right, n == 1 ? L.T("Rent") : L.F("With {0} stations", n), data.rent[n - 1], owned == n && !deed.mortgaged);
                    }
                    if (footer != null)
                    {
                        footer.text = L.F("Mortgage value {0}", MonopolyStyle.Money(data.MortgageValue));
                    }
                    break;
                }
                case SpaceKind.Utility:
                {
                    int owned = deed.Owned ? match.CountOwned(deed.owner, ColorGroup.Utility) : 0;
                    Row(left, right, L.T("One utility owned"), L.F("{0} × dice", data.rent[0]), owned == 1 && !deed.mortgaged);
                    Row(left, right, L.T("Both utilities owned"), L.F("{0} × dice", data.rent[1]), owned == 2 && !deed.mortgaged);
                    if (footer != null)
                    {
                        footer.text = L.F("Mortgage value {0}", MonopolyStyle.Money(data.MortgageValue));
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
                    ownerText.text = L.F("For sale: {0}", $"<b>{MonopolyStyle.Money(data.price)}</b>");
                }
                else
                {
                    PlayerState owner = match.players[deed.owner];
                    string rent = deed.mortgaged ? L.T("mortgaged, no rent") : data.kind == SpaceKind.Utility ? L.T("rent by the dice") : L.F("rent now {0}", MonopolyStyle.Money(now));
                    string by = MonopolyStyle.Say(owner, "Owned by {0}", $"Owned by {MonopolyStyle.NamedObject(owner)}", MonopolyStyle.Named(owner));
                    ownerText.text = $"{by}  •  {rent}";
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
                string tag = MonopolyStyle.RedTag;
                left.Append("<color=").Append(tag).Append("><b>").Append(label).Append("</b></color>\n");
                right.Append("<color=").Append(tag).Append("><b>").Append(value).Append("</b></color>\n");
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
                    return L.F("Collect {0} every time you pass GO.", MonopolyStyle.Money(match.rules.salary)) +
                        (match.rules.doubleSalaryOnGo ? "\n" + L.F("Land right on it for {0}!", MonopolyStyle.Money(match.rules.salary * 2)) : "");
                case SpaceKind.Chance:
                    return L.T("Draw a Chance card: trips, windfalls and surprises.");
                case SpaceKind.CommunityChest:
                    return L.T("Draw a Community Chest card: mostly money matters.");
                case SpaceKind.Tax:
                    return match.rules.freeParkingJackpot ? L.F("Pay {0} into the Free Parking pot.", MonopolyStyle.Money(data.tax)) : L.F("Pay {0} to the bank.", MonopolyStyle.Money(data.tax));
                case SpaceKind.Jail:
                    return L.T("Just visiting, unless you were sent here.") + "\n" + L.F("Get out with doubles, a card, or a {0} fine.", MonopolyStyle.Money(match.rules.jailFine));
                case SpaceKind.FreeParking:
                    return match.rules.freeParkingJackpot ? L.F("Jackpot! Land here to win the pot: {0}.", MonopolyStyle.Money(match.pot)) : L.T("A free rest. Nothing happens here.");
                case SpaceKind.GoToJail:
                    return L.T("Go directly to jail. Do not pass GO, do not collect the salary.");
                default:
                    return "";
            }
        }
    }
}
