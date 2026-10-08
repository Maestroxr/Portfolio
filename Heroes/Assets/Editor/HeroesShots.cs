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
    /// creature, every map object, every town, side by side on the ground under the light of the map. The creatures and
    /// heroes stand in their idle clips (the riders in their saddles); the sheets of a theme other than the classic one
    /// are written with its name in front (grim-units.png).
    /// </summary>
    internal static class HeroesShots
    {
        private const int Cell = 320;

        [MenuItem("Heroes/Contact Sheets", false, 40)]
        public static void All()
        {
            Run(HeroesThemeSpec.Classic, "");
        }

        [MenuItem("Heroes/Contact Sheets (Grim Realm)", false, 40)]
        public static void Grim()
        {
            Run(HeroesThemeSpec.GrimRealm, "grim-");
        }

        private static void Run(HeroesThemeSpec spec, string prefix)
        {
            // The sheets are laid out in scenes of their own, so the open scene is saved first if the user wants, and
            // opened again afterwards.
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }
            string open = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
            try
            {
                Sheets(HeroesAssets.Require<HeroesArt>(spec.ArtAsset), prefix);
            }
            finally
            {
                if (!string.IsNullOrEmpty(open))
                {
                    EditorSceneManager.OpenScene(open, OpenSceneMode.Single);
                }
            }
        }

        private static void Sheets(HeroesArt art, string prefix)
        {
            Sheet(prefix + "units", art.units.Select(unit => (unit.prefab, unit.creature.ToString(), unit.idle)).ToList(), 3.2f, 8);
            Sheet(prefix + "heroes", art.heroes.Select(hero => (Mounted(hero), hero.heroClass.ToString(), hero.mountIdle)).ToList(), 4f, 6);
            Sheet(prefix + "objects", Named(art.objects), 4.6f, 7);
            Sheet(prefix + "towns", art.towns.SelectMany(town => town.byColor.Select((prefab, color) =>
                (prefab, $"{town.faction} {color}", ""))).ToList(), 7.5f, 5);
            Sheet(prefix + "scenery", art.forestTrees.Concat(art.pineTrees).Concat(art.deadTrees).Concat(art.mountains)
                .Concat(art.rocks).Concat(art.waterPlants).Concat(art.flags).Concat(art.towers)
                .Where(prefab => prefab != null).Select(prefab => (prefab, prefab.name, "")).ToList(), 6.5f, 8);
        }

        /// <summary>A hero as the map shows him, put together for the sheet: the mount, and the rider in its saddle.</summary>
        private static GameObject Mounted(HeroesArt.HeroArt hero)
        {
            return hero.mount;
        }

        private static List<(GameObject, string, string)> Named(List<HeroesArt.ObjectArt> objects)
        {
            var seen = new HashSet<GameObject>();
            var list = new List<(GameObject, string, string)>();
            foreach (HeroesArt.ObjectArt art in objects)
            {
                if (art.prefab != null && seen.Add(art.prefab))
                {
                    list.Add((art.prefab, art.prefab.name, ""));
                }
            }
            return list;
        }

        private static void Sheet(string name, List<(GameObject prefab, string label, string idle)> items, float spacing, int columns)
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
                // The clips do not play in the editor: the models are posed in their idle a third of the way in.
                if (!string.IsNullOrEmpty(items[i].idle))
                {
                    HeroesPortraits.Pose(instance, items[i].idle);
                }
            }
            if (name.EndsWith("heroes"))
            {
                Riders(items);
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

        /// <summary>Seats the rider of every hero on the mount standing in the sheet, as the map does.</summary>
        private static void Riders(List<(GameObject prefab, string label, string idle)> items)
        {
            HeroesArt art = null;
            foreach (string guid in AssetDatabase.FindAssets("t:HeroesArt"))
            {
                var candidate = AssetDatabase.LoadAssetAtPath<HeroesArt>(AssetDatabase.GUIDToAssetPath(guid));
                if (candidate != null && candidate.heroes.Any(hero => items.Any(item => item.prefab == hero.mount)))
                {
                    art = candidate;
                    break;
                }
            }
            if (art == null)
            {
                return;
            }
            foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(root);
                HeroesArt.HeroArt hero = art.heroes.FirstOrDefault(candidate => candidate.mount == source);
                if (hero == null || hero.rider == null)
                {
                    continue;
                }
                var seat = new GameObject("Seat");
                seat.transform.SetParent(root.transform, false);
                seat.transform.localPosition = hero.seat;
                var rider = (GameObject)PrefabUtility.InstantiatePrefab(hero.rider, seat.transform);
                HeroesPortraits.Pose(rider, hero.riderPose, 0f);
            }
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
