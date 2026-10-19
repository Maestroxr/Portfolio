using System.Collections;
using System.Collections.Generic;
using System.IO;
using Gamebox;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// Development players only. Started with <c>-deathrace-combatcheck &lt;folder&gt;</c>, it runs races built to fight
    /// (every car loaded with mines, rockets, nitro and all three special weapons, a gun and fittings of its own that change
    /// from race to race, starting hurt so wrecks come early, the computer's drivers made trigger happy), follows the first
    /// car, and saves screenshots at the moments of action (explosions near the view, a rocket in the air, a mine lying
    /// armed, a car burning, an oil slick, an EMP going off, a mortar shell in the air and bursting, plasma bolts, the
    /// flamethrower at work, a car on fire, slicked or jammed, a welder at work) and every few seconds, with a log of every
    /// hit, wreck, kill, special and status (<c>combat.log</c>), then quits. The followed car's gun goes round the four
    /// guns, one race each. <c>-deathrace-combatcheck-tracks 0,3</c> picks the tracks (Dust Bowl, Neon Mile, Smelter and
    /// Downtown Carnage by default); <c>-deathrace-combatcheck-seconds 50</c> how long each race is watched;
    /// <c>-deathrace-combatcheck-split 2</c> (to 4) makes that many of the cars people's in split screen (each with its HUD,
    /// driven by the check).
    ///
    /// <c>-deathrace-combatcheck-full</c> runs whole races instead, as a career race would be (every car at full health,
    /// upgraded, with a gun, fittings and specials of a seeded draw, the plain computer drivers in every seat, the first
    /// one's included) to the end, and counts what the fighting costs: the wrecks of the field, by cause, against the cars
    /// that raced (<c>-deathrace-combatcheck-speed 3</c> runs them faster; <c>-deathrace-combatcheck-plain</c> races the same
    /// cars with the machine gun only, no fittings and no specials, to compare). Problems are FAIL lines; errors and
    /// exceptions are counted. Without the argument it does nothing.
    /// </summary>
    public class CombatCheck : MonoBehaviour
    {
        private const string Argument = "-deathrace-combatcheck";
        private const string TracksArgument = "-deathrace-combatcheck-tracks";
        private const string SecondsArgument = "-deathrace-combatcheck-seconds";
        private const string SplitArgument = "-deathrace-combatcheck-split";
        private const string FullArgument = "-deathrace-combatcheck-full";
        private const string SpeedArgument = "-deathrace-combatcheck-speed";
        private const string PlainArgument = "-deathrace-combatcheck-plain";
        private const float ShotGap = 1.4f;
        private const int ShotsPerRace = 26;

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
        private RaceResult result;
        private int shot;
        private int shotsThisRace;
        private int raceIndex;
        private float lastShot = float.NegativeInfinity;
        private int errors;
        private int failures;
        private readonly Dictionary<DamageSource, float> damageBySource = new Dictionary<DamageSource, float>();
        private readonly Dictionary<string, int> played = new Dictionary<string, int>();
        private readonly Dictionary<string, int> fired = new Dictionary<string, int>();
        private readonly Dictionary<CarStatusKind, int> statuses = new Dictionary<CarStatusKind, int>();
        private readonly Dictionary<DamageSource, int> wreckCauses = new Dictionary<DamageSource, int>();
        private readonly HashSet<string> moments = new HashSet<string>();
        private int kills;
        private int wrecks;
        private int carsRaced;
        private int pickups;
        private float welded;

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

        private static bool Full => MobilePlatform.HasArgument(FullArgument);

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
            Write($"combat check: theme {(manager.Look != null ? manager.Look.name : "none")}, content {(manager.Content != null ? manager.Content.name : "none")}" +
                  $"{(Full ? ", whole races" : "")}");
            Shoot("menu");

            float seconds = 50f;
            if (float.TryParse(MobilePlatform.ArgumentValue(SecondsArgument), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float asked) && asked > 5f)
            {
                seconds = asked;
            }
            foreach (int track in Tracks())
            {
                if (Full)
                {
                    yield return WholeRace(track);
                }
                else
                {
                    yield return Fight(track, seconds);
                }
                raceIndex++;
            }
            Time.timeScale = 1f;

            Write("---- summary");
            foreach (KeyValuePair<DamageSource, float> pair in damageBySource)
            {
                Write($"damage {pair.Key}: {pair.Value:0}");
            }
            foreach (KeyValuePair<string, int> pair in played)
            {
                Write($"effect {pair.Key}: {pair.Value}");
            }
            foreach (KeyValuePair<string, int> pair in fired)
            {
                Write($"fired {pair.Key}: {pair.Value}");
            }
            foreach (KeyValuePair<CarStatusKind, int> pair in statuses)
            {
                Write($"status {pair.Key}: {pair.Value}");
            }
            foreach (KeyValuePair<DamageSource, int> pair in wreckCauses)
            {
                Write($"wrecked by {pair.Key}: {pair.Value}");
            }
            Write($"wrecks {wrecks} of {carsRaced} cars ({(carsRaced > 0 ? wrecks / (float)carsRaced : 0f):0.00}), kills credited {kills}, pickups {pickups}, welded {welded:0}");
            Write($"moments shot: {string.Join(", ", moments)}");
            Check(damageBySource.ContainsKey(DamageSource.Gun), "the guns hurt someone");
            Check(played.ContainsKey("explosion"), "something exploded");
            if (!Full)
            {
                Check(damageBySource.ContainsKey(DamageSource.Rocket) || damageBySource.ContainsKey(DamageSource.Mine), "a rocket or a mine hurt someone");
                Check(wrecks > 0, "a car was wrecked");
                Check(fired.ContainsKey("oil") && fired.ContainsKey("emp") && fired.ContainsKey("mortar"), "every special weapon was fired");
                Check(damageBySource.ContainsKey(DamageSource.Flame), "the flamethrower hurt someone");
                Check(damageBySource.ContainsKey(DamageSource.Plasma), "a plasma bolt hurt someone");
                Check(statuses.ContainsKey(CarStatusKind.Burning), "a car was set burning");
                Check(statuses.ContainsKey(CarStatusKind.Jammed), "a car was jammed");
                Check(statuses.ContainsKey(CarStatusKind.Slicked), "a car was slicked");
            }
            else
            {
                Check(carsRaced == 0 || wrecks <= carsRaced * 0.45f, "the field mostly finishes (wrecks not much over 1 in 4)");
            }
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
                yield return 5;
                yield return 7;
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
            int people = Mathf.Clamp(Number(SplitArgument, 1f), 1, 4);
            RaceSetup setup = Setup(track, raceIndex, people);
            HideWindows();
            Write($"---- race on {TrackLayouts.Of(track).Name}: {setup.Entrants.Count} cars, {seconds:0} s, {people} at this device");
            foreach (RaceEntrant entrant in setup.Entrants)
            {
                CarLoadout loadout = entrant.Loadout;
                Write($"  seat {entrant.Seat} {entrant.Name}: {loadout.model} gun {loadout.gun} oil {loadout.oil} emp {loadout.emp} mortar {loadout.mortar} " +
                      $"armed {loadout.special} {Fittings(loadout)}");
            }
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
            var shotOnce = new HashSet<string>();
            var since = new Dictionary<string, float>();
            var seen = new HashSet<string>();
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
                // A loop (a flame, sparks) is shot once it has burned a moment, not on the frame it lights with nothing out yet.
                seen.Clear();
                foreach (string moment in MomentsInView())
                {
                    seen.Add(moment);
                    if (!since.ContainsKey(moment))
                    {
                        since[moment] = t;
                    }
                    if (!shotOnce.Contains(moment) && t - since[moment] >= Settle(moment) && Shoot($"t{track}-{moment}", true))
                    {
                        shotOnce.Add(moment);
                        moments.Add(moment);
                    }
                }
                foreach (string moment in new List<string>(since.Keys))
                {
                    if (!seen.Contains(moment))
                    {
                        since.Remove(moment);
                    }
                }
                if (race.Phase == RacePhase.Over)
                {
                    break;
                }
                loudest = Mathf.Max(loudest, RaceAudio.Current != null ? RaceAudio.Current.VoicesPlaying : 0);
                yield return null;
            }
            Tally(race);
            RaceAudio audio = RaceAudio.Current;
            Write($"audio: music {(audio != null && audio.MusicClip != null ? audio.MusicClip.name : "none")}, gun clip {(audio != null && audio.Sounds?.gun != null ? audio.Sounds.gun.name : "none")}, at most {loudest} one-shots at once");
            Check(audio != null && (audio.Sounds?.gun == null || loudest > 0), "the fight was heard");
            if (race.Effects != null)
            {
                race.Effects.Played -= OnPlayed;
            }
        }

        /// <summary>A whole race as a career would run it, the plain computer drivers in every seat, to the end.</summary>
        private IEnumerator WholeRace(int track)
        {
            RaceSetup setup = FullSetup(track, raceIndex);
            HideWindows();
            Write($"---- whole race on {TrackLayouts.Of(track).Name}: {setup.Entrants.Count} cars, {(setup.Laps > 0 ? setup.Laps : TrackLayouts.Of(track).Laps)} laps");
            foreach (RaceEntrant entrant in setup.Entrants)
            {
                CarLoadout loadout = entrant.Loadout;
                Write($"  seat {entrant.Seat} {entrant.Name}: {loadout.model} skill {entrant.Skill:0.00} gun {loadout.gun} oil {loadout.oil} emp {loadout.emp} " +
                      $"mortar {loadout.mortar} mines {loadout.mines} rockets {loadout.rockets} {Fittings(loadout)}");
            }
            RaceDirector director = manager.Director;
            director.DriverFor = entrant => entrant.IsHuman ? new AiDriver(0.8f, unchecked(setup.Seed * 31 + 7)) : null;
            result = null;
            director.Finished += OnFinished;
            manager.BeginRace(setup);
            director.DriverFor = null;
            race = director.Current;
            if (!Check(race != null, "the race began"))
            {
                director.Finished -= OnFinished;
                yield break;
            }
            Time.timeScale = Mathf.Clamp(Value(SpeedArgument, 1f), 0.25f, 3f);
            shotsThisRace = 0;
            Watch(race);
            float start = Time.unscaledTime;
            float nextShot = start + 8f;
            while (result == null && Time.unscaledTime - start < 420f && director.Current == race)
            {
                if (Time.unscaledTime >= nextShot)
                {
                    nextShot += 15f;
                    Shoot($"full{track}-{race.Time:000}s");
                }
                yield return null;
            }
            Time.timeScale = 1f;
            director.Finished -= OnFinished;
            Check(result != null, $"the race on {TrackLayouts.Of(track).Name} ended");
            Tally(race);
            if (result != null)
            {
                foreach (RacerResult racer in result.Racers)
                {
                    Write($"  P{racer.Place} seat {racer.Seat} {racer.Name}: {(racer.Finished ? "finished" : racer.Wrecked ? "WRECKED" : "running")} " +
                          $"time {racer.Time:0.0} kills {racer.Kills} oil {racer.OilUsed} emp {racer.EmpUsed} mortar {racer.MortarUsed} " +
                          $"mines {racer.MinesUsed} rockets {racer.RocketsUsed}");
                }
            }
            if (race.Effects != null)
            {
                race.Effects.Played -= OnPlayed;
            }
        }

        private void OnFinished(RaceResult finished)
        {
            result = finished;
        }

        /// <summary>The last race's results window gives way to the next race.</summary>
        private static void HideWindows()
        {
            foreach (Gamebox.UI.OverlayWindow window in FindObjectsByType<Gamebox.UI.OverlayWindow>(FindObjectsSortMode.None))
            {
                if (window.IsOpen)
                {
                    window.Hide();
                }
            }
        }

        /// <summary>What each car did and what it took this race.</summary>
        private void Tally(Race watched)
        {
            foreach (Car car in watched.Cars)
            {
                if (car == null)
                {
                    continue;
                }
                carsRaced++;
                welded += car.Health.Welded;
                CarWeapons weapons = car.Weapons;
                Write($"{car.Entrant.Name}: health {car.Health.Health01:0.00} wrecked {car.IsWrecked} kills {car.Racer?.Kills} gun {weapons.Gun} " +
                      $"rounds {weapons.RoundsFired} hit {weapons.RoundsHit} mines {weapons.MinesUsed} rockets {weapons.RocketsUsed} " +
                      $"oil {weapons.SpecialsUsed(SpecialKind.Oil)} emp {weapons.SpecialsUsed(SpecialKind.Emp)} mortar {weapons.SpecialsUsed(SpecialKind.Mortar)} " +
                      $"nitro {weapons.NitroUsed} cash {car.Racer?.CashPicked} damage taken {car.Health.DamageTaken:0} welded {car.Health.Welded:0}");
            }
        }

        /// <summary>
        /// Four cars of different weights, every one armed to the teeth and already hurt: a gun each (the followed car's
        /// goes round the four, one race each), all three specials (each car arms another first), and two fittings each.
        /// <paramref name="people"/> of them are people's cars in split screen (all driven by the check).
        /// </summary>
        private static RaceSetup Setup(int track, int index, int people)
        {
            var setup = new RaceSetup
            {
                Mode = people > 1 ? RaceMode.Local : RaceMode.Quick,
                Track = track,
                Laps = 3,
                Difficulty = Difficulty.Hard,
                Seed = 1234 + track,
                CashPerPickup = 150,
                Pickups = true
            };
            GunKind[] order = { GunKind.MachineGun, GunKind.Minigun, GunKind.Flamethrower, GunKind.Plasma };
            for (int seat = 0; seat < 4; seat++)
            {
                CarLoadout loadout = seat == 0 ? new CarLoadout { model = Models[0], armor = CarSpecs.Of(Models[0]).MaxLevelOf(UpgradeKind.Armor) } : CarLoadout.Upgraded(Models[seat], 1);
                loadout.mines = 8;
                loadout.rockets = 6;
                loadout.nitro = 3;
                loadout.oil = 2;
                loadout.emp = 2;
                loadout.mortar = 3;
                loadout.special = (SpecialKind)((seat + index) % Specials.Count);
                loadout.GiveGun(order[(seat + index) % order.Length]);
                loadout.spikes = seat == 1;
                loadout.piercing = seat == 3;
                loadout.cage = seat == 0;
                loadout.welder = seat == 0 || seat == 2;
                loadout.shield = seat == 1;
                loadout.jammer = seat == 2;
                loadout.magazine = seat == 3;
                loadout.health = seat == 0 ? 0.7f : 0.45f + 0.1f * seat;
                bool person = seat < people;
                setup.Entrants.Add(new RaceEntrant
                {
                    Seat = seat,
                    Name = seat == 0 ? "Checker" : person ? $"Checker {seat + 1}" : $"Rival {seat}",
                    Color = Colors[seat],
                    Driver = person ? DriverKind.Human : DriverKind.Computer,
                    Skill = 0.8f,
                    Loadout = loadout,
                    Viewport = person ? seat : -1
                });
            }
            return setup;
        }

        /// <summary>
        /// A whole race as a hard career race of the middle of the game: four cars of tiers 3 to 6, upgraded, at full
        /// health, each with a seeded gun, two fittings, a few mines, rockets and nitro charges and one or two specials of
        /// a seeded kind, raced by skilled drivers.
        /// </summary>
        private static RaceSetup FullSetup(int track, int index)
        {
            var random = new System.Random(4321 + track * 17 + index);
            var setup = new RaceSetup
            {
                Mode = RaceMode.Quick,
                Track = track,
                Laps = 0,
                Difficulty = Difficulty.Hard,
                Seed = 5678 + track + index * 101,
                CashPerPickup = 150,
                Pickups = true
            };
            CarModel[] models = { CarModel.Bulldog, CarModel.Viper, CarModel.Reaper, CarModel.Tyrant, CarModel.Mauler, CarModel.Banshee };
            for (int seat = 0; seat < 4; seat++)
            {
                CarLoadout loadout = CarLoadout.Upgraded(models[random.Next(models.Length)], 2 + random.Next(2));
                loadout.mines = 2 + random.Next(2);
                loadout.rockets = 1 + random.Next(2);
                loadout.nitro = 2 + random.Next(2);
                // The draws are made either way, so a plain race has the same cars, drivers and seeds as the armed one.
                var gun = (GunKind)random.Next(Guns.Count);
                var kind = (SpecialKind)random.Next(Specials.Count);
                int specials = 1 + random.Next(2);
                var fittings = new[] { (EquipmentKind)random.Next(Equipment.Count), (EquipmentKind)random.Next(Equipment.Count) };
                if (!MobilePlatform.HasArgument(PlainArgument))
                {
                    loadout.GiveGun(gun);
                    loadout.SetSpecials(kind, specials);
                    loadout.special = kind;
                    foreach (EquipmentKind fitting in fittings)
                    {
                        loadout.Set(fitting, true);
                    }
                }
                else
                {
                    // The upgrades of the new kinds stay off too: the first game's car.
                    loadout.boost = 0;
                    loadout.gunMount = 0;
                }
                setup.Entrants.Add(new RaceEntrant
                {
                    Seat = seat,
                    Name = seat == 0 ? "Checker" : $"Rival {seat}",
                    Color = Colors[seat],
                    Driver = seat == 0 ? DriverKind.Human : DriverKind.Computer,
                    Skill = 0.7f + 0.1f * (float)random.NextDouble(),
                    Loadout = loadout,
                    Viewport = seat == 0 ? 0 : -1
                });
            }
            return setup;
        }

        private static string Fittings(CarLoadout loadout)
        {
            var parts = new List<string>();
            foreach (EquipmentSpec spec in Equipment.All)
            {
                if (loadout.Has(spec.Kind))
                {
                    parts.Add(spec.Id);
                }
            }
            return parts.Count > 0 ? string.Join("+", parts) : "no fittings";
        }

        /// <summary>
        /// The computer's driving with a quicker trigger: guns at anything ahead (the flamethrower only close), mines at
        /// anything behind, rockets into the cone, oil for a car behind, an EMP for a car near, a mortar for a car ahead.
        /// </summary>
        private static ICarDriver Pilot(RaceEntrant entrant, int seed)
        {
            var ai = new AiDriver(0.85f, unchecked(seed * 31 + entrant.Seat * 7919));
            float mineReady = 2f + entrant.Seat;
            float rocketReady = 3f + entrant.Seat * 1.5f;
            float nitroReady = 5f + entrant.Seat * 2f;
            float specialReady = 3f + entrant.Seat * 0.8f;
            float specialWaited = 0f;
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
                specialReady -= deltaTime;
                if (specialReady <= 0f)
                {
                    specialWaited += deltaTime;
                }
                GunSpec gun = car.Weapons != null ? car.Weapons.GunSpec : Guns.Of(GunKind.MachineGun);
                SpecialKind? next = car.Weapons != null ? car.Weapons.NextSpecial : null;
                foreach (Car other in car.Race.Cars)
                {
                    if (other == null || other == car || other.IsWrecked)
                    {
                        continue;
                    }
                    Vector3 local = car.transform.InverseTransformPoint(other.transform.position);
                    float reach = gun.IsFlame ? gun.Range : 55f;
                    float wide = gun.IsFlame ? 2f + local.z * 0.27f : 2.6f;
                    if (local.z > 2f && local.z < reach && Mathf.Abs(local.x) < wide)
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
                    if (specialReady <= 0f && next.HasValue)
                    {
                        bool suits = next.Value == SpecialKind.Oil ? local.z < -3f && local.z > -30f && Mathf.Abs(local.x) < 6f
                            : next.Value == SpecialKind.Emp ? local.magnitude < CombatRules.EmpRadius
                            : local.z > 12f && local.z < 60f && Mathf.Abs(local.x) < 10f;
                        if (suits)
                        {
                            input.Special = true;
                        }
                    }
                }
                // The check wants to see every special: one that found no use for a while is fired anyway.
                if (specialReady <= 0f && next.HasValue && specialWaited > 7f)
                {
                    input.Special = true;
                }
                if (input.Special)
                {
                    specialReady = 4f;
                    specialWaited = 0f;
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
                    if (info.Source != DamageSource.Gun && info.Source != DamageSource.Flame)
                    {
                        Write($"{watched.Time:0.0}s {victim.Entrant.Name} hit: {info} -> health {victim.Health.Health01:0.00}");
                    }
                };
                car.Wrecked += (wreck, cause) =>
                {
                    wrecks++;
                    kills += cause.Attacker != null ? 1 : 0;
                    wreckCauses.TryGetValue(cause.Source, out int count);
                    wreckCauses[cause.Source] = count + 1;
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
                if (car.Status != null)
                {
                    car.Status.Started += kind =>
                    {
                        statuses.TryGetValue(kind, out int count);
                        statuses[kind] = count + 1;
                        Car cause = victim.Status.Cause(kind);
                        Write($"{watched.Time:0.0}s {victim.Entrant.Name} {kind} by {(cause != null ? cause.Entrant.Name : "nobody")}");
                    };
                }
                if (car.Weapons != null)
                {
                    car.Weapons.OilDropped += slick => Special("oil", victim, watched);
                    car.Weapons.EmpFired += blast => Special($"emp ({blast.Caught.Count} caught)", victim, watched, "emp");
                    car.Weapons.MortarFired += shell => Special("mortar", victim, watched);
                }
            }
        }

        private void Special(string what, Car by, Race watched, string key = null)
        {
            key ??= what;
            fired.TryGetValue(key, out int count);
            fired[key] = count + 1;
            Write($"{watched.Time:0.0}s {by.Entrant.Name} fired {what}");
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
            else if (kind == "emp" && InView(at, 24f) && moments.Add("emp-ring"))
            {
                StartCoroutine(ShootSoon("emp-ring", 0.18f));
            }
            else if (kind == "plasma burst" && InView(at, 20f) && moments.Add("plasma-burst"))
            {
                StartCoroutine(ShootSoon("plasma-burst", 0.05f));
            }
        }

        private IEnumerator ShootSoon(string label, float delay)
        {
            yield return new WaitForSeconds(delay);
            Shoot(label, true);
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

        /// <summary>How long a moment has to last before it is shot: the loops need a moment to fill out.</summary>
        private static float Settle(string moment)
        {
            switch (moment)
            {
                case "flamethrower":
                case "welding":
                case "jammed":
                case "on-fire":
                case "slicked":
                case "minigun":
                    return 0.45f;
                default:
                    return 0f;
            }
        }

        /// <summary>The moments worth a screenshot in view now (each is taken once a race).</summary>
        private IEnumerable<string> MomentsInView()
        {
            foreach (Rocket rocket in Rocket.Active)
            {
                if (rocket != null && rocket.Age > 0.25f && InView(rocket.Position, 22f))
                {
                    yield return "rocket";
                    break;
                }
            }
            foreach (Mine mine in Mine.Active)
            {
                if (mine != null && mine.Armed && InView(mine.Position, 18f))
                {
                    yield return "mine";
                    break;
                }
            }
            foreach (OilSlick slick in OilSlick.Active)
            {
                if (slick != null && slick.Age > 0.5f && InView(slick.Position, 18f))
                {
                    yield return "oil-slick";
                    break;
                }
            }
            foreach (MortarShell shell in MortarShell.Active)
            {
                if (shell != null && shell.Progress > 0.35f && shell.Progress < 0.75f && InView(shell.Position, 26f))
                {
                    yield return "mortar-shell";
                    break;
                }
            }
            foreach (PlasmaBolt bolt in PlasmaBolt.Active)
            {
                if (bolt != null && bolt.Age > 0.04f && InView(bolt.Position, 20f))
                {
                    yield return "plasma-bolt";
                    break;
                }
            }
            foreach (Car car in race.Cars)
            {
                if (car == null || car.IsWrecked || !InView(car.transform.position, 20f))
                {
                    continue;
                }
                if (car.Health.Health01 < CombatRules.BurningAt)
                {
                    yield return "burning-wreck-to-be";
                }
                if (car.Weapons != null && car.Weapons.Firing && car.Weapons.GunSpec.IsFlame)
                {
                    yield return "flamethrower";
                }
                if (car.Weapons != null && car.Weapons.Firing && car.Weapons.Gun == GunKind.Minigun)
                {
                    yield return "minigun";
                }
                if (car.Status != null && car.Status.IsBurning)
                {
                    yield return "on-fire";
                }
                if (car.Status != null && car.Status.IsJammed)
                {
                    yield return "jammed";
                }
                if (car.Status != null && car.Status.IsSlicked)
                {
                    yield return "slicked";
                }
                if (car.Health.Welding)
                {
                    yield return "welding";
                }
            }
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
            if (moment && now - lastShot < 0.25f)
            {
                return false;
            }
            lastShot = now;
            shotsThisRace++;
            shot++;
            // The development console (opened by the services' start-up error of an unlinked project) would cover the HUD.
            Debug.developerConsoleVisible = false;
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

        private static float Value(string argument, float fallback)
        {
            return float.TryParse(MobilePlatform.ArgumentValue(argument), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture,
                out float value) ? value : fallback;
        }

        private static int Number(string argument, float fallback)
        {
            return Mathf.RoundToInt(Value(argument, fallback));
        }
    }
}
