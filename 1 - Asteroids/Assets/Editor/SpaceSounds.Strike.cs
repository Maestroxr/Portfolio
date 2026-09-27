using System;
using UnityEngine;

namespace Portfolio.Asteroids.EditorTools
{
    /// <summary>
    /// The strike sounds and music of the synthesizer: the guns, missiles, beams and bombs of the strike arsenal, ground
    /// explosions, the megabomb, money and item pickups, the Supply Room's till, the pilot's warnings, the boss alarm, the
    /// fly-off whoosh, and two loops (the strike mission and its boss).
    /// </summary>
    internal static partial class SpaceSounds
    {
        /// <summary>The strike effects, written to Art/Strike/Audio/{name}.wav.</summary>
        public static readonly string[] StrikeEffectNames =
        {
            "MachineGun", "MissileLaunch", "BeamHum", "LaserZap", "BombDrop", "GroundBoom", "Megabomb", "CashPickup", "ItemPickup",
            "ShopBuy", "ShopSell", "ShieldLow", "WeaponLost", "BossAlarm", "FlyBy"
        };


        /// <summary>Whether a strike effect is a seamless loop (the beam's hum and the boss alarm).</summary>
        public static bool IsStrikeLoop(string name)
        {
            return name == "BeamHum" || name == "BossAlarm";
        }


        /// <summary>The samples of the strike effect <paramref name="name"/>.</summary>
        public static float[] StrikeEffect(string name)
        {
            noiseState = 31337;
            Buffer b;
            switch (name)
            {
                case "MachineGun":
                    // A short dry crack with a low thump: it repeats twelve times a second, so it stays tiny.
                    b = new Buffer(0.09f);
                    Tone(b, 0f, 0.05f, Wave.Noise, 9000f, 2500f, 0.55f, 0.0005f, 45f);
                    Tone(b, 0f, 0.06f, Wave.Square, 260f, 90f, 0.3f, 0.0005f, 40f, 0f, 6f, 0.01f, 0f, 0f, 1800f);
                    Tone(b, 0f, 0.012f, Wave.Pulse, 3200f, 1800f, 0.15f, 0.0005f, 90f);
                    break;
                case "MissileLaunch":
                    b = new Buffer(0.55f);
                    Tone(b, 0f, 0.05f, Wave.Noise, 6000f, 3000f, 0.45f, 0.001f, 40f);
                    Sweep(b, 0.01f, 0.5f, 2600f, 700f, 0.6f, 0.01f, 5f);
                    Tone(b, 0f, 0.18f, Wave.Sine, 180f, 70f, 0.4f, 0.002f, 14f);
                    break;
                case "BeamHum":
                {
                    // One second of an electric drone whose partials all complete whole cycles, so the loop is seamless.
                    b = new Buffer(1f, true);
                    Tone(b, 0f, 1f, Wave.Saw, 110f, 110f, 0.22f, 0.0001f, 0f, 0f, 6f, 0f, 0f, 0f, 1400f);
                    Tone(b, 0f, 1f, Wave.Square, 220f, 220f, 0.1f, 0.0001f, 0f, 0.01f, 8f, 0f, 0f, 0f, 2400f);
                    Tone(b, 0f, 1f, Wave.Sine, 880f, 880f, 0.08f, 0.0001f, 0f, 0.004f, 12f, 0f, 2f, 0.4f);
                    for (int i = 0; i < b.Samples.Length; i++)
                    {
                        float t = i / (float)SampleRate;
                        b.Samples[i] *= 0.8f + 0.2f * Mathf.Sin(t * Mathf.PI * 2f * 16f);
                    }
                    break;
                }
                case "LaserZap":
                    b = new Buffer(0.16f);
                    Tone(b, 0f, 0.14f, Wave.Saw, 4200f, 900f, 0.25f, 0.0008f, 18f, 0f, 6f, 0.01f, 0f, 0f, 6000f);
                    Tone(b, 0f, 0.12f, Wave.Sine, 2400f, 600f, 0.35f, 0.0008f, 16f, 0f, 6f, 0.01f, 1.5f, 0.4f);
                    break;
                case "BombDrop":
                    b = new Buffer(0.7f);
                    Tone(b, 0f, 0.05f, Wave.Noise, 3000f, 1500f, 0.35f, 0.001f, 30f);
                    Tone(b, 0.02f, 0.65f, Wave.Sine, 1800f, 500f, 0.25f, 0.02f, 2.5f, 0.01f, 9f);
                    Tone(b, 0f, 0.1f, Wave.Square, 140f, 80f, 0.25f, 0.001f, 25f, 0f, 6f, 0.01f, 0f, 0f, 900f);
                    break;
                case "GroundBoom":
                    b = new Buffer(1.3f);
                    Boom(b, 0f, 1.2f, 0.9f, 2600f);
                    Tone(b, 0f, 0.5f, Wave.Sine, 70f, 30f, 0.6f, 0.002f, 5f);
                    Tone(b, 0.05f, 0.9f, Wave.Noise, 900f, 150f, 0.3f, 0.05f, 3f);
                    break;
                case "Megabomb":
                    b = new Buffer(2.4f);
                    Sweep(b, 0f, 0.35f, 800f, 6000f, 0.5f, 0.2f, 1f);
                    Boom(b, 0.3f, 2f, 1f, 5000f);
                    Tone(b, 0.3f, 1.6f, Wave.Sine, 55f, 25f, 0.8f, 0.004f, 1.6f);
                    Tone(b, 0.3f, 1.2f, Wave.Saw, 330f, 60f, 0.12f, 0.002f, 2.4f, 0f, 6f, 0.05f, 0f, 0f, 1800f);
                    Echo(b, 0.21f, 0.25f, 3);
                    break;
                case "CashPickup":
                    // A cash register "ka-ching": a click and two bright bells.
                    b = new Buffer(0.7f);
                    Tone(b, 0f, 0.03f, Wave.Noise, 7000f, 5000f, 0.35f, 0.0005f, 60f);
                    Tone(b, 0.04f, 0.5f, Wave.Sine, Midi(88), Midi(88), 0.35f, 0.001f, 6f, 0f, 6f, 0.05f, 3.01f, 0.25f);
                    Tone(b, 0.1f, 0.55f, Wave.Sine, Midi(93), Midi(93), 0.35f, 0.001f, 5f, 0f, 6f, 0.05f, 3.01f, 0.25f);
                    Tone(b, 0.1f, 0.3f, Wave.Triangle, Midi(81), Midi(81), 0.15f, 0.001f, 9f);
                    break;
                case "ItemPickup":
                    b = new Buffer(0.5f);
                    for (int i = 0; i < 4; i++)
                    {
                        int note = new[] { 67, 71, 74, 79 }[i];
                        Tone(b, i * 0.055f, 0.25f, Wave.Square, Midi(note), Midi(note), 0.14f, 0.002f, 9f, 0f, 6f, 0.02f, 0f, 0f, 4000f);
                    }
                    Tone(b, 0.22f, 0.26f, Wave.Sine, Midi(91), Midi(91), 0.2f, 0.002f, 7f, 0.01f, 12f);
                    break;
                case "ShopBuy":
                    b = new Buffer(0.45f);
                    Tone(b, 0f, 0.02f, Wave.Noise, 6000f, 4000f, 0.3f, 0.0005f, 70f);
                    Tone(b, 0.02f, 0.18f, Wave.Triangle, Midi(76), Midi(76), 0.3f, 0.001f, 10f);
                    Tone(b, 0.1f, 0.3f, Wave.Triangle, Midi(83), Midi(83), 0.3f, 0.001f, 8f);
                    Tone(b, 0.1f, 0.3f, Wave.Sine, Midi(95), Midi(95), 0.12f, 0.001f, 8f);
                    break;
                case "ShopSell":
                    b = new Buffer(0.45f);
                    Tone(b, 0f, 0.02f, Wave.Noise, 6000f, 4000f, 0.3f, 0.0005f, 70f);
                    Tone(b, 0.02f, 0.18f, Wave.Triangle, Midi(83), Midi(83), 0.3f, 0.001f, 10f);
                    Tone(b, 0.1f, 0.3f, Wave.Triangle, Midi(76), Midi(76), 0.3f, 0.001f, 8f);
                    Tone(b, 0.1f, 0.25f, Wave.Sine, Midi(64), Midi(64), 0.15f, 0.001f, 8f);
                    break;
                case "ShieldLow":
                    // Two falling beeps of a cockpit warning.
                    b = new Buffer(0.6f);
                    Tone(b, 0f, 0.16f, Wave.Square, 1180f, 1180f, 0.22f, 0.002f, 2f, 0f, 6f, 0.02f, 0f, 0f, 3500f);
                    Tone(b, 0.22f, 0.16f, Wave.Square, 880f, 880f, 0.22f, 0.002f, 2f, 0f, 6f, 0.02f, 0f, 0f, 3500f);
                    break;
                case "WeaponLost":
                    b = new Buffer(0.8f);
                    Tone(b, 0f, 0.06f, Wave.Noise, 5000f, 2000f, 0.5f, 0.001f, 30f);
                    Tone(b, 0.02f, 0.6f, Wave.Saw, 700f, 90f, 0.25f, 0.002f, 3f, 0.02f, 18f, 0.05f, 0f, 0f, 2200f);
                    Tone(b, 0.02f, 0.4f, Wave.Square, 350f, 60f, 0.2f, 0.002f, 5f, 0f, 6f, 0.05f, 0f, 0f, 1200f);
                    for (int i = 0; i < 5; i++)
                    {
                        Tone(b, 0.05f + i * 0.07f, 0.03f, Wave.Noise, 8000f, 4000f, 0.2f * (1f - i / 5f), 0.0005f, 60f);
                    }
                    break;
                case "BossAlarm":
                {
                    // A two-tone klaxon over two seconds: 1 s high, 1 s low, whole cycles so the loop is seamless.
                    b = new Buffer(2f, true);
                    Tone(b, 0f, 1f, Wave.Square, 660f, 660f, 0.18f, 0.02f, 0f, 0f, 6f, 0.04f, 0f, 0f, 2200f);
                    Tone(b, 1f, 1f, Wave.Square, 495f, 495f, 0.18f, 0.02f, 0f, 0f, 6f, 0.04f, 0f, 0f, 2200f);
                    Tone(b, 0f, 1f, Wave.Saw, 330f, 330f, 0.08f, 0.02f, 0f, 0f, 6f, 0.04f, 0f, 0f, 1200f);
                    Tone(b, 1f, 1f, Wave.Saw, 247.5f, 247.5f, 0.08f, 0.02f, 0f, 0f, 6f, 0.04f, 0f, 0f, 1200f);
                    break;
                }
                case "FlyBy":
                    b = new Buffer(2.2f);
                    Sweep(b, 0f, 2.1f, 300f, 4200f, 0.9f, 0.6f, 1.2f);
                    Tone(b, 0f, 2.1f, Wave.Saw, 90f, 260f, 0.12f, 0.5f, 1.2f, 0.01f, 5f, 0.3f, 0f, 0f, 900f);
                    Tone(b, 0.2f, 1.8f, Wave.Sine, 180f, 520f, 0.18f, 0.4f, 1.4f);
                    break;
                default:
                    throw new ArgumentException($"No strike sound named {name}.", nameof(name));
            }
            // The square-wave warnings are dense: a lower peak keeps them as loud as the rest.
            float peak = name == "BossAlarm" ? 0.5f : name == "ShieldLow" ? 0.65f : 0.9f;
            return Finish(b.Samples, peak, b.Loop);
        }


        /// <summary>
        /// The strike mission loop: 16 bars of military synth at 132 BPM in E minor (Em - C - D - Bm): a galloping bass,
        /// marching snares, brass-like stabs and a heroic square lead from the fifth bar.
        /// </summary>
        public static float[] StrikeMusic()
        {
            noiseState = 424242;
            const float bpm = 132f;
            const int bars = 16;
            float beat = 60f / bpm;
            float sixteenth = beat / 4f;
            var b = new Buffer(bars * 4 * beat, true);
            int[] roots = { 40, 36, 38, 35 };
            int[][] chords = { new[] { 64, 67, 71 }, new[] { 60, 64, 67 }, new[] { 62, 66, 69 }, new[] { 59, 62, 66 } };
            int[][] melody =
            {
                new[] { 76, 0, 76, 79, 83, 0, 81, 79 }, new[] { 76, 0, 72, 76, 79, 0, 76, 72 },
                new[] { 74, 0, 78, 81, 86, 0, 83, 81 }, new[] { 83, 81, 78, 74, 71, 0, 74, 78 }
            };
            int[] gallop = { 0, 2, 3, 4, 6, 7, 8, 10, 11, 12, 14, 15 };
            for (int bar = 0; bar < bars; bar++)
            {
                int section = bar % 4;
                bool intro = bar < 4;
                bool lift = bar >= 12;
                float barStart = bar * 4 * beat;
                foreach (int s in gallop)
                {
                    float at = barStart + s * sixteenth;
                    int bass = roots[section] - 12 + (s % 4 == 0 ? 0 : 12);
                    Tone(b, at, sixteenth * 0.85f, Wave.Saw, Midi(bass), Midi(bass), 0.19f, 0.002f, 7f, 0f, 6f, 0.01f, 0f, 0f, 800f);
                }
                for (int s = 0; s < 16; s++)
                {
                    float at = barStart + s * sixteenth;
                    if (s % 4 == 0 || (s == 14 && bar % 2 == 1))
                    {
                        Kick(b, at, 0.6f);
                    }
                    if (!intro && (s == 4 || s == 12))
                    {
                        Snare(b, at, 0.3f);
                    }
                    if (bar % 4 == 3 && s >= 12)
                    {
                        Snare(b, at, 0.12f + (s - 12) * 0.04f);
                    }
                    Hat(b, at, s % 2 == 0 ? 0.07f : 0.035f);
                }
                foreach (int note in chords[section])
                {
                    Tone(b, barStart, beat * 0.45f, Wave.Saw, Midi(note), Midi(note), 0.06f, 0.004f, 5f, 0f, 6f, 0.02f, 0f, 0f, 2200f);
                    Tone(b, barStart + beat * 1.5f, beat * 0.45f, Wave.Saw, Midi(note), Midi(note), 0.05f, 0.004f, 5f, 0f, 6f, 0.02f, 0f, 0f, 2200f);
                    Tone(b, barStart, 4 * beat, Wave.Triangle, Midi(note - 12), Midi(note - 12), 0.03f, 0.2f, 0.3f, 0f, 6f, 0.1f, 0f, 0f, 900f);
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
                        int note = line[n] + (lift ? 12 : 0);
                        Tone(b, barStart + n * beat / 2f, beat * 0.46f, Wave.Square, Midi(note), Midi(note), 0.065f, 0.004f, 2.5f, 0.005f, 6f, 0.02f, 0f, 0f, 2800f);
                    }
                }
            }
            Echo(b, beat * 0.75f, 0.18f, 2);
            return Finish(b.Samples, 0.85f, true);
        }


        /// <summary>
        /// The strike boss loop: 8 bars at 150 BPM in C minor, a pounding octave bass, double-time drums, an alarm-like
        /// siren lead and descending brass stabs.
        /// </summary>
        public static float[] StrikeBossMusic()
        {
            noiseState = 9001;
            const float bpm = 150f;
            const int bars = 8;
            float beat = 60f / bpm;
            float sixteenth = beat / 4f;
            var b = new Buffer(bars * 4 * beat, true);
            int[] ostinato = { 36, 48, 36, 36, 48, 36, 39, 36, 36, 48, 36, 43, 42, 41, 39, 38 };
            int[] shifts = { 0, 0, -4, -2, 0, 0, 3, -1 };
            for (int bar = 0; bar < bars; bar++)
            {
                float barStart = bar * 4 * beat;
                int shift = shifts[bar];
                for (int s = 0; s < 16; s++)
                {
                    float at = barStart + s * sixteenth;
                    int bass = ostinato[s] + shift;
                    Tone(b, at, sixteenth * 0.9f, Wave.Saw, Midi(bass), Midi(bass), 0.22f, 0.002f, 6f, 0f, 6f, 0.01f, 0f, 0f, 1000f);
                    Tone(b, at, sixteenth * 0.9f, Wave.Square, Midi(bass - 12), Midi(bass - 12), 0.1f, 0.002f, 8f, 0f, 6f, 0.01f, 0f, 0f, 400f);
                    if (s % 2 == 0)
                    {
                        Kick(b, at, s % 4 == 0 ? 0.7f : 0.4f);
                    }
                    if (s == 4 || s == 12 || (bar % 2 == 1 && s == 14))
                    {
                        Snare(b, at, 0.35f);
                    }
                    Hat(b, at, s % 2 == 0 ? 0.08f : 0.05f);
                }
                int[] stab = { 60 + shift, 63 + shift, 67 + shift };
                foreach (int note in stab)
                {
                    Tone(b, barStart, beat * 0.4f, Wave.Saw, Midi(note), Midi(note), 0.06f, 0.002f, 5f, 0f, 6f, 0.02f, 0f, 0f, 2600f);
                    Tone(b, barStart + beat * 1.5f, beat * 0.4f, Wave.Saw, Midi(note - 1), Midi(note - 1), 0.05f, 0.002f, 5f, 0f, 6f, 0.02f, 0f, 0f, 2600f);
                    Tone(b, barStart + beat * 3f, beat * 0.4f, Wave.Saw, Midi(note - 2), Midi(note - 2), 0.05f, 0.002f, 5f, 0f, 6f, 0.02f, 0f, 0f, 2600f);
                }
                if (bar >= 2)
                {
                    // The siren: a slow rise and fall of a detuned lead.
                    int lead = bar % 2 == 0 ? 79 : 75;
                    Tone(b, barStart, 2 * beat, Wave.Saw, Midi(lead), Midi(lead + 3), 0.05f, 0.1f, 0.3f, 0.012f, 5f, 0.1f, 0f, 0f, 3200f);
                    Tone(b, barStart + 2 * beat, 2 * beat, Wave.Saw, Midi(lead + 3), Midi(lead), 0.05f, 0.05f, 0.3f, 0.012f, 5f, 0.1f, 0f, 0f, 3200f);
                }
            }
            Echo(b, beat * 0.5f, 0.15f, 2);
            return Finish(b.Samples, 0.85f, true);
        }
    }
}
