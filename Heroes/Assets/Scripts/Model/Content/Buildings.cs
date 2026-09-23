using System.Collections.Generic;

namespace Portfolio.Heroes
{
    /// <summary>The buildings of a town. Every faction has the same ones under names of its own.</summary>
    public enum BuildingId
    {
        VillageHall = 0,
        TownHall = 1,
        CityHall = 2,
        Fort = 3,
        Citadel = 4,
        Castle = 5,
        Tavern = 6,
        Marketplace = 7,
        MageGuild1 = 8,
        MageGuild2 = 9,
        MageGuild3 = 10,
        Dwelling1 = 11,
        Dwelling2 = 12,
        Dwelling3 = 13,
        Dwelling4 = 14,
        Dwelling5 = 15,
        Dwelling6 = 16,
        Dwelling7 = 17,
        Silo = 18
    }

    public sealed class BuildingDef
    {
        public BuildingId Id;
        public string Name;
        public ResourceSet Cost;
        public BuildingId[] Requires;
        public string Description;

        /// <summary>The creature tier (1 to 7) the building houses, or 0.</summary>
        public int DwellingTier => Id >= BuildingId.Dwelling1 && Id <= BuildingId.Dwelling7 ? Id - BuildingId.Dwelling1 + 1 : 0;
    }

    public static class Buildings
    {
        public const int Count = 19;

        private static readonly Dictionary<(Faction, BuildingId), BuildingDef> Table = Build();

        public static BuildingDef Get(Faction faction, BuildingId id)
        {
            if (faction == Faction.Neutral)
            {
                faction = Faction.Castle;
            }
            return Table.TryGetValue((faction, id), out BuildingDef def) ? def : null;
        }

        public static BuildingId DwellingOf(int tier)
        {
            return BuildingId.Dwelling1 + (tier - 1);
        }

        /// <summary>Gold a day from the town's hall.</summary>
        public static int Income(TownState town)
        {
            if (town.Has(BuildingId.CityHall))
            {
                return 2000;
            }
            if (town.Has(BuildingId.TownHall))
            {
                return 1000;
            }
            return 500;
        }

        /// <summary>Percent the weekly growth of the dwellings is raised by the town's fortifications.</summary>
        public static int GrowthBonus(TownState town)
        {
            if (town.Has(BuildingId.Castle))
            {
                return 100;
            }
            if (town.Has(BuildingId.Citadel))
            {
                return 50;
            }
            return 0;
        }

        public static int MageGuildLevel(TownState town)
        {
            if (town.Has(BuildingId.MageGuild3))
            {
                return 3;
            }
            if (town.Has(BuildingId.MageGuild2))
            {
                return 2;
            }
            return town.Has(BuildingId.MageGuild1) ? 1 : 0;
        }

        /// <summary>Arrow towers that defend the town in a siege.</summary>
        public static int Towers(TownState town)
        {
            if (town.Has(BuildingId.Castle))
            {
                return 3;
            }
            if (town.Has(BuildingId.Citadel))
            {
                return 1;
            }
            return 0;
        }

        private static string[] Names(Faction faction)
        {
            switch (faction)
            {
                case Faction.Necropolis:
                    return new[]
                    {
                        "Village Hall", "Town Hall", "City Hall", "Fort", "Citadel", "Castle", "Tavern", "Marketplace", "Mage Guild", "Mage Guild II", "Mage Guild III",
                        "Cursed Temple", "Graveyard", "Tomb of Guards", "Mausoleum", "Blood Spire", "Lich Tower", "Dragon Vault", "Alchemist's Crypt"
                    };
                case Faction.Stronghold:
                    return new[]
                    {
                        "Village Hall", "Town Hall", "City Hall", "Fort", "Citadel", "Castle", "Tavern", "Marketplace", "Mage Guild", "Mage Guild II", "Mage Guild III",
                        "Wolf Pens", "Fighting Pit", "Hunters' Lodge", "Spirit Totem", "Berserker Hall", "Bull Pastures", "Dragon Cliffs", "Crystal Cave"
                    };
                default:
                    return new[]
                    {
                        "Village Hall", "Town Hall", "City Hall", "Fort", "Citadel", "Castle", "Tavern", "Marketplace", "Mage Guild", "Mage Guild II", "Mage Guild III",
                        "Guardhouse", "Archery Range", "Barracks", "Chapel", "Crusader Hall", "Stables", "Dragon Spire", "Jeweler"
                    };
            }
        }

        private static Dictionary<(Faction, BuildingId), BuildingDef> Build()
        {
            var table = new Dictionary<(Faction, BuildingId), BuildingDef>();
            foreach (Faction faction in new[] { Faction.Castle, Faction.Necropolis, Faction.Stronghold })
            {
                string[] names = Names(faction);
                ResourceKind rare = Land.RareOf(faction);
                ResourceSet Rare(int gold, int wood, int ore, int rareAmount)
                {
                    var set = new ResourceSet(gold, wood, ore);
                    set[rare] += rareAmount;
                    return set;
                }
                void Add(BuildingId id, ResourceSet cost, string description, params BuildingId[] requires)
                {
                    table[(faction, id)] = new BuildingDef { Id = id, Name = names[(int)id], Cost = cost, Requires = requires, Description = description };
                }

                Add(BuildingId.VillageHall, new ResourceSet(0), "+500 gold a day.");
                Add(BuildingId.TownHall, new ResourceSet(2500), "+1000 gold a day.", BuildingId.Tavern);
                Add(BuildingId.CityHall, new ResourceSet(5000), "+2000 gold a day.", BuildingId.TownHall, BuildingId.Marketplace, BuildingId.MageGuild1);
                Add(BuildingId.Fort, new ResourceSet(3000, 10, 10), "Walls for sieges. Needed for every dwelling.");
                Add(BuildingId.Citadel, new ResourceSet(2500, 0, 5), "+50% creature growth, and an arrow tower in sieges.", BuildingId.Fort);
                Add(BuildingId.Castle, new ResourceSet(5000, 10, 10), "+100% creature growth, and three arrow towers in sieges.", BuildingId.Citadel, BuildingId.Dwelling5);
                Add(BuildingId.Tavern, new ResourceSet(500, 5), "Hire heroes here.");
                Add(BuildingId.Marketplace, new ResourceSet(500, 5), "Trade resources. More marketplaces, better prices.");
                Add(BuildingId.MageGuild1, new ResourceSet(2000, 5, 5), "Visiting heroes learn two first level spells.");
                Add(BuildingId.MageGuild2, Rare(1000, 5, 5, 4), "Two second level spells.", BuildingId.MageGuild1);
                Add(BuildingId.MageGuild3, Rare(1000, 5, 5, 6), "Two third level spells.", BuildingId.MageGuild2);
                Add(BuildingId.Dwelling1, new ResourceSet(800, 5, 5), "Houses the first tier.", BuildingId.Fort);
                Add(BuildingId.Dwelling2, new ResourceSet(1000, 5, 5), "Houses the second tier.", BuildingId.Dwelling1);
                Add(BuildingId.Dwelling3, new ResourceSet(1500, 5, 10), "Houses the third tier.", BuildingId.Dwelling1);
                Add(BuildingId.Dwelling4, Rare(2000, 5, 5, 2), "Houses the fourth tier.", BuildingId.Dwelling3, BuildingId.MageGuild1);
                Add(BuildingId.Dwelling5, Rare(3000, 10, 10, 3), "Houses the fifth tier.", BuildingId.Dwelling2, BuildingId.Dwelling4);
                Add(BuildingId.Dwelling6, Rare(5000, 10, 10, 5), "Houses the sixth tier.", BuildingId.Dwelling5, BuildingId.Citadel);
                Add(BuildingId.Dwelling7, Rare(10000, 10, 20, 10), "Houses the seventh tier.", BuildingId.Dwelling6, BuildingId.TownHall);
                Add(BuildingId.Silo, new ResourceSet(2000, 5, 5), $"+1 {Land.ResourceName(rare)} a day.", BuildingId.Marketplace);
            }
            return table;
        }

        /// <summary>The order the computer players like to build in.</summary>
        public static readonly BuildingId[] AiOrder =
        {
            BuildingId.Fort, BuildingId.Dwelling1, BuildingId.Tavern, BuildingId.Dwelling2, BuildingId.Dwelling3, BuildingId.TownHall,
            BuildingId.MageGuild1, BuildingId.Dwelling4, BuildingId.Marketplace, BuildingId.Citadel, BuildingId.Dwelling5, BuildingId.CityHall,
            BuildingId.Dwelling6, BuildingId.Castle, BuildingId.MageGuild2, BuildingId.Dwelling7, BuildingId.Silo, BuildingId.MageGuild3
        };
    }
}
