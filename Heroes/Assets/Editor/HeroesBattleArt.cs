using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gamebox.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Portfolio.Heroes.EditorTools
{
    /// <summary>
    /// The art of the separate battlefield: a sky for every ground (Poly Haven pure skies on Skybox/Panoramic materials,
    /// with the light that goes with each), an obstacle for every <see cref="BattleObstacle"/> in the looks of every
    /// ground, each standing on one hexagon of 2.08 m, and the hills, mountains and woods that ring a field. The models
    /// are those of the map (Art/Models); where a ground needs other colors (snow on the pines and mountains, sand,
    /// ash, bog) the packs' palette textures are repainted for it.
    /// </summary>
    internal static class HeroesBattleArt
    {
        private const string Folder = "Art/Generated/Battlefield";

        /// <summary>A hexagon of the battlefield is 2.08 m across; an obstacle keeps inside it.</summary>
        private const float Cell = HexLayout.DefaultRadius * 1.7320508f;

        [MenuItem("Heroes/Art/Battlefield", false, 44)]
        public static void BuildMenu()
        {
            HeroesArt art = HeroesAssets.Require<HeroesArt>("Art/HeroesArt.asset");
            HeroesArtBuilder.Staged(() => Build(art));
            EditorUtility.SetDirty(art);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Renders every obstacle and backdrop into Logs/shots/battlefield, a picture each, to look them over.</summary>
        [MenuItem("Heroes/Art/Battlefield Pictures", false, 45)]
        public static void PicturesMenu()
        {
            HeroesArt art = HeroesAssets.Require<HeroesArt>("Art/HeroesArt.asset");
            string folder = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "shots", "battlefield");
            Directory.CreateDirectory(folder);
            foreach (string old in Directory.GetFiles(folder, "*.png"))
            {
                File.Delete(old);
            }
            using (var studio = new ModelStudio())
            {
                var shot = new StudioShot
                {
                    Width = 192, Height = 192, Yaw = 200f, Pitch = 28f, Padding = 0.04f, Supersample = 2,
                    Key = 1.05f, KeyColor = new Color(1f, 0.97f, 0.92f), Rim = 0.5f, Fill = 0.35f
                };
                foreach (HeroesArt.ObstacleArt obstacle in art.obstacles)
                {
                    string ground = obstacle.terrain < 0 ? "Any" : ((TerrainType)obstacle.terrain).ToString();
                    for (int i = 0; i < obstacle.variants.Length; i++)
                    {
                        Picture(studio, obstacle.variants[i], shot, Path.Combine(folder, $"{obstacle.kind}_{ground}_{i}.png"));
                    }
                }
                shot.Pitch = 12f;
                foreach (HeroesArt.BackdropArt backdrop in art.backdrops)
                {
                    for (int i = 0; i < backdrop.prefabs.Length; i++)
                    {
                        Picture(studio, backdrop.prefabs[i], shot, Path.Combine(folder, $"~Backdrop_{backdrop.terrain}_{i}.png"));
                    }
                }
            }
            HeroesArtBuilder.Staged(() =>
            {
                foreach (HeroesArt.SkyArt sky in art.skies)
                {
                    SkyPicture(sky, Path.Combine(folder, $"~~Sky_{sky.name}.png"));
                }
            });
            Debug.Log($"Heroes: the battlefield art is drawn in {folder}.");
        }

        /// <summary>A view of a sky toward its sun, from a camera standing in an empty scene lit as the sky says.</summary>
        private static void SkyPicture(HeroesArt.SkyArt sky, string file)
        {
            const int width = 480;
            const int height = 270;
            RenderSettings.skybox = sky.material;
            var cameraObject = new GameObject("Sky Camera") { hideFlags = HideFlags.HideAndDontSave };
            var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.cullingMask = 0;
            camera.fieldOfView = 70f;
            camera.transform.rotation = Quaternion.Euler(-12f, sky.sunAngles.y + 180f, 0f);
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            camera.targetTexture = target;
            camera.Render();
            camera.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            var picture = new Texture2D(width, height, TextureFormat.RGB24, false);
            picture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            picture.Apply();
            RenderTexture.active = previous;
            camera.targetTexture = null;
            File.WriteAllBytes(file, picture.EncodeToPNG());
            Object.DestroyImmediate(picture);
            target.Release();
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(cameraObject);
        }

        private static void Picture(ModelStudio studio, GameObject prefab, StudioShot shot, string file)
        {
            if (prefab == null)
            {
                return;
            }
            Texture2D picture = studio.Shoot(prefab, shot);
            File.WriteAllBytes(file, picture.EncodeToPNG());
            Object.DestroyImmediate(picture);
        }

        public static void Build(HeroesArt art)
        {
            Skies(art);
            Obstacles(art);
            Backdrops(art);
        }

        // ------------------------------------------------------------------ skies

        private static readonly (string name, string file, Color sun, float intensity, float lowest, Color sky, Color equator, Color ground)[] SkyTable =
        {
            ("Clear", "kloofendal_48d_partly_cloudy_puresky", new Color(1f, 0.96f, 0.88f), 1.45f, 38f,
                new Color(0.52f, 0.6f, 0.72f), new Color(0.44f, 0.46f, 0.46f), new Color(0.26f, 0.23f, 0.18f)),
            ("Overcast", "kloofendal_overcast_puresky", new Color(0.9f, 0.92f, 0.95f), 1.0f, 45f,
                new Color(0.6f, 0.62f, 0.66f), new Color(0.5f, 0.5f, 0.5f), new Color(0.3f, 0.28f, 0.25f)),
            ("Snow", "snow_field_puresky", new Color(0.96f, 0.97f, 1f), 1.15f, 35f,
                new Color(0.64f, 0.68f, 0.74f), new Color(0.56f, 0.58f, 0.62f), new Color(0.42f, 0.42f, 0.45f)),
            ("Wasteland", "wasteland_clouds_puresky", new Color(1f, 0.87f, 0.68f), 1.35f, 26f,
                new Color(0.55f, 0.58f, 0.66f), new Color(0.52f, 0.46f, 0.4f), new Color(0.3f, 0.24f, 0.18f)),
            ("Dusk", "qwantani_dusk_2_puresky", new Color(1f, 0.64f, 0.44f), 1.0f, 18f,
                new Color(0.38f, 0.36f, 0.52f), new Color(0.38f, 0.3f, 0.32f), new Color(0.16f, 0.12f, 0.12f))
        };

        /// <summary>By <see cref="TerrainType"/>: the name of the sky a battle on it is fought under.</summary>
        private static readonly string[] SkyOfTerrain =
        {
            "Clear", // Grass
            "Clear", // Dirt
            "Wasteland", // Sand
            "Snow", // Snow
            "Overcast", // Swamp
            "Clear", // Rough
            "Wasteland", // Wasteland
            "Clear", // Water
            "Dusk" // Rock, under the earth
        };

        private static void Skies(HeroesArt art)
        {
            Shader panoramic = Shader.Find("Skybox/Panoramic");
            art.skies.Clear();
            foreach ((string name, string file, Color sun, float intensity, float lowest, Color sky, Color equator, Color ground) in SkyTable)
            {
                string relative = $"Art/Sky/{file}.jpg";
                if (HeroesAssets.Load<Texture2D>(relative) == null)
                {
                    Debug.LogWarning($"Heroes: the sky {relative} is missing.");
                    continue;
                }
                // Skybox/Panoramic reads a flat latitude-longitude picture, which is what the importer makes of it.
                Texture2D texture = HeroesAssets.ConfigureTexture(relative, importer =>
                {
                    importer.textureType = TextureImporterType.Default;
                    importer.sRGBTexture = true;
                    importer.mipmapEnabled = false;
                    importer.maxTextureSize = 2048;
                    importer.wrapModeU = TextureWrapMode.Repeat;
                    importer.wrapModeV = TextureWrapMode.Clamp;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.textureCompression = TextureImporterCompression.CompressedHQ;
                    importer.npotScale = TextureImporterNPOTScale.None;
                });
                Material material = HeroesAssets.Material($"{Folder}/Sky{name}.mat", panoramic, m =>
                {
                    m.SetTexture("_MainTex", texture);
                    m.SetFloat("_Mapping", 1f);
                    m.SetFloat("_ImageType", 0f);
                    m.SetFloat("_MirrorOnBack", 0f);
                    m.SetFloat("_Layout", 0f);
                    m.SetFloat("_Exposure", 1f);
                    m.SetFloat("_Rotation", 0f);
                    m.SetColor("_Tint", new Color(0.5f, 0.5f, 0.5f, 0.5f));
                    m.EnableKeyword("_MAPPING_LATITUDE_LONGITUDE_LAYOUT");
                    m.DisableKeyword("_MAPPING_6_FRAMES_LAYOUT");
                });
                Measure(relative, lowest, out Vector2 sunAngles, out Color horizon);
                art.skies.Add(new HeroesArt.SkyArt
                {
                    name = name,
                    material = material,
                    sun = sun,
                    sunIntensity = intensity,
                    sunAngles = sunAngles,
                    ambientSky = sky,
                    ambientEquator = equator,
                    ambientGround = ground,
                    fog = horizon
                });
            }
            art.terrainSkies = new int[SkyOfTerrain.Length];
            for (int t = 0; t < SkyOfTerrain.Length; t++)
            {
                art.terrainSkies[t] = Mathf.Max(0, art.skies.FindIndex(sky => sky.name == SkyOfTerrain[t]));
            }
            art.duskSky = art.skies.FindIndex(sky => sky.name == "Dusk");
        }

        /// <summary>
        /// Finds the sun in a latitude-longitude sky (the middle of its brightest pixels above the horizon) and turns it
        /// into the angles of a directional light shining from there, lifted to at least <paramref name="lowest"/>
        /// degrees so a low sun still lights the field; and the color of the sky along its horizon, for the fog.
        /// </summary>
        private static void Measure(string relative, float lowest, out Vector2 angles, out Color horizon)
        {
            angles = new Vector2(46f, 138f);
            horizon = new Color(0.62f, 0.68f, 0.75f);
            string file = HeroesAssets.FullPath(HeroesAssets.Path(relative));
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(file)))
            {
                Object.DestroyImmediate(texture);
                return;
            }
            int width = texture.width;
            int height = texture.height;
            Color[] pixels = texture.GetPixels();
            Object.DestroyImmediate(texture);
            float best = 0f;
            for (int i = pixels.Length / 2; i < pixels.Length; i++)
            {
                best = Mathf.Max(best, pixels[i].grayscale);
            }
            Vector2 sum = Vector2.zero;
            float weight = 0f;
            Vector3 direction = Vector3.zero;
            for (int y = height / 2; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float v = pixels[y * width + x].grayscale;
                    if (v < best * 0.97f)
                    {
                        continue;
                    }
                    float u = (x + 0.5f) / width;
                    float t = (y + 0.5f) / height;
                    // The inverse of the panoramic shader's mapping: u = 0.5 - longitude / 2pi, v = 1 - latitude / pi.
                    float longitude = (0.5f - u) * 2f * Mathf.PI;
                    float latitude = (1f - t) * Mathf.PI;
                    direction += new Vector3(Mathf.Sin(latitude) * Mathf.Cos(longitude), Mathf.Cos(latitude), Mathf.Sin(latitude) * Mathf.Sin(longitude));
                    weight += 1f;
                }
            }
            if (weight > 0f)
            {
                direction.Normalize();
                float elevation = Mathf.Asin(Mathf.Clamp(direction.y, -1f, 1f)) * Mathf.Rad2Deg;
                float azimuth = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                elevation = Mathf.Max(elevation, lowest);
                Vector3 toSun = Quaternion.Euler(-elevation, azimuth, 0f) * Vector3.forward;
                Vector3 euler = Quaternion.LookRotation(-toSun).eulerAngles;
                angles = new Vector2(Mathf.Round(euler.x * 10f) / 10f, Mathf.Round(euler.y * 10f) / 10f);
            }
            var band = new Color(0f, 0f, 0f, 0f);
            int count = 0;
            for (int y = Mathf.RoundToInt(height * 0.5f); y < Mathf.RoundToInt(height * 0.53f); y++)
            {
                for (int x = 0; x < width; x += 4)
                {
                    band += pixels[y * width + x];
                    count++;
                }
            }
            if (count > 0)
            {
                band /= count;
                horizon = new Color(Mathf.Round(band.r * 100f) / 100f, Mathf.Round(band.g * 100f) / 100f, Mathf.Round(band.b * 100f) / 100f, 1f);
            }
        }

        // ------------------------------------------------------------------ repainted palettes

        /// <summary>How the packs' palette colors are repainted for a ground.</summary>
        internal enum Paint
        {
            None,
            /// <summary>Foliage and grass under snow, rock a little paler and bluer.</summary>
            Snow,
            /// <summary>The orange and yellow needles of the Halloween pines turned dark green.</summary>
            Evergreen,
            /// <summary>The orange and yellow needles under snow.</summary>
            SnowPine,
            Sand,
            Ash,
            Bog,
            /// <summary>Stone made dark and cold, for under the earth.</summary>
            Deep,
            /// <summary>
            /// The yellow green the hills of the nature pack are topped with turned to the green of the map's grass
            /// (it reads as sand beside it); the leaves of the trees keep their own green.
            /// </summary>
            Meadow
        }

        private static readonly Dictionary<(Texture, Paint), Material> Repainted = new Dictionary<(Texture, Paint), Material>();

        /// <summary>Gives every textured material of an instance the same texture repainted for <paramref name="paint"/>.</summary>
        internal static void Repaint(GameObject instance, Paint paint)
        {
            if (paint == Paint.None || instance == null)
            {
                return;
            }
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material source = materials[i];
                    Texture map = source != null && source.HasProperty("_BaseMap") ? source.GetTexture("_BaseMap") : null;
                    if (map == null)
                    {
                        continue;
                    }
                    materials[i] = RepaintedMaterial(map, paint, source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor") : Color.white);
                }
                renderer.sharedMaterials = materials;
            }
        }

        private static Material RepaintedMaterial(Texture map, Paint paint, Color tint)
        {
            if (Repainted.TryGetValue((map, paint), out Material cached) && cached != null)
            {
                return cached;
            }
            string source = AssetDatabase.GetAssetPath(map);
            string name = $"{Path.GetFileNameWithoutExtension(source)}_{paint}";
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.LoadImage(File.ReadAllBytes(HeroesAssets.FullPath(source)));
            Color[] pixels = texture.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Recolor(pixels[i], paint);
            }
            texture.SetPixels(pixels);
            texture.Apply();
            string relative = $"{Folder}/Textures/{name}.png";
            Texture2D saved = HeroesAssets.SaveTexture(texture, relative, importer =>
            {
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = true;
                importer.mipmapEnabled = true;
                importer.maxTextureSize = 1024;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
            });
            Material material = HeroesAssets.Material($"{Folder}/Materials/{name}.mat", Shader.Find("Universal Render Pipeline/Lit"), m =>
            {
                m.SetTexture("_BaseMap", saved);
                m.SetColor("_BaseColor", new Color(tint.r, tint.g, tint.b, 1f));
                m.SetFloat("_Smoothness", paint == Paint.Snow || paint == Paint.SnowPine ? 0.25f : 0.1f);
                m.SetFloat("_Metallic", 0f);
            });
            Repainted[(map, paint)] = material;
            return material;
        }

        /// <summary>One palette color repainted: the hue says what it is (green is grass and leaves, orange and yellow are the pines' needles).</summary>
        private static Color Recolor(Color color, Paint paint)
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);
            bool green = s > 0.3f && h > 0.17f && h < 0.47f;
            bool needles = s > 0.55f && v > 0.5f && h > 0.02f && h < 0.16f;
            bool grey = s < 0.22f;
            Color result = color;
            switch (paint)
            {
                case Paint.Snow:
                    if (green)
                    {
                        result = Color.Lerp(new Color(0.7f, 0.78f, 0.88f), new Color(0.97f, 0.98f, 1f), Mathf.Pow(v, 0.6f));
                    }
                    else if (grey)
                    {
                        result = Color.Lerp(color, new Color(0.8f, 0.85f, 0.92f) * (0.6f + 0.4f * v), 0.45f);
                    }
                    break;
                case Paint.Evergreen:
                    if (needles)
                    {
                        result = Color.Lerp(new Color(0.08f, 0.22f, 0.12f), new Color(0.24f, 0.46f, 0.26f), v);
                    }
                    break;
                case Paint.SnowPine:
                    if (needles)
                    {
                        // Dark needles under a heavy coat of snow: the upper, lighter half of each swatch is snow.
                        result = v > 0.75f
                            ? Color.Lerp(new Color(0.8f, 0.86f, 0.94f), new Color(0.97f, 0.98f, 1f), (v - 0.75f) / 0.25f)
                            : Color.Lerp(new Color(0.07f, 0.2f, 0.14f), new Color(0.2f, 0.36f, 0.3f), v / 0.75f);
                    }
                    break;
                case Paint.Sand:
                    if (green)
                    {
                        result = Color.Lerp(new Color(0.62f, 0.5f, 0.32f), new Color(0.93f, 0.82f, 0.6f), v);
                    }
                    else if (grey)
                    {
                        result = Color.Lerp(color, new Color(0.8f, 0.7f, 0.55f) * (0.5f + 0.5f * v), 0.4f);
                    }
                    break;
                case Paint.Ash:
                    if (green)
                    {
                        result = Color.Lerp(new Color(0.18f, 0.15f, 0.13f), new Color(0.42f, 0.36f, 0.3f), v);
                    }
                    else if (grey)
                    {
                        result = Color.Lerp(color, new Color(0.45f, 0.38f, 0.34f) * (0.5f + 0.5f * v), 0.4f);
                    }
                    break;
                case Paint.Bog:
                    if (green)
                    {
                        result = Color.Lerp(new Color(0.16f, 0.2f, 0.1f), new Color(0.4f, 0.44f, 0.24f), v);
                    }
                    break;
                case Paint.Deep:
                    if (green)
                    {
                        result = Color.Lerp(new Color(0.12f, 0.12f, 0.16f), new Color(0.3f, 0.3f, 0.36f), v);
                    }
                    else
                    {
                        result = Color.Lerp(color, new Color(0.3f, 0.3f, 0.38f) * (0.4f + 0.6f * v), 0.55f);
                    }
                    break;
                case Paint.Meadow:
                    if (s > 0.4f && h > 0.13f && h < 0.2f)
                    {
                        result = Color.Lerp(new Color(0.18f, 0.33f, 0.09f), new Color(0.44f, 0.62f, 0.22f), v);
                    }
                    break;
            }
            result.a = color.a;
            return result;
        }

        // ------------------------------------------------------------------ obstacles

        /// <summary>A model in an obstacle: which, where on the cell (x, z in meters), turned how far, how tall, and its tint.</summary>
        private readonly struct Bit
        {
            public readonly string Model;
            public readonly float X;
            public readonly float Z;
            public readonly float Yaw;
            public readonly float Height;
            public readonly Color Tint;

            public Bit(string model, float x = 0f, float z = 0f, float yaw = 0f, float height = 0f, Color tint = default)
            {
                Model = model;
                X = x;
                Z = z;
                Yaw = yaw;
                Height = height;
                Tint = tint;
            }
        }

        private static readonly Color SnowRock = new Color(0.86f, 0.9f, 0.97f);
        private static readonly Color SandRock = new Color(0.96f, 0.84f, 0.64f);
        private static readonly Color AshRock = new Color(0.62f, 0.52f, 0.46f);
        private static readonly Color DeepRock = new Color(0.55f, 0.55f, 0.64f);
        private static readonly Color BogWood = new Color(0.62f, 0.66f, 0.52f);
        private static readonly Color Timber = new Color(0.58f, 0.44f, 0.36f);
        private static readonly Color Amethyst = new Color(0.68f, 0.44f, 1f);
        private static readonly Color Ruby = new Color(0.92f, 0.2f, 0.22f);

        /// <summary>By the name of a group of grounds: the terrain texture its mounds of earth are covered with.</summary>
        private static readonly Dictionary<string, string> Earth = new Dictionary<string, string>
        {
            { "Green", "Grass" }, { "Snow", "Snow" }, { "Sand", "Sand" }, { "Swamp", "Swamp" }, { "Waste", "Wasteland" }, { "Deep", "Rock" }
        };

        /// <summary>The grounds grouped by the look their obstacles share; the first of each is named in the prefab.</summary>
        private static readonly (string name, TerrainType[] grounds, Color rock, Paint paint)[] Grounds =
        {
            ("Green", new[] { TerrainType.Grass, TerrainType.Dirt, TerrainType.Rough, TerrainType.Water }, Color.white, Paint.None),
            ("Snow", new[] { TerrainType.Snow }, SnowRock, Paint.Snow),
            ("Sand", new[] { TerrainType.Sand }, SandRock, Paint.Sand),
            ("Swamp", new[] { TerrainType.Swamp }, Color.white, Paint.Bog),
            ("Waste", new[] { TerrainType.Wasteland }, AshRock, Paint.Ash),
            ("Deep", new[] { TerrainType.Rock }, DeepRock, Paint.Deep)
        };

        private static void Obstacles(HeroesArt art)
        {
            art.obstacles.Clear();
            Material scorch = Scorch();
            Material pool = HeroesAssets.Material($"{Folder}/Materials/Pool.mat", Shader.Find("Universal Render Pipeline/Lit"), m =>
            {
                m.SetColor("_BaseColor", new Color(0.1f, 0.3f, 0.42f, 0.82f));
                m.SetFloat("_Smoothness", 0.93f);
                m.SetFloat("_Metallic", 0.05f);
                Transparent(m);
            });
            Material bog = HeroesAssets.Material($"{Folder}/Materials/Bog.mat", Shader.Find("Universal Render Pipeline/Lit"), m =>
            {
                m.SetColor("_BaseColor", new Color(0.16f, 0.2f, 0.1f, 0.9f));
                m.SetFloat("_Smoothness", 0.85f);
                m.SetFloat("_Metallic", 0f);
                Transparent(m);
            });

            foreach ((string name, TerrainType[] grounds, Color rock, Paint paint) in Grounds)
            {
                bool green = name == "Green";
                bool snow = name == "Snow";
                bool deep = name == "Deep";
                bool swamp = name == "Swamp";
                bool dry = name == "Sand" || name == "Waste";
                Color stone = rock;

                Add(art, BattleObstacle.Rock, grounds, name, paint,
                    new[] { new Bit("KayKit/Nature/rock_single_A", 0f, 0f, 20f, 0.95f, stone) },
                    new[] { new Bit("KayKit/Nature/rock_single_B", 0f, 0f, 70f, 0.8f, stone), new Bit("KayKit/Nature/rock_single_D", 0.5f, -0.35f, 10f, 0.4f, stone) },
                    new[] { new Bit("KayKit/Nature/rock_single_E", 0f, 0f, 140f, 0.9f, stone) });
                Add(art, BattleObstacle.Boulder, grounds, name, paint,
                    new[] { new Bit("KayKit/Nature/rock_single_C", 0f, 0f, 0f, 1.5f, stone), new Bit("KayKit/Nature/rock_single_D", 0.62f, -0.4f, 40f, 0.45f, stone) },
                    new[] { new Bit("KayKit/Nature/rock_single_A", 0f, 0f, 200f, 1.45f, stone), new Bit("KayKit/Nature/rock_single_B", -0.6f, -0.35f, 0f, 0.5f, stone) });

                if (green)
                {
                    Add(art, BattleObstacle.Tree, grounds, name, paint,
                        new[] { new Bit("KayKit/Nature/tree_single_A", 0f, 0f, 0f, 3.4f) },
                        new[] { new Bit("KayKit/Nature/tree_single_B", 0f, 0f, 60f, 3.7f) },
                        new[] { new Bit("KayKit/Nature/tree_single_A", 0.1f, 0f, 130f, 2.8f), new Bit("KayKit/Nature/rock_single_D", -0.55f, -0.4f, 0f, 0.35f) });
                }
                else if (snow)
                {
                    Add(art, BattleObstacle.Tree, grounds, name, Paint.SnowPine,
                        new[] { new Bit("KayKit/Halloween/tree_pine_yellow_large", 0f, 0f, 0f, 4.0f) },
                        new[] { new Bit("KayKit/Halloween/tree_pine_orange_large", 0f, 0f, 90f, 3.8f) });
                }
                else
                {
                    Add(art, BattleObstacle.Tree, grounds, name, paint,
                        new[] { new Bit("KayKit/Halloween/tree_dead_large", 0f, 0f, 0f, 3.4f, swamp ? BogWood : default) },
                        new[] { new Bit("KayKit/Halloween/tree_dead_medium", 0f, 0f, 120f, 2.9f, swamp ? BogWood : default) });
                }
                Add(art, BattleObstacle.Pine, grounds, name, snow ? Paint.SnowPine : Paint.Evergreen,
                    new[] { new Bit("KayKit/Halloween/tree_pine_yellow_large", 0f, 0f, 0f, 4.0f) },
                    new[] { new Bit("KayKit/Halloween/tree_pine_orange_medium", 0f, 0f, 45f, 3.2f), new Bit("KayKit/Halloween/tree_pine_yellow_small", 0.5f, -0.45f, 0f, 1.6f) },
                    new[] { new Bit("KayKit/Halloween/tree_pine_orange_large", 0f, 0f, 200f, 3.8f) });
                Add(art, BattleObstacle.DeadTree, grounds, name, paint,
                    new[] { new Bit("KayKit/Halloween/tree_dead_large", 0f, 0f, 30f, 3.2f, swamp ? BogWood : default) },
                    new[] { new Bit("KayKit/Halloween/tree_dead_medium", 0f, 0f, 150f, 2.7f, swamp ? BogWood : default) },
                    new[] { new Bit("KayKit/Halloween/tree_dead_small", 0f, 0f, 260f, 2.1f, swamp ? BogWood : default), new Bit("KayKit/Halloween/bone_A", 0.55f, -0.4f, 30f, 0.15f) });
                Add(art, BattleObstacle.Stump, grounds, name, paint,
                    new[] { new Bit("KayKit/Nature/tree_single_A_cut", 0f, 0f, 0f, 0.75f) },
                    new[] { new Bit("KayKit/Nature/tree_single_B_cut", 0f, 0f, 80f, 0.85f), new Bit("KayKit/Nature/rock_single_D", 0.55f, 0.35f, 0f, 0.3f, stone) });
                Add(art, BattleObstacle.Logs, grounds, name, paint,
                    new[] { new Bit("KayKit/Props/resource_lumber", 0f, 0f, 20f, 0.7f, Timber) },
                    new[] { new Bit("KayKit/Props/resource_lumber", -0.1f, 0.1f, 110f, 0.6f, Timber), new Bit("KayKit/Nature/tree_single_B_cut", 0.62f, -0.35f, 0f, 0.5f) });
                Add(art, BattleObstacle.Bones, grounds, name, paint,
                    new[] { new Bit("KayKit/Halloween/ribcage", 0f, 0f, 30f, 0.55f), new Bit("KayKit/Halloween/skull", 0.55f, -0.3f, 200f, 0.3f), new Bit("KayKit/Halloween/bone_B", -0.5f, -0.35f, 70f, 0.12f) },
                    new[] { new Bit("KayKit/Halloween/skull", 0f, 0f, 160f, 0.38f), new Bit("KayKit/Halloween/bone_A", 0.4f, 0.3f, 20f, 0.14f), new Bit("KayKit/Halloween/bone_C", -0.45f, -0.2f, 110f, 0.14f), new Bit("KayKit/Halloween/gravemarker_A", -0.2f, 0.5f, 10f, 0.8f) });
                // The crystals of a cluster share one color: amethyst, or the red of the crystal the mines dig.
                Add(art, BattleObstacle.Crystal, grounds, name, Paint.None,
                    new[] { new Bit("Quaternius/Items/Crystal1", 0f, 0f, 0f, 1.7f, Amethyst), new Bit("Quaternius/Items/Crystal2", 0.5f, -0.3f, 40f, 0.9f, Amethyst), new Bit("Quaternius/Items/Crystal3", -0.45f, 0.25f, 0f, 0.75f, Amethyst) },
                    new[] { new Bit("Quaternius/Items/Crystal4", 0f, 0f, 0f, 1.5f, Ruby), new Bit("Quaternius/Items/Crystal5", -0.5f, -0.3f, 20f, 0.8f, Ruby), new Bit("KayKit/Nature/rock_single_D", 0.5f, 0.3f, 0f, 0.35f, stone) });
                Mound(art, grounds, name, paint, stone);
                Material water = swamp ? bog : pool;
                Add(art, BattleObstacle.Pool, grounds, name, paint, water,
                    new[] { new Bit("KayKit/Nature/rock_single_D", 0.7f, 0.35f, 0f, 0.3f, stone), new Bit("KayKit/Nature/rock_single_E", -0.72f, -0.2f, 90f, 0.25f, stone), deep || dry || snow ? new Bit("KayKit/Nature/rock_single_B", 0.1f, -0.75f, 0f, 0.22f, stone) : new Bit("KayKit/Nature/waterlily_A", 0.2f, 0.1f, 0f, 0.06f) },
                    new[] { new Bit("KayKit/Nature/waterplant_A", -0.6f, 0.45f, 0f, 0.7f), new Bit("KayKit/Nature/rock_single_D", 0.65f, -0.4f, 30f, 0.28f, stone), deep || dry || snow ? new Bit("KayKit/Nature/rock_single_E", 0.2f, 0.7f, 0f, 0.2f, stone) : new Bit("KayKit/Nature/waterlily_B", -0.1f, -0.2f, 60f, 0.06f) });
                Add(art, BattleObstacle.Rubble, grounds, name, paint,
                    new[] { new Bit("KayKit/Dungeon/rubble_large", 0f, 0f, 0f, 0.7f, stone) },
                    new[] { new Bit("KayKit/Dungeon/rubble_half", 0f, 0f, 90f, 0.5f, stone), new Bit("KayKit/Dungeon/sword_shield_broken", 0.4f, -0.4f, 30f, 0.35f) });
                Crater(art, grounds, name, paint, scorch, stone);
            }

            // The wall of a besieged town is the same on every ground.
            var wall = new List<GameObject>();
            foreach ((string model, float yaw) in new[] { ("KayKit/Buildings/Neutral/wall_straight", 0f), ("KayKit/Buildings/Neutral/wall_straight", 180f) })
            {
                GameObject root = HeroesArtBuilder.Root($"Wall{wall.Count}");
                GameObject piece = HeroesArtBuilder.Piece(root.transform, model, Vector3.zero, yaw, 1f, 0f);
                if (piece != null)
                {
                    // Stretched along its length to run from one side of the cell to the other.
                    Bounds bounds = HeroesArtBuilder.BoundsOf(piece);
                    Vector3 size = Quaternion.Euler(0f, yaw, 0f) * bounds.size;
                    float along = Mathf.Max(Mathf.Abs(size.x), 0.01f);
                    float scale = (Cell + 0.1f) / along;
                    piece.transform.localScale *= scale;
                    Recenter(piece);
                }
                wall.Add(HeroesArtBuilder.Save(root, $"{Folder}/Obstacles/Wall{wall.Count}.prefab"));
            }
            art.obstacles.Add(new HeroesArt.ObstacleArt { kind = BattleObstacle.Wall, terrain = -1, variants = wall.Where(p => p != null).ToArray() });
        }

        private static void Add(HeroesArt art, BattleObstacle kind, TerrainType[] grounds, string ground, Paint paint, params Bit[][] looks)
        {
            Add(art, kind, grounds, ground, paint, null, looks);
        }

        /// <summary>
        /// Builds the looks of an obstacle for a group of grounds (the first ground in the file names) and files them
        /// under every ground of the group; the Green group also serves every ground with nothing of its own.
        /// </summary>
        private static void Add(HeroesArt art, BattleObstacle kind, TerrainType[] grounds, string ground, Paint paint, Material water, params Bit[][] looks)
        {
            var variants = new List<GameObject>();
            for (int i = 0; i < looks.Length; i++)
            {
                GameObject root = HeroesArtBuilder.Root($"{kind}{ground}{i}");
                if (water != null)
                {
                    Pond(root.transform, water, i);
                }
                foreach (Bit bit in looks[i])
                {
                    GameObject piece = HeroesArtBuilder.Piece(root.transform, bit.Model, new Vector3(bit.X, 0f, bit.Z), bit.Yaw, 1f, bit.Height, bit.Tint);
                    if (piece != null && bit.Height <= 0f)
                    {
                        // A hill of the nature pack is fitted to the cell across, and flattened into a mound.
                        Bounds bounds = HeroesArtBuilder.BoundsOf(piece);
                        float widest = Mathf.Max(bounds.size.x, bounds.size.z);
                        if (widest > 0.001f)
                        {
                            float scale = Cell * 0.92f / widest;
                            piece.transform.localScale = new Vector3(piece.transform.localScale.x * scale, piece.transform.localScale.y * scale * 0.55f,
                                piece.transform.localScale.z * scale);
                            Recenter(piece);
                        }
                    }
                    if (piece != null && bit.Model.StartsWith("Quaternius/Items/", StringComparison.Ordinal))
                    {
                        Stand(piece, bit);
                    }
                    Repaint(piece, paint);
                }
                Fit(root);
                variants.Add(HeroesArtBuilder.Save(root, $"{Folder}/Obstacles/{kind}{ground}{i}.prefab"));
            }
            Catalog(art, kind, grounds, ground, variants);
        }

        /// <summary>
        /// Stands a model of the item pack up as it was made. Its file turns it upright on its root (it was drawn with Z
        /// up), and <see cref="HeroesArtBuilder.Piece"/> gives the root the yaw alone, which lays it on its side: the map
        /// lets its treasures lie on the ground that way, but a crystal of the battlefield grows up out of it.
        /// </summary>
        private static void Stand(GameObject piece, Bit bit)
        {
            GameObject model = HeroesArtBuilder.Model(bit.Model);
            piece.transform.localRotation = Quaternion.Euler(0f, bit.Yaw, 0f) * model.transform.localRotation;
            Bounds bounds = HeroesArtBuilder.BoundsOf(piece);
            if (bit.Height > 0f && bounds.size.y > 0.0001f)
            {
                piece.transform.localScale *= bit.Height / bounds.size.y;
                bounds = HeroesArtBuilder.BoundsOf(piece);
            }
            // The crystals are cut at both ends; sunk by a third they grow out of the ground instead of balancing on a point.
            float sunk = bit.Model.StartsWith("Quaternius/Items/Crystal", StringComparison.Ordinal) ? bounds.size.y * 0.3f : 0f;
            piece.transform.localPosition += new Vector3(bit.X - bounds.center.x, -bounds.min.y - sunk, bit.Z - bounds.center.z);
        }

        /// <summary>A crater: a scorched hollow in the ground (a dark disc lying on it) ringed by thrown up rubble.</summary>
        private static void Crater(HeroesArt art, TerrainType[] grounds, string ground, Paint paint, Material scorch, Color stone)
        {
            const BattleObstacle kind = BattleObstacle.Crater;
            var variants = new List<GameObject>();
            for (int i = 0; i < 2; i++)
            {
                GameObject root = HeroesArtBuilder.Root($"{kind}{ground}{i}");
                var disc = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Object.DestroyImmediate(disc.GetComponent<Collider>());
                disc.name = "Scorch";
                disc.transform.SetParent(root.transform, false);
                disc.transform.localPosition = new Vector3(0f, 0.025f, 0f);
                disc.transform.localRotation = Quaternion.Euler(90f, i * 70f, 0f);
                disc.transform.localScale = new Vector3(Cell * 0.95f, Cell * 0.95f, 1f);
                var renderer = disc.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = scorch;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                int stones = 7 + i;
                for (int s = 0; s < stones; s++)
                {
                    float angle = (s + 0.37f * i) / stones * Mathf.PI * 2f;
                    float radius = Cell * 0.36f + (s % 3) * 0.06f;
                    string model = s % 3 == 0 ? "KayKit/Dungeon/rubble_half" : s % 3 == 1 ? "KayKit/Nature/rock_single_D" : "KayKit/Nature/rock_single_E";
                    GameObject piece = HeroesArtBuilder.Piece(root.transform, model, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius),
                        s * 53f, 1f, s % 3 == 0 ? 0.22f : 0.26f + (s % 2) * 0.08f, stone);
                    Repaint(piece, paint);
                }
                Fit(root);
                variants.Add(HeroesArtBuilder.Save(root, $"{Folder}/Obstacles/{kind}{ground}{i}.prefab"));
            }
            Catalog(art, kind, grounds, ground, variants);
        }

        /// <summary>
        /// A mound: a low dome of the ground's own earth (the terrain's texture on a flattened sphere, half sunk), with a
        /// stone or two on it.
        /// </summary>
        private static void Mound(HeroesArt art, TerrainType[] grounds, string ground, Paint paint, Color stone)
        {
            const BattleObstacle kind = BattleObstacle.Mound;
            string texture = Earth.TryGetValue(ground, out string name) ? name : "Grass";
            var diffuse = HeroesAssets.Load<Texture2D>($"Art/Terrain/{texture}.jpg");
            var normal = HeroesAssets.Load<Texture2D>($"Art/Terrain/{texture}_Normal.jpg");
            Material earth = HeroesAssets.Material($"{Folder}/Materials/Earth{ground}.mat", Shader.Find("Universal Render Pipeline/Lit"), m =>
            {
                m.SetTexture("_BaseMap", diffuse);
                m.SetTextureScale("_BaseMap", new Vector2(2f, 1f));
                m.SetColor("_BaseColor", Color.white);
                if (normal != null)
                {
                    m.SetTexture("_BumpMap", normal);
                    m.SetTextureScale("_BumpMap", new Vector2(2f, 1f));
                    m.SetFloat("_BumpScale", 0.8f);
                    m.EnableKeyword("_NORMALMAP");
                }
                m.SetFloat("_Smoothness", 0f);
                m.SetFloat("_Metallic", 0f);
            });
            var variants = new List<GameObject>();
            for (int i = 0; i < 2; i++)
            {
                GameObject root = HeroesArtBuilder.Root($"{kind}{ground}{i}");
                var dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Object.DestroyImmediate(dome.GetComponent<Collider>());
                dome.name = "Earth";
                dome.transform.SetParent(root.transform, false);
                dome.transform.localRotation = Quaternion.Euler(0f, i * 50f, 0f);
                dome.transform.localScale = new Vector3(Cell * (i == 0 ? 0.86f : 0.78f), i == 0 ? 0.7f : 0.9f, Cell * (i == 0 ? 0.76f : 0.8f));
                dome.GetComponent<MeshRenderer>().sharedMaterial = earth;
                Repaint(HeroesArtBuilder.Piece(root.transform, "KayKit/Nature/rock_single_D", new Vector3(0.35f, i == 0 ? 0.2f : 0.28f, -0.2f), 30f + i * 90f, 1f, 0.3f, stone), paint);
                if (i == 1)
                {
                    Repaint(HeroesArtBuilder.Piece(root.transform, "KayKit/Nature/rock_single_E", new Vector3(-0.4f, 0.12f, 0.3f), 200f, 1f, 0.25f, stone), paint);
                }
                Fit(root);
                variants.Add(HeroesArtBuilder.Save(root, $"{Folder}/Obstacles/{kind}{ground}{i}.prefab"));
            }
            Catalog(art, kind, grounds, ground, variants);
        }

        /// <summary>Files the looks of an obstacle under every ground of a group, and those of the Green group under any ground.</summary>
        private static void Catalog(HeroesArt art, BattleObstacle kind, TerrainType[] grounds, string ground, List<GameObject> variants)
        {
            GameObject[] built = variants.Where(prefab => prefab != null).ToArray();
            foreach (TerrainType terrain in grounds)
            {
                art.obstacles.Add(new HeroesArt.ObstacleArt { kind = kind, terrain = (int)terrain, variants = built });
            }
            if (ground == "Green")
            {
                art.obstacles.Add(new HeroesArt.ObstacleArt { kind = kind, terrain = -1, variants = built });
            }
        }

        /// <summary>A pond: a flat disc of water just above the ground.</summary>
        private static void Pond(Transform parent, Material water, int variant)
        {
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.DestroyImmediate(disc.GetComponent<Collider>());
            disc.name = "Water";
            disc.transform.SetParent(parent, false);
            disc.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            disc.transform.localRotation = Quaternion.Euler(0f, variant * 35f, 0f);
            disc.transform.localScale = new Vector3(Cell * (variant == 0 ? 0.78f : 0.7f), 0.02f, Cell * (variant == 0 ? 0.62f : 0.72f));
            var renderer = disc.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = water;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        /// <summary>Scales an obstacle down, about its middle on the ground, if it spills out of its cell.</summary>
        private static void Fit(GameObject root)
        {
            Bounds bounds = HeroesArtBuilder.BoundsOf(root);
            float reach = 0f;
            foreach (Vector3 corner in new[] { bounds.min, bounds.max, new Vector3(bounds.min.x, 0f, bounds.max.z), new Vector3(bounds.max.x, 0f, bounds.min.z) })
            {
                reach = Mathf.Max(reach, new Vector2(corner.x, corner.z).magnitude);
            }
            // A hexagon's inner circle is its width; the corners of a box around round things overstate them a little.
            float limit = Cell * 0.5f * 1.12f;
            if (reach > limit)
            {
                float scale = limit / reach;
                foreach (Transform child in root.transform)
                {
                    child.localPosition *= scale;
                    child.localScale *= scale;
                }
            }
        }

        /// <summary>Puts a piece's footprint back over the origin and its base back on the ground after scaling.</summary>
        private static void Recenter(GameObject piece)
        {
            Bounds sized = HeroesArtBuilder.BoundsOf(piece);
            Vector3 at = piece.transform.localPosition;
            piece.transform.localPosition = new Vector3(at.x - sized.center.x, at.y - sized.min.y, at.z - sized.center.z);
        }

        /// <summary>The scorch of a crater: dark ash in the middle, fading out to nothing at its rim, a little ragged.</summary>
        private static Material Scorch()
        {
            const int size = 256;
            var raster = new Raster(size, size, Color.clear);
            var center = new Vector2(size / 2f, size / 2f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - center;
                    float angle = Mathf.Atan2(p.y, p.x);
                    float ragged = 1f + 0.08f * Mathf.Sin(angle * 7f) + 0.05f * Mathf.Sin(angle * 13f + 1.3f);
                    float r = p.magnitude / (size * 0.5f * ragged);
                    float a = Mathf.Clamp01(1f - Mathf.SmoothStep(0.45f, 1f, r));
                    float ember = Mathf.Clamp01(1f - r / 0.35f);
                    var color = Color.Lerp(new Color(0.09f, 0.07f, 0.06f), new Color(0.05f, 0.035f, 0.03f), ember);
                    raster.Set(x, y, new Color(color.r, color.g, color.b, a * 0.9f));
                }
            }
            Texture2D texture = HeroesAssets.SaveTexture(raster.ToTexture(), $"{Folder}/Textures/Scorch.png", importer =>
            {
                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.maxTextureSize = 256;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
            });
            return HeroesAssets.Material($"{Folder}/Materials/Scorch.mat", Shader.Find("Universal Render Pipeline/Unlit"), m =>
            {
                m.SetTexture("_BaseMap", texture);
                m.SetColor("_BaseColor", Color.white);
                Transparent(m);
            });
        }

        private static void Transparent(Material material)
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_ZWrite", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
        }

        // ------------------------------------------------------------------ backdrops

        /// <summary>The hills, mountains and woods that ring a field: big pieces of the nature pack, many meters across.</summary>
        private static void Backdrops(HeroesArt art)
        {
            art.backdrops.Clear();
            (TerrainType terrain, Paint paint, Color tint, (string model, float width)[] pieces)[] table =
            {
                (TerrainType.Grass, Paint.Meadow, Color.white, new[]
                {
                    ("KayKit/Nature/mountain_A_grass_trees", 16f), ("KayKit/Nature/mountain_B_grass_trees", 14f), ("KayKit/Nature/hills_A_trees", 12f),
                    ("KayKit/Nature/hills_B_trees", 12f), ("KayKit/Nature/trees_A_large", 9f)
                }),
                (TerrainType.Dirt, Paint.Meadow, Color.white, new[]
                {
                    ("KayKit/Nature/mountain_A_grass", 16f), ("KayKit/Nature/mountain_C", 14f), ("KayKit/Nature/hills_C_trees", 12f), ("KayKit/Nature/trees_B_large", 9f)
                }),
                (TerrainType.Rough, Paint.Meadow, Color.white, new[]
                {
                    ("KayKit/Nature/mountain_A", 16f), ("KayKit/Nature/mountain_B", 15f), ("KayKit/Nature/mountain_C", 14f), ("KayKit/Nature/hills_C", 12f)
                }),
                (TerrainType.Water, Paint.Meadow, Color.white, new[]
                {
                    ("KayKit/Nature/hills_A_trees", 12f), ("KayKit/Nature/hills_B", 12f), ("KayKit/Nature/mountain_C_grass_trees", 14f)
                }),
                (TerrainType.Snow, Paint.Snow, Color.white, new[]
                {
                    ("KayKit/Nature/mountain_A_grass", 16f), ("KayKit/Nature/mountain_B_grass", 15f), ("KayKit/Nature/mountain_C_grass", 14f), ("KayKit/Nature/hills_A", 12f)
                }),
                (TerrainType.Sand, Paint.Sand, SandRock, new[]
                {
                    ("KayKit/Nature/mountain_A", 16f), ("KayKit/Nature/mountain_C", 14f), ("KayKit/Nature/hills_A", 12f), ("KayKit/Nature/hills_C", 12f)
                }),
                (TerrainType.Swamp, Paint.Bog, Color.white, new[]
                {
                    ("KayKit/Nature/hills_A_trees", 12f), ("KayKit/Nature/hills_B_trees", 12f), ("KayKit/Nature/trees_B_large", 9f), ("KayKit/Nature/mountain_C_grass_trees", 14f)
                }),
                (TerrainType.Wasteland, Paint.Ash, AshRock, new[]
                {
                    ("KayKit/Nature/mountain_A", 16f), ("KayKit/Nature/mountain_B", 15f), ("KayKit/Nature/hills_C", 12f)
                }),
                (TerrainType.Rock, Paint.Deep, DeepRock, new[]
                {
                    ("KayKit/Nature/mountain_A", 16f), ("KayKit/Nature/mountain_B", 15f), ("KayKit/Nature/mountain_C", 14f)
                })
            };
            foreach ((TerrainType terrain, Paint paint, Color tint, (string model, float width)[] pieces) in table)
            {
                var prefabs = new List<GameObject>();
                for (int i = 0; i < pieces.Length; i++)
                {
                    GameObject root = HeroesArtBuilder.Root($"Backdrop{terrain}{i}");
                    GameObject piece = HeroesArtBuilder.Piece(root.transform, pieces[i].model, Vector3.zero, i * 60f, 1f, 0f, tint);
                    if (piece != null)
                    {
                        Bounds bounds = HeroesArtBuilder.BoundsOf(piece);
                        float widest = Mathf.Max(bounds.size.x, bounds.size.z);
                        if (widest > 0.001f)
                        {
                            piece.transform.localScale *= pieces[i].width / widest;
                            Recenter(piece);
                        }
                        Repaint(piece, paint);
                    }
                    prefabs.Add(HeroesArtBuilder.Save(root, $"{Folder}/Backdrops/{terrain}{i}.prefab"));
                }
                art.backdrops.Add(new HeroesArt.BackdropArt { terrain = terrain, prefabs = prefabs.Where(p => p != null).ToArray() });
            }
        }
    }
}
