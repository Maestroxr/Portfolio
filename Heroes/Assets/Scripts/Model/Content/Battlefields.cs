using System;
using Gamebox.Lockstep;

namespace Portfolio.Heroes
{
    /// <summary>
    /// What a battlefield of its own is strewn with on one kind of land: how many clumps of obstacles, and of what
    /// kinds (by weight, the heavier the more often).
    /// </summary>
    public sealed class FieldObstacles
    {
        public int MinClumps;
        public int MaxClumps;
        public BattleObstacle[] Kinds;
        public int[] Weights;

        public FieldObstacles(int minClumps, int maxClumps, params (BattleObstacle kind, int weight)[] kinds)
        {
            MinClumps = minClumps;
            MaxClumps = maxClumps;
            Kinds = new BattleObstacle[kinds.Length];
            Weights = new int[kinds.Length];
            for (int i = 0; i < kinds.Length; i++)
            {
                Kinds[i] = kinds[i].kind;
                Weights[i] = kinds[i].weight;
            }
        }

        /// <summary>A kind drawn by weight.</summary>
        public BattleObstacle Pick(IRandom random)
        {
            int total = 0;
            foreach (int weight in Weights)
            {
                total += weight;
            }
            int roll = random.Range(0, total);
            for (int i = 0; i < Kinds.Length; i++)
            {
                roll -= Weights[i];
                if (roll < 0)
                {
                    return Kinds[i];
                }
            }
            return Kinds[Kinds.Length - 1];
        }
    }

    /// <summary>
    /// The battlefield of its own (<see cref="BattleStyle.Battlefield"/>): fifteen hexes by eleven, the attacker lining
    /// up in the two columns on the left, the defender in the two on the right, and the land between strewn with
    /// clumps of one to three obstacles of the kinds that grow or lie on the land the armies met on. A town with walls
    /// puts a wall across the field, with two breaches, a gate for the defenders and its arrow towers in the wall.
    /// The layout is in the rules (<see cref="HeroesGame.LayOutField"/>); these are its tables.
    /// </summary>
    public static class Battlefields
    {
        /// <summary>Columns 0-1 and 13-14 are where the armies line up: obstacles stay in the columns between.</summary>
        public const int FirstObstacleColumn = 2;
        public const int LastObstacleColumn = BattleState.FieldColumns - 3;
        /// <summary>Layouts tried before the field is left bare (each must leave every open cell within reach).</summary>
        public const int Attempts = 10;

        /// <summary>The column of a town's wall; the defenders line up behind it, the besiegers keep a column clear before it.</summary>
        public const int WallColumn = 10;
        /// <summary>The rows the wall is broken at, for everybody to pass.</summary>
        public static readonly int[] BreachRows = { 2, 8 };
        /// <summary>The row of the gate, which only the defenders pass.</summary>
        public const int GateRow = 5;
        /// <summary>The rows of the wall the arrow towers stand in, in the order a town gets them (Citadel one, Castle three).</summary>
        public static readonly int[] TowerRows = { 4, 0, 10 };

        /// <summary>
        /// The shapes of a clump by its size, as the steps from its first hex to each next one: one hex, two side by side
        /// (three ways), and three in a triangle or a line.
        /// </summary>
        public static readonly HexSide[][][] Shapes =
        {
            new[] { new HexSide[0] },
            new[] { new[] { HexSide.East }, new[] { HexSide.NorthEast }, new[] { HexSide.SouthEast } },
            new[]
            {
                new[] { HexSide.East, HexSide.NorthWest },
                new[] { HexSide.East, HexSide.SouthWest },
                new[] { HexSide.East, HexSide.East },
                new[] { HexSide.NorthEast, HexSide.NorthEast },
                new[] { HexSide.SouthEast, HexSide.SouthEast }
            }
        };

        private static readonly FieldObstacles Grass = new FieldObstacles(4, 7,
            (BattleObstacle.Tree, 4), (BattleObstacle.Rock, 2), (BattleObstacle.Stump, 2), (BattleObstacle.Logs, 1), (BattleObstacle.Mound, 1));

        private static readonly FieldObstacles Dirt = new FieldObstacles(4, 7,
            (BattleObstacle.Rock, 3), (BattleObstacle.Stump, 2), (BattleObstacle.Logs, 2), (BattleObstacle.Rubble, 2), (BattleObstacle.Mound, 2));

        private static readonly FieldObstacles Sand = new FieldObstacles(3, 6,
            (BattleObstacle.Rock, 3), (BattleObstacle.Boulder, 2), (BattleObstacle.Bones, 2), (BattleObstacle.Crater, 2));

        private static readonly FieldObstacles Snow = new FieldObstacles(4, 7,
            (BattleObstacle.Pine, 4), (BattleObstacle.Rock, 2), (BattleObstacle.Boulder, 2));

        private static readonly FieldObstacles Swamp = new FieldObstacles(4, 8,
            (BattleObstacle.DeadTree, 3), (BattleObstacle.Pool, 4), (BattleObstacle.Stump, 2), (BattleObstacle.Bones, 1));

        private static readonly FieldObstacles Rough = new FieldObstacles(4, 7,
            (BattleObstacle.Boulder, 3), (BattleObstacle.Rock, 3), (BattleObstacle.Rubble, 2), (BattleObstacle.Crater, 2));

        private static readonly FieldObstacles Wasteland = new FieldObstacles(4, 7,
            (BattleObstacle.Bones, 2), (BattleObstacle.DeadTree, 3), (BattleObstacle.Crater, 3), (BattleObstacle.Rubble, 2));

        private static readonly FieldObstacles Underground = new FieldObstacles(4, 7,
            (BattleObstacle.Crystal, 3), (BattleObstacle.Boulder, 3), (BattleObstacle.Rubble, 2));

        /// <summary>The obstacles of the land; a fight at a shore (water) gets the beach's.</summary>
        public static FieldObstacles For(TerrainType terrain)
        {
            switch (terrain)
            {
                case TerrainType.Grass: return Grass;
                case TerrainType.Dirt: return Dirt;
                case TerrainType.Sand: return Sand;
                case TerrainType.Snow: return Snow;
                case TerrainType.Swamp: return Swamp;
                case TerrainType.Rough: return Rough;
                case TerrainType.Wasteland: return Wasteland;
                case TerrainType.Rock: return Underground;
                case TerrainType.Water: return Sand;
                default: return Grass;
            }
        }

        /// <summary>The most hexes a clump of <paramref name="kind"/> covers: a grove or a pond spreads, a boulder does not.</summary>
        public static int MaxClump(BattleObstacle kind)
        {
            switch (kind)
            {
                case BattleObstacle.Tree:
                case BattleObstacle.Pine:
                case BattleObstacle.Pool:
                    return 3;
                case BattleObstacle.Boulder:
                case BattleObstacle.Stump:
                case BattleObstacle.Bones:
                    return 1;
                default:
                    return 2;
            }
        }

        /// <summary>A shape for a clump of <paramref name="kind"/>, drawn from <paramref name="random"/>.</summary>
        public static HexSide[] Shape(BattleObstacle kind, IRandom random)
        {
            int size = random.Range(1, Math.Min(MaxClump(kind), Shapes.Length) + 1);
            HexSide[][] shapes = Shapes[size - 1];
            return shapes[random.Range(0, shapes.Length)];
        }
    }
}
