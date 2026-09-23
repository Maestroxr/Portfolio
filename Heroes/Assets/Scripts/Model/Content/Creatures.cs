using System;
using System.Collections.Generic;

namespace Portfolio.Heroes
{
    /// <summary>Every kind of creature. The numbers are saved and sent online: only ever append.</summary>
    public enum CreatureId
    {
        None = -1,

        // Castle
        Militia = 0,
        Archer = 1,
        Swordsman = 2,
        Priest = 3,
        Crusader = 4,
        Cavalier = 5,
        GoldDragon = 6,

        // Necropolis
        Skeleton = 7,
        Zombie = 8,
        BoneGuard = 9,
        SkeletonArcher = 10,
        VampireBat = 11,
        Lich = 12,
        BoneDragon = 13,

        // Stronghold
        Wolf = 14,
        Brawler = 15,
        Hunter = 16,
        Shaman = 17,
        Berserker = 18,
        WarBull = 19,
        RedDragon = 20,

        // Neutrals of the wilds
        Slime = 21,
        Bandit = 22,
        DireWolf = 23,
        FireFox = 24,
        Hermit = 25,
        ElderStag = 26,
        GreenDragon = 27,

        // Town defences
        ArrowTower = 28
    }

    [Flags]
    public enum Ability
    {
        None = 0,
        Flying = 1 << 0,
        Ranged = 1 << 1,
        NoMeleePenalty = 1 << 2,
        DoubleAttack = 1 << 3,
        NoRetaliation = 1 << 4,
        UnlimitedRetaliation = 1 << 5,
        Undead = 1 << 6,
        LifeDrain = 1 << 7,
        AreaShot = 1 << 8,
        Charge = 1 << 9,
        Breath = 1 << 10,
        TwoRetaliations = 1 << 11,
        Regenerate = 1 << 12,
        Immobile = 1 << 13,
        MagicResist = 1 << 14,
        Fearsome = 1 << 15
    }

    /// <summary>What a creature is: its numbers in battle, its price and weekly growth, its look.</summary>
    public sealed class CreatureDef
    {
        public CreatureId Id;
        public Faction Faction;
        public int Tier;
        public string Name;
        public string Plural;
        public int Attack;
        public int Defense;
        public int MinDamage;
        public int MaxDamage;
        public int Health;
        public int Speed;
        public int Shots;
        public int Growth;
        public ResourceSet Cost;
        public Ability Abilities;
        /// <summary>How much a computer player values one of these, in gold.</summary>
        public int Value;
        public string Description;

        public bool Has(Ability ability)
        {
            return (Abilities & ability) != 0;
        }

        public bool IsRanged => Has(Ability.Ranged);
        public bool IsFlying => Has(Ability.Flying);
        public bool IsUndead => Has(Ability.Undead);

        public string NameFor(int count)
        {
            return count == 1 ? Name : Plural;
        }
    }

    public static class Creatures
    {
        private static readonly CreatureDef[] Table = Build();

        public static int Count => Table.Length;

        public static CreatureDef Get(CreatureId id)
        {
            int index = (int)id;
            return index >= 0 && index < Table.Length ? Table[index] : null;
        }

        public static CreatureDef Get(int id)
        {
            return id >= 0 && id < Table.Length ? Table[id] : null;
        }

        public static IEnumerable<CreatureDef> All => Table;

        /// <summary>The creature of a faction's dwelling of <paramref name="tier"/> (1 to 7).</summary>
        public static CreatureId OfTier(Faction faction, int tier)
        {
            if (faction == Faction.Neutral || tier < 1 || tier > 7)
            {
                return CreatureId.None;
            }
            return (CreatureId)((int)faction * 7 + tier - 1);
        }

        public static readonly CreatureId[] Neutrals =
        {
            CreatureId.Slime, CreatureId.Bandit, CreatureId.DireWolf, CreatureId.FireFox, CreatureId.Hermit,
            CreatureId.ElderStag, CreatureId.GreenDragon
        };

        private static CreatureDef Def(CreatureId id, Faction faction, int tier, string name, string plural, int attack, int defense,
            int min, int max, int health, int speed, int growth, ResourceSet cost, Ability abilities, int shots, int value, string description)
        {
            return new CreatureDef
            {
                Id = id,
                Faction = faction,
                Tier = tier,
                Name = name,
                Plural = plural,
                Attack = attack,
                Defense = defense,
                MinDamage = min,
                MaxDamage = max,
                Health = health,
                Speed = speed,
                Growth = growth,
                Cost = cost,
                Abilities = abilities,
                Shots = shots,
                Value = value,
                Description = description
            };
        }

        private static CreatureDef[] Build()
        {
            var list = new List<CreatureDef>
            {
                // ------------------------------------------------------------------ Castle
                Def(CreatureId.Militia, Faction.Castle, 1, "Militiaman", "Militia", 4, 5, 1, 3, 10, 4, 14,
                    new ResourceSet(60), Ability.None, 0, 80, "Levied farmhands with spear and buckler. Cheap, stubborn and plentiful."),
                Def(CreatureId.Archer, Faction.Castle, 2, "Archer", "Archers", 6, 3, 2, 3, 10, 4, 9,
                    new ResourceSet(100), Ability.Ranged, 12, 150, "Longbowmen of the royal forests. Weak up close, deadly from behind a wall of pikes."),
                Def(CreatureId.Swordsman, Faction.Castle, 3, "Swordsman", "Swordsmen", 9, 10, 5, 8, 30, 5, 7,
                    new ResourceSet(250), Ability.None, 0, 330, "Sword and shield, the backbone of every royal army."),
                Def(CreatureId.Priest, Faction.Castle, 4, "Priest", "Priests", 11, 8, 9, 12, 30, 5, 4,
                    new ResourceSet(400), Ability.Ranged | Ability.NoMeleePenalty, 12, 560, "Their blessed staves strike with holy light at any range."),
                Def(CreatureId.Crusader, Faction.Castle, 5, "Crusader", "Crusaders", 14, 12, 8, 12, 50, 6, 3,
                    new ResourceSet(650), Ability.DoubleAttack, 0, 900, "Zealots in heavy plate who strike twice before the enemy can answer."),
                Def(CreatureId.Cavalier, Faction.Castle, 6, "Cavalier", "Cavaliers", 16, 15, 15, 25, 100, 7, 2,
                    new ResourceSet(1100, gems: 1), Ability.Charge, 0, 1700, "Armoured knights on white chargers; every hex of a gallop adds to the blow."),
                Def(CreatureId.GoldDragon, Faction.Castle, 7, "Gold Dragon", "Gold Dragons", 25, 23, 40, 50, 220, 11, 1,
                    new ResourceSet(3500, gems: 3), Ability.Flying | Ability.Breath | Ability.MagicResist, 0, 5500, "Ancient guardians of the crown. Their breath burns two ranks at once."),

                // ------------------------------------------------------------------ Necropolis
                Def(CreatureId.Skeleton, Faction.Necropolis, 1, "Skeleton", "Skeletons", 5, 4, 1, 3, 6, 4, 12,
                    new ResourceSet(60), Ability.Undead, 0, 70, "The restless dead, raised by the thousand after every battle."),
                Def(CreatureId.Zombie, Faction.Necropolis, 2, "Zombie", "Zombies", 5, 5, 2, 3, 20, 3, 8,
                    new ResourceSet(100), Ability.Undead, 0, 140, "Slow, rotting and hard to put down."),
                Def(CreatureId.BoneGuard, Faction.Necropolis, 3, "Bone Guard", "Bone Guards", 8, 10, 4, 7, 25, 5, 7,
                    new ResourceSet(220), Ability.Undead | Ability.UnlimitedRetaliation, 0, 300, "Shield bearers that strike back at every blow, again and again."),
                Def(CreatureId.SkeletonArcher, Faction.Necropolis, 4, "Bone Archer", "Bone Archers", 10, 7, 6, 10, 25, 5, 5,
                    new ResourceSet(350), Ability.Undead | Ability.Ranged, 12, 480, "Hooded marksmen whose crossbows never tire."),
                Def(CreatureId.VampireBat, Faction.Necropolis, 5, "Vampire Bat", "Vampire Bats", 12, 9, 7, 11, 40, 9, 4,
                    new ResourceSet(500), Ability.Undead | Ability.Flying | Ability.NoRetaliation | Ability.LifeDrain, 0, 750, "Swooping blood drinkers: nobody strikes back, and their bite heals their kin."),
                Def(CreatureId.Lich, Faction.Necropolis, 6, "Lich", "Liches", 13, 12, 11, 15, 40, 6, 2,
                    new ResourceSet(900, mercury: 1), Ability.Undead | Ability.Ranged | Ability.AreaShot, 12, 1400, "Sorcerers who cheated death. Their death clouds rot everything around the target."),
                Def(CreatureId.BoneDragon, Faction.Necropolis, 7, "Bone Dragon", "Bone Dragons", 22, 20, 30, 45, 180, 9, 1,
                    new ResourceSet(2800, mercury: 3), Ability.Undead | Ability.Flying | Ability.Fearsome, 0, 4600, "A dragon's skeleton bound by necromancy. Its presence chills the bravest."),

                // ------------------------------------------------------------------ Stronghold
                Def(CreatureId.Wolf, Faction.Stronghold, 1, "Wolf", "Wolves", 5, 3, 1, 4, 8, 7, 13,
                    new ResourceSet(70), Ability.None, 0, 90, "Fast pack hunters that harry the flanks."),
                Def(CreatureId.Brawler, Faction.Stronghold, 2, "Brawler", "Brawlers", 7, 4, 2, 5, 15, 5, 9,
                    new ResourceSet(120), Ability.DoubleAttack, 0, 170, "Bare knuckled monks of the steppe who hit twice as often."),
                Def(CreatureId.Hunter, Faction.Stronghold, 3, "Hunter", "Hunters", 8, 6, 3, 6, 18, 6, 7,
                    new ResourceSet(200), Ability.Ranged, 10, 280, "Hooded trackers with heavy crossbows."),
                Def(CreatureId.Shaman, Faction.Stronghold, 4, "Shaman", "Shamans", 11, 7, 8, 12, 30, 5, 4,
                    new ResourceSet(380), Ability.Ranged | Ability.MagicResist, 10, 520, "Spirit callers whose bolts of lightning crackle across the field."),
                Def(CreatureId.Berserker, Faction.Stronghold, 5, "Berserker", "Berserkers", 16, 9, 10, 16, 60, 6, 3,
                    new ResourceSet(600), Ability.NoRetaliation, 0, 880, "Frenzied axemen: their rage leaves no chance to strike back."),
                Def(CreatureId.WarBull, Faction.Stronghold, 6, "War Bull", "War Bulls", 18, 14, 16, 26, 110, 8, 2,
                    new ResourceSet(1000, crystal: 1), Ability.Charge, 0, 1650, "Painted aurochs that trample everything in their path."),
                Def(CreatureId.RedDragon, Faction.Stronghold, 7, "Red Dragon", "Red Dragons", 24, 20, 35, 50, 200, 10, 1,
                    new ResourceSet(3200, crystal: 3), Ability.Flying | Ability.Breath, 0, 5000, "The pride of the warlords, bred in volcanic pits."),

                // ------------------------------------------------------------------ Neutrals
                Def(CreatureId.Slime, Faction.Neutral, 1, "Bog Slime", "Bog Slimes", 3, 6, 1, 2, 12, 3, 12,
                    new ResourceSet(50), Ability.Regenerate, 0, 60, "Quivering swamp ooze that knits itself back together."),
                Def(CreatureId.Bandit, Faction.Neutral, 2, "Bandit", "Bandits", 7, 4, 2, 4, 14, 6, 8,
                    new ResourceSet(110), Ability.None, 0, 150, "Cutthroats who prey on lonely roads."),
                Def(CreatureId.DireWolf, Faction.Neutral, 3, "Dire Wolf", "Dire Wolves", 9, 6, 3, 6, 24, 8, 6,
                    new ResourceSet(200), Ability.DoubleAttack, 0, 320, "Huge grey wolves of the northern woods."),
                Def(CreatureId.FireFox, Faction.Neutral, 3, "Fire Fox", "Fire Foxes", 10, 5, 4, 7, 20, 9, 6,
                    new ResourceSet(220), Ability.NoRetaliation, 0, 330, "Quick as sparks, gone before anyone can strike back."),
                Def(CreatureId.Hermit, Faction.Neutral, 4, "Hermit Mage", "Hermit Mages", 12, 7, 8, 12, 28, 5, 4,
                    new ResourceSet(400), Ability.Ranged | Ability.NoMeleePenalty, 12, 560, "Recluses who guard forgotten lore with fire."),
                Def(CreatureId.ElderStag, Faction.Neutral, 5, "Elder Stag", "Elder Stags", 15, 12, 12, 18, 70, 8, 3,
                    new ResourceSet(700), Ability.Charge | Ability.Regenerate, 0, 1000, "Spirits of the old forest wearing the shape of stags."),
                Def(CreatureId.GreenDragon, Faction.Neutral, 7, "Green Dragon", "Green Dragons", 22, 21, 35, 45, 200, 10, 1,
                    new ResourceSet(3000, sulfur: 2), Ability.Flying | Ability.Breath, 0, 4800, "They nest on hoards and do not share."),

                Def(CreatureId.ArrowTower, Faction.Neutral, 0, "Arrow Tower", "Arrow Towers", 10, 20, 10, 15, 200, 0, 0,
                    new ResourceSet(0), Ability.Ranged | Ability.Immobile | Ability.NoMeleePenalty | Ability.MagicResist, 99, 1500, "The towers of the town shoot at the besiegers every round."),
            };
            var table = new CreatureDef[list.Count];
            foreach (CreatureDef def in list)
            {
                table[(int)def.Id] = def;
            }
            return table;
        }
    }
}
