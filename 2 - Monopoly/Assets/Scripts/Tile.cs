using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Monopoly
{
    public class Tile : MonoBehaviour
    {
        public int TileLocation { get; private set; }
        public HashSet<PlayerId> VisitingPlayers { get; private set; } = new HashSet<PlayerId>();
        public delegate void TilePlayer(MonopolyPlayer player);
        public event TilePlayer PlayerVisitedEvent;
        public event TilePlayer PlayerLeftEvent;


        public void SetupTile(int location)
        {
            TileLocation = location;
        }


        public virtual void PlayerVisit(MonopolyPlayer player)
        {
            VisitingPlayers.Add(player.PlayerId);
            PlayerVisitedEvent?.Invoke(player);
        }


        public virtual void PlayerLeave(MonopolyPlayer player)
        {
            VisitingPlayers.Remove(player.PlayerId);
            PlayerLeftEvent?.Invoke(player);
        }
    }
}
