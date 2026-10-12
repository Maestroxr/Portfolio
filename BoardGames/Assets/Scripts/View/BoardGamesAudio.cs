using UnityEngine;

namespace Portfolio.BoardGames
{
    /// <summary>The sounds of the games, one clip each.</summary>
    public enum ClipKind
    {
        /// <summary>A piece slides (draughts and chess).</summary>
        Move,
        Capture,
        /// <summary>A man is crowned, a pawn promoted.</summary>
        King,
        Win,
        Lose,
        Click,
        /// <summary>Chess: a king is in check.</summary>
        Check,
        /// <summary>Backgammon: the dice rattle out of the cup.</summary>
        Roll,
        /// <summary>Backgammon: a checker lands on a point (or a die bounces).</summary>
        Clack,
        /// <summary>Backgammon: a checker is borne off, or one is hit.</summary>
        BearOff,
        /// <summary>Backgammon: the doubling cube is turned.</summary>
        Cube
    }

    /// <summary>
    /// Plays the games' sounds through one audio source: a piece sliding, a capture, a crowning, a check, the dice, a
    /// checker landing, the cube, the end of a game and the clicks of the interface. The manager sets the volume from the
    /// settings.
    /// </summary>
    public sealed class BoardGamesAudio : MonoBehaviour
    {
        [SerializeField] private AudioSource effects;
        [SerializeField] private AudioClip move;
        [SerializeField] private AudioClip capture;
        [SerializeField] private AudioClip king;
        [SerializeField] private AudioClip win;
        [SerializeField] private AudioClip lose;
        [SerializeField] private AudioClip click;
        [SerializeField] private AudioClip check;
        [SerializeField] private AudioClip roll;
        [SerializeField] private AudioClip clack;
        [SerializeField] private AudioClip bearOff;
        [SerializeField] private AudioClip cube;

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
            Play(kind, kind == ClipKind.Click ? 0.6f : 1f);
        }

        /// <summary>Plays a clip at a part of the volume (several checkers landing at once are quieter each).</summary>
        public void Play(ClipKind kind, float scale)
        {
            AudioClip clip = Clip(kind);
            if (effects == null || clip == null || volume <= 0f)
            {
                return;
            }
            effects.PlayOneShot(clip, Mathf.Clamp01(scale));
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
                case ClipKind.Check: return check != null ? check : king;
                case ClipKind.Roll: return roll;
                case ClipKind.Clack: return clack != null ? clack : move;
                case ClipKind.BearOff: return bearOff != null ? bearOff : capture;
                case ClipKind.Cube: return cube != null ? cube : click;
                default: return null;
            }
        }
    }
}
