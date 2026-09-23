using System;
using System.Collections.Generic;

namespace Portfolio.Heroes
{
    /// <summary>A seat of a scenario: who plays it, with what, on which team.</summary>
    [Serializable]
    public sealed class PlayerSpec
    {
        public Faction faction;
        public bool human;
        /// <summary>0 easy, 1 normal, 2 hard.</summary>
        public int aiLevel = 1;
        public int team;
        /// <summary>The roster entry of the starting hero, or -1 for one of the faction chosen by the seed.</summary>
        public int hero = -1;
        /// <summary>Buildings the town starts with beyond the village hall and fort.</summary>
        public List<BuildingId> buildings = new List<BuildingId>();
        public string name = "";

        public PlayerSpec Clone()
        {
            return new PlayerSpec { faction = faction, human = human, aiLevel = aiLevel, team = team, hero = hero, buildings = new List<BuildingId>(buildings), name = name };
        }
    }

    public enum SpecialKind
    {
        Monster = 0,
        Artifact = 1,
        NeutralTown = 2,
        Treasure = 3,
        Mine = 4,
        Building = 5
    }

    /// <summary>Something a scenario puts on its map on purpose: the dragon on its hoard, the relic to find.</summary>
    [Serializable]
    public sealed class SpecialSpec
    {
        public SpecialKind kind;
        /// <summary>Where, in thousandths of the map's width and height from the bottom left.</summary>
        public int x = 500;
        public int y = 500;
        public CreatureId creature = CreatureId.None;
        public int count;
        public ArtifactId artifact = ArtifactId.None;
        public Faction faction = Faction.Neutral;
        public ObjectKind building = ObjectKind.Treasure;
        public ResourceKind resource = ResourceKind.Gold;
        public string tag = "";
        /// <summary>Guards placed next to it (a creature and a count), for artifacts and treasures.</summary>
        public CreatureId guard = CreatureId.None;
        public int guardCount;
    }

    /// <summary>
    /// The recipe of a map: its size, its seats, how rich and how dangerous it is and what a scenario adds. The
    /// generator makes the same map from the same recipe on every device.
    /// </summary>
    [Serializable]
    public sealed class MapSpec
    {
        public string name = "Map";
        public int columns = 48;
        public int rows = 54;
        public uint seed = 1;
        public List<PlayerSpec> players = new List<PlayerSpec>();
        /// <summary>Neutral zones between the players' home zones, per player.</summary>
        public int zonesPerPlayer = 1;
        public int neutralTowns;
        /// <summary>1 poor, 2 normal, 3 rich.</summary>
        public int treasure = 2;
        /// <summary>1 weak, 2 normal, 3 strong, 4 deadly.</summary>
        public int monsters = 2;
        /// <summary>0 none, 1 a few lakes, 2 many.</summary>
        public int water = 1;
        /// <summary>0 a mix of lands, else the terrain most neutral zones have (a winter map, a desert).</summary>
        public int theme;
        public TerrainType themeTerrain = TerrainType.Grass;
        public ScenarioRules rules = new ScenarioRules();
        public List<SpecialSpec> specials = new List<SpecialSpec>();
        /// <summary>Gold the people start with (the computer players by their level).</summary>
        public int startingGold = 10000;

        public MapSpec Clone()
        {
            var copy = (MapSpec)MemberwiseClone();
            copy.players = new List<PlayerSpec>();
            foreach (PlayerSpec player in players)
            {
                copy.players.Add(player.Clone());
            }
            copy.specials = new List<SpecialSpec>(specials);
            copy.rules = new ScenarioRules
            {
                victory = rules.victory,
                victoryValue = rules.victoryValue,
                victoryTag = rules.victoryTag,
                loss = rules.loss,
                lossValue = rules.lossValue,
                dayLimit = rules.dayLimit
            };
            return copy;
        }
    }
}
