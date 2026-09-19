using Gamebox;
using Gamebox.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.MemoryCards
{
    /// <summary>
    /// Settings panel of the Memory Cards module: the rules of free play (board size, cards per match, timings,
    /// limits and special cards). The shared <see cref="SettingsUI"/> handles the default/custom switch and the save,
    /// load and back buttons.
    /// </summary>
    public class MemoryCardsSettingsUI : SettingsUI
    {
        [SerializeField] internal TMP_InputField CardsAmount;
        [SerializeField] internal TMP_InputField CardsPerRow;
        [SerializeField] internal TMP_InputField FlippedCardsPerMatch;
        [SerializeField] internal TMP_InputField TimePerGame;
        [SerializeField] internal TMP_InputField TimeUntilUnflip;
        [SerializeField] internal TMP_InputField PreviewTime;
        [SerializeField] internal TMP_InputField Hearts;
        [SerializeField] internal TMP_InputField MoveLimit;
        [SerializeField] internal TMP_InputField ShuffleEvery;
        [SerializeField] internal TMP_InputField Bombs;
        [SerializeField] internal TMP_InputField Wilds;
        [SerializeField] internal TMP_InputField Clocks;
        [SerializeField] internal TMP_InputField Peeks;
        [SerializeField] internal TMP_InputField Frozen;

        private TMP_InputField[] Fields => new[]
        {
            CardsAmount, CardsPerRow, FlippedCardsPerMatch, TimePerGame, TimeUntilUnflip, PreviewTime, Hearts,
            MoveLimit, ShuffleEvery, Bombs, Wilds, Clocks, Peeks, Frozen
        };

        protected override void Awake()
        {
            base.Awake();
            foreach (Button button in new[] { CustomSettingsButton, SaveSettingsButton, LoadSettingsButton })
            {
                if (button != null)
                {
                    button.onClick.AddListener(() => (Manager as MemoryCardsGameManager)?.PlayClick());
                }
            }
        }

        protected override void UpdateCustomSettingsLabel(bool usingDefault)
        {
            if (CustomSettingsButtonText != null)
            {
                CustomSettingsButtonText.text = usingDefault ? "Use Custom Rules" : "Use Default Rules";
                CustomSettingsButtonText.color = new Color(0.17f, 0.18f, 0.26f);
            }
        }

        protected override void SetInputsInteractable(bool interactable)
        {
            foreach (TMP_InputField field in Fields)
            {
                SetInteractable(field, interactable);
            }
        }

        public override void UpdateFromSettings(IGameSettings settings)
        {
            if (!(settings is MemoryCardsSettings cards))
            {
                return;
            }
            SetText(CardsAmount, cards.CardsAmount);
            SetText(CardsPerRow, cards.CardsPerRow);
            SetText(FlippedCardsPerMatch, cards.FlippedCardsPerMatch);
            SetText(TimePerGame, cards.TimePerGame);
            SetText(TimeUntilUnflip, cards.TimeUntilUnflip);
            SetText(PreviewTime, cards.PreviewTime);
            SetText(Hearts, cards.Hearts);
            SetText(MoveLimit, cards.MoveLimit);
            SetText(ShuffleEvery, cards.ShuffleEvery);
            SetText(Bombs, cards.Bombs);
            SetText(Wilds, cards.Wilds);
            SetText(Clocks, cards.Clocks);
            SetText(Peeks, cards.Peeks);
            SetText(Frozen, cards.Frozen);
        }

        public override void CopyToSettings(IGameSettings settings)
        {
            if (!(settings is MemoryCardsSettings cards))
            {
                return;
            }
            cards.CardsAmount = ParseInt(CardsAmount, cards.CardsAmount);
            cards.CardsPerRow = ParseInt(CardsPerRow, cards.CardsPerRow);
            cards.FlippedCardsPerMatch = ParseInt(FlippedCardsPerMatch, cards.FlippedCardsPerMatch);
            cards.TimePerGame = ParseFloat(TimePerGame, cards.TimePerGame);
            cards.TimeUntilUnflip = ParseFloat(TimeUntilUnflip, cards.TimeUntilUnflip);
            cards.PreviewTime = ParseFloat(PreviewTime, cards.PreviewTime);
            cards.Hearts = ParseInt(Hearts, cards.Hearts);
            cards.MoveLimit = ParseInt(MoveLimit, cards.MoveLimit);
            cards.ShuffleEvery = ParseInt(ShuffleEvery, cards.ShuffleEvery);
            cards.Bombs = ParseInt(Bombs, cards.Bombs);
            cards.Wilds = ParseInt(Wilds, cards.Wilds);
            cards.Clocks = ParseInt(Clocks, cards.Clocks);
            cards.Peeks = ParseInt(Peeks, cards.Peeks);
            cards.Frozen = ParseInt(Frozen, cards.Frozen);
        }
    }
}
