using System;
using System.Collections.Generic;

namespace Portfolio.BoardGames
{
    /// <summary>
    /// The seats of an online table played at this device: the seat of the player here and those of the players who
    /// joined at the same device (the companions of the base server), in seat order. One seat is the usual game; two
    /// fill the table, and the room then has no player on another device. The manager asks it who acts for the people
    /// here (<see cref="Acting"/>), whether a seat is theirs (<see cref="Has"/>) and which side the camera shows at the
    /// bottom (<see cref="HomeSide"/>), the same way local play treats one or two people at the device.
    /// </summary>
    public sealed class DeviceSeats
    {
        /// <summary>A table of the board games has two seats, 0 and 1.</summary>
        public const int TableSize = 2;

        /// <summary>No seat at all: a game that is not played online.</summary>
        public static readonly DeviceSeats None = new DeviceSeats(Array.Empty<int>());

        private readonly int[] seats;

        /// <summary>The seats among <paramref name="seats"/> that a table has (0 and 1), each once, in seat order; the others are dropped.</summary>
        public DeviceSeats(IEnumerable<int> seats)
        {
            var kept = new List<int>();
            if (seats != null)
            {
                foreach (int seat in seats)
                {
                    if (seat >= 0 && seat < TableSize && !kept.Contains(seat))
                    {
                        kept.Add(seat);
                    }
                }
            }
            kept.Sort();
            this.seats = kept.ToArray();
        }

        /// <summary>The seats played here, in seat order.</summary>
        public IReadOnlyList<int> All => seats;

        public int Count => seats.Length;

        /// <summary>Whether anybody at this device plays.</summary>
        public bool Any => seats.Length > 0;

        /// <summary>Whether more than one person plays at this device.</summary>
        public bool Several => seats.Length > 1;

        /// <summary>The first seat played here, or -1.</summary>
        public int First => seats.Length > 0 ? seats[0] : -1;

        /// <summary>Whether <paramref name="seat"/> is played at this device.</summary>
        public bool Has(int seat)
        {
            return Array.IndexOf(seats, seat) >= 0;
        }

        /// <summary>
        /// The seat the people here act for now: <paramref name="seatToAct"/> when it is one of theirs, else the first of
        /// theirs (whose turn it is not), or -1 when nobody here plays.
        /// </summary>
        public int Acting(int seatToAct)
        {
            return Has(seatToAct) ? seatToAct : First;
        }

        /// <summary>
        /// The side the camera shows at the bottom: the side of the one seat played here, and Light (White) when two
        /// people share the device, as local play shows it; Light too when nobody here plays.
        /// </summary>
        public Side HomeSide(TableSetup setup)
        {
            return setup != null && seats.Length == 1 ? setup.SideOf(seats[0]) : Side.Light;
        }

        public override string ToString()
        {
            return seats.Length == 0 ? "no seat" : string.Join(", ", seats);
        }
    }
}
