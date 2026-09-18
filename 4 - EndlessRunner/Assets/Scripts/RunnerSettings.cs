using Gamebox;
using UnityEngine;

namespace Portfolio.EndlessRunner
{
    /// <summary>User configurable values of a run: player speeds and how many collectibles each terrain gets.</summary>
    [CreateAssetMenu(fileName = "RunnerSettings", menuName = "Endless Runner/Settings", order = 1)]
    public class RunnerSettings : GameSettings
    {
        [field: SerializeField]
        public float ForwardSpeed { get; set; } = 12f;
        [field: SerializeField]
        public float SideSpeed { get; set; } = 15f;
        [field: SerializeField]
        public int CollidablesPerTerrain { get; set; } = 5;
        [field: SerializeField]
        public int CollidablesRadius { get; set; } = 10;


        public override bool AreSettingsValid(out string message)
        {
            if (ForwardSpeed <= 0f)
            {
                message = $"Forward speed {ForwardSpeed} has to be positive";
                return false;
            }
            if (SideSpeed < 0f)
            {
                message = $"Side speed {SideSpeed} cannot be negative";
                return false;
            }
            if (CollidablesPerTerrain < 0)
            {
                message = $"Collidables per terrain {CollidablesPerTerrain} cannot be negative";
                return false;
            }
            if (CollidablesRadius < 0)
            {
                message = $"Collidables radius {CollidablesRadius} cannot be negative";
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
            storage.SetFloat($"{prefix}SideSpeed", SideSpeed);
            storage.SetInt($"{prefix}CollidablesPerTerrain", CollidablesPerTerrain);
            storage.SetInt($"{prefix}CollidablesRadius", CollidablesRadius);
        }


        public override void LoadSettings(IStorageStrategy storage, string prefix)
        {
            if (storage.DoesKeyExist($"{prefix}ForwardSpeed"))
            {
                ForwardSpeed = storage.GetFloat($"{prefix}ForwardSpeed");
            }
            if (storage.DoesKeyExist($"{prefix}SideSpeed"))
            {
                SideSpeed = storage.GetFloat($"{prefix}SideSpeed");
            }
            if (storage.DoesKeyExist($"{prefix}CollidablesPerTerrain"))
            {
                CollidablesPerTerrain = storage.GetInt($"{prefix}CollidablesPerTerrain");
            }
            if (storage.DoesKeyExist($"{prefix}CollidablesRadius"))
            {
                CollidablesRadius = storage.GetInt($"{prefix}CollidablesRadius");
            }
        }


        public override void CopySettings(IGameSettings other)
        {
            if (!(other is RunnerSettings source))
            {
                return;
            }
            ForwardSpeed = source.ForwardSpeed;
            SideSpeed = source.SideSpeed;
            CollidablesPerTerrain = source.CollidablesPerTerrain;
            CollidablesRadius = source.CollidablesRadius;
        }
    }
}
