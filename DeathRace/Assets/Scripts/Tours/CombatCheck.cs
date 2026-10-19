using System.Collections;
using System.Collections.Generic;
using System.IO;
using Gamebox;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// Development players only. Started with <c>-deathrace-combatcheck &lt;folder&gt;</c>, it runs races built to fight
    /// (every car loaded with mines, rockets and nitro, starting hurt so wrecks come early, the computer's drivers made
    /// trigger happy), follows the first car, and saves screenshots at the moments of action (explosions near the view,
    /// a rocket in the air, a mine lying armed, a car burning) and every few seconds, with a log of every hit, wreck and
    /// kill (<c>combat.log</c>), then quits. <c>-deathrace-combatcheck-tracks 0,3</c> picks the tracks (Dust Bowl and
    /// Neon Mile by default); <c>-deathrace-combatcheck-seconds 50</c> how long each race is watched. Problems are FAIL
    /// lines; errors and exceptions are counted. Without the argument it does nothing.
    /// </summary>
    public class CombatCheck : MonoBehaviour
    {
        private const string Argument = "-deathrace-combatcheck";
        private const string TracksArgument = "-deathrace-combatcheck-tracks";
        private const string SecondsArgument = "-deathrace-combatcheck-seconds";
        private const float ShotGap = 1.4f;
        private const int ShotsPerRace = 22;

        /// <summary>The followed car is a fully armoured Bulldog: slower than the rest, so it stays behind them in the fight.</summary>
        private static readonly CarModel[] Models = { CarModel.Bulldog, CarModel.Viper, CarModel.Hornet, CarModel.Reaper };
        private static readonly Color[] Colors =
        {
            new Color(0.95f, 0.75f, 0.1f), new Color(0.85f, 0.15f, 0.12f), new Color(0.2f, 0.55f, 0.95f), new Color(0.25f, 0.8f, 0.35f)
        };

        private string folder;
        private StreamWriter log;
        private DeathRaceManager manager;
        private Race race;
        private int shot;
        private int shotsThisRace;
        private float lastShot = float.NegativeInfinity;
        private int errors;
        private int failures;
        private readonly Dictionary<DamageSource, float> damageBySource = new Dictionary<DamageSource, float>();
        private readonly Dictionary<string, int> played = new Dictionary<string, int>();
        private int kills;
        private int wrecks;
        private int pickups;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!Debug.isDebugBuild || Application.isEditor)
            {
                return;
            }
            string target = MobilePlatform.ArgumentValue(Argument);
            if (string.IsNullOrEmpty(target) || FindAnyObjectByType<CombatCheck>() != null)
            {
                return;
            }
            var host = new GameObject("Combat Check");
            DontDestroyOnLoad(host);
            host.AddComponent<CombatCheck>().folder = target;
        }

        private IEnumerator Start()
        {
            Directory.CreateDirectory(folder);
            log = new StreamWriter(Path.Combine(folder, "combat.log")) { AutoFlush = true };
            Application.logMessageReceived += OnLog;
            while ((manager = FindAnyObjectByType<DeathRaceManager>()) == null || manager.Director == null)
            {
                yield return null;
            }
            yield return new WaitForSecondsRealtime(2.5f);
            Write($"combat check: theme {(manager.Look != null ? manager.Look.name : "none")}, content {(manager.Content != null ? manager.Content.name : "none")}");
            Shoot("menu");

            float seconds = 50f;
            if (float.TryParse(MobilePlatform.ArgumentValue(SecondsArgument), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float asked) && asked > 5f)
            {
                seconds = asked;
            }
            foreach (int track in Tracks())
            {
                yield return Fight(track, seconds);
            }

            Write("---- summary");
            foreach (KeyValuePair<DamageSource, float> pair in damageBySource)
            {
                Write($"damage {pair.Key}: {pair.Value:0}");
            }
            foreach (KeyValuePair<string, int> pair in played)
            {
                Write($"effect {pair.Key}: {pair.Value}");
            }
            Write($"wrecks {wrecks}, kills credited {kills}, pickups {pickups}");
            Check(damageBySource.ContainsKey(DamageSource.Gun), "the guns hurt someone");
            Check(damageBySource.ContainsKey(DamageSource.Rocket) || damageBySource.ContainsKey(DamageSource.Mine), "a rocket or a mine hurt someone");
            Check(played.ContainsKey("explosion"), "something exploded");
            Check(wrecks > 0, "a car was wrecked");
            Write($"combat check finished: {failures} FAIL, {errors} errors, {shot} screenshots");
            Application.logMessageReceived -= OnLog;
            manager.LeaveRace();
            yield return new WaitForSecondsRealtime(0.5f);
            log.Dispose();
            Application.Quit();
        }

        private IEnumerable<int> Tracks()
        {
            string asked = MobilePlatform.ArgumentValue(TracksArgument);
            if (string.IsNullOrEmpty(asked))
            {
                yield return 0;
                yield return 3;
                yield break;
            }
            foreach (string part in asked.Split(','))
            {
                if (int.TryParse(part.Trim(), out int track))
                {
                    yield return Mathf.Clamp(track, 0, TrackLayouts.Count - 1);
                }
            }
        }

        // ------------------------------------------------------------------------------------------------
        // One fight
        // ------------------------------------------------------------------------------------------------

        private IEnumerator Fight(int track, float seconds)
        {
            RaceSetup setup = Setup(track);
            // The last race's results window gives way to the next race.
            foreach (Gamebox.UI.OverlayWindow window in FindObjectsByType<Gamebox.UI.OverlayWindow>(FindObjectsSortMode.None))
            {
                if (window.IsOpen)
                {
                    window.Hide();
                }
            }
            Write($"---- race on {TrackLayouts.Of(track).Name}: {setup.Entrants.Count} cars, {seconds:0} s");
            RaceDirector director = manager.Director;
            director.DriverFor = entrant => Pilot(entrant, setup.Seed);
            manager.BeginRace(setup);
            director.DriverFor = null;
            race = director.Current;
            if (!Check(race != null, "the race began"))
            {
                yield break;
            }
            shotsThisRace = 0;
            Watch(race);
            Shoot($"t{track}-grid");
            float start = Time.time;
            float nextPeriodic = 6f;
            float nextReport = 2f;
            bool rocketShot = false;
            bool mineShot = false;
            bool burningShot = false;
            int loudest = 0;
            while (Time.time - start < seconds && director.Current == race)
            {
                float t = Time.time - start;
                if (t >= nextReport)
                {
                    nextReport += 10f;
                    ReportLooks();
                }
                if (t >= nextPeriodic)
                {
                    nextPeriodic += 7f;
                    Shoot($"t{track}-{t:00}s");
                }
                if (!rocketShot && RocketInView())
                {
                    rocketShot = Shoot($"t{track}-rocket", true);
                }
                if (!mineShot && ArmedMineInView())
                {
                    mineShot = Shoot($"t{track}-mine", true);
                }
                if (!burningShot && BurningInView())
                {
                    burningShot = Shoot($"t{track}-burning", true);
                }
                if (race.Phase == RacePhase.Over)
                {
                    break;
                }
                loudest = Mathf.Max(loudest, RaceAudio.Current != null ? RaceAudio.Current.VoicesPlaying : 0);
                yield return null;
            }
            foreach (Car car in race.Cars)
            {
                if (car == null)
                {
                    continue;
                }
                CarWeapons weapons = car.Weapons;
                Write($"{car.Entrant.Name}: health {car.Health.Health01:0.00} wrecked {car.IsWrecked} kills {car.Racer?.Kills} " +
                    $"rounds {weapons.RoundsFired} hit {weapons.RoundsHit} mines {weapons.MinesUsed} rockets {weapons.RocketsUsed} " +
                    $"nitro {weapons.NitroUsed} cash {car.Racer?.CashPicked} damage taken {car.Health.DamageTaken:0}");
            }
            RaceAudio audio = RaceAudio.Current;
            Write($"audio: music {(audio != null && audio.MusicClip != null ? audio.MusicClip.name : "none")}, gun clip {(audio != null && audio.Sounds?.gun != null ? audio.Sounds.gun.name : "none")}, at most {loudest} one-shots at once");
            Check(audio != null && (audio.Sounds?.gun == null || loudest > 0), "the fight was heard");
            if (race.Effects != null)
            {
                race.Effects.Played -= OnPlayed;
            }
        }

        /// <summary>Four cars of different weights, every one armed to the teeth and already hurt.</summary>
        private static RaceSetup Setup(int track)
        {
            var setup = new RaceSetup
            {
                Mode = RaceMode.Quick,
                Track = track,
                Laps = 3,
                Difficulty = Difficulty.Hard,
                Seed = 1234 + track,
                CashPerPickup = 150,
                Pickups = true
            };
            for (int seat = 0; seat < 4; seat++)
            {
                CarLoadout loadout = seat == 0 ? new CarLoadout { model = Models[0], armor = CarSpecs.Of(Models[0]).MaxLevelOf(UpgradeKind.Armor) } : CarLoadout.Upgraded(Models[seat], 1);
                loadout.mines = 8;
                loadout.rockets = 6;
                loadout.nitro = 3;
                loadout.spikes = seat == 1;
                loadout.piercing = seat == 3;
                loadout.health = seat == 0 ? 1f : 0.45f + 0.1f * seat;
                setup.Entrants.Add(new RaceEntrant
                {
                    Seat = seat,
                    Name = seat == 0 ? "Checker" : $"Rival {seat}",
                    Color = Colors[seat],
                    Driver = seat == 0 ? DriverKind.Human : DriverKind.Computer,
                    Skill = 0.8f,
                    Loadout = loadout,
                    Viewport = seat == 0 ? 0 : -1
                });
            }
            return setup;
        }

        /// <summary>The computer's driving with a quicker trigger: guns at anything ahead, mines at anything behind, rockets into the cone.</summary>
        private static ICarDriver Pilot(RaceEntrant entrant, int seed)
        {
            var ai = new AiDriver(0.85f, unchecked(seed * 31 + entrant.Seat * 7919));
            float mineReady = 2f + entrant.Seat;
            float rocketReady = 3f + entrant.Seat * 1.5f;
            float nitroReady = 5f + entrant.Seat * 2f;
            return new ScriptedDriver((car, deltaTime) =>
            {
                CarInput input = ai.Drive(car, deltaTime);
                if (car.Race == null || !car.Race.IsRunning || car.IsWrecked)
                {
                    return input;
                }
                mineReady -= deltaTime;
                rocketReady -= deltaTime;
                nitroReady -= deltaTime;
                foreach (Car other in car.Race.Cars)
                {
                    if (other == null || other == car || other.IsWrecked)
                    {
                        continue;
                    }
                    Vector3 local = car.transform.InverseTransformPoint(other.transform.position);
                    if (local.z > 2f && local.z < 55f && Mathf.Abs(local.x) < 2.6f)
                    {
                        input.Fire = true;
                    }
                    if (mineReady <= 0f && local.z < -5f && local.z > -28f && Mathf.Abs(local.x) < 4f)
                    {
                        input.DropMine = true;
                        mineReady = 3f;
                    }
                    if (rocketReady <= 0f && local.z > 12f && local.z < 70f && CombatRules.InCone(Vector2.up, new Vector2(local.x, local.z), CombatRules.RocketCone))
                    {
                        input.FireRocket = true;
                        rocketReady = 5f;
                    }
                }
                if (nitroReady <= 0f && input.Throttle > 0.9f && Mathf.Abs(input.Steer) < 0.12f)
                {
                    input.Nitro = true;
                    nitroReady = 9f;
                }
                return input;
            });
        }

        private void Watch(Race watched)
        {
            if (watched.Effects != null)
            {
                watched.Effects.Played += OnPlayed;
            }
            foreach (Car car in watched.Cars)
            {
                Car victim = car;
                car.Health.Damaged += info =>
                {
                    damageBySource.TryGetValue(info.Source, out float sum);
                    damageBySource[info.Source] = sum + info.Amount;
                    if (info.Source != DamageSource.Gun)
                    {
                        Write($"{watched.Time:0.0}s {victim.Entrant.Name} hit: {info} -> health {victim.Health.Health01:0.00}");
                    }
                };
                car.Wrecked += (wreck, cause) =>
                {
                    wrecks++;
                    kills += cause.Attacker != null ? 1 : 0;
                    Write($"{watched.Time:0.0}s WRECK {wreck.Entrant.Name} by {(cause.Attacker != null ? cause.Attacker.Entrant.Name : "nobody")} ({cause.Source})");
                };
                car.Happened += happened =>
                {
                    if (happened.Kind == CarEventKind.Pickup)
                    {
                        pickups++;
                        Write($"{watched.Time:0.0}s {victim.Entrant.Name} picked {happened.Pickup} ({happened.Amount})");
                    }
                    else if (happened.Kind == CarEventKind.Kill)
                    {
                        Write($"{watched.Time:0.0}s KILL for {victim.Entrant.Name} ({(happened.Other != null ? happened.Other.Entrant.Name : "?")})");
                    }
                };
            }
        }

        /// <summary>What each car shows of its damage: its health, and the effects riding on it with their particles.</summary>
        private void ReportLooks()
        {
            foreach (Car car in race.Cars)
            {
                if (car == null)
                {
                    continue;
                }
                var parts = new List<string>();
                foreach (RaceEffect effect in car.GetComponentsInChildren<RaceEffect>(true))
                {
                    int particles = 0;
                    string sample = "";
                    foreach (ParticleSystem system in effect.GetComponentsInChildren<ParticleSystem>(true))
                    {
                        particles += system.particleCount;
                        if (sample.Length == 0 && system.particleCount > 0)
                        {
                            var one = new ParticleSystem.Particle[1];
                            system.GetParticles(one, 1);
                            Vector3 at = system.main.simulationSpace == ParticleSystemSimulationSpace.Local ? system.transform.TransformPoint(one[0].position) : one[0].position;
                            sample = $" [{system.name}: {at - car.transform.position:F1} from the car, size {one[0].GetCurrentSize(system):0.0}, colour {one[0].GetCurrentColor(system)}, renderer {(system.GetComponent<ParticleSystemRenderer>().enabled ? "on" : "off")} {system.GetComponent<ParticleSystemRenderer>().sharedMaterial?.shader?.name}]";
                        }
                    }
                    parts.Add($"{effect.name}{(effect.gameObject.activeInHierarchy ? "" : " (off)")}{(effect.Playing ? "" : " (stopping)")} {particles}p{sample}");
                }
                Write($"{race.Time:0.0}s looks {car.Entrant.Name}: health {car.Health.Health01:0.00} wrecked {car.IsWrecked}: {string.Join(", ", parts)}");
            }
        }

        private void OnPlayed(string kind, Vector3 at)
        {
            played.TryGetValue(kind, out int count);
            played[kind] = count + 1;
            if ((kind == "explosion" || kind == "big explosion") && InView(at, 28f))
            {
                StartCoroutine(ShootSoon($"{kind.Replace(' ', '-')}", kind == "big explosion" ? 0.16f : 0.1f));
            }
        }

        private IEnumerator ShootSoon(string label, float delay)
        {
            yield return new WaitForSeconds(delay);
            Shoot(label);
        }

        // ------------------------------------------------------------------------------------------------
        // What is in view
        // ------------------------------------------------------------------------------------------------

        private Vector3 Focus
        {
            get
            {
                RaceDirector director = manager.Director;
                return director != null && director.Cameras.Count > 0 && director.Cameras[0] != null ? director.Cameras[0].Focus : Vector3.zero;
            }
        }

        private bool InView(Vector3 at, float reach)
        {
            Vector3 focus = Focus;
            return new Vector2(at.x - focus.x, at.z - focus.z).magnitude < reach;
        }

        private bool RocketInView()
        {
            foreach (Rocket rocket in Rocket.Active)
            {
                if (rocket != null && rocket.Age > 0.25f && InView(rocket.Position, 22f))
                {
                    return true;
                }
            }
            return false;
        }

        private bool ArmedMineInView()
        {
            foreach (Mine mine in Mine.Active)
            {
                if (mine != null && mine.Armed && InView(mine.Position, 18f))
                {
                    return true;
                }
            }
            return false;
        }

        private bool BurningInView()
        {
            foreach (Car car in race.Cars)
            {
                if (car != null && !car.IsWrecked && car.Health.Health01 < CombatRules.BurningAt && InView(car.transform.position, 20f))
                {
                    return true;
                }
            }
            return false;
        }

        // ------------------------------------------------------------------------------------------------
        // Output
        // ------------------------------------------------------------------------------------------------

        /// <summary>Saves a numbered screenshot (not too many, not too close together, unless it is a moment asked for).</summary>
        private bool Shoot(string label, bool moment = false)
        {
            float now = Time.unscaledTime;
            if (!moment && (now - lastShot < ShotGap || shotsThisRace >= ShotsPerRace))
            {
                return false;
            }
            lastShot = now;
            shotsThisRace++;
            shot++;
            string file = Path.Combine(folder, $"{shot:00}-{label}.png");
            ScreenCapture.CaptureScreenshot(file);
            Write($"screenshot {Path.GetFileName(file)}");
            return true;
        }

        private bool Check(bool condition, string what)
        {
            if (!condition)
            {
                failures++;
                Write($"FAIL {what}");
            }
            return condition;
        }

        private void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error)
            {
                errors++;
                Write($"{type}: {message}\n{stack}");
            }
        }

        private void Write(string line)
        {
            log?.WriteLine(line);
        }
    }
}
