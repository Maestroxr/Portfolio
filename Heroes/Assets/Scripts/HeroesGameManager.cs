using System.Collections;
using System.Collections.Generic;
using Gamebox;
using Gamebox.UI;
using Portfolio.Heroes.UI;
using UnityEngine;

namespace Portfolio.Heroes
{
    /// <summary>
    /// The Heroes module. It lays out a map from the scenario, starts a <see cref="HeroesGame"/> on it and directs it:
    /// every event the rules record is played out on the map (heroes walking, banners changing hands, armies fighting
    /// on the hexagons where they met) before the next decision is asked for, from a player through the interface or
    /// from a computer player through <see cref="AdventureAI"/> and <see cref="BattleAI"/>. Menus, pausing, settings
    /// and the state machine come from <see cref="BaseGameManager"/>; a scenario in progress is saved at the start of
    /// every turn and continues from the title screen. Games with players on other devices are in
    /// HeroesGameManager.Online.cs.
    /// </summary>
    public partial class HeroesGameManager : BaseGameManager
    {
        [SerializeField] private HeroesSettings settings;
        [SerializeField] private HeroesController controller;
        [SerializeField] private HeroesUI ui;
        [SerializeField] private HeroesCampaign campaign;
        [SerializeField] private HeroesArt art;
        [SerializeField] private CameraRig cameraRig;
        [SerializeField] private HeroesAudio sound;

        private const string SavePrefix = "Heroes.Save.";

        private HeroesSettings customSettings;
        private HeroesSettings activeSettings;
        private HeroesSettings scenarioSettings;
        private HeroesProgress progress;
        private AdventureAI brain;
        private Coroutine director;
        private MapSpec pendingMap;
        private bool finishing;
        private int refused;
        private int savedDay = -1;

        public HeroesGame Game { get; private set; }

        public MapView Map { get; private set; }

        public BattleView Battle { get; private set; }

        public PathView Path { get; private set; }

        public Effects Effects { get; private set; }

        public HeroesArt Art => art;

        public HeroesAudio Sound => sound;

        public CameraRig Rig => cameraRig;

        public HeroesUI HeroesUI => ui;

        public HeroesSettings Options => Settings as HeroesSettings ?? settings;

        public HeroesProgress Progress => progress ??= new HeroesProgress(Disk, GameType.Heroes);

        public HeroesLevel Scenario => campaign != null ? campaign.Scenario(LevelIndex) : null;

        public string ScenarioName => Scenario != null ? Scenario.Title : "Skirmish";

        /// <summary>The commands of the interface go to the local rules, or to the server in an online game.</summary>
        public IHeroesCommands Commands => IsOnlineGame && online != null ? online : (IHeroesCommands)controller;

        /// <summary>Whether the director is waiting for the player at this device to decide.</summary>
        public bool WaitingForHuman { get; private set; }

        /// <summary>The seat the screen belongs to: what the fog hides and whose resources are shown.</summary>
        public int Viewer { get; private set; }

        public PlayerState ViewerState => Game != null ? Game.State.Player(Viewer) : null;

        public override IGameController Controller => controller;
        public override IGameUI UI => ui;
        public override ICampaign Campaign => campaign;
        public override IGameSettings DefaultSettings => settings;
        public override IGameSettings CustomSettings => customSettings != null ? customSettings : customSettings = CreateCustomSettings();

        public override IGameSettings Settings
        {
            get => activeSettings != null ? activeSettings : settings;
            set => activeSettings = value as HeroesSettings;
        }

        protected override bool UsesTimer => false;

        private HeroesSettings CreateCustomSettings()
        {
            HeroesSettings copy = settings != null ? Instantiate(settings) : ScriptableObject.CreateInstance<HeroesSettings>();
            copy.name = $"{(settings != null ? settings.name : "HeroesSettings")} (custom)";
            return copy;
        }

        // ------------------------------------------------------------------ setting up

        protected override void Awake()
        {
            base.Awake();
            brain = new AdventureAI();
            Effects = gameObject.AddComponent<Effects>();
            Effects.Setup(art, cameraRig != null ? cameraRig.View : Camera.main);
            Battle = gameObject.AddComponent<BattleView>();
        }

        protected override void Start()
        {
            base.Start();
            sound?.PlayMenuMusic();
        }

        public override void LoadLevel(int level)
        {
            LevelIndex = campaign != null ? Mathf.Clamp(level, 0, Mathf.Max(0, campaign.Count - 1)) : 0;
            CurrentLevel = LevelData.Create(LevelIndex);
            UpdateLevel();
        }

        protected override void UpdateLevel()
        {
        }

        protected override void UpdateScore()
        {
        }

        /// <summary>The new game screen's Start: remember the map asked for and go through the controller.</summary>
        public void BeginScenario(MapSpec map, int level)
        {
            pendingMap = map;
            controller.PrepareGame(LevelData.Create(level));
        }

        public override void StartGame()
        {
            HeroesLevel scenario = Scenario;
            scenarioSettings = Options;
            string error = "no settings";
            if (scenarioSettings == null || !scenarioSettings.AreSettingsValid(out error))
            {
                UI?.UpdateError($"Cannot start: {error}");
                return;
            }
            if (InSession)
            {
                StartOnlineGame();
                return;
            }
            MapSpec spec = (pendingMap ?? (scenario != null ? scenario.Map.Clone() : null) ?? new MapSpec()).Clone();
            pendingMap = null;
            if (spec.seed == 0)
            {
                spec.seed = (uint)Random.Range(1, int.MaxValue);
            }
            GameState state = MapGenerator.Generate(spec);
            var game = new HeroesGame(state, new SeededRandom(spec.seed ^ 0x5bd1e995u));
            // The first day has to be opened; a loaded game goes on where it left off.
            game.Begin();
            Direct(game, true);
        }

        /// <summary>Builds the map and the interface for a game and starts playing it out.</summary>
        private void Direct(HeroesGame game, bool fresh)
        {
            StopDirecting();
            Game = game;
            finishing = false;
            savedDay = -1;
            refused = 0;
            brain = new AdventureAI();

            Build();
            Viewer = FirstHuman(game);
            Map.Sync();
            Map.Fog.Show(game.State.Player(Viewer));
            ui.Bind(this);
            ui.CloseAll();
            ui.Refresh();

            HeroState first = Game.State.Player(Viewer)?.heroes.Count > 0
                ? Game.State.Hero(Game.State.Player(Viewer).heroes[0])
                : null;
            if (first != null)
            {
                cameraRig?.Snap(Map.Point(first.cell));
            }
            cameraRig?.SetLimits(Map.Layout.GridBounds(TerrainBuilder.MaxHeight));

            TransitionState(new Gamebox.GameState(BaseGameState.Running));
            sound?.PlayAdventureMusic();
            // A lobby in the scene does not make a game online; a lockstep game does.
            director = StartCoroutine(IsOnlineGame ? DirectOnline() : Run());
        }

        private static int FirstHuman(HeroesGame game)
        {
            foreach (PlayerState player in game.State.players)
            {
                if (player.human && player.alive)
                {
                    return player.index;
                }
            }
            return 0;
        }

        /// <summary>Makes the map, the grid, the fog and the path markers for the game about to be played.</summary>
        private void Build()
        {
            if (Map != null)
            {
                Destroy(Map.gameObject);
            }
            var holder = new GameObject("Map");
            Map = holder.AddComponent<MapView>();
            Map.Build(Game.State, art, cameraRig != null ? cameraRig.View : Camera.main);
            Path = holder.AddComponent<PathView>();
            Path.Setup(Map, art);
            Battle.Setup(Map, art, Effects, Options);
        }

        private void StopDirecting()
        {
            if (director != null)
            {
                StopCoroutine(director);
                director = null;
            }
            WaitingForHuman = false;
        }

        /// <summary>Leaves the scenario for the title screen (it stays saved and continues from there).</summary>
        public void ReturnToTitle()
        {
            if (Game != null && !Game.IsOver)
            {
                SaveGame();
            }
            StopDirecting();
            LeaveOnline();
            Game = null;
            if (Map != null)
            {
                Destroy(Map.gameObject);
                Map = null;
            }
            ui.CloseAll();
            TransitionState(new Gamebox.GameState(BaseGameState.Initialization));
            sound?.PlayMenuMusic();
        }

        // ------------------------------------------------------------------ directing

        private float Speed => Mathf.Max(0.25f, Options != null ? Options.heroSpeed : 1f);

        /// <summary>The main loop: play out what happened, then ask whoever is due for the next command.</summary>
        private IEnumerator Run()
        {
            yield return null;
            while (Game != null)
            {
                if (!IsGameRunning)
                {
                    yield return null;
                    continue;
                }
                if (Game.HasEvents)
                {
                    WaitingForHuman = false;
                    ui.SetBusy(true);
                    foreach (GameEvent what in Game.TakeEvents())
                    {
                        yield return Play(what);
                    }
                    ui.SetBusy(false);
                    ui.Refresh();
                    continue;
                }
                if (Game.IsOver)
                {
                    yield return Finish();
                    director = null;
                    yield break;
                }
                int who = Game.WaitingPlayer;
                // In a battle the wandering armies of the wilds have no seat of their own (-1); the computer moves
                // them like any other. Outside a battle there is nothing to wait for.
                if (who < 0 && !(who == -1 && Game.InBattle))
                {
                    yield return null;
                    continue;
                }
                if (Game.IsComputer(who))
                {
                    yield return Think(who);
                    continue;
                }
                yield return Human(who);
            }
        }

        /// <summary>A computer player takes its turn, a command at a time, at a pace the player can follow.</summary>
        private IEnumerator Think(int who)
        {
            WaitingForHuman = false;
            GameCommand command = refused < 6 ? Game.InBattle ? BattleAI.Next(Game) : brain.Next(Game, who) : null;
            if (command == null)
            {
                // Nothing left to do, or nothing that works: end the turn rather than stall.
                command = Fallback(who);
            }
            yield return new WaitForSeconds((Game.InBattle ? 0.25f : 0.12f) / Speed);
            if (Submit(command))
            {
                refused = 0;
            }
            else
            {
                brain.Refused(command);
                refused++;
                if (refused > 12)
                {
                    Debug.LogWarning($"Heroes: {command.kind} keeps being refused for seat {who}; the game is stuck.");
                    Game.State.over = true;
                }
                // A refused command would spin the loop; give the rules a turn to move on.
                yield return null;
            }
        }

        /// <summary>The move that always works: defend in a battle, end the turn on the map.</summary>
        private GameCommand Fallback(int who)
        {
            return Game.InBattle
                ? GameCommand.Of(CommandKind.BattleDefend, who, Game.Battle.current)
                : GameCommand.Of(CommandKind.EndTurn, who);
        }

        /// <summary>Waits for the player at this device, showing the choice, the battle or the map as it stands.</summary>
        private IEnumerator Human(int who)
        {
            if (Viewer != who && !InSession)
            {
                // A hot seat: the screen changes hands, so the fog and the panels follow the player.
                Viewer = who;
                Map.Fog.Show(Game.State.Player(who));
                ui.Refresh();
            }
            if (Game.State.pending.Count > 0)
            {
                ui.ShowChoice(Game.State.pending[0]);
            }
            else if (Game.InBattle)
            {
                ui.ShowBattleTurn(Game.Battle);
            }
            else
            {
                Save(who);
                ui.ShowTurn(who);
            }
            WaitingForHuman = true;
            while (WaitingForHuman && Game != null && IsGameRunning && !Game.HasEvents)
            {
                yield return null;
            }
        }

        /// <summary>The interface sends everything through here; it is also where a command counts as played.</summary>
        public bool Submit(GameCommand command)
        {
            if (Game == null || command == null)
            {
                return false;
            }
            if (IsOnlineGame)
            {
                return SendOnline(command);
            }
            bool accepted = Game.Apply(command);
            if (accepted)
            {
                WaitingForHuman = false;
            }
            else
            {
                sound?.Play(Sfx.Error, 0.6f);
            }
            return accepted;
        }

        /// <summary>The scenario is over: the stars are counted and the end screen is shown.</summary>
        private IEnumerator Finish()
        {
            if (finishing)
            {
                yield break;
            }
            finishing = true;
            WaitingForHuman = false;
            GameState state = Game.State;
            bool won = state.winner >= 0 && state.Player(state.winner) != null && state.Player(state.winner).human;
            int days = state.day;
            HeroesLevel scenario = Scenario;
            int stars = won && scenario != null ? scenario.StarsFor(days) : 0;
            ClearSave();
            if (won)
            {
                PlayerState me = state.Player(Viewer);
                Progress.RecordWin(LevelIndex, stars, Mathf.Max(0, 1000 - days * 5), me != null ? me.creaturesKilled : 0);
                sound?.PlayVictoryMusic();
            }
            else
            {
                Progress.RecordLoss();
                sound?.PlayDefeatMusic();
            }
            yield return new WaitForSeconds(0.6f);
            ui.ShowEnd(won, stars, days);
            TransitionState(new Gamebox.GameState(won ? BaseGameState.Victory : BaseGameState.GameOver));
        }

        // ------------------------------------------------------------------ saving

        private string SaveKey => $"{SavePrefix}{LevelIndex}";

        /// <summary>Saves once a day, at the start of a human turn, so a scenario can be taken up again.</summary>
        private void Save(int who)
        {
            if (!Options.autoSave || InSession || Game.State.day == savedDay || Game.InBattle)
            {
                return;
            }
            savedDay = Game.State.day;
            SaveGame();
        }

        public override void SaveGame()
        {
            if (Game == null || Disk == null || InSession)
            {
                return;
            }
            Disk.SetString(SaveKey, JsonUtility.ToJson(Game.State));
            Disk.SetInt($"{SavePrefix}Level", LevelIndex);
            Disk.Persist();
        }

        public override bool DoesSaveGameExist()
        {
            return Disk != null && !string.IsNullOrEmpty(Disk.GetString(SaveKey));
        }

        public override void LoadGame()
        {
            if (Disk == null)
            {
                return;
            }
            string json = Disk.GetString(SaveKey);
            if (string.IsNullOrEmpty(json))
            {
                return;
            }
            var state = JsonUtility.FromJson<GameState>(json);
            if (state == null || state.version != GameState.Version)
            {
                ClearSave();
                return;
            }
            Direct(new HeroesGame(state, new SeededRandom(state.seed ^ (uint)state.day)), false);
        }

        private void ClearSave()
        {
            if (Disk != null)
            {
                Disk.SetString(SaveKey, "");
                Disk.Persist();
            }
        }

        // ------------------------------------------------------------------ what the interface asks of the manager

        /// <summary>The hero the player is giving orders to, or null.</summary>
        public HeroState Selected { get; private set; }

        public void Select(HeroState hero)
        {
            Selected = hero;
            Path.Clear();
            if (hero != null)
            {
                cameraRig?.Focus(Map.Point(hero.cell));
            }
            ui.Refresh();
        }

        /// <summary>The hero of the player whose turn it is that still has movement left, after the selected one.</summary>
        public HeroState NextHero()
        {
            PlayerState player = Game?.State.Player(Game.State.currentPlayer);
            if (player == null || player.heroes.Count == 0)
            {
                return null;
            }
            int start = Selected != null ? player.heroes.IndexOf(Selected.id) : -1;
            for (int i = 1; i <= player.heroes.Count; i++)
            {
                HeroState hero = Game.State.Hero(player.heroes[(start + i + player.heroes.Count) % player.heroes.Count]);
                if (hero != null && hero.alive && !hero.sleeping && hero.movement > 0)
                {
                    return hero;
                }
            }
            return null;
        }

        /// <summary>Works out the way a hero would take to a cell, for the trail under the pointer.</summary>
        public MovePlan PlanFor(HeroState hero, int cell)
        {
            return hero == null || Game == null ? null : Game.PlanPath(hero, cell);
        }

        public List<IPlayer> Seats => Players;
    }
}
