using Gamebox;

namespace Portfolio.MemoryCards
{
    /// <summary>
    /// The single player of a Memory Cards game: counts the sets found, the mistakes and the cards flipped in the
    /// current game (an endless run counts over all of its boards).
    /// </summary>
    public class MemoryCardsPlayer : PlayerBase
    {
        public int Matches { get; private set; }

        public int Mistakes { get; private set; }

        public int Flips { get; private set; }

        public void RegisterFlip()
        {
            Flips++;
        }

        public void RegisterMatch()
        {
            Matches++;
        }

        public void RegisterMistake()
        {
            Mistakes++;
        }

        public void ResetMatches()
        {
            Matches = 0;
            Mistakes = 0;
            Flips = 0;
        }
    }
}
