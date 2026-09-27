using System;
using System.Collections.Generic;

namespace Portfolio.Asteroids
{
    /// <summary>What the Supply Room says about one item (design section 1.2).</summary>
    public struct StrikeItemInfo
    {
        public StrikeItem Item;
        public string Title;
        public string Description;
        public ItemGroup Group;
        /// <summary>Price of one (the energy module: of one point of energy).</summary>
        public int Price;
        /// <summary>What selling one pays, and what a pickup past the cap pays instead: half the price.</summary>
        public int Resale;
        /// <summary>The most a pilot can own; 0 for the energy module, whose limit is full energy.</summary>
        public int Max;
        /// <summary>The Supply Room never sells it (the machine gun, the cut power disrupter).</summary>
        public bool NotForSale;

        /// <summary>Whether a level may place it as a pickup (priced at or below <see cref="StrikeRules.MaxPickupPrice"/>).</summary>
        public bool CanBePickup => !NotForSale && Item != StrikeItem.MachineGun && Price <= StrikeRules.MaxPickupPrice;
    }


    /// <summary>
    /// The catalog of the Supply Room: every <see cref="StrikeItem"/> with its title, price, resale and cap, and the shop's
    /// rules for buying and selling against a pilot's <see cref="StrikeLoadout"/>.
    /// </summary>
    public static class StrikeArmory
    {
        /// <summary>Energy an energy module adds per purchase (at most up to full energy).</summary>
        public const int EnergyModuleStep = 25;

        /// <summary>Energy sold at once, and what it pays (only while 25 stays, see <see cref="CanSellEnergy"/>).</summary>
        public const int EnergySellAmount = 25;
        public const int EnergySellPrice = 5000;

        private static StrikeItemInfo[] catalog;

        /// <summary>Every item in enum order.</summary>
        public static IReadOnlyList<StrikeItemInfo> All => catalog ??= Build();

        /// <summary>The Supply Room's entry of <paramref name="item"/>.</summary>
        public static StrikeItemInfo Info(StrikeItem item)
        {
            IReadOnlyList<StrikeItemInfo> all = All;
            int index = (int)item;
            return index >= 0 && index < all.Count ? all[index] : new StrikeItemInfo { Item = item, Title = item.ToString(), NotForSale = true };
        }

        public static string Title(StrikeItem item)
        {
            return Info(item).Title;
        }

        public static int Price(StrikeItem item)
        {
            return Info(item).Price;
        }

        public static int Resale(StrikeItem item)
        {
            return Info(item).Resale;
        }

        /// <summary>The items of <paramref name="groups"/> (all when empty) sorted by price, ties in enum order.</summary>
        public static List<StrikeItemInfo> Sorted(params ItemGroup[] groups)
        {
            var items = new List<StrikeItemInfo>();
            foreach (StrikeItemInfo info in All)
            {
                if (groups == null || groups.Length == 0 || Array.IndexOf(groups, info.Group) >= 0)
                {
                    items.Add(info);
                }
            }
            items.Sort((a, b) => a.Price != b.Price ? a.Price.CompareTo(b.Price) : ((int)a.Item).CompareTo((int)b.Item));
            return items;
        }

        // ------------------------------------------------------------------ the shop's rules

        /// <summary>The items the Supply Room offers in <paramref name="groups"/> (all when empty): <see cref="Sorted"/> without what is not for sale.</summary>
        public static List<StrikeItemInfo> ForSale(params ItemGroup[] groups)
        {
            List<StrikeItemInfo> items = Sorted(groups);
            items.RemoveAll(info => info.NotForSale);
            return items;
        }


        /// <summary>
        /// What buying <paramref name="item"/> would come to for <paramref name="pilot"/>, without buying it: NotForSale for
        /// the machine gun and the power disrupter, Full at the item's cap (the energy module: at full energy), NoMoney when
        /// the wallet is short, else Bought.
        /// </summary>
        public static BuyResult CanBuy(StrikeLoadout pilot, StrikeItem item)
        {
            StrikeItemInfo info = Info(item);
            if (pilot == null || info.NotForSale)
            {
                return BuyResult.NotForSale;
            }
            if (item == StrikeItem.EnergyModule ? EnergyToBuy(pilot) <= 0f : pilot.Count(item) >= info.Max)
            {
                return BuyResult.Full;
            }
            return pilot.Money >= BuyPrice(pilot, item) ? BuyResult.Bought : BuyResult.NoMoney;
        }


        /// <summary>
        /// Buys one <paramref name="item"/> for <paramref name="pilot"/> (the loadout raises Changed when bought): a weapon
        /// adds a copy (the first special is selected), a phase shield or a megabomb one more, the energy module up to 25
        /// energy at 400 a point.
        /// </summary>
        public static BuyResult Buy(StrikeLoadout pilot, StrikeItem item)
        {
            BuyResult result = CanBuy(pilot, item);
            if (result != BuyResult.Bought)
            {
                return result;
            }
            pilot.Money -= BuyPrice(pilot, item);
            if (item == StrikeItem.EnergyModule)
            {
                pilot.Energy = Math.Min(StrikeRules.MaxEnergy, pilot.Energy + EnergyToBuy(pilot));
            }
            else
            {
                pilot.AddItem(item);
            }
            pilot.NotifyChanged();
            return BuyResult.Bought;
        }


        /// <summary>
        /// Whether <paramref name="pilot"/> can sell one <paramref name="item"/>: owned and for sale; a phase shield only
        /// when an undamaged one is left (a spare, or the one in use at full points); energy only while the launch minimum
        /// stays (<see cref="CanSellEnergy"/>).
        /// </summary>
        public static bool CanSell(StrikeLoadout pilot, StrikeItem item)
        {
            if (pilot == null || Info(item).NotForSale)
            {
                return false;
            }
            switch (item)
            {
                case StrikeItem.EnergyModule:
                    return CanSellEnergy(pilot);
                case StrikeItem.PhaseShield:
                    return pilot.PhaseShields > 1 || pilot.PhaseShields == 1 && pilot.ShieldPoints >= StrikeRules.PhaseShieldPoints;
                default:
                    return pilot.Count(item) > 0;
            }
        }


        /// <summary>
        /// Whether <paramref name="pilot"/> can sell 25 energy: only while at least <see cref="StrikeRules.MinLaunchEnergy"/>
        /// stays (50 or more), as a launch tops anything less up to 25 for free.
        /// </summary>
        public static bool CanSellEnergy(StrikeLoadout pilot)
        {
            return pilot != null && pilot.Energy - EnergySellAmount >= StrikeRules.MinLaunchEnergy;
        }


        /// <summary>
        /// Whether the wallet of <paramref name="pilot"/> takes the whole of what selling <paramref name="item"/> pays (the
        /// wallet is capped at <see cref="StrikeRules.WalletCap"/>; the Supply Room refuses a sale it would cut short).
        /// </summary>
        public static bool WalletTakes(StrikeLoadout pilot, StrikeItem item)
        {
            return pilot != null && (long)pilot.Money + SellPrice(pilot, item) <= StrikeRules.WalletCap;
        }


        /// <summary>
        /// Sells one <paramref name="item"/> of <paramref name="pilot"/> for its resale (energy: 25 for 5,000). A special
        /// sold out of the selection hands it to the next owned special. False when it cannot be sold.
        /// </summary>
        public static bool Sell(StrikeLoadout pilot, StrikeItem item)
        {
            if (!CanSell(pilot, item))
            {
                return false;
            }
            pilot.Money = StrikeRules.AddToWallet(pilot.Money, SellPrice(pilot, item));
            switch (item)
            {
                case StrikeItem.EnergyModule:
                    pilot.Energy -= EnergySellAmount;
                    break;
                case StrikeItem.PhaseShield:
                    // A spare goes first; the last one (only sold whole) takes its points with it.
                    pilot.PhaseShields--;
                    if (pilot.PhaseShields == 0)
                    {
                        pilot.ShieldPoints = 0f;
                    }
                    break;
                default:
                    pilot.RemoveItem(item);
                    break;
            }
            pilot.NotifyChanged();
            return true;
        }


        /// <summary>What one purchase of <paramref name="item"/> costs <paramref name="pilot"/> now (the energy module depends on the energy).</summary>
        public static int BuyPrice(StrikeLoadout pilot, StrikeItem item)
        {
            if (item == StrikeItem.EnergyModule)
            {
                // 400 a point for the points missing, at most 25 (a fraction of a point is charged in full).
                float points = pilot != null ? EnergyToBuy(pilot) : EnergyModuleStep;
                return (int)Math.Ceiling(points * Price(item) - 0.001f);
            }
            return Price(item);
        }


        /// <summary>What selling one <paramref name="item"/> pays <paramref name="pilot"/> now.</summary>
        public static int SellPrice(StrikeLoadout pilot, StrikeItem item)
        {
            return item == StrikeItem.EnergyModule ? EnergySellPrice : Resale(item);
        }


        /// <summary>The energy one energy module would add to <paramref name="pilot"/>: up to 25, never past full.</summary>
        private static float EnergyToBuy(StrikeLoadout pilot)
        {
            float missing = StrikeRules.MaxEnergy - pilot.Energy;
            return missing < 0.001f ? 0f : Math.Min(EnergyModuleStep, missing);
        }

        private static StrikeItemInfo[] Build()
        {
            return new[]
            {
                Item(StrikeItem.MachineGun, "MG21 Reaver Machine Gun", "Twin wing guns. Every pilot flies with them and never loses them.", 12000, 1, true),
                Item(StrikeItem.PlasmaCannon, "Firestorm Plasma Cannon", "A heavy centre bolt that burns through aircraft. Always on. Air only.", 78800, 1),
                Item(StrikeItem.MicroMissiles, "Micro-Missile Launcher", "Pairs of small fast missiles at air and ground targets. Always on.", 175600, 1),
                Item(StrikeItem.Dumbfire, "DM1R Psycho Dumbfire", "Missiles drop to the sides, then ignite and streak up. Air and ground.", 145200, 20),
                Item(StrikeItem.MiniGun, "TH19 Thor Auto-Track Mini-Gun", "A rotary gun that tracks a random target on either layer.", 250650, 20),
                Item(StrikeItem.LaserTurret, "OD55 Odin Laser Turret", "Zaps a random aircraft on screen. Never misses. Air only.", 512850, 20),
                Item(StrikeItem.MissilePods, "AARL-1201 Missile Pods", "Very fast missile pairs that shred aircraft. Air only.", 204950, 20),
                Item(StrikeItem.AirMissiles, "AIM-31 Mauler Air/Air Missiles", "Pairs of missiles for aircraft. Air only.", 63500, 20),
                Item(StrikeItem.GroundMissiles, "AGM-26L Banshee Air/Ground Missiles", "Slow heavy missiles that crack bunkers. Ground only.", 110000, 20),
                Item(StrikeItem.Bombs, "MK-133 Bombs", "Bombs that burst on the ground ahead of the ship. Ground only.", 98200, 20),
                Item(StrikeItem.PowerDisrupter, "Power Disrupter", "Withdrawn from service.", 0, 0, true),
                Item(StrikeItem.PulseCannon, "RX1 Tsunami Pulse Cannon", "A wide shockwave that hits air and ground alike.", 725000, 20),
                Item(StrikeItem.Deathray, "MSIL Atlas Deathray", "A beam from the nose to the first enemy in its path.", 950000, 20),
                Item(StrikeItem.TwinLaser, "CAL-10 Eclipse Twin Laser", "Two beams from the wing tips. The best money can buy.", 1750000, 20),
                Item(StrikeItem.MegaBomb, "CBU-80 Guillotine Megabomb", "Clears every enemy shot and hits everything on screen.", 32250, 5),
                Item(StrikeItem.EnergyModule, "Energy Module", "Restores up to 25 energy. Priced per point.", 400, 0),
                Item(StrikeItem.PhaseShield, "SA17 Ares Phase Shield", "Absorbs 100 damage before the energy is touched.", 78500, 5),
                Item(StrikeItem.IonScanner, "Ion Scanner", "Shows the health of the boss.", 10000, 1)
            };
        }

        private static StrikeItemInfo Item(StrikeItem item, string title, string description, int price, int max, bool notForSale = false)
        {
            return new StrikeItemInfo
            {
                Item = item,
                Title = title,
                Description = description,
                Group = StrikeWeaponRules.Group(item),
                Price = price,
                Resale = price / 2,
                Max = max,
                NotForSale = notForSale
            };
        }
    }


    /// <summary>
    /// A pilot of the strike campaign: money, energy, phase shields, megabombs, the items owned, the selected special and
    /// the difficulty. The saved pilot lives in the progress; a mission flies a working copy (<see cref="Clone"/>) that
    /// replaces the saved one only when the mission is won and the ship lands. The loadout is the source of truth for the
    /// ship in strike: the ship mirrors it after every <see cref="Changed"/>.
    /// </summary>
    public class StrikeLoadout
    {
        private readonly int[] counts = new int[(int)StrikeItem.IonScanner + 1];

        /// <summary>Anything of the loadout changed (raised by the rules after a change, and by <see cref="NotifyChanged"/>).</summary>
        public event Action Changed;

        /// <summary>Money in the wallet (at most <see cref="StrikeRules.WalletCap"/>).</summary>
        public int Money { get; set; }

        /// <summary>Energy, 0 to 100: the ship's hull. 0 destroys the ship.</summary>
        public float Energy { get; set; }

        /// <summary>Phase shields owned, the one in use included (0 to 5).</summary>
        public int PhaseShields { get; set; }

        /// <summary>Points left in the phase shield in use (0 to 100); the others are whole.</summary>
        public float ShieldPoints { get; set; }

        public int Megabombs { get; set; }

        /// <summary>The selected special weapon; <see cref="StrikeItem.MachineGun"/> when none is owned.</summary>
        public StrikeItem Special { get; set; }

        public StrikeDifficulty Difficulty { get; set; } = StrikeRules.NewPilotDifficulty;

        /// <summary>Whether the pilot has the Ion Scanner (the boss bar shows its health).</summary>
        public bool HasScanner => Count(StrikeItem.IonScanner) > 0;

        /// <summary>Whether a special weapon is selected.</summary>
        public bool HasSpecial => Special != StrikeItem.MachineGun && Count(Special) > 0;


        /// <summary>A new pilot: 10,000 money, the machine gun, 75 energy, Veteran, nothing else.</summary>
        public static StrikeLoadout NewPilot()
        {
            var pilot = new StrikeLoadout
            {
                Money = StrikeRules.NewPilotMoney,
                Energy = StrikeRules.NewPilotEnergy,
                Difficulty = StrikeRules.NewPilotDifficulty,
                Special = StrikeItem.MachineGun
            };
            pilot.SetCount(StrikeItem.MachineGun, 1);
            return pilot;
        }


        /// <summary>How many of <paramref name="item"/> the pilot owns (weapons: copies; megabombs and shields: their counts).</summary>
        public int Count(StrikeItem item)
        {
            switch (item)
            {
                case StrikeItem.MegaBomb: return Megabombs;
                case StrikeItem.PhaseShield: return PhaseShields;
                case StrikeItem.EnergyModule: return 0;
                default:
                    int index = (int)item;
                    return index >= 0 && index < counts.Length ? counts[index] : 0;
            }
        }


        /// <summary>Sets the count of <paramref name="item"/> directly, without rules or <see cref="Changed"/> (loading, tests).</summary>
        public void SetCount(StrikeItem item, int count)
        {
            switch (item)
            {
                case StrikeItem.MegaBomb:
                    Megabombs = Math.Max(0, count);
                    return;
                case StrikeItem.PhaseShield:
                    PhaseShields = Math.Max(0, count);
                    return;
                case StrikeItem.EnergyModule:
                    return;
            }
            int index = (int)item;
            if (index >= 0 && index < counts.Length)
            {
                counts[index] = Math.Max(0, count);
            }
        }


        public bool Owns(StrikeItem item)
        {
            return Count(item) > 0;
        }


        /// <summary>The special weapons owned, in cycling (enum) order.</summary>
        public List<StrikeItem> OwnedSpecials()
        {
            var specials = new List<StrikeItem>();
            for (var item = StrikeWeaponRules.FirstSpecial; item <= StrikeWeaponRules.LastSpecial; item++)
            {
                if (StrikeWeaponRules.IsSpecial(item) && Count(item) > 0)
                {
                    specials.Add(item);
                }
            }
            return specials;
        }


        /// <summary>A copy for a mission to fly with (no listeners).</summary>
        public StrikeLoadout Clone()
        {
            var copy = new StrikeLoadout();
            copy.CopyFrom(this);
            return copy;
        }


        /// <summary>Takes over every value of <paramref name="other"/> (the listeners stay); raises <see cref="Changed"/>.</summary>
        public void CopyFrom(StrikeLoadout other)
        {
            if (other == null)
            {
                return;
            }
            Array.Copy(other.counts, counts, counts.Length);
            Money = other.Money;
            Energy = other.Energy;
            PhaseShields = other.PhaseShields;
            ShieldPoints = other.ShieldPoints;
            Megabombs = other.Megabombs;
            Special = other.Special;
            Difficulty = other.Difficulty;
            NotifyChanged();
        }


        /// <summary>Tells the listeners that the loadout changed.</summary>
        public void NotifyChanged()
        {
            Changed?.Invoke();
        }


        // ------------------------------------------------------------------ rules (model)

        /// <summary>Energy missing to full (0 to 100).</summary>
        public float EnergyMissing => Math.Max(0f, StrikeRules.MaxEnergy - Energy);


        /// <summary>
        /// Whether a hit now also destroys a weapon: energy at or below <see cref="StrikeRules.LowEnergy"/> with no phase
        /// shield left (<see cref="LoseWeapon"/> checks it itself).
        /// </summary>
        public bool WeaponsAtRisk => PhaseShields <= 0 && Energy <= StrikeRules.LowEnergy;


        /// <summary>Adds <paramref name="amount"/> money, up to the wallet cap.</summary>
        public void AddMoney(int amount)
        {
            int money = StrikeRules.AddToWallet(Money, amount);
            if (money != Money)
            {
                Money = money;
                NotifyChanged();
            }
        }


        /// <summary>
        /// A pickup of <paramref name="item"/>: a weapon adds a copy (the first special is selected), a megabomb, a phase
        /// shield or the scanner one more, an energy module +25 energy (at full energy a quarter to the phase shield). What
        /// is over its cap pays its resale instead: that money is returned and NOT added to <see cref="Money"/> (mission money
        /// is the score, so the caller pays it through the manager's AddMoney); 0 when the item was added. The machine gun
        /// (no item) and the cut power disrupter do nothing.
        /// </summary>
        public int Collect(StrikeItem item)
        {
            switch (item)
            {
                case StrikeItem.MachineGun:
                case StrikeItem.PowerDisrupter:
                    return 0;
                case StrikeItem.EnergyModule:
                    CollectEnergy(StrikeRules.EnergyPickup);
                    return 0;
            }
            if (Count(item) >= StrikeArmory.Info(item).Max)
            {
                return StrikeArmory.Resale(item);
            }
            AddItem(item);
            NotifyChanged();
            return 0;
        }


        /// <summary>
        /// An energy pickup of <paramref name="amount"/>: energy up to 100; when the energy is already full, a quarter of it
        /// goes to the phase shield in use (up to its 100 points).
        /// </summary>
        public void CollectEnergy(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }
            if (EnergyMissing <= 0f)
            {
                if (PhaseShields > 0 && ShieldPoints < StrikeRules.PhaseShieldPoints)
                {
                    ShieldPoints = Math.Min(StrikeRules.PhaseShieldPoints, ShieldPoints + amount * 0.25f);
                    NotifyChanged();
                }
                return;
            }
            AddEnergy(amount);
        }


        /// <summary>Selects the next owned special in enum order (wrapping); returns the selected one.</summary>
        public StrikeItem CycleSpecial()
        {
            StrikeItem next = NextSpecial(Special, false);
            if (next != Special)
            {
                Special = next;
                NotifyChanged();
            }
            return Special;
        }


        /// <summary>
        /// Applies <paramref name="amount"/> of damage (after the difficulty and hull multipliers): the phase shield in use
        /// first, then the next one is equipped (and takes the rest), then energy. Returns the damage taken (for the damage
        /// star): what the shields and the energy absorbed.
        /// </summary>
        public float AbsorbDamage(float amount)
        {
            if (amount <= 0f)
            {
                return 0f;
            }
            float taken = 0f;
            while (amount > 0f && PhaseShields > 0)
            {
                float absorbed = Math.Min(amount, Math.Max(0f, ShieldPoints));
                ShieldPoints -= absorbed;
                amount -= absorbed;
                taken += absorbed;
                if (ShieldPoints <= 0f)
                {
                    PhaseShields--;
                    ShieldPoints = PhaseShields > 0 ? StrikeRules.PhaseShieldPoints : 0f;
                }
            }
            if (amount > 0f)
            {
                float absorbed = Math.Min(amount, Math.Max(0f, Energy));
                Energy -= absorbed;
                taken += absorbed;
            }
            if (taken > 0f)
            {
                NotifyChanged();
            }
            return taken;
        }


        /// <summary>
        /// A hit at low energy with no phase shield destroys a weapon (call it after <see cref="AbsorbDamage"/>; it checks
        /// <see cref="WeaponsAtRisk"/> itself): the selected special (a spare copy takes its place; the last copy hands the
        /// selection to the next owned special), else the micro missiles, then the plasma cannon; never the machine gun.
        /// Returns the item lost, MachineGun for none.
        /// </summary>
        public StrikeItem LoseWeapon()
        {
            if (!WeaponsAtRisk)
            {
                return StrikeItem.MachineGun;
            }
            if (!HasSpecial)
            {
                Special = NextSpecial(Special, true);
            }
            StrikeItem lost = HasSpecial ? Special
                : Owns(StrikeItem.MicroMissiles) ? StrikeItem.MicroMissiles
                : Owns(StrikeItem.PlasmaCannon) ? StrikeItem.PlasmaCannon
                : StrikeItem.MachineGun;
            if (lost == StrikeItem.MachineGun)
            {
                return lost;
            }
            RemoveItem(lost);
            NotifyChanged();
            return lost;
        }


        /// <summary>Adds energy up to 100 (a negative amount takes it, down to 0).</summary>
        public void AddEnergy(float amount)
        {
            float energy = Math.Max(0f, Math.Min(StrikeRules.MaxEnergy, Energy + amount));
            if (energy != Energy)
            {
                Energy = energy;
                NotifyChanged();
            }
        }


        /// <summary>Uses one megabomb. False when there is none.</summary>
        public bool UseMegabomb()
        {
            if (Megabombs <= 0)
            {
                return false;
            }
            Megabombs--;
            NotifyChanged();
            return true;
        }


        /// <summary>A mission launched with less than <see cref="StrikeRules.MinLaunchEnergy"/> energy starts with that much.</summary>
        public void EnsureLaunchEnergy()
        {
            if (Energy < StrikeRules.MinLaunchEnergy)
            {
                Energy = StrikeRules.MinLaunchEnergy;
                NotifyChanged();
            }
        }


        /// <summary>
        /// A won mission lands: the saved pilot takes over the mission's working copy <paramref name="working"/> and the
        /// money <paramref name="earned"/> in the mission (the score) goes into the wallet, up to the cap. A failed,
        /// aborted or left mission simply drops its working copy.
        /// </summary>
        public void CommitMission(StrikeLoadout working, int earned)
        {
            if (working != null && working != this)
            {
                Array.Copy(working.counts, counts, counts.Length);
                Money = working.Money;
                Energy = working.Energy;
                PhaseShields = working.PhaseShields;
                ShieldPoints = working.ShieldPoints;
                Megabombs = working.Megabombs;
                Special = working.Special;
                Difficulty = working.Difficulty;
            }
            Money = StrikeRules.AddToWallet(Money, Math.Max(0, earned));
            NotifyChanged();
        }


        /// <summary>
        /// Puts every value back inside the rules (a loaded or hand-edited pilot): the machine gun owned, counts within
        /// their caps, energy 0 to 100, shield points only with a phase shield, money within the wallet cap and a special
        /// selected only when owned. Raises no <see cref="Changed"/>.
        /// </summary>
        public void Normalise()
        {
            for (int i = 0; i < counts.Length; i++)
            {
                var item = (StrikeItem)i;
                if (item == StrikeItem.MegaBomb || item == StrikeItem.PhaseShield || item == StrikeItem.EnergyModule)
                {
                    counts[i] = 0;
                    continue;
                }
                counts[i] = Math.Max(0, Math.Min(counts[i], StrikeArmory.Info(item).Max));
            }
            counts[(int)StrikeItem.MachineGun] = 1;
            Money = Math.Max(0, Math.Min(StrikeRules.WalletCap, Money));
            Energy = Math.Max(0f, Math.Min(StrikeRules.MaxEnergy, Energy));
            PhaseShields = Math.Max(0, Math.Min(StrikeRules.MaxPhaseShields, PhaseShields));
            ShieldPoints = PhaseShields > 0 ? Math.Max(0f, Math.Min(StrikeRules.PhaseShieldPoints, ShieldPoints)) : 0f;
            if (PhaseShields > 0 && ShieldPoints <= 0f)
            {
                ShieldPoints = StrikeRules.PhaseShieldPoints;
            }
            Megabombs = Math.Max(0, Math.Min(StrikeRules.MaxMegabombs, Megabombs));
            if (Difficulty > StrikeDifficulty.Elite)
            {
                Difficulty = StrikeRules.NewPilotDifficulty;
            }
            if (!HasSpecial)
            {
                Special = NextSpecial(Special, true);
            }
        }


        /// <summary>One more of <paramref name="item"/> (no cap, no Changed): a phase shield onto an empty stack comes whole, the first special is selected.</summary>
        internal void AddItem(StrikeItem item)
        {
            if (item == StrikeItem.PhaseShield && PhaseShields == 0)
            {
                ShieldPoints = StrikeRules.PhaseShieldPoints;
            }
            SetCount(item, Count(item) + 1);
            if (StrikeWeaponRules.IsSpecial(item) && !HasSpecial)
            {
                Special = item;
            }
        }


        /// <summary>One less of <paramref name="item"/> (no Changed); a special sold or lost out of the selection hands it on.</summary>
        internal void RemoveItem(StrikeItem item)
        {
            SetCount(item, Count(item) - 1);
            if (item == StrikeItem.PhaseShield && PhaseShields == 0)
            {
                ShieldPoints = 0f;
            }
            if (item == Special && Count(item) == 0)
            {
                Special = NextSpecial(item, true);
            }
        }


        /// <summary>
        /// The owned special after <paramref name="from"/> in enum order, wrapping (<paramref name="from"/> itself only when
        /// it is the only one, or when <paramref name="orSelf"/> and it is still owned); MachineGun when none is owned.
        /// </summary>
        private StrikeItem NextSpecial(StrikeItem from, bool orSelf)
        {
            if (orSelf && StrikeWeaponRules.IsSpecial(from) && Count(from) > 0)
            {
                return from;
            }
            List<StrikeItem> owned = OwnedSpecials();
            if (owned.Count == 0)
            {
                return StrikeItem.MachineGun;
            }
            foreach (StrikeItem special in owned)
            {
                if (special > from)
                {
                    return special;
                }
            }
            return owned[0];
        }
    }
}
