using System;
using System.Collections.Generic;

namespace Portfolio.Heroes
{
    /// <summary>What a hero meets at the end of a path.</summary>
    public enum PathEnd
    {
        /// <summary>An empty cell to stand on.</summary>
        Move,
        /// <summary>Something to pick up; the hero stands on its cell.</summary>
        Pickup,
        /// <summary>A building visited from the cell before it.</summary>
        Visit,
        /// <summary>The hero's own town: the hero stands in its gate.</summary>
        EnterTown,
        /// <summary>A fight: a monster, an enemy hero, a town of somebody else, or a cell a monster guards.</summary>
        Battle,
        /// <summary>A hero of the same player, to swap troops with.</summary>
        Meet
    }

    /// <summary>A path of a hero: the cells after the start, what each step costs, and where today's movement ends.</summary>
    public sealed class MovePlan
    {
        public readonly List<int> Cells = new List<int>();
        /// <summary>Movement points spent to arrive at each cell, from the start.</summary>
        public readonly List<int> Cost = new List<int>();
        public PathEnd End;
        /// <summary>How many cells of the path today's movement reaches.</summary>
        public int Today;
        /// <summary>The last cell is entered (true) or only reached from the one before (false: buildings, fights, meetings).</summary>
        public bool EntersLast;

        public int Destination => Cells.Count > 0 ? Cells[Cells.Count - 1] : -1;
        public int TotalCost => Cost.Count > 0 ? Cost[Cost.Count - 1] : 0;
        public bool ReachesToday => Today >= Cells.Count;
    }

    /// <summary>A binary heap of cells keyed by cost, ties broken by the cell index so every device picks the same path.</summary>
    internal sealed class CellQueue
    {
        private readonly List<long> heap = new List<long>();

        public int Count => heap.Count;

        public void Clear()
        {
            heap.Clear();
        }

        public void Push(int cost, int cell)
        {
            long key = ((long)cost << 24) | (uint)cell;
            heap.Add(key);
            int i = heap.Count - 1;
            while (i > 0)
            {
                int parent = (i - 1) / 2;
                if (heap[parent] <= heap[i])
                {
                    break;
                }
                (heap[parent], heap[i]) = (heap[i], heap[parent]);
                i = parent;
            }
        }

        public int Pop(out int cost)
        {
            long top = heap[0];
            long last = heap[heap.Count - 1];
            heap.RemoveAt(heap.Count - 1);
            if (heap.Count > 0)
            {
                heap[0] = last;
                int i = 0;
                while (true)
                {
                    int left = i * 2 + 1;
                    int right = left + 1;
                    int smallest = i;
                    if (left < heap.Count && heap[left] < heap[smallest])
                    {
                        smallest = left;
                    }
                    if (right < heap.Count && heap[right] < heap[smallest])
                    {
                        smallest = right;
                    }
                    if (smallest == i)
                    {
                        break;
                    }
                    (heap[smallest], heap[i]) = (heap[i], heap[smallest]);
                    i = smallest;
                }
            }
            cost = (int)(top >> 24);
            return (int)(top & 0xFFFFFF);
        }
    }

    public sealed partial class HeroesGame
    {
        private int[] searchCost = Array.Empty<int>();
        private int[] searchFrom = Array.Empty<int>();
        private int[] guarded = Array.Empty<int>();
        private readonly CellQueue queue = new CellQueue();
        private readonly List<int> neighborScratch = new List<int>(6);

        /// <summary>What stepping onto <paramref name="to"/> from <paramref name="from"/> costs the hero.</summary>
        public int StepCost(HeroState hero, int from, int to)
        {
            if (Map.HasRoad(from) && Map.HasRoad(to))
            {
                return Land.RoadCost;
            }
            TerrainType terrain = Map.TerrainAt(to);
            int cost = Land.Cost(terrain);
            HeroDef def = hero.Def;
            if (def != null && terrain == Land.Native(def.Faction))
            {
                return 100;
            }
            int pathfinding = hero.SkillLevel(SkillId.Pathfinding);
            if (cost > 100 && pathfinding > 0)
            {
                cost = 100 + (cost - 100) * (4 - pathfinding) / 4;
            }
            return cost;
        }

        /// <summary>The monster that guards <paramref name="cell"/> (stands next to it), or null.</summary>
        public MapObject GuardOf(int cell)
        {
            Grid.Neighbors(cell, neighborScratch);
            MapObject guard = null;
            foreach (int next in neighborScratch)
            {
                MapObject obj = State.ObjectAt(next);
                if (obj != null && obj.kind == ObjectKind.Monster && obj.cell == next && (guard == null || obj.id < guard.id))
                {
                    guard = obj;
                }
            }
            return guard;
        }

        public TownState TownAtGate(int cell)
        {
            foreach (TownState town in State.towns)
            {
                if (town.cell == cell)
                {
                    return town;
                }
            }
            return null;
        }

        /// <summary>What a hero of <paramref name="player"/> meets on <paramref name="cell"/> when it is the end of a path.</summary>
        public PathEnd EndAt(int player, int cell, out bool enters)
        {
            enters = true;
            HeroState other = State.HeroAt(cell);
            TownState town = TownAtGate(cell);
            if (town != null)
            {
                if (town.owner == player || (town.owner >= 0 && SameTeam(town.owner, player)))
                {
                    if (other != null && other.owner != player)
                    {
                        enters = false;
                        return PathEnd.Battle;
                    }
                    if (other != null)
                    {
                        enters = false;
                        return PathEnd.Meet;
                    }
                    return PathEnd.EnterTown;
                }
                enters = false;
                return PathEnd.Battle;
            }
            if (other != null)
            {
                enters = false;
                return other.owner == player || SameTeam(other.owner, player) ? PathEnd.Meet : PathEnd.Battle;
            }
            MapObject obj = State.ObjectAt(cell);
            if (obj != null)
            {
                if (obj.kind == ObjectKind.Monster)
                {
                    enters = false;
                    return PathEnd.Battle;
                }
                if (MapObjects.IsPickup(obj.kind))
                {
                    return GuardOf(cell) != null || GuardianOf(obj) != null ? PathEnd.Battle : PathEnd.Pickup;
                }
                enters = false;
                return GuardianOf(obj) != null && obj.owner != player ? PathEnd.Battle : PathEnd.Visit;
            }
            return GuardOf(cell) != null ? PathEnd.Battle : PathEnd.Move;
        }

        public bool SameTeam(int a, int b)
        {
            PlayerState pa = State.Player(a);
            PlayerState pb = State.Player(b);
            return pa != null && pb != null && pa.team == pb.team;
        }

        /// <summary>Whether a path may go on through <paramref name="cell"/> (not only end there). Uses the guards of the current search.</summary>
        private bool PassThrough(int cell)
        {
            if (!Map.Open(cell) || Map.occupant[cell] >= 0 || State.HeroAt(cell) != null)
            {
                return false;
            }
            return guarded[cell] < 0;
        }

        /// <summary>Whether a path may end on <paramref name="cell"/> because a monster guards it (a fight on open land).</summary>
        private bool GuardedLand(int cell)
        {
            return guarded[cell] >= 0 && Map.Open(cell) && Map.occupant[cell] < 0 && State.HeroAt(cell) == null;
        }

        /// <summary>Whether a path may end on <paramref name="cell"/>: open land, or something to visit.</summary>
        private bool CanEnd(int cell)
        {
            if (Map.occupant[cell] >= 0 || State.HeroAt(cell) != null)
            {
                return true;
            }
            return Map.Open(cell);
        }

        private void EnsureSearch()
        {
            int count = Grid.Count;
            if (searchCost.Length != count)
            {
                searchCost = new int[count];
                searchFrom = new int[count];
            }
            for (int i = 0; i < count; i++)
            {
                searchCost[i] = int.MaxValue;
                searchFrom[i] = -1;
            }
            if (guarded.Length != count)
            {
                guarded = new int[count];
            }
            for (int i = 0; i < count; i++)
            {
                guarded[i] = -1;
            }
            foreach (MapObject obj in State.objects)
            {
                if (obj.removed || obj.kind != ObjectKind.Monster)
                {
                    continue;
                }
                Grid.Neighbors(obj.cell, neighborScratch);
                foreach (int next in neighborScratch)
                {
                    if (guarded[next] < 0 || obj.id < guarded[next])
                    {
                        guarded[next] = obj.id;
                    }
                }
            }
        }

        /// <summary>The cheapest path of <paramref name="hero"/> to <paramref name="target"/>, or null when there is none.</summary>
        public MovePlan PlanPath(HeroState hero, int target)
        {
            if (hero == null || !hero.alive || !Grid.Valid(target))
            {
                return null;
            }
            // Clicking any cell of a town or a building means its gate.
            MapObject targetObject = State.ObjectAt(target);
            if (targetObject != null)
            {
                target = targetObject.cell;
            }
            if (target == hero.cell || !CanEnd(target))
            {
                return null;
            }
            EnsureSearch();
            queue.Clear();
            searchCost[hero.cell] = 0;
            queue.Push(0, hero.cell);
            while (queue.Count > 0)
            {
                int cell = queue.Pop(out int cost);
                if (cost > searchCost[cell])
                {
                    continue;
                }
                if (cell == target)
                {
                    break;
                }
                if (cell != hero.cell && !PassThrough(cell))
                {
                    continue;
                }
                Grid.Neighbors(cell, neighborScratch);
                foreach (int next in neighborScratch)
                {
                    if (next != target && !PassThrough(next) && !GuardedLand(next))
                    {
                        continue;
                    }
                    int total = cost + StepCost(hero, cell, next);
                    if (total < searchCost[next])
                    {
                        searchCost[next] = total;
                        searchFrom[next] = cell;
                        queue.Push(total, next);
                    }
                }
            }
            if (searchFrom[target] < 0)
            {
                return null;
            }
            var plan = new MovePlan();
            var reversed = new List<int>();
            for (int cell = target; cell != hero.cell; cell = searchFrom[cell])
            {
                reversed.Add(cell);
            }
            for (int i = reversed.Count - 1; i >= 0; i--)
            {
                plan.Cells.Add(reversed[i]);
                plan.Cost.Add(searchCost[reversed[i]]);
            }
            // A guarded cell on the way ends the walk there: the monster attacks.
            for (int i = 0; i < plan.Cells.Count - 1; i++)
            {
                if (GuardOf(plan.Cells[i]) != null)
                {
                    plan.Cells.RemoveRange(i + 1, plan.Cells.Count - i - 1);
                    plan.Cost.RemoveRange(i + 1, plan.Cost.Count - i - 1);
                    break;
                }
            }
            plan.End = EndAt(hero.owner, plan.Destination, out bool enters);
            plan.EntersLast = enters;
            int today = 0;
            while (today < plan.Cells.Count && plan.Cost[today] <= hero.movement)
            {
                today++;
            }
            plan.Today = today;
            return plan;
        }

        /// <summary>
        /// Every cell the hero could reach within <paramref name="budget"/> movement points, with its cost (int.MaxValue
        /// where it cannot go). Cells that end a path (objects, fights) are included but not gone through.
        /// </summary>
        public int[] Reachable(HeroState hero, int budget)
        {
            EnsureSearch();
            queue.Clear();
            searchCost[hero.cell] = 0;
            queue.Push(0, hero.cell);
            while (queue.Count > 0)
            {
                int cell = queue.Pop(out int cost);
                if (cost > searchCost[cell])
                {
                    continue;
                }
                if (cell != hero.cell && !PassThrough(cell))
                {
                    continue;
                }
                Grid.Neighbors(cell, neighborScratch);
                foreach (int next in neighborScratch)
                {
                    if (!CanEnd(next))
                    {
                        continue;
                    }
                    int total = cost + StepCost(hero, cell, next);
                    if (total <= budget && total < searchCost[next])
                    {
                        searchCost[next] = total;
                        searchFrom[next] = cell;
                        queue.Push(total, next);
                    }
                }
            }
            return (int[])searchCost.Clone();
        }
    }
}
