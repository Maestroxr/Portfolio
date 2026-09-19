using System;
using System.IO;
using UnityEngine;

namespace Portfolio.Asteroids.EditorTools
{
    /// <summary>
    /// A small synthesizer for the Asteroids module's sound effects and its three music loops: oscillators with pitch
    /// slides, vibrato, FM and a one-pole low-pass, filtered noise, drums and a feedback echo. Everything is mono
    /// 22 kHz, 16-bit PCM written as WAV files. Loop buffers wrap notes that ring past their end, so loops are seamless.
    /// </summary>
    internal static class SpaceSounds
    {
        public const int SampleRate = 22050;

        private enum Wave
        {
            Sine,
            Square,
            Pulse,
            Triangle,
            Saw,
            Noise
        }

        private sealed class Buffer
        {
            public readonly float[] Samples;
            public readonly bool Loop;

            public Buffer(float seconds, bool loop = false)
            {
                Samples = new float[Mathf.CeilToInt(seconds * SampleRate)];
                Loop = loop;
            }

            public void Add(int index, float value)
            {
                if (Loop)
                {
                    index %= Samples.Length;
                    if (index < 0)
                    {
                        index += Samples.Length;
                    }
                }
                else if (index >= Samples.Length || index < 0)
                {
                    return;
                }
                Samples[index] += value;
            }
        }

        private static uint noiseState = 22222;

        private static float NextNoise()
        {
            noiseState ^= noiseState << 13;
            noiseState ^= noiseState >> 17;
            noiseState ^= noiseState << 5;
            return (noiseState & 0xFFFF) / 32767.5f - 1f;
        }

        private static float Oscillator(Wave wave, float phase)
        {
            float t = phase - Mathf.Floor(phase);
            switch (wave)
            {
                case Wave.Sine: return Mathf.Sin(t * Mathf.PI * 2f);
                case Wave.Square: return t < 0.5f ? 0.8f : -0.8f;
                case Wave.Pulse: return t < 0.25f ? 0.8f : -0.8f;
                case Wave.Triangle: return 1f - 4f * Mathf.Abs(t - 0.5f);
                case Wave.Saw: return 2f * t - 1f;
                default: return NextNoise();
            }
        }

        private static float Midi(int note)
        {
            return 440f * Mathf.Pow(2f, (note - 69) / 12f);
        }

        /// <summary>
        /// One tone: frequency slides from <paramref name="from"/> to <paramref name="to"/> Hz with an attack, an
        /// exponential decay, optional vibrato, FM (a sine modulator at <paramref name="fmRatio"/> times the frequency)
        /// and a one-pole low-pass at <paramref name="lowpass"/> Hz (0 = off). Noise uses the frequencies as its cutoff.
        /// </summary>
        private static void Tone(Buffer buffer, float start, float duration, Wave wave, float from, float to, float volume,
            float attack = 0.005f, float decay = 8f, float vibrato = 0f, float vibratoRate = 6f, float release = 0.02f,
            float fmRatio = 0f, float fmDepth = 0f, float lowpass = 0f)
        {
            int first = Mathf.RoundToInt(start * SampleRate);
            int count = Mathf.RoundToInt(duration * SampleRate);
            float phase = 0f;
            float modPhase = 0f;
            float filtered = 0f;
            float lowpassState = 0f;
            float lpCoefficient = lowpass > 0f ? 1f - Mathf.Exp(-2f * Mathf.PI * lowpass / SampleRate) : 1f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float progress = i / (float)count;
                float frequency = Mathf.Lerp(from, to, progress) * (1f + vibrato * Mathf.Sin(t * vibratoRate * Mathf.PI * 2f));
                float envelope = Mathf.Min(1f, t / Mathf.Max(attack, 0.0001f)) * Mathf.Exp(-decay * t);
                float tail = duration - t;
                if (tail < release)
                {
                    envelope *= Mathf.Max(0f, tail / release);
                }
                float sample;
                if (wave == Wave.Noise)
                {
                    float cutoff = Mathf.Clamp01(frequency / SampleRate * 6f);
                    filtered += (NextNoise() - filtered) * cutoff;
                    sample = filtered * 2f;
                }
                else
                {
                    float modulation = 0f;
                    if (fmRatio > 0f)
                    {
                        modPhase += frequency * fmRatio / SampleRate;
                        modulation = Mathf.Sin(modPhase * Mathf.PI * 2f) * fmDepth;
                    }
                    phase += frequency / SampleRate;
                    sample = Oscillator(wave, phase + modulation);
                }
                if (lowpass > 0f)
                {
                    lowpassState += (sample - lowpassState) * lpCoefficient;
                    sample = lowpassState;
                }
                buffer.Add(first + i, sample * envelope * volume);
            }
        }

        /// <summary>Noise through a band that sweeps from <paramref name="from"/> to <paramref name="to"/> Hz (a whoosh).</summary>
        private static void Sweep(Buffer buffer, float start, float duration, float from, float to, float volume, float attack = 0.05f, float decay = 3f)
        {
            int first = Mathf.RoundToInt(start * SampleRate);
            int count = Mathf.RoundToInt(duration * SampleRate);
            float low = 0f;
            float band = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float progress = i / (float)count;
                float frequency = Mathf.Lerp(from, to, progress);
                float a = Mathf.Clamp01(frequency / SampleRate * 6f);
                float noise = NextNoise();
                low += (noise - low) * a;
                band += ((noise - low) - band) * a;
                float envelope = Mathf.Min(1f, t / Mathf.Max(attack, 0.0001f)) * Mathf.Exp(-decay * t) * Mathf.Min(1f, (duration - t) / 0.05f);
                buffer.Add(first + i, band * 2.5f * envelope * volume);
            }
        }

        /// <summary>Feedback echo over the whole buffer (wrapping for loops).</summary>
        private static void Echo(Buffer buffer, float delay, float feedback, int repeats = 4)
        {
            int offset = Mathf.RoundToInt(delay * SampleRate);
            float[] source = (float[])buffer.Samples.Clone();
            float gain = feedback;
            for (int r = 1; r <= repeats; r++)
            {
                for (int i = 0; i < source.Length; i++)
                {
                    buffer.Add(i + offset * r, source[i] * gain);
                }
                gain *= feedback;
            }
        }

        private static void Kick(Buffer buffer, float start, float volume)
        {
            Tone(buffer, start, 0.2f, Wave.Sine, 160f, 40f, volume, 0.002f, 12f);
            Tone(buffer, start, 0.02f, Wave.Noise, 5000f, 2000f, volume * 0.25f, 0.001f, 60f);
        }

        private static void Snare(Buffer buffer, float start, float volume)
        {
            Tone(buffer, start, 0.18f, Wave.Noise, 9000f, 5000f, volume, 0.001f, 18f);
            Tone(buffer, start, 0.09f, Wave.Triangle, 220f, 170f, volume * 0.5f, 0.001f, 28f);
        }

        private static void Hat(Buffer buffer, float start, float volume, float length = 0.04f)
        {
            int first = Mathf.RoundToInt(start * SampleRate);
            int count = Mathf.RoundToInt(length * SampleRate);
            float previous = 0f;
            for (int i = 0; i < count; i++)
            {
                float noise = NextNoise();
                float highpass = noise - previous;
                previous = noise;
                buffer.Add(first + i, highpass * Mathf.Exp(-80f * i / SampleRate) * volume);
            }
        }

        private static void Boom(Buffer buffer, float start, float length, float volume, float cutoff = 4000f)
        {
            Tone(buffer, start, length, Wave.Noise, cutoff, 180f, volume, 0.002f, 3.2f / length);
            Tone(buffer, start, length * 0.8f, Wave.Sine, 90f, 32f, volume * 0.9f, 0.003f, 3f / length);
            for (int i = 0; i < 8; i++)
            {
                float at = start + 0.02f + i * length * 0.07f + (NextNoise() + 1f) * 0.02f;
                Tone(buffer, at, 0.03f, Wave.Noise, 7000f, 3000f, volume * 0.18f * (1f - i / 8f), 0.001f, 60f);
            }
        }

        // ------------------------------------------------------------------ effects

        public static readonly string[] EffectNames =
        {
            "Blaster", "Laser", "Scatter", "Missile", "DroneShot", "EnemyShot", "Nova", "Dash", "Denied",
            "RockBreak", "RockBreakSmall", "IceBreak", "CrystalBreak", "Explosion", "BigExplosion", "HullHit", "ShieldHit",
            "ShieldDown", "ShipExplode", "EnemyExplode",
            "Pickup", "Crystal", "PowerUp", "WeaponUp", "ExtraLife", "PodOpen",
            "MineBeep", "BombTick", "Warning", "CometPass", "WarpIn", "WellOpen", "BossRoar", "Respawn",
            "Countdown", "Go", "WaveStart", "WaveClear", "Victory", "GameOver", "Star", "Click", "Combo",
            "SaucerHum", "WellHum"
        };

        public static float[] Effect(string name)
        {
            noiseState = 22222;
            Buffer b;
            switch (name)
            {
                case "Blaster":
                    b = new Buffer(0.18f);
                    Tone(b, 0f, 0.12f, Wave.Pulse, 1700f, 480f, 0.4f, 0.001f, 14f);
                    Tone(b, 0f, 0.14f, Wave.Sine, 900f, 260f, 0.45f, 0.001f, 10f);
                    Tone(b, 0f, 0.015f, Wave.Noise, 8000f, 6000f, 0.25f, 0.001f, 80f);
                    break;
                case "Laser":
                    b = new Buffer(0.3f);
                    Tone(b, 0f, 0.24f, Wave.Saw, 2600f, 700f, 0.25f, 0.002f, 7f, 0.02f, 40f, 0.02f, 0f, 0f, 5000f);
                    Tone(b, 0f, 0.22f, Wave.Sine, 1300f, 380f, 0.4f, 0.002f, 8f, 0f, 6f, 0.02f, 2f, 0.3f);
                    Tone(b, 0f, 0.1f, Wave.Sine, 4200f, 3000f, 0.12f, 0.001f, 20f);
                    break;
                case "Scatter":
                    b = new Buffer(0.3f);
                    Tone(b, 0f, 0.22f, Wave.Noise, 6000f, 900f, 0.6f, 0.001f, 12f);
                    Tone(b, 0f, 0.14f, Wave.Square, 420f, 140f, 0.3f, 0.001f, 16f, 0f, 6f, 0.02f, 0f, 0f, 2500f);
                    Tone(b, 0f, 0.1f, Wave.Sine, 180f, 70f, 0.5f, 0.001f, 18f);
                    break;
                case "Missile":
                    b = new Buffer(0.5f);
                    Sweep(b, 0f, 0.45f, 500f, 3200f, 0.7f, 0.02f, 4f);
                    Tone(b, 0f, 0.16f, Wave.Sine, 130f, 55f, 0.6f, 0.002f, 14f);
                    break;
                case "DroneShot":
                    b = new Buffer(0.08f);
                    Tone(b, 0f, 0.06f, Wave.Pulse, 2400f, 1400f, 0.3f, 0.001f, 30f);
                    break;
                case "EnemyShot":
                    b = new Buffer(0.3f);
                    Tone(b, 0f, 0.26f, Wave.Sine, 760f, 330f, 0.45f, 0.002f, 7f, 0.15f, 28f);
                    Tone(b, 0f, 0.2f, Wave.Triangle, 1520f, 660f, 0.15f, 0.002f, 9f, 0.15f, 28f);
                    break;
                case "Nova":
                    b = new Buffer(2.2f);
                    Boom(b, 0f, 1.6f, 0.8f, 3000f);
                    Tone(b, 0f, 0.7f, Wave.Sine, 180f, 2200f, 0.25f, 0.01f, 3f, 0f, 6f, 0.05f, 1.5f, 0.4f);
                    Sweep(b, 0f, 1.2f, 3000f, 200f, 0.5f, 0.01f, 2f);
                    Echo(b, 0.16f, 0.35f, 3);
                    break;
                case "Dash":
                    b = new Buffer(0.32f);
                    Sweep(b, 0f, 0.28f, 700f, 3200f, 0.7f, 0.02f, 6f);
                    Tone(b, 0f, 0.2f, Wave.Sine, 320f, 720f, 0.25f, 0.005f, 10f);
                    break;
                case "Denied":
                    b = new Buffer(0.25f);
                    Tone(b, 0f, 0.08f, Wave.Square, 210f, 200f, 0.3f, 0.002f, 6f, 0f, 6f, 0.01f, 0f, 0f, 1800f);
                    Tone(b, 0.11f, 0.1f, Wave.Square, 160f, 150f, 0.3f, 0.002f, 6f, 0f, 6f, 0.01f, 0f, 0f, 1800f);
                    break;
                case "RockBreak":
                    b = new Buffer(0.6f);
                    Tone(b, 0f, 0.45f, Wave.Noise, 2600f, 250f, 0.75f, 0.002f, 7f);
                    Tone(b, 0f, 0.3f, Wave.Sine, 120f, 45f, 0.7f, 0.002f, 10f);
                    for (int i = 0; i < 6; i++)
                    {
                        Tone(b, 0.03f + i * 0.04f, 0.025f, Wave.Noise, 6000f, 2500f, 0.25f, 0.001f, 70f);
                    }
                    break;
                case "RockBreakSmall":
                    b = new Buffer(0.3f);
                    Tone(b, 0f, 0.22f, Wave.Noise, 4000f, 600f, 0.6f, 0.001f, 14f);
                    Tone(b, 0f, 0.15f, Wave.Sine, 220f, 90f, 0.45f, 0.001f, 18f);
                    break;
                case "IceBreak":
                    b = new Buffer(0.7f);
                    Tone(b, 0f, 0.15f, Wave.Noise, 9000f, 4000f, 0.5f, 0.001f, 20f);
                    float[] pings = { 2637f, 3136f, 3951f, 2349f, 4699f, 2960f };
                    for (int i = 0; i < pings.Length; i++)
                    {
                        Tone(b, i * 0.03f, 0.4f, Wave.Sine, pings[i], pings[i] * 0.98f, 0.14f, 0.001f, 10f, 0f, 6f, 0.02f, 2.76f, 0.2f);
                    }
                    break;
                case "CrystalBreak":
                    b = new Buffer(0.9f);
                    Tone(b, 0f, 0.2f, Wave.Noise, 8000f, 3000f, 0.35f, 0.001f, 16f);
                    int[] bells = { 88, 84, 79, 76 };
                    for (int i = 0; i < bells.Length; i++)
                    {
                        Tone(b, i * 0.05f, 0.6f, Wave.Sine, Midi(bells[i]), Midi(bells[i]), 0.2f, 0.001f, 6f, 0f, 6f, 0.02f, 3.5f, 0.6f);
                    }
                    Echo(b, 0.09f, 0.3f, 2);
                    break;
                case "Explosion":
                    b = new Buffer(1f);
                    Boom(b, 0f, 0.85f, 0.85f);
                    break;
                case "BigExplosion":
                    b = new Buffer(2f);
                    Boom(b, 0f, 1.6f, 0.9f, 3500f);
                    Boom(b, 0.12f, 1.2f, 0.4f, 2000f);
                    Echo(b, 0.14f, 0.25f, 2);
                    break;
                case "HullHit":
                    b = new Buffer(0.5f);
                    Tone(b, 0f, 0.4f, Wave.Sine, 190f, 150f, 0.55f, 0.001f, 9f, 0f, 6f, 0.02f, 1.53f, 1.6f);
                    Tone(b, 0f, 0.15f, Wave.Noise, 5000f, 1200f, 0.5f, 0.001f, 18f);
                    break;
                case "ShieldHit":
                    b = new Buffer(0.4f);
                    Tone(b, 0f, 0.3f, Wave.Saw, 950f, 300f, 0.25f, 0.001f, 9f, 0.05f, 30f, 0.02f, 0f, 0f, 3000f);
                    Tone(b, 0f, 0.28f, Wave.Sine, 1900f, 1500f, 0.2f, 0.001f, 10f, 0.08f, 25f);
                    break;
                case "ShieldDown":
                    b = new Buffer(0.8f);
                    Tone(b, 0f, 0.65f, Wave.Sine, 1300f, 180f, 0.4f, 0.002f, 3f, 0.04f, 18f, 0.05f, 2f, 0.8f);
                    Tone(b, 0f, 0.2f, Wave.Noise, 7000f, 2000f, 0.35f, 0.001f, 12f);
                    break;
                case "ShipExplode":
                    b = new Buffer(2.6f);
                    Boom(b, 0f, 2f, 0.95f, 4500f);
                    Tone(b, 0f, 1.2f, Wave.Saw, 1500f, 90f, 0.18f, 0.005f, 2f, 0.02f, 9f, 0.1f, 0f, 0f, 2500f);
                    Echo(b, 0.18f, 0.3f, 3);
                    break;
                case "EnemyExplode":
                    b = new Buffer(1.2f);
                    Boom(b, 0f, 0.9f, 0.8f, 3500f);
                    Tone(b, 0f, 0.6f, Wave.Sine, 900f, 120f, 0.25f, 0.002f, 4f, 0.2f, 20f);
                    break;
                case "Pickup":
                    b = new Buffer(0.45f);
                    Tone(b, 0f, 0.12f, Wave.Triangle, Midi(88), Midi(88), 0.35f, 0.002f, 8f);
                    Tone(b, 0.07f, 0.3f, Wave.Triangle, Midi(95), Midi(95), 0.35f, 0.002f, 7f);
                    Tone(b, 0.07f, 0.3f, Wave.Sine, Midi(107), Midi(107), 0.1f, 0.002f, 10f);
                    break;
                case "Crystal":
                    b = new Buffer(0.3f);
                    Tone(b, 0f, 0.22f, Wave.Sine, 2637f, 2637f, 0.3f, 0.001f, 14f, 0f, 6f, 0.02f, 2f, 0.3f);
                    Tone(b, 0.03f, 0.2f, Wave.Sine, 3951f, 3951f, 0.18f, 0.001f, 16f);
                    break;
                case "PowerUp":
                    b = new Buffer(0.9f);
                    int[] rising = { 72, 76, 79, 84, 88, 91 };
                    for (int i = 0; i < rising.Length; i++)
                    {
                        Tone(b, i * 0.05f, 0.14f, Wave.Square, Midi(rising[i]), Midi(rising[i]), 0.16f, 0.002f, 10f, 0f, 6f, 0.02f, 0f, 0f, 4000f);
                    }
                    Tone(b, 0.3f, 0.45f, Wave.Triangle, Midi(96), Midi(96), 0.3f, 0.002f, 5f, 0.012f, 10f);
                    Echo(b, 0.11f, 0.3f, 2);
                    break;
                case "WeaponUp":
                    b = new Buffer(0.9f);
                    Tone(b, 0f, 0.35f, Wave.Saw, 180f, 1300f, 0.22f, 0.01f, 1f, 0f, 6f, 0.02f, 0f, 0f, 3000f);
                    Tone(b, 0.33f, 0.45f, Wave.Square, Midi(84), Midi(84), 0.15f, 0.002f, 5f, 0f, 6f, 0.02f, 0f, 0f, 5000f);
                    Tone(b, 0.33f, 0.45f, Wave.Square, Midi(91), Midi(91), 0.12f, 0.002f, 5f, 0f, 6f, 0.02f, 0f, 0f, 5000f);
                    Tone(b, 0.33f, 0.45f, Wave.Sine, Midi(96), Midi(96), 0.2f, 0.002f, 5f);
                    break;
                case "ExtraLife":
                    b = new Buffer(1.2f);
                    int[] fanfareUp = { 79, 83, 86, 91, 86, 91 };
                    for (int i = 0; i < fanfareUp.Length; i++)
                    {
                        bool last = i == fanfareUp.Length - 1;
                        Tone(b, i * 0.09f, last ? 0.6f : 0.12f, Wave.Square, Midi(fanfareUp[i]), Midi(fanfareUp[i]), 0.18f, 0.003f, last ? 3f : 8f, 0f, 6f, 0.02f, 0f, 0f, 4500f);
                    }
                    break;
                case "PodOpen":
                    b = new Buffer(0.5f);
                    Tone(b, 0f, 0.12f, Wave.Noise, 6000f, 2000f, 0.4f, 0.001f, 20f);
                    Tone(b, 0.05f, 0.12f, Wave.Triangle, Midi(76), Midi(76), 0.3f, 0.002f, 10f);
                    Tone(b, 0.13f, 0.25f, Wave.Triangle, Midi(83), Midi(83), 0.3f, 0.002f, 8f);
                    break;
                case "MineBeep":
                    b = new Buffer(0.09f);
                    Tone(b, 0f, 0.07f, Wave.Sine, 1500f, 1500f, 0.4f, 0.001f, 12f);
                    break;
                case "BombTick":
                    b = new Buffer(0.12f);
                    Tone(b, 0f, 0.01f, Wave.Noise, 8000f, 8000f, 0.5f, 0.0005f, 100f);
                    Tone(b, 0f, 0.08f, Wave.Square, 880f, 870f, 0.25f, 0.001f, 20f, 0f, 6f, 0.01f, 0f, 0f, 3000f);
                    break;
                case "Warning":
                    b = new Buffer(0.9f);
                    for (int i = 0; i < 3; i++)
                    {
                        Tone(b, i * 0.28f, 0.13f, Wave.Square, 880f, 880f, 0.22f, 0.003f, 2f, 0f, 6f, 0.01f, 0f, 0f, 3500f);
                        Tone(b, i * 0.28f + 0.14f, 0.13f, Wave.Square, 660f, 660f, 0.22f, 0.003f, 2f, 0f, 6f, 0.01f, 0f, 0f, 3500f);
                    }
                    break;
                case "CometPass":
                    b = new Buffer(1.6f);
                    Sweep(b, 0f, 0.7f, 300f, 2800f, 0.6f, 0.2f, 0.5f);
                    Sweep(b, 0.7f, 0.9f, 2800f, 250f, 0.6f, 0.01f, 2.5f);
                    Tone(b, 0f, 1.5f, Wave.Noise, 400f, 200f, 0.3f, 0.3f, 1.5f);
                    break;
                case "WarpIn":
                    b = new Buffer(0.6f);
                    Tone(b, 0f, 0.5f, Wave.Sine, 220f, 1700f, 0.3f, 0.01f, 3f, 0f, 6f, 0.05f, 2f, 0.5f);
                    Sweep(b, 0f, 0.4f, 1500f, 5000f, 0.25f, 0.05f, 5f);
                    break;
                case "WellOpen":
                    b = new Buffer(1.8f);
                    Tone(b, 0f, 1.6f, Wave.Sine, 220f, 38f, 0.6f, 0.05f, 1.2f, 0.06f, 5f, 0.1f, 0.5f, 0.8f);
                    Tone(b, 0f, 1.5f, Wave.Noise, 500f, 120f, 0.35f, 0.1f, 1.4f);
                    break;
                case "BossRoar":
                    b = new Buffer(1.4f);
                    Tone(b, 0f, 1.2f, Wave.Saw, 92f, 70f, 0.35f, 0.08f, 1.8f, 0.05f, 11f, 0.1f, 0.5f, 1.2f, 900f);
                    Tone(b, 0f, 1.2f, Wave.Saw, 97f, 73f, 0.3f, 0.08f, 1.8f, 0.05f, 13f, 0.1f, 0f, 0f, 900f);
                    Tone(b, 0f, 1f, Wave.Noise, 900f, 250f, 0.4f, 0.1f, 2f);
                    break;
                case "Respawn":
                    b = new Buffer(0.9f);
                    Tone(b, 0f, 0.5f, Wave.Sine, 300f, 1200f, 0.3f, 0.01f, 3f, 0f, 6f, 0.05f, 2f, 0.4f);
                    Tone(b, 0.35f, 0.5f, Wave.Triangle, Midi(84), Midi(84), 0.25f, 0.003f, 4f);
                    Tone(b, 0.35f, 0.5f, Wave.Triangle, Midi(88), Midi(88), 0.2f, 0.003f, 4f);
                    Echo(b, 0.1f, 0.3f, 2);
                    break;
                case "Countdown":
                    b = new Buffer(0.25f);
                    Tone(b, 0f, 0.18f, Wave.Square, 660f, 660f, 0.28f, 0.002f, 8f, 0f, 6f, 0.02f, 0f, 0f, 3000f);
                    break;
                case "Go":
                    b = new Buffer(0.7f);
                    Tone(b, 0f, 0.55f, Wave.Square, 990f, 990f, 0.22f, 0.002f, 3f, 0f, 6f, 0.02f, 0f, 0f, 3500f);
                    Tone(b, 0f, 0.55f, Wave.Square, 1320f, 1320f, 0.16f, 0.002f, 3f, 0f, 6f, 0.02f, 0f, 0f, 3500f);
                    Echo(b, 0.12f, 0.25f, 2);
                    break;
                case "WaveStart":
                    b = new Buffer(1f);
                    int[] chime = { 69, 76, 81 };
                    for (int i = 0; i < chime.Length; i++)
                    {
                        Tone(b, i * 0.1f, 0.5f, Wave.Sine, Midi(chime[i]), Midi(chime[i]), 0.3f, 0.003f, 5f, 0f, 6f, 0.02f, 2f, 0.25f);
                    }
                    Echo(b, 0.14f, 0.35f, 3);
                    break;
                case "WaveClear":
                    b = new Buffer(1.2f);
                    int[] clear = { 72, 76, 79, 84, 88 };
                    for (int i = 0; i < clear.Length; i++)
                    {
                        Tone(b, i * 0.07f, 0.4f, Wave.Triangle, Midi(clear[i]), Midi(clear[i]), 0.25f, 0.002f, 5f);
                    }
                    Tone(b, 0.3f, 0.6f, Wave.Sine, Midi(96), Midi(96), 0.15f, 0.003f, 4f, 0.01f, 8f);
                    Echo(b, 0.12f, 0.3f, 2);
                    break;
                case "Victory":
                    b = new Buffer(2.4f);
                    int[] fanfare = { 72, 76, 79, 84 };
                    float[] lengths = { 0.13f, 0.13f, 0.13f, 1.2f };
                    float time = 0f;
                    for (int i = 0; i < fanfare.Length; i++)
                    {
                        bool last = i == fanfare.Length - 1;
                        Tone(b, time, lengths[i] + 0.12f, Wave.Saw, Midi(fanfare[i]), Midi(fanfare[i]), 0.2f, 0.01f, last ? 1.2f : 6f, last ? 0.01f : 0f, 5.5f, 0.05f, 0f, 0f, 2500f);
                        Tone(b, time, lengths[i] + 0.12f, Wave.Square, Midi(fanfare[i] - 12), Midi(fanfare[i] - 12), 0.16f, 0.01f, last ? 1.2f : 6f, 0f, 6f, 0.05f, 0f, 0f, 1500f);
                        time += last ? 0f : lengths[i];
                    }
                    Tone(b, 0.39f, 1.4f, Wave.Triangle, Midi(88), Midi(88), 0.14f, 0.02f, 1.3f, 0.012f, 5.5f);
                    Tone(b, 0.39f, 1.4f, Wave.Triangle, Midi(91), Midi(91), 0.12f, 0.02f, 1.3f, 0.012f, 5.5f);
                    Echo(b, 0.15f, 0.3f, 3);
                    break;
                case "GameOver":
                    b = new Buffer(2.4f);
                    int[] sad = { 69, 67, 65, 64 };
                    for (int i = 0; i < sad.Length; i++)
                    {
                        bool last = i == sad.Length - 1;
                        Tone(b, i * 0.34f, last ? 1.2f : 0.32f, Wave.Saw, Midi(sad[i]), Midi(sad[i]) * (last ? 0.96f : 1f), 0.18f, 0.01f, last ? 1.6f : 3f, last ? 0.03f : 0f, 6f, 0.05f, 0f, 0f, 1800f);
                        Tone(b, i * 0.34f, last ? 1.2f : 0.32f, Wave.Triangle, Midi(sad[i] - 12), Midi(sad[i] - 12), 0.25f, 0.01f, last ? 1.6f : 3f);
                    }
                    Echo(b, 0.2f, 0.3f, 2);
                    break;
                case "Star":
                    b = new Buffer(0.6f);
                    Tone(b, 0f, 0.5f, Wave.Pulse, Midi(91), Midi(91), 0.2f, 0.002f, 6f, 0f, 6f, 0.02f, 0f, 0f, 5000f);
                    Tone(b, 0.05f, 0.45f, Wave.Sine, Midi(98), Midi(98), 0.25f, 0.002f, 7f);
                    Echo(b, 0.1f, 0.25f, 2);
                    break;
                case "Click":
                    b = new Buffer(0.06f);
                    Tone(b, 0f, 0.045f, Wave.Square, 1800f, 1300f, 0.22f, 0.001f, 60f, 0f, 6f, 0.01f, 0f, 0f, 4000f);
                    break;
                case "Combo":
                    b = new Buffer(0.3f);
                    Tone(b, 0f, 0.08f, Wave.Square, Midi(84), Midi(84), 0.2f, 0.002f, 12f, 0f, 6f, 0.01f, 0f, 0f, 4000f);
                    Tone(b, 0.07f, 0.18f, Wave.Square, Midi(91), Midi(91), 0.2f, 0.002f, 9f, 0f, 6f, 0.01f, 0f, 0f, 4000f);
                    break;
                case "SaucerHum":
                    b = new Buffer(1f, true);
                    for (int i = 0; i < 8; i++)
                    {
                        float low = i % 2 == 0 ? 580f : 760f;
                        Tone(b, i * 0.125f, 0.13f, Wave.Square, low, low, 0.18f, 0.005f, 0f, 0f, 6f, 0.01f, 0f, 0f, 2200f);
                    }
                    Tone(b, 0f, 1f, Wave.Sine, 110f, 110f, 0.2f, 0.001f, 0f, 0.02f, 3f, 0.001f);
                    break;
                case "WellHum":
                    b = new Buffer(2f, true);
                    Tone(b, 0f, 2f, Wave.Sine, 55f, 55f, 0.45f, 0.001f, 0f, 0f, 6f, 0.001f);
                    Tone(b, 0f, 2f, Wave.Sine, 58f, 58f, 0.35f, 0.001f, 0f, 0f, 6f, 0.001f);
                    Tone(b, 0f, 2f, Wave.Noise, 300f, 300f, 0.2f, 0.001f, 0f, 0f, 6f, 0.001f);
                    break;
                default:
                    throw new ArgumentException($"Unknown effect {name}");
            }
            return Finish(b.Samples, 0.9f, b.Loop);
        }

        /// <summary>Whether an effect is a seamless loop (played by looping audio sources).</summary>
        public static bool IsLoop(string name)
        {
            return name == "SaucerHum" || name == "WellHum";
        }

        // ------------------------------------------------------------------ music

        /// <summary>The calm mission select loop: 16 bars at 88 BPM of pads, a plucked arpeggio with echo and a sparse bell melody.</summary>
        public static float[] MenuMusic()
        {
            noiseState = 54321;
            const float bpm = 88f;
            const int bars = 16;
            float beat = 60f / bpm;
            var b = new Buffer(bars * 4 * beat, true);
            int[] roots = { 45, 41, 48, 43 };
            int[][] chords = { new[] { 57, 60, 64 }, new[] { 53, 57, 60 }, new[] { 55, 60, 64 }, new[] { 55, 59, 62 } };
            int[] bells = { 76, 72, 79, 74, 76, 81, 79, 76 };
            for (int bar = 0; bar < bars; bar++)
            {
                int chord = bar % 4;
                float barStart = bar * 4 * beat;
                Tone(b, barStart, 4 * beat + 0.4f, Wave.Triangle, Midi(roots[chord]), Midi(roots[chord]), 0.3f, 0.3f, 0.3f, 0f, 6f, 0.4f, 0f, 0f, 400f);
                foreach (int note in chords[chord])
                {
                    Tone(b, barStart, 4 * beat + 0.6f, Wave.Saw, Midi(note), Midi(note), 0.045f, 0.9f, 0.2f, 0.004f, 0.3f, 0.6f, 0f, 0f, 900f);
                    Tone(b, barStart, 4 * beat + 0.6f, Wave.Saw, Midi(note) * 1.006f, Midi(note) * 1.006f, 0.045f, 0.9f, 0.2f, 0.004f, 0.37f, 0.6f, 0f, 0f, 900f);
                }
                for (int s = 0; s < 8; s++)
                {
                    int note = chords[chord][s % 3] + (s >= 4 ? 12 : 0) + 12;
                    Tone(b, barStart + s * beat / 2f, beat * 0.8f, Wave.Triangle, Midi(note), Midi(note), 0.07f, 0.004f, 5f);
                }
                if (bar % 2 == 1)
                {
                    int bell = bells[(bar / 2) % bells.Length];
                    Tone(b, barStart + beat, 2f, Wave.Sine, Midi(bell), Midi(bell), 0.12f, 0.003f, 1.5f, 0f, 6f, 0.05f, 3.01f, 0.3f);
                }
                Hat(b, barStart + 2 * beat, 0.03f);
            }
            Echo(b, beat * 0.75f, 0.35f, 3);
            return Finish(b.Samples, 0.8f, true);
        }

        /// <summary>The battle loop: 16 bars of driving synthwave at 124 BPM over Am - F - C - G.</summary>
        public static float[] BattleMusic()
        {
            noiseState = 12345;
            const float bpm = 124f;
            const int bars = 16;
            float beat = 60f / bpm;
            float sixteenth = beat / 4f;
            var b = new Buffer(bars * 4 * beat, true);
            int[] roots = { 45, 41, 48, 43 };
            int[][] chords = { new[] { 57, 60, 64 }, new[] { 57, 60, 65 }, new[] { 55, 60, 64 }, new[] { 55, 59, 62 } };
            int[][] melody =
            {
                new[] { 76, 0, 72, 74, 76, 0, 79, 76 }, new[] { 77, 0, 76, 72, 69, 0, 72, 74 },
                new[] { 76, 0, 72, 79, 84, 0, 83, 79 }, new[] { 81, 79, 76, 74, 71, 0, 74, 0 }
            };
            for (int bar = 0; bar < bars; bar++)
            {
                int section = bar % 4;
                bool intro = bar < 4;
                bool bridge = bar >= 12;
                float barStart = bar * 4 * beat;
                for (int s = 0; s < 16; s++)
                {
                    float at = barStart + s * sixteenth;
                    int bass = roots[section] - 12 + (s % 2 == 1 ? 12 : 0);
                    Tone(b, at, sixteenth * 0.9f, Wave.Saw, Midi(bass), Midi(bass), 0.2f, 0.003f, 6f, 0f, 6f, 0.01f, 0f, 0f, 700f);
                    if (s % 4 == 0)
                    {
                        Kick(b, at, 0.6f);
                    }
                    if (!intro && (s == 4 || s == 12))
                    {
                        Snare(b, at, 0.28f);
                    }
                    Hat(b, at, s % 4 == 2 ? 0.1f : 0.045f);
                    if (!intro && s % 2 == 0)
                    {
                        int arp = chords[section][(s / 2) % 3] + 12;
                        Tone(b, at, sixteenth * 1.6f, Wave.Pulse, Midi(arp), Midi(arp), 0.05f, 0.002f, 9f, 0f, 6f, 0.01f, 0f, 0f, 3500f);
                    }
                }
                foreach (int note in chords[section])
                {
                    Tone(b, barStart, 4 * beat, Wave.Saw, Midi(note), Midi(note), 0.035f, 0.15f, 0.4f, 0.003f, 0.5f, 0.1f, 0f, 0f, 1200f);
                }
                if (!intro)
                {
                    int[] line = melody[section];
                    for (int n = 0; n < line.Length; n++)
                    {
                        if (line[n] == 0)
                        {
                            continue;
                        }
                        int note = line[n] + (bridge ? 12 : 0);
                        Tone(b, barStart + n * beat / 2f, beat * 0.48f, Wave.Square, Midi(note), Midi(note), 0.07f, 0.004f, 3f, 0.006f, 6f, 0.02f, 0f, 0f, 3000f);
                    }
                }
            }
            Echo(b, beat * 0.75f, 0.2f, 2);
            return Finish(b.Samples, 0.85f, true);
        }

        /// <summary>The boss loop: 8 bars at 140 BPM, a D minor ostinato under heavy drums, stabs and a siren lead.</summary>
        public static float[] BossMusic()
        {
            noiseState = 777;
            const float bpm = 140f;
            const int bars = 8;
            float beat = 60f / bpm;
            float sixteenth = beat / 4f;
            var b = new Buffer(bars * 4 * beat, true);
            int[] ostinato = { 38, 38, 50, 38, 41, 38, 49, 38, 38, 38, 50, 38, 44, 41, 40, 37 };
            int[] stabs = { 62, 65, 69 };
            for (int bar = 0; bar < bars; bar++)
            {
                float barStart = bar * 4 * beat;
                int shift = bar % 4 == 3 ? 3 : bar % 4 == 2 ? -2 : 0;
                for (int s = 0; s < 16; s++)
                {
                    float at = barStart + s * sixteenth;
                    int bass = ostinato[s] + shift;
                    Tone(b, at, sixteenth * 0.95f, Wave.Saw, Midi(bass), Midi(bass), 0.22f, 0.002f, 5f, 0f, 6f, 0.01f, 0f, 0f, 900f);
                    if (s % 4 == 0 || s == 10)
                    {
                        Kick(b, at, 0.7f);
                    }
                    if (s == 4 || s == 12)
                    {
                        Snare(b, at, 0.35f);
                    }
                    Hat(b, at, s % 2 == 0 ? 0.08f : 0.04f);
                }
                if (bar % 2 == 0)
                {
                    foreach (int note in stabs)
                    {
                        Tone(b, barStart, beat * 0.5f, Wave.Square, Midi(note + shift), Midi(note + shift), 0.06f, 0.002f, 5f, 0f, 6f, 0.02f, 0f, 0f, 2500f);
                        Tone(b, barStart + beat * 2.5f, beat * 0.5f, Wave.Square, Midi(note + shift + 1), Midi(note + shift + 1), 0.05f, 0.002f, 5f, 0f, 6f, 0.02f, 0f, 0f, 2500f);
                    }
                }
                if (bar >= 4)
                {
                    int lead = bar % 2 == 0 ? 74 : 77;
                    Tone(b, barStart, 4 * beat, Wave.Saw, Midi(lead), Midi(lead - 1), 0.06f, 0.2f, 0.4f, 0.015f, 5.5f, 0.2f, 0f, 0f, 3000f);
                }
            }
            return Finish(b.Samples, 0.85f, true);
        }

        // ------------------------------------------------------------------ output

        /// <summary>Soft-clips, removes any DC offset and normalises to <paramref name="peak"/>.</summary>
        private static float[] Finish(float[] samples, float peak, bool loop)
        {
            double mean = 0.0;
            for (int i = 0; i < samples.Length; i++)
            {
                mean += samples[i];
            }
            float offset = samples.Length > 0 ? (float)(mean / samples.Length) : 0f;
            float max = 0.0001f;
            for (int i = 0; i < samples.Length; i++)
            {
                samples[i] = (float)Math.Tanh((samples[i] - offset) * 1.2f);
                max = Mathf.Max(max, Mathf.Abs(samples[i]));
            }
            float gain = peak / max;
            for (int i = 0; i < samples.Length; i++)
            {
                samples[i] *= gain;
            }
            if (!loop && samples.Length > 64)
            {
                // A tiny fade out so effects never end on a click.
                for (int i = 0; i < 64; i++)
                {
                    samples[samples.Length - 1 - i] *= i / 64f;
                }
            }
            return samples;
        }

        /// <summary>The samples as the bytes of a 16-bit mono WAV file.</summary>
        public static byte[] Wav(float[] samples)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                int dataSize = samples.Length * 2;
                writer.Write(new[] { 'R', 'I', 'F', 'F' });
                writer.Write(36 + dataSize);
                writer.Write(new[] { 'W', 'A', 'V', 'E' });
                writer.Write(new[] { 'f', 'm', 't', ' ' });
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)1);
                writer.Write(SampleRate);
                writer.Write(SampleRate * 2);
                writer.Write((short)2);
                writer.Write((short)16);
                writer.Write(new[] { 'd', 'a', 't', 'a' });
                writer.Write(dataSize);
                foreach (float sample in samples)
                {
                    writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(sample, -1f, 1f) * 32767f));
                }
                writer.Flush();
                return stream.ToArray();
            }
        }
    }
}
