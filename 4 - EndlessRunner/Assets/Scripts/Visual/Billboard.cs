using UnityEngine;

namespace Portfolio.EndlessRunner
{
    /// <summary>Keeps a quad (a glow or a halo) facing the camera, optionally pulsing its size.</summary>
    public class Billboard : MonoBehaviour
    {
        [SerializeField] internal float pulse = 0.1f;
        [SerializeField] internal float pulseSpeed = 4f;

        private Vector3 restScale;
        private static Camera cachedCamera;

        private void Awake()
        {
            restScale = transform.localScale;
        }

        private void LateUpdate()
        {
            if (cachedCamera == null || !cachedCamera.isActiveAndEnabled)
            {
                cachedCamera = Camera.main;
                if (cachedCamera == null)
                {
                    return;
                }
            }
            transform.rotation = cachedCamera.transform.rotation;
            if (pulse > 0f)
            {
                transform.localScale = restScale * (1f + Mathf.Sin(Time.time * pulseSpeed) * pulse);
            }
        }
    }
}
