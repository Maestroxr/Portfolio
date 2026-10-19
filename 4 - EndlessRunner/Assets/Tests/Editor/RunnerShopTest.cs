using Gamebox;
using Gamebox.Economy;
using NUnit.Framework;

namespace Portfolio.EndlessRunner.Tests
{
    /// <summary>
    /// The Endless Runner's side of the cross-game shop: the two perks it sells through the cloud, the registry finding
    /// its definition, and what the Coin Magnet does to the pull of the coins.
    /// </summary>
    public class RunnerShopTest
    {
        [Test]
        public void TheShopSellsAHeadStartForMoneyAndACoinMagnetForTokens()
        {
            var catalog = new CloudCatalog();
            new RunnerShop().DefineCloud(catalog);
            Assert.AreEqual(2, catalog.Count);

            CloudItem headStart = catalog[RunnerShop.HeadStartId];
            Assert.IsNotNull(headStart);
            Assert.AreEqual("RUNNER_HEAD_START", headStart.Id);
            Assert.AreEqual(GameType.EndlessRunner, headStart.Game);
            Assert.AreEqual(ItemKind.Owned, headStart.Kind, "a perk, bought once");
            Assert.AreEqual(100, headStart.Cents);
            Assert.AreEqual("$1.00", headStart.MoneyPrice);
            Assert.IsTrue(headStart.ForMoney);
            Assert.IsFalse(headStart.ForTokens);
            Assert.AreEqual("com.skinnerboxes.runner.headstart", headStart.AppleProductId);
            Assert.AreEqual("com.skinnerboxes.runner.headstart", headStart.GooglePlayProductId);
            Assert.AreEqual("Head Start", headStart.Title);

            CloudItem magnet = catalog[RunnerShop.CoinMagnetId];
            Assert.IsNotNull(magnet);
            Assert.AreEqual("RUNNER_COIN_MAGNET", magnet.Id);
            Assert.AreEqual(GameType.EndlessRunner, magnet.Game);
            Assert.AreEqual(ItemKind.Owned, magnet.Kind, "a perk, bought once");
            Assert.AreEqual(150, magnet.Tokens);
            Assert.IsTrue(magnet.ForTokens);
            Assert.IsFalse(magnet.ForMoney);
            Assert.AreEqual("Coin Magnet", magnet.Title);

            CollectionAssert.AreEqual(new[] { headStart }, catalog.ForMoney());
            CollectionAssert.AreEqual(new[] { magnet }, catalog.ForTokens());
        }

        [Test]
        public void TheRegistryFindsTheDefinition()
        {
            Assert.IsInstanceOf<RunnerShop>(GameShops.Find(GameType.EndlessRunner));
            Assert.AreEqual(2, GameShops.CloudCatalog(GameType.EndlessRunner).Count);
        }

        [Test]
        public void TheCoinMagnetReachesFartherAndPullsWithoutAMagnet()
        {
            Assert.AreEqual(1f, RunnerShop.MagnetFactor(false), "without the perk a magnet reaches as far as it always did");
            Assert.AreEqual(0f, RunnerShop.PassiveMagnetFactor(false), "without the perk the coins stay put until a magnet is picked up");
            Assert.AreEqual(1.5f, RunnerShop.MagnetFactor(true));
            Assert.Greater(RunnerShop.PassiveMagnetFactor(true), 0f);
            Assert.Less(RunnerShop.PassiveMagnetFactor(true), RunnerShop.MagnetFactor(true), "a magnet picked up still reaches farther than none");
        }
    }
}
