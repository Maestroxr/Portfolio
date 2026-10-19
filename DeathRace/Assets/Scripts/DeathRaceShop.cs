using System;
using System.Threading.Tasks;
using Gamebox;
using Gamebox.Economy;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// Death Race's side of the cross-game shop (<see cref="GameShopDefinition"/>): the garage, the black market and
    /// the dealer of the career being played as pages of the shared window, kept in the career's save
    /// (<see cref="CareerLedger"/>), then the cloud's pages with the two items of the game. The Pit Crew (a perk for
    /// money) makes repairs free; a bundle of cash (for Tokens) goes into the career's wallet, or waits for the next
    /// career when none is open (<see cref="TakePendingCash"/>).
    /// </summary>
    public sealed class DeathRaceShop : GameShopDefinition
    {
        public const string PitCrewId = "DEATHRACE_PIT_CREW";
        public const int PitCrewCents = 100;
        public const string PitCrewProductId = "com.skinnerboxes.deathrace.pitcrew";

        public const string CashId = "DEATHRACE_CASH_5000";
        public const int CashTokens = 120;
        public const int CashAmount = 5000;
        public const int CashMax = 99;

        /// <summary>The progress value that holds cash bought while no career was open.</summary>
        public const string PendingCashKey = "PendingCash";

        public override GameType Game => GameType.DeathRace;

        public override void DefineCloud(CloudCatalog catalog)
        {
            catalog.AddForMoney(Game, PitCrewId, "Pit Crew", "Repairs cost nothing between races", PitCrewCents, PitCrewProductId);
            catalog.AddForTokens(Game, CashId, "$5,000 Cash", "Five thousand dollars into your career's wallet", CashTokens, ItemKind.Counted, CashMax);
        }

        /// <summary>The career's own pages, while a career is being played.</summary>
        public override void DefineGame(GameShop shop, BaseGameManager manager)
        {
            if (manager is DeathRaceManager game && game.HasCareer)
            {
                shop.Add(new CareerLedger(game.CurrentCareer, () => game.Autosave()));
            }
        }

        public override void OnCloudBought(CloudItem item, BaseGameManager manager)
        {
            if (item != null && item.Id == CashId)
            {
                Deliver(manager as DeathRaceManager);
            }
        }

        public override Sprite IconOf(CloudItem item)
        {
            DeathRaceTheme theme = GameThemes.Active<DeathRaceTheme>(GameType.DeathRace);
            if (item == null || theme == null)
            {
                return null;
            }
            switch (item.Id)
            {
                case PitCrewId:
                    return theme.iconRepair;
                case CashId:
                    return theme.iconCash;
                default:
                    return null;
            }
        }

        /// <summary>Whether the account has the Pit Crew (repairs are free).</summary>
        public static bool HasPitCrew => CloudEconomy.Owns(PitCrewId);

        /// <summary>What a point of repair costs for a car of <paramref name="tier"/>: nothing with the Pit Crew.</summary>
        public static int RepairPrice(int tier, bool pitCrew)
        {
            return pitCrew ? 0 : CareerRules.RepairPrice(tier);
        }

        /// <summary>Keeps <paramref name="amount"/> of cash in the progress until a career takes it.</summary>
        public static void AddPendingCash(DeathRaceProgress progress, int amount)
        {
            if (progress != null && amount > 0)
            {
                progress.SetValue(PendingCashKey, progress.Value(PendingCashKey) + amount);
            }
        }

        /// <summary>The cash bought while no career was open, taken out of the progress (zero when none waits).</summary>
        public static int TakePendingCash(DeathRaceProgress progress)
        {
            int cash = progress != null ? progress.Value(PendingCashKey) : 0;
            if (cash > 0)
            {
                progress.SetValue(PendingCashKey, 0);
            }
            return Math.Max(0, cash);
        }

        /// <summary>Takes one cash bundle out of the cloud and pays it into the career, or keeps it for the next one.</summary>
        private static async void Deliver(DeathRaceManager game)
        {
            string error = await CloudEconomy.ConsumeAsync(CashId);
            if (error != null)
            {
                Debug.LogWarning("The cash bundle could not be taken out of the cloud: " + error);
                return;
            }
            if (game != null && game.HasCareer)
            {
                game.CurrentCareer.wallet.Add(CashAmount);
                game.Autosave();
                return;
            }
            AddPendingCash(game != null ? game.Progress : new DeathRaceProgress(new PlayerPrefsStrategy()), CashAmount);
        }
    }

    /// <summary>
    /// The career's garage, black market and dealer as a ledger of the shared shop window: the rules and the catalog of
    /// <see cref="CareerShop"/> over the career's wallet and consumables, saved after every purchase. Sabotage stays in
    /// the garage window, which has the picker of a rival; a new car makes the catalog over (its own upgrades), which the
    /// window follows (<see cref="Stale"/>).
    /// </summary>
    public sealed class CareerLedger : ShopLedger
    {
        private readonly CareerState state;
        private readonly Action save;

        /// <param name="save">Saves the career after a purchase; null in a test.</param>
        public CareerLedger(CareerState state, Action save = null) : base(ShopKeeping.Local, new CareerShop(state))
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.save = save;
            state.wallet.Changed += RaiseChanged;
            state.consumables.Changed += RaiseChanged;
        }

        /// <summary>The career's shop, as built for the car being driven.</summary>
        public CareerShop Career => (CareerShop)Rules;

        public override Wallet Wallet => state.wallet;

        public override Inventory Inventory => state.consumables;

        public override string Caption => "Kept in your career";

        public override string PageTitle(string page)
        {
            switch (page)
            {
                case CareerShop.GaragePage:
                    return "Garage";
                case CareerShop.MarketPage:
                    return "Black Market";
                case CareerShop.DealerPage:
                    return "Dealer";
                default:
                    return page;
            }
        }

        public override bool Shows(ShopItem item)
        {
            return item != null && item.Id != CareerShop.SabotageId;
        }

        public override bool Stale => Career.IsStale;

        public override async Task<ShopOutcome> BuyAsync(ShopItem item, int points)
        {
            ShopOutcome outcome = await base.BuyAsync(item, points);
            if (outcome.Bought)
            {
                Settle();
            }
            return outcome;
        }

        public override async Task<int> SellAsync(ShopItem item)
        {
            int paid = await base.SellAsync(item);
            if (paid > 0)
            {
                Settle();
            }
            return paid;
        }

        /// <summary>After a purchase or a sale: a catalog that went stale is built again, and the career is saved.</summary>
        private void Settle()
        {
            if (Career.IsStale)
            {
                Rules = new CareerShop(state);
            }
            save?.Invoke();
            RaiseChanged();
        }
    }
}
