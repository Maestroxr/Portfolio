using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Gamebox;
using Gamebox.UI;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The tour's steps for the field campaigns (AsteroidsTour.FieldCampaigns.cs):
    /// <c>fly:&lt;index&gt;</c> flies the field mission at that campaign index to its end with the autopilot (victory, defeat
    /// or the timeout), photographing it as it goes (the start, every 25 seconds, the boss's arrival and phases, the
    /// results) and logging its status and outcome; with <c>-asteroids-tour-god</c> the ship keeps its hull and shield.
    /// <c>fly:arcs</c> flies every mission of the field campaigns after the first, in order. <c>fly:local&lt;index&gt;</c>
    /// flies it as a local co-op match of two pilots, both on the autopilot.
    /// <c>gallery:&lt;index&gt;</c> starts the mission at that index, stops its waves and shows, one by one in a quiet field,
    /// every rock, hazard, enemy, ally and beam of the campaigns and every boss of their missions, a screenshot each.
    /// </summary>
    public partial class AsteroidsTour
    {
        /// <summary>Seconds between two screenshots of a field mission in flight.</summary>
        private const float FlyShotInterval = 25f;


        /// <summary>The field missions of every campaign after the first, in campaign order.</summary>
        private List<int> CampaignFieldMissions()
        {
            var missions = new List<int>();
            AsteroidsCampaign campaign = manager.AsteroidsCampaign;
            if (campaign == null)
            {
                return missions;
            }
            for (int arc = 1; arc < campaign.ArcCount(MissionMode.Field); arc++)
            {
                missions.AddRange(campaign.MissionsOf(MissionMode.Field, arc));
            }
            return missions;
        }


        private IEnumerator FlyStep(string argument)
        {
            if (argument == "arcs")
            {
                foreach (int index in CampaignFieldMissions())
                {
                    yield return Fly(index, false);
                }
                yield break;
            }
            if (argument.StartsWith("local"))
            {
                yield return Fly(ParseInt(argument.Substring(5), 0), true);
                yield break;
            }
            yield return Fly(ParseInt(argument, 0), false);
        }


        /// <summary>Starts the mission at <paramref name="index"/> as a local co-op match of two pilots; false if it did not.</summary>
        private bool StartLocalMatch(int index)
        {
            manager.SelectMission(index);
            manager.OpenLocalPlay();
            LocalPlayUI setup = LocalPlayUI.For(manager);
            if (!setup.IsOpen || setup.Match == null)
            {
                Note($"FAIL the local play setup did not open over {(manager.Mission != null ? manager.Mission.Title : "-")}");
                return false;
            }
            setup.Match.Count = 2;
            // Nobody's keys: both ships fly on the autopilot.
            ControlInput.Source = new ScriptedInput();
            if (!setup.StartMatch())
            {
                Note($"FAIL the local match did not start: {setup.Match.Problem()}");
                ControlInput.Source = null;
                setup.Hide();
                return false;
            }
            return true;
        }


        private IEnumerator Fly(int index, bool local)
        {
            AsteroidsLevel mission = manager.AsteroidsCampaign != null ? manager.AsteroidsCampaign.Mission(index) : null;
            if (mission == null || mission.Mode != MissionMode.Field || mission.IsEndless)
            {
                Note($"no field campaign mission at {index}");
                yield break;
            }
            string name = local ? $"localfly{index}" : $"fly{index}";
            yield return ToMenu();
            // Every flight on the starter hull: the saved hangar choice is shared with other development players.
            int hull = manager.Progress != null ? manager.Progress.SelectedShip : 0;
            manager.SelectShip(0);
            Note($"{name} hull: was {hull}, flies {(manager.Progress != null ? manager.Progress.SelectedShip : 0)}");
            if (local)
            {
                BackupLocalPlay();
                manager.SelectMode(MissionMode.Field);
                yield return Wait(0.5f);
            }
            if (local ? !StartLocalMatch(index) : !StartMission(index))
            {
                yield break;
            }
            yield return Wait(1.2f);
            yield return Shot($"{name}_briefing");
            yield return WaitUntil(() => manager.IsMissionActive || Ended, 12f, "mission start");
            if (!manager.IsMissionActive)
            {
                Note($"FAIL the field mission {index} did not start");
                manager.ReturnToMissionSelect();
                yield break;
            }
            Note($"{name} started: {mission.Title}, objective {mission.Objective} {mission.ObjectiveTarget}, sector {mission.Sector}, arc {manager.AsteroidsCampaign.ArcOf(index)}, " +
                 $"score goal {mission.ScoreGoal}" + (local ? $", in local match {manager.InLocalMatch}, {DescribePilots()}" : string.Empty));
            if (local)
            {
                SetLocalAutopilots(true, false, 0);
            }
            else
            {
                SetAutopilot(true, false);
            }
            float started = Time.unscaledTime;
            float nextStatus = 0f;
            float nextShot = 6f;
            int flyShots = 0;
            // What hurts the ship, by source, for judging the difficulty.
            var hurt = new Dictionary<string, float>();
            void OnHurt(DamageInfo hit)
            {
                string key = hit.Shot != null ? hit.Shot.name.Replace("(Clone)", string.Empty) : hit.Source.ToString();
                if (hit.Source == DamageSource.Collision && manager.Field != null)
                {
                    // What it ran into: the nearest body to the point of contact.
                    Shootable into = manager.Field.NearestTarget(hit.Point, 6f, target => target.IsAlive);
                    key += into is Asteroid rock ? $"/{rock.Kind}" : into != null ? $"/{into.GetType().Name}" : "/?";
                }
                hurt[key] = (hurt.TryGetValue(key, out float sum) ? sum : 0f) + hit.Amount;
            }
            AsteroidsPlayer hurtShip = manager.Ship;
            if (hurtShip != null)
            {
                hurtShip.Damaged += OnHurt;
            }
            Boss seenBoss = null;
            int bossPhase = 0;
            int livesLost = 0;
            int lastLives = manager.Lives;
            while (!Ended && Time.unscaledTime - started < timeout)
            {
                Time.timeScale = speed;
                KeepShipAlive();
                if (manager.Lives < lastLives)
                {
                    livesLost += lastLives - manager.Lives;
                }
                lastLives = manager.Lives;
                float elapsed = Time.unscaledTime - started;
                if (elapsed >= nextStatus)
                {
                    nextStatus = elapsed + 5f;
                    Note(FieldStatus() + (local ? $", {DescribePilots()}" : string.Empty));
                }
                if (elapsed >= nextShot && flyShots < 8)
                {
                    nextShot = elapsed + FlyShotInterval;
                    flyShots++;
                    yield return Shot($"{name}_flying_{flyShots}");
                }
                Boss boss = manager.ActiveBoss;
                if (boss != null && boss != seenBoss)
                {
                    seenBoss = boss;
                    bossPhase = 0;
                    Note($"boss arrived: {boss.DisplayName} after {elapsed:0.0} s");
                    yield return Wait(2.5f);
                    yield return Shot($"{name}_boss");
                }
                if (boss != null && boss.Phase > bossPhase)
                {
                    bossPhase = boss.Phase;
                    Note($"boss phase {bossPhase}: {boss.BarFraction:0.00} left after {elapsed:0.0} s");
                    yield return Wait(1.2f);
                    yield return Shot($"{name}_boss_phase{bossPhase}");
                }
                yield return null;
            }
            Time.timeScale = 1f;
            if (hurtShip != null)
            {
                hurtShip.Damaged -= OnHurt;
                var parts = new List<string>();
                foreach (KeyValuePair<string, float> entry in hurt)
                {
                    parts.Add($"{entry.Key} {entry.Value:0}");
                }
                Note($"{name} damage taken: {string.Join(", ", parts)}");
            }
            if (local)
            {
                SetLocalAutopilots(false, false, 0);
            }
            else
            {
                SetAutopilot(false, false);
            }
            if (!Ended)
            {
                Note($"TIMEOUT {name} after {timeout} s: {FieldStatus()}");
                manager.ReturnToMissionSelect();
                ControlInput.Source = null;
                yield return Wait(1f);
                yield break;
            }
            bool won = manager.State.Is(BaseGameState.Victory);
            Note($"{name} outcome: {(won ? "VICTORY" : "DEFEAT")} after {Time.unscaledTime - started:0.0} s, score {manager.Scoring.Score} (goal {mission.ScoreGoal}), " +
                 $"lives lost {livesLost}, stars {manager.Progress.Stars(index)}, kills {manager.Scoring.Kills}, {FieldStatus()}");
            yield return Wait(2f);
            yield return Shot($"{name}_results");
            manager.ReturnToMissionSelect();
            ControlInput.Source = null;
            yield return Wait(1f);
            if (local)
            {
                Note($"{name} back at the title: in local match {manager.InLocalMatch}, wingmen {manager.Field.Wingmen.Count}");
            }
        }


        /// <summary>God mode in the asteroid field: the hull and the shield of every local ship stay full.</summary>
        private void KeepShipAlive()
        {
            if (!god)
            {
                return;
            }
            SpaceField field = manager.Field;
            for (int i = 0; field != null && i < field.LocalShipCount; i++)
            {
                AsteroidsPlayer ship = field.LocalShip(i);
                if (ship != null && ship.IsAlive)
                {
                    ship.Repair(ship.MaxHealth);
                    ship.RestoreShield(ship.MaxShield);
                }
            }
        }


        private string FieldStatus()
        {
            SpaceField field = manager.Field;
            MissionObjective objective = manager.Objective;
            Boss boss = manager.ActiveBoss;
            Ally ally = null;
            foreach (Ally candidate in Ally.Active)
            {
                ally = candidate != null && candidate.InPlay ? candidate : ally;
            }
            return string.Format(CultureInfo.InvariantCulture,
                "status: wave {0}, objective {1} ({2:0.00}), score {3}, lives {4}, targets {5}, enemy shots {6}, bodies {7}, boss {8}, ally {9}",
                manager.Director != null ? manager.Director.WaveNumber : 0, objective != null ? objective.Status(manager.Director != null ? manager.Director.WaveNumber : 1) : "-",
                objective != null ? objective.Progress : 0f, manager.Scoring.Score, manager.Lives, field != null ? field.Targets.Count : 0,
                field != null ? field.EnemyShots.Count : 0, field != null ? field.Bodies.Count : 0,
                boss != null ? $"{boss.DisplayName} {boss.BarFraction:0.00} phase {boss.Phase}" : "-",
                ally != null ? $"{ally.Kind} {ally.Hull:0}/{ally.MaxHull:0} share {ally.Share:0.00}" : "-");
        }


        // ------------------------------------------------------------------ the gallery

        private IEnumerator Gallery(int index)
        {
            AsteroidsLevel mission = manager.AsteroidsCampaign != null ? manager.AsteroidsCampaign.Mission(index) : null;
            if (mission == null || mission.Mode != MissionMode.Field)
            {
                Note($"gallery: no field mission at {index}");
                yield break;
            }
            yield return ToMenu();
            if (!StartMission(index))
            {
                yield break;
            }
            yield return WaitUntil(() => manager.IsMissionActive || Ended, 12f, "mission start");
            manager.Director?.Stop();
            SpaceField field = manager.Field;
            SpawnService spawner = manager.spawner;
            AsteroidsPlayer ship = manager.Ship;
            bool keepGod = god;
            god = true;

            IEnumerator Quiet()
            {
                field.Clear();
                ship.Position = new Vector2(-manager.Playground.HalfSize.x * 0.75f, -manager.Playground.HalfSize.y * 0.6f);
                ship.transform.rotation = Quaternion.Euler(0f, 0f, -45f);
                yield return Wait(0.3f);
            }

            IEnumerator Hold(string shot, float seconds)
            {
                float until = Time.unscaledTime + seconds;
                while (Time.unscaledTime < until)
                {
                    KeepShipAlive();
                    yield return null;
                }
                yield return Shot(shot);
            }

            // The rocks: every size of every new kind in a row.
            yield return Quiet();
            float x = -9f;
            foreach (AsteroidKind kind in new[] { AsteroidKind.Metal, AsteroidKind.Hydra, AsteroidKind.Wreck })
            {
                for (int s = 2; s >= 0; s--)
                {
                    Asteroid rock = spawner.SpawnAsteroid(kind, (AsteroidSize)s, new Vector2(x + (2 - s) * 3.2f, 4.5f - ((int)kind - 5) * 3.5f), Vector2.zero, false);
                    if (rock != null)
                    {
                        rock.Velocity = Vector2.zero;
                    }
                }
            }
            yield return Hold("gallery_rocks", 1.5f);

            // The enemies and the allies.
            yield return Quiet();
            foreach (HazardKind kind in new[] { HazardKind.Sentry, HazardKind.Minelayer, HazardKind.Corsair, HazardKind.Stalker })
            {
                spawner.SpawnHazard(kind);
            }
            Ally freighter = spawner.SpawnAlly(AllyKind.Freighter, 120f);
            freighter?.Launch(new Vector2(-6f, 2f), new Vector2(30f, 2f), 200f);
            Ally station = spawner.SpawnAlly(AllyKind.Station, 200f);
            station?.Anchor(new Vector2(6f, -3f));
            yield return Hold("gallery_enemies", 2.5f);
            yield return Hold("gallery_enemies_later", 2f);

            // The hazards: a flare's warning and its wall, a storm and its lightning.
            yield return Quiet();
            spawner.SpawnFlare(SolarFlare.Side.Left);
            spawner.SpawnHazard(HazardKind.IonStorm);
            yield return Hold("gallery_flare_warning", 1.2f);
            yield return Hold("gallery_flare_wall", 1.6f);
            yield return Hold("gallery_storm", 2f);

            // The beams and the orbs.
            yield return Quiet();
            spawner.FireEnemyShot(EnemyShotKind.Lance, new Vector2(-12f, 5f), new Vector2(1f, -0.2f).normalized);
            spawner.FireEnemyShot(EnemyShotKind.SweepBeam, new Vector2(10f, -6f), Vector2.up * FieldBeam.ClockwiseSpeed);
            spawner.FireEnemyShot(EnemyShotKind.Lightning, new Vector2(2f, 3f), Vector2.zero);
            spawner.FireEnemyShot(EnemyShotKind.NovaOrb, new Vector2(-4f, -2f), new Vector2(0.5f, 0.2f));
            yield return Hold("gallery_beams_warning", 0.5f);
            yield return Hold("gallery_beams_burning", 0.5f);

            // Every boss of the campaigns after the first.
            var bosses = new List<Boss>();
            foreach (int mission2 in CampaignFieldMissions())
            {
                Boss prefab = manager.AsteroidsCampaign.Mission(mission2).BossPrefab;
                if (prefab != null && !bosses.Contains(prefab))
                {
                    bosses.Add(prefab);
                }
            }
            foreach (Boss prefab in bosses)
            {
                yield return Quiet();
                Boss boss = spawner.SpawnBoss(prefab);
                if (boss == null)
                {
                    Note($"gallery: {prefab.name} did not spawn");
                    continue;
                }
                Note($"gallery: {boss.DisplayName}");
                yield return Hold($"gallery_boss_{prefab.name}", boss.entrySeconds + 0.5f);
                yield return Hold($"gallery_boss_{prefab.name}_fight", 4f);
                if (boss.InPlay)
                {
                    boss.Despawn();
                }
            }
            god = keepGod;
            field.Clear();
            manager.ReturnToMissionSelect();
            yield return Wait(1f);
        }
    }
}
