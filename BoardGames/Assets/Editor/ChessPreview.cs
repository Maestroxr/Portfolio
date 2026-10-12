using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace Portfolio.BoardGames.EditorTools
{
    /// <summary>
    /// Pictures of the chess pieces without the game: builds the chess art, lays a board out in a temporary scene, stands
    /// the pieces on it through <see cref="ChessPieceView"/> and renders PNGs through a camera's render texture, per
    /// look: the whole board from the game's angle (an Italian opening), and the six kinds of piece of both sides up
    /// close, from the game's angle and from the side. The pictures go to the folder after <c>-chessPreviewOut</c> on the
    /// command line, else to Logs/shots of the project. Batch: <c>-executeMethod Portfolio.BoardGames.EditorTools.ChessPreview.Render</c>.
    /// </summary>
    internal static class ChessPreview
    {
        private const string Italian = "r1bqk1nr/pppp1ppp/2n5/2b1p3/2B1P3/5N2/PPPP1PPP/RNBQK2R";
        private const string Lineup = "8/8/8/1kqbnrp1/1KQBNRP1/8/8/8";
        private const float Surface = 0.15f;
        private const int Width = 1600;
        private const int Height = 1000;

        [MenuItem("Board Games/Chess Preview", priority = 40)]
        public static void Render()
        {
            string folder = OutputFolder();
            Directory.CreateDirectory(folder);
            ChessArt.BuildShared();
            foreach (BoardGamesThemeSpec spec in BoardGamesThemeSpec.All)
            {
                ChessArt.BuildLook(spec);
            }
            AssetDatabase.SaveAssets();
            foreach (BoardGamesThemeSpec spec in BoardGamesThemeSpec.All)
            {
                string suffix = spec == BoardGamesThemeSpec.Classic ? "" : "-" + spec.Name.ToLowerInvariant();
                Shot(spec, Italian, 52f, 17.5f, Vector3.zero, Path.Combine(folder, $"preview-chess{suffix}.png"));
                Shot(spec, Lineup, 52f, 6.2f, new Vector3(0f, 0.35f, 0f), Path.Combine(folder, $"preview-chess{suffix}-close.png"));
                Shot(spec, Lineup, 14f, 6.2f, new Vector3(0f, 0.45f, 0f), Path.Combine(folder, $"preview-chess{suffix}-side.png"));
            }
            Debug.Log($"Chess preview written to {folder}");
        }

        private static string OutputFolder()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-chessPreviewOut")
                {
                    return args[i + 1];
                }
            }
            return Path.Combine(Directory.GetCurrentDirectory(), "Logs", "shots");
        }

        /// <summary>Renders <paramref name="placement"/> (the placement field of a FEN) in <paramref name="spec"/>'s look to <paramref name="file"/>.</summary>
        private static void Shot(BoardGamesThemeSpec spec, string placement, float pitch, float distance, Vector3 offset, string file)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var theme = ScriptableObject.CreateInstance<BoardGamesTheme>();
            ChessArt.Dress(theme, spec);
            Lights(spec);
            Board(spec);
            Material shadow = BoardGamesArtBuilder.Material("ContactShadow", spec);
            Material highlight = BoardGamesArtBuilder.Material("Highlight", spec);
            sbyte[] cells = Cells(placement);
            for (int square = 0; square < cells.Length; square++)
            {
                if (cells[square] == ChessPieces.None)
                {
                    continue;
                }
                GameObject piece = ChessSceneParts.PieceObject(spec);
                var view = piece.GetComponent<ChessPieceView>();
                view.SetLook(theme.chess, shadow, highlight);
                view.Show(cells[square], new Vector3(square % 8 - 3.5f, Surface, square / 8 - 3.5f), Quaternion.identity);
                if (placement == Lineup && square == ChessGame.Square(4, 4))
                {
                    // The glow of a piece a move may take, on Black's knight.
                    view.Glow(spec.Capture, 1f);
                }
            }

            var cameraObject = new GameObject("Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = spec.Background;
            camera.fieldOfView = 30f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 150f;
            camera.allowHDR = true;
            camera.aspect = Width / (float)Height;
            Quaternion rotation = Quaternion.Euler(pitch, 0f, 0f);
            Vector3 pivot = new Vector3(0f, Surface, 0f) + offset;
            camera.transform.SetPositionAndRotation(pivot - rotation * Vector3.forward * distance, rotation);
            UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.renderShadows = true;
            var volume = new GameObject("Volume").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = BoardGamesAssets.Load<VolumeProfile>(BoardGamesSceneBuilder.VolumePath);

            var target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            camera.targetTexture = target;
            // The first frame of a batch session draws before the shaders are ready, so one is thrown away.
            camera.Render();
            camera.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            var picture = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            picture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            picture.Apply();
            RenderTexture.active = previous;
            camera.targetTexture = null;
            File.WriteAllBytes(file, picture.EncodeToPNG());
            Object.DestroyImmediate(picture);
            target.Release();
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(theme);
            Debug.Log($"Chess preview: {file}");
        }

        /// <summary>The look's ambient light, the sun and the fill, as the game's scene has them.</summary>
        private static void Lights(BoardGamesThemeSpec spec)
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = spec.AmbientSky;
            RenderSettings.ambientEquatorColor = spec.AmbientEquator;
            RenderSettings.ambientGroundColor = spec.AmbientGround;
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = spec.SunColor;
            sun.intensity = spec.SunIntensity;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.65f;
            sun.shadowBias = 0.02f;
            sun.shadowNormalBias = 0.3f;
            sun.transform.rotation = Quaternion.Euler(52f, -32f, 0f);
            sun.gameObject.AddComponent<UniversalAdditionalLightData>();
            RenderSettings.sun = sun;
            var fill = new GameObject("Fill").AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = spec.AmbientSky;
            fill.intensity = 0.35f;
            fill.shadows = LightShadows.None;
            fill.transform.rotation = Quaternion.Euler(30f, 150f, 0f);
            fill.gameObject.AddComponent<UniversalAdditionalLightData>();
        }

        /// <summary>The table, the slab and the 64 squares (a1 dark), laid out as the board view does.</summary>
        private static void Board(BoardGamesThemeSpec spec)
        {
            MeshObject("Table", BoardGamesArtBuilder.Mesh("Table"), BoardGamesArtBuilder.Material("Table", spec), new Vector3(0f, -0.001f, 0f), Vector3.one);
            MeshObject("Slab", BoardGamesArtBuilder.Mesh("Slab"), BoardGamesArtBuilder.Material("Slab", spec), new Vector3(0f, Surface - 0.152f, 0f), new Vector3(8.6f, 1f, 8.6f));
            Mesh square = BoardGamesArtBuilder.Mesh("Square");
            Material light = BoardGamesArtBuilder.Material("LightSquare", spec);
            Material dark = BoardGamesArtBuilder.Material("DarkSquare", spec);
            for (int i = 0; i < 64; i++)
            {
                int file = i % 8;
                int rank = i / 8;
                MeshObject($"Square {i}", square, (file + rank) % 2 == 0 ? dark : light, new Vector3(file - 3.5f, Surface, rank - 3.5f), Vector3.one);
            }
        }

        private static void MeshObject(string name, Mesh mesh, Material material, Vector3 position, Vector3 scale)
        {
            var go = new GameObject(name);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        /// <summary>The cells of a FEN placement (rank 8 first), without the model.</summary>
        private static sbyte[] Cells(string placement)
        {
            var cells = new sbyte[64];
            int rank = 7;
            int file = 0;
            foreach (char c in placement)
            {
                if (c == '/')
                {
                    rank--;
                    file = 0;
                    continue;
                }
                if (char.IsDigit(c))
                {
                    file += c - '0';
                    continue;
                }
                ChessPieceKind kind = char.ToUpperInvariant(c) switch
                {
                    'P' => ChessPieceKind.Pawn,
                    'N' => ChessPieceKind.Knight,
                    'B' => ChessPieceKind.Bishop,
                    'R' => ChessPieceKind.Rook,
                    'Q' => ChessPieceKind.Queen,
                    'K' => ChessPieceKind.King,
                    _ => ChessPieceKind.None
                };
                if (kind != ChessPieceKind.None && rank >= 0 && file < 8)
                {
                    cells[rank * 8 + file] = ChessPieces.Make(char.IsUpper(c) ? Side.Light : Side.Dark, kind);
                }
                file++;
            }
            return cells;
        }
    }
}
