using UnityEngine;

namespace Portfolio.Heroes
{
    /// <summary>
    /// The sound of the game: one source for the music that fades from one piece to the next, and a handful of sources
    /// the short sounds take turns on so several can be heard at once. Volumes come from the settings.
    /// </summary>
    public sealed class HeroesAudio : MonoBehaviour
    {
        private const int Voices = 6;

        [SerializeField] private HeroesArt art;
        [SerializeField] private HeroesSettings settings;

        private AudioSource music;
        private AudioSource[] voices;
        private int voice;
        private AudioClip playing;
        private float fade;
        private AudioClip next;

        public HeroesSettings Settings
        {
            get => settings;
            set
            {
                settings = value;
                ApplyVolume();
            }
        }

        private void Awake()
        {
            music = gameObject.AddComponent<AudioSource>();
            music.loop = true;
            music.playOnAwake = false;
            music.spatialBlend = 0f;
            voices = new AudioSource[Voices];
            for (int i = 0; i < Voices; i++)
            {
                voices[i] = gameObject.AddComponent<AudioSource>();
                voices[i].playOnAwake = false;
                voices[i].spatialBlend = 0f;
            }
            ApplyVolume();
        }

        public void ApplyVolume()
        {
            if (music != null && settings != null)
            {
                music.volume = settings.musicVolume;
            }
        }

        // ------------------------------------------------------------------ music

        public void PlayMenuMusic()
        {
            Play(art != null ? art.menuMusic : null);
        }

        public void PlayAdventureMusic()
        {
            Play(Pick(art != null ? art.adventureMusic : null));
        }

        public void PlayBattleMusic()
        {
            Play(Pick(art != null ? art.battleMusic : null));
        }

        public void PlayTownMusic(Faction faction)
        {
            AudioClip[] clips = art != null ? art.townMusic : null;
            if (clips != null && clips.Length > 0)
            {
                Play(clips[Mathf.Clamp((int)faction, 0, clips.Length - 1)]);
            }
        }

        public void PlayVictoryMusic()
        {
            Play(art != null ? art.victoryMusic : null, loop: false);
        }

        public void PlayDefeatMusic()
        {
            Play(art != null ? art.defeatMusic : null, loop: false);
        }

        private AudioClip Pick(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0)
            {
                return null;
            }
            // Never the same piece twice in a row while there is another to play.
            for (int tries = 0; tries < 4; tries++)
            {
                AudioClip clip = clips[Random.Range(0, clips.Length)];
                if (clip != playing || clips.Length == 1)
                {
                    return clip;
                }
            }
            return clips[0];
        }

        private void Play(AudioClip clip, bool loop = true)
        {
            if (clip == null || clip == playing)
            {
                return;
            }
            next = clip;
            music.loop = loop;
            if (!music.isPlaying)
            {
                Begin();
            }
        }

        private void Begin()
        {
            playing = next;
            next = null;
            music.clip = playing;
            music.volume = 0f;
            fade = 1f;
            music.Play();
        }

        private void Update()
        {
            float target = settings != null ? settings.musicVolume : 0.5f;
            if (next != null)
            {
                // Fade the old piece out, then bring the new one in.
                music.volume = Mathf.MoveTowards(music.volume, 0f, Time.unscaledDeltaTime * target);
                if (music.volume <= 0.001f)
                {
                    Begin();
                }
            }
            else if (fade > 0f)
            {
                music.volume = Mathf.MoveTowards(music.volume, target, Time.unscaledDeltaTime * target);
                if (Mathf.Approximately(music.volume, target))
                {
                    fade = 0f;
                }
            }
        }

        // ------------------------------------------------------------------ sounds

        public void Play(Sfx sfx, float volume = 1f)
        {
            AudioClip clip = art != null ? art.Sound(sfx) : null;
            if (clip == null)
            {
                return;
            }
            AudioSource source = voices[voice];
            voice = (voice + 1) % voices.Length;
            source.pitch = 1f;
            source.PlayOneShot(clip, volume * (settings != null ? settings.effectsVolume : 0.8f));
        }

        /// <summary>A sound with a little variation in pitch, for things heard over and over.</summary>
        public void PlayVaried(Sfx sfx, float volume = 1f, float spread = 0.12f)
        {
            AudioClip clip = art != null ? art.Sound(sfx) : null;
            if (clip == null)
            {
                return;
            }
            AudioSource source = voices[voice];
            voice = (voice + 1) % voices.Length;
            source.pitch = 1f + Random.Range(-spread, spread);
            source.PlayOneShot(clip, volume * (settings != null ? settings.effectsVolume : 0.8f));
        }

        public void Bind(HeroesArt catalog, HeroesSettings options)
        {
            art = catalog;
            Settings = options;
        }
    }
}
