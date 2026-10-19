using System.Collections.Generic;
using Gamebox.Economy;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// The career's shop on BaseGame's economy. The garage: the engine, tyres, armour, nitro system and gun mount
    /// upgrades of the current car and repairs by the health point. The workshop: the seven fittings and the guns (a gun
    /// bought is mounted on the bonnet; a gun the car has can be mounted again for nothing, the machine gun always). The
    /// black market: mines, rockets, nitro, the three special weapons and sabotage of a rival of the day. The dealer: the
    /// cars of the league's tiers (buying one trades the current car in, with everything on it). The upgrades, fittings,
    /// guns, repairs and cars live on the career's car rather than in an inventory, so <see cref="Owned"/> and
    /// <see cref="Grant"/> read and write the <see cref="CareerState"/>; the mines, rockets, nitro and special weapons
    /// are the career's inventory (<see cref="CareerState.consumables"/>).
    ///
    /// The catalog is made for the car being driven (its tier sets the prices, its model the levels) and the league (its
    /// dealer); after buying a car or armour the shop is <see cref="IsStale"/> and a new one is made.
    /// </summary>
    public sealed class CareerShop : Shop
    {
        /// <summary>The pages (catalog categories) of the garage window.</summary>
        public const string GaragePage = "garage";
        public const string WorkshopPage = "workshop";
        public const string MarketPage = "market";
        public const string DealerPage = "dealer";

        public const string EngineId = "engine";
        public const string TiresId = "tires";
        public const string ArmorId = "armor";
        public const string BoostId = "boost";
        public const string GunMountId = "gunmount";
        public const string SpikesId = "spikes";
        public const string PiercingId = "piercing";
        public const string RepairId = "repair";
        public const string MineId = "mine";
        public const string RocketId = "rocket";
        public const string NitroId = "nitro";
        public const string SabotageId = "sabotage";
        public const string CarPrefix = "car.";

        public const string NoTargetReason = "Pick a driver to sabotage";
        public const string SabotagedReason = "A car is sabotaged already";
        public const string BetterCarReason = "Your car is better";
        public const string YoursReason = "You drive it already";
        public const string RepairedReason = "The car is in one piece";
        public const string MountedReason = "It is on the bonnet already";
        public const string FittedReason = "It is fitted already";

        private readonly CarModel builtFor;
        private readonly int builtArmor;

        public CareerShop(CareerState state) : base(BuildCatalog(state))
        {
            State = state;
            builtFor = state.car;
            builtArmor = state.Current != null ? state.Current.loadout.armor : 0;
        }

        public CareerState State { get; }

        /// <summary>The rival the sabotage on the shelf is aimed at (picked in the market), or null.</summary>
        public string SabotageTarget { get; set; }

        /// <summary>The car or its armour (and so its health) changed since the catalog was made: make a new shop.</summary>
        public bool IsStale => State.car != builtFor || (State.Current != null ? State.Current.loadout.armor : 0) != builtArmor;

        private CarLoadout Car => State.Current?.loadout;

        public static string CarId(CarModel model)
        {
            return CarPrefix + model.ToString().ToLowerInvariant();
        }

        public static string UpgradeId(UpgradeKind kind)
        {
            switch (kind)
            {
                case UpgradeKind.Engine: return EngineId;
                case UpgradeKind.Tires: return TiresId;
                case UpgradeKind.Armor: return ArmorId;
                case UpgradeKind.Boost: return BoostId;
                default: return GunMountId;
            }
        }

        /// <summary>The shop's id of a fitting (the ids of the first two are the ones saves have always had).</summary>
        public static string FittingId(EquipmentKind kind)
        {
            return Equipment.Of(kind).Id;
        }

        public static string GunId(GunKind kind)
        {
            return Guns.Of(kind).Id;
        }

        public static string SpecialId(SpecialKind kind)
        {
            return Specials.Of(kind).Id;
        }

        // ------------------------------------------------------------------------------------------------
        // The catalog
        // ------------------------------------------------------------------------------------------------

        private static ShopCatalog BuildCatalog(CareerState state)
        {
            OwnedCar owned = state.Current;
            CarModel model = owned != null ? owned.Model : state.car;
            CarSpec spec = CarSpecs.Of(model);
            int tier = spec.Tier;
            var catalog = new ShopCatalog();
            catalog.Add(Upgrade(UpgradeKind.Engine, spec, "Engine", "More power: a higher top speed and harder acceleration."));
            catalog.Add(Upgrade(UpgradeKind.Tires, spec, "Tires", "More grip: tighter corners and less sliding."));
            catalog.Add(Upgrade(UpgradeKind.Armor, spec, "Armor", "Thicker plating: every hit hurts less and the car takes more."));
            catalog.Add(Upgrade(UpgradeKind.Boost, spec, "Nitro System", "Bigger injectors: every nitro charge burns longer and pushes harder."));
            catalog.Add(Upgrade(UpgradeKind.GunMount, spec, "Gun Mount",
                "A heavier mount and faster feed: the gun fires faster, hits harder and carries more rounds."));
            float maxHealth = owned != null ? owned.loadout.Stats().MaxHealth : spec.MaxHealth;
            catalog.Add(new ShopItem
            {
                Id = RepairId, Title = "Repair", Description = "Panel beating and welding, paid by the point of health.",
                Category = GaragePage, Kind = ItemKind.Metered, Max = Mathf.Max(1, Mathf.CeilToInt(maxHealth - 0.001f)),
                Price = DeathRaceShop.RepairPrice(tier, DeathRaceShop.HasPitCrew), Sellable = false, Icon = "repair"
            });
            foreach (EquipmentKind kind in CareerRules.Fittings)
            {
                EquipmentSpec fitting = Equipment.Of(kind);
                catalog.Add(new ShopItem
                {
                    Id = fitting.Id, Title = fitting.Name, Description = fitting.Blurb, Category = WorkshopPage, Kind = ItemKind.Owned, Max = 1,
                    Price = CareerRules.FittingPrice(kind, tier), Sellable = false, Icon = fitting.Icon, Tag = kind
                });
            }
            foreach (GunSpec gun in Guns.All)
            {
                catalog.Add(new ShopItem
                {
                    Id = gun.Id, Title = gun.Name, Description = gun.Blurb, Category = WorkshopPage, Kind = ItemKind.Owned, Max = 1,
                    Price = CareerRules.GunPrice(gun.Kind, tier), Sellable = false, Icon = gun.Icon, Tag = gun.Kind
                });
            }
            catalog.Add(Consumable(MineId, "Mine", "Dropped behind the car; it arms in a moment and blows up whoever drives over it.",
                CareerRules.MinePrice, CareerRules.MineMax));
            catalog.Add(Consumable(RocketId, "Rocket", "Fired ahead; it finds the nearest car in front and hits hard.",
                CareerRules.RocketPrice, CareerRules.RocketMax));
            catalog.Add(Consumable(NitroId, "Nitro", "A bottle of boost: a few seconds of much more speed.",
                CareerRules.NitroPrice, CareerRules.NitroMax));
            foreach (SpecialSpec special in Specials.All)
            {
                ShopItem item = Consumable(special.Id, special.Name, special.Blurb, special.Price, special.Max);
                item.Icon = special.Icon;
                item.Tag = special.Kind;
                catalog.Add(item);
            }
            catalog.Add(new ShopItem
            {
                Id = SabotageId, Title = "Sabotage", Description = "A word with a mechanic: the driver you pick starts their next race badly hurt.",
                Category = MarketPage, Kind = ItemKind.Owned, Max = 1, PriceOf = _ => 0, Sellable = false, Icon = "sabotage"
            });
            LeagueSpec league = state.League;
            foreach (CarSpec car in CarSpecs.All)
            {
                if (!league.Sells(car.Model) && car.Model != model)
                {
                    continue;
                }
                catalog.Add(new ShopItem
                {
                    Id = CarId(car.Model), Title = car.Name, Description = car.Blurb, Category = DealerPage, Kind = ItemKind.Owned, Max = 1,
                    Price = car.Price, Sellable = false, Icon = "car", Tag = car.Model
                });
            }
            return catalog;
        }

        private static ShopItem Upgrade(UpgradeKind kind, CarSpec spec, string title, string description)
        {
            int tier = spec.Tier;
            return new ShopItem
            {
                Id = UpgradeId(kind), Title = title, Description = description, Category = GaragePage, Kind = ItemKind.Leveled,
                Max = spec.MaxLevelOf(kind), PriceOf = level => CareerRules.UpgradePrice(kind, tier, level), Sellable = false,
                Icon = UpgradeId(kind), Tag = kind
            };
        }

        private static ShopItem Consumable(string id, string title, string description, int price, int max)
        {
            return new ShopItem
            {
                Id = id, Title = title, Description = description, Category = MarketPage, Kind = ItemKind.Counted, Max = max, Price = price,
                Resale = CareerRules.MarketResale, Icon = id
            };
        }

        // ------------------------------------------------------------------------------------------------
        // The rules, bent to the career
        // ------------------------------------------------------------------------------------------------

        /// <summary>
        /// What the career has of <paramref name="item"/>: an upgrade's level, a fitting (1 when fitted), a gun (1 while it
        /// is the one mounted), the repairs (the health points the car has), the sabotage of the day, the car driven, else
        /// the count of the inventory.
        /// </summary>
        public override int Owned(Inventory inventory, ShopItem item)
        {
            CarLoadout car = Car;
            if (item == null)
            {
                return 0;
            }
            switch (item.Tag)
            {
                case UpgradeKind kind:
                    return car != null ? car.Level(kind) : 0;
                case EquipmentKind fitting:
                    return car != null && car.Has(fitting) ? 1 : 0;
                case GunKind gun:
                    return car != null && car.gun == gun && car.OwnsGun(gun) ? 1 : 0;
                case CarModel model:
                    return State.car == model ? 1 : 0;
            }
            switch (item.Id)
            {
                case RepairId: return car != null ? Mathf.Max(0, item.Max - Career.MissingPoints(car)) : item.Max;
                case SabotageId: return State.HasSabotage ? 1 : 0;
            }
            return base.Owned(inventory ?? State.consumables, item);
        }

        /// <summary>
        /// A car costs its price less what the dealer gives for the current one; a gun the car has costs nothing to mount
        /// again; sabotage costs by the driver picked.
        /// </summary>
        public override int NextPrice(Inventory inventory, ShopItem item)
        {
            if (item != null && item.Tag is CarModel model)
            {
                return Owned(inventory, item) > 0 ? -1 : Mathf.Max(0, CarSpecs.Of(model).Price - TradeInValue);
            }
            if (item != null && item.Tag is GunKind gun)
            {
                return Owned(inventory, item) > 0 ? -1 : Car != null && Car.OwnsGun(gun) ? 0 : item.Cost(1);
            }
            if (item != null && item.Id == SabotageId)
            {
                return State.HasSabotage ? -1 : SabotagePrice(SabotageTarget);
            }
            return base.NextPrice(inventory, item);
        }

        /// <summary>The career's own words for what cannot be bought (an owned car, a gun mounted, a car in one piece, sabotage done).</summary>
        public override BuyResult CanBuy(Wallet wallet, Inventory inventory, ShopItem item, out string reason)
        {
            BuyResult result = base.CanBuy(wallet ?? State.wallet, inventory ?? State.consumables, item, out reason);
            if (result == BuyResult.Full && item != null)
            {
                if (item.Tag is CarModel)
                {
                    reason = YoursReason;
                }
                else if (item.Tag is GunKind)
                {
                    reason = MountedReason;
                }
                else if (item.Tag is EquipmentKind)
                {
                    reason = FittedReason;
                }
                else if (item.Id == RepairId)
                {
                    reason = RepairedReason;
                }
                else if (item.Id == SabotageId)
                {
                    reason = SabotagedReason;
                }
            }
            return result;
        }

        /// <summary>A car of a lower tier than the one driven is locked (another of the same tier is not); sabotage needs a target.</summary>
        protected override bool IsLocked(Inventory inventory, ShopItem item, out string reason)
        {
            reason = null;
            if (item.Tag is CarModel model && model != State.car && CarSpecs.Of(model).Tier < CarSpecs.Of(State.car).Tier)
            {
                reason = BetterCarReason;
                return true;
            }
            if (item.Id == SabotageId && !State.HasSabotage && SabotagePrice(SabotageTarget) < 0)
            {
                reason = NoTargetReason;
                return true;
            }
            return false;
        }

        protected override void Grant(Inventory inventory, ShopItem item, int amount)
        {
            CarLoadout car = Car;
            switch (item.Tag)
            {
                case UpgradeKind kind:
                    car?.SetLevel(kind, car.Level(kind) + amount);
                    return;
                case EquipmentKind fitting:
                    car?.Set(fitting, amount > 0);
                    return;
                case GunKind gun:
                    if (car != null && amount > 0)
                    {
                        car.GiveGun(gun);
                    }
                    return;
                case CarModel model:
                    TradeTo(model);
                    return;
            }
            switch (item.Id)
            {
                case RepairId:
                    if (car != null)
                    {
                        int points = Owned(inventory, item) + amount;
                        car.health = points >= item.Max ? 1f : Mathf.Clamp01(points / car.Stats().MaxHealth);
                    }
                    return;
                case SabotageId:
                    State.sabotage = amount > 0 ? SabotageTarget ?? "" : "";
                    return;
            }
            base.Grant(inventory ?? State.consumables, item, amount);
        }

        /// <summary>The dealer takes the current car (and everything on it) and hands over a new <paramref name="model"/>, in one piece, without upgrades.</summary>
        private void TradeTo(CarModel model)
        {
            State.garage.Clear();
            State.garage.Add(OwnedCar.New(model));
            State.car = model;
        }

        // ------------------------------------------------------------------------------------------------
        // What the windows ask
        // ------------------------------------------------------------------------------------------------

        /// <summary>What the dealer gives for the car being driven.</summary>
        public int TradeInValue => CareerRules.TradeIn(Car);

        /// <summary>Whether the car has the gun <paramref name="kind"/> (bought, mounted or not).</summary>
        public bool HasGun(GunKind kind)
        {
            return Car != null && Car.OwnsGun(kind);
        }

        /// <summary>The special weapon the car fires first (the player's choice in the market).</summary>
        public SpecialKind Armed => State.armed;

        /// <summary>Makes <paramref name="kind"/> the special weapon the car fires first.</summary>
        public void Arm(SpecialKind kind)
        {
            State.armed = (SpecialKind)Mathf.Clamp((int)kind, 0, Specials.Count - 1);
        }

        /// <summary>
        /// What sabotaging <paramref name="id"/> costs: by the race they are offered in today (the champion: the Showdown's
        /// price), else by their third of the ladder; -1 for nobody (or a driver not racing today).
        /// </summary>
        public int SabotagePrice(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return -1;
            }
            foreach (RaceOffer offer in State.offers)
            {
                if (offer.rivals.Contains(id))
                {
                    return CareerRules.SabotagePrice(offer.difficulty);
                }
            }
            return -1;
        }

        /// <summary>The drivers that can be sabotaged: everyone racing today, in the order of the offers.</summary>
        public List<string> SabotageTargets()
        {
            var targets = new List<string>();
            foreach (RaceOffer offer in State.offers)
            {
                foreach (string id in offer.rivals)
                {
                    if (!targets.Contains(id))
                    {
                        targets.Add(id);
                    }
                }
            }
            return targets;
        }

        /// <summary>Mends the car fully, or as far as the money goes; returns the points bought.</summary>
        public int RepairAll()
        {
            ShopItem item = Catalog[RepairId];
            return BuyPoints(State.wallet, State.consumables, item, Career.MissingPoints(Car));
        }

        /// <summary>Mends half of the damage (rounded up), or as far as the money goes.</summary>
        public int RepairHalf()
        {
            ShopItem item = Catalog[RepairId];
            return BuyPoints(State.wallet, State.consumables, item, (Career.MissingPoints(Car) + 1) / 2);
        }

        /// <summary>Buys one more of <paramref name="id"/> with the career's money.</summary>
        public BuyResult Buy(string id)
        {
            return Buy(State.wallet, State.consumables, Catalog[id]);
        }

        /// <summary>Sabotages <paramref name="target"/> (a driver racing today).</summary>
        public BuyResult Sabotage(string target)
        {
            SabotageTarget = target;
            return Buy(SabotageId);
        }
    }
}
