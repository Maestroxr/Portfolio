using System.Collections.Generic;

namespace Portfolio.Heroes
{
    /// <summary>Spells. The numbers are saved and sent online: only ever append.</summary>
    public enum SpellId
    {
        None = -1,
        MagicArrow = 0,
        Bless = 1,
        Cure = 2,
        Haste = 3,
        Slow = 4,
        StoneSkin = 5,
        LightningBolt = 6,
        Bloodlust = 7,
        Curse = 8,
        Weakness = 9,
        IceBolt = 10,
        Fireball = 11,
        AnimateDead = 12,
        Prayer = 13,
        MeteorShower = 14,
        Resurrection = 15,
        Implosion = 16
    }

    public enum SpellTarget
    {
        Enemy,
        Friend,
        Area,
        AllFriends,
        DeadFriend
    }

    public enum SpellSchool
    {
        Fire,
        Air,
        Water,
        Earth
    }

    public sealed class SpellDef
    {
        public SpellId Id;
        public string Name;
        public int Level;
        public int Cost;
        public SpellTarget Target;
        public SpellSchool School;
        /// <summary>Damage (or healing, or resurrected health) is Base + PerPower times the caster's spell power.</summary>
        public int Base;
        public int PerPower;
        /// <summary>Radius of an area spell.</summary>
        public int Radius;
        /// <summary>For effects: the size of the change (attack, defense, speed points).</summary>
        public int Amount;
        public bool IsEffect;
        public bool IsDamage;
        public bool IsPositive;
        public string Description;
    }

    public static class Spells
    {
        private static readonly SpellDef[] Table =
        {
            D(SpellId.MagicArrow, "Magic Arrow", 1, 5, SpellTarget.Enemy, SpellSchool.Air, 10, 10, "A bolt of force strikes one enemy troop."),
            E(SpellId.Bless, "Bless", 1, 5, SpellTarget.Friend, SpellSchool.Water, 0, true, "The troop deals its greatest damage with every blow."),
            new SpellDef { Id = SpellId.Cure, Name = "Cure", Level = 1, Cost = 6, Target = SpellTarget.Friend, School = SpellSchool.Water, Base = 10, PerPower = 5, IsPositive = true,
                Description = "Heals a troop and lifts every harmful spell from it." },
            E(SpellId.Haste, "Haste", 1, 6, SpellTarget.Friend, SpellSchool.Air, 3, true, "The troop moves three hexes farther."),
            E(SpellId.Slow, "Slow", 1, 6, SpellTarget.Enemy, SpellSchool.Earth, 0, false, "The troop moves at half its speed."),
            E(SpellId.StoneSkin, "Stone Skin", 1, 5, SpellTarget.Friend, SpellSchool.Earth, 3, true, "The troop's skin hardens: +3 defense."),
            D(SpellId.LightningBolt, "Lightning Bolt", 2, 10, SpellTarget.Enemy, SpellSchool.Air, 10, 25, "Lightning strikes one enemy troop."),
            E(SpellId.Bloodlust, "Bloodlust", 2, 5, SpellTarget.Friend, SpellSchool.Fire, 3, true, "+3 attack in melee."),
            E(SpellId.Curse, "Curse", 2, 6, SpellTarget.Enemy, SpellSchool.Fire, 0, false, "The troop deals its least damage with every blow."),
            E(SpellId.Weakness, "Weakness", 2, 8, SpellTarget.Enemy, SpellSchool.Water, 3, false, "-3 attack."),
            D(SpellId.IceBolt, "Ice Bolt", 2, 8, SpellTarget.Enemy, SpellSchool.Water, 10, 20, "A lance of ice pierces one enemy troop."),
            new SpellDef { Id = SpellId.Fireball, Name = "Fireball", Level = 3, Cost = 15, Target = SpellTarget.Area, School = SpellSchool.Fire, Base = 15, PerPower = 10, Radius = 1, IsDamage = true,
                Description = "Burns every troop on the target hex and around it, friend or foe." },
            new SpellDef { Id = SpellId.AnimateDead, Name = "Animate Dead", Level = 3, Cost = 15, Target = SpellTarget.DeadFriend, School = SpellSchool.Earth, Base = 30, PerPower = 50, IsPositive = true,
                Description = "Raises fallen undead of a troop back to its ranks." },
            new SpellDef { Id = SpellId.Prayer, Name = "Prayer", Level = 4, Cost = 16, Target = SpellTarget.AllFriends, School = SpellSchool.Water, Amount = 2, IsEffect = true, IsPositive = true,
                Description = "+2 attack, defense and speed for every friendly troop." },
            new SpellDef { Id = SpellId.MeteorShower, Name = "Meteor Shower", Level = 4, Cost = 16, Target = SpellTarget.Area, School = SpellSchool.Earth, Base = 25, PerPower = 25, Radius = 1, IsDamage = true,
                Description = "Rocks rain on the target hex and around it." },
            new SpellDef { Id = SpellId.Resurrection, Name = "Resurrection", Level = 4, Cost = 20, Target = SpellTarget.DeadFriend, School = SpellSchool.Earth, Base = 40, PerPower = 50, IsPositive = true,
                Description = "Brings fallen living creatures of a troop back to life." },
            D(SpellId.Implosion, "Implosion", 5, 30, SpellTarget.Enemy, SpellSchool.Earth, 100, 75, "Crushes one enemy troop from within."),
        };

        private static SpellDef D(SpellId id, string name, int level, int cost, SpellTarget target, SpellSchool school, int baseDamage, int perPower, string description)
        {
            return new SpellDef { Id = id, Name = name, Level = level, Cost = cost, Target = target, School = school, Base = baseDamage, PerPower = perPower, IsDamage = true, Description = description };
        }

        private static SpellDef E(SpellId id, string name, int level, int cost, SpellTarget target, SpellSchool school, int amount, bool positive, string description)
        {
            return new SpellDef { Id = id, Name = name, Level = level, Cost = cost, Target = target, School = school, Amount = amount, IsEffect = true, IsPositive = positive, Description = description };
        }

        public static int Count => Table.Length;

        public static SpellDef Get(SpellId id)
        {
            int index = (int)id;
            return index >= 0 && index < Table.Length ? Table[index] : null;
        }

        public static IEnumerable<SpellDef> All => Table;

        public static List<SpellId> OfLevel(int level)
        {
            var list = new List<SpellId>();
            foreach (SpellDef def in Table)
            {
                if (def.Level == level)
                {
                    list.Add(def.Id);
                }
            }
            return list;
        }
    }
}
