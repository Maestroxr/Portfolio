using System.Collections.Generic;
using Gamebox.Economy;
using Gamebox.Lockstep;
using UnityEngine;

namespace Portfolio.DeathRace.Tests
{
    /// <summary>
    /// Plays a career of a league the way an average player would, without Unity: every day it enters the hardest race
    /// whose rivals its car can live with (the Showdown, with the champion sabotaged, as soon as it can be won now and
    /// then), races it with a driver
    /// of middling skill (the same rating and luck as the races the career simulates, a kill now and then, some cash,
    /// some damage, a special weapon fired), then mends the car, restocks a few mines and nitros (and a special weapon in
    /// the later leagues), and saves up for the next car of the dealer, spending what is left on the cheapest upgrades,
    /// fittings and a gun. What the tests check the tuning of every league with.
    /// </summary>
    public sealed class CareerSimulation
    {
        public const float PlayerSkill = 0.75f;

        /// <summary>The player takes on the champion once the Showdown is won about this often.</summary>
        public const float ShowdownChance = 0.1f;

        private readonly SeededRandom random;

        /// <summary>What the visits so far have set aside for upgrades and not spent.</summary>
        private int savedBudget;

        public CareerSimulation(int seed, LeagueKind league = LeagueKind.Wasteland)
        {
            random = new SeededRandom((uint)seed * 7919u + 13u);
            State = Career.New("Sim", 0, seed, league);
        }

        public CareerState State { get; }

        public LeagueSpec League => State.League;

        /// <summary>The races run before the player first stood first on the ladder, or -1 while they have not.</summary>
        public int RacesToTop { get; private set; } = -1;

        /// <summary>The race that won the championship, or -1.</summary>
        public int RacesToTitle { get; private set; } = -1;

        public List<CareerReport> Reports { get; } = new List<CareerReport>();

        /// <summary>Money spent on repairs, on mines, rockets and nitro, on upgrades and equipment, on cars (net of trade-ins).</summary>
        public int SpentRepairs, SpentMarket, SpentUpgrades, SpentCars;

        /// <summary>A line about the races entered: per difficulty how many and the average place.</summary>
        public string RaceSummary()
        {
            var lines = new List<string>();
            foreach (Difficulty difficulty in new[] { Difficulty.Easy, Difficulty.Medium, Difficulty.Hard, Difficulty.Showdown })
            {
                List<CareerReport> raced = Reports.FindAll(report => report.Difficulty == difficulty);
                if (raced.Count == 0)
                {
                    continue;
                }
                float place = 0f;
                int wrecks = 0;
                foreach (CareerReport report in raced)
                {
                    place += report.Place;
                    wrecks += report.Wrecked ? 1 : 0;
                }
                lines.Add($"{difficulty} {raced.Count}x place {place / raced.Count:0.0} wrecks {wrecks}");
            }
            return string.Join("; ", lines);
        }

        /// <summary>The cars bought, with the race before which each was bought.</summary>
        public List<(CarModel car, int race)> Cars { get; } = new List<(CarModel car, int race)>();

        /// <summary>Plays one day: a race and a visit to the garage.</summary>
        public CareerReport Day()
        {
            int index = Choose();
            if (State.offers[index].IsShowdown && !State.HasSabotage)
            {
                // Before the Showdown a sensible player pays a mechanic to visit the champion's garage.
                int before = State.wallet.Money;
                new CareerShop(State).Sabotage(League.Champion.Id);
                SpentMarket += before - State.wallet.Money;
            }
            RaceSetup setup = Career.Enter(State, index, color => Color.white);
            RaceResult result = Race(setup);
            CareerReport report = Career.Apply(State, result);
            Reports.Add(report);
            if (RacesToTop < 0 && report.RankAfter == 1)
            {
                RacesToTop = State.races;
            }
            if (RacesToTitle < 0 && report.Champion)
            {
                RacesToTitle = State.races;
            }
            Shop();
            return report;
        }

        public float PlayerRating => Career.Rating(League, PlayerSkill, State.Current.loadout);

        /// <summary>
        /// The offer worth most to a sensible player: the points it can be expected to score (and a little of the money),
        /// from a few hundred imagined races with everyone's rating and luck. The Showdown once it can be won now and then.
        /// </summary>
        private int Choose()
        {
            int choice = 0;
            float best = float.MinValue;
            for (int i = 0; i < State.offers.Count; i++)
            {
                RaceOffer offer = State.offers[i];
                (float points, float money, float wins) = Expect(offer);
                if (offer.IsShowdown)
                {
                    if (wins >= ShowdownChance)
                    {
                        return i;
                    }
                    continue;
                }
                float value = points + money / 1500f;
                if (offer.difficulty == Difficulty.Hard && Ladder.Rank(State) <= 5)
                {
                    // Near the top the points that count are the ones the leaders do not get: race them.
                    value *= 1.4f;
                }
                if (value > best)
                {
                    best = value;
                    choice = i;
                }
            }
            return choice;
        }

        private (float points, float money, float wins) Expect(RaceOffer offer)
        {
            const int Samples = 200;
            var imagined = new SeededRandom((uint)(offer.seed ^ State.day * 31));
            float player = PlayerRating;
            var rivals = new List<float>();
            foreach (string id in offer.rivals)
            {
                if (League.IsChampion(id))
                {
                    rivals.Add(Career.Rating(League, Career.ChampionSkill, Career.ChampionLoadout(League)));
                    continue;
                }
                RivalState rival = State.Rival(id);
                if (rival != null)
                {
                    CarLoadout car = Career.AiLoadout(League, rival.tier, rival.id, State.day, offer.difficulty, null);
                    rivals.Add(Career.Rating(League, CareerRules.SkillOn(League, rival.skill, State.day), car));
                }
            }
            float points = 0f, money = 0f, wins = 0f;
            int wreck = WreckChance(player, rivals);
            for (int sample = 0; sample < Samples; sample++)
            {
                if (imagined.Chance(wreck))
                {
                    continue;
                }
                float mine = player + Career.Form(imagined);
                int place = 1;
                foreach (float rival in rivals)
                {
                    place += rival + Career.Form(imagined) > mine ? 1 : 0;
                }
                points += CareerRules.Points(League, offer.difficulty, place);
                money += CareerRules.Prize(League, offer.difficulty, place);
                wins += place == 1 ? 1f : 0f;
            }
            return (points / Samples, money / Samples, wins / Samples);
        }

        /// <summary>
        /// How likely the player is wrecked (percent): a little always, much more in a car the others outgun (a
        /// Rustbucket among Vipers does not see the flag).
        /// </summary>
        private static int WreckChance(float player, List<float> rivals)
        {
            float average = 0f;
            foreach (float rival in rivals)
            {
                average += rival / Mathf.Max(1, rivals.Count);
            }
            return Mathf.Clamp(6 + Mathf.RoundToInt(Mathf.Max(0f, average - player) * 220f), 6, 80);
        }

        /// <summary>A race as it might go: everyone's rating plus the luck of the day; a car now and then wrecked.</summary>
        private RaceResult Race(RaceSetup setup)
        {
            var ratings = new List<float>();
            foreach (RaceEntrant entrant in setup.Entrants)
            {
                if (!entrant.IsHuman)
                {
                    ratings.Add(Career.Rating(League, RosterSkill(entrant), entrant.Loadout));
                }
            }
            int playerWreck = WreckChance(PlayerRating, ratings);
            var scores = new List<(RaceEntrant entrant, float score, bool wrecked)>();
            foreach (RaceEntrant entrant in setup.Entrants)
            {
                float skill = entrant.IsHuman ? PlayerSkill : RosterSkill(entrant);
                float score = Career.Rating(League, skill, entrant.Loadout) + Career.Form(random) - (1f - entrant.Loadout.health) * 0.15f;
                bool wrecked = random.Chance(entrant.IsHuman ? playerWreck : CareerRules.WreckChance(League, setup.Difficulty));
                scores.Add((entrant, score, wrecked));
            }
            scores.Sort((a, b) => a.wrecked != b.wrecked ? a.wrecked.CompareTo(b.wrecked) : b.score.CompareTo(a.score));
            var result = new RaceResult { Setup = setup };
            for (int place = 1; place <= scores.Count; place++)
            {
                RaceEntrant entrant = scores[place - 1].entrant;
                bool wrecked = scores[place - 1].wrecked;
                result.Racers.Add(new RacerResult
                {
                    Seat = entrant.Seat, Name = entrant.Name, Driver = entrant.Driver, RivalId = entrant.RivalId, Place = place,
                    Finished = !wrecked, Wrecked = wrecked, Time = 120f + place * 3f, BestLap = 28f,
                    Kills = entrant.IsHuman ? random.Range(0, 3) / 2 + (random.Chance(15) ? 1 : 0) : 0,
                    CashPicked = entrant.IsHuman ? random.Range(2, 7) * setup.CashPerPickup : 0,
                    HealthLeft = wrecked ? 0f : 0.35f + (float)random.Value() * 0.5f,
                    MinesUsed = entrant.IsHuman ? Mathf.Min(entrant.Loadout.mines, random.Range(0, 3)) : 0,
                    RocketsUsed = entrant.IsHuman ? Mathf.Min(entrant.Loadout.rockets, random.Range(0, 2)) : 0,
                    NitroUsed = entrant.IsHuman ? Mathf.Min(entrant.Loadout.nitro, random.Range(0, 3)) : 0,
                    OilUsed = entrant.IsHuman && entrant.Loadout.oil > 0 ? random.Range(0, 2) : 0,
                    EmpUsed = entrant.IsHuman && entrant.Loadout.emp > 0 ? random.Range(0, 2) : 0,
                    MortarUsed = entrant.IsHuman && entrant.Loadout.mortar > 0 ? random.Range(0, 2) : 0
                });
            }
            return result;
        }

        /// <summary>
        /// How good a rival is in the career's own terms (what the races the player is not in go by), not the band of the
        /// driving AI the race setup gives it.
        /// </summary>
        private float RosterSkill(RaceEntrant entrant)
        {
            RivalState rival = State.Rival(entrant.RivalId);
            return rival != null ? CareerRules.SkillOn(League, rival.skill, State.day) : entrant.Skill;
        }

        /// <summary>Mends the car, restocks, saves up for the next car, and upgrades with what the plan leaves over.</summary>
        private void Shop()
        {
            var shop = new CareerShop(State);
            int before = State.wallet.Money;
            shop.RepairAll();
            SpentRepairs += before - State.wallet.Money;
            before = State.wallet.Money;
            // An early career buys a mine and a nitro at most; later two of each and a rocket, and one of the special weapon
            // armed in the leagues after the first.
            bool early = State.car == League.StartCar;
            Restock(shop, CareerShop.MineId, early ? 1 : 2);
            Restock(shop, CareerShop.NitroId, early ? 1 : 2);
            Restock(shop, CareerShop.RocketId, early ? 0 : 1);
            if (League.Kind != LeagueKind.Wasteland)
            {
                Restock(shop, CareerShop.SpecialId(State.armed), 1);
            }
            SpentMarket += before - State.wallet.Money;
            int tier = State.Current.loadout.Spec.Tier;
            if (tier < League.MaxDealerTier)
            {
                CarModel next = CarSpecs.OfTier(tier + 1);
                ShopItem car = shop.Catalog[CareerShop.CarId(next)];
                int price = shop.NextPrice(State.consumables, car);
                if (price >= 0 && State.wallet.Money >= price + Reserve)
                {
                    SpentCars += price;
                    shop.Buy(State.wallet, State.consumables, car);
                    Cars.Add((next, State.races));
                    shop = new CareerShop(State);
                    shop.RepairAll();
                }
                // Saving for the next car: a third of what the race paid may go on upgrades, never below half its price.
                Upgrade(ref shop, Mathf.RoundToInt(price * 0.5f), Reports[Reports.Count - 1].Earned / 3);
            }
            else
            {
                Upgrade(ref shop, 0, int.MaxValue);
            }
        }

        /// <summary>The car with one more level or piece of <paramref name="item"/> (an upgrade, a fitting, a gun mounted).</summary>
        private static CarLoadout With(CarLoadout car, ShopItem item)
        {
            CarLoadout after = car.Clone();
            switch (item.Tag)
            {
                case UpgradeKind kind:
                    after.SetLevel(kind, after.Level(kind) + 1);
                    break;
                case EquipmentKind kind:
                    after.Set(kind, true);
                    break;
                case GunKind gun:
                    after.GiveGun(gun);
                    break;
            }
            return after;
        }

        /// <summary>What the player upgrades the car with: the five upgrades, the seven fittings, and a gun while it has only the machine gun.</summary>
        private IEnumerable<string> Parts()
        {
            foreach (UpgradeKind kind in CareerRules.Upgrades)
            {
                yield return CareerShop.UpgradeId(kind);
            }
            foreach (EquipmentKind kind in CareerRules.Fittings)
            {
                yield return CareerShop.FittingId(kind);
            }
            if (State.Current.loadout.gun == GunKind.MachineGun)
            {
                foreach (GunKind gun in CareerRules.BoughtGuns)
                {
                    yield return CareerShop.GunId(gun);
                }
            }
        }

        private void Restock(CareerShop shop, string id, int keep)
        {
            while (State.consumables.Get(id) < keep && shop.Buy(id) == BuyResult.Bought)
            {
            }
        }

        /// <summary>What the player keeps after buying a car (for the repairs of the first races in it): more in the later leagues.</summary>
        private int Reserve => 600 * CarSpecs.Of(League.StartCar).Tier;

        /// <summary>
        /// Buys the next levels of upgrades, fittings and a gun that add the most to the car's rating for their price, for
        /// up to <paramref name="budget"/>, while the money stays above
        /// <paramref name="reserve"/>; a part dearer than the budget is bought once the budgets of a few visits have added up.
        /// </summary>
        private void Upgrade(ref CareerShop shop, int reserve, int budget)
        {
            savedBudget = budget == int.MaxValue ? int.MaxValue : savedBudget + budget;
            while (true)
            {
                ShopItem cheapest = null;
                int best = int.MaxValue;
                float bestCost = float.MaxValue;
                float rating = Career.CarRating(League, State.Current.loadout);
                foreach (string id in Parts())
                {
                    ShopItem item = shop.Catalog[id];
                    int price = shop.NextPrice(State.consumables, item);
                    float gain = Career.CarRating(League, With(State.Current.loadout, item)) - rating;
                    float cost = price / Mathf.Max(1e-5f, gain);
                    if (price >= 0 && gain > 0f && cost < bestCost)
                    {
                        bestCost = cost;
                        best = price;
                        cheapest = item;
                    }
                }
                if (cheapest == null || best > savedBudget || State.wallet.Money - best < reserve ||
                    shop.Buy(State.wallet, State.consumables, cheapest) != BuyResult.Bought)
                {
                    return;
                }
                SpentUpgrades += best;
                savedBudget = savedBudget == int.MaxValue ? savedBudget : savedBudget - best;
                if (shop.IsStale)
                {
                    shop = new CareerShop(State);
                }
            }
        }
    }
}
