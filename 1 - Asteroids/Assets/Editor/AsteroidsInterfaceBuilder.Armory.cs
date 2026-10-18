using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Asteroids.EditorTools
{
    /// <summary>
    /// The armory's part of the strike interface: the Supply Room's list, which grew from 17 items to 28. It has two pages,
    /// WEAPONS and GEAR, picked by the tabs at the right of BUY and SELL, and its grid of cards (three columns, six rows in
    /// view) sits in a scroll view that the mouse wheel and a finger drag, with a thin bar that shows only when a page holds
    /// more cards than it shows.
    /// </summary>
    internal static partial class AsteroidsInterfaceBuilder
    {
        private static readonly Vector2 SupplyCard = new Vector2(388f, 92f);
        private static readonly Vector2 SupplyGap = new Vector2(18f, 12f);


        /// <summary>
        /// Builds the Supply Room's pages and its scrolling list under <paramref name="root"/> and wires them to
        /// <paramref name="strike"/>; returns the grid the cards go into.
        /// </summary>
        private static RectTransform SupplyList(Transform root, StrikeUI strike)
        {
            strike.weaponsTab = Button(root, "WeaponsTab", "WEAPONS", StrikeIconOr(StrikeItem.AirMissiles.ToString(), "Missile"), StrikeAccent, new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(776f, -194f), new Vector2(236f, 52f), 24f, out _);
            strike.gearTab = Button(root, "GearTab", "GEAR", StrikeIconOr(StrikeItem.PhaseShield.ToString(), "Shield"), TabIdle, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(1024f, -194f), new Vector2(236f, 52f), 24f, out _);

            // The view: masks the cards and catches drags in the gaps between them.
            RectTransform view = Rect(root, "Items", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60f, -270f), new Vector2(1200f, 620f));
            view.gameObject.AddComponent<RectMask2D>();
            var catcher = view.gameObject.AddComponent<Image>();
            catcher.color = new Color(0f, 0f, 0f, 0f);

            RectTransform content = Rect(view, "Cards", new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(1200f, 0f));
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = SupplyCard;
            grid.spacing = SupplyGap;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = view;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.elasticity = 0.08f;
            scroll.inertia = true;
            scroll.decelerationRate = 0.12f;
            scroll.scrollSensitivity = SupplyCard.y + SupplyGap.y;

            // The bar between the list and the item's details, only while the page scrolls.
            Image track = Image(root, "ItemsBar", AsteroidsArtBuilder.Interface("Bar"), new Color(1f, 1f, 1f, 0.1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(1270f, -270f), new Vector2(12f, 620f), true);
            RectTransform slide = Rect(track.rectTransform, "Slide", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            StretchFull(slide);
            Image handle = Image(slide, "Handle", AsteroidsArtBuilder.Interface("Bar"), StrikeAccent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                Vector2.zero, true);
            StretchFull(handle.rectTransform);
            var bar = track.gameObject.AddComponent<Scrollbar>();
            bar.handleRect = handle.rectTransform;
            bar.targetGraphic = handle;
            bar.direction = Scrollbar.Direction.BottomToTop;
            bar.colors = ButtonColors();
            scroll.verticalScrollbar = bar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            strike.shopScroll = scroll;
            return content;
        }
    }
}
