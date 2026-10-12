using UnityEngine;

namespace Portfolio.Checkers
{
    /// <summary>
    /// Frames the board from the side of the player at the bottom: the whole board and a margin fit the part of the
    /// screen the interface leaves free, whatever the screen's shape. The view turns half a circle, smoothly, when the
    /// bottom side changes (hot seat); on the title it sits a touch lower and closer.
    /// </summary>
    public sealed class CameraRig : MonoBehaviour
    {
        [SerializeField] private Camera view;
        [SerializeField] private BoardView board;
        [SerializeField] private float pitch = 52f;
        [SerializeField] private float titlePitch = 35f;
        [SerializeField] private float smoothTime = 0.6f;
        [SerializeField] private float fieldOfView = 30f;
        [Tooltip("Squares of margin around the board that must stay on screen.")]
        [SerializeField] private float margin = 0.8f;
        [Tooltip("Part of the screen the board may use in play: x/y = lower left, z/w = upper right, as fractions of the screen.")]
        [SerializeField] private Vector4 playArea = new Vector4(0.02f, 0.13f, 0.98f, 0.87f);

        private int size = 8;
        private Side bottom = Side.Light;
        private bool title = true;
        private bool snap = true;
        private float yaw;
        private float currentPitch;
        private float distance;
        private Vector3 target;
        private Vector3 targetVelocity;
        private float distanceVelocity;
        private float pitchVelocity;
        private float yawVelocity;
        private float fitDistance = 20f;
        private Vector2Int fitScreen;
        private int fitSize;
        private bool fitTitle;
        private float fitFov;

        public Camera View => view;

        /// <summary>The side the camera shows at the bottom of the screen.</summary>
        public Side Bottom => bottom;

        /// <summary>Frames a board of <paramref name="size"/> squares from <paramref name="bottom"/>'s side, at once or smoothly.</summary>
        public void Frame(int size, Side bottom, bool immediate)
        {
            this.size = Mathf.Max(size, 2);
            this.bottom = bottom;
            snap |= immediate;
            fitScreen = Vector2Int.zero;
        }

        /// <summary>The title's framing (lower and closer) or the one for play.</summary>
        public void SetTitle(bool title, bool immediate)
        {
            this.title = title;
            snap |= immediate;
        }

        private void Awake()
        {
            if (view != null)
            {
                view.fieldOfView = fieldOfView;
            }
        }

        private void LateUpdate()
        {
            if (view == null)
            {
                return;
            }
            if (!Mathf.Approximately(view.fieldOfView, fieldOfView))
            {
                view.fieldOfView = fieldOfView;
                fitScreen = Vector2Int.zero;
            }
            Vector3 center = board != null ? board.transform.TransformPoint(new Vector3(0f, board.Surface, 0f)) : transform.position;
            float desiredPitch = title ? titlePitch : pitch;
            float desiredYaw = bottom == Side.Light ? 0f : 180f;
            float desiredDistance = FitDistance(center, desiredPitch) * (title ? 0.92f : 1f);
            Vector3 desiredTarget = center;
            if (snap)
            {
                snap = false;
                target = desiredTarget;
                distance = desiredDistance;
                currentPitch = desiredPitch;
                yaw = desiredYaw;
            }
            else
            {
                float dt = Time.unscaledDeltaTime;
                target = Vector3.SmoothDamp(target, desiredTarget, ref targetVelocity, smoothTime, Mathf.Infinity, dt);
                distance = Mathf.SmoothDamp(distance, desiredDistance, ref distanceVelocity, smoothTime, Mathf.Infinity, dt);
                currentPitch = Mathf.SmoothDamp(currentPitch, desiredPitch, ref pitchVelocity, smoothTime * 1.5f, Mathf.Infinity, dt);
                yaw = Mathf.SmoothDampAngle(yaw, desiredYaw, ref yawVelocity, smoothTime * 1.8f, Mathf.Infinity, dt);
            }
            Apply(target, currentPitch, yaw, distance);
        }

        /// <summary>Places the camera <paramref name="dist"/> from the pivot, looking down at it.</summary>
        private void Apply(Vector3 pivot, float pitchDegrees, float yawDegrees, float dist)
        {
            Quaternion rotation = Quaternion.Euler(pitchDegrees, yawDegrees, 0f);
            view.transform.SetPositionAndRotation(pivot - rotation * Vector3.forward * dist, rotation);
        }

        /// <summary>The camera distance at which the board and its margin just fit the screen (cached per screen, size and mode).</summary>
        private float FitDistance(Vector3 center, float pitchDegrees)
        {
            var screen = new Vector2Int(Screen.width, Screen.height);
            if (screen == fitScreen && fitSize == size && fitTitle == title && Mathf.Approximately(fitFov, fieldOfView))
            {
                return fitDistance;
            }
            fitScreen = screen;
            fitSize = size;
            fitTitle = title;
            fitFov = fieldOfView;
            Vector3 position = view.transform.position;
            Quaternion rotation = view.transform.rotation;
            float half = size * 0.5f + margin;
            Vector3[] corners =
            {
                center + new Vector3(-half, 0f, -half), center + new Vector3(half, 0f, -half),
                center + new Vector3(-half, 0f, half), center + new Vector3(half, 0f, half),
                center + new Vector3(-half, 0.4f, half), center + new Vector3(half, 0.4f, half)
            };
            Vector4 area = title ? new Vector4(0.02f, 0.02f, 0.98f, 0.98f) : playArea;
            float low = size * 0.3f;
            float high = size * 10f;
            for (int i = 0; i < 28; i++)
            {
                float mid = (low + high) * 0.5f;
                Apply(center, pitchDegrees, 0f, mid);
                if (Fits(corners, area))
                {
                    high = mid;
                }
                else
                {
                    low = mid;
                }
            }
            fitDistance = high;
            view.transform.SetPositionAndRotation(position, rotation);
            return fitDistance;
        }

        private bool Fits(Vector3[] corners, Vector4 area)
        {
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (Vector3 corner in corners)
            {
                Vector3 p = view.WorldToViewportPoint(corner);
                if (p.z <= 0f)
                {
                    return false;
                }
                minX = Mathf.Min(minX, p.x);
                maxX = Mathf.Max(maxX, p.x);
                minY = Mathf.Min(minY, p.y);
                maxY = Mathf.Max(maxY, p.y);
            }
            // The picture is centred on the pivot, so the fit is checked against the narrower half of the area.
            float halfWidth = Mathf.Min(0.5f - area.x, area.z - 0.5f);
            float halfHeight = Mathf.Min(0.5f - area.y, area.w - 0.5f);
            return maxX - 0.5f <= halfWidth && 0.5f - minX <= halfWidth && maxY - 0.5f <= halfHeight && 0.5f - minY <= halfHeight;
        }
    }
}
