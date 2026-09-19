namespace Portfolio.MemoryCards.Tests
{
    /// <summary>Builds deals for the tests from a short layout string.</summary>
    internal static class Boards
    {
        /// <summary>
        /// A deal from space separated tokens in slot order: a number is an animal, W a wild card, B a bomb, C a clock
        /// and P a peek card; a trailing * freezes the card. Animals are numbered 0 to n - 1 and map to pool 0 to n - 1.
        /// </summary>
        public static Deal Make(string layout, params int[] parade)
        {
            var deal = new Deal();
            int animals = 0;
            string[] tokens = layout.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < tokens.Length; i++)
            {
                string token = tokens[i];
                bool frozen = token.EndsWith("*");
                token = token.TrimEnd('*');
                var card = new MemoryCard { Id = i, Slot = i, Frozen = frozen };
                switch (token)
                {
                    case "W":
                        card.Kind = CardKind.Wild;
                        break;
                    case "B":
                        card.Kind = CardKind.Bomb;
                        break;
                    case "C":
                        card.Kind = CardKind.Clock;
                        break;
                    case "P":
                        card.Kind = CardKind.Peek;
                        break;
                    default:
                        card.Kind = CardKind.Animal;
                        card.Animal = int.Parse(token);
                        if (card.Animal + 1 > animals)
                        {
                            animals = card.Animal + 1;
                        }
                        break;
                }
                deal.Cards.Add(card);
            }
            for (int i = 0; i < animals; i++)
            {
                deal.Animals.Add(i);
            }
            deal.Parade.AddRange(parade);
            return deal;
        }

        public static RoundRules Rules(int cards, int columns = 4, int matchSize = 2)
        {
            return new RoundRules { Cards = cards, Columns = columns, MatchSize = matchSize, UnflipDelay = 0.5f };
        }
    }
}
