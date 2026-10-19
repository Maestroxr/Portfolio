using System;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>The six cars of the game, from the wreck a career starts in to the champion's interceptor.</summary>
    public enum CarModel
    {
        Rustbucket = 0,
        Hornet = 1,
        Bulldog = 2,
        Viper = 3,
        Reaper = 4,
        Tyrant = 5
    }

    /// <summary>What the garage upgrades on a car.</summary>
    public enum UpgradeKind
    {
        Engine = 0,
        Tires = 1,
        Armor = 2
    }

    /// <summary>
    /// What a car is before upgrades: its price and tier, its health, speed, acceleration, grip and steering, its weight,
    /// the rounds its machine gun carries and how far each part can be upgraded. The table is the same for the career,
    /// the computer's cars, the physics and the builders of the car models.
    /// </summary>
    public sealed class CarSpec
    {
        public CarModel Model;
        public string Name;
        public string Blurb;

        /// <summary>1 (the Rustbucket) to 6 (the Tyrant): what prices and the computer's choice of cars go by.</summary>
        public int Tier;

        /// <summary>The dealer's price; the Rustbucket is given away.</summary>
        public int Price;

        public float MaxHealth;

        /// <summary>Metres per second.</summary>
        public float TopSpeed;

        /// <summary>Metres per second per second from a standstill.</summary>
        public float Acceleration;

        /// <summary>How well the tyres hold the road sideways: 1 is the Rustbucket.</summary>
        public float Grip;

        /// <summary>How quickly the car turns: 1 is the Rustbucket.</summary>
        public float Steering;

        /// <summary>Kilograms: heavier cars push lighter ones around.</summary>
        public float Mass;

        public int AmmoCapacity;

        /// <summary>The highest level of each <see cref="UpgradeKind"/>.</summary>
        public int[] MaxLevel;

        /// <summary>The box the car collides with: width, height, length in metres.</summary>
        public Vector3 Size;

        public int MaxLevelOf(UpgradeKind kind)
        {
            return MaxLevel[(int)kind];
        }
    }

    /// <summary>The table of the six cars.</summary>
    public static class CarSpecs
    {
        public static readonly CarSpec[] All =
        {
            new CarSpec
            {
                Model = CarModel.Rustbucket, Name = "Rustbucket", Blurb = "A hatchback held together by rust and hope. Free, and worth it.",
                Tier = 1, Price = 0, MaxHealth = 100f, TopSpeed = 30f, Acceleration = 13f, Grip = 1f, Steering = 1f, Mass = 1100f,
                AmmoCapacity = 100, MaxLevel = new[] { 2, 2, 2 }, Size = new Vector3(2.0f, 1.4f, 4.2f)
            },
            new CarSpec
            {
                Model = CarModel.Hornet, Name = "Hornet", Blurb = "A light coupe that darts through the pack. Quick off the line, quicker into the barrier.",
                Tier = 2, Price = 5000, MaxHealth = 110f, TopSpeed = 34f, Acceleration = 15f, Grip = 1.08f, Steering = 1.08f, Mass = 1150f,
                AmmoCapacity = 120, MaxLevel = new[] { 3, 3, 3 }, Size = new Vector3(1.9f, 1.25f, 4.3f)
            },
            new CarSpec
            {
                Model = CarModel.Bulldog, Name = "Bulldog", Blurb = "A pickup with a bull bar and a grudge. Slow to turn, hard to stop, harder to kill.",
                Tier = 3, Price = 14000, MaxHealth = 150f, TopSpeed = 33f, Acceleration = 14.5f, Grip = 1f, Steering = 0.95f, Mass = 1500f,
                AmmoCapacity = 140, MaxLevel = new[] { 3, 3, 5 }, Size = new Vector3(2.2f, 1.7f, 4.8f)
            },
            new CarSpec
            {
                Model = CarModel.Viper, Name = "Viper", Blurb = "Eight cylinders, a spoiler and no patience. The first car that can win a hard race.",
                Tier = 4, Price = 30000, MaxHealth = 120f, TopSpeed = 40f, Acceleration = 18f, Grip = 1.15f, Steering = 1.12f, Mass = 1250f,
                AmmoCapacity = 150, MaxLevel = new[] { 4, 4, 3 }, Size = new Vector3(2.0f, 1.25f, 4.6f)
            },
            new CarSpec
            {
                Model = CarModel.Reaper, Name = "Reaper", Blurb = "A low black wedge that corners like it is on rails. Expensive, and it shows.",
                Tier = 5, Price = 60000, MaxHealth = 140f, TopSpeed = 44f, Acceleration = 20f, Grip = 1.22f, Steering = 1.18f, Mass = 1300f,
                AmmoCapacity = 170, MaxLevel = new[] { 4, 4, 4 }, Size = new Vector3(2.05f, 1.1f, 4.5f)
            },
            new CarSpec
            {
                Model = CarModel.Tyrant, Name = "Tyrant", Blurb = "An armoured interceptor built for one thing: being the last car running.",
                Tier = 6, Price = 120000, MaxHealth = 175f, TopSpeed = 48f, Acceleration = 22f, Grip = 1.28f, Steering = 1.18f, Mass = 1700f,
                AmmoCapacity = 200, MaxLevel = new[] { 5, 5, 5 }, Size = new Vector3(2.3f, 1.5f, 5.0f)
            }
        };

        public static int Count => All.Length;

        public static CarSpec Of(CarModel model)
        {
            int index = Mathf.Clamp((int)model, 0, All.Length - 1);
            return All[index];
        }

        /// <summary>The car of a tier (1 to 6).</summary>
        public static CarModel OfTier(int tier)
        {
            return (CarModel)Mathf.Clamp(tier - 1, 0, All.Length - 1);
        }
    }

    /// <summary>
    /// What a car brings to a race: the model, its upgrade levels and equipment, the mines, rockets and nitro it carries
    /// and how damaged it is. The career keeps one per car in the garage; the computer's cars and quick races make one.
    /// Plain serializable fields, so it is saved inside a career save as it is.
    /// </summary>
    [Serializable]
    public sealed class CarLoadout
    {
        public CarModel model;
        public int engine;
        public int tires;
        public int armor;

        /// <summary>A spiked bumper: ramming hurts the other car more and this one less.</summary>
        public bool spikes;

        /// <summary>Armour-piercing rounds: the machine gun hits harder.</summary>
        public bool piercing;

        public int mines;
        public int rockets;
        public int nitro;

        /// <summary>How much of its health the car starts with, 0 to 1.</summary>
        public float health = 1f;

        public CarSpec Spec => CarSpecs.Of(model);

        public int Level(UpgradeKind kind)
        {
            switch (kind)
            {
                case UpgradeKind.Engine: return engine;
                case UpgradeKind.Tires: return tires;
                default: return armor;
            }
        }

        public void SetLevel(UpgradeKind kind, int level)
        {
            level = Mathf.Clamp(level, 0, Spec.MaxLevelOf(kind));
            switch (kind)
            {
                case UpgradeKind.Engine: engine = level; break;
                case UpgradeKind.Tires: tires = level; break;
                default: armor = level; break;
            }
        }

        public CarStats Stats()
        {
            return CarStats.Of(this);
        }

        public CarLoadout Clone()
        {
            return (CarLoadout)MemberwiseClone();
        }

        /// <summary>A car of <paramref name="model"/> with every part at <paramref name="level"/> (capped per part).</summary>
        public static CarLoadout Upgraded(CarModel model, int level)
        {
            var loadout = new CarLoadout { model = model };
            foreach (UpgradeKind kind in (UpgradeKind[])Enum.GetValues(typeof(UpgradeKind)))
            {
                loadout.SetLevel(kind, level);
            }
            return loadout;
        }
    }

    /// <summary>
    /// The numbers of a car with its upgrades and equipment, which the physics and the weapons read. Each upgrade level
    /// adds a share of the base value, so an upgraded cheap car never catches up with an upgraded expensive one.
    /// </summary>
    public struct CarStats
    {
        public float MaxHealth;
        public float TopSpeed;
        public float Acceleration;
        public float Grip;
        public float Steering;
        public float Mass;

        /// <summary>What incoming damage is multiplied by (armour lowers it).</summary>
        public float DamageTaken;

        /// <summary>What the damage of this car ramming another is multiplied by.</summary>
        public float RamDealt;

        /// <summary>What the damage this car takes from ramming head on is multiplied by.</summary>
        public float RamTaken;

        public float GunDamage;
        public int AmmoCapacity;

        public const float EnginePerLevelSpeed = 0.06f;
        public const float EnginePerLevelAcceleration = 0.09f;
        public const float TiresPerLevelGrip = 0.07f;
        public const float TiresPerLevelSteering = 0.05f;
        public const float ArmorPerLevelProtection = 0.09f;
        public const float ArmorPerLevelHealth = 0.05f;

        public static CarStats Of(CarLoadout loadout)
        {
            CarSpec spec = loadout != null ? loadout.Spec : CarSpecs.All[0];
            int engine = loadout?.engine ?? 0;
            int tires = loadout?.tires ?? 0;
            int armor = loadout?.armor ?? 0;
            return new CarStats
            {
                MaxHealth = spec.MaxHealth * (1f + ArmorPerLevelHealth * armor),
                TopSpeed = spec.TopSpeed * (1f + EnginePerLevelSpeed * engine),
                Acceleration = spec.Acceleration * (1f + EnginePerLevelAcceleration * engine),
                Grip = spec.Grip * (1f + TiresPerLevelGrip * tires),
                Steering = spec.Steering * (1f + TiresPerLevelSteering * tires),
                Mass = spec.Mass,
                DamageTaken = Mathf.Max(0.3f, 1f - ArmorPerLevelProtection * armor),
                RamDealt = loadout != null && loadout.spikes ? 2.5f : 1f,
                RamTaken = loadout != null && loadout.spikes ? 0.5f : 1f,
                GunDamage = loadout != null && loadout.piercing ? 1.5f : 1f,
                AmmoCapacity = spec.AmmoCapacity
            };
        }
    }
}
