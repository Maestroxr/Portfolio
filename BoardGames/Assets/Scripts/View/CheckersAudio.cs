using UnityEngine;

namespace Portfolio.Checkers
{
    /// <summary>The sounds of the game, one clip each.</summary>
    public enum ClipKind
    {
        Move,
        Capture,
        King,
        Win,
        Lose,
        Click
    }

    /// <summary>
    /// Plays the game's sounds through one audio source: a piece sliding, a capture, a crowning, the end of a game and
    /// the clicks of the interface. The manager sets the volume from the settings.
    /// </summary>
    public sealed class CheckersAudio : MonoBehaviour
    {
        [SerializeField] private AudioSource effects;
        [SerializeField] private AudioClip move;
        [SerializeField] private AudioClip capture;
        [SerializeField] private AudioClip king;
        [SerializeField] private AudioClip win;
        [SerializeField] private AudioClip lose;
        [SerializeField] private AudioClip click;

        private float volume = 0.8f;

        /// <summary>How loud the sounds play, 0 to 1.</summary>
        public float Volume
        {
            get => volume;
            set
            {
                volume = Mathf.Clamp01(value);
                if (effects != null)
                {
                    effects.volume = volume;
                }
            }
        }

        private void Awake()
        {
            if (effects == null)
            {
                effects = GetComponent<AudioSource>();
            }
            if (effects != null)
            {
                effects.playOnAwake = false;
                effects.volume = volume;
            }
        }

        public void Play(ClipKind kind)
        {
            AudioClip clip = Clip(kind);
            if (effects == null || clip == null || volume <= 0f)
            {
                return;
            }
            effects.PlayOneShot(clip, kind == ClipKind.Click ? 0.6f : 1f);
        }

        private AudioClip Clip(ClipKind kind)
        {
            switch (kind)
            {
                case ClipKind.Move: return move;
                case ClipKind.Capture: return capture;
                case ClipKind.King: return king;
                case ClipKind.Win: return win;
                case ClipKind.Lose: return lose;
                case ClipKind.Click: return click;
                default: return null;
            }
        }
    }
}
