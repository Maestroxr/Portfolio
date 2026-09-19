using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>Flaps a creature's wings around its long axis (the parent's z), left and right wings mirrored.</summary>
    public class Flapper : MonoBehaviour
    {
        [SerializeField] internal Transform[] wings = new Transform[0];
        [SerializeField] internal float speed = 18f;
        [SerializeField] internal float angle = 28f;

        private Quaternion[] rest;


        private void Awake()
        {
            rest = new Quaternion[wings.Length];
            for (int i = 0; i < wings.Length; i++)
            {
                rest[i] = wings[i] != null ? wings[i].localRotation : Quaternion.identity;
            }
        }


        private void Update()
        {
            float flap = Mathf.Sin(Time.time * speed) * angle;
            for (int i = 0; i < wings.Length; i++)
            {
                if (wings[i] != null)
                {
                    wings[i].localRotation = Quaternion.AngleAxis(i % 2 == 0 ? flap : -flap, Vector3.forward) * rest[i];
                }
            }
        }
    }
}
