using UnityEngine;

namespace Portfolio.Monopoly
{
    /// <summary>
    /// Frames the board. In play the whole board fits into the part of the screen the interface leaves free (between
    /// the player panels), whatever the screen's shape; <see cref="Focus"/> leans the view towards a token while it
    /// moves. On the title and setup screens the camera circles slowly over the board.
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        public enum Mode { Title, Overview }

        [SerializeField] private Camera view;
        [SerializeField] private BoardView board;
        [SerializeField] private float pitch = 56f;
        [SerializeField] private float titlePitch = 38f;
        [SerializeField] private float smoothTime = 0.55f;
        [Tooltip("How far a full focus moves the view towards the point, as a fraction of the way from the board's centre.")]
        [SerializeField] private float focusLean = 0.2f;
        [Tooltip("How much closer a full focus brings the camera, as a fraction of the overview distance.")]
        [SerializeField] private float focusZoom = 0.1f;
        [Tooltip("Part of the screen the board may use in play: x/y = lower left, z/w = upper right, as fractions of the screen.")]
        [SerializeField] private Vector4 playArea = new Vector4(0.2f, 0.03f, 0.8f, 0.93f);

        private Mode mode = Mode.Title;
        private Vector3 focusPoint;
        private float focusWeight;
        private float focusTarget;
        private float yaw;
        private float currentPitch;
        private float distance;
        private Vector3 target;
        private Vector3 targetVelocity;
        private float distanceVelocity;
        private float pitchVelocity;
        private float yawVelocity;
        private float fitDistance = 20f;
        private Vector2 fitShift;
        private Vector2 shift;
        private Vector2 shiftVelocity;
        private Vector2Int fitScreen;
        private Vector4 fitArea;
        private bool snap = true;

        public Camera View => view;

        public void SetMode(Mode value, bool immediate = false)
        {
            mode = value;
            snap |= immediate;
        }

        /// <summary>The fraction of the screen (lower left, upper right) the board should fit into in play.</summary>
        public void SetPlayArea(Vector4 area)
        {
            if (area != playArea)
            {
                playArea = area;
                fitScreen = Vector2Int.zero;
            }
        }

        /// <summary>Leans the view towards <paramref name="point"/> (0 = the whole board, 1 = fully on the point).</summary>
        public void Focus(Vector3 point, float weight)
        {
            focusPoint = point;
            focusTarget = Mathf.Clamp01(weight);
        }

        public void ClearFocus()
        {
            focusTarget = 0f;
        }

        private void LateUpdate()
        {
            if (view == null || board == null)
            {
                return;
            }
            Vector3 center = board.transform.TransformPoint(new Vector3(0f, board.Surface, 0f));
            float desiredPitch;
            float desiredYaw;
            float desiredDistance;
            Vector3 desiredTarget;
            Vector2 desiredShift = Vector2.zero;
            if (mode == Mode.Title)
            {
                desiredPitch = titlePitch;
                desiredYaw = Mathf.Sin(Time.unscaledTime * 0.07f) * 35f;
                desiredDistance = board.Side * 1.05f;
                desiredTarget = center + new Vector3(0f, 0f, board.Side * 0.08f);
            }
            else
            {
                desiredPitch = pitch;
                desiredYaw = 0f;
                desiredDistance = FitDistance(center);
                desiredShift = fitShift;
                focusWeight = Mathf.MoveTowards(focusWeight, focusTarget, Time.unscaledDeltaTime * 1.5f);
                desiredTarget = Vector3.Lerp(center, new Vector3(focusPoint.x, center.y, focusPoint.z), focusWeight * focusLean);
                desiredDistance *= 1f - focusZoom * focusWeight;
            }
            if (snap)
            {
                snap = false;
                target = desiredTarget;
                distance = desiredDistance;
                currentPitch = desiredPitch;
                yaw = desiredYaw;
                shift = desiredShift;
            }
            else
            {
                float dt = Time.unscaledDeltaTime;
                target = Vector3.SmoothDamp(target, desiredTarget, ref targetVelocity, smoothTime, Mathf.Infinity, dt);
                distance = Mathf.SmoothDamp(distance, desiredDistance, ref distanceVelocity, smoothTime, Mathf.Infinity, dt);
                currentPitch = Mathf.SmoothDamp(currentPitch, desiredPitch, ref pitchVelocity, smoothTime * 1.6f, Mathf.Infinity, dt);
                yaw = Mathf.SmoothDampAngle(yaw, desiredYaw, ref yawVelocity, smoothTime * 1.6f, Mathf.Infinity, dt);
                shift = Vector2.SmoothDamp(shift, desiredShift, ref shiftVelocity, smoothTime, Mathf.Infinity, dt);
            }
            Apply(target, currentPitch, yaw, distance, shift);
        }

        /// <summary>Places the camera <paramref name="dist"/> from the pivot, moved sideways and up by <paramref name="offset"/>.</summary>
        private void Apply(Vector3 pivot, float pitchDegrees, float yawDegrees, float dist, Vector2 offset)
        {
            Quaternion rotation = Quaternion.Euler(pitchDegrees, yawDegrees, 0f);
            Vector3 position = pivot - rotation * Vector3.forward * dist + rotation * new Vector3(offset.x, offset.y, 0f);
            view.transform.SetPositionAndRotation(position, rotation);
        }

        /// <summary>The camera distance at which the board just fits the play area (cached per screen size and area).</summary>
        private float FitDistance(Vector3 center)
        {
            var screen = new Vector2Int(Screen.width, Screen.height);
            if (screen == fitScreen && fitArea == playArea)
            {
                return fitDistance;
            }
            fitScreen = screen;
            fitArea = playArea;
            Vector3 position = view.transform.position;
            Quaternion rotation = view.transform.rotation;
            float half = board.Side * 0.5f + 0.15f;
            Vector3[] corners =
            {
                center + new Vector3(-half, 0f, -half), center + new Vector3(half, 0f, -half),
                center + new Vector3(-half, 0f, half), center + new Vector3(half, 0f, half),
                center + new Vector3(-half, 0.6f, half), center + new Vector3(half, 0.6f, half)
            };
            // The distance is found with the board centred on screen; the picture is moved into the area afterwards.
            float low = board.Side * 0.3f;
            float high = board.Side * 8f;
            for (int i = 0; i < 28; i++)
            {
                float mid = (low + high) * 0.5f;
                Apply(center, pitch, 0f, mid, Vector2.zero);
                if (Fits(corners))
                {
                    high = mid;
                }
                else
                {
                    low = mid;
                }
            }
            fitDistance = high;
            // Centre the board's picture in the area: move the camera by the gap between the two centres.
            Apply(center, pitch, 0f, fitDistance, Vector2.zero);
            Rect bounds = ViewportBounds(corners);
            float viewHeight = 2f * fitDistance * Mathf.Tan(view.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float viewWidth = viewHeight * view.aspect;
            var areaCenter = new Vector2((playArea.x + playArea.z) * 0.5f, (playArea.y + playArea.w) * 0.5f);
            fitShift = new Vector2(-(areaCenter.x - bounds.center.x) * viewWidth, -(areaCenter.y - bounds.center.y) * viewHeight);
            view.transform.SetPositionAndRotation(position, rotation);
            return fitDistance;
        }

        private Rect ViewportBounds(Vector3[] corners)
        {
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (Vector3 corner in corners)
            {
                Vector3 p = view.WorldToViewportPoint(corner);
                minX = Mathf.Min(minX, p.x);
                maxX = Mathf.Max(maxX, p.x);
                minY = Mathf.Min(minY, p.y);
                maxY = Mathf.Max(maxY, p.y);
            }
            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }

        private bool Fits(Vector3[] corners)
        {
            foreach (Vector3 corner in corners)
            {
                if (view.WorldToViewportPoint(corner).z <= 0f)
                {
                    return false;
                }
            }
            Rect bounds = ViewportBounds(corners);
            return bounds.width <= playArea.z - playArea.x && bounds.height <= playArea.w - playArea.y;
        }
    }
}
