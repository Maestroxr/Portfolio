using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>One projectile of a volley: its angle off the ship's heading and its sideways offset from the gun.</summary>
    public struct Barrel
    {
        public float Angle;
        public float Offset;

        public Barrel(float angle, float offset)
        {
            Angle = angle;
            Offset = offset;
        }
    }


    /// <summary>
    /// The numbers and volley patterns of the ship's eight weapons at their four levels. Collecting a weapon pickup
    /// switches to that weapon and raises the level; losing a ship lowers it by one. The last four (the armory's) carry a
    /// trick of their own besides the numbers: the Arc Caster's chains, the Rail Driver's pierce, the Flak Burster's
    /// shrapnel and the Ion Wave's growth (<see cref="Chains"/>, <see cref="Fragments"/>, <see cref="Growth"/>).
    /// </summary>
    public static class WeaponRules
    {
        public const int MaxLevel = 4;

        /// <summary>Seconds between volleys while the trigger is held.</summary>
        public static float Cooldown(WeaponType type, int level)
        {
            switch (type)
            {
                case WeaponType.Laser: return 0.3f - 0.02f * (level - 1);
                case WeaponType.Scatter: return 0.5f - 0.03f * (level - 1);
                case WeaponType.Missiles: return 0.62f - 0.05f * (level - 1);
                case WeaponType.Arc: return 0.34f - 0.02f * (level - 1);
                case WeaponType.Rail: return 0.8f - 0.06f * (level - 1);
                case WeaponType.Flak: return 0.52f - 0.03f * (level - 1);
                case WeaponType.Wave: return 0.55f - 0.03f * (level - 1);
                default: return 0.17f - 0.01f * (level - 1);
            }
        }

        public static float Damage(WeaponType type, int level)
        {
            switch (type)
            {
                case WeaponType.Laser: return level >= 3 ? 3f : 2f;
                case WeaponType.Scatter: return 1f;
                case WeaponType.Missiles: return 3f;
                case WeaponType.Arc: return level >= 3 ? 2f : 1.5f;
                case WeaponType.Rail: return 3f + level;
                case WeaponType.Flak: return 2f;
                case WeaponType.Wave: return level >= 3 ? 2f : 1.5f;
                default: return level >= 4 ? 1.5f : 1f;
            }
        }

        public static float Speed(WeaponType type)
        {
            switch (type)
            {
                case WeaponType.Laser: return 42f;
                case WeaponType.Scatter: return 21f;
                case WeaponType.Missiles: return 13f;
                case WeaponType.Arc: return 30f;
                case WeaponType.Rail: return 70f;
                case WeaponType.Flak: return 19f;
                case WeaponType.Wave: return 16f;
                default: return 27f;
            }
        }

        /// <summary>Seconds a projectile flies before it fades out (a flak shell bursts then).</summary>
        public static float Lifetime(WeaponType type)
        {
            switch (type)
            {
                case WeaponType.Laser: return 0.65f;
                case WeaponType.Scatter: return 0.42f;
                case WeaponType.Missiles: return 2.4f;
                case WeaponType.Arc: return 0.68f;
                case WeaponType.Rail: return 0.5f;
                case WeaponType.Flak: return 0.5f;
                case WeaponType.Wave: return 0.9f;
                default: return 0.95f;
            }
        }

        /// <summary>How much of the ship's velocity a projectile takes along (the beams and slugs none).</summary>
        public static float Inherit(WeaponType type)
        {
            return type == WeaponType.Laser || type == WeaponType.Rail ? 0f : 0.35f;
        }

        /// <summary>How many targets one projectile can pass through.</summary>
        public static int Pierce(WeaponType type, int level)
        {
            switch (type)
            {
                case WeaponType.Laser: return level + 1;
                case WeaponType.Rail: return new[] { 4, 6, 8, 12 }[Mathf.Clamp(level, 1, MaxLevel) - 1];
                case WeaponType.Wave: return new[] { 4, 5, 6, 8 }[Mathf.Clamp(level, 1, MaxLevel) - 1];
                default: return 1;
            }
        }

        /// <summary>The Arc Caster: how many more targets the lightning jumps to from the one its spark hits.</summary>
        public static int Chains(WeaponType type, int level)
        {
            return type == WeaponType.Arc ? new[] { 2, 3, 3, 4 }[Mathf.Clamp(level, 1, MaxLevel) - 1] : 0;
        }

        /// <summary>The Arc Caster: how far (m) the lightning jumps from one target to the next.</summary>
        public static float ChainRange(WeaponType type, int level)
        {
            return type == WeaponType.Arc ? 5f + 0.4f * Mathf.Clamp(level, 1, MaxLevel) : 0f;
        }

        /// <summary>The Arc Caster: the damage of the first jump; every further jump does 15% less.</summary>
        public static float ChainDamage(WeaponType type, int level)
        {
            return type == WeaponType.Arc ? Damage(type, level) * 0.75f : 0f;
        }

        /// <summary>The Flak Burster: the shrapnel one shell bursts into.</summary>
        public static int Fragments(WeaponType type, int level)
        {
            return type == WeaponType.Flak ? new[] { 6, 8, 8, 10 }[Mathf.Clamp(level, 1, MaxLevel) - 1] : 0;
        }

        /// <summary>The Flak Burster: the damage of one piece of shrapnel.</summary>
        public static float FragmentDamage(WeaponType type, int level)
        {
            return type == WeaponType.Flak ? (level >= 3 ? 1.5f : 1f) : 0f;
        }

        /// <summary>The Ion Wave: the hit radius (m) it grows to by the end of its flight (it starts at 0.4).</summary>
        public static float Growth(WeaponType type, int level)
        {
            return type == WeaponType.Wave ? 1.2f + 0.25f * Mathf.Clamp(level, 1, MaxLevel) : 0f;
        }

        /// <summary>Radius of the small blast a missile makes when it hits.</summary>
        public static float BlastRadius(WeaponType type)
        {
            return type == WeaponType.Missiles ? 1.6f : 0f;
        }

        /// <summary>Fills <paramref name="barrels"/> with the volley of <paramref name="type"/> at <paramref name="level"/>.</summary>
        public static void Volley(WeaponType type, int level, List<Barrel> barrels)
        {
            barrels.Clear();
            level = Mathf.Clamp(level, 1, MaxLevel);
            switch (type)
            {
                case WeaponType.Laser:
                    if (level < 3)
                    {
                        barrels.Add(new Barrel(0f, 0f));
                    }
                    else if (level == 3)
                    {
                        barrels.Add(new Barrel(0f, -0.22f));
                        barrels.Add(new Barrel(0f, 0.22f));
                    }
                    else
                    {
                        barrels.Add(new Barrel(0f, 0f));
                        barrels.Add(new Barrel(-4f, -0.3f));
                        barrels.Add(new Barrel(4f, 0.3f));
                    }
                    break;
                case WeaponType.Scatter:
                {
                    int pellets = new[] { 5, 7, 9, 12 }[level - 1];
                    float cone = 44f + level * 4f;
                    for (int i = 0; i < pellets; i++)
                    {
                        float t = pellets == 1 ? 0.5f : i / (pellets - 1f);
                        float jitter = ((i * 7919) % 11 - 5) * 0.6f;
                        barrels.Add(new Barrel(Mathf.Lerp(-cone * 0.5f, cone * 0.5f, t) + jitter, 0f));
                    }
                    break;
                }
                case WeaponType.Missiles:
                    for (int i = 0; i < level; i++)
                    {
                        float side = level == 1 ? 0f : Mathf.Lerp(-1f, 1f, i / (level - 1f));
                        barrels.Add(new Barrel(side * 28f, side * 0.35f));
                    }
                    break;
                case WeaponType.Arc:
                    // One spark, then two and three in a narrow fan: each chains on its own.
                    Fan(barrels, level < 3 ? 1 : level - 1, level < 4 ? 10f : 18f, 0.12f);
                    break;
                case WeaponType.Rail:
                    if (level < 4)
                    {
                        barrels.Add(new Barrel(0f, 0f));
                    }
                    else
                    {
                        barrels.Add(new Barrel(0f, -0.26f));
                        barrels.Add(new Barrel(0f, 0.26f));
                    }
                    break;
                case WeaponType.Flak:
                    Fan(barrels, level < 3 ? 1 : level - 1, level < 4 ? 14f : 20f, 0.15f);
                    break;
                case WeaponType.Wave:
                    Fan(barrels, level < 4 ? 1 : 2, 24f, 0.2f);
                    break;
                default:
                    switch (level)
                    {
                        case 1:
                            barrels.Add(new Barrel(0f, 0f));
                            break;
                        case 2:
                            barrels.Add(new Barrel(0f, -0.2f));
                            barrels.Add(new Barrel(0f, 0.2f));
                            break;
                        case 3:
                            barrels.Add(new Barrel(0f, 0f));
                            barrels.Add(new Barrel(-8f, -0.15f));
                            barrels.Add(new Barrel(8f, 0.15f));
                            break;
                        default:
                            barrels.Add(new Barrel(0f, 0f));
                            barrels.Add(new Barrel(-6f, -0.18f));
                            barrels.Add(new Barrel(6f, 0.18f));
                            barrels.Add(new Barrel(-14f, -0.3f));
                            barrels.Add(new Barrel(14f, 0.3f));
                            break;
                    }
                    break;
            }
        }

        /// <summary><paramref name="count"/> barrels fanned over <paramref name="cone"/> degrees, <paramref name="spacing"/> m apart at the gun.</summary>
        private static void Fan(List<Barrel> barrels, int count, float cone, float spacing)
        {
            for (int i = 0; i < count; i++)
            {
                float side = count == 1 ? 0f : Mathf.Lerp(-1f, 1f, i / (count - 1f));
                barrels.Add(new Barrel(side * cone * 0.5f, side * spacing * (count - 1)));
            }
        }

        public static string Title(WeaponType type)
        {
            switch (type)
            {
                case WeaponType.Laser: return "Lancer Laser";
                case WeaponType.Scatter: return "Scatter Cannon";
                case WeaponType.Missiles: return "Seeker Missiles";
                case WeaponType.Arc: return "Arc Caster";
                case WeaponType.Rail: return "Rail Driver";
                case WeaponType.Flak: return "Flak Burster";
                case WeaponType.Wave: return "Ion Wave";
                default: return "Pulse Blaster";
            }
        }

        public static string ShortTitle(WeaponType type)
        {
            switch (type)
            {
                case WeaponType.Laser: return "LASER";
                case WeaponType.Scatter: return "SCATTER";
                case WeaponType.Missiles: return "MISSILES";
                case WeaponType.Arc: return "ARC";
                case WeaponType.Rail: return "RAIL";
                case WeaponType.Flak: return "FLAK";
                case WeaponType.Wave: return "WAVE";
                default: return "BLASTER";
            }
        }

        public static Color Tint(WeaponType type)
        {
            switch (type)
            {
                case WeaponType.Laser: return new Color(1f, 0.3f, 0.45f);
                case WeaponType.Scatter: return new Color(1f, 0.62f, 0.2f);
                case WeaponType.Missiles: return new Color(0.45f, 1f, 0.45f);
                case WeaponType.Arc: return new Color(0.7f, 0.55f, 1f);
                case WeaponType.Rail: return new Color(0.85f, 0.95f, 1f);
                case WeaponType.Flak: return new Color(1f, 0.82f, 0.3f);
                case WeaponType.Wave: return new Color(0.3f, 1f, 0.85f);
                default: return new Color(0.35f, 0.85f, 1f);
            }
        }
    }


    /// <summary>Names, colours and durations of the timed power-ups.</summary>
    public static class PowerUps
    {
        public const int Count = 6;

        /// <summary>Targets the Phase Rounds add to every projectile's pierce.</summary>
        public const int ExtraPierce = 2;

        public static float Duration(PowerUpType type)
        {
            switch (type)
            {
                case PowerUpType.Magnet: return 15f;
                case PowerUpType.Chrono: return 7f;
                case PowerUpType.Drones: return 18f;
                case PowerUpType.Piercing: return 12f;
                case PowerUpType.Aegis: return 8f;
                default: return 10f;
            }
        }

        public static string Title(PowerUpType type)
        {
            switch (type)
            {
                case PowerUpType.Magnet: return "Tractor Magnet";
                case PowerUpType.Chrono: return "Chrono Field";
                case PowerUpType.Drones: return "Wing Drones";
                case PowerUpType.Piercing: return "Phase Rounds";
                case PowerUpType.Aegis: return "Aegis Barrier";
                default: return "Overdrive";
            }
        }

        public static string Description(PowerUpType type)
        {
            switch (type)
            {
                case PowerUpType.Magnet: return "Crystals and pickups fly to you";
                case PowerUpType.Chrono: return "Time slows down around you";
                case PowerUpType.Drones: return "Two drones fight at your side";
                case PowerUpType.Piercing: return "Every shot passes through two more targets";
                case PowerUpType.Aegis: return "Nothing gets through your shield";
                default: return "Double fire rate";
            }
        }

        public static Color Tint(PowerUpType type)
        {
            switch (type)
            {
                case PowerUpType.Magnet: return new Color(1f, 0.35f, 0.4f);
                case PowerUpType.Chrono: return new Color(0.5f, 0.6f, 1f);
                case PowerUpType.Drones: return new Color(0.4f, 1f, 0.75f);
                case PowerUpType.Piercing: return new Color(0.85f, 0.45f, 1f);
                case PowerUpType.Aegis: return new Color(1f, 0.62f, 0.28f);
                default: return new Color(1f, 0.85f, 0.25f);
            }
        }
    }
}
