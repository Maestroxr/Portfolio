using Gamebox;
using Gamebox.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.BoardGames
{
    /// <summary>
    /// The settings panel: whether legal moves are marked, whether the board turns to the player to move in a hot seat
    /// game, how long the computer thinks, the sound volume, the draughts rules of a quick game of checkers, whether a
    /// quick game of backgammon plays with the doubling cube, and the computer's strength in a quick game. The base
    /// class handles the default/custom switch and the save, load and back buttons.
    /// </summary>
    public sealed class BoardGamesSettingsUI : SettingsUI
    {
        [SerializeField] private Toggle showMoves;
        [SerializeField] private Toggle turnBoard;
        [SerializeField] private Slider thinkingDelay;
        [SerializeField] private Slider volume;
        [Tooltip("The draughts rules of a quick game of checkers: English, Russian, International.")]
        [SerializeField] private ChoiceSelector rules;
        [Tooltip("Whether a quick game of backgammon plays with the doubling cube.")]
        [SerializeField] private Toggle doublingCube;
        [SerializeField] private Slider computerLevel;

        public override void UpdateFromSettings(IGameSettings settings)
        {
            if (!(settings is BoardGamesSettings options))
            {
                return;
            }
            SetToggle(showMoves, options.showMoves);
            SetToggle(turnBoard, options.turnBoard);
            SetSlider(thinkingDelay, options.thinkingDelay);
            SetSlider(volume, options.volume);
            SetToggle(doublingCube, options.doublingCube);
            SetSlider(computerLevel, options.computerLevel);
            if (rules != null)
            {
                rules.SetValueWithoutNotify(options.variant);
            }
        }

        public override void CopyToSettings(IGameSettings settings)
        {
            if (!(settings is BoardGamesSettings options))
            {
                return;
            }
            options.showMoves = Read(showMoves, options.showMoves);
            options.turnBoard = Read(turnBoard, options.turnBoard);
            options.thinkingDelay = Read(thinkingDelay, options.thinkingDelay);
            options.volume = Read(volume, options.volume);
            options.doublingCube = Read(doublingCube, options.doublingCube);
            options.computerLevel = Mathf.RoundToInt(Read(computerLevel, options.computerLevel));
            if (rules != null)
            {
                options.variant = rules.Value;
            }
        }

        protected override void SetInputsInteractable(bool interactable)
        {
            SetInteractable(showMoves, interactable);
            SetInteractable(turnBoard, interactable);
            SetInteractable(thinkingDelay, interactable);
            SetInteractable(volume, interactable);
            SetInteractable(doublingCube, interactable);
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
