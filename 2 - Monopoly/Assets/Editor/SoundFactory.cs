using System;
using System.IO;
using UnityEngine;

namespace Portfolio.Monopoly.EditorTools
{
    /// <summary>
    /// A small synthesiser for what Monopoly does not take from the Kenney packs: two swinging lounge-jazz loops (the
    /// menu and the game: electric piano comping, walking bass, ride cymbal and brushes, a vibraphone tune) and a few
    /// effects (the winner's fanfare, the jail door, the auction gavel, a whoosh and the sad trombone of a
    /// bankruptcy). Everything is seeded, so a rebuild writes the same bytes.
    /// </summary>
    internal static class SoundFactory
    {
        public const int SampleRate = 22050;

        internal sealed class Track
        {
            public readonly float[] Samples;
            private readonly bool loop;

            public Track(float seconds, bool loops = false)
            {
                Samples = new float[Mathf.CeilToInt(seconds * SampleRate)];
                loop = loops;
            }

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

        private static float Note(int midi)
        {
            return 440f * Mathf.Pow(2f, (midi - 69) / 12f);
        }

        // ------------------------------------------------------------------ instruments

        /// <summary>An electric piano: FM with a quickly fading modulation index, and a bell-like tine on the attack.</summary>
        private static void Piano(Track track, float start, int midi, float duration, float volume)
        {
            float f = Note(midi);
            int first = Mathf.RoundToInt(start * SampleRate);
            int count = Mathf.RoundToInt((duration + 0.35f) * SampleRate);
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float release = t < duration ? 1f : Mathf.Exp(-(t - duration) * 14f);
                float envelope = Mathf.Clamp01(t / 0.004f) * Mathf.Exp(-t * 1.8f) * release;
                float index = 1.8f * Mathf.Exp(-t * 5f) + 0.25f;
                float value = Mathf.Sin(2f * Mathf.PI * f * t + index * Mathf.Sin(2f * Mathf.PI * f * t));
                value += 0.12f * Mathf.Sin(2f * Mathf.PI * f * 7.02f * t) * Mathf.Exp(-t * 30f);
                track.Add(first + i, value * envelope * volume);
            }
        }

        /// <summary>A plucked upright bass.</summary>
        private static void Bass(Track track, float start, int midi, float duration, float volume)
        {
            float f = Note(midi);
            int first = Mathf.RoundToInt(start * SampleRate);
            int count = Mathf.RoundToInt((duration + 0.08f) * SampleRate);
            var random = new System.Random(midi * 97 + first);
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float release = t < duration ? 1f : Mathf.Exp(-(t - duration) * 40f);
                float envelope = Mathf.Clamp01(t / 0.005f) * (0.35f + 0.65f * Mathf.Exp(-t * 4f)) * release;
                float value = Mathf.Sin(2f * Mathf.PI * f * t) + 0.45f * Mathf.Sin(4f * Mathf.PI * f * t) * Mathf.Exp(-t * 6f)
                    + 0.18f * Mathf.Sin(6f * Mathf.PI * f * t) * Mathf.Exp(-t * 10f);
                float thump = t < 0.02f ? ((float)random.NextDouble() * 2f - 1f) * (1f - t / 0.02f) * 0.25f : 0f;
                track.Add(first + i, (value + thump) * envelope * volume);
            }
        }

        /// <summary>A vibraphone: a pure tone with a soft upper partial and the motor's tremolo.</summary>
        private static void Vibes(Track track, float start, int midi, float duration, float volume)
        {
            float f = Note(midi);
            int first = Mathf.RoundToInt(start * SampleRate);
            int count = Mathf.RoundToInt((duration + 0.6f) * SampleRate);
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float release = t < duration ? 1f : Mathf.Exp(-(t - duration) * 6f);
                float envelope = Mathf.Clamp01(t / 0.003f) * Mathf.Exp(-t * 1.6f) * release;
                float tremolo = 1f - 0.22f * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 5.2f * t));
                float value = Mathf.Sin(2f * Mathf.PI * f * t) + 0.28f * Mathf.Sin(2f * Mathf.PI * f * 4f * t) * Mathf.Exp(-t * 6f)
                    + 0.05f * Mathf.Sin(2f * Mathf.PI * f * 10f * t) * Mathf.Exp(-t * 20f);
                track.Add(first + i, value * envelope * tremolo * volume);
            }
        }

        /// <summary>A ride cymbal: metallic partials over hissing noise.</summary>
        private static void Ride(Track track, float start, float volume, System.Random random)
        {
            int first = Mathf.RoundToInt(start * SampleRate);
            int count = Mathf.RoundToInt(0.7f * SampleRate);
            float previous = 0f;
            float[] partials = { 3120f, 4410f, 5230f, 6870f, 8190f };
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float noise = (float)random.NextDouble() * 2f - 1f;
                float high = noise - previous;
                previous = noise;
                float metal = 0f;
                foreach (float p in partials)
                {
                    metal += Mathf.Sin(2f * Mathf.PI * p * t);
                }
                float envelope = Mathf.Clamp01(t / 0.002f) * (0.3f * Mathf.Exp(-t * 30f) + 0.7f * Mathf.Exp(-t * 5f));
                track.Add(first + i, (high * 0.5f + metal * 0.06f) * envelope * volume);
            }
        }

        /// <summary>A closed hi-hat, or the pedal "chick".</summary>
        private static void Hat(Track track, float start, float volume, System.Random random)
        {
            int first = Mathf.RoundToInt(start * SampleRate);
            int count = Mathf.RoundToInt(0.08f * SampleRate);
            float previous = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float noise = (float)random.NextDouble() * 2f - 1f;
                float high = noise - previous;
                previous = noise;
                track.Add(first + i, high * Mathf.Exp(-t * 60f) * volume);
            }
        }

        /// <summary>A brush sweep on the snare.</summary>
        private static void Brush(Track track, float start, float length, float volume, System.Random random)
        {
            int first = Mathf.RoundToInt(start * SampleRate);
            int count = Mathf.RoundToInt(length * SampleRate);
            float low = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float noise = (float)random.NextDouble() * 2f - 1f;
                low += (noise - low) * 0.35f;
                float envelope = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / length)) * (0.6f + 0.4f * Mathf.Exp(-t * 8f));
                track.Add(first + i, (noise - low) * envelope * volume);
            }
        }

        // ------------------------------------------------------------------ the loops

        private struct Chord
        {
            public int root;
            public int[] voicing;

            public Chord(int root, params int[] voicing)
            {
                this.root = root;
                this.voicing = voicing;
            }
        }

        // Voicings in the middle of the keyboard (MIDI numbers), bass roots an octave or two down.
        private static readonly Chord Fmaj7 = new Chord(41, 57, 60, 64, 69);
        private static readonly Chord F6 = new Chord(41, 57, 62, 65, 69);
        private static readonly Chord D7 = new Chord(38, 54, 60, 64, 66);
        private static readonly Chord Gm7 = new Chord(43, 58, 62, 65, 69);
        private static readonly Chord C7 = new Chord(36, 58, 62, 64, 67);
        private static readonly Chord Cm7 = new Chord(36, 58, 63, 67, 70);
        private static readonly Chord F7 = new Chord(41, 57, 63, 67, 69);
        private static readonly Chord Bbmaj7 = new Chord(46, 57, 62, 65, 69);
        private static readonly Chord Bbm7 = new Chord(46, 56, 61, 65, 68);
        private static readonly Chord Eb7 = new Chord(39, 55, 61, 65, 67);
        private static readonly Chord Am7 = new Chord(45, 55, 60, 64, 67);
        private static readonly Chord G7 = new Chord(43, 53, 59, 62, 65);
        private static readonly Chord Dm7 = new Chord(38, 53, 57, 60, 65);
        private static readonly Chord Ebmaj7 = new Chord(39, 55, 58, 62, 67);

        /// <summary>The game music: sixteen bars of rhythm changes in F, swung at 108 beats a minute.</summary>
        public static float[] GameLoop()
        {
            var bars = new[]
            {
                new[] { Fmaj7 }, new[] { D7 }, new[] { Gm7 }, new[] { C7 },
                new[] { Fmaj7 }, new[] { D7 }, new[] { Gm7 }, new[] { C7 },
                new[] { Cm7, F7 }, new[] { Bbmaj7 }, new[] { Bbm7, Eb7 }, new[] { Am7, D7 },
                new[] { Gm7 }, new[] { C7 }, new[] { F6, D7 }, new[] { Gm7, C7 }
            };
            // A light vibraphone line: (bar, beat, midi, beats).
            var tune = new (int bar, float beat, int midi, float length)[]
            {
                (0, 0.67f, 72, 0.33f), (0, 1f, 74, 0.67f), (0, 1.67f, 76, 1.2f), (1, 0.67f, 74, 0.5f), (1, 1.33f, 72, 0.33f), (1, 2f, 69, 1.5f),
                (2, 1f, 70, 0.5f), (2, 1.67f, 74, 0.33f), (2, 2f, 77, 1.2f), (3, 1.67f, 76, 0.33f), (3, 2f, 74, 0.5f), (3, 2.67f, 72, 1f),
                (4, 0.67f, 72, 0.33f), (4, 1f, 74, 0.67f), (4, 1.67f, 76, 1.2f), (5, 0.67f, 78, 0.5f), (5, 1.33f, 76, 0.33f), (5, 2f, 74, 1.5f),
                (6, 1f, 77, 0.5f), (6, 1.67f, 76, 0.33f), (6, 2f, 74, 0.67f), (6, 2.67f, 70, 0.8f), (7, 1f, 72, 2.5f),
                (9, 0.67f, 74, 0.33f), (9, 1f, 77, 0.67f), (9, 1.67f, 81, 1.2f), (11, 0.67f, 79, 0.33f), (11, 1f, 76, 0.67f), (11, 1.67f, 72, 1.2f),
                (12, 1f, 74, 0.5f), (12, 1.67f, 77, 0.33f), (12, 2f, 81, 1f), (13, 1f, 79, 0.5f), (13, 1.67f, 76, 0.33f), (13, 2f, 72, 1f),
                (14, 0.67f, 69, 0.33f), (14, 1f, 72, 0.67f), (14, 2f, 74, 0.67f), (15, 0f, 76, 0.5f), (15, 1f, 74, 0.5f), (15, 2f, 72, 1.5f)
            };
            return Loop(108f, bars, tune, 11);
        }

        /// <summary>The menu music: eight gentle bars in B flat at 92 beats a minute.</summary>
        public static float[] MenuLoop()
        {
            var bars = new[]
            {
                new[] { Bbmaj7 }, new[] { G7 }, new[] { Cm7 }, new[] { F7 },
                new[] { Dm7, G7 }, new[] { Cm7, F7 }, new[] { Ebmaj7 }, new[] { Cm7, F7 }
            };
            var tune = new (int bar, float beat, int midi, float length)[]
            {
                (0, 0f, 74, 1f), (0, 1f, 77, 0.67f), (0, 1.67f, 81, 1.8f), (1, 1f, 79, 0.67f), (1, 1.67f, 77, 0.33f), (1, 2f, 74, 1.5f),
                (2, 0f, 75, 1f), (2, 1f, 79, 0.67f), (2, 1.67f, 82, 1.8f), (3, 1f, 81, 0.67f), (3, 1.67f, 79, 0.33f), (3, 2f, 77, 1.5f),
                (4, 0f, 77, 0.67f), (4, 0.67f, 74, 0.33f), (4, 1f, 77, 1f), (4, 2f, 79, 1f), (5, 0f, 75, 0.67f), (5, 0.67f, 72, 0.33f),
                (5, 1f, 75, 1f), (5, 2f, 77, 1f), (6, 0f, 79, 1.5f), (6, 2f, 74, 1f), (7, 0f, 72, 1f), (7, 1f, 70, 2.5f)
            };
            return Loop(92f, bars, tune, 23);
        }

        private static float[] Loop(float tempo, Chord[][] bars, (int bar, float beat, int midi, float length)[] tune, int seed)
        {
            float beat = 60f / tempo;
            float bar = beat * 4f;
            var track = new Track(bar * bars.Length, true);
            var random = new System.Random(seed);
            for (int b = 0; b < bars.Length; b++)
            {
                float barStart = b * bar;
                Chord[] chords = bars[b];
                for (int c = 0; c < chords.Length; c++)
                {
                    Chord chord = chords[c];
                    float chordStart = barStart + c * (bar / chords.Length);
                    int beats = 4 / chords.Length;
                    // Comping: a Charleston on the first chord of a bar, a single stab otherwise.
                    float[] hits = chords.Length == 1 ? new[] { 0f, 1.67f } : new[] { 0f };
                    foreach (float hit in hits)
                    {
                        float length = hit == 0f ? beat * 0.9f : beat * 1.2f;
                        foreach (int note in chord.voicing)
                        {
                            Piano(track, chordStart + hit * beat + (float)random.NextDouble() * 0.012f, note, length, 0.085f);
                        }
                    }
                    // Walking bass: root, chord tone, and a chromatic approach to the next root.
                    Chord next = c + 1 < chords.Length ? chords[c + 1] : bars[(b + 1) % bars.Length][0];
                    int approach = next.root + (next.root > chord.root ? -1 : 1);
                    int[] walk = beats == 4
                        ? new[] { chord.root, chord.root + Third(chord), chord.root + 7, approach }
                        : new[] { chord.root, approach };
                    for (int w = 0; w < walk.Length; w++)
                    {
                        Bass(track, chordStart + w * beat, walk[w], beat * 0.92f, 0.34f);
                    }
                }
                // Drums: ride on every beat with the swung skip note, hat on 2 and 4, brushes.
                for (int q = 0; q < 4; q++)
                {
                    float at = barStart + q * beat;
                    Ride(track, at, q % 2 == 1 ? 0.07f : 0.055f, random);
                    if (q % 2 == 1)
                    {
                        Ride(track, at + beat * 0.67f, 0.035f, random);
                        Hat(track, at, 0.08f, random);
                        Brush(track, at - beat * 0.1f, beat * 0.45f, 0.04f, random);
                    }
                }
            }
            foreach (var note in tune)
            {
                Vibes(track, note.bar * bar + note.beat * beat, note.midi, note.length * beat, 0.16f);
            }
            return Normalize(track.Samples, 0.8f);
        }

        /// <summary>The chord's third above the root: minor when the voicing holds a minor third.</summary>
        private static int Third(Chord chord)
        {
            foreach (int note in chord.voicing)
            {
                if ((note - chord.root) % 12 == 3)
                {
                    return 3;
                }
            }
            return 4;
        }

        // ------------------------------------------------------------------ effects

        /// <summary>A short brassy fanfare for the winner: a rising arpeggio and a held chord.</summary>
        public static float[] Fanfare()
        {
            var track = new Track(3.2f);
            int[] rise = { 65, 69, 72, 77 };
            for (int i = 0; i < rise.Length; i++)
            {
                Brass(track, i * 0.16f, rise[i], 0.15f, 0.5f);
            }
            foreach (int note in new[] { 65, 69, 72, 77, 81 })
            {
                Brass(track, 0.72f, note, 1.7f, 0.32f);
            }
            var random = new System.Random(3);
            Ride(track, 0.72f, 0.25f, random);
            for (int i = 0; i < 6; i++)
            {
                Vibes(track, 0.72f + i * 0.09f, 84 + i % 3 * 3, 0.3f, 0.12f);
            }
            return Normalize(track.Samples, 0.9f);
        }

        private static void Brass(Track track, float start, int midi, float duration, float volume)
        {
            float f = Note(midi);
            int first = Mathf.RoundToInt(start * SampleRate);
            int count = Mathf.RoundToInt((duration + 0.2f) * SampleRate);
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float release = t < duration ? 1f : Mathf.Exp(-(t - duration) * 18f);
                float envelope = Mathf.Clamp01(t / 0.03f) * (0.75f + 0.25f * Mathf.Exp(-t * 6f)) * release;
                float brightness = 0.6f + 0.4f * Mathf.Clamp01(t / 0.08f);
                float value = 0f;
                for (int h = 1; h <= 6; h++)
                {
                    value += Mathf.Sin(2f * Mathf.PI * f * h * t * (1f + 0.002f * Mathf.Sin(2f * Mathf.PI * 5f * t))) * Mathf.Pow(brightness, h) / h;
                }
                track.Add(first + i, value * envelope * volume * 0.5f);
            }
        }

        /// <summary>The jail door: a heavy metal clank with a ringing tail.</summary>
        public static float[] JailDoor()
        {
            var track = new Track(1.2f);
            var random = new System.Random(5);
            float[] partials = { 187f, 412f, 655f, 998f, 1432f };
            for (int hit = 0; hit < 2; hit++)
            {
                int first = Mathf.RoundToInt(hit * 0.11f * SampleRate);
                float strength = hit == 0 ? 0.55f : 1f;
                for (int i = 0; i < SampleRate; i++)
                {
                    float t = i / (float)SampleRate;
                    float value = 0f;
                    for (int p = 0; p < partials.Length; p++)
                    {
                        value += Mathf.Sin(2f * Mathf.PI * partials[p] * t) * Mathf.Exp(-t * (6f + p * 3f)) / (p + 1f);
                    }
                    float noise = ((float)random.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 60f);
                    track.Add(first + i, (value * 0.7f + noise * 0.6f) * strength);
                }
            }
            return Normalize(track.Samples, 0.85f);
        }

        /// <summary>The auction gavel: two wooden knocks.</summary>
        public static float[] Gavel()
        {
            var track = new Track(0.6f);
            var random = new System.Random(8);
            for (int hit = 0; hit < 2; hit++)
            {
                int first = Mathf.RoundToInt(hit * 0.18f * SampleRate);
                for (int i = 0; i < SampleRate * 0.3f; i++)
                {
                    float t = i / (float)SampleRate;
                    float body = Mathf.Sin(2f * Mathf.PI * (210f + 180f * Mathf.Exp(-t * 40f)) * t) * Mathf.Exp(-t * 28f);
                    float click = ((float)random.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 180f);
                    track.Add(first + i, body + click * 0.5f);
                }
            }
            return Normalize(track.Samples, 0.8f);
        }

        /// <summary>A whoosh: noise swept through a resonant band.</summary>
        public static float[] Whoosh()
        {
            var track = new Track(0.55f);
            var random = new System.Random(9);
            float low = 0f, band = 0f;
            for (int i = 0; i < track.Samples.Length; i++)
            {
                float t = i / (float)SampleRate;
                float cutoff = Mathf.Lerp(0.04f, 0.35f, Mathf.Sin(Mathf.PI * t / 0.55f));
                float noise = (float)random.NextDouble() * 2f - 1f;
                low += cutoff * band;
                float high = noise - low - 0.5f * band;
                band += cutoff * high;
                track.Add(i, band * Mathf.Sin(Mathf.PI * t / 0.55f));
            }
            return Normalize(track.Samples, 0.7f);
        }

        /// <summary>The sad trombone: four notes sliding down, the last one wobbling.</summary>
        public static float[] SadTrombone()
        {
            var track = new Track(2.6f);
            int[] notes = { 58, 57, 56, 55 };
            float[] starts = { 0f, 0.42f, 0.84f, 1.26f };
            float[] lengths = { 0.38f, 0.38f, 0.38f, 1.1f };
            for (int n = 0; n < notes.Length; n++)
            {
                float f = Note(notes[n]);
                int first = Mathf.RoundToInt(starts[n] * SampleRate);
                int count = Mathf.RoundToInt((lengths[n] + 0.1f) * SampleRate);
                float phase = 0f;
                for (int i = 0; i < count; i++)
                {
                    float t = i / (float)SampleRate;
                    float wobble = n == notes.Length - 1 ? 1f + 0.03f * Mathf.Sin(2f * Mathf.PI * 6f * t) * Mathf.Clamp01(t / 0.3f) : 1f;
                    float slide = 1f - 0.03f * Mathf.Clamp01((t - lengths[n] * 0.6f) / (lengths[n] * 0.4f));
                    phase += f * wobble * slide / SampleRate;
                    float release = t < lengths[n] ? 1f : Mathf.Exp(-(t - lengths[n]) * 30f);
                    float envelope = Mathf.Clamp01(t / 0.04f) * release;
                    float value = 0f;
                    for (int h = 1; h <= 7; h++)
                    {
                        value += Mathf.Sin(2f * Mathf.PI * phase * h) * (h == 1 ? 1f : 0.7f / h);
                    }
                    track.Add(first + i, value * envelope * 0.4f);
                }
            }
            return Normalize(track.Samples, 0.8f);
        }

        // ------------------------------------------------------------------ output

        private static float[] Normalize(float[] samples, float peak)
        {
            float max = 0f;
            foreach (float s in samples)
            {
                max = Mathf.Max(max, Mathf.Abs(s));
            }
            if (max <= 0f)
            {
                return samples;
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
                int bytes = samples.Length * 2;
                writer.Write(new[] { 'R', 'I', 'F', 'F' });
                writer.Write(36 + bytes);
                writer.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)1);
                writer.Write(SampleRate);
                writer.Write(SampleRate * 2);
                writer.Write((short)2);
                writer.Write((short)16);
                writer.Write(new[] { 'd', 'a', 't', 'a' });
                writer.Write(bytes);
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
