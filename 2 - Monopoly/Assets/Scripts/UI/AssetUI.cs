using UnityEngine.UI;

namespace Portfolio.Monopoly
{
    public class AssetUI : TileUI
    {
        public Image AssetImage;

        private AssetTile assetTile;


        // Start is called before the first frame update
        public override void Start()
        {
            base.Start();
            assetTile = GetComponent<AssetTile>();
            if (assetTile != null && assetTile.Asset != null)
            {
                assetTile.Asset.OwnershipChangedEvent += AssetChangedOwnership;
                AssetChangedOwnership(assetTile.Asset.OwningPlayer, null);
            }
            else if (AssetImage != null && monopolyUI != null)
            {
                AssetImage.sprite = monopolyUI.AssetFree;
            }
        }


        public void AssetChangedOwnership(MonopolyPlayer newOwner, MonopolyPlayer oldOwner)
        {
            if (AssetImage == null || monopolyUI == null)
            {
                return;
            }
            if (newOwner != null && monopolyUI.AssetOwnedSprites.TryGetValue(newOwner.PlayerId, out var sprite))
            {
                AssetImage.sprite = sprite;
            }
            else
            {
                AssetImage.sprite = monopolyUI.AssetFree;
            }
        }
    }
}
