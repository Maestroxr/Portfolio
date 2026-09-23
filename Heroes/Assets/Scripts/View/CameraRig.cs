using Gamebox;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Portfolio.Heroes
{
    /// <summary>
    /// The camera over the map, at the fixed angle of the old adventure maps: it pans with the keys, the edges of the
    /// screen, a drag of the right (or middle) mouse button or of one finger, zooms with the wheel or a pinch, glides to
    /// whatever the game wants shown (<see cref="Focus"/>) and frames a battlefield (<see cref="Frame"/>).
    /// </summary>
    public sealed class CameraRig : MonoBehaviour
    {
        [SerializeField] private Camera view;
        [SerializeField] private float pitch = 52f;
        [SerializeField] private float yaw;
        [SerializeField] private float minDistance = 14f;
        [SerializeField] private float maxDistance = 70f;
        [SerializeField] private float distance = 34f;
        [SerializeField] private float panSpeed = 1.1f;
        [SerializeField] private float edgeSize = 12f;

        private Vector3 target;
        private Vector3 goal;
        private float goalDistance;
        private Bounds limits = new Bounds(Vector3.zero, Vector3.one * 1000f);
        private bool dragging;
        private Vector3 dragStart;
        private Vector3 dragTarget;
        private float pinchStart;
        private float pinchDistance;
        private bool gliding;
        private float glideSpeed = 4f;

        public Camera View => view;

        /// <summary>Whether the pointer drags the map right now (a click that ends a drag is not a click on the map).</summary>
        public bool IsDragging => dragging && (Input.mousePosition - dragStart).sqrMagnitude > 64f;

        /// <summary>Scrolling with the edges of the screen (the settings can turn it off).</summary>
        public bool EdgeScroll { get; set; } = true;

        /// <summary>No control by the player (a battle playing out, a dialog open).</summary>
        public bool Locked { get; set; }

        public Vector3 Target => target;

        public float Distance => distance;

        private void Awake()
        {
            if (view == null)
            {
                view = GetComponentInChildren<Camera>();
            }
            target = transform.position;
            goal = target;
            goalDistance = distance;
            Place();
        }

        public void SetLimits(Bounds bounds)
        {
            limits = bounds;
            target = Clamp(target);
            goal = Clamp(goal);
        }

        /// <summary>Glides to <paramref name="point"/> (and to <paramref name="zoom"/> meters away, when given).</summary>
        public void Focus(Vector3 point, float zoom = -1f, float speed = 4f)
        {
            goal = Clamp(point);
            if (zoom > 0f)
            {
                goalDistance = Mathf.Clamp(zoom, minDistance, maxDistance * 1.2f);
            }
            glideSpeed = speed;
            gliding = true;
        }

        /// <summary>Jumps there at once.</summary>
        public void Snap(Vector3 point, float zoom = -1f)
        {
            target = goal = Clamp(point);
            if (zoom > 0f)
            {
                distance = goalDistance = Mathf.Clamp(zoom, minDistance, maxDistance * 1.2f);
            }
            gliding = false;
            Place();
        }

        /// <summary>Frames an area (a battlefield) so all of it shows.</summary>
        public void Frame(Bounds area)
        {
            float aspect = view != null ? Mathf.Max(0.5f, view.aspect) : 16f / 9f;
            float fov = view != null ? view.fieldOfView : 40f;
            float widthDistance = area.size.x * 0.5f / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad) / aspect * 1.08f;
            float depthDistance = area.size.z * 0.62f / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad) * Mathf.Sin(pitch * Mathf.Deg2Rad);
            Focus(area.center + new Vector3(0f, 0f, -area.size.z * 0.06f), Mathf.Max(widthDistance, depthDistance) + 4f, 3f);
        }

        public bool IsVisible(Vector3 point)
        {
            if (view == null)
            {
                return true;
            }
            Vector3 v = view.WorldToViewportPoint(point);
            return v.z > 0f && v.x > 0.05f && v.x < 0.95f && v.y > 0.05f && v.y < 0.95f;
        }

        private Vector3 Clamp(Vector3 point)
        {
            point.x = Mathf.Clamp(point.x, limits.min.x, limits.max.x);
            point.z = Mathf.Clamp(point.z, limits.min.z, limits.max.z);
            return point;
        }

        private void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            if (!Locked)
            {
                HandleInput(dt);
            }
            if (gliding)
            {
                float t = 1f - Mathf.Exp(-glideSpeed * dt);
                target = Vector3.Lerp(target, goal, t);
                distance = Mathf.Lerp(distance, goalDistance, t);
                if ((target - goal).sqrMagnitude < 0.0025f && Mathf.Abs(distance - goalDistance) < 0.05f)
                {
                    gliding = false;
                }
            }
            else
            {
                distance = Mathf.Lerp(distance, goalDistance, 1f - Mathf.Exp(-10f * dt));
            }
            Place();
        }

        private void HandleInput(float dt)
        {
            bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            Vector3 move = Vector3.zero;
            if (!Input.GetKey(KeyCode.LeftControl))
            {
                move.x += (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);
                move.z += (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1f : 0f);
            }
            if (EdgeScroll && !MobilePlatform.UsesTouch && Application.isFocused && !dragging)
            {
                Vector3 mouse = Input.mousePosition;
                if (mouse.x >= 0 && mouse.y >= 0 && mouse.x <= Screen.width && mouse.y <= Screen.height)
                {
                    if (mouse.x < edgeSize) move.x -= 1f;
                    if (mouse.x > Screen.width - edgeSize) move.x += 1f;
                    if (mouse.y < edgeSize) move.z -= 1f;
                    if (mouse.y > Screen.height - edgeSize) move.z += 1f;
                }
            }
            if (move.sqrMagnitude > 0f)
            {
                gliding = false;
                Vector3 step = Quaternion.Euler(0f, yaw, 0f) * move.normalized * (panSpeed * distance * dt);
                target = goal = Clamp(target + step);
            }
            float wheel = Input.mouseScrollDelta.y;
            if (Mathf.Abs(wheel) > 0.01f && !overUi)
            {
                goalDistance = Mathf.Clamp(goalDistance * (1f - wheel * 0.12f), minDistance, maxDistance);
            }
            if (Input.GetKey(KeyCode.Equals) || Input.GetKey(KeyCode.KeypadPlus))
            {
                goalDistance = Mathf.Clamp(goalDistance * (1f - dt), minDistance, maxDistance);
            }
            if (Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.KeypadMinus))
            {
                goalDistance = Mathf.Clamp(goalDistance * (1f + dt), minDistance, maxDistance);
            }
            HandleDrag(overUi);
            HandlePinch();
        }

        private void HandleDrag(bool overUi)
        {
            bool touch = Input.touchCount == 1;
            bool down = touch ? Input.GetTouch(0).phase == TouchPhase.Began : Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2);
            bool held = touch ? Input.GetTouch(0).phase != TouchPhase.Ended && Input.GetTouch(0).phase != TouchPhase.Canceled : Input.GetMouseButton(1) || Input.GetMouseButton(2);
            Vector3 pointer = touch ? (Vector3)Input.GetTouch(0).position : Input.mousePosition;
            if (down && !overUi)
            {
                dragging = true;
                dragStart = pointer;
                dragTarget = target;
            }
            if (dragging && held)
            {
                Vector3 delta = pointer - dragStart;
                if (delta.sqrMagnitude > 64f)
                {
                    gliding = false;
                    float scale = distance / Mathf.Max(1f, Screen.height) * 1.6f;
                    Vector3 offset = Quaternion.Euler(0f, yaw, 0f) * new Vector3(-delta.x, 0f, -delta.y) * scale;
                    target = goal = Clamp(dragTarget + offset);
                }
            }
            else if (!held)
            {
                dragging = false;
            }
        }

        private void HandlePinch()
        {
            if (Input.touchCount != 2)
            {
                pinchStart = 0f;
                return;
            }
            float spread = (Input.GetTouch(0).position - Input.GetTouch(1).position).magnitude;
            if (pinchStart <= 0f)
            {
                pinchStart = spread;
                pinchDistance = goalDistance;
                return;
            }
            goalDistance = Mathf.Clamp(pinchDistance * pinchStart / Mathf.Max(1f, spread), minDistance, maxDistance);
            dragging = false;
        }

        private void Place()
        {
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            transform.SetPositionAndRotation(target + rotation * new Vector3(0f, 0f, -distance), rotation);
        }

        /// <summary>The ground point under a screen position (the terrain, else the ground plane at the target's height).</summary>
        public bool PointerGround(Vector3 screen, out Vector3 point, int mask = ~0)
        {
            point = default;
            if (view == null)
            {
                return false;
            }
            Ray ray = view.ScreenPointToRay(screen);
            if (Physics.Raycast(ray, out RaycastHit hit, 500f, mask, QueryTriggerInteraction.Ignore))
            {
                point = hit.point;
                return true;
            }
            var plane = new Plane(Vector3.up, new Vector3(0f, target.y, 0f));
            if (plane.Raycast(ray, out float enter))
            {
                point = ray.GetPoint(enter);
                return true;
            }
            return false;
        }
    }
}
