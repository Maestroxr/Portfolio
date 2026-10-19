using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>The kind of land a track runs through: what the ground, the barriers and the scenery look like.</summary>
    public enum Biome
    {
        Desert = 0,
        Industrial = 1,
        Snow = 2,
        City = 3
    }

    /// <summary>How hard a race is: who races in it and what it pays. The Showdown is the race against the champion.</summary>
    public enum Difficulty
    {
        Easy = 0,
        Medium = 1,
        Hard = 2,
        Showdown = 3
    }

    /// <summary>Why a race is run, which decides what follows it.</summary>
    public enum RaceMode
    {
        /// <summary>Computer cars racing behind the main menu.</summary>
        Attract = 0,
        Career = 1,
        Quick = 2,
        /// <summary>Several people at this device (split screen).</summary>
        Local = 3,
        Online = 4
    }

    /// <summary>Who drives a car.</summary>
    public enum DriverKind
    {
        Human = 0,
        Computer = 1,
        /// <summary>A player on another device: the car follows what the server says.</summary>
        Remote = 2
    }

    /// <summary>What lies on the track to be driven over.</summary>
    public enum PickupKind
    {
        Cash = 0,
        Repair = 1,
        Ammo = 2,
        Nitro = 3
    }

    /// <summary>One car of a race and who drives it.</summary>
    public sealed class RaceEntrant
    {
        /// <summary>The car's place on the grid, from 0; the player of a career or quick race is 0.</summary>
        public int Seat;

        public string Name = "";
        public Color Color = Color.white;
        public DriverKind Driver = DriverKind.Computer;

        /// <summary>How well a computer drives and shoots, 0 to 1.</summary>
        public float Skill = 0.5f;

        public CarLoadout Loadout = new CarLoadout();

        /// <summary>The career ladder's id of the driver, or null.</summary>
        public string RivalId;

        /// <summary>The <see cref="Gamebox.PlayerControls"/> of a person at this device; null reads the default keys.</summary>
        public object Controls;

        /// <summary>The split screen view of a person at this device (from 0), or -1 for none.</summary>
        public int Viewport = -1;

        /// <summary>The seat a remote player has in the online room, or -1.</summary>
        public int RoomSeat = -1;

        public bool IsHuman => Driver == DriverKind.Human;
    }

    /// <summary>Everything a race is set up from: the track, laps, the cars and who drives them.</summary>
    public sealed class RaceSetup
    {
        public RaceMode Mode = RaceMode.Quick;

        /// <summary>The track: an index into <c>TrackLayouts.All</c>, also the campaign level of the same index.</summary>
        public int Track;

        public int Laps = 4;
        public Difficulty Difficulty = Difficulty.Medium;

        /// <summary>Seeds what is random about the race (the computer's choices, scenery, pickups).</summary>
        public int Seed;

        public List<RaceEntrant> Entrants = new List<RaceEntrant>();

        /// <summary>What a cash pickup is worth.</summary>
        public int CashPerPickup = 50;

        public bool Pickups = true;

        public RaceEntrant EntrantAt(int seat)
        {
            foreach (RaceEntrant entrant in Entrants)
            {
                if (entrant.Seat == seat)
                {
                    return entrant;
                }
            }
            return null;
        }

        public int Humans
        {
            get
            {
                int humans = 0;
                foreach (RaceEntrant entrant in Entrants)
                {
                    humans += entrant.IsHuman ? 1 : 0;
                }
                return humans;
            }
        }
    }

    /// <summary>How one car's race went.</summary>
    public sealed class RacerResult
    {
        public int Seat;
        public string Name;
        public Color Color;
        public DriverKind Driver;
        public string RivalId;

        /// <summary>From 1.</summary>
        public int Place;

        public bool Finished;
        public bool Wrecked;

        /// <summary>Seconds: the finishing time, or when the car was wrecked or the race ended.</summary>
        public float Time;

        public float BestLap;
        public int LapsCompleted;
        public int Kills;
        public int CashPicked;

        /// <summary>The health left, 0 to 1.</summary>
        public float HealthLeft;

        public int MinesUsed;
        public int RocketsUsed;
        public int NitroUsed;
    }

    /// <summary>How a race went: the setup and every car's result, in the order of the places.</summary>
    public sealed class RaceResult
    {
        public RaceSetup Setup;
        public List<RacerResult> Racers = new List<RacerResult>();

        public RacerResult Of(int seat)
        {
            foreach (RacerResult racer in Racers)
            {
                if (racer.Seat == seat)
                {
                    return racer;
                }
            }
            return null;
        }
    }
}
