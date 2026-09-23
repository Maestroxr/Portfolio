using System;
using System.Collections.Generic;
using Gamebox.Lockstep;

namespace Portfolio.Heroes
{
    public sealed partial class HeroesGame
    {
        /// <summary>
        /// Sets up a battlefield of its own (<see cref="BattleStyle.Battlefield"/>) for <paramref name="battle"/>: every
        /// cell of the fifteen by eleven field, the land the armies met on, the seed of its layout and the obstacles (and
        /// a town's wall). The seed is made of what everybody knows of the fight (the game's seed, the day, the place,
        /// who fights), never drawn from <see cref="Random"/>, so the same fight gets the same field on every device and
        /// the dice of the game are not touched.
        /// </summary>
        private void BuildField(BattleState battle, TownState town)
        {
            TerrainType terrain = Map.TerrainAt(battle.mapCell);
            if (!Land.Walkable(terrain) && Grid.Valid(battle.attackerCell))
            {
                // A fight at the water's edge (or at a rock face) is fought on the attacker's side of it.
                terrain = Map.TerrainAt(battle.attackerCell);
            }
            battle.terrain = (int)terrain;
            battle.fieldSeed = SeededRandom.Mix(State.seed, (uint)State.day, (uint)battle.mapCell, (uint)battle.attackerHero,
                (uint)battle.attackerCell, (uint)battle.defenderHero, (uint)battle.monsterObject, (uint)battle.town);
            bool walled = town != null && (town.Has(BuildingId.Fort) || Buildings.Towers(town) > 0);
            LayOutField(battle, walled, town != null ? Buildings.Towers(town) : 0);
        }

        /// <summary>
        /// Lays out a battlefield of its own from <paramref name="battle"/>'s <see cref="BattleState.fieldSeed"/> and
        /// <see cref="BattleState.terrain"/>: its cells, a town's wall (<paramref name="walled"/>, with room for
        /// <paramref name="towers"/> arrow towers) and the clumps of obstacles of the land, in the columns between the two
        /// armies (before the wall in a siege). A layout that would cut any open cell off from the attacker's columns is
        /// thrown away and another tried; after <see cref="Battlefields.Attempts"/> the field stays bare. The same seed
        /// always gives the same field.
        /// </summary>
        public static void LayOutField(BattleState battle, bool walled, int towers)
        {
            HexGrid field = battle.field;
            battle.cells.Clear();
            battle.blocked.Clear();
            battle.obstacleCells.Clear();
            battle.obstacleKinds.Clear();
            battle.walls.Clear();
            battle.gate = -1;
            for (int cell = 0; cell < field.Count; cell++)
            {
                battle.cells.Add(cell);
            }
            if (walled)
            {
                BuildWall(battle, towers);
            }
            int fixedObstacles = battle.obstacleCells.Count;
            int lastColumn = walled ? Battlefields.WallColumn - 2 : Battlefields.LastObstacleColumn;
            FieldObstacles set = Battlefields.For((TerrainType)battle.terrain);
            var random = new SeededRandom(battle.fieldSeed);
            var clump = new List<int>(3);
            for (int attempt = 0; attempt < Battlefields.Attempts; attempt++)
            {
                RemoveObstaclesFrom(battle, fixedObstacles);
                int clumps = random.Range(set.MinClumps, set.MaxClumps + 1);
                for (int i = 0; i < clumps; i++)
                {
                    BattleObstacle kind = set.Pick(random);
                    HexSide[] shape = Battlefields.Shape(kind, random);
                    int cell = field.Index(random.Range(Battlefields.FirstObstacleColumn, lastColumn + 1), random.Range(0, field.rows));
                    clump.Clear();
                    clump.Add(cell);
                    foreach (HexSide side in shape)
                    {
                        cell = cell >= 0 ? field.Neighbor(cell, side) : -1;
                        clump.Add(cell);
                    }
                    bool fits = true;
                    foreach (int each in clump)
                    {
                        if (each < 0 || field.Column(each) < Battlefields.FirstObstacleColumn || field.Column(each) > lastColumn || battle.IsBlocked(each))
                        {
                            fits = false;
                            break;
                        }
                    }
                    if (!fits)
                    {
                        continue;
                    }
                    foreach (int each in clump)
                    {
                        AddObstacle(battle, each, kind);
                    }
                }
                if (FieldHangsTogether(battle))
                {
                    return;
                }
            }
            RemoveObstaclesFrom(battle, fixedObstacles);
        }

        /// <summary>
        /// A town's wall across the field in <see cref="Battlefields.WallColumn"/>: blocked but for the breaches, the gate
        /// (<see cref="BattleState.gate"/>, for the defenders only) and the cells its arrow towers will stand in (which turn
        /// to wall when a tower falls, <see cref="TowerFell"/>).
        /// </summary>
        private static void BuildWall(BattleState battle, int towers)
        {
            HexGrid field = battle.field;
            towers = Math.Max(0, Math.Min(towers, Battlefields.TowerRows.Length));
            for (int row = 0; row < field.rows; row++)
            {
                int cell = field.Index(Battlefields.WallColumn, row);
                if (row == Battlefields.GateRow)
                {
                    battle.gate = cell;
                    continue;
                }
                int tower = Array.IndexOf(Battlefields.TowerRows, row);
                if (Array.IndexOf(Battlefields.BreachRows, row) >= 0 || (tower >= 0 && tower < towers))
                {
                    continue;
                }
                battle.walls.Add(cell);
                AddObstacle(battle, cell, BattleObstacle.Wall);
            }
        }

        /// <summary>
        /// An arrow tower in a town's wall on a battlefield has fallen: its stones fill the wall where it stood, which
        /// becomes wall like the rest (the breaches and the gate stay the only ways through). On the map a tower's cell
        /// is left as it was.
        /// </summary>
        private static void TowerFell(BattleState battle, int cell)
        {
            if (!battle.IsField || battle.gate < 0 || battle.field.Column(cell) != Battlefields.WallColumn || battle.IsBlocked(cell))
            {
                return;
            }
            battle.walls.Add(cell);
            AddObstacle(battle, cell, BattleObstacle.Wall);
        }

        private static void AddObstacle(BattleState battle, int cell, BattleObstacle kind)
        {
            battle.blocked.Add(cell);
            battle.obstacleCells.Add(cell);
            battle.obstacleKinds.Add((int)kind);
        }

        /// <summary>Takes the obstacles from index <paramref name="first"/> on off the field again.</summary>
        private static void RemoveObstaclesFrom(BattleState battle, int first)
        {
            for (int i = battle.obstacleCells.Count - 1; i >= first; i--)
            {
                battle.blocked.Remove(battle.obstacleCells[i]);
                battle.obstacleCells.RemoveAt(i);
                battle.obstacleKinds.RemoveAt(i);
            }
        }

        /// <summary>Clears one obstacle of a battlefield (troops trampled it): off the obstacles, the walls and the blocked cells.</summary>
        private static void ClearObstacle(BattleState battle, int cell)
        {
            battle.blocked.Remove(cell);
            battle.walls.Remove(cell);
            int index = battle.obstacleCells.IndexOf(cell);
            if (index >= 0)
            {
                battle.obstacleCells.RemoveAt(index);
                battle.obstacleKinds.RemoveAt(index);
            }
        }

        /// <summary>
        /// Whether a besieger walking from the attacker's first column can get to every open cell of a battlefield
        /// (the gate aside, which he cannot pass): no pockets, no side cut off.
        /// </summary>
        public static bool FieldHangsTogether(BattleState battle)
        {
            HexGrid field = battle.field;
            var seen = new bool[field.Count];
            var queue = new Queue<int>();
            int open = 0;
            for (int cell = 0; cell < field.Count; cell++)
            {
                if (!battle.Passable(cell, 0))
                {
                    continue;
                }
                open++;
                if (field.Column(cell) == 0)
                {
                    seen[cell] = true;
                    queue.Enqueue(cell);
                }
            }
            int reached = 0;
            var around = new List<int>(6);
            while (queue.Count > 0)
            {
                int cell = queue.Dequeue();
                reached++;
                field.Neighbors(cell, around);
                foreach (int next in around)
                {
                    if (!seen[next] && battle.Passable(next, 0))
                    {
                        seen[next] = true;
                        queue.Enqueue(next);
                    }
                }
            }
            return reached == open;
        }

        /// <summary>
        /// The arrow towers of a town under siege on a battlefield: in the cells of the wall kept for them
        /// (<see cref="Battlefields.TowerRows"/>), as many as the town has built. A cell somebody stands on is skipped.
        /// </summary>
        private void DeployFieldTowers(BattleState battle, TownState town)
        {
            if (battle.gate < 0)
            {
                return;
            }
            int towers = Math.Min(Buildings.Towers(town), Battlefields.TowerRows.Length);
            for (int i = 0; i < towers; i++)
            {
                int cell = battle.field.Index(Battlefields.WallColumn, Battlefields.TowerRows[i]);
                if (battle.IsBlocked(cell) || battle.StackAt(cell) != null)
                {
                    continue;
                }
                AddStack(battle, 1, (int)CreatureId.ArrowTower, 1, -1, 2, 0, cell);
            }
        }
    }
}
