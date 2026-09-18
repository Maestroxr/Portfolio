using System.Collections.Generic;

namespace Portfolio.Monopoly
{
    public class RewardTile : Tile
    {
        public List<Reward> rewards { get; private set; }


        public void InitReward(List<Reward> rewards)
        {
            this.rewards = rewards;
        }


        public override void PlayerVisit(MonopolyPlayer player)
        {
            base.PlayerVisit(player);
            if (rewards == null || rewards.Count == 0)
            {
                return;
            }
            Reward reward = rewards[0];
            player.Award(reward);
            rewards.RemoveAt(0);
            rewards.Add(reward);
        }
    }
}
