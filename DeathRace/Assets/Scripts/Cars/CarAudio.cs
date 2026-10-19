using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// The sound of one car: its engine loop, pitched by the speed through five gears (it drops at each change up and
    /// climbs again) and lifted by the throttle, so a car revving on the grid, pulling away and backing off all sound
    /// different; and the squeal of its tyres while it slides. Heavier cars sound deeper. Both loops are partly placed in
    /// the world, so the cars near the camera are the loud ones, and they pause with the game. A missing clip is silence.
    /// </summary>
    public class CarAudio : MonoBehaviour
    {
        /// <summary>Where each gear ends, as a share of the top speed.</summary>
        private static readonly float[] GearTops = { 0.2f, 0.38f, 0.56f, 0.76f, 1.4f };

        private static bool warned;

        private Car car;
        private AudioSource engine;
        private AudioSource skid;
        private float basePitch = 1f;
        private float loudness = 1f;
        private float pitch = 0.6f;
        private float skidVolume;
        private bool paused;
        private bool silenced;

        /// <summary>
        /// Starts the loops of <paramref name="owner"/> from <paramref name="sounds"/>; <paramref name="volume"/> scales them
        /// (quieter for the computer cars behind the menu).
        /// </summary>
        public void Setup(Car owner, RaceSounds sounds, float volume = 1f)
        {
            car = owner;
            loudness = Mathf.Clamp01(volume);
            float heft = owner != null ? Mathf.InverseLerp(1100f, 1700f, owner.Stats.Mass) : 0f;
            basePitch = Mathf.Lerp(1.1f, 0.82f, heft);
            AudioClip engineClip = sounds != null ? sounds.engine : null;
            AudioClip skidClip = sounds != null ? sounds.skid : null;
            if ((engineClip == null || skidClip == null) && !warned)
            {
                warned = true;
                Debug.LogWarning("Death Race: the engine or skid sound is missing; the cars drive silently.");
            }
            engine = Loop(engineClip, "Engine");
            skid = Loop(skidClip, "Skid");
            if (engine != null)
            {
                // Every engine starts at its own point of the loop, so a grid of the same car does not drone in phase.
                engine.time = Mathf.Repeat((owner != null && owner.Entrant != null ? owner.Entrant.Seat + 1 : 0) * 0.737f, Mathf.Max(0.01f, engineClip.length));
                engine.Play();
            }
        }

        /// <summary>The car blew up: the engine dies away, the tyres are quiet.</summary>
        public void Wreck()
        {
            silenced = true;
            if (skid != null)
            {
                skid.Stop();
            }
        }

        private void Update()
        {
            if (car == null || car.Body == null)
            {
                return;
            }
            bool stopped = Time.timeScale <= 0f;
            if (stopped != paused)
            {
                paused = stopped;
                Pause(engine, paused);
                Pause(skid, paused);
            }
            if (paused)
            {
                return;
            }
            float deltaTime = Time.deltaTime;
            if (engine != null)
            {
                if (silenced)
                {
                    engine.volume = Mathf.MoveTowards(engine.volume, 0f, deltaTime * 1.5f);
                    engine.pitch = Mathf.MoveTowards(engine.pitch, 0.3f, deltaTime * 0.8f);
                    if (engine.volume <= 0f)
                    {
                        engine.Stop();
                    }
                }
                else
                {
                    float throttle = Mathf.Abs(car.Input.Throttle);
                    float target = basePitch * EnginePitch(Mathf.Abs(car.Body.ForwardSpeed) / Mathf.Max(1f, car.Stats.TopSpeed), throttle, car.Body.Held);
                    if (car.Body.Nitro)
                    {
                        target += 0.12f;
                    }
                    // Up quickly, down a little slower: a gear change drops, then the revs climb again.
                    pitch = Mathf.MoveTowards(pitch, target, (target > pitch ? 2.4f : 3.5f) * deltaTime);
                    engine.pitch = pitch;
                    float share = Mathf.Clamp01(Mathf.Abs(car.Body.ForwardSpeed) / Mathf.Max(1f, car.Stats.TopSpeed));
                    engine.volume = loudness * (car.IsHuman ? 1f : 0.75f) * (0.32f + 0.42f * throttle + 0.26f * share);
                }
            }
            if (skid != null && !silenced)
            {
                float speed = car.Body.Speed;
                float wanted = speed > 4f ? Mathf.Clamp01((car.Body.Slip - 0.15f) / 0.6f) : 0f;
                skidVolume = Mathf.MoveTowards(skidVolume, wanted, deltaTime * 5f);
                skid.volume = loudness * 0.7f * skidVolume;
                skid.pitch = Mathf.Lerp(0.85f, 1.15f, Mathf.Clamp01(speed / 40f));
                if (skidVolume > 0.01f && !skid.isPlaying)
                {
                    skid.Play();
                }
                else if (skidVolume <= 0.01f && skid.isPlaying)
                {
                    skid.Stop();
                }
            }
        }

        /// <summary>
        /// The engine's pitch at <paramref name="speedShare"/> of the top speed: idling low, climbing through each gear and
        /// dropping at the change; the throttle lifts it (and revs it on the grid while <paramref name="held"/>).
        /// </summary>
        public static float EnginePitch(float speedShare, float throttle, bool held)
        {
            if (held)
            {
                return 0.62f + 0.75f * throttle;
            }
            float low = 0f;
            for (int gear = 0; gear < GearTops.Length; gear++)
            {
                float high = GearTops[gear];
                if (speedShare < high || gear == GearTops.Length - 1)
                {
                    float within = Mathf.Clamp01((speedShare - low) / (high - low));
                    return 0.62f + 0.62f * within + 0.07f * gear + 0.1f * throttle;
                }
                low = high;
            }
            return 1f;
        }

        private AudioSource Loop(AudioClip clip, string sourceName)
        {
            if (clip == null)
            {
                return null;
            }
            var holder = new GameObject(sourceName);
            holder.transform.SetParent(transform, false);
            var source = holder.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0.6f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 25f;
            source.maxDistance = 150f;
            source.dopplerLevel = 0f;
            source.volume = 0f;
            return source;
        }

        private static void Pause(AudioSource source, bool pause)
        {
            if (source == null)
            {
                return;
            }
            if (pause)
            {
                source.Pause();
            }
            else
            {
                source.UnPause();
            }
        }
    }
}
