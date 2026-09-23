using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Portfolio.Heroes.EditorTools
{
    /// <summary>
    /// Batch mode helper: writes what the importer made of every model under Art/Models (size, pivot, clips, materials),
    /// for tuning the art builder without an editor window. <c>-executeMethod Portfolio.Heroes.EditorTools.HeroesInspect.Models -heroesOut file</c>.
    /// </summary>
    internal static class HeroesInspect
    {
        public static void Models()
        {
            string output = Argument("-heroesOut") ?? "models.txt";
            var text = new StringBuilder();
            string root = HeroesAssets.Path("Art/Models");
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { root }).OrderBy(g => AssetDatabase.GUIDToAssetPath(g)))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (importer == null || model == null)
                {
                    continue;
                }
                GameObject instance = UnityEngine.Object.Instantiate(model);
                Bounds bounds = new Bounds(instance.transform.position, Vector3.zero);
                bool first = true;
                foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
                {
                    if (first)
                    {
                        bounds = renderer.bounds;
                        first = false;
                    }
                    else
                    {
                        bounds.Encapsulate(renderer.bounds);
                    }
                }
                string clips = string.Join(",", AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                    .Where(c => !c.name.StartsWith("__preview__")).Select(c => $"{c.name}:{c.length:0.00}"));
                string materials = string.Join(",", instance.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials)
                    .Where(m => m != null).Select(m => m.name).Distinct());
                string children = string.Join(",", instance.transform.Cast<Transform>().Select(t => t.name));
                text.AppendLine($"{path.Substring(root.Length + 1)} | anim {importer.animationType} | scale {importer.globalScale} file {importer.useFileScale} | size {bounds.size.x:0.00}x{bounds.size.y:0.00}x{bounds.size.z:0.00} | center {bounds.center.x:0.00},{bounds.center.y:0.00},{bounds.center.z:0.00} | renderers {instance.GetComponentsInChildren<Renderer>(true).Length} skinned {instance.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length} | mats {materials} | children {children} | clips {clips}");
                UnityEngine.Object.DestroyImmediate(instance);
            }
            File.WriteAllText(output, text.ToString());
            Debug.Log($"HeroesInspect: {output}");
        }

        internal static string Argument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name)
                {
                    return args[i + 1];
                }
            }
            return null;
        }
    }
}
