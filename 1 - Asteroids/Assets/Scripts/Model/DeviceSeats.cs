using System.Collections.Generic;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The seats of a room that are flown at one device in a shared mission: the device's own first, then the seats of the
    /// companions who play through its connection, in seat order. What a ship flown here does goes out under the seat of
    /// its pilot (<see cref="Of"/>): a wingman names its pilot's seat, the device's own ship names none and goes out
    /// under the device's seat. What arrives for any of these seats is this device's own doing (<see cref="Contains"/>).
    /// </summary>
    public sealed class DeviceSeats
    {
        /// <summary>No seat: the base server's number for "the caller's own seat" and for "nobody".</summary>
        public const byte NoSeat = byte.MaxValue;

        private readonly List<byte> seats = new List<byte>();

        /// <summary>The seats of <paramref name="ordered"/>, the device's own first (as <see cref="Order"/> puts them); a seat is kept once.</summary>
        public DeviceSeats(IEnumerable<byte> ordered)
        {
            if (ordered == null)
            {
                return;
            }
            foreach (byte seat in ordered)
            {
                if (seat != NoSeat && !seats.Contains(seat))
                {
                    seats.Add(seat);
                }
            }
        }

        /// <summary>The seats, the device's own first.</summary>
        public IReadOnlyList<byte> All => seats;

        /// <summary>The device's own seat, or <see cref="NoSeat"/> when it has none (not in a room).</summary>
        public byte Device => seats.Count > 0 ? seats[0] : NoSeat;

        /// <summary>More than one pilot flies at the device: a party.</summary>
        public bool IsParty => seats.Count > 1;

        /// <summary>Whether <paramref name="seat"/> is flown at this device.</summary>
        public bool Contains(int seat)
        {
            return seat >= 0 && seat < NoSeat && seats.Contains((byte)seat);
        }

        /// <summary>
        /// The seat a deed of a ship flown at this device goes out under: <paramref name="pilotSeat"/> when that is one of
        /// this device's seats (a wingman's pilot), else the device's own (its own ship has no seat of its own on a hit).
        /// </summary>
        public byte Of(int? pilotSeat)
        {
            return pilotSeat.HasValue && Contains(pilotSeat.Value) ? (byte)pilotSeat.Value : Device;
        }

        /// <summary>
        /// The seats played at a device in the order a shared mission takes them: <paramref name="device"/> (its own seat)
        /// first, then the other seats of <paramref name="local"/> in seat order. Without a seat of its own the device has
        /// no seats.
        /// </summary>
        public static List<byte> Order(byte device, IEnumerable<byte> local)
        {
            var ordered = new List<byte>();
            if (device == NoSeat)
            {
                return ordered;
            }
            ordered.Add(device);
            var others = new List<byte>();
            if (local != null)
            {
                foreach (byte seat in local)
                {
                    if (seat != device && seat != NoSeat && !others.Contains(seat))
                    {
                        others.Add(seat);
                    }
                }
            }
            others.Sort();
            ordered.AddRange(others);
            return ordered;
        }
    }
}
