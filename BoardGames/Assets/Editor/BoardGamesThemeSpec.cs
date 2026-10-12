using UnityEngine;

namespace Portfolio.BoardGames.EditorTools
{
    /// <summary>
    /// Plain editor data of one look of the game: the colours and material parameters the art builder draws with and
    /// the content builder writes into the theme asset. The same builders run for every spec; Classic is the warm wooden
    /// board, Midnight the slate board with glowing pieces.
    /// </summary>
    internal sealed class BoardGamesThemeSpec
    {
        public string Name;
        public string AssetName;
        public string Description;
        public string Folder;

        // The board and the table.
        public Color LightSquare;
        public Color DarkSquare;
        public Color SquareGrainA;
        public Color SquareGrainB;
        public float SquareSmoothness;
        public bool Slate;
        public Color Slab;
        public float SlabMetallic;
        public float SlabSmoothness;
        public Color Table;
        public float TableSmoothness;

        // The pieces: [0] Light, [1] Dark.
        public Color LightPiece;
        public Color DarkPiece;
        public Color LightCrown;
        public Color DarkCrown;
        public float PieceMetallic;
        public float PieceSmoothness;
        public bool Emissive;
        public Color LightGlow;
        public Color DarkGlow;

        // Seat colours shown in the interface.
        public Color LightColor;
        public Color DarkColor;

        // Highlights.
        public Color Selected;
        public Color Legal;
        public Color Capture;
        public Color LastMove;
        public Color Hover;

        // The scene.
        public Color Background;
        public Color AmbientSky;
        public Color AmbientEquator;
        public Color AmbientGround;
        public Color SunColor;
        public float SunIntensity;

        // The menu and the lobby.
        public Color Ink;
        /// <summary>The words on the menu's buttons and header banner, which stay white in every look.</summary>
        public Color ButtonInk;
        public Color Paper;
        public Color Panel;
        public Color Muted;
        public Color Accent;
        public Color Error;
        public Color LobbyWindow;
        public Color LobbyRow;

        public string TexturesFolder => $"{Folder}/Textures";
        public string MaterialsFolder => $"{Folder}/Materials";
        public string AssetPath => $"Config/Themes/{AssetName}.asset";

        public static Color Hex(uint rgb, float alpha = 1f)
        {
            return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, alpha);
        }

        public static readonly BoardGamesThemeSpec Classic = new BoardGamesThemeSpec
        {
            Name = "Classic",
            AssetName = "Classic",
            Description = "A warm wooden board on green felt, ivory against ebony.",
            Folder = "Art",
            LightSquare = Hex(0xEBD9B4),
            DarkSquare = Hex(0x6B4426),
            SquareGrainA = Hex(0xFFFFFF, 0.08f),
            SquareGrainB = Hex(0x000000, 0.10f),
            SquareSmoothness = 0.42f,
            Slate = false,
            Slab = Hex(0x3E2616),
            SlabMetallic = 0.05f,
            SlabSmoothness = 0.5f,
            Table = Hex(0x1F5A36),
            TableSmoothness = 0.12f,
            LightPiece = Hex(0xF4EBD6),
            DarkPiece = Hex(0x2A2320),
            LightCrown = Hex(0xD9B45A),
            DarkCrown = Hex(0xD9B45A),
            PieceMetallic = 0.12f,
            PieceSmoothness = 0.62f,
            Emissive = false,
            LightGlow = Color.black,
            DarkGlow = Color.black,
            LightColor = Hex(0xF4EBD6),
            DarkColor = Hex(0x3B3230),
            Selected = Hex(0xFFD24A, 0.85f),
            Legal = Hex(0x7FD86A, 0.75f),
            Capture = Hex(0xFF6A4A, 0.9f),
            LastMove = Hex(0x5AA0FF, 0.35f),
            Hover = Hex(0xFFFFFF, 0.9f),
            Background = Hex(0x1A2A1E),
            AmbientSky = Hex(0x8FA7B8),
            AmbientEquator = Hex(0x6F6A5C),
            AmbientGround = Hex(0x2B2A26),
            SunColor = Hex(0xFFF4E0),
            SunIntensity = 1.1f,
            Ink = Hex(0x2A2320),
            ButtonInk = Hex(0x2A2320),
            Paper = Hex(0xF4EBD6),
            Panel = Hex(0xE2D3B0),
            Muted = Hex(0x7A6A58),
            Accent = Hex(0x1F5A36),
            Error = Hex(0xB3402A),
            LobbyWindow = Hex(0x2A2320),
            LobbyRow = Hex(0x3E3430)
        };

        public static readonly BoardGamesThemeSpec Midnight = new BoardGamesThemeSpec
        {
            Name = "Midnight",
            AssetName = "Midnight",
            Description = "A slate glass board under a deep blue night, neon cyan against magenta.",
            Folder = "Art/Themes/Midnight",
            LightSquare = Hex(0x3C4659),
            DarkSquare = Hex(0x161B26),
            SquareGrainA = Hex(0xFFFFFF, 0.05f),
            SquareGrainB = Hex(0x000000, 0.12f),
            SquareSmoothness = 0.78f,
            Slate = true,
            Slab = Hex(0x0E1420),
            SlabMetallic = 0.3f,
            SlabSmoothness = 0.8f,
            Table = Hex(0x080B14),
            TableSmoothness = 0.3f,
            LightPiece = Hex(0x1F6F7A),
            DarkPiece = Hex(0x6E1F62),
            LightCrown = Hex(0xBFFFFF),
            DarkCrown = Hex(0xFFBFF4),
            PieceMetallic = 0.25f,
            PieceSmoothness = 0.85f,
            Emissive = true,
            LightGlow = new Color(0.1f, 1.4f, 1.6f),
            DarkGlow = new Color(1.6f, 0.2f, 1.3f),
            LightColor = Hex(0x3FE6FF),
            DarkColor = Hex(0xFF4FD8),
            Selected = Hex(0xFFFFFF, 0.8f),
            Legal = Hex(0x3FE6FF, 0.7f),
            Capture = Hex(0xFF4FD8, 0.9f),
            LastMove = Hex(0xA0B4FF, 0.3f),
            Hover = Hex(0xFFFFFF, 0.95f),
            Background = Hex(0x060A18),
            AmbientSky = Hex(0x2A3C66),
            AmbientEquator = Hex(0x1C2238),
            AmbientGround = Hex(0x090B14),
            SunColor = Hex(0xBFD4FF),
            SunIntensity = 0.8f,
            Ink = Hex(0xE6EEFF),
            ButtonInk = Hex(0x141B2C),
            Paper = Hex(0x141B2C),
            Panel = Hex(0x1C253A),
            Muted = Hex(0x8A98B8),
            Accent = Hex(0x3FE6FF),
            Error = Hex(0xFF4F6A),
            LobbyWindow = Hex(0x141B2C),
            LobbyRow = Hex(0x1C253A)
        };

        public static readonly BoardGamesThemeSpec[] All = { Classic, Midnight };
    }
}
