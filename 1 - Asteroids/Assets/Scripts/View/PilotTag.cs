using TMPro;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// Who flies a ship when several pilots share the playfield: the halo under the ship glows in the colour of the
    /// pilot's seat and the pilot's name floats upright above it in that colour. The stand-ins of an online room
    /// (<see cref="RemoteShip"/>) and the ships of a local co-op mission wear one; <see cref="Remove"/> gives the ship its
    /// own look back.
    /// </summary>
    public class PilotTag : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        /// <summary>How far above the middle of the ship the name floats.</summary>
        private const float LabelHeight = 1.45f;

        private TextMeshPro label;
        private Renderer halo;

        public Color Color { get; private set; } = Color.white;

        /// <summary>The ship's tag (added the first time) showing <paramref name="pilotName"/> in <paramref name="color"/>.</summary>
        public static PilotTag Show(GameObject ship, string pilotName, Color color)
        {
            if (!ship.TryGetComponent(out PilotTag tag))
            {
                tag = ship.AddComponent<PilotTag>();
            }
            tag.Set(pilotName, color);
            return tag;
        }

        /// <summary>Takes the tag off <paramref name="ship"/>: the halo has its own colour again and the name goes.</summary>
        public static void Remove(GameObject ship)
        {
            if (ship != null && ship.TryGetComponent(out PilotTag tag))
            {
                tag.Clear();
                Destroy(tag);
            }
        }

        public void Set(string pilotName, Color color)
        {
            Color = color;
            Tint();
            if (label == null)
            {
                var labelObject = new GameObject("Name");
                labelObject.transform.SetParent(transform, false);
                label = labelObject.AddComponent<TextMeshPro>();
                label.alignment = TextAlignmentOptions.Center;
                label.fontSize = 3.2f;
                label.fontStyle = FontStyles.Bold;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Overflow;
                label.rectTransform.sizeDelta = new Vector2(8f, 1f);
                label.sortingOrder = 5;
            }
            label.text = pilotName;
            label.color = color;
            Place();
        }

        private void LateUpdate()
        {
            Place();
        }

        /// <summary>The name stays upright above the ship however it turns.</summary>
        private void Place()
        {
            if (label != null)
            {
                label.transform.position = transform.position + new Vector3(0f, LabelHeight, -0.3f);
                label.transform.rotation = Quaternion.identity;
            }
        }

        /// <summary>The halo under the ship glows in the colour of the seat.</summary>
        private void Tint()
        {
            if (halo == null)
            {
                Transform glow = transform.Find("Halo");
                if (glow == null || !glow.TryGetComponent(out halo))
                {
                    return;
                }
            }
            var block = new MaterialPropertyBlock();
            block.SetColor(BaseColorId, new Color(Color.r * 1.6f, Color.g * 1.6f, Color.b * 1.6f, 0.5f));
            halo.SetPropertyBlock(block);
        }

        private void Clear()
        {
            if (halo != null)
            {
                halo.SetPropertyBlock(null);
            }
            if (label != null)
            {
                Destroy(label.gameObject);
                label = null;
            }
        }
    }
}
