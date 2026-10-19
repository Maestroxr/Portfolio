using System;

namespace Portfolio.DeathRace
{
    /// <summary>One driver of the career as they are written: an id that saves keep, the name the ladder shows and a line about them.</summary>
    public sealed class RosterDriver
    {
        public readonly string Id;
        public readonly string Name;
        public readonly string Blurb;

        public RosterDriver(string id, string name, string blurb)
        {
            Id = id;
            Name = name;
            Blurb = blurb;
        }
    }

    /// <summary>
    /// The nineteen rivals of the ladder from the worst to the best on the first day, and The Warden, the champion who
    /// waits above it. The names are English keys of the localization table (a Hebrew player reads them in Hebrew); the
    /// ids are what saves keep, so they never change even if a name does.
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
            new RosterDriver("sally", "Sally Shrapnel", "Drops mines like breadcrumbs."),
            new RosterDriver("crash", "Crash Kowalski", "Brakes are for other people."),
            new RosterDriver("nell", "Nitro Nell", "Never seen a straight she could not set on fire."),
            new RosterDriver("diesel", "Diesel Dan", "Slow, heavy and impossible to push off the road."),
            new RosterDriver("scorpion", "Scorpion Sue", "Smiles at you on the grid. Shoots you at the first corner."),
            new RosterDriver("butcher", "The Butcher", "Keeps a tally of wrecks painted on his door."),
            new RosterDriver("lockjaw", "Lady Lockjaw", "Once she is on your bumper she does not let go."),
            new RosterDriver("napalm", "Johnny Napalm", "Lit his first car on fire on purpose."),
            new RosterDriver("venom", "Vera Venom", "Armour-piercing rounds and a short temper."),
            new RosterDriver("ivan", "Iron Ivan", "Drives a tank and calls it a car."),
            new RosterDriver("blaze", "Blaze Monroe", "Former stunt driver. Still does the stunts, now with rockets."),
            new RosterDriver("hammerhead", "Hammerhead", "Rams first, asks never."),
            new RosterDriver("maxine", "Mad Maxine", "The fastest pair of hands in the wasteland."),
            new RosterDriver("vex", "Viktor Vex", "Cold, clean and three wins from the title."),
            new RosterDriver("undertaker", "The Undertaker", "Measures his rivals for coffins before the green light.")
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

        /// <summary>The driver with <paramref name="id"/> (a rival or The Warden), or null.</summary>
        public static RosterDriver Find(string id)
        {
            if (id == WardenId)
            {
                return Warden;
            }
            int index = IndexOf(id);
            return index >= 0 ? Rivals[index] : null;
        }
    }
}
