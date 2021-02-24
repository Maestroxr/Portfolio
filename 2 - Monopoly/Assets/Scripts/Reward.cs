using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Reward", menuName = "Monopoly/Reward", order = 1)]
public class Reward : ScriptableObject
{
    [SerializeField] private int baseReward;
    [SerializeField] private List<int> probabilities;
    [SerializeField] private List<int> percentages;


    public int CalculateReward()
    {
        int random = Random.Range(0, 100);
        int probability = 0;
        for (int i = 0; i < probabilities.Count; i++)
        {
            probability += probabilities[i];
            if (random <= probability)
            {
                return baseReward * percentages[i] / 100;
            }
        }
        throw new System.ArgumentException("Reward has not been asserted");
    }


    public bool Assert(out string error)
    {
        if (probabilities.Count != percentages.Count || probabilities.Count == 0)
        {
            error = $"No probabilities for this reward";
            return false;
        }
        int probabilitySum = 0;
        for (int i = 0; i < probabilities.Count; i++)
        {
            int probability = probabilities[i];
            if (probability <= 0)
            {
                error = $"Non-positive probability";
                return false;
            }

            if (percentages[i] <= 0)
            {
                error = $"Non-positive percentage from base reward";
                return false;
            }
            probabilitySum += probability;
        }

        if (probabilitySum != 100)
        {
            error = $"Probabilities do not amount to 100";
            return false;
        }

        error = null;
        return true;
    }
}
