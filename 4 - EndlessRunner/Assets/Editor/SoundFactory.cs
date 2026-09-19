using System;
using System.IO;
using UnityEngine;

namespace Portfolio.EndlessRunner.EditorTools
{
    /// <summary>
    /// A tiny synthesizer for the Endless Runner's sound effects and its two chiptune loops. Everything is mono
    /// 22 kHz, 16-bit PCM written as WAV files. Music notes that ring past the end of a loop wrap to its start, so the
    /// loops are seamless.
    /// </summary>
    internal static class SoundFactory
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
                }
                else if (index >= Samples.Length)
                {
                    return;
                }
                if (index >= 0)
                {
                    Samples[index] += value;
                }
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
        /// Adds one tone: frequency slides from <paramref name="from"/> to <paramref name="to"/> Hz, with a short
        /// attack, an exponential decay and an optional vibrato.
        /// </summary>
        private static void Tone(Buffer buffer, float start, float duration, Wave wave, float from, float to, float volume,
            float attack = 0.005f, float decay = 8f, float vibrato = 0f, float vibratoRate = 6f, float release = 0.02f)
        {
            int first = Mathf.RoundToInt(start * SampleRate);
            int count = Mathf.RoundToInt(duration * SampleRate);
            float phase = 0f;
            float lowpass = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float progress = i / (float)count;
                float frequency = Mathf.Lerp(from, to, progress) * (1f + vibrato * Mathf.Sin(t * vibratoRate * Mathf.PI * 2f));
                phase += frequency / SampleRate;
                float envelope = Mathf.Min(1f, t / Mathf.Max(attack, 0.0001f)) * Mathf.Exp(-decay * t);
                float tail = duration - t;
                if (tail < release)
                {
                    envelope *= Mathf.Max(0f, tail / release);
                }
                float sample = Oscillator(wave, phase);
                if (wave == Wave.Noise)
                {
                    float cutoff = Mathf.Clamp01(Mathf.Lerp(from, to, progress) / SampleRate * 6f);
                    lowpass += (sample - lowpass) * cutoff;
                    sample = lowpass * 2f;
                }
                buffer.Add(first + i, sample * envelope * volume);
            }
        }

        private static void Kick(Buffer buffer, float start, float volume)
        {
            Tone(buffer, start, 0.16f, Wave.Sine, 150f, 42f, volume, 0.002f, 14f);
            Tone(buffer, start, 0.02f, Wave.Noise, 4000f, 2000f, volume * 0.3f, 0.001f, 60f);
        }

        private static void Snare(Buffer buffer, float start, float volume)
        {
            Tone(buffer, start, 0.14f, Wave.Noise, 9000f, 6000f, volume, 0.001f, 22f);
            Tone(buffer, start, 0.08f, Wave.Triangle, 200f, 160f, volume * 0.6f, 0.001f, 30f);
        }

        private static void Hat(Buffer buffer, float start, float volume)
        {
            int first = Mathf.RoundToInt(start * SampleRate);
            int count = Mathf.RoundToInt(0.04f * SampleRate);
            float previous = 0f;
            for (int i = 0; i < count; i++)
            {
                float noise = NextNoise();
                float highpass = noise - previous;
                previous = noise;
                buffer.Add(first + i, highpass * Mathf.Exp(-90f * i / SampleRate) * volume);
            }
        }

        // ------------------------------------------------------------------ effects

        public static float[] Effect(string name)
        {
            noiseState = 22222;
            Buffer b;
            switch (name)
            {
                case "Coin":
                    b = new Buffer(0.32f);
                    Tone(b, 0f, 0.07f, Wave.Pulse, 1318.5f, 1318.5f, 0.35f, 0.002f, 6f);
                    Tone(b, 0.06f, 0.26f, Wave.Pulse, 1975.5f, 1975.5f, 0.35f, 0.002f, 9f);
                    Tone(b, 0.06f, 0.26f, Wave.Sine, 3951f, 3951f, 0.12f, 0.002f, 14f);
                    break;
                case "Gem":
                    b = new Buffer(0.6f);
                    int[] gemNotes = { 84, 88, 91, 96, 100 };
                    for (int i = 0; i < gemNotes.Length; i++)
                    {
                        Tone(b, i * 0.045f, i == gemNotes.Length - 1 ? 0.4f : 0.1f, Wave.Pulse, Midi(gemNotes[i]), Midi(gemNotes[i]), 0.28f, 0.002f, 7f, 0.01f, 12f);
                        Tone(b, i * 0.045f, 0.3f, Wave.Sine, Midi(gemNotes[i] + 12), Midi(gemNotes[i] + 12), 0.1f, 0.002f, 10f);
                    }
                    break;
                case "Jump":
                    b = new Buffer(0.22f);
                    Tone(b, 0f, 0.2f, Wave.Triangle, 300f, 760f, 0.55f, 0.004f, 9f);
                    Tone(b, 0f, 0.12f, Wave.Pulse, 600f, 1300f, 0.08f, 0.004f, 18f);
                    break;
                case "Slide":
                    b = new Buffer(0.4f);
                    Tone(b, 0f, 0.38f, Wave.Noise, 1800f, 500f, 0.5f, 0.03f, 5f);
                    break;
                case "Land":
                    b = new Buffer(0.18f);
                    Tone(b, 0f, 0.16f, Wave.Sine, 120f, 50f, 0.7f, 0.002f, 16f);
                    Tone(b, 0f, 0.06f, Wave.Noise, 2500f, 800f, 0.35f, 0.001f, 35f);
                    break;
                case "Crash":
                    b = new Buffer(0.6f);
                    Tone(b, 0f, 0.5f, Wave.Noise, 5000f, 400f, 0.7f, 0.001f, 7f);
                    Tone(b, 0f, 0.4f, Wave.Sine, 90f, 38f, 0.8f, 0.002f, 7f);
                    Tone(b, 0f, 0.22f, Wave.Square, 160f, 55f, 0.25f, 0.002f, 12f);
                    break;
                case "Bump":
                    b = new Buffer(0.16f);
                    Tone(b, 0f, 0.14f, Wave.Sine, 200f, 110f, 0.7f, 0.002f, 18f);
                    Tone(b, 0f, 0.05f, Wave.Noise, 3000f, 1000f, 0.2f, 0.001f, 40f);
                    break;
                case "Whoosh":
                    b = new Buffer(0.26f);
                    Tone(b, 0f, 0.25f, Wave.Noise, 700f, 2600f, 0.45f, 0.06f, 6f, 0f, 6f, 0.1f);
                    break;
                case "PowerUp":
                    b = new Buffer(0.7f);
                    int[] rising = { 72, 76, 79, 84, 88, 91 };
                    for (int i = 0; i < rising.Length; i++)
                    {
                        Tone(b, i * 0.055f, 0.12f, Wave.Square, Midi(rising[i]), Midi(rising[i]), 0.2f, 0.002f, 10f);
                    }
                    Tone(b, 0.33f, 0.35f, Wave.Triangle, Midi(96), Midi(96), 0.3f, 0.002f, 6f, 0.015f, 10f);
                    Tone(b, 0.33f, 0.35f, Wave.Sine, Midi(91), Midi(91), 0.2f, 0.002f, 6f);
                    break;
                case "ShieldBreak":
                    b = new Buffer(0.6f);
                    Tone(b, 0f, 0.3f, Wave.Noise, 9000f, 3000f, 0.45f, 0.001f, 12f);
                    float[] shards = { 3100f, 2600f, 3700f, 2200f, 4200f };
                    for (int i = 0; i < shards.Length; i++)
                    {
                        Tone(b, i * 0.035f, 0.3f, Wave.Sine, shards[i], shards[i] * 0.8f, 0.18f, 0.001f, 14f);
                    }
                    break;
                case "Bounce":
                    b = new Buffer(0.5f);
                    Tone(b, 0f, 0.45f, Wave.Sine, 170f, 560f, 0.7f, 0.003f, 4f, 0.18f, 24f);
                    Tone(b, 0f, 0.3f, Wave.Triangle, 340f, 1100f, 0.2f, 0.003f, 8f, 0.1f, 24f);
                    break;
                case "Splash":
                    b = new Buffer(0.7f);
                    Tone(b, 0f, 0.6f, Wave.Noise, 3500f, 600f, 0.6f, 0.005f, 5f);
                    for (int i = 0; i < 6; i++)
                    {
                        float at = 0.08f + i * 0.07f;
                        Tone(b, at, 0.06f, Wave.Sine, 500f + i * 90f, 1200f + i * 120f, 0.15f, 0.002f, 20f);
                    }
                    break;
                case "Countdown":
                    b = new Buffer(0.2f);
                    Tone(b, 0f, 0.16f, Wave.Square, 660f, 660f, 0.3f, 0.002f, 8f);
                    break;
                case "Go":
                    b = new Buffer(0.5f);
                    Tone(b, 0f, 0.45f, Wave.Square, 990f, 990f, 0.25f, 0.002f, 4f);
                    Tone(b, 0f, 0.45f, Wave.Square, 1320f, 1320f, 0.18f, 0.002f, 4f);
                    break;
                case "Victory":
                    b = new Buffer(1.8f);
                    int[] fanfare = { 72, 76, 79, 84 };
                    float[] lengths = { 0.12f, 0.12f, 0.12f, 1f };
                    float time = 0f;
                    for (int i = 0; i < fanfare.Length; i++)
                    {
                        Tone(b, time, lengths[i] + 0.1f, Wave.Square, Midi(fanfare[i]), Midi(fanfare[i]), 0.22f, 0.004f, i == 3 ? 1.6f : 6f, i == 3 ? 0.012f : 0f, 5.5f);
                        Tone(b, time, lengths[i] + 0.1f, Wave.Triangle, Midi(fanfare[i] - 12), Midi(fanfare[i] - 12), 0.25f, 0.004f, i == 3 ? 1.6f : 6f);
                        time += lengths[i] == 1f ? 0f : lengths[i];
                    }
                    Tone(b, 0.36f, 1.1f, Wave.Pulse, Midi(88), Midi(88), 0.12f, 0.01f, 1.6f, 0.012f, 5.5f);
                    Tone(b, 0.36f, 1.1f, Wave.Pulse, Midi(91), Midi(91), 0.1f, 0.01f, 1.6f, 0.012f, 5.5f);
                    for (int i = 0; i < 6; i++)
                    {
                        Snare(b, 0.36f + i * 0.05f, 0.08f + i * 0.02f);
                    }
                    break;
                case "GameOver":
                    b = new Buffer(2f);
                    int[] sad = { 67, 66, 65, 64 };
                    for (int i = 0; i < sad.Length; i++)
                    {
                        bool last = i == sad.Length - 1;
                        Tone(b, i * 0.32f, last ? 1f : 0.3f, Wave.Square, Midi(sad[i]), Midi(sad[i]) * (last ? 0.97f : 1f), 0.2f, 0.01f, last ? 1.8f : 3f, last ? 0.03f : 0f, 6f);
                        Tone(b, i * 0.32f, last ? 1f : 0.3f, Wave.Triangle, Midi(sad[i] - 12), Midi(sad[i] - 12), 0.25f, 0.01f, last ? 1.8f : 3f);
                    }
                    break;
                case "Star":
                    b = new Buffer(0.5f);
                    Tone(b, 0f, 0.45f, Wave.Pulse, Midi(91), Midi(91), 0.25f, 0.002f, 6f);
                    Tone(b, 0.05f, 0.4f, Wave.Sine, Midi(98), Midi(98), 0.25f, 0.002f, 7f);
                    break;
                case "Click":
                    b = new Buffer(0.05f);
                    Tone(b, 0f, 0.04f, Wave.Square, 1500f, 1200f, 0.25f, 0.001f, 60f);
                    break;
                default:
                    throw new ArgumentException($"Unknown effect {name}");
            }
            return Finish(b.Samples, 0.9f);
        }

        public static readonly string[] EffectNames =
        {
            "Coin", "Gem", "Jump", "Slide", "Land", "Crash", "Bump", "Whoosh", "PowerUp", "ShieldBreak", "Bounce", "Splash",
            "Countdown", "Go", "Victory", "GameOver", "Star", "Click"
        };

        // ------------------------------------------------------------------ music

        /// <summary>The upbeat run loop: 16 bars at 140 BPM over C - G - Am - F.</summary>
        public static float[] RunMusic()
        {
            noiseState = 12345;
            const float bpm = 140f;
            const int bars = 16;
            float beat = 60f / bpm;
            float step = beat / 2f;
            var b = new Buffer(bars * 4 * beat, true);
            int[] roots = { 48, 48, 43, 43, 45, 45, 41, 43 };
            int[][] chords =
            {
                new[] { 60, 64, 67 }, new[] { 60, 64, 67 }, new[] { 59, 62, 67 }, new[] { 59, 62, 67 },
                new[] { 57, 60, 64 }, new[] { 57, 60, 64 }, new[] { 57, 60, 65 }, new[] { 59, 62, 67 }
            };
            int[][] melody =
            {
                new[] { 76, 79, 84, 79, 76, 79, 84, 86 }, new[] { 88, 86, 84, 79, 81, 79, 76, 79 },
                new[] { 74, 79, 83, 79, 74, 79, 83, 84 }, new[] { 86, 84, 83, 79, 81, 83, 79, 74 },
                new[] { 72, 76, 81, 76, 72, 76, 81, 83 }, new[] { 84, 83, 81, 76, 79, 81, 76, 72 },
                new[] { 77, 81, 84, 81, 77, 81, 89, 88 }, new[] { 86, 84, 81, 77, 79, 81, 83, 86 }
            };
            for (int bar = 0; bar < bars; bar++)
            {
                int section = bar % 8;
                bool second = bar >= 8;
                float barStart = bar * 4 * beat;
                for (int s = 0; s < 8; s++)
                {
                    float at = barStart + s * step;
                    int bass = roots[section] + (s == 2 || s == 6 ? 12 : 0) + (s == 4 ? 7 : 0);
                    Tone(b, at, step * 0.9f, Wave.Triangle, Midi(bass), Midi(bass), 0.34f, 0.004f, 3f);
                    int note = melody[section][s];
                    if (second && s % 2 == 1)
                    {
                        note += 12;
                    }
                    if (!(second && section == 7 && s > 5))
                    {
                        Tone(b, at, step * 0.85f, Wave.Pulse, Midi(note), Midi(note), 0.13f, 0.004f, 2.5f, 0.004f, 6f);
                    }
                    if (second)
                    {
                        Tone(b, at, step * 0.85f, Wave.Square, Midi(note - 4), Midi(note - 4), 0.045f, 0.004f, 3f);
                    }
                    for (int k = 0; k < 2; k++)
                    {
                        int arp = chords[section][(s * 2 + k) % 3] + 12;
                        Tone(b, at + k * step / 2f, step / 2f * 0.8f, Wave.Triangle, Midi(arp), Midi(arp), 0.07f, 0.002f, 6f);
                    }
                    if (s == 0 || s == 4)
                    {
                        Kick(b, at, 0.55f);
                    }
                    if (s == 2 || s == 6)
                    {
                        Snare(b, at, 0.22f);
                    }
                    Hat(b, at, s % 2 == 1 ? 0.12f : 0.07f);
                    if (section == 7 && s >= 6)
                    {
                        Snare(b, at + step / 2f, 0.16f);
                    }
                }
            }
            return Finish(b.Samples, 0.85f);
        }

        /// <summary>The calm menu loop: 8 bars at 96 BPM of arpeggios with a bell melody.</summary>
        public static float[] MenuMusic()
        {
            noiseState = 54321;
            const float bpm = 96f;
            const int bars = 8;
            float beat = 60f / bpm;
            var b = new Buffer(bars * 4 * beat, true);
            int[][] chords = { new[] { 60, 64, 67, 72 }, new[] { 57, 60, 64, 69 }, new[] { 53, 57, 60, 65 }, new[] { 55, 59, 62, 67 } };
            int[] roots = { 48, 45, 41, 43 };
            int[] bells = { 76, 79, 81, 79, 76, 72, 74, 76, 72, 76, 77, 76, 74, 71, 72, 74 };
            for (int bar = 0; bar < bars; bar++)
            {
                int chord = bar % 4;
                float barStart = bar * 4 * beat;
                Tone(b, barStart, 4 * beat, Wave.Triangle, Midi(roots[chord]), Midi(roots[chord]), 0.3f, 0.02f, 0.6f);
                for (int s = 0; s < 8; s++)
                {
                    int note = chords[chord][s % 4] + (s >= 4 ? 12 : 0);
                    Tone(b, barStart + s * beat / 2f, beat * 0.9f, Wave.Triangle, Midi(note), Midi(note), 0.09f, 0.005f, 3f);
                }
                for (int s = 0; s < 2; s++)
                {
                    int bell = bells[(bar * 2 + s) % bells.Length];
                    Tone(b, barStart + s * 2 * beat, 1.5f, Wave.Sine, Midi(bell), Midi(bell), 0.16f, 0.003f, 2.2f);
                    Tone(b, barStart + s * 2 * beat, 0.8f, Wave.Sine, Midi(bell + 12), Midi(bell + 12), 0.05f, 0.003f, 4f);
                }
                Hat(b, barStart + beat, 0.04f);
                Hat(b, barStart + 3 * beat, 0.04f);
            }
            return Finish(b.Samples, 0.8f);
        }

        // ------------------------------------------------------------------ output

        /// <summary>Soft-clips and normalises to <paramref name="peak"/>.</summary>
        private static float[] Finish(float[] samples, float peak)
        {
            float max = 0.0001f;
            for (int i = 0; i < samples.Length; i++)
            {
                samples[i] = (float)Math.Tanh(samples[i] * 1.2f);
                max = Mathf.Max(max, Mathf.Abs(samples[i]));
            }
            float gain = peak / max;
            for (int i = 0; i < samples.Length; i++)
            {
                samples[i] *= gain;
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
