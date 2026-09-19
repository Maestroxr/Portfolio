using UnityEngine;

namespace Portfolio.EndlessRunner
{
    /// <summary>Sound effects and music of the runner. Coins picked up in quick succession climb in pitch.</summary>
    public class RunnerAudio : MonoBehaviour
    {
        [SerializeField] internal AudioSource effects;
        [SerializeField] internal AudioSource coinSource;
        [SerializeField] internal AudioSource music;
        [SerializeField] internal float musicVolume = 0.4f;

        [Header("Clips")]
        [SerializeField] internal AudioClip coin;
        [SerializeField] internal AudioClip gem;
        [SerializeField] internal AudioClip jump;
        [SerializeField] internal AudioClip slide;
        [SerializeField] internal AudioClip land;
        [SerializeField] internal AudioClip crash;
        [SerializeField] internal AudioClip bump;
        [SerializeField] internal AudioClip whoosh;
        [SerializeField] internal AudioClip powerUp;
        [SerializeField] internal AudioClip shieldBreak;
        [SerializeField] internal AudioClip bounce;
        [SerializeField] internal AudioClip splash;
        [SerializeField] internal AudioClip countdown;
        [SerializeField] internal AudioClip go;
        [SerializeField] internal AudioClip victory;
        [SerializeField] internal AudioClip gameOver;
        [SerializeField] internal AudioClip star;
        [SerializeField] internal AudioClip click;
        [SerializeField] internal AudioClip menuMusic;
        [SerializeField] internal AudioClip runMusic;

        private int coinStreak;
        private float lastCoinTime = -10f;
        private float duck = 1f;
        private float duckTarget = 1f;

        public void Play(AudioClip clip, float volume = 1f)
        {
            if (clip != null && effects != null)
            {
                effects.PlayOneShot(clip, volume);
            }
        }

        public void Coin(bool isGem)
        {
            if (coinSource == null)
            {
                return;
            }
            if (Time.time - lastCoinTime > 0.7f)
            {
                coinStreak = 0;
            }
            coinStreak = Mathf.Min(coinStreak + 1, 14);
            lastCoinTime = Time.time;
            coinSource.pitch = 1f + coinStreak * 0.03f;
            AudioClip clip = isGem && gem != null ? gem : coin;
            if (clip != null)
            {
                coinSource.PlayOneShot(clip, isGem ? 0.9f : 0.55f);
            }
        }

        public void PlayMusic(AudioClip clip)
        {
            if (music == null || clip == null)
            {
                return;
            }
            if (music.clip == clip && music.isPlaying)
            {
                return;
            }
            music.clip = clip;
            music.loop = true;
            music.Play();
        }

        public void StopMusic()
        {
            if (music != null)
            {
                music.Stop();
            }
        }

        /// <summary>Lowers the music, e.g. while the game is paused.</summary>
        public void Duck(bool ducked)
        {
            duckTarget = ducked ? 0.35f : 1f;
        }

        private void Update()
        {
            duck = Mathf.MoveTowards(duck, duckTarget, Time.unscaledDeltaTime * 2f);
            if (music != null)
            {
                music.volume = musicVolume * duck;
            }
        }
    }
}
