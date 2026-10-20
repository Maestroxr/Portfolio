using System.Linq;
using System.Threading.Tasks;
using Gamebox.Economy;
using NUnit.Framework;

namespace Portfolio.DeathRace.Tests
{
    /// <summary>
    /// The career shop's later pages through BaseGame's economy (<see cref="CareerShop"/>): the garage's five upgrades,
    /// the workshop's fittings and guns, the special weapons of the black market and the one armed first, the dealer of
    /// each league, and the trade-in that counts everything on the car.
    /// </summary>
    public sealed class WorkshopShopTest
    {
        private static (CareerState state, CareerShop shop) Rich(LeagueKind league = LeagueKind.Wasteland)
        {
            CareerState state = Career.New("Tester", 0, CareerTestKit.Seed, league);
            state.wallet.Add(2000000);
            return (state, new CareerShop(state));
        }

        [Test]
        public void TheGarageHasFiveUpgradesOfTheCarsOwnMaximums()
        {
            (CareerState state, CareerShop shop) = Rich();
            Assert.AreEqual(CareerRules.Upgrades.Select(CareerShop.UpgradeId).Append(CareerShop.RepairId),
                shop.Catalog.InCategory(CareerShop.GaragePage).Select(item => item.Id));
            foreach (UpgradeKind kind in CareerRules.Upgrades)
            {
                ShopItem item = shop.Catalog[CareerShop.UpgradeId(kind)];
                Assert.AreEqual(ItemKind.Leveled, item.Kind);
                Assert.AreEqual(state.Current.loadout.Spec.MaxLevelOf(kind), item.Max, kind.ToString());
                Assert.AreEqual(CareerRules.UpgradePrice(kind, 1, 1), shop.NextPrice(state.consumables, item));
            }
            Assert.AreEqual(BuyResult.Bought, shop.Buy(CareerShop.BoostId));
            Assert.AreEqual(1, state.Current.loadout.boost);
            Assert.AreEqual(BuyResult.Full, shop.Buy(CareerShop.BoostId), "the Rustbucket's nitro system goes to level 1");
            Assert.AreEqual(BuyResult.Bought, shop.Buy(CareerShop.GunMountId));
            Assert.AreEqual(1, state.Current.loadout.gunMount);
            Assert.Greater(state.Current.loadout.Stats().NitroSeconds, CombatRules.NitroSeconds, "the charge burns longer");
        }

        [Test]
        public void TheWorkshopFitsEveryFittingOnce()
        {
            (CareerState state, CareerShop shop) = Rich();
            Assert.AreEqual(CareerRules.Fittings.Length + Guns.Count, shop.Catalog.InCategory(CareerShop.WorkshopPage).Count);
            foreach (EquipmentKind kind in CareerRules.Fittings)
            {
                string id = CareerShop.FittingId(kind);
                ShopItem item = shop.Catalog[id];
                Assert.AreEqual(ItemKind.Owned, item.Kind);
                Assert.AreEqual(CareerRules.FittingPrice(kind, 1), shop.NextPrice(state.consumables, item));
                int money = state.wallet.Money;
                Assert.AreEqual(BuyResult.Bought, shop.Buy(id), id);
                Assert.IsTrue(state.Current.loadout.Has(kind), id);
                Assert.AreEqual(money - CareerRules.FittingPrice(kind, 1), state.wallet.Money);
                Assert.AreEqual(BuyResult.Full, shop.CanBuy(null, null, item, out string reason), id);
                Assert.AreEqual(CareerShop.FittedReason, reason);
            }
            Assert.AreEqual(CareerShop.SpikesId, CareerShop.FittingId(EquipmentKind.Spikes), "saves keep the old ids");
            Assert.AreEqual(CareerShop.PiercingId, CareerShop.FittingId(EquipmentKind.Piercing));
            CarStats stats = state.Current.loadout.Stats();
            Assert.IsTrue(stats.Jammer && stats.Welder);
            Assert.Less(stats.CrashTaken, 1f);
            Assert.Less(stats.BlastTaken, 1f);
            Assert.Greater(CareerRules.FittingPrice(EquipmentKind.Shield, 4), CareerRules.FittingPrice(EquipmentKind.Shield, 1), "dearer on a better car");
            Assert.AreEqual(CareerRules.SpikesPrice(2), CareerRules.FittingPrice(EquipmentKind.Spikes, 2), "the bumper's price as it always was");
        }

        [Test]
        public void AGunBoughtIsMountedAndAGunOwnedIsMountedAgainForNothing()
        {
            (CareerState state, CareerShop shop) = Rich();
            CarLoadout car = state.Current.loadout;
            ShopItem machineGun = shop.Catalog[CareerShop.GunId(GunKind.MachineGun)];
            Assert.AreEqual(1, shop.Owned(state.consumables, machineGun), "the machine gun is on the bonnet");
            Assert.AreEqual(BuyResult.Full, shop.CanBuy(null, null, machineGun, out string reason));
            Assert.AreEqual(CareerShop.MountedReason, reason);

            ShopItem minigun = shop.Catalog[CareerShop.GunId(GunKind.Minigun)];
            Assert.AreEqual(CareerRules.GunPrice(GunKind.Minigun, 1), shop.NextPrice(state.consumables, minigun));
            int money = state.wallet.Money;
            Assert.AreEqual(BuyResult.Bought, shop.Buy(CareerShop.GunId(GunKind.Minigun)));
            Assert.AreEqual(GunKind.Minigun, car.gun);
            Assert.AreEqual(GunKind.Minigun, car.Stats().Gun);
            Assert.AreEqual(money - CareerRules.GunPrice(GunKind.Minigun, 1), state.wallet.Money);
            Assert.AreEqual(0, shop.NextPrice(state.consumables, machineGun), "the machine gun goes back on for nothing");

            Assert.AreEqual(BuyResult.Bought, shop.Buy(CareerShop.GunId(GunKind.Plasma)));
            Assert.AreEqual(GunKind.Plasma, car.gun);
            Assert.IsTrue(car.OwnsGun(GunKind.Minigun) && car.OwnsGun(GunKind.Plasma));
            Assert.AreEqual(0, shop.NextPrice(state.consumables, minigun), "a gun the car has costs nothing to mount");
            money = state.wallet.Money;
            Assert.AreEqual(BuyResult.Bought, shop.Buy(CareerShop.GunId(GunKind.Minigun)));
            Assert.AreEqual(GunKind.Minigun, car.gun);
            Assert.AreEqual(money, state.wallet.Money);
            Assert.AreEqual(BuyResult.Bought, shop.Buy(CareerShop.GunId(GunKind.MachineGun)));
            Assert.AreEqual(GunKind.MachineGun, car.gun);
            Assert.IsTrue(shop.HasGun(GunKind.Plasma), "the others stay in the garage");
            Assert.IsFalse(shop.CanSell(state.consumables, minigun, out _), "guns stay with the car");
        }

        [Test]
        public void TheBlackMarketSellsTheSpecialWeaponsAndArmsOneFirst()
        {
            (CareerState state, CareerShop shop) = Rich();
            Assert.AreEqual(new[] { CareerShop.MineId, CareerShop.RocketId, CareerShop.NitroId, "oil", "emp", "mortar", CareerShop.SabotageId },
                shop.Catalog.InCategory(CareerShop.MarketPage).Select(item => item.Id));
            foreach (SpecialSpec special in Specials.All)
            {
                ShopItem item = shop.Catalog[special.Id];
                Assert.AreEqual(ItemKind.Counted, item.Kind);
                Assert.AreEqual(special.Max, item.Max);
                Assert.AreEqual(special.Price, shop.NextPrice(state.consumables, item));
                while (shop.Buy(special.Id) == BuyResult.Bought)
                {
                }
                Assert.AreEqual(special.Max, state.consumables.Get(special.Id));
                int money = state.wallet.Money;
                Assert.AreEqual(special.Price / 2, shop.Sell(state.wallet, state.consumables, item), "bought back for half");
                Assert.AreEqual(money + special.Price / 2, state.wallet.Money);
            }
            Assert.AreEqual(SpecialKind.Oil, shop.Armed);
            shop.Arm(SpecialKind.Mortar);
            Assert.AreEqual(SpecialKind.Mortar, state.armed);
            Assert.AreEqual(SpecialKind.Mortar, Career.PlayerLoadout(state).special);
            Assert.AreEqual(Specials.Of(SpecialKind.Mortar).Max - 1, Career.PlayerLoadout(state).mortar);
        }

        [TestCase(LeagueKind.Wasteland, 1, 6)]
        [TestCase(LeagueKind.Iron, 3, 8)]
        [TestCase(LeagueKind.Hellfire, 5, 9)]
        public void TheDealerSellsTheCarsOfTheLeaguesTiers(LeagueKind league, int lowest, int highest)
        {
            (CareerState state, CareerShop shop) = Rich(league);
            var sold = shop.Catalog.InCategory(CareerShop.DealerPage).Select(item => (CarModel)item.Tag).ToList();
            Assert.AreEqual(CarSpecs.All.Where(car => car.Tier >= lowest && car.Tier <= highest).Select(car => car.Model).OrderBy(model => model),
                sold.OrderBy(model => model));
            Assert.IsTrue(sold.Contains(state.car));
            CarModel start = state.car;
            CarModel better = CarSpecs.OfTier(CarSpecs.Of(start).Tier + 1);
            Assert.AreEqual(BuyResult.Bought, shop.Buy(state.wallet, state.consumables, shop.Catalog[CareerShop.CarId(better)]));
            shop = new CareerShop(state);
            Assert.AreEqual(BuyResult.Locked, shop.CanBuy(null, null, shop.Catalog[CareerShop.CarId(start)], out string reason), "a car of a lower tier");
            Assert.AreEqual(CareerShop.BetterCarReason, reason);
            foreach (CarModel other in CarSpecs.InTier(CarSpecs.Of(better).Tier).Where(model => model != better))
            {
                Assert.AreEqual(BuyResult.Bought, shop.CanBuy(null, null, shop.Catalog[CareerShop.CarId(other)], out _), $"{other}: another car of the same tier");
            }
        }

        [Test]
        public void TheDealerCountsEverythingOnTheCarInTheTradeIn()
        {
            (CareerState state, CareerShop shop) = Rich(LeagueKind.Iron);
            shop.Buy(CareerShop.EngineId);
            shop.Buy(CareerShop.BoostId);
            shop.Buy(CareerShop.GunMountId);
            shop.Buy(CareerShop.FittingId(EquipmentKind.Cage));
            shop.Buy(CareerShop.FittingId(EquipmentKind.Welder));
            shop.Buy(CareerShop.GunId(GunKind.Flamethrower));
            shop.Buy(CareerShop.GunId(GunKind.Minigun));
            int tier = CarSpecs.Of(state.car).Tier;
            int invested = CareerRules.UpgradePrice(UpgradeKind.Engine, tier, 1) + CareerRules.UpgradePrice(UpgradeKind.Boost, tier, 1) +
                           CareerRules.UpgradePrice(UpgradeKind.GunMount, tier, 1) + CareerRules.FittingPrice(EquipmentKind.Cage, tier) +
                           CareerRules.FittingPrice(EquipmentKind.Welder, tier) + CareerRules.GunPrice(GunKind.Flamethrower, tier) +
                           CareerRules.GunPrice(GunKind.Minigun, tier);
            Assert.AreEqual(invested, CareerRules.Invested(state.Current.loadout));
            Assert.AreEqual(CareerRules.RoundPrice(0.5 * (CarSpecs.Of(state.car).Price + invested)), shop.TradeInValue);
            ShopItem viper = shop.Catalog[CareerShop.CarId(CarModel.Viper)];
            Assert.AreEqual(BuyResult.Bought, shop.Buy(state.wallet, state.consumables, viper));
            CarLoadout car = state.Current.loadout;
            Assert.AreEqual(GunKind.MachineGun, car.gun, "the guns went with the old car");
            Assert.IsFalse(car.cage || car.welder || car.OwnsGun(GunKind.Flamethrower));
        }

        [Test]
        public async Task TheCrossGameShopSellsTheWorkshopToo()
        {
            (CareerState state, CareerShop _) = Rich(LeagueKind.Iron);
            var ledger = new CareerLedger(state);
            ShopItem plasma = ledger.Catalog[CareerShop.GunId(GunKind.Plasma)];
            Assert.IsTrue(ledger.Shows(plasma));
            Assert.IsTrue((await ledger.BuyAsync(plasma, 1)).Bought);
            Assert.AreEqual(GunKind.Plasma, state.Current.loadout.gun);
            ShopItem shield = ledger.Catalog[CareerShop.FittingId(EquipmentKind.Shield)];
            Assert.IsTrue((await ledger.BuyAsync(shield, 1)).Bought);
            Assert.IsTrue(state.Current.loadout.shield);
            ShopItem emp = ledger.Catalog[CareerShop.SpecialId(SpecialKind.Emp)];
            int before = state.consumables.Get(emp.Id);
            Assert.IsTrue((await ledger.BuyAsync(emp, 1)).Bought);
            Assert.AreEqual(before + 1, state.consumables.Get(emp.Id));
            Assert.IsFalse(ledger.Catalog.InCategory(CareerShop.DealerPage).Any(item => (CarModel)item.Tag == CarModel.Rustbucket),
                "the Iron League's dealer has no Rustbucket");
        }
    }
}
