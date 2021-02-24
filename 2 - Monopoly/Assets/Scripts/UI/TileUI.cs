using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TileUI : MonoBehaviour
{
    [SerializeField] protected MonopolyUI monopolyUI;
    [SerializeField] protected Button assetButton;

    private Tile tile;
    private float playerMoveTime;


    // Start is called before the first frame update
    public virtual void Start()
    {
        tile = GetComponent<Tile>();
        tile.PlayerVisitedEvent += PlayerVisit;
        tile.PlayerLeftEvent += PlayerLeave;
        playerMoveTime = monopolyUI.Settings.PlayerMoveTime;
    }


    public virtual void PlayerVisit(Player player)
    {
        Image inTile = monopolyUI.PlayerInTileImages[player.PlayerId];
        RectTransform rect = inTile.GetComponent<RectTransform>();
        Vector2 oldPos = rect.position;
        inTile.transform.SetParent(transform, false);
        inTile.transform.rotation = Quaternion.LookRotation(Vector3.forward);
        iTween.MoveFrom(inTile.gameObject, oldPos, playerMoveTime * player.DistanceFromLastLocation());
    }


    public virtual void PlayerLeave(Player player)
    {
        
    }
}
