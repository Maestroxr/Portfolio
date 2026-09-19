using UnityEngine;

namespace Portfolio.MemoryCards
{
    /// <summary>
    /// The boards of the endless run. The clock carries over from board to board: every board starts with the time
    /// left on the previous one plus <see cref="ClearBonus"/>. The first boards bring in the twists one by one; after
    /// the eighth the last four patterns repeat with more ice, faster shuffles and less time per match.
    /// </summary>
    public static class EndlessRules
    {
        public const float StartTime = 60f;

        /// <summary>Seconds clearing board <paramref name="board"/> (1 based) adds to the clock.</summary>
        public static float ClearBonus(int board)
        {
            return 12f + Mathf.Min(Mathf.Max(1, board), 8) * 1.5f;
        }

        /// <summary>The rules of board <paramref name="board"/> (1 based); the time limit still has to be set.</summary>
        public static RoundRules ForBoard(int board)
        {
            int n = Mathf.Max(1, board);
            int pattern = n <= 8 ? n : 5 + (n - 9) % 4;
            int cycle = n <= 8 ? 0 : 1 + (n - 9) / 4;
            var rules = new RoundRules
            {
                MatchSize = 2,
                UnflipDelay = 0.75f,
                PreviewTime = n == 1 ? 2.5f : 1.5f,
                MatchTimeBonus = Mathf.Max(0.5f, 1.5f - cycle * 0.25f),
                TimeLimit = StartTime
            };
            switch (pattern)
            {
                case 1:
                    rules.Cards = 12;
                    rules.Columns = 4;
                    break;
                case 2:
                    rules.Cards = 16;
                    rules.Columns = 4;
                    rules.Clocks = 1;
                    rules.Peeks = 1;
                    break;
                case 3:
                    rules.Cards = 20;
                    rules.Columns = 5;
                    rules.Clocks = 1;
                    rules.Bombs = 1;
                    break;
                case 4:
                    rules.Cards = 18;
                    rules.Columns = 6;
                    rules.MatchSize = 3;
                    rules.PreviewTime = 2f;
                    break;
                case 5:
                    rules.Cards = 24;
                    rules.Columns = 6;
                    rules.Wilds = 1;
                    rules.Clocks = 1;
                    rules.ShuffleEvery = 4;
                    break;
                case 6:
                    rules.Cards = 24;
                    rules.Columns = 6;
                    rules.Bombs = 2;
                    rules.Frozen = 4;
                    break;
                case 7:
                    rules.Cards = 28;
                    rules.Columns = 7;
                    rules.Wilds = 1;
                    rules.Bombs = 1;
                    rules.Clocks = 1;
                    rules.Peeks = 1;
                    rules.ShuffleEvery = 3;
                    break;
                default:
                    rules.Cards = 24;
                    rules.Columns = 6;
                    rules.MatchSize = 3;
                    rules.Wilds = 1;
                    rules.Bombs = 1;
                    rules.Clocks = 1;
                    rules.PreviewTime = 2f;
                    break;
            }
            if (cycle > 0)
            {
                rules.Frozen = Mathf.Min(rules.AnimalCards / 2, rules.Frozen + cycle * 2);
                rules.ShuffleEvery = rules.ShuffleEvery > 0 ? Mathf.Max(2, rules.ShuffleEvery - cycle) : Mathf.Max(2, 6 - cycle);
                rules.PreviewTime = 1f;
            }
            return rules;
        }
    }
}
