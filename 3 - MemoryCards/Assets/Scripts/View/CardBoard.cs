using UnityEngine;

namespace Portfolio.MemoryCards
{
    /// <summary>
    /// Lays the cards out on a grid that fits the board's rect: the card size follows from the rows and columns, the
    /// spacing and the card aspect. Slot positions are local to the board (its pivot is the centre of the grid).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class CardBoard : MonoBehaviour
    {
        [Tooltip("Gap between two cards as a fraction of the card width.")]
        [SerializeField] internal float spacing = 0.16f;
        [Tooltip("Card width divided by card height.")]
        [SerializeField] internal float aspect = 0.76f;
        [SerializeField] internal float maxCardHeight = 300f;

        private RectTransform rect;
        private int columns = 1;
        private int rows = 1;

        public RectTransform Rect => rect != null ? rect : rect = (RectTransform)transform;

        public Vector2 CardSize { get; private set; } = new Vector2(150f, 200f);

        public int Columns => columns;

        public int Rows => rows;

        /// <summary>Where dealt cards come from: below the middle of the board.</summary>
        public Vector2 DeckPosition => new Vector2(0f, -Rect.rect.height * 0.5f - CardSize.y);

        /// <summary>Computes the card size for a grid of <paramref name="columnCount"/> by <paramref name="rowCount"/>.</summary>
        public void Layout(int columnCount, int rowCount)
        {
            columns = Mathf.Max(1, columnCount);
            rows = Mathf.Max(1, rowCount);
            Rect area = Rect.rect;
            float widthUnits = columns + (columns - 1) * spacing;
            float heightUnits = rows / aspect + (rows - 1) * spacing;
            float width = Mathf.Min(area.width / widthUnits, area.height / heightUnits);
            width = Mathf.Min(width, maxCardHeight * aspect);
            CardSize = new Vector2(width, width / aspect);
        }

        public Vector2 SlotPosition(int slot)
        {
            int column = slot % columns;
            int row = slot / columns;
            float step = CardSize.x * (1f + spacing);
            float stepY = CardSize.y + CardSize.x * spacing;
            float x = (column - (columns - 1) * 0.5f) * step;
            float y = ((rows - 1) * 0.5f - row) * stepY;
            return new Vector2(x, y);
        }

        /// <summary>A point off the board in the direction of <paramref name="slot"/>, where cards fly to when it is cleared.</summary>
        public Vector2 ExitPosition(int slot)
        {
            Vector2 from = SlotPosition(slot);
            Vector2 direction = from.sqrMagnitude > 1f ? from.normalized : Vector2.down;
            return from + direction * (Rect.rect.width * 0.8f) + Vector2.down * Rect.rect.height * 0.3f;
        }
    }
}
