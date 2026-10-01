using Gamebox;
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
    /// Where a <see cref="RunnerPlayer"/> takes its moves from, read once per frame while it runs: the keys and swipes
    /// of a runner played alone (<see cref="RunnerInput"/>), or the controls of a seat of a local race
    /// (<see cref="SeatInput"/>). Either one takes presses from code too (on-screen buttons, the autopilot).
    /// </summary>
    internal interface IRunnerInput
    {
        bool Left { get; }
        bool Right { get; }
        bool Jump { get; }
        bool Slide { get; }

        /// <summary>Presses a control from code; it is read on the next frame.</summary>
        void Press(RunnerAction action);

        /// <summary>Reads what was pressed this frame.</summary>
        void Read();
    }


    /// <summary>The moves pressed from code since the last read, which every kind of input adds to its own.</summary>
    internal struct QueuedMoves
    {
        private bool left;
        private bool right;
        private bool jump;
        private bool slide;

        public void Press(RunnerAction action)
        {
            switch (action)
            {
                case RunnerAction.Left: left = true; break;
                case RunnerAction.Right: right = true; break;
                case RunnerAction.Jump: jump = true; break;
                case RunnerAction.Slide: slide = true; break;
            }
        }

        /// <summary>Hands out the queued moves and forgets them.</summary>
        public void Take(out bool takeLeft, out bool takeRight, out bool takeJump, out bool takeSlide)
        {
            takeLeft = left;
            takeRight = right;
            takeJump = jump;
            takeSlide = slide;
            left = right = jump = slide = false;
        }
    }


    /// <summary>
    /// Reads the runner's controls once per frame: arrow keys, WASD and space on a keyboard, or swipes with a finger
    /// or the mouse. Presses that start on the interface are ignored.
    /// </summary>
    internal sealed class RunnerInput : IRunnerInput
    {
        /// <summary>Swipe length that counts as a swipe, as a share of the screen height.</summary>
        private const float SwipeThreshold = 0.06f;

        private Vector2 pressPosition;
        private bool tracking;
        private bool consumed;

        private QueuedMoves queued;

        public bool Left { get; private set; }
        public bool Right { get; private set; }
        public bool Jump { get; private set; }
        public bool Slide { get; private set; }

        public void Press(RunnerAction action)
        {
            queued.Press(action);
        }

        public void Read()
        {
            queued.Take(out bool left, out bool right, out bool jump, out bool slide);
            Left = left || Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A);
            Right = right || Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D);
            Jump = jump || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space);
            Slide = slide || Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S);
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


    /// <summary>
    /// The moves of a runner of a local race, from the controls of its seat (<see cref="PlayerControls"/>): the keys,
    /// mouse buttons and pad controls the players bound to <see cref="Left"/>, <see cref="Right"/>, <see cref="Jump"/> and
    /// <see cref="Slide"/> on the controls page. No swipes: several runners share the screen.
    /// </summary>
    internal sealed class SeatInput : IRunnerInput
    {
        public const string LeftAction = "left";
        public const string RightAction = "right";
        public const string JumpAction = "jump";
        public const string SlideAction = "slide";

        private readonly PlayerControls controls;
        private QueuedMoves queued;

        public SeatInput(PlayerControls controls)
        {
            this.controls = controls;
        }

        /// <summary>The actions every runner of a local race binds controls to, with the directions as their defaults.</summary>
        public static ControlScheme Scheme()
        {
            return new ControlScheme()
                .Add(LeftAction, "Left", ControlRole.Left)
                .Add(RightAction, "Right", ControlRole.Right)
                .Add(JumpAction, "Jump", ControlRole.Up)
                .Add(SlideAction, "Slide", ControlRole.Down);
        }

        public bool Left { get; private set; }
        public bool Right { get; private set; }
        public bool Jump { get; private set; }
        public bool Slide { get; private set; }

        public void Press(RunnerAction action)
        {
            queued.Press(action);
        }

        public void Read()
        {
            queued.Take(out bool left, out bool right, out bool jump, out bool slide);
            // Every action is read every frame, so a stick pushed and let go counts once.
            bool leftPressed = controls != null && controls.Pressed(LeftAction);
            bool rightPressed = controls != null && controls.Pressed(RightAction);
            bool jumpPressed = controls != null && controls.Pressed(JumpAction);
            bool slidePressed = controls != null && controls.Pressed(SlideAction);
            Left = left || leftPressed;
            Right = right || rightPressed;
            Jump = jump || jumpPressed;
            Slide = slide || slidePressed;
        }
    }
}
