using System;
using Gamebox.Editor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Portfolio.Asteroids.EditorTools
{
    /// <summary>
    /// The armory's prefabs: the shots of the four new field weapons (Shots/Player{Arc, Rail, Flak, Wave} and the flak's
    /// Shots/FlakShrapnel; their pickups Pickups/Weapon{Name} and the power-ups' come from the field's pickup loop), the
    /// shots of the six new strike specials (Strike/Shots/{StrikeShotKind}, kinds 11 to 19), the pickups of the new strike
    /// items that a level may place (Strike/Pickups/{StrikeItem}), the bolt of lightning (Effects/Lightning) and the burst
    /// of a flak shell (Effects/FlakBurst). The numbers come from <see cref="WeaponRules"/> (level 1; the spawner sets each
    /// level's) and <see cref="StrikeWeaponRules"/>.
    /// </summary>
    internal static partial class AsteroidsPrefabBuilder
    {
        /// <summary>The armory's strike items a level may place as pickups (priced at or below the pickup limit), in the order of their pools.</summary>
        public static readonly StrikeItem[] ArmoryStrikePickups =
        {
            StrikeItem.FlakCannon, StrikeItem.ArmorPlating, StrikeItem.Afterburner, StrikeItem.SalvageMagnet, StrikeItem.RepairKit
        };

        /// <summary>The prefab of the field shots of the armory's weapons, by <see cref="WeaponType"/> from Arc on.</summary>
        public static readonly string[] ArmoryFieldShots = { "PlayerArc", "PlayerRail", "PlayerFlak", "PlayerWave" };


        static partial void BuildArmoryPrefabs()
        {
            Progress("Armory", 0.995f);
            BuildArmoryFieldShots();
            BuildArmoryStrikeShots();
            BuildArmoryEffects();
            BuildArmoryPickups();
        }


        // ------------------------------------------------------------------ field shots

        private static void BuildArmoryFieldShots()
        {
            // Arc Caster: a violet spark with a short trail; the lightning jumps from what it hits.
            {
                ArcShot shot = ArmoryShot<ArcShot>("PlayerArc", M("ArcSpark"), new Vector2(0.8f, 0.8f), 0.26f, WeaponRules.Lifetime(WeaponType.Arc),
                    WeaponRules.Tint(WeaponType.Arc), new Color(0.6f, 0.45f, 1f), 0.12f);
                Quad(shot.visual, "Core", M("Bolt"), new Vector3(0f, 0f, -0.05f), new Vector2(0.18f, 0.7f));
                shot.chains = WeaponRules.Chains(WeaponType.Arc, 1);
                shot.chainRange = WeaponRules.ChainRange(WeaponType.Arc, 1);
                shot.chainDamage = WeaponRules.ChainDamage(WeaponType.Arc, 1);
                shot.arcTint = WeaponRules.Tint(WeaponType.Arc);
                Save(shot.gameObject, "Shots/PlayerArc");
            }

            // Rail Driver: a long white-hot slug with a bright wake.
            {
                RailShot shot = ArmoryShot<RailShot>("PlayerRail", M("RailSlug"), new Vector2(0.32f, 3.4f), 0.22f, WeaponRules.Lifetime(WeaponType.Rail),
                    WeaponRules.Tint(WeaponType.Rail), new Color(0.6f, 0.85f, 1f), 0.16f);
                shot.pierce = WeaponRules.Pierce(WeaponType.Rail, 1);
                Save(shot.gameObject, "Shots/PlayerRail");
            }

            // Flak Burster: an amber shell that bursts forward into shrapnel near a target or at the end of its range.
            {
                BurstShot shot = ArmoryShot<BurstShot>("PlayerFlak", M("FlakShell"), new Vector2(0.6f, 0.6f), 0.22f, WeaponRules.Lifetime(WeaponType.Flak),
                    WeaponRules.Tint(WeaponType.Flak), new Color(1f, 0.7f, 0.25f), 0.08f);
                shot.fragments = WeaponRules.Fragments(WeaponType.Flak, 1);
                shot.fragmentKind = BurstShot.FieldShrapnel;
                shot.fragmentDamage = WeaponRules.FragmentDamage(WeaponType.Flak, 1);
                shot.fragmentSpeed = 15f;
                shot.fragmentLifetime = 0.32f;
                shot.fragmentCone = 160f;
                shot.fragmentInherit = 0.35f;
                shot.fuseRadius = 1.4f;
                shot.fuseLayers = Altitude.Both;
                shot.flashSize = 0.5f;
                shot.burstTint = WeaponRules.Tint(WeaponType.Flak);
                shot.sound = BurstShot.BurstSound.Flak;
                Save(shot.gameObject, "Shots/PlayerFlak");
            }
            {
                Shot shot = ArmoryShot<Shot>("FlakShrapnel", M("FlakShrapnel"), new Vector2(0.2f, 0.6f), 0.15f, 0.32f, WeaponRules.Tint(WeaponType.Flak), null, 0f);
                shot.damage = WeaponRules.FragmentDamage(WeaponType.Flak, 1);
                Save(shot.gameObject, "Shots/FlakShrapnel");
            }

            // Ion Wave: a teal crescent that widens as it flies.
            {
                WaveShot shot = ArmoryShot<WaveShot>("PlayerWave", M("IonWave"), new Vector2(1.1f, 0.55f), 0.4f, WeaponRules.Lifetime(WeaponType.Wave),
                    WeaponRules.Tint(WeaponType.Wave), null, 0f);
                shot.startRadius = 0.4f;
                shot.growth = WeaponRules.Growth(WeaponType.Wave, 1);
                shot.pierce = WeaponRules.Pierce(WeaponType.Wave, 1);
                Save(shot.gameObject, "Shots/PlayerWave");
            }
        }


        /// <summary>
        /// A glowing projectile of the pilot's with a component of <typeparamref name="T"/>: a quad of
        /// <paramref name="material"/> under "Visual" and, with a <paramref name="trail"/> colour, a trail of
        /// <paramref name="trailTime"/> seconds. Not saved yet: the caller sets its own numbers first.
        /// </summary>
        private static T ArmoryShot<T>(string name, Material material, Vector2 size, float radius, float lifetime, Color impact, Color? trail, float trailTime,
            bool align = true) where T : Shot
        {
            var root = new GameObject(name);
            var shot = root.AddComponent<T>();
            Transform visual = Child(root.transform, "Visual");
            Quad(visual, "Glow", material, Vector3.zero, size);
            shot.visual = visual;
            shot.enemy = false;
            shot.damage = 1f;
            shot.pierce = 1;
            shot.radius = radius;
            shot.lifetime = lifetime;
            shot.alignToVelocity = align;
            shot.impactTint = impact;
            shot.wraps = false;
            shot.pulledByGravity = false;
            shot.altitude = Altitude.Air;
            if (trail.HasValue)
            {
                AddTrail(root, shot, trail.Value, size.x * 0.5f, trailTime);
            }
            return shot;
        }


        // ------------------------------------------------------------------ strike shots

        private static void BuildArmoryStrikeShots()
        {
            StrikeWeaponInfo flak = StrikeWeaponRules.Info(StrikeItem.FlakCannon);
            {
                BurstShot shot = ArmoryShot<BurstShot>(StrikeShotKind.FlakShell.ToString(), M("FlakShell"), new Vector2(0.65f, 0.65f), flak.ShotRadius, flak.Lifetime,
                    new Color(1f, 0.8f, 0.35f), new Color(1f, 0.7f, 0.3f), 0.08f);
                StrikeNumbers(shot, flak);
                shot.fragments = flak.Fragments;
                shot.fragmentKind = (int)flak.FragmentKind;
                shot.fragmentDamage = flak.FragmentDamage;
                shot.fragmentSpeed = 12f;
                shot.fragmentLifetime = 0.26f;
                shot.fragmentCone = 360f;
                shot.fragmentInherit = 0.2f;
                shot.fuseRadius = flak.FuseRadius;
                shot.fuseLayers = Altitude.Air;
                shot.flashSize = 0.8f;
                shot.burstTint = new Color(1f, 0.75f, 0.3f);
                shot.sound = BurstShot.BurstSound.Flak;
                SaveStrike(shot.gameObject, $"Shots/{StrikeShotKind.FlakShell}");
            }
            {
                Shot shot = ArmoryShot<Shot>(StrikeShotKind.FlakShard.ToString(), M("FlakShrapnel"), new Vector2(0.2f, 0.6f), 0.18f, 0.26f, new Color(1f, 0.75f, 0.3f), null, 0f);
                shot.damage = flak.FragmentDamage;
                shot.reach = Altitude.Air;
                SaveStrike(shot.gameObject, $"Shots/{StrikeShotKind.FlakShard}");
            }

            StrikeWeaponInfo cluster = StrikeWeaponRules.Info(StrikeItem.ClusterBombs);
            {
                BurstShot shot = Canister<BurstShot>(StrikeShotKind.ClusterShell.ToString(), 0.95f, StrikeTint("ClusterShellGlow", new Color(2.6f, 1.2f, 0.35f, 0.4f)));
                StrikeNumbers(shot, cluster);
                shot.reach = Altitude.None;
                shot.fragments = cluster.Fragments;
                shot.fragmentKind = (int)cluster.FragmentKind;
                shot.fragmentDamage = cluster.FragmentDamage;
                shot.fragmentSpeed = 7f;
                shot.fragmentLifetime = 0.34f;
                shot.fragmentCone = 360f;
                shot.fragmentInherit = 0f;
                shot.scatter = 0.4f;
                shot.flashSize = 0.6f;
                shot.burstTint = new Color(1f, 0.6f, 0.25f);
                shot.sound = BurstShot.BurstSound.Cluster;
                SaveStrike(shot.gameObject, $"Shots/{StrikeShotKind.ClusterShell}");
            }
            {
                Shot shot = ArmoryShot<Shot>(StrikeShotKind.Bomblet.ToString(), SM("ClusterGlow"), new Vector2(0.9f, 0.9f), 0.2f, 0.34f, new Color(1f, 0.6f, 0.25f), null, 0f);
                Quad(shot.visual, "Core", M("FlakShell"), new Vector3(0f, 0f, -0.05f), new Vector2(0.38f, 0.38f));
                shot.damage = cluster.FragmentDamage;
                shot.reach = Altitude.None;
                shot.burstOnExpire = true;
                shot.burstRadius = cluster.BurstRadius;
                shot.burstDamage = cluster.BurstDamage;
                SaveStrike(shot.gameObject, $"Shots/{StrikeShotKind.Bomblet}");
            }

            StrikeWeaponInfo napalm = StrikeWeaponRules.Info(StrikeItem.Napalm);
            {
                BurstShot shot = Canister<BurstShot>(StrikeShotKind.NapalmCanister.ToString(), 1f, SM("NapalmFire"));
                StrikeNumbers(shot, napalm);
                shot.reach = Altitude.None;
                shot.fragments = napalm.Fragments;
                shot.fragmentKind = (int)napalm.FragmentKind;
                shot.fragmentDamage = napalm.FragmentDamage;
                shot.fragmentSpeed = 0f;
                shot.fragmentLifetime = 3f;
                shot.fragmentCone = 360f;
                shot.fragmentInherit = 0f;
                shot.flashSize = 0.9f;
                shot.burstTint = new Color(1f, 0.5f, 0.15f);
                shot.sound = BurstShot.BurstSound.Napalm;
                SaveStrike(shot.gameObject, $"Shots/{StrikeShotKind.NapalmCanister}");
            }
            NapalmFire(napalm);

            StrikeWeaponInfo swarm = StrikeWeaponRules.Info(StrikeItem.SwarmRockets);
            {
                GameObject root = MissileBody(StrikeShotKind.SwarmRocket.ToString(), 0.13f, new Color(0.6f, 1f, 0.5f), true, out Shot shot);
                StrikeNumbers(shot, swarm);
                shot.radius = 0.2f;
                shot.homing = swarm.Homing;
                shot.homingTime = 1.2f;
                shot.homingRange = 14f;
                shot.wobble = 0.2f;
                shot.impactTint = new Color(0.6f, 1f, 0.5f);
                SaveStrike(root, $"Shots/{StrikeShotKind.SwarmRocket}");
            }

            StrikeWeaponInfo arc = StrikeWeaponRules.Info(StrikeItem.ArcLightning);
            {
                ArcShot shot = ArmoryShot<ArcShot>(StrikeShotKind.ArcBolt.ToString(), M("ArcSpark"), new Vector2(0.95f, 0.95f), arc.ShotRadius, arc.Lifetime,
                    new Color(0.75f, 0.6f, 1f), new Color(0.6f, 0.45f, 1f), 0.1f);
                Quad(shot.visual, "Core", M("Bolt"), new Vector3(0f, 0f, -0.05f), new Vector2(0.22f, 0.85f));
                StrikeNumbers(shot, arc);
                shot.chains = arc.Chains;
                shot.chainRange = arc.ChainRange;
                shot.chainDamage = arc.ChainDamage;
                shot.arcTint = new Color(0.7f, 0.55f, 1f);
                SaveStrike(shot.gameObject, $"Shots/{StrikeShotKind.ArcBolt}");
            }

            StrikeWeaponInfo rail = StrikeWeaponRules.Info(StrikeItem.RailGun);
            {
                RailShot shot = ArmoryShot<RailShot>(StrikeShotKind.RailSlug.ToString(), M("RailSlug"), new Vector2(0.4f, 4.2f), rail.ShotRadius, rail.Lifetime,
                    new Color(0.85f, 0.95f, 1f), new Color(0.6f, 0.85f, 1f), 0.18f);
                StrikeNumbers(shot, rail);
                SaveStrike(shot.gameObject, $"Shots/{StrikeShotKind.RailSlug}");
            }
        }


        /// <summary>The numbers of a strike special on its projectile: damage, pierce, reach, lifetime and the acceleration to its top speed.</summary>
        private static void StrikeNumbers(Shot shot, StrikeWeaponInfo info)
        {
            shot.enemy = false;
            shot.damage = info.Damage;
            shot.pierce = Mathf.Max(1, info.Pierce);
            shot.lifetime = info.Lifetime;
            shot.reach = info.Mask;
            shot.acceleration = info.Acceleration;
            shot.maxSpeed = info.MaxSpeed > info.Speed ? info.MaxSpeed : 0f;
            shot.altitude = Altitude.Air;
        }


        /// <summary>A canister that climbs like the MK-133 bomb (the pack's bomb model and a glow of its own), not saved yet.</summary>
        private static T Canister<T>(string name, float length, Material glow) where T : Shot
        {
            var root = new GameObject(name);
            var shot = root.AddComponent<T>();
            Transform visual = Child(root.transform, "Visual");
            Mesh mesh = AsteroidsArtBuilder.PackMesh("BonusContent/MissilesSample.FBX", "MissileBomb");
            float scale = length / Mathf.Max(0.01f, mesh != null ? mesh.bounds.size.z : 1f);
            Model(visual, "Model", mesh, AsteroidsArtBuilder.PackMaterial("BonusContent/Missile"), -(FaceCamera * (mesh != null ? mesh.bounds.center : Vector3.zero)) * scale,
                FaceCamera, Vector3.one * scale);
            Quad(visual, "Glow", glow, new Vector3(0f, 0f, 0.2f), Vector2.one * length * 1.3f);
            shot.visual = visual;
            shot.enemy = false;
            shot.pierce = 1;
            shot.radius = 0.3f;
            shot.alignToVelocity = true;
            shot.impactTint = new Color(1f, 0.6f, 0.25f);
            shot.wraps = false;
            shot.pulledByGravity = false;
            shot.altitude = Altitude.Air;
            return shot;
        }


        /// <summary>
        /// The napalm's burning ground: a scorched patch the size of the fire, a hot glow over it and flames that flicker up
        /// all over it (moving with the patch, which the scroll carries).
        /// </summary>
        private static void NapalmFire(StrikeWeaponInfo napalm)
        {
            var root = new GameObject(StrikeShotKind.NapalmFire.ToString());
            var shot = root.AddComponent<FireShot>();
            Transform visual = Child(root.transform, "Visual");
            float size = napalm.BurstRadius * 2.1f;
            Quad(visual, "Scorch", SM("NapalmScorch"), new Vector3(0f, 0f, 0.3f), Vector2.one * size);
            Quad(visual, "Glow", SM("NapalmFire"), new Vector3(0f, 0f, 0.1f), Vector2.one * size * 1.1f);
            ParticleSystem flames = ParticleFactory.Create("Flames", root.transform, M("ParticleAdd"));
            ParticleFactory.Circle(flames, napalm.BurstRadius * 0.8f, false);
            ParticleFactory.Lifetime(flames, 0.35f, 0.75f);
            ParticleFactory.Speed(flames, 0.2f, 1.2f);
            ParticleFactory.Size(flames, 0.5f, 1.15f);
            ParticleFactory.Rate(flames, 55f);
            ParticleFactory.SizeOverLife(flames, 0.6f, 1.2f);
            ColorOverLife(flames, new Color(1f, 0.95f, 0.6f), new Color(1f, 0.5f, 0.12f), new Color(0.45f, 0.08f, 0.03f));
            ParticleFactory.MaxParticles(flames, 80);
            ParticleSystem.MainModule main = flames.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            ParticleSystem smoke = ParticleFactory.Create("Smoke", flames.transform, M("ParticleSmoke"));
            ParticleFactory.Circle(smoke, napalm.BurstRadius * 0.7f, false);
            ParticleFactory.Lifetime(smoke, 0.8f, 1.4f);
            ParticleFactory.Speed(smoke, 0.3f, 1f);
            ParticleFactory.Size(smoke, 0.9f, 1.6f);
            ParticleFactory.Rate(smoke, 9f);
            ParticleFactory.Colors(smoke, new Color(0.15f, 0.12f, 0.1f, 0.5f), new Color(0.25f, 0.2f, 0.18f, 0.4f));
            ParticleFactory.SizeOverLife(smoke, 0.7f, 1.6f);
            ParticleFactory.FadeOut(smoke, 0.2f);
            ParticleFactory.MaxParticles(smoke, 24);
            ParticleSystem.MainModule smokeMain = smoke.main;
            smokeMain.simulationSpace = ParticleSystemSimulationSpace.Local;
            shot.visual = visual;
            shot.exhaust = flames;
            shot.flames = flames;
            shot.burns = Altitude.Ground;
            shot.enemy = false;
            shot.damage = napalm.FragmentDamage;
            shot.pierce = 1;
            shot.radius = napalm.BurstRadius;
            shot.lifetime = 3f;
            shot.reach = Altitude.None;
            shot.alignToVelocity = false;
            shot.impactTint = new Color(1f, 0.5f, 0.15f);
            shot.wraps = false;
            shot.pulledByGravity = false;
            shot.altitude = Altitude.Ground;
            SaveStrike(root, $"Shots/{StrikeShotKind.NapalmFire}");
        }


        // ------------------------------------------------------------------ effects

        private static void BuildArmoryEffects()
        {
            // A bolt of lightning: a bright jagged core over a wide soft glow (LightningArc strikes and fades it).
            {
                var root = new GameObject("Lightning");
                var arc = root.AddComponent<LightningArc>();
                arc.glow = LightningLine(root.transform, "Glow", M("Lightning"));
                arc.core = LightningLine(root.transform, "Core", M("LightningCore"));
                arc.duration = 0.2f;
                arc.jag = 0.18f;
                arc.coreWidth = 0.14f;
                arc.glowWidth = 0.65f;
                arc.restrike = 0.05f;
                arc.depth = -0.15f;
                Save(root, "Effects/Lightning");
            }

            // The burst of a flak shell: a flash, hot sparks flung out and a puff of smoke.
            {
                PooledEffect effect = Effect("FlakBurst", 1.2f, out Transform root);
                ParticleSystem flare = Burst(root, "Flare", M("ParticleFlare"), 1, 0.08f, 0.12f, 0f, 0f, 1.8f, 2.2f, Color.white, Color.white);
                ParticleFactory.SizeOverLife(flare, 1f, 0.3f);
                ParticleSystem sparks = Burst(root, "Sparks", M("ParticleSpark"), 12, 0.15f, 0.35f, 5f, 11f, 0.08f, 0.14f, Color.white, Color.white);
                Flat(sparks);
                Stretch(sparks, 0.05f);
                ParticleFactory.FadeOut(sparks);
                ParticleSystem smoke = Burst(root, "Smoke", M("ParticleSmoke"), 5, 0.5f, 0.9f, 0.3f, 1.2f, 0.6f, 1.1f, new Color(0.2f, 0.18f, 0.17f, 0.6f),
                    new Color(0.32f, 0.28f, 0.26f, 0.45f));
                Flat(smoke);
                ParticleFactory.SizeOverLife(smoke, 0.6f, 1.5f);
                ParticleFactory.FadeOut(smoke);
                foreach (ParticleSystem system in new[] { flare, sparks, smoke })
                {
                    ParticleSystem.MainModule main = system.main;
                    main.simulationSpace = ParticleSystemSimulationSpace.Local;
                }
                Ring(root, effect, 0.22f, 2.2f);
                effect.systems = new[] { flare, sparks, smoke };
                effect.tinted = new[] { sparks, flare };
                effect.scaleSpeed = false;
                Save(root.gameObject, "Effects/FlakBurst");
            }
        }


        /// <summary>A line of the lightning: ten points in world space, facing the camera, hidden until struck.</summary>
        private static LineRenderer LightningLine(Transform parent, string name, Material material)
        {
            Transform t = Child(parent, name);
            var line = t.gameObject.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = true;
            line.positionCount = 10;
            var positions = new Vector3[10];
            for (int i = 0; i < positions.Length; i++)
            {
                positions[i] = Vector3.up * i * 0.2f;
            }
            line.SetPositions(positions);
            line.widthMultiplier = 0.2f;
            line.textureMode = LineTextureMode.Stretch;
            line.alignment = LineAlignment.View;
            line.numCapVertices = 2;
            line.numCornerVertices = 1;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;
            return line;
        }


        // ------------------------------------------------------------------ pickups

        /// <summary>The pickups of the armory's strike items a level may place (the frame, a core of the item's colour and its icon).</summary>
        private static void BuildArmoryPickups()
        {
            foreach (StrikeItem item in ArmoryStrikePickups)
            {
                if (!StrikeArmory.Info(item).CanBePickup)
                {
                    throw new InvalidOperationException($"{item} is priced over the pickup limit: a level may not place it.");
                }
                ItemPickup(item.ToString(), item, item.ToString(), PickupColor(item), 0);
            }
        }


        private static Color PickupColor(StrikeItem item)
        {
            switch (item)
            {
                case StrikeItem.FlakCannon: return new Color(0.45f, 0.8f, 1f);
                case StrikeItem.ArmorPlating: return new Color(0.7f, 0.8f, 0.95f);
                case StrikeItem.Afterburner: return new Color(1f, 0.7f, 0.3f);
                case StrikeItem.SalvageMagnet: return new Color(1f, 0.4f, 0.45f);
                default: return new Color(0.45f, 1f, 0.55f);
            }
        }
    }
}
