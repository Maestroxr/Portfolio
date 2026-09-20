using System;
using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Monopoly
{
    public enum Sfx
    {
        Click, Error, DiceShake, DiceThrow, DiceBounce, Hop, CoinsIn, CoinsOut, Buy, Card, Jail, Bankrupt, Win, Build,
        Sell, Mortgage, Gavel, Turn, Doubles, Jackpot, Trade, Whoosh, Lose
    }

    /// <summary>
    /// The sounds and the music of the game. Clips are grouped by <see cref="Sfx"/> (a random clip of the group plays,
    /// slightly re-pitched, so repeated sounds do not grate); the music loops on a source of its own. Music and sound
    /// can be switched off; the choice is remembered.
    /// </summary>
    public class MonopolyAudio : MonoBehaviour
    {
        [Serializable]
        public class Bank
        {
            public Sfx id;
            public AudioClip[] clips = new AudioClip[0];
            [Range(0f, 1f)] public float volume = 1f;
            public float pitchJitter = 0.04f;
        }

        [SerializeField] private AudioSource effects;
        [SerializeField] private AudioSource music;
        [SerializeField] private AudioClip menuMusic;
        [SerializeField] private AudioClip gameMusic;
        [SerializeField] private float musicVolume = 0.32f;
        [SerializeField] private List<Bank> banks = new List<Bank>();

        private const string SoundKey = "Monopoly.Sound";
        private const string MusicKey = "Monopoly.Music";

        private readonly Dictionary<Sfx, Bank> lookup = new Dictionary<Sfx, Bank>();
        private readonly Dictionary<Sfx, float> lastPlayed = new Dictionary<Sfx, float>();
        private bool soundOn = true;
        private bool musicOn = true;
        private float musicFade = 1f;
        private float musicTarget = 1f;

        public bool SoundOn => soundOn;
        public bool MusicOn => musicOn;

        private void Awake()
        {
            foreach (Bank bank in banks)
            {
                lookup[bank.id] = bank;
            }
            soundOn = PlayerPrefs.GetInt(SoundKey, 1) == 1;
            musicOn = PlayerPrefs.GetInt(MusicKey, 1) == 1;
        }

        public void Play(Sfx id, float volume = 1f, float pitch = 1f)
        {
            if (!soundOn || effects == null || !lookup.TryGetValue(id, out Bank bank) || bank.clips.Length == 0)
            {
                return;
            }
            // The same sound twice in one frame (a burst of events) only plays once.
            float now = Time.unscaledTime;
            if (lastPlayed.TryGetValue(id, out float last) && now - last < 0.03f)
            {
                return;
            }
            lastPlayed[id] = now;
            AudioClip clip = bank.clips[UnityEngine.Random.Range(0, bank.clips.Length)];
            if (clip == null)
            {
                return;
            }
            effects.pitch = pitch * (1f + UnityEngine.Random.Range(-bank.pitchJitter, bank.pitchJitter));
            effects.PlayOneShot(clip, bank.volume * volume);
        }

        /// <summary>Plays the menu or the game loop (switching tracks with a short fade).</summary>
        public void PlayMusic(bool inGame)
        {
            AudioClip clip = inGame ? gameMusic : menuMusic;
            if (music == null || clip == null)
            {
                return;
            }
            if (music.clip != clip)
            {
                music.clip = clip;
                music.loop = true;
                music.time = 0f;
                musicFade = 0f;
                if (musicOn)
                {
                    music.Play();
                }
            }
            else if (musicOn && !music.isPlaying)
            {
                music.Play();
            }
        }

        /// <summary>Lowers the music under a jingle, then brings it back.</summary>
        public void Duck(float seconds)
        {
            musicTarget = 0.25f;
            CancelInvoke(nameof(Unduck));
            Invoke(nameof(Unduck), seconds);
        }

        private void Unduck()
        {
            musicTarget = 1f;
        }

        public void SetSound(bool value)
        {
            soundOn = value;
            PlayerPrefs.SetInt(SoundKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void SetMusic(bool value)
        {
            musicOn = value;
            PlayerPrefs.SetInt(MusicKey, value ? 1 : 0);
            PlayerPrefs.Save();
            if (music == null)
            {
                return;
            }
            if (value && music.clip != null && !music.isPlaying)
            {
                music.Play();
            }
            else if (!value)
            {
                music.Stop();
            }
        }

        private void Update()
        {
            if (music == null)
            {
                return;
            }
            musicFade = Mathf.MoveTowards(musicFade, musicTarget, Time.unscaledDeltaTime * 0.8f);
            music.volume = musicVolume * musicFade;
        }
    }
}
