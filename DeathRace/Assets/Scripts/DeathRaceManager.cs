using System.Collections;
using Gamebox;
using Gamebox.UI;
using UnityEngine;
using UnityEngine.Rendering;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// The Death Race module. Every race, whatever it is for, goes through one path: something builds a
    /// <see cref="RaceSetup"/> (the career's offer, a quick race, the split screen setup, the online room), the manager
    /// hands it to the <see cref="RaceDirector"/> (<see cref="BeginRace"/>), which builds the track, spawns the cars and
    /// runs the race, and when it is over the director's result comes back here (<see cref="HandleRaceFinished"/>) and
    /// goes to whatever the race was for. Behind the main menu the director runs computer cars round a track
    /// (attract mode). The halves: DeathRaceManager.Race.cs (attract mode, the race's hooks), .Career.cs and .Saves.cs
    /// (the career, quick races, named saves), .Local.cs and .Online.cs (several people), .Look.cs (the theme's light).
    /// Menus, pausing, custom settings and the state machine come from <see cref="BaseGameManager"/>.
    /// </summary>
    public partial class DeathRaceManager : BaseGameManager
    {
        [SerializeField] private DeathRaceSettings settings;
        [SerializeField] private DeathRaceController controller;
        [SerializeField] private DeathRaceUI ui;
        [SerializeField] private DeathRaceCampaign campaign;
        [SerializeField] private DeathRaceContent content;
        [SerializeField] private RaceDirector director;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Light sun;
        [SerializeField] private Volume volume;
        [SerializeField] private Component online;
        [SerializeField] private AudioSource music;
        [SerializeField] private RaceAudio sounds;

        private DeathRaceSettings customSettings;
        private DeathRaceSettings activeSettings;
        private DeathRaceProgress progress;

        public override IGameController Controller => controller;
        public override IGameUI UI => ui;
        public override ICampaign Campaign => campaign;
        public override IGameSettings DefaultSettings => settings;
        public override IGameSettings CustomSettings => customSettings != null ? customSettings : (customSettings = CreateCustomSettings());

        public override IGameSettings Settings
        {
            get => activeSettings != null ? activeSettings : settings;
            set
            {
                activeSettings = value as DeathRaceSettings;
                OnSettingsApplied();
            }
        }

        protected override bool UsesTimer => false;

        /// <summary>The settings in use (custom or default).</summary>
        public DeathRaceSettings Options => Settings as DeathRaceSettings ?? settings;

        public DeathRaceProgress Progress => progress ??= new DeathRaceProgress(Disk) { Avatar = LocalAvatar.Instance };

        /// <summary>The look in use.</summary>
        public DeathRaceTheme Look => ThemeAs<DeathRaceTheme>();

        public DeathRaceContent Content => content;

        public DeathRaceCampaign Tracks => campaign;

        public RaceDirector Director => director;

        public DeathRaceUI Interface => ui;

        /// <summary>The scene's camera: the overview behind the menu and the first player's view in a race.</summary>
        public Camera MainCamera => mainCamera;

        public Light Sun => sun;

        public Volume PostProcessing => volume;

        public AudioSource Music => music;

        /// <summary>The sounds of the game (one-shots in the world, the interface, the music).</summary>
        public RaceAudio Sounds => sounds;

        /// <summary>The race being run (attract mode included), or null.</summary>
        public Race CurrentRace => director != null ? director.Current : null;

        /// <summary>Whether a race of people is on (not the computer cars behind the menu).</summary>
        public bool IsRacing => director != null && director.IsRacing && director.Current != null && director.Current.Setup.Mode != RaceMode.Attract;

        protected override IOnlineLobby OnlineLobby => online as IOnlineLobby;

        private DeathRaceSettings CreateCustomSettings()
        {
            DeathRaceSettings copy = settings != null ? Instantiate(settings) : ScriptableObject.CreateInstance<DeathRaceSettings>();
            copy.name = $"{(settings != null ? settings.name : "DeathRaceSettings")} (custom)";
            return copy;
        }

        protected override void Awake()
        {
            base.Awake();
            if (director != null)
            {
                director.Finished += HandleRaceFinished;
                director.Started += HandleRaceStarted;
            }
        }

        protected override void Start()
        {
            progress = new DeathRaceProgress(Disk) { Avatar = LocalAvatar.Instance };
            if (sounds != null && content != null)
            {
                sounds.Setup(content.sounds, music);
            }
            base.Start();
            ApplyLook();
            OnSettingsApplied();
            StartAttract();
        }

        protected override void OnDestroy()
        {
            if (director != null)
            {
                director.Finished -= HandleRaceFinished;
                director.Started -= HandleRaceStarted;
            }
            base.OnDestroy();
        }

        /// <summary>The player picked another look: the light, the track and the cars take it.</summary>
        protected override void OnThemeChanged(GameTheme theme)
        {
            base.OnThemeChanged(theme);
            ApplyLook();
            if (director != null && director.Current != null && director.Current.Setup.Mode == RaceMode.Attract)
            {
                // The track behind the menu is built in the theme it was started in: build it again.
                StopAttract();
                StartAttract();
            }
            else
            {
                director?.Restyle(Look);
            }
        }

        protected override void OnLanguageChanged()
        {
            base.OnLanguageChanged();
            director?.RefreshTexts();
        }

        /// <summary>The settings changed: the sound and music volumes and the camera follow them.</summary>
        private void OnSettingsApplied()
        {
            DeathRaceSettings options = Options;
            if (options == null)
            {
                return;
            }
            AudioListener.volume = Mathf.Clamp01(options.volume);
            if (music != null)
            {
                music.volume = Mathf.Clamp01(options.musicVolume);
            }
            director?.ApplySettings(options);
        }

        // ------------------------------------------------------------------------------------------------
        // Races
        // ------------------------------------------------------------------------------------------------

        /// <summary>
        /// Runs <paramref name="setup"/>: the computer cars behind the menu make way, the director builds the race and it
        /// starts with the countdown. Every kind of race comes through here.
        /// </summary>
        public void BeginRace(RaceSetup setup)
        {
            if (setup == null || director == null)
            {
                return;
            }
            StopAttract();
            LoadLevel(setup.Track);
            director.Begin(setup);
            if (setup.Mode != RaceMode.Attract)
            {
                TransitionState(BaseGameState.Running);
            }
        }

        /// <summary>Leaves whatever race is on and goes back to the menu, with the computer cars behind it again.</summary>
        public void LeaveRace()
        {
            director?.Abort();
            if (InLocalMatch)
            {
                EndLocalMatch();
            }
            TransitionState(BaseGameState.Initialization);
        }

        private void HandleRaceStarted(Race race)
        {
            OnRaceStarted(race);
        }

        /// <summary>The director's race is over: the result goes to whatever the race was for.</summary>
        private void HandleRaceFinished(RaceResult result)
        {
            if (result == null || result.Setup == null)
            {
                return;
            }
            RecordTrack(result);
            switch (result.Setup.Mode)
            {
                case RaceMode.Attract:
                    // The computer cars behind the menu go round again on the next track.
                    StartAttract();
                    return;
                case RaceMode.Career:
                    OnCareerRaceFinished(result);
                    break;
                case RaceMode.Quick:
                    OnQuickRaceFinished(result);
                    break;
                case RaceMode.Local:
                    OnLocalRaceFinished(result);
                    break;
                case RaceMode.Online:
                    OnOnlineRaceFinished(result);
                    break;
            }
        }

        /// <summary>Stars and the best lap of the track, for a person at this device racing offline.</summary>
        private void RecordTrack(RaceResult result)
        {
            if (result.Setup.Mode == RaceMode.Attract || result.Setup.Mode == RaceMode.Online)
            {
                return;
            }
            foreach (RacerResult racer in result.Racers)
            {
                if (racer.Driver != DriverKind.Human)
                {
                    continue;
                }
                if (racer.BestLap > 0f)
                {
                    Progress.RecordLap(result.Setup.Track, racer.BestLap);
                }
                if (racer.Finished)
                {
                    Progress.RecordLevel(result.Setup.Track, DeathRaceCampaign.StarsFor(racer.Place), Mathf.Max(0, 4 - racer.Place) * 1000 + racer.Kills * 100);
                }
                Progress.CountWrecks(racer.Kills);
            }
        }

        /// <summary>Whether the people at this device all finished (or wrecked) a race: what the results screen is titled by.</summary>
        public static bool AnyHumanFinished(RaceResult result)
        {
            foreach (RacerResult racer in result.Racers)
            {
                if (racer.Driver == DriverKind.Human && racer.Finished)
                {
                    return true;
                }
            }
            return false;
        }

        partial void OnRaceStarted(Race race);
        partial void OnCareerRaceFinished(RaceResult result);
        partial void OnQuickRaceFinished(RaceResult result);
        partial void OnLocalRaceFinished(RaceResult result);
        partial void OnOnlineRaceFinished(RaceResult result);
        partial void ApplyLook();

        // ------------------------------------------------------------------------------------------------
        // BaseGameManager
        // ------------------------------------------------------------------------------------------------

        public override void TransitionState(GameState state)
        {
            base.TransitionState(state);
            if (state.BaseState == BaseGameState.Initialization && director != null && !InSession)
            {
                // Back on the menu: whatever race was on gives way to the computer cars.
                if (director.Current != null && director.Current.Setup.Mode != RaceMode.Attract)
                {
                    director.Abort();
                }
                if (director.Current == null)
                {
                    StartAttract();
                }
            }
        }

        /// <summary>The menu's Start (and a controller's Prepare): a quick race on the track loaded, with the settings' car.</summary>
        public override void StartGame()
        {
            BeginRace(QuickRaceSetup(LevelIndex));
        }

        public override void LoadLevel(int level)
        {
            int count = campaign != null ? campaign.Count : 1;
            LevelIndex = Mathf.Clamp(level, 0, Mathf.Max(0, count - 1));
            CurrentLevel = LevelData.Create(LevelIndex);
            UpdateLevel();
        }

        public override bool DoesSaveGameExist()
        {
            return HasSavedGames;
        }

        public override void SaveGame()
        {
            ShowSavedGames(true);
        }

        public override void LoadGame()
        {
            ShowSavedGames(false);
        }

        /// <summary>
        /// The launcher's picture: the computer cars behind the menu, caught at a moment when three of them are close
        /// together (a fight for a place reads better than one car alone), the menu hidden. Gives up waiting after a while.
        /// </summary>
        public override IEnumerator PoseForScreenshot()
        {
            float giveUp = Time.realtimeSinceStartup + 25f;
            yield return new WaitForSecondsRealtime(5f);
            while (Time.realtimeSinceStartup < giveUp && !PackTogether(3, 24f))
            {
                yield return null;
            }
            HideScreenInterface();
        }

        /// <summary>Whether <paramref name="cars"/> cars of the race (wrecks not counted) are within <paramref name="radius"/> metres of one of them.</summary>
        private bool PackTogether(int cars, float radius)
        {
            Race race = CurrentRace;
            if (race == null)
            {
                return false;
            }
            foreach (Car car in race.Cars)
            {
                if (car == null || car.IsWrecked)
                {
                    continue;
                }
                int near = 0;
                foreach (Car other in race.Cars)
                {
                    if (other != null && !other.IsWrecked && (other.Position2D - car.Position2D).sqrMagnitude <= radius * radius)
                    {
                        near++;
                    }
                }
                if (near >= cars)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
