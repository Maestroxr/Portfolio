using System.Collections.Generic;
using Gamebox;
using Gamebox.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// HUD and menu of the Asteroids module. The per-player score and health bars are the game's own; the menu
    /// buttons and texts come from the shared base game menu prefab and are wired through <see cref="GameUI"/>.
    /// </summary>
    public class AsteroidsUI : GameUI
    {
        [SerializeField]
        private List<PlayerUI> playerUIs = new List<PlayerUI>();
        [SerializeField]
        private Text gameOver;

        private AsteroidsGameManager Asteroids => Manager as AsteroidsGameManager;


        protected override void Awake()
        {
            base.Awake();
            if (Asteroids == null)
            {
                Debug.LogWarning("AsteroidsUI has no AsteroidsGameManager assigned; player HUDs stay unbound.", this);
                return;
            }
            List<AsteroidsPlayer> players = Asteroids.AsteroidPlayers;
            if (players.Count != playerUIs.Count)
            {
                Debug.LogWarning($"Player controllers {players.Count} and views {playerUIs.Count} amounts are not equal", this);
            }
            for (int i = 0; i < players.Count && i < playerUIs.Count; i++)
            {
                playerUIs[i].Setup(players[i]);
            }
            Asteroids.StateChangedEvent += GameStateChanged;
        }


        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1) && StartNewGame != null)
            {
                StartNewGame.onClick.Invoke();
            }
            if (Input.GetKeyDown(KeyCode.F4) && SaveGame != null)
            {
                SaveGame.onClick.Invoke();
            }
            if (Input.GetKeyDown(KeyCode.F5) && LoadGame != null)
            {
                LoadGame.onClick.Invoke();
            }
        }


        private void GameStateChanged(BaseGameState state)
        {
            switch (state)
            {
                case BaseGameState.GameOver:
                    if (gameOver != null)
                    {
                        gameOver.gameObject.SetActive(true);
                    }
                    break;
                case BaseGameState.Running:
                    playerUIs.ForEach(playerUI => playerUI.ResetHealth());
                    if (gameOver != null)
                    {
                        gameOver.gameObject.SetActive(false);
                    }
                    break;
            }
        }
    }
}
