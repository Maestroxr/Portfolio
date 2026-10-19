using System;
using UnityEngine;

namespace Portfolio.DeathRace.EditorTools
{
    /// <summary>
    /// The surfaces of the four biomes: one set of pictures per biome (Art/Surfaces/&lt;Biome&gt;), shared by both looks,
    /// and one set of materials per look (Art/Materials/&lt;Look&gt;/&lt;Biome&gt;&lt;Part&gt;.mat) that tints them, makes them
    /// dry or wet and lights the barriers' strips at night.
    /// </summary>
    internal static partial class DeathRaceArtBuilder
    {
        public const string SurfacesFolder = "Art/Surfaces";

        /// <summary>The parts of a biome's look, by the name of their material.</summary>
        public const string Ground = "Ground";
        public const string Road = "Road";
        public const string Shoulder = "Shoulder";
        public const string Curb = "Curb";
        public const string Barrier = "Barrier";
        public const string Line = "Line";

        /// <summary>How much of the ground's macro picture shows: its detail tiling relative to the 32 m tile (about 200 m).</summary>
        private const float MacroTiling = 0.15625f;

        private static string SurfacePath(Biome biome, string name)
        {
            return $"{SurfacesFolder}/{biome}/{name}.png";
        }

        /// <summary>The material of <paramref name="part"/> (Ground, Road, Shoulder, Curb, Barrier, Line) of a biome in a look.</summary>
        public static Material SurfaceMaterial(DeathRaceThemeSpec spec, Biome biome, string part)
        {
            return DeathRaceAssets.Require<Material>($"{spec.MaterialsFolder}/{biome}{part}.mat");
        }

        private static void BuildSurfaces()
        {
            foreach (Biome biome in (Biome[])Enum.GetValues(typeof(Biome)))
            {
                Progress($"{biome} surfaces", 0.25f + (int)biome * 0.07f);
                BuildSurfacePictures(biome);
            }
            SurfaceMap line = DeathRaceArtSurfaces.Line();
            DeathRaceAssets.SaveTexture(line.AlbedoTexture(), $"{SurfacesFolder}/Line.png", SurfaceImport(256));
            DeathRaceAssets.SaveTexture(DeathRaceArtSurfaces.BarrierGlow(), $"{SurfacesFolder}/BarrierGlow.png", SurfaceImport(128, true));
            foreach (DeathRaceThemeSpec spec in DeathRaceThemeSpec.All)
            {
                foreach (Biome biome in (Biome[])Enum.GetValues(typeof(Biome)))
                {
                    BuildSurfaceMaterials(spec, biome);
                }
            }
        }

        private static void BuildSurfacePictures(Biome biome)
        {
            SurfaceMap ground = DeathRaceArtSurfaces.Ground(biome);
            Save(ground, SurfacePath(biome, "Ground"), SurfacePath(biome, "GroundNormal"), DeathRaceArtSurfaces.GroundRelief(biome), 1024, false);
            DeathRaceAssets.SaveTexture(DeathRaceArtSurfaces.Macro(biome), SurfacePath(biome, "GroundMacro"), LinearImport(256));
            SurfaceMap road = DeathRaceArtSurfaces.Road(biome);
            Save(road, SurfacePath(biome, "Road"), SurfacePath(biome, "RoadNormal"), DeathRaceArtSurfaces.RoadRelief, 512, false);
            // The night look's road: the same picture after rain, every spot glossier.
            DeathRaceAssets.SaveTexture(DeathRaceArtSurfaces.Wet(road).AlbedoTexture(), SurfacePath(biome, "RoadWet"), SurfaceImport(512));
            Save(DeathRaceArtSurfaces.Shoulder(biome), SurfacePath(biome, "Shoulder"), SurfacePath(biome, "ShoulderNormal"), 4f, 512, false);
            Save(DeathRaceArtSurfaces.Curb(biome), SurfacePath(biome, "Curb"), SurfacePath(biome, "CurbNormal"), 3f, 256, false);
            Save(DeathRaceArtSurfaces.Barrier(biome), SurfacePath(biome, "Barrier"), SurfacePath(biome, "BarrierNormal"), 4f, 512, true);
        }

        private static void Save(SurfaceMap map, string albedoPath, string normalPath, float relief, int maxSize, bool clampV)
        {
            DeathRaceAssets.SaveTexture(map.AlbedoTexture(), albedoPath, SurfaceImport(maxSize, clampV));
            DeathRaceAssets.SaveTexture(map.NormalTexture(relief), normalPath, NormalImport(maxSize, clampV));
        }

        private static void BuildSurfaceMaterials(DeathRaceThemeSpec spec, Biome biome)
        {
            BiomeSpec look = spec.Of(biome);
            Texture2D Picture(string name)
            {
                return DeathRaceAssets.Require<Texture2D>(SurfacePath(biome, name));
            }

            string folder = spec.MaterialsFolder;
            SaveLit($"{folder}/{biome}{Ground}.mat", new LitSetup
            {
                Color = (spec.Night ? new Color(0.82f, 0.84f, 0.9f) : Color.white) * look.GroundColor,
                BaseMap = Picture("Ground"),
                NormalMap = Picture("GroundNormal"),
                NormalScale = 1f,
                Smoothness = look.GroundSmoothness,
                SmoothnessFromAlpha = true,
                Detail = Picture("GroundMacro"),
                DetailTiling = MacroTiling
            });
            SaveLit($"{folder}/{biome}{Road}.mat", new LitSetup
            {
                Color = look.RoadTint,
                BaseMap = Picture(spec.Night ? "RoadWet" : "Road"),
                NormalMap = Picture("RoadNormal"),
                NormalScale = 1f,
                Smoothness = look.RoadSmoothness,
                SmoothnessFromAlpha = true
            });
            SaveLit($"{folder}/{biome}{Shoulder}.mat", new LitSetup
            {
                Color = (spec.Night ? new Color(0.85f, 0.86f, 0.9f) : Color.white) * look.GroundColor,
                BaseMap = Picture("Shoulder"),
                NormalMap = Picture("ShoulderNormal"),
                NormalScale = 1f,
                Smoothness = Mathf.Lerp(look.GroundSmoothness, look.RoadSmoothness, 0.5f),
                SmoothnessFromAlpha = true
            });
            SaveLit($"{folder}/{biome}{Curb}.mat", new LitSetup
            {
                Color = Color.white,
                BaseMap = Picture("Curb"),
                NormalMap = Picture("CurbNormal"),
                NormalScale = 1f,
                Smoothness = spec.Night ? 0.85f : 0.5f,
                SmoothnessFromAlpha = true
            });
            bool neon = look.Neon.maxColorComponent > 0f;
            SaveLit($"{folder}/{biome}{Barrier}.mat", new LitSetup
            {
                Color = Color.white,
                BaseMap = Picture("Barrier"),
                NormalMap = Picture("BarrierNormal"),
                NormalScale = 1f,
                Smoothness = spec.BarrierSmoothness,
                SmoothnessFromAlpha = true,
                Emission = neon ? look.Neon : Color.black,
                EmissionMap = neon ? DeathRaceAssets.Require<Texture2D>($"{SurfacesFolder}/BarrierGlow.png") : null
            });
            SaveLit($"{folder}/{biome}{Line}.mat", new LitSetup
            {
                Color = spec.Night ? new Color(0.9f, 0.9f, 0.92f) : Color.white,
                BaseMap = DeathRaceAssets.Require<Texture2D>($"{SurfacesFolder}/Line.png"),
                Smoothness = spec.Night ? 0.8f : 0.45f,
                SmoothnessFromAlpha = true
            });
        }
    }
}
