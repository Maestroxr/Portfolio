using System.Collections;
using UnityEngine;

namespace Portfolio.Monopoly
{
    /// <summary>
    /// A panel that pops in (a quick scale with overshoot and a fade) and fades out. The animations run on unscaled
    /// time, so popups work while the game is paused.
    /// </summary>
    public class Popup : MonoBehaviour
    {
        [SerializeField] protected CanvasGroup group;
        [SerializeField] protected RectTransform card;

        private Coroutine animating;
        private bool closing;

        public bool IsOpen => gameObject.activeSelf && !closing;

        /// <summary>Whether opening scales the card up (popups with an entrance of their own turn it off).</summary>
        protected virtual bool PopsCard => true;

        public virtual void Open()
        {
            closing = false;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            if (animating != null)
            {
                StopCoroutine(animating);
            }
            animating = StartCoroutine(In());
        }

        public virtual void Close()
        {
            if (!gameObject.activeSelf || closing)
            {
                return;
            }
            closing = true;
            if (!isActiveAndEnabled)
            {
                gameObject.SetActive(false);
                return;
            }
            if (animating != null)
            {
                StopCoroutine(animating);
            }
            animating = StartCoroutine(Out());
        }

        /// <summary>Hides at once, without the fade (a new match, a loaded one).</summary>
        public void HideNow()
        {
            if (animating != null)
            {
                StopCoroutine(animating);
                animating = null;
            }
            closing = false;
            gameObject.SetActive(false);
        }

        private IEnumerator In()
        {
            if (group != null)
            {
                group.blocksRaycasts = true;
                group.interactable = true;
            }
            yield return Tween.Run(0.22f, t =>
            {
                if (group != null)
                {
                    group.alpha = Mathf.Clamp01(t * 2f);
                }
                if (card != null && PopsCard)
                {
                    float s = Mathf.Lerp(0.86f, 1f, Tween.OutBack(t, 2.2f));
                    card.localScale = new Vector3(s, s, 1f);
                }
            }, true);
            animating = null;
        }

        private IEnumerator Out()
        {
            if (group != null)
            {
                group.blocksRaycasts = false;
                group.interactable = false;
            }
            yield return Tween.Run(0.14f, t =>
            {
                if (group != null)
                {
                    group.alpha = 1f - t;
                }
                if (card != null)
                {
                    float s = Mathf.Lerp(1f, 0.92f, t);
                    card.localScale = new Vector3(s, s, 1f);
                }
            }, true);
            closing = false;
            animating = null;
            gameObject.SetActive(false);
        }
    }
}
