using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Asset", menuName = "Monopoly/Asset", order = 1)]
public class Asset : ScriptableObject
{
    public int Price => price;
    public int Fine => fine;
   
    public Player OwningPlayer
    {
        get { return owningPlayer; }
        set
        {
            OwnershipChangedEvent?.Invoke(value, owningPlayer);
            owningPlayer = value;
        }
    }
    public delegate void OwnershipChanged(Player newOwnerId, Player oldOwnerId);
    public event OwnershipChanged OwnershipChangedEvent;

    [SerializeField] private string assetName;
    [SerializeField] private int price;
    [SerializeField] private int fine;

    private Player owningPlayer;
}
