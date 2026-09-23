using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Portfolio.Heroes.EditorTools
{
    /// <summary>
    /// Where the Heroes assets live and how the builders write them. The game's files are mounted at Assets/ in its own
    /// project and at Packages/com.skinnerboxes.heroes/ in BaseGame, so every path is built from <see cref="Root"/>.
    /// Existing assets are updated in place (GUIDs and references are kept) and files whose content did not change are
    /// not rewritten, so a rebuild does not touch files another editor may be importing.
    /// </summary>
    internal static class HeroesAssets
    {
        private static string root;

        public static string Root
        {
            get
            {
                if (root == null)
                {
                    PackageInfo package = PackageInfo.FindForAssembly(typeof(HeroesAssets).Assembly);
                    root = package != null ? package.assetPath : "Assets";
                }
                return root;
            }
        }

        public static string Path(string relative)
        {
            return $"{Root}/{relative}";
        }

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

        public static void EnsureFolderOf(string relativeFile)
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

        public static T Require<T>(string relative) where T : Object
        {
            T asset = Load<T>(relative);
            if (asset == null)
            {
                throw new InvalidOperationException($"Heroes: {Path(relative)} is missing.");
            }
            return asset;
        }

        public static bool WriteIfChanged(string assetPath, byte[] bytes)
        {
            string file = FullPath(assetPath);
            if (File.Exists(file) && File.ReadAllBytes(file).SequenceEqual(bytes))
            {
                return false;
            }
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(file) ?? ".");
            File.WriteAllBytes(file, bytes);
            return true;
        }

        /// <summary>Creates the asset, or copies the new values into the one already there (keeping its GUID).</summary>
        public static T CreateOrReplace<T>(T asset, string relative) where T : Object
        {
            EnsureFolderOf(relative);
            string path = Path(relative);
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(asset, path);
                return asset;
            }
            string before = EditorJsonUtility.ToJson(existing);
            EditorUtility.CopySerialized(asset, existing);
            if (EditorJsonUtility.ToJson(existing) != before)
            {
                EditorUtility.SetDirty(existing);
            }
            if (!ReferenceEquals(asset, existing))
            {
                Object.DestroyImmediate(asset, true);
            }
            return existing;
        }

        /// <summary>A material asset with <paramref name="shader"/>, created or updated in place.</summary>
        public static Material Material(string relative, Shader shader, Action<Material> configure)
        {
            EnsureFolderOf(relative);
            string path = Path(relative);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                configure(material);
                AssetDatabase.CreateAsset(material, path);
                return material;
            }
            string before = EditorJsonUtility.ToJson(material);
            if (material.shader != shader)
            {
                material.shader = shader;
            }
            configure(material);
            if (EditorJsonUtility.ToJson(material) != before)
            {
                EditorUtility.SetDirty(material);
            }
            return material;
        }

        /// <summary>Writes a generated texture as a PNG and imports it with <paramref name="configure"/> applied.</summary>
        public static Texture2D SaveTexture(Texture2D texture, string relative, Action<TextureImporter> configure)
        {
            EnsureFolderOf(relative);
            string path = Path(relative);
            bool written = WriteIfChanged(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            return ConfigureTexture(relative, configure, written);
        }

        public static Texture2D ConfigureTexture(string relative, Action<TextureImporter> configure, bool reimport = false)
        {
            string path = Path(relative);
            if (reimport || AssetImporter.GetAtPath(path) == null)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Heroes: {path} is not a texture.");
            }
            string before = EditorJsonUtility.ToJson(importer);
            // A meta written by hand could read as a cubemap; a texture here is always flat.
            importer.textureShape = TextureImporterShape.Texture2D;
            configure(importer);
            if (reimport || EditorJsonUtility.ToJson(importer) != before)
            {
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        public static Sprite Sprite(string relative)
        {
            return AssetDatabase.LoadAllAssetsAtPath(Path(relative)).OfType<Sprite>().FirstOrDefault();
        }

        public static void SpriteImport(TextureImporter importer, Vector4 border = default, int maxSize = 512)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = maxSize;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            if (border != Vector4.zero)
            {
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.spriteBorder = border;
                importer.SetTextureSettings(settings);
            }
        }


        // ------------------------------------------------------------------ writing fields the game keeps private

        /// <summary>Writes a serialized field of an asset, backing fields of auto properties included.</summary>
        public static void Set(Object target, string property, Action<SerializedProperty> assign)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty found = serialized.FindProperty(property);
            if (found == null)
            {
                throw new InvalidOperationException($"{target.GetType().Name} has no serialized property '{property}'.");
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

        public static void SetInt(Object target, string property, int value)
        {
            Set(target, property, p => p.intValue = value);
        }

        public static void SetString(Object target, string property, string value)
        {
            Set(target, property, p => p.stringValue = value);
        }

        public static void SetBool(Object target, string property, bool value)
        {
            Set(target, property, p => p.boolValue = value);
        }

        public static void SetFloat(Object target, string property, float value)
        {
            Set(target, property, p => p.floatValue = value);
        }

        public static void Delete(string relative)
        {
            string path = Path(relative);
            if (AssetDatabase.IsValidFolder(path) || AssetDatabase.LoadMainAssetAtPath(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
            }
        }
    }
}
