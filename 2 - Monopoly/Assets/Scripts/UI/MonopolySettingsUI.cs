using Gamebox;
using Gamebox.UI;
using TMPro;
using UnityEngine;

namespace Portfolio.Monopoly
{
    /// <summary>Settings panel of the Monopoly module: starting money, dice roll time and token move time.</summary>
    public class MonopolySettingsUI : SettingsUI
    {
        [SerializeField] private TMP_InputField initialPlayerMoney;
        [SerializeField] private TMP_InputField diceRollTime;
        [SerializeField] private TMP_InputField playerMoveTime;

        public override void UpdateFromSettings(IGameSettings settings)
        {
            if (!(settings is MonopolySettings monopolySettings))
            {
                return;
            }
            SetText(initialPlayerMoney, monopolySettings.InitialPlayerMoney);
            SetText(diceRollTime, monopolySettings.DiceRollTime);
            SetText(playerMoveTime, monopolySettings.PlayerMoveTime);
        }

        public override void CopyToSettings(IGameSettings settings)
        {
            if (!(settings is MonopolySettings monopolySettings))
            {
                return;
            }
            monopolySettings.InitialPlayerMoney = ParseInt(initialPlayerMoney, monopolySettings.InitialPlayerMoney);
            monopolySettings.DiceRollTime = ParseFloat(diceRollTime, monopolySettings.DiceRollTime);
            monopolySettings.PlayerMoveTime = ParseFloat(playerMoveTime, monopolySettings.PlayerMoveTime);
        }

        protected override void SetInputsInteractable(bool interactable)
        {
            SetInteractable(initialPlayerMoney, interactable);
            SetInteractable(diceRollTime, interactable);
            SetInteractable(playerMoveTime, interactable);
        }
    }
}
