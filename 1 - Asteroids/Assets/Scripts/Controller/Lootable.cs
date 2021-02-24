using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Portfolio
{
    public class Lootable : Shootable, ICyclic<Shootable>
    {
        public Transform RewardParent;

        [SerializeField]
        private Loot loot;


        public override void WasShot(Shot shot)
        {
            base.WasShot(shot);
            Loot(shot);
        }


        private void Loot(Shot shot)
        {
            int index = Random.Range(0, loot.Rewards.Count);
            Reward reward = Instantiate(loot.Rewards[index], Camera.main.WorldToScreenPoint(transform.position), Quaternion.identity, RewardParent);
            reward.Cause = shot;
        }
    }
}