using System;
using Gamebox.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Portfolio.Checkers.EditorTools
{
    /// <summary>
    /// The asset writer of the Checkers package: BaseGame's shared <see cref="GameAssets"/> rooted at this package, with
    /// thin forwarders so the builders read like Monopoly's. Everything is written in place and only when it changed,
    /// so GUIDs survive a rebuild.
    /// </summary>
    internal static class CheckersAssets
    {
        public static readonly GameAssets Assets = new GameAssets(typeof(CheckersAssets).Assembly);

        public static string Root => Assets.Root;

        public static string Path(string relative)
        {
            return Assets.Path(relative);
        }

        public static void EnsureFolder(string relativeFolder)
        {
            Assets.EnsureFolder(relativeFolder);
        }

        public static T Load<T>(string relative) where T : Object
        {
            return Assets.Load<T>(relative);
        }

        public static T Require<T>(string relative) where T : Object
        {
            return Assets.Require<T>(relative);
        }

        public static Texture2D SaveTexture(Texture2D texture, string relative, Action<TextureImporter> configure)
        {
            return Assets.SaveTexture(texture, relative, configure);
        }

        public static Sprite LoadSprite(string relative)
        {
            return Assets.LoadSprite(relative);
        }

        public static AudioClip SaveAudio(byte[] wav, string relative, bool music)
        {
            return Assets.SaveAudio(wav, relative, music);
        }

        public static T SaveScriptable<T>(string relative, Action<T> setup) where T : ScriptableObject
        {
            return Assets.SaveScriptable(relative, setup);
        }

        public static Material SaveMaterial(string relative, Shader shader, Action<Material> setup)
        {
            return Assets.SaveMaterial(relative, shader, setup);
        }

        public static Mesh SaveMesh(Mesh mesh, string relative)
        {
            return Assets.SaveMesh(mesh, relative);
        }

        public static GameObject SavePrefab(GameObject rootObject, string relative)
        {
            return Assets.SavePrefab(rootObject, relative);
        }

        public static void ApplyIfChanged(Object asset, Action change)
        {
            GameAssets.ApplyIfChanged(asset, change);
        }

        public static void Set(Object target, string property, Action<SerializedProperty> assign)
        {
            GameAssets.Set(target, property, assign);
        }

        public static void SetObject(Object target, string property, Object value)
        {
            GameAssets.SetObject(target, property, value);
        }

        public static void SetObjects(Object target, string property, params Object[] values)
        {
            GameAssets.SetObjects(target, property, values);
        }

        public static void SetFloat(Object target, string property, float value)
        {
            GameAssets.SetFloat(target, property, value);
        }

        public static void SetInt(Object target, string property, int value)
        {
            GameAssets.SetInt(target, property, value);
        }

        public static void SetBool(Object target, string property, bool value)
        {
            GameAssets.SetBool(target, property, value);
        }

        public static void SetString(Object target, string property, string value)
        {
            GameAssets.SetString(target, property, value);
        }

        public static void Delete(string relative)
        {
            Assets.Delete(relative);
        }
    }
}
