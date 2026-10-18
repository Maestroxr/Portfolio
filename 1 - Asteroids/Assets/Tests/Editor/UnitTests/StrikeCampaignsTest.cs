using System.Collections.Generic;
using System.Linq;
using Gamebox;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Portfolio.Asteroids.Tests
{
    /// <summary>
    /// The Iron Tide and Hive Fall strike campaigns: the shapes of their new attack patterns, the shield generator's dome,
    /// the artillery shell's landing spot, the unit table's new rows, and the built campaign (two open campaigns of nine
    /// missions in three gated sectors, every level valid and fair: aircraft come in from off screen, only items the Supply
    /// Room had before them are dropped, every unit and boss has its prefab, most bosses are new).
    /// </summary>
    public class StrikeCampaignsTest
    {
        private const float Step = 1f / 60f;

        private static readonly string[] IronTideBosses =
        {
            "GlacierHauler", "FrostBattery", "Icebreaker", "GaleCarrier", "ReefBastion", "SeaWolves", "Drydock", "Battleship", "Leviathan"
        };

        private static readonly string[] HiveFallBosses =
        {
            "BroodMound", "SporeTowers", "BurrowWyrm", "ShardColossus", "PrismArray", "Resonator", "HaloGate", "BroodShip", "HiveMother"
        };

        private readonly TemporaryObjects objects = new TemporaryObjects();
        private SpaceField field;
        private Playground playground;


        [SetUp]
        public void SetUp()
        {
            playground = objects.Component<Playground>("Playground");
            playground.Fix(StrikeRules.HalfSize);
            playground.Wraps = false;
            field = objects.Component<SpaceField>("Field");
            field.playground = playground;
        }


        [TearDown]
        public void TearDown()
        {
            objects.Dispose();
        }


        private static string Root
        {
            get
            {
                PackageInfo package = PackageInfo.FindForAssembly(typeof(StrikeLevel).Assembly);
                return package != null ? package.assetPath : "Assets";
            }
        }

        // ------------------------------------------------------------------ patterns

        [Test]
        public void RingsSpreadEvenlyAndInterleave()
        {
            const int count = 8;
            var directions = new List<Vector2>();
            for (int i = 0; i < count; i++)
            {
                directions.Add(StrikeCampaignRules.RingDirection(i, count, false));
            }
            Assert.That((directions[0] - Vector2.down).magnitude, Is.LessThan(1e-4f), "A ring starts straight down.");
            for (int i = 0; i < count; i++)
            {
                Assert.That(directions[i].magnitude, Is.EqualTo(1f).Within(1e-4f));
                float between = Vector2.Angle(directions[i], directions[(i + 1) % count]);
                Assert.That(between, Is.EqualTo(45f).Within(1e-3f), "Evenly round.");
            }
            Assert.That(Vector2.Angle(StrikeCampaignRules.RingDirection(0, count, true), Vector2.down), Is.EqualTo(22.5f).Within(1e-3f),
                "Every other ring is turned by half a step.");
        }


        [Test]
        public void SweepsSwingFromSideToSide()
        {
            float widest = 0f;
            float left = 0f;
            float right = 0f;
            for (float time = 0f; time < 4f; time += 0.05f)
            {
                Vector2 middle = StrikeCampaignRules.SweepDirection(1, 3, time);
                float angle = Vector2.SignedAngle(Vector2.down, middle);
                widest = Mathf.Max(widest, Mathf.Abs(angle));
                left = Mathf.Min(left, angle);
                right = Mathf.Max(right, angle);
                float spread = Vector2.Angle(StrikeCampaignRules.SweepDirection(0, 3, time), StrikeCampaignRules.SweepDirection(2, 3, time));
                Assert.That(spread, Is.EqualTo(StrikeCampaignRules.SweepFan * 2f).Within(1e-3f), "The fan keeps its width.");
            }
            Assert.That(widest, Is.LessThanOrEqualTo(StrikeCampaignRules.SweepSwing + 1e-3f));
            Assert.That(left, Is.LessThan(-StrikeCampaignRules.SweepSwing * 0.9f), "It swings to one side...");
            Assert.That(right, Is.GreaterThan(StrikeCampaignRules.SweepSwing * 0.9f), "...and to the other.");
            Assert.AreEqual(StrikeCampaignRules.SweepDirection(1, 3, 1.3f), StrikeCampaignRules.SweepDirection(1, 3, 1.3f), "The same at the same age on every client.");
        }


        [Test]
        public void BarragesLandOnAndAroundTheAimedSpot()
        {
            var aimed = new Vector2(3f, -6f);
            Assert.AreEqual(aimed, StrikeCampaignRules.BarrageSpot(aimed, 0), "The first shell lands on the spot.");
            for (int i = 1; i <= 3; i++)
            {
                Assert.That((StrikeCampaignRules.BarrageSpot(aimed, i) - aimed).magnitude, Is.EqualTo(StrikeCampaignRules.BarrageScatter).Within(1e-4f));
            }
            Assert.That((StrikeCampaignRules.BarrageSpot(aimed, 1) - StrikeCampaignRules.BarrageSpot(aimed, 2)).magnitude, Is.GreaterThan(1f), "Apart from each other.");
            var from = new Vector2(-8f, 9f);
            Vector2 velocity = StrikeCampaignRules.ShellVelocity(from, aimed);
            Assert.That((from + velocity * StrikeCampaignRules.ShellFlightTime - aimed).magnitude, Is.LessThan(1e-4f), "A shell flies there in its flight time.");
            Assert.That(StrikeUnitRules.Shot(EnemyShotKind.Shell).Lifetime, Is.EqualTo(StrikeCampaignRules.ShellFlightTime), "Its lifetime is the flight.");
        }


        [Test]
        public void AShellsRingMarksWhereItLands()
        {
            EnemyShell shell = objects.Component<EnemyShell>("Shell");
            Transform marker = new GameObject("Marker").transform;
            marker.SetParent(shell.transform, false);
            shell.marker = marker;
            shell.enemy = true;
            shell.lifetime = StrikeCampaignRules.ShellFlightTime;
            shell.radius = 0.4f;
            var from = new Vector2(0f, 8f);
            var spot = new Vector2(5f, -4f);
            shell.Position = from;
            shell.Velocity = StrikeCampaignRules.ShellVelocity(from, spot);
            field.Add(shell);
            for (int i = 0; i < 30; i++)
            {
                field.Tick(Step);
                Assert.That((shell.LandingSpot - spot).magnitude, Is.LessThan(1e-3f), "The landing spot holds still while the shell flies.");
                Assert.That(((Vector2)marker.position - spot).magnitude, Is.LessThan(1e-3f), "The ring sits on it.");
            }
            Assert.IsFalse(shell.Touches(spot, 5f), "Only its burst hurts.");
        }

        // ------------------------------------------------------------------ the shield generator

        private GroundUnit Ground(Vector2 at)
        {
            GroundUnit unit = objects.Component<GroundUnit>("Ground");
            unit.radius = 1f;
            unit.maxHealth = 20f;
            unit.pattern = AttackPattern.None;
            unit.Place(new StrikeEvent { unit = StrikeUnit.Turret }, 0, at);
            field.Add(unit);
            return unit;
        }


        [Test]
        public void AShieldGeneratorCoversTheGroundUnderItsDome()
        {
            ShieldGenerator generator = objects.Component<ShieldGenerator>("Generator");
            generator.radius = 1.4f;
            generator.maxHealth = 40f;
            generator.pattern = AttackPattern.None;
            generator.Place(new StrikeEvent { unit = StrikeUnit.ShieldGenerator }, 0, Vector2.zero);
            field.Add(generator);
            GroundUnit covered = Ground(new Vector2(4f, 2f));
            GroundUnit outside = Ground(new Vector2(-12f, 0f));
            field.Tick(Step);
            Assert.IsTrue(covered.Invulnerable, "Under the dome.");
            Assert.IsFalse(outside.Invulnerable, "Out of its reach.");
            Assert.IsFalse(generator.Invulnerable, "The generator itself can be shot.");
            Assert.IsFalse(covered.TakeHit(new DamageInfo(100f, Vector2.up, covered.Position, DamageSource.PlayerShot, true)));
            Assert.That(covered.Health, Is.EqualTo(20f), "A covered unit takes nothing.");

            Assert.IsTrue(generator.TakeHit(new DamageInfo(100f, Vector2.up, Vector2.zero, DamageSource.PlayerShot, true)));
            Assert.IsFalse(covered.Invulnerable, "The dome fell with its generator.");
            Assert.IsTrue(covered.TakeHit(new DamageInfo(100f, Vector2.up, covered.Position, DamageSource.PlayerShot, true)));
        }


        [Test]
        public void TwoDomesCoverTogether()
        {
            ShieldGenerator first = objects.Component<ShieldGenerator>("First");
            ShieldGenerator second = objects.Component<ShieldGenerator>("Second");
            foreach ((ShieldGenerator generator, Vector2 at) in new[] { (first, new Vector2(-3f, 0f)), (second, new Vector2(3f, 0f)) })
            {
                generator.radius = 1.4f;
                generator.maxHealth = 40f;
                generator.pattern = AttackPattern.None;
                generator.Place(new StrikeEvent { unit = StrikeUnit.ShieldGenerator }, 0, at);
                field.Add(generator);
            }
            GroundUnit between = Ground(Vector2.zero);
            field.Tick(Step);
            Assert.IsTrue(between.Invulnerable);
            Assert.IsFalse(first.Invulnerable || second.Invulnerable, "Generators never cover each other.");
            first.TakeHit(new DamageInfo(100f, Vector2.up, first.Position, DamageSource.PlayerShot, true));
            field.Tick(Step);
            Assert.IsTrue(between.Invulnerable, "The other dome still covers it.");
            second.TakeHit(new DamageInfo(100f, Vector2.up, second.Position, DamageSource.PlayerShot, true));
            field.Tick(Step);
            Assert.IsFalse(between.Invulnerable);
        }

        // ------------------------------------------------------------------ the unit table

        [Test]
        public void TheNewUnitsHaveTheirRowsAndPrefabs()
        {
            StrikeUnit[] units =
            {
                StrikeUnit.Drone, StrikeUnit.Cruiser, StrikeUnit.Stinger, StrikeUnit.Mite, StrikeUnit.SamSite, StrikeUnit.Artillery, StrikeUnit.Walker,
                StrikeUnit.ShieldGenerator, StrikeUnit.Destroyer, StrikeUnit.HiveSpawner, StrikeUnit.Spitter, StrikeUnit.Prism
            };
            foreach (StrikeUnit unit in units)
            {
                Assert.That(StrikeUnitRules.All, Does.Contain(unit), unit.ToString());
                StrikeUnitInfo info = StrikeUnitRules.Info(unit);
                Assert.That(info.Unit, Is.EqualTo(unit), $"{unit} has its own row.");
                Assert.That(info.Bounty, Is.GreaterThan(0));
            }
            Assert.That(StrikeUnitRules.Info(StrikeUnit.HiveSpawner).Pattern, Is.EqualTo(AttackPattern.Launch));
            Assert.That(StrikeUnitRules.Info(StrikeUnit.Artillery).ShotKind, Is.EqualTo(EnemyShotKind.Shell));
            Assert.That(StrikeUnitRules.Info(StrikeUnit.Prism).Telegraph, Is.GreaterThan(0f), "The prism glows before its ring.");
            foreach (EnemyShotKind kind in new[] { EnemyShotKind.Shell, EnemyShotKind.Spore, EnemyShotKind.Needle, EnemyShotKind.Seeker })
            {
                Assert.That(StrikeUnitRules.Shot(kind).Damage, Is.GreaterThan(0f), kind.ToString());
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Prefabs/Strike/Air/Dart.prefab") == null)
            {
                Assert.Ignore("The strike prefabs are not built in this project.");
            }
            foreach (StrikeUnit unit in StrikeUnitRules.All)
            {
                string folder = StrikeUnitRules.IsAir(unit) ? "Air" : "Ground";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Prefabs/Strike/{folder}/{unit}.prefab");
                Assert.IsNotNull(prefab, $"{unit} has a prefab.");
            }
            var generatorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Prefabs/Strike/Ground/ShieldGenerator.prefab");
            Assert.IsNotNull(generatorPrefab.GetComponent<ShieldGenerator>().dome, "The generator shows its dome.");
            var spawnerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Prefabs/Strike/Ground/HiveSpawner.prefab");
            Assert.That(spawnerPrefab.GetComponent<UnitLauncher>().unit, Is.EqualTo(StrikeUnit.Stinger), "The spawner launches stingers.");
            foreach (EnemyShotKind kind in new[] { EnemyShotKind.Shell, EnemyShotKind.Spore, EnemyShotKind.Needle, EnemyShotKind.Seeker })
            {
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Prefabs/Strike/Shots/Enemy{kind}.prefab"), $"The {kind} shot has a prefab.");
            }
        }

        // ------------------------------------------------------------------ the campaigns

        private static AsteroidsCampaign Campaign => AssetDatabase.LoadAssetAtPath<AsteroidsCampaign>($"{Root}/Config/Campaign/AsteroidsCampaign.asset");


        [Test]
        public void TwoOpenStrikeCampaignsOfNineMissions()
        {
            AsteroidsCampaign campaign = Campaign;
            if (campaign == null)
            {
                Assert.Ignore("The campaign is not built in this project.");
            }
            Assert.That(campaign.ArcCount(MissionMode.Strike), Is.EqualTo(3));
            string[] titles = { "Shadow Strike", "Iron Tide", "Hive Fall" };
            for (int arc = 0; arc < 3; arc++)
            {
                Assert.That(campaign.GetArc(MissionMode.Strike, arc).title, Is.EqualTo(titles[arc]));
                Assert.That(campaign.ArcStarsRequired(MissionMode.Strike, arc), Is.Zero, $"{titles[arc]} is open from the start.");
                List<int> missions = campaign.MissionsOf(MissionMode.Strike, arc);
                Assert.That(missions.Count, Is.EqualTo(9), titles[arc]);
                List<int> sectors = campaign.SectorsOf(MissionMode.Strike, arc);
                Assert.That(sectors.Select(s => campaign.GetSector(s).starsRequired), Is.EqualTo(new[] { 0, 4, 10 }), $"{titles[arc]}'s gates.");
                var progress = new AsteroidsProgress(new TransientStrategy(), GameType.Asteroids);
                Assert.IsTrue(campaign.IsUnlocked(missions[0], progress), $"{titles[arc]}'s first mission is open.");
                Assert.IsFalse(campaign.IsUnlocked(missions[1], progress), "The second waits for the first.");
                Assert.That(campaign.NextMission(missions[8]), Is.EqualTo(-1), "The last mission of a campaign leads nowhere.");
                for (int i = 0; i < 9; i++)
                {
                    Assert.That(campaign.Mission(missions[i]).Index, Is.EqualTo(missions[i]), "A mission knows its index.");
                    Assert.That(campaign.SectorOf(missions[i]), Is.SameAs(campaign.GetSector(sectors[i / 3])), "Three missions to a sector.");
                }
            }
        }


        [Test]
        public void EveryCampaignLevelIsValidAndFair()
        {
            int found = 0;
            var bosses = new HashSet<string>();
            foreach (string folder in new[] { "IronTide", "HiveFall" })
            {
                for (int number = 1; number <= 9; number++)
                {
                    var level = AssetDatabase.LoadAssetAtPath<StrikeLevel>($"{Root}/Config/Strike/{folder}/Level{number}.asset");
                    if (level == null)
                    {
                        continue;
                    }
                    found++;
                    string name = $"{folder} Level{number} ({level.Title})";
                    Assert.IsTrue(level.IsLevelValid(out string message), $"{name}: {message}");
                    Assert.IsNotNull(level.StrikeBossPrefab, name);
                    bosses.Add(level.StrikeBossPrefab.name);
                    foreach (StrikeEvent spawn in level.Events)
                    {
                        Assert.IsTrue(spawn.bonus == StrikeItem.MachineGun || StrikeArmory.Info(spawn.bonus).CanBePickup,
                            $"{name}: only items cheap enough to be pickups ({spawn.bonus}).");
                        if (!spawn.IsAir)
                        {
                            continue;
                        }
                        Vector2 start = ScrollDirector.AirStart(spawn, FlightPaths.Waypoints(spawn.path, spawn.mirror), 0f);
                        Assert.IsFalse(playground.IsInside(start, -0.5f), $"{name}: {spawn.unit} at {spawn.at} starts on screen at {start}.");
                    }
                    Assert.That(level.Events.Count(e => e.IsAir && e.minDifficulty == StrikeDifficulty.Rookie), Is.GreaterThan(5), $"{name} has aircraft for every pilot.");
                }
            }
            if (found == 0)
            {
                Assert.Ignore("The campaign levels are not built in this project.");
            }
            Assert.AreEqual(18, found, "All eighteen levels are built.");
            Assert.That(bosses, Is.EquivalentTo(IronTideBosses.Concat(HiveFallBosses)), "Every mission has a boss of its own.");
        }


        [Test]
        public void TheBossesAreBuiltWithTheirParts()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Prefabs/Strike/Bosses/SandCrawler.prefab") == null)
            {
                Assert.Ignore("The strike bosses are not built in this project.");
            }
            foreach (string name in IronTideBosses.Concat(HiveFallBosses))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Prefabs/Strike/Bosses/{name}.prefab");
                Assert.IsNotNull(prefab, name);
                StrikeBoss boss = prefab.GetComponent<StrikeBoss>();
                Assert.That(boss.Parts.Count, Is.GreaterThanOrEqualTo(3), $"{name} is made of parts.");
                Assert.That(boss.Parts.Select(p => p.name).Distinct().Count(), Is.EqualTo(boss.Parts.Count), $"{name}'s parts have names of their own.");
                Assert.That(boss.Parts.Count(p => p.Tier == 0), Is.GreaterThan(0), $"{name} has parts to break first.");
                Assert.That(boss.CoreTier, Is.LessThanOrEqualTo(boss.Parts.Max(p => p.Tier) + 1), $"{name}'s core opens after its last tier.");
                if (boss.IsCoreless)
                {
                    Assert.IsTrue(boss.invulnerable, $"{name}: a coreless boss cannot be hit itself.");
                }
                foreach (BossPart part in boss.Parts)
                {
                    if (part.pattern == AttackPattern.Launch)
                    {
                        Assert.IsNotNull(part.GetComponent<UnitLauncher>(), $"{name}.{part.name} launches with a launcher.");
                    }
                }
                Assert.IsFalse(string.IsNullOrEmpty(boss.DisplayName), name);
            }
        }
    }
}
