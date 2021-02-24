using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonopolyController : MonoBehaviour
{
    [SerializeField] private Monopoly monopoly;
    [SerializeField] private MonopolyUI monopolyUI;
    [SerializeField] private Dice dice;

    private float diceRollTime;
    bool diceRolling = false;

    // Start is called before the first frame update
    void Start()
    {
        diceRollTime = monopoly.Settings.DiceRollTime;
        monopoly.PlayerMovedEvent += PlayerMoved;
        PlayerDialog("", "");
    }

    
    public void OK()
    {
        if (diceRolling) return;
        diceRolling = true;
        StartCoroutine(RollTheDice());
    }


    public void NextTurn()
    {
        PlayerDialog("", "");
        int move = dice.CastDie();
        Player player = monopoly.GetPlayer(monopoly.CurrentPlayer);
        player.Move(move);
        monopoly.NextTurn();
        diceRolling = false;
    }


    private IEnumerator RollTheDice()
    {
        dice.RollDie(diceRollTime);
        yield return new WaitForSeconds(diceRollTime);
        NextTurn();
    }


    public void PlayerMoved(Player player)
    {
        Tile newTile = monopoly.GetTile(player.Location);
        newTile.PlayerVisit(player);
    }


    public void PlayerDialog(string header, string body)
    {
        monopolyUI.DisplayPlayerDialog(header, body);
    }
}
