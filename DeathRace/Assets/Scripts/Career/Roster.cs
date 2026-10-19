using System;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// One driver of the career as they are written: an id that saves keep, the name the ladder shows, a line about them,
    /// and what they like to fight with once their league's cars carry more than the machine gun (a gun and a special
    /// weapon of their own; a driver without a liking gets one picked by their id, the same every race).
    /// </summary>
    public sealed class RosterDriver
    {
        public readonly string Id;
        public readonly string Name;
        public readonly string Blurb;

        /// <summary>The gun the driver mounts when their league's computer cars mount guns, or null for one picked by the id.</summary>
        public readonly GunKind? Gun;

        /// <summary>The special weapon the driver carries, or null for one picked by the id.</summary>
        public readonly SpecialKind? Special;

        public RosterDriver(string id, string name, string blurb, GunKind? gun = null, SpecialKind? special = null)
        {
            Id = id;
            Name = name;
            Blurb = blurb;
            Gun = gun;
            Special = special;
        }

        /// <summary>
        /// A stable number of <paramref name="id"/> (FNV-1a over its characters, like <see cref="CarSpecs.ForDriver"/>): what
        /// picks a driver's gun, special and fittings when the roster does not say, so they keep them from race to race.
        /// </summary>
        public static uint Hash(string id, uint salt = 0u)
        {
            uint hash = 2166136261u ^ salt;
            foreach (char c in id ?? "")
            {
                hash = (hash ^ c) * 16777619u;
            }
            return hash;
        }
    }

    /// <summary>
    /// The nineteen rivals of the first league's ladder (the Wasteland Circuit) from the worst to the best on the first
    /// day, and The Warden, the champion who waits above it. The other leagues have rosters of their own
    /// (<see cref="Leagues"/>). The names are English keys of the localization table (a Hebrew player reads them in
    /// Hebrew); the ids are what saves keep, so they never change even if a name does.
    /// </summary>
    public static class Roster
    {
        public const string WardenId = "warden";

        public static readonly RosterDriver Warden = new RosterDriver(WardenId, "The Warden",
            "Nobody has seen the face behind the visor. Nobody who raced The Warden has kept their car.");

        /// <summary>The rivals, bottom of the ladder first.</summary>
        public static readonly RosterDriver[] Rivals =
        {
            new RosterDriver("terry", "Two-Bit Terry", "Races for gas money and usually loses it."),
            new RosterDriver("nails", "Rusty Nails", "Welds his own armour. It shows."),
            new RosterDriver("danny", "Dirt Nap Danny", "Has been wrecked more times than anyone alive."),
            new RosterDriver("lou", "Lugnut Lou", "Fixes cars by day, crashes them by night."),
            new RosterDriver("sally", "Sally Shrapnel", "Drops mines like breadcrumbs.", special: SpecialKind.Mortar),
            new RosterDriver("crash", "Crash Kowalski", "Brakes are for other people."),
            new RosterDriver("nell", "Nitro Nell", "Never seen a straight she could not set on fire.", GunKind.Flamethrower),
            new RosterDriver("diesel", "Diesel Dan", "Slow, heavy and impossible to push off the road.", special: SpecialKind.Oil),
            new RosterDriver("scorpion", "Scorpion Sue", "Smiles at you on the grid. Shoots you at the first corner."),
            new RosterDriver("butcher", "The Butcher", "Keeps a tally of wrecks painted on his door."),
            new RosterDriver("lockjaw", "Lady Lockjaw", "Once she is on your bumper she does not let go."),
            new RosterDriver("napalm", "Johnny Napalm", "Lit his first car on fire on purpose.", GunKind.Flamethrower),
            new RosterDriver("venom", "Vera Venom", "Armour-piercing rounds and a short temper.", GunKind.Minigun),
            new RosterDriver("ivan", "Iron Ivan", "Drives a tank and calls it a car."),
            new RosterDriver("blaze", "Blaze Monroe", "Former stunt driver. Still does the stunts, now with rockets.", special: SpecialKind.Mortar),
            new RosterDriver("hammerhead", "Hammerhead", "Rams first, asks never."),
            new RosterDriver("maxine", "Mad Maxine", "The fastest pair of hands in the wasteland.", GunKind.Minigun),
            new RosterDriver("vex", "Viktor Vex", "Cold, clean and three wins from the title.", GunKind.Plasma, SpecialKind.Emp),
            new RosterDriver("undertaker", "The Undertaker", "Measures his rivals for coffins before the green light.", GunKind.Plasma)
        };

        /// <summary>The roster position of the rival with <paramref name="id"/> (0 = the worst on the first day), or -1.</summary>
        public static int IndexOf(string id)
        {
            for (int i = 0; i < Rivals.Length; i++)
            {
                if (string.Equals(Rivals[i].Id, id, StringComparison.Ordinal))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>The driver with <paramref name="id"/>: a rival or the champion of any league, or null.</summary>
        public static RosterDriver Find(string id)
        {
            if (id == WardenId)
            {
                return Warden;
            }
            int index = IndexOf(id);
            return index >= 0 ? Rivals[index] : Leagues.OfDriver(id)?.Find(id);
        }
    }
}
