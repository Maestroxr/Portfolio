using System;
using UnityEngine;

namespace Portfolio.Asteroids.EditorTools
{
    /// <summary>
    /// The prefabs of the Iron Tide and Hive Fall campaigns: the components and dressing of their ground units (built with
    /// the others by <see cref="BuildGroundUnit"/>: the shield generator's dome, the hive spawner's launcher), their three
    /// aircraft, their four enemy shot kinds (the artillery shell with its landing ring, spores, crystal needles, seeker
    /// rockets) and their bosses (AsteroidsPrefabBuilder.StrikeCampaignBosses.cs).
    /// </summary>
    internal static partial class AsteroidsPrefabBuilder
    {
        static partial void BuildStrikeCampaignPrefabs()
        {
            Progress("Strike campaigns: aircraft", 0.991f);
            BuildCampaignAircraft();
            Progress("Strike campaigns: shots", 0.992f);
            BuildCampaignShots();
            Progress("Strike campaigns: bosses", 0.994f);
            BuildCampaignBosses();
        }


        // ------------------------------------------------------------------ ground units

        /// <summary>The component of a ground unit's prefab: the shield generator has rules of its own.</summary>
        private static Type GroundUnitComponent(StrikeUnit unit)
        {
            return unit == StrikeUnit.ShieldGenerator ? typeof(ShieldGenerator) : typeof(GroundUnit);
        }


        /// <summary>
        /// What a campaign unit needs beyond the common build: the shield generator's dome over the ground it covers (half
        /// buried, so it shows as a dome), the hive spawner's launcher of stingers, slower turrets for the heavy guns.
        /// </summary>
        private static void DressGroundUnit(StrikeUnit unit, GroundUnit body, UnitModel model)
        {
            switch (unit)
            {
                case StrikeUnit.ShieldGenerator:
                {
                    var generator = (ShieldGenerator)body;
                    Transform depth = body.transform.Find("Depth");
                    float diameter = StrikeCampaignRules.ShieldRadius * 2f;
                    Vector3 scale = SphereScale(diameter);
                    MeshRenderer dome = Model(depth != null ? depth : body.transform, "Dome", SphereMesh, SM("ShieldDome"), new Vector3(0f, 0f, model.Height * 0.5f),
                        Quaternion.identity, new Vector3(scale.x, scale.y, scale.z * 0.35f));
                    generator.dome = dome.transform;
                    break;
                }
                case StrikeUnit.HiveSpawner:
                {
                    UnitLauncher launcher = body.gameObject.AddComponent<UnitLauncher>();
                    launcher.unit = StrikeUnit.Stinger;
                    launcher.perLaunch = StrikeUnitRules.Info(unit).Shots;
                    launcher.launches = StrikeCampaignRules.SpawnerLaunches;
                    launcher.paths = new[] { FlightPath.Swoop, FlightPath.DiveLeft, FlightPath.Hook };
                    break;
                }
                case StrikeUnit.Artillery:
                    body.turretTurnRate = 70f;
                    break;
                case StrikeUnit.Destroyer:
                    body.turretTurnRate = 80f;
                    break;
            }
        }


        // ------------------------------------------------------------------ aircraft

        private static void BuildCampaignAircraft()
        {
            Aircraft(StrikeUnit.Drone, new Color(1f, 0.6f, 0.3f), visual => Model(visual, "Body", AsteroidsArtBuilder.StrikeModel("Drone"), SM("StrikeIronSteel")),
                new[] { new Vector2(0f, -0.5f) }, new Vector2[0], 0f);
            Aircraft(StrikeUnit.Cruiser, new Color(1f, 0.6f, 0.3f), visual =>
            {
                MeshRenderer hull = PackHull(visual, "StarSparrow12", SM("AirIronSteel"), 6.4f);
                for (int side = -1; side <= 1; side += 2)
                {
                    Model(visual, side < 0 ? "PodLeft" : "PodRight", AsteroidsArtBuilder.StrikeModel("ModulePod"), SM("StrikeIronSteel"), new Vector3(side * 1.95f, 0.05f, 0.4f),
                        Quaternion.identity, Vector3.one * 0.72f);
                }
                return hull;
            }, new[] { new Vector2(-1.95f, -1.6f), new Vector2(1.95f, -1.6f) }, new[] { new Vector2(-0.9f, 3.2f), new Vector2(0.9f, 3.2f) }, 1.6f);
            Aircraft(StrikeUnit.Stinger, new Color(0.7f, 1f, 0.35f), visual => Model(visual, "Body", AsteroidsArtBuilder.StrikeModel("Stinger"), SM("StrikeHivePurple")),
                new[] { new Vector2(0f, -0.9f) }, new Vector2[0], 0f);
            Aircraft(StrikeUnit.Mite, new Color(0.7f, 1f, 0.35f), visual => Model(visual, "Body", AsteroidsArtBuilder.StrikeModel("Mite"), SM("StrikeHivePurple")),
                new Vector2[0], new Vector2[0], 0f);
        }


        // ------------------------------------------------------------------ enemy shots

        /// <summary>The enemy shot kinds 14 to 17: the shell, the spore, the needle and the seeker rocket.</summary>
        private static void BuildCampaignShots()
        {
            {
                var root = new GameObject(EnemyShotName(EnemyShotKind.Shell));
                var shell = root.AddComponent<EnemyShell>();
                Transform visual = Child(root.transform, "Visual");
                Quad(visual, "Glow", SM("EnemyShellGlow"), Vector3.zero, Vector2.one * 1.2f);
                Quad(visual, "Core", SM("EnemyFlak"), new Vector3(0f, 0f, -0.05f), Vector2.one * 0.6f);
                MeshRenderer marker = Quad(root.transform, "Marker", SM("EnemyShellMarker"), new Vector3(0f, 0f, 0.05f), Vector2.one);
                shell.visual = visual;
                EnemyShotNumbers(shell, EnemyShotKind.Shell);
                shell.radius = 0.4f;
                shell.alignToVelocity = false;
                shell.impactTint = new Color(1f, 0.5f, 0.25f);
                shell.marker = marker.transform;
                shell.burst = StrikeCampaignRules.ShellBurstRadius;
                SaveStrike(root, $"Shots/{EnemyShotName(EnemyShotKind.Shell)}");
            }
            {
                var root = new GameObject(EnemyShotName(EnemyShotKind.Spore));
                var spore = root.AddComponent<Shot>();
                Transform visual = Child(root.transform, "Visual");
                Quad(visual, "Glow", SM("EnemySporeGlow"), Vector3.zero, Vector2.one * 1.05f);
                Quad(visual, "Core", SM("EnemySporeCore"), new Vector3(0f, 0f, -0.05f), Vector2.one * 0.5f);
                spore.visual = visual;
                EnemyShotNumbers(spore, EnemyShotKind.Spore);
                spore.radius = 0.32f;
                spore.alignToVelocity = false;
                spore.Spin = new Vector3(0f, 0f, 140f);
                spore.homing = StrikeCampaignRules.SporeHoming;
                spore.homingTime = StrikeCampaignRules.SporeHomingTime;
                spore.impactTint = new Color(0.6f, 1f, 0.3f);
                SaveStrike(root, $"Shots/{EnemyShotName(EnemyShotKind.Spore)}");
            }
            EnemyBolt(EnemyShotKind.Needle, SM("EnemyNeedle"), new Vector2(0.26f, 1.05f), 0.16f, true, new Color(0.5f, 0.9f, 1f));
            {
                GameObject root = MissileBody(EnemyShotName(EnemyShotKind.Seeker), 0.15f, new Color(1f, 0.4f, 0.8f), true, out Shot shot);
                EnemyShotNumbers(shot, EnemyShotKind.Seeker);
                shot.radius = 0.22f;
                shot.impactTint = new Color(1f, 0.45f, 0.7f);
                shot.homing = StrikeCampaignRules.SeekerHoming;
                shot.homingTime = StrikeCampaignRules.SeekerHomingTime;
                Transform glow = root.transform.Find("Visual/Glow");
                if (glow != null)
                {
                    glow.GetComponent<MeshRenderer>().sharedMaterial = SM("EnemySeekerFlame");
                }
                SaveStrike(root, $"Shots/{EnemyShotName(EnemyShotKind.Seeker)}");
            }
        }
    }
}
