using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Portfolio
{
    /// <summary>
    /// Holds all game settings which are user configurable.
    /// Allows saving and loading of settings using storage, or copying to another instance.
    /// </summary>
    [CreateAssetMenu(fileName = "GameSettings", menuName = "GameSettings", order = 1)]
    public class GameSettings : ScriptableObject
    {
        public List<Sprite> CardTypes;
        public int CardsAmount;
        public int CardsPerRow;
        public int FlippedCardsPerMatch;
        public float TimePerGame;
        public float TimeUntilUnflip;


        public void SaveSettings(IStorageStrategy storage, string prefix)
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

        public void LoadSettings(IStorageStrategy storage, string prefix)
        {
            CardsAmount = storage.GetInt($"{prefix}CardsAmount");
            CardsPerRow = storage.GetInt($"{prefix}CardsPerRow");
            FlippedCardsPerMatch = storage.GetInt($"{prefix}FlippedCardsPerMatch");
            TimePerGame = storage.GetFloat($"{prefix}TimePerGame");
            TimeUntilUnflip = storage.GetFloat($"{prefix}TimeUntilUnflip");
        }
            
        public void CopySettings(GameSettings other)
        {
            CardTypes = new List<Sprite>(other.CardTypes);
            CardsAmount = other.CardsAmount;
            CardsPerRow = other.CardsPerRow;
            FlippedCardsPerMatch = other.FlippedCardsPerMatch;
            TimePerGame = other.TimePerGame;
            TimeUntilUnflip = other.TimeUntilUnflip;
        }

        public bool AreSettingsValid(out string message)
        {
            message = "OK";
            if (CardsAmount / FlippedCardsPerMatch > CardTypes.Count ||
                FlippedCardsPerMatch < 2 ||
                CardsAmount % FlippedCardsPerMatch > 0 ||
                CardsAmount % CardsPerRow > 0 ||
                CardsAmount < 2 ||
                TimeUntilUnflip < 0 ||
                TimePerGame < 1 ||
                CardsPerRow < 1)
            {
                if (CardsAmount / FlippedCardsPerMatch > CardTypes.Count)
                    message = $"Cards Amount divided by Flipped Cards Per Match {CardsAmount / FlippedCardsPerMatch} is bigger than card types {CardTypes.Count}";
                else if (FlippedCardsPerMatch < 2) message = $"Flipped Cards Per Match {FlippedCardsPerMatch} has to be 2 or more";
                else if (CardsAmount % FlippedCardsPerMatch > 0) message = $"Cards Amount {CardsAmount} has to be a multiply of Flipped Cards Per Match {FlippedCardsPerMatch}";
                else if (CardsAmount % CardsPerRow > 0) message = $"Cards Amount {CardsAmount} has to be a multiply of Cards Per Row {CardsPerRow}";
                else if (CardsAmount < 2) message = $"Cards Amount {CardsAmount} has to be 2 or more";
                else if (TimeUntilUnflip < 0) message = $"Time Until Unflip {TimeUntilUnflip} has to be positive";
                else if (TimePerGame < 1) message = $"Time Per Game {TimePerGame} has to be one second or more";
                else if (CardsPerRow < 1) message = $"Cards Per Row {CardsPerRow} has to be one per row or more";
                    return false;
            }
            return true;
        }
    }

    public class GameSettingsException : Exception
    {
        public GameSettingsException(string message) : base(message) { }
    }
}
