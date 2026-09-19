using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Portfolio.Asteroids.EditorTools
{
    /// <summary>
    /// Where the Asteroids module's generated assets live and how they are written. The game's files are mounted at
    /// Assets/ in its own project and at Packages/com.skinnerboxes.asteroids/ in BaseGame, so every path is built from
    /// <see cref="Root"/>. Existing assets are updated in place, which keeps their GUIDs and every reference, and files
    /// whose content did not change are not rewritten (another editor may be reading them).
    /// </summary>
    internal static class AsteroidsAssets
    {
        private static string root;

        public static string Root
        {
            get
            {
                if (root == null)
                {
                    PackageInfo package = PackageInfo.FindForAssembly(typeof(AsteroidsAssets).Assembly);
                    root = package != null ? package.assetPath : "Assets";
                }
                return root;
            }
        }

        public static string Path(string relative)
        {
            return $"{Root}/{relative}";
        }

        /// <summary>The file on disk behind an asset path, also for paths inside a package.</summary>
        public static string FullPath(string assetPath)
        {
            string physical = FileUtil.GetPhysicalPath(assetPath);
            return System.IO.Path.GetFullPath(string.IsNullOrEmpty(physical) ? assetPath : physical);
        }

        public static void EnsureFolder(string relativeFolder)
        {
            string current = Root;
            foreach (string part in relativeFolder.Split('/'))
            {
                string next = $"{current}/{part}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, part);
                }
                current = next;
            }
        }

        private static void EnsureFolderOf(string relativeFile)
        {
            int slash = relativeFile.LastIndexOf('/');
            if (slash > 0)
            {
                EnsureFolder(relativeFile.Substring(0, slash));
            }
        }

        public static T Load<T>(string relative) where T : Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(Path(relative));
        }

        /// <summary>A sub-asset (a mesh inside a model file, a sprite inside a texture) by name.</summary>
        public static T LoadSub<T>(string relative, string name) where T : Object
        {
            return AssetDatabase.LoadAllAssetsAtPath(Path(relative)).OfType<T>().FirstOrDefault(asset => asset.name == name);
        }

        /// <summary>Writes a mesh asset, replacing the geometry of an existing one.</summary>
        public static Mesh SaveMesh(MeshBuilder builder, string relative)
        {
            EnsureFolderOf(relative);
            string path = Path(relative);
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            string name = System.IO.Path.GetFileNameWithoutExtension(relative);
            if (mesh == null)
            {
                mesh = builder.ToMesh(name);
                AssetDatabase.CreateAsset(mesh, path);
            }
            else
            {
                Mesh fresh = builder.ToMesh(name);
                if (!SameMesh(mesh, fresh))
                {
                    builder.Fill(mesh);
                    mesh.name = name;
                    EditorUtility.SetDirty(mesh);
                }
                Object.DestroyImmediate(fresh);
            }
            return mesh;
        }

        private static bool SameMesh(Mesh a, Mesh b)
        {
            if (a.vertexCount != b.vertexCount || a.subMeshCount != b.subMeshCount)
            {
                return false;
            }
            if (!a.vertices.SequenceEqual(b.vertices) || !a.normals.SequenceEqual(b.normals) || !a.uv.SequenceEqual(b.uv))
            {
                return false;
            }
            for (int i = 0; i < a.subMeshCount; i++)
            {
                if (!a.GetTriangles(i).SequenceEqual(b.GetTriangles(i)))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>Writes <paramref name="bytes"/> unless the file already holds exactly them. Returns true when written.</summary>
        private static bool WriteIfChanged(string assetPath, byte[] bytes)
        {
            string file = FullPath(assetPath);
            if (File.Exists(file) && File.ReadAllBytes(file).SequenceEqual(bytes))
            {
                return false;
            }
            File.WriteAllBytes(file, bytes);
            return true;
        }

        /// <summary>Marks <paramref name="asset"/> dirty only when <paramref name="change"/> altered its serialized data.</summary>
        private static void ApplyIfChanged(Object asset, Action change)
        {
            string before = EditorJsonUtility.ToJson(asset);
            change();
            if (EditorJsonUtility.ToJson(asset) != before)
            {
                EditorUtility.SetDirty(asset);
            }
        }

        /// <summary>Writes a texture as a PNG file and imports it with <paramref name="configure"/> applied.</summary>
        public static Texture2D SaveTexture(Texture2D texture, string relative, Action<TextureImporter> configure)
        {
            EnsureFolderOf(relative);
            string path = Path(relative);
            bool written = WriteIfChanged(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            if (written || AssetImporter.GetAtPath(path) == null)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            string before = EditorJsonUtility.ToJson(importer);
            configure(importer);
            if (written || EditorJsonUtility.ToJson(importer) != before)
            {
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        public static void TileableTexture(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Default;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.mipmapEnabled = true;
            importer.anisoLevel = 4;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
        }

        /// <summary>Linear data (noise channels) that tiles.</summary>
        public static void DataTexture(TextureImporter importer)
        {
            TileableTexture(importer);
            importer.sRGBTexture = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }

        public static void ClampedTexture(TextureImporter importer)
        {
            TileableTexture(importer);
            importer.wrapModeU = TextureWrapMode.Repeat;
            importer.wrapModeV = TextureWrapMode.Clamp;
        }

        /// <summary>A latitude-longitude picture imported as a cube map, convolved for glossy reflections.</summary>
        public static void CubemapTexture(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Default;
            importer.textureShape = TextureImporterShape.TextureCube;
            importer.generateCubemap = TextureImporterGenerateCubemap.Cylindrical;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Trilinear;
            importer.mipmapEnabled = true;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.cubemapConvolution = TextureImporterCubemapConvolution.Specular;
            settings.seamlessCubemap = true;
            importer.SetTextureSettings(settings);
        }

        public static void PaletteTexture(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Default;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
        }

        public static void ParticleTexture(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Default;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = true;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }

        public static Action<TextureImporter> SpriteTexture(Vector4 border)
        {
            return importer =>
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.spritePixelsPerUnit = 100f;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.spriteBorder = border;
                importer.SetTextureSettings(settings);
            };
        }

        /// <summary>Creates or updates a material asset; <paramref name="setup"/> sets its properties.</summary>
        public static Material SaveMaterial(string relative, Shader shader, Action<Material> setup)
        {
            EnsureFolderOf(relative);
            string path = Path(relative);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            ApplyIfChanged(material, () =>
            {
                if (material.shader != shader)
                {
                    material.shader = shader;
                }
                setup(material);
            });
            return material;
        }

        /// <summary>Saves <paramref name="rootObject"/> as a prefab (keeping the GUID of an existing one) and destroys it.</summary>
        public static GameObject SavePrefab(GameObject rootObject, string relative)
        {
            EnsureFolderOf(relative);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(rootObject, Path(relative));
            Object.DestroyImmediate(rootObject);
            return prefab;
        }

        public static AudioClip SaveAudio(float[] samples, string relative, bool music)
        {
            EnsureFolderOf(relative);
            string path = Path(relative);
            bool written = WriteIfChanged(path, SpaceSounds.Wav(samples));
            if (written || AssetImporter.GetAtPath(path) == null)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }
            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            string before = EditorJsonUtility.ToJson(importer);
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = music ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = music ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.PCM;
            settings.quality = 0.6f;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = true;
            if (written || EditorJsonUtility.ToJson(importer) != before)
            {
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        public static T SaveScriptable<T>(string relative, Action<T> setup) where T : ScriptableObject
        {
            EnsureFolderOf(relative);
            string path = Path(relative);
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                Object existing = AssetDatabase.LoadMainAssetAtPath(path);
                if (existing is ScriptableObject scriptable && ConvertScript(scriptable, typeof(T)))
                {
                    asset = AssetDatabase.LoadAssetAtPath<T>(path);
                }
            }
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                asset.name = System.IO.Path.GetFileNameWithoutExtension(relative);
                AssetDatabase.CreateAsset(asset, path);
            }
            ApplyIfChanged(asset, () => setup(asset));
            return asset;
        }

        /// <summary>
        /// Points an existing asset at another script (a base class asset becoming its derived class), keeping its GUID
        /// and whatever serialized data the two share.
        /// </summary>
        private static bool ConvertScript(ScriptableObject asset, Type type)
        {
            MonoScript script = FindScript(type);
            if (script == null)
            {
                return false;
            }
            string path = AssetDatabase.GetAssetPath(asset);
            var serialized = new SerializedObject(asset);
            SerializedProperty property = serialized.FindProperty("m_Script");
            if (property == null)
            {
                return false;
            }
            property.objectReferenceValue = script;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            return true;
        }

        private static MonoScript FindScript(Type type)
        {
            foreach (string guid in AssetDatabase.FindAssets($"t:MonoScript {type.Name}"))
            {
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(guid));
                if (script != null && script.GetClass() == type)
                {
                    return script;
                }
            }
            return null;
        }

        /// <summary>Sets a serialized property by path (for private and auto-property backing fields).</summary>
        public static void Set(Object target, string property, Action<SerializedProperty> assign)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty found = serialized.FindProperty(property);
            if (found == null)
            {
                throw new ArgumentException($"{target.GetType().Name} has no serialized property '{property}'.");
            }
            assign(found);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetObject(Object target, string property, Object value)
        {
            Set(target, property, p => p.objectReferenceValue = value);
        }

        public static void SetObjects(Object target, string property, params Object[] values)
        {
            Set(target, property, p =>
            {
                p.arraySize = values.Length;
                for (int i = 0; i < values.Length; i++)
                {
                    p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
                }
            });
        }

        /// <summary>Sets a <c>[field: SerializeField]</c> auto-property by its property name.</summary>
        public static void SetAuto(Object target, string propertyName, Action<SerializedProperty> assign)
        {
            Set(target, $"<{propertyName}>k__BackingField", assign);
        }
    }
}
