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
using UnityEngine;

namespace Portfolio.Asteroids.Tests
{
    /// <summary>
    /// The Hebrew of Asteroids: every word of the table has its Hebrew, every English text the code shows through
    /// <see cref="AsteroidsText"/> (or <c>Loc.T/F(GameType.Asteroids, ...)</c>) is in the table, and so is every word of the
    /// game's data that the screens show (missions, sectors, ships, bosses, pickups, the armory, themes).
    /// </summary>
    public class LocalizationTest
    {
        private static readonly Regex CodeKey = new Regex(
            @"(?:(?<![\w.])[TF]|Loc\.[TF]\(\s*GameType\.Asteroids\s*,)\(?\s*""((?:[^""\\]|\\.)*)""", RegexOptions.Compiled);

        /// <summary>The game's folder (Assets in its own project, the package folder in BaseGame), on disk.</summary>
        private static string GameRoot
        {
            get
            {
                string guid = AssetDatabase.FindAssets("AsteroidsText t:MonoScript").First();
                string script = AssetDatabase.GUIDToAssetPath(guid);
                string assetRoot = script.Substring(0, script.IndexOf("/Scripts/", System.StringComparison.Ordinal));
                return FileUtil.GetPhysicalPath(assetRoot);
            }
        }

        private static string CsvPath => Path.GetFullPath(Path.Combine(GameRoot, "Localization", "Asteroids.csv"));

        private static HashSet<string> TableKeys()
        {
            List<string[]> rows = LocalizationTools.ReadCsv(File.ReadAllText(CsvPath, Encoding.UTF8));
            return new HashSet<string>(rows.Skip(1).Where(row => row.Length > 0 && !row[0].StartsWith("#")).Select(row => row[0]));
        }

        [Test]
        public void EveryWordHasItsHebrew()
        {
            Assert.That(File.Exists(CsvPath), Is.True, CsvPath);
            List<string> missing = LocalizationTools.MissingWords(CsvPath, "he");
            Assert.That(missing, Is.Empty, "Words without Hebrew: " + string.Join(" | ", missing));
        }

        [Test]
        public void EveryWordOfTheCodeIsInTheTable()
        {
            HashSet<string> keys = TableKeys();
            var missing = new List<string>();
            foreach (string file in Directory.GetFiles(Path.Combine(GameRoot, "Scripts"), "*.cs", SearchOption.AllDirectories))
            {
                foreach (string line in File.ReadAllLines(file))
                {
                    if (line.TrimStart().StartsWith("//"))
                    {
                        continue;
                    }
                    foreach (Match match in CodeKey.Matches(line))
                    {
                        string key = Regex.Unescape(match.Groups[1].Value);
                        if (!keys.Contains(key))
                        {
                            missing.Add($"{Path.GetFileName(file)}: {key}");
                        }
                    }
                }
            }
            Assert.That(missing, Is.Empty, "Code words missing from Asteroids.csv: " + string.Join(" | ", missing));
        }

        [Test]
        public void EveryWordOfTheDataIsInTheTable()
        {
            HashSet<string> keys = TableKeys();
            var words = new List<string>();
            foreach (AsteroidsCampaign campaign in Assets<AsteroidsCampaign>())
            {
                for (int i = 0; i < campaign.SectorCount; i++)
                {
                    words.Add(campaign.GetSector(i).title);
                    if (campaign.GetSector(i).theme != null)
                    {
                        words.Add(campaign.GetSector(i).theme.Title);
                    }
                }
            }
            foreach (AsteroidsLevel level in Assets<AsteroidsLevel>())
            {
                if (level.name.StartsWith("Tour"))
                {
                    continue;
                }
                words.Add(level.Title);
                words.Add(level.Description);
                words.Add(level.Introduces);
                words.AddRange(level.Hints);
                words.AddRange(level.touchHints ?? new string[0]);
                words.AddRange(level.Waves.Select(wave => wave.title));
            }
            words.AddRange(Assets<SectorTheme>().Select(theme => theme.Title));
            words.AddRange(Assets<StrikeTheme>().Select(theme => theme.Title));
            foreach (PlayerSettings hull in Assets<PlayerSettings>())
            {
                words.Add(hull.DisplayName);
                words.Add(hull.Description);
            }
            foreach (GameObject prefab in Assets<GameObject>())
            {
                if (prefab.TryGetComponent(out Boss boss))
                {
                    words.Add(boss.DisplayName);
                }
                if (prefab.TryGetComponent(out Reward reward) && !(reward is PointReward) && !reward.Title.StartsWith("+"))
                {
                    words.Add(reward.Title);
                }
            }
            foreach (GameTheme theme in Assets<AsteroidsTheme>())
            {
                words.Add(theme.DisplayName);
                words.Add(theme.Description);
            }
            foreach (GameDefinition game in Assets<GameDefinition>())
            {
                words.Add(game.DisplayName);
                words.Add(game.Description);
            }
            foreach (WeaponType weapon in System.Enum.GetValues(typeof(WeaponType)))
            {
                words.Add(WeaponRules.Title(weapon));
                words.Add(WeaponRules.ShortTitle(weapon));
            }
            foreach (PowerUpType powerUp in System.Enum.GetValues(typeof(PowerUpType)))
            {
                words.Add(PowerUps.Title(powerUp));
            }
            foreach (StrikeItem item in System.Enum.GetValues(typeof(StrikeItem)))
            {
                StrikeItemInfo info = StrikeArmory.Info(item);
                words.Add(info.Title);
                words.Add(info.Description);
                if (item != StrikeItem.PowerDisrupter)
                {
                    words.Add(StrikeUI.ShortName(item));
                }
            }
            words.AddRange(CoopRules.DifficultyLabels);
            words.Add("DEEP FIELD");
            words.Add("the boss");
            words.Add("Ships per pilot");
            words.Add("Strike difficulty");
            Assert.That(words.Count, Is.GreaterThan(100), "The data was found.");
            List<string> missing = words.Where(word => !string.IsNullOrWhiteSpace(word) && !keys.Contains(word)).Distinct().ToList();
            Assert.That(missing, Is.Empty, "Data words missing from Asteroids.csv: " + string.Join(" | ", missing));
        }

        /// <summary>Every asset of type <typeparamref name="T"/> in the game's folder.</summary>
        private static IEnumerable<T> Assets<T>() where T : Object
        {
            string guid = AssetDatabase.FindAssets("AsteroidsText t:MonoScript").First();
            string script = AssetDatabase.GUIDToAssetPath(guid);
            string assetRoot = script.Substring(0, script.IndexOf("/Scripts/", System.StringComparison.Ordinal));
            string filter = typeof(T) == typeof(GameObject) ? "t:Prefab" : "t:" + typeof(T).Name;
            foreach (string found in AssetDatabase.FindAssets(filter, new[] { assetRoot }))
            {
                T asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(found));
                if (asset != null)
                {
                    yield return asset;
                }
            }
        }
    }
}
