using System;
using System.Collections.Generic;

namespace Portfolio.Heroes
{
    public sealed partial class HeroesGame
    {
        public void GainExperience(HeroState hero, int amount)
        {
            if (hero == null || !hero.alive || amount <= 0)
            {
                return;
            }
            hero.experience += amount;
            Emit(EventKind.ExperienceGained, hero.owner, hero.id, amount, text: $"{hero.Name} gains {amount} experience.");
            int target = HeroData.LevelFor(hero.experience);
            while (hero.level < target)
            {
                LevelUp(hero);
            }
        }

        private void LevelUp(HeroState hero)
        {
            hero.level++;
            HeroClassDef heroClass = HeroData.Class(hero.Def.Class);
            // A primary stat by the chances of the class.
            int roll = Random.Range(0, 100);
            var stat = PrimaryStat.Knowledge;
            int sum = 0;
            for (int i = 0; i < 4; i++)
            {
                sum += heroClass.Growth[i];
                if (roll < sum)
                {
                    stat = (PrimaryStat)i;
                    break;
                }
            }
            switch (stat)
            {
                case PrimaryStat.Attack: hero.attack++; break;
                case PrimaryStat.Defense: hero.defense++; break;
                case PrimaryStat.Power: hero.power++; break;
                default: hero.knowledge++; break;
            }
            Emit(EventKind.HeroLeveled, hero.owner, hero.id, hero.level, (int)stat, text: $"{hero.Name} reaches level {hero.level}: +1 {HeroData.StatName(stat)}.");
            int[] options = SkillOptions(hero);
            if (options[0] < 0 && options[1] < 0)
            {
                return;
            }
            PlayerState owner = State.Player(hero.owner);
            if (owner != null && owner.human)
            {
                State.pending.Add(new PendingChoice { kind = ChoiceKind.LevelUp, player = owner.index, hero = hero.id, stat = (int)stat, options = options });
                Emit(EventKind.ChoiceNeeded, owner.index, hero.id, (int)ChoiceKind.LevelUp);
            }
            else
            {
                LearnSkill(hero, (SkillId)(options[0] >= 0 ? options[0] : options[1]));
            }
        }

        /// <summary>Two skills to choose from on a level up: an upgrade of one the hero has and a new one (or two upgrades).</summary>
        private int[] SkillOptions(HeroState hero)
        {
            HeroClassDef heroClass = HeroData.Class(hero.Def.Class);
            var upgrades = new List<int>();
            var fresh = new List<int>();
            for (int skill = 0; skill < HeroData.SkillCount; skill++)
            {
                int level = hero.SkillLevel((SkillId)skill);
                int weight = heroClass.SkillWeights[skill];
                if (skill == (int)SkillId.Necromancy && hero.Def.Faction != Faction.Necropolis)
                {
                    weight = 0;
                }
                if (level > 0 && level < HeroData.MaxSkillLevel)
                {
                    upgrades.Add(skill);
                }
                else if (level == 0 && weight > 0 && hero.skills.Count < HeroData.MaxSkills)
                {
                    fresh.Add(skill);
                }
            }
            int first = PickWeighted(upgrades, heroClass);
            if (first >= 0)
            {
                upgrades.Remove(first);
            }
            int second = PickWeighted(fresh, heroClass);
            if (second < 0)
            {
                second = PickWeighted(upgrades, heroClass);
            }
            if (first < 0 && second >= 0)
            {
                // Only new skills: offer two different ones.
                fresh.Remove(second);
                first = PickWeighted(fresh, heroClass);
            }
            return new[] { first, second };
        }

        private int PickWeighted(List<int> skills, HeroClassDef heroClass)
        {
            int total = 0;
            foreach (int skill in skills)
            {
                total += Math.Max(1, heroClass.SkillWeights[skill]);
            }
            if (total == 0)
            {
                return -1;
            }
            int roll = Random.Range(0, total);
            foreach (int skill in skills)
            {
                roll -= Math.Max(1, heroClass.SkillWeights[skill]);
                if (roll < 0)
                {
                    return skill;
                }
            }
            return skills[skills.Count - 1];
        }

        private void LearnSkill(HeroState hero, SkillId skill)
        {
            foreach (SkillEntry entry in hero.skills)
            {
                if (entry.skill == (int)skill)
                {
                    entry.level = Math.Min(HeroData.MaxSkillLevel, entry.level + 1);
                    Emit(EventKind.StatRaised, hero.owner, hero.id, -1, (int)skill, text: $"{hero.Name}: {HeroData.SkillLevelName(entry.level)} {HeroData.Skill(skill).Name}");
                    AfterSkill(hero, skill);
                    return;
                }
            }
            if (hero.skills.Count >= HeroData.MaxSkills)
            {
                return;
            }
            hero.skills.Add(new SkillEntry { skill = (int)skill, level = 1 });
            Emit(EventKind.StatRaised, hero.owner, hero.id, -1, (int)skill, text: $"{hero.Name}: Basic {HeroData.Skill(skill).Name}");
            AfterSkill(hero, skill);
        }

        private void AfterSkill(HeroState hero, SkillId skill)
        {
            if (skill == SkillId.Wisdom)
            {
                // Spells of the guild the hero stands in that were too hard until now.
                TownState town = TownAtGate(hero.cell);
                if (town != null)
                {
                    LearnGuildSpells(hero, town);
                }
            }
        }

        private bool Choose(int player, int option)
        {
            if (State.pending.Count == 0 || State.pending[0].player != player)
            {
                return false;
            }
            PendingChoice choice = State.pending[0];
            HeroState hero = State.Hero(choice.hero);
            State.pending.RemoveAt(0);
            if (hero == null || !hero.alive)
            {
                return true;
            }
            switch (choice.kind)
            {
                case ChoiceKind.LevelUp:
                {
                    int index = option == 1 ? 1 : 0;
                    int skill = choice.options[index] >= 0 ? choice.options[index] : choice.options[1 - index];
                    if (skill >= 0)
                    {
                        LearnSkill(hero, (SkillId)skill);
                    }
                    break;
                }
                case ChoiceKind.Treasure:
                    GiveTreasure(hero, option == 1 ? choice.options[1] : choice.options[0], option != 1);
                    break;
                case ChoiceKind.Arena:
                    RaiseStat(hero, option == 1 ? PrimaryStat.Defense : PrimaryStat.Attack, 2);
                    break;
            }
            CheckGoalsNow();
            return true;
        }

        // ------------------------------------------------------------------ artifacts

        public void GiveArtifact(HeroState hero, ArtifactId id)
        {
            ArtifactDef def = Artifacts.Get(id);
            if (def == null)
            {
                return;
            }
            int slot = (int)def.Slot;
            int worn = hero.equipped[slot];
            if (worn < 0)
            {
                hero.equipped[slot] = (int)id;
            }
            else if (Artifacts.Get((ArtifactId)worn).Value < def.Value)
            {
                hero.backpack.Add(worn);
                hero.equipped[slot] = (int)id;
            }
            else
            {
                hero.backpack.Add((int)id);
            }
            if (def.Knowledge > 0)
            {
                hero.mana = Math.Min(MaxMana(hero), hero.mana + def.Knowledge * 10);
            }
            Emit(EventKind.ArtifactFound, hero.owner, hero.id, (int)id, text: $"{hero.Name} finds the {def.Name} ({def.Description}).");
            CheckGoalsNow();
        }

        /// <summary>Wears an artifact from the backpack, putting what was worn in its place into the backpack.</summary>
        public bool Equip(HeroState hero, ArtifactId id)
        {
            int index = hero.backpack.IndexOf((int)id);
            ArtifactDef def = Artifacts.Get(id);
            if (index < 0 || def == null)
            {
                return false;
            }
            hero.backpack.RemoveAt(index);
            int slot = (int)def.Slot;
            if (hero.equipped[slot] >= 0)
            {
                hero.backpack.Add(hero.equipped[slot]);
            }
            hero.equipped[slot] = (int)id;
            return true;
        }
    }
}
