using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// The sounds of the game: pooled one-shots placed in the world, the sounds of the interface, and the music. One on
    /// the manager's object, reached through <see cref="DeathRaceManager.Sounds"/> (and <see cref="Current"/>, and a
    /// race's <see cref="Race.Audio"/>). The listener rides the camera some forty metres above the road, so the world's
    /// sounds keep their full volume across the whole view and fade only beyond it, with soft panning and no doppler.
    /// The same clip is never stacked more than a few times at once (four guns firing do not deafen). The music
    /// crossfades between the menu's tune and the race's on its own (the manager's music source sets its volume, the
    /// settings' music volume), unless someone picks a clip with <see cref="PlayMusic"/>.
    /// </summary>
    public class RaceAudio : MonoBehaviour
    {
        private const int Voices = 28;
        private const int SameClipAtOnce = 5;
        private const float SameClipGap = 0.03f;

        /// <summary>The share of their volume the world's sounds keep behind the menu (the computer cars racing there).</summary>
        private const float MenuShare = 0.35f;

        /// <summary>Distances from the listener (the camera): full volume within <see cref="NearDistance"/>, silent at <see cref="FarDistance"/>.</summary>
        public const float NearDistance = 55f;

        public const float FarDistance = 150f;

        private readonly List<AudioSource> voices = new List<AudioSource>();
        private readonly Dictionary<AudioClip, float> lastPlayed = new Dictionary<AudioClip, float>();
        private readonly System.Random random = new System.Random(3);
        private AudioSource music;
        private AudioSource ui;
        private AudioSource[] tracks = new AudioSource[0];
        private float[] fades = new float[0];
        private int playing;
        private float fadeSeconds = 1f;
        private DeathRaceManager manager;
        private AnimationCurve rolloff;
        private bool warnedSilent;

        /// <summary>The game's sounds (the last set up), for parts that have no race to ask.</summary>
        public static RaceAudio Current { get; private set; }

        public RaceSounds Sounds { get; protected set; }

        /// <summary>
        /// Whether the music follows the game by itself: the menu's tune behind the menu, the race's in a race. Set false by
        /// <see cref="PlayMusic"/>; set it true again to hand the music back.
        /// </summary>
        public bool AutoMusic { get; set; } = true;

        /// <summary>Whether the race on is the one behind the menu (or none): its noise stays in the background.</summary>
        private bool BehindTheMenu => manager != null && !manager.IsRacing;

        /// <summary>How many of the world's one-shots are sounding now (development checks).</summary>
        public int VoicesPlaying
        {
            get
            {
                int playing = 0;
                foreach (AudioSource voice in voices)
                {
                    playing += voice != null && voice.isPlaying ? 1 : 0;
                }
                return playing;
            }
        }

        /// <summary>The clip the music plays now (null for silence).</summary>
        public AudioClip MusicClip => tracks.Length > 0 && tracks[playing] != null ? tracks[playing].clip : null;

        public virtual void Setup(RaceSounds sounds, AudioSource music)
        {
            Sounds = sounds ?? new RaceSounds();
            this.music = music;
            if (Sounds.gun == null && Sounds.explosion == null && !warnedSilent)
            {
                warnedSilent = true;
                Debug.LogWarning("[Death Race] The race's sounds are not built yet: the game plays silent.");
            }
            Current = this;
            manager = GetComponent<DeathRaceManager>();
            if (music != null)
            {
                // The manager's source is only the knob of the music's volume; two of our own play and crossfade.
                music.Stop();
                music.playOnAwake = false;
            }
            if (tracks.Length == 0)
            {
                tracks = new[] { MakeMusicTrack("Music A"), MakeMusicTrack("Music B") };
                fades = new float[tracks.Length];
            }
            if (ui == null)
            {
                ui = MakeSource("Interface", false);
                ui.ignoreListenerPause = true;
            }
        }

        private void Awake()
        {
            if (Current == null)
            {
                Current = this;
            }
        }

        private void OnDestroy()
        {
            if (Current == this)
            {
                Current = null;
            }
        }

        /// <summary>Plays <paramref name="clip"/> once at <paramref name="at"/> in the world.</summary>
        public virtual void Play(AudioClip clip, Vector3 at, float volume = 1f, float pitch = 1f)
        {
            if (clip == null || volume <= 0f || !Allowed(clip))
            {
                return;
            }
            AudioSource voice = FreeVoice();
            if (voice == null)
            {
                return;
            }
            voice.transform.position = at;
            voice.clip = clip;
            voice.volume = Mathf.Clamp01(volume) * (BehindTheMenu ? MenuShare : 1f);
            voice.pitch = Mathf.Clamp(pitch, 0.3f, 3f);
            voice.Play();
        }

        /// <summary>Plays <paramref name="clip"/> once without a place: the interface, the countdown, the jingles.</summary>
        public virtual void PlayUi(AudioClip clip, float volume = 1f)
        {
            if (clip == null)
            {
                return;
            }
            if (ui == null)
            {
                Setup(Sounds, music);
            }
            ui.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        /// <summary>
        /// Crossfades the music to <paramref name="clip"/> over <paramref name="fadeSeconds"/> (null fades to silence) and
        /// takes the music out of the game's hands until <see cref="AutoMusic"/> is set again.
        /// </summary>
        public virtual void PlayMusic(AudioClip clip, float fadeSeconds = 1f)
        {
            AutoMusic = false;
            Crossfade(clip, fadeSeconds);
        }

        private void Crossfade(AudioClip clip, float seconds)
        {
            if (tracks.Length == 0)
            {
                Setup(Sounds, music);
            }
            fadeSeconds = Mathf.Max(0.01f, seconds);
            if (tracks[playing].clip == clip && (clip == null || tracks[playing].isPlaying))
            {
                return;
            }
            playing = (playing + 1) % tracks.Length;
            AudioSource track = tracks[playing];
            track.clip = clip;
            if (clip != null)
            {
                track.time = 0f;
                track.Play();
            }
            else
            {
                track.Stop();
            }
        }

        private void Update()
        {
            if (tracks.Length == 0)
            {
                return;
            }
            if (AutoMusic && Sounds != null)
            {
                bool racing = manager != null && manager.IsRacing;
                Crossfade(racing ? Sounds.raceMusic : Sounds.menuMusic, 1.5f);
            }
            float master = music != null ? music.volume : 0.5f;
            float step = Time.unscaledDeltaTime / fadeSeconds;
            for (int i = 0; i < tracks.Length; i++)
            {
                AudioSource track = tracks[i];
                if (track == null)
                {
                    continue;
                }
                fades[i] = Mathf.MoveTowards(fades[i], i == playing && track.clip != null ? 1f : 0f, step);
                track.volume = master * fades[i];
                if (fades[i] <= 0f && track.isPlaying && i != playing)
                {
                    track.Stop();
                }
            }
        }

        // ------------------------------------------------------------------------------------------------
        // The common sounds, one line each for the rest of the game
        // ------------------------------------------------------------------------------------------------

        public void Gun(Vector3 at)
        {
            Play(Sounds?.gun, at, 0.5f, Vary(0.08f));
        }

        public void Ricochet(Vector3 at)
        {
            Play(Sounds?.ricochet, at, 0.4f, Vary(0.2f));
        }

        /// <summary>A crash; <paramref name="strength"/> 0 (a bump) to 1 (head on at full speed).</summary>
        public void Impact(Vector3 at, float strength)
        {
            Play(Sounds?.impact, at, Mathf.Lerp(0.35f, 1f, strength), Vary(0.1f) * Mathf.Lerp(1.15f, 0.85f, strength));
        }

        public void Scrape(Vector3 at, float strength)
        {
            Play(Sounds?.scrape, at, Mathf.Lerp(0.25f, 0.7f, strength), Vary(0.1f));
        }

        public void Explosion(Vector3 at, bool big)
        {
            Play(big && Sounds?.bigExplosion != null ? Sounds.bigExplosion : Sounds?.explosion, at, big ? 1f : 0.85f, Vary(0.08f));
        }

        public void MineDrop(Vector3 at)
        {
            Play(Sounds?.mineDrop, at, 0.7f, Vary(0.05f));
        }

        public void MineArm(Vector3 at)
        {
            Play(Sounds?.mineArm, at, 0.6f);
        }

        public void RocketLaunch(Vector3 at)
        {
            Play(Sounds?.rocketLaunch, at, 0.85f, Vary(0.06f));
        }

        public void Nitro(Vector3 at)
        {
            Play(Sounds?.nitro, at, 0.8f, Vary(0.04f));
        }

        /// <summary>One round of the minigun (the machine gun's crack when the minigun's own is missing).</summary>
        public void Minigun(Vector3 at)
        {
            Play(Sounds?.minigun != null ? Sounds.minigun : Sounds?.gun, at, 0.42f, Vary(0.06f) * (Sounds?.minigun != null ? 1f : 1.25f));
        }

        public void PlasmaShot(Vector3 at)
        {
            Play(Sounds?.plasmaShot, at, 0.6f, Vary(0.05f));
        }

        public void PlasmaBurst(Vector3 at)
        {
            Play(Sounds?.plasmaBurst, at, 0.55f, Vary(0.1f));
        }

        public void OilPour(Vector3 at)
        {
            Play(Sounds?.oilPour, at, 0.75f, Vary(0.05f));
        }

        public void Emp(Vector3 at)
        {
            Play(Sounds?.emp, at, 0.9f, Vary(0.03f));
        }

        public void MortarLaunch(Vector3 at)
        {
            Play(Sounds?.mortarLaunch, at, 0.85f, Vary(0.05f));
        }

        /// <summary>The whistle of a falling shell, played where it will land (it ends as the shell does).</summary>
        public void MortarWhistle(Vector3 at)
        {
            Play(Sounds?.mortarWhistle, at, 0.6f, Vary(0.03f));
        }

        /// <summary>A car was jammed: its electrics buzz and cough.</summary>
        public void Jammed(Vector3 at)
        {
            Play(Sounds?.jammed, at, 0.6f, Vary(0.06f));
        }

        /// <summary>
        /// A looped source on <paramref name="parent"/> for a sound that lasts while something goes on (the flamethrower):
        /// placed in the world like the one-shots, quieter behind the menu, silent until played. Null without the clip.
        /// </summary>
        public AudioSource LoopOn(Transform parent, AudioClip clip, string name)
        {
            if (parent == null || clip == null)
            {
                return null;
            }
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.clip = clip;
            source.dopplerLevel = 0f;
            source.spatialBlend = 1f;
            source.spread = 70f;
            source.rolloffMode = AudioRolloffMode.Custom;
            source.minDistance = 1f;
            source.maxDistance = FarDistance;
            source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, Rolloff);
            source.volume = 0f;
            return source;
        }

        /// <summary>The volume a loop of <paramref name="volume"/> plays at now (quieter behind the menu).</summary>
        public float LoopVolume(float volume)
        {
            return Mathf.Clamp01(volume) * (BehindTheMenu ? MenuShare : 1f);
        }

        public void Pickup(PickupKind kind, Vector3 at)
        {
            AudioClip clip;
            switch (kind)
            {
                case PickupKind.Cash: clip = Sounds?.pickupCash; break;
                case PickupKind.Repair: clip = Sounds?.pickupRepair; break;
                case PickupKind.Ammo: clip = Sounds?.pickupAmmo; break;
                default: clip = Sounds?.pickupNitro; break;
            }
            Play(clip, at, 0.8f);
        }

        public void Countdown()
        {
            PlayUi(Sounds?.countdown, 0.8f);
        }

        public void Go()
        {
            PlayUi(Sounds?.go, 0.9f);
        }

        public void Lap()
        {
            PlayUi(Sounds?.lap, 0.7f);
        }

        public void Finish()
        {
            PlayUi(Sounds?.finish, 0.9f);
        }

        public void WreckJingle()
        {
            PlayUi(Sounds?.wreckJingle, 0.8f);
        }

        public void Victory()
        {
            PlayUi(Sounds?.victory, 0.9f);
        }

        public void Buy()
        {
            PlayUi(Sounds?.buy, 0.8f);
        }

        public void Sell()
        {
            PlayUi(Sounds?.sell, 0.8f);
        }

        /// <summary>No: not enough money, nothing left to fire.</summary>
        public void Deny()
        {
            PlayUi(Sounds?.deny, 0.6f);
        }

        public void Click()
        {
            PlayUi(Sounds?.click, 0.6f);
        }

        // ------------------------------------------------------------------------------------------------
        // Voices
        // ------------------------------------------------------------------------------------------------

        private float Vary(float amount)
        {
            return 1f + ((float)random.NextDouble() * 2f - 1f) * amount;
        }

        /// <summary>Whether the clip may play again now: not within a moment of itself, not more than a few at once.</summary>
        private bool Allowed(AudioClip clip)
        {
            float now = Time.unscaledTime;
            if (lastPlayed.TryGetValue(clip, out float last) && now - last < SameClipGap)
            {
                return false;
            }
            int same = 0;
            foreach (AudioSource voice in voices)
            {
                if (voice != null && voice.isPlaying && voice.clip == clip)
                {
                    same++;
                }
            }
            if (same >= SameClipAtOnce)
            {
                return false;
            }
            lastPlayed[clip] = now;
            return true;
        }

        private AudioSource FreeVoice()
        {
            AudioSource oldest = null;
            foreach (AudioSource voice in voices)
            {
                if (voice == null)
                {
                    continue;
                }
                if (!voice.isPlaying)
                {
                    return voice;
                }
                if (oldest == null || voice.time > oldest.time)
                {
                    oldest = voice;
                }
            }
            if (voices.Count < Voices)
            {
                AudioSource made = MakeSource($"Voice {voices.Count + 1}", true);
                voices.Add(made);
                return made;
            }
            // Every voice busy: the one nearest its end makes way.
            return oldest;
        }

        private AudioSource MakeSource(string name, bool spatial)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.dopplerLevel = 0f;
            if (spatial)
            {
                source.spatialBlend = 1f;
                source.spread = 70f;
                source.rolloffMode = AudioRolloffMode.Custom;
                source.minDistance = 1f;
                source.maxDistance = FarDistance;
                source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, Rolloff);
            }
            else
            {
                source.spatialBlend = 0f;
            }
            return source;
        }

        /// <summary>Full volume as far as the camera sees, then a fade to nothing (distances as a share of the farthest).</summary>
        private AnimationCurve Rolloff => rolloff ??= new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(NearDistance / FarDistance, 1f),
            new Keyframe(0.62f, 0.45f),
            new Keyframe(0.82f, 0.12f),
            new Keyframe(1f, 0f));

        private AudioSource MakeMusicTrack(string name)
        {
            AudioSource track = MakeSource(name, false);
            track.loop = true;
            track.volume = 0f;
            track.priority = 0;
            // The music has its own volume in the settings; the sound volume (the listener's) leaves it alone.
            track.ignoreListenerVolume = true;
            track.ignoreListenerPause = true;
            return track;
        }
    }
}
