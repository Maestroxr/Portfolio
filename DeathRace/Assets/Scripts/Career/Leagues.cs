using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>The career's leagues, each a ladder of its own with a champion at the top. The numbers are what saves keep.</summary>
    public enum LeagueKind
    {
        /// <summary>The first career: a Rustbucket, twenty drivers and The Warden.</summary>
        Wasteland = 0,

        /// <summary>The second: tier 3 to 8 cars, sharper drivers, bigger purses and The Iron Baron.</summary>
        Iron = 1,

        /// <summary>The last: tier 5 to 9 cars, the best drivers alive and Nemesis.</summary>
        Hellfire = 2
    }

    /// <summary>
    /// Everything that makes one league of the career what it is: what a career in it starts with, its nineteen rivals
    /// and its champion, the cars the rivals drive and how they grow, how well they drive, what races pay and score,
    /// the tracks and the order they open in, and the cars its dealer sells. <see cref="CareerRules"/> works the numbers
    /// out from the league of the career (<see cref="CareerState.League"/>).
    ///
    /// A league opens on a device once the league before it has been won there (<see cref="DeathRaceProgress.IsLeagueOpen"/>).
    /// </summary>
    public sealed class LeagueSpec
    {
        public LeagueKind Kind;

        /// <summary>The league's name and a line about it (English keys of the localization table).</summary>
        public string Name;

        public string Blurb;

        // The start

        public CarModel StartCar;
        public int StartMoney;
        public int StartMines;
        public int StartRockets;
        public int StartNitro;
        public int StartOil;
        public int StartEmp;
        public int StartMortar;

        // The ladder

        /// <summary>The rivals, bottom of the ladder first (ids unique across the leagues).</summary>
        public RosterDriver[] Rivals;

        /// <summary>The champion who waits above the ladder and races the Showdown.</summary>
        public RosterDriver Champion;

        /// <summary>The champion's car (every upgrade and fitting on it), its gun, the special it fires first, and its paint.</summary>
        public CarModel ChampionCar;

        public GunKind ChampionGun;
        public SpecialKind ChampionSpecial;
        public Color ChampionPaint;

        /// <summary>The points of the best rival on the first day; the others are spread evenly down to zero.</summary>
        public int TopRivalStartPoints;

        /// <summary>The tier of each rival's car on the first day, bottom of the ladder first.</summary>
        public int[] StartTiers;

        /// <summary>The highest tier a rival earns (the champion's car is a tier above).</summary>
        public int MaxRivalTier;

        /// <summary>A rival moves up a tier for every this many points they score after the first day.</summary>
        public int PointsPerTier;

        /// <summary>The skill of the worst and the best rival on the first day, how much they sharpen a day and the most they reach.</summary>
        public float BottomSkill;

        public float TopSkill;
        public float SkillPerDay;
        public float MaxRivalSkill;

        /// <summary>The AI's skill band of a race per <see cref="Difficulty"/> (Easy, Medium, Hard, Showdown): lowest, highest.</summary>
        public float[][] AiSkillBands;

        // Races

        /// <summary>Prize money for places 1 to 4 per <see cref="Difficulty"/>.</summary>
        public int[][] PrizeTable;

        /// <summary>Ladder points for places 1 to 4 per <see cref="Difficulty"/>.</summary>
        public int[][] PointTable;

        /// <summary>The bounty for every car wrecked, per <see cref="Difficulty"/>.</summary>
        public int[] BountyTable;

        /// <summary>What a cash pickup is worth, per <see cref="Difficulty"/>.</summary>
        public int[] CashTable;

        /// <summary>How likely a car is wrecked in a race the player does not drive in, per <see cref="Difficulty"/> (percent).</summary>
        public int[] SimulatedWreckChance;

        // The computer's cars

        /// <summary>Upgrade levels every computer car has on the first day, on top of the ones the days bring.</summary>
        public int AiLevelBonus;

        /// <summary>The computer's cars get one more upgrade level on every part every this many days.</summary>
        public int DaysPerAiUpgrade;

        /// <summary>The day from which the computer's cars have the nitro system and the gun mount at the level of their other parts (none before).</summary>
        public int AiLaterUpgradesDay = 1;

        /// <summary>The days from which tier 3 and better rivals bolt on spiked bumpers, and tier 4 and better armour-piercing rounds.</summary>
        public int AiSpikesDay;

        public int AiPiercingDay;

        /// <summary>
        /// The day from which the rivals bolt on the other fittings (roll cage, blast shielding, radar jammer, field welder,
        /// extended magazine), one of them at first and one more every <see cref="DaysPerAiFitting"/> days, each driver in
        /// an order of their own; a Hard race's cars have one more, an Easy race's one fewer.
        /// </summary>
        public int AiFittingsDay;

        public int DaysPerAiFitting;

        /// <summary>
        /// The day from which the computer's cars of a race of each <see cref="Difficulty"/> mount a gun of their own (the
        /// driver's liking: a minigun, flamethrower or plasma cannon) in place of the machine gun; <see cref="Leagues.Never"/>
        /// for never.
        /// </summary>
        public int[] AiGunDays;

        /// <summary>What a computer car carries into a race: mines, rockets, nitro per <see cref="Difficulty"/>.</summary>
        public int[][] AiConsumables;

        /// <summary>How many special weapons a computer car carries per <see cref="Difficulty"/> (of the driver's liking).</summary>
        public int[] AiSpecials;

        // Tracks

        /// <summary>The ordinary tracks of the league in the order they open (indices into <c>TrackLayouts.All</c>).</summary>
        public int[] Tracks;

        /// <summary>How many of <see cref="Tracks"/> are open on the first day.</summary>
        public int StartTracks;

        /// <summary>Reaching each of these ranks opens the next track of <see cref="Tracks"/> (a rank once reached is kept).</summary>
        public int[] TrackRanks;

        /// <summary>The Showdown's track and laps.</summary>
        public int ShowdownTrack;

        public int ShowdownLaps;

        // The dealer

        /// <summary>The tiers of the cars the dealer sells (the car being driven is listed whatever its tier).</summary>
        public int MinDealerTier;

        public int MaxDealerTier;

        public int Index => (int)Kind;

        /// <summary>The champion's portrait among the theme's driver portraits: after the twenty colours of the drivers.</summary>
        public int ChampionPortrait => CareerRules.LadderSize + Index;

        /// <summary>The league after this one, or null for the last.</summary>
        public LeagueSpec Next => Index + 1 < Leagues.Count ? Leagues.Of(Index + 1) : null;

        /// <summary>The league before this one (whose title opens it), or null for the first.</summary>
        public LeagueSpec Previous => Index > 0 ? Leagues.Of(Index - 1) : null;

        /// <summary>The roster position of the rival with <paramref name="id"/> (0 = the worst on the first day), or -1.</summary>
        public int IndexOf(string id)
        {
            for (int i = 0; i < Rivals.Length; i++)
            {
                if (string.Equals(Rivals[i].Id, id, System.StringComparison.Ordinal))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>The driver with <paramref name="id"/> (a rival or the champion), or null.</summary>
        public RosterDriver Find(string id)
        {
            if (Champion != null && id == Champion.Id)
            {
                return Champion;
            }
            int index = IndexOf(id);
            return index >= 0 ? Rivals[index] : null;
        }

        public bool IsChampion(string id)
        {
            return Champion != null && id == Champion.Id;
        }

        /// <summary>Whether the dealer of this league sells <paramref name="model"/>.</summary>
        public bool Sells(CarModel model)
        {
            int tier = CarSpecs.Of(model).Tier;
            return tier >= MinDealerTier && tier <= MaxDealerTier;
        }

        /// <summary>The position of <paramref name="track"/> in <see cref="Tracks"/> (the order it opens in), or -1.</summary>
        public int TrackOrder(int track)
        {
            return System.Array.IndexOf(Tracks, track);
        }
    }

    /// <summary>
    /// The three leagues of the career. The first is the career as it was before the leagues, number for number; the
    /// other two start where it ends (a Bulldog, then a Reaper), with rivals in better cars who drive better and fight
    /// with everything the shop sells, purses to match, and the higher tiers' cars at the dealer. Tuned with the
    /// simulated careers of the tests: an average player becomes champion of each in about 30 to 50 races, a few more in
    /// each league than in the one before.
    /// </summary>
    public static class Leagues
    {
        /// <summary>A day that never comes (for a schedule entry that does not apply).</summary>
        public const int Never = int.MaxValue;

        private static readonly int[][] Points =
        {
            new[] { 3, 2, 1, 0 },
            new[] { 6, 4, 2, 1 },
            new[] { 10, 7, 4, 2 },
            new[] { 0, 0, 0, 0 }
        };

        public static readonly LeagueSpec Wasteland = new LeagueSpec
        {
            Kind = LeagueKind.Wasteland,
            Name = "Wasteland Circuit",
            Blurb = "Twenty drivers, one champion. You start at the bottom with a wreck, a little money and a grudge.",
            StartCar = CarModel.Rustbucket,
            StartMoney = 1500,
            StartMines = 2,
            StartRockets = 0,
            StartNitro = 2,
            Rivals = Roster.Rivals,
            Champion = Roster.Warden,
            ChampionCar = CarModel.Tyrant,
            ChampionGun = GunKind.Flamethrower,
            ChampionSpecial = SpecialKind.Mortar,
            ChampionPaint = new Color(0.42f, 0.03f, 0.05f, 1f),
            TopRivalStartPoints = 40,
            StartTiers = new[] { 1, 1, 1, 1, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 4, 4, 4, 4, 5 },
            MaxRivalTier = 5,
            PointsPerTier = 80,
            BottomSkill = 0.3f,
            TopSkill = 0.8f,
            SkillPerDay = 0.001f,
            MaxRivalSkill = 0.95f,
            AiSkillBands = new[]
            {
                new[] { 0.10f, 0.30f },
                new[] { 0.40f, 0.60f },
                new[] { 0.70f, 0.90f },
                new[] { 1f, 1f }
            },
            PrizeTable = new[]
            {
                new[] { 1000, 550, 250, 50 },
                new[] { 3500, 2000, 700, 150 },
                new[] { 7500, 4200, 1800, 500 },
                new[] { 30000, 0, 0, 0 }
            },
            PointTable = Points,
            BountyTable = new[] { 250, 500, 1200, 3000 },
            CashTable = new[] { 50, 100, 200, 250 },
            SimulatedWreckChance = new[] { 6, 8, 11, 14 },
            AiLevelBonus = 0,
            DaysPerAiUpgrade = 15,
            AiLaterUpgradesDay = Never,
            AiSpikesDay = 8,
            AiPiercingDay = 14,
            AiFittingsDay = Never,
            DaysPerAiFitting = 12,
            AiGunDays = new[] { Never, Never, Never, 1 },
            AiConsumables = new[]
            {
                new[] { 1, 0, 1 },
                new[] { 2, 1, 2 },
                new[] { 3, 2, 2 },
                new[] { 4, 3, 3 }
            },
            AiSpecials = new[] { 0, 0, 1, 2 },
            Tracks = new[] { 0, 1, 2, 3, 4, 5, 6, 7 },
            StartTracks = 3,
            TrackRanks = new[] { 16, 12, 9, 6, 3 },
            ShowdownTrack = 8,
            ShowdownLaps = 3,
            MinDealerTier = 1,
            MaxDealerTier = 6
        };

        public static readonly LeagueSpec Iron = new LeagueSpec
        {
            Kind = LeagueKind.Iron,
            Name = "Iron League",
            Blurb = "The circuit for drivers who survived the wasteland: armoured cars, sharper rivals and The Iron Baron at the top.",
            StartCar = CarModel.Bulldog,
            StartMoney = 12000,
            StartMines = 3,
            StartRockets = 2,
            StartNitro = 2,
            StartOil = 1,
            Rivals = new[]
            {
                new RosterDriver("gus", "Gearbox Gus", "Strips his rivals' cars for parts after the race."),
                new RosterDriver("patch", "Patch Perkins", "Half his car is duct tape. The other half is armour."),
                new RosterDriver("rosa", "Rivet Rosa", "Builds her own cars and blows up everyone else's.", special: SpecialKind.Mortar),
                new RosterDriver("earl", "Big Earl", "Weighs as much as his engine and drives like it."),
                new RosterDriver("kerosene", "Kid Kerosene", "Too young to drive, old enough to set you on fire.", GunKind.Flamethrower),
                new RosterDriver("vinnie", "Slick Vinnie", "Every corner he takes, he leaves a puddle of oil in.", special: SpecialKind.Oil),
                new RosterDriver("tatiana", "Tank Tatiana", "Learned to drive in an armoured column. It shows.", GunKind.Minigun),
                new RosterDriver("ray", "Ricochet Ray", "Never aims. Hits anyway.", GunKind.Minigun),
                new RosterDriver("mechanic", "The Mechanic", "Fixes his car between corners. Fixes yours for good."),
                new RosterDriver("widowmaker", "Widowmaker", "Her rivals' families send the flowers in advance.", GunKind.Plasma),
                new RosterDriver("detonator", "Dr. Detonator", "A chemist with a mortar and a doctorate in blowing things up.", special: SpecialKind.Mortar),
                new RosterDriver("mary", "Bloody Mary", "Red car, red visor, red mist.", GunKind.Flamethrower),
                new RosterDriver("copperhead", "Copperhead", "Strikes from the inside line without a sound.", GunKind.Plasma),
                new RosterDriver("gray", "Grinder Gray", "Rides your door until the paint gives out, then the metal."),
                new RosterDriver("betty", "Black Betty", "Her engine is the last thing a rival hears.", GunKind.Minigun),
                new RosterDriver("dynamo", "Duke Dynamo", "Charged his first EMP off a power line. Still tingles.", special: SpecialKind.Emp),
                new RosterDriver("sven", "Steel Sven", "Built like his roll cage, and just as hard to bend."),
                new RosterDriver("fontaine", "Fury Fontaine", "Champion of three wastelands and a sore loser in all of them.", GunKind.Plasma),
                new RosterDriver("carnage", "Count Carnage", "The Baron's right hand, and sure he is next in line.", GunKind.Minigun, SpecialKind.Mortar)
            },
            Champion = new RosterDriver("baron", "The Iron Baron",
                "Rules the Iron League from a war rig plated with his rivals' doors. He has never lost a Showdown."),
            ChampionCar = CarModel.Goliath,
            ChampionGun = GunKind.Minigun,
            ChampionSpecial = SpecialKind.Oil,
            ChampionPaint = new Color(0.2f, 0.22f, 0.25f, 1f),
            TopRivalStartPoints = 60,
            StartTiers = new[] { 3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 5, 5, 5, 5, 6, 6, 6, 7 },
            MaxRivalTier = 7,
            PointsPerTier = 90,
            BottomSkill = 0.4f,
            TopSkill = 0.85f,
            SkillPerDay = 0.0012f,
            MaxRivalSkill = 0.97f,
            AiSkillBands = new[]
            {
                new[] { 0.35f, 0.55f },
                new[] { 0.60f, 0.78f },
                new[] { 0.82f, 0.95f },
                new[] { 1f, 1f }
            },
            PrizeTable = new[]
            {
                new[] { 7000, 4000, 1800, 450 },
                new[] { 20000, 11500, 4500, 1100 },
                new[] { 43000, 25000, 11000, 2700 },
                new[] { 200000, 0, 0, 0 }
            },
            PointTable = Points,
            BountyTable = new[] { 900, 1800, 3500, 8000 },
            CashTable = new[] { 150, 300, 500, 600 },
            SimulatedWreckChance = new[] { 8, 10, 13, 16 },
            AiLevelBonus = 0,
            DaysPerAiUpgrade = 12,
            AiSpikesDay = 1,
            AiPiercingDay = 5,
            AiFittingsDay = 10,
            DaysPerAiFitting = 10,
            AiGunDays = new[] { 12, 6, 1, 1 },
            AiConsumables = new[]
            {
                new[] { 2, 1, 2 },
                new[] { 3, 2, 2 },
                new[] { 4, 3, 3 },
                new[] { 5, 4, 4 }
            },
            AiSpecials = new[] { 0, 1, 2, 3 },
            Tracks = new[] { 0, 1, 2, 3, 9, 4, 5, 10, 6, 7, 11 },
            StartTracks = 5,
            TrackRanks = new[] { 17, 14, 11, 8, 5, 3 },
            ShowdownTrack = 8,
            ShowdownLaps = 4,
            MinDealerTier = 3,
            MaxDealerTier = 8
        };

        public static readonly LeagueSpec Hellfire = new LeagueSpec
        {
            Kind = LeagueKind.Hellfire,
            Name = "Hellfire League",
            Blurb = "The last ladder: the fastest cars ever built, the best drivers alive, and Nemesis waiting at the end.",
            StartCar = CarModel.Reaper,
            StartMoney = 40000,
            StartMines = 4,
            StartRockets = 3,
            StartNitro = 3,
            StartOil = 2,
            StartEmp = 1,
            StartMortar = 1,
            Rivals = new[]
            {
                new RosterDriver("ash", "Ash Ashford", "Survived the Iron League. Barely."),
                new RosterDriver("cinder", "Cinder", "Nobody remembers her real name. Nobody asks.", GunKind.Flamethrower),
                new RosterDriver("harry", "Hot Rod Harry", "Three hundred races, three hundred wrecks, one smile."),
                new RosterDriver("lava", "Lady Lava", "Drives like molten rock: slow to start, impossible to stop.", GunKind.Flamethrower, SpecialKind.Oil),
                new RosterDriver("jansen", "Jet Jansen", "Strapped a jet engine to a hearse. Won with it."),
                new RosterDriver("pete", "Pyro Pete", "Thinks a flamethrower is a steering aid.", GunKind.Flamethrower),
                new RosterDriver("hana", "Hellcat Hana", "Smaller than her car and meaner than her guns.", GunKind.Minigun),
                new RosterDriver("surgeon", "The Surgeon", "Takes your car apart one precise shot at a time.", GunKind.Plasma),
                new RosterDriver("brimstone", "Brimstone", "Leaves scorch marks on the grid before the lights go out.", GunKind.Flamethrower, SpecialKind.Mortar),
                new RosterDriver("mayhem", "Mother Mayhem", "Raised half the drivers on this ladder. Wrecked the other half."),
                new RosterDriver("zero", "Zero", "No name, no face, no mercy, no brakes.", GunKind.Plasma),
                new RosterDriver("ivy", "Inferno Ivy", "Burns everything she passes, her bridges included.", GunKind.Flamethrower),
                new RosterDriver("major", "Major Mortar", "Shells the corners ahead as if the war never ended.", special: SpecialKind.Mortar),
                new RosterDriver("sasha", "Static Sasha", "Her EMP has stopped more cars than her guns.", special: SpecialKind.Emp),
                new RosterDriver("wraith", "Wraith", "Appears in your mirror, then in your wreckage.", GunKind.Plasma, SpecialKind.Oil),
                new RosterDriver("doomsday", "Doomsday Dan", "Has been predicting your end since the first lap.", special: SpecialKind.Mortar),
                new RosterDriver("valkyrie", "Valkyrie", "Chooses who leaves the race. Never herself.", GunKind.Plasma, SpecialKind.Emp),
                new RosterDriver("overkill", "Overkill", "Every weapon he owns, all at once, all the time.", GunKind.Minigun),
                new RosterDriver("butcherjr", "The Butcher's Daughter", "Carries on the family trade with better tools.", GunKind.Minigun, SpecialKind.Mortar)
            },
            Champion = new RosterDriver("nemesis", "Nemesis",
                "Nobody knows where Nemesis came from. Everybody knows where the rivals went."),
            ChampionCar = CarModel.Apocalypse,
            ChampionGun = GunKind.Plasma,
            ChampionSpecial = SpecialKind.Emp,
            ChampionPaint = new Color(0.22f, 0.05f, 0.3f, 1f),
            TopRivalStartPoints = 75,
            StartTiers = new[] { 5, 5, 5, 5, 5, 5, 6, 6, 6, 6, 6, 6, 7, 7, 7, 7, 7, 8, 8 },
            MaxRivalTier = 8,
            PointsPerTier = 100,
            BottomSkill = 0.55f,
            TopSkill = 0.92f,
            SkillPerDay = 0.0012f,
            MaxRivalSkill = 0.99f,
            AiSkillBands = new[]
            {
                new[] { 0.55f, 0.72f },
                new[] { 0.75f, 0.90f },
                new[] { 0.90f, 1.00f },
                new[] { 1f, 1f }
            },
            PrizeTable = new[]
            {
                new[] { 19000, 10500, 4800, 1300 },
                new[] { 48000, 27000, 11000, 2900 },
                new[] { 96000, 54000, 24000, 6400 },
                new[] { 600000, 0, 0, 0 }
            },
            PointTable = Points,
            BountyTable = new[] { 2500, 5000, 9000, 20000 },
            CashTable = new[] { 400, 700, 1100, 1500 },
            SimulatedWreckChance = new[] { 10, 12, 15, 18 },
            AiLevelBonus = 0,
            DaysPerAiUpgrade = 10,
            AiSpikesDay = 1,
            AiPiercingDay = 1,
            AiFittingsDay = 4,
            DaysPerAiFitting = 8,
            AiGunDays = new[] { 1, 1, 1, 1 },
            AiConsumables = new[]
            {
                new[] { 3, 2, 2 },
                new[] { 4, 3, 3 },
                new[] { 5, 4, 3 },
                new[] { 6, 5, 4 }
            },
            AiSpecials = new[] { 1, 2, 3, 4 },
            Tracks = new[] { 0, 1, 2, 4, 9, 5, 10, 3, 6, 11, 7 },
            StartTracks = 7,
            TrackRanks = new[] { 15, 11, 7, 3 },
            ShowdownTrack = 8,
            ShowdownLaps = 5,
            MinDealerTier = 5,
            MaxDealerTier = 9
        };

        /// <summary>The leagues in the order they open.</summary>
        public static readonly LeagueSpec[] All = { Wasteland, Iron, Hellfire };

        public static int Count => All.Length;

        public static LeagueSpec Of(int league)
        {
            return All[Mathf.Clamp(league, 0, All.Length - 1)];
        }

        public static LeagueSpec Of(LeagueKind kind)
        {
            return Of((int)kind);
        }

        /// <summary>The league whose ladder (or champion) has the driver <paramref name="id"/>, or null.</summary>
        public static LeagueSpec OfDriver(string id)
        {
            foreach (LeagueSpec league in All)
            {
                if (league.Find(id) != null)
                {
                    return league;
                }
            }
            return null;
        }
    }
}
