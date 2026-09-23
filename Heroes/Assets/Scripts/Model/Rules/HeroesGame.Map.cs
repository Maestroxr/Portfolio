using System;
using System.Collections.Generic;

namespace Portfolio.Heroes
{
    public sealed partial class HeroesGame
    {
        /// <summary>
        /// Walks a hero of the current player toward <paramref name="target"/> as far as its movement goes today. The walk
        /// stops early where something happens: a monster guarding a cell attacks, a treasure asks a question, a level
        /// is gained. Reaching the target visits it (a building, the player's own town), picks it up or starts a battle.
        /// </summary>
        private bool MoveHero(int player, int heroId, int target)
        {
            HeroState hero = State.Hero(heroId);
            if (hero == null || !hero.alive || hero.owner != player)
            {
                return false;
            }
            MovePlan plan = PlanPath(hero, target);
            if (plan == null || plan.Today == 0)
            {
                return false;
            }
            hero.sleeping = false;
            int steps = Math.Min(plan.Today, plan.Cells.Count);
            int spent = 0;
            for (int i = 0; i < steps; i++)
            {
                int next = plan.Cells[i];
                int from = hero.cell;
                bool last = i == plan.Cells.Count - 1;
                spent = plan.Cost[i];
                if (last && !plan.EntersLast)
                {
                    // Buildings, monsters, other heroes and foreign towns are dealt with from the cell before them.
                    hero.movement = Math.Max(0, hero.movement - (spent - (i > 0 ? plan.Cost[i - 1] : 0)));
                    hero.facing = (int)Grid.SideTowards(from, next);
                    Emit(EventKind.HeroStopped, player, hero.id, hero.cell);
                    Interact(hero, next, plan.End);
                    return true;
                }
                hero.movement = Math.Max(0, hero.movement - (plan.Cost[i] - (i > 0 ? plan.Cost[i - 1] : 0)));
                hero.facing = (int)Grid.SideTowards(from, next);
                hero.cell = next;
                Emit(EventKind.HeroMoved, player, hero.id, from, next, hero.movement);
                Reveal(player, next, Sight(hero));
                if (Arrived(hero, next))
                {
                    break;
                }
            }
            Emit(EventKind.HeroStopped, player, hero.id, hero.cell);
            CheckGoalsNow();
            return true;
        }

        /// <summary>Whatever happens on the cell a hero just stepped on. Returns true when the walk has to stop.</summary>
        private bool Arrived(HeroState hero, int cell)
        {
            MapObject pickup = State.ObjectAt(cell);
            if (pickup != null && !MapObjects.IsPickup(pickup.kind))
            {
                pickup = null;
            }
            MapObject guard = (pickup != null ? GuardianOf(pickup) : null) ?? GuardOf(cell);
            if (guard != null)
            {
                // The monster attacks; a treasure it guards is taken once it is beaten.
                StartBattle(hero, guard.cell, pickup != null ? pickup.id : -1);
                return true;
            }
            if (pickup != null)
            {
                Collect(hero, pickup);
                return true;
            }
            TownState town = TownAtGate(cell);
            if (town != null && (town.owner == hero.owner || SameTeam(town.owner, hero.owner)))
            {
                EnterTown(hero, town);
                return true;
            }
            return false;
        }

        private void Interact(HeroState hero, int cell, PathEnd end)
        {
            switch (end)
            {
                case PathEnd.Battle:
                case PathEnd.Visit:
                case PathEnd.Meet:
                    break;
                default:
                    return;
            }
            TownState town = TownAtGate(cell);
            HeroState other = State.HeroAt(cell);
            if (town != null && town.owner != hero.owner && !SameTeam(town.owner, hero.owner))
            {
                StartBattle(hero, cell, -1);
                return;
            }
            if (other != null)
            {
                if (other.owner != hero.owner && !SameTeam(other.owner, hero.owner))
                {
                    StartBattle(hero, cell, -1);
                }
                else
                {
                    Emit(EventKind.ObjectVisited, hero.owner, hero.id, -1, other.id, text: $"{hero.Name} meets {other.Name}.");
                }
                return;
            }
            MapObject obj = State.ObjectAt(cell);
            if (obj == null)
            {
                return;
            }
            if (obj.kind == ObjectKind.Monster)
            {
                StartBattle(hero, cell, -1);
                return;
            }
            MapObject guardian = GuardianOf(obj);
            if (guardian != null && obj.owner != hero.owner)
            {
                // Whoever wants the mine (or the shrine) has to get past its guards first.
                StartBattle(hero, guardian.cell, obj.id);
                return;
            }
            Visit(hero, obj);
        }

        /// <summary>The live monster set to guard <paramref name="obj"/>, or null.</summary>
        public MapObject GuardianOf(MapObject obj)
        {
            MapObject guard = State.Object(obj.guard);
            return guard != null && !guard.removed && guard.kind == ObjectKind.Monster ? guard : null;
        }

        private void EnterTown(HeroState hero, TownState town)
        {
            Emit(EventKind.ObjectVisited, hero.owner, hero.id, town.objectId, -1, 1, text: $"{hero.Name} enters {town.name}.");
            LearnGuildSpells(hero, town);
            if (Buildings.MageGuildLevel(town) > 0)
            {
                hero.mana = Math.Max(hero.mana, MaxMana(hero));
            }
        }

        // ------------------------------------------------------------------ pickups

        private void Collect(HeroState hero, MapObject obj)
        {
            PlayerState player = State.Player(hero.owner);
            switch (obj.kind)
            {
                case ObjectKind.Resource:
                    player.resources[(ResourceKind)obj.subtype] += obj.amount;
                    Emit(EventKind.ResourcesGained, player.index, obj.cell, obj.subtype, obj.amount, text: $"{obj.amount} {Land.ResourceName((ResourceKind)obj.subtype)}");
                    break;
                case ObjectKind.Campfire:
                    player.resources[ResourceKind.Gold] += obj.amount;
                    player.resources[(ResourceKind)obj.subtype] += obj.amount2;
                    Emit(EventKind.ResourcesGained, player.index, obj.cell, (int)ResourceKind.Gold, obj.amount,
                        text: $"{obj.amount} Gold and {obj.amount2} {Land.ResourceName((ResourceKind)obj.subtype)}");
                    break;
                case ObjectKind.Treasure:
                    if (obj.subtype >= 0)
                    {
                        // A chest with an artifact inside.
                        GiveArtifact(hero, (ArtifactId)obj.subtype);
                    }
                    else if (!player.human)
                    {
                        // The computer takes the gold early and the experience late.
                        bool gold = State.day < 21 || player.resources.Gold < 5000;
                        GiveTreasure(hero, gold ? obj.amount : obj.amount2, gold);
                    }
                    else
                    {
                        State.pending.Add(new PendingChoice { kind = ChoiceKind.Treasure, player = player.index, hero = hero.id, options = new[] { obj.amount, obj.amount2 } });
                        Emit(EventKind.ChoiceNeeded, player.index, hero.id, (int)ChoiceKind.Treasure);
                    }
                    break;
                case ObjectKind.Artifact:
                    GiveArtifact(hero, (ArtifactId)obj.subtype);
                    break;
                case ObjectKind.Scroll:
                    if (!hero.Knows((SpellId)obj.subtype))
                    {
                        hero.spells.Add(obj.subtype);
                        Emit(EventKind.SpellLearned, player.index, hero.id, obj.subtype, text: $"{hero.Name} learns {Spells.Get((SpellId)obj.subtype).Name}.");
                    }
                    break;
            }
            RemoveObject(obj);
            CheckGoalsNow();
        }

        private void GiveTreasure(HeroState hero, int amount, bool gold)
        {
            if (gold)
            {
                State.Player(hero.owner).resources[ResourceKind.Gold] += amount;
                Emit(EventKind.ResourcesGained, hero.owner, hero.cell, (int)ResourceKind.Gold, amount, text: $"{amount} Gold");
            }
            else
            {
                GainExperience(hero, amount);
            }
        }

        public void RemoveObject(MapObject obj)
        {
            obj.removed = true;
            foreach (int cell in obj.footprint)
            {
                if (Map.occupant[cell] == obj.id)
                {
                    Map.occupant[cell] = -1;
                }
            }
            Emit(EventKind.ObjectRemoved, -1, obj.id, obj.cell);
        }

        // ------------------------------------------------------------------ buildings

        private void Visit(HeroState hero, MapObject obj)
        {
            PlayerState player = State.Player(hero.owner);
            string name = MapObjects.Name(obj.kind);
            if (MapObjects.OncePerHero(obj.kind) && obj.visitedBy.Contains(hero.id))
            {
                Emit(EventKind.ObjectVisited, player.index, hero.id, obj.id, 0, text: $"{hero.Name} has already visited the {name}.");
                return;
            }
            bool once = MapObjects.OncePerHero(obj.kind);
            switch (obj.kind)
            {
                case ObjectKind.Mine:
                    if (obj.owner != player.index)
                    {
                        obj.owner = player.index;
                        Emit(EventKind.ObjectCaptured, player.index, obj.id, player.index, text: $"The {MapObjects.MineName((ResourceKind)obj.subtype)} now flies your flag.");
                    }
                    return;
                case ObjectKind.Dwelling:
                    obj.owner = player.index;
                    Emit(EventKind.ObjectVisited, player.index, hero.id, obj.id, 1, text: $"{Creatures.Get(obj.subtype).Plural} for hire: {obj.amount}.");
                    return;
                case ObjectKind.Shrine:
                {
                    var spell = (SpellId)obj.amount;
                    SpellDef def = Spells.Get(spell);
                    if (def == null || hero.Knows(spell))
                    {
                        Emit(EventKind.ObjectVisited, player.index, hero.id, obj.id, 0, text: $"{hero.Name} already knows {def?.Name}.");
                        return;
                    }
                    if (def.Level > SpellLevelLimit(hero))
                    {
                        Emit(EventKind.ObjectVisited, player.index, hero.id, obj.id, 0, text: $"{hero.Name} lacks the wisdom to learn {def.Name}.");
                        return;
                    }
                    hero.spells.Add((int)spell);
                    Emit(EventKind.SpellLearned, player.index, hero.id, (int)spell, text: $"{hero.Name} learns {def.Name}.");
                    break;
                }
                case ObjectKind.MercenaryCamp:
                    RaiseStat(hero, PrimaryStat.Attack, 1);
                    break;
                case ObjectKind.MarlettoTower:
                    RaiseStat(hero, PrimaryStat.Defense, 1);
                    break;
                case ObjectKind.StarAxis:
                    RaiseStat(hero, PrimaryStat.Power, 1);
                    break;
                case ObjectKind.GardenOfRevelation:
                    RaiseStat(hero, PrimaryStat.Knowledge, 1);
                    break;
                case ObjectKind.Arena:
                    if (player.human)
                    {
                        State.pending.Add(new PendingChoice { kind = ChoiceKind.Arena, player = player.index, hero = hero.id, options = new[] { 0, 1 } });
                        Emit(EventKind.ChoiceNeeded, player.index, hero.id, (int)ChoiceKind.Arena);
                    }
                    else
                    {
                        RaiseStat(hero, hero.Def != null && HeroData.Class(hero.Def.Class).Growth[0] >= HeroData.Class(hero.Def.Class).Growth[1] ? PrimaryStat.Attack : PrimaryStat.Defense, 2);
                    }
                    break;
                case ObjectKind.LearningStone:
                    GainExperience(hero, 1000);
                    break;
                case ObjectKind.TreeOfKnowledge:
                    GainExperience(hero, Math.Max(0, HeroData.ExperienceFor(hero.level + 1) - hero.experience));
                    break;
                case ObjectKind.WitchHut:
                {
                    var skill = (SkillId)obj.subtype;
                    if (hero.SkillLevel(skill) > 0 || hero.skills.Count >= HeroData.MaxSkills || (skill == SkillId.Necromancy && hero.Def.Faction != Faction.Necropolis))
                    {
                        Emit(EventKind.ObjectVisited, player.index, hero.id, obj.id, 0, text: $"The witch has nothing to teach {hero.Name}.");
                        return;
                    }
                    hero.skills.Add(new SkillEntry { skill = (int)skill, level = 1 });
                    Emit(EventKind.ObjectVisited, player.index, hero.id, obj.id, 1, text: $"{hero.Name} learns Basic {HeroData.Skill(skill).Name}.");
                    break;
                }
                case ObjectKind.Windmill:
                case ObjectKind.WaterWheel:
                    if (obj.visitedBy.Count > 0)
                    {
                        Emit(EventKind.ObjectVisited, player.index, hero.id, obj.id, 0, text: $"The {name} is empty until next week.");
                        return;
                    }
                    if (obj.kind == ObjectKind.Windmill)
                    {
                        var kind = (ResourceKind)Random.Range(1, 7);
                        int amount = kind == ResourceKind.Wood || kind == ResourceKind.Ore ? Random.Range(5, 11) : Random.Range(3, 7);
                        player.resources[kind] += amount;
                        Emit(EventKind.ResourcesGained, player.index, obj.cell, (int)kind, amount, text: $"{amount} {Land.ResourceName(kind)}");
                    }
                    else
                    {
                        int gold = State.Week == 1 ? 500 : 1000;
                        player.resources[ResourceKind.Gold] += gold;
                        Emit(EventKind.ResourcesGained, player.index, obj.cell, (int)ResourceKind.Gold, gold, text: $"{gold} Gold");
                    }
                    obj.visitedBy.Add(player.index);
                    return;
                case ObjectKind.Watchtower:
                    Reveal(player.index, obj.cell, 12);
                    Emit(EventKind.ObjectVisited, player.index, hero.id, obj.id, 1, text: "From the tower the land lies open.");
                    return;
                case ObjectKind.Stables:
                    if (hero.stablesWeek != State.Week)
                    {
                        hero.stablesWeek = State.Week;
                        hero.movement += 400;
                        hero.maxMovement += 400;
                    }
                    Emit(EventKind.ObjectVisited, player.index, hero.id, obj.id, 1, text: "Fresh horses: +400 movement this week.");
                    return;
                case ObjectKind.Fountain:
                    hero.luckBonus = Math.Max(hero.luckBonus, 1);
                    Emit(EventKind.ObjectVisited, player.index, hero.id, obj.id, 1, text: "+1 Luck until the next battle.");
                    return;
                case ObjectKind.Temple:
                    hero.moraleBonus = Math.Max(hero.moraleBonus, 1);
                    Emit(EventKind.ObjectVisited, player.index, hero.id, obj.id, 1, text: "+1 Morale until the next battle.");
                    return;
            }
            if (once)
            {
                obj.visitedBy.Add(hero.id);
                if (!hero.visited.Contains(obj.id))
                {
                    hero.visited.Add(obj.id);
                }
            }
        }

        private void RaiseStat(HeroState hero, PrimaryStat stat, int amount)
        {
            switch (stat)
            {
                case PrimaryStat.Attack: hero.attack += amount; break;
                case PrimaryStat.Defense: hero.defense += amount; break;
                case PrimaryStat.Power: hero.power += amount; break;
                default: hero.knowledge += amount; break;
            }
            Emit(EventKind.StatRaised, hero.owner, hero.id, (int)stat, amount, text: $"{hero.Name}: +{amount} {HeroData.StatName(stat)}");
        }

        private bool Sleep(int player, int heroId, bool sleep)
        {
            HeroState hero = State.Hero(heroId);
            if (hero == null || !hero.alive || hero.owner != player)
            {
                return false;
            }
            hero.sleeping = sleep;
            return true;
        }
    }
}
