using UnityEngine;
using UnityEngine.EventSystems;

namespace Portfolio.EndlessRunner
{
    /// <summary>The four moves of the runner.</summary>
    public enum RunnerAction
    {
        Left,
        Right,
        Jump,
        Slide
    }


    /// <summary>
    /// Reads the runner's controls once per frame: arrow keys, WASD and space on a keyboard, or swipes with a finger
    /// or the mouse. Presses that start on the interface are ignored.
    /// </summary>
    internal sealed class RunnerInput
    {
        /// <summary>Swipe length that counts as a swipe, as a share of the screen height.</summary>
        private const float SwipeThreshold = 0.06f;

        private Vector2 pressPosition;
        private bool tracking;
        private bool consumed;

        private bool queuedLeft;
        private bool queuedRight;
        private bool queuedJump;
        private bool queuedSlide;

        public bool Left { get; private set; }
        public bool Right { get; private set; }
        public bool Jump { get; private set; }
        public bool Slide { get; private set; }

        /// <summary>Presses a control from code (on-screen buttons, the autopilot); it is read on the next frame.</summary>
        public void Press(RunnerAction action)
        {
            switch (action)
            {
                case RunnerAction.Left: queuedLeft = true; break;
                case RunnerAction.Right: queuedRight = true; break;
                case RunnerAction.Jump: queuedJump = true; break;
                case RunnerAction.Slide: queuedSlide = true; break;
            }
        }

        public void Read()
        {
            Left = queuedLeft || Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A);
            Right = queuedRight || Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D);
            Jump = queuedJump || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space);
            Slide = queuedSlide || Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S);
            queuedLeft = queuedRight = queuedJump = queuedSlide = false;
            ReadSwipe();
        }

        private void ReadSwipe()
        {
            Vector2 position;
            bool pressed;
            bool released;
            int pointerId = -1;
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                position = touch.position;
                pressed = touch.phase == TouchPhase.Began;
                released = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
                pointerId = touch.fingerId;
            }
            else
            {
                position = Input.mousePosition;
                pressed = Input.GetMouseButtonDown(0);
                released = Input.GetMouseButtonUp(0);
            }

            if (pressed)
            {
                EventSystem events = EventSystem.current;
                tracking = events == null || !events.IsPointerOverGameObject(pointerId);
                consumed = false;
                pressPosition = position;
                return;
            }
            if (tracking && !consumed)
            {
                Vector2 delta = position - pressPosition;
                if (delta.magnitude >= SwipeThreshold * Screen.height)
                {
                    consumed = true;
                    if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                    {
                        Left |= delta.x < 0f;
                        Right |= delta.x > 0f;
                    }
                    else
                    {
                        Jump |= delta.y > 0f;
                        Slide |= delta.y < 0f;
                    }
                }
            }
            if (released)
            {
                tracking = false;
            }
        }
    }
}
