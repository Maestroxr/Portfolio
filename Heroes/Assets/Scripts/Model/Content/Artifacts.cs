using System.Collections.Generic;

namespace Portfolio.Heroes
{
    public enum ArtifactSlot
    {
        Weapon = 0,
        Shield = 1,
        Helm = 2,
        Armor = 3,
        Cloak = 4,
        Boots = 5,
        Ring = 6,
        Neck = 7
    }

    /// <summary>Artifacts. The numbers are saved and sent online: only ever append.</summary>
    public enum ArtifactId
    {
        None = -1,
        IronBlade = 0,
        RunedAxe = 1,
        SunSword = 2,
        OakShield = 3,
        TowerShield = 4,
        DragonScaleShield = 5,
        ScholarCap = 6,
        CrownOfTheMagi = 7,
        LeatherJerkin = 8,
        MithrilMail = 9,
        GoldenPlate = 10,
        TravelerCloak = 11,
        CloakOfShadows = 12,
        BootsOfSpeed = 13,
        WingedBoots = 14,
        RingOfVitality = 15,
        RingOfTheMagi = 16,
        CloverRing = 17,
        PendantOfCourage = 18,
        AmuletOfTheGrave = 19,
        GoblinGoldPouch = 20,
        ChaliceOfLife = 21,
        TomeOfWar = 22,
        CrownOfDragons = 23
    }

    public sealed class ArtifactDef
    {
        public ArtifactId Id;
        public string Name;
        public ArtifactSlot Slot;
        public int Attack;
        public int Defense;
        public int Power;
        public int Knowledge;
        public int Morale;
        public int Luck;
        /// <summary>Extra movement points a day.</summary>
        public int Movement;
        /// <summary>Extra health of every creature of the hero's army.</summary>
        public int Health;
        /// <summary>Extra speed of every creature of the hero's army.</summary>
        public int Speed;
        /// <summary>Gold a day.</summary>
        public int Gold;
        /// <summary>Extra percent of the fallen raised by necromancy.</summary>
        public int Necromancy;
        /// <summary>Rarity 1 (treasure) to 3 (relic).</summary>
        public int Rank;
        /// <summary>The item picture (a Quaternius RPG item) the view shows.</summary>
        public string Item;
        public string Description;

        public int Value => Rank * 1500 + (Attack + Defense + Power + Knowledge) * 300 + (Morale + Luck) * 400 + Movement + Health * 400 + Speed * 1500 + Gold * 2;
    }

    public static class Artifacts
    {
        private static readonly ArtifactDef[] Table =
        {
            new ArtifactDef { Id = ArtifactId.IronBlade, Name = "Iron Blade of the Marches", Slot = ArtifactSlot.Weapon, Attack = 2, Rank = 1, Item = "Sword", Description = "+2 Attack" },
            new ArtifactDef { Id = ArtifactId.RunedAxe, Name = "Runed Axe of the Clans", Slot = ArtifactSlot.Weapon, Attack = 4, Rank = 2, Item = "Axe_Double", Description = "+4 Attack" },
            new ArtifactDef { Id = ArtifactId.SunSword, Name = "Sunforged Greatsword", Slot = ArtifactSlot.Weapon, Attack = 6, Defense = 1, Rank = 3, Item = "Sword_big_Golden", Description = "+6 Attack, +1 Defense" },
            new ArtifactDef { Id = ArtifactId.OakShield, Name = "Oaken Shield of the Watch", Slot = ArtifactSlot.Shield, Defense = 2, Rank = 1, Item = "Chest_Closed", Description = "+2 Defense" },
            new ArtifactDef { Id = ArtifactId.TowerShield, Name = "Tower Shield of the Keep", Slot = ArtifactSlot.Shield, Defense = 4, Rank = 2, Item = "Armor_Metal", Description = "+4 Defense" },
            new ArtifactDef { Id = ArtifactId.DragonScaleShield, Name = "Dragonscale Shield", Slot = ArtifactSlot.Shield, Defense = 6, Attack = 1, Rank = 3, Item = "Armor_Golden", Description = "+6 Defense, +1 Attack" },
            new ArtifactDef { Id = ArtifactId.ScholarCap, Name = "Scholar's Cap", Slot = ArtifactSlot.Helm, Knowledge = 2, Rank = 1, Item = "Book1_Closed", Description = "+2 Knowledge" },
            new ArtifactDef { Id = ArtifactId.CrownOfTheMagi, Name = "Crown of the Magi", Slot = ArtifactSlot.Helm, Knowledge = 4, Power = 2, Rank = 3, Item = "Crown", Description = "+4 Knowledge, +2 Spell Power" },
            new ArtifactDef { Id = ArtifactId.LeatherJerkin, Name = "Hunter's Jerkin", Slot = ArtifactSlot.Armor, Defense = 1, Attack = 1, Rank = 1, Item = "Armor_Leather", Description = "+1 Attack, +1 Defense" },
            new ArtifactDef { Id = ArtifactId.MithrilMail, Name = "Mithril Mail", Slot = ArtifactSlot.Armor, Defense = 3, Power = 1, Rank = 2, Item = "Armor_Metal", Description = "+3 Defense, +1 Spell Power" },
            new ArtifactDef { Id = ArtifactId.GoldenPlate, Name = "Golden Plate of Kings", Slot = ArtifactSlot.Armor, Attack = 2, Defense = 2, Power = 2, Knowledge = 2, Rank = 3, Item = "Armor_Golden", Description = "+2 to every primary skill" },
            new ArtifactDef { Id = ArtifactId.TravelerCloak, Name = "Traveler's Cloak", Slot = ArtifactSlot.Cloak, Movement = 200, Rank = 1, Item = "Bag", Description = "+200 movement a day" },
            new ArtifactDef { Id = ArtifactId.CloakOfShadows, Name = "Cloak of Shadows", Slot = ArtifactSlot.Cloak, Power = 3, Luck = 1, Rank = 2, Item = "Pouch", Description = "+3 Spell Power, +1 Luck" },
            new ArtifactDef { Id = ArtifactId.BootsOfSpeed, Name = "Boots of Speed", Slot = ArtifactSlot.Boots, Movement = 400, Rank = 2, Item = "Glove", Description = "+400 movement a day" },
            new ArtifactDef { Id = ArtifactId.WingedBoots, Name = "Winged Sandals", Slot = ArtifactSlot.Boots, Movement = 300, Speed = 1, Rank = 3, Item = "Star", Description = "+300 movement a day, +1 speed for the army" },
            new ArtifactDef { Id = ArtifactId.RingOfVitality, Name = "Ring of Vitality", Slot = ArtifactSlot.Ring, Health = 1, Rank = 1, Item = "Ring3", Description = "+1 health for every creature" },
            new ArtifactDef { Id = ArtifactId.RingOfTheMagi, Name = "Ring of the Magi", Slot = ArtifactSlot.Ring, Power = 2, Knowledge = 1, Rank = 2, Item = "Ring5", Description = "+2 Spell Power, +1 Knowledge" },
            new ArtifactDef { Id = ArtifactId.CloverRing, Name = "Four-leaf Ring", Slot = ArtifactSlot.Ring, Luck = 2, Rank = 1, Item = "Ring6", Description = "+2 Luck" },
            new ArtifactDef { Id = ArtifactId.PendantOfCourage, Name = "Pendant of Courage", Slot = ArtifactSlot.Neck, Morale = 2, Luck = 1, Rank = 2, Item = "Necklace1", Description = "+2 Morale, +1 Luck" },
            new ArtifactDef { Id = ArtifactId.AmuletOfTheGrave, Name = "Amulet of the Grave", Slot = ArtifactSlot.Neck, Necromancy = 10, Power = 1, Rank = 2, Item = "Necklace3", Description = "+10% necromancy, +1 Spell Power" },
            new ArtifactDef { Id = ArtifactId.GoblinGoldPouch, Name = "Bottomless Purse", Slot = ArtifactSlot.Neck, Gold = 500, Rank = 2, Item = "Coin_Star", Description = "+500 gold a day" },
            new ArtifactDef { Id = ArtifactId.ChaliceOfLife, Name = "Chalice of Life", Slot = ArtifactSlot.Helm, Health = 2, Morale = 1, Rank = 3, Item = "Chalice", Description = "+2 health for every creature, +1 Morale" },
            new ArtifactDef { Id = ArtifactId.TomeOfWar, Name = "Tome of War", Slot = ArtifactSlot.Cloak, Attack = 3, Defense = 3, Rank = 3, Item = "Book3_Closed", Description = "+3 Attack, +3 Defense" },
            new ArtifactDef { Id = ArtifactId.CrownOfDragons, Name = "Crown of Dragons", Slot = ArtifactSlot.Helm, Attack = 4, Defense = 4, Power = 4, Knowledge = 4, Rank = 4, Item = "Crown2", Description = "+4 to every primary skill. The prize of kings." },
        };

        public static int Count => Table.Length;

        public static ArtifactDef Get(ArtifactId id)
        {
            int index = (int)id;
            return index >= 0 && index < Table.Length ? Table[index] : null;
        }

        public static IEnumerable<ArtifactDef> All => Table;

        public static List<ArtifactId> OfRank(int rank)
        {
            var list = new List<ArtifactId>();
            foreach (ArtifactDef def in Table)
            {
                if (def.Rank == rank)
                {
                    list.Add(def.Id);
                }
            }
            return list;
        }

        public static string SlotName(ArtifactSlot slot)
        {
            switch (slot)
            {
                case ArtifactSlot.Weapon: return "Weapon";
                case ArtifactSlot.Shield: return "Shield";
                case ArtifactSlot.Helm: return "Head";
                case ArtifactSlot.Armor: return "Torso";
                case ArtifactSlot.Cloak: return "Shoulders";
                case ArtifactSlot.Boots: return "Feet";
                case ArtifactSlot.Ring: return "Ring";
                default: return "Neck";
            }
        }
    }
}
