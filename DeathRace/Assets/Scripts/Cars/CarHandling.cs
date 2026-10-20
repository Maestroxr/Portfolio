using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// The arcade handling of one car as plain numbers, worked out once from its <see cref="CarStats"/>: how hard it
    /// pulls at a speed, how hard it brakes, how much sideways grip its tyres have and how fast the wheel can turn it.
    /// <see cref="CarBody"/> drives the car by these rules and the computer's drivers plan their speeds with the same
    /// ones, so a computer car never asks its car for a corner the car cannot take.
    ///
    /// The model: the engine pulls with a power curve that fades to nothing at the top speed; the brakes are strong; the
    /// tyres take out sideways speed up to <see cref="LateralGrip"/> (metres per second per second), a little less at
    /// speed and much less while braking into a turn (the tail steps out: a drift without a handbrake button); the
    /// wheel sets a yaw rate that needs speed, flips in reverse and tightens less the faster the car goes.
    /// </summary>
    public readonly struct CarHandling
    {
        /// <summary>The sideways grip of Grip 1 tyres, in metres per second per second (about 2 g: an arcade car that still has to brake for its bends).</summary>
        public const float BaseLateralGrip = 20f;

        /// <summary>How hard the brakes stop a car of Grip 1, metres per second per second.</summary>
        public const float BaseBrake = 30f;

        /// <summary>Rolling resistance and engine braking with no pedal pressed.</summary>
        public const float Coast = 2.4f;

        /// <summary>Air drag at speed when coasting, per (m/s) squared.</summary>
        public const float CoastDrag = 0.0009f;

        /// <summary>The share of the top speed a car reaches in reverse.</summary>
        public const float ReverseShare = 0.35f;

        /// <summary>The share of the acceleration a car has in reverse.</summary>
        public const float ReverseAcceleration = 0.6f;

        /// <summary>
        /// Nitro: the top speed and the pull it adds (+35 %, +60 % stock; the nitro system's <see cref="NitroPower"/>
        /// scales the extra).
        /// </summary>
        public const float NitroTopSpeed = 1.35f;
        public const float NitroAcceleration = 1.6f;

        /// <summary>The shape of the power curve: higher keeps the pull longer before it fades at the top speed.</summary>
        public const float PowerExponent = 2.4f;

        /// <summary>How quickly the car slows down to its top speed when it is over it (after the nitro), m/s^2 at most.</summary>
        public const float OverSpeedDrag = 6f;

        /// <summary>The yaw rate at full lock of a Steering 1 car once it rolls fast enough, radians per second.</summary>
        public const float YawBase = 2.3f;

        /// <summary>The speed from which the wheel has its full say (below it, it turns the car less: it needs speed).</summary>
        public const float YawFullSpeed = 7f;

        /// <summary>
        /// At speed, full lock turns the car with at most this sideways acceleration (times Steering): a little more than
        /// the tyres hold, so holding the key through a fast corner lets the tail slide out a little.
        /// </summary>
        public const float YawSpeedLimit = 29f;

        /// <summary>How fast the yaw follows the wheel at the reference mass (radians per second per second).</summary>
        public const float YawResponseBase = 16f;

        /// <summary>The mass the handling is tuned around; heavier cars respond a little slower and keep slides longer.</summary>
        public const float ReferenceMass = 1200f;

        /// <summary>How quickly a small sideways slide is taken out (per second) before the grip limit applies.</summary>
        public const float LateralDamping = 10f;

        /// <summary>The share of the sideways grip left while braking into a turn at full lock.</summary>
        public const float BrakeTurnGrip = 0.55f;

        /// <summary>The angle the nose may run ahead of the way the car moves, in radians: normally and while braking into a turn.</summary>
        public const float DriftAngle = 0.36f;
        public const float BrakeDriftAngle = 0.72f;

        public CarHandling(CarStats stats)
        {
            TopSpeed = Mathf.Max(1f, stats.TopSpeed);
            Acceleration = Mathf.Max(0.5f, stats.Acceleration);
            Grip = Mathf.Max(0.1f, stats.Grip);
            Steering = Mathf.Max(0.1f, stats.Steering);
            Mass = Mathf.Max(100f, stats.Mass);
            NitroPower = stats.NitroPower > 0f ? stats.NitroPower : 1f;
            float heft = ReferenceMass / Mass;
            LateralGrip = BaseLateralGrip * Grip * Mathf.Pow(heft, 0.15f);
            Brake = BaseBrake * (0.85f + 0.15f * Grip);
            YawResponse = YawResponseBase * Steering * Mathf.Sqrt(heft);
        }

        public float TopSpeed { get; }
        public float Acceleration { get; }
        public float Grip { get; }
        public float Steering { get; }
        public float Mass { get; }

        /// <summary>What the nitro system makes of a charge: its extra top speed and pull times this (1 = stock).</summary>
        public float NitroPower { get; }

        /// <summary>What a burning charge multiplies the top speed by (1.35 stock, more with the nitro system).</summary>
        public float NitroTopShare => 1f + (NitroTopSpeed - 1f) * NitroPower;

        /// <summary>What a burning charge multiplies the pull by (1.6 stock, more with the nitro system).</summary>
        public float NitroPullShare => 1f + (NitroAcceleration - 1f) * NitroPower;

        /// <summary>The sideways acceleration the tyres hold at low speed, m/s^2.</summary>
        public float LateralGrip { get; }

        /// <summary>Braking, m/s^2.</summary>
        public float Brake { get; }

        /// <summary>How fast the yaw rate follows the wheel, rad/s^2.</summary>
        public float YawResponse { get; }

        public float ReverseTopSpeed => TopSpeed * ReverseShare;

        /// <summary>The top speed now (with or without nitro).</summary>
        public float TopSpeedWith(bool nitro)
        {
            return nitro ? TopSpeed * NitroTopShare : TopSpeed;
        }

        /// <summary>
        /// The pull of the engine at full throttle at <paramref name="speed"/> (forward, m/s): all of the acceleration from a
        /// standstill, fading along the power curve to nothing at the top speed, and a gentle drag back down above it.
        /// <paramref name="topShare"/> is what is left of the top speed (an EMP's jam leaves a share).
        /// </summary>
        public float DriveAcceleration(float speed, bool nitro, float topShare = 1f)
        {
            float top = Mathf.Max(1f, TopSpeedWith(nitro) * Mathf.Clamp01(topShare <= 0f ? 1f : topShare));
            float acceleration = nitro ? Acceleration * NitroPullShare : Acceleration;
            float share = Mathf.Max(0f, speed) / top;
            if (share >= 1f)
            {
                return -Mathf.Min(OverSpeedDrag, (share - 1f) * top * 2f);
            }
            return acceleration * (1f - Mathf.Pow(share, PowerExponent));
        }

        /// <summary>The pull backwards at full reverse at <paramref name="backwardSpeed"/> (m/s, positive going backwards).</summary>
        public float ReverseAccelerationAt(float backwardSpeed)
        {
            float share = Mathf.Max(0f, backwardSpeed) / ReverseTopSpeed;
            return share >= 1f ? 0f : Acceleration * ReverseAcceleration * (1f - share * share);
        }

        /// <summary>What slows a car rolling at <paramref name="speed"/> with no pedal pressed, m/s^2 (positive).</summary>
        public static float CoastDeceleration(float speed)
        {
            return Coast + CoastDrag * speed * speed;
        }

        /// <summary>
        /// The yaw rate at full lock at <paramref name="speed"/> (m/s, either way), radians per second: nothing standing
        /// still, the full rate from <see cref="YawFullSpeed"/>, then less and less as the speed grows (speed sensitive
        /// steering), so full lock at speed asks for about <see cref="YawSpeedLimit"/> m/s^2 sideways.
        /// </summary>
        public float MaxYawRate(float speed)
        {
            float magnitude = Mathf.Abs(speed);
            float ramp = Mathf.Pow(Mathf.Clamp01(magnitude / YawFullSpeed), 0.8f);
            float low = YawBase * ramp;
            float high = YawSpeedLimit / Mathf.Max(0.5f, magnitude);
            return Steering * Mathf.Min(low, high);
        }

        /// <summary>
        /// The sideways grip at <paramref name="speed"/>, m/s^2: a little less at high speed, much less while braking into a
        /// turn (<paramref name="brakeTurn"/> 0 to 1: how hard the brake and the wheel are both pressed).
        /// </summary>
        public float GripAt(float speed, float brakeTurn)
        {
            float fade = 1f - 0.12f * Mathf.Clamp01((Mathf.Abs(speed) - 15f) / 30f);
            float drift = Mathf.Lerp(1f, BrakeTurnGrip, Mathf.Clamp01(brakeTurn));
            return LateralGrip * fade * drift;
        }

        /// <summary>The fastest a car can take a bend of <paramref name="curvature"/> (1/m) with a share of its grip, m/s.</summary>
        public float CornerSpeed(float curvature, float gripShare = 1f)
        {
            float k = Mathf.Abs(curvature);
            if (k < 1e-4f)
            {
                return TopSpeed;
            }
            // The grip fades a little at speed: two rounds of the fixed point settle it closely enough.
            float speed = Mathf.Sqrt(LateralGrip * gripShare / k);
            for (int i = 0; i < 2; i++)
            {
                speed = Mathf.Sqrt(GripAt(speed, 0f) * gripShare / k);
            }
            return Mathf.Min(TopSpeed, speed);
        }

        /// <summary>Seconds from a standstill to <paramref name="share"/> of the top speed at full throttle (for tests and tuning).</summary>
        public float TimeTo(float share, bool nitro = false, float step = 0.01f)
        {
            float target = TopSpeedWith(nitro) * Mathf.Clamp(share, 0f, 0.99f);
            float speed = 0f;
            float time = 0f;
            while (speed < target && time < 60f)
            {
                speed += DriveAcceleration(speed, nitro) * step;
                time += step;
            }
            return time;
        }
    }
}
