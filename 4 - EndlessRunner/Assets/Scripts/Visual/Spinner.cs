using UnityEngine;

namespace Portfolio.EndlessRunner
{
    /// <summary>Spins and bobs a pickup's model. Put it on the visual child so the pickup itself stays still.</summary>
    public class Spinner : MonoBehaviour
    {
        [SerializeField] internal float degreesPerSecond = 180f;
        [SerializeField] internal float bobHeight = 0.08f;
        [SerializeField] internal float bobSpeed = 3f;
        [SerializeField] internal Vector3 axis = Vector3.up;

        private Vector3 restPosition;
        private float offset;

        private void Awake()
        {
            restPosition = transform.localPosition;
        }

        private void OnEnable()
        {
            // Neighbouring pickups spin out of step, which reads better than a row turning in unison.
            offset = transform.position.z * 0.7f;
        }

        private void Update()
        {
            float time = Time.time + offset;
            transform.localRotation = Quaternion.AngleAxis(time * degreesPerSecond, axis);
            transform.localPosition = restPosition + Vector3.up * (Mathf.Sin(time * bobSpeed) * bobHeight);
        }
    }
}
