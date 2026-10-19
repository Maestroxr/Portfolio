using System.Linq;
using Gamebox.Economy;
using NUnit.Framework;

namespace Portfolio.DeathRace.Tests
{
    /// <summary>The garage, the black market and the dealer through BaseGame's economy (<see cref="CareerShop"/>).</summary>
    public sealed class CareerShopTest
    {
        private CareerState state;
        private CareerShop shop;

        [SetUp]
        public void SetUp()
        {
            state = CareerTestKit.NewCareer();
            state.wallet.Add(100000);
            shop = new CareerShop(state);
        }

        [Test]
        public void TheCatalogHasEveryPage()
        {
            Assert.AreEqual(new[] { CareerShop.GaragePage, CareerShop.MarketPage, CareerShop.DealerPage }, shop.Catalog.Categories);
            Assert.AreEqual(CarSpecs.Count, shop.Catalog.InCategory(CareerShop.DealerPage).Count);
            Assert.AreEqual(ItemKind.Leveled, shop.Catalog[CareerShop.EngineId].Kind);
            Assert.AreEqual(ItemKind.Counted, shop.Catalog[CareerShop.MineId].Kind);
            Assert.AreEqual(ItemKind.Owned, shop.Catalog[CareerShop.SpikesId].Kind);
            Assert.AreEqual(ItemKind.Metered, shop.Catalog[CareerShop.RepairId].Kind);
            Assert.AreEqual(CarSpecs.Of(CarModel.Rustbucket).MaxLevelOf(UpgradeKind.Engine), shop.Catalog[CareerShop.EngineId].Max);
            Assert.AreEqual(CarModel.Viper, shop.Catalog[CareerShop.CarId(CarModel.Viper)].Tag);
        }

        [Test]
        public void UpgradesLevelTheCarUpToItsMaximum()
        {
            int money = state.wallet.Money;
            Assert.AreEqual(450, shop.NextPrice(state.consumables, shop.Catalog[CareerShop.EngineId]));
            Assert.AreEqual(BuyResult.Bought, shop.Buy(CareerShop.EngineId));
            Assert.AreEqual(1, state.Current.loadout.engine);
            Assert.AreEqual(money - 450, state.wallet.Money);
            Assert.AreEqual(BuyResult.Bought, shop.Buy(CareerShop.EngineId));
            Assert.AreEqual(BuyResult.Full, shop.Buy(CareerShop.EngineId), "the Rustbucket's engine goes to level 2");
            Assert.AreEqual(money - 450 - 1250, state.wallet.Money);
            Assert.AreEqual(2, shop.Owned(state.consumables, shop.Catalog[CareerShop.EngineId]));
            Assert.IsFalse(shop.CanSell(state.consumables, shop.Catalog[CareerShop.EngineId], out _), "upgrades stay on the car");
        }

        [Test]
        public void NothingIsBoughtWithoutTheMoney()
        {
            state.wallet.TrySpend(state.wallet.Money - 100);
            Assert.AreEqual(BuyResult.NoMoney, shop.Buy(CareerShop.EngineId));
            Assert.AreEqual(0, state.Current.loadout.engine);
            Assert.AreEqual(100, state.wallet.Money);
            Assert.AreEqual(BuyResult.NoMoney, shop.CanBuy(null, null, shop.Catalog[CareerShop.RocketId], out string reason));
            Assert.AreEqual(Shop.NoMoneyReason, reason);
        }

        [Test]
        public void EquipmentIsBoughtOnce()
        {
            Assert.AreEqual(BuyResult.Bought, shop.Buy(CareerShop.SpikesId));
            Assert.IsTrue(state.Current.loadout.spikes);
            Assert.AreEqual(BuyResult.Full, shop.Buy(CareerShop.SpikesId));
            Assert.AreEqual(BuyResult.Bought, shop.Buy(CareerShop.PiercingId));
            Assert.IsTrue(state.Current.loadout.piercing);
            Assert.AreEqual(2.5f, state.Current.loadout.Stats().RamDealt);
        }

        [Test]
        public void TheMarketStacksMinesRocketsAndNitroAndBuysThemBack()
        {
            int mines = state.consumables.Get(CareerShop.MineId);
            while (shop.Buy(CareerShop.MineId) == BuyResult.Bought)
            {
            }
            Assert.AreEqual(CareerRules.MineMax, state.consumables.Get(CareerShop.MineId));
            Assert.AreEqual(BuyResult.Full, shop.Buy(CareerShop.MineId));
            int money = state.wallet.Money;
            Assert.AreEqual(CareerRules.MinePrice / 2, shop.Sell(state.wallet, state.consumables, shop.Catalog[CareerShop.MineId]));
            Assert.AreEqual(money + CareerRules.MinePrice / 2, state.wallet.Money);
            Assert.AreEqual(CareerRules.MineMax - 1, state.consumables.Get(CareerShop.MineId));
            Assert.Greater(CareerRules.MineMax, mines);
        }

        [Test]
        public void RepairsArePaidByThePoint()
        {
            CarLoadout car = state.Current.loadout;
            car.health = 0.5f;
            Assert.AreEqual(50, Career.MissingPoints(car));
            Assert.AreEqual(150, Career.RepairCost(car), "3 a point on a tier 1 car");
            int money = state.wallet.Money;
            Assert.AreEqual(25, shop.RepairHalf());
            Assert.AreEqual(0.75f, car.health, 1e-4f);
            Assert.AreEqual(money - 75, state.wallet.Money);
            Assert.AreEqual(25, shop.RepairAll());
            Assert.AreEqual(1f, car.health);
            Assert.AreEqual(BuyResult.Full, shop.CanBuy(null, null, shop.Catalog[CareerShop.RepairId], out string reason));
            Assert.AreEqual(CareerShop.RepairedReason, reason);
        }

        [Test]
        public void ARepairStopsWhereTheMoneyDoes()
        {
            CarLoadout car = state.Current.loadout;
            car.health = 0f;
            state.wallet.TrySpend(state.wallet.Money - 31);
            Assert.AreEqual(10, shop.RepairAll(), "31 dollars buy ten points");
            Assert.AreEqual(0.1f, car.health, 1e-4f);
            Assert.AreEqual(1, state.wallet.Money);
        }

        [Test]
        public void ArmourRaisesTheHealthTheRepairsCountTo()
        {
            state.Current.loadout.health = 0.5f;
            shop.Buy(CareerShop.ArmorId);
            Assert.IsTrue(shop.IsStale);
            shop = new CareerShop(state);
            Assert.AreEqual(105, shop.Catalog[CareerShop.RepairId].Max);
            shop.RepairAll();
            Assert.AreEqual(1f, state.Current.loadout.health);
        }

        [Test]
        public void TheDealerTradesTheCarIn()
        {
            shop.Buy(CareerShop.EngineId);
            shop.Buy(CareerShop.SpikesId);
            int tradeIn = shop.TradeInValue;
            Assert.AreEqual(CareerRules.RoundPrice(0.5 * (450 + 900)), tradeIn);
            ShopItem viper = shop.Catalog[CareerShop.CarId(CarModel.Viper)];
            Assert.AreEqual(30000 - tradeIn, shop.NextPrice(state.consumables, viper));
            int money = state.wallet.Money;
            int mines = state.consumables.Get(CareerShop.MineId);
            Assert.AreEqual(BuyResult.Bought, shop.Buy(state.wallet, state.consumables, viper));
            Assert.AreEqual(money - (30000 - tradeIn), state.wallet.Money);
            Assert.AreEqual(CarModel.Viper, state.car);
            Assert.AreEqual(1, state.garage.Count);
            CarLoadout car = state.Current.loadout;
            Assert.AreEqual(0, car.engine);
            Assert.IsFalse(car.spikes, "the equipment went with the old car");
            Assert.AreEqual(1f, car.health);
            Assert.AreEqual(mines, state.consumables.Get(CareerShop.MineId), "the mines are the career's, not the car's");
            Assert.IsTrue(shop.IsStale);
            shop = new CareerShop(state);
            Assert.AreEqual(BuyResult.Full, shop.CanBuy(null, null, shop.Catalog[CareerShop.CarId(CarModel.Viper)], out string reason));
            Assert.AreEqual(CareerShop.YoursReason, reason);
            Assert.AreEqual(BuyResult.Locked, shop.CanBuy(null, null, shop.Catalog[CareerShop.CarId(CarModel.Hornet)], out reason));
            Assert.AreEqual(CareerShop.BetterCarReason, reason);
            Assert.AreEqual(CarSpecs.Of(CarModel.Viper).MaxLevelOf(UpgradeKind.Engine), shop.Catalog[CareerShop.EngineId].Max);
            Assert.AreEqual(CareerRules.UpgradePrice(UpgradeKind.Engine, 4, 1), shop.NextPrice(state.consumables, shop.Catalog[CareerShop.EngineId]));
        }

        [Test]
        public void SabotageNeedsADriverRacingToday()
        {
            Assert.AreEqual(BuyResult.Locked, shop.Buy(CareerShop.SabotageId));
            Assert.AreEqual(-1, shop.SabotagePrice("nobody"));
            Assert.AreEqual(9, shop.SabotageTargets().Count, "the nine rivals of the three offers");
            string hard = state.offers.First(offer => offer.difficulty == Difficulty.Hard).rivals[0];
            Assert.AreEqual(CareerRules.SabotagePrice(Difficulty.Hard), shop.SabotagePrice(hard));
            int money = state.wallet.Money;
            Assert.AreEqual(BuyResult.Bought, shop.Sabotage(hard));
            Assert.AreEqual(hard, state.sabotage);
            Assert.AreEqual(money - CareerRules.SabotagePrice(Difficulty.Hard), state.wallet.Money);
            string easy = state.offers.First(offer => offer.difficulty == Difficulty.Easy).rivals[0];
            Assert.AreEqual(BuyResult.Full, shop.Sabotage(easy), "one sabotage a day");
            Assert.AreEqual(hard, state.sabotage);
        }

        [Test]
        public void PurchasesAreAnnounced()
        {
            ShopItem bought = null;
            BuyResult result = BuyResult.NotForSale;
            shop.Purchased += (item, outcome) =>
            {
                bought = item;
                result = outcome;
            };
            shop.Buy(CareerShop.NitroId);
            Assert.AreEqual(CareerShop.NitroId, bought.Id);
            Assert.AreEqual(BuyResult.Bought, result);
        }

        [Test]
        public void EveryItemHasWords()
        {
            foreach (ShopItem item in shop.Catalog)
            {
                Assert.IsFalse(string.IsNullOrEmpty(item.Title), item.Id);
                Assert.IsFalse(string.IsNullOrEmpty(item.Description), item.Id);
                Assert.IsFalse(string.IsNullOrEmpty(item.Category), item.Id);
            }
            Assert.AreEqual(shop.Catalog.Count, shop.Catalog.Select(item => item.Id).Distinct().Count());
        }
    }
}
