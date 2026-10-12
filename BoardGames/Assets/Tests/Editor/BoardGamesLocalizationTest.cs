using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Gamebox;
using Gamebox.Editor;
using Gamebox.Launcher;
using NUnit.Framework;
using UnityEditor;

namespace Portfolio.BoardGames.Tests
{
    /// <summary>
    /// The Hebrew of the games: every word of the table has its Hebrew, every key the code asks for is in the table (or in
    /// the shared one, whose words the game must not repeat), and the words the games show from their data (the
    /// variants, the rule sets and games, the levels and star rules, the themes, the sides of each game, the launcher's
    /// name and description, the setup and room options, the menu and the settings) are there too.
    /// </summary>
    public class BoardGamesLocalizationTest
    {
        private const string Table = "BoardGames";
        private const string Shared = "Gamebox";

        private static string CsvPath(string table)
        {
            string path = AssetDatabase.FindAssets(table + " t:TextAsset")
                .Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(p => p.EndsWith($"/Localization/{table}.csv"));
            Assert.IsNotNull(path, $"Localization/{table}.csv is missing");
            return path;
        }

        private static HashSet<string> Keys(string table)
        {
            List<string[]> rows = LocalizationTools.ReadCsv(File.ReadAllText(Path.GetFullPath(CsvPath(table)), Encoding.UTF8));
            return new HashSet<string>(rows.Skip(1).Where(r => r.Length > 0 && !r[0].StartsWith("#")).Select(r => r[0]));
        }

        /// <summary>The runtime sources of the game: the Scripts folder next to the Localization folder, without the server bindings.</summary>
        private static IEnumerable<string> Sources()
        {
            string localization = Path.GetDirectoryName(Path.GetFullPath(CsvPath(Table)));
            string scripts = Path.Combine(Path.GetDirectoryName(localization), "Scripts");
            Assert.IsTrue(Directory.Exists(scripts), scripts);
            return Directory.GetFiles(scripts, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Replace('\\', '/').Contains("/Scripts/Server/"));
        }

        private static string Unescape(string literal)
        {
            return literal.Replace("\\\"", "\"").Replace("\\n", "\n").Replace("\\\\", "\\");
        }

        [Test]
        public void EveryWordHasHebrew()
        {
            List<string> missing = LocalizationTools.MissingWords(CsvPath(Table), "he");
            Assert.IsEmpty(missing, $"{Table}.csv has no Hebrew for: " + string.Join(" | ", missing));
        }

        [Test]
        public void TheTableRepeatsNoSharedWord()
        {
            HashSet<string> keys = Keys(Table);
            HashSet<string> shared = Keys(Shared);
            string[] repeated = keys.Where(shared.Contains).OrderBy(k => k).ToArray();
            Assert.IsEmpty(repeated, $"{Table}.csv repeats words of {Shared}.csv (the game falls back to the shared table): " + string.Join(" | ", repeated));
        }

        [Test]
        public void EveryKeyTheCodeAsksForIsInATable()
        {
            HashSet<string> keys = Keys(Table);
            HashSet<string> shared = Keys(Shared);
            const string literal = "\"((?:[^\"\\\\]|\\\\.)*)\"\\s*[,)]";
            var plain = new Regex(@"(?:\bL\.(?:T|F)\(\s*|\bLoc\.(?:T|F)\(\s*GameType\.BoardGames\s*,\s*)" + literal);
            var roomOption = new Regex(@"\bRoomOptionSpec\.\w+\(\s*" + literal);
            var missing = new List<string>();
            int found = 0;
            foreach (string file in Sources())
            {
                string source = File.ReadAllText(file);
                string name = Path.GetFileName(file);
                foreach (Match m in plain.Matches(source).Cast<Match>().Concat(roomOption.Matches(source).Cast<Match>()))
                {
                    found++;
                    Need(keys, shared, Unescape(m.Groups[1].Value), name, missing);
                }
            }
            Assert.Greater(found, 40, "the scan found too few keys; did the helpers change?");
            Assert.IsEmpty(missing, $"neither in {Table}.csv nor in {Shared}.csv: " + string.Join(" | ", missing));
        }

        private static void Need(HashSet<string> keys, HashSet<string> shared, string key, string file, List<string> missing)
        {
            if (!keys.Contains(key) && !shared.Contains(key))
            {
                missing.Add($"{key} ({file})");
            }
        }

        [Test]
        public void TheWordsOfTheDataAreInTheTable()
        {
            HashSet<string> keys = Keys(Table);
            HashSet<string> shared = Keys(Shared);
            var missing = new List<string>();
            foreach (Variant variant in VariantRules.All)
            {
                VariantRules rules = VariantRules.Of(variant);
                Check(keys, shared, rules.NameKey, "variant", missing);
                Check(keys, shared, rules.DescriptionKey, "variant description", missing);
            }
            foreach (RuleSet ruleSet in RuleSets.All)
            {
                Check(keys, shared, RuleSets.NameKey(ruleSet), "rule set", missing);
            }
            foreach (GameMode mode in RuleSets.Modes)
            {
                Check(keys, shared, RuleSets.ModeNameKey(mode), "game", missing);
                Check(keys, shared, BoardGamesCampaign.StarRulesOf(mode), "star rules", missing);
                foreach (Side side in new[] { Side.Light, Side.Dark })
                {
                    Check(keys, shared, Sides.NameKey(side, mode), "side", missing);
                }
            }
            Check(keys, shared, "Campaign: {0}", "campaign window title", missing);
            var local = new BoardGamesLocalRules();
            Check(keys, shared, local.Title, "local play title", missing);
            Check(keys, shared, local.Hint, "local play hint", missing);
            foreach (LocalOption option in local.Options)
            {
                Check(keys, shared, option.Label, "setup option", missing);
                foreach (string value in option.Values)
                {
                    Check(keys, shared, value, $"choice of {option.Label}", missing);
                }
            }
            foreach (string level in local.ComputerLevels)
            {
                Check(keys, shared, level, "computer level", missing);
            }
            Check(keys, shared, BoardGamesCampaign.StarRules, "star rules", missing);
            foreach (string guid in AssetDatabase.FindAssets("t:BoardGamesLevel"))
            {
                var level = AssetDatabase.LoadAssetAtPath<BoardGamesLevel>(AssetDatabase.GUIDToAssetPath(guid));
                Check(keys, shared, level.Title, "level", missing);
                Check(keys, shared, level.Description, "level description", missing);
            }
            foreach (string guid in AssetDatabase.FindAssets("t:BoardGamesTheme"))
            {
                var theme = AssetDatabase.LoadAssetAtPath<BoardGamesTheme>(AssetDatabase.GUIDToAssetPath(guid));
                Check(keys, shared, theme.DisplayName, "theme", missing);
                Check(keys, shared, theme.Description, "theme description", missing);
                foreach (SideLook side in theme.sides)
                {
                    Check(keys, shared, side.nameKey, "side of the theme", missing);
                }
            }
            foreach (string guid in AssetDatabase.FindAssets("t:GameDefinition"))
            {
                var definition = AssetDatabase.LoadAssetAtPath<GameDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (definition != null && definition.Type == GameType.BoardGames)
                {
                    Check(keys, shared, definition.DisplayName, "game", missing);
                    Check(keys, shared, definition.Description, "game description", missing);
                }
            }
            foreach (string label in new[] { "Time to move", "Computer opponent", "Computer level", "Doubling cube" })
            {
                Check(keys, shared, label, "room option", missing);
            }
            foreach (string label in new[] { "Campaign", "Resume", "Save Game", "Load Game", "Settings", "Exit", "Play vs Computer", "Two Players", "Play Online" })
            {
                Check(keys, shared, label, "menu button", missing);
            }
            foreach (string label in new[] { "Show legal moves", "Turn board each move", "Computer thinking time", "Sound volume", "Draughts rules", "Doubling cube", "Computer", "Look" })
            {
                Check(keys, shared, label, "setting", missing);
            }
            Assert.IsEmpty(missing, "not in the tables: " + string.Join(" | ", missing));
        }

        private static void Check(HashSet<string> keys, HashSet<string> shared, string word, string what, List<string> missing)
        {
            if (!string.IsNullOrEmpty(word) && !keys.Contains(word) && !shared.Contains(word))
            {
                missing.Add($"{what} \"{word}\"");
            }
        }
    }
}
