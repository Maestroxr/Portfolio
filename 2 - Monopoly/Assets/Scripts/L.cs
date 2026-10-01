using Gamebox;

namespace Portfolio.Monopoly
{
    /// <summary>
    /// The words of Monopoly in the language shown (see <see cref="Loc"/>): the key is the English text, and the table is
    /// the game's (<c>Assets/Localization/Monopoly.csv</c>), then the shared one. English needs no lookup.
    /// </summary>
    public static class L
    {
        public const GameType Game = GameType.Monopoly;

        /// <summary>Whether the language shown is English (the keys are the English texts).</summary>
        public static bool English => GameLanguages.IsEnglish;

        public static string T(string key)
        {
            return Loc.T(Game, key);
        }

        public static string F(string key, params object[] args)
        {
            return Loc.F(Game, key, args);
        }

        /// <summary>A word read from data (a space, a card, a mode): translated when the table has it, else as it is.</summary>
        public static string Data(string english)
        {
            return string.IsNullOrEmpty(english) || English ? english : Loc.T(Game, english);
        }
    }
}
