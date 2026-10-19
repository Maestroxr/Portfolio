using System;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// Every number of the career in one place: what the garage, the workshop, the black market and the dealer charge,
    /// and, from the league's table (<see cref="LeagueSpec"/>), what a career starts with, what a race pays and scores,
    /// how the computer's drivers grow over the days and which tracks the ladder opens. Tuned so a career of the first
    /// league climbs from twentieth to first in about 25 to 40 races and a career of a later league takes its title in
    /// about 30 to 50: early on money is tight (an Easy win barely pays for the repairs and a mine or two), later a Hard
    /// race in a good car pays for a new part every day. The simulated careers of the tests keep the tuning honest.
    ///
    /// The calls without a league are the first league's (the Wasteland Circuit), for what knows no league: the quick
    /// race, the HUD, the older tests.
    /// </summary>
    public static class CareerRules
    {
        /// <summary>The drivers of the ladder: the player and the rivals.</summary>
        public const int LadderSize = 20;

        public const int RivalCount = LadderSize - 1;

        // ------------------------------------------------------------------------------------------------
        // The start (of the first league)
        // ------------------------------------------------------------------------------------------------

        public static CarModel StartCar => Leagues.Wasteland.StartCar;
        public static int StartMoney => Leagues.Wasteland.StartMoney;
        public static int StartMines => Leagues.Wasteland.StartMines;
        public static int StartRockets => Leagues.Wasteland.StartRockets;
        public static int StartNitro => Leagues.Wasteland.StartNitro;

        /// <summary>The points of the best rival on the first day; the others are spread evenly down to zero.</summary>
        public static int TopRivalStartPoints => Leagues.Wasteland.TopRivalStartPoints;

        /// <summary>The tracks open on the first day of a career (Dust Bowl, Scrapyard Loop, Frostbite Pass).</summary>
        public static int StartTracks => Leagues.Wasteland.StartTracks;

        /// <summary>The race against the champion is on this track (The Crucible), never on an ordinary day.</summary>
        public static int ShowdownTrack => Leagues.Wasteland.ShowdownTrack;

        // ------------------------------------------------------------------------------------------------
        // The garage
        // ------------------------------------------------------------------------------------------------

        /// <summary>Prices are rounded to this many dollars, so the shelves show tidy numbers.</summary>
        public const int PriceStep = 50;

        /// <summary>The price of the first level of each <see cref="UpgradeKind"/> on a tier 1 car (engine, tyres, armour, nitro system, gun mount).</summary>
        private static readonly int[] UpgradeBase = { 450, 320, 380, 280, 360 };

        private const double UpgradeTierExponent = 1.3;
        private const double UpgradeLevelExponent = 1.5;

        /// <summary>A point of health costs this much per tier of the car: a better car is dearer to mend.</summary>
        public const int RepairPerTier = 3;

        /// <summary>The share of a car's price and of what was spent on it that the dealer gives for it.</summary>
        public const float TradeInShare = 0.5f;

        /// <summary>
        /// A car never starts a race with less than this share of its health: the crew patches a wreck up for free, so a
        /// career without money can always race on.
        /// </summary>
        public const float MinStartHealth = 0.2f;

        /// <summary>The fittings and the guns cost their base price (<see cref="EquipmentSpec.PriceBase"/>, <see cref="GunSpec.PriceBase"/>) times the car's tier to this power.</summary>
        private const double EquipmentTierExponent = 0.8;

        // ------------------------------------------------------------------------------------------------
        // The black market (the special weapons' prices and maximums are theirs: Specials)
        // ------------------------------------------------------------------------------------------------

        public const int MinePrice = 100;
        public const int MineMax = 8;
        public const int RocketPrice = 250;
        public const int RocketMax = 6;
        public const int NitroPrice = 150;
        public const int NitroMax = 6;

        /// <summary>The share of a mine's, rocket's, nitro's or special weapon's price the market pays when one is sold back.</summary>
        public const float MarketResale = 0.5f;

        private const int SabotageBase = 400;

        /// <summary>A sabotaged driver starts the race with this share of their health.</summary>
        public const float SabotageHealth = 0.55f;

        // ------------------------------------------------------------------------------------------------
        // Races
        // ------------------------------------------------------------------------------------------------

        /// <summary>The cars of a race: the player and three others.</summary>
        public const int CarsPerRace = 4;

        public const int OffersPerDay = 3;

        // ------------------------------------------------------------------------------------------------
        // The rivals (of the first league)
        // ------------------------------------------------------------------------------------------------

        /// <summary>The highest tier of a rival's car (a Reaper); the Tyrant is The Warden's alone.</summary>
        public static int MaxRivalTier => Leagues.Wasteland.MaxRivalTier;

        /// <summary>
        /// A rival moves up a tier for every this many points they score after the first day (never down, never past
        /// <see cref="MaxRivalTier"/>): the leaders every couple of weeks, the bottom of the ladder hardly ever.
        /// </summary>
        public static int PointsPerTier => Leagues.Wasteland.PointsPerTier;

        /// <summary>The computer's cars get one more upgrade level on every part every this many days.</summary>
        public static int DaysPerAiUpgrade => Leagues.Wasteland.DaysPerAiUpgrade;

        /// <summary>The day from which tier 3 and better rivals bolt on spiked bumpers, and tier 4 and better armour-piercing rounds.</summary>
        public static int AiSpikesDay => Leagues.Wasteland.AiSpikesDay;

        public static int AiPiercingDay => Leagues.Wasteland.AiPiercingDay;

        /// <summary>How much better a rival drives every day, and the most any rival reaches.</summary>
        public static float SkillPerDay => Leagues.Wasteland.SkillPerDay;

        public static float MaxRivalSkill => Leagues.Wasteland.MaxRivalSkill;

        /// <summary>The skill of the worst and the best rival on the first day.</summary>
        public static float BottomSkill => Leagues.Wasteland.BottomSkill;

        public static float TopSkill => Leagues.Wasteland.TopSkill;

        /// <summary>How far into its band a driver's skill moves as the season goes on (at most), so late races are harder.</summary>
        public const float SeasonSkillShare = 0.15f;

        /// <summary>The season over which a driver's skill moves up its band.</summary>
        public const int SeasonDays = 40;

        /// <summary>How much luck decides a race the player does not drive in (the spread of a driver's form).</summary>
        public const float SimulatedForm = 0.28f;

        // ------------------------------------------------------------------------------------------------
        // How much the parts of a car count for its strength (Career.CarRating)
        // ------------------------------------------------------------------------------------------------

        /// <summary>
        /// The parts a car had before the workshop (the engine, tyres and armour levels, the spiked bumper and the
        /// armour-piercing rounds) are worth up to one tier, as they always were; the parts after them (the nitro system and
        /// gun mount levels, the five later fittings and a gun) up to this share of a tier more.
        /// </summary>
        public const float ExtraPartsTiers = 0.5f;

        /// <summary>A gun counts for this many of the later parts (a level of the nitro system or a fitting counts for one).</summary>
        public const int GunExtraParts = 2;

        // ------------------------------------------------------------------------------------------------
        // Prices
        // ------------------------------------------------------------------------------------------------

        public static int RoundPrice(double price)
        {
            return (int)(Math.Round(price / PriceStep, MidpointRounding.AwayFromZero) * PriceStep);
        }

        /// <summary>The price of taking <paramref name="kind"/> from level <paramref name="level"/> - 1 to <paramref name="level"/> on a car of <paramref name="tier"/>.</summary>
        public static int UpgradePrice(UpgradeKind kind, int tier, int level)
        {
            if (level <= 0)
            {
                return 0;
            }
            int index = Math.Max(0, Math.Min((int)kind, UpgradeBase.Length - 1));
            return RoundPrice(UpgradeBase[index] * Math.Pow(Math.Max(1, tier), UpgradeTierExponent) * Math.Pow(level, UpgradeLevelExponent));
        }

        /// <summary>What every level of <paramref name="kind"/> up to <paramref name="level"/> cost together.</summary>
        public static int UpgradesCost(UpgradeKind kind, int tier, int level)
        {
            int total = 0;
            for (int n = 1; n <= level; n++)
            {
                total += UpgradePrice(kind, tier, n);
            }
            return total;
        }

        public static int RepairPrice(int tier)
        {
            return RepairPerTier * Math.Max(1, tier);
        }

        /// <summary>The price of fitting <paramref name="kind"/> to a car of <paramref name="tier"/>.</summary>
        public static int FittingPrice(EquipmentKind kind, int tier)
        {
            return RoundPrice(Equipment.Of(kind).PriceBase * Math.Pow(Math.Max(1, tier), EquipmentTierExponent));
        }

        public static int SpikesPrice(int tier)
        {
            return FittingPrice(EquipmentKind.Spikes, tier);
        }

        public static int PiercingPrice(int tier)
        {
            return FittingPrice(EquipmentKind.Piercing, tier);
        }

        /// <summary>The price of the gun <paramref name="kind"/> for a car of <paramref name="tier"/> (the machine gun comes with every car).</summary>
        public static int GunPrice(GunKind kind, int tier)
        {
            return kind == GunKind.MachineGun ? 0 : RoundPrice(Guns.Of(kind).PriceBase * Math.Pow(Math.Max(1, tier), EquipmentTierExponent));
        }

        /// <summary>Sabotage costs more the better the driver (the race they are offered in).</summary>
        public static int SabotagePrice(Difficulty difficulty)
        {
            return SabotageBase * ((int)difficulty + 1);
        }

        /// <summary>What the dealer pays for <paramref name="car"/>: half its price and half of what its upgrades, fittings and guns cost.</summary>
        public static int TradeIn(CarLoadout car)
        {
            if (car == null)
            {
                return 0;
            }
            return RoundPrice(TradeInShare * (car.Spec.Price + Invested(car)));
        }

        /// <summary>What was spent on <paramref name="car"/> beyond its price: its upgrade levels, its fittings and the guns bought for it.</summary>
        public static int Invested(CarLoadout car)
        {
            int tier = car.Spec.Tier;
            int total = 0;
            foreach (UpgradeKind kind in Upgrades)
            {
                total += UpgradesCost(kind, tier, car.Level(kind));
            }
            foreach (EquipmentKind kind in Fittings)
            {
                total += car.Has(kind) ? FittingPrice(kind, tier) : 0;
            }
            foreach (GunKind kind in BoughtGuns)
            {
                total += car.OwnsGun(kind) ? GunPrice(kind, tier) : 0;
            }
            return total;
        }

        /// <summary>The upgrade kinds in the order the garage lists them.</summary>
        public static readonly UpgradeKind[] Upgrades =
        {
            UpgradeKind.Engine, UpgradeKind.Tires, UpgradeKind.Armor, UpgradeKind.Boost, UpgradeKind.GunMount
        };

        /// <summary>The upgrades the career had before the nitro system and the gun mount (what the AI's luck goes by).</summary>
        public static readonly UpgradeKind[] ClassicUpgrades = { UpgradeKind.Engine, UpgradeKind.Tires, UpgradeKind.Armor };

        /// <summary>The fittings the career had before the workshop.</summary>
        public static readonly EquipmentKind[] ClassicFittings = { EquipmentKind.Spikes, EquipmentKind.Piercing };

        /// <summary>The fittings in the order the workshop lists them.</summary>
        public static readonly EquipmentKind[] Fittings =
        {
            EquipmentKind.Spikes, EquipmentKind.Piercing, EquipmentKind.Cage, EquipmentKind.Shield, EquipmentKind.Jammer, EquipmentKind.Welder,
            EquipmentKind.Magazine
        };

        /// <summary>The fittings the computer's drivers bolt on one by one from the league's fittings day (not the bumper and the rounds).</summary>
        public static readonly EquipmentKind[] LaterFittings =
        {
            EquipmentKind.Cage, EquipmentKind.Shield, EquipmentKind.Jammer, EquipmentKind.Welder, EquipmentKind.Magazine
        };

        /// <summary>The upgrades the career had before the workshop (the nitro system and gun mount came with it).</summary>
        public static readonly UpgradeKind[] LaterUpgrades = { UpgradeKind.Boost, UpgradeKind.GunMount };

        /// <summary>The guns the workshop sells (the machine gun comes with the car).</summary>
        public static readonly GunKind[] BoughtGuns = { GunKind.Minigun, GunKind.Flamethrower, GunKind.Plasma };

        // ------------------------------------------------------------------------------------------------
        // Races
        // ------------------------------------------------------------------------------------------------

        /// <summary>The prize of <paramref name="place"/> (from 1) in a race of <paramref name="difficulty"/>; nothing past fourth.</summary>
        public static int Prize(LeagueSpec league, Difficulty difficulty, int place)
        {
            int[] row = league.PrizeTable[Index(difficulty)];
            return place >= 1 && place <= row.Length ? row[place - 1] : 0;
        }

        public static int Points(LeagueSpec league, Difficulty difficulty, int place)
        {
            int[] row = league.PointTable[Index(difficulty)];
            return place >= 1 && place <= row.Length ? row[place - 1] : 0;
        }

        public static int Bounty(LeagueSpec league, Difficulty difficulty)
        {
            return league.BountyTable[Index(difficulty)];
        }

        public static int CashPerPickup(LeagueSpec league, Difficulty difficulty)
        {
            return league.CashTable[Index(difficulty)];
        }

        public static int WreckChance(LeagueSpec league, Difficulty difficulty)
        {
            return league.SimulatedWreckChance[Index(difficulty)];
        }

        /// <summary>The mines, rockets and nitro a computer car carries into a race of <paramref name="difficulty"/>.</summary>
        public static (int mines, int rockets, int nitro) AiCarries(LeagueSpec league, Difficulty difficulty)
        {
            int[] row = league.AiConsumables[Index(difficulty)];
            return (row[0], row[1], row[2]);
        }

        /// <summary>How many special weapons a computer car carries into a race of <paramref name="difficulty"/>.</summary>
        public static int AiSpecials(LeagueSpec league, Difficulty difficulty)
        {
            return league.AiSpecials[Index(difficulty)];
        }

        /// <summary>Whether the computer's cars of a race of <paramref name="difficulty"/> on <paramref name="day"/> mount guns of their own.</summary>
        public static bool AiMountsGun(LeagueSpec league, int day, Difficulty difficulty)
        {
            return day >= league.AiGunDays[Index(difficulty)];
        }

        /// <summary>How many of the later fittings (<see cref="LaterFittings"/>) a computer car has on <paramref name="day"/>.</summary>
        public static int AiFittings(LeagueSpec league, int day, Difficulty difficulty)
        {
            if (day < league.AiFittingsDay)
            {
                return 0;
            }
            int count = 1 + (day - league.AiFittingsDay) / Math.Max(1, league.DaysPerAiFitting);
            count += difficulty >= Difficulty.Hard ? 1 : difficulty == Difficulty.Easy ? -1 : 0;
            return Math.Max(0, Math.Min(LaterFittings.Length, count));
        }

        /// <summary>The laps of a race of <paramref name="difficulty"/> on a track of <paramref name="trackLaps"/> laps: an Easy race is a lap shorter.</summary>
        public static int Laps(Difficulty difficulty, int trackLaps)
        {
            return difficulty == Difficulty.Easy ? Math.Max(3, trackLaps - 1) : Math.Max(1, trackLaps);
        }

        // The first league's (what knows no league: the quick race, the HUD).

        public static int Prize(Difficulty difficulty, int place)
        {
            return Prize(Leagues.Wasteland, difficulty, place);
        }

        public static int Points(Difficulty difficulty, int place)
        {
            return Points(Leagues.Wasteland, difficulty, place);
        }

        public static int Bounty(Difficulty difficulty)
        {
            return Bounty(Leagues.Wasteland, difficulty);
        }

        public static int CashPerPickup(Difficulty difficulty)
        {
            return CashPerPickup(Leagues.Wasteland, difficulty);
        }

        public static int WreckChance(Difficulty difficulty)
        {
            return WreckChance(Leagues.Wasteland, difficulty);
        }

        public static (int mines, int rockets, int nitro) AiCarries(Difficulty difficulty)
        {
            return AiCarries(Leagues.Wasteland, difficulty);
        }

        // ------------------------------------------------------------------------------------------------
        // The rivals and the ladder
        // ------------------------------------------------------------------------------------------------

        /// <summary>The points of the rival at roster position <paramref name="index"/> (0 = the worst) on the first day.</summary>
        public static int StartPoints(LeagueSpec league, int index)
        {
            return (int)Math.Round((double)league.TopRivalStartPoints * index / (RivalCount - 1), MidpointRounding.AwayFromZero);
        }

        /// <summary>The tier of the rival at roster position <paramref name="index"/> on the first day (1 at the bottom to 5 at the top of the first league).</summary>
        public static int StartTier(LeagueSpec league, int index)
        {
            int[] tiers = league.StartTiers;
            return tiers[Math.Max(0, Math.Min(index, tiers.Length - 1))];
        }

        public static float StartSkill(LeagueSpec league, int index)
        {
            return league.BottomSkill + (league.TopSkill - league.BottomSkill) * index / (RivalCount - 1);
        }

        /// <summary>The tier the rival at roster position <paramref name="index"/> has earned with <paramref name="points"/> (they keep a higher one).</summary>
        public static int TierFor(LeagueSpec league, int index, int points)
        {
            int earned = Math.Max(0, points - StartPoints(league, index)) / league.PointsPerTier;
            return Math.Min(league.MaxRivalTier, StartTier(league, index) + earned);
        }

        /// <summary>A rival's skill on <paramref name="day"/>: drivers sharpen as the season goes on.</summary>
        public static float SkillOn(LeagueSpec league, float baseSkill, int day)
        {
            return Math.Min(league.MaxRivalSkill, baseSkill + league.SkillPerDay * Math.Max(0, day - 1));
        }

        /// <summary>
        /// The driving skill the AI gets in a race of <paramref name="difficulty"/> for a driver at <paramref name="share"/>
        /// (0 the weakest, 1 the strongest) of the drivers it is drawn from.
        /// </summary>
        public static float AiSkill(LeagueSpec league, Difficulty difficulty, float share)
        {
            float[] band = league.AiSkillBands[Index(difficulty)];
            return band[0] + (band[1] - band[0]) * Math.Max(0f, Math.Min(1f, share));
        }

        /// <summary>The upgrade level of every part of a computer car on <paramref name="day"/>; a Hard race gets one more.</summary>
        public static int AiLevel(LeagueSpec league, int day, Difficulty difficulty)
        {
            int level = league.AiLevelBonus + Math.Max(0, day - 1) / Math.Max(1, league.DaysPerAiUpgrade);
            return level + (difficulty >= Difficulty.Hard ? 1 : 0);
        }

        /// <summary>How many of the league's tracks are open for a career whose best rank so far is <paramref name="bestRank"/>.</summary>
        public static int TracksOpenAt(LeagueSpec league, int bestRank)
        {
            int open = league.StartTracks;
            foreach (int rank in league.TrackRanks)
            {
                if (bestRank <= rank)
                {
                    open++;
                }
            }
            return Math.Min(open, league.Tracks.Length);
        }

        /// <summary>The rank that opens track <paramref name="track"/> in <paramref name="league"/>, or 0 for one open from the start (or not opened by the ladder).</summary>
        public static int RankOpening(LeagueSpec league, int track)
        {
            int index = league.TrackOrder(track) - league.StartTracks;
            return index >= 0 && index < league.TrackRanks.Length ? league.TrackRanks[index] : 0;
        }

        // The first league's.

        public static int StartPoints(int index)
        {
            return StartPoints(Leagues.Wasteland, index);
        }

        public static int StartTier(int index)
        {
            return StartTier(Leagues.Wasteland, index);
        }

        public static float StartSkill(int index)
        {
            return StartSkill(Leagues.Wasteland, index);
        }

        public static int TierFor(int index, int points)
        {
            return TierFor(Leagues.Wasteland, index, points);
        }

        public static float SkillOn(float baseSkill, int day)
        {
            return SkillOn(Leagues.Wasteland, baseSkill, day);
        }

        public static float AiSkill(Difficulty difficulty, float share)
        {
            return AiSkill(Leagues.Wasteland, difficulty, share);
        }

        public static int AiLevel(int day, Difficulty difficulty)
        {
            return AiLevel(Leagues.Wasteland, day, difficulty);
        }

        public static int TracksOpenAt(int bestRank)
        {
            return TracksOpenAt(Leagues.Wasteland, bestRank);
        }

        /// <summary>The rank of a career of the first league that opens <paramref name="track"/> (what the quick race window says opens it).</summary>
        public static int RankOpening(int track)
        {
            return RankOpening(Leagues.Wasteland, track);
        }

        private static int Index(Difficulty difficulty)
        {
            return Math.Max(0, Math.Min((int)difficulty, (int)Difficulty.Showdown));
        }
    }
}
