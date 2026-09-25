using System.Collections;
using System.Collections.Generic;
using Gamebox;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Monopoly
{
    /// <summary>
    /// Numbers that pop up over the board or a panel (+$200 when passing GO, -$26 of rent) and banknotes that fly from
    /// one panel to another when money changes hands.
    /// </summary>
    public class FloatingTexts : MonoBehaviour
    {
        [SerializeField] private RectTransform layer;
        [SerializeField] private TMP_Text textTemplate;
        [SerializeField] private Image billTemplate;
        [SerializeField] private Camera worldCamera;

        private readonly Queue<TMP_Text> spareTexts = new Queue<TMP_Text>();
        private readonly Queue<Image> spareBills = new Queue<Image>();

        private void Awake()
        {
            if (textTemplate != null)
            {
                textTemplate.gameObject.SetActive(false);
            }
            if (billTemplate != null)
            {
                billTemplate.gameObject.SetActive(false);
            }
        }

        /// <summary>Pops a text up over a point of the board.</summary>
        public void AtWorld(string text, Color color, Vector3 world, float size = 46f)
        {
            if (worldCamera == null)
            {
                return;
            }
            Vector3 screen = worldCamera.WorldToScreenPoint(world);
            if (screen.z < 0f)
            {
                return;
            }
            Spawn(text, color, ScreenToLayer(screen), size);
        }

        /// <summary>Pops a text up next to an interface element.</summary>
        public void AtRect(string text, Color color, RectTransform target, Vector2 offset, float size = 40f)
        {
            if (target == null)
            {
                return;
            }
            Vector2 local = LayerPoint(target) + offset;
            Spawn(text, color, local, size);
        }

        /// <summary>Banknotes flying from one interface element (or a board point) to another.</summary>
        public void FlyMoney(Vector2 from, Vector2 to, int bills)
        {
            if (billTemplate == null)
            {
                return;
            }
            for (int i = 0; i < bills; i++)
            {
                StartCoroutine(Bill(from, to, i * 0.07f));
            }
        }

        public Vector2 LayerPoint(RectTransform target)
        {
            Vector3 world = target.TransformPoint(target.rect.center);
            Canvas canvas = layer.GetComponentInParent<Canvas>();
            Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(eventCamera, world);
            return ScreenToLayer(screen);
        }

        public Vector2 WorldToLayer(Vector3 world)
        {
            return worldCamera != null ? ScreenToLayer(worldCamera.WorldToScreenPoint(world)) : Vector2.zero;
        }

        private Vector2 ScreenToLayer(Vector2 screen)
        {
            Canvas canvas = layer.GetComponentInParent<Canvas>();
            Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, screen, eventCamera, out Vector2 local);
            return local;
        }

        private void Spawn(string text, Color color, Vector2 at, float size)
        {
            if (textTemplate == null)
            {
                return;
            }
            TMP_Text label = spareTexts.Count > 0 ? spareTexts.Dequeue() : Instantiate(textTemplate, layer);
            label.gameObject.SetActive(true);
            label.text = text;
            label.color = color;
            label.fontSize = size;
            // Keep the whole rise on screen (the panels in the top corners are close to the edge).
            Rect bounds = layer.rect;
            at.y = Mathf.Min(at.y, bounds.yMax - 90f - size);
            at.x = Mathf.Clamp(at.x, bounds.xMin + 120f, bounds.xMax - 120f);
            StartCoroutine(Rise(label, at));
        }

        private IEnumerator Rise(TMP_Text label, Vector2 at)
        {
            RectTransform rect = label.rectTransform;
            yield return Tween.Run(1.3f, t =>
            {
                rect.anchoredPosition = at + new Vector2(0f, 90f * Tween.OutCubic(t));
                float pop = t < 0.15f ? Tween.OutBack(t / 0.15f, 3f) : 1f;
                rect.localScale = new Vector3(pop, pop, 1f);
                Color c = label.color;
                c.a = t < 0.7f ? 1f : 1f - (t - 0.7f) / 0.3f;
                label.color = c;
            }, true);
            label.gameObject.SetActive(false);
            spareTexts.Enqueue(label);
        }

        private IEnumerator Bill(Vector2 from, Vector2 to, float delay)
        {
            yield return Tween.Wait(delay, true);
            Image bill = spareBills.Count > 0 ? spareBills.Dequeue() : Instantiate(billTemplate, layer);
            bill.gameObject.SetActive(true);
            RectTransform rect = bill.rectTransform;
            float spin = Random.Range(-40f, 40f);
            Vector2 control = (from + to) * 0.5f + new Vector2(Random.Range(-80f, 80f), 160f);
            yield return Tween.Run(0.6f, t =>
            {
                float e = Tween.InOutCubic(t);
                Vector2 a = Vector2.Lerp(from, control, e);
                Vector2 b = Vector2.Lerp(control, to, e);
                rect.anchoredPosition = Vector2.Lerp(a, b, e);
                rect.localRotation = Quaternion.Euler(0f, 0f, spin * (1f - e));
                float s = t < 0.8f ? 1f : 1f - (t - 0.8f) / 0.2f * 0.6f;
                rect.localScale = new Vector3(s, s, 1f);
            }, true);
            bill.gameObject.SetActive(false);
            spareBills.Enqueue(bill);
        }
    }
}
