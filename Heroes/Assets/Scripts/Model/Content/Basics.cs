using System;
using System.Text;

namespace Portfolio.Heroes
{
    public enum ResourceKind
    {
        Gold = 0,
        Wood = 1,
        Ore = 2,
        Mercury = 3,
        Sulfur = 4,
        Crystal = 5,
        Gems = 6
    }

    public enum Faction
    {
        Castle = 0,
        Necropolis = 1,
        Stronghold = 2,
        Neutral = 3
    }

    public enum TerrainType : byte
    {
        Grass = 0,
        Dirt = 1,
        Sand = 2,
        Snow = 3,
        Swamp = 4,
        Rough = 5,
        Wasteland = 6,
        Water = 7,
        Rock = 8
    }

    /// <summary>What stands on a cell of the map and keeps armies out of it (on the map and in battle alike).</summary>
    public enum Obstacle : byte
    {
        None = 0,
        Forest = 1,
        Rocks = 2,
        Mountain = 3,
        Lake = 4,
        DeadTrees = 5,
        Edge = 6
    }

    /// <summary>A bundle of the seven resources, in whole units. Serializable for saves.</summary>
    [Serializable]
    public sealed class ResourceSet
    {
        public const int Kinds = 7;

        public int[] values = new int[Kinds];

        public ResourceSet()
        {
        }

        public ResourceSet(int gold, int wood = 0, int ore = 0, int mercury = 0, int sulfur = 0, int crystal = 0, int gems = 0)
        {
            values = new[] { gold, wood, ore, mercury, sulfur, crystal, gems };
        }

        public int this[ResourceKind kind]
        {
            get => values[(int)kind];
            set => values[(int)kind] = value;
        }

        public int Gold => values[0];

        public ResourceSet Clone()
        {
            return new ResourceSet { values = (int[])values.Clone() };
        }

        public bool CanAfford(ResourceSet cost, int times = 1)
        {
            for (int i = 0; i < Kinds; i++)
            {
                if ((long)cost.values[i] * times > values[i])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>How many times <paramref name="cost"/> fits into this bundle.</summary>
        public int Times(ResourceSet cost)
        {
            int best = int.MaxValue;
            for (int i = 0; i < Kinds; i++)
            {
                if (cost.values[i] > 0)
                {
                    best = Math.Min(best, values[i] / cost.values[i]);
                }
            }
            return best == int.MaxValue ? 0 : best;
        }

        public void Add(ResourceSet other, int times = 1)
        {
            for (int i = 0; i < Kinds; i++)
            {
                values[i] += other.values[i] * times;
            }
        }

        public void Subtract(ResourceSet other, int times = 1)
        {
            for (int i = 0; i < Kinds; i++)
            {
                values[i] -= other.values[i] * times;
            }
        }

        public bool IsEmpty
        {
            get
            {
                foreach (int value in values)
                {
                    if (value != 0)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        public override string ToString()
        {
            var text = new StringBuilder();
            for (int i = 0; i < Kinds; i++)
            {
                if (values[i] != 0)
                {
                    if (text.Length > 0)
                    {
                        text.Append(", ");
                    }
                    text.Append(values[i]).Append(' ').Append(Land.ResourceName((ResourceKind)i));
                }
            }
            return text.ToString();
        }
    }

    /// <summary>Movement costs and names of the terrains, and the names and worth of the resources.</summary>
    public static class Land
    {
        /// <summary>Movement points a step onto a cell of the terrain costs, 100 being one step on grass.</summary>
        public static int Cost(TerrainType terrain)
        {
            switch (terrain)
            {
                case TerrainType.Rough: return 125;
                case TerrainType.Sand: return 150;
                case TerrainType.Snow: return 150;
                case TerrainType.Swamp: return 175;
                case TerrainType.Water:
                case TerrainType.Rock: return 0;
                default: return 100;
            }
        }

        public const int RoadCost = 60;

        public static bool Walkable(TerrainType terrain)
        {
            return terrain != TerrainType.Water && terrain != TerrainType.Rock;
        }

        public static string Name(TerrainType terrain)
        {
            switch (terrain)
            {
                case TerrainType.Grass: return "Grassland";
                case TerrainType.Dirt: return "Dirt";
                case TerrainType.Sand: return "Sand";
                case TerrainType.Snow: return "Snow";
                case TerrainType.Swamp: return "Swamp";
                case TerrainType.Rough: return "Rough";
                case TerrainType.Wasteland: return "Wasteland";
                case TerrainType.Water: return "Water";
                default: return "Mountains";
            }
        }

        public static string ResourceName(ResourceKind kind)
        {
            switch (kind)
            {
                case ResourceKind.Gold: return "Gold";
                case ResourceKind.Wood: return "Wood";
                case ResourceKind.Ore: return "Ore";
                case ResourceKind.Mercury: return "Mercury";
                case ResourceKind.Sulfur: return "Sulfur";
                case ResourceKind.Crystal: return "Crystal";
                default: return "Gems";
            }
        }

        /// <summary>What one unit is worth in gold, for trade prices and for the computer players' plans.</summary>
        public static int Value(ResourceKind kind)
        {
            switch (kind)
            {
                case ResourceKind.Gold: return 1;
                case ResourceKind.Wood:
                case ResourceKind.Ore: return 100;
                default: return 250;
            }
        }

        public static int Value(ResourceSet set)
        {
            int total = 0;
            for (int i = 0; i < ResourceSet.Kinds; i++)
            {
                total += set.values[i] * Value((ResourceKind)i);
            }
            return total;
        }

        /// <summary>The terrain an army of the faction feels at home on (no movement penalty from its native land).</summary>
        public static TerrainType Native(Faction faction)
        {
            switch (faction)
            {
                case Faction.Castle: return TerrainType.Grass;
                case Faction.Necropolis: return TerrainType.Wasteland;
                case Faction.Stronghold: return TerrainType.Rough;
                default: return TerrainType.Dirt;
            }
        }

        public static string FactionName(Faction faction)
        {
            switch (faction)
            {
                case Faction.Castle: return "Castle";
                case Faction.Necropolis: return "Necropolis";
                case Faction.Stronghold: return "Stronghold";
                default: return "Neutral";
            }
        }

        /// <summary>The rare resource the faction's best buildings and creatures need.</summary>
        public static ResourceKind RareOf(Faction faction)
        {
            switch (faction)
            {
                case Faction.Castle: return ResourceKind.Gems;
                case Faction.Necropolis: return ResourceKind.Mercury;
                case Faction.Stronghold: return ResourceKind.Crystal;
                default: return ResourceKind.Sulfur;
            }
        }
    }

    /// <summary>The colors of the players, in the order seats take them.</summary>
    public enum PlayerColor
    {
        Red = 0,
        Blue = 1,
        Green = 2,
        Yellow = 3
    }
}
