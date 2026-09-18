using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Asteroids
{
    [CreateAssetMenu(fileName = "Loot", menuName = "Asteroids/Loot", order = 1)]
    public class Loot : ScriptableObject
    {
        public List<Reward> Rewards;
    }
}