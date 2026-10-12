using UnityEngine;
using UnityEngine.EventSystems;

namespace Portfolio.Checkers
{
    /// <summary>
    /// Clicks and taps on the board (not on the interface): a tap on a square goes to the manager, which selects a piece
    /// or plays a move with it; the mouse also hovers squares. Nothing is sent while the manager is not waiting for a
    /// person at this device.
    /// </summary>
    public sealed class BoardInput : MonoBehaviour
    {
        [SerializeField] private CheckersGameManager manager;
        [SerializeField] private BoardView board;
        [SerializeField] private Camera view;

        private Vector2 pressPosition;
        private bool pressed;
        private int hovered = -1;

        private void Update()
        {
            if (manager == null || board == null || view == null)
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
                Hover(OverInterface(-1) ? -1 : board.SquareAt(view.ScreenPointToRay(Input.mousePosition)));
            }
        }

        private void Hover(int square)
        {
            if (square == hovered)
            {
                return;
            }
            hovered = square;
            manager.SquareHovered(square);
        }

        private void Click(Vector2 screen)
        {
            int square = board.SquareAt(view.ScreenPointToRay(screen));
            if (square >= 0)
            {
                manager.SquareClicked(square);
            }
        }

        private static bool OverInterface(int pointer)
        {
            EventSystem events = EventSystem.current;
            return events != null && (pointer >= 0 ? events.IsPointerOverGameObject(pointer) : events.IsPointerOverGameObject());
        }
    }
}
