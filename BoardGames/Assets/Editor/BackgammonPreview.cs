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
    /// Pictures of the backgammon board without the game: builds the backgammon art, puts the board of the scene parts on
    /// the table in a temporary scene, sets a game in the middle (a stack of seven, a checker on the bar picked with its
    /// target lit, the last play marked, the dice of the turn, the cube owned by Black) by the game's fields, and renders
    /// PNGs through a camera's render texture, per look: the whole board from the game's angle, and the home boards up
    /// close. The pictures go to the folder after <c>-backgammonPreviewOut</c> on the command line, else to Logs/shots of
    /// the project. Batch: <c>-executeMethod Portfolio.BoardGames.EditorTools.BackgammonPreview.Render</c>.
    /// </summary>
    internal static class BackgammonPreview
    {
        private const float Surface = BoardGamesSceneBuilder.Surface;
        private const int Width = 1600;
        private const int Height = 1000;

        [MenuItem("Board Games/Backgammon Preview", priority = 41)]
        public static void Render()
        {
            string folder = OutputFolder();
            Directory.CreateDirectory(folder);
            BackgammonArt.BuildShared();
            foreach (BoardGamesThemeSpec spec in BoardGamesThemeSpec.All)
            {
                BackgammonArt.BuildLook(spec);
            }
            AssetDatabase.SaveAssets();
            foreach (BoardGamesThemeSpec spec in BoardGamesThemeSpec.All)
            {
                string suffix = spec == BoardGamesThemeSpec.Classic ? "" : "-" + spec.Name.ToLowerInvariant();
                Shot(spec, 52f, 0f, 25f, Vector3.zero, true, Path.Combine(folder, $"preview-backgammon{suffix}.png"));
                Shot(spec, 52f, 0f, 11f, new Vector3(2.3f, 0f, -1.6f), false, Path.Combine(folder, $"preview-backgammon{suffix}-close.png"));
                Shot(spec, 52f, 180f, 25f, Vector3.zero, true, Path.Combine(folder, $"preview-backgammon{suffix}-black.png"));
            }
            Debug.Log($"Backgammon preview written to {folder}");
        }

        private static string OutputFolder()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-backgammonPreviewOut")
                {
                    return args[i + 1];
                }
            }
            return Path.Combine(Directory.GetCurrentDirectory(), "Logs", "shots");
        }

        /// <summary>A game in the middle, set by its fields (the model's rules are not needed to show it).</summary>
        private static BackgammonGame MiddleGame()
        {
            var game = new BackgammonGame
            {
                sideToAct = (int)Side.Light,
                phase = (int)BackgammonPhase.Moving,
                die1 = 5,
                die2 = 3,
                cube = true,
                cubeValue = 2,
                cubeOwner = (int)Side.Dark,
                turn = 23,
                lastPlay = new[] { 11, 16, 16, 19 },
                lastSide = (int)Side.Dark
            };
            game.board[23] = 1;
            game.board[12] = 2;
            game.board[7] = 2;
            game.board[5] = 7;
            game.board[4] = 2;
            game.bar[0] = 1;
            game.board[0] = -1;
            game.board[11] = -4;
            game.board[16] = -3;
            game.board[18] = -4;
            game.board[19] = -2;
            game.off[1] = 1;
            return game;
        }

        /// <summary>
        /// Renders the middle game in <paramref name="spec"/>'s look to <paramref name="file"/>: with the checker on the bar
        /// <paramref name="picked"/> and its target lit, else with the checkers of the home board marked as movable.
        /// </summary>
        private static void Shot(BoardGamesThemeSpec spec, float pitch, float yaw, float distance, Vector3 offset, bool picked, string file)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BoardGamesTheme theme = Theme(spec);
            Lights(spec);
            var table = new GameObject("Table");
            table.transform.localPosition = new Vector3(0f, -0.001f, 0f);
            table.AddComponent<MeshFilter>().sharedMesh = BoardGamesArtBuilder.Mesh("Table");
            table.AddComponent<MeshRenderer>().sharedMaterial = BoardGamesArtBuilder.Material("Table", spec);

            BackgammonBoard board = BackgammonSceneParts.BuildBoard(null, spec);
            board.transform.position = new Vector3(0f, Surface, 0f);
            board.ApplyTheme(theme);
            Side bottom = Mathf.Approximately(yaw, 180f) ? Side.Dark : Side.Light;
            board.SetBottom(bottom);
            BackgammonGame game = MiddleGame();
            board.Show(game);
            var marks = new BackgammonMarks { side = Side.Light, selected = picked ? BackgammonGame.Bar : -1, hovered = 21, lastSide = Side.Dark };
            if (picked)
            {
                marks.targets.Add(21);
            }
            else
            {
                marks.movable.AddRange(new[] { 4, 5, 7 });
            }
            marks.lastFrom.AddRange(new[] { 11, 16 });
            marks.lastTo.AddRange(new[] { 16, 19 });
            board.SetMarks(marks);

            var cameraObject = new GameObject("Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = spec.Background;
            camera.fieldOfView = 30f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 150f;
            camera.allowHDR = true;
            camera.aspect = Width / (float)Height;
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
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
            Debug.Log($"Backgammon preview: {file}");
        }

        /// <summary>A theme with what the board reads: the backgammon look, the highlight and shadow materials, the colours.</summary>
        private static BoardGamesTheme Theme(BoardGamesThemeSpec spec)
        {
            var theme = ScriptableObject.CreateInstance<BoardGamesTheme>();
            BackgammonArt.Dress(theme, spec);
            theme.highlight = BoardGamesArtBuilder.Material("Highlight", spec);
            theme.contactShadow = BoardGamesArtBuilder.Material("ContactShadow", spec);
            theme.selectedColor = spec.Selected;
            theme.legalColor = spec.Legal;
            theme.captureColor = spec.Capture;
            theme.lastMoveColor = spec.LastMove;
            theme.hoverColor = spec.Hover;
            theme.sides = new[]
            {
                new SideLook { nameKey = "White", color = spec.LightColor },
                new SideLook { nameKey = "Black", color = spec.DarkColor }
            };
            return theme;
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
    }
}
