using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Portfolio.EndlessRunner
{
    /// <summary>
    /// Keeps a quad (a glow, a halo or a name) facing the camera, optionally pulsing its size. While several cameras
    /// share the screen (<see cref="FaceEveryCamera"/>, a local race in split screen) it turns to each camera just before
    /// that camera draws, so it faces every player's view and not only the main camera's.
    /// </summary>
    public class Billboard : MonoBehaviour
    {
        [SerializeField] internal float pulse = 0.1f;
        [SerializeField] internal float pulseSpeed = 4f;

        private static readonly List<Billboard> Shown = new List<Billboard>();
        private static Camera cachedCamera;
        private static bool everyCamera;

        private Vector3 restScale;

        /// <summary>
        /// Whether the billboards turn to every camera as it draws (several views on the screen) instead of to the main
        /// camera once a frame.
        /// </summary>
        public static bool FaceEveryCamera
        {
            get => everyCamera;
            set
            {
                if (everyCamera == value)
                {
                    return;
                }
                everyCamera = value;
                if (value)
                {
                    RenderPipelineManager.beginCameraRendering += FaceCamera;
                }
                else
                {
                    RenderPipelineManager.beginCameraRendering -= FaceCamera;
                }
            }
        }

        private void Awake()
        {
            restScale = transform.localScale;
        }

        private void OnEnable()
        {
            Shown.Add(this);
        }

        private void OnDisable()
        {
            Shown.Remove(this);
        }

        private void LateUpdate()
        {
            if (!everyCamera)
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
            }
            if (pulse > 0f)
            {
                transform.localScale = restScale * (1f + Mathf.Sin(Time.time * pulseSpeed) * pulse);
            }
        }

        /// <summary>A camera is about to draw: every billboard turns to it.</summary>
        private static void FaceCamera(ScriptableRenderContext context, Camera camera)
        {
            if (camera.cameraType != CameraType.Game)
            {
                return;
            }
            Quaternion rotation = camera.transform.rotation;
            for (int i = 0; i < Shown.Count; i++)
            {
                Shown[i].transform.rotation = rotation;
            }
        }
    }
}
