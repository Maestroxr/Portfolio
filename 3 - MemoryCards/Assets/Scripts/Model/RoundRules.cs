using System;
using UnityEngine;

namespace Portfolio.MemoryCards
{
    /// <summary>
    /// The rules one round is played with: the size of the board, how many cards make a set, the limits (time, hearts,
    /// moves), the twists (memorize, shuffle, parade) and the special cards dealt. Built from <see cref="MemoryCardsSettings"/>
    /// for campaign levels and free play, or generated for the boards of the endless run.
    /// </summary>
    [Serializable]
    public class RoundRules
    {
        public const int MaxCards = 60;
        public const int MaxColumns = 10;

        [Tooltip("Cards on the board, special cards included.")]
        public int Cards = 16;
        [Tooltip("Cards in one row of the board.")]
        public int Columns = 4;
        [Tooltip("Cards of one animal that make a set: 2 for pairs, 3 for triplets.")]
        public int MatchSize = 2;
        [Tooltip("Seconds on the clock; 0 plays without a time limit (the clock counts up).")]
        public float TimeLimit;
        [Tooltip("Seconds a wrong set stays face up.")]
        public float UnflipDelay = 0.9f;
        [Tooltip("Seconds every card is shown before the round starts; 0 for none.")]
        public float PreviewTime;
        [Tooltip("Mistakes allowed (a heart each); 0 for no hearts.")]
        public int Hearts;
        [Tooltip("Attempts allowed; 0 for no limit.")]
        public int MoveLimit;
        [Tooltip("Seconds a completed set adds to the clock of a timed round.")]
        public float MatchTimeBonus;
        [Tooltip("Mistakes after which the hidden cards swap places; 0 never shuffles.")]
        public int ShuffleEvery;
        public int Bombs;
        public int Wilds;
        public int Clocks;
        public int Peeks;
        [Tooltip("Animal cards that start frozen.")]
        public int Frozen;
        [Tooltip("The animals have to be matched in the order the parade shows.")]
        public bool Parade;

        [Header("Special cards")]
        public float ClockBonus = 6f;
        public float BombPenalty = 5f;
        public float PeekTime = 1.6f;

        public int Rows => Columns > 0 ? Mathf.CeilToInt(Cards / (float)Columns) : 0;

        public int SpecialCards => Bombs + Wilds + Clocks + Peeks;

        public int AnimalCards => Cards - SpecialCards;

        /// <summary>Sets of animals on the board, which is also the number of different animals needed.</summary>
        public int Sets => MatchSize > 0 ? AnimalCards / MatchSize : 0;

        public bool HasTimeLimit => TimeLimit > 0f;

        public RoundRules Copy()
        {
            return (RoundRules)MemberwiseClone();
        }

        /// <summary>
        /// Whether a board can be dealt with these rules from <paramref name="animalPool"/> different animals (a
        /// negative pool skips the animal count check).
        /// </summary>
        public bool IsValid(int animalPool, out string message)
        {
            if (MatchSize < 2 || MatchSize > 4)
            {
                message = $"Cards per match {MatchSize} has to be 2, 3 or 4";
                return false;
            }
            if (Columns < 1 || Columns > MaxColumns)
            {
                message = $"Cards per row {Columns} has to be between 1 and {MaxColumns}";
                return false;
            }
            if (Cards < MatchSize || Cards > MaxCards)
            {
                message = $"Cards {Cards} has to be between {MatchSize} and {MaxCards}";
                return false;
            }
            if (Cards % Columns != 0)
            {
                message = $"Cards {Cards} has to be a multiple of cards per row {Columns}";
                return false;
            }
            if (Bombs < 0 || Wilds < 0 || Clocks < 0 || Peeks < 0 || Frozen < 0)
            {
                message = "Special card counts cannot be negative";
                return false;
            }
            if (AnimalCards < MatchSize)
            {
                message = $"{SpecialCards} special cards leave no room for a set of {MatchSize} animals";
                return false;
            }
            if (AnimalCards % MatchSize != 0)
            {
                message = $"The {AnimalCards} animal cards (cards minus special cards) have to be a multiple of cards per match {MatchSize}";
                return false;
            }
            if (animalPool >= 0 && Sets > animalPool)
            {
                message = $"The board needs {Sets} different animals but only {animalPool} are available";
                return false;
            }
            if (Frozen > AnimalCards)
            {
                message = $"Frozen cards {Frozen} cannot be more than the {AnimalCards} animal cards";
                return false;
            }
            if (TimeLimit < 0f || UnflipDelay < 0f || PreviewTime < 0f || MatchTimeBonus < 0f)
            {
                message = "Times cannot be negative";
                return false;
            }
            if (UnflipDelay > 5f)
            {
                message = $"Unflip delay {UnflipDelay} has to be 5 seconds or less";
                return false;
            }
            if (PreviewTime > 30f)
            {
                message = $"Memorize time {PreviewTime} has to be 30 seconds or less";
                return false;
            }
            if (Hearts < 0 || MoveLimit < 0 || ShuffleEvery < 0)
            {
                message = "Hearts, moves and shuffle cannot be negative";
                return false;
            }
            if (MoveLimit > 0 && MoveLimit < Sets)
            {
                message = $"A move limit of {MoveLimit} is too low to clear {Sets} sets";
                return false;
            }
            message = "OK";
            return true;
        }
    }
}
