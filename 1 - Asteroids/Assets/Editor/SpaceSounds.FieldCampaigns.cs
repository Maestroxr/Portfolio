using UnityEngine;

namespace Portfolio.Asteroids.EditorTools
{
    /// <summary>
    /// The sounds of the field campaigns from the synthesizer (SpaceSounds.FieldCampaigns.cs): the roar of a solar flare,
    /// the crackle of an ion storm and its lightning, the charge and fire of the lasers, the sentry's charge, a mine
    /// dropped, a stalker's or a boss's blink, a shot glancing off iron, a hydra rock growing back, the alarm of an ally
    /// under fire and a freighter's jump, the burst of a nova orb; and two battle loops, one for each campaign.
    /// </summary>
    internal static partial class SpaceSounds
    {
        /// <summary>The effects of the field campaigns, written to Art/Field/Audio/{name}.wav.</summary>
        public static readonly string[] FieldEffectNames =
        {
            "FlareRoar", "StormCrackle", "LightningStrike", "LanceCharge", "LanceFire", "SentryCharge", "MineDrop", "Blink",
            "MetalGlance", "HydraRegrow", "AllyAlarm", "FreighterJump", "OrbBurst"
        };

        /// <summary>Whether a field campaign effect is a seamless loop (the storm's crackle).</summary>
        public static bool IsFieldLoop(string name)
        {
            return name == "StormCrackle";
        }


        /// <summary>The samples of the field campaign effect <paramref name="name"/>.</summary>
        public static float[] FieldEffect(string name)
        {
            noiseState = 40404;
            Buffer b;
            switch (name)
            {
                case "FlareRoar":
                    b = new Buffer(1.8f);
                    Sweep(b, 0f, 1.6f, 300f, 2400f, 0.8f, 0.15f, 1.6f);
                    Tone(b, 0f, 1.5f, Wave.Noise, 1200f, 250f, 0.55f, 0.2f, 1.8f);
                    Tone(b, 0f, 1.2f, Wave.Saw, 70f, 50f, 0.35f, 0.15f, 2f, 0.08f, 9f, 0.1f, 0f, 0f, 600f);
                    break;
                case "StormCrackle":
                {
                    // Two seconds of a low electric hum with crackles; every partial completes whole cycles.
                    b = new Buffer(2f, true);
                    Tone(b, 0f, 2f, Wave.Saw, 60f, 60f, 0.12f, 0.0001f, 0f, 0f, 6f, 0f, 0f, 0f, 500f);
                    for (int i = 0; i < 26; i++)
                    {
                        float at = (NextNoise() + 1f) * 0.98f;
                        Tone(b, at, 0.03f, Wave.Noise, 9000f, 4000f, 0.22f + (NextNoise() + 1f) * 0.1f, 0.0005f, 70f);
                    }
                    break;
                }
                case "LightningStrike":
                    b = new Buffer(1.1f);
                    Tone(b, 0f, 0.05f, Wave.Noise, 12000f, 8000f, 0.9f, 0.0005f, 30f);
                    Boom(b, 0.02f, 0.9f, 0.7f, 5000f);
                    for (int i = 0; i < 6; i++)
                    {
                        Tone(b, 0.01f + i * 0.025f, 0.02f, Wave.Noise, 10000f, 6000f, 0.4f, 0.0005f, 80f);
                    }
                    break;
                case "LanceCharge":
                    b = new Buffer(0.8f);
                    Tone(b, 0f, 0.75f, Wave.Sine, 300f, 1800f, 0.3f, 0.05f, 0.6f, 0.02f, 18f, 0.05f, 2f, 0.4f);
                    Tone(b, 0f, 0.75f, Wave.Saw, 150f, 900f, 0.12f, 0.05f, 0.6f, 0f, 6f, 0.05f, 0f, 0f, 2500f);
                    break;
                case "LanceFire":
                    b = new Buffer(0.9f);
                    Tone(b, 0f, 0.8f, Wave.Saw, 220f, 180f, 0.4f, 0.004f, 2f, 0.01f, 40f, 0.05f, 0f, 0f, 3000f);
                    Tone(b, 0f, 0.8f, Wave.Square, 110f, 90f, 0.25f, 0.004f, 2f, 0.01f, 33f, 0.05f, 0f, 0f, 1500f);
                    Tone(b, 0f, 0.1f, Wave.Noise, 9000f, 3000f, 0.5f, 0.001f, 25f);
                    break;
                case "SentryCharge":
                    b = new Buffer(0.6f);
                    Tone(b, 0f, 0.55f, Wave.Square, 400f, 1400f, 0.18f, 0.01f, 1.2f, 0f, 6f, 0.05f, 0f, 0f, 3000f);
                    Tone(b, 0f, 0.55f, Wave.Sine, 800f, 2800f, 0.14f, 0.01f, 1.2f);
                    break;
                case "MineDrop":
                    b = new Buffer(0.4f);
                    Tone(b, 0f, 0.12f, Wave.Square, 160f, 90f, 0.35f, 0.001f, 18f, 0f, 6f, 0.01f, 0f, 0f, 1500f);
                    Tone(b, 0.08f, 0.25f, Wave.Sine, 900f, 600f, 0.2f, 0.002f, 9f, 0f, 6f, 0.02f, 2f, 0.3f);
                    break;
                case "Blink":
                    b = new Buffer(0.7f);
                    Tone(b, 0f, 0.6f, Wave.Sine, 1600f, 200f, 0.35f, 0.005f, 3f, 0.03f, 24f, 0.05f, 1.5f, 0.6f);
                    Sweep(b, 0f, 0.5f, 6000f, 800f, 0.3f, 0.01f, 5f);
                    Echo(b, 0.07f, 0.35f, 3);
                    break;
                case "MetalGlance":
                    b = new Buffer(0.5f);
                    Tone(b, 0f, 0.45f, Wave.Sine, 2100f, 2050f, 0.3f, 0.001f, 9f, 0f, 6f, 0.02f, 2.76f, 0.5f);
                    Tone(b, 0f, 0.35f, Wave.Sine, 3300f, 3250f, 0.15f, 0.001f, 12f, 0f, 6f, 0.02f, 1.5f, 0.3f);
                    Tone(b, 0f, 0.03f, Wave.Noise, 9000f, 6000f, 0.3f, 0.0005f, 60f);
                    break;
                case "HydraRegrow":
                    b = new Buffer(0.8f);
                    Tone(b, 0f, 0.7f, Wave.Saw, 70f, 140f, 0.3f, 0.05f, 2f, 0.1f, 7f, 0.05f, 0.5f, 1.5f, 700f);
                    Tone(b, 0f, 0.6f, Wave.Noise, 600f, 1500f, 0.25f, 0.05f, 3f);
                    break;
                case "AllyAlarm":
                    b = new Buffer(0.7f);
                    for (int i = 0; i < 2; i++)
                    {
                        Tone(b, i * 0.3f, 0.22f, Wave.Triangle, 1100f, 750f, 0.3f, 0.005f, 2f, 0f, 6f, 0.03f, 0f, 0f, 4000f);
                    }
                    break;
                case "FreighterJump":
                    b = new Buffer(1.2f);
                    Tone(b, 0f, 0.9f, Wave.Sine, 120f, 2400f, 0.35f, 0.05f, 1.5f, 0f, 6f, 0.1f, 2f, 0.5f);
                    Sweep(b, 0.2f, 0.8f, 800f, 7000f, 0.35f, 0.05f, 3f);
                    Echo(b, 0.11f, 0.3f, 2);
                    break;
                case "OrbBurst":
                    b = new Buffer(0.8f);
                    Boom(b, 0f, 0.6f, 0.6f, 3500f);
                    Tone(b, 0f, 0.4f, Wave.Sine, 900f, 200f, 0.3f, 0.002f, 6f, 0f, 6f, 0.03f, 1.5f, 0.6f);
                    break;
                default:
                    b = new Buffer(0.1f);
                    break;
            }
            return Finish(b.Samples, 0.9f, b.Loop);
        }


        /// <summary>
        /// The battle loop of the second campaign, NOVA FRONTIER: 16 bars at 132 BPM in E minor (Em - C - D - Bm), a
        /// galloping bass, a bright arpeggio and a soaring lead over it.
        /// </summary>
        public static float[] FrontierMusic()
        {
            noiseState = 13579;
            const float bpm = 132f;
            const int bars = 16;
            float beat = 60f / bpm;
            float sixteenth = beat / 4f;
            var b = new Buffer(bars * 4 * beat, true);
            int[] roots = { 40, 36, 38, 35 };
            int[][] chords = { new[] { 55, 59, 64 }, new[] { 55, 60, 64 }, new[] { 54, 57, 62 }, new[] { 54, 59, 62 } };
            int[][] melody =
            {
                new[] { 79, 0, 78, 76, 0, 74, 76, 0 }, new[] { 72, 0, 74, 76, 79, 0, 76, 0 },
                new[] { 78, 0, 76, 74, 0, 69, 74, 0 }, new[] { 71, 74, 78, 0, 83, 0, 81, 78 }
            };
            for (int bar = 0; bar < bars; bar++)
            {
                int section = bar % 4;
                bool intro = bar < 4;
                bool lift = bar >= 8 && bar < 12;
                float barStart = bar * 4 * beat;
                for (int s = 0; s < 16; s++)
                {
                    float at = barStart + s * sixteenth;
                    // A galloping bass: two sixteenths and an eighth.
                    if (s % 4 != 3)
                    {
                        int bass = roots[section] - 12 + (s % 4 == 2 ? 12 : 0);
                        Tone(b, at, sixteenth * 0.85f, Wave.Saw, Midi(bass), Midi(bass), 0.2f, 0.003f, 7f, 0f, 6f, 0.01f, 0f, 0f, 650f);
                    }
                    if (s % 4 == 0)
                    {
                        Kick(b, at, 0.62f);
                    }
                    if (!intro && (s == 4 || s == 12))
                    {
                        Snare(b, at, 0.3f);
                    }
                    Hat(b, at, s % 2 == 1 ? 0.07f : 0.035f);
                    if (!intro)
                    {
                        int arp = chords[section][s % 3] + 24;
                        Tone(b, at, sixteenth * 1.2f, Wave.Pulse, Midi(arp), Midi(arp), 0.04f, 0.002f, 10f, 0f, 6f, 0.01f, 0f, 0f, 4500f);
                    }
                }
                foreach (int note in chords[section])
                {
                    Tone(b, barStart, 4 * beat, Wave.Saw, Midi(note), Midi(note), 0.032f, 0.2f, 0.35f, 0.004f, 0.45f, 0.1f, 0f, 0f, 1400f);
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
                        Tone(b, barStart + n * beat / 2f, beat * 0.46f, Wave.Saw, Midi(note), Midi(note), 0.06f, 0.006f, 2.5f, 0.008f, 5.5f, 0.02f, 0f, 0f, 3200f);
                    }
                }
            }
            Echo(b, beat * 0.75f, 0.22f, 2);
            return Finish(b.Samples, 0.85f, true);
        }


        /// <summary>
        /// The battle loop of the third campaign, THE LAST GATE: 16 bars at 112 BPM in C# phrygian, a slow pulse under dark
        /// pads, a ticking hat and a bell-like lead that sounds like the gate calling.
        /// </summary>
        public static float[] GateMusic()
        {
            noiseState = 24680;
            const float bpm = 112f;
            const int bars = 16;
            float beat = 60f / bpm;
            float sixteenth = beat / 4f;
            var b = new Buffer(bars * 4 * beat, true);
            int[] roots = { 37, 38, 37, 33 };
            int[][] chords = { new[] { 56, 61, 64 }, new[] { 57, 62, 65 }, new[] { 56, 61, 64 }, new[] { 57, 61, 64 } };
            int[][] bells =
            {
                new[] { 73, 0, 0, 76, 0, 74, 73, 0 }, new[] { 74, 0, 0, 77, 0, 76, 74, 0 },
                new[] { 73, 0, 80, 0, 76, 0, 73, 0 }, new[] { 69, 0, 73, 0, 76, 0, 74, 73 }
            };
            for (int bar = 0; bar < bars; bar++)
            {
                int section = bar % 4;
                bool intro = bar < 4;
                float barStart = bar * 4 * beat;
                for (int s = 0; s < 16; s++)
                {
                    float at = barStart + s * sixteenth;
                    if (s % 2 == 0)
                    {
                        int bass = roots[section] - 12;
                        Tone(b, at, sixteenth * 1.6f, Wave.Square, Midi(bass), Midi(bass), 0.16f, 0.004f, 5f, 0f, 6f, 0.01f, 0f, 0f, 420f);
                    }
                    if (s == 0 || s == 6 || s == 10)
                    {
                        Kick(b, at, 0.6f);
                    }
                    if (!intro && s == 8)
                    {
                        Snare(b, at, 0.32f);
                    }
                    Hat(b, at, s % 4 == 2 ? 0.06f : 0.025f, 0.03f);
                }
                foreach (int note in chords[section])
                {
                    Tone(b, barStart, 4 * beat + 0.5f, Wave.Saw, Midi(note), Midi(note), 0.04f, 0.6f, 0.25f, 0.004f, 0.3f, 0.5f, 0f, 0f, 900f);
                    Tone(b, barStart, 4 * beat + 0.5f, Wave.Saw, Midi(note) * 1.007f, Midi(note) * 1.007f, 0.04f, 0.6f, 0.25f, 0.004f, 0.37f, 0.5f, 0f, 0f, 900f);
                }
                if (!intro)
                {
                    int[] line = bells[section];
                    for (int n = 0; n < line.Length; n++)
                    {
                        if (line[n] == 0)
                        {
                            continue;
                        }
                        Tone(b, barStart + n * beat / 2f, beat * 1.4f, Wave.Sine, Midi(line[n]), Midi(line[n]), 0.11f, 0.003f, 2f, 0f, 6f, 0.05f, 3.01f, 0.35f);
                    }
                }
            }
            Echo(b, beat * 0.75f, 0.33f, 3);
            return Finish(b.Samples, 0.85f, true);
        }
    }
}
