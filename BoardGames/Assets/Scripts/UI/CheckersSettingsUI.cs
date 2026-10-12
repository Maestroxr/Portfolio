using Gamebox;
using Gamebox.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Checkers
{
    /// <summary>
    /// The settings panel: whether legal moves are marked, whether the board turns to the player to move in a hot seat
    /// game, how long the computer thinks, the sound volume, and the rules and computer strength of a quick game. The
    /// base class handles the default/custom switch and the save, load and back buttons.
    /// </summary>
    public sealed class CheckersSettingsUI : SettingsUI
    {
        [SerializeField] private Toggle showMoves;
        [SerializeField] private Toggle turnBoard;
        [SerializeField] private Slider thinkingDelay;
        [SerializeField] private Slider volume;
        [SerializeField] private ChoiceSelector rules;
        [SerializeField] private Slider computerLevel;

        public override void UpdateFromSettings(IGameSettings settings)
        {
            if (!(settings is CheckersSettings checkers))
            {
                return;
            }
            SetToggle(showMoves, checkers.showMoves);
            SetToggle(turnBoard, checkers.turnBoard);
            SetSlider(thinkingDelay, checkers.thinkingDelay);
            SetSlider(volume, checkers.volume);
            SetSlider(computerLevel, checkers.computerLevel);
            if (rules != null)
            {
                rules.SetValueWithoutNotify(checkers.variant);
            }
        }

        public override void CopyToSettings(IGameSettings settings)
        {
            if (!(settings is CheckersSettings checkers))
            {
                return;
            }
            checkers.showMoves = Read(showMoves, checkers.showMoves);
            checkers.turnBoard = Read(turnBoard, checkers.turnBoard);
            checkers.thinkingDelay = Read(thinkingDelay, checkers.thinkingDelay);
            checkers.volume = Read(volume, checkers.volume);
            checkers.computerLevel = Mathf.RoundToInt(Read(computerLevel, checkers.computerLevel));
            if (rules != null)
            {
                checkers.variant = rules.Value;
            }
        }

        protected override void SetInputsInteractable(bool interactable)
        {
            SetInteractable(showMoves, interactable);
            SetInteractable(turnBoard, interactable);
            SetInteractable(thinkingDelay, interactable);
            SetInteractable(volume, interactable);
            SetInteractable(computerLevel, interactable);
            if (rules != null)
            {
                rules.Interactable = interactable;
            }
        }

        private static void SetToggle(Toggle toggle, bool value)
        {
            if (toggle != null)
            {
                toggle.SetIsOnWithoutNotify(value);
            }
        }

        private static void SetSlider(Slider slider, float value)
        {
            if (slider != null)
            {
                slider.SetValueWithoutNotify(value);
            }
        }

        private static bool Read(Toggle toggle, bool fallback)
        {
            return toggle != null ? toggle.isOn : fallback;
        }

        private static float Read(Slider slider, float fallback)
        {
            return slider != null ? slider.value : fallback;
        }
    }
}
