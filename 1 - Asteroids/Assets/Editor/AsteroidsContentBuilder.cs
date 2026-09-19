using System;
using System.Collections.Generic;
using System.Linq;
using Gamebox;
using Gamebox.Launcher;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace Portfolio.Asteroids.EditorTools
{
    /// <summary>
    /// Builds the content of the Asteroids campaign: the hangar's ships, the four sector themes, the settings and drop
    /// tables of every mission, the twelve missions and the endless one, the <see cref="AsteroidsCampaign"/> that holds
    /// them, the post-processing profile and the launcher entry. The numbers live here, so hand edits to those assets are
    /// overwritten by a rebuild.
    /// </summary>
    internal static class AsteroidsContentBuilder
    {
        public const string CampaignPath = "Config/Campaign/AsteroidsCampaign.asset";

        private sealed class Mission
        {
            public string Title;
            public string Description;
            public string Introduces;
            public string[] Hints = new string[0];
            /// <summary>Touch wording of the hints that name keys, by position in Hints; null or empty keeps the hint.</summary>
            public string[] TouchHints = new string[0];
            public int Sector;
            public LevelObjective Objective;
            public int Target;
            public WaveSpec[] Waves;
            public string Boss;
            public int ScoreGoal;
            public float Speed = 1f;
            public float SpawnRate = 6f;
            public float Explosion = 3f;
        }

        private static readonly (string title, string theme, int stars)[] Sectors =
        {
            ("Kepler Belt", "Kepler", 0), ("Crimson Expanse", "Crimson", 3), ("Frost Rings", "Frost", 8), ("Void Core", "Void", 15)
        };

        public static void BuildAll()
        {
            Progress("Ships", 0.1f);
            BuildHulls();
            Progress("Sectors", 0.2f);
            BuildThemes();
            Progress("Drop tables", 0.35f);
            BuildLoot();
            Progress("Missions", 0.5f);
            BuildMissions();
            Progress("Post-processing", 0.85f);
            BuildVolumeProfile();
            UpdateGameDefinition();
            AssetDatabase.SaveAssets();
            EditorUtility.ClearProgressBar();
        }

        private static void Progress(string step, float value)
        {
            EditorUtility.DisplayProgressBar("Asteroids", $"Building {step}...", value);
        }

        // ------------------------------------------------------------------ hangar

        private static string HullPath(int index)
        {
            return index == 0 ? "Config/PlayerSettings.asset" : $"Config/Ships/{AsteroidsArtBuilder.Hulls[index].name}.asset";
        }

        public static PlayerSettings Hull(int index)
        {
            return AsteroidsAssets.Load<PlayerSettings>(HullPath(index));
        }

        public static PlayerSettings[] Hangar => Enumerable.Range(0, AsteroidsArtBuilder.Hulls.Length).Select(Hull).ToArray();

        /// <summary>The ships of the hangar, with their flight values and the engine and gun positions measured on their models.</summary>
        public static void BuildHulls()
        {
            // name, description, stars, thrust, max speed, drag, brake, turn, turn response, dash speed, dash time, dash cooldown,
            // hull, shield, fire rate, hit radius
            var stats = new (string description, int stars, float thrust, float speed, float drag, float turn, float response, float dash, float dashCooldown,
                float hull, float shield, float fire, float radius)[]
            {
                ("Balanced interceptor. Easy to fly and good at everything.", 0, 22f, 11f, 0.55f, 250f, 10f, 22f, 2.2f, 1f, 100f, 1f, 0.68f),
                ("Fast and nimble racer with a light hull. Dashes often.", 6, 28f, 13.5f, 0.5f, 290f, 12f, 26f, 1.8f, 0.8f, 80f, 1.1f, 0.64f),
                ("Heavy gunship. Slow to turn but very hard to kill.", 14, 18f, 9.5f, 0.65f, 215f, 8f, 18f, 2.8f, 1.5f, 140f, 0.95f, 0.76f),
                ("Prototype striker with the fastest guns and dash.", 24, 24f, 12.5f, 0.5f, 270f, 11f, 24f, 1.4f, 0.9f, 100f, 1.3f, 0.6f)
            };
            for (int i = 0; i < AsteroidsArtBuilder.Hulls.Length; i++)
            {
                (string name, string model, string material, Color engine) = AsteroidsArtBuilder.Hulls[i];
                var s = stats[i];
                Mesh mesh = AsteroidsArtBuilder.ShipMesh(model);
                Material paint = AsteroidsArtBuilder.PackMaterial(material);
                const float length = 1.95f;
                float scale = mesh != null ? length / Mathf.Max(0.01f, mesh.bounds.size.z) : 0.12f;
                Vector2[] engines = mesh != null ? MeasureEngines(mesh, scale) : new[] { new Vector2(0f, -0.75f) };
                Vector2 gun = mesh != null ? new Vector2(0f, mesh.bounds.max.z * scale) : new Vector2(0f, 0.8f);
                AsteroidsAssets.SaveScriptable<PlayerSettings>(HullPath(i), hull =>
                {
                    Auto(hull, nameof(PlayerSettings.DisplayName), p => p.stringValue = name);
                    Auto(hull, nameof(PlayerSettings.Description), p => p.stringValue = s.description);
                    Auto(hull, nameof(PlayerSettings.StarsToUnlock), p => p.intValue = s.stars);
                    Auto(hull, nameof(PlayerSettings.Thrust), p => p.floatValue = s.thrust);
                    Auto(hull, nameof(PlayerSettings.MovementSpeed), p => p.floatValue = s.speed);
                    Auto(hull, nameof(PlayerSettings.Drag), p => p.floatValue = s.drag);
                    Auto(hull, nameof(PlayerSettings.BrakeForce), p => p.floatValue = 20f);
                    Auto(hull, nameof(PlayerSettings.RotationSpeed), p => p.floatValue = s.turn);
                    Auto(hull, nameof(PlayerSettings.TorqueDrag), p => p.floatValue = s.response);
                    Auto(hull, nameof(PlayerSettings.DashSpeed), p => p.floatValue = s.dash);
                    Auto(hull, nameof(PlayerSettings.DashDuration), p => p.floatValue = 0.22f);
                    Auto(hull, nameof(PlayerSettings.DashCooldown), p => p.floatValue = s.dashCooldown);
                    Auto(hull, nameof(PlayerSettings.HullMultiplier), p => p.floatValue = s.hull);
                    Auto(hull, nameof(PlayerSettings.ShieldCapacity), p => p.floatValue = s.shield);
                    Auto(hull, nameof(PlayerSettings.FireRateMultiplier), p => p.floatValue = s.fire);
                    Auto(hull, nameof(PlayerSettings.HitRadius), p => p.floatValue = s.radius);
                    Auto(hull, nameof(PlayerSettings.ModelMesh), p => p.objectReferenceValue = mesh);
                    Auto(hull, nameof(PlayerSettings.ModelMaterial), p => p.objectReferenceValue = paint);
                    Auto(hull, nameof(PlayerSettings.ModelScale), p => p.floatValue = scale);
                    Auto(hull, nameof(PlayerSettings.EnginePoints), p =>
                    {
                        p.arraySize = engines.Length;
                        for (int e = 0; e < engines.Length; e++)
                        {
                            p.GetArrayElementAtIndex(e).vector2Value = engines[e];
                        }
                    });
                    Auto(hull, nameof(PlayerSettings.GunPoint), p => p.vector2Value = gun);
                    Auto(hull, nameof(PlayerSettings.EngineColor), p => p.colorValue = engine);
                    Auto(hull, nameof(PlayerSettings.Forward), p => p.intValue = (int)KeyCode.W);
                    Auto(hull, nameof(PlayerSettings.Left), p => p.intValue = (int)KeyCode.A);
                    Auto(hull, nameof(PlayerSettings.Right), p => p.intValue = (int)KeyCode.D);
                    Auto(hull, nameof(PlayerSettings.Brake), p => p.intValue = (int)KeyCode.S);
                    Auto(hull, nameof(PlayerSettings.Shoot), p => p.intValue = (int)KeyCode.Space);
                    Auto(hull, nameof(PlayerSettings.Dash), p => p.intValue = (int)KeyCode.LeftShift);
                    Auto(hull, nameof(PlayerSettings.Bomb), p => p.intValue = (int)KeyCode.B);
                });
            }
        }

        /// <summary>
        /// Engine flame positions in ship space (x across, y along the nose): the clusters of vertices at the very back of
        /// the model, at most three.
        /// </summary>
        private static Vector2[] MeasureEngines(Mesh mesh, float scale)
        {
            Bounds bounds = mesh.bounds;
            float back = bounds.min.z + bounds.size.z * 0.08f;
            List<float> xs = mesh.vertices.Where(v => v.z <= back).Select(v => v.x).OrderBy(x => x).ToList();
            if (xs.Count == 0)
            {
                return new[] { new Vector2(0f, bounds.min.z * scale) };
            }
            float gap = bounds.size.x * 0.08f;
            var clusters = new List<List<float>> { new List<float> { xs[0] } };
            for (int i = 1; i < xs.Count; i++)
            {
                if (xs[i] - xs[i - 1] > gap)
                {
                    clusters.Add(new List<float>());
                }
                clusters[clusters.Count - 1].Add(xs[i]);
            }
            IEnumerable<List<float>> biggest = clusters.OrderByDescending(cluster => cluster.Count).Take(3);
            return biggest.Select(cluster => new Vector2(cluster.Average() * scale, (bounds.min.z + bounds.size.z * 0.04f) * scale))
                .OrderBy(point => point.x).ToArray();
        }

        // ------------------------------------------------------------------ sectors

        public static SectorTheme Theme(string name)
        {
            return AsteroidsAssets.Load<SectorTheme>($"Config/Themes/{name}.asset");
        }

        private static void BuildThemes()
        {
            AsteroidsAssets.SaveScriptable<SectorTheme>("Config/Themes/Kepler.asset", t =>
            {
                t.title = "Kepler Belt";
                t.accent = Hex("3FD8FF");
                t.spaceColor = new Color(0.006f, 0.01f, 0.028f);
                t.nebulaColor = new Color(0.04f, 0.18f, 0.38f);
                t.highlightColor = new Color(0.28f, 0.72f, 0.9f);
                t.dustColor = new Color(0.008f, 0.012f, 0.03f);
                t.starDensity = 0.75f;
                t.planetMaterial = AsteroidsArtBuilder.Material("PlanetKepler");
                t.cloudMaterial = null;
                t.atmosphereColor = new Color(0.3f, 0.75f, 1.3f);
                t.planetPlacement = new Vector3(0.86f, 0.16f, 150f);
                t.planetSize = 95f;
                t.planetTilt = new Vector3(18f, 0f, -12f);
                t.rings = false;
                t.sunColor = new Color(1f, 0.97f, 0.9f);
                t.sunIntensity = 1.55f;
                t.sunAngles = new Vector3(35f, -40f, 0f);
                t.ambientColor = new Color(0.15f, 0.18f, 0.27f);
                t.motesColor = new Color(0.5f, 0.8f, 1f);
            });
            AsteroidsAssets.SaveScriptable<SectorTheme>("Config/Themes/Crimson.asset", t =>
            {
                t.title = "Crimson Expanse";
                t.accent = Hex("FF6A3D");
                t.spaceColor = new Color(0.02f, 0.006f, 0.01f);
                t.nebulaColor = new Color(0.4f, 0.08f, 0.04f);
                t.highlightColor = new Color(0.9f, 0.5f, 0.2f);
                t.dustColor = new Color(0.03f, 0.005f, 0.01f);
                t.starDensity = 0.65f;
                t.planetMaterial = AsteroidsArtBuilder.PackMaterial("BonusContent/TinniuqPlanet");
                t.cloudMaterial = AsteroidsArtBuilder.PackMaterial("BonusContent/CloudsRed");
                t.atmosphereColor = new Color(1.3f, 0.45f, 0.3f);
                t.planetPlacement = new Vector3(0.13f, 0.82f, 170f);
                t.planetSize = 110f;
                t.planetTilt = new Vector3(25f, 0f, 10f);
                t.rings = false;
                t.sunColor = new Color(1f, 0.82f, 0.62f);
                t.sunIntensity = 1.6f;
                t.sunAngles = new Vector3(30f, 35f, 0f);
                t.ambientColor = new Color(0.24f, 0.15f, 0.15f);
                t.motesColor = new Color(1f, 0.6f, 0.4f);
            });
            AsteroidsAssets.SaveScriptable<SectorTheme>("Config/Themes/Frost.asset", t =>
            {
                t.title = "Frost Rings";
                t.accent = Hex("8FE3FF");
                t.spaceColor = new Color(0.01f, 0.016f, 0.03f);
                t.nebulaColor = new Color(0.13f, 0.24f, 0.42f);
                t.highlightColor = new Color(0.62f, 0.75f, 0.92f);
                t.dustColor = new Color(0.02f, 0.03f, 0.05f);
                t.starDensity = 0.85f;
                t.planetMaterial = AsteroidsArtBuilder.Material("PlanetFrost");
                t.cloudMaterial = null;
                t.atmosphereColor = new Color(0.6f, 0.85f, 1.3f);
                t.planetPlacement = new Vector3(0.83f, 0.78f, 200f);
                t.planetSize = 46f;
                t.planetTilt = new Vector3(62f, 0f, 22f);
                t.rings = true;
                t.ringColor = new Color(0.62f, 0.7f, 0.8f, 0.5f);
                t.sunColor = new Color(0.85f, 0.92f, 1f);
                t.sunIntensity = 1.5f;
                t.sunAngles = new Vector3(40f, -30f, 0f);
                t.ambientColor = new Color(0.19f, 0.22f, 0.29f);
                t.motesColor = new Color(0.85f, 0.95f, 1f);
            });
            AsteroidsAssets.SaveScriptable<SectorTheme>("Config/Themes/Void.asset", t =>
            {
                t.title = "Void Core";
                t.accent = Hex("D46BFF");
                t.spaceColor = new Color(0.014f, 0.004f, 0.024f);
                t.nebulaColor = new Color(0.25f, 0.04f, 0.33f);
                t.highlightColor = new Color(0.9f, 0.28f, 0.78f);
                t.dustColor = new Color(0.02f, 0f, 0.03f);
                t.starDensity = 0.7f;
                t.planetMaterial = AsteroidsArtBuilder.Material("PlanetVoid");
                t.cloudMaterial = null;
                t.atmosphereColor = new Color(0.9f, 0.35f, 1.3f);
                t.planetPlacement = new Vector3(0.12f, 0.2f, 150f);
                t.planetSize = 100f;
                t.planetTilt = new Vector3(10f, 0f, 28f);
                t.rings = false;
                t.sunColor = new Color(0.95f, 0.8f, 1f);
                t.sunIntensity = 1.4f;
                t.sunAngles = new Vector3(35f, 30f, 0f);
                t.ambientColor = new Color(0.19f, 0.14f, 0.26f);
                t.motesColor = new Color(0.9f, 0.5f, 1f);
            });
        }

        private static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString("#" + hex, out Color color) ? color : Color.white;
        }

        // ------------------------------------------------------------------ drop tables

        private static Loot LoadLoot(string name)
        {
            return AsteroidsAssets.Load<Loot>(name == "Rocks1" ? "Config/Loot.asset" : $"Config/Loot/{name}.asset");
        }

        private static void BuildLoot()
        {
            Table("Rocks1", ("Repair", 3), ("Shield", 3), ("WeaponBlaster", 3));
            Table("Rocks2", ("Repair", 3), ("Shield", 3), ("WeaponBlaster", 2), ("WeaponScatter", 2), ("PowerUpOverdrive", 2), ("PowerUpMagnet", 1));
            Table("Rocks3", ("Repair", 3), ("Shield", 3), ("WeaponLaser", 2), ("WeaponScatter", 1), ("PowerUpOverdrive", 2), ("PowerUpMagnet", 1), ("PowerUpChrono", 2), ("NovaBomb", 1));
            Table("Rocks4", ("Repair", 3), ("Shield", 3), ("WeaponMissiles", 2), ("WeaponLaser", 1), ("PowerUpOverdrive", 1), ("PowerUpMagnet", 1), ("PowerUpChrono", 1),
                ("PowerUpDrones", 2), ("NovaBomb", 1), ("ExtraShip", 1));
            Table("Pods1", ("WeaponBlaster", 4), ("Shield", 3), ("Repair", 2), ("NovaBomb", 1));
            Table("Pods2", ("WeaponScatter", 3), ("WeaponBlaster", 2), ("Shield", 2), ("PowerUpOverdrive", 2), ("PowerUpMagnet", 1), ("NovaBomb", 1));
            Table("Pods3", ("WeaponLaser", 3), ("Shield", 2), ("PowerUpChrono", 2), ("PowerUpOverdrive", 1), ("NovaBomb", 2), ("Repair", 1));
            Table("Pods4", ("WeaponMissiles", 3), ("PowerUpDrones", 2), ("Shield", 2), ("WeaponLaser", 1), ("NovaBomb", 1), ("ExtraShip", 1));
            Table("RocksEndless", ("Repair", 3), ("Shield", 3), ("WeaponBlaster", 1), ("WeaponScatter", 1), ("WeaponLaser", 1), ("WeaponMissiles", 1),
                ("PowerUpOverdrive", 1), ("PowerUpMagnet", 1), ("PowerUpChrono", 1), ("PowerUpDrones", 1), ("NovaBomb", 1));
            Table("PodsEndless", ("WeaponBlaster", 1), ("WeaponScatter", 1), ("WeaponLaser", 1), ("WeaponMissiles", 1), ("Shield", 2), ("PowerUpDrones", 1),
                ("PowerUpChrono", 1), ("NovaBomb", 2), ("ExtraShip", 1));
        }

        private static void Table(string name, params (string reward, int weight)[] entries)
        {
            string path = name == "Rocks1" ? "Config/Loot.asset" : $"Config/Loot/{name}.asset";
            AsteroidsAssets.SaveScriptable<Loot>(path, loot =>
            {
                loot.Rewards = entries.Select(entry => AsteroidsPrefabBuilder.Load<Reward>($"Pickups/{entry.reward}")).ToList();
                loot.Weights = entries.Select(entry => entry.weight).ToList();
            });
        }

        // ------------------------------------------------------------------ missions

        private static WaveSpec W(int rocks = 0, int ores = 0, int magma = 0, int ice = 0, int crystals = 0, int mines = 0, int bombs = 0,
            int comets = 0, int wells = 0, int saucers = 0, int wasps = 0, int pods = 0, float interval = 6f, float duration = 30f, string title = "")
        {
            return new WaveSpec
            {
                title = title,
                rocks = rocks,
                ores = ores,
                magma = magma,
                ice = ice,
                crystals = crystals,
                mines = mines,
                bombs = bombs,
                comets = comets,
                wells = wells,
                saucers = saucers,
                wasps = wasps,
                pods = pods,
                hazardInterval = interval,
                duration = duration
            };
        }

        // ScoreGoal (the third star) is about 55% of what AsteroidsAutopilot scores on the mission, so a clean clear
        // without combos stays below it. Re-measure after changing waves, scores or bosses.
        private static readonly Mission[] Missions =
        {
            new Mission
            {
                Title = "First Light", Sector = 0, Objective = LevelObjective.ClearWaves, ScoreGoal = 15000, Speed = 0.85f, SpawnRate = 7f,
                Description = "Your first patrol in the Kepler Belt. Break up the drifting rocks before they break you.",
                Introduces = "Big rocks split into smaller, faster ones. Shoot supply pods for upgrades.",
                Hints = new[]
                {
                    "W / UP thrusts, A D / LEFT RIGHT turn, SPACE fires.",
                    "Big rocks split in two - the small ones are worth the most points.",
                    "Shoot the green supply pods for weapon upgrades and shields.",
                    "SHIFT dashes through danger. Kills in a row build a combo."
                },
                TouchHints = new[]
                {
                    "Left thumb steers and thrusts toward where it points. Hold FIRE to shoot.",
                    null,
                    null,
                    "DASH bursts through danger. Kills in a row build a combo."
                },
                Waves = new[] { W(rocks: 3), W(rocks: 4, pods: 1, interval: 5f), W(rocks: 5, pods: 1) }
            },
            new Mission
            {
                Title = "Ore Rush", Sector = 0, Objective = LevelObjective.Collect, Target = 12, ScoreGoal = 6500, Speed = 0.9f, SpawnRate = 6.5f,
                Description = "Gold-veined ore rocks drift through the belt. Crack them open and scoop up the crystals.",
                Introduces = "Ore rocks: tougher, and full of crystals.",
                Hints = new[] { "Ore rocks take more hits but drop crystals.", "Crystals drift toward you when you fly close." },
                Waves = new[] { W(rocks: 2, ores: 2), W(rocks: 2, ores: 3, pods: 1), W(rocks: 3, ores: 3, pods: 1) }
            },
            new Mission
            {
                Title = "Titan", Sector = 0, Objective = LevelObjective.Boss, Boss = "RockTitan", ScoreGoal = 28000, Speed = 0.9f, SpawnRate = 7f,
                Description = "Something enormous moves through the belt, shedding rocks as it goes. Destroy it.",
                Introduces = "Your first boss: the Rock Titan.",
                Hints = new[] { "Bosses take many hits and grow angrier as they weaken.", "Keep moving - the Titan throws rocks and shrapnel." },
                Waves = new[] { W(rocks: 4, pods: 1, title: "Warm-up") }
            },
            new Mission
            {
                Title = "Minefield", Sector = 1, Objective = LevelObjective.ClearWaves, ScoreGoal = 15000, Speed = 0.95f, SpawnRate = 6f,
                Description = "Raiders seeded the Crimson Expanse with proximity mines. Clear the rocks without tripping them.",
                Introduces = "Proximity mines arm when you come close. Their blast breaks rocks too.",
                Hints = new[] { "Mines arm when you get close - back off or shoot them from afar.", "A mine's blast destroys nearby rocks: use it!" },
                Waves = new[] { W(rocks: 3, mines: 3, interval: 4f), W(rocks: 3, ores: 1, mines: 4, pods: 1, interval: 4f), W(rocks: 4, ores: 1, mines: 5, interval: 3.5f) }
            },
            new Mission
            {
                Title = "Magma Run", Sector = 1, Objective = LevelObjective.ClearWaves, ScoreGoal = 20000, Speed = 1f, SpawnRate = 6f, Explosion = 3.2f,
                Description = "Molten rocks from a shattered world. They explode when destroyed, and comets streak through the sector.",
                Introduces = "Magma rocks explode. Comets cross the sector after a warning.",
                Hints = new[] { "Magma rocks explode - keep your distance when you pop them.", "A red lane warns of an incoming comet. Get out of its path!" },
                Waves = new[] { W(rocks: 2, magma: 2, comets: 1), W(rocks: 2, magma: 3, comets: 2, pods: 1), W(rocks: 2, magma: 4, comets: 3, mines: 2) }
            },
            new Mission
            {
                Title = "Raider Mothership", Sector = 1, Objective = LevelObjective.Boss, Boss = "Mothership", ScoreGoal = 26000, Speed = 1f, SpawnRate = 6f, Explosion = 3.2f,
                Description = "The raiders' flagship arrives, escorted by flying saucers that shoot back.",
                Introduces = "Flying saucers. Boss: the Raider Mothership.",
                Hints = new[] { "Saucers shoot back. The small scouts aim at you - and are worth 1000 points.", "The Mothership drops mines and calls in saucers." },
                Waves = new[] { W(rocks: 3, magma: 1, saucers: 2, pods: 1, interval: 5f) }
            },
            new Mission
            {
                Title = "Shatter Point", Sector = 2, Objective = LevelObjective.ClearWaves, ScoreGoal = 14000, Speed = 1.05f, SpawnRate = 5.5f, Explosion = 3.2f,
                Description = "The Frost Rings are full of ice. Every chunk you hit shatters into a spray of fast shards.",
                Introduces = "Ice rocks shatter into shards. New weapon: the Lancer Laser.",
                Hints = new[] { "Ice shatters into many small shards at once - be ready.", "The laser pierces through several rocks in a row." },
                Waves = new[] { W(rocks: 1, ice: 3), W(rocks: 2, ice: 4, saucers: 1, pods: 1), W(ores: 2, ice: 5, comets: 1, pods: 1) }
            },
            new Mission
            {
                Title = "Event Horizon", Sector = 2, Objective = LevelObjective.Survive, Target = 90, ScoreGoal = 24000, Speed = 1.05f, SpawnRate = 5f, Explosion = 3.4f,
                Description = "Gravity wells tear open in the rings. Hold out for ninety seconds while they pull everything in.",
                Introduces = "Black holes pull you in. Cluster bombs burst into shrapnel.",
                Hints = new[] { "Black holes pull everything in - thrust away from them!", "Cluster bombs count down and burst. Shoot them early or get clear of the ring." },
                Waves = new[]
                {
                    W(rocks: 3, ice: 2, wells: 1, interval: 8f),
                    W(rocks: 3, magma: 2, wells: 1, bombs: 2, pods: 1, interval: 6f),
                    W(ores: 2, ice: 3, wells: 1, bombs: 2, mines: 2, interval: 5f)
                }
            },
            new Mission
            {
                Title = "The Hive", Sector = 2, Objective = LevelObjective.Boss, Boss = "HiveQueen", ScoreGoal = 24000, Speed = 1.1f, SpawnRate = 5.5f, Explosion = 3.4f,
                Description = "Alien wasps nest among the rings, and their queen is angry.",
                Introduces = "Alien wasps hunt you. Boss: the Hive Queen.",
                Hints = new[] { "Wasps chase you - shoot them before they reach you.", "A nova bomb (B) clears the screen when you are swamped." },
                TouchHints = new[] { null, "A nova bomb (NOVA) clears the screen when you are swamped." },
                Waves = new[] { W(rocks: 3, ice: 2, wasps: 4, pods: 1, interval: 3.5f) }
            },
            new Mission
            {
                Title = "Crystal Storm", Sector = 3, Objective = LevelObjective.ClearWaves, ScoreGoal = 24000, Speed = 1.1f, SpawnRate = 5f, Explosion = 3.5f,
                Description = "Void crystals grow in the Core. Shatter one and its shards come hunting for you.",
                Introduces = "Void crystals release homing shards. New weapon: Seeker Missiles.",
                Hints = new[] { "Void crystal shards home in on you - keep moving.", "Seeker missiles find their own targets." },
                Waves = new[]
                {
                    W(rocks: 2, crystals: 2, wasps: 2),
                    W(ice: 2, crystals: 3, saucers: 1, mines: 2, pods: 1),
                    W(magma: 2, crystals: 4, wasps: 3, comets: 1, pods: 1)
                }
            },
            new Mission
            {
                Title = "Gauntlet", Sector = 3, Objective = LevelObjective.Survive, Target = 120, ScoreGoal = 60000, Speed = 1.15f, SpawnRate = 4.5f, Explosion = 3.5f,
                Description = "Everything the Void can throw at you, all at once. Survive for two minutes.",
                Introduces = "Everything you have faced - at once.",
                Hints = new[] { "Wing drones fight at your side for a while.", "A chrono field slows everything but you." },
                Waves = new[]
                {
                    W(rocks: 3, magma: 2, mines: 3, wasps: 2, interval: 5f),
                    W(ice: 3, crystals: 2, saucers: 2, bombs: 2, comets: 1, pods: 1, interval: 4.5f),
                    W(ores: 3, magma: 3, wells: 1, wasps: 3, interval: 4.5f),
                    W(rocks: 3, crystals: 3, mines: 3, saucers: 2, comets: 2, pods: 1, interval: 4f)
                }
            },
            new Mission
            {
                Title = "Dreadnought", Sector = 3, Objective = LevelObjective.Boss, Boss = "Dreadnought", ScoreGoal = 32000, Speed = 1.15f, SpawnRate = 5f, Explosion = 3.5f,
                Description = "The raiders' capital ship guards the heart of the Void. End this.",
                Introduces = "The final boss: the Dreadnought.",
                Hints = new[] { "The Dreadnought fires homing missiles - keep moving.", "Save a nova bomb for when it enrages." },
                Waves = new[] { W(rocks: 3, crystals: 2, saucers: 2, pods: 1, interval: 5f) }
            }
        };

        public static string LevelPath(int index)
        {
            return $"Config/Campaign/AsteroidsLevel{index + 1}.asset";
        }

        private static string SettingsPath(int index)
        {
            return index == 0 ? "Config/AsteroidSettings.asset" : $"Config/Campaign/Settings/Level{index + 1}.asset";
        }

        public static AsteroidsLevel FirstLevel => AsteroidsAssets.Load<AsteroidsLevel>(LevelPath(0));

        public static AsteroidsCampaign Campaign => AsteroidsAssets.Load<AsteroidsCampaign>(CampaignPath);

        private static void BuildMissions()
        {
            var levels = new List<GameLevel>();
            for (int i = 0; i < Missions.Length; i++)
            {
                Mission mission = Missions[i];
                AsteroidSettings settings = BuildSettings(SettingsPath(i), mission.Speed, mission.SpawnRate, mission.Explosion);
                int sector = mission.Sector;
                string themeName = Sectors[sector].theme;
                int index = i;
                AsteroidsLevel level = AsteroidsAssets.SaveScriptable<AsteroidsLevel>(LevelPath(i), l =>
                {
                    Auto(l, nameof(GameLevel.Index), p => p.intValue = index);
                    Auto(l, nameof(GameLevel.Title), p => p.stringValue = mission.Title);
                    Auto(l, nameof(AsteroidsLevel.Settings), p => p.objectReferenceValue = settings);
                    l.description = mission.Description;
                    l.introduces = mission.Introduces;
                    l.hints = mission.Hints;
                    l.touchHints = mission.TouchHints;
                    l.sector = sector;
                    l.theme = Theme(themeName);
                    l.themeRotation = new SectorTheme[0];
                    l.objective = mission.Objective;
                    l.objectiveTarget = mission.Target;
                    l.waves = mission.Waves.Select(wave => wave.Copy()).ToArray();
                    l.boss = string.IsNullOrEmpty(mission.Boss) ? null : AsteroidsPrefabBuilder.Load<Boss>($"Bosses/{mission.Boss}");
                    l.bossRotation = new Boss[0];
                    l.asteroidLoot = LoadLoot($"Rocks{sector + 1}");
                    l.lootChance = 0.06f + sector * 0.005f;
                    l.podLoot = LoadLoot($"Pods{sector + 1}");
                    l.scoreGoal = mission.ScoreGoal;
                    l.speedMultiplier = 1f;
                });
                levels.Add(level);
            }

            AsteroidSettings endlessSettings = BuildSettings("Config/Campaign/Settings/Endless.asset", 1f, 5f, 3.2f);
            AsteroidsLevel endless = AsteroidsAssets.SaveScriptable<AsteroidsLevel>("Config/Campaign/AsteroidsEndless.asset", l =>
            {
                Auto(l, nameof(GameLevel.Index), p => p.intValue = Missions.Length);
                Auto(l, nameof(GameLevel.Title), p => p.stringValue = "Deep Field");
                Auto(l, nameof(AsteroidsLevel.Settings), p => p.objectReferenceValue = endlessSettings);
                l.description = "Endless waves through every sector, with a boss every ten waves. How far can you get?";
                l.introduces = "Endless survival. Your best score and wave are kept.";
                l.hints = new[] { "Waves keep coming and keep growing. Bosses arrive every ten waves." };
                l.sector = Sectors.Length;
                l.theme = Theme("Kepler");
                l.themeRotation = Sectors.Select(sector => Theme(sector.theme)).ToArray();
                l.wavesPerTheme = 5;
                l.objective = LevelObjective.Endless;
                l.objectiveTarget = 0;
                l.waves = new WaveSpec[0];
                l.boss = null;
                l.bossRotation = new[] { "RockTitan", "Mothership", "HiveQueen", "Dreadnought" }.Select(boss => AsteroidsPrefabBuilder.Load<Boss>($"Bosses/{boss}")).ToArray();
                l.bossEvery = 10;
                l.asteroidLoot = LoadLoot("RocksEndless");
                l.lootChance = 0.07f;
                l.podLoot = LoadLoot("PodsEndless");
                l.scoreGoal = 0;
                l.speedMultiplier = 1f;
            });
            levels.Add(endless);

            AsteroidsAssets.SaveScriptable<AsteroidsCampaign>(CampaignPath, campaign =>
            {
                AsteroidsAssets.Set(campaign, "<LevelList>k__BackingField", p =>
                {
                    p.arraySize = levels.Count;
                    for (int i = 0; i < levels.Count; i++)
                    {
                        p.GetArrayElementAtIndex(i).objectReferenceValue = levels[i];
                    }
                });
                campaign.sectors = Sectors.Select(sector => new AsteroidsCampaign.Sector
                {
                    title = sector.title,
                    theme = Theme(sector.theme),
                    starsRequired = sector.stars
                }).ToArray();
                campaign.endlessAfter = 3;
            });
        }

        private static AsteroidSettings BuildSettings(string path, float speed, float spawnRate, float explosion)
        {
            return AsteroidsAssets.SaveScriptable<AsteroidSettings>(path, settings =>
            {
                settings.Lives = 3;
                settings.HullStrength = 100f;
                settings.AsteroidSpeed = speed;
                settings.AsteroidSpawnRate = spawnRate;
                settings.SpawnAsteroid = true;
                settings.AsteroidExplosionRadius = explosion;
            });
        }

        private static void Auto(Object target, string property, Action<SerializedProperty> assign)
        {
            AsteroidsAssets.SetAuto(target, property, assign);
        }

        // ------------------------------------------------------------------ post-processing and launcher

        public static VolumeProfile VolumeProfile => AsteroidsAssets.Load<VolumeProfile>("Config/AsteroidsVolume.asset");

        private static void BuildVolumeProfile()
        {
            AsteroidsAssets.EnsureFolder("Config");
            string path = AsteroidsAssets.Path("Config/AsteroidsVolume.asset");
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }
            var bloom = Get<Bloom>(profile);
            bloom.threshold.Override(0.85f);
            bloom.intensity.Override(1.25f);
            bloom.scatter.Override(0.72f);
            bloom.highQualityFiltering.Override(true);
            var tonemapping = Get<Tonemapping>(profile);
            tonemapping.mode.Override(TonemappingMode.ACES);
            var colors = Get<ColorAdjustments>(profile);
            colors.postExposure.Override(0.35f);
            colors.contrast.Override(12f);
            colors.saturation.Override(10f);
            var vignette = Get<Vignette>(profile);
            vignette.intensity.Override(0.3f);
            vignette.smoothness.Override(0.5f);
            vignette.color.Override(Color.black);
            var aberration = Get<ChromaticAberration>(profile);
            aberration.intensity.Override(0.06f);
            var lens = Get<LensDistortion>(profile);
            lens.intensity.Override(0f);
            var grain = Get<FilmGrain>(profile);
            grain.type.Override(FilmGrainLookup.Thin1);
            grain.intensity.Override(0.12f);
            EditorUtility.SetDirty(profile);
        }

        private static T Get<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (profile.TryGet(out T existing))
            {
                return existing;
            }
            var component = profile.Add<T>(true);
            component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            component.name = typeof(T).Name;
            AssetDatabase.AddObjectToAsset(component, profile);
            return component;
        }

        private static void UpdateGameDefinition()
        {
            var definition = AsteroidsAssets.Load<GameDefinition>("Resources/Games/Asteroids.asset");
            if (definition == null)
            {
                return;
            }
            AsteroidsAssets.Set(definition, "displayName", p => p.stringValue = "Asteroids");
            AsteroidsAssets.Set(definition, "description", p => p.stringValue =
                "Fly through four sectors of space: split asteroids, dodge mines, comets and black holes, fight saucers, wasps and " +
                "four bosses, and upgrade your guns. Twelve missions with three stars each, plus an endless mode.");
            AsteroidsAssets.SetObject(definition, "icon", AsteroidsArtBuilder.Icon("GameIcon"));
            EditorUtility.SetDirty(definition);
        }
    }
}
