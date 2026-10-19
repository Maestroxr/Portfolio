using System.Threading.Tasks;
using Gamebox;
using Gamebox.Economy;
using NUnit.Framework;

namespace Portfolio.DeathRace.Tests
{
    /// <summary>
    /// Death Race in the cross-game shop: the two cloud items, the career's pages as a ledger of the shared window, the
    /// Pit Crew's free repairs and the cash that waits for a career.
    /// </summary>
    public sealed class DeathRaceShopTest
    {
        [Test]
        public void TheDefinitionSellsThePitCrewForMoneyAndCashForTokens()
        {
            var catalog = new CloudCatalog();
            new DeathRaceShop().DefineCloud(catalog);
            Assert.AreEqual(2, catalog.Count);
            CloudItem pitCrew = catalog[DeathRaceShop.PitCrewId];
            Assert.AreEqual(GameType.DeathRace, pitCrew.Game);
            Assert.AreEqual(100, pitCrew.Cents);
            Assert.AreEqual("$1.00", pitCrew.MoneyPrice);
            Assert.AreEqual(ItemKind.Owned, pitCrew.Kind);
            Assert.AreEqual(DeathRaceShop.PitCrewProductId, pitCrew.GooglePlayProductId);
            Assert.IsFalse(pitCrew.ForTokens);
            CloudItem cash = catalog[DeathRaceShop.CashId];
            Assert.AreEqual(120, cash.Tokens);
            Assert.AreEqual(ItemKind.Counted, cash.Kind);
            Assert.AreEqual(DeathRaceShop.CashMax, cash.Max);
            Assert.IsFalse(cash.ForMoney);
            Assert.AreEqual("DEATHRACE_CASH_5000_FOR_TOKENS", cash.TokensPurchaseId);
        }

        [Test]
        public async Task TheCareerLedgerSellsTheGaragePagesOverTheCareer()
        {
            CareerState state = CareerTestKit.NewCareer();
            state.wallet.Add(100000);
            int saved = 0;
            var ledger = new CareerLedger(state, () => saved++);
            Assert.AreEqual(ShopKeeping.Local, ledger.Keeping);
            Assert.AreEqual(new[] { CareerShop.GaragePage, CareerShop.WorkshopPage, CareerShop.MarketPage, CareerShop.DealerPage }, ledger.Catalog.Categories);
            Assert.AreEqual("Garage", ledger.PageTitle(CareerShop.GaragePage));
            Assert.AreEqual("Workshop", ledger.PageTitle(CareerShop.WorkshopPage));
            Assert.AreEqual("Black Market", ledger.PageTitle(CareerShop.MarketPage));
            Assert.AreEqual("Dealer", ledger.PageTitle(CareerShop.DealerPage));
            Assert.AreSame(state.wallet, ledger.Wallet);
            Assert.AreSame(state.consumables, ledger.Inventory);
            Assert.IsFalse(ledger.Shows(ledger.Catalog[CareerShop.SabotageId]), "Sabotage needs the garage's picker of a rival");
            Assert.IsTrue(ledger.Shows(ledger.Catalog[CareerShop.MineId]));

            int money = state.wallet.Money;
            ShopItem engine = ledger.Catalog[CareerShop.EngineId];
            ShopOutcome bought = await ledger.BuyAsync(engine, 1);
            Assert.IsTrue(bought.Bought, bought.Reason);
            Assert.AreEqual(1, state.Current.loadout.engine);
            Assert.Less(state.wallet.Money, money);
            Assert.AreEqual(1, saved, "The career is saved after a purchase");
            Assert.IsFalse(ledger.Stale, "An upgrade keeps the catalog");

            ShopItem mine = ledger.Catalog[CareerShop.MineId];
            int mines = state.consumables.Get(CareerShop.MineId);
            Assert.IsTrue((await ledger.BuyAsync(mine, 1)).Bought);
            Assert.Greater(state.consumables.Get(CareerShop.MineId), mines, "One more mine than the career started with");

            // A new car makes the catalog over: the ledger builds its shop again for the car now driven.
            CareerShop before = ledger.Career;
            ShopItem car = null;
            foreach (ShopItem offered in ledger.Catalog.InCategory(CareerShop.DealerPage))
            {
                if (ledger.CanBuy(offered, out _) == BuyResult.Bought)
                {
                    car = offered;
                    break;
                }
            }
            Assert.IsNotNull(car, "A car the money buys");
            Assert.IsTrue((await ledger.BuyAsync(car, 1)).Bought);
            Assert.AreEqual(car.Tag, state.car);
            Assert.AreNotSame(before, ledger.Career, "The shop was built again for the new car");
            Assert.IsFalse(ledger.Stale);
            Assert.AreEqual(3, saved);
        }

        [Test]
        public void ThePitCrewMakesRepairsFree()
        {
            Assert.AreEqual(CareerRules.RepairPrice(2), DeathRaceShop.RepairPrice(2, false));
            Assert.AreEqual(0, DeathRaceShop.RepairPrice(2, true));
            Assert.AreEqual(0, DeathRaceShop.RepairPrice(1, true));
        }

        [Test]
        public void CashBoughtWithoutACareerWaitsForTheNextOne()
        {
            var progress = new DeathRaceProgress(new TransientStrategy());
            Assert.AreEqual(0, DeathRaceShop.TakePendingCash(progress));
            DeathRaceShop.AddPendingCash(progress, DeathRaceShop.CashAmount);
            DeathRaceShop.AddPendingCash(progress, DeathRaceShop.CashAmount);
            Assert.AreEqual(2 * DeathRaceShop.CashAmount, DeathRaceShop.TakePendingCash(progress));
            Assert.AreEqual(0, DeathRaceShop.TakePendingCash(progress), "Taken once");
        }
    }
}
