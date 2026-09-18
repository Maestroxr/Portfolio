using System.Collections.Generic;
using Gamebox;
using UnityEngine;

namespace Portfolio.MemoryCards
{
    /// <summary>
    /// Holds all game settings which are user configurable.
    /// Allows saving and loading of settings using storage, or copying from another instance.
    /// </summary>
    [CreateAssetMenu(fileName = "MemoryCardsSettings", menuName = "Memory Cards/Settings", order = 1)]
    public class MemoryCardsSettings : GameSettings
    {
        public List<Sprite> CardTypes = new List<Sprite>();
        public int CardsAmount;
        public int CardsPerRow;
        public int FlippedCardsPerMatch;
        public float TimePerGame;
        public float TimeUntilUnflip;


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
        }

        public override void LoadSettings(IStorageStrategy storage, string prefix)
        {
            if (storage.DoesKeyExist($"{prefix}CardsAmount"))
            {
                CardsAmount = storage.GetInt($"{prefix}CardsAmount");
            }
            if (storage.DoesKeyExist($"{prefix}CardsPerRow"))
            {
                CardsPerRow = storage.GetInt($"{prefix}CardsPerRow");
            }
            if (storage.DoesKeyExist($"{prefix}FlippedCardsPerMatch"))
            {
                FlippedCardsPerMatch = storage.GetInt($"{prefix}FlippedCardsPerMatch");
            }
            if (storage.DoesKeyExist($"{prefix}TimePerGame"))
            {
                TimePerGame = storage.GetFloat($"{prefix}TimePerGame");
            }
            if (storage.DoesKeyExist($"{prefix}TimeUntilUnflip"))
            {
                TimeUntilUnflip = storage.GetFloat($"{prefix}TimeUntilUnflip");
            }
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
        }

        public override bool AreSettingsValid(out string message)
        {
            message = "OK";
            int cardTypes = CardTypes != null ? CardTypes.Count : 0;
            if (FlippedCardsPerMatch < 2)
            {
                message = $"Flipped Cards Per Match {FlippedCardsPerMatch} has to be 2 or more";
                return false;
            }
            if (CardsPerRow < 1)
            {
                message = $"Cards Per Row {CardsPerRow} has to be one per row or more";
                return false;
            }
            if (CardsAmount < 2)
            {
                message = $"Cards Amount {CardsAmount} has to be 2 or more";
                return false;
            }
            if (CardsAmount / FlippedCardsPerMatch > cardTypes)
            {
                message = $"Cards Amount divided by Flipped Cards Per Match {CardsAmount / FlippedCardsPerMatch} is bigger than card types {cardTypes}";
                return false;
            }
            if (CardsAmount % FlippedCardsPerMatch > 0)
            {
                message = $"Cards Amount {CardsAmount} has to be a multiply of Flipped Cards Per Match {FlippedCardsPerMatch}";
                return false;
            }
            if (CardsAmount % CardsPerRow > 0)
            {
                message = $"Cards Amount {CardsAmount} has to be a multiply of Cards Per Row {CardsPerRow}";
                return false;
            }
            if (TimeUntilUnflip < 0)
            {
                message = $"Time Until Unflip {TimeUntilUnflip} has to be positive";
                return false;
            }
            if (TimePerGame < 1)
            {
                message = $"Time Per Game {TimePerGame} has to be one second or more";
                return false;
            }
            return true;
        }
    }
}
