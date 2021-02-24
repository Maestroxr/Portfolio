using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Portfolio
{
    public class PointReward : Reward
    {
        [field: SerializeField]
        public int PointsAward { get; private set; }


        public override void Award()
        {
            Cause.FiredBy.AwardPoints(this);
            Destroy(gameObject);
        }
    }
}