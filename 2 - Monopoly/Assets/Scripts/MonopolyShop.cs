using Gamebox;
using Gamebox.Economy;
using UnityEngine;

namespace Portfolio.Monopoly
{
    /// <summary>
    /// What Monopoly sells in the shop of every game (<see cref="GameShopDefinition"/>): two perks kept in the cloud.
    /// Lucky Start, for money, pays every person at the table $500 from the bank on top of the starting cash of a game
    /// played at this device (<see cref="StartingCashBonus"/>, given in <see cref="MonopolyGameManager.StartGame"/>; a
    /// match of a room is simulated alike by every client and gets none). Golden Dice, for Tokens, gilds the two white
    /// dice on the board (<see cref="Dice"/>). Both are read where they apply with <see cref="CloudEconomy.Owns"/>.
    /// </summary>
    public sealed class MonopolyShop : GameShopDefinition
    {
        public const string LuckyStartId = "MONOPOLY_LUCKY_START";
        public const int LuckyStartCents = 100;
        public const string LuckyStartProductId = "com.skinnerboxes.monopoly.luckystart";
        /// <summary>What Lucky Start adds to the starting cash of every person at the table.</summary>
        public const int LuckyStartCash = 500;

        public const string GoldenDiceId = "MONOPOLY_GOLDEN_DICE";
        public const int GoldenDiceTokens = 150;

        public override GameType Game => GameType.Monopoly;

        public override void DefineCloud(CloudCatalog catalog)
        {
            catalog.AddForMoney(Game, LuckyStartId, "Lucky Start", "+$500 cash at the start of every game", LuckyStartCents, LuckyStartProductId);
            catalog.AddForTokens(Game, GoldenDiceId, "Golden Dice", "The dice on the board are cast in gold.", GoldenDiceTokens);
        }

        /// <summary>
        /// The cash every person at the table gets on top of the starting cash: Lucky Start's bonus in a game played at
        /// this device (<paramref name="local"/>) while the account owns it (<paramref name="owned"/>), nothing otherwise.
        /// An online match gets none: every client simulates the same match from the rules of the room, and the bonus of
        /// one account would set its match apart from the others'.
        /// </summary>
        public static int StartingCashBonus(bool local, bool owned)
        {
            return local && owned ? LuckyStartCash : 0;
        }

        /// <summary>The banknote of the theme for Lucky Start; the game's icon for the rest.</summary>
        public override Sprite IconOf(CloudItem item)
        {
            MonopolyTheme theme = MonopolyStyle.Theme;
            return item != null && item.Id == LuckyStartId && theme != null ? theme.ui.bill : null;
        }
    }
}
