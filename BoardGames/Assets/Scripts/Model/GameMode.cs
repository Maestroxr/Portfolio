namespace Portfolio.BoardGames
{
    /// <summary>The three games of the package.</summary>
    public enum GameMode
    {
        Checkers = 0,
        Chess = 1,
        Backgammon = 2
    }

    /// <summary>
    /// The rule sets a table is played by: the three of draughts, chess and backgammon. The number is the level of a room
    /// online, the rule set of a campaign level and of a saved game, so the numbers never change.
    /// </summary>
    public enum RuleSet
    {
        EnglishDraughts = 0,
        RussianDraughts = 1,
        InternationalDraughts = 2,
        Chess = 3,
        Backgammon = 4
    }

    /// <summary>What the rule sets have in common: their mode, their names, who moves first.</summary>
    public static class RuleSets
    {
        public const int Count = 5;

        /// <summary>The strengths of the computer in every game: 0 Easy, 1 Normal, 2 Hard.</summary>
        public const int ComputerLevels = 3;

        public static readonly RuleSet[] All =
        {
            RuleSet.EnglishDraughts, RuleSet.RussianDraughts, RuleSet.InternationalDraughts, RuleSet.Chess, RuleSet.Backgammon
        };

        public static readonly GameMode[] Modes = { GameMode.Checkers, GameMode.Chess, GameMode.Backgammon };

        /// <summary>The rule set of a number from data or the network; out of range numbers clamp.</summary>
        public static RuleSet Of(int index)
        {
            return index <= 0 ? RuleSet.EnglishDraughts : index >= Count ? RuleSet.Backgammon : (RuleSet)index;
        }

        public static bool IsValid(int index)
        {
            return index >= 0 && index < Count;
        }

        public static GameMode ModeOf(RuleSet ruleSet)
        {
            switch (ruleSet)
            {
                case RuleSet.Chess:
                    return GameMode.Chess;
                case RuleSet.Backgammon:
                    return GameMode.Backgammon;
                default:
                    return GameMode.Checkers;
            }
        }

        public static GameMode ModeOf(int ruleSet)
        {
            return ModeOf(Of(ruleSet));
        }

        /// <summary>The draughts variant of a draughts rule set (English for the other games).</summary>
        public static Variant CheckersVariant(RuleSet ruleSet)
        {
            return ModeOf(ruleSet) == GameMode.Checkers ? (Variant)(int)ruleSet : Variant.English;
        }

        /// <summary>The rule set of a draughts variant.</summary>
        public static RuleSet OfVariant(Variant variant)
        {
            return (RuleSet)(int)VariantRules.Of(variant).Variant;
        }

        /// <summary>The rule set a mode plays: the given draughts variant for draughts, the one rule set of the others.</summary>
        public static RuleSet Of(GameMode mode, int checkersVariant = 0)
        {
            switch (mode)
            {
                case GameMode.Chess:
                    return RuleSet.Chess;
                case GameMode.Backgammon:
                    return RuleSet.Backgammon;
                default:
                    return OfVariant(VariantRules.Of(checkersVariant).Variant);
            }
        }

        /// <summary>The English name of a rule set (a key of the localization table), as the lobby and the setup show it.</summary>
        public static string NameKey(RuleSet ruleSet)
        {
            switch (ruleSet)
            {
                case RuleSet.EnglishDraughts:
                    return "English draughts";
                case RuleSet.RussianDraughts:
                    return "Russian draughts";
                case RuleSet.InternationalDraughts:
                    return "International draughts";
                case RuleSet.Chess:
                    return "Chess";
                default:
                    return "Backgammon";
            }
        }

        /// <summary>The English name of a mode (a key of the localization table).</summary>
        public static string ModeNameKey(GameMode mode)
        {
            switch (mode)
            {
                case GameMode.Chess:
                    return "Chess";
                case GameMode.Backgammon:
                    return "Backgammon";
                default:
                    return "Checkers";
            }
        }

        /// <summary>
        /// The side that moves first: the draughts variant's, White in chess and in backgammon (where, by default, the
        /// opening roll decides who actually starts; see <see cref="TableSetup.openingRoll"/>).
        /// </summary>
        public static Side FirstMover(RuleSet ruleSet)
        {
            return ModeOf(ruleSet) == GameMode.Checkers ? VariantRules.Of(CheckersVariant(ruleSet)).FirstMover : Side.Light;
        }

        /// <summary>The side of a mode's name: Light and Dark in draughts, White and Black elsewhere.</summary>
        public static string SideNameKey(RuleSet ruleSet, Side side)
        {
            return Sides.NameKey(side, ModeOf(ruleSet));
        }
    }
}
