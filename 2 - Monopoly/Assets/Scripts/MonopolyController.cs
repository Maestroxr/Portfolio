using System.Collections;
using Gamebox;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Monopoly
{
    /// <summary>
    /// Local controller of the Monopoly module: rolls the dice for the current player when the dialog's OK button
    /// is pressed, moves the player and shows the dialogs. Starting a game goes through the base
    /// <see cref="OfflineGameController"/> flow into <see cref="MonopolyGameManager.StartGame"/>.
    /// </summary>
    public class MonopolyController : OfflineGameController
    {
        [SerializeField] private Dice dice;
        [SerializeField] private Button okButton;

        private bool diceRolling = false;

        public MonopolyGameManager Monopoly => BaseManager as MonopolyGameManager;


        protected override void Start()
        {
            base.Start();
            if (Monopoly != null)
            {
                Monopoly.PlayerMovedEvent += PlayerMoved;
            }
            if (okButton != null)
            {
                okButton.onClick.AddListener(OK);
            }
            PlayerDialog("", "");
        }


        public void OK()
        {
            if (diceRolling || Monopoly == null || !Monopoly.IsGameRunning)
            {
                return;
            }
            diceRolling = true;
            StartCoroutine(RollTheDice());
        }


        public void NextTurn()
        {
            PlayerDialog("", "");
            int move = dice.CastDie();
            MonopolyPlayer player = Monopoly.GetPlayer(Monopoly.CurrentPlayer);
            if (player != null)
            {
                player.Move(move);
            }
            Monopoly.NextTurn();
            diceRolling = false;
        }


        private IEnumerator RollTheDice()
        {
            float diceRollTime = Monopoly.MonopolySettings.DiceRollTime;
            dice.RollDie(diceRollTime);
            yield return new WaitForSeconds(diceRollTime);
            if (Monopoly.IsGameRunning)
            {
                NextTurn();
            }
            else
            {
                diceRolling = false;
            }
        }


        public void PlayerMoved(MonopolyPlayer player)
        {
            Tile newTile = Monopoly.GetTile(player.Location);
            if (newTile != null)
            {
                newTile.PlayerVisit(player);
            }
        }


        public void PlayerDialog(string header, string body)
        {
            (UI as MonopolyUI)?.DisplayPlayerDialog(header, body);
        }


        public void ResetDialog()
        {
            diceRolling = false;
            PlayerDialog("", "");
        }
    }
}
