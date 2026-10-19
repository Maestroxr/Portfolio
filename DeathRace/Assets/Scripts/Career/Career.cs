using System;
using System.Collections.Generic;
using Gamebox.Economy;
using Gamebox.Lockstep;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// The rules that move a career on: a new career, the race setup of an offer, and what a finished race does to the
    /// career (<see cref="Apply"/>): money, points, the car's damage, what was used up, the other races of the day, the
    /// rivals' cars, the tracks the ladder opens, the title, and the offers of the next day. Plain code over a
    /// <see cref="CareerState"/>, so the tests play whole careers without Unity.
    /// </summary>
    public static class Career
    {
        /// <summary>The paint of The Warden's car: a dark blood red that reads against every track.</summary>
        public static readonly Color WardenPaint = new Color(0.42f, 0.03f, 0.05f, 1f);

        /// <summary>The Warden drives perfectly.</summary>
        public const float WardenSkill = 1f;

        /// <summary>How many colours the theme has for the drivers (the player picks one, the rivals take the others).</summary>
        public const int Colors = CareerRules.LadderSize;

        // ------------------------------------------------------------------------------------------------
        // A new career
        // ------------------------------------------------------------------------------------------------

        /// <summary>A career on its first day: the Rustbucket, a little money, two mines and two nitros, the ladder of the roster.</summary>
        public static CareerState New(string driverName, int colorIndex, int seed)
        {
            var state = new CareerState
            {
                driverName = string.IsNullOrWhiteSpace(driverName) ? "Rookie" : driverName.Trim(),
                colorIndex = Mathf.Clamp(colorIndex, 0, Colors - 1),
                seed = seed,
                wallet = new Wallet(CareerRules.StartMoney),
                car = CareerRules.StartCar
            };
            state.garage.Add(OwnedCar.New(CareerRules.StartCar));
            state.consumables.Set(CareerShop.MineId, CareerRules.StartMines);
            state.consumables.Set(CareerShop.RocketId, CareerRules.StartRockets);
            state.consumables.Set(CareerShop.NitroId, CareerRules.StartNitro);
            for (int i = 0; i < Roster.Rivals.Length; i++)
            {
                RosterDriver driver = Roster.Rivals[i];
                state.rivals.Add(new RivalState
                {
                    id = driver.Id,
                    name = driver.Name,
                    points = CareerRules.StartPoints(i),
                    tier = CareerRules.StartTier(i),
                    skill = CareerRules.StartSkill(i)
                });
            }
            AssignColors(state);
            state.offers = RaceOffers.Generate(state, state.day);
            return state;
        }

        /// <summary>
        /// Gives every rival a colour of their own: rival n has colour n + 1, and the one whose colour the player picked
        /// takes the player's unused colour 0 instead.
        /// </summary>
        public static void AssignColors(CareerState state)
        {
            for (int i = 0; i < state.rivals.Count; i++)
            {
                int index = Roster.IndexOf(state.rivals[i].id);
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
                CashPerPickup = CareerRules.CashPerPickup(offer.difficulty),
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

        /// <summary>The player's car as it goes to the start: the current car, patched up if need be, with every mine, rocket and nitro the career has.</summary>
        public static CarLoadout PlayerLoadout(CareerState state)
        {
            OwnedCar owned = state.Current;
            CarLoadout loadout = owned != null ? owned.loadout.Clone() : new CarLoadout { model = state.car };
            loadout.health = Mathf.Max(CareerRules.MinStartHealth, Mathf.Clamp01(loadout.health));
            loadout.mines = state.consumables.Get(CareerShop.MineId);
            loadout.rockets = state.consumables.Get(CareerShop.RocketId);
            loadout.nitro = state.consumables.Get(CareerShop.NitroId);
            return loadout;
        }

        private static RaceEntrant RivalEntrant(CareerState state, string id, Difficulty difficulty, Func<int, Color> colorOf, IRandom random)
        {
            bool sabotaged = state.HasSabotage && state.sabotage == id;
            if (id == Roster.WardenId)
            {
                CarLoadout warden = WardenLoadout();
                warden.health = sabotaged ? CareerRules.SabotageHealth : 1f;
                return new RaceEntrant
                {
                    Name = Roster.Warden.Name, Color = WardenPaint, Driver = DriverKind.Computer, Skill = WardenSkill, Loadout = warden,
                    RivalId = id
                };
            }
            RivalState rival = state.Rival(id);
            if (rival == null)
            {
                return null;
            }
            CarLoadout loadout = AiLoadout(rival.tier, state.day, difficulty, random);
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
        /// The AI's skill for <paramref name="rival"/> in a race of <paramref name="difficulty"/>: within the band of the
        /// difficulty, by where the rival's own skill stands among the drivers of its third of the ladder, a little higher
        /// as the season goes on. (The rivals the Warden brings to the Showdown race as Hard drivers.)
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
            return CareerRules.AiSkill(band, share * (1f - CareerRules.SeasonSkillShare) + season * CareerRules.SeasonSkillShare);
        }

        /// <summary>
        /// A computer car of <paramref name="tier"/> for a race of <paramref name="difficulty"/> on <paramref name="day"/>:
        /// upgraded a level every week of the season (a part or two a level off, by luck), the spiked bumper and the
        /// armour-piercing rounds on the better cars later on, and the mines, rockets and nitro of the difficulty.
        /// </summary>
        public static CarLoadout AiLoadout(int tier, int day, Difficulty difficulty, IRandom random)
        {
            tier = Mathf.Clamp(tier, 1, CarSpecs.MaxTier);
            var loadout = new CarLoadout { model = CarSpecs.OfTier(tier), health = 1f };
            int level = CareerRules.AiLevel(day, difficulty);
            foreach (UpgradeKind kind in CareerRules.Upgrades)
            {
                int luck = random != null ? random.Range(-1, 1) : 0;
                loadout.SetLevel(kind, Mathf.Max(0, level + luck));
            }
            loadout.spikes = tier >= 3 && day >= CareerRules.AiSpikesDay;
            loadout.piercing = tier >= 4 && day >= CareerRules.AiPiercingDay;
            (int mines, int rockets, int nitro) = CareerRules.AiCarries(difficulty);
            loadout.mines = mines;
            loadout.rockets = rockets;
            loadout.nitro = nitro;
            return loadout;
        }

        /// <summary>The Tyrant with every upgrade and both pieces of equipment, loaded for war.</summary>
        public static CarLoadout WardenLoadout()
        {
            CarLoadout loadout = CarLoadout.Upgraded(CarModel.Tyrant, 99);
            loadout.spikes = true;
            loadout.piercing = true;
            (int mines, int rockets, int nitro) = CareerRules.AiCarries(Difficulty.Showdown);
            loadout.mines = mines;
            loadout.rockets = rockets;
            loadout.nitro = nitro;
            loadout.health = 1f;
            return loadout;
        }

        // ------------------------------------------------------------------------------------------------
        // A race finished
        // ------------------------------------------------------------------------------------------------

        /// <summary>
        /// Books a finished career race: pays the prize of the place (none for a wreck), the bounties and the cash picked
        /// up, scores the ladder (none for a wreck), leaves the car with the damage it came back with (patched up to the
        /// minimum), takes away the mines, rockets and nitro used, simulates the other races of the day, lets rivals earn
        /// better cars, opens tracks by the best rank, crowns the champion, and starts the next day with its offers.
        /// </summary>
        public static CareerReport Apply(CareerState state, RaceResult result)
        {
            RaceOffer offer = EnteredOffer(state, result);
            RacerResult player = PlayerResult(result);
            var report = new CareerReport
            {
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
            Difficulty difficulty = report.Difficulty;
            report.Place = player.Place;
            report.Finished = player.Finished;
            report.Wrecked = player.Wrecked;
            report.Kills = Mathf.Max(0, player.Kills);
            report.Prize = player.Wrecked ? 0 : CareerRules.Prize(difficulty, player.Place);
            report.Bounty = report.Kills * CareerRules.Bounty(difficulty);
            report.Cash = Mathf.Max(0, player.CashPicked);
            report.Points = player.Wrecked ? 0 : CareerRules.Points(difficulty, player.Place);
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
            state.consumables.Add(CareerShop.MineId, -report.MinesUsed);
            state.consumables.Add(CareerShop.RocketId, -report.RocketsUsed);
            state.consumables.Add(CareerShop.NitroId, -report.NitroUsed);
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
                int points = racer.Wrecked ? 0 : CareerRules.Points(report.Difficulty, racer.Place);
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
        /// The Warden races nobody but the player.
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
            var form = new List<(RivalState rival, float score, bool wrecked)>(field.Count);
            foreach (RivalState rival in field)
            {
                CarLoadout car = AiLoadout(rival.tier, state.day, offer.difficulty, random);
                float score = Rating(CareerRules.SkillOn(rival.skill, state.day), car) + Form(random);
                if (state.HasSabotage && state.sabotage == rival.id)
                {
                    score -= 1f - CareerRules.SabotageHealth;
                }
                form.Add((rival, score, random.Chance(CareerRules.WreckChance(offer.difficulty))));
            }
            form.Sort((a, b) => a.wrecked != b.wrecked ? a.wrecked.CompareTo(b.wrecked) : b.score.CompareTo(a.score));
            var race = new DayRace { Difficulty = offer.difficulty, Track = offer.track };
            for (int i = 0; i < form.Count; i++)
            {
                RivalState rival = form[i].rival;
                bool wrecked = form[i].wrecked;
                rival.points += wrecked ? 0 : CareerRules.Points(offer.difficulty, i + 1);
                rival.races++;
                rival.wins += i == 0 && !wrecked ? 1 : 0;
                race.Order.Add(rival.id);
                race.Wrecked.Add(wrecked);
            }
            return race;
        }

        /// <summary>
        /// How strong a driver in a car is, 0 to 1: half their skill, half the car (its tier, its upgrades and equipment).
        /// What decides the races the player is not in, and what the tests' simulated player drives with.
        /// </summary>
        public static float Rating(float skill, CarLoadout car)
        {
            return 0.5f * Mathf.Clamp01(skill) + 0.5f * CarRating(car);
        }

        /// <summary>A car's strength, 0 (a Rustbucket off the lot) to 1 (The Warden's Tyrant).</summary>
        public static float CarRating(CarLoadout car)
        {
            if (car == null)
            {
                return 0f;
            }
            CarSpec spec = car.Spec;
            int levels = 0, max = 0;
            foreach (UpgradeKind kind in CareerRules.Upgrades)
            {
                levels += car.Level(kind);
                max += spec.MaxLevelOf(kind);
            }
            levels += (car.spikes ? 1 : 0) + (car.piercing ? 1 : 0);
            max += 2;
            float upgrades = max > 0 ? (float)levels / max : 0f;
            return Mathf.Clamp01((spec.Tier - 1 + upgrades) / (CareerRules.MaxRivalTier + 1));
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
            foreach (RivalState rival in state.rivals)
            {
                int earned = CareerRules.TierFor(Roster.IndexOf(rival.id), rival.points);
                if (earned > rival.tier)
                {
                    rival.tier = earned;
                    report.Promotions.Add(new Promotion { Id = rival.id, Name = rival.name, Tier = rival.tier, Car = rival.Car });
                }
            }
        }

        private static void OpenTracks(CareerState state, CareerReport report)
        {
            state.bestRank = Mathf.Min(state.bestRank, report.RankAfter);
            int open = Mathf.Min(CareerRules.ShowdownTrack, CareerRules.TracksOpenAt(state.bestRank));
            for (int track = state.tracksOpen; track < open; track++)
            {
                report.TracksOpened.Add(track);
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
        /// bottom, a missing car back in the garage, the offers of the day, numbers within their bounds). Returns why it
        /// cannot be played (an English key), or null when it can.
        /// </summary>
        public static string Validate(CareerState state)
        {
            if (state == null)
            {
                return "The saved career is incomplete.";
            }
            if (state.version > CareerState.CurrentVersion)
            {
                return "This career was saved by a newer version of the game.";
            }
            if (state.rivals == null || state.garage == null || state.wallet == null)
            {
                return "The saved career is incomplete.";
            }
            state.version = CareerState.CurrentVersion;
            state.day = Mathf.Max(1, state.day);
            state.driverName = string.IsNullOrWhiteSpace(state.driverName) ? "Rookie" : state.driverName;
            state.colorIndex = Mathf.Clamp(state.colorIndex, 0, Colors - 1);
            state.consumables ??= new Inventory();
            state.offers ??= new List<RaceOffer>();
            state.sabotage ??= "";
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
                owned.loadout.health = Mathf.Clamp01(owned.loadout.health);
                foreach (UpgradeKind kind in CareerRules.Upgrades)
                {
                    owned.loadout.SetLevel(kind, owned.loadout.Level(kind));
                }
                owned.loadout.mines = owned.loadout.rockets = owned.loadout.nitro = 0;
            }
            state.consumables.Clamp(new CareerShop(state).Catalog);
            state.rivals.RemoveAll(rival => rival == null || Roster.IndexOf(rival.id) < 0);
            for (int i = 0; i < Roster.Rivals.Length; i++)
            {
                if (state.Rival(Roster.Rivals[i].Id) == null)
                {
                    state.rivals.Add(new RivalState
                    {
                        id = Roster.Rivals[i].Id, name = Roster.Rivals[i].Name, tier = CareerRules.StartTier(i), skill = CareerRules.StartSkill(i)
                    });
                }
            }
            foreach (RivalState rival in state.rivals)
            {
                RosterDriver driver = Roster.Find(rival.id);
                rival.name = string.IsNullOrEmpty(rival.name) ? driver.Name : rival.name;
                rival.tier = Mathf.Clamp(rival.tier, 1, CareerRules.MaxRivalTier);
                rival.skill = Mathf.Clamp01(rival.skill);
                rival.points = Mathf.Max(0, rival.points);
            }
            AssignColors(state);
            state.tracksOpen = Mathf.Clamp(state.tracksOpen, CareerRules.StartTracks, CareerRules.ShowdownTrack);
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
