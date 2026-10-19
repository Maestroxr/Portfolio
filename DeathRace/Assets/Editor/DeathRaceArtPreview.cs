using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Portfolio.DeathRace.EditorTools
{
    /// <summary>
    /// Contact sheets of the world art, rendered in batch mode to PNG files so the looks can be judged without the
    /// editor: per look and biome a stretch of road (asphalt, shoulders, a curb on the inside of a bend, barriers, the
    /// line) seen by the race camera, the props and landmarks up close and from high above, the barrier close up; per look
    /// the interface sprites and icons on a canvas. Run with
    /// <c>-executeMethod Portfolio.DeathRace.EditorTools.DeathRaceArtPreview.RenderSheets -artshots &lt;folder&gt;</c>
    /// (or <c>RenderWorld</c> / <c>RenderInterface</c>). Nothing is saved to the project: the scenes are made in memory.
    /// </summary>
    internal static class DeathRaceArtPreview
    {
        private const int ShotWidth = 960;
        private const int ShotHeight = 540;
        private const float RoadWidth = 14f;
        private const float ShoulderWidth = 3f;
        private const float CurbWidth = 1.5f;
        private const float BarrierWidth = 0.5f;
        private const float BarrierHeight = 0.95f;

        /// <summary>Builds the world's art and the looks (not the car side), then renders every sheet.</summary>
        public static void BuildAndRender()
        {
            DeathRaceArtBuilder.BuildWorld();
            DeathRaceThemeBuilder.BuildThemes();
            AssetDatabase.SaveAssets();
            RenderSheets();
        }

        public static void RenderSheets()
        {
            RenderWorld();
            RenderInterface();
            RenderTracks();
        }

        public static void RenderWorld()
        {
            string folder = OutputFolder();
            foreach (DeathRaceThemeSpec spec in DeathRaceThemeSpec.All)
            {
                DeathRaceTheme theme = DeathRaceAssets.Require<DeathRaceTheme>(spec.AssetPath);
                foreach (Biome biome in (Biome[])Enum.GetValues(typeof(Biome)))
                {
                    if (!Wanted(spec.Name, biome.ToString()))
                    {
                        continue;
                    }
                    RenderBiome(theme, spec, biome, Path.Combine(folder, $"{spec.Name}-{biome}.png"));
                }
            }
        }

        /// <summary>
        /// Whether the sheet of <paramref name="look"/> (and <paramref name="biome"/>, or null for the interface) is
        /// wanted: the command line's -artonly lists looks and biomes by name (e.g. "Nightfall,City"); a kind it does
        /// not mention is not filtered.
        /// </summary>
        private static bool Wanted(string look, string biome)
        {
            string only = Argument("-artonly");
            if (string.IsNullOrEmpty(only))
            {
                return true;
            }
            string[] words = only.Split(',');
            bool namesLooks = Array.Exists(words, w => Array.Exists(DeathRaceThemeSpec.All, s => s.Name.Equals(w, StringComparison.OrdinalIgnoreCase)));
            bool namesBiomes = Array.Exists(words, w => Enum.TryParse(w, true, out Biome _));
            bool lookWanted = !namesLooks || Array.Exists(words, w => w.Equals(look, StringComparison.OrdinalIgnoreCase));
            bool biomeWanted = biome == null || !namesBiomes || Array.Exists(words, w => w.Equals(biome, StringComparison.OrdinalIgnoreCase));
            return lookWanted && biomeWanted;
        }

        private static string Argument(string flag)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == flag)
                {
                    return args[i + 1];
                }
            }
            return null;
        }

        private static string OutputFolder()
        {
            string folder = Argument("-artshots") ?? Path.Combine(Path.GetTempPath(), "DeathRaceArt");
            Directory.CreateDirectory(folder);
            return folder;
        }

        // ------------------------------------------------------------------------------------------------
        // The world
        // ------------------------------------------------------------------------------------------------

        private static void RenderBiome(DeathRaceTheme theme, DeathRaceThemeSpec spec, Biome biome, string file)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BiomeLook look = theme.Biome(biome);
            Camera camera = Stage(theme, look);
            var root = new GameObject("Preview").transform;
            BuildStretch(look, root);
            PlaceProps(look, root);
            PlaceCars(theme, root);

            var sheet = new Texture2D(ShotWidth * 2, ShotHeight * 2, TextureFormat.RGBA32, false);
            // The race camera's view of the bend, the props close up, the land from high up, the barrier close up.
            Shot(camera, sheet, 0, 1, new Vector3(4f, 0f, 2f), 38f, 60f, 40f);
            Shot(camera, sheet, 1, 1, new Vector3(0f, 0f, 22f), 20f, 50f, 40f);
            Shot(camera, sheet, 0, 0, new Vector3(0f, 0f, 20f), 120f, 65f, 40f);
            Shot(camera, sheet, 1, 0, new Vector3(-14f, 0f, 9f), 7f, 32f, 40f);
            File.WriteAllBytes(file, sheet.EncodeToPNG());
            Object.DestroyImmediate(sheet);
            Debug.Log($"Art preview: {file}");
        }

        /// <summary>The light, fog, reflected sky and post processing of a look, and a camera.</summary>
        private static Camera Stage(DeathRaceTheme theme, BiomeLook look)
        {
            var sunObject = new GameObject("Sun");
            var sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = theme.sunColor;
            sun.intensity = theme.sunIntensity;
            sun.shadows = (Argument("-artdebug") ?? "").Contains("noshadow") ? LightShadows.None : LightShadows.Soft;
            sun.shadowStrength = 0.75f;
            sun.shadowBias = 0.03f;
            sun.shadowNormalBias = 0.4f;
            sunObject.transform.rotation = Quaternion.Euler(theme.sunEuler);
            sunObject.AddComponent<UniversalAdditionalLightData>();
            RenderSettings.sun = sun;
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = theme.ambientSky;
            RenderSettings.ambientEquatorColor = theme.ambientEquator;
            RenderSettings.ambientGroundColor = theme.ambientGround;
            RenderSettings.fog = theme.fog;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = theme.fogStart;
            RenderSettings.fogEndDistance = theme.fogEnd;
            RenderSettings.fogColor = look.fogColor;
            if (theme.TextureOf(DeathRaceThemeBuilder.ReflectionSlot) is Cubemap sky)
            {
                RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
                RenderSettings.customReflectionTexture = sky;
                RenderSettings.reflectionIntensity = 1f;
            }
            DynamicGI.UpdateEnvironment();

            var volumeObject = new GameObject("Volume");
            var volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = theme.volume;

            var cameraObject = new GameObject("Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = look.fogColor;
            camera.nearClipPlane = 0.5f;
            camera.farClipPlane = 900f;
            camera.allowHDR = true;
            camera.allowMSAA = false;
            camera.enabled = false;
            UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.renderShadows = true;
            return camera;
        }

        /// <summary>Renders one shot (looking north at <paramref name="target"/> from <paramref name="height"/> at <paramref name="pitch"/>) into a quarter of the sheet.</summary>
        private static void Shot(Camera camera, Texture2D sheet, int column, int row, Vector3 target, float height, float pitch, float fov)
        {
            float back = height / Mathf.Tan(pitch * Mathf.Deg2Rad);
            camera.fieldOfView = fov;
            camera.transform.SetPositionAndRotation(target + new Vector3(0f, height, -back), Quaternion.Euler(pitch, 0f, 0f));
            Color[] pixels = Render(camera, ShotWidth, ShotHeight);
            sheet.SetPixels(column * ShotWidth, row * ShotHeight, ShotWidth, ShotHeight, pixels);
        }

        private static Color[] Render(Camera camera, int width, int height)
        {
            RenderTexture target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            camera.targetTexture = target;
            // The first render of a session comes out untextured: render twice.
            camera.Render();
            camera.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            var read = new Texture2D(width, height, TextureFormat.RGBA32, false);
            read.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            read.Apply();
            RenderTexture.active = previous;
            camera.targetTexture = null;
            RenderTexture.ReleaseTemporary(target);
            Color[] pixels = read.GetPixels();
            Object.DestroyImmediate(read);
            return pixels;
        }

        /// <summary>
        /// A stretch of track along X with a bend to the north at its east end, in the UV convention of the track
        /// builder: the ground, the road, shoulders, a curb on the inside of the bend, barriers both sides and the line.
        /// </summary>
        private static void BuildStretch(BiomeLook look, Transform root)
        {
            var centre = new List<Vector2>();
            for (float x = -60f; x <= 10f; x += 2f)
            {
                centre.Add(new Vector2(x, 0f));
            }
            const float radius = 24f;
            var bendCentre = new Vector2(10f, radius);
            for (int i = 1; i <= 24; i++)
            {
                float a = -Mathf.PI * 0.5f + i * (Mathf.PI * 0.5f / 24f);
                centre.Add(bendCentre + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius);
            }
            for (float z = radius + 2f; z <= 90f; z += 2f)
            {
                centre.Add(new Vector2(10f + radius, z));
            }

            Quad(root, "Ground", look.ground, new Vector3(0f, -0.02f, 20f), 320f, p => new Vector2(p.x / 32f, p.z / 32f));
            float half = RoadWidth * 0.5f;
            Strip(root, "Road", Debugged(look.road), centre, -half, half, 0f, (offset, d) => new Vector2((half - offset) / RoadWidth, d / 16f));
            Strip(root, "ShoulderLeft", look.shoulder, centre, half, half + ShoulderWidth, -0.005f, (offset, d) => new Vector2((offset - half) / ShoulderWidth, d / 8f));
            Strip(root, "ShoulderRight", look.shoulder, centre, -half - ShoulderWidth, -half, -0.005f, (offset, d) => new Vector2((-offset - half) / ShoulderWidth, d / 8f));
            // The bend turns left: its inside is the left side.
            var bend = centre.GetRange(34, 26);
            Strip(root, "Curb", look.curb, bend, half, half + CurbWidth, 0.02f, (offset, d) => new Vector2((offset - half) / CurbWidth, d / 4f));
            Barrier(root, look.barrier, centre, half + ShoulderWidth + BarrierWidth * 0.5f);
            Barrier(root, look.barrier, centre, -half - ShoulderWidth - BarrierWidth * 0.5f);
            var line = new GameObject("Line");
            line.transform.SetParent(root, false);
            line.transform.position = new Vector3(-20f, 0.01f, 0f);
            Mesh lineMesh = MeshOf(new[] { new Vector3(-2f, 0f, -half), new Vector3(-2f, 0f, half), new Vector3(2f, 0f, half), new Vector3(2f, 0f, -half) },
                new[] { new Vector2(RoadWidth / 4f, 0f), new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(RoadWidth / 4f, 1f) });
            line.AddComponent<MeshFilter>().sharedMesh = lineMesh;
            line.AddComponent<MeshRenderer>().sharedMaterial = look.line;
        }

        /// <summary>
        /// A copy of <paramref name="material"/> changed as the command line's -artdebug asks ("flat": no normal map,
        /// "matte": no smoothness, "plain": no base map), to tell where a look comes from; the material itself otherwise.
        /// </summary>
        private static Material Debugged(Material material)
        {
            string debug = Argument("-artdebug");
            if (string.IsNullOrEmpty(debug) || material == null)
            {
                return material;
            }
            var copy = new Material(material);
            if (debug.Contains("flat"))
            {
                copy.SetTexture("_BumpMap", null);
                copy.DisableKeyword("_NORMALMAP");
            }
            if (debug.Contains("matte"))
            {
                copy.SetFloat("_Smoothness", 0f);
            }
            if (debug.Contains("plain"))
            {
                copy.SetTexture("_BaseMap", null);
            }
            return copy;
        }

        private static void Quad(Transform root, string name, Material material, Vector3 center, float size, Func<Vector3, Vector2> uv)
        {
            var corners = new[]
            {
                center + new Vector3(-size * 0.5f, 0f, -size * 0.5f), center + new Vector3(-size * 0.5f, 0f, size * 0.5f),
                center + new Vector3(size * 0.5f, 0f, size * 0.5f), center + new Vector3(size * 0.5f, 0f, -size * 0.5f)
            };
            var uvs = new Vector2[4];
            for (int i = 0; i < 4; i++)
            {
                uvs[i] = uv(corners[i]);
            }
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.AddComponent<MeshFilter>().sharedMesh = MeshOf(corners, uvs);
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        /// <summary>A quad's mesh (corners clockwise seen from above).</summary>
        private static Mesh MeshOf(Vector3[] corners, Vector2[] uvs)
        {
            var mesh = new Mesh();
            mesh.vertices = corners;
            mesh.uv = uvs;
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>A flat strip between two lateral offsets (+ = left of the way of travel) along a centre line.</summary>
        private static void Strip(Transform root, string name, Material material, List<Vector2> centre, float from, float to, float y, Func<float, float, Vector2> uv)
        {
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            float distance = 0f;
            for (int i = 0; i < centre.Count; i++)
            {
                if (i > 0)
                {
                    distance += Vector2.Distance(centre[i - 1], centre[i]);
                }
                Vector2 tangent = (centre[Mathf.Min(i + 1, centre.Count - 1)] - centre[Mathf.Max(i - 1, 0)]).normalized;
                var left = new Vector2(-tangent.y, tangent.x);
                foreach (float offset in new[] { from, to })
                {
                    Vector2 p = centre[i] + left * offset;
                    vertices.Add(new Vector3(p.x, y, p.y));
                    uvs.Add(uv(offset, distance));
                }
                if (i > 0)
                {
                    // Clockwise seen from above: the strip faces up.
                    int a = (i - 1) * 2;
                    triangles.AddRange(new[] { a, a + 1, a + 2, a + 1, a + 3, a + 2 });
                }
            }
            Build(root, name, material, vertices, uvs, triangles);
        }

        /// <summary>A barrier with its centre at <paramref name="offset"/>: two faces (V 0 at the foot to 0.9 at the top) and the cap (V 0.9 to 1).</summary>
        private static void Barrier(Transform root, Material material, List<Vector2> centre, float offset)
        {
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            float halfWidth = BarrierWidth * 0.5f;
            var profile = new[]
            {
                (side: -halfWidth, height: 0f, v: 0f), (side: -halfWidth, height: BarrierHeight, v: 0.9f),
                (side: -halfWidth, height: BarrierHeight, v: 0.9f), (side: halfWidth, height: BarrierHeight, v: 1f),
                (side: halfWidth, height: BarrierHeight, v: 0.9f), (side: halfWidth, height: 0f, v: 0f)
            };
            float distance = 0f;
            for (int i = 0; i < centre.Count; i++)
            {
                if (i > 0)
                {
                    distance += Vector2.Distance(centre[i - 1], centre[i]);
                }
                Vector2 tangent = (centre[Mathf.Min(i + 1, centre.Count - 1)] - centre[Mathf.Max(i - 1, 0)]).normalized;
                var left = new Vector2(-tangent.y, tangent.x);
                foreach (var corner in profile)
                {
                    Vector2 p = centre[i] + left * (offset + corner.side);
                    vertices.Add(new Vector3(p.x, corner.height, p.y));
                    uvs.Add(new Vector2(distance / 4f, corner.v));
                }
                if (i > 0)
                {
                    int a = (i - 1) * profile.Length;
                    int b = i * profile.Length;
                    for (int face = 0; face < profile.Length; face += 2)
                    {
                        triangles.AddRange(new[] { a + face, b + face, a + face + 1, a + face + 1, b + face, b + face + 1 });
                        triangles.AddRange(new[] { a + face, a + face + 1, b + face, a + face + 1, b + face + 1, b + face });
                    }
                }
            }
            Build(root, "Barrier", material, vertices, uvs, triangles);
        }

        private static void Build(Transform root, string name, Material material, List<Vector3> vertices, List<Vector2> uvs, List<int> triangles)
        {
            var mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        /// <summary>The props in a row north of the road, the landmarks behind them, lamps along the north barrier.</summary>
        private static void PlaceProps(BiomeLook look, Transform root)
        {
            float x = -50f;
            foreach (GameObject prop in look.props)
            {
                Place(prop, root, new Vector3(x, 0f, 17f), 0f);
                x += 6.5f;
            }
            float lx = -55f;
            foreach (GameObject landmark in look.landmarks)
            {
                Bounds bounds = BoundsOf(landmark);
                lx += bounds.extents.x;
                Place(landmark, root, new Vector3(lx, 0f, 44f), 0f);
                lx += bounds.extents.x + 6f;
            }
            if (look.lamp != null)
            {
                for (float lampX = -56f; lampX <= 4f; lampX += 20f)
                {
                    Place(look.lamp, root, new Vector3(lampX, 0f, RoadWidth * 0.5f + ShoulderWidth + 1.6f), 180f);
                    Place(look.lamp, root, new Vector3(lampX + 10f, 0f, -(RoadWidth * 0.5f + ShoulderWidth + 1.6f)), 0f);
                }
            }
        }

        /// <summary>A few of ART-CARS' cars on the road, for scale and to see them against the land (when they are built).</summary>
        private static void PlaceCars(DeathRaceTheme theme, Transform root)
        {
            var spots = new[] { (CarModel.Viper, new Vector3(-6f, 0f, 2.5f), 90f), (CarModel.Bulldog, new Vector3(-14f, 0f, -2.5f), 90f), (CarModel.Rustbucket, new Vector3(22f, 0f, 8f), 45f) };
            for (int i = 0; i < spots.Length; i++)
            {
                GameObject prefab = DeathRacePrefabBuilder.CarPrefab(spots[i].Item1);
                if (prefab == null)
                {
                    continue;
                }
                var car = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
                car.transform.SetPositionAndRotation(spots[i].Item2, Quaternion.Euler(0f, spots[i].Item3, 0f));
                var view = car.GetComponent<CarModelView>();
                if (view != null)
                {
                    view.ApplyTheme(theme, theme.RivalColor(i * 3));
                }
            }
        }

        private static Bounds BoundsOf(GameObject prefab)
        {
            var filter = prefab.GetComponent<MeshFilter>();
            return filter != null && filter.sharedMesh != null ? filter.sharedMesh.bounds : new Bounds(Vector3.zero, Vector3.one * 4f);
        }

        private static void Place(GameObject prefab, Transform root, Vector3 at, float yaw)
        {
            if (prefab == null)
            {
                return;
            }
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
            instance.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, yaw, 0f));
        }

        // ------------------------------------------------------------------------------------------------
        // Built tracks
        // ------------------------------------------------------------------------------------------------

        /// <summary>
        /// The real tracks, built by TRACK's TrackBuilder in each look and lit as the game lights them: per track and
        /// look a sheet of the whole track from above, the grid and the sharpest corner seen by the race camera, and a
        /// low view over the scenery. <c>-arttracks 0,3,8</c> picks tracks (all nine otherwise).
        /// </summary>
        public static void RenderTracks()
        {
            string folder = OutputFolder();
            string only = Argument("-arttracks");
            foreach (DeathRaceThemeSpec spec in DeathRaceThemeSpec.All)
            {
                if (!Wanted(spec.Name, null))
                {
                    continue;
                }
                for (int index = 0; index < TrackLayouts.Count; index++)
                {
                    if (!string.IsNullOrEmpty(only) && !Array.Exists(only.Split(','), word => word.Trim() == index.ToString()))
                    {
                        continue;
                    }
                    TrackLayout layout = TrackLayouts.Of(index);
                    RenderTrack(spec, layout, Path.Combine(folder, $"Track{index}-{spec.Name}.png"));
                }
            }
        }

        private static void RenderTrack(DeathRaceThemeSpec spec, TrackLayout layout, string file)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // Loaded after the new scene: opening one unloads what nothing references.
            DeathRaceTheme theme = DeathRaceAssets.Require<DeathRaceTheme>(spec.AssetPath);
            var content = DeathRaceAssets.Load<DeathRaceContent>(DeathRaceContentBuilder.ContentPath);
            BiomeLook look = theme.Biome(layout.Biome);
            Camera camera = Stage(theme, look);
            var root = new GameObject("Preview").transform;
            Track track = TrackBuilder.Build(layout, theme, content, root);

            // The four cars on the grid, two more racing through the sharpest corner.
            float corner = SharpestCorner(track);
            var models = new[] { CarModel.Viper, CarModel.Bulldog, CarModel.Hornet, CarModel.Reaper, CarModel.Tyrant, CarModel.Rustbucket };
            for (int i = 0; i < 4; i++)
            {
                Pose slot = track.GridSlot(i);
                PlaceCar(theme, root, models[i], slot.position, slot.rotation, i * 4);
            }
            for (int i = 0; i < 2; i++)
            {
                float d = corner - 6f + i * 9f;
                Vector2 point = track.Path.PointAt(d) + track.Path.NormalAt(d) * (i == 0 ? 2.5f : -2f);
                Vector2 tangent = track.Path.TangentAt(d);
                PlaceCar(theme, root, models[4 + i], RacePathToWorld(point), Quaternion.LookRotation(new Vector3(tangent.x, 0f, tangent.y)), 9 + i * 5);
            }

            var sheet = new Texture2D(ShotWidth * 2, ShotHeight * 2, TextureFormat.RGBA32, false);
            Overview(camera, sheet, 0, 1, track.Bounds);
            Vector2 grid = track.Path.PointAt(track.LineDistance - 10f);
            Shot(camera, sheet, 1, 1, RacePathToWorld(grid), 38f, 60f, 40f);
            Vector2 bend = track.Path.PointAt(corner);
            Shot(camera, sheet, 0, 0, RacePathToWorld(bend), 38f, 60f, 40f);
            Vector2 side = track.Path.PointAt(corner) + track.Path.NormalAt(corner) * (track.HalfWidth + 12f);
            Shot(camera, sheet, 1, 0, RacePathToWorld(side), 16f, 40f, 45f);
            File.WriteAllBytes(file, sheet.EncodeToPNG());
            Object.DestroyImmediate(sheet);
            Debug.Log($"Art preview: {file}");
        }

        private static Vector3 RacePathToWorld(Vector2 point)
        {
            return new Vector3(point.x, 0f, point.y);
        }

        /// <summary>Where the track bends hardest (the most curvature along its centre line).</summary>
        private static float SharpestCorner(Track track)
        {
            float best = 0f;
            float bestCurve = 0f;
            for (float d = 0f; d < track.Path.Length; d += 2f)
            {
                float curve = Mathf.Abs(track.Path.CurvatureAt(d));
                if (curve > bestCurve)
                {
                    bestCurve = curve;
                    best = d;
                }
            }
            return best;
        }

        /// <summary>The whole track from straight above (orthographic, no fog: from up there it would grey the picture).</summary>
        private static void Overview(Camera camera, Texture2D sheet, int column, int row, Bounds bounds)
        {
            bool fog = RenderSettings.fog;
            RenderSettings.fog = false;
            camera.orthographic = true;
            float aspect = ShotWidth / (float)ShotHeight;
            camera.orthographicSize = Mathf.Max(bounds.extents.z, bounds.extents.x / aspect) * 1.12f;
            camera.transform.SetPositionAndRotation(new Vector3(bounds.center.x, 300f, bounds.center.z), Quaternion.Euler(90f, 0f, 0f));
            Color[] pixels = Render(camera, ShotWidth, ShotHeight);
            sheet.SetPixels(column * ShotWidth, row * ShotHeight, ShotWidth, ShotHeight, pixels);
            camera.orthographic = false;
            RenderSettings.fog = fog;
        }

        private static void PlaceCar(DeathRaceTheme theme, Transform root, CarModel model, Vector3 position, Quaternion rotation, int colour)
        {
            GameObject prefab = DeathRacePrefabBuilder.CarPrefab(model);
            if (prefab == null)
            {
                return;
            }
            var car = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
            car.transform.SetPositionAndRotation(position, rotation);
            var view = car.GetComponent<CarModelView>();
            if (view != null)
            {
                view.ApplyTheme(theme, theme.RivalColor(colour));
            }
        }

        // ------------------------------------------------------------------------------------------------
        // The interface
        // ------------------------------------------------------------------------------------------------

        public static void RenderInterface()
        {
            string folder = OutputFolder();
            foreach (DeathRaceThemeSpec spec in DeathRaceThemeSpec.All)
            {
                if (!Wanted(spec.Name, null))
                {
                    continue;
                }
                RenderSprites(spec, Path.Combine(folder, $"{spec.Name}-Interface.png"));
            }
        }

        private static void RenderSprites(DeathRaceThemeSpec spec, string file)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = spec.Night ? DeathRaceThemeSpec.Hex(0x1A2238) : DeathRaceThemeSpec.Hex(0x8A7458);
            camera.orthographic = true;
            camera.enabled = false;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;

            var canvasObject = new GameObject("Canvas", typeof(RectTransform));
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 5f;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.referencePixelsPerUnit = 100f;
            var rootRect = (RectTransform)canvasObject.transform;

            Image backdrop = AddImage(rootRect, DeathRaceArtBuilder.UI(spec, "Backdrop"), new Vector2(960f, 540f), new Vector2(1920f, 1080f), Image.Type.Simple);
            backdrop.color = Color.white;

            // The menu window with its banner and buttons in their four states.
            AddImage(rootRect, DeathRaceArtBuilder.UI(spec, "Window"), new Vector2(330f, 520f), new Vector2(520f, 780f), Image.Type.Sliced);
            AddImage(rootRect, DeathRaceArtBuilder.UI(spec, "Banner"), new Vector2(330f, 900f), new Vector2(420f, 80f), Image.Type.Sliced);
            AddText(rootRect, "DEATH RACE", new Vector2(330f, 902f), 46f, spec.Night ? spec.Ink : spec.Second, DeathRaceArtBuilder.TitleFont, DeathRaceArtBuilder.TitleMaterial(spec));
            string[] states = { "Button", "ButtonHover", "ButtonPressed", "ButtonDisabled", "Button", "Button" };
            string[] labels = { "Career", "Quick Race", "Split Screen", "Play Online", "Settings", "Exit" };
            for (int i = 0; i < states.Length; i++)
            {
                var at = new Vector2(330f, 780f - i * 72f);
                AddImage(rootRect, DeathRaceArtBuilder.UI(spec, states[i]), at, new Vector2(420f, 60f), Image.Type.Sliced);
                AddText(rootRect, labels[i], at, 26f, i == 3 ? DeathRaceArtInterface.WithAlpha(spec.ButtonInk, 0.5f) : spec.ButtonInk, DeathRaceArtBuilder.BoldFont, null);
            }

            // A settings row with a slider, a toggle and a choice with arrows; a field.
            AddImage(rootRect, DeathRaceArtBuilder.UI(spec, "Panel"), new Vector2(1060f, 760f), new Vector2(760f, 330f), Image.Type.Sliced);
            for (int i = 0; i < 3; i++)
            {
                var at = new Vector2(1060f, 860f - i * 70f);
                AddImage(rootRect, DeathRaceArtBuilder.UI(spec, "Row"), at, new Vector2(700f, 56f), Image.Type.Sliced);
            }
            AddText(rootRect, "Sound volume", new Vector2(860f, 860f), 22f, spec.Ink, DeathRaceArtBuilder.BoldFont, null);
            AddImage(rootRect, DeathRaceArtBuilder.UI(spec, "Track"), new Vector2(1180f, 860f), new Vector2(300f, 18f), Image.Type.Sliced);
            Image fill = AddImage(rootRect, DeathRaceArtBuilder.UI(spec, "Fill"), new Vector2(1120f, 860f), new Vector2(180f, 18f), Image.Type.Sliced);
            fill.color = spec.Accent;
            AddImage(rootRect, DeathRaceArtBuilder.UI(spec, "Knob"), new Vector2(1210f, 860f), new Vector2(34f, 34f), Image.Type.Simple);
            AddText(rootRect, "80%", new Vector2(1370f, 860f), 22f, spec.Night ? spec.Accent : spec.Second, DeathRaceArtBuilder.BoldFont, null);
            AddText(rootRect, "Screen shake", new Vector2(860f, 790f), 22f, spec.Ink, DeathRaceArtBuilder.BoldFont, null);
            AddImage(rootRect, DeathRaceArtBuilder.UI(spec, "CheckBox"), new Vector2(1180f, 790f), new Vector2(36f, 36f), Image.Type.Simple);
            AddImage(rootRect, DeathRaceArtBuilder.UI(spec, "CheckMark"), new Vector2(1180f, 790f), new Vector2(36f, 36f), Image.Type.Simple);
            AddText(rootRect, "Look", new Vector2(860f, 720f), 22f, spec.Ink, DeathRaceArtBuilder.BoldFont, null);
            AddImage(rootRect, DeathRaceArtBuilder.UI(spec, "Arrow"), new Vector2(1080f, 720f), new Vector2(44f, 44f), Image.Type.Sliced);
            AddText(rootRect, spec.Name, new Vector2(1200f, 720f), 22f, spec.Night ? spec.Accent : spec.Second, DeathRaceArtBuilder.BoldFont, null);
            AddImage(rootRect, DeathRaceArtBuilder.UI(spec, "Arrow"), new Vector2(1320f, 720f), new Vector2(44f, 44f), Image.Type.Sliced);
            AddImage(rootRect, DeathRaceArtBuilder.UI(spec, "InputBox"), new Vector2(1060f, 640f), new Vector2(420f, 52f), Image.Type.Sliced);
            AddText(rootRect, "Max Payne", new Vector2(1060f, 640f), 22f, spec.Ink, DeathRaceArtBuilder.BodyFont, null);

            // The HUD: a panel with bars and counters.
            AddImage(rootRect, DeathRaceArtBuilder.UI(spec, "Panel"), new Vector2(1060f, 470f), new Vector2(760f, 200f), Image.Type.Sliced);
            AddImage(rootRect, DeathRaceArtBuilder.UI(spec, "BarFrame"), new Vector2(960f, 520f), new Vector2(360f, 40f), Image.Type.Sliced);
            Image bar = AddImage(rootRect, DeathRaceArtBuilder.UI(spec, "BarFill"), new Vector2(920f, 520f), new Vector2(280f, 40f), Image.Type.Sliced);
            bar.color = DeathRaceThemeSpec.Hex(0x5CCB2D);
            AddImage(rootRect, DeathRaceArtBuilder.UI(spec, "BarFrame"), new Vector2(960f, 470f), new Vector2(360f, 40f), Image.Type.Sliced);
            Image nitro = AddImage(rootRect, DeathRaceArtBuilder.UI(spec, "BarFill"), new Vector2(880f, 470f), new Vector2(200f, 40f), Image.Type.Sliced);
            nitro.color = DeathRaceThemeSpec.Hex(0x2F9BFF);
            AddText(rootRect, "2nd / 4", new Vector2(1280f, 520f), 40f, spec.HudText, DeathRaceArtBuilder.TitleFont, DeathRaceArtBuilder.TitleMaterial(spec));
            AddText(rootRect, "LAP 2/4", new Vector2(1280f, 460f), 28f, spec.HudAccent, DeathRaceArtBuilder.BoldFont, null);

            // The icons and the emblem.
            for (int i = 0; i < DeathRaceArtIcons.Names.Length; i++)
            {
                var at = new Vector2(740f + (i % 10) * 76f, 290f - (i / 10) * 80f);
                AddImage(rootRect, DeathRaceArtBuilder.Icon(spec, DeathRaceArtIcons.Names[i]), at, new Vector2(64f, 64f), Image.Type.Simple);
            }
            AddImage(rootRect, DeathRaceArtBuilder.Icon("GameIcon"), new Vector2(1640f, 230f), new Vector2(240f, 240f), Image.Type.Simple);
            AddImage(rootRect, DeathRaceArtBuilder.UI(spec, "Preview"), new Vector2(1640f, 520f), new Vector2(200f, 200f), Image.Type.Simple);
            for (int i = 0; i < DeathRaceArtBuilder.UpgradeIcons.Length; i++)
            {
                AddImage(rootRect, DeathRaceArtBuilder.UpgradePicture(spec, DeathRaceArtBuilder.UpgradeIcons[i]), new Vector2(1560f + i * 120f, 860f), new Vector2(110f, 110f), Image.Type.Simple);
            }

            Canvas.ForceUpdateCanvases();
            Color[] pixels = Render(camera, 1920, 1080);
            var sheet = new Texture2D(1920, 1080, TextureFormat.RGBA32, false);
            sheet.SetPixels(pixels);
            File.WriteAllBytes(file, sheet.EncodeToPNG());
            Object.DestroyImmediate(sheet);
            Debug.Log($"Art preview: {file}");
        }

        private static Image AddImage(RectTransform parent, Sprite sprite, Vector2 center, Vector2 size, Image.Type type)
        {
            var go = new GameObject(sprite != null ? sprite.name : "Missing", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = center;
            rect.sizeDelta = size;
            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.type = type;
            image.color = sprite != null ? Color.white : Color.magenta;
            return image;
        }

        private static void AddText(RectTransform parent, string text, Vector2 center, float size, Color color, TMP_FontAsset font, Material material)
        {
            var go = new GameObject(text, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = center;
            rect.sizeDelta = new Vector2(520f, 80f);
            var label = go.AddComponent<TextMeshProUGUI>();
            label.font = font;
            if (material != null)
            {
                label.fontSharedMaterial = material;
            }
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
        }
    }
}
