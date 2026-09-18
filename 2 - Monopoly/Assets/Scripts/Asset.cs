using UnityEngine;

namespace Portfolio.Monopoly
{
    [CreateAssetMenu(fileName = "Asset", menuName = "Monopoly/Asset", order = 1)]
    public class Asset : ScriptableObject
    {
        public int Price => price;
        public int Fine => fine;

        public MonopolyPlayer OwningPlayer
        {
            get { return owningPlayer; }
            set
            {
                OwnershipChangedEvent?.Invoke(value, owningPlayer);
                owningPlayer = value;
            }
        }
        public delegate void OwnershipChanged(MonopolyPlayer newOwnerId, MonopolyPlayer oldOwnerId);
        public event OwnershipChanged OwnershipChangedEvent;

        [SerializeField] private string assetName;
        [SerializeField] private int price;
        [SerializeField] private int fine;

        private MonopolyPlayer owningPlayer;
    }
}
