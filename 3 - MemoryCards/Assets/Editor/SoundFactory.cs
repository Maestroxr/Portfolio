using System;
using System.IO;
using UnityEngine;

namespace Portfolio.MemoryCards.EditorTools
{
    /// <summary>
    /// A small synthesiser for the sounds Memory Cards does not take from the Kenney packs: the menu and play music
    /// loops (mallets, bass, pad and light percussion over a four chord progression) and a few effects (the match
    /// chime, the bomb, the wild card sparkle, cracking ice, a lost heart). Everything is seeded, so a rebuild writes
    /// the same bytes.
    /// </summary>
    internal static class SoundFactory
    {
        public const int SampleRate = 22050;

        /// <summary>A mono buffer; a looping one wraps notes that ring past its end around to the start.</summary>
        internal sealed class Track
        {
            public readonly float[] Samples;
            private readonly bool loop;

            public Track(float seconds, bool loops = false)
            {
                Samples = new float[Mathf.CeilToInt(seconds * SampleRate)];
                loop = loops;
            }

            public float Seconds => Samples.Length / (float)SampleRate;

            public void Add(int index, float value)
            {
                if (loop)
                {
                    index %= Samples.Length;
                    if (index < 0)
                    {
                        index += Samples.Length;
                    }
                }
                else if (index < 0 || index >= Samples.Length)
                {
                    return;
                }
                Samples[index] += value;
            }
        }

        public static float Note(int midi)
        {
            return 440f * Mathf.Pow(2f, (midi - 69) / 12f);
        }

        #region Instruments

        /// <summary>A marimba-like mallet: the fundamental with a quickly fading fourth partial.</summary>
        public static void Mallet(Track track, float start, float frequency, float duration, float volume)
        {
            int first = Mathf.RoundToInt(start * SampleRate);
            int count = Mathf.RoundToInt(duration * SampleRate);
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float attack = Mathf.Clamp01(t / 0.003f);
                float body = Mathf.Sin(2f * Mathf.PI * frequency * t) * Mathf.Exp(-t * 5.5f);
                float bright = 0.28f * Mathf.Sin(2f * Mathf.PI * frequency * 3.99f * t) * Mathf.Exp(-t * 28f);
                float knock = 0.08f * Mathf.Sin(2f * Mathf.PI * frequency * 9.9f * t) * Mathf.Exp(-t * 90f);
                track.Add(first + i, (body + bright + knock) * attack * volume);
            }
        }

        /// <summary>A small bell: inharmonic partials with their own decays.</summary>
        public static void Bell(Track track, float start, float frequency, float duration, float volume)
        {
            float[] ratios = { 1f, 2f, 2.76f, 5.4f };
            float[] levels = { 1f, 0.45f, 0.3f, 0.14f };
            float[] decays = { 3.2f, 5f, 7f, 12f };
            int first = Mathf.RoundToInt(start * SampleRate);
            int count = Mathf.RoundToInt(duration * SampleRate);
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float attack = Mathf.Clamp01(t / 0.002f);
                float value = 0f;
                for (int k = 0; k < ratios.Length; k++)
                {
                    value += levels[k] * Mathf.Sin(2f * Mathf.PI * frequency * ratios[k] * t) * Mathf.Exp(-t * decays[k]);
                }
                track.Add(first + i, value * attack * volume * 0.55f);
            }
        }

        /// <summary>A round bass note with a soft attack and release.</summary>
        public static void Bass(Track track, float start, float frequency, float duration, float volume)
        {
            int first = Mathf.RoundToInt(start * SampleRate);
            int count = Mathf.RoundToInt(duration * SampleRate);
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float envelope = Mathf.Clamp01(t / 0.01f) * Mathf.Lerp(1f, 0.55f, Mathf.Clamp01(t / 0.25f)) * Mathf.Clamp01((duration - t) / 0.06f);
                float value = Mathf.Sin(2f * Mathf.PI * frequency * t) + 0.25f * Mathf.Sin(4f * Mathf.PI * frequency * t) + 0.1f * Mathf.Sin(6f * Mathf.PI * frequency * t);
                track.Add(first + i, value * envelope * volume);
            }
        }

        /// <summary>A soft chord of slightly detuned triangle waves.</summary>
        public static void Pad(Track track, float start, float[] frequencies, float duration, float volume)
        {
            int first = Mathf.RoundToInt(start * SampleRate);
            int count = Mathf.RoundToInt(duration * SampleRate);
            float[] detune = { 0.997f, 1f, 1.003f };
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float envelope = Mathf.Clamp01(t / 0.35f) * Mathf.Clamp01((duration - t) / 0.35f);
                float value = 0f;
                foreach (float frequency in frequencies)
                {
                    foreach (float d in detune)
                    {
                        float phase = frequency * d * t;
                        value += 1f - 4f * Mathf.Abs(phase - Mathf.Floor(phase + 0.5f));
                    }
                }
                track.Add(first + i, value * envelope * volume / (frequencies.Length * detune.Length));
            }
        }

        public static void Kick(Track track, float start, float volume)
        {
            int first = Mathf.RoundToInt(start * SampleRate);
            int count = Mathf.RoundToInt(0.32f * SampleRate);
            float phase = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float frequency = 48f + 110f * Mathf.Exp(-t * 28f);
                phase += frequency / SampleRate;
                track.Add(first + i, Mathf.Sin(2f * Mathf.PI * phase) * Mathf.Exp(-t * 11f) * volume);
            }
        }

        /// <summary>Noise through a crude band pass: a clap or snare depending on the decay.</summary>
        public static void Clap(Track track, float start, float volume, System.Random random)
        {
            int first = Mathf.RoundToInt(start * SampleRate);
            int count = Mathf.RoundToInt(0.2f * SampleRate);
            float low = 0f;
            float previous = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float noise = (float)random.NextDouble() * 2f - 1f;
                low += (noise - low) * 0.35f;
                float band = low - previous;
                previous = low;
                float envelope = Mathf.Exp(-t * 22f) * (t < 0.012f ? 0.6f + 0.4f * Mathf.Sin(t * 1600f) : 1f);
                track.Add(first + i, band * envelope * volume * 2.2f);
            }
        }

        public static void Hat(Track track, float start, float volume, System.Random random)
        {
            int first = Mathf.RoundToInt(start * SampleRate);
            int count = Mathf.RoundToInt(0.06f * SampleRate);
            float previous = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float noise = (float)random.NextDouble() * 2f - 1f;
                float high = noise - previous;
                previous = noise;
                track.Add(first + i, high * Mathf.Exp(-t * 70f) * volume * 0.5f);
            }
        }

        public static void Shaker(Track track, float start, float volume, System.Random random)
        {
            int first = Mathf.RoundToInt(start * SampleRate);
            int count = Mathf.RoundToInt(0.09f * SampleRate);
            float previous = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float noise = (float)random.NextDouble() * 2f - 1f;
                float high = noise - previous;
                previous = noise;
                float envelope = Mathf.Clamp01(t / 0.015f) * Mathf.Exp(-t * 40f);
                track.Add(first + i, high * envelope * volume * 0.4f);
            }
        }

        #endregion

        #region Music

        // I - vi - IV - V in C, twice; chord tones as MIDI notes.
        private static readonly int[][] Progression =
        {
            new[] { 60, 64, 67 }, new[] { 57, 60, 64 }, new[] { 53, 57, 60 }, new[] { 55, 59, 62 },
            new[] { 60, 64, 67 }, new[] { 57, 60, 64 }, new[] { 53, 57, 60 }, new[] { 55, 59, 62 }
        };

        private static readonly int[] Scale = { 60, 62, 64, 67, 69, 72, 74, 76, 79, 81 };

        /// <summary>The level select loop: mellow mallet arpeggios over a pad and a soft bass, with a shaker.</summary>
        public static float[] MenuMusic()
        {
            const float bpm = 92f;
            float beat = 60f / bpm;
            var track = new Track(beat * 4f * Progression.Length, true);
            var random = new System.Random(3);
            int[] arpeggio = { 0, 1, 2, 1, 0, 2, 1, 2 };
            for (int bar = 0; bar < Progression.Length; bar++)
            {
                int[] chord = Progression[bar];
                float barStart = bar * beat * 4f;
                Pad(track, barStart, new[] { Note(chord[0]), Note(chord[1]), Note(chord[2]) }, beat * 4f + 0.3f, 0.16f);
                Bass(track, barStart, Note(chord[0] - 24), beat * 1.8f, 0.38f);
                Bass(track, barStart + beat * 2f, Note(chord[0] - 24), beat * 1.6f, 0.3f);
                for (int step = 0; step < 8; step++)
                {
                    int note = chord[arpeggio[step]] + 12;
                    Mallet(track, barStart + step * beat * 0.5f, Note(note), 0.9f, step % 2 == 0 ? 0.24f : 0.17f);
                }
                if (bar % 2 == 1)
                {
                    int top = chord[2] + 24;
                    Bell(track, barStart + beat * 3f, Note(top), 1.4f, 0.12f);
                }
                for (int step = 0; step < 8; step++)
                {
                    Shaker(track, barStart + step * beat * 0.5f + beat * 0.25f, step % 2 == 0 ? 0.35f : 0.22f, random);
                }
            }
            return Master(track.Samples, 0.8f);
        }

        /// <summary>The play loop: bouncy bass, mallet riffs, a bell melody, kick, clap and hats.</summary>
        public static float[] PlayMusic()
        {
            const float bpm = 118f;
            float beat = 60f / bpm;
            var track = new Track(beat * 4f * Progression.Length, true);
            var random = new System.Random(5);
            var melody = new System.Random(11);
            int[] riff = { 0, 2, 1, 2, 0, 2, 1, 2 };
            int scaleIndex = 4;
            for (int bar = 0; bar < Progression.Length; bar++)
            {
                int[] chord = Progression[bar];
                float barStart = bar * beat * 4f;
                Pad(track, barStart, new[] { Note(chord[0]), Note(chord[1]), Note(chord[2]) }, beat * 4f + 0.2f, 0.09f);
                float[] bassBeats = { 0f, 1.5f, 2f, 3f, 3.5f };
                int[] bassNotes = { chord[0], chord[0], chord[0] + 7, chord[0], chord[1] };
                for (int b = 0; b < bassBeats.Length; b++)
                {
                    Bass(track, barStart + bassBeats[b] * beat, Note(bassNotes[b] - 24), beat * 0.45f, 0.42f);
                }
                for (int step = 0; step < 8; step++)
                {
                    int note = chord[riff[step]] + 12;
                    Mallet(track, barStart + step * beat * 0.5f, Note(note), 0.5f, step % 4 == 0 ? 0.2f : 0.13f);
                }
                // A melody that walks the pentatonic scale, landing on chord tones on the strong beats.
                float[] rhythm = bar % 2 == 0 ? new[] { 0f, 1f, 1.5f, 2.5f, 3f } : new[] { 0f, 0.5f, 1f, 2f };
                foreach (float at in rhythm)
                {
                    scaleIndex = Mathf.Clamp(scaleIndex + melody.Next(-2, 3), 2, Scale.Length - 1);
                    int note = Scale[scaleIndex];
                    if (Mathf.Approximately(at, 0f))
                    {
                        note = chord[melody.Next(0, 3)] + 12;
                    }
                    Bell(track, barStart + at * beat, Note(note + 12), 0.8f, 0.13f);
                }
                for (int b = 0; b < 4; b++)
                {
                    float at = barStart + b * beat;
                    if (b % 2 == 0)
                    {
                        Kick(track, at, 0.55f);
                    }
                    else
                    {
                        Clap(track, at, 0.3f, random);
                    }
                    Hat(track, at + beat * 0.5f, 0.45f, random);
                    Hat(track, at, 0.2f, random);
                }
                if (bar == Progression.Length - 1)
                {
                    Kick(track, barStart + beat * 3.5f, 0.35f);
                }
            }
            return Master(track.Samples, 0.8f);
        }

        #endregion

        #region Effects

        /// <summary>The match chime, played higher for every step of the combo.</summary>
        public static float[] Match()
        {
            var track = new Track(0.7f);
            Bell(track, 0f, Note(84), 0.7f, 0.8f);
            Mallet(track, 0.045f, Note(91), 0.5f, 0.35f);
            return Master(track.Samples, 0.85f);
        }

        public static float[] Bomb()
        {
            var track = new Track(1.1f);
            var random = new System.Random(21);
            float phase = 0f;
            float low = 0f;
            for (int i = 0; i < track.Samples.Length; i++)
            {
                float t = i / (float)SampleRate;
                float frequency = 32f + 95f * Mathf.Exp(-t * 6f);
                phase += frequency / SampleRate;
                float boom = Mathf.Sin(2f * Mathf.PI * phase) * Mathf.Exp(-t * 3.2f);
                float noise = (float)random.NextDouble() * 2f - 1f;
                float cutoff = Mathf.Lerp(0.6f, 0.03f, Mathf.Clamp01(t / 0.6f));
                low += (noise - low) * cutoff;
                float rumble = low * Mathf.Exp(-t * 4.5f) * 1.4f;
                float crackle = t < 0.4f && random.NextDouble() < 0.004 ? (float)random.NextDouble() * 0.6f : 0f;
                track.Add(i, Mathf.Clamp01(t / 0.004f) * (boom * 0.9f + rumble + crackle));
            }
            return Master(track.Samples, 0.95f);
        }

        public static float[] Wild()
        {
            var track = new Track(1.2f);
            int[] notes = { 84, 88, 91, 96, 100, 103 };
            for (int i = 0; i < notes.Length; i++)
            {
                Bell(track, i * 0.055f, Note(notes[i]), 0.9f, 0.55f);
            }
            for (int i = 0; i < track.Samples.Length; i++)
            {
                float t = i / (float)SampleRate;
                float frequency = 2400f + 1800f * t;
                float shimmer = Mathf.Sin(2f * Mathf.PI * frequency * t) * 0.06f * Mathf.Exp(-t * 3f) * (0.5f + 0.5f * Mathf.Sin(t * 70f));
                track.Add(i, shimmer);
            }
            return Master(track.Samples, 0.8f);
        }

        public static float[] Crack()
        {
            var track = new Track(0.3f);
            var random = new System.Random(8);
            float[] clicks = { 0f, 0.028f, 0.061f, 0.09f };
            foreach (float at in clicks)
            {
                int first = Mathf.RoundToInt(at * SampleRate);
                float previous = 0f;
                for (int i = 0; i < 0.03f * SampleRate; i++)
                {
                    float t = i / (float)SampleRate;
                    float noise = (float)random.NextDouble() * 2f - 1f;
                    track.Add(first + i, (noise - previous) * Mathf.Exp(-t * 160f) * 0.9f);
                    previous = noise;
                }
            }
            Bell(track, 0.01f, 2900f, 0.25f, 0.25f);
            return Master(track.Samples, 0.75f);
        }

        public static float[] Heart()
        {
            var track = new Track(0.45f);
            float phase = 0f;
            for (int i = 0; i < track.Samples.Length; i++)
            {
                float t = i / (float)SampleRate;
                float frequency = Mathf.Lerp(700f, 330f, Mathf.Clamp01(t / 0.32f)) * (1f + 0.02f * Mathf.Sin(t * 75f));
                phase += frequency / SampleRate;
                float envelope = Mathf.Clamp01(t / 0.01f) * Mathf.Exp(-t * 6f);
                track.Add(i, (Mathf.Sin(2f * Mathf.PI * phase) + 0.3f * Mathf.Sin(4f * Mathf.PI * phase)) * envelope);
            }
            return Master(track.Samples, 0.7f);
        }

        #endregion

        /// <summary>Soft clips and scales the buffer to <paramref name="peak"/>.</summary>
        private static float[] Master(float[] samples, float peak)
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

        /// <summary>16 bit mono PCM WAV bytes.</summary>
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
                    writer.Write((short)Mathf.Clamp(Mathf.RoundToInt(sample * 32767f), -32768, 32767));
                }
                writer.Flush();
                return stream.ToArray();
            }
        }
    }
}
