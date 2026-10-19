using Gamebox.Editor;
using UnityEngine;
using Wave = Gamebox.Editor.SoundSynth.Wave;
using SynthTrack = Gamebox.Editor.SoundSynth.Track;

namespace Portfolio.DeathRace.EditorTools
{
    internal static partial class DeathRaceSounds
    {
        /// <summary>
        /// A piece of music being written: looping tracks for the drums and bass (dry), the guitar (distorted as a whole)
        /// and the leads (with their own echo), mixed at the end. Everything is a loop, so notes and echoes that ring past
        /// the end come round at the start and the seam is seamless.
        /// </summary>
        private sealed class Song
        {
            public readonly SynthTrack Dry;
            public readonly SynthTrack Guitar;
            public readonly SynthTrack Lead;
            public readonly float Beat;
            public readonly float Bar;
            public readonly int Bars;

            public Song(float bpm, int bars, int seed)
            {
                Beat = 60f / bpm;
                Bar = Beat * 4f;
                Bars = bars;
                Dry = new SynthTrack(bars * Bar, true, seed);
                Guitar = new SynthTrack(bars * Bar, true, seed + 1);
                Lead = new SynthTrack(bars * Bar, true, seed + 2);
            }

            /// <summary>The time of a beat (fractions allowed) in a bar.</summary>
            public float At(int bar, float beat)
            {
                return bar * Bar + beat * Beat;
            }

            /// <summary>The finished mix: the guitar driven hard and darkened, the leads echoed, everything mastered as a loop.</summary>
            public float[] Master(float guitar, float lead, float echo, float peak)
            {
                SoundSynth.Distort(Guitar, 5f);
                SoundSynth.LowPass(Guitar, SoundSynth.Cutoff(2800f));
                SoundSynth.LowPass(Guitar, SoundSynth.Cutoff(3600f));
                SoundSynth.HighPass(Guitar, SoundSynth.Cutoff(90f));
                SoundSynth.Echo(Lead, Beat * 0.75f, echo, 3);
                SoundSynth.Mix(Dry, Guitar.Samples, 0f, guitar);
                SoundSynth.Mix(Dry, Lead.Samples, 0f, lead);
                return SoundSynth.Finish(Dry, peak);
            }
        }

        /// <summary>A plucked synth bass note: a low-passed saw over a sine an octave down.</summary>
        private static void BassNote(SynthTrack track, float start, float duration, int midi, float volume, float decay = 3.5f)
        {
            float f = SoundSynth.Note(midi);
            SoundSynth.Tone(track, start, duration, Wave.Saw, f, f, volume, 0.004f, decay, 0f, 6f, 0.02f, 0f, 0f, 700f);
            SoundSynth.Tone(track, start, duration, Wave.Sine, f * 0.5f, f * 0.5f, volume * 0.7f, 0.004f, decay * 0.7f, 0f, 6f, 0.02f);
        }

        /// <summary>A power chord (root, fifth, octave) into the guitar track; a short decay is a palm-muted chug.</summary>
        private static void Power(SynthTrack guitar, float start, float duration, int root, float volume, float decay)
        {
            float f = SoundSynth.Note(root);
            SoundSynth.Tone(guitar, start, duration, Wave.Saw, f, f, volume, 0.003f, decay, 0f, 6f, 0.015f);
            SoundSynth.Tone(guitar, start, duration, Wave.Saw, f * 1.4983f * 1.002f, f * 1.4983f * 1.002f, volume * 0.8f, 0.003f, decay, 0f, 6f, 0.015f);
            SoundSynth.Tone(guitar, start, duration, Wave.Saw, f * 2f * 0.998f, f * 2f * 0.998f, volume * 0.55f, 0.003f, decay, 0f, 6f, 0.015f);
        }

        /// <summary>A lead note: a pulse wave with a touch of vibrato, softened.</summary>
        private static void LeadNote(SynthTrack lead, float start, float duration, int midi, float volume, Wave wave = Wave.Pulse, float cutoff = 3000f)
        {
            float f = SoundSynth.Note(midi);
            SoundSynth.Tone(lead, start, duration, wave, f, f, volume, 0.01f, 1.2f, 0.006f, 5.5f, 0.04f, 0f, 0f, cutoff);
            SoundSynth.Tone(lead, start, duration, Wave.Triangle, f * 1.003f, f * 1.003f, volume * 0.5f, 0.01f, 1.2f, 0.006f, 5.2f, 0.04f);
        }

        private static float[] Triad(int root, bool minor)
        {
            return new[] { SoundSynth.Note(root), SoundSynth.Note(root + (minor ? 3 : 4)), SoundSynth.Note(root + 7) };
        }

        // ------------------------------------------------------------------ the menu

        /// <summary>
        /// The menu: dark, driving synth-rock in E minor at 120 beats a minute, 16 bars (exactly 32 s, a seamless loop).
        /// Em-Em-C-D | Em-Em-C-B: an intro groove of kick, hats and a pad for two bars, then palm-muted power chords
        /// over a pulsing bass, a brooding lead in the second half and a snare fill into the top of the loop.
        /// </summary>
        private static float[] MenuMusic()
        {
            var song = new Song(120f, 16, 3001);
            int[] roots = { 40, 40, 36, 38, 40, 40, 36, 35, 40, 40, 36, 38, 36, 38, 35, 35 };
            bool[] minor = { true, true, false, false, true, true, false, false, true, true, false, false, false, false, false, false };
            for (int bar = 0; bar < song.Bars; bar++)
            {
                int root = roots[bar];
                bool full = bar >= 2;
                // Drums.
                for (int beat = 0; beat < 4; beat++)
                {
                    float t = song.At(bar, beat);
                    if (beat == 0 || beat == 2)
                    {
                        SoundSynth.Kick(song.Dry, t, 0.85f);
                    }
                    if (full && beat == 2)
                    {
                        SoundSynth.Kick(song.Dry, song.At(bar, 2.5f), 0.6f);
                    }
                    if (full && (beat == 1 || beat == 3))
                    {
                        SoundSynth.Snare(song.Dry, t, 0.55f);
                    }
                    SoundSynth.Hat(song.Dry, t, 0.16f);
                    SoundSynth.Hat(song.Dry, song.At(bar, beat + 0.5f), full ? 0.26f : 0.18f, full && beat == 3 ? 0.12f : 0.05f);
                }
                if (bar == 7 || bar == 15)
                {
                    for (int k = 0; k < 4; k++)
                    {
                        SoundSynth.Snare(song.Dry, song.At(bar, 3f + k * 0.25f), 0.3f + k * 0.08f);
                    }
                }
                if (bar == 0 || bar == 8)
                {
                    SoundSynth.Noise(song.Dry, song.At(bar, 0f), 1.6f, 0.18f, 1f, 2.4f, 0.002f, 0.35f);
                }
                // Pad: the chord, soft, an octave up.
                SoundSynth.Pad(song.Dry, song.At(bar, 0f), Triad(root + 12, minor[bar]), song.Bar, 0.16f);
                // Bass: pulsing eighths.
                for (int e = 0; e < 8; e++)
                {
                    if (!full && e % 2 == 1)
                    {
                        continue;
                    }
                    int note = root + (e == 7 && bar % 2 == 1 ? 12 : 0);
                    BassNote(song.Dry, song.At(bar, e * 0.5f), song.Beat * 0.48f, note, 0.38f);
                }
                // Guitar: a ringing chord on the one every other bar, chugs in between.
                if (full)
                {
                    if (bar % 2 == 0)
                    {
                        Power(song.Guitar, song.At(bar, 0f), song.Beat * 1.4f, root, 0.3f, 1.4f);
                    }
                    for (int e = bar % 2 == 0 ? 3 : 0; e < 8; e++)
                    {
                        Power(song.Guitar, song.At(bar, e * 0.5f), song.Beat * 0.4f, root, 0.22f, 9f);
                    }
                }
            }
            // The lead: two brooding phrases over the second half.
            int[] phrase = { 64, 67, 71, 69, 67, 66, 64, 62 };
            float[] beats = { 0f, 1f, 2f, 3.5f, 4.5f, 5f, 6f, 7.5f };
            float[] lengths = { 0.9f, 0.9f, 1.4f, 0.9f, 0.45f, 0.9f, 1.4f, 0.45f };
            for (int round = 0; round < 2; round++)
            {
                int startBar = 8 + round * 4;
                for (int i = 0; i < phrase.Length; i++)
                {
                    int note = phrase[i] + (round == 1 && i >= 4 ? 5 : 0);
                    LeadNote(song.Lead, song.At(startBar, beats[i]), song.Beat * lengths[i], note + 12, 0.16f);
                }
            }
            LeadNote(song.Lead, song.At(14, 0f), song.Beat * 6f, 71 + 12, 0.14f, Wave.Square, 2200f);
            return song.Master(0.55f, 0.75f, 0.32f, 0.75f);
        }

        // ------------------------------------------------------------------ the race

        /// <summary>
        /// The race: energetic in A minor at 140 beats a minute, 28 bars (exactly 48 s, a seamless loop).
        /// Am-Am-F-G | Am-Am-F-E. Four on the floor with sixteenth hats, a driving sixteenth bass, chugging power
        /// chords, an arpeggio from bar 8, the hook from bar 12, a high answer from bar 20 and a snare roll that builds
        /// into the top of the loop.
        /// </summary>
        private static float[] RaceMusic()
        {
            var song = new Song(140f, 28, 4001);
            int[] cycle = { 45, 45, 41, 43, 45, 45, 41, 40 };
            bool[] cycleMinor = { true, true, false, false, true, true, false, false };
            for (int bar = 0; bar < song.Bars; bar++)
            {
                int root = cycle[bar % cycle.Length];
                bool minor = cycleMinor[bar % cycle.Length];
                bool build = bar >= 24;
                // Drums.
                for (int beat = 0; beat < 4; beat++)
                {
                    float t = song.At(bar, beat);
                    SoundSynth.Kick(song.Dry, t, 0.9f);
                    if (beat == 1 || beat == 3)
                    {
                        SoundSynth.Snare(song.Dry, t, 0.6f);
                        SoundSynth.Clap(song.Dry, t, 0.2f);
                    }
                    for (int s = 0; s < 4; s++)
                    {
                        SoundSynth.Hat(song.Dry, song.At(bar, beat + s * 0.25f), s == 2 ? 0.24f : 0.13f, s == 2 ? 0.08f : 0.04f);
                    }
                }
                if (build)
                {
                    int hits = 4 * (bar - 23);
                    for (int k = 0; k < hits; k++)
                    {
                        SoundSynth.Snare(song.Dry, song.At(bar, k * 4f / hits), 0.12f + 0.25f * (bar - 24 + k / (float)hits) / 4f);
                    }
                }
                if (bar % 8 == 0)
                {
                    SoundSynth.Noise(song.Dry, song.At(bar, 0f), 1.5f, 0.22f, 1f, 2.2f, 0.002f, 0.35f);
                }
                // Bass: sixteenths, the octave on the off-beats of the last beat.
                for (int s = 0; s < 16; s++)
                {
                    int note = root + (s >= 12 && s % 2 == 1 ? 12 : 0);
                    BassNote(song.Dry, song.At(bar, s * 0.25f), song.Beat * 0.23f, note, s % 4 == 0 ? 0.4f : 0.3f, 6f);
                }
                // Guitar: eighth chugs with an accent on the one; held through the build.
                if (build)
                {
                    Power(song.Guitar, song.At(bar, 0f), song.Bar * 0.98f, root, 0.26f, 0.6f);
                }
                else
                {
                    Power(song.Guitar, song.At(bar, 0f), song.Beat * 0.9f, root, 0.3f, 2f);
                    for (int e = 2; e < 8; e++)
                    {
                        Power(song.Guitar, song.At(bar, e * 0.5f), song.Beat * 0.4f, root, 0.2f, 10f);
                    }
                }
                // Arpeggio: sixteenths up and down the chord, from bar 8.
                if (bar >= 8 && !build)
                {
                    int third = minor ? 3 : 4;
                    int[] shape = { 12, 12 + third, 19, 24, 19, 12 + third, 12, 7 };
                    for (int s = 0; s < 16; s++)
                    {
                        LeadNote(song.Lead, song.At(bar, s * 0.25f), song.Beat * 0.2f, root + 12 + shape[s % shape.Length], 0.05f, Wave.Pulse, 3800f);
                    }
                }
                SoundSynth.Pad(song.Dry, song.At(bar, 0f), Triad(root + 12, minor), song.Bar, bar >= 8 ? 0.1f : 0.14f);
            }
            // The hook (bars 12-19), and its high answer (bars 20-23).
            int[] hook = { 69, 72, 74, 76, 74, 72, 74, 67, 69 };
            float[] hookBeats = { 0f, 0.5f, 1f, 1.5f, 2.5f, 3f, 3.5f, 5f, 6f };
            float[] hookLengths = { 0.45f, 0.45f, 0.45f, 0.9f, 0.45f, 0.45f, 1.4f, 0.9f, 1.9f };
            for (int round = 0; round < 4; round++)
            {
                int startBar = 12 + round * 2;
                int lift = round == 3 ? 5 : 0;
                for (int i = 0; i < hook.Length; i++)
                {
                    LeadNote(song.Lead, song.At(startBar, hookBeats[i]), song.Beat * hookLengths[i], hook[i] + 12 + lift, 0.15f, Wave.Square, 2600f);
                }
            }
            int[] answer = { 81, 79, 76, 79, 81, 84, 83, 79 };
            for (int i = 0; i < answer.Length; i++)
            {
                LeadNote(song.Lead, song.At(20 + i / 2, (i % 2) * 2f), song.Beat * 1.9f, answer[i] + 12, 0.11f, Wave.Pulse, 3200f);
            }
            return song.Master(0.6f, 0.7f, 0.28f, 0.75f);
        }
    }
}
