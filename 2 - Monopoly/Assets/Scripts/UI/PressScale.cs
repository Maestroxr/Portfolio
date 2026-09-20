using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Portfolio.Monopoly
{
    /// <summary>Makes a button give a little under the finger or the mouse (and grow a touch on hover).</summary>
    public class PressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private float pressedScale = 0.93f;
        [SerializeField] private float hoverScale = 1.03f;

        private Selectable selectable;
        private bool down;
        private bool over;
        private float scale = 1f;

        private void Awake()
        {
            selectable = GetComponent<Selectable>();
        }

        private void OnDisable()
        {
            down = false;
            over = false;
            scale = 1f;
            transform.localScale = Vector3.one;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            down = Interactable;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            down = false;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            over = Interactable;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            over = false;
            down = false;
        }

        private bool Interactable => selectable == null || selectable.IsInteractable();

        private void Update()
        {
            float target = down ? pressedScale : over ? hoverScale : 1f;
            scale = Mathf.MoveTowards(scale, target, Time.unscaledDeltaTime * 1.6f);
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
