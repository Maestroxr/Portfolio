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
    /// The numbers and volley patterns of the ship's four weapons at their four levels. Collecting a weapon pickup
    /// switches to that weapon and raises the level; losing a ship lowers it by one.
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
                default: return 27f;
            }
        }

        /// <summary>Seconds a projectile flies before it fades out.</summary>
        public static float Lifetime(WeaponType type)
        {
            switch (type)
            {
                case WeaponType.Laser: return 0.65f;
                case WeaponType.Scatter: return 0.42f;
                case WeaponType.Missiles: return 2.4f;
                default: return 0.95f;
            }
        }

        /// <summary>How many targets one projectile can pass through.</summary>
        public static int Pierce(WeaponType type, int level)
        {
            return type == WeaponType.Laser ? level + 1 : 1;
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

        public static string Title(WeaponType type)
        {
            switch (type)
            {
                case WeaponType.Laser: return "Lancer Laser";
                case WeaponType.Scatter: return "Scatter Cannon";
                case WeaponType.Missiles: return "Seeker Missiles";
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
                default: return new Color(0.35f, 0.85f, 1f);
            }
        }
    }


    /// <summary>Names, colours and durations of the timed power-ups.</summary>
    public static class PowerUps
    {
        public const int Count = 4;

        public static float Duration(PowerUpType type)
        {
            switch (type)
            {
                case PowerUpType.Magnet: return 15f;
                case PowerUpType.Chrono: return 7f;
                case PowerUpType.Drones: return 18f;
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
                default: return new Color(1f, 0.85f, 0.25f);
            }
        }
    }
}
