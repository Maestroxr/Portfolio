using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Portfolio.Asteroids.EditorTools
{
    /// <summary>
    /// Builds the prefabs of the Asteroids module from the generated art and the model packs: the five kinds of
    /// asteroid, the hazards, the enemies and bosses, every projectile, the pickups, the pooled effects and the ship.
    /// Models are placed under a "Visual" child (which tumbles and flashes) and turned so their nose points up the
    /// screen and their top faces the camera.
    /// </summary>
    internal static class AsteroidsPrefabBuilder
    {
        /// <summary>Turns a model built with +Y up and +Z forward so it faces the camera with its nose up the screen.</summary>
        private static readonly Quaternion FaceCamera = Quaternion.Euler(-90f, 0f, 0f);

        private static Mesh QuadMesh => Resources.GetBuiltinResource<Mesh>("Quad.fbx");
        private static Mesh SphereMesh => Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
        private static Mesh CubeMesh => Resources.GetBuiltinResource<Mesh>("Cube.fbx");

        /// <summary>The scale that gives the built-in sphere (which is not one unit across) a <paramref name="diameter"/>.</summary>
        public static Vector3 SphereScale(float diameter)
        {
            Mesh sphere = SphereMesh;
            float size = sphere != null ? Mathf.Max(0.001f, sphere.bounds.size.x) : 1f;
            return Vector3.one * (diameter / size);
        }

        public static void BuildAll()
        {
            Progress("Asteroids", 0.1f);
            BuildAsteroids();
            Progress("Hazards", 0.25f);
            BuildHazards();
            Progress("Enemies", 0.4f);
            BuildEnemies();
            Progress("Projectiles", 0.5f);
            BuildShots();
            Progress("Pickups", 0.6f);
            BuildPickups();
            Progress("Effects", 0.75f);
            BuildEffects();
            Progress("Bosses", 0.85f);
            BuildBosses();
            Progress("Ship", 0.95f);
            BuildShip();
            AssetDatabase.SaveAssets();
            EditorUtility.ClearProgressBar();
        }

        private static void Progress(string step, float value)
        {
            EditorUtility.DisplayProgressBar("Asteroids", $"Building prefabs: {step}...", value);
        }

        public static T Load<T>(string relative) where T : Component
        {
            var prefab = AsteroidsAssets.Load<GameObject>($"Prefabs/{relative}.prefab");
            return prefab != null ? prefab.GetComponent<T>() : null;
        }

        // ------------------------------------------------------------------ helpers

        private static Transform Child(Transform parent, string name, Vector3 position = default, Quaternion? rotation = null, Vector3? scale = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = rotation ?? Quaternion.identity;
            go.transform.localScale = scale ?? Vector3.one;
            return go.transform;
        }

        private static MeshRenderer Model(Transform parent, string name, Mesh mesh, Material material, Vector3 position = default,
            Quaternion? rotation = null, Vector3? scale = null)
        {
            Transform t = Child(parent, name, position, rotation, scale);
            t.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = t.gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return renderer;
        }

        /// <summary>A camera-facing quad (the camera looks along +Z at the XY plane).</summary>
        private static MeshRenderer Quad(Transform parent, string name, Material material, Vector3 position, Vector2 size)
        {
            return Model(parent, name, QuadMesh, material, position, Quaternion.identity, new Vector3(size.x, size.y, 1f));
        }

        /// <summary>Copies the meshes, materials and transforms of a pack prefab (without its colliders) under <paramref name="parent"/>.</summary>
        private static Transform CopyModel(GameObject source, Transform parent, string name, Material overrideMaterial = null)
        {
            Transform copy = Child(parent, name, source.transform.localPosition, source.transform.localRotation, source.transform.localScale);
            var filter = source.GetComponent<MeshFilter>();
            var sourceRenderer = source.GetComponent<MeshRenderer>();
            if (filter != null && sourceRenderer != null)
            {
                copy.gameObject.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                var renderer = copy.gameObject.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = overrideMaterial != null ? new[] { overrideMaterial } : sourceRenderer.sharedMaterials;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            foreach (Transform child in source.transform)
            {
                CopyModel(child.gameObject, copy, child.name, overrideMaterial);
            }
            return copy;
        }

        private static GameObject PackPrefab(string relative)
        {
            return AsteroidsAssets.Load<GameObject>($"{AsteroidsArtBuilder.StarSparrow}/Prefabs/{relative}.prefab");
        }

        private static Material M(string name)
        {
            return AsteroidsArtBuilder.Material(name);
        }

        private static AudioSource Loop(GameObject host, AudioClip clip, float volume)
        {
            var source = host.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
            source.volume = volume;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            return source;
        }

        private static GameObject Save(GameObject root, string relative)
        {
            return AsteroidsAssets.SavePrefab(root, $"Prefabs/{relative}.prefab");
        }

        private static Renderer[] Renderers(Transform root)
        {
            return root.GetComponentsInChildren<Renderer>(true);
        }

        // ------------------------------------------------------------------ asteroids

        private static void BuildAsteroids()
        {
            Asteroid(AsteroidKind.Rock, "Rock", new[] { "Rock1", "Rock2", "Rock3" }, "Rock", new Color(1f, 0.8f, 0.6f));
            Asteroid(AsteroidKind.Ore, "Ore", new[] { "Rock1", "Rock2", "Rock3" }, "Ore", new Color(1f, 0.85f, 0.4f));
            Asteroid(AsteroidKind.Magma, "Magma", new[] { "Rock1", "Rock2", "Rock3" }, "Magma", new Color(1f, 0.55f, 0.2f));
            Asteroid(AsteroidKind.Ice, "Ice", new[] { "Ice1", "Ice2", "Ice3" }, "IcePalette", new Color(0.6f, 0.9f, 1f));
            Asteroid(AsteroidKind.Crystal, "Crystal", new[] { "Crystal1", "Crystal2", "Crystal3" }, "CrystalPalette", new Color(1f, 0.5f, 1f));
        }

        private static void Asteroid(AsteroidKind kind, string name, string[] meshes, string material, Color flash)
        {
            var root = new GameObject(name);
            var asteroid = root.AddComponent<Asteroid>();
            Transform visual = Child(root.transform, "Visual");
            var meshList = new List<Mesh>();
            foreach (string mesh in meshes)
            {
                meshList.Add(AsteroidsArtBuilder.Model(mesh));
            }
            MeshRenderer renderer = Model(visual, "Model", meshList[0], M(material));
            asteroid.kind = kind;
            asteroid.meshes = meshList.ToArray();
            asteroid.meshFilter = renderer.GetComponent<MeshFilter>();
            asteroid.modelRadius = 1.05f;
            asteroid.visual = visual;
            asteroid.flashRenderers = new Renderer[] { renderer };
            asteroid.flashColor = flash;
            asteroid.radius = AsteroidRules.Radius(kind, AsteroidSize.Large);
            asteroid.maxHealth = AsteroidRules.Health(kind, AsteroidSize.Large);
            asteroid.wraps = true;
            asteroid.pulledByGravity = true;
            Save(root, $"Asteroids/{name}");
        }

        // ------------------------------------------------------------------ hazards

        private static void BuildHazards()
        {
            // Proximity mine
            {
                var root = new GameObject("Mine");
                var mine = root.AddComponent<Mine>();
                Transform visual = Child(root.transform, "Visual");
                Mesh mesh = AsteroidsArtBuilder.PackMesh("BonusContent/MinesSample.FBX", "SpaceMine1");
                MeshRenderer body = Model(visual, "Model", mesh, AsteroidsArtBuilder.PackMaterial("BonusContent/Mine Sample 1"), -(mesh != null ? mesh.bounds.center : Vector3.zero) * 0.52f,
                    Quaternion.identity, Vector3.one * 0.52f);
                MeshRenderer beacon = Quad(root.transform, "Beacon", M("Beacon"), new Vector3(0f, 0f, -0.9f), Vector2.one * 1.9f);
                MeshRenderer ring = Quad(root.transform, "Range", M("DangerRing"), new Vector3(0f, 0f, 0.4f), Vector2.one * 6f);
                ring.gameObject.SetActive(false);
                mine.visual = visual;
                mine.beacon = beacon;
                mine.rangeRing = ring.transform;
                mine.flashRenderers = new Renderer[] { body };
                mine.flashColor = new Color(0.6f, 0.9f, 1f);
                mine.radius = 0.72f;
                mine.maxHealth = 1f;
                mine.score = 150;
                mine.contactDamage = 30f;
                mine.randomSpin = 30f;
                mine.blastScale = 1f;
                mine.blastDamage = 5f;
                mine.playerBlastDamage = 38f;
                mine.blastPush = 6f;
                mine.blastTint = new Color(0.45f, 0.85f, 1f);
                Save(root, "Hazards/Mine");
            }

            // Cluster bomb
            {
                var root = new GameObject("ClusterBomb");
                var bomb = root.AddComponent<ClusterBomb>();
                Transform visual = Child(root.transform, "Visual");
                Mesh mesh = AsteroidsArtBuilder.PackMesh("BonusContent/MinesSample.FBX", "SpaceMine11");
                MeshRenderer body = Model(visual, "Model", mesh, AsteroidsArtBuilder.PackMaterial("BonusContent/Mine Sample 2"), -(mesh != null ? mesh.bounds.center : Vector3.zero) * 0.52f,
                    Quaternion.identity, Vector3.one * 0.52f);
                MeshRenderer core = Quad(root.transform, "Core", M("Beacon"), new Vector3(0f, 0f, -0.9f), Vector2.one * 2.3f);
                MeshRenderer ring = Quad(root.transform, "Range", M("DangerRing"), new Vector3(0f, 0f, 0.4f), Vector2.one * 8f);
                TextMeshPro countdown = WorldText(root.transform, "Countdown", "5", 7f, new Vector3(0f, 0f, -1.9f));
                bomb.visual = visual;
                bomb.core = core;
                bomb.rangeRing = ring.transform;
                bomb.countdown = countdown;
                bomb.flashRenderers = new Renderer[] { body };
                bomb.flashColor = new Color(1f, 0.6f, 0.3f);
                bomb.radius = 0.8f;
                bomb.maxHealth = 2f;
                bomb.score = 200;
                bomb.contactDamage = 30f;
                bomb.randomSpin = 20f;
                bomb.blastScale = 1.35f;
                bomb.blastDamage = 6f;
                bomb.playerBlastDamage = 45f;
                bomb.blastPush = 8f;
                bomb.blastTint = new Color(1f, 0.55f, 0.2f);
                bomb.fuseTime = 4.5f;
                bomb.shrapnel = 14;
                bomb.shrapnelSpeed = 9f;
                Save(root, "Hazards/ClusterBomb");
            }

            // Supply pod
            {
                var root = new GameObject("SupplyPod");
                var pod = root.AddComponent<Lootable>();
                Transform visual = Child(root.transform, "Visual");
                Transform tilt = Child(visual, "Tilt", Vector3.zero, FaceCamera * Quaternion.Euler(0f, 90f, 0f));
                MeshRenderer body = Model(tilt, "Model", AsteroidsArtBuilder.Model("SupplyPod"), M("Palette"), Vector3.zero, Quaternion.identity, Vector3.one * 0.9f);
                MeshRenderer beacon = Quad(root.transform, "Beacon", M("Beacon"), new Vector3(0f, 0f, -0.8f), Vector2.one * 1.4f);
                pod.visual = visual;
                pod.beacon = beacon;
                pod.flashRenderers = new Renderer[] { body };
                pod.flashColor = new Color(0.7f, 1f, 0.8f);
                pod.radius = 0.6f;
                pod.maxHealth = 2f;
                pod.score = 100;
                pod.contactDamage = 10f;
                pod.wraps = false;
                pod.Spin = new Vector3(0f, 0f, 25f);
                Save(root, "Hazards/SupplyPod");
            }

            // Comet
            {
                var root = new GameObject("Comet");
                var comet = root.AddComponent<Comet>();
                Transform visual = Child(root.transform, "Visual");
                Model(visual, "Model", AsteroidsArtBuilder.Model("Comet"), AsteroidsArtBuilder.PackMaterial("BonusContent/AsteroidsLava_Red"), Vector3.zero, Quaternion.identity, Vector3.one * 0.95f);
                Quad(root.transform, "Glow", M("Glow"), new Vector3(0f, 0f, -0.5f), Vector2.one * 4f).sharedMaterial = GlowTint("CometGlow", new Color(2.5f, 1f, 0.35f, 0.7f));
                ParticleSystem trail = ParticleFactory.Create("Trail", root.transform, M("ParticleAdd"));
                ParticleFactory.Lifetime(trail, 0.45f, 0.8f);
                ParticleFactory.Speed(trail, 0.2f, 1.2f);
                ParticleFactory.Size(trail, 0.9f, 1.8f);
                ParticleFactory.Rate(trail, 90f);
                ParticleFactory.Sphere(trail, 0.6f);
                ParticleFactory.SizeOverLife(trail, 1f, 0.1f);
                ColorOverLife(trail, new Color(1f, 0.95f, 0.6f), new Color(1f, 0.45f, 0.1f), new Color(0.6f, 0.1f, 0.05f));
                ParticleFactory.MaxParticles(trail, 200);
                var light = Child(root.transform, "Light", new Vector3(0f, 0f, -1.5f)).gameObject.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(1f, 0.55f, 0.2f);
                light.range = 7f;
                light.intensity = 4f;
                light.shadows = LightShadows.None;
                comet.visual = visual;
                comet.trail = trail;
                comet.radius = 0.95f;
                comet.wraps = false;
                comet.pulledByGravity = false;
                comet.randomSpin = 120f;
                comet.damage = 45f;
                comet.speed = 19f;
                Save(root, "Hazards/Comet");
            }

            // Gravity well
            {
                var root = new GameObject("GravityWell");
                var well = root.AddComponent<GravityWell>();
                Transform visual = Child(root.transform, "Visual");
                Model(visual, "Core", SphereMesh, M("BlackHole"), Vector3.zero, Quaternion.identity, SphereScale(1.8f));
                MeshRenderer disk = Quad(visual, "Disk", M("Accretion"), new Vector3(0f, 0f, 0.3f), Vector2.one * 7f);
                Quad(visual, "Halo", AsteroidsArtBuilder.GlowMaterial("WellHalo", AsteroidsArtBuilder.Texture("Ring"), new Color(0.8f, 0.5f, 2f, 0.6f), true),
                    new Vector3(0f, 0f, 0.35f), Vector2.one * 4.2f);
                ParticleSystem swirl = ParticleFactory.Create("Swirl", visual, M("ParticleAdd"));
                ParticleSystem.MainModule main = swirl.main;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                ParticleFactory.Lifetime(swirl, 1.2f, 1.8f);
                ParticleFactory.Speed(swirl, 0f, 0f);
                ParticleFactory.Size(swirl, 0.12f, 0.3f);
                ParticleFactory.Rate(swirl, 70f);
                ParticleFactory.Circle(swirl, 7f, true);
                ParticleSystem.ShapeModule shape = swirl.shape;
                shape.rotation = Vector3.zero;
                ParticleSystem.VelocityOverLifetimeModule velocity = swirl.velocityOverLifetime;
                velocity.enabled = true;
                velocity.space = ParticleSystemSimulationSpace.Local;
                velocity.radial = new ParticleSystem.MinMaxCurve(-4f);
                velocity.orbitalZ = new ParticleSystem.MinMaxCurve(1.6f);
                velocity.x = new ParticleSystem.MinMaxCurve(0f);
                velocity.y = new ParticleSystem.MinMaxCurve(0f);
                velocity.z = new ParticleSystem.MinMaxCurve(0f);
                ParticleFactory.Colors(swirl, new Color(0.7f, 0.5f, 1f), new Color(1f, 0.9f, 1f));
                ParticleFactory.FadeOut(swirl, 0.2f);
                well.visual = visual;
                well.disk = disk.transform;
                well.hum = Loop(root, AsteroidsArtBuilder.Sound("WellHum"), 0.5f);
                well.radius = 1f;
                well.wraps = true;
                well.pulledByGravity = false;
                well.pullRadius = 9f;
                well.strength = 22f;
                well.coreRadius = 0.95f;
                well.coreDamage = 45f;
                Save(root, "Hazards/GravityWell");
            }
        }

        private static Material GlowTint(string name, Color color)
        {
            return AsteroidsArtBuilder.GlowMaterial(name, AsteroidsArtBuilder.Texture("Glow"), color, true);
        }

        private static TextMeshPro WorldText(Transform parent, string name, string text, float size, Vector3 position)
        {
            Transform t = Child(parent, name, position);
            var label = t.gameObject.AddComponent<TextMeshPro>();
            label.text = text;
            label.fontSize = size;
            label.alignment = TextAlignmentOptions.Center;
            label.fontStyle = FontStyles.Bold;
            label.color = Color.white;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.rectTransform.sizeDelta = new Vector2(6f, 3f);
            Material outline = AsteroidsSceneBuilder.HudFont;
            if (outline != null)
            {
                label.fontSharedMaterial = outline;
            }
            label.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            return label;
        }

        private static void ColorOverLife(ParticleSystem system, Color start, Color middle, Color end)
        {
            ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(start, 0f), new GradientColorKey(middle, 0.35f), new GradientColorKey(end, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
            color.color = new ParticleSystem.MinMaxGradient(gradient);
        }

        // ------------------------------------------------------------------ enemies

        private static void BuildEnemies()
        {
            Saucer("Saucer", false);
            Saucer("Scout", true);

            var root = new GameObject("Wasp");
            var wasp = root.AddComponent<Wasp>();
            Transform visual = Child(root.transform, "Visual", Vector3.zero, FaceCamera);
            Transform body = CopyModel(PackPrefab("BonusContent/Flying Insect"), visual, "Insect", M("Wasp"));
            body.localScale = Vector3.one * (1.55f / 6.76f);
            body.localPosition = Vector3.zero;
            body.localRotation = Quaternion.identity;
            var wings = new List<Transform>();
            foreach (Transform child in body)
            {
                if (child.name.Contains("Wing"))
                {
                    wings.Add(child);
                }
            }
            wasp.visual = visual;
            wasp.wings = wings.ToArray();
            wasp.flashRenderers = Renderers(body);
            wasp.flashColor = new Color(1f, 0.6f, 1f);
            wasp.radius = 0.7f;
            wasp.maxHealth = 3f;
            wasp.score = 150;
            wasp.contactDamage = 22f;
            wasp.fireInterval = 0f;
            wasp.explosionTint = new Color(0.8f, 0.45f, 1f);
            wasp.explosionScale = 1.1f;
            Save(root, "Enemies/Wasp");
        }

        private static void Saucer(string name, bool scout)
        {
            var root = new GameObject(name);
            var saucer = root.AddComponent<Saucer>();
            float size = scout ? 0.68f : 1.05f;
            Transform visual = Child(root.transform, "Visual", Vector3.zero, FaceCamera, Vector3.one * size);
            MeshRenderer hull = Model(visual, "Hull", AsteroidsArtBuilder.Model(scout ? "Scout" : "Saucer"), M("Palette"));
            Transform rotor = Child(root.transform, "Lights", Vector3.zero, FaceCamera, Vector3.one * size);
            Model(rotor, "Model", AsteroidsArtBuilder.Model(scout ? "ScoutLights" : "SaucerLights"), M("Palette"));
            Quad(root.transform, "Glow", GlowTint(scout ? "ScoutGlow" : "SaucerGlow", scout ? new Color(2f, 0.4f, 0.3f, 0.35f) : new Color(0.4f, 2f, 1.2f, 0.3f)),
                new Vector3(0f, 0f, 0.4f), Vector2.one * size * 3.2f);
            saucer.visual = visual;
            saucer.rotor = rotor;
            saucer.hum = Loop(root, AsteroidsArtBuilder.Sound("SaucerHum"), scout ? 0.18f : 0.22f);
            saucer.flashRenderers = new Renderer[] { hull };
            saucer.flashColor = new Color(1f, 1f, 1f);
            saucer.radius = size * 0.95f;
            saucer.maxHealth = scout ? 3f : 5f;
            saucer.score = scout ? 1000 : 250;
            saucer.contactDamage = 30f;
            saucer.fireInterval = scout ? 1.15f : 1.45f;
            saucer.shotSpeed = scout ? 9.5f : 7.5f;
            saucer.aimError = 8f;
            saucer.aims = scout;
            saucer.speed = scout ? 4.3f : 3.2f;
            saucer.weaveAmplitude = scout ? 2.8f : 2f;
            saucer.weaveFrequency = scout ? 0.8f : 0.55f;
            saucer.shotKind = EnemyShotKind.Plasma;
            saucer.explosionTint = scout ? new Color(1f, 0.45f, 0.3f) : new Color(0.45f, 1f, 0.7f);
            saucer.explosionScale = scout ? 1.2f : 1.7f;
            saucer.wraps = false;
            saucer.pulledByGravity = false;
            Save(root, $"Enemies/{name}");
        }

        // ------------------------------------------------------------------ projectiles

        private static void BuildShots()
        {
            Shot("PlayerBolt", false, M("Bolt"), new Vector2(0.42f, 1.5f), 0.24f, 0.95f, new Color(0.45f, 0.9f, 1f), trail: new Color(0.3f, 0.8f, 1f));
            Shot("PlayerLaser", false, M("Laser"), new Vector2(0.34f, 3.4f), 0.26f, 0.65f, new Color(1f, 0.4f, 0.55f));
            Shot("PlayerPellet", false, M("Pellet"), new Vector2(0.5f, 0.5f), 0.2f, 0.42f, new Color(1f, 0.7f, 0.3f));
            Shot("DroneBolt", false, M("DroneBolt"), new Vector2(0.3f, 0.95f), 0.18f, 0.8f, new Color(0.5f, 1f, 0.7f));
            MissileShot("PlayerMissile", false, new Color(0.5f, 1f, 0.5f), 260f, 2.2f, 14f);

            Shot("EnemyPlasma", true, M("EnemyPlasma"), new Vector2(0.85f, 0.85f), 0.28f, 3.2f, new Color(1f, 0.4f, 0.8f), damage: 16f, align: false);
            Shot("EnemyShrapnel", true, M("Shrapnel"), new Vector2(0.28f, 0.95f), 0.2f, 1.2f, new Color(1f, 0.6f, 0.2f), damage: 12f);
            Shot("VoidShard", true, M("VoidShard"), new Vector2(0.7f, 0.7f), 0.24f, 3.5f, new Color(0.8f, 0.4f, 1f), damage: 14f, homing: 70f, homingTime: 3f);
            MissileShot("EnemyMissile", true, new Color(1f, 0.35f, 0.3f), 110f, 3.5f, 30f);
            Shot("EnemyAcid", true, M("Acid"), new Vector2(0.9f, 0.9f), 0.3f, 3f, new Color(0.6f, 1f, 0.3f), damage: 18f, align: false);
        }

        private static void Shot(string name, bool enemy, Material material, Vector2 size, float radius, float lifetime, Color impact,
            float damage = 1f, bool align = true, float homing = 0f, float homingTime = 3f, Color? trail = null)
        {
            var root = new GameObject(name);
            var shot = root.AddComponent<Shot>();
            Transform visual = Child(root.transform, "Visual");
            Quad(visual, "Glow", material, Vector3.zero, size);
            shot.visual = visual;
            shot.enemy = enemy;
            shot.damage = damage;
            shot.pierce = 1;
            shot.radius = radius;
            shot.lifetime = lifetime;
            shot.alignToVelocity = align;
            shot.homing = homing;
            shot.homingTime = homingTime;
            shot.impactTint = impact;
            shot.wraps = false;
            shot.pulledByGravity = false;
            if (!align)
            {
                shot.Spin = new Vector3(0f, 0f, 240f);
            }
            if (trail.HasValue)
            {
                var line = root.AddComponent<TrailRenderer>();
                line.sharedMaterial = M("ParticleAdd");
                line.time = 0.08f;
                line.startWidth = size.x * 0.45f;
                line.endWidth = 0f;
                line.minVertexDistance = 0.1f;
                line.startColor = trail.Value * 1.6f;
                line.endColor = new Color(trail.Value.r, trail.Value.g, trail.Value.b, 0f);
                line.shadowCastingMode = ShadowCastingMode.Off;
                line.receiveShadows = false;
                line.alignment = LineAlignment.View;
                shot.trail = line;
            }
            Save(root, $"Shots/{name}");
        }

        private static void MissileShot(string name, bool enemy, Color tint, float homing, float homingTime, float homingRange)
        {
            var root = new GameObject(name);
            var shot = root.AddComponent<Shot>();
            Transform visual = Child(root.transform, "Visual");
            Mesh mesh = AsteroidsArtBuilder.PackMesh("BonusContent/MissilesSample.FBX", "MissileViking");
            Model(visual, "Model", mesh, AsteroidsArtBuilder.PackMaterial("BonusContent/Missile"), -(FaceCamera * (mesh != null ? mesh.bounds.center : Vector3.zero)) * 0.2f,
                FaceCamera, Vector3.one * 0.2f);
            Quad(visual, "Glow", GlowTint(name + "Glow", new Color(tint.r * 3f, tint.g * 3f, tint.b * 3f, 0.9f)), new Vector3(0f, -0.3f, -0.2f), Vector2.one * 0.8f);
            ParticleSystem exhaust = ParticleFactory.Create("Exhaust", root.transform, M("ParticleAdd"));
            exhaust.transform.localPosition = new Vector3(0f, -0.3f, 0f);
            ParticleFactory.Lifetime(exhaust, 0.25f, 0.45f);
            ParticleFactory.Speed(exhaust, 0f, 0.3f);
            ParticleFactory.Size(exhaust, 0.25f, 0.45f);
            ParticleFactory.Rate(exhaust, 60f);
            ParticleFactory.SizeOverLife(exhaust, 1f, 0.2f);
            ParticleFactory.Colors(exhaust, tint, Color.white);
            ParticleFactory.FadeOut(exhaust);
            ParticleFactory.MaxParticles(exhaust, 80);
            shot.visual = visual;
            shot.exhaust = exhaust;
            shot.enemy = enemy;
            shot.damage = enemy ? 22f : 3f;
            shot.pierce = 1;
            shot.radius = 0.3f;
            shot.lifetime = enemy ? 4.5f : 2.4f;
            shot.homing = homing;
            shot.homingTime = homingTime;
            shot.homingRange = homingRange;
            shot.alignToVelocity = true;
            shot.impactTint = tint;
            shot.wraps = false;
            shot.pulledByGravity = false;
            Save(root, $"Shots/{name}");
        }

        // ------------------------------------------------------------------ pickups

        private static void BuildPickups()
        {
            // Crystal
            {
                var root = new GameObject("Crystal");
                var crystal = root.AddComponent<PointReward>();
                Transform visual = Child(root.transform, "Visual");
                MeshRenderer gem = Model(visual, "Gem", AsteroidsArtBuilder.Model("Crystal"), M("Palette"), Vector3.zero, Quaternion.identity, Vector3.one * 0.62f);
                MeshRenderer glow = Quad(root.transform, "Glow", GlowTint("CrystalGlow", new Color(0.4f, 1.6f, 2.2f, 0.5f)), new Vector3(0f, 0f, 0.3f), Vector2.one * 1.3f);
                crystal.visual = visual;
                crystal.blinkRenderers = new Renderer[] { gem, glow };
                crystal.title = "Crystal";
                crystal.color = new Color(0.4f, 0.95f, 1f);
                crystal.radius = 0.35f;
                crystal.lifetime = 10f;
                crystal.Spin = new Vector3(0f, 200f, 0f);
                crystal.attractRange = 2.8f;
                AsteroidsAssets.SetAuto(crystal, nameof(PointReward.PointsAward), p => p.intValue = 50);
                Save(root, "Pickups/Crystal");
            }

            Pickup<HealthReward>("Repair", "Repair Kit", new Color(0.4f, 1f, 0.5f), "Repair", reward => AsteroidsAssets.SetAuto(reward, nameof(HealthReward.HealthAward), p => p.floatValue = 40f));
            Pickup<ShieldReward>("Shield", "Shield Cell", new Color(0.35f, 0.7f, 1f), "Shield", reward => AsteroidsAssets.SetAuto(reward, nameof(ShieldReward.ShieldAward), p => p.floatValue = 100f));
            foreach (WeaponType weapon in (WeaponType[])Enum.GetValues(typeof(WeaponType)))
            {
                string icon = weapon == WeaponType.Missiles ? "Missile" : weapon.ToString();
                Pickup<WeaponReward>($"Weapon{weapon}", WeaponRules.Title(weapon), WeaponRules.Tint(weapon), icon,
                    reward => AsteroidsAssets.SetAuto(reward, nameof(WeaponReward.Weapon), p => p.enumValueIndex = (int)weapon));
            }
            foreach (PowerUpType power in (PowerUpType[])Enum.GetValues(typeof(PowerUpType)))
            {
                Pickup<PowerUpReward>($"PowerUp{power}", PowerUps.Title(power), PowerUps.Tint(power), power.ToString(),
                    reward => AsteroidsAssets.SetAuto(reward, nameof(PowerUpReward.PowerUp), p => p.enumValueIndex = (int)power));
            }
            Pickup<LifeReward>("ExtraShip", "Extra Ship", new Color(1f, 0.85f, 0.35f), "Ship", null);
            Pickup<BombReward>("NovaBomb", "Nova Bomb", new Color(0.5f, 0.9f, 1f), "Nova", null);
        }

        private static void Pickup<T>(string name, string title, Color color, string icon, Action<T> setup) where T : Reward
        {
            var root = new GameObject(name);
            var reward = root.AddComponent<T>();
            Transform visual = Child(root.transform, "Visual");
            Transform frameRoot = Child(visual, "Frame", Vector3.zero, FaceCamera);
            MeshRenderer frame = Model(frameRoot, "Model", AsteroidsArtBuilder.Model("PickupFrame"), M("Palette"), Vector3.zero, Quaternion.identity, Vector3.one * 0.95f);
            MeshRenderer core = Quad(root.transform, "Core", GlowTint($"Core{name}", new Color(color.r * 2.2f, color.g * 2.2f, color.b * 2.2f, 0.85f)), new Vector3(0f, 0f, 0.1f), Vector2.one * 1.5f);
            MeshRenderer symbol = Quad(root.transform, "Icon", M($"Icon{icon}"), new Vector3(0f, 0f, -0.35f), Vector2.one * 0.72f);
            reward.visual = visual;
            reward.blinkRenderers = new Renderer[] { frame, core, symbol };
            reward.title = title;
            reward.color = color;
            reward.radius = 0.55f;
            reward.lifetime = 12f;
            reward.Spin = new Vector3(0f, 0f, 70f);
            reward.attractRange = 2.4f;
            reward.bobHeight = 0.12f;
            setup?.Invoke(reward);
            Save(root, $"Pickups/{name}");
        }

        /// <summary>Every pickup prefab, for the pools.</summary>
        public static List<Reward> AllPickups()
        {
            var names = new List<string> { "Crystal", "Repair", "Shield", "ExtraShip", "NovaBomb" };
            foreach (WeaponType weapon in (WeaponType[])Enum.GetValues(typeof(WeaponType)))
            {
                names.Add($"Weapon{weapon}");
            }
            foreach (PowerUpType power in (PowerUpType[])Enum.GetValues(typeof(PowerUpType)))
            {
                names.Add($"PowerUp{power}");
            }
            var rewards = new List<Reward>();
            foreach (string name in names)
            {
                var reward = Load<Reward>($"Pickups/{name}");
                if (reward != null)
                {
                    rewards.Add(reward);
                }
            }
            return rewards;
        }

        // ------------------------------------------------------------------ effects

        private static PooledEffect Effect(string name, float duration, out Transform root)
        {
            var go = new GameObject(name);
            root = go.transform;
            var effect = go.AddComponent<PooledEffect>();
            effect.duration = duration;
            return effect;
        }

        private static ParticleSystem Burst(Transform parent, string name, Material material, int count, float minLife, float maxLife, float minSpeed, float maxSpeed,
            float minSize, float maxSize, Color a, Color b)
        {
            ParticleSystem system = ParticleFactory.Create(name, parent, material);
            ParticleSystem.MainModule main = system.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.2f;
            ParticleFactory.Lifetime(system, minLife, maxLife);
            ParticleFactory.Speed(system, minSpeed, maxSpeed);
            ParticleFactory.Size(system, minSize, maxSize);
            ParticleFactory.Colors(system, a, b);
            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            ParticleFactory.Sphere(system, 0.2f);
            ParticleFactory.MaxParticles(system, Mathf.Max(count, 10));
            return system;
        }

        private static void Stretch(ParticleSystem system, float velocityScale)
        {
            var renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = velocityScale;
            renderer.lengthScale = 1f;
        }

        private static void Flat(ParticleSystem system)
        {
            // Everything happens in the plane of the playfield.
            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = Mathf.Max(0.05f, shape.radius);
            shape.rotation = Vector3.zero;
        }

        private static Light Flash(Transform parent, PooledEffect effect, float range, float intensity, float time)
        {
            var light = Child(parent, "Flash", new Vector3(0f, 0f, -1.5f)).gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = range;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
            light.enabled = false;
            effect.flash = light;
            effect.flashIntensity = intensity;
            effect.flashTime = time;
            return light;
        }

        private static MeshRenderer Ring(Transform parent, PooledEffect effect, float time, float growth)
        {
            MeshRenderer ring = Quad(parent, "Ring", M("ParticleRing"), new Vector3(0f, 0f, 0.2f), Vector2.one);
            ring.enabled = false;
            effect.ring = ring;
            effect.ringTime = time;
            effect.ringGrowth = growth;
            return ring;
        }

        private static void BuildEffects()
        {
            // Explosion: flash, fireball, sparks, smoke and a shock ring.
            {
                PooledEffect effect = Effect("Explosion", 2f, out Transform root);
                ParticleSystem flare = Burst(root, "Flare", M("ParticleFlare"), 1, 0.12f, 0.16f, 0f, 0f, 3.2f, 3.8f, Color.white, Color.white);
                ParticleFactory.SizeOverLife(flare, 1f, 0.3f);
                ParticleSystem fire = Burst(root, "Fire", M("ParticleAdd"), 22, 0.35f, 0.75f, 1.5f, 5.5f, 0.7f, 1.5f, Color.white, Color.white);
                Flat(fire);
                ParticleFactory.SizeOverLife(fire, 1f, 0.2f);
                ColorOverLife(fire, new Color(1f, 0.95f, 0.7f), new Color(1f, 0.55f, 0.15f), new Color(0.5f, 0.1f, 0.05f));
                ParticleSystem sparks = Burst(root, "Sparks", M("ParticleSpark"), 16, 0.3f, 0.6f, 6f, 13f, 0.12f, 0.22f, Color.white, Color.white);
                Flat(sparks);
                Stretch(sparks, 0.06f);
                ParticleFactory.FadeOut(sparks);
                ParticleSystem smoke = Burst(root, "Smoke", M("ParticleSmoke"), 8, 0.9f, 1.6f, 0.4f, 1.6f, 1.2f, 2.2f, new Color(0.18f, 0.16f, 0.18f, 0.7f), new Color(0.3f, 0.26f, 0.26f, 0.55f));
                Flat(smoke);
                ParticleFactory.SizeOverLife(smoke, 0.6f, 1.5f);
                ParticleFactory.FadeOut(smoke);
                ParticleFactory.Spin(smoke, 60f);
                Flash(root, effect, 9f, 7f, 0.3f);
                Ring(root, effect, 0.35f, 3.2f);
                effect.systems = new[] { flare, fire, sparks, smoke };
                effect.tinted = new[] { sparks };
                Save(root.gameObject, "Effects/Explosion");
            }

            // Rock debris: tumbling chunks, dust and sparks.
            {
                PooledEffect effect = Effect("RockDebris", 1.8f, out Transform root);
                ParticleSystem chunks = Burst(root, "Chunks", M("Debris"), 12, 0.7f, 1.4f, 2f, 6f, 0.14f, 0.34f, new Color(0.8f, 0.78f, 0.76f), new Color(0.55f, 0.52f, 0.5f));
                Flat(chunks);
                ParticleFactory.Tumble(chunks, 400f);
                ParticleFactory.MeshParticles(chunks, AsteroidsArtBuilder.Model("Rock1"));
                ParticleFactory.SizeOverLife(chunks, 1f, 0.4f);
                ParticleSystem dust = Burst(root, "Dust", M("ParticleSmoke"), 7, 0.6f, 1.1f, 0.5f, 2f, 0.8f, 1.6f, new Color(0.5f, 0.45f, 0.4f, 0.55f), new Color(0.4f, 0.36f, 0.34f, 0.4f));
                Flat(dust);
                ParticleFactory.SizeOverLife(dust, 0.7f, 1.4f);
                ParticleFactory.FadeOut(dust);
                ParticleSystem sparks = Burst(root, "Sparks", M("ParticleSpark"), 10, 0.2f, 0.45f, 4f, 9f, 0.1f, 0.18f, Color.white, Color.white);
                Flat(sparks);
                Stretch(sparks, 0.05f);
                ParticleFactory.FadeOut(sparks);
                effect.systems = new[] { chunks, dust, sparks };
                effect.tinted = new[] { dust };
                Save(root.gameObject, "Effects/RockDebris");
            }

            // Ice shards
            {
                PooledEffect effect = Effect("IceShards", 1.6f, out Transform root);
                ParticleSystem shards = Burst(root, "Shards", M("ParticleShard"), 22, 0.5f, 1.1f, 3f, 9f, 0.25f, 0.55f, Color.white, Color.white);
                Flat(shards);
                ParticleFactory.Spin(shards, 400f);
                ParticleFactory.FadeOut(shards);
                ParticleSystem mist = Burst(root, "Mist", M("ParticleAdd"), 10, 0.4f, 0.8f, 0.5f, 2.5f, 0.8f, 1.6f, new Color(0.5f, 0.8f, 1f, 0.35f), new Color(0.8f, 0.95f, 1f, 0.25f));
                Flat(mist);
                ParticleFactory.FadeOut(mist);
                ParticleSystem flare = Burst(root, "Flare", M("ParticleFlare"), 1, 0.1f, 0.14f, 0f, 0f, 2.2f, 2.6f, Color.white, Color.white);
                effect.systems = new[] { shards, mist, flare };
                effect.tinted = new[] { shards };
                Save(root.gameObject, "Effects/IceShards");
            }

            // Void crystal shards
            {
                PooledEffect effect = Effect("CrystalShards", 1.6f, out Transform root);
                ParticleSystem shards = Burst(root, "Shards", M("ParticleShard"), 18, 0.5f, 1f, 2.5f, 8f, 0.3f, 0.6f, Color.white, Color.white);
                Flat(shards);
                ParticleFactory.Spin(shards, 500f);
                ParticleFactory.FadeOut(shards);
                ParticleSystem glow = Burst(root, "Glow", M("ParticleAdd"), 12, 0.3f, 0.7f, 1f, 4f, 0.5f, 1.1f, Color.white, Color.white);
                Flat(glow);
                ParticleFactory.FadeOut(glow);
                Flash(root, effect, 7f, 5f, 0.25f);
                effect.systems = new[] { shards, glow };
                effect.tinted = new[] { shards, glow };
                Save(root.gameObject, "Effects/CrystalShards");
            }

            // Hit spark
            {
                PooledEffect effect = Effect("Spark", 0.6f, out Transform root);
                ParticleSystem sparks = Burst(root, "Sparks", M("ParticleSpark"), 7, 0.15f, 0.32f, 4f, 10f, 0.08f, 0.14f, Color.white, Color.white);
                Flat(sparks);
                Stretch(sparks, 0.05f);
                ParticleFactory.FadeOut(sparks);
                ParticleSystem flare = Burst(root, "Flare", M("ParticleFlare"), 1, 0.07f, 0.1f, 0f, 0f, 0.9f, 1.1f, Color.white, Color.white);
                effect.systems = new[] { sparks, flare };
                effect.tinted = new[] { sparks, flare };
                effect.scaleSpeed = false;
                Save(root.gameObject, "Effects/Spark");
            }

            // Pickup burst
            {
                PooledEffect effect = Effect("Pickup", 1f, out Transform root);
                ParticleSystem sparkle = Burst(root, "Sparkle", M("ParticleFlare"), 14, 0.35f, 0.7f, 1.5f, 4.5f, 0.25f, 0.5f, Color.white, Color.white);
                Flat(sparkle);
                ParticleFactory.SizeOverLife(sparkle, 1f, 0f);
                Ring(root, effect, 0.35f, 2.6f);
                effect.systems = new[] { sparkle };
                effect.tinted = new[] { sparkle };
                effect.scaleSpeed = false;
                Save(root.gameObject, "Effects/Pickup");
            }

            // Warp-in: sparks rushing inward and a flash.
            {
                PooledEffect effect = Effect("WarpIn", 1f, out Transform root);
                ParticleSystem rush = Burst(root, "Rush", M("ParticleSpark"), 20, 0.3f, 0.35f, -4f, -3f, 0.12f, 0.2f, Color.white, Color.white);
                ParticleFactory.Circle(rush, 1.2f, true);
                ParticleSystem.ShapeModule shape = rush.shape;
                shape.rotation = Vector3.zero;
                Stretch(rush, 0.08f);
                ParticleSystem flare = Burst(root, "Flare", M("ParticleFlare"), 1, 0.25f, 0.3f, 0f, 0f, 2.6f, 3f, Color.white, Color.white);
                ParticleSystem.MainModule flareMain = flare.main;
                flareMain.startDelay = 0.25f;
                ParticleFactory.SizeOverLife(flare, 1f, 0f);
                Ring(root, effect, 0.3f, 2.2f);
                effect.systems = new[] { rush, flare };
                effect.tinted = new[] { rush, flare };
                Save(root.gameObject, "Effects/WarpIn");
            }

            // Shockwave ring
            {
                PooledEffect effect = Effect("Shockwave", 0.8f, out Transform root);
                Ring(root, effect, 0.5f, 2.2f);
                effect.systems = new ParticleSystem[0];
                Save(root.gameObject, "Effects/Shockwave");
            }

            // Nova: a huge ring over the playfield, a flash and a burst of streaks.
            {
                PooledEffect effect = Effect("Nova", 1.6f, out Transform root);
                ParticleSystem streaks = Burst(root, "Streaks", M("ParticleSpark"), 90, 0.5f, 0.9f, 18f, 34f, 0.2f, 0.35f, new Color(0.6f, 0.9f, 1f), Color.white);
                Flat(streaks);
                Stretch(streaks, 0.05f);
                ParticleFactory.FadeOut(streaks);
                ParticleSystem flare = Burst(root, "Flare", M("ParticleFlare"), 1, 0.25f, 0.3f, 0f, 0f, 9f, 10f, Color.white, Color.white);
                Flash(root, effect, 40f, 8f, 0.6f);
                Ring(root, effect, 0.9f, 48f);
                effect.systems = new[] { streaks, flare };
                effect.tinted = new[] { streaks };
                effect.scaleSpeed = false;
                Save(root.gameObject, "Effects/Nova");
            }

            // Ship explosion
            {
                PooledEffect effect = Effect("ShipExplosion", 3f, out Transform root);
                ParticleSystem flare = Burst(root, "Flare", M("ParticleFlare"), 1, 0.2f, 0.25f, 0f, 0f, 6f, 7f, Color.white, Color.white);
                ParticleSystem fire = Burst(root, "Fire", M("ParticleAdd"), 40, 0.5f, 1.1f, 2f, 8f, 0.9f, 1.9f, Color.white, Color.white);
                Flat(fire);
                ParticleFactory.SizeOverLife(fire, 1f, 0.2f);
                ColorOverLife(fire, new Color(1f, 0.97f, 0.85f), new Color(1f, 0.6f, 0.2f), new Color(0.5f, 0.1f, 0.05f));
                ParticleSystem chunks = Burst(root, "Hull", M("Debris"), 16, 1f, 1.8f, 2f, 7f, 0.1f, 0.25f, new Color(0.75f, 0.8f, 0.9f), new Color(0.4f, 0.45f, 0.55f));
                Flat(chunks);
                ParticleFactory.Tumble(chunks, 500f);
                ParticleFactory.MeshParticles(chunks, CubeMesh);
                ParticleSystem sparks = Burst(root, "Sparks", M("ParticleSpark"), 30, 0.4f, 0.9f, 8f, 16f, 0.12f, 0.24f, Color.white, Color.white);
                Flat(sparks);
                Stretch(sparks, 0.06f);
                ParticleFactory.FadeOut(sparks);
                ParticleSystem smoke = Burst(root, "Smoke", M("ParticleSmoke"), 12, 1.2f, 2.2f, 0.5f, 2f, 1.5f, 2.8f, new Color(0.15f, 0.14f, 0.16f, 0.75f), new Color(0.28f, 0.26f, 0.28f, 0.6f));
                Flat(smoke);
                ParticleFactory.SizeOverLife(smoke, 0.6f, 1.6f);
                ParticleFactory.FadeOut(smoke);
                Flash(root, effect, 14f, 9f, 0.5f);
                Ring(root, effect, 0.55f, 7f);
                effect.systems = new[] { flare, fire, chunks, sparks, smoke };
                effect.tinted = new[] { sparks };
                Save(root.gameObject, "Effects/ShipExplosion");
            }

            // Dash trail
            {
                PooledEffect effect = Effect("DashTrail", 0.8f, out Transform root);
                ParticleSystem streaks = Burst(root, "Streaks", M("ParticleSpark"), 16, 0.2f, 0.4f, 5f, 10f, 0.12f, 0.2f, Color.white, Color.white);
                ParticleFactory.Cone(streaks, 18f, 0.3f);
                ParticleSystem.ShapeModule shape = streaks.shape;
                shape.rotation = new Vector3(90f, 0f, 0f);
                Stretch(streaks, 0.06f);
                ParticleFactory.FadeOut(streaks);
                effect.systems = new[] { streaks };
                effect.tinted = new[] { streaks };
                effect.scaleSpeed = false;
                Save(root.gameObject, "Effects/DashTrail");
            }

            // Boss charge telegraph
            {
                PooledEffect effect = Effect("Telegraph", 0.9f, out Transform root);
                Ring(root, effect, 0.7f, 2.6f);
                ParticleSystem flare = Burst(root, "Flare", M("ParticleFlare"), 1, 0.6f, 0.7f, 0f, 0f, 4f, 4.5f, Color.white, Color.white);
                ParticleFactory.SizeOverLife(flare, 0.4f, 1.2f);
                ParticleFactory.FadeOut(flare);
                effect.systems = new[] { flare };
                effect.tinted = new[] { flare };
                Save(root.gameObject, "Effects/Telegraph");
            }

            // Floating score
            {
                var root = new GameObject("ScorePopup");
                var popup = root.AddComponent<ScorePopup>();
                TextMeshPro label = WorldText(root.transform, "Label", "100", 5f, Vector3.zero);
                popup.label = label;
                Save(root, "Effects/ScorePopup");
            }
        }

        // ------------------------------------------------------------------ bosses

        private static BossAttack Attack(BossAttackType type, float cooldown, int count, float speed = 8f, int fromPhase = 0, float spread = 50f,
            EnemyShotKind shot = EnemyShotKind.Plasma, MinionKind minion = MinionKind.Asteroid, AsteroidKind asteroid = AsteroidKind.Rock)
        {
            return new BossAttack
            {
                type = type,
                cooldown = cooldown,
                count = count,
                speed = speed,
                fromPhase = fromPhase,
                spread = spread,
                shot = shot,
                minion = minion,
                asteroidKind = asteroid
            };
        }

        private static Boss BossRoot(string name, string title, float radius, float health, int score, out Transform visual)
        {
            var root = new GameObject(name);
            var boss = root.AddComponent<Boss>();
            visual = Child(root.transform, "Visual");
            boss.visual = visual;
            boss.displayName = title;
            boss.radius = radius;
            boss.maxHealth = health;
            boss.score = score;
            boss.contactDamage = 40f;
            boss.pulledByGravity = false;
            boss.wraps = false;
            MeshRenderer shield = Model(root.transform, "Shield", SphereMesh, M("BossShield"), Vector3.zero, Quaternion.identity, SphereScale(radius * 2.35f));
            shield.gameObject.SetActive(false);
            boss.shield = shield.gameObject;
            return boss;
        }

        private static void BuildBosses()
        {
            // Rock Titan: a giant molten asteroid that sheds rocks and shrapnel.
            {
                Boss boss = BossRoot("RockTitan", "Rock Titan", 3.1f, 75f, 5000, out Transform visual);
                MeshRenderer body = Model(visual, "Body", AsteroidsArtBuilder.Model("TitanRock"), M("TitanRock"), Vector3.zero, Quaternion.identity, Vector3.one * 3.2f);
                Transform orbit = Child(boss.transform, "Orbit");
                for (int i = 0; i < 3; i++)
                {
                    float angle = i * Mathf.PI * 2f / 3f;
                    Model(orbit, $"Moonlet{i + 1}", AsteroidsArtBuilder.Model($"Rock{i + 1}"), M("Magma"), new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * 4.3f,
                        UnityEngine.Random.rotation, Vector3.one * 0.55f);
                }
                var light = Child(boss.transform, "Core", new Vector3(0f, 0f, -4f)).gameObject.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(1f, 0.4f, 0.6f);
                light.range = 12f;
                light.intensity = 3f;
                light.shadows = LightShadows.None;
                boss.flashRenderers = new Renderer[] { body };
                boss.flashColor = new Color(1f, 0.6f, 0.4f);
                boss.movement = BossMovement.Drift;
                boss.moveSpeed = 1.4f;
                boss.randomSpin = 12f;
                boss.spinners = new[] { orbit };
                boss.spinSpeed = 35f;
                boss.explosionTint = new Color(1f, 0.5f, 0.3f);
                boss.attacks = new[]
                {
                    Attack(BossAttackType.SpawnMinions, 6.5f, 3, minion: MinionKind.Asteroid, asteroid: AsteroidKind.Rock),
                    Attack(BossAttackType.RingShot, 5.5f, 12, 7f, 1, shot: EnemyShotKind.Shrapnel),
                    Attack(BossAttackType.SpawnMinions, 9f, 2, minion: MinionKind.Asteroid, asteroid: AsteroidKind.Ore),
                    Attack(BossAttackType.SpreadShot, 4f, 7, 8f, 2, 60f, EnemyShotKind.Shrapnel)
                };
                Save(boss.gameObject, "Bosses/RockTitan");
            }

            // Raider Mothership: a giant saucer that sends scouts, mines and plasma.
            {
                Boss boss = BossRoot("Mothership", "Raider Mothership", 2.7f, 150f, 7500, out Transform visual);
                visual.localRotation = FaceCamera;
                MeshRenderer hull = Model(visual, "Hull", AsteroidsArtBuilder.Model("Saucer"), M("Palette"), Vector3.zero, Quaternion.identity, Vector3.one * 2.8f);
                Transform rotor = Child(boss.transform, "Lights", Vector3.zero, FaceCamera, Vector3.one * 2.8f);
                Model(rotor, "Model", AsteroidsArtBuilder.Model("SaucerLights"), M("Palette"));
                Transform inner = Child(boss.transform, "InnerLights", new Vector3(0f, 0f, -0.3f), FaceCamera, Vector3.one * 1.6f);
                Model(inner, "Model", AsteroidsArtBuilder.Model("ScoutLights"), M("Palette"));
                Quad(boss.transform, "Glow", GlowTint("MothershipGlow", new Color(0.4f, 2f, 1.3f, 0.35f)), new Vector3(0f, 0f, 0.8f), Vector2.one * 9f);
                boss.flashRenderers = new Renderer[] { hull };
                boss.flashColor = Color.white;
                boss.movement = BossMovement.HoverTop;
                boss.moveSpeed = 3f;
                boss.hoverHeight = 0.52f;
                boss.spinners = new[] { rotor, inner };
                boss.spinSpeed = 60f;
                boss.explosionTint = new Color(0.45f, 1f, 0.7f);
                boss.attacks = new[]
                {
                    Attack(BossAttackType.AimedBurst, 3f, 5, 9f),
                    Attack(BossAttackType.SpawnMinions, 10f, 1, minion: MinionKind.Saucer),
                    Attack(BossAttackType.DropMines, 8f, 2, fromPhase: 1),
                    Attack(BossAttackType.RingShot, 7f, 16, 6f, 1),
                    Attack(BossAttackType.SpreadShot, 4f, 9, 8f, 2, 70f)
                };
                Save(boss.gameObject, "Bosses/Mothership");
            }

            // Hive Queen: a giant alien wasp that spits acid, calls her brood and charges.
            {
                Boss boss = BossRoot("HiveQueen", "Hive Queen", 2.3f, 125f, 9000, out Transform visual);
                visual.localRotation = FaceCamera;
                Transform body = CopyModel(PackPrefab("BonusContent/Flying Insect"), visual, "Queen", M("HiveQueen"));
                body.localScale = Vector3.one * (5f / 6.76f);
                body.localPosition = Vector3.zero;
                body.localRotation = Quaternion.identity;
                var wings = new List<Transform>();
                foreach (Transform child in body)
                {
                    if (child.name.Contains("Wing"))
                    {
                        wings.Add(child);
                    }
                }
                var flapper = boss.gameObject.AddComponent<Flapper>();
                flapper.wings = wings.ToArray();
                flapper.speed = 20f;
                flapper.angle = 26f;
                Quad(boss.transform, "Glow", GlowTint("QueenGlow", new Color(1.4f, 0.4f, 2f, 0.4f)), new Vector3(0f, 0f, 0.8f), Vector2.one * 8f);
                boss.flashRenderers = Renderers(body);
                boss.flashColor = new Color(1f, 0.6f, 1f);
                boss.movement = BossMovement.Wander;
                boss.moveSpeed = 2.6f;
                boss.facesShip = true;
                boss.explosionTint = new Color(0.8f, 0.45f, 1f);
                boss.attacks = new[]
                {
                    Attack(BossAttackType.SpawnMinions, 7f, 3, minion: MinionKind.Wasp),
                    Attack(BossAttackType.SpreadShot, 3.5f, 5, 7f, 0, 50f, EnemyShotKind.Acid),
                    Attack(BossAttackType.Charge, 8f, 1, fromPhase: 1),
                    Attack(BossAttackType.RingShot, 6f, 12, 6f, 2, shot: EnemyShotKind.Acid)
                };
                Save(boss.gameObject, "Bosses/HiveQueen");
            }

            // Dreadnought: a capital ship with missiles, mines and walls of plasma.
            {
                Boss boss = BossRoot("Dreadnought", "Dreadnought", 3f, 210f, 12000, out Transform visual);
                visual.localRotation = FaceCamera;
                Mesh mesh = AsteroidsArtBuilder.ShipMesh("StarSparrow8");
                float scale = 6.6f / Mathf.Max(0.01f, mesh != null ? mesh.bounds.size.z : 13f);
                MeshRenderer hull = Model(visual, "Hull", mesh, AsteroidsArtBuilder.PackMaterial("StarSparrow Red"), -(mesh != null ? mesh.bounds.center : Vector3.zero) * scale,
                    Quaternion.identity, Vector3.one * scale);
                for (int i = -1; i <= 1; i += 2)
                {
                    Model(boss.transform, $"Engine{(i < 0 ? "L" : "R")}", AsteroidArtFlame, M("BossFlame"), new Vector3(i * 0.9f, -3f, 0.1f), Quaternion.identity, new Vector3(0.9f, 2f, 1f));
                }
                Quad(boss.transform, "Glow", GlowTint("DreadnoughtGlow", new Color(2f, 0.5f, 0.35f, 0.35f)), new Vector3(0f, 0f, 0.8f), Vector2.one * 10f);
                boss.flashRenderers = new Renderer[] { hull };
                boss.flashColor = new Color(1f, 0.8f, 0.6f);
                boss.movement = BossMovement.HoverTop;
                boss.moveSpeed = 2.2f;
                boss.hoverHeight = 0.58f;
                boss.facesShip = true;
                boss.explosionTint = new Color(1f, 0.6f, 0.3f);
                boss.attacks = new[]
                {
                    Attack(BossAttackType.MissileSalvo, 5f, 4, 7f),
                    Attack(BossAttackType.SpreadShot, 3f, 7, 9f, 0, 60f),
                    Attack(BossAttackType.DropMines, 9f, 3, fromPhase: 1),
                    Attack(BossAttackType.RingShot, 6f, 20, 6f, 1),
                    Attack(BossAttackType.SpawnMinions, 12f, 1, fromPhase: 2, minion: MinionKind.Saucer),
                    Attack(BossAttackType.AimedBurst, 4f, 8, 11f, 2)
                };
                Save(boss.gameObject, "Bosses/Dreadnought");
            }
        }

        private static Mesh AsteroidArtFlame => AsteroidsArtBuilder.Model("FlameQuad");

        // ------------------------------------------------------------------ ship

        private static void BuildShip()
        {
            var root = new GameObject("Ship");
            var player = root.AddComponent<AsteroidsPlayer>();
            var visuals = root.AddComponent<ShipVisuals>();
            root.AddComponent<AsteroidsAutopilot>().enabled = false;
            Transform bank = Child(root.transform, "Bank");
            Transform modelRoot = Child(bank, "Model", Vector3.zero, FaceCamera, Vector3.one * 0.12f);
            modelRoot.gameObject.AddComponent<MeshFilter>();
            var modelRenderer = modelRoot.gameObject.AddComponent<MeshRenderer>();
            modelRenderer.shadowCastingMode = ShadowCastingMode.Off;
            modelRenderer.receiveShadows = false;

            var flames = new List<Transform>();
            var exhausts = new List<ParticleSystem>();
            for (int i = 0; i < 3; i++)
            {
                MeshRenderer flame = Model(bank, $"Flame{i + 1}", AsteroidsArtBuilder.Model("FlameQuad"), M("EngineFlame"), new Vector3(0f, -0.7f, 0.05f), Quaternion.identity, new Vector3(0.34f, 1f, 1f));
                flames.Add(flame.transform);
                ParticleSystem exhaust = ParticleFactory.Create($"Exhaust{i + 1}", root.transform, M("ParticleAdd"));
                exhaust.transform.localPosition = new Vector3(0f, -0.8f, 0.05f);
                ParticleFactory.Lifetime(exhaust, 0.25f, 0.45f);
                ParticleFactory.Speed(exhaust, 0f, 0.4f);
                ParticleFactory.Size(exhaust, 0.18f, 0.32f);
                ParticleFactory.Rate(exhaust, 30f);
                ParticleFactory.SizeOverLife(exhaust, 1f, 0.1f);
                ParticleFactory.FadeOut(exhaust);
                ParticleFactory.MaxParticles(exhaust, 120);
                exhausts.Add(exhaust);
            }

            MeshRenderer shield = Model(root.transform, "Shield", SphereMesh, M("ShipShield"), Vector3.zero, Quaternion.identity, SphereScale(2.55f));
            Quad(root.transform, "Halo", GlowTint("ShipHalo", new Color(0.35f, 0.75f, 1.4f, 0.22f)), new Vector3(0f, 0f, 0.6f), Vector2.one * 3.4f);
            MeshRenderer muzzle = Quad(root.transform, "Muzzle", M("Muzzle"), new Vector3(0f, 1f, -0.05f), Vector2.one * 1.2f);
            muzzle.enabled = false;
            ParticleSystem smoke = ParticleFactory.Create("DamageSmoke", root.transform, M("ParticleSmoke"));
            ParticleFactory.Lifetime(smoke, 0.8f, 1.4f);
            ParticleFactory.Speed(smoke, 0.2f, 0.8f);
            ParticleFactory.Size(smoke, 0.35f, 0.7f);
            ParticleFactory.Rate(smoke, 14f);
            ParticleFactory.Colors(smoke, new Color(0.25f, 0.24f, 0.26f, 0.6f), new Color(0.4f, 0.38f, 0.38f, 0.45f));
            ParticleFactory.SizeOverLife(smoke, 0.6f, 1.6f);
            ParticleFactory.FadeOut(smoke);
            ParticleSystem.MainModule smokeMain = smoke.main;
            smokeMain.playOnAwake = false;
            smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var drones = new List<Drone>();
            for (int i = 0; i < 2; i++)
            {
                Transform droneRoot = Child(root.transform, $"Drone{i + 1}");
                var drone = droneRoot.gameObject.AddComponent<Drone>();
                Transform droneModel = Child(droneRoot, "Model", Vector3.zero, FaceCamera, Vector3.one * 1.1f);
                droneModel.gameObject.AddComponent<MeshFilter>().sharedMesh = AsteroidsArtBuilder.Model("Drone");
                var droneRenderer = droneModel.gameObject.AddComponent<MeshRenderer>();
                droneRenderer.sharedMaterial = M("Palette");
                droneRenderer.shadowCastingMode = ShadowCastingMode.Off;
                Quad(droneRoot, "Glow", GlowTint("DroneGlow", new Color(0.4f, 2f, 1.2f, 0.5f)), new Vector3(0f, 0f, 0.2f), Vector2.one * 1.1f);
                drone.model = droneModel;
                droneRoot.gameObject.SetActive(false);
                drones.Add(drone);
            }

            visuals.bank = bank;
            visuals.modelRoot = modelRoot;
            visuals.modelFilter = modelRoot.GetComponent<MeshFilter>();
            visuals.modelRenderer = modelRenderer;
            visuals.flames = flames.ToArray();
            visuals.exhausts = exhausts.ToArray();
            visuals.shieldBubble = shield;
            visuals.muzzle = muzzle;
            visuals.damageSmoke = smoke;
            player.visuals = visuals;
            player.drones = drones.ToArray();
            PlayerSettings hull = AsteroidsContentBuilder.Hull(0);
            if (hull != null)
            {
                AsteroidsAssets.SetAuto(player, nameof(AsteroidsPlayer.PlayerSettings), p => p.objectReferenceValue = hull);
                visuals.SetModel(hull);
            }
            Save(root, "Ship");
        }
    }
}
