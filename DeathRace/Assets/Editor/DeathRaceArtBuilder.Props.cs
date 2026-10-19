using System;
using System.Collections.Generic;
using Gamebox.Editor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Portfolio.DeathRace.EditorTools
{
    /// <summary>
    /// The scenery: the meshes of every prop, landmark and lamp (Art/Props/Meshes, shared by both looks), the palette of
    /// each look (its colours, glow and metal) as one material, and per look a prefab of each (Art/Props/&lt;Look&gt;/
    /// &lt;Biome&gt;/&lt;Name&gt;.prefab): a single mesh renderer at the root, pivot at the middle of the base, no colliders.
    /// A lamp has a child "Head" where its light is and, at night, a child "Pool": the light it throws on the ground, an
    /// additive quad (the URP pipeline is forward with four lights per object, so the lamps carry no real lights).
    /// </summary>
    internal static partial class DeathRaceArtBuilder
    {
        public const string PropMeshesFolder = "Art/Props/Meshes";
        public const string LampName = "Lamp";

        /// <summary>The light pool's size on the ground, in metres.</summary>
        private const float PoolSize = 14f;

        private sealed class PropRecipe
        {
            public readonly string Name;
            public readonly Func<int, LowPolyBuilder> Build;
            public readonly int Seed;

            public PropRecipe(string name, Func<int, LowPolyBuilder> build, int seed)
            {
                Name = name;
                Build = build;
                Seed = seed;
            }
        }

        private delegate LowPolyBuilder LampRecipe(int seed, out Vector3 head);

        private static PropRecipe R(string name, Func<int, LowPolyBuilder> build, int seed)
        {
            return new PropRecipe(name, build, seed);
        }

        /// <summary>The scenery of each biome, scattered outside the barriers.</summary>
        private static PropRecipe[] PropsOf(Biome biome)
        {
            switch (biome)
            {
                case Biome.Industrial:
                    return new[]
                    {
                        R("ContainerStack", PropMeshes.ContainerStack, 21), R("Container", PropMeshes.Container, 22), R("Crates", PropMeshes.Crates, 23),
                        R("Pallets", PropMeshes.Pallets, 24), R("Pipes", PropMeshes.Pipes, 25), R("Tank", PropMeshes.Tank, 26),
                        R("HazardDrums", PropMeshes.HazardDrums, 27), R("ChainFence", PropMeshes.ChainFence, 28), R("Forklift", PropMeshes.Forklift, 29),
                        R("ScrapPile", PropMeshes.ScrapPile, 30), R("Dumpster", PropMeshes.Dumpster, 31), R("Transformer", PropMeshes.Transformer, 32)
                    };
                case Biome.Snow:
                    return new[]
                    {
                        R("SnowPine", PropMeshes.SnowPine, 41), R("PineGroup", PropMeshes.PineGroup, 42), R("SnowRock", PropMeshes.SnowRock, 43),
                        R("IceBlocks", PropMeshes.IceBlocks, 44), R("Snowbank", PropMeshes.Snowbank, 45), R("Cabin", PropMeshes.Cabin, 46),
                        R("FrozenTruck", PropMeshes.FrozenTruck, 47), R("Snowman", PropMeshes.Snowman, 48), R("SnowDrums", PropMeshes.SnowDrums, 49),
                        R("DeadPine", PropMeshes.DeadPine, 50), R("LogPile", PropMeshes.LogPile, 51), R("WarningSign", PropMeshes.WarningSign, 52)
                    };
                case Biome.City:
                    return new[]
                    {
                        R("ParkedCar", PropMeshes.ParkedCar, 61), R("ParkedCarB", PropMeshes.ParkedCar, 64), R("TrafficCones", PropMeshes.TrafficCones, 62),
                        R("Barricade", PropMeshes.Barricade, 63), R("BusStop", PropMeshes.BusStop, 65), R("StreetTree", PropMeshes.StreetTree, 66),
                        R("Kiosk", PropMeshes.Kiosk, 67), R("Shop", PropMeshes.Shop, 68), R("Townhouse", PropMeshes.Townhouse, 69),
                        R("WaterTower", PropMeshes.WaterTower, 70), R("CityBillboard", PropMeshes.CityBillboard, 71), R("CityDumpster", PropMeshes.Dumpster, 72)
                    };
                default:
                    return new[]
                    {
                        R("Boulders", PropMeshes.Boulders, 1), R("RockSpire", PropMeshes.RockSpire, 2), R("Saguaro", PropMeshes.Saguaro, 3),
                        R("BarrelCacti", PropMeshes.BarrelCacti, 4), R("DeadTree", PropMeshes.DeadTree, 5), R("TyreStack", PropMeshes.TyreStack, 6),
                        R("OilDrums", PropMeshes.OilDrums, 7), R("CarHulk", PropMeshes.CarHulk, 8), R("DesertBillboard", PropMeshes.DesertBillboard, 9),
                        R("Tumbleweed", PropMeshes.Tumbleweed, 10), R("BurningBarrel", PropMeshes.BurningBarrel, 11), R("FirePit", PropMeshes.FirePit, 12)
                    };
            }
        }

        /// <summary>The big set pieces of each biome, a few per track.</summary>
        private static PropRecipe[] LandmarksOf(Biome biome)
        {
            switch (biome)
            {
                case Biome.Industrial:
                    return new[] { R("GantryCrane", PropMeshes.GantryCrane, 121), R("Smokestack", PropMeshes.Smokestack, 122), R("TankFarm", PropMeshes.TankFarm, 123) };
                case Biome.Snow:
                    return new[] { R("Lodge", PropMeshes.Lodge, 141), R("FrozenTanker", PropMeshes.FrozenTanker, 142), R("IceRidge", PropMeshes.IceRidge, 143) };
                case Biome.City:
                    return new[] { R("OfficeTower", PropMeshes.OfficeTower, 161), R("Apartments", PropMeshes.Apartments, 162), R("NeonHotel", PropMeshes.NeonHotel, 163) };
                default:
                    return new[] { R("Mesa", PropMeshes.Mesa, 101), R("WreckedBus", PropMeshes.WreckedBus, 102), R("GasStation", PropMeshes.GasStation, 103) };
            }
        }

        private static LampRecipe LampOf(Biome biome)
        {
            switch (biome)
            {
                case Biome.Industrial: return PropMeshes.FloodMast;
                case Biome.City: return PropMeshes.StreetLamp;
                default: return PropMeshes.PoleLamp;
            }
        }

        private static string PrefabPath(DeathRaceThemeSpec spec, Biome biome, string name)
        {
            return $"{spec.PropsFolder}/{biome}/{name}.prefab";
        }

        public static GameObject[] Props(DeathRaceThemeSpec spec, Biome biome)
        {
            return Prefabs(spec, biome, PropsOf(biome));
        }

        public static GameObject[] Landmarks(DeathRaceThemeSpec spec, Biome biome)
        {
            return Prefabs(spec, biome, LandmarksOf(biome));
        }

        public static GameObject Lamp(DeathRaceThemeSpec spec, Biome biome)
        {
            return DeathRaceAssets.Require<GameObject>(PrefabPath(spec, biome, LampName));
        }

        public static Material PropMaterial(DeathRaceThemeSpec spec)
        {
            return DeathRaceAssets.Require<Material>($"{spec.MaterialsFolder}/PropPalette.mat");
        }

        private static GameObject[] Prefabs(DeathRaceThemeSpec spec, Biome biome, PropRecipe[] recipes)
        {
            var prefabs = new GameObject[recipes.Length];
            for (int i = 0; i < recipes.Length; i++)
            {
                prefabs[i] = DeathRaceAssets.Require<GameObject>(PrefabPath(spec, biome, recipes[i].Name));
            }
            return prefabs;
        }

        private static void BuildProps()
        {
            // The meshes, once for both looks.
            var meshes = new Dictionary<string, Mesh>();
            var heads = new Dictionary<Biome, Vector3>();
            foreach (Biome biome in (Biome[])Enum.GetValues(typeof(Biome)))
            {
                foreach (PropRecipe recipe in Concat(PropsOf(biome), LandmarksOf(biome)))
                {
                    meshes[$"{biome}/{recipe.Name}"] = SavePropMesh(recipe.Build(recipe.Seed), $"{biome}/{recipe.Name}");
                }
                LowPolyBuilder lamp = LampOf(biome)(900 + (int)biome, out Vector3 head);
                meshes[$"{biome}/{LampName}"] = SavePropMesh(lamp, $"{biome}/{LampName}");
                heads[biome] = head;
            }
            Mesh pool = SavePoolMesh();
            Texture2D poolTexture = DeathRaceAssets.SaveTexture(PoolPicture(128), "Art/Props/LightPool.png", GameAssets.DecalTexture(128));

            foreach (DeathRaceThemeSpec spec in DeathRaceThemeSpec.All)
            {
                Material palette = BuildPalette(spec);
                Material warmPool = SaveAdditive($"{spec.MaterialsFolder}/LightPoolWarm.mat", poolTexture, new Color(1f, 0.6f, 0.26f, 0.7f));
                Material coolPool = SaveAdditive($"{spec.MaterialsFolder}/LightPoolCool.mat", poolTexture, new Color(0.62f, 0.8f, 1f, 0.55f));
                foreach (Biome biome in (Biome[])Enum.GetValues(typeof(Biome)))
                {
                    foreach (PropRecipe recipe in Concat(PropsOf(biome), LandmarksOf(biome)))
                    {
                        SavePropPrefab(spec, biome, recipe.Name, meshes[$"{biome}/{recipe.Name}"], palette);
                    }
                    SaveLampPrefab(spec, biome, meshes[$"{biome}/{LampName}"], palette, heads[biome], pool, biome == Biome.Industrial ? coolPool : warmPool);
                }
            }
        }

        private static IEnumerable<PropRecipe> Concat(PropRecipe[] first, PropRecipe[] second)
        {
            foreach (PropRecipe recipe in first)
            {
                yield return recipe;
            }
            foreach (PropRecipe recipe in second)
            {
                yield return recipe;
            }
        }

        private static Mesh SavePropMesh(LowPolyBuilder builder, string name)
        {
            Mesh mesh = builder.ToMesh(name.Replace('/', '_'));
            return DeathRaceAssets.SaveMesh(mesh, $"{PropMeshesFolder}/{name}.asset");
        }

        /// <summary>A flat 1 m quad facing up, centred on its pivot: the light pool, scaled to <see cref="PoolSize"/>.</summary>
        private static Mesh SavePoolMesh()
        {
            var mesh = new Mesh { name = "LightPool" };
            mesh.vertices = new[] { new Vector3(-0.5f, 0f, -0.5f), new Vector3(0.5f, 0f, -0.5f), new Vector3(0.5f, 0f, 0.5f), new Vector3(-0.5f, 0f, 0.5f) };
            mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
            mesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateBounds();
            return DeathRaceAssets.SaveMesh(mesh, $"{PropMeshesFolder}/LightPool.asset");
        }

        /// <summary>A soft round pool of light, brightest in the middle, falling off smoothly to nothing at the edge.</summary>
        private static Texture2D PoolPicture(int size)
        {
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f;
                    float dy = (y + 0.5f) / size * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a * (3f - 2f * a);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a * a);
                }
            }
            return DeathRaceArtNoise.ToTexture(pixels, size, size);
        }

        /// <summary>The look's palette as textures and one URP Lit material (metallic map, emission at night).</summary>
        private static Material BuildPalette(DeathRaceThemeSpec spec)
        {
            string folder = spec.PropsFolder;
            Texture2D albedo = DeathRaceAssets.SaveTexture(PropPalette.Albedo(spec.Night), $"{folder}/Palette.png", PaletteImport(false));
            Texture2D glow = DeathRaceAssets.SaveTexture(PropPalette.Emission(spec.Night), $"{folder}/PaletteEmission.png", PaletteImport(false));
            Texture2D metal = DeathRaceAssets.SaveTexture(PropPalette.MetalGloss(spec.Night), $"{folder}/PaletteMetal.png", PaletteImport(true));
            return SaveLit($"{spec.MaterialsFolder}/PropPalette.mat", new LitSetup
            {
                Color = Color.white,
                BaseMap = albedo,
                Smoothness = spec.PropSmoothness,
                MetallicMap = metal,
                Metallic = 1f,
                Emission = new Color(PropPalette.MaxGlow, PropPalette.MaxGlow, PropPalette.MaxGlow, 1f),
                EmissionMap = glow
            });
        }

        private static GameObject NewProp(string name, Mesh mesh, Material material)
        {
            var root = new GameObject(name);
            root.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = root.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.BlendProbesAndSkybox;
            return root;
        }

        private static void SavePropPrefab(DeathRaceThemeSpec spec, Biome biome, string name, Mesh mesh, Material material)
        {
            DeathRaceAssets.SavePrefab(NewProp(name, mesh, material), PrefabPath(spec, biome, name));
        }

        private static void SaveLampPrefab(DeathRaceThemeSpec spec, Biome biome, Mesh mesh, Material material, Vector3 head, Mesh pool, Material poolMaterial)
        {
            GameObject root = NewProp(LampName, mesh, material);
            var headObject = new GameObject("Head");
            headObject.transform.SetParent(root.transform, false);
            headObject.transform.localPosition = head;
            if (spec.Night)
            {
                var poolObject = new GameObject("Pool");
                poolObject.transform.SetParent(root.transform, false);
                poolObject.transform.localPosition = new Vector3(head.x, 0.06f, head.z);
                poolObject.transform.localScale = new Vector3(PoolSize, 1f, PoolSize);
                poolObject.AddComponent<MeshFilter>().sharedMesh = pool;
                var renderer = poolObject.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = poolMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            }
            DeathRaceAssets.SavePrefab(root, PrefabPath(spec, biome, LampName));
        }
    }
}
