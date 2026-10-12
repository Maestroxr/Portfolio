using System.IO;
using UnityEngine;

namespace Portfolio.BoardGames.EditorTools
{
    /// <summary>
    /// A small synthesiser for the sounds of the games: a click for the interface, a slide for a plain move, the thud of
    /// a capture, a chime for a new king, a short fanfare for a win and a falling tone for a loss; for chess a bright
    /// two-note check; for backgammon the dice rattling in the cup and thrown, a wooden checker landing, the soft thunk
    /// of a checker borne off and the click-clack of the doubling cube. Everything is seeded, so a rebuild writes the
    /// same bytes.
    /// </summary>
    internal static class BoardGamesSounds
    {
        public const int SampleRate = 22050;

        /// <summary>A short wooden tap: the interface click.</summary>
        public static float[] Click()
        {
            float[] samples = Track(0.08f);
            var random = new System.Random(11);
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Exp(-t * 90f);
                float tone = Mathf.Sin(2f * Mathf.PI * 1800f * t) * 0.6f + Mathf.Sin(2f * Mathf.PI * 2700f * t) * 0.3f;
                float noise = ((float)random.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 300f);
                samples[i] = (tone * env + noise * 0.4f) * 0.5f;
            }
            return Fade(samples);
        }

        /// <summary>A piece sliding over the board and settling: filtered noise with a soft knock at the end.</summary>
        public static float[] Slide()
        {
            float[] samples = Track(0.28f);
            var random = new System.Random(23);
            float low = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)SampleRate;
                float white = (float)random.NextDouble() * 2f - 1f;
                low += (white - low) * 0.12f;
                float swish = Mathf.Sin(Mathf.Clamp01(t / 0.2f) * Mathf.PI) * 0.35f;
                float knock = t > 0.2f ? Mathf.Sin(2f * Mathf.PI * 240f * (t - 0.2f)) * Mathf.Exp(-(t - 0.2f) * 60f) * 0.6f : 0f;
                samples[i] = low * swish + knock;
            }
            return Fade(samples);
        }

        /// <summary>A capture: a deeper knock with a little body.</summary>
        public static float[] Capture()
        {
            float[] samples = Track(0.32f);
            var random = new System.Random(37);
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Exp(-t * 18f);
                float body = Mathf.Sin(2f * Mathf.PI * (150f - t * 120f) * t) * env;
                float tap = Mathf.Sin(2f * Mathf.PI * 900f * t) * Mathf.Exp(-t * 120f) * 0.5f;
                float noise = ((float)random.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 200f) * 0.3f;
                samples[i] = (body * 0.8f + tap + noise) * 0.7f;
            }
            return Fade(samples);
        }

        /// <summary>A new king: two bright bell notes.</summary>
        public static float[] King()
        {
            float[] samples = Track(0.9f);
            Bell(samples, 0f, 76, 0.7f, 0.35f);
            Bell(samples, 0.16f, 83, 0.74f, 0.4f);
            return Fade(samples);
        }

        /// <summary>The winner's fanfare: a rising arpeggio with a held chord.</summary>
        public static float[] Win()
        {
            float[] samples = Track(2.2f);
            int[] notes = { 67, 71, 74, 79 };
            for (int i = 0; i < notes.Length; i++)
            {
                Bell(samples, i * 0.14f, notes[i], 1.6f - i * 0.1f, 0.3f);
            }
            Bell(samples, 0.7f, 74, 1.4f, 0.22f);
            Bell(samples, 0.7f, 79, 1.4f, 0.22f);
            Bell(samples, 0.7f, 83, 1.4f, 0.26f);
            return Fade(samples);
        }

        /// <summary>The loser's tone: two soft notes falling.</summary>
        public static float[] Lose()
        {
            float[] samples = Track(1.4f);
            Bell(samples, 0f, 64, 0.9f, 0.32f);
            Bell(samples, 0.42f, 59, 1.0f, 0.3f);
            return Fade(samples);
        }

        /// <summary>Chess: a king in check, two short bright notes a fourth apart.</summary>
        public static float[] Check()
        {
            float[] samples = Track(0.55f);
            Bell(samples, 0f, 84, 0.32f, 0.36f);
            Bell(samples, 0.09f, 89, 0.42f, 0.42f);
            return Fade(samples);
        }

        /// <summary>
        /// Backgammon: two dice shaken in a cup (a quick run of hollow rattles), then thrown onto the board, where they
        /// bounce three times and settle.
        /// </summary>
        public static float[] Roll()
        {
            float[] samples = Track(1.0f);
            var random = new System.Random(53);
            // The rattle: short bursts of noise ringing in the cup, at uneven times.
            float time = 0.01f;
            while (time < 0.46f)
            {
                float volume = 0.22f + (float)random.NextDouble() * 0.18f;
                float pitch = 520f + (float)random.NextDouble() * 260f;
                Knock(samples, time, pitch, 140f, volume, 0.75f, random);
                time += 0.028f + (float)random.NextDouble() * 0.035f;
            }
            // The throw: two dice hitting the board, harder and drier, each bounce softer and sooner.
            float[] bounces = { 0.56f, 0.68f, 0.76f, 0.81f };
            for (int i = 0; i < bounces.Length; i++)
            {
                float volume = 0.75f * Mathf.Pow(0.62f, i);
                Knock(samples, bounces[i], 1250f + i * 60f, 90f, volume, 0.5f, random);
                Knock(samples, bounces[i] + 0.018f, 1480f - i * 40f, 95f, volume * 0.8f, 0.5f, random);
            }
            return Fade(samples);
        }

        /// <summary>Backgammon: a wooden checker set down on a point, a short hollow knock.</summary>
        public static float[] Clack()
        {
            float[] samples = Track(0.18f);
            var random = new System.Random(61);
            Knock(samples, 0f, 760f, 55f, 0.8f, 0.35f, random);
            Knock(samples, 0.002f, 1640f, 90f, 0.35f, 0f, random);
            return Fade(samples);
        }

        /// <summary>Backgammon: a checker borne off into the tray, a soft low thunk.</summary>
        public static float[] BearOff()
        {
            float[] samples = Track(0.3f);
            var random = new System.Random(67);
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)SampleRate;
                float attack = Mathf.Clamp01(t / 0.004f);
                float body = Mathf.Sin(2f * Mathf.PI * (135f - t * 90f) * t) * Mathf.Exp(-t * 22f);
                float felt = ((float)random.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 70f) * 0.18f;
                samples[i] = (body * 0.75f + felt) * attack;
            }
            return Fade(samples);
        }

        /// <summary>Backgammon: the doubling cube turned and set down, a click and a clack.</summary>
        public static float[] Cube()
        {
            float[] samples = Track(0.3f);
            var random = new System.Random(71);
            Knock(samples, 0f, 2100f, 140f, 0.5f, 0.4f, random);
            Knock(samples, 0.1f, 980f, 70f, 0.75f, 0.35f, random);
            Knock(samples, 0.102f, 1900f, 110f, 0.3f, 0f, random);
            return Fade(samples);
        }

        /// <summary>Encodes samples as 16 bit mono PCM.</summary>
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

        private static float[] Track(float seconds)
        {
            return new float[Mathf.CeilToInt(seconds * SampleRate)];
        }

        private static float Note(int midi)
        {
            return 440f * Mathf.Pow(2f, (midi - 69) / 12f);
        }

        /// <summary>Adds a struck bell: a sine with a faint third harmonic that decays over <paramref name="duration"/>.</summary>
        private static void Bell(float[] samples, float start, int midi, float duration, float volume)
        {
            float frequency = Note(midi);
            int from = Mathf.RoundToInt(start * SampleRate);
            int count = Mathf.RoundToInt(duration * SampleRate);
            for (int i = 0; i < count && from + i < samples.Length; i++)
            {
                float t = i / (float)SampleRate;
                float attack = Mathf.Clamp01(t / 0.006f);
                float env = attack * Mathf.Exp(-t * (3.5f / duration));
                float value = Mathf.Sin(2f * Mathf.PI * frequency * t) + Mathf.Sin(2f * Mathf.PI * frequency * 3f * t) * 0.12f * Mathf.Exp(-t * 8f);
                samples[from + i] += value * env * volume;
            }
        }

        /// <summary>
        /// Adds a knock: a tone at <paramref name="frequency"/> dying away at <paramref name="decay"/> per second, mixed
        /// with a burst of noise (<paramref name="noise"/> of it) that dies faster, starting at <paramref name="start"/>.
        /// </summary>
        private static void Knock(float[] samples, float start, float frequency, float decay, float volume, float noise, System.Random random)
        {
            int from = Mathf.RoundToInt(start * SampleRate);
            int count = Mathf.RoundToInt(6f / decay * SampleRate);
            float low = 0f;
            for (int i = 0; i < count && from + i < samples.Length; i++)
            {
                float t = i / (float)SampleRate;
                float attack = Mathf.Clamp01(t / 0.0015f);
                float tone = Mathf.Sin(2f * Mathf.PI * frequency * t) * Mathf.Exp(-t * decay);
                float white = (float)random.NextDouble() * 2f - 1f;
                low += (white - low) * 0.35f;
                float burst = low * Mathf.Exp(-t * decay * 2.5f);
                samples[from + i] += (tone * (1f - noise) + burst * noise * 1.6f) * attack * volume;
            }
        }

        /// <summary>Fades the last 10 ms out and keeps the peak under 0.9.</summary>
        private static float[] Fade(float[] samples)
        {
            int tail = Mathf.Min(samples.Length, SampleRate / 100);
            for (int i = 0; i < tail; i++)
            {
                samples[samples.Length - 1 - i] *= i / (float)tail;
            }
            float peak = 0f;
            foreach (float s in samples)
            {
                peak = Mathf.Max(peak, Mathf.Abs(s));
            }
            if (peak > 0.9f)
            {
                float gain = 0.9f / peak;
                for (int i = 0; i < samples.Length; i++)
                {
                    samples[i] *= gain;
                }
            }
            return samples;
        }
    }
}
