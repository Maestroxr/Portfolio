using System.Linq;
using Gamebox;
using UnityEngine;

namespace Portfolio.Monopoly
{
    /// <summary>
    /// The game modes. Unlike a campaign of levels every mode is open from the start (a board game is played the way the
    /// table likes); stars reward beating the computer: one for a win against computer players, two for beating three of
    /// them, three for beating three hard ones.
    /// </summary>
    [CreateAssetMenu(fileName = "MonopolyCampaign", menuName = "Monopoly/Campaign", order = 3)]
    public class MonopolyCampaign : Campaign
    {
        public override int MaxStarsPerLevel => 3;

        public override bool IsUnlocked(int index, CampaignProgress progress)
        {
            return index >= 0 && index < Count;
        }

        public MonopolyLevel Mode(int index)
        {
            return this[index] as MonopolyLevel;
        }

        /// <summary>The stars a finished match earns: none unless a human player beat at least one computer player.</summary>
        public static int StarsFor(MonopolyMatch match)
        {
            if (match == null || !match.IsOver || match.winner < 0 || match.players[match.winner].bot)
            {
                return 0;
            }
            var bots = match.players.Where(p => p.bot).ToList();
            if (bots.Count == 0)
            {
                return 0;
            }
            if (bots.Count >= 3)
            {
                return bots.All(p => p.level == BotLevel.Hard) ? 3 : 2;
            }
            return 1;
        }

        public const string StarRules = "1 star: beat the computer.  2 stars: beat three computer players.  3 stars: beat three hard ones.";
    }
}
