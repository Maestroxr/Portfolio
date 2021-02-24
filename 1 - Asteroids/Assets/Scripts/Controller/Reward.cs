using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio
{
    /// <summary>
    /// Abstract base class for rewards.
    /// Award is the main method for overriding by derived classes.
    /// </summary>
    public abstract class Reward : MonoBehaviour
    {
        public Shot Cause;

        [SerializeField]
        private Button clickToReward;


        protected virtual void Start()
        {
            clickToReward.onClick.AddListener(() =>
            {
                if (Cause.FiredBy.gameObject.activeInHierarchy)
                {
                    Award();
                }
            });
        }


        abstract public void Award();
    }
}