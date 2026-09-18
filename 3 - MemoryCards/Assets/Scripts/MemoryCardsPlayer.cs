using Gamebox;

namespace Portfolio.MemoryCards
{
    /// <summary>The single player of a Memory Cards game; counts the matches found in the current game.</summary>
    public class MemoryCardsPlayer : PlayerBase
    {
        public int Matches { get; private set; }

        public void RegisterMatch()
        {
            Matches++;
        }

        public void ResetMatches()
        {
            Matches = 0;
        }
    }
}
