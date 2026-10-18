using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The sounds of the armory: the four new guns of the asteroid field (which the strike specials of the same kind share),
    /// the crackle of a lightning jump, the bursts of flak shells, cluster canisters and napalm, and the repair kit.
    /// </summary>
    public partial class AsteroidsAudio
    {
        [Header("Armory")]
        [SerializeField] internal AudioClip arcClip;
        [SerializeField] internal AudioClip railClip;
        [SerializeField] internal AudioClip flakClip;
        [SerializeField] internal AudioClip waveClip;
        [SerializeField] internal AudioClip lightningClip;
        [SerializeField] internal AudioClip flakBurstClip;
        [SerializeField] internal AudioClip clusterClip;
        [SerializeField] internal AudioClip napalmClip;
        [SerializeField] internal AudioClip repairClip;


        /// <summary>The sound of a volley of the armory's field weapons; false for the other weapons.</summary>
        private bool FireArmory(WeaponType type)
        {
            switch (type)
            {
                case WeaponType.Arc: ArcFire(); return true;
                case WeaponType.Rail: RailFire(); return true;
                case WeaponType.Flak: FlakFire(); return true;
                case WeaponType.Wave: Play(waveClip, 0.6f, 1f, 0.05f); return true;
                default: return false;
            }
        }

        /// <summary>The Arc Caster's spark and the arc lightning.</summary>
        public void ArcFire() => Play(arcClip, 0.5f, 1f, 0.08f, 0.05f);

        /// <summary>The Rail Driver's and the rail gun's slug.</summary>
        public void RailFire() => Play(railClip, 0.75f, 1f, 0.03f);

        /// <summary>The Flak Burster's and the flak cannon's shell leaving the gun.</summary>
        public void FlakFire() => Play(flakClip, 0.55f, 1f, 0.06f, 0.05f);

        /// <summary>The crackle of lightning jumping between targets (quiet, and not more often than every 60 ms).</summary>
        public void Lightning() => Play(lightningClip, 0.45f, 1f, 0.12f, 0.06f);

        /// <summary>A shell or canister bursting.</summary>
        public void Burst(BurstShot.BurstSound sound)
        {
            switch (sound)
            {
                case BurstShot.BurstSound.Flak: Play(flakBurstClip, 0.45f, 1f, 0.1f, 0.05f); break;
                case BurstShot.BurstSound.Cluster: Play(clusterClip, 0.65f, 1f, 0.06f); break;
                case BurstShot.BurstSound.Napalm: Play(napalmClip, 0.7f, 1f, 0.06f); break;
            }
        }

        /// <summary>A repair kit patched the hull.</summary>
        public void Repair() => Play(repairClip, 0.85f, 1f, 0f, 0.2f);
    }
}
