using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Portfolio
{
    public class Floatable : MonoBehaviour
    {
        public Vector3 Direction;

        public bool RandomDirection;
        [SerializeField]
        private float floatSpeed;


        void Awake()
        {
            if (RandomDirection)
            {
                Direction = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
            }
        }


        void Update()
        {
            transform.position += Direction.normalized * floatSpeed * Time.deltaTime;
        }
    }
}