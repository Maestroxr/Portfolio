using System;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// The physics of a car: an arcade model on a Rigidbody that lives in the ground plane (no gravity, the height and
    /// the roll and pitch frozen), driven by the rules of <see cref="CarHandling"/>. Every physics step it reads the
    /// car's velocity as forward and sideways speed, lets the pedals change the first and the tyres take out the second
    /// (up to their grip, so a car that turns harder than its tyres hold slides), and sets the yaw rate the wheel asks
    /// for, so the physics engine is left with what it does best: collisions. A hit therefore pushes a car by mass and can
    /// spin it, and the car works its way back to where the wheel points at a rate it can be caught from. The collider
    /// is slippery, so a car that meets a barrier at an angle slides along it instead of sticking.
    /// </summary>
    public class CarBody : MonoBehaviour
    {
        /// <summary>The collider stands this far off the road, so it never touches a ground or road collider.</summary>
        public const float ColliderLift = 0.15f;

        /// <summary>How hard a wrecked car slows down, m/s^2.</summary>
        public const float WreckedDrag = 9f;

        /// <summary>Extra drag on the shoulder off the road, m/s^2 (above <see cref="OffRoadFreeSpeed"/>).</summary>
        public const float OffRoadDrag = 5f;
        public const float OffRoadFreeSpeed = 9f;

        /// <summary>The radius of the wheels when the model does not say, metres (for the wheels' roll).</summary>
        public const float WheelRadius = 0.36f;

        /// <summary>How far the front wheels turn at full lock standing still and at the top speed, degrees.</summary>
        public const float WheelLockLow = 32f;
        public const float WheelLockHigh = 10f;

        private static PhysicsMaterial slideMaterial;

        private float steerShown;
        private float wheelRadius = WheelRadius;

        public Rigidbody Rigidbody { get; private set; }
        public BoxCollider Collider { get; private set; }
        public Car Car { get; private set; }

        /// <summary>The handling rules of this car (from its stats).</summary>
        public CarHandling Handling { get; private set; }

        /// <summary>Speed along the car's forward axis, metres per second.</summary>
        public float ForwardSpeed { get; protected set; }

        /// <summary>Speed across the car (positive to its right), metres per second.</summary>
        public float LateralSpeed { get; protected set; }

        /// <summary>How much the car slides sideways, 0 to 1 (skid marks, tyre smoke, the skid sound).</summary>
        public float Slip { get; protected set; }

        /// <summary>Set by the weapons while a nitro charge burns.</summary>
        public bool Nitro { get; set; }

        /// <summary>Whether the brake is pressed while moving forward (brake lights).</summary>
        public bool Braking { get; protected set; }

        /// <summary>On the grid before the green light: the car stays where it is (the engine may still be revved).</summary>
        public bool Held { get; set; }

        /// <summary>Off the road on the shoulder: more drag, less grip (set by the car from the track every step).</summary>
        public bool OffRoad { get; set; }

        /// <summary>The share of the tyres' grip left (1: all; oil leaves little), set by the car's <see cref="CarStatus"/> every step.</summary>
        public float GripFactor { get; set; } = 1f;

        /// <summary>The share of the wheel's turn left (1: all; oil leaves half).</summary>
        public float SteerFactor { get; set; } = 1f;

        /// <summary>The share of the top speed left (1: all; an EMP's jam leaves a share).</summary>
        public float TopSpeedFactor { get; set; } = 1f;

        public bool Wrecked { get; private set; }

        /// <summary>The front wheels' angle to show, degrees (positive to the right).</summary>
        public float SteerAngle { get; private set; }

        /// <summary>How far the wheels have rolled, degrees.</summary>
        public float WheelSpin { get; private set; }

        /// <summary>The throttle of the last step, -1 to 1 (the engine's sound).</summary>
        public float Throttle { get; private set; }

        /// <summary>The yaw rate, radians per second (positive turns right).</summary>
        public float YawRate => Rigidbody != null && !Rigidbody.isKinematic ? Rigidbody.angularVelocity.y : 0f;

        public Vector3 Velocity => Rigidbody != null && !Rigidbody.isKinematic ? Rigidbody.linearVelocity : Vector3.zero;

        /// <summary>The ground speed, metres per second.</summary>
        public float Speed => new Vector2(ForwardSpeed, LateralSpeed).magnitude;

        /// <summary>A collision: the other collider, the contact point and normal, the impact speed along the normal (m/s).</summary>
        public event Action<Collision, float> Collided;

        /// <summary>The slippery material of every car's collider.</summary>
        public static PhysicsMaterial SlideMaterial
        {
            get
            {
                if (slideMaterial == null)
                {
                    slideMaterial = new PhysicsMaterial("Car")
                    {
                        dynamicFriction = 0.04f,
                        staticFriction = 0.04f,
                        bounciness = 0.12f,
                        frictionCombine = PhysicsMaterialCombine.Minimum,
                        bounceCombine = PhysicsMaterialCombine.Maximum
                    };
                }
                return slideMaterial;
            }
        }

        /// <summary>Sets up the Rigidbody and the collider of the car (the factory added them) for its stats and size.</summary>
        public virtual void Setup(Car car, CarStats stats)
        {
            Car = car;
            Handling = new CarHandling(stats);
            Rigidbody = GetComponent<Rigidbody>();
            if (Rigidbody == null)
            {
                Rigidbody = gameObject.AddComponent<Rigidbody>();
            }
            Collider = GetComponent<BoxCollider>();
            if (Collider == null)
            {
                Collider = gameObject.AddComponent<BoxCollider>();
            }
            Vector3 size = car != null && car.Loadout != null ? car.Loadout.Spec.Size : new Vector3(2f, 1.4f, 4.2f);
            Collider.size = size;
            Collider.center = new Vector3(0f, size.y * 0.5f + ColliderLift, 0f);
            Collider.sharedMaterial = SlideMaterial;

            Rigidbody.mass = Handling.Mass;
            Rigidbody.useGravity = false;
            Rigidbody.linearDamping = 0f;
            Rigidbody.angularDamping = 0f;
            Rigidbody.isKinematic = false;
            Rigidbody.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            Rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            Rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            Rigidbody.maxAngularVelocity = 14f;
            Rigidbody.centerOfMass = new Vector3(0f, Collider.center.y, 0f);
            Wrecked = false;
            wheelRadius = car != null && car.View != null && car.View.frontWheelRadius > 0.05f ? car.View.frontWheelRadius : WheelRadius;
        }

        /// <summary>One physics step with the driver's input.</summary>
        public virtual void Step(CarInput input, float deltaTime)
        {
            if (Rigidbody == null || deltaTime <= 0f)
            {
                return;
            }
            if (Rigidbody.isKinematic)
            {
                ForwardSpeed = LateralSpeed = Slip = 0f;
                Braking = false;
                return;
            }
            float throttle = Wrecked ? 0f : Mathf.Clamp(input.Throttle, -1f, 1f);
            float steer = Wrecked ? 0f : Mathf.Clamp(input.Steer, -1f, 1f);
            Throttle = throttle;
            if (Held)
            {
                HoldStill(throttle, steer, deltaTime);
                return;
            }

            // The physics pose, not the transform: with interpolation the transform shows a pose between two steps.
            Vector3 forward = Flat(Rigidbody.rotation * Vector3.forward);
            var right = new Vector3(forward.z, 0f, -forward.x);
            Vector3 velocity = Rigidbody.linearVelocity;
            float along = Vector3.Dot(velocity, forward);
            float across = Vector3.Dot(velocity, right);

            // The pedals: the accelerator drives (or first brakes a car rolling back), the brake brakes and then reverses.
            float acceleration;
            bool braking = false;
            bool reversing = false;
            if (throttle > 0.01f)
            {
                acceleration = along < -0.5f ? Handling.Brake * throttle : Handling.DriveAcceleration(along, Nitro, TopSpeedFactor) * throttle;
            }
            else if (throttle < -0.01f)
            {
                if (along > 0.5f)
                {
                    acceleration = -Handling.Brake * -throttle;
                    braking = true;
                }
                else
                {
                    acceleration = -Handling.ReverseAccelerationAt(-along) * -throttle;
                    reversing = true;
                }
            }
            else
            {
                acceleration = -Mathf.Sign(along) * Mathf.Min(Mathf.Abs(along) / deltaTime, CarHandling.CoastDeceleration(along));
            }
            if (TopSpeedFactor < 0.999f && throttle <= 0.01f && along > Handling.TopSpeed * TopSpeedFactor)
            {
                // A jammed engine holds nothing back off the throttle either: the car sinks to what it has left.
                acceleration = Mathf.Min(acceleration, -CarHandling.OverSpeedDrag);
            }
            if (Wrecked)
            {
                acceleration = -Mathf.Sign(along) * Mathf.Min(Mathf.Abs(along) / deltaTime, WreckedDrag);
            }
            if (OffRoad && Mathf.Abs(along) > OffRoadFreeSpeed)
            {
                acceleration -= Mathf.Sign(along) * OffRoadDrag;
            }

            // Braking into a turn: the tail lets go (less grip, more yaw, a little less braking): a drift.
            float brakeTurn = braking ? Mathf.Clamp01(-throttle) * Mathf.Abs(steer) * Mathf.Clamp01((along - 8f) / 10f) : 0f;
            if (braking)
            {
                acceleration *= Mathf.Lerp(1f, 0.7f, brakeTurn);
            }

            float newAlong = along + acceleration * deltaTime;
            if (braking && newAlong < 0f)
            {
                newAlong = 0f;
            }
            else if (throttle > 0f && along < -0.5f && newAlong > 0f)
            {
                newAlong = 0f;
            }

            // The tyres: a small slide is taken out quickly, a big one only as fast as the grip allows.
            float grip = Handling.GripAt(along, brakeTurn) * (OffRoad ? 0.85f : 1f) * (Wrecked ? 1.6f : Mathf.Clamp01(GripFactor));
            float decay = across * (1f - Mathf.Exp(-CarHandling.LateralDamping * deltaTime));
            float limit = grip * deltaTime;
            float taken = Mathf.Clamp(decay, -limit, limit);
            float newAcross = across - taken;
            if (Mathf.Abs(across) > 2f)
            {
                // Sliding costs speed: the tyres scrub.
                newAlong = Mathf.MoveTowards(newAlong, 0f, Mathf.Abs(taken) * 0.22f);
            }
            Rigidbody.linearVelocity = forward * newAlong + right * newAcross;

            // The wheel: a yaw rate that needs speed and flips in reverse, followed at a rate a spin can be caught at.
            float direction = newAlong < -0.3f || (reversing && newAlong <= 0.3f) ? -1f : 1f;
            float target = steer * Handling.MaxYawRate(newAlong) * Mathf.Clamp01(SteerFactor) * direction * (1f + 0.25f * brakeTurn);
            if (direction > 0f && newAlong > 4f)
            {
                // The nose may run ahead of the way the car goes only so far: past it the wheel stops turning it further.
                float nose = Mathf.Atan2(-newAcross, newAlong);
                if (target * nose > 0f)
                {
                    float limitAngle = Mathf.Lerp(CarHandling.DriftAngle, CarHandling.BrakeDriftAngle, brakeTurn);
                    target *= Mathf.Clamp01(1f - (Mathf.Abs(nose) - limitAngle) / 0.15f);
                }
            }
            float slide = Mathf.Clamp01((Mathf.Abs(newAcross) - 1.5f) / 7f);
            float response = Wrecked ? 3f : Handling.YawResponse * (1f - 0.6f * slide);
            float yaw = Mathf.MoveTowards(Rigidbody.angularVelocity.y, target, response * deltaTime);
            Rigidbody.angularVelocity = new Vector3(0f, yaw, 0f);

            ForwardSpeed = newAlong;
            LateralSpeed = newAcross;
            Braking = braking || (throttle < -0.01f && Mathf.Abs(newAlong) < 0.5f);
            float brakeSkid = braking && along > 6f ? 0.35f * -throttle : 0f;
            float launch = throttle > 0.6f && along >= 0f && along < 9f ? (1f - along / 9f) * 0.5f * Mathf.Clamp01(Handling.Acceleration / 18f) : 0f;
            Slip = Wrecked ? 0f : Mathf.Max(slide, Mathf.Max(brakeSkid, launch));
            AdvanceWheels(steer, newAlong, deltaTime);

            if (Wrecked && Mathf.Abs(newAlong) < 0.4f && Mathf.Abs(newAcross) < 0.4f && Mathf.Abs(yaw) < 0.2f)
            {
                // The wreck has come to rest: from now on it is an obstacle the others drive round (or into).
                Rigidbody.linearVelocity = Vector3.zero;
                Rigidbody.angularVelocity = Vector3.zero;
                Rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                Rigidbody.isKinematic = true;
            }
        }

        /// <summary>
        /// Moves a kinematic car (a puppet of another device's car) to <paramref name="position"/> and
        /// <paramref name="rotation"/> for this physics step, and works out what the model and the sound show from the
        /// <paramref name="velocity"/> it reports: the forward and sideways speed, the slide, the brake lights, the wheels'
        /// angle (from the turn between two steps) and their roll.
        /// </summary>
        public virtual void Follow(Vector3 position, Quaternion rotation, Vector3 velocity, bool braking, float deltaTime)
        {
            if (Rigidbody == null || deltaTime <= 0f)
            {
                return;
            }
            position.y = transform.position.y;
            Quaternion before = Rigidbody.rotation;
            Rigidbody.MovePosition(position);
            Rigidbody.MoveRotation(rotation);
            Vector3 forward = Flat(rotation * Vector3.forward);
            var right = new Vector3(forward.z, 0f, -forward.x);
            ForwardSpeed = Vector3.Dot(velocity, forward);
            LateralSpeed = Vector3.Dot(velocity, right);
            Braking = braking;
            Slip = Wrecked ? 0f : Mathf.Clamp01((Mathf.Abs(LateralSpeed) - 1.5f) / 7f);
            float yawRate = Mathf.DeltaAngle(before.eulerAngles.y, rotation.eulerAngles.y) * Mathf.Deg2Rad / deltaTime;
            float maxYaw = Handling.MaxYawRate(ForwardSpeed);
            float steer = maxYaw > 0.05f ? Mathf.Clamp(yawRate / maxYaw * Mathf.Sign(ForwardSpeed == 0f ? 1f : ForwardSpeed), -1f, 1f) : 0f;
            AdvanceWheels(steer, ForwardSpeed, deltaTime);
        }

        /// <summary>The car is wrecked: no more drive, it rolls to a stop and stays as an obstacle.</summary>
        public virtual void Wreck()
        {
            Wrecked = true;
            Nitro = false;
            Held = false;
        }

        public virtual void Push(Vector3 impulse)
        {
            if (Rigidbody != null && !Rigidbody.isKinematic)
            {
                impulse.y = 0f;
                Rigidbody.AddForce(impulse, ForceMode.Impulse);
            }
        }

        public virtual void Spin(float yawDegreesPerSecond)
        {
            if (Rigidbody != null && !Rigidbody.isKinematic)
            {
                Rigidbody.angularVelocity += new Vector3(0f, yawDegreesPerSecond * Mathf.Deg2Rad, 0f);
            }
        }

        /// <summary>Puts the car at <paramref name="pose"/> at rest (a stuck computer car put back on the road).</summary>
        public virtual void Teleport(Pose pose)
        {
            if (Rigidbody == null)
            {
                transform.SetPositionAndRotation(pose.position, pose.rotation);
                return;
            }
            Rigidbody.linearVelocity = Vector3.zero;
            Rigidbody.angularVelocity = Vector3.zero;
            Rigidbody.position = pose.position;
            Rigidbody.rotation = pose.rotation;
            transform.SetPositionAndRotation(pose.position, pose.rotation);
            ForwardSpeed = LateralSpeed = Slip = 0f;
        }

        protected virtual void OnCollisionEnter(Collision collision)
        {
            if (collision.contactCount == 0)
            {
                return;
            }
            Vector3 normal = collision.GetContact(0).normal;
            RaiseCollided(collision, Mathf.Abs(Vector3.Dot(collision.relativeVelocity, normal)));
        }

        protected void RaiseCollided(Collision collision, float impactSpeed)
        {
            Collided?.Invoke(collision, impactSpeed);
        }

        /// <summary>On the grid: the car does not move, but the wheels and the throttle still show what the driver does.</summary>
        private void HoldStill(float throttle, float steer, float deltaTime)
        {
            Rigidbody.linearVelocity = Vector3.zero;
            Rigidbody.angularVelocity = Vector3.zero;
            ForwardSpeed = LateralSpeed = Slip = 0f;
            Braking = throttle < -0.01f;
            AdvanceWheels(steer, 0f, deltaTime);
        }

        private void AdvanceWheels(float steer, float along, float deltaTime)
        {
            float lock01 = Mathf.Clamp01(Mathf.Abs(along) / Mathf.Max(1f, Handling.TopSpeed));
            float target = steer * Mathf.Lerp(WheelLockLow, WheelLockHigh, lock01);
            steerShown = Mathf.MoveTowards(steerShown, target, 240f * deltaTime);
            SteerAngle = steerShown;
            WheelSpin = Mathf.Repeat(WheelSpin + along / wheelRadius * Mathf.Rad2Deg * deltaTime, 360f);
        }

        private static Vector3 Flat(Vector3 direction)
        {
            direction.y = 0f;
            return direction.sqrMagnitude > 1e-6f ? direction.normalized : Vector3.forward;
        }
    }
}
