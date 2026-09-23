using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Portfolio.Heroes.EditorTools
{
    /// <summary>
    /// Renders the built prefabs into contact sheets, so the art can be looked over without opening a scene: every
    /// creature, every map object, every town, side by side on the ground under the light of the map.
    /// </summary>
    internal static class HeroesShots
    {
        private const int Cell = 320;

        [MenuItem("Heroes/Contact Sheets", false, 40)]
        public static void All()
        {
            HeroesArt art = HeroesAssets.Require<HeroesArt>("Art/HeroesArt.asset");
            Sheet("units", art.units.Select(unit => (unit.prefab, unit.creature.ToString())).ToList(), 3.2f, 8);
            Sheet("heroes", art.heroes.SelectMany(hero => new[]
            {
                (hero.mount, hero.heroClass + " mount"), (hero.rider, hero.heroClass + " rider")
            }).ToList(), 3.2f, 6);
            Sheet("objects", Named(art.objects), 4.6f, 7);
            Sheet("towns", art.towns.SelectMany(town => town.byColor.Select((prefab, color) =>
                (prefab, $"{town.faction} {color}"))).ToList(), 7.5f, 5);
            Sheet("scenery", art.forestTrees.Concat(art.pineTrees).Concat(art.deadTrees).Concat(art.mountains)
                .Concat(art.rocks).Concat(art.waterPlants).Concat(art.flags).Concat(art.towers)
                .Where(prefab => prefab != null).Select(prefab => (prefab, prefab.name)).ToList(), 6.5f, 8);
        }

        private static List<(GameObject, string)> Named(List<HeroesArt.ObjectArt> objects)
        {
            var seen = new HashSet<GameObject>();
            var list = new List<(GameObject, string)>();
            foreach (HeroesArt.ObjectArt art in objects)
            {
                if (art.prefab != null && seen.Add(art.prefab))
                {
                    list.Add((art.prefab, art.prefab.name));
                }
            }
            return list;
        }

        private static void Sheet(string name, List<(GameObject prefab, string label)> items, float spacing, int columns)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.52f, 0.58f, 0.68f);
            RenderSettings.ambientEquatorColor = new Color(0.42f, 0.44f, 0.42f);
            RenderSettings.ambientGroundColor = new Color(0.25f, 0.22f, 0.18f);

            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.96f, 0.86f);
            sun.intensity = 1.5f;
            sun.transform.rotation = Quaternion.Euler(48f, 140f, 0f);
            sun.shadows = LightShadows.Soft;

            int rows = Mathf.CeilToInt(items.Count / (float)columns);
            var center = new Vector3((columns - 1) * spacing * 0.5f, 0f, -(rows - 1) * spacing * 0.5f);
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = new Vector3(columns * spacing * 0.2f, 1f, rows * spacing * 0.2f);
            ground.transform.position = center;
            ground.GetComponent<Renderer>().sharedMaterial = Ground();

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].prefab == null)
                {
                    continue;
                }
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(items[i].prefab);
                instance.transform.position = new Vector3(i % columns * spacing, 0f, -(i / columns) * spacing);
                instance.transform.rotation = Quaternion.Euler(0f, 205f, 0f);
                // The clips do not play in the editor, so the models stand in their bind pose.
            }

            // The camera looks down at the same slant the map is played at, so the sheet shows what the game shows.
            const float pitch = 50f;
            float slant = Mathf.Sin(pitch * Mathf.Deg2Rad);
            float worldWidth = columns * spacing;
            float worldHeight = rows * spacing * slant + 6f * Mathf.Cos(pitch * Mathf.Deg2Rad);
            var camera = new GameObject("Camera").AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = worldHeight * 0.5f;
            camera.aspect = worldWidth / worldHeight;
            camera.transform.rotation = Quaternion.Euler(pitch, 0f, 0f);
            camera.transform.position = center + Quaternion.Euler(pitch, 0f, 0f) * Vector3.back * 90f + Vector3.up * 1.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.09f, 0.12f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 400f;

            int width = columns * Cell;
            int height = Mathf.Max(Cell / 2, Mathf.RoundToInt(width / camera.aspect));
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            camera.targetTexture = target;
            // The first frame of a batch session draws before the shaders are ready, so one is thrown away.
            camera.Render();
            camera.Render();

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;
            camera.targetTexture = null;

            string folder = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "shots");
            Directory.CreateDirectory(folder);
            string file = Path.Combine(folder, name + ".png");
            File.WriteAllBytes(file, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            target.Release();
            Object.DestroyImmediate(target);
            Debug.Log($"Heroes: {file} ({items.Count} in {columns}x{rows})");
        }

        private static Material Ground()
        {
            return HeroesAssets.Material("Art/Generated/Materials/ShotGround.mat", Shader.Find("Universal Render Pipeline/Lit"), m =>
            {
                m.SetColor("_BaseColor", new Color(0.30f, 0.35f, 0.22f));
                m.SetFloat("_Smoothness", 0.05f);
            });
        }
    }
}
