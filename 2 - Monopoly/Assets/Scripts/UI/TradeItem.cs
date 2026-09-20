using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Monopoly
{
    /// <summary>A property that can be ticked into a trade.</summary>
    public class TradeItem : MonoBehaviour
    {
        [SerializeField] private Toggle toggle;
        [SerializeField] private Image colorBar;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text detail;
        [SerializeField] private Image background;

        private Action changed;

        public int Space { get; private set; } = -1;

        public bool IsOn => toggle != null && toggle.isOn;

        private void Awake()
        {
            if (toggle != null)
            {
                toggle.onValueChanged.AddListener(on =>
                {
                    Paint();
                    changed?.Invoke();
                });
            }
        }

        public void Bind(MonopolyMatch match, int space, Action onChanged)
        {
            Space = space;
            changed = null;
            SpaceData data = match.Space(space);
            DeedState deed = match.Deed(space);
            if (toggle != null)
            {
                toggle.isOn = false;
            }
            colorBar.color = MonopolyStyle.GroupColor(data.group);
            nameText.text = data.name;
            detail.text = deed.mortgaged ? $"{MonopolyStyle.Money(data.price)}  <color=#E4002B>mortgaged</color>" : MonopolyStyle.Money(data.price);
            changed = onChanged;
            Paint();
        }

        private void Paint()
        {
            if (background != null)
            {
                background.color = IsOn ? MonopolyStyle.Tint(MonopolyStyle.Gold, 0.55f) : Color.white;
            }
        }
    }
}
