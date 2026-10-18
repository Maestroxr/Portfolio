using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Asteroids.EditorTools
{
    /// <summary>
    /// The eighteen bosses of the Iron Tide and Hive Fall campaigns (Prefabs/Strike/Bosses/{name}): a core with parts in a
    /// fixed order, as the Shadow Strike's (<see cref="StrikeBossRoot"/>, <see cref="Part"/>), on the campaigns' own hulls
    /// (SpaceModels.StrikeCampaignBosses.cs) with their new parts: howitzers that shell where the pilot is, SAM racks of
    /// seeker rockets, sweeping guns, hangars and spore sacs that launch aircraft, hive maws and crystal prisms. Three are
    /// tougher cousins of the Shadow Strike's (the Frost Battery on the Dome Fortress's bastion, the Prism Array on the
    /// Twin Silos' pad, the Halo Gate on the Shadow's rings); the others are new. The sockets of a hull are in its model
    /// space (+Z the nose); <see cref="At"/> turns them into the boss's plane as the hull is turned.
    /// </summary>
    internal static partial class AsteroidsPrefabBuilder
    {
        /// <summary>The prefab names of the Iron Tide's bosses, missions 1 to 9.</summary>
        public static readonly string[] IronTideBossNames =
        {
            "GlacierHauler", "FrostBattery", "Icebreaker", "GaleCarrier", "ReefBastion", "SeaWolves", "Drydock", "Battleship", "Leviathan"
        };

        /// <summary>The prefab names of Hive Fall's bosses, missions 1 to 9.</summary>
        public static readonly string[] HiveFallBossNames =
        {
            "BroodMound", "SporeTowers", "BurrowWyrm", "ShardColossus", "PrismArray", "Resonator", "HaloGate", "BroodShip", "HiveMother"
        };

        /// <summary>A hull turned to lie across the screen, its bow to the left (the long ships sail across the fight).</summary>
        private static Quaternion Sideways => FaceDown * Quaternion.Euler(0f, 90f, 0f);

        private static Color IronTint => new Color(1f, 0.62f, 0.28f);
        private static Color HiveTint => new Color(0.65f, 1f, 0.35f);
        private static Color CrystalTint => new Color(1f, 0.45f, 0.95f);


        /// <summary>Where the socket (<paramref name="x"/>, <paramref name="z"/>) of a hull turned by <paramref name="hull"/> lies on the boss's plane.</summary>
        private static Vector2 At(Quaternion hull, float x, float z)
        {
            Vector3 r = hull * new Vector3(x, 0f, z);
            return new Vector2(r.x, r.y);
        }


        private static Mesh CM(string name)
        {
            return AsteroidsArtBuilder.StrikeModel(name);
        }


        private static Vector2[] Same(Vector2 point, int count)
        {
            var points = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                points[i] = point;
            }
            return points;
        }

        // ------------------------------------------------------------------ parts

        /// <summary>A howitzer on a socket: <paramref name="shells"/> shells a volley at the nearest pilot's spot (Barrage).</summary>
        private static BossPart HowitzerPart(Transform parent, string name, Vector2 at, bool ground, float baseHeight, int tier, float health, int score, Material material,
            int shells = 1, float interval = 3.4f, bool large = false)
        {
            return Part(parent, name, at, ground, baseHeight, tier, health, score, large ? 1.3f : 1.05f, AttackPattern.Barrage, 1, 0f, interval, EnemyShotKind.Shell,
                CM(large ? "TurretBaseLarge" : "TurretBase"), CM("HowitzerHead"), material, Same(new Vector2(0f, 3.1f), shells), 0.75f);
        }


        /// <summary>A SAM rack: a pair of seeker rockets a volley.</summary>
        private static BossPart SamPart(Transform parent, string name, Vector2 at, bool ground, float baseHeight, int tier, float health, int score, Material material,
            float interval = 3.6f)
        {
            return Part(parent, name, at, ground, baseHeight, tier, health, score, 0.95f, AttackPattern.Rockets, 1, 0f, interval, EnemyShotKind.Seeker,
                CM("TurretBase"), CM("SamRack"), material, new[] { new Vector2(-0.25f, 0.8f), new Vector2(0.25f, 0.8f) }, 1.2f);
        }


        /// <summary>A sweeping gun: three bolts at a time from a fan that swings from side to side.</summary>
        private static BossPart SweepPart(Transform parent, string name, Vector2 at, bool ground, float baseHeight, int tier, float health, int score, Material material,
            EnemyShotKind shot = EnemyShotKind.Bolt, float interval = 2.6f)
        {
            return Part(parent, name, at, ground, baseHeight, tier, health, score, 1f, AttackPattern.Sweep, 3, 0.25f, interval, shot,
                null, CM("SweepGun"), material, new[] { new Vector2(-0.3f, 1.3f), new Vector2(0f, 1.3f), new Vector2(0.3f, 1.3f) }, 0.4f, false);
        }


        /// <summary>A part that launches <paramref name="unit"/>: a hangar bay or a spore sac.</summary>
        private static BossPart LaunchPart(Transform parent, string name, Vector2 at, bool ground, float baseHeight, int tier, float health, int score, Material material,
            Mesh mesh, float radius, float headHeight, StrikeUnit unit, int perLaunch, int launches, float interval)
        {
            BossPart part = Part(parent, name, at, ground, baseHeight, tier, health, score, radius, AttackPattern.Launch, 1, 0f, interval, EnemyShotKind.Flak,
                null, mesh, material, new[] { Vector2.zero }, headHeight, false);
            UnitLauncher launcher = part.gameObject.AddComponent<UnitLauncher>();
            launcher.unit = unit;
            launcher.perLaunch = perLaunch;
            launcher.launches = launches;
            launcher.paths = unit == StrikeUnit.Drone
                ? new[] { FlightPath.Swoop, FlightPath.Hook, FlightPath.DiveLeft }
                : new[] { FlightPath.DiveLeft, FlightPath.Swoop, FlightPath.Weave };
            return part;
        }


        /// <summary>A hive maw: bursts of spores at the nearest pilot.</summary>
        private static BossPart MawPart(Transform parent, string name, Vector2 at, bool ground, float baseHeight, int tier, float health, int score, Material material,
            int burst = 3, float interval = 2.6f)
        {
            BossPart part = Part(parent, name, at, ground, baseHeight, tier, health, score, 1f, AttackPattern.AimedBurst, burst, 0.22f, interval, EnemyShotKind.Spore,
                null, CM("HiveMaw"), material, new[] { new Vector2(0f, 1.15f) }, 0.55f);
            part.explosionTint = HiveTint;
            return part;
        }


        /// <summary>A spore pod: a fan of <paramref name="spores"/> spores down the screen.</summary>
        private static BossPart SporePodPart(Transform parent, string name, Vector2 at, bool ground, float baseHeight, int tier, float health, int score, Material material,
            int spores = 5, float interval = 2.8f)
        {
            BossPart part = Part(parent, name, at, ground, baseHeight, tier, health, score, 1.1f, AttackPattern.Spread, 1, 0f, interval, EnemyShotKind.Spore,
                null, CM("SporeSac"), material, Same(Vector2.zero, spores), 1.4f, false);
            part.explosionTint = HiveTint;
            return part;
        }


        /// <summary>A crystal prism: a ring of needles, or aimed needles in bursts.</summary>
        private static BossPart PrismPart(Transform parent, string name, Vector2 at, bool ground, float baseHeight, int tier, float health, int score, Material material,
            AttackPattern pattern, int shots, int burst, float interval, bool large = false)
        {
            BossPart part = Part(parent, name, at, ground, baseHeight, tier, health, score, large ? 1.25f : 1f, pattern, burst, 0.2f, interval, EnemyShotKind.Needle,
                null, CM(large ? "CrystalPrismLarge" : "CrystalPrism"), material, Same(Vector2.zero, shots), large ? 1.9f : 1.3f, false);
            part.explosionTint = CrystalTint;
            return part;
        }


        /// <summary>A ring of parts that turns (its structure, depth placed for a ground boss, under it).</summary>
        private static Transform TurningRing(Transform root, string name, Mesh structure, Material material, bool ground, float height)
        {
            Transform ring = Child(root, name);
            if (structure == null)
            {
                return ring;
            }
            if (ground)
            {
                Transform depth = Child(ring, "Depth");
                depth.gameObject.AddComponent<DepthAnchor>().depth = StrikeRules.GroundDepth - height;
                Model(depth, "Structure", structure, material, Vector3.zero, FaceDown);
            }
            else
            {
                Model(ring, "Structure", structure, material, new Vector3(0f, 0f, -height), FaceCamera);
            }
            return ring;
        }


        // ------------------------------------------------------------------ the bosses

        private static void BuildCampaignBosses()
        {
            BuildIronTideBosses();
            BuildHiveFallBosses();
        }


        private static void BuildIronTideBosses()
        {
            Material steel = SM("StrikeIronSteel");

            // 1 Glacier Hauler: a tracked ice hauler; two twin turrets and two SAM racks (tier 0), then its reactor (tier 1).
            {
                StrikeBoss boss = StrikeBossRoot("GlacierHauler", "Glacier Hauler", true, 2f, 360f, 9000, 1, 0.32f, out Transform visual);
                Model(visual, "Hull", CM("HaulerHull"), steel, Vector3.zero, FaceDown);
                Model(visual, "Reactor", CM("Radome"), steel, new Vector3(0f, -0.2f, -2f), FaceDown);
                var parts = new List<BossPart>
                {
                    TwinTurret(boss.transform, "TurretLeft", At(FaceDown, 1.75f, 2.2f), true, 2f, 0, 110f, 1500, steel, 2, 1.7f),
                    TwinTurret(boss.transform, "TurretRight", At(FaceDown, -1.75f, 2.2f), true, 2f, 0, 110f, 1500, steel, 2, 1.7f),
                    SamPart(boss.transform, "SamLeft", At(FaceDown, 1.75f, -3.9f), true, 2f, 0, 90f, 1200, steel, 4f),
                    SamPart(boss.transform, "SamRight", At(FaceDown, -1.75f, -3.9f), true, 2f, 0, 90f, 1200, steel, 4f)
                };
                FinishBoss(boss, visual, parts, IronTint,
                    Attack(BossAttackType.AimedBurst, 2.8f, 3, 9f, 0, 20f, EnemyShotKind.Bolt),
                    Attack(BossAttackType.SpreadShot, 4.2f, 5, 7f, 1, 60f, EnemyShotKind.Flak));
                SaveStrike(boss.gameObject, "Bosses/GlacierHauler");
            }

            // 2 Frost Battery: the Dome Fortress's bastion, rearmed: two howitzers and two SAM racks (tier 0), then the command radome.
            {
                StrikeBoss boss = StrikeBossRoot("FrostBattery", "Frost Battery", true, 2.4f, 650f, 14000, 1, 0.4f, out Transform visual);
                Model(visual, "Bastion", CM("Bastion"), steel, Vector3.zero, FaceDown);
                Model(visual, "Radome", CM("RadomeLarge"), steel, new Vector3(0f, 0f, -1.5f), FaceDown);
                var parts = new List<BossPart>
                {
                    HowitzerPart(boss.transform, "HowitzerLeft", new Vector2(-3.6f, -2.6f), true, 1.6f, 0, 170f, 2000, steel, 2, 3.6f),
                    HowitzerPart(boss.transform, "HowitzerRight", new Vector2(3.6f, -2.6f), true, 1.6f, 0, 170f, 2000, steel, 2, 3.6f),
                    SamPart(boss.transform, "SamLeft", new Vector2(-3.6f, 2.6f), true, 1.6f, 0, 140f, 1800, steel),
                    SamPart(boss.transform, "SamRight", new Vector2(3.6f, 2.6f), true, 1.6f, 0, 140f, 1800, steel)
                };
                FinishBoss(boss, visual, parts, IronTint,
                    Attack(BossAttackType.SpreadShot, 4f, 5, 7f, 0, 60f, EnemyShotKind.Flak),
                    Attack(BossAttackType.RingShot, 4.6f, 14, 7f, 1, 0f, EnemyShotKind.Bolt),
                    Attack(BossAttackType.AimedBurst, 3.2f, 4, 10f, 1, 15f, EnemyShotKind.Flak));
                SaveStrike(boss.gameObject, "Bosses/FrostBattery");
            }

            // 3 Icebreaker: a ship across the screen; bow and stern turrets, a howitzer and two launchers (tier 0), then the bridge.
            {
                StrikeBoss boss = StrikeBossRoot("Icebreaker", "Icebreaker", true, 2.2f, 1000f, 110000, 1, 0.5f, out Transform visual);
                Model(visual, "Hull", CM("IcebreakerHull"), steel, Vector3.zero, Sideways);
                var parts = new List<BossPart>
                {
                    TwinTurret(boss.transform, "BowTurret", At(Sideways, 0f, 5.2f), true, 1.4f, 0, 260f, 2000, steel, 3, 1.6f),
                    HowitzerPart(boss.transform, "Howitzer", At(Sideways, 0f, 2.9f), true, 1.4f, 0, 300f, 2500, steel, 2, 3.4f),
                    LauncherPart(boss.transform, "LauncherPort", At(Sideways, 1.3f, -3.6f), true, 1.4f, 0, 240f, 2000, steel),
                    LauncherPart(boss.transform, "LauncherStarboard", At(Sideways, -1.3f, -3.6f), true, 1.4f, 0, 240f, 2000, steel),
                    FlakTurret(boss.transform, "SternFlak", At(Sideways, 0f, -5.8f), true, 1.4f, 0, 260f, 2000, steel)
                };
                FinishBoss(boss, visual, parts, IronTint,
                    Attack(BossAttackType.SpreadShot, 3.4f, 5, 8f, 0, 50f, EnemyShotKind.Bolt),
                    Attack(BossAttackType.RingShot, 5f, 12, 6.5f, 1, 0f, EnemyShotKind.Flak),
                    Attack(BossAttackType.AimedBurst, 3.8f, 1, 0f, 1, 0f, EnemyShotKind.Beam));
                boss.sweep = 3f;
                boss.sweepSpeed = 0.3f;
                SaveStrike(boss.gameObject, "Bosses/Icebreaker");
            }

            // 4 Gale Carrier: a flying wing; two hangars launching drone swarms and two flak turrets (tier 0), then the carrier.
            {
                StrikeBoss boss = StrikeBossRoot("GaleCarrier", "Gale Carrier", false, 2.2f, 900f, 22000, 1, 0.5f, out Transform visual);
                Model(visual, "Hull", CM("CarrierHull"), steel, Vector3.zero, FaceDown);
                foreach ((float x, float z, string name) in new[] { (1f, -3.55f, "EngineA"), (-1f, -3.55f, "EngineB"), (4.6f, -2.55f, "EngineC"), (-4.6f, -2.55f, "EngineD") })
                {
                    EngineFlame(boss.transform, name, At(FaceDown, x, z) + new Vector2(0f, 0.5f), 1.8f);
                }
                var parts = new List<BossPart>
                {
                    LaunchPart(boss.transform, "HangarLeft", At(FaceDown, 3.6f, -0.4f), false, 0.85f, 0, 300f, 2500, steel, CM("HangarBay"), 1.3f, 0.8f, StrikeUnit.Drone, 3, 4, 6.5f),
                    LaunchPart(boss.transform, "HangarRight", At(FaceDown, -3.6f, -0.4f), false, 0.85f, 0, 300f, 2500, steel, CM("HangarBay"), 1.3f, 0.8f, StrikeUnit.Drone, 3, 4, 6.5f),
                    FlakTurret(boss.transform, "FlakLeft", At(FaceDown, 5.6f, 1.6f), false, 0.85f, 0, 250f, 2000, steel),
                    FlakTurret(boss.transform, "FlakRight", At(FaceDown, -5.6f, 1.6f), false, 0.85f, 0, 250f, 2000, steel)
                };
                FinishBoss(boss, visual, parts, IronTint,
                    Attack(BossAttackType.SpreadShot, 3f, 5, 8f, 0, 50f, EnemyShotKind.Bolt),
                    Attack(BossAttackType.RingShot, 5f, 12, 6f, 1, 0f, EnemyShotKind.Flak));
                boss.sweep = 8f;
                boss.sweepSpeed = 0.4f;
                AddShadow(boss.transform);
                SaveStrike(boss.gameObject, "Bosses/GaleCarrier");
            }

            // 5 Reef Bastion: a sea fort; four howitzer towers and two sweeping guns (tier 0), then the keep.
            {
                StrikeBoss boss = StrikeBossRoot("ReefBastion", "Reef Bastion", true, 2.3f, 1100f, 32000, 1, 0.45f, out Transform visual);
                Model(visual, "Fort", CM("AtollFortress"), steel, Vector3.zero, FaceDown);
                Model(visual, "Keep", CM("RadomeLarge"), steel, new Vector3(0f, 0f, -2.3f), FaceDown);
                var parts = new List<BossPart>
                {
                    HowitzerPart(boss.transform, "TowerFrontLeft", At(FaceDown, 4.3f, 3.1f), true, 1.9f, 0, 260f, 2500, steel, 1, 3f),
                    HowitzerPart(boss.transform, "TowerFrontRight", At(FaceDown, -4.3f, 3.1f), true, 1.9f, 0, 260f, 2500, steel, 1, 3f),
                    HowitzerPart(boss.transform, "TowerBackLeft", At(FaceDown, 4.3f, -3.1f), true, 1.9f, 0, 260f, 2500, steel, 1, 3.4f),
                    HowitzerPart(boss.transform, "TowerBackRight", At(FaceDown, -4.3f, -3.1f), true, 1.9f, 0, 260f, 2500, steel, 1, 3.4f),
                    SweepPart(boss.transform, "SweeperLeft", At(FaceDown, 2f, 4.4f), true, 1.75f, 0, 280f, 2500, steel),
                    SweepPart(boss.transform, "SweeperRight", At(FaceDown, -2f, 4.4f), true, 1.75f, 0, 280f, 2500, steel)
                };
                FinishBoss(boss, visual, parts, IronTint,
                    Attack(BossAttackType.AimedBurst, 3f, 4, 10f, 0, 15f, EnemyShotKind.Flak),
                    Attack(BossAttackType.RingShot, 4.5f, 14, 7f, 1, 0f, EnemyShotKind.Bolt));
                SaveStrike(boss.gameObject, "Bosses/ReefBastion");
            }

            // 6 Sea Wolves: two hydrofoils as one pack, without a core; each a turret and a SAM rack. The pack sinks with the last.
            {
                StrikeBoss boss = StrikeBossRoot("SeaWolves", "Sea Wolves", true, 1.5f, 1f, 240000, 0, 0.4f, out Transform visual);
                Model(visual, "Hulls", CM("Hydrofoils"), steel, Vector3.zero, FaceDown);
                boss.coreless = true;
                boss.invulnerable = true;
                var parts = new List<BossPart>
                {
                    TwinTurret(boss.transform, "TurretLeft", At(FaceDown, 3.6f, 1.9f), true, 1.25f, 0, 450f, 3000, steel, 3, 1.5f),
                    TwinTurret(boss.transform, "TurretRight", At(FaceDown, -3.6f, 1.9f), true, 1.25f, 0, 450f, 3000, steel, 3, 1.5f),
                    SamPart(boss.transform, "SamLeft", At(FaceDown, 3.6f, -2.2f), true, 1.25f, 0, 400f, 3000, steel, 3.2f),
                    SamPart(boss.transform, "SamRight", At(FaceDown, -3.6f, -2.2f), true, 1.25f, 0, 400f, 3000, steel, 3.2f)
                };
                FinishBoss(boss, visual, parts, IronTint,
                    Attack(BossAttackType.SpreadShot, 3.6f, 5, 8f, 0, 60f, EnemyShotKind.Bolt));
                boss.sweep = 7f;
                boss.sweepSpeed = 0.5f;
                SaveStrike(boss.gameObject, "Bosses/SeaWolves");
            }

            // 7 Drydock: a dock with a gantry crane; two sweeping guns and two SAM racks on its walls (tier 0), then the crane.
            {
                StrikeBoss boss = StrikeBossRoot("Drydock", "Drydock", true, 2f, 1500f, 48000, 1, 0.45f, out Transform visual);
                Model(visual, "Dock", CM("Drydock"), steel, Vector3.zero, FaceDown);
                Model(visual, "CraneGun", CM("SweepGun"), steel, new Vector3(0f, 0f, -4.05f), FaceDown);
                var parts = new List<BossPart>
                {
                    SweepPart(boss.transform, "SweeperLeft", At(FaceDown, 5.7f, 2.6f), true, 2.2f, 0, 450f, 3000, steel),
                    SweepPart(boss.transform, "SweeperRight", At(FaceDown, -5.7f, 2.6f), true, 2.2f, 0, 450f, 3000, steel),
                    SamPart(boss.transform, "SamLeft", At(FaceDown, 5.7f, -2.6f), true, 2.2f, 0, 400f, 3000, steel, 3.4f),
                    SamPart(boss.transform, "SamRight", At(FaceDown, -5.7f, -2.6f), true, 2.2f, 0, 400f, 3000, steel, 3.4f)
                };
                FinishBoss(boss, visual, parts, IronTint,
                    Attack(BossAttackType.AimedBurst, 3.2f, 5, 11f, 0, 15f, EnemyShotKind.Flak),
                    Attack(BossAttackType.SpreadShot, 3.8f, 7, 8f, 1, 70f, EnemyShotKind.Bolt),
                    Attack(BossAttackType.AimedBurst, 4.5f, 1, 0f, 1, 0f, EnemyShotKind.Beam));
                SaveStrike(boss.gameObject, "Bosses/Drydock");
            }

            // 8 Battleship: across the screen; eight guns along its sides (tier 0), two main howitzer turrets (tier 1), the bridge (tier 2).
            {
                StrikeBoss boss = StrikeBossRoot("Battleship", "Battleship", true, 2.4f, 1500f, 70000, 2, 0.5f, out Transform visual);
                Model(visual, "Hull", CM("BattleshipHull"), steel, Vector3.zero, Sideways);
                var parts = new List<BossPart>();
                foreach (float z in new[] { 7f, -7f })
                {
                    parts.Add(TwinTurret(boss.transform, $"Gun{(z > 0f ? "Bow" : "Stern")}Port", At(Sideways, 1.75f, z), true, 1.5f, 0, 240f, 2500, steel, 2, 1.8f));
                    parts.Add(TwinTurret(boss.transform, $"Gun{(z > 0f ? "Bow" : "Stern")}Starboard", At(Sideways, -1.75f, z), true, 1.5f, 0, 240f, 2500, steel, 2, 1.8f));
                }
                foreach (float z in new[] { 2.4f, -2.4f })
                {
                    parts.Add(SamPart(boss.transform, $"Sam{(z > 0f ? "Fore" : "Aft")}", At(Sideways, 1.75f, z), true, 1.5f, 0, 260f, 2500, steel, 4f));
                    parts.Add(FlakTurret(boss.transform, $"Flak{(z > 0f ? "Fore" : "Aft")}", At(Sideways, -1.75f, z), true, 1.5f, 0, 240f, 2500, steel));
                }
                parts.Add(HowitzerPart(boss.transform, "MainFore", At(Sideways, 0f, 4.6f), true, 1.8f, 1, 600f, 4000, steel, 2, 3.6f, true));
                parts.Add(HowitzerPart(boss.transform, "MainAft", At(Sideways, 0f, -4.6f), true, 1.8f, 1, 600f, 4000, steel, 2, 4f, true));
                FinishBoss(boss, visual, parts, IronTint,
                    Attack(BossAttackType.SpreadShot, 4f, 5, 8f, 0, 50f, EnemyShotKind.Bolt),
                    Attack(BossAttackType.AimedBurst, 3f, 4, 10f, 1, 15f, EnemyShotKind.Flak),
                    Attack(BossAttackType.RingShot, 3.4f, 16, 7f, 2, 0f, EnemyShotKind.Bolt),
                    Attack(BossAttackType.AimedBurst, 4f, 1, 0f, 2, 0f, EnemyShotKind.Beam));
                boss.sweep = 2.5f;
                boss.sweepSpeed = 0.25f;
                boss.alarm = true;
                SaveStrike(boss.gameObject, "Bosses/Battleship");
            }

            // 9 Leviathan: the flagship; six wing guns (tier 0), hangars and sweeping guns (tier 1), the reactor crown (tier 2).
            {
                StrikeBoss boss = StrikeBossRoot("Leviathan", "Leviathan", false, 2.6f, 2600f, 520000, 2, 0.42f, out Transform visual);
                Model(visual, "Hull", CM("LeviathanHull"), steel, Vector3.zero, FaceDown);
                Model(visual, "Crown", CM("ReactorCrown"), steel, new Vector3(0f, -0.6f, -2.4f), FaceDown);
                for (int i = 0; i < 3; i++)
                {
                    EngineFlame(boss.transform, $"Engine{i + 1}", At(FaceDown, -1.3f + i * 1.3f, -5.2f) + new Vector2(0f, 0.4f), 2f);
                }
                EngineFlame(boss.transform, "NacelleLeft", At(FaceDown, 6.2f, -3.4f) + new Vector2(0f, 0.6f), 2.4f);
                EngineFlame(boss.transform, "NacelleRight", At(FaceDown, -6.2f, -3.4f) + new Vector2(0f, 0.6f), 2.4f);
                var parts = new List<BossPart>
                {
                    FlakTurret(boss.transform, "WingFlakLeft", At(FaceDown, 8.2f, 0.4f), false, 1.1f, 0, 300f, 4000, steel),
                    FlakTurret(boss.transform, "WingFlakRight", At(FaceDown, -8.2f, 0.4f), false, 1.1f, 0, 300f, 4000, steel),
                    SamPart(boss.transform, "WingSamLeft", At(FaceDown, 5.6f, 1.9f), false, 1.1f, 0, 320f, 4000, steel, 3.6f),
                    SamPart(boss.transform, "WingSamRight", At(FaceDown, -5.6f, 1.9f), false, 1.1f, 0, 320f, 4000, steel, 3.6f),
                    TwinTurret(boss.transform, "BowTurretLeft", At(FaceDown, 3.4f, 3f), false, 1.1f, 0, 300f, 4000, steel, 3, 1.6f),
                    TwinTurret(boss.transform, "BowTurretRight", At(FaceDown, -3.4f, 3f), false, 1.1f, 0, 300f, 4000, steel, 3, 1.6f),
                    LaunchPart(boss.transform, "HangarLeft", At(FaceDown, 3.6f, -1.6f), false, 1.1f, 1, 500f, 5000, steel, CM("HangarBay"), 1.3f, 0.8f, StrikeUnit.Drone, 3, 5, 6f),
                    LaunchPart(boss.transform, "HangarRight", At(FaceDown, -3.6f, -1.6f), false, 1.1f, 1, 500f, 5000, steel, CM("HangarBay"), 1.3f, 0.8f, StrikeUnit.Drone, 3, 5, 6f),
                    SweepPart(boss.transform, "SweeperLeft", At(FaceDown, 6.6f, -1.2f), false, 1.1f, 1, 500f, 5000, steel),
                    SweepPart(boss.transform, "SweeperRight", At(FaceDown, -6.6f, -1.2f), false, 1.1f, 1, 500f, 5000, steel)
                };
                FinishBoss(boss, visual, parts, IronTint,
                    Attack(BossAttackType.SpreadShot, 3.4f, 7, 8f, 0, 80f, EnemyShotKind.Bolt),
                    Attack(BossAttackType.AimedBurst, 3f, 5, 11f, 1, 15f, EnemyShotKind.Flak),
                    Attack(BossAttackType.RingShot, 2.8f, 18, 7f, 2, 0f, EnemyShotKind.Flak),
                    Attack(BossAttackType.AimedBurst, 3.8f, 1, 0f, 2, 0f, EnemyShotKind.Beam),
                    Attack(BossAttackType.SpreadShot, 3.2f, 5, 6f, 2, 60f, EnemyShotKind.Seeker));
                boss.sweep = 5f;
                boss.sweepSpeed = 0.22f;
                boss.alarm = true;
                AddShadow(boss.transform);
                SaveStrike(boss.gameObject, "Bosses/Leviathan");
            }
        }


        private static void BuildHiveFallBosses()
        {
            Material flesh = SM("StrikeHivePurple");
            Material crystal = SM("StrikeCrystalRose");

            // 1 Brood Mound: two maws and a spore sac that launches stingers (tier 0), then the heart in its well (tier 1).
            {
                StrikeBoss boss = StrikeBossRoot("BroodMound", "Brood Mound", true, 1.9f, 380f, 10000, 1, 0.45f, out Transform visual);
                Model(visual, "Mound", CM("BroodMound"), flesh, Vector3.zero, FaceDown);
                Model(visual, "Heart", CM("HiveHeart"), flesh, new Vector3(0f, 0f, -1.7f), FaceDown);
                var parts = new List<BossPart>
                {
                    MawPart(boss.transform, "MawLeft", new Vector2(-3f, -1.6f), true, 1.1f, 0, 130f, 1500, flesh),
                    MawPart(boss.transform, "MawRight", new Vector2(3f, -1.6f), true, 1.1f, 0, 130f, 1500, flesh),
                    LaunchPart(boss.transform, "BroodSac", new Vector2(0f, 2.8f), true, 1.2f, 0, 120f, 1500, flesh, CM("SporeSac"), 1.1f, 1.4f, StrikeUnit.Stinger, 2, 3, 7f)
                };
                FinishBoss(boss, visual, parts, HiveTint,
                    Attack(BossAttackType.AimedBurst, 3f, 3, 5f, 0, 15f, EnemyShotKind.Spore),
                    Attack(BossAttackType.SpreadShot, 4.5f, 5, 4.5f, 1, 60f, EnemyShotKind.Spore));
                SaveStrike(boss.gameObject, "Bosses/BroodMound");
            }

            // 2 Spore Towers: two towers spraying spores and two sacs launching stingers, without a core.
            {
                StrikeBoss boss = StrikeBossRoot("SporeTowers", "Spore Towers", true, 1.5f, 1f, 16000, 0, 0.42f, out Transform visual);
                Model(visual, "Ridge", CM("TowerRidge"), flesh, Vector3.zero, FaceDown);
                boss.coreless = true;
                boss.invulnerable = true;
                var parts = new List<BossPart>
                {
                    Part(boss.transform, "TowerLeft", new Vector2(-4.2f, 0f), true, 0.6f, 0, 600f, 3000, 1.5f, AttackPattern.Spread, 1, 0f, 2.6f, EnemyShotKind.Spore,
                        null, CM("SporeTower"), flesh, Same(Vector2.zero, 5), 3.7f, false),
                    Part(boss.transform, "TowerRight", new Vector2(4.2f, 0f), true, 0.6f, 0, 600f, 3000, 1.5f, AttackPattern.Spread, 1, 0f, 2.6f, EnemyShotKind.Spore,
                        null, CM("SporeTower"), flesh, Same(Vector2.zero, 5), 3.7f, false),
                    LaunchPart(boss.transform, "SacLeft", new Vector2(-1.4f, 1f), true, 1f, 0, 220f, 2000, flesh, CM("SporeSac"), 1f, 1.4f, StrikeUnit.Stinger, 1, 4, 6.5f),
                    LaunchPart(boss.transform, "SacRight", new Vector2(1.4f, 1f), true, 1f, 0, 220f, 2000, flesh, CM("SporeSac"), 1f, 1.4f, StrikeUnit.Stinger, 1, 4, 6.5f)
                };
                foreach (BossPart part in parts)
                {
                    part.explosionTint = HiveTint;
                }
                FinishBoss(boss, visual, parts, HiveTint,
                    Attack(BossAttackType.RingShot, 5.5f, 10, 4.5f, 0, 0f, EnemyShotKind.Spore));
                SaveStrike(boss.gameObject, "Bosses/SporeTowers");
            }

            // 3 Burrow Wyrm: a worm rising from its burrow; three tail segments (tier 0), two neck segments (tier 1), the head (tier 2).
            {
                StrikeBoss boss = StrikeBossRoot("BurrowWyrm", "Burrow Wyrm", true, 2f, 900f, 120000, 2, 0.2f, out Transform visual);
                Model(visual, "Burrow", CM("Burrow"), flesh, Vector3.zero, FaceDown);
                Model(visual, "Head", CM("WyrmHead"), flesh, Vector3.zero, FaceDown);
                Vector2[] spine = { new Vector2(0.8f, 1.9f), new Vector2(1.7f, 3.6f), new Vector2(1.2f, 5.3f), new Vector2(-0.3f, 6.3f), new Vector2(-1.3f, 7.1f) };
                var parts = new List<BossPart>();
                for (int i = spine.Length - 1; i >= 0; i--)
                {
                    bool neck = i < 2;
                    BossPart segment = neck
                        ? Part(boss.transform, $"Neck{i + 1}", spine[i], true, 0.2f, 1, 340f, 3000, 1.3f, AttackPattern.Ring, 1, 0f, 3.2f, EnemyShotKind.Spore,
                            null, CM("WyrmSegmentLarge"), flesh, Same(Vector2.zero, 8), 1.4f, false)
                        : Part(boss.transform, $"Tail{spine.Length - i}", spine[i], true, 0.2f, 0, 260f, 2500, 1.1f, AttackPattern.Sweep, 2, 0.3f, 2.4f, EnemyShotKind.Spore,
                            null, CM("WyrmSegment"), flesh, Same(Vector2.zero, 2), 1.2f, false);
                    segment.explosionTint = HiveTint;
                    parts.Add(segment);
                }
                FinishBoss(boss, visual, parts, HiveTint,
                    Attack(BossAttackType.SpreadShot, 3.4f, 5, 5f, 0, 70f, EnemyShotKind.Spore),
                    Attack(BossAttackType.AimedBurst, 3f, 4, 6f, 1, 15f, EnemyShotKind.Spore),
                    Attack(BossAttackType.RingShot, 3.6f, 14, 4.5f, 2, 0f, EnemyShotKind.Spore));
                boss.sweep = 4f;
                boss.sweepSpeed = 0.5f;
                SaveStrike(boss.gameObject, "Bosses/BurrowWyrm");
            }

            // 4 Shard Colossus: shoulder prisms firing rings of needles and lasers in its arms (tier 0), then its heart.
            {
                StrikeBoss boss = StrikeBossRoot("ShardColossus", "Shard Colossus", true, 2.1f, 1100f, 24000, 1, 0.45f, out Transform visual);
                Model(visual, "Body", CM("ColossusBody"), crystal, Vector3.zero, FaceDown);
                Model(visual, "Heart", CM("CrystalHeart"), crystal, new Vector3(0f, 0.4f, -1.4f), FaceDown);
                var parts = new List<BossPart>
                {
                    PrismPart(boss.transform, "ShoulderLeft", At(FaceDown, 2.8f, -0.6f), true, 1.6f, 0, 320f, 2500, crystal, AttackPattern.Ring, 10, 1, 3.2f),
                    PrismPart(boss.transform, "ShoulderRight", At(FaceDown, -2.8f, -0.6f), true, 1.6f, 0, 320f, 2500, crystal, AttackPattern.Ring, 10, 1, 3.6f),
                    LaserPart(boss.transform, "ArmLeft", At(FaceDown, 4.8f, 2.6f), true, 0.7f, 0, 300f, 2500, crystal, CM("CrystalPrismLarge"), 1.1f, 3.4f),
                    LaserPart(boss.transform, "ArmRight", At(FaceDown, -4.8f, 2.6f), true, 0.7f, 0, 300f, 2500, crystal, CM("CrystalPrismLarge"), 1.1f, 3.8f)
                };
                foreach (BossPart part in parts)
                {
                    part.explosionTint = CrystalTint;
                }
                FinishBoss(boss, visual, parts, CrystalTint,
                    Attack(BossAttackType.AimedBurst, 2.8f, 3, 12f, 0, 15f, EnemyShotKind.Needle),
                    Attack(BossAttackType.RingShot, 4f, 14, 6f, 1, 0f, EnemyShotKind.Needle));
                SaveStrike(boss.gameObject, "Bosses/ShardColossus");
            }

            // 5 Prism Array: the Twin Silos' pad grown over with crystal: two laser prisms and a ring prism, without a core.
            {
                StrikeBoss boss = StrikeBossRoot("PrismArray", "Prism Array", true, 1.5f, 1f, 34000, 0, 0.4f, out Transform visual);
                Model(visual, "Pad", CM("SiloPad"), crystal, Vector3.zero, FaceDown);
                boss.coreless = true;
                boss.invulnerable = true;
                var parts = new List<BossPart>
                {
                    LaserPart(boss.transform, "PrismLeft", new Vector2(-4.2f, 0f), true, 0.6f, 0, 800f, 3000, crystal, CM("CrystalPrismLarge"), 1.5f, 3f),
                    PrismPart(boss.transform, "PrismCentre", new Vector2(0f, 0f), true, 1.3f, 0, 900f, 3000, crystal, AttackPattern.Ring, 12, 1, 3f, true),
                    LaserPart(boss.transform, "PrismRight", new Vector2(4.2f, 0f), true, 0.6f, 0, 800f, 3000, crystal, CM("CrystalPrismLarge"), 1.5f, 3.3f)
                };
                foreach (BossPart part in parts)
                {
                    part.explosionTint = CrystalTint;
                }
                FinishBoss(boss, visual, parts, CrystalTint,
                    Attack(BossAttackType.SpreadShot, 4f, 5, 7f, 0, 70f, EnemyShotKind.Needle));
                SaveStrike(boss.gameObject, "Bosses/PrismArray");
            }

            // 6 Resonator: a turning ring of six crystal emitters (tier 0), then the singing crystal at its heart.
            {
                StrikeBoss boss = StrikeBossRoot("Resonator", "Resonator", true, 2.2f, 1600f, 260000, 1, 0.38f, out Transform visual);
                Model(visual, "Base", CM("ResonatorBase"), crystal, Vector3.zero, FaceDown);
                Model(visual, "Heart", CM("CrystalHeart"), crystal, new Vector3(0f, 0f, -1.3f), FaceDown, Vector3.one * 1.25f);
                Transform ring = TurningRing(boss.transform, "Ring", CM("CrystalRing"), crystal, true, 1.3f);
                var parts = new List<BossPart>();
                for (int i = 0; i < 6; i++)
                {
                    float angle = (i * 60f + 30f) * Mathf.Deg2Rad;
                    var at = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 5.2f;
                    parts.Add(PrismPart(ring, $"Emitter{i + 1}", at, true, 1.3f, 0, 260f, 3000, crystal, AttackPattern.Aimed, 2, 2, 2.4f));
                }
                boss.rings = new[] { ring };
                boss.ringSpeeds = new[] { 16f };
                FinishBoss(boss, visual, parts, CrystalTint,
                    Attack(BossAttackType.RingShot, 3.8f, 16, 6f, 0, 0f, EnemyShotKind.Needle),
                    Attack(BossAttackType.AimedBurst, 4f, 1, 0f, 1, 0f, EnemyShotKind.Beam),
                    Attack(BossAttackType.SpreadShot, 3f, 7, 9f, 1, 80f, EnemyShotKind.Needle));
                SaveStrike(boss.gameObject, "Bosses/Resonator");
            }

            // 7 Halo Gate: the Shadow's station rings, taken by the Hive: sacs and maws on the turning ring (tier 0), then the gate's core.
            {
                StrikeBoss boss = StrikeBossRoot("HaloGate", "Halo Gate", false, 2.4f, 2000f, 52000, 1, 0.42f, out Transform visual);
                Model(visual, "Core", CM("StationCore"), flesh, Vector3.zero, FaceCamera);
                Transform ring = TurningRing(boss.transform, "OuterRing", CM("StationOuterRing"), SM("StrikeIronSteel"), false, -0.3f);
                var parts = new List<BossPart>();
                for (int i = 0; i < 6; i++)
                {
                    float angle = (i * 60f + 90f) * Mathf.Deg2Rad;
                    var at = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 6.2f;
                    parts.Add(i % 2 == 0
                        ? LaunchPart(ring, $"Sac{i / 2 + 1}", at, false, 0.6f, 0, 300f, 4000, flesh, CM("SporeSac"), 1.1f, 1.4f, StrikeUnit.Stinger, 1, 4, 7f)
                        : MawPart(ring, $"Maw{i / 2 + 1}", at, false, 0.6f, 0, 300f, 4000, flesh, 3, 2.4f));
                }
                boss.rings = new[] { ring };
                boss.ringSpeeds = new[] { 10f };
                FinishBoss(boss, visual, parts, HiveTint,
                    Attack(BossAttackType.RingShot, 4f, 14, 4.5f, 0, 0f, EnemyShotKind.Spore),
                    Attack(BossAttackType.AimedBurst, 3f, 4, 10f, 1, 15f, EnemyShotKind.Bolt),
                    Attack(BossAttackType.SpreadShot, 3.4f, 5, 5f, 1, 70f, EnemyShotKind.Spore));
                boss.sweep = 4f;
                boss.sweepSpeed = 0.25f;
                AddShadow(boss.transform);
                SaveStrike(boss.gameObject, "Bosses/HaloGate");
            }

            // 8 Brood Ship: maws and stinger sacs (tier 0), spines that sweep spores (tier 1), then the ship.
            {
                StrikeBoss boss = StrikeBossRoot("BroodShip", "Brood Ship", false, 2.2f, 1700f, 75000, 2, 0.5f, out Transform visual);
                Model(visual, "Body", CM("BroodShipBody"), flesh, Vector3.zero, FaceDown);
                var parts = new List<BossPart>
                {
                    MawPart(boss.transform, "MawLeft", At(FaceDown, 1.9f, 3.4f), false, 1.3f, 0, 380f, 4000, flesh, 3, 2.4f),
                    MawPart(boss.transform, "MawRight", At(FaceDown, -1.9f, 3.4f), false, 1.3f, 0, 380f, 4000, flesh, 3, 2.4f),
                    LaunchPart(boss.transform, "SacLeft", At(FaceDown, 4.6f, 0.2f), false, 0.9f, 0, 350f, 4000, flesh, CM("SporeSac"), 1.1f, 1.4f, StrikeUnit.Stinger, 2, 4, 6.5f),
                    LaunchPart(boss.transform, "SacRight", At(FaceDown, -4.6f, 0.2f), false, 0.9f, 0, 350f, 4000, flesh, CM("SporeSac"), 1.1f, 1.4f, StrikeUnit.Stinger, 2, 4, 6.5f),
                    SweepPart(boss.transform, "SpineLeft", At(FaceDown, 2.2f, -3.6f), false, 1.3f, 1, 450f, 5000, flesh, EnemyShotKind.Spore, 2.4f),
                    SweepPart(boss.transform, "SpineRight", At(FaceDown, -2.2f, -3.6f), false, 1.3f, 1, 450f, 5000, flesh, EnemyShotKind.Spore, 2.4f)
                };
                foreach (BossPart part in parts)
                {
                    part.explosionTint = HiveTint;
                }
                FinishBoss(boss, visual, parts, HiveTint,
                    Attack(BossAttackType.SpreadShot, 3.2f, 5, 5f, 0, 70f, EnemyShotKind.Spore),
                    Attack(BossAttackType.AimedBurst, 3f, 4, 6f, 1, 15f, EnemyShotKind.Spore),
                    Attack(BossAttackType.RingShot, 3.4f, 16, 4.5f, 2, 0f, EnemyShotKind.Spore));
                boss.sweep = 7f;
                boss.sweepSpeed = 0.35f;
                AddShadow(boss.transform);
                SaveStrike(boss.gameObject, "Bosses/BroodShip");
            }

            // 9 Hive Mother: a ring of six sacs (tier 0), a ring of four maws (tier 1), then her heart (tier 2), in three phases.
            {
                StrikeBoss boss = StrikeBossRoot("HiveMother", "Hive Mother", true, 2.6f, 3200f, 600000, 2, 0.35f, out Transform visual);
                Model(visual, "Body", CM("HiveMotherBody"), flesh, Vector3.zero, FaceDown);
                Model(visual, "Heart", CM("HiveHeartLarge"), flesh, new Vector3(0f, 0f, -2.4f), FaceDown);
                Transform outer = TurningRing(boss.transform, "OuterRing", null, flesh, true, 0f);
                Transform inner = TurningRing(boss.transform, "InnerRing", null, flesh, true, 0f);
                var parts = new List<BossPart>();
                for (int i = 0; i < 6; i++)
                {
                    float angle = (i * 60f + 30f) * Mathf.Deg2Rad;
                    var at = new Vector2(Mathf.Cos(angle) * 6.6f, Mathf.Sin(angle) * 6.2f);
                    parts.Add(i % 2 == 0
                        ? LaunchPart(outer, $"Sac{i + 1}", at, true, 0.5f, 0, 300f, 5000, flesh, CM("SporeSacLarge"), 1.3f, 1.9f, StrikeUnit.Stinger, 2, 4, 7.5f)
                        : SporePodPart(outer, $"Pod{i + 1}", at, true, 0.5f, 0, 300f, 5000, flesh, 5, 3f));
                }
                for (int i = 0; i < 4; i++)
                {
                    float angle = (i * 90f + 45f) * Mathf.Deg2Rad;
                    var at = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 3.9f;
                    parts.Add(i % 2 == 0
                        ? MawPart(inner, $"Maw{i + 1}", at, true, 1.4f, 1, 500f, 6000, flesh, 4, 2.6f)
                        : SweepPart(inner, $"Spine{i + 1}", at, true, 1.4f, 1, 500f, 6000, flesh, EnemyShotKind.Spore, 2.2f));
                }
                foreach (BossPart part in parts)
                {
                    part.explosionTint = HiveTint;
                }
                boss.rings = new[] { outer, inner };
                boss.ringSpeeds = new[] { 8f, -14f };
                boss.alarm = true;
                FinishBoss(boss, visual, parts, HiveTint,
                    Attack(BossAttackType.SpreadShot, 3.2f, 7, 5f, 0, 80f, EnemyShotKind.Spore),
                    Attack(BossAttackType.AimedBurst, 2.8f, 5, 10f, 1, 15f, EnemyShotKind.Bolt),
                    Attack(BossAttackType.RingShot, 3f, 18, 4.5f, 2, 0f, EnemyShotKind.Spore),
                    Attack(BossAttackType.AimedBurst, 4f, 1, 0f, 2, 0f, EnemyShotKind.Beam));
                SaveStrike(boss.gameObject, "Bosses/HiveMother");
            }
        }
    }
}
