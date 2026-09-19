using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// A drop table: the pickups something can drop and how likely each one is. Asteroids roll against the mission's
    /// loot chance first; supply pods and bosses always drop.
    /// </summary>
    [CreateAssetMenu(fileName = "Loot", menuName = "Asteroids/Loot", order = 1)]
    public class Loot : ScriptableObject
    {
        public List<Reward> Rewards = new List<Reward>();
        [Tooltip("Relative weight of each reward, in the order of Rewards; missing weights count as 1.")]
        public List<int> Weights = new List<int>();

        public int TotalWeight
        {
            get
            {
                int total = 0;
                for (int i = 0; i < Rewards.Count; i++)
                {
                    total += Weight(i);
                }
                return total;
            }
        }

        public int Weight(int index)
        {
            if (Rewards == null || index < 0 || index >= Rewards.Count || Rewards[index] == null)
            {
                return 0;
            }
            return Weights != null && index < Weights.Count ? Mathf.Max(0, Weights[index]) : 1;
        }

        /// <summary>A reward picked by weight with <paramref name="roll"/> in [0, 1); null when the table is empty.</summary>
        public Reward Pick(float roll)
        {
            int total = TotalWeight;
            if (total <= 0)
            {
                return null;
            }
            int target = Mathf.Min(total - 1, Mathf.FloorToInt(Mathf.Clamp01(roll) * total));
            for (int i = 0; i < Rewards.Count; i++)
            {
                target -= Weight(i);
                if (target < 0)
                {
                    return Rewards[i];
                }
            }
            return null;
        }

        public Reward Pick()
        {
            return Pick(Random.value);
        }
    }
}
