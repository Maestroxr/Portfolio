using UnityEngine;

namespace Portfolio.Asteroids.EditorTools
{
    /// <summary>Creates and configures the particle systems of the Asteroids module from code.</summary>
    internal static class ParticleFactory
    {
        public static ParticleSystem Create(string name, Transform parent, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = system.main;
            main.duration = 1f;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = 300;
            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 0f;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return system;
        }

        public static void Lifetime(ParticleSystem system, float min, float max)
        {
            ParticleSystem.MainModule main = system.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(min, max);
        }

        public static void Speed(ParticleSystem system, float min, float max)
        {
            ParticleSystem.MainModule main = system.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(min, max);
        }

        public static void Size(ParticleSystem system, float min, float max)
        {
            ParticleSystem.MainModule main = system.main;
            main.startSize = new ParticleSystem.MinMaxCurve(min, max);
        }

        public static void Colors(ParticleSystem system, Color a, Color b)
        {
            ParticleSystem.MainModule main = system.main;
            main.startColor = new ParticleSystem.MinMaxGradient(a, b);
        }

        public static void RandomColors(ParticleSystem system, params Color[] colors)
        {
            var gradient = new Gradient();
            var keys = new GradientColorKey[Mathf.Min(8, colors.Length)];
            for (int i = 0; i < keys.Length; i++)
            {
                keys[i] = new GradientColorKey(colors[i], keys.Length == 1 ? 0f : i / (keys.Length - 1f));
            }
            gradient.SetKeys(keys, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            gradient.mode = GradientMode.Fixed;
            ParticleSystem.MainModule main = system.main;
            main.startColor = new ParticleSystem.MinMaxGradient(gradient) { mode = ParticleSystemGradientMode.RandomColor };
        }

        public static void Gravity(ParticleSystem system, float gravity)
        {
            ParticleSystem.MainModule main = system.main;
            main.gravityModifier = gravity;
        }

        public static void Rate(ParticleSystem system, float rate)
        {
            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = rate;
        }

        public static void Sphere(ParticleSystem system, float radius, bool hemisphere = false)
        {
            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = hemisphere ? ParticleSystemShapeType.Hemisphere : ParticleSystemShapeType.Sphere;
            shape.radius = radius;
            shape.rotation = hemisphere ? new Vector3(-90f, 0f, 0f) : Vector3.zero;
        }

        public static void Cone(ParticleSystem system, float angle, float radius)
        {
            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = angle;
            shape.radius = radius;
            shape.rotation = new Vector3(-90f, 0f, 0f);
        }

        public static void Circle(ParticleSystem system, float radius, bool edge)
        {
            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius;
            shape.radiusThickness = edge ? 0f : 1f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
        }

        public static void Box(ParticleSystem system, Vector3 size)
        {
            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = size;
            shape.rotation = Vector3.zero;
        }

        public static void FadeOut(ParticleSystem system, float fadeIn = 0f)
        {
            ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                fadeIn > 0f
                    ? new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, fadeIn), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) }
                    : new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.5f), new GradientAlphaKey(0f, 1f) });
            color.color = new ParticleSystem.MinMaxGradient(gradient);
        }

        public static void SizeOverLife(ParticleSystem system, float start, float end)
        {
            ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, start, 1f, end));
        }

        public static void Noise(ParticleSystem system, float strength, float frequency)
        {
            ParticleSystem.NoiseModule noise = system.noise;
            noise.enabled = true;
            noise.strength = strength;
            noise.frequency = frequency;
            noise.scrollSpeed = 0.3f;
            noise.quality = ParticleSystemNoiseQuality.Low;
        }

        public static void Spin(ParticleSystem system, float degreesPerSecond)
        {
            ParticleSystem.MainModule main = system.main;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            ParticleSystem.RotationOverLifetimeModule rotation = system.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-degreesPerSecond * Mathf.Deg2Rad, degreesPerSecond * Mathf.Deg2Rad);
        }

        public static void Tumble(ParticleSystem system, float degreesPerSecond)
        {
            ParticleSystem.MainModule main = system.main;
            main.startRotation3D = true;
            main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            ParticleSystem.RotationOverLifetimeModule rotation = system.rotationOverLifetime;
            rotation.enabled = true;
            rotation.separateAxes = true;
            float r = degreesPerSecond * Mathf.Deg2Rad;
            rotation.x = new ParticleSystem.MinMaxCurve(-r, r);
            rotation.y = new ParticleSystem.MinMaxCurve(-r, r);
            rotation.z = new ParticleSystem.MinMaxCurve(-r, r);
        }

        public static void MeshParticles(ParticleSystem system, Mesh mesh)
        {
            var renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = mesh;
            renderer.alignment = ParticleSystemRenderSpace.World;
        }

        public static void Velocity(ParticleSystem system, Vector3 min, Vector3 max)
        {
            ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(min.x, max.x);
            velocity.y = new ParticleSystem.MinMaxCurve(min.y, max.y);
            velocity.z = new ParticleSystem.MinMaxCurve(min.z, max.z);
        }

        public static void MaxParticles(ParticleSystem system, int count)
        {
            ParticleSystem.MainModule main = system.main;
            main.maxParticles = count;
        }
    }
}
