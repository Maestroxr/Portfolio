using Gamebox;
using TMPro;
using UnityEngine;

namespace Portfolio.Heroes.UI
{
    /// <summary>
    /// A label the interface wrote once from an English key (a button's word, a heading): it writes the key again in the
    /// language shown whenever the language changes, also when it was hidden at the time. A label whose words code wrote
    /// over since is the code's to redraw and left alone. <see cref="UIKit.Label"/> puts it on its labels.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TranslatedLabel : MonoBehaviour
    {
        private TMP_Text label;
        private string key;
        private string written;
        private string language;
        private bool sentence;

        public string Key => key;

        /// <summary>
        /// Writes <paramref name="english"/> on <paramref name="text"/> in the language shown, and keeps it so. A
        /// <paramref name="sentence"/> is a line the rules wrote (see <see cref="Words.Sentence"/>) rather than a key.
        /// </summary>
        public static void Write(TMP_Text text, string english, bool sentence = false)
        {
            if (text == null)
            {
                return;
            }
            if (string.IsNullOrEmpty(english))
            {
                text.text = english ?? "";
                return;
            }
            TranslatedLabel keeper = text.GetComponent<TranslatedLabel>();
            if (keeper == null)
            {
                keeper = text.gameObject.AddComponent<TranslatedLabel>();
            }
            keeper.label = text;
            keeper.key = english;
            keeper.sentence = sentence;
            keeper.Apply();
        }

        private void OnEnable()
        {
            GameLanguages.Changed += OnChanged;
            if (label != null && language != null && language != GameLanguages.Code)
            {
                OnChanged();
            }
        }

        private void OnDisable()
        {
            GameLanguages.Changed -= OnChanged;
        }

        private void OnChanged()
        {
            if (this == null || label == null || label.text != written)
            {
                return;
            }
            Apply();
        }

        private void Apply()
        {
            written = sentence ? Words.Sentence(key) : Words.T(key);
            language = GameLanguages.Code;
            label.text = written;
        }
    }
}
