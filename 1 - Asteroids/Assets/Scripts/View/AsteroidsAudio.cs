using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// Sound of the game: a small pool of voices for the effects (with a little pitch variation and a limit on how
    /// often one clip can restart, so a chain reaction does not turn into noise) and two music sources that crossfade
    /// between the menu, battle and boss themes. The music ducks while the game is paused.
    /// </summary>
    public class AsteroidsAudio : MonoBehaviour
    {
        [Header("Weapons")]
        [SerializeField] internal AudioClip blaster;
        [SerializeField] internal AudioClip laser;
        [SerializeField] internal AudioClip scatter;
        [SerializeField] internal AudioClip missile;
        [SerializeField] internal AudioClip droneShot;
        [SerializeField] internal AudioClip enemyShot;
        [SerializeField] internal AudioClip nova;
        [SerializeField] internal AudioClip dash;
        [SerializeField] internal AudioClip denied;

        [Header("Impacts")]
        [SerializeField] internal AudioClip rockBreak;
        [SerializeField] internal AudioClip rockBreakSmall;
        [SerializeField] internal AudioClip iceBreak;
        [SerializeField] internal AudioClip crystalBreak;
        [SerializeField] internal AudioClip explosion;
        [SerializeField] internal AudioClip bigExplosion;
        [SerializeField] internal AudioClip hullHit;
        [SerializeField] internal AudioClip shieldHit;
        [SerializeField] internal AudioClip shieldDown;
        [SerializeField] internal AudioClip shipExplode;
        [SerializeField] internal AudioClip enemyExplode;

        [Header("Pickups")]
        [SerializeField] internal AudioClip pickup;
        [SerializeField] internal AudioClip crystal;
        [SerializeField] internal AudioClip powerUp;
        [SerializeField] internal AudioClip weaponUp;
        [SerializeField] internal AudioClip extraLife;
        [SerializeField] internal AudioClip podOpen;

        [Header("Hazards")]
        [SerializeField] internal AudioClip mineBeep;
        [SerializeField] internal AudioClip bombTick;
        [SerializeField] internal AudioClip warning;
        [SerializeField] internal AudioClip cometPass;
        [SerializeField] internal AudioClip warpIn;
        [SerializeField] internal AudioClip wellOpen;
        [SerializeField] internal AudioClip bossRoar;
        [SerializeField] internal AudioClip respawn;

        [Header("Interface")]
        [SerializeField] internal AudioClip countdown;
        [SerializeField] internal AudioClip go;
        [SerializeField] internal AudioClip waveStart;
        [SerializeField] internal AudioClip waveClear;
        [SerializeField] internal AudioClip victory;
        [SerializeField] internal AudioClip gameOver;
        [SerializeField] internal AudioClip star;
        [SerializeField] internal AudioClip click;
        [SerializeField] internal AudioClip combo;

        [Header("Music")]
        [SerializeField] internal AudioClip menuMusic;
        [SerializeField] internal AudioClip battleMusic;
        [SerializeField] internal AudioClip bossMusic;
        [SerializeField] internal float musicVolume = 0.45f;
        [SerializeField] internal float effectsVolume = 0.85f;
        [SerializeField] internal int voices = 16;
        [SerializeField] internal float crossfade = 1.2f;

        private readonly Dictionary<AudioClip, float> lastPlayed = new Dictionary<AudioClip, float>();
        private AudioSource[] sources = new AudioSource[0];
        private int nextVoice;
        private AudioSource musicA;
        private AudioSource musicB;
        private AudioSource currentMusic;
        private float fade = 1f;
        private float duck = 1f;
        private float duckTarget = 1f;

        public AudioClip MenuMusic => menuMusic;
        public AudioClip BattleMusic => battleMusic;
        public AudioClip BossMusic => bossMusic;


        private void Awake()
        {
            sources = new AudioSource[Mathf.Max(1, voices)];
            for (int i = 0; i < sources.Length; i++)
            {
                sources[i] = CreateSource(false);
            }
            musicA = CreateSource(true);
            musicB = CreateSource(true);
            currentMusic = musicA;
        }


        private AudioSource CreateSource(bool loop)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            source.volume = loop ? 0f : effectsVolume;
            return source;
        }


        /// <summary>Plays <paramref name="clip"/> on a free voice unless it restarted a moment ago.</summary>
        public void Play(AudioClip clip, float volume = 1f, float pitch = 1f, float variance = 0.05f, float minGap = 0.045f)
        {
            if (clip == null || sources.Length == 0)
            {
                return;
            }
            float now = Time.unscaledTime;
            if (lastPlayed.TryGetValue(clip, out float last) && now - last < minGap)
            {
                return;
            }
            lastPlayed[clip] = now;
            AudioSource source = sources[nextVoice];
            nextVoice = (nextVoice + 1) % sources.Length;
            source.clip = clip;
            source.volume = Mathf.Clamp01(volume) * effectsVolume;
            source.pitch = pitch * (1f + Random.Range(-variance, variance));
            source.Play();
        }


        public void PlayMusic(AudioClip clip)
        {
            if (clip == null || musicA == null)
            {
                return;
            }
            if (currentMusic != null && currentMusic.clip == clip && currentMusic.isPlaying)
            {
                return;
            }
            AudioSource next = currentMusic == musicA ? musicB : musicA;
            next.clip = clip;
            next.volume = 0f;
            next.Play();
            currentMusic = next;
            fade = 0f;
        }


        public void StopMusic()
        {
            PlayMusic(null);
            if (musicA != null)
            {
                musicA.Stop();
            }
            if (musicB != null)
            {
                musicB.Stop();
            }
        }


        /// <summary>Lowers the music while paused.</summary>
        public void Duck(bool ducked)
        {
            duckTarget = ducked ? 0.35f : 1f;
        }


        private void Update()
        {
            float deltaTime = Time.unscaledDeltaTime;
            duck = Mathf.MoveTowards(duck, duckTarget, deltaTime * 2f);
            if (musicA == null)
            {
                return;
            }
            fade = Mathf.Min(1f, fade + deltaTime / Mathf.Max(0.01f, crossfade));
            AudioSource other = currentMusic == musicA ? musicB : musicA;
            currentMusic.volume = musicVolume * duck * fade;
            other.volume = musicVolume * duck * (1f - fade);
            if (fade >= 1f && other.isPlaying)
            {
                other.Stop();
            }
        }


        // ------------------------------------------------------------------ game sounds

        public void Fire(WeaponType type)
        {
            switch (type)
            {
                case WeaponType.Laser: Play(laser, 0.55f, 1f, 0.04f); break;
                case WeaponType.Scatter: Play(scatter, 0.65f); break;
                case WeaponType.Missiles: Play(missile, 0.6f); break;
                default: Play(blaster, 0.45f, 1f, 0.08f); break;
            }
        }

        public void DroneShot() => Play(droneShot, 0.25f, 1.2f, 0.1f);
        public void EnemyShot() => Play(enemyShot, 0.45f, 1f, 0.08f, 0.07f);
        public void MissileLaunch() => Play(missile, 0.6f, 0.8f);
        public void Nova() => Play(nova, 1f, 1f, 0f);
        public void Dash() => Play(dash, 0.7f);
        public void Denied() => Play(denied, 0.5f, 1f, 0f, 0.2f);

        public void RockBreak(AsteroidKind kind, AsteroidSize size)
        {
            float pitch = size == AsteroidSize.Large ? 0.8f : size == AsteroidSize.Medium ? 1f : 1.25f;
            switch (kind)
            {
                case AsteroidKind.Ice: Play(iceBreak, 0.7f, pitch, 0.08f); break;
                case AsteroidKind.Crystal: Play(crystalBreak, 0.7f, pitch, 0.08f); break;
                case AsteroidKind.Magma: Play(explosion, 0.8f, pitch * 0.9f, 0.08f); break;
                default: Play(size == AsteroidSize.Small ? rockBreakSmall : rockBreak, 0.7f, pitch, 0.1f); break;
            }
        }

        public void Explosion(float intensity) => Play(intensity > 1.2f ? bigExplosion : explosion, Mathf.Clamp01(0.5f + intensity * 0.4f), 1f, 0.12f, 0.06f);
        public void EnemyExplode() => Play(enemyExplode, 0.85f);
        public void HullHit() => Play(hullHit, 0.8f);
        public void ShieldHit(bool broken) => Play(broken ? shieldDown : shieldHit, 0.75f);
        public void ShipExplode() => Play(shipExplode, 1f, 1f, 0f);
        public void Respawn() => Play(respawn, 0.8f, 1f, 0f);

        public void Pickup(Reward reward)
        {
            switch (reward)
            {
                case PointReward _: Play(crystal, 0.5f, 1f, 0.12f, 0.03f); break;
                case WeaponReward _: Play(weaponUp, 0.9f, 1f, 0f); break;
                case PowerUpReward _: Play(powerUp, 0.85f, 1f, 0f); break;
                case LifeReward _: Play(extraLife, 1f, 1f, 0f); break;
                default: Play(pickup, 0.8f, 1f, 0.02f); break;
            }
        }

        public void PodOpen() => Play(podOpen, 0.7f);
        public void MineBeep(float urgency) => Play(mineBeep, 0.45f, 1f + urgency * 0.6f, 0f, 0.03f);
        public void BombTick() => Play(bombTick, 0.55f, 1f, 0f, 0.1f);
        public void Warning() => Play(warning, 0.7f, 1f, 0f, 0.4f);
        public void CometPass() => Play(cometPass, 0.8f);
        public void WarpIn() => Play(warpIn, 0.5f, 1f, 0.1f, 0.1f);
        public void WellOpen() => Play(wellOpen, 0.8f, 1f, 0f);
        public void BossRoar(float intensity) => Play(bossRoar, Mathf.Clamp01(0.5f + intensity * 0.4f), 1.1f - intensity * 0.2f, 0.03f, 0.3f);

        public void Countdown() => Play(countdown, 0.7f, 1f, 0f, 0f);
        public void Go() => Play(go, 0.8f, 1f, 0f, 0f);
        public void WaveStart() => Play(waveStart, 0.7f, 1f, 0f);
        public void WaveClear() => Play(waveClear, 0.8f, 1f, 0f);
        public void Victory() => Play(victory, 1f, 1f, 0f);
        public void GameOver() => Play(gameOver, 1f, 1f, 0f);
        public void Star(int index) => Play(star, 0.9f, 1f + index * 0.12f, 0f, 0f);
        public void Click() => Play(click, 0.6f, 1f, 0f, 0.02f);
        public void Combo(int multiplier) => Play(combo, 0.6f, 0.9f + multiplier * 0.1f, 0f, 0.1f);
    }
}
