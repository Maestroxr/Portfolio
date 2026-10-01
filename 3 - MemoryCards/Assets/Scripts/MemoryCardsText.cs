using System.Globalization;
using System.Text.RegularExpressions;
using Gamebox;
using UnityEngine;

namespace Portfolio.MemoryCards
{
    /// <summary>
    /// The words Memory Cards shows, in the language picked (see <see cref="Loc"/>): the key of a word is its English, in
    /// the MemoryCards table (Assets/Localization/MemoryCards.csv). The model (rules, goals, versus) is shared with the
    /// server and knows no languages, so what it words is translated here: the star goals, the rule checks and the names
    /// of the card faces.
    /// </summary>
    public static class MemoryCardsText
    {
        /// <summary>The word of <paramref name="key"/> (its English) in the language shown.</summary>
        public static string T(string key)
        {
            return Loc.T(GameType.MemoryCards, key);
        }

        /// <summary>The sentence of <paramref name="key"/> in the language shown with its holes filled.</summary>
        public static string F(string key, params object[] args)
        {
            return Loc.F(GameType.MemoryCards, key, args);
        }

        /// <summary>Marks a key that is translated where it is shown (a table of tips, say); returns it as it is.</summary>
        public static string K(string key)
        {
            return key;
        }

        /// <summary>A whole number with thousands separators, the same in every language.</summary>
        public static string N(int value)
        {
            return value.ToString("N0", CultureInfo.InvariantCulture);
        }

        /// <summary>The text of star 1, 2 or 3 of <paramref name="goals"/> (see <see cref="StarGoals.Describe"/>).</summary>
        public static string Goal(StarGoals goals, int star, bool countdown)
        {
            if (goals == null)
            {
                return string.Empty;
            }
            switch (star)
            {
                case 1:
                    return T("Clear the board");
                case 2:
                    return goals.maxMistakes == 0 ? T("Make no mistakes") : goals.maxMistakes == 1 ? T("At most 1 mistake") : F("At most {0} mistakes", goals.maxMistakes);
                default:
                    int target = Mathf.RoundToInt(goals.thirdTarget);
                    switch (goals.third)
                    {
                        case StarGoal.Time:
                            return countdown ? F("Finish with {0} s left", target) : F("Finish within {0} s", target);
                        case StarGoal.Moves:
                            return F("Finish in {0} moves or less", target);
                        case StarGoal.Combo:
                            return F("Reach a x{0} combo", target);
                        case StarGoal.Score:
                            return F("Score {0} points", N(target));
                        default:
                            return Goal(goals, 2, countdown);
                    }
            }
        }

        /// <summary>The three goals of a level, in the language shown.</summary>
        public static string[] Goals(StarGoals goals, bool countdown)
        {
            return new[] { Goal(goals, 1, countdown), Goal(goals, 2, countdown), Goal(goals, 3, countdown) };
        }

        // The messages of RoundRules.IsValid, as patterns of their English with the numbers as holes.
        private static readonly (Regex pattern, string key)[] RuleMessages =
        {
            (new Regex(@"^Cards per match (\S+) has to be 2, 3 or 4$"), K("Cards per match {0} has to be 2, 3 or 4")),
            (new Regex(@"^Cards per row (\S+) has to be between 1 and (\S+)$"), K("Cards per row {0} has to be between 1 and {1}")),
            (new Regex(@"^Cards (\S+) has to be between (\S+) and (\S+)$"), K("Cards {0} has to be between {1} and {2}")),
            (new Regex(@"^Cards (\S+) has to be a multiple of cards per row (\S+)$"), K("Cards {0} has to be a multiple of cards per row {1}")),
            (new Regex(@"^Special card counts cannot be negative$"), K("Special card counts cannot be negative")),
            (new Regex(@"^(\S+) special cards leave no room for a set of (\S+) animals$"), K("{0} special cards leave no room for a set of {1} animals")),
            (new Regex(@"^The (\S+) animal cards \(cards minus special cards\) have to be a multiple of cards per match (\S+)$"),
                K("The {0} animal cards (cards minus special cards) have to be a multiple of cards per match {1}")),
            (new Regex(@"^The board needs (\S+) different animals but only (\S+) are available$"), K("The board needs {0} different animals but only {1} are available")),
            (new Regex(@"^Frozen cards (\S+) cannot be more than the (\S+) animal cards$"), K("Frozen cards {0} cannot be more than the {1} animal cards")),
            (new Regex(@"^Times cannot be negative$"), K("Times cannot be negative")),
            (new Regex(@"^Unflip delay (\S+) has to be 5 seconds or less$"), K("Unflip delay {0} has to be 5 seconds or less")),
            (new Regex(@"^Memorize time (\S+) has to be 30 seconds or less$"), K("Memorize time {0} has to be 30 seconds or less")),
            (new Regex(@"^Hearts, moves and shuffle cannot be negative$"), K("Hearts, moves and shuffle cannot be negative")),
            (new Regex(@"^A move limit of (\S+) is too low to clear (\S+) sets$"), K("A move limit of {0} is too low to clear {1} sets")),
        };

        /// <summary>
        /// A message of the rule checks of the model (<see cref="RoundRules.IsValid"/>) in the language shown; any other
        /// message is looked up as it is.
        /// </summary>
        public static string RuleMessage(string english)
        {
            if (string.IsNullOrEmpty(english) || GameLanguages.IsEnglish)
            {
                return english;
            }
            foreach ((Regex pattern, string key) in RuleMessages)
            {
                Match match = pattern.Match(english);
                if (!match.Success)
                {
                    continue;
                }
                var holes = new object[match.Groups.Count - 1];
                for (int i = 1; i < match.Groups.Count; i++)
                {
                    holes[i - 1] = match.Groups[i].Value;
                }
                return F(key, holes);
            }
            return T(english);
        }

        /// <summary>The name of a card face (its sprite's name, "polar bear" for polar_bear) in the language shown.</summary>
        public static string FaceName(Sprite face, string fallback)
        {
            if (face == null || string.IsNullOrEmpty(face.name))
            {
                return fallback;
            }
            return T(face.name.Replace('_', ' ').ToLowerInvariant());
        }
    }
}
