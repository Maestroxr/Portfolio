using System;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// Every number of the career in one place: what a career starts with, what the garage, the black market and the
    /// dealer charge, what a race pays and scores, how the computer's drivers grow over the days and which tracks the
    /// ladder opens. Tuned so a career climbs from twentieth to first in about 25 to 40 races: early on money is tight
    /// (an Easy win barely pays for the repairs and a mine or two), later a Hard race in a good car pays for a new part
    /// every day. The simulated career of the tests keeps the tuning honest.
    /// </summary>
    public static class CareerRules
    {
        /// <summary>The drivers of the ladder: the player and the rivals.</summary>
        public const int LadderSize = 20;

        public const int RivalCount = LadderSize - 1;

        // ------------------------------------------------------------------------------------------------
        // The start
        // ------------------------------------------------------------------------------------------------

        public const CarModel StartCar = CarModel.Rustbucket;
        public const int StartMoney = 1500;
        public const int StartMines = 2;
        public const int StartRockets = 0;
        public const int StartNitro = 2;

        /// <summary>The points of the best rival on the first day; the others are spread evenly down to zero.</summary>
        public const int TopRivalStartPoints = 40;

        /// <summary>The tracks open on the first day of a career (Dust Bowl, Scrapyard Loop, Frostbite Pass).</summary>
        public const int StartTracks = 3;

        /// <summary>The race against the champion is on this track (The Crucible), never on an ordinary day.</summary>
        public const int ShowdownTrack = 8;

        // ------------------------------------------------------------------------------------------------
        // The garage
        // ------------------------------------------------------------------------------------------------

        /// <summary>Prices are rounded to this many dollars, so the shelves show tidy numbers.</summary>
        public const int PriceStep = 50;

        /// <summary>The price of the first level of each <see cref="UpgradeKind"/> on a tier 1 car.</summary>
        private static readonly int[] UpgradeBase = { 450, 320, 380 };

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

        private const int SpikesBase = 900;
        private const int PiercingBase = 1400;
        private const double EquipmentTierExponent = 0.8;

        // ------------------------------------------------------------------------------------------------
        // The black market
        // ------------------------------------------------------------------------------------------------

        public const int MinePrice = 100;
        public const int MineMax = 8;
        public const int RocketPrice = 250;
        public const int RocketMax = 6;
        public const int NitroPrice = 150;
        public const int NitroMax = 6;

        /// <summary>The share of a mine's, rocket's or nitro's price the market pays when one is sold back.</summary>
        public const float MarketResale = 0.5f;

        private const int SabotageBase = 400;

        /// <summary>A sabotaged driver starts the race with this share of their health.</summary>
        public const float SabotageHealth = 0.55f;

        // ------------------------------------------------------------------------------------------------
        // Races
        // ------------------------------------------------------------------------------------------------

        /// <summary>Prize money for places 1 to 4, per <see cref="Difficulty"/> (Easy, Medium, Hard, Showdown).</summary>
        private static readonly int[][] PrizeTable =
        {
            new[] { 1000, 550, 250, 50 },
            new[] { 3500, 2000, 700, 150 },
            new[] { 7500, 4200, 1800, 500 },
            new[] { 30000, 0, 0, 0 }
        };

        /// <summary>Ladder points for places 1 to 4, per <see cref="Difficulty"/>; the Showdown is for the title, not points.</summary>
        private static readonly int[][] PointTable =
        {
            new[] { 3, 2, 1, 0 },
            new[] { 6, 4, 2, 1 },
            new[] { 10, 7, 4, 2 },
            new[] { 0, 0, 0, 0 }
        };

        /// <summary>The bounty for every car wrecked, per <see cref="Difficulty"/>.</summary>
        private static readonly int[] BountyTable = { 250, 500, 1200, 3000 };

        /// <summary>What a cash pickup on the track is worth, per <see cref="Difficulty"/>.</summary>
        private static readonly int[] CashTable = { 50, 100, 200, 250 };

        /// <summary>The cars of a race: the player and three others.</summary>
        public const int CarsPerRace = 4;

        public const int OffersPerDay = 3;

        // ------------------------------------------------------------------------------------------------
        // The rivals
        // ------------------------------------------------------------------------------------------------

        /// <summary>The highest tier of a rival's car (a Reaper); the Tyrant is The Warden's alone.</summary>
        public const int MaxRivalTier = 5;

        /// <summary>
        /// A rival moves up a tier for every this many points they score after the first day (never down, never past
        /// <see cref="MaxRivalTier"/>): the leaders every couple of weeks, the bottom of the ladder hardly ever.
        /// </summary>
        public const int PointsPerTier = 80;

        /// <summary>
        /// The tier of each rival's car on the first day, bottom of the ladder first: Rustbuckets at the bottom, Vipers at
        /// the top and one Reaper, The Undertaker's. The others earn theirs (<see cref="PointsPerTier"/>).
        /// </summary>
        private static readonly int[] StartTiers = { 1, 1, 1, 1, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 4, 4, 4, 4, 5 };

        /// <summary>The computer's cars get one more upgrade level on every part every this many days.</summary>
        public const int DaysPerAiUpgrade = 15;

        /// <summary>The day from which tier 3 and better rivals bolt on spiked bumpers, and tier 4 and better armour-piercing rounds.</summary>
        public const int AiSpikesDay = 8;

        public const int AiPiercingDay = 14;

        /// <summary>How much better a rival drives every day, and the most any rival reaches.</summary>
        public const float SkillPerDay = 0.001f;

        public const float MaxRivalSkill = 0.95f;

        /// <summary>The skill of the worst and the best rival on the first day.</summary>
        public const float BottomSkill = 0.3f;

        public const float TopSkill = 0.8f;

        /// <summary>
        /// How well the computer drives in a race of each <see cref="Difficulty"/> (the driving AI is tuned for these
        /// spreads): the weakest driver of a race at the bottom of its band, the strongest at the top. The Warden is 1.
        /// </summary>
        private static readonly float[][] AiSkillBands =
        {
            new[] { 0.10f, 0.30f },
            new[] { 0.40f, 0.60f },
            new[] { 0.70f, 0.90f },
            new[] { 1f, 1f }
        };

        /// <summary>How far into its band a driver's skill moves as the season goes on (at most), so late races are harder.</summary>
        public const float SeasonSkillShare = 0.15f;

        /// <summary>The season over which a driver's skill moves up its band.</summary>
        public const int SeasonDays = 40;

        /// <summary>What a computer car carries into a race: mines, rockets, nitro per <see cref="Difficulty"/>.</summary>
        private static readonly int[][] AiConsumables =
        {
            new[] { 1, 0, 1 },
            new[] { 2, 1, 2 },
            new[] { 3, 2, 2 },
            new[] { 4, 3, 3 }
        };

        /// <summary>How likely a car is wrecked in a race the player does not drive in, per <see cref="Difficulty"/> (percent).</summary>
        private static readonly int[] SimulatedWreckChance = { 6, 8, 11, 14 };

        /// <summary>How much luck decides a race the player does not drive in (the spread of a driver's form).</summary>
        public const float SimulatedForm = 0.28f;

        // ------------------------------------------------------------------------------------------------
        // The ladder opens tracks
        // ------------------------------------------------------------------------------------------------

        /// <summary>
        /// Reaching each of these ranks opens one more track (Neon Mile, Canyon Run, Smelter, Glacier Ring, Downtown
        /// Carnage). A rank once reached is kept.
        /// </summary>
        private static readonly int[] TrackRanks = { 16, 12, 9, 6, 3 };

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
            return RoundPrice(UpgradeBase[(int)kind] * Math.Pow(Math.Max(1, tier), UpgradeTierExponent) * Math.Pow(level, UpgradeLevelExponent));
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

        public static int SpikesPrice(int tier)
        {
            return RoundPrice(SpikesBase * Math.Pow(Math.Max(1, tier), EquipmentTierExponent));
        }

        public static int PiercingPrice(int tier)
        {
            return RoundPrice(PiercingBase * Math.Pow(Math.Max(1, tier), EquipmentTierExponent));
        }

        /// <summary>Sabotage costs more the better the driver (the race they are offered in).</summary>
        public static int SabotagePrice(Difficulty difficulty)
        {
            return SabotageBase * ((int)difficulty + 1);
        }

        /// <summary>What the dealer pays for <paramref name="car"/>: half its price and half of what its upgrades and equipment cost.</summary>
        public static int TradeIn(CarLoadout car)
        {
            if (car == null)
            {
                return 0;
            }
            return RoundPrice(TradeInShare * (car.Spec.Price + Invested(car)));
        }

        /// <summary>What was spent on <paramref name="car"/> beyond its price: its upgrade levels and its equipment.</summary>
        public static int Invested(CarLoadout car)
        {
            int tier = car.Spec.Tier;
            int total = 0;
            foreach (UpgradeKind kind in Upgrades)
            {
                total += UpgradesCost(kind, tier, car.Level(kind));
            }
            total += car.spikes ? SpikesPrice(tier) : 0;
            total += car.piercing ? PiercingPrice(tier) : 0;
            return total;
        }

        /// <summary>The upgrade kinds in the order the garage lists them.</summary>
        public static readonly UpgradeKind[] Upgrades = { UpgradeKind.Engine, UpgradeKind.Tires, UpgradeKind.Armor };

        // ------------------------------------------------------------------------------------------------
        // Races
        // ------------------------------------------------------------------------------------------------

        /// <summary>The prize of <paramref name="place"/> (from 1) in a race of <paramref name="difficulty"/>; nothing past fourth.</summary>
        public static int Prize(Difficulty difficulty, int place)
        {
            int[] row = PrizeTable[Index(difficulty)];
            return place >= 1 && place <= row.Length ? row[place - 1] : 0;
        }

        public static int Points(Difficulty difficulty, int place)
        {
            int[] row = PointTable[Index(difficulty)];
            return place >= 1 && place <= row.Length ? row[place - 1] : 0;
        }

        public static int Bounty(Difficulty difficulty)
        {
            return BountyTable[Index(difficulty)];
        }

        public static int CashPerPickup(Difficulty difficulty)
        {
            return CashTable[Index(difficulty)];
        }

        public static int WreckChance(Difficulty difficulty)
        {
            return SimulatedWreckChance[Index(difficulty)];
        }

        /// <summary>The mines, rockets and nitro a computer car carries into a race of <paramref name="difficulty"/>.</summary>
        public static (int mines, int rockets, int nitro) AiCarries(Difficulty difficulty)
        {
            int[] row = AiConsumables[Index(difficulty)];
            return (row[0], row[1], row[2]);
        }

        /// <summary>The laps of a race of <paramref name="difficulty"/> on a track of <paramref name="trackLaps"/> laps: an Easy race is a lap shorter.</summary>
        public static int Laps(Difficulty difficulty, int trackLaps)
        {
            return difficulty == Difficulty.Easy ? Math.Max(3, trackLaps - 1) : Math.Max(1, trackLaps);
        }

        // ------------------------------------------------------------------------------------------------
        // The rivals and the ladder
        // ------------------------------------------------------------------------------------------------

        /// <summary>The points of the rival at roster position <paramref name="index"/> (0 = the worst) on the first day.</summary>
        public static int StartPoints(int index)
        {
            return (int)Math.Round((double)TopRivalStartPoints * index / (RivalCount - 1), MidpointRounding.AwayFromZero);
        }

        /// <summary>The tier of the rival at roster position <paramref name="index"/> on the first day: 1 at the bottom to 5 at the top.</summary>
        public static int StartTier(int index)
        {
            return StartTiers[Math.Max(0, Math.Min(index, StartTiers.Length - 1))];
        }

        public static float StartSkill(int index)
        {
            return BottomSkill + (TopSkill - BottomSkill) * index / (RivalCount - 1);
        }

        /// <summary>The tier the rival at roster position <paramref name="index"/> has earned with <paramref name="points"/> (they keep a higher one).</summary>
        public static int TierFor(int index, int points)
        {
            int earned = Math.Max(0, points - StartPoints(index)) / PointsPerTier;
            return Math.Min(MaxRivalTier, StartTier(index) + earned);
        }

        /// <summary>A rival's skill on <paramref name="day"/>: drivers sharpen as the season goes on.</summary>
        public static float SkillOn(float baseSkill, int day)
        {
            return Math.Min(MaxRivalSkill, baseSkill + SkillPerDay * Math.Max(0, day - 1));
        }

        /// <summary>
        /// The driving skill the AI gets in a race of <paramref name="difficulty"/> for a driver at <paramref name="share"/>
        /// (0 the weakest, 1 the strongest) of the drivers it is drawn from.
        /// </summary>
        public static float AiSkill(Difficulty difficulty, float share)
        {
            float[] band = AiSkillBands[Index(difficulty)];
            return band[0] + (band[1] - band[0]) * Math.Max(0f, Math.Min(1f, share));
        }

        /// <summary>The upgrade level of every part of a computer car on <paramref name="day"/>; a Hard race gets one more.</summary>
        public static int AiLevel(int day, Difficulty difficulty)
        {
            int level = Math.Max(0, day - 1) / DaysPerAiUpgrade;
            return level + (difficulty >= Difficulty.Hard ? 1 : 0);
        }

        /// <summary>How many tracks are open for a career whose best rank so far is <paramref name="bestRank"/>.</summary>
        public static int TracksOpenAt(int bestRank)
        {
            int open = StartTracks;
            foreach (int rank in TrackRanks)
            {
                if (bestRank <= rank)
                {
                    open++;
                }
            }
            return open;
        }

        /// <summary>The rank that opens track <paramref name="track"/>, or 0 for one open from the start (or not opened by the ladder).</summary>
        public static int RankOpening(int track)
        {
            int index = track - StartTracks;
            return index >= 0 && index < TrackRanks.Length ? TrackRanks[index] : 0;
        }

        private static int Index(Difficulty difficulty)
        {
            return Math.Max(0, Math.Min((int)difficulty, (int)Difficulty.Showdown));
        }
    }
}
