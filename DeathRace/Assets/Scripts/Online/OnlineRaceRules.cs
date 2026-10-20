using System;
using System.Collections.Generic;
using System.Globalization;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// What the devices of an online race and the server agree on, in one file that both compile (the server module
    /// includes it, Server/StdbModule.csproj): the kinds of the room's actions and how their payloads are written, the
    /// bits of a car's pose, the keys of the claims, the options of a room, how a racer's result becomes the score the
    /// base server ranks the room by, and what a result can plausibly be. It knows nothing of Unity.
    /// </summary>
    public static class OnlineRaceRules
    {
        // ------------------------------------------------------------------------------------------------
        // Actions of the room's log
        // ------------------------------------------------------------------------------------------------

        /// <summary>A car dropped a mine: every device lays it (payload <see cref="EncodeMine"/>).</summary>
        public const uint MineAction = 1;

        /// <summary>A car fired a rocket: every device launches it (payload <see cref="EncodeRocket"/>).</summary>
        public const uint RocketAction = 2;

        /// <summary>A car was wrecked, judged by its own device, which names the killer (payload <see cref="EncodeWreck"/>).</summary>
        public const uint WreckAction = 3;

        /// <summary>A car poured an oil slick: every device lays it (payload <see cref="EncodeOil"/>, the mine's layout).</summary>
        public const uint OilAction = 4;

        /// <summary>A car set off an EMP blast: every device shows it and jams the cars it drives in reach (payload <see cref="EncodeEmp"/>).</summary>
        public const uint EmpAction = 5;

        /// <summary>A car lobbed a mortar shell: every device flies it and judges its blast on the cars it drives (payload <see cref="EncodeMortar"/>).</summary>
        public const uint MortarAction = 6;

        /// <summary>The longest payload of a race's action.</summary>
        public const int MaxPayloadLength = 64;

        // ------------------------------------------------------------------------------------------------
        // The bits of a pose (RoomPose.State) and its value (the health)
        // ------------------------------------------------------------------------------------------------

        public const uint FiringBit = 1;
        public const uint NitroBit = 2;
        public const uint BrakingBit = 4;
        public const uint WreckedBit = 8;
        public const uint FinishedBit = 16;

        /// <summary>The state bits of a car: what the others show of it (the gun, the flames, the brake lights, the wreck).</summary>
        public static uint PackState(bool firing, bool nitro, bool braking, bool wrecked, bool finished)
        {
            return (firing ? FiringBit : 0) | (nitro ? NitroBit : 0) | (braking ? BrakingBit : 0) | (wrecked ? WreckedBit : 0) | (finished ? FinishedBit : 0);
        }

        public static bool Has(uint state, uint bit)
        {
            return (state & bit) != 0;
        }

        /// <summary>The health of a car (0 to 1) as the value of its pose: per mille.</summary>
        public static int PackHealth(float health01)
        {
            return (int)Math.Round(Math.Max(0f, Math.Min(1f, health01)) * 1000f);
        }

        public static float UnpackHealth(int value)
        {
            return Math.Max(0, Math.Min(1000, value)) / 1000f;
        }

        // ------------------------------------------------------------------------------------------------
        // Claims: the first device to claim a pickup or a mine gets it
        // ------------------------------------------------------------------------------------------------

        public const byte PickupClaim = 0;
        public const byte MineClaim = 1;

        /// <summary>The key of a claim: the room in the upper half, the kind and the item in the lower one.</summary>
        public static ulong ClaimKey(ulong roomId, byte kind, uint item)
        {
            return (roomId << 32) | ((ulong)(kind & 0xF) << 28) | (item & 0x0FFFFFFFu);
        }

        /// <summary>
        /// The item of a pickup's claim: the pickup (its index in the race's field) and how often it was taken before, so
        /// the pickup that comes back after a while is claimed afresh.
        /// </summary>
        public static uint PickupItem(int index, int generation)
        {
            return ((uint)(generation & 0xFFFF) << 12) | (uint)(index & 0xFFF);
        }

        public static int PickupIndexOf(uint item)
        {
            return (int)(item & 0xFFF);
        }

        public static int PickupGenerationOf(uint item)
        {
            return (int)((item >> 12) & 0xFFFF);
        }

        /// <summary>The item of a mine's claim: the seat of the car that dropped it and the number of the mine in its race.</summary>
        public static uint MineItem(int ownerSeat, int mineId)
        {
            return ((uint)(ownerSeat & 0xFF) << 16) | (uint)(mineId & 0xFFFF);
        }

        public static int MineOwnerOf(uint item)
        {
            return (int)((item >> 16) & 0xFF);
        }

        public static int MineIdOf(uint item)
        {
            return (int)(item & 0xFFFF);
        }

        // ------------------------------------------------------------------------------------------------
        // The options of a room
        // ------------------------------------------------------------------------------------------------

        /// <summary>The laps (0 = the track's own).</summary>
        public const string LapsOption = "laps";

        /// <summary>The car everybody drives (a CarModel).</summary>
        public const string CarOption = "car";

        /// <summary>The track's laps as the host's device wrote them (the server knows no tracks).</summary>
        public const string TrackLapsOption = "tracklaps";

        /// <summary>The track's length in metres as the host's device wrote it.</summary>
        public const string LengthOption = "length";

        public const int MaxTracks = 16;
        public const int MinLaps = 1;
        public const int MaxLaps = 9;
        public const int CarCount = 12;
        public const int MinLength = 100;
        public const int MaxLength = 5000;

        /// <summary>The laps a room races: its own choice, else the track's.</summary>
        public static int LapsOf(int chosen, int trackLaps)
        {
            int laps = chosen > 0 ? chosen : trackLaps;
            return Math.Max(MinLaps, Math.Min(MaxLaps, laps));
        }

        // ------------------------------------------------------------------------------------------------
        // Results and scores
        // ------------------------------------------------------------------------------------------------

        /// <summary>The width of each band of scores: the finishers', the running racers', the wrecked racers'.</summary>
        public const long Band = 2_000_000_000_000L;

        /// <summary>Scores of the racers who finished: this less their time in milliseconds (the faster, the higher).</summary>
        public const long FinishedBase = 3 * Band;

        /// <summary>Scores of the racers still running when the race ended: this plus their progress in centimetres.</summary>
        public const long RunningBase = Band;

        /// <summary>The highest score a racer can report while the race runs: the top of the running band.</summary>
        public const long MostRunningScore = 2 * Band - 1;

        /// <summary>Faster than any car can go, with nitro, upgrades and a tail wind (m/s): what a lap time is held to.</summary>
        public const float FastestSpeed = 100f;

        /// <summary>
        /// The score of a racer's result, which the base server ranks a room by (the higher first): the finishers by their
        /// time, then those still running by how far they got, then the wrecked by how far they got - the order of the
        /// places of an offline race (Gamebox.Racing.RaceRanking).
        /// </summary>
        public static long Score(bool finished, bool wrecked, long timeMs, long progressCm)
        {
            if (finished)
            {
                return FinishedBase - Math.Max(0L, Math.Min(timeMs, Band - 1));
            }
            long progress = Math.Max(0L, Math.Min(progressCm, Band - 1));
            return wrecked ? progress : RunningBase + progress;
        }

        public static bool IsFinishedScore(long score)
        {
            return score > FinishedBase - Band;
        }

        /// <summary>The time of a finisher's score in milliseconds (0 for a score that did not finish).</summary>
        public static long TimeOfScore(long score)
        {
            return IsFinishedScore(score) ? FinishedBase - score : 0L;
        }

        /// <summary>The fastest a race of <paramref name="laps"/> on a track of <paramref name="length"/> metres can be done, in milliseconds.</summary>
        public static long FastestTimeMs(int laps, int length)
        {
            return (long)(Math.Max(1, laps) * (double)Math.Max(0, length) / FastestSpeed * 1000.0);
        }

        /// <summary>The most a racer can have covered of such a race, in centimetres (a lap more, for the grid and the slack of a measure).</summary>
        public static long MostProgressCm(int laps, int length)
        {
            return (Math.Max(1, laps) + 1L) * Math.Max(0, length) * 100L;
        }

        // ------------------------------------------------------------------------------------------------
        // Payloads
        // ------------------------------------------------------------------------------------------------

        /// <summary>A mine as its action carries it: its number in the dropper's race and where it lies.</summary>
        public struct MineDrop
        {
            public int Id;
            public float X;
            public float Z;
            public float Yaw;
        }

        /// <summary>A rocket as its action carries it: where it left, which way, how fast.</summary>
        public struct RocketShot
        {
            public float X;
            public float Z;
            public float Y;
            public float DirectionX;
            public float DirectionZ;
            public float Speed;
        }

        /// <summary>An EMP blast as its action carries it: where it went off.</summary>
        public struct EmpPulse
        {
            public float X;
            public float Z;
        }

        /// <summary>A mortar shell as its action carries it: where it left and where it lands.</summary>
        public struct MortarLob
        {
            public float X;
            public float Y;
            public float Z;
            public float TargetX;
            public float TargetZ;
        }

        /// <summary>"id,x,z,yaw" with the place in centimetres and the yaw in tenths of a degree.</summary>
        public static string EncodeMine(MineDrop mine)
        {
            return Join(mine.Id, Centi(mine.X), Centi(mine.Z), Deci(mine.Yaw));
        }

        public static bool TryDecodeMine(string payload, out MineDrop mine)
        {
            mine = default;
            if (!TrySplit(payload, 4, out int[] values))
            {
                return false;
            }
            mine = new MineDrop { Id = values[0], X = values[1] / 100f, Z = values[2] / 100f, Yaw = values[3] / 10f };
            return true;
        }

        /// <summary>"x,y,z,dx,dz,speed": centimetres, the direction in thousandths, the speed in centimetres a second.</summary>
        public static string EncodeRocket(RocketShot rocket)
        {
            return Join(Centi(rocket.X), Centi(rocket.Y), Centi(rocket.Z), Milli(rocket.DirectionX), Milli(rocket.DirectionZ), Centi(rocket.Speed));
        }

        public static bool TryDecodeRocket(string payload, out RocketShot rocket)
        {
            rocket = default;
            if (!TrySplit(payload, 6, out int[] values))
            {
                return false;
            }
            float dx = values[3] / 1000f;
            float dz = values[4] / 1000f;
            double length = Math.Sqrt(dx * dx + dz * dz);
            if (length < 0.001)
            {
                return false;
            }
            rocket = new RocketShot
            {
                X = values[0] / 100f, Y = values[1] / 100f, Z = values[2] / 100f,
                DirectionX = (float)(dx / length), DirectionZ = (float)(dz / length), Speed = Math.Max(0, values[5]) / 100f
            };
            return true;
        }

        /// <summary>An oil slick is written as a mine is: "id,x,z,yaw" (its number in the car's race, where it lies).</summary>
        public static string EncodeOil(MineDrop slick)
        {
            return EncodeMine(slick);
        }

        public static bool TryDecodeOil(string payload, out MineDrop slick)
        {
            return TryDecodeMine(payload, out slick);
        }

        /// <summary>"x,z" in centimetres.</summary>
        public static string EncodeEmp(EmpPulse pulse)
        {
            return Join(Centi(pulse.X), Centi(pulse.Z));
        }

        public static bool TryDecodeEmp(string payload, out EmpPulse pulse)
        {
            pulse = default;
            if (!TrySplit(payload, 2, out int[] values))
            {
                return false;
            }
            pulse = new EmpPulse { X = values[0] / 100f, Z = values[1] / 100f };
            return true;
        }

        /// <summary>The farthest a shell can land from where it left, in metres (what a payload is held to).</summary>
        public const float MortarReach = 120f;

        /// <summary>"x,y,z,tx,tz": where the shell left and where it lands, in centimetres.</summary>
        public static string EncodeMortar(MortarLob lob)
        {
            return Join(Centi(lob.X), Centi(lob.Y), Centi(lob.Z), Centi(lob.TargetX), Centi(lob.TargetZ));
        }

        public static bool TryDecodeMortar(string payload, out MortarLob lob)
        {
            lob = default;
            if (!TrySplit(payload, 5, out int[] values))
            {
                return false;
            }
            var decoded = new MortarLob
            {
                X = values[0] / 100f, Y = values[1] / 100f, Z = values[2] / 100f, TargetX = values[3] / 100f, TargetZ = values[4] / 100f
            };
            float dx = decoded.TargetX - decoded.X;
            float dz = decoded.TargetZ - decoded.Z;
            if (dx * dx + dz * dz > MortarReach * MortarReach)
            {
                return false;
            }
            lob = decoded;
            return true;
        }

        /// <summary>The seat of the killer, or "-" for a car that wrecked itself (a barrier, its own mine with nobody to blame).</summary>
        public static string EncodeWreck(int killerSeat)
        {
            return killerSeat >= 0 && killerSeat < 255 ? killerSeat.ToString(CultureInfo.InvariantCulture) : "-";
        }

        /// <summary>The killer's seat of a wreck's payload, or -1 for none.</summary>
        public static bool TryDecodeWreck(string payload, out int killerSeat)
        {
            killerSeat = -1;
            if (payload == "-")
            {
                return true;
            }
            if (int.TryParse(payload, NumberStyles.Integer, CultureInfo.InvariantCulture, out int seat) && seat >= 0 && seat < 255)
            {
                killerSeat = seat;
                return true;
            }
            return false;
        }

        private static int Centi(float value)
        {
            return (int)Math.Round(Clamp(value, -200000f, 200000f) * 100f);
        }

        private static int Deci(float value)
        {
            float degrees = value % 360f;
            if (degrees < 0f)
            {
                degrees += 360f;
            }
            return (int)Math.Round(degrees * 10f) % 3600;
        }

        private static int Milli(float value)
        {
            return (int)Math.Round(Clamp(value, -1f, 1f) * 1000f);
        }

        private static float Clamp(float value, float min, float max)
        {
            return float.IsNaN(value) ? 0f : Math.Max(min, Math.Min(max, value));
        }

        private static string Join(params int[] values)
        {
            var parts = new List<string>(values.Length);
            foreach (int value in values)
            {
                parts.Add(value.ToString(CultureInfo.InvariantCulture));
            }
            return string.Join(",", parts);
        }

        private static bool TrySplit(string payload, int count, out int[] values)
        {
            values = Array.Empty<int>();
            if (string.IsNullOrEmpty(payload) || payload.Length > MaxPayloadLength)
            {
                return false;
            }
            string[] parts = payload.Split(',');
            if (parts.Length != count)
            {
                return false;
            }
            var numbers = new int[count];
            for (int i = 0; i < count; i++)
            {
                if (!int.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out numbers[i]))
                {
                    return false;
                }
            }
            values = numbers;
            return true;
        }
    }
}
