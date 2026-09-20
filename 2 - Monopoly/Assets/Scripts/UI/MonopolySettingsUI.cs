using Gamebox;
using Gamebox.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Monopoly
{
    /// <summary>
    /// The house rules panel: the custom rules the "Custom Rules" mode plays with. Starting cash, a round limit and
    /// dealt title deeds, and switches for auctions, the speed die, the Free Parking jackpot, double salary on GO, no
    /// rent from jail, the party cards and fast animations. The base class handles the default/custom switch and the
    /// save, load and back buttons.
    /// </summary>
    public class MonopolySettingsUI : SettingsUI
    {
        [SerializeField] private TMP_InputField startingCash;
        [SerializeField] private TMP_InputField roundLimit;
        [SerializeField] private TMP_InputField dealtProperties;
        [SerializeField] private Toggle auctions;
        [SerializeField] private Toggle speedDie;
        [SerializeField] private Toggle jackpot;
        [SerializeField] private Toggle doubleGo;
        [SerializeField] private Toggle noRentInJail;
        [SerializeField] private Toggle partyCards;
        [SerializeField] private Toggle fastAnimations;

        public override void UpdateFromSettings(IGameSettings settings)
        {
            if (!(settings is MonopolySettings monopoly))
            {
                return;
            }
            RuleSet rules = monopoly.Rules;
            SetText(startingCash, rules.startingCash);
            SetText(roundLimit, rules.roundLimit);
            SetText(dealtProperties, rules.dealtProperties);
            SetToggle(auctions, rules.auctions);
            SetToggle(speedDie, rules.speedDie);
            SetToggle(jackpot, rules.freeParkingJackpot);
            SetToggle(doubleGo, rules.doubleSalaryOnGo);
            SetToggle(noRentInJail, rules.noRentInJail);
            SetToggle(partyCards, rules.partyCards);
            SetToggle(fastAnimations, monopoly.AnimationSpeed > 1.2f);
        }

        public override void CopyToSettings(IGameSettings settings)
        {
            if (!(settings is MonopolySettings monopoly))
            {
                return;
            }
            RuleSet rules = monopoly.Rules;
            rules.startingCash = ParseInt(startingCash, rules.startingCash);
            rules.roundLimit = ParseInt(roundLimit, rules.roundLimit);
            rules.dealtProperties = ParseInt(dealtProperties, rules.dealtProperties);
            rules.bankruptciesToEnd = rules.dealtProperties > 0 ? 2 : 0;
            rules.housesForHotel = rules.dealtProperties > 0 ? 3 : 4;
            rules.auctions = Read(auctions, rules.auctions);
            rules.speedDie = Read(speedDie, rules.speedDie);
            rules.freeParkingJackpot = Read(jackpot, rules.freeParkingJackpot);
            rules.doubleSalaryOnGo = Read(doubleGo, rules.doubleSalaryOnGo);
            rules.noRentInJail = Read(noRentInJail, rules.noRentInJail);
            rules.partyCards = Read(partyCards, rules.partyCards);
            monopoly.AnimationSpeed = Read(fastAnimations, monopoly.AnimationSpeed > 1.2f) ? 1.7f : 1f;
        }

        protected override void SetInputsInteractable(bool interactable)
        {
            SetInteractable(startingCash, interactable);
            SetInteractable(roundLimit, interactable);
            SetInteractable(dealtProperties, interactable);
            SetInteractable(auctions, interactable);
            SetInteractable(speedDie, interactable);
            SetInteractable(jackpot, interactable);
            SetInteractable(doubleGo, interactable);
            SetInteractable(noRentInJail, interactable);
            SetInteractable(partyCards, interactable);
            SetInteractable(fastAnimations, interactable);
        }

        protected override void UpdateCustomSettingsLabel(bool usingDefault)
        {
            if (CustomSettingsButtonText == null)
            {
                return;
            }
            CustomSettingsButtonText.text = usingDefault ? "Edit house rules" : "Back to the standard rules";
            CustomSettingsButtonText.color = Color.white;
        }

        private static void SetToggle(Toggle toggle, bool value)
        {
            if (toggle != null)
            {
                toggle.SetIsOnWithoutNotify(value);
            }
        }

        private static bool Read(Toggle toggle, bool fallback)
        {
            return toggle != null ? toggle.isOn : fallback;
        }
    }
}
