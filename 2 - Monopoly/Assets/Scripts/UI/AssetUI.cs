using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AssetUI : TileUI  
{
    public Image AssetImage;
    
    private AssetTile assetTile;


    // Start is called before the first frame update
    public override void Start()
    {
        base.Start();
        assetTile = GetComponent<AssetTile>();
        assetTile.Asset.OwnershipChangedEvent += AssetChangedOwnership;
        AssetImage.sprite = monopolyUI.AssetFree;
    }


    public void AssetChangedOwnership(Player newOwner, Player oldOwner)
    {
        if (newOwner != null)
        {
        AssetImage.sprite = monopolyUI.AssetOwnedSprites[newOwner.PlayerId];

        }
        else
        {
            AssetImage.sprite = monopolyUI.AssetFree;
        }
    }
}
