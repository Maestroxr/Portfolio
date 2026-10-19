using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Portfolio.DeathRace.EditorTools
{
    /// <summary>
    /// Builds the moving things of a race from code: the car models (visual prefabs with a CarModelView), the mine, the
    /// rocket, the oil slick, the mortar shell, the plasma bolt, the four pickups, the effects and the sounds. <see cref="BuildArt"/> writes the meshes, textures,
    /// materials and sounds; <see cref="BuildAll"/> the prefabs and the car pictures; <see cref="FillContent"/> points the
    /// content asset at them. Everything is generated (no downloads) and seeded, so a rebuild writes the same files.
    /// </summary>
    internal static partial class DeathRacePrefabBuilder
    {
        public const string CarFolder = "Prefabs/Cars";
        public const string ItemFolder = "Prefabs/Items";
        public const string EffectFolder = "Prefabs/Effects";

        /// <summary>The meshes, materials, textures and sounds of the cars, items and effects (before the themes).</summary>
        public static void BuildArt()
        {
            Progress("Car materials", 0.05f);
            CarArt.BuildMaterials();
            Progress("Car meshes", 0.15f);
            BuildCarMeshes();
            Progress("Effects", 0.25f);
            DeathRaceEffectsBuilder.BuildArt();
            Progress("Items", 0.3f);
            BuildItemArt();
            Progress("Sounds and music", 0.35f);
            DeathRaceSounds.BuildAll();
        }

        /// <summary>The prefabs of the cars, items and effects, then the pictures of the cars (they need the prefabs).</summary>
        public static void BuildAll()
        {
            Progress("Car prefabs", 0.6f);
            BuildCars();
            Progress("Item prefabs", 0.7f);
            BuildItems();
            Progress("Effect prefabs", 0.75f);
            DeathRaceEffectsBuilder.BuildPrefabs();
            Progress("Car pictures", 0.8f);
            CarArt.RenderPictures();
        }

        /// <summary>Points every field of the content asset at the built prefabs, effects, materials and sounds.</summary>
        public static void FillContent(DeathRaceContent content)
        {
            var cars = new GameObject[CarSpecs.Count];
            for (int i = 0; i < cars.Length; i++)
            {
                cars[i] = CarPrefab((CarModel)i);
            }
            content.carPrefabs = cars;
            content.mine = ItemPrefab("Mine");
            content.rocket = ItemPrefab("Rocket");
            content.oilSlick = ItemPrefab("OilSlick");
            content.mortarShell = ItemPrefab("MortarShell");
            content.plasmaBolt = ItemPrefab("PlasmaBolt");
            var pickups = new GameObject[System.Enum.GetValues(typeof(PickupKind)).Length];
            for (int i = 0; i < pickups.Length; i++)
            {
                pickups[i] = ItemPrefab(((PickupKind)i).ToString());
            }
            content.pickups = pickups;
            content.explosion = Effect("Explosion");
            content.bigExplosion = Effect("BigExplosion");
            content.sparks = Effect("Sparks");
            content.smoke = Effect("Smoke");
            content.blackSmoke = Effect("BlackSmoke");
            content.muzzleFlash = Effect("MuzzleFlash");
            content.fire = Effect("Fire");
            content.nitroFlame = Effect("NitroFlame");
            content.dust = Effect("Dust");
            content.scorch = Effect("Scorch");
            content.flameJet = Effect("FlameJet");
            content.plasmaBurst = Effect("PlasmaBurst");
            content.empBlast = Effect("EmpBlast");
            content.electric = Effect("Electric");
            content.weld = Effect("Weld");
            content.tracer = DeathRaceEffectsBuilder.EffectMaterial("Tracer");
            content.skid = DeathRaceEffectsBuilder.EffectMaterial("Skid");
            content.sounds = content.sounds ?? new RaceSounds();
            DeathRaceSounds.Fill(content.sounds);
        }

        public static GameObject CarPrefab(CarModel model)
        {
            return DeathRaceAssets.Load<GameObject>($"{CarFolder}/{model}.prefab");
        }

        private static void Progress(string step, float progress)
        {
            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayProgressBar("Death Race: cars, items and effects", step, progress);
            }
        }

        // ------------------------------------------------------------------ prefab parts

        /// <summary>A child with a mesh renderer.</summary>
        private static MeshRenderer Part(Transform parent, string name, Mesh mesh, Material material, Vector3 position, Quaternion rotation, bool shadows = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = rotation;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            renderer.receiveShadows = shadows;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = shadows ? ReflectionProbeUsage.BlendProbes : ReflectionProbeUsage.Off;
            return renderer;
        }

        /// <summary>An empty child marking a point (a muzzle, a mount, an exhaust).</summary>
        private static Transform Mount(Transform parent, string name, Vector3 position, Quaternion rotation)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = rotation;
            return go.transform;
        }

        /// <summary>Saves a built model as a mesh asset (in place: its GUID and the prefabs using it are kept).</summary>
        private static Mesh SaveMesh(Gamebox.Editor.LowPolyBuilder builder, string relative)
        {
            return DeathRaceAssets.SaveMesh(builder.ToMesh(System.IO.Path.GetFileNameWithoutExtension(relative)), relative);
        }
    }
}
