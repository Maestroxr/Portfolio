using System;
using System.Collections.Generic;
using System.Linq;
using Gamebox.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Portfolio.Heroes.EditorTools
{
    /// <summary>
    /// Turns the downloaded models, textures, fonts, icons and sounds into what the game shows: import settings,
    /// materials, terrain layers, the prefabs of the creatures, heroes, map objects and towns, and the
    /// <see cref="HeroesArt"/> asset that ties them together. Run it from the Heroes menu, or in batch with
    /// -executeMethod Portfolio.Heroes.EditorTools.HeroesArtBuilder.Build.
    /// </summary>
    internal static class HeroesArtBuilder
    {
        private const string Models = "Art/Models";
        private const string Generated = "Art/Generated";

        /// <summary>Meters a creature of each tier stands tall. A hexagon is 2.08 m across, so they fit in one.</summary>
        private static readonly float[] TierHeight = { 1.35f, 1.5f, 1.6f, 1.6f, 1.75f, 1.9f, 2.6f };

        [MenuItem("Heroes/Build Art", false, 20)]
        public static void Build()
        {
            var log = new System.Diagnostics.Stopwatch();
            log.Start();
            try
            {
                AssetDatabase.StartAssetEditing();
                Importers();
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }

            HeroesArt art = HeroesAssets.Load<HeroesArt>("Art/HeroesArt.asset");
            if (art == null)
            {
                art = ScriptableObject.CreateInstance<HeroesArt>();
                HeroesAssets.EnsureFolderOf("Art/HeroesArt.asset");
                AssetDatabase.CreateAsset(art, HeroesAssets.Path("Art/HeroesArt.asset"));
            }

            Staged(() =>
            {
                Materials(art);
                TerrainLayers(art);
                Units(art);
                Heroes(art);
                Scatter(art);
                HeroesObjectArt.Build(art);
                HeroesBattleArt.Build(art);
                HeroesPortraits.Build(art);
                HeroesUIArt.BuildAll(art);
                Audio(art);
            });

            EditorUtility.SetDirty(art);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Heroes: art built in {log.ElapsedMilliseconds} ms ({art.units.Count} creatures, " +
                      $"{art.objects.Count} map objects, {art.towns.Count} towns, {art.icons.Count} icons).");
        }

        /// <summary>
        /// Runs <paramref name="build"/> with a scene of its own made active, thrown away afterwards: the prefabs are put
        /// together there, so whatever scene is open is not marked as changed by the objects coming and going. While a
        /// scene that was never saved is open (as in batch mode, or after File > New Scene) Unity refuses to add another
        /// untitled one next to it, and there is nothing in it to keep clean, so the work is done in the open scene.
        /// </summary>
        public static void Staged(Action build)
        {
            if (UntitledOpen())
            {
                build();
                return;
            }
            Scene open = SceneManager.GetActiveScene();
            Scene stage = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(stage);
            try
            {
                build();
            }
            finally
            {
                if (open.IsValid())
                {
                    SceneManager.SetActiveScene(open);
                }
                EditorSceneManager.CloseScene(stage, true);
            }
        }

        /// <summary>Whether a scene that has never been saved is loaded.</summary>
        public static bool UntitledOpen()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path))
                {
                    return true;
                }
            }
            return false;
        }

        // ------------------------------------------------------------------ import settings

        /// <summary>Every model imports the same way: legacy clips the puppets play by name, and materials of its own.</summary>
        private static void Importers()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { HeroesAssets.Path(Models) }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null)
                {
                    continue;
                }
                string before = EditorJsonUtility.ToJson(importer);
                importer.globalScale = 1f;
                importer.useFileScale = true;
                importer.importVisibility = false;
                importer.importCameras = false;
                importer.importLights = false;
                importer.importBlendShapes = false;
                importer.importConstraints = false;
                importer.weldVertices = true;
                importer.meshCompression = ModelImporterMeshCompression.Medium;
                importer.isReadable = false;
                // Materials come from what the file describes and stay inside it, so the packs keep their own look.
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
                bool animated = HasClips(path);
                importer.importAnimation = animated;
                importer.animationType = animated ? ModelImporterAnimationType.Legacy : ModelImporterAnimationType.None;
                if (animated)
                {
                    importer.animationCompression = ModelImporterAnimationCompression.KeyframeReduction;
                    importer.animationWrapMode = WrapMode.Loop;
                    importer.resampleCurves = true;
                }
                if (EditorJsonUtility.ToJson(importer) != before)
                {
                    importer.SaveAndReimport();
                }
            }
        }

        private static bool HasClips(string path)
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview", StringComparison.Ordinal))
                {
                    return true;
                }
            }
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            return importer != null && importer.importedTakeInfos.Length > 0;
        }

        // ------------------------------------------------------------------ models and prefabs

        public static GameObject Model(string relative)
        {
            var model = HeroesAssets.Load<GameObject>($"{Models}/{relative}.fbx");
            if (model == null)
            {
                Debug.LogWarning($"Heroes: the model {relative} is missing.");
            }
            return model;
        }

        /// <summary>The bounds of a model in its own space, from its renderers.</summary>
        public static Bounds BoundsOf(GameObject model)
        {
            var bounds = new Bounds();
            bool first = true;
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                Bounds local = renderer is SkinnedMeshRenderer skinned && skinned.sharedMesh != null
                    ? skinned.sharedMesh.bounds
                    : renderer is MeshRenderer && renderer.TryGetComponent(out MeshFilter filter) && filter.sharedMesh != null
                        ? filter.sharedMesh.bounds
                        : renderer.bounds;
                Transform t = renderer.transform;
                var world = new Bounds(t.TransformPoint(local.center), Vector3.zero);
                foreach (Vector3 corner in Corners(local))
                {
                    world.Encapsulate(t.TransformPoint(corner));
                }
                if (first)
                {
                    bounds = world;
                    first = false;
                }
                else
                {
                    bounds.Encapsulate(world);
                }
            }
            return bounds;
        }

        private static IEnumerable<Vector3> Corners(Bounds bounds)
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            yield return min;
            yield return new Vector3(max.x, min.y, min.z);
            yield return new Vector3(min.x, max.y, min.z);
            yield return new Vector3(min.x, min.y, max.z);
            yield return new Vector3(max.x, max.y, min.z);
            yield return new Vector3(max.x, min.y, max.z);
            yield return new Vector3(min.x, max.y, max.z);
            yield return max;
        }

        /// <summary>Adds a model under <paramref name="parent"/>, standing on the ground at the offset given.</summary>
        public static GameObject Piece(Transform parent, string relative, Vector3 offset = default, float yaw = 0f,
            float scale = 1f, float height = 0f, Color tint = default)
        {
            GameObject model = Model(relative);
            if (model == null)
            {
                return null;
            }
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, parent);
            Bounds bounds = BoundsOf(model);
            if (height > 0f && bounds.size.y > 0.0001f)
            {
                scale *= height / bounds.size.y;
            }
            // Some files are authored in centimeters and carry a hundredfold scale on their root, which has to be
            // kept: overwriting it shrinks the model to nothing.
            instance.transform.localScale = model.transform.localScale * scale;
            instance.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            // The model's feet go on the ground, its middle over the point asked for.
            var foot = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z) * scale;
            instance.transform.localPosition = offset - new Vector3(foot.x, foot.y, foot.z);
            Dress(instance, relative, tint);
            return instance;
        }

        /// <summary>
        /// Gives an instance its materials: the pack's own where they came out right, the pack's texture sheet where
        /// the import left them blank, and a tinted copy where the creature wears a color of its own.
        /// </summary>
        public static void Dress(GameObject instance, string relative, Color tint)
        {
            bool tinted = tint.a > 0f && tint != Color.white;
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material source = materials[i];
                    if (source == null)
                    {
                        continue;
                    }
                    bool missing = Missing(source);
                    if (!missing && !tinted)
                    {
                        continue;
                    }
                    Texture2D sheet = missing ? Sheet(relative, renderer.name, source.name) : null;
                    materials[i] = sheet != null ? Skin(sheet, tint) : Variant(source, tint);
                    changed = true;
                }
                if (changed)
                {
                    renderer.sharedMaterials = materials;
                }
            }
        }

        /// <summary>Whether a material came out of the import with nothing on it.</summary>
        private static bool Missing(Material material)
        {
            return material.HasProperty("_BaseMap") && material.GetTexture("_BaseMap") == null;
        }

        /// <summary>The texture sheet a pack paints its models with, and the one its weapons use.</summary>
        private static Texture2D Sheet(string relative, string rendererName, string materialName)
        {
            string parts = rendererName + " " + materialName;
            bool Holds(string word) => parts.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0;

            if (relative.StartsWith("Quaternius/Characters/", StringComparison.Ordinal))
            {
                string who = relative.Substring("Quaternius/Characters/".Length);
                string weapon = Holds("Staff") ? "_Staff" : Holds("Bow") || Holds("Arrow") ? "_Bow"
                    : Holds("Dagger") ? "_Dagger" : Holds("Sword") ? "_Sword" : "";
                return Texture($"{Models}/Quaternius/Characters/Textures/{who}{weapon}_Texture.png")
                       ?? Texture($"{Models}/Quaternius/Characters/Textures/{who}_Texture.png");
            }
            if (relative.StartsWith("Quaternius/Monsters/Zombie", StringComparison.Ordinal))
            {
                return Texture($"{Models}/Quaternius/Monsters/ZombieTexture.png");
            }
            if (relative.StartsWith("KayKit/", StringComparison.Ordinal))
            {
                string pack = relative.StartsWith("KayKit/Dungeon/", StringComparison.Ordinal) ? "dungeon_texture"
                    : relative.StartsWith("KayKit/Halloween/", StringComparison.Ordinal) ? "halloweenbits_texture"
                    : relative.StartsWith("KayKit/Characters/Skeleton", StringComparison.Ordinal) ? "skeleton_texture"
                    : relative.StartsWith("KayKit/Characters/Knight", StringComparison.Ordinal) ? "knight_texture"
                    : relative.StartsWith("KayKit/Characters/Barbarian", StringComparison.Ordinal) ? "barbarian_texture"
                    : relative.StartsWith("KayKit/Characters/Mage", StringComparison.Ordinal) ? "mage_texture"
                    : relative.StartsWith("KayKit/Characters/Rogue", StringComparison.Ordinal) ? "rogue_texture"
                    : "hexagons_medieval";
                return Texture($"{Models}/KayKit/Textures/{pack}.png");
            }
            return null;
        }

        private static Texture2D Texture(string relative)
        {
            return HeroesAssets.Load<Texture2D>(relative);
        }

        private static readonly Dictionary<(Texture2D, Color), Material> Skins = new Dictionary<(Texture2D, Color), Material>();

        /// <summary>A material that paints a model with its pack's sheet, made once and shared.</summary>
        private static Material Skin(Texture2D sheet, Color tint)
        {
            Color color = tint.a > 0f ? tint : Color.white;
            if (Skins.TryGetValue((sheet, color), out Material cached) && cached != null)
            {
                return cached;
            }
            string name = color == Color.white ? sheet.name : $"{sheet.name}_{ColorUtility.ToHtmlStringRGB(color)}";
            Material material = HeroesAssets.Material($"{Generated}/Materials/{name}.mat",
                Shader.Find("Universal Render Pipeline/Lit"), m =>
                {
                    m.SetTexture("_BaseMap", sheet);
                    m.SetColor("_BaseColor", color);
                    m.SetFloat("_Smoothness", 0.1f);
                    m.SetFloat("_Metallic", 0f);
                });
            Skins[(sheet, color)] = material;
            return material;
        }

        private static readonly Dictionary<(Material, Color), Material> Variants = new Dictionary<(Material, Color), Material>();

        /// <summary>
        /// A plain opaque stand-in for a material of a model: its texture if it had one, else the flat color it was
        /// given, under the tint. Built rather than copied, because some of the packs import materials that draw
        /// nothing at all.
        /// </summary>
        private static Material Variant(Material source, Color tint)
        {
            if (source == null)
            {
                return null;
            }
            if (Variants.TryGetValue((source, tint), out Material cached) && cached != null)
            {
                return cached;
            }
            // Two packs can name a material the same, so the file is named after the one it stands in for.
            string owner = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source));
            string name = $"{source.name}_{(owner.Length >= 6 ? owner.Substring(0, 6) : "x")}_{ColorUtility.ToHtmlStringRGB(tint)}";
            var map = source.HasProperty("_BaseMap") ? source.GetTexture("_BaseMap") : null;
            Color baseColor = source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor")
                : source.HasProperty("_Color") ? source.GetColor("_Color") : Color.white;
            baseColor.a = 1f;
            // A painted model keeps its own colors under the tint; a plain one takes the tint outright, or a dark
            // dragon would stay dark whichever color it was meant to be.
            Color color = tint.a > 0f && tint != Color.white
                ? map != null
                    ? new Color(baseColor.r * tint.r, baseColor.g * tint.g, baseColor.b * tint.b, 1f)
                    : Recolor(baseColor, tint)
                : baseColor;
            Material material = HeroesAssets.Material($"{Generated}/Materials/{name}.mat",
                Shader.Find("Universal Render Pipeline/Lit"), m =>
                {
                    m.SetTexture("_BaseMap", map);
                    m.SetColor("_BaseColor", color);
                    m.SetFloat("_Smoothness", source.HasProperty("_Smoothness") ? source.GetFloat("_Smoothness") : 0.1f);
                    m.SetFloat("_Metallic", source.HasProperty("_Metallic") ? source.GetFloat("_Metallic") : 0f);
                });
            material.name = name;
            Variants[(source, tint)] = material;
            return material;
        }

        /// <summary>The tint's own color, lit the way the model was: its darker parts stay darker.</summary>
        private static Color Recolor(Color source, Color tint)
        {
            Color.RGBToHSV(source, out _, out _, out float value);
            Color.RGBToHSV(tint, out float hue, out float saturation, out float target);
            return Color.HSVToRGB(hue, saturation, Mathf.Clamp01(target * (0.55f + 0.45f * value)));
        }

        /// <summary>Saves a built object as a prefab, keeping the GUID of the one already there.</summary>
        public static GameObject Save(GameObject root, string relative)
        {
            // Parts made of the same model are told apart by name, or a rebuild matches them to each other's saved
            // copies in a different order every time and rewrites a prefab that has not changed.
            var seen = new Dictionary<string, int>();
            foreach (Transform child in root.transform)
            {
                seen[child.name] = seen.TryGetValue(child.name, out int count) ? count + 1 : 1;
                if (seen[child.name] > 1)
                {
                    child.name = $"{child.name} ({seen[child.name]})";
                }
            }
            string path = HeroesAssets.Path(relative);
            HeroesAssets.EnsureFolderOf(relative);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        /// <summary>An empty root for a prefab, named after the file.</summary>
        public static GameObject Root(string name)
        {
            return new GameObject(name);
        }

        // ------------------------------------------------------------------ materials and terrain

        private static void Materials(HeroesArt art)
        {
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
            Shader fog = Shader.Find("Heroes/FogOfWar");

            // The terrain needs a material of the pipeline's own terrain shader; without one in an asset the shader is
            // not in a built player at all and the ground comes out blank.
            Shader ground = Shader.Find("Universal Render Pipeline/Terrain/Lit");
            art.ground = ground != null
                ? HeroesAssets.Material($"{Generated}/Materials/Ground.mat", ground, m => { })
                : null;
            art.water = HeroesAssets.Material($"{Generated}/Materials/Water.mat", lit, m =>
            {
                m.SetColor("_BaseColor", new Color(0.09f, 0.32f, 0.46f, 0.78f));
                m.SetFloat("_Smoothness", 0.92f);
                m.SetFloat("_Metallic", 0.1f);
                Transparent(m);
            });
            art.fog = fog != null
                ? HeroesAssets.Material($"{Generated}/Materials/FogOfWar.mat", fog, m =>
                {
                    m.SetColor("_FogColor", new Color(0.015f, 0.02f, 0.035f, 1f));
                    m.SetColor("_EdgeColor", new Color(0.16f, 0.19f, 0.28f, 1f));
                    m.SetFloat("_Outside", 0.55f);
                })
                : null;
            art.marker = HeroesAssets.Material($"{Generated}/Materials/Marker.mat", unlit, m =>
            {
                m.SetColor("_BaseColor", new Color(1f, 0.92f, 0.6f, 0.75f));
                Transparent(m);
            });
            art.ring = HeroesAssets.Material($"{Generated}/Materials/Ring.mat", unlit, m =>
            {
                m.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.9f));
                Transparent(m);
            });
            art.arrow = Projectile("Arrow", "Quaternius/Items/Arrow", 0.55f, new Color(0.75f, 0.6f, 0.4f));
            art.bolt = Projectile("Bolt", "Quaternius/Items/Arrow", 0.4f, new Color(0.55f, 0.55f, 0.6f));

            // The particles of the spells and bursts: a soft round dot, blended over the scene or adding light to it.
            Texture2D soft = SoftDot();
            Shader particles = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            art.particleMaterial = HeroesAssets.Material($"{Generated}/Effects/Particle.mat", particles, m => Particle(m, soft, false));
            art.glowMaterial = HeroesAssets.Material($"{Generated}/Effects/Glow.mat", particles, m => Particle(m, soft, true));
        }

        /// <summary>A white dot, solid in its middle and fading softly to nothing at its rim.</summary>
        private static Texture2D SoftDot()
        {
            const int size = 64;
            var raster = new Raster(size, size, Color.clear);
            var center = new Vector2(size / 2f, size / 2f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float r = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) / (size * 0.5f);
                    float a = 1f - Mathf.SmoothStep(0.2f, 1f, r);
                    raster.Set(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            return HeroesAssets.SaveTexture(raster.ToTexture(), $"{Generated}/Effects/Soft.png", importer =>
            {
                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.maxTextureSize = 64;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
            });
        }

        /// <summary>A transparent particle material of the pipeline: alpha blended, or additive (it only adds light).</summary>
        private static void Particle(Material material, Texture texture, bool additive)
        {
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", additive ? 2f : 0f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)(additive ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
            material.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)(additive ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.DisableKeyword("_ALPHAMODULATE_ON");
        }

        private static void Transparent(Material material)
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_ZWrite", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
        }

        private static GameObject Projectile(string name, string model, float length, Color tint)
        {
            GameObject root = Root(name);
            GameObject piece = Piece(root.transform, model, Vector3.zero, 0f, 1f, 0f, tint);
            if (piece != null)
            {
                Bounds bounds = BoundsOf(piece);
                float scale = bounds.size.magnitude > 0.001f ? length / Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z)) : 1f;
                piece.transform.localScale *= scale;
                // The arrow lies along its flight, pointing at +Z.
                piece.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                piece.transform.localPosition = Vector3.zero;
            }
            return Save(root, $"{Generated}/Prefabs/{name}.prefab");
        }

        private static void TerrainLayers(HeroesArt art)
        {
            (GroundLayer layer, string texture, float size)[] table =
            {
                (GroundLayer.Grass, "Grass", 8f),
                (GroundLayer.Dirt, "Dirt", 8f),
                (GroundLayer.Sand, "Sand", 8f),
                (GroundLayer.Snow, "Snow", 8f),
                (GroundLayer.Swamp, "Swamp", 8f),
                (GroundLayer.Rough, "Rough", 8f),
                (GroundLayer.Wasteland, "Wasteland", 8f),
                (GroundLayer.Rock, "Rock", 10f),
                (GroundLayer.Road, "Road", 4f),
                (GroundLayer.ForestFloor, "ForestFloor", 6f)
            };
            art.layers = new TerrainLayer[table.Length];
            foreach ((GroundLayer layer, string texture, float size) in table)
            {
                var diffuse = HeroesAssets.Load<Texture2D>($"Art/Terrain/{texture}.jpg");
                var normal = HeroesAssets.Load<Texture2D>($"Art/Terrain/{texture}_Normal.jpg");
                if (normal != null)
                {
                    HeroesAssets.ConfigureTexture($"Art/Terrain/{texture}_Normal.jpg", importer =>
                    {
                        importer.textureType = TextureImporterType.NormalMap;
                        importer.maxTextureSize = 1024;
                    });
                }
                if (diffuse != null)
                {
                    HeroesAssets.ConfigureTexture($"Art/Terrain/{texture}.jpg", importer =>
                    {
                        importer.textureType = TextureImporterType.Default;
                        importer.maxTextureSize = 1024;
                        importer.wrapMode = TextureWrapMode.Repeat;
                    });
                }
                var terrainLayer = new TerrainLayer
                {
                    name = texture,
                    diffuseTexture = diffuse,
                    normalMapTexture = normal,
                    tileSize = new Vector2(size, size),
                    smoothness = 0f,
                    metallic = 0f,
                    normalScale = 0.8f
                };
                art.layers[(int)layer] = HeroesAssets.CreateOrReplace(terrainLayer, $"{Generated}/Terrain/{texture}.terrainlayer");
            }
        }

        // ------------------------------------------------------------------ creatures

        private sealed class UnitSpec
        {
            public CreatureId Creature;
            public string Model;
            public string Idle = "Idle";
            public string Walk = "Walking_A";
            public string Attack = "1H_Melee_Attack_Chop";
            public string Shoot = "";
            public string Hit = "Hit_A";
            public string Death = "Death_A";
            public string Cast = "";
            public ProjectileKind Projectile;
            public float Height;
            public float Yaw;
            public Color Tint = Color.white;
        }

        /// <summary>The clips of the KayKit adventurers and skeletons, which all share one rig.</summary>
        private static UnitSpec Kay(CreatureId creature, string model, string attack = "1H_Melee_Attack_Chop")
        {
            return new UnitSpec { Creature = creature, Model = model, Attack = attack, Cast = "Spellcast_Shoot" };
        }

        /// <summary>The clips of the Quaternius adventurers, whose names carry the armature they belong to.</summary>
        private static UnitSpec Quat(CreatureId creature, string model, string attack)
        {
            return new UnitSpec
            {
                Creature = creature,
                Model = model,
                Idle = "CharacterArmature|Idle",
                Walk = "CharacterArmature|Walk",
                Attack = "CharacterArmature|" + attack,
                Hit = "CharacterArmature|RecieveHit",
                Death = "CharacterArmature|Death"
            };
        }

        private static UnitSpec Animal(CreatureId creature, string model, string attack = "Attack")
        {
            return new UnitSpec
            {
                Creature = creature,
                Model = model,
                Idle = "AnimalArmature|Idle",
                Walk = "AnimalArmature|Walk",
                Attack = "AnimalArmature|" + attack,
                Hit = "AnimalArmature|Idle_HitReact_Left",
                Death = "AnimalArmature|Death"
            };
        }

        private static UnitSpec[] UnitTable()
        {
            var table = new List<UnitSpec>();

            // Castle: peasants who hold the line, archers behind them, and the gold dragon above.
            table.Add(Quat(CreatureId.Militia, "Quaternius/Characters/Monk", "Attack"));
            UnitSpec archer = Quat(CreatureId.Archer, "Quaternius/Characters/Ranger", "Punch");
            archer.Shoot = "CharacterArmature|Bow_Attack_Shoot";
            archer.Projectile = ProjectileKind.Arrow;
            table.Add(archer);
            table.Add(Kay(CreatureId.Swordsman, "KayKit/Characters/Knight"));
            UnitSpec priest = Quat(CreatureId.Priest, "Quaternius/Characters/Cleric", "Punch");
            priest.Shoot = "CharacterArmature|Spell1";
            priest.Cast = "CharacterArmature|Spell1";
            priest.Projectile = ProjectileKind.HolyLight;
            table.Add(priest);
            UnitSpec crusader = Quat(CreatureId.Crusader, "Quaternius/Characters/Warrior", "Sword_Attack");
            table.Add(crusader);
            UnitSpec cavalier = Animal(CreatureId.Cavalier, "Quaternius/Animals/Horse_White", "Attack_Kick");
            cavalier.Walk = "AnimalArmature|Gallop";
            table.Add(cavalier);
            UnitSpec gold = Dragon(CreatureId.GoldDragon, new Color(1f, 0.82f, 0.32f));
            table.Add(gold);

            // Necropolis: the bones that get up again.
            table.Add(Kay(CreatureId.Skeleton, "KayKit/Characters/Skeleton_Minion"));
            table.Add(new UnitSpec
            {
                Creature = CreatureId.Zombie,
                Model = "Quaternius/Monsters/Zombie",
                Idle = "Zombie|ZombieIdle",
                Walk = "Zombie|ZombieWalk",
                Attack = "Zombie|ZombieBite",
                Hit = "",
                Death = "",
                Tint = new Color(0.72f, 0.85f, 0.65f)
            });
            table.Add(Kay(CreatureId.BoneGuard, "KayKit/Characters/Skeleton_Warrior", "2H_Melee_Attack_Chop"));
            UnitSpec boneArcher = Kay(CreatureId.SkeletonArcher, "KayKit/Characters/Skeleton_Rogue");
            boneArcher.Shoot = "1H_Ranged_Shoot";
            boneArcher.Projectile = ProjectileKind.Arrow;
            table.Add(boneArcher);
            table.Add(new UnitSpec
            {
                Creature = CreatureId.VampireBat,
                Model = "Quaternius/Monsters/Bat",
                Idle = "BatArmature|Bat_Flying",
                Walk = "BatArmature|Bat_Flying",
                Attack = "BatArmature|Bat_Attack",
                Hit = "BatArmature|Bat_Hit",
                Death = "BatArmature|Bat_Death",
                Tint = new Color(0.62f, 0.5f, 0.66f)
            });
            UnitSpec lich = Kay(CreatureId.Lich, "KayKit/Characters/Skeleton_Mage", "Spellcast_Shoot");
            lich.Shoot = "Spellcast_Shoot";
            lich.Projectile = ProjectileKind.DeathCloud;
            table.Add(lich);
            table.Add(Dragon(CreatureId.BoneDragon, new Color(0.78f, 0.76f, 0.68f)));

            // Stronghold: wolves, brawlers and the red dragon of the cliffs.
            table.Add(Animal(CreatureId.Wolf, "Quaternius/Animals/Wolf"));
            table.Add(Kay(CreatureId.Brawler, "KayKit/Characters/Barbarian"));
            UnitSpec hunter = Kay(CreatureId.Hunter, "KayKit/Characters/RogueHooded");
            hunter.Shoot = "1H_Ranged_Shoot";
            hunter.Projectile = ProjectileKind.Arrow;
            table.Add(hunter);
            UnitSpec shaman = Quat(CreatureId.Shaman, "Quaternius/Characters/Wizard", "Punch");
            shaman.Shoot = "CharacterArmature|Spell1";
            shaman.Cast = "CharacterArmature|Spell1";
            shaman.Projectile = ProjectileKind.Lightning;
            table.Add(shaman);
            UnitSpec berserker = Kay(CreatureId.Berserker, "KayKit/Characters/Barbarian", "2H_Melee_Attack_Chop");
            berserker.Tint = new Color(0.9f, 0.42f, 0.34f);
            table.Add(berserker);
            UnitSpec bull = Animal(CreatureId.WarBull, "Quaternius/Animals/Bull", "Attack_Headbutt");
            bull.Tint = new Color(0.72f, 0.45f, 0.3f);
            table.Add(bull);
            table.Add(Dragon(CreatureId.RedDragon, new Color(0.95f, 0.35f, 0.25f)));

            // The wilds.
            table.Add(new UnitSpec
            {
                Creature = CreatureId.Slime,
                Model = "Quaternius/Monsters/Slime",
                Idle = "Armature|Slime_Idle",
                Walk = "Armature|Slime_Walk",
                Attack = "Armature|Slime_Attack",
                Hit = "",
                Death = "Armature|Slime_Death",
                Tint = new Color(0.6f, 0.95f, 0.65f)
            });
            table.Add(Quat(CreatureId.Bandit, "Quaternius/Characters/Rogue", "Dagger_Attack"));
            UnitSpec direWolf = Animal(CreatureId.DireWolf, "Quaternius/Animals/Husky");
            direWolf.Tint = new Color(0.45f, 0.45f, 0.5f);
            table.Add(direWolf);
            UnitSpec fireFox = Animal(CreatureId.FireFox, "Quaternius/Animals/Fox");
            fireFox.Tint = new Color(1f, 0.55f, 0.25f);
            table.Add(fireFox);
            UnitSpec hermit = Kay(CreatureId.Hermit, "KayKit/Characters/Mage", "Spellcast_Shoot");
            hermit.Shoot = "Spellcast_Shoot";
            hermit.Projectile = ProjectileKind.Fire;
            table.Add(hermit);
            UnitSpec stag = Animal(CreatureId.ElderStag, "Quaternius/Animals/Stag", "Attack_Headbutt");
            table.Add(stag);
            table.Add(Dragon(CreatureId.GreenDragon, new Color(0.45f, 0.85f, 0.45f)));

            // The arrow tower of a besieged town does not move.
            table.Add(new UnitSpec
            {
                Creature = CreatureId.ArrowTower,
                Model = "KayKit/Buildings/Red/building_tower_A_red",
                Idle = "",
                Walk = "",
                Attack = "",
                Shoot = "",
                Hit = "",
                Death = "",
                Projectile = ProjectileKind.Bolt,
                Height = 3.2f
            });
            return table.ToArray();
        }

        private static UnitSpec Dragon(CreatureId creature, Color tint)
        {
            return new UnitSpec
            {
                Creature = creature,
                Model = "Quaternius/Monsters/Dragon",
                Idle = "DragonArmature|Dragon_Flying",
                Walk = "DragonArmature|Dragon_Flying",
                Attack = "DragonArmature|Dragon_Attack",
                Hit = "DragonArmature|Dragon_Hit",
                Death = "DragonArmature|Dragon_Death",
                Tint = tint
            };
        }

        private static void Units(HeroesArt art)
        {
            art.units.Clear();
            foreach (UnitSpec spec in UnitTable())
            {
                CreatureDef def = Creatures.Get(spec.Creature);
                float height = spec.Height > 0f ? spec.Height : TierHeight[Mathf.Clamp(def.Tier - 1, 0, TierHeight.Length - 1)];
                if (def.Abilities.HasFlag(Ability.Flying) && spec.Height <= 0f)
                {
                    height *= 1.15f;
                }
                GameObject root = Root(def.Name.Replace(" ", string.Empty));
                Piece(root.transform, spec.Model, Vector3.zero, spec.Yaw, 1f, height, spec.Tint);
                GameObject prefab = Save(root, $"{Generated}/Units/{spec.Creature}.prefab");
                art.units.Add(new HeroesArt.UnitArt
                {
                    creature = spec.Creature,
                    prefab = prefab,
                    height = height,
                    idle = spec.Idle,
                    walk = spec.Walk,
                    attack = spec.Attack,
                    shoot = spec.Shoot,
                    hit = spec.Hit,
                    death = spec.Death,
                    cast = spec.Cast,
                    projectile = spec.Projectile,
                    flies = def.Abilities.HasFlag(Ability.Flying)
                });
            }
        }

        // ------------------------------------------------------------------ heroes

        private static void Heroes(HeroesArt art)
        {
            art.heroes.Clear();
            (HeroClass heroClass, string rider, string mount, string pose, string cast, Color tint)[] table =
            {
                (HeroClass.Knight, "KayKit/Characters/Knight", "Quaternius/Animals/Horse_White", "Sit_Chair_Pose", "", Color.white),
                (HeroClass.Cleric, "Quaternius/Characters/Cleric", "Quaternius/Animals/Horse_White", "", "CharacterArmature|Spell1", Color.white),
                (HeroClass.DeathKnight, "KayKit/Characters/Skeleton_Warrior", "Quaternius/Animals/Horse", "Sit_Chair_Pose", "", new Color(0.7f, 0.7f, 0.78f)),
                (HeroClass.Necromancer, "KayKit/Characters/Skeleton_Mage", "Quaternius/Animals/Horse", "Sit_Chair_Pose", "Spellcast_Shoot", new Color(0.7f, 0.7f, 0.78f)),
                (HeroClass.Barbarian, "KayKit/Characters/Barbarian", "Quaternius/Animals/Bull", "Sit_Chair_Pose", "", Color.white),
                (HeroClass.BattleMage, "Quaternius/Characters/Wizard", "Quaternius/Animals/Horse", "", "CharacterArmature|Spell1", Color.white)
            };
            foreach ((HeroClass heroClass, string rider, string mount, string pose, string cast, Color tint) in table)
            {
                GameObject mountRoot = Root(heroClass + "Mount");
                Piece(mountRoot.transform, mount, Vector3.zero, 0f, 1f, 1.85f);
                GameObject mountPrefab = Save(mountRoot, $"{Generated}/Heroes/{heroClass}Mount.prefab");

                GameObject riderRoot = Root(heroClass + "Rider");
                GameObject model = Piece(riderRoot.transform, rider, Vector3.zero, 0f, 1f, 1.7f, tint);
                string seated = pose;
                if (string.IsNullOrEmpty(seated) && model != null)
                {
                    // The Quaternius adventurers have no seated clip, so one is made of their idle with the legs bent.
                    seated = Saddle(model, $"{Generated}/Heroes/{heroClass}Saddle.anim");
                }
                GameObject riderPrefab = Save(riderRoot, $"{Generated}/Heroes/{heroClass}Rider.prefab");

                art.heroes.Add(new HeroesArt.HeroArt
                {
                    heroClass = heroClass,
                    rider = riderPrefab,
                    mount = mountPrefab,
                    riderPose = seated,
                    riderCast = cast,
                    mountIdle = "AnimalArmature|Idle",
                    mountWalk = "AnimalArmature|Walk",
                    seat = Seat(mountPrefab, riderPrefab, "AnimalArmature|Idle", seated)
                });
            }
        }

        /// <summary>
        /// Bakes a seated pose for a rider whose pack has none: his idle a third of the way in, thighs forward and shins
        /// down, held in a legacy clip added to his model's clips, so the map's puppet plays it like any other. Returns
        /// the name the clip plays by, which is the name of its file.
        /// </summary>
        private static string Saddle(GameObject model, string relative)
        {
            var animation = model.GetComponentInChildren<Animation>(true);
            if (animation == null)
            {
                return "";
            }
            Transform[] bones = animation.GetComponentsInChildren<Transform>(true);
            var rest = bones.Select(bone => (bone.localPosition, bone.localRotation)).ToArray();
            HeroesPortraits.Pose(model, "Idle");
            HeroesPortraits.Straddle(model);
            var clip = new AnimationClip { name = System.IO.Path.GetFileNameWithoutExtension(relative), legacy = true, wrapMode = WrapMode.Loop };
            for (int i = 0; i < bones.Length; i++)
            {
                Transform bone = bones[i];
                if (bone == animation.transform)
                {
                    continue;
                }
                string path = AnimationUtility.CalculateTransformPath(bone, animation.transform);
                Quaternion r = bone.localRotation;
                Vector3 p = bone.localPosition;
                clip.SetCurve(path, typeof(Transform), "localRotation.x", AnimationCurve.Constant(0f, 1f, r.x));
                clip.SetCurve(path, typeof(Transform), "localRotation.y", AnimationCurve.Constant(0f, 1f, r.y));
                clip.SetCurve(path, typeof(Transform), "localRotation.z", AnimationCurve.Constant(0f, 1f, r.z));
                clip.SetCurve(path, typeof(Transform), "localRotation.w", AnimationCurve.Constant(0f, 1f, r.w));
                clip.SetCurve(path, typeof(Transform), "localPosition.x", AnimationCurve.Constant(0f, 1f, p.x));
                clip.SetCurve(path, typeof(Transform), "localPosition.y", AnimationCurve.Constant(0f, 1f, p.y));
                clip.SetCurve(path, typeof(Transform), "localPosition.z", AnimationCurve.Constant(0f, 1f, p.z));
            }
            clip.EnsureQuaternionContinuity();
            // The model goes back to its rest pose before it is saved; the clip holds the seated one.
            for (int i = 0; i < bones.Length; i++)
            {
                bones[i].localPosition = rest[i].localPosition;
                bones[i].localRotation = rest[i].localRotation;
            }
            clip = HeroesAssets.CreateOrReplace(clip, relative);
            // Added under its own name: a clip added under another is copied, and the copy, being no asset, would be
            // left out of the saved prefab.
            if (animation.GetClip(clip.name) == null)
            {
                animation.AddClip(clip, clip.name);
            }
            return clip.name;
        }

        /// <summary>
        /// Where the rider's feet go on the mount so that, seated, his hips rest in the saddle: a third of the way back
        /// from the withers (the first bone of the neck) to the croup (the root of the tail), a little under the top of
        /// the mount's back there, with the mount in its idle and the rider in his seated pose.
        /// </summary>
        private static Vector3 Seat(GameObject mount, GameObject rider, string mountIdle, string pose)
        {
            var fallback = new Vector3(0f, 1.25f, -0.05f);
            using (var studio = new ModelStudio())
            {
                GameObject horse = studio.Place(mount);
                GameObject man = studio.Place(rider);
                HeroesPortraits.Pose(horse, mountIdle);
                HeroesPortraits.Pose(man, pose, 0f);
                Transform croup = Bone(horse, "Back");
                Transform withers = Bone(horse, "Neck1");
                if (withers == null)
                {
                    withers = Bone(horse, "Torso");
                }
                Transform hips = Bone(man, "hips");
                if (croup == null || withers == null || hips == null)
                {
                    return fallback;
                }
                Vector3 back = horse.transform.InverseTransformPoint(croup.position);
                Vector3 front = horse.transform.InverseTransformPoint(withers.position);
                float z = Mathf.Lerp(front.z, back.z, 1f / 3f);
                // The hips sink a little under the top of the back, the thighs hanging down its flanks.
                float y = BackTop(horse, z, out float top) ? top - 0.03f : Mathf.Max(back.y, front.y) + 0.08f;
                Vector3 hipsAt = man.transform.InverseTransformPoint(hips.position);
                return new Vector3(0f, y - hipsAt.y, z - hipsAt.z);
            }
        }

        /// <summary>The highest point of a posed model over a strip across its middle at <paramref name="z"/>, in its own space.</summary>
        private static bool BackTop(GameObject model, float z, out float top)
        {
            top = float.MinValue;
            foreach (SkinnedMeshRenderer skin in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var baked = new Mesh();
                skin.BakeMesh(baked);
                // A baked mesh is in the renderer's place and turn, without its scale.
                Matrix4x4 toModel = model.transform.worldToLocalMatrix * Matrix4x4.TRS(skin.transform.position, skin.transform.rotation, Vector3.one);
                foreach (Vector3 vertex in baked.vertices)
                {
                    Vector3 p = toModel.MultiplyPoint3x4(vertex);
                    if (Mathf.Abs(p.x) < 0.25f && Mathf.Abs(p.z - z) < 0.12f)
                    {
                        top = Mathf.Max(top, p.y);
                    }
                }
                Object.DestroyImmediate(baked);
            }
            return top > float.MinValue;
        }

        private static Transform Bone(GameObject root, string name)
        {
            return root.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => string.Equals(t.name, name, StringComparison.OrdinalIgnoreCase));
        }

        // ------------------------------------------------------------------ scenery

        private static void Scatter(HeroesArt art)
        {
            // The nature pack is built in hexagons of its own, so its woods and mountains are fitted to the cell
            // across rather than given a height, and the single trees and rocks are given a height.
            const float cell = HexLayout.DefaultRadius * 1.7320508f;
            art.forestTrees = Trees("Forest", new[]
            {
                ("KayKit/Nature/tree_single_A", 3.4f), ("KayKit/Nature/tree_single_B", 3.8f),
                ("KayKit/Nature/tree_single_A", 2.6f), ("KayKit/Nature/tree_single_B", 2.9f),
                ("KayKit/Nature/tree_single_A_cut", 1.6f), ("KayKit/Nature/tree_single_B_cut", 1.8f)
            });
            // The only pines of the packs are the Halloween pack's autumn ones; their needles are repainted green.
            art.pineTrees = Trees("Pine", new[]
            {
                ("KayKit/Halloween/tree_pine_yellow_large", 4.2f), ("KayKit/Halloween/tree_pine_yellow_medium", 3.2f),
                ("KayKit/Halloween/tree_pine_orange_large", 4.2f), ("KayKit/Halloween/tree_pine_orange_medium", 3.2f)
            }, HeroesBattleArt.Paint.Evergreen);
            art.deadTrees = Trees("Dead", new[]
            {
                ("KayKit/Halloween/tree_dead_large", 3.8f), ("KayKit/Halloween/tree_dead_medium", 3.0f),
                ("KayKit/Halloween/tree_dead_small", 2.2f)
            });
            // Kept for a map that wants a peak of its own; the terrain raises its own mountains.
            art.mountains = Group("Mountain", new[]
            {
                Wide("KayKit/Nature/mountain_A", cell * 1.15f), Wide("KayKit/Nature/mountain_B", cell * 1.15f),
                Wide("KayKit/Nature/mountain_C", cell * 1.15f)
            });
            art.rocks = Group("Rock", new[]
            {
                Tall("KayKit/Nature/rock_single_A", 1.1f), Tall("KayKit/Nature/rock_single_B", 0.9f),
                Tall("KayKit/Nature/rock_single_C", 1.3f), Tall("KayKit/Nature/rock_single_D", 0.8f),
                Tall("KayKit/Nature/rock_single_E", 1.0f)
            });
            art.waterPlants = Group("Water", new[]
            {
                Tall("KayKit/Nature/waterlily_A", 0.08f), Tall("KayKit/Nature/waterlily_B", 0.08f),
                Tall("KayKit/Nature/waterplant_A", 0.9f), Tall("KayKit/Nature/waterplant_B", 1.0f),
                Tall("KayKit/Nature/waterplant_C", 0.8f)
            });
            art.flags = new[]
            {
                Flag(0, "KayKit/Props/flag_red"), Flag(1, "KayKit/Props/flag_blue"),
                Flag(2, "KayKit/Props/flag_green"), Flag(3, "KayKit/Props/flag_yellow"),
                Flag(4, "KayKit/Dungeon/banner_patternA_white")
            };
            art.towers = new[]
            {
                Tower(0, "Red"), Tower(1, "Blue"), Tower(2, "Green"), Tower(3, "Yellow"), Tower(4, "Red")
            };
        }

        private static (string model, float size, bool across) Tall(string model, float height)
        {
            return (model, height, false);
        }

        private static (string model, float size, bool across) Wide(string model, float width)
        {
            return (model, width, true);
        }

        /// <summary>
        /// The trees the terrain plants: the model itself is the prefab, because a terrain prototype has to carry a
        /// renderer on its own root and an empty holder around it would be refused.
        /// </summary>
        private static GameObject[] Trees(string name, (string model, float height)[] pieces,
            HeroesBattleArt.Paint paint = HeroesBattleArt.Paint.None)
        {
            var prefabs = new List<GameObject>();
            for (int i = 0; i < pieces.Length; i++)
            {
                GameObject root = Root($"{name}{i + 1}");
                GameObject piece = Piece(root.transform, pieces[i].model, Vector3.zero, 0f, 1f, pieces[i].height);
                if (piece == null)
                {
                    Object.DestroyImmediate(root);
                    continue;
                }
                HeroesBattleArt.Repaint(piece, paint);
                // The model stands on its own: its scale is kept, and it starts at the point the terrain plants it.
                piece.transform.SetParent(null, true);
                piece.transform.localPosition = Vector3.zero;
                Object.DestroyImmediate(root);
                prefabs.Add(Save(piece, $"{Generated}/Scenery/{name}{i + 1}.prefab"));
            }
            return prefabs.ToArray();
        }

        private static GameObject[] Group(string name, (string model, float size, bool across)[] pieces)
        {
            var prefabs = new List<GameObject>();
            for (int i = 0; i < pieces.Length; i++)
            {
                GameObject root = Root($"{name}{i + 1}");
                (string model, float size, bool across) = pieces[i];
                GameObject piece = Piece(root.transform, model, Vector3.zero, 0f, 1f, across ? 0f : size);
                if (piece == null)
                {
                    Object.DestroyImmediate(root);
                    continue;
                }
                if (across)
                {
                    // A tile of the nature pack is fitted to the cell it stands on.
                    Bounds bounds = BoundsOf(piece);
                    float widest = Mathf.Max(bounds.size.x, bounds.size.z);
                    if (widest > 0.0001f)
                    {
                        piece.transform.localScale *= size / widest;
                        Bounds sized = BoundsOf(piece);
                        Vector3 at = piece.transform.localPosition;
                        piece.transform.localPosition = new Vector3(at.x - sized.center.x, at.y - sized.min.y, at.z - sized.center.z);
                    }
                }
                prefabs.Add(Save(root, $"{Generated}/Scenery/{name}{i + 1}.prefab"));
            }
            return prefabs.ToArray();
        }

        private static GameObject Flag(int color, string model)
        {
            GameObject root = Root($"Flag{color}");
            Piece(root.transform, model, Vector3.zero, 0f, 1f, 1.9f);
            return Save(root, $"{Generated}/Scenery/Flag{color}.prefab");
        }

        private static GameObject Tower(int color, string folder)
        {
            GameObject root = Root($"Tower{color}");
            Piece(root.transform, $"KayKit/Buildings/{folder}/building_tower_A_{folder.ToLowerInvariant()}", Vector3.zero, 0f, 1f, 3.2f);
            return Save(root, $"{Generated}/Scenery/Tower{color}.prefab");
        }

        // ------------------------------------------------------------------ sound

        private static void Audio(HeroesArt art)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { HeroesAssets.Path("Art/Audio") }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer == null)
                {
                    continue;
                }
                bool music = path.Contains("/Music/");
                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                settings.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = music ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.Vorbis;
                settings.quality = music ? 0.6f : 0.85f;
                string before = EditorJsonUtility.ToJson(importer);
                importer.defaultSampleSettings = settings;
                importer.forceToMono = !music;
                importer.loadInBackground = music;
                if (EditorJsonUtility.ToJson(importer) != before)
                {
                    importer.SaveAndReimport();
                }
            }

            art.menuMusic = Music("Heroic Age");
            art.adventureMusic = new[] { Music("Celtic Impulse"), Music("Minstrel Guild"), Music("Teller of the Tales") }
                .Where(clip => clip != null).ToArray();
            art.battleMusic = new[] { Music("Five Armies"), Music("Crusade") }.Where(clip => clip != null).ToArray();
            art.townMusic = new[] { Music("Master of the Feast"), Music("Unholy Knight"), Music("Crusade") };
            art.victoryMusic = Music("Heroic Age");
            art.defeatMusic = Music("Unholy Knight");

            var sounds = new AudioClip[Enum.GetValues(typeof(Sfx)).Length];
            foreach (Sfx sfx in Enum.GetValues(typeof(Sfx)))
            {
                sounds[(int)sfx] = Sound(sfx.ToString());
            }
            art.sounds = sounds;
        }

        private static AudioClip Music(string name)
        {
            return HeroesAssets.Load<AudioClip>($"Art/Audio/Music/{name}.mp3");
        }

        private static AudioClip Sound(string name)
        {
            return HeroesAssets.Load<AudioClip>($"Art/Audio/Sfx/{name}.ogg")
                   ?? HeroesAssets.Load<AudioClip>($"Art/Audio/Sfx/{name}.wav");
        }
    }
}
