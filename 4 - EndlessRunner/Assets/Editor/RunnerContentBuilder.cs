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
    /// Creates the game content from the generated art: the five worlds, the run settings of every level, the
    /// campaign (eight levels and the endless run), the catalog of track pieces, the post-processing profile and the
    /// launcher entry.
    /// </summary>
    internal static class RunnerContentBuilder
    {
        private const TrackFeatures Basics = TrackFeatures.Hurdles | TrackFeatures.Blocks;

        public static void BuildAll()
        {
            Dictionary<string, RunnerTheme> themes = BuildThemes();
            BuildCatalog();
            BuildCampaign(themes);
            BuildVolumeProfile();
            UpdateGameDefinition();
            AssetDatabase.SaveAssets();
        }

        // ------------------------------------------------------------------ themes

        private static Dictionary<string, RunnerTheme> BuildThemes()
        {
            var themes = new Dictionary<string, RunnerTheme>
            {
                ["Meadow"] = Theme("Meadow", "Sunny Meadows", t =>
                {
                    Sky(t, C(0.24f, 0.52f, 0.96f), C(0.74f, 0.89f, 1f), C(0.62f, 0.77f, 0.64f), C(1f, 0.97f, 0.86f), 0.045f, 0f);
                    Clouds(t, C(1f, 1f, 1f), 0.5f, C(0.56f, 0.72f, 0.9f), C(0.42f, 0.66f, 0.52f), 0.085f);
                    Light(t, C(1f, 0.96f, 0.88f), 1.15f, new Vector3(42f, -30f, 0f), C(0.62f, 0.72f, 0.88f), C(0.62f, 0.68f, 0.62f), C(0.4f, 0.44f, 0.32f), 0.55f);
                    Air(t, C(0.76f, 0.88f, 0.98f), 0.0105f, AmbientEffect.Pollen, C(0.72f, 0.66f, 0.5f, 0.6f));
                    t.chasmFill = RunnerArtBuilder.Material("Water");
                    t.scenery = Items("Meadow",
                        ("TreeRound", 3f, 8f, 34f, 0.8f, 1.3f), ("AppleTree", 1.5f, 8f, 30f, 0.8f, 1.2f), ("Poplar", 1.5f, 9f, 36f, 0.8f, 1.3f),
                        ("Pine", 1.5f, 10f, 40f, 0.8f, 1.4f), ("Bush", 2f, 6.5f, 22f, 0.8f, 1.3f), ("Flowers", 2.5f, 6f, 16f, 0.9f, 1.3f),
                        ("Rock", 1f, 7f, 30f, 0.6f, 1.4f), ("Mushroom", 0.6f, 6.5f, 18f, 0.8f, 1.4f), ("Cottage", 0.25f, 18f, 40f, 1f, 1.2f));
                    t.sceneryPerTile = 7;
                    Roadside(t, "Meadow", "Fence", 1, 5.3f);
                    t.accent = C(0.34f, 0.72f, 0.3f);
                }),
                ["Desert"] = Theme("Desert", "Sunset Canyon", t =>
                {
                    Sky(t, C(0.26f, 0.24f, 0.55f), C(1f, 0.68f, 0.42f), C(0.86f, 0.62f, 0.46f), C(1f, 0.86f, 0.6f), 0.07f, 0.1f);
                    Clouds(t, C(1f, 0.74f, 0.6f), 0.35f, C(0.76f, 0.45f, 0.42f), C(0.6f, 0.32f, 0.28f), 0.1f);
                    Light(t, C(1f, 0.8f, 0.58f), 1.1f, new Vector3(24f, -60f, 0f), C(0.72f, 0.56f, 0.62f), C(0.82f, 0.62f, 0.52f), C(0.46f, 0.33f, 0.26f), 0.6f);
                    Air(t, C(0.98f, 0.72f, 0.52f), 0.0105f, AmbientEffect.Dust, C(0.9f, 0.72f, 0.5f, 0.6f));
                    t.chasmFill = RunnerArtBuilder.Material("Water");
                    t.scenery = Items("Desert",
                        ("Saguaro", 3f, 7f, 34f, 0.8f, 1.3f), ("SaguaroTall", 1.5f, 9f, 36f, 1f, 1.4f), ("BarrelCactus", 2f, 6.5f, 20f, 0.8f, 1.4f),
                        ("DesertRock", 1.5f, 7f, 30f, 0.7f, 1.5f), ("DeadBush", 1.5f, 6.5f, 24f, 0.8f, 1.3f), ("PalmTree", 0.8f, 9f, 30f, 0.9f, 1.2f),
                        ("Mesa", 0.35f, 34f, 70f, 0.8f, 1.4f));
                    t.sceneryPerTile = 6;
                    t.accent = C(0.95f, 0.55f, 0.24f);
                }),
                ["Snow"] = Theme("Snow", "Frosty Peaks", t =>
                {
                    Sky(t, C(0.36f, 0.6f, 0.92f), C(0.86f, 0.93f, 1f), C(0.86f, 0.9f, 0.96f), C(1f, 1f, 0.95f), 0.04f, 0f);
                    Clouds(t, C(1f, 1f, 1f), 0.6f, C(0.8f, 0.88f, 0.98f), C(0.93f, 0.96f, 1f), 0.13f);
                    Light(t, C(1f, 0.98f, 0.95f), 1.05f, new Vector3(36f, -20f, 0f), C(0.76f, 0.83f, 0.96f), C(0.8f, 0.85f, 0.93f), C(0.7f, 0.75f, 0.83f), 0.45f);
                    Air(t, C(0.86f, 0.92f, 1f), 0.012f, AmbientEffect.Snow, C(0.95f, 0.97f, 1f, 0.7f));
                    t.chasmFill = RunnerArtBuilder.Material("IceWater");
                    t.scenery = Items("Snow",
                        ("SnowPine", 3f, 7.5f, 36f, 0.8f, 1.3f), ("SnowPineTall", 1.5f, 10f, 40f, 0.9f, 1.3f), ("Snowman", 0.5f, 7f, 20f, 0.9f, 1.2f),
                        ("IceRock", 1.2f, 7f, 30f, 0.6f, 1.4f), ("IceCrystals", 0.8f, 7f, 26f, 0.8f, 1.5f), ("SnowBush", 1.5f, 6.5f, 24f, 0.8f, 1.4f));
                    t.sceneryPerTile = 6;
                    Roadside(t, "Snow", "SnowPole", 2, 4.9f);
                    t.accent = C(0.38f, 0.66f, 1f);
                }),
                ["Night"] = Theme("Night", "Moonlit Woods", t =>
                {
                    Sky(t, C(0.03f, 0.04f, 0.15f), C(0.17f, 0.21f, 0.43f), C(0.1f, 0.12f, 0.25f), C(0.92f, 0.95f, 1f), 0.05f, 1f);
                    Clouds(t, C(0.3f, 0.35f, 0.55f, 0.6f), 0.35f, C(0.12f, 0.15f, 0.3f), C(0.07f, 0.09f, 0.2f), 0.09f);
                    Light(t, C(0.55f, 0.66f, 1f), 0.6f, new Vector3(50f, 150f, 0f), C(0.24f, 0.28f, 0.48f), C(0.2f, 0.22f, 0.38f), C(0.1f, 0.1f, 0.17f), 0.5f);
                    Air(t, C(0.13f, 0.16f, 0.33f), 0.0115f, AmbientEffect.Fireflies, C(0.4f, 0.45f, 0.6f, 0.5f));
                    t.chasmFill = RunnerArtBuilder.Material("GlowWater");
                    t.scenery = Items("Night",
                        ("NightTree", 2.5f, 8f, 34f, 0.8f, 1.3f), ("NightPine", 1.5f, 9f, 38f, 0.9f, 1.4f), ("GlowMushrooms", 2f, 6.5f, 22f, 0.8f, 1.4f),
                        ("NightCrystals", 1.2f, 7f, 28f, 0.8f, 1.4f), ("NightRock", 1f, 7f, 30f, 0.6f, 1.4f));
                    t.sceneryPerTile = 6;
                    Roadside(t, "Night", "LampPost", 3, 5.2f);
                    t.accent = C(0.55f, 0.45f, 1f);
                }),
                ["Volcano"] = Theme("Volcano", "Lava Land", t =>
                {
                    Sky(t, C(0.12f, 0.04f, 0.05f), C(0.86f, 0.33f, 0.13f), C(0.3f, 0.1f, 0.06f), C(1f, 0.56f, 0.26f), 0.06f, 0.2f);
                    Clouds(t, C(0.36f, 0.18f, 0.15f), 0.55f, C(0.36f, 0.12f, 0.1f), C(0.18f, 0.07f, 0.07f), 0.12f);
                    Light(t, C(1f, 0.58f, 0.38f), 0.95f, new Vector3(30f, 40f, 0f), C(0.5f, 0.26f, 0.21f), C(0.56f, 0.29f, 0.21f), C(0.3f, 0.12f, 0.08f), 0.6f);
                    Air(t, C(0.56f, 0.21f, 0.11f), 0.012f, AmbientEffect.Embers, C(0.35f, 0.3f, 0.3f, 0.6f));
                    t.chasmFill = RunnerArtBuilder.Material("Lava");
                    t.scenery = Items("Volcano",
                        ("BasaltColumns", 2f, 7f, 32f, 0.8f, 1.4f), ("DeadTree", 1.5f, 7.5f, 30f, 0.8f, 1.3f), ("LavaRock", 2f, 7f, 28f, 0.7f, 1.4f),
                        ("AshRock", 1f, 7f, 30f, 0.6f, 1.4f), ("Volcano", 0.08f, 60f, 110f, 0.8f, 1.2f));
                    t.sceneryPerTile = 6;
                    Roadside(t, "Volcano", "Torch", 3, 5.2f);
                    t.accent = C(1f, 0.4f, 0.15f);
                })
            };
            return themes;
        }

        private static RunnerTheme Theme(string key, string displayName, System.Action<RunnerTheme> setup)
        {
            return RunnerAssets.SaveScriptable<RunnerTheme>($"Config/Themes/{key}.asset", theme =>
            {
                theme.displayName = displayName;
                theme.road = RunnerArtBuilder.Material($"Road{key}");
                theme.stripes = RunnerArtBuilder.Material($"Stripes{key}");
                theme.curbs = RunnerArtBuilder.Material($"Curb{key}");
                theme.ground = RunnerArtBuilder.Material($"Ground{key}");
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

        private static SceneryItem[] Items(string theme, params (string name, float weight, float near, float far, float minScale, float maxScale)[] items)
        {
            return items
                .Select(item => new SceneryItem
                {
                    prefab = RunnerArtBuilder.SceneryPiece(theme, item.name),
                    weight = item.weight,
                    distance = new Vector2(item.near, item.far),
                    scale = new Vector2(item.minScale, item.maxScale)
                })
                .Where(item => item.prefab != null)
                .ToArray();
        }

        private static void Roadside(RunnerTheme t, string theme, string name, int every, float offset)
        {
            t.roadsideProp = RunnerArtBuilder.SceneryPiece(theme, name);
            t.roadsideEvery = every;
            t.roadsideOffset = offset;
        }

        // ------------------------------------------------------------------ catalog

        private static PieceCatalog BuildCatalog()
        {
            return RunnerAssets.SaveScriptable<PieceCatalog>("Config/PieceCatalog.asset", catalog =>
            {
                catalog.roadTile = RunnerArtBuilder.Tile(false);
                catalog.chasmTile = RunnerArtBuilder.Tile(true);
                catalog.coin = RunnerArtBuilder.Piece<Coin>("Coin");
                catalog.gem = RunnerArtBuilder.Piece<Coin>("Gem");
                catalog.magnet = RunnerArtBuilder.Piece<PowerUpPickup>("Magnet");
                catalog.shield = RunnerArtBuilder.Piece<PowerUpPickup>("Shield");
                catalog.multiplier = RunnerArtBuilder.Piece<PowerUpPickup>("Multiplier");
                catalog.superJump = RunnerArtBuilder.Piece<PowerUpPickup>("SuperJump");
                catalog.jumpPad = RunnerArtBuilder.Piece<JumpPad>("JumpPad");
                catalog.hurdle = RunnerArtBuilder.Piece<Obstacle>("Hurdle");
                catalog.barrier = RunnerArtBuilder.Piece<Obstacle>("Barrier");
                catalog.ramp = RunnerArtBuilder.Piece<Obstacle>("Ramp");
                catalog.bridge = RunnerArtBuilder.Piece<Obstacle>("Bridge");
                catalog.cart = RunnerArtBuilder.Piece<Obstacle>("Cart");
                catalog.blocks = new[] { RunnerArtBuilder.Piece<Obstacle>("CrateStack"), RunnerArtBuilder.Piece<Obstacle>("CrateStack"), RunnerArtBuilder.Piece<Obstacle>("BarrelStack") };
                catalog.shortWagons = RunnerArtBuilder.WagonNames(false).Select(RunnerArtBuilder.Piece<Obstacle>).ToArray();
                catalog.longWagons = RunnerArtBuilder.WagonNames(true).Select(RunnerArtBuilder.Piece<Obstacle>).ToArray();
                catalog.finishLine = RunnerArtBuilder.Piece<TrackPiece>("FinishLine");
                catalog.startLine = null;
            });
        }

        public static PieceCatalog Catalog => RunnerAssets.Load<PieceCatalog>("Config/PieceCatalog.asset");

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

        private static void UpdateGameDefinition()
        {
            var definition = RunnerAssets.Load<GameDefinition>("Resources/Games/EndlessRunner.asset");
            if (definition == null)
            {
                return;
            }
            RunnerAssets.Set(definition, "description", p => p.stringValue =
                "Run through five worlds: dodge crates, jump hurdles, slide under barriers and climb ramps onto wagons to grab the coins. " +
                "Eight campaign levels with three stars each, plus an endless run.");
            RunnerAssets.SetObject(definition, "icon", RunnerArtBuilder.Icon("Runner"));
            EditorUtility.SetDirty(definition);
        }
    }
}
