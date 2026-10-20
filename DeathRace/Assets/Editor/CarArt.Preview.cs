using System;
using System.Collections.Generic;
using System.IO;
using Gamebox.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace Portfolio.DeathRace.EditorTools
{
    /// <summary>
    /// A small stage for looking at the cars, items and effects the way the race shows them: a stretch of road under
    /// the look's sun (or night sky), a camera about sixty degrees above, and a PNG of what it sees. Nothing it makes
    /// is saved; it runs in a fresh empty scene.
    /// </summary>
    internal sealed class PreviewStage : IDisposable
    {
        private readonly List<Object> made = new List<Object>();
        private readonly GameObject root;
        private readonly Camera camera;
        private readonly bool night;
        private bool warm;

        public PreviewStage(bool night)
        {
            this.night = night;
            root = new GameObject("Preview Stage");
            made.Add(root);
            DeathRaceTheme look = DeathRaceAssets.Load<DeathRaceTheme>(night ? DeathRaceThemeBuilder.NightfallPath : DeathRaceThemeBuilder.ScorchedPath);
            DeathRaceTheme defaults = ScriptableObject.CreateInstance<DeathRaceTheme>();
            made.Add(defaults);
            DeathRaceTheme light = look != null && look.sunIntensity > 0f ? look : defaults;

            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.transform.SetParent(root.transform, false);
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            if (night && (look == null || !look.night))
            {
                sun.color = new Color(0.55f, 0.62f, 0.9f);
                sun.intensity = 0.28f;
                sun.transform.rotation = Quaternion.Euler(58f, 30f, 0f);
            }
            else
            {
                sun.color = light.sunColor;
                sun.intensity = light.sunIntensity;
                sun.transform.rotation = Quaternion.Euler(light.sunEuler);
            }
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            if (night && (look == null || !look.night))
            {
                RenderSettings.ambientSkyColor = new Color(0.12f, 0.14f, 0.24f);
                RenderSettings.ambientEquatorColor = new Color(0.1f, 0.09f, 0.14f);
                RenderSettings.ambientGroundColor = new Color(0.05f, 0.05f, 0.07f);
            }
            else
            {
                RenderSettings.ambientSkyColor = light.ambientSky;
                RenderSettings.ambientEquatorColor = light.ambientEquator;
                RenderSettings.ambientGroundColor = light.ambientGround;
            }
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.fog = false;

            Color asphalt = night ? new Color(0.09f, 0.095f, 0.11f) : new Color(0.3f, 0.29f, 0.28f);
            Color ground = night ? new Color(0.07f, 0.07f, 0.08f) : new Color(0.62f, 0.5f, 0.36f);
            Slab("Ground", new Vector3(0f, -0.06f, 0f), new Vector3(400f, 0.1f, 400f), ground, 0.05f);
            Slab("Road", new Vector3(0f, -0.02f, 0f), new Vector3(400f, 0.04f, 24f), asphalt, night ? 0.55f : 0.15f);
            for (float x = -200f; x < 200f; x += 6f)
            {
                Slab("Line", new Vector3(x, 0.001f, 0f), new Vector3(3f, 0.002f, 0.25f), night ? new Color(0.45f, 0.45f, 0.4f) : new Color(0.85f, 0.83f, 0.75f), 0.2f);
            }

            var cameraObject = new GameObject("Camera");
            cameraObject.transform.SetParent(root.transform, false);
            camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = night ? new Color(0.03f, 0.03f, 0.05f) : new Color(0.62f, 0.5f, 0.36f);
            camera.allowHDR = true;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 600f;
            UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.renderShadows = true;

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            made.Add(profile);
            VolumeProfile source = look != null ? look.volume : null;
            if (source != null)
            {
                foreach (VolumeComponent component in source.components)
                {
                    VolumeComponent copy = Object.Instantiate(component);
                    made.Add(copy);
                    profile.components.Add(copy);
                }
            }
            else
            {
                Bloom bloom = profile.Add<Bloom>(true);
                bloom.intensity.Override(night ? 1.1f : 0.5f);
                bloom.threshold.Override(night ? 0.8f : 1f);
                bloom.scatter.Override(0.7f);
                Tonemapping tonemapping = profile.Add<Tonemapping>(true);
                tonemapping.mode.Override(TonemappingMode.Neutral);
            }
            var volume = root.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
        }

        /// <summary>A box of one colour (the ground, the road, the lines).</summary>
        public GameObject Slab(string name, Vector3 center, Vector3 size, Color color, float smoothness)
        {
            GameObject slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab.name = name;
            Object.DestroyImmediate(slab.GetComponent<Collider>());
            slab.transform.SetParent(root.transform, false);
            slab.transform.localPosition = center;
            slab.transform.localScale = size;
            var material = new Material(CarArt.Lit);
            CarArt.Surface(material, color, null, smoothness, 0f);
            made.Add(material);
            slab.GetComponent<MeshRenderer>().sharedMaterial = material;
            return slab;
        }

        /// <summary>Places a copy of a prefab on the stage.</summary>
        public GameObject Place(GameObject prefab, Vector3 position, float yaw)
        {
            var copy = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (copy == null)
            {
                copy = Object.Instantiate(prefab);
            }
            copy.transform.SetParent(root.transform, false);
            copy.transform.localPosition = position;
            copy.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            return copy;
        }

        /// <summary>
        /// Renders the stage looking at <paramref name="target"/> from <paramref name="distance"/> metres at
        /// <paramref name="pitch"/> degrees (north up, like the race camera) into a PNG.
        /// </summary>
        public void Shoot(string file, Vector3 target, float distance, float pitch = 60f, float fieldOfView = 40f, int width = 1600, int height = 900,
            Action<float> advance = null, float time = 0f)
        {
            camera.fieldOfView = fieldOfView;
            camera.aspect = width / (float)height;
            camera.transform.rotation = Quaternion.Euler(pitch, 0f, 0f);
            camera.transform.position = target - camera.transform.forward * distance;
            var target2 = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
            try
            {
                camera.targetTexture = target2;
                if (!warm)
                {
                    camera.Render();
                    warm = true;
                }
                advance?.Invoke(time);
                camera.Render();
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = target2;
                var read = new Texture2D(width, height, TextureFormat.RGB24, false);
                read.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                read.Apply();
                RenderTexture.active = previous;
                File.WriteAllBytes(file, read.EncodeToPNG());
                Object.DestroyImmediate(read);
            }
            finally
            {
                camera.targetTexture = null;
                target2.Release();
                Object.DestroyImmediate(target2);
            }
        }

        public void Dispose()
        {
            foreach (Object thing in made)
            {
                if (thing != null)
                {
                    Object.DestroyImmediate(thing);
                }
            }
        }
    }

    internal static partial class CarArt
    {
        private static readonly Color[] PreviewPaints =
        {
            new Color(0.86f, 0.14f, 0.12f), new Color(0.15f, 0.42f, 0.9f), new Color(0.98f, 0.78f, 0.1f),
            new Color(0.2f, 0.66f, 0.3f), new Color(0.92f, 0.92f, 0.9f), new Color(0.56f, 0.24f, 0.8f)
        };

        private static string Argument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++)
            {
                if (args[i] == name)
                {
                    return args[i + 1];
                }
            }
            return null;
        }

        private static bool Flag(string name)
        {
            return Array.IndexOf(Environment.GetCommandLineArgs(), name) >= 0;
        }

        /// <summary>
        /// Batch entry (<c>-executeMethod Portfolio.DeathRace.EditorTools.CarArt.ContactSheet -artcars-out &lt;folder&gt;</c>,
        /// <c>-artcars-nobuild</c> to skip the rebuild): builds the car, item and effect art and prefabs, then renders
        /// them into PNGs: the cars by day and by night as the race camera sees them, six to a sheet close up, at race
        /// height and from the side, and all twelve together at race height; their states (braking, nitro, damaged,
        /// wrecked), the pictures of the garage, the items and frames of the effects.
        /// </summary>
        public static void ContactSheet()
        {
            string folder = Argument("-artcars-out") ?? Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? ".", "Temp", "ArtCars");
            Directory.CreateDirectory(folder);
            if (!Flag("-artcars-nobuild"))
            {
                DeathRacePrefabBuilder.BuildArt();
                DeathRacePrefabBuilder.BuildAll();
                AssetDatabase.SaveAssets();
            }
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            foreach (bool night in new[] { false, true })
            {
                DeathRaceTheme theme = PreviewTheme(night);
                try
                {
                    for (int page = 0; page < 2; page++)
                    {
                        using (var stage = new PreviewStage(night))
                        {
                            CarLineup(stage, theme, folder, night, page);
                        }
                        using (var stage = new PreviewStage(night))
                        {
                            CarStates(stage, theme, folder, night, page);
                        }
                    }
                    using (var stage = new PreviewStage(night))
                    {
                        Field(stage, theme, folder, night);
                    }
                    using (var stage = new PreviewStage(night))
                    {
                        Items(stage, folder, night);
                    }
                    using (var stage = new PreviewStage(night))
                    {
                        Effects(stage, folder, night, 0);
                    }
                    using (var stage = new PreviewStage(night))
                    {
                        Effects(stage, folder, night, 1);
                    }
                }
                finally
                {
                    Object.DestroyImmediate(theme);
                }
            }
            PictureSheet(folder);
            Debug.Log($"ART-CARS contact sheets written to {folder}");
        }

        /// <summary>
        /// Six cars (page 0 the first six, page 1 the next six) in a row of three by two: close up, at race height and
        /// from low at the side. The second page's files are named cars2-.
        /// </summary>
        private static void CarLineup(PreviewStage stage, DeathRaceTheme theme, string folder, bool night, int page)
        {
            float[] yaws = { 0f, 35f, 180f, 270f, 140f, 90f };
            for (int i = 0; i < 6; i++)
            {
                GameObject prefab = DeathRacePrefabBuilder.CarPrefab((CarModel)(page * 6 + i));
                if (prefab == null)
                {
                    continue;
                }
                var position = new Vector3((i % 3 - 1) * 7.5f, 0f, (i < 3 ? 4.5f : -4.5f));
                GameObject car = stage.Place(prefab, position, yaws[i]);
                car.GetComponent<CarModelView>().ApplyTheme(theme, PreviewPaints[i]);
            }
            string look = Look(night);
            string sheet = page == 0 ? "cars" : "cars2";
            stage.Shoot(Path.Combine(folder, $"{sheet}-{look}-close.png"), Vector3.zero, 26f, 60f, 40f);
            stage.Shoot(Path.Combine(folder, $"{sheet}-{look}-race.png"), Vector3.zero, 44f, 60f, 40f);
            stage.Shoot(Path.Combine(folder, $"{sheet}-{look}-side.png"), Vector3.zero, 22f, 30f, 40f);
        }

        /// <summary>All the cars in three rows of four, heading the same way, at race height: how they compare in a race.</summary>
        private static void Field(PreviewStage stage, DeathRaceTheme theme, string folder, bool night)
        {
            for (int i = 0; i < CarSpecs.Count; i++)
            {
                GameObject prefab = DeathRacePrefabBuilder.CarPrefab((CarModel)i);
                if (prefab == null)
                {
                    continue;
                }
                var position = new Vector3((i % 4 - 1.5f) * 6.5f, 0f, (1 - i / 4) * 8f);
                GameObject car = stage.Place(prefab, position, 0f);
                car.GetComponent<CarModelView>().ApplyTheme(theme, PreviewPaints[(i + i / 4) % PreviewPaints.Length]);
            }
            stage.Shoot(Path.Combine(folder, $"field-{Look(night)}.png"), Vector3.zero, 44f, 60f, 40f);
        }

        /// <summary>One car per state: as new, braking, nitro, damaged, badly damaged, wrecked (page 1: the next six cars).</summary>
        private static void CarStates(PreviewStage stage, DeathRaceTheme theme, string folder, bool night, int page)
        {
            // The second page puts the Apocalypse's jets in the nitro slot.
            string[] states = page == 0
                ? new[] { "new", "braking", "nitro", "damaged", "battered", "wrecked" }
                : new[] { "new", "braking", "wrecked", "damaged", "battered", "nitro" };
            for (int i = 0; i < states.Length; i++)
            {
                GameObject prefab = DeathRacePrefabBuilder.CarPrefab((CarModel)((page * 6 + i) % CarSpecs.Count));
                if (prefab == null)
                {
                    continue;
                }
                var position = new Vector3((i % 3 - 1) * 7.5f, 0f, (i < 3 ? 4.5f : -4.5f));
                GameObject car = stage.Place(prefab, position, 200f);
                var view = car.GetComponent<CarModelView>();
                view.ApplyTheme(theme, PreviewPaints[(i + 2) % PreviewPaints.Length]);
                view.SetWheels(18f, 40f);
                switch (states[i])
                {
                    case "braking": view.SetBraking(true); break;
                    case "nitro": view.SetNitro(true); break;
                    case "damaged": view.SetDamage(0.5f); break;
                    case "battered": view.SetDamage(0.1f); break;
                    case "wrecked": view.SetWrecked(true); break;
                }
            }
            stage.Shoot(Path.Combine(folder, $"{(page == 0 ? "states" : "states2")}-{Look(night)}.png"), Vector3.zero, 26f, 60f, 40f);
        }

        /// <summary>The mine, the rocket and the four pickups in a row, close up and at race height.</summary>
        private static void Items(PreviewStage stage, string folder, bool night)
        {
            string[] names = { "Mine", "Rocket", "Cash", "Repair", "Ammo", "Nitro" };
            for (int i = 0; i < names.Length; i++)
            {
                GameObject prefab = DeathRacePrefabBuilder.ItemPrefab(names[i]);
                if (prefab == null)
                {
                    continue;
                }
                bool rocket = names[i] == "Rocket";
                GameObject item = stage.Place(prefab, new Vector3((i - 2.5f) * 3.2f, rocket ? 0.8f : 0f, 0f), rocket ? 60f : 20f);
                Transform model = item.transform.Find("Item");
                if (model != null)
                {
                    model.localRotation = Quaternion.Euler(0f, 25f, 0f);
                }
            }
            stage.Shoot(Path.Combine(folder, $"items-{Look(night)}-close.png"), Vector3.zero, 13f, 60f, 40f, 1600, 600);
            stage.Shoot(Path.Combine(folder, $"items-{Look(night)}-race.png"), Vector3.zero, 44f, 60f, 40f, 1600, 900);
        }

        /// <summary>
        /// Frames of the effects, each simulated to a moment worth seeing: the explosion early and late, the wreck's
        /// blast, sparks, the muzzle flash, the smokes and the fire in full flow, the nitro jet, dust and a scorch mark.
        /// </summary>
        private static void Effects(PreviewStage stage, string folder, bool night, int sheet)
        {
            (string name, float time, float yaw)[] frames = sheet == 0
                ? new[] { ("Explosion", 0.08f, 0f), ("Explosion", 0.35f, 0f), ("BigExplosion", 0.25f, 0f), ("BigExplosion", 1.2f, 0f), ("Scorch", 0f, 0f), ("Sparks", 0.07f, 90f) }
                : new[] { ("MuzzleFlash", 0.02f, 0f), ("Smoke", 2.5f, 0f), ("BlackSmoke", 2.5f, 0f), ("Fire", 2.5f, 0f), ("NitroFlame", 0.6f, 180f), ("Dust", 0.35f, 0f) };
            for (int i = 0; i < frames.Length; i++)
            {
                GameObject prefab = DeathRaceEffectsBuilder.Prefab(frames[i].name);
                if (prefab == null)
                {
                    continue;
                }
                var position = new Vector3((i % 3 - 1) * 9f, 0.4f, i < 3 ? 5f : -5f);
                GameObject effect = stage.Place(prefab, position, frames[i].yaw);
                if (frames[i].name == "Scorch")
                {
                    effect.transform.localScale = Vector3.one * 5f;
                    effect.transform.localPosition = new Vector3(position.x, 0f, position.z);
                }
                foreach (ParticleSystem system in effect.GetComponentsInChildren<ParticleSystem>())
                {
                    if (system.gameObject.name.IndexOf("Tint", StringComparison.Ordinal) >= 0)
                    {
                        // What the race does: the theme's colour for the effect.
                        ParticleSystem.MainModule main = system.main;
                        main.startColor = TintOf(frames[i].name, night);
                    }
                    system.Simulate(frames[i].time, false, true, false);
                }
                var flash = effect.GetComponentInChildren<Light>(true);
                if (flash != null && frames[i].time < 0.2f)
                {
                    flash.enabled = true;
                }
            }
            stage.Shoot(Path.Combine(folder, $"effects{sheet + 1}-{Look(night)}.png"), Vector3.zero, 30f, 60f, 40f);
        }

        /// <summary>The theme colour the race gives an effect's "Tint" systems (see <see cref="Dress"/>).</summary>
        private static Color TintOf(string effect, bool night)
        {
            DeathRaceTheme look = ScriptableObject.CreateInstance<DeathRaceTheme>();
            try
            {
                Dress(look, night);
                switch (effect)
                {
                    case "Sparks": return look.sparks;
                    case "MuzzleFlash": return look.muzzle;
                    case "Dust": return night ? new Color(0.55f, 0.56f, 0.6f) : new Color(0.82f, 0.7f, 0.52f);
                    default: return look.explosion;
                }
            }
            finally
            {
                Object.DestroyImmediate(look);
            }
        }

        /// <summary>
        /// The garage pictures of both looks on one sheet, six to a row (the day rows on top, the night rows below), over a
        /// grey check so the transparency shows.
        /// </summary>
        private static void PictureSheet(string folder)
        {
            const int cell = 256;
            const int cellHeight = 192;
            const int columns = 6;
            int rowsPerLook = (CarSpecs.Count + columns - 1) / columns;
            var sheet = new Texture2D(cell * columns, cellHeight * rowsPerLook * 2, TextureFormat.RGBA32, false);
            var pixels = new Color[sheet.width * sheet.height];
            for (int i = 0; i < pixels.Length; i++)
            {
                int x = i % sheet.width / 16;
                int y = i / sheet.width / 16;
                pixels[i] = (x + y) % 2 == 0 ? new Color(0.32f, 0.32f, 0.34f) : new Color(0.4f, 0.4f, 0.42f);
            }
            for (int look = 0; look < 2; look++)
            {
                for (int i = 0; i < CarSpecs.Count; i++)
                {
                    Sprite sprite = Picture((CarModel)i, look == 1);
                    if (sprite == null)
                    {
                        continue;
                    }
                    // Rows count from the bottom of the texture: the day rows go on top.
                    int row = (1 - look) * rowsPerLook + (rowsPerLook - 1 - i / columns);
                    Texture2D texture = Readable(sprite.texture);
                    for (int y = 0; y < cellHeight; y++)
                    {
                        for (int x = 0; x < cell; x++)
                        {
                            Color c = texture.GetPixelBilinear((x + 0.5f) / cell, (y + 0.5f) / cellHeight);
                            int index = (row * cellHeight + y) * sheet.width + i % columns * cell + x;
                            pixels[index] = Color.Lerp(pixels[index], new Color(c.r, c.g, c.b, 1f), c.a);
                        }
                    }
                    Object.DestroyImmediate(texture);
                }
            }
            sheet.SetPixels(pixels);
            sheet.Apply();
            File.WriteAllBytes(Path.Combine(folder, "pictures.png"), sheet.EncodeToPNG());
            Object.DestroyImmediate(sheet);
        }

        /// <summary>A CPU copy of a texture (imported textures are not readable).</summary>
        private static Texture2D Readable(Texture2D texture)
        {
            RenderTexture temporary = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(texture, temporary);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = temporary;
            var copy = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
            copy.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            copy.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(temporary);
            return copy;
        }
    }
}
