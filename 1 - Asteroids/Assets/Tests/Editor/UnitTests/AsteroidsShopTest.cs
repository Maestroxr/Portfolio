using System.Collections.Generic;
using System.Threading.Tasks;
using Gamebox;
using Gamebox.Economy;
using NUnit.Framework;
using UnityEngine;
using EconomyResult = Gamebox.Economy.BuyResult;

namespace Portfolio.Asteroids.Tests
{
    /// <summary>
    /// The game's shop: the two cloud items of the definition, the Supply Room as a ledger over a pilot (buying and
    /// selling a weapon through the window's calls, the energy module by the point, the pilot saved where it is kept)
    /// and the spare ship's extra life.
    /// </summary>
    public class AsteroidsShopTest
    {
        private const string Owned = "Gamebox.Cloud.Owned";
        private const string Balance = "Gamebox.Cloud.Balance.TOKENS";
        private const string Pending = "Gamebox.Cloud.Pending.TOKENS";
        private readonly Dictionary<string, string> putAside = new Dictionary<string, string>();


        [SetUp]
        public void SetUp()
        {
            // The editor's own cloud mirror stays as it was.
            putAside.Clear();
            try
            {
                foreach (string key in new[] { Owned, Balance, Pending })
                {
                    if (PlayerPrefs.HasKey(key))
                    {
                        putAside[key] = PlayerPrefs.GetString(key, PlayerPrefs.GetInt(key, 0).ToString());
                    }
                }
            }
            catch (System.Exception)
            {
                // No PlayerPrefs outside the engine: the mirror lives in memory there.
            }
            CloudEconomy.Reset();
        }


        [TearDown]
        public void TearDown()
        {
            CloudEconomy.Reset();
            try
            {
                foreach (KeyValuePair<string, string> pair in putAside)
                {
                    if (pair.Key == Pending)
                    {
                        PlayerPrefs.SetInt(pair.Key, int.TryParse(pair.Value, out int pending) ? pending : 0);
                    }
                    else
                    {
                        PlayerPrefs.SetString(pair.Key, pair.Value);
                    }
                }
            }
            catch (System.Exception)
            {
            }
        }


        private static StrikeLoadout Rich()
        {
            StrikeLoadout pilot = StrikeLoadout.NewPilot();
            pilot.Money = 5000000;
            return pilot;
        }


        [Test]
        public void TheCloudSellsASpareShipForMoneyAndCreditsForTokens()
        {
            var shop = new AsteroidsShop();
            Assert.That(shop.Game, Is.EqualTo(GameType.Asteroids));
            Assert.That(shop.Title, Is.EqualTo("Supply Room"));
            var catalog = new CloudCatalog();
            shop.DefineCloud(catalog);
            Assert.That(catalog.Count, Is.EqualTo(2));

            CloudItem ship = catalog["ASTEROIDS_SPARE_SHIP"];
            Assert.IsNotNull(ship);
            Assert.That(ship.Game, Is.EqualTo(GameType.Asteroids));
            Assert.That(ship.Cents, Is.EqualTo(100));
            Assert.That(ship.MoneyPrice, Is.EqualTo("$1.00"));
            Assert.IsTrue(ship.ForMoney);
            Assert.IsFalse(ship.ForTokens);
            Assert.That(ship.Kind, Is.EqualTo(ItemKind.Owned));
            Assert.That(ship.AppleProductId, Is.EqualTo("com.skinnerboxes.asteroids.spareship"));
            Assert.That(ship.GooglePlayProductId, Is.EqualTo("com.skinnerboxes.asteroids.spareship"));

            CloudItem credits = catalog["ASTEROIDS_CREDITS_5000"];
            Assert.IsNotNull(credits);
            Assert.That(credits.Tokens, Is.EqualTo(120));
            Assert.IsTrue(credits.ForTokens);
            Assert.IsFalse(credits.ForMoney);
            Assert.That(credits.Kind, Is.EqualTo(ItemKind.Counted));
            Assert.That(credits.Max, Is.EqualTo(99));
        }


        [Test]
        public void TheLedgerListsTheSupplyRoomOnTwoPages()
        {
            var ledger = new PilotLedger(ShopKeeping.Local, StrikeLoadout.NewPilot(), null);
            Assert.That(ledger.Keeping, Is.EqualTo(ShopKeeping.Local));
            Assert.That(ledger.Caption, Is.EqualTo("Kept on this device"));
            Assert.That(ledger.Catalog.Categories, Is.EqualTo(new[] { "Weapons", "Gear" }), "The weapons come first, as in the room.");
            foreach (StrikeItemInfo info in StrikeArmory.All)
            {
                ShopItem item = ledger.Catalog[info.Item.ToString()];
                if (info.NotForSale)
                {
                    Assert.IsNull(item, $"{info.Item} is not for sale.");
                    continue;
                }
                Assert.IsNotNull(item, info.Item.ToString());
                Assert.That(item.Category, Is.EqualTo(StrikeUI.CategoryOf(info.Item) == StrikeUI.SupplyCategory.Weapons ? "Weapons" : "Gear"), info.Item.ToString());
                Assert.That(item.Title, Is.EqualTo(info.Title));
                Assert.That(item.Description, Is.EqualTo(info.Description));
                Assert.That(item.Cost(1), Is.EqualTo(info.Price), info.Item.ToString());
                Assert.That(item.Resale, Is.EqualTo(0.5f).Within(1e-5f), info.Item.ToString());
                Assert.That(item.Tag, Is.EqualTo(info.Item));
                Assert.That(PilotLedger.ItemOf(item), Is.EqualTo(info.Item));
            }
            IReadOnlyList<ShopItem> weapons = ledger.Catalog.InCategory("Weapons");
            Assert.That(weapons[0].Id, Is.EqualTo("AirMissiles"), "The cheapest weapon comes first.");
            Assert.That(weapons[weapons.Count - 1].Id, Is.EqualTo("TwinLaser"));
            Assert.That(ledger.Catalog["Dumbfire"].Kind, Is.EqualTo(ItemKind.Counted));
            Assert.That(ledger.Catalog["Dumbfire"].Max, Is.EqualTo(20));
            Assert.That(ledger.Catalog["PlasmaCannon"].Kind, Is.EqualTo(ItemKind.Owned), "One at most: owned, as the room's card says.");
            ShopItem energy = ledger.Catalog["EnergyModule"];
            Assert.That(energy.Category, Is.EqualTo("Gear"));
            Assert.That(energy.Kind, Is.EqualTo(ItemKind.Metered));
            Assert.That(energy.Max, Is.EqualTo(StrikeRules.MaxEnergy));
            Assert.That(ledger.MeteredStep(energy), Is.EqualTo(StrikeArmory.EnergyModuleStep), "75 energy: a whole module to go.");

            // A new pilot: the wallet mirrors the money, the inventory the energy (the machine gun is not for sale).
            Assert.That(ledger.Wallet.Money, Is.EqualTo(StrikeRules.NewPilotMoney));
            Assert.That(ledger.Rules.Owned(ledger.Inventory, energy), Is.EqualTo(75));
            Assert.That(ledger.Inventory.Get("EnergyModule"), Is.EqualTo(75));
            Assert.That(ledger.Inventory.entries.Count, Is.EqualTo(1));
        }


        [Test]
        public async Task BuyingAWeaponTakesTheMoneyAndSellingItPaysHalf()
        {
            var progress = new AsteroidsProgress(new TransientStrategy(), GameType.Asteroids);
            StrikeLoadout pilot = Rich();
            var ledger = new PilotLedger(ShopKeeping.Local, pilot, progress.SavePilot);
            int changes = 0;
            ledger.Changed += () => changes++;
            ShopItem plasma = ledger.Catalog["PlasmaCannon"];
            Assert.That(ledger.CanBuy(plasma, out _), Is.EqualTo(EconomyResult.Bought));
            Assert.That(ledger.Rules.NextPrice(ledger.Inventory, plasma), Is.EqualTo(78800));

            ShopOutcome outcome = await ledger.BuyAsync(plasma, 1);
            Assert.IsTrue(outcome.Bought);
            Assert.That(pilot.Money, Is.EqualTo(5000000 - 78800));
            Assert.That(pilot.Count(StrikeItem.PlasmaCannon), Is.EqualTo(1));
            Assert.That(ledger.Wallet.Money, Is.EqualTo(pilot.Money), "The wallet mirrors the pilot.");
            Assert.That(ledger.Inventory.Get("PlasmaCannon"), Is.EqualTo(1), "The inventory mirrors the pilot.");
            Assert.That(ledger.Rules.Owned(ledger.Inventory, plasma), Is.EqualTo(1));
            Assert.That(changes, Is.GreaterThan(0), "The window is told.");
            StrikeLoadout saved = progress.LoadPilot();
            Assert.That(saved.Count(StrikeItem.PlasmaCannon), Is.EqualTo(1), "Saved where the pilot is kept.");
            Assert.That(saved.Money, Is.EqualTo(5000000 - 78800));
            Assert.That(ledger.CanBuy(plasma, out string reason), Is.EqualTo(EconomyResult.Full), "One plasma cannon at most.");
            Assert.That(reason, Is.EqualTo(Shop.FullReason));
            Assert.That(ledger.Rules.NextPrice(ledger.Inventory, plasma), Is.EqualTo(-1));
            Assert.IsFalse((await ledger.BuyAsync(plasma, 1)).Bought);

            Assert.IsTrue(ledger.Rules.CanSell(ledger.Inventory, plasma, out _));
            Assert.That(ledger.Rules.SellPrice(ledger.Inventory, plasma), Is.EqualTo(39400));
            Assert.That(await ledger.SellAsync(plasma), Is.EqualTo(39400));
            Assert.That(pilot.Count(StrikeItem.PlasmaCannon), Is.Zero);
            Assert.That(pilot.Money, Is.EqualTo(5000000 - 78800 + 39400));
            Assert.That(ledger.Inventory.Get("PlasmaCannon"), Is.Zero);
            Assert.That(progress.LoadPilot().Count(StrikeItem.PlasmaCannon), Is.Zero, "The sale is saved too.");
            Assert.IsFalse(ledger.Rules.CanSell(ledger.Inventory, plasma, out string why));
            Assert.That(why, Is.EqualTo(Shop.NothingToSellReason));
            Assert.That(await ledger.SellAsync(plasma), Is.Zero, "Nothing left to sell.");

            // Counted items stack up to their cap.
            ShopItem bombs = ledger.Catalog["MegaBomb"];
            for (int i = 0; i < 5; i++)
            {
                Assert.IsTrue((await ledger.BuyAsync(bombs, 1)).Bought, $"Megabomb {i + 1}");
            }
            Assert.That(ledger.Rules.Owned(ledger.Inventory, bombs), Is.EqualTo(5));
            Assert.That(ledger.CanBuy(bombs, out _), Is.EqualTo(EconomyResult.Full));
            Assert.That(progress.LoadPilot().Megabombs, Is.EqualTo(5));

            // A new pilot cannot afford a special.
            var poor = new PilotLedger(ShopKeeping.Local, StrikeLoadout.NewPilot(), null);
            Assert.That(poor.CanBuy(poor.Catalog["AirMissiles"], out string broke), Is.EqualTo(EconomyResult.NoMoney));
            Assert.That(broke, Is.EqualTo(Shop.NoMoneyReason));
            Assert.IsFalse((await poor.BuyAsync(poor.Catalog["AirMissiles"], 1)).Bought);
            Assert.That(poor.Pilot.Money, Is.EqualTo(StrikeRules.NewPilotMoney), "A refused purchase takes nothing.");
        }


        [Test]
        public async Task TheEnergyModuleBuysTwentyFivePoints()
        {
            StrikeLoadout pilot = StrikeLoadout.NewPilot();
            pilot.Money = 100000;
            var ledger = new PilotLedger(ShopKeeping.Online, pilot, null);
            Assert.That(ledger.Caption, Is.EqualTo("Kept in your online progress"));
            ShopItem energy = ledger.Catalog["EnergyModule"];
            int step = ledger.MeteredStep(energy);
            Assert.That(step, Is.EqualTo(25));
            Assert.That(ledger.Rules.NextPrice(ledger.Inventory, energy), Is.EqualTo(10000), "25 points at 400.");
            Assert.That(ledger.Rules.MeteredPrice(energy, step), Is.EqualTo(10000), "What the window shows is what is charged.");

            ShopOutcome outcome = await ledger.BuyAsync(energy, step);
            Assert.IsTrue(outcome.Bought);
            Assert.That(outcome.Points, Is.EqualTo(25));
            Assert.That(pilot.Energy, Is.EqualTo(100f));
            Assert.That(pilot.Money, Is.EqualTo(90000));
            Assert.That(ledger.Rules.Owned(ledger.Inventory, energy), Is.EqualTo(100));
            Assert.That(ledger.Inventory.Get("EnergyModule"), Is.EqualTo(100));
            Assert.That(ledger.CanBuy(energy, out string reason), Is.EqualTo(EconomyResult.Full));
            Assert.That(ledger.Rules.NextPrice(ledger.Inventory, energy), Is.EqualTo(-1));
            Assert.IsFalse((await ledger.BuyAsync(energy, step)).Bought);
            Assert.That(pilot.Money, Is.EqualTo(90000));

            // Only the points missing are charged.
            pilot.Energy = 90f;
            pilot.NotifyChanged();
            Assert.That(ledger.MeteredStep(energy), Is.EqualTo(10));
            Assert.That(ledger.Rules.NextPrice(ledger.Inventory, energy), Is.EqualTo(4000));
            outcome = await ledger.BuyAsync(energy, ledger.MeteredStep(energy));
            Assert.That(outcome.Points, Is.EqualTo(10));
            Assert.That(pilot.Money, Is.EqualTo(86000));

            // Energy sells 25 at a time for 5,000 while the launch minimum stays.
            Assert.That(ledger.Rules.SellPrice(ledger.Inventory, energy), Is.EqualTo(StrikeArmory.EnergySellPrice));
            Assert.That(await ledger.SellAsync(energy), Is.EqualTo(5000));
            Assert.That(pilot.Energy, Is.EqualTo(75f));
            Assert.That(pilot.Money, Is.EqualTo(91000));
            pilot.Energy = 40f;
            pilot.NotifyChanged();
            Assert.IsFalse(ledger.Rules.CanSell(ledger.Inventory, energy, out _), "25 must stay.");
            Assert.That(await ledger.SellAsync(energy), Is.Zero);
        }


        [Test]
        public void TheSpareShipAddsALifeWithinTheLimit()
        {
            Assert.IsFalse(CloudEconomy.Owns(AsteroidsShop.SpareShipId));
            Assert.That(AsteroidsShop.WithSpareShip(3), Is.EqualTo(3), "Without the perk the game plays as before.");
            CloudEconomy.TakeItemChange(AsteroidsShop.SpareShipId, 1);
            Assert.IsTrue(CloudEconomy.Owns(AsteroidsShop.SpareShipId));
            Assert.That(AsteroidsShop.WithSpareShip(3), Is.EqualTo(4));
            Assert.That(AsteroidsShop.WithSpareShip(AsteroidSettings.LivesLimit), Is.EqualTo(AsteroidSettings.LivesLimit), "Never past the limit.");
            CloudEconomy.TakeItemChange(AsteroidsShop.SpareShipId, -1);
            Assert.That(AsteroidsShop.WithSpareShip(3), Is.EqualTo(3));
        }
    }
}
