using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio
{
    public class SettingsUI : MonoBehaviour
    {
        [SerializeField] private Text CustomSettingsButtonText;
        [SerializeField] private Button SaveSettingsButton;
        [SerializeField] private Button LoadSettingsButton;
        [SerializeField] private InputField CardsAmount;
        [SerializeField] private InputField CardsPerRow;
        [SerializeField] private InputField FlippedCardsPerMatch;
        [SerializeField] private InputField TimePerGame;
        [SerializeField] private InputField TimeUntilUnflip;
        [SerializeField] private GameSettings DefaultSettings;
        [SerializeField] private bool UsingDefaultSettings;
        private const string customSettingsPrefix = "CustomSettings";


        public void UsingDefault(bool usingDefault)
        {
            if (usingDefault)
            {
                UpdateFromSettings(DefaultSettings);
                CardsAmount.interactable = false;
                CardsPerRow.interactable = false;
                FlippedCardsPerMatch.interactable = false;
                TimePerGame.interactable = false;
                TimeUntilUnflip.interactable = false;
                SaveSettingsButton.interactable = false;
                CustomSettingsButtonText.text = "Enable Custom Settings";
                CustomSettingsButtonText.color = Color.green;
            }
            else
            {
                CardsAmount.interactable = true;
                CardsPerRow.interactable = true;
                FlippedCardsPerMatch.interactable = true;
                TimePerGame.interactable = true;
                TimeUntilUnflip.interactable = true;
                SaveSettingsButton.interactable = true;
                CustomSettingsButtonText.text = "Restore Default Settings";
                CustomSettingsButtonText.color = Color.red;
            }
            UsingDefaultSettings = usingDefault;
        }


        public void SaveSettings(IStorageStrategy storage, GameSettings settings)
        {
            CopyToSettings(settings);
            settings.SaveSettings(storage, customSettingsPrefix);
        }


        public void LoadSettings(IStorageStrategy storage, GameSettings settings, string settingsPrefix)
        {
            settings.LoadSettings(storage, settingsPrefix);
            UpdateFromSettings(settings);
        }


        public void UpdateFromSettings(GameSettings settings)
        {
            CardsAmount.text = settings.CardsAmount.ToString();
            CardsPerRow.text = settings.CardsPerRow.ToString();
            FlippedCardsPerMatch.text = settings.FlippedCardsPerMatch.ToString();
            TimePerGame.text = settings.TimePerGame.ToString();
            TimeUntilUnflip.text = settings.TimeUntilUnflip.ToString();
        }

        public void CopyToSettings(GameSettings settings)
        {
            settings.CardsAmount = int.Parse(CardsAmount.text);
            settings.CardsPerRow = int.Parse(CardsPerRow.text);
            settings.FlippedCardsPerMatch = int.Parse(FlippedCardsPerMatch.text);
            settings.TimePerGame = float.Parse(TimePerGame.text);
            settings.TimeUntilUnflip = float.Parse(TimeUntilUnflip.text);
        }
    }
}