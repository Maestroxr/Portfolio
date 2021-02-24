using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TileBehaviour { Start, Asset, Reward}
[CreateAssetMenu(fileName = "Board", menuName = "Monopoly/Board", order = 1)]
public class Board : ScriptableObject
{
    public List<Asset> Assets => new List<Asset>(assets);
    public List<Reward> Rewards => new List<Reward>(rewards);
    public List<TileBehaviour> TileLayout => new List<TileBehaviour>(tileLayout);
   
    [SerializeField]
    private List<Asset> assets = new List<Asset>();
    [SerializeField]
    private List<Reward> rewards = new List<Reward>();
    [SerializeField]
    private List<TileBehaviour> tileLayout = new List<TileBehaviour>();


    public bool Check(out string error)
    {
        if (tileLayout.Count == 0 || tileLayout[0] != TileBehaviour.Start)
        {
            error = $"Tiles number cannot be 0 or the first tile has to be the starting tile";
            return false;
        }
        error = null;
        return true;
    }
}
