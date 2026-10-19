using System;
using System.Collections.Generic;
using Gamebox.Economy;
using Gamebox.Lockstep;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// The rules that move a career on: a new career in a league, the race setup of an offer, and what a finished race
    /// does to the career (<see cref="Apply"/>): money, points, the car's damage, what was used up, the other races of
    /// the day, the rivals' cars, the tracks the ladder opens, the title, and the offers of the next day. Plain code over
    /// a <see cref="CareerState"/>, so the tests play whole careers without Unity. The numbers are the league's
    /// (<see cref="LeagueSpec"/>, through <see cref="CareerRules"/>).
    /// </summary>
    public static class Career
    {
        /// <summary>The paint of The Warden's car: a dark blood red that reads against every track.</summary>
        public static Color WardenPaint => Leagues.Wasteland.ChampionPaint;

        /// <summary>The champions drive perfectly.</summary>
        public const float ChampionSkill = 1f;

        public const float WardenSkill = ChampionSkill;

        /// <summary>How many colours the theme has for the drivers (the player picks one, the rivals take the others).</summary>
        public const int Colors = CareerRules.LadderSize;

        /// <summary>The plasma cannon is a gun of the better cars: a driver who likes it mounts a minigun below this tier.</summary>
        public const int AiPlasmaTier = 4;

        // ------------------------------------------------------------------------------------------------
        // A new career
        // ------------------------------------------------------------------------------------------------

        /// <summary>A career of the first league on its first day: the Rustbucket, a little money, two mines and two nitros, the ladder of the roster.</summary>
        public static CareerState New(string driverName, int colorIndex, int seed)
        {
            return New(driverName, colorIndex, seed, LeagueKind.Wasteland);
        }

        /// <summary>A career of <paramref name="league"/> on its first day: the league's car and kit, the bottom of its ladder.</summary>
        public static CareerState New(string driverName, int colorIndex, int seed, LeagueKind league)
        {
            LeagueSpec spec = Leagues.Of(league);
            var state = new CareerState
            {
                league = spec.Index,
                driverName = string.IsNullOrWhiteSpace(driverName) ? "Rookie" : driverName.Trim(),
                colorIndex = Mathf.Clamp(colorIndex, 0, Colors - 1),
                seed = seed,
                wallet = new Wallet(spec.StartMoney),
                car = spec.StartCar,
                tracksOpen = spec.StartTracks,
                armed = spec.StartOil == 0 && spec.StartEmp > 0 ? SpecialKind.Emp : spec.StartOil == 0 && spec.StartMortar > 0 ? SpecialKind.Mortar : SpecialKind.Oil
            };
            state.garage.Add(OwnedCar.New(spec.StartCar));
            state.consumables.Set(CareerShop.MineId, spec.StartMines);
            state.consumables.Set(CareerShop.RocketId, spec.StartRockets);
            state.consumables.Set(CareerShop.NitroId, spec.StartNitro);
            state.consumables.Set(Specials.Of(SpecialKind.Oil).Id, spec.StartOil);
            state.consumables.Set(Specials.Of(SpecialKind.Emp).Id, spec.StartEmp);
            state.consumables.Set(Specials.Of(SpecialKind.Mortar).Id, spec.StartMortar);
            for (int i = 0; i < spec.Rivals.Length; i++)
            {
                RosterDriver driver = spec.Rivals[i];
                state.rivals.Add(new RivalState
                {
                    id = driver.Id,
                    name = driver.Name,
                    points = CareerRules.StartPoints(spec, i),
                    tier = CareerRules.StartTier(spec, i),
                    skill = CareerRules.StartSkill(spec, i)
                });
            }
            AssignColors(state);
            state.offers = RaceOffers.Generate(state, state.day);
            return state;
        }

        /// <summary>
        /// The champion of <paramref name="state"/>'s league moves up: a new career in the next league for the same driver
        /// in the same colour, or null after the last league.
        /// </summary>
        public static CareerState MoveUp(CareerState state, int seed)
        {
            LeagueSpec next = state?.League.Next;
            return next != null ? New(state.driverName, state.colorIndex, seed, next.Kind) : null;
        }

        /// <summary>
        /// Gives every rival a colour of their own: rival n has colour n + 1, and the one whose colour the player picked
        /// takes the player's unused colour 0 instead.
        /// </summary>
        public static void AssignColors(CareerState state)
        {
            LeagueSpec league = state.League;
            for (int i = 0; i < state.rivals.Count; i++)
            {
                int index = league.IndexOf(state.rivals[i].id);
                int color = (index >= 0 ? index : i) + 1;
                state.rivals[i].colorIndex = color == state.colorIndex ? 0 : color;
            }
        }

        // ------------------------------------------------------------------------------------------------
        // Racing an offer
        // ------------------------------------------------------------------------------------------------

        /// <summary>
        /// The race of today's offer <paramref name="index"/>: the player in their car with everything they carry on pole,
        /// the offer's rivals behind in cars of their tier upgraded for the day (a sabotaged one hurt), coloured by
        /// <paramref name="colorOf"/> (the theme's driver colours). Marks the offer as the one being raced.
        /// </summary>
        public static RaceSetup Enter(CareerState state, int index, Func<int, Color> colorOf)
        {
            if (state == null || index < 0 || index >= state.offers.Count)
            {
                return null;
            }
            state.entered = index;
            return Setup(state, state.offers[index], colorOf);
        }

        public static RaceSetup Setup(CareerState state, RaceOffer offer, Func<int, Color> colorOf)
        {
            var setup = new RaceSetup
            {
                Mode = RaceMode.Career,
                Track = offer.track,
                Laps = offer.laps,
                Difficulty = offer.difficulty,
                Seed = offer.seed,
                CashPerPickup = CareerRules.CashPerPickup(state.League, offer.difficulty),
                Bounty = CareerRules.Bounty(state.League, offer.difficulty),
                Pickups = true
            };
            setup.Entrants.Add(new RaceEntrant
            {
                Seat = 0,
                Name = state.driverName,
                Color = colorOf != null ? colorOf(state.colorIndex) : Color.white,
                Driver = DriverKind.Human,
                Skill = 1f,
                Loadout = PlayerLoadout(state),
                Viewport = 0
            });
            var random = new SeededRandom(SeededRandom.Mix((uint)offer.seed, 0xCA75u));
            int seat = 1;
            foreach (string id in offer.rivals)
            {
                RaceEntrant entrant = RivalEntrant(state, id, offer.difficulty, colorOf, random);
                if (entrant == null)
                {
                    continue;
                }
                entrant.Seat = seat++;
                setup.Entrants.Add(entrant);
            }
            return setup;
        }

        /// <summary>
        /// The player's car as it goes to the start: the current car, patched up if need be, with every mine, rocket,
        /// nitro and special weapon the career has, the one armed first as the player chose.
        /// </summary>
        public static CarLoadout PlayerLoadout(CareerState state)
        {
            OwnedCar owned = state.Current;
            CarLoadout loadout = owned != null ? owned.loadout.Clone() : new CarLoadout { model = state.car };
            loadout.health = Mathf.Max(CareerRules.MinStartHealth, Mathf.Clamp01(loadout.health));
            loadout.mines = state.consumables.Get(CareerShop.MineId);
            loadout.rockets = state.consumables.Get(CareerShop.RocketId);
            loadout.nitro = state.consumables.Get(CareerShop.NitroId);
            foreach (SpecialSpec special in Specials.All)
            {
                loadout.SetSpecials(special.Kind, state.consumables.Get(special.Id));
            }
            loadout.special = state.armed;
            return loadout;
        }

        private static RaceEntrant RivalEntrant(CareerState state, string id, Difficulty difficulty, Func<int, Color> colorOf, IRandom random)
        {
            bool sabotaged = state.HasSabotage && state.sabotage == id;
            LeagueSpec league = state.League;
            if (league.IsChampion(id))
            {
                CarLoadout champion = ChampionLoadout(league);
                champion.health = sabotaged ? CareerRules.SabotageHealth : 1f;
                return new RaceEntrant
                {
                    Name = league.Champion.Name, Color = league.ChampionPaint, Driver = DriverKind.Computer, Skill = ChampionSkill, Loadout = champion,
                    RivalId = id
                };
            }
            RivalState rival = state.Rival(id);
            if (rival == null)
            {
                return null;
            }
            CarLoadout loadout = AiLoadout(league, rival.tier, rival.id, state.day, difficulty, random);
            loadout.health = sabotaged ? CareerRules.SabotageHealth : 1f;
            return new RaceEntrant
            {
                Name = rival.name,
                Color = colorOf != null ? colorOf(rival.colorIndex) : Color.gray,
                Driver = DriverKind.Computer,
                Skill = RaceSkill(state, rival, difficulty),
                Loadout = loadout,
                RivalId = id
            };
        }

        /// <summary>
        /// The AI's skill for <paramref name="rival"/> in a race of <paramref name="difficulty"/>: within the league's band
        /// of the difficulty, by where the rival's own skill stands among the drivers of its third of the ladder, a little
        /// higher as the season goes on. (The rivals the champion brings to the Showdown race as Hard drivers.)
        /// </summary>
        public static float RaceSkill(CareerState state, RivalState rival, Difficulty difficulty)
        {
            Difficulty band = difficulty == Difficulty.Showdown ? Difficulty.Hard : difficulty;
            float lowest = float.MaxValue, highest = float.MinValue;
            foreach (RivalState other in Ladder.Third(state, band))
            {
                lowest = Mathf.Min(lowest, other.skill);
                highest = Mathf.Max(highest, other.skill);
            }
            float share = highest > lowest ? Mathf.Clamp01((rival.skill - lowest) / (highest - lowest)) : 0.5f;
            float season = Mathf.Clamp01((state.day - 1) / (float)CareerRules.SeasonDays);
            return CareerRules.AiSkill(state.League, band, share * (1f - CareerRules.SeasonSkillShare) + season * CareerRules.SeasonSkillShare);
        }

        /// <summary>A computer car of the first league: the main car of <paramref name="tier"/> (no driver's own choices).</summary>
        public static CarLoadout AiLoadout(int tier, int day, Difficulty difficulty, IRandom random)
        {
            return AiLoadout(Leagues.Wasteland, tier, null, day, difficulty, random);
        }

        /// <summary>
        /// A computer car of <paramref name="tier"/> for a race of <paramref name="difficulty"/> on <paramref name="day"/>
        /// of <paramref name="league"/>, for the driver <paramref name="driverId"/> (null: nobody in particular, the main
        /// car of the tier): the driver's own car of the tier, upgraded by the league's schedule (the engine, tyres and
        /// armour a level off now and then, by luck; the nitro system and gun mount with them from the league's day for
        /// them), the spiked bumper and the
        /// armour-piercing rounds on the better cars later on, the other fittings one by one in the driver's order, the
        /// driver's gun once the league's cars mount guns, and the mines, rockets, nitro and special weapons (of the
        /// driver's liking) of the difficulty.
        /// </summary>
        public static CarLoadout AiLoadout(LeagueSpec league, int tier, string driverId, int day, Difficulty difficulty, IRandom random)
        {
            tier = Mathf.Clamp(tier, 1, CarSpecs.MaxTier);
            CarModel model = driverId != null ? CarSpecs.ForDriver(tier, driverId) : CarSpecs.OfTier(tier);
            var loadout = new CarLoadout { model = model, health = 1f };
            int level = CareerRules.AiLevel(league, day, difficulty);
            foreach (UpgradeKind kind in CareerRules.ClassicUpgrades)
            {
                int luck = random != null ? random.Range(-1, 1) : 0;
                loadout.SetLevel(kind, Mathf.Max(0, level + luck));
            }
            int later = day >= league.AiLaterUpgradesDay ? level : 0;
            loadout.SetLevel(UpgradeKind.Boost, later);
            loadout.SetLevel(UpgradeKind.GunMount, later);
            loadout.spikes = tier >= 3 && day >= league.AiSpikesDay;
            loadout.piercing = tier >= 4 && day >= league.AiPiercingDay;
            int fittings = CareerRules.AiFittings(league, day, difficulty);
            List<EquipmentKind> order = FittingOrder(driverId);
            for (int i = 0; i < fittings && i < order.Count; i++)
            {
                loadout.Set(order[i], true);
            }
            if (CareerRules.AiMountsGun(league, day, difficulty))
            {
                loadout.GiveGun(GunOf(driverId, tier));
            }
            (int mines, int rockets, int nitro) = CareerRules.AiCarries(league, difficulty);
            loadout.mines = mines;
            loadout.rockets = rockets;
            loadout.nitro = nitro;
            int specials = CareerRules.AiSpecials(league, difficulty);
            if (specials > 0)
            {
                SpecialKind kind = SpecialOf(driverId);
                loadout.SetSpecials(kind, Mathf.Min(specials, Specials.Of(kind).Max));
                loadout.special = kind;
            }
            return loadout;
        }

        /// <summary>The gun the driver <paramref name="driverId"/> mounts on a car of <paramref name="tier"/>: their liking, else one picked by their id.</summary>
        public static GunKind GunOf(string driverId, int tier)
        {
            RosterDriver driver = Roster.Find(driverId);
            GunKind gun = driver != null && driver.Gun.HasValue
                ? driver.Gun.Value
                : CareerRules.BoughtGuns[(int)(RosterDriver.Hash(driverId, 0x6u) % (uint)CareerRules.BoughtGuns.Length)];
            return gun == GunKind.Plasma && tier < AiPlasmaTier ? GunKind.Minigun : gun;
        }

        /// <summary>The special weapon the driver <paramref name="driverId"/> carries: their liking, else one picked by their id.</summary>
        public static SpecialKind SpecialOf(string driverId)
        {
            RosterDriver driver = Roster.Find(driverId);
            return driver != null && driver.Special.HasValue ? driver.Special.Value : (SpecialKind)(RosterDriver.Hash(driverId, 0x5u) % (uint)Specials.Count);
        }

        /// <summary>The order the driver <paramref name="driverId"/> bolts on the later fittings in (the same every race).</summary>
        public static List<EquipmentKind> FittingOrder(string driverId)
        {
            var order = new List<EquipmentKind>(CareerRules.LaterFittings);
            order.Sort((a, b) => RosterDriver.Hash(driverId, 0x100u + (uint)a).CompareTo(RosterDriver.Hash(driverId, 0x100u + (uint)b)));
            return order;
        }

        /// <summary>
        /// The champion's car of <paramref name="league"/>: every upgrade and every fitting, the champion's gun, a full load
        /// of the Showdown's mines, rockets and nitro and of every special weapon.
        /// </summary>
        public static CarLoadout ChampionLoadout(LeagueSpec league)
        {
            CarLoadout loadout = CarLoadout.Upgraded(league.ChampionCar, 99);
            foreach (EquipmentKind kind in CareerRules.Fittings)
            {
                loadout.Set(kind, true);
            }
            loadout.GiveGun(league.ChampionGun);
            (int mines, int rockets, int nitro) = CareerRules.AiCarries(league, Difficulty.Showdown);
            loadout.mines = mines;
            loadout.rockets = rockets;
            loadout.nitro = nitro;
            int specials = CareerRules.AiSpecials(league, Difficulty.Showdown);
            foreach (SpecialSpec special in Specials.All)
            {
                loadout.SetSpecials(special.Kind, Mathf.Min(specials, special.Max));
            }
            loadout.special = league.ChampionSpecial;
            loadout.health = 1f;
            return loadout;
        }

        /// <summary>The Warden's Tyrant (the first league's champion), the best car of the first league: what the stat bars measure by.</summary>
        public static CarLoadout WardenLoadout()
        {
            return ChampionLoadout(Leagues.Wasteland);
        }

        // ------------------------------------------------------------------------------------------------
        // A race finished
        // ------------------------------------------------------------------------------------------------

        /// <summary>
        /// Books a finished career race: pays the prize of the place (none for a wreck), the bounties and the cash picked
        /// up, scores the ladder (none for a wreck), leaves the car with the damage it came back with (patched up to the
        /// minimum), takes away the mines, rockets, nitro and special weapons used, simulates the other races of the day,
        /// lets rivals earn better cars, opens tracks by the best rank, crowns the champion, and starts the next day with
        /// its offers.
        /// </summary>
        public static CareerReport Apply(CareerState state, RaceResult result)
        {
            RaceOffer offer = EnteredOffer(state, result);
            RacerResult player = PlayerResult(result);
            var report = new CareerReport
            {
                League = state.League.Kind,
                Day = state.day,
                Offer = offer,
                Difficulty = offer.difficulty,
                Track = offer.track,
                RankBefore = Ladder.Rank(state)
            };
            if (player != null)
            {
                BookPlayer(state, report, player);
            }
            BookRivals(state, report, result);
            SimulateDay(state, report, offer);
            Promote(state, report);
            if (report.Champion)
            {
                state.champion = true;
                state.championDay = state.day;
            }
            else if (offer.IsShowdown)
            {
                report.ShowdownLost = true;
                state.showdownsLost++;
            }
            report.RankAfter = Ladder.Rank(state);
            OpenTracks(state, report);
            NextDay(state);
            return report;
        }

        private static void BookPlayer(CareerState state, CareerReport report, RacerResult player)
        {
            LeagueSpec league = state.League;
            Difficulty difficulty = report.Difficulty;
            report.Place = player.Place;
            report.Finished = player.Finished;
            report.Wrecked = player.Wrecked;
            report.Kills = Mathf.Max(0, player.Kills);
            report.Prize = player.Wrecked ? 0 : CareerRules.Prize(league, difficulty, player.Place);
            report.Bounty = report.Kills * CareerRules.Bounty(league, difficulty);
            report.Cash = Mathf.Max(0, player.CashPicked);
            report.Points = player.Wrecked ? 0 : CareerRules.Points(league, difficulty, player.Place);
            state.wallet.Add(report.Earned);
            report.Champion = difficulty == Difficulty.Showdown && player.Place == 1 && !player.Wrecked;

            state.points += report.Points;
            state.races++;
            state.wins += player.Place == 1 && !player.Wrecked ? 1 : 0;
            state.kills += report.Kills;
            state.wrecks += player.Wrecked ? 1 : 0;
            state.earned += report.Earned;

            OwnedCar car = state.Current;
            report.HealthLeft = player.Wrecked ? 0f : Mathf.Clamp01(player.HealthLeft);
            report.HealthNow = Mathf.Max(CareerRules.MinStartHealth, report.HealthLeft);
            report.Patched = report.HealthLeft < CareerRules.MinStartHealth;
            if (car != null)
            {
                car.loadout.health = report.HealthNow;
                car.races++;
                car.kills += report.Kills;
                report.RepairCost = RepairCost(car.loadout);
            }

            report.MinesUsed = Mathf.Max(0, player.MinesUsed);
            report.RocketsUsed = Mathf.Max(0, player.RocketsUsed);
            report.NitroUsed = Mathf.Max(0, player.NitroUsed);
            report.OilUsed = Mathf.Max(0, player.OilUsed);
            report.EmpUsed = Mathf.Max(0, player.EmpUsed);
            report.MortarUsed = Mathf.Max(0, player.MortarUsed);
            state.consumables.Add(CareerShop.MineId, -report.MinesUsed);
            state.consumables.Add(CareerShop.RocketId, -report.RocketsUsed);
            state.consumables.Add(CareerShop.NitroId, -report.NitroUsed);
            foreach (SpecialSpec special in Specials.All)
            {
                state.consumables.Add(special.Id, -report.SpecialsUsed(special.Kind));
            }
        }

        /// <summary>The rivals of the player's race score by their places in it (none for a wreck).</summary>
        private static void BookRivals(CareerState state, CareerReport report, RaceResult result)
        {
            foreach (RacerResult racer in result.Racers)
            {
                RivalState rival = racer.Driver == DriverKind.Human ? null : state.Rival(racer.RivalId);
                if (rival == null)
                {
                    continue;
                }
                int points = racer.Wrecked ? 0 : CareerRules.Points(state.League, report.Difficulty, racer.Place);
                rival.points += points;
                rival.races++;
                rival.kills += Mathf.Max(0, racer.Kills);
                rival.wins += racer.Place == 1 && !racer.Wrecked ? 1 : 0;
                report.Rivals.Add(new RivalOutcome { Id = rival.id, Place = racer.Place, Wrecked = racer.Wrecked, Points = points });
            }
        }

        /// <summary>
        /// The races of the day the player was not in: each offer's three rivals and one more of the same third of the
        /// ladder, placed by how well they drive, what they drive and the luck of the day (a car or two wrecked).
        /// The champion races nobody but the player.
        /// </summary>
        private static void SimulateDay(CareerState state, CareerReport report, RaceOffer entered)
        {
            var raced = new HashSet<string>();
            foreach (RaceOffer offer in state.offers)
            {
                raced.UnionWith(offer.rivals);
            }
            var random = new SeededRandom(SeededRandom.Mix((uint)state.seed, (uint)state.day, 0x5100u));
            foreach (RaceOffer offer in state.offers)
            {
                if (offer == entered || offer.IsShowdown)
                {
                    continue;
                }
                var field = new List<RivalState>();
                foreach (string id in offer.rivals)
                {
                    RivalState rival = state.Rival(id);
                    if (rival != null)
                    {
                        field.Add(rival);
                    }
                }
                List<RivalState> third = Ladder.Third(state, offer.difficulty);
                random.Shuffle(third);
                foreach (RivalState extra in third)
                {
                    if (field.Count >= CareerRules.CarsPerRace)
                    {
                        break;
                    }
                    if (raced.Add(extra.id))
                    {
                        field.Add(extra);
                    }
                }
                report.OtherRaces.Add(Simulate(state, offer, field, random));
            }
        }

        private static DayRace Simulate(CareerState state, RaceOffer offer, List<RivalState> field, IRandom random)
        {
            LeagueSpec league = state.League;
            var form = new List<(RivalState rival, float score, bool wrecked)>(field.Count);
            foreach (RivalState rival in field)
            {
                CarLoadout car = AiLoadout(league, rival.tier, rival.id, state.day, offer.difficulty, random);
                float score = Rating(league, CareerRules.SkillOn(league, rival.skill, state.day), car) + Form(random);
                if (state.HasSabotage && state.sabotage == rival.id)
                {
                    score -= 1f - CareerRules.SabotageHealth;
                }
                form.Add((rival, score, random.Chance(CareerRules.WreckChance(league, offer.difficulty))));
            }
            form.Sort((a, b) => a.wrecked != b.wrecked ? a.wrecked.CompareTo(b.wrecked) : b.score.CompareTo(a.score));
            var race = new DayRace { Difficulty = offer.difficulty, Track = offer.track };
            for (int i = 0; i < form.Count; i++)
            {
                RivalState rival = form[i].rival;
                bool wrecked = form[i].wrecked;
                rival.points += wrecked ? 0 : CareerRules.Points(league, offer.difficulty, i + 1);
                rival.races++;
                rival.wins += i == 0 && !wrecked ? 1 : 0;
                race.Order.Add(rival.id);
                race.Wrecked.Add(wrecked);
            }
            return race;
        }

        /// <summary>
        /// How strong a driver in a car is, 0 to 1: half their skill, half the car (its tier, its upgrades, fittings and
        /// gun) as the league measures cars (<see cref="CarRating(LeagueSpec, CarLoadout)"/>). What decides the races the player is not in, and what the tests' simulated
        /// player drives with.
        /// </summary>
        public static float Rating(LeagueSpec league, float skill, CarLoadout car)
        {
            return 0.5f * Mathf.Clamp01(skill) + 0.5f * CarRating(league, car);
        }

        /// <summary>The rating of the first league.</summary>
        public static float Rating(float skill, CarLoadout car)
        {
            return Rating(Leagues.Wasteland, skill, car);
        }

        /// <summary>
        /// A car's strength in <paramref name="league"/>, 0 (a tier 1 car off the lot) to 1 (the champion's car): its tier,
        /// up to one tier more for the parts the career always had (<see cref="ClassicShare"/>) and up to half a tier for
        /// the later ones (<see cref="ExtraShare"/>), over the tiers up to the champion's.
        /// </summary>
        public static float CarRating(LeagueSpec league, CarLoadout car)
        {
            if (car == null)
            {
                return 0f;
            }
            float extra = CareerRules.ExtraPartsTiers;
            return Mathf.Clamp01((car.Spec.Tier - 1 + ClassicShare(car) + extra * ExtraShare(car)) / (league.MaxRivalTier + 1));
        }

        /// <summary>A car's strength in the first league: 0 a Rustbucket off the lot, 1 The Warden's Tyrant.</summary>
        public static float CarRating(CarLoadout car)
        {
            return CarRating(Leagues.Wasteland, car);
        }

        /// <summary>How much a car has of the engine, tyre and armour levels its model takes and of the bumper and the rounds, 0 to 1.</summary>
        public static float ClassicShare(CarLoadout car)
        {
            CarSpec spec = car.Spec;
            int levels = 0, max = 0;
            foreach (UpgradeKind kind in CareerRules.ClassicUpgrades)
            {
                levels += car.Level(kind);
                max += spec.MaxLevelOf(kind);
            }
            foreach (EquipmentKind kind in CareerRules.ClassicFittings)
            {
                levels += car.Has(kind) ? 1 : 0;
                max++;
            }
            return max > 0 ? Mathf.Clamp01((float)levels / max) : 0f;
        }

        /// <summary>How much a car has of the nitro system and gun mount levels its model takes, the later fittings and a gun, 0 to 1.</summary>
        public static float ExtraShare(CarLoadout car)
        {
            CarSpec spec = car.Spec;
            int parts = 0, max = 0;
            foreach (UpgradeKind kind in CareerRules.LaterUpgrades)
            {
                parts += car.Level(kind);
                max += spec.MaxLevelOf(kind);
            }
            foreach (EquipmentKind kind in CareerRules.LaterFittings)
            {
                parts += car.Has(kind) ? 1 : 0;
                max++;
            }
            parts += car.gun != GunKind.MachineGun && car.OwnsGun(car.gun) ? CareerRules.GunExtraParts : 0;
            max += CareerRules.GunExtraParts;
            return max > 0 ? Mathf.Clamp01((float)parts / max) : 0f;
        }

        /// <summary>A driver's form on the day: a bell curve around zero (Box-Muller).</summary>
        public static float Form(IRandom random)
        {
            double u = Math.Max(1e-6, random.Value());
            double v = random.Value();
            return (float)(Math.Sqrt(-2.0 * Math.Log(u)) * Math.Cos(2.0 * Math.PI * v)) * CareerRules.SimulatedForm;
        }

        /// <summary>Rivals whose points earned a better tier move up to it (never down).</summary>
        private static void Promote(CareerState state, CareerReport report)
        {
            LeagueSpec league = state.League;
            foreach (RivalState rival in state.rivals)
            {
                int earned = CareerRules.TierFor(league, league.IndexOf(rival.id), rival.points);
                if (earned > rival.tier)
                {
                    rival.tier = earned;
                    report.Promotions.Add(new Promotion { Id = rival.id, Name = rival.name, Tier = rival.tier, Car = rival.Car });
                }
            }
        }

        private static void OpenTracks(CareerState state, CareerReport report)
        {
            LeagueSpec league = state.League;
            state.bestRank = Mathf.Min(state.bestRank, report.RankAfter);
            int open = Mathf.Min(league.Tracks.Length, CareerRules.TracksOpenAt(league, state.bestRank));
            for (int i = state.tracksOpen; i < open; i++)
            {
                report.TracksOpened.Add(league.Tracks[i]);
            }
            state.tracksOpen = Mathf.Max(state.tracksOpen, open);
        }

        private static void NextDay(CareerState state)
        {
            state.day++;
            state.entered = -1;
            state.sabotage = "";
            state.offers = RaceOffers.Generate(state, state.day);
        }

        /// <summary>The offer that was raced: the one marked when it was entered, else the one the race's setup matches.</summary>
        private static RaceOffer EnteredOffer(CareerState state, RaceResult result)
        {
            if (state.entered >= 0 && state.entered < state.offers.Count)
            {
                return state.offers[state.entered];
            }
            RaceSetup setup = result?.Setup;
            foreach (RaceOffer offer in state.offers)
            {
                if (setup != null && offer.difficulty == setup.Difficulty && offer.track == setup.Track)
                {
                    return offer;
                }
            }
            var raced = new RaceOffer
            {
                difficulty = setup != null ? setup.Difficulty : Difficulty.Easy,
                track = setup != null ? setup.Track : 0,
                laps = setup != null ? setup.Laps : 3
            };
            if (result != null)
            {
                foreach (RacerResult racer in result.Racers)
                {
                    if (!string.IsNullOrEmpty(racer.RivalId))
                    {
                        raced.rivals.Add(racer.RivalId);
                    }
                }
            }
            return raced;
        }

        private static RacerResult PlayerResult(RaceResult result)
        {
            if (result == null)
            {
                return null;
            }
            foreach (RacerResult racer in result.Racers)
            {
                if (racer.Driver == DriverKind.Human)
                {
                    return racer;
                }
            }
            return result.Of(0);
        }

        // ------------------------------------------------------------------------------------------------
        // The car
        // ------------------------------------------------------------------------------------------------

        /// <summary>The health points a car is missing.</summary>
        public static int MissingPoints(CarLoadout car)
        {
            if (car == null)
            {
                return 0;
            }
            float max = car.Stats().MaxHealth;
            return Mathf.Max(0, Mathf.CeilToInt(max * (1f - Mathf.Clamp01(car.health)) - 0.001f));
        }

        /// <summary>What mending <paramref name="car"/> fully costs.</summary>
        public static int RepairCost(CarLoadout car)
        {
            return car != null ? MissingPoints(car) * CareerRules.RepairPrice(car.Spec.Tier) : 0;
        }

        // ------------------------------------------------------------------------------------------------
        // Loading a save
        // ------------------------------------------------------------------------------------------------

        /// <summary>
        /// Checks a career read from a save and mends what can be mended (a missing rival back on the ladder at the
        /// bottom, a missing car back in the garage, a gun mounted that the car does not have, the offers of the day,
        /// numbers within their bounds). A save from before the leagues reads as a career of the first league. Returns why
        /// it cannot be played (an English key), or null when it can.
        /// </summary>
        public static string Validate(CareerState state)
        {
            if (state == null)
            {
                return "The saved career is incomplete.";
            }
            if (state.version > CareerState.CurrentVersion || state.league >= Leagues.Count)
            {
                return "This career was saved by a newer version of the game.";
            }
            if (state.rivals == null || state.garage == null || state.wallet == null || state.league < 0)
            {
                return "The saved career is incomplete.";
            }
            LeagueSpec league = state.League;
            state.version = CareerState.CurrentVersion;
            state.day = Mathf.Max(1, state.day);
            state.driverName = string.IsNullOrWhiteSpace(state.driverName) ? "Rookie" : state.driverName;
            state.colorIndex = Mathf.Clamp(state.colorIndex, 0, Colors - 1);
            state.consumables ??= new Inventory();
            state.offers ??= new List<RaceOffer>();
            state.sabotage ??= "";
            state.armed = (SpecialKind)Mathf.Clamp((int)state.armed, 0, Specials.Count - 1);
            state.garage.RemoveAll(owned => owned == null || owned.loadout == null);
            if (state.garage.Count == 0)
            {
                state.garage.Add(OwnedCar.New(state.car));
            }
            if (state.Current.loadout.model != state.car)
            {
                state.car = state.Current.loadout.model;
            }
            foreach (OwnedCar owned in state.garage)
            {
                CarLoadout car = owned.loadout;
                car.model = (CarModel)Mathf.Clamp((int)car.model, 0, CarSpecs.Count - 1);
                car.health = Mathf.Clamp01(car.health);
                foreach (UpgradeKind kind in CareerRules.Upgrades)
                {
                    car.SetLevel(kind, car.Level(kind));
                }
                // Only the guns there are, and the one mounted one of them (else the machine gun).
                int valid = 0;
                foreach (GunKind gun in CareerRules.BoughtGuns)
                {
                    valid |= 1 << (int)gun;
                }
                car.guns &= valid;
                if ((int)car.gun < 0 || (int)car.gun >= Guns.Count || !car.OwnsGun(car.gun))
                {
                    car.gun = GunKind.MachineGun;
                }
                car.mines = car.rockets = car.nitro = 0;
                car.oil = car.emp = car.mortar = 0;
            }
            state.consumables.Clamp(new CareerShop(state).Catalog);
            state.rivals.RemoveAll(rival => rival == null || league.IndexOf(rival.id) < 0);
            for (int i = 0; i < league.Rivals.Length; i++)
            {
                if (state.Rival(league.Rivals[i].Id) == null)
                {
                    state.rivals.Add(new RivalState
                    {
                        id = league.Rivals[i].Id, name = league.Rivals[i].Name, tier = CareerRules.StartTier(league, i),
                        skill = CareerRules.StartSkill(league, i)
                    });
                }
            }
            foreach (RivalState rival in state.rivals)
            {
                RosterDriver driver = league.Find(rival.id);
                rival.name = string.IsNullOrEmpty(rival.name) ? driver.Name : rival.name;
                rival.tier = Mathf.Clamp(rival.tier, 1, league.MaxRivalTier);
                rival.skill = Mathf.Clamp01(rival.skill);
                rival.points = Mathf.Max(0, rival.points);
            }
            AssignColors(state);
            state.tracksOpen = Mathf.Clamp(state.tracksOpen, league.StartTracks, league.Tracks.Length);
            state.bestRank = Mathf.Clamp(state.bestRank, 1, CareerRules.LadderSize);
            if (state.offers.Count == 0 || state.offers.Exists(offer => offer == null || offer.rivals == null || offer.rivals.Count == 0))
            {
                state.offers = RaceOffers.Generate(state, state.day);
            }
            state.entered = -1;
            return null;
        }
    }
}
