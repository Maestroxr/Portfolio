using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Portfolio.EndlessRunner.EditorTools
{
    /// <summary>
    /// Where the Endless Runner's generated assets live and how they are written. The game's files are mounted at
    /// Assets/ in its own project and at Packages/com.skinnerboxes.endlessrunner/ in BaseGame, so every path is built
    /// from <see cref="Root"/>. Existing assets are updated in place, which keeps their GUIDs and every reference.
    /// </summary>
    internal static class RunnerAssets
    {
        private static string root;

        public static string Root
        {
            get
            {
                if (root == null)
                {
                    PackageInfo package = PackageInfo.FindForAssembly(typeof(RunnerAssets).Assembly);
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

        /// <summary>
        /// Writes a texture as a PNG file and imports it with <paramref name="configure"/> applied. Unchanged files are
        /// left alone, so a rebuild does not touch assets another editor may be reading at the same time.
        /// </summary>
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
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.textureCompression = TextureImporterCompression.Compressed;
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

        /// <summary>Saves <paramref name="root"/> as a prefab (keeping the GUID of an existing one) and destroys it.</summary>
        public static GameObject SavePrefab(GameObject root, string relative)
        {
            EnsureFolderOf(relative);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, Path(relative));
            Object.DestroyImmediate(root);
            return prefab;
        }

        public static AudioClip SaveAudio(float[] samples, string relative, bool music)
        {
            EnsureFolderOf(relative);
            string path = Path(relative);
            bool written = WriteIfChanged(path, SoundFactory.Wav(samples));
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
                asset = ScriptableObject.CreateInstance<T>();
                asset.name = System.IO.Path.GetFileNameWithoutExtension(relative);
                AssetDatabase.CreateAsset(asset, path);
            }
            ApplyIfChanged(asset, () => setup(asset));
            return asset;
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
    }
}
