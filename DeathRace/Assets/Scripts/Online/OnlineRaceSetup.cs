using System;
using System.Collections.Generic;
using Gamebox;
using Gamebox.Online;
using Gamebox.Racing;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// The race of an online room as every device builds it (<see cref="Build"/>), and what a room tells the server about
    /// its track (<see cref="OptionsFor"/>): plain code over the room's numbers, apart from the connection, so the same
    /// room gives the same race on every device and the tests can check it.
    /// </summary>
    public static class OnlineRaceSetup
    {
        /// <summary>
        /// One member of a room as the race needs them: their seat, their name, whether their car is driven at this
        /// device (a player here, or a remote control's seat on the room's shared screen).
        /// </summary>
        public struct RoomRacer
        {
            public byte Seat;
            public string Name;

            /// <summary>Their car is simulated and driven here (<see cref="Gamebox.Online.OnlineGameController.DrivesSeat"/>).</summary>
            public bool Local;

            /// <summary>
            /// Driven here from a remote control (this device is the room's shared screen): their controls come over the
            /// network and are always the ones read, however many drive here.
            /// </summary>
            public bool Remote;

            /// <summary>Their place among the cars driven here (their view and controls), -1 elsewhere.</summary>
            public int LocalIndex;

            /// <summary>The controls bound for them when several drive at this device, or their remote control's.</summary>
            public PlayerControls Controls;
        }

        /// <summary>
        /// The race of a room on <paramref name="track"/> with <paramref name="options"/> and <paramref name="seed"/> for
        /// <paramref name="members"/>: every member a car in the order of their seats (that is the grid), all in the room's
        /// car with the same load, the cars driven at this device as people (each on the controls the local play setup binds
        /// for them when several drive here, or on their remote control's, in a view each), the others as cars of other
        /// devices. A member keeps their seat's colour on every device (<paramref name="seatColor"/>).
        /// </summary>
        public static RaceSetup Build(int track, string options, uint seed, IReadOnlyList<RoomRacer> members, Func<int, Color> seatColor)
        {
            TrackLayout layout = TrackLayouts.Of(track);
            var setup = new RaceSetup
            {
                Mode = RaceMode.Online,
                Track = Mathf.Clamp(track, 0, TrackLayouts.Count - 1),
                Laps = OnlineRaceRules.LapsOf(RoomOptions.Number(options, OnlineRaceRules.LapsOption, 0), layout.Laps),
                Difficulty = Difficulty.Medium,
                // A seed of the room fits a System.Random as long as it is positive.
                Seed = Mathf.Max(1, (int)(seed & int.MaxValue)),
                CashPerPickup = CareerRules.CashPerPickup(Difficulty.Medium),
                Pickups = true
            };
            var car = (CarModel)Mathf.Clamp(RoomOptions.Number(options, OnlineRaceRules.CarOption, (int)DeathRaceLocalRules.DefaultCar), 0, CarSpecs.Count - 1);
            var ordered = new List<RoomRacer>(members);
            ordered.Sort((a, b) => a.Seat.CompareTo(b.Seat));
            int localCount = 0;
            foreach (RoomRacer member in ordered)
            {
                localCount += member.Local ? 1 : 0;
            }
            foreach (RoomRacer member in ordered)
            {
                var entrant = new RaceEntrant
                {
                    Seat = setup.Entrants.Count,
                    RoomSeat = member.Seat,
                    Name = member.Name ?? "",
                    Color = seatColor != null ? seatColor(member.Seat) : Color.white,
                    Driver = member.Local ? DriverKind.Human : DriverKind.Remote,
                    Skill = 1f,
                    Loadout = DeathRaceLocalRules.Loadout(car)
                };
                if (member.Local)
                {
                    entrant.Viewport = Mathf.Max(0, member.LocalIndex);
                    // Alone at the device the player keeps every keyboard layout; a party drives on a set each, and a seat
                    // driven from a remote control on the controls that arrive over the network.
                    entrant.Controls = member.Remote || localCount > 1 ? member.Controls : null;
                }
                setup.Entrants.Add(entrant);
            }
            return setup;
        }

        /// <summary>The options of a room on <paramref name="level"/> with the host's <paramref name="picked"/> laps and car.</summary>
        public static string OptionsFor(int level, string picked)
        {
            TrackLayout layout = TrackLayouts.Of(level);
            int laps = RoomOptions.Number(picked, OnlineRaceRules.LapsOption, 0);
            int car = Mathf.Clamp(RoomOptions.Number(picked, OnlineRaceRules.CarOption, (int)DeathRaceLocalRules.DefaultCar), 0, CarSpecs.Count - 1);
            return RoomOptions.Write(OnlineRaceRules.LapsOption, Mathf.Max(0, laps), OnlineRaceRules.CarOption, car,
                OnlineRaceRules.TrackLapsOption, layout.Laps, OnlineRaceRules.LengthOption, LapLength(level));
        }

        /// <summary>The length of a lap of <paramref name="track"/> in whole metres, as the room tells the server.</summary>
        public static int LapLength(int track)
        {
            TrackLayout layout = TrackLayouts.Of(track);
            float length = new RacePath(layout.Points).Length;
            return Mathf.Clamp(Mathf.RoundToInt(length), OnlineRaceRules.MinLength, OnlineRaceRules.MaxLength);
        }

    }
}
