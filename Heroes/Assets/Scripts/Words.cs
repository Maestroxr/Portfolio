using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Gamebox;

namespace Portfolio.Heroes
{
    /// <summary>
    /// The words of Heroes in the language shown, from the game's string table (see <see cref="Loc"/>: a key is the
    /// English text). The rules (<c>Skinnerboxes.Heroes.Model</c>) stay in English, since they also run on the server
    /// and in plain .NET tests: the names they hold (creatures, buildings, spells...) are translated where they are
    /// shown, and the lines they write (what happened, why a command is refused) by <see cref="Sentence"/>, which knows
    /// their English shapes (<see cref="Templates"/>) and translates each shape as a whole, its names inside it too.
    /// </summary>
    public static class Words
    {
        public const GameType Game = GameType.Heroes;

        /// <summary>A word of the game in the language shown (its English when there is no translation).</summary>
        public static string T(string key)
        {
            if (string.IsNullOrEmpty(key) || GameLanguages.IsEnglish || !HasLetters(key))
            {
                return Loc.English(key);
            }
            if (IsTranslated(key))
            {
                // Words code already put into the language shown.
                return key;
            }
            return Loc.T(Game, key);
        }

        /// <summary>A sentence of the game in the language shown, with its <c>{0}</c> holes filled.</summary>
        public static string F(string key, params object[] args)
        {
            return Loc.F(Game, key, args);
        }

        /// <summary>
        /// A name (of a creature, a hero, a town, a realm...) in the language shown; a name the table does not know (a
        /// player's own) stays as it is.
        /// </summary>
        public static string Name(string name)
        {
            return T(name);
        }

        /// <summary>
        /// The English shapes of the lines the rules write (the text of their events, the reasons they give), each a key
        /// of the table: <see cref="Sentence"/> matches a line against them. Numbers and names fill the holes.
        /// </summary>
        public static readonly string[] Templates =
        {
            // Battles
            "The armies cannot get at each other: {0} breaks off the fight.",
            "{0} retreats from the field.",
            "{0} is defeated.",
            "{0} Skeletons rise to join {1}.",
            // Spells in battle
            "No hero to cast.",
            "The hero does not know it.",
            "One spell a round.",
            "Not enough mana.",
            "Wait for your troops' turn.",
            // Heroes
            "{0} gains {1} experience.",
            "{0} reaches level {1}: +1 {2}.",
            "{0}: Basic {1}",
            "{0}: Advanced {1}",
            "{0}: Expert {1}",
            "{0} finds the {1} ({2}).",
            "{0}: +{1} {2}",
            // The map
            "{0} meets {1}.",
            "{0} enters {1}.",
            "{0} Gold and {1} {2}",
            "{0} Gold",
            "{0} {1}",
            "{0} learns {1}.",
            "{0} has already visited the {1}.",
            "The {0} now flies your flag.",
            "{0} for hire: {1}.",
            "{0} already knows {1}.",
            "{0} lacks the wisdom to learn {1}.",
            "The witch has nothing to teach {0}.",
            "{0} learns Basic {1}.",
            "The {0} is empty until next week.",
            "From the tower the land lies open.",
            "Fresh horses: +400 movement this week.",
            "+1 Luck until the next battle.",
            "+1 Morale until the next battle.",
            // Towns
            "There is no such building.",
            "Not your town.",
            "Already built.",
            "One building a day.",
            "Requires {0}.",
            "Not enough resources.",
            "{0} built in {1}.",
            "{0} joins your cause.",
            "{0} now belongs to {1}.",
            "{0} stands abandoned.",
            // Turns
            "{0} has been defeated.",
            "{0} is victorious!",
            "Nobody is left standing.",
            "{0} is now played by the computer.",
            // Lines the view writes in the chronicle (HeroesGameManager), kept in English there
            "The {0} now flies the flag of {1}.",
            "{0} fights {1}.",
            "{0} wins the battle.",
            "{0} holds the field.",
            "{0} retreats from the battle.",
            "a band of {0}",
            // Online (HeroesGameManager.Online)
            "Time is up for {0}.",
            "{0} left. The computer plays on.",
            "The game was called off.",
            "Seat {0}",
        };

        private sealed class Shape
        {
            public string Key;
            public Regex Pattern;
            public int Literal;
        }

        private static List<Shape> shapes;
        private static readonly Dictionary<string, string> Made = new Dictionary<string, string>();
        private static string madeIn;

        /// <summary>
        /// A line the rules wrote in English, in the language shown: the most specific of <see cref="Templates"/> that
        /// matches it, its holes translated in turn (a name, a resource, another shape), else the line as a key itself.
        /// </summary>
        public static string Sentence(string english)
        {
            if (string.IsNullOrEmpty(english) || GameLanguages.IsEnglish)
            {
                return english;
            }
            if (madeIn != GameLanguages.Code)
            {
                Made.Clear();
                madeIn = GameLanguages.Code;
            }
            if (Made.TryGetValue(english, out string known))
            {
                return known;
            }
            string result = Translate(english, 0);
            Made[english] = result;
            return result;
        }

        /// <summary>Several lines of the rules (a line per event joined by line breaks), each through <see cref="Sentence"/>.</summary>
        public static string Lines(string english)
        {
            if (string.IsNullOrEmpty(english) || GameLanguages.IsEnglish || english.IndexOf('\n') < 0)
            {
                return Sentence(english);
            }
            string[] lines = english.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = Sentence(lines[i]);
            }
            return string.Join("\n", lines);
        }

        private static string Translate(string text, int depth)
        {
            Shape best = null;
            Match bestMatch = null;
            foreach (Shape shape in Shapes())
            {
                if (best != null && shape.Literal <= best.Literal)
                {
                    continue;
                }
                Match match = shape.Pattern.Match(text);
                // A shape of hardly any words of its own ("{0} {1}": an amount and a resource) needs a number first.
                if (match.Success && (shape.Literal >= 3 || !HasLetters(match.Groups[1].Value)))
                {
                    best = shape;
                    bestMatch = match;
                }
            }
            if (best == null)
            {
                return T(text);
            }
            var args = new object[bestMatch.Groups.Count - 1];
            for (int i = 1; i < bestMatch.Groups.Count; i++)
            {
                args[i - 1] = Piece(bestMatch.Groups[i].Value, depth);
            }
            return F(best.Key, args);
        }

        /// <summary>
        /// A hole of a shape: a number stays, a name (a hero, a creature, a resource...) is translated, and so is a
        /// phrase of a shape of its own ("a band of Wolves").
        /// </summary>
        private static string Piece(string piece, int depth)
        {
            if (!HasLetters(piece))
            {
                return piece;
            }
            return depth < 2 ? Translate(piece, depth + 1) : T(piece);
        }

        private static List<Shape> Shapes()
        {
            if (shapes != null)
            {
                return shapes;
            }
            shapes = new List<Shape>();
            var hole = new Regex(@"\{(\d+)\}");
            foreach (string key in Templates)
            {
                string english = Loc.English(key);
                var pattern = new StringBuilder("^");
                int literal = 0;
                int at = 0;
                foreach (Match match in hole.Matches(english))
                {
                    string before = english.Substring(at, match.Index - at);
                    literal += before.Length;
                    pattern.Append(Regex.Escape(before)).Append("(.+?)");
                    at = match.Index + match.Length;
                }
                string rest = english.Substring(at);
                literal += rest.Length;
                pattern.Append(Regex.Escape(rest)).Append('$');
                shapes.Add(new Shape { Key = key, Pattern = new Regex(pattern.ToString(), RegexOptions.CultureInvariant | RegexOptions.IgnoreCase), Literal = literal });
            }
            return shapes;
        }

        /// <summary>Whether <paramref name="text"/> has Hebrew in it: words already translated, not a key.</summary>
        public static bool IsTranslated(string text)
        {
            foreach (char c in text)
            {
                if (c >= '\u0590' && c <= '\u05FF')
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Whether <paramref name="text"/> has letters outside its rich text tags: words, not just numbers and icons.</summary>
        public static bool HasLetters(string text)
        {
            if (text == null)
            {
                return false;
            }
            bool inTag = false;
            foreach (char c in text)
            {
                if (c == '<')
                {
                    inTag = true;
                }
                else if (c == '>')
                {
                    inTag = false;
                }
                else if (!inTag && char.IsLetter(c))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>A secondary skill at a level (1 Basic, 2 Advanced, 3 Expert): "Advanced Wisdom".</summary>
        public static string SkillName(int level, string skill)
        {
            string name = T(skill);
            switch (level)
            {
                case 1: return F("Basic {0}", name);
                case 2: return F("Advanced {0}", name);
                default: return F("Expert {0}", name);
            }
        }

        /// <summary>A whole number as the interface writes it.</summary>
        public static string Number(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
