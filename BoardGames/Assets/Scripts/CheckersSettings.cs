using Gamebox;
using UnityEngine;

namespace Portfolio.Checkers
{
    /// <summary>
    /// What the player can tune: whether legal moves are shown, whether the board turns to the player to move in a game
    /// of two people, how long the computer pretends to think, the sound volume, and the rules and computer strength of
    /// a quick game. The settings panel edits a runtime copy of the asset.
    /// </summary>
    [CreateAssetMenu(fileName = "CheckersSettings", menuName = "Checkers/Settings", order = 1)]
    public sealed class CheckersSettings : GameSettings
    {
        [Tooltip("Mark the squares the selected piece may move to.")]
        public bool showMoves = true;
        [Tooltip("In a game of two people at this device, turn the camera to the player to move.")]
        public bool turnBoard;
        [Tooltip("Seconds the computer waits before each move.")]
        public float thinkingDelay = 0.6f;
        [Tooltip("The volume of the sound effects, 0 to 1.")]
        public float volume = 0.8f;
        [Tooltip("The rules of a quick game (0 English, 1 Russian, 2 International).")]
        public int variant;
        [Tooltip("The computer's strength in a quick game (0 Easy, 1 Normal, 2 Hard).")]
        public int computerLevel = 1;

        public VariantRules Rules => VariantRules.Of(variant);

        public override bool AreSettingsValid(out string message)
        {
            if (thinkingDelay < 0f || thinkingDelay > 10f)
            {
                message = $"Thinking delay {thinkingDelay} has to be between 0 and 10 seconds";
                return false;
            }
            if (volume < 0f || volume > 1f)
            {
                message = $"Volume {volume} has to be between 0 and 1";
                return false;
            }
            if (variant < 0 || variant >= VariantRules.All.Length)
            {
                message = $"Variant {variant} is not one of the {VariantRules.All.Length} rule sets";
                return false;
            }
            if (computerLevel < 0 || computerLevel >= CheckersAI.Levels)
            {
                message = $"Computer level {computerLevel} has to be between 0 and {CheckersAI.Levels - 1}";
                return false;
            }
            message = "OK";
            return true;
        }

        public override void SaveSettings(IStorageStrategy storage, string prefix)
        {
            if (!AreSettingsValid(out string error))
            {
                throw new GameSettingsException($"Cannot save invalid settings. Reason: {error}");
            }
            storage.SetBool($"{prefix}.ShowMoves", showMoves);
            storage.SetBool($"{prefix}.TurnBoard", turnBoard);
            storage.SetFloat($"{prefix}.ThinkingDelay", thinkingDelay);
            storage.SetFloat($"{prefix}.Volume", volume);
            storage.SetInt($"{prefix}.Variant", variant);
            storage.SetInt($"{prefix}.ComputerLevel", computerLevel);
        }

        public override void LoadSettings(IStorageStrategy storage, string prefix)
        {
            showMoves = storage.GetBool($"{prefix}.ShowMoves", showMoves);
            turnBoard = storage.GetBool($"{prefix}.TurnBoard", turnBoard);
            if (storage.DoesKeyExist($"{prefix}.ThinkingDelay"))
            {
                thinkingDelay = storage.GetFloat($"{prefix}.ThinkingDelay");
            }
            if (storage.DoesKeyExist($"{prefix}.Volume"))
            {
                volume = storage.GetFloat($"{prefix}.Volume");
            }
            variant = storage.GetInt($"{prefix}.Variant", variant);
            computerLevel = storage.GetInt($"{prefix}.ComputerLevel", computerLevel);
        }

        public override void CopySettings(IGameSettings other)
        {
            if (!(other is CheckersSettings source))
            {
                return;
            }
            showMoves = source.showMoves;
            turnBoard = source.turnBoard;
            thinkingDelay = source.thinkingDelay;
            volume = source.volume;
            variant = source.variant;
            computerLevel = source.computerLevel;
        }
    }
}
