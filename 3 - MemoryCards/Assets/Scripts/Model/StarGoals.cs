using System;
using System.Globalization;
using UnityEngine;

namespace Portfolio.MemoryCards
{
    /// <summary>What the third star of a level asks for.</summary>
    public enum StarGoal
    {
        /// <summary>Only clearing the board and the mistake goal count; the third star comes with the second.</summary>
        None,
        /// <summary>Finish within a number of seconds, or with that many seconds left on a timed level.</summary>
        Time,
        /// <summary>Finish in at most a number of moves.</summary>
        Moves,
        /// <summary>Reach a combo.</summary>
        Combo,
        /// <summary>Reach a score.</summary>
        Score
    }


    /// <summary>
    /// The three stars of a level: clearing the board, making at most <see cref="maxMistakes"/> mistakes, and the
    /// <see cref="third"/> goal.
    /// </summary>
    [Serializable]
    public class StarGoals
    {
        [Tooltip("Most mistakes that still earn the second star.")]
        public int maxMistakes = 4;
        public StarGoal third = StarGoal.Time;
        [Tooltip("Seconds, moves, combo or score of the third star, depending on the goal.")]
        public float thirdTarget = 60f;

        public int Stars(MemoryRound round)
        {
            if (round == null || !round.IsCleared)
            {
                return 0;
            }
            return 1 + (SecondStar(round) ? 1 : 0) + (ThirdStar(round) ? 1 : 0);
        }

        public bool SecondStar(MemoryRound round)
        {
            return round != null && round.IsCleared && round.Mistakes <= maxMistakes;
        }

        public bool ThirdStar(MemoryRound round)
        {
            if (round == null || !round.IsCleared)
            {
                return false;
            }
            switch (third)
            {
                case StarGoal.Time:
                    return round.HasCountdown ? round.Clock >= thirdTarget : round.Clock <= thirdTarget;
                case StarGoal.Moves:
                    return round.Moves <= Mathf.RoundToInt(thirdTarget);
                case StarGoal.Combo:
                    return round.BestCombo >= Mathf.RoundToInt(thirdTarget);
                case StarGoal.Score:
                    return round.Score >= Mathf.RoundToInt(thirdTarget);
                default:
                    return SecondStar(round);
            }
        }

        /// <summary>The text of star 1, 2 or 3.</summary>
        public string Describe(int star, bool countdown)
        {
            switch (star)
            {
                case 1:
                    return "Clear the board";
                case 2:
                    return maxMistakes == 0 ? "Make no mistakes" : maxMistakes == 1 ? "At most 1 mistake" : $"At most {maxMistakes} mistakes";
                default:
                    switch (third)
                    {
                        case StarGoal.Time:
                            return countdown ? $"Finish with {Mathf.RoundToInt(thirdTarget)} s left" : $"Finish within {Mathf.RoundToInt(thirdTarget)} s";
                        case StarGoal.Moves:
                            return $"Finish in {Mathf.RoundToInt(thirdTarget)} moves or less";
                        case StarGoal.Combo:
                            return $"Reach a x{Mathf.RoundToInt(thirdTarget)} combo";
                        case StarGoal.Score:
                            return $"Score {Mathf.RoundToInt(thirdTarget).ToString("N0", CultureInfo.InvariantCulture)} points";
                        default:
                            return Describe(2, countdown);
                    }
            }
        }
    }
}
