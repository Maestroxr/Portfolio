using Gamebox;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Monopoly
{
    /// <summary>Slides the knob of a switch-style toggle to the side it is set to.</summary>
    public class SwitchKnob : MonoBehaviour
    {
        [SerializeField] private RectTransform knob;
        [SerializeField] private Toggle toggle;
        [SerializeField] private float offX = -46f;
        [SerializeField] private float onX = -4f;

        private float position = -1f;

        private void Update()
        {
            if (knob == null || toggle == null)
            {
                return;
            }
            float target = toggle.isOn ? 1f : 0f;
            position = position < 0f ? target : Mathf.MoveTowards(position, target, Time.unscaledDeltaTime * 8f);
            knob.anchoredPosition = new Vector2(Mathf.Lerp(offX, onX, Tween.InOutCubic(position)), 0f);
        }
    }
}
