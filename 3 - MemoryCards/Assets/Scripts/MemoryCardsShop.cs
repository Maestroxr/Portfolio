using Gamebox;
using Gamebox.Economy;
using UnityEngine;

namespace Portfolio.MemoryCards
{
    /// <summary>
    /// What Memory Cards sells through the cloud shop (<see cref="GameShopDefinition"/>): two perks the account keeps.
    /// Extra Time, sold for money, puts more seconds on the clock of every timed level; Long Preview, sold for Tokens,
    /// keeps the cards face up longer in the memorize phase. Neither needs delivering: <see cref="MemoryCardsGameManager"/>
    /// reads them where they apply through <see cref="CloudEconomy.Owns"/>, and the numbers they change are here
    /// (<see cref="TimeBonus"/>, <see cref="PreviewFactor"/>). An online board is judged by the server, which keeps its
    /// own clock and preview, so the perks count in the games played on this device only.
    /// </summary>
    public sealed class MemoryCardsShop : GameShopDefinition
    {
        public const string ExtraTimeId = "MEMORYCARDS_EXTRA_TIME";
        public const int ExtraTimeCents = 100;
        public const string ExtraTimeProductId = "com.skinnerboxes.memorycards.extratime";
        public const string LongPreviewId = "MEMORYCARDS_LONG_PREVIEW";
        public const int LongPreviewTokens = 150;

        /// <summary>The seconds Extra Time adds to the clock of a timed level.</summary>
        public const float ExtraSeconds = 15f;

        /// <summary>How many times longer the cards show at the start with Long Preview.</summary>
        public const float LongPreviewTimes = 2f;

        public override GameType Game => GameType.MemoryCards;

        public override void DefineCloud(CloudCatalog catalog)
        {
            catalog.AddForMoney(Game, ExtraTimeId, "Extra Time", "+15 seconds on every timed level", ExtraTimeCents, ExtraTimeProductId);
            catalog.AddForTokens(Game, LongPreviewId, "Long Preview", "The cards show twice as long at the start", LongPreviewTokens);
        }

        /// <summary>The clock card's face for Extra Time and the first world's card back for Long Preview, from the theme shown.</summary>
        public override Sprite IconOf(CloudItem item)
        {
            MemoryCardsTheme look = GameThemes.Active<MemoryCardsTheme>(GameType.MemoryCards);
            if (item == null || look == null)
            {
                return null;
            }
            switch (item.Id)
            {
                case ExtraTimeId:
                    return look.Special?.clock != null ? look.Special.clock : look.Icons?.stopwatch;
                case LongPreviewId:
                    CardWorld first = look.Worlds != null && look.Worlds.Count > 0 ? look.Worlds[0] : null;
                    return first != null && first.cardBack != null ? first.cardBack : look.Card?.defaultBack;
                default:
                    return null;
            }
        }

        /// <summary>The seconds a timed level's clock gets on top of its own: 15 with Extra Time, none without.</summary>
        public static float TimeBonus(bool owned)
        {
            return owned ? ExtraSeconds : 0f;
        }

        /// <summary>The seconds this account's Extra Time puts on a timed level's clock.</summary>
        public static float TimeBonus()
        {
            return TimeBonus(CloudEconomy.Owns(ExtraTimeId));
        }

        /// <summary>What the memorize time is multiplied by: 2 with Long Preview, 1 without.</summary>
        public static float PreviewFactor(bool owned)
        {
            return owned ? LongPreviewTimes : 1f;
        }

        /// <summary>What this account's Long Preview multiplies the memorize time by.</summary>
        public static float PreviewFactor()
        {
            return PreviewFactor(CloudEconomy.Owns(LongPreviewId));
        }
    }
}
