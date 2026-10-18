using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Gamebox.Editor;
using NUnit.Framework;
using UnityEditor;

namespace Portfolio.Heroes.Tests
{
    /// <summary>
    /// The Hebrew of Heroes: every word of the game's table has its Hebrew, and every key the code asks the table for
    /// (a literal given to Words.T / Words.F, a shape of the rules' lines in Words.Templates) is in the table, and so is
    /// every word of the campaigns (their names and descriptions, and of every scenario its title, story, goal, how it
    /// is lost and the names of its realms).
    /// </summary>
    public class HeroesLocalizationTests
    {
        private static readonly Regex Literal = new Regex("\"((?:[^\"\\\\]|\\\\.)*)\"");
        private static readonly Regex DirectKey = new Regex("Words\\.(?:T|F)\\(\\s*\"((?:[^\"\\\\]|\\\\.)*)\"");
        private static readonly Regex Conditional = new Regex("Words\\.(?:T|F)\\(([^;\"]*\\?[^;]*?)\\)");

        /// <summary>The game's Assets folder on disk, wherever the project or a package holds it (found by Words.cs).</summary>
        private static string Root
        {
            get
            {
                string script = AssetDatabase.FindAssets("Words t:MonoScript")
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .FirstOrDefault(path => path.EndsWith("/Scripts/Words.cs"));
                Assert.IsNotNull(script, "Words.cs not found");
                string physical = FileUtil.GetPhysicalPath(script).Replace('\\', '/');
                return Path.GetFullPath(physical.Substring(0, physical.Length - "/Scripts/Words.cs".Length));
            }
        }

        private static string CsvPath => Path.Combine(Root, "Localization", "Heroes.csv");

        private static HashSet<string> TableKeys()
        {
            List<string[]> rows = LocalizationTools.ReadCsv(File.ReadAllText(CsvPath));
            return new HashSet<string>(rows.Skip(1).Where(row => row.Length > 0).Select(row => row[0]));
        }

        [Test]
        public void EveryWordHasHebrew()
        {
            List<string> missing = LocalizationTools.MissingWords(CsvPath, "he");
            Assert.IsEmpty(missing, "Keys without Hebrew:\n" + string.Join("\n", missing));
        }

        [Test]
        public void EveryKeyOfTheCodeIsInTheTable()
        {
            HashSet<string> keys = TableKeys();
            var missing = new SortedSet<string>();
            string scripts = Path.Combine(Root, "Scripts");
            foreach (string file in Directory.GetFiles(scripts, "*.cs", SearchOption.AllDirectories))
            {
                string normalized = file.Replace('\\', '/');
                if (normalized.Contains("/Scripts/Model/") || normalized.Contains("/Scripts/Server/"))
                {
                    continue;
                }
                string text = File.ReadAllText(file);
                var found = new List<string>();
                foreach (Match match in DirectKey.Matches(text))
                {
                    found.Add(match.Groups[1].Value);
                }
                // Words.T(a ? "one" : "other"): both sides are keys.
                foreach (Match match in Conditional.Matches(text))
                {
                    foreach (Match literal in Literal.Matches(match.Groups[1].Value))
                    {
                        found.Add(literal.Groups[1].Value);
                    }
                }
                foreach (string raw in found)
                {
                    string key = Regex.Unescape(raw);
                    if (Regex.IsMatch(Regex.Replace(key, "<[^>]*>", ""), "[A-Za-z]") && !keys.Contains(key))
                    {
                        missing.Add($"{key}   ({Path.GetFileName(file)})");
                    }
                }
            }
            foreach (string template in Words.Templates)
            {
                if (!keys.Contains(template))
                {
                    missing.Add($"{template}   (Words.Templates)");
                }
            }
            Assert.IsEmpty(missing, "Keys the code uses that the table lacks:\n" + string.Join("\n", missing));
        }

        [Test]
        public void EveryWordOfTheCampaignsIsInTheTable()
        {
            HashSet<string> keys = TableKeys();
            var missing = new SortedSet<string>();
            void Check(string text, string where)
            {
                if (!string.IsNullOrEmpty(text) && Words.HasLetters(text) && !keys.Contains(text.Trim()))
                {
                    missing.Add($"{text.Trim()}   ({where})");
                }
            }
            foreach (string guid in AssetDatabase.FindAssets("t:HeroesCampaign"))
            {
                var campaign = AssetDatabase.LoadAssetAtPath<HeroesCampaign>(AssetDatabase.GUIDToAssetPath(guid));
                if (campaign == null)
                {
                    continue;
                }
                foreach (HeroesCampaign.Arc arc in campaign.Arcs)
                {
                    Check(arc.title, "a campaign");
                    Check(arc.tab, arc.title);
                    Check(arc.description, arc.title);
                }
                for (int i = 0; i < campaign.Count; i++)
                {
                    HeroesLevel level = campaign.Scenario(i);
                    Check(level.Title, $"scenario {i}");
                    Check(level.Goal, level.Title);
                    Check(level.Loss, level.Title);
                    Check(level.Intro, level.Title);
                    Check(level.Outro, level.Title);
                    foreach (PlayerSpec seat in level.Map.players)
                    {
                        Check(seat.name, level.Title);
                    }
                }
            }
            Assert.IsEmpty(missing, "Words of the campaigns the table lacks:\n" + string.Join("\n", missing));
        }

        [Test]
        public void TranslationsKeepTheirHoles()
        {
            List<string[]> rows = LocalizationTools.ReadCsv(File.ReadAllText(CsvPath));
            int he = System.Array.IndexOf(rows[0].Select(column => column.Trim().ToLowerInvariant()).ToArray(), "he");
            var wrong = new List<string>();
            foreach (string[] row in rows.Skip(1))
            {
                if (row.Length <= he || string.IsNullOrEmpty(row[0]))
                {
                    continue;
                }
                string english = row[0].Contains("|") ? row[0].Substring(row[0].IndexOf('|') + 1) : row[0];
                string Holes(string text) => string.Join(",", Regex.Matches(text, "\\{\\d+\\}").Cast<Match>().Select(m => m.Value).OrderBy(v => v));
                if (Holes(english) != Holes(row[he]))
                {
                    wrong.Add(row[0]);
                }
            }
            Assert.IsEmpty(wrong, "Translations whose {0} holes differ from the English:\n" + string.Join("\n", wrong));
        }
    }
}
