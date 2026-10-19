using Gamebox;
using Gamebox.Economy;
using NUnit.Framework;

namespace Portfolio.MemoryCards.Tests
{
    /// <summary>
    /// The shop of Memory Cards: the two items its definition puts in the cloud catalog, with the ids and prices of the
    /// design, and the numbers its perks change only when the account owns them.
    /// </summary>
    public class ShopTest
    {
        [Test]
        public void TheDefinitionSellsExtraTimeForMoneyAndLongPreviewForTokens()
        {
            var catalog = new CloudCatalog();
            var shop = new MemoryCardsShop();
            Assert.AreEqual(GameType.MemoryCards, shop.Game);
            shop.DefineCloud(catalog);
            Assert.AreEqual(2, catalog.Count, "two items");

            CloudItem extraTime = catalog["MEMORYCARDS_EXTRA_TIME"];
            Assert.IsNotNull(extraTime, "Extra Time is in the catalog");
            Assert.AreEqual(GameType.MemoryCards, extraTime.Game);
            Assert.AreEqual(ItemKind.Owned, extraTime.Kind, "a perk");
            Assert.AreEqual(100, extraTime.Cents, "$1.00");
            Assert.IsTrue(extraTime.ForMoney);
            Assert.IsFalse(extraTime.ForTokens);
            Assert.AreEqual("com.skinnerboxes.memorycards.extratime", extraTime.AppleProductId);
            Assert.AreEqual("com.skinnerboxes.memorycards.extratime", extraTime.GooglePlayProductId);
            Assert.AreEqual("MEMORYCARDS_EXTRA_TIME_FOR_MONEY", extraTime.MoneyPurchaseId);

            CloudItem longPreview = catalog["MEMORYCARDS_LONG_PREVIEW"];
            Assert.IsNotNull(longPreview, "Long Preview is in the catalog");
            Assert.AreEqual(GameType.MemoryCards, longPreview.Game);
            Assert.AreEqual(ItemKind.Owned, longPreview.Kind, "a perk");
            Assert.AreEqual(150, longPreview.Tokens);
            Assert.IsTrue(longPreview.ForTokens);
            Assert.IsFalse(longPreview.ForMoney);
            Assert.AreEqual("MEMORYCARDS_LONG_PREVIEW_FOR_TOKENS", longPreview.TokensPurchaseId);

            Assert.AreEqual(1, catalog.ForMoney().Count);
            Assert.AreEqual(1, catalog.ForTokens().Count);
        }

        [Test]
        public void ThePerksChangeTheNumbersOnlyWhenOwned()
        {
            Assert.AreEqual(15f, MemoryCardsShop.TimeBonus(true), "Extra Time adds a quarter minute");
            Assert.AreEqual(0f, MemoryCardsShop.TimeBonus(false), "nothing without it");
            Assert.AreEqual(2f, MemoryCardsShop.PreviewFactor(true), "Long Preview doubles the memorize time");
            Assert.AreEqual(1f, MemoryCardsShop.PreviewFactor(false), "the level's own time without it");
        }
    }
}
