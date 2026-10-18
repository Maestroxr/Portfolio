using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// Turns its transform around its parent's Z axis (the axis of the screen) at a steady speed: the rings of a gate or
    /// a station, the drum of a minelayer. A negative speed turns it the other way.
    /// </summary>
    public class Spinner : MonoBehaviour
    {
        [Tooltip("Degrees per second; negative turns clockwise.")]
        [SerializeField] internal float speed = 30f;


        private void Update()
        {
            transform.localRotation = Quaternion.Euler(0f, 0f, speed * Time.deltaTime) * transform.localRotation;
        }
    }
}
