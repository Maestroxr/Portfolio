using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Monopoly
{
    /// <summary>A row of the property manager: a property with its buildings and the build, sell and mortgage buttons.</summary>
    public class PropertyRow : MonoBehaviour
    {
        [SerializeField] private Image colorBar;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text stateText;
        [SerializeField] private Button buildButton;
        [SerializeField] private TMP_Text buildLabel;
        [Tooltip("The build button's icon: a house, or a hotel when the next building is one.")]
        [SerializeField] private TMP_Text buildIcon;
        [SerializeField] private Button sellButton;
        [SerializeField] private TMP_Text sellLabel;
        [SerializeField] private Button mortgageButton;
        [SerializeField] private TMP_Text mortgageLabel;
        [SerializeField] private Image background;

        private Action onBuild;
        private Action onSell;
        private Action onMortgage;

        public int Space { get; private set; } = -1;

        private void Awake()
        {
            buildButton.onClick.AddListener(() => onBuild?.Invoke());
            sellButton.onClick.AddListener(() => onSell?.Invoke());
            mortgageButton.onClick.AddListener(() => onMortgage?.Invoke());
        }

        public void Bind(MonopolyMatch match, int seat, int space, Action build, Action sell, Action mortgage)
        {
            Space = space;
            onBuild = build;
            onSell = sell;
            onMortgage = mortgage;
            SpaceData data = match.Space(space);
            DeedState deed = match.Deed(space);
            colorBar.color = MonopolyStyle.GroupColor(data.group);
            nameText.text = data.name;
            string buildings = deed.houses == MonopolyMatch.Hotel ? $"{Icons.Hotel} Hotel" : deed.houses > 0 ? Repeat(Icons.House, deed.houses) : "";
            string rent = deed.mortgaged ? "<color=#E4002B>Mortgaged</color>" : $"Rent {MonopolyStyle.Money(match.Rent(space, 7))}" + (data.kind == SpaceKind.Utility ? " (at a 7)" : "");
            stateText.text = string.IsNullOrEmpty(buildings) ? rent : $"{buildings}   {rent}";
            if (background != null)
            {
                background.color = deed.mortgaged ? MonopolyStyle.Tint(MonopolyStyle.Red, 0.9f) : Color.white;
            }

            bool street = data.kind == SpaceKind.Street;
            bool canBuild = match.CanBuild(seat, space, out _);
            bool canSell = match.CanSellBuilding(seat, space, out _);
            buildButton.gameObject.SetActive(street);
            sellButton.gameObject.SetActive(street);
            buildButton.interactable = canBuild;
            sellButton.interactable = canSell;
            if (street)
            {
                bool hotelNext = deed.houses >= match.HousesForHotel && deed.houses < MonopolyMatch.Hotel;
                buildLabel.text = MonopolyStyle.Money(data.houseCost);
                if (buildIcon != null)
                {
                    buildIcon.text = hotelNext ? Icons.Hotel : Icons.House;
                }
                sellLabel.text = $"Sell +{MonopolyStyle.Money(data.houseCost / 2)}";
            }
            if (deed.mortgaged)
            {
                bool can = match.CanUnmortgage(seat, space, out _);
                mortgageButton.interactable = can;
                mortgageLabel.text = $"Lift {MonopolyStyle.Money(data.UnmortgageCost)}";
            }
            else
            {
                bool can = match.CanMortgage(seat, space, out _);
                mortgageButton.interactable = can;
                mortgageLabel.text = $"Mortgage +{MonopolyStyle.Money(data.MortgageValue)}";
            }
        }

        private static string Repeat(string text, int count)
        {
            var builder = new System.Text.StringBuilder();
            for (int i = 0; i < count; i++)
            {
                builder.Append(text);
            }
            return builder.ToString();
        }
    }
}
