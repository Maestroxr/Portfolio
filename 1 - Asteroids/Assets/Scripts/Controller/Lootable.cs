using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// A supply pod drifting through the sector with a blinking beacon. It takes a couple of hits and always drops a
    /// pickup from its drop table: weapons, shields, repairs, power-ups. If nobody shoots it, it drifts away.
    /// </summary>
    public class Lootable : Shootable
    {
        [SerializeField] internal Renderer beacon;
        [SerializeField, ColorUsage(false, true)] internal Color beaconColor = new Color(0.4f, 2.5f, 0.8f);

        private static MaterialPropertyBlock block;
        private static readonly int ColorId = Shader.PropertyToID("_BaseColor");


        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);
            if (beacon == null)
            {
                return;
            }
            bool on = Mathf.Repeat(Age, 0.9f) < 0.18f;
            block ??= new MaterialPropertyBlock();
            block.Clear();
            block.SetColor(ColorId, on ? beaconColor : beaconColor * 0.1f);
            beacon.SetPropertyBlock(block);
        }


        protected override void OnDestroyed(DamageInfo hit)
        {
            if (Field == null)
            {
                return;
            }
            if (Field.Effects != null)
            {
                Field.Effects.Explosion(Position, 1.2f, new Color(0.5f, 1f, 0.6f));
            }
            if (Field.Sounds != null)
            {
                Field.Sounds.PodOpen();
            }
            if (Field.Spawner != null && Loot != null && !IsPuppet)
            {
                Reward drop = Loot.Pick();
                if (drop != null)
                {
                    Field.Spawner.SpawnReward(drop, Position, Velocity * 0.3f);
                }
            }
        }
    }
}
