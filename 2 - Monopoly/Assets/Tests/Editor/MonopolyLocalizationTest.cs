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

namespace Portfolio.Monopoly.Tests
{
    /// <summary>
    /// The Hebrew of the game: every word of the tables has its Hebrew, every key the code asks for is in the table (with
    /// the "you|" and "toyou|" variants the sentences about the player called You use), and the words the game shows from
    /// its data (spaces, cities, cards, modes, themes, tokens, the rules engine's messages) are there too.
    /// </summary>
    public class MonopolyLocalizationTest
    {
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

        /// <summary>The runtime sources of the game: the Scripts folder next to the Localization folder.</summary>
        private static IEnumerable<string> Sources()
        {
            string localization = Path.GetDirectoryName(Path.GetFullPath(CsvPath("Monopoly")));
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
            foreach (string table in new[] { "Monopoly", "MonopolyBoard" })
            {
                List<string> missing = LocalizationTools.MissingWords(CsvPath(table), "he");
                Assert.IsEmpty(missing, $"{table}.csv has no Hebrew for: " + string.Join(" | ", missing));
            }
        }

        [Test]
        public void EveryKeyTheCodeAsksForIsInTheTable()
        {
            HashSet<string> keys = Keys("Monopoly");
            const string literal = "\"((?:[^\"\\\\]|\\\\.)*)\"\\s*[,):]";
            var plain = new Regex(@"(?:\bL\.(?:T|F)\(\s*|\bLoc\.(?:T|F)\(\s*GameType\.Monopoly\s*,\s*)" + literal);
            var say = new Regex(@"\bSay\(\s*[^,""]+,\s*(?:[^,""?]+\?\s*)?" + literal);
            var sayTo = new Regex(@"\bSayTo\(\s*[^,""]+,\s*[^,""]+,\s*" + literal);
            var missing = new List<string>();
            int found = 0;
            foreach (string file in Sources())
            {
                string source = File.ReadAllText(file);
                string name = Path.GetFileName(file);
                foreach (Match m in plain.Matches(source))
                {
                    found++;
                    Need(keys, Unescape(m.Groups[1].Value), name, missing);
                }
                foreach (Match m in say.Matches(source))
                {
                    found++;
                    string key = Unescape(m.Groups[1].Value);
                    Need(keys, key, name, missing);
                    Need(keys, "you|" + key, name, missing);
                }
                foreach (Match m in sayTo.Matches(source))
                {
                    found++;
                    string key = Unescape(m.Groups[1].Value);
                    Need(keys, key, name, missing);
                    Need(keys, "you|" + key, name, missing);
                    Need(keys, "toyou|" + key, name, missing);
                }
            }
            Assert.Greater(found, 150, "the scan found too few keys; did the helpers change?");
            Assert.IsEmpty(missing, "not in Monopoly.csv: " + string.Join(" | ", missing));
        }

        private static void Need(HashSet<string> keys, string key, string file, List<string> missing)
        {
            if (!keys.Contains(key))
            {
                missing.Add($"{key} ({file})");
            }
        }

        [Test]
        public void TheWordsOfTheDataAreInTheTables()
        {
            HashSet<string> keys = Keys("Monopoly");
            HashSet<string> board = Keys("MonopolyBoard");
            var missing = new List<string>();
            BoardLayout layout = WorldTourBoard.Create();
            foreach (SpaceData space in layout.spaces)
            {
                Check(keys, space.name, "space", missing);
                Check(keys, space.city, "city", missing);
                Check(board, space.name.ToUpperInvariant(), "printed name", missing);
                if (space.kind == SpaceKind.Street)
                {
                    Check(board, space.city.ToUpperInvariant(), "printed city", missing);
                }
            }
            foreach (CardData card in layout.chance.Concat(layout.communityChest))
            {
                Check(keys, card.text, "card", missing);
            }
            // The board asset the game plays on holds the same words.
            foreach (string guid in AssetDatabase.FindAssets("t:Board"))
            {
                var asset = AssetDatabase.LoadAssetAtPath<Board>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset == null || asset.Layout == null)
                {
                    continue;
                }
                foreach (SpaceData space in asset.Layout.spaces)
                {
                    Check(keys, space.name, "board asset space", missing);
                }
                foreach (CardData card in asset.Layout.chance.Concat(asset.Layout.communityChest))
                {
                    Check(keys, card.text, "board asset card", missing);
                }
            }
            foreach (string guid in AssetDatabase.FindAssets("t:MonopolyLevel"))
            {
                var mode = AssetDatabase.LoadAssetAtPath<MonopolyLevel>(AssetDatabase.GUIDToAssetPath(guid));
                Check(keys, mode.Title, "mode", missing);
                Check(keys, mode.Tagline, "mode tagline", missing);
                Check(keys, mode.Description, "mode description", missing);
            }
            foreach (string guid in AssetDatabase.FindAssets("t:MonopolyTheme"))
            {
                var theme = AssetDatabase.LoadAssetAtPath<MonopolyTheme>(AssetDatabase.GUIDToAssetPath(guid));
                Check(keys, theme.DisplayName, "theme", missing);
                Check(keys, theme.Description, "theme description", missing);
                Check(keys, theme.editionLabel, "edition", missing);
                Check(board, theme.editionLabel, "printed edition", missing);
                foreach (TokenLook token in theme.tokens)
                {
                    Check(keys, token.name, "token", missing);
                }
            }
            foreach (string guid in AssetDatabase.FindAssets("t:GameDefinition"))
            {
                var definition = AssetDatabase.LoadAssetAtPath<GameDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (definition != null && definition.Type == GameType.Monopoly)
                {
                    Check(keys, definition.DisplayName, "game", missing);
                    Check(keys, definition.Description, "game description", missing);
                }
            }
            Check(keys, MonopolyCampaign.StarRules, "star rules", missing);
            foreach (string name in MatchSetup.BotNames.Append("You"))
            {
                Check(keys, "name|" + name, "player name", missing);
            }
            foreach (string label in new[] { "Time to move", "Computer players", "Computer level" })
            {
                Check(keys, label, "lobby option", missing);
            }
            Assert.IsEmpty(missing, "not in the tables: " + string.Join(" | ", missing));
        }

        private static void Check(HashSet<string> keys, string word, string what, List<string> missing)
        {
            if (!string.IsNullOrEmpty(word) && !keys.Contains(word))
            {
                missing.Add($"{what} \"{word}\"");
            }
        }
    }
}
