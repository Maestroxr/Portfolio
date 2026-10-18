using Gamebox;
using UnityEngine;
using static Portfolio.Asteroids.AsteroidsText;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The field campaigns' half of the manager (AsteroidsGameManager.FieldCampaigns.cs): the freighters of an escort (one
    /// after the other, each across the sector) and the station of a defence, the objective's measure and the ally's hull
    /// on the HUD, the marked quarry of a hunt, the warnings of the new hazards, the calls of a boss's phases, and the
    /// failure when the freighter or station is lost. Only the simulator spawns the ally and counts the objective; the
    /// other pilots of a shared mission follow the ally's puppet, and a lost ally takes every pilot out (the room then
    /// fails, as when every ship is lost).
    /// </summary>
    public partial class AsteroidsGameManager
    {
        /// <summary>Seconds after the start before the first freighter sets off, and between two freighters.</summary>
        private const float FreighterDelay = 2.5f;

        private Ally ally;
        private float allyTimer;
        private float allyAlarm;
        private bool allyLost;
        private float restoredAllyHull = -1f;
        private Boss calledBoss;


        /// <summary>A mission was prepared: nothing of the last one's ally is left, and the campaign's music plays.</summary>
        private void PrepareFieldCampaign()
        {
            ally = null;
            allyTimer = FreighterDelay;
            allyAlarm = 0f;
            allyLost = false;
            calledBoss = null;
            if (objective != null)
            {
                objective.AllyHull = objective.HasAlly ? 1f : -1f;
            }
            // Every campaign after the first has a battle loop of its own.
            int arc = AsteroidsCampaign != null && !IsStrike && Mission != null && !Mission.IsEndless ? AsteroidsCampaign.ArcOf(LevelIndex) : 0;
            if (arc > 0 && sounds != null)
            {
                sounds.PlayMusic(sounds.FieldMusic(arc));
            }
        }


        /// <summary>Every frame of a running field mission, after the field moved.</summary>
        private void UpdateFieldCampaign(float deltaTime)
        {
            WatchBossPhases();
            if (objective == null || !objective.HasAlly)
            {
                return;
            }
            if (ally != null && (!ally.InPlay || ally.Field != field))
            {
                ally = null;
            }
            if (ally == null)
            {
                ally = FindAlly();
            }
            if (Simulates && ally == null && !allyLost && !objective.IsComplete && (phase == MissionPhase.Playing || phase == MissionPhase.Respawning || phase == MissionPhase.Watching))
            {
                allyTimer -= deltaTime;
                if (allyTimer <= 0f || objective.Type == LevelObjective.Defend)
                {
                    ally = LaunchAlly();
                }
            }
            objective.AllyHull = ally != null ? ally.HullFraction : allyLost ? 0f : 1f;
            if (ally == null)
            {
                return;
            }
            if (ally.IsWrecked)
            {
                AllyLost();
                return;
            }
            if (!Simulates)
            {
                return;
            }
            if (objective.Type == LevelObjective.Escort)
            {
                objective.EscortUnderWay(ally.Share);
            }
            else if (phase != MissionPhase.Victory && phase != MissionPhase.Defeat)
            {
                objective.Hold(deltaTime);
            }
            allyAlarm -= deltaTime;
        }


        /// <summary>The ally in play (a puppet on the clients that do not simulate), if any.</summary>
        private Ally FindAlly()
        {
            foreach (Ally candidate in Ally.Active)
            {
                if (candidate != null && candidate.InPlay && candidate.Field == field)
                {
                    return candidate;
                }
            }
            return null;
        }


        /// <summary>The simulator's next freighter (from one side of the sector to the other) or its station.</summary>
        private Ally LaunchAlly()
        {
            AsteroidsLevel mission = Mission;
            if (mission == null || spawner == null || Playground == null)
            {
                return null;
            }
            bool escort = objective.Type == LevelObjective.Escort;
            Ally launched = spawner.SpawnAlly(escort ? AllyKind.Freighter : AllyKind.Station, mission.AllyHull);
            if (launched == null)
            {
                return null;
            }
            Vector2 half = Playground.HalfSize;
            if (escort)
            {
                bool fromLeft = Random.value < 0.5f;
                float side = fromLeft ? -1f : 1f;
                float reach = half.x + launched.Radius + 1f;
                var from = new Vector2(side * reach, Random.Range(-half.y * 0.45f, half.y * 0.45f));
                var to = new Vector2(-side * reach, Mathf.Clamp(-from.y * 0.6f + Random.Range(-2f, 2f), -half.y * 0.55f, half.y * 0.55f));
                launched.Launch(from, to, mission.Crossing);
                ui?.Toast(objective.Escorted == 0 ? T("FREIGHTER INBOUND - escort it across") : T("NEXT FREIGHTER INBOUND"), new Color(0.5f, 0.9f, 1f));
            }
            else
            {
                launched.Anchor(new Vector2(0f, -half.y * 0.1f));
            }
            if (restoredAllyHull > 0f)
            {
                launched.Damage(launched.MaxHull - Mathf.Min(launched.MaxHull, restoredAllyHull));
                restoredAllyHull = -1f;
            }
            launched.Delivered += OnFreighterDelivered;
            launched.Damaged += OnAllyDamaged;
            return launched;
        }


        private void OnFreighterDelivered(Ally delivered)
        {
            if (!Simulates || objective == null)
            {
                return;
            }
            objective.FreighterDelivered();
            allyTimer = FreighterDelay;
            if (ally == delivered)
            {
                ally = null;
            }
            int bonus = Mathf.RoundToInt(1500f * delivered.HullFraction) + 1000;
            if (IsLocal)
            {
                squad.WaveCleared(bonus);
            }
            else
            {
                score.Add(bonus);
            }
            SyncScore();
            if (!objective.IsComplete)
            {
                ui?.Announce(T("FREIGHTER SAFE"), F("{0} of {1}", objective.Escorted, objective.Target), new Color(0.5f, 1f, 0.7f));
                sounds?.WaveClear();
            }
        }


        private void OnAllyDamaged(Ally hurt, float amount)
        {
            if (allyAlarm > 0f || !IsMissionActive)
            {
                return;
            }
            allyAlarm = 6f;
            bool freighter = hurt.Kind == AllyKind.Freighter;
            ui?.Toast(freighter ? T("FREIGHTER UNDER FIRE!") : T("STATION UNDER ATTACK!"), new Color(1f, 0.5f, 0.3f));
        }


        /// <summary>The freighter or station broke up: the mission is lost (in a shared mission, for every pilot).</summary>
        private void AllyLost()
        {
            if (allyLost)
            {
                return;
            }
            allyLost = true;
            objective.AllyHull = 0f;
            if (phase != MissionPhase.Playing && phase != MissionPhase.Respawning && phase != MissionPhase.Watching)
            {
                return;
            }
            bool freighter = ally != null && ally.Kind == AllyKind.Freighter;
            string title = freighter ? T("FREIGHTER LOST") : T("STATION LOST");
            if (IsCoop)
            {
                // Every pilot drops out, so the server fails the room as when the last ship is lost.
                if (phase != MissionPhase.Watching)
                {
                    phase = MissionPhase.Watching;
                    phaseTime = 0f;
                    online?.PilotOut(score.Score);
                }
                ui?.Announce(title, T("The mission has failed"), new Color(1f, 0.35f, 0.3f));
                sounds?.GameOver();
                return;
            }
            phase = MissionPhase.Defeat;
            phaseTime = 0f;
            director?.Stop();
            spawner?.ClearPending();
            ui?.Announce(title, T("The mission has failed"), new Color(1f, 0.35f, 0.3f));
            sounds?.GameOver();
        }


        /// <summary>A marked enemy went down: the hunt counts it (the simulator, whoever made the kill).</summary>
        private void CountQuarry(Shootable target)
        {
            if (!Simulates || objective == null || objective.Type != LevelObjective.Hunt || !(target is CampaignEnemy enemy) || !enemy.Marked || !IsMissionActive)
            {
                return;
            }
            objective.MarkedDown();
            if (!objective.IsComplete)
            {
                ui?.Toast(F("MARKED TARGET DOWN  {0} / {1}", objective.MarkedDestroyed, objective.Target), new Color(1f, 0.75f, 0.3f));
            }
        }


        /// <summary>The first sighting of a hazard of the campaigns.</summary>
        private void AnnounceFieldHazard(HazardKind kind)
        {
            switch (kind)
            {
                case HazardKind.SolarFlare:
                    ui?.Toast(MobilePlatform.UsesTouch ? T("SOLAR FLARE - DASH through the fire!") : T("SOLAR FLARE - dash through the fire (SHIFT)!"), new Color(1f, 0.6f, 0.2f));
                    break;
                case HazardKind.IonStorm:
                    ui?.Toast(T("ION STORM - stay out of the cloud"), new Color(0.55f, 0.7f, 1f));
                    break;
                case HazardKind.Sentry:
                    ui?.Toast(T("SENTRY GUN - it glows before it fires"), new Color(1f, 0.6f, 0.3f));
                    break;
                case HazardKind.Minelayer:
                    ui?.Toast(T("MINELAYER - stop it before it seeds the sector"), new Color(1f, 0.85f, 0.3f));
                    break;
                case HazardKind.Stalker:
                    ui?.Toast(T("PHASE STALKER - shoot it while it shows"), new Color(0.8f, 0.55f, 1f));
                    break;
                case HazardKind.Corsair:
                    ui?.Toast(objective != null && objective.HasAlly ? T("CORSAIRS - they go for the convoy") : T("CORSAIRS INBOUND"), new Color(1f, 0.4f, 0.4f));
                    break;
            }
        }


        /// <summary>The calls a boss of the campaigns makes as it enters a phase.</summary>
        private void WatchBossPhases()
        {
            if (boss == calledBoss)
            {
                return;
            }
            calledBoss = boss;
            if (boss != null)
            {
                boss.PhaseChanged += OnFieldBossPhase;
            }
        }


        private void OnFieldBossPhase(Boss source, int newPhase)
        {
            string call = source != null ? source.PhaseCall(newPhase) : string.Empty;
            if (!string.IsNullOrEmpty(call))
            {
                ui?.Announce(T(call).ToUpperInvariant(), string.Empty, new Color(1f, 0.55f, 0.3f));
            }
        }


        // ------------------------------------------------------------------ saved missions

        private void SaveFieldCampaign(IStorageStrategy disk)
        {
            disk.SetFloat(SaveKey("AllyHull"), ally != null && ally.InPlay ? ally.Hull : -1f);
        }


        private void LoadFieldCampaign(IStorageStrategy disk)
        {
            restoredAllyHull = disk.DoesKeyExist(SaveKey("AllyHull")) ? disk.GetFloat(SaveKey("AllyHull")) : -1f;
            allyTimer = 0.5f;
        }
    }
}
