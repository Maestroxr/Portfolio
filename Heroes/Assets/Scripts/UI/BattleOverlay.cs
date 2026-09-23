using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Heroes.UI
{
    /// <summary>
    /// Black over the whole screen, faded in and out while the game changes scenes under it (a battlefield of its own
    /// coming up, the map coming back). It catches every click while it covers anything.
    /// </summary>
    public sealed class BattleFade : MonoBehaviour
    {
        private CanvasGroup group;
        private Image black;

        public static BattleFade Make(RectTransform parent)
        {
            Image image = UIKit.Sprite(parent, "Fade", null, Color.black);
            UIKit.Stretch((RectTransform)image.transform);
            var fade = image.gameObject.AddComponent<BattleFade>();
            fade.black = image;
            fade.group = image.gameObject.AddComponent<CanvasGroup>();
            fade.group.alpha = 0f;
            image.gameObject.SetActive(false);
            return fade;
        }

        public float Alpha => group != null ? group.alpha : 0f;

        /// <summary>Fades to <paramref name="alpha"/> (1 black, 0 clear) over <paramref name="seconds"/> of real time.</summary>
        public IEnumerator To(float alpha, float seconds)
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            black.raycastTarget = true;
            float from = group.alpha;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / Mathf.Max(0.01f, seconds);
                group.alpha = Mathf.Lerp(from, alpha, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)));
                yield return null;
            }
            group.alpha = alpha;
            if (alpha <= 0.001f)
            {
                gameObject.SetActive(false);
            }
        }

        public void Clear()
        {
            group.alpha = 0f;
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// The call to arms at the start of a battle: a crimson ribbon across the top of the field with who fights whom
    /// under it, held a moment and faded away.
    /// </summary>
    public sealed class BattleBanner : MonoBehaviour
    {
        private CanvasGroup group;
        private TextMeshProUGUI title;
        private TextMeshProUGUI matchup;
        private Coroutine showing;

        public static BattleBanner Make(RectTransform parent)
        {
            RectTransform holder = UIKit.Rect(parent, "Battle Banner");
            UIKit.Pin(holder, new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(1100f, 170f));
            var banner = holder.gameObject.AddComponent<BattleBanner>();
            banner.group = holder.gameObject.AddComponent<CanvasGroup>();
            banner.group.blocksRaycasts = false;
            Image ribbon = UIKit.Ribbon(holder, "Ribbon", "To Battle!", 44f);
            UIKit.Pin((RectTransform)ribbon.transform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(620f, 112f));
            banner.title = ribbon.GetComponentInChildren<TextMeshProUGUI>();
            banner.matchup = UIKit.Label(holder, "Matchup", "", 30f, UIKit.Ink, TextAlignmentOptions.Center);
            UIKit.Look(banner.matchup, TextLook.Shadow);
            banner.matchup.textWrappingMode = TextWrappingModes.NoWrap;
            UIKit.Pin((RectTransform)banner.matchup.transform, new Vector2(0.5f, 1f), new Vector2(0f, -116f), new Vector2(1100f, 44f));
            holder.gameObject.SetActive(false);
            return banner;
        }

        public void Show(string heading, string line)
        {
            title.text = heading;
            matchup.text = line;
            gameObject.SetActive(true);
            if (showing != null)
            {
                StopCoroutine(showing);
            }
            showing = StartCoroutine(Run());
        }

        public void Hide()
        {
            if (showing != null)
            {
                StopCoroutine(showing);
                showing = null;
            }
            gameObject.SetActive(false);
        }

        private IEnumerator Run()
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / 0.35f;
                group.alpha = Mathf.Clamp01(t);
                transform.localScale = Vector3.one * Mathf.Lerp(1.08f, 1f, Mathf.Clamp01(t));
                yield return null;
            }
            yield return new WaitForSecondsRealtime(1.9f);
            t = 1f;
            while (t > 0f)
            {
                t -= Time.unscaledDeltaTime / 0.6f;
                group.alpha = Mathf.Clamp01(t);
                yield return null;
            }
            gameObject.SetActive(false);
            showing = null;
        }
    }

    /// <summary>
    /// A question before a step that cannot be taken back, such as a retreat, which leaves the hero's whole army on the
    /// field: a small window over the battle with the step and a way back, as in the old games. Enter (or the key of the
    /// step itself, pressed again) takes it; Escape, the other button or a click beside the window does not.
    /// </summary>
    public sealed class BattleQuestion : MonoBehaviour
    {
        private const float Width = 760f;
        private const float Height = 330f;

        private RectTransform window;
        private TextMeshProUGUI heading;
        private TextMeshProUGUI question;
        private TextMeshProUGUI yesLabel;
        private CanvasGroup group;
        private Action yes;
        private KeyCode key;
        private int askedOnFrame;
        private float shownAt;

        public bool IsOpen => gameObject.activeSelf;

        public static BattleQuestion Make(RectTransform parent)
        {
            RectTransform holder = UIKit.Rect(parent, "Battle Question");
            UIKit.Stretch(holder);
            var box = holder.gameObject.AddComponent<BattleQuestion>();
            box.group = holder.gameObject.AddComponent<CanvasGroup>();
            Image shade = UIKit.Shade(holder, "Shade", 0.55f);
            shade.raycastTarget = true;
            Button beside = shade.gameObject.AddComponent<Button>();
            beside.transition = Selectable.Transition.None;
            beside.navigation = new Navigation { mode = Navigation.Mode.None };
            beside.onClick.AddListener(box.Close);

            Image frame = UIKit.Frame(holder, "Window");
            box.window = (RectTransform)frame.transform;
            UIKit.Pin(box.window, new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(Width, Height));
            RectTransform content = UIKit.Content(frame, 18f);

            Image ribbon = UIKit.Ribbon(box.window, "Title", "", 34f);
            UIKit.Pin((RectTransform)ribbon.transform, new Vector2(0.5f, 1f), new Vector2(0f, 36f), new Vector2(460f, 92f));
            box.heading = ribbon.GetComponentInChildren<TextMeshProUGUI>();

            box.question = UIKit.Label(content, "Question", "", 25f, UIKit.Ink, TextAlignmentOptions.Center);
            UIKit.Pin((RectTransform)box.question.transform, new Vector2(0.5f, 1f), new Vector2(0f, -56f), new Vector2(Width - 110f, 130f));

            Button take = UIKit.Push(content, "Yes", "Retreat", box.Take, 26f);
            UIKit.Pin((RectTransform)take.transform, new Vector2(0.5f, 0f), new Vector2(-150f, 10f), new Vector2(260f, 62f));
            box.yesLabel = take.GetComponentInChildren<TextMeshProUGUI>();
            Button back = UIKit.Push(content, "No", "Fight On", box.Close, 26f);
            UIKit.Pin((RectTransform)back.transform, new Vector2(0.5f, 0f), new Vector2(150f, 10f), new Vector2(260f, 62f));
            holder.gameObject.SetActive(false);
            return box;
        }

        /// <summary>
        /// Puts the question: <paramref name="title"/> on the ribbon, <paramref name="text"/> under it, and a button
        /// <paramref name="answer"/> that takes the step (<paramref name="onYes"/>), as does <paramref name="shortcut"/>.
        /// </summary>
        public void Ask(string title, string text, string answer, KeyCode shortcut, Action onYes)
        {
            heading.text = title;
            question.text = text;
            yesLabel.text = answer;
            yes = onYes;
            key = shortcut;
            // The key that asked the question is still down this frame: it does not answer it too.
            askedOnFrame = Time.frameCount;
            shownAt = Time.unscaledTime;
            group.alpha = 0f;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        /// <summary>Takes the question away without the step.</summary>
        public void Close()
        {
            yes = null;
            gameObject.SetActive(false);
        }

        private void Take()
        {
            Action then = yes;
            Close();
            then?.Invoke();
        }

        private void Update()
        {
            float t = Mathf.Clamp01((Time.unscaledTime - shownAt) / 0.2f);
            group.alpha = t;
            window.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, 1f - (1f - t) * (1f - t));
            if (Time.frameCount > askedOnFrame &&
                (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || key != KeyCode.None && Input.GetKeyDown(key)))
            {
                Take();
            }
        }
    }
}
