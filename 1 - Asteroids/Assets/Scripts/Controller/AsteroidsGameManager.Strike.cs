using System.Collections.Generic;
using Gamebox;
using UnityEngine;
using static Portfolio.Asteroids.AsteroidsText;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The strike half of the manager: the planet strike missions (a vertical scroller over the ground, see
    /// <see cref="StrikeLevel"/>), the pilot and the Supply Room, the strike tab of the mission select, the strike HUD and
    /// results. The main file calls in here at its branch points whenever <see cref="IsStrike"/>; the asteroid field
    /// missions never reach this code.
    /// </summary>
    public partial class AsteroidsGameManager
    {
        /// <summary>How fast the ground of a previewed mission drifts behind the mission select (m/s).</summary>
        private const float PreviewScrollSpeed = 1.4f;

        /// <summary>The previewed ground loops back after this far (m).</summary>
        private const float PreviewLoop = 120f;

        /// <summary>A co-op simulator tells the others its scroll this often (s).</summary>
        private const float ScrollReportInterval = 0.25f;

        /// <summary>Energy at or below which, without a phase shield, the HUD warns SHIELD LOW.</summary>
        private const float ShieldLowEnergy = 25f;

        /// <summary>Runs the scroll and the events of a strike mission (the wave director is null in strike).</summary>
        private ScrollDirector scroll;

        /// <summary>The working copy of the pilot a strike mission flies with; replaces the saved pilot only after a win.</summary>
        private StrikeLoadout working;

        /// <summary>The saved pilot (loaded from the progress when first needed).</summary>
        private StrikeLoadout pilot;

        private MissionMode menuMode = MissionMode.Field;
        private int fieldMenuMission = -1;
        private int strikeMenuMission = -1;
        private float previewDistance;
        private int hostilesEntered;
        private int hostilesDestroyed;
        private float damageTaken;
        private int walletAtStart;
        private float scrollReportTimer;
        private bool shieldLowWarned;
        private bool flyingOff;
        private float flyOffTime;
        private Vector2 flyOffFrom;
        private bool landed;
        private StrikeGunnery gunnery;

        /// <summary>Whether the current (or previewed) mission is a planet strike mission.</summary>
        public bool IsStrike => Mission != null && Mission.Mode == MissionMode.Strike;

        /// <summary>The tab of the mission select: the asteroid field or the planet strike.</summary>
        public MissionMode MenuMode => menuMode;

        /// <summary>The saved pilot of the strike campaign.</summary>
        public StrikeLoadout Pilot => pilot ??= progress != null ? progress.LoadPilot() : StrikeLoadout.NewPilot();

        /// <summary>The loadout the current strike mission flies with; null outside a strike mission.</summary>
        public StrikeLoadout Working => working;

        /// <summary>The scroll director of the current strike mission (on the simulator); null otherwise.</summary>
        public ScrollDirector Scroll => scroll;

        /// <summary>How far the ground has scrolled in the current strike mission (m).</summary>
        public float ScrollDistance => field != null ? field.ScrollDistance : 0f;

        /// <summary>How far the current strike mission got toward its boss, 0 to 1.</summary>
        public float ScrollProgress
        {
            get
            {
                if (scroll != null)
                {
                    return scroll.Progress;
                }
                return Mission is StrikeLevel level && level.BossAt > 0f ? Mathf.Clamp01(ScrollDistance / level.BossAt) : 0f;
            }
        }

        /// <summary>Strike hostiles (aircraft and ground units) that came on screen in this mission.</summary>
        public int HostilesEntered => hostilesEntered;

        /// <summary>Strike hostiles destroyed in this mission.</summary>
        public int HostilesDestroyed => hostilesDestroyed;

        /// <summary>Damage the pilot took in this mission (phase shields and energy, after the multipliers).</summary>
        public float DamageTaken => damageTaken;

        /// <summary>Whether the local ship of a won strike mission has flown off the top and landed (its pilot is saved).</summary>
        public bool HasLanded => landed;

        /// <summary>
        /// Whether a won strike mission is in its fly-off: the input is locked and the ship is flown out, so nothing can
        /// hurt it any more (the free flight before it is still the pilot's to survive).
        /// </summary>
        public bool IsFlyingOff => IsStrike && flyingOff && phase == MissionPhase.Victory;

        /// <summary>The boss of the mission while it is in play (tests and tours).</summary>
        public Boss ActiveBoss => boss != null && boss.InPlay ? boss : null;

        /// <summary>
        /// The difficulty the current strike mission is flown at: the room's in co-op, the setup's in local co-op, else the
        /// pilot's.
        /// </summary>
        public StrikeDifficulty MissionDifficulty => IsCoop ? coop.Difficulty : IsLocal ? LocalCoopRules.Difficulty(LocalMatch) : Pilot.Difficulty;

        /// <summary>The storage the progress lives on (the tour backs up and restores the keys it touches); null without one.</summary>
        internal IStorageStrategy ProgressDisk => Disk;

        private StrikeTerrain Terrain => field != null ? field.Terrain : null;

        /// <summary>The strike screens of the interface (tours and tests); null in a scene without them.</summary>
        internal StrikeUI StrikeScreens => ui != null ? ui.Strike : null;


        partial void AwakeStrike()
        {
            if (field != null)
            {
                field.HostileEntered += OnHostileEntered;
            }
        }


        partial void OnDestroyStrike()
        {
            if (field != null)
            {
                field.HostileEntered -= OnHostileEntered;
            }
            ReleaseGunnery();
            ReleaseScroll();
        }


        #region Menu

        /// <summary>Switches the mission select to the asteroid field or the planet strike tab.</summary>
        public void SelectMode(MissionMode mode)
        {
            if (phase != MissionPhase.Menu)
            {
                return;
            }
            if (mode == menuMode && (Mission == null || Mission.Mode == mode))
            {
                return;
            }
            sounds?.Click();
            int index = mode == MissionMode.Strike
                ? (IsMissionOf(strikeMenuMission, MissionMode.Strike) ? strikeMenuMission : SuggestedStrikeMission())
                : (IsMissionOf(fieldMenuMission, MissionMode.Field) ? fieldMenuMission : SuggestedMission());
            if (mode == MissionMode.Strike && index < 0)
            {
                // No strike missions in this campaign: the tab shows that, and the field mission stays selected underneath.
                menuMode = mode;
                EndStrikePreview();
                ui?.ShowMode(MissionMode.Strike);
                StrikeScreens?.ShowMissionSelect(new List<StrikeMissionSummary>(), -1, 0, 0, Pilot);
                return;
            }
            PreviewMission(index);
        }


        /// <summary>Opens the Supply Room over the strike tab (not inside an online room).</summary>
        public void OpenSupply()
        {
            if (phase != MissionPhase.Menu || InSession || IsLobbyOpen)
            {
                ui?.UpdateError(T("The Supply Room is closed during a mission and inside an online room."));
                sounds?.Denied();
                return;
            }
            if (StrikeScreens == null)
            {
                return;
            }
            sounds?.Click();
            StrikeScreens.ShowSupply(Pilot, StrikeScreens.SelectedItem);
            StrikeScreens.Quartermaster(Greeting());
        }


        /// <summary>The results' SUPPLY ROOM button: back to the strike tab with the Supply Room open.</summary>
        public void OpenSupplyAfterResults()
        {
            if (IsCoop)
            {
                return;
            }
            ReturnToMissionSelect();
            OpenSupply();
        }


        public void CloseSupply()
        {
            if (StrikeScreens == null || !StrikeScreens.IsSupplyOpen)
            {
                return;
            }
            sounds?.Click();
            StrikeScreens.HideSupply();
            if (phase == MissionPhase.Menu && IsStrike)
            {
                ShowStrikeMenu();
            }
        }


        /// <summary>Buys one <paramref name="item"/> for the saved pilot and saves it.</summary>
        public void BuyItem(StrikeItem item)
        {
            if (phase != MissionPhase.Menu || InSession)
            {
                sounds?.Denied();
                return;
            }
            int price = StrikeArmory.BuyPrice(Pilot, item);
            BuyResult result = StrikeArmory.Buy(Pilot, item);
            switch (result)
            {
                case BuyResult.Bought:
                    progress?.SavePilot(Pilot);
                    sounds?.ShopBuy();
                    StrikeScreens?.Quartermaster(item == StrikeItem.EnergyModule ? T("Topped up. Fly careful.") : F("{0} - ${1:N0}. Good hunting.", T(StrikeArmory.Title(item)), price));
                    break;
                case BuyResult.NoMoney:
                    sounds?.Denied();
                    StrikeScreens?.Quartermaster(T("Credit's no good here, pilot. Come back with cash."), false);
                    break;
                case BuyResult.Full:
                    sounds?.Denied();
                    StrikeScreens?.Quartermaster(item == StrikeItem.EnergyModule ? T("Your energy is full already.") : T("You can't carry any more of those."), false);
                    break;
                default:
                    sounds?.Denied();
                    StrikeScreens?.Quartermaster(T("That's not for sale."), false);
                    break;
            }
            RefreshSupply(item);
        }


        /// <summary>Sells one <paramref name="item"/> of the saved pilot and saves it.</summary>
        public void SellItem(StrikeItem item)
        {
            if (phase != MissionPhase.Menu || InSession)
            {
                sounds?.Denied();
                return;
            }
            int price = StrikeArmory.SellPrice(Pilot, item);
            if (StrikeArmory.CanSell(Pilot, item) && !StrikeArmory.WalletTakes(Pilot, item))
            {
                // The wallet is capped: the sale would pay only part of the price (or nothing) and still take the item.
                sounds?.Denied();
                StrikeScreens?.Quartermaster(T("Your wallet is full, pilot. Spend some first."), false);
            }
            else if (StrikeArmory.Sell(Pilot, item))
            {
                progress?.SavePilot(Pilot);
                sounds?.ShopSell();
                StrikeScreens?.Quartermaster(F("Sold for ${0:N0}. Pleasure doing business.", price));
            }
            else
            {
                sounds?.Denied();
                StrikeScreens?.Quartermaster(item == StrikeItem.PhaseShield ? T("I only buy undamaged shields.") : T("I can't buy that from you."), false);
            }
            RefreshSupply(item);
        }


        /// <summary>Sets the saved pilot's difficulty.</summary>
        public void SetDifficulty(StrikeDifficulty difficulty)
        {
            if (phase != MissionPhase.Menu || Pilot.Difficulty == difficulty)
            {
                return;
            }
            sounds?.Click();
            Pilot.Difficulty = difficulty;
            Pilot.NotifyChanged();
            progress?.SavePilot(Pilot);
            if (menuMode == MissionMode.Strike)
            {
                ShowStrikeMenu();
            }
        }


        /// <summary>
        /// The pilot left a room before the ship of a won mission landed (or any mission ended without a landing): the working
        /// copy goes and the saved pilot stays as it was.
        /// </summary>
        internal void DiscardStrikeWorking()
        {
            working = null;
            ReleaseGunnery();
        }


        /// <summary>The main file's menu keeps the field world; a strike preview replaces the menu field (see <see cref="PreviewStrike"/>).</summary>
        private void EnterStrikeMenu()
        {
            ReleaseScroll();
            DiscardStrikeWorking();
            flyingOff = false;
            landed = false;
            sounds?.SetBossAlarm(false);
            sounds?.SetBeam(false);
            StrikeScreens?.HideAll();
        }


        /// <summary>Shows the first tiles of <paramref name="mission"/> scrolling slowly behind the menu (no asteroids, no backdrop).</summary>
        private void PreviewStrike(StrikeLevel mission)
        {
            menuMode = MissionMode.Strike;
            strikeMenuMission = LevelIndex;
            if (phase == MissionPhase.Menu && field != null)
            {
                // The menu asteroids of the field tab would float over the ground.
                field.Clear();
            }
            StrikeTerrain terrain = Terrain;
            if (terrain != null && mission.Terrain != null)
            {
                if (terrain.Level != mission)
                {
                    previewDistance = 0f;
                }
                terrain.Show(mission);
                terrain.SetDistance(previewDistance);
                backdrop?.SetVisible(false);
            }
            else
            {
                terrain?.Hide();
                backdrop?.SetVisible(true);
            }
            ShowStrikeMenu();
        }


        /// <summary>A field mission is previewed again: the ground of a strike preview goes and the asteroids come back.</summary>
        private void EndStrikePreview()
        {
            StrikeTerrain terrain = Terrain;
            bool wasPreviewing = terrain != null && terrain.IsShown;
            terrain?.Hide();
            backdrop?.SetVisible(true);
            if (wasPreviewing && phase == MissionPhase.Menu && field != null && field.Targets.Count == 0 && !IsStrike)
            {
                SpawnMenuField();
            }
        }


        /// <summary>A frame of the menu while a strike mission is previewed (the terrain scrolls slowly).</summary>
        private void UpdateStrikePreview(float deltaTime)
        {
            StrikeTerrain terrain = Terrain;
            if (phase != MissionPhase.Menu || terrain == null || !terrain.IsShown)
            {
                return;
            }
            float loop = PreviewLoop;
            if (terrain.Level != null && terrain.Level.Length > StrikeRules.TileLength * 2f)
            {
                loop = Mathf.Min(loop, terrain.Level.Length - StrikeRules.TileLength);
            }
            previewDistance = Mathf.Repeat(previewDistance + PreviewScrollSpeed * deltaTime, loop);
            terrain.SetDistance(previewDistance);
        }


        /// <summary>The strike tab of the mission select, with its missions, the strike stars and the pilot.</summary>
        private void ShowStrikeMenu()
        {
            ui?.ShowMode(MissionMode.Strike);
            AsteroidsCampaign sectors = AsteroidsCampaign;
            int stars = sectors != null && progress != null ? sectors.TotalStars(progress, MissionMode.Strike) : 0;
            int maxStars = sectors != null ? sectors.MaxStarsOf(MissionMode.Strike) : 0;
            StrikeScreens?.ShowMissionSelect(BuildStrikeSummaries(), IsStrike ? LevelIndex : -1, stars, maxStars, Pilot);
            if (StrikeScreens != null && StrikeScreens.IsSupplyOpen)
            {
                StrikeScreens.ShowSupply(Pilot, StrikeScreens.SelectedItem);
            }
        }


        private void RefreshSupply(StrikeItem item)
        {
            if (StrikeScreens == null)
            {
                return;
            }
            if (StrikeScreens.IsSupplyOpen)
            {
                StrikeScreens.ShowSupply(Pilot, item);
            }
            if (menuMode == MissionMode.Strike)
            {
                AsteroidsCampaign sectors = AsteroidsCampaign;
                int stars = sectors != null && progress != null ? sectors.TotalStars(progress, MissionMode.Strike) : 0;
                int maxStars = sectors != null ? sectors.MaxStarsOf(MissionMode.Strike) : 0;
                StrikeScreens.ShowMissionSelect(BuildStrikeSummaries(), IsStrike ? LevelIndex : -1, stars, maxStars, Pilot);
            }
        }


        /// <summary>The quartermaster greets again in the language just picked (the Supply Room is open).</summary>
        private void GreetAgain()
        {
            if (StrikeScreens != null && StrikeScreens.IsSupplyOpen)
            {
                StrikeScreens.Quartermaster(Greeting());
            }
        }


        private string Greeting()
        {
            if (Pilot.Money < 20000)
            {
                return T("Short on cash? Fly a mission and come back.");
            }
            return Pilot.Energy < StrikeRules.MaxEnergy ? T("Welcome back. Want that hull patched up?") : T("Welcome back, pilot. What'll it be?");
        }


        private List<StrikeMissionSummary> BuildStrikeSummaries()
        {
            var summaries = new List<StrikeMissionSummary>();
            AsteroidsCampaign sectors = AsteroidsCampaign;
            int number = 0;
            for (int i = 0; i < LevelCount; i++)
            {
                if (!(MissionAt(i) is StrikeLevel mission))
                {
                    continue;
                }
                number++;
                AsteroidsCampaign.Sector sector = sectors != null ? sectors.SectorOf(i) : null;
                Color accent = mission.Terrain != null ? AsteroidsThemes.Accent(mission.Terrain, new Color(1f, 0.6f, 0.25f))
                    : sector != null && sector.theme != null ? AsteroidsThemes.Accent(sector.theme, new Color(1f, 0.6f, 0.25f)) : new Color(1f, 0.6f, 0.25f);
                summaries.Add(new StrikeMissionSummary
                {
                    Index = i,
                    Number = number,
                    Title = T(mission.Title),
                    Description = T(mission.Description),
                    Sector = mission.Sector,
                    SectorTitle = T(sector != null && !string.IsNullOrEmpty(sector.title) ? sector.title : mission.Terrain != null ? mission.Terrain.Title : string.Empty),
                    Accent = accent,
                    Unlocked = IsUnlocked(i),
                    LockReason = LockReason(i),
                    Stars = progress != null ? progress.Stars(i) : 0,
                    BestMoney = progress != null ? progress.BestScore(i) : 0,
                    BossName = mission.BossPrefab != null ? T(mission.BossPrefab.DisplayName) : string.Empty,
                    SectorStars = sector != null ? sector.starsRequired : 0
                });
            }
            return summaries;
        }


        /// <summary>The first open strike mission without stars, or the last open one; -1 when there are no strike missions.</summary>
        private int SuggestedStrikeMission()
        {
            int suggested = -1;
            for (int i = 0; i < LevelCount; i++)
            {
                if (!(MissionAt(i) is StrikeLevel))
                {
                    continue;
                }
                if (suggested < 0 || IsUnlocked(i))
                {
                    suggested = i;
                }
                if (IsUnlocked(i) && (progress == null || progress.Stars(i) == 0))
                {
                    break;
                }
            }
            return suggested;
        }


        private bool IsMissionOf(int index, MissionMode mode)
        {
            AsteroidsLevel mission = index >= 0 && index < LevelCount ? MissionAt(index) : null;
            return mission != null && mission.Mode == mode;
        }

        #endregion


        #region Mission

        /// <summary>
        /// Puts the playfield and the world back as the asteroid field has them: wrapping edges, the camera's size (or the
        /// room's in co-op), no scroll, no terrain, the space backdrop. Called by the menu, a field mission and leaving a session.
        /// </summary>
        private void RestoreFieldWorld()
        {
            if (Playground != null)
            {
                Playground.Wraps = true;
            }
            if (field != null)
            {
                field.ScrollSpeed = 0f;
                field.ScrollDistance = 0f;
                field.PickupPull = 0f;
                if (field.Terrain != null)
                {
                    field.Terrain.Hide();
                }
            }
            backdrop?.SetVisible(true);
            FitPlayfield();
        }


        /// <summary>
        /// The strike part of preparing a mission (the main file resets the score, the objective and the HUD around it):
        /// the fixed playfield without wrapping, the terrain, the working copy of the pilot, the ship, one life, the scroll
        /// director (director stays null).
        /// </summary>
        private void PrepareStrike()
        {
            var level = (StrikeLevel)Mission;
            StrikeDifficulty difficulty = MissionDifficulty;
            ReleaseScroll();
            ReleaseGunnery();

            // The screen is the playfield: a fixed size (the server's in co-op), no wrapping edges.
            if (Playground != null)
            {
                Playground.Fix(IsCoop ? coop.HalfSize : StrikeRules.HalfSize);
                Playground.Wraps = false;
            }
            cameraRig?.Place();
            field.ScrollSpeed = 0f;
            field.ScrollDistance = 0f;
            field.PickupPull = 0f;
            StrikeTerrain terrain = Terrain;
            if (terrain != null && level.Terrain != null)
            {
                terrain.Show(level);
                terrain.SetDistance(0f);
                backdrop?.SetVisible(false);
            }
            else
            {
                terrain?.Hide();
                backdrop?.SetVisible(true);
            }

            // The pilot flies a copy; the saved pilot changes only when the ship lands after a win.
            working = Pilot.Clone();
            working.Difficulty = difficulty;
            working.EnsureLaunchEnergy();
            walletAtStart = working.Money;
            spawner.BossHealthScale = StrikeRules.BossHealthScale(difficulty, IsCoop ? Mathf.Max(1, coop.Pilots) : IsLocal ? localPilots.Count : 1);
            spawner.BossBurstScale = StrikeRules.BossBurstScale(difficulty);

            lives = 1;
            hostilesEntered = 0;
            hostilesDestroyed = 0;
            damageTaken = 0f;
            shieldLowWarned = false;
            flyingOff = false;
            flyOffTime = 0f;
            landed = false;
            scrollReportTimer = 0f;

            ship.ResetForStrike(working);
            if (IsCoop)
            {
                // The pilots of a room start side by side along the bottom.
                ship.Position = FieldMath.RowPoint(coop.Slot, coop.Pilots, coop.HalfSize);
            }
            gunnery = ship.Strike;
            if (gunnery != null)
            {
                gunnery.WeaponLost += OnWeaponLost;
            }

            // Only the simulator runs the scroll; the others follow its reports (CoopScrollReported).
            if (Simulates)
            {
                scroll = new ScrollDirector(level, spawner, difficulty)
                {
                    Top = Playground != null ? Playground.Top : StrikeRules.HalfSize.y
                };
                scroll.BossArrived += OnStrikeBossArrived;
            }
            ui?.ShowMode(MissionMode.Strike);
            StrikeScreens?.BeginMission(level, SectorTitleOf(level));
        }


        /// <summary>GO: the simulator starts the scroll.</summary>
        private void BeginStrike()
        {
            scroll?.Begin(0f);
        }


        /// <summary>A frame of a running strike mission (replaces the field's UpdateMission).</summary>
        private void UpdateStrike(float deltaTime)
        {
            missionTime += deltaTime;
            field.WorldTimeScale = 1f;
            SimulateShips(deltaTime);
            AdvanceScroll(deltaTime);
            if (IsLocal)
            {
                UpdateLocalPilots(deltaTime);
            }
            field.Tick(deltaTime);
            Terrain?.SetDistance(field.ScrollDistance);
            UpdateHints(deltaTime);
            WatchPilot();
            // In a shared mission the objective is the simulator's to judge, also while its own pilot only watches.
            if ((phase == MissionPhase.Playing || phase == MissionPhase.Watching) && objective.IsComplete && Simulates)
            {
                Win();
            }
        }


        /// <summary>
        /// The simulator runs the scroll (and tells the others about it); the others keep their ground moving at the last
        /// speed the simulator reported.
        /// </summary>
        private void AdvanceScroll(float deltaTime)
        {
            if (Simulates && scroll != null)
            {
                scroll.Tick(deltaTime);
                field.ScrollSpeed = scroll.Speed;
                field.ScrollDistance = scroll.Distance;
                if (IsCoop && field.Link != null)
                {
                    scrollReportTimer -= deltaTime;
                    if (scrollReportTimer <= 0f)
                    {
                        scrollReportTimer = ScrollReportInterval;
                        field.Link.ScrollReported(field.ScrollDistance, field.ScrollSpeed);
                    }
                }
            }
            else if (FollowsScroll)
            {
                // A guest's ground follows the simulator's reports (AsteroidsGameManager.Coop.cs).
                FollowCoopScroll(deltaTime);
            }
        }


        /// <summary>
        /// The ship took <paramref name="hit"/> (its amount is what the phase shields and the energy lost, after the
        /// multipliers): it counts toward the third star hit by hit, so an energy gain in the same frame cannot hide it.
        /// </summary>
        private void CountStrikeDamage(DamageInfo hit)
        {
            if (working != null && hit.Amount > 0f && IsMissionActiveOrEnding)
            {
                damageTaken += hit.Amount;
            }
        }


        /// <summary>The SHIELD LOW warning, from the working copy (not in local co-op, where the pilots list shows every pilot's energy).</summary>
        private void WatchPilot()
        {
            if (working == null || IsLocal)
            {
                return;
            }
            bool low = working.PhaseShields <= 0 && working.Energy <= ShieldLowEnergy && ship.IsAlive;
            if (low && !shieldLowWarned && IsMissionActive)
            {
                shieldLowWarned = true;
                StrikeScreens?.Warn(T("SHIELD LOW"));
                sounds?.ShieldLow();
            }
            else if (!low && working.Energy > ShieldLowEnergy + 5f)
            {
                shieldLowWarned = false;
            }
        }


        /// <summary>A frame of the Victory or Defeat phase of a strike mission: free flight, the fly-off, the results.</summary>
        private void UpdateStrikeEnding(float deltaTime)
        {
            field.WorldTimeScale = 1f;
            if (phase == MissionPhase.Victory)
            {
                if (!flyingOff)
                {
                    // Free flight: every pickup on screen drifts to the ships (40 m pull).
                    field.PickupPull = StrikeRules.EndPickupPull;
                    SimulateShips(deltaTime);
                    if (phaseTime >= StrikeRules.FreeFlightTime)
                    {
                        BeginFlyOff();
                    }
                }
                else
                {
                    FlyOff(deltaTime);
                }
            }
            else if (FollowsScroll)
            {
                FollowCoopScroll(deltaTime);
            }
            else
            {
                // The ground rolls to a stop under the wreck.
                field.ScrollSpeed = Mathf.MoveTowards(field.ScrollSpeed, 0f, deltaTime * 1.5f);
                field.ScrollDistance += field.ScrollSpeed * deltaTime;
            }
            field.Tick(deltaTime);
            Terrain?.SetDistance(field.ScrollDistance);
            WatchPilot();
            if (phase == MissionPhase.Victory && flyingOff && flyOffTime >= StrikeRules.FlyOffTime)
            {
                ShowResults(true);
            }
            else if (phase == MissionPhase.Defeat && phaseTime > 2.4f)
            {
                ShowResults(false);
            }
        }


        /// <summary>
        /// Input locks, the ship centres and climbs out of the top with a whoosh. From here the ship cannot be hurt
        /// (<see cref="IsFlyingOff"/>), and on the simulator the enemy fire on screen fizzles out and the units left hold
        /// their fire, so no shot passes through the climbing ship.
        /// </summary>
        private void BeginFlyOff()
        {
            flyingOff = true;
            flyOffTime = 0f;
            field.PickupPull = 0f;
            flyOffFrom = ship.Position;
            if (ship.IsAlive)
            {
                ship.Simulation?.Stop();
            }
            foreach (LocalPilot pilot in localPilots)
            {
                pilot.FlyOffFrom = pilot.Ship.Position;
                if (pilot.Ship.IsAlive)
                {
                    pilot.Ship.Simulation?.Stop();
                }
            }
            if (IsLocal ? AnyLocalShipAlive : ship.IsAlive)
            {
                sounds?.FlyBy();
            }
            sounds?.SetBeam(false);
            if (!field.IsReplica)
            {
                CeaseEnemyFire();
            }
        }


        /// <summary>Every enemy shot in play fizzles out and every enemy left on the field stops firing (the fly-off).</summary>
        private void CeaseEnemyFire()
        {
            IReadOnlyList<Shot> shots = field.EnemyShots;
            for (int i = shots.Count - 1; i >= 0; i--)
            {
                if (i < shots.Count && shots[i] != null && shots[i].InPlay)
                {
                    shots[i].Impact();
                }
            }
            IReadOnlyList<Shootable> targets = field.Targets;
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] is Enemy enemy && enemy.InPlay)
                {
                    enemy.ShotsDisabled = true;
                }
            }
        }


        private void FlyOff(float deltaTime)
        {
            flyOffTime += deltaTime;
            float t = Mathf.Clamp01(flyOffTime / StrikeRules.FlyOffTime);
            float top = Playground != null ? Playground.Top : StrikeRules.HalfSize.y;
            if (IsLocal)
            {
                // The ships of a local mission close up into a row and climb out side by side.
                Vector2 halfSize = Playground != null ? Playground.HalfSize : StrikeRules.HalfSize;
                for (int i = 0; i < localPilots.Count; i++)
                {
                    LocalPilot pilot = localPilots[i];
                    ClimbOut(pilot.Ship, pilot.FlyOffFrom, FieldMath.RowPoint(i, localPilots.Count, halfSize).x, t, top);
                }
            }
            else if (!ship.IsAlive)
            {
                return;
            }
            else
            {
                ClimbOut(ship, flyOffFrom, 0f, t, top);
            }
            if (t >= 1f && !landed)
            {
                Land();
            }
        }


        /// <summary>
        /// Where <paramref name="flying"/> is <paramref name="t"/> of the way through the fly-off from <paramref name="from"/>:
        /// over to <paramref name="x"/> in the first half, then accelerating out past the top edge.
        /// </summary>
        private static void ClimbOut(AsteroidsPlayer flying, Vector2 from, float x, float t, float top)
        {
            if (flying == null || !flying.IsAlive)
            {
                return;
            }
            float across = Mathf.Lerp(from.x, x, Tween.InOutCubic(Mathf.Clamp01(t * 1.6f)));
            float up = Mathf.Lerp(from.y, top + 6f, t * t * t);
            flying.Position = new Vector2(across, up);
            flying.transform.rotation = Quaternion.identity;
        }


        /// <summary>
        /// The ship of a won mission is home: the working copy with the mission's money becomes the saved pilot. In local
        /// co-op the ships are home when one of them is: only the money the squad earned goes into the saved pilot's
        /// wallet (once), the rest of the saved pilot stays as it was (the pilots flew copies of its kit).
        /// </summary>
        private void Land()
        {
            if (IsLocal)
            {
                if (landed || working == null || !AnyLocalShipAlive)
                {
                    return;
                }
                landed = true;
                Pilot.CommitMission(null, squad.TeamScore);
                progress?.SavePilot(Pilot);
                return;
            }
            if (landed || working == null || !ship.IsAlive)
            {
                return;
            }
            landed = true;
            // The room's difficulty was for that room only.
            StrikeDifficulty own = Pilot.Difficulty;
            Pilot.CommitMission(working, score.Score);
            Pilot.Difficulty = own;
            progress?.SavePilot(Pilot);
        }


        /// <summary>The strike part of winning (the main file sets the phase): no life bonus, no clearing of ground units.</summary>
        private void WinStrike()
        {
            flyingOff = false;
            flyOffTime = 0f;
            landed = false;
            sounds?.SetBossAlarm(false);
            ui?.Announce(T("SECTOR SECURED"), T("Head for home"), new Color(0.45f, 1f, 0.6f));
            sounds?.Victory();
            cameraRig?.Pulse(0.6f);
        }


        /// <summary>The strike part of losing solo (the main file sets the phase): the scroll stops, the working copy is discarded.</summary>
        private void LoseStrike()
        {
            scroll?.Stop();
            sounds?.SetBossAlarm(false);
            sounds?.SetBeam(false);
        }


        /// <summary>The results of a solo strike mission: stars, money, the pilot saved on a win.</summary>
        private void ShowStrikeResults(bool victory)
        {
            phase = MissionPhase.Results;
            if (victory)
            {
                // A win whose fly-off was cut short still lands the ship.
                Land();
            }
            bool won = victory && landed;
            bool killStar = hostilesDestroyed >= StrikeRules.StarKillShare * hostilesEntered;
            bool damageStar = damageTaken <= StrikeRules.StarDamage;
            int stars = won ? 1 + (killStar ? 1 : 0) + (damageStar ? 1 : 0) : 0;
            bool newBest = won && progress != null && progress.RecordLevel(LevelIndex, stars, score.Score);
            int next = AsteroidsCampaign != null ? AsteroidsCampaign.NextMission(LevelIndex) : -1;
            var result = new StrikeResult
            {
                Title = Mission != null ? Mission.Title : "Mission",
                Victory = won,
                Stars = stars,
                KillStar = killStar,
                DamageStar = damageStar,
                Money = score.Score,
                Wallet = Pilot.Money,
                HostilesEntered = hostilesEntered,
                HostilesDestroyed = hostilesDestroyed,
                DamageTaken = damageTaken,
                Time = missionTime,
                NewBest = newBest,
                HasNext = next >= 0,
                NextLocked = next >= 0 && !IsUnlocked(next),
                NextLockReason = next >= 0 ? LockReason(next) : string.Empty
            };
            ReleaseScroll();
            DiscardStrikeWorking();
            sounds?.SetBossAlarm(false);
            sounds?.SetBeam(false);
            TransitionState(won ? BaseGameState.Victory : BaseGameState.GameOver);
            StrikeScreens?.ShowResults(result);
        }


        /// <summary>The strike HUD (replaces the field HUD's refresh).</summary>
        private void RefreshStrikeHud()
        {
            if (StrikeScreens == null || working == null)
            {
                return;
            }
            StrikeGunnery guns = ship.Strike;
            var state = new StrikeHudState
            {
                Wallet = walletAtStart,
                MissionMoney = IsLocal && squad != null ? squad.TeamScore : score.Score,
                Energy = Mathf.Clamp01(working.Energy / StrikeRules.MaxEnergy),
                Shield = working.PhaseShields > 0 ? Mathf.Clamp01(working.ShieldPoints / StrikeRules.PhaseShieldPoints) : 0f,
                PhaseShields = working.PhaseShields,
                Megabombs = working.Megabombs,
                MegabombCooldown = guns != null ? Mathf.Clamp01(guns.MegabombCooldown / StrikeRules.MegabombCooldown) : 0f,
                Special = working.HasSpecial ? working.Special : StrikeItem.MachineGun,
                Progress = ScrollProgress,
                BossActive = boss != null && boss.InPlay,
                BossName = boss != null ? boss.DisplayName : string.Empty,
                BossHealth = boss != null ? boss.BarFraction : 0f,
                HasScanner = working.HasScanner,
                ShieldLow = working.PhaseShields <= 0 && working.Energy <= ShieldLowEnergy
            };
            StrikeScreens.UpdateHud(state);
        }


        /// <summary>A shootable of a strike mission was destroyed: bounty through <see cref="AddMoney"/>, the kill count.</summary>
        private void StrikeTargetDestroyed(Shootable target, DamageInfo hit)
        {
            if (!hit.ByPlayer || !IsMissionActiveOrEnding)
            {
                return;
            }
            if (target is StrikeAircraft || target is GroundUnit)
            {
                hostilesDestroyed++;
            }
            // The kill of a pilot on another device pays on their device; a local wingman's pays that pilot.
            if (hit.Seat.HasValue && !IsLocal)
            {
                return;
            }
            int bounty = target is StrikeBoss && spawner != null && spawner.BossBounty > 0 ? spawner.BossBounty : target.Score;
            if (bounty > 0)
            {
                AddMoney(bounty, target.Position, IsLocal ? LocalPilotOf(hit)?.Ship : null);
            }
        }


        /// <summary>A strike hostile came on screen (for the 70% star).</summary>
        private void OnHostileEntered(SpaceBody body)
        {
            if (IsStrike && (body is StrikeAircraft || body is GroundUnit) && (IsMissionActiveOrEnding || phase == MissionPhase.Briefing))
            {
                hostilesEntered++;
            }
        }


        /// <summary>
        /// All mission money goes through here: the score (no combo multiplier, no toasts) and a "$" popup at
        /// <paramref name="at"/>.
        /// </summary>
        public void AddMoney(int amount, Vector2 at)
        {
            AddMoney(amount, at, null);
        }


        /// <summary>
        /// Mission money that <paramref name="earner"/> earned: in local co-op it is that ship's pilot's (the first pilot's
        /// when the ship is none of theirs), otherwise the mission's like any other.
        /// </summary>
        public void AddMoney(int amount, Vector2 at, AsteroidsPlayer earner)
        {
            if (amount <= 0 || !IsMissionActiveOrEnding)
            {
                return;
            }
            (IsLocal ? LocalScoreOf(earner) : score).Add(amount);
            SyncScore();
            Color color = amount >= 10000 ? new Color(1f, 0.85f, 0.3f) : new Color(0.75f, 1f, 0.6f);
            effects?.Popup(at, $"${amount:N0}", color, amount >= 10000 ? 1.4f : amount >= 1000 ? 1f : 0.8f);
            // Money that comes in after the win is reported at once: the room is about to close.
            if (IsCoop && phase == MissionPhase.Victory)
            {
                online?.ReportScoreNow(score.Score);
            }
        }


        /// <summary>Tours and tests: jumps the scroll to just before the boss.</summary>
        public void SkipToBoss()
        {
            if (!Simulates || scroll == null || !IsMissionActive)
            {
                return;
            }
            scroll.SkipToBoss();
            field.ScrollDistance = scroll.Distance;
            field.ScrollSpeed = scroll.Speed;
            Terrain?.SetDistance(field.ScrollDistance);
        }


        private void OnStrikeBossArrived(Boss arrived)
        {
            if (arrived != null)
            {
                OnBossArrived(arrived);
            }
        }


        private void OnWeaponLost(StrikeItem item)
        {
            StrikeScreens?.Warn(T("WEAPON DESTROYED"));
            sounds?.WeaponLost();
            ui?.Toast(F("{0} LOST", T(StrikeUI.ShortName(item))), new Color(1f, 0.4f, 0.35f));
        }


        private void ReleaseGunnery()
        {
            if (gunnery != null)
            {
                gunnery.WeaponLost -= OnWeaponLost;
                gunnery = null;
            }
        }


        private void ReleaseScroll()
        {
            if (scroll != null)
            {
                scroll.Stop();
                scroll.BossArrived -= OnStrikeBossArrived;
                scroll = null;
            }
        }

        #endregion
    }
}
