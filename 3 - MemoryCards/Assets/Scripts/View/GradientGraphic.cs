using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.MemoryCards
{
    /// <summary>A UI quad with a vertical colour gradient, for backdrops.</summary>
    public class GradientGraphic : MaskableGraphic
    {
        [SerializeField] internal Color top = Color.white;
        [SerializeField] internal Color bottom = Color.gray;

        public Color Top
        {
            get => top;
            set
            {
                if (top != value)
                {
                    top = value;
                    SetVerticesDirty();
                }
            }
        }

        public Color Bottom
        {
            get => bottom;
            set
            {
                if (bottom != value)
                {
                    bottom = value;
                    SetVerticesDirty();
                }
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = GetPixelAdjustedRect();
            Color32 topColor = top * color;
            Color32 bottomColor = bottom * color;
            vh.AddVert(new Vector3(r.xMin, r.yMin), bottomColor, new Vector2(0f, 0f));
            vh.AddVert(new Vector3(r.xMin, r.yMax), topColor, new Vector2(0f, 1f));
            vh.AddVert(new Vector3(r.xMax, r.yMax), topColor, new Vector2(1f, 1f));
            vh.AddVert(new Vector3(r.xMax, r.yMin), bottomColor, new Vector2(1f, 0f));
            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }
    }
}
