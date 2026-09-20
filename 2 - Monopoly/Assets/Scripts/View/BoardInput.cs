using UnityEngine;
using UnityEngine.EventSystems;

namespace Portfolio.Monopoly
{
    /// <summary>
    /// Clicks and taps on the board (not on the interface): a tap on a space shows its title deed, or picks the space
    /// when triples of the speed die let the player move anywhere. The mouse also hovers spaces while picking.
    /// </summary>
    public class BoardInput : MonoBehaviour
    {
        [SerializeField] private MonopolyGameManager manager;
        [SerializeField] private BoardView board;
        [SerializeField] private Camera view;

        private Vector2 pressPosition;
        private bool pressed;

        private void Update()
        {
            if (manager == null || board == null || view == null || !manager.IsGameRunning)
            {
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
            else if (!OverInterface(-1))
            {
                manager.BoardHovered(board.Raycast(view.ScreenPointToRay(Input.mousePosition)));
            }
        }

        private void Click(Vector2 screen)
        {
            int space = board.Raycast(view.ScreenPointToRay(screen));
            if (space >= 0)
            {
                manager.BoardClicked(space);
            }
        }

        private static bool OverInterface(int pointer)
        {
            EventSystem events = EventSystem.current;
            return events != null && (pointer >= 0 ? events.IsPointerOverGameObject(pointer) : events.IsPointerOverGameObject());
        }
    }
}
