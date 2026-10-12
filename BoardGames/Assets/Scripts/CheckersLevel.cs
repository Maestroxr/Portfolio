using Gamebox;
using UnityEngine;

namespace Portfolio.Checkers
{
    /// <summary>
    /// One game of the campaign: the rules it is played by, the strength of the computer, the side the player takes and
    /// the two targets that earn the second and third star (pieces kept at the end, and plies the game may take).
    /// Title and Description are English keys of the localization table.
    /// </summary>
    [CreateAssetMenu(fileName = "CheckersLevel", menuName = "Checkers/Level", order = 2)]
    public sealed class CheckersLevel : GameLevel
    {
        [field: SerializeField]
        public int Variant { get; private set; }

        [field: SerializeField]
        public int ComputerLevel { get; private set; } = 1;

        [field: SerializeField]
        public int PlayerSide { get; private set; }

        [field: SerializeField]
        public int PiecesKept { get; private set; } = 6;

        [field: SerializeField]
        public int MaxPlies { get; private set; } = 80;

        [field: SerializeField, TextArea(2, 6)]
        public string Description { get; private set; }

        public VariantRules Rules => VariantRules.Of(Variant);

        public Side Side => PlayerSide == (int)Side.Dark ? Side.Dark : Side.Light;

        public override bool IsLevelValid(out string message)
        {
            if (Variant < 0 || Variant >= VariantRules.All.Length)
            {
                message = $"Variant {Variant} is not one of the {VariantRules.All.Length} rule sets";
                return false;
            }
            if (ComputerLevel < 0 || ComputerLevel >= CheckersAI.Levels)
            {
                message = $"Computer level {ComputerLevel} has to be between 0 and {CheckersAI.Levels - 1}";
                return false;
            }
            if (PlayerSide < 0 || PlayerSide > 1)
            {
                message = $"Player side {PlayerSide} has to be 0 (Light) or 1 (Dark)";
                return false;
            }
            if (PiecesKept < 1 || PiecesKept > Rules.MenPerSide)
            {
                message = $"Pieces kept {PiecesKept} has to be between 1 and {Rules.MenPerSide}";
                return false;
            }
            if (MaxPlies < 2)
            {
                message = $"Max plies {MaxPlies} has to be at least 2";
                return false;
            }
            if (string.IsNullOrEmpty(Title))
            {
                message = "The level has no title";
                return false;
            }
            message = "OK";
            return true;
        }

#if UNITY_EDITOR
        /// <summary>Editor only: the content builder fills the level in.</summary>
        public void Configure(int variant, int computerLevel, int playerSide, int piecesKept, int maxPlies, string description)
        {
            Variant = variant;
            ComputerLevel = computerLevel;
            PlayerSide = playerSide;
            PiecesKept = piecesKept;
            MaxPlies = maxPlies;
            Description = description;
        }
#endif
    }
}
