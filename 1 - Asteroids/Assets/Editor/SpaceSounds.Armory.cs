using System;
using UnityEngine;

namespace Portfolio.Asteroids.EditorTools
{
    /// <summary>
    /// The armory's sounds from the synthesizer: the four new guns of the asteroid field (the strike specials of the same
    /// kind share them), the crackle of lightning jumping between targets, the bursts of a flak shell, a cluster canister
    /// and a napalm canister, and the chime of a repair kit.
    /// </summary>
    internal static partial class SpaceSounds
    {
        /// <summary>The armory's effects, written to Art/Audio/{name}.wav.</summary>
        public static readonly string[] ArmoryEffectNames =
        {
            "ArcCaster", "RailDriver", "FlakBurster", "IonWave", "Lightning", "FlakBurst", "ClusterBurst", "NapalmBurst", "RepairKit"
        };


        /// <summary>The samples of the armory effect <paramref name="name"/>.</summary>
        public static float[] ArmoryEffect(string name)
        {
            noiseState = 40404;
            Buffer b;
            switch (name)
            {
                case "ArcCaster":
                    // A short electric snap: a buzzing saw falling fast over a hiss.
                    b = new Buffer(0.22f);
                    Tone(b, 0f, 0.16f, Wave.Saw, 2200f, 500f, 0.3f, 0.001f, 14f, 0.08f, 55f, 0.02f, 0f, 0f, 6000f);
                    Tone(b, 0f, 0.05f, Wave.Noise, 12000f, 7000f, 0.35f, 0.0005f, 50f);
                    Tone(b, 0.01f, 0.12f, Wave.Square, 900f, 300f, 0.15f, 0.001f, 20f, 0.1f, 70f, 0.02f, 0f, 0f, 3500f);
                    break;
                case "RailDriver":
                    // A rising charge, the crack of the slug and a ringing barrel.
                    b = new Buffer(0.75f);
                    Tone(b, 0f, 0.07f, Wave.Sine, 600f, 2600f, 0.25f, 0.005f, 2f);
                    Tone(b, 0.06f, 0.08f, Wave.Noise, 12000f, 4000f, 0.8f, 0.0005f, 30f);
                    Tone(b, 0.06f, 0.25f, Wave.Sine, 160f, 45f, 0.75f, 0.001f, 9f);
                    Tone(b, 0.06f, 0.6f, Wave.Sine, 2900f, 2850f, 0.12f, 0.001f, 5f, 0.004f, 30f, 0.05f, 1.41f, 0.6f);
                    Sweep(b, 0.06f, 0.4f, 6000f, 900f, 0.35f, 0.005f, 8f);
                    break;
                case "FlakBurster":
                    // A hollow thump of a shell leaving the barrel.
                    b = new Buffer(0.3f);
                    Tone(b, 0f, 0.2f, Wave.Square, 230f, 90f, 0.35f, 0.001f, 16f, 0f, 6f, 0.02f, 0f, 0f, 1600f);
                    Tone(b, 0f, 0.06f, Wave.Noise, 5000f, 1500f, 0.45f, 0.0005f, 35f);
                    Tone(b, 0f, 0.16f, Wave.Sine, 140f, 60f, 0.5f, 0.001f, 18f);
                    break;
                case "IonWave":
                    // A warbling sweep that opens up like the wave itself.
                    b = new Buffer(0.5f);
                    Tone(b, 0f, 0.42f, Wave.Sine, 260f, 880f, 0.4f, 0.01f, 5f, 0.06f, 18f, 0.05f, 2f, 0.5f);
                    Tone(b, 0f, 0.42f, Wave.Triangle, 520f, 1320f, 0.15f, 0.01f, 6f, 0.06f, 18f);
                    Sweep(b, 0f, 0.4f, 600f, 3000f, 0.35f, 0.02f, 5f);
                    break;
                case "Lightning":
                    // Crackle: a burst of tiny bright snaps.
                    b = new Buffer(0.28f);
                    for (int i = 0; i < 9; i++)
                    {
                        float at = i * 0.022f + (NextNoise() + 1f) * 0.006f;
                        Tone(b, at, 0.025f, Wave.Noise, 14000f, 6000f, 0.5f * (1f - i / 11f), 0.0005f, 70f);
                    }
                    Tone(b, 0f, 0.2f, Wave.Saw, 120f, 90f, 0.18f, 0.001f, 10f, 0.2f, 60f, 0.02f, 0f, 0f, 2500f);
                    break;
                case "FlakBurst":
                    b = new Buffer(0.4f);
                    Tone(b, 0f, 0.1f, Wave.Noise, 7000f, 1200f, 0.6f, 0.0005f, 25f);
                    Tone(b, 0f, 0.25f, Wave.Sine, 120f, 50f, 0.5f, 0.001f, 14f);
                    for (int i = 0; i < 4; i++)
                    {
                        Tone(b, 0.03f + i * 0.03f, 0.02f, Wave.Noise, 9000f, 5000f, 0.2f, 0.0005f, 80f);
                    }
                    break;
                case "ClusterBurst":
                    // A rattle of small blasts, one after the other.
                    b = new Buffer(0.9f);
                    for (int i = 0; i < 6; i++)
                    {
                        float at = i * 0.07f + (NextNoise() + 1f) * 0.01f;
                        Boom(b, at, 0.35f, 0.4f, 3200f);
                    }
                    break;
                case "NapalmBurst":
                    // A deep whoomp and the roar of fire catching.
                    b = new Buffer(1.1f);
                    Tone(b, 0f, 0.35f, Wave.Sine, 90f, 35f, 0.8f, 0.004f, 6f);
                    Sweep(b, 0f, 0.6f, 300f, 2400f, 0.6f, 0.03f, 3f);
                    Tone(b, 0.05f, 1f, Wave.Noise, 1400f, 400f, 0.35f, 0.08f, 2.4f);
                    break;
                case "RepairKit":
                    // A bright rising chime with a shimmer.
                    b = new Buffer(0.7f);
                    int[] notes = { 72, 76, 79, 84 };
                    for (int i = 0; i < notes.Length; i++)
                    {
                        Tone(b, i * 0.06f, 0.4f, Wave.Sine, Midi(notes[i]), Midi(notes[i]), 0.25f, 0.002f, 6f, 0f, 6f, 0.03f, 2f, 0.3f);
                    }
                    Tone(b, 0.2f, 0.45f, Wave.Triangle, Midi(91), Midi(96), 0.12f, 0.01f, 5f, 0.01f, 14f);
                    break;
                default:
                    throw new ArgumentException($"No armory sound named {name}.", nameof(name));
            }
            return Finish(b.Samples, 0.85f, false);
        }
    }
}
