using System.Collections.Generic;
using System.Text;
using Gamebox;
using Gamebox.Online;
using UnityEngine;
using UnityEngine.Rendering;

namespace Portfolio.EndlessRunner
{
    /// <summary>
    /// The local half of the manager: a race of two to four runners at this device, in split screen
    /// (<see cref="LocalRace"/>). The shared local play setup (<see cref="BaseGameManager.OpenLocalPlay"/>) names the
    /// players and binds their controls; every seat then gets a runner of its own (the first one is the runner of the
    /// scene, the others copies of the runner prefab, tinted in the seat's colour with the player's name over the head),
    /// a camera that draws its part of the screen and a HUD over it. Everybody runs the selected level on one track: the
    /// track reaches from the last runner to the first, carts wake for whoever comes first, the leader brings the next
    /// world, and a coin or a power-up is gone once a runner took it. Runners run through each other. A runner across
    /// the line or out of hearts watches the leader; when every run is over the results show the standings, and Retry
    /// races again with the same players. A local race records no stars and no records. Several people at this device
    /// in an online race (a party) run on the same machinery without a match (<see cref="SetUpParty"/>), among the
    /// ghosts of the other devices' runners.
    /// </summary>
    public partial class RunnerGameManager
    {
        [Header("Local play")]
        [Tooltip("The most track kept behind the first runner of a local race for the last one, in meters (the pools hold no more); a runner farther back is out.")]
        [SerializeField] internal float localKeepBehind = 200f;

        private LocalPlayRules localRules;
        private LocalRace localRace;
        private RunnerRun[] alone;
        // What a local race made and takes away again: the other runners, their cameras and the names over the heads.
        private readonly List<GameObject> localParts = new List<GameObject>();
        // The parts of each run among them, for a run that goes before the race is over (its seat is played elsewhere now).
        private readonly Dictionary<RunnerRun, List<GameObject>> runParts = new Dictionary<RunnerRun, List<GameObject>>();
        private LocalStandingsBoard standingsBoard;
        // The models moved aside while one view draws, and where they were.
        private readonly List<(Transform model, Vector3 position)> steppedAside = new List<(Transform, Vector3)>();
        private const float StepAsideReach = 0.65f;
        // The results of a local race are on the screen: their standings are written again in another language.
        private bool localResultsShown;

        /// <summary>The rules of a local race: two to four players at once, each on controls of their own.</summary>
        public override LocalPlayRules LocalPlay => localRules ?? (localRules = new LocalPlayRules
        {
            Hint = "Everybody races the selected level at once. Meters and coins make the score.",
            MinPlayers = 2,
            MaxPlayers = ControlBindings.MaxSeats,
            DefaultPlayers = 2,
            Simultaneous = true,
            Controls = SeatInput.Scheme(),
            AllowComputers = false,
            // The chips of the setup in the colours of the runners.
            SeatColors = racerColors
        });

        /// <summary>A race at this device is set up: from the start of the setup to the way back to the level select.</summary>
        internal bool InLocalRace => localRace != null;

        /// <summary>The race at this device, or null.</summary>
        internal LocalRace LocalRace => localRace;

        /// <summary>The runs of the game: the one of the runner of the scene, or one per seat of a local race.</summary>
        private IReadOnlyList<RunnerRun> Runs => InLocalRace ? localRace.Runs : alone ?? (alone = new[] { Main });


        #region Setting up

        /// <summary>The local play button of the level select: the setup of a race of the selected level at this device.</summary>
        public override void OpenLocalPlay()
        {
            if (phase != RunPhase.Menu || InSession)
            {
                return;
            }
            RunnerLevel level = RunnerLevel;
            if (level == null || (!level.IsEndless && !progress.IsUnlocked(LevelIndex)))
            {
                UI?.UpdateError(RunnerText.T("Finish the previous level to unlock this one."));
                return;
            }
            sounds?.Play(sounds.click);
            base.OpenLocalPlay();
        }


        /// <summary>The setup's start: a runner, a camera and a HUD for every seat, then the selected level.</summary>
        protected override void OnLocalMatchBegun(LocalMatch match)
        {
            SetUpLocalRace(match);
            PlayLevel(LevelIndex);
        }


        /// <summary>Back in the level select: the runner of the scene is the only one again, on the whole screen.</summary>
        protected override void OnLocalMatchEnded(LocalMatch match)
        {
            TearDownLocalRace();
        }


        private void SetUpLocalRace(LocalMatch match)
        {
            TearDownLocalRace();
            localRace = new LocalRace(match);
            var inputs = new List<IRunnerInput>();
            foreach (LocalSeat seat in match.Players)
            {
                inputs.Add(new SeatInput(seat.Controls));
            }
            SetUpRuns(match.Players, inputs, null);
        }


        /// <summary>
        /// The runners driven at this device in an online race, when their runs are explicit (a party: the device's player
        /// and the companions who play through its connection; or the shared screen with the seats of its remotes): a
        /// runner, a camera and a HUD for each, as in a local race, the device's player first, each on the controls the
        /// race gives its seat (the keys of a seat here, the packets of a remote control) and in the colour its runner has
        /// on every device. The runners of the other devices are ghosts in every view (RunnerGameManager.Online.cs).
        /// </summary>
        private void SetUpParty()
        {
            TearDownLocalRace();
            localRace = new LocalRace(null);
            var seats = new List<LocalSeat>();
            var inputs = new List<IRunnerInput>();
            var partyRacers = new List<Racer>();
            foreach (int seatOfRoom in race.DrivenSeats)
            {
                Racer racer = RacerAt(seatOfRoom);
                if (racer == null)
                {
                    continue;
                }
                race.Controls.TryGetValue(seatOfRoom, out PlayerControls controls);
                seats.Add(new LocalSeat(seats.Count) { Name = racer.Name, Color = RacerColor(racer.Slot) });
                inputs.Add(new SeatInput(controls));
                partyRacers.Add(racer);
            }
            SetUpRuns(seats, inputs, partyRacers);
        }


        /// <summary>
        /// A run, a camera and a HUD for each of <paramref name="seats"/>, each in its part of the screen and moving on its
        /// input; the runs carry <paramref name="runnersOfRoom"/> in a party (their seats of the room), else a line of
        /// their own on the scoreboard of the local race.
        /// </summary>
        private void SetUpRuns(IReadOnlyList<LocalSeat> seats, IReadOnlyList<IRunnerInput> inputs, IReadOnlyList<Racer> runnersOfRoom)
        {
            BeginRuns();
            for (int i = 0; i < seats.Count; i++)
            {
                AddRun(seats[i], inputs[i], runnersOfRoom != null ? runnersOfRoom[i] : null);
            }
            LayoutRuns();
            var active = new List<IPlayer>();
            foreach (RunnerRun run in localRace.Runs)
            {
                active.Add(run.Runner);
            }
            ActivePlayers = active;
            ui?.ShowSplitScreen(true);
        }


        /// <summary>The split screen wakes for a race at this device (or the runs of a party), with no run in it yet.</summary>
        private void BeginRuns()
        {
            localRace ??= new LocalRace(null);
            // The glows and the names face every view, not only the first one.
            Billboard.FaceEveryCamera = true;
            StepAsideInViews(true);
        }


        /// <summary>
        /// One more run of the split screen: the runner of the scene while it is free (with its camera and <see cref="Main"/>),
        /// else a copy of the runner prefab and of the camera; the name over the head, the HUD, and the runner moving on
        /// <paramref name="input"/>. The run carries <paramref name="runnerOfRoom"/> in a party (its seat of the room),
        /// else a line of its own on the scoreboard of the local race. <see cref="LayoutRuns"/> places the views next.
        /// </summary>
        private RunnerRun AddRun(LocalSeat seat, IRunnerInput input, Racer runnerOfRoom)
        {
            RunnerGameTheme look = ThemeAs<RunnerGameTheme>();
            Transform hudRoot = ui != null ? ui.SplitScreenRoot : null;
            bool sceneRunner = localRace.RunOf(runner) == null;
            int partsBefore = localParts.Count;
            RunnerPlayer player = sceneRunner ? runner : CreateLocalRunner(seat);
            RunnerCamera view = sceneRunner ? runnerCamera : CreateLocalCamera(player, seat);
            RunnerRun run = sceneRunner ? Main : new RunnerRun(player, view);
            if (sceneRunner)
            {
                runner.gameObject.SetActive(true);
                runnerCamera.Watch(null);
            }
            player.InputSource = input;
            player.Assign(runnerOfRoom != null ? runnerOfRoom.Seat : seat.Index, PlayerControl.Local, seat.Name);
            RunnerGhost.Tint(player.animator, player.transform, seat.Color);
            localParts.Add(RunnerGhost.AddLabel(player.transform, seat.Name, seat.Color, ghostLabelMaterial));
            if (hudRoot != null)
            {
                run.Hud = RunnerSeatHud.Create(hudRoot, new Rect(0f, 0f, 1f, 1f), seat, look);
                localParts.Add(run.Hud.gameObject);
            }
            // Runners run through each other.
            foreach (RunnerRun other in localRace.Runs)
            {
                Physics.IgnoreCollision(player.Body, other.Runner.Body);
            }
            if (runnerOfRoom != null)
            {
                localRace.Add(run, seat, runnerOfRoom);
            }
            else
            {
                localRace.Add(run, seat);
            }
            runParts[run] = localParts.GetRange(partsBefore, localParts.Count - partsBefore);
            return run;
        }


        /// <summary>
        /// Lays the runs over the screen by their count (two side by side: a tall view suits a runner that runs into the
        /// screen; three or four in quarters), each HUD over its view, and the standings board in the quarter of three
        /// that no runner has. Again whenever a run comes or goes mid-race.
        /// </summary>
        private void LayoutRuns()
        {
            IReadOnlyList<RunnerRun> runs = localRace.Runs;
            for (int i = 0; i < runs.Count; i++)
            {
                Rect viewport = SplitScreen.Viewport(i, runs.Count, true);
                if (runs[i].Camera.View != null)
                {
                    runs[i].Camera.View.rect = viewport;
                }
                runs[i].Hud?.MoveTo(viewport);
            }
            Rect? spare = SplitScreen.Spare(runs.Count);
            Transform hudRoot = ui != null ? ui.SplitScreenRoot : null;
            if (standingsBoard != null && !spare.HasValue)
            {
                localParts.Remove(standingsBoard.gameObject);
                Destroy(standingsBoard.gameObject);
                standingsBoard = null;
            }
            if (spare.HasValue && standingsBoard == null && hudRoot != null)
            {
                standingsBoard = LocalStandingsBoard.Create(hudRoot, spare.Value, ThemeAs<RunnerGameTheme>());
                localParts.Add(standingsBoard.gameObject);
            }
        }


        /// <summary>
        /// Takes <paramref name="run"/> out of the split screen in the middle of a race (its seat is played elsewhere from
        /// now on): its runner, view, name and HUD go; the runner of the scene only hides, to be free for the next run.
        /// </summary>
        private void RemoveRun(RunnerRun run)
        {
            run.Runner.StopRun();
            ClearPowerUps(run);
            localRace.Remove(run);
            ActivePlayers.Remove(run.Runner);
            if (themes != null && runnerCamera != null && (themes.follow == null || themes.follow == run.Camera.transform))
            {
                themes.follow = runnerCamera.transform;
            }
            if (runParts.TryGetValue(run, out List<GameObject> parts))
            {
                foreach (GameObject part in parts)
                {
                    localParts.Remove(part);
                    if (part != null)
                    {
                        Destroy(part);
                    }
                }
                runParts.Remove(run);
            }
            if (run.Runner == runner)
            {
                runner.gameObject.SetActive(false);
                runner.InputSource = null;
                runner.Assign(-1, PlayerControl.Local);
                RunnerGhost.Tint(runner.animator, runner.transform, null);
                run.Seat = null;
                run.Racer = null;
                run.Phase = RunPhase.Menu;
            }
            else
            {
                PlayerList.Remove(run.Runner);
            }
            run.Hud = null;
        }


        /// <summary>A runner for a seat after the first: a copy of the runner prefab next to the runner of the scene.</summary>
        private RunnerPlayer CreateLocalRunner(LocalSeat seat)
        {
            GameObject source = ghostPrefab != null ? ghostPrefab : runner.gameObject;
            GameObject copy = Instantiate(source, runner.transform.parent);
            copy.name = $"Runner {seat.Index + 1}";
            localParts.Add(copy);
            var player = copy.GetComponent<RunnerPlayer>();
            RegisterPlayer(player);
            return player;
        }


        /// <summary>
        /// A camera for a seat after the first: a copy of the camera of the scene, put together asleep so it never hears
        /// (one ear for the whole screen: the first camera's) and never counts as the main camera.
        /// </summary>
        private RunnerCamera CreateLocalCamera(RunnerPlayer player, LocalSeat seat)
        {
            var holder = new GameObject("Camera holder");
            holder.SetActive(false);
            GameObject copy = Instantiate(runnerCamera.gameObject, holder.transform);
            copy.name = $"Camera {seat.Index + 1}";
            copy.tag = "Untagged";
            if (copy.TryGetComponent(out AudioListener ears))
            {
                DestroyImmediate(ears);
            }
            var view = copy.GetComponent<RunnerCamera>();
            view.target = player;
            copy.transform.SetParent(runnerCamera.transform.parent, false);
            copy.transform.SetPositionAndRotation(runnerCamera.transform.position, runnerCamera.transform.rotation);
            Destroy(holder);
            localParts.Add(copy);
            return view;
        }


        /// <summary>The runners of a local race run the way the settings say, like the runner of the scene.</summary>
        private void ApplyLocalSettings()
        {
            if (!InLocalRace)
            {
                return;
            }
            foreach (RunnerRun run in localRace.Runs)
            {
                run.Runner.ApplySettings(RunnerSettings);
            }
        }


        /// <summary>Takes the race away: the other runners, cameras and HUDs go, the runner of the scene is itself again.</summary>
        private void TearDownLocalRace()
        {
            if (localRace == null)
            {
                return;
            }
            foreach (RunnerRun run in localRace.Runs)
            {
                if (run.Runner != runner && run.Runner != null)
                {
                    PlayerList.Remove(run.Runner);
                }
            }
            foreach (GameObject part in localParts)
            {
                if (part != null)
                {
                    Destroy(part);
                }
            }
            localParts.Clear();
            runParts.Clear();
            standingsBoard = null;
            localRace = null;
            localResultsShown = false;

            RunnerRun first = Main;
            first.Seat = null;
            first.Racer = null;
            first.Hud = null;
            first.Phase = RunPhase.Menu;
            if (runner != null)
            {
                runner.gameObject.SetActive(true);
                runner.InputSource = null;
                runner.Assign(-1, PlayerControl.Local);
                RunnerGhost.Tint(runner.animator, runner.transform, null);
                ActivePlayers = new List<IPlayer> { runner };
            }
            if (runnerCamera != null)
            {
                runnerCamera.Watch(null);
                if (runnerCamera.View != null)
                {
                    runnerCamera.View.rect = new Rect(0f, 0f, 1f, 1f);
                }
            }
            if (themes != null && runnerCamera != null)
            {
                themes.follow = runnerCamera.transform;
            }
            Billboard.FaceEveryCamera = false;
            StepAsideInViews(false);
            ui?.ShowSplitScreen(false);
        }

        #endregion


        #region Running

        /// <summary>
        /// The runners line up on the start line side by side, each camera behind its runner; a party's runners stand
        /// where their places at the start of the race put them, beside the ghosts of the others.
        /// </summary>
        private void LineUpLocalRunners()
        {
            IReadOnlyList<RunnerRun> runs = localRace.Runs;
            for (int i = 0; i < runs.Count; i++)
            {
                RunnerRun run = runs[i];
                int slot = run.Racer != null ? run.Racer.Slot : i;
                run.Runner.ResetToStart(StartPositionOf(slot), StartLane(slot));
                run.Z = run.Runner.transform.position.z;
                run.Enter(RunPhase.Countdown);
                run.Camera.Watch<RunnerPlayer>(null);
                run.Camera.SetMode(RunnerCamera.Mode.Chase);
            }
        }


        /// <summary>The countdown of a local race begins: every HUD starts over.</summary>
        private void BeginLocalRace()
        {
            localResultsShown = false;
            RunnerLevel level = RunnerLevel;
            bool endless = level == null || level.IsEndless;
            foreach (RunnerRun run in localRace.Runs)
            {
                run.Hud?.Begin(endless, endless ? 0f : track.FinishZ, run.MaxHearts);
                run.Hud?.Banner(null);
            }
            ui?.ShowSplitScreen(true);
            RankLocalRunners();
            RefreshLocalHuds();
        }


        /// <summary>Gives the runners at this device their places: among themselves in a local race, in the whole race in a party.</summary>
        private void RankLocalRunners()
        {
            if (race != null)
            {
                RefreshRaceHud();
            }
            else
            {
                localRace.Rank();
            }
        }


        /// <summary>Every frame of a local race: each runner on its own, then the track, the world, the places and the HUDs.</summary>
        private void UpdateLocalRace(float deltaTime)
        {
            // Runners side by side reach a coin in the same frame: who is first to try goes round, so no seat always wins it.
            IReadOnlyList<RunnerRun> runs = localRace.Runs;
            if (runs.Count == 0)
            {
                return;
            }
            int firstToTry = Time.frameCount % runs.Count;
            for (int i = 0; i < runs.Count; i++)
            {
                RunnerRun run = runs[(firstToTry + i) % runs.Count];
                run.PhaseTime += deltaTime;
                switch (run.Phase)
                {
                    case RunPhase.Running:
                        UpdateLocalRun(run, deltaTime);
                        break;
                    case RunPhase.Finishing:
                        run.Z = run.Runner.transform.position.z;
                        UpdateLocalFinish(run);
                        break;
                    case RunPhase.Dying:
                        run.Z = run.Runner.transform.position.z;
                        if (run.PhaseTime > 1.8f)
                        {
                            EndLocalRun(run);
                        }
                        break;
                }
            }
            LeaveStragglersBehind();
            RunnerRun leader = localRace.Leader();
            UpdateTrackAroundField();
            if (leader != null)
            {
                UpdateWorld(leader.Z);
                // The rain and the snow fall around the one in front.
                if (themes != null)
                {
                    themes.follow = leader.Camera.transform;
                }
            }
            foreach (RunnerRun run in localRace.Runs)
            {
                if (run.Done)
                {
                    WatchLeader(run);
                }
            }
            RankLocalRunners();
            RefreshLocalHuds();
            if (!localRace.IsOver)
            {
                return;
            }
            if (race != null)
            {
                // A party is done: the runners of the other devices may still be out there.
                WatchPartyRace();
            }
            else
            {
                ShowLocalResults();
            }
        }


        private void UpdateLocalRun(RunnerRun run, float deltaTime)
        {
            RunnerPlayer player = run.Runner;
            float z = player.transform.position.z;
            run.RunTo(z);
            player.SetTargetSpeed(RunnerSettings.SpeedAtDistance(z));
            UpdatePickups(run, deltaTime);
            UpdateMovingObstacles(run);
            UpdatePowerUps(run, deltaTime);
            if (run.Phase == RunPhase.Running && z >= track.FinishZ)
            {
                BeginLocalFinish(run);
            }
        }


        /// <summary>
        /// A runner more than <see cref="localKeepBehind"/> meters behind the first one would run off the end of the track
        /// kept for the race: it is out.
        /// </summary>
        private void LeaveStragglersBehind()
        {
            RunnerRun leader = localRace.Leader();
            if (leader == null)
            {
                return;
            }
            foreach (RunnerRun run in localRace.Runs)
            {
                if (run.Phase == RunPhase.Running && run.Z < leader.Z - localKeepBehind)
                {
                    Toast(run, RunnerText.T("Left behind!"), Color.white);
                    Die(run);
                }
            }
        }


        /// <summary>
        /// The track reaches from the last runner on it to the first; the coins behind the last are out of reach. In a
        /// party it reaches back for a slower ghost too, as in a race of one runner here.
        /// </summary>
        private void UpdateTrackAroundField()
        {
            if (!localRace.TrySpan(out float front, out float back))
            {
                return;
            }
            float spread = Mathf.Clamp(front - back, 0f, localKeepBehind);
            float behind = spread + track.keepBehind;
            float floatingBehind = spread + TrackGenerator.FloatingBehind;
            if (race != null)
            {
                float forGhosts = KeepBehind(front);
                behind = Mathf.Max(behind, forGhosts);
                floatingBehind = Mathf.Max(floatingBehind, forGhosts);
            }
            track.UpdateTrack(front, behind, floatingBehind);
        }


        /// <summary>The z the track and the world of a local race follow: the first runner on the track.</summary>
        private float LocalFocusZ()
        {
            return localRace.TrySpan(out float front, out _) ? front : runner.transform.position.z;
        }


        private void BeginLocalFinish(RunnerRun run)
        {
            run.Enter(RunPhase.Finishing);
            run.Finished = true;
            run.Runner.SetTargetSpeed(3f);
            run.Camera.SetMode(RunnerCamera.Mode.Finish);
            effects?.Confetti(run.Runner.transform.position + new Vector3(0f, 5f, 6f));
            sounds?.Play(sounds.victory);
            Toast(run, RunnerText.T("FINISH!"), ThemeAccent());
            ClearPowerUps(run);
        }


        private void UpdateLocalFinish(RunnerRun run)
        {
            if (run.Runner.IsRunning && run.PhaseTime > 1.3f)
            {
                run.Runner.StopRun();
                run.Runner.animator?.Celebrate();
                effects?.Confetti(run.Runner.transform.position + new Vector3(0f, 5f, 2f));
            }
            if (run.PhaseTime > 3.4f)
            {
                EndLocalRun(run);
            }
        }


        /// <summary>A runner of a local race is out of hearts: it falls, and its view turns to the leader a moment later.</summary>
        private void KnockOut(RunnerRun run)
        {
            run.Enter(RunPhase.Dying);
            run.Runner.StopRun();
            run.Runner.animator?.Die();
            run.Camera.SetMode(RunnerCamera.Mode.Crash);
            sounds?.Play(sounds.gameOver);
            ClearPowerUps(run);
        }


        /// <summary>The run is over: its player watches the others until everybody is done. In a party the server hears of it.</summary>
        private void EndLocalRun(RunnerRun run)
        {
            run.Enter(RunPhase.Watching);
            run.Runner.StopRun();
            WatchLeader(run);
            if (race != null)
            {
                EndPartyRun(run);
            }
        }


        /// <summary>The view of a runner who is done follows the runner it watches while that one runs, else the leader.</summary>
        private void WatchLeader(RunnerRun run)
        {
            RunnerRun watched = localRace.RunOf(run.Camera.WatchedRunner as RunnerPlayer);
            if (watched == null || watched.Phase != RunPhase.Running)
            {
                watched = localRace.Leader();
            }
            if (watched != null)
            {
                run.Camera.Watch(watched.Runner);
            }
        }


        /// <summary>Every HUD shows its run; the shared screen tells every remote control the same about its seat.</summary>
        private void RefreshLocalHuds()
        {
            foreach (RunnerRun run in localRace.Runs)
            {
                PublishRunStatus(run);
                RunnerSeatHud hud = run.Hud;
                if (hud == null)
                {
                    continue;
                }
                hud.UpdateRun(run.Coins, Mathf.FloorToInt(run.Score), run.Distance, run.Hearts, run.MaxHearts);
                hud.SetPlace(run.Racer.Place);
                for (int i = 0; i < PowerUps.Count; i++)
                {
                    hud.SetPowerUp((PowerUpType)i, PowerUpFraction(run, (PowerUpType)i));
                }
                hud.Banner(BannerOf(run));
            }
            // A party's board has the whole race.
            standingsBoard?.Show(race != null ? RaceStandingsText() : LocalStandingsText());
        }


        /// <summary>
        /// What the banner of a run says once it is over: how it ended, and whom its view follows now (another runner at
        /// this device, or in a party the ghost of a runner of another device).
        /// </summary>
        private string BannerOf(RunnerRun run)
        {
            if (run.Phase != RunPhase.Watching && run.Phase != RunPhase.Dying)
            {
                return null;
            }
            string ended = run.Finished ? RunnerText.F("Finished - {0}", Standings.Ordinal(run.Racer.Place)) : RunnerText.T("Out of hearts");
            string watching = null;
            if (run.Done)
            {
                RunnerRun watched = localRace.RunOf(run.Camera.WatchedRunner as RunnerPlayer);
                RunnerGhost ghost = run.Camera.Watched;
                Racer ghostRacer = ghost != null ? RacerAt(ghost.Seat) : null;
                if (watched != null && watched != run)
                {
                    watching = watched.Seat.ColoredName;
                }
                else if (ghostRacer != null)
                {
                    watching = ColoredName(ghostRacer);
                }
            }
            return watching == null ? ended : ended + "\n<size=75%>" + RunnerText.F("Watching {0}", watching) + "</size>";
        }


        private Color ThemeAccent()
        {
            RunnerGameTheme look = ThemeAs<RunnerGameTheme>();
            return look != null ? look.Colors.accent : new Color(1f, 0.85f, 0.2f);
        }


        /// <summary>
        /// A view of a local race is about to draw: the other runners right where the runner it follows is (they run
        /// through each other) step aside in it for the moment, so nobody's runner is hidden in its own view by another.
        /// </summary>
        private void StepAsideFor(ScriptableRenderContext context, Camera view)
        {
            if (!InLocalRace)
            {
                return;
            }
            RunnerRun owner = null;
            foreach (RunnerRun run in localRace.Runs)
            {
                if (run.Camera != null && run.Camera.View == view)
                {
                    owner = run;
                    break;
                }
            }
            if (owner == null)
            {
                return;
            }
            Transform subject = owner.Camera.WatchedRunner != null ? owner.Camera.WatchedRunner.transform : owner.Runner.transform;
            Vector3 there = subject.position;
            foreach (RunnerRun run in localRace.Runs)
            {
                Transform model = run.Runner.animator != null ? run.Runner.animator.body : null;
                if (model == null || run.Runner.transform == subject)
                {
                    continue;
                }
                Vector3 position = run.Runner.transform.position;
                if (Mathf.Abs(position.x - there.x) > StepAsideReach || Mathf.Abs(position.z - there.z) > 2.5f)
                {
                    continue;
                }
                // Toward the open side of the road, a little farther for every seat so two do not step into each other.
                float side = there.x > 0.1f ? -1f : 1f;
                Vector3 shifted = model.position + Vector3.right * (side * StepAsideReach * (1f + 0.4f * (run.Racer.Slot % 2)));
                steppedAside.Add((model, model.localPosition));
                model.position = shifted;
            }
        }


        /// <summary>The view has drawn: the runners that stepped aside for it are back where they run.</summary>
        private void StepBack(ScriptableRenderContext context, Camera view)
        {
            for (int i = steppedAside.Count - 1; i >= 0; i--)
            {
                (Transform model, Vector3 position) = steppedAside[i];
                if (model != null)
                {
                    model.localPosition = position;
                }
            }
            steppedAside.Clear();
        }


        /// <summary>Whether the views of a local race make overlapping runners step aside while they draw.</summary>
        private void StepAsideInViews(bool on)
        {
            RenderPipelineManager.beginCameraRendering -= StepAsideFor;
            RenderPipelineManager.endCameraRendering -= StepBack;
            if (on)
            {
                RenderPipelineManager.beginCameraRendering += StepAsideFor;
                RenderPipelineManager.endCameraRendering += StepBack;
            }
        }

        #endregion


        #region Results

        /// <summary>Every run is over: the standings, with Retry for the same race and the level select.</summary>
        private void ShowLocalResults()
        {
            phase = RunPhase.Menu;
            localRace.Rank();
            localResultsShown = true;
            sounds?.StopMusic();
            sounds?.Play(sounds.victory);
            List<RunnerRun> winners = localRace.Winners();
            RunnerLevel level = RunnerLevel;
            RunnerRun best = winners.Count > 0 ? winners[0] : Main;
            // A local race records nothing: the stars and records are the player's alone.
            ui?.ShowResults(new RunResult
            {
                LevelTitle = level != null ? RunnerGameTheme.TitleFor(level) : string.Empty,
                Victory = true,
                Endless = level != null && level.IsEndless,
                Local = true,
                Winner = winners.Count == 1 ? winners[0].Seat.ColoredName : null,
                Coins = best.Coins,
                Distance = best.Distance,
                Score = Mathf.FloorToInt(best.Score),
                Place = 1,
                Runners = localRace.Runs.Count,
                Standings = LocalStandingsText()
            });
            TransitionState(BaseGameState.Victory);
        }


        /// <summary>The standings of a local race, a line a runner in the colour of its seat, in the language shown.</summary>
        private string LocalStandingsText()
        {
            var standings = new StringBuilder();
            foreach (Racer racer in localRace.Ranking)
            {
                RunnerRun run = localRace.Runs[racer.Slot];
                string detail = RunnerText.SayF("{0:0} m, {1} coins", Mathf.Floor(racer.Distance), racer.Coins);
                standings.Append(standings.Length > 0 ? "\n" : string.Empty).Append(Standings.Line(Mathf.Max(1, racer.Place), racer.Name, false,
                    racer.Score.ToString(), Color.Lerp(run.Seat.Color, Color.white, 0.3f), detail));
            }
            return standings.ToString();
        }

        #endregion
    }
}
