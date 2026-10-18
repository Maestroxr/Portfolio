using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The Supply Room's two pages: WEAPONS (the primary and special weapons) and GEAR (supplies and equipment), picked by
    /// the tabs next to BUY and SELL, each listed by price in a grid that scrolls when a page holds more cards than it
    /// shows. Showing the room for an item opens that item's page and scrolls its card into view.
    /// </summary>
    public partial class StrikeUI
    {
        /// <summary>The pages of the Supply Room's list.</summary>
        public enum SupplyCategory : byte
        {
            /// <summary>The primary and special weapons.</summary>
            Weapons = 0,
            /// <summary>The supplies (megabombs, energy, shields, repair kits) and the equipment.</summary>
            Gear = 1
        }

        [Header("Supply Room pages")]
        [SerializeField] internal Button weaponsTab;
        [SerializeField] internal Button gearTab;
        [Tooltip("The list of cards; it scrolls when a page holds more than it shows.")]
        [SerializeField] internal ScrollRect shopScroll;

        private SupplyCategory category;
        private bool scrollToSelected;

        /// <summary>The page the Supply Room shows.</summary>
        public SupplyCategory Category => category;


        /// <summary>The page of the Supply Room that lists <paramref name="item"/>.</summary>
        public static SupplyCategory CategoryOf(StrikeItem item)
        {
            return StrikeWeaponRules.IsWeapon(item) ? SupplyCategory.Weapons : SupplyCategory.Gear;
        }


        /// <summary>Shows the page <paramref name="shown"/> of the list, scrolled to its top.</summary>
        public void SetCategory(SupplyCategory shown)
        {
            if (category != shown)
            {
                Sounds?.Click();
            }
            category = shown;
            scrollToSelected = true;
            RefreshSupply();
        }


        /// <summary>Keeps the items of <paramref name="offers"/> that the page <paramref name="shown"/> lists.</summary>
        internal static void KeepCategory(List<StrikeItemInfo> offers, SupplyCategory shown)
        {
            offers.RemoveAll(info => CategoryOf(info.Item) != shown);
        }


        private void ListenCategories()
        {
            Listen(weaponsTab, () => SetCategory(SupplyCategory.Weapons));
            Listen(gearTab, () => SetCategory(SupplyCategory.Gear));
        }


        /// <summary>The room was shown for <paramref name="item"/>: its page, with its card in view.</summary>
        private void FollowCategory(StrikeItem item)
        {
            category = CategoryOf(item);
            scrollToSelected = true;
        }


        private void RefreshCategories()
        {
            SetTab(weaponsTab, category == SupplyCategory.Weapons);
            SetTab(gearTab, category == SupplyCategory.Gear);
            if (scrollToSelected)
            {
                scrollToSelected = false;
                ScrollTo(offers.FindIndex(info => info.Item == selectedItem));
            }
        }


        /// <summary>Scrolls the list so the card at <paramref name="index"/> shows in the middle (the top for the first row, or when all fit).</summary>
        private void ScrollTo(int index)
        {
            if (shopScroll == null || shopScroll.content == null || shopScroll.viewport == null)
            {
                return;
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(shopScroll.content);
            float content = shopScroll.content.rect.height;
            float view = shopScroll.viewport.rect.height;
            var grid = shopScroll.content.GetComponent<GridLayoutGroup>();
            if (content <= view + 0.5f || index <= 0 || grid == null)
            {
                shopScroll.verticalNormalizedPosition = 1f;
                return;
            }
            int columns = Mathf.Max(1, grid.constraintCount);
            float row = grid.cellSize.y + grid.spacing.y;
            float top = index / columns * row;
            float wanted = Mathf.Clamp(top - (view - row) * 0.5f, 0f, content - view);
            shopScroll.verticalNormalizedPosition = 1f - wanted / (content - view);
        }
    }
}
