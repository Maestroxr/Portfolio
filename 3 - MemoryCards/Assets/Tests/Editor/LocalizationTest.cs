using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Gamebox.Editor;
using NUnit.Framework;

namespace Portfolio.MemoryCards.Tests
{
    /// <summary>
    /// The Hebrew of Memory Cards: every word of its table (Localization/MemoryCards.csv) has a translation, and every key
    /// the runtime code translates (<see cref="MemoryCardsText"/>'s T, F and K, or Loc.T/F with GameType.MemoryCards) is in
    /// the table.
    /// </summary>
    public class LocalizationTest
    {
        private static readonly Regex CodeKey = new Regex(@"\b(?:[TFK]|Loc\.[TF])\(\s*(?:GameType\.MemoryCards\s*,\s*)?""((?:[^""\\]|\\.)*)""");

        /// <summary>The folder of the game's files on disk, in its own project (Assets) or mounted as a package.</summary>
        private static string Root
        {
            get
            {
                var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(MemoryCardsGameManager).Assembly);
                return package != null ? package.resolvedPath : Path.GetFullPath("Assets");
            }
        }

        private static string CsvPath => Path.Combine(Root, "Localization", "MemoryCards.csv");

        [Test]
        public void EveryWordHasHebrew()
        {
            Assert.IsTrue(File.Exists(CsvPath), $"{CsvPath} is missing");
            List<string> missing = LocalizationTools.MissingWords(CsvPath, "he");
            Assert.IsEmpty(missing, "No Hebrew for: " + string.Join(" | ", missing));
        }

        [Test]
        public void EveryKeyOfTheCodeIsInTheTable()
        {
            var table = new HashSet<string>(LocalizationTools.ReadCsv(File.ReadAllText(CsvPath, Encoding.UTF8))
                .Skip(1).Where(row => row.Length > 0).Select(row => row[0]));
            var missing = new List<string>();
            string scripts = Path.Combine(Root, "Scripts");
            foreach (string file in Directory.GetFiles(scripts, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Replace('\\', '/').Contains("/Server/"))
                {
                    continue;
                }
                foreach (Match match in CodeKey.Matches(File.ReadAllText(file)))
                {
                    string key = match.Groups[1].Value.Replace("\\\"", "\"").Replace("\\n", "\n");
                    if (!table.Contains(key) && !missing.Contains(key))
                    {
                        missing.Add($"{key} ({Path.GetFileName(file)})");
                    }
                }
            }
            Assert.Greater(table.Count, 100, "the table looks empty");
            Assert.IsEmpty(missing, "Keys of the code missing from MemoryCards.csv: " + string.Join(" | ", missing));
        }

        [Test]
        public void TheGoalsOfTheModelReadTheSameInEnglish()
        {
            var goals = new StarGoals { maxMistakes = 3, third = StarGoal.Score, thirdTarget = 1500f };
            for (int star = 1; star <= 3; star++)
            {
                if (Gamebox.GameLanguages.IsEnglish)
                {
                    Assert.AreEqual(goals.Describe(star, false), MemoryCardsText.Goal(goals, star, false));
                }
            }
        }
    }
}
