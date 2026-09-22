using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// What a ship shows of itself to the other pilots of a shared mission, next to where it is: the two numbers that
    /// travel with a pose of the base server. The state holds what changes in a moment (a change is sent at once), the
    /// value what changes slowly: the hull that was picked in the hangar, and how much hull and shield are left.
    /// </summary>
    public struct ShipPose
    {
        private const uint AliveBit = 1;
        private const uint ThrustingBit = 2;
        private const uint DashingBit = 4;
        private const uint InvulnerableBit = 8;
        private const uint MagnetBit = 16;
        private const uint DronesBit = 32;

        public bool Alive;
        public bool Thrusting;
        public bool Dashing;
        public bool Invulnerable;
        /// <summary>The tractor magnet is on: pickups come to this ship from far away.</summary>
        public bool Magnet;
        public bool Drones;

        /// <summary>Index of the ship in the hangar.</summary>
        public int Hull;

        /// <summary>Hull points left, 0 to 1.</summary>
        public float Health;

        /// <summary>Shield charge, 0 to 1.</summary>
        public float Shield;

        public uint PackState()
        {
            return (Alive ? AliveBit : 0u) | (Thrusting ? ThrustingBit : 0u) | (Dashing ? DashingBit : 0u) |
                   (Invulnerable ? InvulnerableBit : 0u) | (Magnet ? MagnetBit : 0u) | (Drones ? DronesBit : 0u);
        }

        public int PackValue()
        {
            return (Mathf.Clamp(Hull, 0, 15)) | (Percent(Health) << 8) | (Percent(Shield) << 16);
        }

        public static ShipPose Unpack(uint state, int value)
        {
            return new ShipPose
            {
                Alive = (state & AliveBit) != 0,
                Thrusting = (state & ThrustingBit) != 0,
                Dashing = (state & DashingBit) != 0,
                Invulnerable = (state & InvulnerableBit) != 0,
                Magnet = (state & MagnetBit) != 0,
                Drones = (state & DronesBit) != 0,
                Hull = value & 0xF,
                Health = ((value >> 8) & 0x7F) / 100f,
                Shield = ((value >> 16) & 0x7F) / 100f
            };
        }

        private static int Percent(float fraction)
        {
            return Mathf.Clamp(Mathf.RoundToInt(fraction * 100f), 0, 100);
        }
    }
}
