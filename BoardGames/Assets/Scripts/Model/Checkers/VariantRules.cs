namespace Portfolio.BoardGames
{
    /// <summary>The rule sets the game is played by; the number is the room level online, so it never changes.</summary>
    public enum Variant
    {
        English = 0,
        Russian = 1,
        International = 2
    }

    /// <summary>
    /// What tells the three rule sets apart: the size of the board, how men and kings move and capture, what happens to
    /// a man that reaches the far rank in the middle of a capture, and who moves first. One instance per variant;
    /// <see cref="Of(Variant)"/> hands it out.
    /// </summary>
    public sealed class VariantRules
    {
        /// <summary>Quiet plies (no capture, no man moved) in a row that end the game in a draw.</summary>
        public const int DrawPliesCount = 50;

        private static readonly VariantRules[] all =
        {
            new VariantRules(Variant.English, 8, 3, false, false, false, false, Side.Dark, "English",
                "Men capture forward only, kings move one step; a man crowned stops there."),
            new VariantRules(Variant.Russian, 8, 3, true, true, false, true, Side.Light, "Russian",
                "Men capture backward too, kings fly; a man crowned mid-capture goes on as a king."),
            new VariantRules(Variant.International, 10, 4, true, true, true, false, Side.Light, "International",
                "On a 10x10 board: flying kings, and a capture must take the most pieces it can.")
        };

        /// <summary>English, Russian, International.</summary>
        public static readonly Variant[] All = { Variant.English, Variant.Russian, Variant.International };

        private VariantRules(Variant variant, int size, int rowsOfMen, bool menCaptureBackward, bool flyingKings,
            bool majorityCapture, bool promoteDuringCapture, Side firstMover, string nameKey, string descriptionKey)
        {
            Variant = variant;
            Size = size;
            RowsOfMen = rowsOfMen;
            MenCaptureBackward = menCaptureBackward;
            FlyingKings = flyingKings;
            MajorityCapture = majorityCapture;
            PromoteDuringCapture = promoteDuringCapture;
            FirstMover = firstMover;
            NameKey = nameKey;
            DescriptionKey = descriptionKey;
        }

        public static VariantRules Of(Variant variant)
        {
            return Of((int)variant);
        }

        /// <summary>The rules of variant number <paramref name="variant"/>, clamped into the known ones.</summary>
        public static VariantRules Of(int variant)
        {
            if (variant < 0)
            {
                variant = 0;
            }
            if (variant >= all.Length)
            {
                variant = all.Length - 1;
            }
            return all[variant];
        }

        public Variant Variant { get; }

        /// <summary>Squares along one edge: 8 or 10.</summary>
        public int Size { get; }

        /// <summary>Rows each side's men fill at the start: 3 on 8x8, 4 on 10x10.</summary>
        public int RowsOfMen { get; }

        /// <summary>Whether a man may capture towards its own home rank.</summary>
        public bool MenCaptureBackward { get; }

        /// <summary>Whether a king slides any distance and captures at any distance (else one step, a short jump).</summary>
        public bool FlyingKings { get; }

        /// <summary>Whether only the sequences that take the most pieces are legal.</summary>
        public bool MajorityCapture { get; }

        /// <summary>
        /// Whether a man that lands on the far rank in the middle of a capture is crowned at once and goes on as a king.
        /// Where this is false the English man stops there, the International man goes on as a man and is crowned only
        /// when the sequence ends on that rank.
        /// </summary>
        public bool PromoteDuringCapture { get; }

        public Side FirstMover { get; }

        public int DrawPlies => DrawPliesCount;

        /// <summary>"English", "Russian" or "International": the key of the name in the localization table.</summary>
        public string NameKey { get; }

        /// <summary>One English sentence about the variant: a key of the localization table.</summary>
        public string DescriptionKey { get; }

        /// <summary>Men each side starts with: 12, 12 or 20.</summary>
        public int MenPerSide => RowsOfMen * Size / 2;

        public int Squares => Size * Size;

        /// <summary>Whether <paramref name="row"/> is the far rank of <paramref name="side"/>, where its men are crowned.</summary>
        public bool IsCrowningRow(Side side, int row)
        {
            return side == Side.Light ? row == Size - 1 : row == 0;
        }

        /// <summary>The row step a man of <paramref name="side"/> moves by: +1 for Light, -1 for Dark.</summary>
        public static int Forward(Side side)
        {
            return side == Side.Light ? 1 : -1;
        }
    }
}
