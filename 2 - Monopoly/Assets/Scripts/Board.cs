using UnityEngine;

namespace Portfolio.Monopoly
{
    /// <summary>
    /// The board of the game as an asset: the 40 spaces with their prices and rents, the Chance and Community Chest
    /// cards and the bank's houses and hotels. The content builder fills it with the World Tour board
    /// (<see cref="WorldTourBoard"/>); the rules engine plays on a copy of its <see cref="Layout"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "Board", menuName = "Monopoly/Board", order = 1)]
    public class Board : ScriptableObject
    {
        [SerializeField] private BoardLayout layout = new BoardLayout();

        public BoardLayout Layout => layout;

        /// <summary>A fresh copy for a match, so nothing a match does can change the asset.</summary>
        public BoardLayout CreateLayout()
        {
            return layout.Clone();
        }

        public void SetLayout(BoardLayout value)
        {
            layout = value.Clone();
        }

        public bool Check(out string error)
        {
            if (layout == null)
            {
                error = "The board has no layout";
                return false;
            }
            return layout.Check(out error);
        }
    }
}
