using UnityEngine;

namespace Portfolio.MemoryCards.EditorTools
{
    /// <summary>What the generators need to know about one world: names, colours, card back pattern and animals.</summary>
    internal sealed class WorldSpec
    {
        public string Id;
        public string Name;
        public string Tagline;
        public string SkyTop;
        public string SkyBottom;
        public string Accent;
        public string AccentDark;
        public string Card;
        public string CardDark;
        public CardPattern Pattern;
        public string Ambient;
        public Color AmbientColor;
        public AmbientMotion Motion;
        public string Mascot;
        /// <summary>Animal file names; empty means every animal of the game.</summary>
        public string[] Animals;
        public int StarsRequired;
    }


    /// <summary>The four worlds of the campaign and the thirty animals of the game.</summary>
    internal static class WorldSpecs
    {
        /// <summary>Every animal (the renders in Art/Animals), in the order save games refer to them.</summary>
        public static readonly string[] Animals =
        {
            "bear", "buffalo", "chick", "chicken", "cow", "crocodile", "dog", "duck", "elephant", "frog",
            "giraffe", "goat", "gorilla", "hippo", "horse", "monkey", "moose", "narwhal", "owl", "panda",
            "parrot", "penguin", "pig", "rabbit", "rhino", "sloth", "snake", "walrus", "whale", "zebra"
        };

        public static readonly WorldSpec[] All =
        {
            new WorldSpec
            {
                Id = "Farm", Name = "Sunny Farm", Tagline = "Meet the barnyard gang!",
                SkyTop = "#62C4FF", SkyBottom = "#B8EC86", Accent = "#FF9A1F", AccentDark = "#C96A00",
                Card = "#4DB85A", CardDark = "#2E8A3C", Pattern = CardPattern.Dots,
                Ambient = "Cloud", AmbientColor = new Color(1f, 1f, 1f, 0.85f), Motion = AmbientMotion.Drift,
                Mascot = "chick", StarsRequired = 0,
                Animals = new[] { "chick", "chicken", "cow", "dog", "duck", "goat", "horse", "pig", "rabbit", "owl" }
            },
            new WorldSpec
            {
                Id = "Jungle", Name = "Jungle Jam", Tagline = "Swing into the wild side!",
                SkyTop = "#26B89A", SkyBottom = "#0E6B4B", Accent = "#FFC233", AccentDark = "#B98300",
                Card = "#EE6B35", CardDark = "#B2431A", Pattern = CardPattern.Stripes,
                Ambient = "Leaf", AmbientColor = new Color(0.72f, 0.95f, 0.62f, 0.5f), Motion = AmbientMotion.Fall,
                Mascot = "monkey", StarsRequired = 10,
                Animals = new[] { "monkey", "gorilla", "parrot", "snake", "crocodile", "sloth", "frog", "hippo", "elephant", "giraffe", "zebra", "rhino" }
            },
            new WorldSpec
            {
                Id = "Frosty", Name = "Frosty Shores", Tagline = "Chill out with icy friends!",
                SkyTop = "#3E6FD6", SkyBottom = "#A6DBFA", Accent = "#8E7CFF", AccentDark = "#5B47D6",
                Card = "#2E9BDB", CardDark = "#1A6CA3", Pattern = CardPattern.Flakes,
                Ambient = "Snowflake", AmbientColor = new Color(1f, 1f, 1f, 0.7f), Motion = AmbientMotion.Fall,
                Mascot = "penguin", StarsRequired = 26,
                Animals = new[] { "penguin", "walrus", "narwhal", "whale", "moose", "bear", "buffalo", "panda", "dog", "owl", "rabbit" }
            },
            new WorldSpec
            {
                Id = "Carnival", Name = "Critter Carnival", Tagline = "Every critter, every twist!",
                SkyTop = "#8A55E0", SkyBottom = "#F062B0", Accent = "#1FC2FF", AccentDark = "#0B84C2",
                Card = "#EF4F9E", CardDark = "#B12A72", Pattern = CardPattern.Zigzag,
                Ambient = "Balloon", AmbientColor = new Color(1f, 0.9f, 0.98f, 0.45f), Motion = AmbientMotion.Rise,
                Mascot = "panda", StarsRequired = 0,
                Animals = new string[0]
            }
        };

        public static Color Color(string hex)
        {
            return Shapes.Hex(hex);
        }
    }
}
