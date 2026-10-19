using System;
using Gamebox.Editor;
using UnityEngine;
using Wave = Gamebox.Editor.SoundSynth.Wave;
using SynthTrack = Gamebox.Editor.SoundSynth.Track;

namespace Portfolio.DeathRace.EditorTools
{
    /// <summary>
    /// Every sound and both pieces of music of the game, synthesised with BaseGame's <see cref="SoundSynth"/> (seeded, so
    /// a rebuild writes the same WAV files) into <c>Audio/Generated</c>: one clip per field of <see cref="RaceSounds"/>.
    ///
    /// The engine is a seamless two-second loop of exhaust pulses (an eight-cylinder burble at 56 firings a second,
    /// resonant bursts, a mechanical whine and intake noise, a little grit) that the car's audio pitches from 0.6x (idle)
    /// to 2x (flat out). Levels: every clip is mastered to a peak that suits its kind, loud (explosions) to quiet (the
    /// interface), so the mix holds together without per-clip volumes in code.
    /// </summary>
    internal static partial class DeathRaceSounds
    {
        public const string Folder = "Audio/Generated";

        private static float Pi2 => Mathf.PI * 2f;

        /// <summary>The clip of a sound by its file name (e.g. "Engine"), or null before it was built.</summary>
        public static AudioClip Clip(string name)
        {
            return DeathRaceAssets.Load<AudioClip>($"{Folder}/{name}.wav");
        }

        /// <summary>Synthesises and saves every clip (only changed files are written).</summary>
        public static void BuildAll()
        {
            Save("Engine", Engine(), -15f);
            Save("Skid", Skid(), -17f);
            Save("Gun", Gun(), -12f);
            Save("Ricochet", Ricochet(), -17f);
            Save("Impact", Impact(), -10f);
            Save("Scrape", Scrape(), -15f);
            Save("Explosion", Explosion(), -7f);
            Save("BigExplosion", BigExplosion(), -6f);
            Save("MineDrop", MineDrop(), -14f);
            Save("MineArm", MineArm(), -18f);
            Save("RocketLaunch", RocketLaunch(), -12f);
            Save("Nitro", Nitro(), -12f);
            Save("PickupCash", PickupCash(), -13f);
            Save("PickupRepair", PickupRepair(), -13f);
            Save("PickupAmmo", PickupAmmo(), -13f);
            Save("PickupNitro", PickupNitro(), -13f);
            Save("Countdown", Countdown(), -13f);
            Save("Go", Go(), -11f);
            Save("Lap", Lap(), -13f);
            Save("Finish", FinishFanfare(), -11f);
            Save("WreckJingle", WreckJingle(), -12f);
            Save("Victory", Victory(), -11f);
            Save("Buy", Buy(), -15f);
            Save("Sell", Sell(), -15f);
            Save("Deny", Deny(), -16f);
            Save("Click", Click(), -20f);
            Save("MenuMusic", MenuMusic(), -13f, true);
            Save("RaceMusic", RaceMusic(), -13f, true);
        }

        /// <summary>Points the content's sounds at the clips.</summary>
        public static void Fill(RaceSounds sounds)
        {
            sounds.engine = Clip("Engine");
            sounds.skid = Clip("Skid");
            sounds.gun = Clip("Gun");
            sounds.ricochet = Clip("Ricochet");
            sounds.impact = Clip("Impact");
            sounds.scrape = Clip("Scrape");
            sounds.explosion = Clip("Explosion");
            sounds.bigExplosion = Clip("BigExplosion");
            sounds.mineDrop = Clip("MineDrop");
            sounds.mineArm = Clip("MineArm");
            sounds.rocketLaunch = Clip("RocketLaunch");
            sounds.nitro = Clip("Nitro");
            sounds.pickupCash = Clip("PickupCash");
            sounds.pickupRepair = Clip("PickupRepair");
            sounds.pickupAmmo = Clip("PickupAmmo");
            sounds.pickupNitro = Clip("PickupNitro");
            sounds.countdown = Clip("Countdown");
            sounds.go = Clip("Go");
            sounds.lap = Clip("Lap");
            sounds.finish = Clip("Finish");
            sounds.wreckJingle = Clip("WreckJingle");
            sounds.victory = Clip("Victory");
            sounds.buy = Clip("Buy");
            sounds.sell = Clip("Sell");
            sounds.deny = Clip("Deny");
            sounds.click = Clip("Click");
            sounds.menuMusic = Clip("MenuMusic");
            sounds.raceMusic = Clip("RaceMusic");
        }

        /// <summary>Levels a clip to <paramref name="loudness"/> and writes it.</summary>
        private static void Save(string name, float[] samples, float loudness, bool music = false)
        {
            DeathRaceAssets.SaveAudio(SoundSynth.Wav(Level(samples, loudness)), $"{Folder}/{name}.wav", music);
        }

        /// <summary>
        /// Scales a mastered clip so its loudest 400 ms sit at <paramref name="loudness"/> dB (RMS, full scale = 0). The
        /// peaks of a clip that has to come up a lot (a short clank) are rounded off by a soft limiter under
        /// <paramref name="ceiling"/> instead of clipping. Pointwise, so a seamless loop stays seamless. The targets keep
        /// the game's sounds in proportion: explosions loudest, the gun and hits a step down, pickups and cues below,
        /// the engine and skid (which play all the time) further down, the interface quietest.
        /// </summary>
        public static float[] Level(float[] samples, float loudness, float ceiling = 0.97f)
        {
            int window = Mathf.Min(samples.Length, SoundSynth.Index(0.4f));
            if (window <= 0)
            {
                return samples;
            }
            double sum = 0.0;
            for (int i = 0; i < window; i++)
            {
                sum += samples[i] * samples[i];
            }
            double best = sum;
            for (int i = window; i < samples.Length; i++)
            {
                sum += samples[i] * samples[i] - samples[i - window] * samples[i - window];
                best = Math.Max(best, sum);
            }
            float rms = (float)Math.Sqrt(Math.Max(best, 0.0) / window);
            if (rms < 1e-6f)
            {
                return samples;
            }
            float gain = Mathf.Pow(10f, loudness / 20f) / rms;
            float peak = SoundSynth.Peak(samples) * gain;
            for (int i = 0; i < samples.Length; i++)
            {
                float x = samples[i] * gain;
                samples[i] = peak > ceiling ? ceiling * (float)Math.Tanh(x / ceiling) : x;
            }
            return samples;
        }

        private static float Rand(SynthTrack track)
        {
            return (float)track.Random.NextDouble();
        }

        // ------------------------------------------------------------------ the car

        /// <summary>
        /// The engine: a seamless 2 s loop. 112 exhaust pulses (56 a second, a multiple of the eight cylinders, so the
        /// burble of the uneven firing repeats exactly), each a resonant burst that dies before the next; a whine at the
        /// fourth and seventh harmonics; intake noise breathing with the pulses; a sub rumble at half the firing rate.
        /// Every periodic part fits the loop a whole number of times, so the seam cannot be heard at any pitch.
        /// </summary>
        private static float[] Engine()
        {
            const float seconds = 2f;
            const float firing = 56f;
            var track = new SynthTrack(seconds, true, 101);
            int pulses = Mathf.RoundToInt(seconds * firing);
            float[] cylinders = { 1f, 0.8f, 0.96f, 0.68f, 1.06f, 0.76f, 0.9f, 0.72f };
            var breath = new float[track.Length];
            int length = SoundSynth.Index(3.2f / firing);
            for (int n = 0; n < pulses; n++)
            {
                int first = SoundSynth.Index(n / firing);
                float level = cylinders[n % cylinders.Length] * (0.9f + 0.2f * Rand(track));
                float resonance = 112f + 18f * Rand(track);
                for (int i = 0; i < length; i++)
                {
                    float t = i / (float)SoundSynth.SampleRate;
                    float shape = (1f - Mathf.Exp(-t * 1600f)) * Mathf.Exp(-t * 48f);
                    float value = Mathf.Sin(Pi2 * resonance * t) * 0.7f
                        + Mathf.Sin(Pi2 * resonance * 2.03f * t) * 0.22f
                        + Mathf.Sin(Pi2 * resonance * 0.5f * t) * 0.4f;
                    track.Add(first + i, value * shape * level);
                    breath[(first + i) % breath.Length] += shape * level;
                }
            }
            float[] samples = track.Samples;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)SoundSynth.SampleRate;
                samples[i] += 0.2f * Mathf.Sin(Pi2 * firing * 0.5f * t)
                    + 0.045f * Mathf.Sin(Pi2 * firing * 4f * t)
                    + 0.028f * Mathf.Sin(Pi2 * firing * 7f * t);
            }
            var air = new SynthTrack(seconds, true, 102);
            for (int i = 0; i < air.Length; i++)
            {
                air.Samples[i] = air.NextNoise() * (0.25f + breath[i] * 0.85f);
            }
            SoundSynth.LowPass(air, SoundSynth.Cutoff(650f));
            SoundSynth.LowPass(air, SoundSynth.Cutoff(1300f));
            SoundSynth.Mix(track, air.Samples, 0f, 0.55f);
            SoundSynth.Distort(track, 1.5f);
            SoundSynth.LowPass(track, SoundSynth.Cutoff(3200f));
            return SoundSynth.Finish(track, 0.7f);
        }

        /// <summary>
        /// A tyre squeal, a seamless 1.2 s loop: three partials (whole numbers of cycles in the loop) wobbling on a slow
        /// vibrato and tremolo, over a band of hiss.
        /// </summary>
        private static float[] Skid()
        {
            const float seconds = 1.2f;
            var track = new SynthTrack(seconds, true, 201);
            float[] frequencies = { 1150f, 1480f, 1900f };
            float[] levels = { 0.5f, 0.32f, 0.18f };
            float rate = 5f / seconds;
            float tremolo = 3f / seconds;
            float[] samples = track.Samples;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)SoundSynth.SampleRate;
                float value = 0f;
                for (int k = 0; k < frequencies.Length; k++)
                {
                    float depth = frequencies[k] * 0.025f;
                    float phase = frequencies[k] * t + depth / (Pi2 * rate) * Mathf.Sin(Pi2 * rate * t + k);
                    float amplitude = 0.7f + 0.3f * Mathf.Sin(Pi2 * tremolo * t + k * 1.7f);
                    value += Mathf.Sin(Pi2 * phase) * levels[k] * amplitude;
                }
                samples[i] = value;
            }
            var hiss = new SynthTrack(seconds, true, 202);
            for (int i = 0; i < hiss.Length; i++)
            {
                hiss.Samples[i] = hiss.NextNoise();
            }
            SoundSynth.HighPass(hiss, SoundSynth.Cutoff(900f));
            SoundSynth.LowPass(hiss, SoundSynth.Cutoff(3600f));
            SoundSynth.Mix(track, hiss.Samples, 0f, 0.22f);
            SoundSynth.Distort(track, 1.3f);
            return SoundSynth.Finish(track, 0.55f);
        }

        // ------------------------------------------------------------------ weapons and hits

        /// <summary>One round of the machine gun: a sharp crack, a thump, a short body of noise and the click of the action.</summary>
        private static float[] Gun()
        {
            var track = new SynthTrack(0.2f, false, 301);
            SoundSynth.Noise(track, 0f, 0.1f, 1.25f, 0.9f, 46f, 0.0004f, 0.08f);
            SoundSynth.Noise(track, 0f, 0.12f, 0.7f, 0.3f, 30f, 0.0006f);
            SoundSynth.Tone(track, 0f, 0.09f, Wave.Sine, 170f, 60f, 0.7f, 0.001f, 48f);
            SoundSynth.Noise(track, 0f, 0.09f, 0.45f, 0.12f, 36f);
            SoundSynth.Knock(track, 0.003f, 2300f, 90f, 0.22f, 0.2f);
            SoundSynth.Distort(track, 3f);
            return SoundSynth.Finish(track, 0.8f);
        }

        /// <summary>A bullet glancing off metal: a tick and a falling, warbling whine.</summary>
        private static float[] Ricochet()
        {
            var track = new SynthTrack(0.48f, false, 401);
            SoundSynth.Noise(track, 0f, 0.02f, 0.5f, 1f, 80f, 0.0005f, 0.3f);
            SoundSynth.Tone(track, 0.005f, 0.42f, Wave.Sine, 4200f, 1850f, 0.6f, 0.002f, 6f, 0.012f, 38f);
            SoundSynth.Tone(track, 0.005f, 0.3f, Wave.Triangle, 6100f, 3200f, 0.14f, 0.002f, 10f);
            SoundSynth.Echo(track, 0.05f, 0.2f, 2);
            return SoundSynth.Finish(track, 0.5f);
        }

        /// <summary>A crash: a short boom, the clang of panels at three pitches, a ringing metal partial and a crunch.</summary>
        private static float[] Impact()
        {
            var track = new SynthTrack(0.7f, false, 501);
            SoundSynth.Boom(track, 0f, 0.35f, 0.8f, 2200f);
            SoundSynth.Knock(track, 0f, 310f, 16f, 0.6f, 0.65f);
            SoundSynth.Knock(track, 0.01f, 1270f, 22f, 0.3f, 0.3f);
            SoundSynth.Knock(track, 0.03f, 2150f, 30f, 0.22f, 0.2f);
            SoundSynth.Tone(track, 0f, 0.5f, Wave.Sine, 520f, 500f, 0.15f, 0.001f, 9f, 0f, 6f, 0.02f, 2.76f, 1.2f);
            for (int k = 0; k < 8; k++)
            {
                SoundSynth.Noise(track, 0.02f + k * 0.03f * Rand(track) * 1.5f, 0.03f, 0.25f, 0.6f, 60f, 0.0005f, 0.2f);
            }
            SoundSynth.Distort(track, 2.5f);
            return SoundSynth.Finish(track, 0.85f);
        }

        /// <summary>Metal grinding along a barrier: a jittery band of noise and two screeching partials.</summary>
        private static float[] Scrape()
        {
            var track = new SynthTrack(0.65f, false, 601);
            for (int k = 0; k < 50; k++)
            {
                SoundSynth.Noise(track, k * 0.012f, 0.03f, 0.35f * (0.5f + 0.5f * Rand(track)), 0.55f, 6f, 0.004f, 0.12f);
            }
            SoundSynth.Tone(track, 0f, 0.6f, Wave.Saw, 1320f, 1180f, 0.12f, 0.02f, 2.5f, 0.02f, 23f, 0.08f, 0f, 0f, 3000f);
            SoundSynth.Tone(track, 0f, 0.55f, Wave.Saw, 2110f, 1990f, 0.07f, 0.03f, 3f, 0.015f, 17f, 0.08f, 0f, 0f, 4000f);
            SoundSynth.Distort(track, 1.8f);
            return SoundSynth.Finish(track, 0.6f);
        }

        /// <summary>A blast: a deep boom with its crackle, a sub drop, a first crack, debris pattering down, a short echo.</summary>
        private static float[] Explosion()
        {
            var track = new SynthTrack(2.1f, false, 701);
            SoundSynth.Boom(track, 0f, 1.6f, 1f, 5200f);
            SoundSynth.Tone(track, 0f, 0.9f, Wave.Sine, 75f, 28f, 0.7f, 0.002f, 3.5f);
            SoundSynth.Noise(track, 0f, 0.06f, 0.6f, 1f, 50f, 0.0005f, 0.15f);
            for (int k = 0; k < 10; k++)
            {
                SoundSynth.Knock(track, 0.25f + Rand(track) * 0.9f, 900f + Rand(track) * 2400f, 40f + Rand(track) * 30f, 0.05f + 0.06f * Rand(track), 0.6f);
            }
            SoundSynth.Echo(track, 0.13f, 0.22f, 3);
            return SoundSynth.Finish(track, 0.95f);
        }

        /// <summary>A car going up: a double boom, a deeper sub, a fiercer crack, then metal clattering down for seconds.</summary>
        private static float[] BigExplosion()
        {
            var track = new SynthTrack(3.3f, false, 801);
            SoundSynth.Boom(track, 0f, 2.4f, 1f, 6000f);
            SoundSynth.Boom(track, 0.14f, 1.8f, 0.75f, 3500f);
            SoundSynth.Tone(track, 0f, 1.4f, Wave.Sine, 62f, 24f, 0.85f, 0.002f, 2.4f);
            SoundSynth.Noise(track, 0f, 0.08f, 0.75f, 1f, 40f, 0.0005f, 0.12f);
            for (int k = 0; k < 16; k++)
            {
                SoundSynth.Knock(track, 0.2f + Rand(track) * 1.2f, 800f + Rand(track) * 2600f, 35f + Rand(track) * 35f, 0.05f + 0.07f * Rand(track), 0.55f);
            }
            for (int k = 0; k < 6; k++)
            {
                SoundSynth.Knock(track, 0.8f + Rand(track) * 1.5f, 220f + Rand(track) * 420f, 18f + Rand(track) * 12f, 0.12f + 0.1f * Rand(track), 0.5f);
            }
            SoundSynth.Echo(track, 0.17f, 0.25f, 3);
            return SoundSynth.Finish(track, 0.98f);
        }

        /// <summary>A mine hitting the road: a heavy metal clunk, a thud and a small bounce.</summary>
        private static float[] MineDrop()
        {
            var track = new SynthTrack(0.42f, false, 901);
            SoundSynth.Knock(track, 0f, 380f, 26f, 0.7f, 0.55f);
            SoundSynth.Knock(track, 0.005f, 1650f, 45f, 0.3f, 0.25f);
            SoundSynth.Tone(track, 0f, 0.15f, Wave.Sine, 120f, 70f, 0.5f, 0.001f, 25f);
            SoundSynth.Knock(track, 0.11f, 520f, 40f, 0.25f, 0.5f);
            return SoundSynth.Finish(track, 0.7f);
        }

        /// <summary>A mine arming: two short rising electronic beeps.</summary>
        private static float[] MineArm()
        {
            var track = new SynthTrack(0.3f, false, 1001);
            SoundSynth.Tone(track, 0f, 0.07f, Wave.Square, 1760f, 1760f, 0.4f, 0.002f, 0f, 0f, 6f, 0.01f, 0f, 0f, 5000f);
            SoundSynth.Tone(track, 0.12f, 0.1f, Wave.Square, 2349f, 2349f, 0.45f, 0.002f, 0f, 0f, 6f, 0.015f, 0f, 0f, 6000f);
            return SoundSynth.Finish(track, 0.5f);
        }

        /// <summary>A rocket leaving its tube: ignition crack and thump, a rising whoosh, the roar of the motor fading away.</summary>
        private static float[] RocketLaunch()
        {
            var track = new SynthTrack(1.2f, false, 1101);
            SoundSynth.Noise(track, 0f, 0.05f, 0.7f, 1f, 40f, 0.0005f, 0.1f);
            SoundSynth.Tone(track, 0f, 0.25f, Wave.Sine, 160f, 60f, 0.5f, 0.001f, 10f);
            SoundSynth.Sweep(track, 0.02f, 1.1f, 350f, 2600f, 0.9f, 0.04f, 2.2f);
            SoundSynth.Noise(track, 0.02f, 1.1f, 0.45f, 0.12f, 2.5f, 0.05f);
            return SoundSynth.Finish(track, 0.8f);
        }

        /// <summary>Nitro lighting: a whump, a hiss of gas, and a roar that climbs in pitch.</summary>
        private static float[] Nitro()
        {
            var track = new SynthTrack(1.4f, false, 1201);
            SoundSynth.Tone(track, 0f, 0.3f, Wave.Sine, 90f, 45f, 0.6f, 0.002f, 9f);
            SoundSynth.Sweep(track, 0f, 0.35f, 600f, 4200f, 0.7f, 0.01f, 6f);
            SoundSynth.Noise(track, 0.05f, 1.3f, 0.55f, 0.18f, 1.8f, 0.08f);
            SoundSynth.Tone(track, 0.05f, 1.3f, Wave.Saw, 85f, 170f, 0.25f, 0.1f, 1.5f, 0.02f, 9f, 0.3f, 0f, 0f, 900f);
            SoundSynth.Distort(track, 1.6f);
            return SoundSynth.Finish(track, 0.75f);
        }

        // ------------------------------------------------------------------ pickups

        /// <summary>A cash register's cha-ching: the lever and drawer, a two-note bell, coins.</summary>
        private static float[] PickupCash()
        {
            var track = new SynthTrack(0.9f, false, 1301);
            SoundSynth.Noise(track, 0f, 0.06f, 0.5f, 0.6f, 45f, 0.001f, 0.2f);
            SoundSynth.Knock(track, 0f, 1400f, 60f, 0.3f, 0.4f);
            SoundSynth.Knock(track, 0.05f, 900f, 50f, 0.25f, 0.5f);
            SoundSynth.Bell(track, 0.11f, SoundSynth.Note(93), 0.75f, 0.7f);
            SoundSynth.Bell(track, 0.11f, SoundSynth.Note(100), 0.6f, 0.4f);
            for (int k = 0; k < 6; k++)
            {
                SoundSynth.Bell(track, 0.13f + k * 0.045f + Rand(track) * 0.02f, 3000f + Rand(track) * 2500f, 0.18f, 0.12f);
            }
            return SoundSynth.Finish(track, 0.72f);
        }

        /// <summary>A wrench: the clank of heavy steel, a quick ratchet, and a little rising chime (fixed).</summary>
        private static float[] PickupRepair()
        {
            var track = new SynthTrack(0.8f, false, 1401);
            SoundSynth.Tone(track, 0f, 0.6f, Wave.Sine, 620f, 618f, 0.45f, 0.001f, 7f, 0f, 6f, 0.02f, 2.76f, 1.4f);
            SoundSynth.Knock(track, 0f, 1180f, 18f, 0.4f, 0.35f);
            SoundSynth.Knock(track, 0f, 2930f, 26f, 0.18f, 0.2f);
            for (int k = 0; k < 5; k++)
            {
                SoundSynth.Knock(track, 0.16f + k * 0.045f, 2600f + k * 40f, 140f, 0.18f, 0.6f);
            }
            SoundSynth.Mallet(track, 0.4f, SoundSynth.Note(76), 0.3f, 0.35f);
            SoundSynth.Mallet(track, 0.5f, SoundSynth.Note(83), 0.3f, 0.4f);
            return SoundSynth.Finish(track, 0.72f);
        }

        /// <summary>Ammunition: two clacks of the box, shells rattling, the slide racked.</summary>
        private static float[] PickupAmmo()
        {
            var track = new SynthTrack(0.55f, false, 1501);
            SoundSynth.Knock(track, 0f, 640f, 38f, 0.55f, 0.7f);
            SoundSynth.Knock(track, 0.09f, 720f, 40f, 0.5f, 0.7f);
            for (int k = 0; k < 7; k++)
            {
                SoundSynth.Knock(track, 0.02f + Rand(track) * 0.25f, 3200f + Rand(track) * 2200f, 90f, 0.08f, 0.3f);
            }
            SoundSynth.Noise(track, 0.2f, 0.05f, 0.35f, 0.7f, 40f, 0.001f, 0.25f);
            SoundSynth.Knock(track, 0.27f, 1900f, 70f, 0.3f, 0.4f);
            return SoundSynth.Finish(track, 0.72f);
        }

        /// <summary>A nitro bottle: a rising whoosh of gas with an airy, climbing shimmer.</summary>
        private static float[] PickupNitro()
        {
            var track = new SynthTrack(0.75f, false, 1601);
            SoundSynth.Sweep(track, 0f, 0.6f, 400f, 3800f, 0.8f, 0.02f, 3.5f);
            SoundSynth.Noise(track, 0f, 0.5f, 0.25f, 0.5f, 5f, 0.01f, 0.3f);
            SoundSynth.Tone(track, 0.02f, 0.45f, Wave.Sine, 520f, 1250f, 0.3f, 0.01f, 3f);
            SoundSynth.Tone(track, 0.02f, 0.45f, Wave.Sine, 780f, 1875f, 0.15f, 0.01f, 4f);
            return SoundSynth.Finish(track, 0.7f);
        }

        // ------------------------------------------------------------------ race moments

        /// <summary>A countdown beep (three, two, one).</summary>
        private static float[] Countdown()
        {
            var track = new SynthTrack(0.3f, false, 1701);
            SoundSynth.Tone(track, 0f, 0.22f, Wave.Square, 880f, 880f, 0.5f, 0.003f, 2f, 0f, 6f, 0.04f, 0f, 0f, 3200f);
            SoundSynth.Tone(track, 0f, 0.22f, Wave.Sine, 440f, 440f, 0.4f, 0.003f, 2f, 0f, 6f, 0.04f);
            return SoundSynth.Finish(track, 0.7f);
        }

        /// <summary>GO: an octave above the countdown, longer, with a fifth in it.</summary>
        private static float[] Go()
        {
            var track = new SynthTrack(0.7f, false, 1801);
            SoundSynth.Tone(track, 0f, 0.62f, Wave.Square, 1760f, 1760f, 0.42f, 0.003f, 1.6f, 0.004f, 7f, 0.08f, 0f, 0f, 4500f);
            SoundSynth.Tone(track, 0f, 0.62f, Wave.Square, 1318.5f, 1318.5f, 0.24f, 0.003f, 1.6f, 0.004f, 7f, 0.08f, 0f, 0f, 4500f);
            SoundSynth.Tone(track, 0f, 0.62f, Wave.Sine, 880f, 880f, 0.4f, 0.003f, 1.4f, 0f, 6f, 0.08f);
            return SoundSynth.Finish(track, 0.8f);
        }

        /// <summary>A lap done: a bright rising two-note chime.</summary>
        private static float[] Lap()
        {
            var track = new SynthTrack(0.55f, false, 1901);
            SoundSynth.Mallet(track, 0f, SoundSynth.Note(88), 0.3f, 0.6f);
            SoundSynth.Mallet(track, 0.1f, SoundSynth.Note(93), 0.45f, 0.7f);
            SoundSynth.Bell(track, 0.1f, SoundSynth.Note(105), 0.4f, 0.18f);
            return SoundSynth.Finish(track, 0.65f);
        }

        /// <summary>A brassy stab: stacked saws through a low-pass, with a soft attack (fanfares and jingles).</summary>
        private static void Brass(SynthTrack track, float start, float duration, int midi, float volume, float vibrato = 0.004f)
        {
            float f = SoundSynth.Note(midi);
            SoundSynth.Tone(track, start, duration, Wave.Saw, f, f, volume, 0.025f, 0.8f, vibrato, 5.5f, 0.06f, 0f, 0f, 2200f);
            SoundSynth.Tone(track, start, duration, Wave.Saw, f * 1.004f, f * 1.004f, volume * 0.6f, 0.03f, 0.8f, vibrato, 5.2f, 0.06f, 0f, 0f, 1800f);
            SoundSynth.Tone(track, start, duration, Wave.Square, f * 0.5f, f * 0.5f, volume * 0.25f, 0.02f, 0.8f, 0f, 6f, 0.06f, 0f, 0f, 900f);
        }

        /// <summary>The finish: a quick arpeggio up to a held chord and a crash.</summary>
        private static float[] FinishFanfare()
        {
            var track = new SynthTrack(1.6f, false, 2001);
            int[] arpeggio = { 72, 76, 79 };
            for (int i = 0; i < arpeggio.Length; i++)
            {
                Brass(track, i * 0.09f, 0.14f, arpeggio[i], 0.35f);
            }
            foreach (int note in new[] { 72, 76, 79, 84 })
            {
                Brass(track, 0.27f, 1.2f, note, 0.22f);
            }
            SoundSynth.Noise(track, 0.27f, 1.3f, 0.25f, 1f, 2.6f, 0.002f, 0.35f);
            SoundSynth.Kick(track, 0.27f, 0.5f);
            return SoundSynth.Finish(track, 0.8f);
        }

        /// <summary>Wrecked: a sad descending brass line (the last note wobbling) over a dull boom.</summary>
        private static float[] WreckJingle()
        {
            var track = new SynthTrack(1.7f, false, 2101);
            int[] notes = { 64, 63, 62 };
            for (int i = 0; i < notes.Length; i++)
            {
                Brass(track, i * 0.24f, 0.22f, notes[i], 0.4f);
            }
            Brass(track, 0.72f, 0.9f, 61, 0.42f, 0.03f);
            SoundSynth.Boom(track, 0f, 0.8f, 0.45f, 1200f);
            return SoundSynth.Finish(track, 0.75f);
        }

        /// <summary>Victory: a fanfare over four chords with timpani and a crash at the top.</summary>
        private static float[] Victory()
        {
            var track = new SynthTrack(3.2f, false, 2201);
            int[][] chords = { new[] { 60, 64, 67, 72 }, new[] { 65, 69, 72, 77 }, new[] { 67, 71, 74, 79 }, new[] { 72, 76, 79, 84 } };
            float[] starts = { 0f, 0.42f, 0.84f, 1.26f };
            float[] lengths = { 0.38f, 0.38f, 0.38f, 1.8f };
            for (int c = 0; c < chords.Length; c++)
            {
                foreach (int note in chords[c])
                {
                    Brass(track, starts[c], lengths[c], note, 0.16f);
                }
                SoundSynth.Tone(track, starts[c], 0.5f, Wave.Sine, SoundSynth.Note(chords[c][0] - 24) * 1.4f, SoundSynth.Note(chords[c][0] - 24), 0.55f, 0.002f, 6f);
                SoundSynth.Snare(track, starts[c], 0.25f);
            }
            int[] melody = { 79, 81, 83, 84 };
            for (int i = 0; i < melody.Length; i++)
            {
                Brass(track, starts[i] + 0.21f, i == melody.Length - 1 ? 1.6f : 0.18f, melody[i] + 12, 0.14f, 0.008f);
            }
            SoundSynth.Noise(track, 1.26f, 1.9f, 0.3f, 1f, 1.8f, 0.002f, 0.35f);
            SoundSynth.Kick(track, 1.26f, 0.7f);
            return SoundSynth.Finish(track, 0.8f);
        }

        // ------------------------------------------------------------------ interface

        /// <summary>Bought: a bright coin ching.</summary>
        private static float[] Buy()
        {
            var track = new SynthTrack(0.5f, false, 2301);
            SoundSynth.Knock(track, 0f, 1500f, 70f, 0.2f, 0.4f);
            SoundSynth.Bell(track, 0.03f, SoundSynth.Note(96), 0.4f, 0.55f);
            SoundSynth.Bell(track, 0.1f, SoundSynth.Note(100), 0.38f, 0.5f);
            return SoundSynth.Finish(track, 0.6f);
        }

        /// <summary>Sold: coins tumbling down into a till.</summary>
        private static float[] Sell()
        {
            var track = new SynthTrack(0.55f, false, 2401);
            for (int k = 0; k < 6; k++)
            {
                SoundSynth.Bell(track, k * 0.05f + Rand(track) * 0.015f, 3800f - k * 300f + Rand(track) * 400f, 0.2f, 0.25f);
            }
            SoundSynth.Knock(track, 0.3f, 700f, 40f, 0.25f, 0.5f);
            return SoundSynth.Finish(track, 0.6f);
        }

        /// <summary>Not possible: a low, slightly sour buzz.</summary>
        private static float[] Deny()
        {
            var track = new SynthTrack(0.34f, false, 2501);
            SoundSynth.Tone(track, 0f, 0.32f, Wave.Square, 140f, 136f, 0.45f, 0.004f, 3f, 0f, 6f, 0.05f, 0f, 0f, 1500f);
            SoundSynth.Tone(track, 0f, 0.32f, Wave.Square, 133f, 129f, 0.4f, 0.004f, 3f, 0f, 6f, 0.05f, 0f, 0f, 1500f);
            return SoundSynth.Finish(track, 0.5f);
        }

        /// <summary>A button: a short dry tick.</summary>
        private static float[] Click()
        {
            var track = new SynthTrack(0.07f, false, 2601);
            SoundSynth.Knock(track, 0f, 1900f, 160f, 0.6f, 0.25f);
            SoundSynth.Tone(track, 0f, 0.03f, Wave.Sine, 2400f, 2000f, 0.25f, 0.0005f, 90f);
            return SoundSynth.Finish(track, 0.45f);
        }
    }
}
