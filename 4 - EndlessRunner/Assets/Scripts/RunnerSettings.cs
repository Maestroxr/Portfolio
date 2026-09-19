using Gamebox;
using UnityEngine;

namespace Portfolio.EndlessRunner
{
    /// <summary>
    /// User configurable values of a run: how fast the runner starts, how fast it can get, how quickly it switches
    /// lanes, how high it jumps and how many hits it can take. Every campaign level carries its own settings asset.
    /// </summary>
    [CreateAssetMenu(fileName = "RunnerSettings", menuName = "Endless Runner/Settings", order = 1)]
    public class RunnerSettings : GameSettings
    {
        public const float SpeedLimit = 60f;
        public const int HeartLimit = 9;

        [field: SerializeField, Tooltip("Speed at the start of a run, in meters per second.")]
        public float ForwardSpeed { get; set; } = 10f;

        [field: SerializeField, Tooltip("Top speed the run accelerates to, in meters per second.")]
        public float MaxSpeed { get; set; } = 16f;

        [field: SerializeField, Tooltip("Speed gained per meter run, as in v^2 = v0^2 + 2 * a * distance.")]
        public float Acceleration { get; set; } = 0.15f;

        [field: SerializeField, Tooltip("Sideways speed of a lane switch, in meters per second.")]
        public float SideSpeed { get; set; } = 14f;

        [field: SerializeField, Tooltip("Height of a normal jump, in meters.")]
        public float JumpHeight { get; set; } = 1.8f;

        [field: SerializeField, Tooltip("Hits the runner can take before the run ends.")]
        public int Hearts { get; set; } = 3;


        /// <summary>Target speed of the run after <paramref name="distance"/> meters.</summary>
        public float SpeedAtDistance(float distance)
        {
            float start = Mathf.Max(0.1f, ForwardSpeed);
            float speed = Mathf.Sqrt(start * start + 2f * Mathf.Max(0f, Acceleration) * Mathf.Max(0f, distance));
            return Mathf.Min(Mathf.Max(MaxSpeed, start), speed);
        }


        public override bool AreSettingsValid(out string message)
        {
            if (ForwardSpeed <= 0f || ForwardSpeed > SpeedLimit)
            {
                message = $"Start speed {ForwardSpeed} has to be between 0 and {SpeedLimit}";
                return false;
            }
            if (MaxSpeed < ForwardSpeed || MaxSpeed > SpeedLimit)
            {
                message = $"Max speed {MaxSpeed} has to be between the start speed and {SpeedLimit}";
                return false;
            }
            if (Acceleration < 0f || Acceleration > 5f)
            {
                message = $"Acceleration {Acceleration} has to be between 0 and 5";
                return false;
            }
            if (SideSpeed <= 0f || SideSpeed > SpeedLimit)
            {
                message = $"Lane switch speed {SideSpeed} has to be between 0 and {SpeedLimit}";
                return false;
            }
            if (JumpHeight < 0.5f || JumpHeight > 6f)
            {
                message = $"Jump height {JumpHeight} has to be between 0.5 and 6";
                return false;
            }
            if (Hearts < 1 || Hearts > HeartLimit)
            {
                message = $"Hearts {Hearts} has to be between 1 and {HeartLimit}";
                return false;
            }
            message = "OK";
            return true;
        }


        public override void SaveSettings(IStorageStrategy storage, string prefix)
        {
            if (!AreSettingsValid(out string error))
            {
                throw new GameSettingsException($"Cannot save invalid settings. Reason: {error}");
            }
            storage.SetFloat($"{prefix}ForwardSpeed", ForwardSpeed);
            storage.SetFloat($"{prefix}MaxSpeed", MaxSpeed);
            storage.SetFloat($"{prefix}Acceleration", Acceleration);
            storage.SetFloat($"{prefix}SideSpeed", SideSpeed);
            storage.SetFloat($"{prefix}JumpHeight", JumpHeight);
            storage.SetInt($"{prefix}Hearts", Hearts);
        }


        public override void LoadSettings(IStorageStrategy storage, string prefix)
        {
            ForwardSpeed = LoadFloat(storage, $"{prefix}ForwardSpeed", ForwardSpeed);
            MaxSpeed = LoadFloat(storage, $"{prefix}MaxSpeed", MaxSpeed);
            Acceleration = LoadFloat(storage, $"{prefix}Acceleration", Acceleration);
            SideSpeed = LoadFloat(storage, $"{prefix}SideSpeed", SideSpeed);
            JumpHeight = LoadFloat(storage, $"{prefix}JumpHeight", JumpHeight);
            if (storage.DoesKeyExist($"{prefix}Hearts"))
            {
                Hearts = storage.GetInt($"{prefix}Hearts");
            }
        }


        public override void CopySettings(IGameSettings other)
        {
            if (!(other is RunnerSettings source))
            {
                return;
            }
            ForwardSpeed = source.ForwardSpeed;
            MaxSpeed = source.MaxSpeed;
            Acceleration = source.Acceleration;
            SideSpeed = source.SideSpeed;
            JumpHeight = source.JumpHeight;
            Hearts = source.Hearts;
        }


        private static float LoadFloat(IStorageStrategy storage, string key, float fallback)
        {
            return storage.DoesKeyExist(key) ? storage.GetFloat(key) : fallback;
        }
    }
}
