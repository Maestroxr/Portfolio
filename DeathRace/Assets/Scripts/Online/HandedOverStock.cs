using Gamebox.Online;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// What a car carries when this device takes it over mid-race: a seat handed from the room's shared screen to the
    /// player's own device, or back (<see cref="DeathRaceOnlineController"/>). Its real stock was counted by the device
    /// that drove it until now. A player's device takes it from the status the screen published for the seat last
    /// (<see cref="FromStatus"/>: the screen tells every remote its whole stock a few times a second). Without one, and
    /// on the screen when a car comes back to it, the stock is reckoned (<see cref="Reckon"/>) from the start loadout
    /// and what the room saw since the start (<see cref="Usage"/>): the mines, rockets and special weapons in the room's
    /// action log, the ammo and nitro pickups claimed for the seat, and the rounds and nitro charges its puppet spent
    /// here, which stand for the real ones (the puppet's gun fires as long as the pose's bit says, and lights a charge
    /// whenever the bit does). Plain arithmetic, for the tests.
    /// </summary>
    public static class HandedOverStock
    {
        /// <summary>What a seat used and picked up since the race started, as this device saw it.</summary>
        public struct Usage
        {
            /// <summary>Mines dropped (actions of the room).</summary>
            public int Mines;

            /// <summary>Rockets fired (actions of the room).</summary>
            public int Rockets;

            /// <summary>Oil slicks poured (actions of the room).</summary>
            public int Oil;

            /// <summary>EMP blasts set off (actions of the room).</summary>
            public int Emp;

            /// <summary>Mortar shells lobbed (actions of the room).</summary>
            public int Mortar;

            /// <summary>Rounds the car's gun fired here (the puppet's as long as the pose said so, the real ones while driven here).</summary>
            public int RoundsFired;

            /// <summary>Nitro charges lit here.</summary>
            public int NitroBurned;

            /// <summary>Rounds the ammo pickups claimed for the seat gave.</summary>
            public int AmmoPicked;

            /// <summary>Charges the nitro pickups claimed for the seat gave.</summary>
            public int NitroPicked;
        }

        /// <summary>The stock a taken-over car gets (<see cref="CarWeapons.Restock"/>).</summary>
        public struct Stock
        {
            public int Ammo;
            public int Mines;
            public int Rockets;
            public int Nitro;
            public int Oil;
            public int Emp;
            public int Mortar;
        }

        /// <summary>
        /// The stock as the shared screen last published it for the seat (<see cref="RemoteRacePanel.Compose"/>, at most a
        /// quarter second behind the car): what a player's device starts with when it takes its own car over. False
        /// without a status, or one without the stock (then the stock is reckoned, <see cref="Reckon"/>).
        /// </summary>
        public static bool FromStatus(string status, out Stock stock)
        {
            stock = default;
            if (string.IsNullOrEmpty(status) || RoomOptions.Value(status, RemoteRacePanel.AmmoKey) == null
                || RoomOptions.Value(status, RemoteRacePanel.MinesKey) == null || RoomOptions.Value(status, RemoteRacePanel.RocketsKey) == null
                || RoomOptions.Value(status, RemoteRacePanel.NitroKey) == null)
            {
                return false;
            }
            stock = new Stock
            {
                Ammo = Mathf.Max(0, RoomOptions.Number(status, RemoteRacePanel.AmmoKey, 0)),
                Mines = Mathf.Max(0, RoomOptions.Number(status, RemoteRacePanel.MinesKey, 0)),
                Rockets = Mathf.Max(0, RoomOptions.Number(status, RemoteRacePanel.RocketsKey, 0)),
                Nitro = Mathf.Max(0, RoomOptions.Number(status, RemoteRacePanel.NitroKey, 0)),
                Oil = Mathf.Max(0, RoomOptions.Number(status, RemoteRacePanel.OilKey, 0)),
                Emp = Mathf.Max(0, RoomOptions.Number(status, RemoteRacePanel.EmpKey, 0)),
                Mortar = Mathf.Max(0, RoomOptions.Number(status, RemoteRacePanel.MortarKey, 0))
            };
            return true;
        }

        /// <summary>
        /// The stock of a car that started with <paramref name="loadout"/> and a gun of <paramref name="ammoCapacity"/>
        /// rounds after <paramref name="usage"/>: nothing below zero, the rounds up to the capacity. The fallback of
        /// <see cref="FromStatus"/>, and what the shared screen uses when a car comes back to it.
        /// </summary>
        public static Stock Reckon(CarLoadout loadout, int ammoCapacity, Usage usage)
        {
            int capacity = Mathf.Max(0, ammoCapacity);
            return new Stock
            {
                Ammo = Mathf.Clamp(capacity + usage.AmmoPicked - usage.RoundsFired, 0, capacity),
                Mines = Mathf.Max(0, (loadout?.mines ?? 0) - usage.Mines),
                Rockets = Mathf.Max(0, (loadout?.rockets ?? 0) - usage.Rockets),
                Nitro = Mathf.Max(0, (loadout?.nitro ?? 0) + usage.NitroPicked - usage.NitroBurned),
                Oil = Mathf.Max(0, (loadout?.oil ?? 0) - usage.Oil),
                Emp = Mathf.Max(0, (loadout?.emp ?? 0) - usage.Emp),
                Mortar = Mathf.Max(0, (loadout?.mortar ?? 0) - usage.Mortar)
            };
        }

        /// <summary>Counts an action of the room's log of <paramref name="kind"/> (<see cref="OnlineRaceRules"/>) in <paramref name="usage"/>.</summary>
        public static void Count(ref Usage usage, uint kind)
        {
            switch (kind)
            {
                case OnlineRaceRules.MineAction:
                    usage.Mines++;
                    break;
                case OnlineRaceRules.RocketAction:
                    usage.Rockets++;
                    break;
                case OnlineRaceRules.OilAction:
                    usage.Oil++;
                    break;
                case OnlineRaceRules.EmpAction:
                    usage.Emp++;
                    break;
                case OnlineRaceRules.MortarAction:
                    usage.Mortar++;
                    break;
            }
        }
    }
}
