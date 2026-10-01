using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Portfolio.Asteroids.AsteroidsText;

namespace Portfolio.Asteroids
{
    /// <summary>One item of the Supply Room's list: icon, name, price and what the pilot owns of it.</summary>
    public class ShopCard : MonoBehaviour
    {
        private static readonly Color Normal = new Color(0.14f, 0.3f, 0.5f, 1f);
        private static readonly Color Chosen = new Color(0.3f, 0.62f, 0.95f, 1f);

        [SerializeField] internal Button button;
        [SerializeField] internal Image icon;
        [SerializeField] internal TMP_Text title;
        [SerializeField] internal TMP_Text price;
        [SerializeField] internal TMP_Text owned;
        [SerializeField] internal Image highlight;
        [Tooltip("The card's background, brighter while selected.")]
        [SerializeField] internal Image background;

        /// <summary>The item the card shows.</summary>
        public StrikeItem Item { get; private set; }

        /// <summary>The card was clicked: the item (as its value) is selected.</summary>
        public event Action<int> Clicked;


        private void Awake()
        {
            if (button != null)
            {
                button.onClick.AddListener(() => Clicked?.Invoke((int)Item));
            }
        }


        /// <summary>Shows <paramref name="info"/> with the pilot's count and price now, highlighted when <paramref name="selected"/>.</summary>
        public void Show(StrikeItemInfo info, int count, int currentPrice, bool selected, Sprite sprite)
        {
            Item = info.Item;
            if (icon != null)
            {
                icon.sprite = sprite;
                icon.enabled = sprite != null;
            }
            if (title != null)
            {
                title.text = T(info.Title);
            }
            if (price != null)
            {
                // No price now: the energy is full, or the ship carries as many as it can.
                price.text = currentPrice > 0 ? $"${currentPrice:N0}" : T("FULL");
            }
            if (owned != null)
            {
                owned.text = OwnedText(info, count);
                owned.color = count > 0 ? new Color(0.55f, 1f, 0.65f) : new Color(1f, 1f, 1f, 0.45f);
            }
            if (highlight != null)
            {
                highlight.enabled = selected;
            }
            if (background != null)
            {
                background.color = selected ? Chosen : Normal;
            }
        }


        /// <summary>"OWNED", "x3 / 20", or empty for something the pilot does not have.</summary>
        internal static string OwnedText(StrikeItemInfo info, int count)
        {
            if (info.Item == StrikeItem.EnergyModule)
            {
                return string.Empty;
            }
            if (count <= 0)
            {
                return info.Max > 1 ? $"0 / {info.Max}" : string.Empty;
            }
            return info.Max > 1 ? $"{count} / {info.Max}" : T("OWNED");
        }
    }
}
