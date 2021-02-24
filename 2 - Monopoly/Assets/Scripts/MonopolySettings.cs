using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MonopolySettings", menuName = "Monopoly/Settings", order = 1)]
public class MonopolySettings : ScriptableObject
{
    // Board class contains the tile layout of the monopoly board
    [SerializeField]
    private Board board;
    public Board Board => board;

    [SerializeField]
    private uint maxPlayers = 2;
    public uint MaxPlayers => maxPlayers;

    public Player PlayerPrefab;
    
    [SerializeField]
    private int initialPlayerLocation  = 0;
    public int InitialPlayerLocation => initialPlayerLocation;

    [field: SerializeField]
    public int InitialPlayerMoney { get; private set; } = 5;

    [SerializeField]
    private float diceRollTime;
    public float DiceRollTime => diceRollTime;

    [SerializeField]
    private float diceRollChangeValueTime;
    public float DiceRollChangeValueTime => diceRollChangeValueTime;

    [SerializeField]
    private float playerMoveTime;
    public float PlayerMoveTime => playerMoveTime;
}
