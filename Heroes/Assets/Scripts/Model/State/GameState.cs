using System;
using System.Collections.Generic;

namespace Portfolio.Heroes
{
    /// <summary>
    /// The whole state of a game of Heroes: the map, the players, their heroes and towns, the objects on the map, a battle
    /// under way and the choices a player has to make. Plain fields only, so Unity's JsonUtility saves it as it is and
    /// two devices can compare it. Everything that changes it is in <see cref="HeroesGame"/>.
    /// </summary>
    [Serializable]
    public sealed class GameState
    {
        public const int Version = 1;

        public int version = Version;
        public uint seed;
        public string scenario = "";
        public int day = 1;
        public int currentPlayer;
        public MapData map = new MapData();
        public List<PlayerState> players = new List<PlayerState>();
        public List<HeroState> heroes = new List<HeroState>();
        public List<TownState> towns = new List<TownState>();
        public List<MapObject> objects = new List<MapObject>();
        public BattleState battle = new BattleState();
        public List<PendingChoice> pending = new List<PendingChoice>();
        public ScenarioRules rules = new ScenarioRules();
        /// <summary>The winning player, or -1.</summary>
        public int winner = -1;
        public bool over;
        /// <summary>Hero roster entries nobody has hired, for the taverns.</summary>
        public List<int> freeHeroes = new List<int>();

        public int Week => (day - 1) / 7 + 1;
        public int DayOfWeek => (day - 1) % 7 + 1;
        public int Month => (day - 1) / 28 + 1;
        public int WeekOfMonth => (day - 1) / 7 % 4 + 1;

        public HeroState Hero(int id)
        {
            return id >= 0 && id < heroes.Count ? heroes[id] : null;
        }

        public TownState Town(int id)
        {
            return id >= 0 && id < towns.Count ? towns[id] : null;
        }

        public MapObject Object(int id)
        {
            return id >= 0 && id < objects.Count ? objects[id] : null;
        }

        public PlayerState Player(int index)
        {
            return index >= 0 && index < players.Count ? players[index] : null;
        }

        /// <summary>The live hero standing on <paramref name="cell"/>, or null.</summary>
        public HeroState HeroAt(int cell)
        {
            foreach (HeroState hero in heroes)
            {
                if (hero.alive && hero.cell == cell)
                {
                    return hero;
                }
            }
            return null;
        }

        /// <summary>The object occupying <paramref name="cell"/> (any of its cells), or null.</summary>
        public MapObject ObjectAt(int cell)
        {
            if (!map.grid.Valid(cell))
            {
                return null;
            }
            int id = map.occupant[cell];
            MapObject obj = Object(id);
            return obj != null && !obj.removed ? obj : null;
        }
    }

    [Serializable]
    public sealed class MapData
    {
        public HexGrid grid = new HexGrid();
        public byte[] terrain = Array.Empty<byte>();
        public byte[] obstacle = Array.Empty<byte>();
        /// <summary>Height of the land, 0 (water level) to 255, for the view.</summary>
        public byte[] height = Array.Empty<byte>();
        public byte[] road = Array.Empty<byte>();
        /// <summary>The zone of the generator a cell belongs to.</summary>
        public byte[] zone = Array.Empty<byte>();
        /// <summary>Object standing on the cell (any cell of its footprint), or -1.</summary>
        public int[] occupant = Array.Empty<int>();

        public void Allocate(int columns, int rows)
        {
            grid = new HexGrid(columns, rows);
            int count = columns * rows;
            terrain = new byte[count];
            obstacle = new byte[count];
            height = new byte[count];
            road = new byte[count];
            zone = new byte[count];
            occupant = new int[count];
            for (int i = 0; i < count; i++)
            {
                occupant[i] = -1;
            }
        }

        public TerrainType TerrainAt(int cell)
        {
            return (TerrainType)terrain[cell];
        }

        public Obstacle ObstacleAt(int cell)
        {
            return (Obstacle)obstacle[cell];
        }

        public bool HasRoad(int cell)
        {
            return road[cell] != 0;
        }

        /// <summary>Whether land armies can stand on the cell at all (terrain and obstacles; objects aside).</summary>
        public bool Open(int cell)
        {
            return grid.Valid(cell) && obstacle[cell] == 0 && Land.Walkable((TerrainType)terrain[cell]);
        }
    }

    [Serializable]
    public sealed class PlayerState
    {
        public int index;
        public string name = "";
        public PlayerColor color;
        public Faction faction;
        public bool human = true;
        /// <summary>0 easy, 1 normal, 2 hard, for computer players.</summary>
        public int aiLevel = 1;
        public int team;
        public bool alive = true;
        public ResourceSet resources = new ResourceSet();
        public byte[] explored = Array.Empty<byte>();
        public List<int> heroes = new List<int>();
        public List<int> towns = new List<int>();
        /// <summary>The two heroes waiting in this player's taverns (roster ids), or -1.</summary>
        public int[] tavern = { -1, -1 };
        public int daysWithoutTown;
        /// <summary>Days the player had finished (for the statistics of the results).</summary>
        public int battlesWon;
        public int creaturesKilled;

        public bool Explored(int cell)
        {
            return cell >= 0 && cell < explored.Length && explored[cell] != 0;
        }
    }

    [Serializable]
    public sealed class ArmySlot
    {
        public int creature = -1;
        public int count;

        public bool IsEmpty => creature < 0 || count <= 0;

        public CreatureDef Def => Creatures.Get(creature);

        public ArmySlot Clone()
        {
            return new ArmySlot { creature = creature, count = count };
        }
    }

    [Serializable]
    public sealed class Army
    {
        public ArmySlot[] slots = NewSlots();

        public static ArmySlot[] NewSlots()
        {
            var slots = new ArmySlot[HeroData.ArmySlots];
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = new ArmySlot();
            }
            return slots;
        }

        public ArmySlot this[int slot] => slots[slot];

        public bool IsEmpty
        {
            get
            {
                foreach (ArmySlot slot in slots)
                {
                    if (!slot.IsEmpty)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        public int StackCount
        {
            get
            {
                int count = 0;
                foreach (ArmySlot slot in slots)
                {
                    if (!slot.IsEmpty)
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        public int TotalCreatures
        {
            get
            {
                int count = 0;
                foreach (ArmySlot slot in slots)
                {
                    if (!slot.IsEmpty)
                    {
                        count += slot.count;
                    }
                }
                return count;
            }
        }

        /// <summary>Adds creatures to the slot that has them already, or to the first free slot. Returns false when there is no room.</summary>
        public bool Add(int creature, int count)
        {
            if (count <= 0 || creature < 0)
            {
                return true;
            }
            foreach (ArmySlot slot in slots)
            {
                if (!slot.IsEmpty && slot.creature == creature)
                {
                    slot.count += count;
                    return true;
                }
            }
            foreach (ArmySlot slot in slots)
            {
                if (slot.IsEmpty)
                {
                    slot.creature = creature;
                    slot.count = count;
                    return true;
                }
            }
            return false;
        }

        public bool CanAdd(int creature)
        {
            foreach (ArmySlot slot in slots)
            {
                if (slot.IsEmpty || slot.creature == creature)
                {
                    return true;
                }
            }
            return false;
        }

        public void Clear()
        {
            foreach (ArmySlot slot in slots)
            {
                slot.creature = -1;
                slot.count = 0;
            }
        }

        /// <summary>Removes empty slots' leftovers (count 0 with a creature).</summary>
        public void Tidy()
        {
            foreach (ArmySlot slot in slots)
            {
                if (slot.count <= 0)
                {
                    slot.creature = -1;
                    slot.count = 0;
                }
            }
        }

        public Army Clone()
        {
            var copy = new Army();
            for (int i = 0; i < slots.Length; i++)
            {
                copy.slots[i] = slots[i].Clone();
            }
            return copy;
        }

        /// <summary>The slowest speed in the army, for the hero's daily movement.</summary>
        public int SlowestSpeed
        {
            get
            {
                int slowest = int.MaxValue;
                foreach (ArmySlot slot in slots)
                {
                    if (!slot.IsEmpty)
                    {
                        slowest = Math.Min(slowest, slot.Def.Speed);
                    }
                }
                return slowest == int.MaxValue ? 5 : slowest;
            }
        }
    }

    [Serializable]
    public sealed class SkillEntry
    {
        public int skill;
        public int level;
    }

    [Serializable]
    public sealed class HeroState
    {
        public int id;
        /// <summary>Entry of the roster (<see cref="HeroData.Hero"/>).</summary>
        public int def;
        public int owner = -1;
        public int cell = -1;
        public bool alive;
        public int level = 1;
        public int experience;
        public int attack;
        public int defense;
        public int power;
        public int knowledge;
        public int mana;
        public int movement;
        public int maxMovement;
        public Army army = new Army();
        public List<SkillEntry> skills = new List<SkillEntry>();
        public List<int> spells = new List<int>();
        /// <summary>The artifact worn in each slot (<see cref="ArtifactSlot"/>), or -1.</summary>
        public int[] equipped = { -1, -1, -1, -1, -1, -1, -1, -1 };
        public List<int> backpack = new List<int>();
        /// <summary>Objects that give their bonus once per hero and were visited.</summary>
        public List<int> visited = new List<int>();
        public int luckBonus;
        public int moraleBonus;
        /// <summary>The week the hero visited stables in (extra movement until it ends), or 0.</summary>
        public int stablesWeek;
        /// <summary>The side the hero faces (<see cref="HexSide"/>), for the view.</summary>
        public int facing;
        /// <summary>A hero who stopped for the day on purpose (skipped by "next hero").</summary>
        public bool sleeping;

        public HeroDef Def => HeroData.Hero(def);
        public string Name => Def != null ? Def.Name : "Hero";

        public int SkillLevel(SkillId skill)
        {
            foreach (SkillEntry entry in skills)
            {
                if (entry.skill == (int)skill)
                {
                    return entry.level;
                }
            }
            return 0;
        }

        public bool Knows(SpellId spell)
        {
            return spells.Contains((int)spell);
        }
    }

    [Serializable]
    public sealed class TownState
    {
        public int id;
        public int objectId;
        public string name = "";
        public Faction faction;
        public int owner = -1;
        /// <summary>The gate: the cell heroes stand on when they visit.</summary>
        public int cell;
        public int center;
        public List<int> built = new List<int>();
        public bool builtToday;
        public Army garrison = new Army();
        /// <summary>Creatures of each tier waiting to be recruited.</summary>
        public int[] available = new int[7];
        public List<int> guildSpells = new List<int>();

        public bool Has(BuildingId building)
        {
            return built.Contains((int)building);
        }
    }

    [Serializable]
    public sealed class MapObject
    {
        public int id;
        public ObjectKind kind;
        /// <summary>The cell a hero visits it through (where it stands).</summary>
        public int cell;
        /// <summary>Every cell it blocks, <see cref="cell"/> included.</summary>
        public List<int> footprint = new List<int>();
        public int owner = -1;
        /// <summary>Resource kind, creature, artifact, spell level, town id... depending on the kind.</summary>
        public int subtype;
        public int amount;
        public int amount2;
        /// <summary>Heroes (or players, for weekly objects) that already took what it gives.</summary>
        public List<int> visitedBy = new List<int>();
        public int lastWeek;
        public bool removed;
        /// <summary>The monster that guards it (a treasure, a mine): it fights whoever comes for the prize. -1 for none.</summary>
        public int guard = -1;
        /// <summary>A name a scenario refers to the object by (the dragon to slay), or empty.</summary>
        public string tag = "";
    }

    public enum ChoiceKind
    {
        LevelUp = 0,
        Treasure = 1,
        Arena = 2
    }

    /// <summary>A question a player has to answer before the game goes on: which skill on a level up, gold or experience.</summary>
    [Serializable]
    public sealed class PendingChoice
    {
        public ChoiceKind kind;
        public int player;
        public int hero;
        /// <summary>The primary stat a level up raised, for the dialog.</summary>
        public int stat = -1;
        /// <summary>The options (skills for a level up, amounts for a treasure).</summary>
        public int[] options = { -1, -1 };
        public int value;
    }

    public enum VictoryKind
    {
        DefeatAll = 0,
        CaptureTown = 1,
        DefeatMonster = 2,
        GatherGold = 3,
        FindArtifact = 4
    }

    public enum LossKind
    {
        LoseAll = 0,
        LoseHero = 1,
        TimeLimit = 2
    }

    /// <summary>Where armies fight when they meet. It is fixed for a whole game and the same on every device of a table.</summary>
    public enum BattleStyle
    {
        /// <summary>On the cells of the adventure map around the two armies, with what stands there in the way.</summary>
        OnTheMap = 0,
        /// <summary>On a battlefield of its own, fifteen hexes by eleven, as in the original game.</summary>
        Battlefield = 1
    }

    /// <summary>What a scenario asks for: how it is won and lost, and the star thresholds of a campaign.</summary>
    [Serializable]
    public sealed class ScenarioRules
    {
        public VictoryKind victory = VictoryKind.DefeatAll;
        /// <summary>Town id, monster tag index, gold amount or artifact, depending on the victory.</summary>
        public int victoryValue;
        public string victoryTag = "";
        public LossKind loss = LossKind.LoseAll;
        public int lossValue;
        /// <summary>The day by which the scenario must be won, or 0.</summary>
        public int dayLimit;
        /// <summary>Where the battles of this game are fought: chosen when it starts (a setting, or the room's option).</summary>
        public BattleStyle battleStyle = BattleStyle.OnTheMap;
    }
}
