using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// What a ship shows of itself to the other pilots of a shared mission, next to where it is: the two numbers that
    /// travel with a pose of the base server. The state holds what changes in a moment (a change is sent at once), the
    /// value what changes slowly: the hull that was picked in the hangar, and how much hull and shield are left.
    /// The upper bits of the state carry what another device needs to take the seat over mid-mission (a remote control
    /// turning into a player on their own device, or back): the ships left, the bombs, the weapon and its level.
    /// </summary>
    public struct ShipPose
    {
        private const uint AliveBit = 1;
        private const uint ThrustingBit = 2;
        private const uint DashingBit = 4;
        private const uint InvulnerableBit = 8;
        private const uint MagnetBit = 16;
        private const uint DronesBit = 32;
        private const uint BeamBit = 64;
        private const int BeamKindShift = 24;
        private const int LivesShift = 8;
        private const int BombsShift = 12;
        private const int WeaponShift = 16;
        private const int WeaponLevelShift = 20;

        public bool Alive;
        public bool Thrusting;
        public bool Dashing;
        public bool Invulnerable;
        /// <summary>The tractor magnet is on: pickups come to this ship from far away.</summary>
        public bool Magnet;
        public bool Drones;

        /// <summary>Strike: a beam weapon is held (the on and off pulse is drawn by each client).</summary>
        public bool Beam;

        /// <summary>Strike: which beam weapon is held, 0 to 15 (value bits 24 to 27).</summary>
        public int BeamKind;

        /// <summary>Index of the ship in the hangar.</summary>
        public int Hull;

        /// <summary>Hull points left, 0 to 1.</summary>
        public float Health;

        /// <summary>Shield charge, 0 to 1.</summary>
        public float Shield;

        /// <summary>Ships left, the one flying included, 0 to 15 (state bits 8 to 11).</summary>
        public int Lives;

        /// <summary>Nova bombs (megabombs in strike), 0 to 7 (state bits 12 to 14).</summary>
        public int Bombs;

        /// <summary>The field weapon (<see cref="WeaponType"/> as a number), 0 to 15 (state bits 16 to 19).</summary>
        public int Weapon;

        /// <summary>The level of the field weapon, 0 to 7 (state bits 20 to 22).</summary>
        public int WeaponLevel;

        public uint PackState()
        {
            return (Alive ? AliveBit : 0u) | (Thrusting ? ThrustingBit : 0u) | (Dashing ? DashingBit : 0u) |
                   (Invulnerable ? InvulnerableBit : 0u) | (Magnet ? MagnetBit : 0u) | (Drones ? DronesBit : 0u) | (Beam ? BeamBit : 0u) |
                   ((uint)Mathf.Clamp(Lives, 0, 15) << LivesShift) | ((uint)Mathf.Clamp(Bombs, 0, 7) << BombsShift) |
                   ((uint)Mathf.Clamp(Weapon, 0, 15) << WeaponShift) | ((uint)Mathf.Clamp(WeaponLevel, 0, 7) << WeaponLevelShift);
        }

        public int PackValue()
        {
            return (Mathf.Clamp(Hull, 0, 15)) | (Percent(Health) << 8) | (Percent(Shield) << 16) | (Mathf.Clamp(BeamKind, 0, 15) << BeamKindShift);
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
                Beam = (state & BeamBit) != 0,
                Lives = (int)((state >> LivesShift) & 0xF),
                Bombs = (int)((state >> BombsShift) & 0x7),
                Weapon = (int)((state >> WeaponShift) & 0xF),
                WeaponLevel = (int)((state >> WeaponLevelShift) & 0x7),
                BeamKind = (value >> BeamKindShift) & 0xF,
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
