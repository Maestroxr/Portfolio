using Gamebox.Editor;
using UnityEngine;
using Wave = Gamebox.Editor.SoundSynth.Wave;
using SynthTrack = Gamebox.Editor.SoundSynth.Track;

namespace Portfolio.DeathRace.EditorTools
{
    /// <summary>
    /// The sounds of the guns and special weapons that came with the workshop: the minigun's round (short and bright, so
    /// sixteen a second blur into a ripping buzz), the flamethrower's roar (a seamless loop), the plasma cannon's shot
    /// and its burst, oil glugging out of a barrel, the EMP's thump and electric whine, the mortar's launch and the
    /// whistle of a falling shell (as long as its flight), and the buzz of a jammed car. Levelled like the rest
    /// (<see cref="Level"/>).
    /// </summary>
    internal static partial class DeathRaceSounds
    {
        /// <summary>Synthesises and saves the weapons' clips (called by <see cref="BuildAll"/>).</summary>
        private static void BuildWeapons()
        {
            Save("Minigun", Minigun(), -13f);
            Save("Flame", Flame(), -14f);
            Save("PlasmaShot", PlasmaShot(), -13f);
            Save("PlasmaBurst", PlasmaBurst(), -12f);
            Save("OilPour", OilPour(), -14f);
            Save("Emp", Emp(), -10f);
            Save("MortarLaunch", MortarLaunch(), -11f);
            Save("MortarWhistle", MortarWhistle(), -15f);
            Save("Jammed", Jammed(), -15f);
        }

        /// <summary>Points the content's weapon sounds at their clips (called by <see cref="Fill"/>).</summary>
        private static void FillWeapons(RaceSounds sounds)
        {
            sounds.minigun = Clip("Minigun");
            sounds.flame = Clip("Flame");
            sounds.plasmaShot = Clip("PlasmaShot");
            sounds.plasmaBurst = Clip("PlasmaBurst");
            sounds.oilPour = Clip("OilPour");
            sounds.emp = Clip("Emp");
            sounds.mortarLaunch = Clip("MortarLaunch");
            sounds.mortarWhistle = Clip("MortarWhistle");
            sounds.jammed = Clip("Jammed");
        }

        /// <summary>One round of the minigun: a tight, bright crack with a small thump, shorter than the machine gun's.</summary>
        private static float[] Minigun()
        {
            var track = new SynthTrack(0.11f, false, 3101);
            SoundSynth.Noise(track, 0f, 0.06f, 1.1f, 1f, 70f, 0.0003f, 0.12f);
            SoundSynth.Tone(track, 0f, 0.05f, Wave.Sine, 260f, 110f, 0.5f, 0.001f, 60f);
            SoundSynth.Noise(track, 0f, 0.07f, 0.35f, 0.2f, 50f);
            SoundSynth.Knock(track, 0.002f, 3100f, 120f, 0.15f, 0.2f);
            SoundSynth.Distort(track, 3f);
            return SoundSynth.Finish(track, 0.8f);
        }

        /// <summary>
        /// The flamethrower's roar, a seamless 1.5 s loop: a low rushing band of noise that breathes (an amplitude flutter of
        /// whole cycles in the loop), a deep rumble under it and the crackle of burning fuel.
        /// </summary>
        private static float[] Flame()
        {
            const float seconds = 1.5f;
            var track = new SynthTrack(seconds, true, 3201);
            float[] samples = track.Samples;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)SoundSynth.SampleRate;
                float breath = 0.75f + 0.15f * Mathf.Sin(Pi2 * (6f / seconds) * t) + 0.1f * Mathf.Sin(Pi2 * (11f / seconds) * t + 1.3f);
                samples[i] = track.NextNoise() * breath;
            }
            SoundSynth.LowPass(track, SoundSynth.Cutoff(1400f));
            SoundSynth.LowPass(track, SoundSynth.Cutoff(2200f));
            var rumble = new SynthTrack(seconds, true, 3202);
            for (int i = 0; i < rumble.Length; i++)
            {
                rumble.Samples[i] = rumble.NextNoise();
            }
            SoundSynth.LowPass(rumble, SoundSynth.Cutoff(160f));
            SoundSynth.LowPass(rumble, SoundSynth.Cutoff(220f));
            SoundSynth.Mix(track, rumble.Samples, 0f, 2.2f);
            for (int k = 0; k < 26; k++)
            {
                SoundSynth.Noise(track, Rand(track) * seconds, 0.012f, 0.25f + 0.25f * Rand(track), 0.9f, 90f, 0.0004f, 0.3f);
            }
            SoundSynth.Distort(track, 1.4f);
            return SoundSynth.Finish(track, 0.7f);
        }

        /// <summary>A plasma shot: a falling electric zap over a hum, with a soft whoosh as the bolt leaves.</summary>
        private static float[] PlasmaShot()
        {
            var track = new SynthTrack(0.42f, false, 3301);
            SoundSynth.Tone(track, 0f, 0.3f, Wave.Saw, 1500f, 240f, 0.45f, 0.002f, 9f, 0.01f, 30f, 0.02f, 1.5f, 0.6f, 4200f);
            SoundSynth.Tone(track, 0f, 0.36f, Wave.Sine, 180f, 90f, 0.5f, 0.003f, 7f);
            SoundSynth.Sweep(track, 0f, 0.3f, 3000f, 700f, 0.4f, 0.005f, 8f);
            SoundSynth.Noise(track, 0f, 0.03f, 0.35f, 0.9f, 80f, 0.0004f, 0.3f);
            SoundSynth.Distort(track, 1.6f);
            return SoundSynth.Finish(track, 0.75f);
        }

        /// <summary>A bolt bursting: a sharp electric crack, a fizz of sparks and a short low thud.</summary>
        private static float[] PlasmaBurst()
        {
            var track = new SynthTrack(0.55f, false, 3401);
            SoundSynth.Noise(track, 0f, 0.05f, 0.8f, 1f, 55f, 0.0003f, 0.2f);
            SoundSynth.Tone(track, 0f, 0.2f, Wave.Square, 900f, 160f, 0.3f, 0.001f, 14f, 0f, 6f, 0.02f, 0f, 0f, 3500f);
            SoundSynth.Tone(track, 0f, 0.25f, Wave.Sine, 120f, 50f, 0.6f, 0.002f, 12f);
            for (int k = 0; k < 10; k++)
            {
                SoundSynth.Noise(track, 0.02f + Rand(track) * 0.3f, 0.015f, 0.15f + 0.1f * Rand(track), 1f, 120f, 0.0003f, 0.5f);
            }
            SoundSynth.Distort(track, 2f);
            return SoundSynth.Finish(track, 0.8f);
        }

        /// <summary>Oil poured from a drum: a hollow clank of the drum, then thick glugs of falling pitch and a slap on the road.</summary>
        private static float[] OilPour()
        {
            var track = new SynthTrack(1f, false, 3501);
            SoundSynth.Knock(track, 0f, 240f, 20f, 0.5f, 0.35f);
            SoundSynth.Knock(track, 0.01f, 610f, 34f, 0.25f, 0.3f);
            for (int k = 0; k < 6; k++)
            {
                float at = 0.08f + k * 0.11f + Rand(track) * 0.03f;
                float pitch = 340f - k * 30f + Rand(track) * 40f;
                SoundSynth.Tone(track, at, 0.09f, Wave.Sine, pitch * 1.6f, pitch, 0.42f - k * 0.04f, 0.004f, 30f);
                SoundSynth.Noise(track, at, 0.05f, 0.12f, 0.12f, 40f, 0.003f);
            }
            SoundSynth.Noise(track, 0.62f, 0.3f, 0.4f, 0.35f, 9f, 0.004f, 0.08f);
            SoundSynth.Tone(track, 0.62f, 0.2f, Wave.Sine, 140f, 70f, 0.4f, 0.003f, 16f);
            return SoundSynth.Finish(track, 0.75f);
        }

        /// <summary>An EMP going off: a heavy thump, a rising electric whine, crackling arcs and the hum dying away.</summary>
        private static float[] Emp()
        {
            var track = new SynthTrack(1.4f, false, 3601);
            SoundSynth.Tone(track, 0f, 0.5f, Wave.Sine, 110f, 32f, 0.9f, 0.002f, 5f);
            SoundSynth.Noise(track, 0f, 0.05f, 0.6f, 1f, 50f, 0.0004f, 0.2f);
            SoundSynth.Tone(track, 0f, 0.6f, Wave.Saw, 300f, 2600f, 0.3f, 0.01f, 3f, 0.02f, 40f, 0.1f, 0f, 0f, 5000f);
            SoundSynth.Tone(track, 0.05f, 1.2f, Wave.Square, 120f, 60f, 0.22f, 0.01f, 2.5f, 0.03f, 13f, 0.3f, 0f, 0f, 900f);
            for (int k = 0; k < 18; k++)
            {
                SoundSynth.Noise(track, 0.04f + Rand(track) * 0.8f, 0.01f + Rand(track) * 0.02f, 0.2f + 0.2f * Rand(track), 1f, 100f, 0.0002f, 0.6f);
            }
            SoundSynth.Echo(track, 0.09f, 0.2f, 2);
            SoundSynth.Distort(track, 1.8f);
            return SoundSynth.Finish(track, 0.9f);
        }

        /// <summary>A mortar firing: a deep hollow pop out of the tube and a short rush of air.</summary>
        private static float[] MortarLaunch()
        {
            var track = new SynthTrack(0.7f, false, 3701);
            SoundSynth.Tone(track, 0f, 0.35f, Wave.Sine, 130f, 55f, 0.9f, 0.001f, 9f);
            SoundSynth.Knock(track, 0f, 420f, 28f, 0.45f, 0.5f);
            SoundSynth.Noise(track, 0f, 0.04f, 0.7f, 0.8f, 60f, 0.0004f, 0.15f);
            SoundSynth.Sweep(track, 0.02f, 0.5f, 900f, 2400f, 0.35f, 0.01f, 6f);
            SoundSynth.Distort(track, 2f);
            return SoundSynth.Finish(track, 0.85f);
        }

        /// <summary>A shell coming down: a thin whistle falling in pitch over its flight (1.1 s), louder as it nears the ground.</summary>
        private static float[] MortarWhistle()
        {
            float seconds = CombatRules.MortarFlightSeconds;
            var track = new SynthTrack(seconds, false, 3801);
            int count = SoundSynth.Index(seconds);
            float phase = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SoundSynth.SampleRate;
                float progress = t / seconds;
                float frequency = Mathf.Lerp(2600f, 900f, progress * progress) * (1f + 0.01f * Mathf.Sin(Pi2 * 9f * t));
                phase += frequency / SoundSynth.SampleRate;
                float swell = Mathf.Lerp(0.15f, 1f, progress * progress) * Mathf.Clamp01((seconds - t) / 0.03f);
                track.Add(i, Mathf.Sin(Pi2 * phase) * swell * 0.5f + Mathf.Sin(Pi2 * phase * 2.01f) * swell * 0.08f);
            }
            SoundSynth.Noise(track, 0f, seconds, 0.08f, 0.3f, 0f, 0.3f, 0.2f);
            return SoundSynth.Finish(track, 0.6f);
        }

        /// <summary>A car jammed: its electrics buzz and stutter, the engine coughing twice.</summary>
        private static float[] Jammed()
        {
            var track = new SynthTrack(1.2f, false, 3901);
            SoundSynth.Tone(track, 0f, 1.1f, Wave.Square, 100f, 92f, 0.3f, 0.01f, 1.5f, 0.06f, 23f, 0.2f, 0f, 0f, 1800f);
            SoundSynth.Tone(track, 0f, 1.1f, Wave.Saw, 151f, 139f, 0.18f, 0.01f, 1.5f, 0.05f, 17f, 0.2f, 0f, 0f, 2400f);
            for (int k = 0; k < 14; k++)
            {
                SoundSynth.Noise(track, Rand(track) * 1f, 0.02f, 0.2f + 0.15f * Rand(track), 1f, 90f, 0.0003f, 0.5f);
            }
            SoundSynth.Knock(track, 0.3f, 90f, 14f, 0.45f, 0.6f);
            SoundSynth.Knock(track, 0.62f, 80f, 14f, 0.4f, 0.6f);
            SoundSynth.Distort(track, 2.4f);
            return SoundSynth.Finish(track, 0.6f);
        }
    }
}
