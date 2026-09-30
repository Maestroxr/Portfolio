using System;
using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Portfolio.Heroes.EditorTools
{
    /// <summary>
    /// Gives the material of a model the texture sheet its file names, when the importer left it without one. The packs
    /// keep their sheets in a Textures folder some levels above the models (KayKit/Textures) and a model names its sheet
    /// by file name alone, so it is up to Unity's own search to find it, and that search does not find it everywhere:
    /// with the game mounted as a package in BaseGame and imported for WebGL every model came out blank, and the towns
    /// and map objects that wear the materials of their models were drawn white. Here the sheet is looked up from the
    /// model's folder upwards, the same way in every project and for every build target.
    /// </summary>
    internal sealed class HeroesModelTextures : AssetPostprocessor
    {
        private const string Models = "Art/Models";
        private const string SheetFolder = "Textures";
        private static readonly string[] Extensions = { ".png", ".jpg", ".tga" };

        public override uint GetVersion()
        {
            return 1;
        }

        /// <summary>After the render pipeline's own, which makes the material from the description (order -980).</summary>
        public override int GetPostprocessOrder()
        {
            return 100;
        }

        public void OnPreprocessMaterialDescription(MaterialDescription description, Material material, AnimationClip[] clips)
        {
            string models = HeroesAssets.Path(Models) + "/";
            if (!assetPath.StartsWith(models, StringComparison.OrdinalIgnoreCase) || !material.HasProperty("_BaseMap")
                || material.GetTexture("_BaseMap") != null)
            {
                return;
            }
            string sheet = SheetOf(description, out TexturePropertyDescription property);
            if (sheet == null)
            {
                return;
            }
            // The sheet has to be imported before the model: without this a fresh library imports them in any order.
            context.DependsOnArtifact(sheet);
            var texture = AssetDatabase.LoadAssetAtPath<Texture>(sheet);
            if (texture == null)
            {
                return;
            }
            material.SetTexture("_BaseMap", texture);
            material.SetTextureOffset("_BaseMap", property.offset);
            material.SetTextureScale("_BaseMap", property.scale == Vector2.zero ? Vector2.one : property.scale);
            // What the pipeline does for a model whose sheet it found: the sheet is the color.
            Color color = Color.white;
            color.a = material.GetColor("_BaseColor").a;
            material.SetColor("_BaseColor", color);
        }

        /// <summary>The sheet the model names for its color, as an asset path, or null when it names none that is there.</summary>
        private string SheetOf(MaterialDescription description, out TexturePropertyDescription property)
        {
            if (!description.TryGetProperty("DiffuseColor", out property))
            {
                return null;
            }
            string named = string.IsNullOrEmpty(property.relativePath) ? property.path : property.relativePath;
            string file = string.IsNullOrEmpty(named) ? null : Path.GetFileName(named.Replace('\\', '/'));
            if (string.IsNullOrEmpty(file) || Array.IndexOf(Extensions, Path.GetExtension(file).ToLowerInvariant()) < 0)
            {
                return null;
            }
            string top = HeroesAssets.Path(Models);
            for (string folder = Folder(assetPath); folder.Length >= top.Length; folder = Folder(folder))
            {
                foreach (string candidate in new[] { $"{folder}/{file}", $"{folder}/{SheetFolder}/{file}" })
                {
                    if (File.Exists(HeroesAssets.FullPath(candidate)))
                    {
                        return candidate;
                    }
                }
            }
            return null;
        }

        private static string Folder(string path)
        {
            int slash = path.LastIndexOf('/');
            return slash > 0 ? path.Substring(0, slash) : "";
        }
    }
}
