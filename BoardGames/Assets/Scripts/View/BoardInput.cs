using UnityEngine;
using UnityEngine.EventSystems;

namespace Portfolio.BoardGames
{
    /// <summary>
    /// Clicks and taps on the board (not on the interface): the manager says what is under the pointer
    /// (<see cref="BoardGamesManager.PickTarget"/>: a square, or a point of the backgammon board) and the game played
    /// reacts to a click or a tap on it (<see cref="BoardGamesManager.TargetClicked"/>, -1 for a click on nothing); the
    /// mouse also hovers targets. Nothing is sent while the manager is not waiting for a person at this device.
    /// </summary>
    public sealed class BoardInput : MonoBehaviour
    {
        [SerializeField] private BoardGamesManager manager;
        [Tooltip("The board of the square games; the manager picks the targets of every game (see BoardGamesManager.PickTarget).")]
        [SerializeField] private BoardView board;
        [SerializeField] private Camera view;

        private Vector2 pressPosition;
        private bool pressed;
        private int hovered = -1;

        private void Update()
        {
            if (manager == null || view == null)
            {
                return;
            }
            if (!manager.WaitingForHuman)
            {
                pressed = false;
                Hover(-1);
                return;
            }
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    pressed = !OverInterface(touch.fingerId);
                    pressPosition = touch.position;
                }
                else if (touch.phase == TouchPhase.Ended && pressed)
                {
                    pressed = false;
                    if ((touch.position - pressPosition).magnitude < 30f)
                    {
                        Click(touch.position);
                    }
                }
                return;
            }
            if (Input.GetMouseButtonDown(0))
            {
                pressed = !OverInterface(-1);
                pressPosition = Input.mousePosition;
            }
            else if (Input.GetMouseButtonUp(0) && pressed)
            {
                pressed = false;
                if (((Vector2)Input.mousePosition - pressPosition).magnitude < 12f)
                {
                    Click(Input.mousePosition);
                }
            }
            else
            {
                Hover(OverInterface(-1) ? -1 : manager.PickTarget(view.ScreenPointToRay(Input.mousePosition)));
            }
        }

        private void Hover(int target)
        {
            if (target == hovered)
            {
                return;
            }
            hovered = target;
            manager.TargetHovered(target);
        }

        private void Click(Vector2 screen)
        {
            manager.TargetClicked(manager.PickTarget(view.ScreenPointToRay(screen)));
        }

        private static bool OverInterface(int pointer)
        {
            EventSystem events = EventSystem.current;
            return events != null && (pointer >= 0 ? events.IsPointerOverGameObject(pointer) : events.IsPointerOverGameObject());
        }
    }
}
