using System;
using System.Collections.Generic;
using Gamebox;
using UnityEngine;

namespace Portfolio.BoardGames
{
    /// <summary>The look of one side's pieces: the material of the discs, the crown of a king and the colour the interface uses for the side.</summary>
    [Serializable]
    public sealed class SideLook
    {
        [Tooltip("The English name of the side (a key of the localization table): Light or Dark.")]
        public string nameKey = "Light";
        public Material piece;
        public Material crown;
        [Tooltip("The side's colour in the interface (seat panels, the local play setup).")]
        public Color color = Color.white;
    }

    /// <summary>
    /// The whole look of the board games in one asset: the two sides' pieces, the squares, the slab and the table, the
    /// meshes, the highlight colours, the lights, and the looks of chess and backgammon. "Classic" is warm wood, "Midnight" slate and neon; the content builder
    /// makes both. The board applies it through <c>BoardView.ApplyTheme</c>.
    /// </summary>
    [CreateAssetMenu(fileName = "BoardGamesTheme", menuName = "Board Games/Theme", order = 4)]
    public sealed class BoardGamesTheme : GameTheme
    {
        [Header("Sides")]
        [Tooltip("The two sides, Light then Dark.")]
        public SideLook[] sides = new SideLook[2];

        [Header("Materials")]
        public Material lightSquare;
        public Material darkSquare;
        public Material slab;
        public Material table;
        [Tooltip("An unlit transparent material the highlight quads are tinted with per use.")]
        public Material highlight;
        public Material contactShadow;

        [Header("Meshes")]
        public Mesh pieceMesh;
        public Mesh crownMesh;
        public Mesh squareMesh;
        public Mesh slabMesh;
        public Mesh tableMesh;

        [Header("Highlights")]
        public Color selectedColor = new Color(1f, 0.85f, 0.3f, 0.9f);
        public Color legalColor = new Color(0.4f, 0.9f, 0.5f, 0.8f);
        public Color captureColor = new Color(1f, 0.45f, 0.3f, 0.9f);
        public Color lastMoveColor = new Color(0.5f, 0.7f, 1f, 0.35f);
        public Color hoverColor = new Color(1f, 1f, 1f, 0.6f);

        [Header("Scene")]
        public Color background = new Color(0.12f, 0.1f, 0.08f);
        public Color ambientSky = new Color(0.6f, 0.62f, 0.7f);
        public Color ambientEquator = new Color(0.45f, 0.42f, 0.4f);
        public Color ambientGround = new Color(0.2f, 0.18f, 0.16f);
        public Color sunColor = new Color(1f, 0.96f, 0.9f);
        public float sunIntensity = 1.1f;

        [Header("Chess")]
        [Tooltip("The chess pieces: their meshes and materials (the chess view reads them, its builder fills them).")]
        public ChessLook chess = new ChessLook();

        [Header("Backgammon")]
        [Tooltip("The backgammon board, checkers, dice and cube (the backgammon view reads them, its builder fills them).")]
        public BackgammonLook backgammon = new BackgammonLook();

        [Header("Launcher")]
        [Tooltip("The icon of the game in the launcher (the classic theme has it).")]
        [ThemeOptional] public Sprite gameIcon;

        public override GameType Game => GameType.BoardGames;

        /// <summary>The look of a side, or null when the theme does not list it.</summary>
        public SideLook SideOf(Side side)
        {
            int index = (int)side;
            return sides != null && index >= 0 && index < sides.Length ? sides[index] : null;
        }

        /// <summary>The colour of a side in the interface: the theme's, else white or a dark grey.</summary>
        public Color ColorOf(Side side)
        {
            SideLook look = SideOf(side);
            return look != null ? look.color : (side == Side.Light ? Color.white : new Color(0.2f, 0.2f, 0.2f));
        }

        public override bool Validate(List<string> problems)
        {
            bool complete = base.Validate(problems);
            if (sides == null || sides.Length != 2)
            {
                problems.Add($"{name}.sides lists {(sides != null ? sides.Length : 0)} sides, the game has 2");
                return false;
            }
            for (int i = 0; i < sides.Length; i++)
            {
                if (sides[i] == null)
                {
                    problems.Add($"{name}.sides[{i}] is empty");
                    complete = false;
                    continue;
                }
                if (sides[i].piece == null)
                {
                    problems.Add($"{name}.sides[{i}].piece has no material");
                    complete = false;
                }
                if (sides[i].crown == null)
                {
                    problems.Add($"{name}.sides[{i}].crown has no material");
                    complete = false;
                }
                if (string.IsNullOrEmpty(sides[i].nameKey))
                {
                    problems.Add($"{name}.sides[{i}] has no name");
                    complete = false;
                }
            }
            if (chess == null || !chess.Validate(name, problems))
            {
                complete = false;
            }
            if (backgammon == null || !backgammon.Validate(name, problems))
            {
                complete = false;
            }
            return complete;
        }
    }
}
