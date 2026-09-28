using System.Collections.Generic;
using Gamebox;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Portfolio.Asteroids.EditorTools
{
    /// <summary>
    /// The keys by which the themed parts of the prefabs and the scene find their look in an <see cref="AsteroidsTheme"/>:
    /// the path of the asset the part was built with, relative to Art/ and without its extension ("Materials/Rock",
    /// "Strike/Materials/AirCrimson", "StarSparrow/Materials/StarSparrow Blue", "Models/Saucer", "Icons/Star"). The Classic
    /// theme lists every key with the asset itself; another theme lists its own asset under the same key. <see cref="Mark"/>
    /// puts the generic <see cref="ThemedRenderer"/> and <see cref="ThemedMesh"/> on a built object.
    /// </summary>
    internal static class ThemeKeys
    {
        /// <summary>The key of <paramref name="material"/>, or null for one outside the module's art (a font material, a built-in).</summary>
        public static string MaterialKey(Material material)
        {
            if (material == null || material.shader != null && material.shader.name.StartsWith("TextMeshPro", System.StringComparison.Ordinal))
            {
                return null;
            }
            return Key(material);
        }


        /// <summary>The key of <paramref name="mesh"/>, or null for a built-in or a mesh outside the module's art. A mesh of a model file carries its name.</summary>
        public static string MeshKey(Mesh mesh)
        {
            if (mesh == null)
            {
                return null;
            }
            string key = Key(mesh);
            if (key == null)
            {
                return null;
            }
            string path = AssetDatabase.GetAssetPath(mesh);
            return AssetDatabase.IsMainAsset(mesh) || path.EndsWith(".asset", System.StringComparison.OrdinalIgnoreCase) ? key : $"{key}#{mesh.name}";
        }


        /// <summary>The key of <paramref name="sprite"/> (its texture's path), or null for one outside the module's art.</summary>
        public static string SpriteKey(Sprite sprite)
        {
            return sprite != null ? Key(sprite) : null;
        }


        /// <summary>The key of a texture (the texture of a sprite, the icon a material shows), or null.</summary>
        public static string TextureKey(Texture texture)
        {
            return texture != null ? Key(texture) : null;
        }


        private static string Key(Object asset)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            string root = AsteroidsAssets.Root + "/Art/";
            if (string.IsNullOrEmpty(path) || !path.StartsWith(root, System.StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            string relative = path.Substring(root.Length);
            if (relative.StartsWith("Themes/", System.StringComparison.OrdinalIgnoreCase))
            {
                // The art of a generated theme is never re-themed.
                return null;
            }
            int dot = relative.LastIndexOf('.');
            return dot > relative.LastIndexOf('/') ? relative.Substring(0, dot) : relative;
        }


        /// <summary>
        /// Puts a <see cref="ThemedRenderer"/> on every renderer under <paramref name="root"/> for each of its materials that
        /// has a key, and a <see cref="ThemedMesh"/> on every mesh filter whose mesh has one, so a theme can replace them.
        /// Texts keep their font materials (a theme changes those through its fonts), the ship's hull and the shape of a
        /// rock are set by their own code from the theme's typed looks. Returns the keys found (materials, then meshes).
        /// </summary>
        public static void Mark(GameObject root, List<string> materialKeys = null, List<string> meshKeys = null)
        {
            StripMissingScripts(root);
            var asteroid = root.GetComponent<Asteroid>();
            var ship = root.GetComponent<ShipVisuals>();
            // The ship's drop shadow copies its hull, which ShipVisuals sets from the theme's look of the hull.
            Transform shipShadow = ship != null ? ship.transform.Find("Shadow/Flat/Model") : null;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.GetComponent<TMP_Text>() != null)
                {
                    continue;
                }
                Material[] materials = renderer.sharedMaterials;
                for (int slot = 0; slot < materials.Length; slot++)
                {
                    string key = MaterialKey(materials[slot]);
                    if (key == null)
                    {
                        continue;
                    }
                    var themed = renderer.gameObject.AddComponent<ThemedRenderer>();
                    themed.MaterialKey = key;
                    themed.Slot = slot;
                    if (materialKeys != null && !materialKeys.Contains(key))
                    {
                        materialKeys.Add(key);
                    }
                }
            }
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (asteroid != null && filter == asteroid.meshFilter || ship != null && filter == ship.modelFilter || shipShadow != null && filter.transform == shipShadow)
                {
                    continue;
                }
                string key = MeshKey(filter.sharedMesh);
                if (key == null)
                {
                    continue;
                }
                var themed = filter.gameObject.AddComponent<ThemedMesh>();
                themed.MeshKey = key;
                if (meshKeys != null && !meshKeys.Contains(key))
                {
                    meshKeys.Add(key);
                }
            }
        }


        /// <summary>Removes the components whose script is missing from <paramref name="root"/> and everything under it (a part copied from an older build).</summary>
        public static void StripMissingScripts(GameObject root)
        {
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            {
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(transform.gameObject);
            }
        }
    }
}
