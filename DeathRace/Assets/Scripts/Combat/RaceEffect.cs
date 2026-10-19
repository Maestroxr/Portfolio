using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// One pooled visual effect: the particle systems of a prefab (sized and tinted per use), and a light that flashes
    /// and fades. A one-shot goes back to its pool when it has played; a loop (smoke, fire, the nitro flame) burns until
    /// <see cref="Stop"/>, then goes back once its last particle is gone. Systems on objects named with "Tint" take the
    /// colour of each use (the theme's explosion, sparks or muzzle colour); the others keep their own.
    /// </summary>
    public class RaceEffect : MonoBehaviour
    {
        private ParticleSystem[] systems = new ParticleSystem[0];
        private bool[] tinted = new bool[0];
        private ParticleSystem.MinMaxCurve[] baseSizes = new ParticleSystem.MinMaxCurve[0];
        private ParticleSystem.MinMaxCurve[] baseSpeeds = new ParticleSystem.MinMaxCurve[0];
        private float[] baseRadii = new float[0];
        private Light flash;
        private float flashIntensity;
        private float flashRange;
        private float flashTime = 0.25f;
        private float flashPeak;
        private float duration = 1f;
        private float age;
        private bool stopping;
        private bool prepared;
        private readonly Dictionary<ParticleSystem, float> rates = new Dictionary<ParticleSystem, float>();

        internal EffectPool Pool { get; set; }

        /// <summary>Whether the effect keeps burning until stopped.</summary>
        public bool Looping { get; private set; }

        public bool Playing => this != null && gameObject.activeSelf && !stopping;

        /// <summary>Reads the systems of the prefab once: their authored sizes and speeds are what each use scales.</summary>
        internal void Prepare()
        {
            if (prepared)
            {
                return;
            }
            prepared = true;
            systems = GetComponentsInChildren<ParticleSystem>(true);
            int count = systems.Length;
            tinted = new bool[count];
            baseSizes = new ParticleSystem.MinMaxCurve[count];
            baseSpeeds = new ParticleSystem.MinMaxCurve[count];
            baseRadii = new float[count];
            duration = 0.1f;
            for (int i = 0; i < count; i++)
            {
                ParticleSystem system = systems[i];
                ParticleSystem.MainModule main = system.main;
                main.playOnAwake = false;
                tinted[i] = system.gameObject.name.IndexOf("Tint", System.StringComparison.OrdinalIgnoreCase) >= 0;
                baseSizes[i] = main.startSize;
                baseSpeeds[i] = main.startSpeed;
                ParticleSystem.ShapeModule shape = system.shape;
                baseRadii[i] = shape.enabled ? shape.radius : 0f;
                if (main.loop)
                {
                    Looping = true;
                }
                else
                {
                    duration = Mathf.Max(duration, main.startDelay.constantMax + main.duration + main.startLifetime.constantMax);
                }
            }
            flash = GetComponentInChildren<Light>(true);
            if (flash != null)
            {
                flashIntensity = flash.intensity;
                flashRange = flash.range;
                flash.enabled = false;
            }
        }

        /// <summary>
        /// Plays the effect at <paramref name="position"/> facing <paramref name="rotation"/>, <paramref name="size"/> times
        /// its authored size, the tinted systems in <paramref name="tint"/>. <paramref name="lightScale"/> brightens the
        /// flash (more at night).
        /// </summary>
        public void Play(Vector3 position, Quaternion rotation, float size, Color tint, float lightScale = 1f)
        {
            Prepare();
            transform.SetPositionAndRotation(position, rotation);
            gameObject.SetActive(true);
            age = 0f;
            stopping = false;
            float scale = Mathf.Max(0.05f, size);
            for (int i = 0; i < systems.Length; i++)
            {
                ParticleSystem system = systems[i];
                if (system == null)
                {
                    continue;
                }
                system.Clear(false);
                ParticleSystem.MainModule main = system.main;
                main.startSize = Scaled(baseSizes[i], scale);
                main.startSpeed = Scaled(baseSpeeds[i], Mathf.Sqrt(scale));
                if (baseRadii[i] > 0f)
                {
                    ParticleSystem.ShapeModule shape = system.shape;
                    shape.radius = baseRadii[i] * scale;
                }
                if (tinted[i])
                {
                    main.startColor = new ParticleSystem.MinMaxGradient(tint, Color.Lerp(tint, Color.white, 0.4f));
                }
            }
            foreach (ParticleSystem system in systems)
            {
                if (system != null)
                {
                    system.Play(false);
                }
            }
            if (flash != null)
            {
                flash.enabled = lightScale > 0f;
                flash.color = Color.Lerp(tint, Color.white, 0.3f);
                flash.range = flashRange * Mathf.Sqrt(scale);
                flashPeak = flashIntensity * lightScale;
                flash.intensity = flashPeak;
                flashTime = 0.18f + 0.1f * Mathf.Sqrt(scale);
            }
        }

        /// <summary>A curve of the main module times <paramref name="scale"/>, whatever its mode (the multiplier alone only scales the top of a range).</summary>
        private static ParticleSystem.MinMaxCurve Scaled(ParticleSystem.MinMaxCurve curve, float scale)
        {
            switch (curve.mode)
            {
                case ParticleSystemCurveMode.Constant:
                    return new ParticleSystem.MinMaxCurve(curve.constant * scale);
                case ParticleSystemCurveMode.TwoConstants:
                    return new ParticleSystem.MinMaxCurve(curve.constantMin * scale, curve.constantMax * scale);
                case ParticleSystemCurveMode.Curve:
                    return new ParticleSystem.MinMaxCurve(curve.curveMultiplier * scale, curve.curve);
                default:
                    return new ParticleSystem.MinMaxCurve(curve.curveMultiplier * scale, curve.curveMin, curve.curveMax);
            }
        }

        /// <summary>Keeps the effect on <paramref name="parent"/> (a car, a muzzle, an exhaust) until it ends.</summary>
        public void Follow(Transform parent, Vector3 localPosition, Quaternion localRotation)
        {
            if (parent == null)
            {
                return;
            }
            transform.SetParent(parent, false);
            transform.localPosition = localPosition;
            transform.localRotation = localRotation;
        }

        /// <summary>Stops emitting; the effect goes back to its pool once what it emitted has faded.</summary>
        public void Stop()
        {
            if (this == null || !gameObject.activeSelf || stopping)
            {
                return;
            }
            stopping = true;
            age = 0f;
            foreach (ParticleSystem system in systems)
            {
                if (system != null)
                {
                    system.Stop(false, ParticleSystemStopBehavior.StopEmitting);
                }
            }
            if (flash != null)
            {
                flash.enabled = false;
            }
        }

        /// <summary>Changes how hard a loop burns (0 to 1 and more) without starting it again: smoke thickens as a car is hurt.</summary>
        public void SetRate(float multiplier)
        {
            Prepare();
            foreach (ParticleSystem system in systems)
            {
                if (system == null)
                {
                    continue;
                }
                ParticleSystem.EmissionModule emission = system.emission;
                emission.rateOverTimeMultiplier = Mathf.Max(0f, multiplier) * RateOf(system);
            }
        }

        private float RateOf(ParticleSystem system)
        {
            if (!rates.TryGetValue(system, out float rate))
            {
                rate = system.emission.rateOverTimeMultiplier;
                rates[system] = rate;
            }
            return rate;
        }

        private void Update()
        {
            age += Time.deltaTime;
            if (flash != null && flash.enabled)
            {
                float t = Mathf.Clamp01(age / flashTime);
                flash.intensity = flashPeak * (1f - t) * (1f - t);
                if (t >= 1f)
                {
                    flash.enabled = false;
                }
            }
            if (stopping)
            {
                if (!AnyAlive() || age > 8f)
                {
                    Release();
                }
                return;
            }
            if (!Looping && age >= duration)
            {
                Release();
            }
        }

        private bool AnyAlive()
        {
            foreach (ParticleSystem system in systems)
            {
                if (system != null && system.IsAlive(false))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Back to the pool (or away, when the pool is gone with its race).</summary>
        internal void Release()
        {
            stopping = false;
            if (Pool != null && Pool.Owns)
            {
                Pool.Give(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            Pool?.Forget(this);
        }
    }

    /// <summary>
    /// The instances of one effect: handed out by <see cref="Take"/>, back by <see cref="Give"/>, at most a cap of them
    /// at once so a pile-up cannot flood the frame (the extra uses are skipped).
    /// </summary>
    internal sealed class EffectPool
    {
        private readonly GameObject prefab;
        private readonly Transform home;
        private readonly int cap;
        private readonly List<RaceEffect> free = new List<RaceEffect>();
        private int made;

        public EffectPool(GameObject prefab, Transform home, int cap)
        {
            this.prefab = prefab;
            this.home = home;
            this.cap = cap;
        }

        public GameObject Prefab => prefab;

        /// <summary>Whether the pool still lives (its race was not torn down).</summary>
        public bool Owns => home != null;

        public RaceEffect Take()
        {
            while (free.Count > 0)
            {
                RaceEffect waiting = free[free.Count - 1];
                free.RemoveAt(free.Count - 1);
                if (waiting != null)
                {
                    return waiting;
                }
            }
            if (prefab == null || home == null || made >= cap)
            {
                return null;
            }
            GameObject instance = Object.Instantiate(prefab, home, false);
            instance.name = prefab.name;
            instance.SetActive(false);
            RaceEffect effect = instance.GetComponent<RaceEffect>();
            if (effect == null)
            {
                effect = instance.AddComponent<RaceEffect>();
            }
            effect.Pool = this;
            effect.Prepare();
            made++;
            return effect;
        }

        public void Give(RaceEffect effect)
        {
            if (effect == null)
            {
                return;
            }
            effect.gameObject.SetActive(false);
            effect.transform.SetParent(home, false);
            if (!free.Contains(effect))
            {
                free.Add(effect);
            }
        }

        public void Forget(RaceEffect effect)
        {
            free.Remove(effect);
            made = Mathf.Max(0, made - 1);
        }
    }
}
