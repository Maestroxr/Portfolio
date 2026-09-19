using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// Newtonian flight of the ship, free of MonoBehaviours so it can be tested on its own: thrust along the nose,
    /// drag while coasting, a brake, a turn rate that eases toward the stick and a short dash. The ship keeps drifting
    /// when the engines are off, as in the arcade original.
    /// </summary>
    public class PlayerSimulation
    {
        private readonly ILocalTransformAdapter transform;
        private PlayerSettings settings;
        private float dashTime;
        private float dashCooldown;
        private bool thrustedThisStep;

        public Vector3 Velocity { get; set; }

        /// <summary>Current turn rate in degrees per second; positive turns left (counter-clockwise).</summary>
        public float AngularVelocity { get; private set; }

        public bool IsThrusting { get; private set; }

        public bool IsDashing => dashTime > 0f;

        /// <summary>0 when the dash is ready, 1 right after dashing.</summary>
        public float DashRecharge => settings != null && settings.DashCooldown > 0f ? Mathf.Clamp01(dashCooldown / settings.DashCooldown) : 0f;

        public float Speed => Velocity.magnitude;


        public PlayerSimulation(ILocalTransformAdapter transformAdapter, PlayerSettings settings)
        {
            transform = transformAdapter;
            this.settings = settings;
        }


        /// <summary>Switches to another ship's flight values, keeping the current motion.</summary>
        public void Apply(PlayerSettings newSettings)
        {
            settings = newSettings;
        }


        /// <summary>Accelerates along the nose for <paramref name="time"/> seconds.</summary>
        public void Thrust(float time, float amount = 1f)
        {
            if (settings == null)
            {
                return;
            }
            Velocity += transform.Forward.normalized * settings.Thrust * Mathf.Clamp01(amount) * time;
            thrustedThisStep = true;
        }


        /// <summary>Kept for the original tests: moves straight along the nose at top speed, without inertia.</summary>
        public void MoveForward(float time)
        {
            transform.LocalPosition += transform.Forward.normalized * settings.MovementSpeed * time;
        }


        /// <summary>Slows the ship down.</summary>
        public void Brake(float time)
        {
            if (settings == null)
            {
                return;
            }
            Velocity = Vector3.MoveTowards(Velocity, Vector3.zero, settings.BrakeForce * time);
        }


        /// <summary>Eases the turn rate toward <paramref name="input"/> (-1 right to 1 left) times the top turn rate.</summary>
        public void Steer(float input, float time)
        {
            if (settings == null)
            {
                return;
            }
            float target = Mathf.Clamp(input, -1f, 1f) * settings.RotationSpeed;
            float response = settings.RotationSpeed * Mathf.Max(0.1f, settings.TorqueDrag);
            AngularVelocity = Mathf.MoveTowards(AngularVelocity, target, response * time);
        }


        /// <summary>Kept for the original call sites: a full turn input toward <paramref name="direction"/> (forward = left).</summary>
        public void Rotate(Vector3 direction, float time)
        {
            Steer(direction == Vector3.back ? -1f : 1f, time);
        }


        /// <summary>Bursts forward along the nose. Returns false while the dash recharges.</summary>
        public bool Dash()
        {
            if (settings == null || dashCooldown > 0f)
            {
                return false;
            }
            dashTime = settings.DashDuration;
            dashCooldown = settings.DashCooldown;
            Vector3 direction = transform.Forward.normalized;
            float along = Vector3.Dot(Velocity, direction);
            Velocity += direction * Mathf.Max(0f, settings.DashSpeed - along);
            return true;
        }


        /// <summary>Adds a push (a collision, a blast, gravity) to the ship's motion.</summary>
        public void Push(Vector3 impulse)
        {
            Velocity += impulse;
        }


        public void Stop()
        {
            Velocity = Vector3.zero;
            AngularVelocity = 0f;
            dashTime = 0f;
            IsThrusting = false;
        }


        /// <summary>Moves and turns the ship by <paramref name="time"/> seconds and applies drag and the speed limit.</summary>
        public void Step(float time)
        {
            if (settings == null)
            {
                return;
            }
            IsThrusting = thrustedThisStep;
            thrustedThisStep = false;
            dashTime = Mathf.Max(0f, dashTime - time);
            dashCooldown = Mathf.Max(0f, dashCooldown - time);

            if (!IsThrusting && !IsDashing)
            {
                Velocity *= Mathf.Clamp01(1f - settings.Drag * time);
            }
            float limit = IsDashing ? Mathf.Max(settings.DashSpeed, settings.MovementSpeed) : settings.MovementSpeed;
            float speed = Velocity.magnitude;
            if (speed > limit)
            {
                // Above the limit (after a dash or a push) the ship sheds speed quickly instead of stopping dead.
                float target = Mathf.Max(limit, speed - settings.Thrust * 1.5f * time);
                Velocity = Velocity / speed * target;
            }
            transform.LocalPosition += Velocity * time;
            UpdateRotation(time);
        }


        /// <summary>Turns the ship by the current turn rate.</summary>
        public void UpdateRotation(float time)
        {
            if (Mathf.Abs(AngularVelocity) > 0.001f)
            {
                transform.LocalRotation = Quaternion.AngleAxis(AngularVelocity * time, Vector3.forward) * transform.LocalRotation;
            }
        }
    }
}
