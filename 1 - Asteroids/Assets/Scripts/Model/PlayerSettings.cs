using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// A ship of the hangar: how it flies (thrust, top speed, drag, turn rate, dash), how tough it is, how fast it
    /// fires, what it looks like and how many campaign stars unlock it. Also holds the keys the ship is flown with.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerSettings", menuName = "Asteroids/PlayerSettings", order = 1)]
    public class PlayerSettings : ScriptableObject
    {
        [field: SerializeField]
        public string DisplayName { get; private set; } = "Sparrow";

        [field: SerializeField, TextArea]
        public string Description { get; private set; } = "";

        [field: SerializeField, Tooltip("Campaign stars needed before the ship can be chosen in the hangar.")]
        public int StarsToUnlock { get; private set; }

        [field: Header("Flight")]
        [field: SerializeField, Tooltip("Acceleration while thrusting, in meters per second squared.")]
        public float Thrust { get; private set; } = 22f;

        [field: SerializeField, Tooltip("Top cruising speed in meters per second.")]
        public float MovementSpeed { get; private set; } = 11f;

        [field: SerializeField, Tooltip("Share of the speed lost per second while coasting.")]
        public float Drag { get; private set; } = 0.55f;

        [field: SerializeField, Tooltip("Deceleration while braking, in meters per second squared.")]
        public float BrakeForce { get; private set; } = 20f;

        [field: SerializeField, Tooltip("Top turn rate in degrees per second.")]
        public float RotationSpeed { get; private set; } = 250f;

        [field: SerializeField, Tooltip("How quickly the turn rate follows the stick; higher is snappier.")]
        public float TorqueDrag { get; private set; } = 10f;

        [field: SerializeField, Tooltip("Speed of a dash in meters per second.")]
        public float DashSpeed { get; private set; } = 22f;

        [field: SerializeField]
        public float DashDuration { get; private set; } = 0.22f;

        [field: SerializeField]
        public float DashCooldown { get; private set; } = 2.2f;

        [field: Header("Combat")]
        [field: SerializeField, Tooltip("Multiplies the hull strength of the mission settings.")]
        public float HullMultiplier { get; private set; } = 1f;

        [field: SerializeField]
        public float ShieldCapacity { get; private set; } = 100f;

        [field: SerializeField, Tooltip("Multiplies the fire rate of every weapon.")]
        public float FireRateMultiplier { get; private set; } = 1f;

        [field: SerializeField, Tooltip("Collision radius of the ship in meters.")]
        public float HitRadius { get; private set; } = 0.62f;

        [field: Header("Look")]
        [field: SerializeField]
        public Mesh ModelMesh { get; private set; }

        [field: SerializeField]
        public Material ModelMaterial { get; private set; }

        [field: SerializeField, Tooltip("Scale that fits the model to the ship's size.")]
        public float ModelScale { get; private set; } = 0.12f;

        [field: SerializeField, Tooltip("Where the engine flames sit, in ship space (x across, y along the nose).")]
        public Vector2[] EnginePoints { get; private set; } = { new Vector2(0f, -0.7f) };

        [field: SerializeField, Tooltip("Where the guns sit, in ship space.")]
        public Vector2 GunPoint { get; private set; } = new Vector2(0f, 0.8f);

        [field: SerializeField]
        public Color EngineColor { get; private set; } = new Color(0.4f, 0.8f, 1f);

        [field: Header("Keys")]
        [field: SerializeField]
        public KeyCode Forward { get; private set; } = KeyCode.W;

        [field: SerializeField]
        public KeyCode Left { get; private set; } = KeyCode.A;

        [field: SerializeField]
        public KeyCode Right { get; private set; } = KeyCode.D;

        [field: SerializeField]
        public KeyCode Brake { get; private set; } = KeyCode.S;

        [field: SerializeField]
        public KeyCode Shoot { get; private set; } = KeyCode.Space;

        [field: SerializeField]
        public KeyCode Dash { get; private set; } = KeyCode.LeftShift;

        [field: SerializeField]
        public KeyCode Bomb { get; private set; } = KeyCode.B;


        /// <summary>Used by tests: flight values without an asset.</summary>
        public void MockSettings(float thrust = 20f, float maxSpeed = 10f, float drag = 0.5f, float rotationSpeed = 200f, float torqueDrag = 10f)
        {
            Thrust = thrust;
            MovementSpeed = maxSpeed;
            Drag = drag;
            RotationSpeed = rotationSpeed;
            TorqueDrag = torqueDrag;
        }
    }
}
