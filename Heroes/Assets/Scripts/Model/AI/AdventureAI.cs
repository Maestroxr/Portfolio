using System;
using System.Collections.Generic;

namespace Portfolio.Heroes
{
    /// <summary>
    /// Plays the adventure map for a computer player, one command at a time: build and recruit in the towns, hand the
    /// garrison to a visiting hero, hire heroes, and send every hero after the thing most worth its trouble (value per
    /// day of travel), fighting only what it can beat with room to spare. In a scenario the person loses with their town,
    /// it marches on that town as soon as it is strong enough. Ends the turn when nothing is left to do.
    /// </summary>
    public sealed class AdventureAI
    {
        private readonly HashSet<int> done = new HashSet<int>();
        private readonly HashSet<int> gaveUp = new HashSet<int>();
        private int day = -1;
        private int player = -1;
        private int lastHero = -1;
        private int lastTarget = -1;

        /// <summary>A command of the player failed: the hero it was for is finished for the day.</summary>
        public void Refused(GameCommand command)
        {
            if (command != null && command.kind == CommandKind.MoveHero)
            {
                done.Add(command.a);
            }
            else if (command != null && (command.kind == CommandKind.Build || command.kind == CommandKind.Recruit || command.kind == CommandKind.HireHero || command.kind == CommandKind.MoveArmy))
            {
                gaveUp.Add(((int)command.kind << 16) ^ command.a ^ (command.b << 8));
            }
        }

        public GameCommand Next(HeroesGame game, int who)
        {
            GameState state = game.State;
            if (state.day != day || who != player)
            {
                day = state.day;
                player = who;
                done.Clear();
                gaveUp.Clear();
                lastHero = -1;
            }
            PlayerState me = state.Player(who);
            if (me == null || !me.alive)
            {
                return GameCommand.EndTurn(who);
            }
            GameCommand command = TownWork(game, me) ?? HireWork(game, me) ?? HeroWork(game, me);
            return command ?? GameCommand.EndTurn(who);
        }

        private bool Tried(GameCommand command)
        {
            return gaveUp.Contains(((int)command.kind << 16) ^ command.a ^ (command.b << 8));
        }

        // ------------------------------------------------------------------ towns

        private GameCommand TownWork(HeroesGame game, PlayerState me)
        {
            GameState state = game.State;
            var towns = new List<int>(me.towns);
            towns.Sort();
            foreach (int id in towns)
            {
                TownState town = state.Town(id);
                if (!town.builtToday)
                {
                    foreach (BuildingId building in Buildings.AiOrder)
                    {
                        if (game.CannotBuild(me.index, town, building) == null)
                        {
                            BuildingDef def = Buildings.Get(town.faction, building);
                            // Keep some gold for recruits after the first week.
                            if (state.day > 7 && me.resources.Gold - def.Cost.Gold < 1500 && def.DwellingTier == 0)
                            {
                                continue;
                            }
                            GameCommand build = GameCommand.Build(me.index, town.id, building);
                            if (!Tried(build))
                            {
                                return build;
                            }
                        }
                    }
                }
                HeroState visitor = game.Visitor(town);
                bool toHero = visitor != null && visitor.owner == me.index;
                for (int tier = 7; tier >= 1; tier--)
                {
                    if (!town.Has(Buildings.DwellingOf(tier)) || town.available[tier - 1] <= 0)
                    {
                        continue;
                    }
                    CreatureDef def = game.TierCreature(town, tier);
                    int affordable = Math.Min(town.available[tier - 1], me.resources.Times(def.Cost));
                    if (affordable <= 0)
                    {
                        continue;
                    }
                    Army army = toHero ? visitor.army : town.garrison;
                    if (!army.CanAdd((int)def.Id))
                    {
                        continue;
                    }
                    GameCommand recruit = GameCommand.Recruit(me.index, town.id, tier, affordable, toHero);
                    if (!Tried(recruit))
                    {
                        return recruit;
                    }
                }
                if (toHero)
                {
                    // Everything in the garrison marches with the hero.
                    for (int slot = 0; slot < HeroData.ArmySlots; slot++)
                    {
                        ArmySlot source = town.garrison.slots[slot];
                        if (source.IsEmpty)
                        {
                            continue;
                        }
                        int target = -1;
                        for (int i = 0; i < HeroData.ArmySlots; i++)
                        {
                            if (!visitor.army.slots[i].IsEmpty && visitor.army.slots[i].creature == source.creature)
                            {
                                target = i;
                                break;
                            }
                        }
                        if (target < 0)
                        {
                            for (int i = 0; i < HeroData.ArmySlots; i++)
                            {
                                if (visitor.army.slots[i].IsEmpty)
                                {
                                    target = i;
                                    break;
                                }
                            }
                        }
                        if (target >= 0)
                        {
                            GameCommand move = GameCommand.MoveArmy(me.index, GameCommand.Garrison(town.id), slot, visitor.id, target, 0);
                            if (!Tried(move))
                            {
                                return move;
                            }
                        }
                    }
                }
            }
            return null;
        }

        private GameCommand HireWork(HeroesGame game, PlayerState me)
        {
            GameState state = game.State;
            int limit = me.aiLevel == 0 ? 2 : me.aiLevel == 2 ? 4 : 3;
            limit = Math.Min(limit, 1 + state.Week);
            if (game.CountHeroes(me.index) >= limit || me.resources.Gold < HeroData.HireCost + 1500)
            {
                return null;
            }
            foreach (int id in me.towns)
            {
                TownState town = state.Town(id);
                if (!town.Has(BuildingId.Tavern) || game.Visitor(town) != null)
                {
                    continue;
                }
                for (int slot = 0; slot < 2; slot++)
                {
                    if (me.tavern[slot] >= 0)
                    {
                        GameCommand hire = GameCommand.Hire(me.index, town.id, slot);
                        if (!Tried(hire))
                        {
                            return hire;
                        }
                    }
                }
            }
            return null;
        }

        // ------------------------------------------------------------------ heroes

        private GameCommand HeroWork(HeroesGame game, PlayerState me)
        {
            GameState state = game.State;
            var heroes = new List<int>(me.heroes);
            heroes.Sort();
            foreach (int id in heroes)
            {
                HeroState hero = state.Hero(id);
                if (hero == null || !hero.alive || hero.movement <= 0 || done.Contains(id))
                {
                    continue;
                }
                int target = ChooseTarget(game, me, hero);
                if (target < 0)
                {
                    done.Add(id);
                    continue;
                }
                if (id == lastHero && target == lastTarget && hero.movement < 100)
                {
                    done.Add(id);
                    continue;
                }
                lastHero = id;
                lastTarget = target;
                return GameCommand.Move(me.index, id, target);
            }
            return null;
        }

        private int ChooseTarget(HeroesGame game, PlayerState me, HeroState hero)
        {
            GameState state = game.State;
            int daily = Math.Max(1000, hero.maxMovement);
            int[] cost = game.Reachable(hero, daily * 3);
            int strength = game.Strength(hero);
            int best = -1;
            long bestScore = 0;

            void Consider(int cell, long value)
            {
                if (value <= 0 || cell < 0 || cost[cell] == int.MaxValue)
                {
                    return;
                }
                long days10 = 10 + cost[cell] * 10L / daily;
                long score = value * 100 / days10;
                if (score > bestScore || (score == bestScore && cell < best))
                {
                    bestScore = score;
                    best = cell;
                }
            }

            foreach (MapObject obj in state.objects)
            {
                if (obj.removed || cost[obj.cell] == int.MaxValue)
                {
                    continue;
                }
                long value = ObjectValue(game, me, hero, obj);
                MapObject guard = game.GuardianOf(obj);
                if (guard != null && obj.owner != me.index)
                {
                    if (!CanBeat(game, strength, Creatures.Get(guard.subtype).Value * (long)guard.amount, me.aiLevel))
                    {
                        continue;
                    }
                    value += Creatures.Get(guard.subtype).Value * (long)guard.amount / 5;
                }
                Consider(obj.cell, value);
            }
            foreach (HeroState other in state.heroes)
            {
                if (!other.alive || other.owner == me.index || game.SameTeam(other.owner, me.index) || cost[other.cell] == int.MaxValue)
                {
                    continue;
                }
                if (CanBeat(game, strength, game.Strength(other), me.aiLevel))
                {
                    Consider(other.cell, game.Strength(other) + 3000);
                }
            }
            foreach (TownState town in state.towns)
            {
                if (cost[town.cell] == int.MaxValue)
                {
                    continue;
                }
                if (town.owner == me.index)
                {
                    // Home to pick up recruits and the garrison, or to defend it.
                    long waiting = 0;
                    for (int tier = 1; tier <= 7; tier++)
                    {
                        if (town.Has(Buildings.DwellingOf(tier)))
                        {
                            waiting += (long)town.available[tier - 1] * game.TierCreature(town, tier).Value;
                        }
                    }
                    waiting += game.Strength(town.garrison);
                    if (game.Visitor(town) == null && waiting > strength / 3)
                    {
                        Consider(town.cell, waiting / 2);
                    }
                    continue;
                }
                if (game.SameTeam(town.owner, me.index))
                {
                    continue;
                }
                long defense = game.Strength(town.garrison) + Buildings.Towers(town) * 1500L;
                HeroState visitor = game.Visitor(town);
                if (visitor != null)
                {
                    defense += game.Strength(visitor);
                }
                if (CanBeat(game, strength, defense, me.aiLevel))
                {
                    Consider(town.cell, 25000 + defense / 2);
                }
            }
            MarchOnTheKeptTown(game, me, hero, strength, daily, ref best, ref bestScore);
            if (best >= 0)
            {
                MovePlan plan = game.PlanPath(hero, best);
                if (plan == null || plan.Today == 0)
                {
                    return -1;
                }
                // A monster guarding the way: fight it only when it can be beaten.
                if (plan.End == PathEnd.Battle && plan.Destination != best)
                {
                    MapObject guard = game.GuardOf(plan.Destination);
                    if (guard != null && !CanBeat(game, strength, Creatures.Get(guard.subtype).Value * (long)guard.amount, me.aiLevel))
                    {
                        return Explore(game, hero, cost, true);
                    }
                }
                return best;
            }
            return Explore(game, hero, cost, false);
        }

        /// <summary>
        /// A scenario the person loses with their first town (<see cref="LossKind.LoseTown"/>): once the first week is
        /// over (they gather their armies in it) the computer players against them march on that town from anywhere on
        /// the map, fighting their way through what bars it, and storm it as soon as a hero of theirs is a little stronger
        /// than what defends it, bolder than they are with any other town. Without such a scenario (online, a skirmish)
        /// nothing changes.
        /// </summary>
        private static void MarchOnTheKeptTown(HeroesGame game, PlayerState me, HeroState hero, int strength, int daily, ref int best, ref long bestScore)
        {
            GameState state = game.State;
            TownState town = state.rules.loss == LossKind.LoseTown ? state.Town(state.rules.lossValue) : null;
            PlayerState holder = town != null ? state.Player(town.owner) : null;
            if (holder == null || !holder.human || game.SameTeam(town.owner, me.index) || state.Week < 2)
            {
                return;
            }
            long defense = game.Strength(town.garrison) + Buildings.Towers(town) * 1500L;
            HeroState keeper = game.Visitor(town);
            if (keeper != null)
            {
                defense += game.Strength(keeper);
            }
            if (strength * 100L <= defense * 115L)
            {
                return;
            }
            int goal = town.cell;
            MovePlan march = game.PlanPath(hero, goal);
            if (march == null)
            {
                // Wandering armies bar the way (a guarded pass): the first of them on it is fought first, if it can be,
                // by walking up to it.
                MapObject guard = FirstGuardOnTheWay(game, hero.cell, town.cell, out goal);
                if (guard == null || !CanBeat(game, strength, Creatures.Get(guard.subtype).Value * (long)guard.amount, me.aiLevel))
                {
                    return;
                }
                march = game.PlanPath(hero, goal);
            }
            if (march == null || march.Cells.Count == 0 || march.Today == 0)
            {
                return;
            }
            long days10 = 10 + march.Cost[march.Cost.Count - 1] * 10L / daily;
            long score = (40000 + defense / 2) * 100 / days10;
            if (score > bestScore)
            {
                bestScore = score;
                best = goal;
            }
        }

        /// <summary>
        /// The wandering army that stands first on the shortest way over open land from <paramref name="from"/> to
        /// <paramref name="to"/> (on it, or guarding a cell of it), with the cell of the way where it attacks
        /// (<paramref name="fightAt"/>); null when nothing does or there is no way at all.
        /// </summary>
        private static MapObject FirstGuardOnTheWay(HeroesGame game, int from, int to, out int fightAt)
        {
            fightAt = -1;
            HexGrid grid = game.Grid;
            var came = new int[grid.Count];
            for (int i = 0; i < came.Length; i++)
            {
                came[i] = -2;
            }
            var queue = new Queue<int>();
            queue.Enqueue(from);
            came[from] = -1;
            var around = new List<int>(6);
            while (queue.Count > 0 && came[to] == -2)
            {
                int cell = queue.Dequeue();
                grid.Neighbors(cell, around);
                foreach (int next in around)
                {
                    if (came[next] != -2 || !game.Map.Open(next))
                    {
                        continue;
                    }
                    MapObject there = game.State.ObjectAt(next);
                    if (there != null && there.kind != ObjectKind.Monster && next != to)
                    {
                        continue;
                    }
                    came[next] = cell;
                    queue.Enqueue(next);
                }
            }
            if (came[to] == -2)
            {
                return null;
            }
            var way = new List<int>();
            for (int cell = to; cell != from; cell = came[cell])
            {
                way.Add(cell);
            }
            for (int i = way.Count - 1; i >= 0; i--)
            {
                MapObject there = game.State.ObjectAt(way[i]);
                MapObject guard = there != null && there.kind == ObjectKind.Monster ? there : game.GuardOf(way[i]);
                if (guard != null)
                {
                    fightAt = way[i];
                    return guard;
                }
            }
            return null;
        }

        private static bool CanBeat(HeroesGame game, long mine, long theirs, int level)
        {
            // An easy computer is careful, a hard one bold; all of them grow bolder as the weeks go by.
            int margin = level == 0 ? 180 : level == 2 ? 125 : 150;
            margin = Math.Max(110, margin - 6 * Math.Max(0, game.State.Week - 2));
            return mine * 100 > theirs * margin;
        }

        private long ObjectValue(HeroesGame game, PlayerState me, HeroState hero, MapObject obj)
        {
            GameState state = game.State;
            switch (obj.kind)
            {
                case ObjectKind.Resource:
                    return (long)obj.amount * Land.Value((ResourceKind)obj.subtype);
                case ObjectKind.Campfire:
                    return obj.amount + (long)obj.amount2 * Land.Value((ResourceKind)obj.subtype);
                case ObjectKind.Treasure:
                    return obj.subtype >= 0 ? Artifacts.Get((ArtifactId)obj.subtype).Value : obj.amount;
                case ObjectKind.Artifact:
                    return Artifacts.Get((ArtifactId)obj.subtype).Value;
                case ObjectKind.Scroll:
                    return hero.Knows((SpellId)obj.subtype) ? 0 : 900;
                case ObjectKind.Mine:
                    return obj.owner == me.index ? 0 : MapObjects.MineYield((ResourceKind)obj.subtype) * (long)Land.Value((ResourceKind)obj.subtype) * 12;
                case ObjectKind.Dwelling:
                {
                    CreatureDef def = Creatures.Get(obj.subtype);
                    int affordable = Math.Min(obj.amount, me.resources.Times(def.Cost));
                    return hero.army.CanAdd(obj.subtype) ? (long)affordable * def.Value / 2 : 0;
                }
                case ObjectKind.Monster:
                {
                    long value = Creatures.Get(obj.subtype).Value * (long)obj.amount;
                    return CanBeat(game, game.Strength(hero), value, me.aiLevel) ? value / 3 + 500 : 0;
                }
                case ObjectKind.Windmill:
                case ObjectKind.WaterWheel:
                    return obj.visitedBy.Count > 0 ? 0 : 900;
                case ObjectKind.Town:
                    return 0;
                default:
                    if (MapObjects.OncePerHero(obj.kind))
                    {
                        if (obj.visitedBy.Contains(hero.id))
                        {
                            return 0;
                        }
                        if (obj.kind == ObjectKind.Shrine)
                        {
                            SpellDef spell = Spells.Get((SpellId)obj.amount);
                            return spell != null && !hero.Knows(spell.Id) && spell.Level <= game.SpellLevelLimit(hero) ? 1200 : 0;
                        }
                        if (obj.kind == ObjectKind.WitchHut)
                        {
                            return hero.SkillLevel((SkillId)obj.subtype) == 0 && hero.skills.Count < HeroData.MaxSkills ? 1000 : 0;
                        }
                        return obj.kind == ObjectKind.TreeOfKnowledge ? 2500 : 1500;
                    }
                    if (obj.kind == ObjectKind.Watchtower)
                    {
                        return obj.visitedBy.Contains(me.index) ? 0 : 200;
                    }
                    if (obj.kind == ObjectKind.Stables)
                    {
                        return hero.stablesWeek == state.Week ? 0 : 400;
                    }
                    return hero.luckBonus + hero.moraleBonus > 0 ? 0 : 250;
            }
        }

        /// <summary>No goal in sight: walk toward the nearest unexplored land.</summary>
        private int Explore(HeroesGame game, HeroState hero, int[] cost, bool avoidFights)
        {
            GameState state = game.State;
            PlayerState me = state.Player(hero.owner);
            int best = -1;
            long bestScore = long.MinValue;
            for (int cell = 0; cell < cost.Length; cell++)
            {
                if (cost[cell] == int.MaxValue || cell == hero.cell || !game.Map.Open(cell) || state.ObjectAt(cell) != null || state.HeroAt(cell) != null)
                {
                    continue;
                }
                if (avoidFights && game.GuardOf(cell) != null)
                {
                    continue;
                }
                int unexplored = 0;
                foreach (int near in game.Grid.Disk(cell, 3))
                {
                    if (!me.Explored(near))
                    {
                        unexplored++;
                    }
                }
                if (unexplored == 0)
                {
                    continue;
                }
                long score = unexplored * 1000L - cost[cell];
                if (score > bestScore)
                {
                    bestScore = score;
                    best = cell;
                }
            }
            return best;
        }
    }
}
