using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RewardTile : Tile
{
    public List<Reward> rewards { get; private set; }
    

    public void InitReward(List<Reward> rewards)
    {
        this.rewards = rewards;
    }


    public override void PlayerVisit(Player player)
    {
        base.PlayerVisit(player);
        Reward reward = rewards[0];
        player.Award(reward);
        rewards.RemoveAt(0);
        rewards.Add(reward);
    }
}
