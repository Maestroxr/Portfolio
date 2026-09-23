using System;
using System.Collections.Generic;

namespace Portfolio.Heroes
{
    public sealed partial class HeroesGame
    {
        /// <summary>Why a building cannot be built now, or null when it can.</summary>
        public string CannotBuild(int player, TownState town, BuildingId building)
        {
            BuildingDef def = Buildings.Get(town.faction, building);
            if (def == null)
            {
                return "There is no such building.";
            }
            if (town.owner != player)
            {
                return "Not your town.";
            }
            if (town.Has(building))
            {
                return "Already built.";
            }
            if (town.builtToday)
            {
                return "One building a day.";
            }
            foreach (BuildingId required in def.Requires)
            {
                if (!town.Has(required))
                {
                    return $"Requires {Buildings.Get(town.faction, required).Name}.";
                }
            }
            if (!State.Player(player).resources.CanAfford(def.Cost))
            {
                return "Not enough resources.";
            }
            return null;
        }

        private bool Build(int player, int townId, BuildingId building)
        {
            TownState town = State.Town(townId);
            if (town == null || CannotBuild(player, town, building) != null)
            {
                return false;
            }
            BuildingDef def = Buildings.Get(town.faction, building);
            State.Player(player).resources.Subtract(def.Cost);
            town.built.Add((int)building);
            town.builtToday = true;
            int tier = def.DwellingTier;
            if (tier > 0)
            {
                // A new dwelling opens with its first week of creatures.
                CreatureDef creature = Creatures.Get(Creatures.OfTier(town.faction, tier));
                town.available[tier - 1] += creature.Growth;
            }
            if (building == BuildingId.MageGuild1 || building == BuildingId.MageGuild2 || building == BuildingId.MageGuild3)
            {
                AddGuildSpells(town, building - BuildingId.MageGuild1 + 1);
                HeroState visitor = State.HeroAt(town.cell);
                if (visitor != null && visitor.owner == player)
                {
                    LearnGuildSpells(visitor, town);
                }
            }
            Emit(EventKind.TownBuilt, player, town.id, (int)building, text: $"{def.Name} built in {town.name}.");
            return true;
        }

        private void AddGuildSpells(TownState town, int level)
        {
            List<SpellId> pool = Spells.OfLevel(level);
            // The deterministic pick of the town: its id and the level decide, not the dice of the moment.
            var picker = new SeededRandom((uint)(State.seed * 31 + town.id * 7919 + level * 104729));
            int count = Math.Min(2, pool.Count);
            for (int i = 0; i < count; i++)
            {
                int index = picker.Range(0, pool.Count);
                town.guildSpells.Add((int)pool[index]);
                pool.RemoveAt(index);
            }
        }

        private void LearnGuildSpells(HeroState hero, TownState town)
        {
            if (town.owner != hero.owner)
            {
                return;
            }
            foreach (int id in town.guildSpells)
            {
                SpellDef def = Spells.Get((SpellId)id);
                if (def != null && !hero.Knows((SpellId)id) && def.Level <= SpellLevelLimit(hero))
                {
                    hero.spells.Add(id);
                    Emit(EventKind.SpellLearned, hero.owner, hero.id, id, text: $"{hero.Name} learns {def.Name}.");
                }
            }
        }

        private void LearnGuildSpellsInTowns(int player)
        {
            PlayerState state = State.Player(player);
            if (state == null)
            {
                return;
            }
            foreach (int id in state.towns)
            {
                TownState town = State.Town(id);
                HeroState visitor = State.HeroAt(town.cell);
                if (visitor != null && visitor.owner == player)
                {
                    LearnGuildSpells(visitor, town);
                }
            }
        }

        /// <summary>The hero standing in the town's gate, or null.</summary>
        public HeroState Visitor(TownState town)
        {
            return State.HeroAt(town.cell);
        }

        public CreatureDef TierCreature(TownState town, int tier)
        {
            return Creatures.Get(Creatures.OfTier(town.faction, tier));
        }

        private bool Recruit(int player, int townId, int tier, int count, bool toHero)
        {
            TownState town = State.Town(townId);
            if (town == null || town.owner != player || tier < 1 || tier > 7 || count <= 0 || !town.Has(Buildings.DwellingOf(tier)))
            {
                return false;
            }
            if (count > town.available[tier - 1])
            {
                return false;
            }
            CreatureDef def = TierCreature(town, tier);
            PlayerState owner = State.Player(player);
            if (!owner.resources.CanAfford(def.Cost, count))
            {
                return false;
            }
            Army army = town.garrison;
            HeroState visitor = Visitor(town);
            if (toHero)
            {
                if (visitor == null || visitor.owner != player)
                {
                    return false;
                }
                army = visitor.army;
            }
            if (!army.Add((int)def.Id, count))
            {
                return false;
            }
            owner.resources.Subtract(def.Cost, count);
            town.available[tier - 1] -= count;
            Emit(EventKind.CreaturesRecruited, player, town.objectId, (int)def.Id, count, toHero ? visitor.id : -1);
            Emit(EventKind.ArmyChanged, player, toHero ? visitor.id : GameCommand.Garrison(town.id));
            return true;
        }

        private bool RecruitAtDwelling(int player, int objectId, int heroId, int count)
        {
            MapObject obj = State.Object(objectId);
            HeroState hero = State.Hero(heroId);
            if (obj == null || obj.removed || obj.kind != ObjectKind.Dwelling || hero == null || !hero.alive || hero.owner != player)
            {
                return false;
            }
            if (Grid.Distance(hero.cell, obj.cell) > 1 || count <= 0 || count > obj.amount)
            {
                return false;
            }
            CreatureDef def = Creatures.Get(obj.subtype);
            PlayerState owner = State.Player(player);
            if (!owner.resources.CanAfford(def.Cost, count) || !hero.army.Add(obj.subtype, count))
            {
                return false;
            }
            owner.resources.Subtract(def.Cost, count);
            obj.amount -= count;
            Emit(EventKind.CreaturesRecruited, player, obj.id, obj.subtype, count, hero.id);
            Emit(EventKind.ArmyChanged, player, hero.id);
            return true;
        }

        private bool Hire(int player, int townId, int slot)
        {
            TownState town = State.Town(townId);
            PlayerState owner = State.Player(player);
            if (town == null || town.owner != player || !town.Has(BuildingId.Tavern) || slot < 0 || slot > 1)
            {
                return false;
            }
            int def = owner.tavern[slot];
            if (def < 0 || !State.freeHeroes.Contains(def) || State.HeroAt(town.cell) != null || !owner.resources.CanAfford(new ResourceSet(HeroData.HireCost)))
            {
                return false;
            }
            if (CountHeroes(player) >= 8)
            {
                return false;
            }
            owner.resources[ResourceKind.Gold] -= HeroData.HireCost;
            HeroState hero = SpawnHero(player, def, town.cell, true);
            owner.tavern[slot] = -1;
            RefreshTavern(owner);
            Emit(EventKind.HeroHired, player, hero.id, town.cell, text: $"{hero.Name} joins your cause.");
            LearnGuildSpells(hero, town);
            return true;
        }

        public int CountHeroes(int player)
        {
            int count = 0;
            foreach (int id in State.Player(player).heroes)
            {
                HeroState hero = State.Hero(id);
                if (hero != null && hero.alive)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Puts a hero of the roster on the map for <paramref name="player"/>: a fresh one with the starting army of the
        /// class, or one who fled a battle before, as they were but without an army (then with a few recruits).
        /// </summary>
        public HeroState SpawnHero(int player, int def, int cell, bool starterArmy)
        {
            State.freeHeroes.Remove(def);
            HeroState hero = null;
            foreach (HeroState existing in State.heroes)
            {
                if (existing.def == def)
                {
                    hero = existing;
                    break;
                }
            }
            if (hero == null)
            {
                hero = CreateHero(def);
            }
            hero.owner = player;
            hero.cell = cell;
            hero.alive = true;
            hero.sleeping = false;
            hero.army.Clear();
            if (starterArmy)
            {
                Faction faction = hero.Def.Faction;
                hero.army.Add((int)Creatures.OfTier(faction, 1), 10 + Random.Range(0, 11));
                hero.army.Add((int)Creatures.OfTier(faction, 2), 4 + Random.Range(0, 4));
                if (Random.Range(0, 2) == 0)
                {
                    hero.army.Add((int)Creatures.OfTier(faction, 3), 1 + Random.Range(0, 3));
                }
            }
            hero.maxMovement = DailyMovement(hero);
            hero.movement = hero.maxMovement;
            hero.mana = MaxMana(hero);
            PlayerState owner = State.Player(player);
            if (!owner.heroes.Contains(hero.id))
            {
                owner.heroes.Add(hero.id);
            }
            Reveal(player, cell, Sight(hero));
            return hero;
        }

        private HeroState CreateHero(int def)
        {
            HeroDef heroDef = HeroData.Hero(def);
            HeroClassDef heroClass = HeroData.Class(heroDef.Class);
            var hero = new HeroState
            {
                id = State.heroes.Count,
                def = def,
                attack = heroClass.Start[0],
                defense = heroClass.Start[1],
                power = heroClass.Start[2],
                knowledge = heroClass.Start[3]
            };
            hero.skills.Add(new SkillEntry { skill = (int)heroDef.FirstSkill, level = heroDef.FirstSkill == heroDef.SecondSkill ? 2 : 1 });
            if (heroDef.SecondSkill != heroDef.FirstSkill && heroDef.SecondSkill != SkillId.None)
            {
                hero.skills.Add(new SkillEntry { skill = (int)heroDef.SecondSkill, level = 1 });
            }
            if (heroDef.Spell != SpellId.None)
            {
                hero.spells.Add((int)heroDef.Spell);
            }
            State.heroes.Add(hero);
            return hero;
        }

        /// <summary>Takes a hero off the map (beaten, fled or dismissed): back in the roster for the taverns, without an army.</summary>
        private void RemoveHero(HeroState hero)
        {
            hero.alive = false;
            PlayerState owner = State.Player(hero.owner);
            owner?.heroes.Remove(hero.id);
            hero.owner = -1;
            hero.cell = -1;
            hero.army.Clear();
            hero.luckBonus = 0;
            hero.moraleBonus = 0;
            if (!State.freeHeroes.Contains(hero.def))
            {
                State.freeHeroes.Add(hero.def);
            }
        }

        private Army Container(int player, int code, out HeroState hero, out TownState town)
        {
            hero = null;
            town = null;
            if (code >= 0)
            {
                hero = State.Hero(code);
                return hero != null && hero.alive && hero.owner == player ? hero.army : null;
            }
            town = State.Town(-code - 1);
            return town != null && town.owner == player ? town.garrison : null;
        }

        /// <summary>The cell a container stands on, for checking that two armies meet.</summary>
        private int ContainerCell(HeroState hero, TownState town)
        {
            return hero != null ? hero.cell : town != null ? town.cell : -1;
        }

        private bool MoveArmy(int player, int from, int fromSlot, int to, int toSlot, int count)
        {
            Army source = Container(player, from, out HeroState fromHero, out TownState fromTown);
            Army target = Container(player, to, out HeroState toHero, out TownState toTown);
            if (source == null || target == null || fromSlot < 0 || fromSlot >= HeroData.ArmySlots || toSlot < 0 || toSlot >= HeroData.ArmySlots)
            {
                return false;
            }
            if (from != to)
            {
                int a = ContainerCell(fromHero, fromTown);
                int b = ContainerCell(toHero, toTown);
                if (a < 0 || b < 0 || Grid.Distance(a, b) > 1)
                {
                    return false;
                }
            }
            ArmySlot src = source.slots[fromSlot];
            ArmySlot dst = target.slots[toSlot];
            if (src.IsEmpty || (from == to && fromSlot == toSlot))
            {
                return false;
            }
            int moving = count <= 0 || count >= src.count ? src.count : count;
            // A hero may not be left without any troops.
            if (fromHero != null && from != to && moving == src.count && source.StackCount == 1 && (dst.IsEmpty || dst.creature == src.creature))
            {
                return false;
            }
            if (!dst.IsEmpty && dst.creature != src.creature)
            {
                if (moving != src.count)
                {
                    return false;
                }
                // Swap two different stacks.
                (src.creature, dst.creature) = (dst.creature, src.creature);
                (src.count, dst.count) = (dst.count, src.count);
            }
            else
            {
                dst.creature = src.creature;
                dst.count += moving;
                src.count -= moving;
                source.Tidy();
            }
            Emit(EventKind.ArmyChanged, player, from, to);
            return true;
        }

        private bool Dismiss(int player, int container, int slot)
        {
            Army army = Container(player, container, out HeroState hero, out _);
            if (army == null || slot < 0 || slot >= HeroData.ArmySlots || army.slots[slot].IsEmpty)
            {
                return false;
            }
            if (hero != null && army.StackCount == 1)
            {
                return false;
            }
            army.slots[slot].creature = -1;
            army.slots[slot].count = 0;
            Emit(EventKind.ArmyChanged, player, container);
            return true;
        }

        // ------------------------------------------------------------------ the marketplace

        public int Marketplaces(int player)
        {
            int count = 0;
            foreach (int id in State.Player(player).towns)
            {
                if (State.Town(id).Has(BuildingId.Marketplace))
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>What one unit of <paramref name="get"/> costs in units of <paramref name="give"/> (gold counts in gold).</summary>
        public int TradeRate(int player, ResourceKind give, ResourceKind get)
        {
            int markets = Math.Min(4, Marketplaces(player));
            if (markets == 0 || give == get)
            {
                return 0;
            }
            // Buying costs the value times a factor that falls from 2.5 to 1 with more marketplaces; selling earns a
            // sixth of the value, up to five sixths.
            int buyFactor10 = 30 - 5 * markets;
            int sell6 = 1 + markets;
            if (give == ResourceKind.Gold)
            {
                return Land.Value(get) * buyFactor10 / 10;
            }
            if (get == ResourceKind.Gold)
            {
                // Negative: units of gold earned per unit given.
                return -(Land.Value(give) * sell6 / 6);
            }
            // Resource for resource: through gold, rounded up.
            int earned = Land.Value(give) * sell6 / 6;
            int price = Land.Value(get) * buyFactor10 / 10;
            return Math.Max(1, (price + earned - 1) / earned);
        }

        /// <summary>Gets <paramref name="amount"/> of <paramref name="get"/> for <paramref name="give"/> (selling gives gold for the amount of <paramref name="give"/>).</summary>
        private bool Trade(int player, ResourceKind give, ResourceKind get, int amount)
        {
            PlayerState owner = State.Player(player);
            int rate = TradeRate(player, give, get);
            if (rate == 0 || amount <= 0)
            {
                return false;
            }
            if (rate < 0)
            {
                // Selling: amount is what is given.
                if (owner.resources[give] < amount)
                {
                    return false;
                }
                owner.resources[give] -= amount;
                owner.resources[ResourceKind.Gold] += -rate * amount;
            }
            else
            {
                long cost = (long)rate * amount;
                if (owner.resources[give] < cost)
                {
                    return false;
                }
                owner.resources[give] -= (int)cost;
                owner.resources[get] += amount;
            }
            Emit(EventKind.ResourcesGained, player, -1, (int)get, amount);
            return true;
        }

        // ------------------------------------------------------------------ ownership

        private void SetTownOwner(TownState town, int owner)
        {
            PlayerState previous = State.Player(town.owner);
            previous?.towns.Remove(town.id);
            town.owner = owner;
            MapObject obj = State.Object(town.objectId);
            if (obj != null)
            {
                obj.owner = owner;
            }
            PlayerState next = State.Player(owner);
            if (next != null && !next.towns.Contains(town.id))
            {
                next.towns.Add(town.id);
                Reveal(owner, town.center, 7);
            }
            Emit(EventKind.TownCaptured, owner, town.id, previous != null ? previous.index : -1, text: next != null ? $"{town.name} now belongs to {next.name}." : $"{town.name} stands abandoned.");
        }
    }
}
