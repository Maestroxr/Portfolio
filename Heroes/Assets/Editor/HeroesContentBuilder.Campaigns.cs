using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Heroes.EditorTools
{
    /// <summary>
    /// The second and third campaigns, after the skirmish maps in the list: <i>The Pale Coven</i>, seven chapters played
    /// as the Necropolis, and <i>The Ironjaw Oath</i>, seven played as the Stronghold. Each chapter is won and lost its
    /// own way, and between them they use every kind of victory and loss the rules know. Their maps say more than the
    /// first campaign's: where the homes lie, which land covers the wilds and how much of them, how wooded it is, and
    /// the towns, beasts, relics and buildings the story puts in its places.
    /// </summary>
    internal static partial class HeroesContentBuilder
    {
        private const int CovenArc = 1;
        private const int ClansArc = 2;

        // The heroes the stories are about (entries of the roster, HeroData).
        private const int SirAldric = 0;
        private const int LadyRowena = 1;
        private const int SirGawen = 2;
        private const int FatherAnselm = 4;
        private const int BrotherCaius = 6;
        private const int Malachar = 12;
        private const int Ysolde = 15;
        private const int Grom = 16;
        private const int HaraldAxe = 18;

        /// <summary>The wilds of the map are of <paramref name="land"/>, <paramref name="share"/> zones in a hundred.</summary>
        private static MapSpec Lands(MapSpec spec, TerrainType land, int share)
        {
            spec.theme = 1;
            spec.themeTerrain = land;
            spec.themeShare = share;
            return spec;
        }

        /// <summary>The seat's home lies at <paramref name="x"/>, <paramref name="y"/> (thousandths of the map from the bottom left).</summary>
        private static PlayerSpec Home(PlayerSpec player, int x, int y)
        {
            player.homeX = x;
            player.homeY = y;
            return player;
        }

        /// <summary>The seat's town starts with these buildings as well.</summary>
        private static PlayerSpec Built(PlayerSpec player, params BuildingId[] buildings)
        {
            player.buildings.AddRange(buildings);
            return player;
        }

        /// <summary>The seat starts with this hero of the roster.</summary>
        private static PlayerSpec Led(PlayerSpec player, int hero)
        {
            player.hero = hero;
            return player;
        }

        private static SpecialSpec Town(Faction faction, int x, int y, string tag, int owner = -1, CreatureId garrison = CreatureId.None, int count = 0)
        {
            return new SpecialSpec { kind = SpecialKind.NeutralTown, faction = faction, x = x, y = y, tag = tag, owner = owner, creature = garrison, count = count };
        }

        private static SpecialSpec Visit(ObjectKind building, int x, int y, CreatureId guard = CreatureId.None, int guardCount = 0)
        {
            return new SpecialSpec { kind = SpecialKind.Building, building = building, x = x, y = y, guard = guard, guardCount = guardCount };
        }

        private static SpecialSpec Chest(ArtifactId artifact, int x, int y, CreatureId guard, int guardCount)
        {
            return new SpecialSpec { kind = SpecialKind.Treasure, artifact = artifact, x = x, y = y, guard = guard, guardCount = guardCount };
        }

        private static SpecialSpec Relic(ArtifactId artifact, int x, int y, CreatureId guard, int guardCount, string tag)
        {
            return new SpecialSpec { kind = SpecialKind.Artifact, artifact = artifact, x = x, y = y, guard = guard, guardCount = guardCount, tag = tag };
        }

        private static SpecialSpec Mine(ResourceKind resource, int x, int y, CreatureId guard, int guardCount)
        {
            return new SpecialSpec { kind = SpecialKind.Mine, resource = resource, x = x, y = y, guard = guard, guardCount = guardCount };
        }

        private static SpecialSpec Beast(CreatureId creature, int count, int x, int y, string tag = "")
        {
            return new SpecialSpec { kind = SpecialKind.Monster, creature = creature, count = count, x = x, y = y, tag = tag };
        }

        /// <summary>
        /// <i>The Pale Coven</i>: Ysolde, the last mistress of the coven the knights burned out of the Salt Marches, takes
        /// the fen back, gathers the covens that hid, robs a saint, outlasts a crusade, hunts the witch hunter, makes a
        /// bone dragon of the oldest wyrm in the land and marches on the capital.
        /// </summary>
        private static void PaleCoven(List<HeroesLevel> levels)
        {
            // A small fen of lakes: the hunters' camp across it, a chest in the middle, a tower to look out from.
            MapSpec first = Lands(Map("Still Waters", 1201, 34, 38, 2, 1, 2, 0,
                Led(Player(Faction.Necropolis, true, 0), Ysolde),
                Led(Player(Faction.Castle, false, 1, 0, "The Witch Hunters"), BrotherCaius)), TerrainType.Swamp, 100);
            first.rules.victory = VictoryKind.CaptureTown;
            first.rules.victoryTag = "player:1";
            first.rules.loss = LossKind.LoseHero;
            first.specials.Add(Visit(ObjectKind.Watchtower, 330, 260));
            first.specials.Add(Chest(ArtifactId.CloakOfShadows, 520, 520, CreatureId.Bandit, 14));
            levels.Add(Level(levels.Count, "C2_1_StillWaters", "Still Waters", first,
                "Take the witch hunters' town.",
                "The knights burned the coven's tower and sang while it fell. Ysolde crawled out of the black water with a " +
                "dozen of the dead behind her. The witch hunters still camp at the edge of the fen, waiting to see what else " +
                "comes out.",
                "The hunters' camp is ash, and their dead stand up to serve you. The fen is quiet again, the way you like it.",
                26, 42, false, new Vector2(220f, 600f), CovenArc, "Ysolde must not fall."));

            // The wastes in four quarters: you in one corner, the Order in the other, the two hidden covens in the rest.
            MapSpec second = Lands(Map("The Scattered Covens", 1302, 44, 48, 2, 2, 1, 0,
                Home(Led(Player(Faction.Necropolis, true, 0), Ysolde), 180, 180),
                Home(Player(Faction.Castle, false, 1, 1, "The Holy Order"), 820, 820)), TerrainType.Wasteland, 85);
            second.neutralTowns = 0;
            second.zonesPerPlayer = 2;
            second.rules.victory = VictoryKind.HoldTowns;
            second.rules.victoryValue = 3;
            second.rules.loss = LossKind.TimeLimit;
            second.rules.dayLimit = 70;
            second.specials.Add(Town(Faction.Necropolis, 820, 230, "coven"));
            second.specials.Add(Town(Faction.Necropolis, 230, 800, "coven"));
            second.specials.Add(Visit(ObjectKind.GardenOfRevelation, 520, 480, CreatureId.Hermit, 8));
            levels.Add(Level(levels.Count, "C2_2_ScatteredCovens", "The Scattered Covens", second,
                "Hold three towns at once.",
                "Three covens hid from the purge in three dead towns of the wastes, and each thinks it is the last. Bring them " +
                "under one banner before the Holy Order finds them one by one.",
                "Three covens, one banner. The Order's spies ride home with news nobody there will believe.",
                35, 52, false, new Vector2(360f, 520f), CovenArc, "Win by day 70, when the crusade sets out."));

            // Three realms around the saint's tomb in the middle of the hills, which the abbey's crusaders still guard.
            MapSpec third = Lands(Map("The Saint's Amulet", 1403, 44, 48, 3, 2, 1, 0,
                Led(Player(Faction.Necropolis, true, 0), Ysolde),
                Player(Faction.Castle, false, 1, 1, "Abbey of Saint Ardo"),
                Player(Faction.Stronghold, false, 2, 1, "Bone Pickers")), TerrainType.Dirt, 70);
            third.rules.victory = VictoryKind.FindArtifact;
            third.rules.victoryValue = (int)ArtifactId.AmuletOfTheGrave;
            third.specials.Add(Relic(ArtifactId.AmuletOfTheGrave, 500, 540, CreatureId.Crusader, 14, "amulet"));
            third.specials.Add(Visit(ObjectKind.Temple, 340, 420));
            third.specials.Add(Visit(ObjectKind.TreeOfKnowledge, 660, 620, CreatureId.ElderStag, 6));
            levels.Add(Level(levels.Count, "C2_3_SaintsAmulet", "The Saint's Amulet", third,
                "Find the Amulet of the Grave in the saint's tomb.",
                "Saint Ardo was buried with an amulet that keeps the dead in their graves. In the right hands it does the " +
                "opposite. The abbey still guards his tomb, and the clans have heard there is gold in it.",
                "The amulet is cold to the touch, and the saint is no longer resting. He kneels.",
                40, 60, false, new Vector2(500f, 470f), CovenArc));

            // Winter: your walled town at the bottom, the two armies of the crusade along the top, nothing but snow between.
            MapSpec fourth = Lands(Map("The Long Night", 1504, 44, 48, 2, 2, 0, 0,
                Home(Built(Led(Player(Faction.Necropolis, true, 0), Ysolde), BuildingId.Citadel, BuildingId.TownHall, BuildingId.Dwelling3, BuildingId.Dwelling4), 500, 150),
                Home(Built(Led(Player(Faction.Castle, false, 1, 1, "The Holy Order"), FatherAnselm), BuildingId.Dwelling3), 160, 840),
                Home(Built(Led(Player(Faction.Castle, false, 1, 2, "The King's Marshal"), SirAldric), BuildingId.Dwelling3, BuildingId.Dwelling4), 840, 840)),
                TerrainType.Snow, 100);
            fourth.startingGold = 12000;
            fourth.rules.victory = VictoryKind.Survive;
            fourth.rules.victoryValue = 29;
            fourth.rules.loss = LossKind.LoseTown;
            levels.Add(Level(levels.Count, "C2_4_LongNight", "The Long Night", fourth,
                "Hold out for four weeks.",
                "The crusade has come. Two armies march on the coven's town, and the nights grow long. Hold the walls for four " +
                "weeks, until the winter breaks them for you.",
                "The snow takes what you left of them. The crusade breaks against your walls, and the roads home are long.",
                21, 29, false, new Vector2(640f, 560f), CovenArc, "Your town must not fall."));

            // A land of many zones and woods for a hunt: the witch hunter roams it with the best army of the land.
            MapSpec fifth = Map("The Hunter's Oath", 1605, 46, 50, 3, 2, 1, 0,
                Led(Player(Faction.Necropolis, true, 0), Ysolde),
                Led(Player(Faction.Castle, false, 1, 2, "The Witch Hunters"), SirGawen));
            fifth.zonesPerPlayer = 2;
            fifth.woods = 40;
            fifth.rules.victory = VictoryKind.DefeatHero;
            fifth.rules.victoryTag = "player:1";
            fifth.specials.Add(Visit(ObjectKind.Arena, 480, 520, CreatureId.Swordsman, 20));
            fifth.specials.Add(Chest(ArtifactId.BootsOfSpeed, 300, 640, CreatureId.DireWolf, 16));
            levels.Add(Level(levels.Count, "C2_5_HuntersOath", "The Hunter's Oath", fifth,
                "Defeat Sir Gawen.",
                "Sir Gawen swore on his father's sword to hang the last witch in the land. He has hanged a great many people " +
                "who were not witches. Find him before he finds you.",
                "Sir Gawen hangs nobody any more. His bowmen march for you now, and they never miss twice.",
                36, 55, false, new Vector2(780f, 480f), CovenArc));

            // A deep green forest with the two realms along the bottom and the wyrm asleep at the top of it.
            MapSpec sixth = Lands(Map("The Mosswood Wyrm", 1706, 48, 52, 3, 3, 2, 0,
                Home(Led(Player(Faction.Necropolis, true, 0), Ysolde), 180, 200),
                Home(Player(Faction.Stronghold, false, 1, 2, "The Mosswood Clans"), 820, 200)), TerrainType.Grass, 100);
            sixth.woods = 70;
            sixth.rules.victory = VictoryKind.DefeatMonster;
            sixth.rules.victoryTag = "wyrm";
            sixth.specials.Add(Beast(CreatureId.GreenDragon, 12, 500, 880, "wyrm"));
            sixth.specials.Add(Visit(ObjectKind.StarAxis, 360, 560, CreatureId.ElderStag, 8));
            sixth.specials.Add(Mine(ResourceKind.Mercury, 260, 420, CreatureId.FireFox, 18));
            levels.Add(Level(levels.Count, "C2_6_MosswoodWyrm", "The Mosswood Wyrm", sixth,
                "Slay the green wyrm of the Mosswood.",
                "A bone dragon is only as great as the dragon it was. The oldest wyrm in the land sleeps in the Mosswood, and " +
                "the clans of the wood worship it. Wake it, and kill it.",
                "The wyrm falls in the moss. Before dawn its bones stand up again, and they are yours.",
                45, 66, false, new Vector2(920f, 540f), CovenArc));

            // Four realms in the corners and the walled capital in the middle, held by the king from the start.
            MapSpec seventh = Map("The Pale Crown", 1807, 54, 60, 3, 3, 1, 0,
                Led(Built(Player(Faction.Necropolis, true, 0), BuildingId.Dwelling3), Ysolde),
                Led(Player(Faction.Castle, false, 1, 2, "The King's Host"), LadyRowena),
                Led(Player(Faction.Castle, false, 1, 1, "The Witch Hunters"), SirGawen),
                Led(Player(Faction.Stronghold, false, 2, 2, "The Ironjaw Clan"), HaraldAxe));
            seventh.startingGold = 15000;
            seventh.rules.loss = LossKind.LoseHero;
            seventh.specials.Add(Town(Faction.Castle, 520, 520, "capital", 1, CreatureId.Cavalier, 8));
            levels.Add(Level(levels.Count, "C2_7_PaleCrown", "The Pale Crown", seventh,
                "Defeat every realm that stands against the coven.",
                "The new king sits in the capital under a crown glued back together, and the land is afraid of the dark again. " +
                "Let it be. Ysolde marches on the capital, and nothing that falls on the way stays down.",
                "The bells of the capital ring once, and then never again. Ysolde does not wear the crown. She has no need of it.",
                60, 90, false, new Vector2(1080f, 520f), CovenArc, "Ysolde must not fall."));
        }

        /// <summary>
        /// <i>The Ironjaw Oath</i>: Grom of the ash clans unites the clans the crown war broke: he beats the chief who
        /// says no loudest, fills the war chest, takes the clan halls, finds the axe of the first chief, holds out against
        /// the king and the coven together, takes the Kingsgate and leads one horde against them both.
        /// </summary>
        private static void IronjawOath(List<HeroesLevel> levels)
        {
            // Open ash plains and two chiefs: a duel.
            MapSpec first = Lands(Map("Trial of Chiefs", 1901, 32, 36, 2, 1, 0, 0,
                Led(Player(Faction.Stronghold, true, 0), Grom),
                Led(Player(Faction.Stronghold, false, 1, 0, "The Axe Clan"), HaraldAxe)), TerrainType.Rough, 100);
            first.woods = -60;
            first.rules.victory = VictoryKind.DefeatHero;
            first.rules.victoryTag = "player:1";
            first.rules.loss = LossKind.LoseHero;
            first.specials.Add(Visit(ObjectKind.MercenaryCamp, 480, 460));
            levels.Add(Level(levels.Count, "C3_1_TrialOfChiefs", "Trial of Chiefs", first,
                "Defeat Harald Axe.",
                "The clans lost the war for the crown, and now they fight over its bones. By the old law, a chief who wants the " +
                "clans must first beat the chiefs who say no. Harald Axe says no the loudest.",
                "Harald Axe kneels in the ash and swears the oath. His clan follows him, grumbling. Clans always grumble.",
                22, 36, false, new Vector2(220f, 360f), ClansArc, "Grom must not fall."));

            // Farmland of the border, little gold to start with and two guarded gold mines, a purse in a dragon's keep.
            MapSpec second = Lands(Map("The Iron Tribute", 2002, 42, 46, 3, 2, 1, 0,
                Led(Player(Faction.Stronghold, true, 0), Grom),
                Player(Faction.Castle, false, 1, 1, "The Border Barons")), TerrainType.Dirt, 80);
            second.startingGold = 3000;
            second.rules.victory = VictoryKind.GatherGold;
            second.rules.victoryValue = 40000;
            second.rules.loss = LossKind.TimeLimit;
            second.rules.dayLimit = 84;
            second.specials.Add(Mine(ResourceKind.Gold, 520, 300, CreatureId.Swordsman, 24));
            second.specials.Add(Mine(ResourceKind.Gold, 300, 560, CreatureId.Bandit, 40));
            second.specials.Add(Chest(ArtifactId.GoblinGoldPouch, 640, 620, CreatureId.Cavalier, 8));
            levels.Add(Level(levels.Count, "C3_2_IronTribute", "The Iron Tribute", second,
                "Gather 40,000 gold.",
                "A horde marches on its stomach, and the border barons keep the granaries. Gold buys grain, and grain buys " +
                "loyalty. Fill the war chest before the winter moon.",
                "The war chest is full. For the first time in a generation every clan eats, and every clan listens.",
                50, 70, false, new Vector2(380f, 300f), ClansArc, "Win by day 84."));

            // The hills of the clans: seven halls, yours in one corner, the Bloodmoon's in the other with a second hall.
            MapSpec third = Lands(Map("The Seven Halls", 2103, 50, 54, 2, 3, 1, 0,
                Home(Led(Player(Faction.Stronghold, true, 0), Grom), 180, 180),
                Home(Player(Faction.Stronghold, false, 1, 1, "The Bloodmoon Clan"), 820, 820)), TerrainType.Rough, 80);
            third.neutralTowns = 0;
            third.rules.victory = VictoryKind.HoldTowns;
            third.rules.victoryValue = 5;
            third.specials.Add(Town(Faction.Stronghold, 520, 170, "hall"));
            third.specials.Add(Town(Faction.Stronghold, 170, 540, "hall"));
            third.specials.Add(Town(Faction.Stronghold, 500, 500, "hall", -1, CreatureId.Berserker, 14));
            third.specials.Add(Town(Faction.Stronghold, 470, 840, "hall"));
            third.specials.Add(Town(Faction.Stronghold, 840, 470, "hall", 1));
            levels.Add(Level(levels.Count, "C3_3_SevenHalls", "The Seven Halls", third,
                "Hold five towns at once.",
                "Seven clan halls stand in the hills, and whoever holds most of them speaks for all. The Bloodmoon clan holds " +
                "two already and wants the rest.",
                "Five halls fly the Ironjaw banner. The other two send gifts and say they always meant to.",
                45, 66, false, new Vector2(540f, 380f), ClansArc));

            // A bare red desert: you at the bottom, the coven and the knights at the top corners, the axe between them.
            MapSpec fourth = Lands(Map("The Red Waste", 2204, 46, 50, 3, 3, 0, 0,
                Home(Led(Player(Faction.Stronghold, true, 0), Grom), 500, 150),
                Home(Led(Player(Faction.Necropolis, false, 1, 1, "The Pale Coven"), Malachar), 150, 800),
                Home(Player(Faction.Castle, false, 2, 1, "Knights of the Border"), 850, 800)), TerrainType.Sand, 100);
            fourth.woods = -70;
            fourth.rules.victory = VictoryKind.FindArtifact;
            fourth.rules.victoryValue = (int)ArtifactId.RunedAxe;
            fourth.specials.Add(Relic(ArtifactId.RunedAxe, 500, 720, CreatureId.RedDragon, 5, "axe"));
            fourth.specials.Add(Visit(ObjectKind.MarlettoTower, 300, 400));
            fourth.specials.Add(Visit(ObjectKind.Fountain, 700, 400));
            levels.Add(Level(levels.Count, "C3_4_RedWaste", "The Red Waste", fourth,
                "Find the Runed Axe of the Clans.",
                "Ulfgar the First carried an axe with the oath of the clans cut into its blade. He died in the Red Waste, and " +
                "the red wyrm's brood has slept on his bones ever since. A chief with that axe in his fist needs no other " +
                "argument.",
                "The runes on the blade still burn. Nobody argues with Grom any more.",
                40, 60, false, new Vector2(700f, 320f), ClansArc));

            // Green border hills: your walled town at the bottom, the king and the coven along the top, both strong.
            MapSpec fifth = Lands(Map("Fire on the Walls", 2305, 46, 50, 2, 2, 1, 0,
                Home(Built(Led(Player(Faction.Stronghold, true, 0), Grom), BuildingId.Citadel, BuildingId.TownHall, BuildingId.Dwelling3, BuildingId.Dwelling4), 500, 150),
                Home(Built(Led(Player(Faction.Castle, false, 1, 1, "The King's Host"), LadyRowena), BuildingId.Dwelling3, BuildingId.Dwelling4), 160, 840),
                Home(Built(Led(Player(Faction.Necropolis, false, 1, 2, "The Pale Coven"), Ysolde), BuildingId.Dwelling3), 840, 840)),
                TerrainType.Grass, 80);
            fifth.startingGold = 12000;
            fifth.rules.victory = VictoryKind.Survive;
            fifth.rules.victoryValue = 36;
            fifth.rules.loss = LossKind.LoseTown;
            levels.Add(Level(levels.Count, "C3_5_FireOnTheWalls", "Fire on the Walls", fifth,
                "Hold out for five weeks.",
                "The king has had enough of the clans, and for once the dead agree with him. Two armies march on your town. " +
                "The clans will come, but not before the fifth week is out. Hold until they do.",
                "The clans pour over the hills on the thirty-sixth day, and the besiegers find out how far it is to home.",
                28, 36, false, new Vector2(860f, 380f), ClansArc, "Your town must not fall."));

            // Snowy mountains with the walled Kingsgate in the middle; the king's own town beyond it.
            MapSpec sixth = Lands(Map("The Kingsgate", 2406, 48, 54, 3, 2, 1, 0,
                Home(Led(Player(Faction.Stronghold, true, 0), Grom), 200, 160),
                Home(Led(Player(Faction.Castle, false, 1, 2, "The King's Host"), SirAldric), 800, 840)), TerrainType.Snow, 90);
            sixth.woods = 30;
            sixth.rules.victory = VictoryKind.CaptureTown;
            sixth.rules.victoryTag = "kingsgate";
            sixth.rules.loss = LossKind.TimeLimit;
            sixth.rules.dayLimit = 75;
            sixth.specials.Add(Town(Faction.Castle, 520, 540, "kingsgate", 1, CreatureId.Crusader, 24));
            sixth.specials.Add(Visit(ObjectKind.LearningStone, 340, 380));
            levels.Add(Level(levels.Count, "C3_6_Kingsgate", "The Kingsgate", sixth,
                "Take the Kingsgate.",
                "The Kingsgate is the only road through the mountains to the capital, and the king's best knights hold it. " +
                "Take it before the snows close the passes.",
                "The Kingsgate is yours, and beyond it the capital lies open. The king sends a herald. Grom sends the herald " +
                "back without his trumpet.",
                45, 62, false, new Vector2(1000f, 320f), ClansArc, "Win by day 75."));

            // The whole land: you and Harald along the bottom, the king and the coven, at peace for once, along the top.
            MapSpec seventh = Map("One Horde", 2507, 56, 62, 3, 3, 1, 0,
                Home(Led(Player(Faction.Stronghold, true, 0), Grom), 180, 180),
                Home(Led(Player(Faction.Stronghold, false, 0, 1, "Harald's Axes"), HaraldAxe), 820, 180),
                Home(Led(Player(Faction.Castle, false, 1, 2, "The King's Host"), LadyRowena), 180, 820),
                Home(Led(Player(Faction.Necropolis, false, 1, 2, "The Pale Coven"), Ysolde), 820, 820));
            seventh.startingGold = 15000;
            levels.Add(Level(levels.Count, "C3_7_OneHorde", "One Horde", seventh,
                "Defeat the king and the coven.",
                "The king and the coven have made peace with each other, which shows how afraid they are. Harald Axe rides at " +
                "your side. One horde, one war, and then the clans never kneel again.",
                "The horde rides home with the crown of the realm on a spear. Grom hangs it in his hall, where the children can " +
                "look at it.",
                62, 92, false, new Vector2(1160f, 360f), ClansArc));
        }
    }
}
