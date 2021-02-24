using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{
    public int TileLocation { get; private set; }
    public HashSet<PlayerId> VisitingPlayers { get; private set; } = new HashSet<PlayerId>();
    public delegate void TilePlayer(Player player);
    public event TilePlayer PlayerVisitedEvent;
    public event TilePlayer PlayerLeftEvent;
    

    public void SetupTile(int location)
    {
        TileLocation = location;
    }


    public virtual void PlayerVisit(Player player)
    {
        VisitingPlayers.Add(player.PlayerId);
        PlayerVisitedEvent?.Invoke(player);
    }


    public virtual void PlayerLeave(Player player)
    {
        VisitingPlayers.Remove(player.PlayerId);
        PlayerLeftEvent?.Invoke(player);
    }
}
