namespace Portfolio.Asteroids
{
    /// <summary>
    /// The rules of the rocks the field campaigns add (AsteroidTypes.FieldCampaigns.cs): iron shrugs off half of every
    /// shot and cracks under blasts, a small hydra rock grows back into a medium one when it is left alone, and the piece of
    /// wreckage that carries a fuel tank explodes like magma.
    /// </summary>
    public static partial class AsteroidRules
    {
        /// <summary>Seconds a small hydra rock takes to grow back into a medium one.</summary>
        public const float HydraRegrowSeconds = 8f;

        /// <summary>Seconds before it grows back during which a small hydra rock swells as a warning.</summary>
        public const float HydraWarningSeconds = 2.5f;

        /// <summary>Hydra rocks in play at most for a small one to grow back; with more it waits.</summary>
        public const int HydraRegrowCap = 12;

        /// <summary>The shape (model variant) of the wreckage that carries a fuel tank.</summary>
        public const int VolatileWreckShape = 2;


        /// <summary>
        /// The damage a hit of <paramref name="amount"/> by <paramref name="source"/> does to a rock of <paramref name="kind"/>:
        /// iron takes half of what a shot does and half again more of a blast; every other rock takes it all.
        /// </summary>
        public static float DamageTaken(AsteroidKind kind, DamageSource source, float amount)
        {
            if (kind != AsteroidKind.Metal)
            {
                return amount;
            }
            switch (source)
            {
                case DamageSource.PlayerShot: return amount * 0.5f;
                case DamageSource.Explosion: return amount * 1.5f;
                default: return amount;
            }
        }


        /// <summary>Whether a rock of <paramref name="kind"/> and model variant <paramref name="shape"/> explodes when destroyed.</summary>
        public static bool Explodes(AsteroidKind kind, int shape)
        {
            return kind == AsteroidKind.Magma || kind == AsteroidKind.Wreck && shape == VolatileWreckShape;
        }


        /// <summary>Whether a rock of <paramref name="kind"/> and <paramref name="size"/> grows back when left alone.</summary>
        public static bool Regrows(AsteroidKind kind, AsteroidSize size)
        {
            return kind == AsteroidKind.Hydra && size == AsteroidSize.Small;
        }
    }
}
