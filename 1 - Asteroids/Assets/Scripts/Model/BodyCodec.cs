using System;
using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>What a body of a shared playfield is, as the simulator announces it. The numbers travel: do not reorder.</summary>
    public enum BodyKind : byte
    {
        Asteroid = 0,
        Mine = 1,
        ClusterBomb = 2,
        SupplyPod = 3,
        Saucer = 4,
        Scout = 5,
        Wasp = 6,
        Comet = 7,
        GravityWell = 8,
        EnemyShot = 9,
        Reward = 10,
        Boss = 11,
        /// <summary>A strike aircraft; variant = (int)StrikeUnit.</summary>
        StrikeAir = 12,
        /// <summary>A strike ground unit; variant = (int)StrikeUnit.</summary>
        StrikeGround = 13,
        /// <summary>A part of a strike boss; variant = (boss net id &lt;&lt; 8) | part index. Bound to the boss's part, never spawned.</summary>
        BossPart = 14
    }


    /// <summary>Why a body left the playfield, which decides what the other clients play.</summary>
    public enum ExitReason : byte
    {
        /// <summary>Its time ran out, it flew off, or the field was cleared: it just goes.</summary>
        Expired = 0,
        /// <summary>Destroyed: it breaks or explodes, and scores for the seat that did it.</summary>
        Destroyed = 1,
        /// <summary>A pickup a pilot collected.</summary>
        Collected = 2,
        /// <summary>A shot that stopped against a ship or a nova.</summary>
        Impact = 3
    }


    /// <summary>The little state of a body that shows: travels with its position.</summary>
    [Flags]
    public enum BodyFlags : uint
    {
        None = 0,
        /// <summary>A mine that was tripped.</summary>
        Armed = 1,
        /// <summary>Cannot be hurt right now (a boss behind its shield).</summary>
        Shielded = 2,
        /// <summary>A boss making its entrance.</summary>
        Entering = 4,
        /// <summary>A boss going down.</summary>
        Dying = 8,
        /// <summary>The body warped in: the clients play the entrance.</summary>
        WarpIn = 16,
        /// <summary>A strike unit warns of its next attack (a laser tower's glow, an interceptor at the edge it comes in by).</summary>
        Warning = 32
    }


    /// <summary>What a pilot does beyond flying, sent to the others as it happens.</summary>
    public enum ShipSignalKind : byte
    {
        /// <summary>A nova bomb at x, y with its damage (a) and its damage to bosses (b).</summary>
        Nova = 0,
        /// <summary>A new ship at x, y pushes what is around it away.</summary>
        MakeRoom = 1
    }


    /// <summary>The moments of the simulator's playfield that travel as signals.</summary>
    public enum FieldSignalKind : byte
    {
        Blast = 0,
        CometWarning = 1,
        /// <summary>The scroll of a strike mission: X = the distance, Y = the speed.</summary>
        Scroll = 2
    }


    /// <summary>How the mission of a room stands. Mirrors the outcome numbers of the server.</summary>
    public enum MissionOutcome : byte
    {
        Running = 0,
        Victory = 1,
        Failed = 2,
        /// <summary>The pilot who simulated the world left.</summary>
        Abandoned = 3
    }


    /// <summary>Packs what the bodies of a shared playfield need to say about themselves into the numbers of a row.</summary>
    public static class BodyCodec
    {
        /// <summary>The kind of shot that is a wing drone's bolt, next to the <see cref="WeaponType"/> numbers.</summary>
        public const byte DroneShot = 100;

        /// <summary>The most spawns, moves, exits, signals, shots or hits the server takes in one call.</summary>
        public const int MaxBatch = 512;

        private const int PhaseShift = 8;
        private const uint PhaseMask = 3u << PhaseShift;
        private const int PartShift = 8;
        private const int PartMask = 0xFF;

        public static int PackAsteroid(AsteroidKind kind, AsteroidSize size, int shape)
        {
            return ((int)kind & 0xF) | (((int)size & 0xF) << 4) | ((Mathf.Max(0, shape) & 0xFF) << 8);
        }

        public static void UnpackAsteroid(int variant, out AsteroidKind kind, out AsteroidSize size, out int shape)
        {
            kind = (AsteroidKind)(variant & 0xF);
            size = (AsteroidSize)((variant >> 4) & 0xF);
            shape = (variant >> 8) & 0xFF;
        }

        /// <summary>
        /// The variant of a strike boss part: the net id of its boss above the lowest 8 bits, its index among the boss's
        /// parts in them. Net ids count from 1 per mission, so they fit the 23 bits left.
        /// </summary>
        public static int PackPart(uint bossNetId, int index)
        {
            return (int)(((bossNetId & 0x7FFFFFu) << PartShift) | (uint)(index & PartMask));
        }

        public static void UnpackPart(int variant, out uint bossNetId, out int index)
        {
            bossNetId = (uint)variant >> PartShift;
            index = variant & PartMask;
        }

        /// <summary>How many calls of at most <see cref="MaxBatch"/> items <paramref name="count"/> items need (none for none).</summary>
        public static int BatchCount(int count)
        {
            return count <= 0 ? 0 : (count + MaxBatch - 1) / MaxBatch;
        }

        /// <summary>Call <paramref name="index"/>'s share of <paramref name="items"/>: a new list, empty past the end.</summary>
        public static List<T> Batch<T>(List<T> items, int index)
        {
            int start = index * MaxBatch;
            if (items == null || index < 0 || start >= items.Count)
            {
                return new List<T>();
            }
            return items.GetRange(start, Math.Min(MaxBatch, items.Count - start));
        }

        /// <summary>The flags with the phase of a boss (0 to 2) in them.</summary>
        public static BodyFlags WithPhase(BodyFlags flags, int phase)
        {
            return (BodyFlags)(((uint)flags & ~PhaseMask) | ((uint)Mathf.Clamp(phase, 0, 3) << PhaseShift));
        }

        public static int Phase(BodyFlags flags)
        {
            return (int)(((uint)flags & PhaseMask) >> PhaseShift);
        }

        public static bool Has(BodyFlags flags, BodyFlags flag)
        {
            return (flags & flag) != 0;
        }

        /// <summary>A colour as 0xRRGGBB. Tints brighter than white are clamped: they only colour an effect.</summary>
        public static uint PackColor(Color color)
        {
            uint r = (uint)Mathf.RoundToInt(Mathf.Clamp01(color.r) * 255f);
            uint g = (uint)Mathf.RoundToInt(Mathf.Clamp01(color.g) * 255f);
            uint b = (uint)Mathf.RoundToInt(Mathf.Clamp01(color.b) * 255f);
            return (r << 16) | (g << 8) | b;
        }

        public static Color UnpackColor(uint packed)
        {
            return new Color(((packed >> 16) & 0xFF) / 255f, ((packed >> 8) & 0xFF) / 255f, (packed & 0xFF) / 255f, 1f);
        }
    }
}
