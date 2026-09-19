using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Portfolio.MemoryCards
{
    /// <summary>Makes a button feel springy: it grows a little under the pointer and squashes while pressed.</summary>
    public class PressScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] internal RectTransform target;
        [SerializeField] internal float hoverScale = 1.06f;
        [SerializeField] internal float pressScale = 0.93f;

        private Selectable selectable;
        private bool hovered;
        private bool pressed;
        private float scale = 1f;
        private float velocity;

        private void Awake()
        {
            selectable = GetComponent<Selectable>();
            if (target == null)
            {
                target = (RectTransform)transform;
            }
        }

        private void OnDisable()
        {
            hovered = false;
            pressed = false;
            scale = 1f;
            velocity = 0f;
            if (target != null)
            {
                target.localScale = Vector3.one;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovered = false;
            pressed = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            pressed = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            pressed = false;
        }

        private void Update()
        {
            bool active = selectable == null || selectable.IsInteractable();
            float goal = !active ? 1f : pressed ? pressScale : hovered ? hoverScale : 1f;
            // A critically damped spring with a little overshoot.
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            float force = (goal - scale) * 320f - velocity * 22f;
            velocity += force * dt;
            scale += velocity * dt;
            if (target != null)
            {
                target.localScale = new Vector3(scale, scale, 1f);
            }
        }
    }
}
