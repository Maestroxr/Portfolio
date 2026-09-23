namespace Portfolio.Heroes
{
    /// <summary>What can stand on the adventure map. The numbers are saved and sent online: only ever append.</summary>
    public enum ObjectKind
    {
        Town = 0,
        Mine = 1,
        Resource = 2,
        Treasure = 3,
        Campfire = 4,
        Artifact = 5,
        Monster = 6,
        Dwelling = 7,
        Shrine = 8,
        MercenaryCamp = 9,
        MarlettoTower = 10,
        StarAxis = 11,
        GardenOfRevelation = 12,
        Arena = 13,
        LearningStone = 14,
        WitchHut = 15,
        Windmill = 16,
        WaterWheel = 17,
        Watchtower = 18,
        Stables = 19,
        Fountain = 20,
        Temple = 21,
        TreeOfKnowledge = 22,
        Scroll = 23
    }

    /// <summary>How the map objects behave when a hero comes by.</summary>
    public static class MapObjects
    {
        /// <summary>Taken by the hero who steps on it, then gone.</summary>
        public static bool IsPickup(ObjectKind kind)
        {
            switch (kind)
            {
                case ObjectKind.Resource:
                case ObjectKind.Treasure:
                case ObjectKind.Campfire:
                case ObjectKind.Artifact:
                case ObjectKind.Scroll:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>A building visited from the next cell: the hero does not stand on it.</summary>
        public static bool IsBuilding(ObjectKind kind)
        {
            return !IsPickup(kind) && kind != ObjectKind.Monster && kind != ObjectKind.Town;
        }

        /// <summary>Gives its bonus once to every hero.</summary>
        public static bool OncePerHero(ObjectKind kind)
        {
            switch (kind)
            {
                case ObjectKind.MercenaryCamp:
                case ObjectKind.MarlettoTower:
                case ObjectKind.StarAxis:
                case ObjectKind.GardenOfRevelation:
                case ObjectKind.Arena:
                case ObjectKind.LearningStone:
                case ObjectKind.WitchHut:
                case ObjectKind.Shrine:
                case ObjectKind.TreeOfKnowledge:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>Gives its bonus once a week to whoever comes first.</summary>
        public static bool Weekly(ObjectKind kind)
        {
            return kind == ObjectKind.Windmill || kind == ObjectKind.WaterWheel;
        }

        public static string Name(ObjectKind kind)
        {
            switch (kind)
            {
                case ObjectKind.Town: return "Town";
                case ObjectKind.Mine: return "Mine";
                case ObjectKind.Resource: return "Resources";
                case ObjectKind.Treasure: return "Treasure Chest";
                case ObjectKind.Campfire: return "Campfire";
                case ObjectKind.Artifact: return "Artifact";
                case ObjectKind.Monster: return "Monsters";
                case ObjectKind.Dwelling: return "Dwelling";
                case ObjectKind.Shrine: return "Shrine of Magic";
                case ObjectKind.MercenaryCamp: return "Mercenary Camp";
                case ObjectKind.MarlettoTower: return "Watchman's Tower";
                case ObjectKind.StarAxis: return "Star Axis";
                case ObjectKind.GardenOfRevelation: return "Garden of Revelation";
                case ObjectKind.Arena: return "Arena";
                case ObjectKind.LearningStone: return "Learning Stone";
                case ObjectKind.WitchHut: return "Witch Hut";
                case ObjectKind.Windmill: return "Windmill";
                case ObjectKind.WaterWheel: return "Water Wheel";
                case ObjectKind.Watchtower: return "Redwood Watchtower";
                case ObjectKind.Stables: return "Stables";
                case ObjectKind.Fountain: return "Fountain of Fortune";
                case ObjectKind.Temple: return "Temple";
                case ObjectKind.TreeOfKnowledge: return "Tree of Knowledge";
                case ObjectKind.Scroll: return "Spell Scroll";
                default: return kind.ToString();
            }
        }

        public static string MineName(ResourceKind kind)
        {
            switch (kind)
            {
                case ResourceKind.Gold: return "Gold Mine";
                case ResourceKind.Wood: return "Sawmill";
                case ResourceKind.Ore: return "Ore Pit";
                case ResourceKind.Mercury: return "Alchemist's Lab";
                case ResourceKind.Sulfur: return "Sulfur Dune";
                case ResourceKind.Crystal: return "Crystal Cavern";
                default: return "Gem Pond";
            }
        }

        /// <summary>What a mine yields a day.</summary>
        public static int MineYield(ResourceKind kind)
        {
            switch (kind)
            {
                case ResourceKind.Gold: return 1000;
                case ResourceKind.Wood:
                case ResourceKind.Ore: return 2;
                default: return 1;
            }
        }

        /// <summary>A short line about what the object gives, for the hover text.</summary>
        public static string Hint(ObjectKind kind)
        {
            switch (kind)
            {
                case ObjectKind.Mine: return "Yields resources every day to its owner.";
                case ObjectKind.Treasure: return "Gold or experience.";
                case ObjectKind.Campfire: return "Gold and a resource.";
                case ObjectKind.Dwelling: return "Recruit creatures every week.";
                case ObjectKind.Shrine: return "Teaches a spell.";
                case ObjectKind.MercenaryCamp: return "+1 Attack for every hero once.";
                case ObjectKind.MarlettoTower: return "+1 Defense for every hero once.";
                case ObjectKind.StarAxis: return "+1 Spell Power for every hero once.";
                case ObjectKind.GardenOfRevelation: return "+1 Knowledge for every hero once.";
                case ObjectKind.Arena: return "+2 Attack or +2 Defense, once.";
                case ObjectKind.LearningStone: return "+1000 experience, once.";
                case ObjectKind.WitchHut: return "Teaches a secondary skill.";
                case ObjectKind.Windmill: return "Rare resources every week.";
                case ObjectKind.WaterWheel: return "Gold every week.";
                case ObjectKind.Watchtower: return "Reveals the land around it.";
                case ObjectKind.Stables: return "+400 movement until the end of the week.";
                case ObjectKind.Fountain: return "+1 Luck until the next battle.";
                case ObjectKind.Temple: return "+1 Morale until the next battle.";
                case ObjectKind.TreeOfKnowledge: return "A free level, once.";
                case ObjectKind.Scroll: return "A spell for the hero's book.";
                default: return "";
            }
        }
    }
}
