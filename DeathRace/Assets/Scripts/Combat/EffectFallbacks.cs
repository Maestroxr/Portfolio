using UnityEngine;
using UnityEngine.Rendering;

namespace Portfolio.DeathRace
{
    /// <summary>The effects of a race, by what they show.</summary>
    public enum EffectKind
    {
        Explosion = 0,
        BigExplosion = 1,
        Sparks = 2,
        Smoke = 3,
        BlackSmoke = 4,
        Muzzle = 5,
        Fire = 6,
        NitroFlame = 7,
        Dust = 8
    }

    /// <summary>
    /// The effects drawn in code when the content has no prefab for one yet: the same parts a builder would make (a
    /// fireball, embers, smoke, a shock ring and a flash for an explosion; streaks for sparks; puffs for smoke and dust;
    /// flames for fire and nitro), sized for a camera forty metres up. Each is an inactive template the pools copy.
    /// </summary>
    public static class EffectFallbacks
    {
        public static GameObject Build(EffectKind kind, Transform parent)
        {
            var root = new GameObject($"{kind} (fallback)");
            root.SetActive(false);
            root.transform.SetParent(parent, false);
            switch (kind)
            {
                case EffectKind.Explosion:
                    Explosion(root.transform, 1f);
                    break;
                case EffectKind.BigExplosion:
                    Explosion(root.transform, 1.6f);
                    break;
                case EffectKind.Sparks:
                    Sparks(root.transform);
                    break;
                case EffectKind.Smoke:
                    Smoke(root.transform, new Color(0.66f, 0.64f, 0.61f, 0.8f), new Color(0.8f, 0.78f, 0.74f, 0.7f), 14f, 1f, 0.8f);
                    break;
                case EffectKind.BlackSmoke:
                    // Born nearly black, it greys as it climbs and spreads: dark on the sand, still a shape on the asphalt.
                    Smoke(root.transform, new Color(0.34f, 0.32f, 0.3f, 0.92f), new Color(0.42f, 0.4f, 0.38f, 0.85f), 18f, 1.35f, 0.3f);
                    break;
                case EffectKind.Muzzle:
                    Muzzle(root.transform);
                    break;
                case EffectKind.Fire:
                    Fire(root.transform);
                    break;
                case EffectKind.NitroFlame:
                    NitroFlame(root.transform);
                    break;
                default:
                    Dust(root.transform);
                    break;
            }
            return root;
        }

        private static void Explosion(Transform root, float scale)
        {
            ParticleSystem fireball = System("Fireball Tint", root, CombatArt.Glow, 3);
            OneShot(fireball, 0.9f, 0.4f, 0.75f);
            Burst(fireball, Mathf.RoundToInt(16 * scale));
            Size(fireball, 2.2f * scale, 4.4f * scale);
            Speed(fireball, 2f, 9f * Mathf.Sqrt(scale));
            Sphere(fireball, 0.9f * scale, true);
            Drag(fireball, 4f);
            Fade(fireball, new Color(1f, 1f, 0.9f), new Color(1f, 0.7f, 0.45f), new Color(0.45f, 0.16f, 0.08f));
            Grow(fireball, 0.6f, 1.35f);
            Spin(fireball, 90f);

            ParticleSystem core = System("Core Tint", root, CombatArt.Glow, 4);
            OneShot(core, 0.2f, 0.12f, 0.2f);
            Burst(core, 2);
            Size(core, 5f * scale, 6.5f * scale);
            Speed(core, 0f, 0f);
            Fade(core, Color.white, new Color(1f, 0.9f, 0.7f), new Color(1f, 0.6f, 0.3f));

            ParticleSystem embers = System("Embers Tint", root, CombatArt.Glow, 5);
            OneShot(embers, 0.6f, 0.35f, 0.9f);
            Burst(embers, Mathf.RoundToInt(34 * scale));
            Size(embers, 0.18f, 0.38f);
            Speed(embers, 9f, 24f * Mathf.Sqrt(scale));
            Sphere(embers, 0.6f, true);
            Gravity(embers, 1.4f);
            Drag(embers, 1.5f);
            Stretch(embers, 0.05f);
            Fade(embers, Color.white, new Color(1f, 0.85f, 0.5f), new Color(1f, 0.4f, 0.1f));

            ParticleSystem smoke = System("Smoke", root, CombatArt.Smoke, 1);
            OneShot(smoke, 1.2f, 1.4f, 2.6f * Mathf.Sqrt(scale));
            ParticleSystem.MainModule main = smoke.main;
            main.startDelay = 0.08f;
            Burst(smoke, Mathf.RoundToInt(12 * scale));
            Size(smoke, 2.6f * scale, 4.6f * scale);
            Speed(smoke, 1.5f, 6f * Mathf.Sqrt(scale));
            Sphere(smoke, 1.2f * scale, true);
            Drag(smoke, 2.5f);
            Colors(smoke, new Color(0.12f, 0.11f, 0.1f, 0.85f), new Color(0.3f, 0.28f, 0.26f, 0.75f));
            Fade(smoke, Color.white, Color.white, Color.white);
            Grow(smoke, 0.7f, 1.8f);
            Spin(smoke, 30f);
            Lift(smoke, 1.5f);

            ParticleSystem ring = System("Ring Tint", root, CombatArt.Ring, 2);
            OneShot(ring, 0.1f, 0.38f, 0.38f);
            Burst(ring, 1);
            Size(ring, 3f * scale, 3f * scale);
            Speed(ring, 0f, 0f);
            Fade(ring, Color.white, new Color(1f, 0.8f, 0.6f), new Color(1f, 0.5f, 0.3f));
            Grow(ring, 0.25f, 3.6f);
            ParticleSystemRenderer renderer = ring.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
            ring.transform.localPosition = new Vector3(0f, 0.15f, 0f);

            var light = new GameObject("Flash").AddComponent<Light>();
            light.transform.SetParent(root, false);
            light.transform.localPosition = new Vector3(0f, 2.5f, 0f);
            light.type = LightType.Point;
            light.range = 16f * scale;
            light.intensity = 9f;
            light.shadows = LightShadows.None;
            light.enabled = false;
        }

        private static void Sparks(Transform root)
        {
            ParticleSystem sparks = System("Sparks Tint", root, CombatArt.Glow, 5);
            OneShot(sparks, 0.1f, 0.14f, 0.36f);
            Burst(sparks, 14);
            Size(sparks, 0.12f, 0.22f);
            Speed(sparks, 7f, 19f);
            Cone(sparks, 38f, 0.05f);
            Gravity(sparks, 1f);
            Drag(sparks, 2f);
            Stretch(sparks, 0.045f);
            Fade(sparks, Color.white, new Color(1f, 0.9f, 0.6f), new Color(1f, 0.5f, 0.2f));

            ParticleSystem pop = System("Pop Tint", root, CombatArt.Glow, 4);
            OneShot(pop, 0.05f, 0.06f, 0.1f);
            Burst(pop, 1);
            Size(pop, 0.9f, 1.3f);
            Speed(pop, 0f, 0f);
            Fade(pop, Color.white, Color.white, new Color(1f, 0.8f, 0.5f));
        }

        /// <param name="young">How dark the smoke is when it leaves (a share of its colour), lightening to its colour as it fades.</param>
        private static void Smoke(Transform root, Color a, Color b, float rate, float scale, float young)
        {
            ParticleSystem smoke = System("Smoke", root, CombatArt.Smoke, 1);
            Loop(smoke, 1.3f, 2.2f * scale);
            Rate(smoke, rate);
            Size(smoke, 0.9f * scale, 1.6f * scale);
            Speed(smoke, 0.6f, 1.6f);
            Cone(smoke, 25f, 0.35f);
            Colors(smoke, a, b);
            Fade(smoke, new Color(young, young, young, 1f), Color.Lerp(new Color(young, young, young, 1f), Color.white, 0.6f), Color.white, 0.08f);
            Grow(smoke, 0.7f, 3f);
            Spin(smoke, 40f);
            Lift(smoke, 2.4f * scale);
            Noise(smoke, 0.6f, 0.4f);
            smoke.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        }

        private static void Muzzle(Transform root)
        {
            ParticleSystem flash = System("Flash Tint", root, CombatArt.Glow, 6);
            OneShot(flash, 0.03f, 0.05f, 0.07f);
            ParticleSystem.MainModule main = flash.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            Burst(flash, 2);
            Size(flash, 0.8f, 1.2f);
            Speed(flash, 0f, 0.5f);
            Fade(flash, Color.white, Color.white, new Color(1f, 0.8f, 0.5f));

            ParticleSystem tongue = System("Tongue Tint", root, CombatArt.Glow, 6);
            OneShot(tongue, 0.03f, 0.04f, 0.06f);
            ParticleSystem.MainModule tongueMain = tongue.main;
            tongueMain.simulationSpace = ParticleSystemSimulationSpace.Local;
            Burst(tongue, 3);
            Size(tongue, 0.35f, 0.55f);
            Speed(tongue, 14f, 22f);
            Cone(tongue, 8f, 0.02f);
            Stretch(tongue, 0.06f);
            Fade(tongue, Color.white, Color.white, new Color(1f, 0.7f, 0.4f));
        }

        private static void Fire(Transform root)
        {
            ParticleSystem flames = System("Flames", root, CombatArt.Glow, 3);
            Loop(flames, 0.3f, 0.55f);
            Rate(flames, 26f);
            Size(flames, 0.6f, 1.15f);
            Speed(flames, 1.5f, 3.2f);
            Cone(flames, 16f, 0.4f);
            RandomColors(flames, new Color(1f, 0.85f, 0.35f), new Color(1f, 0.55f, 0.15f), new Color(1f, 0.4f, 0.1f));
            Fade(flames, Color.white, new Color(1f, 0.8f, 0.6f), new Color(0.6f, 0.2f, 0.1f), 0.1f);
            Grow(flames, 1f, 0.3f);
            Spin(flames, 120f);
            flames.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

            ParticleSystem embers = System("Embers", root, CombatArt.Glow, 4);
            Loop(embers, 0.5f, 1.1f);
            Rate(embers, 8f);
            Size(embers, 0.1f, 0.2f);
            Speed(embers, 2f, 5f);
            Cone(embers, 30f, 0.3f);
            Colors(embers, new Color(1f, 0.75f, 0.3f), new Color(1f, 0.45f, 0.15f));
            Fade(embers, Color.white, Color.white, new Color(1f, 0.4f, 0.1f));
            Noise(embers, 1.5f, 1f);
            embers.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        }

        private static void NitroFlame(Transform root)
        {
            ParticleSystem jet = System("Jet", root, CombatArt.Glow, 4);
            Loop(jet, 0.09f, 0.16f);
            ParticleSystem.MainModule main = jet.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            Rate(jet, 70f);
            Size(jet, 0.35f, 0.6f);
            Speed(jet, 9f, 13f);
            Cone(jet, 6f, 0.06f);
            Colors(jet, new Color(0.55f, 0.8f, 1f), new Color(0.85f, 0.95f, 1f));
            Fade(jet, Color.white, new Color(0.7f, 0.85f, 1f), new Color(1f, 0.55f, 0.3f));
            Grow(jet, 1f, 0.4f);
            Stretch(jet, 0.03f);

            ParticleSystem glow = System("Glow", root, CombatArt.Glow, 3);
            Loop(glow, 0.05f, 0.08f);
            ParticleSystem.MainModule glowMain = glow.main;
            glowMain.simulationSpace = ParticleSystemSimulationSpace.Local;
            Rate(glow, 30f);
            Size(glow, 0.9f, 1.2f);
            Speed(glow, 0f, 0.5f);
            Colors(glow, new Color(0.45f, 0.7f, 1f, 0.8f), new Color(0.7f, 0.85f, 1f, 0.8f));
            Fade(glow, Color.white, Color.white, Color.white);
        }

        private static void Dust(Transform root)
        {
            ParticleSystem dust = System("Dust Tint", root, CombatArt.Smoke, 0);
            OneShot(dust, 0.1f, 0.7f, 1.3f);
            Burst(dust, 3);
            Size(dust, 1.1f, 2f);
            Speed(dust, 0.5f, 2f);
            Sphere(dust, 0.4f, true);
            Drag(dust, 2f);
            Fade(dust, new Color(1f, 1f, 1f, 0.55f), new Color(1f, 1f, 1f, 0.35f), Color.white, 0.1f);
            Grow(dust, 0.7f, 2f);
            Spin(dust, 25f);
        }

        // ------------------------------------------------------------------------------------------------
        // Particle system parts
        // ------------------------------------------------------------------------------------------------

        private static ParticleSystem System(string name, Transform parent, Material material, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = system.main;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = 200;
            main.startColor = Color.white;
            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 0f;
            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = false;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingOrder = order;
            renderer.maxParticleSize = 4f;
            return system;
        }

        private static void OneShot(ParticleSystem system, float duration, float minLife, float maxLife)
        {
            ParticleSystem.MainModule main = system.main;
            main.loop = false;
            main.duration = Mathf.Max(0.05f, duration);
            main.startLifetime = new ParticleSystem.MinMaxCurve(minLife, maxLife);
        }

        private static void Loop(ParticleSystem system, float minLife, float maxLife)
        {
            ParticleSystem.MainModule main = system.main;
            main.loop = true;
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(minLife, maxLife);
        }

        private static void Burst(ParticleSystem system, int count)
        {
            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
        }

        private static void Rate(ParticleSystem system, float rate)
        {
            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = rate;
        }

        private static void Size(ParticleSystem system, float min, float max)
        {
            ParticleSystem.MainModule main = system.main;
            main.startSize = new ParticleSystem.MinMaxCurve(min, max);
        }

        private static void Speed(ParticleSystem system, float min, float max)
        {
            ParticleSystem.MainModule main = system.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(min, max);
        }

        private static void Colors(ParticleSystem system, Color a, Color b)
        {
            ParticleSystem.MainModule main = system.main;
            main.startColor = new ParticleSystem.MinMaxGradient(a, b);
        }

        private static void RandomColors(ParticleSystem system, params Color[] colors)
        {
            var gradient = new Gradient();
            var keys = new GradientColorKey[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                keys[i] = new GradientColorKey(colors[i], colors.Length == 1 ? 0f : i / (colors.Length - 1f));
            }
            gradient.SetKeys(keys, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            gradient.mode = GradientMode.Fixed;
            ParticleSystem.MainModule main = system.main;
            main.startColor = new ParticleSystem.MinMaxGradient(gradient) { mode = ParticleSystemGradientMode.RandomColor };
        }

        private static void Sphere(ParticleSystem system, float radius, bool hemisphere)
        {
            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = hemisphere ? ParticleSystemShapeType.Hemisphere : ParticleSystemShapeType.Sphere;
            shape.radius = radius;
            // A hemisphere opens along +Z; lying it on the ground opens it to the sky.
            shape.rotation = hemisphere ? new Vector3(-90f, 0f, 0f) : Vector3.zero;
        }

        private static void Cone(ParticleSystem system, float angle, float radius)
        {
            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = angle;
            shape.radius = radius;
        }

        private static void Gravity(ParticleSystem system, float gravity)
        {
            ParticleSystem.MainModule main = system.main;
            main.gravityModifier = gravity;
        }

        private static void Drag(ParticleSystem system, float drag)
        {
            ParticleSystem.LimitVelocityOverLifetimeModule limit = system.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.limit = 100f;
            limit.drag = drag;
            limit.multiplyDragByParticleSize = false;
            limit.multiplyDragByParticleVelocity = false;
        }

        /// <summary>Smoke rises toward the camera as it thins.</summary>
        private static void Lift(ParticleSystem system, float speed)
        {
            ParticleSystem.ForceOverLifetimeModule force = system.forceOverLifetime;
            force.enabled = true;
            force.space = ParticleSystemSimulationSpace.World;
            force.x = new ParticleSystem.MinMaxCurve(0f);
            force.y = new ParticleSystem.MinMaxCurve(speed);
            force.z = new ParticleSystem.MinMaxCurve(0f);
        }

        private static void Fade(ParticleSystem system, Color start, Color middle, Color end, float fadeIn = 0f)
        {
            ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(start, 0f), new GradientColorKey(middle, 0.35f), new GradientColorKey(end, 1f) },
                fadeIn > 0f
                    ? new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(start.a, fadeIn), new GradientAlphaKey(middle.a, 0.5f), new GradientAlphaKey(0f, 1f) }
                    : new[] { new GradientAlphaKey(start.a, 0f), new GradientAlphaKey(middle.a, 0.5f), new GradientAlphaKey(0f, 1f) });
            color.color = new ParticleSystem.MinMaxGradient(gradient);
        }

        private static void Grow(ParticleSystem system, float start, float end)
        {
            ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, start, 1f, end));
        }

        private static void Spin(ParticleSystem system, float degreesPerSecond)
        {
            ParticleSystem.MainModule main = system.main;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            ParticleSystem.RotationOverLifetimeModule rotation = system.rotationOverLifetime;
            rotation.enabled = true;
            float radians = degreesPerSecond * Mathf.Deg2Rad;
            rotation.z = new ParticleSystem.MinMaxCurve(-radians, radians);
        }

        private static void Noise(ParticleSystem system, float strength, float frequency)
        {
            ParticleSystem.NoiseModule noise = system.noise;
            noise.enabled = true;
            noise.strength = strength;
            noise.frequency = frequency;
            noise.quality = ParticleSystemNoiseQuality.Low;
        }

        private static void Stretch(ParticleSystem system, float velocityScale)
        {
            var renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = velocityScale;
            renderer.lengthScale = 1.5f;
        }
    }
}
