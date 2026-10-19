using System;
using UnityEngine;

namespace Portfolio.DeathRace.EditorTools
{
    internal static partial class DeathRacePrefabBuilder
    {
        private const string ItemArt = "Art/Items";
        private const string ItemMeshFolder = ItemArt + "/Meshes";
        private const string ItemMaterialFolder = ItemArt + "/Materials";

        private static Material ItemMaterial(string name)
        {
            return DeathRaceAssets.Require<Material>($"{ItemMaterialFolder}/{name}.mat");
        }

        private static Mesh ItemMesh(string name)
        {
            return DeathRaceAssets.Require<Mesh>($"{ItemMeshFolder}/{name}.asset");
        }

        /// <summary>The meshes and materials of the mine, the rocket and the pickups.</summary>
        private static void BuildItemArt()
        {
            Texture2D map = CarArt.PaletteMap;
            Texture2D glow = CarArt.PaletteGlow;
            // One palette material for every item; its glow (the mine's lamp, the rings) is bright by day and by night.
            DeathRaceAssets.SaveMaterial($"{ItemMaterialFolder}/ItemPalette.mat", CarArt.Lit, m =>
                CarArt.Surface(m, Color.white, map, 0.42f, 0.12f, glow, Color.white * 2.2f));
            var dot = DeathRaceAssets.Load<Texture2D>($"{CarArt.TextureFolder}/Glow.png");
            foreach (PickupKind kind in (PickupKind[])Enum.GetValues(typeof(PickupKind)))
            {
                Color color = ItemMeshes.GlowColor(kind);
                color.a = 0.6f;
                DeathRaceAssets.SaveMaterial($"{ItemMaterialFolder}/Glow{kind}.mat", CarArt.ParticlesUnlit, m => CarArt.Transparent(m, dot, color, Blending.Additive, false, -3));
                SaveMesh(ItemMeshes.Ring(ItemMeshes.Glow(kind)), $"{ItemMeshFolder}/Ring{kind}.asset");
                SaveMesh(ItemMeshes.Pickup(kind), $"{ItemMeshFolder}/{kind}.asset");
            }
            DeathRaceAssets.SaveMaterial($"{ItemMaterialFolder}/RocketFlame.mat", CarArt.ParticlesUnlit, m => CarArt.Transparent(m, dot, Color.white, Blending.Additive));
            SaveMesh(ItemMeshes.Mine(), $"{ItemMeshFolder}/Mine.asset");
            SaveMesh(ItemMeshes.MineBeacon(), $"{ItemMeshFolder}/MineBeacon.asset");
            SaveMesh(ItemMeshes.Rocket(), $"{ItemMeshFolder}/Rocket.asset");
            DeathRaceAssets.SaveMesh(CarMeshes.RocketFlameMesh(), $"{ItemMeshFolder}/RocketFlame.asset");

            // The special weapons and the plasma bolt: the oil drawn over the scorch marks (they sit under it), the bolt's
            // violet glow added on top of whatever it flies over.
            Texture2D oil = DeathRaceAssets.SaveTexture(ItemMeshes.OilTexture(), $"{ItemArt}/Textures/Oil.png", CarArt.SoftImport);
            DeathRaceAssets.SaveMaterial($"{ItemMaterialFolder}/OilSlick.mat", CarArt.ParticlesUnlit,
                m => CarArt.Transparent(m, oil, Color.white, Blending.Alpha, false, -15));
            Color plasma = EffectFallbacks.PlasmaColor;
            plasma.a = 0.85f;
            DeathRaceAssets.SaveMaterial($"{ItemMaterialFolder}/PlasmaGlow.mat", CarArt.ParticlesUnlit,
                m => CarArt.Transparent(m, dot, plasma, Blending.Additive, false, -2));
            DeathRaceAssets.SaveMesh(ItemMeshes.OilPuddle(), $"{ItemMeshFolder}/OilPuddle.asset");
            SaveMesh(ItemMeshes.MortarShell(), $"{ItemMeshFolder}/MortarShell.asset");
            SaveMesh(ItemMeshes.PlasmaCore(), $"{ItemMeshFolder}/PlasmaCore.asset");
        }

        public static GameObject ItemPrefab(string name)
        {
            return DeathRaceAssets.Load<GameObject>($"{ItemFolder}/{name}.prefab");
        }

        /// <summary>
        /// The mine (body + "Beacon", the lamp the race blinks), the rocket (+z forward, a "Flame" cone at its nozzle; the
        /// race draws its smoke trail), the oil slick's puddle (1 m radius), the mortar shell and the plasma bolt (+z
        /// forward; the race adds their trails) and a pickup of each kind: "Base" (a soft glow and a crisp ring on the
        /// ground) and "Item" (the model, resting 0.35 m up; the race spins and bobs it).
        /// </summary>
        private static void BuildItems()
        {
            Material palette = ItemMaterial("ItemPalette");
            Mesh quad = DeathRaceAssets.Require<Mesh>($"{CarArt.MeshFolder}/GroundQuad.asset");

            var mine = new GameObject("Mine");
            Part(mine.transform, "Body", ItemMesh("Mine"), palette, Vector3.zero, Quaternion.identity);
            Part(mine.transform, "Beacon", ItemMesh("MineBeacon"), palette, Vector3.zero, Quaternion.identity, false);
            DeathRaceAssets.SavePrefab(mine, $"{ItemFolder}/Mine.prefab");

            var rocket = new GameObject("Rocket");
            Part(rocket.transform, "Body", ItemMesh("Rocket"), palette, Vector3.zero, Quaternion.identity);
            Part(rocket.transform, "Flame", ItemMesh("RocketFlame"), ItemMaterial("RocketFlame"), new Vector3(0f, 0f, -0.66f), Quaternion.Euler(0f, 180f, 0f), false);
            DeathRaceAssets.SavePrefab(rocket, $"{ItemFolder}/Rocket.prefab");

            // The oil slick: the puddle, 1 m in radius on the root (the race scales the root to the slick and fades it).
            var oil = new GameObject("OilSlick");
            Part(oil.transform, "Puddle", ItemMesh("OilPuddle"), ItemMaterial("OilSlick"), Vector3.zero, Quaternion.identity, false);
            DeathRaceAssets.SavePrefab(oil, $"{ItemFolder}/OilSlick.prefab");

            // Half as big again as the mesh: it has to read as it arcs over the cars, forty metres under the camera.
            var shell = new GameObject("MortarShell");
            MeshRenderer shellBody = Part(shell.transform, "Body", ItemMesh("MortarShell"), palette, Vector3.zero, Quaternion.identity);
            shellBody.transform.localScale = Vector3.one * 1.5f;
            DeathRaceAssets.SavePrefab(shell, $"{ItemFolder}/MortarShell.prefab");

            // The plasma bolt: its white-hot core and a violet glow lying under it (seen from above, a soft halo round the core).
            var bolt = new GameObject("PlasmaBolt");
            Part(bolt.transform, "Core", ItemMesh("PlasmaCore"), palette, Vector3.zero, Quaternion.identity, false);
            MeshRenderer halo = Part(bolt.transform, "Glow", quad, ItemMaterial("PlasmaGlow"), new Vector3(0f, -0.05f, 0f), Quaternion.identity, false);
            halo.transform.localScale = new Vector3(2.6f, 1f, 3.8f);
            DeathRaceAssets.SavePrefab(bolt, $"{ItemFolder}/PlasmaBolt.prefab");

            foreach (PickupKind kind in (PickupKind[])Enum.GetValues(typeof(PickupKind)))
            {
                var pickup = new GameObject(kind.ToString());
                Transform ground = Mount(pickup.transform, "Base", Vector3.zero, Quaternion.identity);
                MeshRenderer glow = Part(ground, "Glow", quad, ItemMaterial($"Glow{kind}"), new Vector3(0f, 0.04f, 0f), Quaternion.identity, false);
                glow.transform.localScale = new Vector3(3.2f, 1f, 3.2f);
                Part(ground, "Ring", ItemMesh($"Ring{kind}"), palette, Vector3.zero, Quaternion.identity, false);
                MeshRenderer model = Part(pickup.transform, "Item", ItemMesh(kind.ToString()), palette, new Vector3(0f, 0.35f, 0f), Quaternion.identity);
                model.transform.localScale = Vector3.one * 1.2f;
                DeathRaceAssets.SavePrefab(pickup, $"{ItemFolder}/{kind}.prefab");
            }
        }

        /// <summary>The effect prefab of a content field.</summary>
        private static GameObject Effect(string name)
        {
            return DeathRaceEffectsBuilder.Prefab(name);
        }
    }
}
