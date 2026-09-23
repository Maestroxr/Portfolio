using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Portfolio.Heroes.EditorTools
{
    /// <summary>
    /// The content of the game as assets: the default settings, the eight chapters of the campaign and the skirmish
    /// maps that can also be played online. Every scenario is a <see cref="MapSpec"/> the generator lays out the same
    /// way every time from its seed, so a chapter is the same land for everybody who plays it.
    /// </summary>
    internal static class HeroesContentBuilder
    {
        private const string SettingsPath = "Content/HeroesSettings.asset";
        private const string CampaignPath = "Content/HeroesCampaign.asset";

        public static HeroesSettings DefaultSettings => HeroesAssets.Load<HeroesSettings>(SettingsPath);

        public static HeroesCampaign Campaign => HeroesAssets.Load<HeroesCampaign>(CampaignPath);

        [MenuItem("Heroes/Rebuild Content", false, 21)]
        public static void BuildAll()
        {
            HeroesSettings settings = Settings();
            List<HeroesLevel> levels = Scenarios();
            HeroesCampaign campaign = HeroesAssets.Load<HeroesCampaign>(CampaignPath);
            if (campaign == null)
            {
                campaign = ScriptableObject.CreateInstance<HeroesCampaign>();
                HeroesAssets.EnsureFolderOf(CampaignPath);
                AssetDatabase.CreateAsset(campaign, HeroesAssets.Path(CampaignPath));
            }
            campaign.Configure("The Shattered Crown", new List<Gamebox.GameLevel>(levels));
            EditorUtility.SetDirty(campaign);
            AssetDatabase.SaveAssets();
            Debug.Log($"Heroes: content built ({levels.Count} scenarios, settings {(settings != null ? "ok" : "missing")}).");
        }

        private static HeroesSettings Settings()
        {
            HeroesSettings settings = HeroesAssets.Load<HeroesSettings>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<HeroesSettings>();
                HeroesAssets.EnsureFolderOf(SettingsPath);
                AssetDatabase.CreateAsset(settings, HeroesAssets.Path(SettingsPath));
            }
            EditorUtility.SetDirty(settings);
            return settings;
        }

        // ------------------------------------------------------------------ the scenarios

        private static HeroesLevel Level(int index, string file, string title, MapSpec map, string goal, string intro,
            string outro, int threeStars, int twoStars, bool skirmish, Vector2 position)
        {
            string path = $"Content/Scenarios/{file}.asset";
            HeroesLevel level = HeroesAssets.Load<HeroesLevel>(path);
            if (level == null)
            {
                level = ScriptableObject.CreateInstance<HeroesLevel>();
                HeroesAssets.EnsureFolderOf(path);
                AssetDatabase.CreateAsset(level, HeroesAssets.Path(path));
            }
            level.name = file;
            // Index and title belong to the base class, which keeps them to itself.
            HeroesAssets.SetString(level, "<Title>k__BackingField", title);
            HeroesAssets.SetInt(level, "<Index>k__BackingField", index);
            level.Configure(map, goal, intro, outro, threeStars, twoStars, skirmish, position);
            EditorUtility.SetDirty(level);
            return level;
        }

        private static PlayerSpec Player(Faction faction, bool human, int team, int aiLevel = 1, string name = "")
        {
            return new PlayerSpec
            {
                faction = faction,
                human = human,
                team = team,
                aiLevel = aiLevel,
                name = string.IsNullOrEmpty(name) ? (human ? "You" : Land.FactionName(faction)) : name
            };
        }

        private static MapSpec Map(string name, uint seed, int columns, int rows, int treasure, int monsters,
            int water, int theme, params PlayerSpec[] players)
        {
            var spec = new MapSpec
            {
                name = name,
                seed = seed,
                columns = columns,
                rows = rows,
                treasure = treasure,
                monsters = monsters,
                water = water,
                theme = theme,
                neutralTowns = players.Length > 2 ? 2 : 1,
                startingGold = 10000
            };
            spec.players.AddRange(players);
            return spec;
        }

        /// <summary>
        /// The campaign, in eight chapters: a lord driven from his own lands who takes them back, learns what tore the
        /// crown apart, and stands against what the necromancers woke. Four skirmish maps follow it.
        /// </summary>
        private static List<HeroesLevel> Scenarios()
        {
            var levels = new List<HeroesLevel>();

            MapSpec first = Map("The Long Road Home", 101, 34, 38, 2, 1, 0, 0,
                Player(Faction.Castle, true, 0),
                Player(Faction.Stronghold, false, 1, 0, "The Ironjaw Clan"));
            first.rules.victory = VictoryKind.DefeatAll;
            levels.Add(Level(0, "01_LongRoadHome", "The Long Road Home", first,
                "Drive the Ironjaw raiders out of the valley.",
                "The raiders came in the spring and took the valley while the King's armies were away. You have a town, " +
                "a handful of militia, and a road that leads home.",
                "The valley is yours again. Word comes from the capital: the King is dead, and the crown is broken.",
                28, 45, false, new Vector2(240f, 640f)));

            MapSpec second = Map("The Salt Marches", 202, 38, 42, 2, 2, 2, 4,
                Player(Faction.Castle, true, 0),
                Player(Faction.Necropolis, false, 1, 1, "The Pale Coven"));
            second.rules.victory = VictoryKind.CaptureTown;
            levels.Add(Level(1, "02_SaltMarches", "The Salt Marches", second,
                "Take the coven's town in the marshes.",
                "Something in the marshes raises the drowned. Follow the road east and put an end to whoever is calling them.",
                "The coven's tower falls. In its cellar you find a map of the old barrows.",
                34, 52, false, new Vector2(400f, 560f)));

            MapSpec third = Map("The Barrow Fields", 303, 40, 44, 3, 2, 1, 6,
                Player(Faction.Castle, true, 0),
                Player(Faction.Necropolis, false, 1, 1, "Barrow Wardens"),
                Player(Faction.Stronghold, false, 2, 1, "Grave Diggers"));
            third.rules.victory = VictoryKind.FindArtifact;
            third.rules.victoryValue = (int)ArtifactId.CrownOfDragons;
            third.specials.Add(new SpecialSpec
            {
                kind = SpecialKind.Artifact,
                artifact = ArtifactId.CrownOfDragons,
                x = 800,
                y = 500,
                guard = CreatureId.BoneDragon,
                guardCount = 6,
                tag = "crown"
            });
            levels.Add(Level(2, "03_BarrowFields", "The Barrow Fields", third,
                "Find the Crown of Dragons in the barrows.",
                "The old kings were buried with their crowns. One of them is worth more than the rest, and you are not " +
                "the only one digging.",
                "The Crown of Dragons is yours. It is heavier than it looks.",
                40, 60, false, new Vector2(560f, 500f)));

            MapSpec fourth = Map("Gold and Ashes", 404, 42, 46, 3, 2, 1, 1,
                Player(Faction.Castle, true, 0),
                Player(Faction.Stronghold, false, 1, 2, "The Ironjaw Clan"));
            fourth.rules.victory = VictoryKind.GatherGold;
            fourth.rules.victoryValue = 60000;
            fourth.startingGold = 4000;
            levels.Add(Level(3, "04_GoldAndAshes", "Gold and Ashes", fourth,
                "Raise 60,000 gold to pay an army.",
                "An army costs gold, and the mines of the east are held by the clans. Take them, work them, and pay your men.",
                "The chests are full. The mercenaries will follow you south.",
                45, 65, false, new Vector2(700f, 540f)));

            MapSpec fifth = Map("The Hundred Isles", 505, 44, 46, 3, 3, 3, 2,
                Player(Faction.Castle, true, 0),
                Player(Faction.Necropolis, false, 1, 2, "The Drowned"),
                Player(Faction.Stronghold, false, 2, 1, "Reavers"));
            levels.Add(Level(4, "05_HundredIsles", "The Hundred Isles", fifth,
                "Clear the isles of both claimants.",
                "Two fleets and one narrow sea. Whoever holds the isles holds the road to the capital.",
                "The isles are quiet. The road to the capital lies open.",
                48, 70, false, new Vector2(840f, 480f)));

            MapSpec sixth = Map("Winter at the Gate", 606, 44, 48, 2, 3, 0, 3,
                Player(Faction.Castle, true, 0),
                Player(Faction.Necropolis, false, 1, 2, "The Pale Coven"),
                Player(Faction.Stronghold, false, 1, 2, "Ironjaw Remnant"));
            levels.Add(Level(5, "06_WinterAtTheGate", "Winter at the Gate", sixth,
                "Hold the pass against both armies.",
                "Snow closes the pass in a month. Until then, everything that wants the capital has to come through you.",
                "The snow closes the pass behind the last of them.",
                50, 72, false, new Vector2(960f, 560f)));

            MapSpec seventh = Map("The Dragon's Debt", 707, 46, 50, 3, 3, 1, 5,
                Player(Faction.Castle, true, 0),
                Player(Faction.Stronghold, false, 1, 2, "Dragon Cliffs"));
            seventh.rules.victory = VictoryKind.DefeatMonster;
            seventh.rules.victoryTag = "wyrm";
            seventh.specials.Add(new SpecialSpec
            {
                kind = SpecialKind.Monster,
                creature = CreatureId.RedDragon,
                count = 14,
                x = 850,
                y = 150,
                tag = "wyrm"
            });
            levels.Add(Level(6, "07_DragonsDebt", "The Dragon's Debt", seventh,
                "Slay the red wyrm of the cliffs.",
                "The clans keep a wyrm, and the wyrm keeps the cliffs. Both debts come due today.",
                "The wyrm is dead. Its hoard pays for the last march.",
                52, 75, false, new Vector2(1080f, 500f)));

            MapSpec eighth = Map("The Shattered Crown", 808, 48, 54, 3, 3, 1, 0,
                Player(Faction.Castle, true, 0),
                Player(Faction.Necropolis, false, 1, 2, "The Pale Coven"),
                Player(Faction.Stronghold, false, 1, 2, "The Ironjaw Clan"),
                Player(Faction.Castle, false, 1, 2, "The Pretender"));
            eighth.startingGold = 15000;
            levels.Add(Level(7, "08_ShatteredCrown", "The Shattered Crown", eighth,
                "Defeat everyone who claims the crown.",
                "Three claimants, one capital, and a crown in pieces. Whoever is left standing at the end of it wears them all.",
                "The crown is whole, and heavy, and yours.",
                60, 90, false, new Vector2(1200f, 560f)));

            // The skirmish maps, which are also what an online room plays.
            levels.Add(Level(8, "S1_RiverCrossing", "River Crossing",
                Map("River Crossing", 911, 36, 40, 2, 2, 2, 0,
                    Player(Faction.Castle, true, 0),
                    Player(Faction.Necropolis, false, 1)),
                "Defeat your rival.", "", "", 30, 50, true, new Vector2(300f, 300f)));
            levels.Add(Level(9, "S2_ThreeCrowns", "Three Crowns",
                Map("Three Crowns", 922, 42, 46, 2, 2, 1, 1,
                    Player(Faction.Castle, true, 0),
                    Player(Faction.Necropolis, false, 1),
                    Player(Faction.Stronghold, false, 2)),
                "Outlast two rivals.", "", "", 40, 60, true, new Vector2(500f, 300f)));
            levels.Add(Level(10, "S3_HighPasses", "The High Passes",
                Map("The High Passes", 933, 40, 44, 3, 3, 0, 3,
                    Player(Faction.Stronghold, true, 0),
                    Player(Faction.Castle, false, 1)),
                "Take the passes.", "", "", 35, 55, true, new Vector2(700f, 300f)));
            levels.Add(Level(11, "S4_FourWinds", "Four Winds",
                Map("Four Winds", 944, 48, 52, 3, 3, 2, 2,
                    Player(Faction.Castle, true, 0),
                    Player(Faction.Necropolis, false, 1),
                    Player(Faction.Stronghold, false, 2),
                    Player(Faction.Castle, false, 3)),
                "Be the last to hold a town.", "", "", 45, 70, true, new Vector2(900f, 300f)));
            return levels;
        }
    }
}
