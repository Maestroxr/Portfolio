using UnityEngine;

namespace Portfolio.EndlessRunner
{
    /// <summary>
    /// One-shot particle effects. Every effect is a single world-space particle system that is moved to the spot and
    /// asked to emit, so no effect objects are created at runtime.
    /// </summary>
    public class RunnerEffects : MonoBehaviour
    {
        [SerializeField] internal ParticleSystem coinSparkle;
        [SerializeField] internal ParticleSystem gemSparkle;
        [SerializeField] internal ParticleSystem powerUpBurst;
        [SerializeField] internal ParticleSystem crashDebris;
        [SerializeField] internal ParticleSystem crashPuff;
        [SerializeField] internal ParticleSystem landPuff;
        [SerializeField] internal ParticleSystem confetti;
        [SerializeField] internal ParticleSystem shieldBreak;
        [SerializeField] internal ParticleSystem bounceRing;
        [SerializeField] internal ParticleSystem splash;

        /// <summary>Draws the effects with the materials of a theme.</summary>
        public void ApplyTheme(ParticleMaterials materials)
        {
            if (materials == null)
            {
                return;
            }
            Skin(coinSparkle, materials.sparkle);
            Skin(gemSparkle, materials.sparkle);
            Skin(powerUpBurst, materials.sparkle);
            Skin(shieldBreak, materials.sparkle);
            Skin(bounceRing, materials.glow);
            Skin(crashDebris, materials.debris);
            Skin(crashPuff, materials.smoke);
            Skin(landPuff, materials.smoke);
            Skin(confetti, materials.confetti);
            Skin(splash, materials.soft);
        }

        private static void Skin(ParticleSystem system, Material material)
        {
            if (system != null && material != null && system.TryGetComponent(out ParticleSystemRenderer renderer))
            {
                renderer.sharedMaterial = material;
            }
        }

        public void Coin(Vector3 position)
        {
            Emit(coinSparkle, position, 9);
        }

        public void Gem(Vector3 position)
        {
            Emit(gemSparkle, position, 22);
        }

        public void PowerUp(Vector3 position, Color color)
        {
            Emit(powerUpBurst, position, 28, color);
        }

        public void Crash(Vector3 position)
        {
            Emit(crashDebris, position, 16);
            Emit(crashPuff, position, 10);
        }

        public void Land(Vector3 position, Color color)
        {
            Emit(landPuff, position, 8, color);
        }

        public void Confetti(Vector3 position)
        {
            Emit(confetti, position, 140);
        }

        public void ShieldBreak(Vector3 position)
        {
            Emit(shieldBreak, position, 36);
        }

        public void Bounce(Vector3 position)
        {
            Emit(bounceRing, position, 18);
        }

        public void Splash(Vector3 position, Color color)
        {
            Emit(splash, position, 34, color);
        }

        private static void Emit(ParticleSystem system, Vector3 position, int count)
        {
            if (system == null)
            {
                return;
            }
            system.transform.position = position;
            system.Emit(count);
        }

        private static void Emit(ParticleSystem system, Vector3 position, int count, Color color)
        {
            if (system == null)
            {
                return;
            }
            ParticleSystem.MainModule main = system.main;
            main.startColor = color;
            Emit(system, position, count);
        }
    }
}
