using System;
using System.Collections.Generic;

namespace Portfolio.MemoryCards
{
    /// <summary>
    /// A computer player of a versus game at one device. It plays fair: it knows only the faces it saw turn up (anybody's
    /// flips, the memorize phase, a peek) and only as many of them as its memory holds, the oldest going first. With a
    /// set in mind it takes it, unless it slips (like the <see cref="MemoryCardsAutopilot"/>'s mistake rate) and turns a
    /// card it does not know instead; without one it explores the cards it has not seen. It never turns a bomb it knows,
    /// cracks ice like anybody and takes a peek card it remembers. The <see cref="Skill"/> of each level of the local play
    /// setup (Easy, Normal, Hard) sets how much it remembers, how often it slips and how fast it clicks.
    /// </summary>
    public sealed class MemoryCardsComputer
    {
        /// <summary>How well a computer plays.</summary>
        public struct Skill
        {
            /// <summary>How many cards it remembers at once.</summary>
            public int Memory;

            /// <summary>The chance that it turns a card it does not know when it remembers a better one.</summary>
            public float Slip;

            /// <summary>Seconds between two clicks, so the people at the device can follow its turn.</summary>
            public float Interval;
        }

        /// <summary>The levels of the local play setup, easiest first: Easy, Normal, Hard.</summary>
        public static readonly Skill[] Levels =
        {
            new Skill { Memory = 4, Slip = 0.35f, Interval = 0.95f },
            new Skill { Memory = 10, Slip = 0.15f, Interval = 0.8f },
            new Skill { Memory = int.MaxValue, Slip = 0.04f, Interval = 0.7f },
        };

        /// <summary>Seconds the computer waits at the start of its turn, while the banner names it.</summary>
        public const float FirstDelay = 1.1f;

        private readonly Random random;
        /// <summary>The cards it remembers, the one seen longest ago first.</summary>
        private readonly LinkedList<MemoryCard> memory = new LinkedList<MemoryCard>();
        private readonly Dictionary<int, LinkedListNode<MemoryCard>> remembered = new Dictionary<int, LinkedListNode<MemoryCard>>();

        public MemoryCardsComputer(Skill skill, Random random)
        {
            Ability = skill;
            this.random = random ?? throw new ArgumentNullException(nameof(random));
        }

        /// <summary>The skill of a level of the setup (an index into <see cref="Levels"/>, clamped).</summary>
        public static Skill SkillOf(int level)
        {
            return Levels[Math.Max(0, Math.Min(level, Levels.Length - 1))];
        }

        public Skill Ability { get; }

        /// <summary>How many cards it remembers now.</summary>
        public int Remembered => memory.Count;

        public bool Knows(MemoryCard card)
        {
            return card != null && remembered.ContainsKey(card.Id);
        }

        /// <summary>A card turned face up where the computer could see it: it remembers it as the newest.</summary>
        public void See(MemoryCard card)
        {
            if (card == null)
            {
                return;
            }
            // Cards that left the board take no room.
            LinkedListNode<MemoryCard> node = memory.First;
            while (node != null)
            {
                LinkedListNode<MemoryCard> next = node.Next;
                if (node.Value.IsDone || node.Value == card)
                {
                    remembered.Remove(node.Value.Id);
                    memory.Remove(node);
                }
                node = next;
            }
            if (card.IsDone)
            {
                return;
            }
            remembered[card.Id] = memory.AddLast(card);
            while (memory.Count > Math.Max(0, Ability.Memory))
            {
                remembered.Remove(memory.First.Value.Id);
                memory.RemoveFirst();
            }
        }

        /// <summary>Many cards showed at once (the memorize phase, a peek): what stays in a small memory is down to chance.</summary>
        public void SeeAll(IEnumerable<MemoryCard> cards)
        {
            var shown = new List<MemoryCard>(cards);
            for (int i = shown.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                MemoryCard swap = shown[i];
                shown[i] = shown[j];
                shown[j] = swap;
            }
            foreach (MemoryCard card in shown)
            {
                See(card);
            }
        }

        /// <summary>The card to turn next on <paramref name="board"/>, or null when no card can be turned.</summary>
        public MemoryCard Choose(MemoryRound board)
        {
            int animal = -1;
            int shown = 0;
            bool wildShowing = false;
            foreach (MemoryCard card in board.Cards)
            {
                if (card.State != CardState.Revealed)
                {
                    continue;
                }
                if (card.IsAnimal)
                {
                    animal = card.Animal;
                    shown++;
                }
                else if (card.Kind == CardKind.Wild)
                {
                    wildShowing = true;
                }
            }
            bool slips = random.NextDouble() < Ability.Slip;

            if (animal >= 0)
            {
                // Half a set is up: the rest of it, or the wild card that takes it all.
                if (!slips)
                {
                    MemoryCard partner = Known(board, card => card.IsAnimal && card.Animal == animal)
                                         ?? Known(board, card => card.Kind == CardKind.Wild);
                    if (partner != null)
                    {
                        return partner;
                    }
                }
                return Unknown(board) ?? Known(board, card => card.IsAnimal && card.Animal == animal) ?? Any(board);
            }
            if (wildShowing)
            {
                // A wild card is up: any animal it remembers is a whole set.
                return (slips ? null : Known(board, card => card.IsAnimal)) ?? Unknown(board) ?? Any(board);
            }
            if (!slips)
            {
                int set = KnownSet(board);
                if (set >= 0)
                {
                    return Known(board, card => card.IsAnimal && card.Animal == set);
                }
                MemoryCard peek = Known(board, card => card.Kind == CardKind.Peek);
                if (peek != null)
                {
                    return peek;
                }
            }
            return Unknown(board) ?? Known(board, card => card.IsAnimal) ?? Any(board);
        }

        /// <summary>An animal of which it remembers a whole set still face down, or -1.</summary>
        private int KnownSet(MemoryRound board)
        {
            var counts = new Dictionary<int, int>();
            foreach (MemoryCard card in board.Cards)
            {
                if (card.State == CardState.Hidden && card.IsAnimal && Knows(card))
                {
                    counts.TryGetValue(card.Animal, out int count);
                    counts[card.Animal] = ++count;
                    if (count >= board.Rules.MatchSize)
                    {
                        return card.Animal;
                    }
                }
            }
            return -1;
        }

        /// <summary>A face down card it remembers that passes <paramref name="filter"/>, or null.</summary>
        private MemoryCard Known(MemoryRound board, Func<MemoryCard, bool> filter)
        {
            foreach (MemoryCard card in board.Cards)
            {
                if (card.State == CardState.Hidden && Knows(card) && filter(card))
                {
                    return card;
                }
            }
            return null;
        }

        /// <summary>A face down card it does not remember, picked at random, or null.</summary>
        private MemoryCard Unknown(MemoryRound board)
        {
            return Random(board, card => !Knows(card));
        }

        /// <summary>Any face down card but a bomb it knows (a bomb only when nothing else is left).</summary>
        private MemoryCard Any(MemoryRound board)
        {
            return Random(board, card => !(card.Kind == CardKind.Bomb && Knows(card))) ?? Random(board, card => true);
        }

        private MemoryCard Random(MemoryRound board, Func<MemoryCard, bool> filter)
        {
            MemoryCard chosen = null;
            int seen = 0;
            foreach (MemoryCard card in board.Cards)
            {
                if (card.State != CardState.Hidden || !filter(card))
                {
                    continue;
                }
                seen++;
                if (random.Next(seen) == 0)
                {
                    chosen = card;
                }
            }
            return chosen;
        }
    }
}
