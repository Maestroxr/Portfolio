using UnityEngine;

namespace Portfolio.EndlessRunner
{
    /// <summary>
    /// Camera of the runner: looks at the character from the front in the menu, swings behind it for the run and
    /// follows it with a little lag, widens the view with speed, circles it at the finish and shakes on crashes.
    /// </summary>
    public class RunnerCamera : MonoBehaviour
    {
        public enum Mode
        {
            Menu,
            Chase,
            Finish,
            Crash
        }

        [SerializeField] internal RunnerPlayer target;
        [SerializeField] internal Camera view;
        [SerializeField] internal Vector3 chaseOffset = new Vector3(0f, 3.1f, -6.6f);
        [SerializeField] internal Vector3 chaseLook = new Vector3(0f, 1.3f, 6f);
        [SerializeField] internal Vector3 menuOffset = new Vector3(2.4f, 1.35f, 4.3f);
        [SerializeField] internal Vector3 menuLook = new Vector3(0.75f, 1.05f, 0f);
        [SerializeField] internal float fieldOfView = 58f;
        [SerializeField] internal float fastFieldOfView = 70f;
        [SerializeField] internal float transitionTime = 1.6f;

        private Mode mode = Mode.Menu;
        private float blend = 1f;
        private Vector3 fromPosition;
        private Quaternion fromRotation;
        private float followX;
        private float followY;
        private float velocityX;
        private float velocityY;
        private bool snap = true;
        private float shake;
        private float orbit;

        public Mode CurrentMode => mode;

        public void SetMode(Mode newMode, bool instant = false)
        {
            fromPosition = transform.position;
            fromRotation = transform.rotation;
            mode = newMode;
            blend = instant ? 1f : 0f;
            if (instant)
            {
                snap = true;
            }
            if (newMode == Mode.Finish)
            {
                orbit = 0f;
            }
        }

        public void Shake(float amount)
        {
            shake = Mathf.Max(shake, amount);
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }
            float deltaTime = Time.deltaTime;
            Vector3 player = target.transform.position;
            if (snap)
            {
                followX = player.x;
                followY = Mathf.Max(player.y, -0.5f);
                velocityX = 0f;
                velocityY = 0f;
                snap = false;
            }
            else if (deltaTime > 0f)
            {
                followX = Mathf.SmoothDamp(followX, player.x, ref velocityX, 0.12f, Mathf.Infinity, deltaTime);
                followY = Mathf.SmoothDamp(followY, Mathf.Max(player.y, -0.5f), ref velocityY, 0.22f, Mathf.Infinity, deltaTime);
            }

            Vector3 desiredPosition;
            Quaternion desiredRotation;
            switch (mode)
            {
                case Mode.Menu:
                {
                    float sway = Mathf.Sin(Time.time * 0.35f);
                    desiredPosition = player + menuOffset + new Vector3(sway * 0.3f, sway * 0.08f, 0f);
                    desiredRotation = Quaternion.LookRotation(player + menuLook - desiredPosition);
                    break;
                }
                case Mode.Finish:
                {
                    // Swings slowly behind the runner, who turns round to cheer at the camera with the road ahead behind it.
                    orbit += deltaTime;
                    Vector3 offset = Quaternion.Euler(0f, Mathf.Sin(orbit * 0.6f) * 28f, 0f) * new Vector3(0f, 1.9f, -5.4f);
                    desiredPosition = player + offset;
                    desiredRotation = Quaternion.LookRotation(player + Vector3.up * 1.2f - desiredPosition);
                    break;
                }
                default:
                {
                    Vector3 anchor = new Vector3(followX * 0.85f, followY, player.z);
                    desiredPosition = anchor + chaseOffset;
                    Vector3 look = new Vector3(followX * 0.9f, followY, player.z) + chaseLook;
                    desiredRotation = Quaternion.LookRotation(look - desiredPosition);
                    break;
                }
            }

            blend = Mathf.Min(1f, blend + (transitionTime > 0f ? deltaTime / transitionTime : 1f));
            float eased = blend * blend * (3f - 2f * blend);
            Vector3 position = Vector3.Lerp(fromPosition, desiredPosition, eased);
            Quaternion rotation = Quaternion.Slerp(fromRotation, desiredRotation, eased);
            if (blend >= 1f)
            {
                position = desiredPosition;
                rotation = desiredRotation;
            }
            if (shake > 0f && deltaTime > 0f)
            {
                float t = Time.time * 30f;
                position += new Vector3(Mathf.PerlinNoise(t, 0.1f) - 0.5f, Mathf.PerlinNoise(0.7f, t) - 0.5f, 0f) * (shake * 0.6f);
                shake = Mathf.MoveTowards(shake, 0f, deltaTime * 2.2f);
            }
            transform.SetPositionAndRotation(position, rotation);

            if (view != null)
            {
                float speedFactor = Mathf.InverseLerp(10f, 22f, target.Speed);
                float fov = mode == Mode.Menu ? fieldOfView - 8f : Mathf.Lerp(fieldOfView, fastFieldOfView, speedFactor);
                view.fieldOfView = Mathf.Lerp(view.fieldOfView, fov, 1f - Mathf.Exp(-3f * Mathf.Max(deltaTime, 0f)));
            }
        }
    }
}
