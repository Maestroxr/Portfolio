using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Portfolio.Monopoly.EditorTools
{
    /// <summary>
    /// Where the Monopoly assets live and how the generators write them. The game's files are mounted at Assets/ in its
    /// own project and at Packages/com.skinnerboxes.monopoly/ in BaseGame, so every path is built from <see cref="Root"/>.
    /// Existing assets are updated in place (GUIDs and references are kept) and files whose content did not change are
    /// not rewritten, so a rebuild does not touch files another editor may be importing.
    /// </summary>
    internal static class MonopolyAssets
    {
        private static string root;

        public static string Root
        {
            get
            {
                if (root == null)
                {
                    PackageInfo package = PackageInfo.FindForAssembly(typeof(MonopolyAssets).Assembly);
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

        /// <summary>Loads a required asset, failing loudly when a build step before did not create it.</summary>
        public static T Require<T>(string relative) where T : Object
        {
            T asset = Load<T>(relative);
            if (asset == null)
            {
                throw new InvalidOperationException($"Monopoly: {Path(relative)} is missing; run Build Everything.");
            }
            return asset;
        }

        /// <summary>Writes <paramref name="bytes"/> unless the file already holds exactly them. Returns true when written.</summary>
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

        /// <summary>Copies a source file (a downloaded font or sound) into the package when it differs.</summary>
        public static bool CopyIfChanged(string sourceFile, string relative)
        {
            EnsureFolderOf(relative);
            bool written = WriteIfChanged(Path(relative), File.ReadAllBytes(sourceFile));
            if (written || AssetImporter.GetAtPath(Path(relative)) == null)
            {
                AssetDatabase.ImportAsset(Path(relative), ImportAssetOptions.ForceSynchronousImport);
            }
            return written;
        }

        /// <summary>Marks <paramref name="asset"/> dirty only when <paramref name="change"/> altered its serialized data.</summary>
        public static void ApplyIfChanged(Object asset, Action change)
        {
            string before = EditorJsonUtility.ToJson(asset);
            change();
            if (EditorJsonUtility.ToJson(asset) != before)
            {
                EditorUtility.SetDirty(asset);
            }
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

        /// <summary>Applies importer settings to a texture file of the package.</summary>
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
                throw new InvalidOperationException($"Monopoly: {path} is not a texture.");
            }
            string before = EditorJsonUtility.ToJson(importer);
            configure(importer);
            if (reimport || EditorJsonUtility.ToJson(importer) != before)
            {
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        public static Sprite LoadSprite(string relative)
        {
            return AssetDatabase.LoadAllAssetsAtPath(Path(relative)).OfType<Sprite>().FirstOrDefault();
        }

        /// <summary>Importer settings of a UI sprite; <paramref name="border"/> makes it nine-sliced.</summary>
        public static Action<TextureImporter> SpriteTexture(Vector4 border, bool mipmaps = false)
        {
            return importer =>
            {
                importer.textureShape = TextureImporterShape.Texture2D;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = mipmaps;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.spritePixelsPerUnit = 100f;
                importer.npotScale = TextureImporterNPOTScale.None;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.spriteBorder = border;
                settings.spriteAlignment = (int)SpriteAlignment.Center;
                importer.SetTextureSettings(settings);
            };
        }

        public static Action<TextureImporter> SpriteTexture()
        {
            return SpriteTexture(Vector4.zero);
        }

        /// <summary>Importer settings of a texture on a 3D surface (the board, the table, the dice).</summary>
        public static Action<TextureImporter> SurfaceTexture(bool repeat, int maxSize = 4096, bool compressed = true)
        {
            return importer =>
            {
                importer.textureShape = TextureImporterShape.Texture2D;
                importer.textureType = TextureImporterType.Default;
                importer.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Trilinear;
                importer.anisoLevel = 8;
                importer.mipmapEnabled = true;
                importer.sRGBTexture = true;
                importer.alphaIsTransparency = false;
                importer.maxTextureSize = maxSize;
                importer.textureCompression = compressed ? TextureImporterCompression.CompressedHQ : TextureImporterCompression.Uncompressed;
                importer.npotScale = TextureImporterNPOTScale.None;
            };
        }

        /// <summary>Importer settings of a transparent texture on a 3D quad (highlights, stamps, the logo).</summary>
        public static Action<TextureImporter> DecalTexture(int maxSize = 1024)
        {
            return importer =>
            {
                importer.textureShape = TextureImporterShape.Texture2D;
                importer.textureType = TextureImporterType.Default;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Trilinear;
                importer.mipmapEnabled = true;
                importer.alphaIsTransparency = true;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.maxTextureSize = maxSize;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.npotScale = TextureImporterNPOTScale.None;
            };
        }

        /// <summary>Writes a synthesised clip as a WAV file and imports it.</summary>
        public static AudioClip SaveAudio(byte[] wav, string relative, bool music)
        {
            EnsureFolderOf(relative);
            string path = Path(relative);
            bool written = WriteIfChanged(path, wav);
            return ConfigureAudio(relative, music, written);
        }

        /// <summary>Applies importer settings to an audio file of the package.</summary>
        public static AudioClip ConfigureAudio(string relative, bool music, bool reimport = false)
        {
            string path = Path(relative);
            if (reimport || AssetImporter.GetAtPath(path) == null)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Monopoly: {path} is not an audio clip.");
            }
            string before = EditorJsonUtility.ToJson(importer);
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = music ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = music ? 0.55f : 0.7f;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = !music;
            importer.loadInBackground = music;
            if (reimport || EditorJsonUtility.ToJson(importer) != before)
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
                // An asset of another script at the path (an older version) is replaced.
                if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                {
                    AssetDatabase.DeleteAsset(path);
                }
                asset = ScriptableObject.CreateInstance<T>();
                asset.name = System.IO.Path.GetFileNameWithoutExtension(relative);
                AssetDatabase.CreateAsset(asset, path);
            }
            ApplyIfChanged(asset, () => setup(asset));
            return asset;
        }

        /// <summary>Creates or updates a material asset; <paramref name="setup"/> sets its properties.</summary>
        public static Material SaveMaterial(string relative, Shader shader, Action<Material> setup)
        {
            if (shader == null)
            {
                throw new InvalidOperationException($"Monopoly: no shader for {relative}.");
            }
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

        /// <summary>Creates or updates a mesh asset in place (its GUID and the references to it are kept).</summary>
        public static Mesh SaveMesh(Mesh mesh, string relative)
        {
            EnsureFolderOf(relative);
            string path = Path(relative);
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            mesh.name = System.IO.Path.GetFileNameWithoutExtension(relative);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(mesh, path);
                return mesh;
            }
            if (Same(existing, mesh))
            {
                Object.DestroyImmediate(mesh);
                return existing;
            }
            EditorUtility.CopySerialized(mesh, existing);
            existing.name = mesh.name;
            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(mesh);
            return existing;
        }

        private static bool Same(Mesh a, Mesh b)
        {
            return a.vertexCount == b.vertexCount && a.vertices.SequenceEqual(b.vertices) && a.triangles.SequenceEqual(b.triangles) &&
                a.uv.SequenceEqual(b.uv) && a.colors32.Select(c => c.r | (c.g << 8) | (c.b << 16)).SequenceEqual(b.colors32.Select(c => c.r | (c.g << 8) | (c.b << 16)));
        }

        /// <summary>Saves <paramref name="rootObject"/> as a prefab (keeping the GUID of an existing one) and destroys it.</summary>
        public static GameObject SavePrefab(GameObject rootObject, string relative)
        {
            EnsureFolderOf(relative);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(rootObject, Path(relative));
            Object.DestroyImmediate(rootObject);
            return prefab;
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

        public static void SetFloat(Object target, string property, float value)
        {
            Set(target, property, p => p.floatValue = value);
        }

        public static void SetInt(Object target, string property, int value)
        {
            Set(target, property, p => p.intValue = value);
        }

        public static void SetBool(Object target, string property, bool value)
        {
            Set(target, property, p => p.boolValue = value);
        }

        public static void SetString(Object target, string property, string value)
        {
            Set(target, property, p => p.stringValue = value);
        }

        /// <summary>Deletes an asset or folder of the package when it exists.</summary>
        public static void Delete(string relative)
        {
            string path = Path(relative);
            if (AssetDatabase.IsValidFolder(path) || AssetDatabase.LoadMainAssetAtPath(path) != null || File.Exists(FullPath(path)))
            {
                AssetDatabase.DeleteAsset(path);
            }
        }
    }
}
