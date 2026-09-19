using UnityEngine;

namespace Portfolio.MemoryCards
{
    /// <summary>
    /// Sounds and music of Memory Cards. Effects play on a pool of sources with a little pitch variation; the match
    /// chime climbs a major scale with the combo. Music cross-fades between the menu and the play loop and ducks while
    /// the game is paused.
    /// </summary>
    public class CardsAudio : MonoBehaviour
    {
        private static readonly int[] ComboSteps = { 0, 2, 4, 5, 7, 9, 11, 12, 14, 16 };

        [SerializeField] internal AudioSource musicA;
        [SerializeField] internal AudioSource musicB;
        [SerializeField] internal AudioSource[] effects = new AudioSource[0];
        [SerializeField] internal float musicVolume = 0.42f;
        [SerializeField] internal float duckedVolume = 0.14f;

        [Header("Music")]
        [SerializeField] internal AudioClip menuMusic;
        [SerializeField] internal AudioClip playMusic;

        [Header("Cards")]
        [SerializeField] internal AudioClip[] flips = new AudioClip[0];
        [SerializeField] internal AudioClip[] deals = new AudioClip[0];
        [SerializeField] internal AudioClip shuffle;
        [SerializeField] internal AudioClip fan;
        [SerializeField] internal AudioClip match;
        [SerializeField] internal AudioClip mismatch;
        [SerializeField] internal AudioClip wrongOrder;
        [SerializeField] internal AudioClip bomb;
        [SerializeField] internal AudioClip clock;
        [SerializeField] internal AudioClip peek;
        [SerializeField] internal AudioClip wild;
        [SerializeField] internal AudioClip crack;
        [SerializeField] internal AudioClip heart;

        [Header("Interface")]
        [SerializeField] internal AudioClip click;
        [SerializeField] internal AudioClip back;
        [SerializeField] internal AudioClip select;
        [SerializeField] internal AudioClip locked;
        [SerializeField] internal AudioClip tick;
        [SerializeField] internal AudioClip go;
        [SerializeField] internal AudioClip star;
        [SerializeField] internal AudioClip victory;
        [SerializeField] internal AudioClip gameOver;
        [SerializeField] internal AudioClip record;

        private int nextEffect;
        private bool ducked;
        private AudioSource currentMusic;
        private AudioClip wantedMusic;

        public void Play(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (clip == null || effects == null || effects.Length == 0)
            {
                return;
            }
            AudioSource source = effects[nextEffect];
            nextEffect = (nextEffect + 1) % effects.Length;
            source.pitch = pitch;
            source.PlayOneShot(clip, volume);
        }

        /// <summary>One of <paramref name="clips"/> at a slightly varied pitch.</summary>
        public void PlayAny(AudioClip[] clips, float volume = 1f, float pitchJitter = 0.06f)
        {
            if (clips == null || clips.Length == 0)
            {
                return;
            }
            Play(clips[Random.Range(0, clips.Length)], volume, 1f + Random.Range(-pitchJitter, pitchJitter));
        }

        /// <summary>The match chime, one scale step higher for every combo step.</summary>
        public void PlayMatch(int combo)
        {
            int step = ComboSteps[Mathf.Clamp(combo - 1, 0, ComboSteps.Length - 1)];
            Play(match, 0.9f, Mathf.Pow(2f, step / 12f));
        }

        public void PlayMusic(AudioClip clip)
        {
            wantedMusic = clip;
        }

        public void StopMusic()
        {
            wantedMusic = null;
        }

        /// <summary>Lowers the music behind the pause menu.</summary>
        public void Duck(bool duck)
        {
            ducked = duck;
        }

        private void Update()
        {
            if (musicA == null || musicB == null)
            {
                return;
            }
            if (currentMusic == null || (wantedMusic != null && currentMusic.clip != wantedMusic))
            {
                if (wantedMusic != null)
                {
                    AudioSource next = currentMusic == musicA ? musicB : musicA;
                    if (next.clip != wantedMusic || !next.isPlaying)
                    {
                        next.clip = wantedMusic;
                        next.loop = true;
                        next.volume = 0f;
                        next.Play();
                    }
                    currentMusic = next;
                }
            }
            float target = ducked ? duckedVolume : musicVolume;
            float dt = Time.unscaledDeltaTime;
            Fade(musicA, target, dt);
            Fade(musicB, target, dt);
        }

        private void Fade(AudioSource source, float target, float dt)
        {
            bool wanted = source == currentMusic && wantedMusic != null;
            float volume = Mathf.MoveTowards(source.volume, wanted ? target : 0f, dt * 0.8f);
            source.volume = volume;
            if (!wanted && volume <= 0f && source.isPlaying)
            {
                source.Stop();
            }
        }
    }
}
