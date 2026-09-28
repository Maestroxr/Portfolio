using System.Collections.Generic;
using System.Linq;
using Gamebox;
using Gamebox.Launcher;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Portfolio.EndlessRunner.EditorTools
{
    /// <summary>
    /// Creates the game content from the generated art: the worlds and the piece catalog of every theme, the game
    /// themes themselves (the classic look and the Night Shift), the run settings of every level, the campaign
    /// (eight levels and the endless run, laid out in the classic worlds), the post-processing profile and the
    /// launcher entry, which points at the classic theme and lists both.
    /// </summary>
    internal static class RunnerContentBuilder
    {
        private const TrackFeatures Basics = TrackFeatures.Hurdles | TrackFeatures.Blocks;

        public static void BuildAll()
        {
            var themes = new List<RunnerGameTheme>();
            Dictionary<string, RunnerTheme> classic = null;
            foreach (ThemeArt art in RunnerThemes.All)
            {
                Dictionary<string, RunnerTheme> biomes = BuildBiomes(art);
                PieceCatalog catalog = BuildCatalog(art);
                themes.Add(BuildGameTheme(art, biomes, catalog));
                if (art.IsClassic)
                {
                    classic = biomes;
                }
            }
            BuildCampaign(classic);
            BuildVolumeProfile();
            UpdateGameDefinition(themes);
            AssetDatabase.SaveAssets();
        }

        // ------------------------------------------------------------------ worlds

        /// <summary>The worlds of a theme, in the order of the campaign.</summary>
        private static Dictionary<string, RunnerTheme> BuildBiomes(ThemeArt art)
        {
            return art.IsClassic ? ClassicBiomes(art) : NightShiftBiomes(art);
        }

        private static Dictionary<string, RunnerTheme> ClassicBiomes(ThemeArt art)
        {
            return new Dictionary<string, RunnerTheme>
            {
                ["Meadow"] = Biome(art, "Meadow", "Sunny Meadows", t =>
                {
                    Sky(t, C(0.24f, 0.52f, 0.96f), C(0.74f, 0.89f, 1f), C(0.62f, 0.77f, 0.64f), C(1f, 0.97f, 0.86f), 0.045f, 0f);
                    Clouds(t, C(1f, 1f, 1f), 0.5f, C(0.56f, 0.72f, 0.9f), C(0.42f, 0.66f, 0.52f), 0.085f);
                    Light(t, C(1f, 0.96f, 0.88f), 1.15f, new Vector3(42f, -30f, 0f), C(0.62f, 0.72f, 0.88f), C(0.62f, 0.68f, 0.62f), C(0.4f, 0.44f, 0.32f), 0.55f);
                    Air(t, C(0.76f, 0.88f, 0.98f), 0.0105f, AmbientEffect.Pollen, C(0.72f, 0.66f, 0.5f, 0.6f));
                    t.chasmFill = RunnerArtBuilder.Material(art, "Water");
                    t.scenery = Items(art, "Meadow",
                        ("TreeRound", 3f, 8f, 34f, 0.8f, 1.3f), ("AppleTree", 1.5f, 8f, 30f, 0.8f, 1.2f), ("Poplar", 1.5f, 9f, 36f, 0.8f, 1.3f),
                        ("Pine", 1.5f, 10f, 40f, 0.8f, 1.4f), ("Bush", 2f, 6.5f, 22f, 0.8f, 1.3f), ("Flowers", 2.5f, 6f, 16f, 0.9f, 1.3f),
                        ("Rock", 1f, 7f, 30f, 0.6f, 1.4f), ("Mushroom", 0.6f, 6.5f, 18f, 0.8f, 1.4f), ("Cottage", 0.25f, 18f, 40f, 1f, 1.2f));
                    t.sceneryPerTile = 7;
                    Roadside(t, art, "Meadow", "Fence", 1, 5.3f);
                    t.accent = C(0.34f, 0.72f, 0.3f);
                }),
                ["Desert"] = Biome(art, "Desert", "Sunset Canyon", t =>
                {
                    Sky(t, C(0.26f, 0.24f, 0.55f), C(1f, 0.68f, 0.42f), C(0.86f, 0.62f, 0.46f), C(1f, 0.86f, 0.6f), 0.07f, 0.1f);
                    Clouds(t, C(1f, 0.74f, 0.6f), 0.35f, C(0.76f, 0.45f, 0.42f), C(0.6f, 0.32f, 0.28f), 0.1f);
                    Light(t, C(1f, 0.8f, 0.58f), 1.1f, new Vector3(24f, -60f, 0f), C(0.72f, 0.56f, 0.62f), C(0.82f, 0.62f, 0.52f), C(0.46f, 0.33f, 0.26f), 0.6f);
                    Air(t, C(0.98f, 0.72f, 0.52f), 0.0105f, AmbientEffect.Dust, C(0.9f, 0.72f, 0.5f, 0.6f));
                    t.chasmFill = RunnerArtBuilder.Material(art, "Water");
                    t.scenery = Items(art, "Desert",
                        ("Saguaro", 3f, 7f, 34f, 0.8f, 1.3f), ("SaguaroTall", 1.5f, 9f, 36f, 1f, 1.4f), ("BarrelCactus", 2f, 6.5f, 20f, 0.8f, 1.4f),
                        ("DesertRock", 1.5f, 7f, 30f, 0.7f, 1.5f), ("DeadBush", 1.5f, 6.5f, 24f, 0.8f, 1.3f), ("PalmTree", 0.8f, 9f, 30f, 0.9f, 1.2f),
                        ("Mesa", 0.35f, 34f, 70f, 0.8f, 1.4f));
                    t.sceneryPerTile = 6;
                    Roadside(t, art, "Desert", "Signpost", 5, 5.4f);
                    t.accent = C(0.95f, 0.55f, 0.24f);
                }),
                ["Snow"] = Biome(art, "Snow", "Frosty Peaks", t =>
                {
                    Sky(t, C(0.36f, 0.6f, 0.92f), C(0.86f, 0.93f, 1f), C(0.86f, 0.9f, 0.96f), C(1f, 1f, 0.95f), 0.04f, 0f);
                    Clouds(t, C(1f, 1f, 1f), 0.6f, C(0.8f, 0.88f, 0.98f), C(0.93f, 0.96f, 1f), 0.13f);
                    Light(t, C(1f, 0.98f, 0.95f), 1.05f, new Vector3(36f, -20f, 0f), C(0.76f, 0.83f, 0.96f), C(0.8f, 0.85f, 0.93f), C(0.7f, 0.75f, 0.83f), 0.45f);
                    Air(t, C(0.86f, 0.92f, 1f), 0.012f, AmbientEffect.Snow, C(0.95f, 0.97f, 1f, 0.7f));
                    t.chasmFill = RunnerArtBuilder.Material(art, "IceWater");
                    t.scenery = Items(art, "Snow",
                        ("SnowPine", 3f, 7.5f, 36f, 0.8f, 1.3f), ("SnowPineTall", 1.5f, 10f, 40f, 0.9f, 1.3f), ("Snowman", 0.5f, 7f, 20f, 0.9f, 1.2f),
                        ("IceRock", 1.2f, 7f, 30f, 0.6f, 1.4f), ("IceCrystals", 0.8f, 7f, 26f, 0.8f, 1.5f), ("SnowBush", 1.5f, 6.5f, 24f, 0.8f, 1.4f));
                    t.sceneryPerTile = 6;
                    Roadside(t, art, "Snow", "SnowPole", 2, 4.9f);
                    t.accent = C(0.38f, 0.66f, 1f);
                }),
                ["Night"] = Biome(art, "Night", "Moonlit Woods", t =>
                {
                    Sky(t, C(0.03f, 0.04f, 0.15f), C(0.17f, 0.21f, 0.43f), C(0.1f, 0.12f, 0.25f), C(0.92f, 0.95f, 1f), 0.05f, 1f);
                    Clouds(t, C(0.3f, 0.35f, 0.55f, 0.6f), 0.35f, C(0.12f, 0.15f, 0.3f), C(0.07f, 0.09f, 0.2f), 0.09f);
                    Light(t, C(0.55f, 0.66f, 1f), 0.6f, new Vector3(50f, 150f, 0f), C(0.24f, 0.28f, 0.48f), C(0.2f, 0.22f, 0.38f), C(0.1f, 0.1f, 0.17f), 0.5f);
                    Air(t, C(0.13f, 0.16f, 0.33f), 0.0115f, AmbientEffect.Fireflies, C(0.4f, 0.45f, 0.6f, 0.5f));
                    t.chasmFill = RunnerArtBuilder.Material(art, "GlowWater");
                    t.scenery = Items(art, "Night",
                        ("NightTree", 2.5f, 8f, 34f, 0.8f, 1.3f), ("NightPine", 1.5f, 9f, 38f, 0.9f, 1.4f), ("GlowMushrooms", 2f, 6.5f, 22f, 0.8f, 1.4f),
                        ("NightCrystals", 1.2f, 7f, 28f, 0.8f, 1.4f), ("NightRock", 1f, 7f, 30f, 0.6f, 1.4f));
                    t.sceneryPerTile = 6;
                    Roadside(t, art, "Night", "LampPost", 3, 5.2f);
                    t.accent = C(0.55f, 0.45f, 1f);
                }),
                ["Volcano"] = Biome(art, "Volcano", "Lava Land", t =>
                {
                    Sky(t, C(0.12f, 0.04f, 0.05f), C(0.86f, 0.33f, 0.13f), C(0.3f, 0.1f, 0.06f), C(1f, 0.56f, 0.26f), 0.06f, 0.2f);
                    Clouds(t, C(0.36f, 0.18f, 0.15f), 0.55f, C(0.36f, 0.12f, 0.1f), C(0.18f, 0.07f, 0.07f), 0.12f);
                    Light(t, C(1f, 0.58f, 0.38f), 0.95f, new Vector3(30f, 40f, 0f), C(0.5f, 0.26f, 0.21f), C(0.56f, 0.29f, 0.21f), C(0.3f, 0.12f, 0.08f), 0.6f);
                    Air(t, C(0.56f, 0.21f, 0.11f), 0.012f, AmbientEffect.Embers, C(0.35f, 0.3f, 0.3f, 0.6f));
                    t.chasmFill = RunnerArtBuilder.Material(art, "Lava");
                    t.scenery = Items(art, "Volcano",
                        ("BasaltColumns", 2f, 7f, 32f, 0.8f, 1.4f), ("DeadTree", 1.5f, 7.5f, 30f, 0.8f, 1.3f), ("LavaRock", 2f, 7f, 28f, 0.7f, 1.4f),
                        ("AshRock", 1f, 7f, 30f, 0.6f, 1.4f), ("Volcano", 0.08f, 60f, 110f, 0.8f, 1.2f));
                    t.sceneryPerTile = 6;
                    Roadside(t, art, "Volcano", "Torch", 3, 5.2f);
                    t.accent = C(1f, 0.4f, 0.15f);
                })
            };
        }

        /// <summary>The night city: downtown, the subway, the docks, the highway and the rooftops at dawn.</summary>
        private static Dictionary<string, RunnerTheme> NightShiftBiomes(ThemeArt art)
        {
            return new Dictionary<string, RunnerTheme>
            {
                ["Downtown"] = Biome(art, "Downtown", "Downtown", t =>
                {
                    Sky(t, C(0.02f, 0.03f, 0.06f), C(0.5f, 0.28f, 0.2f), C(0.06f, 0.06f, 0.08f), C(0.8f, 0.85f, 0.95f), 0.03f, 0.25f);
                    Clouds(t, C(0.14f, 0.11f, 0.15f, 0.7f), 0.5f, C(0.1f, 0.1f, 0.13f), C(0.04f, 0.045f, 0.06f), 0.16f);
                    Light(t, C(0.62f, 0.66f, 0.85f), 0.5f, new Vector3(55f, 140f, 0f), C(0.2f, 0.22f, 0.3f), C(0.24f, 0.2f, 0.22f), C(0.08f, 0.08f, 0.1f), 0.5f);
                    Air(t, C(0.1f, 0.1f, 0.13f), 0.012f, AmbientEffect.Rain, C(0.5f, 0.55f, 0.65f, 0.5f));
                    t.chasmFill = RunnerArtBuilder.Material(art, "Canal");
                    t.scenery = Items(art, "Downtown",
                        ("BlockBrick", 1.5f, 13f, 30f, 0.9f, 1.1f, Square), ("BlockConcrete", 1.5f, 12f, 32f, 0.9f, 1.15f, Square),
                        ("BlockDark", 1f, 15f, 34f, 0.9f, 1.1f, Square), ("BlockTall", 0.8f, 18f, 40f, 0.9f, 1.2f, Square),
                        ("ShopPink", 1f, 9.5f, 14f, 0.95f, 1.05f, Road), ("ShopCyan", 1f, 9.5f, 14f, 0.95f, 1.05f, Road),
                        ("Taxi", 1.2f, 6.2f, 7.2f, 1f, 1f, Along), ("TaxiDark", 0.6f, 6.2f, 7.2f, 1f, 1f, Along),
                        ("ConcreteBarrier", 0.8f, 5.6f, 6.5f, 1f, 1f, Along), ("Hydrant", 0.7f, 5.6f, 6.5f, 0.9f, 1.1f, Any),
                        ("TrashCans", 0.8f, 6f, 9f, 0.9f, 1.1f, Square), ("Billboard", 0.3f, 12f, 22f, 0.9f, 1.2f, Road));
                    t.sceneryPerTile = 7;
                    Roadside(t, art, "Downtown", "StreetLamp", 2, 5.2f);
                    t.accent = C(0.95f, 0.65f, 0.2f);
                }),
                ["Subway"] = Biome(art, "Subway", "Subway", t =>
                {
                    Sky(t, C(0.01f, 0.01f, 0.012f), C(0.06f, 0.05f, 0.04f), C(0.02f, 0.02f, 0.02f), C(0.03f, 0.03f, 0.03f), 0.005f, 0f);
                    Clouds(t, C(0f, 0f, 0f, 0f), 0f, C(0.03f, 0.03f, 0.03f), C(0.02f, 0.02f, 0.02f), 0.05f);
                    Light(t, C(0.9f, 0.78f, 0.55f), 0.65f, new Vector3(60f, 20f, 0f), C(0.26f, 0.24f, 0.22f), C(0.3f, 0.28f, 0.25f), C(0.08f, 0.08f, 0.08f), 0.4f);
                    Air(t, C(0.05f, 0.05f, 0.055f), 0.022f, AmbientEffect.Ash, C(0.4f, 0.4f, 0.4f, 0.5f));
                    t.chasmFill = RunnerArtBuilder.Material(art, "TrackBed");
                    t.scenery = Items(art, "Subway",
                        ("TunnelArch", 1.2f, 0f, 0f, 1f, 1f, Along), ("Pillar", 2f, 6.5f, 8.5f, 0.9f, 1.1f, Square),
                        ("PillarTall", 1.5f, 6.5f, 8.5f, 0.9f, 1.1f, Square), ("Signal", 1f, 5.8f, 6.6f, 1f, 1f, Oncoming),
                        ("SignalGreen", 0.7f, 5.8f, 6.6f, 1f, 1f, Oncoming), ("CableRun", 1.2f, 6.2f, 7.5f, 1f, 1f, Along));
                    t.sceneryPerTile = 4;
                    Roadside(t, art, "Subway", "TunnelWall", 1, 5f);
                    t.accent = C(1f, 0.72f, 0.2f);
                }),
                ["Docks"] = Biome(art, "Docks", "The Docks", t =>
                {
                    Sky(t, C(0.12f, 0.14f, 0.18f), C(0.36f, 0.34f, 0.36f), C(0.2f, 0.2f, 0.22f), C(0.7f, 0.68f, 0.66f), 0.02f, 0f);
                    Clouds(t, C(0.3f, 0.31f, 0.34f, 0.9f), 0.85f, C(0.16f, 0.17f, 0.2f), C(0.1f, 0.11f, 0.13f), 0.1f);
                    Light(t, C(0.6f, 0.62f, 0.68f), 0.55f, new Vector3(35f, -70f, 0f), C(0.3f, 0.32f, 0.36f), C(0.26f, 0.27f, 0.3f), C(0.12f, 0.12f, 0.13f), 0.3f);
                    Air(t, C(0.3f, 0.32f, 0.36f), 0.028f, AmbientEffect.None, C(0.5f, 0.52f, 0.55f, 0.5f));
                    t.chasmFill = RunnerArtBuilder.Material(art, "HarbourWater");
                    t.scenery = Items(art, "Docks",
                        ("ContainerStack", 2f, 8f, 30f, 0.95f, 1.05f, Along), ("ContainerStackB", 1.5f, 8f, 30f, 0.95f, 1.05f, Along),
                        ("ContainerRed", 1.5f, 7f, 24f, 0.95f, 1.05f, Along), ("ContainerBlue", 1.5f, 7f, 24f, 0.95f, 1.05f, Along),
                        ("Crane", 0.3f, 24f, 60f, 0.9f, 1.2f, Along), ("CratePile", 1.5f, 6f, 14f, 0.9f, 1.1f, Square),
                        ("Bollard", 1f, 5.5f, 6.5f, 0.9f, 1.1f, Any));
                    t.sceneryPerTile = 6;
                    Roadside(t, art, "Docks", "FloodlightMast", 3, 5.4f);
                    t.accent = C(0.55f, 0.75f, 0.85f);
                }),
                ["Highway"] = Biome(art, "Highway", "The Highway", t =>
                {
                    Sky(t, C(0.03f, 0.03f, 0.07f), C(0.5f, 0.28f, 0.12f), C(0.08f, 0.07f, 0.08f), C(0.9f, 0.9f, 1f), 0.025f, 0.15f);
                    Clouds(t, C(0.16f, 0.12f, 0.12f, 0.8f), 0.6f, C(0.07f, 0.07f, 0.1f), C(0.04f, 0.04f, 0.06f), 0.08f);
                    Light(t, C(0.95f, 0.75f, 0.45f), 0.6f, new Vector3(48f, -30f, 0f), C(0.2f, 0.16f, 0.14f), C(0.2f, 0.15f, 0.12f), C(0.07f, 0.06f, 0.06f), 0.5f);
                    Air(t, C(0.12f, 0.09f, 0.09f), 0.011f, AmbientEffect.Rain, C(0.5f, 0.45f, 0.42f, 0.5f));
                    t.chasmFill = RunnerArtBuilder.Material(art, "Traffic");
                    t.scenery = Items(art, "Highway",
                        ("Cone", 1.5f, 5.4f, 6.2f, 0.9f, 1.1f, Any), ("ConeRow", 0.8f, 5.6f, 6.5f, 1f, 1f, Along),
                        ("RoadSign", 0.8f, 7f, 9f, 1f, 1.1f, Oncoming), ("RoadSignB", 0.6f, 7f, 9f, 1f, 1.1f, Oncoming),
                        ("Overpass", 0.22f, 0f, 0f, 1f, 1f, Along), ("HighwayLight", 0.9f, 8f, 9f, 1f, 1f, Road),
                        ("Wreck", 0.4f, 6.5f, 8f, 1f, 1f, Along), ("ConcreteBarrier", 1f, 5.6f, 6.5f, 1f, 1f, Along));
                    t.sceneryPerTile = 4;
                    Roadside(t, art, "Highway", "GuardRail", 1, 5f);
                    t.accent = C(1f, 0.5f, 0.15f);
                }),
                ["Rooftops"] = Biome(art, "Rooftops", "The Rooftops", t =>
                {
                    Sky(t, C(0.2f, 0.24f, 0.36f), C(0.85f, 0.55f, 0.4f), C(0.3f, 0.3f, 0.36f), C(1f, 0.75f, 0.5f), 0.05f, 0.05f);
                    Clouds(t, C(0.6f, 0.5f, 0.55f, 0.8f), 0.55f, C(0.35f, 0.33f, 0.4f), C(0.2f, 0.2f, 0.26f), 0.14f);
                    Light(t, C(1f, 0.72f, 0.5f), 0.75f, new Vector3(18f, 75f, 0f), C(0.4f, 0.4f, 0.5f), C(0.5f, 0.4f, 0.42f), C(0.18f, 0.17f, 0.2f), 0.55f);
                    Air(t, C(0.55f, 0.45f, 0.48f), 0.009f, AmbientEffect.Ash, C(0.6f, 0.55f, 0.55f, 0.5f));
                    t.chasmFill = RunnerArtBuilder.Material(art, "StreetBelow");
                    t.scenery = Items(art, "Rooftops",
                        ("AcUnit", 2f, 5.8f, 9f, 0.9f, 1.1f, Square), ("AcUnitTwin", 1.2f, 6f, 9f, 0.9f, 1.1f, Square),
                        ("WaterTower", 0.7f, 7f, 12f, 0.9f, 1.2f, Any), ("Antenna", 1.2f, 6f, 10f, 0.9f, 1.2f, Any),
                        ("AntennaTall", 0.8f, 6f, 10f, 0.9f, 1.2f, Any), ("Skylight", 1.2f, 6f, 9f, 0.9f, 1.1f, Along),
                        ("Vent", 1.5f, 5.6f, 8f, 0.9f, 1.1f, Any), ("Skyscraper", 0.6f, 30f, 70f, 0.9f, 1.4f, Square),
                        ("SkyscraperB", 0.6f, 30f, 70f, 0.9f, 1.4f, Square));
                    t.sceneryPerTile = 6;
                    Roadside(t, art, "Rooftops", "Parapet", 1, 5f);
                    t.accent = C(0.9f, 0.55f, 0.45f);
                })
            };
        }

        private static RunnerTheme Biome(ThemeArt art, string key, string displayName, System.Action<RunnerTheme> setup)
        {
            return RunnerAssets.SaveScriptable<RunnerTheme>(art.Biome(key), theme =>
            {
                theme.displayName = displayName;
                theme.road = RunnerArtBuilder.Material(art, $"Road{key}");
                theme.stripes = RunnerArtBuilder.Material(art, $"Stripes{key}");
                theme.curbs = RunnerArtBuilder.Material(art, $"Curb{key}");
                theme.ground = RunnerArtBuilder.Material(art, $"Ground{key}");
                theme.cliff = RunnerArtBuilder.Material(art, "Cliff");
                theme.roadsideProp = null;
                setup(theme);
            });
        }

        private static Color C(float r, float g, float b, float a = 1f)
        {
            return new Color(r, g, b, a);
        }

        private static void Sky(RunnerTheme t, Color top, Color horizon, Color bottom, Color sun, float sunSize, float stars)
        {
            t.skyTop = top;
            t.skyHorizon = horizon;
            t.skyBottom = bottom;
            t.sunDisc = sun;
            t.sunSize = sunSize;
            t.starDensity = stars;
        }

        private static void Clouds(RunnerTheme t, Color clouds, float coverage, Color far, Color near, float height)
        {
            t.cloudColor = clouds;
            t.cloudCoverage = coverage;
            t.mountainFar = far;
            t.mountainNear = near;
            t.mountainHeight = height;
        }

        private static void Light(RunnerTheme t, Color sun, float intensity, Vector3 rotation, Color sky, Color equator, Color ground, float shadow)
        {
            t.sunColor = sun;
            t.sunIntensity = intensity;
            t.sunRotation = rotation;
            t.ambientSky = sky;
            t.ambientEquator = equator;
            t.ambientGround = ground;
            t.shadowStrength = shadow;
        }

        private static void Air(RunnerTheme t, Color fog, float density, AmbientEffect effect, Color dust)
        {
            t.fogColor = fog;
            t.fogDensity = density;
            t.ambientEffect = effect;
            t.dustColor = dust;
        }

        private const SceneryFacing Any = SceneryFacing.Any;
        private const SceneryFacing Square = SceneryFacing.Square;
        private const SceneryFacing Along = SceneryFacing.Along;
        private const SceneryFacing Road = SceneryFacing.Road;
        private const SceneryFacing Oncoming = SceneryFacing.Oncoming;

        /// <summary>Scenery that stands any way (trees, rocks: the classic worlds).</summary>
        private static SceneryItem[] Items(ThemeArt art, string biome, params (string name, float weight, float near, float far, float minScale, float maxScale)[] items)
        {
            return Items(art, biome, items.Select(item => (item.name, item.weight, item.near, item.far, item.minScale, item.maxScale, Any)).ToArray());
        }

        private static SceneryItem[] Items(ThemeArt art, string biome, params (string name, float weight, float near, float far, float minScale, float maxScale, SceneryFacing facing)[] items)
        {
            return items
                .Select(item => new SceneryItem
                {
                    prefab = RunnerArtBuilder.SceneryPiece(art, biome, item.name),
                    weight = item.weight,
                    distance = new Vector2(item.near, item.far),
                    scale = new Vector2(item.minScale, item.maxScale),
                    facing = item.facing
                })
                .Where(item => item.prefab != null)
                .ToArray();
        }

        private static void Roadside(RunnerTheme t, ThemeArt art, string biome, string name, int every, float offset)
        {
            t.roadsideProp = RunnerArtBuilder.SceneryPiece(art, biome, name);
            t.roadsideEvery = every;
            t.roadsideOffset = offset;
        }

        // ------------------------------------------------------------------ catalog

        /// <summary>The pieces of a theme, by name; the road tiles are shared, their materials come from the world.</summary>
        private static PieceCatalog BuildCatalog(ThemeArt art)
        {
            return RunnerAssets.SaveScriptable<PieceCatalog>(art.Catalog, catalog =>
            {
                catalog.roadTile = RunnerArtBuilder.Tile(false);
                catalog.chasmTile = RunnerArtBuilder.Tile(true);
                catalog.coin = RunnerArtBuilder.Piece<Coin>(art, "Coin");
                catalog.gem = RunnerArtBuilder.Piece<Coin>(art, "Gem");
                catalog.magnet = RunnerArtBuilder.Piece<PowerUpPickup>(art, "Magnet");
                catalog.shield = RunnerArtBuilder.Piece<PowerUpPickup>(art, "Shield");
                catalog.multiplier = RunnerArtBuilder.Piece<PowerUpPickup>(art, "Multiplier");
                catalog.superJump = RunnerArtBuilder.Piece<PowerUpPickup>(art, "SuperJump");
                catalog.jumpPad = RunnerArtBuilder.Piece<JumpPad>(art, "JumpPad");
                catalog.hurdle = RunnerArtBuilder.Piece<Obstacle>(art, "Hurdle");
                catalog.barrier = RunnerArtBuilder.Piece<Obstacle>(art, "Barrier");
                catalog.ramp = RunnerArtBuilder.Piece<Obstacle>(art, "Ramp");
                catalog.bridge = RunnerArtBuilder.Piece<Obstacle>(art, "Bridge");
                catalog.cart = RunnerArtBuilder.Piece<Obstacle>(art, "Cart");
                catalog.blocks = new[] { RunnerArtBuilder.Piece<Obstacle>(art, "CrateStack"), RunnerArtBuilder.Piece<Obstacle>(art, "CrateStack"), RunnerArtBuilder.Piece<Obstacle>(art, "BarrelStack") };
                catalog.shortWagons = RunnerArtBuilder.WagonNames(false).Select(name => RunnerArtBuilder.Piece<Obstacle>(art, name)).ToArray();
                catalog.longWagons = RunnerArtBuilder.WagonNames(true).Select(name => RunnerArtBuilder.Piece<Obstacle>(art, name)).ToArray();
                catalog.finishLine = RunnerArtBuilder.Piece<TrackPiece>(art, "FinishLine");
            });
        }

        /// <summary>The classic catalog: what the scene's track generator falls back on without a theme.</summary>
        public static PieceCatalog Catalog => RunnerAssets.Load<PieceCatalog>(RunnerThemes.Classic.Catalog);

        public static PieceCatalog CatalogOf(ThemeArt art)
        {
            return RunnerAssets.Load<PieceCatalog>(art.Catalog);
        }

        // ------------------------------------------------------------------ game themes

        /// <summary>
        /// The game theme of <paramref name="art"/>: its worlds, pieces, runner, materials, sky, sprites, colours,
        /// fonts and the skin of the shared menu, all from the assets the art builder wrote.
        /// </summary>
        private static RunnerGameTheme BuildGameTheme(ThemeArt art, Dictionary<string, RunnerTheme> biomes, PieceCatalog catalog)
        {
            RunnerGameTheme theme = RunnerAssets.SaveScriptable<RunnerGameTheme>(art.Asset, t =>
            {
                t.biomes = art.Biomes.Select(key => biomes[key]).ToList();
                t.pieces = catalog;
                Material palette = RunnerArtBuilder.Material(art, "Palette");
                t.runner = new RunnerLook
                {
                    torso = RunnerArtBuilder.RunnerPart(art, "Torso"),
                    head = RunnerArtBuilder.RunnerPart(art, "Head"),
                    upperArm = RunnerArtBuilder.RunnerPart(art, "UpperArm"),
                    forearm = RunnerArtBuilder.RunnerPart(art, "Forearm"),
                    thigh = RunnerArtBuilder.RunnerPart(art, "Thigh"),
                    shin = RunnerArtBuilder.RunnerPart(art, "Shin"),
                    material = palette,
                    hipsHeight = art.HipsHeight,
                    neck = art.Neck,
                    shoulder = art.Shoulder,
                    elbow = art.Elbow,
                    hip = art.Hip,
                    knee = art.Knee
                };
                t.materials = new PieceMaterials
                {
                    palette = palette,
                    coin = art.CoinColor.HasValue ? RunnerArtBuilder.Material(art, "Coin") : palette,
                    haloGem = RunnerArtBuilder.Material(art, "HaloGem"),
                    haloMagnet = RunnerArtBuilder.Material(art, "HaloMagnet"),
                    haloShield = RunnerArtBuilder.Material(art, "HaloShield"),
                    haloMultiplier = RunnerArtBuilder.Material(art, "HaloMultiplier"),
                    haloSuperJump = RunnerArtBuilder.Material(art, "HaloSuperJump"),
                    shieldRing = RunnerArtBuilder.Material(art, "ShieldRing")
                };
                t.particles = new ParticleMaterials
                {
                    sparkle = RunnerArtBuilder.Material(art, "ParticleSparkle"),
                    glow = RunnerArtBuilder.Material(art, "ParticleGlow"),
                    soft = RunnerArtBuilder.Material(art, "ParticleSoft"),
                    smoke = RunnerArtBuilder.Material(art, "ParticleSmoke"),
                    confetti = RunnerArtBuilder.Material(art, "ParticleConfetti"),
                    debris = RunnerArtBuilder.Material(art, "ParticleDebris")
                };
                t.sky = RunnerArtBuilder.Material(art, "Sky");
                t.sprites = new InterfaceSprites
                {
                    coin = RunnerArtBuilder.Icon(art, "Coin"),
                    gem = RunnerArtBuilder.Icon(art, "Gem"),
                    heart = RunnerArtBuilder.Icon(art, "Heart"),
                    heartEmpty = RunnerArtBuilder.Icon(art, "HeartEmpty"),
                    star = RunnerArtBuilder.Icon(art, "Star"),
                    starEmpty = RunnerArtBuilder.Icon(art, "StarEmpty"),
                    lockIcon = RunnerArtBuilder.Icon(art, "Lock"),
                    magnet = RunnerArtBuilder.Icon(art, "Magnet"),
                    shield = RunnerArtBuilder.Icon(art, "Shield"),
                    multiplier = RunnerArtBuilder.Icon(art, "Multiplier"),
                    spring = RunnerArtBuilder.Icon(art, "Spring"),
                    pause = RunnerArtBuilder.Icon(art, "Pause"),
                    flag = RunnerArtBuilder.Icon(art, "Flag"),
                    runner = RunnerArtBuilder.Icon(art, "Runner"),
                    infinity = RunnerArtBuilder.Icon(art, "Infinity"),
                    play = RunnerArtBuilder.Icon(art, "Play"),
                    retry = RunnerArtBuilder.Icon(art, "Retry"),
                    levels = RunnerArtBuilder.Icon(art, "Levels"),
                    settings = RunnerArtBuilder.Icon(art, "Settings"),
                    exit = RunnerArtBuilder.Icon(art, "Exit"),
                    check = RunnerArtBuilder.Icon(art, "Check"),
                    panel = RunnerArtBuilder.Icon(art, "Panel"),
                    button = RunnerArtBuilder.Icon(art, "Button"),
                    buttonHover = RunnerArtBuilder.Icon(art, "ButtonHover"),
                    buttonPressed = RunnerArtBuilder.Icon(art, "ButtonPressed"),
                    buttonDisabled = RunnerArtBuilder.Icon(art, "ButtonDisabled"),
                    fade = RunnerArtBuilder.Icon(art, "Fade"),
                    vignette = RunnerArtBuilder.Icon(art, "Vignette"),
                    glow = RunnerArtBuilder.Icon(art, "Glow"),
                    ring = RunnerArtBuilder.Icon(art, "Ring"),
                    grain = RunnerArtBuilder.Icon(art, "Grain"),
                    backdrop = RunnerArtBuilder.Icon(art, "Backdrop")
                };
                t.colors = art.IsClassic ? new InterfaceColors() : NightShiftColors();
                t.levels = art.IsClassic ? new List<LevelWords>() : NightShiftLevels();
                t.words = art.IsClassic ? new List<WordSwap>() : NightShiftWords();
                FillFonts(art, t);
                FillMenu(art, t);
                RunnerAssets.Set(t, "displayName", p => p.stringValue = art.DisplayName);
                RunnerAssets.Set(t, "description", p => p.stringValue = art.Description);
                RunnerAssets.SetObject(t, "preview", RunnerArtBuilder.Icon(art, "Runner"));
            });
            var problems = new List<string>();
            if (!theme.Validate(problems))
            {
                Debug.LogWarning($"Endless Runner: the theme {art.DisplayName} leaves {problems.Count} references empty:\n{string.Join("\n", problems)}", theme);
            }
            return theme;
        }

        /// <summary>The muted colours of the night city: dark glass panels, amber and cyan accents, a film grain.</summary>
        private static InterfaceColors NightShiftColors()
        {
            return new InterfaceColors
            {
                panel = C(0.06f, 0.065f, 0.075f, 0.88f),
                accent = C(1f, 0.72f, 0.25f),
                play = C(0.16f, 0.5f, 0.32f),
                warning = C(0.72f, 0.42f, 0.14f),
                info = C(0.2f, 0.36f, 0.5f),
                danger = C(0.6f, 0.2f, 0.2f),
                soft = C(0.72f, 0.74f, 0.76f),
                badge = C(0.72f, 0.45f, 0.12f, 0.95f),
                hint = C(0.55f, 0.57f, 0.6f),
                locked = C(0.2f, 0.21f, 0.23f),
                goals = C(0.85f, 0.8f, 0.7f),
                logoEndlessTop = C(0.95f, 0.95f, 0.92f),
                logoEndlessBottom = C(0.6f, 0.62f, 0.66f),
                logoRunnerTop = C(1f, 0.78f, 0.3f),
                logoRunnerBottom = C(0.85f, 0.42f, 0.1f),
                overlay = C(0f, 0f, 0f, 0.45f),
                grain = C(1f, 1f, 1f, 0.07f),
                curtain = C(0.01f, 0.01f, 0.012f, 1f),
                magnet = C(1f, 0.3f, 0.25f),
                shield = C(0.4f, 0.75f, 1f),
                multiplier = C(1f, 0.72f, 0.25f),
                superJump = C(0.45f, 1f, 0.55f),
                menuResume = C(0.16f, 0.5f, 0.32f),
                menuRestart = C(0.72f, 0.42f, 0.14f),
                menuSettings = C(0.2f, 0.36f, 0.5f),
                menuQuit = C(0.6f, 0.2f, 0.2f),
                menuText = C(0.9f, 0.9f, 0.88f),
                field = C(0.13f, 0.135f, 0.15f, 0.95f),
                fieldText = C(0.92f, 0.92f, 0.9f)
            };
        }

        /// <summary>What a courier calls the levels of the campaign, in its order.</summary>
        private static List<LevelWords> NightShiftLevels()
        {
            return new List<LevelWords>
            {
                Words("First Shift", "Your first night on the job. Dodge the dumpsters, clear the road blocks and grab every credit chip on the way."),
                Words("Loading Zone", "Delivery trucks line the street. Run up the loading ramps to reach the chips stacked on their roofs."),
                Words("Under the City", "The subway after midnight. Slide under the scaffolds and let the lift pads throw you up to the lights."),
                Words("Mind the Gap", "The tracks cut the platform in two. Time your jumps - or find the steel bridge."),
                Words("Night Freight", "Mail carts roll loose between the containers. Keep your eyes open in the fog."),
                Words("Container Run", "Leap from truck to truck along the harbour."),
                Words("Red Lights", "Everything you have learned, on the highway at night. Follow the sodium lights."),
                Words("Last Drop", "The final run over the rooftops as the sun comes up. Only the best couriers make the drop."),
                Words("Night Shift", "No finish line. Run through the whole city while the pace keeps climbing.")
            };
        }

        private static LevelWords Words(string title, string description)
        {
            return new LevelWords { title = title, description = description };
        }

        /// <summary>The hints, goals and tips of the track in the words of the city.</summary>
        private static List<WordSwap> NightShiftWords()
        {
            var swaps = new (string from, string to)[]
            {
                ("over hurdles", "over road blocks"), ("the crates", "the dumpsters"), ("under barriers", "under scaffolds"),
                ("coins are waiting on the wagon", "chips are waiting on the truck"), ("wagon to wagon", "truck to truck"),
                ("onto the wagons", "onto the trucks"), ("onto wagons", "onto trucks"), ("on wagons", "on trucks"), ("Bounce pads", "Lift pads"), ("into the sky", "up high"),
                ("Runaway cart", "Runaway mail cart"), ("coins in an arc", "chips in an arc"), ("The world changes", "The city changes"),
                ("Coins", "Chips"), ("coins", "chips"), ("Coin ", "Chip "), ("coin ", "chip ")
            };
            return swaps.Select(swap => new WordSwap { from = swap.from, to = swap.to }).ToList();
        }

        private static void FillFonts(ThemeArt art, RunnerGameTheme theme)
        {
            theme.Fonts.body = RunnerArtBuilder.FontAsset(art);
            theme.Fonts.bodyMaterial = RunnerArtBuilder.Material(art, "FontHud");
            theme.Fonts.title = RunnerArtBuilder.FontAsset(art);
            theme.Fonts.titleMaterial = RunnerArtBuilder.Material(art, "FontTitle");
        }

        /// <summary>
        /// The skin of the shared menu (the pause menu and the settings panel): the theme's backdrop, panels and
        /// buttons in its colours. Every sprite is set, also the ones this game's menu has no part for (sliders,
        /// toggles), so that both themes are complete and switching between them restyles every part.
        /// </summary>
        private static void FillMenu(ThemeArt art, RunnerGameTheme theme)
        {
            InterfaceColors colors = theme.colors;
            MenuSkin menu = theme.Menu;
            Sprite panel = RunnerArtBuilder.Icon(art, "Panel");
            Sprite button = RunnerArtBuilder.Icon(art, "Button");
            menu.backdrop = RunnerArtBuilder.Icon(art, "Backdrop");
            menu.backdropColor = colors.panel;
            menu.window = panel;
            menu.windowColor = art.IsClassic ? C(0.1f, 0.12f, 0.27f, 0.97f) : C(0.07f, 0.075f, 0.085f, 0.97f);
            menu.tileWindow = false;
            menu.settingsWindow = panel;
            menu.headerBanner = panel;
            menu.rowsBackground = panel;
            menu.rowsColor = art.IsClassic ? C(0.04f, 0.05f, 0.14f, 0.5f) : C(0.03f, 0.03f, 0.035f, 0.6f);
            menu.rowBackground = panel;
            menu.rowColor = art.IsClassic ? C(0.1f, 0.12f, 0.27f, 0.5f) : C(0.1f, 0.1f, 0.11f, 0.6f);
            menu.button = button;
            menu.buttonHover = RunnerArtBuilder.Icon(art, "ButtonHover");
            menu.buttonPressed = RunnerArtBuilder.Icon(art, "ButtonPressed");
            menu.buttonDisabled = RunnerArtBuilder.Icon(art, "ButtonDisabled");
            menu.arrowButton = button;
            menu.track = panel;
            menu.fill = button;
            menu.fillColor = colors.accent;
            menu.knob = RunnerArtBuilder.Icon(art, "Glow");
            menu.checkBox = panel;
            menu.checkMark = RunnerArtBuilder.Icon(art, "Check");
            menu.headerColor = colors.accent;
            menu.textColor = art.IsClassic ? Color.white : C(0.9f, 0.9f, 0.88f);
            menu.valueColor = colors.accent;
            menu.buttonTextColor = Color.white;
            menu.errorColor = art.IsClassic ? C(1f, 0.45f, 0.4f) : C(1f, 0.4f, 0.3f);
        }

        /// <summary>The game themes as built, the classic one first.</summary>
        public static RunnerGameTheme Theme(ThemeArt art)
        {
            return RunnerAssets.Load<RunnerGameTheme>(art.Asset);
        }

        // ------------------------------------------------------------------ campaign

        private static void BuildCampaign(Dictionary<string, RunnerTheme> themes)
        {
            RunnerAssets.SaveScriptable<RunnerSettings>("Config/RunnerSettings.asset", s => Speeds(s, 10f, 16f, 0.12f, 14f, 1.8f, 3));

            PowerUpType[] all = { PowerUpType.Magnet, PowerUpType.Shield, PowerUpType.Multiplier, PowerUpType.SuperJump };
            var levels = new List<RunnerLevel>
            {
                Level("RunnerLevel1", 0, "Sunny Start", "Learn the ropes in the meadows: dodge the crates, hop the hurdles and grab every coin you can.",
                    themes["Meadow"], 420f, 101, 0f, 0.25f, Basics, Basics, new PowerUpType[0], true, 0.6f,
                    Settings("Level1", 9f, 12f, 0.1f, 13f, 1.8f, 3)),
                Level("RunnerLevel2", 1, "Ramp It Up", "Wagons roll through the fields. Run up the ramps to reach the coins stacked on top.",
                    themes["Meadow"], 520f, 202, 0.1f, 0.4f, Basics | TrackFeatures.Ramps | TrackFeatures.PowerUps | TrackFeatures.Gems,
                    TrackFeatures.Ramps, new[] { PowerUpType.Magnet }, true, 0.6f, Settings("Level2", 10f, 13f, 0.1f, 13.5f, 1.8f, 3)),
                Level("RunnerLevel3", 2, "Duck and Dash", "Sunset in the canyon. Slide under the barriers and let the bounce pads throw you sky-high.",
                    themes["Desert"], 600f, 303, 0.2f, 0.5f, Basics | TrackFeatures.Ramps | TrackFeatures.Barriers | TrackFeatures.JumpPads | TrackFeatures.PowerUps | TrackFeatures.Gems,
                    TrackFeatures.Barriers | TrackFeatures.JumpPads, new[] { PowerUpType.Magnet, PowerUpType.Shield }, true, 0.6f,
                    Settings("Level3", 10.5f, 14f, 0.11f, 14f, 1.8f, 3)),
                Level("RunnerLevel4", 3, "Canyon Leap", "Rivers cut the road in two. Time your jumps - or find the bridge.",
                    themes["Desert"], 700f, 404, 0.3f, 0.6f, Basics | TrackFeatures.Ramps | TrackFeatures.Barriers | TrackFeatures.JumpPads | TrackFeatures.Chasms | TrackFeatures.PowerUps | TrackFeatures.Gems,
                    TrackFeatures.Chasms, new[] { PowerUpType.Magnet, PowerUpType.Shield, PowerUpType.Multiplier }, true, 0.6f,
                    Settings("Level4", 11f, 15f, 0.12f, 14.5f, 1.8f, 3)),
                Level("RunnerLevel5", 4, "Frosty Peaks", "Runaway carts are rolling down the mountain. Keep your eyes open!",
                    themes["Snow"], 760f, 505, 0.35f, 0.65f, TrackFeatures.All & ~TrackFeatures.PlatformChains,
                    TrackFeatures.MovingCarts, all, true, 0.6f, Settings("Level5", 11.5f, 15.5f, 0.12f, 15f, 1.8f, 3)),
                Level("RunnerLevel6", 5, "Avalanche Alley", "Leap from wagon to wagon high above the snow.",
                    themes["Snow"], 840f, 606, 0.45f, 0.75f, TrackFeatures.All, TrackFeatures.PlatformChains, all, true, 0.6f,
                    Settings("Level6", 12f, 16f, 0.12f, 15.5f, 1.8f, 3)),
                Level("RunnerLevel7", 6, "Moonlight Run", "Everything you have learned, under the stars. Follow the glowing road.",
                    themes["Night"], 900f, 707, 0.55f, 0.85f, TrackFeatures.All, TrackFeatures.None, all, false, 0.55f,
                    Settings("Level7", 12.5f, 17f, 0.13f, 16f, 1.8f, 3)),
                Level("RunnerLevel8", 7, "Lava Rush", "The final run across the burning land. Only the bravest make it to the end!",
                    themes["Volcano"], 1000f, 808, 0.65f, 1f, TrackFeatures.All, TrackFeatures.None, all, false, 0.55f,
                    Settings("Level8", 13f, 18f, 0.14f, 16.5f, 1.8f, 3)),
                Level("RunnerLevelEndless", 8, "Endless Run", "No finish line. Run through every world while the speed keeps climbing.",
                    themes["Meadow"], 0f, 909, 0.15f, 1f, TrackFeatures.All, TrackFeatures.None, all, false, 0.6f,
                    Settings("Endless", 11f, 20f, 0.1f, 16f, 1.8f, 3),
                    new[] { themes["Meadow"], themes["Desert"], themes["Snow"], themes["Night"], themes["Volcano"] })
            };

            RunnerAssets.SaveScriptable<Campaign>("Config/Campaign/RunnerCampaign.asset", campaign =>
                RunnerAssets.SetObjects(campaign, "<LevelList>k__BackingField", levels.Cast<Object>().ToArray()));
        }

        private static RunnerSettings Settings(string name, float start, float max, float acceleration, float side, float jump, int hearts)
        {
            return RunnerAssets.SaveScriptable<RunnerSettings>($"Config/Campaign/Settings/{name}.asset", s => Speeds(s, start, max, acceleration, side, jump, hearts));
        }

        private static void Speeds(RunnerSettings s, float start, float max, float acceleration, float side, float jump, int hearts)
        {
            s.ForwardSpeed = start;
            s.MaxSpeed = max;
            s.Acceleration = acceleration;
            s.SideSpeed = side;
            s.JumpHeight = jump;
            s.Hearts = hearts;
        }

        private static RunnerLevel Level(string file, int index, string title, string description, RunnerTheme theme, float length, int seed,
            float startDifficulty, float endDifficulty, TrackFeatures features, TrackFeatures spotlight, PowerUpType[] powerUps, bool hints,
            float coinGoal, RunnerSettings settings, RunnerTheme[] rotation = null)
        {
            return RunnerAssets.SaveScriptable<RunnerLevel>($"Config/Campaign/{file}.asset", level =>
            {
                level.description = description;
                level.theme = theme;
                level.themeRotation = rotation ?? new RunnerTheme[0];
                level.themeLength = 600f;
                level.length = length;
                level.seed = seed;
                level.startDifficulty = startDifficulty;
                level.endDifficulty = endDifficulty;
                level.features = features;
                level.spotlight = spotlight;
                level.powerUps = powerUps;
                level.coinGoal = coinGoal;
                level.showHints = hints;
                RunnerAssets.Set(level, "<Index>k__BackingField", p => p.intValue = index);
                RunnerAssets.Set(level, "<Title>k__BackingField", p => p.stringValue = title);
                RunnerAssets.SetObject(level, "<Settings>k__BackingField", settings);
            });
        }

        public static RunnerLevel FirstLevel => RunnerAssets.Load<RunnerLevel>("Config/Campaign/RunnerLevel1.asset");

        // ------------------------------------------------------------------ post processing

        private static void BuildVolumeProfile()
        {
            RunnerAssets.EnsureFolder("Config");
            string path = RunnerAssets.Path("Config/RunnerVolume.asset");
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }
            foreach (VolumeComponent component in profile.components.ToList())
            {
                profile.components.Remove(component);
                Object.DestroyImmediate(component, true);
            }
            var bloom = Add<Bloom>(profile);
            bloom.threshold.Override(0.95f);
            bloom.intensity.Override(0.75f);
            bloom.scatter.Override(0.65f);
            var colors = Add<ColorAdjustments>(profile);
            colors.saturation.Override(14f);
            colors.contrast.Override(8f);
            var vignette = Add<Vignette>(profile);
            vignette.intensity.Override(0.24f);
            vignette.smoothness.Override(0.45f);
            EditorUtility.SetDirty(profile);
        }

        private static T Add<T>(VolumeProfile profile) where T : VolumeComponent
        {
            var component = profile.Add<T>(true);
            component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            component.name = typeof(T).Name;
            AssetDatabase.AddObjectToAsset(component, profile);
            return component;
        }

        public static VolumeProfile VolumeProfile => RunnerAssets.Load<VolumeProfile>("Config/RunnerVolume.asset");

        // ------------------------------------------------------------------ launcher

        /// <summary>The launcher entry: its words and icon, and the themes: it starts with the classic one and lists them all.</summary>
        private static void UpdateGameDefinition(List<RunnerGameTheme> themes)
        {
            var definition = RunnerAssets.Load<GameDefinition>("Resources/Games/EndlessRunner.asset");
            if (definition == null)
            {
                return;
            }
            RunnerAssets.Set(definition, "description", p => p.stringValue =
                "Run through five worlds: dodge crates, jump hurdles, slide under barriers and climb ramps onto wagons to grab the coins. " +
                "Eight campaign levels with three stars each, plus an endless run. Two looks: the classic worlds and the Night Shift.");
            RunnerAssets.SetObject(definition, "icon", RunnerArtBuilder.Icon("Runner"));
            foreach (RunnerGameTheme theme in themes)
            {
                definition.AddTheme(theme);
            }
            definition.Theme = themes.Count > 0 ? themes[0] : null;
            EditorUtility.SetDirty(definition);
            GameThemes.ForgetDefinitions();
        }
    }
}
