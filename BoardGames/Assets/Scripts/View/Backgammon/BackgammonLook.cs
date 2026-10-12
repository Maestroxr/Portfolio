using System;
using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.BoardGames
{
    /// <summary>
    /// The look of the backgammon board, its checkers, dice and cube inside a <see cref="BoardGamesTheme"/>: the meshes
    /// (the same in every look) and the materials of one look. The board view reads it in <c>ApplyTheme</c>; the builder
    /// (Editor/BackgammonArt.cs) fills it. The highlight and contact shadow materials and the highlight colours are the
    /// theme's own, shared with the draughts board.
    /// </summary>
    [Serializable]
    public sealed class BackgammonLook
    {
        [Header("Meshes")]
        [Tooltip("The box: base, rim, bar, the divider before the trays and the split between them.")]
        public Mesh frameMesh;
        [Tooltip("The felt of the field and the trays, a plane at the board's surface.")]
        public Mesh feltMesh;
        [Tooltip("One point: a flat triangle one unit wide and one long, its base on z = 0, its tip at z = 1.")]
        public Mesh pointMesh;
        public Mesh checkerMesh;
        public Mesh dieMesh;
        public Mesh cubeMesh;

        [Header("Materials")]
        public Material frame;
        public Material felt;
        [Tooltip("The two colours of the points, in turn from White's 1-point.")]
        public Material[] points = new Material[2];
        [Tooltip("The glow over a point (an unlit transparent material tinted per use).")]
        public Material pointGlow;
        [Tooltip("The checkers: White's (Light) then Black's (Dark).")]
        public Material[] checkers = new Material[2];
        [Tooltip("The dice each side throws: White's then Black's.")]
        public Material[] dice = new Material[2];
        [Tooltip("The doubling cube, its faces showing 2 to 64.")]
        public Material cube;

        /// <summary>Adds what is missing to <paramref name="problems"/>; false when the look is incomplete.</summary>
        public bool Validate(string theme, List<string> problems)
        {
            bool complete = true;
            Check(frameMesh, "frameMesh", theme, problems, ref complete);
            Check(feltMesh, "feltMesh", theme, problems, ref complete);
            Check(pointMesh, "pointMesh", theme, problems, ref complete);
            Check(checkerMesh, "checkerMesh", theme, problems, ref complete);
            Check(dieMesh, "dieMesh", theme, problems, ref complete);
            Check(cubeMesh, "cubeMesh", theme, problems, ref complete);
            Check(frame, "frame", theme, problems, ref complete);
            Check(felt, "felt", theme, problems, ref complete);
            Check(pointGlow, "pointGlow", theme, problems, ref complete);
            Check(cube, "cube", theme, problems, ref complete);
            CheckPair(points, "points", theme, problems, ref complete);
            CheckPair(checkers, "checkers", theme, problems, ref complete);
            CheckPair(dice, "dice", theme, problems, ref complete);
            return complete;
        }

        /// <summary>The checkers' material of <paramref name="side"/>, or null.</summary>
        public Material CheckerOf(Side side)
        {
            return checkers != null && checkers.Length > (int)side ? checkers[(int)side] : null;
        }

        /// <summary>The material of the dice <paramref name="side"/> throws, or null.</summary>
        public Material DieOf(Side side)
        {
            return dice != null && dice.Length > (int)side ? dice[(int)side] : null;
        }

        /// <summary>The material of point <paramref name="point"/>: the two colours take turns, so facing points differ too.</summary>
        public Material PointOf(int point)
        {
            return points != null && points.Length == 2 ? points[point & 1] : null;
        }

        private static void Check(UnityEngine.Object value, string field, string theme, List<string> problems, ref bool complete)
        {
            if (value == null)
            {
                problems.Add($"{theme}.backgammon.{field} is missing");
                complete = false;
            }
        }

        private static void CheckPair(Material[] values, string field, string theme, List<string> problems, ref bool complete)
        {
            if (values == null || values.Length != 2)
            {
                problems.Add($"{theme}.backgammon.{field} lists {(values != null ? values.Length : 0)} materials, it needs 2");
                complete = false;
                return;
            }
            for (int i = 0; i < 2; i++)
            {
                Check(values[i], $"{field}[{i}]", theme, problems, ref complete);
            }
        }
    }
}
