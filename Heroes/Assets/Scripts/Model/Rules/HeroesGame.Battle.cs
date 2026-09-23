using System;
using System.Collections.Generic;

namespace Portfolio.Heroes
{
    public sealed partial class HeroesGame
    {
        /// <summary>
        /// Starts a battle where <paramref name="hero"/> met what stands on <paramref name="target"/>: a monster, a hero,
        /// a town. In the style of the game (<see cref="ScenarioRules.battleStyle"/>) the battlefield is either the block
        /// of map cells around the target, the attacker lining up on the side it came from and the defender on the other,
        /// or a battlefield of its own (see HeroesGame.Battlefield), the attacker on the left and the defender on the
        /// right. Everything after the setup (turns, reach, blows, spells, the end) is the same in both.
        ///
        /// The events of a battle, in both styles (cells are field cells on a battlefield of its own, map cells on the
        /// map; see <see cref="EventKind"/> for each one's numbers): BattleStarted with a copy of the battle as it was
        /// deployed, RoundBegan, then per turn StackTurn (or MoraleFail), the action (StackMoved, StackAttacked,
        /// StackShot, StackWaited, StackDefended, SpellCast with EffectAdded, StackDamaged or StackHealed), LuckyStrike
        /// before the blow it doubles, StackDied after the blow that killed, MoraleBoost and another StackTurn for good
        /// morale; and BattleEnded (with a copy of the battle as it ended) before what the battle does to the map
        /// (HeroDefeated, TownCaptured, HeroMoved, ObjectRemoved, CreaturesRaised by necromancy, ExperienceGained...).
        /// </summary>
        private void StartBattle(HeroState hero, int target, int prize)
        {
            var battle = new BattleState
            {
                active = true,
                attackerHero = hero.id,
                attackerPlayer = hero.owner,
                attackerCell = hero.cell,
                targetCell = target,
                center = target,
                prize = prize
            };
            State.battle = battle;
            TownState town = TownAtGate(target);
            HeroState defender = State.HeroAt(target);
            MapObject monster = null;
            if (town != null)
            {
                battle.town = town.id;
                battle.defenderPlayer = town.owner;
                battle.center = town.center;
            }
            else if (defender == null)
            {
                MapObject obj = State.ObjectAt(target);
                if (obj != null && obj.kind == ObjectKind.Monster)
                {
                    monster = obj;
                }
                else
                {
                    monster = GuardOf(target) ?? GuardOf(hero.cell);
                }
                if (monster == null)
                {
                    battle.active = false;
                    return;
                }
                battle.monsterObject = monster.id;
                battle.targetCell = monster.cell;
                battle.center = monster.cell;
            }
            if (defender != null && defender.owner != hero.owner)
            {
                battle.defenderHero = defender.id;
                battle.defenderPlayer = defender.owner;
            }
            battle.mapCell = battle.center;
            if (State.rules.battleStyle == BattleStyle.Battlefield)
            {
                battle.style = BattleStyle.Battlefield;
                battle.field = new HexGrid(BattleState.FieldColumns, BattleState.FieldRows);
                battle.center = battle.field.Index(BattleState.HalfWidth, BattleState.HalfHeight);
                battle.attackerLeft = true;
                BuildField(battle, town);
            }
            else
            {
                battle.terrain = (int)Map.TerrainAt(battle.mapCell);
                battle.attackerLeft = Grid.X2(hero.cell) <= Grid.X2(battle.center);
                BuildBattlefield(battle, hero, defender, monster, town);
            }
            DeployArmy(battle, 0, hero.army, 0, ArmyHealthBonus(hero));
            if (defender != null && battle.defenderHero >= 0)
            {
                DeployArmy(battle, 1, defender.army, 0, ArmyHealthBonus(defender));
            }
            if (town != null)
            {
                DeployArmy(battle, 1, town.garrison, 1, 0);
                if (battle.IsField)
                {
                    DeployFieldTowers(battle, town);
                }
                else
                {
                    DeployTowers(battle, town);
                }
            }
            if (monster != null)
            {
                DeployMonsters(battle, monster);
            }
            ConnectSides(battle);
            GameEvent started = Emit(EventKind.BattleStarted, hero.owner, battle.mapCell, battle.defenderPlayer, battle.monsterObject, battle.town, (int)battle.style);
            started.battle = battle.Clone();
            if (battle.AliveCount(1, true) == 0)
            {
                // An empty town (or a hero without troops) gives in at once.
                EndBattle(BattleResult.AttackerWon);
                return;
            }
            NextRound();
            AdvanceTurn();
        }

        private void BuildBattlefield(BattleState battle, HeroState hero, HeroState defender, MapObject monster, TownState town)
        {
            int cx = Grid.X2(battle.center);
            int centerRow = Grid.Row(battle.center);
            for (int row = centerRow - BattleState.HalfHeight; row <= centerRow + BattleState.HalfHeight; row++)
            {
                if (row < 0 || row >= Grid.rows)
                {
                    continue;
                }
                for (int column = 0; column < Grid.columns; column++)
                {
                    int cell = Grid.Index(column, row);
                    if (Math.Abs(Grid.X2(cell) - cx) > BattleState.HalfWidth * 2)
                    {
                        continue;
                    }
                    battle.cells.Add(cell);
                    if (!Map.Open(cell))
                    {
                        battle.blocked.Add(cell);
                        continue;
                    }
                    int occupant = Map.occupant[cell];
                    bool combatantObject = (monster != null && occupant == monster.id) || (battle.prize >= 0 && occupant == battle.prize && MapObjects.IsPickup(State.Object(battle.prize).kind));
                    if (occupant >= 0 && !combatantObject)
                    {
                        battle.blocked.Add(cell);
                        continue;
                    }
                    HeroState standing = State.HeroAt(cell);
                    if (standing != null && standing != hero && standing != defender)
                    {
                        battle.blocked.Add(cell);
                    }
                }
            }
        }

        /// <summary>
        /// Woods of the map may cut the field in two. Troops trample a way through them (only for this battle: the map
        /// keeps its trees) until every stack of the defender can be walked to from the attacker's side, and every stack
        /// of the attacker (one may have lined up in a pocket of the woods) from the defender's: straight toward it when
        /// only woods are in the way, else by the way that tramples least. Water, rock and buildings are never trampled;
        /// a troop no such way reaches comes round to the nearest cell the other side can walk to. A battlefield of its
        /// own is laid out to hang together already, so there this only guards against the odd case.
        /// </summary>
        private void ConnectSides(BattleState battle)
        {
            HexGrid grid = BattleGrid;
            for (int attempt = 0; attempt < 16; attempt++)
            {
                // The defenders first, then the attackers; done when no troop of either is cut off.
                if (!ConnectOne(battle, grid, 1) && !ConnectOne(battle, grid, 0))
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Finds the first troop of <paramref name="side"/> that the other side cannot walk up to (towers aside, which
        /// stand where they stand) and tramples a way to it, or brings it round. False when every troop of the side can be
        /// reached (or none can be helped).
        /// </summary>
        private bool ConnectOne(BattleState battle, HexGrid grid, int side)
        {
            var reached = new HashSet<int>();
            var queue = new Queue<int>();
            foreach (BattleStack stack in battle.stacks)
            {
                if (stack.side != side && stack.alive && !stack.IsTower && reached.Add(stack.cell))
                {
                    queue.Enqueue(stack.cell);
                }
            }
            while (queue.Count > 0)
            {
                int cell = queue.Dequeue();
                grid.Neighbors(cell, scratch);
                foreach (int next in scratch)
                {
                    if (battle.Passable(next, 0) && reached.Add(next))
                    {
                        queue.Enqueue(next);
                    }
                }
            }
            if (reached.Count == 0)
            {
                return false;
            }
            BattleStack cut = null;
            foreach (BattleStack stack in battle.stacks)
            {
                if (stack.side != side || !stack.alive || stack.IsTower)
                {
                    continue;
                }
                bool touches = reached.Contains(stack.cell);
                grid.Neighbors(stack.cell, scratch);
                foreach (int next in scratch)
                {
                    touches |= reached.Contains(next);
                }
                if (!touches)
                {
                    cut = stack;
                    break;
                }
            }
            if (cut == null)
            {
                return false;
            }
            // The reached cells in order, so every device walks them alike.
            var starts = new List<int>(reached);
            starts.Sort();
            List<int> way = StraightWay(battle, grid, starts, cut) ?? TrampledWay(battle, grid, starts, cut);
            if (way == null)
            {
                // Nothing but water, rock and walls between: the troop comes round to where the fight is.
                int best = -1;
                int nearest = int.MaxValue;
                foreach (int cell in starts)
                {
                    int d = grid.Distance2x4(cell, cut.cell);
                    if (d < nearest && battle.StackAt(cell) == null && cell != battle.gate)
                    {
                        nearest = d;
                        best = cell;
                    }
                }
                if (best < 0)
                {
                    return false;
                }
                cut.cell = best;
                return true;
            }
            foreach (int cell in way)
            {
                if (battle.IsField)
                {
                    ClearObstacle(battle, cell);
                }
                else
                {
                    battle.blocked.Remove(cell);
                }
            }
            return true;
        }

        /// <summary>
        /// The blocked cells between the reached cell nearest the cut off stack and the stack, going straight toward it;
        /// null when something that cannot be trampled (or passed) is on that line, or the line leaves the field.
        /// </summary>
        private List<int> StraightWay(BattleState battle, HexGrid grid, List<int> starts, BattleStack cut)
        {
            int from = -1;
            int nearest = int.MaxValue;
            foreach (int cell in starts)
            {
                int d = grid.Distance2x4(cell, cut.cell);
                if (d < nearest)
                {
                    nearest = d;
                    from = cell;
                }
            }
            var way = new List<int>();
            int current = from;
            for (int guard = 0; guard < 40 && current != cut.cell && !grid.Adjacent(current, cut.cell); guard++)
            {
                grid.Neighbors(current, scratch);
                int step = -1;
                int best = int.MaxValue;
                foreach (int next in scratch)
                {
                    int d = grid.Distance2x4(next, cut.cell);
                    if (battle.Contains(next) && (d < best || (d == best && next < step)))
                    {
                        best = d;
                        step = next;
                    }
                }
                if (step < 0)
                {
                    return null;
                }
                if (!battle.Passable(step, 0))
                {
                    if (!battle.IsBlocked(step) || !CanTrample(battle, step))
                    {
                        return null;
                    }
                    way.Add(step);
                }
                current = step;
            }
            return current == cut.cell || grid.Adjacent(current, cut.cell) ? way : null;
        }

        /// <summary>
        /// The way from the reached cells to the cut off stack that tramples the fewest cells (a breadth first search that
        /// walks open cells for free), as the cells to trample; null when there is none.
        /// </summary>
        private List<int> TrampledWay(BattleState battle, HexGrid grid, List<int> starts, BattleStack cut)
        {
            var cost = new Dictionary<int, int>();
            var from = new Dictionary<int, int>();
            var open = new LinkedList<int>();
            foreach (int cell in starts)
            {
                cost[cell] = 0;
                from[cell] = -1;
                open.AddLast(cell);
            }
            var around = new List<int>(6);
            int end = -1;
            while (open.Count > 0)
            {
                int cell = open.First.Value;
                open.RemoveFirst();
                if (cell == cut.cell || grid.Adjacent(cell, cut.cell))
                {
                    end = cell;
                    break;
                }
                grid.Neighbors(cell, around);
                foreach (int next in around)
                {
                    int step = battle.Passable(next, 0) ? 0 : battle.Contains(next) && CanTrample(battle, next) ? 1 : -1;
                    if (step < 0)
                    {
                        continue;
                    }
                    int total = cost[cell] + step;
                    if (cost.TryGetValue(next, out int known) && known <= total)
                    {
                        continue;
                    }
                    cost[next] = total;
                    from[next] = cell;
                    if (step == 0)
                    {
                        open.AddFirst(next);
                    }
                    else
                    {
                        open.AddLast(next);
                    }
                }
            }
            if (end < 0)
            {
                return null;
            }
            var way = new List<int>();
            for (int cell = end; cell >= 0; cell = from[cell])
            {
                if (battle.IsBlocked(cell))
                {
                    way.Add(cell);
                }
            }
            return way;
        }

        /// <summary>
        /// Whether troops may trample a blocked cell of the field for the battle: woods and dead trees on the map (never
        /// water, rock, buildings or somebody standing there), any obstacle but a wall on a battlefield of its own.
        /// </summary>
        private bool CanTrample(BattleState battle, int cell)
        {
            if (battle.IsField)
            {
                BattleObstacle kind = battle.ObstacleAt(cell);
                return kind != BattleObstacle.None && kind != BattleObstacle.Wall;
            }
            if (!Grid.Valid(cell) || !Land.Walkable(Map.TerrainAt(cell)) || Map.occupant[cell] >= 0 || State.HeroAt(cell) != null)
            {
                return false;
            }
            Obstacle obstacle = Map.ObstacleAt(cell);
            return obstacle == Obstacle.Forest || obstacle == Obstacle.DeadTrees;
        }

        /// <summary>Rows (from the center row) the stacks of an army line up on, like the columns of old: spread out, the middle last.</summary>
        private static readonly int[][] DeployRows =
        {
            new int[0],
            new[] { 0 },
            new[] { -2, 2 },
            new[] { -3, 0, 3 },
            new[] { -4, -1, 1, 4 },
            new[] { -4, -2, 0, 2, 4 },
            new[] { -5, -3, -1, 1, 3, 5 },
            new[] { -5, -3, -1, 0, 1, 3, 5 }
        };

        /// <summary>
        /// The free cell nearest the place a stack of <paramref name="side"/> lines up at: <paramref name="depth"/>
        /// columns in from the side's edge of the field, <paramref name="rowOffset"/> rows from the middle one, on the
        /// side's own half. On a battlefield of its own the edge is exactly the first (or last) column, and the defenders
        /// of a walled town line up behind the wall. -1 when there is no room left.
        /// </summary>
        private int FreeDeployCell(BattleState battle, int side, int rowOffset, int depth)
        {
            HexGrid grid = BattleGrid;
            bool left = side == 0 ? battle.attackerLeft : !battle.attackerLeft;
            int cx = grid.X2(battle.center);
            int tx = cx + (left ? -1 : 1) * (BattleState.HalfWidth - depth) * 2;
            int ty = grid.Row(battle.center) + rowOffset;
            bool behindWall = false;
            if (battle.IsField)
            {
                int row = Math.Max(0, Math.Min(grid.rows - 1, ty));
                tx = grid.X2(grid.Index(left ? depth : grid.columns - 1 - depth, row));
                behindWall = side == 1 && battle.gate >= 0;
            }
            int best = -1;
            long bestDistance = long.MaxValue;
            foreach (int cell in battle.cells)
            {
                if (battle.IsBlocked(cell) || cell == battle.gate || battle.StackAt(cell) != null)
                {
                    continue;
                }
                int x = grid.X2(cell);
                // Stay on the own half of the field.
                if (left ? x > cx - 1 : x < cx + 1)
                {
                    continue;
                }
                if (behindWall && grid.Column(cell) <= Battlefields.WallColumn)
                {
                    continue;
                }
                int dx = x - tx;
                int dy = grid.Row(cell) - ty;
                long d = 3L * dx * dx + 11L * dy * dy;
                if (d < bestDistance || (d == bestDistance && cell < best))
                {
                    bestDistance = d;
                    best = cell;
                }
            }
            return best;
        }

        private BattleStack AddStack(BattleState battle, int side, int creature, int count, int slot, int source, int healthBonus, int cell)
        {
            CreatureDef def = Creatures.Get(creature);
            var stack = new BattleStack
            {
                id = battle.nextStack++,
                side = side,
                creature = creature,
                count = count,
                startCount = count,
                health = def.Health + healthBonus,
                cell = cell,
                slot = slot,
                source = source,
                shots = def.Shots,
                facing = (side == 0) == battle.attackerLeft ? (int)HexSide.East : (int)HexSide.West
            };
            battle.stacks.Add(stack);
            return stack;
        }

        private void DeployArmy(BattleState battle, int side, Army army, int source, int healthBonus)
        {
            var slots = new List<int>();
            for (int i = 0; i < army.slots.Length; i++)
            {
                if (!army.slots[i].IsEmpty)
                {
                    slots.Add(i);
                }
            }
            if (slots.Count == 0)
            {
                return;
            }
            int[] rows = DeployRows[Math.Min(7, slots.Count)];
            for (int i = 0; i < slots.Count; i++)
            {
                int rowOffset = i < rows.Length ? rows[i] : 0;
                int cell = FreeDeployCell(battle, side, rowOffset, source);
                if (cell < 0)
                {
                    continue;
                }
                ArmySlot slot = army.slots[slots[i]];
                AddStack(battle, side, slot.creature, slot.count, slots[i], source, healthBonus, cell);
            }
        }

        private void DeployMonsters(BattleState battle, MapObject monster)
        {
            CreatureDef def = Creatures.Get(monster.subtype);
            int total = Math.Max(1, monster.amount);
            int groups = def.Tier <= 3 ? 1 + total / 12 : 1 + total / 5;
            groups = Math.Max(1, Math.Min(Math.Min(5, groups), total));
            int[] rows = DeployRows[groups];
            int each = total / groups;
            int extra = total - each * groups;
            for (int i = 0; i < groups; i++)
            {
                int count = each + (i < extra ? 1 : 0);
                int cell = FreeDeployCell(battle, 1, rows[i], 0);
                if (cell >= 0 && count > 0)
                {
                    AddStack(battle, 1, monster.subtype, count, i, 0, 0, cell);
                }
            }
        }

        /// <summary>The arrow towers of a town under siege on the map: on its own cells nearest the besiegers.</summary>
        private void DeployTowers(BattleState battle, TownState town)
        {
            int towers = Buildings.Towers(town);
            if (towers == 0)
            {
                return;
            }
            MapObject obj = State.Object(town.objectId);
            var spots = new List<int>();
            foreach (int cell in obj.footprint)
            {
                if (cell != town.cell && battle.Contains(cell))
                {
                    spots.Add(cell);
                }
            }
            // The towers facing the besiegers first.
            spots.Sort((a, b) =>
            {
                int d1 = Grid.Distance2x4(a, battle.attackerCell);
                int d2 = Grid.Distance2x4(b, battle.attackerCell);
                return d1 != d2 ? d1.CompareTo(d2) : a.CompareTo(b);
            });
            for (int i = 0; i < Math.Min(towers, spots.Count); i++)
            {
                battle.blocked.Remove(spots[i]);
                AddStack(battle, 1, (int)CreatureId.ArrowTower, 1, -1, 2, 0, spots[i]);
            }
        }

        // ------------------------------------------------------------------ rounds and turns

        public int Speed(BattleStack stack)
        {
            CreatureDef def = stack.Def;
            if (def.Has(Ability.Immobile))
            {
                return 0;
            }
            int speed = def.Speed + ArmySpeedBonus(State.Hero(Battle.HeroOf(stack.side)));
            speed += stack.EffectAmount(SpellId.Haste);
            speed += stack.EffectAmount(SpellId.Prayer);
            if (stack.HasEffect(SpellId.Slow))
            {
                speed = speed / 2;
            }
            return Math.Max(1, speed);
        }

        private int OrderSpeed(BattleStack stack)
        {
            return stack.IsTower ? 10 : Speed(stack);
        }

        private void NextRound()
        {
            BattleState battle = Battle;
            battle.round++;
            if (battle.round > 60)
            {
                // Nobody can get at anybody any more: the attacker breaks off.
                EndBattle(BattleResult.AttackerFled, true);
                return;
            }
            battle.attackerCast = false;
            battle.defenderCast = false;
            foreach (BattleStack stack in battle.stacks)
            {
                if (!stack.alive)
                {
                    continue;
                }
                stack.retaliations = 0;
                stack.acted = false;
                stack.waited = false;
                stack.moraleUsed = false;
                if (battle.round > 1)
                {
                    for (int i = stack.effects.Count - 1; i >= 0; i--)
                    {
                        stack.effects[i].rounds--;
                        if (stack.effects[i].rounds <= 0)
                        {
                            stack.effects.RemoveAt(i);
                        }
                    }
                    if (stack.Def.Has(Ability.Regenerate))
                    {
                        int full = stack.Def.Health + ArmyHealthBonus(State.Hero(battle.HeroOf(stack.side)));
                        if (stack.health < full)
                        {
                            int healed = full - stack.health;
                            stack.health = full;
                            Emit(EventKind.StackHealed, battle.PlayerOf(stack.side), stack.id, healed, 0, stack.cell);
                        }
                    }
                }
            }
            battle.order.Clear();
            battle.waiting.Clear();
            var alive = new List<BattleStack>();
            foreach (BattleStack stack in battle.stacks)
            {
                if (stack.alive)
                {
                    alive.Add(stack);
                }
            }
            alive.Sort((a, b) =>
            {
                int sa = OrderSpeed(a), sb = OrderSpeed(b);
                if (sa != sb)
                {
                    return sb.CompareTo(sa);
                }
                if (a.side != b.side)
                {
                    return a.side.CompareTo(b.side);
                }
                return a.id.CompareTo(b.id);
            });
            foreach (BattleStack stack in alive)
            {
                battle.order.Add(stack.id);
            }
            Emit(EventKind.RoundBegan, -1, battle.round);
        }

        /// <summary>Hands the turn to the next stack of the round (starting the next round when all acted).</summary>
        private void AdvanceTurn()
        {
            BattleState battle = Battle;
            for (int guard = 0; guard < 400 && battle.active; guard++)
            {
                BattleStack next = null;
                foreach (int id in battle.order)
                {
                    BattleStack stack = battle.Stack(id);
                    if (stack != null && stack.alive && !stack.acted && !stack.waited)
                    {
                        next = stack;
                        break;
                    }
                }
                if (next == null)
                {
                    // Those who waited go last, the slowest first.
                    BattleStack slowest = null;
                    foreach (int id in battle.waiting)
                    {
                        BattleStack stack = battle.Stack(id);
                        if (stack != null && stack.alive && !stack.acted && (slowest == null || OrderSpeed(stack) < OrderSpeed(slowest)))
                        {
                            slowest = stack;
                        }
                    }
                    next = slowest;
                }
                if (next == null)
                {
                    NextRound();
                    continue;
                }
                battle.current = next.id;
                next.defending = false;
                if (next.IsTower)
                {
                    TowerShot(next);
                    if (!battle.active)
                    {
                        return;
                    }
                    continue;
                }
                // Bad morale freezes a troop now and then (once a turn: not again when it comes back from waiting); the
                // undead feel nothing.
                int morale = next.waited ? 0 : StackMorale(next);
                if (morale < 0 && Random.Range(0, 100) < -morale * 4)
                {
                    next.acted = true;
                    Emit(EventKind.MoraleFail, battle.PlayerOf(next.side), next.id);
                    continue;
                }
                Emit(EventKind.StackTurn, battle.PlayerOf(next.side), next.id, next.cell);
                return;
            }
            // Nothing can act any more (a stalled battle): the attacker breaks off.
            if (battle.active)
            {
                EndBattle(BattleResult.AttackerFled, true);
            }
        }

        public int StackMorale(BattleStack stack)
        {
            if (stack.Def.IsUndead || stack.IsTower)
            {
                return 0;
            }
            int morale = Morale(State.Hero(Battle.HeroOf(stack.side)));
            foreach (BattleStack enemy in Battle.stacks)
            {
                if (enemy.alive && enemy.side != stack.side && enemy.Def.Has(Ability.Fearsome))
                {
                    morale -= 1;
                    break;
                }
            }
            return Math.Max(-3, Math.Min(3, morale));
        }

        /// <summary>The turn of a stack is over: another action for good morale, the end of the battle, or the next stack.</summary>
        private void FinishAction(BattleStack stack, bool allowMorale)
        {
            BattleState battle = Battle;
            battle.actions++;
            stack.acted = true;
            if (CheckBattleEnd())
            {
                return;
            }
            if (allowMorale && stack.alive && !stack.moraleUsed)
            {
                int morale = StackMorale(stack);
                if (morale > 0 && Random.Range(0, 100) < morale * 4)
                {
                    stack.moraleUsed = true;
                    stack.acted = false;
                    Emit(EventKind.MoraleBoost, battle.PlayerOf(stack.side), stack.id);
                    Emit(EventKind.StackTurn, battle.PlayerOf(stack.side), stack.id, stack.cell);
                    return;
                }
            }
            if (battle.actions > 2000)
            {
                EndBattle(BattleResult.AttackerFled, true);
                return;
            }
            AdvanceTurn();
        }

        private bool CheckBattleEnd()
        {
            BattleState battle = Battle;
            if (!battle.active)
            {
                return true;
            }
            if (battle.AliveCount(0) == 0)
            {
                EndBattle(BattleResult.DefenderWon);
                return true;
            }
            if (battle.AliveCount(1) == 0)
            {
                EndBattle(BattleResult.AttackerWon);
                return true;
            }
            return false;
        }

        // ------------------------------------------------------------------ the end

        /// <summary>
        /// The battle is over: what is left of every army goes back to it, <see cref="EventKind.BattleEnded"/> is told
        /// (with a copy of the battle as it ended), and then the map learns what the battle did. A hero beaten in a
        /// fight loses his artifacts to the hero who beat him and leaves the map; a hero who retreats (or whose army
        /// could not get at the enemy, <paramref name="stalled"/>) keeps them: the first leaves the map for a tavern, the
        /// second stays where he stood with the troops he has left. The winner gains the experience of the creatures
        /// killed (and of a town taken), raises the dead with necromancy and takes what the monster guarded.
        /// </summary>
        private void EndBattle(BattleResult result, bool stalled = false)
        {
            BattleState battle = Battle;
            battle.active = false;
            battle.result = result;
            battle.stalled = stalled;
            battle.current = -1;
            HeroState attacker = State.Hero(battle.attackerHero);
            HeroState defender = State.Hero(battle.defenderHero);
            TownState town = State.Town(battle.town);
            MapObject monster = State.Object(battle.monsterObject);
            bool attackerWon = result == BattleResult.AttackerWon || result == BattleResult.DefenderFled;

            // What is left of every army goes back to it.
            WriteBack(battle, 0, 0, attacker != null ? attacker.army : null);
            WriteBack(battle, 1, 0, defender != null ? defender.army : null);
            if (town != null)
            {
                WriteBack(battle, 1, 1, town.garrison);
            }
            if (monster != null)
            {
                // The survivors, and the groups that found no room on the field and never fought.
                int left = monster.amount;
                foreach (BattleStack stack in battle.stacks)
                {
                    if (stack.side == 1 && stack.source == 0 && !stack.IsTower)
                    {
                        left += (stack.alive ? stack.count : 0) - stack.startCount;
                    }
                }
                monster.amount = Math.Max(0, left);
            }

            PlayerState attackerPlayer = State.Player(battle.attackerPlayer);
            PlayerState defenderPlayer = State.Player(battle.defenderPlayer);
            if (attackerWon)
            {
                if (attackerPlayer != null)
                {
                    attackerPlayer.battlesWon++;
                }
            }
            else if (defenderPlayer != null)
            {
                defenderPlayer.battlesWon++;
            }
            if (attackerPlayer != null)
            {
                attackerPlayer.creaturesKilled += Killed(battle.defenderLosses);
            }
            if (defenderPlayer != null)
            {
                defenderPlayer.creaturesKilled += Killed(battle.attackerLosses);
            }
            foreach (HeroState hero in new[] { attacker, defender })
            {
                if (hero != null)
                {
                    hero.luckBonus = 0;
                    hero.moraleBonus = 0;
                }
            }
            string broke = stalled && attacker != null ? $"The armies cannot get at each other: {attacker.Name} breaks off the fight." : null;
            GameEvent ended = Emit(EventKind.BattleEnded, battle.attackerPlayer, (int)result, battle.attackerHero, battle.defenderHero, battle.monsterObject, battle.town, broke);
            ended.battle = battle.Clone();

            if (attackerWon)
            {
                if (defender != null && defender.alive)
                {
                    if (result == BattleResult.AttackerWon)
                    {
                        TakeArtifacts(attacker, defender);
                    }
                    Emit(EventKind.HeroDefeated, defender.owner, defender.id,
                        text: result == BattleResult.DefenderFled ? $"{defender.Name} retreats from the field." : $"{defender.Name} is defeated.");
                    RemoveHero(defender);
                }
                if (monster != null)
                {
                    monster.owner = battle.attackerPlayer;
                    RemoveObject(monster);
                }
                if (town != null)
                {
                    town.garrison.Clear();
                    SetTownOwner(town, battle.attackerPlayer);
                    if (attacker != null && attacker.alive && State.HeroAt(town.cell) == null)
                    {
                        int from = attacker.cell;
                        attacker.cell = town.cell;
                        Emit(EventKind.HeroMoved, attacker.owner, attacker.id, from, town.cell, attacker.movement);
                    }
                }
                attacker?.army.Tidy();
            }
            else
            {
                if (attacker != null && attacker.alive && !stalled)
                {
                    if (result == BattleResult.DefenderWon && defender != null)
                    {
                        TakeArtifacts(defender, attacker);
                    }
                    Emit(EventKind.HeroDefeated, attacker.owner, attacker.id,
                        text: result == BattleResult.AttackerFled ? $"{attacker.Name} retreats from the field." : $"{attacker.Name} is defeated.");
                    RemoveHero(attacker);
                }
                if (monster != null && monster.amount <= 0)
                {
                    RemoveObject(monster);
                }
            }

            HeroState winner = attackerWon ? attacker : defender;
            if (winner != null && winner.alive)
            {
                // Necromancy raises the fallen, not the town: the experience counts both.
                int corpses = attackerWon ? battle.defenderLostHealth : battle.attackerLostHealth;
                RaiseDead(winner, corpses);
                GainExperience(winner, corpses + (town != null && attackerWon ? 500 : 0));
            }
            // A level up waits for its choice, the prize does not: it is taken now, with the next choice after it.
            if (attackerWon && battle.prize >= 0 && attacker != null && attacker.alive)
            {
                MapObject prize = State.Object(battle.prize);
                if (prize != null && !prize.removed)
                {
                    if (MapObjects.IsPickup(prize.kind))
                    {
                        Collect(attacker, prize);
                    }
                    else
                    {
                        Visit(attacker, prize);
                    }
                }
            }
            CheckElimination();
            CheckVictory();
        }

        private static int Killed(List<ArmySlot> losses)
        {
            int total = 0;
            foreach (ArmySlot loss in losses)
            {
                total += loss.count;
            }
            return total;
        }

        private void WriteBack(BattleState battle, int side, int source, Army army)
        {
            if (army == null)
            {
                return;
            }
            foreach (BattleStack stack in battle.stacks)
            {
                if (stack.side != side || stack.source != source || stack.slot < 0 || stack.slot >= army.slots.Length)
                {
                    continue;
                }
                ArmySlot slot = army.slots[stack.slot];
                if (slot.creature != stack.creature)
                {
                    continue;
                }
                slot.count = stack.alive ? stack.count : 0;
            }
            army.Tidy();
        }

        private void TakeArtifacts(HeroState winner, HeroState loser)
        {
            if (winner == null || !winner.alive)
            {
                return;
            }
            var taken = new List<int>();
            foreach (int id in loser.equipped)
            {
                if (id >= 0)
                {
                    taken.Add(id);
                }
            }
            taken.AddRange(loser.backpack);
            for (int i = 0; i < loser.equipped.Length; i++)
            {
                loser.equipped[i] = -1;
            }
            loser.backpack.Clear();
            foreach (int id in taken)
            {
                GiveArtifact(winner, (ArtifactId)id);
            }
        }

        private void RaiseDead(HeroState hero, int enemyHealth)
        {
            int level = hero.SkillLevel(SkillId.Necromancy);
            int percent = level * 10;
            foreach (ArtifactDef artifact in Worn(hero))
            {
                percent += artifact.Necromancy;
            }
            if (percent <= 0 || enemyHealth <= 0)
            {
                return;
            }
            int skeletons = enemyHealth * percent / 100 / Creatures.Get(CreatureId.Skeleton).Health;
            if (skeletons <= 0 || !hero.army.CanAdd((int)CreatureId.Skeleton))
            {
                return;
            }
            hero.army.Add((int)CreatureId.Skeleton, skeletons);
            Emit(EventKind.CreaturesRaised, hero.owner, hero.id, (int)CreatureId.Skeleton, skeletons, text: $"{skeletons} Skeletons rise to join {hero.Name}.");
        }
    }
}
