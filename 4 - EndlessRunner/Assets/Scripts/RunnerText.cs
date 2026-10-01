using System;
using System.Globalization;
using Gamebox;

namespace Portfolio.EndlessRunner
{
    /// <summary>
    /// The words of the Endless Runner in the language shown, from its table (Assets/Localization/EndlessRunner.csv; a
    /// key is the English text, see <see cref="Loc"/>). A theme may say some words differently (the Night Shift's chips
    /// for coins, see <see cref="RunnerGameTheme.Say"/>): <see cref="Say(string)"/> applies the theme's words to the
    /// English key first and translates what that gives, so a theme's sentence is a key of its own; a theme's sentence
    /// missing from the table falls back to the translation of the classic one.
    /// </summary>
    public static class RunnerText
    {
        public const GameType Game = GameType.EndlessRunner;

        /// <summary><paramref name="key"/> in the language shown.</summary>
        public static string T(string key)
        {
            return Loc.T(Game, key);
        }

        /// <summary><paramref name="key"/> in the language shown with its holes filled.</summary>
        public static string F(string key, params object[] args)
        {
            return Loc.F(Game, key, args);
        }

        /// <summary>A key kept to be translated when it is shown (lists of hints and tips); returns it as it is.</summary>
        public static string Key(string key)
        {
            return key;
        }

        /// <summary><paramref name="key"/> in the words of the active theme, in the language shown.</summary>
        public static string Say(string key)
        {
            return Say(RunnerGameTheme.Active, key);
        }

        /// <summary><paramref name="key"/> in the words of <paramref name="theme"/>, in the language shown.</summary>
        public static string Say(RunnerGameTheme theme, string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return key;
            }
            string said = theme != null ? theme.Say(key) : key;
            if (said == key || GameLanguages.IsEnglish || Loc.Has(Loc.Table(Game), said))
            {
                return T(said);
            }
            return T(key);
        }

        /// <summary><see cref="Say(RunnerGameTheme, string)"/> with its holes filled.</summary>
        public static string SayF(RunnerGameTheme theme, string key, params object[] args)
        {
            string format = Say(theme, key);
            try
            {
                return string.Format(CultureInfo.InvariantCulture, format, args);
            }
            catch (FormatException)
            {
                return string.Format(CultureInfo.InvariantCulture, Loc.English(theme != null ? theme.Say(key) : key), args);
            }
        }

        /// <summary><see cref="Say(string)"/> with its holes filled.</summary>
        public static string SayF(string key, params object[] args)
        {
            return SayF(RunnerGameTheme.Active, key, args);
        }

        /// <summary>A level's name as the active theme calls it, in the language shown.</summary>
        public static string TitleOf(RunnerLevel level)
        {
            return T(RunnerGameTheme.TitleFor(level));
        }
    }
}
