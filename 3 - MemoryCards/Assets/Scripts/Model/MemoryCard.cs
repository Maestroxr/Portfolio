namespace Portfolio.MemoryCards
{
    /// <summary>What a card on the board is. Animals are matched in sets; the other kinds act when they are flipped.</summary>
    public enum CardKind
    {
        /// <summary>An animal; matched with the other cards of the same animal.</summary>
        Animal = 0,
        /// <summary>Matches with any animal and takes every card of that animal off the board.</summary>
        Wild = 1,
        /// <summary>A trap: costs a heart, time and points.</summary>
        Bomb = 2,
        /// <summary>Gives time back.</summary>
        Clock = 3,
        /// <summary>Shows every hidden card for a moment.</summary>
        Peek = 4,
        /// <summary>A face down card of an online board: only the server knows what it is until it turns.</summary>
        Unknown = 255
    }


    public enum CardState
    {
        /// <summary>Face down, can be flipped.</summary>
        Hidden = 0,
        /// <summary>Face up and waiting for the rest of its set.</summary>
        Revealed = 1,
        /// <summary>Part of a completed set; stays face up.</summary>
        Matched = 2,
        /// <summary>A special card that was used up.</summary>
        Spent = 3
    }


    /// <summary>One card of a round: its kind, the animal it shows, where it lies and how far it got.</summary>
    public sealed class MemoryCard
    {
        /// <summary>Position of the card in the deal; the view of the card has the same index.</summary>
        public int Id;

        public CardKind Kind;

        /// <summary>Index of the card's animal in <see cref="Deal.Animals"/>; -1 for special cards.</summary>
        public int Animal = -1;

        public CardState State;

        /// <summary>A frozen card takes one tap to crack the ice before it can be flipped.</summary>
        public bool Frozen;

        /// <summary>The grid slot the card lies in (row major).</summary>
        public int Slot;

        public bool IsAnimal => Kind == CardKind.Animal;

        /// <summary>Bombs, clocks and peeks act as soon as they are flipped and never join a set.</summary>
        public bool IsInstant => Kind == CardKind.Bomb || Kind == CardKind.Clock || Kind == CardKind.Peek;

        public bool IsHidden => State == CardState.Hidden;

        public bool IsDone => State == CardState.Matched || State == CardState.Spent;

        public override string ToString()
        {
            return Kind == CardKind.Animal ? $"#{Id} animal {Animal} {State}" : $"#{Id} {Kind} {State}";
        }
    }
}
