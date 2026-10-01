using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Gamebox;
using Gamebox.UI;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The local co-op steps of the tour: <c>localsetup</c> (the title with its Local Play button, the shared setup for
    /// three pilots and the controls page), <c>localfield</c> (the first open asteroid field mission flown by three pilots:
    /// pilot 1 on its own keys through a <see cref="ScriptedInput"/>, the wingmen by the autopilot; the objective is
    /// completed by hand after a while if the pilots have not done it; then Retry, and back to the title) and
    /// <c>localstrike</c> (the first strike mission, every ship on the strike autopilot, skipped to the boss). <c>local</c>
    /// runs all three. The remembered setup and controls of local play are put back when the tour ends.
    /// </summary>
    public partial class AsteroidsTour
    {
        /// <summary>Seconds a local asteroid field mission is flown before the tour completes its objective by hand.</summary>
        private const float LocalFieldTime = 40f;

        private string savedLocalSetup;
        private string savedLocalControls;
        private bool localBackedUp;


        private IEnumerator LocalSetup(bool strike)
        {
            BackupLocalPlay();
            yield return ToMenu();
            manager.SelectMode(strike ? MissionMode.Strike : MissionMode.Field);
            yield return Wait(0.5f);
            if (strike)
            {
                List<int> strikes = StrikeMissions();
                if (strikes.Count > 0)
                {
                    manager.SelectMission(strikes[0]);
                }
            }
            yield return Wait(1f);
            string mode = strike ? "strike" : "field";
            yield return Shot($"local_title_{mode}");
            manager.OpenLocalPlay();
            yield return Wait(0.8f);
            LocalPlayUI setup = LocalPlayUI.For(manager);
            if (!setup.IsOpen || setup.Match == null)
            {
                Note($"FAIL the local play setup did not open over {(manager.Mission != null ? manager.Mission.Title : "-")}");
                yield break;
            }
            setup.Match.Count = 3;
            setup.ShowSetup();
            Note($"local setup ({mode}): {manager.Mission.Title}, players {setup.Match.Count}, options " +
                 string.Join(", ", setup.Match.Rules.Options.Select(option => $"{option.Key}={setup.Match.Option(option.Key)}")) +
                 $", problem {setup.Match.Problem() ?? "none"}");
            yield return Wait(0.6f);
            yield return Shot($"local_setup_{mode}");
            if (!strike)
            {
                setup.ShowControls(0);
                yield return Wait(0.6f);
                yield return Shot("local_controls_pilot1");
                setup.ShowControls(2);
                yield return Wait(0.6f);
                yield return Shot("local_controls_pilot3");
                setup.ShowSetup();
                yield return Wait(0.3f);
            }
        }


        private IEnumerator LocalField()
        {
            yield return LocalSetup(false);
            LocalPlayUI setup = LocalPlayUI.For(manager);
            if (!setup.IsOpen)
            {
                yield break;
            }
            var keys = new ScriptedInput();
            ControlInput.Source = keys;
            if (!setup.StartMatch())
            {
                Note($"FAIL the local match did not start: {setup.Match.Problem()}");
                ControlInput.Source = null;
                yield break;
            }
            Note($"local match begun: in local match {manager.InLocalMatch}, local {manager.IsLocal}, pilots {manager.LocalPilots.Count}, " +
                 $"wingmen {manager.Field.Wingmen.Count}, squad {(manager.Squad != null ? manager.Squad.Count.ToString() : "none")}, phase state {manager.State.BaseState}");
            yield return WaitUntil(() => manager.IsMissionActive || Ended, 12f, "the local mission to start");
            Note($"local field started: {manager.Mission.Title}, in local match {manager.InLocalMatch}, {DescribePilots()}");
            SetLocalAutopilots(true, false, 1);
            AsteroidsPlayer first = manager.Ship;
            float heading = first.transform.eulerAngles.z;
            float started = Time.unscaledTime;
            float nextStatus = 0f;
            int shotsTaken = 0;
            bool wonByHand = false;
            while (!Ended && Time.unscaledTime - started < LocalFieldTime + 30f)
            {
                Time.timeScale = speed;
                float elapsed = Time.unscaledTime - started;
                // Pilot 1 on its own keys (WASD, Space): fires all the time, turns left and right and thrusts in bursts.
                keys.EndFrame();
                Hold(keys, KeyCode.Space, true);
                Hold(keys, KeyCode.A, Mathf.Repeat(elapsed, 4f) < 1.2f);
                Hold(keys, KeyCode.D, Mathf.Repeat(elapsed, 4f) >= 2f && Mathf.Repeat(elapsed, 4f) < 2.8f);
                Hold(keys, KeyCode.W, Mathf.Repeat(elapsed, 3f) < 0.8f);
                if (elapsed > 1.5f && heading >= 0f)
                {
                    Note($"pilot 1 on its keys: heading {heading:0} -> {first.transform.eulerAngles.z:0}, at {first.Position}");
                    heading = -1f;
                }
                if (elapsed >= nextStatus)
                {
                    nextStatus = elapsed + 6f;
                    Note($"local field {elapsed:0} s: {DescribePilots()}");
                }
                if (shotsTaken == 0 && elapsed > 5f || shotsTaken == 1 && elapsed > 16f)
                {
                    shotsTaken++;
                    yield return Shot($"local_field_play{shotsTaken}");
                    if (shotsTaken == 1)
                    {
                        // Escape pauses the local mission (no saving offered) and resumes it.
                        manager.Back();
                        yield return Wait(0.6f);
                        Note($"paused: state {manager.State.BaseState}");
                        yield return Shot("local_field_paused");
                        manager.Back();
                        yield return Wait(0.3f);
                        Note($"resumed: state {manager.State.BaseState}");
                    }
                }
                if (!wonByHand && elapsed > LocalFieldTime && manager.Objective != null && manager.IsMissionActive)
                {
                    wonByHand = true;
                    Note("the pilots have not finished the mission yet: the tour completes its objective by hand");
                    Complete(manager.Objective);
                }
                yield return null;
            }
            Time.timeScale = 1f;
            keys.ReleaseAll();
            SetLocalAutopilots(false, false, 1);
            Note($"local field outcome: {manager.State.BaseState}, {DescribePilots()}");
            yield return Wait(2.5f);
            yield return Shot("local_field_results");

            // Retry flies it again with the same pilots; the mission select ends the match.
            manager.RetryMission();
            yield return WaitUntil(() => manager.IsMissionActive || Ended, 12f, "the retried mission to start");
            Note($"retry: in local match {manager.InLocalMatch}, {DescribePilots()}");
            yield return Wait(1.5f);
            yield return Shot("local_field_retry");
            manager.ReturnToMissionSelect();
            yield return Wait(1f);
            Note($"back at the title: in local match {manager.InLocalMatch}, wingmen {manager.Field.Wingmen.Count}, ship seat {manager.Ship.Seat}, " +
                 $"ship controls {manager.Ship.Input.GetType().Name}");
            ControlInput.Source = null;
        }


        private IEnumerator LocalStrike()
        {
            yield return LocalSetup(true);
            LocalPlayUI setup = LocalPlayUI.For(manager);
            if (!setup.IsOpen || !manager.IsStrike)
            {
                Note("no strike mission to fly together");
                setup.Hide();
                yield break;
            }
            // Nobody's keys: every ship flies on the strike autopilot.
            ControlInput.Source = new ScriptedInput();
            int walletBefore = manager.Pilot.Money;
            if (!setup.StartMatch())
            {
                Note($"FAIL the local match did not start: {setup.Match.Problem()}");
                ControlInput.Source = null;
                yield break;
            }
            yield return Wait(1.2f);
            yield return Shot("local_strike_briefing");
            yield return WaitUntil(() => manager.IsMissionActive || Ended, 12f, "the local strike mission to start");
            Note($"local strike started: {manager.Mission.Title}, difficulty {manager.MissionDifficulty}, {DescribePilots()}");
            SetLocalAutopilots(true, true, 0);
            bool noBoss = !(manager.Mission is StrikeLevel flown) || flown.StrikeBossPrefab == null;
            float started = Time.unscaledTime;
            float nextStatus = 0f;
            bool skipped = false;
            bool played = false;
            bool bossSeen = false;
            bool wonByHand = false;
            int bossShots = 0;
            float nextBossShot = 0f;
            while (!Ended && Time.unscaledTime - started < timeout)
            {
                Time.timeScale = speed;
                KeepLocalPilotsAlive();
                float elapsed = Time.unscaledTime - started;
                if (!played && elapsed > 6f)
                {
                    played = true;
                    yield return Shot("local_strike_play");
                }
                if (!skipped && elapsed > 9f)
                {
                    skipped = true;
                    manager.SkipToBoss();
                    Note($"skip to boss: distance {manager.ScrollDistance:0.0}");
                }
                if (noBoss && !wonByHand && elapsed > 14f && manager.Objective != null)
                {
                    wonByHand = true;
                    Note("the mission has no boss: the tour wins it by hand");
                    manager.Objective.Defeat();
                }
                if (elapsed >= nextStatus)
                {
                    nextStatus = elapsed + 6f;
                    Note($"local strike {elapsed:0} s: {Status()}; {DescribePilots()}");
                }
                if (!bossSeen && manager.ActiveBoss != null)
                {
                    bossSeen = true;
                    nextBossShot = elapsed + 5f;
                    Note($"boss arrived: {manager.ActiveBoss.DisplayName}");
                }
                if (bossSeen && bossShots < 2 && manager.ActiveBoss != null && elapsed >= nextBossShot)
                {
                    bossShots++;
                    nextBossShot = elapsed + 10f;
                    yield return Shot($"local_strike_boss{bossShots}");
                }
                yield return null;
            }
            Time.timeScale = 1f;
            SetLocalAutopilots(false, true, 0);
            if (!Ended)
            {
                Note($"TIMEOUT local strike after {timeout} s: {Status()}");
                manager.ReturnToMissionSelect();
                ControlInput.Source = null;
                yield break;
            }
            Note($"local strike outcome: {manager.State.BaseState}, landed {manager.HasLanded}, wallet {walletBefore} -> {manager.Pilot.Money}, " +
                 $"pilot {Describe(manager.Pilot)}, {DescribePilots()}");
            yield return Wait(3f);
            yield return Shot("local_strike_results");
            // Escape at the results goes back to the mission select, which ends the match.
            manager.Back();
            yield return Wait(1f);
            Note($"back at the title: in local match {manager.InLocalMatch}, wingmen {manager.Field.Wingmen.Count}");
            ControlInput.Source = null;
        }


        private static void Hold(ScriptedInput keys, KeyCode key, bool held)
        {
            if (held)
            {
                keys.Press(key);
            }
            else
            {
                keys.Release(key);
            }
        }


        /// <summary>The objective of a field mission is met at once (the tour's way to the victory and the results).</summary>
        private static void Complete(MissionObjective objective)
        {
            for (int i = 0; i < 100 && !objective.IsComplete; i++)
            {
                switch (objective.Type)
                {
                    case LevelObjective.Survive:
                        objective.Survive(objective.Target);
                        break;
                    case LevelObjective.Collect:
                        objective.CrystalCollected();
                        break;
                    case LevelObjective.Boss:
                        objective.Defeat();
                        break;
                    default:
                        objective.WaveCleared();
                        break;
                }
            }
        }


        /// <summary>The autopilot flies the ships of the pilots from <paramref name="from"/> on (0: every ship).</summary>
        private void SetLocalAutopilots(bool on, bool strike, int from)
        {
            IReadOnlyList<AsteroidsGameManager.LocalPilot> pilots = manager.LocalPilots;
            for (int i = from; i < pilots.Count; i++)
            {
                AsteroidsPlayer ship = pilots[i].Ship;
                if (ship == null)
                {
                    continue;
                }
                if (strike && ship.TryGetComponent(out StrikeAutopilot strikePilot))
                {
                    strikePilot.enabled = on;
                }
                else if (!strike && ship.TryGetComponent(out AsteroidsAutopilot fieldPilot))
                {
                    fieldPilot.enabled = on;
                }
            }
        }


        /// <summary>God mode: every local strike pilot's energy stays full.</summary>
        private void KeepLocalPilotsAlive()
        {
            if (!god)
            {
                return;
            }
            foreach (AsteroidsGameManager.LocalPilot pilot in manager.LocalPilots)
            {
                StrikeLoadout loadout = pilot.Working;
                if (loadout != null && pilot.Ship != null && pilot.Ship.IsAlive && loadout.Energy < StrikeRules.MaxEnergy)
                {
                    loadout.Energy = StrikeRules.MaxEnergy;
                    loadout.NotifyChanged();
                }
            }
        }


        private string DescribePilots()
        {
            LocalSquad squad = manager.Squad;
            if (squad == null)
            {
                return "no squad";
            }
            IEnumerable<string> pilots = manager.LocalPilots.Select(pilot =>
                $"{pilot.Seat.Name} [seat {pilot.Ship.Seat}, {(pilot.Ship.IsAlive ? "flying" : "down")} at {pilot.Ship.Position}, " +
                $"score {pilot.Record?.Score.Score}, kills {pilot.Record?.Score.Kills}, ships {pilot.Record?.Lives}{(pilot.Record != null && pilot.Record.Out ? ", OUT" : "")}, " +
                $"input {pilot.Ship.Input.GetType().Name}]");
            return $"squad {squad.TeamScore}: " + string.Join("; ", pilots);
        }


        /// <summary>The remembered local play setup and controls, to put back when the tour ends.</summary>
        private void BackupLocalPlay()
        {
            if (localBackedUp)
            {
                return;
            }
            localBackedUp = true;
            savedLocalSetup = PlayerPrefs.HasKey(LocalMatch.Key(GameType.Asteroids)) ? PlayerPrefs.GetString(LocalMatch.Key(GameType.Asteroids)) : null;
            savedLocalControls = PlayerPrefs.HasKey(ControlBindings.Key(GameType.Asteroids)) ? PlayerPrefs.GetString(ControlBindings.Key(GameType.Asteroids)) : null;
        }


        private void RestoreLocalPlay()
        {
            if (!localBackedUp)
            {
                return;
            }
            localBackedUp = false;
            Put(LocalMatch.Key(GameType.Asteroids), savedLocalSetup);
            Put(ControlBindings.Key(GameType.Asteroids), savedLocalControls);
            PlayerPrefs.Save();
        }


        private static void Put(string key, string value)
        {
            if (value == null)
            {
                PlayerPrefs.DeleteKey(key);
            }
            else
            {
                PlayerPrefs.SetString(key, value);
            }
        }
    }
}
