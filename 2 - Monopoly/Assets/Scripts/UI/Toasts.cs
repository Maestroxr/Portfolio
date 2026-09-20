using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Monopoly
{
    /// <summary>
    /// The news feed at the bottom of the screen: a short line for everything that happens (a purchase, rent, a trade),
    /// newest at the bottom; lines fade out after a few seconds, only the last few stay.
    /// </summary>
    public class Toasts : MonoBehaviour
    {
        [SerializeField] private RectTransform list;
        [SerializeField] private CanvasGroup template;
        [SerializeField] private int maxLines = 4;
        [SerializeField] private float lifetime = 5f;

        private readonly List<CanvasGroup> lines = new List<CanvasGroup>();
        private readonly Queue<CanvasGroup> spare = new Queue<CanvasGroup>();

        private void Awake()
        {
            if (template != null)
            {
                template.gameObject.SetActive(false);
            }
        }

        public void Show(string text, Color accent, string icon = null)
        {
            if (template == null || list == null)
            {
                return;
            }
            CanvasGroup line = spare.Count > 0 ? spare.Dequeue() : Instantiate(template, list);
            line.transform.SetAsLastSibling();
            line.gameObject.SetActive(true);
            line.alpha = 0f;
            TMP_Text label = line.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = string.IsNullOrEmpty(icon) ? text : $"<color={MonopolyStyle.ColorTag(accent)}>{icon}</color>  {text}";
            }
            Image bar = line.transform.childCount > 0 ? line.transform.GetChild(0).GetComponent<Image>() : null;
            if (bar != null)
            {
                bar.color = accent;
            }
            lines.Add(line);
            while (lines.Count > maxLines)
            {
                Recycle(lines[0]);
            }
            StartCoroutine(Life(line));
        }

        public void Clear()
        {
            StopAllCoroutines();
            while (lines.Count > 0)
            {
                Recycle(lines[0]);
            }
        }

        private IEnumerator Life(CanvasGroup line)
        {
            yield return Tween.Run(0.2f, t => line.alpha = t, true);
            yield return Tween.Wait(lifetime, true);
            if (!lines.Contains(line))
            {
                yield break;
            }
            yield return Tween.Run(0.5f, t => line.alpha = 1f - t, true);
            if (lines.Contains(line))
            {
                Recycle(line);
            }
        }

        private void Recycle(CanvasGroup line)
        {
            lines.Remove(line);
            line.gameObject.SetActive(false);
            spare.Enqueue(line);
        }
    }
}
