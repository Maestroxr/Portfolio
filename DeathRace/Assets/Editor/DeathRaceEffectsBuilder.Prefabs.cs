using Gamebox.Editor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Portfolio.DeathRace.EditorTools
{
    internal static partial class DeathRaceEffectsBuilder
    {
        public const string PrefabFolder = DeathRacePrefabBuilder.EffectFolder;

        public static readonly string[] Names =
        {
            "Explosion", "BigExplosion", "Sparks", "Smoke", "BlackSmoke", "MuzzleFlash", "Fire", "NitroFlame", "Dust", "Scorch",
            "FlameJet", "PlasmaBurst", "EmpBlast", "Electric", "Weld"
        };

        public static GameObject Prefab(string name)
        {
            return DeathRaceAssets.Load<GameObject>($"{PrefabFolder}/{name}.prefab");
        }

        /// <summary>Every effect prefab (after <see cref="BuildArt"/>).</summary>
        public static void BuildPrefabs()
        {
            Save(Explosion("Explosion", 1f, false));
            Save(Explosion("BigExplosion", 1f, true));
            Save(Sparks());
            Save(Smoke("Smoke", new Color(0.8f, 0.8f, 0.8f, 0.78f), new Color(0.64f, 0.64f, 0.66f, 0.7f), 16f, false));
            Save(Smoke("BlackSmoke", new Color(0.36f, 0.34f, 0.33f, 0.9f), new Color(0.4f, 0.38f, 0.36f, 0.86f), 22f, true));
            Save(MuzzleFlash());
            Save(Fire());
            Save(NitroFlame());
            Save(Dust());
            Save(Scorch());
            BuildWeaponPrefabs();
        }

        private static void Save(GameObject root)
        {
            DeathRaceAssets.SavePrefab(root, $"{PrefabFolder}/{root.name}.prefab");
        }

        // ------------------------------------------------------------------ helpers

        private static ParticleSystem System(Transform parent, string name, string material, bool loop, float duration = 0.25f)
        {
            ParticleSystem system = ParticleFactory.Create(name, parent, EffectMaterial(material));
            ParticleSystem.MainModule main = system.main;
            main.loop = loop;
            main.duration = duration;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.stopAction = ParticleSystemStopAction.None;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = false;
            return system;
        }

        private static void Burst(ParticleSystem system, int count, float time = 0f)
        {
            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(time, (short)count) });
            ParticleFactory.MaxParticles(system, Mathf.Max(count + 2, 4));
        }

        private static void Delay(ParticleSystem system, float delay)
        {
            ParticleSystem.MainModule main = system.main;
            main.startDelay = delay;
        }

        /// <summary>Colour (and alpha) over the life of a particle, keys at the given times.</summary>
        private static void Over(ParticleSystem system, params (float time, Color color)[] keys)
        {
            ParticleSystem.ColorOverLifetimeModule over = system.colorOverLifetime;
            over.enabled = true;
            var colors = new GradientColorKey[keys.Length];
            var alphas = new GradientAlphaKey[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                colors[i] = new GradientColorKey(keys[i].color, keys[i].time);
                alphas[i] = new GradientAlphaKey(keys[i].color.a, keys[i].time);
            }
            var gradient = new Gradient();
            gradient.SetKeys(colors, alphas);
            over.color = new ParticleSystem.MinMaxGradient(gradient);
        }

        private static void Grow(ParticleSystem system, params Keyframe[] keys)
        {
            ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(keys));
        }

        private static void Stretch(ParticleSystem system, float velocityScale, float lengthScale = 1f)
        {
            var renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = velocityScale;
            renderer.lengthScale = lengthScale;
        }

        private static void Flat(ParticleSystem system)
        {
            system.GetComponent<ParticleSystemRenderer>().renderMode = ParticleSystemRenderMode.HorizontalBillboard;
        }

        /// <summary>Emission in a cone along +z (sparks, flames).</summary>
        private static void Forward(ParticleSystem system, float angle, float radius)
        {
            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = angle;
            shape.radius = radius;
            shape.rotation = Vector3.zero;
        }

        private static void Ball(ParticleSystem system, float radius, bool upper = false)
        {
            ParticleFactory.Sphere(system, radius, upper);
        }

        private static void Rise(ParticleSystem system, float min, float max)
        {
            ParticleFactory.Velocity(system, new Vector3(0f, min, 0f), new Vector3(0f, max, 0f));
        }

        private static void Order(ParticleSystem system, int order)
        {
            system.GetComponent<ParticleSystemRenderer>().sortingFudge = -order;
        }

        private static GameObject Root(string name)
        {
            return new GameObject(name);
        }

        private static void FlashLight(Transform parent, float range, float intensity, Color color)
        {
            var go = new GameObject("Light");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = range;
            light.intensity = intensity;
            light.color = color;
            light.shadows = LightShadows.None;
            light.renderMode = LightRenderMode.Auto;
            light.enabled = false;
        }

        private static readonly Color Hot = new Color(1f, 0.96f, 0.82f, 1f);

        // ------------------------------------------------------------------ explosions

        /// <summary>
        /// A blast: a white flash, a hot core, a tinted fireball that darkens into smoke, sparks, tumbling debris, a shock
        /// ring on the ground and a flash light. The big one (a wreck) is larger, has a second fireball, more debris (with
        /// a tyre) and a smoke column that lingers.
        /// </summary>
        private static GameObject Explosion(string name, float scale, bool big)
        {
            GameObject root = Root(name);
            Transform t = root.transform;
            float s = scale;

            ParticleSystem flash = System(t, "FlashTint", "FlareAdd", false);
            Burst(flash, 1);
            ParticleFactory.Lifetime(flash, 0.12f, 0.16f);
            ParticleFactory.Speed(flash, 0f, 0f);
            ParticleFactory.Size(flash, 6f * s, 7f * s);
            Grow(flash, new Keyframe(0f, 0.6f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0.2f));
            Over(flash, (0f, Color.white), (1f, new Color(1f, 1f, 1f, 0f)));
            Order(flash, 3);

            ParticleSystem core = System(t, "Core", "FireAdd", false);
            Burst(core, big ? 10 : 6);
            ParticleFactory.Lifetime(core, 0.18f, 0.32f);
            ParticleFactory.Speed(core, 0.5f * s, 2f * s);
            ParticleFactory.Size(core, 1.4f * s, 2.2f * s);
            ParticleFactory.Colors(core, Hot, new Color(1f, 0.85f, 0.55f, 1f));
            Ball(core, 0.3f * s);
            Grow(core, new Keyframe(0f, 0.7f), new Keyframe(1f, 1.3f));
            Over(core, (0f, Color.white), (0.6f, new Color(1f, 0.9f, 0.7f, 0.8f)), (1f, new Color(1f, 0.6f, 0.3f, 0f)));
            ParticleFactory.Spin(core, 90f);
            Order(core, 2);

            ParticleSystem fire = System(t, "FireTint", "FireAdd", false);
            Burst(fire, big ? 30 : 18);
            ParticleFactory.Lifetime(fire, 0.45f, 0.95f);
            ParticleFactory.Speed(fire, 1.5f * s, 5f * s);
            ParticleFactory.Size(fire, 1.1f * s, 2.3f * s);
            Ball(fire, 0.6f * s, true);
            Grow(fire, new Keyframe(0f, 0.55f), new Keyframe(0.35f, 1.15f), new Keyframe(1f, 1.4f));
            Over(fire, (0f, Color.white), (0.35f, new Color(1f, 0.82f, 0.7f, 0.95f)), (0.7f, new Color(0.55f, 0.28f, 0.18f, 0.55f)), (1f, new Color(0.2f, 0.12f, 0.1f, 0f)));
            ParticleFactory.Spin(fire, 120f);
            ParticleFactory.Noise(fire, 0.6f * s, 0.8f);
            ParticleFactory.Gravity(fire, -0.25f);
            ParticleSystem.LimitVelocityOverLifetimeModule drag = fire.limitVelocityOverLifetime;
            drag.enabled = true;
            drag.limit = 0.6f * s;
            drag.dampen = 0.25f;

            if (big)
            {
                ParticleSystem second = System(t, "SecondTint", "FireAdd", false);
                Burst(second, 14);
                Delay(second, 0.16f);
                ParticleFactory.Lifetime(second, 0.5f, 0.9f);
                ParticleFactory.Speed(second, 1f * s, 3f * s);
                ParticleFactory.Size(second, 1.2f * s, 2f * s);
                Ball(second, 0.9f * s, true);
                Grow(second, new Keyframe(0f, 0.5f), new Keyframe(0.4f, 1.1f), new Keyframe(1f, 1.3f));
                Over(second, (0f, Color.white), (0.5f, new Color(0.9f, 0.6f, 0.45f, 0.8f)), (1f, new Color(0.2f, 0.12f, 0.1f, 0f)));
                ParticleFactory.Spin(second, 100f);
                ParticleFactory.Gravity(second, -0.3f);
            }

            ParticleSystem smoke = System(t, "Smoke", "Smoke", false);
            Burst(smoke, big ? 16 : 10);
            Delay(smoke, 0.08f);
            ParticleFactory.Lifetime(smoke, big ? 1.8f : 1.2f, big ? 3.2f : 2.2f);
            ParticleFactory.Speed(smoke, 0.6f * s, 2.2f * s);
            ParticleFactory.Size(smoke, 1.8f * s, 3.2f * s);
            ParticleFactory.Colors(smoke, new Color(0.16f, 0.15f, 0.15f, 0.8f), new Color(0.3f, 0.28f, 0.27f, 0.7f));
            Ball(smoke, 0.8f * s, true);
            Rise(smoke, 1f * s, 2.5f * s);
            Grow(smoke, new Keyframe(0f, 0.5f), new Keyframe(1f, 1.9f));
            Over(smoke, (0f, new Color(1f, 1f, 1f, 0f)), (0.12f, Color.white), (0.6f, new Color(1f, 1f, 1f, 0.6f)), (1f, new Color(1f, 1f, 1f, 0f)));
            ParticleFactory.Spin(smoke, 40f);
            ParticleFactory.Noise(smoke, 0.4f * s, 0.5f);
            Order(smoke, -1);

            ParticleSystem sparks = System(t, "SparksTint", "SparkAdd", false);
            Burst(sparks, big ? 40 : 24);
            ParticleFactory.Lifetime(sparks, 0.35f, 0.8f);
            ParticleFactory.Speed(sparks, 8f * s, 19f * s);
            ParticleFactory.Size(sparks, 0.1f * s, 0.2f * s);
            Ball(sparks, 0.3f * s, true);
            ParticleFactory.Gravity(sparks, 1.6f);
            Stretch(sparks, 0.05f, 1.5f);
            Over(sparks, (0f, Color.white), (0.7f, new Color(1f, 0.8f, 0.5f, 1f)), (1f, new Color(1f, 0.4f, 0.1f, 0f)));

            ParticleSystem debris = System(t, "Debris", "Debris", false);
            Burst(debris, big ? 14 : 8);
            ParticleFactory.Lifetime(debris, 0.9f, 1.6f);
            ParticleFactory.Speed(debris, 4f * s, 10f * s);
            ParticleFactory.Size(debris, 0.16f * s, 0.38f * s);
            ParticleFactory.RandomColors(debris, new Color(0.16f, 0.16f, 0.17f), new Color(0.3f, 0.3f, 0.32f), new Color(0.45f, 0.26f, 0.15f), new Color(0.1f, 0.1f, 0.1f));
            Ball(debris, 0.4f * s, true);
            ParticleFactory.Gravity(debris, 2.6f);
            ParticleFactory.Tumble(debris, 540f);
            ParticleFactory.MeshParticles(debris, EffectMesh(big ? "Shard" : "Chunk"));
            Grow(debris, new Keyframe(0f, 1f), new Keyframe(0.8f, 1f), new Keyframe(1f, 0f));
            var debrisRenderer = debris.GetComponent<ParticleSystemRenderer>();
            debrisRenderer.shadowCastingMode = ShadowCastingMode.On;

            if (big)
            {
                ParticleSystem chunks = System(t, "Chunks", "Debris", false);
                Burst(chunks, 8);
                ParticleFactory.Lifetime(chunks, 1f, 1.8f);
                ParticleFactory.Speed(chunks, 3f * s, 8f * s);
                ParticleFactory.Size(chunks, 0.25f * s, 0.45f * s);
                ParticleFactory.RandomColors(chunks, new Color(0.14f, 0.14f, 0.15f), new Color(0.35f, 0.2f, 0.12f), new Color(0.25f, 0.25f, 0.27f));
                Ball(chunks, 0.5f * s, true);
                ParticleFactory.Gravity(chunks, 2.4f);
                ParticleFactory.Tumble(chunks, 400f);
                ParticleFactory.MeshParticles(chunks, EffectMesh("Chunk"));
                Grow(chunks, new Keyframe(0f, 1f), new Keyframe(0.85f, 1f), new Keyframe(1f, 0f));

                ParticleSystem tyre = System(t, "Tyre", "Debris", false);
                Burst(tyre, 1);
                ParticleFactory.Lifetime(tyre, 1.4f, 1.6f);
                ParticleFactory.Speed(tyre, 5f, 7f);
                ParticleFactory.Size(tyre, 0.85f, 0.95f);
                ParticleFactory.Colors(tyre, new Color(0.12f, 0.12f, 0.13f), new Color(0.16f, 0.16f, 0.17f));
                Ball(tyre, 0.3f, true);
                ParticleFactory.Gravity(tyre, 2.2f);
                ParticleFactory.Tumble(tyre, 360f);
                ParticleFactory.MeshParticles(tyre, EffectMesh("Tyre"));
                Grow(tyre, new Keyframe(0f, 1f), new Keyframe(0.9f, 1f), new Keyframe(1f, 0f));

                ParticleSystem column = System(t, "Column", "Smoke", false, 1.2f);
                ParticleSystem.EmissionModule emission = column.emission;
                emission.rateOverTime = 10f;
                Delay(column, 0.35f);
                ParticleFactory.MaxParticles(column, 40);
                ParticleFactory.Lifetime(column, 2.4f, 3.6f);
                ParticleFactory.Speed(column, 0.2f, 0.6f);
                ParticleFactory.Size(column, 2f, 3.2f);
                ParticleFactory.Colors(column, new Color(0.1f, 0.09f, 0.09f, 0.7f), new Color(0.22f, 0.2f, 0.19f, 0.6f));
                Ball(column, 1f);
                Rise(column, 2.5f, 4f);
                Grow(column, new Keyframe(0f, 0.6f), new Keyframe(1f, 2.4f));
                Over(column, (0f, new Color(1f, 1f, 1f, 0f)), (0.15f, Color.white), (1f, new Color(1f, 1f, 1f, 0f)));
                ParticleFactory.Spin(column, 30f);
                ParticleFactory.Noise(column, 0.5f, 0.4f);
                Order(column, -2);
            }

            ParticleSystem ring = System(t, "Ring", "RingAdd", false);
            Burst(ring, 1);
            ParticleFactory.Lifetime(ring, 0.34f, 0.36f);
            ParticleFactory.Speed(ring, 0f, 0f);
            ParticleFactory.Size(ring, 1.5f * s, 1.5f * s);
            ParticleFactory.Colors(ring, new Color(1f, 0.85f, 0.6f, 0.8f), new Color(1f, 0.85f, 0.6f, 0.8f));
            Flat(ring);
            Grow(ring, new Keyframe(0f, 1f), new Keyframe(1f, 5.5f));
            Over(ring, (0f, Color.white), (1f, new Color(1f, 1f, 1f, 0f)));
            ring.transform.localPosition = new Vector3(0f, 0.08f, 0f);

            FlashLight(t, (big ? 18f : 11f) * s, big ? 9f : 6f, new Color(1f, 0.62f, 0.25f));
            return root;
        }

        // ------------------------------------------------------------------ hits, flashes and trails

        /// <summary>Metal hit: a spray of stretched sparks along +z and a pin-prick flash.</summary>
        private static GameObject Sparks()
        {
            GameObject root = Root("Sparks");
            Transform t = root.transform;
            ParticleSystem sparks = System(t, "SparksTint", "SparkAdd", false, 0.1f);
            Burst(sparks, 14);
            ParticleFactory.Lifetime(sparks, 0.14f, 0.36f);
            ParticleFactory.Speed(sparks, 4f, 11f);
            ParticleFactory.Size(sparks, 0.06f, 0.11f);
            Forward(sparks, 38f, 0.05f);
            ParticleFactory.Gravity(sparks, 1.2f);
            Stretch(sparks, 0.045f, 1.2f);
            Over(sparks, (0f, Color.white), (0.6f, new Color(1f, 0.85f, 0.55f, 1f)), (1f, new Color(1f, 0.45f, 0.15f, 0f)));

            ParticleSystem flash = System(t, "FlashTint", "FlareAdd", false, 0.1f);
            Burst(flash, 1);
            ParticleFactory.Lifetime(flash, 0.05f, 0.07f);
            ParticleFactory.Speed(flash, 0f, 0f);
            ParticleFactory.Size(flash, 0.7f, 0.9f);
            ParticleFactory.Spin(flash, 0f);
            Over(flash, (0f, Color.white), (1f, new Color(1f, 1f, 1f, 0f)));

            ParticleSystem puff = System(t, "Puff", "Smoke", false, 0.1f);
            Burst(puff, 2);
            ParticleFactory.Lifetime(puff, 0.3f, 0.5f);
            ParticleFactory.Speed(puff, 0.5f, 1.2f);
            ParticleFactory.Size(puff, 0.35f, 0.55f);
            ParticleFactory.Colors(puff, new Color(0.55f, 0.55f, 0.55f, 0.35f), new Color(0.7f, 0.7f, 0.7f, 0.3f));
            Forward(puff, 30f, 0.05f);
            Grow(puff, new Keyframe(0f, 0.6f), new Keyframe(1f, 1.6f));
            Over(puff, (0f, Color.white), (1f, new Color(1f, 1f, 1f, 0f)));
            return root;
        }

        /// <summary>Smoke pouring off a damaged car (light grey) or a dying one (born black, greying as it rises, with embers).</summary>
        private static GameObject Smoke(string name, Color from, Color to, float rate, bool embers)
        {
            GameObject root = Root(name);
            Transform t = root.transform;
            ParticleSystem smoke = System(t, "Smoke", "Smoke", true, 1f);
            ParticleFactory.Rate(smoke, rate);
            ParticleFactory.MaxParticles(smoke, 80);
            ParticleFactory.Lifetime(smoke, embers ? 1.8f : 1.1f, embers ? 2.8f : 1.7f);
            ParticleFactory.Speed(smoke, 0.1f, 0.5f);
            ParticleFactory.Size(smoke, embers ? 0.9f : 0.8f, embers ? 1.4f : 1.3f);
            ParticleFactory.Colors(smoke, from, to);
            Ball(smoke, 0.3f);
            Rise(smoke, embers ? 2.2f : 1.2f, embers ? 3.6f : 2f);
            Grow(smoke, new Keyframe(0f, 0.5f), new Keyframe(1f, embers ? 3f : 2.3f));
            if (embers)
            {
                // Born dark over the fire, greying as it climbs, so the column reads on sand and on asphalt alike.
                Over(smoke, (0f, new Color(0.3f, 0.3f, 0.3f, 0f)), (0.1f, new Color(0.32f, 0.32f, 0.32f, 1f)), (0.55f, new Color(0.75f, 0.75f, 0.75f, 0.75f)), (1f, new Color(1f, 1f, 1f, 0f)));
            }
            else
            {
                Over(smoke, (0f, new Color(1f, 1f, 1f, 0f)), (0.1f, Color.white), (0.6f, new Color(1f, 1f, 1f, 0.55f)), (1f, new Color(1f, 1f, 1f, 0f)));
            }
            ParticleFactory.Spin(smoke, 45f);
            ParticleFactory.Noise(smoke, 0.35f, 0.6f);
            if (embers)
            {
                ParticleSystem sparks = System(t, "Embers", "SparkAdd", true, 1f);
                ParticleFactory.Rate(sparks, 5f);
                ParticleFactory.MaxParticles(sparks, 20);
                ParticleFactory.Lifetime(sparks, 0.5f, 1f);
                ParticleFactory.Speed(sparks, 0.4f, 1.2f);
                ParticleFactory.Size(sparks, 0.06f, 0.11f);
                ParticleFactory.Colors(sparks, new Color(1f, 0.6f, 0.2f), new Color(1f, 0.85f, 0.4f));
                Ball(sparks, 0.35f);
                Rise(sparks, 1.5f, 3f);
                ParticleFactory.Noise(sparks, 0.8f, 1.2f);
                Over(sparks, (0f, Color.white), (1f, new Color(1f, 0.4f, 0.1f, 0f)));
            }
            return root;
        }

        /// <summary>A gun's flash at the muzzle: a tinted star burst, a soft glow (the light), a tongue of fire along +z, a wisp.</summary>
        private static GameObject MuzzleFlash()
        {
            GameObject root = Root("MuzzleFlash");
            Transform t = root.transform;
            ParticleSystem star = System(t, "FlashTint", "FlareAdd", false, 0.05f);
            Burst(star, 1);
            ParticleFactory.Lifetime(star, 0.05f, 0.07f);
            ParticleFactory.Speed(star, 0f, 0f);
            ParticleFactory.Size(star, 0.9f, 1.25f);
            ParticleFactory.Spin(star, 0f);
            Over(star, (0f, Color.white), (1f, new Color(1f, 1f, 1f, 0f)));
            star.transform.localPosition = new Vector3(0f, 0f, 0.15f);

            ParticleSystem glow = System(t, "GlowTint", "FlareAdd", false, 0.05f);
            Burst(glow, 1);
            ParticleFactory.Lifetime(glow, 0.06f, 0.08f);
            ParticleFactory.Speed(glow, 0f, 0f);
            ParticleFactory.Size(glow, 2.6f, 3f);
            ParticleFactory.Colors(glow, new Color(1f, 1f, 1f, 0.35f), new Color(1f, 1f, 1f, 0.3f));
            Over(glow, (0f, Color.white), (1f, new Color(1f, 1f, 1f, 0f)));

            ParticleSystem tongue = System(t, "Tongue", "FireAdd", false, 0.05f);
            Burst(tongue, 3);
            ParticleFactory.Lifetime(tongue, 0.04f, 0.06f);
            ParticleFactory.Speed(tongue, 8f, 14f);
            ParticleFactory.Size(tongue, 0.28f, 0.42f);
            ParticleFactory.Colors(tongue, Hot, new Color(1f, 0.75f, 0.35f, 1f));
            Forward(tongue, 8f, 0.02f);
            Stretch(tongue, 0.06f, 1.6f);
            Over(tongue, (0f, Color.white), (1f, new Color(1f, 0.6f, 0.2f, 0f)));

            ParticleSystem wisp = System(t, "Wisp", "Smoke", false, 0.05f);
            Burst(wisp, 1);
            ParticleFactory.Lifetime(wisp, 0.3f, 0.45f);
            ParticleFactory.Speed(wisp, 0.6f, 1.2f);
            ParticleFactory.Size(wisp, 0.3f, 0.45f);
            ParticleFactory.Colors(wisp, new Color(0.7f, 0.7f, 0.7f, 0.28f), new Color(0.8f, 0.8f, 0.8f, 0.22f));
            Forward(wisp, 15f, 0.02f);
            Grow(wisp, new Keyframe(0f, 0.5f), new Keyframe(1f, 1.8f));
            Over(wisp, (0f, Color.white), (1f, new Color(1f, 1f, 1f, 0f)));
            return root;
        }

        /// <summary>A burning wreck: flames licking up over a car-sized patch, black smoke rising high, embers.</summary>
        private static GameObject Fire()
        {
            GameObject root = Root("Fire");
            Transform t = root.transform;
            ParticleSystem flames = System(t, "Flames", "FireAdd", true, 1f);
            ParticleFactory.Rate(flames, 42f);
            ParticleFactory.MaxParticles(flames, 80);
            ParticleFactory.Lifetime(flames, 0.45f, 0.85f);
            ParticleFactory.Speed(flames, 0.1f, 0.5f);
            ParticleFactory.Size(flames, 1f, 1.8f);
            ParticleFactory.Colors(flames, new Color(1f, 0.85f, 0.5f), new Color(1f, 0.65f, 0.3f));
            ParticleFactory.Box(flames, new Vector3(1.4f, 0.4f, 2.6f));
            Rise(flames, 1.8f, 3.4f);
            Grow(flames, new Keyframe(0f, 0.5f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0.25f));
            Over(flames, (0f, new Color(1f, 0.95f, 0.75f, 0f)), (0.12f, new Color(1f, 0.9f, 0.6f, 1f)), (0.55f, new Color(1f, 0.5f, 0.15f, 0.8f)), (1f, new Color(0.45f, 0.1f, 0.02f, 0f)));
            ParticleFactory.Spin(flames, 90f);
            ParticleFactory.Noise(flames, 0.5f, 1f);
            flames.transform.localPosition = new Vector3(0f, 0.6f, 0f);

            ParticleSystem smoke = System(t, "Smoke", "Smoke", true, 1f);
            ParticleFactory.Rate(smoke, 9f);
            ParticleFactory.MaxParticles(smoke, 50);
            ParticleFactory.Lifetime(smoke, 2.4f, 3.6f);
            ParticleFactory.Speed(smoke, 0.1f, 0.4f);
            ParticleFactory.Size(smoke, 1.4f, 2.2f);
            ParticleFactory.Colors(smoke, new Color(0.36f, 0.34f, 0.33f, 0.88f), new Color(0.42f, 0.4f, 0.38f, 0.82f));
            ParticleFactory.Box(smoke, new Vector3(1.2f, 0.3f, 2f));
            Rise(smoke, 2.6f, 4.2f);
            Grow(smoke, new Keyframe(0f, 0.5f), new Keyframe(1f, 3.2f));
            Over(smoke, (0f, new Color(0.3f, 0.3f, 0.3f, 0f)), (0.12f, new Color(0.3f, 0.3f, 0.3f, 1f)), (0.6f, new Color(0.75f, 0.75f, 0.75f, 0.7f)), (1f, new Color(1f, 1f, 1f, 0f)));
            ParticleFactory.Spin(smoke, 30f);
            ParticleFactory.Noise(smoke, 0.5f, 0.35f);
            smoke.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            Order(smoke, -1);

            ParticleSystem embers = System(t, "Embers", "SparkAdd", true, 1f);
            ParticleFactory.Rate(embers, 8f);
            ParticleFactory.MaxParticles(embers, 30);
            ParticleFactory.Lifetime(embers, 0.6f, 1.3f);
            ParticleFactory.Speed(embers, 0.3f, 1f);
            ParticleFactory.Size(embers, 0.06f, 0.12f);
            ParticleFactory.Colors(embers, new Color(1f, 0.55f, 0.15f), new Color(1f, 0.85f, 0.4f));
            ParticleFactory.Box(embers, new Vector3(1.4f, 0.4f, 2.4f));
            Rise(embers, 2f, 4.5f);
            ParticleFactory.Noise(embers, 1f, 1.4f);
            Over(embers, (0f, Color.white), (1f, new Color(1f, 0.3f, 0.05f, 0f)));
            embers.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            return root;
        }

        /// <summary>The nitro jet: a blue-white flame streaming along +z, hot white at the nozzle.</summary>
        private static GameObject NitroFlame()
        {
            GameObject root = Root("NitroFlame");
            Transform t = root.transform;
            ParticleSystem jet = System(t, "Jet", "FireAdd", true, 0.5f);
            ParticleFactory.Rate(jet, 120f);
            ParticleFactory.MaxParticles(jet, 60);
            ParticleFactory.Lifetime(jet, 0.1f, 0.18f);
            ParticleFactory.Speed(jet, 6f, 9f);
            ParticleFactory.Size(jet, 0.5f, 0.8f);
            ParticleFactory.Colors(jet, new Color(0.5f, 0.75f, 1f), new Color(0.3f, 0.5f, 1f));
            Forward(jet, 5f, 0.04f);
            Grow(jet, new Keyframe(0f, 1f), new Keyframe(1f, 0.2f));
            Over(jet, (0f, Color.white), (0.4f, new Color(0.6f, 0.75f, 1f, 0.9f)), (1f, new Color(0.25f, 0.2f, 1f, 0f)));
            ParticleFactory.Spin(jet, 180f);

            ParticleSystem core = System(t, "Core", "FireAdd", true, 0.5f);
            ParticleFactory.Rate(core, 50f);
            ParticleFactory.MaxParticles(core, 20);
            ParticleFactory.Lifetime(core, 0.04f, 0.07f);
            ParticleFactory.Speed(core, 5f, 7f);
            ParticleFactory.Size(core, 0.18f, 0.26f);
            ParticleFactory.Colors(core, Color.white, new Color(0.85f, 0.95f, 1f));
            Forward(core, 3f, 0.02f);
            Grow(core, new Keyframe(0f, 1f), new Keyframe(1f, 0.4f));
            Over(core, (0f, Color.white), (1f, new Color(0.7f, 0.85f, 1f, 0f)));

            ParticleSystem glow = System(t, "Glow", "FlareAdd", true, 0.5f);
            ParticleFactory.Rate(glow, 30f);
            ParticleFactory.MaxParticles(glow, 8);
            ParticleFactory.Lifetime(glow, 0.06f, 0.09f);
            ParticleFactory.Speed(glow, 0f, 0.5f);
            ParticleFactory.Size(glow, 1.1f, 1.5f);
            ParticleFactory.Colors(glow, new Color(0.45f, 0.65f, 1f, 0.55f), new Color(0.6f, 0.8f, 1f, 0.45f));
            Over(glow, (0f, Color.white), (1f, new Color(1f, 1f, 1f, 0f)));
            return root;
        }

        /// <summary>Tyre dust: a few low puffs that roll out and settle, tinted by the race with the colour of the land.</summary>
        private static GameObject Dust()
        {
            GameObject root = Root("Dust");
            Transform t = root.transform;
            ParticleSystem dust = System(t, "DustTint", "Smoke", false, 0.1f);
            Burst(dust, 5);
            ParticleFactory.Lifetime(dust, 0.7f, 1.2f);
            ParticleFactory.Speed(dust, 0.3f, 1.2f);
            ParticleFactory.Size(dust, 0.8f, 1.4f);
            ParticleFactory.Colors(dust, Color.white, new Color(0.92f, 0.92f, 0.92f, 1f));
            Ball(dust, 0.35f, true);
            Rise(dust, 0.2f, 0.6f);
            Grow(dust, new Keyframe(0f, 0.6f), new Keyframe(1f, 2.1f));
            Over(dust, (0f, new Color(1f, 1f, 1f, 0f)), (0.15f, new Color(1f, 1f, 1f, 0.45f)), (1f, new Color(1f, 1f, 1f, 0f)));
            ParticleFactory.Spin(dust, 40f);
            dust.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            return root;
        }

        /// <summary>The burnt mark a blast leaves: a 1 m quad facing up on the root, faded through its _BaseColor.</summary>
        private static GameObject Scorch()
        {
            GameObject root = Root("Scorch");
            root.AddComponent<MeshFilter>().sharedMesh = EffectMesh("Decal");
            var renderer = root.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = EffectMaterial("Scorch");
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return root;
        }
    }
}
