using System;
using System.Collections.Generic;
using System.IO;
using Gamebox.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace Portfolio.Asteroids.EditorTools
{
    /// <summary>
    /// The strike prefabs (design 4.3): the seven air units (recoloured StarSparrow hulls, a procedural gunship with a
    /// spinning rotor), the nine bosses (a core with parts in a fixed order), the strike shots and the enemy shot kinds 5
    /// to 9, the strike pickups, the strike effects and the ship prefab's strike children. Every aircraft, air boss and
    /// the ship get a "Shadow" child: a flattened copy of their models in the shadow material, placed by
    /// <see cref="DropShadow"/>.
    /// </summary>
    internal static partial class AsteroidsPrefabBuilder
    {
        /// <summary>Turns a model built with +Y up and +Z forward so it faces the camera with its nose down the screen.</summary>
        /// (A property: the static fields of the other half of the class may not be initialised yet when this one would be.)
        private static Quaternion FaceDown => Quaternion.Euler(0f, 0f, 180f) * Quaternion.Euler(-90f, 0f, 0f);

        /// <summary>The strike pickups in the order they are appended to the spawner's reward pools.</summary>
        public static readonly string[] StrikePickupNames =
        {
            "CreditOrb", "SmallArms", "Isotopes", "Thaelite", "FusionCore", "FreyliumOre", "Energy", "MegaBomb", "PhaseShield",
            "IonScanner", "AirMissiles", "GroundMissiles", "Bombs", "Dumbfire", "PlasmaCannon"
        };

        /// <summary>The prefab name of an enemy shot kind of the strike mode (5 to 9).</summary>
        public static string EnemyShotName(EnemyShotKind kind)
        {
            return $"Enemy{kind}";
        }


        static partial void BuildStrikePrefabs()
        {
            Progress("Strike aircraft", 0.975f);
            BuildAircraft();
            Progress("Strike shots", 0.98f);
            BuildStrikeShots();
            Progress("Strike pickups", 0.985f);
            BuildStrikePickups();
            Progress("Strike effects", 0.99f);
            BuildStrikeEffects();
            Progress("Strike bosses", 0.993f);
            BuildStrikeBosses();
        }


        private static GameObject SaveStrike(GameObject root, string relative)
        {
            return Save(root, $"Strike/{relative}");
        }


        private static Material SM(string name)
        {
            return AsteroidsArtBuilder.StrikeMaterial(name);
        }


        /// <summary>An additive glow of <paramref name="color"/> under Art/Strike/Materials (like the field's GlowTint).</summary>
        private static Material StrikeTint(string name, Color color)
        {
            return AsteroidsArtBuilder.StrikeGlowMaterial(name, AsteroidsArtBuilder.Texture("Glow"), color, true);
        }


        /// <summary>The strike pickups (Prefabs/Strike/Pickups) in <see cref="StrikePickupNames"/> order, for the pools.</summary>
        public static List<StrikeReward> StrikePickups()
        {
            var rewards = new List<StrikeReward>();
            foreach (string name in StrikePickupNames)
            {
                var reward = Load<StrikeReward>($"Strike/Pickups/{name}");
                if (reward != null)
                {
                    rewards.Add(reward);
                }
            }
            return rewards;
        }


        // ------------------------------------------------------------------ shadows

        /// <summary>
        /// Adds the "Shadow" child: a <see cref="DropShadow"/> over "Flat" (squashed along the view to 2 percent), holding a
        /// copy of every lit model under <paramref name="root"/> at its rest pose, in the shadow material. The copy keeps the
        /// models' own rotations (FaceDown included), so the shadow only needs the body's turn about the view axis.
        /// </summary>
        private static DropShadow AddShadow(Transform root)
        {
            Transform shadowRoot = Child(root, "Shadow");
            var shadow = shadowRoot.gameObject.AddComponent<DropShadow>();
            Transform flat = Child(shadowRoot, "Flat", new Vector3(0f, 0f, -0.02f), Quaternion.identity, new Vector3(1f, 1f, 0.02f));
            var renderers = new List<Renderer>();
            Material material = SM("Shadow");
            foreach (Transform child in root)
            {
                if (child != shadowRoot)
                {
                    CopyShadow(child, flat, material, renderers);
                }
            }
            // The copy carries the models' rest rotations, so the shadow only takes the turn of the body's root (which is
            // what DropShadow copies when it has no visual of its own); the bank of the visual is left out.
            shadow.visual = null;
            shadow.renderers = renderers.ToArray();
            return shadow;
        }


        private static void CopyShadow(Transform source, Transform parent, Material material, List<Renderer> renderers)
        {
            if (source.name == "Shield" || source.GetComponent<ParticleSystem>() != null)
            {
                return;
            }
            Transform copy = Child(parent, source.name, source.localPosition, source.localRotation, source.localScale);
            var filter = source.GetComponent<MeshFilter>();
            var sourceRenderer = source.GetComponent<MeshRenderer>();
            if (filter != null && sourceRenderer != null && filter.sharedMesh != null && IsLit(sourceRenderer.sharedMaterial))
            {
                copy.gameObject.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                var renderer = copy.gameObject.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                renderers.Add(renderer);
            }
            foreach (Transform child in source)
            {
                CopyShadow(child, copy, material, renderers);
            }
            if (copy.childCount == 0 && copy.GetComponent<MeshRenderer>() == null)
            {
                Object.DestroyImmediate(copy.gameObject);
            }
        }


        /// <summary>Whether a material is a solid model's (URP Lit), not a glow, a shield or a particle.</summary>
        private static bool IsLit(Material material)
        {
            return material != null && material.shader != null && material.shader.name == "Universal Render Pipeline/Lit";
        }


        // ------------------------------------------------------------------ aircraft

        /// <summary>A StarSparrow hull <paramref name="length"/> long under <paramref name="visual"/> (the Dreadnought's pattern).</summary>
        private static MeshRenderer PackHull(Transform visual, string model, Material material, float length)
        {
            Mesh mesh = AsteroidsArtBuilder.ShipMesh(model);
            float scale = length / Mathf.Max(0.01f, mesh != null ? mesh.bounds.size.z : 1f);
            return Model(visual, "Hull", mesh, material, -(mesh != null ? mesh.bounds.center : Vector3.zero) * scale, Quaternion.identity, Vector3.one * scale);
        }


        /// <summary>A red engine flame behind an aircraft that faces down the screen (it points up, away from the nose).</summary>
        private static void EngineFlame(Transform root, string name, Vector2 at, float size)
        {
            Model(root, name, AsteroidsArtBuilder.Model("FlameQuad"), SM("EngineRed"), new Vector3(at.x, at.y, 0.1f), Quaternion.Euler(0f, 0f, 180f),
                new Vector3(0.34f * size, size, 1f));
        }


        private static void BuildAircraft()
        {
            Aircraft(StrikeUnit.Dart, new Color(1f, 0.5f, 0.35f), visual => PackHull(visual, "StarSparrow13", SM("AirCrimson"), 2.1f),
                new[] { new Vector2(0f, -1.1f) }, new[] { new Vector2(0f, 1f) }, 0.8f);
            Aircraft(StrikeUnit.Hornet, new Color(1f, 0.6f, 0.25f), visual => PackHull(visual, "StarSparrow9", SM("AirBlack"), 2.4f),
                new[] { new Vector2(-0.55f, -0.9f), new Vector2(0.55f, -0.9f) }, new[] { new Vector2(0f, 1.15f) }, 0.9f);
            Aircraft(StrikeUnit.Bomber, new Color(1f, 0.65f, 0.3f), visual => PackHull(visual, "StarSparrow5", SM("AirSteel"), 4.8f),
                new[] { new Vector2(-1.3f, -1.4f), new Vector2(1.3f, -1.4f) }, new[] { new Vector2(-1.35f, 2.1f), new Vector2(1.35f, 2.1f), new Vector2(0f, 2.35f) }, 1.4f);
            Aircraft(StrikeUnit.Gunship, new Color(1f, 0.55f, 0.3f), visual =>
                Model(visual, "Body", AsteroidsArtBuilder.StrikeModel("Gunship"), SM("StrikeSand")),
                new[] { new Vector2(-1.05f, -0.9f), new Vector2(1.05f, -0.9f) }, new Vector2[0], 0f);
            Aircraft(StrikeUnit.Interceptor, new Color(1f, 0.4f, 0.35f), visual => PackHull(visual, "StarSparrow3", SM("AirViolet"), 2.5f),
                new[] { new Vector2(-0.45f, -1f), new Vector2(0.45f, -1f) }, new[] { new Vector2(0f, 1.3f) }, 1f);
            Aircraft(StrikeUnit.Kamikaze, new Color(1f, 0.4f, 0.9f), visual => PackHull(visual, "StarSparrow1", SM("AirRust"), 1.8f),
                new Vector2[0], new[] { new Vector2(0f, 0.95f) }, 0.9f);
            Aircraft(StrikeUnit.Transport, new Color(1f, 0.7f, 0.35f), visual => PackHull(visual, "StarSparrow10", SM("AirOlive"), 6.8f),
                new[] { new Vector2(0f, -3f) }, new[] { new Vector2(-2.1f, 3f), new Vector2(2.1f, 3f), new Vector2(0f, 3.3f) }, 1.6f);
        }


        /// <summary>
        /// An air unit: root (<see cref="StrikeAircraft"/>, numbers from <see cref="StrikeUnitRules"/>) with "Visual" (the model
        /// facing down the screen), "Barrel{n}" points, engine flames, a rotor for the gunship and the "Shadow".
        /// </summary>
        private static void Aircraft(StrikeUnit unit, Color tint, Func<Transform, MeshRenderer> model, Vector2[] barrels,
            Vector2[] engines, float flameSize)
        {
            StrikeUnitInfo info = StrikeUnitRules.Info(unit);
            var root = new GameObject(unit.ToString());
            var aircraft = root.AddComponent<StrikeAircraft>();
            Transform visual = Child(root.transform, "Visual", Vector3.zero, FaceDown);
            model(visual);
            var barrelList = new List<Transform>();
            for (int i = 0; i < barrels.Length; i++)
            {
                barrelList.Add(Child(root.transform, $"Barrel{i + 1}", barrels[i]));
            }
            for (int i = 0; i < engines.Length; i++)
            {
                EngineFlame(root.transform, $"Engine{i + 1}", engines[i], flameSize);
            }
            if (unit == StrikeUnit.Gunship)
            {
                Rotor(root.transform, "Rotor", new Vector3(0f, 0.2f, -1.3f), 4.4f, 1400f);
            }
            aircraft.unit = unit;
            aircraft.visual = visual;
            aircraft.flashRenderers = Renderers(visual);
            aircraft.flashColor = new Color(1f, 0.9f, 0.8f);
            aircraft.radius = info.Radius;
            aircraft.maxHealth = info.Health;
            aircraft.score = info.Bounty;
            aircraft.contactDamage = 0f;
            aircraft.altitude = Altitude.Air;
            aircraft.wraps = false;
            aircraft.pulledByGravity = false;
            aircraft.baseSpeed = info.Speed;
            aircraft.pattern = info.Pattern;
            aircraft.burst = Mathf.Max(1, info.Burst);
            aircraft.burstGap = info.BurstGap;
            aircraft.interval = info.Interval;
            aircraft.barrels = barrelList.ToArray();
            aircraft.turnRate = unit == StrikeUnit.Transport || unit == StrikeUnit.Bomber ? 90f : unit == StrikeUnit.Kamikaze ? 540f : 300f;
            aircraft.bankAmount = unit == StrikeUnit.Gunship ? 0.08f : 0.2f;
            aircraft.shotKind = info.ShotKind;
            aircraft.fireInterval = info.Interval;
            aircraft.shotSpeed = StrikeUnitRules.Shot(info.ShotKind).Speed;
            aircraft.aimError = 4f;
            aircraft.explosionTint = tint;
            aircraft.explosionScale = info.ExplosionScale;
            AddShadow(root.transform);
            SaveStrike(root, $"Air/{unit}");
        }


        /// <summary>A spinning rotor: one particle with the rotor texture, turning at <paramref name="degreesPerSecond"/>, that never dies.</summary>
        private static ParticleSystem Rotor(Transform parent, string name, Vector3 position, float size, float degreesPerSecond)
        {
            ParticleSystem rotor = ParticleFactory.Create(name, parent, SM("Rotor"));
            rotor.transform.localPosition = position;
            ParticleSystem.MainModule main = rotor.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.loop = true;
            main.prewarm = false;
            main.duration = 1000f;
            main.startLifetime = 100000f;
            main.startSpeed = 0f;
            main.startSize = size;
            main.startRotation = 0f;
            main.maxParticles = 1;
            ParticleSystem.EmissionModule emission = rotor.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });
            ParticleSystem.ShapeModule shape = rotor.shape;
            shape.enabled = false;
            ParticleSystem.RotationOverLifetimeModule rotation = rotor.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(degreesPerSecond * Mathf.Deg2Rad);
            return rotor;
        }


        // ------------------------------------------------------------------ shots

        private static void BuildStrikeShots()
        {
            // The pilot's projectiles, one per StrikeShotKind (the pools are indexed by the kind).
            StrikeWeaponInfo gun = StrikeWeaponRules.Info(StrikeItem.MachineGun);
            StrikeBolt(StrikeShotKind.Bullet, SM("Bullet"), new Vector2(0.3f, 1.1f), gun.ShotRadius, gun.Lifetime, gun.Damage, gun.Mask, gun, new Color(1f, 0.85f, 0.4f));
            StrikeWeaponInfo plasma = StrikeWeaponRules.Info(StrikeItem.PlasmaCannon);
            StrikeBolt(StrikeShotKind.PlasmaBolt, SM("PlasmaBolt"), new Vector2(0.9f, 0.9f), 0.35f, plasma.Lifetime, plasma.Damage, plasma.Mask, plasma, new Color(0.8f, 0.4f, 1f),
                trail: new Color(0.7f, 0.35f, 1f));
            StrikeMissile(StrikeShotKind.MicroMissile, StrikeItem.MicroMissiles, 0.11f, new Color(0.5f, 1f, 0.5f), false, 0.2f);
            StrikeMissile(StrikeShotKind.Dumbfire, StrikeItem.Dumbfire, 0.16f, new Color(1f, 0.6f, 0.2f), true, 0.25f);
            StrikeWeaponInfo mini = StrikeWeaponRules.Info(StrikeItem.MiniGun);
            StrikeBolt(StrikeShotKind.MiniGunRound, SM("MiniGunRound"), new Vector2(0.18f, 0.75f), mini.ShotRadius, mini.Lifetime, mini.Damage, mini.Mask, mini, new Color(1f, 0.6f, 0.3f));
            StrikeMissile(StrikeShotKind.PodMissile, StrikeItem.MissilePods, 0.13f, new Color(0.45f, 0.8f, 1f), true, 0.2f);
            StrikeMissile(StrikeShotKind.AirMissile, StrikeItem.AirMissiles, 0.15f, new Color(0.5f, 0.85f, 1f), true, 0.22f);
            StrikeMissile(StrikeShotKind.GroundMissile, StrikeItem.GroundMissiles, 0.24f, new Color(1f, 0.55f, 0.2f), false, 0.3f);
            StrikeBomb();
            StrikeWeaponInfo disrupter = StrikeWeaponRules.Info(StrikeItem.PowerDisrupter);
            StrikeBolt(StrikeShotKind.DisrupterOrb, SM("DisrupterOrb"), new Vector2(0.8f, 0.8f), 0.3f, 1.5f, 1f, Altitude.Both, disrupter, new Color(0.7f, 0.5f, 1f));
            StrikeWeaponInfo pulse = StrikeWeaponRules.Info(StrikeItem.PulseCannon);
            StrikeBolt(StrikeShotKind.Pulse, SM("Pulse"), new Vector2(2.2f, 1.1f), pulse.ShotRadius, pulse.Lifetime, pulse.Damage, pulse.Mask, pulse, new Color(0.4f, 1f, 0.9f));

            // The enemy shot kinds of the strike mode (5 to 9): the damage lives on the prefab (guests only know the kind).
            EnemyBolt(EnemyShotKind.Flak, SM("EnemyFlak"), new Vector2(0.62f, 0.62f), 0.18f, false, new Color(1f, 0.6f, 0.2f));
            EnemyBolt(EnemyShotKind.Bolt, SM("EnemyBolt"), new Vector2(0.34f, 1.05f), 0.2f, true, new Color(1f, 0.35f, 0.3f));
            EnemyRocket();
            EnemyLaser();
            EnemySkyMine();
        }


        /// <summary>A glowing projectile of the pilot's (bullet, tracer, plasma bolt, pulse) under Prefabs/Strike/Shots/{kind}.</summary>
        private static void StrikeBolt(StrikeShotKind kind, Material material, Vector2 size, float radius, float lifetime, float damage, Altitude reach,
            StrikeWeaponInfo info, Color impact, Color? trail = null)
        {
            var root = new GameObject(kind.ToString());
            var shot = root.AddComponent<Shot>();
            Transform visual = Child(root.transform, "Visual");
            Quad(visual, "Glow", material, Vector3.zero, size);
            shot.visual = visual;
            shot.enemy = false;
            shot.damage = damage;
            shot.pierce = 1;
            shot.radius = radius;
            shot.lifetime = lifetime;
            shot.alignToVelocity = true;
            shot.impactTint = impact;
            shot.reach = reach;
            shot.acceleration = info.Acceleration;
            shot.maxSpeed = info.MaxSpeed > info.Speed ? info.MaxSpeed : 0f;
            shot.wraps = false;
            shot.pulledByGravity = false;
            shot.altitude = Altitude.Air;
            if (trail.HasValue)
            {
                AddTrail(root, shot, trail.Value, size.x * 0.4f, 0.1f);
            }
            SaveStrike(root, $"Shots/{kind}");
        }


        private static void AddTrail(GameObject root, Shot shot, Color color, float width, float time)
        {
            var line = root.AddComponent<TrailRenderer>();
            line.sharedMaterial = M("ParticleAdd");
            line.time = time;
            line.startWidth = width;
            line.endWidth = 0f;
            line.minVertexDistance = 0.1f;
            line.startColor = color * 1.6f;
            line.endColor = new Color(color.r, color.g, color.b, 0f);
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.alignment = LineAlignment.View;
            shot.trail = line;
        }


        /// <summary>A missile of the pilot's: the pack missile at <paramref name="scale"/>, a flame and a flame or smoke exhaust.</summary>
        private static void StrikeMissile(StrikeShotKind kind, StrikeItem item, float scale, Color tint, bool smoke, float radius)
        {
            StrikeWeaponInfo info = StrikeWeaponRules.Info(item);
            GameObject root = MissileBody(kind.ToString(), scale, tint, smoke || info.Smoke, out Shot shot);
            shot.enemy = false;
            shot.damage = info.Damage;
            shot.radius = radius;
            shot.lifetime = info.Lifetime;
            shot.reach = info.Mask;
            shot.acceleration = info.Acceleration;
            shot.maxSpeed = info.MaxSpeed > info.Speed ? info.MaxSpeed : 0f;
            shot.impactTint = tint;
            SaveStrike(root, $"Shots/{kind}");
        }


        /// <summary>The body of a missile: the pack's "MissileViking" pointing up the screen, a glow at its tail and an exhaust.</summary>
        private static GameObject MissileBody(string name, float scale, Color tint, bool smoke, out Shot shot)
        {
            var root = new GameObject(name);
            shot = root.AddComponent<Shot>();
            Transform visual = Child(root.transform, "Visual");
            Mesh mesh = AsteroidsArtBuilder.PackMesh("BonusContent/MissilesSample.FBX", "MissileViking");
            float length = mesh != null ? mesh.bounds.size.z * scale : 1f;
            Model(visual, "Model", mesh, AsteroidsArtBuilder.PackMaterial("BonusContent/Missile"), -(FaceCamera * (mesh != null ? mesh.bounds.center : Vector3.zero)) * scale,
                FaceCamera, Vector3.one * scale);
            Quad(visual, "Glow", SM("MissileFlame"), new Vector3(0f, -length * 0.55f, -0.1f), Vector2.one * Mathf.Max(0.35f, length * 0.6f));
            ParticleSystem exhaust = ParticleFactory.Create("Exhaust", root.transform, smoke ? M("ParticleSmoke") : M("ParticleAdd"));
            exhaust.transform.localPosition = new Vector3(0f, -length * 0.5f, 0f);
            ParticleFactory.Lifetime(exhaust, smoke ? 0.45f : 0.2f, smoke ? 0.8f : 0.35f);
            ParticleFactory.Speed(exhaust, 0f, 0.3f);
            ParticleFactory.Size(exhaust, smoke ? 0.25f : 0.18f, smoke ? 0.45f : 0.3f);
            ParticleFactory.Rate(exhaust, smoke ? 45f : 50f);
            ParticleFactory.SizeOverLife(exhaust, smoke ? 0.6f : 1f, smoke ? 2f : 0.2f);
            if (smoke)
            {
                ParticleFactory.Colors(exhaust, new Color(0.75f, 0.75f, 0.75f, 0.55f), new Color(0.55f, 0.55f, 0.58f, 0.45f));
            }
            else
            {
                ParticleFactory.Colors(exhaust, tint, Color.white);
            }
            ParticleFactory.FadeOut(exhaust);
            ParticleFactory.MaxParticles(exhaust, 80);
            shot.visual = visual;
            shot.exhaust = exhaust;
            shot.pierce = 1;
            shot.alignToVelocity = true;
            shot.wraps = false;
            shot.pulledByGravity = false;
            shot.altitude = Altitude.Air;
            return root;
        }


        /// <summary>The MK-133 bomb: it reaches nothing while it falls, then bursts on the ground when it expires.</summary>
        private static void StrikeBomb()
        {
            StrikeWeaponInfo info = StrikeWeaponRules.Info(StrikeItem.Bombs);
            var root = new GameObject(StrikeShotKind.Bomb.ToString());
            var shot = root.AddComponent<Shot>();
            Transform visual = Child(root.transform, "Visual");
            Mesh mesh = AsteroidsArtBuilder.PackMesh("BonusContent/MissilesSample.FBX", "MissileBomb");
            float scale = 0.9f / Mathf.Max(0.01f, mesh != null ? mesh.bounds.size.z : 1f);
            Model(visual, "Model", mesh, AsteroidsArtBuilder.PackMaterial("BonusContent/Missile"), -(FaceCamera * (mesh != null ? mesh.bounds.center : Vector3.zero)) * scale,
                FaceCamera, Vector3.one * scale);
            Quad(visual, "Glow", StrikeTint("StrikeBombGlow", new Color(2f, 0.9f, 0.3f, 0.35f)), new Vector3(0f, 0f, 0.2f), Vector2.one * 1.1f);
            shot.visual = visual;
            shot.enemy = false;
            shot.damage = info.BurstDamage;
            shot.pierce = 1;
            shot.radius = 0.3f;
            shot.lifetime = info.Lifetime;
            shot.alignToVelocity = true;
            shot.impactTint = new Color(1f, 0.6f, 0.25f);
            shot.reach = Altitude.None;
            shot.burstOnExpire = true;
            shot.burstRadius = info.BurstRadius;
            shot.burstDamage = info.BurstDamage;
            shot.wraps = false;
            shot.pulledByGravity = false;
            shot.altitude = Altitude.Air;
            SaveStrike(root, $"Shots/{StrikeShotKind.Bomb}");
        }


        private static void EnemyShotNumbers(Shot shot, EnemyShotKind kind)
        {
            EnemyShotInfo info = StrikeUnitRules.Shot(kind);
            shot.enemy = true;
            shot.damage = info.Damage;
            shot.pierce = 1;
            shot.lifetime = info.Lifetime > 0f ? info.Lifetime : 4f;
            shot.acceleration = info.Acceleration;
            shot.maxSpeed = info.MaxSpeed > info.Speed ? info.MaxSpeed : 0f;
            shot.wobble = info.Wobble;
            shot.reach = Altitude.Both;
            shot.wraps = false;
            shot.pulledByGravity = false;
            shot.altitude = Altitude.Air;
        }


        private static void EnemyBolt(EnemyShotKind kind, Material material, Vector2 size, float radius, bool align, Color impact)
        {
            var root = new GameObject(EnemyShotName(kind));
            var shot = root.AddComponent<Shot>();
            Transform visual = Child(root.transform, "Visual");
            Quad(visual, "Glow", material, Vector3.zero, size);
            shot.visual = visual;
            EnemyShotNumbers(shot, kind);
            shot.radius = radius;
            shot.alignToVelocity = align;
            shot.impactTint = impact;
            if (!align)
            {
                shot.Spin = new Vector3(0f, 0f, 300f);
            }
            SaveStrike(root, $"Shots/{EnemyShotName(kind)}");
        }


        private static void EnemyRocket()
        {
            GameObject root = MissileBody(EnemyShotName(EnemyShotKind.Rocket), 0.16f, new Color(1f, 0.45f, 0.2f), true, out Shot shot);
            EnemyShotNumbers(shot, EnemyShotKind.Rocket);
            shot.radius = 0.22f;
            shot.impactTint = new Color(1f, 0.45f, 0.2f);
            Transform glow = root.transform.Find("Visual/Glow");
            if (glow != null)
            {
                glow.GetComponent<MeshRenderer>().sharedMaterial = SM("EnemyRocketFlame");
            }
            SaveStrike(root, $"Shots/{EnemyShotName(EnemyShotKind.Rocket)}");
        }


        /// <summary>
        /// The laser tower's beam: an <see cref="EnemyBeam"/> whose visual runs from its gun down past the bottom of the
        /// screen (40 m), with a flare at the gun.
        /// </summary>
        private static void EnemyLaser()
        {
            var root = new GameObject(EnemyShotName(EnemyShotKind.Beam));
            var beam = root.AddComponent<EnemyBeam>();
            Transform visual = Child(root.transform, "Visual");
            Quad(visual, "Beam", SM("EnemyBeam"), new Vector3(0f, -20f, 0f), new Vector2(1.1f, 40f));
            Quad(visual, "Flare", StrikeTint("StrikeBeamFlare", new Color(3f, 0.6f, 0.5f, 0.9f)), new Vector3(0f, 0f, -0.1f), Vector2.one * 1.8f);
            beam.visual = visual;
            EnemyShotNumbers(beam, EnemyShotKind.Beam);
            beam.radius = 0.35f;
            beam.halfWidth = 0.35f;
            beam.burnTime = 0.17f;
            beam.alignToVelocity = false;
            beam.impactTint = new Color(1f, 0.35f, 0.3f);
            SaveStrike(root, $"Shots/{EnemyShotName(EnemyShotKind.Beam)}");
        }


        /// <summary>The sky mine: a spiked pack mine that drifts down with a wobble and a pulsing red light.</summary>
        private static void EnemySkyMine()
        {
            var root = new GameObject(EnemyShotName(EnemyShotKind.SkyMine));
            var shot = root.AddComponent<Shot>();
            Transform visual = Child(root.transform, "Visual");
            Mesh mesh = AsteroidsArtBuilder.PackMesh("BonusContent/MinesSample.FBX", "SpaceMine1");
            const float scale = 0.42f;
            Model(visual, "Model", mesh, AsteroidsArtBuilder.PackMaterial("BonusContent/Mine Sample 2"), -(mesh != null ? mesh.bounds.center : Vector3.zero) * scale,
                Quaternion.identity, Vector3.one * scale);
            Quad(root.transform, "Glow", SM("EnemyMineGlow"), new Vector3(0f, 0f, -0.8f), Vector2.one * 1.6f);
            shot.visual = visual;
            EnemyShotNumbers(shot, EnemyShotKind.SkyMine);
            shot.radius = 0.5f;
            shot.alignToVelocity = false;
            shot.Spin = new Vector3(0f, 0f, 60f);
            shot.impactTint = new Color(1f, 0.35f, 0.3f);
            SaveStrike(root, $"Shots/{EnemyShotName(EnemyShotKind.SkyMine)}");
        }


        // ------------------------------------------------------------------ pickups

        private static void BuildStrikePickups()
        {
            MoneyPickup("CreditOrb", "Credits", StrikeRules.CreditOrb, "CreditCoin", new Color(1f, 0.85f, 0.3f), 0.8f, 0.4f);
            MoneyPickup("SmallArms", "Small Arms", StrikeRules.SmallArms, "AmmoCrate", new Color(1f, 0.85f, 0.35f), 1f, 0.6f);
            MoneyPickup("Isotopes", "Isotopes", StrikeRules.Isotopes, "IsotopeCanister", new Color(0.4f, 1f, 0.4f), 0.95f, 0.6f);
            MoneyPickup("Thaelite", "Thaelite", StrikeRules.Thaelite, "ThaeliteCluster", new Color(0.4f, 0.7f, 1f), 1.05f, 0.6f);
            MoneyPickup("FusionCore", "Fusion Core", StrikeRules.FusionCore, "FusionCore", new Color(1f, 0.6f, 0.2f), 1f, 0.6f);
            MoneyPickup("FreyliumOre", "Freylium Ore", StrikeRules.FreyliumOre, "FreyliumOre", new Color(0.95f, 0.45f, 1f), 1.05f, 0.65f);
            ItemPickup("Energy", StrikeItem.EnergyModule, "Energy", new Color(0.4f, 1f, 0.5f), StrikeRules.EnergyPickup);
            ItemPickup("MegaBomb", StrikeItem.MegaBomb, StrikeItem.MegaBomb.ToString(), new Color(1f, 0.75f, 0.4f), 0);
            ItemPickup("PhaseShield", StrikeItem.PhaseShield, StrikeItem.PhaseShield.ToString(), new Color(0.65f, 0.5f, 1f), 0);
            ItemPickup("IonScanner", StrikeItem.IonScanner, StrikeItem.IonScanner.ToString(), new Color(0.4f, 0.85f, 1f), 0);
            ItemPickup("AirMissiles", StrikeItem.AirMissiles, StrikeItem.AirMissiles.ToString(), new Color(0.4f, 0.8f, 1f), 0);
            ItemPickup("GroundMissiles", StrikeItem.GroundMissiles, StrikeItem.GroundMissiles.ToString(), new Color(1f, 0.6f, 0.25f), 0);
            ItemPickup("Bombs", StrikeItem.Bombs, StrikeItem.Bombs.ToString(), new Color(1f, 0.55f, 0.2f), 0);
            ItemPickup("Dumbfire", StrikeItem.Dumbfire, StrikeItem.Dumbfire.ToString(), new Color(0.45f, 1f, 0.5f), 0);
            ItemPickup("PlasmaCannon", StrikeItem.PlasmaCannon, StrikeItem.PlasmaCannon.ToString(), new Color(0.8f, 0.45f, 1f), 0);
        }


        /// <summary>A money pickup: a glowing model (crate, canister, crystals, core, ore, coin) turning over a halo of its colour.</summary>
        private static void MoneyPickup(string name, string title, int money, string model, Color color, float size, float radius)
        {
            var root = new GameObject(name);
            var reward = root.AddComponent<StrikeReward>();
            Transform visual = Child(root.transform, "Visual");
            Transform tilt = Child(visual, "Tilt", Vector3.zero, Quaternion.Euler(-60f, 0f, 0f), Vector3.one * size);
            Mesh mesh = AsteroidsArtBuilder.StrikeModel(model);
            float lift = mesh != null ? -mesh.bounds.center.y : 0f;
            MeshRenderer body = Model(tilt, "Model", mesh, SM("StrikePalette"), new Vector3(0f, lift, 0f));
            MeshRenderer halo = Quad(root.transform, "Halo", StrikeTint($"StrikeHalo{name}", new Color(color.r * 2f, color.g * 2f, color.b * 2f, 0.55f)), new Vector3(0f, 0f, 0.4f),
                Vector2.one * (1.6f + size * 0.6f));
            reward.visual = visual;
            reward.blinkRenderers = new Renderer[] { body, halo };
            reward.title = money == StrikeRules.CreditOrb ? $"+{money}" : title;
            reward.color = color;
            reward.radius = radius;
            reward.lifetime = 0f;
            reward.Spin = new Vector3(0f, 0f, money == StrikeRules.CreditOrb ? 160f : 60f);
            reward.attractRange = 2.4f;
            reward.bobHeight = 0.1f;
            reward.item = StrikeItem.MachineGun;
            reward.money = money;
            reward.energy = 0;
            reward.altitude = Altitude.Ground;
            reward.wraps = false;
            reward.pulledByGravity = false;
            SaveStrike(root, $"Pickups/{name}");
        }


        /// <summary>An item pickup in the style of the field's: the hexagonal frame, a glowing core of its colour and its icon.</summary>
        private static void ItemPickup(string name, StrikeItem item, string icon, Color color, int energy)
        {
            var root = new GameObject(name);
            var reward = root.AddComponent<StrikeReward>();
            Transform visual = Child(root.transform, "Visual");
            Transform frameRoot = Child(visual, "Frame", Vector3.zero, FaceCamera);
            MeshRenderer frame = Model(frameRoot, "Model", AsteroidsArtBuilder.Model("PickupFrame"), M("Palette"), Vector3.zero, Quaternion.identity, Vector3.one * 1.15f);
            MeshRenderer core = Quad(root.transform, "Core", StrikeTint($"StrikeCore{name}", new Color(color.r * 2.2f, color.g * 2.2f, color.b * 2.2f, 0.85f)), new Vector3(0f, 0f, 0.1f),
                Vector2.one * 1.8f);
            MeshRenderer symbol = Quad(root.transform, "Icon", SM($"Icon{icon}"), new Vector3(0f, 0f, -0.35f), Vector2.one * 0.9f);
            reward.visual = visual;
            reward.blinkRenderers = new Renderer[] { frame, core, symbol };
            reward.title = item == StrikeItem.EnergyModule ? "Energy" : StrikeArmory.Title(item);
            reward.color = color;
            reward.radius = 0.7f;
            reward.lifetime = 0f;
            reward.Spin = new Vector3(0f, 0f, 70f);
            reward.attractRange = 2.4f;
            reward.bobHeight = 0.12f;
            reward.item = item;
            reward.money = 0;
            reward.energy = energy;
            reward.altitude = Altitude.Ground;
            reward.wraps = false;
            reward.pulledByGravity = false;
            SaveStrike(root, $"Pickups/{name}");
        }


        // ------------------------------------------------------------------ effects

        private static void BuildStrikeEffects()
        {
            // Ground explosion: a fireball, dirt and rubble thrown up, dust and a scorched shock ring; everything moves with
            // the effect (local simulation), which drifts down with the scroll.
            {
                PooledEffect effect = Effect("GroundExplosion", 2.2f, out Transform root);
                ParticleSystem flare = Burst(root, "Flare", M("ParticleFlare"), 1, 0.12f, 0.16f, 0f, 0f, 3.6f, 4.2f, Color.white, Color.white);
                ParticleFactory.SizeOverLife(flare, 1f, 0.3f);
                ParticleSystem fire = Burst(root, "Fire", M("ParticleAdd"), 26, 0.35f, 0.8f, 1.2f, 4.5f, 0.8f, 1.7f, Color.white, Color.white);
                Flat(fire);
                ParticleFactory.SizeOverLife(fire, 1f, 0.2f);
                ColorOverLife(fire, new Color(1f, 0.92f, 0.65f), new Color(1f, 0.5f, 0.12f), new Color(0.45f, 0.1f, 0.05f));
                ParticleSystem dirt = Burst(root, "Dirt", M("Debris"), 14, 0.6f, 1.2f, 2f, 6.5f, 0.12f, 0.3f, new Color(0.55f, 0.45f, 0.35f), new Color(0.35f, 0.28f, 0.22f));
                Flat(dirt);
                ParticleFactory.Tumble(dirt, 500f);
                ParticleFactory.MeshParticles(dirt, AsteroidsArtBuilder.Model("Rock1"));
                ParticleFactory.SizeOverLife(dirt, 1f, 0.5f);
                ParticleSystem smoke = Burst(root, "Smoke", M("ParticleSmoke"), 10, 1.1f, 2f, 0.3f, 1.3f, 1.4f, 2.6f, new Color(0.16f, 0.14f, 0.13f, 0.75f), new Color(0.3f, 0.26f, 0.22f, 0.6f));
                Flat(smoke);
                ParticleFactory.SizeOverLife(smoke, 0.6f, 1.7f);
                ParticleFactory.FadeOut(smoke);
                ParticleFactory.Spin(smoke, 50f);
                ParticleSystem sparks = Burst(root, "Sparks", M("ParticleSpark"), 14, 0.3f, 0.6f, 5f, 11f, 0.12f, 0.22f, Color.white, Color.white);
                Flat(sparks);
                Stretch(sparks, 0.06f);
                ParticleFactory.FadeOut(sparks);
                foreach (ParticleSystem system in new[] { flare, fire, dirt, smoke, sparks })
                {
                    ParticleSystem.MainModule main = system.main;
                    main.simulationSpace = ParticleSystemSimulationSpace.Local;
                }
                Flash(root, effect, 10f, 7f, 0.35f);
                Ring(root, effect, 0.4f, 3.6f);
                effect.systems = new[] { flare, fire, dirt, smoke, sparks };
                effect.tinted = new[] { sparks };
                SaveStrike(root.gameObject, "Effects/GroundExplosion");
            }

            // Crater: the decal quad (1 m) the terrain puts on the tile under a destroyed ground unit; the theme's crater
            // material replaces this one.
            {
                var root = new GameObject("Crater");
                Quad(root.transform, "Decal", SM("Crater"), Vector3.zero, Vector2.one);
                SaveStrike(root, "Effects/Crater");
            }

            // Megabomb: a white flash over the whole screen, a huge ring and streaks from the centre.
            {
                PooledEffect effect = Effect("MegabombFlash", 1.6f, out Transform root);
                ParticleSystem flash = Burst(root, "Screen", SM("MegabombFlash"), 1, 0.7f, 0.75f, 0f, 0f, 110f, 110f, Color.white, Color.white);
                ParticleFactory.FadeOut(flash);
                ParticleSystem.MainModule flashMain = flash.main;
                flashMain.simulationSpace = ParticleSystemSimulationSpace.Local;
                flash.transform.localPosition = new Vector3(0f, 0f, -3f);
                ParticleSystem streaks = Burst(root, "Streaks", M("ParticleSpark"), 120, 0.5f, 1f, 20f, 40f, 0.25f, 0.4f, new Color(1f, 0.95f, 0.8f), Color.white);
                Flat(streaks);
                Stretch(streaks, 0.05f);
                ParticleFactory.FadeOut(streaks);
                Flash(root, effect, 60f, 9f, 0.8f);
                Ring(root, effect, 1f, 70f);
                effect.systems = new[] { flash, streaks };
                effect.tinted = new ParticleSystem[0];
                effect.scaleSpeed = false;
                SaveStrike(root.gameObject, "Effects/MegabombFlash");
            }

            // Muzzle flash: a quick flare and a few sparks.
            {
                PooledEffect effect = Effect("Muzzle", 0.25f, out Transform root);
                ParticleSystem flare = Burst(root, "Flare", M("ParticleFlare"), 1, 0.06f, 0.08f, 0f, 0f, 0.9f, 1.1f, Color.white, Color.white);
                ParticleFactory.SizeOverLife(flare, 1f, 0.2f);
                ParticleSystem sparks = Burst(root, "Sparks", M("ParticleSpark"), 4, 0.08f, 0.15f, 5f, 9f, 0.06f, 0.1f, Color.white, Color.white);
                ParticleFactory.Cone(sparks, 25f, 0.05f);
                Stretch(sparks, 0.04f);
                effect.systems = new[] { flare, sparks };
                effect.tinted = new[] { flare, sparks };
                effect.scaleSpeed = false;
                SaveStrike(root.gameObject, "Effects/Muzzle");
            }
        }


        // ------------------------------------------------------------------ the ship

        /// <summary>
        /// Adds the strike children to the ship <see cref="BuildShip"/> is building, before its one save: the "Shadow" (a
        /// flattened copy of the hull model), the <see cref="PlayerBeam"/> with its beam and zap lines, and a disabled
        /// <see cref="StrikeAutopilot"/>.
        /// </summary>
        static partial void AddShipStrikeParts(GameObject root)
        {
            Transform model = root.transform.Find("Bank/Model");
            Transform shadowRoot = Child(root.transform, "Shadow");
            var shadow = shadowRoot.gameObject.AddComponent<DropShadow>();
            Transform flat = Child(shadowRoot, "Flat", new Vector3(0f, 0f, -0.02f), Quaternion.identity, new Vector3(1f, 1f, 0.02f));
            var renderers = new List<Renderer>();
            if (model != null)
            {
                MeshFilter source = model.GetComponent<MeshFilter>();
                MeshRenderer copy = Model(flat, "Model", source != null ? source.sharedMesh : null, SM("Shadow"), model.localPosition, model.localRotation, model.localScale);
                renderers.Add(copy);
            }
            // The copy has the model's rest rotation: the shadow takes the ship root's turn only.
            shadow.visual = null;
            shadow.renderers = renderers.ToArray();

            // "PlayerBeam": the deathray and twin laser lines, the laser turret's zap, hot spots where the beams hit
            // and a flare at the gun (the pilot's PlayerBeam places and shows them).
            Transform beamRoot = Child(root.transform, "PlayerBeam");
            var beams = beamRoot.gameObject.AddComponent<PlayerBeam>();
            beams.beams = new[]
            {
                BeamLine(beamRoot, "Beam0", SM("PlayerBeam"), 2),
                BeamLine(beamRoot, "Beam1", SM("PlayerBeamTwin"), 2)
            };
            beams.zap = BeamLine(beamRoot, "Zap", SM("PlayerZap"), 5);
            beams.zapTime = 0.08f;
            // Written by name: these fields belong to the pilot's PlayerBeam and may not exist in an older copy of it.
            Transform tip0 = BeamGlow(beamRoot, "Tip0", StrikeTint("StrikeBeamTip", new Color(1.4f, 2.4f, 3.2f, 0.9f)), 1.4f);
            Transform tip1 = BeamGlow(beamRoot, "Tip1", StrikeTint("StrikeBeamTipTwin", new Color(3f, 1.2f, 3f, 0.9f)), 1.2f);
            Transform flare = BeamGlow(beamRoot, "Flare", StrikeTint("StrikeBeamFlareGun", new Color(1.6f, 2.4f, 3f, 0.8f)), 1.3f);
            SetIfPresent(beams, "tips", p =>
            {
                p.arraySize = 2;
                p.GetArrayElementAtIndex(0).objectReferenceValue = tip0;
                p.GetArrayElementAtIndex(1).objectReferenceValue = tip1;
            });
            SetIfPresent(beams, "flare", p => p.objectReferenceValue = flare);
            SetIfPresent(beams, "beamWidth", p => p.floatValue = 0.6f);
            SetIfPresent(beams, "zapWidth", p => p.floatValue = 0.2f);
            SetIfPresent(beams, "depth", p => p.floatValue = -0.1f);

            root.AddComponent<StrikeAutopilot>().enabled = false;
        }


        /// <summary>
        /// A line of <paramref name="points"/> points in world space, hidden, facing the camera, one meter wide (PlayerBeam
        /// sets its width): a beam or the zap of <see cref="PlayerBeam"/>.
        /// </summary>
        private static LineRenderer BeamLine(Transform parent, string name, Material material, int points)
        {
            Transform t = Child(parent, name);
            var line = t.gameObject.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = true;
            line.positionCount = points;
            var positions = new Vector3[points];
            for (int i = 0; i < points; i++)
            {
                positions[i] = Vector3.up * i;
            }
            line.SetPositions(positions);
            line.startWidth = 1f;
            line.endWidth = 0.85f;
            line.textureMode = LineTextureMode.Stretch;
            line.alignment = LineAlignment.View;
            line.numCapVertices = 2;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;
            return line;
        }


        /// <summary>Sets a serialized field when the component has it (a field another owner adds); skips it otherwise.</summary>
        private static void SetIfPresent(Object target, string property, Action<SerializedProperty> assign)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty found = serialized.FindProperty(property);
            if (found == null)
            {
                Debug.LogWarning($"Asteroids: {target.GetType().Name} has no field {property} yet; it was left out.");
                return;
            }
            assign(found);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }


        /// <summary>A hidden glow quad of <see cref="PlayerBeam"/> (a beam's hot spot or the gun's flare).</summary>
        private static Transform BeamGlow(Transform parent, string name, Material material, float size)
        {
            MeshRenderer glow = Quad(parent, name, material, Vector3.zero, Vector2.one * size);
            glow.gameObject.SetActive(false);
            return glow.transform;
        }


        // ------------------------------------------------------------------ bosses

        /// <summary>The prefab names of the strike bosses, missions 1 to 9 (Prefabs/Strike/Bosses/{name}).</summary>
        public static readonly string[] StrikeBossNames =
        {
            "SandCrawler", "RefineryGuardian", "SeaFortress", "TwinRotor", "TwinSilos", "Skyhammer", "DomeFortress", "FoundryCrawler", "TheShadow"
        };


        /// <summary>
        /// The root of a strike boss: <see cref="StrikeBoss"/> with its core numbers, and "Visual" (under a "Depth" anchor on
        /// the ground for a ground boss, so its base stands on the ground). Ground bosses and their parts are ground targets.
        /// </summary>
        private static StrikeBoss StrikeBossRoot(string name, string title, bool ground, float radius, float health, int score, int coreTier, float holdLine,
            out Transform visual)
        {
            var root = new GameObject(name);
            var boss = root.AddComponent<StrikeBoss>();
            Transform parent = root.transform;
            if (ground)
            {
                Transform depth = Child(root.transform, "Depth");
                depth.gameObject.AddComponent<DepthAnchor>().depth = StrikeRules.GroundDepth;
                parent = depth;
            }
            visual = Child(parent, "Visual");
            boss.visual = visual;
            boss.displayName = title;
            boss.radius = radius;
            boss.maxHealth = health;
            boss.score = score;
            boss.contactDamage = 0f;
            boss.altitude = ground ? Altitude.Ground : Altitude.Air;
            boss.wraps = false;
            boss.pulledByGravity = false;
            boss.movement = BossMovement.HoverTop;
            boss.hoverHeight = holdLine;
            boss.moveSpeed = ground ? 0f : 2f;
            boss.facesShip = false;
            boss.coreTier = coreTier;
            boss.groundBoss = ground;
            boss.holdLine = holdLine;
            boss.entrySeconds = ground ? 0.5f : 2.5f;
            boss.phaseThresholds = new float[0];
            MeshRenderer shield = Model(root.transform, "Shield", SphereMesh, M("BossShield"), Vector3.zero, Quaternion.identity, SphereScale(radius * 2.35f));
            shield.gameObject.SetActive(false);
            boss.shield = shield.gameObject;
            return boss;
        }


        /// <summary>
        /// A boss part: root (<see cref="BossPart"/>) at <paramref name="at"/> under <paramref name="parent"/>, "Visual" (under
        /// a "Depth" anchor at <paramref name="baseHeight"/> above the ground for a ground boss) with an optional fixed base
        /// and a head: "Turret" (the turning head rests pointing up the screen, a fixed one points down) with its
        /// "Muzzle{n}" points at the barrel tips.
        /// </summary>
        private static BossPart Part(Transform parent, string name, Vector2 at, bool ground, float baseHeight, int tier, float health, int score, float radius,
            AttackPattern pattern, int burst, float burstGap, float interval, EnemyShotKind shot, Mesh baseMesh, Mesh headMesh, Material material,
            Vector2[] muzzles, float headHeight, bool aims = true)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(at.x, at.y, 0f);
            var part = root.AddComponent<BossPart>();
            Transform holder = root.transform;
            if (ground)
            {
                Transform depth = Child(root.transform, "Depth");
                depth.gameObject.AddComponent<DepthAnchor>().depth = StrikeRules.GroundDepth - baseHeight;
                holder = depth;
            }
            else
            {
                holder = Child(root.transform, "Lift", new Vector3(0f, 0f, -baseHeight));
            }
            Transform visual = Child(holder, "Visual");
            var renderers = new List<Renderer>();
            if (baseMesh != null)
            {
                renderers.Add(Model(visual, "Base", baseMesh, material, Vector3.zero, FaceCamera));
            }
            // A turning head rests pointing up its parent's +Y, as BossPart aims it; a fixed one points down at the pilots.
            Transform turret = Child(visual, "Turret", Vector3.zero, aims ? Quaternion.identity : Quaternion.Euler(0f, 0f, 180f));
            if (headMesh != null)
            {
                renderers.Add(Model(turret, "Head", headMesh, material, new Vector3(0f, 0f, baseMesh != null ? -0.3f : 0f), FaceCamera));
            }
            var muzzleList = new List<Transform>();
            for (int i = 0; i < muzzles.Length; i++)
            {
                muzzleList.Add(Child(turret, $"Muzzle{i + 1}", new Vector3(muzzles[i].x, muzzles[i].y, -headHeight)));
            }
            part.visual = visual;
            part.flashRenderers = renderers.ToArray();
            part.flashColor = new Color(1f, 0.85f, 0.7f);
            part.radius = radius;
            part.maxHealth = health;
            part.score = score;
            part.contactDamage = 0f;
            part.altitude = ground ? Altitude.Ground : Altitude.Air;
            part.wraps = false;
            part.pulledByGravity = false;
            part.tier = tier;
            part.pattern = pattern;
            part.burst = Mathf.Max(1, burst);
            part.burstGap = burstGap;
            part.interval = interval;
            part.turret = aims ? turret : null;
            part.muzzles = muzzleList.ToArray();
            part.shotKind = shot;
            part.fireInterval = interval;
            part.shotSpeed = StrikeUnitRules.Shot(shot).Speed;
            part.aimError = 3f;
            part.explosionTint = new Color(1f, 0.6f, 0.3f);
            part.explosionScale = 1.6f;
            return part;
        }


        /// <summary>A twin gun turret part on a round base (Aimed Flak).</summary>
        private static BossPart TwinTurret(Transform parent, string name, Vector2 at, bool ground, float baseHeight, int tier, float health, int score, Material material,
            int burst = 2, float interval = 1.6f)
        {
            return Part(parent, name, at, ground, baseHeight, tier, health, score, 1.05f, AttackPattern.Aimed, burst, 0.2f, interval, EnemyShotKind.Flak,
                AsteroidsArtBuilder.StrikeModel("TurretBase"), AsteroidsArtBuilder.StrikeModel("TwinTurret"), material,
                new[] { new Vector2(-0.26f, 1.8f), new Vector2(0.26f, 1.8f) }, 0.65f);
        }


        /// <summary>A four-barrel flak turret part (AimedBurst Flak).</summary>
        private static BossPart FlakTurret(Transform parent, string name, Vector2 at, bool ground, float baseHeight, int tier, float health, int score, Material material)
        {
            return Part(parent, name, at, ground, baseHeight, tier, health, score, 1.05f, AttackPattern.AimedBurst, 3, 0.15f, 2f, EnemyShotKind.Flak,
                AsteroidsArtBuilder.StrikeModel("TurretBase"), AsteroidsArtBuilder.StrikeModel("FlakTurret"), material,
                new[] { new Vector2(-0.36f, 1.5f), new Vector2(-0.12f, 1.5f), new Vector2(0.12f, 1.5f), new Vector2(0.36f, 1.5f) }, 0.65f);
        }


        /// <summary>A missile launcher part (Rockets, two per volley).</summary>
        private static BossPart LauncherPart(Transform parent, string name, Vector2 at, bool ground, float baseHeight, int tier, float health, int score, Material material)
        {
            return Part(parent, name, at, ground, baseHeight, tier, health, score, 1.1f, AttackPattern.Rockets, 2, 0.3f, 2.6f, EnemyShotKind.Rocket,
                AsteroidsArtBuilder.StrikeModel("TurretBase"), AsteroidsArtBuilder.StrikeModel("Launcher"), material,
                new[] { new Vector2(-0.35f, 0.8f), new Vector2(0.35f, 0.8f) }, 0.85f);
        }


        /// <summary>A laser emitter part (a Laser column under it, with a glow telegraph).</summary>
        private static BossPart LaserPart(Transform parent, string name, Vector2 at, bool ground, float baseHeight, int tier, float health, int score, Material material,
            Mesh mesh, float radius, float interval)
        {
            return Part(parent, name, at, ground, baseHeight, tier, health, score, radius, AttackPattern.Laser, 1, 0f, interval, EnemyShotKind.Beam,
                null, mesh, material, new[] { Vector2.zero }, 1f, false);
        }


        private static Transform Ring(Transform parent, string name)
        {
            return Child(parent, name);
        }


        private static void FinishBoss(StrikeBoss boss, Transform visual, List<BossPart> parts, Color tint, params BossAttack[] attacks)
        {
            boss.parts = parts.ToArray();
            boss.flashRenderers = visual.GetComponentsInChildren<Renderer>(true);
            boss.flashColor = new Color(1f, 0.85f, 0.7f);
            boss.explosionTint = tint;
            boss.attacks = attacks;
        }


        private static void BuildStrikeBosses()
        {
            // 1 Sand Crawler: a tracked fortress; two twin turrets (tier 0), then the main cannon (the core, tier 1).
            {
                StrikeBoss boss = StrikeBossRoot("SandCrawler", "Sand Crawler", true, 2.2f, 320f, 8000, 1, 0.45f, out Transform visual);
                Material paint = SM("StrikeSand");
                Model(visual, "Hull", AsteroidsArtBuilder.StrikeModel("CrawlerHull"), paint, Vector3.zero, FaceDown);
                Model(visual, "Cannon", AsteroidsArtBuilder.StrikeModel("MainCannon"), paint, new Vector3(0f, 0.6f, -2.2f), FaceDown);
                var parts = new List<BossPart>
                {
                    TwinTurret(boss.transform, "TurretLeft", new Vector2(-1.7f, -2.9f), true, 2.2f, 0, 100f, 1500, paint),
                    TwinTurret(boss.transform, "TurretRight", new Vector2(1.7f, -2.9f), true, 2.2f, 0, 100f, 1500, paint)
                };
                FinishBoss(boss, visual, parts, new Color(1f, 0.6f, 0.3f),
                    Attack(BossAttackType.AimedBurst, 2.6f, 3, 9f, 0, 20f, EnemyShotKind.Bolt),
                    Attack(BossAttackType.SpreadShot, 4f, 5, 6f, 1, 60f, EnemyShotKind.Rocket));
                SaveStrike(boss.gameObject, "Bosses/SandCrawler");
            }

            // 2 Refinery Guardian: a pump station; four flak turrets on its tanks (tier 0), then the reactor (tier 1).
            {
                StrikeBoss boss = StrikeBossRoot("RefineryGuardian", "Refinery Guardian", true, 2f, 600f, 12000, 1, 0.45f, out Transform visual);
                Material paint = SM("StrikeSand");
                Model(visual, "Station", AsteroidsArtBuilder.StrikeModel("PumpStation"), paint, Vector3.zero, FaceDown);
                Model(visual, "Reactor", AsteroidsArtBuilder.StrikeModel("Reactor"), paint, new Vector3(0f, 0f, -1f), FaceDown);
                var parts = new List<BossPart>();
                Vector2[] tanks = { new Vector2(-3.3f, -2.3f), new Vector2(3.3f, -2.3f), new Vector2(-3.3f, 2.3f), new Vector2(3.3f, 2.3f) };
                string[] names = { "FlakFrontLeft", "FlakFrontRight", "FlakBackLeft", "FlakBackRight" };
                for (int i = 0; i < tanks.Length; i++)
                {
                    parts.Add(FlakTurret(boss.transform, names[i], tanks[i], true, 1.25f, 0, 150f, 1500, paint));
                }
                FinishBoss(boss, visual, parts, new Color(0.6f, 1f, 0.4f),
                    Attack(BossAttackType.RingShot, 4.5f, 10, 7f, 0, 0f, EnemyShotKind.Bolt),
                    Attack(BossAttackType.AimedBurst, 2.5f, 4, 10f, 1, 20f, EnemyShotKind.Flak));
                SaveStrike(boss.gameObject, "Bosses/RefineryGuardian");
            }

            // 3 Sea Fortress: an oil rig; two missile launchers and two flak turrets (tier 0), then the laser core (tier 1).
            {
                StrikeBoss boss = StrikeBossRoot("SeaFortress", "Sea Fortress", true, 1.9f, 1000f, 120000, 1, 0.45f, out Transform visual);
                Material paint = SM("StrikePalette");
                Model(visual, "Rig", AsteroidsArtBuilder.StrikeModel("RigPlatform"), paint, Vector3.zero, FaceDown);
                Model(visual, "Laser", AsteroidsArtBuilder.StrikeModel("LaserCore"), paint, new Vector3(0f, -0.4f, -2.45f), FaceDown);
                var parts = new List<BossPart>
                {
                    LauncherPart(boss.transform, "LauncherLeft", new Vector2(-4.2f, -2.8f), true, 2.45f, 0, 300f, 2000, paint),
                    LauncherPart(boss.transform, "LauncherRight", new Vector2(4.2f, -2.8f), true, 2.45f, 0, 300f, 2000, paint),
                    FlakTurret(boss.transform, "FlakLeft", new Vector2(-1.6f, -3.4f), true, 2.45f, 0, 250f, 2000, paint),
                    FlakTurret(boss.transform, "FlakRight", new Vector2(1.6f, -3.4f), true, 2.45f, 0, 250f, 2000, paint)
                };
                FinishBoss(boss, visual, parts, new Color(1f, 0.45f, 0.35f),
                    Attack(BossAttackType.AimedBurst, 3.5f, 1, 0f, 0, 0f, EnemyShotKind.Beam),
                    Attack(BossAttackType.RingShot, 5f, 12, 7f, 1, 0f, EnemyShotKind.Bolt),
                    Attack(BossAttackType.SpreadShot, 3f, 5, 8f, 1, 50f, EnemyShotKind.Flak));
                boss.sweep = 3f;
                boss.sweepSpeed = 0.4f;
                SaveStrike(boss.gameObject, "Bosses/SeaFortress");
            }

            // 4 Twin Rotor: a tandem-rotor gunship; two side cannons and a rocket pod (tier 0), then the gunship (tier 1).
            {
                StrikeBoss boss = StrikeBossRoot("TwinRotor", "Twin Rotor", false, 2.2f, 900f, 20000, 1, 0.5f, out Transform visual);
                Material paint = SM("StrikeOlive");
                Model(visual, "Body", AsteroidsArtBuilder.StrikeModel("Gunship"), paint, Vector3.zero, FaceDown, Vector3.one * 2.3f);
                Transform front = Child(boss.transform, "RotorFront", new Vector3(0f, -3.1f, -2.6f));
                Model(front, "Blades", AsteroidsArtBuilder.StrikeModel("RotorLarge"), paint, Vector3.zero, FaceCamera);
                Transform back = Child(boss.transform, "RotorBack", new Vector3(0f, 3.4f, -2.8f));
                Model(back, "Blades", AsteroidsArtBuilder.StrikeModel("RotorLarge"), paint, Vector3.zero, FaceCamera * Quaternion.Euler(0f, 60f, 0f));
                var parts = new List<BossPart>
                {
                    Part(boss.transform, "CannonLeft", new Vector2(-2.45f, -0.5f), false, 1.2f, 0, 250f, 2000, 1f, AttackPattern.AimedBurst, 3, 0.15f, 2.2f, EnemyShotKind.Flak,
                        null, AsteroidsArtBuilder.StrikeModel("SideCannon"), paint, new[] { new Vector2(-0.3f, 1.6f), new Vector2(0.3f, 1.6f) }, 0.35f),
                    Part(boss.transform, "CannonRight", new Vector2(2.45f, -0.5f), false, 1.2f, 0, 250f, 2000, 1f, AttackPattern.AimedBurst, 3, 0.15f, 2.2f, EnemyShotKind.Flak,
                        null, AsteroidsArtBuilder.StrikeModel("SideCannon"), paint, new[] { new Vector2(-0.3f, 1.6f), new Vector2(0.3f, 1.6f) }, 0.35f),
                    Part(boss.transform, "RocketPod", new Vector2(0f, -4.6f), false, 1f, 0, 300f, 2000, 1.1f, AttackPattern.Rockets, 2, 0.3f, 2.8f, EnemyShotKind.Rocket,
                        null, AsteroidsArtBuilder.StrikeModel("Launcher"), paint, new[] { new Vector2(-0.35f, 0.8f), new Vector2(0.35f, 0.8f) }, 0.55f, false)
                };
                boss.spinners = new[] { front, back };
                boss.spinSpeed = 720f;
                FinishBoss(boss, visual, parts, new Color(1f, 0.6f, 0.3f),
                    Attack(BossAttackType.SpreadShot, 3f, 5, 8f, 0, 50f, EnemyShotKind.Bolt),
                    Attack(BossAttackType.RingShot, 5f, 12, 6f, 1, 0f, EnemyShotKind.Flak));
                boss.sweep = 9f;
                boss.sweepSpeed = 0.45f;
                AddShadow(boss.transform);
                SaveStrike(boss.gameObject, "Bosses/TwinRotor");
            }

            // 5 Twin Silos: a coreless pad; its two laser silos open and fire in turns, and the boss falls with the second.
            {
                StrikeBoss boss = StrikeBossRoot("TwinSilos", "Twin Silos", true, 1.5f, 1f, 30000, 0, 0.4f, out Transform visual);
                Material paint = SM("StrikeOlive");
                Model(visual, "Pad", AsteroidsArtBuilder.StrikeModel("SiloPad"), paint, Vector3.zero, FaceDown);
                boss.coreless = true;
                boss.invulnerable = true;
                var parts = new List<BossPart>
                {
                    Part(boss.transform, "SiloLeft", new Vector2(-4.2f, 0f), true, 0.6f, 0, 1000f, 2000, 1.6f, AttackPattern.Laser, 1, 0f, 3f, EnemyShotKind.Beam,
                        null, AsteroidsArtBuilder.StrikeModel("Silo"), paint, new[] { Vector2.zero }, 1.9f, false),
                    Part(boss.transform, "SiloRight", new Vector2(4.2f, 0f), true, 0.6f, 0, 1000f, 2000, 1.6f, AttackPattern.Laser, 1, 0f, 3f, EnemyShotKind.Beam,
                        null, AsteroidsArtBuilder.StrikeModel("Silo"), paint, new[] { Vector2.zero }, 1.9f, false)
                };
                FinishBoss(boss, visual, parts, new Color(1f, 0.4f, 0.3f),
                    Attack(BossAttackType.AimedBurst, 2.4f, 3, 9f, 0, 15f, EnemyShotKind.Flak),
                    Attack(BossAttackType.SpreadShot, 4f, 5, 7f, 0, 70f, EnemyShotKind.Bolt));
                SaveStrike(boss.gameObject, "Bosses/TwinSilos");
            }

            // 6 Skyhammer: a heavy transport; two rocket pods and a mine module (tier 0), then the transport (tier 1).
            {
                StrikeBoss boss = StrikeBossRoot("Skyhammer", "Skyhammer", false, 2.8f, 1500f, 250000, 1, 0.5f, out Transform visual);
                Material paint = SM("StrikeNight");
                PackHull(Child(visual, "Frame", Vector3.zero, FaceDown), "StarSparrow10", SM("AirBlack"), 11f);
                EngineFlame(boss.transform, "EngineLeft", new Vector2(-3.4f, 4.9f), 2.2f);
                EngineFlame(boss.transform, "EngineRight", new Vector2(3.4f, 4.9f), 2.2f);
                var parts = new List<BossPart>
                {
                    Part(boss.transform, "PodLeft", new Vector2(-4.1f, -0.6f), false, 0.9f, 0, 600f, 3000, 1.2f, AttackPattern.Rockets, 2, 0.25f, 2.4f, EnemyShotKind.Rocket,
                        null, AsteroidsArtBuilder.StrikeModel("ModulePod"), paint, new[] { new Vector2(0f, 1.6f) }, 0.3f, false),
                    Part(boss.transform, "PodRight", new Vector2(4.1f, -0.6f), false, 0.9f, 0, 600f, 3000, 1.2f, AttackPattern.Rockets, 2, 0.25f, 2.4f, EnemyShotKind.Rocket,
                        null, AsteroidsArtBuilder.StrikeModel("ModulePod"), paint, new[] { new Vector2(0f, 1.6f) }, 0.3f, false),
                    Part(boss.transform, "MineModule", new Vector2(0f, 3.4f), false, 1.1f, 0, 600f, 3000, 1.2f, AttackPattern.Mines, 1, 0f, 3.5f, EnemyShotKind.SkyMine,
                        null, AsteroidsArtBuilder.StrikeModel("ModulePodRed"), paint, new[] { new Vector2(0f, 1.6f) }, 0.3f, false)
                };
                FinishBoss(boss, visual, parts, new Color(1f, 0.5f, 0.8f),
                    Attack(BossAttackType.SpreadShot, 3.2f, 7, 8f, 0, 70f, EnemyShotKind.Bolt),
                    Attack(BossAttackType.AimedBurst, 3f, 5, 11f, 1, 15f, EnemyShotKind.Flak));
                boss.sweep = 6f;
                boss.sweepSpeed = 0.35f;
                AddShadow(boss.transform);
                SaveStrike(boss.gameObject, "Bosses/Skyhammer");
            }

            // 7 Dome Fortress: a bastion; four turrets (tier 0), then the dome (tier 1).
            {
                StrikeBoss boss = StrikeBossRoot("DomeFortress", "Dome Fortress", true, 2.4f, 1500f, 45000, 1, 0.45f, out Transform visual);
                Material paint = SM("StrikeNight");
                Model(visual, "Bastion", AsteroidsArtBuilder.StrikeModel("Bastion"), paint, Vector3.zero, FaceDown);
                Model(visual, "Dome", AsteroidsArtBuilder.StrikeModel("Dome"), paint, new Vector3(0f, 0f, -1.5f), FaceDown);
                var parts = new List<BossPart>();
                Vector2[] sockets = { new Vector2(-3.6f, -2.6f), new Vector2(3.6f, -2.6f), new Vector2(-3.6f, 2.6f), new Vector2(3.6f, 2.6f) };
                string[] names = { "TurretFrontLeft", "TurretFrontRight", "TurretBackLeft", "TurretBackRight" };
                for (int i = 0; i < sockets.Length; i++)
                {
                    parts.Add(TwinTurret(boss.transform, names[i], sockets[i], true, 1.6f, 0, 375f, 3000, paint, 3, 1.8f));
                }
                FinishBoss(boss, visual, parts, new Color(1f, 0.55f, 0.3f),
                    Attack(BossAttackType.RingShot, 4f, 14, 7f, 0, 0f, EnemyShotKind.Bolt),
                    Attack(BossAttackType.SpreadShot, 3.5f, 5, 6f, 1, 60f, EnemyShotKind.Rocket),
                    Attack(BossAttackType.AimedBurst, 4.5f, 1, 0f, 1, 0f, EnemyShotKind.Beam));
                SaveStrike(boss.gameObject, "Bosses/DomeFortress");
            }

            // 8 Foundry Crawler: the crawler's hull in basalt; two flame vents and two turrets (tier 0), then the cannon (tier 1).
            {
                StrikeBoss boss = StrikeBossRoot("FoundryCrawler", "Foundry Crawler", true, 2.2f, 1600f, 60000, 1, 0.45f, out Transform visual);
                Material paint = SM("StrikeBasalt");
                Model(visual, "Hull", AsteroidsArtBuilder.StrikeModel("FoundryHull"), paint, Vector3.zero, FaceDown);
                Model(visual, "Cannon", AsteroidsArtBuilder.StrikeModel("MainCannon"), paint, new Vector3(0f, 0.6f, -2.2f), FaceDown);
                var parts = new List<BossPart>
                {
                    TwinTurret(boss.transform, "TurretLeft", new Vector2(-1.7f, -2.9f), true, 2.2f, 0, 400f, 3000, paint, 3, 1.6f),
                    TwinTurret(boss.transform, "TurretRight", new Vector2(1.7f, -2.9f), true, 2.2f, 0, 400f, 3000, paint, 3, 1.6f),
                    Part(boss.transform, "VentLeft", new Vector2(-3f, 0.5f), true, 1.35f, 0, 450f, 3000, 1f, AttackPattern.Spread, 1, 0f, 2.2f, EnemyShotKind.Bolt,
                        null, AsteroidsArtBuilder.StrikeModel("FlameVent"), paint, new[] { new Vector2(0f, 0.9f) }, 0.5f, false),
                    Part(boss.transform, "VentRight", new Vector2(3f, 0.5f), true, 1.35f, 0, 450f, 3000, 1f, AttackPattern.Spread, 1, 0f, 2.2f, EnemyShotKind.Bolt,
                        null, AsteroidsArtBuilder.StrikeModel("FlameVent"), paint, new[] { new Vector2(0f, 0.9f) }, 0.5f, false)
                };
                FinishBoss(boss, visual, parts, new Color(1f, 0.45f, 0.15f),
                    Attack(BossAttackType.AimedBurst, 3f, 3, 8f, 0, 20f, EnemyShotKind.Rocket),
                    Attack(BossAttackType.RingShot, 4.5f, 16, 7f, 1, 0f, EnemyShotKind.Flak));
                SaveStrike(boss.gameObject, "Bosses/FoundryCrawler");
            }

            // 9 The Shadow: a station; the outer turret ring (tier 0), the inner ring of lasers and launchers (tier 1), the
            // core (tier 2), in three phases.
            {
                StrikeBoss boss = StrikeBossRoot("TheShadow", "The Shadow", false, 2.6f, 2500f, 500000, 2, 0.4f, out Transform visual);
                Material paint = SM("StrikeCrimson");
                Model(visual, "Core", AsteroidsArtBuilder.StrikeModel("StationCore"), paint, Vector3.zero, FaceCamera);
                Transform outer = Ring(boss.transform, "OuterRing");
                Model(outer, "Structure", AsteroidsArtBuilder.StrikeModel("StationOuterRing"), paint, new Vector3(0f, 0f, 0.3f), FaceCamera);
                Transform inner = Ring(boss.transform, "InnerRing");
                Model(inner, "Structure", AsteroidsArtBuilder.StrikeModel("StationInnerRing"), paint, new Vector3(0f, 0f, 0.1f), FaceCamera);
                var parts = new List<BossPart>();
                for (int i = 0; i < 6; i++)
                {
                    float angle = (i * 60f + 90f) * Mathf.Deg2Rad;
                    var at = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 6.2f;
                    parts.Add(Part(outer, $"OuterTurret{i + 1}", at, false, 0.6f, 0, 300f, 4000, 1f, AttackPattern.Aimed, 2, 0.2f, 1.8f, EnemyShotKind.Flak,
                        AsteroidsArtBuilder.StrikeModel("TurretBase"), AsteroidsArtBuilder.StrikeModel("TwinTurret"), paint,
                        new[] { new Vector2(-0.26f, 1.8f), new Vector2(0.26f, 1.8f) }, 0.65f));
                }
                for (int i = 0; i < 4; i++)
                {
                    float angle = (i * 90f + 45f) * Mathf.Deg2Rad;
                    var at = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 3.9f;
                    parts.Add(i % 2 == 0
                        ? LaserPart(inner, $"InnerLaser{i / 2 + 1}", at, false, 0.5f, 1, 550f, 4000, paint, AsteroidsArtBuilder.StrikeModel("LaserEmitter"), 1.1f, 3.2f)
                        : LauncherPart(inner, $"InnerLauncher{i / 2 + 1}", at, false, 0.5f, 1, 550f, 4000, paint));
                }
                boss.rings = new[] { outer, inner };
                boss.ringSpeeds = new[] { 12f, -20f };
                boss.alarm = true;
                FinishBoss(boss, visual, parts, new Color(1f, 0.35f, 0.8f),
                    Attack(BossAttackType.RingShot, 4f, 16, 6f, 0, 0f, EnemyShotKind.Plasma),
                    Attack(BossAttackType.AimedBurst, 2.8f, 5, 10f, 1, 15f, EnemyShotKind.Bolt),
                    Attack(BossAttackType.SpreadShot, 3.4f, 7, 7f, 1, 80f, EnemyShotKind.Rocket),
                    Attack(BossAttackType.AimedBurst, 3.6f, 1, 0f, 2, 0f, EnemyShotKind.Beam),
                    Attack(BossAttackType.RingShot, 2.6f, 20, 8f, 2, 0f, EnemyShotKind.Flak));
                boss.sweep = 4f;
                boss.sweepSpeed = 0.25f;
                AddShadow(boss.transform);
                SaveStrike(boss.gameObject, "Bosses/TheShadow");
            }
        }


        // ------------------------------------------------------------------ game-camera preview

        /// <summary>
        /// Batch mode: a still of the strike playfield through the game's camera (perspective, 40 degrees, the playfield of
        /// <see cref="StrikeRules.HalfSize"/>) over a flat stand-in ground at the ground depth, with a boss, a sky of air units
        /// and their shadows, pickups and shots, to judge sizes and readability. <c>-strikePreview &lt;png&gt;</c> is the
        /// output, <c>-strikePreviewBoss &lt;name&gt;</c> the boss (Prefabs/Strike/Bosses), <c>-strikePreviewGround r,g,b</c>
        /// the ground colour. Opens a new empty scene.
        /// </summary>
        public static void StrikePreviewBatch()
        {
            try
            {
                string output = SheetArgument("-strikePreview") ?? "StrikePreview.png";
                string bossName = SheetArgument("-strikePreviewBoss") ?? "SandCrawler";
                string[] rgb = (SheetArgument("-strikePreviewGround") ?? "0.62,0.52,0.38").Split(',');
                var groundColor = new Color(float.Parse(rgb[0], System.Globalization.CultureInfo.InvariantCulture),
                    float.Parse(rgb[1], System.Globalization.CultureInfo.InvariantCulture), float.Parse(rgb[2], System.Globalization.CultureInfo.InvariantCulture));
                StrikePreview(output, bossName, groundColor);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }


        private static void StrikePreview(string output, string bossName, Color groundColor)
        {
            // The game's own scene: its sun, ambient light, camera and post-processing; the space backdrop and the scene's
            // ship are put away.
            EditorSceneManager.OpenScene(AsteroidsAssets.Path(AsteroidsSceneBuilder.ScenePath), OpenSceneMode.Single);
            foreach (string hidden in new[] { "Backdrop/Space", "Backdrop/Planet", "Backdrop/Motes", "Ship" })
            {
                GameObject found = GameObject.Find(hidden);
                if (found != null)
                {
                    found.SetActive(false);
                }
            }
            // Shader variants the editor has not compiled yet would draw as placeholders: compile them before drawing.
            ShaderUtil.allowAsyncCompilation = false;
            float distance = StrikeRules.HalfSize.y / Mathf.Tan(20f * Mathf.Deg2Rad);
            DepthLayer.Distance = distance;
            Camera camera = Camera.main;
            camera.transform.position = new Vector3(0f, 0f, -distance);
            camera.transform.rotation = Quaternion.identity;
            camera.fieldOfView = 40f;

            int levelNumber = int.TryParse(SheetArgument("-strikePreviewLevel"), out int number) ? number : 0;
            StrikeLevel level = levelNumber > 0 ? AsteroidsAssets.Load<StrikeLevel>($"Config/Strike/Level{levelNumber}.asset") : null;
            if (level == null)
            {
                // The stand-in ground: a flat quad at the ground depth, with a darker band as a road at x = -9.
                float k = DepthLayer.Factor(StrikeRules.GroundDepth);
                var unlit = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                unlit.SetColor("_BaseColor", groundColor);
                var road = new Material(unlit);
                road.SetColor("_BaseColor", groundColor * 0.6f);
                Quad(null, "Ground", unlit, new Vector3(0f, 0f, StrikeRules.GroundDepth + 0.01f), new Vector2(90f, 60f));
                Quad(null, "Road", road, new Vector3(-9f * k, 0f, StrikeRules.GroundDepth), new Vector2(5f * k, 60f));
            }

            GameObject Put(string prefab, Vector2 at, float heading = 0f)
            {
                var asset = AsteroidsAssets.Load<GameObject>($"Prefabs/{prefab}.prefab");
                if (asset == null)
                {
                    Debug.LogWarning($"Strike preview: {prefab} is missing.");
                    return null;
                }
                GameObject instance = Object.Instantiate(asset);
                instance.transform.position = new Vector3(at.x, at.y, 0f);
                instance.transform.rotation = Quaternion.Euler(0f, 0f, heading);
                foreach (DepthAnchor anchor in instance.GetComponentsInChildren<DepthAnchor>(true))
                {
                    anchor.Place();
                }
                foreach (DropShadow shadow in instance.GetComponentsInChildren<DropShadow>(true))
                {
                    Vector3 root = shadow.transform.parent.position;
                    DepthLayer.Place(shadow.transform, new Vector2(root.x, root.y) + StrikeRules.ShadowOffset, StrikeRules.GroundDepth);
                    shadow.transform.rotation = shadow.transform.parent.rotation;
                }
                foreach (ParticleSystem system in instance.GetComponentsInChildren<ParticleSystem>(true))
                {
                    system.Simulate(0.4f, false, true);
                }
                return instance;
            }

            if (level != null)
            {
                float at = float.TryParse(SheetArgument("-strikePreviewDistance"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture,
                    out float value) ? value : 0f;
                PreviewLevel(level, at, Put);
            }
            else
            {
                Put($"Strike/Bosses/{bossName}", new Vector2(0f, 4.5f));
                if (SheetArgument("-strikePreviewMaterials") != null)
                {
                    // A material check: the same hull in the pack's material and in the strike paints, side by side.
                    string[] names = { "PACK", "AirCrimson", "AirRust", "AirSand", "AirOlive" };
                    for (int i = 0; i < names.Length; i++)
                    {
                        Material material = names[i] == "PACK" ? AsteroidsArtBuilder.PackMaterial("StarSparrow Red") : SM(names[i]);
                        var holder = new GameObject(names[i]);
                        holder.transform.position = new Vector3(-12f + i * 6f, -5f, 0f);
                        PackHull(Child(holder.transform, "Visual", Vector3.zero, FaceCamera), "StarSparrow13", material, 5f);
                    }
                }
                string extra = SheetArgument("-strikePreviewExtra");
                if (!string.IsNullOrEmpty(extra))
                {
                    // prefab@x,y (for comparisons with other prefabs).
                    string[] parts = extra.Split('@');
                    string[] xy = parts[1].Split(',');
                    Put(parts[0], new Vector2(float.Parse(xy[0], System.Globalization.CultureInfo.InvariantCulture), float.Parse(xy[1], System.Globalization.CultureInfo.InvariantCulture)));
                }
                Put("Strike/Air/Dart", new Vector2(-12f, 2f), 200f);
                Put("Strike/Air/Dart", new Vector2(-9f, 4f), 200f);
                Put("Strike/Air/Dart", new Vector2(-15f, 4f), 200f);
                Put("Strike/Air/Hornet", new Vector2(12f, 1f), 170f);
                Put("Strike/Air/Hornet", new Vector2(15.5f, 3f), 170f);
                Put("Strike/Air/Gunship", new Vector2(9f, -3f), 180f);
                Put("Strike/Air/Bomber", new Vector2(-4f, -3f), 180f);
                Put("Strike/Air/Interceptor", new Vector2(-14f, -6f), 150f);
                Put("Strike/Air/Kamikaze", new Vector2(4f, 0f), 220f);
                Put("Strike/Air/Transport", new Vector2(15f, -9f), 180f);
                Put("Strike/Pickups/SmallArms", new Vector2(-6f, -7f));
                Put("Strike/Pickups/AirMissiles", new Vector2(-2f, -8f));
                Put("Strike/Pickups/CreditOrb", new Vector2(1f, -7.5f));
                Put("Strike/Pickups/FusionCore", new Vector2(6f, -8f));
                Put("Strike/Shots/EnemyFlak", new Vector2(-8f, 0f));
                Put("Strike/Shots/EnemyBolt", new Vector2(-7f, -1f), 180f);
                Put("Strike/Shots/EnemyRocket", new Vector2(11f, -1f), 170f);
                Put("Strike/Shots/EnemySkyMine", new Vector2(0f, -3f));
                Put("Strike/Shots/Bullet", new Vector2(-0.55f, -5f));
                Put("Strike/Shots/Bullet", new Vector2(0.55f, -5f));
                Put("Strike/Shots/PlasmaBolt", new Vector2(0f, -3.5f));
                Put("Strike/Shots/AirMissile", new Vector2(-1.2f, -4f));
                Put("Strike/Shots/AirMissile", new Vector2(1.2f, -4f));
            }
            GameObject ship = Put("Ship", new Vector2(0f, -7.5f));
            if (ship != null)
            {
                Transform bubble = ship.transform.Find("Shield");
                if (bubble != null)
                {
                    bubble.gameObject.SetActive(false);
                }
            }

            const int width = 1920;
            const int height = 1080;
            camera.aspect = width / (float)height;
            var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            camera.targetTexture = target;
            // The first frames can come out before every shader and texture is ready: they are thrown away.
            for (int warm = 0; warm < 4; warm++)
            {
                camera.Render();
            }
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            var picture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            picture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            picture.Apply();
            RenderTexture.active = previous;
            camera.targetTexture = null;
            RenderTexture.ReleaseTemporary(target);
            File.WriteAllBytes(output, picture.EncodeToPNG());
            Object.DestroyImmediate(picture);
            Debug.Log($"Strike preview written to {output}.");
        }


        /// <summary>
        /// The view of <paramref name="level"/> at scroll distance <paramref name="distance"/>: its terrain tiles (depth
        /// placed as the terrain places them), the ground units and the aircraft of the events near the screen (aircraft at
        /// their event's x, in a row near the top), the boss at the end, and the theme's light.
        /// </summary>
        private static void PreviewLevel(StrikeLevel level, float distance, Func<string, Vector2, float, GameObject> put)
        {
            float top = StrikeRules.HalfSize.y;
            StrikeTheme theme = level.Terrain;
            if (theme != null)
            {
                Light sun = RenderSettings.sun;
                if (sun != null)
                {
                    sun.color = theme.SunColor;
                    sun.intensity = theme.SunIntensity;
                    sun.transform.rotation = Quaternion.Euler(theme.SunAngles);
                }
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = theme.AmbientColor;
                for (int i = 0; i < 40; i++)
                {
                    float y = top + StrikeRules.TileLength * i - StrikeRules.TileLength * 0.5f - distance;
                    if (y < -top - 12f || y > top + 12f)
                    {
                        continue;
                    }
                    TerrainSegment segment = level.SegmentOfTile(i);
                    TerrainTile prefab = segment != null ? theme.Tile(segment.kind, segment.variant) : null;
                    if (prefab == null)
                    {
                        continue;
                    }
                    TerrainTile tile = Object.Instantiate(prefab);
                    DepthLayer.Place(tile.transform, new Vector2(0f, y), StrikeRules.GroundDepth);
                }
            }
            float lastAir = -100f;
            foreach (StrikeEvent spawn in level.Events)
            {
                if (spawn.minDifficulty > StrikeDifficulty.Veteran)
                {
                    continue;
                }
                if (spawn.IsAir)
                {
                    // The squadrons that set off in the last screen, drawn at the top.
                    if (spawn.at <= distance && spawn.at > distance - 20f && spawn.at > lastAir + 2f)
                    {
                        lastAir = spawn.at;
                        for (int m = 0; m < spawn.count; m++)
                        {
                            put($"Strike/Air/{spawn.unit}", new Vector2(spawn.x + (m - (spawn.count - 1) * 0.5f) * spawn.spacing, top - 2.5f - (distance - spawn.at) * 0.4f), 180f);
                        }
                    }
                    continue;
                }
                for (int m = 0; m < spawn.count; m++)
                {
                    Vector2 at = new Vector2(spawn.x, top + spawn.at - distance) + spawn.offset * m;
                    if (at.y < -top - 2f || at.y > top + 2f)
                    {
                        continue;
                    }
                    put($"Strike/Ground/{spawn.unit}", at, spawn.heading);
                    if (spawn.HasPickup)
                    {
                        Debug.Log($"Strike preview: {spawn.unit} at ({at.x:0.#}, {at.y:0.#}) holds {(spawn.money > 0 ? spawn.money.ToString() : spawn.bonus.ToString())}.");
                    }
                }
            }
            StrikeBoss boss = level.StrikeBossPrefab;
            if (boss != null && distance >= level.BossAt - 20f)
            {
                float rest = boss.HoldLine * StrikeRules.HalfSize.y;
                put($"Strike/Bosses/{boss.name}", new Vector2(0f, boss.IsGroundBoss ? rest + (level.BossAt - distance) : rest), 0f);
            }
        }


        // ------------------------------------------------------------------ contact sheets

        /// <summary>
        /// Batch mode (<c>-executeMethod</c>): renders a set of models or prefabs from above, as the game's camera sees them,
        /// into one picture. <c>-strikeSheet &lt;png&gt;</c> is the output file, <c>-strikeSheetSet &lt;names&gt;</c> the sets
        /// (comma separated: pack, modules, air, bosses, shots, pickups, effects, ground) and <c>-strikeSheetCell &lt;px&gt;</c>
        /// the cell size. The order of the cells is logged.
        /// </summary>
        public static void StrikeContactSheetBatch()
        {
            try
            {
                string output = SheetArgument("-strikeSheet") ?? "StrikeSheet.png";
                string set = SheetArgument("-strikeSheetSet") ?? "air";
                int cell = int.TryParse(SheetArgument("-strikeSheetCell"), out int size) ? size : 256;
                ContactSheet(set, output, cell);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }


        private static string SheetArgument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name)
                {
                    return args[i + 1];
                }
            }
            return null;
        }


        /// <summary>Renders the subjects of <paramref name="set"/> into <paramref name="output"/> (a PNG outside the project).</summary>
        public static void ContactSheet(string set, string output, int cell)
        {
            var subjects = new List<(string name, Func<GameObject> make)>();
            foreach (string part in set.Split(','))
            {
                AddSheetSubjects(part.Trim(), subjects);
            }
            if (subjects.Count == 0)
            {
                Debug.LogWarning($"Strike sheet: nothing in the set {set}.");
                return;
            }
            int columns = Mathf.CeilToInt(Mathf.Sqrt(subjects.Count * 1.6f));
            int rows = Mathf.CeilToInt(subjects.Count / (float)columns);
            int width = columns * cell;
            int height = rows * cell;
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                int x = i % width;
                int y = i / width;
                bool even = ((x / cell) + (y / cell)) % 2 == 0;
                pixels[i] = even ? new Color(0.36f, 0.33f, 0.27f) : new Color(0.3f, 0.34f, 0.3f);
            }
            var shot = new StudioShot { Width = cell, Height = cell, Yaw = 0f, Pitch = 0f, Supersample = 2, Padding = 0.06f };
            using (var studio = new ModelStudio())
            {
                for (int i = 0; i < subjects.Count; i++)
                {
                    GameObject subject = subjects[i].make();
                    if (subject == null)
                    {
                        Debug.Log($"Strike sheet {i}: {subjects[i].name} (missing)");
                        continue;
                    }
                    Texture2D picture;
                    try
                    {
                        studio.Adopt(subject);
                        // Particles and trails would stretch the framing to the world origin: run the particles a moment in
                        // the studio, and leave the trails out.
                        foreach (ParticleSystem system in subject.GetComponentsInChildren<ParticleSystem>(true))
                        {
                            system.Simulate(0.4f, false, true);
                        }
                        foreach (TrailRenderer trail in subject.GetComponentsInChildren<TrailRenderer>(true))
                        {
                            Object.DestroyImmediate(trail);
                        }
                        // The depth anchors place their visuals at their depth behind the plane (as the game does, unscaled).
                        foreach (DepthAnchor anchor in subject.GetComponentsInChildren<DepthAnchor>(true))
                        {
                            anchor.transform.localPosition = new Vector3(0f, 0f, anchor.Depth);
                        }
                        picture = studio.ShootPlaced(subject, shot);
                    }
                    finally
                    {
                        Object.DestroyImmediate(subject);
                    }
                    int column = i % columns;
                    int row = rows - 1 - i / columns;
                    Color[] cellPixels = picture.GetPixels();
                    for (int y = 0; y < cell; y++)
                    {
                        for (int x = 0; x < cell; x++)
                        {
                            Color over = cellPixels[y * cell + x];
                            int index = (row * cell + y) * width + column * cell + x;
                            pixels[index] = Color.Lerp(pixels[index], new Color(over.r, over.g, over.b, 1f), over.a);
                        }
                    }
                    Object.DestroyImmediate(picture);
                    Debug.Log($"Strike sheet {i} (row {i / columns}, column {column}): {subjects[i].name}");
                }
            }
            var sheet = new Texture2D(width, height, TextureFormat.RGBA32, false);
            sheet.SetPixels(pixels);
            sheet.Apply();
            string folder = Path.GetDirectoryName(Path.GetFullPath(output));
            if (!string.IsNullOrEmpty(folder))
            {
                Directory.CreateDirectory(folder);
            }
            File.WriteAllBytes(output, sheet.EncodeToPNG());
            Object.DestroyImmediate(sheet);
            Debug.Log($"Strike sheet: {subjects.Count} subjects in {columns} x {rows} written to {output}.");
        }


        private static void AddSheetSubjects(string set, List<(string name, Func<GameObject> make)> subjects)
        {
            switch (set)
            {
                case "pack":
                    for (int i = 1; i <= 13; i++)
                    {
                        string model = $"StarSparrow{i}";
                        subjects.Add((model, () => SheetMesh(AsteroidsArtBuilder.ShipMesh(model), AsteroidsArtBuilder.PackMaterial("StarSparrow Red"))));
                    }
                    break;
                case "modules":
                    foreach (string module in new[] { "Core", "Engine", "Plasma", "Thruster", "Weapon", "Wing_1", "Wing_2", "Wing_3" })
                    {
                        string name = $"StarSparrow_{module}";
                        subjects.Add((name, () => SheetMesh(AsteroidsArtBuilder.PackMesh("StarSparrowModules.FBX", name), AsteroidsArtBuilder.PackMaterial("StarSparrow Red"))));
                    }
                    foreach (string mesh in new[] { "MissileBomb", "MissileViking" })
                    {
                        string name = mesh;
                        subjects.Add((name, () => SheetMesh(AsteroidsArtBuilder.PackMesh("BonusContent/MissilesSample.FBX", name), AsteroidsArtBuilder.PackMaterial("BonusContent/Missile"))));
                    }
                    break;
                default:
                    string folder = SheetFolder(set);
                    if (folder == null)
                    {
                        Debug.LogWarning($"Strike sheet: unknown set {set}.");
                        break;
                    }
                    string path = AsteroidsAssets.Path(folder);
                    if (!AssetDatabase.IsValidFolder(path))
                    {
                        break;
                    }
                    foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { path }))
                    {
                        string asset = AssetDatabase.GUIDToAssetPath(guid);
                        subjects.Add((Path.GetFileNameWithoutExtension(asset), () =>
                        {
                            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(asset);
                            return prefab != null ? Object.Instantiate(prefab) : null;
                        }));
                    }
                    break;
            }
        }


        private static string SheetFolder(string set)
        {
            switch (set)
            {
                case "air": return "Prefabs/Strike/Air";
                case "bosses": return "Prefabs/Strike/Bosses";
                case "shots": return "Prefabs/Strike/Shots";
                case "pickups": return "Prefabs/Strike/Pickups";
                case "effects": return "Prefabs/Strike/Effects";
                case "ground": return "Prefabs/Strike/Ground";
                default: return null;
            }
        }


        /// <summary>A pack model turned as the game shows it (nose up the screen, top to the camera), for a contact sheet.</summary>
        private static GameObject SheetMesh(Mesh mesh, Material material)
        {
            if (mesh == null)
            {
                return null;
            }
            var root = new GameObject(mesh.name);
            float scale = 2f / Mathf.Max(0.01f, mesh.bounds.size.z);
            Model(root.transform, "Model", mesh, material, -(FaceCamera * mesh.bounds.center) * scale, FaceCamera, Vector3.one * scale);
            return root;
        }
    }
}
