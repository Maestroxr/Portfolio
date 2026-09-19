using Gamebox;
using Gamebox.UI;
using TMPro;
using UnityEngine;

namespace Portfolio.EndlessRunner
{
    /// <summary>Settings panel of the Endless Runner module: speeds, jump height and hearts.</summary>
    public class RunnerSettingsUI : SettingsUI
    {
        [SerializeField] private TMP_InputField forwardSpeed;
        [SerializeField] private TMP_InputField maxSpeed;
        [SerializeField] private TMP_InputField acceleration;
        [SerializeField] private TMP_InputField sideSpeed;
        [SerializeField] private TMP_InputField jumpHeight;
        [SerializeField] private TMP_InputField hearts;

        public override void UpdateFromSettings(IGameSettings settings)
        {
            if (!(settings is RunnerSettings runnerSettings))
            {
                return;
            }
            SetText(forwardSpeed, runnerSettings.ForwardSpeed);
            SetText(maxSpeed, runnerSettings.MaxSpeed);
            SetText(acceleration, runnerSettings.Acceleration);
            SetText(sideSpeed, runnerSettings.SideSpeed);
            SetText(jumpHeight, runnerSettings.JumpHeight);
            SetText(hearts, runnerSettings.Hearts);
        }

        public override void CopyToSettings(IGameSettings settings)
        {
            if (!(settings is RunnerSettings runnerSettings))
            {
                return;
            }
            runnerSettings.ForwardSpeed = ParseFloat(forwardSpeed, runnerSettings.ForwardSpeed);
            runnerSettings.MaxSpeed = ParseFloat(maxSpeed, runnerSettings.MaxSpeed);
            runnerSettings.Acceleration = ParseFloat(acceleration, runnerSettings.Acceleration);
            runnerSettings.SideSpeed = ParseFloat(sideSpeed, runnerSettings.SideSpeed);
            runnerSettings.JumpHeight = ParseFloat(jumpHeight, runnerSettings.JumpHeight);
            runnerSettings.Hearts = ParseInt(hearts, runnerSettings.Hearts);
        }

        protected override void SetInputsInteractable(bool interactable)
        {
            SetInteractable(forwardSpeed, interactable);
            SetInteractable(maxSpeed, interactable);
            SetInteractable(acceleration, interactable);
            SetInteractable(sideSpeed, interactable);
            SetInteractable(jumpHeight, interactable);
            SetInteractable(hearts, interactable);
        }
    }
}
