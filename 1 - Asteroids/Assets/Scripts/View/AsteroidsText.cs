using Gamebox;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The words Asteroids shows, in the language picked (<see cref="Loc"/>): the key is the English text, found in the
    /// Asteroids table (Assets/Localization/Asteroids.csv), then in the shared one. Code reads <c>T(english)</c> and
    /// <c>F(englishFormat, n)</c> with <c>using static Portfolio.Asteroids.AsteroidsText;</c>.
    /// </summary>
    public static class AsteroidsText
    {
        /// <summary>The words of <paramref name="key"/> (an English text, or data such as a mission title) in the language shown.</summary>
        public static string T(string key)
        {
            return Loc.T(GameType.Asteroids, key);
        }

        /// <summary>The words of <paramref name="key"/> in the language shown, with its <c>{0}</c> holes filled.</summary>
        public static string F(string key, params object[] args)
        {
            return Loc.F(GameType.Asteroids, key, args);
        }
    }
}
