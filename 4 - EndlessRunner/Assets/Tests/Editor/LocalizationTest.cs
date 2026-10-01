using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Gamebox.Editor;
using NUnit.Framework;
using UnityEditor;

namespace Portfolio.EndlessRunner.Tests
{
    /// <summary>
    /// The Hebrew of the Endless Runner: every key of the game's table has its Hebrew, every English text the code
    /// looks up (<c>RunnerText.T/F/Say/SayF/Key("...")</c>, <c>Loc.T/F(..., "...")</c>) is a key of the table, and so is
    /// the Night Shift's wording of every sentence the code says in the theme's words.
    /// </summary>
    public class LocalizationTest
    {
        private static readonly Regex Lookup = new Regex(
            @"(?:\b(?:RunnerText\.)?(?:T|F|Key|Say|SayF)|Loc\.[TF])\(\s*(?:[A-Za-z_][A-Za-z0-9_.]*\s*,\s*)?""((?:[^""\\]|\\.)*)""");

        private static readonly Regex Themed = new Regex(
            @"(?:\b(?:RunnerText\.)?(?:Key|Say|SayF))\(\s*(?:[A-Za-z_][A-Za-z0-9_.]*\s*,\s*)?""((?:[^""\\]|\\.)*)""");

        private static string CsvPath
        {
            get
            {
                string path = AssetDatabase.FindAssets("EndlessRunner t:TextAsset")
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .FirstOrDefault(found => found.EndsWith("/Localization/EndlessRunner.csv", StringComparison.Ordinal));
                Assert.IsNotNull(path, "Localization/EndlessRunner.csv is missing");
                return path;
            }
        }

        private static string ScriptsFolder
        {
            get
            {
                string script = AssetDatabase.FindAssets("RunnerText t:MonoScript")
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .FirstOrDefault(found => found.EndsWith("/RunnerText.cs", StringComparison.Ordinal));
                Assert.IsNotNull(script, "RunnerText.cs is missing");
                return Path.GetDirectoryName(Path.GetFullPath(script));
            }
        }

        private static HashSet<string> CsvKeys()
        {
            List<string[]> rows = LocalizationTools.ReadCsv(File.ReadAllText(Path.GetFullPath(CsvPath), Encoding.UTF8));
            return new HashSet<string>(rows.Skip(1).Where(row => row.Length > 0 && !string.IsNullOrEmpty(row[0]) && !row[0].StartsWith("#"))
                .Select(row => row[0]));
        }

        /// <summary>The English texts the runtime sources pass to <paramref name="calls"/>, unescaped.</summary>
        private static List<string> KeysInCode(Regex calls)
        {
            var keys = new List<string>();
            foreach (string file in Directory.GetFiles(ScriptsFolder, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Replace('\\', '/').Contains("/Server/"))
                {
                    continue;
                }
                foreach (Match match in calls.Matches(File.ReadAllText(file)))
                {
                    string key = Regex.Unescape(match.Groups[1].Value);
                    if (!keys.Contains(key))
                    {
                        keys.Add(key);
                    }
                }
            }
            return keys;
        }

        [Test]
        public void EveryKeyHasHebrew()
        {
            List<string> missing = LocalizationTools.MissingWords(CsvPath, "he");
            Assert.IsEmpty(missing, "keys without Hebrew:\n" + string.Join("\n", missing));
        }

        [Test]
        public void EveryTextTheCodeLooksUpIsAKey()
        {
            HashSet<string> csv = CsvKeys();
            List<string> keys = KeysInCode(Lookup);
            Assert.Greater(keys.Count, 50, "the scan found too few texts; did the helper's name change?");
            List<string> missing = keys.Where(key => !csv.Contains(key)).ToList();
            Assert.IsEmpty(missing, "texts missing from the CSV:\n" + string.Join("\n", missing));
        }

        [Test]
        public void EveryNameOfTheGamesDataIsAKey()
        {
            HashSet<string> csv = CsvKeys();
            var words = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:RunnerLevel"))
            {
                var level = AssetDatabase.LoadAssetAtPath<RunnerLevel>(AssetDatabase.GUIDToAssetPath(guid));
                words.Add(level.Title);
                words.Add(level.Description);
            }
            foreach (string guid in AssetDatabase.FindAssets("t:RunnerTheme"))
            {
                words.Add(AssetDatabase.LoadAssetAtPath<RunnerTheme>(AssetDatabase.GUIDToAssetPath(guid)).displayName);
            }
            foreach (string guid in AssetDatabase.FindAssets("t:RunnerGameTheme"))
            {
                var theme = AssetDatabase.LoadAssetAtPath<RunnerGameTheme>(AssetDatabase.GUIDToAssetPath(guid));
                words.Add(theme.DisplayName);
                words.Add(theme.Description);
                words.AddRange(theme.levels.SelectMany(level => new[] { level.title, level.description }));
            }
            List<string> missing = words.Where(word => !string.IsNullOrEmpty(word) && !csv.Contains(word)).Distinct().ToList();
            Assert.IsEmpty(missing, "names and descriptions missing from the CSV:\n" + string.Join("\n", missing));
        }

        [Test]
        public void TheNightShiftWordingOfEverySentenceIsAKey()
        {
            RunnerGameTheme nightShift = AssetDatabase.FindAssets("t:RunnerGameTheme")
                .Select(guid => AssetDatabase.LoadAssetAtPath<RunnerGameTheme>(AssetDatabase.GUIDToAssetPath(guid)))
                .FirstOrDefault(theme => theme != null && theme.IsCalled("Night Shift"));
            Assert.IsNotNull(nightShift, "the Night Shift theme asset is missing");
            HashSet<string> csv = CsvKeys();
            List<string> missing = KeysInCode(Themed)
                .Select(nightShift.Say)
                .Where(said => !csv.Contains(said))
                .ToList();
            Assert.IsEmpty(missing, "Night Shift sentences missing from the CSV:\n" + string.Join("\n", missing));
        }
    }
}
