using Gamebox;
using Gamebox.UI;
using TMPro;
using UnityEngine;

namespace Portfolio.EndlessRunner
{
    /// <summary>Settings panel of the Endless Runner module: speeds and collectible density.</summary>
    public class RunnerSettingsUI : SettingsUI
    {
        [SerializeField] private TMP_InputField forwardSpeed;
        [SerializeField] private TMP_InputField sideSpeed;
        [SerializeField] private TMP_InputField collidablesPerTerrain;
        [SerializeField] private TMP_InputField collidablesRadius;

        public override void UpdateFromSettings(IGameSettings settings)
        {
            if (!(settings is RunnerSettings runnerSettings))
            {
                return;
            }
            SetText(forwardSpeed, runnerSettings.ForwardSpeed);
            SetText(sideSpeed, runnerSettings.SideSpeed);
            SetText(collidablesPerTerrain, runnerSettings.CollidablesPerTerrain);
            SetText(collidablesRadius, runnerSettings.CollidablesRadius);
        }

        public override void CopyToSettings(IGameSettings settings)
        {
            if (!(settings is RunnerSettings runnerSettings))
            {
                return;
            }
            runnerSettings.ForwardSpeed = ParseFloat(forwardSpeed, runnerSettings.ForwardSpeed);
            runnerSettings.SideSpeed = ParseFloat(sideSpeed, runnerSettings.SideSpeed);
            runnerSettings.CollidablesPerTerrain = ParseInt(collidablesPerTerrain, runnerSettings.CollidablesPerTerrain);
            runnerSettings.CollidablesRadius = ParseInt(collidablesRadius, runnerSettings.CollidablesRadius);
        }

        protected override void SetInputsInteractable(bool interactable)
        {
            SetInteractable(forwardSpeed, interactable);
            SetInteractable(sideSpeed, interactable);
            SetInteractable(collidablesPerTerrain, interactable);
            SetInteractable(collidablesRadius, interactable);
        }
    }
}
