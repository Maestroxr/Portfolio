using System;
using System.Threading.Tasks;
using Gamebox;
using Gamebox.Economy;
using Gamebox.Online;
using UnityEngine;
using EconomyResult = Gamebox.Economy.BuyResult;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// What Asteroids sells through the shop window (<see cref="GameShopDefinition"/>): the Supply Room of Planet Strike
    /// as two pages over the strike pilot (<see cref="PilotLedger"/>), and the game's items in the cloud: a spare ship
    /// sold for money (one more life in every field mission, read where the lives are set through
    /// <see cref="WithSpareShip"/>) and a purse of credits sold for Tokens, paid into the pilot's wallet when bought.
    /// </summary>
    public sealed class AsteroidsShop : GameShopDefinition
    {
        /// <summary>The perk: +1 life in every field mission.</summary>
        public const string SpareShipId = "ASTEROIDS_SPARE_SHIP";
        public const int SpareShipCents = 100;
        public const string SpareShipProductId = "com.skinnerboxes.asteroids.spareship";

        /// <summary>The consumable: 5,000 credits for the strike pilot.</summary>
        public const string CreditsId = "ASTEROIDS_CREDITS_5000";
        public const int CreditsAmount = 5000;
        public const int CreditsTokens = 120;
        public const int CreditsMax = 99;

        public override GameType Game => GameType.Asteroids;

        public override string Title => "Supply Room";


        public override void DefineCloud(CloudCatalog catalog)
        {
            catalog.AddForMoney(Game, SpareShipId, "Spare Ship", "+1 life in every field mission", SpareShipCents, SpareShipProductId);
            catalog.AddForTokens(Game, CreditsId, "5,000 Credits", "5,000 credits for your strike pilot", CreditsTokens, ItemKind.Counted, CreditsMax);
        }


        /// <summary>The Supply Room over the saved pilot, or over the online pilot in a lobby; nothing while neither is at hand.</summary>
        public override void DefineGame(GameShop shop, BaseGameManager manager)
        {
            PilotKeeper keeper = manager != null && manager.InSession ? PilotKeeper.Online(manager) : PilotKeeper.Local(manager);
            if (keeper != null)
            {
                shop.Add(new PilotLedger(keeper.Keeping, keeper.Pilot, keeper.Save));
            }
        }


        /// <summary>
        /// The credits bought: taken out of the cloud, then paid into the wallet of the pilot the shop works on (in a
        /// lobby the online pilot's, else the game's saved pilot; from the launcher the device's saved pilot).
        /// </summary>
        public override async void OnCloudBought(CloudItem item, BaseGameManager manager)
        {
            if (item == null || item.Id != CreditsId)
            {
                return;
            }
            try
            {
                string error = await CloudEconomy.ConsumeAsync(item.Id);
                if (error != null)
                {
                    Debug.LogWarning($"The credits bought could not be taken out of the cloud: {error}");
                    return;
                }
                PilotKeeper keeper = (manager != null && manager.InSession ? PilotKeeper.Online(manager) : null)
                    ?? PilotKeeper.Local(manager) ?? PilotKeeper.OnDevice();
                keeper.Pilot.AddMoney(CreditsAmount);
                keeper.Save(keeper.Pilot);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }


        /// <summary>The active theme's hangar ship for the spare ship and its credits icon for the purse; null without a theme.</summary>
        public override Sprite IconOf(CloudItem item)
        {
            AsteroidsTheme theme = AsteroidsThemes.Active;
            if (theme == null || item == null)
            {
                return null;
            }
            return item.Id == SpareShipId ? theme.Icon("Ship") : item.Id == CreditsId ? theme.StrikeIcon("Money") : null;
        }


        /// <summary>The lives a field mission starts with: one more than <paramref name="lives"/> with the spare ship, within the limit.</summary>
        public static int WithSpareShip(int lives)
        {
            return CloudEconomy.Owns(SpareShipId) ? Mathf.Min(AsteroidSettings.LivesLimit, lives + 1) : lives;
        }
    }


    /// <summary>
    /// The strike pilot the shop works on and how it is saved: the game's saved pilot (kept on the device through the
    /// manager's progress), the pilot of the online progress while the game is played in a lobby, or the device's saved
    /// pilot read straight from PlayerPrefs where no game runs (the launcher).
    /// </summary>
    public sealed class PilotKeeper
    {
        private readonly Action<StrikeLoadout> save;

        private PilotKeeper(ShopKeeping keeping, StrikeLoadout pilot, Action<StrikeLoadout> save)
        {
            Keeping = keeping;
            Pilot = pilot;
            this.save = save;
        }

        public ShopKeeping Keeping { get; }

        public StrikeLoadout Pilot { get; }


        /// <summary>Saves <paramref name="pilot"/> where it is kept.</summary>
        public void Save(StrikeLoadout pilot)
        {
            save?.Invoke(pilot);
        }


        /// <summary>The saved pilot of <paramref name="manager"/>'s game, saved through its progress; null for another manager.</summary>
        public static PilotKeeper Local(BaseGameManager manager)
        {
            if (!(manager is AsteroidsGameManager asteroids))
            {
                return null;
            }
            AsteroidsProgress progress = asteroids.Progress;
            return new PilotKeeper(ShopKeeping.Local, asteroids.Pilot, pilot => progress?.SavePilot(pilot));
        }


        /// <summary>The pilot of the online progress of <paramref name="manager"/>'s session; null before the first login.</summary>
        public static PilotKeeper Online(BaseGameManager manager)
        {
            if (manager != null && manager.Session is OnlineGameController online && online.OnlineProgress is AsteroidsProgress progress)
            {
                return new PilotKeeper(ShopKeeping.Online, progress.LoadPilot(), progress.SavePilot);
            }
            return null;
        }


        /// <summary>The device's saved pilot, read from PlayerPrefs (the launcher, where no game runs).</summary>
        public static PilotKeeper OnDevice()
        {
            var progress = new AsteroidsProgress(new PlayerPrefsStrategy(), GameType.Asteroids);
            return new PilotKeeper(ShopKeeping.Local, progress.LoadPilot(), progress.SavePilot);
        }
    }


    /// <summary>
    /// The Supply Room as a ledger of the shop window: its items on the pages Weapons and Gear (as the room's own tabs
    /// split them, <see cref="StrikeUI.CategoryOf"/>), bought and sold for a strike pilot by the armory's rules
    /// (<see cref="StrikeArmory"/>). The wallet mirrors the pilot's money and the inventory what the pilot owns (the
    /// energy module: the energy), so the window draws them like any shop; every purchase and sale goes to the pilot
    /// and is saved where the pilot is kept.
    /// </summary>
    public sealed class PilotLedger : ShopLedger
    {
        public const string WeaponsPage = "Weapons";
        public const string GearPage = "Gear";

        private static StrikeUI strikeUI;

        private readonly StrikeLoadout pilot;
        private readonly Action<StrikeLoadout> save;
        private readonly Wallet wallet = new Wallet(0, StrikeRules.WalletCap);
        private readonly Inventory inventory = new Inventory();

        /// <param name="save">Saves the pilot after a purchase or a sale (null keeps it in memory only).</param>
        public PilotLedger(ShopKeeping keeping, StrikeLoadout pilot, Action<StrikeLoadout> save)
            : base(keeping, new PilotRules(pilot ?? throw new ArgumentNullException(nameof(pilot))))
        {
            this.pilot = pilot;
            this.save = save;
            IconOf = item => ItemSprite(ItemOf(item));
            pilot.Changed += Mirror;
            Mirror();
        }

        public StrikeLoadout Pilot => pilot;

        public override Wallet Wallet => wallet;

        public override Inventory Inventory => inventory;


        /// <summary>The item of the Supply Room behind a shop item of this ledger.</summary>
        public static StrikeItem ItemOf(ShopItem item)
        {
            return item?.Tag is StrikeItem strike ? strike : StrikeItem.MachineGun;
        }


        /// <summary>
        /// The Supply Room's catalog: everything for sale, the weapons then the gear, each page by price as the room lists
        /// them. An item's id is its enum name, its price and resale the armory's; the energy module is metered by the
        /// point up to full energy, an item the pilot carries one of is owned, the rest are counted up to their cap.
        /// </summary>
        public static ShopCatalog BuildCatalog()
        {
            var catalog = new ShopCatalog();
            foreach (string page in new[] { WeaponsPage, GearPage })
            {
                foreach (StrikeItemInfo info in StrikeArmory.ForSale())
                {
                    if (PageOf(info.Item) != page)
                    {
                        continue;
                    }
                    bool energy = info.Item == StrikeItem.EnergyModule;
                    catalog.Add(new ShopItem
                    {
                        Id = info.Item.ToString(),
                        Title = info.Title,
                        Description = info.Description,
                        Category = page,
                        Kind = energy ? ItemKind.Metered : info.Max == 1 ? ItemKind.Owned : ItemKind.Counted,
                        Max = energy ? StrikeRules.MaxEnergy : info.Max,
                        Price = info.Price,
                        Resale = info.Price > 0 ? (float)info.Resale / info.Price : 0f,
                        Icon = info.Item.ToString(),
                        Tag = info.Item
                    });
                }
            }
            return catalog;
        }


        /// <summary>The page of the Supply Room that lists <paramref name="item"/>.</summary>
        public static string PageOf(StrikeItem item)
        {
            return StrikeUI.CategoryOf(item) == StrikeUI.SupplyCategory.Weapons ? WeaponsPage : GearPage;
        }


        /// <summary>One press of Buy on the energy module: the points the next module adds (up to 25, never past full).</summary>
        public override int MeteredStep(ShopItem item)
        {
            return Math.Max(1, Math.Min(StrikeArmory.EnergyModuleStep, (int)Math.Ceiling(pilot.EnergyMissing - 0.001f)));
        }


        public override async Task<ShopOutcome> BuyAsync(ShopItem item, int points)
        {
            ShopOutcome outcome = await base.BuyAsync(item, points);
            if (outcome.Bought)
            {
                save?.Invoke(pilot);
            }
            return outcome;
        }


        public override async Task<int> SellAsync(ShopItem item)
        {
            int paid = await base.SellAsync(item);
            if (paid > 0)
            {
                save?.Invoke(pilot);
            }
            return paid;
        }


        /// <summary>Stops following the pilot (a ledger no window shows any more).</summary>
        public void Dispose()
        {
            pilot.Changed -= Mirror;
        }


        /// <summary>Copies the pilot's money and what it owns into the wallet and the inventory the window reads.</summary>
        private void Mirror()
        {
            wallet.Money = Math.Max(0, Math.Min(StrikeRules.WalletCap, pilot.Money));
            inventory.entries.Clear();
            foreach (ShopItem item in Catalog)
            {
                int owned = Rules.Owned(inventory, item);
                if (owned > 0)
                {
                    inventory.entries.Add(new Inventory.Entry { id = item.Id, value = owned });
                }
            }
            RaiseChanged();
        }


        /// <summary>The icon the Supply Room shows for <paramref name="item"/>: the scene's room's (the theme's, else its own sprites), else the theme's alone.</summary>
        private static Sprite ItemSprite(StrikeItem item)
        {
            if (strikeUI == null)
            {
                strikeUI = UnityEngine.Object.FindAnyObjectByType<StrikeUI>(FindObjectsInactive.Include);
            }
            return strikeUI != null ? strikeUI.ItemSprite(item) : AsteroidsUI.Themed(theme => theme.StrikeIcon(item.ToString()), null);
        }


        /// <summary>The armory's rules on the pilot, in the shop's terms: every answer comes from <see cref="StrikeArmory"/>.</summary>
        private sealed class PilotRules : Shop
        {
            private readonly StrikeLoadout pilot;

            public PilotRules(StrikeLoadout pilot) : base(BuildCatalog())
            {
                this.pilot = pilot;
            }


            /// <summary>What the pilot carries of the item; the energy module: the energy.</summary>
            public override int Owned(Inventory inventory, ShopItem item)
            {
                StrikeItem strike = ItemOf(item);
                return strike == StrikeItem.EnergyModule ? (int)Math.Round(pilot.Energy) : pilot.Count(strike);
            }


            /// <summary>What the next purchase costs now (the energy module: the points missing, up to 25); -1 at the cap.</summary>
            public override int NextPrice(Inventory inventory, ShopItem item)
            {
                StrikeItem strike = ItemOf(item);
                return StrikeArmory.CanBuy(pilot, strike) == BuyResult.Full ? -1 : StrikeArmory.BuyPrice(pilot, strike);
            }


            public override EconomyResult CanBuy(Wallet wallet, Inventory inventory, ShopItem item, out string reason)
            {
                EconomyResult result = Translate(StrikeArmory.CanBuy(pilot, ItemOf(item)));
                reason = ShopLedger.ReasonOf(result);
                return result;
            }


            public override EconomyResult Buy(Wallet wallet, Inventory inventory, ShopItem item)
            {
                return Translate(StrikeArmory.Buy(pilot, ItemOf(item)));
            }


            /// <summary>One energy module, whatever step was asked for: the points it added (up to 25), 0 when it was not bought.</summary>
            public override int BuyPoints(Wallet wallet, Inventory inventory, ShopItem item, int points)
            {
                float before = pilot.Energy;
                if (StrikeArmory.Buy(pilot, ItemOf(item)) != BuyResult.Bought)
                {
                    return 0;
                }
                return Math.Max(1, (int)Math.Round(pilot.Energy - before));
            }


            /// <summary>What one energy module costs now, whatever step is asked for (the room sells them whole).</summary>
            public override int MeteredPrice(ShopItem item, int points)
            {
                return StrikeArmory.BuyPrice(pilot, ItemOf(item));
            }


            public override int SellPrice(Inventory inventory, ShopItem item)
            {
                StrikeItem strike = ItemOf(item);
                return StrikeArmory.CanSell(pilot, strike) ? StrikeArmory.SellPrice(pilot, strike) : 0;
            }


            public override bool CanSell(Inventory inventory, ShopItem item, out string reason)
            {
                StrikeItem strike = ItemOf(item);
                if (StrikeArmory.CanSell(pilot, strike))
                {
                    reason = null;
                    return true;
                }
                reason = StrikeArmory.Info(strike).NotForSale ? NotForSaleReason : NothingToSellReason;
                return false;
            }


            /// <summary>Sells one as the room does: only when the wallet takes the whole price. The money paid, or 0.</summary>
            public override int Sell(Wallet wallet, Inventory inventory, ShopItem item)
            {
                StrikeItem strike = ItemOf(item);
                if (!StrikeArmory.CanSell(pilot, strike) || !StrikeArmory.WalletTakes(pilot, strike))
                {
                    return 0;
                }
                int price = StrikeArmory.SellPrice(pilot, strike);
                return StrikeArmory.Sell(pilot, strike) ? price : 0;
            }


            private static EconomyResult Translate(BuyResult result)
            {
                switch (result)
                {
                    case BuyResult.Bought:
                        return EconomyResult.Bought;
                    case BuyResult.NoMoney:
                        return EconomyResult.NoMoney;
                    case BuyResult.Full:
                        return EconomyResult.Full;
                    default:
                        return EconomyResult.NotForSale;
                }
            }
        }
    }
}
