using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Portfolio.MemoryCards
{
    /// <summary>
    /// The view of one card: back and face, the flip, deal and shuffle moves, the pop of a match, the shake of a
    /// mistake, the ice on a frozen card and the explosion of a bomb. It only animates; the manager decides what a
    /// click (<see cref="Clicked"/>) does through <see cref="MemoryRound"/>. Animations run on game time, so they stop
    /// while the game is paused.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class Flippable : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        /// <summary>A timer for one animation: waits <c>delay</c> seconds, then runs from 0 to 1 over <c>duration</c>.</summary>
        private struct Tween
        {
            private float t;
            private float delay;
            private float duration;

            public bool Active => t >= 0f;

            public bool Running => t >= 0f && delay <= 0f;

            public float Progress => Mathf.Clamp01(t);

            public void Start(float wait, float length)
            {
                t = 0f;
                delay = wait;
                duration = Mathf.Max(0.01f, length);
            }

            public void Stop()
            {
                t = -1f;
            }

            /// <summary>Advances the timer. Returns true on the frame it finishes.</summary>
            public bool Tick(float dt)
            {
                if (t < 0f)
                {
                    return false;
                }
                if (delay > 0f)
                {
                    delay -= dt;
                    return false;
                }
                t += dt / duration;
                if (t >= 1f)
                {
                    t = -1f;
                    return true;
                }
                return false;
            }
        }

        [SerializeField] internal RectTransform body;
        [SerializeField] internal Image shadow;
        [SerializeField] internal Image back;
        [SerializeField] internal Image front;
        [SerializeField] internal Image face;
        [SerializeField] internal Image badge;
        [SerializeField] internal Image ice;
        [SerializeField] internal Image glow;
        [SerializeField] internal CanvasGroup group;
        [SerializeField] internal Sprite iceSprite;
        [SerializeField] internal Sprite crackedIceSprite;
        [SerializeField] internal float flipDuration = 0.26f;
        [SerializeField] internal Color mistakeTint = new Color(1f, 0.55f, 0.55f);
        [SerializeField] internal Color matchGlow = new Color(1f, 0.85f, 0.25f);
        [SerializeField] internal Color wiltTint = new Color(0.62f, 0.62f, 0.68f);

        private RectTransform rect;
        private Vector2 position;
        private bool faceUp;
        private bool frozen;
        private bool cracked;
        private bool hovered;
        private float hover;

        private Tween flip;
        private bool flipToFace;
        private bool flipSwapped;

        private Tween move;
        private Vector2 moveFrom;
        private float moveArc;
        private float moveSpin;

        private Tween pop;
        private float popAmount;
        private Tween shake;
        private Tween wobble;
        private Tween tint;
        private Color tintColor = Color.white;
        private bool tintHold;
        private bool tintHeld;
        private Tween explode;
        private Tween celebrate;
        private Tween badgeIn;

        private float glowAlpha;
        private float glowTarget;
        private bool glowFlash;
        private Color glowColor = Color.white;

        private float alpha = 1f;
        private float alphaTarget = 1f;
        private float alphaDelay;

        /// <summary>Raised when the card is clicked, whatever its state.</summary>
        public event Action<Flippable> Clicked;

        /// <summary>The index of the card in the round (<see cref="MemoryCard.Id"/>).</summary>
        public int Index { get; private set; }

        public bool Interactable { get; set; } = true;

        public bool IsFaceUp => faceUp;

        /// <summary>Whether a flip, move or explosion is still playing.</summary>
        public bool IsAnimating => flip.Active || move.Active || explode.Active;

        public RectTransform Rect => rect != null ? rect : rect = (RectTransform)transform;

        /// <summary>Where the card rests (its slot, or where it is flying to).</summary>
        public Vector2 Position => position;

        private void Awake()
        {
            rect = (RectTransform)transform;
        }

        /// <summary>Puts the card face down at <paramref name="slotPosition"/> with its sprites and ice, every animation stopped.</summary>
        public void Setup(int index, Sprite backSprite, Sprite faceSprite, bool isFrozen, Vector2 size, Vector2 slotPosition)
        {
            Index = index;
            Rect.sizeDelta = size;
            position = slotPosition;
            Rect.anchoredPosition = slotPosition;
            Rect.localRotation = Quaternion.identity;
            Rect.localScale = Vector3.one;
            if (back != null)
            {
                back.sprite = backSprite;
            }
            if (face != null)
            {
                face.sprite = faceSprite;
                face.preserveAspect = true;
            }
            frozen = isFrozen;
            cracked = false;
            hovered = false;
            hover = 0f;
            flip.Stop();
            move.Stop();
            pop.Stop();
            shake.Stop();
            wobble.Stop();
            tint.Stop();
            explode.Stop();
            celebrate.Stop();
            badgeIn.Stop();
            flipSwapped = true;
            flipToFace = false;
            tintHold = false;
            tintHeld = false;
            tintColor = Color.white;
            glowAlpha = glowTarget = 0f;
            glowFlash = false;
            alpha = alphaTarget = 1f;
            alphaDelay = 0f;
            Interactable = true;
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
            ShowSide(false);
            SetBadge(false);
            Apply(0f);
        }

        /// <summary>Changes the card's size and slot without animation (the board was laid out again).</summary>
        /// <summary>Gives the card its face later than <see cref="Setup"/>: an online game learns a face when the card turns.</summary>
        public void SetFace(Sprite faceSprite)
        {
            if (face != null)
            {
                face.sprite = faceSprite;
                face.preserveAspect = true;
            }
        }

        public void Place(Vector2 size, Vector2 slotPosition)
        {
            Rect.sizeDelta = size;
            position = slotPosition;
            move.Stop();
            Apply(0f);
        }

        /// <summary>Shows the card in a saved state without animation.</summary>
        public void SetStateInstant(CardState state, bool isFrozen)
        {
            frozen = isFrozen;
            cracked = false;
            switch (state)
            {
                case CardState.Matched:
                    ShowSide(true);
                    SetBadge(true);
                    alpha = alphaTarget = 0.62f;
                    break;
                case CardState.Spent:
                    gameObject.SetActive(false);
                    return;
                default:
                    ShowSide(false);
                    SetBadge(false);
                    alpha = alphaTarget = 1f;
                    break;
            }
            Apply(0f);
        }

        public void FlipUp(float delay = 0f)
        {
            StartFlip(true, delay);
        }

        public void FlipDown(float delay = 0f)
        {
            StartFlip(false, delay);
        }

        private void StartFlip(bool toFace, float delay)
        {
            flipToFace = toFace;
            flipSwapped = false;
            flip.Start(delay, flipDuration);
            if (!toFace)
            {
                glowTarget = 0f;
            }
        }

        /// <summary>Flies the card in from <paramref name="from"/> to its slot, spinning, after <paramref name="delay"/>.</summary>
        public void DealFrom(Vector2 from, float delay, float duration)
        {
            Rect.anchoredPosition = from;
            StartMove(from, position, delay, duration, 70f, UnityEngine.Random.Range(-220f, 220f));
            alpha = 0f;
            alphaTarget = 1f;
            alphaDelay = delay;
            Apply(0f);
        }

        /// <summary>Moves the card to a new slot along an arc (a shuffle).</summary>
        public void MoveTo(Vector2 slotPosition, float delay, float duration, float arc = 110f, float spin = 360f)
        {
            StartMove(CurrentPosition(), slotPosition, delay, duration, arc, spin);
        }

        /// <summary>Flies the card off to <paramref name="to"/> and fades it out (the board is cleared).</summary>
        public void Leave(Vector2 to, float delay, float duration)
        {
            StartMove(CurrentPosition(), to, delay, duration, 90f, UnityEngine.Random.Range(-260f, 260f));
            alphaTarget = 0f;
            alphaDelay = delay + duration * 0.4f;
            Interactable = false;
        }

        private void StartMove(Vector2 from, Vector2 to, float delay, float duration, float arc, float spin)
        {
            moveFrom = from;
            position = to;
            moveArc = arc;
            moveSpin = spin;
            move.Start(delay, duration);
        }

        private Vector2 CurrentPosition()
        {
            if (!move.Active)
            {
                return position;
            }
            float e = Gamebox.Tween.OutCubic(move.Progress);
            return Vector2.LerpUnclamped(moveFrom, position, e);
        }

        /// <summary>A completed set: pop, a golden flash and a badge, then the card settles half transparent.</summary>
        public void ShowMatched(float delay)
        {
            pop.Start(delay, 0.45f);
            popAmount = 0.28f;
            glowColor = matchGlow;
            glowTarget = 1f;
            glowFlash = true;
            alphaTarget = 0.62f;
            alphaDelay = delay + 0.6f;
            SetBadge(true);
            badgeIn.Start(delay + 0.15f, 0.3f);
            if (badge != null)
            {
                badge.transform.localScale = Vector3.zero;
            }
        }

        /// <summary>A wrong set: the cards shake and blush.</summary>
        public void ShowMistake(float delay)
        {
            shake.Start(delay, 0.4f);
            tint.Start(delay, 0.7f);
            tintColor = mistakeTint;
            tintHold = false;
        }

        /// <summary>Outlines the card (part of the set being built).</summary>
        public void Highlight(Color color)
        {
            glowColor = color;
            glowTarget = 0.8f;
            glowFlash = false;
        }

        public void Pop(float amount = 0.18f, float delay = 0f)
        {
            pop.Start(delay, 0.45f);
            popAmount = amount;
        }

        /// <summary>The first tap on a frozen card: the ice cracks and the card wobbles.</summary>
        public void Crack()
        {
            cracked = true;
            frozen = false;
            UpdateIce();
            wobble.Start(0f, 0.35f);
            Pop(0.08f);
        }

        /// <summary>A bomb goes off: the card swells, fades and disappears.</summary>
        public void Explode(float delay)
        {
            explode.Start(delay, 0.32f);
            Interactable = false;
        }

        /// <summary>A used up special card pops and fades away.</summary>
        public void Vanish(float delay)
        {
            Pop(0.3f, delay);
            alphaTarget = 0f;
            alphaDelay = delay + 0.35f;
            Interactable = false;
        }

        /// <summary>The board is cleared: the card hops.</summary>
        public void Celebrate(float delay)
        {
            celebrate.Start(delay, 0.5f);
        }

        /// <summary>The round is lost: the card greys out.</summary>
        public void Wilt(float delay)
        {
            tint.Start(delay, 0.5f);
            tintColor = wiltTint;
            tintHold = true;
            Interactable = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                Clicked?.Invoke(this);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovered = false;
        }

        private void OnDisable()
        {
            hovered = false;
        }

        private void Update()
        {
            Apply(Time.deltaTime);
        }

        private void Apply(float dt)
        {
            bool flipDone = flip.Tick(dt);
            if (!flipSwapped && (flipDone || (flip.Running && flip.Progress >= 0.5f)))
            {
                flipSwapped = true;
                ShowSide(flipToFace);
            }
            move.Tick(dt);
            pop.Tick(dt);
            shake.Tick(dt);
            wobble.Tick(dt);
            celebrate.Tick(dt);
            badgeIn.Tick(dt);
            if (tint.Tick(dt) && tintHold)
            {
                tintHeld = true;
            }
            if (explode.Tick(dt))
            {
                gameObject.SetActive(false);
                return;
            }
            if (alphaDelay > 0f)
            {
                alphaDelay -= dt;
            }
            else
            {
                alpha = Mathf.MoveTowards(alpha, alphaTarget, dt * 3f);
            }
            glowAlpha = Mathf.MoveTowards(glowAlpha, glowTarget, dt * (glowTarget > glowAlpha ? 6f : 1.6f));
            if (glowFlash && glowAlpha >= glowTarget)
            {
                glowFlash = false;
                glowTarget = 0f;
            }
            bool canHover = hovered && Interactable && !faceUp;
            hover = Mathf.MoveTowards(hover, canHover ? 1f : 0f, dt * 8f);

            // Position: the move along its arc, the hover lift, the mistake shake and the celebration hop.
            Vector2 anchored = CurrentPosition();
            float spin = 0f;
            if (move.Active)
            {
                float e = Gamebox.Tween.OutCubic(move.Progress);
                anchored += Vector2.up * (moveArc * Mathf.Sin(Mathf.PI * e));
                spin = moveSpin * (1f - e);
            }
            anchored.y += hover * 6f;
            if (shake.Running)
            {
                float t = shake.Progress;
                anchored.x += Mathf.Sin(t * 42f) * 9f * (1f - t);
            }
            if (celebrate.Running)
            {
                anchored.y += Mathf.Sin(celebrate.Progress * Mathf.PI) * 28f;
            }
            if (wobble.Running)
            {
                float t = wobble.Progress;
                spin += Mathf.Sin(t * 30f) * 7f * (1f - t);
            }
            Rect.anchoredPosition = anchored;
            Rect.localRotation = Quaternion.Euler(0f, 0f, spin);

            // Scale: flip squash, pop, hover and explosion.
            float scaleX = 1f;
            float scaleY = 1f;
            if (flip.Running)
            {
                float t = flip.Progress;
                scaleX = Mathf.Abs(Mathf.Cos(t * Mathf.PI));
                scaleY = 1f + 0.08f * Mathf.Sin(t * Mathf.PI);
            }
            float popScale = 1f;
            if (pop.Running)
            {
                float t = pop.Progress;
                popScale = 1f + popAmount * (1f - t) * (1f - t) * Mathf.Cos(t * Mathf.PI * 3f);
            }
            float grow = 1f + hover * 0.05f;
            if (explode.Running)
            {
                grow *= 1f + explode.Progress * 0.6f;
            }
            if (body != null)
            {
                body.localScale = new Vector3(scaleX * popScale * grow, scaleY * popScale * grow, 1f);
            }
            if (shadow != null)
            {
                float lift = hover + (flip.Running ? 0.6f : 0f) + (celebrate.Running ? 1f : 0f) + (move.Active ? 1.2f : 0f);
                shadow.rectTransform.anchoredPosition = new Vector2(0f, -7f - lift * 8f);
                shadow.rectTransform.localScale = new Vector3(scaleX * popScale * grow, popScale * grow, 1f);
            }

            Color color = Color.white;
            if (tintHeld)
            {
                color = tintColor;
            }
            else if (tint.Running)
            {
                float t = tint.Progress;
                color = tintHold ? Color.Lerp(Color.white, tintColor, t) : Color.Lerp(tintColor, Color.white, t);
            }
            if (front != null)
            {
                front.color = color;
            }
            if (face != null)
            {
                face.color = color;
            }
            if (back != null)
            {
                back.color = color;
            }

            if (glow != null)
            {
                Color glowing = glowColor;
                glowing.a = glowAlpha * (0.78f + 0.22f * Mathf.Sin(Time.unscaledTime * 6f));
                glow.color = glowing;
                glow.enabled = glowAlpha > 0.01f;
            }

            if (badge != null && badge.gameObject.activeSelf && badgeIn.Active)
            {
                float t = badgeIn.Running ? badgeIn.Progress : 0f;
                float s = t <= 0f ? 0f : 1f + 0.35f * Mathf.Sin(t * Mathf.PI) * (1f - t);
                badge.transform.localScale = new Vector3(s * Mathf.Min(1f, t * 3f), s * Mathf.Min(1f, t * 3f), 1f);
            }
            else if (badge != null && badge.gameObject.activeSelf && badge.transform.localScale.x < 1f)
            {
                badge.transform.localScale = Vector3.one;
            }

            float visible = alpha;
            if (explode.Running)
            {
                visible *= 1f - explode.Progress;
            }
            if (group != null)
            {
                group.alpha = visible;
                group.blocksRaycasts = visible > 0.05f;
            }
        }

        private void ShowSide(bool showFace)
        {
            faceUp = showFace;
            if (back != null)
            {
                back.gameObject.SetActive(!showFace);
            }
            if (front != null)
            {
                front.gameObject.SetActive(showFace);
            }
            UpdateIce();
        }

        private void SetBadge(bool visible)
        {
            if (badge != null)
            {
                badge.gameObject.SetActive(visible);
                badge.transform.localScale = Vector3.one;
            }
        }

        private void UpdateIce()
        {
            if (ice == null)
            {
                return;
            }
            bool show = !faceUp && (frozen || cracked);
            if (ice.gameObject.activeSelf != show)
            {
                ice.gameObject.SetActive(show);
            }
            if (show)
            {
                ice.sprite = frozen ? iceSprite : crackedIceSprite;
            }
            if (faceUp)
            {
                cracked = false;
            }
        }
    }
}
