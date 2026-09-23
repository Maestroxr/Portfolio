using System.Collections.Generic;

namespace Portfolio.Heroes
{
    public enum HeroClass
    {
        Knight = 0,
        Cleric = 1,
        DeathKnight = 2,
        Necromancer = 3,
        Barbarian = 4,
        BattleMage = 5
    }

    public enum PrimaryStat
    {
        Attack = 0,
        Defense = 1,
        Power = 2,
        Knowledge = 3
    }

    /// <summary>Secondary skills. The numbers are saved and sent online: only ever append.</summary>
    public enum SkillId
    {
        None = -1,
        Offense = 0,
        Armorer = 1,
        Archery = 2,
        Logistics = 3,
        Pathfinding = 4,
        Scouting = 5,
        Wisdom = 6,
        Sorcery = 7,
        Mysticism = 8,
        Leadership = 9,
        Luck = 10,
        Estates = 11,
        Necromancy = 12
    }

    public sealed class HeroClassDef
    {
        public HeroClass Id;
        public Faction Faction;
        public string Name;
        public bool Magic;
        /// <summary>Starting attack, defense, spell power and knowledge.</summary>
        public int[] Start;
        /// <summary>Chances out of 100 that a level raises attack, defense, spell power or knowledge.</summary>
        public int[] Growth;
        /// <summary>How likely each secondary skill is offered on a level up (0 never).</summary>
        public int[] SkillWeights;
    }

    public sealed class HeroDef
    {
        public int Id;
        public string Name;
        public HeroClass Class;
        public bool Female;
        public SkillId FirstSkill;
        public SkillId SecondSkill;
        /// <summary>A spell the hero starts with, or None.</summary>
        public SpellId Spell;
        public string Biography;

        public Faction Faction => HeroData.Class(Class).Faction;
    }

    public sealed class SkillDef
    {
        public SkillId Id;
        public string Name;
        public string[] Levels;
        public string Description;
    }

    public static class HeroData
    {
        public const int MaxLevel = 30;
        public const int MaxSkills = 8;
        public const int ArmySlots = 7;
        public const int HireCost = 2500;
        public const int BaseSight = 5;
        public const int MaxSkillLevel = 3;

        private static readonly HeroClassDef[] Classes =
        {
            new HeroClassDef { Id = HeroClass.Knight, Faction = Faction.Castle, Name = "Knight", Magic = false, Start = new[] { 2, 2, 1, 1 }, Growth = new[] { 35, 45, 10, 10 },
                SkillWeights = W(offense: 6, armorer: 8, archery: 6, logistics: 6, pathfinding: 4, scouting: 3, wisdom: 3, sorcery: 1, mysticism: 1, leadership: 10, luck: 4, estates: 6, necromancy: 0) },
            new HeroClassDef { Id = HeroClass.Cleric, Faction = Faction.Castle, Name = "Cleric", Magic = true, Start = new[] { 1, 0, 2, 2 }, Growth = new[] { 20, 15, 20, 45 },
                SkillWeights = W(offense: 2, armorer: 3, archery: 3, logistics: 4, pathfinding: 4, scouting: 3, wisdom: 10, sorcery: 6, mysticism: 8, leadership: 6, luck: 5, estates: 5, necromancy: 0) },
            new HeroClassDef { Id = HeroClass.DeathKnight, Faction = Faction.Necropolis, Name = "Death Knight", Magic = false, Start = new[] { 1, 2, 2, 1 }, Growth = new[] { 30, 25, 20, 25 },
                SkillWeights = W(offense: 7, armorer: 6, archery: 5, logistics: 5, pathfinding: 5, scouting: 3, wisdom: 4, sorcery: 3, mysticism: 2, leadership: 0, luck: 3, estates: 3, necromancy: 10) },
            new HeroClassDef { Id = HeroClass.Necromancer, Faction = Faction.Necropolis, Name = "Necromancer", Magic = true, Start = new[] { 1, 0, 2, 2 }, Growth = new[] { 15, 15, 35, 35 },
                SkillWeights = W(offense: 2, armorer: 3, archery: 2, logistics: 4, pathfinding: 3, scouting: 3, wisdom: 8, sorcery: 8, mysticism: 6, leadership: 0, luck: 2, estates: 4, necromancy: 12) },
            new HeroClassDef { Id = HeroClass.Barbarian, Faction = Faction.Stronghold, Name = "Barbarian", Magic = false, Start = new[] { 4, 0, 1, 1 }, Growth = new[] { 55, 35, 5, 5 },
                SkillWeights = W(offense: 10, armorer: 6, archery: 8, logistics: 8, pathfinding: 7, scouting: 4, wisdom: 1, sorcery: 1, mysticism: 1, leadership: 5, luck: 5, estates: 3, necromancy: 0) },
            new HeroClassDef { Id = HeroClass.BattleMage, Faction = Faction.Stronghold, Name = "Battle Mage", Magic = true, Start = new[] { 2, 1, 1, 2 }, Growth = new[] { 30, 20, 25, 25 },
                SkillWeights = W(offense: 5, armorer: 3, archery: 5, logistics: 5, pathfinding: 4, scouting: 4, wisdom: 8, sorcery: 8, mysticism: 5, leadership: 4, luck: 4, estates: 3, necromancy: 0) },
        };

        private static int[] W(int offense, int armorer, int archery, int logistics, int pathfinding, int scouting, int wisdom, int sorcery,
            int mysticism, int leadership, int luck, int estates, int necromancy)
        {
            return new[] { offense, armorer, archery, logistics, pathfinding, scouting, wisdom, sorcery, mysticism, leadership, luck, estates, necromancy };
        }

        public static HeroClassDef Class(HeroClass id)
        {
            return Classes[(int)id];
        }

        public static IEnumerable<HeroClassDef> AllClasses => Classes;

        private static readonly HeroDef[] Roster =
        {
            // Castle, knights
            H(0, "Sir Aldric", HeroClass.Knight, false, SkillId.Leadership, SkillId.Offense, SpellId.None, "A grizzled captain of the border keeps who has never lost a banner."),
            H(1, "Lady Rowena", HeroClass.Knight, true, SkillId.Armorer, SkillId.Leadership, SpellId.None, "Heir of a fallen house, she rides at the head of every charge."),
            H(2, "Sir Gawen", HeroClass.Knight, false, SkillId.Archery, SkillId.Logistics, SpellId.None, "Master of the royal bowmen and of long marches."),
            H(3, "Dame Isolde", HeroClass.Knight, true, SkillId.Estates, SkillId.Leadership, SpellId.None, "A steward turned warlord, as good with ledgers as with lances."),
            // Castle, clerics
            H(4, "Father Anselm", HeroClass.Cleric, false, SkillId.Wisdom, SkillId.Mysticism, SpellId.Bless, "An abbot who believes a blessing is worth a thousand swords."),
            H(5, "Sister Brienne", HeroClass.Cleric, true, SkillId.Wisdom, SkillId.Luck, SpellId.Cure, "Healer of the realm's plague years."),
            H(6, "Brother Caius", HeroClass.Cleric, false, SkillId.Sorcery, SkillId.Wisdom, SpellId.MagicArrow, "His sermons end in holy fire."),
            H(7, "Mother Elowen", HeroClass.Cleric, true, SkillId.Mysticism, SkillId.Scouting, SpellId.StoneSkin, "Keeper of the old chapels in the hills."),
            // Necropolis, death knights
            H(8, "Lord Mordrec", HeroClass.DeathKnight, false, SkillId.Necromancy, SkillId.Offense, SpellId.Curse, "Once a paladin, now the first of the fallen."),
            H(9, "Lady Sanguine", HeroClass.DeathKnight, true, SkillId.Necromancy, SkillId.Armorer, SpellId.None, "Her armour has not been taken off in a hundred years."),
            H(10, "Vorgath", HeroClass.DeathKnight, false, SkillId.Necromancy, SkillId.Logistics, SpellId.None, "A warlord who rides with an army that never sleeps."),
            H(11, "Grimwald", HeroClass.DeathKnight, false, SkillId.Necromancy, SkillId.Archery, SpellId.None, "The dead archers of the fens answer to him."),
            // Necropolis, necromancers
            H(12, "Malachar", HeroClass.Necromancer, false, SkillId.Necromancy, SkillId.Wisdom, SpellId.AnimateDead, "Master of the black tower, collector of souls."),
            H(13, "Vesna", HeroClass.Necromancer, true, SkillId.Necromancy, SkillId.Sorcery, SpellId.MagicArrow, "She speaks to crows and they answer."),
            H(14, "Thorne", HeroClass.Necromancer, false, SkillId.Necromancy, SkillId.Mysticism, SpellId.Slow, "A grave robber who learned too much."),
            H(15, "Ysolde", HeroClass.Necromancer, true, SkillId.Necromancy, SkillId.Scouting, SpellId.Weakness, "The pale lady of the marshes."),
            // Stronghold, barbarians
            H(16, "Grom", HeroClass.Barbarian, false, SkillId.Offense, SkillId.Pathfinding, SpellId.None, "Chief of the ash clans. Talks little, hits hard."),
            H(17, "Yrsa", HeroClass.Barbarian, true, SkillId.Offense, SkillId.Archery, SpellId.None, "Huntress who has tracked a dragon to its lair and back."),
            H(18, "Harald Axe", HeroClass.Barbarian, false, SkillId.Logistics, SkillId.Offense, SpellId.None, "Raids a village before its bells can ring."),
            H(19, "Kara", HeroClass.Barbarian, true, SkillId.Armorer, SkillId.Luck, SpellId.None, "Champion of the pit fights of the south."),
            // Stronghold, battle mages
            H(20, "Zul'kar", HeroClass.BattleMage, false, SkillId.Wisdom, SkillId.Offense, SpellId.Bloodlust, "A shaman who paints his runes in blood."),
            H(21, "Ingrid", HeroClass.BattleMage, true, SkillId.Sorcery, SkillId.Archery, SpellId.LightningBolt, "Storm caller of the northern fjords."),
            H(22, "Oskar", HeroClass.BattleMage, false, SkillId.Mysticism, SkillId.Pathfinding, SpellId.Haste, "Wanderer of the high passes."),
            H(23, "Freya", HeroClass.BattleMage, true, SkillId.Wisdom, SkillId.Luck, SpellId.StoneSkin, "Seer of the war camps."),
        };

        private static HeroDef H(int id, string name, HeroClass heroClass, bool female, SkillId first, SkillId second, SpellId spell, string bio)
        {
            return new HeroDef { Id = id, Name = name, Class = heroClass, Female = female, FirstSkill = first, SecondSkill = second, Spell = spell, Biography = bio };
        }

        public static int RosterCount => Roster.Length;

        public static HeroDef Hero(int id)
        {
            return id >= 0 && id < Roster.Length ? Roster[id] : null;
        }

        public static IEnumerable<HeroDef> AllHeroes => Roster;

        private static readonly SkillDef[] Skills =
        {
            S(SkillId.Offense, "Offense", "+10% melee damage", "+20% melee damage", "+30% melee damage", "The hero's troops hit harder in close combat."),
            S(SkillId.Armorer, "Armorer", "-5% damage taken", "-10% damage taken", "-15% damage taken", "The hero's troops take less damage."),
            S(SkillId.Archery, "Archery", "+10% ranged damage", "+25% ranged damage", "+50% ranged damage", "The hero's shooters deal more damage."),
            S(SkillId.Logistics, "Logistics", "+10% movement", "+20% movement", "+30% movement", "The hero travels farther every day."),
            S(SkillId.Pathfinding, "Pathfinding", "Rough land slows 25% less", "Rough land slows 50% less", "Rough land slows 75% less", "Sand, snow, swamps and rough land slow the hero less."),
            S(SkillId.Scouting, "Scouting", "+1 sight", "+2 sight", "+3 sight", "The hero sees farther across the map."),
            S(SkillId.Wisdom, "Wisdom", "Learns 3rd level spells", "Learns 4th level spells", "Learns 5th level spells", "Without Wisdom a hero learns only first and second level spells."),
            S(SkillId.Sorcery, "Sorcery", "+10% spell damage", "+20% spell damage", "+30% spell damage", "The hero's attack spells deal more damage."),
            S(SkillId.Mysticism, "Mysticism", "+2 mana per day", "+4 mana per day", "+6 mana per day", "The hero regains more spell points every day."),
            S(SkillId.Leadership, "Leadership", "+1 morale", "+2 morale", "+3 morale", "Troops of good morale sometimes act twice in a round."),
            S(SkillId.Luck, "Luck", "+1 luck", "+2 luck", "+3 luck", "Lucky blows deal double damage."),
            S(SkillId.Estates, "Estates", "+125 gold per day", "+250 gold per day", "+500 gold per day", "The hero's lands bring in gold."),
            S(SkillId.Necromancy, "Necromancy", "Raises 10% of the fallen", "Raises 20% of the fallen", "Raises 30% of the fallen", "After a victory, some of the slain rise as skeletons."),
        };

        private static SkillDef S(SkillId id, string name, string basic, string advanced, string expert, string description)
        {
            return new SkillDef { Id = id, Name = name, Levels = new[] { basic, advanced, expert }, Description = description };
        }

        public static int SkillCount => Skills.Length;

        public static SkillDef Skill(SkillId id)
        {
            int index = (int)id;
            return index >= 0 && index < Skills.Length ? Skills[index] : null;
        }

        public static string SkillLevelName(int level)
        {
            switch (level)
            {
                case 1: return "Basic";
                case 2: return "Advanced";
                case 3: return "Expert";
                default: return "";
            }
        }

        /// <summary>Experience needed to reach <paramref name="level"/> (level 1 needs none).</summary>
        public static int ExperienceFor(int level)
        {
            if (level <= 1)
            {
                return 0;
            }
            // 1000, 2000, 3200, 4600, 6200, 8000, 10000, 12200, 14700, 17500, ... then +20% a level.
            int[] table = { 0, 0, 1000, 2000, 3200, 4600, 6200, 8000, 10000, 12200, 14700, 17500, 20600, 24320 };
            if (level < table.Length)
            {
                return table[level];
            }
            long needed = table[table.Length - 1];
            long step = table[table.Length - 1] - table[table.Length - 2];
            for (int l = table.Length; l <= level; l++)
            {
                step = step * 6 / 5;
                needed += step;
            }
            return (int)System.Math.Min(needed, int.MaxValue);
        }

        public static int LevelFor(int experience)
        {
            int level = 1;
            while (level < MaxLevel && experience >= ExperienceFor(level + 1))
            {
                level++;
            }
            return level;
        }

        public static string StatName(PrimaryStat stat)
        {
            switch (stat)
            {
                case PrimaryStat.Attack: return "Attack";
                case PrimaryStat.Defense: return "Defense";
                case PrimaryStat.Power: return "Spell Power";
                default: return "Knowledge";
            }
        }
    }
}
