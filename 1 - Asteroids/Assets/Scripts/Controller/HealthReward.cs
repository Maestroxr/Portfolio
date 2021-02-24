using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Portfolio
{
    public class HealthReward : Reward
    {
        [field: SerializeField]
        public float HealthAward { get; private set; }


        public override void Award()
        {
            Cause.FiredBy.AwardHealth(this);
            Destroy(gameObject);
        }
    }
}