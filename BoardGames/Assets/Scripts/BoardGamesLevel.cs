using Gamebox;
using UnityEngine;
using UnityEngine.Serialization;

namespace Portfolio.BoardGames
{
    /// <summary>
    /// One game of the campaign: the rule set it is played by, the strength of the computer, the side the player takes
    /// and the two targets that earn the second and third star. What the targets count depends on the game:
    /// <list type="bullet">
    /// <item>draughts: <see cref="PiecesKept"/> pieces left at the end, <see cref="MaxPlies"/> plies the game may take;</item>
    /// <item>chess: <see cref="PiecesKept"/> own pieces other than the king left at the end (1 to 15), <see cref="MaxPlies"/> plies;</item>
    /// <item>backgammon: <see cref="PiecesKept"/> the points the win must be worth before the cube (1 single, 2 gammon,
    /// 3 backgammon), <see cref="MaxPlies"/> the decisions the game may take (<see cref="IBoardGame.Turn"/>: rolls, plays,
    /// cube actions).</item>
    /// </list>
    /// Title and Description are English keys of the localization table.
    /// </summary>
    [CreateAssetMenu(fileName = "BoardGamesLevel", menuName = "Board Games/Level", order = 2)]
    public sealed class BoardGamesLevel : GameLevel
    {
        /// <summary>The most own pieces other than the king a chess side has.</summary>
        public const int MaxChessPieces = 15;

        /// <summary>The most a backgammon win is worth before the cube (a backgammon).</summary>
        public const int MaxBackgammonPoints = 3;

        [field: SerializeField, FormerlySerializedAs("<Variant>k__BackingField")]
        public RuleSet RuleSet { get; private set; }

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

        public GameMode Mode => RuleSets.ModeOf(RuleSet);

        /// <summary>The draughts rules of a draughts level (English for the other games).</summary>
        public VariantRules Rules => VariantRules.Of(RuleSets.CheckersVariant(RuleSet));

        public Side Side => PlayerSide == (int)Side.Dark ? Side.Dark : Side.Light;

        public override bool IsLevelValid(out string message)
        {
            if (!RuleSets.IsValid((int)RuleSet))
            {
                message = $"Rule set {(int)RuleSet} is not one of the {RuleSets.Count} rule sets";
                return false;
            }
            if (ComputerLevel < 0 || ComputerLevel >= RuleSets.ComputerLevels)
            {
                message = $"Computer level {ComputerLevel} has to be between 0 and {RuleSets.ComputerLevels - 1}";
                return false;
            }
            if (PlayerSide < 0 || PlayerSide > 1)
            {
                message = $"Player side {PlayerSide} has to be 0 (Light) or 1 (Dark)";
                return false;
            }
            int most = MostPiecesKept;
            if (PiecesKept < 1 || PiecesKept > most)
            {
                message = $"Pieces kept {PiecesKept} has to be between 1 and {most}";
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

        /// <summary>The highest <see cref="PiecesKept"/> the level's game allows.</summary>
        private int MostPiecesKept
        {
            get
            {
                switch (Mode)
                {
                    case GameMode.Chess:
                        return MaxChessPieces;
                    case GameMode.Backgammon:
                        return MaxBackgammonPoints;
                    default:
                        return Rules.MenPerSide;
                }
            }
        }

#if UNITY_EDITOR
        /// <summary>Editor only: the content builder fills the level in.</summary>
        public void Configure(RuleSet ruleSet, int computerLevel, int playerSide, int piecesKept, int maxPlies, string description)
        {
            RuleSet = ruleSet;
            ComputerLevel = computerLevel;
            PlayerSide = playerSide;
            PiecesKept = piecesKept;
            MaxPlies = maxPlies;
            Description = description;
        }

        /// <summary>Editor only: the same with the rule set's number (0 to 2 are the draughts variants).</summary>
        public void Configure(int ruleSet, int computerLevel, int playerSide, int piecesKept, int maxPlies, string description)
        {
            Configure(RuleSets.Of(ruleSet), computerLevel, playerSide, piecesKept, maxPlies, description);
        }
#endif
    }
}
