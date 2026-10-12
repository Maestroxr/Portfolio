using Gamebox;
using UnityEngine;

namespace Portfolio.BoardGames
{
    /// <summary>
    /// What the player can tune: whether legal moves are shown, whether the board turns to the player to move in a game
    /// of two people, how long the computer pretends to think, the sound volume, the game the title shows, and the
    /// draughts rules, doubling cube and computer strength of a quick game. The settings panel edits a runtime copy of
    /// the asset.
    /// </summary>
    [CreateAssetMenu(fileName = "BoardGamesSettings", menuName = "Board Games/Settings", order = 1)]
    public sealed class BoardGamesSettings : GameSettings
    {
        [Tooltip("Mark the squares the selected piece may move to.")]
        public bool showMoves = true;
        [Tooltip("In a game of two people at this device, turn the camera to the player to move.")]
        public bool turnBoard;
        [Tooltip("Seconds the computer waits before each move.")]
        public float thinkingDelay = 0.6f;
        [Tooltip("The volume of the sound effects, 0 to 1.")]
        public float volume = 0.8f;
        [Tooltip("The game the title shows and its buttons play (0 Checkers, 1 Chess, 2 Backgammon).")]
        public int mode;
        [Tooltip("The draughts rules of a quick game of checkers (0 English, 1 Russian, 2 International).")]
        public int variant;
        [Tooltip("Whether a quick game of backgammon plays with the doubling cube (and the local setup's default).")]
        public bool doublingCube = true;
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
            if (mode < 0 || mode >= RuleSets.Modes.Length)
            {
                message = $"Mode {mode} is not one of the {RuleSets.Modes.Length} games";
                return false;
            }
            if (computerLevel < 0 || computerLevel >= RuleSets.ComputerLevels)
            {
                message = $"Computer level {computerLevel} has to be between 0 and {RuleSets.ComputerLevels - 1}";
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
            storage.SetInt($"{prefix}.Mode", mode);
            storage.SetInt($"{prefix}.Variant", variant);
            storage.SetBool($"{prefix}.DoublingCube", doublingCube);
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
            mode = storage.GetInt($"{prefix}.Mode", mode);
            variant = storage.GetInt($"{prefix}.Variant", variant);
            doublingCube = storage.GetBool($"{prefix}.DoublingCube", doublingCube);
            computerLevel = storage.GetInt($"{prefix}.ComputerLevel", computerLevel);
        }

        public override void CopySettings(IGameSettings other)
        {
            if (!(other is BoardGamesSettings source))
            {
                return;
            }
            showMoves = source.showMoves;
            turnBoard = source.turnBoard;
            thinkingDelay = source.thinkingDelay;
            volume = source.volume;
            mode = source.mode;
            variant = source.variant;
            doublingCube = source.doublingCube;
            computerLevel = source.computerLevel;
        }
    }
}
