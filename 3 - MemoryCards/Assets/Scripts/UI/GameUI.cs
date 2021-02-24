using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio
{
    public class GameUI : MonoBehaviour
    {
        [SerializeField] private SettingsUI SettingsUI;
        [SerializeField] private RectTransform MenuButtonsUI;
        [SerializeField] private TextMeshProUGUI Score;
        [SerializeField] private TextMeshProUGUI Timer;
        [SerializeField] private TextMeshProUGUI ErrorConsole;
        [SerializeField] private Button ReturnToGame, SaveGame, LoadGame;
        [SerializeField] private float displayTimeError;
       

        public void UpdateGameState(GameState state)
        {
            switch(state)
            {
                case GameState.Paused:
                    ReturnToGame.interactable = true;
                    SaveGame.interactable = true;
                    break;
                default:
                    ReturnToGame.interactable = false;
                    SaveGame.interactable = false;
                    break;
            }
        }
        

        public void EnableLoad()
        {
            LoadGame.interactable = true;
        }

        public void ShowSettings()
        {
            SettingsUI.gameObject.SetActive(true);
            MenuButtonsUI.gameObject.SetActive(false);
        }


        public void HideSettings()
        {
            SettingsUI.gameObject.SetActive(false);
            MenuButtonsUI.gameObject.SetActive(true);
        }


        public void UpdateTimer(float timer)
        {
            Timer.text = $"Time: {timer.ToString("F2")}";
        }


        public void UpdateScore(int score)
        {
            Score.text = $"Score: {score}";
        }


        public void UpdateError(string error)
        {
            ErrorConsole.text = error;
            Invoke("ClearError", displayTimeError);
        }


       public void ClearError()
        {
            ErrorConsole.text = "";
        }
    }
}