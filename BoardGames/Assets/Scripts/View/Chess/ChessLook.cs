using System;
using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.BoardGames
{
    /// <summary>
    /// The look of the chess pieces inside a <see cref="BoardGamesTheme"/>: one mesh per kind of piece (Staunton shapes,
    /// shared by every look) and per side the material of the body and of the trim (the foot ring, the collar, the
    /// king's cross: lacquered wood like the body in Classic, neon in Midnight). The meshes have two submeshes, body
    /// then trim. Filled by the chess art builder (Editor/ChessArt.cs).
    /// </summary>
    [Serializable]
    public sealed class ChessLook
    {
        [Tooltip("The meshes of the six kinds of piece, by kind: pawn, knight, bishop, rook, queen, king (index kind - 1).")]
        public Mesh[] meshes = new Mesh[6];
        [Tooltip("The body of the pieces, Light (White) then Dark (Black).")]
        public Material[] sideMaterials = new Material[2];
        [Tooltip("The trim of the pieces (the second submesh), Light then Dark; empty uses the body's material.")]
        public Material[] trimMaterials = new Material[2];
        [Tooltip("How far the knights turn from a clean profile towards the player of their side, in degrees.")]
        public float knightTurn = 18f;

        /// <summary>The mesh of <paramref name="kind"/>, or null.</summary>
        public Mesh MeshOf(ChessPieceKind kind)
        {
            int index = (int)kind - 1;
            return meshes != null && index >= 0 && index < meshes.Length ? meshes[index] : null;
        }

        /// <summary>The body material of <paramref name="side"/>, or null.</summary>
        public Material BodyOf(Side side)
        {
            int index = (int)side;
            return sideMaterials != null && index < sideMaterials.Length ? sideMaterials[index] : null;
        }

        /// <summary>The trim material of <paramref name="side"/>: its own, else the body's.</summary>
        public Material TrimOf(Side side)
        {
            int index = (int)side;
            Material trim = trimMaterials != null && index < trimMaterials.Length ? trimMaterials[index] : null;
            return trim != null ? trim : BodyOf(side);
        }

        /// <summary>Adds what is missing to <paramref name="problems"/>; false when the look is incomplete.</summary>
        public bool Validate(string theme, List<string> problems)
        {
            bool complete = true;
            if (meshes == null || meshes.Length != 6)
            {
                problems.Add($"{theme}.chess.meshes lists {(meshes != null ? meshes.Length : 0)} meshes, chess has 6 kinds of piece");
                complete = false;
            }
            else
            {
                for (int i = 0; i < meshes.Length; i++)
                {
                    if (meshes[i] == null)
                    {
                        problems.Add($"{theme}.chess.meshes[{i}] ({ChessPieces.NameKey((ChessPieceKind)(i + 1))}) is empty");
                        complete = false;
                    }
                }
            }
            if (sideMaterials == null || sideMaterials.Length != 2)
            {
                problems.Add($"{theme}.chess.sideMaterials lists {(sideMaterials != null ? sideMaterials.Length : 0)} materials, chess has 2 sides");
                complete = false;
            }
            else
            {
                for (int i = 0; i < sideMaterials.Length; i++)
                {
                    if (sideMaterials[i] == null)
                    {
                        problems.Add($"{theme}.chess.sideMaterials[{i}] is empty");
                        complete = false;
                    }
                }
            }
            return complete;
        }
    }
}
