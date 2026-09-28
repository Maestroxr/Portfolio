using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Portfolio.Asteroids.EditorTools
{
    /// <summary>
    /// The ground prefabs of the strike mode: one prefab per ground unit (Prefabs/Strike/Ground/{Unit}), the terrain tiles
    /// of every theme (Prefabs/Strike/Terrain/{Theme}/{Kind}{Variant}), the crater and rubble decals
    /// (Prefabs/Strike/Decals), a few standalone props (Prefabs/Strike/Props) and the six StrikeTheme assets
    /// (Config/Strike/Themes). Also renders contact sheets of them for checking the art in batch.
    /// </summary>
    internal static partial class AsteroidsPrefabBuilder
    {
        /// <summary>Where a theme asset lives (content-air's levels load them by this path).</summary>
        public static string ThemePath(string theme)
        {
            return $"Config/Strike/Themes/{theme}.asset";
        }


        static partial void BuildGroundPrefabs()
        {
            foreach (StrikeUnit unit in AsteroidsArtBuilder.GroundUnits)
            {
                BuildGroundUnit(unit);
            }
            var themes = new List<GroundTheme>(AsteroidsArtBuilder.GroundThemes);
            themes.AddRange(AsteroidsArtBuilder.GeneratedGroundThemes);
            foreach (GroundTheme theme in themes)
            {
                var sets = new List<StrikeTheme.TileSet>();
                foreach ((TerrainKind kind, int variants) in theme.Kinds)
                {
                    var tiles = new TerrainTile[variants];
                    for (int v = 0; v < variants; v++)
                    {
                        tiles[v] = BuildTile(AsteroidsArtBuilder.Plan(theme, kind, v));
                    }
                    sets.Add(new StrikeTheme.TileSet { kind = kind, variants = tiles });
                }
                BuildThemeAsset(theme, sets.ToArray());
            }
            BuildDecals();
            BuildProps();
        }

        // ------------------------------------------------------------------ terrain

        private static TerrainTile BuildTile(TilePlan plan)
        {
            var root = new GameObject($"{plan.Theme.Name}{plan.Name}");
            var tile = root.AddComponent<TerrainTile>();
            tile.kind = plan.Kind;
            tile.variant = plan.Variant;
            tile.length = StrikeRules.TileLength;
            tile.width = StrikeRules.TileWidth;
            Model(root.transform, "Ground", AsteroidsAssets.Load<Mesh>("Art/Ground/Models/GroundTile.asset"), AsteroidsArtBuilder.TileMaterial(plan));
            // A neon world is a bare grid: no props, no night lights.
            Mesh props = plan.Theme.Neon == null ? AsteroidsArtBuilder.TileProps(plan) : null;
            if (props != null)
            {
                Model(root.transform, "Props", props, GroundPalette);
            }
            tile.decalRoot = Child(root.transform, "Decals", new Vector3(0f, 0f, -0.02f));
            if (plan.Lights.Count > 0 && plan.Theme.Neon == null)
            {
                Transform lights = Child(root.transform, "Lights");
                for (int i = 0; i < plan.Lights.Count; i++)
                {
                    (Vector3 position, float size, Color color) = plan.Lights[i];
                    Quad(lights, $"Light{i}", LightMaterial(color), position, Vector2.one * size);
                }
            }
            GameObject prefab = Save(root, $"{plan.Theme.PrefabFolder}/{plan.Name}");
            return prefab.GetComponent<TerrainTile>();
        }


        private static Material GroundPalette => AsteroidsAssets.Load<Material>("Art/Ground/Materials/GroundPalette.mat");


        /// <summary>An additive glow for night lights, one material per colour.</summary>
        private static Material LightMaterial(Color color)
        {
            string name = ColorUtility.ToHtmlStringRGBA(color);
            return AsteroidsAssets.SaveMaterial($"Art/Ground/Materials/Light{name}.mat", AsteroidsAssets.Load<Shader>("Art/Shaders/Glow.shader"), m =>
            {
                m.SetTexture("_BaseMap", AsteroidsArtBuilder.Texture("Glow"));
                m.SetColor("_BaseColor", color);
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)BlendMode.One);
                m.SetFloat("_Cull", (float)CullMode.Off);
                m.renderQueue = (int)RenderQueue.Transparent;
            });
        }


        private static void BuildThemeAsset(GroundTheme theme, StrikeTheme.TileSet[] sets)
        {
            AsteroidsAssets.SaveScriptable<StrikeTheme>(theme.AssetPath, asset =>
            {
                asset.title = theme.Name.ToUpperInvariant();
                asset.accent = theme.Accent;
                asset.tiles = sets;
                asset.sunColor = theme.Sun;
                asset.sunIntensity = theme.SunIntensity;
                asset.sunAngles = AsteroidsArtBuilder.StrikeSunAngles;
                asset.ambientColor = theme.Ambient;
                asset.shadowColor = theme.Shadow;
                asset.craterMaterial = AsteroidsArtBuilder.CraterMaterial(theme);
                asset.night = theme.Night;
            });
        }


        private static void BuildDecals()
        {
            Material crater = AsteroidsArtBuilder.CraterMaterial(AsteroidsArtBuilder.GroundThemes[0]);
            var root = new GameObject("Crater");
            Quad(root.transform, "Quad", crater, Vector3.zero, Vector2.one);
            Save(root, "Strike/Decals/Crater");
            var rubble = new GameObject("Rubble");
            Quad(rubble.transform, "Quad", AsteroidsAssets.Load<Material>($"{AsteroidsArtBuilder.GroundThemes[0].Folder}/Rubble.mat"), Vector3.zero, Vector2.one);
            Save(rubble, "Strike/Decals/Rubble");
        }


        /// <summary>Standalone props for set pieces and bosses (the tiles bake theirs into one mesh).</summary>
        private static void BuildProps()
        {
            var props = new (string name, PropKind kind, Vector3 size, GroundSwatch main, GroundSwatch accent)[]
            {
                ("Building", PropKind.Building, new Vector3(4f, 0.6f, 3f), GroundSwatch.Tan, GroundSwatch.ConcreteDark),
                ("Hangar", PropKind.Hangar, new Vector3(5.4f, 0.85f, 8f), GroundSwatch.Steel, GroundSwatch.SteelDark),
                ("StorageTank", PropKind.Tank, new Vector3(2.4f, 0.8f, 2.4f), GroundSwatch.ConcreteLight, GroundSwatch.EnemyRed),
                ("Dome", PropKind.Dome, new Vector3(5.5f, 0.9f, 5.5f), GroundSwatch.White, GroundSwatch.Plate),
                ("CoolingTower", PropKind.Tower2, new Vector3(5f, 0.9f, 5f), GroundSwatch.ConcreteDark, GroundSwatch.Rust),
                ("Palm", PropKind.Palm, new Vector3(2.6f, 0.85f, 2.6f), GroundSwatch.Palm, GroundSwatch.Leaf),
                ("Tree", PropKind.Tree, new Vector3(3.2f, 0.88f, 3.2f), GroundSwatch.Leaf, GroundSwatch.LeafLight),
                ("Boulders", PropKind.Boulders, new Vector3(2f, 0.6f, 2f), GroundSwatch.Stone, GroundSwatch.StoneDark),
                ("Pipe", PropKind.Pipe, new Vector3(1f, 0.45f, 8f), GroundSwatch.Pipe, GroundSwatch.PipeDark),
                ("Sandbags", PropKind.Sandbags, new Vector3(3f, 0.35f, 0.5f), GroundSwatch.Khaki, GroundSwatch.Khaki)
            };
            foreach ((string name, PropKind kind, Vector3 size, GroundSwatch main, GroundSwatch accent) in props)
            {
                var builder = new MeshBuilder();
                SpaceModels.Prop(builder, new PropSpot { Kind = kind, Size = size, Main = main, Accent = accent, Seed = name.Length * 97 });
                Mesh mesh = AsteroidsAssets.SaveMesh(builder, $"Art/Ground/Models/Props/{name}.asset");
                var root = new GameObject(name);
                Transform depth = Child(root.transform, "Depth", new Vector3(0f, 0f, StrikeRules.GroundDepth));
                depth.gameObject.AddComponent<DepthAnchor>().depth = StrikeRules.GroundDepth;
                Model(depth, "Visual", mesh, GroundPalette);
                Save(root, $"Strike/Props/{name}");
            }
        }

        // ------------------------------------------------------------------ ground units

        private static void BuildGroundUnit(StrikeUnit unit)
        {
            UnitModel model = SpaceModels.GroundUnit(unit);
            StrikeUnitInfo info = StrikeUnitRules.Info(unit);
            EnemyShotInfo shot = StrikeUnitRules.Shot(info.ShotKind);
            var root = new GameObject(unit.ToString());
            var body = root.AddComponent<GroundUnit>();
            float depthValue = StrikeRules.GroundDepth - model.Height * 0.5f;
            Transform depth = Child(root.transform, "Depth", new Vector3(0f, 0f, depthValue));
            depth.gameObject.AddComponent<DepthAnchor>().depth = depthValue;

            // Centred: the unit turns with its heading, and an offset shadow would turn with it.
            Quad(depth, "Shadow", AsteroidsAssets.Load<Material>("Art/Ground/Materials/UnitShadow.mat"),
                new Vector3(0f, 0f, model.Height * 0.5f - 0.02f), Vector2.one * model.ShadowRadius * 2.5f);

            MeshRenderer hull = Model(depth, "Visual", AsteroidsArtBuilder.GroundUnitMesh(unit), GroundPalette);
            Transform visual = hull.transform;
            var renderers = new List<Renderer> { hull };
            var muzzles = new List<Transform>();
            Transform turret = null;
            if (model.Turret != null)
            {
                MeshRenderer part = Model(visual, "Turret", AsteroidsArtBuilder.GroundUnitMesh(unit, "Turret"), GroundPalette, model.ToVisual(model.TurretPivot));
                turret = part.transform;
                renderers.Add(part);
                for (int i = 0; i < model.Muzzles.Count; i++)
                {
                    muzzles.Add(Child(turret, $"Muzzle{i}", UnitModel.FaceCamera * model.Muzzles[i]));
                }
            }
            else
            {
                for (int i = 0; i < model.Muzzles.Count; i++)
                {
                    muzzles.Add(Child(visual, $"Muzzle{i}", model.ToVisual(model.Muzzles[i])));
                }
            }
            Transform spinner = null;
            if (model.Spinner != null)
            {
                MeshRenderer part = Model(visual, "Spinner", AsteroidsArtBuilder.GroundUnitMesh(unit, "Spinner"), GroundPalette, model.ToVisual(model.SpinnerPivot));
                spinner = part.transform;
                renderers.Add(part);
            }
            GameObject telegraph = null;
            if (model.Glow.HasValue)
            {
                telegraph = Quad(visual, "Telegraph", AsteroidsAssets.Load<Material>("Art/Ground/Materials/Telegraph.mat"),
                    model.ToVisual(model.Glow.Value) + new Vector3(0f, 0f, -0.05f), Vector2.one * 2.2f).gameObject;
                telegraph.SetActive(false);
            }

            body.unit = unit;
            body.altitude = Altitude.Ground;
            body.radius = info.Radius;
            body.wraps = false;
            body.pulledByGravity = false;
            body.visual = visual;
            body.maxHealth = info.Health;
            body.score = info.Bounty;
            body.contactDamage = 0f;
            body.flashRenderers = renderers.ToArray();
            body.flashColor = new Color(1f, 0.9f, 0.75f);
            body.turret = turret;
            body.turretTurnRate = unit == StrikeUnit.Tank ? 90f : 140f;
            body.muzzles = muzzles.ToArray();
            body.pattern = info.Pattern;
            body.burst = Mathf.Max(1, info.Burst);
            body.burstGap = info.BurstGap;
            body.interval = info.Interval;
            body.fireInterval = info.Interval;
            body.shotKind = info.ShotKind;
            body.shotSpeed = shot.Speed;
            body.aimError = 3f;
            body.explodes = info.Explodes;
            body.blastRadius = info.BlastRadius;
            body.blastDamage = info.BlastDamage;
            body.craterSize = info.CraterSize;
            body.spinner = spinner;
            body.spinSpeed = 120f;
            body.telegraph = telegraph;
            body.explosionTint = info.Explodes ? new Color(1f, 0.6f, 0.25f) : new Color(1f, 0.7f, 0.4f);
            body.explosionScale = info.ExplosionScale;
            Save(root, $"Strike/Ground/{unit}");
        }

        // ------------------------------------------------------------------ contact sheets

        /// <summary>
        /// Batch entry: builds the ground art and prefabs, then renders contact sheets into the folder after
        /// "-groundSheets" (default: Temp/GroundSheets under the project). Exits 1 on an exception.
        /// </summary>
        public static void GroundSheetsBatch()
        {
            try
            {
                AsteroidsArtBuilder.BuildGroundArtOnly();
                BuildGroundPrefabs();
                AssetDatabase.SaveAssets();
                RenderGroundSheets(SheetFolder());
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }


        /// <summary>Batch entry: only renders the contact sheets of what is built.</summary>
        public static void GroundSheetsOnlyBatch()
        {
            try
            {
                RenderGroundSheets(SheetFolder());
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }


        private static string SheetFolder()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-groundSheets")
                {
                    return args[i + 1];
                }
            }
            return Path.Combine(Directory.GetCurrentDirectory(), "Temp/GroundSheets");
        }


        /// <summary>
        /// Renders, like the game's camera (perspective, FOV 40, the playfield at 27.5 m, the ground 1.2 m behind it): per
        /// theme a sheet of every tile, a seam check of tile pairs and a frame with the units; plus one full-resolution
        /// frame per theme for judging the crispness at 54 px per metre.
        /// </summary>
        public static void RenderGroundSheets(string folder)
        {
            Directory.CreateDirectory(folder);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            float distance = 10f / Mathf.Tan(20f * Mathf.Deg2Rad);
            DepthLayer.Distance = distance;
            var cameraObject = new GameObject("SheetCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 0f, -distance);
            camera.fieldOfView = 40f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 200f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.allowHDR = true;
            UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.renderShadows = false;
            var volume = new GameObject("Volume").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = AsteroidsContentBuilder.VolumeProfile;
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.None;
            sun.gameObject.AddComponent<UniversalAdditionalLightData>();
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.skybox = null;
            RenderSettings.fog = false;

            var full = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32) { antiAliasing = 1 };
            foreach (GroundTheme theme in AsteroidsArtBuilder.GroundThemes)
            {
                ApplyLighting(sun, theme);
                var theAsset = AsteroidsAssets.Load<StrikeTheme>(ThemePath(theme.Name));
                var plans = new List<TilePlan>();
                foreach ((TerrainKind kind, int variants) in theme.Kinds)
                {
                    for (int v = 0; v < variants; v++)
                    {
                        plans.Add(AsteroidsArtBuilder.Plan(theme, kind, v));
                    }
                }
                const int columns = 3;
                const int cellW = 640;
                const int cellH = 360;
                int rows = (plans.Count + columns - 1) / columns;
                var sheet = new Texture2D(columns * cellW, rows * cellH, TextureFormat.RGB24, false);
                sheet.SetPixels(new Color[sheet.width * sheet.height]);
                for (int i = 0; i < plans.Count; i++)
                {
                    TilePlan plan = plans[i];
                    var shown = new List<GameObject> { PlaceTile(theAsset.Tile(plan.Kind, plan.Variant), 0f) };
                    Texture2D frame = Capture(camera, full);
                    Blit(frame, sheet, (i % columns) * cellW, (rows - 1 - i / columns) * cellH, cellW, cellH);
                    Object.DestroyImmediate(frame);
                    foreach (GameObject go in shown)
                    {
                        Object.DestroyImmediate(go);
                    }
                }
                File.WriteAllBytes(Path.Combine(folder, $"tiles_{theme.Name}.png"), sheet.EncodeToPNG());
                Object.DestroyImmediate(sheet);

                // A strip: the seam between the first kind and every other kind, plus units and an aircraft for the layers.
                var strip = new List<GameObject>();
                TerrainKind first = theme.Kinds[0].kind;
                strip.Add(PlaceTile(theAsset.Tile(first, 0), -10f));
                TerrainKind second = theme.Kinds.Length > 3 ? theme.Kinds[3].kind : theme.Kinds[1].kind;
                strip.Add(PlaceTile(theAsset.Tile(second, 0), 10f));
                PlaceUnits(strip, theme);
                Texture2D mock = Capture(camera, full);
                File.WriteAllBytes(Path.Combine(folder, $"frame_{theme.Name}.png"), mock.EncodeToPNG());
                Object.DestroyImmediate(mock);
                foreach (GameObject go in strip)
                {
                    Object.DestroyImmediate(go);
                }
            }
            full.Release();
            Debug.Log($"[GroundSheets] written to {folder}");
        }


        private static void ApplyLighting(Light sun, GroundTheme theme)
        {
            sun.color = theme.Sun;
            sun.intensity = theme.SunIntensity;
            sun.transform.rotation = Quaternion.Euler(AsteroidsArtBuilder.StrikeSunAngles);
            RenderSettings.ambientLight = theme.Ambient;
            DynamicGI.UpdateEnvironment();
        }


        /// <summary>Instantiates a tile with its centre at logical y (depth-placed like the terrain does).</summary>
        private static GameObject PlaceTile(TerrainTile prefab, float y)
        {
            var tile = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject);
            DepthLayer.Place(tile.transform, new Vector2(0f, y), StrikeRules.GroundDepth);
            return tile;
        }


        /// <summary>Every ground unit in two rows, a few turned, turrets aimed at a ship; the ship and a saucer fly above.</summary>
        private static void PlaceUnits(List<GameObject> shown, GroundTheme theme)
        {
            var ship = new Vector2(2f, -7f);
            StrikeUnit[] units = AsteroidsArtBuilder.GroundUnits;
            for (int i = 0; i < units.Length; i++)
            {
                var prefab = AsteroidsAssets.Load<GameObject>($"Prefabs/Strike/Ground/{units[i]}.prefab");
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                var at = new Vector2(-14f + (i % 6) * 5.6f, i < 6 ? 5.5f : 0.5f);
                go.transform.position = new Vector3(at.x, at.y, 0f);
                if (units[i] == StrikeUnit.Truck || units[i] == StrikeUnit.Gunboat)
                {
                    go.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                }
                var unit = go.GetComponent<GroundUnit>();
                if (unit.turret != null)
                {
                    Vector2 aim = ship - at;
                    float angle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg - 90f;
                    unit.turret.rotation = Quaternion.Euler(0f, 0f, angle);
                }
                if (unit.telegraph != null)
                {
                    unit.telegraph.SetActive(true);
                }
                foreach (DepthAnchor anchor in go.GetComponentsInChildren<DepthAnchor>())
                {
                    anchor.Place();
                }
                shown.Add(go);
            }
            var saucer = AsteroidsAssets.Load<GameObject>("Prefabs/Enemies/Saucer.prefab");
            if (saucer != null)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(saucer);
                go.transform.position = new Vector3(-9f, -4f, 0f);
                shown.Add(go);
            }
            Mesh hull = AsteroidsArtBuilder.ShipMesh("StarSparrow2");
            if (hull != null)
            {
                var root = new GameObject("Ship");
                root.transform.position = new Vector3(ship.x, ship.y, 0f);
                float scale = 1.95f / Mathf.Max(0.01f, hull.bounds.size.z);
                Model(root.transform, "Model", hull, AsteroidsArtBuilder.PackMaterial("StarSparrow Blue"), -(FaceCamera * hull.bounds.center) * scale, FaceCamera,
                    Vector3.one * scale);
                shown.Add(root);
                var shadow = new GameObject("ShipShadow");
                Vector2 offset = StrikeRules.ShadowOffset;
                Model(shadow.transform, "Model", hull, AsteroidsAssets.Load<Material>("Art/Ground/Materials/UnitShadow.mat"), -(FaceCamera * hull.bounds.center) * scale,
                    FaceCamera, new Vector3(scale, scale, scale * 0.05f));
                DepthLayer.Place(shadow.transform, ship + offset, StrikeRules.GroundDepth - 0.03f);
                shown.Add(shadow);
            }
        }


        private static Texture2D Capture(Camera camera, RenderTexture target)
        {
            camera.targetTexture = target;
            camera.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            var texture = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;
            camera.targetTexture = null;
            return texture;
        }


        /// <summary>Copies <paramref name="source"/> scaled (point sampled, 3 x 3 box filter) into a cell of <paramref name="sheet"/>.</summary>
        private static void Blit(Texture2D source, Texture2D sheet, int x0, int y0, int width, int height)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float u = (x + 0.5f) / width;
                    float v = (y + 0.5f) / height;
                    sheet.SetPixel(x0 + x, y0 + y, source.GetPixelBilinear(u, v));
                }
            }
        }
    }
}
