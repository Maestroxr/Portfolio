using System;
using System.Collections.Generic;
using Gamebox;
using Gamebox.Launcher;
using UnityEditor;
using UnityEngine;

namespace Portfolio.Monopoly.EditorTools
{
    /// <summary>
    /// Writes the game's data: the World Tour board, the settings of every game mode, the modes themselves (the
    /// campaign levels), the campaign and the launcher entry. The rules of the modes live here, so hand edits of those
    /// assets are overwritten by a rebuild.
    /// </summary>
    internal static class MonopolyContentBuilder
    {
        public const string BoardPath = "Config/Board.asset";
        public const string DefaultSettingsPath = "Config/MonopolySettings.asset";
        public const string CampaignPath = "Config/Campaign/MonopolyCampaign.asset";
        public const string DefinitionPath = "Resources/Games/Monopoly.asset";

        private sealed class ModeSpec
        {
            public string file;
            public string title;
            public string tagline;
            public string description;
            public string icon;
            public Color accent;
            public Action<RuleSet> rules;
            public bool custom;
        }

        private static readonly ModeSpec[] Modes =
        {
            new ModeSpec
            {
                file = "MonopolyLevel1", title = "Classic", icon = Icons.House, accent = MonopolyStyle.Red,
                tagline = "The full game. Buy, build, bankrupt everyone.",
                description = "The official rules. Buy what you land on or it goes to auction. Collect rent, doubled on a full color set, " +
                    "and build evenly: the bank has 32 houses and 12 hotels. Mortgage to raise cash, trade to complete sets. " +
                    "Doubles roll again, three doubles go to jail. The last player standing wins.",
                rules = r => { }
            },
            new ModeSpec
            {
                file = "MonopolyLevel2", title = "Speed Die", icon = Icons.Bus, accent = MonopolyStyle.Blue,
                tagline = "The Mega Edition's red die: bus rides and Mr. Monopoly.",
                description = "Once you have passed GO, the red speed die rolls too. 1 to 3 add to your move. Mr. Monopoly moves you on to " +
                    "the next property for sale (or the next rent you owe). The bus lets you move by either die or both. " +
                    "Triples move you anywhere you like!",
                rules = r => r.speedDie = true
            },
            new ModeSpec
            {
                file = "MonopolyLevel3", title = "Quick Deal", icon = Icons.Hourglass, accent = MonopolyStyle.Green,
                tagline = "The short game: two deeds each, hotels at three houses.",
                description = "The official short game. Everyone is dealt two title deeds (and pays for them), hotels need only three " +
                    "houses, and the game ends at the second bankruptcy. Then the richest player wins, counting cash, property " +
                    "and buildings.",
                rules = r =>
                {
                    r.dealtProperties = 2;
                    r.housesForHotel = 3;
                    r.bankruptciesToEnd = 2;
                }
            },
            new ModeSpec
            {
                file = "MonopolyLevel4", title = "House Rules", icon = Icons.Party, accent = MonopolyStyle.ChanceOrange,
                tagline = "Free Parking jackpot, double GO, party cards.",
                description = "The table favourites. Taxes and fines go into a Free Parking jackpot, landing right on GO pays double, " +
                    "party cards (Robin Hood, free houses, a charity gala) join the decks, and there are no auctions: what nobody " +
                    "buys stays with the bank.",
                rules = r =>
                {
                    r.freeParkingJackpot = true;
                    r.jackpotSeed = 100;
                    r.doubleSalaryOnGo = true;
                    r.partyCards = true;
                    r.auctions = false;
                }
            },
            new ModeSpec
            {
                file = "MonopolyLevel5", title = "Tycoon Rush", icon = Icons.Trophy, accent = MonopolyStyle.Gold,
                tagline = "20 rounds, the speed die, $2,000 to start.",
                description = "A race against the clock: twenty rounds with the speed die and $2,000 each to start. When time is up the " +
                    "player with the highest net worth wins, so buy early and build fast.",
                rules = r =>
                {
                    r.roundLimit = 20;
                    r.speedDie = true;
                    r.startingCash = 2000;
                }
            },
            new ModeSpec
            {
                file = "MonopolyLevel6", title = "Custom Rules", icon = Icons.Gear, accent = MonopolyStyle.Hex(0x6B4FBB), custom = true,
                tagline = "Your own mix, set in House Rules.",
                description = "Play with your own rules: turn on custom rules in House Rules on the main menu and choose the starting cash, " +
                    "a round limit, dealt title deeds, auctions, the speed die, the jackpot, double GO, no rent from jail and the " +
                    "party cards."
            }
        };

        public static Board Board => MonopolyAssets.Load<Board>(BoardPath);
        public static MonopolySettings DefaultSettings => MonopolyAssets.Load<MonopolySettings>(DefaultSettingsPath);
        public static MonopolyCampaign Campaign => MonopolyAssets.Load<MonopolyCampaign>(CampaignPath);

        public static void BuildAll()
        {
            Board board = MonopolyAssets.SaveScriptable<Board>(BoardPath, b => b.SetLayout(WorldTourBoard.Create()));
            MonopolySettings defaults = MonopolyAssets.SaveScriptable<MonopolySettings>(DefaultSettingsPath, s =>
            {
                s.Board = board;
                s.SetRules(new RuleSet());
                s.AnimationSpeed = 1f;
                s.BotThinkTime = 0.45f;
            });

            var levels = new List<GameLevel>();
            for (int i = 0; i < Modes.Length; i++)
            {
                ModeSpec spec = Modes[i];
                MonopolySettings settings = null;
                if (!spec.custom)
                {
                    settings = MonopolyAssets.SaveScriptable<MonopolySettings>($"Config/Campaign/Settings/{spec.title.Replace(" ", "")}.asset", s =>
                    {
                        var rules = new RuleSet();
                        spec.rules(rules);
                        s.Board = board;
                        s.SetRules(rules);
                        s.AnimationSpeed = 1f;
                        s.BotThinkTime = 0.45f;
                    });
                }
                int index = i;
                MonopolyLevel level = MonopolyAssets.SaveScriptable<MonopolyLevel>($"Config/Campaign/{spec.file}.asset", l =>
                {
                    l.Configure(settings, spec.tagline, spec.description, spec.icon, spec.accent);
                    MonopolyAssets.SetInt(l, "<Index>k__BackingField", index);
                    MonopolyAssets.SetString(l, "<Title>k__BackingField", spec.title);
                });
                levels.Add(level);
            }
            MonopolyAssets.SaveScriptable<MonopolyCampaign>(CampaignPath, c => MonopolyAssets.SetObjects(c, "<LevelList>k__BackingField", levels.ToArray()));

            var definition = MonopolyAssets.Load<GameDefinition>(DefinitionPath);
            if (definition != null)
            {
                MonopolyAssets.ApplyIfChanged(definition, () =>
                {
                    MonopolyAssets.SetString(definition, "displayName", "Monopoly");
                    MonopolyAssets.SetString(definition, "description",
                        "Roll, buy, build and trade your way around the world. Up to four players at one screen, computer opponents on three levels, and six ways to play.");
                });
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"Monopoly content: board, {levels.Count} modes and the campaign built.");
        }
    }
}
