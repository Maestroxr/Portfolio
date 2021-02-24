using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Portfolio
{
    /// <summary>
    /// Generic component which adds cyclical behaviour to objects that
    /// allows circumnavigating a referenced Playground object.
    /// Using a Mirror field, the Cyclical component will make sure to update whichever
    /// component T stands for and update it's Mirror field as well as it's own.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Cyclical<T> : MonoBehaviour where T : MonoBehaviour, ICyclic<T>
    {
        public T Mirror;
        public Playground Playground;
        public bool IsMirrorObject;
        [field: SerializeField]
        public bool Show { get; private set; }

        [SerializeField]
        private Vector3 offset;
        private Vector3 size, halfSize, margin, middle;
        private Vector3 mirrorPos, pos, distance;


        private void Awake()
        {
            Show = false;
            GetComponent<ICyclic<T>>()?.Setup(Mirror, IsMirrorObject);
        }


        void Start()
        {
            size = Playground.Size;
            halfSize = size / 2;
            margin = Playground.Margin;
            middle = Playground.Middle;
        }


        void Update()
        {
            pos = transform.position;
            distance = middle - pos;
            distance.x = Math.Abs(distance.x);
            distance.y = Math.Abs(distance.y);
            mirrorPos = pos;

            bool xAxis = distance.x > halfSize.x ;
            bool yAxis = distance.y > halfSize.y ;
            if (xAxis || yAxis)
            {
                mirrorPos.x = pos.x + (size.x - ((int)((pos.x + halfSize.x + margin.x) / size.x)) * size.x * 2) * Convert.ToInt32(xAxis);
                mirrorPos.y = pos.y + (size.y - ((int)((pos.y + halfSize.y + margin.y) / size.y)) * size.y * 2) * Convert.ToInt32(yAxis);
                Show = true;
                if (distance.x > halfSize.x + margin.x || distance.y > halfSize.y + margin.y)
                {
                    transform.position = mirrorPos;
                    Mirror.gameObject.SetActive(false);
                    Show = false;
                }
                else
                {
                    Mirror.transform.rotation = transform.rotation;
                    Mirror.transform.position = mirrorPos;
                    Mirror.gameObject.SetActive(true);
                }
                //Debug.Log($"{name} distance: {distance}  position: {transform.position}  mirror: {mirrorPos}");
            }
        }

        public void Setup(T mirror, bool isMirror, Playground playground)
        {
            Mirror = mirror;
            IsMirrorObject = isMirror;
            GetComponent<ICyclic<T>>()?.Setup(Mirror, IsMirrorObject);
            Playground = playground;

            if (isMirror)
            {
                enabled = false;
                gameObject.SetActive(false);
            }
        }
    }
}