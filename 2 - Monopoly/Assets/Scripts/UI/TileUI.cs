using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Monopoly
{
    public class TileUI : MonoBehaviour
    {
        [SerializeField] protected MonopolyUI monopolyUI;
        [SerializeField] protected Button assetButton;

        private Tile tile;


        // Start is called before the first frame update
        public virtual void Start()
        {
            tile = GetComponent<Tile>();
            if (tile != null)
            {
                tile.PlayerVisitedEvent += PlayerVisit;
                tile.PlayerLeftEvent += PlayerLeave;
            }
        }


        public virtual void PlayerVisit(MonopolyPlayer player)
        {
            if (monopolyUI == null || !monopolyUI.PlayerInTileImages.TryGetValue(player.PlayerId, out Image inTile))
            {
                return;
            }
            float playerMoveTime = monopolyUI.Settings != null ? monopolyUI.Settings.PlayerMoveTime : 0f;
            RectTransform rect = inTile.GetComponent<RectTransform>();
            Vector2 oldPos = rect.position;
            inTile.transform.SetParent(transform, false);
            inTile.transform.rotation = Quaternion.LookRotation(Vector3.forward);
            iTween.MoveFrom(inTile.gameObject, oldPos, playerMoveTime * player.DistanceFromLastLocation());
        }


        public virtual void PlayerLeave(MonopolyPlayer player)
        {

        }
    }
}
