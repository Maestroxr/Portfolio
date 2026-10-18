using System;
using System.Collections.Generic;

namespace Portfolio.MemoryCards
{
    /// <summary>
    /// The seats of an online versus game as this device sees them. The board numbers its seats 0 to n - 1 in the order
    /// of the room's members, while the server knows them by their room seats, so the two are mapped both ways here. Some
    /// of the seats are played at this device: the device's own player and the companions who signed in at it to play
    /// through its connection (<see cref="Gamebox.Online.OnlineGameController.LocalSeats"/>); the manager accepts clicks
    /// for them, flips for whichever of them has the turn and hands the device over between them. Who is spoken to as
    /// "you" depends on how many sit here: one player is "you" and hears "your turn", as always; with several people at
    /// the device everybody is called by name, so nobody wonders who is meant.
    /// </summary>
    public sealed class OnlineSeats
    {
        private readonly List<int> roomSeats = new List<int>();
        private readonly List<int> local = new List<int>();

        /// <param name="roomSeats">The room seat of every player, in the order of the board.</param>
        /// <param name="localRoomSeats">The room seats of the players at this device.</param>
        /// <param name="deviceRoomSeat">The room seat of the device's own player; -1 for none.</param>
        public OnlineSeats(IEnumerable<int> roomSeats, IEnumerable<int> localRoomSeats, int deviceRoomSeat)
        {
            this.roomSeats.AddRange(roomSeats ?? throw new ArgumentNullException(nameof(roomSeats)));
            foreach (int roomSeat in localRoomSeats ?? throw new ArgumentNullException(nameof(localRoomSeats)))
            {
                int seat = BoardSeatOf(roomSeat);
                if (seat >= 0 && !local.Contains(seat))
                {
                    local.Add(seat);
                }
            }
            local.Sort();
            Device = BoardSeatOf(deviceRoomSeat);
        }

        /// <summary>How many players sit at the board.</summary>
        public int Count => roomSeats.Count;

        /// <summary>The board seats played at this device, in seat order.</summary>
        public IReadOnlyList<int> Local => local;

        /// <summary>The board seat of the device's own player, or -1.</summary>
        public int Device { get; }

        /// <summary>Whether several people play at this device.</summary>
        public bool IsParty => local.Count > 1;

        /// <summary>Whether <paramref name="seat"/> of the board is played at this device.</summary>
        public bool IsLocal(int seat)
        {
            return local.Contains(seat);
        }

        /// <summary>
        /// Whether the player of <paramref name="seat"/> is spoken to as "you": the one player at this device. With several
        /// people here everybody goes by name.
        /// </summary>
        public bool IsYou(int seat)
        {
            return local.Count == 1 && local[0] == seat;
        }

        /// <summary>The board seat of <paramref name="roomSeat"/>, or -1 when nobody sits there.</summary>
        public int BoardSeatOf(int roomSeat)
        {
            return roomSeats.IndexOf(roomSeat);
        }

        /// <summary>The room seat of board seat <paramref name="seat"/>, or -1.</summary>
        public int RoomSeatOf(int seat)
        {
            return seat >= 0 && seat < roomSeats.Count ? roomSeats[seat] : -1;
        }
    }
}
