using UnityEngine;

namespace Portfolio.EndlessRunner
{
    /// <summary>A coin or gem. Gems are worth more and sparkle differently.</summary>
    public class Coin : Collidable
    {
        [SerializeField] internal int value = 1;
        [SerializeField] internal bool gem;

        public int Value => value;

        public bool IsGem => gem;

        protected override void OnTouched(RunnerGameManager manager, RunnerPlayer player)
        {
            manager.CollectCoin(this);
        }
    }
}
