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
    /// The built field campaigns: two more campaigns of the asteroid field, open from the start, each of four sectors of
    /// three missions with a boss at the end of every sector; every mission valid, its sector in both themes, its bosses
    /// of their own, its hunts with a quarry that shows up and its escorts and defences with a tough enough ally; the
    /// endless mission rotating through the new sectors and bosses; and every new kind with its prefab and pool.
    /// </summary>
    public class FieldCampaignContentTest
    {
        private static string Root
        {
            get
            {
                PackageInfo package = PackageInfo.FindForAssembly(typeof(AsteroidsTheme).Assembly);
                return package != null ? package.assetPath : "Assets";
            }
        }

        private static T Asset<T>(string relative) where T : Object
        {
            return AssetDatabase.LoadAssetAtPath<T>($"{Root}/{relative}");
        }

        private static AsteroidsCampaign Campaign => Asset<AsteroidsCampaign>("Config/Campaign/AsteroidsCampaign.asset");

        private static readonly string[] FirstBosses = { "RockTitan", "Mothership", "HiveQueen", "Dreadnought" };


        [SetUp]
        public void SetUp()
        {
            Assume.That(Campaign, Is.Not.Null, "Build Everything has not built the campaign yet.");
            Assume.That(Campaign.ArcCount(MissionMode.Field), Is.GreaterThanOrEqualTo(3), "The field campaigns are not built yet.");
        }


        [Test]
        public void TwoMoreFieldCampaignsOfTwelveMissionsOpenFromTheStart()
        {
            AsteroidsCampaign campaign = Campaign;
            var progress = new AsteroidsProgress(new TransientStrategy(), GameType.Asteroids);
            var titles = new HashSet<string>();
            for (int arc = 1; arc <= 2; arc++)
            {
                AsteroidsCampaign.Arc entry = campaign.GetArc(MissionMode.Field, arc);
                Assert.IsNotNull(entry, $"Field campaign {arc} is listed.");
                Assert.That(entry.title, Is.Not.Empty);
                Assert.That(entry.description, Is.Not.Empty);
                Assert.That(entry.starsRequired, Is.Zero, "The new campaigns are open from the start.");
                Assert.IsTrue(campaign.IsArcOpen(MissionMode.Field, arc, progress));
                List<int> missions = campaign.MissionsOf(MissionMode.Field, arc);
                List<int> sectors = campaign.SectorsOf(MissionMode.Field, arc);
                Assert.That(missions.Count, Is.EqualTo(12), $"{entry.title} has twelve missions.");
                Assert.That(sectors.Count, Is.EqualTo(4), $"{entry.title} has four sectors.");
                Assert.IsTrue(campaign.IsUnlocked(missions[0], progress), $"{entry.title}'s first mission is open.");
                Assert.IsFalse(campaign.IsUnlocked(missions[1], progress), $"{entry.title}'s second mission waits for the first.");
                Assert.That(campaign.GetSector(sectors[0]).starsRequired, Is.Zero);
                for (int s = 1; s < sectors.Count; s++)
                {
                    Assert.Greater(campaign.GetSector(sectors[s]).starsRequired, campaign.GetSector(sectors[s - 1]).starsRequired, "The star gates rise sector by sector.");
                    Assert.LessOrEqual(campaign.GetSector(sectors[s]).starsRequired, campaign.MaxStarsOf(MissionMode.Field, arc) / 2, "The gates leave room for mistakes.");
                }
                for (int i = 0; i < missions.Count; i++)
                {
                    AsteroidsLevel mission = campaign.Mission(missions[i]);
                    Assert.That(mission.Index, Is.EqualTo(missions[i]), $"{mission.Title} knows its index.");
                    Assert.IsTrue(mission.IsLevelValid(out string message), $"{mission.Title}: {message}");
                    Assert.That(campaign.ArcOf(missions[i]), Is.EqualTo(arc));
                    Assert.That(mission.Sector, Is.EqualTo(sectors[i / 3]), $"{mission.Title} is in its sector.");
                    Assert.IsTrue(titles.Add(mission.Title), $"{mission.Title} has a title of its own.");
                    Assert.That(mission.Description, Is.Not.Empty);
                    Assert.That(mission.Introduces, Is.Not.Empty);
                    Assert.That(mission.HintCount, Is.GreaterThan(0));
                    Assert.Greater(mission.ScoreGoal, 0);
                    bool boss = i % 3 == 2;
                    Assert.That(mission.Objective == LevelObjective.Boss, Is.EqualTo(boss), $"{mission.Title}: the third mission of every sector is a boss.");
                }
                Assert.That(campaign.NextMission(missions[11]), Is.EqualTo(-1), "The last mission leads nowhere.");
            }
        }


        [Test]
        public void TheCampaignsUseEveryNewObjectiveAndMostlyNewBosses()
        {
            AsteroidsCampaign campaign = Campaign;
            var objectives = new HashSet<LevelObjective>();
            var bosses = new List<Boss>();
            for (int arc = 1; arc <= 2; arc++)
            {
                foreach (int index in campaign.MissionsOf(MissionMode.Field, arc))
                {
                    AsteroidsLevel mission = campaign.Mission(index);
                    objectives.Add(mission.Objective);
                    if (mission.BossPrefab != null)
                    {
                        bosses.Add(mission.BossPrefab);
                    }
                    if (mission.Objective == LevelObjective.Hunt)
                    {
                        Assert.That(mission.Quarry, Is.Not.Empty);
                        foreach (HazardKind quarry in mission.Quarry)
                        {
                            Assert.IsTrue(mission.Waves.Any(wave => wave.Count(quarry) > 0), $"{mission.Title}: its quarry {quarry} shows up.");
                        }
                        Assert.Greater(mission.QuarryShare, 0f);
                    }
                    if (mission.Objective == LevelObjective.Escort || mission.Objective == LevelObjective.Defend)
                    {
                        Assert.GreaterOrEqual(mission.AllyHull, 100f, $"{mission.Title}'s ally can take some punishment.");
                    }
                }
            }
            foreach (LevelObjective objective in new[] { LevelObjective.Escort, LevelObjective.Defend, LevelObjective.Hunt })
            {
                Assert.That(objectives, Does.Contain(objective));
            }
            Assert.That(bosses.Count, Is.EqualTo(8), "Every boss mission has its boss.");
            Assert.That(bosses.Distinct().Count(), Is.EqualTo(8), "Every boss mission has a boss of its own.");
            int fresh = bosses.Count(boss => !FirstBosses.Contains(boss.name));
            Assert.GreaterOrEqual(fresh, 6, "At least six of the eight bosses are new.");
            foreach (Boss boss in bosses)
            {
                Assert.That(boss.DisplayName, Is.Not.Empty);
                Assert.That(boss.attacks, Is.Not.Empty, $"{boss.name} attacks.");
                Assert.Greater(boss.MaxHealth, 100f, $"{boss.name} is tougher than the first campaign's first boss.");
            }
        }


        [Test]
        public void TheNewSectorsHaveTheirLooksInBothThemes()
        {
            AsteroidsCampaign campaign = Campaign;
            var classic = Asset<AsteroidsTheme>("Config/Themes/Game/Classic.asset");
            var arcade = Asset<AsteroidsTheme>("Config/Themes/Game/Arcade80s.asset");
            Assume.That(classic, Is.Not.Null);
            Assume.That(arcade, Is.Not.Null);
            for (int arc = 1; arc <= 2; arc++)
            {
                foreach (int index in campaign.SectorsOf(MissionMode.Field, arc))
                {
                    SectorTheme theme = campaign.GetSector(index).theme;
                    Assert.IsNotNull(theme, $"{campaign.GetSector(index).title} has a theme.");
                    Assert.IsNotNull(theme.PlanetMaterial, $"{theme.Title} has its star or world.");
                    Assert.AreEqual(theme, classic.Sector(theme));
                    SectorTheme neon = arcade.Sector(theme);
                    Assert.AreNotEqual(theme, neon, $"Arcade 80s has its own look of {theme.Title}.");
                    Assert.AreEqual(theme.name, neon.name);
                    Assert.IsNotNull(neon.PlanetMaterial);
                    Assert.AreEqual("Portfolio/Asteroids/NeonSurface", neon.PlanetMaterial.shader.name);
                }
            }
        }


        [Test]
        public void EveryNewKindHasItsPrefabItsPoolAndItsLookInBothThemes()
        {
            var classic = Asset<AsteroidsTheme>("Config/Themes/Game/Classic.asset");
            var arcade = Asset<AsteroidsTheme>("Config/Themes/Game/Arcade80s.asset");
            foreach (AsteroidKind kind in new[] { AsteroidKind.Metal, AsteroidKind.Hydra, AsteroidKind.Wreck })
            {
                var prefab = Asset<GameObject>($"Prefabs/Asteroids/{kind}.prefab");
                Assert.IsNotNull(prefab, $"{kind} rocks are built.");
                Assert.That(prefab.GetComponent<Asteroid>().Kind, Is.EqualTo(kind));
                Assert.IsNotNull(classic.Asteroid(kind));
                Assert.IsNotNull(arcade.Asteroid(kind));
                Assert.AreNotEqual(classic.Asteroid(kind).meshes[0], arcade.Asteroid(kind).meshes[0], $"Arcade 80s draws {kind} rocks its own way.");
            }
            foreach (string path in new[]
            {
                "Hazards/SolarFlare", "Hazards/IonStorm", "Enemies/Sentry", "Enemies/Minelayer", "Enemies/Stalker", "Enemies/Corsair", "Allies/Freighter",
                "Allies/Station", "Shots/Lightning", "Shots/Lance", "Shots/SweepBeam", "Shots/NovaOrb"
            })
            {
                Assert.IsNotNull(Asset<GameObject>($"Prefabs/{path}.prefab"), $"{path} is built.");
            }
            foreach (string name in new[] { "Sentry", "Minelayer", "Stalker", "Corsair" })
            {
                var enemy = Asset<GameObject>($"Prefabs/Enemies/{name}.prefab").GetComponent<CampaignEnemy>();
                Assert.IsNotNull(enemy, $"{name} is a campaign enemy.");
                Assert.IsNotNull(enemy.reticle, $"{name} can be marked.");
            }
        }


        [Test]
        public void TheEndlessMissionRotatesThroughTheNewSectorsAndBosses()
        {
            AsteroidsCampaign campaign = Campaign;
            AsteroidsLevel endless = campaign.Mission(campaign.EndlessIndex);
            Assert.IsNotNull(endless);
            for (int arc = 1; arc <= 2; arc++)
            {
                foreach (int index in campaign.SectorsOf(MissionMode.Field, arc))
                {
                    Assert.That(endless.themeRotation, Does.Contain(campaign.GetSector(index).theme));
                }
                foreach (int index in campaign.MissionsOf(MissionMode.Field, arc))
                {
                    Boss boss = campaign.Mission(index).BossPrefab;
                    if (boss != null)
                    {
                        Assert.That(endless.bossRotation, Does.Contain(boss));
                    }
                }
            }
            Assert.That(endless.bossRotation.Take(4).Select(boss => boss.name), Is.EqualTo(FirstBosses), "The first campaign's bosses still come first.");
        }
    }
}
