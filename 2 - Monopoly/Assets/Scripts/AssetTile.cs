using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssetTile : Tile
{
    public Asset Asset { get; private set; }


    public void InitAsset(Asset asset)
    {
        Asset = asset;
    }


    public override void PlayerVisit(Player player)
    {
        base.PlayerVisit(player);
        if (Asset.OwningPlayer != null)
        {
            if (Asset.OwningPlayer.PlayerId != player.PlayerId)
            {
                player.PayFine(Asset);
            }
        }
        else
        {
            if (player.CanBuyAsset(Asset))
            {
                player.BuyAsset(Asset);
            }
        }
    }
}
