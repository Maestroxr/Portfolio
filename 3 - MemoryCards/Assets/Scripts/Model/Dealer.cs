using System;
using System.Collections.Generic;

namespace Portfolio.MemoryCards
{
    /// <summary>A dealt board: the cards in slot order, the animals they show and the order of the parade.</summary>
    public sealed class Deal
    {
        /// <summary>The cards; a card's <see cref="MemoryCard.Id"/> is its index here.</summary>
        public readonly List<MemoryCard> Cards = new List<MemoryCard>();

        /// <summary>For every animal of the round (<see cref="MemoryCard.Animal"/>) its index in the animal pool.</summary>
        public readonly List<int> Animals = new List<int>();

        /// <summary>The order the animals (round indices) have to be matched in on a parade board.</summary>
        public readonly List<int> Parade = new List<int>();
    }


    /// <summary>Deals boards: picks the animals, builds the sets and the special cards, freezes some and shuffles.</summary>
    public static class Dealer
    {
        /// <summary>
        /// Deals a board for <paramref name="rules"/> from a pool of <paramref name="animalPool"/> animals. The cards lie
        /// in slots 0 to Cards - 1, row by row. Throws when the rules cannot be dealt.
        /// </summary>
        public static Deal Create(RoundRules rules, int animalPool, Random random)
        {
            if (rules == null)
            {
                throw new ArgumentNullException(nameof(rules));
            }
            if (!rules.IsValid(animalPool, out string message))
            {
                throw new ArgumentException(message);
            }
            random ??= new Random();
            var deal = new Deal();

            // A random choice of animals from the pool, one per set.
            var pool = new List<int>();
            for (int i = 0; i < animalPool; i++)
            {
                pool.Add(i);
            }
            Shuffle(pool, random);
            for (int i = 0; i < rules.Sets; i++)
            {
                deal.Animals.Add(pool[i]);
            }

            var cards = new List<MemoryCard>();
            for (int animal = 0; animal < rules.Sets; animal++)
            {
                for (int copy = 0; copy < rules.MatchSize; copy++)
                {
                    cards.Add(new MemoryCard { Kind = CardKind.Animal, Animal = animal });
                }
            }
            AddSpecials(cards, CardKind.Wild, rules.Wilds);
            AddSpecials(cards, CardKind.Bomb, rules.Bombs);
            AddSpecials(cards, CardKind.Clock, rules.Clocks);
            AddSpecials(cards, CardKind.Peek, rules.Peeks);

            var animals = cards.FindAll(card => card.IsAnimal);
            Shuffle(animals, random);
            for (int i = 0; i < rules.Frozen && i < animals.Count; i++)
            {
                animals[i].Frozen = true;
            }

            Shuffle(cards, random);
            for (int i = 0; i < cards.Count; i++)
            {
                cards[i].Id = i;
                cards[i].Slot = i;
                deal.Cards.Add(cards[i]);
            }

            if (rules.Parade)
            {
                for (int i = 0; i < rules.Sets; i++)
                {
                    deal.Parade.Add(i);
                }
                Shuffle(deal.Parade, random);
            }
            return deal;
        }

        private static void AddSpecials(List<MemoryCard> cards, CardKind kind, int count)
        {
            for (int i = 0; i < count; i++)
            {
                cards.Add(new MemoryCard { Kind = kind, Animal = -1 });
            }
        }

        /// <summary>Fisher-Yates shuffle.</summary>
        public static void Shuffle<T>(IList<T> list, Random random)
        {
            for (int n = list.Count - 1; n > 0; n--)
            {
                int k = random.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}
