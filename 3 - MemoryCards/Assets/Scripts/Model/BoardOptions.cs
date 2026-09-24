using System;
using System.Collections.Generic;

namespace Portfolio.MemoryCards
{
    /// <summary>
    /// The board of an online room in the options of the room ("key=value;key=value"): the host's client writes the
    /// board of the level it picked, and the server, which does not know the levels of the game, reads it back and deals
    /// the same board. One place for the keys and the numbers, for both sides (this file is compiled into the server
    /// module too). The clock of the turn is the base server's own option and not written here.
    /// </summary>
    public static class BoardOptions
    {
        public const string CardsKey = "cards";
        public const string ColumnsKey = "columns";
        public const string MatchKey = "match";
        public const string WildsKey = "wilds";
        public const string BombsKey = "bombs";
        public const string PeeksKey = "peeks";
        public const string FrozenKey = "frozen";
        /// <summary>Seconds the board is shown for memorizing, in tenths, to keep the options whole numbers.</summary>
        public const string PreviewKey = "preview";
        /// <summary>How many different animals the level has to deal from.</summary>
        public const string AnimalsKey = "animals";

        /// <summary>The most of anything a board can ask for, so a written option cannot be absurd.</summary>
        public const int MaxValue = 1000;

        /// <summary>
        /// The keys and values of <paramref name="rules"/> (the rules of the shared board, see
        /// <see cref="VersusMatch.RulesFor"/>) with <paramref name="animals"/> to deal from, in pairs, for the writer
        /// of the room options; <paramref name="first"/> pairs go before them.
        /// </summary>
        public static object[] Pairs(RoundRules rules, int animals, params object[] first)
        {
            if (rules == null)
            {
                throw new ArgumentNullException(nameof(rules));
            }
            var pairs = new List<object>(first ?? Array.Empty<object>())
            {
                CardsKey, rules.Cards,
                ColumnsKey, rules.Columns,
                MatchKey, rules.MatchSize,
                WildsKey, rules.Wilds,
                BombsKey, rules.Bombs,
                PeeksKey, rules.Peeks,
                FrozenKey, rules.Frozen,
                PreviewKey, (int)Math.Round(rules.PreviewTime * 10f),
                AnimalsKey, animals
            };
            return pairs.ToArray();
        }

        /// <summary>
        /// The board written into the options, read with <paramref name="option"/> (the value of a key as a whole
        /// number, or the fallback given) and the animals to deal from. Check the rules with
        /// <see cref="RoundRules.IsValid"/> before dealing: what a client writes is not trusted.
        /// </summary>
        public static RoundRules Read(Func<string, int, int> option, out int animals)
        {
            if (option == null)
            {
                throw new ArgumentNullException(nameof(option));
            }
            var rules = new RoundRules
            {
                Cards = Clamp(option(CardsKey, 16)),
                Columns = Clamp(option(ColumnsKey, 4)),
                MatchSize = Clamp(option(MatchKey, 2)),
                Wilds = Clamp(option(WildsKey, 0)),
                Bombs = Clamp(option(BombsKey, 0)),
                Peeks = Clamp(option(PeeksKey, 0)),
                Frozen = Clamp(option(FrozenKey, 0)),
                PreviewTime = Clamp(option(PreviewKey, 0)) / 10f
            };
            animals = Clamp(option(AnimalsKey, 30));
            return VersusMatch.RulesFor(rules);
        }

        private static int Clamp(int value)
        {
            return Math.Max(0, Math.Min(value, MaxValue));
        }
    }
}
