using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace Portfolio.DeathRace.EditorTools
{
    /// <summary>
    /// Pictures of the tracks for judging them without playing: a line drawing of every layout (centre line, road,
    /// barriers, racing line coloured by its safe speed, grid, pickups, line, set pieces; see TrackPreview.Lines.cs) and,
    /// per look, every track built in an empty scene and photographed from straight above and from the race camera's
    /// angle at the grid, at the slowest corner and half a lap on. Nothing is saved to the project.
    ///
    /// Batch: <c>-executeMethod Portfolio.DeathRace.EditorTools.TrackPreview.RenderAll -trackpreview &lt;folder&gt;</c>
    /// (<c>DrawLayouts</c> for the drawings alone; <c>-trackpreview-only 3</c> for one track). Without a folder the
    /// pictures go to the project's Logs/TrackPreview.
    /// </summary>
    internal static partial class TrackPreview
    {
        private const string FolderArgument = "-trackpreview";
        private const string OnlyArgument = "-trackpreview-only";
        private const float CameraPitch = 60f;
        private const float CameraHeight = 38f;
        private const float CameraFieldOfView = 50f;
        private const float OverviewPixelsPerMetre = 4f;
        private const int OverviewMaxPixels = 2400;

        [MenuItem("Death Race/Previews/Track Pictures")]
        public static void RenderAll()
        {
            string folder = Folder();
            DrawLayouts(folder);
            RenderLooks(folder);
            Debug.Log($"Track previews written to {folder}");
        }

        public static void DrawLayouts()
        {
            string folder = Folder();
            DrawLayouts(folder);
            Debug.Log($"Track drawings written to {folder}");
        }

        private static void DrawLayouts(string folder)
        {
            string lines = Path.Combine(folder, "lines");
            Directory.CreateDirectory(lines);
            foreach (int index in Indices())
            {
                TrackLayout layout = TrackLayouts.Of(index);
                var geometry = new TrackGeometry(layout);
                List<string> problems = geometry.Problems();
                TrackMeasures measures = geometry.Measure();
                string file = Path.Combine(lines, $"{index}-{Slug(layout.Name)}.png");
                File.WriteAllBytes(file, Draw(geometry));
                Debug.Log($"{layout.Name}: {measures.Length:0} m, width {measures.MinWidth:0.0}-{measures.MaxWidth:0.0}, tightest radius {measures.MinRadius:0.0} m, " +
                          $"clearance {measures.Clearance * TrackGeometry.ClearanceWidths:0.00} widths, {(problems.Count == 0 ? "OK" : string.Join("; ", problems))}");
            }
        }

        private static void RenderLooks(string folder)
        {
            var looks = new List<(string name, string path, bool night)>
            {
                ("scorched", DeathRaceThemeBuilder.ScorchedPath, false),
                ("nightfall", DeathRaceThemeBuilder.NightfallPath, true)
            };
            foreach ((string name, string path, bool night) in looks)
            {
                string themeFolder = Path.Combine(folder, name);
                Directory.CreateDirectory(themeFolder);
                foreach (int index in Indices())
                {
                    TrackLayout layout = TrackLayouts.Of(index);
                    // A new scene unloads the assets nothing in it uses, so the look is loaded after the stage is set.
                    using (var stage = new Stage())
                    {
                        DeathRaceTheme theme = Theme(path, night);
                        var content = DeathRaceAssets.Load<DeathRaceContent>(DeathRaceContentBuilder.ContentPath);
                        stage.Light(theme);
                        var watch = System.Diagnostics.Stopwatch.StartNew();
                        Track track = TrackBuilder.Build(layout, theme, content, null);
                        long built = watch.ElapsedMilliseconds;
                        stage.Adopt(track.gameObject);
                        string slug = $"{index}-{Slug(layout.Name)}";
                        stage.Overview(track, Path.Combine(themeFolder, $"{slug}-above.png"));
                        stage.Chase(track, Path.Combine(themeFolder, $"{slug}-grid.png"), GridView(track));
                        stage.Chase(track, Path.Combine(themeFolder, $"{slug}-corner.png"), SlowestCorner(track));
                        stage.Chase(track, Path.Combine(themeFolder, $"{slug}-half.png"), track.LineDistance + track.Path.Length * 0.5f);
                        Debug.Log($"{name} {layout.Name}: built in {built} ms with {theme.name}, {track.GetComponentsInChildren<MeshRenderer>().Length} renderers, " +
                                  $"{track.GetComponentsInChildren<Light>().Length} lights, {track.GetComponentsInChildren<BoxCollider>().Length} wall colliders");
                    }
                }
            }
        }

        /// <summary>The theme asset, or a plain one (the fallbacks show) while the art builder has not made it.</summary>
        private static DeathRaceTheme Theme(string path, bool night)
        {
            var theme = DeathRaceAssets.Load<DeathRaceTheme>(path);
            if (theme != null)
            {
                return theme;
            }
            theme = ScriptableObject.CreateInstance<DeathRaceTheme>();
            theme.name = night ? "Nightfall (plain)" : "Scorched (plain)";
            theme.night = night;
            if (night)
            {
                theme.sunColor = new Color(0.5f, 0.58f, 0.9f, 1f);
                theme.sunIntensity = 0.3f;
                theme.ambientSky = new Color(0.12f, 0.14f, 0.24f, 1f);
                theme.ambientEquator = new Color(0.1f, 0.09f, 0.14f, 1f);
                theme.ambientGround = new Color(0.05f, 0.05f, 0.07f, 1f);
                theme.background = new Color(0.03f, 0.03f, 0.05f, 1f);
            }
            return theme;
        }

        private static float GridView(Track track)
        {
            return track.LineDistance - 12f;
        }

        private static float SlowestCorner(Track track)
        {
            float slowest = float.MaxValue;
            float at = 0f;
            for (int i = 0; i < track.Path.Count; i++)
            {
                float speed = track.SafeSpeed(i * track.Path.Spacing);
                if (speed < slowest)
                {
                    slowest = speed;
                    at = i * track.Path.Spacing;
                }
            }
            return at + 15f;
        }

        private static IEnumerable<int> Indices()
        {
            string only = Argument(OnlyArgument);
            if (only != null && int.TryParse(only, out int index))
            {
                yield return Mathf.Clamp(index, 0, TrackLayouts.Count - 1);
                yield break;
            }
            for (int i = 0; i < TrackLayouts.Count; i++)
            {
                yield return i;
            }
        }

        private static string Folder()
        {
            string folder = Argument(FolderArgument);
            if (string.IsNullOrEmpty(folder))
            {
                folder = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? ".", "Logs", "TrackPreview");
            }
            Directory.CreateDirectory(folder);
            return folder;
        }

        private static string Argument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++)
            {
                if (args[i] == name)
                {
                    return args[i + 1];
                }
            }
            return null;
        }

        private static string Slug(string name)
        {
            return name.Replace(" ", "");
        }

        /// <summary>
        /// An empty scene lit like the look (sun, ambient, background, its volume) with a camera that photographs a track
        /// from above or from the race camera's angle. Everything it makes goes when it is disposed.
        /// </summary>
        private sealed class Stage : IDisposable
        {
            private readonly List<Object> made = new List<Object>();
            private readonly Camera camera;
            private DeathRaceTheme theme;
            private bool warm;

            public Stage()
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var cameraObject = new GameObject("Camera");
                made.Add(cameraObject);
                camera = cameraObject.AddComponent<Camera>();
                camera.enabled = false;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.allowHDR = true;
                UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
                data.renderPostProcessing = true;
                data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                data.renderShadows = true;
            }

            /// <summary>Lights the stage like the look: its sun, ambient light, background, fog and volume.</summary>
            public void Light(DeathRaceTheme look)
            {
                theme = look;
                var sun = new GameObject("Sun").AddComponent<Light>();
                made.Add(sun.gameObject);
                sun.type = LightType.Directional;
                sun.shadows = LightShadows.Soft;
                sun.color = look.sunColor;
                sun.intensity = look.sunIntensity;
                sun.transform.rotation = Quaternion.Euler(look.sunEuler);
                RenderSettings.sun = sun;
                RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = look.ambientSky;
                RenderSettings.ambientEquatorColor = look.ambientEquator;
                RenderSettings.ambientGroundColor = look.ambientGround;
                RenderSettings.fogColor = look.background;
                RenderSettings.fogMode = FogMode.Linear;
                RenderSettings.fogStartDistance = look.fogStart;
                RenderSettings.fogEndDistance = look.fogEnd;
                camera.backgroundColor = look.background;
                if (look.volume != null)
                {
                    var volume = new GameObject("Volume").AddComponent<Volume>();
                    made.Add(volume.gameObject);
                    volume.isGlobal = true;
                    volume.sharedProfile = look.volume;
                }
            }

            public void Adopt(GameObject thing)
            {
                made.Add(thing);
            }

            /// <summary>Straight down on the whole track, orthographic (no fog: it would grey the picture out from up there).</summary>
            public void Overview(Track track, string file)
            {
                Bounds bounds = track.Bounds;
                bounds.Expand(new Vector3(24f, 0f, 24f));
                float scale = Mathf.Min(OverviewPixelsPerMetre, OverviewMaxPixels / Mathf.Max(bounds.size.x, bounds.size.z));
                int width = Mathf.RoundToInt(bounds.size.x * scale);
                int height = Mathf.RoundToInt(bounds.size.z * scale);
                camera.orthographic = true;
                camera.orthographicSize = bounds.size.z * 0.5f;
                camera.aspect = width / (float)height;
                camera.nearClipPlane = 1f;
                camera.farClipPlane = 400f;
                camera.transform.SetPositionAndRotation(new Vector3(bounds.center.x, 200f, bounds.center.z), Quaternion.Euler(90f, 0f, 0f));
                RenderSettings.fog = false;
                Shoot(file, width, height);
            }

            /// <summary>What the race camera sees of a car at <paramref name="distance"/> (north up, sixty degrees down).</summary>
            public void Chase(Track track, string file, float distance)
            {
                Vector3 target = Gamebox.Racing.RacePath.ToWorld(track.RacingLinePoint(distance));
                camera.orthographic = false;
                camera.fieldOfView = CameraFieldOfView;
                camera.aspect = 16f / 9f;
                camera.nearClipPlane = 0.3f;
                camera.farClipPlane = 600f;
                Quaternion rotation = Quaternion.Euler(CameraPitch, 0f, 0f);
                float range = CameraHeight / Mathf.Sin(CameraPitch * Mathf.Deg2Rad);
                camera.transform.SetPositionAndRotation(target - rotation * Vector3.forward * range, rotation);
                RenderSettings.fog = theme.fog;
                Shoot(file, 1600, 900);
            }

            private void Shoot(string file, int width, int height)
            {
                var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
                try
                {
                    camera.targetTexture = target;
                    if (!warm)
                    {
                        camera.Render();
                        warm = true;
                    }
                    camera.Render();
                    RenderTexture previous = RenderTexture.active;
                    RenderTexture.active = target;
                    var read = new Texture2D(width, height, TextureFormat.RGB24, false);
                    read.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    read.Apply();
                    RenderTexture.active = previous;
                    File.WriteAllBytes(file, read.EncodeToPNG());
                    Object.DestroyImmediate(read);
                }
                finally
                {
                    camera.targetTexture = null;
                    target.Release();
                    Object.DestroyImmediate(target);
                }
            }

            public void Dispose()
            {
                foreach (Object thing in made)
                {
                    if (thing != null)
                    {
                        Object.DestroyImmediate(thing);
                    }
                }
            }
        }
    }
}
