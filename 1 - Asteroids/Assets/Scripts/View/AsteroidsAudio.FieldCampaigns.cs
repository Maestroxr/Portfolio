using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>The music of the field campaigns (AsteroidsAudio.FieldCampaigns.cs): a battle loop of its own for every campaign after the first.</summary>
    public partial class AsteroidsAudio
    {
        [Header("Field campaigns")]
        [Tooltip("The battle loop of each asteroid field campaign, by campaign (the first, empty, plays the battle music).")]
        [SerializeField] internal AudioClip[] fieldMusic = new AudioClip[0];

        /// <summary>The battle loop of field campaign <paramref name="arc"/>: its own, or the battle music.</summary>
        public AudioClip FieldMusic(int arc)
        {
            AudioClip own = fieldMusic != null && arc >= 0 && arc < fieldMusic.Length ? fieldMusic[arc] : null;
            return own != null ? own : battleMusic;
        }
    }
}
