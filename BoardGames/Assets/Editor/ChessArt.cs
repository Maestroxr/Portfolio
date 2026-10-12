using System;
using System.Collections.Generic;
using Gamebox.Editor;
using UnityEngine;

namespace Portfolio.BoardGames.EditorTools
{
    /// <summary>
    /// The chess pieces' art: Staunton style meshes for the six kinds (once), the materials of each look, and the
    /// theme's <see cref="ChessLook"/>. Sized for a square of one unit: the king about 1.05 tall, the pawn 0.55, the feet
    /// 0.27 to 0.35 across the radius. The round parts are lathed profiles; the knight's head is an extruded outline on a
    /// lathed foot, the rook's crenellations are ring sectors, the bishop's mitre is carved with a slanted slit, the
    /// queen wears a coronet of balls and the king an extruded cross. Each mesh has two submeshes: the body, and the trim
    /// (the foot ring, the collar's edge, the finials, the knight's eyes, the cross): the same lacquered wood as the body
    /// in Classic, neon over frosted glass in Midnight, the palette of the draughts pieces.
    /// </summary>
    internal static class ChessArt
    {
        public const string MeshesFolder = "Art/Meshes/Chess";

        private const int Segments = 48;
        private const int GrainPixels = 256;

        /// <summary>The warm wood the ivory pieces lean towards.</summary>
        private static readonly Color Boxwood = new Color(0.89f, 0.8f, 0.63f);

        private static readonly ChessPieceKind[] Kinds =
        {
            ChessPieceKind.Pawn, ChessPieceKind.Knight, ChessPieceKind.Bishop, ChessPieceKind.Rook, ChessPieceKind.Queen, ChessPieceKind.King
        };

        private static Shader Lit => Shader.Find("Universal Render Pipeline/Lit");

        /// <summary>What every look shares (the meshes of the six kinds of piece). Called once per Build Everything.</summary>
        public static void BuildShared()
        {
            foreach (ChessPieceKind kind in Kinds)
            {
                BoardGamesAssets.SaveMesh(Build(kind), MeshPath(kind));
            }
        }

        /// <summary>The textures and materials of one look.</summary>
        public static void BuildLook(BoardGamesThemeSpec spec)
        {
            if (spec.Emissive)
            {
                // Frosted glass lit from inside, in the colours of the draughts pieces, with neon trim.
                Texture2D frost = BoardGamesAssets.SaveTexture(Grain(spec, 1f, 0f, 0.16f, 41), $"{spec.TexturesFolder}/ChessFrost.png", GameAssets.SurfaceTexture(true, GrainPixels));
                PieceMaterial(spec, "ChessLight", Color.Lerp(spec.LightPiece, spec.LightColor, 0.3f), 0.1f, 0.9f, frost, spec.LightGlow * 0.11f);
                PieceMaterial(spec, "ChessDark", Color.Lerp(spec.DarkPiece, spec.DarkColor, 0.3f), 0.1f, 0.9f, frost, spec.DarkGlow * 0.11f);
                PieceMaterial(spec, "ChessLightTrim", spec.LightCrown, 0.2f, 0.85f, null, spec.LightGlow);
                PieceMaterial(spec, "ChessDarkTrim", spec.DarkCrown, 0.2f, 0.85f, null, spec.DarkGlow);
            }
            else
            {
                // Lacquered ivory and ebony: the draughts pieces' colours with a fine wood grain and a glossy coat. The ivory
                // is warmer and a shade deeper than the draughts men, towards boxwood, so the carving still reads on the
                // light squares under the bright sun; the ebony's texture is grey with light streaks, its colour lifted by
                // as much.
                const float ebonyBase = 0.78f;
                Texture2D ivory = BoardGamesAssets.SaveTexture(Grain(spec, 1f, 0f, 0.07f, 17), $"{spec.TexturesFolder}/ChessGrainLight.png", GameAssets.SurfaceTexture(true, GrainPixels));
                Texture2D ebony = BoardGamesAssets.SaveTexture(Grain(spec, ebonyBase, 0.32f, 0.12f, 23), $"{spec.TexturesFolder}/ChessGrainDark.png", GameAssets.SurfaceTexture(true, GrainPixels));
                PieceMaterial(spec, "ChessLight", Scaled(Color.Lerp(spec.LightPiece, Boxwood, 0.45f), 0.9f), 0.04f, 0.6f, ivory, default);
                PieceMaterial(spec, "ChessDark", Scaled(spec.DarkPiece, 1f / ebonyBase), 0.04f, 0.72f, ebony, default);
            }
        }

        /// <summary>Fills <paramref name="theme"/>.chess from the assets of <paramref name="spec"/>'s look.</summary>
        public static void Dress(BoardGamesTheme theme, BoardGamesThemeSpec spec)
        {
            var meshes = new Mesh[Kinds.Length];
            foreach (ChessPieceKind kind in Kinds)
            {
                meshes[(int)kind - 1] = BoardGamesAssets.Require<Mesh>(MeshPath(kind));
            }
            Material light = BoardGamesArtBuilder.Material("ChessLight", spec);
            Material dark = BoardGamesArtBuilder.Material("ChessDark", spec);
            theme.chess = new ChessLook
            {
                meshes = meshes,
                sideMaterials = new[] { light, dark },
                trimMaterials = spec.Emissive
                    ? new[] { BoardGamesArtBuilder.Material("ChessLightTrim", spec), BoardGamesArtBuilder.Material("ChessDarkTrim", spec) }
                    : new[] { light, dark },
                knightTurn = 18f
            };
        }

        /// <summary>The mesh asset of <paramref name="kind"/> (for the scene parts and the preview).</summary>
        public static Mesh MeshOf(ChessPieceKind kind)
        {
            return BoardGamesAssets.Require<Mesh>(MeshPath(kind));
        }

        private static string MeshPath(ChessPieceKind kind)
        {
            return $"{MeshesFolder}/{kind}.asset";
        }

        // ------------------------------------------------------------------ materials

        /// <summary>A lit piece material; with an emission colour above black it glows.</summary>
        private static Material PieceMaterial(BoardGamesThemeSpec spec, string name, Color color, float metallic, float smoothness, Texture2D texture, Color emission)
        {
            bool glows = emission.maxColorComponent > 0f;
            return BoardGamesAssets.SaveMaterial($"{spec.MaterialsFolder}/{name}.mat", Lit, m =>
            {
                m.SetColor("_BaseColor", color);
                m.SetTexture("_BaseMap", texture);
                m.SetTextureScale("_BaseMap", Vector2.one);
                m.SetFloat("_Metallic", metallic);
                m.SetFloat("_Smoothness", smoothness);
                m.SetFloat("_Surface", 0f);
                m.SetTexture("_EmissionMap", null);
                m.SetColor("_EmissionColor", glows ? emission : Color.black);
                if (glows)
                {
                    m.EnableKeyword("_EMISSION");
                    m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }
                else
                {
                    m.DisableKeyword("_EMISSION");
                    m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                }
                m.enableInstancing = true;
            });
        }

        private static Color Scaled(Color color, float factor)
        {
            return new Color(color.r * factor, color.g * factor, color.b * factor, color.a);
        }

        /// <summary>
        /// The surface of the pieces, grey <paramref name="baseGrey"/> to be tinted by the material, lightened and darkened
        /// by up to <paramref name="light"/> and <paramref name="dark"/>: wood streaks running up the profile (Classic) or
        /// a cloudy frost (Midnight). Seamless around the piece: the lathes wrap u once around.
        /// </summary>
        private static Texture2D Grain(BoardGamesThemeSpec spec, float baseGrey, float light, float dark, int seed)
        {
            int size = GrainPixels;
            var raster = new Raster(size, size, new Color(baseGrey, baseGrey, baseGrey, 1f));
            float offset = seed * 0.37f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size;
                    float v = y / (float)size;
                    float angle = u * 2f * Mathf.PI;
                    float cos = Mathf.Cos(angle);
                    float sin = Mathf.Sin(angle);
                    float n;
                    if (spec.Emissive)
                    {
                        float cloud = Mathf.PerlinNoise(11.3f + cos * 2.5f + v * 4f, 4.1f + sin * 2.5f + v * 3f);
                        float frost = Mathf.PerlinNoise(2.7f + cos * 14f + v * 22f, 8.9f + sin * 14f - v * 16f);
                        n = cloud * 0.55f + frost * 0.45f;
                    }
                    else
                    {
                        float wobble = Mathf.PerlinNoise(3.1f + offset + v * 2.5f, 1.7f + cos * 0.5f) * 0.8f;
                        float streaks = Mathf.Sin((u * 11f + wobble) * 2f * Mathf.PI) * 0.5f + 0.5f;
                        float fine = Mathf.PerlinNoise(5.3f + offset + cos * 10f, 7.7f + sin * 10f + v * 14f);
                        n = streaks * 0.5f + fine * 0.5f;
                    }
                    float t = n * 2f - 1f;
                    Color tint = t > 0f ? new Color(1f, 1f, 1f, light) : new Color(0f, 0f, 0f, dark);
                    tint.a *= Mathf.Abs(t);
                    raster.Blend(x, y, tint);
                }
            }
            raster.Grain(new Rect(0f, 0f, size, size), 0.015f, seed);
            return raster.ToTexture(false);
        }

        // ------------------------------------------------------------------ meshes

        /// <summary>The mesh of <paramref name="kind"/>: submesh 0 the body, submesh 1 the trim.</summary>
        public static Mesh Build(ChessPieceKind kind)
        {
            var body = new MeshBuilder();
            var trim = new MeshBuilder();
            switch (kind)
            {
                case ChessPieceKind.Pawn:
                    Pawn(body, trim);
                    break;
                case ChessPieceKind.Knight:
                    Knight(body, trim);
                    break;
                case ChessPieceKind.Bishop:
                    Bishop(body, trim);
                    break;
                case ChessPieceKind.Rook:
                    Rook(body, trim);
                    break;
                case ChessPieceKind.Queen:
                    Queen(body, trim);
                    break;
                case ChessPieceKind.King:
                    King(body, trim);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "no such piece");
            }
            return TwoParts(kind.ToString(), body, trim);
        }

        /// <summary>One mesh of two submeshes: the body, then the trim.</summary>
        private static Mesh TwoParts(string name, MeshBuilder body, MeshBuilder trim)
        {
            var all = new MeshBuilder();
            all.Append(body);
            int split = all.Triangles.Count;
            all.Append(trim);
            Mesh mesh = all.ToMesh(name);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(all.Triangles.GetRange(0, split), 0, false);
            mesh.SetTriangles(all.Triangles.GetRange(split, all.Triangles.Count - split), 1, false);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// The Staunton foot of radius <paramref name="r"/>: a rounded rim (trim), a shelf, a big torus, a cove and a bead;
        /// it ends at about (0.6 r, 0.152), where the stem starts.
        /// </summary>
        private static void Foot(Profile p, float r)
        {
            p.Trim();
            p.P(0f, 0f).P(r - 0.012f, 0f).Arc(r - 0.012f, 0.012f, 0.012f, -90f, 0f, 3).P(r, 0.034f);
            p.Body();
            p.P(r - 0.004f, 0.038f).P(r * 0.9f, 0.042f);
            p.Arc(r * 0.86f, 0.067f, 0.027f, -75f, 100f, 9);
            p.P(r * 0.78f, 0.104f).P(r * 0.72f, 0.112f).P(r * 0.67f, 0.121f);
            p.Arc(r * 0.655f, 0.134f, 0.013f, -90f, 90f, 6);
            p.P(r * 0.6f, 0.152f);
        }

        /// <summary>A collar at <paramref name="y"/>: a disc of radius <paramref name="r"/> whose rounded edge is trim, then a smaller bead over it.</summary>
        private static void Collar(Profile p, float r, float y, float bead)
        {
            p.P(r, y);
            p.Trim();
            p.Arc(r, y + 0.012f, 0.012f, -90f, 90f, 6);
            p.Body();
            if (bead > 0f)
            {
                p.P(bead, y + 0.029f);
                p.Arc(bead, y + 0.039f, 0.01f, -90f, 90f, 5);
            }
        }

        private static void Pawn(MeshBuilder body, MeshBuilder trim)
        {
            var p = new Profile();
            Foot(p, 0.27f);
            p.P(0.15f, 0.17f).P(0.132f, 0.205f).P(0.115f, 0.245f).P(0.1f, 0.285f).P(0.09f, 0.312f);
            Collar(p, 0.142f, 0.318f, 0f);
            p.P(0.085f, 0.348f).P(0.07f, 0.36f);
            p.Arc(0f, 0.448f, 0.103f, -47.2f, 90f, 14);
            p.LatheInto(body, trim, Segments);
        }

        private static void Rook(MeshBuilder body, MeshBuilder trim)
        {
            var p = new Profile();
            Foot(p, 0.31f);
            p.P(0.195f, 0.165f).P(0.188f, 0.22f).P(0.18f, 0.3f).P(0.176f, 0.38f).P(0.178f, 0.43f).P(0.188f, 0.465f).P(0.205f, 0.49f).P(0.222f, 0.5f);
            p.Trim();
            p.Arc(0.226f, 0.509f, 0.009f, -90f, 90f, 5);
            p.Body();
            p.P(0.234f, 0.522f).P(0.236f, 0.59f).P(0.229f, 0.598f).P(0.168f, 0.598f).P(0.16f, 0.58f).P(0f, 0.58f);
            p.LatheInto(body, trim, Segments);
            // Six merlons around the top, a gap facing the player of the piece's side (local -z).
            const int merlons = 6;
            for (int i = 0; i < merlons; i++)
            {
                float center = i * 360f / merlons;
                RingSector(body, 0.168f, 0.236f, 0.596f, 0.676f, center - 19f, center + 19f, 6);
            }
        }

        private static void Knight(MeshBuilder body, MeshBuilder trim)
        {
            var p = new Profile();
            Foot(p, 0.31f);
            p.P(0.196f, 0.166f).Arc(0.188f, 0.19f, 0.015f, -30f, 90f, 5).P(0f, 0.207f);
            p.LatheInto(body, trim, Segments);
            // The head looks along +x: chest and throat in front, the mane down the back, one ear up.
            var head = new List<Vector2>
            {
                new Vector2(-0.165f, 0.18f), new Vector2(0.15f, 0.18f), new Vector2(0.168f, 0.235f), new Vector2(0.16f, 0.29f),
                new Vector2(0.135f, 0.34f), new Vector2(0.112f, 0.38f), new Vector2(0.11f, 0.41f), new Vector2(0.14f, 0.44f),
                new Vector2(0.195f, 0.465f), new Vector2(0.245f, 0.482f), new Vector2(0.272f, 0.51f), new Vector2(0.276f, 0.545f),
                new Vector2(0.262f, 0.575f), new Vector2(0.232f, 0.596f), new Vector2(0.17f, 0.632f), new Vector2(0.12f, 0.668f),
                new Vector2(0.09f, 0.71f), new Vector2(0.072f, 0.755f), new Vector2(0.05f, 0.796f), new Vector2(0.026f, 0.802f),
                new Vector2(0.006f, 0.77f), new Vector2(-0.035f, 0.748f), new Vector2(-0.1f, 0.705f), new Vector2(-0.152f, 0.635f),
                new Vector2(-0.19f, 0.54f), new Vector2(-0.207f, 0.44f), new Vector2(-0.2f, 0.33f), new Vector2(-0.185f, 0.245f)
            };
            MeshBuilder carved = MeshFactory.Extrude(head, 0.19f, 0.022f, 3);
            // The extrusion maps each face onto the whole texture: the grain is laid on again at the scale of the lathes.
            for (int i = 0; i < carved.Uvs.Count; i++)
            {
                Vector3 v = carved.Vertices[i];
                carved.Uvs[i] = new Vector2((v.x + v.z) * 3.5f, v.y * 1.4f);
            }
            body.Append(carved);
            // The eyes, a bead on each side of the head.
            foreach (float side in new[] { -1f, 1f })
            {
                trim.Append(MeshFactory.Ellipsoid(new Vector3(0.019f, 0.016f, 0.01f), 12, 8), Matrix4x4.Translate(new Vector3(0.108f, 0.648f, side * 0.094f)));
            }
        }

        private static void Bishop(MeshBuilder body, MeshBuilder trim)
        {
            var p = new Profile();
            Foot(p, 0.3f);
            p.P(0.17f, 0.168f).P(0.15f, 0.21f).P(0.13f, 0.27f).P(0.112f, 0.34f).P(0.098f, 0.41f).P(0.09f, 0.46f);
            Collar(p, 0.15f, 0.466f, 0.105f);
            p.P(0.068f, 0.516f).P(0.062f, 0.528f);
            p.LatheInto(body, trim, Segments);
            // The mitre, an egg pointed at the top, carved with a slit slanting down to the right.
            var mitre = new List<Vector2> { new Vector2(0.062f, 0.528f) };
            const int rows = 72;
            for (int i = 0; i <= rows; i++)
            {
                float angle = Mathf.Lerp(-62f, 76f, i / (float)rows) * Mathf.Deg2Rad;
                float height = angle < 0f ? 0.11f : 0.19f;
                mitre.Add(new Vector2(0.116f * Mathf.Cos(angle), 0.636f + height * Mathf.Sin(angle)));
            }
            Vector3 slitNormal = new Vector3(0.62f, 0.78f, 0f).normalized;
            var slitCenter = new Vector3(0f, 0.69f, 0f);
            body.Append(LatheGrid(mitre, 96, point =>
            {
                float t = Mathf.Abs(Vector3.Dot(point - slitCenter, slitNormal)) / 0.014f;
                if (t >= 1f)
                {
                    return point;
                }
                float scale = 1f - 0.42f * (1f - t * t);
                return new Vector3(point.x * scale, point.y, point.z * scale);
            }));
            var finial = new Profile();
            finial.Trim();
            Vector2 tip = mitre[mitre.Count - 1];
            finial.P(tip.x, tip.y).P(0.02f, 0.826f).Arc(0f, 0.851f, 0.027f, -42f, 90f, 8);
            finial.LatheInto(body, trim, Segments);
        }

        private static void Queen(MeshBuilder body, MeshBuilder trim)
        {
            var p = new Profile();
            Foot(p, 0.33f);
            p.P(0.186f, 0.168f).P(0.162f, 0.215f).P(0.14f, 0.28f).P(0.12f, 0.36f).P(0.105f, 0.45f).P(0.097f, 0.52f);
            Collar(p, 0.168f, 0.527f, 0.12f);
            p.P(0.082f, 0.583f).P(0.078f, 0.595f).P(0.083f, 0.63f).P(0.095f, 0.67f).P(0.115f, 0.71f).P(0.14f, 0.745f).P(0.16f, 0.77f).P(0.172f, 0.785f);
            p.P(0.176f, 0.792f).P(0.172f, 0.8f).P(0.15f, 0.802f).P(0.135f, 0.81f).P(0.11f, 0.83f).P(0.075f, 0.85f).P(0.04f, 0.862f).P(0.02f, 0.866f);
            p.Trim();
            p.Arc(0f, 0.894f, 0.03f, -48f, 90f, 8);
            p.LatheInto(body, trim, Segments);
            // The coronet: a ring of balls on the rim.
            const int balls = 9;
            for (int i = 0; i < balls; i++)
            {
                float angle = (90f + i * 360f / balls) * Mathf.Deg2Rad;
                var at = new Vector3(Mathf.Cos(angle) * 0.162f, 0.812f, Mathf.Sin(angle) * 0.162f);
                trim.Append(MeshFactory.Ellipsoid(Vector3.one * 0.024f, 12, 8), Matrix4x4.Translate(at));
            }
        }

        private static void King(MeshBuilder body, MeshBuilder trim)
        {
            var p = new Profile();
            Foot(p, 0.35f);
            p.P(0.197f, 0.168f).P(0.172f, 0.22f).P(0.15f, 0.29f).P(0.13f, 0.38f).P(0.114f, 0.47f).P(0.106f, 0.555f);
            Collar(p, 0.18f, 0.562f, 0.13f);
            p.P(0.09f, 0.622f).P(0.086f, 0.635f).P(0.092f, 0.67f).P(0.105f, 0.71f).P(0.127f, 0.75f).P(0.152f, 0.785f).P(0.17f, 0.81f).P(0.178f, 0.825f);
            p.P(0.18f, 0.835f).P(0.172f, 0.843f).P(0.15f, 0.846f).P(0.12f, 0.858f).P(0.085f, 0.872f).P(0.05f, 0.88f).P(0.034f, 0.882f);
            p.P(0.034f, 0.893f).P(0.046f, 0.9f).P(0.047f, 0.912f).P(0.03f, 0.918f).P(0f, 0.919f);
            p.LatheInto(body, trim, Segments);
            // The cross faces the players: its outline lies in x/y.
            const float bar = 0.026f, arm = 0.066f, bottom = 0.905f, top = 1.055f, armLow = 0.965f, armHigh = 1.012f;
            var cross = new List<Vector2>
            {
                new Vector2(-bar, bottom), new Vector2(bar, bottom), new Vector2(bar, armLow), new Vector2(arm, armLow),
                new Vector2(arm, armHigh), new Vector2(bar, armHigh), new Vector2(bar, top), new Vector2(-bar, top),
                new Vector2(-bar, armHigh), new Vector2(-arm, armHigh), new Vector2(-arm, armLow), new Vector2(-bar, armLow)
            };
            trim.Append(MeshFactory.Extrude(cross, 0.05f, 0.011f, 2));
        }

        // ------------------------------------------------------------------ shapes

        /// <summary>
        /// A lathe whose rings share their vertices, so a <paramref name="deform"/> can carve it (the bishop's slit) and
        /// the normals still come out smooth: they are averaged from the faces around each vertex, the seam included.
        /// </summary>
        private static MeshBuilder LatheGrid(IList<Vector2> profile, int segments, Func<Vector3, Vector3> deform)
        {
            int rows = profile.Count;
            int columns = segments + 1;
            var positions = new Vector3[rows * columns];
            for (int i = 0; i < rows; i++)
            {
                for (int s = 0; s < segments; s++)
                {
                    float angle = 2f * Mathf.PI * s / segments;
                    var point = new Vector3(profile[i].x * Mathf.Cos(angle), profile[i].y, profile[i].x * Mathf.Sin(angle));
                    positions[i * columns + s] = deform(point);
                }
                positions[i * columns + segments] = positions[i * columns];
            }
            var normals = new Vector3[rows * columns];
            for (int i = 0; i < rows - 1; i++)
            {
                for (int s = 0; s < segments; s++)
                {
                    int a = i * columns + s;
                    int b = (i + 1) * columns + s;
                    int c = (i + 1) * columns + s + 1;
                    int d = i * columns + s + 1;
                    Vector3 face = Vector3.Cross(positions[b] - positions[a], positions[c] - positions[a]) + Vector3.Cross(positions[c] - positions[a], positions[d] - positions[a]);
                    int next = (s + 1) % segments;
                    normals[i * columns + s] += face;
                    normals[(i + 1) * columns + s] += face;
                    normals[(i + 1) * columns + next] += face;
                    normals[i * columns + next] += face;
                }
            }
            var mesh = new MeshBuilder();
            for (int i = 0; i < rows; i++)
            {
                for (int s = 0; s <= segments; s++)
                {
                    Vector3 normal = normals[i * columns + s % segments].normalized;
                    mesh.Add(positions[i * columns + s], normal, new Vector2(s / (float)segments, i / (float)(rows - 1)));
                }
            }
            for (int i = 0; i < rows - 1; i++)
            {
                for (int s = 0; s < segments; s++)
                {
                    int a = i * columns + s;
                    mesh.Quad(a, a + columns, a + columns + 1, a + 1);
                }
            }
            return mesh;
        }

        /// <summary>A block of a ring (a merlon): from radius <paramref name="inner"/> to <paramref name="outer"/>, between two heights and two angles in degrees.</summary>
        private static void RingSector(MeshBuilder mesh, float inner, float outer, float bottom, float top, float from, float to, int steps)
        {
            for (int i = 0; i < steps; i++)
            {
                float a0 = Mathf.Lerp(from, to, i / (float)steps) * Mathf.Deg2Rad;
                float a1 = Mathf.Lerp(from, to, (i + 1) / (float)steps) * Mathf.Deg2Rad;
                var r0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0));
                var r1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));
                // The outer wall, the inner wall and the top.
                Quad(mesh, r0 * outer + Vector3.up * bottom, r0 * outer + Vector3.up * top, r1 * outer + Vector3.up * top, r1 * outer + Vector3.up * bottom, r0, r0, r1, r1);
                Quad(mesh, r0 * inner + Vector3.up * bottom, r1 * inner + Vector3.up * bottom, r1 * inner + Vector3.up * top, r0 * inner + Vector3.up * top, -r0, -r1, -r1, -r0);
                Quad(mesh, r0 * inner + Vector3.up * top, r1 * inner + Vector3.up * top, r1 * outer + Vector3.up * top, r0 * outer + Vector3.up * top, Vector3.up, Vector3.up, Vector3.up, Vector3.up);
            }
            // The two ends.
            float start = from * Mathf.Deg2Rad;
            float end = to * Mathf.Deg2Rad;
            var s = new Vector3(Mathf.Cos(start), 0f, Mathf.Sin(start));
            var e = new Vector3(Mathf.Cos(end), 0f, Mathf.Sin(end));
            var startNormal = new Vector3(Mathf.Sin(start), 0f, -Mathf.Cos(start));
            var endNormal = new Vector3(-Mathf.Sin(end), 0f, Mathf.Cos(end));
            Quad(mesh, s * inner + Vector3.up * bottom, s * inner + Vector3.up * top, s * outer + Vector3.up * top, s * outer + Vector3.up * bottom, startNormal, startNormal, startNormal, startNormal);
            Quad(mesh, e * inner + Vector3.up * bottom, e * outer + Vector3.up * bottom, e * outer + Vector3.up * top, e * inner + Vector3.up * top, endNormal, endNormal, endNormal, endNormal);
        }

        /// <summary>A quad with its own normals, wound so its front faces the way the normals point.</summary>
        private static void Quad(MeshBuilder mesh, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 na, Vector3 nb, Vector3 nc, Vector3 nd)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), na + nb + nc + nd) < 0f)
            {
                (b, d) = (d, b);
                (nb, nd) = (nd, nb);
            }
            int ia = mesh.Add(a, na, new Vector2(0f, 0f));
            int ib = mesh.Add(b, nb, new Vector2(0f, 1f));
            int ic = mesh.Add(c, nc, new Vector2(1f, 1f));
            int id = mesh.Add(d, nd, new Vector2(1f, 0f));
            mesh.Quad(ia, ib, ic, id);
        }

        /// <summary>
        /// A lathe profile as (radius, height) points from the axis at the foot up to the axis at the top, in runs of body
        /// and trim: each run is lathed into its own submesh, the point where they meet in both.
        /// </summary>
        private sealed class Profile
        {
            private readonly List<(List<Vector2> points, bool trim)> runs = new List<(List<Vector2> points, bool trim)>();
            private List<Vector2> current;
            private bool trimming;

            public Profile()
            {
                current = new List<Vector2>();
                runs.Add((current, false));
            }

            public Profile Body()
            {
                return Switch(false);
            }

            public Profile Trim()
            {
                return Switch(true);
            }

            public Profile P(float radius, float height)
            {
                current.Add(new Vector2(radius, height));
                return this;
            }

            /// <summary>Points on a circle around (<paramref name="cx"/>, <paramref name="cy"/>) from one angle to another, in degrees.</summary>
            public Profile Arc(float cx, float cy, float radius, float from, float to, int steps)
            {
                for (int i = 0; i <= steps; i++)
                {
                    float angle = Mathf.Lerp(from, to, i / (float)steps) * Mathf.Deg2Rad;
                    var point = new Vector2(cx + Mathf.Cos(angle) * radius, cy + Mathf.Sin(angle) * radius);
                    if (current.Count > 0 && (current[current.Count - 1] - point).sqrMagnitude < 1e-10f)
                    {
                        continue;
                    }
                    current.Add(point);
                }
                return this;
            }

            public void LatheInto(MeshBuilder body, MeshBuilder trim, int segments)
            {
                foreach ((List<Vector2> points, bool isTrim) in runs)
                {
                    if (points.Count >= 2)
                    {
                        (isTrim ? trim : body).Append(MeshFactory.Lathe(points, segments));
                    }
                }
            }

            private Profile Switch(bool trim)
            {
                if (trim == trimming)
                {
                    return this;
                }
                trimming = trim;
                var next = new List<Vector2>();
                if (current.Count > 0)
                {
                    next.Add(current[current.Count - 1]);
                }
                current = next;
                runs.Add((current, trim));
                return this;
            }
        }
    }
}
