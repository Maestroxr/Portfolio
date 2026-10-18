using System.Collections.Generic;
using Gamebox.Editor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Portfolio.Asteroids.EditorTools
{
    /// <summary>
    /// The prefabs of the second and third asteroid field campaigns (AsteroidsPrefabBuilder.FieldCampaigns.cs): the iron,
    /// hydra and wreck rocks (Asteroids/{kind}, where the themes look for every kind), the solar flare and the ion storm,
    /// the sentry, minelayer, phase stalker and corsair, the freighter and the station, the lightning, lance, sweep beam
    /// and nova orb, and the eight bosses of the new campaigns. The numbers live here.
    /// </summary>
    internal static partial class AsteroidsPrefabBuilder
    {
        static partial void BuildFieldCampaignPrefabs()
        {
            BuildFieldRocks();
            BuildFieldHazards();
            BuildFieldEnemies();
            BuildAllies();
            BuildFieldShots();
            BuildFieldBosses();
        }


        private static Material FM(string name)
        {
            return AsteroidsArtBuilder.FieldMaterial(name);
        }


        private static Mesh FModel(string name)
        {
            return AsteroidsArtBuilder.FieldModel(name);
        }


        /// <summary>The reticle that marks the quarry of a hunt (hidden until the enemy is marked).</summary>
        private static Transform Reticle(Transform root, float size)
        {
            MeshRenderer reticle = Quad(root, "Reticle", FM("Reticle"), new Vector3(0f, 0f, -1.2f), Vector2.one * size);
            reticle.gameObject.SetActive(false);
            return reticle.transform;
        }


        // ------------------------------------------------------------------ rocks

        private static void BuildFieldRocks()
        {
            var rocks = new[] { AsteroidsArtBuilder.Model("Rock1"), AsteroidsArtBuilder.Model("Rock2"), AsteroidsArtBuilder.Model("Rock3") };
            FieldRock(AsteroidKind.Metal, rocks, FM("Metal"), new Color(0.8f, 0.9f, 1f), AsteroidsArtBuilder.FieldSound("MetalGlance"));
            FieldRock(AsteroidKind.Hydra, new[] { FModel("HydraRock1"), FModel("HydraRock2"), FModel("HydraRock3") }, FM("Hydra"), new Color(0.6f, 1f, 0.5f),
                AsteroidsArtBuilder.FieldSound("HydraRegrow"));
            FieldRock(AsteroidKind.Wreck, new[] { FModel("Wreck1"), FModel("Wreck2"), FModel("Wreck3") }, FM("Wreck"), new Color(1f, 0.7f, 0.4f), null);
        }


        private static void FieldRock(AsteroidKind kind, Mesh[] meshes, Material material, Color flash, AudioClip clip)
        {
            var root = new GameObject(kind.ToString());
            var asteroid = root.AddComponent<Asteroid>();
            Transform visual = Child(root.transform, "Visual");
            MeshRenderer renderer = Model(visual, "Model", meshes[0], material);
            asteroid.kind = kind;
            asteroid.meshes = meshes;
            asteroid.meshFilter = renderer.GetComponent<MeshFilter>();
            asteroid.modelRadius = 1.05f;
            asteroid.visual = visual;
            asteroid.flashRenderers = new Renderer[] { renderer };
            asteroid.flashColor = flash;
            asteroid.radius = AsteroidRules.Radius(kind, AsteroidSize.Large);
            asteroid.maxHealth = AsteroidRules.Health(kind, AsteroidSize.Large);
            asteroid.wraps = true;
            asteroid.pulledByGravity = true;
            asteroid.kindClip = clip;
            Save(root, $"Asteroids/{kind}");
        }


        // ------------------------------------------------------------------ hazards

        private static void BuildFieldHazards()
        {
            // Solar flare: the glow on the edge it comes from, then a wall of fire with embers.
            {
                var root = new GameObject("SolarFlare");
                var flare = root.AddComponent<SolarFlare>();
                MeshRenderer wall = Quad(root.transform, "Wall", FM("FlareFront"), new Vector3(0f, 0f, -0.4f), Vector2.one);
                MeshRenderer edge = Quad(root.transform, "Edge", FM("FlareEdge"), new Vector3(0f, 2.5f, 0.3f), Vector2.one);
                ParticleSystem embers = ParticleFactory.Create("Embers", root.transform, FM("FlareEmber"));
                ParticleFactory.Lifetime(embers, 0.5f, 1.1f);
                ParticleFactory.Speed(embers, 0.5f, 3f);
                ParticleFactory.Size(embers, 0.15f, 0.45f);
                ParticleFactory.Rate(embers, 140f);
                ParticleFactory.Box(embers, new Vector3(30f, 0.6f, 0.1f));
                ParticleFactory.SizeOverLife(embers, 1f, 0.1f);
                ParticleFactory.Colors(embers, new Color(1f, 0.85f, 0.4f), new Color(1f, 0.4f, 0.1f));
                ParticleFactory.FadeOut(embers);
                ParticleFactory.MaxParticles(embers, 400);
                flare.wall = wall.transform;
                flare.edgeGlow = edge;
                flare.embers = embers;
                flare.roar = AsteroidsArtBuilder.FieldSound("FlareRoar");
                flare.radius = 1f;
                flare.wraps = false;
                flare.pulledByGravity = false;
                flare.warningTime = 2.2f;
                flare.speed = 13f;
                flare.halfWidth = 1.4f;
                flare.damage = 16f;
                flare.allyDamage = 14f;
                Save(root, "Hazards/SolarFlare");
            }

            // Ion storm: turning layers of cloud with flickering arcs inside.
            {
                var root = new GameObject("IonStorm");
                var storm = root.AddComponent<IonStorm>();
                Transform visual = Child(root.transform, "Visual");
                var swirls = new List<Transform>
                {
                    Quad(visual, "Shade", FM("StormShade"), new Vector3(0f, 0f, 0.5f), Vector2.one * 8.2f).transform,
                    Quad(visual, "Cloud", FM("StormCloud"), new Vector3(0f, 0f, 0.3f), Vector2.one * 7.6f).transform,
                    Quad(visual, "Core", FM("StormCloud"), new Vector3(0f, 0f, 0.2f), Vector2.one * 5f).transform
                };
                swirls[2].localRotation = Quaternion.Euler(0f, 0f, 70f);
                var arcs = new List<Renderer>();
                for (int i = 0; i < 4; i++)
                {
                    float angle = i * 90f + 30f;
                    Vector3 at = Quaternion.Euler(0f, 0f, angle) * new Vector3(0f, 1.3f, 0f);
                    MeshRenderer arc = Quad(visual, $"Arc{i + 1}", FM("StormArc"), at + new Vector3(0f, 0f, 0.1f), new Vector2(1.2f, 3.4f));
                    arc.enabled = false;
                    arcs.Add(arc);
                }
                storm.visual = visual;
                storm.swirls = swirls.ToArray();
                storm.arcs = arcs.ToArray();
                storm.crackle = Loop(root, AsteroidsArtBuilder.FieldSound("StormCrackle"), 0.35f);
                storm.radius = 3.4f;
                storm.wraps = true;
                storm.pulledByGravity = false;
                storm.drag = 1.4f;
                storm.shieldDrain = 30f;
                storm.strikeInterval = 1.15f;
                storm.drift = 0.7f;
                storm.growTime = 1.5f;
                Save(root, "Hazards/IonStorm");
            }
        }


        // ------------------------------------------------------------------ enemies

        private static void BuildFieldEnemies()
        {
            // Sentry: a gun platform whose turret turns to its target and glows before a burst.
            {
                var root = new GameObject("Sentry");
                var sentry = root.AddComponent<Sentry>();
                Transform visual = Child(root.transform, "Visual");
                MeshRenderer body = Model(Child(visual, "Tilt", Vector3.zero, FaceCamera), "Base", FModel("SentryBase"), FM("SentryPaint"), Vector3.zero, Quaternion.identity,
                    Vector3.one * 1.05f);
                Transform turret = Child(root.transform, "Turret", new Vector3(0f, 0f, -0.35f));
                MeshRenderer gun = Model(Child(turret, "Tilt", Vector3.zero, FaceCamera), "Gun", FModel("SentryTurret"), FM("SentryPaint"), Vector3.zero, Quaternion.identity,
                    Vector3.one * 1.1f);
                MeshRenderer charge = Quad(turret, "Charge", FM("SentryCharge"), new Vector3(0f, 1.05f, -0.4f), Vector2.one * 1.1f);
                Quad(root.transform, "Glow", FM("BastionGlow"), new Vector3(0f, 0f, 0.5f), Vector2.one * 3.4f);
                sentry.visual = visual;
                sentry.turret = turret;
                sentry.charge = charge;
                sentry.reticle = Reticle(root.transform, 3f);
                sentry.flashRenderers = new Renderer[] { body, gun };
                sentry.flashColor = new Color(1f, 0.8f, 0.6f);
                sentry.hazard = HazardKind.Sentry;
                sentry.allyRange = 14f;
                sentry.cue = AsteroidsArtBuilder.FieldSound("SentryCharge");
                sentry.radius = 1f;
                sentry.maxHealth = 8f;
                sentry.score = 400;
                sentry.contactDamage = 30f;
                sentry.fireInterval = 2.6f;
                sentry.shotSpeed = 9.5f;
                sentry.aimError = 5f;
                sentry.shotKind = EnemyShotKind.Plasma;
                sentry.burst = 3;
                sentry.chargeTime = 0.75f;
                sentry.explosionTint = new Color(1f, 0.6f, 0.25f);
                sentry.explosionScale = 1.7f;
                sentry.wraps = true;
                sentry.pulledByGravity = true;
                Save(root, "Enemies/Sentry");
            }

            // Minelayer: a broad hauler with a turning mine drum.
            {
                var root = new GameObject("Minelayer");
                var layer = root.AddComponent<Minelayer>();
                Transform visual = Child(root.transform, "Visual", Vector3.zero, FaceCamera);
                MeshRenderer hull = Model(visual, "Hull", FModel("Minelayer"), FM("MinelayerPaint"));
                Transform drum = Child(root.transform, "Drum", new Vector3(0f, -0.55f, -0.45f), FaceCamera);
                Model(drum, "Model", FModel("MineDrum"), FM("MinelayerPaint"));
                for (int side = -1; side <= 1; side += 2)
                {
                    Model(root.transform, side < 0 ? "FlameL" : "FlameR", AsteroidsArtBuilder.Model("FlameQuad"), M("BossFlame"), new Vector3(side * 0.8f, -1.15f, 0.1f),
                        Quaternion.identity, new Vector3(0.38f, 0.9f, 1f));
                }
                Quad(root.transform, "Glow", FM("BastionGlow"), new Vector3(0f, 0f, 0.6f), Vector2.one * 4f);
                layer.visual = visual;
                layer.drum = drum;
                layer.reticle = Reticle(root.transform, 3.6f);
                layer.flashRenderers = new Renderer[] { hull };
                layer.flashColor = new Color(1f, 0.9f, 0.6f);
                layer.hazard = HazardKind.Minelayer;
                layer.allyRange = 16f;
                layer.cue = AsteroidsArtBuilder.FieldSound("MineDrop");
                layer.radius = 1.25f;
                layer.maxHealth = 12f;
                layer.score = 700;
                layer.contactDamage = 35f;
                layer.fireInterval = 0f;
                layer.speed = 2.2f;
                layer.layInterval = 2.4f;
                layer.maxMines = 5;
                layer.weave = 1.2f;
                layer.explosionTint = new Color(1f, 0.8f, 0.3f);
                layer.explosionScale = 2f;
                layer.wraps = false;
                layer.pulledByGravity = false;
                Save(root, "Enemies/Minelayer");
            }

            // Phase stalker: a dark blade with glowing edges.
            {
                var root = new GameObject("Stalker");
                var stalker = root.AddComponent<PhaseStalker>();
                Transform visual = Child(root.transform, "Visual", Vector3.zero, FaceCamera, Vector3.one * 0.85f);
                MeshRenderer body = Model(visual, "Body", FModel("Stalker"), FM("StalkerPaint"));
                Quad(root.transform, "Glow", FM("StalkerGlow"), new Vector3(0f, 0f, 0.4f), Vector2.one * 2.8f);
                stalker.visual = visual;
                stalker.reticle = Reticle(root.transform, 2.4f);
                stalker.flashRenderers = new Renderer[] { body };
                stalker.flashColor = new Color(1f, 0.7f, 1f);
                stalker.hazard = HazardKind.Stalker;
                stalker.allyRange = 0f;
                stalker.cue = AsteroidsArtBuilder.FieldSound("Blink");
                stalker.radius = 0.75f;
                stalker.maxHealth = 4f;
                stalker.score = 500;
                stalker.contactDamage = 25f;
                stalker.fireInterval = 0f;
                stalker.shotSpeed = 10f;
                stalker.aimError = 4f;
                stalker.shotKind = EnemyShotKind.Plasma;
                stalker.fan = 3;
                stalker.fanSpread = 34f;
                stalker.explosionTint = new Color(0.8f, 0.45f, 1f);
                stalker.explosionScale = 1.3f;
                stalker.wraps = true;
                stalker.pulledByGravity = false;
                Save(root, "Enemies/Stalker");
            }

            // Corsair: a raider fighter on a pack hull in raider red.
            {
                var root = new GameObject("Corsair");
                var corsair = root.AddComponent<Corsair>();
                Transform visual = Child(root.transform, "Visual", Vector3.zero, FaceCamera);
                MeshRenderer hull = PackHull(visual, "StarSparrow6", FM("CorsairPaint"), 1.9f);
                var flames = new List<Transform>();
                for (int side = -1; side <= 1; side += 2)
                {
                    flames.Add(Model(root.transform, side < 0 ? "FlameL" : "FlameR", AsteroidsArtBuilder.Model("FlameQuad"), M("BossFlame"),
                        new Vector3(side * 0.2f, -0.9f, 0.1f), Quaternion.identity, new Vector3(0.28f, 0.8f, 1f)).transform);
                }
                Quad(root.transform, "Glow", FM("CorsairGlow"), new Vector3(0f, 0f, 0.4f), Vector2.one * 2.6f);
                corsair.visual = visual;
                corsair.flames = flames.ToArray();
                corsair.reticle = Reticle(root.transform, 2.6f);
                corsair.flashRenderers = new Renderer[] { hull };
                corsair.flashColor = Color.white;
                corsair.hazard = HazardKind.Corsair;
                corsair.allyRange = 30f;
                corsair.radius = 0.75f;
                corsair.maxHealth = 3f;
                corsair.score = 300;
                corsair.contactDamage = 24f;
                corsair.fireInterval = 1.5f;
                corsair.shotSpeed = 10f;
                corsair.aimError = 7f;
                corsair.shotKind = EnemyShotKind.Plasma;
                corsair.speed = 5.4f;
                corsair.acceleration = 7f;
                corsair.orbit = 6f;
                corsair.explosionTint = new Color(1f, 0.45f, 0.3f);
                corsair.explosionScale = 1.3f;
                corsair.wraps = true;
                corsair.pulledByGravity = true;
                Save(root, "Enemies/Corsair");
            }
        }


        // ------------------------------------------------------------------ allies

        private static void BuildAllies()
        {
            // Freighter.
            {
                var root = new GameObject("Freighter");
                var ally = root.AddComponent<Ally>();
                Transform visual = Child(root.transform, "Visual", Vector3.zero, FaceCamera, Vector3.one * 1.25f);
                MeshRenderer hull = Model(visual, "Hull", FModel("Freighter"), FM("AllyPaint"));
                var flames = new List<Transform>();
                for (int side = -1; side <= 1; side += 2)
                {
                    flames.Add(Model(root.transform, side < 0 ? "FlameL" : "FlameR", AsteroidsArtBuilder.Model("FlameQuad"), M("EngineFlame"), new Vector3(side * 0.38f, -2.55f, 0.1f),
                        Quaternion.identity, new Vector3(0.45f, 1.3f, 1f)).transform);
                }
                Quad(root.transform, "Glow", FM("AllyGlow"), new Vector3(0f, 0f, 0.6f), Vector2.one * 7f);
                AllyParts(root.transform, ally, 2.8f);
                ally.kind = AllyKind.Freighter;
                ally.visual = visual;
                ally.flames = flames.ToArray();
                ally.flashRenderers = new Renderer[] { hull };
                ally.radius = 1.55f;
                ally.maxHull = 120f;
                ally.barHeight = 2.9f;
                Save(root, "Allies/Freighter");
            }

            // Station.
            {
                var root = new GameObject("Station");
                var ally = root.AddComponent<Ally>();
                Transform visual = Child(root.transform, "Visual", Vector3.zero, FaceCamera, Vector3.one * 1.1f);
                MeshRenderer hub = Model(visual, "Hub", FModel("StationHub"), FM("AllyPaint"));
                Transform ring = Child(root.transform, "Ring", new Vector3(0f, 0f, 0.05f), FaceCamera, Vector3.one * 1.1f);
                MeshRenderer ringModel = Model(ring, "Model", FModel("StationRing"), FM("AllyPaint"));
                Quad(root.transform, "Glow", FM("AllyGlow"), new Vector3(0f, 0f, 0.6f), Vector2.one * 7.5f);
                AllyParts(root.transform, ally, 3f);
                ally.kind = AllyKind.Station;
                ally.visual = visual;
                ally.spinner = ring;
                ally.spinSpeed = 10f;
                ally.flashRenderers = new Renderer[] { hub, ringModel };
                ally.radius = 1.9f;
                ally.maxHull = 200f;
                ally.barHeight = 2.8f;
                Save(root, "Allies/Station");
            }
        }


        /// <summary>The hull bar, the smoke and the sounds every ally has.</summary>
        private static void AllyParts(Transform root, Ally ally, float barWidth)
        {
            Transform bar = Child(root, "HullBar", new Vector3(0f, 2.4f, -1f), null, new Vector3(barWidth, 0.2f, 1f));
            Quad(bar, "Back", FM("HullBarBack"), new Vector3(0f, 0f, 0.02f), new Vector2(1.06f, 1.5f));
            MeshRenderer fill = Quad(bar, "Fill", FM("HullBarFill"), Vector3.zero, Vector2.one);
            ParticleSystem smoke = ParticleFactory.Create("Smoke", root, M("ParticleSmoke"));
            ParticleFactory.Lifetime(smoke, 0.9f, 1.6f);
            ParticleFactory.Speed(smoke, 0.3f, 1f);
            ParticleFactory.Size(smoke, 0.5f, 1f);
            ParticleFactory.Rate(smoke, 16f);
            ParticleFactory.Colors(smoke, new Color(0.25f, 0.24f, 0.26f, 0.6f), new Color(0.4f, 0.36f, 0.34f, 0.45f));
            ParticleFactory.SizeOverLife(smoke, 0.6f, 1.8f);
            ParticleFactory.FadeOut(smoke);
            ParticleSystem.MainModule main = smoke.main;
            main.playOnAwake = false;
            smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ally.hullBar = fill.transform;
            ally.hullFill = fill;
            ally.smoke = smoke;
            ally.alarm = AsteroidsArtBuilder.FieldSound("AllyAlarm");
            ally.jump = AsteroidsArtBuilder.FieldSound("FreighterJump");
            ally.wraps = false;
            ally.pulledByGravity = false;
            ally.shotFactor = 0.25f;
            ally.contactFactor = 0.25f;
            ally.blastFactor = 0.3f;
            ally.cometDamage = 35f;
        }


        // ------------------------------------------------------------------ shots

        /// <summary>The enemy shot kinds of the field campaigns by name (they extend the enemy shot pools after the strike kinds).</summary>
        public static readonly (EnemyShotKind kind, string name, int pool)[] FieldEnemyShots =
        {
            (EnemyShotKind.Lightning, "Lightning", 16), (EnemyShotKind.Lance, "Lance", 12), (EnemyShotKind.SweepBeam, "SweepBeam", 8), (EnemyShotKind.NovaOrb, "NovaOrb", 16)
        };


        private static void BuildFieldShots()
        {
            // Lightning: a ring that warns, then the bolt and a flash.
            {
                var root = new GameObject("Lightning");
                var zone = root.AddComponent<ZoneShot>();
                MeshRenderer ring = Quad(root.transform, "Ring", FM("ZoneRing"), new Vector3(0f, 0f, 0.2f), Vector2.one);
                MeshRenderer bolt = Quad(root.transform, "Bolt", FM("LightningBolt"), new Vector3(0f, 0f, -0.6f), new Vector2(1.3f, 4.2f));
                bolt.gameObject.SetActive(false);
                var light = Child(root.transform, "Flash", new Vector3(0f, 0f, -2f)).gameObject.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(0.6f, 0.75f, 1f);
                light.range = 7f;
                light.intensity = 6f;
                light.shadows = LightShadows.None;
                light.enabled = false;
                zone.ring = ring.transform;
                zone.bolt = bolt.transform;
                zone.flashLight = light;
                zone.strikeClip = AsteroidsArtBuilder.FieldSound("LightningStrike");
                zone.enemy = true;
                zone.damage = 20f;
                zone.pierce = 1;
                zone.radius = 1.25f;
                zone.warningTime = 1.05f;
                zone.strikeTime = 0.25f;
                zone.lifetime = 1.25f;
                zone.impactTint = new Color(0.6f, 0.8f, 1f);
                zone.wraps = false;
                zone.pulledByGravity = false;
                zone.alignToVelocity = false;
                Save(root, "Shots/Lightning");
            }

            FieldBeamShot("Lance", FM("LanceBeam"), 0f, 1.2f, 0.7f, 30f, 60f);
            FieldBeamShot("SweepBeam", FM("SweepBeam"), 110f, 0.9f, 1.8f, 26f, 42f);

            // Nova orb: a swelling ball of plasma that bursts into a ring.
            {
                var root = new GameObject("NovaOrb");
                var orb = root.AddComponent<NovaOrb>();
                Transform visual = Child(root.transform, "Visual");
                MeshRenderer core = Quad(visual, "Core", FM("NovaOrb"), Vector3.zero, Vector2.one * 1.4f);
                Quad(visual, "Halo", FM("NovaOrbHalo"), new Vector3(0f, 0f, 0.1f), Vector2.one * 3f);
                orb.visual = visual;
                orb.core = core.transform;
                orb.burstClip = AsteroidsArtBuilder.FieldSound("OrbBurst");
                orb.enemy = true;
                orb.damage = 22f;
                orb.pierce = 1;
                orb.radius = 0.55f;
                orb.lifetime = 2.8f;
                orb.homing = 35f;
                orb.homingTime = 2.8f;
                orb.homingRange = 30f;
                orb.ring = 10;
                orb.ringSpeed = 7f;
                orb.ringShot = EnemyShotKind.Plasma;
                orb.impactTint = new Color(1f, 0.6f, 0.25f);
                orb.wraps = false;
                orb.pulledByGravity = false;
                orb.alignToVelocity = false;
                Save(root, "Shots/NovaOrb");
            }
        }


        private static void FieldBeamShot(string name, Material material, float sweep, float warning, float burn, float length, float damagePerSecond)
        {
            var root = new GameObject(name);
            var beam = root.AddComponent<FieldBeam>();
            MeshRenderer column = Quad(root.transform, "Beam", material, new Vector3(0f, length * 0.5f, -0.3f), new Vector2(0.1f, length));
            MeshRenderer source = Quad(root.transform, "Source", FM("BeamSource"), new Vector3(0f, 0f, -0.4f), Vector2.one);
            beam.beam = column.transform;
            beam.beamRenderer = column;
            beam.source = source.transform;
            beam.chargeClip = AsteroidsArtBuilder.FieldSound("LanceCharge");
            beam.fireClip = AsteroidsArtBuilder.FieldSound("LanceFire");
            beam.enemy = true;
            beam.damage = damagePerSecond;
            beam.pierce = 1;
            beam.radius = 0.3f;
            beam.sweep = sweep;
            beam.warningTime = warning;
            beam.burnTime = burn;
            beam.length = length;
            beam.halfWidth = 0.42f;
            beam.lifetime = warning + burn + 0.1f;
            beam.impactTint = new Color(1f, 0.5f, 0.35f);
            beam.wraps = false;
            beam.pulledByGravity = false;
            beam.alignToVelocity = false;
            Save(root, $"Shots/{name}");
        }


        // ------------------------------------------------------------------ bosses

        /// <summary>A model that turns on its own (<see cref="Spinner"/>) under <paramref name="parent"/>.</summary>
        private static Transform Spinning(Transform parent, string name, Mesh mesh, Material material, float scale, float speed, float z = 0f)
        {
            Transform spinner = Child(parent, name, new Vector3(0f, 0f, z));
            spinner.gameObject.AddComponent<Spinner>().speed = speed;
            Model(Child(spinner, "Tilt", Vector3.zero, FaceCamera), "Model", mesh, material, Vector3.zero, Quaternion.identity, Vector3.one * scale);
            return spinner;
        }


        private static void BuildFieldBosses()
        {
            // Slag Titan: the Rock Titan's iron cousin, ringed by iron moonlets; it calls flares when cornered.
            {
                Boss boss = BossRoot("SlagTitan", "Slag Titan", 3.2f, 120f, 9000, out Transform visual);
                boss.fieldRules = true;
                MeshRenderer body = Model(visual, "Body", AsteroidsArtBuilder.Model("TitanRock"), FM("TitanIron"), Vector3.zero, Quaternion.identity, Vector3.one * 3.3f);
                Transform orbit = Child(boss.transform, "Orbit");
                for (int i = 0; i < 4; i++)
                {
                    float angle = i * Mathf.PI * 0.5f;
                    Model(orbit, $"Moonlet{i + 1}", AsteroidsArtBuilder.Model($"Rock{i % 3 + 1}"), FM("Metal"), new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * 4.4f,
                        MoonletTurns[i % MoonletTurns.Length], Vector3.one * 0.55f);
                }
                Quad(boss.transform, "Glow", FM("BastionGlow"), new Vector3(0f, 0f, 1f), Vector2.one * 10f);
                boss.flashRenderers = new Renderer[] { body };
                boss.flashColor = new Color(1f, 0.7f, 0.45f);
                boss.movement = BossMovement.Drift;
                boss.moveSpeed = 1.5f;
                boss.randomSpin = 10f;
                boss.spinners = new[] { orbit };
                boss.spinSpeed = 30f;
                boss.explosionTint = new Color(1f, 0.55f, 0.25f);
                boss.attacks = new[]
                {
                    Attack(BossAttackType.SpawnMinions, 8f, 1, minion: MinionKind.Asteroid, asteroid: AsteroidKind.Metal),
                    Attack(BossAttackType.RingShot, 5f, 14, 7f, 1, shot: EnemyShotKind.Shrapnel),
                    Attack(BossAttackType.SpawnMinions, 12f, 1, minion: MinionKind.Asteroid, asteroid: AsteroidKind.Magma),
                    Attack(BossAttackType.SpreadShot, 3.8f, 8, 8.5f, 2, 70f, EnemyShotKind.Shrapnel),
                    Attack(BossAttackType.SolarFlare, 13f, 1, fromPhase: 2)
                };
                boss.phaseCalls = new[] { string.Empty, "The Titan is calling the star!" };
                Save(boss.gameObject, "Bosses/SlagTitan");
            }

            // Storm Herald: an ion manta circling the sector in a crown of lightning.
            {
                Boss boss = BossRoot("StormHerald", "Storm Herald", 2.6f, 170f, 11000, out Transform visual);
                boss.fieldRules = true;
                boss.contactDamage = 25f;
                visual.localRotation = FaceCamera;
                MeshRenderer body = Model(visual, "Body", FModel("StormHerald"), FM("HeraldPaint"), Vector3.zero, Quaternion.identity, Vector3.one * 1.15f);
                Transform rings = Spinning(boss.transform, "Rings", FModel("HeraldRings"), FM("HeraldPaint"), 1.1f, 0f, -0.2f);
                Quad(boss.transform, "Glow", FM("HeraldGlow"), new Vector3(0f, 0f, 0.8f), Vector2.one * 9f);
                boss.flashRenderers = new Renderer[] { body };
                boss.flashColor = new Color(0.7f, 0.9f, 1f);
                boss.movement = BossMovement.Orbit;
                boss.moveSpeed = 3.2f;
                boss.orbitRadius = 0.72f;
                boss.facesShip = true;
                boss.spinners = new[] { rings };
                boss.spinSpeed = 50f;
                boss.explosionTint = new Color(0.5f, 0.8f, 1f);
                boss.attacks = new[]
                {
                    Attack(BossAttackType.Lightning, 4f, 2, spread: 30f),
                    Attack(BossAttackType.AimedBurst, 4.5f, 4, 9f),
                    Attack(BossAttackType.SpawnMinions, 12f, 1, fromPhase: 1, minion: MinionKind.IonStorm),
                    Attack(BossAttackType.RingShot, 7f, 12, 6f, 1),
                    Attack(BossAttackType.Lightning, 5f, 4, fromPhase: 2, spread: 40f),
                    Attack(BossAttackType.SpawnMinions, 14f, 1, fromPhase: 2, minion: MinionKind.Corsair)
                };
                boss.phaseCalls = new[] { "The storm thickens!", "The Herald calls its raiders!" };
                Save(boss.gameObject, "Bosses/StormHerald");
            }

            // Bastion: a fortress with a turning ring of gun pods, lances and sentries.
            {
                Boss boss = BossRoot("Bastion", "Bastion", 3f, 230f, 14000, out Transform visual);
                boss.fieldRules = true;
                visual.localRotation = FaceCamera;
                MeshRenderer body = Model(visual, "Body", FModel("BastionFortress"), FM("BastionPaint"), Vector3.zero, Quaternion.identity, Vector3.one * 1.25f);
                Transform ring = Spinning(boss.transform, "Ring", FModel("BastionRing"), FM("BastionPaint"), 1.2f, 0f, 0.1f);
                Quad(boss.transform, "Glow", FM("BastionGlow"), new Vector3(0f, 0f, 0.9f), Vector2.one * 10f);
                boss.flashRenderers = new Renderer[] { body };
                boss.flashColor = new Color(1f, 0.85f, 0.6f);
                boss.movement = BossMovement.HoverTop;
                boss.moveSpeed = 1.6f;
                boss.hoverHeight = 0.5f;
                boss.spinners = new[] { ring };
                boss.spinSpeed = 25f;
                boss.explosionTint = new Color(1f, 0.65f, 0.3f);
                boss.attacks = new[]
                {
                    Attack(BossAttackType.Lance, 8f, 1),
                    Attack(BossAttackType.SpawnMinions, 15f, 1, minion: MinionKind.Sentry),
                    Attack(BossAttackType.DropMines, 10f, 2, fromPhase: 1),
                    Attack(BossAttackType.MissileSalvo, 7f, 3, 7f, 1),
                    Attack(BossAttackType.LanceFan, 8f, 3, fromPhase: 2, spread: 50f),
                    Attack(BossAttackType.RingShot, 6f, 14, 6.5f, 2)
                };
                boss.phaseCalls = new[] { "The Bastion opens its mine bays!", "Every gun is firing!" };
                Save(boss.gameObject, "Bosses/Bastion");
            }

            // Sunbreaker: the warlord's flagship with a sun cannon at its nose.
            {
                Boss boss = BossRoot("Sunbreaker", "Sunbreaker", 3f, 290f, 18000, out Transform visual);
                boss.fieldRules = true;
                boss.contactDamage = 25f;
                // Its weapons take turns: a breath between two of them, so its sweeps, flares and orbs can each be read.
                boss.attackGap = 2f;
                visual.localRotation = FaceCamera;
                MeshRenderer hull = PackHull(visual, "StarSparrow4", FM("WarlordPaint"), 6.6f);
                MeshRenderer cannon = Model(visual, "SunCannon", FModel("SunCannon"), FM("GatePaint"), new Vector3(0f, 0.3f, 3.2f), Quaternion.identity, Vector3.one * 1.1f);
                for (int i = -1; i <= 1; i += 2)
                {
                    Model(boss.transform, $"Engine{(i < 0 ? "L" : "R")}", AsteroidsArtBuilder.Model("FlameQuad"), M("BossFlame"), new Vector3(i * 0.9f, -3.1f, 0.1f),
                        Quaternion.identity, new Vector3(0.9f, 2.1f, 1f));
                }
                Quad(boss.transform, "Glow", FM("SunGlow"), new Vector3(0f, 0f, 0.9f), Vector2.one * 11f);
                boss.flashRenderers = new Renderer[] { hull, cannon };
                boss.flashColor = new Color(1f, 0.85f, 0.6f);
                boss.movement = BossMovement.HoverTop;
                boss.moveSpeed = 2.2f;
                boss.hoverHeight = 0.55f;
                boss.facesShip = true;
                boss.explosionTint = new Color(1f, 0.75f, 0.35f);
                boss.attacks = new[]
                {
                    Attack(BossAttackType.SweepBeam, 6f, 1),
                    Attack(BossAttackType.SpreadShot, 5.5f, 4, 9f, 0, 60f),
                    Attack(BossAttackType.SolarFlare, 20f, 1, fromPhase: 1),
                    Attack(BossAttackType.NovaOrbs, 7f, 3, 3.5f, 1, 60f),
                    Attack(BossAttackType.SweepBeam, 7f, 2, fromPhase: 2),
                    Attack(BossAttackType.MissileSalvo, 8f, 4, 7f, 2)
                };
                boss.phaseCalls = new[] { "The sun cannon is charging!", "Supernova protocol!" };
                Save(boss.gameObject, "Bosses/Sunbreaker");
            }

            // Scrap Leviathan: a maw of salvage in a cloud of turning wreckage that drags ships toward it.
            {
                Boss boss = BossRoot("ScrapLeviathan", "Scrap Leviathan", 3f, 240f, 13000, out Transform visual);
                boss.fieldRules = true;
                visual.localRotation = FaceCamera;
                MeshRenderer body = Model(visual, "Maw", FModel("ScrapMaw"), FM("LeviathanPaint"), Vector3.zero, Quaternion.identity, Vector3.one * 1.6f);
                Transform debris = Child(boss.transform, "Debris");
                for (int i = 0; i < 6; i++)
                {
                    float angle = i * Mathf.PI * 2f / 6f;
                    Model(debris, $"Wreck{i + 1}", FModel($"Wreck{i % 2 + 1}"), FM("Wreck"), new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * 3.6f,
                        Quaternion.Euler(i * 47f, i * 83f, i * 29f), Vector3.one * (0.55f + (i % 3) * 0.1f));
                }
                Quad(boss.transform, "Glow", FM("LeviathanGlow"), new Vector3(0f, 0f, 0.9f), Vector2.one * 10f);
                boss.flashRenderers = new Renderer[] { body };
                boss.flashColor = new Color(1f, 0.6f, 0.45f);
                boss.movement = BossMovement.Wander;
                boss.moveSpeed = 1.8f;
                boss.facesShip = true;
                boss.spinners = new[] { debris };
                boss.spinSpeed = 22f;
                boss.explosionTint = new Color(1f, 0.5f, 0.3f);
                boss.attacks = new[]
                {
                    Attack(BossAttackType.SpawnMinions, 9f, 2, minion: MinionKind.Asteroid, asteroid: AsteroidKind.Wreck),
                    Attack(BossAttackType.SpreadShot, 3.5f, 8, 8f, 0, 80f, EnemyShotKind.Shrapnel),
                    Attack(BossAttackType.SpawnMinions, 12f, 1, fromPhase: 1, minion: MinionKind.Well),
                    Attack(BossAttackType.Charge, 9f, 1, fromPhase: 1),
                    Attack(BossAttackType.SpawnMinions, 14f, 1, fromPhase: 2, minion: MinionKind.Minelayer),
                    Attack(BossAttackType.RingShot, 5.5f, 16, 6.5f, 2, shot: EnemyShotKind.Shrapnel)
                };
                boss.phaseCalls = new[] { "Its maw is pulling you in!", "The Leviathan sheds its scrap!" };
                Save(boss.gameObject, "Bosses/ScrapLeviathan");
            }

            // Wraith Matriarch: blinks from place to place with her stalkers.
            {
                Boss boss = BossRoot("WraithMatriarch", "Wraith Matriarch", 2.4f, 230f, 14000, out Transform visual);
                boss.fieldRules = true;
                visual.localRotation = FaceCamera;
                MeshRenderer body = Model(visual, "Body", FModel("WraithMatriarch"), FM("MatriarchPaint"), Vector3.zero, Quaternion.identity, Vector3.one * 1.3f);
                Quad(boss.transform, "Glow", FM("MatriarchGlow"), new Vector3(0f, 0f, 0.8f), Vector2.one * 9f);
                boss.flashRenderers = new Renderer[] { body };
                boss.flashColor = new Color(1f, 0.7f, 1f);
                boss.movement = BossMovement.Blink;
                boss.moveSpeed = 2f;
                boss.blinkInterval = 5.5f;
                boss.blinkTime = 1.1f;
                boss.blinkClip = AsteroidsArtBuilder.FieldSound("Blink");
                boss.facesShip = true;
                boss.explosionTint = new Color(0.8f, 0.45f, 1f);
                boss.attacks = new[]
                {
                    Attack(BossAttackType.SpawnMinions, 9f, 2, minion: MinionKind.Stalker),
                    Attack(BossAttackType.AimedBurst, 3f, 7, 11f),
                    Attack(BossAttackType.RingShot, 6f, 12, 5f, 1, shot: EnemyShotKind.Shard),
                    Attack(BossAttackType.Lightning, 5f, 3, fromPhase: 1, spread: 30f),
                    Attack(BossAttackType.NovaOrbs, 7f, 2, 3.5f, 2, 70f),
                    Attack(BossAttackType.SpawnMinions, 10f, 3, fromPhase: 2, minion: MinionKind.Stalker)
                };
                boss.phaseCalls = new[] { "She blinks faster!", "The Matriarch calls her brood!" };
                Save(boss.gameObject, "Bosses/WraithMatriarch");
            }

            // The Hydra: three heads, acid and hydra rocks; it heals once wounded.
            {
                Boss boss = BossRoot("Hydra", "The Hydra", 3f, 240f, 16000, out Transform visual);
                boss.fieldRules = true;
                visual.localRotation = FaceCamera;
                MeshRenderer body = Model(visual, "Body", FModel("Hydra"), FM("HydraHide"), new Vector3(0f, 0f, -0.8f), Quaternion.identity, Vector3.one * 1.6f);
                Quad(boss.transform, "Glow", FM("HydraGlow"), new Vector3(0f, 0f, 0.8f), Vector2.one * 10f);
                boss.flashRenderers = new Renderer[] { body };
                boss.flashColor = new Color(0.7f, 1f, 0.6f);
                boss.movement = BossMovement.Drift;
                boss.moveSpeed = 1.3f;
                boss.facesShip = true;
                boss.regeneration = 1.2f;
                boss.explosionTint = new Color(0.55f, 1f, 0.4f);
                boss.attacks = new[]
                {
                    Attack(BossAttackType.SpreadShot, 3f, 5, 7f, 0, 50f, EnemyShotKind.Acid),
                    Attack(BossAttackType.SpawnMinions, 9f, 1, minion: MinionKind.Asteroid, asteroid: AsteroidKind.Hydra),
                    Attack(BossAttackType.RingShot, 6f, 12, 6f, 1, shot: EnemyShotKind.Acid),
                    Attack(BossAttackType.SpawnMinions, 12f, 2, fromPhase: 1, minion: MinionKind.Asteroid, asteroid: AsteroidKind.Hydra),
                    Attack(BossAttackType.Charge, 9f, 1, fromPhase: 2),
                    Attack(BossAttackType.SpreadShot, 4f, 7, 7.5f, 2, 80f, EnemyShotKind.Acid)
                };
                boss.phaseCalls = new[] { "It heals when you let up - keep firing!", "The Hydra is frenzied!" };
                Save(boss.gameObject, "Bosses/Hydra");
            }

            // The Gatekeeper: the gate's eye in two turning rings; four phases and everything the rift has seen.
            {
                Boss boss = BossRoot("Gatekeeper", "The Gatekeeper", 3.4f, 300f, 25000, out Transform visual);
                boss.fieldRules = true;
                // The gate holds the middle: it does not ram, so touching it only grazes.
                boss.contactDamage = 25f;
                // Everything the rift has, but one thing at a time: a breath between two attacks.
                boss.attackGap = 2f;
                visual.localRotation = FaceCamera;
                MeshRenderer core = Model(visual, "Core", FModel("GateCore"), FM("GatePaint"), Vector3.zero, Quaternion.identity, Vector3.one * 1.2f);
                // The two rings of the gate turn against each other on their own spinners.
                Spinning(boss.transform, "Gate", FModel("GateRing"), FM("GatePaint"), 1.15f, 14f, 0.1f);
                Spinning(boss.transform, "InnerGate", FModel("GateRingInner"), FM("GatePaint"), 1.1f, -26f, 0.05f);
                Quad(boss.transform, "Glow", FM("GateGlow"), new Vector3(0f, 0f, 0.9f), Vector2.one * 12f);
                boss.flashRenderers = new Renderer[] { core };
                boss.flashColor = new Color(1f, 0.9f, 0.6f);
                boss.movement = BossMovement.Anchor;
                boss.moveSpeed = 1f;
                boss.phaseThresholds = new[] { 0.75f, 0.5f, 0.25f };
                boss.spinners = new Transform[0];
                boss.explosionTint = new Color(1f, 0.85f, 0.4f);
                boss.entrySeconds = 3.5f;
                boss.attacks = new[]
                {
                    Attack(BossAttackType.RingShot, 7f, 6, 6f),
                    Attack(BossAttackType.Lance, 8f, 1, fromPhase: 2),
                    Attack(BossAttackType.Lightning, 6f, 3, spread: 30f),
                    Attack(BossAttackType.SweepBeam, 8f, 1, fromPhase: 1),
                    Attack(BossAttackType.SpawnMinions, 12f, 1, fromPhase: 1, minion: MinionKind.Stalker),
                    Attack(BossAttackType.SpawnMinions, 14f, 1, fromPhase: 2, minion: MinionKind.Asteroid, asteroid: AsteroidKind.Hydra),
                    Attack(BossAttackType.NovaOrbs, 7f, 4, 3.5f, 2, 120f),
                    Attack(BossAttackType.SolarFlare, 16f, 1, fromPhase: 2),
                    Attack(BossAttackType.SweepBeam, 7f, 2, fromPhase: 3),
                    Attack(BossAttackType.LanceFan, 8f, 5, fromPhase: 3, spread: 70f),
                    Attack(BossAttackType.SpawnMinions, 12f, 2, fromPhase: 3, minion: MinionKind.Corsair)
                };
                boss.phaseCalls = new[] { "The gate opens!", "Something is coming through!", "The gate is collapsing - finish it!" };
                Save(boss.gameObject, "Bosses/Gatekeeper");
            }
        }
    }
}
