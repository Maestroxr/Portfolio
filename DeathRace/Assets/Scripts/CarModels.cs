using System;
using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// The twelve cars of the game, from the wreck a career starts in to the interceptor of the last league. The first
    /// six are the cars of tiers 1 to 6; the next three are other cars of tiers 2, 4 and 5 (a different way to spend the
    /// same money), and the last three are the higher tiers 7 to 9 the harder leagues race. The numbers are what saves
    /// keep, so a car is only ever added at the end.
    /// </summary>
    public enum CarModel
    {
        Rustbucket = 0,
        Hornet = 1,
        Bulldog = 2,
        Viper = 3,
        Reaper = 4,
        Tyrant = 5,
        Jackal = 6,
        Mauler = 7,
        Banshee = 8,
        Spectre = 9,
        Goliath = 10,
        Apocalypse = 11
    }

    /// <summary>What the garage upgrades on a car, level by level.</summary>
    public enum UpgradeKind
    {
        Engine = 0,
        Tires = 1,
        Armor = 2,

        /// <summary>The nitro system: every charge burns longer and pushes harder.</summary>
        Boost = 3,

        /// <summary>The gun mount: the gun fires faster, hits harder and carries more rounds.</summary>
        GunMount = 4
    }

    /// <summary>The gun on the bonnet, fired with the machine gun's button. A car has the machine gun from the start.</summary>
    public enum GunKind
    {
        MachineGun = 0,
        Minigun = 1,
        Flamethrower = 2,
        Plasma = 3
    }

    /// <summary>The special weapons, fired with their own button: the one armed, or the next with any left.</summary>
    public enum SpecialKind
    {
        /// <summary>A slick of oil behind the car: whoever drives through it loses the road.</summary>
        Oil = 0,

        /// <summary>An electric pulse round the car: every other car near it is jammed for a few seconds.</summary>
        Emp = 1,

        /// <summary>A shell lobbed over the cars ahead, which bursts where it lands.</summary>
        Mortar = 2
    }

    /// <summary>The fittings of a car, bought once each in the garage's workshop.</summary>
    public enum EquipmentKind
    {
        /// <summary>A spiked bumper: ramming hurts the other car more and this one less.</summary>
        Spikes = 0,

        /// <summary>Armour-piercing rounds: every gun hits harder.</summary>
        Piercing = 1,

        /// <summary>A roll cage: crashes into cars and barriers hurt less.</summary>
        Cage = 2,

        /// <summary>Blast shielding under the car: mines, rockets, shells and explosions hurt less.</summary>
        Shield = 3,

        /// <summary>A radar jammer: homing rockets never lock on to the car.</summary>
        Jammer = 4,

        /// <summary>A field welder: the car mends a little while it is not being hit.</summary>
        Welder = 5,

        /// <summary>An extended magazine: half as many rounds again.</summary>
        Magazine = 6
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

        /// <summary>1 (the Rustbucket) to <see cref="CarSpecs.MaxTier"/> (the Apocalypse): what prices and the computer's choice of cars go by.</summary>
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

        /// <summary>The rounds of the machine gun (other guns carry a share of it, <see cref="GunSpec.AmmoFactor"/>).</summary>
        public int AmmoCapacity;

        /// <summary>The highest level of each <see cref="UpgradeKind"/>, in the order of the enum.</summary>
        public int[] MaxLevel;

        /// <summary>The box the car collides with: width, height, length in metres.</summary>
        public Vector3 Size;

        /// <summary>The first car of its tier: the one a tier names (the others are alternatives of the same tier).</summary>
        public bool MainOfTier;

        public int MaxLevelOf(UpgradeKind kind)
        {
            int index = (int)kind;
            return MaxLevel != null && index >= 0 && index < MaxLevel.Length ? MaxLevel[index] : 0;
        }
    }

    /// <summary>The table of the twelve cars.</summary>
    public static class CarSpecs
    {
        /// <summary>The highest tier of a car.</summary>
        public const int MaxTier = 9;

        public static readonly CarSpec[] All =
        {
            new CarSpec
            {
                Model = CarModel.Rustbucket, Name = "Rustbucket", Blurb = "A hatchback held together by rust and hope. Free, and worth it.",
                Tier = 1, Price = 0, MaxHealth = 100f, TopSpeed = 30f, Acceleration = 13f, Grip = 1f, Steering = 1f, Mass = 1100f,
                AmmoCapacity = 100, MaxLevel = new[] { 2, 2, 2, 1, 1 }, Size = new Vector3(2.0f, 1.4f, 4.2f), MainOfTier = true
            },
            new CarSpec
            {
                Model = CarModel.Hornet, Name = "Hornet", Blurb = "A light coupe that darts through the pack. Quick off the line, quicker into the barrier.",
                Tier = 2, Price = 5000, MaxHealth = 110f, TopSpeed = 34f, Acceleration = 15f, Grip = 1.08f, Steering = 1.08f, Mass = 1150f,
                AmmoCapacity = 120, MaxLevel = new[] { 3, 3, 3, 2, 2 }, Size = new Vector3(1.9f, 1.25f, 4.3f), MainOfTier = true
            },
            new CarSpec
            {
                Model = CarModel.Bulldog, Name = "Bulldog", Blurb = "A pickup with a bull bar and a grudge. Slow to turn, hard to stop, harder to kill.",
                Tier = 3, Price = 14000, MaxHealth = 150f, TopSpeed = 33f, Acceleration = 14.5f, Grip = 1f, Steering = 0.95f, Mass = 1500f,
                AmmoCapacity = 140, MaxLevel = new[] { 3, 3, 5, 2, 2 }, Size = new Vector3(2.2f, 1.7f, 4.8f), MainOfTier = true
            },
            new CarSpec
            {
                Model = CarModel.Viper, Name = "Viper", Blurb = "Eight cylinders, a spoiler and no patience. The first car that can win a hard race.",
                Tier = 4, Price = 30000, MaxHealth = 120f, TopSpeed = 40f, Acceleration = 18f, Grip = 1.15f, Steering = 1.12f, Mass = 1250f,
                AmmoCapacity = 150, MaxLevel = new[] { 4, 4, 3, 3, 3 }, Size = new Vector3(2.0f, 1.25f, 4.6f), MainOfTier = true
            },
            new CarSpec
            {
                Model = CarModel.Reaper, Name = "Reaper", Blurb = "A low black wedge that corners like it is on rails. Expensive, and it shows.",
                Tier = 5, Price = 60000, MaxHealth = 140f, TopSpeed = 44f, Acceleration = 20f, Grip = 1.22f, Steering = 1.18f, Mass = 1300f,
                AmmoCapacity = 170, MaxLevel = new[] { 4, 4, 4, 3, 3 }, Size = new Vector3(2.05f, 1.1f, 4.5f), MainOfTier = true
            },
            new CarSpec
            {
                Model = CarModel.Tyrant, Name = "Tyrant", Blurb = "An armoured interceptor built for one thing: being the last car running.",
                Tier = 6, Price = 120000, MaxHealth = 175f, TopSpeed = 48f, Acceleration = 22f, Grip = 1.28f, Steering = 1.18f, Mass = 1700f,
                AmmoCapacity = 200, MaxLevel = new[] { 5, 5, 5, 4, 4 }, Size = new Vector3(2.3f, 1.5f, 5.0f), MainOfTier = true
            },
            new CarSpec
            {
                Model = CarModel.Jackal, Name = "Jackal", Blurb = "A dune buggy: a roll cage, an engine and nothing to hide behind. First off the line, first to fall apart.",
                Tier = 2, Price = 6500, MaxHealth = 95f, TopSpeed = 35f, Acceleration = 16.5f, Grip = 1.05f, Steering = 1.15f, Mass = 950f,
                AmmoCapacity = 110, MaxLevel = new[] { 3, 3, 2, 3, 2 }, Size = new Vector3(1.95f, 1.35f, 3.9f)
            },
            new CarSpec
            {
                Model = CarModel.Mauler, Name = "Mauler", Blurb = "An armoured security van with a battering ram. Slow, but everything bounces off it.",
                Tier = 4, Price = 27000, MaxHealth = 180f, TopSpeed = 37f, Acceleration = 15.5f, Grip = 1.04f, Steering = 0.96f, Mass = 1800f,
                AmmoCapacity = 170, MaxLevel = new[] { 3, 3, 5, 2, 3 }, Size = new Vector3(2.3f, 1.85f, 5.0f)
            },
            new CarSpec
            {
                Model = CarModel.Banshee, Name = "Banshee", Blurb = "A supercharged muscle car that screams down the straights and sulks in the corners.",
                Tier = 5, Price = 55000, MaxHealth = 130f, TopSpeed = 46f, Acceleration = 21f, Grip = 1.12f, Steering = 1.06f, Mass = 1450f,
                AmmoCapacity = 160, MaxLevel = new[] { 5, 3, 4, 4, 2 }, Size = new Vector3(2.1f, 1.25f, 4.8f)
            },
            new CarSpec
            {
                Model = CarModel.Spectre, Name = "Spectre", Blurb = "A matte stealth hypercar. By the time you hear it, it has passed you.",
                Tier = 7, Price = 200000, MaxHealth = 165f, TopSpeed = 50f, Acceleration = 24f, Grip = 1.34f, Steering = 1.24f, Mass = 1350f,
                AmmoCapacity = 220, MaxLevel = new[] { 5, 5, 4, 4, 4 }, Size = new Vector3(2.1f, 1.05f, 4.7f), MainOfTier = true
            },
            new CarSpec
            {
                Model = CarModel.Goliath, Name = "Goliath", Blurb = "A war rig with a plough, twin guns and armour plate. It does not overtake; it goes through.",
                Tier = 8, Price = 320000, MaxHealth = 250f, TopSpeed = 49f, Acceleration = 23f, Grip = 1.3f, Steering = 1.1f, Mass = 2300f,
                AmmoCapacity = 280, MaxLevel = new[] { 5, 5, 6, 4, 5 }, Size = new Vector3(2.5f, 1.95f, 5.6f), MainOfTier = true
            },
            new CarSpec
            {
                Model = CarModel.Apocalypse, Name = "Apocalypse", Blurb = "A jet-assisted interceptor built from the Warden's own blueprints. The last car you will ever need.",
                Tier = 9, Price = 500000, MaxHealth = 215f, TopSpeed = 52f, Acceleration = 26f, Grip = 1.4f, Steering = 1.26f, Mass = 1650f,
                AmmoCapacity = 300, MaxLevel = new[] { 5, 6, 6, 5, 5 }, Size = new Vector3(2.2f, 1.2f, 5.1f), MainOfTier = true
            }
        };

        public static int Count => All.Length;

        public static CarSpec Of(CarModel model)
        {
            int index = Mathf.Clamp((int)model, 0, All.Length - 1);
            return All[index];
        }

        /// <summary>The main car of a tier (1 to <see cref="MaxTier"/>): the Rustbucket, Hornet, ... Tyrant, Spectre, Goliath, Apocalypse.</summary>
        public static CarModel OfTier(int tier)
        {
            tier = Mathf.Clamp(tier, 1, MaxTier);
            foreach (CarSpec spec in All)
            {
                if (spec.Tier == tier && spec.MainOfTier)
                {
                    return spec.Model;
                }
            }
            return CarModel.Rustbucket;
        }

        /// <summary>Every car of <paramref name="tier"/>, the main one first.</summary>
        public static List<CarModel> InTier(int tier)
        {
            var models = new List<CarModel>();
            foreach (CarSpec spec in All)
            {
                if (spec.Tier == tier)
                {
                    if (spec.MainOfTier)
                    {
                        models.Insert(0, spec.Model);
                    }
                    else
                    {
                        models.Add(spec.Model);
                    }
                }
            }
            return models;
        }

        /// <summary>
        /// The car of <paramref name="tier"/> a driver with <paramref name="driverId"/> drives: always the same one for the
        /// same driver (a stable hash of the id picks among the cars of the tier), so a rival keeps their kind of car.
        /// </summary>
        public static CarModel ForDriver(int tier, string driverId)
        {
            List<CarModel> models = InTier(Mathf.Clamp(tier, 1, MaxTier));
            if (models.Count == 0)
            {
                return OfTier(tier);
            }
            uint hash = 2166136261u;
            foreach (char c in driverId ?? "")
            {
                hash = (hash ^ c) * 16777619u;
            }
            return models[(int)(hash % (uint)models.Count)];
        }
    }

    /// <summary>
    /// What a car brings to a race: the model, its upgrade levels and equipment, the gun mounted (and the others owned),
    /// the mines, rockets, nitro and special weapons it carries and how damaged it is. The career keeps one per car in
    /// the garage; the computer's cars and quick races make one. Plain serializable fields, so it is saved inside a
    /// career save as it is (a field added later reads as its default from an older save).
    /// </summary>
    [Serializable]
    public sealed class CarLoadout
    {
        public CarModel model;
        public int engine;
        public int tires;
        public int armor;

        /// <summary>The nitro system's level (<see cref="UpgradeKind.Boost"/>).</summary>
        public int boost;

        /// <summary>The gun mount's level (<see cref="UpgradeKind.GunMount"/>).</summary>
        public int gunMount;

        /// <summary>A spiked bumper: ramming hurts the other car more and this one less.</summary>
        public bool spikes;

        /// <summary>Armour-piercing rounds: the gun hits harder.</summary>
        public bool piercing;

        public bool cage;
        public bool shield;
        public bool jammer;
        public bool welder;
        public bool magazine;

        /// <summary>The gun mounted on the bonnet.</summary>
        public GunKind gun;

        /// <summary>The guns bought for the car (bit 1 &lt;&lt; GunKind); the machine gun is always there.</summary>
        public int guns;

        public int mines;
        public int rockets;
        public int nitro;

        public int oil;
        public int emp;
        public int mortar;

        /// <summary>The special weapon its button fires first (when there are none of it left, the next one that has some).</summary>
        public SpecialKind special;

        /// <summary>How much of its health the car starts with, 0 to 1.</summary>
        public float health = 1f;

        public CarSpec Spec => CarSpecs.Of(model);

        public int Level(UpgradeKind kind)
        {
            switch (kind)
            {
                case UpgradeKind.Engine: return engine;
                case UpgradeKind.Tires: return tires;
                case UpgradeKind.Armor: return armor;
                case UpgradeKind.Boost: return boost;
                default: return gunMount;
            }
        }

        public void SetLevel(UpgradeKind kind, int level)
        {
            level = Mathf.Clamp(level, 0, Spec.MaxLevelOf(kind));
            switch (kind)
            {
                case UpgradeKind.Engine: engine = level; break;
                case UpgradeKind.Tires: tires = level; break;
                case UpgradeKind.Armor: armor = level; break;
                case UpgradeKind.Boost: boost = level; break;
                default: gunMount = level; break;
            }
        }

        public bool Has(EquipmentKind kind)
        {
            switch (kind)
            {
                case EquipmentKind.Spikes: return spikes;
                case EquipmentKind.Piercing: return piercing;
                case EquipmentKind.Cage: return cage;
                case EquipmentKind.Shield: return shield;
                case EquipmentKind.Jammer: return jammer;
                case EquipmentKind.Welder: return welder;
                default: return magazine;
            }
        }

        public void Set(EquipmentKind kind, bool fitted)
        {
            switch (kind)
            {
                case EquipmentKind.Spikes: spikes = fitted; break;
                case EquipmentKind.Piercing: piercing = fitted; break;
                case EquipmentKind.Cage: cage = fitted; break;
                case EquipmentKind.Shield: shield = fitted; break;
                case EquipmentKind.Jammer: jammer = fitted; break;
                case EquipmentKind.Welder: welder = fitted; break;
                default: magazine = fitted; break;
            }
        }

        /// <summary>Whether the car has <paramref name="kind"/> to mount (the machine gun always).</summary>
        public bool OwnsGun(GunKind kind)
        {
            return kind == GunKind.MachineGun || (guns & (1 << (int)kind)) != 0;
        }

        /// <summary>Adds <paramref name="kind"/> to the guns the car has; <paramref name="mount"/> puts it on the bonnet too.</summary>
        public void GiveGun(GunKind kind, bool mount = true)
        {
            if (kind != GunKind.MachineGun)
            {
                guns |= 1 << (int)kind;
            }
            if (mount)
            {
                gun = kind;
            }
        }

        /// <summary>How many of the special weapon <paramref name="kind"/> the car carries.</summary>
        public int Specials(SpecialKind kind)
        {
            switch (kind)
            {
                case SpecialKind.Oil: return oil;
                case SpecialKind.Emp: return emp;
                default: return mortar;
            }
        }

        public void SetSpecials(SpecialKind kind, int count)
        {
            count = Mathf.Max(0, count);
            switch (kind)
            {
                case SpecialKind.Oil: oil = count; break;
                case SpecialKind.Emp: emp = count; break;
                default: mortar = count; break;
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

        /// <summary>What the gun's damage is multiplied by (armour-piercing rounds, the gun mount).</summary>
        public float GunDamage;

        /// <summary>What the gun's rate of fire is multiplied by (the gun mount).</summary>
        public float GunRate;

        /// <summary>The gun mounted.</summary>
        public GunKind Gun;

        /// <summary>The rounds the mounted gun carries into a race.</summary>
        public int AmmoCapacity;

        /// <summary>How long a nitro charge burns, in seconds.</summary>
        public float NitroSeconds;

        /// <summary>What the extra speed and pull of a burning charge are multiplied by (1 = stock).</summary>
        public float NitroPower;

        /// <summary>What the damage of crashes into cars and barriers (and scraping) is multiplied by (the roll cage).</summary>
        public float CrashTaken;

        /// <summary>What the damage of blasts (mines, rockets, shells, explosions) is multiplied by (blast shielding).</summary>
        public float BlastTaken;

        /// <summary>Homing rockets never lock on to the car.</summary>
        public bool Jammer;

        /// <summary>The car mends a little while it is not being hit.</summary>
        public bool Welder;

        public const float EnginePerLevelSpeed = 0.06f;
        public const float EnginePerLevelAcceleration = 0.09f;
        public const float TiresPerLevelGrip = 0.07f;
        public const float TiresPerLevelSteering = 0.05f;
        public const float ArmorPerLevelProtection = 0.09f;
        public const float ArmorPerLevelHealth = 0.05f;

        /// <summary>A nitro system level: the charge burns this share longer and pushes this share harder.</summary>
        public const float BoostPerLevelTime = 0.12f;
        public const float BoostPerLevelPower = 0.06f;

        /// <summary>A gun mount level: this share more damage, rate of fire and rounds.</summary>
        public const float GunMountPerLevelDamage = 0.06f;
        public const float GunMountPerLevelRate = 0.05f;
        public const float GunMountPerLevelAmmo = 0.1f;

        public const float PiercingDamage = 1.5f;
        public const float SpikesRamDealt = 2.5f;
        public const float SpikesRamTaken = 0.5f;
        public const float CageCrashTaken = 0.6f;
        public const float ShieldBlastTaken = 0.7f;
        public const float MagazineAmmo = 1.5f;

        public static CarStats Of(CarLoadout loadout)
        {
            CarSpec spec = loadout != null ? loadout.Spec : CarSpecs.All[0];
            int engine = loadout?.engine ?? 0;
            int tires = loadout?.tires ?? 0;
            int armor = loadout?.armor ?? 0;
            int boost = loadout?.boost ?? 0;
            int mount = loadout?.gunMount ?? 0;
            GunKind gun = loadout != null && loadout.OwnsGun(loadout.gun) ? loadout.gun : GunKind.MachineGun;
            float ammo = spec.AmmoCapacity * Guns.Of(gun).AmmoFactor * (1f + GunMountPerLevelAmmo * mount) *
                         (loadout != null && loadout.magazine ? MagazineAmmo : 1f);
            return new CarStats
            {
                MaxHealth = spec.MaxHealth * (1f + ArmorPerLevelHealth * armor),
                TopSpeed = spec.TopSpeed * (1f + EnginePerLevelSpeed * engine),
                Acceleration = spec.Acceleration * (1f + EnginePerLevelAcceleration * engine),
                Grip = spec.Grip * (1f + TiresPerLevelGrip * tires),
                Steering = spec.Steering * (1f + TiresPerLevelSteering * tires),
                Mass = spec.Mass,
                DamageTaken = Mathf.Max(0.3f, 1f - ArmorPerLevelProtection * armor),
                RamDealt = loadout != null && loadout.spikes ? SpikesRamDealt : 1f,
                RamTaken = loadout != null && loadout.spikes ? SpikesRamTaken : 1f,
                GunDamage = (loadout != null && loadout.piercing ? PiercingDamage : 1f) * (1f + GunMountPerLevelDamage * mount),
                GunRate = 1f + GunMountPerLevelRate * mount,
                Gun = gun,
                AmmoCapacity = Mathf.Max(1, Mathf.RoundToInt(ammo)),
                NitroSeconds = CombatRules.NitroSeconds * (1f + BoostPerLevelTime * boost),
                NitroPower = 1f + BoostPerLevelPower * boost,
                CrashTaken = loadout != null && loadout.cage ? CageCrashTaken : 1f,
                BlastTaken = loadout != null && loadout.shield ? ShieldBlastTaken : 1f,
                Jammer = loadout != null && loadout.jammer,
                Welder = loadout != null && loadout.welder
            };
        }
    }
}
