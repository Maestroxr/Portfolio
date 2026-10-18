using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The strike attack patterns the Iron Tide and Hive Fall campaigns brought (their numbers are
    /// <see cref="StrikeCampaignRules"/>'): rings of shots in every direction, fans that sweep from side to side, artillery
    /// barrages whose shells burst where the nearest pilot was, and launches of aircraft. Only the simulator fires; the
    /// shots and aircraft travel to the other pilots like any others.
    /// </summary>
    public abstract partial class Enemy
    {
        /// <summary>Whether the next ring is turned by half a step (rings alternate, so they interleave).</summary>
        private bool oddRing;


        /// <summary>
        /// Fires one volley of a campaign pattern: true when <paramref name="pattern"/> is one (the volley went), false for
        /// the patterns <see cref="FireStrikeVolley"/> fires itself.
        /// </summary>
        private bool FireCampaignVolley(AttackPattern pattern, EnemyShotKind kind, int shots, Transform[] muzzles)
        {
            switch (pattern)
            {
                case AttackPattern.Ring:
                    FireRing(kind, shots, muzzles);
                    return true;
                case AttackPattern.Sweep:
                    FireSweep(kind, shots, muzzles);
                    return true;
                case AttackPattern.Barrage:
                    FireBarrage(kind, shots, muzzles);
                    return true;
                case AttackPattern.Launch:
                    UnitLauncher launcher = GetComponent<UnitLauncher>();
                    if (launcher != null)
                    {
                        launcher.Launch(this);
                    }
                    return true;
                default:
                    return false;
            }
        }


        /// <summary><paramref name="shots"/> shots evenly round the unit, from its first muzzle (or its centre).</summary>
        private void FireRing(EnemyShotKind kind, int shots, Transform[] muzzles)
        {
            shots = Mathf.Max(1, shots);
            Vector2 center = muzzles != null && muzzles.Length > 0 ? MuzzlePoint(muzzles[0]) : Position;
            float speed = ShotSpeed(kind);
            for (int i = 0; i < shots; i++)
            {
                Vector2 direction = StrikeCampaignRules.RingDirection(i, shots, oddRing);
                Field.Spawner.FireEnemyShot(kind, center + direction * (radius * 0.6f), direction * speed);
            }
            oddRing = !oddRing;
            Field.Effects?.Muzzle(center);
            Field.Sounds?.EnemyShot();
        }


        /// <summary>A small fan whose middle swings with the unit's age, from its muzzles in turn.</summary>
        private void FireSweep(EnemyShotKind kind, int shots, Transform[] muzzles)
        {
            shots = Mathf.Max(1, shots);
            int muzzleCount = muzzles != null ? muzzles.Length : 0;
            float speed = ShotSpeed(kind);
            for (int i = 0; i < shots; i++)
            {
                Vector2 direction = StrikeCampaignRules.SweepDirection(i, shots, Age);
                Vector2 origin = muzzleCount > 0 ? MuzzlePoint(muzzles[i % muzzleCount]) : Position + direction * (radius * 0.6f);
                Field.Spawner.FireEnemyShot(kind, origin, direction * speed);
            }
            Field.Sounds?.EnemyShot();
        }


        /// <summary>
        /// Artillery: <paramref name="shots"/> shells from the first muzzle, the first one at the nearest pilot's spot and the
        /// others around it (<see cref="StrikeCampaignRules.BarrageSpot"/>), each flying there in the shell's flight time.
        /// </summary>
        private void FireBarrage(EnemyShotKind kind, int shots, Transform[] muzzles)
        {
            Vector2 origin = muzzles != null && muzzles.Length > 0 ? MuzzlePoint(muzzles[0]) : Position;
            AsteroidsPlayer target = Target;
            Vector2 aimed = target != null ? target.Position : origin + Vector2.down * 8f;
            Playground playground = Field.Playground;
            for (int i = 0; i < Mathf.Max(1, shots); i++)
            {
                Vector2 spot = StrikeCampaignRules.BarrageSpot(aimed, i);
                if (playground != null)
                {
                    spot = playground.Clamp(spot, 0.5f);
                }
                Field.Spawner.FireEnemyShot(kind, origin, StrikeCampaignRules.ShellVelocity(origin, spot));
            }
            Field.Effects?.Muzzle(origin);
            Field.Sounds?.GroundBoom(0.35f);
        }


        private float ShotSpeed(EnemyShotKind kind)
        {
            float speed = StrikeUnitRules.Shot(kind).Speed;
            return speed > 0f ? speed : shotSpeed;
        }
    }
}
