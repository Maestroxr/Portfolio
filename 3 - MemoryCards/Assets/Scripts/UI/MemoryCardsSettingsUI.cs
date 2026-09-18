using Gamebox;
using Gamebox.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.MemoryCards
{
    /// <summary>Settings panel of the Memory Cards module: deck size, layout, match size and timings.</summary>
    public class MemoryCardsSettingsUI : SettingsUI
    {
        [SerializeField] private Text customSettingsButtonText;
        [SerializeField] private InputField CardsAmount;
        [SerializeField] private InputField CardsPerRow;
        [SerializeField] private InputField FlippedCardsPerMatch;
        [SerializeField] private InputField TimePerGame;
        [SerializeField] private InputField TimeUntilUnflip;


        protected override void UpdateCustomSettingsLabel(bool usingDefault)
        {
            base.UpdateCustomSettingsLabel(usingDefault);
            if (customSettingsButtonText == null)
            {
                return;
            }
            customSettingsButtonText.text = usingDefault ? "Enable Custom Settings" : "Restore Default Settings";
            customSettingsButtonText.color = usingDefault ? Color.green : Color.red;
        }


        protected override void SetInputsInteractable(bool interactable)
        {
            SetInteractable(CardsAmount, interactable);
            SetInteractable(CardsPerRow, interactable);
            SetInteractable(FlippedCardsPerMatch, interactable);
            SetInteractable(TimePerGame, interactable);
            SetInteractable(TimeUntilUnflip, interactable);
        }


        public override void UpdateFromSettings(IGameSettings settings)
        {
            if (!(settings is MemoryCardsSettings cardSettings))
            {
                return;
            }
            SetText(CardsAmount, cardSettings.CardsAmount);
            SetText(CardsPerRow, cardSettings.CardsPerRow);
            SetText(FlippedCardsPerMatch, cardSettings.FlippedCardsPerMatch);
            SetText(TimePerGame, cardSettings.TimePerGame);
            SetText(TimeUntilUnflip, cardSettings.TimeUntilUnflip);
        }


        public override void CopyToSettings(IGameSettings settings)
        {
            if (!(settings is MemoryCardsSettings cardSettings))
            {
                return;
            }
            cardSettings.CardsAmount = ParseInt(CardsAmount, cardSettings.CardsAmount);
            cardSettings.CardsPerRow = ParseInt(CardsPerRow, cardSettings.CardsPerRow);
            cardSettings.FlippedCardsPerMatch = ParseInt(FlippedCardsPerMatch, cardSettings.FlippedCardsPerMatch);
            cardSettings.TimePerGame = ParseFloat(TimePerGame, cardSettings.TimePerGame);
            cardSettings.TimeUntilUnflip = ParseFloat(TimeUntilUnflip, cardSettings.TimeUntilUnflip);
        }
    }
}
