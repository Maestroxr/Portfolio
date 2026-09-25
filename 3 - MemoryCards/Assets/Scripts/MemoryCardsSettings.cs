using System.Collections.Generic;
using Gamebox;
using UnityEngine;

namespace Portfolio.MemoryCards
{
    /// <summary>
    /// The rules of a Memory Cards board: size, cards per match, timings, limits, twists and special cards. Every
    /// campaign level has its own settings asset; the default and custom settings are the rules of free play, and the
    /// settings panel edits the custom ones. Allows saving and loading of settings using storage, or copying from
    /// another instance.
    /// </summary>
    [CreateAssetMenu(fileName = "MemoryCardsSettings", menuName = "Memory Cards/Settings", order = 1)]
    public class MemoryCardsSettings : GameSettings
    {
        [Tooltip("Animals to deal from. Empty uses the animals of the level's world.")]
        public List<Sprite> CardTypes = new List<Sprite>();
        [Tooltip("Cards on the board, special cards included.")]
        public int CardsAmount = 16;
        public int CardsPerRow = 4;
        [Tooltip("Cards of one animal that make a set: 2 for pairs, 3 for triplets.")]
        public int FlippedCardsPerMatch = 2;
        [Tooltip("Seconds on the clock; 0 plays without a time limit.")]
        public float TimePerGame = 60f;
        [Tooltip("Seconds a wrong set stays face up.")]
        public float TimeUntilUnflip = 0.9f;

        [Header("Twists")]
        [Tooltip("Seconds every card is shown before the round starts.")]
        public float PreviewTime;
        [Tooltip("Mistakes allowed, a heart each; 0 for no hearts.")]
        public int Hearts;
        [Tooltip("Attempts allowed; 0 for no limit.")]
        public int MoveLimit;
        [Tooltip("Seconds a completed set adds to the clock.")]
        public float MatchTimeBonus;
        [Tooltip("Mistakes after which the hidden cards swap places; 0 never shuffles.")]
        public int ShuffleEvery;
        [Tooltip("Animals have to be matched in the order the parade shows.")]
        public bool Parade;

        [Header("Special cards")]
        public int Bombs;
        public int Wilds;
        public int Clocks;
        public int Peeks;
        [Tooltip("Animal cards that start frozen.")]
        public int Frozen;

        /// <summary>The rules of a round played with these settings.</summary>
        public RoundRules ToRules()
        {
            return new RoundRules
            {
                Cards = CardsAmount,
                Columns = CardsPerRow,
                MatchSize = FlippedCardsPerMatch,
                TimeLimit = TimePerGame,
                UnflipDelay = TimeUntilUnflip,
                PreviewTime = PreviewTime,
                Hearts = Hearts,
                MoveLimit = MoveLimit,
                MatchTimeBonus = MatchTimeBonus,
                ShuffleEvery = ShuffleEvery,
                Parade = Parade,
                Bombs = Bombs,
                Wilds = Wilds,
                Clocks = Clocks,
                Peeks = Peeks,
                Frozen = Frozen
            };
        }


        public override void SaveSettings(IStorageStrategy storage, string prefix)
        {
            if (!AreSettingsValid(out string error))
            {
                throw new GameSettingsException($"Cannot save invalid settings. Reason: {error}");
            }
            storage.SetInt($"{prefix}CardsAmount", CardsAmount);
            storage.SetInt($"{prefix}CardsPerRow", CardsPerRow);
            storage.SetInt($"{prefix}FlippedCardsPerMatch", FlippedCardsPerMatch);
            storage.SetFloat($"{prefix}TimePerGame", TimePerGame);
            storage.SetFloat($"{prefix}TimeUntilUnflip", TimeUntilUnflip);
            storage.SetFloat($"{prefix}PreviewTime", PreviewTime);
            storage.SetInt($"{prefix}Hearts", Hearts);
            storage.SetInt($"{prefix}MoveLimit", MoveLimit);
            storage.SetFloat($"{prefix}MatchTimeBonus", MatchTimeBonus);
            storage.SetInt($"{prefix}ShuffleEvery", ShuffleEvery);
            storage.SetBool($"{prefix}Parade", Parade);
            storage.SetInt($"{prefix}Bombs", Bombs);
            storage.SetInt($"{prefix}Wilds", Wilds);
            storage.SetInt($"{prefix}Clocks", Clocks);
            storage.SetInt($"{prefix}Peeks", Peeks);
            storage.SetInt($"{prefix}Frozen", Frozen);
        }

        public override void LoadSettings(IStorageStrategy storage, string prefix)
        {
            CardsAmount = storage.GetInt($"{prefix}CardsAmount", CardsAmount);
            CardsPerRow = storage.GetInt($"{prefix}CardsPerRow", CardsPerRow);
            FlippedCardsPerMatch = storage.GetInt($"{prefix}FlippedCardsPerMatch", FlippedCardsPerMatch);
            TimePerGame = storage.GetFloat($"{prefix}TimePerGame", TimePerGame);
            TimeUntilUnflip = storage.GetFloat($"{prefix}TimeUntilUnflip", TimeUntilUnflip);
            PreviewTime = storage.GetFloat($"{prefix}PreviewTime", PreviewTime);
            Hearts = storage.GetInt($"{prefix}Hearts", Hearts);
            MoveLimit = storage.GetInt($"{prefix}MoveLimit", MoveLimit);
            MatchTimeBonus = storage.GetFloat($"{prefix}MatchTimeBonus", MatchTimeBonus);
            ShuffleEvery = storage.GetInt($"{prefix}ShuffleEvery", ShuffleEvery);
            Parade = storage.GetBool($"{prefix}Parade", Parade);
            Bombs = storage.GetInt($"{prefix}Bombs", Bombs);
            Wilds = storage.GetInt($"{prefix}Wilds", Wilds);
            Clocks = storage.GetInt($"{prefix}Clocks", Clocks);
            Peeks = storage.GetInt($"{prefix}Peeks", Peeks);
            Frozen = storage.GetInt($"{prefix}Frozen", Frozen);
        }

        public override void CopySettings(IGameSettings other)
        {
            if (!(other is MemoryCardsSettings source))
            {
                return;
            }
            if (source.CardTypes != null && source.CardTypes.Count > 0)
            {
                CardTypes = new List<Sprite>(source.CardTypes);
            }
            CardsAmount = source.CardsAmount;
            CardsPerRow = source.CardsPerRow;
            FlippedCardsPerMatch = source.FlippedCardsPerMatch;
            TimePerGame = source.TimePerGame;
            TimeUntilUnflip = source.TimeUntilUnflip;
            PreviewTime = source.PreviewTime;
            Hearts = source.Hearts;
            MoveLimit = source.MoveLimit;
            MatchTimeBonus = source.MatchTimeBonus;
            ShuffleEvery = source.ShuffleEvery;
            Parade = source.Parade;
            Bombs = source.Bombs;
            Wilds = source.Wilds;
            Clocks = source.Clocks;
            Peeks = source.Peeks;
            Frozen = source.Frozen;
        }

        /// <summary>Validates the rules against the settings' own animals, or without an animal count when it has none.</summary>
        public override bool AreSettingsValid(out string message)
        {
            int animals = CardTypes != null && CardTypes.Count > 0 ? CardTypes.Count : -1;
            return AreSettingsValid(animals, out message);
        }

        /// <summary>Validates the rules for a board dealt from <paramref name="animalPool"/> animals.</summary>
        public bool AreSettingsValid(int animalPool, out string message)
        {
            return ToRules().IsValid(animalPool, out message);
        }
    }
}
